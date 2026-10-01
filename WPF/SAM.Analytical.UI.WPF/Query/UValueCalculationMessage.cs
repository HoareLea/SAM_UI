// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Tas;
using System;
using System.Globalization;
using System.Linq;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Query
    {
        /// <summary>How far the achieved U-value may be from the target before the target counts as not reached [W/m2K].</summary>
        /// <remarks>
        /// Tas reports U-values to three decimals and the bisection stops within <c>Core.Tolerance.MacroDistance</c> (1e-3),
        /// so an achieved 0.051 for a target of 0.05 is a hit (measured on the real calculator, U-value PR1b).
        /// </remarks>
        internal const double UValueTargetTolerance = 0.01;

        /// <summary>
        /// Why a layer-thickness U-value calculation cannot even start, or null when it can. Checked BEFORE the
        /// (slow, COM-driven) calculation so the user gets the real reason instead of a generic failure.
        /// </summary>
        /// <param name="data">The calculation data the user confirmed.</param>
        /// <param name="constructionManager">Used to tell "no layer is selected" from "no layer can be adjusted".</param>
        internal static string UValueCalculationMessage(this LayerThicknessCalculationData data, ConstructionManager constructionManager)
        {
            if (data == null)
            {
                return null;
            }

            string constructionName = string.IsNullOrWhiteSpace(data.ConstructionName) ? "the construction" : data.ConstructionName;

            if (data.HeatFlowDirection == HeatFlowDirection.Undefined)
            {
                return string.Format("The heat-flow direction is undefined: {0} has no default panel type. Choose a Heat Flow Direction and try again.", constructionName);
            }

            if (data.LayerIndex == -1 && NoAdjustableLayer(data, constructionManager))
            {
                return NoAdjustableLayerMessage;
            }

            return null;
        }

        /// <summary>
        /// Why a layer-thickness U-value calculation did not reach its target, or null when it did. Classified from
        /// the result fields (verified on the real calculator, see the PR record):
        /// no result or an undefined initial U-value means Tas / TCD could not run (an undefined heat-flow direction
        /// is caught before the call); a valid initial U-value with an undefined thickness or achieved U-value, or an
        /// achieved U-value off the target, means the target is not reachable within the thickness range.
        /// </summary>
        internal static string UValueCalculationMessage(this LayerThicknessCalculationResult result, LayerThicknessCalculationData data, ConstructionManager constructionManager)
        {
            if (result == null || double.IsNaN(result.InitialThermalTransmittance))
            {
                return "The Tas thermal transmittance calculation is unavailable: TCD could not run, so no U-value could be calculated.";
            }

            if (result.LayerIndex == -1)
            {
                return NoAdjustableLayerMessage;
            }

            double calculated = result.CalculatedThermalTransmittance;
            if (double.IsNaN(result.Thickness) || double.IsNaN(calculated) || Math.Abs(calculated - result.ThermalTransmittance) > UValueTargetTolerance)
            {
                return string.Format(
                    CultureInfo.CurrentCulture,
                    "Target U {0} W/m²K is not reachable by varying {1} within {2}-{3} mm.",
                    result.ThermalTransmittance.ToString("0.###", CultureInfo.CurrentCulture),
                    LayerName(result, data, constructionManager),
                    Millimetres(data?.ThicknessRange?.Min),
                    Millimetres(data?.ThicknessRange?.Max));
            }

            return null;
        }

        private const string NoAdjustableLayerMessage = "No adjustable layer: all layers are gas, glass, or thinner than 10 mm.";

        private static bool NoAdjustableLayer(LayerThicknessCalculationData data, ConstructionManager constructionManager)
        {
            Construction construction = constructionManager?.GetConstructions(data.ConstructionName)?.FirstOrDefault();
            if (construction == null)
            {
                return false;
            }

            // -1 with an adjustable layer present only means the user cleared the selection: the calculator picks one.
            return Tas.Query.AdjustableLayerIndex(construction, constructionManager.MaterialLibrary) == -1;
        }

        private static string LayerName(LayerThicknessCalculationResult result, LayerThicknessCalculationData data, ConstructionManager constructionManager)
        {
            string name = null;
            Construction construction = constructionManager?.GetConstructions(data?.ConstructionName ?? result.ConstructionName)?.FirstOrDefault();
            if (construction != null && result.LayerIndex >= 0 && result.LayerIndex < construction.ConstructionLayers.Count)
            {
                name = construction.ConstructionLayers[result.LayerIndex]?.Name;
            }

            return string.IsNullOrWhiteSpace(name) ? "the selected layer" : name;
        }

        private static string Millimetres(double? metres)
        {
            return metres == null || double.IsNaN(metres.Value) ? "?" : (metres.Value * 1000).ToString("0.##", CultureInfo.CurrentCulture);
        }
    }
}
