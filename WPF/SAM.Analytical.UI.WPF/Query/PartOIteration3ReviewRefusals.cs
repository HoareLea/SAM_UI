// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.IO;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Query
    {
        /// <summary>
        /// Everything that has to still be true before a persisted Iteration 3 pairing may be shown, and
        /// the Candidate B model where it is.
        ///
        /// <para><b>Validated by identity and by file fingerprint, never by name alone</b></para>
        /// <para>
        /// A pairing is a statement about two specific results files produced from one specific design
        /// state. Any of the three moving makes the statement false, and each of them can move between
        /// sessions: a design is edited, a simulation is rerun, a file is replaced by hand. So this
        /// re-asks all three - the run being reviewed is the Reference A the record names, Reference A's
        /// design and scenarios still match the fingerprints the record copied from its own provenance,
        /// and every Candidate B file is still byte-length and write-time identical to what was recorded.
        /// The two TM59 reports are the exception, and deliberately: every assessment of the results
        /// rewrites them, a review's included, so they are lineage rather than comparison authority - see
        /// <see cref="PartOIteration3Roles.IsRegeneratedByAssessment"/>.
        /// </para>
        /// <para>
        /// <b>Refused by name.</b> Each failure says which file or which fingerprint, because "this
        /// pairing is stale" without saying what moved is not something a person can act on.
        /// </para>
        /// <para>
        /// <b>Nothing here opens a TAS file.</b> The only file actually read is Candidate B's own
        /// <c>.sam</c>, through SAM's ordinary model reader.
        /// </para>
        /// </summary>
        /// <param name="partORun">The run being reviewed - Reference A.</param>
        /// <param name="partOIteration3Record">The record read off disk.</param>
        /// <param name="analyticalModel_CandidateB">Candidate B's reopened model, where everything passed.</param>
        /// <param name="path_TSD_CandidateB">The bridge results Candidate B's temperatures come from.</param>
        internal static List<string> PartOIteration3ReviewRefusals(
            PartORun partORun,
            PartOIteration3Record partOIteration3Record,
            out AnalyticalModel analyticalModel_CandidateB,
            out string path_TSD_CandidateB)
        {
            List<string> result = [];

            analyticalModel_CandidateB = null;
            path_TSD_CandidateB = null;

            if (partORun is null || partOIteration3Record is null)
            {
                result.Add("There is no Part O run or no Iteration 3 pairing record, so nothing could be validated.");

                return result;
            }

            if (!PartOIteration3Record.IsReadableSchema(partOIteration3Record.Schema))
            {
                result.Add(string.Format(
                    "The Iteration 3 pairing record states schema '{0}' and this build reads only '{1}' or '{2}', so it cannot be read as one.",
                    partOIteration3Record.Schema ?? "<none>",
                    PartOIteration3Record.CurrentSchema,
                    PartOIteration3Record.LegacySchema_V1));

                return result;
            }

            //A pre-PR5A (v1) pairing can only ever be the foundation control: selected-product behaviour
            //did not exist when it was written. One that says otherwise is corrupt, not historical.
            if (partOIteration3Record.IsLegacy_V1
                && (partOIteration3Record.BehaviourMode != PartOIteration3BehaviourMode.Parity
                    || partOIteration3Record.Equipment.Count != 0
                    || !string.IsNullOrWhiteSpace(partOIteration3Record.Directory_VentilationUnitCatalogue)
                    || !string.IsNullOrWhiteSpace(partOIteration3Record.Path_VentilationUnitCatalogue)
                    || !string.IsNullOrWhiteSpace(partOIteration3Record.Schema_VentilationUnitCatalogue)
                    || !string.IsNullOrWhiteSpace(partOIteration3Record.Sha256_VentilationUnitCatalogue)))
            {
                result.Add(string.Format(
                    "This Iteration 3 pairing record states the pre-PR5A schema '{0}', which only ever described the Parity foundation control, but it carries selected-product behaviour or catalogue evidence, so the record contradicts itself.",
                    PartOIteration3Record.LegacySchema_V1));

                return result;
            }

            if (!Enum.IsDefined(typeof(PartOIteration3BehaviourMode), partOIteration3Record.BehaviourMode))
            {
                result.Add("The Iteration 3 pairing record does not name a supported ventilation equipment behaviour, so it cannot be interpreted safely.");

                return result;
            }

            //PR5B: a cooling module belongs only to the cooling mode, and the cooling mode's ventilation is the
            //foundation control's - it resolves no selected-product fan or heat-recovery behaviour.
            if (partOIteration3Record.BehaviourMode != PartOIteration3BehaviourMode.SelectedProductCooling && partOIteration3Record.Cooling.Count != 0)
            {
                result.Add("This pairing records a cooling module but does not call itself the selected-product cooling mode, so the record contradicts itself.");

                return result;
            }

            if (partOIteration3Record.BehaviourMode == PartOIteration3BehaviourMode.Parity)
            {
                if (partOIteration3Record.Equipment.Count != 0)
                {
                    result.Add("This pairing calls itself the Parity foundation control but records selected-product equipment behaviour, so the record contradicts itself.");

                    return result;
                }
            }
            else if (partOIteration3Record.BehaviourMode == PartOIteration3BehaviourMode.SelectedProductCooling)
            {
                if (partOIteration3Record.Equipment.Count != 0)
                {
                    result.Add("This pairing calls itself B0 plus the selected product's cooling module but records selected-product fan or heat-recovery behaviour, so the record contradicts itself.");

                    return result;
                }

                if (partOIteration3Record.IsComplete)
                {
                    if (string.IsNullOrWhiteSpace(partOIteration3Record.Directory_VentilationUnitCatalogue)
                        || string.IsNullOrWhiteSpace(partOIteration3Record.Path_VentilationUnitCatalogue)
                        || string.IsNullOrWhiteSpace(partOIteration3Record.Schema_VentilationUnitCatalogue)
                        || string.IsNullOrWhiteSpace(partOIteration3Record.Sha256_VentilationUnitCatalogue))
                    {
                        result.Add("This completed cooling pairing does not record the catalogue directory, file, schema and SHA-256 it resolved, so its cooling data has no complete provenance.");
                    }

                    List<PartOIteration3CoolingEvidence> cooling = partOIteration3Record.Cooling;
                    if (cooling.Count == 0 || cooling.Count != partOIteration3Record.Count_AirSystem)
                    {
                        result.Add(string.Format(
                            "This completed cooling pairing records {0} cooling row(s) for {1} air system(s), so the cooling module is not accounted for one system at a time.",
                            cooling.Count,
                            partOIteration3Record.Count_AirSystem));
                    }

                    HashSet<Guid> guids_AirHandlingUnit_Cooling = [];
                    HashSet<Guid> guids_AirSystem_Cooling = [];

                    foreach (PartOIteration3CoolingEvidence partOIteration3CoolingEvidence in cooling)
                    {
                        if (!partOIteration3CoolingEvidence.IsComplete)
                        {
                            result.Add(string.Format(
                                "The cooling row for air handling unit {0} is incomplete or records refused behaviour (heating, cooling below its gate, or recirculation outside its law), so it cannot be audited as a valid cooling module.",
                                partOIteration3CoolingEvidence.Guid_AirHandlingUnit));
                        }

                        if (!guids_AirHandlingUnit_Cooling.Add(partOIteration3CoolingEvidence.Guid_AirHandlingUnit)
                            || !guids_AirSystem_Cooling.Add(partOIteration3CoolingEvidence.Guid_AirSystem))
                        {
                            result.Add("The cooling evidence repeats an air handling unit or an air system, so it is not one row per physical unit.");
                        }
                    }

                    if (result.Count != 0)
                    {
                        return result;
                    }
                }
            }
            else if (partOIteration3Record.BehaviourMode == PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance)
            {
                //SAM#123: manufacturer guidance resolves no certified fan or heat-recovery figure and no B4 module.
                if (partOIteration3Record.Equipment.Count != 0)
                {
                    result.Add("This pairing calls itself the selected product to manufacturer guidance but records certified fan or heat-recovery behaviour, so the record contradicts itself.");

                    return result;
                }

                if (partOIteration3Record.IsComplete
                    && (string.IsNullOrWhiteSpace(partOIteration3Record.Directory_VentilationUnitCatalogue)
                        || string.IsNullOrWhiteSpace(partOIteration3Record.Path_VentilationUnitCatalogue)
                        || string.IsNullOrWhiteSpace(partOIteration3Record.Schema_VentilationUnitCatalogue)
                        || string.IsNullOrWhiteSpace(partOIteration3Record.Sha256_VentilationUnitCatalogue)))
                {
                    result.Add("This completed manufacturer-guidance pairing does not record the catalogue directory, file, schema and SHA-256 it resolved, so its guidance has no complete provenance.");

                    return result;
                }
            }
            else if (partOIteration3Record.IsComplete)
            {
                if (string.IsNullOrWhiteSpace(partOIteration3Record.Directory_VentilationUnitCatalogue)
                    || string.IsNullOrWhiteSpace(partOIteration3Record.Path_VentilationUnitCatalogue)
                    || string.IsNullOrWhiteSpace(partOIteration3Record.Schema_VentilationUnitCatalogue)
                    || string.IsNullOrWhiteSpace(partOIteration3Record.Sha256_VentilationUnitCatalogue))
                {
                    result.Add("This completed Selected-product pairing does not record the catalogue directory, file, schema and SHA-256 it resolved, so its manufacturer data has no complete provenance.");
                }

                List<PartOIteration3EquipmentEvidence> equipment = partOIteration3Record.Equipment;
                if (equipment.Count == 0 || equipment.Count != partOIteration3Record.Count_AirSystem)
                {
                    result.Add(string.Format(
                        "This completed Selected-product pairing records {0} equipment row(s) for {1} air system(s), so the selected behaviour is not accounted for one system at a time.",
                        equipment.Count,
                        partOIteration3Record.Count_AirSystem));
                }

                HashSet<Guid> guids_AirHandlingUnit = [];
                HashSet<Guid> guids_AirSystem = [];

                foreach (PartOIteration3EquipmentEvidence partOIteration3EquipmentEvidence in equipment)
                {
                    if (!partOIteration3EquipmentEvidence.IsComplete)
                    {
                        result.Add(string.Format(
                            "The selected-product equipment row for air handling unit {0} is incomplete, so its identity, values, assumptions and materialised lineage cannot all be audited.",
                            partOIteration3EquipmentEvidence.Guid_AirHandlingUnit));
                    }

                    if (!guids_AirHandlingUnit.Add(partOIteration3EquipmentEvidence.Guid_AirHandlingUnit)
                        || !guids_AirSystem.Add(partOIteration3EquipmentEvidence.Guid_AirSystem))
                    {
                        result.Add("The selected-product equipment evidence repeats an air handling unit or an air system, so it is not one row per physical unit.");
                    }
                }

                if (result.Count != 0)
                {
                    return result;
                }
            }

            //-------------------------------------------------------------------------------------------
            //Reference A
            //-------------------------------------------------------------------------------------------
            string path_TSD_ReferenceA = partORun.Path_TSD;

            if (!SamePlace(partOIteration3Record.Path_TSD_ReferenceA, path_TSD_ReferenceA))
            {
                result.Add(string.Format(
                    "This Iteration 3 pairing was recorded against the results at '{0}', and the run being reviewed produced '{1}'.",
                    partOIteration3Record.Path_TSD_ReferenceA ?? "<none>",
                    path_TSD_ReferenceA ?? "<none>"));
            }

            AnalyticalModel analyticalModel_ReferenceA = partORun.AnalyticalModel_Assessment;

            if (analyticalModel_ReferenceA is null
                || !analyticalModel_ReferenceA.TryGetValue(Analytical.AnalyticalModelParameter.SimulationResultProvenance, out SimulationResultProvenance simulationResultProvenance_ReferenceA)
                || simulationResultProvenance_ReferenceA is null)
            {
                result.Add("Reference A carries no simulation-result provenance, so this Iteration 3 pairing cannot be shown to describe it.");
            }
            else
            {
                if (!string.Equals(simulationResultProvenance_ReferenceA.Fingerprint_Model, partOIteration3Record.Fingerprint_Model_ReferenceA, StringComparison.Ordinal))
                {
                    result.Add("Reference A's design state has changed since this Iteration 3 pairing was produced, so the pairing no longer describes the model in front of you. Prepare and run Iteration 1a again, then run Iteration 3.");
                }

                if (!string.Equals(simulationResultProvenance_ReferenceA.Fingerprint_OverheatingScenarios, partOIteration3Record.Fingerprint_Scenarios_ReferenceA, StringComparison.Ordinal))
                {
                    result.Add("Reference A's overheating scenarios have changed since this Iteration 3 pairing was produced, so the two cases were assessed against different criteria. Prepare and run Iteration 1a again, then run Iteration 3.");
                }
            }

            //The TAS case can only be re-checked where the session still holds one. A reopened run
            //deliberately carries no simulation context, and saying so is more honest than silently
            //skipping the check.
            if (partORun.SimulationContext is null)
            {
                //Not a refusal: the fingerprints above already tie the pairing to Reference A's design
                //and scenarios, and the two results files are tied to it by their own provenance.
            }
            else if (!string.Equals(PartOIteration3ScenarioFingerprint(partORun.SimulationContext), partOIteration3Record.Fingerprint_Scenario, StringComparison.Ordinal))
            {
                result.Add(string.Format(
                    "This Iteration 3 pairing was run as '{0}' and this session's run is '{1}', so the two are not the same thermal case.",
                    partOIteration3Record.Fingerprint_Scenario ?? "<none>",
                    PartOIteration3ScenarioFingerprint(partORun.SimulationContext)));
            }

            //-------------------------------------------------------------------------------------------
            //Candidate B's files
            //-------------------------------------------------------------------------------------------
            foreach (PartOIteration3FileRecord partOIteration3FileRecord in partOIteration3Record.Files)
            {
                //A TM59 report is rewritten by every assessment of its results - this review's own
                //included - so it is lineage and not what the comparison is built from. Validating it would
                //make the second review of an unchanged pairing refuse because the first one ran.
                if (PartOIteration3Roles.IsRegeneratedByAssessment(partOIteration3FileRecord.Role))
                {
                    continue;
                }

                if (!partOIteration3FileRecord.Current(out string refusal))
                {
                    result.Add(refusal);
                }
            }

            if (result.Count != 0)
            {
                return result;
            }

            //A refused pairing has no Candidate B model to load and is not supposed to have one - its
            //ledger is the whole answer. Validating the files above still applies, because the artifacts
            //it DID produce are part of that answer.
            if (!partOIteration3Record.IsComplete)
            {
                return result;
            }

            PartOIteration3FileRecord partOIteration3FileRecord_Model = partOIteration3Record.File(PartOIteration3Roles.CandidateB_Model);
            PartOIteration3FileRecord partOIteration3FileRecord_TSD = partOIteration3Record.File(PartOIteration3Roles.Bridge_TSD);

            if (partOIteration3FileRecord_Model is null || partOIteration3FileRecord_TSD is null)
            {
                result.Add("This Iteration 3 pairing records itself as complete but does not name both Candidate B's model and the results it was produced from, so it cannot be reopened.");

                return result;
            }

            try
            {
                analyticalModel_CandidateB = Core.Convert.ToSAM<AnalyticalModel>(partOIteration3FileRecord_Model.Path)?.Find(x => x is not null);
            }
            catch (IOException exception)
            {
                result.Add(string.Format("Candidate B's model at '{0}' could not be read. ({1})", partOIteration3FileRecord_Model.Path, exception.Message));

                return result;
            }

            if (analyticalModel_CandidateB is null)
            {
                result.Add(string.Format("Candidate B's model at '{0}' could not be read as an analytical model.", partOIteration3FileRecord_Model.Path));

                return result;
            }

            //The persisted Candidate B must record its own provenance to the BRIDGE results - the file
            //its resultant temperatures were read from. This is the same authority a reopened Reference A
            //is validated by, asked of the other side of the pairing.
            if (!analyticalModel_CandidateB.TryGetValue(Analytical.AnalyticalModelParameter.SimulationResultProvenance, out SimulationResultProvenance simulationResultProvenance_CandidateB) || simulationResultProvenance_CandidateB is null)
            {
                result.Add(string.Format("Candidate B's model at '{0}' records no simulation results, so it cannot be shown to belong to this pairing.", partOIteration3FileRecord_Model.Path));

                return result;
            }

            if (!simulationResultProvenance_CandidateB.TryResolvePath_TSD(analyticalModel_CandidateB, partOIteration3FileRecord_Model.Path, out path_TSD_CandidateB, out string refusal_CandidateB))
            {
                result.Add(string.Format("Candidate B's model does not belong to its recorded results. {0}", refusal_CandidateB));

                return result;
            }

            if (!SamePlace(path_TSD_CandidateB, partOIteration3FileRecord_TSD.Path))
            {
                result.Add(string.Format(
                    "Candidate B's model records the results at '{0}' and this pairing recorded '{1}', so the model and the temperatures being compared are not from the same run.",
                    path_TSD_CandidateB,
                    partOIteration3FileRecord_TSD.Path));
            }

            return result;
        }

        /// <summary>
        /// Whether two paths name the same place: compared after normalisation, so a spelling (slashes, a '..' segment) is not
        /// a different file. A path that cannot be normalised is compared as written.
        /// </summary>
        private static bool SamePlace(string path_A, string path_B)
        {
            if (!string.IsNullOrWhiteSpace(path_A) && !string.IsNullOrWhiteSpace(path_B))
            {
                try
                {
                    return string.Equals(System.IO.Path.GetFullPath(path_A), System.IO.Path.GetFullPath(path_B), StringComparison.OrdinalIgnoreCase);
                }
                catch (Exception exception) when (exception is ArgumentException || exception is NotSupportedException || exception is System.IO.IOException)
                {
                }
            }

            return string.Equals(path_A, path_B, StringComparison.OrdinalIgnoreCase);
        }
    }
}
