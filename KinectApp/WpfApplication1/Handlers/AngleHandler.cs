using System;
using Microsoft.Kinect;
using System.Threading;

namespace KinectApp
{
    class KinectAngleHandler : Model.IKinectAngleHandler
    {
        #region Variables
        private const String ANGLE = "angle";
        private const int MAX_HORIZONTAL_ANGLE = 25, MIN_HORIZONTAL_ANGLE = -25;

        private KinectSensor kinect;
        private SafeSerialPort port;

        private int horizontalAngle = 0, verticalAngle = 0;
        private float gearRelation = 2.55f;
        #endregion
        
        #region Proporties
        public int VerticalAngle
        {
            get { return verticalAngle; }
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
            set 
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
        public KinectAngleHandler(KinectSensor kinect, SafeSerialPort port,int verticalAngle,int horizontalAngle)
        {
            Logger.writeLine("Angle handler constractor");
            if (kinect == null)
                throw new System.ArgumentException("KinectSensor cannot be null", "kinect");
            this.kinect = kinect;

            setSerialPort(port);
            //Set vertical & horizobtal angles
            changeHorizontalAngleTo(horizontalAngle);
            changeVerticalAngleTo(verticalAngle);
        }
        #endregion

        #region SerialPort
        public void setSerialPort(SafeSerialPort port)
        {
            if (port == null)
                throw new System.InvalidOperationException("Serial port for controlling arduino " +
                    "cannot be null");
            this.port = port;
            this.port.DataReceived += port_DataReceived;
            Logger.writeLine("Serial port established");
        }

        private void port_DataReceived(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            int angle;
            if (int.TryParse(((SafeSerialPort)sender).Message, out angle))
            {
                HorizontalAngle = fromArduinoAngle(angle);
                Logger.writeLine("angle = " + angle);
            }
        }
        #endregion

        #region Vertical
        public void changeVerticalAngleBy(int angle)
        {
            Logger.writeLine("change vertical angle by " + angle);
            changeVerticalAngleTo(VerticalAngle + angle);
        }

        public void changeVerticalAngleTo(int angle)
        {
            Logger.writeLine("change vertical angle to " + angle);
            if (angle <= kinect.MaxElevationAngle && angle >= kinect.MinElevationAngle)
            {
                Thread moveAngle = new Thread(() => {
                    try
                    {
                        kinect.ElevationAngle = angle;
                        VerticalAngle = angle;
                    }
                    catch (Exception e)
                    {
                        //TODO log e
                    }
                });
                moveAngle.Start();
            }
            else
                throw new System.ArgumentException("angle must be between min and max elevation angle: " +
                    "(" + kinect.MinElevationAngle + ", " + kinect.MaxElevationAngle + ")");
        }
        #endregion

        #region Horizontical
        public void changeHorizontalAngleBy(int angle)
        {
            Logger.writeLine("change horizontal angle by " + angle);
            changeHorizontalAngleTo(HorizontalAngle + angle);
        }

        public void changeHorizontalAngleTo(int angle)
        {
            Logger.writeLine("change horizontal angle to " + angle);
            if (port == null)
                throw new System.InvalidOperationException("Serial port for controlling arduino " +
                    "motor is not defined");

            port.WriteLine(Convert.ToString(toArduinoAngle(angle)));
        }
        #endregion

        #region Scan
        public void scan()
        {
            Logger.writeLine("scan");
            changeVerticalAngleTo(0);
            port.WriteLine("scan");
        }
        #endregion

        #region Angle_convertor
        private int fromArduinoAngle(int angle)
        {
            Logger.writeLine("from arduino angle");
            int result = (int)(angle / gearRelation - 90 / gearRelation);
            Logger.writeLine(angle  + " => " + result);
            return result;
        }

        private int toArduinoAngle(int angle)
        {
            Logger.writeLine("to arduino angle");
            int result = (int)(angle * gearRelation + 90);
            Logger.writeLine(angle + " => " + result);
            return result;
        }
        #endregion
    }
}
