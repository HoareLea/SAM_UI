// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Tas;
using SAM.Core;
using System.Collections.Generic;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// U-value calculator PR1b: the legacy Tools > U Value Calculator flow names the real reason a calculation
    /// cannot run or does not reach its target, instead of the generic "Could not calculate construction for
    /// given criteria." The result values used here were measured on the real calculator (see the PR record):
    /// a reachable target gives a thickness and an achieved U; an unreachable one gives NaN, NaN with a valid
    /// initial U; an undefined heat-flow direction gives an undefined initial U.
    /// </summary>
    public class UValueCalculationMessageTests
    {
        private const string Air = "Air50";
        private const string Wool = "Mineral Wool";
        private const string Glass = "Pane";

        private static ConstructionManager Manager(params (string Name, double Thickness)[] layers)
        {
            MaterialLibrary materialLibrary = new MaterialLibrary("Test");
            materialLibrary.Add(Analytical.Create.GasMaterial(Air, string.Empty, Air, string.Empty, 0.024, 1000, 1.2, 1.8E-5, 0.05, 1, 5));
            materialLibrary.Add(Analytical.Create.OpaqueMaterial(Wool, string.Empty, Wool, string.Empty, 0.025, 1000, 20, 0.08, 1, 0.5, 0.5, 0.5, 0.5, 0.9, 0.9, false));
            materialLibrary.Add(Analytical.Create.TransparentMaterial(Glass, string.Empty, Glass, string.Empty, 1.0, 0.006, 1, 0.8, 0.8, 0.1, 0.1, 0.1, 0.1, 0.9, 0.9, false));

            List<ConstructionLayer> constructionLayers = new List<ConstructionLayer>();
            foreach ((string name, double thickness) in layers)
            {
                constructionLayers.Add(new ConstructionLayer(name, thickness));
            }

            return new ConstructionManager(new ApertureConstruction[0], new Construction[] { new Construction("SIM_EXT_SLD", constructionLayers) }, materialLibrary);
        }

        private static ConstructionManager Wall()
        {
            return Manager((Air, 0.05), (Wool, 0.08), (Air, 0.05));
        }

        private static LayerThicknessCalculationData Data(int layerIndex = 1, HeatFlowDirection heatFlowDirection = HeatFlowDirection.Horizontal, double thermalTransmittance = 0.5)
        {
            return new LayerThicknessCalculationData("SIM_EXT_SLD", layerIndex, thermalTransmittance, heatFlowDirection, true);
        }

        private static LayerThicknessCalculationResult Result(int layerIndex, double thickness, double initial, double target, double calculated)
        {
            return new LayerThicknessCalculationResult("Tas", "SIM_EXT_SLD", layerIndex, thickness, initial, target, calculated);
        }

        // -------------------------------------------------------------------------------------------------
        // Before the call
        // -------------------------------------------------------------------------------------------------

        [Fact]
        public void BeforeCall_UndefinedHeatFlowDirection_SaysTheConstructionHasNoDefaultPanelType()
        {
            string message = Data(heatFlowDirection: HeatFlowDirection.Undefined).UValueCalculationMessage(Wall());

            Assert.Contains("heat-flow direction is undefined", message);
            Assert.Contains("no default panel type", message);
            Assert.Contains("SIM_EXT_SLD", message);
        }

        [Fact]
        public void BeforeCall_NoLayerSelected_AndAllLayersGasOrGlass_SaysThereIsNoAdjustableLayer()
        {
            ConstructionManager constructionManager = Manager((Glass, 0.006), (Air, 0.016), (Glass, 0.006));

            string message = Data(layerIndex: -1).UValueCalculationMessage(constructionManager);

            Assert.Equal("No adjustable layer: all layers are gas, glass, or thinner than 10 mm.", message);
        }

        [Fact]
        public void BeforeCall_NoLayerSelected_ButAnAdjustableLayerExists_LetsTheCalculatorPickIt()
        {
            Assert.Null(Data(layerIndex: -1).UValueCalculationMessage(Wall()));
        }

        [Fact]
        public void BeforeCall_ValidData_HasNoMessage()
        {
            Assert.Null(Data().UValueCalculationMessage(Wall()));
        }

        [Fact]
        public void BeforeCall_UndefinedHeatFlowDirection_IsReportedBeforeNoAdjustableLayer()
        {
            ConstructionManager constructionManager = Manager((Glass, 0.006), (Air, 0.016));

            string message = Data(layerIndex: -1, heatFlowDirection: HeatFlowDirection.Undefined).UValueCalculationMessage(constructionManager);

            Assert.Contains("heat-flow direction is undefined", message);
        }

        // -------------------------------------------------------------------------------------------------
        // After the call
        // -------------------------------------------------------------------------------------------------

        [Fact]
        public void AfterCall_NoResult_SaysTheTasCalculationIsUnavailable()
        {
            string message = ((LayerThicknessCalculationResult)null).UValueCalculationMessage(Data(), Wall());

            Assert.Contains("Tas thermal transmittance calculation is unavailable", message);
            Assert.Contains("TCD could not run", message);
        }

        [Fact]
        public void AfterCall_InitialUValueUndefined_SaysTheTasCalculationIsUnavailable()
        {
            LayerThicknessCalculationResult result = Result(1, double.NaN, double.NaN, 0.5, double.NaN);

            string message = result.UValueCalculationMessage(Data(), Wall());

            Assert.Contains("Tas thermal transmittance calculation is unavailable", message);
        }

        [Fact]
        public void AfterCall_ValidInitialUValue_ButNoThickness_SaysTheTargetIsNotReachable()
        {
            // Measured on the real calculator for U = 5 on SIM_EXT_SLD: initial 0.26, thickness NaN, achieved NaN.
            LayerThicknessCalculationResult result = Result(1, double.NaN, 0.26, 5, double.NaN);

            string message = result.UValueCalculationMessage(Data(thermalTransmittance: 5), Wall());

            Assert.Equal(string.Format("Target U {0} W/m²K is not reachable by varying {1} within {2}-{3} mm.", 5.ToString(), Wool, 1.ToString(), 1000.ToString()), message);
        }

        [Fact]
        public void AfterCall_AchievedUValueOffTarget_SaysTheTargetIsNotReachable()
        {
            LayerThicknessCalculationData data = Data();
            data.ThicknessRange = new Range<double>(0.001, 0.1);
            LayerThicknessCalculationResult result = Result(1, 0.1, 0.26, 0.05, 0.2);

            string message = result.UValueCalculationMessage(data, Wall());

            Assert.Contains("not reachable by varying " + Wool, message);
            Assert.Contains("within 1-100 mm", message);
        }

        [Fact]
        public void AfterCall_AchievedUValueWithinTolerance_HasNoMessage()
        {
            // Measured on the real calculator for U = 0.05: Tas reported 0.051 at 469 mm.
            LayerThicknessCalculationResult result = Result(1, 0.469, 0.26, 0.05, 0.051);

            Assert.Null(result.UValueCalculationMessage(Data(thermalTransmittance: 0.05), Wall()));
        }

        [Fact]
        public void AfterCall_ReachedTarget_HasNoMessage()
        {
            // Measured on the real calculator for U = 0.5 with the mineral wool layer: 33.8 mm, achieved 0.5.
            LayerThicknessCalculationResult result = Result(1, 0.0338, 0.26, 0.5, 0.5);

            Assert.Null(result.UValueCalculationMessage(Data(), Wall()));
        }

        [Fact]
        public void AfterCall_CalculatorFoundNoAdjustableLayer_SaysThereIsNoAdjustableLayer()
        {
            LayerThicknessCalculationResult result = Result(-1, double.NaN, 0.26, 0.5, double.NaN);

            Assert.Equal("No adjustable layer: all layers are gas, glass, or thinner than 10 mm.", result.UValueCalculationMessage(Data(layerIndex: -1), Wall()));
        }

        [Fact]
        public void AfterCall_UnknownLayerName_FallsBackToTheSelectedLayer()
        {
            LayerThicknessCalculationResult result = Result(1, double.NaN, 0.26, 5, double.NaN);

            string message = result.UValueCalculationMessage(Data(thermalTransmittance: 5), null);

            Assert.Contains("by varying the selected layer within", message);
        }
    }
}
