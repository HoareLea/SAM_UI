using SAM.Geometry.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace SAM.Analytical.UI
{
    public static partial class Query
    {
        public static Dictionary<Space, Color> Colors(this IEnumerable<Space> spaces, AdjacencyCluster adjacencyCluster = null, ViewSettings viewSettings = null)
        {
            if(spaces == null)
            {
                return null;
            }

            Dictionary<Space, object> dictionary_Values = new Dictionary<Space, object>();
            HashSet<double> doubles = new HashSet<double>();
            HashSet<string> strings = new HashSet<string>();

            Dictionary<Space, Color> result = new Dictionary<Space, Color>();
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
                    doubles.Add(value);
                    dictionary_Values[space] = value;
                }
                else if(@object is string)
                {
                    strings.Add((string)@object);
                }
                else if(Core.Query.TryConvert(@object, out Color color))
                {
                    result[space] = color;
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
            if(doubles != null && doubles.Count != 0)
            {
                max = doubles.Max();
                min = doubles.Min();
                double step = (max - min) / doubles.Count;
                List<Color> colors = Core.Create.Colors(System.Drawing.Color.Red, doubles.Count);

                int index = 0;
                double current = min;
                while(current <= max)
                {
                    double next = current + step;
                    foreach (double @double in doubles)
                    {
                        if(@double >= current && @double < next)
                        {
                            foreach (KeyValuePair<Space, object> keyValuePair in dictionary_Values)
                            {
                                if(!(keyValuePair.Value is double))
                                {
                                    continue;
                                }

                                double value = (double)keyValuePair.Value;
                                if(value == @double)
                                {
                                    result[keyValuePair.Key] = colors[index];
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
                            result[keyValuePair.Key] = color;
                        }
                    }
                }
            }

            return result;
        }
    }
}