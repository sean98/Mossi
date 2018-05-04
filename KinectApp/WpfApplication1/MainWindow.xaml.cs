using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.ComponentModel;
using System.Configuration;
using System.IO.Ports;
using System.Collections;
using System.Timers;

namespace KinectApp
{

    public partial class MainWindow : Window
    {
        #region variables
        //consts
        private const string CONNECTION = "conection: ", STATUS = "status: ";
        private const string STREAM = "Stream", PAUSE = "Pause";
        private const short BACKROUND_ACCURACY = 100;
        private const long TIME_IN_FRAMES = 1;
        //model
        private Model model=null;
        private double KINECT_HEIGHT = 1.75;
        private int KINECT_ANGLE = 0;
        static SafeSerialPort port;
        int verticalAngle = -10, horizontalAngle = 0;
        KinectAngleHandler angleHandler;
        System.Timers.Timer timer;
        bool turnOffKinectFlag = false;
        KinectFrameHandler FrameHandler;


        static Logger logger;
        #endregion
       
        #region constractor
        public MainWindow()
        {
            InitializeComponent();
            //Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.AboveNormal;
            Screen.Height = 700;
            Screen.Width = 900;

            port = new SafeSerialPort();
            port.DataReceived += port_DataReceived;

            logger = new Logger();
            logger.writeLine("Initialize Program");

            timer = new System.Timers.Timer(10 * 1000);
            timer.Elapsed += timer_Elapsed;
            timer.AutoReset = false;
        }
        #endregion

        #region distractor
        void DataWindow_Closing(object sender, CancelEventArgs e)
        {
            logger.writeLine("closing program");
            logger.closeAndDispose();
            if (model != null)
            {
                try
                {
                    model.getSensor().ForceInfraredEmitterOff = true;
                    model = null;
                }
                catch { }
            }
            Thread.Sleep(1000);
        }
        #endregion

        void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (model != null)
                model.stopKinect();
        }

        #region Events
        private void port_DataReceived(object sender, PropertyChangedEventArgs e)
        {
            string msg = ((SafeSerialPort)sender).Message;
            Dispatcher.Invoke(() => lbl_status.Content = msg);

            if (msg.Equals("ON"))
            {
                Dispatcher.Invoke(() => {
                    turnOffKinectFlag = false;
                    NumberOfPeopleChanged(this, null);
                    turnOnKinect();
                });
            }
            else if (msg.Equals("OFF"))
                Dispatcher.Invoke(() => {
                    turnOffKinectFlag = true;
                    NumberOfPeopleChanged(this, null);
                });
        }

        private void NumberOfPeopleChanged(object sender, EventArgs e)
        {
            if (FrameHandler != null)
            {
                if (turnOffKinectFlag && FrameHandler.TrackedPeople == 0)
                {
                    timer.Start();
                    return;
                }
            }
            timer.Close();
        }
        #endregion

        #region KinectOffAndOn
        private void turnOnKinect()
        {
            btn_stream.Content = PAUSE;
            if (model != null)
            {
                model.startKinect();
                return;
            }

            readHeight();
            model = Model.getInstance(KINECT_HEIGHT, KINECT_ANGLE);
            if (model != null)
            {
                FrameHandler = new KinectFrameHandler(Screen, model, new AlertHandler(lbl_conection));
                FrameHandler.NumberOfPeopleChanged += NumberOfPeopleChanged;
                model.setFrameHandler(FrameHandler);
                model.enableDepthFrame();
                //model.enableSkeletonFrame();

                angleHandler = new KinectAngleHandler(model.getSensor(), port, horizontalAngle, verticalAngle);
                model.setAngleHandler(angleHandler);

                model.startKinect();
            }
        }
       
        private void turnOffKinect()
        {
            btn_stream.Content = STREAM;
            if (model!=null)
                model.stopKinect();
        }
        #endregion

        #region GUI_Events
        private void stream_click(object sender, RoutedEventArgs e)
        {
            if (btn_stream.Content.Equals(STREAM))
                turnOnKinect();
            else
                turnOnKinect();
        }
        
        private void angleChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (model != null)
            {
                model.changeVerticalAngleTo((int)e.NewValue);
            }
        }

        private void down_click(object sender, RoutedEventArgs e)
        {
            if (model != null)
            {
                model.changeVerticalAngleBy(-5);
            }
        }

        private void up_click(object sender, RoutedEventArgs e)
        {
            if (model != null)
            {
                model.changeVerticalAngleBy(5);
            }
        }

        private void tracking_checked(object sender, RoutedEventArgs e)
        {
            if (model!=null)
            {
                model.enableSkeletonFrame();
            }
        }

        private void tracking_unchecked(object sender, RoutedEventArgs e)
        {
            if (model != null)
            {
                model.disableSkeletonFrame();
            }
        }

        private void update_Setting(object sender, RoutedEventArgs e)
        {
            while (!updateHeight())
                MessageBox.Show("Invalid input", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        #endregion

        #region read/update HEIGHT
        public void readHeight()
        {
            KINECT_HEIGHT = Double.Parse(ConfigurationManager.AppSettings["KINECT_HEIGHT"]);
            KINECT_ANGLE = Int32.Parse(ConfigurationManager.AppSettings["KINECT_ANGLE"]);
               
            if (model != null)
                model.setKinectHeight(KINECT_HEIGHT);
        }

        public bool updateHeight()
        {
            string result = Microsoft.VisualBasic.Interaction.InputBox("Please enter kinect height in meters and angle degrees.\nFor example 1.75 0");
            double height;
            int angle;

            string[] str = result.Split(' ');
            if (str.Count() != 2)
                return false;
            try { 
                height = Convert.ToDouble(str[0]);
                angle = Convert.ToInt32(str[1]);
            }
            catch { return false; }

            KINECT_HEIGHT = height;
            KINECT_ANGLE = angle;

            if (model != null)
            {
                model.setKinectHeight(KINECT_HEIGHT);
                model.setKinectAngle(KINECT_ANGLE);
            }
            Configuration config = ConfigurationManager.OpenExeConfiguration(System.Reflection.Assembly.GetEntryAssembly().Location);
            config.AppSettings.Settings["KINECT_HEIGHT"].Value = KINECT_HEIGHT.ToString();
            config.AppSettings.Settings["KINECT_ANGLE"].Value = KINECT_ANGLE.ToString();
            config.Save(ConfigurationSaveMode.Minimal); 
            return true;
        }
        #endregion

    }
}