using SAM.Core.UI;
using SAM.Geometry.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace SAM.Analytical.UI
{
    public static partial class Query
    {
        public static Dictionary<Guid, LegendItem> LegendItemDictionary(this IEnumerable<Space> spaces, AdjacencyCluster adjacencyCluster = null, ViewSettings viewSettings = null)
        {
            if(spaces == null)
            {
                return null;
            }

            Dictionary<Space, object> dictionary_Values = new Dictionary<Space, object>();
            
            Dictionary<double, string> dictionary_Doubles = new Dictionary<double, string>();


            HashSet<string> strings = new HashSet<string>();

            Dictionary<Guid, LegendItem> result = new Dictionary<Guid, LegendItem>();
            foreach (Space space in spaces)
            {
                if (!TryGetValue(space, adjacencyCluster, viewSettings, out object @object, out string text))
                {
                    @object = null;
                }

                dictionary_Values[space] = @object;
                if(Core.Query.IsNumeric(@object))
                {
                    double value = System.Convert.ToDouble(@object);
                    dictionary_Doubles[value] = SAM.Core.Query.Round(value, Core.Tolerance.MacroDistance).ToString();
                    dictionary_Values[space] = value;
                }
                else if(@object is string)
                {
                    strings.Add((string)@object);
                }
                else if(Core.Query.TryConvert(@object, out Color color))
                {
                    result[space.Guid] = new LegendItem(color, text);
                }
                else if (@object?.ToString() != null)
                {
                    string value = @object?.ToString();
                    strings.Add(value);
                    dictionary_Values[space] = value;
                }
            }

            double min = double.MinValue;
            double max = double.MaxValue;
            if(dictionary_Doubles != null && dictionary_Doubles.Count != 0)
            {
                List<double> doubles = new List<double>(dictionary_Doubles.Keys);

                max = doubles.Max();
                min = doubles.Min();
                double step = (max - min) / doubles.Count;
                List<Color> colors = Core.Create.Colors(System.Drawing.Color.Red, doubles.Count + 1);

                int index = 0;
                double current = min;
                while(current <= max)
                {
                    double next = current + step;
                    foreach (KeyValuePair<double, string> keyValuePair_Double in dictionary_Doubles)
                    {
                        if(keyValuePair_Double.Key >= current && keyValuePair_Double.Key < next)
                        {
                            foreach (KeyValuePair<Space, object> keyValuePair_Space in dictionary_Values)
                            {
                                if(!(keyValuePair_Space.Value is double))
                                {
                                    continue;
                                }

                                double value = (double)keyValuePair_Space.Value;
                                if(value == keyValuePair_Double.Key)
                                {
                                    result[keyValuePair_Space.Key.Guid] = new LegendItem(colors[index], keyValuePair_Double.Value);
                                }

                            }
                        }
                    }

                    current += step;
                    index++;
                }
            }

            Random random = new Random();
            if (strings != null && strings.Count != 0)
            {
                foreach(string @string in strings)
                {
                    Color color = System.Drawing.Color.FromArgb(random.Next(0, 256), random.Next(0, 256), random.Next(0, 256));
                    foreach(KeyValuePair<Space, object> keyValuePair in dictionary_Values)
                    {
                        if(keyValuePair.Value?.ToString() == @string)
                        {
                            result[keyValuePair.Key.Guid] = new LegendItem(color, @string);
                        }
                    }
                }
            }

            return result;
        }
    }
}