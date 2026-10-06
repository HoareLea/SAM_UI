// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Tas;
using System.Threading;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Who actually runs the TAS workflow for one Approved Document O simulation.
    ///
    /// <para><b>Why this seam exists, and why it is exactly this narrow</b></para>
    /// <para>
    /// Approved Document O Iteration 3 needs a second thermal case that is the <b>same</b> case as the
    /// reference in every respect except one: it must carry no mechanical ventilation of its own, because
    /// the ventilation is about to be modelled explicitly in TAS Systems instead. SAM_Tas owns that -
    /// <c>Create.NoIzamThermalSource</c> is the authority, and it forces both cleanups on and evidences
    /// the run.
    /// </para>
    /// <para>
    /// The alternative was to write the whole thermal preparation pipeline again in the Iteration 3
    /// orchestration: the deep working copy, the material repair, the construction layers, the gbXML, the
    /// solar calculation, the design days, the surface output spec, the day range. That is precisely the
    /// duplication <see cref="Modify.RunPartOSimulation"/> exists to prevent, and a second copy of it
    /// would be a second Part O thermal case that could drift from the first - at which point the A/B
    /// comparison would be measuring the drift.
    /// </para>
    /// <para>
    /// So the pipeline stays in one place and only its <b>last step</b> is substitutable. The signature is
    /// deliberately identical to <see cref="Modify.RunWorkflow(AnalyticalModel, WorkflowSettings, CancellationToken, out bool, bool)"/>'s
    /// owned-model form, so the default is that method and every existing caller executes exactly what it
    /// always did.
    /// </para>
    /// </summary>
    /// <param name="analyticalModel">
    /// The normalized model, already this run's own deep working copy - so an implementation may mutate it
    /// and must not copy it again.
    /// </param>
    /// <param name="workflowSettings">The settings <see cref="Modify.RunPartOSimulation"/> composed.</param>
    /// <param name="cancellationToken">The run's token, shared with its COM pre-steps.</param>
    /// <param name="cancelled">Whether the run was cancelled. A cancelled run returns null.</param>
    /// <returns>The model the workflow returned, or null where it did not run, failed or was cancelled.</returns>
    public delegate AnalyticalModel? PartOWorkflowRunner(AnalyticalModel analyticalModel, WorkflowSettings workflowSettings, CancellationToken cancellationToken, out bool cancelled);
}
