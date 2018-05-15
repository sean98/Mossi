using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows;

namespace KinectApp
{
    class Model
    {
        #region variables
        private KinectSensor kinect;

        private double KINECT_HEIGHT;
        private int KINECT_ANGLE;

        private IKinectFrameHandler frameHandler;
        private IKinectAngleHandler angleHandler;
        #endregion

        #region SingeltoneAndConstructor
        private Model(KinectSensor kinect, double KINECT_HEIGHT, int KINECT_ANGLE)
        {
            this.kinect = kinect;
            this.KINECT_HEIGHT = KINECT_HEIGHT;
            this.KINECT_ANGLE = KINECT_ANGLE;
        }

        public static Model getInstance(double KINECT_HEIGHT, int KINECT_ANGLE)
        {
            if (KinectSensor.KinectSensors.Count > 0)
            {
                if (KinectSensor.KinectSensors[0] != null)
                    return new Model(KinectSensor.KinectSensors[0], KINECT_HEIGHT, KINECT_ANGLE);
            }
            return null;
        }
        #endregion

        #region Frame Handler
        public void SetFrameHandler(IKinectFrameHandler frameHandler)
        {
            this.frameHandler = frameHandler;
            kinect.DepthFrameReady += frameHandler.DepthFrameReady;
            kinect.SkeletonFrameReady += frameHandler.SkeletonFrameReady;
        }

        public void EnableDepthFrame()
        {
            if (frameHandler != null)
                kinect.DepthStream.Enable(DepthImageFormat.Resolution80x60Fps30);
            else
                throw new System.InvalidOperationException("Cannot using frame methods before setting a frame Handler. " +
                    "use: setFrameHandler(IKinectFrameHandler frameHandler)");

        }

        public void DisableDepthFrame()
        {
            kinect.DepthStream.Disable();
        }

        public void EnableSkeletonFrame()
        {
            if (frameHandler != null)
                kinect.SkeletonStream.Enable();
            else
                throw new System.InvalidOperationException("Cannot using frame methods before setting a frame Handler. " +
                    "use: setFrameHandler(IKinectFrameHandler frameHandler)");

        }

        public void DisableSkeletonFrame()
        {
            kinect.SkeletonStream.Disable();
        }

        public void StartKinect()
        {
            kinect.Start();
        }

        public void StopKinect()
        {
            kinect.Stop();
        }        
        #endregion

        #region Angle Handler
        public void setAngleHandler(IKinectAngleHandler angleHandler)
        {
            this.angleHandler = angleHandler;
        }

        public void changeVerticalAngleBy(int angle)
        {
            if (angleHandler != null)
                angleHandler.ChangeVerticalAngleBy(angle);
            else
                throw new System.InvalidOperationException("Cannot using angle methods before setting an angle Handler. " +
                    "use: setAngleHandler(IKinectAngleMovement angleHandler)");
        }

        public void changeVerticalAngleTo(int angle)
        {
            if (angleHandler != null)
                angleHandler.ChangeVerticalAngleTo(angle - KINECT_ANGLE);
            else
                throw new System.InvalidOperationException("Cannot using angle methods before setting an angle Handler. " +
                    "use: setAngleHandler(IKinectAngleMovement angleHandler)");
        }

        public int getVerticalAngle()
        {
            if (angleHandler != null)
                return angleHandler.VerticalAngle + KINECT_ANGLE;
            throw new System.InvalidOperationException("Cannot using angle methods before setting an angle Handler. " +
                    "use: setAngleHandler(IKinectAngleMovement angleHandler)");
        }

        public void changeHorizontalAngleBy(int angle)
        {
            if (angleHandler != null)
                angleHandler.ChangeHorizontalAngleBy(angle);
            else
                throw new System.InvalidOperationException("Cannot using angle methods before setting an angle Handler. " +
                    "use: setAngleHandler(IKinectAngleMovement angleHandler)");
        }

        public void changeHorizontalAngleTo(int angle)
        {
            if (angleHandler != null)
                angleHandler.ChangeHorizontalAngleTo(angle);
            else
                throw new System.InvalidOperationException("Cannot using angle methods before setting an angle Handler. " +
                    "use: setAngleHandler(IKinectAngleMovement angleHandler)");
        }

        public int getHorizontalAngle()
        {
            if (angleHandler != null)
                return angleHandler.HorizontalAngle;
            else
                throw new System.InvalidOperationException("Cannot using angle methods before setting an angle Handler. " +
                    "use: setAngleHandler(IKinectAngleMovement angleHandler)");
        }

        public void scan()
        {
            if (angleHandler != null)
                angleHandler.Scan();
            else
                throw new System.InvalidOperationException("Cannot using angle methods before setting an angle Handler. " +
                    "use: setAngleHandler(IKinectAngleMovement angleHandler)");
        }
        #endregion

        #region Getters
        public IKinectAngleHandler getAngleHandler()
        {
            return angleHandler;
        }

        public IKinectFrameHandler getFrameHandler()
        {
            return frameHandler;
        }

        public KinectSensor getSensor()
        {
            return kinect;
        }

        public double getKinectHeight()
        {
            return KINECT_HEIGHT;
        }

        public int getKinectAngle()
        {
            return KINECT_ANGLE;
        }
        #endregion

        #region Setter
        public void setKinectHeight(double KINECT_HEIGHT)
        {
            this.KINECT_HEIGHT = KINECT_HEIGHT;
        }

        public void setKinectAngle(int KINECT_ANGLE)
        {
            this.KINECT_ANGLE = KINECT_ANGLE;
        }
        #endregion

        public interface IKinectFrameHandler
        {
            void SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e);

            void DepthFrameReady(object sender, DepthImageFrameReadyEventArgs e);
        }

        public interface IKinectAngleHandler
        {
            void ChangeVerticalAngleBy(int angle);

            void ChangeVerticalAngleTo(int angle);

            int VerticalAngle { get; }

            void ChangeHorizontalAngleBy(int angle);

            void ChangeHorizontalAngleTo(int angle);

            int HorizontalAngle { get; }

            void Scan();
        }
    }
}
