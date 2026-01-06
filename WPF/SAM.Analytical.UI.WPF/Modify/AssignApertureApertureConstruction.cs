// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        public static void AssignApertureApertureConstruction(this UIAnalyticalModel uIAnalyticalModel, IEnumerable<Aperture> apertures)
        {
            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if (analyticalModel == null || apertures == null || apertures.Count() == 0)
            {
                return;
            }

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;

            List<ApertureConstruction> apertureConstructions = adjacencyCluster?.GetApertureConstructions();
            if(apertureConstructions == null || apertureConstructions.Count == 0)
            {
                MessageBox.Show("ApertureConstructions missing.");
                return;
            }

            HashSet<string> names = new HashSet<string>();
            foreach(Aperture aperture in apertures)
            {
                names.Add(aperture?.ApertureConstruction?.Name);
            }

            ApertureConstruction apertureConstruction = null;
            using (Core.Windows.Forms.SearchForm<ApertureConstruction> searchForm = new Core.Windows.Forms.SearchForm<ApertureConstruction>("Select ApertureConstruction", apertureConstructions, (ApertureConstruction x) => x.Name, false))
            {
                if(names != null && names.Count == 1)
                {
                    searchForm.SearchText = names.First();
                }

                searchForm.SelectionMode = System.Windows.Forms.SelectionMode.One;
                if(searchForm.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                {
                    return;
                }

                apertureConstruction = searchForm.SelectedItems?.FirstOrDefault();
            }

            if(apertureConstruction == null)
            {
                return;
            }

            List<SAMObject> sAMObjects = new List<SAMObject>();
            foreach(Aperture aperture in apertures)
            {
                Panel panel = adjacencyCluster.GetPanel(aperture);
                if(panel == null)
                {
                    continue;
                }

                panel = Analytical.Create.Panel(panel);

                Aperture aperture_New = new Aperture(aperture, apertureConstruction);

                panel.RemoveAperture(aperture.Guid);

                panel.AddAperture(aperture_New);

                adjacencyCluster.AddObject(panel);
                if(sAMObjects.Find(x => x.Guid == panel.Guid) == null)
                {
                    sAMObjects.Add(panel);
                }
            }

            uIAnalyticalModel.SetJSAMObject(new AnalyticalModel(analyticalModel, adjacencyCluster, analyticalModel.MaterialLibrary, analyticalModel.ProfileLibrary), new AnalyticalModelModification(sAMObjects));
        }
    }
}
