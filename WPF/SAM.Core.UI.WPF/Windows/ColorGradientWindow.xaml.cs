using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SAM.Core.UI.WPF
{
    /// <summary>
    /// Interaction logic for ColorGradientWindow.xaml
    /// </summary>
    public partial class ColorGradientWindow : Window
    {
        public ColorGradientWindow()
        {
            InitializeComponent();
        }

        public bool HasMid
        {
            get
            {
                return colorGradientControl_Main.HasMid;
            }

            set
            {
                colorGradientControl_Main.HasMid = value;
            }
        }

        public System.Drawing.Color High
        {
            get
            {
                return colorGradientControl_Main.High;
            }

            set
            {
                colorGradientControl_Main.High = value;
            }
        }

        public System.Drawing.Color Low
        {
            get
            {
                return colorGradientControl_Main.Low;
            }

            set
            {
                colorGradientControl_Main.Low = value;
            }
        }

        public System.Drawing.Color Mid
        {
            get
            {
                return colorGradientControl_Main.Mid;
            }

            set
            {
                colorGradientControl_Main.Mid = value;
            }
        }

        private void button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void button_OK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult= true;
        }
    }
}
