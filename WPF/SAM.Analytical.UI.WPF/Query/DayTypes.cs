// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Query
    {
        public static List<TBD.dayType> DayTypes(this TBD.Calendar calendar)
        {
            if (calendar == null)
                return null;

            List<TBD.dayType> result = new List<TBD.dayType>();

            for (int i = 1; i <= calendar.GetDayTypeCount(); i++)
            {
                TBD.dayType dayType = calendar.dayTypes(i);
                if (dayType != null)
                    result.Add(dayType);
            }

            return result;
        }
    }
}
