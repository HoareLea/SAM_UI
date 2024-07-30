using Newtonsoft.Json.Linq;
using System;
using System.Drawing;

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
        public double PartialVapourPressure_Interval { get; set; } = 0.5; //TODO: [MACIEK] rename to CodeName PartialVapourPressure, use SI units [Pa]

        public bool Density_Line { get; set; } = true;
        public bool Enthalpy_Line { get; set; } = true;
        public bool SpecificVolume_Line { get; set; } = true;
        public bool WetBulbTemperature_Line { get; set; } = true;

        public bool PartialVapourPressure_Axis { get; set; } = true;
        public ChartType ChartType { get; set; } = ChartType.Mollier;
        public string DefaultTemplateName { get; set; } = "default";
        public bool DisableUnits { get; set; } = false;
        public bool DisableLabels { get; set; } = false;
        public bool GradientPoint { get; set; } = false;

        public bool DisableStartProcessPoint { get; set; } = false;
        public bool DisableEndProcessPoint { get; set; } = false;
        public bool DisablePointBoarder { get; set; } = false;
        public int ProccessLineThickness { get; set; } = -1;
        
        public bool DisableLabelStartProcessPoint { get; set; } = false;
        public bool DisableLabelEndProcessPoint { get; set; } = false;
        public bool DisableLabelProcess { get; set; } = false;
        public Color PointBorderColor { get; set; } = Color.Empty;
        public Color PointColor { get; set; } = Color.Empty;
        public bool DisablePoint { get; set; } = false;
        public int PointSize { get; set; } = -1;
        public int PointBorderSize { get; set; } = -1;

        public bool DisableCoolingAuxiliaryProcesses { get; set; } = false;


        public bool VisualizeSolver { get; set; } = false;
        public bool FindPoint { get; set; } = false;
        public double FindPoint_Factor { get; set; } = 0.4; //TODO: percent again
        public ChartDataType FindPointType { get; set; } = ChartDataType.Enthalpy;
        public bool DivisionArea { get; set; } = false;
        public bool DivisionAreaLabels { get; set; } = true;
        public Color UIMollierZoneColor { get; set; } = Color.Red;
        public string UIMollierZoneText { get; set; } = string.Empty;

        public double Density_Min { get; set; } = Default.Density_Min;
        public double Density_Max { get; set; } = Default.Density_Max;
        public double Density_Interval { get; set; } = Default.Density_Interval;

        public double Enthalpy_Min { get; set; } = Default.Enthalpy_Min;
        public double Enthalpy_Max { get; set; } = Default.Enthalpy_Max;
        public double Enthalpy_Interval { get; set; } = Default.Enthalpy_Interval;

        public double SpecificVolume_Min { get; set; } = Default.SpecificVolume_Min;
        public double SpecificVolume_Max { get; set; } = Default.SpecificVolume_Max;
        public double SpecificVolume_Interval { get; set; } = Default.SpecificVolume_Interval;

        public double WetBulbTemperature_Min { get; set; } = Default.WetBulbTemperature_Min;
        public double WetBulbTemperature_Max { get; set; } = Default.WetBulbTemperature_Max;
        public double WetBulbTemperature_Interval { get; set; } = Default.WetBulbTemperature_Interval;

        public int DivisionAreaEnthalpy_Interval { get; set; } = 3;
        public int DivisionAreaRelativeHumidity_Interval { get; set; } = 10;
        public PointGradientVisibilitySetting PointGradientVisibilitySetting { get; set; } = new PointGradientVisibilitySetting(Color.Red, Color.Blue);

        public int MollierWindowWidth { get; set; } = -1;
        public int MollierWindowHeight { get; set; } = -1;
        public int PsychrometricWindowWidth { get; set; } = -1;
        public int PsychrometricWindowHeight { get; set; } = -1;

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
                PartialVapourPressure_Interval = mollierControlSettings.PartialVapourPressure_Interval;
                Density_Line = mollierControlSettings.Density_Line;
                Enthalpy_Line = mollierControlSettings.Enthalpy_Line;
                SpecificVolume_Line = mollierControlSettings.SpecificVolume_Line;
                WetBulbTemperature_Line = mollierControlSettings.WetBulbTemperature_Line;
                ChartType = mollierControlSettings.ChartType;
                DefaultTemplateName = mollierControlSettings.DefaultTemplateName;
                PartialVapourPressure_Axis = mollierControlSettings.PartialVapourPressure_Axis;

                DivisionAreaEnthalpy_Interval = mollierControlSettings.DivisionAreaEnthalpy_Interval;
                DivisionAreaRelativeHumidity_Interval = mollierControlSettings.DivisionAreaRelativeHumidity_Interval;

                VisibilitySettings visibilitySettings = mollierControlSettings.VisibilitySettings;
                VisibilitySettings = visibilitySettings == null ? null : new VisibilitySettings(visibilitySettings);

                VisualizeSolver = mollierControlSettings.VisualizeSolver;
                DisableUnits = mollierControlSettings.DisableUnits;
                DisableLabels = mollierControlSettings.DisableLabels;

                PointGradientVisibilitySetting pointGradientVisibilitySetting = mollierControlSettings.PointGradientVisibilitySetting;
                PointGradientVisibilitySetting = pointGradientVisibilitySetting == null ? null : pointGradientVisibilitySetting;

                GradientPoint = mollierControlSettings.GradientPoint;
                FindPoint = mollierControlSettings.FindPoint;
                FindPoint_Factor = mollierControlSettings.FindPoint_Factor;
                FindPointType = mollierControlSettings.FindPointType;
                DivisionArea = mollierControlSettings.DivisionArea;
                DivisionAreaLabels = mollierControlSettings.DivisionAreaLabels;
                UIMollierZoneColor = mollierControlSettings.UIMollierZoneColor;
                UIMollierZoneText = mollierControlSettings.UIMollierZoneText;

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

                DisableStartProcessPoint = mollierControlSettings.DisableStartProcessPoint;
                DisableEndProcessPoint = mollierControlSettings.DisableEndProcessPoint;
                DisablePointBoarder = mollierControlSettings.DisablePointBoarder;
                ProccessLineThickness = mollierControlSettings.ProccessLineThickness;

                DisableLabelStartProcessPoint = mollierControlSettings.DisableLabelStartProcessPoint;
                DisableLabelEndProcessPoint = mollierControlSettings.DisableLabelEndProcessPoint;
                DisableLabelProcess = mollierControlSettings.DisableLabelProcess;
                PointBorderColor = mollierControlSettings.PointBorderColor;
                PointColor = mollierControlSettings.PointColor;
                DisablePoint = mollierControlSettings.DisablePoint;

                MollierWindowWidth = mollierControlSettings.MollierWindowWidth;
                MollierWindowHeight = mollierControlSettings.MollierWindowHeight;
                PsychrometricWindowWidth = mollierControlSettings.PsychrometricWindowWidth;
                PsychrometricWindowHeight = mollierControlSettings.PsychrometricWindowHeight;

                DisableCoolingAuxiliaryProcesses = mollierControlSettings.DisableCoolingAuxiliaryProcesses;

                PointBorderSize = mollierControlSettings.PointBorderSize;
                PointSize = mollierControlSettings.PointSize;
            }
        }

        public MollierControlSettings(JObject jObject)
        {
            FromJObject(jObject);
        }

        public bool IsValid()
        {
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
                Temperature_Interval == double.MinValue ||
                double.IsNaN(Density_Min) ||
                double.IsInfinity(Density_Min) ||
                Density_Min == double.MaxValue ||
                Density_Min == double.MinValue ||
                double.IsNaN(Density_Max) ||
                double.IsInfinity(Density_Max) ||
                Density_Max == double.MaxValue ||
                Density_Max == double.MinValue ||
                double.IsNaN(Density_Interval) ||
                double.IsInfinity(Density_Interval) ||
                Density_Interval == double.MaxValue ||
                Density_Interval == double.MinValue ||
                double.IsNaN(Enthalpy_Min) ||
                double.IsInfinity(Enthalpy_Min) ||
                Enthalpy_Min == double.MaxValue ||
                Enthalpy_Min == double.MinValue ||
                double.IsNaN(Enthalpy_Max) ||
                double.IsInfinity(Enthalpy_Max) ||
                Enthalpy_Max == double.MaxValue ||
                Enthalpy_Max == double.MinValue ||
                double.IsNaN(Enthalpy_Interval) ||
                double.IsInfinity(Enthalpy_Interval) ||
                Enthalpy_Interval == double.MaxValue ||
                Enthalpy_Interval == double.MinValue ||
                double.IsNaN(SpecificVolume_Min) ||
                double.IsInfinity(SpecificVolume_Min) ||
                SpecificVolume_Min == double.MaxValue ||
                SpecificVolume_Min == double.MinValue ||
                double.IsNaN(SpecificVolume_Max) ||
                double.IsInfinity(SpecificVolume_Max) ||
                SpecificVolume_Max == double.MaxValue ||
                SpecificVolume_Max == double.MinValue ||
                double.IsNaN(SpecificVolume_Interval) ||
                double.IsInfinity(SpecificVolume_Interval) ||
                SpecificVolume_Interval == double.MaxValue ||
                SpecificVolume_Interval == double.MinValue ||
                double.IsNaN(WetBulbTemperature_Min) ||
                double.IsInfinity(WetBulbTemperature_Min) ||
                WetBulbTemperature_Min == double.MaxValue ||
                WetBulbTemperature_Min == double.MinValue ||
                double.IsNaN(WetBulbTemperature_Max) ||
                double.IsInfinity(WetBulbTemperature_Max) ||
                WetBulbTemperature_Max == double.MaxValue ||
                WetBulbTemperature_Max == double.MinValue ||
                double.IsNaN(WetBulbTemperature_Interval) ||
                double.IsInfinity(WetBulbTemperature_Interval) ||
                WetBulbTemperature_Interval == double.MaxValue ||
                WetBulbTemperature_Interval == double.MinValue)
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
                PartialVapourPressure_Interval = jObject.Value<double>("PartialVapourPressure_Interval");
            }
            if (jObject.ContainsKey("Density_Line"))
            {
                Density_Line = jObject.Value<bool>("Density_Line");
            }
            if (jObject.ContainsKey("Enthalpy_Line"))
            {
                Enthalpy_Line = jObject.Value<bool>("Enthalpy_Line");
            }
            if (jObject.ContainsKey("SpecificVolume_Line"))
            {
                SpecificVolume_Line = jObject.Value<bool>("SpecificVolume_Line");
            }
            if (jObject.ContainsKey("WetBulbTemperature_Line"))
            {
                WetBulbTemperature_Line = jObject.Value<bool>("WetBulbTemperature_Line");
            }
            if (jObject.ContainsKey("WetBulbTemperature_Line"))
            {
                PartialVapourPressure_Axis = jObject.Value<bool>("PartialVapourPressure_Axis");
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

            if (jObject.ContainsKey("DivisionAreaEnthalpy_Interval"))
            {
                DivisionAreaEnthalpy_Interval = jObject.Value<int>("DivisionAreaEnthalpy_Interval");
            }
            if (jObject.ContainsKey("DivisionAreaRelativeHumidity_Interval"))
            {
                DivisionAreaRelativeHumidity_Interval = jObject.Value<int>("DivisionAreaRelativeHumidity_Interval");
            }

            if(jObject.ContainsKey("VisualizeSolver"))
            {
                VisualizeSolver = jObject.Value<bool>("VisualizeSolver");
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

            if (jObject.ContainsKey("DisableStartProcessPoint"))
            {
                DisableStartProcessPoint = jObject.Value<bool>("DisableStartProcessPoint");
            }

            if (jObject.ContainsKey("DisableEndProcessPoint"))
            {
                DisableEndProcessPoint = jObject.Value<bool>("DisableEndProcessPoint");
            }

            if (jObject.ContainsKey("DisablePointBoarder"))
            {
                DisablePointBoarder = jObject.Value<bool>("DisablePointBoarder");
            }

            if (jObject.ContainsKey("ProccessLineThickness"))
            {
                ProccessLineThickness = jObject.Value<int>("ProccessLineThickness");
            }

            if (jObject.ContainsKey("DisableLabelStartProcessPoint"))
            {
                DisableLabelStartProcessPoint = jObject.Value<bool>("DisableLabelStartProcessPoint");
            }

            if (jObject.ContainsKey("DisableLabelEndProcessPoint"))
            {
                DisableLabelEndProcessPoint = jObject.Value<bool>("DisableLabelEndProcessPoint");
            }

            if (jObject.ContainsKey("DisableLabelProcess"))
            {
                DisableLabelProcess = jObject.Value<bool>("DisableLabelProcess");
            }

            if (jObject.ContainsKey("PointBoarderColor"))
            {
                PointBorderColor = new SAMColor(jObject.Value<JObject>("PointBoarderColor")).ToColor();
            }

            if (jObject.ContainsKey("PointColor"))
            {
                PointColor = new SAMColor(jObject.Value<JObject>("PointColor")).ToColor();
            }

            if (jObject.ContainsKey("DisablePoint"))
            {
                DisablePoint = jObject.Value<bool>("DisablePoint");
            }

            if (jObject.ContainsKey("DisableCoolingAuxiliaryProcesses"))
            {
                DisableCoolingAuxiliaryProcesses = jObject.Value<bool>("DisableCoolingAuxiliaryProcesses");
            }

            if (jObject.ContainsKey("MollierWindowWidth"))
            {
                MollierWindowWidth = jObject.Value<int>("MollierWindowWidth");
            }

            if (jObject.ContainsKey("MollierWindowHeight"))
            {
                MollierWindowHeight = jObject.Value<int>("MollierWindowHeight");
            }

            if (jObject.ContainsKey("PsychrometricWindowWidth"))
            {
                PsychrometricWindowWidth = jObject.Value<int>("PsychrometricWindowWidth");
            }

            if (jObject.ContainsKey("PsychrometricWindowHeight"))
            {
                PsychrometricWindowHeight = jObject.Value<int>("PsychrometricWindowHeight");
            }

            if (jObject.ContainsKey("PointBorderSize"))
            {
                PointBorderSize = jObject.Value<int>("PointBorderSize");
            }

            if (jObject.ContainsKey("PointSize"))
            {
                PointSize = jObject.Value<int>("PointSize");
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
            if (!double.IsNaN(PartialVapourPressure_Interval))
            {
                result.Add("PartialVapourPressure_Interval", PartialVapourPressure_Interval);
            }
            result.Add("Density_Line", Density_Line);
            result.Add("Enthalpy_Line", Enthalpy_Line);
            result.Add("SpecificVolume_Line", SpecificVolume_Line);
            result.Add("WetBulbTemperature_Line", WetBulbTemperature_Line);
            result.Add("PartialVapourPressure_Axis", PartialVapourPressure_Axis);
            result.Add("Color", DefaultTemplateName);
            result.Add("VisualizeSolver", VisualizeSolver);
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

            result.Add("DivisionAreaEnthalpy_Interval", DivisionAreaEnthalpy_Interval);
            result.Add("DivisionAreaRelativeHumidity_Interval", DivisionAreaRelativeHumidity_Interval);

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

            result.Add("DisableStartProcessPoint", DisableStartProcessPoint);
            result.Add("DisableEndProcessPoint", DisableEndProcessPoint);
            result.Add("DisablePointBoarder", DisablePointBoarder);
            result.Add("ProccessLineThickness", ProccessLineThickness);

            result.Add("DisableLabelStartProcessPoint", DisableLabelStartProcessPoint);
            result.Add("DisableLabelEndProcessPoint", DisableLabelEndProcessPoint);
            result.Add("DisableLabelProcess", DisableLabelProcess);

            result.Add("PointBoarderColor", new SAMColor(PointBorderColor).ToJObject());
            result.Add("PointColor", new SAMColor(PointColor).ToJObject());

            result.Add("DisablePoint", DisablePoint);

            result.Add("DisableCoolingAuxiliaryProcesses", DisableCoolingAuxiliaryProcesses);

            result.Add("MollierWindowWidth", MollierWindowWidth);
            result.Add("MollierWindowHeight", MollierWindowHeight);
            result.Add("PsychrometricWindowWidth", PsychrometricWindowWidth);
            result.Add("PsychrometricWindowHeight", PsychrometricWindowHeight);

            result.Add("PointBorderSize", PointBorderSize);
            result.Add("PointSize", PointSize);

            return result;
        }
    }
}
