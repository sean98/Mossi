using Microsoft.Kinect;
using System;
using System.Collections;
using System.Drawing;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KinectApp.Handlers
{
    class KinectFrameHandler : Model.IKinectFrameHandler
    {
        #region Variable
        Model model;
        IAlertHandler alertHandler;
        public event EventHandler NumberOfPeopleChanged;
        private int trackedPeople = -1;
        #endregion

        public void OnNumberOfPeopleChanged(string name)
        {
            EventHandler handler = NumberOfPeopleChanged;
            if (handler != null)
            {
                handler(this, new EventArgs());
            }
        }

        public int TrackedPeople
        {
            get { return trackedPeople; }
            private set
            {
                if (value != trackedPeople) ; //TODO OnNumberOfPeopleChanged

                trackedPeople = value;
                if (trackedPeople == 0)
                {
                    try
                    {
                        model.scan();
                    }
                    catch (Exception ex)
                    {
                        //TODO log ex
                    }
                }
            }
        }

        #region skeleton_Variable
        private int skeletonFrameCounter;
        private Skeleton curSkeleton = null, preSkeleton = null;
        #endregion

        #region depth_Variable
        private System.Windows.Controls.Image screen;
        private short[] oldDepth, backround;
        private DepthImagePixel[] depthData;
        private int[] pixelData;
        const int ROUND = 1;

        private bool backroundCheck = false, oneColor = false;
        private const short BACKROUND_ACCURACY = 100;
        private int color = 255;
        #endregion

        #region Constructor
        public KinectFrameHandler(System.Windows.Controls.Image screen, Model model, IAlertHandler alertHandler)
        {
            this.alertHandler = alertHandler;
            this.model = model;
            this.screen = screen;
        }
        #endregion

        #region Frame_Methods
        public void skeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            ArrayList alerts = new ArrayList();//ArrayList of string for each tracked person

            if ((skeletonFrameCounter = ++skeletonFrameCounter % 30) != 0)//use one frame from 30
                return;

            using (SkeletonFrame frame = e.OpenSkeletonFrame())
            {
                if (frame != null)
                {
                    const double OPTIMAL_LOCATION = 0.5, RANGE = 0.1;
                    Skeleton[] skeleton = new Skeleton[frame.SkeletonArrayLength];
                    frame.CopySkeletonDataTo(skeleton);

                    int numOfTrackedPeople = 0;
                    double dist = 0;
                    foreach (Skeleton s in skeleton)
                    {   //if skeleton tracked increase number of people
                        if (s.TrackingState != SkeletonTrackingState.NotTracked && s.Position.X != 0 && s.Position.Y != 0 && s.Position.Z != 0)
                            numOfTrackedPeople++;
                        //if skeleton is not fully tracked exit for loop
                        if (s.TrackingState != SkeletonTrackingState.Tracked && s.Position.X != 0 && s.Position.Y != 0 && s.Position.Z != 0)
                            continue;

                        ArrayList alert = new ArrayList();

                        //checking height
                        JointType head = JointType.Head;
                        double headHeight = getHeight(s.Joints[JointType.Head]);
                        alert.Add(headHeight);
                        alert.Add(Math.Round(Math.Min(getHeight(s.Joints[JointType.FootRight]), getHeight(s.Joints[JointType.FootLeft])), 2));

                        if (headHeight > 1.3)
                            alert.Add(PositionType.standing);
                        else if (headHeight > 0.5)
                            alert.Add(PositionType.sitting);
                        else
                            alert.Add(PositionType.lying);


                        //checking if hands above head
                        //TODO replace 0.05 with const
                        if (getHeight(s.Joints[JointType.WristRight]) - 0.05 > headHeight)
                            alert.Add(PositionType.rightHand);//alert += "right hand  ";
                        if (getHeight(s.Joints[JointType.WristLeft]) - 0.05 > headHeight)
                            alert.Add(PositionType.leftHand);//alert += "left hand  ";

                        if (preSkeleton != null)
                        {
                            dist = skeletonDist(preSkeleton, curSkeleton);
                            dist += skeletonDist(curSkeleton, s);
                            alert.Add((dist > 1) ? PositionType.moves : PositionType.notMoves);
                        }

                        preSkeleton = curSkeleton;
                        curSkeleton = clone(s);

                        //check angles only for the first person
                        if (numOfTrackedPeople != 1)
                            continue;

                        //change vertical angle if person is not in range
                        if (Math.Abs(s.Joints[head].Position.Y - OPTIMAL_LOCATION) > RANGE)
                        {
                            //calculate angle
                            double tmp = Math.Sqrt(Math.Pow(s.Joints[head].Position.Y, 2) + Math.Pow(s.Joints[head].Position.Z, 2));
                            double alpha = Math.Asin(s.Joints[head].Position.Y / tmp);

                            tmp = Math.Sqrt(Math.Pow(OPTIMAL_LOCATION, 2) + Math.Pow(s.Joints[head].Position.Z, 2));
                            double beta = Math.Asin(OPTIMAL_LOCATION / tmp);
                            //chnage angle
                            try
                            {
                                model.changeVerticalAngleBy((int)((beta - alpha) * -180 / Math.PI));
                            }
                            catch (Exception ex)
                            {
                                //TODO log ex
                            }
                        }

                        //change horizontal angle if person not in range
                        if (Math.Abs(s.Position.X) > 0.2)
                        {
                            int angle = (int)(180 * Math.Atan(-s.Position.X / s.Position.Z) / Math.PI);
                            alert.Add("" + angle);
                            try
                            {
                                model.changeHorizontalAngleBy(angle);
                            }
                            catch (Exception ex)
                            {
                                //TODO log ex
                            }
                        }
                        alerts.Add(alert);
                    }
                    TrackedPeople = numOfTrackedPeople;
                    alertHandler.alert(numOfTrackedPeople, alerts);

                }
            }
        }

        public void depthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {
            using (DepthImageFrame frame = e.OpenDepthImageFrame())
            {
                try
                {
                    if (depthData == null)
                        depthData = new DepthImagePixel[frame.PixelDataLength];
                    if (oldDepth == null)
                        oldDepth = new short[depthData.Length];
                    if (pixelData == null)
                        pixelData = new int[depthData.Length];

                    frame.CopyDepthImagePixelDataTo(depthData);

                    int max = /*kSensor.DepthStream.TooFarDepth;*/(int)(((KinectSensor)sender).DepthStream.MaxDepth);
                    int min = 0;//kSensor.DepthStream.TooNearDepth;
                    // int middle = (short)((max + min) / 2), maxDis = (short)(middle - min);//Var for option 1
                    int dis = (max - min) / 3, first = min + dis, second = max - dis;//var for option 2

                    if (backround == null)
                        backround = new short[depthData.Length];
                    int count = 0;

                    for (int i = 0; i < depthData.Length; i++)
                    {
                        //                      depthData[i].Depth = (short)((depthData[i].Depth + depthSense / 2) * depthSense / depthSense);
                        short tempDepth = depthData[i].Depth;

                        if (backroundCheck && dist(oldDepth[i], tempDepth) < BACKROUND_ACCURACY)
                            backround[i] = backround[i] < tempDepth ? tempDepth : backround[i];
                        //pixelData[i] = tempDepth;
                        if (tempDepth > backround[i])
                            backround[i] = tempDepth;
                        //coloring
                        if ((dist(backround[i], tempDepth) < BACKROUND_ACCURACY && backroundCheck
                            || tempDepth >= max || tempDepth <= min))
                        {
                            pixelData[i] = 0;
                            count++;
                        }
                        else if (oneColor)
                            pixelData[i] = color;
                        else //OPTION 2 (red->blue->green->red)
                        {

                            byte r = 0, g = 0, b = 0;

                            if (tempDepth > min && tempDepth <= first)
                            {
                                int tmpDis = tempDepth - min;
                                b = (byte)(tmpDis * 255 / dis);
                                r = (byte)((dis - tmpDis) * 255 / dis);
                            }
                            else if (tempDepth > first && tempDepth <= second)
                            {
                                int tmpDis = tempDepth - first;
                                g = (byte)(tmpDis * 255 / dis);
                                b = (byte)((dis - tmpDis) * 255 / dis);
                            }
                            else if (tempDepth < max && tempDepth > second)
                            {
                                int tmpDis = tempDepth - second;
                                r = (byte)(tmpDis * 255 / dis);
                                g = (byte)((dis - tmpDis) * 255 / dis);
                            }
                            pixelData[i] = createColor(0, r, g, b);
                        }
                        oldDepth[i] = tempDepth;
                    }

                    Bitmap bmpFrame = new Bitmap(frame.Width, frame.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    var bmpData = bmpFrame.LockBits(new System.Drawing.Rectangle(0, 0, frame.Width, frame.Height), System.Drawing.Imaging.ImageLockMode.WriteOnly, bmpFrame.PixelFormat);
                    System.Runtime.InteropServices.Marshal.Copy(pixelData, 0, bmpData.Scan0, frame.PixelDataLength);
                    bmpFrame.UnlockBits(bmpData);
                    screen.Source = imageSourceFromBitmap(bmpFrame);
                }
                catch
                {
                }
            }
        }
        #endregion

        #region skeleton_help_methods
        private double getHeight(Joint joint)
        {
            double ang = -model.getVerticalAngle() * Math.PI / 180 + model.getKinectAngle();//to radians
            double tmp = joint.Position.Y * Math.Cos(ang) - joint.Position.Z * Math.Sin(ang);
            return model.getKinectHeight() + tmp;
        }

        private Skeleton clone(Skeleton skOrigin)
        {
            // isso serializa o skeleton para a memoria e recupera novamente, fazendo uma cópia do objeto
            MemoryStream ms = new MemoryStream();
            BinaryFormatter bf = new BinaryFormatter();

            bf.Serialize(ms, skOrigin);

            ms.Position = 0;
            object obj = bf.Deserialize(ms);
            ms.Close();

            return obj as Skeleton;
        }

        private double skeletonDist(Skeleton s1, Skeleton s2)
        {
            double dist = 0;
            for (int i = 0; i < s1.Joints.Count; i++)
                dist += Math.Sqrt(Math.Pow(s1.Joints[(JointType)i].Position.X - s2.Joints[(JointType)i].Position.X, 2)
                    + Math.Pow(s1.Joints[(JointType)i].Position.Y - s2.Joints[(JointType)i].Position.Y, 2)
                    + Math.Pow(s1.Joints[(JointType)i].Position.Z - s2.Joints[(JointType)i].Position.Z, 2));
            return dist;
        }
        #endregion

        #region depth_help_methods
        private int dist(int x, int y)
        {
            return Math.Abs(x - y);
        }

        private int createColor(byte alpha, byte red, byte green, byte blue)
        {
            return (alpha << 24) | (roundBy(red, ROUND) << 16) | (roundBy(green, ROUND) << 8) | roundBy(blue, ROUND);
        }

        private int roundBy(int num, int round)
        {
            num /= round;
            return num * round;
        }

        private ImageSource imageSourceFromBitmap(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();

                return bitmapimage;
            }
        }
        #endregion

        #region AlertHandler
        public enum PositionType { standing, sitting, lying, rightHand, leftHand, moves, notMoves };

        public interface IAlertHandler
        {
            void alert(int numberOfPeople, ArrayList alerts);
        }
        #endregion
    }
}
