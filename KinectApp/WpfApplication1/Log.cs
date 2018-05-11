using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net.Sockets;
using System.Net;

namespace KinectApp
{
    static class Logger
    {
        static private StreamWriter writer;
        static private Object locker = new Object();
        /*
        static Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        static IPAddress serverIP = IPAddress.Parse("192.168.1.101");
        static IPEndPoint endPoint = new IPEndPoint(serverIP, 11000);
        */
        public static void newLogFile(String path) 
        {
            closeAndDispose();
            lock (locker)
            {
                writer = new StreamWriter(path);
            }
        }

        public static void newLogFile()
        {
            string dir = Directory.GetCurrentDirectory() + "\\logs";
            Directory.CreateDirectory(dir);
            newLogFile(Path.Combine(dir, DateTime.Now.Year + "." + DateTime.Now.Month + "." + DateTime.Now.Day
                + " " + DateTime.Now.Hour + "-" + DateTime.Now.Minute + "-" + DateTime.Now.Second + ".txt"));
        }

        public static void write(string log)
        {
            lock (locker)
            {
                if (writer == null)
                    newLogFile();
                writer.Write(log);
            }
        }

        public static void writeLine(string log)
        {
            write(log + "\n");
        }

        public static void closeAndDispose()
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
