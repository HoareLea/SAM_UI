// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.UI.WPF
{
    public static partial class Query
    {
        /// <summary>
        /// The equipment preselection a preparation should actually run under: what the request states, or
        /// failing that what the <b>project</b> states, or failing that the historic default.
        ///
        /// <para><b>Why the order matters, and why it is a named function</b></para>
        /// <para>
        /// Not every caller states a preference. <c>Modify.RunPartOWorkflow</c>'s Prepare &amp; Run builds
        /// its request from a scenario and a dwelling scope and says nothing about equipment at all. If an
        /// unstated preference fell straight through to "automatic over the whole catalogue", that path
        /// would run the smallest-capable rule over a project whose engineer had taken <b>manual</b>
        /// authority and authored assignments by hand - replacing every one of them with the rule's answer,
        /// and reporting a successful preparation. The engineer would have no way to tell from the result
        /// that their work had been discarded, because a reselected model looks exactly like a correctly
        /// selected one.
        /// </para>
        /// <para>
        /// So the project is the fallback, not the default. This is a function with a name and a test rather
        /// than a <c>??</c> chain inside an orchestrator, because the failure it prevents is silent.
        /// </para>
        ///
        /// <para><b>Absent still means the historic default</b></para>
        /// <para>
        /// A project that has never stated a preference has none, and gets exactly what Iteration 2 did
        /// before a mode existed: automatic selection over every selectable product. Nothing needs
        /// migrating.
        /// </para>
        /// </summary>
        /// <param name="partOWorkflowRequest">What was asked for, if it says anything about equipment.</param>
        /// <param name="analyticalModel">The project, which carries its own preselection.</param>
        /// <returns>The configuration to prepare under. Never null.</returns>
        internal static PartOEquipmentSelection PartOEquipmentSelection(PartOWorkflowRequest? partOWorkflowRequest, AnalyticalModel? analyticalModel)
        {
            return partOWorkflowRequest?.EquipmentSelection
                ?? analyticalModel?.GetValue<PartOEquipmentSelection>(Analytical.AnalyticalModelParameter.PartOEquipmentSelection)
                ?? new PartOEquipmentSelection();
        }
    }
}
