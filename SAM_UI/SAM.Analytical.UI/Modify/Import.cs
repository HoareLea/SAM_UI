using SAM.Core;
using SAM.Core.Windows.Forms;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace SAM.Analytical.UI
{
    public static partial class Modify
    {
        public static void Import(this UIAnalyticalModel uIAnalyticalModel, IWin32Window owner = null)
        {
            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if (analyticalModel == null)
            {
                return;
            }

            string path = null;
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                string directory = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SAM");
                if (System.IO.Directory.Exists(directory))
                {
                    openFileDialog.InitialDirectory = directory;
                }

                openFileDialog.Filter = "json files (*.json)|*.json|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;
                if (openFileDialog.ShowDialog(owner) != DialogResult.OK)
                {
                    return;
                }
                path = openFileDialog.FileName;
            }

            if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path))
            {
                return;
            }

            List<IJSAMObject> jSAMObjects = null;
            try
            {
                jSAMObjects = Core.Convert.ToSAM<IJSAMObject>(path);
            }
            catch (Exception exception)
            {
                MessageBox.Show("Cannot open file specified");
                return;
            }

            if (jSAMObjects == null || jSAMObjects.Count == 0)
            {
                MessageBox.Show("No objects to import");
                return;
            }

            

            List<Tuple<string, string, IJSAMObject>> tuples_All = new List<Tuple<string, string, IJSAMObject>>();
            foreach (IJSAMObject jSAMObject in jSAMObjects)
            {
                if (jSAMObject == null)
                {
                    continue;
                }

                if (jSAMObject is AnalyticalModel)
                {
                    AnalyticalModel analyticalModel_Temp = (AnalyticalModel)jSAMObject;

                    List<IMaterial> materials = analyticalModel_Temp.MaterialLibrary?.GetMaterials();
                    if (materials != null)
                    {
                        foreach (IMaterial material in materials)
                        {
                            tuples_All.Add(new Tuple<string, string, IJSAMObject>(typeof(Material).Name, material.Name, material));
                        }
                    }

                    List<Construction> constructions_Temp = analyticalModel_Temp.AdjacencyCluster.GetConstructions();
                    if (constructions_Temp != null)
                    {
                        foreach (Construction construction in constructions_Temp)
                        {
                            tuples_All.Add(new Tuple<string, string, IJSAMObject>(typeof(Construction).Name, construction.Name, construction));
                        }
                    }

                }
                else if (jSAMObject is MaterialLibrary)
                {
                    List<IMaterial> materials = ((MaterialLibrary)jSAMObject).GetMaterials();
                    if (materials != null)
                    {
                        foreach (IMaterial material in materials)
                        {
                            tuples_All.Add(new Tuple<string, string, IJSAMObject>(typeof(Material).Name, material.Name, material));
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
                            tuples_All.Add(new Tuple<string, string, IJSAMObject>(typeof(Construction).Name, construction.Name, construction));
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
                            tuples_All.Add(new Tuple<string, string, IJSAMObject>(typeof(ApertureConstruction).Name, apertureConstruction.Name, apertureConstruction));
                        }
                    }

                }
                else if (jSAMObject is IMaterial)
                {
                    tuples_All.Add(new Tuple<string, string, IJSAMObject>(typeof(Material).Name, ((IMaterial)jSAMObject).Name, jSAMObject));
                }
                else if (jSAMObject is Construction)
                {
                    tuples_All.Add(new Tuple<string, string, IJSAMObject>(typeof(Construction).Name, ((Construction)jSAMObject).Name, jSAMObject));
                }
                else if (jSAMObject is ApertureConstruction)
                {
                    tuples_All.Add(new Tuple<string, string, IJSAMObject>(typeof(ApertureConstruction).Name, ((ApertureConstruction)jSAMObject).Name, jSAMObject));
                }
            }

            if(tuples_All == null || tuples_All.Count == 0)
            {
                MessageBox.Show("No objects to import");
                return;
            }

            HashSet<string> groups = new HashSet<string>();
            tuples_All.ForEach(x => groups.Add(x.Item1));

            List<Tuple<string, string, IJSAMObject>> tuples_Selected = null;
            using (TreeViewForm<Tuple<string, string, IJSAMObject>> treeViewForm = new TreeViewForm<Tuple<string, string, IJSAMObject>>("Select Objects", tuples_All, (Tuple<string, string, IJSAMObject> x) => x.Item2, (Tuple<string, string, IJSAMObject> x) => x.Item1))
            {
                if(groups.Count < 2)
                {
                    treeViewForm.ExpandAll();
                }

                if (treeViewForm.ShowDialog(owner) != DialogResult.OK)
                {
                    return;
                }

                tuples_Selected = treeViewForm?.SelectedItems;
            }

            if (tuples_Selected == null || tuples_Selected.Count == 0)
            {
                return;
            }

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;

            List<Construction> constructions = new List<Construction>();
            List<ApertureConstruction> apertureConstructions = new List<ApertureConstruction>();
            foreach (Tuple<string, string, IJSAMObject> tuple in tuples_Selected)
            {
                IJSAMObject jSAMObject = tuple?.Item3;

                if (jSAMObject == null)
                {
                    continue;
                }

                if (jSAMObject is IMaterial)
                {
                    analyticalModel.AddMaterial((IMaterial)jSAMObject);
                }
                else if (jSAMObject is Construction)
                {
                    constructions.Add((Construction)jSAMObject);
                }
                else if (jSAMObject is ApertureConstruction)
                {
                    apertureConstructions.Add((ApertureConstruction)jSAMObject);
                }
            }

            if (constructions != null && constructions.Count != 0)
            {
                adjacencyCluster.UpdateConstructions(constructions);
            }

            if (apertureConstructions != null && apertureConstructions.Count != 0)
            {
                adjacencyCluster.UpdateConstructions(constructions);
            }

            if (apertureConstructions != null || constructions != null)
            {
                HashSet<string> names = new HashSet<string>();

                if (constructions != null)
                {
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
                }

                if (apertureConstructions != null)
                {
                    foreach (ApertureConstruction apertureConstruction in apertureConstructions)
                    {
                        List<ConstructionLayer> constructionLayers = null;

                        constructionLayers = apertureConstruction?.PaneConstructionLayers;
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
                }

                List<IMaterial> materials = tuples_All.FindAll(x => x.Item3 is IMaterial).ConvertAll(x => (IMaterial)x.Item3);
                if (materials != null && materials.Count != 0)
                {
                    MaterialLibrary materialLibrary = analyticalModel.MaterialLibrary;

                    HashSet<string> names_Missing = new HashSet<string>();
                    foreach (string name in names)
                    {
                        if (materialLibrary.GetMaterial(name) == null)
                        {
                            names_Missing.Add(name);
                        }
                    }

                    if (names_Missing != null && names_Missing.Count != 0)
                    {
                        DialogResult dialogResult = MessageBox.Show(owner, "Try to import missing materials?", "Materials", MessageBoxButtons.YesNo);
                        if (dialogResult == DialogResult.Yes)
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

            analyticalModel = new AnalyticalModel(analyticalModel);

            uIAnalyticalModel.JSAMObject = new AnalyticalModel(analyticalModel, adjacencyCluster);
        }
    }
}