using System;
using System.Windows;
using System.Net;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace KinectApp
{
    public partial class UpdateSettings : Window, INotifyPropertyChanged
    {
        #region Property_Bindings
        public event PropertyChangedEventHandler PropertyChanged = (sender, e) => { };
        private string _kinectHeight="", _verticalAngle="", _serverIP="", _serverPort="";
        public string KinectHeight  
        {
            get { return _kinectHeight; }
            set
            {
                if (!_kinectHeight.Equals(value))
                {
                    _kinectHeight = value;
                    if (PropertyChanged!=null)
                        PropertyChanged(this, new PropertyChangedEventArgs("KinectHeight"));
                }
            }
        }
        public string VerticalAngle
        {
            get { return _verticalAngle; }
            set
            {
                if (!_verticalAngle.Equals(value))
                {
                    _verticalAngle = value;
                    if (PropertyChanged!=null)
                        PropertyChanged(this, new PropertyChangedEventArgs("VerticalAngle"));
                }
            }
        }
        public string ServerIP
        {
            get { return _serverIP; }
            set
            {
                if (!_serverIP.Equals(value))
                {
                    _serverIP = value;
                    if (PropertyChanged != null)
                        PropertyChanged(this, new PropertyChangedEventArgs("ServerIP"));
                }
            }
        }
        public string ServerPort
        {
            get { return _serverPort; }
            set
            {
                if (!_serverPort.Equals(value))
                {
                    _serverPort = value;
                    if (PropertyChanged != null)
                        PropertyChanged(this, new PropertyChangedEventArgs("ServerPort"));
                }
            }
        }
        #endregion

        public UpdateSettings()
        {
            InitializeComponent();
            DataContext = this;
           
            //set text to be the current settings.
            KinectHeight = Properties.Settings.Default.KINECT_HEIGHT.ToString();
            VerticalAngle = Properties.Settings.Default.VERTICAL_ANGLE.ToString();
            ServerIP = Properties.Settings.Default.SERVER_IP.ToString();
            ServerPort = Properties.Settings.Default.SERVER_PORT.ToString();
        }

        private bool inputValidation()
        {
            //temporarily variables to check validation
            float kinectHeight;
            int verticalAngle, serverPort;
            IPAddress IP;

            if (!float.TryParse(KinectHeight, out kinectHeight) || kinectHeight < 0)
                MessageBox.Show("Kinect Height is not valid.\nshould be an unsigned rational nummber.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            else if (!int.TryParse(VerticalAngle, out verticalAngle))
                MessageBox.Show("Vertical is not valid.\nshould be an integer.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            else if (!IPAddress.TryParse(ServerIP, out IP))
                MessageBox.Show("Server IP is not a valid IP address.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            else if (!int.TryParse(ServerPort, out serverPort) || serverPort < 0)
                MessageBox.Show("Server Port is not valid.\nshould be an unsigned integer.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            else
                return true;
            return false;
        }

        private void save(object sender, RoutedEventArgs e)
        {
            if (!inputValidation())
                return;
            
            Properties.Settings.Default.KINECT_HEIGHT = float.Parse(KinectHeight);
            Properties.Settings.Default.VERTICAL_ANGLE = int.Parse(VerticalAngle);
            Properties.Settings.Default.SERVER_IP = ServerIP;
            Properties.Settings.Default.SERVER_PORT = int.Parse(ServerPort);
            Properties.Settings.Default.Save();
            
            this.Close();
        }
    }
}
