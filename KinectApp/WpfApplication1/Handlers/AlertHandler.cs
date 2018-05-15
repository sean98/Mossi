using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Controls;

namespace KinectApp
{
    class AlertHandler : KinectFrameHandler.IAlertHandler
    {
        public static string[] POSITION = { "standing", "sitting", "lying", "right hand", "left hand", "moves", "not moves" };
        Label lbl;

        Socket socket;
        IPAddress serverIP;
        IPEndPoint endPoint;

        public AlertHandler(Label lbl, string ip, int port)
        {
            this.lbl = lbl;
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            serverIP = IPAddress.Parse(ip);
            endPoint = new IPEndPoint(serverIP, port);
        }

        public void alert(string str)
        {
            try
            {
                socket.SendTo(Encoding.ASCII.GetBytes(str), endPoint);
            }
            catch (Exception e)
            {
                lbl.Content = e.Message;
            }
        }

        public void Alert(int roomNumber, int peopleCount, int peoplePosition)
        {
            byte[] msg = new byte[] { (byte)roomNumber, (byte)peopleCount, (byte)peoplePosition };
            try
            {
                socket.SendTo(msg, endPoint);
            }
            catch (Exception ex)
            {
                //TODO log ex
            }
        }

        public void alert(int numberOfPeople, ArrayList alerts)
        {
            String content = numberOfPeople + " " + (numberOfPeople == 1 ? "person" : "people") + " identified.";
            foreach (ArrayList alert in alerts)
            {
                content += "\n";
                for (int i = 0; i < alert.Count; i++)
                {
                    if (i == 0)
                        content += Math.Round((double)alert[i], 2) + " ";
                    else
                        content += alert[i] + " ";//POSITION[(int)alert[i]] + " ";
                }
            }
            //lbl.Content = content;
            byte[] buffer = Encoding.ASCII.GetBytes(content);
            try
            {
                socket.SendTo(buffer, endPoint);
                lbl.Content = content;
            }
            catch (Exception e)
            {
                lbl.Content = e.Message;
            }
        }
    }
}
