// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.UI.WPF.Tests.Helpers;
using SAM.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// U-value PR2a: <c>Modify.SetUValue</c>. One Apply is one Undo step (one <c>SetJSAMObject</c>, one snapshot);
    /// the adjusted material is ADDED to the Material Library (the legacy apply leaves it out and ModelCheck then
    /// reports an Error, PR1 finding); panels change per apply mode and scope; nothing is silently deleted.
    /// </summary>
    public class SetUValueTests
    {
        private const double Thickness_U030 = 0.06708333333;

        private static SetUValueRequest Request(Construction construction, UValueApplyMode mode = UValueApplyMode.NewConstruction, UValueApplyScope scope = UValueApplyScope.AllPanels, IEnumerable<Guid> selected = null)
        {
            return new SetUValueRequest()
            {
                ConstructionGuid = construction.Guid,
                LayerIndex = UValueFixture.WoolIndex,
                Thickness = Thickness_U030,
                InitialThermalTransmittance = 0.26,
                CalculatedThermalTransmittance = 0.3,
                TargetThermalTransmittance = 0.3,
                HeatFlowDirection = HeatFlowDirection.Horizontal,
                Mode = mode,
                Scope = scope,
                SelectedPanelGuids = selected,
            };
        }

        private static AnalyticalModel Apply(AnalyticalModel analyticalModel, SetUValueRequest request, out SetUValueResult result)
        {
            AnalyticalModel analyticalModel_New = Modify.SetUValue(analyticalModel, request, out result);
            Assert.True(result.Succeeded, result.Error);
            return analyticalModel_New;
        }

        private static Construction Find(AnalyticalModel analyticalModel, Guid guid)
        {
            return analyticalModel.AdjacencyCluster.GetConstructions().Find(x => x.Guid == guid);
        }

        // ModelCheck's per-object rules (SAM Create.Log) over the changed construction and its panels.
        private static List<LogRecord> Errors(AnalyticalModel analyticalModel, Construction construction)
        {
            MaterialLibrary materialLibrary = analyticalModel.MaterialLibrary;
            List<LogRecord> result = new List<LogRecord>();
            result.AddRange(Analytical.Create.Log(construction, materialLibrary) ?? new Log());
            foreach (Panel panel in analyticalModel.AdjacencyCluster.GetPanels(construction))
            {
                result.AddRange(Analytical.Create.Log(panel, materialLibrary) ?? new Log());
            }

            return result.FindAll(x => x.LogRecordType == LogRecordType.Error);
        }

        // No missing-material error (the PR1 finding), and no error the source construction did not already have:
        // the fixture wall, like the real SIM_EXT_SLD, starts with an air gap, which SAM's rules flag on both.
        private static void AssertModelCheckFindsNoMissingMaterial(AnalyticalModel original, Construction source, AnalyticalModel changed, Construction construction)
        {
            List<LogRecord> errors = Errors(changed, construction);
            Assert.DoesNotContain(errors, x => x.Text.Contains("Material Library does not contain"));

            int before = Analytical.Create.Log(source, original.MaterialLibrary).ToList().Count(x => x.LogRecordType == LogRecordType.Error);
            int after = Analytical.Create.Log(construction, changed.MaterialLibrary).ToList().Count(x => x.LogRecordType == LogRecordType.Error);
            Assert.True(before == after, string.Join(Environment.NewLine, errors.Select(x => x.Text)));
        }

        // -------------------------------------------------------------------------------------------------
        // New construction (default)
        // -------------------------------------------------------------------------------------------------

        [Fact]
        public void NewConstruction_AllPanels_ReassignsEveryPanelUsingIt_AndLeavesTheSourceUnchanged()
        {
            AnalyticalModel analyticalModel = UValueFixture.Model(out Construction source);
            List<Guid> panels = UValueFixture.PanelGuids(analyticalModel, source);

            AnalyticalModel changed = Apply(analyticalModel, Request(source), out SetUValueResult result);

            Construction construction = Find(changed, result.Construction.Guid);
            Assert.NotEqual(source.Guid, construction.Guid);
            Assert.Equal("SIM_EXT_SLD U0.30", construction.Name);
            Assert.Equal(0.067, construction.ConstructionLayers[UValueFixture.WoolIndex].Thickness, 6);
            Assert.Equal("I01_Mineral Wool_0.067m", construction.ConstructionLayers[UValueFixture.WoolIndex].Name);
            Assert.Equal(source.ConstructionLayers.Where((x, i) => i != UValueFixture.WoolIndex).Select(x => x.Name), construction.ConstructionLayers.Where((x, i) => i != UValueFixture.WoolIndex).Select(x => x.Name));

            Assert.Equal(panels.OrderBy(x => x), changed.AdjacencyCluster.GetPanels(construction).Select(x => x.Guid).OrderBy(x => x));
            Assert.Equal(12, result.PanelCount);

            // The source is unused now but still in the model, unchanged: no silent deletes.
            Construction source_After = Find(changed, source.Guid);
            Assert.NotNull(source_After);
            Assert.Empty(changed.AdjacencyCluster.GetPanels(source_After));
            Assert.Equal(0.08, source_After.ConstructionLayers[UValueFixture.WoolIndex].Thickness);

            // The input model is not modified.
            Assert.Equal(12, analyticalModel.AdjacencyCluster.GetPanels(source).Count);
        }

        [Fact]
        public void NewConstruction_AddsTheAdjustedMaterialToTheMaterialLibrary_SoModelCheckFindsNoMissingMaterial()
        {
            AnalyticalModel analyticalModel = UValueFixture.Model(out Construction source);

            AnalyticalModel changed = Apply(analyticalModel, Request(source), out SetUValueResult result);

            Assert.True(result.MaterialAdded);
            Assert.Equal("I01_Mineral Wool", result.SourceMaterialName);
            Assert.Equal("I01_Mineral Wool_0.067m", result.MaterialName);

            IMaterial material = changed.MaterialLibrary.GetMaterial("I01_Mineral Wool_0.067m");
            OpaqueMaterial opaqueMaterial = Assert.IsType<OpaqueMaterial>(material);
            Assert.Equal(UValueFixture.WoolConductivity, opaqueMaterial.ThermalConductivity);
            Assert.True(opaqueMaterial.TryGetValue(Core.MaterialParameter.DefaultThickness, out double defaultThickness));
            Assert.Equal(0.067, defaultThickness, 6);
            Assert.NotNull(changed.MaterialLibrary.GetMaterial("I01_Mineral Wool"));

            AssertModelCheckFindsNoMissingMaterial(analyticalModel, source, changed, result.Construction);
        }

        [Fact]
        public void ModelCheck_WouldReportTheMissingMaterial_IfItWereNotAdded()
        {
            // Guards the test above: the same construction against the ORIGINAL library is the PR1 ModelCheck error.
            AnalyticalModel analyticalModel = UValueFixture.Model(out Construction source);
            Apply(analyticalModel, Request(source), out SetUValueResult result);

            List<LogRecord> errors = Analytical.Create.Log(result.Construction, analyticalModel.MaterialLibrary).ToList().FindAll(x => x.LogRecordType == LogRecordType.Error);

            Assert.Contains(errors, x => x.Text.Contains("I01_Mineral Wool_0.067m"));
        }

        [Fact]
        public void ExistingAdjustedMaterial_IsReused_NotDuplicated()
        {
            AnalyticalModel analyticalModel = UValueFixture.Model(out Construction source);
            AnalyticalModel once = Apply(analyticalModel, Request(source, scope: UValueApplyScope.DontAssign), out SetUValueResult first);
            int count = once.MaterialLibrary.GetMaterials().Count;

            Apply(once, Request(source, scope: UValueApplyScope.DontAssign), out SetUValueResult second);

            Assert.True(first.MaterialAdded);
            Assert.False(second.MaterialAdded);
            Assert.Equal("SIM_EXT_SLD U0.30 (2)", second.Construction.Name);
        }

        [Fact]
        public void NewConstruction_SelectedPanelsOnly_ReassignsOnlyTheSelectedPanels()
        {
            AnalyticalModel analyticalModel = UValueFixture.Model(out Construction source);
            List<Guid> panels = UValueFixture.PanelGuids(analyticalModel, source);
            List<Guid> selected = panels.Take(3).ToList();
            selected.Add(Guid.NewGuid());

            AnalyticalModel changed = Apply(analyticalModel, Request(source, scope: UValueApplyScope.SelectedPanels, selected: selected), out SetUValueResult result);

            Assert.Equal(UValueApplyScope.SelectedPanels, result.Scope);
            Assert.Equal(panels.Take(3).OrderBy(x => x), changed.AdjacencyCluster.GetPanels(result.Construction).Select(x => x.Guid).OrderBy(x => x));
            Assert.Equal(9, changed.AdjacencyCluster.GetPanels(source).Count);
            Assert.Equal(3, result.PanelCount);
        }

        [Fact]
        public void NewConstruction_SelectedPanelsOnly_WithNoneSelected_IsRefused()
        {
            AnalyticalModel analyticalModel = UValueFixture.Model(out Construction source);

            AnalyticalModel changed = Modify.SetUValue(analyticalModel, Request(source, scope: UValueApplyScope.SelectedPanels, selected: new[] { Guid.NewGuid() }), out SetUValueResult result);

            Assert.Null(changed);
            Assert.False(result.Succeeded);
            Assert.Equal("None of the selected panels uses SIM_EXT_SLD.", result.Error);
        }

        [Fact]
        public void NewConstruction_DontAssign_ChangesNoPanel_ButKeepsTheNewConstructionAndItsMaterial()
        {
            AnalyticalModel analyticalModel = UValueFixture.Model(out Construction source);

            AnalyticalModel changed = Apply(analyticalModel, Request(source, scope: UValueApplyScope.DontAssign), out SetUValueResult result);

            Assert.Equal(0, result.PanelCount);
            Assert.Equal(12, changed.AdjacencyCluster.GetPanels(source).Count);
            Construction construction = Find(changed, result.Construction.Guid);
            Assert.NotNull(construction);
            Assert.Empty(changed.AdjacencyCluster.GetPanels(construction));
            Assert.NotNull(changed.MaterialLibrary.GetMaterial(result.MaterialName));
            AssertModelCheckFindsNoMissingMaterial(analyticalModel, source, changed, construction);
        }

        // -------------------------------------------------------------------------------------------------
        // Modify in place
        // -------------------------------------------------------------------------------------------------

        [Fact]
        public void ModifyInPlace_ChangesTheConstructionItself_ForEveryPanelUsingIt()
        {
            AnalyticalModel analyticalModel = UValueFixture.Model(out Construction source);
            List<Guid> selected = UValueFixture.PanelGuids(analyticalModel, source).Take(1).ToList();

            AnalyticalModel changed = Apply(analyticalModel, Request(source, UValueApplyMode.ModifyInPlace, UValueApplyScope.SelectedPanels, selected), out SetUValueResult result);

            Assert.Equal(UValueApplyScope.AllPanels, result.Scope);
            Assert.Equal(source.Guid, result.Construction.Guid);
            Assert.Equal("SIM_EXT_SLD", result.Construction.Name);
            List<Panel> panels = changed.AdjacencyCluster.GetPanels(source);
            Assert.Equal(12, panels.Count);
            Assert.All(panels, x => Assert.Equal(0.067, x.Construction.ConstructionLayers[UValueFixture.WoolIndex].Thickness, 6));
            Assert.Single(changed.AdjacencyCluster.GetConstructions());
            AssertModelCheckFindsNoMissingMaterial(analyticalModel, source, changed, result.Construction);
        }

        [Fact]
        public void ModifyInPlace_WhenAnotherConstructionSharesTheName_IsRefused()
        {
            Construction namesake = UValueFixture.Wall();
            AnalyticalModel analyticalModel = UValueFixture.Model(out Construction source, 12, 0, namesake);

            AnalyticalModel changed = Modify.SetUValue(analyticalModel, Request(source, UValueApplyMode.ModifyInPlace), out SetUValueResult result);

            Assert.Null(changed);
            Assert.StartsWith("Other constructions are also named SIM_EXT_SLD", result.Error);
        }

        [Fact]
        public void NewConstruction_WhenAnotherConstructionSharesTheSourceName_LeavesItsPanelsAlone()
        {
            Construction namesake = UValueFixture.Wall();
            AnalyticalModel analyticalModel = UValueFixture.Model(out Construction source, 12, 0, namesake);

            AnalyticalModel changed = Apply(analyticalModel, Request(source), out SetUValueResult result);

            Assert.Equal(12, changed.AdjacencyCluster.GetPanels(result.Construction).Count);
            Panel other = Assert.Single(changed.AdjacencyCluster.GetPanels(namesake));
            Assert.Equal(0.08, other.Construction.ConstructionLayers[UValueFixture.WoolIndex].Thickness);
        }

        // -------------------------------------------------------------------------------------------------
        // One Apply, one Undo
        // -------------------------------------------------------------------------------------------------

        [Fact]
        public void Apply_CallsSetJSAMObjectOnce_AddingExactlyOneUndoSnapshot()
        {
            AnalyticalModel analyticalModel = UValueFixture.Model(out Construction source);
            UIAnalyticalModel uIAnalyticalModel = new UIAnalyticalModel(analyticalModel);
            int modified = 0;
            int historyChanged = 0;
            int thermalParameters = 0;
            uIAnalyticalModel.Modified += (sender, e) => modified++;
            uIAnalyticalModel.HistoryChanged += (sender, e) => historyChanged++;

            SetUValueResult result = Modify.SetUValue(uIAnalyticalModel, Request(source), x => thermalParameters++);

            Assert.True(result.Succeeded, result.Error);
            Assert.Equal(1, modified);
            Assert.Equal(1, historyChanged);
            Assert.Equal(1, thermalParameters);
            Assert.True(uIAnalyticalModel.CanUndo);
            Assert.False(uIAnalyticalModel.CanRedo);

            AnalyticalModel current = uIAnalyticalModel.JSAMObject;
            Assert.Equal(12, current.AdjacencyCluster.GetPanels(result.Construction).Count);
            Assert.NotNull(current.MaterialLibrary.GetMaterial(result.MaterialName));
        }

        [Fact]
        public void Apply_ThermalParameterStep_RunsOnTheModelThatIsCommitted()
        {
            // The Tas refresh mutates the model it is given (as Tas.Modify.UpdateThermalParameters does); that very
            // model must be the one committed, so its changes are part of the same Undo step.
            AnalyticalModel analyticalModel = UValueFixture.Model(out Construction source);
            UIAnalyticalModel uIAnalyticalModel = new UIAnalyticalModel(analyticalModel);
            Panel marker = UValueFixture.Panel(source, PanelType.Roof, 99);

            Modify.SetUValue(uIAnalyticalModel, Request(source), x => x.AddPanel(marker));

            Assert.NotNull(uIAnalyticalModel.JSAMObject.AdjacencyCluster.GetObject<Panel>(marker.Guid));
        }

        [Fact]
        public void FailedApply_DoesNotTouchTheModelOrTheUndoHistory()
        {
            AnalyticalModel analyticalModel = UValueFixture.Model(out Construction source);
            UIAnalyticalModel uIAnalyticalModel = new UIAnalyticalModel(analyticalModel);
            int modified = 0;
            int thermalParameters = 0;
            uIAnalyticalModel.Modified += (sender, e) => modified++;
            SetUValueRequest request = Request(source);
            request.Thickness = double.NaN;

            SetUValueResult result = Modify.SetUValue(uIAnalyticalModel, request, x => thermalParameters++);

            Assert.False(result.Succeeded);
            Assert.Equal(0, modified);
            Assert.Equal(0, thermalParameters);
            Assert.False(uIAnalyticalModel.CanUndo);
        }

        [Fact]
        public void Result_CarriesWhatTheReportNeeds()
        {
            AnalyticalModel analyticalModel = UValueFixture.Model(out Construction source);

            Apply(analyticalModel, Request(source), out SetUValueResult result);

            Assert.Equal(UValueApplyMode.NewConstruction, result.Mode);
            Assert.Equal(source.Guid, result.SourceConstruction.Guid);
            Assert.Equal(UValueFixture.WoolIndex, result.LayerIndex);
            Assert.Equal(0.08, result.OldThickness);
            Assert.Equal(0.067, result.NewThickness, 6);
            Assert.Equal(0.26, result.OldThermalTransmittance);
            Assert.Equal(0.3, result.NewThermalTransmittance);
            Assert.Equal(0.3, result.TargetThermalTransmittance);
            Assert.Equal(HeatFlowDirection.Horizontal, result.HeatFlowDirection);
            Assert.True((DateTime.Now - result.AppliedAt).TotalMinutes < 1);
        }

        // -------------------------------------------------------------------------------------------------
        // Naming
        // -------------------------------------------------------------------------------------------------

        [Theory]
        [InlineData("SIM_EXT_SLD", 0.5, new string[0], "SIM_EXT_SLD U0.50")]
        [InlineData("SIM_EXT_SLD", 0.125, new string[0], "SIM_EXT_SLD U0.125")]
        [InlineData("SIM_EXT_SLD U0.50", 0.3, new string[0], "SIM_EXT_SLD U0.30")]
        [InlineData("SIM_EXT_SLD U0.50 (2)", 0.3, new string[0], "SIM_EXT_SLD U0.30")]
        [InlineData("SIM_EXT_SLD", 0.3, new[] { "SIM_EXT_SLD U0.30", "sim_ext_sld u0.30 (2)" }, "SIM_EXT_SLD U0.30 (3)")]
        public void UValueConstructionName_IsUniqueAndDoesNotStackSuffixes(string source, double u, string[] existing, string expected)
        {
            Assert.Equal(expected, Query.UValueConstructionName(source, u, existing));
        }
    }
}
