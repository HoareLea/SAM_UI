// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using SAM.Core.UI.WPF;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace SAM.Analytical.UI
{
    // In this namespace the bare name "Window" resolves to SAM.Analytical.Window (a building
    // element), so alias it (at namespace scope) to the WPF Window used for dialogs.
    using Window = System.Windows.Window;

    public static partial class Query
    {
        /// <summary>
        /// WPF replacement for SAM.Analytical.Windows.Query.Clean: collects unused profiles/materials/
        /// constructions/aperture constructions/internal conditions, lets the user pick which to
        /// remove, and removes them. The legacy ProgressForms are dropped and the TreeViewForm is the
        /// WPF MultipleSelectionTreeViewWindow.
        /// </summary>
        public static AnalyticalModel Clean(this AnalyticalModel analyticalModel, Window owner = null)
        {
            if (analyticalModel == null)
            {
                return null;
            }

            List<IJSAMObject> jSAMObjects = new List<IJSAMObject>();

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;
            ProfileLibrary profileLibrary = analyticalModel.ProfileLibrary;
            MaterialLibrary materialLibrary = analyticalModel.MaterialLibrary;

            List<Space> spaces = adjacencyCluster.GetSpaces();
            List<InternalCondition> internalConditions = adjacencyCluster?.GetInternalConditions(false, true)?.ToList();
            List<Construction> constructions = adjacencyCluster?.GetConstructions();
            List<ApertureConstruction> apertureConstructions = adjacencyCluster?.GetApertureConstructions();
            List<Panel> panels = adjacencyCluster.GetPanels();

            if (spaces != null)
            {
                foreach (Space space in spaces)
                {
                    InternalCondition internalCondition = space.InternalCondition;
                    if (internalCondition != null)
                    {
                        internalConditions?.RemoveAll(x => x.Name == internalCondition.Name);

                        IEnumerable<Profile> profiles = internalCondition.GetProfiles(profileLibrary);
                        if (profiles != null)
                        {
                            foreach (Profile profile in profiles)
                            {
                                profileLibrary.Remove(profile);
                            }
                        }
                    }
                }
            }

            if (constructions != null)
            {
                foreach (Construction construction in constructions)
                {
                    List<IMaterial> materials = construction.Materials<IMaterial>(materialLibrary)?.ToList();
                    if (materials != null)
                    {
                        foreach (IMaterial material in materials)
                        {
                            materialLibrary.Remove(material);
                        }
                    }
                }
            }

            if (panels != null)
            {
                foreach (Panel panel in panels)
                {
                    Construction construction = panel?.Construction;
                    if (construction != null)
                    {
                        constructions?.RemoveAll(x => x.Guid == construction.Guid);
                    }

                    List<Aperture> apertures = panel?.Apertures;
                    if (apertures != null)
                    {
                        foreach (Aperture aperture in apertures)
                        {
                            ApertureConstruction apertureConstruction = aperture.ApertureConstruction;
                            if (apertureConstruction != null)
                            {
                                apertureConstructions?.RemoveAll(x => x.Guid == apertureConstruction.Guid);

                                IEnumerable<IMaterial> materials = apertureConstruction.Materials(materialLibrary);
                                if (materials != null)
                                {
                                    foreach (IMaterial material in materials)
                                    {
                                        materialLibrary.Remove(material);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            profileLibrary?.GetProfiles()?.ForEach(x => jSAMObjects.Add(x));
            materialLibrary?.GetMaterials()?.ForEach(x => jSAMObjects.Add(x));
            constructions?.ForEach(x => jSAMObjects.Add(x));
            apertureConstructions?.ForEach(x => jSAMObjects.Add(x));
            internalConditions?.ForEach(x => jSAMObjects.Add(x));

            if (jSAMObjects.Count == 0)
            {
                MessageBox.Show("Nothing to be cleaned.");
            }

            MultipleSelectionTreeViewWindow treeViewWindow = new MultipleSelectionTreeViewWindow { Title = "Select Items" };
            treeViewWindow.GettingText += (object sender, GettingTextEventArgs e) => e.Text = (e?.Object as SAMObject)?.Name;
            treeViewWindow.GettingCategory += (object sender, GettingCategoryEventArgs e) =>
            {
                string category = e?.Object?.GetType().Name;
                e.Category = string.IsNullOrEmpty(category) ? null : new Category(category);
            };
            treeViewWindow.SetObjects(jSAMObjects);

            SAM.Core.UI.WPF.Modify.SetOwner(treeViewWindow, owner);

            if (treeViewWindow.ShowDialog() != true)
            {
                return null;
            }

            jSAMObjects = treeViewWindow.GetObjects<IJSAMObject>();
            if (jSAMObjects == null || jSAMObjects.Count == 0)
            {
                return new AnalyticalModel(analyticalModel);
            }

            profileLibrary = analyticalModel.ProfileLibrary;
            materialLibrary = analyticalModel.MaterialLibrary;

            foreach (IJSAMObject jSAMObject in jSAMObjects)
            {
                if (jSAMObject is Profile profile)
                {
                    profileLibrary.Remove(profile);
                }

                if (jSAMObject is Material material)
                {
                    materialLibrary.Remove(material);
                }

                if (jSAMObject is SAMObject sAMObject)
                {
                    adjacencyCluster.RemoveObject(sAMObject.GetType(), sAMObject.Guid);
                }
            }

            return new AnalyticalModel(analyticalModel, adjacencyCluster, materialLibrary, profileLibrary);
        }
    }
}
