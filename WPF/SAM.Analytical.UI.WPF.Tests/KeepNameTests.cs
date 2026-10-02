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
    /// Thermal Stage A-lite: "Modify in place" is now "Keep name". It is offered only where it is safe: the construction itself
    /// changes, so every panel using it changes - never "only the selected", never "don't assign", and never while another
    /// construction shares the name (the legacy post-step matches by name). Where it is unavailable, the view-model says why.
    /// </summary>
    public class KeepNameTests
    {
        private static async Task<UValueViewModel> ViewModel(AnalyticalModel analyticalModel, Construction construction, int selected = 3)
        {
            UValueViewModel viewModel = new UValueViewModel(analyticalModel, construction.Guid, UValueFixture.PanelGuids(analyticalModel, construction).Take(selected), new ImmediateUValueEvaluator());
            await viewModel.InitializeAsync();
            viewModel.TargetText = "0.3";
            await viewModel.LastEvaluationTask;
            return viewModel;
        }

        [Fact]
        public async Task ByDefault_KeepNameIsOffAndAvailable()
        {
            AnalyticalModel analyticalModel = UValueFixture.Model(out Construction construction);
            UValueViewModel viewModel = await ViewModel(analyticalModel, construction);

            Assert.False(viewModel.KeepName);
            Assert.Equal(UValueApplyMode.NewConstruction, viewModel.ApplyMode);
            Assert.Null(viewModel.KeepNameUnavailableReason);
        }

        [Fact]
        public async Task KeepName_IsTheSameAsTheModifyInPlaceMode()
        {
            AnalyticalModel analyticalModel = UValueFixture.Model(out Construction construction);
            UValueViewModel viewModel = await ViewModel(analyticalModel, construction);

            viewModel.KeepName = true;
            Assert.Equal(UValueApplyMode.ModifyInPlace, viewModel.ApplyMode);

            viewModel.ApplyMode = UValueApplyMode.NewConstruction;
            Assert.False(viewModel.KeepName);
        }

        [Fact]
        public async Task KeepName_IsUnavailable_ForOnlyTheSelected_WithTheReason()
        {
            AnalyticalModel analyticalModel = UValueFixture.Model(out Construction construction);
            UValueViewModel viewModel = await ViewModel(analyticalModel, construction);

            viewModel.ApplyScope = ThermalApplyScope.SelectedOnly;

            Assert.Equal("Keep name changes every panel using SIM_EXT_SLD, so it cannot be combined with only the selected panels.", viewModel.KeepNameUnavailableReason);

            viewModel.ApplyScope = ThermalApplyScope.AllUsing;
            Assert.Null(viewModel.KeepNameUnavailableReason);
        }

        [Fact]
        public async Task KeepName_IsUnavailable_ForDontAssign_WithTheReason()
        {
            AnalyticalModel analyticalModel = UValueFixture.Model(out Construction construction);
            UValueViewModel viewModel = await ViewModel(analyticalModel, construction);

            viewModel.ApplyScope = ThermalApplyScope.DontAssign;

            Assert.Equal("Keep name changes SIM_EXT_SLD itself, so it cannot be combined with Don't assign.", viewModel.KeepNameUnavailableReason);
        }

        [Fact]
        public async Task KeepName_IsUnavailable_WhenAnotherConstructionSharesTheName_WithTheReason()
        {
            Construction twin = UValueFixture.Wall(); // another construction, another Guid, the same name
            AnalyticalModel analyticalModel = UValueFixture.Model(out Construction construction, 12, 0, twin);
            UValueViewModel viewModel = await ViewModel(analyticalModel, construction);

            Assert.Equal("Keep name is unavailable: 1 other construction is also named SIM_EXT_SLD and would change too.", viewModel.KeepNameUnavailableReason);
        }

        [Fact]
        public async Task WhileKeepNameIsOn_TheOtherScopesAreUnavailable_AndTheScopeIsAllUsing()
        {
            AnalyticalModel analyticalModel = UValueFixture.Model(out Construction construction);
            UValueViewModel viewModel = await ViewModel(analyticalModel, construction);

            viewModel.KeepName = true;

            Assert.Equal(ThermalApplyScope.AllUsing, viewModel.EffectiveScope);
            Assert.Equal("Keep name changes every panel using SIM_EXT_SLD; turn it off to change only the selected panels.", viewModel.ScopeUnavailableReason);
            Assert.Equal("Keep name changes SIM_EXT_SLD itself; turn it off to create a construction without assigning it.", viewModel.DontAssignUnavailableReason);
            Assert.Null(viewModel.KeepNameUnavailableReason); // it can be turned off again

            viewModel.KeepName = false;
            Assert.Null(viewModel.ScopeUnavailableReason);
            Assert.Null(viewModel.DontAssignUnavailableReason);
        }

        [Fact]
        public async Task OnlyTheSelected_IsUnavailable_WhenNoSelectedPanelUsesTheConstruction()
        {
            AnalyticalModel analyticalModel = UValueFixture.Model(out Construction construction);
            UValueViewModel viewModel = await ViewModel(analyticalModel, construction, selected: 0);

            Assert.Equal("No selected panel uses SIM_EXT_SLD.", viewModel.ScopeUnavailableReason);
        }

        [Fact]
        public async Task ApplyingWithKeepName_ChangesTheConstructionItself_KeepsItsNameAndGuid_ForEveryPanelUsingIt_InOneUndoStep()
        {
            AnalyticalModel analyticalModel = UValueFixture.Model(out Construction construction);
            UIAnalyticalModel uIAnalyticalModel = new UIAnalyticalModel(analyticalModel);
            int modified = 0;
            int historyChanged = 0;
            uIAnalyticalModel.Modified += (sender, e) => modified++;
            uIAnalyticalModel.HistoryChanged += (sender, e) => historyChanged++;

            UValueViewModel viewModel = await ViewModel(analyticalModel, construction);
            viewModel.KeepName = true;
            SetUValueRequest request = viewModel.CreateRequest();
            Assert.NotNull(request);
            Assert.Equal(UValueApplyMode.ModifyInPlace, request.Mode);
            Assert.Equal(ThermalApplyScope.AllUsing, request.Scope);

            SetUValueResult result = Modify.SetUValue(uIAnalyticalModel, request, x => { });

            Assert.True(result.Succeeded, result.Error);
            Assert.Equal(1, modified);
            Assert.Equal(1, historyChanged); // one Apply, one Undo step (Undo itself completes asynchronously on the UI thread: real-app acceptance)
            Assert.True(uIAnalyticalModel.CanUndo);
            Assert.False(uIAnalyticalModel.CanRedo);
            Assert.Equal("SIM_EXT_SLD", result.Construction.Name);
            Assert.Equal(construction.Guid, result.Construction.Guid);
            Assert.Equal(12, uIAnalyticalModel.JSAMObject.AdjacencyCluster.GetPanels(result.Construction).Count);
        }

        [Fact]
        public async Task NoUserFacingTextSays_ModifyInPlace()
        {
            // With and without another construction of the same name: every combination of scope and Keep name, set directly
            // (the window never offers the unavailable ones, but the texts must still use the user's words).
            AnalyticalModel model_1 = UValueFixture.Model(out Construction construction_1);
            AnalyticalModel model_2 = UValueFixture.Model(out Construction construction_2, 12, 0, UValueFixture.Wall());

            foreach (UValueViewModel viewModel in new[] { await ViewModel(model_1, construction_1), await ViewModel(model_2, construction_2) })
            {
                foreach (bool keepName in new[] { false, true })
                {
                    foreach (ThermalApplyScope scope in new[] { ThermalApplyScope.AllUsing, ThermalApplyScope.SelectedOnly, ThermalApplyScope.DontAssign })
                    {
                        viewModel.KeepName = false;
                        viewModel.ApplyScope = scope;
                        viewModel.KeepName = keepName;

                        IEnumerable<string> texts = new[] { viewModel.ScopeText, viewModel.ResultText, viewModel.ApplyBlockReason, viewModel.KeepNameUnavailableReason, viewModel.ScopeUnavailableReason, viewModel.DontAssignUnavailableReason }.Concat(viewModel.Warnings);
                        foreach (string text in texts.Where(x => x != null))
                        {
                            Assert.DoesNotContain("in place", text, StringComparison.OrdinalIgnoreCase);
                            Assert.DoesNotContain("modif", text, StringComparison.OrdinalIgnoreCase);
                        }
                    }
                }
            }
        }
    }
}
