// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;

namespace SAM.Core.UI
{
    public static partial class Modify
    {
        public static void Sort(this List<LegendItem> legendItems)
        {
            if (legendItems == null || legendItems.Count == 0)
            {
                return;
            }

            List<Tuple<LegendItem, double>> tuples = [];
            foreach (LegendItem legendItem in legendItems)
            {
                if (!Core.Query.TryConvert(legendItem.Text, out double value) || double.IsNaN(value))
                {
                    tuples = null;
                    break;
                }

                tuples.Add(new Tuple<LegendItem, double>(legendItem, value));
            }

            if (tuples == null || tuples.Count == 0)
            {
                legendItems.Sort((x, y) => x.Text.CompareTo(y.Text));
            }
            else
            {
                tuples.Sort((x, y) => x.Item2.CompareTo(y.Item2));
                legendItems.Clear();
                legendItems.AddRange(tuples.ConvertAll(x => x.Item1));
            }
        }
    }
}


