// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Globalization;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Query
    {
        /// <summary>
        /// The TAS case an Iteration 3 pairing was run as, as one comparable string.
        ///
        /// <para><b>What it is for</b></para>
        /// <para>
        /// Candidate B is comparable with Reference A only because it inherited the same weather, the same
        /// solar calculation, the same annual day range and the same workflow options. That is true by
        /// construction when the pairing runs - <c>PartOSimulationContext.Copy</c> carries every field -
        /// but a <b>review</b> in a later session is looking at a record and two results files, and has
        /// to be able to say what case they were produced under. This is that statement.
        /// </para>
        /// <para>
        /// <b>Readable, not hashed.</b> It goes into a record a person audits, and "CIBSE 2021
        /// Leeds_TRY | TAS | days 1-365" tells them something a digest does not. It is compared for
        /// equality and never parsed back.
        /// </para>
        /// <para>
        /// Invariant culture throughout, so a record written on one machine reads the same on another.
        /// </para>
        /// </summary>
        internal static string PartOIteration3ScenarioFingerprint(PartOSimulationContext partOSimulationContext)
        {
            if (partOSimulationContext is null)
            {
                return null;
            }

            return string.Format(
                CultureInfo.InvariantCulture,
                "weather={0} | solar={1} | days {2}-{3} | unmetHours={4} | sizing={5} | useWidths={6} | updateConstructionLayersByPanelType={7}",
                partOSimulationContext.WeatherData?.Name ?? "<none>",
                partOSimulationContext.SolarCalculationMethod,
                partOSimulationContext.SimulateFrom,
                partOSimulationContext.SimulateTo,
                partOSimulationContext.UnmetHours,
                partOSimulationContext.Sizing,
                partOSimulationContext.UseWidths,
                partOSimulationContext.UpdateConstructionLayersByPanelType);
        }
    }
}
