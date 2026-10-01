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
        public static void EditApertureConstructions(this UIAnalyticalModel uIAnalyticalModel, IWin32Window owner = null)
        {
            AdjacencyCluster adjacencyCluster = uIAnalyticalModel?.JSAMObject?.AdjacencyCluster;
            if (adjacencyCluster == null)
            {
                adjacencyCluster = new AdjacencyCluster();
            }

            List<ApertureConstruction> apertureConstructions = adjacencyCluster.GetApertureConstructions();
            ApertureConstructionLibrary apertureConstructionLibrary = new ApertureConstructionLibrary(uIAnalyticalModel?.JSAMObject?.Name ?? string.Empty);
            apertureConstructions?.ForEach(x => apertureConstructionLibrary.Add(x));

            MaterialLibrary materialLibrary = uIAnalyticalModel?.JSAMObject.MaterialLibrary;

            ApertureConstructionLibraryWindow apertureConstructionLibraryWindow = new ApertureConstructionLibraryWindow(materialLibrary, apertureConstructionLibrary)
            {
                Title = "Aperture Constructions"
            };
            apertureConstructionLibraryWindow.ConstructionManagerImporting += ApertureConstructionLibraryWindow_ConstructionManagerImporting;
            apertureConstructionLibraryWindow.ConstructionManagerExporting += ApertureConstructionLibraryWindow_ConstructionManagerExporting;
            apertureConstructionLibraryWindow.MultiSelect = true;

            // "Set glazing..." (U-value plan PR3): hand over to the Set glazing window. This window edits a copy, so it
            // closes first - an OK here after a glazing change would overwrite it - and unsaved edits are asked about.
            Guid? guid_SetGlazing = null;
            string state = LibraryState(apertureConstructionLibraryWindow);
            apertureConstructionLibraryWindow.SetGlazingRequested += (sender, e) =>
            {
                bool save = false;
                if (LibraryState(apertureConstructionLibraryWindow) != state)
                {
                    System.Windows.MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show(
                        apertureConstructionLibraryWindow,
                        "Save your changes to the aperture constructions before setting the glazing?\n\nYes saves them (one Undo step); No discards them.",
                        "Set glazing",
                        System.Windows.MessageBoxButton.YesNoCancel,
                        System.Windows.MessageBoxImage.Question);

                    if (messageBoxResult == System.Windows.MessageBoxResult.Cancel)
                    {
                        return;
                    }

                    save = messageBoxResult == System.Windows.MessageBoxResult.Yes;
                }

                e.Handled = true;
                guid_SetGlazing = e.ApertureConstruction?.Guid;
                apertureConstructionLibraryWindow.DialogResult = save;
            };

            if (apertureConstructionLibraryWindow.ShowDialog(owner) == true)
            {
                apertureConstructionLibrary = apertureConstructionLibraryWindow.ApertureConstructionLibrary;
                materialLibrary = apertureConstructionLibraryWindow.MaterialLibrary;

                adjacencyCluster.ReplaceApertureConstructions(apertureConstructionLibrary);

                uIAnalyticalModel.JSAMObject = new AnalyticalModel(uIAnalyticalModel.JSAMObject, adjacencyCluster, materialLibrary, uIAnalyticalModel.JSAMObject.ProfileLibrary);
            }

            if (guid_SetGlazing == null)
            {
                return;
            }

            if (uIAnalyticalModel.JSAMObject?.AdjacencyCluster?.GetApertureConstructions()?.Find(x => x != null && x.Guid == guid_SetGlazing.Value) == null)
            {
                System.Windows.MessageBox.Show("That aperture construction is not in the model yet: save the aperture constructions first, then set its glazing.", "Set glazing");
                return;
            }

            System.Windows.Window window_Owner = System.Windows.Application.Current?.MainWindow;
            uIAnalyticalModel.OpenSetGlazingWindow(guid_SetGlazing, null, window_Owner != null && window_Owner.IsVisible ? window_Owner : null);
        }

        // What the library window would commit, to tell whether it holds unsaved edits.
        private static string LibraryState(ApertureConstructionLibraryWindow apertureConstructionLibraryWindow)
        {
            return string.Concat(
                apertureConstructionLibraryWindow.ApertureConstructionLibrary?.ToJsonObject()?.ToJsonString(),
                "|",
                apertureConstructionLibraryWindow.MaterialLibrary?.ToJsonObject()?.ToJsonString());
        }

        private static void ApertureConstructionLibraryWindow_ConstructionManagerExporting(object sender, ConstructionManagerExportingEventArgs e)
        {
            System.Windows.Window owner = sender as System.Windows.Window;

            e.Handled = true;

            ConstructionManager constructionManager = e.ConstructionManager;
            if (constructionManager == null)
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
                                if (material == null)
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

            System.Windows.MessageBox.Show(result ? "Data exported successfully." : "Data could not be exported.");
        }

        private static void ApertureConstructionLibraryWindow_ConstructionManagerImporting(object sender, ConstructionManagerImportingEventArgs e)
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

                ConstructionManager constructionManager = Tas.Convert.ToSAM_ConstructionManager(path);

                progressBarWindow.Close();

                if (constructionManager?.Constructions == null || constructionManager?.Constructions.Count == 0)
                {
                    System.Windows.MessageBox.Show("Data could not be imported. No ApertureConstructions in source file.");
                }

                ApertureType apertureType = ApertureType.Window;
                ComboBoxWindow<ApertureType> comboBoxWindow = new ComboBoxWindow<ApertureType>("Aperture Type", Enum.GetValues(typeof(ApertureType)).Cast<ApertureType>(), x => x == ApertureType.Undefined ? string.Empty : Core.Query.Description(x))
                {
                    Owner = owner,
                    SelectedItem = apertureType
                };
                if (comboBoxWindow.ShowDialog() == true)
                {
                    apertureType = comboBoxWindow.SelectedItem;
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
                if (constructions != null)
                {
                    foreach (Construction construction in constructions)
                    {
                        ApertureConstruction apertureConstruction = Analytical.Create.ApertureConstruction(apertureType, construction.Name, construction);
                        if (apertureConstruction == null)
                        {
                            continue;
                        }

                        if (construction.TryGetValue(ConstructionParameter.Description, out string description) && description != null)
                        {
                            apertureConstruction.SetValue(ApertureConstructionParameter.Description, description);
                        }

                        if (construction.TryGetValue(Tas.ConstructionParameter.AdditionalHeatTransfer, out double additionalHeatTransfer) && !double.IsNaN(additionalHeatTransfer) && additionalHeatTransfer != 0)
                        {
                            apertureConstruction.SetValue(ApertureConstructionParameter.PaneAdditionalHeatTransfer, additionalHeatTransfer);
                            apertureConstruction.SetValue(ApertureConstructionParameter.FrameAdditionalHeatTransfer, additionalHeatTransfer);
                        }

                        constructionManager.Remove(construction);
                        constructionManager.Add(apertureConstruction);
                    }
                }

                e.ConstructionManager = constructionManager;
            }
            else
            {
                Func<IJSAMObject, bool> func = x => x is Material || x is ApertureConstruction;

                e.ConstructionManager = SAM.Analytical.UI.Query.ImportConstructionManager(path, func, new ImportOptions() { UserSelection = false, SuppressMessages = false }, owner);
            }
        }
    }
}
