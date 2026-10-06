// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;

namespace SAM.Analytical.UI
{
    /// <summary>
    /// What one side of an Iteration 3 pairing took from the <b>existing</b> TM59 assessment - carried
    /// across the pipeline seam so the orchestration can be exercised without a licensed TAS environment.
    ///
    /// <para><b>Every value is <c>PartOTM59Assessment</c>'s, copied</b></para>
    /// <para>
    /// The statuses, the counts, the limits, the combined verdict and the report text are all produced by
    /// the production TM59 authority and are reproduced here without change. Nothing on this type is
    /// derived, rounded, re-classified or recomputed - there is no criterion, threshold, exceedance rule
    /// or verdict in this assembly and there must not be one.
    /// </para>
    /// <para>
    /// <b>Why it is a separate type at all.</b> <c>TM59AssessmentResult</c> and
    /// <c>TM59AssessmentReport</c> can only be constructed by <c>SAM.Analytical</c> itself, from a real
    /// results file. A test that cannot build one cannot exercise a single refusal boundary of the A/B
    /// pipeline - so the seam carries what the orchestration actually reads, and the production
    /// implementation is the only thing that ever produces one from a TSD.
    /// </para>
    /// </summary>
    public class PartOIteration3Assessment
    {
        private readonly List<PartOTM59SpaceResult> spaceResults = [];

        private readonly List<string> associationRefusals = [];

        private readonly List<string> ventilationStrategyRefusals = [];

        private readonly List<Guid> spaceGuids_Unassessed = [];

        private readonly Dictionary<Guid, double[]> resultantTemperatures = [];

        private readonly List<Guid> spaceGuids_InformationOnly = [];

        public PartOIteration3Assessment(
            bool assessed,
            string refusal,
            TM59ComplianceStatus occupiedSpaceComplianceStatus,
            IEnumerable<PartOTM59SpaceResult> spaceResults,
            IEnumerable<string> associationRefusals,
            IEnumerable<string> ventilationStrategyRefusals,
            IEnumerable<Guid> spaceGuids_Unassessed,
            IDictionary<Guid, double[]> resultantTemperatures,
            string reportText,
            string path_Report,
            int count_Processed,
            string refusal_Report = null,
            IEnumerable<Guid> spaceGuids_InformationOnly = null)
        {
            IsAssessed = assessed && refusal is null;
            Refusal = refusal;
            OccupiedSpaceComplianceStatus = occupiedSpaceComplianceStatus;
            ReportText = reportText;
            Path_Report = path_Report;
            Refusal_Report = refusal_Report;
            Count_Processed = count_Processed;

            foreach (PartOTM59SpaceResult partOTM59SpaceResult in spaceResults ?? [])
            {
                if (partOTM59SpaceResult is not null)
                {
                    this.spaceResults.Add(partOTM59SpaceResult);
                }
            }

            foreach (string refusal_Association in associationRefusals ?? [])
            {
                if (!string.IsNullOrWhiteSpace(refusal_Association))
                {
                    this.associationRefusals.Add(refusal_Association);
                }
            }

            foreach (string refusal_Strategy in ventilationStrategyRefusals ?? [])
            {
                if (!string.IsNullOrWhiteSpace(refusal_Strategy))
                {
                    this.ventilationStrategyRefusals.Add(refusal_Strategy);
                }
            }

            foreach (Guid guid in spaceGuids_Unassessed ?? [])
            {
                this.spaceGuids_Unassessed.Add(guid);
            }

            foreach (Guid guid in spaceGuids_InformationOnly ?? [])
            {
                if (!this.spaceGuids_InformationOnly.Contains(guid))
                {
                    this.spaceGuids_InformationOnly.Add(guid);
                }
            }

            foreach (KeyValuePair<Guid, double[]> keyValuePair in resultantTemperatures ?? new Dictionary<Guid, double[]>())
            {
                if (keyValuePair.Value is not null)
                {
                    this.resultantTemperatures[keyValuePair.Key] = keyValuePair.Value;
                }
            }
        }

        /// <summary>Whether there is an assessment to read.</summary>
        public bool IsAssessed { get; }

        /// <summary>Why there is none, in the assessment's own words.</summary>
        public string Refusal { get; }

        /// <summary>The production combined occupied-space verdict. Never recomputed here.</summary>
        public TM59ComplianceStatus OccupiedSpaceComplianceStatus { get; }

        /// <summary>Every occupied-space criterion outcome, keyed to its design space.</summary>
        public List<PartOTM59SpaceResult> SpaceResults => [.. spaceResults];

        /// <summary>Rooms that could not be resolved between the simulation and the design.</summary>
        public List<string> AssociationRefusals => [.. associationRefusals];

        /// <summary>Rooms with no stated ventilation strategy, in the calculation's own words.</summary>
        public List<string> VentilationStrategyRefusals => [.. ventilationStrategyRefusals];

        /// <summary>Design spaces this assessment produced no result for.</summary>
        public List<Guid> SpaceGuids_Unassessed => [.. spaceGuids_Unassessed];

        /// <summary>
        /// The hourly resultant temperature series the assessment read, by design space guid. Empty where
        /// capture was not asked for.
        /// </summary>
        public Dictionary<Guid, double[]> ResultantTemperatures => new(resultantTemperatures);

        /// <summary>
        /// Design spaces reported only as supplementary &gt;28 C information - no occupied-space criterion. Not in
        /// <see cref="SpaceResults"/>, and not unassessed either.
        /// </summary>
        public List<Guid> SpaceGuids_InformationOnly => [.. spaceGuids_InformationOnly];

        /// <summary>The production report an engineer reads, verbatim.</summary>
        public string ReportText { get; }

        /// <summary>
        /// Where THIS assessment wrote that report, or null where it did not. Never the path of a write
        /// that failed: an earlier file still sitting there is not this assessment's report.
        /// </summary>
        public string Path_Report { get; }

        /// <summary>Why no report was written, in the writer's own words, or null.</summary>
        public string Refusal_Report { get; }

        /// <summary>How many simulated spaces the calculation ran over.</summary>
        public int Count_Processed { get; }

        /// <summary>One room's series, or null.</summary>
        public double[] ResultantTemperature(Guid guid_Space)
        {
            return resultantTemperatures.TryGetValue(guid_Space, out double[] result) ? result : null;
        }

        public override string ToString()
        {
            return IsAssessed
                ? string.Format("TM59: {0} over {1} space(s)", Core.Query.Description(OccupiedSpaceComplianceStatus), Count_Processed)
                : string.Format("TM59 REFUSED: {0}", Refusal ?? "no reason was given.");
        }
    }
}
