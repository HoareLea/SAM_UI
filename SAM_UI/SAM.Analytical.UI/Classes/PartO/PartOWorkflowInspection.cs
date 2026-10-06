// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using SAM.Core;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.UI
{
    /// <summary>
    /// What the loaded model already provides for a requested Approved Document O run, what this run would
    /// build, and what nothing in this run can supply.
    ///
    /// <para><b>Why this exists</b></para>
    /// <para>
    /// The Part O commands are individually correct and collectively unusable by anyone who has not read
    /// them: a person had to remember whether they had zoned, mapped TM59 conditions, sized against Approved
    /// Document F and prepared an iteration, in that order, and the only way to find out was to run the next
    /// command and read its refusal. This answers all of it before anything runs, from the model itself, so a
    /// reopened <c>.sam</c> is as legible as one prepared five minutes ago.
    /// </para>
    ///
    /// <para><b>It decides nothing</b></para>
    /// <para>
    /// Every stage is answered by the authority that already owns it, asked here rather than restated:
    /// </para>
    /// <list type="bullet">
    /// <item><b>Dwelling scope</b> - <c>Query.PartFDwellingZones</c>, the single source of the
    /// dwelling-selection policy.</item>
    /// <item><b>TM59 mapping</b> - <c>TM59Manager.TM59SpaceApplications(InternalCondition, TextMap)</c>
    /// falling back to the space name, which is exactly the pair <c>TMOverheatingCalculator</c> classifies a
    /// simulated space with. A space this reports as mapped is a space the assessment will classify.</item>
    /// <item><b>Part F requirements</b> - <c>PartFIndex.PartFRequiredFlowRate_Lps</c>, which is
    /// <c>Query.PartFRequiredFlowRate_Lps(AdjacencyCluster, Space, FlowClassification)</c> reached through
    /// one snapshot of the model space identities instead of re-resolving every room against the whole
    /// model. It reads the space's own <c>PartFSpaceData</c> live on every call. No rate is re-derived, none
    /// is cached here, and none is compared against anything.</item>
    /// <item><b>Which route needs Part F at all</b> - <c>Query.PartOIterationVentilationMode</c> and
    /// <c>Query.PartOPartFAirflowApplication</c>, so Iteration 1b is reported as N/A for the same reason the
    /// preparation skips it rather than because this file knows what "natural" means.</item>
    /// <item><b>Equipment, results and Iteration 2B</b> - the catalogue reader, <c>PartORun.IsAssessable</c>
    /// and <c>Modify.CanOptimise</c>, carried in through <see cref="PartOWorkflowCapabilities"/>.</item>
    /// </list>
    ///
    /// <para><b>Only a genuine impossibility blocks Run</b></para>
    /// <para>
    /// <see cref="PartOWorkflowStageStatus.Blocked"/> is used exactly where the production chain would refuse
    /// - no dwelling zone, nothing TM59 can classify, no Approved Document F requirement on the mechanical
    /// route, a requested product selection with no catalogue. Everything else that is imperfect is stated in
    /// a detail line and left to the authority that owns it. In particular no warning is promoted: a Part O
    /// model carries intentional warnings on every run.
    /// </para>
    ///
    /// <para><b>Reuse is a match, never a cache</b></para>
    /// <para>
    /// <see cref="ReusePreparation"/> is true only where the session's run is
    /// <see cref="PartORunState.Prepared"/> and its <see cref="PartOPreparationContext"/> - the record of
    /// what the preparation was actually given - describes this same request. The model is the authority
    /// throughout; nothing is remembered here between one inspection and the next.
    /// </para>
    /// </summary>
    public class PartOWorkflowInspection
    {
        private readonly List<PartOWorkflowStageState> stages = [];

        private readonly List<string> blockers = [];

        private PartOWorkflowInspection()
        {
        }

        /// <summary>Every stage, in reading order.</summary>
        public IReadOnlyList<PartOWorkflowStageState> Stages => stages;

        /// <summary>Why Run is unavailable, one sentence each. Empty where it is available.</summary>
        public IReadOnlyList<string> Blockers => blockers;

        /// <summary>Whether Prepare and Run may start.</summary>
        public bool CanRun => blockers.Count == 0;

        /// <summary>
        /// Whether the session's prepared iteration already describes this request, so the run may go
        /// straight to simulation. False whenever anything about the request differs, and false for a
        /// restored run, which carries no preparation context by design.
        /// </summary>
        public bool ReusePreparation { get; private set; }

        /// <summary>Whether existing results can be reviewed without running TAS again.</summary>
        public bool CanReviewResults { get; private set; }

        /// <summary>Whether Iteration 2B can start from those results.</summary>
        public bool CanOptimise { get; private set; }

        /// <summary>Why Iteration 2B is unavailable, in <c>Modify.CanOptimise</c>'s own words.</summary>
        public string OptimisationRefusal { get; private set; }

        /// <summary>Why a review is unavailable, in <c>PartORun.IsAssessable</c>'s own words.</summary>
        public string ResultsRefusal { get; private set; }

        /// <summary>
        /// Inspects the model against one request.
        /// </summary>
        /// <param name="analyticalModel">The loaded model. Null is inspected too, and blocks on every stage that needs one.</param>
        /// <param name="partOWorkflowRequest">What the user asked for. Null blocks Run rather than assuming a scenario.</param>
        /// <param name="partORun">The session's run, or null.</param>
        /// <param name="partOWorkflowCapabilities">The session facts a model cannot answer. Null is read as "nothing available".</param>
        /// <param name="textMap_TM59">
        /// The TM59 keyword map spaces are classified against. Null takes
        /// <c>Query.DefaultInternalConditionTextMap_TM59</c>, which is what production does; supplied
        /// explicitly only where the caller already holds one, so a machine without the installed resource
        /// does not silently change what this reports.
        /// </param>
        /// <param name="partFIndex_Scope">
        /// The snapshot of the model space identities this inspection resolves through. Null - which is what
        /// production passes - builds one from <paramref name="analyticalModel"/> for the duration of this
        /// call and drops it. Supplied only by a caller that already holds one for exactly this model, and by
        /// the tests that count what one inspection asks of it.
        /// </param>
        public static PartOWorkflowInspection Inspect(AnalyticalModel analyticalModel, PartOWorkflowRequest partOWorkflowRequest, PartORun partORun, PartOWorkflowCapabilities partOWorkflowCapabilities, TextMap textMap_TM59 = null, PartFIndex partFIndex_Scope = null)
        {
            PartOWorkflowInspection result = new();

            PartOWorkflowCapabilities capabilities = partOWorkflowCapabilities ?? new PartOWorkflowCapabilities();

            result.CanReviewResults = capabilities.ResultsAvailable;
            result.ResultsRefusal = capabilities.ResultsRefusal;
            result.CanOptimise = capabilities.OptimisationAvailable;
            result.OptimisationRefusal = capabilities.OptimisationRefusal;

            AdjacencyCluster adjacencyCluster = analyticalModel?.AdjacencyCluster;

            //ONE snapshot of the model space identities for the whole inspection, built by SAM and asked of
            //SAM. An inspection reads every space of the scope three times - the dwelling scope resolves
            //them, and the Approved Document F stage asks a supply and an extract requirement of each - and
            //every one of those reads used to re-resolve the space against the whole model, which made
            //inspecting a five thousand space project quadratic. It is NOT a cache: it lives for this one
            //call, it holds no rate, and it answers exactly what Query.PartFRequiredFlowRate_Lps answers,
            //read from the model every time.
            PartFIndex partFIndex = partFIndex_Scope ?? new PartFIndex(adjacencyCluster);

            List<Space> spaces_Scope = Spaces_Scope(partFIndex, partOWorkflowRequest);

            result.Add(DwellingScope(analyticalModel, partOWorkflowRequest, spaces_Scope));
            result.Add(InternalConditions(adjacencyCluster, spaces_Scope, textMap_TM59 ?? Analytical.Query.DefaultInternalConditionTextMap_TM59()));
            result.Add(PartFRequirements(partFIndex, partOWorkflowRequest, spaces_Scope));

            result.ReusePreparation = Reusable(partORun, partOWorkflowRequest);

            result.Add(VentilationDesign(partORun, result.ReusePreparation));
            result.Add(Equipment(partOWorkflowRequest, capabilities));
            result.Add(ModelCheck());
            result.Add(Simulation(partORun, capabilities));
            result.Add(Results(capabilities));

            if (partOWorkflowRequest?.Option is null)
            {
                result.blockers.Add("No base provision is selected, so there is no Approved Document O iteration to prepare.");
            }

            //An opened Part O result is reviewed, never run from: every case derives from the design model.
            if (!string.IsNullOrWhiteSpace(capabilities.DesignModelRefusal))
            {
                result.blockers.Insert(0, capabilities.DesignModelRefusal);
            }

            return result;
        }

        private void Add(PartOWorkflowStageState partOWorkflowStageState)
        {
            stages.Add(partOWorkflowStageState);

            if (partOWorkflowStageState.IsBlocking)
            {
                blockers.Add(partOWorkflowStageState.Detail);
            }
        }

        /// <summary>
        /// The spaces the request actually covers: the spaces of the dwelling zones in scope, re-resolved
        /// from the cluster.
        /// <para>
        /// <b>Re-resolved on purpose.</b> A zone's related space instances can predate a later write - the
        /// Part F application replaces spaces wholesale - so the instance reached through the relation is
        /// not necessarily the one the model now holds. Every parameter read below has to be taken from the
        /// current one. <c>Query.PartFRequiredFlowRate_Lps</c> does the same thing internally, for the same
        /// reason.
        /// </para>
        /// <para>
        /// <b>Resolved by SAM, not here.</b> <c>PartFIndex.Spaces_Zones</c> is the one place that turns a set
        /// of dwelling zones into the space instances the model currently carries, and it does it against the
        /// inspection's single snapshot - so this stays linear on a model with thousands of spaces, and this
        /// file keeps no lookup of its own that could go stale against the model.
        /// </para>
        /// </summary>
        private static List<Space> Spaces_Scope(PartFIndex partFIndex, PartOWorkflowRequest partOWorkflowRequest)
        {
            List<Zone> zones = partOWorkflowRequest?.Zones_Dwelling;
            if (zones is null || zones.Count == 0)
            {
                return [];
            }

            return partFIndex.Spaces_Zones(zones);
        }

        private static PartOWorkflowStageState DwellingScope(AnalyticalModel analyticalModel, PartOWorkflowRequest partOWorkflowRequest, List<Space> spaces_Scope)
        {
            if (analyticalModel is null)
            {
                return new PartOWorkflowStageState(PartOWorkflowStage.DwellingScope, PartOWorkflowStageStatus.Blocked, "No analytical model is open.");
            }

            List<Zone> zones = analyticalModel.GetZones() ?? [];
            if (zones.Count == 0)
            {
                return new PartOWorkflowStageState(PartOWorkflowStage.DwellingScope, PartOWorkflowStageStatus.Blocked, "The model has no zones, so no dwelling can be assessed. Zone the model and mark its dwellings first.");
            }

            //The one dwelling rule, asked rather than restated.
            List<Zone> zones_Eligible = Analytical.Query.PartFDwellingZones(zones);
            if (zones_Eligible.Count == 0)
            {
                return new PartOWorkflowStageState(PartOWorkflowStage.DwellingScope, PartOWorkflowStageStatus.Blocked, string.Format("None of the model's {0} is stated as a dwelling, so there is nothing Approved Document O would assess. Mark the dwelling zones first.", Query.PartOCount(zones.Count, "zone", "zones")));
            }

            List<Zone> zones_Selected = partOWorkflowRequest?.Zones_Dwelling ?? [];
            if (zones_Selected.Count == 0)
            {
                return new PartOWorkflowStageState(PartOWorkflowStage.DwellingScope, PartOWorkflowStageStatus.Blocked, string.Format("No dwelling is selected. {0} {1} eligible.", Query.PartOCount(zones_Eligible.Count, "dwelling zone", "dwelling zones"), zones_Eligible.Count == 1 ? "is" : "are"));
            }

            string detail = string.Format(
                "{0} of {1} eligible {2} in scope, {3}{4}.",
                zones_Selected.Count,
                zones_Eligible.Count,
                Query.PartONoun(zones_Eligible.Count, "dwelling zone", "dwelling zones"),
                Query.PartOCount(spaces_Scope.Count, "space", "spaces"),
                partOWorkflowRequest.Isolate ? ", simulated as an isolated thermal model" : string.Empty);

            //The same counts, as the first-level line. "3 dwellings" where the whole eligible set is in
            //scope, "2 of 3 dwellings" where it was narrowed.
            string summary = string.Format(
                "{0} · {1}{2}",
                zones_Selected.Count == zones_Eligible.Count
                    ? Query.PartOCount(zones_Selected.Count, "dwelling", "dwellings")
                    : string.Format("{0} of {1}", zones_Selected.Count, Query.PartOCount(zones_Eligible.Count, "dwelling", "dwellings")),
                Query.PartOCount(spaces_Scope.Count, "space", "spaces"),
                partOWorkflowRequest.Isolate ? " · isolated" : string.Empty);

            //What the MODEL says it already is, as opposed to what this request asks for - stamped by
            //Analytical.Modify.PreparePartOIteration and carried in the .sam, so it survives a reopen and is
            //the authority for what a loaded model actually is. Said here because a model that is already an
            //isolated extract is not the whole building it was taken from, and nothing else on screen would
            //tell a person that before they ran it again.
            PartOIsolationContext partOIsolationContext = analyticalModel.GetValue<PartOIsolationContext>(Analytical.AnalyticalModelParameter.PartOIsolationContext);

            if (partOIsolationContext is not null && partOIsolationContext.IsValid)
            {
                detail = string.Format("{0} This model is ALREADY the isolated thermal model of {1}, so it is no longer the whole building it was extracted from.", detail, string.Join(", ", partOIsolationContext.Names_Dwelling));
            }

            return new PartOWorkflowStageState(PartOWorkflowStage.DwellingScope, PartOWorkflowStageStatus.Ready, detail, summary);
        }

        /// <summary>
        /// Whether the dwellings in scope carry what a TM59 assessment needs: an internal condition on at
        /// least one of them, and at least one room the assessment will classify.
        ///
        /// <para><b>Two prerequisites, and only one of them is about classification</b></para>
        /// <para>
        /// Read as one question they look self-contradictory: the classification below deliberately falls
        /// back to the space name, so a room with no internal condition can still be classified - yet a
        /// scope in which NOTHING has an internal condition is refused anyway. Both are correct, because the
        /// second refusal is not a classification refusal. It is stated in full here so it is not mistaken
        /// for one again.
        /// </para>
        ///
        /// <para><b>1. An internal condition is what states how a room is OCCUPIED</b></para>
        /// <para>
        /// It is not a label the assessment reads. It is the runtime the simulation is driven by, and every
        /// TM59 criterion is a comparison over the occupied hours that runtime produces:
        /// </para>
        /// <list type="bullet">
        /// <item><c>SAM.Analytical.Tas.Modify.UpdateInternalCondition</c> returns false the moment
        /// <c>space.InternalCondition</c> is null, so the TBD internal condition <c>AddInternalCondition</c>
        /// has just created is left exactly as <c>Building.AddIC(null)</c> made it - no occupancy profile,
        /// no gains, no setpoints - and <c>UpdateZone</c> assigns that to the zone regardless. Nothing on
        /// the conversion path refuses it.</item>
        /// <item><c>TMOverheatingCalculator.Collect</c> counts an hour as occupied only where the simulated
        /// occupancy sensible gain is above zero. A room simulated from a blank internal condition therefore
        /// has NO occupied hours, and each criterion is then judged over an empty set:
        /// <c>TM59NaturalVentilationBedroomExtendedResult.Criterion2</c> becomes <c>0 &gt;= 0</c> and
        /// PASSES. A verdict manufactured from an empty occupied set is worse than a refusal - it looks
        /// like an answer.</item>
        /// <item><c>SAM.Analytical.Tas.TM59.Convert.ToTM59(Space, TM59Manager, SystemType)</c> returns null
        /// for a space with no internal condition <i>before</i> it ever asks <c>RoomUse</c>, so the name
        /// fallback is not reached there at all; the building overload turns that into a refusal of the
        /// whole DomOv document.</item>
        /// </list>
        /// <para>
        /// <b>And nothing downstream stops it.</b> <c>Create.Log</c> - the authority
        /// <c>PartOPreSimulationCheck</c> is - records a space with no internal condition as a
        /// <c>Warning</c>, and that check stops only on <c>Error</c>. So this is the one place on the Part O
        /// path that can refuse it, which is why it does.
        /// </para>
        ///
        /// <para><b>2. Classification, asked exactly the way the assessment asks it</b></para>
        /// <para>
        /// <c>TMOverheatingCalculator</c> classifies a simulated space with
        /// <c>TM59Manager.TM59SpaceApplications(space.InternalCondition)</c> and falls back to the space name
        /// when that yields nothing; the same pair is used here, over the same default TM59 text map. So a
        /// space this counts as mapped is a space the assessment will produce a sleeping, living or cooking
        /// result for - not a guess about a name. That fallback is genuine and is counted, which is exactly
        /// why <c>count_Classified</c> and <c>count_InternalCondition</c> are two separate counts.
        /// </para>
        /// <para>
        /// <b>Blocked only where NOTHING classifies.</b> A model with some unclassified rooms is a normal
        /// model - a bathroom, a hall, a store are all correctly unclassified - and the assessment names
        /// them as not assessed rather than failing. Only a scope in which no room at all is a TM59 room
        /// means the run would produce an empty assessment, which is what this refuses.
        /// </para>
        /// </summary>
        private static PartOWorkflowStageState InternalConditions(AdjacencyCluster adjacencyCluster, List<Space> spaces_Scope, TextMap textMap)
        {
            if (adjacencyCluster is null || spaces_Scope.Count == 0)
            {
                return new PartOWorkflowStageState(PartOWorkflowStage.InternalConditions, PartOWorkflowStageStatus.Blocked, "There are no spaces in scope, so no internal condition can be assessed.");
            }

            if (textMap is null)
            {
                //A fact about this machine, not about the building - so it is reported as "not checked"
                //rather than as a defect, and it does not block. The assessment reads the same resource and
                //will state the consequence itself. Blocking here would refuse a model the pipeline accepts,
                //on the strength of something never looked at.
                return new PartOWorkflowStageState(PartOWorkflowStage.InternalConditions, PartOWorkflowStageStatus.Pending, "The TM59 internal-condition text map resource could not be loaded on this machine, so the spaces in scope were not classified here. The assessment reads the same resource and reports what it finds.");
            }

            int count_InternalCondition = 0;
            int count_Classified = 0;

            foreach (Space space in spaces_Scope)
            {
                InternalCondition internalCondition = space.InternalCondition;

                if (internalCondition is not null)
                {
                    count_InternalCondition++;
                }

                List<TM59SpaceApplication> tM59SpaceApplications = TM59Manager.TM59SpaceApplications(internalCondition, textMap);
                if (tM59SpaceApplications is null || tM59SpaceApplications.Count == 0)
                {
                    tM59SpaceApplications = TM59Manager.TM59SpaceApplications(space, textMap);
                }

                if (tM59SpaceApplications is not null && tM59SpaceApplications.Count != 0)
                {
                    count_Classified++;
                }
            }

            //NOT a classification refusal, and it is deliberately reported before the classification one -
            //count_Classified above may well be non-zero here, from the space-name fallback. What is missing
            //is the OCCUPANCY: see the "1." section of this method's summary for the three production
            //authorities. The message says that, rather than implying the rooms are unrecognised.
            if (count_InternalCondition == 0)
            {
                return new PartOWorkflowStageState(PartOWorkflowStage.InternalConditions, PartOWorkflowStageStatus.Blocked, string.Format("None of the {0} in scope has an internal condition, so nothing states how they are occupied. A TM59 room name selects which criterion applies but supplies no occupancy: these spaces would simulate unoccupied, and their TM59 verdicts would be judged over an empty set of occupied hours rather than refused. Run Map IC (TM59) first.", Query.PartOCount(spaces_Scope.Count, "space", "spaces")));
            }

            if (count_Classified == 0)
            {
                return new PartOWorkflowStageState(PartOWorkflowStage.InternalConditions, PartOWorkflowStageStatus.Blocked, string.Format("None of the {0} in scope is recognised as a TM59 sleeping, living or cooking room, so the assessment would report nothing. Run Map IC (TM59) first.", Query.PartOCount(spaces_Scope.Count, "space", "spaces")));
            }

            int count_NoInternalCondition = spaces_Scope.Count - count_InternalCondition;

            string detail = string.Format(
                "{0} of {1} in scope {2} as a TM59 sleeping, living or cooking room{3}.",
                count_Classified,
                Query.PartOCount(spaces_Scope.Count, "space", "spaces"),
                count_Classified == 1 ? "classifies" : "classify",
                count_NoInternalCondition == 0 ? string.Empty : string.Format("; {0} no internal condition and will not be assessed", count_NoInternalCondition == 1 ? "1 space has" : string.Format("{0} spaces have", count_NoInternalCondition)));

            string summary = string.Format(
                "{0}/{1} mapped{2}",
                count_Classified,
                Query.PartOCount(spaces_Scope.Count, "space", "spaces"),
                count_NoInternalCondition == 0 ? string.Empty : string.Format(" · {0} without internal condition", count_NoInternalCondition));

            return new PartOWorkflowStageState(PartOWorkflowStage.InternalConditions, PartOWorkflowStageStatus.Ready, detail, summary);
        }

        /// <summary>
        /// Whether the dwellings in scope carry the continuous Approved Document F requirement the mechanical
        /// route is realized from.
        /// <para>
        /// <b>Whether it is needed at all is SAM's answer, not this file's.</b>
        /// <c>Query.PartOIterationVentilationMode</c> gives the route the requested iteration is defined over
        /// and <c>Query.PartOPartFAirflowApplication</c> says what the preparation does with Part F on that
        /// route. Iteration 1b reports N/A because that pair says the airflow application is skipped - and it
        /// reports it in that query's own words, which is the same sentence the preparation records as a note.
        /// </para>
        /// <para>
        /// <b>Blocked where the preparation would refuse.</b> On the mechanical route with no continuous
        /// requirement anywhere in scope, <c>PrepareBaseMVHR</c> refuses with "no space carries a continuous
        /// requirement that could be realized as a design ventilation terminal". Saying so here costs
        /// nothing; discovering it after a preparation costs the user a dialog and a retry.
        /// </para>
        /// </summary>
        private static PartOWorkflowStageState PartFRequirements(PartFIndex partFIndex, PartOWorkflowRequest partOWorkflowRequest, List<Space> spaces_Scope)
        {
            if (partOWorkflowRequest?.Option is null)
            {
                return new PartOWorkflowStageState(PartOWorkflowStage.PartFRequirements, PartOWorkflowStageStatus.NotRun, "No base provision is selected.");
            }

            PartOVentilationMode partOVentilationMode = Analytical.Query.PartOIterationVentilationMode(partOWorkflowRequest.PartOIteration, out string _);

            PartOPartFAirflowApplication partOPartFAirflowApplication = Analytical.Query.PartOPartFAirflowApplication(partOVentilationMode, out string diagnostic);

            if (partOPartFAirflowApplication != PartOPartFAirflowApplication.Apply)
            {
                return new PartOWorkflowStageState(PartOWorkflowStage.PartFRequirements, PartOWorkflowStageStatus.NotApplicable, diagnostic ?? "This route applies no continuous mechanical rate.");
            }

            if (partFIndex.AdjacencyCluster is null || spaces_Scope.Count == 0)
            {
                return new PartOWorkflowStageState(PartOWorkflowStage.PartFRequirements, PartOWorkflowStageStatus.Blocked, "There are no spaces in scope to carry an Approved Document F requirement.");
            }

            int count = 0;

            foreach (Space space in spaces_Scope)
            {
                //The same answer Query.PartFRequiredFlowRate_Lps gives, read from the model through the
                //inspection's own snapshot rather than by re-resolving the room against the whole model
                //twice over. No rate is held here, and none is compared against anything.
                double? supply = partFIndex.PartFRequiredFlowRate_Lps(space, FlowClassification.Supply);
                double? extract = partFIndex.PartFRequiredFlowRate_Lps(space, FlowClassification.Extract);

                if ((supply.HasValue && supply.Value > 0) || (extract.HasValue && extract.Value > 0))
                {
                    count++;
                }
            }

            if (count == 0)
            {
                return new PartOWorkflowStageState(PartOWorkflowStage.PartFRequirements, PartOWorkflowStageStatus.Blocked, string.Format("No space in scope carries a continuous Approved Document F requirement, so the {0} route has no mechanical ventilation to realize. Run AddVent PartF over these dwellings first.", Core.Query.Description(partOVentilationMode)));
            }

            return new PartOWorkflowStageState(
                PartOWorkflowStage.PartFRequirements,
                PartOWorkflowStageStatus.Ready,
                string.Format("{0} of {1} in scope {2} a continuous Approved Document F supply or extract requirement.", count, Query.PartOCount(spaces_Scope.Count, "space", "spaces"), count == 1 ? "carries" : "carry"),
                string.Format("{0}/{1} defined", count, Query.PartOCount(spaces_Scope.Count, "space", "spaces")));
        }

        /// <summary>
        /// The design the preparation builds: terminals, one system and unit per dwelling, the duties and the
        /// air movements that carry them into TAS.
        /// <para>
        /// <b>Never blocking.</b> This stage is the run's own output. It is either reused, or built.
        /// </para>
        /// </summary>
        private static PartOWorkflowStageState VentilationDesign(PartORun partORun, bool reuse)
        {
            if (reuse)
            {
                return new PartOWorkflowStageState(PartOWorkflowStage.VentilationDesign, PartOWorkflowStageStatus.Reused, "An iteration prepared over exactly this base provision and dwelling scope is already loaded, so it is simulated as it stands rather than prepared again.", "Prepared for this scenario and scope · reused");
            }

            if (partORun is not null && partORun.State == PartORunState.Prepared)
            {
                return new PartOWorkflowStageState(PartOWorkflowStage.VentilationDesign, PartOWorkflowStageStatus.Prepare, "An iteration is prepared, but for a different base provision or dwelling scope, so this run prepares it again.", "Prepared for a different scenario or scope · rebuilt by Prepare & Run");
            }

            return new PartOWorkflowStageState(PartOWorkflowStage.VentilationDesign, PartOWorkflowStageStatus.Prepare, "This run builds the design ventilation terminals, one ventilation system and unit per dwelling, and the air movements that carry the design airflow into TAS.", "Built by Prepare & Run");
        }

        private static PartOWorkflowStageState Equipment(PartOWorkflowRequest partOWorkflowRequest, PartOWorkflowCapabilities partOWorkflowCapabilities)
        {
            if (partOWorkflowRequest is null || !partOWorkflowRequest.SelectVentilationUnit)
            {
                return new PartOWorkflowStageState(PartOWorkflowStage.Equipment, PartOWorkflowStageStatus.NotApplicable, "No manufacturer unit is selected, so this is an Iteration 1 run and the design duty stands on its own.");
            }

            if (!partOWorkflowCapabilities.EquipmentAvailable)
            {
                return new PartOWorkflowStageState(PartOWorkflowStage.Equipment, PartOWorkflowStageStatus.Blocked, string.Format("Iteration 2 selects a real manufacturer unit, and none is available. {0}", partOWorkflowCapabilities.EquipmentDescription));
            }

            return new PartOWorkflowStageState(PartOWorkflowStage.Equipment, PartOWorkflowStageStatus.Ready, "The smallest capable product is selected per dwelling against the realized design duty. A product's maximum is its capability ceiling and never becomes a design airflow.");
        }

        /// <summary>
        /// <c>SAMAnalytical.Check</c> as the Part O pre-simulation gate.
        /// <para>
        /// <b>Always pending, deliberately.</b> The gate judges the NORMALIZED model TAS is actually given -
        /// after the default material repair and the construction-layer update - and that model does not
        /// exist until the run builds it. Running <c>Create.Log</c> here over the source model would report
        /// errors the pipeline was about to repair, which is the defect the gate's own placement fixed. See
        /// <see cref="PartOPreSimulationCheck"/>.
        /// </para>
        /// </summary>
        private static PartOWorkflowStageState ModelCheck()
        {
            return new PartOWorkflowStageState(PartOWorkflowStage.ModelCheck, PartOWorkflowStageStatus.Pending, "SAM Check runs over the prepared model immediately before TAS converts it. Errors stop the run; warnings are recorded and do not.", "Runs after preparation, before TAS");
        }

        private static PartOWorkflowStageState Simulation(PartORun partORun, PartOWorkflowCapabilities partOWorkflowCapabilities)
        {
            if (partOWorkflowCapabilities.ResultsAvailable)
            {
                string detail = partOWorkflowCapabilities.ResultsRestored
                    ? string.Format("Reopened from this model's own saved run{0}. No new simulation is needed to review it.", Where(partOWorkflowCapabilities.Path_Results))
                    : string.Format("Completed in this session{0}.", Where(partOWorkflowCapabilities.Path_Results));

                return new PartOWorkflowStageState(PartOWorkflowStage.Simulation, PartOWorkflowStageStatus.Ready, detail);
            }

            if (partORun is not null && partORun.State == PartORunState.Prepared)
            {
                return new PartOWorkflowStageState(PartOWorkflowStage.Simulation, PartOWorkflowStageStatus.NotRun, "An iteration is prepared and waiting for its full-year TAS simulation.", "Prepared · waiting for the full-year TAS run");
            }

            return new PartOWorkflowStageState(PartOWorkflowStage.Simulation, PartOWorkflowStageStatus.NotRun, "This run converts the prepared model and simulates the full year in TAS.", "Full-year TAS run");
        }

        private static PartOWorkflowStageState Results(PartOWorkflowCapabilities partOWorkflowCapabilities)
        {
            if (partOWorkflowCapabilities.ResultsAvailable)
            {
                return new PartOWorkflowStageState(PartOWorkflowStage.Results, PartOWorkflowStageStatus.Ready, "The TM59 assessment can be reviewed from these results without running TAS again.");
            }

            return new PartOWorkflowStageState(PartOWorkflowStage.Results, PartOWorkflowStageStatus.NotRun, partOWorkflowCapabilities.ResultsRefusal ?? "There are no results to review yet.");
        }

        private static string Where(string path)
        {
            return string.IsNullOrWhiteSpace(path) ? string.Empty : string.Format(" ({0})", System.IO.Path.GetFileName(path));
        }

        /// <summary>
        /// Whether the session's prepared iteration is the one this request asks for.
        /// <para>
        /// <b>Compared against the preparation's own record of what it was given</b> -
        /// <see cref="PartOPreparationContext"/> - which is the same object an Iteration 2B round repeats a
        /// preparation from. Every input that reaches
        /// <c>SAM.Analytical.Modify.PreparePartOIteration</c> is compared: the iteration, the dwelling scope
        /// by guid, the route word stated for each of them, whether a catalogue was offered, HOW the
        /// products were chosen from it, and whether the model was isolated. Anything else differing means a
        /// different engineering case, so the iteration is prepared again.
        /// </para>
        /// <para>
        /// <b>The Iteration 2B settings are deliberately NOT compared</b>, and this is the one exclusion.
        /// They are the only thing on the context the analytical preparation neither reads nor is affected
        /// by, so a changed step or limit does not make the prepared model a different model - matching on
        /// them would rebuild the whole engineering preparation, and on an isolated run re-derive its
        /// geometry, to change two numbers nothing in that preparation looks at. What they DO affect is what
        /// the run records for <c>Modify.CanOptimise</c> to read afterwards, so the orchestration writes the
        /// current choice onto the reused run through <c>PartORun.AdoptOptimisationSettings</c> instead. This
        /// method stays a pure question about the engineering case and changes nothing.
        /// </para>
        /// <para>
        /// <b>A restored run is never reusable</b>, and not because of a flag: a run reopened from its saved
        /// results carries no preparation context at all, which is exactly the distinction
        /// <c>Modify.CanOptimise</c> refuses on.
        /// </para>
        /// </summary>
        private static bool Reusable(PartORun partORun, PartOWorkflowRequest partOWorkflowRequest)
        {
            if (partORun is null || partOWorkflowRequest?.Option is null || partORun.State != PartORunState.Prepared)
            {
                return false;
            }

            PartOPreparationContext partOPreparationContext = partORun.PreparationContext;
            if (partOPreparationContext is null)
            {
                return false;
            }

            if (partOPreparationContext.PartOIteration != partOWorkflowRequest.PartOIteration)
            {
                return false;
            }

            if (partOPreparationContext.Isolated != partOWorkflowRequest.Isolate)
            {
                return false;
            }

            if (partOPreparationContext.HasVentilationUnitCatalogue != partOWorkflowRequest.SelectVentilationUnit)
            {
                return false;
            }

            //HOW the products were chosen, not merely whether any were. A preparation made under
            //"automatic - all catalogue products" holds the products that rule picked; reusing it for a
            //request that has since narrowed the pool, or handed authority to the engineer, would simulate
            //the OLD selection while the dialog reported the new one. Nothing would fail - the answer would
            //just be about different equipment than the screen claimed.
            //
            //Only where equipment selection is in play at all: on an Iteration 1a request the mode is
            //moot, and refusing reuse over it would repeat a preparation for no difference.
            if (partOWorkflowRequest.SelectVentilationUnit && !EquipmentSelection(partOPreparationContext).Matches(EquipmentSelection(partOWorkflowRequest)))
            {
                return false;
            }

            //And WHAT the project's own test ventilation unit is rated at, for the same reason one step
            //further on. That rating is not a selection input - it is a capability - but it IS the ceiling
            //Iteration 2B stops at for any dwelling assigned to it. A what-if re-rated from 165 to 175 l/s
            //is a different what-if, and reusing a preparation made under the old rating would optimise
            //against the old ceiling while the dialog reported the new one.
            //
            //Absent on both sides is a match: a project that has never stated one, reused by a request that
            //states none either, has nothing to disagree about.
            if (partOWorkflowRequest.SelectVentilationUnit && !Matches(partOPreparationContext.ProjectTestVentilationUnit, partOWorkflowRequest.ProjectTestVentilationUnit))
            {
                return false;
            }

            Dictionary<Guid, string> ventilationStrategies = partOWorkflowRequest.VentilationStrategies();

            if (partOPreparationContext.VentilationStrategies.Count != ventilationStrategies.Count)
            {
                return false;
            }

            foreach (KeyValuePair<Guid, string> keyValuePair in ventilationStrategies)
            {
                if (!partOPreparationContext.VentilationStrategies.TryGetValue(keyValuePair.Key, out string ventilationStrategy) || !string.Equals(ventilationStrategy, keyValuePair.Value, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            //Identity, never name: two dwellings may share a display name.
            HashSet<Guid> guids = [];
            foreach (Zone zone in partOPreparationContext.Zones)
            {
                if (zone is not null)
                {
                    guids.Add(zone.Guid);
                }
            }

            List<Zone> zones_Requested = partOWorkflowRequest.Zones_Dwelling;

            if (guids.Count != zones_Requested.Count)
            {
                return false;
            }

            foreach (Zone zone in zones_Requested)
            {
                if (!guids.Contains(zone.Guid))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// What a prepared run was prepared under. Absent reads as the historic default - automatic over
        /// the whole catalogue - which is what a run prepared before a mode existed was.
        /// </summary>
        private static PartOEquipmentSelection EquipmentSelection(PartOPreparationContext partOPreparationContext)
        {
            return partOPreparationContext?.EquipmentSelection ?? new PartOEquipmentSelection();
        }

        /// <summary>What a request is asking for, read the same way.</summary>
        private static PartOEquipmentSelection EquipmentSelection(PartOWorkflowRequest partOWorkflowRequest)
        {
            return partOWorkflowRequest?.EquipmentSelection ?? new PartOEquipmentSelection();
        }

        /// <summary>
        /// Whether two statements about a project test ventilation unit say the same thing, where either or
        /// both may be absent. Absent and absent agree; absent and stated do not.
        /// </summary>
        private static bool Matches(PartOProjectTestVentilationUnit partOProjectTestVentilationUnit_1, PartOProjectTestVentilationUnit partOProjectTestVentilationUnit_2)
        {
            if (partOProjectTestVentilationUnit_1 is null || partOProjectTestVentilationUnit_2 is null)
            {
                return partOProjectTestVentilationUnit_1 is null && partOProjectTestVentilationUnit_2 is null;
            }

            return partOProjectTestVentilationUnit_1.Matches(partOProjectTestVentilationUnit_2);
        }
    }
}
