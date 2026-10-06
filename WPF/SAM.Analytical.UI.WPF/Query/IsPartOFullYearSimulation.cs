// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Tas;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Query
    {
        /// <summary>
        /// Whether the workflow these settings describe is the <b>full-year</b> simulation an Approved
        /// Document O TM59 assessment can be read from: days 1 to 365 of an actual simulation.
        /// <para>
        /// <b>Read off the settings, not off the tick box.</b> <c>Modify.Simulate</c>'s
        /// "Full Year Simulation" check box does not decide the day range on its own - the range still comes
        /// from the two text boxes beside it, and a run with the box unticked is turned into a
        /// <i>one-day</i> simulation when shading was updated. These three fields are what
        /// <c>WorkflowCalculator</c> will actually do, so they are the only honest thing to ask.
        /// </para>
        /// <para>
        /// <b>Why anything less is refused rather than assessed.</b> The TM59 criteria are counts of hours
        /// over annual, summer and night-time windows against limits derived from annual occupied hours. A
        /// series covering one day, a date range, or nothing at all still produces numbers, and those numbers
        /// look like an assessment - so a partial run must not be able to complete a Part O run at all. A
        /// workflow returning an analytical model says nothing about this: sizing alone returns one.
        /// </para>
        /// <para>
        /// <b>Exactly 1 to 365.</b> Not "365 or more days" and not "From &lt;= To": a run of days 2-366 or
        /// 1-364 is a different year from the one the criteria are defined over, and refusing it is the safe
        /// way to be wrong. The dialog ships defaulted to 1 and 365, so the ordinary ticked run passes.
        /// </para>
        /// </summary>
        /// <param name="workflowSettings">The settings about to be handed to <c>WorkflowCalculator</c>.</param>
        /// <returns>Whether a Part O run may be completed by the workflow these settings describe.</returns>
        public static bool IsPartOFullYearSimulation(this WorkflowSettings workflowSettings)
        {
            return workflowSettings is not null
                && workflowSettings.Simulate
                && workflowSettings.SimulateFrom == 1
                && workflowSettings.SimulateTo == 365;
        }
    }
}
