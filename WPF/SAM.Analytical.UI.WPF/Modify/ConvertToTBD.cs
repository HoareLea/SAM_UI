using SAM.Core;
using SAM.Core.Tas;
using SAM.Core.UI.WPF;
using SAM.Weather;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        public static void ConvertToTBD(this UIAnalyticalModel uIAnalyticalModel)
        {
            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if(analyticalModel == null)
            {
                return;
            }

            ConvertToTBDWindow convertToTBDWindow = new ConvertToTBDWindow();
            if (analyticalModel.TryGetValue(Analytical.AnalyticalModelParameter.WeatherData, out WeatherData weatherData))
            {
                convertToTBDWindow.WeatherData = weatherData;
            }

            convertToTBDWindow.TextMap = Analytical.Query.DefaultInternalConditionTextMap_TM59();

            if(!string.IsNullOrWhiteSpace(uIAnalyticalModel.Path))
            {
                convertToTBDWindow.OutputDirectory = System.IO.Path.GetDirectoryName(uIAnalyticalModel.Path);
            }

            convertToTBDWindow.ProjectName = analyticalModel.Name;
            convertToTBDWindow.Simulate = true;
            convertToTBDWindow.SolarCalculationMethod = SolarCalculationMethod.SAM;
            convertToTBDWindow.FullYearSimulation = true;
            convertToTBDWindow.UnmetHours = true;
            convertToTBDWindow.RoomDataSheets = true;
            convertToTBDWindow.CreateSAP = true;
            convertToTBDWindow.CreateTM59 = true;

            convertToTBDWindow.ZoneCategories = analyticalModel.GetZoneCategories()?.ToList();


            bool? showdialog = convertToTBDWindow.ShowDialog();
            if(showdialog == null || !showdialog.HasValue || !showdialog.Value)
            {
                return;
            }

            string projectName = convertToTBDWindow.ProjectName;
            string outputDirectory = convertToTBDWindow.OutputDirectory;
            bool unmetHours = convertToTBDWindow.UnmetHours;
            bool printRoomDataSheets = convertToTBDWindow.RoomDataSheets;

            bool fullYearSimulation = convertToTBDWindow.FullYearSimulation;
            int fullYearSimulation_From = convertToTBDWindow.FullYearSimulation_From;
            int fullYearSimulation_To = convertToTBDWindow.FullYearSimulation_To;

            SolarCalculationMethod solarCalculationMethod = convertToTBDWindow.SolarCalculationMethod;
            bool updateConstructionLayersByPanelType = true;

            TextMap textMap = convertToTBDWindow.SelectedTextMap;
            weatherData = convertToTBDWindow.WeatherData;
            string zoneCategory = convertToTBDWindow.SelectedZoneCategory;

            string path_TBD = System.IO.Path.Combine(outputDirectory, projectName + ".tbd");
            string path_Xml = null;
            if(solarCalculationMethod == SolarCalculationMethod.TAS)
            {
                path_Xml = System.IO.Path.Combine(outputDirectory, projectName + ".xml");
                if(!gbXML.Convert.ToFile(analyticalModel, path_Xml))
                {
                    MessageBox.Show("Could not create gbXML file.");
                    return;
                }
            }

            bool shadingUpdated = false;

            bool converted = false;

            if(convertToTBDWindow.Simulate)
            {
                using (Core.Windows.Forms.ProgressForm progressForm = new Core.Windows.Forms.ProgressForm("Preparing Model", 8))
                {
                    progressForm.Update("Update Materials");

                    IEnumerable<IMaterial> materials = Analytical.Query.Materials(analyticalModel.AdjacencyCluster, Analytical.Query.DefaultMaterialLibrary());
                    if (materials != null)
                    {
                        foreach (IMaterial material in materials)
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
                        try
                        {
                            System.IO.File.Delete(path_TBD);
                        }
                        catch
                        {
                            MessageBox.Show("Cannot override existing TBD file.");
                            return;
                        }


                    }

                    List<int> hoursOfYear = Analytical.Query.DefaultHoursOfYear();

                    progressForm.Update("Solar Calculations");
                    if (solarCalculationMethod != SolarCalculationMethod.None)
                    {
                        SolarCalculator.Modify.Simulate(analyticalModel, hoursOfYear.ConvertAll(x => new DateTime(2018, 1, 1).AddHours(x)), false, Tolerance.MacroDistance, Tolerance.MacroDistance, 0.012, Tolerance.Distance);
                    }

                    using (SAMTBDDocument sAMTBDDocument = new SAMTBDDocument(path_TBD))
                    {
                        TBD.TBDDocument tBDDocument = sAMTBDDocument.TBDDocument;

                        progressForm.Update("Updating WeatherData");
                        Weather.Tas.Modify.UpdateWeatherData(tBDDocument, weatherData, analyticalModel == null ? 0 : analyticalModel.AdjacencyCluster.BuildingHeight());

                        TBD.Calendar calendar = tBDDocument.Building.GetCalendar();

                        List<TBD.dayType> dayTypes = Query.DayTypes(calendar);
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
                        Tas.Convert.ToTBD(analyticalModel, tBDDocument, true);

                        progressForm.Update("Updating Zones");
                        Tas.Modify.UpdateZones(tBDDocument.Building, analyticalModel, true);

                        progressForm.Update("Updating Shading");
                        if(solarCalculationMethod != SolarCalculationMethod.None)
                        {
                            shadingUpdated = Tas.Modify.UpdateShading(tBDDocument, analyticalModel);
                        }

                        sAMTBDDocument.Save();
                    }

                    progressForm.Update("Printing Room Data Sheets");
                    if (printRoomDataSheets && analyticalModel != null)
                    {
                        if (!System.IO.Directory.Exists(outputDirectory))
                        {
                            System.IO.Directory.CreateDirectory(outputDirectory);
                        }

                        UI.Modify.PrintRoomDataSheets(analyticalModel, outputDirectory);
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

                int simulate_From = -1;
                int simulate_To = -1;

                bool simulate = fullYearSimulation;

                if (simulate)
                {
                    simulate_From = fullYearSimulation_From;
                    simulate_To = fullYearSimulation_To;
                }

                if (shadingUpdated)
                {
                    if (!simulate)
                    {
                        simulate_From = 1;
                        simulate_To = 1;
                        simulate = true;
                    }
                }

                analyticalModel = Tas.Modify.RunWorkflow(analyticalModel, path_TBD, path_Xml, null, heatingDesignDays, coolingDesignDays, surfaceOutputSpecs, unmetHours, simulate, false, simulate_From, simulate_To);

                analyticalModel.SetValue(Analytical.AnalyticalModelParameter.WeatherData, weatherData);
                converted = true;
            }

            using (ProgressBarWindowManager progressBarWindowManager = new ProgressBarWindowManager("Convert to TBD", "Converting..."))
            {
                if(!converted)
                {
                    converted = Tas.Convert.ToTBD(analyticalModel, path_TBD, null, null, null, true);
                }

                if (converted)
                {
                    if (Tas.TM59.Modify.TryCreatePath(path_TBD, out string path_TM59))
                    {
                        Tas.TM59.Convert.ToXml(analyticalModel, path_TM59, new TM59Manager(textMap));
                    }

                    if (string.IsNullOrWhiteSpace(zoneCategory))
                    {
                        converted = false;
                    }
                    else
                    {
                        if (!Tas.SAP.Modify.TryCreatePath(path_TBD, out string path_SAP))
                        {
                            converted = false;
                        }
                        else
                        {
                            converted = Tas.SAP.Convert.ToFile(analyticalModel, path_SAP, zoneCategory, textMap);
                        }

                    }
                }
            }

            if(converted)
            {
                MessageBox.Show("Model successfuly converted.");
            }
            else
            {
                MessageBox.Show("Model could not be converted.");
            }
        }
    }
}