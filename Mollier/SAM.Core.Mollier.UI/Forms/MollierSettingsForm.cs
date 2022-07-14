using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SAM.Core.Mollier.UI
{
    public partial class MollierSettingsForm : Form
    {
        private Controls.MollierControl mollierControl;

        public MollierSettingsForm()
        {
            InitializeComponent();
        }

        public MollierSettingsForm(Controls.MollierControl mollierControl)
        {
            InitializeComponent();
            this.mollierControl = mollierControl;
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
                if (humidityRatio_Min < -20 || humidityRatio_Min > 50)
                {
                    return double.NaN;
                }

                return humidityRatio_Min;
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

                return humidityRatio_Interval;
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
                    return double.NaN;
                }

                return temperature_Max;
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
                    return double.NaN;
                }

                return temperature_Min;
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

                return temperature_Interval;
            }
        }

        private void Apply()
        {
            mollierControl.HumidityRatio_Max = HumidityRatio_Max;
            mollierControl.HumidityRatio_Min = HumidityRatio_Min;
            mollierControl.HumidityRatio_Interval = HumidityRatio_Interval;
            mollierControl.Temperature_Max = Temperature_Max;
            mollierControl.Temperature_Min = Temperature_Min;
            mollierControl.Temperature_Interval = Temperature_Interval;
        }


    }
}
