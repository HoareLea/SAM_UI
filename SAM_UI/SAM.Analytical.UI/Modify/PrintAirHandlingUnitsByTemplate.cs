using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using NetOffice.ExcelApi;
using SAM.Analytical.Mollier;

namespace SAM.Analytical.UI
{
    public static partial class Modify
    {
        public static void PrintAirHandlingUnitsByTemplate(this AnalyticalModel analyticalModel, string path_Excel, string directory, string worksheetName, IEnumerable<string> aHUNames = null)
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

                    if(aHUNames != null && aHUNames.Count() != 0)
                    {
                        if (!aHUNames.Contains(name))
                        {
                            continue;
                        }
                    }

                    Worksheet worksheet = Core.Excel.Query.Worksheet(workbook, name);
                    if(worksheet != null)
                    {
                        worksheet.Delete();
                    }

                    worksheet = Core.Excel.Modify.Copy(workseet_Template, name);

                    Dictionary<string, object> dictionary = new Dictionary<string, object>();
                    dictionary["[Name]"] = name;

                    AirSupplyMethod airSupplyMethod = Analytical.Query.AirSupplyMethod(adjacencyCluster, name);
                    if(airSupplyMethod != AirSupplyMethod.Undefined)
                    {
                        dictionary["[AirSupplyMethod]"] = airSupplyMethod == AirSupplyMethod.Total ? "TSA" : "OSA";
                    }

                    double @double;

                    if(airHandlingUnitResult.TryGetValue(AirHandlingUnitResultParameter.SupplyAirFlow, out @double) && !double.IsNaN(@double))
                    {
                        dictionary["[SupplyAirFlow]"] = @double;
                    }

                    if (airHandlingUnitResult.TryGetValue(AirHandlingUnitResultParameter.OutsideSupplyAirFlow, out @double) && !double.IsNaN(@double))
                    {
                        dictionary["[OutsideSupplyAirFlow]"] = @double;
                    }

                    if (airHandlingUnitResult.TryGetValue(AirHandlingUnitResultParameter.WinterSpaceTemperature, out @double) && !double.IsNaN(@double))
                    {
                        dictionary["[WinterSpaceTemperature]"] = @double;
                    }

                    if (airHandlingUnitResult.TryGetValue(AirHandlingUnitResultParameter.WinterSpaceRelativeHumidty, out @double) && !double.IsNaN(@double))
                    {
                        dictionary["[WinterSpaceRelativeHumidty]"] = @double / 100;
                    }

                    if (airHandlingUnitResult.TryGetValue(AirHandlingUnitResultParameter.SummerSpaceTemperature, out @double) && !double.IsNaN(@double))
                    {
                        dictionary["[SummerSpaceTemperature]"] = @double;
                    }

                    if (airHandlingUnitResult.TryGetValue(AirHandlingUnitResultParameter.SummerSpaceRelativeHumidty, out @double) && !double.IsNaN(@double))
                    {
                        dictionary["[SummerSpaceRelativeHumidty]"] = @double / 100;
                    }

                    if (airHandlingUnitResult.TryGetValue(AirHandlingUnitResultParameter.WinterDesignTemperature, out @double) && !double.IsNaN(@double))
                    {
                        dictionary["[WinterDesignTemperature]"] = @double;

                    }

                    if (airHandlingUnitResult.TryGetValue(AirHandlingUnitResultParameter.WinterDesignRelativeHumidity, out @double) && !double.IsNaN(@double))
                    {
                        dictionary["[WinterDesignRelativeHumidity]"] = @double / 100;

                    }

                    if (airHandlingUnitResult.TryGetValue(AirHandlingUnitResultParameter.SummerDesignTemperature, out @double) && !double.IsNaN(@double))
                    {
                        dictionary["[SummerDesignTemperature]"] = @double;
                    }

                    if (airHandlingUnitResult.TryGetValue(AirHandlingUnitResultParameter.SummerDesignRelativeHumidity, out @double) && !double.IsNaN(@double))
                    {
                        dictionary["[SummerDesignRelativeHumidity]"] = @double / 100;
                    }

                    if (airHandlingUnitResult.TryGetValue(AirHandlingUnitResultParameter.FrostCoilOffTemperature, out @double) && !double.IsNaN(@double))
                    {
                        dictionary["[FrostCoilOffTemperature]"] = @double;
                    }

                    if (airHandlingUnitResult.TryGetValue(AirHandlingUnitResultParameter.WinterHeatRecoverySensibleEfficiency, out @double) && !double.IsNaN(@double))
                    {
                        dictionary["[WinterHeatRecoverySensibleEfficiency]"] = @double / 100;
                    }

                    if (airHandlingUnitResult.TryGetValue(AirHandlingUnitResultParameter.WinterHeatRecoveryLatentEfficiency, out @double) && !double.IsNaN(@double))
                    {
                        dictionary["[WinterHeatRecoveryLatentEfficiency]"] = @double / 100;
                    }

                    if (airHandlingUnitResult.TryGetValue(AirHandlingUnitResultParameter.SummerHeatRecoverySensibleEfficiency, out @double) && !double.IsNaN(@double))
                    {
                        dictionary["[SummerHeatRecoverySensibleEfficiency]"] = @double / 100;
                    }

                    if (airHandlingUnitResult.TryGetValue(AirHandlingUnitResultParameter.SummerHeatRecoveryLatentEfficiency, out @double) && !double.IsNaN(@double))
                    {
                        dictionary["[SummerHeatRecoveryLatentEfficiency]"] = @double / 100;
                    }

                    if (airHandlingUnitResult.TryGetValue(AirHandlingUnitResultParameter.CoolingCoilFluidFlowTemperature, out @double) && !double.IsNaN(@double))
                    {
                        dictionary["[CoolingCoilFluidFlowTemperature]"] = @double;
                    }

                    if (airHandlingUnitResult.TryGetValue(AirHandlingUnitResultParameter.CoolingCoilFluidReturnTemperature, out @double) && !double.IsNaN(@double))
                    {
                        dictionary["[CoolingCoilFluidReturnTemperature]"] = @double;
                    }

                    if (airHandlingUnitResult.TryGetValue(AirHandlingUnitResultParameter.WinterHeatingCoilSupplyTemperature, out @double) && !double.IsNaN(@double))
                    {
                        dictionary["[WinterHeatingCoilSupplyTemperature]"] = @double;
                    }

                    //TODO: Insert Data

                    Insert(worksheet.UsedRange, dictionary);

                    Range range = FindRange(worksheet.UsedRange, "[MollierChart]");

                    float left = (float)(double)range.Left;
                    float top = (float)(double)range.Top;


                    //float right = left + (float)(double)range.Width;
                    //float bottom = top - left + (float)(double)range.Height;

                    string path = null;

                    Image image = Image.FromFile(path);

                    worksheet.Shapes.AddPicture(path, NetOffice.OfficeApi.Enums.MsoTriState.msoFalse, NetOffice.OfficeApi.Enums.MsoTriState.msoCTrue, left, top, image.Width, image.Height);
                    range.Value = string.Empty;

                    worksheet.ExportAsFixedFormat(NetOffice.ExcelApi.Enums.XlFixedFormatType.xlTypePDF, System.IO.Path.Combine(directory, name + ".pdf"));
                }

                return true;
            });

            Core.Excel.Modify.Edit(path_Excel, func);
        }

        private static Range FindRange(Range range, string value)
        {
            if (range == null || string.IsNullOrEmpty(value))
            {
                return null;
            }

            object[,] values = range.Value as object[,];
            if (values == null || values.GetLength(0) == 0 || values.GetLength(1) == 0)
            {
                return null;
            }


            for (int i = values.GetLowerBound(0); i <= values.GetUpperBound(0); i++)
            {
                for (int j = values.GetLowerBound(1); j <= values.GetUpperBound(1); j++)
                {
                    object @object = values[i, j];
                    if (@object is string)
                    {
                        //TODO: get Valid Range
                        //return range.Range;
                    }
                }
            }

            return null;
        }

        private static void Insert(Range range, Dictionary<string, object> dictionary)
        {
            if (range == null || dictionary == null || dictionary.Count == 0)
            {
                return;
            }

            object[,] values = range.Value as object[,];
            if (values == null || values.GetLength(0) == 0 || values.GetLength(1) == 0)
            {
                return;
            }

            for (int i = values.GetLowerBound(0); i <= values.GetUpperBound(0); i++)
            {
                for (int j = values.GetLowerBound(1); j <= values.GetUpperBound(1); j++)
                {
                    string value = (values[i, j] as string)?.Trim();
                    if (string.IsNullOrEmpty(value))
                    {
                        continue;
                    }

                    if (!value.Contains("[") || !value.Contains("]"))
                    {
                        continue;
                    }

                    foreach (KeyValuePair<string, object> keyValuePair in dictionary)
                    {
                        string id = keyValuePair.Key;

                        if (!value.Contains(id))
                        {
                            continue;
                        }

                        object value_New = keyValuePair.Value;

                        object @object = values[i, j];
                        if (@object is string)
                        {
                            values[i, j] = ((string)@object).Replace(string.Format("{0}", id), value_New?.ToString() == null ? string.Empty : value_New.ToString());
                        }
                        else
                        {
                            values[i, j] = value_New;
                        }
                    }
                }
            }

            range.Value = values;
        }
    }
}