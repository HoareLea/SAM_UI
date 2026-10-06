// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// What one Iteration 2B round has decided to target, and which failing rooms it deliberately cannot.
    /// <para>
    /// <b>A policy answer, not an engineering one.</b> Nothing here computes an airflow beyond adding the
    /// configured step to the design a room already carries; every consequence of doing so - the balancing,
    /// the Approved Document F floors, the capacity - belongs to
    /// <c>SAM.Analytical.Modify.EvaluateTargetedDesignAirFlows</c> and is not restated in the UI.
    /// </para>
    /// </summary>
    public class PartOOptimisationTargetSelection
    {
        /// <summary>The deliberate targets for this round, one per eligible failing room.</summary>
        public List<DesignAirFlowTarget> Targets { get; } = [];

        /// <summary>
        /// Failing rooms this optimisation cannot act on, each with the reason - out of the Part O dwelling
        /// scope, or with no Approved Document O design terminal to raise.
        /// <para>
        /// <b>Stated rather than silently skipped.</b> An engineer reading a run that stopped with rooms
        /// still failing has to be able to tell "nothing could be done automatically about this room" from
        /// "this room was never looked at".
        /// </para>
        /// </summary>
        public List<string> NotOptimisable { get; } = [];

        /// <summary>Whether there is anything to run a round with.</summary>
        public bool HasTargets => Targets.Count != 0;
    }
}
