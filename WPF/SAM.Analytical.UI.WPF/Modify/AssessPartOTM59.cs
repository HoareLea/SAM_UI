// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Tas;
using SAM.Core.UI.WPF;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        /// <summary>
        /// Assesses a completed Part O run against the CIBSE TM59 criteria and shows the production report.
        /// <para>
        /// <b>The model it assesses is the one the TAS workflow returned</b> -
        /// <see cref="PartORun.AnalyticalModel_Assessment"/>, which exists only in
        /// <see cref="PartORunState.WorkflowCompleted"/>. There is no fallback: not the loaded model, not the
        /// preparation output, not a model read back off disk. A TM59 query resolves a simulated space to a
        /// design space through <c>SpaceParameter.ZoneGuid</c>, and only the model the workflow returns carries
        /// the current TAS zone identities - handing it the preparation output produces an incomplete
        /// <c>SimulationSpaceMap</c> and refuses every space, which is a silent empty answer rather than an
        /// error.
        /// </para>
        /// <para>
        /// <b>The assessment itself is entirely SAM's.</b> This method performs the same sequence the accepted
        /// <c>Tas.TSDQueryTM59Results</c> component performs - <c>Convert.ToSAM(TSD)</c>,
        /// <c>Create.TM59AssessmentCalculator</c>, <c>OverheatingScenarioMap</c>,
        /// <c>RestoreDesignInternalConditions</c>, <c>Spaces</c>, <c>Calculate</c>,
        /// <c>TM59AssessmentReport</c> - and computes no criterion, limit or verdict of its own. The
        /// natural-ventilation criteria use their own summer and night subsets, and the mechanical criterion
        /// its annual one; none of those numbers is stated here.
        /// </para>
        /// <para>
        /// <b>Not the same thing as the simulation window's "Domestic Overheating" tick.</b> That writes the
        /// TAS DomOv XML for TAS to assess. This reads the TSD and produces SAM's own TM59 assessment.
        /// </para>
        /// </summary>
        /// <returns>
        /// The production occupied-space status of the assessment that was shown, or null where nothing was
        /// assessed. Callers that ignore it are unaffected.
        /// </returns>
        public static TM59ComplianceStatus? AssessPartOTM59(this PartORun partORun, IWin32Window? owner = null)
        {
            return ReviewPartOTM59(partORun, owner)?.ComplianceStatus;
        }

        /// <summary>
        /// <see cref="AssessPartOTM59"/>, returning what the result window was headed with - so the Part O Hub
        /// says inline exactly what the window said, including a pass the window would not report as one.
        /// <para>
        /// <b>Every outcome opens the result window.</b> Results that are missing, stale or unreadable open it
        /// as UNAVAILABLE with the reason, where they used to be a message box - never a pass or a fail.
        /// </para>
        /// </summary>
        /// <returns>The summary shown, or null where there was no run.</returns>
        internal static PartOTM59ResultSummary? ReviewPartOTM59(PartORun partORun, IWin32Window? owner = null)
        {
            if (partORun is null)
            {
                return null;
            }

            //Inside a Part O operation that shows its own progress window, the reading is reported there, and
            //that window is hidden before anything modal appears over it.
            PartOProgressHost? partOProgressHost = PartOProgressHost.Current;

            //One gate, and it re-checks the results file rather than trusting the state alone.
            if (!partORun.IsAssessable(out string refusal))
            {
                partOProgressHost?.Hide();

                return ShowResult(PartOTM59ResultSummary.Unavailable(string.Format("The Part O TM59 assessment did not run. {0}", refusal).Trim(), partORun), null, null, null, null, owner);
            }

            AnalyticalModel? analyticalModel_Workflow = partORun.AnalyticalModel_Assessment;
            string? path_TSD = partORun.Path_TSD;

            if (analyticalModel_Workflow is null || string.IsNullOrWhiteSpace(path_TSD))
            {
                //Unreachable through IsAssessable, and asserted rather than assumed: a null slipping through
                //here is exactly the substitution this command exists to make impossible.
                partOProgressHost?.Hide();

                return ShowResult(PartOTM59ResultSummary.Unavailable("The Part O run reports it can be assessed but carries no workflow model or no results path. Nothing was assessed.", partORun), null, null, null, null, owner);
            }

            PartOTM59Assessment partOTM59Assessment;

            //Reviewed on its own - the Hub's Review Results - the reading gets the same Part O progress window
            //as every other Part O operation, rather than a generic box. One stage, because it is one call; no
            //Cancel, because that call observes none.
            using (PartOProgressHost? partOProgressHost_Review = partOProgressHost is null
                ? new(
                    "Checking TM59 results",
                    string.Format("{0} · reading the saved results; no TAS simulation is run.", Query.PartOIterationText(partORun)),
                    ["TM59 assessment"],
                    false)
                : null)
            {
                PartOProgressHost partOProgressHost_Assessment = partOProgressHost ?? partOProgressHost_Review!;

                partOProgressHost_Review?.Start(0);

                partOProgressHost_Assessment.Detail("Reading the simulation results and assessing them against CIBSE TM59");

                //The whole assessment, in the one place that owns it - so this command and the Iteration 2B
                //optimisation can never disagree about what TM59 said. See PartOTM59Assessment.
                partOTM59Assessment = PartOTM59Assessment.Assess(analyticalModel_Workflow, path_TSD, partORun.OverheatingScenarios);

                partOProgressHost_Assessment.Detail(null);

                //The result window is modal; a topmost window on another thread must not sit over it. Its own
                //window closes instead, as the using ends.
                partOProgressHost?.Hide();
            }

            if (!partOTM59Assessment.IsAssessed)
            {
                return ShowResult(PartOTM59ResultSummary.Unavailable(string.Format("The simulation results at '{0}' could not be assessed. {1}", path_TSD, partOTM59Assessment.Refusal).Trim(), partORun), null, partOTM59Assessment.AssociationRefusals, null, null, owner);
            }

            TM59AssessmentResult tM59AssessmentResult = partOTM59Assessment.Result!;
            TM59AssessmentReport tM59AssessmentReport = partOTM59Assessment.Report!;

            //The durable artifact: every assessment of a run persists its report beside that run's own
            //results, whether it was produced in-session or reviewed from a reopened model. A failure to
            //write is reported, never silent - but it fails nothing: the assessment itself is already done.
            bool reportSaved = SavePartOTM59Report(path_TSD, tM59AssessmentReport, out string? path_TM59Report, out string? refusal_Report, PartOTM59ResultSummary.ReportProvenance(partORun, tM59AssessmentReport));

            string summary = Summary(partORun, tM59AssessmentResult, tM59AssessmentReport, reportSaved, path_TM59Report, refusal_Report);

            PartOTM59ResultSummary partOTM59ResultSummary = PartOTM59ResultSummary.Create(partOTM59Assessment, partORun, summary, reportSaved ? path_TM59Report : null, reportSaved ? null : refusal_Report);

            //The production report text, verbatim.
            return ShowResult(partOTM59ResultSummary, tM59AssessmentReport.ToString(), partOTM59Assessment.AssociationRefusals, tM59AssessmentResult.VentilationStrategyRefusals, summary, owner);
        }

        /// <summary>
        /// Shows the summary and returns THAT summary - the caller's own object, never anything read back
        /// off the window - so the Hub's line and the window are two readers of one state.
        /// </summary>
        private static PartOTM59ResultSummary ShowResult(PartOTM59ResultSummary partOTM59ResultSummary, string? report, IEnumerable<string>? associationRefusals, IEnumerable<string>? ventilationStrategyRefusals, string? summary, IWin32Window? owner)
        {
            PartOTM59ResultWindow partOTM59ResultWindow = ResultWindow(partOTM59ResultSummary, report, associationRefusals, ventilationStrategyRefusals, summary);

            if (owner is not null)
            {
                new System.Windows.Interop.WindowInteropHelper(partOTM59ResultWindow).Owner = owner.Handle;
            }

            partOTM59ResultWindow.ShowDialog();

            return partOTM59ResultSummary;
        }

        /// <summary>The result window, filled and not yet shown.</summary>
        /// <remarks>Internal rather than private so what the window is given is pinned by tests.</remarks>
        internal static PartOTM59ResultWindow ResultWindow(PartOTM59ResultSummary partOTM59ResultSummary, string? report, IEnumerable<string>? associationRefusals, IEnumerable<string>? ventilationStrategyRefusals, string? summary)
        {
            PartOTM59ResultWindow partOTM59ResultWindow = new()
            {
                Report = report ?? string.Empty,
                Summary = summary ?? string.Empty,
            };

            partOTM59ResultWindow.SetDiagnostics(associationRefusals ?? [], ventilationStrategyRefusals ?? []);

            partOTM59ResultWindow.ResultSummary = partOTM59ResultSummary;

            return partOTM59ResultWindow;
        }

        /// <summary>
        /// The one-line account of what the assessment covered. <b>"Processed" is the simulated spaces the
        /// calculation ran over; "assessed" is the ones that produced a result.</b> The two differ where a
        /// space reached the calculation but no criterion produced a verdict for it - the corridor that is
        /// processed and named as not assessed, for instance - and wording them as one ("Assessed 9
        /// space(s)") read as though every one of the nine had been.
        /// </summary>
        private static string Summary(PartORun partORun, TM59AssessmentResult tM59AssessmentResult, TM59AssessmentReport tM59AssessmentReport, bool reportSaved, string? path_TM59Report, string? refusal_Report)
        {
            SplitCorridorResults(tM59AssessmentResult.CorridorResults, tM59AssessmentReport, out List<TMResult> communalCorridor, out List<TMResult> supplementary);

            return Summary(
                tM59AssessmentResult.Spaces?.Count ?? 0,
                partORun.Path_TSD,
                tM59AssessmentResult.NaturalVentilationResults,
                tM59AssessmentResult.MechanicalVentilationResults,
                communalCorridor,
                supplementary,
                reportSaved,
                path_TM59Report,
                refusal_Report);
        }

        /// <summary>
        /// Splits the calculation's corridor BUCKET the way the report does. That bucket carries the &gt;28 C row
        /// of every space the calculation gave the check - communal corridors, and also bathrooms and ensuites -
        /// and only the report tells them apart, by the internal condition each space is assigned
        /// (<see cref="TM59AssessmentReport.CorridorChecks"/> vs <see cref="TM59AssessmentReport.SupplementaryChecks"/>).
        /// So the split is read off the report rather than restated: an information-only bathroom row is never
        /// counted as a communal corridor.
        /// </summary>
        /// <remarks>Internal rather than private so the split is pinned by tests.</remarks>
        internal static void SplitCorridorResults(IEnumerable<TMResult>? corridorResults, TM59AssessmentReport? tM59AssessmentReport, out List<TMResult> communalCorridorResults, out List<TMResult> supplementaryResults)
        {
            HashSet<string> references_CommunalCorridor = [.. (tM59AssessmentReport?.CorridorChecks ?? []).Select(x => x.Reference).Where(x => !string.IsNullOrWhiteSpace(x))];

            communalCorridorResults = [];
            supplementaryResults = [];

            foreach (TMResult tMResult in corridorResults ?? [])
            {
                if (tMResult is null)
                {
                    continue;
                }

                if (tMResult.Reference is not null && references_CommunalCorridor.Contains(tMResult.Reference))
                {
                    communalCorridorResults.Add(tMResult);
                }
                else
                {
                    supplementaryResults.Add(tMResult);
                }
            }
        }

        /// <summary>
        /// The same sentence from the plain counts and lists - separated from <c>TM59AssessmentResult</c> so
        /// the wording can be pinned by tests without constructing one (its constructor is internal to
        /// SAM.Analytical).
        /// </summary>
        /// <remarks>Internal rather than private so the wording is pinned by tests.</remarks>
        /// <param name="communalCorridorResults">The report's communal-corridor rows (the TM59 corridor criterion).</param>
        /// <param name="supplementaryResults">
        /// The other &gt;28 C rows the same calculation produced - bathrooms, ensuites - which the report shows as
        /// information only. Counted apart, and never called a corridor.
        /// </param>
        internal static string Summary(int processed, string? path_TSD, IEnumerable<TMResult>? naturalVentilationResults, IEnumerable<TMResult>? mechanicalVentilationResults, IEnumerable<TMResult>? communalCorridorResults, IEnumerable<TMResult>? supplementaryResults, bool reportSaved, string? path_TM59Report, string? refusal_Report)
        {
            //Counts only. Which criterion applies, what its limit is and whether a space passes are all in the
            //report below, stated by the assessment itself.
            List<TMResult> naturalVentilation = [.. naturalVentilationResults ?? []];
            List<TMResult> mechanicalVentilation = [.. mechanicalVentilationResults ?? []];
            List<TMResult> corridor = [.. communalCorridorResults ?? []];
            List<TMResult> supplementary = [.. supplementaryResults ?? []];

            //One result per space, but counted as distinct references rather than assumed: the count is the
            //answer to "how many spaces were assessed", and assuming one-result-per-space is how a duplicate
            //would hide.
            HashSet<string> references = [];
            foreach (TMResult tMResult in naturalVentilation.Concat(mechanicalVentilation).Concat(corridor).Concat(supplementary))
            {
                if (!string.IsNullOrWhiteSpace(tMResult?.Reference))
                {
                    references.Add(tMResult!.Reference);
                }
            }

            int assessed = references.Count;

            string result = string.Format(
                "Processed {0} simulated space(s) from '{1}': {2} assessed ({3} natural ventilation, {4} mechanical ventilation, {5} communal corridor, {6} supplementary >28 C information only), {7} not assessed. The model assessed is the one the TAS workflow returned.",
                processed,
                path_TSD,
                assessed,
                naturalVentilation.Count,
                mechanicalVentilation.Count,
                corridor.Count,
                supplementary.Count,
                processed - assessed);

            result += reportSaved
                ? string.Format(" The report was saved to '{0}'.", path_TM59Report)
                : string.Format(" {0}", refusal_Report);

            return result;
        }
    }
}
