// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Tas;
using SAM.Analytical.UI.WPF.Tests.Helpers;
using SAM.Core;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// U-value PR3: the scoped check and the GLAZING CHANGE report after "Set glazing". The check reads the model through
    /// SAM's per-object rules, the report is written from the result and the check alone, and neither writes to the model.
    /// </summary>
    public class GlazingReportTests : IDisposable
    {
        private readonly string directory = Path.Combine(Path.GetTempPath(), "SAM_GlazingReportTests_" + Guid.NewGuid().ToString("N"));

        public GlazingReportTests()
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

        // A report line: the label and its colon padded to 13 characters, one space, the value.
        private static string Line(string label, string value) => (label + ":").PadRight(13) + " " + value;

        private static ThermalTransmittanceCalculationResult Tas(SetGlazingRequest request)
        {
            return new ThermalTransmittanceCalculationResult(request.ApertureConstruction.Guid, "Fake", 0.70, 0.20, 0.45, 0.25, 0.30, 0.50, 0.60, 0.30, new ThermalTransmittances(2, 2, 2, 2, 2, 2, 1.10));
        }

        private static async Task<(AnalyticalModel model, SetGlazingResult result)> Applied(GlazingApplyScope scope = GlazingApplyScope.AllApertures, Guid? system = null, double target = double.NaN)
        {
            AnalyticalModel analyticalModel = GlazingFixture.Model(5);
            GlazingViewModel viewModel = GlazingFixture.ViewModel(analyticalModel);
            await viewModel.InitializeAsync();
            viewModel.ApplyScope = scope;
            viewModel.SelectedGuid = system ?? GlazingFixture.BetterGuid;
            SetGlazingRequest request = viewModel.CreateRequest();
            request.TargetUw = target;

            AnalyticalModel changed = Modify.SetGlazing(analyticalModel, request, Tas(request), out SetGlazingResult result);
            return (changed, result);
        }

        // ---- The scoped check ----------------------------------------------------------------------------------

        [Fact]
        public async Task TheCheck_CoversTheAppliedSystemAndItsApertures_NotTheWholeModel()
        {
            (AnalyticalModel changed, SetGlazingResult result) = await Applied();

            UValueCheckSummary summary = Query.GlazingCheckSummary(changed, result);

            Assert.Contains("GLZ 2 and its 5 apertures", summary.Text);
            Assert.Equal(summary.Errors == 0 && summary.Warnings == 0, summary.Passed);
            Assert.Equal(summary.Errors > 0 ? "✕" : summary.Warnings > 0 ? "⚠" : "✓", summary.Glyph);
        }

        [Fact]
        public async Task TheCheck_DoesNotWriteToTheModel()
        {
            (AnalyticalModel changed, SetGlazingResult result) = await Applied();
            string before = changed.ToJsonObject().ToJsonString();

            Query.GlazingCheckSummary(changed, result);

            Assert.Equal(before, changed.ToJsonObject().ToJsonString());
        }

        [Fact]
        public async Task TheCheck_ForDontAssign_NamesTheSystemAlone()
        {
            (AnalyticalModel changed, SetGlazingResult result) = await Applied(GlazingApplyScope.DontAssign);

            UValueCheckSummary summary = Query.GlazingCheckSummary(changed, result);

            Assert.Contains("GLZ 2 (assigned to no aperture)", summary.Text);
        }

        [Fact]
        public async Task TheCheck_RunsBothRuleSetsOfTheApertureConstruction_AsModelCheckDoes()
        {
            // Edit > ModelCheck warns about a system without frame layers (the layers rule set); the scoped check must too.
            (AnalyticalModel changed, SetGlazingResult result) = await Applied(system: GlazingFixture.PaneOnlyGuid);

            UValueCheckSummary summary = Query.GlazingCheckSummary(changed, result);

            Assert.False(summary.Passed);
            Assert.Equal(1, summary.Warnings);
            Assert.Contains(summary.Log, x => x.LogRecordType == LogRecordType.Warning && x.Text.Contains("has no Frame ConstructionLayers"));
            Assert.StartsWith("1 warning for GLZ_Pane and its 5 apertures", summary.Text);
            Assert.Equal("⚠", summary.Glyph);
        }

        [Fact]
        public async Task TheCheck_ReportsASystemMadeForAnotherPanelGroup_AsModelCheckDoes()
        {
            // Found in the real-app pass: SIM_EXT_GLZ_SKY (a roof system) applied to wall apertures. The full Edit > ModelCheck
            // warned for every aperture (Default Panel Type vs host panel, and the panel group rule); the scoped check said
            // "No errors or warnings".
            Guid roofGuid = new Guid("a0000000-0000-4000-8000-0000000000a1");
            ApertureConstruction roof = GlazingFixture.System(roofGuid, "GLZ_Roof", ApertureType.Window, GlazingFixture.LowE);
            roof.SetValue(ApertureConstructionParameter.DefaultPanelType, PanelType.Roof.ToString());

            MaterialLibrary materials = GlazingFixture.ModelMaterials();
            materials.Add(GlazingFixture.LowEGlass());
            GlazingSource library = new GlazingSource(GlazingSourceKind.Library, "Default library", new ConstructionManager(new System.Collections.Generic.List<ApertureConstruction>() { GlazingFixture.Current(), roof }, null, materials));

            AnalyticalModel analyticalModel = GlazingFixture.Model(5);
            GlazingViewModel viewModel = GlazingFixture.ViewModel(analyticalModel, null, null, library);
            await viewModel.InitializeAsync();
            viewModel.SelectedGuid = roofGuid;
            SetGlazingRequest request = viewModel.CreateRequest();
            AnalyticalModel changed = Modify.SetGlazing(analyticalModel, request, Tas(request), out SetGlazingResult result);
            Assert.True(result.Succeeded);

            UValueCheckSummary summary = Query.GlazingCheckSummary(changed, result);

            Assert.False(summary.Passed);
            Assert.Equal(0, summary.Errors);
            Assert.Equal(10, summary.Warnings);   // per aperture: the Default Panel Type rule and the panel group rule
            Assert.Equal(5, summary.Log.Count(x => x.Text.Contains("has diiferent Default Panel Type than its")));
            Assert.Equal(5, summary.Log.Count(x => x.Text.Contains("does not match with assigned GLZ_Roof ApertureConstruction")));
            Assert.StartsWith("10 warnings for GLZ_Roof and its 5 apertures", summary.Text);
            Assert.Equal("⚠", summary.Glyph);
        }

        [Fact]
        public async Task TheCheck_OfASystemForTheSamePanelGroup_HasNoHostPanelWarnings()
        {
            (AnalyticalModel changed, SetGlazingResult result) = await Applied();

            UValueCheckSummary summary = Query.GlazingCheckSummary(changed, result);

            Assert.DoesNotContain(summary.Log, x => x.Text.Contains("Default Panel Type") || x.Text.Contains("does not match with assigned"));
        }

        [Fact]
        public async Task TheReport_OfASystemWithoutFrame_SaysNoneNotAQuestionMark()
        {
            (AnalyticalModel changed, SetGlazingResult result) = await Applied(system: GlazingFixture.PaneOnlyGuid);

            string text = Query.GlazingChangeReportText(result, Query.GlazingCheckSummary(changed, result), null);

            Assert.Contains(Line("Uf", "2.000 -> none W/m2K"), text);
            Assert.Contains(Line("Frame", "50 mm Frame -> none"), text);
            Assert.DoesNotContain("?", text.Split(new[] { Environment.NewLine }, StringSplitOptions.None).First(x => x.StartsWith("Uf:")));
        }

        [Fact]
        public void TheCheck_OfAChangeThatWasNotApplied_SaysSo()
        {
            UValueCheckSummary summary = Query.GlazingCheckSummary(GlazingFixture.Model(1), new SetGlazingResult("Nope."));

            Assert.Equal("Nothing was checked: the glazing change was not applied.", summary.Text);
            Assert.True(summary.Passed);
        }

        [Fact]
        public async Task ASystemWhoseMaterialIsMissing_IsCaughtByTheCheck()
        {
            // The rules are SAM's own (Edit > ModelCheck): remove a material the applied system names and the check says so.
            (AnalyticalModel changed, SetGlazingResult result) = await Applied();
            MaterialLibrary materialLibrary = changed.MaterialLibrary;
            materialLibrary.Remove(materialLibrary.GetMaterial(GlazingFixture.LowE));
            AnalyticalModel broken = new AnalyticalModel(changed, changed.AdjacencyCluster, materialLibrary, changed.ProfileLibrary);

            UValueCheckSummary summary = Query.GlazingCheckSummary(broken, result);

            Assert.True(summary.Errors > 0 || summary.Warnings > 0);
            Assert.False(summary.Passed);
        }

        // ---- The report ----------------------------------------------------------------------------------------

        [Fact]
        public async Task TheReport_StatesProvenanceTheChangeAndTheCheck()
        {
            (AnalyticalModel changed, SetGlazingResult result) = await Applied(target: 1.25);
            UValueCheckSummary summary = Query.GlazingCheckSummary(changed, result);

            string text = Query.GlazingChangeReportText(result, summary, @"C:\Models\office.sam");

            Assert.StartsWith("GLAZING CHANGE" + Environment.NewLine + "==============", text);
            Assert.Contains(Line("Model", @"C:\Models\office.sam"), text);
            Assert.Contains("applied at ", text);
            Assert.Contains("may since have been undone", text);
            Assert.Contains(Line("Glazing", "GLZ -> GLZ 2 (added to the model; GLZ unchanged)"), text);
            Assert.Contains(Line("Pane", "6 mm Clear6 / 12 mm Argon12 / 6 mm Clear6 -> 6 mm LowE6 / 12 mm Argon12 / 6 mm Clear6"), text);
            Assert.Contains(Line("Frame", "50 mm Frame -> 50 mm Frame"), text);
            Assert.Contains(Line("Ug", "1.400 -> 1.100 W/m2K"), text);
            Assert.Contains(Line("Uf", "2.000 -> 2.000 W/m2K"), text);
            Assert.Contains(Line("g", "0.600 -> 0.500"), text);
            Assert.Contains(Line("Light", "0.780 -> 0.700"), text);
            Assert.Contains("area-weighted over the apertures' pane and frame areas; target at most 1.250", text);
            Assert.Contains(Line("Scope", "5 apertures (all that used GLZ)"), text);
            Assert.Contains(Line("Materials", "1 added to the Material Library: LowE6"), text);
            Assert.Contains("CHECK (SAM model-check rules over the applied glazing, its apertures and their panels)", text);
            Assert.Contains(summary.Text, text);
        }

        [Fact]
        public async Task TheReport_OfDontAssign_SaysNoApertureChanged()
        {
            (AnalyticalModel changed, SetGlazingResult result) = await Applied(GlazingApplyScope.DontAssign);

            string text = Query.GlazingChangeReportText(result, Query.GlazingCheckSummary(changed, result), null);

            Assert.Contains(Line("Model", "(not saved)"), text);
            Assert.Contains(Line("Scope", "not assigned to any aperture"), text);
        }

        [Fact]
        public async Task TheReport_OfAnApproximateUw_IsLabelledApproximate()
        {
            (AnalyticalModel changed, SetGlazingResult result) = await Applied();
            SetGlazingRequest request = new SetGlazingRequest() { UwBasis = GlazingUwBasis.Approximate, OldUw = 1.5, NewUw = 1.2, Scope = GlazingApplyScope.AllApertures, Values = result.Values, OldValues = result.OldValues };
            SetGlazingResult approximate = new SetGlazingResult(request, result.SourceApertureConstruction, result.ApertureConstruction, true, new string[0], result.ApertureGuids, result.PanelGuids);

            string text = Query.GlazingChangeReportText(approximate, null, null);

            Assert.Contains("approx. Uw (80/20)", text);
            Assert.Contains("Not run.", text);
        }

        [Fact]
        public void TheReport_OfAChangeThatWasNotApplied_SaysWhy()
        {
            string text = Query.GlazingChangeReportText(new SetGlazingResult("No aperture uses GLZ."), null, null);

            Assert.Contains("Not applied: No aperture uses GLZ.", text);
        }

        // ---- Where it is saved ---------------------------------------------------------------------------------

        [Fact]
        public void TheReportPath_IsBesideTheModel_WithATimestamp()
        {
            string path = Query.Path_GlazingChangeReport(Path.Combine(directory, "office.sam"), new DateTime(2026, 10, 2, 9, 5, 7));

            Assert.Equal(Path.Combine(directory, "office_GlazingChange_20261002-090507.txt"), path);
        }

        [Fact]
        public void AnUnsavedModel_HasNoReportPath_AndOffersCopyAll()
        {
            Assert.Null(Query.Path_GlazingChangeReport(null, DateTime.Now));
            Assert.False(Modify.SaveGlazingChangeReport(string.Empty, DateTime.Now, "text", out string path, out string refusal));
            Assert.Null(path);
            Assert.Contains("use Copy All", refusal);
        }

        [Fact]
        public void TheReport_IsSavedBesideTheModel_AndNeverOverwritesAnEarlierOne()
        {
            string model = Path.Combine(directory, "office.sam");
            DateTime appliedAt = new DateTime(2026, 10, 2, 9, 5, 7);

            Assert.True(Modify.SaveGlazingChangeReport(model, appliedAt, "first", out string path_1, out string _));
            Assert.True(Modify.SaveGlazingChangeReport(model, appliedAt, "second", out string path_2, out string _));

            Assert.Equal("first", File.ReadAllText(path_1));
            Assert.Equal("second", File.ReadAllText(path_2));
            Assert.EndsWith("office_GlazingChange_20261002-090507 (2).txt", path_2);
        }

        [Fact]
        public void AMissingModelFolder_IsAReason_NotAnException()
        {
            Assert.False(Modify.SaveGlazingChangeReport(Path.Combine(directory, "nowhere", "office.sam"), DateTime.Now, "text", out string _, out string refusal));

            Assert.Contains("does not exist", refusal);
        }

        [Fact]
        public void TheUValueReportSaving_StillWorksThroughTheSharedHelper()
        {
            string model = Path.Combine(directory, "office.sam");

            Assert.True(Modify.SaveUValueChangeReport(model, new DateTime(2026, 10, 2, 9, 5, 7), "text", out string path, out string _));

            Assert.EndsWith("office_UValueChange_20261002-090507.txt", path);
            Assert.Equal("text", File.ReadAllText(path));
        }
    }
}
