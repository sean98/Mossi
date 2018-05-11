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

        #region Constructor
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
        public void setFrameHandler(IKinectFrameHandler frameHandler)
        {
            this.frameHandler = frameHandler;
            kinect.DepthFrameReady += frameHandler.depthFrameReady;
            kinect.SkeletonFrameReady += frameHandler.skeletonFrameReady;
        }

        public void enableDepthFrame()
        {
            if (frameHandler != null)
                kinect.DepthStream.Enable();
            else
                throw new System.InvalidOperationException("Cannot using frame methods before setting a frame Handler. " +
                    "use: setFrameHandler(IKinectFrameHandler frameHandler)");

        }

        public void disableDepthFrame()
        {
            kinect.DepthStream.Disable();
        }

        public void startKinect()
        {
            kinect.Start();
        }

        public void stopKinect()
        {
            kinect.Stop();
        }

        //** skeleton options **//

        public void enableSkeletonFrame()
        {
            if (frameHandler != null)
                kinect.SkeletonStream.Enable();
            else
                throw new System.InvalidOperationException("Cannot using frame methods before setting a frame Handler. " +
                    "use: setFrameHandler(IKinectFrameHandler frameHandler)");

        }

        public void disableSkeletonFrame()
        {
            kinect.SkeletonStream.Disable();
        }
        #endregion

        #region Angle Handler
        //** Angle movement **//
        public void setAngleHandler(IKinectAngleHandler angleHandler)
        {
            this.angleHandler = angleHandler;
        }

        public void changeVerticalAngleBy(int angle)
        {
            if (angleHandler != null)
                angleHandler.changeVerticalAngleBy(angle);
            else
                throw new System.InvalidOperationException("Cannot using angle methods before setting an angle Handler. " +
                    "use: setAngleHandler(IKinectAngleMovement angleHandler)");
        }

        public void changeVerticalAngleTo(int angle)
        {
            if (angleHandler != null)
                angleHandler.changeVerticalAngleTo(angle);
            else
                throw new System.InvalidOperationException("Cannot using angle methods before setting an angle Handler. " +
                    "use: setAngleHandler(IKinectAngleMovement angleHandler)");
        }

        public int getVerticalAngle()
        {
            if (angleHandler != null)
                return angleHandler.VerticalAngle;

            throw new System.InvalidOperationException("Cannot using angle methods before setting an angle Handler. " +
                    "use: setAngleHandler(IKinectAngleMovement angleHandler)");
        }

        public void changeHorizontalAngleBy(int angle)
        {
            if (angleHandler != null)
                angleHandler.changeHorizontalAngleBy(angle);
            else
                throw new System.InvalidOperationException("Cannot using angle methods before setting an angle Handler. " +
                    "use: setAngleHandler(IKinectAngleMovement angleHandler)");
        }

        public void changeHorizontalAngleTo(int angle)
        {
            if (angleHandler != null)
                angleHandler.changeHorizontalAngleTo(angle);
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
                angleHandler.scan();
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
            void skeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e);

            void depthFrameReady(object sender, DepthImageFrameReadyEventArgs e);
        }

        public interface IKinectAngleHandler
        {
            void changeVerticalAngleBy(int angle);

            void changeVerticalAngleTo(int angle);

            int VerticalAngle
            {
                get;
            }

            void changeHorizontalAngleBy(int angle);

            void changeHorizontalAngleTo(int angle);

            int HorizontalAngle
            {
                get;
            }

            void scan();
        }
    }
}
