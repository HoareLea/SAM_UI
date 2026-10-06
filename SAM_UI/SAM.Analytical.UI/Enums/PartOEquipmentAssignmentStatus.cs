// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.ComponentModel;

namespace SAM.Analytical.UI
{
    /// <summary>
    /// Whether one dwelling's assigned ventilation unit can move that dwelling's design duty.
    /// <para>
    /// <b>Four different things for an engineer to do next</b>, which is why this is stated rather than
    /// left to be read out of a message. "No product yet" is not a failure; "cannot meet the duty" is a
    /// design that does not work; "capacity unknown" is a catalogue problem and never a pass.
    /// </para>
    /// <para>
    /// <b>Not a statement about the pool.</b> Whether the assigned product is still inside the project's
    /// permitted set is <c>PartOEquipmentAssignment.IsOutsideAllowedPool</c>, deliberately separate: a
    /// procurement change is not an engineering failure, and showing it as one would push an engineer to
    /// "fix" a design that is perfectly sound.
    /// </para>
    /// </summary>
    public enum PartOEquipmentAssignmentStatus
    {
        /// <summary>
        /// No product is assigned to this dwelling yet. The normal state of a manual assignment nobody has
        /// made, and of every dwelling at Iteration 1a. <b>Exposed, never invented</b> - nothing fills this
        /// in with a plausible product on the engineer's behalf.
        /// </summary>
        [Description("Not assigned")] NotAssigned,

        /// <summary>
        /// The assigned product can move this dwelling's design duty on both sides. Says nothing about
        /// whether it is the smallest that could - a deliberately oversized capable unit is OK.
        /// </summary>
        [Description("OK")] Ok,

        /// <summary>
        /// The assigned product cannot move this dwelling's design duty. The assignment <b>stays</b>: the
        /// design airflow is not reduced to fit the box and no larger product is substituted. Where a
        /// capable product exists it is offered as a suggestion the engineer may take.
        /// </summary>
        [Description("Insufficient")] Insufficient,

        /// <summary>
        /// A product is assigned but the current catalogue does not say what it can move - the identity is
        /// not in it, or the catalogue gives that identity two different capacities. <b>Unknown is not a
        /// pass.</b> Nothing is assumed about a box whose rating cannot be resolved.
        /// </summary>
        [Description("Capacity unknown")] CapacityUnknown,
    }
}
