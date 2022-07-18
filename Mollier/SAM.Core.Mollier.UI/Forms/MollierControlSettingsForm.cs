using System;
using System.Windows.Forms;

namespace SAM.Core.Mollier.UI
{
    public partial class MollierControlSettingsForm : Form
    {
        private Controls.MollierControl mollierControl;

        public MollierControlSettingsForm()
        {
            InitializeComponent();
        }

        public MollierControlSettingsForm(Controls.MollierControl mollierControl)
        {
            InitializeComponent();
           this.mollierControl = mollierControl;

            HumidityRatio_Max = this.mollierControl.HumidityRatio_Max;
            HumidityRatio_Min = this.mollierControl.HumidityRatio_Min;
            HumidityRatio_Interval = this.mollierControl.HumidityRatio_Interval;
            Temperature_Max = this.mollierControl.Temperature_Max;
            Temperature_Min = this.mollierControl.Temperature_Min;
            Temperature_Interval = this.mollierControl.Temperature_Interval;
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
                HumidityRatioMaximumValueTextbox.Text = value.ToString();
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
                HumidityRatioMinimumValueTextbox.Text = value.ToString();
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
                TemperatureMaximumValueTextbox.Text = value.ToString();
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
                TemperatureMinimumValueTextbox.Text = value.ToString();
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

        private void Apply()
        {
            if (HumidityRatio_Max.ToString() != double.NaN.ToString())
                mollierControl.HumidityRatio_Max = HumidityRatio_Max;
            if (HumidityRatio_Min.ToString() != double.NaN.ToString())   
                mollierControl.HumidityRatio_Min = HumidityRatio_Min;
            if(HumidityRatio_Interval.ToString() != double.NaN.ToString())
                mollierControl.HumidityRatio_Interval = HumidityRatio_Interval;
            if(Temperature_Min.ToString() != double.NaN.ToString())
                mollierControl.Temperature_Min = Temperature_Min;
            if(Temperature_Max.ToString() != double.NaN.ToString())
                mollierControl.Temperature_Max = Temperature_Max;
            if(Temperature_Interval.ToString() != double.NaN.ToString())
                mollierControl.Temperature_Interval = Temperature_Interval;
        }

        private void Button_ResetChart_Click(object sender, EventArgs e)
        {
            MollierControlSettings mollierControlSettings = new MollierControlSettings();
            mollierControl.SetMollierControlSettings(mollierControlSettings);


            //mollierControl.HumidityRatio_Max = 35;
            //mollierControl.HumidityRatio_Min = 0;
            //mollierControl.HumidityRatio_Interval = 5;
            //mollierControl.Temperature_Max = 50;
            //mollierControl.Temperature_Min = -20;
            //mollierControl.Temperature_Interval = 5;
            Close();
        }
    }
}
