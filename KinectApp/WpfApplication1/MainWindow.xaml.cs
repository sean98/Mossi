using System.Windows;
using System.Diagnostics;
using System.ComponentModel;
using System.Timers;

namespace KinectApp
{

    public partial class MainWindow : Window
    {
        #region variables
        //consts
        private readonly string STREAM = "Stream", PAUSE = "Pause";
        
        //model & handlers
        private Model model;
        private SafeSerialPort port;
        private AlertHandler alerHandler;
        private KinectAngleHandler angleHandler;
        private KinectFrameHandler frameHandler;
        
        //variables from settings
        private double KINECT_HEIGHT;
        private int VERTICAL_ANGLE;
        private string SERVER_IP;
        private int SERVER_PORT;
        
        private Timer timer;
        private int timeForTurnOff = 10;

        private bool pir_detection = true;//true only for testing!!! should be false
        public bool PIR_Detection
        {
            get { return pir_detection; }
            set
            {
                if (pir_detection!=value)
                {
                    pir_detection = value;
                    DetectionPropertyChanged(this, new PropertyChangedEventArgs("PIR_Detection"));
                }
            }
        }
        #endregion

        #region constractor
        public MainWindow()
        {
            InitializeComponent();
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.RealTime;
            Screen.Height = 700;
            Screen.Width = 900;

            KINECT_HEIGHT = Properties.Settings.Default.KINECT_HEIGHT;
            VERTICAL_ANGLE = Properties.Settings.Default.VERTICAL_ANGLE;
            SERVER_IP = Properties.Settings.Default.SERVER_IP;
            SERVER_PORT = Properties.Settings.Default.SERVER_PORT;

            port = new SafeSerialPort();
            port.DataReceived += Port_DataReceived;
            
            timer = new Timer(timeForTurnOff * 1000);
			
            timer.Elapsed += timer_Elapsed;
            timer.AutoReset = false;
        }
        #endregion

        #region distractor
        void DataWindow_Closing(object sender, CancelEventArgs e)
        {
            Logger.writeLine("closing program");
            Logger.closeAndDispose();
            port.closeAndDispose();
            if (model != null) ;//TODO Dispose model
        }
        #endregion

        #region Timer
        void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Logger.writeLine("timer_elapsed");
            if (model != null)
                model.StopKinect();
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
        }

        private void DetectionPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (PIR_Detection)
            {
                TurnOnKinect();
                timer.Close();
            }
            if (frameHandler == null)
                return;

            byte Position = 0;
            if (frameHandler.Lying)
                Position |= 1 << 0;
            if (frameHandler.Sitting)
                Position |= 1 << 1;
            if (frameHandler.NotMoves)
                Position |= 1 << 2;
            if (frameHandler.HandsInTheAir)
                Position |= 1 << 3;
            if (frameHandler.LegsInTheAir)
                Position |= 1 << 4;

            if (alerHandler != null)
                alerHandler.Alert(1, frameHandler.TrackedPeople, Position);

            if (frameHandler.TrackedPeople == 0 && !PIR_Detection)
                timer.Start();
            else
                timer.Stop();
        }
        #endregion
      
        #region Kinect_Off_And_On
        private void TurnOnKinect()
        {
            Dispatcher.Invoke(() => btn_stream.Content = PAUSE);
            if (model != null)
            {
                model.StartKinect();
                return;
            }

            model = Model.getInstance(KINECT_HEIGHT, VERTICAL_ANGLE);
            if (model != null)
            {
                alerHandler = new AlertHandler(lbl_conection,SERVER_IP,SERVER_PORT);
                frameHandler = new KinectFrameHandler(Screen, model, alerHandler);
                angleHandler = new KinectAngleHandler(model.getSensor(), port);
                frameHandler.PropertyChanged += DetectionPropertyChanged;

                model.SetFrameHandler(frameHandler);
                model.setAngleHandler(angleHandler);

                model.EnableDepthFrame();
                model.EnableSkeletonFrame();
                
                model.StartKinect();
            }
        }
       
        private void TurnOffKinect()
        {
            Dispatcher.Invoke(() => btn_stream.Content = STREAM);
            if (model!=null)
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
            if (model != null)
                model.changeVerticalAngleTo((int)e.NewValue);
        }

        private void Down_Click(object sender, RoutedEventArgs e)
        {
            if (model != null)
                model.changeVerticalAngleBy(-5);
        }

        private void Up_Click(object sender, RoutedEventArgs e)
        {
            if (model != null)
                model.changeVerticalAngleBy(5);
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
            {
                settingWindow.Show();
                settingWindow.Activate();
            }
        }
        #endregion
    }
}