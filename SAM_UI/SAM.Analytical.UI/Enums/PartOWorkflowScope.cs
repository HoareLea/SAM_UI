// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.ComponentModel;

namespace SAM.Analytical.UI
{
    /// <summary>
    /// Which dwellings an Approved Document O run covers, and whether they are simulated inside the whole
    /// building or as a thermal model of their own.
    /// <para>
    /// <b>Two independent statements, deliberately offered as one choice.</b> The dwelling selection says
    /// which dwellings are ASSESSED; isolation says which building is SIMULATED around them. They are
    /// separate in the analytical API - <c>Modify.PreparePartOIteration</c> takes the zones and the
    /// <c>isolate</c> flag apart - and they stay separate below this enum. They are one control here because
    /// isolating without narrowing the selection is the only combination a person has to think about, and the
    /// dialog says what it costs where the choice is made.
    /// </para>
    /// <para>
    /// <b>Nothing here decides what a dwelling is.</b> That is <c>Query.PartFDwellingZones</c>, asked once
    /// and never restated - see <see cref="PartOWorkflowRequest"/>.
    /// </para>
    /// </summary>
    public enum PartOWorkflowScope
    {
        [Description("All dwellings")] AllDwellings,

        [Description("Selected dwellings")] SelectedDwellings,

        [Description("Selected dwellings in isolation")] SelectedDwellingsIsolated,
    }
}
