// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Query
    {
        /// <summary>
        /// Which rooms one Iteration 2B round deliberately targets, and by how much.
        ///
        /// <para><b>The v1 policy, in full</b></para>
        /// <list type="bullet">
        /// <item>Only rooms the <b>production</b> TM59 assessment failed on its <b>mechanical</b> criterion.
        /// A passing room is never a target - raising a design airflow nobody's assessment asked for is not
        /// an optimisation, it is a change. A naturally ventilated failure is not one either: it is a real
        /// problem, and raising a mechanical design airflow is not how it is solved.</item>
        /// <item>Only rooms inside the <b>current Part O dwelling scope</b>. A communal corridor, and the
        /// simulation-only zones the preparation builds for the air handling units, are both outside it -
        /// and are excluded by scope rather than by name, so nothing here depends on what anybody called
        /// them.</item>
        /// <item>Supply terminals only &#8594; <b>Supply +step</b>. Extract terminals only &#8594;
        /// <b>Extract +step</b>. Both &#8594; <b>Supply +step</b>, which is the settled v1 choice: a room
        /// that overheats is relieved by moving more air into it, and raising both sides in one round would
        /// make the two contributions impossible to read apart afterwards.</item>
        /// <item>No Approved Document O design terminal at all &#8594; <b>not automatically optimisable</b>,
        /// named with its reason. No terminal is created: that would size a duty the Approved Document F
        /// assessment never asked for.</item>
        /// </list>
        ///
        /// <para><b>The step is added to the design, never to the requirement or the capacity</b></para>
        /// <para>
        /// A target is the room's <i>current design</i> airflow plus the step. It is not the Approved
        /// Document F requirement plus the step - that would discard whatever design headroom the room
        /// already had - and it is emphatically not anything derived from the selected unit's rating, which
        /// is a ceiling the round is checked against and never a figure to grow towards.
        /// </para>
        ///
        /// <para><b>Deterministic</b></para>
        /// <para>
        /// Targets come out sorted by room guid, so the same failing set produces the same round whatever
        /// order the assessment happened to report it in. The round itself is order independent anyway;
        /// this makes the <i>report</i> stable too.
        /// </para>
        /// </summary>
        /// <param name="analyticalModel">
        /// The design model the round will be evaluated against - the last valid design, whose terminals
        /// carry the airflows the step is added to.
        /// </param>
        /// <param name="partOTM59SpaceResults">
        /// The production TM59 outcomes for that design, one per criterion per resolved design space -
        /// <c>PartOTM59Assessment.SpaceResults</c>. Taken as results rather than as the assessment object so
        /// that what this policy does with a given set of verdicts can be stated and tested without a TAS
        /// simulation standing behind it.
        /// </param>
        /// <param name="zones_Dwelling">
        /// The Part O dwelling scope this run was prepared over, resolved by guid against
        /// <paramref name="analyticalModel"/>. Anything outside it is not a target.
        /// </param>
        /// <param name="airFlowStep_Lps">How much each targeted room's design airflow is raised [l/s].</param>
        public static PartOOptimisationTargetSelection PartOOptimisationTargets(AnalyticalModel? analyticalModel, IEnumerable<PartOTM59SpaceResult>? partOTM59SpaceResults, IEnumerable<Zone>? zones_Dwelling, double airFlowStep_Lps)
        {
            PartOOptimisationTargetSelection result = new();

            AdjacencyCluster? adjacencyCluster = analyticalModel?.AdjacencyCluster;
            if (adjacencyCluster is null || partOTM59SpaceResults is null)
            {
                return result;
            }

            HashSet<Guid> guids_Scope = PartODwellingSpaceGuids(adjacencyCluster, zones_Dwelling);

            List<Guid> guids_Failing = FailingMechanicalSpaces(partOTM59SpaceResults);

            //Sorted so the round's report does not depend on the order the assessment reported failures in.
            guids_Failing.Sort();

            foreach (Guid guid in guids_Failing)
            {
                //The cluster's own O(1) authority. This was GetSpaces().Find - which rebuilds the model's
                //whole space list and then walks it - taken once per failing room, so a round that failed
                //widely on a block-scale model rebuilt that list once per failure. GetObject<Space> answers
                //exactly what that Find answered: GetSpaces() is the same object dictionary flattened into a
                //list, and the guid a space is stored under is the guid the Find compared. Nothing is
                //snapshotted, so nothing can go stale against the model being optimised.
                Space? space = adjacencyCluster.GetObject<Space>(guid);
                if (space is null)
                {
                    result.NotOptimisable.Add(string.Format("A failing TM59 result resolved to a design space ({0}) that is not in the model being optimised, so it could not be targeted.", guid));

                    continue;
                }

                if (!guids_Scope.Contains(guid))
                {
                    //By scope, never by name: the air handling units' simulation zones and a communal
                    //corridor are both outside the Part O dwelling scope, and that is the whole reason
                    //neither is optimised.
                    result.NotOptimisable.Add(string.Format("Space '{0}' fails its TM59 criterion but is outside the current Part O dwelling scope, so it is not an automatic optimisation target.", space.Name));

                    continue;
                }

                double supply_Lps = Design_Lps(adjacencyCluster, space, FlowClassification.Supply);
                double extract_Lps = Design_Lps(adjacencyCluster, space, FlowClassification.Extract);

                bool hasSupply = !double.IsNaN(supply_Lps);
                bool hasExtract = !double.IsNaN(extract_Lps);

                if (!hasSupply && !hasExtract)
                {
                    result.NotOptimisable.Add(string.Format("Space '{0}' fails its TM59 criterion but has no Approved Document O design supply or extract terminal to raise, so it is not automatically optimisable. A terminal was not created for it - that would size a duty the Approved Document F assessment never asked for.", space.Name));

                    continue;
                }

                //Both sides present: SUPPLY, for Iteration 2B v1. Raising both in one round would make the
                //two contributions impossible to read apart in the next assessment.
                FlowClassification flowClassification = hasSupply ? FlowClassification.Supply : FlowClassification.Extract;

                double design_Lps = hasSupply ? supply_Lps : extract_Lps;

                result.Targets.Add(new DesignAirFlowTarget(space, flowClassification, design_Lps + airFlowStep_Lps));
            }

            return result;
        }

        /// <summary>
        /// The design spaces whose <b>mechanical</b> criterion the production assessment failed - the only
        /// rooms an Iteration 2B design airflow optimisation may consider targeting.
        /// <para>
        /// Mechanical only: a naturally ventilated failure is a real problem, and raising a mechanical
        /// design airflow is not how it is solved. One entry per design space however many criteria it
        /// failed, because a room is raised once per round whatever the reason.
        /// </para>
        /// <para>
        /// The verdict read is the production one. Nothing here re-derives a pass or a fail from an Actual
        /// and a Limit.
        /// </para>
        /// </summary>
        private static List<Guid> FailingMechanicalSpaces(IEnumerable<PartOTM59SpaceResult> partOTM59SpaceResults)
        {
            List<Guid> result = [];

            foreach (PartOTM59SpaceResult partOTM59SpaceResult in partOTM59SpaceResults)
            {
                if (partOTM59SpaceResult is not null && partOTM59SpaceResult.Mechanical && partOTM59SpaceResult.IsFail && !result.Contains(partOTM59SpaceResult.SpaceGuid_Design))
                {
                    result.Add(partOTM59SpaceResult.SpaceGuid_Design);
                }
            }

            return result;
        }

        /// <summary>
        /// Every design space inside the Part O dwelling scope this run was prepared over, resolved through
        /// the model's own zone relations.
        /// <para>
        /// The zones are matched by guid rather than reused as objects: the model being optimised is a later
        /// generation than the one the scope was chosen on, and only the identity survives that.
        /// </para>
        /// </summary>
        internal static HashSet<Guid> PartODwellingSpaceGuids(AdjacencyCluster adjacencyCluster, IEnumerable<Zone>? zones_Dwelling)
        {
            HashSet<Guid> result = [];

            foreach (Zone zone_Dwelling in zones_Dwelling ?? [])
            {
                //The cluster's own O(1) authority, in place of a linear walk of its whole zone list per
                //requested dwelling - which on a block whose dwelling count grows with its room count was
                //quadratic in the zones. Same answer: the zone list is that object dictionary flattened, and
                //the guid a zone is stored under is the guid the Find compared.
                Zone? zone = zone_Dwelling is null ? null : adjacencyCluster.GetObject<Zone>(zone_Dwelling.Guid);
                if (zone is null)
                {
                    continue;
                }

                foreach (Space space in adjacencyCluster.GetRelatedObjects<Space>(zone) ?? [])
                {
                    if (space is not null)
                    {
                        result.Add(space.Guid);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// A room's current design airflow on one side [l/s], or <see cref="double.NaN"/> where it carries
        /// no design terminal of that direction at all.
        /// <para>
        /// NaN rather than zero, deliberately: a room with no terminal and a room with a terminal designed
        /// at nothing are different situations, and only the second one is something an optimisation could
        /// raise.
        /// </para>
        /// </summary>
        private static double Design_Lps(AdjacencyCluster adjacencyCluster, Space space, FlowClassification flowClassification)
        {
            List<VentilationTerminal> ventilationTerminals = Analytical.Query.VentilationTerminals(adjacencyCluster.VentilationTerminals(space), flowClassification) ?? [];

            return ventilationTerminals.Count == 0 ? double.NaN : ventilationTerminals.VentilationTerminalDesignDuty_Lps(flowClassification) ?? 0;
        }
    }
}
