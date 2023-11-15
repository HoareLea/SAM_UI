using SAM.Analytical.Windows;
using SAM.Core;
using SAM.Core.Windows.Forms;
using System.Collections.Generic;
using System;
using System.Windows.Forms;
using SAM.Core.UI.WPF;
using System.Linq;
using SAM.Analytical.Tas;
using SAM.Core.Tas;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        public static void EditMaterialLibrary(this UIAnalyticalModel uIAnalyticalModel, IWin32Window owner = null)
        {
            if (uIAnalyticalModel?.JSAMObject == null)
            {
                return;
            }

            MaterialLibrary materialLibrary = uIAnalyticalModel.JSAMObject.MaterialLibrary;

            using (MaterialLibraryForm materialLibraryForm = new MaterialLibraryForm(materialLibrary, Core.Query.Enums(typeof(IMaterial))))
            {
                materialLibraryForm.MaterialLibraryImporting += MaterialLibraryForm_MaterialLibraryImporting;
                materialLibraryForm.MaterialLibraryExporting += MaterialLibraryForm_MaterialLibraryExporting;
                if (materialLibraryForm.ShowDialog(owner) != DialogResult.OK)
                {
                    return;
                }

                materialLibrary = materialLibraryForm.MaterialLibrary;
            }

            uIAnalyticalModel.JSAMObject = new AnalyticalModel(uIAnalyticalModel.JSAMObject, uIAnalyticalModel.JSAMObject.AdjacencyCluster, materialLibrary, uIAnalyticalModel.JSAMObject.ProfileLibrary);
        }

        private static void MaterialLibraryForm_MaterialLibraryExporting(object sender, Core.Windows.MaterialLibraryExportingEventArgs e)
        {
            IWin32Window win32Widnow = sender as IWin32Window;

            e.Handled = true;

            MaterialLibrary materialLibrary = e.MaterialLibrary;
            if (materialLibrary == null)
            {
                MessageBox.Show("Nothing to be exported");
            }

            string path = null;
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "json files (*.json)|*.json|Tas Construction Databases (*.tcd)|*.tcd|All files (*.*)|*.*";
                saveFileDialog.FilterIndex = 1;
                saveFileDialog.RestoreDirectory = true;
                saveFileDialog.FileName = "SAM_MaterialLibrary_CustomVer00.json";

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
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.Delete(path);
                }

                using (SAMTCDDocument sAMTCDDocument = new SAMTCDDocument())
                {
                    if (sAMTCDDocument.Create(path))
                    {
                        TCD.Document document = sAMTCDDocument.Document;

                        List<IMaterial> materials = materialLibrary.GetMaterials();
                        if (materials != null)
                        {
                            foreach (IMaterial material in materials)
                            {
                                if (!material.TryGetValue(ParameterizedSAMObjectParameter.Category, out Category category))
                                {
                                    category = new Category(document.materialRoot.name);
                                    MaterialType materialType = material.MaterialType();
                                    category = Core.Create.Category(materialType.ToString(), category);
                                }

                                TCD.MaterialFolder materialFolder = Tas.Convert.ToTCD_MaterialFolder(category, document);

                                material.ToTCD(materialFolder);
                            }
                        }

                        if (result)
                        {
                            document.save();
                        }
                    }
                }
            }
            else
            {
                result = Core.Convert.ToFile(materialLibrary, path);
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

        private static void MaterialLibraryForm_MaterialLibraryImporting(object sender, Core.Windows.MaterialLibraryImportingEventArgs e)
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

                ConstructionManager constructionManager = Tas.Convert.ToSAM_ConstructionManager(path, 0.0001);

                marqueeProgressForm.Close();

                if (constructionManager?.Materials == null || constructionManager?.Materials.Count == 0)
                {
                    MessageBox.Show("Data could not be imported. No Materials in source file.");
                }

                TreeViewWindow treeViewWindow = new TreeViewWindow();
                treeViewWindow.GettingCategory += TreeViewWindow_GettingMaterialCategory;
                treeViewWindow.GettingText += TreeViewWindow_GettingMaterialText;
                treeViewWindow.SetObjects(constructionManager?.Materials);
                if (treeViewWindow.ShowDialog() != true)
                {
                    return;
                }



                e.MaterialLibrary = new MaterialLibrary(string.Empty);
                treeViewWindow.GetObjects<IMaterial>()?.ForEach(x => e.MaterialLibrary.Add(x));
            }
            else
            {
                AnalyticalModel analyticalModel = new AnalyticalModel(Guid.NewGuid(), "Temporary AnalyticalModel");
                Func<IJSAMObject, bool> func = new Func<IJSAMObject, bool>(x => { return x is Material; });

                analyticalModel = Analytical.Windows.Query.Import(analyticalModel, path, func, new ImportOptions() { UserSelection = false, SuppressMessages = false }, win32Widnow);
                e.MaterialLibrary = analyticalModel?.MaterialLibrary;
            }
        }

        private static void TreeViewWindow_GettingMaterialText(object sender, GettingTextEventArgs e)
        {
            e.Text = (e?.Object as Material)?.DisplayName;
        }

        private static void TreeViewWindow_GettingMaterialCategory(object sender, GettingCategoryEventArgs e)
        {
            e.Category = (e?.Object as Material)?.GetValue<Category>(ParameterizedSAMObjectParameter.Category);
        }
    }
}