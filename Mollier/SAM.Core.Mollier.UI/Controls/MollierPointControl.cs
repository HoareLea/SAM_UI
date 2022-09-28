using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SAM.Core.Mollier.UI.Controls
{
    public partial class MollierPointControl : UserControl
    {
        private List<string> parameterList = new List<string>() { "Dry Bulb Temperature", "Relative Humidity", "Humidity Ratio", "Wet Bulb Temperature", "Dew Point Temperature" };
       

        public MollierPointControl()
        {
            InitializeComponent();
            foreach (string parameter in parameterList)
            {
                firstParameter_ComboBox.Items.Add(parameter);
                secondParameter_ComboBox.Items.Add(parameter);
            }
            firstParameter_ComboBox.Text = "Dry Bulb Temperature";
            secondParameter_ComboBox.Text = "Relative Humidity";
            firstParameter_Value.Text = "40";
            secondParameter_Value.Text = "20";

        }

        public MollierPointControl(MollierPoint mollierPoint, bool pressureEnabled = true)
        {
            InitializeComponent();
            foreach (string parameter in parameterList)
            {
                firstParameter_ComboBox.Items.Add(parameter);
                secondParameter_ComboBox.Items.Add(parameter);
            }
            firstParameter_ComboBox.Text = "Dry Bulb Temperature";
            secondParameter_ComboBox.Text = "Relative Humidity";
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

        private void SetMollierPoint(MollierPoint value)
        {
            if(value == null)
            {
                return;
            }
            double dryBulbTemperature = value.DryBulbTemperature;
            double relativeHumidity = value.RelativeHumidity;
            double humidityRatio = value.HumidityRatio;
            double wetBulbTemperature = value.WetBulbTemperature();
            double dewPointTemperature = value.DewPointTemperature();
            double pressure = value.Pressure;

            pressureTextBox.Text = pressure.ToString();
            switch (firstParameter_ComboBox.SelectedItem)
            {
                case ChartDataType.DryBulbTemperature:
                    firstParameter_Value.Text = dryBulbTemperature.ToString();
                    break;
                case ChartDataType.RelativeHumidity:
                    firstParameter_Value.Text = relativeHumidity.ToString();
                    break;
                case ChartDataType.HumidityRatio:
                    firstParameter_Value.Text = humidityRatio.ToString();
                    break;
                case ChartDataType.DewPointTemperature:
                    firstParameter_Value.Text = dewPointTemperature.ToString();
                    break;
                case ChartDataType.WetBulbTemperature:
                    firstParameter_Value.Text = wetBulbTemperature.ToString();
                    break;
            }
            switch (secondParameter_ComboBox.SelectedItem)
            {
                case ChartDataType.DryBulbTemperature:
                    secondParameter_Value.Text = dryBulbTemperature.ToString();
                    break;
                case ChartDataType.RelativeHumidity:
                    secondParameter_Value.Text = relativeHumidity.ToString();
                    break;
                case ChartDataType.HumidityRatio:
                    secondParameter_Value.Text = humidityRatio.ToString();
                    break;
                case ChartDataType.DewPointTemperature:
                    secondParameter_Value.Text = dewPointTemperature.ToString();
                    break;
                case ChartDataType.WetBulbTemperature:
                    secondParameter_Value.Text = wetBulbTemperature.ToString();
                    break;
            }
        }

        private MollierPoint GetMollierPoint()
        {
            double dryBulbTemperature = double.NaN;
            double relativeHumidity = double.NaN;
            double humidityRatio = double.NaN;
            double wetBulbTemperature = double.NaN;
            double dewPointTemperature = double.NaN;
            double pressure = Standard.Pressure;
            if (firstParameter_ComboBox.Visible)
            {
                switch (firstParameter_ComboBox.Text)
                {
                    case "Dry Bulb Temperature":
                        Core.Query.TryConvert(firstParameter_Value.Text, out dryBulbTemperature);
                        break;
                    case "Relative Humidity":
                        Core.Query.TryConvert(firstParameter_Value.Text, out relativeHumidity);
                        break;
                    case "Humidity Ratio":
                        Core.Query.TryConvert(firstParameter_Value.Text, out humidityRatio);
                        break;
                    case "Dew Point Temperature":
                        Core.Query.TryConvert(firstParameter_Value.Text, out dewPointTemperature);
                        break;
                    case "Wet Bulb Temperature":
                        Core.Query.TryConvert(firstParameter_Value.Text, out wetBulbTemperature);
                        break;
                }
            }
            if (secondParameter_ComboBox.Visible)
            {
                switch (secondParameter_ComboBox.SelectedItem)
                {
                    case "Dry Bulb Temperature":
                        Core.Query.TryConvert(secondParameter_Value.Text, out dryBulbTemperature);
                        break;
                    case "Relative Humidity":
                        Core.Query.TryConvert(secondParameter_Value.Text, out relativeHumidity);
                        break;
                    case "Humidity Ratio":
                        Core.Query.TryConvert(secondParameter_Value.Text, out humidityRatio);
                        break;
                    case "Dew Point Temperature":
                        Core.Query.TryConvert(secondParameter_Value.Text, out dewPointTemperature);
                        break;
                    case "Wet Bulb Temperature":
                        Core.Query.TryConvert(secondParameter_Value.Text, out wetBulbTemperature);
                        break;
                }
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
            firstParameter_Value.Text = String.Empty;
            this.secondParameter_ComboBox.SelectedIndexChanged -= new EventHandler(secondParameter_ComboBox_SelectedIndexChanged);
            string text = secondParameter_ComboBox.Text;
            secondParameter_ComboBox.Items.Clear();
            foreach (string parameter in parameterList)
            {
                if (firstParameter_ComboBox.Text != parameter)
                {
                    secondParameter_ComboBox.Items.Add(parameter);
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
            secondParameter_Value.Text = String.Empty;

            if (secondParameter_ComboBox.Text == "Dew Point Temperature")
            {
                firstParameter_ComboBox.Text = "Dew Point Temperature";
                return;
            }
            this.firstParameter_ComboBox.SelectedIndexChanged -= new EventHandler(firstParameter_ComboBox_SelectedIndexChanged);
            string text = firstParameter_ComboBox.Text;
            firstParameter_ComboBox.Items.Clear();
            foreach(string parameter in parameterList)
            {
                if(secondParameter_ComboBox.Text != parameter)
                {
                    firstParameter_ComboBox.Items.Add(parameter);
                }
            }
            firstParameter_ComboBox.Text = text;
            this.firstParameter_ComboBox.SelectedIndexChanged += new EventHandler(firstParameter_ComboBox_SelectedIndexChanged);
            updateLabel(secondParameter_ComboBox, secondUnitLabel);

        }

        private void firstParameter_Value_TextChanged(object sender, EventArgs e)
        {

        }

        private void secondParameter_Value_TextChanged(object sender, EventArgs e)
        {

        }

        private void valueSecure(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) &&
                (e.KeyChar != '.'))
            {
                e.Handled = true;
            }
            // only allow one decimal point
            if ((e.KeyChar == '.') && ((sender as TextBox).Text.IndexOf('.') > -1))
            {
                e.Handled = true;
            }
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

        private void ChoosePoint_Button_Click(object sender, EventArgs e)
        {
            SelectPointClicked?.Invoke(this, e);
        }
        public event EventHandler SelectPointClicked;
        

    }
}
