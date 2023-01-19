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
using System.Windows.Shapes;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for ConvertToTBDWindow.xaml
    /// </summary>
    public partial class ConvertToTBDWindow : System.Windows.Window
    {
        public ConvertToTBDWindow()
        {
            InitializeComponent();
        }

        public WeatherData WeatherData
        {
            get
            {
                return convertToTBDControl.WeatherData;
            }
            set
            {
                convertToTBDControl.WeatherData = value;
            }
        }

        public WeatherData SelectWeatherData
        {
            get
            {
                return convertToTBDControl.SelectedWeatherData;
            }
        }

        public TextMap TextMap
        {
            get
            {
                return convertToTBDControl.TextMap;
            }
            set
            {
                convertToTBDControl.TextMap = value;
            }
        }

        public TextMap SelectedTextMap
        {
            get
            {
                return convertToTBDControl.SelectedTextMap;
            }
        }

        public string OutputDirectory
        {
            get
            {
                return convertToTBDControl.OutputDirectory;
            }

            set
            {
                convertToTBDControl.OutputDirectory = value;
            }
        }

        public string ProjectName
        {
            get
            {
                return convertToTBDControl.ProjectName;
            }

            set
            {
                convertToTBDControl.ProjectName = value;
            }
        }

        public bool Simulate
        {
            get
            {
                return convertToTBDControl.Simulate;
            }

            set
            {
                convertToTBDControl.Simulate = value;
            }
        }

        public bool FullYearSimulation
        {
            get
            {
                return convertToTBDControl.FullYearSimulation;
            }

            set
            {
                convertToTBDControl.FullYearSimulation = value;
            }
        }

        public int FullYearSimulation_From
        {
            get
            {
                return convertToTBDControl.FullYearSimulation_From;
            }
        }

        public int FullYearSimulation_To
        {
            get
            {
                return convertToTBDControl.FullYearSimulation_To;
            }
        }

        public string SelectedZoneCategory
        {
            get
            {
                return convertToTBDControl.SelectedZoneCategory;
            }
        }

        public List<string> ZoneCategories
        {
            get
            {
                return convertToTBDControl.ZoneCategories;
            }
            set
            {
                convertToTBDControl.ZoneCategories = value;
            }
        }

        public bool UnmetHours
        {
            get
            {
                return convertToTBDControl.UnmetHours;
            }

            set
            {
                convertToTBDControl.UnmetHours = value;
            }
        }

        public bool RoomDataSheets
        {
            get
            {
                return convertToTBDControl.RoomDataSheets;
            }

            set
            {
                convertToTBDControl.RoomDataSheets = value;
            }
        }

        public bool CreateSAP
        {
            get
            {
                return convertToTBDControl.CreateSAP;
            }

            set
            {
                convertToTBDControl.CreateSAP = value;
            }
        }

        public bool CreateTM59
        {
            get
            {
                return convertToTBDControl.CreateTM59;
            }

            set
            {
                convertToTBDControl.CreateTM59 = value;
            }
        }

        public SolarCalculationMethod SolarCalculationMethod
        {
            get
            {
                return convertToTBDControl.SolarCalculationMethod;
            }

            set
            {
                convertToTBDControl.SolarCalculationMethod = value;
            }
        }

        private void button_OK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
