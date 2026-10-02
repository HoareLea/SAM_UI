// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.UI.WPF.Windows;
using System;
using System.Linq;
using System.Reflection;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// Stage E1 (consolidation / legacy parity, <c>documentation/Thermal-StageE1.md</c>): nothing was proven redundant, so nothing was retired. These tests protect the
    /// user-visible CAPABILITIES behind that decision - the commands a user can reach and the abilities the Thermal Performance panel does not yet have - and deliberately avoid
    /// pinning private handlers, fields or layout, so an intentional refactoring stays possible. Retiring a capability must be a deliberate change that updates the parity matrix
    /// and these tests in the same commit.
    /// </summary>
    public class ThermalConsolidationParityTests
    {
        private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        // The ribbon commands a user clicks: the classic calculators (main button + "(classic)" arrow), the library editors, and the panel's toggle.
        [Theory]
        [InlineData("RibbonButton_ThermalTransmittanceCalculator")]
        [InlineData("RibbonMenuItem_ThermalTransmittanceCalculator_Classic")]
        [InlineData("RibbonButton_GlazingCalculator")]
        [InlineData("RibbonMenuItem_GlazingCalculator_Classic")]
        [InlineData("RibbonButton_EditConstructions")]
        [InlineData("RibbonButton_EditApertureConstructions")]
        [InlineData("RibbonButton_EditMaterialLibrary")]
        [InlineData("RibbonToggleButton_ThermalPerformance")]
        public void The_ribbon_commands_of_the_classic_tools_and_the_panel_are_still_offered(string command)
        {
            Assert.NotNull(typeof(AnalyticalWindow).GetField(command, Members));
        }

        // The classic workflows, by their public entry points: calculators by criteria, the *Assign ... By* tools and the library editors.
        [Theory]
        [InlineData("CalculateGlazing")]
        [InlineData("ThermalTransmittanceCalculator_SingleConstruction")]
        [InlineData("AssignApertureApertureConstructionByThermalTransmittance")]
        [InlineData("AssignPanelConstructionByThermalTransmittance")]
        [InlineData("EditConstructions")]
        [InlineData("EditApertureConstructions")]
        [InlineData("OpenSetGlazingWindow")]
        [InlineData("OpenSetUValueWindow")]
        public void The_classic_workflows_are_still_reachable(string workflow)
        {
            Assert.Contains(typeof(Modify).GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic), x => x.Name == workflow);
        }

        // What the Thermal Performance panel cannot do yet (so the Set windows are not redundant), as capabilities of the shared view-models and the scope model.
        [Fact]
        public void The_glazing_capabilities_the_panel_lacks_still_exist_in_the_shared_glazing_view_model()
        {
            foreach (string capability in new[] { "MinGText", "MaxGText", "MinLightText", "IncludeLibrary", "IncludeLoaded" })
            {
                Assert.NotNull(typeof(GlazingViewModel).GetProperty(capability));
            }
        }

        [Fact]
        public void Adding_a_system_to_the_model_without_assigning_it_is_still_a_choice()
        {
            Assert.Contains(ThermalApplyScope.DontAssign, Enum.GetValues(typeof(ThermalApplyScope)).Cast<ThermalApplyScope>());
        }
    }
}
