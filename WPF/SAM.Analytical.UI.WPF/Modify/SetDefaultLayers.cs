// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using SAM.Core.Windows.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        public static void SetDefaultLayers(this UIAnalyticalModel uIAnalyticalModel, IWin32Window? owner = null)
        {
            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if (analyticalModel == null)
            {
                return;
            }

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;

            List<Tuple<string, string, IJSAMObject>> tuples_All = [];

            List<ApertureConstruction> apertureConstructions = adjacencyCluster.GetApertureConstructions();
            if(apertureConstructions != null)
            {
                foreach(ApertureConstruction apertureConstruction in apertureConstructions)
                {
                    tuples_All.Add(new Tuple<string, string, IJSAMObject>(typeof(ApertureConstruction).Name, apertureConstruction.Name, apertureConstruction));
                }
            }

            List<Construction> constructions = adjacencyCluster.GetConstructions();
            if (apertureConstructions != null)
            {
                foreach (Construction construction in constructions)
                {
                    tuples_All.Add(new Tuple<string, string, IJSAMObject>(typeof(Construction).Name, construction.Name, construction));
                }
            }

            HashSet<string> groups = new HashSet<string>();
            tuples_All.ForEach(x => groups.Add(x.Item1));

            List<IJSAMObject>? jSAMObjects;
            using (TreeViewForm<Tuple<string, string, IJSAMObject>> treeViewForm = new TreeViewForm<Tuple<string, string, IJSAMObject>>("Select Construction", tuples_All, (Tuple<string, string, IJSAMObject> x) => x.Item2, (Tuple<string, string, IJSAMObject> x) => x.Item1))
            {
                if (groups.Count < 2)
                {
                    treeViewForm.ExpandAll();
                }

                if (treeViewForm.ShowDialog(owner) != DialogResult.OK)
                {
                    return;
                }

                jSAMObjects = treeViewForm?.SelectedItems?.ConvertAll(x => x.Item3);
            }

            if(jSAMObjects is null)
            {
                return;
            }

            HashSet<Guid> guids_Aperture = [];
            HashSet<Guid> guids_Panel = [];
            foreach(IJSAMObject jSAMObject in jSAMObjects)
            {
                if(jSAMObject is ApertureConstruction apertureConstruction)
                {
                    List<Aperture> apertures = adjacencyCluster.GetApertures(apertureConstruction);
                    if(apertures is not null)
                    {
                        foreach(Aperture aperture in apertures)
                        {
                            guids_Aperture.Add(aperture.Guid);
                        }
                    }
                }

                if(jSAMObject is Construction construction)
                {
                    List<Panel> panels = adjacencyCluster.GetPanels(construction);
                    if (panels is not null)
                    {
                        foreach (Panel panel in panels)
                        {
                            guids_Panel.Add(panel.Guid);
                        }
                    }
                }
            }

            List<SAMObject> sAMObjects = [];
            if(guids_Aperture != null && guids_Aperture.Count != 0)
            {
                IEnumerable<Aperture> apertures = Analytical.Modify.SetDefaultApertureConstructionLayers(adjacencyCluster, guids_Aperture);
                apertures.ToList().ForEach(sAMObjects.Add);
            }

            if (guids_Panel != null && guids_Panel.Count != 0)
            {
                IEnumerable<Panel> panels = Analytical.Modify.SetDefaultConstructionLayerByPanelType(adjacencyCluster, out List<Panel> panels_Issues, guids_Panel);
                panels.ToList().ForEach(sAMObjects.Add);
            }

            uIAnalyticalModel?.SetJSAMObject(new AnalyticalModel(analyticalModel, adjacencyCluster), new AnalyticalModelModification(sAMObjects));
        }
    }
}