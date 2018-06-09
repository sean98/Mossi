using System;
using System.Net;
using System.Net.Sockets;

namespace MossiApi
{
    public class AlertHandler
    {
        Socket socket;
        IPAddress serverIP;
        IPEndPoint endPoint;

        public AlertHandler(string ip, int port)
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            serverIP = IPAddress.Parse("127.0.0.1");        //ip);
            Log.WriteLine("\n\n\n" + serverIP.ToString() + "\n\n\n");
            endPoint = new IPEndPoint(serverIP, port);
        }

        public void Alert(byte[] msg)
        {
            try
            {
                socket.SendTo(msg, endPoint);
            }
            catch (Exception ex)
            {
                Log.WriteLine(ex.Message);
            }
        }

        public void Dispose()
        {
            if (socket!=null)
                socket.Dispose();
        }
    }
}
