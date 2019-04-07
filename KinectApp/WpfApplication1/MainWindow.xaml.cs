using System;
using MossiApi;
using System.IO;
using System.Timers;
using System.Windows;
using System.Drawing;
using System.Diagnostics;
using System.Windows.Media;
using System.ComponentModel;
using System.Windows.Media.Imaging;
using System.Threading;

namespace KinectApp
{

    public partial class MainWindow : Window
    {
        #region variables
        //consts
        private enum Situations : byte { Lying, Sitting, NotMoves, HandsInTheAir, LegsInTheAir, DoorClosed };
        private readonly string STREAM = "Stream", PAUSE = "Pause";

        //model & handlers
        private Model model = null;
        private SafeSerialPort port = null;
        private AlertHandler alerHandler = null;
        private KinectAngleHandler angleHandler = null;
        private KinectFrameHandler frameHandler = null;


        //variables from settings
        private int SERVER_PORT;
        private string SERVER_IP;
        private int VERTICAL_ANGLE;
        private double KINECT_HEIGHT;


        private int timeForTurnOff = 30;//seconds
        private System.Timers.Timer shutDownTimer, restartTimer;

        //Properties
        private bool pir_detection = false; //true only for testing!!! should be false
        public bool PIR_Detection
        {
            get { return pir_detection; }
            set
            {
                if (pir_detection != value)
                {
                    pir_detection = value;
                    DetectionPropertyChanged(this, new PropertyChangedEventArgs("PIR_Detection"));
                }
            }
        }

        private bool doorClosed = false;
        public bool DoorClosed
        {
            get { return doorClosed; }
            set
            {
                if (doorClosed != value)
                {
                    doorClosed = value;
                    DetectionPropertyChanged(this, new PropertyChangedEventArgs("DoorClosed"));
                }
            }
        }
        #endregion
        
        #region constractor
        public MainWindow()
        {
            InitializeComponent();
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.RealTime;

            KINECT_HEIGHT = Properties.Settings.Default.KINECT_HEIGHT;
            VERTICAL_ANGLE = Properties.Settings.Default.VERTICAL_ANGLE;
            SERVER_IP = Properties.Settings.Default.SERVER_IP;
            SERVER_PORT = Properties.Settings.Default.SERVER_PORT;

            port = new SafeSerialPort();
            port.DataReceived += Port_DataReceived;

            shutDownTimer = new System.Timers.Timer(timeForTurnOff * 1000);
            shutDownTimer.Elapsed += ShutDownTimer_Elapsed;
            shutDownTimer.AutoReset = false;

            restartTimer = new System.Timers.Timer(60*1000);
            restartTimer.Elapsed += RestartTimer_Elapsed;
            restartTimer.AutoReset = false;

            TurnOnKinect();
        }
        #endregion

        #region distractor
        void DataWindow_Closing(object sender, CancelEventArgs e)
        {
            if (model != null)
            {
                model.getSensor().Dispose();
                model = null;
            }

            if (alerHandler != null)
                alerHandler.Dispose();
            //timer.Dispose();
            port.closeAndDispose();
            Application.Current.Shutdown();
        }
        #endregion

        #region Timers

        private void RestartTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            TurnOffKinect();
            Thread.Sleep(2000);
            TurnOnKinect();
        }

        void ShutDownTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            TurnOffKinect();
        }
        #endregion

        #region Events
        private void Port_DataReceived(object sender, PropertyChangedEventArgs e)
        {
            string msg = ((SafeSerialPort)sender).Message;
            Dispatcher.Invoke(() => lbl_status.Content = msg);

            if (msg.Equals("ON"))
                PIR_Detection = true;
            else if (msg.Equals("OFF"))
                PIR_Detection = false;
            else if (msg.Equals("OPENED"))
                DoorClosed = false;
            else if (msg.Equals("CLOSED"))
                DoorClosed = true;
        }

        private void DetectionPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            restartTimer.Stop();
            restartTimer.Start();
            try
            {
                string alert = "";// = "angle is " + model.getSensor().ElevationAngle + "\n";
                if (PIR_Detection)
                {
                    TurnOnKinect();
                    shutDownTimer.Close();
                }
                byte Position = 0;
                if (DoorClosed)
                {
                    Position |= 1 << (byte)Situations.DoorClosed;
                    alert += "Door closed";
                }
                if (frameHandler != null)
                {


                    alert += frameHandler.TrackedPeople + (frameHandler.TrackedPeople == 1 ? " Person" : " People") + " In the room";
                    if (frameHandler.TrackedPeople > 0)
                    {
                        if (!frameHandler.FrameRealiable)
                        {
                            Dispatcher.Invoke(() => lbl_conection.Content = "bad data");
                            alerHandler.Alert(new byte[] { 0 });
                            return;
                        }

                        if (frameHandler.Lying)
                        {
                            Position |= 1 << (byte)Situations.Lying;
                            alert += " Lying";
                        }
                        if (frameHandler.Sitting)
                        {
                            Position |= 1 << (byte)Situations.Sitting;
                            alert += " Sitting";
                        }
                        if (frameHandler.NotMoves)
                        {
                            Position |= 1 << (byte)Situations.NotMoves;
                            alert += " Not Moves";
                        }
                        if (frameHandler.HandsInTheAir)
                        {
                            Position |= 1 << (byte)Situations.HandsInTheAir;
                            alert += " Hand In The Air";
                        }
                        //if (frameHandler.LegsInTheAir)
                        //{
                        //    Position |= 1 << (byte)Situations.LegsInTheAir;
                        //    alert += " Legs In The Air";
                        //}
                    }
                    if (frameHandler.TrackedPeople == 0 && !PIR_Detection)
                        shutDownTimer.Start();
                    else
                        shutDownTimer.Stop();
                }
                Dispatcher.Invoke(() => lbl_conection.Content = alert);
                if (alerHandler != null)
                {
                    byte[] msg = new byte[] { (byte)2, (byte)frameHandler.TrackedPeople, (byte)Position };
                    alerHandler.Alert(msg);
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine(ex.Message);
            }
        }

        private void HeightPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            lbl_conection.Content = "Head Height: " + Math.Round(((KinectFrameHandler)sender).HeadHeight, 2)
                               + " Legs Height: " + Math.Round(((KinectFrameHandler)sender).LegHeight, 2);
        }

        private void PixelDataReady(object sender, EventArgs e)
        {
            int[] pixels = ((PixelDataEventArgs)e).GetPixelData();
            int width = ((PixelDataEventArgs)e).getWidth(), height = ((PixelDataEventArgs)e).getHeight();

            Bitmap bmpFrame = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            var bmpData = bmpFrame.LockBits(new Rectangle(0, 0, width, height), System.Drawing.Imaging.ImageLockMode.WriteOnly, bmpFrame.PixelFormat);
            System.Runtime.InteropServices.Marshal.Copy(pixels, 0, bmpData.Scan0, pixels.Length);
            bmpFrame.UnlockBits(bmpData);
            Dispatcher.Invoke(() => Screen.Source = imageSourceFromBitmap(bmpFrame));
        }
        #endregion

        #region Kinect_Off_And_On
        private void TurnOnKinect()
        {
            Dispatcher.Invoke(() => btn_stream.Content = PAUSE);
            if (model != null)
                model.StartKinect();
            else
            {
                model = Model.getInstance(KINECT_HEIGHT, VERTICAL_ANGLE);
                if (model != null)
                {
                    alerHandler = new AlertHandler(SERVER_IP, SERVER_PORT);
                    frameHandler = new KinectFrameHandler(model);
                    angleHandler = new KinectAngleHandler(model.getSensor(), port, VERTICAL_ANGLE);

                    frameHandler.PositionPropertyChanged += DetectionPropertyChanged;
                    frameHandler.PixelDataReady += PixelDataReady;
                    frameHandler.GenaratePixelData = true;
                    frameHandler.HeightPropertyChanged += HeightPropertyChanged;

                    model.SetFrameHandler(frameHandler);
                    model.setAngleHandler(angleHandler);

                    model.EnableDepthFrame();
                    model.EnableSkeletonFrame();

                    model.StartKinect();
                }
            }
        }

        private void TurnOffKinect()
        {
            restartTimer.Stop();
            Dispatcher.Invoke(() => btn_stream.Content = STREAM);
            if (model != null)
                model.StopKinect();
        }
        #endregion

        #region GUI_Events
        private void Stream_Click(object sender, RoutedEventArgs e)
        {
            if (btn_stream.Content.Equals(STREAM))
                TurnOnKinect();
            else
                TurnOffKinect();
        }

        private void Angle_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                if (model != null)
                    model.changeVerticalAngleTo((int)e.NewValue);
            } catch (Exception ex)
            {
                Log.WriteLine(ex.Message);
            }
        }

        private void Down_Click(object sender, RoutedEventArgs e)
        {
            try
            { 
            if (model != null)
                model.changeVerticalAngleBy(-5);
            }
            catch (Exception ex)
            {
                Log.WriteLine(ex.Message);
            }
        }

        private void Up_Click(object sender, RoutedEventArgs e)
        {
            try
            { 
            if (model != null)
                model.changeVerticalAngleBy(5);
            }
            catch (Exception ex)
            {
                Log.WriteLine(ex.Message);
            }
        }

        private void Tracking_Checked(object sender, RoutedEventArgs e)
        {
            if (model != null)
                model.EnableSkeletonFrame();
        }

        private void Tracking_Unchecked(object sender, RoutedEventArgs e)
        {
            if (model != null)
                model.DisableSkeletonFrame();
        }

        UpdateSettings settingWindow = new UpdateSettings();
        private void Update_Setting(object sender, RoutedEventArgs e)
        {
            if (!settingWindow.IsActive)
                settingWindow.Show();
        }
        #endregion

        #region Helpers
        private bool IsSituation(byte state, Situations s)
        {
            return (state & (1 << (byte)s)) == 1 ? true : false;
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
    }
}