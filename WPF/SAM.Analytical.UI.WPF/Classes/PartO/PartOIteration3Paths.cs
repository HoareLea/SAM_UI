// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.IO;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Where every file of one Approved Document O Iteration 3 pairing lives - <b>the one naming
    /// authority</b>, derived from Reference A's own output directory and project name and from nothing
    /// else.
    ///
    /// <para><b>Deterministic, so the pairing is reopenable</b></para>
    /// <para>
    /// A review in a later session has to find Candidate B's artifacts and prove they are the ones the run
    /// produced. That is only possible if the run and the review derive the same paths from the same two
    /// facts - so both ask this, exactly as every writer and reader of the per-run model already asks
    /// <see cref="Query.Path_PartORunModel(string)"/> and every TM59 report asks
    /// <see cref="Query.Path_TM59Report(string)"/>.
    /// </para>
    ///
    /// <para><b>And therefore stale-prone, which is handled rather than avoided</b></para>
    /// <para>
    /// Deterministic paths mean a failed attempt leaves its files exactly where the next attempt will
    /// look. That is not solved by randomising the names - a random name is not reopenable - but by
    /// proving ownership: <see cref="PartOIteration3Artifacts"/> fingerprints every path here before the
    /// attempt starts, and a file that has not changed since is never reported as this attempt's. The
    /// previous Candidate B model is additionally <b>deleted</b> at attempt start, because a reopenable
    /// <c>.sam</c> left behind by a failed attempt is the one artifact a later session would act on.
    /// </para>
    ///
    /// <para><b>The suffix is fixed, and is not an optimisation round</b></para>
    /// <para>
    /// Candidate B is <c>&lt;project&gt;-It3B</c> and the bridge is <c>&lt;project&gt;-It3B-Bridge</c>.
    /// Deliberately not a <c>-Opt</c><i>nn</i> name: those belong to Iteration 2B's rounds, sort among
    /// them, and are parsed back by <c>PartOSimulationContext.Iteration_ProjectName</c>. An Iteration 3
    /// candidate is not a round of anything and must not be read as the latest and best of a sequence.
    /// </para>
    /// </summary>
    public class PartOIteration3Paths
    {
        /// <summary>What Candidate B's project name adds to Reference A's.</summary>
        public const string Suffix_CandidateB = "-It3B";

        /// <summary>What the thermostat bridge's copy adds to Candidate B's.</summary>
        public const string Suffix_Bridge = "-Bridge";

        /// <summary>What the pairing record adds to Reference A's results file name.</summary>
        public const string Suffix_Record = "-Iteration3";

        /// <summary>What the persisted A/B review report adds to Reference A's results file name.</summary>
        public const string Suffix_Report = "-Iteration3-Review";

        /// <summary>
        /// PR5B (SAM#111): what Candidate B's project name adds to Reference A's when it carries the selected
        /// product's cooling module (B4) - its own documents, so a B4 run never overwrites the B0 control's
        /// TPD, bridge or model and the two stay side by side for pairing.
        /// </summary>
        public const string Suffix_CandidateB_Cooling = "-It3B4";

        /// <summary>
        /// SAM#123: what Candidate B's project name adds to Reference A's when it runs the selected product to its
        /// manufacturer's guidance - its own documents, beside B0's and B4's.
        /// </summary>
        public const string Suffix_CandidateB_ManufacturerGuidance = "-It3BMG";

        /// <summary>
        /// What Candidate B's project name adds to Reference A's when it runs the selected products' certified
        /// efficiency and SFP - its own documents, so it no longer overwrites B0's and the two can be kept side by
        /// side. Records written before this existed name their own files, so they are unaffected.
        /// </summary>
        public const string Suffix_CandidateB_SelectedProduct = "-It3BP";

        /// <summary>
        /// Each behaviour mode's own record tag, so every method run against one Reference A keeps its own
        /// pairing: <c>&lt;run&gt;-Iteration3-B0.json</c>, <c>-BP</c>, <c>-B4</c>, <c>-MG</c>. The mode-independent
        /// <c>&lt;run&gt;-Iteration3.json</c> written before this is still read, by the mode recorded inside it -
        /// see <see cref="Query.PartOIteration3RecordPath"/>. Storage only: never shown to an engineer.
        /// </summary>
        public static string Tag(PartOIteration3BehaviourMode partOIteration3BehaviourMode)
        {
            return partOIteration3BehaviourMode switch
            {
                PartOIteration3BehaviourMode.SelectedProduct => "BP",
                PartOIteration3BehaviourMode.SelectedProductCooling => "B4",
                PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance => "MG",
                _ => "B0",
            };
        }

        /// <summary>PR5B: the hourly OperatingAirFlow history a B4 run persists beside its TPD.</summary>
        public const string Suffix_OperatingAirFlow = "-OperatingAirFlow";

        /// <summary>
        /// What every file an Iteration 3 run owns adds after Reference A's name when Reference A is an
        /// <b>Iteration 1a</b> run - Candidate B's TAS files, bridge, model, TM59 report, histories, record and review.
        /// <para>
        /// Iteration 3 accepts either MVHR reference, 1a or 2, and both are named from the same model, so without it a
        /// pairing against a model's 1a results and one against its Iteration 2 results would write the same
        /// <c>Iteration3</c> files. Iteration 2 - the ordinary reference - keeps the unqualified names. Read off
        /// Reference A's own results folder (<see cref="Qualifier_Reference"/>), so a review holding only those results
        /// derives the same names. A reference in a legacy flat folder is unqualified, as every pairing was before.
        /// </para>
        /// </summary>
        public const string Qualifier_ReferenceIteration1a = "-It1a";

        /// <summary>
        /// <see cref="Qualifier_ReferenceIteration1a"/> where Reference A's results are in a SAM-created
        /// <c>Iteration1a</c> case folder, and nothing otherwise.
        /// </summary>
        public static string Qualifier_Reference(string path_TSD_ReferenceA)
        {
            return PartOOutputPaths.FindForFile(path_TSD_ReferenceA)?.Case == PartOOutputCase.Iteration1a ? Qualifier_ReferenceIteration1a : string.Empty;
        }

        private PartOIteration3Paths(PartOOutputPaths partOOutputPaths, string projectName_ReferenceA, string path_TSD_ReferenceA, PartOIteration3BehaviourMode partOIteration3BehaviourMode = PartOIteration3BehaviourMode.Parity)
        {
            OutputPaths = partOOutputPaths;
            OutputDirectory = partOOutputPaths.Directory_Tas;
            ProjectName_ReferenceA = projectName_ReferenceA;
            Path_TSD_ReferenceA = path_TSD_ReferenceA;

            //Reference A's name, qualified where Reference A is Iteration 1a so its pairing and an Iteration 2 one
            //never share a file.
            ProjectName_CandidateB = string.Concat(
                projectName_ReferenceA,
                Qualifier_Reference(path_TSD_ReferenceA),
                partOIteration3BehaviourMode == PartOIteration3BehaviourMode.SelectedProductCooling ? Suffix_CandidateB_Cooling
                : partOIteration3BehaviourMode == PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance ? Suffix_CandidateB_ManufacturerGuidance
                : partOIteration3BehaviourMode == PartOIteration3BehaviourMode.SelectedProduct ? Suffix_CandidateB_SelectedProduct
                : Suffix_CandidateB);
            Path_OperatingAirFlow = Path.Combine(partOOutputPaths.Directory_Diagnostics, ProjectName_CandidateB + Suffix_OperatingAirFlow + ".csv");
            ProjectName_Bridge = string.Concat(ProjectName_CandidateB, Suffix_Bridge);

            //Every TAS file of Candidate B together, in Iteration 3's own tas folder.
            Path_TBD_ThermalSource = Path.Combine(OutputDirectory, ProjectName_CandidateB + ".tbd");
            Path_TSD_ThermalSource = Path.ChangeExtension(Path_TBD_ThermalSource, "tsd");
            Path_TPD = Path.Combine(OutputDirectory, ProjectName_CandidateB + ".tpd");
            Path_TBD_Bridge = Path.Combine(OutputDirectory, ProjectName_Bridge + ".tbd");
            Path_TSD_Bridge = Path.ChangeExtension(Path_TBD_Bridge, "tsd");

            //Derived from the results as every run's are: the model beside them in tas, the reports in reports.
            //Reference A's report is Reference A's own, wherever its results are.
            Path_Model_CandidateB = Query.Path_PartORunModel(Path_TSD_Bridge);
            Path_TM59Report_CandidateB = Query.Path_TM59Report(Path_TSD_Bridge);
            Path_TM59Report_ReferenceA = Query.Path_TM59Report(path_TSD_ReferenceA);

            BehaviourMode = partOIteration3BehaviourMode;

            //This mode's own record, in Iteration 3's reports folder. Records written beside Reference A's
            //results before that - per mode, or the mode-independent one - are read, never written.
            Path_Record = Path_Record_ForResults(path_TSD_ReferenceA, partOIteration3BehaviourMode);
        }

        /// <summary>The behaviour mode these paths are for.</summary>
        public PartOIteration3BehaviourMode BehaviourMode { get; }

        /// <summary>
        /// Iteration 3's folders beneath the Part O root Reference A's run was written under - see
        /// <see cref="PartOOutputPaths"/>. For a Reference A in a legacy flat folder, that folder is the root.
        /// </summary>
        public PartOOutputPaths OutputPaths { get; }

        /// <summary>
        /// Where Candidate B's TAS files are written: Iteration 3's <c>tas</c> folder. Reference A's own files are
        /// read where they are - in Iteration 2's (or 1a's) folder, or a legacy flat one - and never copied here.
        /// </summary>
        public string OutputDirectory { get; }

        public string ProjectName_ReferenceA { get; }

        public string ProjectName_CandidateB { get; }

        public string ProjectName_Bridge { get; }

        /// <summary>Reference A's results - the pairing is named from these.</summary>
        public string Path_TSD_ReferenceA { get; }

        public string Path_TBD_ThermalSource { get; }

        public string Path_TSD_ThermalSource { get; }

        public string Path_TPD { get; }

        public string Path_TBD_Bridge { get; }

        public string Path_TSD_Bridge { get; }

        /// <summary>Candidate B's reopenable model, beside its own results and named from them.</summary>
        public string Path_Model_CandidateB { get; }

        public string Path_TM59Report_ReferenceA { get; }

        public string Path_TM59Report_CandidateB { get; }

        /// <summary>The pairing record, beside Reference A's results.</summary>
        public string Path_Record { get; }

        /// <summary>PR5B: the hourly recirculation OperatingAirFlow history of a B4 run, beside its TPD. Written only in that mode.</summary>
        public string Path_OperatingAirFlow { get; }

        /// <summary>
        /// Every fixed path this pairing may write, for the attempt-start snapshot. Reference A's own TSD
        /// and its TM59 report are deliberately <b>not</b> here: A is an input, this run does not write it,
        /// and a path in the snapshot is a path something is expected to have produced.
        /// </summary>
        public string[] Paths_CandidateB =>
        [
            Path_TBD_ThermalSource,
            Path_TSD_ThermalSource,
            Path_TPD,
            Path_TBD_Bridge,
            Path_TSD_Bridge,
            Path_Model_CandidateB,
            Path_TM59Report_CandidateB,
            Path_Record,
            Path_OperatingAirFlow,
        ];

        /// <summary>
        /// The pairing's paths for one completed Part O run, or null where the run states no output
        /// directory, no project name or no results file to derive them from.
        /// </summary>
        public static PartOIteration3Paths Create(PartOSimulationContext partOSimulationContext, string path_TSD_ReferenceA)
        {
            return Create(partOSimulationContext, path_TSD_ReferenceA, PartOIteration3BehaviourMode.Parity);
        }

        /// <summary>
        /// PR5B: the same, for a stated behaviour mode - the cooling mode's Candidate B writes under
        /// <see cref="Suffix_CandidateB_Cooling"/>, every other mode under <see cref="Suffix_CandidateB"/>.
        /// </summary>
        public static PartOIteration3Paths Create(PartOSimulationContext partOSimulationContext, string path_TSD_ReferenceA, PartOIteration3BehaviourMode partOIteration3BehaviourMode)
        {
            if (partOSimulationContext is null
                || string.IsNullOrWhiteSpace(partOSimulationContext.OutputDirectory)
                || string.IsNullOrWhiteSpace(partOSimulationContext.ProjectName)
                || string.IsNullOrWhiteSpace(path_TSD_ReferenceA))
            {
                return null;
            }

            //Reference A's own Part O root: the root of the case folder its results are in, or its flat folder.
            PartOOutputPaths partOOutputPaths = PartOOutputPaths.Create(PartOOutputPaths.Root(partOSimulationContext.OutputDirectory), PartOOutputCase.Iteration3);
            if (partOOutputPaths is null)
            {
                return null;
            }

            return new PartOIteration3Paths(partOOutputPaths, partOSimulationContext.ProjectName, path_TSD_ReferenceA, partOIteration3BehaviourMode);
        }

        /// <summary>
        /// The LEGACY, mode-independent record path for a results file - <c>&lt;run&gt;-Iteration3.json</c>,
        /// which every pairing written before per-mode records used. Still read, by the mode recorded inside
        /// it; never written. See <see cref="Query.PartOIteration3RecordPath"/>.
        /// </summary>
        public static string Path_Record_ForResults(string path_TSD)
        {
            return Path_Record_ForResults(path_TSD, null);
        }

        /// <summary>
        /// One behaviour mode's own record path for a results file alone - what a <b>review</b> uses, which
        /// has only the reopened run's TSD and no simulation context at all - and where a run writes it:
        /// <c>&lt;root&gt;/Iteration3/reports/&lt;run&gt;[-It1a]-Iteration3-&lt;tag&gt;.json</c>, the root read off the
        /// results' own folder (<see cref="PartOOutputPaths.Root"/>) and the qualifier off their case
        /// (<see cref="Qualifier_Reference"/>). Null mode is the legacy mode-independent
        /// path beside the results. A per-mode record written beside the results before Iteration 3 had its own
        /// folder is <see cref="Path_Record_ForResults_Legacy"/>.
        /// </summary>
        public static string Path_Record_ForResults(string path_TSD, PartOIteration3BehaviourMode? partOIteration3BehaviourMode)
        {
            if (string.IsNullOrWhiteSpace(path_TSD))
            {
                return null;
            }

            if (!partOIteration3BehaviourMode.HasValue)
            {
                return Path_Record_ForResults_Legacy(path_TSD, null);
            }

            string fileName = Path.GetFileNameWithoutExtension(path_TSD);
            string directory = PartOOutputPaths.Create(PartOOutputPaths.Root(Path.GetDirectoryName(path_TSD)), PartOOutputCase.Iteration3)?.Directory_Reports;

            if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(fileName))
            {
                return null;
            }

            return Path.Combine(directory, fileName + Qualifier_Reference(path_TSD) + Suffix_Record + "-" + Tag(partOIteration3BehaviourMode.Value) + ".json");
        }

        /// <summary>
        /// Where a record was written BESIDE the results file, as every pairing before Iteration 3 had its own
        /// folder was: the per-mode <c>&lt;run&gt;-Iteration3-&lt;tag&gt;.json</c>, or with no mode the
        /// mode-independent <c>&lt;run&gt;-Iteration3.json</c>. Read, never written.
        /// </summary>
        public static string Path_Record_ForResults_Legacy(string path_TSD, PartOIteration3BehaviourMode? partOIteration3BehaviourMode)
        {
            if (string.IsNullOrWhiteSpace(path_TSD))
            {
                return null;
            }

            string directory = Path.GetDirectoryName(path_TSD);
            string fileName = Path.GetFileNameWithoutExtension(path_TSD);

            if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(fileName))
            {
                return null;
            }

            return partOIteration3BehaviourMode.HasValue
                ? Path.Combine(directory, fileName + Suffix_Record + "-" + Tag(partOIteration3BehaviourMode.Value) + ".json")
                : Path.Combine(directory, fileName + Suffix_Record + ".json");
        }

        /// <summary>
        /// Where this pairing's persisted A/B review report lives, derived from the pairing record's own
        /// path - so a run, a review, and a later session looking for the last successful report all
        /// arrive at the same file without holding anything but the record's name.
        /// </summary>
        /// <param name="path_Record">The pairing record's path.</param>
        /// <param name="extension">
        /// <c>"txt"</c> for the report an engineer reads, <c>"json"</c> for its structured sibling.
        /// </param>
        public static string Path_Report_ForRecord(string path_Record, string extension = "txt")
        {
            if (string.IsNullOrWhiteSpace(path_Record) || string.IsNullOrWhiteSpace(extension))
            {
                return null;
            }

            string directory = Path.GetDirectoryName(path_Record);
            string fileName = Path.GetFileNameWithoutExtension(path_Record);

            if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(fileName))
            {
                return null;
            }

            //The record is <run>-Iteration3.json, so the report is <run>-Iteration3-Review.txt; a per-mode
            //record <run>-Iteration3-MG.json reports to <run>-Iteration3-MG-Review.txt. Derived from the record
            //rather than re-derived from the TSD: one of them moving must move both.
            if (fileName.EndsWith(Suffix_Record, StringComparison.OrdinalIgnoreCase))
            {
                fileName = fileName.Substring(0, fileName.Length - Suffix_Record.Length);

                return Path.Combine(directory, fileName + Suffix_Report + "." + extension.TrimStart('.'));
            }

            foreach (PartOIteration3BehaviourMode partOIteration3BehaviourMode in Enum.GetValues(typeof(PartOIteration3BehaviourMode)))
            {
                if (fileName.EndsWith(Suffix_Record + "-" + Tag(partOIteration3BehaviourMode), StringComparison.OrdinalIgnoreCase))
                {
                    return Path.Combine(directory, fileName + "-Review." + extension.TrimStart('.'));
                }
            }

            return Path.Combine(directory, fileName + Suffix_Report + "." + extension.TrimStart('.'));
        }
    }
}
