// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.UI;
using System;
using System.Collections.Generic;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// <b>The Iteration 3 pipeline rule, pinned where it is enforced.</b> Once a stage refuses, no later
    /// stage may ever be recorded as having run.
    /// <para>
    /// This is the property everything else in the feature leans on: the comparison exists only where
    /// the ledger is complete, and the window presents no Candidate B number where it is not. If the
    /// ledger could be talked into recording a completion after a refusal, both of those guarantees
    /// would quietly become conventions.
    /// </para>
    /// </summary>
    public class PartOIteration3LedgerTests
    {
        private static readonly PartOIteration3Stage[] order =
        [
            PartOIteration3Stage.Input,
            PartOIteration3Stage.ReferenceA,
            PartOIteration3Stage.ReferenceATM59,
            PartOIteration3Stage.SystemScope,
            PartOIteration3Stage.EquipmentResolution,
            PartOIteration3Stage.Materialisation,
            PartOIteration3Stage.ThermalSource,
            PartOIteration3Stage.SystemsConversion,
            PartOIteration3Stage.SystemsSimulation,
            PartOIteration3Stage.ZoneTemperature,
            PartOIteration3Stage.ResultantTemperature,
            PartOIteration3Stage.CandidateBTM59,
            PartOIteration3Stage.Reconciliation,
            PartOIteration3Stage.Comparison,
            PartOIteration3Stage.Persistence,
        ];

        /// <summary>
        /// The declared order IS the pipeline, and the whole feature depends on it - so it is written out
        /// once here rather than read off the enum the code under test also reads. Reordering the enum
        /// changes the pipeline and has to fail this.
        /// </summary>
        [Fact]
        public void Stages_are_declared_in_the_order_they_run()
        {
            Assert.Equal(order, (PartOIteration3Stage[])Enum.GetValues(typeof(PartOIteration3Stage)));
        }

        [Fact]
        public void A_new_ledger_reports_every_stage_as_not_run()
        {
            PartOIteration3Ledger partOIteration3Ledger = new();

            Assert.Equal(order.Length, partOIteration3Ledger.Stages.Count);
            Assert.All(partOIteration3Ledger.Stages, x => Assert.Equal(PartOIteration3StageStatus.NotRun, x.Status));
            Assert.False(partOIteration3Ledger.IsComplete);
            Assert.False(partOIteration3Ledger.IsRefused);
        }

        [Fact]
        public void Every_stage_after_a_refusal_stays_not_run_even_when_the_caller_asks_for_a_completion()
        {
            PartOIteration3Ledger partOIteration3Ledger = new();

            Assert.True(partOIteration3Ledger.Complete(PartOIteration3Stage.Input, "ready"));
            Assert.True(partOIteration3Ledger.Complete(PartOIteration3Stage.ReferenceA, "A"));
            Assert.True(partOIteration3Ledger.Complete(PartOIteration3Stage.ReferenceATM59, "A TM59"));
            Assert.True(partOIteration3Ledger.Refuse(PartOIteration3Stage.SystemScope, "scope", ["a competing design"]));

            //Every later stage, asked for explicitly. Not one of them may be taken.
            for (int i = 4; i < order.Length; i++)
            {
                Assert.False(partOIteration3Ledger.Complete(order[i], "this must not be recorded"));
            }

            Assert.True(partOIteration3Ledger.IsRefused);
            Assert.Equal(PartOIteration3Stage.SystemScope, partOIteration3Ledger.Stage_Refused);
            Assert.False(partOIteration3Ledger.IsComplete);

            for (int i = 4; i < order.Length; i++)
            {
                Assert.Equal(PartOIteration3StageStatus.NotRun, partOIteration3Ledger.State(order[i]).Status);
            }
        }

        [Fact]
        public void A_refusal_carries_its_reasons_verbatim()
        {
            const string reason = "Ventilation system 'MV 1' (0c8f...) carries a design Extract terminal of 13 l/s in 'Plant Room'.";

            PartOIteration3Ledger partOIteration3Ledger = new();

            partOIteration3Ledger.Refuse(PartOIteration3Stage.Input, "refused", [reason, "   ", null]);

            Assert.Equal([reason], partOIteration3Ledger.Reasons);
        }

        [Fact]
        public void A_refusal_with_nothing_to_say_records_that_as_the_defect_it_is()
        {
            PartOIteration3Ledger partOIteration3Ledger = new();

            partOIteration3Ledger.Refuse(PartOIteration3Stage.Materialisation, "refused", null);

            Assert.Single(partOIteration3Ledger.Reasons);
            Assert.Contains("said nothing about why", partOIteration3Ledger.Reasons[0]);
            Assert.Contains("defect", partOIteration3Ledger.Reasons[0]);
        }

        [Fact]
        public void A_stage_cannot_be_recorded_before_the_stage_that_precedes_it()
        {
            PartOIteration3Ledger partOIteration3Ledger = new();

            Assert.False(partOIteration3Ledger.Complete(PartOIteration3Stage.Comparison, "out of order"));
            Assert.Equal(PartOIteration3StageStatus.NotRun, partOIteration3Ledger.State(PartOIteration3Stage.Comparison).Status);
        }

        [Fact]
        public void A_stage_cannot_be_recorded_twice()
        {
            PartOIteration3Ledger partOIteration3Ledger = new();

            Assert.True(partOIteration3Ledger.Complete(PartOIteration3Stage.Input, "first"));
            Assert.False(partOIteration3Ledger.Complete(PartOIteration3Stage.Input, "second"));

            Assert.Equal("first", partOIteration3Ledger.State(PartOIteration3Stage.Input).Detail);
        }

        [Fact]
        public void A_second_refusal_does_not_move_the_refused_stage()
        {
            PartOIteration3Ledger partOIteration3Ledger = new();

            partOIteration3Ledger.Complete(PartOIteration3Stage.Input, "ready");
            partOIteration3Ledger.Refuse(PartOIteration3Stage.ReferenceA, "first", ["first reason"]);

            Assert.False(partOIteration3Ledger.Refuse(PartOIteration3Stage.ReferenceATM59, "second", ["second reason"]));
            Assert.Equal(PartOIteration3Stage.ReferenceA, partOIteration3Ledger.Stage_Refused);
            Assert.Equal(["first reason"], partOIteration3Ledger.Reasons);
        }

        [Fact]
        public void A_ledger_is_complete_only_when_every_stage_completed()
        {
            PartOIteration3Ledger partOIteration3Ledger = new();

            for (int i = 0; i < order.Length; i++)
            {
                Assert.False(partOIteration3Ledger.IsComplete);
                Assert.True(partOIteration3Ledger.Complete(order[i], order[i].ToString()));
            }

            Assert.True(partOIteration3Ledger.IsComplete);
            Assert.False(partOIteration3Ledger.IsRefused);
        }

        /// <summary>
        /// A refused run's artifacts are the ones that attempt genuinely produced, in stage order - and
        /// nothing from a stage that never ran.
        /// </summary>
        [Fact]
        public void Artifacts_are_reported_in_stage_order_and_only_from_stages_that_ran()
        {
            PartOIteration3Ledger partOIteration3Ledger = new();

            partOIteration3Ledger.Complete(PartOIteration3Stage.Input, "ready", ["a.json (created)"]);
            partOIteration3Ledger.Complete(PartOIteration3Stage.ReferenceA, "A");
            partOIteration3Ledger.Complete(PartOIteration3Stage.ReferenceATM59, "A TM59", ["a-TM59.txt (created)"]);
            partOIteration3Ledger.Refuse(PartOIteration3Stage.SystemScope, "scope", ["no"], ["scope.txt (updated)"]);

            Assert.Equal(["a.json (created)", "a-TM59.txt (created)", "scope.txt (updated)"], partOIteration3Ledger.Artifacts);
        }

        /// <summary>
        /// The result drops the comparison unless the ledger is complete - the second, independent place
        /// the same rule is enforced, so a presentation layer cannot be handed one.
        /// </summary>
        [Fact]
        public void A_result_over_a_refused_ledger_carries_no_comparison_even_when_one_is_supplied()
        {
            PartOIteration3Ledger partOIteration3Ledger = new();

            partOIteration3Ledger.Refuse(PartOIteration3Stage.Input, "no", ["no"]);

            PartOIteration3Comparison partOIteration3Comparison = PartOIteration3ComparisonTests.Comparison();

            Assert.NotNull(partOIteration3Comparison);

            PartOIteration3Result partOIteration3Result = new(partOIteration3Ledger, null, partOIteration3Comparison, null, null, null, null, null, false, null);

            Assert.Null(partOIteration3Result.Comparison);
            Assert.False(partOIteration3Result.IsComplete);
            Assert.True(partOIteration3Result.IsRefused);
        }

        /// <summary>The names a person reads come from the enum's own descriptions, never a second spelling.</summary>
        [Fact]
        public void Stage_and_status_names_come_from_the_enum_descriptions()
        {
            PartOIteration3StageState partOIteration3StageState = new(PartOIteration3Stage.CandidateBTM59, PartOIteration3StageStatus.Refused, "detail");

            Assert.Equal(Core.Query.Description(PartOIteration3Stage.CandidateBTM59), partOIteration3StageState.Name);
            Assert.Equal(Core.Query.Description(PartOIteration3StageStatus.Refused), partOIteration3StageState.StatusText);
        }
    }
}
