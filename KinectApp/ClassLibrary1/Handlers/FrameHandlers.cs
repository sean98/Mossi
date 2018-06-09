using System;
using Microsoft.Kinect;
using System.Drawing;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MossiApi
{
    public class KinectFrameHandler : Model.IKinectFrameHandler
    {
        #region Variable
        Model model;
        #endregion

        #region Properties
        public event PropertyChangedEventHandler SituationPropertyChanged, HeightPropertyChanged;
        public event EventHandler PixelDataReady;

        private bool propertyUpdated = false, sitting = false, lying = false, handsInTheAir = false,
            legsInTheAir = false, notMoves = false, frameRealiable = false;
        private double headHeight = 0, legHeight = 0, handsHeight=0;
        private int trackedPeople = -1;

        public double SkeletonFPS { get; set; }
        public double DepthFPS { get; set; }
        public bool GenaratePixelData { get; set; } = false;

        public void OnHeightPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChangedEventHandler handler = HeightPropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
            //HeightPropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public void OnSituationPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChangedEventHandler handler = SituationPropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
            //PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public void OnPixelDataReady(int width, int height)
        {
            EventHandler handler = PixelDataReady;
            if (handler != null)
            {
                handler(this, new PixelDataEventArgs((int[]) pixelData.Clone(), width, height));
            }
        }

        public int TrackedPeople
        {
            get { return trackedPeople; }
            private set
            {
                if (value != trackedPeople)
                {
                    trackedPeople = value;
                    propertyUpdated = true;
                }
                if (trackedPeople == 0)
                    model.scan();
            }
        }

        public bool Sitting
        {
            get { return sitting; }
            set
            {
                if (value != sitting)
                {
                    sitting = value;
                    propertyUpdated = true;
                    Debug.WriteLine("Sitting");
                }
            }
        }

        public bool Lying
        {
            get { return lying; }
            set
            {
                if (lying != value)
                {
                    lying = value;
                    propertyUpdated = true;
                    Debug.WriteLine("Lying");
                }
            }
        }

        public bool NotMoves
        {
            get { return notMoves; }
            set
            {
                if (value != notMoves)
                {
                    notMoves = value;
                    propertyUpdated = true;
                    Debug.WriteLine("NotMoves");
                }
            }
        }

        public bool HandsInTheAir
        {
            get { return handsInTheAir; }
            set
            {
                if (value != handsInTheAir)
                {
                    handsInTheAir = value;
                    propertyUpdated = true;
                    Debug.WriteLine("HandsInTheAir");
                }
            }
        }

        public bool LegsInTheAir
        {
            get { return legsInTheAir; }
            set
            {
                if (value != legsInTheAir)
                {
                    legsInTheAir = value;
                    propertyUpdated = true;
                    Debug.WriteLine("LegsInTheAir");
                }
            }
        }

        public double HeadHeight
        {
            get { return headHeight; }
            set
            {
                if (value!=headHeight)
                {
                    headHeight = value;
                    OnHeightPropertyChanged();

                    if (headHeight > 1.4)
                    {
                        Lying = false;
                        Sitting = false;
                    }
                    else if (headHeight > 0.5)
                    {
                        Lying = false;
                        Sitting = true;
                    }
                    else
                    {
                        Lying = true;
                        Sitting = false;
                    }
                }
            }
        }

        public double LegHeight
        {
            get { return legHeight; }
            set
            {
                if (value != legHeight)
                {
                    legHeight = value;
                    OnHeightPropertyChanged();

                    if (LegHeight > 0.25)
                        LegsInTheAir = true;
                    else
                        LegsInTheAir = false;
                }
            }
        }

        public double HandsHeight
        {
            get { return handsHeight; }
            set
            {
                if (handsHeight!=value)
                {
                    handsHeight = value;
                    OnHeightPropertyChanged();

                    if (handsHeight > HeadHeight)
                        HandsInTheAir = true;
                    else
                        HandsInTheAir = false;
                }
            }
        }

        public bool FrameRealiable
        {
            get { return frameRealiable; }
            set
            {
                if (frameRealiable!=value)
                {
                    frameRealiable = value;
                    OnSituationPropertyChanged();
                }
            }
        }

        #endregion

        #region skeleton_Variable
        private readonly double OPTIMAL_LOCATION = 0.5, RANGE = 0.1;
        private static readonly int skletonArrayListMaxSize = 4;
        private IList<Skeleton> skeletonHistory = new CircularArrayList<Skeleton>(skletonArrayListMaxSize);
        private SkeletonFrame skeletonFrame;
        private Skeleton[] skeleton;
        private int skeletonFrameCounter;
        #endregion

        #region depth_Variable
        private int maxDepth, minDepth, disDepth, firstDepth, secondDepth;
        private DepthImageFrame depthFrame;
        private DepthImagePixel[] depthData;
        private int[] pixelData;
        private int depthFrameCounter = 0;
        #endregion

        #region Constructor
        public KinectFrameHandler(Model model)
        {
            this.model = model;

            SkeletonFPS = 1;
            DepthFPS = 30;

            maxDepth = 8000;// model.getSensor().DepthStream.TooFarDepth - 1;
            minDepth = model.getSensor().DepthStream.TooNearDepth + 1;

            disDepth = (maxDepth - minDepth) / 3;
            firstDepth = minDepth + disDepth;
            secondDepth = firstDepth + disDepth;
        }
        #endregion

        #region Frame_Methods
        
        public void SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            try
            {
                skeletonFrame = e.OpenSkeletonFrame();
                if (skeletonFrame == null)
                    return;
                if ((skeletonFrameCounter = ++skeletonFrameCounter % (int)(30/SkeletonFPS)) != 0)//use one frame from 30
                {
                    skeletonFrame.Dispose();
                    return;
                }
                skeleton = new Skeleton[skeletonFrame.SkeletonArrayLength];
                skeletonFrame.CopySkeletonDataTo(skeleton);
                
                int tempTrackedPeople = 0;
                foreach (Skeleton s in skeleton)
                {
                    //if s is untracked exit for loop
                    if (s.TrackingState == SkeletonTrackingState.NotTracked ||
                        (s.Position.X == 0 && s.Position.Y == 0 && s.Position.Z == 0))
                        continue;
                    tempTrackedPeople++;
                    //if s is not fully tracked exit for loop
                    if (s.TrackingState != SkeletonTrackingState.Tracked)
                        continue;
                   
                    //checking height
                    HeadHeight = GetHeight(s.Joints[JointType.Head]);

                    LegHeight = Math.Min(GetHeight(s.Joints[JointType.FootRight]),
                        GetHeight(s.Joints[JointType.FootLeft]));

                    //checking if hands above head
                    HandsHeight = Math.Min(GetHeight(s.Joints[JointType.HandRight]), GetHeight(s.Joints[JointType.HandLeft]));

                    skeletonHistory.Add(CloneSkeleton(s));
                    NotMoves = (skletonArrayListMaxSize==skeletonHistory.Count) && 
                        (SkeletonDistance(skeletonHistory) < 0.1 ? true : false);

                    //check angles only for the first person
                    if (tempTrackedPeople > 1)
                        continue;

                    //change vertical angle if person is not in range
                    if (Math.Abs(s.Joints[JointType.Head].Position.Y - OPTIMAL_LOCATION) > RANGE)
                    {
                        //calculate angle
                        int angle = (int)(180 * (Math.Atan(s.Joints[JointType.Head].Position.Y / s.Position.Z) -
                            Math.Atan(OPTIMAL_LOCATION / s.Position.Z)) / Math.PI);
                        try
                        {
                            model.changeVerticalAngleBy(angle);
                        }
                        catch (Exception ex)
                        {
                            Log.WriteLine(ex.Message);
                        }
                    }

                    //change horizontal angle if person not in range
                    if (Math.Abs(s.Position.X) > 0.2)
                    {
                        int angle = (int)(180 * Math.Atan(s.Position.X / s.Position.Z) / Math.PI);
                        try
                        {
                            model.changeHorizontalAngleBy(-angle);
                        }
                        catch (Exception ex)
                        {
                            Log.WriteLine(ex.Message);
                        }
                    }
                }//End of for
                
                TrackedPeople = tempTrackedPeople;
                
                if (propertyUpdated)
                {
                    propertyUpdated = false;
                    OnSituationPropertyChanged();
                }
            } catch (Exception ex)
            {
                Log.WriteLine(ex.Message);
            }
            finally
            {
                if (skeletonFrame != null)
                    skeletonFrame.Dispose();
            }
        }

        public void DepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {
            //if ((depthFrameCounter = ++depthFrameCounter % 30) != 0)//use one frame from 30
            //    return;
            try
            {
                depthFrame = e.OpenDepthImageFrame();
                if (depthFrame == null)
                    return;

                if ((depthFrameCounter = ++depthFrameCounter % (int)(30 / DepthFPS)) != 0)//use one frame from 30
                {
                    depthFrame.Dispose();
                    return;
                }
                try
                {
                    if (depthData == null)
                        depthData = new DepthImagePixel[depthFrame.PixelDataLength];
                    if (pixelData == null)
                        pixelData = new int[depthData.Length];

                    depthFrame.CopyDepthImagePixelDataTo(depthData);

                    int badPixels = 0;
                    for( int i=0; i<depthData.Length; i++)
                    {
                        if (depthData[i].Depth < minDepth || depthData[i].Depth > maxDepth)
                            ++badPixels;
                    }

                    if ((double)badPixels / depthData.Length > 0.8)
                        FrameRealiable = false;
                    else
                        FrameRealiable = true;

                    if (GenaratePixelData)
                    {
                        for(int i=0; i < depthData.Length; i++)
                        {

                            short tempDepth = depthData[i].Depth;
                            byte r = 0, g = 0, b = 0;

                            if (tempDepth >= minDepth && tempDepth < firstDepth)
                            {
                                tempDepth = (short)((tempDepth - minDepth) * 256 / disDepth);
                                b = (byte)tempDepth;
                                r = (byte)(255 - tempDepth);
                            }
                            else if (tempDepth >= firstDepth && tempDepth < secondDepth)
                            {
                                tempDepth = (short)((tempDepth - firstDepth) * 256 / disDepth);
                                g = (byte)(tempDepth);
                                b = (byte)(255 - tempDepth);
                            }
                            else if (tempDepth >= secondDepth && tempDepth <= maxDepth)
                            {
                                tempDepth = (short)((tempDepth - secondDepth) * 256 / disDepth);
                                r = (byte)(tempDepth);
                                g = (byte)(255 - tempDepth);
                            }

                            pixelData[i] = CreateColor(0, r, g, b);
                        };
                        OnPixelDataReady(depthFrame.Width, depthFrame.Height);
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteLine(ex.Message);
                }
            }
            catch (Exception ex)
            {
                //TODO log ex
            }
            finally
            {
                if (depthFrame != null)
                    depthFrame.Dispose();
            }
        }
        #endregion

        #region skeleton_help_methods
        private double GetHeight(Joint joint)
        {
            double ang = (-model.getVerticalAngle() + model.getKinectAngle()) * Math.PI / 180;//to radians
            double tmp = joint.Position.Y * Math.Cos(ang) - joint.Position.Z * Math.Sin(ang);
            return model.getKinectHeight() + tmp;
        }

        private Skeleton CloneSkeleton(Skeleton origin)
        {
            // isso serializa o skeleton para a memoria e recupera novamente, fazendo uma cópia do objeto
            MemoryStream ms = new MemoryStream();
            BinaryFormatter bf = new BinaryFormatter();

            bf.Serialize(ms, origin);

            ms.Position = 0;
            object obj = bf.Deserialize(ms);
            ms.Close();

            return obj as Skeleton;
        }

        private double SkeletonDistance(IList<Skeleton> skeletons)
        {
            double dist = 0;
            for (int i=1;i<skeletons.Count;i++)
            {
                dist += Math.Sqrt(Math.Pow(skeletons[i].Position.X - skeletons[i - 1].Position.X, 2)
                                + Math.Pow(skeletons[i].Position.Y - skeletons[i - 1].Position.Y, 2)
                                + Math.Pow(skeletons[i].Position.Z - skeletons[i - 1].Position.Z, 2));
            }
            return dist;
        }
        #endregion

        #region depth_help_methods
        private int dist(int x, int y)
        {
            return Math.Abs(x - y);
        }

        private int CreateColor(byte alpha, byte red, byte green, byte blue)
        {
            return (alpha << 24) | (red << 16) | (green << 8) | blue;
        }
        #endregion
    }

    public class PixelDataEventArgs : EventArgs
    {
        private int[] pixelData;
        private int width, height;

        public PixelDataEventArgs(int[] pixelData, int width, int height)
        {
            this.pixelData = pixelData;
            this.width = width;
            this.height = height;
        }

        public int[] GetPixelData()
        {
            return pixelData;
        }

        public int getWidth()
        {
            return width;
        }

        public int getHeight()
        {
            return height;
        }
    }
}
