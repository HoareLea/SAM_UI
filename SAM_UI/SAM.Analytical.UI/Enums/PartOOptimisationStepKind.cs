// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.ComponentModel;

namespace SAM.Analytical.UI
{
    /// <summary>
    /// What one recorded step of an Iteration 2B run <b>is</b> - the three things a reader of the history
    /// has to be able to tell apart, and must never have to tell apart by reading a project name.
    /// <para>
    /// <b>Why this is an enum and not <c>Iteration == 0</c> plus a convention.</b> The baseline and the
    /// optimisation rounds were distinguishable by their iteration number alone, and that stopped being
    /// enough the moment a capacity envelope existed: an envelope <i>is</i> prepared, simulated over the
    /// full year and assessed, exactly as a round is, and it completes. Read as a round it would appear in
    /// the round count, be eligible to be the run's last valid design, and be reported as another
    /// successful +5 l/s step - which is the one thing it certainly is not.
    /// </para>
    /// </summary>
    public enum PartOOptimisationStepKind
    {
        /// <summary>
        /// The Iteration 2 design as it stood, with its own results and its own assessment. Iteration 0,
        /// no targets, no round - what every later step is read against.
        /// </summary>
        [Description("Baseline")] Baseline,

        /// <summary>
        /// One ordinary optimisation round: every eligible failing room raised by the <b>whole</b>
        /// configured step, or nothing. A step of this kind that completed is a design the run may carry
        /// forward and may hand back as its answer.
        /// </summary>
        [Description("Optimisation Round")] OptimisationRound,

        /// <summary>
        /// The final <b>diagnostic</b> selected-equipment capacity envelope: the last valid design grown
        /// proportionally - every terminal keeping its share of it - until the already-selected unit's own
        /// ceiling binds.
        /// <para>
        /// <b>Never an optimisation round and never the run's answer.</b> It is a design the ordinary
        /// all-or-nothing policy deliberately refuses, evaluated to say what the equipment already bought
        /// could support. It does not count as a round, is not eligible to be the last valid design, and
        /// never becomes the baseline of anything.
        /// </para>
        /// </summary>
        [Description("Capacity Envelope")] CapacityEnvelope,
    }
}
