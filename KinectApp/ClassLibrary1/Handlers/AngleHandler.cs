using System;
using Microsoft.Kinect;
using System.Threading;

namespace MossiApi
{
    public class KinectAngleHandler : Model.IKinectAngleHandler
    {
        #region Variables
        private static readonly string SCAN = "scan", ANGLE = "angle";
        public static  int DEAFULT_VERTICAL_ANGLE = 20, DEAFULT_HORIZONTAL_ANGLE = 0,
            MAX_HORIZONTAL_ANGLE = 30, MIN_HORIZONTAL_ANGLE = -30,
            MAX_VERTICAL_ANGLE = 27, MIN_VERTICAL_ANGLE = -27;

        private KinectSensor kinect;
        private SafeSerialPort port;

        private int horizontalAngle = 0, verticalAngle = 0;
        private float gearRelation = 2.55f;
        #endregion

        #region Proporties
        public int VerticalAngle
        {
            get { return DEAFULT_VERTICAL_ANGLE; }
            private set
            {
                if (value > kinect.MaxElevationAngle)
                    verticalAngle = kinect.MaxElevationAngle;
                else if (value < kinect.MinElevationAngle)
                    verticalAngle = kinect.MaxElevationAngle;
                else
                    verticalAngle = value;
            }
        }

        public int HorizontalAngle
        {
            get { return horizontalAngle; }
            private set
            {
                if (value > MAX_HORIZONTAL_ANGLE)
                    horizontalAngle = MAX_HORIZONTAL_ANGLE;
                else if (value < MIN_HORIZONTAL_ANGLE)
                    horizontalAngle = MIN_HORIZONTAL_ANGLE;
                else
                    horizontalAngle = value;
            }
        }
        #endregion

        #region Constructor
        public KinectAngleHandler(KinectSensor kinect, SafeSerialPort port, int deafultVerticalAngle)
        {
            if (kinect == null)
                throw new System.ArgumentException("KinectSensor cannot be null", "kinect");
            this.kinect = kinect;

            DEAFULT_VERTICAL_ANGLE = deafultVerticalAngle;

            SetSerialPort(port);
            //Set default vertical & horizobtal angles
            ChangeVerticalAngleTo(DEAFULT_VERTICAL_ANGLE);
            ChangeHorizontalAngleTo(DEAFULT_HORIZONTAL_ANGLE);
        }
        #endregion

        #region SerialPort
        public void SetSerialPort(SafeSerialPort port)
        {
            if (port == null)
                throw new System.InvalidOperationException("Serial port for controlling arduino " +
                    "cannot be null");
            this.port = port;
            this.port.DataReceived += Port_DataReceived;
        }

        private void Port_DataReceived(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (int.TryParse(((SafeSerialPort)sender).Message, out int angle))
            {
                HorizontalAngle = fromArduinoAngle(angle);
            }
        }
        #endregion

        #region Vertical
        public void ChangeVerticalAngleBy(int angle)
        {
            ChangeVerticalAngleTo(VerticalAngle + angle);
        }

        public void ChangeVerticalAngleTo(int angle)
        {
            if (angle <= MAX_VERTICAL_ANGLE && angle >= MIN_VERTICAL_ANGLE)
            {
                Thread moveAngle = new Thread(() => {
                    try
                    {
                        kinect.ElevationAngle = angle;
                        VerticalAngle = angle;
                    }
                    catch (Exception e)
                    {
                        Log.WriteLine(e.Message);
                    }
                });
                if (verticalAngle!=angle)
                    moveAngle.Start();
            }
            else
                throw new System.ArgumentException("angle must be between min and max elevation angle: " +
                    "(" + kinect.MinElevationAngle + ", " + kinect.MaxElevationAngle + ")");
        }
        #endregion

        #region Horizontical
        public void ChangeHorizontalAngleBy(int angle)
        {
            ChangeHorizontalAngleTo(HorizontalAngle + angle);
        }

        public void ChangeHorizontalAngleTo(int angle)
        {
            if (port == null)
                throw new System.InvalidOperationException("Serial port for controlling arduino " +
                    "motor is not defined");

            port.WriteLine(Convert.ToString(toArduinoAngle(angle)));
        }
        #endregion

        #region Scan
        public void Scan()
        {
            Log.WriteLine("vertical angle is: " + DEAFULT_VERTICAL_ANGLE.ToString());
            ChangeVerticalAngleTo(DEAFULT_VERTICAL_ANGLE);
            port.WriteLine(SCAN);
        }
        #endregion

        #region Angle_convertor
        private int fromArduinoAngle(int angle)
        {
            int result = (int)(angle / gearRelation - 90 / gearRelation);
            return result;
        }

        private int toArduinoAngle(int angle)
        {
            int result = (int)(angle * gearRelation + 90);
            return result;
        }
        #endregion
    }
}
