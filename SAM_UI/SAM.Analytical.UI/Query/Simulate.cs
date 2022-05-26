using SAM.Core.Tas;
using SAM.Weather;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace SAM.Analytical.UI
{
    public static partial class Query
    {
        public static AnalyticalModel Simulate(this AnalyticalModel analyticalModel, string path, IWin32Window owner = null)
        {
            if(analyticalModel == null)
            {
                return null;
            }

            if(string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            if(!analyticalModel.TryGetValue(AnalyticalModelParameter.WeatherData, out WeatherData weatherData))
            {
                weatherData = null;
            }

            string projectName = null;
            string outputDirectory = null;
            bool unmetHours = false;
            bool printRoomDataSheets = false;

            SolarCalculationMethod solarCalculationMethod = SolarCalculationMethod.None;
            bool updateConstructionLayersByPanelType = false;

            using (Forms.SimulateForm simulateForm = new Forms.SimulateForm(System.IO.Path.GetFileNameWithoutExtension(path), System.IO.Path.GetDirectoryName(path)))
            {
                simulateForm.WeatherData = weatherData;
                if (simulateForm.ShowDialog(owner) != DialogResult.OK)
                {
                    return null;
                }

                projectName = simulateForm.ProjectName;
                outputDirectory = simulateForm.OutputDirectory;
                unmetHours = simulateForm.UnmetHours;
                weatherData = simulateForm.WeatherData;
                solarCalculationMethod = simulateForm.SolarCalculationMethod;
                updateConstructionLayersByPanelType = simulateForm.UpdateConstructionLayersByPanelType;
                printRoomDataSheets = simulateForm.PrintRoomDataSheets;
            }

            if (weatherData == null)
            {
                return null;
            }

            string path_TBD = System.IO.Path.Combine(outputDirectory, projectName + ".tbd");

            bool simulate = false;

            using (Core.Windows.Forms.ProgressForm progressForm = new Core.Windows.Forms.ProgressForm("Preparing Model", 8))
            {
                progressForm.Update("Update Materials");

                IEnumerable<Core.IMaterial> materials = Analytical.Query.Materials(analyticalModel.AdjacencyCluster, Analytical.Query.DefaultMaterialLibrary());
                if (materials != null)
                {
                    foreach (Core.IMaterial material in materials)
                    {
                        if (analyticalModel.HasMaterial(material))
                        {
                            continue;
                        }

                        analyticalModel.AddMaterial(material);
                    }
                }

                progressForm.Update("Update ConstructionLayers By PanelTypes");

                analyticalModel = updateConstructionLayersByPanelType ? analyticalModel.UpdateConstructionLayersByPanelType() : analyticalModel;

                if (System.IO.File.Exists(path_TBD))
                {
                    System.IO.File.Delete(path_TBD);
                }

                List<int> hoursOfYear = Analytical.Query.DefaultHoursOfYear();

                progressForm.Update("Solar Calculations");
                if (solarCalculationMethod != SolarCalculationMethod.None)
                {
                    SolarCalculator.Modify.Simulate(analyticalModel, hoursOfYear.ConvertAll(x => new DateTime(2018, 1, 1).AddHours(x)), Core.Tolerance.MacroDistance, Core.Tolerance.MacroDistance, 0.012, Core.Tolerance.Distance);
                }

                using (SAMTBDDocument sAMTBDDocument = new SAMTBDDocument(path_TBD))
                {
                    TBD.TBDDocument tBDDocument = sAMTBDDocument.TBDDocument;

                    progressForm.Update("Updating WeatherData");
                    Weather.Tas.Modify.UpdateWeatherData(tBDDocument, weatherData, analyticalModel == null ? 0 : analyticalModel.AdjacencyCluster.BuildingHeight());

                    TBD.Calendar calendar = tBDDocument.Building.GetCalendar();

                    List<TBD.dayType> dayTypes = DayTypes(calendar);
                    if (dayTypes.Find(x => x.name == "HDD") == null)
                    {
                        TBD.dayType dayType = calendar.AddDayType();
                        dayType.name = "HDD";
                    }

                    if (dayTypes.Find(x => x.name == "CDD") == null)
                    {
                        TBD.dayType dayType = calendar.AddDayType();
                        dayType.name = "CDD";
                    }

                    progressForm.Update("Converting to TBD");
                    Tas.Convert.ToTBD(analyticalModel, tBDDocument);

                    progressForm.Update("Updating Zones");
                    Tas.Modify.UpdateZones(tBDDocument.Building, analyticalModel, true);

                    progressForm.Update("Updating Shading");
                    simulate = Tas.Modify.UpdateShading(tBDDocument, analyticalModel);

                    sAMTBDDocument.Save();
                }

                progressForm.Update("Printing Room Data Sheets");
                if (printRoomDataSheets && analyticalModel != null)
                {
                    if (!System.IO.Directory.Exists(outputDirectory))
                    {
                        System.IO.Directory.CreateDirectory(outputDirectory);
                    }

                    Modify.PrintRoomDataSheets(analyticalModel, outputDirectory);
                }
            }

            List<DesignDay> heatingDesignDays = new List<DesignDay>() { Analytical.Query.HeatingDesignDay(weatherData) };
            List<DesignDay> coolingDesignDays = new List<DesignDay>() { Analytical.Query.CoolingDesignDay(weatherData) };

            SurfaceOutputSpec surfaceOutputSpec = new SurfaceOutputSpec("Tas.Simulate")
            {
                SolarGain = true,
                Conduction = true,
                ApertureData = false,
                Condensation = false,
                Convection = false,
                LongWave = false,
                Temperature = false
            };

            List<SurfaceOutputSpec> surfaceOutputSpecs = new List<SurfaceOutputSpec>() { surfaceOutputSpec };

            analyticalModel = Tas.Modify.RunWorkflow(analyticalModel, path_TBD, null, null, heatingDesignDays, coolingDesignDays, surfaceOutputSpecs, unmetHours, simulate, false);

            analyticalModel.SetValue(AnalyticalModelParameter.WeatherData, weatherData);

            return analyticalModel;

        }
    }
}