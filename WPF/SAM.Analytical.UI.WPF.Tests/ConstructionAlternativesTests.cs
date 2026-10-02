// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Tas;
using SAM.Analytical.UI.WPF.Tests.Helpers;
using SAM.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// Thermal Stage D1: next to the generated thickness variant, the existing constructions that meet the target or come close - the
    /// model's own and a library's - each with its U-value on the row's basis, where it comes from and what choosing it does. The U-values
    /// are calculated in ONE batch per pool through the existing Tas calculator and cached; listing changes nothing in the model; nothing is
    /// chosen for the user; an alternative blocked by a material that differs from the model's cannot be applied.
    /// </summary>
    public class ConstructionAlternativesTests
    {
        private sealed class Setup : IDisposable
        {
            public AnalyticalModel Model;
            public Construction Current, Thick, Medium, Thin;
            public UValueViewModel UValue;
            public ConstructionAlternatives Alternatives;
            public FakeConstructionUValueEvaluator Evaluator;
            public ConstructionUValueCache Cache;
            public List<Guid> WallPanels;

            public void Dispose()
            {
                Alternatives?.Dispose();
                UValue?.Dispose();
            }
        }

        private static Setup Create(string target = "0.18", GlazingSource library = null, int roofs = 0, FakeConstructionUValueEvaluator evaluator = null, ConstructionUValueCache cache = null, IEnumerable<int> selected = null)
        {
            Setup setup = new Setup();
            setup.Model = AlternativesFixture.Model(out setup.Current, out setup.Thick, out setup.Medium, out setup.Thin, roofs: roofs);
            setup.WallPanels = setup.Model.AdjacencyCluster.GetPanels(setup.Current).Select(x => x.Guid).ToList();
            setup.Evaluator = evaluator ?? new FakeConstructionUValueEvaluator();
            setup.Cache = cache ?? new ConstructionUValueCache();

            List<Guid> selectedGuids = selected == null ? new List<Guid>() : selected.Select(x => setup.WallPanels[x]).ToList();
            setup.UValue = new UValueViewModel(setup.Model, setup.Current.Guid, selectedGuids, new ImmediateUValueEvaluator());
            setup.UValue.TargetText = target;
            setup.UValue.LastEvaluationTask.Wait(TimeSpan.FromSeconds(10));

            GlazingSource source = library ?? AlternativesFixture.Library();
            setup.Alternatives = new ConstructionAlternatives(setup.Model, setup.UValue, setup.Evaluator, setup.Cache, () => source);
            setup.Alternatives.Refresh();
            setup.Alternatives.Idle().Wait(TimeSpan.FromSeconds(10));
            return setup;
        }

        private static string[] Names(Setup setup)
        {
            return setup.Alternatives.Rows.Select(x => x.Name).ToArray();
        }

        // ---- The list ----------------------------------------------------------------------------------------------

        [Fact]
        public void The_generated_variant_comes_first_then_the_existing_constructions_that_meet_the_target_or_come_close()
        {
            using (Setup setup = Create())
            {
                // Target 0.18: meets = MODEL_THICK (library copy) 0.177, LIB_THICK 0.171, LIB_ROOF 0.165, LIB_AEROGEL 0.160; close (within 10 %, <= 0.198) =
                // MODEL_THICK (model) 0.1835. Not listed: MODEL_MED 0.215, MODEL_THIN 0.444, LIB_MISSING (no U-value), the current construction.
                ConstructionAlternativeRow generated = setup.Alternatives.Rows[0];
                Assert.True(generated.IsGenerated);
                Assert.Equal(ConstructionAlternativeKind.Generated, generated.Kind);
                Assert.Equal(0.18, generated.ThermalTransmittance, 3);
                Assert.Equal("Generated", generated.KindText);
                Assert.True(generated.CanApply);

                string[] existing = setup.Alternatives.Rows.Skip(1).Select(x => x.Name + "|" + x.KindText).ToArray();
                Assert.Equal(new[]
                {
                    "MODEL_THICK|Library",       // meets, margin 0.003: the closest to the target from below
                    "LIB_THICK|Library",         // meets, margin 0.009
                    "LIB_ROOF|Library",          // meets, margin 0.015
                    "LIB_AEROGEL|Library",       // meets, margin 0.020
                    "MODEL_THICK|Existing model" // does not meet (0.1835) but is within 10 %
                }, existing);

                Assert.DoesNotContain(setup.Alternatives.Rows, x => x.Name == "MODEL_MED" || x.Name == "MODEL_THIN" || x.Name == "LIB_MISSING" || x.Name == setup.Current.Name);
                Assert.Equal(5, setup.Alternatives.ExistingCount);
                Assert.Equal("4 existing constructions meet U 0.18; 1 more is within 10 %; 1 could not be calculated.", setup.Alternatives.CountText);
            }
        }

        [Fact]
        public void Each_line_says_what_it_is_its_U_value_where_it_comes_from_and_how_the_model_uses_it()
        {
            using (Setup setup = Create())
            {
                ConstructionAlternativeRow model = setup.Alternatives.Rows.Single(x => x.Kind == ConstructionAlternativeKind.Model);
                Assert.Equal("MODEL_THICK", model.Name);
                Assert.Equal(UValueFixture.U(0.12), model.ThermalTransmittance, 6);
                Assert.Equal("U 0.183", model.UText);
                Assert.Equal("0.003 above the target", model.StatusText);
                Assert.False(model.Meets);
                Assert.Equal(1, model.UsedBy);
                Assert.Equal("Existing model · used by 1 panel", model.OriginText);
                Assert.Equal("Model", model.SourceLabel);
                Assert.Contains("120 I01_Mineral Wool", model.BuildUp);

                ConstructionAlternativeRow library = setup.Alternatives.Rows.Single(x => x.Name == "LIB_THICK");
                Assert.Equal(ConstructionAlternativeKind.Library, library.Kind);
                Assert.True(library.Meets);
                Assert.Equal("meets the target by 0.009", library.StatusText);
                Assert.Equal("Library · not in the model yet", library.OriginText);
                Assert.Equal("Default library", library.SourceLabel);
                Assert.Equal(0, library.UsedBy);
                Assert.Equal(AlternativesFixture.LibraryThickGuid, library.Guid);
            }
        }

        [Fact]
        public void An_alternative_made_for_another_panel_group_a_clashing_name_and_missing_materials_are_noted_before_it_is_chosen()
        {
            using (Setup setup = Create())
            {
                ConstructionAlternativeRow roof = setup.Alternatives.Rows.Single(x => x.Name == "LIB_ROOF");
                Assert.Contains(roof.Warnings, x => x.Contains("made for roofs") && x.Contains("sit in walls"));
                Assert.True(roof.CanApply);

                ConstructionAlternativeRow aerogel = setup.Alternatives.Rows.Single(x => x.Name == "LIB_AEROGEL");
                Assert.Contains(aerogel.Warnings, x => x.Contains("Adds 1 material to the model: Aerogel"));

                // The library's MODEL_THICK shares its name with a model construction: it is added under a numbered name.
                ConstructionAlternativeRow clash = setup.Alternatives.Rows.Single(x => x.Name == "MODEL_THICK" && x.Kind == ConstructionAlternativeKind.Library);
                Assert.Contains(clash.Warnings, x => x.Contains("already has a construction named MODEL_THICK"));
                Assert.StartsWith("⚠ ", clash.WarningText);
                Assert.Contains("MODEL_THICK", clash.Tooltip);
            }
        }

        [Fact]
        public void An_existing_construction_whose_material_differs_from_the_models_is_blocked_with_the_reason()
        {
            using (Setup setup = Create(library: AlternativesFixture.LibraryWithDifferentWool()))
            {
                ConstructionAlternativeRow row = setup.Alternatives.Rows.Single(x => x.Name == "LIB_THICK");
                Assert.False(row.CanApply);
                Assert.Contains("I01_Mineral Wool", row.BlockReason);
                Assert.Contains("differs from the model's material", row.BlockReason);

                setup.Alternatives.SelectedRow = row;
                Assert.True(setup.Alternatives.ExistingChosen);
                Assert.False(setup.Alternatives.ApplyEnabled);
                Assert.Contains("differs from the model's material", setup.Alternatives.ApplyBlockReason);
                Assert.Null(setup.Alternatives.CreateRequest());
            }
        }

        [Fact]
        public void A_target_the_generated_variant_cannot_reach_still_lists_the_existing_constructions_that_do()
        {
            // Below what the wool range reaches (U 0.0246 at 1 m, more than the calculator's tolerance away): no thickness gives it, and no existing construction is near it either.
            using (Setup setup = Create(target: "0.01"))
            {
                Assert.False(setup.Alternatives.Rows[0].CanApply);
                Assert.Equal(UValuePreviewStatus.Unreachable, setup.UValue.Status);
                Assert.Equal(1, setup.Alternatives.Rows.Count(x => x.IsGenerated));
                Assert.Equal(ConstructionAlternativesStatus.Ready, setup.Alternatives.Status);
                Assert.Equal("No existing construction meets U 0.01 or is within 10 % of it.", setup.Alternatives.CountText);
            }
        }

        // ---- Batch and cache ---------------------------------------------------------------------------------------

        [Fact]
        public void The_existing_constructions_are_calculated_in_one_batch_per_pool_not_one_call_each()
        {
            using (Setup setup = Create())
            {
                // Model pool: MODEL_THICK, MODEL_MED, MODEL_THIN (the current construction is not asked). Library pool: 4 - LIB_MISSING names a
                // material the library lacks, so it cannot be keyed and is not sent.
                Assert.Equal(2, setup.Evaluator.Requests.Count);
                Assert.Equal(new[] { 3, 4 }, setup.Evaluator.Requests.Select(x => x.Constructions.Count).OrderBy(x => x).ToArray());
                Assert.DoesNotContain(setup.Evaluator.Requests.SelectMany(x => x.Constructions), x => x.Guid == setup.Current.Guid);
                Assert.Equal(2, setup.Evaluator.FakeTas.ThermalTransmittanceCalls);
            }
        }

        [Fact]
        public void A_second_target_or_a_second_row_never_asks_the_evaluator_again_for_what_the_cache_has()
        {
            using (Setup setup = Create())
            {
                int requests = setup.Evaluator.Requests.Count;
                int calculated = setup.Cache.Count;
                Assert.Equal(7, calculated); // 3 model + 4 library

                setup.UValue.TargetText = "0.25";
                setup.UValue.LastEvaluationTask.Wait(TimeSpan.FromSeconds(10));
                setup.Alternatives.Refresh();
                setup.Alternatives.Idle().Wait(TimeSpan.FromSeconds(10));

                Assert.Equal(requests, setup.Evaluator.Requests.Count);
                Assert.Equal(calculated, setup.Cache.Count);
                Assert.Contains(setup.Alternatives.Rows, x => x.Name == "MODEL_MED");

                // Another row of the session with the same cache: the same constructions are not calculated again.
                Setup other = Create(target: "0.30", cache: setup.Cache);
                using (other)
                {
                    Assert.Equal(0, other.Evaluator.Requests.Count);
                    Assert.True(other.Cache.Hits > 0);
                }
            }
        }

        [Fact]
        public void A_changed_heat_flow_basis_asks_again_because_the_value_is_for_another_basis()
        {
            using (Setup setup = Create())
            {
                int requests = setup.Evaluator.Requests.Count;

                setup.UValue.HeatFlowDirectionOverride = HeatFlowDirection.Up;
                setup.UValue.LastEvaluationTask.Wait(TimeSpan.FromSeconds(10));
                setup.Alternatives.Refresh();
                setup.Alternatives.Idle().Wait(TimeSpan.FromSeconds(10));

                Assert.Equal(requests * 2, setup.Evaluator.Requests.Count);
                Assert.All(setup.Evaluator.Requests.Skip(requests), x => Assert.Equal(HeatFlowDirection.Up, x.HeatFlowDirection));
            }
        }

        [Fact]
        public void The_cache_key_is_the_content_that_decides_the_U_value()
        {
            MaterialLibrary materials = UValueFixture.Materials();
            Construction a = AlternativesFixture.Wall("A", 0.08);
            Construction b = AlternativesFixture.Wall("B (other name and guid)", 0.08);
            Construction c = AlternativesFixture.Wall("C", 0.09);

            string key_A = ConstructionUValueCache.Key(a, materials, HeatFlowDirection.Horizontal, true);
            Assert.NotNull(key_A);
            Assert.Equal(key_A, ConstructionUValueCache.Key(b, materials, HeatFlowDirection.Horizontal, true));
            Assert.NotEqual(key_A, ConstructionUValueCache.Key(c, materials, HeatFlowDirection.Horizontal, true));
            Assert.NotEqual(key_A, ConstructionUValueCache.Key(a, materials, HeatFlowDirection.Up, true));
            Assert.NotEqual(key_A, ConstructionUValueCache.Key(a, materials, HeatFlowDirection.Horizontal, false));

            // A changed material definition is another key; a missing material cannot be keyed.
            Assert.NotEqual(key_A, ConstructionUValueCache.Key(a, AlternativesFixture.LibraryMaterials(differentWool: true), HeatFlowDirection.Horizontal, true));
            Assert.Null(ConstructionUValueCache.Key(AlternativesFixture.Wall("G", 0.08, woolMaterial: "Ghost"), materials, HeatFlowDirection.Horizontal, true));
        }

        [Fact]
        public void Listing_the_alternatives_changes_nothing_in_the_model()
        {
            Setup setup = Create();
            using (setup)
            {
                string before = setup.Model.ToJsonObject().ToJsonString();

                setup.Alternatives.Refresh();
                setup.Alternatives.SelectedRow = setup.Alternatives.Rows.Single(x => x.Name == "LIB_AEROGEL");
                setup.Alternatives.CreateRequest();

                Assert.Equal(before, setup.Model.ToJsonObject().ToJsonString());
                // The library's construction and its Aerogel material are outside the model.
                Assert.Null(setup.Model.MaterialLibrary.GetMaterial(AlternativesFixture.Aerogel));
                Assert.DoesNotContain(setup.Model.AdjacencyCluster.GetConstructions(), x => x.Name == "LIB_AEROGEL");
            }
        }

        // ---- Failure and idle --------------------------------------------------------------------------------------

        [Fact]
        public void When_Tas_is_unavailable_the_list_says_so_and_the_generated_variant_still_stands()
        {
            FakeTas fakeTas = new FakeTas() { Unavailable = true };
            using (Setup setup = Create(evaluator: new FakeConstructionUValueEvaluator(fakeTas)))
            {
                Assert.Equal(ConstructionAlternativesStatus.Failed, setup.Alternatives.Status);
                Assert.False(string.IsNullOrEmpty(setup.Alternatives.CountText));
                Assert.Single(setup.Alternatives.Rows);
                Assert.True(setup.Alternatives.Rows[0].IsGenerated);
                Assert.False(setup.Alternatives.ExistingChosen);
                Assert.Equal(0, setup.Cache.Count);
            }
        }

        [Fact]
        public void Without_a_target_there_is_no_list_and_nothing_is_asked()
        {
            using (Setup setup = Create(target: ""))
            {
                Assert.Equal(ConstructionAlternativesStatus.Idle, setup.Alternatives.Status);
                Assert.Empty(setup.Alternatives.Rows);
                Assert.Equal(0, setup.Evaluator.Requests.Count);
                Assert.Equal(string.Empty, setup.Alternatives.CountText);
            }
        }

        // ---- Choosing ----------------------------------------------------------------------------------------------

        [Fact]
        public void Nothing_is_chosen_for_the_user_and_choosing_an_existing_construction_builds_its_request()
        {
            using (Setup setup = Create(selected: new[] { 0, 1 }))
            {
                // No automatic choice, however close a construction is: the generated variant stays the row's change.
                Assert.False(setup.Alternatives.ExistingChosen);
                Assert.Null(setup.Alternatives.SelectedGuid);
                Assert.True(setup.Alternatives.SelectedRow.IsGenerated);
                Assert.Null(setup.Alternatives.CreateRequest());
                Assert.NotNull(setup.UValue.CreateRequest());

                setup.Alternatives.SelectedRow = setup.Alternatives.Rows.Single(x => x.Name == "LIB_AEROGEL");
                Assert.True(setup.Alternatives.ExistingChosen);
                Assert.True(setup.Alternatives.ApplyEnabled);
                Assert.Contains("→", setup.Alternatives.PreviewText);
                Assert.Contains("LIB_AEROGEL (Library)", setup.Alternatives.PreviewText);
                Assert.StartsWith("Assigns LIB_AEROGEL to 6 panels; SIM_EXT_SLD stays unchanged.", setup.Alternatives.ResultText);

                SetConstructionRequest request = setup.Alternatives.CreateRequest();
                Assert.NotNull(request);
                Assert.Equal(setup.Current.Guid, request.SourceConstructionGuid);
                Assert.Equal(AlternativesFixture.LibraryAerogelGuid, request.Construction.Guid);
                Assert.Equal(new[] { AlternativesFixture.Aerogel }, request.MaterialsToAdd.Select(x => x.Name).ToArray());
                Assert.Equal(ThermalApplyScope.AllUsing, request.Scope);
                Assert.Equal(2, request.SelectedPanelGuids.Count());
                Assert.Equal(UValueFixture.U(0.14), request.NewThermalTransmittance, 6);
                Assert.Equal(0.18, request.TargetThermalTransmittance, 6);
                Assert.Equal("Default library", request.SourceLabel);
                Assert.Equal(GlazingSourceKind.Library, request.SourceKind);

                // Back to the generated variant.
                setup.Alternatives.SelectedRow = setup.Alternatives.Rows[0];
                Assert.False(setup.Alternatives.ExistingChosen);
                Assert.Null(setup.Alternatives.CreateRequest());
            }
        }

        [Fact]
        public void The_scope_choice_Keep_name_and_a_model_construction_are_respected()
        {
            using (Setup setup = Create(selected: new[] { 0, 1 }))
            {
                ConstructionAlternativeRow modelRow = setup.Alternatives.Rows.Single(x => x.Kind == ConstructionAlternativeKind.Model);
                setup.Alternatives.SelectedRow = modelRow;
                Assert.True(setup.Alternatives.ApplyEnabled);
                Assert.Empty(setup.Alternatives.CreateRequest().MaterialsToAdd);
                Assert.StartsWith("Assigns MODEL_THICK to 6 panels; SIM_EXT_SLD stays unchanged.", setup.Alternatives.ResultText);
                Assert.DoesNotContain("added to the model", setup.Alternatives.ResultText);

                setup.UValue.ApplyScope = ThermalApplyScope.SelectedOnly;
                Assert.Equal(ThermalApplyScope.SelectedOnly, setup.Alternatives.CreateRequest().Scope);
                Assert.StartsWith("Assigns MODEL_THICK to 2 panels", setup.Alternatives.ResultText);

                // Keep name changes the generated construction itself: it cannot go with an existing one.
                setup.UValue.ApplyScope = ThermalApplyScope.AllUsing;
                setup.UValue.KeepName = true;
                Assert.False(setup.Alternatives.ApplyEnabled);
                Assert.Contains("Keep name", setup.Alternatives.ApplyBlockReason);
                setup.UValue.KeepName = false;
                Assert.True(setup.Alternatives.ApplyEnabled);
            }
        }

        [Fact]
        public void A_choice_that_is_no_longer_listed_after_a_new_target_goes_back_to_the_generated_variant()
        {
            using (Setup setup = Create())
            {
                setup.Alternatives.SelectedRow = setup.Alternatives.Rows.Single(x => x.Name == "LIB_AEROGEL");
                Assert.True(setup.Alternatives.ExistingChosen);

                // Target 0.35: LIB_AEROGEL 0.160 is still below it, so it stays listed (it meets any target above it).
                setup.UValue.TargetText = "0.35";
                setup.UValue.LastEvaluationTask.Wait(TimeSpan.FromSeconds(10));
                setup.Alternatives.Refresh();
                Assert.True(setup.Alternatives.ExistingChosen);

                // Target 0.10: LIB_AEROGEL is 60 % above it - not close - so it is dropped, and so is the choice.
                setup.UValue.TargetText = "0.10";
                setup.UValue.LastEvaluationTask.Wait(TimeSpan.FromSeconds(10));
                setup.Alternatives.Refresh();
                Assert.False(setup.Alternatives.ExistingChosen);
                Assert.Null(setup.Alternatives.SelectedGuid);
                Assert.True(setup.Alternatives.SelectedRow.IsGenerated);
            }
        }

        // ---- The real evaluator over the existing calculator -----------------------------------------------------------

        private static ConstructionUValueRequest Request(int count, bool ghost = false)
        {
            List<Construction> constructions = Enumerable.Range(0, count).Select(i => AlternativesFixture.Wall("W" + i, 0.05 + i * 0.001)).ToList();
            if (ghost)
            {
                constructions.Add(AlternativesFixture.Wall("GHOST", 0.08, woolMaterial: "Ghost"));
            }

            return new ConstructionUValueRequest(constructions, UValueFixture.Materials(), HeatFlowDirection.Horizontal, true);
        }

        [Fact]
        public void The_real_evaluator_sends_many_constructions_to_one_tas_run_per_chunk_and_reads_each_by_guid()
        {
            FakeTas fakeTas = new FakeTas();
            using (TasConstructionUValueEvaluator evaluator = new TasConstructionUValueEvaluator(fakeTas.ThermalTransmittances))
            {
                ConstructionUValueRequest request = Request(TasConstructionUValueEvaluator.ChunkSize + 5);

                IReadOnlyList<ConstructionUValue> results = evaluator.EvaluateAsync(request, CancellationToken.None).Result;

                // 45 constructions: two Tas runs (40 + 5), not 45.
                Assert.Equal(2, fakeTas.ThermalTransmittanceCalls);
                Assert.Equal(45, results.Count);
                Assert.All(request.Constructions, c => Assert.Equal(UValueFixture.U(c.ConstructionLayers[UValueFixture.WoolIndex].Thickness), results.Single(x => x.Guid == c.Guid).ThermalTransmittance, 9));
                Assert.All(results, x => Assert.True(x.Calculated));
                // Tas needs an STA thread.
                Assert.Equal(ApartmentState.STA, fakeTas.LastApartment);
            }
        }

        [Fact]
        public void A_construction_with_a_missing_material_is_reported_not_sent_to_tas_and_a_failed_run_is_reported_per_construction()
        {
            FakeTas fakeTas = new FakeTas();
            using (TasConstructionUValueEvaluator evaluator = new TasConstructionUValueEvaluator(fakeTas.ThermalTransmittances))
            {
                IReadOnlyList<ConstructionUValue> results = evaluator.EvaluateAsync(Request(3, ghost: true), CancellationToken.None).Result;
                ConstructionUValue ghost = results.Single(x => !x.Calculated);
                Assert.Contains("'Ghost' is not in the Material Library", ghost.Message);
                Assert.Equal(3, results.Count(x => x.Calculated));
                Assert.Equal(1, fakeTas.ThermalTransmittanceCalls);
            }

            using (TasConstructionUValueEvaluator evaluator = new TasConstructionUValueEvaluator((manager, guids) => throw new InvalidOperationException("TCD is not available")))
            {
                IReadOnlyList<ConstructionUValue> results = evaluator.EvaluateAsync(Request(2), CancellationToken.None).Result;
                Assert.All(results, x => Assert.False(x.Calculated));
                Assert.All(results, x => Assert.Equal("TCD is not available", x.Message));
            }
        }

        [Fact]
        public void A_request_cancelled_before_it_runs_is_dropped_and_requests_are_served_in_order()
        {
            ManualResetEventSlim gate = new ManualResetEventSlim(false);
            List<int> order = new List<int>();
            FakeTas fakeTas = new FakeTas();
            using (TasConstructionUValueEvaluator evaluator = new TasConstructionUValueEvaluator((manager, guids) =>
            {
                lock (order)
                {
                    order.Add(manager.Constructions.Count);
                }

                gate.Wait(TimeSpan.FromSeconds(10));
                return fakeTas.ThermalTransmittances(manager, guids);
            }))
            {
                Task<IReadOnlyList<ConstructionUValue>> first = evaluator.EvaluateAsync(Request(1), CancellationToken.None);
                SpinWait.SpinUntil(() => { lock (order) { return order.Count == 1; } }, TimeSpan.FromSeconds(10));

                using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource())
                {
                    Task<IReadOnlyList<ConstructionUValue>> cancelled = evaluator.EvaluateAsync(Request(2), cancellationTokenSource.Token);
                    Task<IReadOnlyList<ConstructionUValue>> third = evaluator.EvaluateAsync(Request(3), CancellationToken.None);
                    cancellationTokenSource.Cancel();
                    gate.Set();

                    Assert.Single(first.Result);
                    Assert.Equal(3, third.Result.Count);
                    Assert.True(cancelled.IsCanceled);
                }

                // The cancelled request never reached Tas; the others did, in the order asked.
                Assert.Equal(new[] { 1, 3 }, order.ToArray());
            }
        }
    }
}
