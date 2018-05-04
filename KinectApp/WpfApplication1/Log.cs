using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace KinectApp
{
    class Logger
    {
        private StreamWriter writer;
        private Object locker = new Object();

        public Logger()
        {
            newLogFile();
        }

        public void newLogFile(String path) 
        {
            closeAndDispose();
            lock (locker)
            {
                writer = new StreamWriter(path);
            }
        }

        public void newLogFile()
        {
            string dir = Directory.GetCurrentDirectory() + "\\logs";
            Directory.CreateDirectory(dir);
            newLogFile(Path.Combine(dir, DateTime.Now.Year + "." + DateTime.Now.Month + "." + DateTime.Now.Day
                + " " + DateTime.Now.Hour + "-" + DateTime.Now.Minute + "-" + DateTime.Now.Second + ".txt"));
        }

        public void write(string log)
        {
            lock (locker)
            {
                writer.Write(log);
            }
        }

        public void writeLine(string log)
        {
            write(log + "\n");
        }

        public void closeAndDispose()
        {
            lock (locker)
            {
                if (writer != null)
                {
                    writer.Close();
                    writer.Dispose();
                }
            }
        }
    }
}
