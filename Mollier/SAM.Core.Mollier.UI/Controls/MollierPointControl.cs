using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;

namespace SAM.Core.Mollier.UI.Controls
{
    public partial class MollierPointControl : UserControl
    {
        private List<ChartDataType> chartDataTypes = new List<ChartDataType>() { ChartDataType.DryBulbTemperature, ChartDataType.RelativeHumidity, ChartDataType.HumidityRatio, ChartDataType.WetBulbTemperature, ChartDataType.DewPointTemperature};

        public event SelectMollierPointEventHandler SelectMollierPoint;

        public event EventHandler ValueHanged;

        public MollierPointControl()
        {
            InitializeComponent();
            
            chartDataTypes.ForEach(x => firstParameter_ComboBox.Items.Add(x.Description()));
            chartDataTypes.ForEach(x => secondParameter_ComboBox.Items.Add(x.Description()));

            firstParameter_ComboBox.Text = ChartDataType.DryBulbTemperature.Description();
            secondParameter_ComboBox.Text = ChartDataType.RelativeHumidity.Description();
            firstParameter_Value.Text = "35";
            secondParameter_Value.Text = "50";

        }

        public MollierPointControl(MollierPoint mollierPoint, bool pressureEnabled = true)
        {
            InitializeComponent();
            chartDataTypes.ForEach(x => firstParameter_ComboBox.Items.Add(x.Description()));
            chartDataTypes.ForEach(x => secondParameter_ComboBox.Items.Add(x.Description()));

            firstParameter_ComboBox.Text = ChartDataType.DryBulbTemperature.Description();
            secondParameter_ComboBox.Text = ChartDataType.HumidityRatio.Description();

            SetMollierPoint(mollierPoint);
            pressureTextBox.ReadOnly = pressureEnabled;
        }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public MollierPoint MollierPoint
        {
            get
            {
                return GetMollierPoint();
            }
            set
            {
                SetMollierPoint(value);
            }
        }

        private void SetMollierPoint(MollierPoint mollierPoint)
        {
            if(mollierPoint == null)
            {
                return;
            }

            ChartDataType chartDataType = ChartDataType.Undefined;
            double value = double.NaN;

            chartDataType = Core.Query.Enum<ChartDataType>(firstParameter_ComboBox.Text);
            value = mollierPoint.Value(chartDataType);
            firstParameter_Value.Text = double.IsNaN(value ) ? null : Core.Query.Round(value).ToString();

            chartDataType = Core.Query.Enum<ChartDataType>(secondParameter_ComboBox.Text);
            value = mollierPoint.Value(chartDataType);
            secondParameter_Value.Text = double.IsNaN(value) ? null : Core.Query.Round(value).ToString();
        }

        private MollierPoint GetMollierPoint()
        {
            double dryBulbTemperature = double.NaN;
            double relativeHumidity = double.NaN;
            double humidityRatio = double.NaN;
            double wetBulbTemperature = double.NaN;
            double dewPointTemperature = double.NaN;
            double pressure = Standard.Pressure;
            
            ChartDataType chartDataType = Core.Query.Enum<ChartDataType>(firstParameter_ComboBox.Text);
            switch (chartDataType)
            {
                case ChartDataType.DryBulbTemperature:
                    Core.Query.TryConvert(firstParameter_Value.Text, out dryBulbTemperature);
                    break;
                case ChartDataType.RelativeHumidity:
                    Core.Query.TryConvert(firstParameter_Value.Text, out relativeHumidity);
                    break;
                case ChartDataType.HumidityRatio:
                    Core.Query.TryConvert(firstParameter_Value.Text, out humidityRatio);
                    break;
                case ChartDataType.DewPointTemperature:
                    Core.Query.TryConvert(firstParameter_Value.Text, out dewPointTemperature);
                    break;
                case ChartDataType.WetBulbTemperature:
                    Core.Query.TryConvert(firstParameter_Value.Text, out wetBulbTemperature);
                    break;
            }

            chartDataType = Core.Query.Enum<ChartDataType>(secondParameter_ComboBox.Text);
            switch (chartDataType)
            {
                case ChartDataType.DryBulbTemperature:
                    Core.Query.TryConvert(secondParameter_Value.Text, out dryBulbTemperature);
                    break;
                case ChartDataType.RelativeHumidity:
                    Core.Query.TryConvert(secondParameter_Value.Text, out relativeHumidity);
                    break;
                case ChartDataType.HumidityRatio:
                    Core.Query.TryConvert(secondParameter_Value.Text, out humidityRatio);
                    break;
                case ChartDataType.DewPointTemperature:
                    Core.Query.TryConvert(secondParameter_Value.Text, out dewPointTemperature);
                    break;
                case ChartDataType.WetBulbTemperature:
                    Core.Query.TryConvert(secondParameter_Value.Text, out wetBulbTemperature);
                    break;
            }

            Core.Query.TryConvert(pressureTextBox.Text, out pressure);
            
            if (!double.IsNaN(humidityRatio))
            {
                humidityRatio /= 1000;
            }

            MollierPoint result = Query.MollierPointByTwoParameters(pressure: pressure, humidityRatio: humidityRatio, dryBulbTemperature: dryBulbTemperature, relativeHumidity: relativeHumidity, wetBulbTemperature: wetBulbTemperature, dewPointTemperature: dewPointTemperature);

            return result;
        }

        private void firstParameter_ComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            //firstParameter_Value.Text = String.Empty;
            this.secondParameter_ComboBox.SelectedIndexChanged -= new EventHandler(secondParameter_ComboBox_SelectedIndexChanged);
            string text = secondParameter_ComboBox.Text;
            secondParameter_ComboBox.Items.Clear();
            foreach (ChartDataType chartDataType in chartDataTypes)
            {
                if (Core.Query.Enum<ChartDataType>(firstParameter_ComboBox.Text) != chartDataType)
                {
                    secondParameter_ComboBox.Items.Add(chartDataType.Description());
                }
            }
            secondParameter_ComboBox.Text = text;
            this.secondParameter_ComboBox.SelectedIndexChanged += new EventHandler(secondParameter_ComboBox_SelectedIndexChanged);

            if(firstParameter_ComboBox.Text == "Dew Point Temperature")
            {
                secondParameter_ComboBox.Visible = false;
                secondParameter_Value.Visible = false;
                secondUnitLabel.Visible = false;
                secondParameter_ComboBox.Text = secondParameter_ComboBox.Items[0].ToString();
            }
            else
            {
                secondParameter_ComboBox.Visible = true;
                secondParameter_Value.Visible = true;
                secondUnitLabel.Visible = true;

            }
            updateLabel(firstParameter_ComboBox, firstUnitLabel);
        }

        private void secondParameter_ComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (secondParameter_ComboBox.Text == "Dew Point Temperature")
            {
                firstParameter_ComboBox.Text = "Dew Point Temperature";
                return;
            }
            this.firstParameter_ComboBox.SelectedIndexChanged -= new EventHandler(firstParameter_ComboBox_SelectedIndexChanged);
            string text = firstParameter_ComboBox.Text;
            firstParameter_ComboBox.Items.Clear();
            foreach(ChartDataType chartDataType in chartDataTypes)
            {
                if(Core.Query.Enum<ChartDataType>(secondParameter_ComboBox.Text) != chartDataType)
                {
                    firstParameter_ComboBox.Items.Add(chartDataType.Description());
                }
            }

            firstParameter_ComboBox.Text = text;
            this.firstParameter_ComboBox.SelectedIndexChanged += new EventHandler(firstParameter_ComboBox_SelectedIndexChanged);
            updateLabel(secondParameter_ComboBox, secondUnitLabel);

        }

        private void firstParameter_Value_TextChanged(object sender, EventArgs e)
        {
            ValueHanged?.Invoke(this, e);
        }

        private void secondParameter_Value_TextChanged(object sender, EventArgs e)
        {
            ValueHanged?.Invoke(this, e);
        }

        private void pressureTextBox_TextChanged(object sender, EventArgs e)
        {
            ValueHanged?.Invoke(this, e);
        }

        private void valueSecure(object sender, KeyPressEventArgs e)
        {
            if(!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && (e.KeyChar != '.') && (e.KeyChar != '-'))
            {
                e.Handled = true;
            }
            // only allow one decimal point
            //if ((e.KeyChar == '.') && ((sender as TextBox).Text.IndexOf('.') > -1))
            //{
            //    e.Handled = true;
            //}
        }

        private void firstParameter_Value_KeyPress(object sender, KeyPressEventArgs e)
       {
            valueSecure(sender, e);
        }

        private void secondParameter_Value_KeyPress(object sender, KeyPressEventArgs e)
        {
            valueSecure(sender, e);
        }

        private void pressureTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            valueSecure(sender, e);
        }
        
        private void updateLabel(ComboBox comboBox, Label label)
        {
            switch (comboBox.Text)
            {
                case "Dry Bulb Temperature":
                    label.Text = "°C";
                    break;
                case "Relative Humidity":
                    label.Text = "%";
                    break;
                case "Humidity Ratio":
                    label.Text = "g/kg";
                    break;
                case "Dew Point Temperature":
                    label.Text = "°C";
                    break;
                case "Wet Bulb Temperature":
                    label.Text = "°C";
                    break;
            }
        }

        private void Button_SelectMollierPoint_Click(object sender, EventArgs e)
        {
            SelectMollierPointEventArgs selectMollierPointEventArgs = new SelectMollierPointEventArgs();

            SelectMollierPoint?.Invoke(this, selectMollierPointEventArgs);

            MollierPoint mollierPoint = selectMollierPointEventArgs?.MollierPoint;
            if(mollierPoint == null)
            {
                return;
            }

            MollierPoint = mollierPoint;
        }

        public bool PressureVisible
        {
            get
            {
                return Label_Pressure.Visible;
            }

            set
            {
                Label_Pressure.Visible = value;
                pressureTextBox.Visible = value;
                pressureUnitLabel.Visible = value;
            }
        }

        public bool SelectMollierPointVisible
        {
            get
            {
                return Button_SelectMollierPoint.Visible;
            }
            set
            {
                Button_SelectMollierPoint.Visible = value;
            }

        }
        
        public double Pressure
        {
            get
            {
                Core.Query.TryConvert(pressureTextBox.Text, out double result);
                return result;
            }
            set
            {
                pressureTextBox.Text = value.ToString();
            }
        }
    }
}
