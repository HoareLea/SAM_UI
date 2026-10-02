// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

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
        /// Assigns an EXISTING construction (the model's own, or one from a library) to the panels that use the current construction, as
        /// ONE Undo step: every edit is made on one clone of the model and <c>SetJSAMObject</c> is called exactly once (never on failure).
        /// Only the chosen construction and the materials it lacks enter the model; nothing is generated. The stored thermal parameters
        /// are refreshed by the whole-model Tas run, as after <see cref="SetUValue(UIAnalyticalModel, SetUValueRequest)"/>.
        /// </summary>
        public static SetConstructionResult SetConstruction(this UIAnalyticalModel uIAnalyticalModel, SetConstructionRequest request)
        {
            return SetConstruction(uIAnalyticalModel, request, x => Tas.Modify.UpdateThermalParameters(x));
        }

        /// <param name="updateThermalParameters">The Tas thermal-parameter refresh (a whole-model TCD run). Tests pass a stand-in so they need no Tas.</param>
        internal static SetConstructionResult SetConstruction(this UIAnalyticalModel uIAnalyticalModel, SetConstructionRequest request, Action<AnalyticalModel> updateThermalParameters)
        {
            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if (analyticalModel == null)
            {
                return new SetConstructionResult("There is no model to change.");
            }

            analyticalModel = SetConstruction(analyticalModel, request, out SetConstructionResult result);
            if (analyticalModel == null || result == null || !result.Succeeded)
            {
                return result ?? new SetConstructionResult("The construction change could not be applied.");
            }

            updateThermalParameters?.Invoke(analyticalModel);

            uIAnalyticalModel.SetJSAMObject(analyticalModel, new FullModification());

            return result;
        }

        /// <summary>
        /// The model-only part of <see cref="SetConstruction(UIAnalyticalModel, SetConstructionRequest)"/>: returns the changed model (null on
        /// failure, with <paramref name="result"/> saying why). <paramref name="analyticalModel"/> is not modified.
        /// </summary>
        internal static AnalyticalModel SetConstruction(this AnalyticalModel analyticalModel, SetConstructionRequest request, out SetConstructionResult result)
        {
            result = null;

            if (analyticalModel == null || request == null || request.Construction == null)
            {
                result = new SetConstructionResult("There is no model or construction to apply.");
                return null;
            }

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;
            if (adjacencyCluster == null)
            {
                result = new SetConstructionResult("The model has no adjacency cluster.");
                return null;
            }

            List<Construction> constructions = adjacencyCluster.GetConstructions() ?? new List<Construction>();
            Construction source = constructions.Find(x => x != null && x.Guid == request.SourceConstructionGuid);
            if (source == null)
            {
                result = new SetConstructionResult("The current construction is no longer in the model.");
                return null;
            }

            if (source.Guid == request.Construction.Guid)
            {
                result = new SetConstructionResult(string.Format("{0} is the construction the panels use already.", source.Name));
                return null;
            }

            ThermalApplyScope scope = request.Scope;
            List<Panel> panels_Using = adjacencyCluster.GetPanels(source) ?? new List<Panel>();
            List<Panel> panels;
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
                        result = new SetConstructionResult(string.Format("None of the selected panels uses {0}.", source.Name));
                        return null;
                    }

                    break;

                default:
                    result = new SetConstructionResult("An existing construction is always assigned: choose all the panels using the current construction, or the selected ones.");
                    return null;
            }

            if (panels.Count == 0)
            {
                result = new SetConstructionResult(string.Format("No panel uses {0}.", source.Name));
                return null;
            }

            // The materials: only the chosen construction's, and only those the model lacks. A name the model already has for another
            // definition is refused by the candidate (the materials are matched by name everywhere in SAM); the check here is the
            // backstop for a request built by hand.
            MaterialLibrary materialLibrary = analyticalModel.MaterialLibrary ?? new MaterialLibrary("Default MaterialLibrary");
            List<string> materialNamesAdded = new List<string>();
            foreach (IMaterial material in request.MaterialsToAdd ?? Enumerable.Empty<IMaterial>())
            {
                if (material?.Name == null || materialLibrary.GetMaterial(material.Name) != null)
                {
                    continue;
                }

                if (materialLibrary.Add(material))
                {
                    materialNamesAdded.Add(material.Name);
                }
            }

            foreach (ConstructionLayer constructionLayer in request.Construction.ConstructionLayers ?? new List<ConstructionLayer>())
            {
                if (constructionLayer?.Name != null && materialLibrary.GetMaterial(constructionLayer.Name) == null)
                {
                    result = new SetConstructionResult(string.Format("Material {0} of {1} is not in the Material Library.", constructionLayer.Name, request.Construction.Name));
                    return null;
                }
            }

            // The chosen construction: the model's own when it has that Guid; otherwise added under a name no other construction of the
            // model has (the legacy post-step matches constructions by NAME), keeping its Guid so its provenance is stable.
            Construction chosen = constructions.Find(x => x != null && x.Guid == request.Construction.Guid);
            bool added = chosen == null;
            if (added)
            {
                string name = request.Construction.Name;
                string name_Unique = name;
                for (int index = 2; constructions.Any(x => x != null && string.Equals(x.Name?.Trim(), name_Unique?.Trim(), StringComparison.OrdinalIgnoreCase)); index++)
                {
                    name_Unique = string.Format("{0} {1}", name, index);
                }

                chosen = name_Unique == name ? new Construction(request.Construction) : new Construction(request.Construction.Guid, request.Construction, name_Unique);
            }

            List<Guid> panelGuids = new List<Guid>();
            foreach (Panel panel in panels)
            {
                Panel panel_New = Analytical.Create.Panel(panel, chosen);
                if (panel_New == null)
                {
                    continue;
                }

                adjacencyCluster.AddObject(panel_New);
                panelGuids.Add(panel_New.Guid);
            }

            // As in SetUValue: a construction exists in the model through its panels or as a stored object; keep a stored one stored and
            // the source stored when its last panel moved away - no silent deletes, no second copy of a construction its panels carry.
            StoreConstruction(adjacencyCluster, chosen);
            StoreConstruction(adjacencyCluster, source);

            AnalyticalModel analyticalModel_New = new AnalyticalModel(analyticalModel, adjacencyCluster, materialLibrary, analyticalModel.ProfileLibrary);

            ConstructionManager constructionManager = new ConstructionManager(null, new Construction[] { chosen }, materialLibrary);
            analyticalModel_New = Analytical.Query.UpdateConstructions(analyticalModel_New, constructionManager) ?? analyticalModel_New;

            result = new SetConstructionResult(request, source, chosen, added, materialNamesAdded, panelGuids);
            return analyticalModel_New;
        }
    }
}
