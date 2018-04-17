using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApplication1
{
    class Controller
    {
        private MainWindow view;
        private Model model;

        public Controller(MainWindow view)
        {
            this.view = view;
        }
    }
}
