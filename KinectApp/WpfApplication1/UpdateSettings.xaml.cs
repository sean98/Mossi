using System.Windows;
using System.Net;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace KinectApp
{
    public partial class UpdateSettings : Window, INotifyPropertyChanged
    {
        #region Property_Bindings
        public event PropertyChangedEventHandler PropertyChanged;
        
        public void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
       
        private string kinectHeight="", verticalAngle="", serverIP="", serverPort="";

        public string KinectHeight  
        {
            get { return kinectHeight; }
            set
            {
                if (!kinectHeight.Equals(value))
                {
                    kinectHeight = value;
                    if (PropertyChanged != null)
                        OnPropertyChanged();
//                        PropertyChanged(this, new PropertyChangedEventArgs("KinectHeight"));
                }
            }
        }

        public string VerticalAngle
        {
            get { return verticalAngle; }
            set
            {
                if (!verticalAngle.Equals(value))
                {
                    verticalAngle = value;
                    if (PropertyChanged!=null)
                        OnPropertyChanged();
//                    PropertyChanged(this, new PropertyChangedEventArgs("VerticalAngle"));
                }
            }
        }

        public string ServerIP
        {
            get { return serverIP; }
            set
            {
                if (!serverIP.Equals(value))
                {
                    serverIP = value;
                    if (PropertyChanged != null)
                        OnPropertyChanged();
//                    PropertyChanged(this, new PropertyChangedEventArgs("ServerIP"));
                }
            }
        }

        public string ServerPort
        {
            get { return serverPort; }
            set
            {
                if (!serverPort.Equals(value))
                {
                    serverPort = value;
                    if (PropertyChanged != null)
                        OnPropertyChanged();
//                    PropertyChanged(this, new PropertyChangedEventArgs("ServerPort"));
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

        private bool InputValidation()
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

        private void Save(object sender, RoutedEventArgs e)
        {
            if (!InputValidation())
                return;
            
            Properties.Settings.Default.KINECT_HEIGHT = float.Parse(KinectHeight);
            Properties.Settings.Default.VERTICAL_ANGLE = int.Parse(VerticalAngle);
            Properties.Settings.Default.SERVER_IP = ServerIP;
            Properties.Settings.Default.SERVER_PORT = int.Parse(ServerPort);
            Properties.Settings.Default.Save();
        
            System.Diagnostics.Process.Start(Application.ResourceAssembly.Location);
            Application.Current.Shutdown();
        }
    }
}
