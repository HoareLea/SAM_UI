// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.UI.WPF
{
    public static partial class Query
    {
        /// <summary>
        /// The thermal model a Part O run simulates, said as a scope - "Whole building", or "Isolated ·
        /// selected dwellings: Flat 1, Flat 2" with what that means for the results in
        /// <paramref name="detail"/>.
        /// <para>
        /// <b>One spelling for the two screens that state it</b> - the Review iteration window before the
        /// run and the TM59 result window after it - so the scope an engineer accepted is the scope the
        /// result is headed with. Read off the run's own <see cref="PartOIsolationContext"/>; presentation
        /// only.
        /// </para>
        /// </summary>
        /// <param name="detail">
        /// What an isolated scope means for the results, or null for the whole building. Shown, not
        /// tooltipped: an isolated run is a different thermal model.
        /// </param>
        internal static string PartOThermalModelScopeText(PartOIsolationContext? partOIsolationContext, out string? detail)
        {
            if (partOIsolationContext is null || !partOIsolationContext.IsValid)
            {
                detail = null;

                return "Whole building";
            }

            detail = "Interfaces to excluded spaces are simulated as adiabatic and surrounding external geometry is retained as shading context, so these results may differ from a whole-building simulation of the same dwellings. The Part O criteria and the Part F requirements are unchanged.";

            return string.Format("Isolated · selected dwellings: {0}", string.Join(", ", partOIsolationContext.Names_Dwelling));
        }
    }
}
