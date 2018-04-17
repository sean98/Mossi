using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;

namespace WpfApplication1
{
    class Vector
    {
        private double x, y, z;

        public double X
        {
            get { return x; }
            set { x = value; }
        }

        public double Y
        {
            get { return y; }
            set { y = value; }
        }

        public double Z
        {
            get { return z; }
            set { z = value; }
        }

        public Vector(Joint j)
        {
            x = j.Position.X;
            y = j.Position.Y;
            z = j.Position.Z;
        }

        public Vector(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public double getLength()
        {
            return Math.Sqrt(x*x + y*y + z*z);
        }

        public double angleBetweenVector(Vector v)
        {
            double scalar = v.x * x + v.y * y + v.z * z;
            return scalar / (v.getLength() * this.getLength());
        }

        public double angleBetweenXZ()
        {
            Vector projection = new Vector(x, 0, z);
            double scalarMul = projection.x * x + projection.y * y + projection.z * z;
            return Math.Acos(scalarMul / (this.getLength() * projection.getLength()));
        }
    }
}
