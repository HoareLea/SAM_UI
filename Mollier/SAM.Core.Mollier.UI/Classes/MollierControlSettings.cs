using Newtonsoft.Json.Linq;
using System;

namespace SAM.Core.Mollier.UI
{
    public class MollierControlSettings : IJSAMObject
    {
        public double Pressure { get; set; } = Standard.Pressure;
        public double Elevation { get; set; } = 0; //TODO: [MACIEK] Remove elevation or Pressure (one value is calculated from another)
        public double HumidityRatio_Max { get; set; } = Default.HumidityRatio_Max; //TODO: [MACIEK] Update Units Humidity Ratio [kg_waterVapor/kg_dryAir]
        public double HumidityRatio_Min { get; set; } = Default.HumidityRatio_Min; //TODO: [MACIEK] Update Units Humidity Ratio [kg_waterVapor/kg_dryAir]
        public double HumidityRatio_Interval { get; set; } = Default.HumidityRatio_Interval; //TODO: [MACIEK] Update Units Humidity Ratio [kg_waterVapor/kg_dryAir]
        public double Temperature_Max { get; set; } = Default.DryBulbTemperature_Max;
        public double Temperature_Min { get; set; } = Default.DryBulbTemperature_Min;
        public double Temperature_Interval { get; set; } = Default.DryBulbTemperature_Interval;
        public double PartialVapourPressure { get; set; } = 0.5; //TODO: [MACIEK] rename to CodeName PartialVapourPressure, use SI units [Pa]
       
        public bool Density_line { get; set; } = true;
        public bool Enthalpy_line { get; set; } = true;
        public bool SpecificVolume_line { get; set; } = true;
        public bool WetBulbTemperature_line { get; set; } = true;
        public ChartType ChartType { get; set; } = ChartType.Mollier;
        public string DefaultTemplateName { get; set; } = "default";
        public bool DisableUnits { get; set; } = false;
        public bool DisableLabels { get; set; } = false;
        public bool GradientPoint { get; set; } = false;
        public bool FindPoint { get; set; } = false;
        public double Percent { get; set; } = 0.4; //TODO: [MACIEK] Adjust value -> Percent 1-100% or factor 0-1
        public string FindPointType { get; set; } = "Enthalpy";
        public bool DivisionArea { get; set; } = false;
        public bool DivisionAreaLabels { get; set; } = true;

        public double Density_Min { get; set; } = Default.Density_Min;
        public double Density_Max { get; set; } = Default.Density_Max;
        public double Density_Interval { get; set; } = Default.Density_Interval;

        public double Enthalpy_Min { get; set; } = -20000; //TODO: [MACIEK] Implement Enthalpy, use SI units [J/kg]
        public double Enthalpy_Max { get; set; } = 140000; //TODO: [MACIEK] Implement Enthalpy, use SI units [J/kg]
        public double Enthalpy_Interval { get; set; } = 1000; //TODO: [MACIEK] Implement Enthalpy, use SI units [J/kg]

        public double SpecificVolume_Min { get; set; } = Default.SpecificVolume_Min;
        public double SpecificVolume_Max { get; set; } = Default.SpecificVolume_Max;
        public double SpecificVolume_Interval { get; set; } = Default.SpecificVolume_Interval;

        public double WetBulbTemperature_Min { get; set; } = Default.WetBulbTemperature_Min;
        public double WetBulbTemperature_Max { get; set; } = Default.WetBulbTemperature_Max;
        public double WetBulbTemperature_Interval { get; set; } = Default.WetBulbTemperature_Interval;
        public PointGradientVisibilitySetting PointGradientVisibilitySetting { get; set; } = new PointGradientVisibilitySetting(System.Drawing.Color.Red, System.Drawing.Color.Blue);

        public VisibilitySettings VisibilitySettings { get; set; } = Query.DefaultVisibilitySettings();

        public MollierControlSettings()
        {

        }

        public MollierControlSettings(MollierControlSettings mollierControlSettings)
        {
            if(mollierControlSettings != null)
            {
                Pressure = mollierControlSettings.Pressure;
                Elevation = mollierControlSettings.Elevation;
                HumidityRatio_Max = mollierControlSettings.HumidityRatio_Max;
                HumidityRatio_Min = mollierControlSettings.HumidityRatio_Min;
                HumidityRatio_Interval = mollierControlSettings.HumidityRatio_Interval;
                Temperature_Max = mollierControlSettings.Temperature_Max;
                Temperature_Min = mollierControlSettings.Temperature_Min;
                Temperature_Interval = mollierControlSettings.Temperature_Interval;
                PartialVapourPressure = mollierControlSettings.PartialVapourPressure;
                Density_line = mollierControlSettings.Density_line;
                Enthalpy_line = mollierControlSettings.Enthalpy_line;
                SpecificVolume_line = mollierControlSettings.SpecificVolume_line;
                WetBulbTemperature_line = mollierControlSettings.WetBulbTemperature_line;
                ChartType = mollierControlSettings.ChartType;
                DefaultTemplateName = mollierControlSettings.DefaultTemplateName;

                VisibilitySettings visibilitySettings = mollierControlSettings.VisibilitySettings;
                VisibilitySettings = visibilitySettings == null ? null : new VisibilitySettings(visibilitySettings);

                DisableUnits = mollierControlSettings.DisableUnits;
                DisableLabels = mollierControlSettings.DisableLabels;

                PointGradientVisibilitySetting pointGradientVisibilitySetting = mollierControlSettings.PointGradientVisibilitySetting;
                PointGradientVisibilitySetting = pointGradientVisibilitySetting == null ? null : pointGradientVisibilitySetting;

                GradientPoint = mollierControlSettings.GradientPoint;
                FindPoint = mollierControlSettings.FindPoint;
                Percent = mollierControlSettings.Percent;
                FindPointType = mollierControlSettings.FindPointType;
                DivisionArea = mollierControlSettings.DivisionArea;
                DivisionAreaLabels = mollierControlSettings.DivisionAreaLabels;

                Density_Max = mollierControlSettings.Density_Max;
                Density_Min = mollierControlSettings.Density_Min;
                Density_Interval = mollierControlSettings.Density_Interval;

                Enthalpy_Max = mollierControlSettings.Enthalpy_Max;
                Enthalpy_Min = mollierControlSettings.Enthalpy_Min;
                Enthalpy_Interval = mollierControlSettings.Enthalpy_Interval;

                SpecificVolume_Max = mollierControlSettings.SpecificVolume_Max;
                SpecificVolume_Min = mollierControlSettings.SpecificVolume_Min;
                SpecificVolume_Interval = mollierControlSettings.SpecificVolume_Interval;

                WetBulbTemperature_Min = mollierControlSettings.WetBulbTemperature_Min;
                WetBulbTemperature_Max = mollierControlSettings.WetBulbTemperature_Max;
                WetBulbTemperature_Interval = mollierControlSettings.WetBulbTemperature_Interval;
            }
        }

        public MollierControlSettings(JObject jObject)
        {
            FromJObject(jObject);
        }

        public bool IsValid()
        {
            //TODO: [MACIEK] Add Density_Max, Density_Min, Density_Interval, Enthalpy_Min, Enthalpy_Max, Enthalpy_Interval,SpecificVolume_Min, SpecificVolume_Max, SpecificVolume_Interval

            if (double.IsNaN(Pressure) ||
                double.IsInfinity(Pressure) ||
                Pressure == double.MaxValue ||
                Pressure == double.MinValue ||
                double.IsNaN(HumidityRatio_Min) ||
                double.IsInfinity(HumidityRatio_Min) ||
                HumidityRatio_Min == double.MaxValue ||
                HumidityRatio_Min == double.MinValue ||
                double.IsNaN(HumidityRatio_Max) ||
                double.IsInfinity(HumidityRatio_Max) ||
                HumidityRatio_Max == double.MaxValue ||
                HumidityRatio_Max == double.MinValue ||
                double.IsNaN(HumidityRatio_Interval) ||
                double.IsInfinity(HumidityRatio_Interval) ||
                HumidityRatio_Interval == double.MaxValue ||
                HumidityRatio_Interval == double.MinValue ||
                double.IsNaN(Temperature_Min) ||
                double.IsInfinity(Temperature_Min) ||
                Temperature_Min == double.MaxValue ||
                Temperature_Min == double.MinValue ||
                double.IsNaN(Temperature_Max) ||
                double.IsInfinity(Temperature_Max) ||
                Temperature_Max == double.MaxValue ||
                Temperature_Max == double.MinValue ||
                double.IsNaN(Temperature_Interval) ||
                double.IsInfinity(Temperature_Interval) ||
                Temperature_Interval == double.MaxValue ||
                Temperature_Interval == double.MinValue)
            {
                return false;
            }

            return true;
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
            if (jObject.ContainsKey("PartialVapourPressure_Interval"))
            {
                PartialVapourPressure = jObject.Value<double>("PartialVapourPressure_Interval");
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
                DefaultTemplateName = jObject.Value<string>("Color");
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

            if (jObject.ContainsKey("PointGradientVisibilitySetting"))
            {
                JObject jObject_PointGradientVisibilitySetting = jObject.Value<JObject>("PointGradientVisibilitySetting");
                if (jObject_PointGradientVisibilitySetting != null)
                {
                    PointGradientVisibilitySetting = new PointGradientVisibilitySetting(jObject_PointGradientVisibilitySetting);
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

            //Enthalpy
            if (jObject.ContainsKey("Enthalpy_Max"))
            {
                Enthalpy_Max = jObject.Value<double>("Enthalpy_Max");
            }
            if (jObject.ContainsKey("Enthalpy_Min"))
            {
                Enthalpy_Min = jObject.Value<double>("Enthalpy_Min");
            }
            if (jObject.ContainsKey("Enthalpy_Interval"))
            {
                Enthalpy_Interval = jObject.Value<double>("Enthalpy_Interval");
            }

            //Density
            if (jObject.ContainsKey("Density_Max"))
            {
                Density_Max = jObject.Value<double>("Density_Max");
            }
            if (jObject.ContainsKey("Density_Min"))
            {
                Density_Min = jObject.Value<double>("Density_Min");
            }
            if (jObject.ContainsKey("Density_Interval"))
            {
                Density_Interval = jObject.Value<double>("Density_Interval");
            }

            //SpecificVolume
            if (jObject.ContainsKey("SpecificVolume_Max"))
            {
                SpecificVolume_Max = jObject.Value<double>("SpecificVolume_Max");
            }
            if (jObject.ContainsKey("SpecificVolume_Min"))
            {
                SpecificVolume_Min = jObject.Value<double>("SpecificVolume_Min");
            }
            if (jObject.ContainsKey("SpecificVolume_Interval"))
            {
                SpecificVolume_Interval = jObject.Value<double>("SpecificVolume_Interval");
            }

            return true;
        }

        //saving to file
        public JObject ToJObject()
        {
            JObject result = new JObject();
            result.Add("_type", Core.Query.FullTypeName(this));

            if (!double.IsNaN(Pressure))
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
            if (!double.IsNaN(PartialVapourPressure))
            {
                result.Add("PartialVapourPressure_Interval", PartialVapourPressure);
            }
            result.Add("Density_line", Density_line);
            result.Add("Enthalpy_line", Enthalpy_line);
            result.Add("SpecificVolume_line", SpecificVolume_line);
            result.Add("WetBulbTemperature_line", WetBulbTemperature_line);
            result.Add("Color", DefaultTemplateName);
            result.Add("DisableUnits", DisableUnits);
            result.Add("DisableLabels", DisableLabels);
            result.Add("ChartType", ChartType.ToString());
            if (VisibilitySettings != null)
            {
                result.Add("VisibilitySettings", VisibilitySettings.ToJObject());
            }

            if(PointGradientVisibilitySetting != null)
            {
                result.Add("PointGradientVisibilitySetting", PointGradientVisibilitySetting.ToJObject());
            }

            //Density
            if (!double.IsNaN(Density_Max))
            {
                result.Add("Density_Max", Density_Max);
            }
            if (!double.IsNaN(Density_Min))
            {
                result.Add("Density_Min", Density_Min);
            }
            if (!double.IsNaN(Density_Interval))
            {
                result.Add("Density_Interval", Density_Interval);
            }

            //Enthalpy
            if (!double.IsNaN(Enthalpy_Max))
            {
                result.Add("Enthalpy_Max", Enthalpy_Max);
            }
            if (!double.IsNaN(Enthalpy_Min))
            {
                result.Add("Enthalpy_Min", Enthalpy_Min);
            }
            if (!double.IsNaN(Enthalpy_Interval))
            {
                result.Add("Enthalpy_Interval", Enthalpy_Interval);
            }

            //SpecificVolume
            if (!double.IsNaN(SpecificVolume_Max))
            {
                result.Add("SpecificVolume_Max", SpecificVolume_Max);
            }
            if (!double.IsNaN(SpecificVolume_Min))
            {
                result.Add("SpecificVolume_Min", SpecificVolume_Min);
            }
            if (!double.IsNaN(SpecificVolume_Interval))
            {
                result.Add("SpecificVolume_Interval", SpecificVolume_Interval);
            }

            return result;
        }
    }
}
