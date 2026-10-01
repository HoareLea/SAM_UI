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
    /// U-value PR3: <see cref="GlazingViewModel"/> and the pieces it is built on - the candidate pool (identity by Guid,
    /// aperture type, materials), the overall Uw, the comparison table with its filters, the automatic choice, the apply
    /// rules, loading more glazing - against the fixture model and a Tas stand-in that answers from a table.
    /// </summary>
    public class GlazingViewModelTests
    {
        // Uw of the fixture system with Ug / Uf: windows 2.0 x 1.5 m with a 0.05 m frame = pane 2.66 m², frame 0.34 m².
        private static double Uw(double ug, double uf) => (ug * 2.66 + uf * 0.34) / 3.0;

        private static async Task<GlazingViewModel> Ready(AnalyticalModel analyticalModel = null, IEnumerable<Guid> selected = null, FakeGlazingEvaluator evaluator = null)
        {
            analyticalModel = analyticalModel ?? GlazingFixture.Model();
            GlazingViewModel viewModel = GlazingFixture.ViewModel(analyticalModel, selected, evaluator);
            await viewModel.InitializeAsync();
            return viewModel;
        }

        // ---- The pool ------------------------------------------------------------------------------------------

        [Fact]
        public async Task TheTable_ListsCompleteSystemsOfTheApertureType_OnceEach_BestUwFirst()
        {
            FakeGlazingEvaluator evaluator = new FakeGlazingEvaluator();
            GlazingViewModel viewModel = await Ready(evaluator: evaluator);

            // current (also in the library: one row), the better "GLZ", the pane-only one, the one with a missing material; no door.
            Assert.Equal(new[] { GlazingFixture.MissingMaterialGuid, GlazingFixture.BetterGuid, GlazingFixture.PaneOnlyGuid, GlazingFixture.CurrentGuid }, viewModel.Rows.Select(x => x.Guid).ToArray());
            Assert.DoesNotContain(viewModel.Rows, x => x.Guid == GlazingFixture.DoorGuid);

            // One Tas calculation for the whole pool, once: the twin of the current system is not asked for twice.
            Assert.Single(evaluator.Requests);
            Assert.Equal(4, evaluator.GuidsRequested);
            Assert.Equal(GlazingPreviewStatus.Ready, viewModel.Status);
        }

        [Fact]
        public async Task SystemsSharingAName_AreToldApartByGuid()
        {
            GlazingViewModel viewModel = await Ready();

            List<GlazingCandidateRow> named = viewModel.Rows.Where(x => x.Name == GlazingFixture.CurrentName).ToList();
            Assert.Equal(2, named.Count);
            Assert.NotEqual(named[0].Guid, named[1].Guid);
            Assert.NotEqual(named[0].Candidate.ShortId, named[1].Candidate.ShortId);
            Assert.Contains(named[0].Candidate.ShortId, named[0].Tooltip);
            Assert.Contains("Low-e double glazing", named.Single(x => x.Guid == GlazingFixture.BetterGuid).Tooltip);
        }

        [Fact]
        public async Task TheCurrentSystem_IsTheModelsOwn_AndAlwaysShown()
        {
            GlazingViewModel viewModel = await Ready();

            Assert.Equal(GlazingSourceKind.Model, viewModel.CurrentRow.Candidate.Kind);
            Assert.True(viewModel.CurrentRow.IsCurrent);

            // Filtered out by a target it does not meet, it stays as the reference row.
            viewModel.TargetText = "1.0";
            Assert.Contains(viewModel.Rows, x => x.IsCurrent && !x.Passes);
        }

        [Fact]
        public async Task GlassIsReplacedByGlass_NotBySolidSystems()
        {
            GlazingViewModel viewModel = await Ready();

            Assert.DoesNotContain(viewModel.Rows, x => x.Guid == GlazingFixture.SolidGuid);
        }

        [Fact]
        public async Task ASolidDoor_IsOfferedOnlyOtherSolidDoors()
        {
            ApertureConstruction door = GlazingFixture.Solid(new Guid("a0000000-0000-4000-8000-0000000000dd"), "Door A", ApertureType.Door);
            Panel panel = GlazingFixture.PanelWithWindow(door, 0, out Aperture _);
            AdjacencyCluster adjacencyCluster = new AdjacencyCluster();
            adjacencyCluster.AddObject(panel);
            AnalyticalModel analyticalModel = new AnalyticalModel("Doors", null, null, null, adjacencyCluster, GlazingFixture.ModelMaterials(), new ProfileLibrary("Profiles"));
            GlazingViewModel viewModel = new GlazingViewModel(analyticalModel, door.Guid, null, new FakeGlazingEvaluator(), GlazingFixture.Library());
            await viewModel.InitializeAsync();

            Assert.Equal(new[] { door.Guid, GlazingFixture.SolidDoorGuid }.OrderBy(x => x), viewModel.Rows.Select(x => x.Guid).OrderBy(x => x));
            Assert.DoesNotContain(viewModel.Rows, x => x.Guid == GlazingFixture.DoorGuid);
        }

        [Fact]
        public async Task OnlyTheSystemsOfTheCurrentApertureTypeAreOffered()
        {
            AnalyticalModel analyticalModel = GlazingFixture.Model();
            GlazingViewModel viewModel = await Ready(analyticalModel);

            Assert.All(viewModel.Rows, x => Assert.Equal(ApertureType.Window, x.Candidate.ApertureType));
        }

        [Fact]
        public async Task ASystemWhoseMaterialIsMissing_CannotBeUsed_AndSaysWhy()
        {
            GlazingViewModel viewModel = await Ready();

            GlazingCandidateRow row = viewModel.Rows.Single(x => x.Guid == GlazingFixture.MissingMaterialGuid);
            Assert.False(row.CanApply);
            Assert.Contains("'Mystery'", row.Candidate.MaterialIssue);
            Assert.Equal("✕ cannot be used", row.StatusText);
        }

        [Fact]
        public async Task OnlyTheMaterialsTheModelLacks_AreToBeAdded()
        {
            GlazingViewModel viewModel = await Ready();

            GlazingCandidate better = viewModel.Rows.Single(x => x.Guid == GlazingFixture.BetterGuid).Candidate;
            Assert.Equal(new[] { GlazingFixture.LowE }, better.MaterialsToAdd.Select(x => x.Name).ToArray());
            Assert.Empty(viewModel.CurrentRow.Candidate.MaterialsToAdd);
        }

        // ---- Uw ------------------------------------------------------------------------------------------------

        [Fact]
        public async Task Uw_IsWeighedByThePaneAndFrameAreasOfTheAffectedApertures()
        {
            GlazingViewModel viewModel = await Ready();

            Assert.Equal(Uw(1.40, 2.00), viewModel.CurrentRow.Uw, 3);
            Assert.Equal(Uw(1.10, 2.00), viewModel.Rows.Single(x => x.Guid == GlazingFixture.BetterGuid).Uw, 3);
            Assert.Equal(GlazingUwBasis.Area, viewModel.CurrentRow.UwBasis);
            Assert.Equal("1.47", viewModel.CurrentRow.UwText);
        }

        [Fact]
        public async Task ASystemWithoutFrameLayers_HasNoFrameArea_SoUwIsUg()
        {
            GlazingViewModel viewModel = await Ready();

            GlazingCandidateRow row = viewModel.Rows.Single(x => x.Guid == GlazingFixture.PaneOnlyGuid);
            Assert.Equal(1.30, row.Uw, 3);
            Assert.Equal("no frame", row.FrameText);
            Assert.Equal("–", row.UfText);
        }

        [Fact]
        public void Uw_WithTheCandidatesOwnFrameWidth()
        {
            AnalyticalModel analyticalModel = GlazingFixture.Model(1);
            List<Aperture> apertures = analyticalModel.AdjacencyCluster.GetApertures();

            ApertureConstruction wide = new ApertureConstruction(Guid.NewGuid(), "Wide frame", ApertureType.Window,
                new List<ConstructionLayer>() { new ConstructionLayer(GlazingFixture.Clear, 0.006) },
                new List<ConstructionLayer>() { new ConstructionLayer(GlazingFixture.FrameMaterial, 0.1) });

            double uw = Query.GlazingOverallThermalTransmittance(apertures, wide, 1.0, 2.0, out GlazingUwBasis basis);

            // 0.1 m frame: pane 1.8 x 1.3 = 2.34 m², frame 0.66 m².
            Assert.Equal((1.0 * 2.34 + 2.0 * 0.66) / 3.0, uw, 3);
            Assert.Equal(GlazingUwBasis.Area, basis);
        }

        [Fact]
        public void Uw_WithoutGeometry_IsTheLabelledEightyTwentyApproximation()
        {
            ApertureConstruction framed = GlazingFixture.Current();

            double uw = Query.GlazingOverallThermalTransmittance(null, framed, 1.0, 2.0, out GlazingUwBasis basis);

            Assert.Equal(0.8 * 1.0 + 0.2 * 2.0, uw, 6);
            Assert.Equal(GlazingUwBasis.Approximate, basis);
        }

        [Fact]
        public void Uw_NeedsUg_AndUfForAFramedSystem()
        {
            ApertureConstruction framed = GlazingFixture.Current();
            List<Aperture> apertures = GlazingFixture.Model(1).AdjacencyCluster.GetApertures();

            Assert.True(double.IsNaN(Query.GlazingOverallThermalTransmittance(apertures, framed, double.NaN, 2.0, out GlazingUwBasis basis_1)));
            Assert.Equal(GlazingUwBasis.None, basis_1);
            Assert.True(double.IsNaN(Query.GlazingOverallThermalTransmittance(apertures, framed, 1.0, double.NaN, out GlazingUwBasis basis_2)));
            Assert.Equal(GlazingUwBasis.None, basis_2);
        }

        [Fact]
        public async Task AnApproximateUw_IsMarkedWithATilde()
        {
            // An aperture without usable geometry cannot be weighed: build the row directly.
            GlazingViewModel viewModel = await Ready();
            GlazingCandidateRow row = new GlazingCandidateRow(viewModel.CurrentRow.Candidate, new GlazingValues(1.0, 0.5, 0.7, 2.0), true, 1.2, GlazingUwBasis.Approximate, false, true, 1.5);

            Assert.Equal("≈1.20", row.UwText);
            Assert.Equal("+0.30", row.MarginText);
        }

        // ---- Filters, sort, margin, status ---------------------------------------------------------------------

        [Fact]
        public async Task TheTargetUw_FiltersTheTable_AndFillsTheMargin()
        {
            GlazingViewModel viewModel = await Ready();

            viewModel.TargetText = "1.25";

            Assert.Equal(new[] { GlazingFixture.MissingMaterialGuid, GlazingFixture.BetterGuid, GlazingFixture.CurrentGuid }, viewModel.Rows.Select(x => x.Guid).ToArray());
            Assert.Equal("Showing 2 of 4 systems.", viewModel.CandidateCountText);

            GlazingCandidateRow better = viewModel.Rows.Single(x => x.Guid == GlazingFixture.BetterGuid);
            Assert.Equal(1.25 - Uw(1.10, 2.00), better.Margin, 3);
            Assert.Equal("✓ meets target", better.StatusText);
            Assert.Equal("✕ above target", viewModel.CurrentRow.StatusText);
        }

        [Fact]
        public async Task TheGAndLightFilters_ApplyToGlass_NotToTheCurrentReference()
        {
            GlazingViewModel viewModel = await Ready();

            viewModel.MinGText = "0.55";
            Assert.Equal(new[] { GlazingFixture.PaneOnlyGuid, GlazingFixture.CurrentGuid }, viewModel.Rows.Select(x => x.Guid).ToArray());

            viewModel.MinGText = string.Empty;
            viewModel.MaxGText = "0.55";
            Assert.Equal(new[] { GlazingFixture.MissingMaterialGuid, GlazingFixture.BetterGuid }, viewModel.Rows.Where(x => x.Passes).Select(x => x.Guid).ToArray());

            viewModel.MaxGText = string.Empty;
            viewModel.MinLightText = "0.75";
            Assert.Equal(new[] { GlazingFixture.PaneOnlyGuid, GlazingFixture.CurrentGuid }, viewModel.Rows.Where(x => x.Passes).Select(x => x.Guid).ToArray());
        }

        [Fact]
        public async Task ATextThatIsNotANumber_FiltersNothing()
        {
            GlazingViewModel viewModel = await Ready();

            viewModel.TargetText = "abc";
            viewModel.MinGText = "-1";

            Assert.True(double.IsNaN(viewModel.Target));
            Assert.Equal(4, viewModel.Rows.Count(x => x.Passes));
        }

        // ---- The choice ----------------------------------------------------------------------------------------

        [Fact]
        public async Task AgainstATargetTheCurrentSystemMisses_TheBestUsableSystemIsChosen()
        {
            GlazingViewModel viewModel = await Ready();

            viewModel.TargetText = "1.25";

            // The missing-material system has the best Uw but cannot be used; the next one is chosen.
            Assert.Equal(GlazingFixture.BetterGuid, viewModel.SelectedGuid);
            Assert.True(viewModel.ApplyEnabled);
            Assert.Equal("✓ Meets target", viewModel.ComparisonStatus);
            Assert.Equal(1.25 - Uw(1.10, 2.00), viewModel.Margin, 3);
        }

        [Fact]
        public async Task WhenTheCurrentSystemAlreadyMeetsTheTarget_NothingIsChosenForTheUser()
        {
            GlazingViewModel viewModel = await Ready();

            viewModel.TargetText = "1.6";

            Assert.Null(viewModel.SelectedGuid);
            Assert.False(viewModel.ApplyEnabled);
        }

        [Fact]
        public async Task WithoutATarget_NothingIsChosenAutomatically()
        {
            GlazingViewModel viewModel = await Ready();

            Assert.Null(viewModel.SelectedGuid);
            Assert.False(viewModel.ApplyEnabled);
            Assert.Equal("Choose a glazing system from the table.", viewModel.ResultText);
        }

        [Fact]
        public async Task AChoiceTheUserMade_IsKept_UntilItIsFilteredOut()
        {
            GlazingViewModel viewModel = await Ready();
            viewModel.SelectedGuid = GlazingFixture.PaneOnlyGuid;

            viewModel.TargetText = "1.35";
            Assert.Equal(GlazingFixture.PaneOnlyGuid, viewModel.SelectedGuid);

            viewModel.TargetText = "1.25";
            Assert.NotEqual(GlazingFixture.PaneOnlyGuid, viewModel.SelectedGuid);
        }

        [Fact]
        public async Task ApplyIsEnabledOnlyForAUsableSystemThatIsNotTheCurrentOne()
        {
            GlazingViewModel viewModel = await Ready();

            viewModel.SelectedGuid = GlazingFixture.CurrentGuid;
            Assert.False(viewModel.ApplyEnabled);
            Assert.Contains("is the system the apertures use now", viewModel.ApplyBlockReason);

            viewModel.SelectedGuid = GlazingFixture.MissingMaterialGuid;
            Assert.False(viewModel.ApplyEnabled);
            Assert.Contains("'Mystery'", viewModel.ApplyBlockReason);

            viewModel.SelectedGuid = GlazingFixture.BetterGuid;
            Assert.True(viewModel.ApplyEnabled);
            Assert.Null(viewModel.ApplyBlockReason);
        }

        [Fact]
        public async Task ApplyIsDisabledWhileCalculating_AndWhenTasFails()
        {
            ManualGlazingEvaluator manual = new ManualGlazingEvaluator();
            GlazingViewModel viewModel = new GlazingViewModel(GlazingFixture.Model(), GlazingFixture.CurrentGuid, null, manual, GlazingFixture.Library());
            Task initial = viewModel.InitializeAsync();

            Assert.Equal(GlazingPreviewStatus.Calculating, viewModel.Status);
            Assert.True(viewModel.IsBusy);
            Assert.False(viewModel.ApplyEnabled);

            manual.Release(manual.Calls[0]);
            await initial;
            Assert.False(viewModel.IsBusy);
            Assert.Equal(GlazingPreviewStatus.Ready, viewModel.Status);

            FakeGlazingEvaluator failing = new FakeGlazingEvaluator() { Fail = true };
            GlazingViewModel viewModel_Failed = await Ready(evaluator: failing);
            viewModel_Failed.SelectedGuid = GlazingFixture.BetterGuid;
            Assert.Equal(GlazingPreviewStatus.Failed, viewModel_Failed.Status);
            Assert.Equal("Tas could not calculate the glazing values.", viewModel_Failed.StatusMessage);
            Assert.False(viewModel_Failed.ApplyEnabled);
        }

        // ---- Scope and wording ---------------------------------------------------------------------------------

        [Fact]
        public async Task TheScopeIsStatedInline_ForEachChoice()
        {
            AnalyticalModel analyticalModel = GlazingFixture.Model();
            List<Guid> selected = GlazingFixture.ApertureGuids(analyticalModel, GlazingFixture.CurrentGuid).Take(3).ToList();
            GlazingViewModel viewModel = await Ready(analyticalModel, selected);

            Assert.Equal("Applies to 20 apertures using GLZ (3 selected).", viewModel.ScopeText);

            viewModel.ApplyScope = GlazingApplyScope.SelectedApertures;
            Assert.Equal("Applies to 3 selected apertures of the 20 using GLZ.", viewModel.ScopeText);

            viewModel.ApplyScope = GlazingApplyScope.DontAssign;
            Assert.Equal("Adds the chosen system to the model without assigning it to any aperture.", viewModel.ScopeText);
        }

        [Fact]
        public async Task ChoosingASystem_StatesWhatEntersTheModel_AndTheNameItGets()
        {
            GlazingViewModel viewModel = await Ready();

            viewModel.SelectedGuid = GlazingFixture.BetterGuid;

            // A different system with the SAME name as the current one: it is added under a name of its own.
            Assert.Equal("Adds GLZ 2 and 1 material to the model; GLZ stays unchanged.", viewModel.ResultText);
            Assert.Equal("GLZ 2", viewModel.ModelName(viewModel.ProposedRow.Candidate));
        }

        [Fact]
        public async Task ChoosingASystemTheModelAlreadyHas_SaysSo()
        {
            ApertureConstruction other = GlazingFixture.System(new Guid("a0000000-0000-4000-8000-0000000000aa"), "Other", ApertureType.Window, GlazingFixture.Clear);
            AnalyticalModel analyticalModel = GlazingFixture.Model(5, other, 2);
            GlazingViewModel viewModel = await Ready(analyticalModel);

            viewModel.SelectedGuid = other.Guid;

            Assert.Equal("Uses Other, already in the model; GLZ stays unchanged.", viewModel.ResultText);
        }

        [Fact]
        public async Task TheChange_IsDescribedByTheValuesAndTheBuildUp()
        {
            GlazingViewModel viewModel = await Ready();

            viewModel.SelectedGuid = GlazingFixture.BetterGuid;

            Assert.StartsWith("Ug 1.40 → 1.10 · g 0.60 → 0.50 · light 0.78 → 0.70 · Uw 1.47 → 1.20", viewModel.ChangeText);
            Assert.EndsWith("pane build-up changes, same frame.", viewModel.ChangeText);
        }

        [Fact]
        public async Task AFramelessChoice_WarnsThatTheFrameIsLost()
        {
            GlazingViewModel viewModel = await Ready();

            viewModel.SelectedGuid = GlazingFixture.PaneOnlyGuid;

            Assert.Contains(viewModel.Warnings, x => x.StartsWith("The chosen system has no frame layers"));
        }

        [Fact]
        public async Task OtherConstructionsWithTheName_AndOtherSelectedApertures_AreWarnedAbout()
        {
            ApertureConstruction twin = GlazingFixture.System(new Guid("a0000000-0000-4000-8000-0000000000bb"), GlazingFixture.CurrentName, ApertureType.Window, GlazingFixture.Clear);
            AnalyticalModel analyticalModel = GlazingFixture.Model(6, twin, 2);
            List<Guid> selected = GlazingFixture.ApertureGuids(analyticalModel, GlazingFixture.CurrentGuid).Take(2).Concat(GlazingFixture.ApertureGuids(analyticalModel, twin.Guid).Take(1)).ToList();

            GlazingViewModel viewModel = await Ready(analyticalModel, selected);

            Assert.Equal(6, viewModel.AperturesUsingCount);
            Assert.Equal(2, viewModel.SelectedAperturesCount);
            Assert.Equal(1, viewModel.OtherSelectedCount);
            Assert.Contains("1 other aperture construction is also named GLZ; it is not changed.", viewModel.Warnings);
            Assert.Contains("1 other selected aperture does not use GLZ and is not affected.", viewModel.Warnings);
        }

        [Fact]
        public async Task ASelectedApertureOfAnotherType_IsCountedAsMixed()
        {
            ApertureConstruction door = GlazingFixture.System(new Guid("a0000000-0000-4000-8000-0000000000cc"), "Door", ApertureType.Door, GlazingFixture.Clear);
            AnalyticalModel analyticalModel = GlazingFixture.Model(4, door, 1);
            List<Guid> selected = GlazingFixture.ApertureGuids(analyticalModel, GlazingFixture.CurrentGuid).Take(1).Concat(GlazingFixture.ApertureGuids(analyticalModel, door.Guid)).ToList();

            GlazingViewModel viewModel = await Ready(analyticalModel, selected);

            Assert.Equal(1, viewModel.MixedApertureTypeCount);
            Assert.Contains(viewModel.Warnings, x => x.Contains("of another type than GLZ"));
        }

        [Fact]
        public async Task TheScopeChangesWhichAperturesUwIsWeighedOver()
        {
            AnalyticalModel analyticalModel = GlazingFixture.Model();
            List<Guid> selected = GlazingFixture.ApertureGuids(analyticalModel, GlazingFixture.CurrentGuid).Take(2).ToList();
            GlazingViewModel viewModel = await Ready(analyticalModel, selected);

            double all = viewModel.CurrentRow.Uw;
            viewModel.ApplyScope = GlazingApplyScope.SelectedApertures;

            // The same windows everywhere: the weighted value does not depend on how many, and is recomputed, not stale.
            Assert.Equal(all, viewModel.CurrentRow.Uw, 6);
        }

        // ---- Advanced: where the candidates come from ----------------------------------------------------------

        [Fact]
        public async Task TheLibraryCanBeLeftOut()
        {
            GlazingViewModel viewModel = await Ready();

            viewModel.IncludeLibrary = false;

            Assert.Equal(new[] { GlazingFixture.CurrentGuid }, viewModel.Rows.Select(x => x.Guid).ToArray());
        }

        // ---- Load more glazing ---------------------------------------------------------------------------------

        [Fact]
        public async Task LoadingMoreGlazing_CalculatesOnlyTheNewSystems_AndAddsThemToTheWindowPool()
        {
            FakeGlazingEvaluator evaluator = new FakeGlazingEvaluator();
            AnalyticalModel analyticalModel = GlazingFixture.Model();
            GlazingViewModel viewModel = await Ready(analyticalModel, evaluator: evaluator);

            await viewModel.AddSourceAsync(GlazingFixture.LoadedGood());

            Assert.Equal(2, evaluator.Requests.Count);
            Assert.Equal(new[] { GlazingFixture.LoadedGuid }, evaluator.Requests[1].Batches.SelectMany(x => x.Guids).ToArray());
            Assert.Equal(GlazingFixture.LoadedGuid, viewModel.Rows.First().Guid);
            Assert.Equal("good.json", viewModel.Rows.First().SourceLabel);
            Assert.Equal("Showing 5 of 5 systems.", viewModel.CandidateCountText);

            // The model is not touched by loading.
            Assert.Equal(1, analyticalModel.AdjacencyCluster.GetApertureConstructions().Count);
            Assert.Null(analyticalModel.MaterialLibrary.GetMaterial(GlazingFixture.LowE));
        }

        [Fact]
        public async Task ALoadedSystemWhoseMaterialDiffersFromTheModels_IsBlocked()
        {
            GlazingViewModel viewModel = await Ready();

            await viewModel.AddSourceAsync(GlazingFixture.Loaded());

            GlazingCandidateRow row = viewModel.Rows.Single(x => x.Guid == GlazingFixture.DifferentMaterialGuid);
            Assert.False(row.CanApply);
            Assert.Contains("'Clear6' differs from the model's material of the same name", row.Candidate.MaterialIssue);
        }

        [Fact]
        public async Task LoadedFilesCanBeLeftOut_AndTheirNotesAreShown()
        {
            GlazingViewModel viewModel = await Ready();
            GlazingSource source = new GlazingSource(GlazingSourceKind.Loaded, "panes.tcd", new ConstructionManager()) { Note = "panes.tcd contains 11,664 panes and no glazing systems." };

            await viewModel.AddSourceAsync(source);
            await viewModel.AddSourceAsync(GlazingFixture.LoadedGood());

            Assert.Equal(new[] { "panes.tcd contains 11,664 panes and no glazing systems." }, viewModel.Notes.ToArray());

            viewModel.IncludeLoaded = false;
            Assert.DoesNotContain(viewModel.Rows, x => x.Guid == GlazingFixture.LoadedGuid);
        }

        [Fact]
        public async Task ResultsArrivingOutOfOrder_AllEndUpInTheTable()
        {
            ManualGlazingEvaluator manual = new ManualGlazingEvaluator();
            GlazingViewModel viewModel = new GlazingViewModel(GlazingFixture.Model(), GlazingFixture.CurrentGuid, null, manual, GlazingFixture.Library());

            Task initial = viewModel.InitializeAsync();
            Task more = viewModel.AddSourceAsync(GlazingFixture.LoadedGood());
            Assert.Equal(2, manual.Calls.Count);

            // The second calculation (the loaded file) answers first.
            manual.Release(manual.Calls[1]);
            await more;
            Assert.True(viewModel.IsBusy);
            Assert.Equal(GlazingPreviewStatus.Calculating, viewModel.Status);

            manual.Release(manual.Calls[0]);
            await initial;

            Assert.False(viewModel.IsBusy);
            Assert.Equal(GlazingPreviewStatus.Ready, viewModel.Status);
            Assert.All(viewModel.Rows, x => Assert.NotNull(x.Values));
            Assert.Equal(5, viewModel.Rows.Count);
        }

        // ---- The request ---------------------------------------------------------------------------------------

        [Fact]
        public async Task TheRequest_CarriesTheChosenSystem_TheMaterialsToAdd_TheScopeAndTheValuesShown()
        {
            AnalyticalModel analyticalModel = GlazingFixture.Model();
            List<Guid> selected = GlazingFixture.ApertureGuids(analyticalModel, GlazingFixture.CurrentGuid).Take(3).ToList();
            GlazingViewModel viewModel = await Ready(analyticalModel, selected);
            viewModel.TargetText = "1.25";
            viewModel.ApplyScope = GlazingApplyScope.SelectedApertures;

            SetGlazingRequest request = viewModel.CreateRequest();

            Assert.NotNull(request);
            Assert.Equal(GlazingFixture.CurrentGuid, request.SourceApertureConstructionGuid);
            Assert.Equal(GlazingFixture.BetterGuid, request.ApertureConstruction.Guid);
            Assert.Equal(new[] { GlazingFixture.LowE }, request.MaterialsToAdd.Select(x => x.Name).ToArray());
            Assert.Equal(GlazingApplyScope.SelectedApertures, request.Scope);
            Assert.Equal(selected.OrderBy(x => x), request.SelectedApertureGuids.OrderBy(x => x));
            Assert.Equal(1.10, request.Values.Ug);
            Assert.Equal(1.40, request.OldValues.Ug);
            Assert.Equal(Uw(1.40, 2.00), request.OldUw, 3);
            Assert.Equal(Uw(1.10, 2.00), request.NewUw, 3);
            Assert.Equal(1.25, request.TargetUw);
            Assert.Equal(GlazingUwBasis.Area, request.UwBasis);
            Assert.Equal(GlazingSourceKind.Library, request.Source.Kind);
        }

        [Fact]
        public async Task NoRequest_WhileApplyIsDisabled()
        {
            GlazingViewModel viewModel = await Ready();

            Assert.Null(viewModel.CreateRequest());
        }

        [Fact]
        public void TheViewModelNeedsAModelAndAnApertureConstructionThatIsInIt()
        {
            Assert.Throws<ArgumentNullException>(() => new GlazingViewModel(null, GlazingFixture.CurrentGuid, null, new FakeGlazingEvaluator(), null));
            Assert.Throws<ArgumentException>(() => new GlazingViewModel(GlazingFixture.Model(), Guid.NewGuid(), null, new FakeGlazingEvaluator(), null));
        }

        // ---- The Tas evaluator -----------------------------------------------------------------------------------

        [Fact]
        public void TheTasEvaluator_MapsGlazingResultsToValues_OnAnStaWorker_OnceForEachSource()
        {
            List<ApartmentState> apartments = new List<ApartmentState>();
            int calls = 0;
            using TasGlazingEvaluator evaluator = new TasGlazingEvaluator((constructionManager, guids) =>
            {
                calls++;
                apartments.Add(Thread.CurrentThread.GetApartmentState());
                return guids.Select(x =>
                {
                    GlazingValues v = GlazingFixture.Values[x];
                    return double.IsNaN(v.Uf)
                        ? new GlazingCalculationResult(x, "Fake", v.G, v.LightTransmittance, v.Ug)
                        : (GlazingCalculationResult)new ApertureGlazingCalculationResult(x, "Fake", v.G, v.LightTransmittance, v.Ug, 0, 0, v.Uf);
                }).ToList();
            });

            GlazingSource library = GlazingFixture.Library();
            GlazingEvaluation result = evaluator.EvaluateAsync(new GlazingEvaluationRequest(new[]
            {
                new GlazingEvaluationBatch(library, new[] { GlazingFixture.BetterGuid, GlazingFixture.PaneOnlyGuid }),
                new GlazingEvaluationBatch(GlazingFixture.LoadedGood(), new[] { GlazingFixture.LoadedGuid }),
            }), CancellationToken.None).Result;

            Assert.Equal(2, calls);
            Assert.All(apartments, x => Assert.Equal(ApartmentState.STA, x));
            Assert.Null(result.Error);
            Assert.Equal(3, result.Values.Count);
            Assert.Equal(1.10, result.Values[GlazingFixture.BetterGuid].Ug);
            Assert.Equal(2.00, result.Values[GlazingFixture.BetterGuid].Uf);
            Assert.Equal(0.50, result.Values[GlazingFixture.BetterGuid].G);
            Assert.True(double.IsNaN(result.Values[GlazingFixture.PaneOnlyGuid].Uf));
        }

        [Fact]
        public void TheTasEvaluator_ReportsWhyNothingWasCalculated()
        {
            using (TasGlazingEvaluator evaluator = new TasGlazingEvaluator((constructionManager, guids) => throw new InvalidOperationException("TCD is not installed.")))
            {
                GlazingEvaluation result = evaluator.EvaluateAsync(new GlazingEvaluationRequest(new[] { new GlazingEvaluationBatch(GlazingFixture.Library(), new[] { GlazingFixture.BetterGuid }) }), CancellationToken.None).Result;

                Assert.Empty(result.Values);
                Assert.Equal("TCD is not installed.", result.Error);
            }

            using (TasGlazingEvaluator evaluator = new TasGlazingEvaluator((constructionManager, guids) => new List<GlazingCalculationResult>()))
            {
                GlazingEvaluation result = evaluator.EvaluateAsync(new GlazingEvaluationRequest(new[] { new GlazingEvaluationBatch(GlazingFixture.Library(), new[] { GlazingFixture.BetterGuid }) }), CancellationToken.None).Result;

                Assert.Empty(result.Values);
                Assert.Equal("Tas could not calculate the glazing values.", result.Error);
            }
        }
    }
}
