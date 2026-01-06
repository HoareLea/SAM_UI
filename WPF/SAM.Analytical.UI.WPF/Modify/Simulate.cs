// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Tas;
using SAM.Core;
using SAM.Core.Tas;
using SAM.Core.UI;
using SAM.Core.UI.WPF;
using SAM.Weather;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        public static void Simulate(this UIAnalyticalModel uIAnalyticalModel)
        {
            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if(analyticalModel == null)
            {
                return;
            }

            SimulateWindow simulateWindow = new SimulateWindow();
            ActiveSetting.Setting.TryGetValue(AnalyticalSettingParameter.SimulateOptions, out SimulateOptions simulateOptions);
            if(simulateOptions == null)
            {
                simulateOptions = UI.Create.SimulateOptions(uIAnalyticalModel);
            }

            simulateWindow.ProjectName = analyticalModel.Name;

            if (!string.IsNullOrWhiteSpace(uIAnalyticalModel.Path))
            {
                simulateWindow.OutputDirectory = System.IO.Path.GetDirectoryName(uIAnalyticalModel.Path);
            }

            simulateWindow.SimulateOptions = simulateOptions;

            if (analyticalModel.TryGetValue(Analytical.AnalyticalModelParameter.WeatherData, out WeatherData weatherData))
            {
                simulateWindow.WeatherData = weatherData;
            }

            bool? showdialog = simulateWindow.ShowDialog();
            if(showdialog == null || !showdialog.HasValue || !showdialog.Value)
            {
                return;
            }

            ActiveSetting.Setting.SetValue(AnalyticalSettingParameter.SimulateOptions, simulateWindow.SimulateOptions);

            string projectName = simulateWindow.ProjectName;
            string outputDirectory = simulateWindow.OutputDirectory;
            bool unmetHours = simulateWindow.UnmetHours;
            bool printRoomDataSheets = simulateWindow.RoomDataSheets;

            bool fullYearSimulation = simulateWindow.FullYearSimulation;
            int fullYearSimulation_From = simulateWindow.FullYearSimulation_From;
            int fullYearSimulation_To = simulateWindow.FullYearSimulation_To;

            bool createSAP = simulateWindow.CreateSAP;
            bool createTM59 = simulateWindow.CreateTM59;
            bool createTPD = simulateWindow.CreateTPD;
            bool createPartL = simulateWindow.CreatePartL;

            bool sizing = simulateWindow.Sizing;

            bool useWidths = simulateWindow.UseWidths;

            SolarCalculationMethod solarCalculationMethod = simulateWindow.SolarCalculationMethod;
            bool updateConstructionLayersByPanelType = true;

            TextMap textMap = simulateWindow.SelectedTextMap;
            weatherData = simulateWindow.SelectedWeatherData;
            string zoneCategory = simulateWindow.SelectedZoneCategory;

            if (!simulateWindow.Simulate && !createSAP && !createTM59)
            {
                return;
            }

            analyticalModel.Name = projectName;

            DateTime dateTime = DateTime.Now;

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

            string path_TBD = System.IO.Path.Combine(outputDirectory, projectName + ".tbd");

            bool shadingUpdated = false;

            bool converted = false;

            if(simulateWindow.Simulate)
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

                    if (solarCalculationMethod == SolarCalculationMethod.SAM)
                    {
                        List<int> hoursOfYear = Analytical.Query.DefaultHoursOfYear();

                        SolarCalculator.Modify.Simulate(analyticalModel, hoursOfYear.ConvertAll(x => new DateTime(2018, 1, 1).AddHours(x)), false, Tolerance.MacroDistance, Tolerance.MacroDistance, 0.012, Tolerance.Distance);

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
                            shadingUpdated = Tas.Modify.UpdateShading(tBDDocument, analyticalModel);

                            sAMTBDDocument.Save();
                        }

                    }
                }

                List<DesignDay> heatingDesignDays = new List<DesignDay>() { Analytical.Query.HeatingDesignDay(weatherData) };
                List<DesignDay> coolingDesignDays = new List<DesignDay>() { Analytical.Query.CoolingDesignDay(weatherData) };

                SurfaceOutputSpec surfaceOutputSpec = new SurfaceOutputSpec("Tas.Simulate")
                {
                    SolarGain = true,
                    Conduction = true,
                    ApertureData = true,
                    Condensation = false,
                    Convection = false,
                    LongWave = false,
                    Temperature = true
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

                WeatherData weatherData_Workflow = null;
                bool updateZones_Workflow = false;
                if(solarCalculationMethod == SolarCalculationMethod.TAS)
                {
                    weatherData_Workflow = weatherData;
                    updateZones_Workflow = true;
                }

                WorkflowSettings workflowSettings = new WorkflowSettings()
                {
                    Path_TBD = path_TBD,
                    Path_gbXML = path_Xml,
                    WeatherData = weatherData_Workflow,
                    DesignDays_Heating = heatingDesignDays,
                    DesignDays_Cooling = coolingDesignDays,
                    SurfaceOutputSpecs = surfaceOutputSpecs,
                    UnmetHours = unmetHours,
                    Simulate = simulate,
                    Sizing = sizing,
                    UpdateZones = updateZones_Workflow,
                    UseWidths = useWidths,
                    SimulateFrom = simulate_From,
                    SimulateTo = simulate_To
                };

                analyticalModel = Tas.Modify.RunWorkflow(analyticalModel, workflowSettings);

                if (printRoomDataSheets && analyticalModel != null)
                {
                    if (!System.IO.Directory.Exists(outputDirectory))
                    {
                        System.IO.Directory.CreateDirectory(outputDirectory);
                    }

                    UI.Modify.PrintRoomDataSheets(analyticalModel, outputDirectory);
                }

                analyticalModel.SetValue(Analytical.AnalyticalModelParameter.WeatherData, weatherData);
                converted = true;
            }

            if(createSAP || createTM59 || createPartL)
            {
                using (ProgressBarWindowManager progressBarWindowManager = new ProgressBarWindowManager("Convert to TBD", "Converting..."))
                {
                    if (!converted)
                    {
                        converted = Tas.Convert.ToTBD(analyticalModel, path_TBD, null, null, null, true);
                    }

                    if (converted)
                    {
                        AnalyticalModel analyticalModel_TBD = Tas.Convert.ToSAM(path_TBD, false);
                        if(analyticalModel != null && analyticalModel_TBD != null)
                        {
                            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;
                            List<Space> spaces_TBD = analyticalModel_TBD.AdjacencyCluster?.GetSpaces();
                            if(spaces_TBD != null && spaces_TBD.Count != 0)
                            {
                                List<Space> spaces = adjacencyCluster.GetSpaces();
                                foreach(Space space in spaces)
                                {
                                    Space space_TBD = spaces_TBD.Find(x => x?.Name == space?.Name);
                                    if(space_TBD == null)
                                    {
                                        continue;
                                    }

                                    if(space_TBD.TryGetValue(Tas.SpaceParameter.ZoneGuid, out Guid guid))
                                    {
                                        space.SetValue(Tas.SpaceParameter.ZoneGuid, guid);
                                        adjacencyCluster.AddObject(space);
                                    }
                                }
                            }

                            analyticalModel = new AnalyticalModel(analyticalModel, adjacencyCluster);
                        }

                        if (createTM59)
                        {
                            if (Tas.TM59.Modify.TryCreatePath(path_TBD, out string path_TM59))
                            {
                                Tas.TM59.Convert.ToXml(analyticalModel, path_TM59, new TM59Manager(textMap));
                            }
                        }

                        if (createSAP)
                        {
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
                                    converted = Tas.SAP.Convert.ToFile(analyticalModel_TBD, path_SAP, zoneCategory, textMap);
                                }

                            }
                        }

                        if(createPartL)
                        {
                            converted = Tas.Create.TBD_ByPartL(analyticalModel, path_TBD, out string path_TBD_Destination);
                        }
                    }
                }
            }

            if(createTPD)
            {
                string directory = System.IO.Path.GetDirectoryName(path_TBD);
                string fileName = System.IO.Path.GetFileNameWithoutExtension(path_TBD);

                string path_TSD = System.IO.Path.Combine(directory, string.Format("{0}.{1}", fileName, "tsd"));
                if(System.IO.File.Exists(path_TSD))
                {
                    string path_TPD = System.IO.Path.Combine(directory, string.Format("{0}.{1}", fileName, "tpd"));

                    Tas.Create.TPD(path_TPD, path_TSD, analyticalModel);
                }
            }

            TimeSpan timeSpan = new TimeSpan(DateTime.Now.Ticks - dateTime.Ticks);

            string message = converted ? "Model successfuly converted." : "Model could not be converted.";
            message += string.Format("\n Time elapsed: {0}min{1}sec", timeSpan.Minutes, timeSpan.Seconds);
            MessageBox.Show(message);

            uIAnalyticalModel.SetJSAMObject(analyticalModel, new FullModification());
        }
    }
}
