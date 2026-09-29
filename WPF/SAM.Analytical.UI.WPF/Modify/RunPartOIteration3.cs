// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Systems;
using SAM.Analytical.Tas.TPD;
using SAM.Core;
using SAM.Core.Tas;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        /// <summary>
        /// How close two design airflows have to be to be the same airflow [l/s].
        /// <para>
        /// The two numbers being compared are the <b>same</b> design duty read twice - once off the
        /// prepared model's terminals, once off what the materialisation put on the route - so the only
        /// difference that can legitimately exist is the last bit of a double that has been summed in a
        /// different order. A thousandth of a litre per second is far below anything a ventilation design
        /// states and far above that. It is deliberately <b>not</b> an engineering tolerance: there is no
        /// airflow difference this run is willing to accept.
        /// </para>
        /// </summary>
        internal const double Tolerance_DesignAirFlow_Lps = 1e-6;

        /// <summary>
        /// Runs one Approved Document O Iteration 3 A/B pairing, start to finish, and answers the ordered
        /// ledger, the durable record and - only where every stage completed - the comparison.
        ///
        /// <para><b>Orchestration only. There is no second engineering authority here</b></para>
        /// <para>
        /// Every number this produces is either another authority's, carried verbatim, or a descriptive
        /// statistic over two series those authorities produced. SAM remains the analytical and design
        /// authority and owns TM59; SAM_Systems owns the mechanical-system materialisation; SAM_Tas owns
        /// the no-IZAM source, the TPD conversion, the Systems simulation and the resultant-temperature
        /// provider. What is decided here is sequencing, scope, identity and presentation.
        /// </para>
        ///
        /// <para><b>Fail closed at every boundary</b></para>
        /// <para>
        /// The ledger, not this method, enforces the pipeline rule: once a stage refuses, no later stage
        /// can be recorded as completed, and <c>PartOIteration3Result</c> drops the comparison unless the
        /// whole chain completed. Each stage below therefore returns as soon as it refuses, and the two
        /// rules agree by construction rather than by discipline.
        /// </para>
        ///
        /// <para><b>Deterministic paths, proven ownership</b></para>
        /// <para>
        /// Candidate B's files are named from Reference A's project, which is what makes the pairing
        /// reopenable and also means a failed attempt leaves its files exactly where this one will look.
        /// So every fixed path is fingerprinted before anything is written, and a file that has not
        /// changed since is never reported as this attempt's - see
        /// <see cref="PartOIteration3Artifacts"/>. The previous Candidate B model and the previous record
        /// are additionally <b>deleted</b> at the start, because those two are the artifacts a later
        /// session would act on.
        /// </para>
        ///
        /// <para><b>Scaling</b></para>
        /// <para>
        /// Every join below is a dictionary lookup on a guid, every index is built once, and the only
        /// walks over the annual series are the two the comparison itself has to make. Nothing is looked
        /// up by name - three flats hold three rooms called "Bedroom 2" - and no list is scanned inside a
        /// loop over another list.
        /// </para>
        /// </summary>
        /// <param name="partORun">The completed, eligible Iteration 1a run this pairing is built on.</param>
        /// <param name="iPartOIteration3Pipeline">Who does the work - see <see cref="IPartOIteration3Pipeline"/>.</param>
        /// <param name="cancellationToken">Aborts the TAS steps between stages.</param>
        /// <param name="partOIteration3BehaviourMode">
        /// PR5A (SAM#111 plan §J): <c>Parity</c> (the default - the foundation control, B0) or
        /// <c>SelectedProduct</c>, which resolves every scoped air handling unit's already-selected
        /// product before materialising. Placed after <paramref name="cancellationToken"/>, both optional,
        /// so every existing positional call site - which passes at most three arguments - is unaffected.
        /// </param>
        /// <param name="stageStarting">
        /// Told which ledger stage is about to start - for a progress window, which is the only reason it
        /// exists. It observes; nothing it does can change the run.
        /// <para>
        /// <b>Cancellation is between stages</b>, and only there: <paramref name="cancellationToken"/> is
        /// checked as each of the long stages is about to start (the thermal source, the Systems route, the
        /// resultant temperature and Candidate B's assessment). A TAS call already in flight is never
        /// interrupted. A cancelled run is a refusal at the stage it did not start, recorded like any other,
        /// so it never makes the method reviewable and can simply be run again.
        /// </para>
        /// </param>
        public static PartOIteration3Result RunPartOIteration3(PartORun partORun, IPartOIteration3Pipeline iPartOIteration3Pipeline, CancellationToken cancellationToken = default, PartOIteration3BehaviourMode partOIteration3BehaviourMode = PartOIteration3BehaviourMode.Parity, Action<PartOIteration3Stage> stageStarting = null)
        {
            PartOIteration3Ledger partOIteration3Ledger = new();

            //Announces a stage and, for the long ones, honours a cancel requested before it starts.
            bool Starting(PartOIteration3Stage partOIteration3Stage, bool cancellable = false)
            {
                stageStarting?.Invoke(partOIteration3Stage);

                if (!cancellable || !cancellationToken.IsCancellationRequested)
                {
                    return true;
                }

                partOIteration3Ledger.Refuse(
                    partOIteration3Stage,
                    "Cancelled.",
                    [string.Format("The run was cancelled before '{0}' started, so no comparison was produced. It can be run again.", Core.Query.Description(partOIteration3Stage))]);

                return false;
            }

            stageStarting?.Invoke(PartOIteration3Stage.Input);

            PartOIteration3Record partOIteration3Record = new()
            {
                Guid_Run = Guid.NewGuid(),
                Ticks_Utc = DateTime.UtcNow.Ticks,
                BehaviourMode = partOIteration3BehaviourMode,
            };

            List<string> notes = [];

            //=================================================================================================
            //Input
            //=================================================================================================
            if (partORun is null || iPartOIteration3Pipeline is null)
            {
                partOIteration3Ledger.Refuse(PartOIteration3Stage.Input, "No Part O run or no pipeline was supplied.", ["No Part O run or no pipeline was supplied, so no Approved Document O Iteration 3 pairing could be attempted."]);

                return Result(partOIteration3Ledger, partOIteration3Record, null, null, null, null, notes);
            }

            if (!Enum.IsDefined(typeof(PartOIteration3BehaviourMode), partOIteration3BehaviourMode))
            {
                partOIteration3Ledger.Refuse(
                    PartOIteration3Stage.Input,
                    "The requested ventilation equipment behaviour is not supported.",
                    [string.Format("Ventilation equipment behaviour value '{0}' is not defined, so no Candidate B was attempted.", (int)partOIteration3BehaviourMode)]);

                return Result(partOIteration3Ledger, partOIteration3Record, null, null, null, null, notes);
            }

            PartOIteration3Eligibility partOIteration3Eligibility = Query.PartOIteration3Eligibility(partORun, partORun.IsAssessable(out string refusal_Assessable), refusal_Assessable);

            if (!partOIteration3Eligibility.CanRun)
            {
                partOIteration3Ledger.Refuse(PartOIteration3Stage.Input, "This Part O run cannot produce a Candidate B.", [partOIteration3Eligibility.Refusal_Run]);

                return Result(partOIteration3Ledger, partOIteration3Record, null, null, null, null, notes);
            }

            AnalyticalModel analyticalModel_Prepared = partORun.AnalyticalModel_Prepared;
            AnalyticalModel analyticalModel_ReferenceA = partORun.AnalyticalModel_Assessment;
            PartOSimulationContext partOSimulationContext = partORun.SimulationContext;
            PartOPreparationContext partOPreparationContext = partORun.PreparationContext;
            List<OverheatingScenario> overheatingScenarios = partORun.OverheatingScenarios;

            string path_TSD_ReferenceA = partORun.Path_TSD;

            PartOIteration3Paths partOIteration3Paths = PartOIteration3Paths.Create(partOSimulationContext, path_TSD_ReferenceA, partOIteration3BehaviourMode);

            PartOIteration3Artifacts partOIteration3Artifacts = new(partOIteration3Record.Guid_Run);
            partOIteration3Artifacts.Snapshot(partOIteration3Paths.Paths_CandidateB);

            //Reference A's TM59 report is not Candidate B's to own, but this attempt's own assessment of A
            //rewrites it - so it is fingerprinted too, and recorded only where that rewrite happened.
            partOIteration3Artifacts.Snapshot([partOIteration3Paths.Path_TM59Report_ReferenceA]);

            //Deleted, not merely fingerprinted. These two are the artifacts a LATER session acts on - one
            //is a reopenable Candidate B model and the other is the pairing record itself - so a failed or
            //abandoned attempt must not leave either of them behind claiming to describe this design.
            //Everything else Candidate B writes is evidence, and evidence is proven by ownership rather
            //than by deletion. This does not weaken the no-IZAM source's or the bridge's own stale-output
            //deletion, which still runs.
            List<string> refusals_Clear = [];
            Delete(partOIteration3Paths.Path_Model_CandidateB, "the previous Candidate B model", refusals_Clear, notes);
            Delete(partOIteration3Paths.Path_Record, "the previous Iteration 3 pairing record", refusals_Clear, notes);

            if (refusals_Clear.Count != 0)
            {
                partOIteration3Ledger.Refuse(PartOIteration3Stage.Input, "An earlier attempt's Candidate B could not be cleared.", refusals_Clear);

                return Result(partOIteration3Ledger, partOIteration3Record, null, null, null, partOIteration3Paths, notes);
            }

            partOIteration3Record.Path_TSD_ReferenceA = path_TSD_ReferenceA;
            partOIteration3Record.Path_Model_ReferenceA = Query.Path_PartORunModel(path_TSD_ReferenceA);
            partOIteration3Record.ProjectName_ReferenceA = partOIteration3Paths.ProjectName_ReferenceA;
            partOIteration3Record.ProjectName_CandidateB = partOIteration3Paths.ProjectName_CandidateB;
            partOIteration3Record.Fingerprint_Scenario = Query.PartOIteration3ScenarioFingerprint(partOSimulationContext);
            partOIteration3Record.AddPreparedSystems(partORun.Guids_VentilationSystem_Prepared);

            partOIteration3Ledger.Complete(
                PartOIteration3Stage.Input,
                string.Format(
                    "Reference run '{0}' ({3}) is in this session, complete over the full year, and captured {1} prepared ventilation system identity(ies). Candidate B will be written as '{2}'.",
                    partOIteration3Paths.ProjectName_ReferenceA,
                    partORun.Guids_VentilationSystem_Prepared.Count,
                    partOIteration3Paths.ProjectName_CandidateB,
                    Query.PartOIterationText(partORun)));

            //=================================================================================================
            //Reference A
            //=================================================================================================
            Starting(PartOIteration3Stage.ReferenceA);

            if (!analyticalModel_ReferenceA.TryGetValue(Analytical.AnalyticalModelParameter.SimulationResultProvenance, out SimulationResultProvenance simulationResultProvenance) || simulationResultProvenance is null || !simulationResultProvenance.IsComplete)
            {
                partOIteration3Ledger.Refuse(
                    PartOIteration3Stage.ReferenceA,
                    "Reference A does not record the results it was produced from.",
                    ["The Iteration 1a run's model carries no complete simulation-result provenance, so the pairing could not state which design state and which results it is built on. Prepare and run the iteration again."]);

                return Result(partOIteration3Ledger, partOIteration3Record, null, null, null, partOIteration3Paths, notes);
            }

            partOIteration3Record.Fingerprint_Model_ReferenceA = simulationResultProvenance.Fingerprint_Model;
            partOIteration3Record.Fingerprint_Scenarios_ReferenceA = simulationResultProvenance.Fingerprint_OverheatingScenarios;

            AdjacencyCluster adjacencyCluster_Prepared = analyticalModel_Prepared?.AdjacencyCluster;

            if (adjacencyCluster_Prepared is null)
            {
                partOIteration3Ledger.Refuse(
                    PartOIteration3Stage.ReferenceA,
                    "Reference A carries no prepared design.",
                    ["The Iteration 1a run holds no prepared analytical model, so Candidate B has no design to be built from."]);

                return Result(partOIteration3Ledger, partOIteration3Record, null, null, null, partOIteration3Paths, notes);
            }

            //One walk. Everything below is a dictionary lookup off this.
            Dictionary<Guid, PartOIteration3Room> dictionary_Room = Query.PartOIteration3Rooms(adjacencyCluster_Prepared, partOPreparationContext.Zones);

            if (dictionary_Room.Count == 0)
            {
                partOIteration3Ledger.Refuse(
                    PartOIteration3Stage.ReferenceA,
                    "The dwelling scope resolves to no room.",
                    ["The Approved Document O dwelling scope this run was prepared over resolves to no space on the prepared model, so there is nothing to compare."]);

                return Result(partOIteration3Ledger, partOIteration3Record, null, null, null, partOIteration3Paths, notes);
            }

            List<Guid> guids_Space_Dwelling = [.. dictionary_Room.Keys];
            guids_Space_Dwelling.Sort();

            partOIteration3Ledger.Complete(
                PartOIteration3Stage.ReferenceA,
                string.Format("Reference A is '{0}' over {1} dwelling room(s), produced by the existing TBD/IZAM route.", path_TSD_ReferenceA, dictionary_Room.Count));

            //=================================================================================================
            //Reference A TM59 - the UNCHANGED authority, with its resultant temperatures captured
            //=================================================================================================
            Starting(PartOIteration3Stage.ReferenceATM59);

            PartOIteration3Assessment partOIteration3Assessment_A = iPartOIteration3Pipeline.Assess(analyticalModel_ReferenceA, path_TSD_ReferenceA, overheatingScenarios, guids_Space_Dwelling);

            if (partOIteration3Assessment_A is null || !partOIteration3Assessment_A.IsAssessed)
            {
                partOIteration3Ledger.Refuse(
                    PartOIteration3Stage.ReferenceATM59,
                    "Reference A could not be assessed.",
                    [partOIteration3Assessment_A?.Refusal ?? string.Format("The Iteration 1a results at '{0}' could not be assessed.", path_TSD_ReferenceA)]);

                return Result(partOIteration3Ledger, partOIteration3Record, null, null, null, partOIteration3Paths, notes);
            }

            partOIteration3Record.Status_ReferenceA = partOIteration3Assessment_A.OccupiedSpaceComplianceStatus;

            partOIteration3Ledger.Complete(
                PartOIteration3Stage.ReferenceATM59,
                string.Format(
                    "The existing TM59 authority assessed Reference A over {0} space(s): {1}. {2} dwelling room(s) carry a captured resultant temperature series.",
                    partOIteration3Assessment_A.Count_Processed,
                    Core.Query.Description(partOIteration3Assessment_A.OccupiedSpaceComplianceStatus),
                    partOIteration3Assessment_A.ResultantTemperatures.Count));

            //=================================================================================================
            //System scope - SAM #114
            //=================================================================================================
            Starting(PartOIteration3Stage.SystemScope);

            PartOIteration3SystemScope partOIteration3SystemScope = Query.PartOIteration3SystemScope(adjacencyCluster_Prepared, partORun.Guids_VentilationSystem_Prepared, guids_Space_Dwelling);

            if (!partOIteration3SystemScope.IsScoped)
            {
                partOIteration3Ledger.Refuse(PartOIteration3Stage.SystemScope, "The ventilation design under assessment could not be scoped.", partOIteration3SystemScope.Refusals);

                return Result(partOIteration3Ledger, partOIteration3Record, null, null, null, partOIteration3Paths, notes);
            }

            partOIteration3Record.AddScopedOutSystems(partOIteration3SystemScope.Guids_Removed);
            partOIteration3Record.AddScopeNotes(partOIteration3SystemScope.Notes);

            partOIteration3Ledger.Complete(
                PartOIteration3Stage.SystemScope,
                string.Format(
                    "{0} ventilation system(s) built by this iteration are the design under assessment; {1} authored system(s) carry no mechanical duty and were left out of the materialisation input.",
                    partOIteration3SystemScope.Guids_Retained.Count,
                    partOIteration3SystemScope.Guids_Removed.Count));

            //=================================================================================================
            //Equipment resolution - PR5A (SAM#111 plan §J). A no-op in Parity mode.
            //=================================================================================================
            Starting(PartOIteration3Stage.EquipmentResolution);

            Dictionary<Guid, MechanicalVentilationUnitSettings> unitSettings = [];
            List<PartOIteration3EquipmentEvidence> equipment = [];
            Dictionary<Guid, MechanicalVentilationCoolingSettings> coolingSettings = [];
            List<PartOIteration3CoolingEvidence> cooling = [];
            Dictionary<Guid, MechanicalVentilationGuidanceSettings> guidanceSettings = [];
            SystemVentilationFanHeatGainPolicy fanHeatGainPolicy = SystemVentilationFanHeatGainPolicy.ClearToZero;

            if (partOIteration3BehaviourMode == PartOIteration3BehaviourMode.Parity)
            {
                partOIteration3Ledger.Complete(PartOIteration3Stage.EquipmentResolution, "Parity mode: Candidate B0, unchanged. No product was resolved.");
            }
            else if (partOIteration3BehaviourMode == PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance)
            {
                //SAM#123: the selected product operated to its manufacturer's guidance. Everything product-
                //specific is the catalogue's; the fans stay heat-gain free (ClearToZero) as in B0.
                VentilationUnitCatalogue ventilationUnitCatalogue = VentilationUnitCatalogue.Read();

                partOIteration3Record.Directory_VentilationUnitCatalogue = ventilationUnitCatalogue.Directory;
                partOIteration3Record.Path_VentilationUnitCatalogue = ventilationUnitCatalogue.Path;
                partOIteration3Record.Schema_VentilationUnitCatalogue = ventilationUnitCatalogue.Schema;
                partOIteration3Record.Sha256_VentilationUnitCatalogue = ventilationUnitCatalogue.Sha256;

                List<string> refusals_Guidance = Query.PartOIteration3GuidanceResolution(
                    partOIteration3SystemScope.AdjacencyCluster,
                    ventilationUnitCatalogue,
                    out guidanceSettings,
                    out List<string> notes_Guidance,
                    out List<PartOIteration3GuidanceEvidence> guidanceEvidence);

                if (refusals_Guidance.Count != 0)
                {
                    partOIteration3Ledger.Refuse(
                        PartOIteration3Stage.EquipmentResolution,
                        "The selected products' manufacturer guidance could not all be resolved.",
                        refusals_Guidance);

                    return Result(partOIteration3Ledger, partOIteration3Record, null, null, null, partOIteration3Paths, notes);
                }

                AddNotes(notes, notes_Guidance);
                partOIteration3Record.AddScopeNotes(notes_Guidance);

                foreach (PartOIteration3GuidanceEvidence partOIteration3GuidanceEvidence in guidanceEvidence)
                {
                    partOIteration3Record.Add(partOIteration3GuidanceEvidence);
                }

                partOIteration3Ledger.Complete(
                    PartOIteration3Stage.EquipmentResolution,
                    string.Format("{0} air handling unit(s) resolved to a selected product's manufacturer guidance (provisional, not certified performance).", guidanceSettings.Count));
            }
            else if (partOIteration3BehaviourMode == PartOIteration3BehaviourMode.SelectedProductCooling)
            {
                //PR5B (SAM#111): B0 plus the selected product's cooling module. The ventilation is the
                //foundation control's own - MV.json, no unit settings, ClearToZero - so B4 - B0 is the cooling
                //layer alone; only the cooling module is resolved.
                VentilationUnitCatalogue ventilationUnitCatalogue = VentilationUnitCatalogue.Read();

                partOIteration3Record.Directory_VentilationUnitCatalogue = ventilationUnitCatalogue.Directory;
                partOIteration3Record.Path_VentilationUnitCatalogue = ventilationUnitCatalogue.Path;
                partOIteration3Record.Schema_VentilationUnitCatalogue = ventilationUnitCatalogue.Schema;
                partOIteration3Record.Sha256_VentilationUnitCatalogue = ventilationUnitCatalogue.Sha256;

                List<string> refusals_Cooling = Query.PartOIteration3CoolingResolution(
                    partOIteration3SystemScope.AdjacencyCluster,
                    ventilationUnitCatalogue,
                    out coolingSettings,
                    out cooling,
                    out List<string> notes_Cooling);

                if (refusals_Cooling.Count != 0)
                {
                    partOIteration3Ledger.Refuse(
                        PartOIteration3Stage.EquipmentResolution,
                        "The selected products' cooling modules could not all be resolved.",
                        refusals_Cooling);

                    return Result(partOIteration3Ledger, partOIteration3Record, null, null, null, partOIteration3Paths, notes);
                }

                AddNotes(notes, notes_Cooling);

                partOIteration3Ledger.Complete(
                    PartOIteration3Stage.EquipmentResolution,
                    string.Format("{0} air handling unit(s) resolved to a selected product's published cooling module; ventilation stays the Parity foundation control.", cooling.Count));
            }
            else
            {
                VentilationUnitCatalogue ventilationUnitCatalogue = VentilationUnitCatalogue.Read();

                partOIteration3Record.Directory_VentilationUnitCatalogue = ventilationUnitCatalogue.Directory;
                partOIteration3Record.Path_VentilationUnitCatalogue = ventilationUnitCatalogue.Path;
                partOIteration3Record.Schema_VentilationUnitCatalogue = ventilationUnitCatalogue.Schema;
                partOIteration3Record.Sha256_VentilationUnitCatalogue = ventilationUnitCatalogue.Sha256;

                List<string> refusals_Equipment = Query.PartOIteration3EquipmentResolution(
                    partOIteration3SystemScope.AdjacencyCluster,
                    ventilationUnitCatalogue,
                    out unitSettings,
                    out equipment,
                    out List<string> notes_Equipment);

                if (refusals_Equipment.Count != 0)
                {
                    partOIteration3Ledger.Refuse(
                        PartOIteration3Stage.EquipmentResolution,
                        "The selected products could not all be resolved to certified manufacturer data.",
                        refusals_Equipment);

                    return Result(partOIteration3Ledger, partOIteration3Record, null, null, null, partOIteration3Paths, notes);
                }

                AddNotes(notes, notes_Equipment);

                fanHeatGainPolicy = SystemVentilationFanHeatGainPolicy.FromSystemsGraph;

                partOIteration3Ledger.Complete(
                    PartOIteration3Stage.EquipmentResolution,
                    string.Format("{0} air handling unit(s) resolved to a selected product's certified heat-recovery efficiency and specific fan power.", equipment.Count));
            }

            //=================================================================================================
            //Materialisation - SAM_Systems
            //=================================================================================================
            Starting(PartOIteration3Stage.Materialisation);

            List<Space> spaces_Scope = [];
            foreach (Guid guid in guids_Space_Dwelling)
            {
                Space space = partOIteration3SystemScope.AdjacencyCluster.GetObject<Space>(guid);

                if (space is not null)
                {
                    spaces_Scope.Add(space);
                }
            }

            MechanicalVentilationMaterialisation mechanicalVentilationMaterialisation = iPartOIteration3Pipeline.Materialise(partOIteration3SystemScope.AdjacencyCluster, spaces_Scope, unitSettings, coolingSettings, guidanceSettings);

            if (mechanicalVentilationMaterialisation is null || !mechanicalVentilationMaterialisation.IsMaterialised)
            {
                partOIteration3Ledger.Refuse(
                    PartOIteration3Stage.Materialisation,
                    "The explicit mechanical ventilation could not be materialised.",
                    mechanicalVentilationMaterialisation?.Refusals ?? ["The mechanical ventilation materialisation produced nothing and said nothing about why."]);

                return Result(partOIteration3Ledger, partOIteration3Record, null, null, null, partOIteration3Paths, notes);
            }

            if (equipment.Count != 0)
            {
                List<string> refusals_EquipmentBinding = Query.PartOIteration3EquipmentBindings(equipment, mechanicalVentilationMaterialisation.Bindings);

                if (refusals_EquipmentBinding.Count != 0)
                {
                    partOIteration3Ledger.Refuse(
                        PartOIteration3Stage.Materialisation,
                        "The manufacturer-aware equipment evidence could not be bound to the materialised systems.",
                        refusals_EquipmentBinding);

                    return Result(partOIteration3Ledger, partOIteration3Record, null, null, null, partOIteration3Paths, notes);
                }

                foreach (PartOIteration3EquipmentEvidence partOIteration3EquipmentEvidence in equipment)
                {
                    partOIteration3Record.Add(partOIteration3EquipmentEvidence);
                }
            }

            //PR5B: every resolved cooling module has to have become exactly one branch, inside its own unit's
            //air system - bound here by identity before any TAS work is spent on it.
            if (cooling.Count != 0)
            {
                List<string> refusals_CoolingBinding = Query.PartOIteration3CoolingBindings(cooling, mechanicalVentilationMaterialisation);

                if (refusals_CoolingBinding.Count != 0)
                {
                    partOIteration3Ledger.Refuse(
                        PartOIteration3Stage.Materialisation,
                        "The resolved cooling modules could not be bound to the materialised systems.",
                        refusals_CoolingBinding);

                    return Result(partOIteration3Ledger, partOIteration3Record, null, null, null, partOIteration3Paths, notes);
                }
            }

            partOIteration3Ledger.Complete(
                PartOIteration3Stage.Materialisation,
                string.Format("SAM_Systems materialised the explicit mechanical ventilation over {0} room(s) with {1} analytical binding(s).", spaces_Scope.Count, mechanicalVentilationMaterialisation.Bindings.Count));

            //=================================================================================================
            //Thermal source - SAM_Tas, the same case as Reference A with no mechanical ventilation of its own
            //=================================================================================================
            if (!Starting(PartOIteration3Stage.ThermalSource, true))
            {
                return Result(partOIteration3Ledger, partOIteration3Record, null, null, null, partOIteration3Paths, notes);
            }

            PartOSimulationContext partOSimulationContext_CandidateB = partOSimulationContext.Copy(partOIteration3Paths.ProjectName_CandidateB);

            NoIzamThermalSource noIzamThermalSource = iPartOIteration3Pipeline.ThermalSource(
                analyticalModel_Prepared,
                partOSimulationContext_CandidateB,
                partOIteration3Paths.ProjectName_CandidateB,
                cancellationToken,
                out AnalyticalModel analyticalModel_CandidateB,
                out bool cancelled,
                out List<string> notes_ThermalSource,
                out string refusal_ThermalSource);

            AddNotes(notes, notes_ThermalSource);

            if (cancelled || noIzamThermalSource is null || !noIzamThermalSource.IsComplete || analyticalModel_CandidateB is null)
            {
                List<string> reasons = [];

                if (cancelled)
                {
                    reasons.Add("The Candidate B thermal source run was cancelled, so no comparison was produced.");
                }

                if (!string.IsNullOrWhiteSpace(refusal_ThermalSource))
                {
                    reasons.Add(refusal_ThermalSource);
                }

                AddNotes(reasons, noIzamThermalSource?.Refusals);

                if (analyticalModel_CandidateB is null && !cancelled)
                {
                    reasons.Add("The no-IZAM workflow returned no analytical model, so there is nothing carrying the TAS zone identities Candidate B's assessment resolves against.");
                }

                partOIteration3Ledger.Refuse(
                    PartOIteration3Stage.ThermalSource,
                    "Candidate B's dedicated no-IZAM thermal source was not produced.",
                    reasons,
                    Claim(partOIteration3Artifacts, [partOIteration3Paths.Path_TBD_ThermalSource, partOIteration3Paths.Path_TSD_ThermalSource]));

                return Result(partOIteration3Ledger, partOIteration3Record, null, null, null, partOIteration3Paths, notes);
            }

            partOIteration3Record.RemovedIZAMs = noIzamThermalSource.RemovedIZAMs;
            partOIteration3Record.RemovedMechanicalVentilationGains = noIzamThermalSource.RemovedMechanicalVentilationGains;

            if (!partOIteration3Artifacts.TryClaim(new[] { partOIteration3Paths.Path_TBD_ThermalSource, partOIteration3Paths.Path_TSD_ThermalSource }, out List<string> artifacts_ThermalSource, out List<string> refusals_ThermalSource))
            {
                partOIteration3Ledger.Refuse(PartOIteration3Stage.ThermalSource, "Candidate B's thermal source cannot be told apart from an earlier attempt's.", refusals_ThermalSource, artifacts_ThermalSource);

                return Result(partOIteration3Ledger, partOIteration3Record, null, null, null, partOIteration3Paths, notes);
            }

            partOIteration3Record.Add(new PartOIteration3FileRecord(PartOIteration3Roles.ThermalSource_TBD, partOIteration3Paths.Path_TBD_ThermalSource, Length(partOIteration3Paths.Path_TBD_ThermalSource), Ticks(partOIteration3Paths.Path_TBD_ThermalSource)));
            partOIteration3Record.Add(new PartOIteration3FileRecord(PartOIteration3Roles.ThermalSource_TSD, partOIteration3Paths.Path_TSD_ThermalSource, Length(partOIteration3Paths.Path_TSD_ThermalSource), Ticks(partOIteration3Paths.Path_TSD_ThermalSource)));

            partOIteration3Ledger.Complete(
                PartOIteration3Stage.ThermalSource,
                string.Format(
                    "The no-IZAM thermal source simulated the same TAS case as Reference A: inherited IZAMs {0}, mechanical ventilation gain {1}, {2} room(s) carrying a TAS zone identity.",
                    noIzamThermalSource.RemovedIZAMs ? "removed" : "NOT removed",
                    noIzamThermalSource.RemovedMechanicalVentilationGains ? "neutralised" : "NOT neutralised",
                    noIzamThermalSource.Count_ZoneReferences),
                artifacts_ThermalSource);

            //=================================================================================================
            //Systems conversion, simulation and zone temperature - SAM_Tas, one call, three stages
            //=================================================================================================
            if (!Starting(PartOIteration3Stage.SystemsConversion, true))
            {
                return Result(partOIteration3Ledger, partOIteration3Record, null, null, null, partOIteration3Paths, notes);
            }

            int startHour = 0;
            int endHour = PartOSimulationContext.HourCount_FullYear - 1;

            SystemVentilationRoute systemVentilationRoute = iPartOIteration3Pipeline.Route(noIzamThermalSource, mechanicalVentilationMaterialisation, partOIteration3Paths.Path_TPD, startHour, endHour, fanHeatGainPolicy);

            if (systemVentilationRoute is null || !systemVentilationRoute.IsComplete)
            {
                //Attributed to the stage the route actually got to, read off what it handed back: no
                //simulation evidence at all means the conversion never ran one; evidence that is not
                //complete means the simulation itself failed; complete evidence means the results did not
                //reconcile. The route does not tag its refusals with a stage, and inventing a fourth
                //place that decides what failed would be a second opinion about SAM_Tas' own run.
                SimulationEvidence simulationEvidence = systemVentilationRoute?.SimulationEvidence;

                PartOIteration3Stage partOIteration3Stage = simulationEvidence is null
                    ? PartOIteration3Stage.SystemsConversion
                    : simulationEvidence.Completed
                        ? PartOIteration3Stage.ZoneTemperature
                        : PartOIteration3Stage.SystemsSimulation;

                //Every stage before the failing one still completed, and the ledger will not record a
                //later stage until the earlier ones are there.
                if (partOIteration3Stage != PartOIteration3Stage.SystemsConversion)
                {
                    partOIteration3Ledger.Complete(PartOIteration3Stage.SystemsConversion, "The explicit ventilation converted to a TAS Systems document and reconciled against the source graph.", Claim(partOIteration3Artifacts, [partOIteration3Paths.Path_TPD]));
                }

                if (partOIteration3Stage == PartOIteration3Stage.ZoneTemperature)
                {
                    partOIteration3Ledger.Complete(PartOIteration3Stage.SystemsSimulation, "The TAS Systems simulation is evidenced as complete.");
                }

                List<string> reasons = [.. systemVentilationRoute?.Refusals ?? []];

                if (reasons.Count == 0)
                {
                    reasons.Add("The explicit TAS Systems ventilation route did not complete, and said nothing about why.");
                }

                partOIteration3Ledger.Refuse(
                    partOIteration3Stage,
                    "The explicit TAS Systems ventilation route did not complete.",
                    reasons,
                    partOIteration3Stage == PartOIteration3Stage.SystemsConversion ? Claim(partOIteration3Artifacts, [partOIteration3Paths.Path_TPD]) : null);

                return Result(partOIteration3Ledger, partOIteration3Record, null, null, null, partOIteration3Paths, notes);
            }

            AddNotes(notes, systemVentilationRoute.Notes);

            if (!partOIteration3Artifacts.TryClaim(partOIteration3Paths.Path_TPD, out string artifact_TPD, out string refusal_TPD))
            {
                partOIteration3Ledger.Refuse(PartOIteration3Stage.SystemsConversion, "Candidate B's TAS Systems document cannot be told apart from an earlier attempt's.", [refusal_TPD]);

                return Result(partOIteration3Ledger, partOIteration3Record, null, null, null, partOIteration3Paths, notes);
            }

            List<SystemVentilationBinding> systemVentilationBindings = systemVentilationRoute.Bindings;
            List<SystemVentilationConnectionBinding> systemVentilationConnectionBindings = systemVentilationRoute.ConnectionBindings;

            HashSet<Guid> guids_AirSystem = [];
            int count_Supply = 0;
            int count_Extract = 0;
            int count_Transfer = 0;

            foreach (SystemVentilationConnectionBinding systemVentilationConnectionBinding in systemVentilationConnectionBindings)
            {
                switch (systemVentilationConnectionBinding.ConnectionType)
                {
                    case SystemVentilationConnectionType.Supply:
                        count_Supply++;
                        break;

                    case SystemVentilationConnectionType.Extract:
                        count_Extract++;
                        break;

                    case SystemVentilationConnectionType.Transfer:
                        count_Transfer++;
                        break;
                }
            }

            foreach (SystemVentilationBinding systemVentilationBinding in systemVentilationBindings)
            {
                guids_AirSystem.Add(systemVentilationBinding.Guid_AirSystem);
            }

            partOIteration3Record.Count_Connection_Supply = count_Supply;
            partOIteration3Record.Count_Connection_Extract = count_Extract;
            partOIteration3Record.Count_Connection_Transfer = count_Transfer;
            partOIteration3Record.Count_AirSystem = guids_AirSystem.Count;

            partOIteration3Ledger.Complete(
                PartOIteration3Stage.SystemsConversion,
                string.Format(
                    "{0} physical air system(s), {1} room(s) and {2} directed leg(s) ({3} supply, {4} extract, {5} transfer) converted and reconciled against the source graph.",
                    guids_AirSystem.Count,
                    systemVentilationBindings.Count,
                    systemVentilationConnectionBindings.Count,
                    count_Supply,
                    count_Extract,
                    count_Transfer),
                [artifact_TPD]);

            partOIteration3Ledger.Complete(
                PartOIteration3Stage.SystemsSimulation,
                string.Format("The TAS Systems simulation is evidenced as complete. {0}", systemVentilationRoute.SimulationEvidence?.NativeDiagnostic is string diagnostic && !string.IsNullOrWhiteSpace(diagnostic) ? string.Format("TAS said: {0}", diagnostic) : "TAS reported no diagnostic."));

            SystemZoneTemperatureResults systemZoneTemperatureResults = systemVentilationRoute.SystemZoneTemperatureResults;

            //PR5B: each unit's cooling outcome, verbatim from the route's own checked evidence, and the hourly
            //OperatingAirFlow history persisted beside the TPD - kept apart from every DesignAirFlow.
            if (cooling.Count != 0)
            {
                List<string> refusals_CoolingOutcome = Query.PartOIteration3CoolingOutcomes(cooling, systemVentilationRoute.RecirculationCoolingResults);
                List<string> artifacts_OperatingAirFlow = [];

                if (refusals_CoolingOutcome.Count == 0)
                {
                    try
                    {
                        File.WriteAllText(partOIteration3Paths.Path_OperatingAirFlow, Query.PartOIteration3OperatingAirFlowCsv(systemVentilationRoute.RecirculationCoolingResults, cooling));

                        if (partOIteration3Artifacts.TryClaim(partOIteration3Paths.Path_OperatingAirFlow, out string artifact_OperatingAirFlow, out string refusal_OperatingAirFlow))
                        {
                            artifacts_OperatingAirFlow.Add(artifact_OperatingAirFlow);
                            partOIteration3Record.Add(new PartOIteration3FileRecord(PartOIteration3Roles.OperatingAirFlow, partOIteration3Paths.Path_OperatingAirFlow, Length(partOIteration3Paths.Path_OperatingAirFlow), Ticks(partOIteration3Paths.Path_OperatingAirFlow)));
                        }
                        else
                        {
                            refusals_CoolingOutcome.Add(refusal_OperatingAirFlow);
                        }
                    }
                    catch (Exception exception)
                    {
                        refusals_CoolingOutcome.Add(string.Format("The OperatingAirFlow history could not be written to '{0}'. ({1})", partOIteration3Paths.Path_OperatingAirFlow, exception.Message));
                    }
                }

                if (refusals_CoolingOutcome.Count != 0)
                {
                    partOIteration3Ledger.Refuse(PartOIteration3Stage.ZoneTemperature, "The cooling modules' behaviour could not be recorded.", refusals_CoolingOutcome, artifacts_OperatingAirFlow);

                    return Result(partOIteration3Ledger, partOIteration3Record, null, null, null, partOIteration3Paths, notes);
                }

                foreach (PartOIteration3CoolingEvidence partOIteration3CoolingEvidence in cooling)
                {
                    partOIteration3Record.Add(partOIteration3CoolingEvidence);
                    AddNotes(notes, [string.Format("Cooling: {0}", partOIteration3CoolingEvidence)]);
                }
            }

            //SAM#123: what each manufacturer-guidance unit did, hour by hour, read back from TAS by the route,
            //persisted beside the TPD, and summarised into the record so a reopened pairing still states it.
            if (guidanceSettings.Count != 0)
            {
                GuidanceCoolingResults guidanceCoolingResults = systemVentilationRoute.GuidanceCoolingResults;
                List<string> refusals_Guidance = [];
                List<string> artifacts_Guidance = [];

                if (guidanceCoolingResults is null || !guidanceCoolingResults.IsComplete || guidanceCoolingResults.Results.Count != guidanceSettings.Count)
                {
                    refusals_Guidance.Add(string.Format(
                        "The route returned {0} manufacturer-guidance operation record(s) for {1} unit(s).",
                        guidanceCoolingResults?.Results.Count ?? 0,
                        guidanceSettings.Count));
                    refusals_Guidance.AddRange(guidanceCoolingResults?.Refusals ?? []);
                }
                else
                {
                    try
                    {
                        File.WriteAllText(partOIteration3Paths.Path_OperatingAirFlow, guidanceCoolingResults.ToCsv());

                        if (partOIteration3Artifacts.TryClaim(partOIteration3Paths.Path_OperatingAirFlow, out string artifact_Guidance, out string refusal_Guidance))
                        {
                            artifacts_Guidance.Add(artifact_Guidance);
                            partOIteration3Record.Add(new PartOIteration3FileRecord(PartOIteration3Roles.GuidanceOperation, partOIteration3Paths.Path_OperatingAirFlow, Length(partOIteration3Paths.Path_OperatingAirFlow), Ticks(partOIteration3Paths.Path_OperatingAirFlow)));
                        }
                        else
                        {
                            refusals_Guidance.Add(refusal_Guidance);
                        }
                    }
                    catch (Exception exception)
                    {
                        refusals_Guidance.Add(string.Format("The manufacturer-guidance operation history could not be written to '{0}'. ({1})", partOIteration3Paths.Path_OperatingAirFlow, exception.Message));
                    }
                }

                if (refusals_Guidance.Count != 0)
                {
                    partOIteration3Ledger.Refuse(PartOIteration3Stage.ZoneTemperature, "The manufacturer-guidance units' operation could not be recorded.", refusals_Guidance, artifacts_Guidance);

                    return Result(partOIteration3Ledger, partOIteration3Record, null, null, null, partOIteration3Paths, notes);
                }

                List<string> notes_Operation = guidanceCoolingResults.Results.ConvertAll(x => "MANUFACTURER GUIDANCE operation (TAS read-back): " + x.Summary());
                AddNotes(notes, notes_Operation);
                partOIteration3Record.AddScopeNotes(notes_Operation);
            }

            partOIteration3Ledger.Complete(
                PartOIteration3Stage.ZoneTemperature,
                string.Format(
                    "{0} room(s) returned a complete finite ZoneTemperature series over hours {1}..{2}.",
                    systemZoneTemperatureResults.Results.Count,
                    systemZoneTemperatureResults.StartHour,
                    systemZoneTemperatureResults.EndHour));

            //=================================================================================================
            //Resultant temperature - SAM_Tas' replaceable provider
            //=================================================================================================
            if (!Starting(PartOIteration3Stage.ResultantTemperature, true))
            {
                return Result(partOIteration3Ledger, partOIteration3Record, null, null, null, partOIteration3Paths, notes);
            }

            ResultantTemperatureResults resultantTemperatureResults = iPartOIteration3Pipeline.ResultantTemperatures(systemVentilationRoute, partOIteration3Paths.Path_TBD_Bridge);

            if (resultantTemperatureResults is null || !resultantTemperatureResults.IsComplete)
            {
                partOIteration3Ledger.Refuse(
                    PartOIteration3Stage.ResultantTemperature,
                    "Candidate B's resultant temperature was not produced.",
                    resultantTemperatureResults?.Refusals ?? ["The resultant temperature provider produced nothing and said nothing about why."],
                    Claim(partOIteration3Artifacts, [partOIteration3Paths.Path_TBD_Bridge, partOIteration3Paths.Path_TSD_Bridge]));

                return Result(partOIteration3Ledger, partOIteration3Record, null, null, null, partOIteration3Paths, notes);
            }

            AddNotes(notes, resultantTemperatureResults.Notes);

            if (!partOIteration3Artifacts.TryClaim(new[] { partOIteration3Paths.Path_TBD_Bridge, partOIteration3Paths.Path_TSD_Bridge }, out List<string> artifacts_Bridge, out List<string> refusals_Bridge))
            {
                partOIteration3Ledger.Refuse(PartOIteration3Stage.ResultantTemperature, "Candidate B's resultant temperature files cannot be told apart from an earlier attempt's.", refusals_Bridge, artifacts_Bridge);

                return Result(partOIteration3Ledger, partOIteration3Record, null, null, null, partOIteration3Paths, notes);
            }

            partOIteration3Record.Method_ResultantTemperature = resultantTemperatureResults.Method;
            partOIteration3Record.Add(new PartOIteration3FileRecord(PartOIteration3Roles.Bridge_TBD, partOIteration3Paths.Path_TBD_Bridge, Length(partOIteration3Paths.Path_TBD_Bridge), Ticks(partOIteration3Paths.Path_TBD_Bridge)));
            partOIteration3Record.Add(new PartOIteration3FileRecord(PartOIteration3Roles.Bridge_TSD, partOIteration3Paths.Path_TSD_Bridge, Length(partOIteration3Paths.Path_TSD_Bridge), Ticks(partOIteration3Paths.Path_TSD_Bridge)));
            partOIteration3Record.Add(new PartOIteration3FileRecord(PartOIteration3Roles.Systems_TPD, partOIteration3Paths.Path_TPD, Length(partOIteration3Paths.Path_TPD), Ticks(partOIteration3Paths.Path_TPD)));

            partOIteration3Ledger.Complete(
                PartOIteration3Stage.ResultantTemperature,
                string.Format(
                    "{0} room(s) carry a complete finite ResultantTemperature series over hours {1}..{2}, obtained by: {3}",
                    resultantTemperatureResults.Results.Count,
                    resultantTemperatureResults.StartHour,
                    resultantTemperatureResults.EndHour,
                    resultantTemperatureResults.Method ?? "an unnamed method"),
                artifacts_Bridge);

            //=================================================================================================
            //Candidate B TM59 - the SAME unchanged authority, over the bridge results
            //=================================================================================================
            if (!Starting(PartOIteration3Stage.CandidateBTM59, true))
            {
                return Result(partOIteration3Ledger, partOIteration3Record, null, null, null, partOIteration3Paths, notes);
            }

            List<Guid> guids_Space_Bound = [];
            foreach (SystemVentilationBinding systemVentilationBinding in systemVentilationBindings)
            {
                guids_Space_Bound.Add(systemVentilationBinding.Guid_Space);
            }

            guids_Space_Bound.Sort();

            PartOIteration3Assessment partOIteration3Assessment_B = iPartOIteration3Pipeline.Assess(analyticalModel_CandidateB, partOIteration3Paths.Path_TSD_Bridge, overheatingScenarios, guids_Space_Bound);

            if (partOIteration3Assessment_B is null || !partOIteration3Assessment_B.IsAssessed)
            {
                partOIteration3Ledger.Refuse(
                    PartOIteration3Stage.CandidateBTM59,
                    "Candidate B could not be assessed.",
                    [partOIteration3Assessment_B?.Refusal ?? string.Format("The Candidate B results at '{0}' could not be assessed.", partOIteration3Paths.Path_TSD_Bridge)]);

                return Result(partOIteration3Ledger, partOIteration3Record, null, null, null, partOIteration3Paths, notes);
            }

            //The provider's own series and the values TM59 then read out of the SAME file must be the same
            //numbers. They are produced by two independent readers of one result file, and if they ever
            //disagree then one of them is resolving a room to the wrong zone - which would be invisible in
            //every other check, because both answers are complete, finite and plausible.
            List<string> refusals_Identity = Query.PartOIteration3ProviderIdentityRefusals(resultantTemperatureResults, partOIteration3Assessment_B, guids_Space_Bound, dictionary_Room, out long count_IdentityValues);

            if (refusals_Identity.Count != 0)
            {
                partOIteration3Ledger.Refuse(PartOIteration3Stage.CandidateBTM59, "The resultant temperatures the provider produced and the ones TM59 read back are not the same numbers.", refusals_Identity);

                return Result(partOIteration3Ledger, partOIteration3Record, null, null, null, partOIteration3Paths, notes);
            }

            partOIteration3Record.Status_CandidateB = partOIteration3Assessment_B.OccupiedSpaceComplianceStatus;
            partOIteration3Record.ProviderMatchesResultFile = true;
            partOIteration3Record.Count_ProviderIdentityValues = count_IdentityValues;

            partOIteration3Ledger.Complete(
                PartOIteration3Stage.CandidateBTM59,
                string.Format(
                    "The existing TM59 authority assessed Candidate B over {0} space(s): {1}. The provider's series and the {2} value(s) TM59 read from the same file are identical.",
                    partOIteration3Assessment_B.Count_Processed,
                    Core.Query.Description(partOIteration3Assessment_B.OccupiedSpaceComplianceStatus),
                    count_IdentityValues));

            //=================================================================================================
            //Reconciliation - guid only, fail closed
            //=================================================================================================
            Starting(PartOIteration3Stage.Reconciliation);

            List<string> refusals_Reconciliation = Query.PartOIteration3ReconciliationRefusals(
                adjacencyCluster_Prepared,
                partOIteration3SystemScope,
                systemVentilationRoute,
                noIzamThermalSource,
                partOIteration3Assessment_A,
                partOIteration3Assessment_B,
                dictionary_Room,
                out List<PartOIteration3Room> rooms_Comparable,
                out List<PartOIteration3CriterionComparison> criteria,
                out List<string> notes_Reconciliation);

            AddNotes(notes, notes_Reconciliation);

            if (refusals_Reconciliation.Count != 0)
            {
                partOIteration3Ledger.Refuse(PartOIteration3Stage.Reconciliation, "Reference A and Candidate B did not reconcile.", refusals_Reconciliation);

                return Result(partOIteration3Ledger, partOIteration3Record, null, null, null, partOIteration3Paths, notes);
            }

            foreach (SystemVentilationBinding systemVentilationBinding in systemVentilationBindings)
            {
                dictionary_Room.TryGetValue(systemVentilationBinding.Guid_Space, out PartOIteration3Room partOIteration3Room);

                partOIteration3Record.Add(new PartOIteration3BindingRecord(
                    systemVentilationBinding.Guid_Space,
                    partOIteration3Room?.Name_Space,
                    partOIteration3Room?.Guid_Dwelling ?? Guid.Empty,
                    partOIteration3Room?.Name_Dwelling,
                    systemVentilationBinding.Guid_SystemSpace,
                    systemVentilationBinding.Guid_AirSystem,
                    systemVentilationBinding.Reference_SystemZone,
                    systemVentilationBinding.DesignFlowRate_Supply_Lps,
                    systemVentilationBinding.DesignFlowRate_Extract_Lps));
            }

            partOIteration3Ledger.Complete(
                PartOIteration3Stage.Reconciliation,
                string.Format(
                    "{0} room(s) reconcile by identity between Reference A and Candidate B: same rooms, same TM59 criteria, same design airflows and the same transfer topology.",
                    rooms_Comparable.Count));

            //=================================================================================================
            //Comparison - descriptive statistics, no parity threshold
            //=================================================================================================
            Starting(PartOIteration3Stage.Comparison);

            PartOIteration3Comparison partOIteration3Comparison = PartOIteration3Comparison.Create(
                rooms_Comparable,
                partOIteration3Assessment_A.ResultantTemperatures,
                partOIteration3Assessment_B.ResultantTemperatures,
                criteria,
                out List<string> refusals_Comparison);

            if (partOIteration3Comparison is null)
            {
                partOIteration3Ledger.Refuse(PartOIteration3Stage.Comparison, "The A/B comparison could not be computed.", refusals_Comparison);

                return Result(partOIteration3Ledger, partOIteration3Record, null, null, null, partOIteration3Paths, notes);
            }

            partOIteration3Ledger.Complete(
                PartOIteration3Stage.Comparison,
                string.Format("{0}. {1} TM59 criterion outcome(s) differ between the two routes.", partOIteration3Comparison.Statistics, partOIteration3Comparison.Count_Changed));

            //=================================================================================================
            //Persistence
            //=================================================================================================
            Starting(PartOIteration3Stage.Persistence);

            List<string> refusals_Persistence = [];
            List<string> artifacts_Persistence = [];

            //Candidate B's own reopenable model, provenanced to the BRIDGE results - the file its
            //resultant temperatures were actually read from - and stamped with the same overheating
            //scenarios Reference A was assessed under, which is what makes a reopened Candidate B
            //reviewable against the same criteria.
            analyticalModel_CandidateB.SetValue(Analytical.AnalyticalModelParameter.OverheatingScenarios, new SAMCollection<OverheatingScenario>(overheatingScenarios));
            analyticalModel_CandidateB.SetValue(Analytical.AnalyticalModelParameter.SimulationResultProvenance, new SimulationResultProvenance(analyticalModel_CandidateB, partOIteration3Paths.Path_TSD_Bridge));

            if (!iPartOIteration3Pipeline.Persist(analyticalModel_CandidateB, partOIteration3Paths.Path_TSD_Bridge, partOIteration3Paths.Path_TBD_ThermalSource, out string note_Persist))
            {
                refusals_Persistence.Add(note_Persist ?? string.Format("Candidate B's analytical model could not be written to '{0}', so this pairing could not be reopened.", partOIteration3Paths.Path_Model_CandidateB));
            }
            else
            {
                AddNotes(notes, [note_Persist]);

                if (partOIteration3Artifacts.TryClaim(partOIteration3Paths.Path_Model_CandidateB, out string artifact_Model, out string refusal_Model))
                {
                    artifacts_Persistence.Add(artifact_Model);

                    partOIteration3Record.Add(new PartOIteration3FileRecord(PartOIteration3Roles.CandidateB_Model, partOIteration3Paths.Path_Model_CandidateB, Length(partOIteration3Paths.Path_Model_CandidateB), Ticks(partOIteration3Paths.Path_Model_CandidateB)));
                }
                else
                {
                    refusals_Persistence.Add(refusal_Model);
                }
            }

            //The two TM59 reports. Each is recorded - and offered - only where THIS attempt demonstrably
            //wrote it: the assessment names a path only when its save succeeded, and the fingerprint taken
            //at attempt start proves the file there now is not one an earlier assessment left. A report is
            //evidence of an assessment rather than an input to the pairing, so a missing one is said and
            //does not refuse. Reference A's sits beside A's own results and is recorded as a report, never
            //claimed as an artifact of this run.
            RecordReport(partOIteration3Artifacts, partOIteration3Record, PartOIteration3Roles.ReferenceA_TM59Report, partOIteration3Assessment_A, null, notes);
            RecordReport(partOIteration3Artifacts, partOIteration3Record, PartOIteration3Roles.CandidateB_TM59Report, partOIteration3Assessment_B, artifacts_Persistence, notes);

            if (refusals_Persistence.Count != 0)
            {
                partOIteration3Ledger.Refuse(PartOIteration3Stage.Persistence, "This pairing could not be made reopenable.", refusals_Persistence, artifacts_Persistence);

                return Result(partOIteration3Ledger, partOIteration3Record, null, partOIteration3Assessment_A, partOIteration3Assessment_B, partOIteration3Paths, notes);
            }

            partOIteration3Ledger.Complete(
                PartOIteration3Stage.Persistence,
                string.Format("Candidate B is reopenable at '{0}', provenanced to '{1}', and the pairing record is at '{2}'.", partOIteration3Paths.Path_Model_CandidateB, partOIteration3Paths.Path_TSD_Bridge, partOIteration3Paths.Path_Record),
                artifacts_Persistence);

            return Result(partOIteration3Ledger, partOIteration3Record, partOIteration3Comparison, partOIteration3Assessment_A, partOIteration3Assessment_B, partOIteration3Paths, notes);
        }

        /// <summary>
        /// Adopts the ledger into the record, writes the record, and answers the result.
        ///
        /// <para><b>The record is written on every path, including every refusal</b></para>
        /// <para>
        /// A refused pairing's ledger is its diagnosis, and a person who reopens the model tomorrow needs
        /// it as much as a completed one - more, in fact. So this is the single exit and every refusal
        /// above returns through it.
        /// </para>
        /// <para>
        /// <b>The record file is not an artifact of the Persistence stage.</b> It cannot be: the stage's
        /// own row has to be inside the record, so the record is serialized after the last stage is
        /// recorded. It is reported as the pairing's location instead, which is what a reader actually
        /// needs from it.
        /// </para>
        /// </summary>
        private static PartOIteration3Result Result(
            PartOIteration3Ledger partOIteration3Ledger,
            PartOIteration3Record partOIteration3Record,
            PartOIteration3Comparison partOIteration3Comparison,
            PartOIteration3Assessment partOIteration3Assessment_A,
            PartOIteration3Assessment partOIteration3Assessment_B,
            PartOIteration3Paths partOIteration3Paths,
            List<string> notes)
        {
            partOIteration3Record.Adopt(partOIteration3Ledger);

            string path_Record = partOIteration3Paths?.Path_Record;

            if (!string.IsNullOrWhiteSpace(path_Record))
            {
                try
                {
                    File.WriteAllText(path_Record, partOIteration3Record.ToString());
                }
                catch (Exception exception)
                {
                    //Noted, never fatal. The comparison in front of the user is already correct; what is
                    //lost is the ability to reopen it, and saying so is more use than failing a run that
                    //succeeded.
                    notes.Add(string.Format("The Iteration 3 pairing record could not be written to '{0}', so this pairing cannot be reopened in a later session. ({1})", path_Record, exception.Message));
                }
            }

            //The reports offered are the ones the record holds, and the record holds only reports this
            //attempt wrote - see RecordReport.
            PartOIteration3Result partOIteration3Result = new(
                partOIteration3Ledger,
                partOIteration3Record,
                partOIteration3Comparison,
                partOIteration3Assessment_A,
                partOIteration3Assessment_B,
                partOIteration3Record.File(PartOIteration3Roles.ReferenceA_TM59Report)?.Path,
                partOIteration3Record.File(PartOIteration3Roles.CandidateB_TM59Report)?.Path,
                path_Record,
                false,
                notes);

            //The A/B review report, beside the record. Writes only where the pairing completed, so a
            //refused attempt cannot replace the last report that described a real comparison.
            SavePartOIteration3Report(partOIteration3Result);

            return partOIteration3Result;
        }

        /// <summary>
        /// Records one assessment's TM59 report as part of the pairing where this attempt wrote it, and
        /// says why not where it did not.
        /// </summary>
        /// <param name="artifacts">
        /// Where a claimed artifact is listed - or null for a report this attempt rewrote but does not own,
        /// which is checked by the same rule without being claimed.
        /// </param>
        private static void RecordReport(
            PartOIteration3Artifacts partOIteration3Artifacts,
            PartOIteration3Record partOIteration3Record,
            string role,
            PartOIteration3Assessment partOIteration3Assessment,
            List<string> artifacts,
            List<string> notes)
        {
            string path = partOIteration3Assessment?.Path_Report;

            if (string.IsNullOrWhiteSpace(path))
            {
                notes.Add(string.Format("No {0} was written by this attempt, so none is recorded for this pairing. {1}", role, partOIteration3Assessment?.Refusal_Report ?? "The assessment named no report."));

                return;
            }

            string refusal;
            bool written;

            if (artifacts is null)
            {
                written = partOIteration3Artifacts.IsWritten(path, out refusal);
            }
            else
            {
                written = partOIteration3Artifacts.TryClaim(path, out string artifact, out refusal);

                if (written)
                {
                    artifacts.Add(artifact);
                }
            }

            if (!written)
            {
                notes.Add(string.Format("The {0} at '{1}' is not recorded for this pairing, because this attempt cannot show that it wrote it. {2}", role, path, refusal));

                return;
            }

            partOIteration3Record.Add(new PartOIteration3FileRecord(role, path, Length(path), Ticks(path)));
        }

        private static void Delete(string path, string description, List<string> refusals, List<string> notes)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return;
            }

            try
            {
                File.Delete(path);

                notes.Add(string.Format("Deleted {0} at '{1}' so a failed attempt cannot leave a reopenable Candidate B behind.", description, path));
            }
            catch (Exception exception)
            {
                refusals.Add(string.Format(
                    "{0} at '{1}' could not be deleted, so an earlier attempt's Candidate B would remain reopenable beside this one's. ({2})",
                    char.ToUpperInvariant(description[0]) + description.Substring(1),
                    path,
                    exception.Message));
            }
        }

        /// <summary>Whatever of the stated paths this attempt can honestly claim. Refusals are ignored: this is the FAILURE path, where the question is only what to show.</summary>
        private static List<string> Claim(PartOIteration3Artifacts partOIteration3Artifacts, IEnumerable<string> paths)
        {
            partOIteration3Artifacts.TryClaim(paths, out List<string> artifacts, out List<string> _);

            return artifacts;
        }

        private static long Length(string path)
        {
            return PartOIteration3Artifacts.TryRead(path, out long length, out long _) ? length : -1;
        }

        private static long Ticks(string path)
        {
            return PartOIteration3Artifacts.TryRead(path, out long _, out long ticks) ? ticks : -1;
        }

        private static void AddNotes(List<string> notes, IEnumerable<string> additions)
        {
            foreach (string addition in additions ?? [])
            {
                if (!string.IsNullOrWhiteSpace(addition))
                {
                    notes.Add(addition);
                }
            }
        }
    }
}
