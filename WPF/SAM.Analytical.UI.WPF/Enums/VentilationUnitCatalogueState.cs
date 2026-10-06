// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// What came back from reading the manufacturer ventilation-unit catalogue. Three states, kept apart
    /// because they need three different things done about them and only one of them is a fault.
    /// </summary>
    public enum VentilationUnitCatalogueState
    {
        /// <summary>
        /// The catalogue could not be read - not installed, not found, unreadable, or declaring a schema this
        /// reader does not accept. Nothing is known about what products exist.
        /// <para>
        /// Must never be presented as "no product can serve this dwelling". That sentence is an engineering
        /// answer; this state is the absence of the data needed to give one.
        /// </para>
        /// </summary>
        Unavailable,

        /// <summary>
        /// The catalogue was read and holds products, but none of them is selectable - every entry's maximum
        /// airflow is unresolved. A real, documented condition: a published product with performance data and
        /// no stated maximum is in the catalogue on purpose, and guessing a maximum from the largest
        /// published duty point is the mistake the capacity seam exists to prevent.
        /// </summary>
        NoneSelectable,

        /// <summary>At least one product can be selected against a dwelling's design duty.</summary>
        Selectable
    }
}
