// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>Where one step of a Part O workflow strip stands.</summary>
    public enum PartOWorkflowStepState
    {
        /// <summary>Still to come.</summary>
        Upcoming,

        /// <summary>The step the primary action performs next.</summary>
        Current,

        /// <summary>Can be done now, beside the current step - an optional follow-on.</summary>
        Available,

        /// <summary>Complete, on the evidence the authorities report.</summary>
        Done,

        /// <summary>Stops the workflow until something is fixed.</summary>
        Blocked,
    }

    /// <summary>
    /// One step of a workflow strip: a name, a state, and a sentence saying why it is in that state.
    /// <para>
    /// <b>Reusable across Part O windows.</b> Nothing here is specific to the Hub; a window builds its own
    /// list and hands it to <see cref="PartOWorkflowStepStripControl"/>. The state is always shown as a word
    /// and a glyph as well as a colour.
    /// </para>
    /// </summary>
    public class PartOWorkflowStep
    {
        public PartOWorkflowStep(int number, string name, PartOWorkflowStepState partOWorkflowStepState, string? detail)
        {
            Number = number;
            Name = name;
            State = partOWorkflowStepState;
            Detail = detail;
        }

        public int Number { get; }

        public string Name { get; }

        public PartOWorkflowStepState State { get; }

        /// <summary>Why the step is in its state - the tooltip.</summary>
        public string? Detail { get; }

        public bool IsFirst => Number == 1;

        /// <summary>What sits in the step's circle: ✓ done, ! blocked, otherwise its number.</summary>
        public string Glyph => State switch
        {
            PartOWorkflowStepState.Done => "✓",
            PartOWorkflowStepState.Blocked => "!",
            _ => Number.ToString(System.Globalization.CultureInfo.InvariantCulture),
        };

        /// <summary>The state in words, under the name. Empty for a step that is simply still to come.</summary>
        public string StateText => State switch
        {
            PartOWorkflowStepState.Done => "Done",
            PartOWorkflowStepState.Current => "Next",
            PartOWorkflowStepState.Available => "Available",
            PartOWorkflowStepState.Blocked => "Blocked",
            _ => string.Empty,
        };

        /// <summary>Name and state together, for the tooltip heading and for screen readers.</summary>
        public string AccessibleText => string.IsNullOrEmpty(StateText) ? Name : string.Format("{0} — {1}", Name, StateText);

        public override string ToString()
        {
            return AccessibleText;
        }
    }

    /// <summary>
    /// The Prepare &amp; Run workflow strip, derived from the inspection the Hub already built.
    ///
    /// <para><b>It reads statuses and adds none</b></para>
    /// <para>
    /// Every step's state is a function of the stage statuses <see cref="PartOWorkflowInspection"/> assigned
    /// and of the actions it offers - nothing is stored, timed or inferred beyond that.
    /// </para>
    /// <para>
    /// <b>Check and simulate are one step, on purpose.</b> The model check has no recorded outcome of its own:
    /// the inspection reports it as always pending, because it runs inside the run, immediately before TAS
    /// converts the prepared model, and an error stops that run. So there is no state that could say
    /// "checked, not yet simulated", and a strip that showed the two apart would be claiming one.
    /// </para>
    /// <para>
    /// <b>A mixed state is shown as it is.</b> Results saved by an earlier run can sit beside a ventilation
    /// design that the next run rebuilds; the strip then shows the simulation done and preparation still to
    /// come, and Review as the step the engineer can take now.
    /// </para>
    /// </summary>
    public static class PartOWorkflowProgress
    {
        public const string Name_Configure = "Configure";
        public const string Name_Prepare = "Prepare model";
        public const string Name_Simulate = "Check & simulate";
        public const string Name_Review = "Review";
        public const string Name_Optimise = "Optimise (2B)";

        /// <param name="partOWorkflowInspection">The inspection the window is showing.</param>
        /// <param name="includeOptimisation">Whether the scenario can carry an Iteration 2B at all - the step is shown only then.</param>
        public static List<PartOWorkflowStep> Steps(PartOWorkflowInspection? partOWorkflowInspection, bool includeOptimisation)
        {
            List<PartOWorkflowStageState> stages = partOWorkflowInspection is null ? [] : [.. partOWorkflowInspection.Stages];

            PartOWorkflowStageState? Stage(PartOWorkflowStage partOWorkflowStage) => stages.FirstOrDefault(x => x.Stage == partOWorkflowStage);

            List<PartOWorkflowStageState> stages_Configuration = stages.FindAll(x => x.Stage is PartOWorkflowStage.DwellingScope or PartOWorkflowStage.InternalConditions or PartOWorkflowStage.PartFRequirements or PartOWorkflowStage.Equipment);

            PartOWorkflowStageState? stage_Blocked = stages_Configuration.Find(x => x.IsBlocking);
            PartOWorkflowStageState? stage_Design = Stage(PartOWorkflowStage.VentilationDesign);
            PartOWorkflowStageState? stage_Simulation = Stage(PartOWorkflowStage.Simulation);
            PartOWorkflowStageState? stage_ModelCheck = Stage(PartOWorkflowStage.ModelCheck);
            PartOWorkflowStageState? stage_Results = Stage(PartOWorkflowStage.Results);

            bool configured = partOWorkflowInspection is not null && stage_Blocked is null && stages_Configuration.Count != 0;
            bool prepared = stage_Design?.Status == PartOWorkflowStageStatus.Reused;
            bool simulated = stage_Simulation?.Status == PartOWorkflowStageStatus.Ready;
            bool reviewable = partOWorkflowInspection?.CanReviewResults ?? false;

            PartOWorkflowStepState state_Configure = configured ? PartOWorkflowStepState.Done : PartOWorkflowStepState.Blocked;
            PartOWorkflowStepState state_Prepare = prepared ? PartOWorkflowStepState.Done : PartOWorkflowStepState.Upcoming;
            PartOWorkflowStepState state_Simulate = simulated ? PartOWorkflowStepState.Done : PartOWorkflowStepState.Upcoming;
            PartOWorkflowStepState state_Review = PartOWorkflowStepState.Upcoming;

            //The one Current step: Review wherever there are results to review - even while the configuration
            //blocks, because Review reads the existing run and does not depend on the current inputs.
            //Otherwise the first step the primary action still has to perform, and none at
            //all while the configuration blocks.
            if (reviewable)
            {
                state_Review = PartOWorkflowStepState.Current;
            }
            else if (configured)
            {
                if (!prepared)
                {
                    state_Prepare = PartOWorkflowStepState.Current;
                }
                else if (!simulated)
                {
                    state_Simulate = PartOWorkflowStepState.Current;
                }
            }

            List<PartOWorkflowStep> result =
            [
                new(1, Name_Configure, state_Configure, stage_Blocked is null ? "Scenario, scope, TM59 mapping and Part F requirements are in place." : string.Format("{0}: {1}", stage_Blocked.Name, stage_Blocked.Detail)),
                new(2, Name_Prepare, state_Prepare, !prepared && simulated
                    ? "The existing results were simulated by an earlier run. The loaded model is not prepared for this scenario and scope, so the next Prepare & Run rebuilds the ventilation design before it simulates again."
                    : stage_Design?.Detail),
                new(3, Name_Simulate, state_Simulate, string.Join(" ", new[] { stage_ModelCheck?.Detail, stage_Simulation?.Detail }.Where(x => !string.IsNullOrWhiteSpace(x)))),
                new(4, Name_Review, state_Review, stage_Results?.Detail),
            ];

            if (includeOptimisation)
            {
                bool canOptimise = partOWorkflowInspection?.CanOptimise ?? false;

                result.Add(new(5, Name_Optimise, canOptimise ? PartOWorkflowStepState.Available : PartOWorkflowStepState.Upcoming, partOWorkflowInspection?.OptimisationRefusal ?? (canOptimise ? "TM59 optimisation of this Iteration 2 design." : null)));
            }

            return result;
        }
    }
}
