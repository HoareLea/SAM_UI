// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// What the Part O TM59 result window says first: the verdict, how many spaces were assessed and how
    /// they came out, why there is no verdict where there is none, and what the result is of.
    ///
    /// <para><b>Presentation only, and read off the production objects</b></para>
    /// <para>
    /// The verdict is the production <see cref="TM59AssessmentReport.OccupiedSpaceComplianceStatus"/>, with
    /// one existing Part O guard in front of a pass: <see cref="Modify.PartialAssessment"/>, the same one
    /// Iteration 2B stops on, which refuses a pass over part of the dwelling scope. Nothing here applies a
    /// criterion, a limit or an hour count, and nothing parses the report text.
    /// </para>
    /// <para>
    /// The per-space counts only COUNT <see cref="TM59AssessmentReport.OccupiedSpaces"/>: each space's overall
    /// status is SAM's <see cref="TM59AssessmentReportSpace.ComplianceStatus"/>, the value the text report's
    /// own "Overall" column prints. No combining rule is restated here, so the counts cannot disagree with the
    /// report or with the verdict above them.
    /// </para>
    /// <para>
    /// <b>One state, two readers.</b> This object is built by <c>Modify.ReviewPartOTM59</c> from the
    /// assessment, BEFORE any window exists; the result window is given it to display and the Hub's outcome
    /// line is worded from the same instance (<c>Modify.ReviewOutcome</c>). Neither reads the other.
    /// </para>
    /// <para>
    /// <b>Not assessed</b> is <see cref="PartOTM59Assessment.SpaceGuids_NoResult"/>: design spaces with no
    /// TM59 result of any kind. The sentences saying why stay in the window's "Not assessed" box.
    /// </para>
    /// <para>
    /// <b>Communal corridor and supplementary &gt;28 °C rows are counted apart</b>, as the report keeps
    /// them: a risk reference and information only, never an occupied-space pass or fail.
    /// </para>
    /// </summary>
    public class PartOTM59ResultSummary
    {
        /// <summary>One labelled line of the result's context.</summary>
        public class Fact
        {
            public Fact(string label, string value, string? detail = null)
            {
                Label = label ?? string.Empty;
                Value = value ?? string.Empty;
                Detail = string.IsNullOrWhiteSpace(detail) ? null : detail;
            }

            public string Label { get; }

            public string Value { get; }

            /// <summary>The tooltip - a full path, or what a short value stands for.</summary>
            public string? Detail { get; }
        }

        private PartOTM59ResultSummary(PartOTM59Verdict partOTM59Verdict, TM59ComplianceStatus? tM59ComplianceStatus, string? counts, string? countsDetail, string? reason, List<Fact> facts)
        {
            Verdict = partOTM59Verdict;
            ComplianceStatus = tM59ComplianceStatus;
            Counts = counts;
            CountsDetail = countsDetail;
            Reason = string.IsNullOrWhiteSpace(reason) ? null : reason;
            Facts = facts ?? [];
        }

        public PartOTM59Verdict Verdict { get; }

        /// <summary>
        /// The production occupied-space status the verdict was read from, or null where no assessment was
        /// produced. What <see cref="Modify.AssessPartOTM59"/> returns, unchanged.
        /// </summary>
        public TM59ComplianceStatus? ComplianceStatus { get; }

        /// <summary>"8 spaces assessed · 2 pass · 6 fail · 1 not assessed", or null where nothing was assessed.</summary>
        public string? Counts { get; }

        /// <summary>The processed/assessed sentence per criterion list, for the counts line's tooltip.</summary>
        public string? CountsDetail { get; }

        /// <summary>Why there is no pass or fail, where there is none.</summary>
        public string? Reason { get; }

        /// <summary>What the result is of: scenario, route, thermal model, weather, results file, report, method.</summary>
        public List<Fact> Facts { get; }

        public int SpaceCount_Assessed { get; private set; }

        public int SpaceCount_Pass { get; private set; }

        public int SpaceCount_Fail { get; private set; }

        /// <summary>Occupied spaces whose every check was not applicable - neither a pass nor a fail.</summary>
        public int SpaceCount_NotApplicable { get; private set; }

        public int SpaceCount_NotAssessed { get; private set; }

        /// <summary>The verdict in capitals - the word the heading carries.</summary>
        public string VerdictText => Verdict switch
        {
            PartOTM59Verdict.Pass => "PASS",
            PartOTM59Verdict.Fail => "FAIL",
            PartOTM59Verdict.NotAssessed => "NOT ASSESSED",
            _ => "UNAVAILABLE",
        };

        /// <summary>The verdict in sentence case, for the Hub's outcome line.</summary>
        public string VerdictWord => Verdict switch
        {
            PartOTM59Verdict.Pass => "Pass",
            PartOTM59Verdict.Fail => "Fail",
            PartOTM59Verdict.NotAssessed => "Not assessed",
            _ => "Unavailable",
        };

        /// <summary>The Part O glyph beside the word, so the verdict never depends on colour alone.</summary>
        public string Glyph => Verdict switch
        {
            PartOTM59Verdict.Pass => "✓",
            PartOTM59Verdict.Fail => "✕",
            PartOTM59Verdict.NotAssessed => "–",
            _ => "!",
        };

        /// <summary>Whether there is a pass or fail to report.</summary>
        public bool HasVerdict => Verdict == PartOTM59Verdict.Pass || Verdict == PartOTM59Verdict.Fail;

        public string Heading => string.Format("TM59 assessment — {0}", VerdictText);

        /// <summary>
        /// The report's own caveat, on one line: a TM59 result is an assessment of simulated temperatures and
        /// not, by itself, a statement that the Part O modelling assumptions were applied.
        /// </summary>
        public static string Caveat => TM59AssessmentReportFormatter.Caveat.Replace("\r\n", " ");

        /// <summary>Everything above, as one block - what Copy All puts first.</summary>
        public string Text
        {
            get
            {
                StringBuilder stringBuilder = new();

                stringBuilder.AppendLine(Heading);

                if (!string.IsNullOrWhiteSpace(Counts))
                {
                    stringBuilder.AppendLine(Counts);
                }

                if (Reason is not null)
                {
                    stringBuilder.AppendLine(Reason);
                }

                foreach (Fact fact in Facts)
                {
                    stringBuilder.AppendLine(string.Format("{0}: {1}{2}", fact.Label, fact.Value, fact.Detail is null ? string.Empty : string.Format(" ({0})", fact.Detail)));
                }

                if (!string.IsNullOrWhiteSpace(CountsDetail))
                {
                    stringBuilder.AppendLine(CountsDetail);
                }

                stringBuilder.Append(Caveat);

                return stringBuilder.ToString();
            }
        }

        public override string ToString()
        {
            return Text;
        }

        /// <summary>
        /// The summary of a production assessment of <paramref name="partORun"/>.
        /// </summary>
        /// <param name="countsDetail">The processed/assessed sentence (<c>Modify.Summary</c>), for the counts tooltip.</param>
        public static PartOTM59ResultSummary Create(PartOTM59Assessment partOTM59Assessment, PartORun? partORun, string? countsDetail = null, string? path_TM59Report = null, string? refusal_Report = null)
        {
            if (partOTM59Assessment is null || !partOTM59Assessment.IsAssessed)
            {
                return Unavailable(partOTM59Assessment?.Refusal ?? "No TM59 assessment was produced.", partORun);
            }

            //The existing guard, asked of a pass only - exactly where Iteration 2B asks it.
            string? refusal_Partial = null;

            if (partOTM59Assessment.OccupiedSpaceComplianceStatus == TM59ComplianceStatus.Pass && partOTM59Assessment.SpaceGuids_Unassessed.Count != 0)
            {
                refusal_Partial = partORun?.PreparationContext is null
                    ? string.Format(
                        "The production TM59 assessment returned a pass, but {0} produced no result at all, and this run records no dwelling scope to show they were outside it. A pass that may be over part of the dwellings is not reported as one.",
                        UI.Query.PartOCount(partOTM59Assessment.SpaceGuids_Unassessed.Count, "space", "spaces"))
                    : Modify.PartialAssessment(partORun.AnalyticalModel_Assessment, partORun.PreparationContext, partOTM59Assessment);
            }

            return Create(partOTM59Assessment.Report!, partOTM59Assessment.SpaceGuids_NoResult.Count, refusal_Partial, RunFacts(partORun, partOTM59Assessment.Report, path_TM59Report, refusal_Report), countsDetail);
        }

        /// <summary>
        /// The summary from the report itself, the count of spaces with no result, and the partial-scope
        /// refusal already asked - separated from <see cref="Create(PartOTM59Assessment, PartORun, string, string, string)"/>
        /// so the verdict and the counts can be pinned without a TSD.
        /// </summary>
        /// <remarks>Internal rather than private so the verdict and counts are pinned by tests.</remarks>
        internal static PartOTM59ResultSummary Create(TM59AssessmentReport tM59AssessmentReport, int spaceCount_NotAssessed, string? refusal_Partial, List<Fact>? facts = null, string? countsDetail = null)
        {
            TM59ComplianceStatus tM59ComplianceStatus = tM59AssessmentReport.OccupiedSpaceComplianceStatus;

            int pass = 0;
            int fail = 0;
            int notApplicable = 0;

            //Counted, never decided: each occupied space's overall status is SAM's own
            //(TM59AssessmentReportSpace.ComplianceStatus - the value the report's Overall column prints).
            foreach (TM59AssessmentReportSpace tM59AssessmentReportSpace in tM59AssessmentReport.OccupiedSpaces ?? [])
            {
                switch (tM59AssessmentReportSpace?.ComplianceStatus)
                {
                    case TM59ComplianceStatus.Fail:
                        fail++;
                        break;

                    case TM59ComplianceStatus.Pass:
                        pass++;
                        break;

                    case null:
                        break;

                    default:
                        notApplicable++;
                        break;
                }
            }

            int assessed = pass + fail + notApplicable;

            PartOTM59Verdict partOTM59Verdict;
            string? reason = null;

            switch (tM59ComplianceStatus)
            {
                case TM59ComplianceStatus.Fail:
                    //A failure is certain whatever else went unassessed; the counts say what did.
                    partOTM59Verdict = PartOTM59Verdict.Fail;
                    break;

                case TM59ComplianceStatus.Pass:
                    partOTM59Verdict = refusal_Partial is null ? PartOTM59Verdict.Pass : PartOTM59Verdict.NotAssessed;
                    reason = refusal_Partial;
                    break;

                default:
                    partOTM59Verdict = PartOTM59Verdict.NotAssessed;
                    reason = assessed == 0
                        ? "No occupied space was assessed against a TM59 criterion, so there is no pass or fail to report. Why each space was not assessed is listed below."
                        : string.Format("The production assessment reached no occupied-space pass or fail (its combined status is '{0}'), so there is none to report.", Core.Query.Description(tM59ComplianceStatus));
                    break;
            }

            List<string> parts =
            [
                UI.Query.PartOCount(assessed, "space assessed", "spaces assessed"),
                string.Format("{0} pass", pass),
                string.Format("{0} fail", fail),
            ];

            if (notApplicable != 0)
            {
                parts.Add(string.Format("{0} not applicable", notApplicable));
            }

            parts.Add(string.Format("{0} not assessed", spaceCount_NotAssessed));

            List<Fact> result_Facts = [.. facts ?? []];

            //Kept apart from the occupied-space counts, as the report keeps them.
            if (tM59AssessmentReport.CorridorChecks is { Count: > 0 } corridorChecks)
            {
                result_Facts.Add(new Fact(
                    "Communal corridor",
                    string.Format("{0} · {1}", tM59AssessmentReport.CorridorRiskStatus == TM59RiskStatus.SignificantRisk ? "Significant risk" : "Acceptable", UI.Query.PartOCount(corridorChecks.Select(x => x.Reference).Distinct().Count(), "corridor", "corridors")),
                    "The >28 °C communal-corridor reference threshold. A risk for design review, never an occupied-space pass or fail."));
            }

            if (tM59AssessmentReport.SupplementaryChecks is { Count: > 0 } supplementaryChecks)
            {
                result_Facts.Add(new Fact(
                    "Other >28 °C checks",
                    string.Format("{0} · information only", UI.Query.PartOCount(supplementaryChecks.Select(x => x.Reference).Distinct().Count(), "space", "spaces")),
                    "Spaces with no assessed occupied-space use, checked against >28 °C for information. Not a TM59 criterion."));
            }

            return new PartOTM59ResultSummary(partOTM59Verdict, tM59ComplianceStatus, string.Join(" · ", parts), countsDetail, reason, result_Facts)
            {
                SpaceCount_Assessed = assessed,
                SpaceCount_Pass = pass,
                SpaceCount_Fail = fail,
                SpaceCount_NotApplicable = notApplicable,
                SpaceCount_NotAssessed = spaceCount_NotAssessed,
            };
        }

        /// <summary>
        /// The verdict of an Iteration 3 case that WAS assessed, from its production occupied-space status alone -
        /// so the case's report window is headed by that verdict, never by "unavailable". Pass and fail only; any
        /// other status is "not assessed". No counts: the case carries its report text, not its report object.
        /// </summary>
        internal static PartOTM59ResultSummary ForStatus(TM59ComplianceStatus tM59ComplianceStatus)
        {
            return tM59ComplianceStatus switch
            {
                TM59ComplianceStatus.Pass => new PartOTM59ResultSummary(PartOTM59Verdict.Pass, tM59ComplianceStatus, null, null, null, []),
                TM59ComplianceStatus.Fail => new PartOTM59ResultSummary(PartOTM59Verdict.Fail, tM59ComplianceStatus, null, null, null, []),
                _ => new PartOTM59ResultSummary(PartOTM59Verdict.NotAssessed, tM59ComplianceStatus, null, null, string.Format("The production assessment reached no occupied-space pass or fail (its combined status is '{0}').", Core.Query.Description(tM59ComplianceStatus)), []),
            };
        }

        /// <summary>
        /// There is no assessment: the run's results are missing, stale or could not be read. Stated in the
        /// window with the reason, never as a pass or a fail.
        /// </summary>
        public static PartOTM59ResultSummary Unavailable(string? reason, PartORun? partORun)
        {
            return new PartOTM59ResultSummary(PartOTM59Verdict.Unavailable, null, null, null, reason ?? "No TM59 assessment was produced.", RunFacts(partORun, null, null, null));
        }

        /// <summary>
        /// What the result is of, from the run's own record. A fact the run does not hold is left out, never
        /// guessed: a reopened run carries no simulation context, so it states no weather file.
        /// </summary>
        /// <summary>
        /// The provenance block a saved plain-text TM59 report is headed with, so the file is understandable
        /// when opened later without the GUI. Built from the SAME facts the result window shows
        /// (<see cref="RunFacts"/>) - never a second source of truth - with the full results path rather than
        /// its file name, and a weather line that says so where this session does not hold one.
        /// </summary>
        /// <param name="lines_Context">
        /// Lines stated directly under the scenario, where the results serve a further assessment - such as
        /// the Iteration 3 case the report is. Null or empty for an ordinary report, which then states no
        /// context at all.
        /// </param>
        /// <param name="scenario">
        /// What the results are of, where that is not the run's own scenario - the Iteration 3 system case's
        /// bridge results. Null states the run's.
        /// </param>
        internal static List<string> ReportProvenance(PartORun? partORun, TM59AssessmentReport? tM59AssessmentReport, IEnumerable<(string Label, string Value)>? lines_Context = null, string? scenario = null, string? path_TSD = null)
        {
            List<Fact> facts = RunFacts(partORun, tM59AssessmentReport, null, null);

            //The scenario first - it is what the results file physically is - and then any assessment it serves.
            List<(string Label, string Value)> lines = [];

            string? scenario_Stated = scenario ?? facts.Find(x => x.Label == "Scenario")?.Value;
            if (!string.IsNullOrWhiteSpace(scenario_Stated))
            {
                lines.Add(("Scenario", scenario_Stated!));
            }

            lines.AddRange(lines_Context ?? []);

            bool weather = false;

            foreach (Fact fact in facts)
            {
                switch (fact.Label)
                {
                    case "Scenario":
                        break;

                    case "Thermal model":
                        lines.Add(("Thermal model scope", string.IsNullOrWhiteSpace(fact.Detail) ? fact.Value : string.Format("{0} ({1})", fact.Value, fact.Detail)));
                        break;

                    case "Weather":
                        weather = true;
                        lines.Add(("Weather", fact.Value));
                        break;

                    case "Results":
                        if (path_TSD is null)
                        {
                            lines.Add(("Source TAS result", fact.Detail ?? fact.Value));
                        }
                        break;

                    case "Method":
                        lines.Add(("TM59 method", fact.Value));
                        break;

                    default:
                        lines.Add((fact.Label, fact.Value));
                        break;
                }
            }

            if (path_TSD is not null)
            {
                lines.Add(("Source TAS result", path_TSD));
            }

            if (!weather && partORun is not null)
            {
                lines.Add(("Weather", "not recorded in this session (the run was reopened from a saved model)"));
            }

            int width = 0;
            foreach ((string label, string _) in lines)
            {
                width = System.Math.Max(width, label.Length);
            }

            List<string> result = [];
            foreach ((string label, string value) in lines)
            {
                result.Add(string.Format("{0} {1}", (label + ":").PadRight(width + 1), value));
            }

            return result;
        }

        internal static List<Fact> RunFacts(PartORun? partORun, TM59AssessmentReport? tM59AssessmentReport, string? path_TM59Report, string? refusal_Report)
        {
            List<Fact> result = [];

            if (partORun is null)
            {
                return result;
            }

            PartOWorkflowScenario? partOWorkflowScenario = PartOWorkflowScenario.Find(partORun.PreparationContext);
            if (partOWorkflowScenario is not null)
            {
                result.Add(new Fact("Scenario", partOWorkflowScenario.Text));

                if (partOWorkflowScenario.Option is not null)
                {
                    result.Add(new Fact("Route", Query.PartOVentilationRouteText(partOWorkflowScenario.Option.PartOVentilationMode, partOWorkflowScenario.Option.VentilationStrategy)));
                }
            }

            AnalyticalModel? analyticalModel = partORun.AnalyticalModel_Assessment;
            if (analyticalModel is not null)
            {
                string scope = Query.PartOThermalModelScopeText(analyticalModel.GetValue<PartOIsolationContext>(Analytical.AnalyticalModelParameter.PartOIsolationContext), out string? scopeDetail);

                result.Add(new Fact("Thermal model", scope, scopeDetail));
            }

            string? weather = partORun.SimulationContext?.WeatherData?.Name;
            if (!string.IsNullOrWhiteSpace(weather))
            {
                result.Add(new Fact("Weather", weather!));
            }

            string? path_TSD = partORun.Path_TSD;
            if (!string.IsNullOrWhiteSpace(path_TSD))
            {
                result.Add(new Fact("Results", System.IO.Path.GetFileName(path_TSD), path_TSD));
            }

            if (!string.IsNullOrWhiteSpace(path_TM59Report))
            {
                result.Add(new Fact("Report saved", System.IO.Path.GetFileName(path_TM59Report), path_TM59Report));
            }
            else if (!string.IsNullOrWhiteSpace(refusal_Report))
            {
                result.Add(new Fact("Report saved", "Not saved", refusal_Report));
            }

            if (tM59AssessmentReport is not null)
            {
                //The category the results themselves report; left off where they state none.
                result.Add(new Fact("Method", tM59AssessmentReport.TM52BuildingCategory == TM52BuildingCategory.Undefined
                    ? "CIBSE TM59:2017"
                    : string.Format("CIBSE TM59:2017 · TM52 {0}", tM59AssessmentReport.TM52BuildingCategory.Description())));
            }

            return result;
        }
    }
}
