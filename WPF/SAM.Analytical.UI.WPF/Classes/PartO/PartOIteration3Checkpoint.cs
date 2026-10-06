// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Systems;
using SAM.Analytical.Tas.TPD;
using SAM.Core.Tas;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// What an Iteration 3 pairing's TAS work was built from - every input the thermal source, the TAS Systems route
    /// and the resultant temperature depend on, as values that are cheap to compare.
    ///
    /// <para><b>Nothing new is invented here</b></para>
    /// <para>
    /// Every value is one an existing authority already states and already relies on: Reference A's own
    /// <see cref="SimulationResultProvenance"/> (its results file, their length and write time, the design state and
    /// the overheating scenarios they were produced under), the TAS case string the pairing record already carries
    /// (<see cref="Query.PartOIteration3ScenarioFingerprint"/>), the prepared design's fingerprint by the same
    /// <see cref="SimulationResultProvenance.Fingerprint(AnalyticalModel)"/> the saved-preparation sidecar uses, the
    /// dwelling scope and the ventilation systems the preparation built, and - for a product method - the
    /// catalogue's SHA-256 the record already carries. Two identities are the same pairing input only where every one
    /// of those is equal; any one that differs is named.
    /// </para>
    /// </summary>
    internal sealed class PartOIteration3ResumeIdentity
    {
        internal PartOIteration3BehaviourMode BehaviourMode { get; init; }

        internal string? Path_TSD_ReferenceA { get; init; }

        internal long Length_TSD_ReferenceA { get; init; } = -1;

        internal long Timestamp_TSD_ReferenceA { get; init; } = -1;

        internal string? Fingerprint_Model_ReferenceA { get; init; }

        internal string? Fingerprint_Scenarios_ReferenceA { get; init; }

        internal string? Fingerprint_Scenario { get; init; }

        internal string? Fingerprint_PreparedModel { get; init; }

        internal string? Guids_Zone { get; init; }

        internal string? Guids_VentilationSystem { get; init; }

        /// <summary>The catalogue a product method resolves against; null for Parity, which reads none.</summary>
        internal string? Sha256_VentilationUnitCatalogue { get; init; }

        /// <summary>
        /// Whether every value a reuse depends on is stated. An identity with a hole in it proves nothing, so it is
        /// never matched - not even by another identity with the same hole.
        /// </summary>
        internal bool IsComplete(out string? refusal)
        {
            refusal = null;

            List<string> missing = [];

            if (string.IsNullOrWhiteSpace(Path_TSD_ReferenceA) || Length_TSD_ReferenceA < 0 || Timestamp_TSD_ReferenceA < 0)
            {
                missing.Add("Reference A's results file");
            }

            if (string.IsNullOrWhiteSpace(Fingerprint_Model_ReferenceA) || string.IsNullOrWhiteSpace(Fingerprint_Scenarios_ReferenceA))
            {
                missing.Add("Reference A's design and scenario fingerprints");
            }

            if (string.IsNullOrWhiteSpace(Fingerprint_Scenario))
            {
                missing.Add("the TAS case");
            }

            if (string.IsNullOrWhiteSpace(Fingerprint_PreparedModel))
            {
                missing.Add("the prepared design");
            }

            if (BehaviourMode != PartOIteration3BehaviourMode.Parity && string.IsNullOrWhiteSpace(Sha256_VentilationUnitCatalogue))
            {
                missing.Add("the ventilation unit catalogue");
            }

            if (missing.Count != 0)
            {
                refusal = string.Format("This run does not state {0}, so it cannot be shown that earlier TAS results belong to it.", string.Join(", ", missing));

                return false;
            }

            return true;
        }

        /// <summary>
        /// Every way <paramref name="partOIteration3ResumeIdentity"/> - what an earlier attempt was built from - differs
        /// from this one, in words. Empty only where both are complete and equal.
        /// </summary>
        internal List<string> Differences(PartOIteration3ResumeIdentity? partOIteration3ResumeIdentity)
        {
            List<string> result = [];

            if (partOIteration3ResumeIdentity is null)
            {
                result.Add("the earlier attempt states nothing it was built from");

                return result;
            }

            if (!IsComplete(out string? refusal) || !partOIteration3ResumeIdentity.IsComplete(out refusal))
            {
                result.Add(refusal!);

                return result;
            }

            void Compare(string? value, string? value_Earlier, string what)
            {
                if (!string.Equals(value, value_Earlier, StringComparison.Ordinal))
                {
                    result.Add(what);
                }
            }

            if (BehaviourMode != partOIteration3ResumeIdentity.BehaviourMode)
            {
                result.Add(string.Format("the method is '{0}', not '{1}'", Query.PartOIteration3MethodLabel(BehaviourMode), Query.PartOIteration3MethodLabel(partOIteration3ResumeIdentity.BehaviourMode)));
            }

            if (!string.Equals(Path_TSD_ReferenceA, partOIteration3ResumeIdentity.Path_TSD_ReferenceA, StringComparison.OrdinalIgnoreCase)
                || Length_TSD_ReferenceA != partOIteration3ResumeIdentity.Length_TSD_ReferenceA
                || Timestamp_TSD_ReferenceA != partOIteration3ResumeIdentity.Timestamp_TSD_ReferenceA)
            {
                result.Add("the reference case's results are not the same file");
            }

            Compare(Fingerprint_Model_ReferenceA, partOIteration3ResumeIdentity.Fingerprint_Model_ReferenceA, "the reference case's design state has changed");
            Compare(Fingerprint_Scenarios_ReferenceA, partOIteration3ResumeIdentity.Fingerprint_Scenarios_ReferenceA, "the overheating scenarios have changed");
            Compare(Fingerprint_Scenario, partOIteration3ResumeIdentity.Fingerprint_Scenario, "the TAS case (weather, solar method, day range or workflow options) has changed");
            Compare(Fingerprint_PreparedModel, partOIteration3ResumeIdentity.Fingerprint_PreparedModel, "the prepared design has changed");
            Compare(Guids_Zone, partOIteration3ResumeIdentity.Guids_Zone, "the dwelling scope has changed");
            Compare(Guids_VentilationSystem, partOIteration3ResumeIdentity.Guids_VentilationSystem, "the ventilation systems the preparation built have changed");
            Compare(Sha256_VentilationUnitCatalogue, partOIteration3ResumeIdentity.Sha256_VentilationUnitCatalogue, "the ventilation unit catalogue has changed");

            return result;
        }

        /// <summary>Sorted, so the same set in another order is the same set.</summary>
        internal static string Join(IEnumerable<Guid>? guids)
        {
            List<Guid> list = [.. guids ?? []];
            list.Sort();

            return string.Join(",", list.ConvertAll(x => x.ToString("N", CultureInfo.InvariantCulture)));
        }
    }

    /// <summary>
    /// <b>One Iteration 3 attempt's completed TAS work</b>, kept in this session so a retry after a later stage failed
    /// does not run TAS again.
    ///
    /// <para><b>What it holds, and why each</b></para>
    /// <para>
    /// Exactly what the stages after the resultant temperature read and that nothing on disk can give back: the
    /// SAM_Systems materialisation the TAS Systems document was converted from, the no-IZAM thermal source, the model
    /// the no-IZAM workflow returned (the only one carrying Candidate B's TAS zone identities), the TAS Systems route
    /// with its room and connection bindings, and the provider's resultant-temperature series - the series TM59's own
    /// reading of the bridge results is checked against. Plus <see cref="Identity"/>, what they were built from, and
    /// <see cref="Files"/>, every TAS file the attempt proved it wrote, with the length and write time it recorded.
    /// </para>
    ///
    /// <para><b>Why it is in memory and nowhere else</b></para>
    /// <para>
    /// The model, the bindings and the provider's series are persisted only when a pairing completes - Candidate B's
    /// model at the Persistence stage, the bindings as part of the completed record - so a failed attempt leaves
    /// nothing on disk a later session could prove them from. Writing them earlier would be a new persistence
    /// format; this is the narrow, session-scoped alternative, held by <see cref="PartORun"/> so any change that drops
    /// the run drops it too.
    /// </para>
    /// </summary>
    internal sealed class PartOIteration3Checkpoint
    {
        internal PartOIteration3Checkpoint(
            Guid guid_Run,
            DateTime dateTime_Utc,
            PartOIteration3ResumeIdentity partOIteration3ResumeIdentity,
            string sha256_VentilationUnitCatalogue_Resolved,
            string settings,
            MechanicalVentilationMaterialisation mechanicalVentilationMaterialisation,
            NoIzamThermalSource noIzamThermalSource,
            AnalyticalModel analyticalModel_CandidateB,
            IEnumerable<string>? notes_ThermalSource,
            SystemVentilationRoute systemVentilationRoute,
            ResultantTemperatureResults resultantTemperatureResults,
            IEnumerable<PartOIteration3FileRecord> files)
        {
            Guid_Run = guid_Run;
            DateTime_Utc = dateTime_Utc;
            Identity = partOIteration3ResumeIdentity;
            Sha256_VentilationUnitCatalogue_Resolved = sha256_VentilationUnitCatalogue_Resolved;
            Settings = settings;
            Materialisation = mechanicalVentilationMaterialisation;
            ThermalSource = noIzamThermalSource;
            AnalyticalModel_CandidateB = analyticalModel_CandidateB;
            Notes_ThermalSource = [.. notes_ThermalSource ?? []];
            Route = systemVentilationRoute;
            ResultantTemperatures = resultantTemperatureResults;
            Files = [.. files ?? []];
        }

        /// <summary>The attempt that produced the TAS work.</summary>
        internal Guid Guid_Run { get; }

        internal DateTime DateTime_Utc { get; }

        internal PartOIteration3ResumeIdentity Identity { get; }

        /// <summary>The catalogue that attempt's equipment resolution actually read (null for Parity).</summary>
        internal string Sha256_VentilationUnitCatalogue_Resolved { get; }

        /// <summary>Which air handling units that attempt resolved unit, cooling and guidance settings for - see <see cref="Query.PartOIteration3ResumeSettings"/>.</summary>
        internal string Settings { get; }

        internal MechanicalVentilationMaterialisation Materialisation { get; }

        internal NoIzamThermalSource ThermalSource { get; }

        internal AnalyticalModel AnalyticalModel_CandidateB { get; }

        internal List<string> Notes_ThermalSource { get; }

        internal SystemVentilationRoute Route { get; }

        internal ResultantTemperatureResults ResultantTemperatures { get; }

        internal List<PartOIteration3FileRecord> Files { get; }

        /// <summary>When the TAS work was done, in the engineer's local time - for the words a person reads.</summary>
        internal string When => DateTime_Utc.ToLocalTime().ToString("d MMM yyyy HH:mm", CultureInfo.CurrentCulture);

        /// <summary>
        /// Whether every piece of TAS work this holds is still the complete result it was when it was kept - the same
        /// completeness rules the attempt itself applied before it moved on. Cheap: no series is walked beyond what
        /// the result types' own checks do.
        /// </summary>
        internal bool IsComplete(out string? refusal)
        {
            refusal = Materialisation is null || !Materialisation.IsMaterialised ? "the ventilation systems"
                : ThermalSource is null || !ThermalSource.IsComplete ? "the thermal source"
                : AnalyticalModel_CandidateB is null ? "the system case model"
                : Route is null || !Route.IsComplete ? "the TAS Systems results"
                : ResultantTemperatures is null || !ResultantTemperatures.IsComplete ? "the resultant temperatures"
                : Files.Count == 0 ? "the list of files it wrote"
                : null;

            if (refusal is null)
            {
                return true;
            }

            refusal = string.Format("the earlier attempt's kept result for {0} is no longer complete", refusal);

            return false;
        }

        /// <summary>
        /// Whether every TAS file the attempt wrote is still exactly the file it recorded - present, same length,
        /// same write time. The first one that is not is named. Missing one is never made up for by the others.
        /// </summary>
        internal bool IsCurrent(out string? refusal)
        {
            foreach (PartOIteration3FileRecord partOIteration3FileRecord in Files)
            {
                if (!partOIteration3FileRecord.Current(out refusal))
                {
                    return false;
                }
            }

            refusal = null;

            return true;
        }

        /// <summary>
        /// Stands in for claiming a file this attempt wrote: the file is one the reused attempt wrote and recorded, and
        /// is unchanged since. Anything else refuses, by name.
        /// </summary>
        internal bool TryVerify(string path, out string? artifact, out string? refusal)
        {
            artifact = null;

            PartOIteration3FileRecord? partOIteration3FileRecord = Files.Find(x => string.Equals(x.Path, path, StringComparison.OrdinalIgnoreCase));

            if (partOIteration3FileRecord is null)
            {
                refusal = string.Format("'{0}' was not written by the attempt whose TAS results are being reused, so it is not evidence of them.", path);

                return false;
            }

            if (!partOIteration3FileRecord.Current(out refusal))
            {
                return false;
            }

            artifact = string.Format("{0} (reused from the attempt of {1}, unchanged)", path, When);

            return true;
        }

        internal bool TryVerify(IEnumerable<string> paths, out List<string> artifacts, out List<string> refusals)
        {
            artifacts = [];
            refusals = [];

            foreach (string path in paths)
            {
                if (TryVerify(path, out string? artifact, out string? refusal))
                {
                    artifacts.Add(artifact!);
                }
                else
                {
                    refusals.Add(refusal!);
                }
            }

            return refusals.Count == 0;
        }

        /// <summary>The ledger detail of a stage whose work this attempt reused rather than ran.</summary>
        internal string Reused(string detail)
        {
            return string.Format("Reused from the attempt of {0}; TAS was not run again and its files are unchanged. {1}", When, detail);
        }
    }
}
