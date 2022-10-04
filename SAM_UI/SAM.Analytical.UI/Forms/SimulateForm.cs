using SAM.Weather;
using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace SAM.Analytical.UI.Forms
{
    public partial class SimulateForm : Form
    {
        public SimulateForm()
        {
            InitializeComponent();
        }

        public SimulateForm(string projectName, string outputDirectory)
        {
            InitializeComponent();

            SimulateControl_Main.OutputDirectory = outputDirectory;
            SimulateControl_Main.ProjectName = projectName;
        }

        private void SimulateForm_Load(object sender, EventArgs e)
        {
        }

        private void Button_OK_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ProjectName))
            {
                MessageBox.Show("Provide project name");
                return;
            }

            if (string.IsNullOrWhiteSpace(OutputDirectory) || !System.IO.Directory.Exists(OutputDirectory))
            {
                MessageBox.Show("Given output directory does not exists. Please provide valid directory");
                return;
            }

            if(WeatherData == null)
            {
                MessageBox.Show("Provide Wether Data");
                return;
            }

            DialogResult = DialogResult.OK;

            Close();
        }

        private void Button_Cancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;

            Close();
        }

        public string OutputDirectory
        {
            get
            {
                return SimulateControl_Main.OutputDirectory;
            }
        }

        public string ProjectName
        {
            get
            {
                return SimulateControl_Main.ProjectName;
            }
        }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public WeatherData WeatherData
        {
            get
            {
                return SimulateControl_Main.WeatherData;
            }

            set
            {
                SimulateControl_Main.WeatherData = value;
            }
        }

        public bool UnmetHours
        {
            get
            {
                return SimulateControl_Main.UnmetHours;
            }
        }

        public bool PrintRoomDataSheets
        {
            get
            {
                return SimulateControl_Main.RoomDataSheets;
            }
        }

        public bool FullYearSimulation
        {
            get
            {
                return SimulateControl_Main.FullYearSimulation;
            }
        }

        public int FullYearSimulation_From
        {
            get
            {
                return SimulateControl_Main.FullYearSimulation_From;
            }
        }

        public int FullYearSimulation_To
        {
            get
            {
                return SimulateControl_Main.FullYearSimulation_To;
            }
        }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public SolarCalculationMethod SolarCalculationMethod
        {
            get
            {
                return SimulateControl_Main.SolarCalculationMethod;
            }
        }

        public bool UpdateConstructionLayersByPanelType
        {
            get
            {
                return SimulateControl_Main.UpdateConstructionLayersByPanelType;
            }
        }
    }
}
