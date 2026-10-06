// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Query
    {
        /// <summary>
        /// The ventilation route a prepared Part O iteration was made over, said once.
        ///
        /// <para><b>The duplication this deletes</b></para>
        /// <para>
        /// The review window's header stated the route twice - the mode the preparation settled on, then
        /// the canonical word that was handed to it, in brackets - which on the mechanical route printed
        /// the literal reading <c>MVHR (MVHR)</c>. Native acceptance flagged it, and rightly: it reads as
        /// a defect rather than as two facts.
        /// </para>
        ///
        /// <para><b>They are one fact, and this checks that rather than assuming it</b></para>
        /// <para>
        /// The canonical word is read back through <c>Analytical.Query.PartOVentilationMode</c> - the same
        /// authority the preparation itself used - and where it maps to the mode the preparation settled
        /// on, the two are the same statement and the word is printed once. That agreement is guaranteed by
        /// <c>Modify.PreparePartOIteration</c>, which refuses a pairing whose iteration is not the base
        /// configuration for the stated route, so the single word is the normal and expected answer.
        /// </para>
        /// <para>
        /// Where they somehow disagree, <b>both are stated</b> rather than one being quietly preferred. A
        /// header that had hidden that disagreement would be the worse failure by far, and this is the only
        /// case in which the bracket survives.
        /// </para>
        ///
        /// <para><b>Presentation only</b></para>
        /// <para>
        /// Nothing here decides, resolves or writes a route. The canonical value the assessment reads is
        /// untouched, no enum or persisted value is involved, and this is never asked what the route
        /// <i>is</i> - only how to spell what it already was.
        /// </para>
        /// </summary>
        /// <param name="partOVentilationMode">The route the preparation settled on.</param>
        /// <param name="ventilationStrategy">The canonical word that was handed to the preparation.</param>
        internal static string PartOVentilationRouteText(PartOVentilationMode partOVentilationMode, string? ventilationStrategy)
        {
            string strategy = ventilationStrategy?.Trim() ?? string.Empty;

            //Nothing was handed in, so there is only the mode to state.
            if (strategy.Length == 0)
            {
                return Core.Query.Description(partOVentilationMode);
            }

            //Qualified: SAM.Analytical.UI.WPF declares a Query of its own.
            PartOVentilationMode partOVentilationMode_Strategy = Analytical.Query.PartOVentilationMode(strategy, out string _);

            return partOVentilationMode_Strategy == partOVentilationMode
                ? strategy
                : string.Format("{0} (the preparation settled on {1})", strategy, Core.Query.Description(partOVentilationMode));
        }
    }
}
