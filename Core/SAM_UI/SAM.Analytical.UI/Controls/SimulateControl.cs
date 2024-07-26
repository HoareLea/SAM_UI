using SAM.Weather;
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;

namespace SAM.Analytical.UI.Controls
{
    public partial class SimulateControl : UserControl
    {
        private WeatherData weatherData;

        public SimulateControl()
        {
            InitializeComponent();
        }

        private void Button_OutputDirectory_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog())
            {
                folderBrowserDialog.Description = "Select Output Directory";
                folderBrowserDialog.ShowNewFolderButton = true;
                if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                {
                    TextBox_OutputDirectory.Text = folderBrowserDialog.SelectedPath;
                    TextBox_OutputDirectory.SelectionStart = TextBox_OutputDirectory.Text.Length;
                    TextBox_OutputDirectory.SelectionLength = 0;
                }
            }
        }

        private void Button_WeatherData_Click(object sender, EventArgs e)
        {
            if (!Query.TryGetWeatherData(out WeatherData weatherData_Temp) || weatherData_Temp == null)
            {
                return;
            }

            weatherData = weatherData_Temp;

            TextBox_WeatherData.Text = string.IsNullOrWhiteSpace(weatherData?.Name) ? "???" : weatherData.Name;
        }

        public string OutputDirectory
        {
            get
            {
                return TextBox_OutputDirectory.Text;
            }

            set
            {
                TextBox_OutputDirectory.Text = value;
            }
        }

        public string ProjectName
        {
            get
            {
                return TextBox_ProjectName.Text;
            }

            set
            {
                TextBox_ProjectName.Text = value;
            }
        }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public WeatherData WeatherData
        {
            get
            {
                return weatherData;
            }

            set
            {
                weatherData = value;
                TextBox_WeatherData.Text = string.IsNullOrWhiteSpace(weatherData?.Name) ? "???" : weatherData.Name;
            }
        }

        public bool UnmetHours
        {
            get
            {
                return CheckBox_UnmetHours.Checked;
            }

            set
            {
                CheckBox_UnmetHours.Checked = value;
            }
        }

        public bool RoomDataSheets
        {
            get
            {
                return CheckBox_RoomDataSheets.Checked;
            }

            set
            {
                CheckBox_RoomDataSheets.Checked = value;
            }
        }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public SolarCalculationMethod SolarCalculationMethod
        {
            get
            {
                return ComboBoxControl_SolarCalculationMethod.GetSelectedItem<SolarCalculationMethod>();
            }

            set
            {
                ComboBoxControl_SolarCalculationMethod.SetSelectedItem(value);
            }
        }

        public bool Sizing
        {
            get
            {
                return CheckBox_Sizing.Checked;
            }
        }

        public bool UpdateConstructionLayersByPanelType
        {
            get
            {
                return CheckBox_UpdateConstructionLayersByPanelType.Checked;
            }

            set
            {
                CheckBox_UpdateConstructionLayersByPanelType.Checked = value;
            }
        }

        public bool FullYearSimulation
        {
            get
            {
                return CheckBox_FullYearSimulation.Checked;
            }

            set
            {
                CheckBox_FullYearSimulation.Checked = value;
            }
        }

        public int FullYearSimulation_From
        {
            get
            {
                if (!Core.Query.TryConvert(TextBox_From.Text, out int result))
                {
                    return -1;
                }

                return result;
            }

            set
            {
                TextBox_From.Text = value.ToString();
            }
        }

        public int FullYearSimulation_To
        {
            get
            {
                if (!Core.Query.TryConvert(TextBox_To.Text, out int result))
                {
                    return -1;
                }

                return result;
            }

            set
            {
                TextBox_To.Text = value.ToString();
            }
        }

        private void SimulateControl_Load(object sender, EventArgs e)
        {
            ComboBoxControl_SolarCalculationMethod.AddRange(Enum.GetValues(typeof(SolarCalculationMethod)).Cast<Enum>(), (Enum x) => Core.Query.Description(x));
            ComboBoxControl_SolarCalculationMethod.SetSelectedItem(SolarCalculationMethod.SAM);

            SetEnabled();
        }

        private void CheckBox_FullYearSimulation_CheckedChanged(object sender, EventArgs e)
        {
            SetEnabled();
        }

        private void SetEnabled()
        {
            bool fullYearSimulation = CheckBox_FullYearSimulation.Checked;

            Label_From.Enabled = fullYearSimulation;
            TextBox_From.Enabled = fullYearSimulation;
            Label_To.Enabled = fullYearSimulation;
            TextBox_To.Enabled = fullYearSimulation;
        }

        private void ComboBoxControl_SolarCalculationMethod_Load(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }
    }
}
