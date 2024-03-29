﻿using NetOffice.ExcelApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace SAM.Analytical.UI
{
    public static partial class Modify
    {
        public static void PrintRoomDataSheets(this UIAnalyticalModel uIAnalyticalModel, IWin32Window owner = null)
        {
            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if (analyticalModel == null)
            {
                return;
            }

            string directory = uIAnalyticalModel.Path;
            if(!string.IsNullOrWhiteSpace(directory))
            {
                directory = System.IO.Path.GetDirectoryName(directory);
            }

            PrintRoomDataSheets(analyticalModel, directory, owner);
        }

        public static void PrintRoomDataSheets(this AnalyticalModel analyticalModel, string directory = null, IWin32Window owner = null)
        {
            if (analyticalModel == null)
            {
                return;
            }

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;
            if (adjacencyCluster == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(directory) || !System.IO.Directory.Exists(directory))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(directory) || !System.IO.Directory.Exists(directory))
            {
                using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog())
                {
                    if (folderBrowserDialog.ShowDialog(owner) != DialogResult.OK)
                    {
                        return;
                    }

                    directory = folderBrowserDialog.SelectedPath;
                }
            }

            using (Core.Windows.Forms.ProgressForm progressForm = new Core.Windows.Forms.ProgressForm("Print RDS", 4))
            {
                progressForm.Update("Collecting Data");

                string path_Template = Core.Query.TemplatesDirectory(typeof(AnalyticalModel).Assembly);
                if (!System.IO.Directory.Exists(path_Template))
                {
                    return;
                }

                path_Template = System.IO.Path.Combine(path_Template, "RDS", "PDF_Print_RDS.xlsm");
                if (!System.IO.File.Exists(path_Template))
                {
                    return;
                }

                List<Space> spaces = adjacencyCluster.GetSpaces();
                if (spaces == null || spaces.Count == 0)
                {
                    return;
                }

                int year = 2018;

                string path = System.IO.Path.Combine(directory, System.IO.Path.GetFileName(path_Template));

                System.IO.File.Copy(path_Template, path, true);

                int min = 1;
                int max = int.MinValue;

                Func<Worksheet, bool> func = new Func<Worksheet, bool>((Worksheet worksheet) =>
                {
                    if (worksheet == null)
                    {
                        return false;
                    }

                    int lastRowIndex = worksheet.Cells.SpecialCells(NetOffice.ExcelApi.Enums.XlCellType.xlCellTypeLastCell, Type.Missing).Row;
                    int lastColumnIndex = worksheet.Cells.SpecialCells(NetOffice.ExcelApi.Enums.XlCellType.xlCellTypeLastCell, Type.Missing).Column;

                    worksheet.Range(worksheet.Range("A3"), worksheet.Cells[lastRowIndex, lastColumnIndex]).ClearContents();

                    max = 0;

                    //adjust nr 112 to latest column in RDS excel..
                    object[,] values = new object[spaces.Count, 113];
                    for (int i = 0; i < spaces.Count; i++)
                    {
                        Space space = spaces[i];
                        if (space == null)
                        {
                            continue;
                        }

                        max++;

                        InternalCondition internalCondition = space.InternalCondition;

                        List<Panel> panels = adjacencyCluster.GetPanels(space);

                        values[i, 0] = space.Name;

                        if (space.TryGetValue(SpaceParameter.LevelName, out string levelName) && !string.IsNullOrWhiteSpace(levelName))
                        {
                            values[i, 2] = levelName;
                        }

                        values[i, 3] = i + 1;
                        values[i, 4] = space.Guid;

                        double occupancy = Analytical.Query.CalculatedOccupancy(space);
                        if(!double.IsNaN(occupancy))
                        {
                            values[i, 5] = occupancy;
                        }

                        if (panels != null && panels.Count != 0)
                        {
                            SortedDictionary<string, double> sortedDictionary = new SortedDictionary<string, double>();
                            foreach (PanelType panelType in Enum.GetValues(typeof(PanelType)))
                            {
                                if (panelType == PanelType.Undefined)
                                {
                                    continue;
                                }

                                string name = panelType.ToString();
                                if (panelType == PanelType.CurtainWall)
                                {
                                    sortedDictionary[name + " External"] = 0;
                                    sortedDictionary[name + " Internal"] = 0;
                                }
                                else
                                {
                                    sortedDictionary[name] = 0;
                                }
                            }

                            List<Aperture> apertures_External = new List<Aperture>();
                            List<Aperture> apertures_Internal = new List<Aperture>();
                            foreach (Panel panel in panels)
                            {
                                if (panel == null)
                                {
                                    continue;
                                }

                                double area = panel.GetAreaNet();
                                if (double.IsNaN(area))
                                {
                                    continue;
                                }

                                bool external = adjacencyCluster.External(panel);

                                PanelType panelType = panel.PanelType;

                                string name = panelType.ToString();
                                if (panelType == PanelType.CurtainWall)
                                {
                                    name += external? " External" : " Internal";
                                }

                                sortedDictionary[name] += area;

                                List<Aperture> apertures_Panel = panel.Apertures;
                                if(apertures_Panel != null)
                                {

                                    List<Aperture> apertures_Temp = external ? apertures_External : apertures_Internal;
                                    apertures_Temp.AddRange(apertures_Panel);
                                }
                            }

                            double area_WindowPane_External = 0;
                            double area_WindowPane_Internal = 0;
                            double area_DoorPane_External = 0;
                            double area_DoorPane_Internal = 0;
                            double area_WindowFrame_External = 0;
                            double area_WindowFrame_Internal = 0;
                            double area_DoorFrame_External = 0;
                            double area_DoorFrame_Internal = 0;

                            foreach(Aperture aperture in apertures_External)
                            {
                                ApertureType apertureType = aperture.ApertureType;
                                if(apertureType == ApertureType.Undefined)
                                {
                                    continue;
                                }

                                double area = Analytical.Query.Area(aperture, out double paneArea, out double frameArea);
                                if(!double.IsNaN(paneArea))
                                {
                                   if(apertureType == ApertureType.Door)
                                    {
                                        area_DoorPane_External += paneArea;
                                    }
                                    else
                                    {
                                        area_WindowPane_External += paneArea;
                                    }
                                }

                                if (!double.IsNaN(frameArea))
                                {
                                    if (apertureType == ApertureType.Door)
                                    {
                                        area_DoorFrame_External += frameArea;
                                    }
                                    else
                                    {
                                        area_WindowFrame_External += frameArea;
                                    }
                                }
                            }

                            foreach (Aperture aperture in apertures_Internal)
                            {
                                ApertureType apertureType = aperture.ApertureType;
                                if (apertureType == ApertureType.Undefined)
                                {
                                    continue;
                                }

                                double area = Analytical.Query.Area(aperture, out double paneArea, out double frameArea);
                                if (!double.IsNaN(paneArea))
                                {
                                    if (apertureType == ApertureType.Door)
                                    {
                                        area_DoorPane_Internal += paneArea;
                                    }
                                    else
                                    {
                                        area_WindowPane_Internal += paneArea;
                                    }
                                }

                                if (!double.IsNaN(frameArea))
                                {
                                    if (apertureType == ApertureType.Door)
                                    {
                                        area_DoorFrame_Internal += frameArea;
                                    }
                                    else
                                    {
                                        area_WindowFrame_Internal += frameArea;
                                    }
                                }
                            }

                            values[i, 101] = area_WindowPane_External;
                            values[i, 102] = area_WindowPane_Internal;
                            values[i, 103] = area_DoorPane_External;
                            values[i, 104] = area_DoorPane_Internal;
                            values[i, 105] = area_WindowFrame_External;
                            values[i, 106] = area_WindowFrame_Internal;
                            values[i, 107] = area_DoorFrame_External;
                            values[i, 108] = area_DoorFrame_Internal;

                            int index = 82;
                            foreach (double value in sortedDictionary.Values)
                            {
                                index++;

                                values[i, index] = value;
                            }
                        }

                        if (space.TryGetValue(SpaceParameter.CoolingRiserName, out string coolingRiserName) && !string.IsNullOrWhiteSpace(coolingRiserName))
                        {
                            values[i, 25] = coolingRiserName;
                        }

                        if (space.TryGetValue(SpaceParameter.HeatingRiserName, out string heatingRiserName) && !string.IsNullOrWhiteSpace(heatingRiserName))
                        {
                            values[i, 26] = heatingRiserName;
                        }

                        if (space.TryGetValue(SpaceParameter.VentilationRiserName, out string ventilationRiserName) && !string.IsNullOrWhiteSpace(ventilationRiserName))
                        {
                            values[i, 27] = ventilationRiserName;
                        }

                        if (space.TryGetValue(SpaceParameter.SupplyAirFlow, out double supplyAirFlow) && !double.IsNaN(supplyAirFlow))
                        {
                            values[i, 40] = supplyAirFlow;
                        }

                        if (space.TryGetValue(SpaceParameter.ExhaustAirFlow, out double exhaustAirFlow) && !double.IsNaN(exhaustAirFlow))
                        {
                            values[i, 41] = exhaustAirFlow;
                        }

                        if (space.TryGetValue(SpaceParameter.OutsideSupplyAirFlow, out double outsideSupplyAirFlow) && !double.IsNaN(outsideSupplyAirFlow))
                        {
                            values[i, 42] = outsideSupplyAirFlow;
                        }

                        if (space.TryGetValue(SpaceParameter.DesignHeatingLoad, out double designHeatingLoad) && !double.IsNaN(designHeatingLoad))
                        {
                            values[i, 60] = designHeatingLoad;
                        }

                        if (space.TryGetValue(SpaceParameter.DesignCoolingLoad, out double designCoolingLoad) && !double.IsNaN(designCoolingLoad))
                        {
                            values[i, 62] = designCoolingLoad;
                        }

                        double coolingDesignTemperature = Analytical.Query.CoolingDesignTemperature(space, analyticalModel);
                        if (!double.IsNaN(coolingDesignTemperature))
                        {
                            values[i, 67] = coolingDesignTemperature;
                        }

                        double heatingDesignTemperature = Analytical.Query.HeatingDesignTemperature(space, analyticalModel);
                        if (!double.IsNaN(heatingDesignTemperature))
                        {
                            values[i, 68] = heatingDesignTemperature;
                        }

                        double specificOccupancySensibleGain = space.SpecificOccupancySensibleGain();
                        if (!double.IsNaN(specificOccupancySensibleGain))
                        {
                            values[i, 45] = specificOccupancySensibleGain;
                        }

                        Geometry.Spatial.Shell shell = adjacencyCluster.Shell(space);
                        if (shell != null)
                        {
                            double area = Geometry.Spatial.Query.Area(shell, 0.1);
                            if (!double.IsNaN(area))
                            {
                                values[i, 17] = area;
                            }
                        }

                        VentilationSystem ventilationSystem = Analytical.Query.Systems<VentilationSystem>(adjacencyCluster, space).FirstOrDefault();
                        if (ventilationSystem != null)
                        {
                            if (ventilationSystem.TryGetValue(VentilationSystemParameter.SupplyUnitName, out string supplyUnitName))
                            {
                                values[i, 6] = supplyUnitName;
                            }

                            if (ventilationSystem.TryGetValue(VentilationSystemParameter.ExhaustUnitName, out string exhaustUnitName))
                            {
                                values[i, 7] = exhaustUnitName;
                            }

                            VentilationSystemType ventilationSystemType = ventilationSystem.Type as VentilationSystemType;
                            if (ventilationSystemType != null)
                            {
                                if (!string.IsNullOrWhiteSpace(ventilationSystemType.Name))
                                {
                                    values[i, 9] = ventilationSystemType.Name;
                                }
                            }
                        }

                        if (internalCondition != null)
                        {
                            values[i, 8] = internalCondition.Name;

                            if (internalCondition.TryGetValue(InternalConditionParameter.InfiltrationAirChangesPerHour, out double infiltrationAirChangesPerHour) && !double.IsNaN(infiltrationAirChangesPerHour))
                            {
                                values[i, 11] = infiltrationAirChangesPerHour;
                            }

                            if (internalCondition.TryGetValue(InternalConditionParameter.LightingLevel, out double lightingLevel) && !double.IsNaN(lightingLevel))
                            {
                                values[i, 13] = lightingLevel;
                            }

                            if (internalCondition.TryGetValue(InternalConditionParameter.OccupancyProfileName, out string occupancyProfileName) && !string.IsNullOrEmpty(occupancyProfileName))
                            {
                                values[i, 21] = occupancyProfileName;
                            }

                            if (internalCondition.TryGetValue(InternalConditionParameter.EquipmentSensibleGainPerArea, out double equipmentSensibleGainPerArea) && !double.IsNaN(equipmentSensibleGainPerArea))
                            {
                                values[i, 44] = equipmentSensibleGainPerArea;
                            }

                            if (internalCondition.TryGetValue(InternalConditionParameter.LightingGainPerArea, out double lightingGainPerArea) && !double.IsNaN(lightingGainPerArea))
                            {
                                values[i, 46] = lightingGainPerArea;
                            }
                        }

                        HeatingSystem heatingSystem = Analytical.Query.Systems<HeatingSystem>(adjacencyCluster, space).FirstOrDefault();
                        if (heatingSystem != null)
                        {
                            HeatingSystemType heatingSystemType = heatingSystem.Type as HeatingSystemType;
                            if (heatingSystemType != null)
                            {
                                if (!string.IsNullOrWhiteSpace(heatingSystemType.Name))
                                {
                                    values[i, 10] = heatingSystemType.Name;
                                }
                            }
                        }

                        CoolingSystem coolingSystem = Analytical.Query.Systems<CoolingSystem>(adjacencyCluster, space).FirstOrDefault();
                        if (coolingSystem != null)
                        {
                            CoolingSystemType coolingSystemType = coolingSystem.Type as CoolingSystemType;
                            if (coolingSystemType != null)
                            {
                                if (!string.IsNullOrWhiteSpace(coolingSystemType.Name))
                                {
                                    values[i, 24] = coolingSystemType.Name;
                                }
                            }
                        }

                        List<SpaceSimulationResult> spaceSimulationResults = adjacencyCluster.GetResults<SpaceSimulationResult>(space);
                        if (spaceSimulationResults != null && spaceSimulationResults.Count != 0)
                        {
                            SpaceSimulationResult spaceSimulationResult_Heating = spaceSimulationResults.Find(x => x.HasValue(SpaceSimulationResultParameter.LoadType) && LoadType.Heating.ToString().Equals(x.GetValue(SpaceSimulationResultParameter.LoadType)));
                            if (spaceSimulationResult_Heating != null)
                            {
                                if (values[i, 17] == null)
                                {
                                    if (spaceSimulationResult_Heating.TryGetValue(SpaceSimulationResultParameter.Area, out double area) && !double.IsNaN(area))
                                    {
                                        values[i, 17] = area;
                                    }
                                }

                                if (spaceSimulationResult_Heating.TryGetValue(SpaceSimulationResultParameter.Volume, out double volume) && !double.IsNaN(volume))
                                {
                                    values[i, 19] = volume;
                                }

                                double specificDesignLoad = Analytical.Query.SpecificDesignLoad(spaceSimulationResult_Heating);
                                if (!double.IsNaN(specificDesignLoad))
                                {
                                    values[i, 48] = specificDesignLoad;
                                }

                                if (spaceSimulationResult_Heating.TryGetValue(SpaceSimulationResultParameter.Load, out double load) && !double.IsNaN(load))
                                {
                                    values[i, 61] = load;
                                }

                                if (spaceSimulationResult_Heating.TryGetValue(SpaceSimulationResultParameter.InfiltrationGain, out double infiltrationGain) && !double.IsNaN(infiltrationGain))
                                {
                                    values[i, 69] = infiltrationGain;
                                }

                                if (spaceSimulationResult_Heating.TryGetValue(SpaceSimulationResultParameter.AirMovementGain, out double airMovementGain) && !double.IsNaN(airMovementGain))
                                {
                                    values[i, 70] = airMovementGain;
                                }

                                double specificBuildingHeatTransferGain = spaceSimulationResult_Heating.SpecificBuildingHeatTransferGain();
                                if (!double.IsNaN(specificBuildingHeatTransferGain))
                                {
                                    values[i, 71] = specificBuildingHeatTransferGain;
                                }

                                if (spaceSimulationResult_Heating.TryGetValue(SpaceSimulationResultParameter.GlazingExternalConduction, out double glazingExternalConduction) && !double.IsNaN(glazingExternalConduction))
                                {
                                    values[i, 72] = glazingExternalConduction;
                                }

                                if (spaceSimulationResult_Heating.TryGetValue(SpaceSimulationResultParameter.OpaqueExternalConduction, out double opaqueExternalConduction) && !double.IsNaN(opaqueExternalConduction))
                                {
                                    values[i, 73] = opaqueExternalConduction;
                                }

                                if (spaceSimulationResult_Heating.TryGetValue(SpaceSimulationResultParameter.SizingMethod, out string sizingMethod) && !string.IsNullOrWhiteSpace(sizingMethod))
                                {
                                    values[i, 74] = sizingMethod;
                                }

                                if (spaceSimulationResult_Heating.TryGetValue(SpaceSimulationResultParameter.DryBulbTempearture, out double dryBulbTempearture) && !double.IsNaN(dryBulbTempearture))
                                {
                                    values[i, 75] = dryBulbTempearture;
                                }

                                if (spaceSimulationResult_Heating.TryGetValue(SpaceSimulationResultParameter.ResultantTemperature, out double resultantTemperature) && !double.IsNaN(resultantTemperature))
                                {
                                    values[i, 76] = resultantTemperature;
                                }

                                if (spaceSimulationResult_Heating.TryGetValue(SpaceSimulationResultParameter.DesignDayTemperature, out double designDayTemperature) && !double.IsNaN(designDayTemperature))
                                {
                                    values[i, 109] = designDayTemperature;
                                }

                                if (spaceSimulationResult_Heating.TryGetValue(SpaceSimulationResultParameter.DesignDayRelativeHumidity, out double designDayRelativeHumidity) && !double.IsNaN(designDayRelativeHumidity))
                                {
                                    values[i, 110] = designDayRelativeHumidity;
                                }
                            }

                            values[i, 22] = spaceSimulationResults.ConvertAll(x => x.DateTime).Max();

                            SpaceSimulationResult spaceSimulationResult_Cooling = spaceSimulationResults.Find(x => x.HasValue(SpaceSimulationResultParameter.LoadType) && LoadType.Cooling.ToString().Equals(x.GetValue(SpaceSimulationResultParameter.LoadType)));
                            if (spaceSimulationResult_Cooling != null)
                            {
                                if (spaceSimulationResult_Cooling.TryGetValue(SpaceSimulationResultParameter.LoadIndex, out int loadIndex))
                                {
                                    values[i, 18] = loadIndex;
                                }

                                if (spaceSimulationResult_Cooling.TryGetValue(SpaceSimulationResultParameter.RelativeHumidity, out double relativeHumidity))
                                {
                                    values[i, 23] = relativeHumidity;
                                }

                                if (loadIndex != -1)
                                {
                                    values[i, 30] = Analytical.Convert.ToDateTime(loadIndex, year);
                                }

                                if (spaceSimulationResult_Cooling.TryGetValue(SpaceSimulationResultParameter.HumidityRatio, out double humidityRatio))
                                {
                                    values[i, 31] = humidityRatio;
                                }

                                if (spaceSimulationResult_Cooling.TryGetValue(SpaceSimulationResultParameter.SizingMethod, out string sizingMethod) && !string.IsNullOrWhiteSpace(sizingMethod))
                                {
                                    values[i, 32] = sizingMethod;
                                }

                                if (spaceSimulationResult_Cooling.TryGetValue(SpaceSimulationResultParameter.OccupiedHours25, out int occupiedHours_25) && int.MinValue != occupiedHours_25 && int.MaxValue != occupiedHours_25)
                                {
                                    values[i, 33] = occupiedHours_25;
                                }

                                if (spaceSimulationResult_Cooling.TryGetValue(SpaceSimulationResultParameter.OccupiedHours28, out int occupiedHours_28) && int.MinValue != occupiedHours_28 && int.MaxValue != occupiedHours_28)
                                {
                                    values[i, 34] = occupiedHours_28;
                                }

                                if (spaceSimulationResult_Cooling.TryGetValue(SpaceSimulationResultParameter.OccupiedHours, out int occupiedHours) && int.MinValue != occupiedHours && int.MaxValue != occupiedHours)
                                {
                                    values[i, 39] = occupiedHours;
                                }

                                double specificBuildingHeatTransferGain = spaceSimulationResult_Cooling.SpecificBuildingHeatTransferGain();
                                if (!double.IsNaN(specificBuildingHeatTransferGain))
                                {
                                    values[i, 47] = specificBuildingHeatTransferGain;
                                }

                                if (spaceSimulationResult_Cooling.TryGetValue(SpaceSimulationResultParameter.SolarGain, out double solarGain) && !double.IsNaN(solarGain))
                                {
                                    values[i, 49] = solarGain;
                                }

                                if (spaceSimulationResult_Cooling.TryGetValue(SpaceSimulationResultParameter.LightingGain, out double lightingGain) && !double.IsNaN(lightingGain))
                                {
                                    values[i, 50] = lightingGain;
                                }

                                if (spaceSimulationResult_Cooling.TryGetValue(SpaceSimulationResultParameter.InfiltrationGain, out double infiltrationGain) && !double.IsNaN(infiltrationGain))
                                {
                                    values[i, 51] = infiltrationGain;
                                }

                                if (spaceSimulationResult_Cooling.TryGetValue(SpaceSimulationResultParameter.AirMovementGain, out double airMovementGain) && !double.IsNaN(airMovementGain))
                                {
                                    values[i, 52] = airMovementGain;
                                }

                                if (spaceSimulationResult_Cooling.TryGetValue(SpaceSimulationResultParameter.BuildingHeatTransfer, out double buildingHeatTransfer) && !double.IsNaN(buildingHeatTransfer))
                                {
                                    values[i, 53] = buildingHeatTransfer;
                                }

                                if (spaceSimulationResult_Cooling.TryGetValue(SpaceSimulationResultParameter.GlazingExternalConduction, out double glazingExternalConduction) && !double.IsNaN(glazingExternalConduction))
                                {
                                    values[i, 54] = glazingExternalConduction;
                                }

                                if (spaceSimulationResult_Cooling.TryGetValue(SpaceSimulationResultParameter.OpaqueExternalConduction, out double opaqueExternalConduction) && !double.IsNaN(opaqueExternalConduction))
                                {
                                    values[i, 55] = opaqueExternalConduction;
                                }

                                if (spaceSimulationResult_Cooling.TryGetValue(SpaceSimulationResultParameter.OccupancySensibleGain, out double occupancySensibleGain) && !double.IsNaN(occupancySensibleGain))
                                {
                                    values[i, 56] = occupancySensibleGain;
                                }

                                if (spaceSimulationResult_Cooling.TryGetValue(SpaceSimulationResultParameter.EquipmentSensibleGain, out double equipmentSensibleGain) && !double.IsNaN(equipmentSensibleGain))
                                {
                                    values[i, 57] = equipmentSensibleGain;
                                }

                                if (spaceSimulationResult_Cooling.TryGetValue(SpaceSimulationResultParameter.EquipmentLatentGain, out double equipmentLatentGain) && !double.IsNaN(equipmentLatentGain))
                                {
                                    values[i, 58] = equipmentLatentGain;
                                }

                                if (spaceSimulationResult_Cooling.TryGetValue(SpaceSimulationResultParameter.OccupancyLatentGain, out double occupancyLatentGain) && !double.IsNaN(occupancyLatentGain))
                                {
                                    values[i, 59] = occupancyLatentGain;
                                }

                                if (spaceSimulationResult_Cooling.TryGetValue(SpaceSimulationResultParameter.Load, out double load) && !double.IsNaN(load))
                                {
                                    values[i, 63] = load;
                                }

                                if (spaceSimulationResult_Cooling.TryGetValue(SpaceSimulationResultParameter.DryBulbTempearture, out double dryBulbTempearture) && !double.IsNaN(dryBulbTempearture))
                                {
                                    values[i, 64] = dryBulbTempearture;
                                }

                                if (spaceSimulationResult_Cooling.TryGetValue(SpaceSimulationResultParameter.ResultantTemperature, out double resultantTemperature) && !double.IsNaN(resultantTemperature))
                                {
                                    values[i, 65] = resultantTemperature;
                                }

                                if (spaceSimulationResult_Cooling.TryGetValue(SpaceSimulationResultParameter.MaxDryBulbTemperature, out double maxDryBulbTemperature) && !double.IsNaN(maxDryBulbTemperature))
                                {
                                    values[i, 79] = maxDryBulbTemperature;
                                }

                                if (spaceSimulationResult_Cooling.TryGetValue(SpaceSimulationResultParameter.MinDryBulbTemperature, out double minDryBulbTemperature) && !double.IsNaN(minDryBulbTemperature))
                                {
                                    values[i, 80] = minDryBulbTemperature;
                                }

                                if (spaceSimulationResult_Cooling.TryGetValue(SpaceSimulationResultParameter.MaxDryBulbTemperatureIndex, out int maxDryBulbTemperatureIndex) && !double.IsNaN(maxDryBulbTemperatureIndex))
                                {
                                    values[i, 81] = Analytical.Convert.ToDateTime(maxDryBulbTemperatureIndex, year);
                                }

                                if (spaceSimulationResult_Cooling.TryGetValue(SpaceSimulationResultParameter.MinDryBulbTemperatureIndex, out int minDryBulbTemperatureIndex) && !double.IsNaN(minDryBulbTemperatureIndex))
                                {
                                    values[i, 82] = Analytical.Convert.ToDateTime(minDryBulbTemperatureIndex, year);
                                }

                                if (spaceSimulationResult_Cooling.TryGetValue(SpaceSimulationResultParameter.DesignDayTemperature, out double designDayTemperature) && !double.IsNaN(designDayTemperature))
                                {
                                    values[i, 111] = designDayTemperature;
                                }

                                if (spaceSimulationResult_Cooling.TryGetValue(SpaceSimulationResultParameter.DesignDayRelativeHumidity, out double designDayRelativeHumidity) && !double.IsNaN(designDayRelativeHumidity))
                                {
                                    values[i, 112] = designDayRelativeHumidity;
                                }
                            }
                        }
                    }

                    return Core.Excel.Modify.Write(worksheet, values, 3, 1, Core.Excel.ClearOption.None);
                });

                progressForm.Update("Writing Data");
                bool written = Core.Excel.Modify.Edit(path, "Data", func);
                if (!written)
                {
                    return;
                }

                if (max == int.MinValue || max == 0)
                {
                    return;
                }

                progressForm.Update("Printing Data");
                Core.Excel.Modify.TryRunMacro(path, true, "PrintRange", min, max);

                progressForm.Update("Finishing");
            }


        }
    }
}