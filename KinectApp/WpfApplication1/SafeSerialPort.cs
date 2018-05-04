using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Threading;
using System.ComponentModel;

namespace KinectApp
{
    class SafeSerialPort
    {
        private SerialPort port;
        private bool validPort;
        private Thread initThread;

        public string message;
        public event PropertyChangedEventHandler DataReceived;

        public String Message
        {
            get { return message; }
        }

        public SafeSerialPort()
        {
            init();
        }

        public void WriteLine(String msg)
        {
            try
            {
                port.WriteLine(msg);
            }
            catch (Exception e)
            {
                //TODO log e
                if (port!=null && initThread==null)
                {
                    if (port.IsOpen)
                        port.Close();
                    port.Dispose();
                }
                init();
            }
        }

        public void init()
        {
            if (initThread != null && initThread.IsAlive)
                return;

            initThread = new Thread(() => {
                validPort = false;
                while (!validPort)
                {
                    string[] portsNames = SerialPort.GetPortNames();
                    Thread.Sleep(500);

                    foreach (string name in portsNames)
                    {
                        port = new SerialPort(name, 9600, Parity.None, 8, StopBits.One);
                        try
                        {
                            if (!port.IsOpen)
                                port.Open();
                            port.WriteLine("init");
                            port.DataReceived += serialPortDataReceived;
                            Thread.Sleep(500);
                        }
                        catch (Exception e)
                        {
                            //TODO log e
                            if (port.IsOpen)
                                port.Close();
                            port.Dispose();
                            port = null;
                        }
                        if (validPort)
                            break;
                    }
                }
            });
            initThread.Start();
        }

        private void serialPortDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            message = ((SerialPort)sender).ReadLine();

            if (message.Equals("init"))
                validPort = true;
            else
                OnDataReceived("Message");
        }

        protected void OnDataReceived(string name)
        {
            PropertyChangedEventHandler handler = DataReceived;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}
