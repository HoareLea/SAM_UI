using SAM.Core.Mollier.UI.Controls;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace SAM.Core.Mollier.UI
{
    public partial class MollierControlSettingsForm : Form
    {
        public event EventHandler ApplyClicked;

        private MollierFormSettings mollierFormSettings;
        
        private MollierControl mollierControl;
        private ToolTip toolTip = new ToolTip();

        private System.Drawing.Color initialColor = System.Drawing.Color.Empty;

        public List<int> CustomColors { get; set; }  = new List<int>();

        public MollierControlSettingsForm()
        {
            InitializeComponent();
        }

        public MollierControlSettingsForm(MollierControl mollierControl, MollierFormSettings mollierFormSettings)
        {
            InitializeComponent();

            initialColor = Button_PointColor.BackColor;

            this.mollierFormSettings = mollierFormSettings;

            this.mollierControl = mollierControl;
            MollierControlSettings mollierControlSettings = mollierControl.MollierControlSettings;
            HumidityRatio_Max = mollierControlSettings.HumidityRatio_Max * 1000;
            HumidityRatio_Min = mollierControlSettings.HumidityRatio_Min * 1000;
            HumidityRatio_Interval = mollierControlSettings.HumidityRatio_Interval * 1000;
            Temperature_Max = mollierControlSettings.Temperature_Max;
            Temperature_Min = mollierControlSettings.Temperature_Min;
            Temperature_Interval = mollierControlSettings.Temperature_Interval;
            PartialVapourPressure = mollierControlSettings.PartialVapourPressure_Interval;
            Density_Interval = mollierControlSettings.Density_Interval;
            Enthalpy_Interval = mollierControlSettings.Enthalpy_Interval;
            SpecificVolume_Interval = mollierControlSettings.SpecificVolume_Interval;
            WetBulbTemperature_Interval = mollierControlSettings.WetBulbTemperature_Interval;

            GradientPoint = mollierControlSettings.GradientPoint;
            DisableUnits = mollierControlSettings.DisableUnits;
            DisableLabels = mollierControlSettings.DisableLabels;
            VisualizeSolver = mollierControlSettings.VisualizeSolver;
            PointGradientVisibilitySetting pointGradientVisibilitySetting = mollierControl.MollierControlSettings.VisibilitySettings.GetVisibilitySetting("User", ChartParameterType.Point) as PointGradientVisibilitySetting;
            if(pointGradientVisibilitySetting != null)
            {
                Button_LowIntensityColor.BackColor = pointGradientVisibilitySetting.Color;
                Button_HighIntensityColor.BackColor = pointGradientVisibilitySetting.GradientColor;
                CheckBox_GradientPoint.Checked = true;
            }
            else
            {
                PointGradientVisibilitySetting defaultPointGradientVisibilitySetting = Query.DefaultPointGradientVisibilitySetting();
                Button_LowIntensityColor.BackColor = defaultPointGradientVisibilitySetting.Color;
                Button_HighIntensityColor.BackColor = defaultPointGradientVisibilitySetting.GradientColor;
                CheckBox_GradientPoint.Checked = false;
            }

            DisableStartProcessPoint = mollierControlSettings.DisableStartProcessPoint;
            DisableEndProcessPoint = mollierControlSettings.DisableEndProcessPoint;
            DisablePointBoarder = mollierControlSettings.DisablePointBoarder;
            ProccessLineThickness = mollierControlSettings.ProccessLineThickness;

            DisableLabelStartProcessPoint = mollierControlSettings.DisableLabelStartProcessPoint;
            DisableLabelEndProcessPoint = mollierControlSettings.DisableLabelEndProcessPoint;
            DisableLabelProcess = mollierControlSettings.DisableLabelProcess;
            PointBoarderColor = mollierControlSettings.PointBorderColor;
            PointColor = mollierControlSettings.PointColor;
            DisablePoint = mollierControlSettings.DisablePoint;
            DisableCoolingAuxiliaryProcesses = mollierControlSettings.DisableCoolingAuxiliaryProcesses;

            VisibilitySettings visibilitySettings = mollierControlSettings.VisibilitySettings; 
            if(visibilitySettings != null)
            {
                List<BuiltInVisibilitySetting> builtInVisibilitySettings = visibilitySettings.GetVisibilitySettings<BuiltInVisibilitySetting>(mollierControlSettings.DefaultTemplateName);
                if(builtInVisibilitySettings != null)
                {
                    foreach(BuiltInVisibilitySetting builtInVisibilitySetting in builtInVisibilitySettings)
                    {
                        BuiltInVisibilitySettingControl builtInVisibilitySettingControl = new BuiltInVisibilitySettingControl(builtInVisibilitySetting);
                        builtInVisibilitySettingControl.ColorChanged += BuiltInVisibilitySettingControl_ColorChanged;

                        FlowLayoutPanel_BuiltInVisibilitySettings.Controls.Add(builtInVisibilitySettingControl);
                    }
                }
            }

            TextBox_WindowHeight.Text = mollierFormSettings?.Height == -1 ? string.Empty : mollierFormSettings.Height.ToString();
            TextBox_WindowWidth.Text = mollierFormSettings?.Width == -1 ? string.Empty : mollierFormSettings.Width.ToString();

            TextBox_WindowHeight.KeyPress += new KeyPressEventHandler(Windows.EventHandler.ControlText_IntegerOnly);
            TextBox_WindowWidth.KeyPress += new KeyPressEventHandler(Windows.EventHandler.ControlText_IntegerOnly);
        }

        private void BuiltInVisibilitySettingControl_ColorChanged(object sender, EventArgs e)
        {
            BuiltInVisibilitySettingControl builtInVisibilitySettingControl = sender as BuiltInVisibilitySettingControl;
            if(builtInVisibilitySettingControl == null)
            {
                return;
            }

            CustomColors = builtInVisibilitySettingControl.CustomColors;
            foreach(Control control in FlowLayoutPanel_BuiltInVisibilitySettings.Controls)
            {
                BuiltInVisibilitySettingControl builtInVisibilitySettingControl_Temp = control as BuiltInVisibilitySettingControl;
                if(builtInVisibilitySettingControl_Temp == null)
                {
                    continue;
                }

                builtInVisibilitySettingControl_Temp.CustomColors = CustomColors;
            }
        }

        public MollierFormSettings MollierFormSettings
        {
            get
            {
                MollierFormSettings result = mollierFormSettings == null ? new MollierFormSettings() : new MollierFormSettings(mollierFormSettings);

                if(Core.Query.TryConvert(TextBox_WindowHeight.Text, out int height))
                {
                    result.Height = height;
                }
                else
                {
                    result.Height = -1;
                }

                if (Core.Query.TryConvert(TextBox_WindowWidth.Text, out int width))
                {
                    result.Width = width;
                }
                else
                {
                    result.Width = -1;
                }

                return result;
            }

            set
            {
                mollierFormSettings = value;
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
                if (humidityRatio_Max > Limit.HumidityRatio_Max)
                {
                    MessageBox.Show("Wrong range\nMaximal Humidity Ratio is " + Limit.HumidityRatio_Max * 1000 + "!");
                    return double.NaN;
                }
                if (humidityRatio_Max <= System.Convert.ToDouble(HumidityRatioMinimumValueTextbox.Text))
                {
                    MessageBox.Show("Wrong range\nMaximal Humidity Ratio must be greater than minimal Humidity Ratio!");
                    return double.NaN;
                }
                return humidityRatio_Max / 1000;
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
                if (humidityRatio_Min < Limit.HumidityRatio_Min)
                {
                    MessageBox.Show("Wrong range\nMinimal Humidity Ratio is " + Limit.HumidityRatio_Min * 1000 + "!");
                    return double.NaN;
                }
                if(humidityRatio_Min >= System.Convert.ToDouble(HumidityRatioMaximumValueTextbox.Text))
                {
                    MessageBox.Show("Wrong range\nMinimal Humidity Ratio must be less than maximal Humidity Ratio!");
                    return double.NaN;
                }

                return humidityRatio_Min / 1000;
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
                    MessageBox.Show("Wrong range\nInterval has to be positive!");
                    return double.NaN;
                }
                if (humidityRatio_Interval > System.Convert.ToDouble(HumidityRatioMaximumValueTextbox.Text) - System.Convert.ToDouble(HumidityRatioMinimumValueTextbox.Text))
                {
                    MessageBox.Show("Wrong range\nInterval can not be greater than the axis lenght!");
                    return double.NaN;
                }
                return humidityRatio_Interval / 1000;
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
                if(temperature_Max > Limit.DryBulbTemperature_Max)
                {
                    MessageBox.Show("Wrong range\nMaximal possibly temperature is " + Limit.DryBulbTemperature_Max + "!");
                    return double.NaN;
                }
                if (System.Convert.ToDouble(TemperatureMinimumValueTextbox.Text) >= temperature_Max)
                {
                    MessageBox.Show("Wrong range\nMaximal Temperature must be greater than minimal Temperature!");
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
                if(temperature_Min < Limit.DryBulbTemperature_Min)
                {
                    MessageBox.Show("Wrong range\nMinimal possibly temperature is " + Limit.DryBulbTemperature_Min + "!");
                    return double.NaN;
                }
                if (temperature_Min >= System.Convert.ToDouble(TemperatureMaximumValueTextbox.Text))
                {
                    MessageBox.Show("Wrong range\nMinimal Temperature must be less than maximal Temperature!");
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
                    MessageBox.Show("Wrong range\nInterval has to be positive!");
                    return double.NaN;
                }
                if(temperature_Interval > System.Convert.ToDouble(TemperatureMaximumValueTextbox.Text) - System.Convert.ToDouble(TemperatureMinimumValueTextbox.Text))
                {
                    MessageBox.Show("Wrong range\nInterval can not be greater than the axis lenght!");
                    return double.NaN;
                }
                return temperature_Interval;
            }
            set
            {
                TemperatureIntervalTextbox.Text = value.ToString();
            }
        }
        public double PartialVapourPressure
        {
            get
            {
                if (!Core.Query.TryConvert(PartialVapourPressure_IntervalTextBox.Text, out double PartialVapourPressure))
                {
                    return double.NaN;
                }
                if (PartialVapourPressure <= 0)
                {
                    MessageBox.Show("Wrong range\n Interval has to be positive!");
                    return double.NaN;
                }
                return PartialVapourPressure;
            }
            set
            {
                PartialVapourPressure_IntervalTextBox.Text = value.ToString();
            }
        }
       
        public double Density_Interval
        {
            get
            {
                if (!Core.Query.TryConvert(DensityIntervalTextBox.Text, out double DensityInterval))
                {
                    return double.NaN;
                }
                if (DensityInterval <= 0)
                {
                    MessageBox.Show("Wrong range\n Interval has to be positive!");
                    return double.NaN;
                }
                return DensityInterval;
            }
            set
            {
                DensityIntervalTextBox.Text = value.ToString();
            }
        }
        public double Enthalpy_Interval
        {
            get
            {
                if (!Core.Query.TryConvert(EnthalpyIntervalTextBox.Text, out double EnthalpyInterval))
                {
                    return double.NaN;
                }
                if (EnthalpyInterval <= 0)
                {
                    MessageBox.Show("Wrong range\n Interval has to be positive!");
                    return double.NaN;
                }
                return EnthalpyInterval * 1000;
            }
            set
            {
                EnthalpyIntervalTextBox.Text = (value / 1000).ToString();
            }
        }
        public double SpecificVolume_Interval
        {
            get
            {
                if (!Core.Query.TryConvert(SpecificVolumeIntervalTextBox.Text, out double SpecificVolumeInterval))
                {
                    return double.NaN;
                }
                if (SpecificVolumeInterval <= 0)
                {
                    MessageBox.Show("Wrong range\n Interval has to be positive!");
                    return double.NaN;
                }
                return SpecificVolumeInterval;
            }
            set
            {
                SpecificVolumeIntervalTextBox.Text = value.ToString();
            }
        }
        public double WetBulbTemperature_Interval
        {
            get
            {
                if (!Core.Query.TryConvert(WetBulbTemperatureIntervalTextBox.Text, out double WetBulbTemperatureInterval))
                {
                    return double.NaN;
                }
                if (WetBulbTemperatureInterval <= 0)
                {
                    MessageBox.Show("Wrong range\n Interval has to be positive!");
                    return double.NaN;
                }
                return WetBulbTemperatureInterval;
            }
            set
            {
                WetBulbTemperatureIntervalTextBox.Text = value.ToString();
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
        public bool VisualizeSolver
        {
            get
            {
                return VisualizeSolver_Checkbox.Checked;
            }
            set
            {
                VisualizeSolver_Checkbox.Checked = value;
            }
        }

        public bool DisableStartProcessPoint
        {
            get
            {
                return !CheckBox_EnableStartProcessPoint.Checked;
            }

            set
            {
                CheckBox_EnableStartProcessPoint.Checked = !value;
            }
        }

        public bool DisableEndProcessPoint
        {
            get
            {
                return !CheckBox_EnableEndProcessPoint.Checked;
            }

            set
            {
                CheckBox_EnableEndProcessPoint.Checked = !value;
            }
        }

        public bool DisablePointBoarder
        {
            get
            {
                return !CheckBox_DisablePointBorder.Checked;
            }

            set
            {
                CheckBox_DisablePointBorder.Checked = !value;
            }
        }

        public int ProccessLineThickness
        {
            get
            {
                return CheckBox_ProccessLineThickness.Checked ? 1 : -1;
            }

            set
            {
                CheckBox_ProccessLineThickness.Checked = value > 0;
            }
        }

        public bool DisableLabelStartProcessPoint
        {
            get
            {
                return !checkBox_EnableProcessStartPointLabel.Checked;
            }

            set
            {
                checkBox_EnableProcessStartPointLabel.Checked = !value;
            }
        }

        public bool DisableLabelEndProcessPoint
        {
            get
            {
                return !checkBox_EnableProcessEndPointLabel.Checked;
            }

            set
            {
                checkBox_EnableProcessEndPointLabel.Checked = !value;
            }
        }

        public bool DisableLabelProcess
        {
            get
            {
                return !CheckBox_EnableProcessLabel.Checked;
            }

            set
            {
                CheckBox_EnableProcessLabel.Checked = !value;
            }
        }

        public System.Drawing.Color PointBoarderColor
        {
            get
            {
                return Button_PointBorderColor.BackColor == initialColor ? System.Drawing.Color.Empty : Button_PointBorderColor.BackColor;
            }

            set
            {
                Button_PointBorderColor.BackColor = value;
            }
        }

        public System.Drawing.Color PointColor
        {
            get
            {
                return Button_PointColor.BackColor == initialColor ? System.Drawing.Color.Empty : Button_PointColor.BackColor;
            }

            set
            {
                Button_PointColor.BackColor = value;
            }
        }

        public bool DisablePoint
        {
            get
            {
                return !CheckBox_DisablePoint.Checked;
            }

            set
            {
                CheckBox_DisablePoint.Checked = !value;
            }
        }

        public bool DisableCoolingAuxiliaryProcesses
        {
            get
            {
                return !CheckBox_EnableCoolingAuxiliaryProcesses.Checked;
            }

            set
            {
                CheckBox_EnableCoolingAuxiliaryProcesses.Checked = !value;
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
            if (PartialVapourPressure.ToString() != double.NaN.ToString())
                mollierControlSettings.PartialVapourPressure_Interval = PartialVapourPressure;
            if(Density_Interval.ToString() != double.NaN.ToString())
                mollierControlSettings.Density_Interval = Density_Interval;
            if (Enthalpy_Interval.ToString() != double.NaN.ToString())
                mollierControlSettings.Enthalpy_Interval = Enthalpy_Interval;
            if (SpecificVolume_Interval.ToString() != double.NaN.ToString())
                mollierControlSettings.SpecificVolume_Interval = SpecificVolume_Interval;
            if (WetBulbTemperature_Interval.ToString() != double.NaN.ToString())
                mollierControlSettings.WetBulbTemperature_Interval = WetBulbTemperature_Interval;

            mollierControlSettings.DisableUnits = DisableUnits;
            mollierControlSettings.DisableLabels = DisableLabels;
            mollierControlSettings.VisualizeSolver = VisualizeSolver;

            mollierControlSettings.DisableStartProcessPoint = DisableStartProcessPoint;
            mollierControlSettings.DisableEndProcessPoint = DisableEndProcessPoint;
            mollierControlSettings.DisablePointBoarder = DisablePointBoarder;
            mollierControlSettings.ProccessLineThickness = ProccessLineThickness;


            mollierControlSettings.DisableLabelStartProcessPoint = DisableLabelStartProcessPoint;
            mollierControlSettings.DisableLabelEndProcessPoint = DisableLabelEndProcessPoint;
            mollierControlSettings.DisableLabelProcess = DisableLabelProcess;
            mollierControlSettings.PointBorderColor = PointBoarderColor;
            mollierControlSettings.PointColor = PointColor;
            mollierControlSettings.DisablePoint = DisablePoint;
            mollierControlSettings.DisableCoolingAuxiliaryProcesses = DisableCoolingAuxiliaryProcesses;

            VisibilitySettings visibilitySettings = mollierControlSettings.VisibilitySettings;
            if(visibilitySettings == null)
            {
                visibilitySettings = new VisibilitySettings();
            }

            List<IVisibilitySetting> visibilitySettingsList = new List<IVisibilitySetting>();
            foreach(Control control in FlowLayoutPanel_BuiltInVisibilitySettings.Controls)
            {
                BuiltInVisibilitySettingControl builtInVisibilitySettingControl = control as BuiltInVisibilitySettingControl;
                if(builtInVisibilitySettingControl  == null)
                {
                    continue;
                }

                visibilitySettingsList.Add(builtInVisibilitySettingControl.BuiltInVisibilitySetting);
            }
            if (CheckBox_GradientPoint.Checked)
            {
                PointGradientVisibilitySetting pointGradientVisibilitySetting = new PointGradientVisibilitySetting(Button_LowIntensityColor.BackColor, Button_HighIntensityColor.BackColor);
                visibilitySettingsList.Add(pointGradientVisibilitySetting);
            }

            visibilitySettings.SetVisibilitySettings("User", visibilitySettingsList);
            mollierControlSettings.DefaultTemplateName = "User";

            mollierControlSettings.VisibilitySettings = visibilitySettings;

            mollierControl.MollierControlSettings = mollierControlSettings;

            ApplyClicked.Invoke(this, EventArgs.Empty);
        }

        private void Button_LowIntensityColor_Click(object sender, EventArgs e)
        {
            using(ColorDialog colorDialog = new ColorDialog())
            {
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

        private void Button_PointColor_Click(object sender, EventArgs e)
        {
            using (ColorDialog colorDialog = new ColorDialog())
            {
                if (colorDialog.ShowDialog() != DialogResult.OK)
                {
                    return;
                }
                Button_PointColor.BackColor = colorDialog.Color;
            }
        }

        private void Button_PointBorderColor_Click(object sender, EventArgs e)
        {
            using (ColorDialog colorDialog = new ColorDialog())
            {
                if (colorDialog.ShowDialog() != DialogResult.OK)
                {
                    return;
                }
                Button_PointBorderColor.BackColor = colorDialog.Color;
            }
        }
    }
}
