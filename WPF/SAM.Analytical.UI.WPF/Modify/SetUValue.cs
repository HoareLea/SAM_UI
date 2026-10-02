// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using SAM.Core.UI;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        /// <summary>
        /// Applies an evaluated U-value change as ONE Undo step: every edit is made on one clone of the model and
        /// <c>SetJSAMObject</c> is called exactly once (never on failure). Keeps the legacy post-steps
        /// (<c>UpdateConstructions</c>, <c>UpdateApertureConstructions</c>, <c>Tas.Modify.UpdateThermalParameters</c>)
        /// and, unlike the legacy apply, adds the adjusted layer material to the model's Material Library.
        /// </summary>
        public static SetUValueResult SetUValue(this UIAnalyticalModel uIAnalyticalModel, SetUValueRequest request)
        {
            return SetUValue(uIAnalyticalModel, request, x => Tas.Modify.UpdateThermalParameters(x));
        }

        /// <param name="updateThermalParameters">
        /// The Tas thermal-parameter refresh (a whole-model TCD run). Tests pass a stand-in so they need no Tas.
        /// </param>
        internal static SetUValueResult SetUValue(this UIAnalyticalModel uIAnalyticalModel, SetUValueRequest request, Action<AnalyticalModel> updateThermalParameters)
        {
            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if (analyticalModel == null)
            {
                return new SetUValueResult("There is no model to change.");
            }

            analyticalModel = SetUValue(analyticalModel, request, out SetUValueResult result);
            if (analyticalModel == null || result == null || !result.Succeeded)
            {
                return result ?? new SetUValueResult("The U-value change could not be applied.");
            }

            updateThermalParameters?.Invoke(analyticalModel);

            uIAnalyticalModel.SetJSAMObject(analyticalModel, new FullModification());

            return result;
        }

        /// <summary>
        /// The model-only part of <see cref="SetUValue(UIAnalyticalModel, SetUValueRequest)"/>: returns the changed
        /// model (null on failure, with <paramref name="result"/> saying why). <paramref name="analyticalModel"/> is
        /// not modified.
        /// </summary>
        internal static AnalyticalModel SetUValue(this AnalyticalModel analyticalModel, SetUValueRequest request, out SetUValueResult result)
        {
            result = null;

            if (analyticalModel == null || request == null)
            {
                result = new SetUValueResult("There is no model or request to apply.");
                return null;
            }

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;
            if (adjacencyCluster == null)
            {
                result = new SetUValueResult("The model has no adjacency cluster.");
                return null;
            }

            List<Construction> constructions = adjacencyCluster.GetConstructions() ?? new List<Construction>();
            Construction source = constructions.Find(x => x != null && x.Guid == request.ConstructionGuid);
            if (source == null)
            {
                result = new SetUValueResult("The construction is no longer in the model.");
                return null;
            }

            List<ConstructionLayer> constructionLayers = source.ConstructionLayers;
            if (constructionLayers == null || request.LayerIndex < 0 || request.LayerIndex >= constructionLayers.Count || constructionLayers[request.LayerIndex] == null)
            {
                result = new SetUValueResult(string.Format("{0} has no layer {1}.", source.Name, request.LayerIndex + 1));
                return null;
            }

            if (double.IsNaN(request.Thickness) || request.Thickness <= 0)
            {
                result = new SetUValueResult("No layer thickness was calculated.");
                return null;
            }

            UValueApplyMode mode = request.Mode;
            ThermalApplyScope scope = mode == UValueApplyMode.ModifyInPlace ? ThermalApplyScope.AllUsing : request.Scope;

            // UpdateConstructions (a legacy post-step) matches by NAME: keeping the name (modifying in place) would also rewrite
            // other constructions that share the name, so that combination is refused.
            if (mode == UValueApplyMode.ModifyInPlace && constructions.Any(x => x != null && x.Guid != source.Guid && x.Name == source.Name))
            {
                result = new SetUValueResult(string.Format("Other constructions are also named {0}; keeping the name would change them too. Create a new construction instead.", source.Name));
                return null;
            }

            List<Panel> panels_Using = adjacencyCluster.GetPanels(source) ?? new List<Panel>();
            List<Panel> panels = null;
            switch (scope)
            {
                case ThermalApplyScope.AllUsing:
                    panels = panels_Using;
                    break;

                case ThermalApplyScope.SelectedOnly:
                    HashSet<Guid> selected = new HashSet<Guid>(request.SelectedPanelGuids ?? Enumerable.Empty<Guid>());
                    panels = panels_Using.FindAll(x => selected.Contains(x.Guid));
                    if (panels.Count == 0)
                    {
                        result = new SetUValueResult(string.Format("None of the selected panels uses {0}.", source.Name));
                        return null;
                    }

                    break;

                default:
                    panels = new List<Panel>();
                    break;
            }

            // The adjusted material: a copy of the source material at the new default thickness, named as the
            // legacy flow names it, and ADDED to the model's Material Library (the legacy apply leaves it out,
            // and ModelCheck then reports a missing material).
            MaterialLibrary materialLibrary = analyticalModel.MaterialLibrary ?? new MaterialLibrary("Default MaterialLibrary");

            ConstructionLayer constructionLayer = constructionLayers[request.LayerIndex];
            double oldThickness = constructionLayer.Thickness;
            double thickness = Core.Query.Round(request.Thickness, Tolerance.MacroDistance);
            // Named as the legacy flow names it, "<material>_<thickness>m"; a layer already adjusted once
            // ("<material>_0.067m") is renamed from its base material, so the suffixes do not stack.
            string materialName_Base = constructionLayer.Name;
            string materialName_Stripped = Regex.Replace(materialName_Base ?? string.Empty, @"_\d+(\.\d+)?m$", string.Empty, RegexOptions.CultureInvariant);
            if (materialName_Stripped != materialName_Base && materialLibrary.GetMaterial(materialName_Stripped) != null)
            {
                materialName_Base = materialName_Stripped;
            }

            string materialName = string.Format(CultureInfo.InvariantCulture, "{0}_{1}m", materialName_Base, thickness);

            bool materialAdded = false;
            IMaterial material = materialLibrary.GetMaterial(materialName);
            if (material == null)
            {
                Material material_Source = materialLibrary.GetMaterial(constructionLayer.Name) as Material;
                if (material_Source == null)
                {
                    result = new SetUValueResult(string.Format("Material {0} is not in the Material Library.", constructionLayer.Name));
                    return null;
                }

                Material material_New = Core.Create.Material(material_Source, materialName, materialName, material_Source.Description);
                material_New.SetValue(Core.MaterialParameter.DefaultThickness, thickness);
                materialLibrary.Add(material_New);
                material = material_New;
                materialAdded = true;
            }

            constructionLayers[request.LayerIndex] = new ConstructionLayer(material.Name, thickness);

            Construction construction;
            if (mode == UValueApplyMode.ModifyInPlace)
            {
                construction = new Construction(source, constructionLayers);
            }
            else
            {
                string name = request.NewConstructionName?.Trim();
                if (string.IsNullOrWhiteSpace(name) || constructions.Any(x => x != null && string.Equals(x.Name?.Trim(), name, StringComparison.OrdinalIgnoreCase)))
                {
                    name = Query.UValueConstructionName(source.Name, request.CalculatedThermalTransmittance, constructions.Select(x => x?.Name));
                }

                construction = new Construction(new Construction(Guid.NewGuid(), source, name), constructionLayers);
            }

            if (construction.TryGetValue(ConstructionParameter.DefaultThickness, out double _))
            {
                construction.SetValue(ConstructionParameter.DefaultThickness, construction.GetThickness());
            }

            List<Guid> panelGuids = new List<Guid>();
            foreach (Panel panel in panels)
            {
                Panel panel_New = Analytical.Create.Panel(panel, construction);
                if (panel_New == null)
                {
                    continue;
                }

                adjacencyCluster.AddObject(panel_New);
                panelGuids.Add(panel_New.Guid);
            }

            // A construction exists in the model through its panels or as a stored object (GetConstructions lists
            // both, without merging). Store the changed construction when no panel carries it ("don't assign") or
            // when it was stored already (in place), and store the source when its last panel moved away: no
            // silent deletes, and no second copy of a construction its panels already carry.
            StoreConstruction(adjacencyCluster, construction);
            if (mode == UValueApplyMode.NewConstruction)
            {
                StoreConstruction(adjacencyCluster, source);
            }

            AnalyticalModel analyticalModel_New = new AnalyticalModel(analyticalModel, adjacencyCluster, materialLibrary, analyticalModel.ProfileLibrary);

            // Legacy post-steps, limited to the changed construction: UpdateConstructions matches by name, and the
            // new name is unique (in-place with a shared name is refused above), so no other construction is touched.
            ConstructionManager constructionManager = new ConstructionManager(null, new Construction[] { construction }, materialLibrary);
            analyticalModel_New = Analytical.Query.UpdateConstructions(analyticalModel_New, constructionManager) ?? analyticalModel_New;
            analyticalModel_New = Analytical.Query.UpdateApertureConstructions(analyticalModel_New, constructionManager) ?? analyticalModel_New;

            result = new SetUValueResult(request, source, construction, constructionLayer.Name, material.Name, materialAdded, oldThickness, thickness, panelGuids);
            return analyticalModel_New;
        }

        private static void StoreConstruction(AdjacencyCluster adjacencyCluster, Construction construction)
        {
            bool stored = adjacencyCluster.GetObject<Construction>(construction.Guid) != null;
            bool carried = (adjacencyCluster.GetPanels(construction)?.Count ?? 0) != 0;
            if (stored || !carried)
            {
                adjacencyCluster.AddObject(construction);
            }
        }
    }
}
