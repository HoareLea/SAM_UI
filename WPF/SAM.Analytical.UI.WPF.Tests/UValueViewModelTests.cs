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
    /// U-value PR2a: the "Set U-value" view-model. It pre-fills the target with the current U, names the
    /// auto-detected layer, previews the change, states the apply scope inline, warns about mixed panel groups and
    /// shared names, keeps Apply disabled until a reached result for the CURRENT inputs is shown, and drops stale
    /// results. The evaluator is the real <see cref="TasUValueEvaluator"/> logic on <see cref="FakeTas"/>.
    /// </summary>
    public class UValueViewModelTests
    {
        private static async Task<UValueViewModel> Open(AnalyticalModel analyticalModel, Construction construction, IEnumerable<Guid> selected = null, IUValueEvaluator evaluator = null)
        {
            UValueViewModel viewModel = new UValueViewModel(analyticalModel, construction.Guid, selected, evaluator ?? new ImmediateUValueEvaluator());
            await viewModel.InitializeAsync();
            return viewModel;
        }

        private static async Task Type(UValueViewModel viewModel, string text)
        {
            viewModel.TargetText = text;
            await viewModel.LastEvaluationTask;
        }

        // -------------------------------------------------------------------------------------------------
        // Opening
        // -------------------------------------------------------------------------------------------------

        [Fact]
        public async Task Open_PrefillsTheTargetWithTheCurrentU_AndApplyIsDisabled()
        {
            AnalyticalModel analyticalModel = UValueFixture.Model(out Construction construction);

            UValueViewModel viewModel = await Open(analyticalModel, construction);

            Assert.Equal(0.26, viewModel.CurrentThermalTransmittance, 3);
            Assert.Equal(0.26.ToString("0.###"), viewModel.TargetText);
            Assert.Equal(UValuePreviewStatus.NoChange, viewModel.Status);
            Assert.False(viewModel.ApplyEnabled);
            Assert.Null(viewModel.CreateRequest());
            Assert.Equal(12, viewModel.PanelsUsingCount);
        }

        [Fact]
        public async Task Open_NamesTheAutoDetectedLayer_NeverTheAirGap()
        {
            AnalyticalModel analyticalModel = UValueFixture.Model(out Construction construction);

            UValueViewModel viewModel = await Open(analyticalModel, construction);

            Assert.Equal(UValueFixture.WoolIndex, viewModel.AutomaticLayerIndex);
            Assert.Equal(UValueFixture.WoolIndex, viewModel.LayerIndex);
            Assert.Equal("I01_Mineral Wool 80 mm will be adjusted; other layers stay fixed.", viewModel.LayerSentence);
            Assert.Equal(5, viewModel.Layers.Count);
            Assert.True(viewModel.Layers[UValueFixture.WoolIndex].Adjusted);
        }

        [Fact]
        public async Task Open_KeepsTheReachableRange_AfterThePrefilledNoChangeTarget()
        {
            // Found by the real-Tas engine probe: the "no change" pre-fill cleared the evaluation and with it the range.
            AnalyticalModel analyticalModel = UValueFixture.Model(out Construction construction);
            UValueViewModel viewModel = await Open(analyticalModel, construction);

            Assert.Equal(UValuePreviewStatus.NoChange, viewModel.Status);
            Assert.Equal(UValueFixture.U(0.001), viewModel.MinThicknessThermalTransmittance, 6);
            Assert.Equal(UValueFixture.U(1.0), viewModel.MaxThicknessThermalTransmittance, 6);

            // A different range invalidates the stored values until they are calculated for it.
            viewModel.MaxThickness = 0.5;
            Assert.True(double.IsNaN(viewModel.MaxThicknessThermalTransmittance));
            await viewModel.LastEvaluationTask;
        }

        // -------------------------------------------------------------------------------------------------
        // Preview
        // -------------------------------------------------------------------------------------------------

        [Fact]
        public async Task ReachableTarget_IsReachedWithinTolerance_ShowsThePreview_AndEnablesApply()
        {
            AnalyticalModel analyticalModel = UValueFixture.Model(out Construction construction);
            UValueViewModel viewModel = await Open(analyticalModel, construction);

            await Type(viewModel, "0.3");

            Assert.Equal(UValuePreviewStatus.Reached, viewModel.Status);
            Assert.True(viewModel.ApplyEnabled);
            Assert.InRange(Math.Abs(viewModel.CalculatedThermalTransmittance - 0.3), 0, Query.UValueTargetTolerance);
            Assert.Equal(UValueFixture.Thickness(0.3), viewModel.CalculatedThickness, 6);
            Assert.Equal(0.3 - viewModel.CalculatedThermalTransmittance, viewModel.Margin, 9);
            Assert.True(viewModel.LastEvaluationMilliseconds >= 0);

            UValueLayerRow wool = viewModel.PreviewRows[UValueFixture.WoolIndex];
            Assert.True(wool.Adjusted);
            Assert.Equal(80, wool.ThicknessBefore);
            Assert.Equal(67, wool.ThicknessAfter);
            Assert.All(viewModel.PreviewRows.Where(x => !x.Adjusted), x => Assert.Equal(x.ThicknessBefore, x.ThicknessAfter));
        }

        [Fact]
        public async Task UnreachableTarget_KeepsApplyDisabled_AndNamesTheBestAchievableU()
        {
            AnalyticalModel analyticalModel = UValueFixture.Model(out Construction construction);
            UValueViewModel viewModel = await Open(analyticalModel, construction);

            await Type(viewModel, "0.01");

            Assert.Equal(UValuePreviewStatus.Unreachable, viewModel.Status);
            Assert.False(viewModel.ApplyEnabled);
            Assert.Null(viewModel.CreateRequest());
            Assert.Equal(UValueFixture.U(1.0), viewModel.BestAchievableThermalTransmittance, 6);
            Assert.Contains("not reachable", viewModel.StatusMessage);
            Assert.Contains("Best achievable: U 0.025 W/m²K at 1000 mm.", viewModel.StatusMessage);
        }

        [Fact]
        public async Task InvalidTarget_ExplainsAndKeepsApplyDisabled()
        {
            AnalyticalModel analyticalModel = UValueFixture.Model(out Construction construction);
            UValueViewModel viewModel = await Open(analyticalModel, construction);

            await Type(viewModel, "abc");

            Assert.Equal(UValuePreviewStatus.None, viewModel.Status);
            Assert.Equal("Enter a target U-value in W/m²K.", viewModel.StatusMessage);
            Assert.False(viewModel.ApplyEnabled);
        }

        [Fact]
        public async Task LayerOverride_EvaluatesTheChosenLayer()
        {
            AnalyticalModel analyticalModel = UValueFixture.Model(out Construction construction);
            ImmediateUValueEvaluator evaluator = new ImmediateUValueEvaluator();
            UValueViewModel viewModel = await Open(analyticalModel, construction, evaluator: evaluator);
            await Type(viewModel, "0.3");

            viewModel.LayerIndexOverride = 1;
            await viewModel.LastEvaluationTask;

            Assert.Equal(1, viewModel.LayerIndex);
            Assert.Equal(1, evaluator.Requests.Last().LayerIndex);
            Assert.Equal("Cement Particleboard 12 mm will be adjusted; other layers stay fixed.", viewModel.LayerSentence);
            Assert.False(viewModel.ApplyEnabled);
        }

        [Fact]
        public async Task ThicknessRange_IsPassedToTheEvaluator()
        {
            AnalyticalModel analyticalModel = UValueFixture.Model(out Construction construction);
            ImmediateUValueEvaluator evaluator = new ImmediateUValueEvaluator();
            UValueViewModel viewModel = await Open(analyticalModel, construction, evaluator: evaluator);
            await Type(viewModel, "0.05");
            Assert.Equal(UValuePreviewStatus.Reached, viewModel.Status);

            viewModel.MaxThickness = 0.1;
            await viewModel.LastEvaluationTask;

            Assert.Equal(0.1, evaluator.Requests.Last().MaxThickness);
            Assert.Equal(UValuePreviewStatus.Unreachable, viewModel.Status);
            Assert.Contains("within 1-100 mm", viewModel.StatusMessage);
        }

        // -------------------------------------------------------------------------------------------------
        // Scope
        // -------------------------------------------------------------------------------------------------

        [Fact]
        public async Task ScopeText_StatesTheScopeInline_ForEveryApplyModeAndScope()
        {
            AnalyticalModel analyticalModel = UValueFixture.Model(out Construction construction);
            List<Guid> selected = UValueFixture.PanelGuids(analyticalModel, construction).Take(3).ToList();
            UValueViewModel viewModel = await Open(analyticalModel, construction, selected);
            await Type(viewModel, "0.3");

            Assert.Equal("Applies to 12 panels using SIM_EXT_SLD (3 selected).", viewModel.ScopeText);
            Assert.Equal("Creates SIM_EXT_SLD U0.30; SIM_EXT_SLD stays unchanged.", viewModel.ResultText);

            viewModel.ApplyScope = UValueApplyScope.SelectedPanels;
            Assert.Equal("Applies to 3 selected panels of the 12 using SIM_EXT_SLD.", viewModel.ScopeText);

            viewModel.ApplyScope = UValueApplyScope.DontAssign;
            Assert.Equal("Creates SIM_EXT_SLD U0.30 without assigning it to any panel.", viewModel.ScopeText);

            viewModel.ApplyMode = UValueApplyMode.ModifyInPlace;
            Assert.Equal(UValueApplyScope.AllPanels, viewModel.EffectiveScope);
            Assert.Equal("Applies to all 12 panels using SIM_EXT_SLD (3 selected), changed in place.", viewModel.ScopeText);
            Assert.Equal("Modifies SIM_EXT_SLD; every panel using it changes.", viewModel.ResultText);
            Assert.True(viewModel.ApplyEnabled);
        }

        [Fact]
        public async Task SelectedPanelsOnly_WithNoSelectedPanelUsingIt_BlocksApply()
        {
            AnalyticalModel analyticalModel = UValueFixture.Model(out Construction construction);
            UValueViewModel viewModel = await Open(analyticalModel, construction, new[] { Guid.NewGuid() });
            await Type(viewModel, "0.3");

            Assert.Equal(0, viewModel.SelectedPanelsCount);
            Assert.Equal("Applies to 12 panels using SIM_EXT_SLD.", viewModel.ScopeText);

            viewModel.ApplyScope = UValueApplyScope.SelectedPanels;

            Assert.Equal(UValuePreviewStatus.Reached, viewModel.Status);
            Assert.False(viewModel.ApplyEnabled);
            Assert.Equal("No selected panel uses SIM_EXT_SLD.", viewModel.ApplyBlockReason);
        }

        [Fact]
        public async Task CreateRequest_CarriesTheReachedResultAndTheOptions()
        {
            AnalyticalModel analyticalModel = UValueFixture.Model(out Construction construction);
            List<Guid> selected = UValueFixture.PanelGuids(analyticalModel, construction).Take(2).ToList();
            UValueViewModel viewModel = await Open(analyticalModel, construction, selected);
            await Type(viewModel, "0.3");
            viewModel.ApplyScope = UValueApplyScope.SelectedPanels;

            SetUValueRequest request = viewModel.CreateRequest();

            Assert.Equal(construction.Guid, request.ConstructionGuid);
            Assert.Equal(UValueFixture.WoolIndex, request.LayerIndex);
            Assert.Equal(UValueFixture.Thickness(0.3), request.Thickness, 6);
            Assert.Equal(0.3, request.TargetThermalTransmittance);
            Assert.Equal(UValueApplyMode.NewConstruction, request.Mode);
            Assert.Equal(UValueApplyScope.SelectedPanels, request.Scope);
            Assert.Equal(selected, request.SelectedPanelGuids);
            Assert.Equal("SIM_EXT_SLD U0.30", request.NewConstructionName);
            Assert.Equal(HeatFlowDirection.Horizontal, request.HeatFlowDirection);
        }

        // -------------------------------------------------------------------------------------------------
        // Heat-flow basis and warnings
        // -------------------------------------------------------------------------------------------------

        [Fact]
        public async Task HeatFlowBasis_ComesFromThePanels()
        {
            AnalyticalModel analyticalModel = UValueFixture.Model(out Construction construction, walls: 0, roofs: 4);
            ImmediateUValueEvaluator evaluator = new ImmediateUValueEvaluator();

            UValueViewModel viewModel = await Open(analyticalModel, construction, evaluator: evaluator);

            // The construction's Default Panel Type is the generic Wall; the panels are roofs, so heat flows up.
            Assert.True(viewModel.HeatFlowBasis.FromPanels);
            Assert.Equal(PanelType.Roof, viewModel.HeatFlowBasis.PanelType);
            Assert.Equal(HeatFlowDirection.Up, viewModel.HeatFlowDirection);
            Assert.Equal(HeatFlowDirection.Up, evaluator.Requests.Last().HeatFlowDirection);
            Assert.Empty(viewModel.Warnings);
        }

        [Fact]
        public async Task HeatFlowBasis_FallsBackToTheConstructionDefaultPanelType_WhenNoPanelUsesIt()
        {
            AnalyticalModel analyticalModel = UValueFixture.Model(out Construction construction, walls: 0);

            UValueViewModel viewModel = await Open(analyticalModel, construction);

            Assert.False(viewModel.HeatFlowBasis.FromPanels);
            Assert.Equal(PanelType.Wall, viewModel.HeatFlowBasis.PanelType);
            Assert.Equal(HeatFlowDirection.Horizontal, viewModel.HeatFlowDirection);
            Assert.True(viewModel.External);
            Assert.Equal("No panel uses SIM_EXT_SLD; only the new construction is created.", viewModel.ScopeText);
        }

        [Fact]
        public async Task MixedPanelGroups_WarnAndUseTheMostCommonBasis()
        {
            AnalyticalModel analyticalModel = UValueFixture.Model(out Construction construction, walls: 12, roofs: 2);

            UValueViewModel viewModel = await Open(analyticalModel, construction);

            Assert.True(viewModel.HeatFlowBasis.Mixed);
            Assert.Equal(HeatFlowDirection.Horizontal, viewModel.HeatFlowDirection);
            string warning = Assert.Single(viewModel.Warnings);
            Assert.Equal("The affected panels are of mixed types (12 WallExternal, 2 Roof); the U-value is calculated for WallExternal (horizontal heat flow).", warning);
        }

        [Fact]
        public async Task SelectingOnlyRoofPanels_RecalculatesOnTheRoofBasis()
        {
            AnalyticalModel analyticalModel = UValueFixture.Model(out Construction construction, walls: 12, roofs: 2);
            List<Guid> roofs = analyticalModel.AdjacencyCluster.GetPanels(construction).FindAll(x => x.PanelType == PanelType.Roof).ConvertAll(x => x.Guid);
            ImmediateUValueEvaluator evaluator = new ImmediateUValueEvaluator();
            UValueViewModel viewModel = await Open(analyticalModel, construction, roofs, evaluator);
            await Type(viewModel, "0.3");

            viewModel.ApplyScope = UValueApplyScope.SelectedPanels;
            await viewModel.LastEvaluationTask;

            Assert.Equal(HeatFlowDirection.Up, viewModel.HeatFlowDirection);
            Assert.Equal(HeatFlowDirection.Up, evaluator.Requests.Last().HeatFlowDirection);
            Assert.False(viewModel.HeatFlowBasis.Mixed);
            Assert.True(viewModel.ApplyEnabled);
        }

        [Fact]
        public async Task UndefinedHeatFlowBasis_FailsWithThePr1Message_UntilADirectionIsChosen()
        {
            Construction construction = UValueFixture.Wall(defaultPanelType: null);
            AdjacencyCluster adjacencyCluster = new AdjacencyCluster();
            adjacencyCluster.AddObject(construction);
            AnalyticalModel analyticalModel = new AnalyticalModel("UValue", null, null, null, adjacencyCluster, UValueFixture.Materials(), new ProfileLibrary("Profiles"));

            UValueViewModel viewModel = await Open(analyticalModel, construction);

            Assert.Equal(UValuePreviewStatus.Failed, viewModel.Status);
            Assert.Contains("heat-flow direction is undefined", viewModel.StatusMessage);

            viewModel.HeatFlowDirectionOverride = HeatFlowDirection.Horizontal;
            await viewModel.LastEvaluationTask;
            await Type(viewModel, "0.3");

            Assert.Equal(UValuePreviewStatus.Reached, viewModel.Status);
            Assert.True(viewModel.ApplyEnabled);
        }

        [Fact]
        public async Task OtherConstructionsSharingTheName_AreWarnedAbout_AndBlockModifyInPlace()
        {
            Construction namesake = UValueFixture.Wall();
            AnalyticalModel analyticalModel = UValueFixture.Model(out Construction construction, 12, 0, namesake);
            UValueViewModel viewModel = await Open(analyticalModel, construction);
            await Type(viewModel, "0.3");

            Assert.Equal(12, viewModel.PanelsUsingCount);
            Assert.Contains("1 other construction is also named SIM_EXT_SLD; it is not changed.", viewModel.Warnings);
            Assert.True(viewModel.ApplyEnabled);

            viewModel.ApplyMode = UValueApplyMode.ModifyInPlace;

            Assert.False(viewModel.ApplyEnabled);
            Assert.StartsWith("Modify in place is unavailable", viewModel.ApplyBlockReason);
        }

        // -------------------------------------------------------------------------------------------------
        // Stale results
        // -------------------------------------------------------------------------------------------------

        [Fact]
        public async Task StaleResult_IsDropped_AndTheLatestInputWins()
        {
            AnalyticalModel analyticalModel = UValueFixture.Model(out Construction construction);
            ManualUValueEvaluator evaluator = new ManualUValueEvaluator();
            UValueViewModel viewModel = new UValueViewModel(analyticalModel, construction.Guid, null, evaluator);

            Task opening = viewModel.InitializeAsync();
            evaluator.Complete(evaluator.Calls[0]);
            await opening;

            viewModel.TargetText = "0.3";
            Task first = viewModel.LastEvaluationTask;
            viewModel.TargetText = "0.4";
            Task second = viewModel.LastEvaluationTask;

            Assert.Equal(3, evaluator.Calls.Count);
            Assert.True(evaluator.Calls[1].CancellationToken.IsCancellationRequested);
            Assert.False(evaluator.Calls[2].CancellationToken.IsCancellationRequested);
            Assert.Equal(UValuePreviewStatus.Calculating, viewModel.Status);
            Assert.True(viewModel.IsBusy);
            Assert.False(viewModel.ApplyEnabled);

            // The newer request finishes first; the older one arrives late (a COM call cannot be interrupted).
            evaluator.Complete(evaluator.Calls[2]);
            await second;
            evaluator.Complete(evaluator.Calls[1]);
            await first;

            Assert.Equal(UValuePreviewStatus.Reached, viewModel.Status);
            Assert.Equal(0.4, viewModel.CalculatedThermalTransmittance, 3);
            Assert.Equal(0.4, viewModel.CreateRequest().TargetThermalTransmittance);
            Assert.Equal(UValueFixture.Thickness(0.4), viewModel.CreateRequest().Thickness, 6);
        }

        [Fact]
        public async Task ChangingAnInput_DisablesApply_UntilTheNewResultArrives()
        {
            AnalyticalModel analyticalModel = UValueFixture.Model(out Construction construction);
            ManualUValueEvaluator evaluator = new ManualUValueEvaluator();
            UValueViewModel viewModel = new UValueViewModel(analyticalModel, construction.Guid, null, evaluator);
            Task opening = viewModel.InitializeAsync();
            evaluator.Complete(evaluator.Calls[0]);
            await opening;

            viewModel.TargetText = "0.3";
            evaluator.Complete(evaluator.Calls[1]);
            await viewModel.LastEvaluationTask;
            Assert.True(viewModel.ApplyEnabled);

            viewModel.TargetText = "0.35";

            Assert.False(viewModel.ApplyEnabled);
            Assert.Null(viewModel.CreateRequest());
        }

        [Fact]
        public void Constructor_RejectsAConstructionThatIsNotInTheModel()
        {
            AnalyticalModel analyticalModel = UValueFixture.Model(out Construction _);

            Assert.Throws<ArgumentException>(() => new UValueViewModel(analyticalModel, Guid.NewGuid(), null, new ImmediateUValueEvaluator()));
        }
    }
}
