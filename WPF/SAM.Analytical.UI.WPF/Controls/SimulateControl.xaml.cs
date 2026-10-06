// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

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

        public bool UpdateConstructionLayersByPanelType
        {
            get
            {
                return checkBox_UpdateConstructionLayersByPanelType.IsChecked != null && checkBox_UpdateConstructionLayersByPanelType.IsChecked.HasValue && checkBox_UpdateConstructionLayersByPanelType.IsChecked.Value;
            }

            set
            {
                checkBox_UpdateConstructionLayersByPanelType.IsChecked = value;
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
                CreatePartL = CreatePartL,
                UpdateConstructionLayersByPanelType = UpdateConstructionLayersByPanelType
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

            UpdateConstructionLayersByPanelType = simulateOptions.UpdateConstructionLayersByPanelType;

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

        /// <summary>
        /// Locks the settings an Approved Document O run does not leave open, so what remains on screen is
        /// only what a person actually decides.
        ///
        /// <para><b>Which ones, and why they are locked rather than merely preselected</b></para>
        /// <para>
        /// <b>Simulate</b> and <b>Full Year Simulation</b>: unticking either produces a workflow that returns
        /// a model with no annual hourly series, which <c>Query.IsPartOFullYearSimulation</c> then correctly
        /// refuses to complete the run from - after the TAS run has already been paid for. There is no such
        /// thing as a Part O run over any other range, so offering the choice only offers the chance to waste
        /// an hour of somebody's afternoon.
        /// </para>
        /// <para>
        /// <b>Sizing</b>, <b>Unmet Hours</b>, <b>Use Widths</b> and <b>Update Construction Layers</b>: these
        /// are the TAS case, and the case has to be identical for the baseline and every Iteration 2B round
        /// that repeats it - <c>PartOCanonicalTBD</c> fingerprints all four. They are settled by
        /// <c>Create.SimulateOptions_PartO</c>, which is also where the reasoning for each value is written
        /// down.
        /// </para>
        /// <para>
        /// <b>The five export boxes</b>: room data sheets, SAP, Part L, TPD and the TAS domestic-overheating
        /// XML. None is a Part O deliverable - the overheating assessment Part O produces is
        /// <c>Modify.AssessPartOTM59</c>, which reads the TSD directly.
        /// </para>
        /// <para>
        /// <b>The project name</b>: it is the run's <i>identity</i>, not a label on it. Every artifact a Part O
        /// run is judged by derives from it - <c>&lt;project&gt;.tbd</c>, <c>.tsd</c>, <c>.sam</c> and
        /// <c>&lt;project&gt;-TM59.txt</c> - and on an isolated run it already carries the scope token
        /// <c>Query.ProjectName_Isolated</c> put there so that run's evidence cannot land on a full run's or on
        /// another selection's. <c>PartOSimulationContext.Iteration_ProjectName</c> also reads the optimisation
        /// round back out of it, so an edited name can restart Iteration 2B's numbering at <c>-Opt01</c> and
        /// overwrite a previous optimisation's evidence. A person typing over it can therefore destroy the
        /// provenance of a run without anything refusing them, which is why it is derived and locked rather
        /// than prepopulated. See <c>Create.SimulateOptions_PartO</c>.
        /// </para>
        ///
        /// <para><b>What is deliberately left alone</b></para>
        /// <para>
        /// The output directory, the weather and the solar calculation method. <b>Where</b> the evidence is
        /// written stays a person's to redirect, because moving a run's files changes nothing about what the
        /// run is; the other two are engineering inputs, and Part O has no business choosing them silently.
        /// </para>
        ///
        /// <para>
        /// <b>Call this after <see cref="SimulateOptions"/> has been set</b>, never before: the
        /// <see cref="Simulate"/> setter runs <c>EnableSimulate</c>, which re-enables the sizing and
        /// full-year boxes as a group, and would undo this.
        /// </para>
        /// </summary>
        public void LockPartOSettings()
        {
            checkBox_Simulate.IsEnabled = false;
            checkBox_FullYearSimulation.IsEnabled = false;
            checkBox_Sizing.IsEnabled = false;
            checkBox_UnmetHours.IsEnabled = false;
            checkBox_UseWidths.IsEnabled = false;
            checkBox_UpdateConstructionLayersByPanelType.IsEnabled = false;

            checkBox_RoomDataSheets.IsEnabled = false;
            checkBox_CreateSAP.IsEnabled = false;
            checkBox_CreatePartL.IsEnabled = false;
            checkBox_CreateTPD.IsEnabled = false;
            checkBox_CreateTM59.IsEnabled = false;

            //The run's identity, derived from the prepared model. Read-only rather than merely disabled so the
            //name is still selectable and copyable - somebody looking for this run's TBD needs to be able to
            //take it - while remaining unchangeable. IsEnabled is set too, so "is it locked?" has one answer
            //whichever property a caller or a test asks.
            textBox_ProjectName.IsReadOnly = true;
            textBox_ProjectName.IsEnabled = false;

            //SAYS WHY, on the box itself. A locked field with no explanation reads as a defect - and this
            //one is the same dialog the ordinary Energy Simulation command opens with the field editable,
            //so an engineer has every reason to expect to be able to type in it. ShowOnDisabled, because
            //the box is disabled and a tooltip on a disabled control is otherwise never shown.
            System.Windows.Controls.ToolTipService.SetShowOnDisabled(textBox_ProjectName, true);

            textBox_ProjectName.ToolTip = string.Concat(
                "Locked on the Part O route: this is the run's identity, not a label on it.",
                System.Environment.NewLine,
                "Every artifact is named from it - .tbd, .tsd, .sam and the TM59 report - an isolated run carries its scope in it, and Iteration 2B reads its round number back out of it, so an edited name can overwrite a previous optimisation's evidence.",
                System.Environment.NewLine,
                "It is derived from the prepared model. Simulate - Energy Simulation opens this same dialog with the name editable.");

            //The From/To boxes follow the full-year box, and its own rule already disables them while it is
            //ticked. Asked again here rather than assumed, so this method states the whole locked state.
            EnableFullYearSimulation();

            //Both are driven by the SAP and domestic-overheating boxes, which are now off and locked.
            EnableTextMap();
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
