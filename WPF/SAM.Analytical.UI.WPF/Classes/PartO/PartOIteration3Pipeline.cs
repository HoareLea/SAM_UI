// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Systems;
using SAM.Analytical.Tas;
using SAM.Analytical.Tas.TPD;
using SAM.Core.Systems;
using SAM.Core.Tas;
using System;
using System.Collections.Generic;
using System.Threading;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// The production <see cref="IPartOIteration3Pipeline"/>: each member is one call into the authority
    /// that owns that work, and nothing else.
    ///
    /// <para><b>What is deliberately not here</b></para>
    /// <para>
    /// No airflow is computed, balanced or substituted; no equipment is selected or capacity read as an
    /// airflow; no IZAM decision is taken; no TM59 criterion is evaluated. The parity configuration this
    /// run needs - continuous operation at a factor of 1.0 - arrives as a schedule on the materialisation
    /// settings, and everything else about the topology is the shipped template's, which is read and never
    /// written.
    /// </para>
    /// <para>
    /// <b>PR5A (SAM#111 plan §J) - the one thing that is here.</b> <see cref="Materialise"/> chooses
    /// between the shipped <c>MV.json</c> (no unit settings - the B0 control) and <c>MVRE.json</c>
    /// (manufacturer-aware unit settings resolved) purely on whether it was handed any - it does not
    /// resolve a product, read a selection, or compute a certified figure itself; that resolution is
    /// <c>Query.PartOIteration3EquipmentResolution</c>'s, called by the orchestrator before this pipeline
    /// is reached. This class only carries the resolved numbers the rest of the way, exactly as it already
    /// carries the schedule.
    /// </para>
    /// </summary>
    public class PartOIteration3Pipeline : IPartOIteration3Pipeline
    {
        /// <summary>
        /// The ventilation identity of the shipped topology template the Part O route materialises onto.
        /// <para>
        /// Resolved through SAM_Systems' own capability index rather than by composing a path, so the
        /// installed template is found the same way every other consumer finds it, and a template that
        /// is missing or named twice in the index refuses instead of resolving to a file nobody chose.
        /// </para>
        /// </summary>
        public const string Ventilation_Template = "MV";

        /// <summary>The name given to the materialised energy centre.</summary>
        public const string Name_SystemEnergyCentre = "Part O Iteration 3 mechanical ventilation";

        /// <summary>
        /// PR1's materialisation over the shipped <c>MV.json</c>, with the frozen parity operating
        /// schedule.
        /// <para>
        /// <b>The three settings, and why each is what it is.</b> The schedule is the exact 8760 x 1.0
        /// constant the architecture fixes for the initial parity configuration - see
        /// <see cref="Query.PartOIteration3OperatingSchedule"/>, including why its name is preserved
        /// letter for letter. The name is this run's, so the graph says what produced it. In-room
        /// components are <b>off</b>: Candidate B models air distribution, and radiators, fan coils and
        /// chilled beams copied from the template prototype would be plant this comparison never asked
        /// for and did not exist in Reference A.
        /// </para>
        /// </summary>
        /// <summary>
        /// PR5A (SAM#111 plan §J): the topology template a manufacturer-aware unit settings dictionary
        /// materialises onto - the existing shipped MVHR topology, per <c>CapabilityIndex.JSON</c>. Read
        /// only, exactly as <see cref="Ventilation_Template"/> is; <see cref="Materialise"/> chooses between
        /// the two solely on whether <paramref name="unitSettings"/> is non-empty, never on a name a caller
        /// passes in.
        /// </summary>
        public const string Ventilation_Template_ManufacturerAware = "MVRE";

        /// <summary>
        /// PR5B (SAM#111): <paramref name="coolingSettings"/> - each scoped unit's resolved cooling module - is
        /// carried to SAM_Systems verbatim and never chooses the topology template: B4 is B0's own
        /// <c>MV.json</c> ventilation plus the cooling branch, so the template still follows
        /// <paramref name="unitSettings"/> alone.
        /// </summary>
        public MechanicalVentilationMaterialisation Materialise(AdjacencyCluster adjacencyCluster, IEnumerable<Space> spaces, IReadOnlyDictionary<Guid, MechanicalVentilationUnitSettings> unitSettings = null, IReadOnlyDictionary<Guid, MechanicalVentilationCoolingSettings> coolingSettings = null, IReadOnlyDictionary<Guid, MechanicalVentilationGuidanceSettings> guidanceSettings = null)
        {
            bool hasUnitSettings = unitSettings is not null && unitSettings.Count != 0;
            bool hasGuidanceSettings = guidanceSettings is not null && guidanceSettings.Count != 0;

            //SAM#123: a manufacturer-guidance unit is the product's own arrangement, which has an exchanger.
            string ventilationTemplate = hasUnitSettings || hasGuidanceSettings ? Ventilation_Template_ManufacturerAware : Ventilation_Template;

            SystemEnergyCentre systemEnergyCentre = new SystemTemplate(ventilationTemplate, null, null, null, null, null).SystemEnergyCentre();

            if (systemEnergyCentre is null)
            {
                return new MechanicalVentilationMaterialisation(
                    null,
                    [string.Format("The installed mechanical ventilation topology template '{0}' could not be resolved, so there is nothing to materialise the explicit Part O ventilation onto.", ventilationTemplate)],
                    null,
                    null);
            }

            MechanicalVentilationSettings mechanicalVentilationSettings = new()
            {
                Schedule = Query.PartOIteration3OperatingSchedule(),
                Name = Name_SystemEnergyCentre,
                MaterialiseSystemSpaceComponents = false,
                UnitSettings = unitSettings,
                CoolingSettings = coolingSettings,
                GuidanceSettings = guidanceSettings,
            };

            return adjacencyCluster.MechanicalVentilation(systemEnergyCentre, mechanicalVentilationSettings, spaces);
        }

        /// <summary>
        /// Mixed Part O (PR3C, SAM PR3B record §2): ONE SAM_Systems graph for a mixed model - every unit the ordinary
        /// <c>MV.json</c> ventilation, and only the units named in <paramref name="guidanceSettings"/> the product's own
        /// manufacturer-guidance arrangement (MVRE exchanger + supply DX coil, SAM_Systems' <c>GuidanceTemplate</c>).
        /// The same schedule, name and flags as <see cref="Materialise"/>; nothing computed here - each unit's guidance
        /// settings are SAM_Systems' resolution of SAM's cooling rule, handed in.
        /// </summary>
        public MechanicalVentilationMaterialisation MaterialiseMixed(AdjacencyCluster adjacencyCluster, IEnumerable<Space> spaces, IReadOnlyDictionary<Guid, MechanicalVentilationGuidanceSettings> guidanceSettings)
        {
            SystemEnergyCentre systemEnergyCentre = new SystemTemplate(Ventilation_Template, null, null, null, null, null).SystemEnergyCentre();
            SystemEnergyCentre systemEnergyCentre_Guidance = new SystemTemplate(Ventilation_Template_ManufacturerAware, null, null, null, null, null).SystemEnergyCentre();

            if (systemEnergyCentre is null || systemEnergyCentre_Guidance is null)
            {
                return new MechanicalVentilationMaterialisation(
                    null,
                    [string.Format("The installed mechanical ventilation topology template '{0}' could not be resolved, so there is nothing to materialise the mixed Part O ventilation onto.", systemEnergyCentre is null ? Ventilation_Template : Ventilation_Template_ManufacturerAware)],
                    null,
                    null);
            }

            MechanicalVentilationSettings mechanicalVentilationSettings = new()
            {
                Schedule = Query.PartOIteration3OperatingSchedule(),
                Name = Name_SystemEnergyCentre,
                MaterialiseSystemSpaceComponents = false,
                GuidanceTemplate = systemEnergyCentre_Guidance,
                GuidanceSettings = guidanceSettings,
            };

            return adjacencyCluster.MechanicalVentilation(systemEnergyCentre, mechanicalVentilationSettings, spaces);
        }

        /// <summary>
        /// Candidate B's thermal source, produced by <b>the same pipeline that produced Reference A</b>
        /// with only its last step changed - see <see cref="PartOWorkflowRunner"/>.
        ///
        /// <para><b>Two arguments carry the whole safety of this call</b></para>
        /// <para>
        /// <c>partORun</c> is <b>null</b>, deliberately. A run handed in would be armed with
        /// <c>ExpectResults</c>, stamped with Reference A's overheating scenarios and provenance, and
        /// persisted as a reopenable <c>.sam</c> - so Candidate B's no-IZAM results would be recorded as
        /// though they were a completed Part O run of their own. They are not: they are the load source
        /// for a ventilation network that has not been built yet. Candidate B gets its provenance later,
        /// against the bridge results, once there is something to attribute.
        /// </para>
        /// <para>
        /// <c>partOCanonicalTBD</c> is null too: a warm start would reuse Reference A's converted TBD, and
        /// the whole point of this run is that the building is <b>not</b> the same one - the no-IZAM
        /// workflow has to build it.
        /// </para>
        /// </summary>
        public NoIzamThermalSource ThermalSource(
            AnalyticalModel analyticalModel_Prepared,
            PartOSimulationContext partOSimulationContext,
            string projectName,
            CancellationToken cancellationToken,
            out AnalyticalModel analyticalModel_Source,
            out bool cancelled,
            out List<string> notes,
            out string refusal)
        {
            NoIzamThermalSource noIzamThermalSource = null;

            //The seam. Everything above this - the deep working copy, the material repair, the
            //construction layers, the gbXML, the solar calculation, the design days, the day range - is
            //Reference A's own pipeline, unchanged and shared.
            AnalyticalModel model = Modify.RunPartOSimulation(
                analyticalModel_Prepared,
                partOSimulationContext,
                projectName,
                null,
                cancellationToken,
                out string _,
                out string _,
                out cancelled,
                out bool _,
                out notes,
                out refusal,
                null,
                (AnalyticalModel analyticalModel, WorkflowSettings workflowSettings, CancellationToken token, out bool cancelled_Workflow) =>
                {
                    cancelled_Workflow = false;

                    //SAM_Tas' authority, and the one place both cleanups are forced on and the run is
                    //evidenced. The settings it is given are the ones Reference A's own pipeline composed.
                    noIzamThermalSource = Analytical.Tas.Create.NoIzamThermalSource(analyticalModel, workflowSettings, out AnalyticalModel analyticalModel_Result);

                    return analyticalModel_Result;
                });

            analyticalModel_Source = model;

            //A refusal before the workflow ran leaves no source at all; say so as a source rather than as
            //a null, so the caller has one shape to read.
            return noIzamThermalSource ?? new NoIzamThermalSource(
                null,
                null,
                false,
                false,
                null,
                null,
                [refusal ?? (cancelled ? "The Candidate B thermal source run was cancelled." : "The Candidate B thermal source did not run, and nothing said why.")],
                notes);
        }

        /// <summary>PR2's route: convert, simulate the air systems, read every room's ZoneTemperature.</summary>
        public SystemVentilationRoute Route(
            NoIzamThermalSource noIzamThermalSource,
            MechanicalVentilationMaterialisation mechanicalVentilationMaterialisation,
            string path_TPD,
            int startHour,
            int endHour,
            SystemVentilationFanHeatGainPolicy fanHeatGainPolicy = SystemVentilationFanHeatGainPolicy.ClearToZero)
        {
            return Analytical.Tas.TPD.Create.SystemVentilationRoute(noIzamThermalSource, mechanicalVentilationMaterialisation, path_TPD, startHour, endHour, fanHeatGainPolicy);
        }

        /// <summary>
        /// PR3's replaceable provider. Constructed here and held nowhere, so the day TAS Systems answers
        /// a native resultant temperature this line changes and nothing else does.
        /// </summary>
        public ResultantTemperatureResults ResultantTemperatures(SystemVentilationRoute systemVentilationRoute, string path_TBD_Bridge)
        {
            IResultantTemperatureProvider iResultantTemperatureProvider = new ThermostatBridgeResultantTemperatureProvider(path_TBD_Bridge);

            return iResultantTemperatureProvider.ResultantTemperatureResults(systemVentilationRoute);
        }

        /// <summary>
        /// The unchanged production TM59 assessment, with capture on, and its report written beside the
        /// results it assessed - exactly as <c>Modify.AssessPartOTM59</c> does for an ordinary review.
        /// </summary>
        public PartOIteration3Assessment Assess(
            AnalyticalModel analyticalModel_Workflow,
            string path_TSD,
            IEnumerable<OverheatingScenario> overheatingScenarios,
            IEnumerable<Guid> spaceGuids_Capture)
        {
            PartOTM59Assessment partOTM59Assessment = PartOTM59Assessment.Assess(analyticalModel_Workflow, path_TSD, overheatingScenarios, true, spaceGuids_Capture);

            if (!partOTM59Assessment.IsAssessed)
            {
                return new PartOIteration3Assessment(
                    false,
                    partOTM59Assessment.Refusal ?? string.Format("The simulation results at '{0}' could not be assessed.", path_TSD),
                    TM59ComplianceStatus.Undefined,
                    null,
                    partOTM59Assessment.AssociationRefusals,
                    null,
                    partOTM59Assessment.SpaceGuids_Unassessed,
                    null,
                    null,
                    null,
                    0);
            }

            //The durable artifact, written whether the pairing goes on to complete or not. A failure to
            //write is carried as NO report path plus the writer's reason, and never fails the assessment,
            //which is already done.
            string path_TM59Report = Report(path_TSD, partOTM59Assessment.Report, out string refusal_Report);

            return new PartOIteration3Assessment(
                true,
                null,
                partOTM59Assessment.OccupiedSpaceComplianceStatus,
                partOTM59Assessment.SpaceResults,
                partOTM59Assessment.AssociationRefusals,
                partOTM59Assessment.Result?.VentilationStrategyRefusals,
                partOTM59Assessment.SpaceGuids_Unassessed,
                partOTM59Assessment.ResultantTemperatures,
                partOTM59Assessment.Report.ToString(),
                path_TM59Report,
                partOTM59Assessment.Result?.Spaces?.Count ?? 0,
                refusal_Report,
                partOTM59Assessment.SpaceGuids_InformationOnly);
        }

        /// <summary>
        /// Writes one assessment's TM59 report beside its results and answers where - or <b>null</b>, with
        /// the writer's reason, where it could not.
        /// <para>
        /// <c>Modify.SavePartOTM59Report</c> states the path it would have written even when the write
        /// fails, which its own callers read together with its answer. Handing that path on regardless
        /// would name whatever earlier report is still sitting there as this assessment's.
        /// </para>
        /// </summary>
        internal static string Report(string path_TSD, TM59AssessmentReport tM59AssessmentReport, out string refusal)
        {
            return Modify.SavePartOTM59Report(path_TSD, tM59AssessmentReport, out string path_TM59Report, out refusal) ? path_TM59Report : null;
        }

        /// <summary>
        /// Candidate B's reopenable model, through the one writer a Part O run already uses - so it lands
        /// beside its own results, at the path the single naming authority states, and reopens through
        /// the ordinary Open path.
        /// </summary>
        public bool Persist(AnalyticalModel analyticalModel, string path_TSD, string path_TBD, out string note)
        {
            return Modify.PersistPartORunModel(analyticalModel, path_TSD, path_TBD, out note);
        }
    }
}
