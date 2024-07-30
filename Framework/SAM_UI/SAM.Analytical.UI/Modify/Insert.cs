using NetOffice.ExcelApi;
using System.Collections.Generic;

namespace SAM.Analytical.UI
{
    public static partial class Modify
    {
        public static void Insert(this Range range, Dictionary<string, object> dictionary)
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