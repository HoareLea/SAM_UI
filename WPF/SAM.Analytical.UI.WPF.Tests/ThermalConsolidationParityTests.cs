// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Linq;
using System.Reflection;
using SAM.Analytical.UI.WPF.Windows;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// Stage E1 (consolidation / legacy parity, <c>documentation/Thermal-StageE1.md</c>): nothing was proven redundant, so nothing was retired. These tests keep that
    /// decision honest - the classic glazing / U-value entry points and the capabilities the Thermal Performance panel does not yet have still exist. Removing or
    /// redirecting one of them must be a deliberate change that updates the parity matrix (and these tests) in the same commit.
    /// </summary>
    public class ThermalConsolidationParityTests
    {
        private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        [Theory]
        [InlineData("RibbonButton_ThermalTransmittanceCalculator")]
        [InlineData("RibbonMenuItem_ThermalTransmittanceCalculator_Classic")]
        [InlineData("RibbonButton_GlazingCalculator")]
        [InlineData("RibbonMenuItem_GlazingCalculator_Classic")]
        [InlineData("RibbonButton_EditConstructions")]
        [InlineData("RibbonButton_EditApertureConstructions")]
        [InlineData("RibbonButton_EditMaterialLibrary")]
        [InlineData("RibbonToggleButton_ThermalPerformance")]
        public void The_ribbon_entry_points_of_the_classic_tools_and_the_panel_are_all_still_there(string name)
        {
            Assert.NotNull(typeof(AnalyticalWindow).GetField(name, Members));
        }

        [Theory]
        [InlineData("MenuItem_SetGlazing_Click")]
        [InlineData("MenuItem_SetUValue_Click")]
        [InlineData("MenuItem_AssignApertureConstructionByThermalTransmittance_Click")]
        [InlineData("MenuItem_AssignConstructionByThermalTransmittance_Click")]
        [InlineData("RibbonMenuItem_GlazingCalculator_Classic_Click")]
        [InlineData("RibbonMenuItem_ThermalTransmittanceCalculator_Classic_Click")]
        public void The_context_menu_and_classic_handlers_are_all_still_there(string name)
        {
            Assert.NotNull(typeof(AnalyticalWindow).GetMethod(name, Members));
        }

        [Fact]
        public void The_classic_workflows_still_have_their_implementations()
        {
            Type modify = typeof(Modify);
            foreach (string name in new[] { "CalculateGlazing", "ThermalTransmittanceCalculator_SingleConstruction", "AssignApertureApertureConstructionByThermalTransmittance", "AssignPanelConstructionByThermalTransmittance", "EditConstructions", "EditApertureConstructions" })
            {
                Assert.True(modify.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Any(x => x.Name == name), name);
            }

            foreach (string name in new[] { "SetGlazingWindow", "SetUValueWindow", "GlazingCalculationDataWindow", "GlazingCalculationResultWindow", "ThermalTransmittanceCalculationResultWindow", "ConstructionCalculationDataWindow", "ApertureConstructionCalculationDataWindow" })
            {
                Assert.NotNull(typeof(Modify).Assembly.GetType("SAM.Analytical.UI.WPF." + name));
            }
        }

        [Fact]
        public void The_capabilities_the_panel_lacks_are_still_offered_by_the_classic_glazing_window()
        {
            // The glazing view-model (shared) has the g / light filters and the "don't assign" choice; the panel only exposes the target Uw and two scopes, so the classic
            // Set glazing window is not redundant (documentation/Thermal-StageE1.md).
            Type viewModel = typeof(GlazingViewModel);
            foreach (string name in new[] { "MinGText", "MaxGText", "MinLightText", "IncludeLibrary", "IncludeLoaded" })
            {
                Assert.NotNull(viewModel.GetProperty(name));
            }

            Assert.Contains(ThermalApplyScope.DontAssign, Enum.GetValues(typeof(ThermalApplyScope)).Cast<ThermalApplyScope>());
            Assert.NotNull(typeof(SetGlazingWindow).GetField("checkBox_DontAssign", Members));
            Assert.NotNull(typeof(SetGlazingWindow).GetField("button_LoadMore", Members));
            Assert.NotNull(typeof(SetUValueWindow).GetField("checkBox_DontAssign", Members));
        }
    }
}
