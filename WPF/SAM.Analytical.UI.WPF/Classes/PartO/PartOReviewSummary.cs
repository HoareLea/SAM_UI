// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// What the Review iteration window says about the prepared run, in the parts it shows them in: the
    /// scenario as a heading, then scope, route, design duty, equipment and overheating scenarios.
    /// <para>
    /// <b>Presentation only.</b> Every part is a sentence <c>Modify.PreparePartOIteration</c> already wrote
    /// from the preparation - nothing is counted, compared or decided here. The parts exist so the window can
    /// lay them out as a short summary rather than one paragraph; <see cref="Text"/> is the same content as one
    /// block, for Copy All.
    /// </para>
    /// <para>
    /// <b>The scenario is the WORKFLOW scenario</b>, from <see cref="PartOWorkflowScenario.Find"/>, and never
    /// the engine option's text: Iteration 2 prepares as Iteration 1a with a catalogue, and a review that
    /// printed the option headed an Iteration 2 run "Iteration 1a".
    /// </para>
    /// </summary>
    public class PartOReviewSummary
    {
        public PartOReviewSummary(string scenario, string scope, string? scopeDetail, string route, string duty, string? dutyDetail, string equipment, string? equipmentDetail, string overheatingScenarios, bool hasMechanicalDesignDuty = true)
        {
            HasMechanicalDesignDuty = hasMechanicalDesignDuty;
            Scenario = scenario ?? string.Empty;
            Scope = scope ?? string.Empty;
            ScopeDetail = scopeDetail;
            Route = route ?? string.Empty;
            Duty = duty ?? string.Empty;
            DutyDetail = dutyDetail;
            Equipment = equipment ?? string.Empty;
            EquipmentDetail = equipmentDetail;
            OverheatingScenarios = overheatingScenarios ?? string.Empty;
        }

        /// <summary>
        /// Whether this run has a mechanical design duty at all. False on the natural ventilation route
        /// (Iteration 1b), where the review must not present design SUP/EXT columns as though the case had a
        /// mechanical system. Presentation only - no airflow is changed.
        /// </summary>
        public bool HasMechanicalDesignDuty { get; }

        /// <summary>The scenario, as the Hub names it - "Iteration 2 — MVHR with manufacturer unit".</summary>
        public string Scenario { get; }

        /// <summary>The thermal model scope - "Whole building" or "Isolated · Flat 1, Flat 2".</summary>
        public string Scope { get; }

        /// <summary>
        /// What an isolated scope means for the results, or null for the whole building. Shown, not hidden:
        /// an isolated run is a different thermal model and a reader must not have to go looking for that.
        /// </summary>
        public string? ScopeDetail { get; }

        /// <summary>The ventilation route - "MVHR", "NV".</summary>
        public string Route { get; }

        /// <summary>The whole-run design duty in one line.</summary>
        public string Duty { get; }

        /// <summary>What the duty is a total of, for its tooltip.</summary>
        public string? DutyDetail { get; }

        /// <summary>The equipment selection in one line.</summary>
        public string Equipment { get; }

        /// <summary>The rest of what the preparation said about equipment, for its tooltip.</summary>
        public string? EquipmentDetail { get; }

        /// <summary>How many overheating scenarios the run states.</summary>
        public string OverheatingScenarios { get; }

        /// <summary>Every part as one block - what Copy All puts first.</summary>
        public string Text
        {
            get
            {
                System.Text.StringBuilder stringBuilder = new();

                stringBuilder.AppendLine(Scenario);
                stringBuilder.AppendLine(string.Format("Thermal model scope: {0}.{1}", Scope, string.IsNullOrWhiteSpace(ScopeDetail) ? string.Empty : " " + ScopeDetail));
                stringBuilder.AppendLine(string.Format("Route: {0}.", Route));
                stringBuilder.AppendLine(string.Format("Design duty: {0}.{1}", Duty, string.IsNullOrWhiteSpace(DutyDetail) ? string.Empty : " " + DutyDetail));
                stringBuilder.AppendLine(string.Format("Equipment: {0}{1}", Equipment, string.IsNullOrWhiteSpace(EquipmentDetail) ? string.Empty : " " + EquipmentDetail));
                stringBuilder.Append(string.Format("Overheating: {0}.", OverheatingScenarios));

                return stringBuilder.ToString();
            }
        }

        public override string ToString()
        {
            return Text;
        }
    }
}
