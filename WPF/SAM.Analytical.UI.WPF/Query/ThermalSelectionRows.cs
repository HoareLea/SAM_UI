// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Query
    {
        /// <summary>
        /// B0 docking spike (read-only): the selected panels and apertures grouped by kind and construction, each with the
        /// selected count and how many elements of that kind use the construction in the model. Reads the model only; other
        /// selected objects (spaces, shades ...) are ignored. A selected object is looked up in the model by Guid, so the
        /// construction shown is the model's current one.
        /// </summary>
        public static List<ThermalSelectionRow> ThermalSelectionRows(AnalyticalModel analyticalModel, IEnumerable<SAMObject> selected)
        {
            List<ThermalSelectionRow> result = new List<ThermalSelectionRow>();

            AdjacencyCluster adjacencyCluster = analyticalModel?.AdjacencyCluster;
            if (adjacencyCluster == null || selected == null)
            {
                return result;
            }

            Dictionary<Guid, List<Panel>> panels = new Dictionary<Guid, List<Panel>>();
            Dictionary<Guid, List<Aperture>> apertures = new Dictionary<Guid, List<Aperture>>();
            Dictionary<Guid, Construction> constructions = new Dictionary<Guid, Construction>();
            Dictionary<Guid, ApertureConstruction> apertureConstructions = new Dictionary<Guid, ApertureConstruction>();

            foreach (SAMObject sAMObject in selected)
            {
                if (sAMObject is Panel panel)
                {
                    Panel panel_Model = adjacencyCluster.GetObject<Panel>(panel.Guid) ?? panel;
                    Construction construction = panel_Model.Construction;
                    if (construction == null)
                    {
                        continue;
                    }

                    constructions[construction.Guid] = construction;
                    if (!panels.TryGetValue(construction.Guid, out List<Panel> list))
                    {
                        panels[construction.Guid] = list = new List<Panel>();
                    }

                    list.Add(panel_Model);
                }
                else if (sAMObject is Aperture aperture)
                {
                    Aperture aperture_Model = adjacencyCluster.GetAperture(aperture.Guid) ?? aperture;
                    ApertureConstruction apertureConstruction = aperture_Model.ApertureConstruction;
                    if (apertureConstruction == null)
                    {
                        continue;
                    }

                    apertureConstructions[apertureConstruction.Guid] = apertureConstruction;
                    if (!apertures.TryGetValue(apertureConstruction.Guid, out List<Aperture> list))
                    {
                        apertures[apertureConstruction.Guid] = list = new List<Aperture>();
                    }

                    list.Add(aperture_Model);
                }
            }

            foreach (KeyValuePair<Guid, List<Panel>> pair in panels)
            {
                Construction construction = constructions[pair.Key];
                string type = string.Join(", ", pair.Value.Select(x => x.PanelType.ToString()).Distinct().OrderBy(x => x));
                result.Add(new ThermalSelectionRow("Panel", type, construction.Guid, construction.Name, pair.Value.Select(x => x.Guid).Distinct().Count(), adjacencyCluster.GetPanels(construction)?.Count ?? 0));
            }

            foreach (KeyValuePair<Guid, List<Aperture>> pair in apertures)
            {
                ApertureConstruction apertureConstruction = apertureConstructions[pair.Key];
                string type = string.Join(", ", pair.Value.Select(x => x.ApertureType.ToString()).Distinct().OrderBy(x => x));
                result.Add(new ThermalSelectionRow("Aperture", type, apertureConstruction.Guid, apertureConstruction.Name, pair.Value.Select(x => x.Guid).Distinct().Count(), adjacencyCluster.GetApertures(apertureConstruction)?.Count ?? 0));
            }

            return result.OrderBy(x => x.Kind, StringComparer.Ordinal).ThenBy(x => x.ConstructionName, StringComparer.CurrentCultureIgnoreCase).ToList();
        }
    }
}
