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
    /// Stage E1 (consolidation / legacy parity, <c>documentation/Thermal-StageE1.md</c>) and Stage F (final convergence, <c>documentation/Thermal-StageF-Final-Convergence.md</c>):
    /// these tests protect the user-visible CAPABILITIES and who owns them - the commands a user can reach, the specialist tools that are kept on purpose, and the
    /// candidate-selection abilities that moved into the Thermal Performance panel in F1 - and deliberately avoid pinning private handlers, fields or layout, so an
    /// intentional refactoring stays possible. Changing who owns a capability must be a deliberate change that updates the ownership matrix and these tests in the same commit.
    /// </summary>
    [Collection(WpfCollection.Name)]
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

        // The retained specialist workflows, by their public entry points: calculators by criteria, the *Assign ... By* tools, the library editors and the
        // construction-level Set windows (Tools ribbon, library hand-overs: no element selected, "don't assign").
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

        // F1: the g / light filters, the order, the source toggles and the target comparison are the panel's (its glazing Change... list), on the same shared
        // view-model the construction-level Set glazing window keeps using. Behaviour: ThermalCandidateFilterTests.
        [Fact]
        public void The_glazing_candidate_filters_are_offered_by_the_panel_and_still_by_the_shared_glazing_view_model()
        {
            foreach (string capability in new[] { "GlazingMinGText", "GlazingMaxGText", "GlazingMinLightText", "GlazingIncludeLibrary", "GlazingIncludeAdded", "GlazingSelectedSort", "GlazingComparisonText" })
            {
                Assert.NotNull(typeof(ThermalRowEditor).GetProperty(capability));
            }

            foreach (string capability in new[] { "MinGText", "MaxGText", "MinLightText", "IncludeLibrary", "IncludeLoaded", "SortOrder" })
            {
                Assert.NotNull(typeof(GlazingViewModel).GetProperty(capability));
            }
        }

        // F2: the element-assignment entry points (3D right-click Set U-value... / Set glazing...) lead to the panel. Behaviour: ThermalRedirectTests.
        [WpfFact]
        public void The_right_click_Set_commands_lead_to_the_panel()
        {
            Assert.Contains("Thermal Performance", (string)Create.MenuItem_ThermalPerformance(null, false, null).ToolTip);
            Assert.Contains("Thermal Performance", (string)Create.MenuItem_ThermalPerformance(null, true, null).ToolTip);
            Assert.NotNull(typeof(ThermalPerformanceControl).GetMethod("BeginEdit"));
        }

        // Kept on purpose in the construction-level Set windows (the panel has no "don't assign"; for glazing, Save as predefined keeps a system without changing the model).
        [Fact]
        public void Adding_a_system_to_the_model_without_assigning_it_is_still_a_choice()
        {
            Assert.Contains(ThermalApplyScope.DontAssign, Enum.GetValues(typeof(ThermalApplyScope)).Cast<ThermalApplyScope>());
        }
    }
}
