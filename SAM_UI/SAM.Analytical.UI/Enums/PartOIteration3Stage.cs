// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.ComponentModel;

namespace SAM.Analytical.UI
{
    /// <summary>
    /// One stage of the Approved Document O Iteration 3 A/B run, in the order it is attempted.
    /// <para>
    /// <b>The declaration order IS the pipeline order</b>, and <see cref="PartOIteration3Ledger"/> depends on
    /// it: a refusal at any stage leaves every LATER stage <see cref="PartOIteration3StageStatus.NotRun"/>,
    /// which is decided by comparing enum values rather than by remembering to skip each one at its call
    /// site. Inserting a member in the middle therefore changes the pipeline, and is a deliberate act.
    /// </para>
    /// <para>
    /// <b>No stage is an engineering authority.</b> Each names work an existing authority does - SAM's
    /// preparation and TM59, SAM_Systems' materialisation, SAM_Tas' no-IZAM source, TPD conversion,
    /// Systems simulation and resultant-temperature provider. The three stages that are genuinely this
    /// orchestration's own - <see cref="SystemScope"/>, <see cref="Reconciliation"/> and
    /// <see cref="Comparison"/> - decide scope, check identity and compute descriptive statistics. None of
    /// them computes a thermal result or a compliance verdict.
    /// </para>
    /// </summary>
    public enum PartOIteration3Stage
    {
        /// <summary>The eligible completed Iteration 1a run this pairing is built on.</summary>
        [Description("Input")] Input,

        /// <summary>Reference A - the existing TBD/IZAM route's results, already produced by that run.</summary>
        [Description("Reference A")] ReferenceA,

        /// <summary>Reference A's assessment, through the unchanged TM59 authority.</summary>
        [Description("Reference A TM59")] ReferenceATM59,

        /// <summary>Which authored ventilation systems are the design under assessment - see SAM #114.</summary>
        [Description("System scope")] SystemScope,

        /// <summary>
        /// PR5A (SAM#111 plan §J): in Selected-product mode, resolves every scoped air handling unit's
        /// selected product to its certified heat-recovery efficiency and specific fan power. A no-op in
        /// Parity mode - it always completes with nothing resolved, which is what materialises B0.
        /// </summary>
        [Description("Equipment resolution")] EquipmentResolution,

        /// <summary>SAM_Systems materialises the explicit MVHR systems from the analytical design.</summary>
        [Description("Materialisation")] Materialisation,

        /// <summary>SAM_Tas builds Candidate B's dedicated no-IZAM thermal source.</summary>
        [Description("Thermal source")] ThermalSource,

        /// <summary>SAM_Tas converts the explicit systems to a TAS Systems document.</summary>
        [Description("Systems conversion")] SystemsConversion,

        /// <summary>TAS simulates the air systems.</summary>
        [Description("Systems simulation")] SystemsSimulation,

        /// <summary>Every room's achieved ZoneTemperature comes back complete and finite.</summary>
        [Description("Zone temperature")] ZoneTemperature,

        /// <summary>The IResultantTemperatureProvider answers every room's ResultantTemperature.</summary>
        [Description("Resultant temperature")] ResultantTemperature,

        /// <summary>Candidate B's assessment, through the SAME unchanged TM59 authority.</summary>
        [Description("Candidate B TM59")] CandidateBTM59,

        /// <summary>Guid-only engineering reconciliation of the two cases against each other.</summary>
        [Description("Reconciliation")] Reconciliation,

        /// <summary>Descriptive A/B temperature statistics. Exists only where the whole chain completed.</summary>
        [Description("Comparison")] Comparison,

        /// <summary>The pairing record and Candidate B's reopenable model.</summary>
        [Description("Persistence")] Persistence,
    }
}
