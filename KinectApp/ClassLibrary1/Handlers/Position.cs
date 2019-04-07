using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace MossiApi.Handlers
{
    class Position
    {
        private Timer timer;

        public event PropertyChangedEventHandler PositionPropertyChanged;
        private void OnPositionPropertyChanged()
        {
            PropertyChangedEventHandler handler = PositionPropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(Name));
            }
        }

        private bool active;
        public bool Active
        {
            get { return active; }
            set
            {
                if (value != active)
                {
                    active = value;
                    OnPositionPropertyChanged();
                }
            }
        }

        public string Name { get; private set; }

        public Position(string name, int interval)
        {
            Name = name;
            timer = new Timer(interval);
            timer.AutoReset = false;
            timer.Elapsed += Timer_Elapsed;
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Active = true;
        }

        public void Start()
        {
            timer.Start();
        }

        public void Stop()
        {
            timer.Stop();
            Active = false;
        }
    }
}
