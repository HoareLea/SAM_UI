using SAM.Analytical.Tas;
using SAM.Analytical.Windows;
using SAM.Core;
using SAM.Core.Tas;
using SAM.Core.Windows.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        public static void EditApertureConstructions(this UIAnalyticalModel uIAnalyticalModel, IWin32Window owner = null)
        {
            AdjacencyCluster adjacencyCluster = uIAnalyticalModel?.JSAMObject?.AdjacencyCluster;
            if (adjacencyCluster == null)
            {
                adjacencyCluster = new AdjacencyCluster();
            }

            List<ApertureConstruction> apertureConstructions = adjacencyCluster.GetApertureConstructions();
            ApertureConstructionLibrary apertureConstructionLibrary = new ApertureConstructionLibrary(uIAnalyticalModel.JSAMObject.Name);
            apertureConstructions?.ForEach(x => apertureConstructionLibrary.Add(x));

            MaterialLibrary materialLibrary = uIAnalyticalModel.JSAMObject.MaterialLibrary;

            using (Analytical.Windows.Forms.ApertureConstructionLibraryForm apertureConstructionLibraryForm = new Analytical.Windows.Forms.ApertureConstructionLibraryForm(materialLibrary, apertureConstructionLibrary))
            {
                apertureConstructionLibraryForm.ConstructionManagerImporting += ApertureConstructionLibraryForm_ConstructionManagerImporting;
                apertureConstructionLibraryForm.ConstructionManagerExporting += ApertureConstructionLibraryForm_ConstructionManagerExporting;
                apertureConstructionLibraryForm.MultiSelect = true;

                apertureConstructionLibraryForm.Text = "Aperture Constructions";
                if (apertureConstructionLibraryForm.ShowDialog(owner) != DialogResult.OK)
                {
                    return;
                }

                apertureConstructionLibrary = apertureConstructionLibraryForm.ApertureConstructionLibrary;
                materialLibrary = apertureConstructionLibraryForm.MaterialLibrary;
            }

            adjacencyCluster.ReplaceApertureConstructions(apertureConstructionLibrary);

            uIAnalyticalModel.JSAMObject = new AnalyticalModel(uIAnalyticalModel.JSAMObject, adjacencyCluster, materialLibrary, uIAnalyticalModel.JSAMObject.ProfileLibrary);
        }

        private static void ApertureConstructionLibraryForm_ConstructionManagerExporting(object sender, ConstructionManagerExportingEventArgs e)
        {
            IWin32Window win32Widnow = sender as IWin32Window;

            e.Handled = true;

            ConstructionManager constructionManager = e.ConstructionManager;
            if (constructionManager == null)
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
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.Delete(path);
                }

                using (SAMTCDDocument sAMTCDDocument = new SAMTCDDocument())
                {
                    if (sAMTCDDocument.Create(path))
                    {
                        TCD.Document document = sAMTCDDocument.Document;

                        List<IMaterial> materials = constructionManager.Materials;
                        if (materials != null)
                        {
                            foreach (IMaterial material in materials)
                            {
                                if(material == null)
                                {
                                    continue;
                                }
                                
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

                        List<ApertureConstruction> apertureConstructions = constructionManager.ApertureConstructions;
                        if (apertureConstructions != null)
                        {
                            foreach (ApertureConstruction apertureConstruction in apertureConstructions)
                            {
                                if (!apertureConstruction.TryGetValue(ParameterizedSAMObjectParameter.Category, out Category category))
                                {
                                    category = new Category(document.constructionRoot.name);
                                }

                                TCD.ConstructionFolder constructionFolder = Tas.Convert.ToTCD_ConstructionFolder(category, document);
                                if (constructionFolder == null)
                                {
                                    continue;
                                }

                                Tas.Modify.Update(constructionFolder, constructionManager, apertureConstruction);
                                result = true;
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
                if (materialLibrary == null || materialLibrary.GetMaterials() == null)
                {
                    ApertureConstructionLibrary apertureConstructionLibrary = new ApertureConstructionLibrary(System.IO.Path.GetFileNameWithoutExtension(path));
                    constructionManager.ApertureConstructions?.ForEach(x => apertureConstructionLibrary.Add(x));

                    result = Core.Convert.ToFile(apertureConstructionLibrary, path);
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

        private static void ApertureConstructionLibraryForm_ConstructionManagerImporting(object sender, ConstructionManagerImportingEventArgs e)
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

                if(constructionManager?.Constructions == null || constructionManager?.Constructions.Count == 0)
                {
                    MessageBox.Show("Data could not be imported. No ApertureConstructions in source file.");
                }

                ApertureType apertureType = ApertureType.Window;
                using (ComboBoxForm<ApertureType> comboBoxForm = new ComboBoxForm<ApertureType>("Aperture Type", Enum.GetValues(typeof(ApertureType)).Cast<ApertureType>(), x => x == ApertureType.Undefined ? string.Empty : x.Description()))
                {
                    comboBoxForm.SelectedItem = apertureType;
                    if(comboBoxForm.ShowDialog() == DialogResult.OK)
                    {
                        apertureType = comboBoxForm.SelectedItem;
                    }
                }

                Core.UI.WPF.TreeViewWindow treeViewWindow = new Core.UI.WPF.TreeViewWindow();
                treeViewWindow.GettingCategory += TreeViewWindow_GettingConstructionCategory;
                treeViewWindow.GettingText += TreeViewWindow_GettingConstructionText;
                treeViewWindow.SetObjects(constructionManager?.Constructions);
                if (treeViewWindow.ShowDialog() != true)
                {
                    return;
                }

                constructionManager = constructionManager.Filter(treeViewWindow.GetObjects<Construction>(), removeUnusedMaterials: true);
                List<Construction> constructions = constructionManager?.Constructions;
                if (constructions != null)
                {
                    foreach(Construction construction in constructions)
                    {
                        ApertureConstruction apertureConstruction = Analytical.Create.ApertureConstruction(apertureType, construction.Name, construction);
                        if(apertureConstruction == null)
                        {
                            continue;
                        }

                        if (construction.TryGetValue(ConstructionParameter.Description, out string description) && description != null)
                        {
                            apertureConstruction.SetValue(ApertureConstructionParameter.Description, description);
                        }

                        if (construction.TryGetValue(Tas.ConstructionParameter.AdditionalHeatTransfer, out double additionalHeatTransfer) && !double.IsNaN(additionalHeatTransfer) && additionalHeatTransfer != 0)
                        {
                            apertureConstruction.SetValue(Tas.ApertureConstructionParameter.AdditionalHeatTransfer, additionalHeatTransfer);
                        }

                        constructionManager.Remove(construction);
                        constructionManager.Add(apertureConstruction);
                    }
                }

                e.ConstructionManager = constructionManager;
            }
            else
            {
                AnalyticalModel analyticalModel = new AnalyticalModel(Guid.NewGuid(), "Temporary AnalyticalModel");
                Func<IJSAMObject, bool> func = new Func<IJSAMObject, bool>(x => { return x is Material || x is ApertureConstruction; });

                analyticalModel = Analytical.Windows.Query.Import(analyticalModel, path, func, new ImportOptions() { UserSelection = false, SuppressMessages = false }, win32Widnow);
                e.ConstructionManager = analyticalModel?.ConstructionManager;
            }
        }
    }
}