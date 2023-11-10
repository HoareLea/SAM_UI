using SAM.Analytical.Windows;
using SAM.Core;
using SAM.Core.Tas;
using SAM.Core.Windows.Forms;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        public static void EditConstructions(this UIAnalyticalModel uIAnalyticalModel, IWin32Window owner = null)
        {
            AdjacencyCluster adjacencyCluster = uIAnalyticalModel.JSAMObject.AdjacencyCluster;
            if(adjacencyCluster == null)
            {
                adjacencyCluster = new AdjacencyCluster();
            }

            List<Construction> constructions = adjacencyCluster.GetConstructions();

            ConstructionLibrary constructionLibrary = new ConstructionLibrary(uIAnalyticalModel.JSAMObject.Name);
            constructions?.ForEach(x => constructionLibrary.Add(x));

            MaterialLibrary materialLibrary = uIAnalyticalModel.JSAMObject.MaterialLibrary;
            if(materialLibrary == null)
            {
                materialLibrary = new MaterialLibrary(string.Format("MaterialLibrary"));
            }

            using (Analytical.Windows.Forms.ConstructionLibraryForm constructionLibraryForm = new Analytical.Windows.Forms.ConstructionLibraryForm(materialLibrary, constructionLibrary))
            {
                constructionLibraryForm.ConstructionManagerImporting += ConstructionLibraryForm_ConstructionManagerImporting;
                constructionLibraryForm.ConstructionManagerExporting += ConstructionLibraryForm_ConstructionManagerExporting;

                constructionLibraryForm.Text = "Constructions";
                if (constructionLibraryForm.ShowDialog(owner) != DialogResult.OK)
                {
                    return;
                }

                constructionLibrary = constructionLibraryForm.ConstructionLibrary;
                materialLibrary = constructionLibraryForm.MaterialLibrary;
            }

            adjacencyCluster.ReplaceConstructions(constructionLibrary);

            uIAnalyticalModel.JSAMObject = new AnalyticalModel(uIAnalyticalModel.JSAMObject, adjacencyCluster, materialLibrary, uIAnalyticalModel.JSAMObject.ProfileLibrary);
        }

        private static void ConstructionLibraryForm_ConstructionManagerExporting(object sender, ConstructionManagerExportingEventArgs e)
        {
            IWin32Window win32Widnow = sender as IWin32Window;

            e.Handled = true;

            ConstructionManager constructionManager = e.ConstructionManager;
            if(constructionManager == null)
            {
                MessageBox.Show("Nothing to be exported");
            }

            MaterialLibrary materialLibrary = constructionManager.MaterialLibrary;

            string path = null;
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "json files (*.json)|*.json|Tas Construction Databases (*.tcd)|*.tcd|All files (*.*)|*.*";
                saveFileDialog.FilterIndex = 1;
                saveFileDialog.RestoreDirectory = true;
                if (materialLibrary == null || materialLibrary.GetMaterials() == null)
                {
                    saveFileDialog.FileName = "SAM_ConstructionLibrary_CustomVer00.json";
                }
                else
                {
                    saveFileDialog.FileName = "SAM_ConstructionManager_CustomVer00.json";
                }

                if (saveFileDialog.ShowDialog(win32Widnow) != DialogResult.OK)
                {
                    return;
                }
                path = saveFileDialog.FileName;
            }

            if (path == null)
            {
                return;
            }

            bool result = false;

            if (System.IO.Path.GetExtension(path) == ".tcd")
            {
                if(System.IO.File.Exists(path))
                {
                    System.IO.File.Delete(path);
                }
                
                using (SAMTCDDocument sAMTCDDocument = new SAMTCDDocument())
                {
                    if(sAMTCDDocument.Create(path))
                    {
                        TCD.Document document = sAMTCDDocument.Document;

                        List<IMaterial> materials = constructionManager.Materials;
                        if (materials != null)
                        {
                            foreach (IMaterial material in materials)
                            {
                                if (!material.TryGetValue(ParameterizedSAMObjectParameter.Category, out Category category))
                                {
                                    category = new Category(document.materialRoot.name);
                                }
                            }
                        }

                        List<Construction> constructions = constructionManager.Constructions;
                        if (constructions != null)
                        {
                            foreach (Construction construction in constructions)
                            {
                                if (!construction.TryGetValue(ParameterizedSAMObjectParameter.Category, out Category category))
                                {
                                    category = new Category(document.constructionRoot.name);
                                }

                                TCD.ConstructionFolder constructionFolder = Tas.Convert.ToTCD_ConstructionFolder(category, document);
                                if (constructionFolder == null)
                                {
                                    continue;
                                }

                                Tas.Convert.ToTCD(construction, constructionFolder, constructionManager);
                                result = true;
                            }
                        }

                        if(result)
                        {
                            document.save();
                        }
                    }
                }
            }
            else
            {
                if (materialLibrary == null || materialLibrary.GetMaterials() == null)
                {
                    ConstructionLibrary constructionLibrary = new ConstructionLibrary(System.IO.Path.GetFileNameWithoutExtension(path));
                    constructionManager.Constructions?.ForEach(x => constructionLibrary.Add(x));

                    result = Core.Convert.ToFile(constructionLibrary, path);
                }
                else
                {
                    result = Core.Convert.ToFile(constructionManager, path);
                }
            }

            if (result)
            {
                MessageBox.Show("Data exported successfully.");
            }
            else
            {
                MessageBox.Show("Data could not be exported.");
            }

        }

        private static void ConstructionLibraryForm_ConstructionManagerImporting(object sender, ConstructionManagerImportingEventArgs e)
        {
            IWin32Window win32Widnow = sender as IWin32Window;

            e.Handled = true;

            string path = null;
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                string directory = Analytical.Query.ResourcesDirectory();
                if (System.IO.Directory.Exists(directory))
                {
                    openFileDialog.InitialDirectory = directory;
                }

                openFileDialog.Filter = "json files (*.json)|*.json|Tas Construction Databases (*.tcd)|*.tcd|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 3;
                openFileDialog.RestoreDirectory = true;
                if (openFileDialog.ShowDialog(win32Widnow) != DialogResult.OK)
                {
                    return;
                }
                path = openFileDialog.FileName;
            }

            if (path == null)
            {
                return;
            }

            if (System.IO.Path.GetExtension(path) == ".tcd")
            {
                MarqueeProgressForm marqueeProgressForm = new MarqueeProgressForm("Importing");
                marqueeProgressForm.Show();

                ConstructionManager constructionManager = Tas.Convert.ToSAM_ConstructionManager(path);

                marqueeProgressForm.Close();

                Core.UI.WPF.TreeViewWindow treeViewWindow = new Core.UI.WPF.TreeViewWindow();
                treeViewWindow.GettingCategory += TreeViewWindow_GettingCategory;
                treeViewWindow.GettingText += TreeViewWindow_GettingText;
                treeViewWindow.SetObjects(constructionManager?.Constructions);
                if(treeViewWindow.ShowDialog() != true)
                {
                    return;
                }

                e.ConstructionManager = constructionManager.Filter(treeViewWindow.GetObjects<Construction>(), removeUnusedMaterials: true);
            }
            else
            {
                AnalyticalModel analyticalModel = new AnalyticalModel(Guid.NewGuid(), "Temporary AnalyticalModel");
                Func<IJSAMObject, bool> func = new Func<IJSAMObject, bool>(x => { return x is Material || x is Construction; });

                analyticalModel = Analytical.Windows.Query.Import(analyticalModel, path, func, new ImportOptions() { UserSelection = false, SuppressMessages = false }, win32Widnow);
                e.ConstructionManager = analyticalModel?.ConstructionManager;
            }
        }

        private static void TreeViewWindow_GettingText(object sender, Core.UI.WPF.GettingTextEventArgs e)
        {
            e.Text = (e?.Object as Construction)?.Name;
        }

        private static void TreeViewWindow_GettingCategory(object sender, Core.UI.WPF.GettingCategoryEventArgs e)
        {
            e.Category = (e?.Object as Construction)?.GetValue<Category>(ParameterizedSAMObjectParameter.Category);
        }
    }
}