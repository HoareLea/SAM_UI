// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.UI.WPF.Tests.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// Thermal Stage A-lite: a glazing candidate is marked <i>before</i> it is chosen - made for another panel group than the
    /// panels carrying the windows, no frame where the current system has one, a material that differs from the model's - and
    /// the markers agree with what the scoped check (the rules behind Edit &gt; ModelCheck) reports after Apply.
    /// </summary>
    public class GlazingRowWarningTests
    {
        private static readonly Guid RoofGuid = new Guid("a0000000-0000-4000-8000-0000000000a1");
        private static readonly Guid WallGuid = new Guid("a0000000-0000-4000-8000-0000000000a2");

        private static GlazingSource Library()
        {
            return GlazingFixture.Library(GlazingFixture.RoofSystem(RoofGuid), GlazingFixture.RoofSystem(WallGuid, "GLZ_Wall", "WallExternal"));
        }

        private static async Task<GlazingViewModel> ViewModel(AnalyticalModel analyticalModel, IEnumerable<Guid> selected = null)
        {
            GlazingViewModel viewModel = new GlazingViewModel(analyticalModel, GlazingFixture.CurrentGuid, selected, new FakeGlazingEvaluator(), Library());
            await viewModel.InitializeAsync();
            return viewModel;
        }

        private static GlazingCandidateRow Row(GlazingViewModel viewModel, Guid guid) => viewModel.Rows.First(x => x.Guid == guid);

        private static GlazingRowWarning Warning(GlazingCandidateRow row, GlazingWarningKind kind) => row.Warnings.FirstOrDefault(x => x.Kind == kind);

        // ---- Panel group -------------------------------------------------------------------------------------------

        [Fact]
        public async Task ASystemMadeForRoofs_IsMarkedOnItsRow_WhenTheWindowsAreInWalls()
        {
            GlazingViewModel viewModel = await ViewModel(GlazingFixture.Model(windows: 20));

            GlazingRowWarning warning = Warning(Row(viewModel, RoofGuid), GlazingWarningKind.PanelGroup);

            Assert.NotNull(warning);
            Assert.Equal("made for roofs", warning.ShortText);
            Assert.False(warning.Blocks);
            Assert.Equal("GLZ_Roof is made for roofs (Default Panel Type Roof), but 20 of the 20 apertures sit in walls: Edit > ModelCheck will warn about them.", warning.Text);
            Assert.Equal("⚠ made for roofs", Row(viewModel, RoofGuid).WarningText);
        }

        [Fact]
        public async Task ASystemMadeForTheSamePanelGroup_OrForNone_IsNotMarked()
        {
            GlazingViewModel viewModel = await ViewModel(GlazingFixture.Model(windows: 20));

            Assert.Null(Warning(Row(viewModel, WallGuid), GlazingWarningKind.PanelGroup)); // Default Panel Type WallExternal, windows in walls
            Assert.Null(Warning(Row(viewModel, GlazingFixture.BetterGuid), GlazingWarningKind.PanelGroup)); // no Default Panel Type at all
        }

        [Fact]
        public async Task ThePanelGroupMarker_CountsOnlyTheWindowsInScope()
        {
            AnalyticalModel analyticalModel = GlazingFixture.Model(windows: 10, roofWindows: 5);
            List<Aperture> apertures = analyticalModel.AdjacencyCluster.GetApertures().Where(x => x.TypeGuid == GlazingFixture.CurrentGuid).ToList();
            List<Guid> roofApertures = analyticalModel.AdjacencyCluster.GetPanels().Where(x => x.PanelType == PanelType.Roof).SelectMany(x => x.Apertures).Select(x => x.Guid).ToList();
            Assert.Equal(5, roofApertures.Count);
            Assert.Equal(15, apertures.Count);

            // All 15: 10 of them sit in walls, which a roof system does not suit.
            GlazingViewModel viewModel = await ViewModel(analyticalModel, roofApertures);
            Assert.Contains("10 of the 15 apertures sit in walls", Warning(Row(viewModel, RoofGuid), GlazingWarningKind.PanelGroup).Text);

            // Only the 5 roof windows: the roof system fits them, so the marker goes - and a wall system now carries one.
            viewModel.ApplyScope = ThermalApplyScope.SelectedOnly;
            Assert.Null(Warning(Row(viewModel, RoofGuid), GlazingWarningKind.PanelGroup));
            Assert.Contains("5 of the 5 apertures sit in roofs", Warning(Row(viewModel, WallGuid), GlazingWarningKind.PanelGroup).Text);

            // Back to all: the roof system is marked again.
            viewModel.ApplyScope = ThermalApplyScope.AllUsing;
            Assert.NotNull(Warning(Row(viewModel, RoofGuid), GlazingWarningKind.PanelGroup));
        }

        [Fact]
        public async Task TheMarkerOfAChosenSystem_IsAWarningAboveApply_ThatDoesNotBlockApply()
        {
            GlazingViewModel viewModel = await ViewModel(GlazingFixture.Model(windows: 20));

            viewModel.SelectedGuid = RoofGuid;

            Assert.Contains(viewModel.Warnings, x => x.StartsWith("GLZ_Roof is made for roofs", StringComparison.Ordinal));
            Assert.True(viewModel.ApplyEnabled);
            Assert.Null(viewModel.ApplyBlockReason);
        }

        [Fact]
        public async Task ThePanelGroupMarker_PredictsWhatTheScopedCheckReportsAfterApply()
        {
            AnalyticalModel analyticalModel = GlazingFixture.Model(windows: 20);
            GlazingViewModel viewModel = await ViewModel(analyticalModel);

            foreach (Guid guid in new[] { RoofGuid, WallGuid })
            {
                bool marked = Warning(Row(viewModel, guid), GlazingWarningKind.PanelGroup) != null;

                viewModel.SelectedGuid = guid;
                SetGlazingRequest request = viewModel.CreateRequest();
                AnalyticalModel applied = Modify.SetGlazing(analyticalModel, request, null, out SetGlazingResult result);
                Assert.True(result.Succeeded, result.Error);

                UValueCheckSummary check = Query.GlazingCheckSummary(applied, result);
                bool reported = check.Log.ToList().Any(x => x.LogRecordType == SAM.Core.LogRecordType.Warning && x.Text.Contains("does not match with assigned"));

                Assert.Equal(marked, reported);
            }
        }

        // ---- Frameless ---------------------------------------------------------------------------------------------

        [Fact]
        public async Task ASystemWithoutFrameLayers_IsMarkedNoFrame_WhenTheCurrentSystemHasOne()
        {
            GlazingViewModel viewModel = await ViewModel(GlazingFixture.Model(windows: 20));

            GlazingRowWarning warning = Warning(Row(viewModel, GlazingFixture.PaneOnlyGuid), GlazingWarningKind.Frameless);

            Assert.NotNull(warning);
            Assert.Equal("no frame", warning.ShortText);
            Assert.False(warning.Blocks);
            Assert.Equal("The chosen system has no frame layers: the apertures lose their frame, so Uw equals Ug.", warning.Text);
            Assert.Null(Warning(Row(viewModel, GlazingFixture.BetterGuid), GlazingWarningKind.Frameless));

            viewModel.SelectedGuid = GlazingFixture.PaneOnlyGuid;
            Assert.Contains("The chosen system has no frame layers: the apertures lose their frame, so Uw equals Ug.", viewModel.Warnings);
            Assert.True(viewModel.ApplyEnabled);
        }

        [Fact]
        public async Task NoFrameIsNotMarked_WhenTheCurrentSystemHasNoFrameEither()
        {
            // Windows of a frameless current system: nothing is lost by choosing another frameless one.
            ApertureConstruction current = GlazingFixture.System(GlazingFixture.CurrentGuid, GlazingFixture.CurrentName, ApertureType.Window, GlazingFixture.Clear, frame: false);
            AdjacencyCluster adjacencyCluster = new AdjacencyCluster();
            for (int i = 0; i < 4; i++)
            {
                adjacencyCluster.AddObject(GlazingFixture.PanelWithWindow(current, i, out Aperture _));
            }

            AnalyticalModel analyticalModel = new AnalyticalModel("Frameless", null, null, null, adjacencyCluster, GlazingFixture.ModelMaterials(), new ProfileLibrary("Profiles"));
            GlazingViewModel viewModel = await ViewModel(analyticalModel);

            Assert.Null(Warning(Row(viewModel, GlazingFixture.PaneOnlyGuid), GlazingWarningKind.Frameless));
        }

        // ---- Material ----------------------------------------------------------------------------------------------

        [Fact]
        public async Task ASystemWhoseMaterialDiffersFromTheModels_IsMarkedOnItsRow_AndStillBlocked()
        {
            GlazingViewModel viewModel = await ViewModel(GlazingFixture.Model(windows: 20));
            await viewModel.AddSourceAsync(GlazingFixture.Loaded());

            GlazingCandidateRow row = Row(viewModel, GlazingFixture.DifferentMaterialGuid);
            GlazingRowWarning warning = Warning(row, GlazingWarningKind.Material);

            Assert.NotNull(warning);
            Assert.Equal("material differs from model", warning.ShortText);
            Assert.True(warning.Blocks);
            Assert.Contains("differs from the model's material of the same name", warning.Text);
            Assert.False(row.CanApply);

            viewModel.SelectedGuid = GlazingFixture.DifferentMaterialGuid;
            Assert.False(viewModel.ApplyEnabled);
            Assert.Equal(warning.Text, viewModel.ApplyBlockReason);
            Assert.DoesNotContain(warning.Text, viewModel.Warnings); // a blocker is shown once, as the block reason
        }

        [Fact]
        public async Task ASystemWhoseMaterialIsMissingFromItsSource_IsMarkedMaterialMissing()
        {
            GlazingViewModel viewModel = await ViewModel(GlazingFixture.Model(windows: 20));

            GlazingRowWarning warning = Warning(Row(viewModel, GlazingFixture.MissingMaterialGuid), GlazingWarningKind.Material);

            Assert.NotNull(warning);
            Assert.Equal("material missing", warning.ShortText);
            Assert.True(warning.Blocks);
        }

        // ---- The current system and the others ---------------------------------------------------------------------

        [Fact]
        public async Task TheCurrentSystem_IsTheReference_AndCarriesNoMarker()
        {
            GlazingViewModel viewModel = await ViewModel(GlazingFixture.Model(windows: 20));

            GlazingCandidateRow current = viewModel.CurrentRow;

            Assert.Empty(current.Warnings);
            Assert.Equal(string.Empty, current.WarningText);
            Assert.False(current.HasWarnings);
        }

        [Fact]
        public async Task SeveralMarkers_ShareTheRowAndTheTooltip()
        {
            ApertureConstruction roofNoFrame = GlazingFixture.System(RoofGuid, "GLZ_RoofPane", ApertureType.Window, GlazingFixture.Clear, frame: false);
            roofNoFrame.SetValue(ApertureConstructionParameter.DefaultPanelType, "Roof");
            GlazingViewModel viewModel = new GlazingViewModel(GlazingFixture.Model(windows: 20), GlazingFixture.CurrentGuid, null, new FakeGlazingEvaluator(), GlazingFixture.Library(roofNoFrame));
            await viewModel.InitializeAsync();

            GlazingCandidateRow row = Row(viewModel, RoofGuid);

            Assert.Equal("⚠ no frame · ⚠ made for roofs", row.WarningText);
            Assert.Contains("lose their frame", row.Tooltip);
            Assert.Contains("is made for roofs", row.Tooltip);
        }

        [Fact]
        public async Task TheMarkers_NeverWriteTheModel()
        {
            AnalyticalModel analyticalModel = GlazingFixture.Model(windows: 10, roofWindows: 5);
            string before = analyticalModel.ToJsonObject().ToJsonString();

            GlazingViewModel viewModel = await ViewModel(analyticalModel);
            await viewModel.AddSourceAsync(GlazingFixture.Loaded());
            viewModel.SelectedGuid = RoofGuid;
            viewModel.ApplyScope = ThermalApplyScope.SelectedOnly;
            viewModel.ApplyScope = ThermalApplyScope.AllUsing;
            viewModel.Dispose();

            Assert.Equal(before, analyticalModel.ToJsonObject().ToJsonString());
        }
    }
}
