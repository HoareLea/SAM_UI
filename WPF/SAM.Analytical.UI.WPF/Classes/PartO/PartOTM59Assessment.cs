// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Tas;
using SAM.Weather;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// One production CIBSE TM59 assessment of one completed Part O run, with every result resolved back to
    /// the <b>design</b> space it belongs to.
    ///
    /// <para><b>Why this exists as a class</b></para>
    /// <para>
    /// The assessment sequence - convert the TSD, build the calculator, map the scenarios, restore the
    /// design internal conditions, calculate, report - has two callers now: the command that shows an
    /// engineer the report, and the Iteration 2B optimisation that reads the verdicts to decide what to
    /// target next. Writing it twice would give the two a way to disagree about what the assessment said,
    /// which is the one thing neither may do.
    /// </para>
    ///
    /// <para><b>The assessment itself is entirely SAM's</b></para>
    /// <para>
    /// This performs the same sequence the accepted <c>Tas.TSDQueryTM59Results</c> component performs and
    /// computes no criterion, limit or verdict of its own. <see cref="Report"/> is the production
    /// <c>TM59AssessmentReport</c>, and every <see cref="SpaceResults"/> row carries that report's own
    /// <c>ComplianceStatus</c> verbatim - never a status re-derived from Actual and Limit, which would
    /// overrule the calculation for any room sitting exactly on its limit.
    /// </para>
    ///
    /// <para><b>Simulated results, design identities</b></para>
    /// <para>
    /// A TM59 result is produced for a <i>simulated</i> space, and an optimisation has to move a design
    /// terminal, which belongs to a <i>design</i> space. The translation is <c>SimulationSpaceMap</c>'s and
    /// it is by identity: a result whose simulated space does not resolve to exactly one design space is
    /// reported in <see cref="AssociationRefusals"/> and left out, never matched to a same-named room in
    /// another flat. That refusal is what stops an optimisation raising Flat 3's kitchen because Flat 2's
    /// failed.
    /// </para>
    ///
    /// <para><b>The model assessed is the workflow's</b></para>
    /// <para>
    /// The design side of the calculation is the model the TAS workflow <b>returned</b>. Only that one
    /// carries the current TAS zone identities the map matches on; a preparation output can still hold
    /// guids from an earlier round trip, which produces an incomplete map and a silent empty answer. This
    /// class takes the model it is given and never reads one back off disk, so the caller's own
    /// <c>PartORun.AnalyticalModel_Assessment</c> discipline is what governs.
    /// </para>
    /// </summary>
    public class PartOTM59Assessment
    {
        //Internal rather than private so tests can fabricate the assessment the subset-pass guard reads -
        //the production route to one remains Assess, which needs a real TSD.
        internal PartOTM59Assessment(TM59AssessmentResult tM59AssessmentResult, TM59AssessmentReport tM59AssessmentReport, List<PartOTM59SpaceResult> spaceResults, List<string> associationRefusals, List<Guid> spaceGuids_Unassessed, string refusal, Dictionary<Guid, double[]>? resultantTemperatures = null, List<Guid>? spaceGuids_NoResult = null, Dictionary<Guid, TM59ComplianceStatus>? occupiedSpaceStatuses = null, List<Guid>? spaceGuids_InformationOnly = null)
        {
            SpaceGuids_InformationOnly = spaceGuids_InformationOnly ?? [];
            OccupiedSpaceStatuses = occupiedSpaceStatuses ?? [];
            Result = tM59AssessmentResult;
            Report = tM59AssessmentReport;
            SpaceResults = spaceResults ?? [];
            AssociationRefusals = associationRefusals ?? [];
            SpaceGuids_Unassessed = spaceGuids_Unassessed ?? [];
            SpaceGuids_NoResult = spaceGuids_NoResult ?? [];
            Refusal = refusal;
            ResultantTemperatures = resultantTemperatures;
        }

        /// <summary>
        /// The <b>design</b> spaces this assessment produced no TM59 result of any kind for: those in
        /// <see cref="SpaceGuids_Unassessed"/>, plus those that did resolve and were calculated but reached no
        /// criterion - the corridor no overheating scenario covers, for instance, which the report lists under
        /// "Spaces not assessed".
        /// <para>
        /// <b>Presentation's count, read off the same result.</b> It is what the result window's "not
        /// assessed" figure is, so that figure is a count of design spaces taken from the calculation's own
        /// results rather than of the prose sentences explaining them (one space can have two). It decides
        /// nothing: <see cref="SpaceGuids_Unassessed"/> is still what a pass is guarded on.
        /// </para>
        /// </summary>
        public List<Guid> SpaceGuids_NoResult { get; }

        /// <summary>
        /// The <b>design</b> spaces the report shows only under "Supplementary &gt;28 C checks - information only":
        /// a bathroom or ensuite given the &gt;28 C calculation, which is advisory and is not an occupied-space
        /// criterion. They are not in <see cref="SpaceResults"/>, which carries criteria only, and they are not
        /// "no result" either - so a caller comparing two assessments room by room can tell a consistently
        /// information-only room from one that was assessed on one side and not the other.
        /// </summary>
        public List<Guid> SpaceGuids_InformationOnly { get; }

        /// <summary>
        /// The hourly resultant temperature series this assessment actually read, keyed by <b>design</b>
        /// space guid - or null where the caller did not ask for them.
        ///
        /// <para><b>Off by default, and scoped when on</b></para>
        /// <para>
        /// Nothing needs these to get a TM59 verdict; they exist so an Approved Document O Iteration 3
        /// A/B comparison can state how far apart two thermal routes are <b>using the very numbers the two
        /// assessments were computed from</b>, rather than re-reading the results file through a second
        /// path that could resolve a room differently. So every existing caller gets null and pays nothing.
        /// </para>
        /// <para>
        /// <b>Why scoping matters rather than being a nicety.</b> A project of five thousand spaces with a
        /// full annual series is forty-three million doubles - about 350 MB - per case, and a comparison
        /// holds two. Only the rooms the caller names are captured, which for Iteration 3 is the set bound
        /// to the explicit ventilation route and nothing else.
        /// </para>
        /// <para>
        /// <b>Keyed through <c>SimulationSpaceMap</c>, exactly as every result on this class is.</b> A
        /// simulated space that does not resolve to exactly one design space contributes no series, for
        /// the same reason it contributes no verdict.
        /// </para>
        /// </summary>
        public Dictionary<Guid, double[]>? ResultantTemperatures { get; }

        /// <summary>
        /// Each assessed occupied space's <b>overall</b> TM59 status, keyed by <b>design</b> space guid - SAM's own
        /// <c>TM59AssessmentReportSpace.ComplianceStatus</c>, the value the report's Overall column prints, resolved
        /// through the same <c>SimulationSpaceMap</c> every other result on this class is.
        /// <para>
        /// <b>Read, never decided.</b> It exists so a mixed Part O design can tally a dwelling from the statuses
        /// SAM gave its rooms (<c>Query.PartODwellingResults</c>) instead of re-combining the per-criterion rows. A
        /// space that does not resolve to exactly one design space has no entry, as it has no verdict.
        /// </para>
        /// </summary>
        public Dictionary<Guid, TM59ComplianceStatus> OccupiedSpaceStatuses { get; }

        /// <summary>The production assessment result, or null where none could be produced.</summary>
        public TM59AssessmentResult? Result { get; }

        /// <summary>The production report - the text an engineer reads, and the checks a policy reads.</summary>
        public TM59AssessmentReport? Report { get; }

        /// <summary>
        /// Every occupied-space criterion outcome, resolved to its design space. Natural- and
        /// mechanical-ventilation criteria both, distinguished by
        /// <see cref="PartOTM59SpaceResult.Mechanical"/> - a policy that may only optimise mechanical
        /// airflow has to be able to see that a natural failure exists without being able to mistake it for
        /// one it can act on.
        /// <para>
        /// Corridor and supplementary &gt;28 °C rows are deliberately not here. They are reported as a risk
        /// rather than as an occupied-space compliance verdict, and folding them in would state a regulatory
        /// failure TM59 does not make.
        /// </para>
        /// </summary>
        public List<PartOTM59SpaceResult> SpaceResults { get; }

        /// <summary>Spaces that could not be resolved between the simulation and the design, one sentence each.</summary>
        public List<string> AssociationRefusals { get; }

        /// <summary>
        /// The <b>design</b> spaces this assessment produced no result for - those whose simulated
        /// counterpart could not be resolved to exactly one design space, so they were excluded before the
        /// calculation ran.
        /// <para>
        /// <b>Why a caller must look at this before believing a pass.</b> The report's combined status is a
        /// verdict over the spaces that WERE assessed. Where an occupied room in scope failed to resolve it
        /// is dropped with a warning, and the remaining rooms can then all pass - so an unguarded reading of
        /// <see cref="OccupiedSpaceComplianceStatus"/> would announce that every eligible space passes on
        /// the strength of an assessment that never looked at one of them. The refusal is recorded in
        /// <see cref="AssociationRefusals"/> as prose; this is the same fact as identities, so a caller can
        /// act on it.
        /// </para>
        /// </summary>
        public List<Guid> SpaceGuids_Unassessed { get; }

        /// <summary>Why no assessment was produced at all, or null where one was.</summary>
        public string? Refusal { get; }

        /// <summary>Whether there is an assessment to read.</summary>
        public bool IsAssessed => Result is not null && Report is not null && Refusal is null;

        /// <summary>
        /// The production verdict over the occupied spaces, combined by the report itself.
        /// <b>Never recomputed</b> from <see cref="SpaceResults"/>.
        /// </summary>
        public TM59ComplianceStatus OccupiedSpaceComplianceStatus => Report?.OccupiedSpaceComplianceStatus ?? TM59ComplianceStatus.Undefined;

        /// <summary>
        /// Runs the production assessment.
        /// </summary>
        /// <param name="analyticalModel_Workflow">
        /// <b>The model the TAS workflow returned</b>, and nothing else - see the class documentation.
        /// </param>
        /// <param name="path_TSD">The results file that workflow wrote.</param>
        /// <param name="overheatingScenarios">
        /// The scenarios of the preparation this run was built on. They are authoritative over which TM59
        /// criterion applies to which space, and are never derived from an internal condition or a name.
        /// </param>
        /// <param name="captureResultantTemperature">
        /// Whether to also hand back the hourly resultant temperature series this assessment read - see
        /// <see cref="ResultantTemperatures"/>. <b>False, the default, is every existing caller</b>, and
        /// changes nothing about what is calculated or reported.
        /// </param>
        /// <param name="spaceGuids_Capture">
        /// Which DESIGN spaces to capture, where capture is on. Null captures every assessed room, which
        /// on a large project is a great deal of memory for series nobody asked about - so a caller that
        /// knows its rooms says so.
        /// </param>
        public static PartOTM59Assessment Assess(AnalyticalModel? analyticalModel_Workflow, string? path_TSD, IEnumerable<OverheatingScenario>? overheatingScenarios, bool captureResultantTemperature = false, IEnumerable<Guid>? spaceGuids_Capture = null)
        {
            return Assess(analyticalModel_Workflow, path_TSD, overheatingScenarios, captureResultantTemperature, spaceGuids_Capture, ConvertTSD);
        }

        /// <summary>
        /// The TSD conversion settings a Part O TM59 assessment reads its results with: the two series the
        /// assessment reads, the zones and the weather data it needs - and <b>a full-year simulation</b>.
        /// <para>
        /// <see cref="TSDConversionSettings.RequireFullYear"/> is SAM_Tas's own check (SAM_Tas#73) of the
        /// simulation's stated day range, <c>firstDay == 1 &amp;&amp; lastDay == 365</c>, made before any
        /// result is read. It is off by default because a part-year TSD is a legitimate input elsewhere (a
        /// Grasshopper TM52/TM59 summer run); Part O's dynamic method is defined over a whole year, so it is
        /// switched on here, and only here. <see cref="TM59AssessmentCalculator.HourCount_Expected"/> could not
        /// catch a part year on its own: TSD answers 8760 hours for any simulation and pads the days it did not
        /// simulate with -1.
        /// </para>
        /// <para>A new instance on every call, so no caller can change what another one reads.</para>
        /// </summary>
        internal static TSDConversionSettings PartOTSDConversionSettings()
        {
            return new TSDConversionSettings()
            {
                SpaceDataTypes = new HashSet<SpaceDataType>() { SpaceDataType.ResultantTemperature, SpaceDataType.OccupantSensibleGain },
                ConvertWeaterData = true,
                ConvertZones = true,
                RequireFullYear = true,
            };
        }

        /// <summary>The production conversion: SAM_Tas's, which states why it refused where it did.</summary>
        private static AnalyticalModel? ConvertTSD(string path_TSD, TSDConversionSettings tSDConversionSettings, out string? refusal)
        {
            AnalyticalModel? result = Analytical.Tas.Convert.ToSAM(path_TSD, tSDConversionSettings, out string refusal_TSD);
            refusal = refusal_TSD;
            return result;
        }

        /// <summary>A TSD conversion: the model the file holds, or null with the reason when there is one.</summary>
        internal delegate AnalyticalModel? TSDConversion(string path_TSD, TSDConversionSettings tSDConversionSettings, out string? refusal);

        /// <summary>
        /// <see cref="Assess(AnalyticalModel, string, IEnumerable{OverheatingScenario}, bool, IEnumerable{Guid})"/>
        /// with the TSD conversion supplied, so the assessment can be exercised without TAS.
        /// </summary>
        internal static PartOTM59Assessment Assess(AnalyticalModel? analyticalModel_Workflow, string? path_TSD, IEnumerable<OverheatingScenario>? overheatingScenarios, bool captureResultantTemperature, IEnumerable<Guid>? spaceGuids_Capture, TSDConversion tSDConversion)
        {
            if (analyticalModel_Workflow is null || string.IsNullOrWhiteSpace(path_TSD))
            {
                return new PartOTM59Assessment(null, null, null, null, null, "No workflow model or no results path was supplied, so nothing could be assessed.");
            }

            //A results file that does not hold the full simulated year is refused HERE, from its stated day
            //range, before a single result is read or assessed (PartOTSDConversionSettings()).
            AnalyticalModel? analyticalModel_TSD = tSDConversion(path_TSD, PartOTSDConversionSettings(), out string? refusal_TSD);
            if (analyticalModel_TSD is null)
            {
                return new PartOTM59Assessment(null, null, null, null, null, refusal_TSD is null
                    ? string.Format("The simulation results at '{0}' could not be read.", path_TSD)
                    : string.Format("The simulation results at '{0}' were refused: {1}", path_TSD, refusal_TSD));
            }

            List<string> associationRefusals = [];

            //One map, built once, serving both the plant-zone exclusion below and the calculator.
            SimulationSpaceMap simulationSpaceMap = Analytical.Tas.Create.SimulationSpaceMap(analyticalModel_Workflow, analyticalModel_TSD);

            //The simulation-only plant zones the TAS export generates for the air handling units (one TAS
            //zone per unit, named after it) come back in the TSD like any other zone. They are not design
            //spaces, they were never expected to resolve to one, and they must not be assessed - so they are
            //excluded HERE, by the positive identification of Query.PartOPlantZoneSpaces, before the
            //calculator sees them. Everything genuinely unresolved that remains still refuses below, exactly
            //as before: this removes a false warning, never a real one.
            analyticalModel_TSD = WithoutPlantZoneSpaces(analyticalModel_TSD, analyticalModel_Workflow, simulationSpaceMap);

            //The design side of this call is the WORKFLOW model. Its spaces carry the zone guids TAS
            //stamped on the round trip, which is what the map matches on.
            TM59AssessmentCalculator tM59AssessmentCalculator = analyticalModel_TSD.TM59AssessmentCalculator(analyticalModel_Workflow, simulationSpaceMap);

            //A FULL YEAR, or the space is refused rather than assessed over part of one.
            //
            //Approved Document O's dynamic method assesses annual and summer criteria, so a verdict from a
            //partial series is not the verdict the document asks for - and until this was stated, a damaged
            //or partially written TSD produced one silently: TMOverheatingCalculator walked whatever hours
            //the two series shared and reported the result as the room's. Part O's own full-year check is
            //over the simulation's nominal DATE RANGE (PartOSimulationContext.IsFullYear, and the
            //fullYear flag RunPartOSimulation hands back), which says what was asked of TAS and not what
            //the results file actually contains.
            //
            //Taken from the REQUESTED day range that defines a Part O full year, and deliberately not from
            //anything inside the file being validated. Counting it from the weather year the TSD carries -
            //which is what this did at first - defeats the check entirely: a damaged file that lost two
            //thirds of its weather lost two thirds of its results with it, so the requirement fell to match
            //and the partial year passed. A results file may not decide how much of a year it was supposed
            //to contain. See PartOSimulationContext.HourCount_FullYear, including why it is the static
            //1-to-365 authority rather than an instance - a RESTORED run carries no context at all.
            //
            //This length check alone cannot see a part-year SIMULATION: TSD answers 8760 hours for any day range
            //and pads the days it did not simulate with -1. That case is refused earlier, from the file's stated
            //day range (PartOTSDConversionSettings().RequireFullYear); this one still refuses a short series.
            tM59AssessmentCalculator.HourCount_Expected = PartOSimulationContext.HourCount_FullYear;

            OverheatingScenarioMap overheatingScenarioMap = new(overheatingScenarios, analyticalModel_Workflow, tM59AssessmentCalculator.SimulationSpaceMap);
            tM59AssessmentCalculator.VentilationStrategyMap = overheatingScenarioMap.VentilationStrategyMap;

            associationRefusals.AddRange(overheatingScenarioMap.Refusals ?? []);

            tM59AssessmentCalculator.RestoreDesignInternalConditions();

            associationRefusals.AddRange(tM59AssessmentCalculator.AssociationRefusals);

            //Null spaces and null zones: the whole model, which for this calculator means every simulated
            //space that resolved to exactly one design space.
            List<Space> spaces = tM59AssessmentCalculator.Spaces(null, null);

            associationRefusals.AddRange(tM59AssessmentCalculator.AssociationRefusals);

            TM59AssessmentResult tM59AssessmentResult = tM59AssessmentCalculator.Calculate(spaces);
            if (tM59AssessmentResult is null)
            {
                return new PartOTM59Assessment(null, null, null, associationRefusals, null, string.Format("The simulation results at '{0}' could not be assessed.", path_TSD));
            }

            TM59AssessmentReport tM59AssessmentReport = new(tM59AssessmentResult, path_TSD);

            //Read off the DESIGN model, which is where the run's isolation context was stamped and which is
            //what survives into the .sam - so a restored review states the same scope as the run that
            //produced it, without either of them consulting a filename.
            PartOIsolationContext? partOIsolationContext = analyticalModel_Workflow?.GetValue<PartOIsolationContext>(Analytical.AnalyticalModelParameter.PartOIsolationContext);

            if (partOIsolationContext is not null && partOIsolationContext.IsValid)
            {
                tM59AssessmentReport.ThermalModelScope = string.Format(
                    "ISOLATED. Selected dwellings: {0}. Interfaces to excluded spaces were simulated as adiabatic, so these results are not a whole-building simulation of the same dwellings.",
                    string.Join(", ", partOIsolationContext.Names_Dwelling));
            }

            List<PartOTM59SpaceResult> spaceResults = ResolvedSpaceResults(tM59AssessmentReport, tM59AssessmentCalculator.SimulationSpaceMap, spaces, associationRefusals);

            //The hourly-series refusals reach the run's diagnostics like any other reason a room went
            //unassessed, so the notes on the step say which rooms had unusable results and why.
            associationRefusals.AddRange(tM59AssessmentResult.HourlySeriesRefusals);

            //Design side, not simulation side: which rooms of the model being assessed produced nothing.
            List<Guid> spaceGuids_Unassessed = [];

            //A room whose series were refused produced no result either, and it has to count as unassessed
            //for the same reason an unresolved one does: PartialAssessment refuses a PASS that has a hole in
            //the dwelling scope, and without this a truncated room simply vanished from the verdict and the
            //rooms whose data happened to survive were reported as a pass over all of them. Mapped back to
            //the DESIGN space, because that is the side the scope is expressed in.
            HashSet<Guid> guids_Simulation_Refused = [.. tM59AssessmentResult.SpaceGuids_HourlySeriesRefused];

            //Every simulated space the calculation produced a result for, in any of its three lists - the
            //same identity (TMResult.Reference) the report groups its rows by.
            HashSet<string> references_Result = [];
            foreach (TMResult tMResult in (tM59AssessmentResult.NaturalVentilationResults ?? []).Concat(tM59AssessmentResult.MechanicalVentilationResults ?? []).Concat(tM59AssessmentResult.CorridorResults ?? []))
            {
                if (!string.IsNullOrWhiteSpace(tMResult?.Reference))
                {
                    references_Result.Add(tMResult!.Reference);
                }
            }

            List<Guid> spaceGuids_NoResult = [];

            foreach (Space space_Design in analyticalModel_Workflow.GetSpaces() ?? [])
            {
                if (space_Design is null)
                {
                    continue;
                }

                Space? space_Simulation = tM59AssessmentCalculator.SimulationSpaceMap?.Simulation(space_Design);

                if (space_Simulation is null || guids_Simulation_Refused.Contains(space_Simulation.Guid))
                {
                    spaceGuids_Unassessed.Add(space_Design.Guid);
                }

                if (space_Simulation is null || !references_Result.Contains(space_Simulation.Guid.ToString()))
                {
                    spaceGuids_NoResult.Add(space_Design.Guid);
                }
            }

            //Read AFTER the calculation, off the very spaces it ran over and through the very key it read
            //them with - so the series handed back cannot be a different room's or a different quantity's
            //than the one the verdict above was computed from. Only where the caller asked.
            Dictionary<Guid, double[]>? resultantTemperatures = captureResultantTemperature
                ? CaptureResultantTemperatures(spaces, tM59AssessmentCalculator.SimulationSpaceMap, tM59AssessmentCalculator.ResultantTemperatureSeriesKey, spaceGuids_Capture)
                : null;

            //Each occupied space's overall status, as SAM combined it, keyed to its design space - through the same
            //map, so a room that resolves to no single design space has no status here, as it has no verdict.
            Dictionary<Guid, TM59ComplianceStatus> occupiedSpaceStatuses = [];

            Dictionary<string, Space> dictionary_Simulation = [];
            foreach (Space space_Simulation in spaces ?? [])
            {
                if (space_Simulation is not null)
                {
                    dictionary_Simulation[space_Simulation.Guid.ToString()] = space_Simulation;
                }
            }

            foreach (TM59AssessmentReportSpace tM59AssessmentReportSpace in tM59AssessmentReport.OccupiedSpaces ?? [])
            {
                if (tM59AssessmentReportSpace?.Reference is null || !dictionary_Simulation.TryGetValue(tM59AssessmentReportSpace.Reference, out Space? space_Simulation))
                {
                    continue;
                }

                Space? space_Design = tM59AssessmentCalculator.SimulationSpaceMap?.Design(space_Simulation);
                if (space_Design is not null)
                {
                    occupiedSpaceStatuses[space_Design.Guid] = tM59AssessmentReportSpace.ComplianceStatus;
                }
            }

            //The information-only rows, resolved to design spaces through the same map as every criterion row.
            List<Guid> spaceGuids_InformationOnly = [];
            foreach (TM59AssessmentReportCheck tM59AssessmentReportCheck in tM59AssessmentReport.SupplementaryChecks ?? [])
            {
                if (tM59AssessmentReportCheck?.Reference is null || !dictionary_Simulation.TryGetValue(tM59AssessmentReportCheck.Reference, out Space? space_Simulation))
                {
                    continue;
                }

                Space? space_Design = tM59AssessmentCalculator.SimulationSpaceMap?.Design(space_Simulation);
                if (space_Design is not null && !spaceGuids_InformationOnly.Contains(space_Design.Guid))
                {
                    spaceGuids_InformationOnly.Add(space_Design.Guid);
                }
            }

            return new PartOTM59Assessment(tM59AssessmentResult, tM59AssessmentReport, spaceResults, associationRefusals, spaceGuids_Unassessed, null, resultantTemperatures, spaceGuids_NoResult, occupiedSpaceStatuses, spaceGuids_InformationOnly);
        }

        /// <summary>
        /// The hourly resultant temperature of each assessed simulated space, keyed to the design space it
        /// resolves to.
        ///
        /// <para><b>The same series, read the same way</b></para>
        /// <para>
        /// The key is <c>TM59AssessmentCalculator.ResultantTemperatureSeriesKey</c> - the calculator's own,
        /// passed in rather than restated here, because a second spelling of it would be a second series.
        /// Each hour is converted exactly as <c>TMOverheatingCalculator</c> converts it: a value that is
        /// not a JSON number, or that will not convert, or that is not finite, becomes
        /// <see cref="double.NaN"/> rather than a zero. A consumer then refuses the room - which is the
        /// right answer, and the opposite of what silently reading an unknown hour as 0 C would produce.
        /// </para>
        /// <para>
        /// <b>A room is captured once.</b> Two simulated spaces resolving to one design space would be an
        /// identity failure the map itself refuses; if one reached here, the first is kept and the second
        /// is dropped rather than overwriting it.
        /// </para>
        /// </summary>
        internal static Dictionary<Guid, double[]> CaptureResultantTemperatures(IEnumerable<Space>? spaces_Simulation, SimulationSpaceMap? simulationSpaceMap, string key, IEnumerable<Guid>? spaceGuids_Capture)
        {
            Dictionary<Guid, double[]> result = [];

            HashSet<Guid>? guids_Wanted = spaceGuids_Capture is null ? null : [.. spaceGuids_Capture];

            if (guids_Wanted is not null && guids_Wanted.Count == 0)
            {
                return result;
            }

            foreach (Space space_Simulation in spaces_Simulation ?? [])
            {
                if (space_Simulation is null)
                {
                    continue;
                }

                Space? space_Design = simulationSpaceMap?.Design(space_Simulation);
                if (space_Design is null)
                {
                    continue;
                }

                Guid guid_Design = space_Design.Guid;

                if ((guids_Wanted is not null && !guids_Wanted.Contains(guid_Design)) || result.ContainsKey(guid_Design))
                {
                    continue;
                }

                if (!Core.Query.TryGetValue(space_Simulation, key, out JsonArray jsonArray) || jsonArray is null)
                {
                    continue;
                }

                double[] values = new double[jsonArray.Count];

                for (int i = 0; i < values.Length; i++)
                {
                    values[i] = Value(jsonArray[i]);
                }

                result[guid_Design] = values;
            }

            return result;
        }

        /// <summary>
        /// One hour of a captured series, or <see cref="double.NaN"/>.
        /// <para>
        /// The node's own JSON kind is asked FIRST and not only whether it converts: <c>Core.Query.TryConvert</c>
        /// routes a JSON boolean through its bool-to-double conversion, so <c>true</c> would read as 1 C.
        /// <c>GetValueKind()</c> is itself guarded because it throws on the NaN and infinity a
        /// <c>JsonArray</c> can hold - it has to decide how the value would serialize, and those do not.
        /// This mirrors <c>TMOverheatingCalculator</c>'s own reader deliberately; the two must agree about
        /// which hours are numbers.
        /// </para>
        /// </summary>
        private static double Value(JsonNode? jsonNode)
        {
            if (jsonNode is not JsonValue jsonValue)
            {
                return double.NaN;
            }

            try
            {
                if (jsonValue.GetValueKind() != System.Text.Json.JsonValueKind.Number)
                {
                    return double.NaN;
                }

                return Core.Query.TryConvert(jsonValue, out double value) ? value : double.NaN;
            }
            catch (ArgumentException)
            {
                return double.NaN;
            }
        }

        /// <summary>
        /// The simulated model minus its generated air handling unit plant zones - see
        /// <see cref="Query.PartOPlantZoneSpaces"/> for what identifies one. The input model is returned
        /// unmodified where there is nothing to exclude; otherwise a new model over a cluster the plant
        /// zones were removed from, so the exclusion never mutates the caller's conversion result.
        /// </summary>
        /// <remarks>Internal rather than private so the exclusion itself is pinned by tests.</remarks>
        internal static AnalyticalModel WithoutPlantZoneSpaces(AnalyticalModel analyticalModel_Simulated, AnalyticalModel analyticalModel_Design, SimulationSpaceMap simulationSpaceMap)
        {
            AdjacencyCluster adjacencyCluster = analyticalModel_Simulated?.AdjacencyCluster;
            if (adjacencyCluster is null)
            {
                return analyticalModel_Simulated;
            }

            List<Space> spaces_PlantZone = Query.PartOPlantZoneSpaces(adjacencyCluster.GetSpaces(), analyticalModel_Design?.AdjacencyCluster?.GetObjects<AirHandlingUnit>(), simulationSpaceMap);
            if (spaces_PlantZone.Count == 0)
            {
                return analyticalModel_Simulated;
            }

            adjacencyCluster.Remove(spaces_PlantZone);

            return new AnalyticalModel(analyticalModel_Simulated, adjacencyCluster);
        }

        /// <summary>
        /// Every occupied-space check of the report, carried over unchanged and keyed to the design space
        /// its simulated space resolves to.
        /// <para>
        /// A check whose <c>Reference</c> names no simulated space this assessment calculated, or whose
        /// simulated space does not resolve to exactly one design space, is <b>reported and dropped</b>. It
        /// is still in the report an engineer reads; what it must not be is an identity an automatic
        /// optimisation then acts on.
        /// </para>
        /// </summary>
        private static List<PartOTM59SpaceResult> ResolvedSpaceResults(TM59AssessmentReport tM59AssessmentReport, SimulationSpaceMap simulationSpaceMap, List<Space> spaces_Simulation, List<string> associationRefusals)
        {
            List<PartOTM59SpaceResult> result = [];

            Dictionary<string, Space> dictionary_Simulation = [];
            foreach (Space space_Simulation in spaces_Simulation ?? [])
            {
                if (space_Simulation is not null)
                {
                    dictionary_Simulation[space_Simulation.Guid.ToString()] = space_Simulation;
                }
            }

            Add(result, tM59AssessmentReport.MechanicalVentilationChecks, true, dictionary_Simulation, simulationSpaceMap, associationRefusals);
            Add(result, tM59AssessmentReport.NaturalVentilationChecks, false, dictionary_Simulation, simulationSpaceMap, associationRefusals);

            return result;
        }

        private static void Add(List<PartOTM59SpaceResult> result, List<TM59AssessmentReportCheck>? tM59AssessmentReportChecks, bool mechanical, Dictionary<string, Space> dictionary_Simulation, SimulationSpaceMap simulationSpaceMap, List<string> associationRefusals)
        {
            foreach (TM59AssessmentReportCheck tM59AssessmentReportCheck in tM59AssessmentReportChecks ?? [])
            {
                if (tM59AssessmentReportCheck is null)
                {
                    continue;
                }

                Space? space_Design = tM59AssessmentReportCheck.Reference is not null && dictionary_Simulation.TryGetValue(tM59AssessmentReportCheck.Reference, out Space? space_Simulation)
                    ? simulationSpaceMap?.Design(space_Simulation)
                    : null;

                if (space_Design is null)
                {
                    associationRefusals.Add(string.Format(
                        "The TM59 result for '{0}' ({1}) could not be resolved to exactly one design space, so it is reported but cannot be acted on automatically.",
                        tM59AssessmentReportCheck.SpaceName,
                        tM59AssessmentReportCheck.Check));

                    continue;
                }

                result.Add(new PartOTM59SpaceResult(
                    space_Design.Guid,
                    space_Design.Name,
                    tM59AssessmentReportCheck.Check,
                    tM59AssessmentReportCheck.Actual,
                    tM59AssessmentReportCheck.Limit,
                    tM59AssessmentReportCheck.ComplianceStatus,
                    mechanical));
            }
        }
    }
}
