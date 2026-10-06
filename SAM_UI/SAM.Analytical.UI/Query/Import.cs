// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace SAM.Analytical.UI
{
    // Inside this namespace the bare name "Window" resolves to SAM.Analytical.Window (a building
    // element), so alias it (at namespace scope) to the WPF Window used for dialog ownership.
    using Window = System.Windows.Window;

    public static partial class Query
    {
        /// <summary>
        /// WPF replacement for SAM.Analytical.Windows.Query.Import: opens a JSON file and extracts
        /// the requested IJSAMObjects, optionally letting the user pick them in a tree. Returns the
        /// selected objects and outputs every parsed object via <paramref name="jSAMObjects"/>.
        /// </summary>
        public static List<T> Import<T>(out List<IJSAMObject> jSAMObjects, Func<T, bool> func = null, ImportOptions importOptions = null, Window owner = null) where T : IJSAMObject
        {
            jSAMObjects = null;

            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "json files (*.json)|*.json|All files (*.*)|*.*",
                FilterIndex = 2,
                RestoreDirectory = true
            };

            string directory = Analytical.Query.ResourcesDirectory();
            if (System.IO.Directory.Exists(directory))
            {
                openFileDialog.InitialDirectory = directory;
            }

            if (openFileDialog.ShowDialog(owner) != true)
            {
                return null;
            }

            return Import(openFileDialog.FileName, out jSAMObjects, func, importOptions, owner);
        }

        public static List<T> Import<T>(string path, out List<IJSAMObject> jSAMObjects, Func<T, bool> func = null, ImportOptions importOptions = null, Window owner = null) where T : IJSAMObject
        {
            jSAMObjects = null;

            if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path))
            {
                return null;
            }

            if (importOptions == null)
            {
                importOptions = new ImportOptions();
            }

            List<IJSAMObject> jSAMObjects_Open;

            try
            {
                jSAMObjects_Open = Core.Convert.ToSAM<IJSAMObject>(path);
            }
            catch
            {
                if (!importOptions.SuppressMessages)
                {
                    MessageBox.Show("Cannot open file specified");
                }
                return null;
            }

            if (jSAMObjects_Open == null || jSAMObjects_Open.Count == 0)
            {
                if (!importOptions.SuppressMessages)
                {
                    MessageBox.Show("No objects to import");
                }

                return null;
            }

            List<Tuple<string, string, T>> tuples_All = new List<Tuple<string, string, T>>();
            jSAMObjects = new List<IJSAMObject>();
            foreach (IJSAMObject jSAMObject in jSAMObjects_Open)
            {
                if (jSAMObject == null)
                {
                    continue;
                }

                AdjacencyCluster adjacencyCluster = null;

                if (jSAMObject is AdjacencyCluster)
                {
                    adjacencyCluster = (AdjacencyCluster)jSAMObject;
                }
                else if (jSAMObject is AnalyticalModel)
                {
                    AnalyticalModel analyticalModel = (AnalyticalModel)jSAMObject;

                    List<IMaterial> materials = analyticalModel.MaterialLibrary?.GetMaterials();
                    if (materials != null)
                    {
                        foreach (IMaterial material in materials)
                        {
                            if (material is T)
                            {
                                tuples_All.Add(new Tuple<string, string, T>(typeof(Material).Name, material.Name, (T)material));
                            }

                            jSAMObjects.Add(material);
                        }
                    }

                    List<Profile> profiles_Temp = analyticalModel.ProfileLibrary?.GetProfiles();
                    if (profiles_Temp != null)
                    {
                        foreach (Profile profile in profiles_Temp)
                        {
                            if (profile is T)
                            {
                                tuples_All.Add(new Tuple<string, string, T>(typeof(Profile).Name, profile.Name, (T)(object)profile));
                            }

                            jSAMObjects.Add(profile);
                        }
                    }

                    adjacencyCluster = analyticalModel.AdjacencyCluster;
                }

                if (adjacencyCluster != null)
                {
                    List<Construction> constructions_Temp = adjacencyCluster.GetConstructions();
                    if (constructions_Temp != null)
                    {
                        foreach (Construction construction in constructions_Temp)
                        {
                            if (construction is T)
                            {
                                tuples_All.Add(new Tuple<string, string, T>(typeof(Construction).Name, construction.Name, (T)(object)construction));
                            }

                            jSAMObjects.Add(construction);
                        }
                    }

                    List<ApertureConstruction> apertureConstructions_Temp = adjacencyCluster.GetApertureConstructions();
                    if (apertureConstructions_Temp != null)
                    {
                        foreach (ApertureConstruction apertureConstruction in apertureConstructions_Temp)
                        {
                            if (apertureConstruction is T)
                            {
                                tuples_All.Add(new Tuple<string, string, T>(typeof(ApertureConstruction).Name, apertureConstruction.Name, (T)(object)apertureConstruction));
                            }

                            jSAMObjects.Add(apertureConstruction);
                        }
                    }

                    IEnumerable<InternalCondition> internalConditions = adjacencyCluster.GetInternalConditions(false, true);
                    if (internalConditions != null)
                    {
                        foreach (InternalCondition internalCondition in internalConditions)
                        {
                            if (internalCondition is T)
                            {
                                tuples_All.Add(new Tuple<string, string, T>(typeof(InternalCondition).Name, internalCondition.Name, (T)(object)internalCondition));
                            }

                            jSAMObjects.Add(internalCondition);
                        }
                    }

                    List<MechanicalSystemType> mechanicalSystemTypes = adjacencyCluster.GetMechanicalSystemTypes<MechanicalSystemType>();
                    if (mechanicalSystemTypes != null)
                    {
                        foreach (MechanicalSystemType mechanicalSystemType in mechanicalSystemTypes)
                        {
                            if (mechanicalSystemType is T)
                            {
                                tuples_All.Add(new Tuple<string, string, T>(typeof(MechanicalSystemType).Name, mechanicalSystemType.Name, (T)(object)mechanicalSystemType));
                            }

                            jSAMObjects.Add(mechanicalSystemType);
                        }
                    }
                }

                if (jSAMObject is MaterialLibrary)
                {
                    List<IMaterial> materials = ((MaterialLibrary)jSAMObject).GetMaterials();
                    if (materials != null)
                    {
                        foreach (IMaterial material in materials)
                        {
                            if (material is T)
                            {
                                tuples_All.Add(new Tuple<string, string, T>(typeof(Material).Name, material.Name, (T)material));
                            }

                            jSAMObjects.Add(material);
                        }
                    }
                }
                else if (jSAMObject is ConstructionLibrary)
                {
                    List<Construction> constructions_Temp = ((ConstructionLibrary)jSAMObject).GetConstructions();
                    if (constructions_Temp != null)
                    {
                        foreach (Construction construction in constructions_Temp)
                        {
                            if (construction is T)
                            {
                                tuples_All.Add(new Tuple<string, string, T>(typeof(Construction).Name, construction.Name, (T)(object)construction));
                            }

                            jSAMObjects.Add(construction);
                        }
                    }
                }
                else if (jSAMObject is ProfileLibrary)
                {
                    List<Profile> profiles_Temp = ((ProfileLibrary)jSAMObject).GetProfiles();
                    if (profiles_Temp != null)
                    {
                        foreach (Profile profile in profiles_Temp)
                        {
                            if (profile is T)
                            {
                                tuples_All.Add(new Tuple<string, string, T>(typeof(Profile).Name, profile.Name, (T)(object)profile));
                            }

                            jSAMObjects.Add(profile);
                        }
                    }
                }
                else if (jSAMObject is SystemTypeLibrary)
                {
                    List<MechanicalSystemType> mechanicalSystemTypes_Temp = ((SystemTypeLibrary)jSAMObject).GetSystemTypes<MechanicalSystemType>();
                    if (mechanicalSystemTypes_Temp != null)
                    {
                        foreach (MechanicalSystemType mechanicalSystemType in mechanicalSystemTypes_Temp)
                        {
                            if (mechanicalSystemType is T)
                            {
                                tuples_All.Add(new Tuple<string, string, T>(typeof(MechanicalSystemType).Name, mechanicalSystemType.Name, (T)(object)mechanicalSystemType));
                            }

                            jSAMObjects.Add(mechanicalSystemType);
                        }
                    }
                }
                else if (jSAMObject is ApertureConstructionLibrary)
                {
                    List<ApertureConstruction> apertureConstructions_Temp = ((ApertureConstructionLibrary)jSAMObject).GetApertureConstructions();
                    if (apertureConstructions_Temp != null)
                    {
                        foreach (ApertureConstruction apertureConstruction in apertureConstructions_Temp)
                        {
                            if (apertureConstruction is T)
                            {
                                tuples_All.Add(new Tuple<string, string, T>(typeof(ApertureConstruction).Name, apertureConstruction.Name, (T)(object)apertureConstruction));
                            }

                            jSAMObjects.Add(apertureConstruction);
                        }
                    }
                }
                else if (jSAMObject is InternalConditionLibrary)
                {
                    List<InternalCondition> internalConditions_Temp = ((InternalConditionLibrary)jSAMObject).GetInternalConditions();
                    if (internalConditions_Temp != null)
                    {
                        foreach (InternalCondition internalCondition in internalConditions_Temp)
                        {
                            if (internalCondition is T)
                            {
                                tuples_All.Add(new Tuple<string, string, T>(typeof(InternalCondition).Name, internalCondition.Name, (T)(object)internalCondition));
                            }

                            jSAMObjects.Add(internalCondition);
                        }
                    }
                }
                else if (jSAMObject is ConstructionManager)
                {
                    ConstructionManager constructionManager = (ConstructionManager)jSAMObject;

                    List<Construction> constructions_Temp = constructionManager.Constructions;
                    if (constructions_Temp != null)
                    {
                        foreach (Construction construction in constructions_Temp)
                        {
                            if (construction is T)
                            {
                                tuples_All.Add(new Tuple<string, string, T>(typeof(Construction).Name, construction.Name, (T)(object)construction));
                            }

                            jSAMObjects.Add(construction);
                        }
                    }

                    List<ApertureConstruction> apertureConstructions_Temp = constructionManager.ApertureConstructions;
                    if (apertureConstructions_Temp != null)
                    {
                        foreach (ApertureConstruction apertureConstruction in apertureConstructions_Temp)
                        {
                            if (apertureConstruction is T)
                            {
                                tuples_All.Add(new Tuple<string, string, T>(typeof(ApertureConstruction).Name, apertureConstruction.Name, (T)(object)apertureConstruction));
                            }

                            jSAMObjects.Add(apertureConstruction);
                        }
                    }

                    List<IMaterial> materials_Temp = constructionManager.Materials;
                    if (materials_Temp != null)
                    {
                        foreach (IMaterial material in materials_Temp)
                        {
                            if (material is T)
                            {
                                tuples_All.Add(new Tuple<string, string, T>(typeof(Material).Name, material.Name, (T)(object)material));
                            }

                            jSAMObjects.Add(material);
                        }
                    }
                }
                else if (jSAMObject is T)
                {
                    jSAMObjects.Add(jSAMObject);

                    if (jSAMObject is IMaterial)
                    {
                        tuples_All.Add(new Tuple<string, string, T>(typeof(Material).Name, ((IMaterial)jSAMObject).Name, (T)jSAMObject));
                    }
                    else if (jSAMObject is Construction)
                    {
                        tuples_All.Add(new Tuple<string, string, T>(typeof(Construction).Name, ((Construction)jSAMObject).Name, (T)jSAMObject));
                    }
                    else if (jSAMObject is ApertureConstruction)
                    {
                        tuples_All.Add(new Tuple<string, string, T>(typeof(ApertureConstruction).Name, ((ApertureConstruction)jSAMObject).Name, (T)jSAMObject));
                    }
                    else if (jSAMObject is Profile)
                    {
                        tuples_All.Add(new Tuple<string, string, T>(typeof(Profile).Name, ((Profile)jSAMObject).Name, (T)jSAMObject));
                    }
                    else if (jSAMObject is InternalCondition)
                    {
                        tuples_All.Add(new Tuple<string, string, T>(typeof(InternalCondition).Name, ((InternalCondition)jSAMObject).Name, (T)jSAMObject));
                    }
                    else if (jSAMObject is MechanicalSystemType)
                    {
                        tuples_All.Add(new Tuple<string, string, T>(jSAMObject.GetType().Name, ((MechanicalSystemType)jSAMObject).Name, (T)jSAMObject));
                    }
                    else if (jSAMObject is SAMObject && !(jSAMObject is AnalyticalModel) && !(jSAMObject is AdjacencyCluster))
                    {
                        tuples_All.Add(new Tuple<string, string, T>(jSAMObject.GetType().Name, ((SAMObject)jSAMObject).Name, (T)jSAMObject));
                    }
                }
            }

            if (func != null && tuples_All != null)
            {
                for (int i = tuples_All.Count - 1; i >= 0; i--)
                {
                    if (!func.Invoke(tuples_All[i].Item3))
                    {
                        tuples_All.RemoveAt(i);
                    }
                }
            }

            if (tuples_All == null || tuples_All.Count == 0)
            {
                if (!importOptions.SuppressMessages)
                {
                    MessageBox.Show("No objects to import");
                }
                return null;
            }

            List<Tuple<string, string, T>> tuples_Selected = tuples_All;
            if (importOptions.UserSelection)
            {
                SAM.Core.UI.WPF.MultipleSelectionTreeViewWindow treeViewWindow = new SAM.Core.UI.WPF.MultipleSelectionTreeViewWindow { Title = "Select Objects" };
                treeViewWindow.GettingText += (object sender, SAM.Core.UI.WPF.GettingTextEventArgs e) =>
                {
                    e.Text = (e?.Object as Tuple<string, string, T>)?.Item2;
                };
                treeViewWindow.GettingCategory += (object sender, SAM.Core.UI.WPF.GettingCategoryEventArgs e) =>
                {
                    string category = (e?.Object as Tuple<string, string, T>)?.Item1;
                    e.Category = string.IsNullOrEmpty(category) ? null : new Category(category);
                };
                treeViewWindow.SetObjects(tuples_All);

                SAM.Core.UI.WPF.Modify.SetOwner(treeViewWindow, owner);

                if (treeViewWindow.ShowDialog() != true)
                {
                    return null;
                }

                tuples_Selected = treeViewWindow.GetObjects<Tuple<string, string, T>>();
            }

            if (tuples_Selected == null || tuples_Selected.Count == 0)
            {
                return null;
            }

            return tuples_Selected.ConvertAll(x => x.Item3);
        }

        /// <summary>
        /// Opens a JSON file and returns a ConstructionManager built from the user-selected objects.
        /// </summary>
        public static ConstructionManager ImportConstructionManager(Func<IJSAMObject, bool> func, ImportOptions importOptions = null, Window owner = null)
        {
            List<IJSAMObject> selected = Import<IJSAMObject>(out List<IJSAMObject> jSAMObjects_All, func, importOptions, owner);

            return ToConstructionManager(selected, jSAMObjects_All);
        }

        public static ConstructionManager ImportConstructionManager(string path, Func<IJSAMObject, bool> func, ImportOptions importOptions = null, Window owner = null)
        {
            List<IJSAMObject> selected = Import<IJSAMObject>(path, out List<IJSAMObject> jSAMObjects_All, func, importOptions, owner);

            return ToConstructionManager(selected, jSAMObjects_All);
        }

        // Builds a ConstructionManager from the user-selected objects, pulling in every material
        // parsed from the file so construction layers resolve. This replaces the WinForms flow that
        // merged into a temporary AnalyticalModel and read back its ConstructionManager - the old
        // AdjacencyCluster.UpdateConstructions overload that relied on no longer exists in SAM.
        private static ConstructionManager ToConstructionManager(List<IJSAMObject> selected, List<IJSAMObject> jSAMObjects_All)
        {
            if (selected == null || selected.Count == 0)
            {
                return null;
            }

            ConstructionManager constructionManager = new ConstructionManager();

            if (jSAMObjects_All != null)
            {
                foreach (IJSAMObject jSAMObject in jSAMObjects_All)
                {
                    if (jSAMObject is IMaterial material)
                    {
                        constructionManager.Add(material);
                    }
                }
            }

            foreach (IJSAMObject jSAMObject in selected)
            {
                if (jSAMObject is Construction construction)
                {
                    constructionManager.Add(construction);
                }
                else if (jSAMObject is ApertureConstruction apertureConstruction)
                {
                    constructionManager.Add(apertureConstruction);
                }
                else if (jSAMObject is IMaterial material)
                {
                    constructionManager.Add(material);
                }
            }

            return constructionManager;
        }

        /// <summary>
        /// WPF replacement for the merging SAM.Analytical.Windows.Query.Import(AnalyticalModel, ...)
        /// overloads: opens a JSON file, lets the user pick objects, and merges them into a COPY of
        /// <paramref name="analyticalModel"/> (materials/profiles into the libraries; constructions,
        /// aperture constructions, internal conditions and mechanical system types into the
        /// AdjacencyCluster). Returns null when nothing was imported. Imports everything (no type filter).
        /// </summary>
        public static AnalyticalModel Import(this AnalyticalModel analyticalModel, ImportOptions importOptions = null, Window owner = null)
        {
            Func<IJSAMObject, bool> func = null;

            return Import(analyticalModel, func, importOptions, owner);
        }

        /// <summary>Merging import restricted to objects of type <typeparamref name="T"/>.</summary>
        public static AnalyticalModel Import<T>(this AnalyticalModel analyticalModel, ImportOptions importOptions = null, Window owner = null) where T : IJSAMObject
        {
            return Import(analyticalModel, (IJSAMObject x) => x is T, importOptions, owner);
        }

        /// <summary>Merging import from a known file path, filtered by <paramref name="func"/>.</summary>
        public static AnalyticalModel Import<T>(this AnalyticalModel analyticalModel, string path, Func<T, bool> func = null, ImportOptions importOptions = null, Window owner = null) where T : IJSAMObject
        {
            List<T> jSAMObjects = Import(path, out List<IJSAMObject> jSAMObjects_All, func, importOptions, owner);

            return Merge(analyticalModel, jSAMObjects, jSAMObjects_All, importOptions, owner);
        }

        /// <summary>Merging import (file chosen via dialog), filtered by <paramref name="func"/>.</summary>
        public static AnalyticalModel Import<T>(this AnalyticalModel analyticalModel, Func<T, bool> func = null, ImportOptions importOptions = null, Window owner = null) where T : IJSAMObject
        {
            List<T> jSAMObjects = Import(out List<IJSAMObject> jSAMObjects_All, func, importOptions, owner);

            return Merge(analyticalModel, jSAMObjects, jSAMObjects_All, importOptions, owner);
        }

        // Merges the user-selected objects into a copy of analyticalModel. Mirrors the legacy
        // SAM.Analytical.Windows merge, except the two batch calls it relied on
        // (AdjacencyCluster.UpdateConstructions/UpdateApertureConstructions taking a List) are gone
        // from SAM - imported constructions/aperture constructions are added to the cluster directly
        // via AddObject, which is how modern SAM stores them.
        private static AnalyticalModel Merge<T>(AnalyticalModel analyticalModel, IEnumerable<T> jSAMObjects, IEnumerable<IJSAMObject> jSAMObjects_All, ImportOptions importOptions = null, Window owner = null) where T : IJSAMObject
        {
            if (analyticalModel == null || jSAMObjects == null)
            {
                return null;
            }

            if (importOptions == null)
            {
                importOptions = new ImportOptions();
            }

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;
            if (adjacencyCluster == null)
            {
                adjacencyCluster = new AdjacencyCluster();
            }

            List<Construction> constructions = new List<Construction>();
            List<ApertureConstruction> apertureConstructions = new List<ApertureConstruction>();
            List<InternalCondition> internalConditions = new List<InternalCondition>();

            foreach (T jSAMObject in jSAMObjects)
            {
                if (jSAMObject == null)
                {
                    continue;
                }

                if (jSAMObject is IMaterial)
                {
                    analyticalModel.AddMaterial((IMaterial)jSAMObject);
                }
                else if (jSAMObject is Profile)
                {
                    analyticalModel.AddProfile((Profile)(object)jSAMObject);
                }
                else if (jSAMObject is Construction)
                {
                    constructions.Add((Construction)(object)jSAMObject);
                }
                else if (jSAMObject is ApertureConstruction)
                {
                    apertureConstructions.Add((ApertureConstruction)(object)jSAMObject);
                }
                else if (jSAMObject is InternalCondition)
                {
                    internalConditions.Add((InternalCondition)(object)jSAMObject);
                }
                else if (jSAMObject is MechanicalSystemType)
                {
                    adjacencyCluster.AddObject((MechanicalSystemType)(object)jSAMObject);
                }
            }

            foreach (Construction construction in constructions)
            {
                adjacencyCluster.AddObject(construction);
            }

            foreach (ApertureConstruction apertureConstruction in apertureConstructions)
            {
                adjacencyCluster.AddObject(apertureConstruction);
            }

            // Pull in any materials referenced by imported (aperture) construction layers but not yet
            // present in the model's MaterialLibrary.
            if (constructions.Count != 0 || apertureConstructions.Count != 0)
            {
                HashSet<string> names = new HashSet<string>();

                foreach (Construction construction in constructions)
                {
                    List<ConstructionLayer> constructionLayers = construction?.ConstructionLayers;
                    if (constructionLayers != null)
                    {
                        foreach (ConstructionLayer constructionLayer in constructionLayers)
                        {
                            names.Add(constructionLayer.Name);
                        }
                    }
                }

                foreach (ApertureConstruction apertureConstruction in apertureConstructions)
                {
                    List<ConstructionLayer> constructionLayers = apertureConstruction?.PaneConstructionLayers;
                    if (constructionLayers != null)
                    {
                        foreach (ConstructionLayer constructionLayer in constructionLayers)
                        {
                            names.Add(constructionLayer.Name);
                        }
                    }

                    constructionLayers = apertureConstruction?.FrameConstructionLayers;
                    if (constructionLayers != null)
                    {
                        foreach (ConstructionLayer constructionLayer in constructionLayers)
                        {
                            names.Add(constructionLayer.Name);
                        }
                    }
                }

                List<IMaterial> materials = jSAMObjects_All?.ToList().FindAll(x => x is IMaterial).ConvertAll(x => (IMaterial)x);
                if (materials != null && materials.Count != 0)
                {
                    MaterialLibrary materialLibrary = analyticalModel.MaterialLibrary;

                    HashSet<string> names_Missing = new HashSet<string>();
                    foreach (string name in names)
                    {
                        if (materialLibrary?.GetMaterial(name) == null)
                        {
                            names_Missing.Add(name);
                        }
                    }

                    if (names_Missing.Count != 0)
                    {
                        bool import = importOptions.SuppressMessages || Confirm("Try to import missing materials?", "Materials", owner);
                        if (import)
                        {
                            foreach (string name in names_Missing)
                            {
                                IMaterial material = materials.Find(x => x.Name == name);
                                if (material != null)
                                {
                                    analyticalModel.AddMaterial(material);
                                }
                            }
                        }
                    }
                }
            }

            if (internalConditions.Count != 0)
            {
                ProfileLibrary profileLibrary = analyticalModel.ProfileLibrary;

                List<Profile> profiles = jSAMObjects_All?.ToList().FindAll(x => x is Profile).ConvertAll(x => (Profile)x);

                Dictionary<ProfileType, HashSet<string>> dictionary_Profile = new Dictionary<ProfileType, HashSet<string>>();
                foreach (InternalCondition internalCondition in internalConditions)
                {
                    adjacencyCluster.AddObject(internalCondition);

                    if (profiles != null && profiles.Count != 0)
                    {
                        IEnumerable<ProfileType> profileTypes = internalCondition.GetProfileTypes();
                        if (profileTypes != null)
                        {
                            foreach (ProfileType profileType in profileTypes)
                            {
                                string name = internalCondition.GetProfileName(profileType);
                                if (string.IsNullOrEmpty(name))
                                {
                                    continue;
                                }

                                Profile profile = internalCondition.GetProfile(profileType, profileLibrary);
                                if (profile != null)
                                {
                                    continue;
                                }

                                if (!dictionary_Profile.TryGetValue(profileType, out HashSet<string> names))
                                {
                                    names = new HashSet<string>();
                                    dictionary_Profile[profileType] = names;
                                }

                                names.Add(name);
                            }
                        }
                    }
                }

                if (dictionary_Profile.Count != 0)
                {
                    bool import = importOptions.SuppressMessages || Confirm("Try to import missing profiles?", "Profiles", owner);
                    if (import)
                    {
                        foreach (KeyValuePair<ProfileType, HashSet<string>> keyValuePair in dictionary_Profile)
                        {
                            foreach (string name in keyValuePair.Value)
                            {
                                Profile profile = Analytical.Query.Profile(profiles, name, keyValuePair.Key, true);
                                if (profile != null)
                                {
                                    analyticalModel.AddProfile(profile);
                                }
                            }
                        }
                    }
                }
            }

            analyticalModel = new AnalyticalModel(analyticalModel);

            return new AnalyticalModel(analyticalModel, adjacencyCluster);
        }

        private static bool Confirm(string text, string caption, Window owner)
        {
            MessageBoxResult result = owner == null
                ? MessageBox.Show(text, caption, MessageBoxButton.YesNo)
                : MessageBox.Show(owner, text, caption, MessageBoxButton.YesNo);

            return result == MessageBoxResult.Yes;
        }
    }
}
