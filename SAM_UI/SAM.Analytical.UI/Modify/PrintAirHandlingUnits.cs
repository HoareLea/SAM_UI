using System;
using System.Collections.Generic;
using NetOffice.ExcelApi;
using SAM.Analytical.Mollier;

namespace SAM.Analytical.UI
{
    public static partial class Modify
    {
        public static void PrintAirHandlingUnits(this AnalyticalModel analyticalModel, string path_Excel, string worksheetName)
        {
            if(analyticalModel == null || string.IsNullOrWhiteSpace(path_Excel) || !System.IO.File.Exists(path_Excel) || string.IsNullOrWhiteSpace(worksheetName))
            {
                return;
            }

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;

            List<AirHandlingUnitResult> airHandlingUnitResults = adjacencyCluster?.GetObjects<AirHandlingUnitResult>();
            if(airHandlingUnitResults == null || airHandlingUnitResults.Count == 0)
            {
                return;
            }

            Func<Workbook, bool> func = new Func<Workbook, bool>((Workbook workbook) => 
            {
                if(workbook == null)
                {
                    return false;
                }

                Worksheet workseet_Template = Core.Excel.Query.Worksheet(workbook, worksheetName);
                if(workseet_Template == null)
                {
                    return false;
                }

                foreach (AirHandlingUnitResult airHandlingUnitResult in airHandlingUnitResults)
                {
                    string name = airHandlingUnitResult?.Name;
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        continue;
                    }

                    Worksheet workseet = Core.Excel.Query.Worksheet(workbook, name);
                    if(workseet != null)
                    {
                        continue;
                    }

                    workseet = Core.Excel.Modify.Copy(workseet_Template, name);

                    workseet.Cells[8, 4].Value = name;

                    AirSupplyMethod airSupplyMethod = Analytical.Query.AirSupplyMethod(adjacencyCluster, name);
                    if(airSupplyMethod != AirSupplyMethod.Undefined)
                    {
                        workseet.Cells[9, 4].Value = airSupplyMethod == AirSupplyMethod.Total ? "TSA" : "OSA";
                    }

                    double @double;

                    if(airHandlingUnitResult.TryGetValue(AirHandlingUnitResultParameter.SupplyAirFlow, out @double) && !double.IsNaN(@double))
                    {
                        workseet.Cells[10, 4].Value = @double;
                    }

                    if (airHandlingUnitResult.TryGetValue(AirHandlingUnitResultParameter.OutsideSupplyAirFlow, out @double) && !double.IsNaN(@double))
                    {
                        workseet.Cells[11, 4].Value = @double;
                    }

                    if (airHandlingUnitResult.TryGetValue(AirHandlingUnitResultParameter.WinterDesignTemperature, out @double) && !double.IsNaN(@double))
                    {
                        workseet.Cells[17, 4].Value = @double;
                    }

                    if (airHandlingUnitResult.TryGetValue(AirHandlingUnitResultParameter.WinterDesignRelativeHumidity, out @double) && !double.IsNaN(@double))
                    {
                        workseet.Cells[19, 4].Value = @double;
                    }

                    if (airHandlingUnitResult.TryGetValue(AirHandlingUnitResultParameter.SummerDesignTemperature, out @double) && !double.IsNaN(@double))
                    {
                        workseet.Cells[17, 6].Value = @double;
                    }

                    if (airHandlingUnitResult.TryGetValue(AirHandlingUnitResultParameter.SummerDesignRelativeHumidity, out @double) && !double.IsNaN(@double))
                    {
                        workseet.Cells[19, 6].Value = @double;
                    }
                }

                return true;
            });

            Core.Excel.Modify.Edit(path_Excel, func);
        }
    }
}