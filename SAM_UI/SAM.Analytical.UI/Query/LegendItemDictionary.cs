// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace SAM.Analytical.UI
{
    public static partial class Query
    {   
        public static Dictionary<Guid, LegendItem> LegendItemDictionary(this IEnumerable<LegendItemData> legendItemDatas, bool editable = true, LegendItem undefinedLegendItem = null)
        {
            if (legendItemDatas == null)
            {
                return null;
            }

            Dictionary<LegendItemData, object> dictionary_Values = new Dictionary<LegendItemData, object>();

            Dictionary<double, string> dictionary_Doubles = new Dictionary<double, string>();


            //HashSet<string> strings = new HashSet<string>();
            Dictionary<string, List<LegendItemData>> dictionary_Strings = new Dictionary<string, List<LegendItemData>>();

            Dictionary<Guid, LegendItem> result = new Dictionary<Guid, LegendItem>();
            foreach (LegendItemData legendItemData in legendItemDatas)
            {
                if (legendItemData?.SAMObject == null)
                {
                    continue;
                }

                object @object = legendItemData.Value;

                dictionary_Values[legendItemData] = @object;
                if(@object is Enum)
                {
                    string value = Core.Query.Description((Enum)@object);
                    if(!dictionary_Strings.TryGetValue(value, out List<LegendItemData> legendItemDatas_Temp))
                    {
                        legendItemDatas_Temp = new List<LegendItemData>();
                        dictionary_Strings[value] = legendItemDatas_Temp;
                    }
                    legendItemDatas_Temp.Add(legendItemData);
                }
                else if (Core.Query.IsNumeric(@object))
                {
                    double value = System.Convert.ToDouble(@object);
                    dictionary_Doubles[value] = Core.Query.Round(value, Core.Tolerance.MacroDistance).ToString();
                    dictionary_Values[legendItemData] = value;
                }
                else if (@object is string)
                {
                    string value = (string)@object;
                    if (!dictionary_Strings.TryGetValue(value, out List<LegendItemData> legendItemDatas_Temp))
                    {
                        legendItemDatas_Temp = new List<LegendItemData>();
                        dictionary_Strings[value] = legendItemDatas_Temp;
                    }
                    legendItemDatas_Temp.Add(legendItemData);
                }
                else if (Core.Query.TryConvert(@object, out Color color))
                {
                    result[legendItemData.Guid] = new LegendItem(color, legendItemData.Text) { Editable = editable };
                }
                else if (@object?.ToString() != null)
                {
                    string value = @object?.ToString();
                    if (!dictionary_Strings.TryGetValue(value, out List<LegendItemData> legendItemDatas_Temp))
                    {
                        legendItemDatas_Temp = new List<LegendItemData>();
                        dictionary_Strings[value] = legendItemDatas_Temp;
                    }
                    legendItemDatas_Temp.Add(legendItemData);
                    dictionary_Values[legendItemData] = value;
                }
            }

            double min = double.MinValue;
            double max = double.MaxValue;
            if (dictionary_Doubles != null && dictionary_Doubles.Count != 0)
            {
                List<double> doubles = new List<double>(dictionary_Doubles.Keys);

                max = doubles.Max();
                min = doubles.Min();
                double step = (max - min) / doubles.Count;

                Color color_Start = System.Drawing.Color.Red;

                if (step == 0)
                {
                    foreach (KeyValuePair<double, string> keyValuePair_Double in dictionary_Doubles)
                    {
                        if (keyValuePair_Double.Key == max || keyValuePair_Double.Key == min)
                        {
                            foreach (KeyValuePair<LegendItemData, object> keyValuePair_Space in dictionary_Values)
                            {
                                if (!(keyValuePair_Space.Value is double))
                                {
                                    continue;
                                }

                                double value = (double)keyValuePair_Space.Value;
                                if (value == keyValuePair_Double.Key)
                                {
                                    result[keyValuePair_Space.Key.Guid] = new LegendItem(color_Start, keyValuePair_Double.Value) { Editable = editable };
                                }

                            }
                        }
                    }
                }
                else
                {
                    List<Color> colors = Core.Create.Colors(color_Start, doubles.Count + 1, 0.1, 0.9);

                    int index = 0;
                    double current = min;
                    while (current <= max)
                    {
                        double next = current + step;
                        foreach (KeyValuePair<double, string> keyValuePair_Double in dictionary_Doubles)
                        {
                            if (keyValuePair_Double.Key >= current && keyValuePair_Double.Key < next)
                            {
                                foreach (KeyValuePair<LegendItemData, object> keyValuePair_Space in dictionary_Values)
                                {
                                    if (!(keyValuePair_Space.Value is double))
                                    {
                                        continue;
                                    }

                                    double value = (double)keyValuePair_Space.Value;
                                    if (value == keyValuePair_Double.Key)
                                    {
                                        result[keyValuePair_Space.Key.Guid] = new LegendItem(colors[index], keyValuePair_Double.Value) { Editable = editable };
                                    }

                                }
                            }
                        }

                        current += step;
                        index++;
                    }
                }
            }

            Random random = new Random();
            if (dictionary_Strings != null && dictionary_Strings.Count != 0)
            {
                foreach (KeyValuePair<string, List<LegendItemData>> keyValuePair in dictionary_Strings)
                {
                    Color color = System.Drawing.Color.FromArgb(random.Next(0, 256), random.Next(0, 256), random.Next(0, 256));
                    foreach (LegendItemData legendItemData in keyValuePair.Value)
                    {
                        result[legendItemData.Guid] = new LegendItem(color, keyValuePair.Key);
                    }
                }
            }

            if (undefinedLegendItem != null)
            {
                foreach (LegendItemData legendItemData in legendItemDatas)
                {
                    if (legendItemData == null || result.ContainsKey(legendItemData.Guid))
                    {
                        continue;
                    }

                    result[legendItemData.Guid] = undefinedLegendItem;
                }
            }

            return result;
        }
    }
}
