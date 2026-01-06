// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        public static void SortByIndex(this List<DisplayGlazingCalculationResult> displayGlazingCalculationResults)
        {
            if(displayGlazingCalculationResults == null || displayGlazingCalculationResults.Count < 2)
            {
                return;
            }

            List<DisplayGlazingCalculationResult> displayGlazingCalculationResults_Temp = displayGlazingCalculationResults.FindAll(x => x.Index == null || !x.Index.HasValue);
            displayGlazingCalculationResults.RemoveAll(x => displayGlazingCalculationResults_Temp.Contains(x));

            displayGlazingCalculationResults.Sort((x, y) => x.Index.Value.CompareTo(y.Index.Value));

            displayGlazingCalculationResults.AddRange(displayGlazingCalculationResults_Temp);
        }
    }
}
