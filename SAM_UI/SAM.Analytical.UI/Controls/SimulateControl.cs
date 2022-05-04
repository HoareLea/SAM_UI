using SAM.Weather;
using System;
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

        private void SimulateControl_Load(object sender, EventArgs e)
        {
            ComboBoxControl_SolarCalculationMethod.AddRange(Enum.GetValues(typeof(SolarCalculationMethod)).Cast<Enum>(), (Enum x) => Core.Query.Description(x));
            ComboBoxControl_SolarCalculationMethod.SetSelectedItem(SolarCalculationMethod.SAM);
        }
    }
}
