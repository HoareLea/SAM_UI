using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using NetOffice.ExcelApi;
using SAM.Analytical.Mollier;
using SAM.Core.Mollier;
using SAM.Core.Mollier.UI;
using SAM.Core.Mollier.UI.Controls;

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

            List<string> paths = new List<string>();

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
                    string name = airHandlingUnitResult?.Name + "TEST";
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        continue;
                    }

                    if (aHUNames != null && aHUNames.Count() != 0)
                    {
                        if (!aHUNames.Contains(name))
                        {
                            continue;
                        }
                    }

                    Worksheet worksheet = Core.Excel.Query.Worksheet(workbook, name);
                    if (worksheet != null)
                    {
                        worksheet.Delete();
                    }

                    worksheet = Core.Excel.Modify.Copy(workseet_Template, name);

                    Dictionary<string, object> dictionary = new Dictionary<string, object>();
                    dictionary["[Name]"] = name;

                    AirSupplyMethod airSupplyMethod = Analytical.Query.AirSupplyMethod(adjacencyCluster, name);
                    if (airSupplyMethod != AirSupplyMethod.Undefined)
                    {
                        dictionary["[AirSupplyMethod]"] = airSupplyMethod == AirSupplyMethod.Total ? "TSA" : "OSA";
                    }

                    double @double;

                    if (airHandlingUnitResult.TryGetValue(AirHandlingUnitResultParameter.SupplyAirFlow, out @double) && !double.IsNaN(@double))
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

                    Range range = FindRange(worksheet.UsedRange, "[Mollier_Chart]");
                    Range range_2 = FindRange(worksheet.UsedRange, "[Psychrometric_Chart]");

                    if(range == null || range_2 == null)
                    {
                        continue;
                    }

                    float left = (float)(double)range.Left;
                    float left_2 = (float)(double)range_2.Left;
                    float top = (float)(double)range.Top;
                    float top_2 = (float)(double)range_2.Top;

                    float width = (float)(double)range.Width;
                    float width_2 = (float)(double)range_2.Width;
                    float height = (float)(double)range.Height;
                    float height_2 = (float)(double)range_2.Height;

                    string path = System.IO.Path.GetTempFileName();
                    string path_2 = System.IO.Path.GetTempFileName();
                    
                    paths.Add(path);
                    paths.Add(path_2);

                    Mollier.Modify.UpdateMollierProcesses(new AirHandlingUnitResult(airHandlingUnitResult), out List<IMollierProcess> mollierProcesses);

                    using (MollierControl mollierControl = new MollierControl() { Visible = false })
                    {
                        MollierControlSettings mollierControlSettings = new MollierControlSettings();
                        mollierControlSettings.HumidityRatio_Max = 25;
                        mollierControlSettings.ChartType = ChartType.Mollier;
                        mollierControl.MollierControlSettings = mollierControlSettings;
                        
                        mollierProcesses?.ForEach(x => mollierControl.AddProcess(x, false));


                        //StartPosition = FormStartPosition.Manual;
                        //mollierControl.WindowState = System.Windows.Forms.FormWindowState.Normal;
                        //mollierControl.Location = new System.Drawing.Point(0, 0);
                        mollierControl.Size = new Size(System.Convert.ToInt32(width * 2), System.Convert.ToInt32(height * 2));
                        mollierControl.Refresh();
                        mollierControl.Save("EMF", 18, path: path);


                        mollierControlSettings.HumidityRatio_Max = 30;
                        //mollierForm.WindowState = System.Windows.Forms.FormWindowState.Normal;
                        //mollierForm.Location = new System.Drawing.Point(0, 0);
                        mollierControl.Size = new Size(System.Convert.ToInt32(width_2 * 2), System.Convert.ToInt32(height_2 * 2));
                        //mollierForm.Refresh();
                        mollierControlSettings.ChartType = ChartType.Psychrometric; 
                        mollierControl.MollierControlSettings = mollierControlSettings;
                        
                        mollierControl.Save("EMF", 18, path: path_2);
                    }

                    float width_Picture = float.NaN;
                    float height_Picture = float.NaN;




                    //using (Image image = Image.FromFile(path))
                    //{
                    //    width_Picture = image.Width;
                    //    height_Picture = image.Height;
                    //    image.Dispose();
                    //}


                        

                    worksheet.Shapes.AddPicture(path, NetOffice.OfficeApi.Enums.MsoTriState.msoFalse, NetOffice.OfficeApi.Enums.MsoTriState.msoCTrue, left, top, width, height);

                    worksheet.Shapes.AddPicture(path_2, NetOffice.OfficeApi.Enums.MsoTriState.msoFalse, NetOffice.OfficeApi.Enums.MsoTriState.msoCTrue, left_2, top_2, width_2, height_2);
                    
                    range.Value = string.Empty;

                    string path_Pdf = System.IO.Path.Combine(directory, name + ".pdf");
                    if(System.IO.File.Exists(path_Pdf))
                    {
                        System.IO.File.Delete(path_Pdf);
                    }

                    worksheet.ExportAsFixedFormat(NetOffice.ExcelApi.Enums.XlFixedFormatType.xlTypePDF, path_Pdf);
                }

                return false;
            });

            Core.Excel.Modify.Edit(path_Excel, func);

            System.Threading.Thread.Sleep(1000);

            if(paths != null && paths.Count != 0)
            {
                foreach(string path in paths)
                {
                    if(System.IO.File.Exists(path))
                    {
                        if(Core.Query.WaitToUnlock(path))
                        {
                            System.IO.File.Delete(path);
                        }
                    }
                }
            }
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
                        if ((string)@object == value)
                        {
                            Range range_Temp = range.Cells[i, j];
                            return range.Range(range_Temp, range_Temp.MergeArea);
                        }
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