using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace SAM.Core.Mollier.UI
{
    public partial class MollierControlSettingsForm : Form
    {
        private Controls.MollierControl mollierControl;
        private ToolTip toolTip = new ToolTip();
        public MollierControlSettingsForm()
        {
            InitializeComponent();
        }

        public MollierControlSettingsForm(Controls.MollierControl mollierControl)
        {
            InitializeComponent();
            this.mollierControl = mollierControl;
            MollierControlSettings mollierControlSettings = mollierControl.MollierControlSettings;

            HumidityRatio_Max = mollierControlSettings.HumidityRatio_Max;
            HumidityRatio_Min = mollierControlSettings.HumidityRatio_Min;
            HumidityRatio_Interval = mollierControlSettings.HumidityRatio_Interval;
            Temperature_Max = mollierControlSettings.Temperature_Max;
            Temperature_Min = mollierControlSettings.Temperature_Min;
            Temperature_Interval = mollierControlSettings.Temperature_Interval;
            P_w_Interval = mollierControlSettings.P_w_Interval;
            GradientPoint = mollierControlSettings.GradientPoint;
            DisableUnits = mollierControlSettings.DisableUnits;
            DisableLabels = mollierControlSettings.DisableLabels;
            GradientColors = mollierControlSettings.GradientColors;

            VisibilitySettings visibilitySettings = mollierControlSettings.VisibilitySettings; 
            if(visibilitySettings != null)
            {
                List<BuiltInVisibilitySetting> builtInVisibilitySettings = visibilitySettings.GetVisibilitySettings<BuiltInVisibilitySetting>(mollierControlSettings.Color);
                if(builtInVisibilitySettings != null)
                {
                    foreach(BuiltInVisibilitySetting builtInVisibilitySetting in builtInVisibilitySettings)
                    {
                        FlowLayoutPanel_BuiltInVisibilitySettings.Controls.Add(new Controls.BuiltInVisibilitySettingControl(builtInVisibilitySetting));
                    }
                }
            }
        }


        private void Button_Cancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;

            Close();
        }
        private void Button_OK_Click(object sender, EventArgs e)
        {
            Apply();
            DialogResult = DialogResult.OK;

            Close();
        }
        private void Button_Apply_Click(object sender, EventArgs e)
        {
            Apply();
        }

        public double HumidityRatio_Max
        {
            get
            {
                if (!Core.Query.TryConvert(HumidityRatioMaximumValueTextbox.Text, out double humidityRatio_Max))
                {
                    return double.NaN;
                }
                if (humidityRatio_Max < -20 || humidityRatio_Max > 50)
                {
                    MessageBox.Show("Wrong range\n Maximal Humidity Ratio is 50!");
                    return double.NaN;
                }

                return humidityRatio_Max;
            }

            set
            {
                HumidityRatioMaximumValueTextbox.Text = System.Math.Round(value, 2).ToString();
            }
        }
        public double HumidityRatio_Min
        {
            get
            {
                if (!Core.Query.TryConvert(HumidityRatioMinimumValueTextbox.Text, out double humidityRatio_Min))
                {
                    return double.NaN;
                }
                if (humidityRatio_Min < 0 || humidityRatio_Min > 50)
                {
                    MessageBox.Show("Wrong range\n Minimal Humidity Ratio is 0!");
                    return double.NaN;
                }

                return humidityRatio_Min;
            }
            set
            {
                HumidityRatioMinimumValueTextbox.Text = System.Math.Round(value, 2).ToString();
            }
        }
        public double HumidityRatio_Interval
        {
            get
            {
                if(!Core.Query.TryConvert(HumidityRatioIntervalTextbox.Text, out double humidityRatio_Interval))
                {
                    return double.NaN;
                }
                if(humidityRatio_Interval <= 0)
                {
                    MessageBox.Show("Wrong range\n Interval has to be positive!");
                    return double.NaN;
                }
                return humidityRatio_Interval;
            }
            set
            {
                HumidityRatioIntervalTextbox.Text = value.ToString();
            }
        }
        public double Temperature_Max
        {
            get
            {
                if(!Core.Query.TryConvert(TemperatureMaximumValueTextbox.Text, out double temperature_Max))
                {
                    return double.NaN;
                }
                if(temperature_Max < -20 || temperature_Max > 50)
                {
                    MessageBox.Show("Wrong range\n Maximal possibly temperature is 50!");
                    return double.NaN;
                }

                return temperature_Max;
            }
            set
            {
                TemperatureMaximumValueTextbox.Text = System.Math.Round(value, 2).ToString();
            }
        }
        public double Temperature_Min
        {
            get
            {
                if(!Core.Query.TryConvert(TemperatureMinimumValueTextbox.Text, out double temperature_Min))
                {
                    return double.NaN;
                }
                if(temperature_Min < -20 || temperature_Min > 50)
                {
                    MessageBox.Show("Wrong range\n Minimal possibly temperature is -20!");
                    return double.NaN;
                }

                return temperature_Min;
            }
            set
            {
                TemperatureMinimumValueTextbox.Text = System.Math.Round(value, 2).ToString();
            }
        }
        public double Temperature_Interval
        {
            get
            {
                if(!Core.Query.TryConvert(TemperatureIntervalTextbox.Text, out double temperature_Interval))
                {
                    return double.NaN;
                }
                if(temperature_Interval <= 0)
                {
                    MessageBox.Show("Wrong range\n Interval has to be positive!");
                    return double.NaN;
                }
                return temperature_Interval;
            }
            set
            {
                TemperatureIntervalTextbox.Text = value.ToString();
            }
        }
        public double P_w_Interval
        {
            get
            {
                if (!Core.Query.TryConvert(P_w_IntervalTextBox.Text, out double P_w_Interval))
                {
                    return double.NaN;
                }
                if (P_w_Interval <= 0)
                {
                    MessageBox.Show("Wrong range\n Interval has to be positive!");
                    return double.NaN;
                }
                return P_w_Interval;
            }
            set
            {
                P_w_IntervalTextBox.Text = value.ToString();
            }
        }
        public bool GradientPoint
        {
            get
            {
                return CheckBox_GradientPoint.Checked;
            }
            set
            {
                CheckBox_GradientPoint.Checked = value;
            }
        }
        public bool DisableUnits
        {
            get
            {
                return CheckBox_DisableUnits.Checked;
            }
            set
            {
                CheckBox_DisableUnits.Checked = value;
            }
        }
        public bool DisableLabels
        {
            get
            {
                return CheckBox_DisableLabels.Checked;
            }
            set
            {
                CheckBox_DisableLabels.Checked = value;
            }
        }
        public PointGradientVisibilitySetting GradientColors
        {
            get
            {
                PointGradientVisibilitySetting pointGradientVisibilitySetting = new PointGradientVisibilitySetting(Button_LowIntensityColor.BackColor, Button_HighIntensityColor.BackColor); 
                return pointGradientVisibilitySetting;
            }
            set
            {
                PointGradientVisibilitySetting pointGradientVisibilitySetting = value;
                if(pointGradientVisibilitySetting != null)
                {
                    Button_LowIntensityColor.BackColor = pointGradientVisibilitySetting.Color;
                    Button_HighIntensityColor.BackColor = pointGradientVisibilitySetting.GradientColor;
                }
            }
        }
        private void Apply()
        {
            MollierControlSettings mollierControlSettings = mollierControl.MollierControlSettings;
            if (HumidityRatio_Max.ToString() != double.NaN.ToString())
                mollierControlSettings.HumidityRatio_Max = HumidityRatio_Max;
            if (HumidityRatio_Min.ToString() != double.NaN.ToString())
                mollierControlSettings.HumidityRatio_Min = HumidityRatio_Min;
            if(HumidityRatio_Interval.ToString() != double.NaN.ToString())
                mollierControlSettings.HumidityRatio_Interval = HumidityRatio_Interval;
            if(Temperature_Min.ToString() != double.NaN.ToString())
                mollierControlSettings.Temperature_Min = Temperature_Min;
            if(Temperature_Max.ToString() != double.NaN.ToString())
                mollierControlSettings.Temperature_Max = Temperature_Max;
            if(Temperature_Interval.ToString() != double.NaN.ToString())
                mollierControlSettings.Temperature_Interval = Temperature_Interval;
            if (P_w_Interval.ToString() != double.NaN.ToString())
                mollierControlSettings.P_w_Interval = P_w_Interval;

            mollierControlSettings.GradientPoint = GradientPoint;
            mollierControlSettings.DisableUnits = DisableUnits;
            mollierControlSettings.DisableLabels = DisableLabels;
            mollierControlSettings.GradientColors = GradientColors;

            VisibilitySettings visibilitySettings = mollierControlSettings.VisibilitySettings;
            if(visibilitySettings == null)
            {
                visibilitySettings = new VisibilitySettings();
            }

            List<IVisibilitySetting> visibilitySettingsList = new List<IVisibilitySetting>();
            foreach(Control control in FlowLayoutPanel_BuiltInVisibilitySettings.Controls)
            {
                Controls.BuiltInVisibilitySettingControl builtInVisibilitySettingControl = control as Controls.BuiltInVisibilitySettingControl;
                if(builtInVisibilitySettingControl  == null)
                {
                    continue;
                }

                visibilitySettingsList.Add(builtInVisibilitySettingControl.BuiltInVisibilitySetting);
            }

            visibilitySettings.SetVisibilitySettings("User", visibilitySettingsList);
            mollierControlSettings.Color = "User";

            mollierControlSettings.VisibilitySettings = visibilitySettings;

            mollierControl.MollierControlSettings = mollierControlSettings;
        }

        private void Button_LowIntensityColor_Click(object sender, EventArgs e)
        {
            using(ColorDialog colorDialog = new ColorDialog())
            {
                colorDialog.Color = mollierControl.MollierControlSettings.GradientColors.Color;
                if (colorDialog.ShowDialog() != DialogResult.OK)
                {
                    return;
                }
                Button_LowIntensityColor.BackColor = colorDialog.Color;
            }
        }
        private void Button_HighIntensityColor_Click(object sender, EventArgs e)
        {
            using (ColorDialog colorDialog = new ColorDialog())
            {
                colorDialog.Color = mollierControl.MollierControlSettings.GradientColors.GradientColor;
                if (colorDialog.ShowDialog() != DialogResult.OK)
                {
                    return;
                }
                Button_HighIntensityColor.BackColor = colorDialog.Color;
            }
        }

        private void CheckBox_GradientPoint_MouseHover(object sender, EventArgs e)
        {
            toolTip.Show("Paints the points in two colors depending on their intensity", CheckBox_GradientPoint);
            toolTip.Active = true;
        }

        private void CheckBox_GradientPoint_MouseLeave(object sender, EventArgs e)
        {
            toolTip.Active = false;
            toolTip.Active = true;
        }

        private void CheckBox_DisableUnits_MouseHover(object sender, EventArgs e)
        {
            toolTip.Show("Turns off the visibility of all units", CheckBox_DisableUnits);
            toolTip.Active = true;
        }

        private void CheckBox_DisableUnits_MouseLeave(object sender, EventArgs e)
        {
            toolTip.Active = false;
            toolTip.Active = true;
        }

        private void CheckBox_DisableLabels_MouseHover(object sender, EventArgs e)
        {
            toolTip.Show("Turns off the visibility of all line names", CheckBox_DisableLabels);
            toolTip.Active = true;
        }

        private void CheckBox_DisableLabels_MouseLeave(object sender, EventArgs e)
        {
            toolTip.Active = false;
            toolTip.Active = true;
        }

        private void Button_LowIntensityColor_MouseHover(object sender, EventArgs e)
        {
            toolTip.Show("The color of the less intense points", Button_LowIntensityColor);
            toolTip.Active = true;
        }

        private void Button_LowIntensityColor_MouseLeave(object sender, EventArgs e)
        {
            toolTip.Active = false;
            toolTip.Active = true;
        }

        private void Button_HighIntensityColor_MouseHover(object sender, EventArgs e)
        {
            toolTip.Show("The color of the more intense points", Button_HighIntensityColor);
            toolTip.Active = true;
        }

        private void Button_HighIntensityColor_MouseLeave(object sender, EventArgs e)
        {
            toolTip.Active = false;
            toolTip.Active = true;
        }
    }
}
