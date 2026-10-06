// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Whether an Iteration 3 attempt about to start may reuse an earlier attempt's TAS work - see
    /// <see cref="Query.PartOIteration3ResumePlan"/>, the only thing that makes one.
    /// </summary>
    internal sealed class PartOIteration3ResumePlan
    {
        internal PartOIteration3ResumePlan(PartOIteration3ResumeIdentity? partOIteration3ResumeIdentity, PartOIteration3Checkpoint? partOIteration3Checkpoint, string? reason)
        {
            Identity = partOIteration3ResumeIdentity;
            Checkpoint = partOIteration3Checkpoint;
            Reason = reason;
        }

        /// <summary>What this attempt is built from - kept with its own TAS work where it gets that far.</summary>
        internal PartOIteration3ResumeIdentity? Identity { get; }

        /// <summary>The earlier attempt's TAS work, proven reusable - or null, and TAS runs.</summary>
        internal PartOIteration3Checkpoint? Checkpoint { get; }

        internal bool Reuse => Checkpoint is not null;

        /// <summary>
        /// Why TAS work that an earlier attempt completed is NOT being reused, where there was some - said to the
        /// engineer, never silent. Null where nothing was reusable to begin with, and where it is reused.
        /// </summary>
        internal string? Reason { get; }
    }

    public static partial class Query
    {
        /// <summary>
        /// <b>The resume decision for one Iteration 3 attempt</b>: reuse the earlier attempt's TAS work, or run TAS.
        ///
        /// <para><b>SAFE - reused</b> only where all of these hold:</para>
        /// <list type="number">
        /// <item>This session's run holds TAS work for this method (<see cref="PartORun.Iteration3Checkpoint"/>). The
        /// run drops it on every model change, re-preparation, restore, reset, and whenever its own results file is
        /// rewritten or goes - so it cannot outlive what it was built from.</item>
        /// <item>What this attempt would be built from equals what that one was
        /// (<see cref="PartOIteration3ResumeIdentity.Differences"/>): the method, Reference A's results and their
        /// provenance, the TAS case, the prepared design, the dwelling scope, the prepared systems and, for a product
        /// method, the catalogue.</item>
        /// <item>Every piece of that TAS work is still complete, and every TAS file it wrote is present with the exact
        /// length and write time recorded when it was claimed.</item>
        /// </list>
        /// <para>
        /// <b>UNSAFE or UNKNOWN - TAS runs.</b> Anything else, including any value missing on either side. The reason
        /// is returned in words so the attempt can say it. There is no partial reuse: the TAS stages feed each other,
        /// so a thermal source is never paired with another attempt's TAS Systems or bridge results.
        /// </para>
        /// <para>
        /// <b>Never from disk alone.</b> A record left by an attempt in an earlier session is not reusable, however
        /// complete its files look - see <see cref="PartOIteration3Checkpoint"/> for what cannot be proven from it. It
        /// is only explained.
        /// </para>
        /// <para>
        /// <b>Cost.</b> One fingerprint of the prepared design (the serialisation the run's provenance already uses),
        /// the catalogue read a product method makes anyway, a stat of each TAS file and, with no kept work, one small
        /// JSON read. Nothing walks a result series.
        /// </para>
        /// </summary>
        internal static PartOIteration3ResumePlan PartOIteration3ResumePlan(PartORun? partORun, PartOIteration3BehaviourMode partOIteration3BehaviourMode)
        {
            if (partORun is null || !partORun.CanAssess)
            {
                return new PartOIteration3ResumePlan(null, null, null);
            }

            string? sha256 = partOIteration3BehaviourMode == PartOIteration3BehaviourMode.Parity ? null : VentilationUnitCatalogue.Read().Sha256;

            PartOIteration3ResumeIdentity partOIteration3ResumeIdentity = PartOIteration3ResumeIdentity(partORun, partOIteration3BehaviourMode, sha256);

            if (partORun.Iteration3Checkpoint(partOIteration3BehaviourMode) is not PartOIteration3Checkpoint partOIteration3Checkpoint)
            {
                return new PartOIteration3ResumePlan(partOIteration3ResumeIdentity, null, PartOIteration3ResumeReason_NotKept(partORun, partOIteration3BehaviourMode));
            }

            List<string> reasons = partOIteration3ResumeIdentity.Differences(partOIteration3Checkpoint.Identity);

            //Files first: a missing or rewritten one is named, where the completeness check below could only say which
            //result it broke.
            if (reasons.Count == 0 && !partOIteration3Checkpoint.IsCurrent(out string? refusal))
            {
                reasons.Add(refusal!);
            }

            if (reasons.Count == 0 && !partOIteration3Checkpoint.IsComplete(out refusal))
            {
                reasons.Add(refusal!);
            }

            if (reasons.Count != 0)
            {
                return new PartOIteration3ResumePlan(
                    partOIteration3ResumeIdentity,
                    null,
                    string.Format(
                        "The TAS results of the attempt of {0} were not reused, because {1}. TAS is run again.",
                        partOIteration3Checkpoint.When,
                        string.Join("; ", reasons.ConvertAll(x => x.Length > 1 && char.IsUpper(x[0]) && char.IsLower(x[1]) ? char.ToLowerInvariant(x[0]) + x.Substring(1) : x)).TrimEnd('.')));
            }

            return new PartOIteration3ResumePlan(partOIteration3ResumeIdentity, partOIteration3Checkpoint, null);
        }

        /// <summary>What a completed run's Iteration 3 attempt for one method is built from, right now.</summary>
        /// <param name="sha256_VentilationUnitCatalogue">The catalogue a product method will resolve against; null for Parity.</param>
        internal static PartOIteration3ResumeIdentity PartOIteration3ResumeIdentity(PartORun partORun, PartOIteration3BehaviourMode partOIteration3BehaviourMode, string? sha256_VentilationUnitCatalogue)
        {
            SimulationResultProvenance? simulationResultProvenance = null;
            partORun.AnalyticalModel_Assessment?.TryGetValue(Analytical.AnalyticalModelParameter.SimulationResultProvenance, out simulationResultProvenance);

            //The reference results as they are on disk NOW - so a file rewritten since would differ from the one the
            //earlier attempt was built on even where nothing else had yet noticed.
            long length_TSD = -1;
            long ticks_TSD = -1;
            bool exists_TSD = simulationResultProvenance?.IsComplete == true && PartOIteration3Artifacts.TryRead(partORun.Path_TSD, out length_TSD, out ticks_TSD);

            AnalyticalModel? analyticalModel_Prepared = partORun.AnalyticalModel_Prepared;

            List<Guid> guids_Zone = [];
            foreach (Zone zone in partORun.PreparationContext?.Zones ?? [])
            {
                if (zone is not null)
                {
                    guids_Zone.Add(zone.Guid);
                }
            }

            return new PartOIteration3ResumeIdentity()
            {
                BehaviourMode = partOIteration3BehaviourMode,
                Path_TSD_ReferenceA = partORun.Path_TSD,
                Length_TSD_ReferenceA = exists_TSD ? length_TSD : -1,
                Timestamp_TSD_ReferenceA = exists_TSD ? ticks_TSD : -1,
                Fingerprint_Model_ReferenceA = simulationResultProvenance?.IsComplete == true ? simulationResultProvenance.Fingerprint_Model : null,
                Fingerprint_Scenarios_ReferenceA = simulationResultProvenance?.IsComplete == true ? simulationResultProvenance.Fingerprint_OverheatingScenarios : null,
                Fingerprint_Scenario = PartOIteration3ScenarioFingerprint(partORun.SimulationContext),
                Fingerprint_PreparedModel = analyticalModel_Prepared is null ? null : SimulationResultProvenance.Fingerprint(analyticalModel_Prepared),
                Guids_Zone = Analytical.UI.WPF.PartOIteration3ResumeIdentity.Join(guids_Zone),
                Guids_VentilationSystem = Analytical.UI.WPF.PartOIteration3ResumeIdentity.Join(partORun.Guids_VentilationSystem_Prepared),
                Sha256_VentilationUnitCatalogue = partOIteration3BehaviourMode == PartOIteration3BehaviourMode.Parity ? null : sha256_VentilationUnitCatalogue,
            };
        }

        /// <summary>
        /// Which air handling units an attempt's equipment resolution produced unit, cooling and guidance settings for,
        /// as one comparable string - so a resumed attempt can show its own resolution is the one the reused TAS work
        /// was materialised from.
        /// </summary>
        internal static string PartOIteration3ResumeSettings(IEnumerable<Guid>? guids_UnitSettings, IEnumerable<Guid>? guids_CoolingSettings, IEnumerable<Guid>? guids_GuidanceSettings)
        {
            return string.Format(
                "unit={0} | cooling={1} | guidance={2}",
                Analytical.UI.WPF.PartOIteration3ResumeIdentity.Join(guids_UnitSettings),
                Analytical.UI.WPF.PartOIteration3ResumeIdentity.Join(guids_CoolingSettings),
                Analytical.UI.WPF.PartOIteration3ResumeIdentity.Join(guids_GuidanceSettings));
        }

        /// <summary>
        /// Where there is no kept TAS work but this method's saved record shows an attempt that got past its TAS work
        /// and then stopped - the one case a person would expect a resume and will not get one - why not. Null
        /// otherwise.
        /// </summary>
        private static string? PartOIteration3ResumeReason_NotKept(PartORun partORun, PartOIteration3BehaviourMode partOIteration3BehaviourMode)
        {
            string? path_Record = PartOIteration3Paths.Create(partORun.SimulationContext, partORun.Path_TSD, partOIteration3BehaviourMode)?.Path_Record;

            if (string.IsNullOrWhiteSpace(path_Record) || !System.IO.File.Exists(path_Record))
            {
                return null;
            }

            PartOIteration3Record? partOIteration3Record = PartOIteration3PairingRecord(path_Record);

            if (partOIteration3Record is null || partOIteration3Record.IsComplete || !partOIteration3Record.Stages.Exists(x => x.Stage == PartOIteration3Stage.ResultantTemperature && x.IsCompleted))
            {
                return null;
            }

            PartOIteration3StageState? partOIteration3StageState_Refused = partOIteration3Record.Stages.Find(x => x.Status == PartOIteration3StageStatus.Refused);

            return string.Format(
                "The attempt of {0} completed its TAS work and stopped at {1}, but its TAS results are not reused: they can be proven only in the SAM session that produced them, because the system case model and the TAS Systems bindings are kept in memory until a pairing completes. TAS is run again.",
                new DateTime(partOIteration3Record.Ticks_Utc, DateTimeKind.Utc).ToLocalTime().ToString("d MMM yyyy HH:mm", System.Globalization.CultureInfo.CurrentCulture),
                partOIteration3StageState_Refused is null ? "a later stage" : Core.Query.Description(partOIteration3StageState_Refused.Stage).ToLowerInvariant());
        }
    }
}
