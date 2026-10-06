// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.UI
{
    public static partial class Query
    {
        /// <summary>
        /// A count with its noun in the right number - "1 space", "8 spaces" - for engineer-facing Part O text,
        /// in place of the programmatic "space(s)".
        /// </summary>
        public static string PartOCount(int count, string singular, string plural)
        {
            return string.Format("{0} {1}", count, count == 1 ? singular : plural);
        }

        /// <summary>The noun alone, in the number <paramref name="count"/> calls for.</summary>
        public static string PartONoun(int count, string singular, string plural)
        {
            return count == 1 ? singular : plural;
        }
    }
}
