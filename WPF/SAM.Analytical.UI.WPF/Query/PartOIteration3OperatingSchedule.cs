// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Systems;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Query
    {
        /// <summary>
        /// The exact name the Approved Document O Iteration 3 operating schedule is materialised under.
        ///
        /// <para><b>Preserved letter for letter, and not normalised</b></para>
        /// <para>
        /// This string was accepted by PR1's licensed materialisation and by PR2's licensed TAS Systems
        /// conversion. Components reference schedules <b>by name</b>, and SAM #113 has already shown that
        /// TAS behaviour can depend on order and on naming - so re-casing it, trimming it, or making it
        /// "tidier" is a change to a value a licensed acceptance was taken against, not a cosmetic edit.
        /// It is stated here once so no call site can spell it a second way.
        /// </para>
        /// </summary>
        public const string Name_PartOIteration3OperatingSchedule = "Part O continuous operation";

        /// <summary>
        /// The parity operating schedule Candidate B's fans are materialised with: <b>8760 values of
        /// exactly 1.0</b>.
        ///
        /// <para><b>Why constant 1.0, and why a full year of it</b></para>
        /// <para>
        /// SAM #111 fixes the initial parity configuration at a continuous operation factor of 1.0 -
        /// Candidate B is a comparison of routes, not of control strategies, so anything that modulated
        /// the fans would put a second difference into an A/B pair that is supposed to have one. A full
        /// annual array rather than a repeated day because the Part O case is the full annual series, and
        /// because a 24-value schedule presented as an annual one is exactly the defect PR1 had to fix in
        /// <c>YearlySchedule</c>'s copy constructor: the other 8736 hours were silently zero, so an
        /// "always on" system ran for one day a year and every stage downstream reported success.
        /// </para>
        /// <para>
        /// <b>A fresh instance every call.</b> <c>MechanicalVentilationSettings</c> clones what it is given
        /// on the way in and on the way out, but handing out a shared mutable array from a query would be
        /// one edit away from a schedule that is 1.0 in one run and something else in the next.
        /// </para>
        /// </summary>
        public static YearlySchedule PartOIteration3OperatingSchedule()
        {
            double[] values = new double[8760];

            for (int i = 0; i < values.Length; i++)
            {
                values[i] = 1.0;
            }

            return new YearlySchedule(Name_PartOIteration3OperatingSchedule)
            {
                Values = values,
            };
        }
    }
}
