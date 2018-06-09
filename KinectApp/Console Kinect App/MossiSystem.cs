using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using MossiApi;

namespace KinectApp.ConsoleApp
{
    public class MossiSystem
    {

        static void Main(string[] args)
        {
            new MossiSystem();
        }

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
        private double KINECT_HEIGHT;
        private int VERTICAL_ANGLE;
        private string SERVER_IP;
        private int SERVER_PORT;

        private Timer timer;
        private int timeForTurnOff = 10;

        private bool pir_detection = true;//true only for testing!!! should be false
        private bool doorClosed = false;

        private bool IsSituation(byte state, Situations s)
        {
            return (state & (1 << (byte)s)) == 1 ? true : false;
        }

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

        public bool DoorClosed
        {
            get { return doorClosed; }
            set
            {
                if (doorClosed != value)
                {
                    doorClosed = value;
                    // DetectionPropertyChanged(this, new PropertyChangedEventArgs("DoorClosed"));
                }
            }
        }
        #endregion

        #region constractor
        public MossiSystem()
        {
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.RealTime;

            KINECT_HEIGHT = Properties.Settings.Default.KINECT_HEIGHT;
            VERTICAL_ANGLE = Properties.Settings.Default.VERTICAL_ANGLE;
            SERVER_IP = Properties.Settings.Default.SERVER_IP;
            SERVER_PORT = Properties.Settings.Default.SERVER_PORT;

            port = new SafeSerialPort();
            port.DataReceived += Port_DataReceived;

            timer = new Timer(timeForTurnOff * 1000);
            timer.Elapsed += Timer_Elapsed;
            timer.AutoReset = false;
            TurnOnKinect();
        }
        #endregion

        #region Timer
        void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (model != null)
                model.StopKinect();
        }
        #endregion

        #region Events
        private void Port_DataReceived(object sender, PropertyChangedEventArgs e)
        {
            string msg = ((SafeSerialPort)sender).Message;
            Console.WriteLine(msg);

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
            try
            {
                string alert = "";
                if (PIR_Detection)
                {
                    TurnOnKinect();
                    timer.Close();
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
                    if (!frameHandler.FrameRealiable)
                    {
                        Console.WriteLine("bad data");
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
                    /*if (frameHandler.LegsInTheAir)
                    {
                        Position |= 1 << (byte)Situations.LegsInTheAir;
                        alert += " Legs In The Air";
                    }*/

                    if (frameHandler.TrackedPeople == 0 && !PIR_Detection)
                        timer.Start();
                    else
                        timer.Stop();
                }
                Console.WriteLine(alert);
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
            Console.WriteLine("Head Height: " + Math.Round(((KinectFrameHandler)sender).HeadHeight, 2)
                               + " Legs Height: " + Math.Round(((KinectFrameHandler)sender).LegHeight, 2));
        }
        #endregion

        #region Kinect_Off_And_On
        private void TurnOnKinect()
        {
            if (model != null)
            {
                model.StartKinect();
                return;
            }

            model = Model.getInstance(KINECT_HEIGHT, VERTICAL_ANGLE);
            if (model != null)
            {
                alerHandler = new AlertHandler(SERVER_IP, SERVER_PORT);
                frameHandler = new KinectFrameHandler(model);
                angleHandler = new KinectAngleHandler(model.getSensor(), port);
                frameHandler.SituationPropertyChanged += DetectionPropertyChanged;
                frameHandler.GenaratePixelData = true;
                //frameHandler.HeightPropertyChanged += HeightPropertyChanged;

                model.SetFrameHandler(frameHandler);
                model.setAngleHandler(angleHandler);

                model.EnableDepthFrame();
                model.EnableSkeletonFrame();

                model.StartKinect();
            }
        }

        private void TurnOffKinect()
        {
            if (model != null)
                model.StopKinect();
        }
        #endregion

    }
}
