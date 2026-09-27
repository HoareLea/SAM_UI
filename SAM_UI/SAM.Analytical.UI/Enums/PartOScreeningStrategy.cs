// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.ComponentModel;

namespace SAM.Analytical.UI
{
    /// <summary>
    /// An engineering strategy a mixed Part O design can screen every dwelling against - named for what it is,
    /// never for the iteration number that happens to build it today.
    /// <para>
    /// <b>Ordered from least to most intervention</b> (PR0 §D6): natural ventilation, then MVHR at the Approved
    /// Document F requirement, then MVHR with a product selected from the project pool, then a retained optimised
    /// design airflow, then active cooling. The suggestion reads that order; it never re-orders it.
    /// </para>
    /// <para>
    /// <b>Members are appended, never reordered.</b> Screening evidence is persisted by member name, and a later
    /// strategy (cooling, PR3) arrives as a new member without changing how the others read.
    /// </para>
    /// </summary>
    public enum PartOScreeningStrategy
    {
        [Description("Undefined")] Undefined,

        /// <summary>Every screened dwelling naturally ventilated - the Iteration 1b case.</summary>
        [Description("Natural ventilation")] Natural,

        /// <summary>Every screened dwelling with MVHR at its Part F requirement and a generic unit - the Iteration 1a case.</summary>
        [Description("MVHR baseline")] MechanicalBaseline,

        /// <summary>Every screened dwelling with MVHR and a product selected from the project pool - the Iteration 2 case.</summary>
        [Description("Selected-product MVHR")] SelectedProduct,

        /// <summary>MVHR with an optimised, retained design airflow - the Iteration 2B case. Not yet screenable in the mixed workflow.</summary>
        [Description("Optimised MVHR")] Optimised,

        /// <summary>MVHR with active supply-air cooling - the Iteration 3 case. Gated until the cooling workflow (PR3).</summary>
        [Description("Active cooling")] ActiveCooling,
    }
}
