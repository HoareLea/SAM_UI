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

            // The generated construction is made by the one pure query the Thermal Performance panel previews and saves from as well. Its input
            // errors (layer, thickness, a shared name when keeping the name) come before the scope's, as they always did.
            MaterialLibrary materialLibrary = analyticalModel.MaterialLibrary ?? new MaterialLibrary("Default MaterialLibrary");
            ProposedConstructionResult proposed = Query.ProposedConstruction(source, materialLibrary, request.LayerIndex, request.Thickness, request.Mode, request.NewConstructionName, request.CalculatedThermalTransmittance, constructions);
            if (!proposed.Succeeded && proposed.ErrorKind == ProposedConstructionErrorKind.Input)
            {
                result = new SetUValueResult(proposed.Error);
                return null;
            }

            UValueApplyMode mode = request.Mode;
            ThermalApplyScope scope = mode == UValueApplyMode.ModifyInPlace ? ThermalApplyScope.AllUsing : request.Scope;

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

            // A material the layer needs that the Material Library lacks stops the change only after the scope was checked.
            if (!proposed.Succeeded)
            {
                result = new SetUValueResult(proposed.Error);
                return null;
            }

            // The adjusted material enters the model's Material Library (the legacy apply leaves it out, and ModelCheck then reports a missing material).
            if (proposed.MaterialAdded)
            {
                materialLibrary.Add(proposed.Material);
            }

            Construction construction = proposed.Construction;

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

            result = new SetUValueResult(request, source, construction, proposed.SourceMaterialName, proposed.Material.Name, proposed.MaterialAdded, proposed.OldThickness, proposed.NewThickness, panelGuids);
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
