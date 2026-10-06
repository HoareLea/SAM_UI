// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.UI.WPF.Tests.Helpers;
using SAM.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// Reporting parity for D1: an existing construction assigned in the Thermal Performance panel leaves the same kind of per-Apply report as the
    /// generated U-value path (same folder and naming, same save routine, same scoped check): the construction chosen, where it came from, the
    /// scope and count, U before and after, the materials added, the notes shown before Apply, and the check result.
    /// </summary>
    public class ConstructionChangeReportTests : IDisposable
    {
        private readonly string directory = Path.Combine(Path.GetTempPath(), "SAM_ConstructionChangeReportTests_" + Guid.NewGuid().ToString("N"));

        public ConstructionChangeReportTests()
        {
            Directory.CreateDirectory(directory);
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(directory, true);
            }
            catch (IOException)
            {
            }
        }

        private static SetConstructionResult Applied(out AnalyticalModel changed, ThermalApplyScope scope = ThermalApplyScope.AllUsing)
        {
            AnalyticalModel model = AlternativesFixture.Model(out Construction current, out _, out _, out _);
            GlazingSource library = AlternativesFixture.Library();
            Construction aerogel = library.GetConstructions().Single(x => x.Guid == AlternativesFixture.LibraryAerogelGuid);

            SetConstructionRequest request = new SetConstructionRequest()
            {
                SourceConstructionGuid = current.Guid,
                Construction = aerogel,
                MaterialsToAdd = new[] { library.GetMaterials()[AlternativesFixture.Aerogel] },
                Scope = scope,
                SelectedPanelGuids = model.AdjacencyCluster.GetPanels(current).Take(2).Select(x => x.Guid).ToList(),
                OldThermalTransmittance = 0.2597,
                NewThermalTransmittance = 0.16,
                TargetThermalTransmittance = 0.18,
                HeatFlowDirection = HeatFlowDirection.Horizontal,
                SourceLabel = "Default library",
                SourceKind = GlazingSourceKind.Library,
                Notes = new[] { "LIB_AEROGEL is made for roofs (Default Panel Type Roof), but 6 of the 6 panels sit in walls: it is not what the construction was made for.", "Adds 1 material to the model: Aerogel." },
            };

            changed = Modify.SetConstruction(model, request, out SetConstructionResult result);
            Assert.True(result.Succeeded, result.Error);
            return result;
        }

        [Fact]
        public void The_report_names_the_construction_its_source_the_scope_the_U_values_the_materials_the_notes_and_the_check()
        {
            SetConstructionResult result = Applied(out AnalyticalModel changed);

            string text = Query.ConstructionChangeReportText(result, Query.ConstructionCheckSummary(changed, result), @"C:\Models\model.sam");

            Assert.StartsWith("CONSTRUCTION CHANGE" + Environment.NewLine + "===================" + Environment.NewLine, text);
            Assert.Contains("Model:        C:\\Models\\model.sam", text);
            Assert.Contains("may since have been undone", text);
            Assert.Contains("Construction: SIM_EXT_SLD -> LIB_AEROGEL (added to the model; SIM_EXT_SLD unchanged)", text);
            Assert.Contains("Identity:     Guid " + AlternativesFixture.LibraryAerogelGuid, text);
            Assert.Contains("Source:       Default library (default library)", text);
            Assert.Contains("Build-up:     50 mm Air / 12 mm Cement Particleboard / 140 mm Aerogel / 50 mm Air / 3 mm Rainscreen", text);
            Assert.Contains("U-value:      0.260 -> 0.160 W/m2K (target 0.180; horizontal heat flow)", text);
            Assert.Contains("Scope:        6 panels (all that used SIM_EXT_SLD)", text);
            Assert.Contains("Materials:    1 added to the Material Library: Aerogel", text);
            Assert.Contains("NOTES SHOWN BEFORE APPLY", text);
            Assert.Contains("  LIB_AEROGEL is made for roofs", text);
            Assert.Contains("CHECK (SAM model-check rules over the assigned construction and its panels)", text);
            Assert.Contains("LIB_AEROGEL and its 6 panels", text);
        }

        [Fact]
        public void A_selected_scope_a_model_construction_and_an_unsaved_model_read_plainly_and_a_failed_change_says_it_was_not_applied()
        {
            SetConstructionResult selected = Applied(out _, ThermalApplyScope.SelectedOnly);
            string text = Query.ConstructionChangeReportText(selected, null, null);
            Assert.Contains("Model:        (not saved)", text);
            Assert.Contains("Scope:        2 selected panels that used SIM_EXT_SLD", text);
            Assert.Contains("Not run.", text);

            AnalyticalModel model = AlternativesFixture.Model(out Construction current, out Construction thick, out _, out _);
            Modify.SetConstruction(model, new SetConstructionRequest() { SourceConstructionGuid = current.Guid, Construction = thick, SourceLabel = "Model", SourceKind = GlazingSourceKind.Model }, out SetConstructionResult own);
            string text_Own = Query.ConstructionChangeReportText(own, null, null);
            Assert.Contains("MODEL_THICK (already in the model; SIM_EXT_SLD unchanged)", text_Own);
            Assert.Contains("Source:       Model (existing model construction)", text_Own);
            Assert.Contains("Materials:    none added (the model has them all)", text_Own);
            Assert.DoesNotContain("NOTES SHOWN BEFORE APPLY", text_Own);

            Assert.Contains("Not applied: The current construction is no longer in the model.", Query.ConstructionChangeReportText(new SetConstructionResult("The current construction is no longer in the model."), null, null));
        }

        [Fact]
        public void The_report_is_saved_beside_the_model_under_its_own_kind_and_never_overwrites()
        {
            DateTime appliedAt = new DateTime(2026, 10, 1, 14, 5, 9);

            Assert.Equal(@"C:\Models\Block A_ConstructionChange_20261001-140509.txt", Query.Path_ConstructionChangeReport(@"C:\Models\Block A.sam", appliedAt));
            Assert.Null(Query.Path_ConstructionChangeReport(null, appliedAt));

            string path_Model = Path.Combine(directory, "model.sam");
            Assert.True(Modify.SaveConstructionChangeReport(path_Model, appliedAt, "first", out string first, out _));
            Assert.True(Modify.SaveConstructionChangeReport(path_Model, appliedAt, "second", out string second, out _));
            Assert.Equal(Path.Combine(directory, "model_ConstructionChange_20261001-140509.txt"), first);
            Assert.Equal(Path.Combine(directory, "model_ConstructionChange_20261001-140509 (2).txt"), second);

            Assert.False(Modify.SaveConstructionChangeReport(null, appliedAt, "x", out _, out string refusal));
            Assert.Contains("Copy All", refusal);
        }

        [Fact]
        public void Choosing_an_existing_construction_in_the_panel_and_applying_writes_the_report_with_the_notes_and_the_result_line_names_it()
        {
            AnalyticalModel model = AlternativesFixture.Model(out Construction current, out _, out _, out _);
            UIAnalyticalModel ui = new UIAnalyticalModel(model) { Path = Path.Combine(directory, "model.sam") };

            using (UValueViewModel uValue = new UValueViewModel(model, current.Guid, new List<Guid>(), new ImmediateUValueEvaluator()))
            using (ConstructionAlternatives alternatives = new ConstructionAlternatives(model, uValue, new FakeConstructionUValueEvaluator(), new ConstructionUValueCache(), () => AlternativesFixture.Library()))
            {
                uValue.TargetText = "0.18";
                uValue.LastEvaluationTask.Wait(TimeSpan.FromSeconds(10));
                alternatives.Refresh();
                alternatives.Idle().Wait(TimeSpan.FromSeconds(10));
                alternatives.SelectedRow = alternatives.Rows.Single(x => x.Guid == AlternativesFixture.LibraryAerogelGuid);

                ThermalChangeSet set = new ThermalChangeSet().Add(alternatives.CreateRequest());
                ThermalChangeResult result = Modify.ApplyThermalChange(ui, set, x => { }, null);
                Assert.True(result.Succeeded, result.Error);

                Modify.WriteReports(ui, result);

                string line = Assert.Single(result.ReportLines);
                Assert.StartsWith("Report saved: " + Path.Combine(directory, "model_ConstructionChange_"), line);
                string text = File.ReadAllText(line.Substring("Report saved: ".Length));
                Assert.StartsWith("CONSTRUCTION CHANGE", text);
                Assert.Contains("SIM_EXT_SLD -> LIB_AEROGEL (added to the model", text);
                Assert.Contains("Source:       Default library (default library)", text);
                Assert.Contains("Scope:        6 panels (all that used SIM_EXT_SLD)", text);
                Assert.Contains("NOTES SHOWN BEFORE APPLY", text);
                Assert.Contains("Adds 1 material to the model: Aerogel.", text);
                Assert.Contains("U-value:      0.260 -> 0.160 W/m2K (target 0.180; horizontal heat flow)", text);
            }
        }

        [Fact]
        public void A_change_set_with_a_thickness_change_an_existing_construction_and_glazing_writes_one_report_of_each_kind()
        {
            ThermalParts parts = ThermalFixture.Build();
            UIAnalyticalModel ui = new UIAnalyticalModel(parts.Model) { Path = Path.Combine(directory, "mixed.sam") };

            GlazingSource library = AlternativesFixture.Library();
            ThermalChangeSet set = new ThermalChangeSet();
            set.Add(new SetUValueRequest() { ConstructionGuid = parts.OtherWall.Guid, LayerIndex = UValueFixture.WoolIndex, Thickness = 0.1, InitialThermalTransmittance = 0.26, CalculatedThermalTransmittance = 0.2, TargetThermalTransmittance = 0.2, HeatFlowDirection = HeatFlowDirection.Horizontal });
            set.Add(new SetConstructionRequest() { SourceConstructionGuid = parts.Wall.Guid, Construction = library.GetConstructions().Single(x => x.Guid == AlternativesFixture.LibraryThickGuid), SourceLabel = "Default library", SourceKind = GlazingSourceKind.Library });

            ThermalChangeResult result = Modify.ApplyThermalChange(ui, set, x => { }, null);
            Assert.True(result.Succeeded, result.Error);
            Modify.WriteReports(ui, result);

            Assert.Equal(2, result.ReportLines.Count);
            Assert.All(result.ReportLines, x => Assert.StartsWith("Report saved: ", x));
            Assert.Single(Directory.GetFiles(directory, "mixed_UValueChange_*.txt"));
            Assert.Single(Directory.GetFiles(directory, "mixed_ConstructionChange_*.txt"));
        }

        [Fact]
        public void A_model_that_is_not_saved_says_so_in_the_result_line_instead_of_writing_a_file()
        {
            AnalyticalModel model = AlternativesFixture.Model(out Construction current, out Construction thick, out _, out _);
            UIAnalyticalModel ui = new UIAnalyticalModel(model);

            ThermalChangeResult result = Modify.ApplyThermalChange(ui, new ThermalChangeSet().Add(new SetConstructionRequest() { SourceConstructionGuid = current.Guid, Construction = thick }), x => { }, null);
            Modify.WriteReports(ui, result);

            Assert.Contains("Copy All", Assert.Single(result.ReportLines));
        }
    }
}
