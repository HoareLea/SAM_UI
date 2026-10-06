// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// One row of the Iteration 2B airflow history: what happened to one room at one iteration, and what
    /// the production TM59 assessment said about it afterwards.
    ///
    /// <para><b><see cref="Type"/> is the column that matters most - it is the EVIDENCE for the row</b></para>
    /// <list type="bullet">
    /// <item><b>BASELINE</b> - where the room started, before any round touched it.</item>
    /// <item><b>TARGETED</b> - an ordinary optimisation round made an engineering decision about this room:
    /// it failed TM59, and the policy asked it for one whole step.</item>
    /// <item><b>DERIVED</b> - the room moved to keep its dwelling balanced. Nobody chose it.</item>
    /// <item><b>SCALED</b> - <b>capacity envelope rows only.</b> The room moved because the <i>complete last
    /// valid design vector</i> was grown proportionally towards the selected unit's capacity ceiling. It was
    /// chosen by the diagnostic and not by the optimisation, and it kept its share of a design rather than
    /// being handed a figure anybody asked for.</item>
    /// </list>
    /// <para>
    /// <b>SCALED exists because TARGETED would be a lie on an envelope row.</b> A history printing the
    /// envelope's rooms as TARGETED would say the optimisation had asked for those figures, and it never
    /// did: it asked for one +5 l/s step on the failing rooms, and the envelope answers a different question
    /// entirely. Merging the two is the same misreading the targeted/derived split exists to prevent.
    /// </para>
    ///
    /// <para><b>What the four airflow columns mean, on each kind of row</b></para>
    /// <list type="table">
    /// <item>
    /// <term><see cref="DesignBefore_Lps"/></term>
    /// <description>The design entering this step. On a CAPACITY ENVELOPE row that is the <b>last valid
    /// ordinary design</b>, several rounds along - not the baseline.</description>
    /// </item>
    /// <item>
    /// <term><see cref="Requested_Lps"/></term>
    /// <description>On TARGETED, the deliberate optimisation request. On SCALED, the proportionally grown
    /// figure the diagnostic asked for at the selected unit's capacity envelope. On BASELINE and DERIVED a
    /// dash - nobody asked.</description>
    /// </item>
    /// <item>
    /// <term><see cref="Achieved_Lps"/></term>
    /// <description>The design actually realised after the complete step, its balancing and its capacity
    /// validation. Printed beside Requested on purpose: a step is adopted at exactly what it asked for or
    /// not at all, so a disagreement here would be visible rather than silent.</description>
    /// </item>
    /// <item>
    /// <term><see cref="Requirement_Lps"/></term>
    /// <description>What Approved Document F requires of the room. <b>Never altered by anything in this
    /// history</b>, on an envelope row least of all.</description>
    /// </item>
    /// </list>
    ///
    /// <para><b><see cref="Stage"/> tells the three kinds of step apart</b></para>
    /// <para>
    /// BASELINE, OPTIMISATION and CAPACITY ENVELOPE. The envelope's rows are a design the all-or-nothing
    /// policy deliberately refuses, evaluated to say what the equipment already bought could support; read
    /// as optimisation rows they would be the run's best result, which they are not. Its <see cref="Run"/>
    /// is <c>MAX</c> rather than a number, for the same reason its results file is <c>-OptMax</c>: a number
    /// would place it in the rounds' sequence, where the last of them is the answer.
    /// </para>
    ///
    /// <para><b>A capacity envelope's rows are complete</b></para>
    /// <para>
    /// The proportional growth targets every space and direction the selected units serve, so every
    /// contribution needed to reconcile a unit's supply and extract duty has its own row - one per space and
    /// direction, aggregated at the same room grain as the rest of the history rather than terminal by
    /// terminal. Summing the visible MAX rows of one direction reproduces that unit's duty with nothing
    /// hidden. An earlier revision could not: for the real Flat 1 it showed the studio's <i>supply</i> and
    /// the bathroom's <i>extract</i>, and left the studio's own 22 l/s extract contribution nowhere in the
    /// table, so a reader could not see where the remaining extract duty came from.
    /// </para>
    /// </summary>
    public class PartOOptimisationAirFlowRow
    {
        private PartOOptimisationAirFlowRow(PartOOptimisationStep partOOptimisationStep, string spaceName, string type, string direction, double before_Lps, double? requested_Lps, double achieved_Lps, double requirement_Lps, TM59ComplianceStatus tM59ComplianceStatus)
        {
            Run = partOOptimisationStep.IsCapacityEnvelope ? "MAX" : partOOptimisationStep.Iteration.ToString();
            Stage = Stages(partOOptimisationStep.Kind);
            Space = spaceName;
            Type = type;
            Direction = direction;
            DesignBefore_Lps = before_Lps;
            Requested_Lps = requested_Lps;
            Achieved_Lps = achieved_Lps;
            Requirement_Lps = requirement_Lps;
            ComplianceStatus = tM59ComplianceStatus;
        }

        /// <summary>Which iteration - 0 is the baseline, and MAX is the diagnostic capacity envelope.</summary>
        public string Run { get; }

        /// <summary>
        /// BASELINE, OPTIMISATION or CAPACITY ENVELOPE. <b>Read this before reading any airflow on the
        /// row</b>: an envelope row is a diagnostic and not a design the optimisation accepted.
        /// </summary>
        public string Stage { get; }

        /// <summary>The room.</summary>
        public string Space { get; }

        /// <summary>
        /// BASELINE, TARGETED, DERIVED or SCALED - the evidence for the row. See the class summary; the
        /// distinction between TARGETED and SCALED in particular is not cosmetic.
        /// </summary>
        public string Type { get; }

        /// <summary>Supply or extract.</summary>
        public string Direction { get; }

        /// <summary>What the room was designed at before this iteration [l/s].</summary>
        public double DesignBefore_Lps { get; }

        /// <summary>What was asked of it [l/s], or null on a baseline or derived row - nobody asked.</summary>
        public double? Requested_Lps { get; }

        /// <summary>What it is designed at after this iteration [l/s].</summary>
        public double Achieved_Lps { get; }

        /// <summary>
        /// What Approved Document F requires of the room [l/s]. Shown so a reader can see at a glance that
        /// the design stayed above the floor - it is never altered by an optimisation.
        /// </summary>
        public double Requirement_Lps { get; }

        /// <summary>The production TM59 verdict for this room after this iteration.</summary>
        public TM59ComplianceStatus ComplianceStatus { get; }

        /// <summary>The verdict as the report words it.</summary>
        public string TM59Status => Core.Query.Description(ComplianceStatus);

        /// <summary>
        /// Every airflow row of a whole optimisation run, iteration by iteration.
        /// <para>
        /// The baseline contributes one row per room the optimisation later touched, so a reader can see
        /// where each of them started. Rooms nothing ever happened to are not listed: their design airflow
        /// is in the model and printing it here would bury the rooms that moved.
        /// </para>
        /// </summary>
        public static List<PartOOptimisationAirFlowRow> Rows(PartOOptimisationRun? partOOptimisationRun)
        {
            List<PartOOptimisationAirFlowRow> result = [];

            if (partOOptimisationRun is null)
            {
                return result;
            }

            result.AddRange(BaselineRows(partOOptimisationRun));

            foreach (PartOOptimisationStep partOOptimisationStep in partOOptimisationRun.Steps)
            {
                if (partOOptimisationStep.IsBaseline)
                {
                    continue;
                }

                //A deliberate adjustment of the CAPACITY ENVELOPE is SCALED and not TARGETED. Both were
                //chosen, but by different authorities answering different questions: an optimisation round
                //asked this room for one +5 l/s step because it failed TM59, and the envelope grew the whole
                //last valid design vector proportionally towards the selected unit's ceiling. Printing the
                //second as the first would claim the optimisation had requested figures it never did.
                string type = partOOptimisationStep.IsCapacityEnvelope ? "SCALED" : "TARGETED";

                Dictionary<string, DesignAirFlowAdjustment> dictionary = [];

                foreach (DesignAirFlowAdjustment designAirFlowAdjustment in partOOptimisationStep.Adjustments())
                {
                    dictionary[Key(designAirFlowAdjustment.SpaceGuid, designAirFlowAdjustment.FlowClassification)] = designAirFlowAdjustment;
                }

                //Every space and direction this run's equipment serves, moved or not - see the class summary.
                foreach (PartODesignAirFlowState partODesignAirFlowState in partOOptimisationStep.DesignAirFlowStates)
                {
                    string key = Key(partODesignAirFlowState.SpaceGuid, partODesignAirFlowState.FlowClassification);

                    if (dictionary.TryGetValue(key, out DesignAirFlowAdjustment designAirFlowAdjustment))
                    {
                        result.Add(Row(partOOptimisationStep, designAirFlowAdjustment, type));

                        dictionary.Remove(key);

                        continue;
                    }

                    //Present in the design and untouched by this step. It is stated rather than omitted:
                    //leaving it out reads as though the direction had been removed, when the ventilation
                    //network still carries it and the dwelling is balanced around it. Design before and
                    //achieved are the same figure because nothing moved, and nothing was requested of it.
                    result.Add(new PartOOptimisationAirFlowRow(
                        partOOptimisationStep,
                        partODesignAirFlowState.SpaceName,
                        "UNCHANGED",
                        Core.Query.Description(partODesignAirFlowState.FlowClassification),
                        partODesignAirFlowState.Design_Lps,
                        null,
                        partODesignAirFlowState.Design_Lps,
                        partODesignAirFlowState.Requirement_Lps,
                        Status(partOOptimisationStep, partODesignAirFlowState.SpaceGuid)));
                }

                //Anything adjusted that the recorded vector does not account for is still printed. A step
                //recorded before the vector existed has none, which is what keeps such a run readable; and
                //an adjustment to a room the vector could not read must not vanish from the evidence.
                foreach (DesignAirFlowAdjustment designAirFlowAdjustment in partOOptimisationStep.Adjustments())
                {
                    if (!dictionary.ContainsKey(Key(designAirFlowAdjustment.SpaceGuid, designAirFlowAdjustment.FlowClassification)))
                    {
                        continue;
                    }

                    result.Add(Row(partOOptimisationStep, designAirFlowAdjustment, type));
                }
            }

            return result;
        }

        /// <summary>One adjusted row, TARGETED/SCALED or DERIVED according to which authority moved it.</summary>
        private static PartOOptimisationAirFlowRow Row(PartOOptimisationStep partOOptimisationStep, DesignAirFlowAdjustment designAirFlowAdjustment, string type_Targeted)
        {
            return new PartOOptimisationAirFlowRow(
                partOOptimisationStep,
                designAirFlowAdjustment.SpaceName,
                designAirFlowAdjustment.IsDerived ? "DERIVED" : type_Targeted,
                Core.Query.Description(designAirFlowAdjustment.FlowClassification),
                designAirFlowAdjustment.Before_Lps,
                designAirFlowAdjustment.IsDerived ? null : designAirFlowAdjustment.After_Lps,
                designAirFlowAdjustment.After_Lps,
                designAirFlowAdjustment.Requirement_Lps,
                Status(partOOptimisationStep, designAirFlowAdjustment.SpaceGuid));
        }

        /// <summary>One room and one direction, matched on identity rather than on the printed name.</summary>
        private static string Key(System.Guid spaceGuid, FlowClassification flowClassification)
        {
            return string.Format("{0}|{1}", spaceGuid, flowClassification);
        }

        /// <summary>
        /// Where every room the optimisation later touched started, as run 0.
        ///
        /// <para><b>Why these are synthesised rather than recorded</b></para>
        /// <para>
        /// The baseline is the Iteration 2 design as it stood: it has no targets and no round, so it holds
        /// no adjustments to read rows from. But a before-and-after history that begins at run 1 cannot be
        /// read as a before-and-after history - a reader looking at "Kitchen_4 55 -> 60" in run 1 has to
        /// take on trust that 55 was where it started, and a room whose first move is in run 4 has no
        /// stated origin at all.
        /// </para>
        /// <para>
        /// Each room's baseline design airflow is exactly the <c>Before_Lps</c> of the FIRST adjustment
        /// that ever moved it, and that is a recorded fact rather than an inference: the round that made
        /// that adjustment read the value off the design it was given. The baseline TM59 verdict comes from
        /// the baseline step's own production results. Nothing here is computed.
        /// </para>
        /// <para>
        /// Rooms nothing ever happened to are deliberately not listed - their design airflow is in the
        /// model, and printing every room would bury the ones that moved.
        /// </para>
        /// </summary>
        private static List<PartOOptimisationAirFlowRow> BaselineRows(PartOOptimisationRun partOOptimisationRun)
        {
            List<PartOOptimisationAirFlowRow> result = [];

            PartOOptimisationStep? partOOptimisationStep_Baseline = partOOptimisationRun.Step_Baseline;
            if (partOOptimisationStep_Baseline is null)
            {
                return result;
            }

            HashSet<string> keys = [];

            //The baseline's OWN recorded vector, where the run has one: every space and direction the
            //equipment serves, at the design the baseline actually carried. Read directly rather than
            //reconstructed from later rounds, so a direction no round ever moved is present too - which is
            //the whole complaint the synthesis below could not answer.
            foreach (PartODesignAirFlowState partODesignAirFlowState in partOOptimisationStep_Baseline.DesignAirFlowStates)
            {
                if (!keys.Add(Key(partODesignAirFlowState.SpaceGuid, partODesignAirFlowState.FlowClassification)))
                {
                    continue;
                }

                result.Add(new PartOOptimisationAirFlowRow(
                    partOOptimisationStep_Baseline,
                    partODesignAirFlowState.SpaceName,
                    "BASELINE",
                    Core.Query.Description(partODesignAirFlowState.FlowClassification),
                    partODesignAirFlowState.Design_Lps,
                    null,
                    partODesignAirFlowState.Design_Lps,
                    partODesignAirFlowState.Requirement_Lps,
                    Status(partOOptimisationStep_Baseline, partODesignAirFlowState.SpaceGuid)));
            }

            foreach (PartOOptimisationStep partOOptimisationStep in partOOptimisationRun.Steps)
            {
                //The ENVELOPE is excluded as well as the baseline. Its Before_Lps is the last ACCEPTED
                //design's airflow, not the baseline's, so a room whose first ever movement is in the
                //envelope would otherwise contribute a "baseline" row stating a figure the baseline never
                //carried. Its own rows print that Before honestly, beside its own stage.
                if (!partOOptimisationStep.IsOptimisationRound)
                {
                    continue;
                }

                //Targeted first, then derived - the order the round itself settled on, so the baseline
                //block reads in the same order as the rounds beneath it.
                foreach (DesignAirFlowAdjustment designAirFlowAdjustment in partOOptimisationStep.Adjustments())
                {
                    if (!keys.Add(Key(designAirFlowAdjustment.SpaceGuid, designAirFlowAdjustment.FlowClassification)))
                    {
                        continue;
                    }

                    result.Add(new PartOOptimisationAirFlowRow(
                        partOOptimisationStep_Baseline,
                        designAirFlowAdjustment.SpaceName,
                        "BASELINE",
                        Core.Query.Description(designAirFlowAdjustment.FlowClassification),
                        designAirFlowAdjustment.Before_Lps,
                        null,
                        designAirFlowAdjustment.Before_Lps,
                        designAirFlowAdjustment.Requirement_Lps,
                        Status(partOOptimisationStep_Baseline, designAirFlowAdjustment.SpaceGuid)));
                }
            }

            return result;
        }

        /// <summary>
        /// The stage label, in the words the grid shows - the three things a reader has to tell apart, taken
        /// from what the step says it is rather than from its number.
        /// </summary>
        private static string Stages(PartOOptimisationStepKind partOOptimisationStepKind)
        {
            return partOOptimisationStepKind switch
            {
                PartOOptimisationStepKind.Baseline => "BASELINE",
                PartOOptimisationStepKind.CapacityEnvelope => "CAPACITY ENVELOPE",
                _ => "OPTIMISATION",
            };
        }

        /// <summary>
        /// The production verdict for one design space at one iteration - the mechanical criterion where
        /// there is one, and any failure winning over a pass.
        /// <para>
        /// <b>Read off the assessment's own statuses, never derived from Actual and Limit</b>, which differ
        /// in whether a zero margin passes. Combining is only ever "a Fail beats a Pass", which is the same
        /// rule the production report combines by.
        /// </para>
        /// </summary>
        private static TM59ComplianceStatus Status(PartOOptimisationStep partOOptimisationStep, System.Guid guid)
        {
            TM59ComplianceStatus result = TM59ComplianceStatus.Undefined;

            foreach (PartOTM59SpaceResult partOTM59SpaceResult in partOOptimisationStep.TM59Results)
            {
                if (partOTM59SpaceResult.SpaceGuid_Design != guid)
                {
                    continue;
                }

                if (partOTM59SpaceResult.ComplianceStatus == TM59ComplianceStatus.Fail)
                {
                    return TM59ComplianceStatus.Fail;
                }

                if (partOTM59SpaceResult.ComplianceStatus == TM59ComplianceStatus.Pass)
                {
                    result = TM59ComplianceStatus.Pass;
                }
            }

            return result;
        }
    }
}
