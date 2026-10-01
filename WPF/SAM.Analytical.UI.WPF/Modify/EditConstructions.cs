// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Tas;
using SAM.Analytical.UI;
using SAM.Core;
using SAM.Core.Tas;
using SAM.Core.UI.WPF;
using System;
using System.Collections.Generic;
using System.Linq;
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

            ConstructionLibraryWindow constructionLibraryWindow = new ConstructionLibraryWindow(materialLibrary, constructionLibrary)
            {
                Title = "Constructions"
            };
            constructionLibraryWindow.ConstructionManagerImporting += ConstructionLibraryWindow_ConstructionManagerImporting;
            constructionLibraryWindow.ConstructionManagerExporting += ConstructionLibraryWindow_ConstructionManagerExporting;
            constructionLibraryWindow.MultiSelect = true;

            // "Set U-value..." (U-value plan PR2b): hand over to the Set U-value window. This window edits a copy, so it
            // closes first - an OK here after a U-value change would overwrite it - and unsaved edits are asked about.
            Guid? guid_SetUValue = null;
            string state = LibraryState(constructionLibraryWindow);
            constructionLibraryWindow.SetUValueRequested += (sender, e) =>
            {
                bool save = false;
                if (LibraryState(constructionLibraryWindow) != state)
                {
                    System.Windows.MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show(
                        constructionLibraryWindow,
                        "Save your changes to the constructions before setting the U-value?\n\nYes saves them (one Undo step); No discards them.",
                        "Set U-value",
                        System.Windows.MessageBoxButton.YesNoCancel,
                        System.Windows.MessageBoxImage.Question);

                    if (messageBoxResult == System.Windows.MessageBoxResult.Cancel)
                    {
                        return;
                    }

                    save = messageBoxResult == System.Windows.MessageBoxResult.Yes;
                }

                e.Handled = true;
                guid_SetUValue = e.Construction?.Guid;
                constructionLibraryWindow.DialogResult = save;
            };

            if (constructionLibraryWindow.ShowDialog(owner) == true)
            {
                constructionLibrary = constructionLibraryWindow.ConstructionLibrary;
                materialLibrary = constructionLibraryWindow.MaterialLibrary;

                adjacencyCluster.ReplaceConstructions(constructionLibrary);

                uIAnalyticalModel.JSAMObject = new AnalyticalModel(uIAnalyticalModel.JSAMObject, adjacencyCluster, materialLibrary, uIAnalyticalModel.JSAMObject.ProfileLibrary);
            }

            if (guid_SetUValue == null)
            {
                return;
            }

            if (uIAnalyticalModel.JSAMObject?.AdjacencyCluster?.GetConstructions()?.Find(x => x != null && x.Guid == guid_SetUValue.Value) == null)
            {
                System.Windows.MessageBox.Show("That construction is not in the model yet: save the constructions first, then set its U-value.", "Set U-value");
                return;
            }

            System.Windows.Window window_Owner = System.Windows.Application.Current?.MainWindow;
            uIAnalyticalModel.OpenSetUValueWindow(guid_SetUValue, null, window_Owner != null && window_Owner.IsVisible ? window_Owner : null);
        }

        // What the library window would commit, to tell whether it holds unsaved edits.
        private static string LibraryState(ConstructionLibraryWindow constructionLibraryWindow)
        {
            return string.Concat(
                constructionLibraryWindow.ConstructionLibrary?.ToJsonObject()?.ToJsonString(),
                "|",
                constructionLibraryWindow.MaterialLibrary?.ToJsonObject()?.ToJsonString());
        }

        private static void ConstructionLibraryWindow_ConstructionManagerExporting(object sender, ConstructionManagerExportingEventArgs e)
        {
            System.Windows.Window owner = sender as System.Windows.Window;

            e.Handled = true;

            ConstructionManager constructionManager = e.ConstructionManager;
            if(constructionManager == null)
            {
                System.Windows.MessageBox.Show("Nothing to be exported");
                return;
            }

            MaterialLibrary materialLibrary = constructionManager.MaterialLibrary;

            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "json files (*.json)|*.json|Tas Construction Databases (*.tcd)|*.tcd|All files (*.*)|*.*",
                FilterIndex = 1,
                RestoreDirectory = true,
                FileName = (materialLibrary == null || materialLibrary.GetMaterials() == null) ? "SAM_ConstructionLibrary_CustomVer00.json" : "SAM_ConstructionManager_CustomVer00.json"
            };

            if (saveFileDialog.ShowDialog(owner) != true)
            {
                return;
            }

            string path = saveFileDialog.FileName;
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
                                    MaterialType materialType = material.MaterialType();
                                    category = Core.Create.Category(materialType.ToString(), category);
                                }

                                TCD.MaterialFolder materialFolder = Tas.Convert.ToTCD_MaterialFolder(category, document);

                                material.ToTCD(materialFolder);
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

            System.Windows.MessageBox.Show(result ? "Data exported successfully." : "Data could not be exported.");
        }

        private static void ConstructionLibraryWindow_ConstructionManagerImporting(object sender, ConstructionManagerImportingEventArgs e)
        {
            System.Windows.Window owner = sender as System.Windows.Window;

            e.Handled = true;

            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "json files (*.json)|*.json|Tas Construction Databases (*.tcd)|*.tcd|All files (*.*)|*.*",
                FilterIndex = 3,
                RestoreDirectory = true
            };

            string directory = Analytical.Query.ResourcesDirectory();
            if (System.IO.Directory.Exists(directory))
            {
                openFileDialog.InitialDirectory = directory;
            }

            if (openFileDialog.ShowDialog(owner) != true)
            {
                return;
            }

            string path = openFileDialog.FileName;
            if (path == null)
            {
                return;
            }

            if (System.IO.Path.GetExtension(path) == ".tcd")
            {
                ProgressBarWindow progressBarWindow = new ProgressBarWindow("Importing", "Importing");
                progressBarWindow.Show();

                ConstructionManager constructionManager = Tas.Convert.ToSAM_ConstructionManager(path, 0.0001);

                progressBarWindow.Close();

                if (constructionManager?.Constructions == null || constructionManager?.Constructions.Count == 0)
                {
                    System.Windows.MessageBox.Show("Data could not be imported. No ApertureConstructions in source file.");
                }

                PanelType panelType = PanelType.Undefined;
                ComboBoxWindow<PanelType> comboBoxWindow = new ComboBoxWindow<PanelType>("PanelType", Enum.GetValues(typeof(PanelType)).Cast<PanelType>(), x => x == PanelType.Undefined ? string.Empty : Core.Query.Description(x))
                {
                    Owner = owner,
                    SelectedItem = panelType
                };
                if (comboBoxWindow.ShowDialog() == true)
                {
                    panelType = comboBoxWindow.SelectedItem;
                }

                MultipleSelectionTreeViewWindow treeViewWindow = new MultipleSelectionTreeViewWindow();
                treeViewWindow.GettingCategory += TreeViewWindow_GettingConstructionCategory;
                treeViewWindow.GettingText += TreeViewWindow_GettingConstructionText;
                treeViewWindow.SetObjects(constructionManager?.Constructions);
                SAM.Core.UI.WPF.Modify.SetOwner(treeViewWindow, owner);

                if (treeViewWindow.ShowDialog() != true)
                {
                    return;
                }

                constructionManager = constructionManager.Filter(treeViewWindow.GetObjects<Construction>(), removeUnusedMaterials: true);
                List<Construction> constructions = constructionManager?.Constructions;
                if (constructions != null && panelType != PanelType.Undefined)
                {
                    foreach (Construction construction in constructions)
                    {
                        construction.SetValue(ConstructionParameter.DefaultPanelType, panelType);
                        constructionManager.Add(construction);
                    }
                }

                e.ConstructionManager = constructionManager;
            }
            else
            {
                Func<IJSAMObject, bool> func = x => x is Material || x is Construction;

                e.ConstructionManager = SAM.Analytical.UI.Query.ImportConstructionManager(path, func, new ImportOptions() { UserSelection = false, SuppressMessages = false }, owner);
            }
        }

        private static void TreeViewWindow_GettingConstructionText(object sender, GettingTextEventArgs e)
        {
            e.Text = (e?.Object as Construction)?.Name;
        }

        private static void TreeViewWindow_GettingConstructionCategory(object sender, GettingCategoryEventArgs e)
        {
            e.Category = (e?.Object as Construction)?.GetValue<Category>(ParameterizedSAMObjectParameter.Category);
        }
    }
}
