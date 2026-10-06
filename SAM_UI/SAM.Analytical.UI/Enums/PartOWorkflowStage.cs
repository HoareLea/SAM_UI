// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.ComponentModel;

namespace SAM.Analytical.UI
{
    /// <summary>
    /// One stage of the Approved Document O workflow, as the Prepare &amp; Run dialog reports it.
    /// <para>
    /// <b>A reporting vocabulary, not a pipeline.</b> The order the stages actually run in belongs to
    /// <c>Modify.RunPartOWorkflow</c> and to the authorities it calls; this names them so a status line can
    /// be attributed to one. Each stage is answered by exactly one existing authority - see
    /// <see cref="PartOWorkflowInspection"/>, which names it per stage.
    /// </para>
    /// </summary>
    public enum PartOWorkflowStage
    {
        [Description("Dwelling scope")] DwellingScope,

        [Description("TM59 mapping")] InternalConditions,

        [Description("Part F requirements")] PartFRequirements,

        [Description("Ventilation design")] VentilationDesign,

        [Description("Equipment")] Equipment,

        [Description("Model check")] ModelCheck,

        [Description("Simulation")] Simulation,

        [Description("Results")] Results,
    }
}
