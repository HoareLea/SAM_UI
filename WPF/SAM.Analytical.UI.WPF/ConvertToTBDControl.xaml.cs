using SAM.Core;
using SAM.Weather;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for ConvertToTBDControl.xaml
    /// </summary>
    public partial class ConvertToTBDControl : UserControl
    {
        private WeatherData weatherData;
        private TextMap textMap;

        public ConvertToTBDControl()
        {
            InitializeComponent();
        }

        public string OutputDirectory
        {
            get
            {
                return textBox_OutputDirectory.Text;
            }

            set
            {
                textBox_OutputDirectory.Text = value;
            }
        }

        public string ProjectName
        {
            get
            {
                return textBox_ProjectName.Text;
            }

            set
            {
                textBox_ProjectName.Text = value;
            }
        }

        public bool UnmetHours
        {
            get
            {
                return checkBox_UnmetHours.IsChecked.Value;
            }

            set
            {
                checkBox_UnmetHours.IsChecked = value;
            }
        }

        public bool RoomDataSheets
        {
            get
            {
                return checkBox_RoomDataSheets.IsChecked.Value;
            }

            set
            {
                checkBox_RoomDataSheets.IsChecked = value;
            }
        }
    }
}
