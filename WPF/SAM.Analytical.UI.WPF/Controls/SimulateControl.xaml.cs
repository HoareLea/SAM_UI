// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using SAM.Core.UI.WPF;
using SAM.Weather;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for ConvertToTBDControl.xaml
    /// </summary>
    public partial class SimulateControl : System.Windows.Controls.UserControl
    {
        public static string InternalText { get; set; } = Core.UI.WPF.Query.DefaultInternalText();

        public SimulateControl()
        {
            InitializeComponent();

            Load();
        }

        public void Load()
        {
            selectSAMObjectComboBoxControl_TextMap.ValidateFunc = new Func<IJSAMObject, bool>(x => x is TextMap);
            
            selectSAMObjectComboBoxControl_WeatherData.ValidateFunc = new Func<IJSAMObject, bool>(x => x is WeatherData);
            selectSAMObjectComboBoxControl_WeatherData.ReadFunc = new Func<string, IJSAMObject>(x => 
            {
                if (!UI.Query.TryGetWeatherData(x, out WeatherData weatherData_Temp) || weatherData_Temp == null)
                {
                    return null;
                }

                return weatherData_Temp;
            });

            selectSAMObjectComboBoxControl_WeatherData.DialogFilter = "epw files (*.epw)|*.epw|TAS TBD files (*.tbd)|*.tbd|TAS TSD files (*.tsd)|*.tsd|TAS TWD files (*.twd)|*.twd|All files (*.*)|*.*"; ;
            selectSAMObjectComboBoxControl_WeatherData.DialogFilterIndex = 1;

            foreach (SolarCalculationMethod solarCalculationMethod in Enum.GetValues(typeof(SolarCalculationMethod)))
            {
                if(solarCalculationMethod == SolarCalculationMethod.Undefined)
                {
                    continue;
                }

                comboBox_SolarCalculationMethod.Items.Add(Core.Query.Description(solarCalculationMethod));
            }

            EnableTextMap();
            EnableFullYearSimulation();
            EnableSimulate();
        }

        public WeatherData WeatherData
        {
            get
            {
                return selectSAMObjectComboBoxControl_WeatherData.GetJSAMObject<WeatherData>(InternalText);
            }
            set
            {
                WeatherData weatherData = value == null ? null : new WeatherData(value);
                selectSAMObjectComboBoxControl_WeatherData.Add(InternalText, weatherData);
                selectSAMObjectComboBoxControl_WeatherData.SelectedText = InternalText;
            }
        }

        public TextMap TextMap
        {
            get
            {
                return selectSAMObjectComboBoxControl_TextMap.GetJSAMObject<TextMap>(InternalText);
            }
            set
            {
                selectSAMObjectComboBoxControl_TextMap.Add(InternalText, value);
                selectSAMObjectComboBoxControl_TextMap.SelectedText = InternalText;
            }
        }

        public WeatherData SelectedWeatherData
        {
            get
            {
                return selectSAMObjectComboBoxControl_WeatherData.GetJSAMObject<WeatherData>();
            }

            set
            {
                selectSAMObjectComboBoxControl_WeatherData.SetIJSAMObject(value);
            }
        }

        public TextMap SelectedTextMap
        {
            get
            {
                return selectSAMObjectComboBoxControl_TextMap.GetJSAMObject<TextMap>();
            }

            set
            {
                selectSAMObjectComboBoxControl_TextMap.SetIJSAMObject(value);
            }
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

        public bool CreateSAP
        {
            get
            {
                return checkBox_CreateSAP.IsChecked != null && checkBox_CreateSAP.IsChecked.HasValue && checkBox_CreateSAP.IsChecked.Value;
            }

            set
            {
                checkBox_CreateSAP.IsChecked = value;
                EnableTextMap();
            }
        }

        public bool CreateTM59
        {
            get
            {
                return checkBox_CreateTM59.IsChecked != null && checkBox_CreateTM59.IsChecked.HasValue && checkBox_CreateTM59.IsChecked.Value;
            }
            set
            {
                checkBox_CreateTM59.IsChecked = value;
                EnableTextMap();
            }
        }

        public bool CreateTPD
        {
            get
            {
                return checkBox_CreateTPD.IsChecked != null && checkBox_CreateTPD.IsChecked.HasValue && checkBox_CreateTPD.IsChecked.Value;
            }

            set
            {
                checkBox_CreateTPD.IsChecked = value;
                EnableTextMap();
            }
        }

        public bool CreatePartL
        {
            get
            {
                return checkBox_CreatePartL.IsChecked != null && checkBox_CreatePartL.IsChecked.HasValue && checkBox_CreatePartL.IsChecked.Value;
            }

            set
            {
                checkBox_CreatePartL.IsChecked = value;
                EnableTextMap();
            }
        }

        public bool FullYearSimulation
        {
            get
            {
                return checkBox_FullYearSimulation.IsChecked != null && checkBox_FullYearSimulation.IsChecked.HasValue && checkBox_FullYearSimulation.IsChecked.Value;
            }

            set
            {
                checkBox_FullYearSimulation.IsChecked = value;
                EnableFullYearSimulation();
            }
        }

        public int FullYearSimulation_From
        {
            get
            {
                if(!Core.Query.TryConvert(textBox_FullYearSimulation_From.Text, out int result))
                {
                    return -1;
                }

                return result;
            }
        }

        public int FullYearSimulation_To
        {
            get
            {
                if (!Core.Query.TryConvert(textBox_FullYearSimulation_To.Text, out int result))
                {
                    return -1;
                }

                return result;
            }
        }

        public bool Simulate
        {
            get
            {
                return checkBox_Simulate.IsChecked != null && checkBox_Simulate.IsChecked.HasValue && checkBox_Simulate.IsChecked.Value;
            }

            set
            {
                checkBox_Simulate.IsChecked = value;
                EnableSimulate();
            }
        }

        public bool Sizing
        {
            get
            {
                return checkBox_Sizing.IsChecked != null && checkBox_Sizing.IsChecked.HasValue && checkBox_Sizing.IsChecked.Value;
            }

            set
            {
                checkBox_Sizing.IsChecked = value;
            }
        }

        public bool UseWidths
        {
            get
            {
                return checkBox_UseWidths.IsChecked != null && checkBox_UseWidths.IsChecked.HasValue && checkBox_UseWidths.IsChecked.Value;
            }

            set
            {
                checkBox_UseWidths.IsChecked = value;
            }
        }

        public string SelectedZoneCategory
        {
            get
            {
                return comboBox_ZoneCategory.SelectedItem as string;
            }
        }

        public SolarCalculationMethod SolarCalculationMethod
        {
            get
            {
                return Core.Query.Enum<SolarCalculationMethod>(comboBox_SolarCalculationMethod.Text);
            }

            set
            {
                comboBox_SolarCalculationMethod.SelectedItem = Core.Query.Description(value);
            }
        }

        public List<string> ZoneCategories
        {
            get
            {
                List<string> result = new List<string>();
                foreach(object @object in comboBox_ZoneCategory.Items)
                {
                    if(@object is string)
                    {
                        result.Add((string)@object);
                    }
                }

                return result;
            }
            set
            {
                string value_Temp = comboBox_ZoneCategory.SelectedItem as string;
                comboBox_ZoneCategory.Items.Clear();
                if(value != null)
                {
                    foreach (string value_New in value)
                    {
                        comboBox_ZoneCategory.Items.Add(value_New);
                    }
                }

                if(!string.IsNullOrWhiteSpace(value_Temp))
                {
                    comboBox_ZoneCategory.SelectedItem = value_Temp;
                }
                else if(comboBox_ZoneCategory.Items.Count != 0)
                {
                    comboBox_ZoneCategory.SelectedItem = comboBox_ZoneCategory.Items[0];
                }
            }
        }

        public SimulateOptions SimulateOptions
        {
            get
            {
                return GetSimulateOptions();
            }
            set
            {
                SetSimulateOptions(value);
            }
        }

        private SimulateOptions GetSimulateOptions()
        {
            SimulateOptions result = new SimulateOptions()
            {
                ProjectName = ProjectName,
                ZoneCategories = ZoneCategories,
                WeatherData = SelectedWeatherData,
                TextMap = SelectedTextMap,
                OutputDirectory = OutputDirectory,
                Simulate = Simulate,
                SolarCalculationMethod = SolarCalculationMethod,
                FullYearSimulation = FullYearSimulation,
                UnmetHours = UnmetHours,
                RoomDataSheets = RoomDataSheets,
                CreateSAP = CreateSAP,
                CreateTM59 = CreateTM59,
                UseWidths = UseWidths,
                CreateTPD = CreateTPD,
                Sizing = Sizing,
                CreatePartL = CreatePartL
            };

            return result;
        }

        private bool SetSimulateOptions(SimulateOptions simulateOptions)
        {
            if(simulateOptions == null)
            {
                return false;
            }

            ProjectName = simulateOptions.ProjectName;
            ZoneCategories = simulateOptions.ZoneCategories;
            OutputDirectory = simulateOptions.OutputDirectory;
            Simulate = simulateOptions.Simulate;
            SolarCalculationMethod = simulateOptions.SolarCalculationMethod;
            FullYearSimulation = simulateOptions.FullYearSimulation;
            UnmetHours = simulateOptions.UnmetHours;
            RoomDataSheets = simulateOptions.RoomDataSheets;
            CreateSAP = simulateOptions.CreateSAP;
            CreateTM59 = simulateOptions.CreateTM59;
            CreatePartL = CreatePartL;

            UseWidths = simulateOptions.UseWidths;
            CreateTPD = simulateOptions.CreateTPD;

            TextMap = simulateOptions.TextMap;
            WeatherData = simulateOptions.WeatherData;

            Sizing = simulateOptions.Sizing;

            return true;
        }

        private void checkBox_CreateTM59_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            EnableTextMap();
        }

        private void checkBox_CreateSAP_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            EnableTextMap();
        }

        private void EnableTextMap()
        {
            bool enable = CreateSAP || CreateTM59;
            label_TextMap.IsEnabled = enable;
            selectSAMObjectComboBoxControl_TextMap.IsEnabled = enable;
            label_ZoneCategory.IsEnabled = enable;
            comboBox_ZoneCategory.IsEnabled = enable;
        }

        private void EnableFullYearSimulation()
        {
            bool enable = !FullYearSimulation && checkBox_FullYearSimulation.IsEnabled;
            label_FullYearSimulation_From.IsEnabled = enable;
            textBox_FullYearSimulation_From.IsEnabled = enable;
            label_FullYearSimulation_To.IsEnabled = enable;
            textBox_FullYearSimulation_To.IsEnabled = enable;
        }

        private void EnableSimulate()
        {
            bool enable = Simulate;
            EnableSimulate(enable);
        }

        private void EnableSimulate(bool enable)
        {
            checkBox_UseWidths.IsEnabled = enable;
            label_SolarCalculationMethod.IsEnabled = enable;
            comboBox_SolarCalculationMethod.IsEnabled = enable;
            checkBox_Sizing.IsEnabled = enable;
            checkBox_FullYearSimulation.IsEnabled = enable;
            EnableFullYearSimulation();
            checkBox_UnmetHours.IsEnabled = enable;
            checkBox_CreateTPD.IsEnabled = enable;
        }

        private void checkBox_Simulate_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            EnableSimulate();
        }

        private void button_OutputDirectory_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog())
            {
                folderBrowserDialog.Description = "Select Output Directory";
                folderBrowserDialog.ShowNewFolderButton = true;
                if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                {
                    textBox_OutputDirectory.Text = folderBrowserDialog.SelectedPath;
                    textBox_OutputDirectory.SelectionStart = textBox_OutputDirectory.Text.Length;
                    textBox_OutputDirectory.SelectionLength = 0;
                }
            }
        }

        private void textBox_FullYearSimulation_From_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text);
        }

        private static bool IsTextAllowed(string text)
        {
            bool result = !new Regex("[^0-9.-]+").IsMatch(text);
            if(!result)
            {
                return false;
            }

            if(!Core.Query.TryConvert(text, out int value))
            {
                return false;
            }

            return value >= 0 && value <= 365;
        }

        private void textBox_FullYearSimulation_To_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text);
        }

        private void checkBox_FullYearSimulation_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            EnableFullYearSimulation();
        }

        private void checkBox_FullYearSimulation_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            EnableFullYearSimulation();
        }

        private void checkBox_CreateTPD_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            checkBox_FullYearSimulation.IsChecked = true;
            checkBox_FullYearSimulation.IsEnabled = false;
        }

        private void checkBox_CreateTPD_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            checkBox_FullYearSimulation.IsEnabled = true;
        }
    }
}
