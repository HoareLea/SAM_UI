// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using Newtonsoft.Json.Linq;
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

        public SimulateOptions(JObject jObject)
        {
            FromJObject(jObject);
        }

        public bool FromJObject(JObject jObject)
        {
            if(jObject == null)
            {
                return false;
            }

            if (jObject.ContainsKey("ProjectName"))
            {
                ProjectName = jObject.Value<string>("ProjectName");
            }

            if (jObject.ContainsKey("ZoneCategories"))
            {
                JArray jArray = jObject.Value<JArray>("ZoneCategories");
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
                WeatherData = new WeatherData(jObject.Value<JObject>("WeatherData"));
            }

            if(jObject.ContainsKey("OutputDirectory"))
            {
                OutputDirectory = jObject.Value<string>("OutputDirectory");
            }

            if(jObject.ContainsKey("Simulate"))
            {
                Simulate = jObject.Value<bool>("Simulate");
            }

            if (jObject.ContainsKey("SolarCalculationMethod"))
            {
                SolarCalculationMethod = Core.Query.Enum<SolarCalculationMethod>(jObject.Value<string>("SolarCalculationMethod"));
            }

            if (jObject.ContainsKey("FullYearSimulation"))
            {
                FullYearSimulation = jObject.Value<bool>("FullYearSimulation");
            }

            if (jObject.ContainsKey("UnmetHours"))
            {
                UnmetHours = jObject.Value<bool>("UnmetHours");
            }

            if (jObject.ContainsKey("RoomDataSheets"))
            {
                RoomDataSheets = jObject.Value<bool>("RoomDataSheets");
            }

            if (jObject.ContainsKey("CreateSAP"))
            {
                CreateSAP = jObject.Value<bool>("CreateSAP");
            }

            if (jObject.ContainsKey("CreateTM59"))
            {
                CreateTM59 = jObject.Value<bool>("CreateTM59");
            }

            if (jObject.ContainsKey("TextMap"))
            {
                TextMap = Core.Query.IJSAMObject<TextMap>(jObject.Value<JObject>("TextMap"));
            }

            if (jObject.ContainsKey("UseWidths"))
            {
                UseWidths = jObject.Value<bool>("UseWidths");
            }

            if (jObject.ContainsKey("CreateTPD"))
            {
                CreateTPD = jObject.Value<bool>("CreateTPD");
            }

            if (jObject.ContainsKey("CreatePartL"))
            {
                CreatePartL = jObject.Value<bool>("CreatePartL");
            }

            if (jObject.ContainsKey("Sizing"))
            {
                Sizing = jObject.Value<bool>("Sizing");
            }

            return true;
        }

        public JObject ToJObject()
        {
            JObject result = new JObject();
            result.Add("_type", Core.Query.FullTypeName(this));

            if (ProjectName != null)
            {
                result.Add("ProjectName", ProjectName);
            }

            if (ZoneCategories != null)
            {
                JArray jArray = new JArray();

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
                result.Add("WeatherData", WeatherData.ToJObject());
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
                result.Add("TextMap", TextMap.ToJObject());
            }

            return result;
        }
    }
}
