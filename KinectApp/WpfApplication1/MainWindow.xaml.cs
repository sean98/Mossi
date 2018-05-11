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
        private readonly string STREAM = "Stream", PAUSE = "Pause";
        
        //model & handlers
        private Model model=null;
        private SafeSerialPort port;
        private AlertHandler alerHandler;
        private KinectAngleHandler angleHandler;
        private KinectFrameHandler frameHandler;
        
        //variables from settings
        private double KINECT_HEIGHT;
        private int VERTICAL_ANGLE;
        private string IP;
        private int PORT;
        
        private System.Timers.Timer timer;
        private int timeForTurnOff = 10;

        private bool pir_detection = false;
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
            IP = Properties.Settings.Default.SERVER_IP;
            PORT = Properties.Settings.Default.SERVER_PORT;

            port = new SafeSerialPort();
            port.DataReceived += port_DataReceived;

            
            Logger.writeLine("Initialize Program");
            
            timer = new System.Timers.Timer(timeForTurnOff * 1000);
            timer.Elapsed += timer_Elapsed;
            timer.AutoReset = false;
        }
        #endregion

        #region distractor
        void DataWindow_Closing(object sender, CancelEventArgs e)
        {
            Logger.writeLine("closing program");
            Logger.closeAndDispose();
            if (model != null)
            {
                try
                {
                    model.getSensor().ForceInfraredEmitterOff = true;
                    model = null;
                }
                catch
                {
                }
            }
        }
        #endregion

        #region Timer
        void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Logger.writeLine("timer_elapsed");
            if (model != null)
                model.stopKinect();
        }
        #endregion

        #region Events
        private void port_DataReceived(object sender, PropertyChangedEventArgs e)
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
                turnOnKinect();
            else if (frameHandler != null)
            {
                if (frameHandler.TrackedPeople == 0)
                    timer.Start();
                else
                    timer.Close();
            }
            else ; //TODO decide what to do
        }
        #endregion
      
        #region Kinect_Off_And_On
        private void turnOnKinect()
        {
            Logger.writeLine("turn on kinect");
            Dispatcher.Invoke(() => btn_stream.Content = PAUSE);
            if (model != null)
            {
                model.startKinect();
                return;
            }

            model = Model.getInstance(KINECT_HEIGHT, VERTICAL_ANGLE);
            if (model != null)
            {
                frameHandler = new KinectFrameHandler(Screen, model, new AlertHandler(lbl_conection));
                frameHandler.PropertyChanged += DetectionPropertyChanged;
                model.setFrameHandler(frameHandler);
                model.enableDepthFrame();
                //model.enableSkeletonFrame();

                angleHandler = new KinectAngleHandler(model.getSensor(), port, -20, 0);
                model.setAngleHandler(angleHandler);

                model.startKinect();
            }
        }
       
        private void turnOffKinect()
        {
            Logger.writeLine("turn off kinect");
            Dispatcher.Invoke(() => btn_stream.Content = STREAM);
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
                turnOffKinect();
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
            //TODO open update settings windows
            UpdateSettings us = new UpdateSettings();
            us.Show();
            us.Activate();
        }
        #endregion
    }
}