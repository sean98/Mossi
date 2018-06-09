using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Threading;
using System.ComponentModel;

namespace MossiApi
{
    public class SafeSerialPort
    {
        private SerialPort port;
        private bool validPort;
        private Thread initThread;

        private string message;
        public event PropertyChangedEventHandler DataReceived;

        public String Message
        {
            get { return message; }
            set
            {
                message = value;
                OnDataReceived("Message");
            }
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
                Log.WriteLine(e.Message);
                init();
            }
        }

        public void init()
        {
            if (initThread != null && initThread.IsAlive)
                return;
            validPort = false;
            initThread = new Thread(() =>
            {
                while (!validPort)
                {
                    string[] portsNames = SerialPort.GetPortNames();
                    Thread.Sleep(2000);

                    foreach (string name in portsNames)
                    {
                        port = new SerialPort(name, 9600, Parity.None, 8, StopBits.One);
                        try
                        {
                            port.Open();
                            port.WriteLine("init");
                            port.DataReceived += serialPortDataReceived;
                            Thread.Sleep(1000);
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
            try
            {
                message = ((SerialPort)sender).ReadLine();
                if (message.Equals("init"))
                    validPort = true;
                Message = message;
            }
            catch (Exception ex)
            {
                Log.WriteLine(ex.Message);
            }
        }

        protected void OnDataReceived(string name)
        {
            PropertyChangedEventHandler handler = DataReceived;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        public void closeAndDispose()
        {
            if (initThread!=null)
            {
                validPort = true;
                initThread.Join();
            }
            if (port!=null)
            {
                if (port.IsOpen)
                    port.Close();
                port.Dispose();
            }
        }
    }
}
