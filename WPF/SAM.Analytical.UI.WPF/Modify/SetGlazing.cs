// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Tas;
using SAM.Core;
using SAM.Core.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        /// <summary>
        /// Applies a chosen glazing system as ONE Undo step: every edit is made on one clone of the model and
        /// <c>SetJSAMObject</c> is called exactly once (never on failure). Only the chosen system and the materials it
        /// needs that the model lacks enter the model. The apertures' parameters (U, g, light, solar, Pilkington) are
        /// refreshed from a Tas calculation of the chosen system, as the legacy "assign by g-value" flow does - the
        /// whole-model <c>Tas.Modify.UpdateThermalParameters</c> only covers panel constructions, not aperture constructions.
        /// Glazing never creates or edits pane material properties.
        /// </summary>
        public static SetGlazingResult SetGlazing(this UIAnalyticalModel uIAnalyticalModel, SetGlazingRequest request)
        {
            return SetGlazing(uIAnalyticalModel, request, CalculateGlazingParameters);
        }

        /// <param name="calculate">
        /// The Tas calculation of the chosen system's aperture parameters (a TCD run). Tests pass a stand-in so they need no Tas.
        /// </param>
        internal static SetGlazingResult SetGlazing(this UIAnalyticalModel uIAnalyticalModel, SetGlazingRequest request, Func<SetGlazingRequest, ThermalTransmittanceCalculationResult> calculate)
        {
            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if (analyticalModel == null)
            {
                return new SetGlazingResult("There is no model to change.");
            }

            ThermalTransmittanceCalculationResult thermalTransmittanceCalculationResult = null;
            if (request?.ApertureConstruction != null && request.Scope != GlazingApplyScope.DontAssign)
            {
                thermalTransmittanceCalculationResult = calculate?.Invoke(request);
            }

            analyticalModel = SetGlazing(analyticalModel, request, thermalTransmittanceCalculationResult, out SetGlazingResult result);
            if (analyticalModel == null || result == null || !result.Succeeded)
            {
                return result ?? new SetGlazingResult("The glazing change could not be applied.");
            }

            uIAnalyticalModel.SetJSAMObject(analyticalModel, new FullModification());

            return result;
        }

        private static ThermalTransmittanceCalculationResult CalculateGlazingParameters(SetGlazingRequest request)
        {
            ConstructionManager constructionManager = request?.Source?.ConstructionManager;
            if (constructionManager == null || request.ApertureConstruction == null)
            {
                return null;
            }

            try
            {
                return new ThermalTransmittanceCalculator(constructionManager).Calculate(new Guid[] { request.ApertureConstruction.Guid })?.FirstOrDefault();
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// The model-only part of <see cref="SetGlazing(UIAnalyticalModel, SetGlazingRequest)"/>: returns the changed
        /// model (null on failure, with <paramref name="result"/> saying why). <paramref name="analyticalModel"/> is
        /// not modified.
        /// </summary>
        /// <param name="thermalTransmittanceCalculationResult">Tas' result for the chosen system; null falls back to the request's Ug / g / light values.</param>
        internal static AnalyticalModel SetGlazing(this AnalyticalModel analyticalModel, SetGlazingRequest request, ThermalTransmittanceCalculationResult thermalTransmittanceCalculationResult, out SetGlazingResult result)
        {
            result = null;

            if (analyticalModel == null || request == null || request.ApertureConstruction == null)
            {
                result = new SetGlazingResult("There is no model or glazing system to apply.");
                return null;
            }

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;
            if (adjacencyCluster == null)
            {
                result = new SetGlazingResult("The model has no adjacency cluster.");
                return null;
            }

            List<ApertureConstruction> apertureConstructions = adjacencyCluster.GetApertureConstructions() ?? new List<ApertureConstruction>();
            ApertureConstruction source = apertureConstructions.Find(x => x != null && x.Guid == request.SourceApertureConstructionGuid);
            if (source == null)
            {
                result = new SetGlazingResult("The current aperture construction is no longer in the model.");
                return null;
            }

            GlazingApplyScope scope = request.Scope;
            List<Aperture> apertures_Using = adjacencyCluster.GetApertures(source) ?? new List<Aperture>();
            List<Aperture> apertures;
            switch (scope)
            {
                case GlazingApplyScope.AllApertures:
                    apertures = apertures_Using;
                    break;

                case GlazingApplyScope.SelectedApertures:
                    HashSet<Guid> selected = new HashSet<Guid>(request.SelectedApertureGuids ?? Enumerable.Empty<Guid>());
                    apertures = apertures_Using.FindAll(x => selected.Contains(x.Guid));
                    if (apertures.Count == 0)
                    {
                        result = new SetGlazingResult(string.Format("None of the selected apertures uses {0}.", source.Name));
                        return null;
                    }

                    break;

                default:
                    apertures = new List<Aperture>();
                    break;
            }

            if (scope == GlazingApplyScope.AllApertures && apertures.Count == 0)
            {
                result = new SetGlazingResult(string.Format("No aperture uses {0}.", source.Name));
                return null;
            }

            // The materials: only the chosen system's, and only those the model lacks. A name the model already has
            // for another definition is refused (the materials are matched by name everywhere in SAM).
            MaterialLibrary materialLibrary = analyticalModel.MaterialLibrary ?? new MaterialLibrary("Default MaterialLibrary");
            List<string> materialNamesAdded = new List<string>();
            foreach (IMaterial material in request.MaterialsToAdd ?? Enumerable.Empty<IMaterial>())
            {
                if (material?.Name == null)
                {
                    continue;
                }

                if (materialLibrary.GetMaterial(material.Name) != null)
                {
                    continue;
                }

                if (materialLibrary.Add(material))
                {
                    materialNamesAdded.Add(material.Name);
                }
            }

            foreach (ConstructionLayer constructionLayer in (request.ApertureConstruction.PaneConstructionLayers ?? new List<ConstructionLayer>()).Concat(request.ApertureConstruction.FrameConstructionLayers ?? new List<ConstructionLayer>()))
            {
                if (constructionLayer?.Name != null && materialLibrary.GetMaterial(constructionLayer.Name) == null)
                {
                    result = new SetGlazingResult(string.Format("Material {0} of {1} is not in the Material Library.", constructionLayer.Name, request.ApertureConstruction.Name));
                    return null;
                }
            }

            // The chosen system: the model's own when it has that Guid; otherwise added, under a name no other aperture
            // construction of the model has (the legacy post-steps match aperture constructions by NAME).
            ApertureConstruction chosen = apertureConstructions.Find(x => x != null && x.Guid == request.ApertureConstruction.Guid);
            bool added = chosen == null;
            if (added)
            {
                string name = request.ApertureConstruction.Name;
                string name_Unique = name;
                for (int index = 2; apertureConstructions.Any(x => x != null && string.Equals(x.Name?.Trim(), name_Unique?.Trim(), StringComparison.OrdinalIgnoreCase)); index++)
                {
                    name_Unique = string.Format("{0} {1}", name, index);
                }

                chosen = name_Unique == name ? new ApertureConstruction(request.ApertureConstruction) : new ApertureConstruction(request.ApertureConstruction.Guid, request.ApertureConstruction, name_Unique);
            }

            bool transparent = chosen.Transparent(materialLibrary);

            List<Guid> apertureGuids = new List<Guid>();
            List<Guid> panelGuids = new List<Guid>();
            foreach (Aperture aperture in apertures)
            {
                Panel panel = adjacencyCluster.GetPanel(aperture);
                if (panel == null)
                {
                    continue;
                }

                panel = Analytical.Create.Panel(panel);

                Aperture aperture_New = new Aperture(aperture, chosen);
                SetApertureParameters(aperture_New, request.Values, thermalTransmittanceCalculationResult, transparent, panel.PanelType);

                panel.RemoveAperture(aperture.Guid);
                panel.AddAperture(aperture_New);
                adjacencyCluster.AddObject(panel);

                apertureGuids.Add(aperture_New.Guid);
                if (!panelGuids.Contains(panel.Guid))
                {
                    panelGuids.Add(panel.Guid);
                }
            }

            // An aperture construction exists in the model through its apertures or as a stored object (GetApertureConstructions
            // lists both, without merging). Store the chosen one when no aperture carries it ("don't assign") or when it was
            // stored already, and keep the source stored when its last aperture moved away: no silent deletes, and no second
            // copy of a construction its apertures already carry.
            StoreApertureConstruction(adjacencyCluster, chosen);
            StoreApertureConstruction(adjacencyCluster, source);

            AnalyticalModel analyticalModel_New = new AnalyticalModel(analyticalModel, adjacencyCluster, materialLibrary, analyticalModel.ProfileLibrary);

            // Legacy post-step, limited to the chosen system: it re-points apertures to the constructions of the
            // manager by identity (then name), and adds the layer materials. The chosen name is unique in the model.
            ConstructionManager constructionManager = new ConstructionManager(new ApertureConstruction[] { chosen }, null, materialLibrary);
            analyticalModel_New = Analytical.Query.UpdateApertureConstructions(analyticalModel_New, constructionManager) ?? analyticalModel_New;

            result = new SetGlazingResult(request, source, chosen, added, materialNamesAdded, apertureGuids, panelGuids);
            return analyticalModel_New;
        }

        // The aperture parameters, as the legacy "assign by g-value" flow sets them: the full Tas result when there is
        // one (transparent: Ug, solar, light, Pilkington; opaque: U for the panel type), else the Ug / g / light of the comparison.
        private static void SetApertureParameters(Aperture aperture, GlazingValues values, ThermalTransmittanceCalculationResult result, bool transparent, PanelType panelType)
        {
            double Round(double value) => Core.Query.Round(value, Tolerance.MacroDistance);

            if (result != null)
            {
                if (transparent)
                {
                    aperture.SetValue(ApertureParameter.ThermalTransmittance, Round(result.GetTransparentThermalTransmittance()));
                    aperture.SetValue(ApertureParameter.DirectSolarEnergyAbsorptance, Round(result.DirectSolarEnergyAbosrtptance));
                    aperture.SetValue(ApertureParameter.DirectSolarEnergyReflectance, Round(result.DirectSolarEnergyReflectance));
                    aperture.SetValue(ApertureParameter.DirectSolarEnergyTransmittance, Round(result.DirectSolarEnergyTransmittance));
                    aperture.SetValue(ApertureParameter.LightReflectance, Round(result.LightReflectance));
                    aperture.SetValue(ApertureParameter.LightTransmittance, Round(result.LightTransmittance));
                    aperture.SetValue(ApertureParameter.PilkingtonShadingLongWavelengthCoefficient, Round(result.PilkingtonLongWavelengthCoefficient));
                    aperture.SetValue(ApertureParameter.PilkingtonShadingShortWavelengthCoefficient, Round(result.PilkingtonShortWavelengthCoefficient));
                    aperture.SetValue(ApertureParameter.TotalSolarEnergyTransmittance, Round(result.TotalSolarEnergyTransmittance));
                }
                else
                {
                    aperture.SetValue(ApertureParameter.ThermalTransmittance, Round(result.GetThermalTransmittance(panelType)));
                }

                return;
            }

            if (values == null)
            {
                return;
            }

            if (!double.IsNaN(values.Ug))
            {
                aperture.SetValue(ApertureParameter.ThermalTransmittance, Round(values.Ug));
            }

            if (transparent)
            {
                if (!double.IsNaN(values.G))
                {
                    aperture.SetValue(ApertureParameter.TotalSolarEnergyTransmittance, Round(values.G));
                }

                if (!double.IsNaN(values.LightTransmittance))
                {
                    aperture.SetValue(ApertureParameter.LightTransmittance, Round(values.LightTransmittance));
                }
            }
        }

        private static void StoreApertureConstruction(AdjacencyCluster adjacencyCluster, ApertureConstruction apertureConstruction)
        {
            bool stored = adjacencyCluster.GetObject<ApertureConstruction>(apertureConstruction.Guid) != null;
            bool carried = (adjacencyCluster.GetPanels(apertureConstruction)?.Count ?? 0) != 0;
            if (stored || !carried)
            {
                adjacencyCluster.AddObject(apertureConstruction);
            }
        }
    }
}
