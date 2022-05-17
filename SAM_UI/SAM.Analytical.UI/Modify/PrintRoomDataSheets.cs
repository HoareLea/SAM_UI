using NetOffice.ExcelApi;
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

            string path_Template = Core.Query.TemplatesDirectory(typeof(AnalyticalModel).Assembly);
            if (!System.IO.Directory.Exists(path_Template))
            {
                return;
            }

            path_Template = System.IO.Path.Combine(path_Template, "RDS","PDF_Print_RDS.xlsm");
            if (!System.IO.File.Exists(path_Template))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(directory) || !System.IO.Directory.Exists(directory))
            {
                using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog())
                {
                    if(folderBrowserDialog.ShowDialog(owner) != DialogResult.OK)
                    {
                        return;
                    }

                    directory = folderBrowserDialog.SelectedPath;
                }
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

                worksheet.Range("A3").End(NetOffice.ExcelApi.Enums.XlDirection.xlDown).Clear();

                max = 0;

                object[,] values = new object[spaces.Count, 83];
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
                    values[i, 5] = Analytical.Query.CalculatedOccupancy(space);

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
                    if(double.IsNaN(coolingDesignTemperature))
                    {
                        values[i, 67] = coolingDesignTemperature;
                    }

                    double heatingDesignTemperature = Analytical.Query.HeatingDesignTemperature(space, analyticalModel);
                    if (double.IsNaN(heatingDesignTemperature))
                    {
                        values[i, 68] = heatingDesignTemperature;
                    }

                    double specificOccupancySensibleGain = space.SpecificOccupancySensibleGain();
                    if(!double.IsNaN(specificOccupancySensibleGain))
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
                    if(ventilationSystem != null)
                    {
                        if(ventilationSystem.TryGetValue(VentilationSystemParameter.SupplyUnitName, out string supplyUnitName))
                        {
                            values[i, 6] = supplyUnitName;
                        }

                        if (ventilationSystem.TryGetValue(VentilationSystemParameter.ExhaustUnitName, out string exhaustUnitName))
                        {
                            values[i, 7] = exhaustUnitName;
                        }

                        VentilationSystemType ventilationSystemType = ventilationSystem.Type as VentilationSystemType;
                        if(ventilationSystemType != null)
                        {
                            if(!string.IsNullOrWhiteSpace(ventilationSystemType.Name))
                            {
                                values[i, 9] = ventilationSystemType.Name;
                            }
                        }
                    }

                    if(internalCondition != null)
                    {
                        values[i, 8] = internalCondition.Name;

                        if(internalCondition.TryGetValue(InternalConditionParameter.InfiltrationAirChangesPerHour, out double infiltrationAirChangesPerHour) && !double.IsNaN(infiltrationAirChangesPerHour))
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
                    if(heatingSystem != null)
                    {
                        HeatingSystemType heatingSystemType = heatingSystem.Type as HeatingSystemType;
                        if(heatingSystemType != null)
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
                    if(spaceSimulationResults != null && spaceSimulationResults.Count != 0)
                    {
                        SpaceSimulationResult spaceSimulationResult_Heating = spaceSimulationResults.Find(x => x.HasValue(SpaceSimulationResultParameter.LoadType) && LoadType.Heating.ToString().Equals(x.GetValue(SpaceSimulationResultParameter.LoadType)));
                        if(spaceSimulationResult_Heating != null)
                        {
                            if(values[i, 17] == null)
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
                            if(!double.IsNaN(specificDesignLoad))
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
                        }

                        values[i, 22] = spaceSimulationResults.ConvertAll(x => x.DateTime).Max();

                        SpaceSimulationResult spaceSimulationResult_Cooling = spaceSimulationResults.Find(x => x.HasValue(SpaceSimulationResultParameter.LoadType) && LoadType.Cooling.ToString().Equals(x.GetValue(SpaceSimulationResultParameter.LoadType)));
                        if (spaceSimulationResult_Cooling != null)
                        {
                            if(spaceSimulationResult_Cooling.TryGetValue(SpaceSimulationResultParameter.LoadIndex, out int loadIndex))
                            {
                                values[i, 18] = loadIndex;
                            }

                            if (spaceSimulationResult_Cooling.TryGetValue(SpaceSimulationResultParameter.RelativeHumidity, out double relativeHumidity))
                            {
                                values[i, 23] = loadIndex;
                            }

                            if (loadIndex != -1)
                            {
                                values[i, 30] = Convert.ToDateTime(loadIndex, year);
                            }

                            if (spaceSimulationResult_Cooling.TryGetValue(SpaceSimulationResultParameter.HumidityRatio, out double humidityRatio))
                            {
                                values[i, 31] = humidityRatio;
                            }

                            if (spaceSimulationResult_Cooling.TryGetValue(SpaceSimulationResultParameter.SizingMethod, out string sizingMethod) && !string.IsNullOrWhiteSpace(sizingMethod))
                            {
                                values[i, 32] = sizingMethod;
                            }

                            double specificBuildingHeatTransferGain = spaceSimulationResult_Cooling.SpecificBuildingHeatTransferGain();
                            if(!double.IsNaN(specificBuildingHeatTransferGain))
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
                                values[i, 81] = Convert.ToDateTime(maxDryBulbTemperatureIndex, year);
                            }

                            if (spaceSimulationResult_Cooling.TryGetValue(SpaceSimulationResultParameter.MinDryBulbTemperatureIndex, out int minDryBulbTemperatureIndex) && !double.IsNaN(minDryBulbTemperatureIndex))
                            {
                                values[i, 82] = Convert.ToDateTime(minDryBulbTemperatureIndex, year);
                            }
                        }
                    }
                }

                return Core.Excel.Modify.Write(worksheet, values, 3, 1, Core.Excel.ClearOption.None);
            });

            bool written = Core.Excel.Modify.Write(path, "Data", func);
            if(!written)
            {
                return;
            }

            if(max == int.MinValue || max == 0)
            {
                return;
            }

            Core.Excel.Modify.TryRunMacro(path, true, "PrintRange", min, max);
        }
    }
}