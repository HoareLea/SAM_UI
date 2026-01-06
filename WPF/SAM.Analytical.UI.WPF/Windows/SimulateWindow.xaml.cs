// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using SAM.Weather;
using System.Collections.Generic;
using System.Windows;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for ConvertToTBDWindow.xaml
    /// </summary>
    public partial class SimulateWindow : System.Windows.Window
    {
        public SimulateWindow()
        {
            InitializeComponent();
        }

        public bool Sizing
        {
            get
            {
                return simulateControl.Sizing;
            }
            set
            {
                simulateControl.Sizing = value;
            }
        }

        public WeatherData WeatherData
        {
            get
            {
                return simulateControl.WeatherData;
            }
            set
            {
                simulateControl.WeatherData = value;
            }
        }

        public WeatherData SelectedWeatherData
        {
            get
            {
                return simulateControl.SelectedWeatherData;
            }
        }

        public TextMap TextMap
        {
            get
            {
                return simulateControl.TextMap;
            }
            set
            {
                simulateControl.TextMap = value;
            }
        }

        public TextMap SelectedTextMap
        {
            get
            {
                return simulateControl.SelectedTextMap;
            }
        }

        public string OutputDirectory
        {
            get
            {
                return simulateControl.OutputDirectory;
            }

            set
            {
                simulateControl.OutputDirectory = value;
            }
        }

        public string ProjectName
        {
            get
            {
                return simulateControl.ProjectName;
            }

            set
            {
                simulateControl.ProjectName = value;
            }
        }

        public bool Simulate
        {
            get
            {
                return simulateControl.Simulate;
            }

            set
            {
                simulateControl.Simulate = value;
            }
        }

        public bool FullYearSimulation
        {
            get
            {
                return simulateControl.FullYearSimulation;
            }

            set
            {
                simulateControl.FullYearSimulation = value;
            }
        }

        public int FullYearSimulation_From
        {
            get
            {
                return simulateControl.FullYearSimulation_From;
            }
        }

        public int FullYearSimulation_To
        {
            get
            {
                return simulateControl.FullYearSimulation_To;
            }
        }

        public string SelectedZoneCategory
        {
            get
            {
                return simulateControl.SelectedZoneCategory;
            }
        }

        public List<string> ZoneCategories
        {
            get
            {
                return simulateControl.ZoneCategories;
            }
            set
            {
                simulateControl.ZoneCategories = value;
            }
        }

        public bool UseWidths
        {
            get
            {
                return simulateControl.UseWidths;
            }

            set
            {
                simulateControl.UseWidths = value;
            }
        }

        public bool UnmetHours
        {
            get
            {
                return simulateControl.UnmetHours;
            }

            set
            {
                simulateControl.UnmetHours = value;
            }
        }

        public bool RoomDataSheets
        {
            get
            {
                return simulateControl.RoomDataSheets;
            }

            set
            {
                simulateControl.RoomDataSheets = value;
            }
        }

        public bool CreateSAP
        {
            get
            {
                return simulateControl.CreateSAP;
            }

            set
            {
                simulateControl.CreateSAP = value;
            }
        }

        public bool CreateTM59
        {
            get
            {
                return simulateControl.CreateTM59;
            }

            set
            {
                simulateControl.CreateTM59 = value;
            }
        }

        public bool CreateTPD
        {
            get
            {
                return simulateControl.CreateTPD;
            }

            set
            {
                simulateControl.CreateTPD = value;
            }
        }

        public bool CreatePartL
        {
            get
            {
                return simulateControl.CreatePartL;
            }

            set
            {
                simulateControl.CreatePartL = value;
            }
        }

        public SolarCalculationMethod SolarCalculationMethod
        {
            get
            {
                return simulateControl.SolarCalculationMethod;
            }

            set
            {
                simulateControl.SolarCalculationMethod = value;
            }
        }

        public SimulateOptions SimulateOptions
        {
            get
            {
                return simulateControl.SimulateOptions;
            }

            set
            {
                simulateControl.SimulateOptions = value;
            }
        }

        private void button_OK_Click(object sender, RoutedEventArgs e)
        {
            if(string.IsNullOrWhiteSpace(SelectedZoneCategory) && CreateSAP)
            {
                MessageBox.Show("You need to provide the Zone Category if create SAP selected");
                return;
            }

            if(SelectedWeatherData == null)
            {
                MessageBox.Show("Select Weather Data");
                return;
            }

            DialogResult = true;
        }

        private void button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
