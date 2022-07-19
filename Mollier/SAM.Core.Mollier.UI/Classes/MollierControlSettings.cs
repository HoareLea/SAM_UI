using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SAM.Core.Mollier.UI
{
    public class MollierControlSettings : IJSAMObject
    {
        public double Pressure { get; set; } = 101325;
        public double HumidityRatio_Max { get; set; } = 35;
        public double HumidityRatio_Min { get; set; } = 0;
        public double HumidityRatio_Interval { get; set; } = 5;
        public double Temperature_Max { get; set; } = 50;
        public double Temperature_Min { get; set; } = -20;
        public double Temperature_Interval { get; set; } = 5;
        public bool density_line { get; set; } = true;
        public bool enthalpy_line { get; set; } = true;
        public bool specificVolume_line { get; set; } = true;
        public bool wetBulbTemperature_line { get; set; } = true;

        public ChartType ChartType { get; set; } = ChartType.Mollier;

        public VisibilitySettings VisibilitySettings { get; set; } = Query.DefaultVisibilitySettings();

        public MollierControlSettings()
        {

        }

        public MollierControlSettings(MollierControlSettings mollierControlSettings)
        {
            Pressure = mollierControlSettings.Pressure;
            HumidityRatio_Max = mollierControlSettings.HumidityRatio_Max;
            HumidityRatio_Min = mollierControlSettings.HumidityRatio_Min;
            HumidityRatio_Interval = mollierControlSettings.HumidityRatio_Interval;
            Temperature_Max = mollierControlSettings.Temperature_Max;
            Temperature_Min = mollierControlSettings.Temperature_Min;
            Temperature_Interval = mollierControlSettings.Temperature_Interval;
            density_line = mollierControlSettings.density_line;
            enthalpy_line = mollierControlSettings.enthalpy_line;
            specificVolume_line = mollierControlSettings.specificVolume_line;
            wetBulbTemperature_line = mollierControlSettings.wetBulbTemperature_line;
            ChartType = mollierControlSettings.ChartType;

            //TODO: Add missing parameters
        }

        public MollierControlSettings(JObject jObject)
        {
            FromJObject(jObject);
        }

        public bool FromJObject(JObject jObject)
        {
            if(jObject.ContainsKey("Pressure"))
            {
                Pressure = jObject.Value<double>("Pressure");
            }
            if (jObject.ContainsKey("HumidityRatio_Max"))
            {
                HumidityRatio_Max = jObject.Value<double>("HumidityRatio_Max");
            }
            if (jObject.ContainsKey("HumidityRatio_Min"))
            {
                HumidityRatio_Min = jObject.Value<double>("HumidityRatio_Min");
            }
            if (jObject.ContainsKey("HumidityRatio_Interval"))
            {
                HumidityRatio_Interval = jObject.Value<double>("HumidityRatio_Interval");
            }
            if (jObject.ContainsKey("Temperature_Max"))
            {
                Temperature_Max = jObject.Value<double>("Temperature_Max");
            }
            if (jObject.ContainsKey("Temperature_Min"))
            {
                Temperature_Min = jObject.Value<double>("Temperature_Min");
            }
            if (jObject.ContainsKey("Temperature_Interval"))
            {
                Temperature_Interval = jObject.Value<double>("Temperature_Interval");
            }
            if (jObject.ContainsKey("density_line"))
            {
                density_line = jObject.Value<bool>("density_line");
            }
            if (jObject.ContainsKey("enthalpy_line"))
            {
                enthalpy_line = jObject.Value<bool>("enthalpy_line");
            }
            if (jObject.ContainsKey("specificVolume_line"))
            {
                specificVolume_line = jObject.Value<bool>("specificVolume_line");
            }
            if (jObject.ContainsKey("wetBulbTemperature_line"))
            {
                wetBulbTemperature_line = jObject.Value<bool>("wetBulbTemperature_line");
            }

            if (jObject.ContainsKey("ChartType"))
            {
                if(Enum.TryParse(jObject.Value<string>("ChartType"), out ChartType chartType))
                {
                    ChartType = chartType;
                }
            }
            return true;
        }

        public JObject ToJObject()
        {
            JObject result = new JObject();
            result.Add("_type", Core.Query.FullTypeName(this));

            if(!double.IsNaN(Pressure))
            {
                result.Add("Pressure", Pressure);
            }
            if (!double.IsNaN(HumidityRatio_Max))
            {
                result.Add("HumidityRatio_Max", HumidityRatio_Max);
            }
            if (!double.IsNaN(HumidityRatio_Min))
            {
                result.Add("HumidityRatio_Min", HumidityRatio_Min);
            }
            if (!double.IsNaN(HumidityRatio_Interval))
            {
                result.Add("HumidityRatio_Interval", HumidityRatio_Interval);
            }
            if (!double.IsNaN(Temperature_Max))
            {
                result.Add("Temperature_Max", Temperature_Max);
            }
            if (!double.IsNaN(Temperature_Min))
            {
                result.Add("Temperature_Min", Temperature_Min);
            }
            if (!double.IsNaN(Temperature_Interval))
            {
                result.Add("Temperature_Interval", Temperature_Interval);
            }
            result.Add("density_line", density_line);
            result.Add("enthalpy_line", enthalpy_line);
            result.Add("specificVolume_line", specificVolume_line);
            result.Add("wetBulbTemprature_line", wetBulbTemperature_line);
            result.Add("ChartType", ChartType.ToString());


            return result;
        }
    }
}
