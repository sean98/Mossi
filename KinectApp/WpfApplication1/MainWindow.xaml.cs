﻿using System;
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

namespace WpfApplication1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //consts
        private const string CONNECTION = "conection: ", STATUS = "status: ";
        private const string STREAM = "Stream", PAUSE = "Pause";
        private const short BACKROUND_ACCURACY = 100;
        private const long TIME_IN_FRAMES = 1;
        //variables
        private bool backroundCheck = false, oneColor = false;
        private int skeletonFrameCounter;
        private KinectSensor kSensor;
        //arrays
        private short [] backround;
        //model
        private Model model;
        private double KINECT_HEIGHT = 1.75;
        private int KINECT_ANGLE = 0;
        private static string SETTING_FILE_PATH = "setting";
        SerialPort port;

        public MainWindow()
        {
            InitializeComponent();
            Screen.Height = 700;
            Screen.Width = 900;
            port = new SerialPort("COM4", 9600, Parity.None, 8, StopBits.One);
            port.DataReceived += serialPortDataReceived;
            port.Open();
        }

        private void serialPortDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            Dispatcher.Invoke(() => lbl_status.Content = ((SerialPort)sender).ReadExisting());
        }

        void DataWindow_Closing(object sender, CancelEventArgs e)
        {
            if (model != null)
            {
                try
                {
                    model.getSensor().ForceInfraredEmitterOff = true;
                    model = null;
                }
                catch { }
            }
        }

        private void stream_click(object sender, RoutedEventArgs e)
        {
            if(btn_stream.Content.Equals(STREAM))
            {
                readHeight();
                model = Model.getInstance(KINECT_HEIGHT,KINECT_ANGLE);
                if (model != null)
                {

                    btn_stream.Content = PAUSE;
                    model.setFrameHandler(new KinectFrameHandler(Screen, model,new AlertHandler(lbl_conection)));
                    model.enableDepthFrame();
                    model.startKinect();

                    model.setAngleHandler(new KinectAngleHandler(model.getSensor(), 0, -10));
                    ((KinectAngleHandler)model.getAngleHandler()).setSerialPort(port);
                }
            }
            else
            {
                btn_stream.Content = STREAM;
                model = null;
            }
        }

  

        public void clearBackround()
        {
            if (backround != null)
            {
                for (int i = 0; i < backround.Length; i++)
                    backround[i] = 0;
            }
        }

        private void sensitivityChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (model != null)
            {
                model.changeVerticalAngleTo((int)e.NewValue);
            }
        }

        private void backround_check(object sender, RoutedEventArgs e)
        {
            backroundCheck = true;
            clearBackround();
            if (model!=null)
                model.disableSkeletonFrame();
        }

        private void backround_unCheck(object sender, RoutedEventArgs e)
        {
            backroundCheck = false;
        }

        private void color_unchecked(object sender, RoutedEventArgs e)
        {
            oneColor = false;
        }

        private void color_checked(object sender, RoutedEventArgs e)
        {
            oneColor = true;
        }

        private void down_click(object sender, RoutedEventArgs e)
        {
            if (kSensor != null)
            {
                model.changeVerticalAngleBy(-5);
            }
        }

        private void up_click(object sender, RoutedEventArgs e)
        {
            if (kSensor != null)
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
            if (kSensor != null)
            {
                model.disableSkeletonFrame();
            }
        }

        private void update_Setting(object sender, RoutedEventArgs e)
        {
            while (!updateHeight())
                MessageBox.Show("Invalid input", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

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