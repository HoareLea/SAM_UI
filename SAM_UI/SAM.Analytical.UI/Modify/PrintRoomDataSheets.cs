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

            PrindRoomDataSheets(analyticalModel, directory, owner);
        }

        public static void PrindRoomDataSheets(this AnalyticalModel analyticalModel, string directory = null, IWin32Window owner = null)
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

            path_Template = System.IO.Path.Combine(path_Template, "PDF_Print_RDS.xlsm");
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

            string path = System.IO.Path.Combine(directory, System.IO.Path.GetFileName(path_Template));

            System.IO.File.Copy(path_Template, path);

            int min = 1;
            int max = int.MinValue;

            Func<Worksheet, bool> func = new Func<Worksheet, bool>((Worksheet worksheet) =>
            {
                if (worksheet == null)
                {
                    return false;
                }

                worksheet.Range("A3").End(NetOffice.ExcelApi.Enums.XlDirection.xlDown).Clear();

                object[,] values = new object[spaces.Count, 83];
                for (int i = 0; i < spaces.Count; i++)
                {

                    Space space = spaces[i];
                    if (space == null)
                    {
                        continue;
                    }

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

                        }

                        values[i, 22] = spaceSimulationResults.ConvertAll(x => x.DateTime).Max();

                        SpaceSimulationResult spaceSimulationResult_Cooling = spaceSimulationResults.Find(x => x.HasValue(SpaceSimulationResultParameter.LoadType) && LoadType.Cooling.ToString().Equals(x.GetValue(SpaceSimulationResultParameter.LoadType)));
                        if (spaceSimulationResult_Cooling != null)
                        {
                            if (spaceSimulationResult_Cooling.TryGetValue(SpaceSimulationResultParameter.SizingMethod, out string sizingMethod) && !string.IsNullOrWhiteSpace(sizingMethod))
                            {
                                values[i, 32] = sizingMethod;
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

            if(max == int.MinValue)
            {
                return;
            }

            Core.Excel.Modify.TryRunMacro(path, true, "PrintRange", min, max);
        }
    }
}