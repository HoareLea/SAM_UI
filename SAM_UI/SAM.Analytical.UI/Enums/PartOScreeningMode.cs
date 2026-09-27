// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.ComponentModel;

namespace SAM.Analytical.UI
{
    /// <summary>
    /// How much of the screening list a screening simulates.
    /// <para>
    /// A TAS run simulates the whole building whichever dwellings it assesses, so the only work that can be saved
    /// is a whole strategy's run. <see cref="Minimum"/> saves exactly that and nothing else: a strategy is skipped
    /// only once every screened dwelling already has a passing, permitted strategy, and a skipped strategy's cells
    /// are <see cref="PartODwellingOutcome.NotRun"/> - never inferred.
    /// </para>
    /// </summary>
    public enum PartOScreeningMode
    {
        [Description("Undefined")] Undefined,

        /// <summary>
        /// Find each dwelling's first permitted passing strategy. After each strategy, only the dwellings with no
        /// passing permitted strategy yet are assessed by the next one; the run stops when none are left.
        /// </summary>
        [Description("Minimum screening")] Minimum,

        /// <summary>Simulate every selected strategy for every dwelling and populate the whole comparison.</summary>
        [Description("Full comparison")] FullComparison,
    }
}
