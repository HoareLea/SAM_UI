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
        public double Elevation { get; set; } = 0;
        public double HumidityRatio_Max { get; set; } = 35;
        public double HumidityRatio_Min { get; set; } = 0;
        public double HumidityRatio_Interval { get; set; } = 5;
        public double Temperature_Max { get; set; } = 50;
        public double Temperature_Min { get; set; } = -20;
        public double Temperature_Interval { get; set; } = 5;
        public double P_w_Interval { get; set; } = 1;
        public bool Density_line { get; set; } = true;
        public bool Enthalpy_line { get; set; } = true;
        public bool SpecificVolume_line { get; set; } = true;
        public bool WetBulbTemperature_line { get; set; } = true;
        public ChartType ChartType { get; set; } = ChartType.Mollier;
        public string Color { get; set; } = "default";
        public bool DisableUnits { get; set; } = false;
        public bool DisableLabels { get; set; } = false;
        public bool GradientPoint { get; set; } = false;
        public bool FindPoint { get; set; } = false;
        public PointGradientVisibilitySetting GradientColors { get; set; } = new PointGradientVisibilitySetting(System.Drawing.Color.Red, System.Drawing.Color.Blue);

        public VisibilitySettings VisibilitySettings { get; set; } = Query.DefaultVisibilitySettings();

        public MollierControlSettings()
        {

        }

        public MollierControlSettings(MollierControlSettings mollierControlSettings)
        {
            Pressure = mollierControlSettings.Pressure;
            Elevation = mollierControlSettings.Elevation;
            HumidityRatio_Max = mollierControlSettings.HumidityRatio_Max;
            HumidityRatio_Min = mollierControlSettings.HumidityRatio_Min;
            HumidityRatio_Interval = mollierControlSettings.HumidityRatio_Interval;
            Temperature_Max = mollierControlSettings.Temperature_Max;
            Temperature_Min = mollierControlSettings.Temperature_Min;
            Temperature_Interval = mollierControlSettings.Temperature_Interval;
            P_w_Interval = mollierControlSettings.P_w_Interval;
            Density_line = mollierControlSettings.Density_line;
            Enthalpy_line = mollierControlSettings.Enthalpy_line;
            SpecificVolume_line = mollierControlSettings.SpecificVolume_line;
            WetBulbTemperature_line = mollierControlSettings.WetBulbTemperature_line;
            ChartType = mollierControlSettings.ChartType;
            Color = mollierControlSettings.Color;
            VisibilitySettings = new VisibilitySettings(mollierControlSettings.VisibilitySettings);

            DisableUnits = mollierControlSettings.DisableUnits;
            DisableLabels = mollierControlSettings.DisableLabels;
            GradientColors = mollierControlSettings.GradientColors;
            GradientPoint = mollierControlSettings.GradientPoint;
            FindPoint = mollierControlSettings.FindPoint;

        }

        public MollierControlSettings(JObject jObject)
        {
            FromJObject(jObject);
        }

        //loading from file
        public bool FromJObject(JObject jObject)
        {
            if (jObject.ContainsKey("Pressure"))
            {
                Pressure = jObject.Value<double>("Pressure");
            }
            if (jObject.ContainsKey("Elevation"))
            {
                Elevation = jObject.Value<double>("Elevation");
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
            if (jObject.ContainsKey("P_w_Interval"))
            {
                P_w_Interval = jObject.Value<double>("P_w_Interval");
            }
            if (jObject.ContainsKey("Density_line"))
            {
                Density_line = jObject.Value<bool>("Density_line");
            }
            if (jObject.ContainsKey("Enthalpy_line"))
            {
                Enthalpy_line = jObject.Value<bool>("Enthalpy_line");
            }
            if (jObject.ContainsKey("SpecificVolume_line"))
            {
                SpecificVolume_line = jObject.Value<bool>("SpecificVolume_line");
            }
            if (jObject.ContainsKey("WetBulbTemperature_line"))
            {
                WetBulbTemperature_line = jObject.Value<bool>("WetBulbTemperature_line");
            }
            if (jObject.ContainsKey("Color"))
            {
                Color = jObject.Value<string>("Color");
            }
            if (jObject.ContainsKey("ChartType"))
            {
                if (Enum.TryParse(jObject.Value<string>("ChartType"), out ChartType chartType))
                {
                    ChartType = chartType;
                }
            }
            if (jObject.ContainsKey("VisibilitySettings"))
            {
                JObject jObject_VisibilitySettings = jObject.Value<JObject>("VisibilitySettings");
                if (jObject_VisibilitySettings != null)
                {
                    VisibilitySettings = new VisibilitySettings(jObject_VisibilitySettings);
                }
            }
            if (jObject.ContainsKey("DisableUnits"))
            {
                DisableUnits = jObject.Value<bool>("DisableUnits");
            }
            if (jObject.ContainsKey("DisableLabels"))
            {
                DisableLabels = jObject.Value<bool>("DisableLabels");
            }

            return true;
        }

        //saving to file
        public JObject ToJObject()
        {
            JObject result = new JObject();
            result.Add("_type", Core.Query.FullTypeName(this));

            if(!double.IsNaN(Pressure))
            {
                result.Add("Pressure", Pressure);
            }
            if (!double.IsNaN(Elevation))
            {
                result.Add("Elevation", Elevation);
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
            if (!double.IsNaN(P_w_Interval))
            {
                result.Add("P_w_Interval", P_w_Interval);
            }
            result.Add("Density_line", Density_line);
            result.Add("Enthalpy_line", Enthalpy_line);
            result.Add("SpecificVolume_line", SpecificVolume_line);
            result.Add("WetBulbTemperature_line", WetBulbTemperature_line);
            result.Add("Color", Color);
            result.Add("DisableUnits", DisableUnits);
            result.Add("DisableLabels", DisableLabels);
            result.Add("ChartType", ChartType.ToString());
            if(VisibilitySettings != null)
            {
                result.Add("VisibilitySettings", VisibilitySettings.ToJObject());
            }
            
            //result.Add("GradientColors", GradientColors.ToJObject()); TODO after change saving GradientColor

            return result;
        }
    }
}
