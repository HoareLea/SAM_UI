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

        public bool UpdateConstructionLayersByPanelType
        {
            get
            {
                return simulateControl.UpdateConstructionLayersByPanelType;
            }

            set
            {
                simulateControl.UpdateConstructionLayersByPanelType = value;
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

        /// <summary>
        /// The title the ordinary Energy Simulation command shows. Kept here so both are named once.
        /// <para>
        /// <b>It claims the conversion and nothing else.</b> On this route the Simulate box belongs to the
        /// user, and <c>Modify.Simulate</c> supports leaving it unticked while SAP or the
        /// domestic-overheating XML is selected - it returns early only when all three are off. So a
        /// conversion and export with no simulation is a supported outcome, and a title promising a
        /// simulation would be wrong for it.
        /// </para>
        /// </summary>
        internal const string Title_Manual = "Convert to TAS";

        /// <summary>
        /// The title the guided Part O run shows. See <see cref="LockPartOSettings"/>.
        /// <para>
        /// Here "and simulate" is a fact rather than a promise: <c>Create.SimulateOptions_PartO</c> sets
        /// <c>Simulate = true</c> and <see cref="LockPartOSettings"/> disables the box, because a Part O
        /// run with no full-year simulation is refused by <c>Query.IsPartOFullYearSimulation</c> after the
        /// TAS run has already been paid for.
        /// </para>
        /// </summary>
        internal const string Title_PartO = "Part O — Convert to TAS and simulate";

        /// <summary>
        /// Locks the settings an Approved Document O run does not leave open - see
        /// <see cref="SimulateControl.LockPartOSettings"/> for which, and why each one.
        ///
        /// <para><b>And says which dialog this is</b></para>
        /// <para>
        /// The guided Part O run and the ordinary Energy Simulation open the SAME window, and until this
        /// title existed they were indistinguishable - so a locked project name on the Part O one read as
        /// the ordinary dialog having broken, rather than as a different route with a derived identity. The
        /// title is the cheapest honest answer to "why can I not type here?", and the box's own tooltip is
        /// the full one.
        /// </para>
        /// <para>
        /// <b>Call it after <see cref="SimulateOptions"/> has been set</b>, not before.
        /// </para>
        /// </summary>
        public void LockPartOSettings()
        {
            Title = Title_PartO;

            simulateControl.LockPartOSettings();
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
