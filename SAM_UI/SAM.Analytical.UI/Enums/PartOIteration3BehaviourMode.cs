// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.ComponentModel;

namespace SAM.Analytical.UI
{
    /// <summary>
    /// Which ventilation equipment behaviour an Iteration 3 run materialises Candidate B with - PR5A
    /// (SAM#111 plan §J).
    /// <para>
    /// <b><see cref="Parity"/> is the foundation control, and stays the default.</b> It is exactly Candidate
    /// B0: no unit settings are resolved, the topology template is the shipped <c>MV.json</c>, and the
    /// route's fan heat gain policy stays <c>ClearToZero</c>. Every existing caller of
    /// <c>Modify.RunPartOIteration3</c> that does not name a mode gets this, byte-for-byte unchanged from
    /// before PR5A existed.
    /// </para>
    /// <para>
    /// <b><see cref="SelectedProduct"/> resolves every scoped air handling unit's already-selected
    /// product</b> (Iteration 2's own selection authority; nothing here selects or reselects) to its
    /// certified heat-recovery efficiency and specific fan power, and materialises the same design with
    /// those values applied. It is a <i>paired</i> comparison, not a replacement: Reference A is unchanged
    /// and B0 remains separately available. A unit with no selection, an unresolvable reference, or
    /// missing certified data (E1/E2) refuses the whole run rather than silently falling back to B0 for
    /// that one unit.
    /// </para>
    /// <para>
    /// <b><see cref="SelectedProductCooling"/> (PR5B, B4) is B0 plus the selected product's cooling
    /// module, and nothing else.</b> The ventilation stays exactly the foundation control - <c>MV.json</c>,
    /// no unit settings, <c>ClearToZero</c> - and each scoped unit's already-selected product's published
    /// cooling table and flow-fraction law are materialised as an internal recirculation branch inside that
    /// unit's own air system, on its own rooms. B4 - B0 is therefore the cooling layer alone. A unit with no
    /// selection, an unresolvable reference, or no valid cooling data refuses the whole run.
    /// </para>
    /// </summary>
    public enum PartOIteration3BehaviourMode
    {
        /// <summary>The foundation control - Candidate B0, unchanged. The default.</summary>
        [Description("Parity (foundation control)")] Parity,

        /// <summary>Every scoped air handling unit's selected product, resolved and applied.</summary>
        [Description("Selected product")] SelectedProduct,

        /// <summary>PR5B: the foundation control plus every scoped unit's selected product's cooling module (B4).</summary>
        [Description("Selected product cooling module (B0 + cooling)")] SelectedProductCooling,

        /// <summary>
        /// SAM#123: every scoped unit's selected product operated to its manufacturer's modelling guidance -
        /// the product's own arrangement (exchanger + supply DX coil, MVRE topology), its cooling-stat, its
        /// elevated supply and extract airflow while cooling and its stated supply-temperature rule, all read
        /// from the catalogue and grounded natively in TAS. <b>Manufacturer guidance, provisional, not
        /// certified performance.</b> B0 and B4 are unchanged; this is paired against Reference A like them.
        /// </summary>
        [Description("Selected product \u2013 manufacturer guidance")] SelectedProductManufacturerGuidance,
    }
}
