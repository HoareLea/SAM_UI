using System.Text.Json.Nodes;
using SAM.Core;
using SAM.Weather;
using System.Collections.Generic;

namespace SAM.Analytical.UI.WPF
{
    public class SimulateOptions : IJSAMObject
    {
        public string ProjectName { get; set; } = null;
        public List<string> ZoneCategories { get; set; } = null;
        public WeatherData WeatherData { get; set; } = null;
        public string OutputDirectory { get; set; } = null;
        public bool Simulate { get; set; } = true;
        public SolarCalculationMethod SolarCalculationMethod { get; set; } = SolarCalculationMethod.TAS;
        public bool FullYearSimulation { get; set; } = false;
        public bool UnmetHours { get; set; } = false;
        public bool RoomDataSheets { get; set; } = false;
        public bool CreateSAP { get; set; } = false;
        public bool CreateTPD { get; set; } = false;
        public bool CreateTM59 { get; set; } = false;
        public bool CreatePartL { get; set; } = false;
        public bool UseWidths { get; set; } = false;
        public bool Sizing { get; set; } = true;

        public TextMap TextMap { get; set; } = Analytical.Query.DefaultInternalConditionTextMap_TM59();

        public SimulateOptions()
        {

        }

        public SimulateOptions(SimulateOptions simulateOptions)
        {
            if(simulateOptions != null)
            {
                ProjectName = simulateOptions.ProjectName;
                ZoneCategories = simulateOptions.ZoneCategories == null ? null : simulateOptions.ZoneCategories;
                WeatherData = simulateOptions.WeatherData == null ? null : new WeatherData(simulateOptions.WeatherData);
                OutputDirectory = simulateOptions.OutputDirectory;
                Simulate = simulateOptions.Simulate;
                SolarCalculationMethod = simulateOptions.SolarCalculationMethod;
                FullYearSimulation = simulateOptions.FullYearSimulation;
                UnmetHours = simulateOptions.UnmetHours;
                RoomDataSheets = simulateOptions.RoomDataSheets;
                CreateSAP = simulateOptions.CreateSAP;
                CreateTM59 = simulateOptions.CreateTM59;
                TextMap = simulateOptions.TextMap == null ? null : simulateOptions.TextMap;
                UseWidths = simulateOptions.UseWidths;
                CreateTPD = simulateOptions.CreateTPD;
                Sizing = simulateOptions.Sizing;
                CreatePartL = simulateOptions.CreatePartL;
            }
        }

        public SimulateOptions(JsonObject jObject)
        {
            FromJsonObject(jObject);
        }

        public bool FromJsonObject(JsonObject jObject)
        {
            if(jObject == null)
            {
                return false;
            }

            if (jObject.ContainsKey("ProjectName"))
            {
                ProjectName = jObject["ProjectName"]?.GetValue<string>() ?? null;
            }

            if (jObject.ContainsKey("ZoneCategories"))
            {
                JsonArray jArray = jObject["ZoneCategories"] as JsonArray;
                if(jArray != null)
                {
                    ZoneCategories = new List<string>();
                    foreach(string category in jArray)
                    {
                        ZoneCategories.Add(category);
                    }
                }
            }

            if(jObject.ContainsKey("WeatherData"))
            {
                WeatherData = new WeatherData(jObject["WeatherData"] as JsonObject);
            }

            if(jObject.ContainsKey("OutputDirectory"))
            {
                OutputDirectory = jObject["OutputDirectory"]?.GetValue<string>() ?? null;
            }

            if(jObject.ContainsKey("Simulate"))
            {
                Simulate = jObject["Simulate"]?.GetValue<bool>() ?? default(bool);
            }

            if (jObject.ContainsKey("SolarCalculationMethod"))
            {
                SolarCalculationMethod = Core.Query.Enum<SolarCalculationMethod>(jObject["SolarCalculationMethod"]?.GetValue<string>() ?? null);
            }

            if (jObject.ContainsKey("FullYearSimulation"))
            {
                FullYearSimulation = jObject["FullYearSimulation"]?.GetValue<bool>() ?? default(bool);
            }

            if (jObject.ContainsKey("UnmetHours"))
            {
                UnmetHours = jObject["UnmetHours"]?.GetValue<bool>() ?? default(bool);
            }

            if (jObject.ContainsKey("RoomDataSheets"))
            {
                RoomDataSheets = jObject["RoomDataSheets"]?.GetValue<bool>() ?? default(bool);
            }

            if (jObject.ContainsKey("CreateSAP"))
            {
                CreateSAP = jObject["CreateSAP"]?.GetValue<bool>() ?? default(bool);
            }

            if (jObject.ContainsKey("CreateTM59"))
            {
                CreateTM59 = jObject["CreateTM59"]?.GetValue<bool>() ?? default(bool);
            }

            if (jObject.ContainsKey("TextMap"))
            {
                TextMap = Core.Query.IJSAMObject<TextMap>(jObject["TextMap"] as JsonObject);
            }

            if (jObject.ContainsKey("UseWidths"))
            {
                UseWidths = jObject["UseWidths"]?.GetValue<bool>() ?? default(bool);
            }

            if (jObject.ContainsKey("CreateTPD"))
            {
                CreateTPD = jObject["CreateTPD"]?.GetValue<bool>() ?? default(bool);
            }

            if (jObject.ContainsKey("CreatePartL"))
            {
                CreatePartL = jObject["CreatePartL"]?.GetValue<bool>() ?? default(bool);
            }

            if (jObject.ContainsKey("Sizing"))
            {
                Sizing = jObject["Sizing"]?.GetValue<bool>() ?? default(bool);
            }

            return true;
        }

        public JsonObject ToJsonObject()
        {
            JsonObject result = new JsonObject();
            result.Add("_type", Core.Query.FullTypeName(this));

            if (ProjectName != null)
            {
                result.Add("ProjectName", ProjectName);
            }

            if (ZoneCategories != null)
            {
                JsonArray jArray = new JsonArray();

                foreach (string zoneCategory in ZoneCategories)
                {
                    if (zoneCategory == null)
                    {
                        continue;
                    }

                    jArray.Add(zoneCategory);
                }

                result.Add("ZoneCategories", jArray);
            }

            if (WeatherData != null)
            {
                result.Add("WeatherData", WeatherData.ToJsonObject());
            }

            if (OutputDirectory != null)
            {
                result.Add("OutputDirectory", OutputDirectory);
            }

            result.Add("Simulate", Simulate);

            if (SolarCalculationMethod != SolarCalculationMethod.Undefined)
            {
                result.Add("SolarCalculationMethod", SolarCalculationMethod.ToString());
            }

            result.Add("FullYearSimulation", FullYearSimulation);

            result.Add("UnmetHours", UnmetHours);

            result.Add("RoomDataSheets", RoomDataSheets);

            result.Add("CreateSAP", CreateSAP);

            result.Add("CreateTM59", CreateTM59);

            result.Add("CreateTPD", CreateTPD);

            result.Add("CreatePartL", CreatePartL);

            result.Add("UseWidths", UseWidths);

            result.Add("Sizing", Sizing);

            if (TextMap != null)
            {
                result.Add("TextMap", TextMap.ToJsonObject());
            }

            return result;
        }
    }
}
