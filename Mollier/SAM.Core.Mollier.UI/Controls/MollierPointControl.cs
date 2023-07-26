using SAM.Units;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;

namespace SAM.Core.Mollier.UI.Controls
{
    public partial class MollierPointControl : UserControl
    {
        private List<ChartDataType> chartDataTypes = new List<ChartDataType>() { ChartDataType.DryBulbTemperature, ChartDataType.RelativeHumidity, ChartDataType.HumidityRatio, ChartDataType.WetBulbTemperature, ChartDataType.DewPointTemperature, ChartDataType.Enthalpy};

        public event SelectMollierPointEventHandler SelectMollierPoint;

        public event EventHandler ValueHanged;

        private MollierPoint mollierPoint;

        public MollierPointControl()
        {
            InitializeComponent();
            
            chartDataTypes.ForEach(x => ComboBox_FirstParameter.Items.Add(x.Description()));
            chartDataTypes.ForEach(x => ComboBox_SecondParameter.Items.Add(x.Description()));

            ComboBox_FirstParameter.Text = ChartDataType.DryBulbTemperature.Description();
            ComboBox_SecondParameter.Text = ChartDataType.RelativeHumidity.Description();

            NumberBoxControl_FirstParameter.ValueChanged += NumberBoxControl_ValueChanged;
            NumberBoxControl_SecondParameter.ValueChanged += NumberBoxControl_ValueChanged;
            NumberBoxControl_Pressure.ValueChanged += NumberBoxControl_ValueChanged;

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

            NumberBoxControl_Pressure.Value = mollierPoint.Pressure;

            ChartDataType chartDataType = ChartDataType.Undefined;
            double value = double.NaN;

            chartDataType = Core.Query.Enum<ChartDataType>(ComboBox_FirstParameter.Text);
            value = mollierPoint.Value(chartDataType);
            if(!double.IsNaN(value))
            {
                switch (chartDataType)
                {
                    case ChartDataType.HumidityRatio:
                        value = value * 1000;
                        break;
                }
            }

            NumberBoxControl_FirstParameter.Value = value;

            chartDataType = Core.Query.Enum<ChartDataType>(ComboBox_SecondParameter.Text);
            value = mollierPoint.Value(chartDataType);
            if (!double.IsNaN(value))
            {
                switch (chartDataType)
                {
                    case ChartDataType.HumidityRatio:
                        value = value * 1000;
                        break;
                }
            }
            NumberBoxControl_SecondParameter.Value = value;
        }
         
        private MollierPoint GetMollierPoint()
        {
            double dryBulbTemperature = double.NaN;
            double relativeHumidity = double.NaN;
            double humidityRatio = double.NaN;
            double wetBulbTemperature = double.NaN;
            double dewPointTemperature = double.NaN;
            double pressure = Standard.Pressure;
            double enthalpy = double.NaN;

            ChartDataType chartDataType = Core.Query.Enum<ChartDataType>(ComboBox_FirstParameter.Text);
            switch (chartDataType)
            {
                case ChartDataType.DryBulbTemperature:
                    dryBulbTemperature = NumberBoxControl_FirstParameter.Value;
                    break;
                case ChartDataType.RelativeHumidity:
                    relativeHumidity = NumberBoxControl_FirstParameter.Value;
                    break;
                case ChartDataType.HumidityRatio:
                    humidityRatio = NumberBoxControl_FirstParameter.Value;
                    break;
                case ChartDataType.DewPointTemperature:
                    dewPointTemperature = NumberBoxControl_FirstParameter.Value;
                    break;
                case ChartDataType.WetBulbTemperature:
                    wetBulbTemperature = NumberBoxControl_FirstParameter.Value;
                    break;
                case ChartDataType.Enthalpy:
                    enthalpy = NumberBoxControl_FirstParameter.Value;
                    break;
            }

            chartDataType = Core.Query.Enum<ChartDataType>(ComboBox_SecondParameter.Text);
            switch (chartDataType)
            {
                case ChartDataType.DryBulbTemperature:
                    dryBulbTemperature = NumberBoxControl_SecondParameter.Value; 
                    break;
                case ChartDataType.RelativeHumidity:
                    relativeHumidity = NumberBoxControl_SecondParameter.Value;
                    break;
                case ChartDataType.HumidityRatio:
                    humidityRatio = NumberBoxControl_SecondParameter.Value;
                    break;
                case ChartDataType.DewPointTemperature:
                    dewPointTemperature = NumberBoxControl_SecondParameter.Value;
                    break;
                case ChartDataType.WetBulbTemperature:
                    wetBulbTemperature = NumberBoxControl_SecondParameter.Value;
                    break;
                case ChartDataType.Enthalpy:
                    enthalpy = NumberBoxControl_SecondParameter.Value;
                    break;
            }

            pressure = NumberBoxControl_Pressure.Value;
            
            if (!double.IsNaN(humidityRatio))
            {
                humidityRatio /= 1000;
            }

            if (!double.IsNaN(enthalpy))
            {
                enthalpy *= 1000;
            }

            if(!double.IsNaN(dewPointTemperature))
            {
                humidityRatio = double.NaN;
                enthalpy = double.NaN;
                dryBulbTemperature = double.NaN;
                relativeHumidity = double.NaN;
            }


            MollierPoint result = Query.MollierPointByTwoParameters(pressure: pressure, humidityRatio: humidityRatio, dryBulbTemperature: dryBulbTemperature, relativeHumidity: relativeHumidity, wetBulbTemperature: wetBulbTemperature, dewPointTemperature: dewPointTemperature, enthalpy: enthalpy);

            return result;
        }

        private void ComboBox_FirstParameter_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBox_SecondParameter.SelectedIndexChanged -= new EventHandler(ComboBox_SecondParameter_SelectedIndexChanged);
            string text = ComboBox_SecondParameter.Text;    
            ComboBox_SecondParameter.Items.Clear();
            foreach (ChartDataType chartDataType in chartDataTypes)
            {
                if (Core.Query.Enum<ChartDataType>(ComboBox_FirstParameter.Text) != chartDataType)
                {
                    ComboBox_SecondParameter.Items.Add(chartDataType.Description());
                }
            }
            ComboBox_SecondParameter.Text = text;
            ComboBox_SecondParameter.SelectedIndexChanged += new EventHandler(ComboBox_SecondParameter_SelectedIndexChanged);

            if(ComboBox_FirstParameter.Text == "Dew Point Temperature")
            {
                ComboBox_SecondParameter.Visible = false;
                NumberBoxControl_SecondParameter.Visible = false;
                Label_SecondParameterUnit.Visible = false;
                ComboBox_SecondParameter.Text = ChartDataType.DewPointTemperature.Description();
            }
            else
            {
                ComboBox_SecondParameter.Visible = true;
                NumberBoxControl_SecondParameter.Visible = true;
                Label_SecondParameterUnit.Visible = true;

            }
            
            if(ComboBox_FirstParameter.Text == ChartDataType.Enthalpy.Description())
            {
                ComboBox_SecondParameter.Text = ChartDataType.HumidityRatio.Description();
            }

            UnitType unitType = Query.DefaultUnitType(ComboBox_FirstParameter.Text.Enum<ChartDataType>());

            Label_FirstParameterUnit.Text = unitType.Abbreviation();
            NumberBoxControl_FirstParameter.Tolerance = Units.Query.DefaultTolerance(unitType);

            SetMollierPoint(mollierPoint);
            mollierPoint = null;
        }

        private void ComboBox_FirstParameter_Click(object sender, EventArgs e)
        {
            mollierPoint = GetMollierPoint();
        }

        private void ComboBox_SecondParameter_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ComboBox_SecondParameter.Text == ChartDataType.DewPointTemperature.Description())
            {
                ComboBox_FirstParameter.Text = ChartDataType.DewPointTemperature.Description();
                return;
            }
            ComboBox_FirstParameter.SelectedIndexChanged -= new EventHandler(ComboBox_FirstParameter_SelectedIndexChanged);
            string text = ComboBox_FirstParameter.Text;
            ComboBox_FirstParameter.Items.Clear();
            foreach(ChartDataType chartDataType in chartDataTypes)
            {
                if(Core.Query.Enum<ChartDataType>(ComboBox_SecondParameter.Text) != chartDataType)
                {
                    ComboBox_FirstParameter.Items.Add(chartDataType.Description());
                }
            }

            ComboBox_FirstParameter.Text = text;
            ComboBox_FirstParameter.SelectedIndexChanged += new EventHandler(ComboBox_FirstParameter_SelectedIndexChanged);

            if (ComboBox_SecondParameter.Text == ChartDataType.Enthalpy.Description())
            {
                ComboBox_FirstParameter.Text = ChartDataType.HumidityRatio.Description();
            }

            UnitType unitType = Query.DefaultUnitType(ComboBox_SecondParameter.Text.Enum<ChartDataType>());

            Label_SecondParameterUnit.Text = unitType.Abbreviation();
            NumberBoxControl_SecondParameter.Tolerance = Units.Query.DefaultTolerance(unitType);

            SetMollierPoint(mollierPoint);
            mollierPoint = null;

        }

        private void ComboBox_SecondParameter_Click(object sender, EventArgs e)
        {
            mollierPoint = GetMollierPoint();
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

        private void NumberBoxControl_ValueChanged(object sender, EventArgs e)
        {
            ValueHanged?.Invoke(this, e);
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
                NumberBoxControl_Pressure.Visible = value;
                Label_PressureUnit.Visible = value;
            }
        }

        public bool PressureEnabled
        {
            get
            {
                return NumberBoxControl_Pressure.Enabled;
            }

            set
            {
                NumberBoxControl_Pressure.Enabled = value;
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
                return NumberBoxControl_Pressure.Value;
            }
            set
            {
                NumberBoxControl_Pressure.Value = value;
            }
        }

    }
}
