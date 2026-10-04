// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using SAM.Core.UI.WPF;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace SAM.Analytical.UI
{
    /// <summary>
    /// WPF replacement for the WinForms SAM.Analytical.Windows.Forms.ConstructionLibraryForm: a
    /// searchable construction-library manager (add / remove / duplicate / import / export, edit on
    /// double-click, change default panel type via the row context menu). Mirrors the original public
    /// API (constructors, the ConstructionManagerImporting/Exporting events, MultiSelect,
    /// MaterialsButtonVisible, Enabled, the ConstructionLibrary and MaterialLibrary properties, and
    /// GetConstructions) so hosts can drop it in with minimal changes.
    /// </summary>
    public partial class ConstructionLibraryWindow : System.Windows.Window
    {
        public event EventHandler<ConstructionManagerExportingEventArgs> ConstructionManagerExporting;
        public event EventHandler<ConstructionManagerImportingEventArgs> ConstructionManagerImporting;

        /// <summary>
        /// Raised by the "Set U-value..." button (shown only while a handler is attached) with the one selected
        /// construction. The handler hands over to the "Set U-value" window and closes this one.
        /// </summary>
        public event EventHandler<SetUValueRequestedEventArgs> SetUValueRequested
        {
            add
            {
                setUValueRequested += value;
                Button_SetUValue.Visibility = setUValueRequested == null ? Visibility.Collapsed : Visibility.Visible;
            }

            remove
            {
                setUValueRequested -= value;
                Button_SetUValue.Visibility = setUValueRequested == null ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        private EventHandler<SetUValueRequestedEventArgs> setUValueRequested;

        /// <summary>
        /// Raised by the "Save to My constructions..." button (shown only while a handler is attached) with the one selected construction and the
        /// window's material library. The handler asks for a name and saves a NEW construction to "My constructions"; this window stays open and
        /// unchanged.
        /// </summary>
        public event EventHandler<SaveToMyConstructionsRequestedEventArgs> SaveToMyConstructionsRequested
        {
            add
            {
                saveToMyConstructionsRequested += value;
                Button_SaveToMyConstructions.Visibility = saveToMyConstructionsRequested == null ? Visibility.Collapsed : Visibility.Visible;
            }

            remove
            {
                saveToMyConstructionsRequested -= value;
                Button_SaveToMyConstructions.Visibility = saveToMyConstructionsRequested == null ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        private EventHandler<SaveToMyConstructionsRequestedEventArgs> saveToMyConstructionsRequested;

        private MaterialLibrary materialLibrary;
        private ConstructionLibrary constructionLibrary;
        private Construction construction_Selected;

        private readonly ObservableCollection<Row> rows = new ObservableCollection<Row>();

        public ConstructionLibraryWindow()
        {
            InitializeComponent();
            DataGrid_Constructions.ItemsSource = rows;

            List<string> panelTypes = new List<string> { string.Empty };
            foreach (PanelType panelType in Enum.GetValues(typeof(PanelType)))
            {
                if (panelType == PanelType.Undefined)
                {
                    continue;
                }

                panelTypes.Add(Core.Query.Description(panelType));
            }

            Column_Type.ItemsSource = panelTypes;
        }

        public ConstructionLibraryWindow(MaterialLibrary materialLibrary, ConstructionLibrary constructionLibrary)
            : this()
        {
            this.materialLibrary = materialLibrary;
            SetConstructionLibrary(constructionLibrary);
        }

        public ConstructionLibraryWindow(MaterialLibrary materialLibrary, ConstructionLibrary constructionLibrary, Construction construction)
            : this()
        {
            this.materialLibrary = materialLibrary;
            construction_Selected = construction;
            SetConstructionLibrary(constructionLibrary);
        }

        private void SetConstructionLibrary(ConstructionLibrary constructionLibrary)
        {
            this.constructionLibrary = constructionLibrary ?? new ConstructionLibrary("Construction Library");

            string uniqueId = this.constructionLibrary?.GetUniqueId(construction_Selected);

            rows.Clear();

            List<Construction> constructions = this.constructionLibrary?.GetConstructions();
            if (constructions != null)
            {
                Row selectedRow = null;
                foreach (Construction construction in constructions)
                {
                    Row row = Add(construction);
                    if (uniqueId != null && uniqueId.Equals(this.constructionLibrary?.GetUniqueId(construction)))
                    {
                        selectedRow = row;
                    }
                }

                if (selectedRow != null)
                {
                    DataGrid_Constructions.SelectedItem = selectedRow;
                }
            }

            bool hasMaterials = materialLibrary != null && materialLibrary.GetMaterials() != null;
            Button_Materials.Visibility = hasMaterials ? Visibility.Visible : Visibility.Collapsed;
            Button_Add.Visibility = hasMaterials ? Visibility.Visible : Visibility.Collapsed;
        }

        private Row Add(Construction construction)
        {
            if (construction == null)
            {
                return null;
            }

            Row row = new Row(construction);
            rows.Add(row);
            return row;
        }

        private void FillRows(string search)
        {
            if (constructionLibrary == null)
            {
                return;
            }

            List<string> selectedIds = DataGrid_Constructions.SelectedItems.Cast<Row>()
                .Select(x => constructionLibrary.GetUniqueId(x.Construction))
                .Where(x => !string.IsNullOrWhiteSpace(x)).ToList();

            rows.Clear();

            List<Construction> constructions = constructionLibrary.GetConstructions();
            if (constructions == null || constructions.Count == 0)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                constructions = constructions.Search(search, (Construction construction) =>
                {
                    if (construction == null)
                    {
                        return null;
                    }

                    string result = construction.Name;
                    if (construction.TryGetValue(ConstructionParameter.DefaultPanelType, out string panelTypeString) && !string.IsNullOrWhiteSpace(panelTypeString))
                    {
                        PanelType panelType = Core.Query.Enum<PanelType>(panelTypeString);
                        if (panelType != PanelType.Undefined)
                        {
                            result = result == null ? Core.Query.Description(panelType) : string.Format("{0} {1}", result, Core.Query.Description(panelType));
                        }
                    }

                    return result;
                });
            }

            if (constructions == null)
            {
                return;
            }

            foreach (Construction construction in constructions)
            {
                Row row = Add(construction);
                string uniqueId = constructionLibrary.GetUniqueId(construction);
                if (uniqueId != null && selectedIds.Contains(uniqueId))
                {
                    DataGrid_Constructions.SelectedItems.Add(row);
                }
            }
        }

        private void TextBox_Search_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            FillRows(TextBox_Search.Text);
        }

        public List<Construction> GetConstructions(bool selected = true)
        {
            IEnumerable<Row> source = selected ? DataGrid_Constructions.SelectedItems.Cast<Row>() : rows;
            if (source == null)
            {
                return null;
            }

            List<Construction> result = new List<Construction>();
            foreach (Row row in source)
            {
                Construction construction = row.Construction;
                if (construction == null)
                {
                    continue;
                }

                PanelType panelType = Core.Query.Enum<PanelType>(row.Type);
                if (panelType == PanelType.Undefined)
                {
                    construction.RemoveValue(ConstructionParameter.DefaultPanelType);
                }
                else
                {
                    construction.SetValue(ConstructionParameter.DefaultPanelType, panelType);
                }

                if (string.IsNullOrEmpty(row.Description))
                {
                    construction.RemoveValue(ConstructionParameter.Description);
                }
                else
                {
                    construction.SetValue(ConstructionParameter.Description, row.Description);
                }

                result.Add(construction);
            }

            return result;
        }

        public bool MultiSelect
        {
            get
            {
                return DataGrid_Constructions.SelectionMode == System.Windows.Controls.DataGridSelectionMode.Extended;
            }

            set
            {
                DataGrid_Constructions.SelectionMode = value ? System.Windows.Controls.DataGridSelectionMode.Extended : System.Windows.Controls.DataGridSelectionMode.Single;
            }
        }

        public bool MaterialsButtonVisible
        {
            get
            {
                return Button_Materials.Visibility == Visibility.Visible;
            }

            set
            {
                Button_Materials.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public bool Enabled
        {
            set
            {
                Visibility visibility = value ? Visibility.Visible : Visibility.Collapsed;
                Button_Add.Visibility = visibility;
                Button_Duplicate.Visibility = visibility;
                Button_Remove.Visibility = visibility;
                DataGrid_Constructions.IsReadOnly = !value;
            }
        }

        public ConstructionLibrary ConstructionLibrary
        {
            get
            {
                if (constructionLibrary == null)
                {
                    return null;
                }

                ConstructionLibrary result = new ConstructionLibrary(constructionLibrary);
                constructionLibrary.GetConstructions().ForEach(x => result.Remove(x));

                GetConstructions(false)?.ForEach(x => result.Add(x));
                return result;
            }
        }

        public MaterialLibrary MaterialLibrary
        {
            get
            {
                return materialLibrary == null ? null : new MaterialLibrary(materialLibrary);
            }
        }

        private void Button_OK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void Button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Button_Add_Click(object sender, RoutedEventArgs e)
        {
            Construction construction = null;
            ConstructionWindow constructionWindow = new ConstructionWindow(materialLibrary, constructionLibrary) { Owner = this };
            if (constructionWindow.ShowDialog() != true)
            {
                return;
            }

            construction = constructionWindow.Construction;
            if (construction == null)
            {
                return;
            }

            constructionLibrary?.Add(construction);
            Add(construction);
        }

        private void Button_Remove_Click(object sender, RoutedEventArgs e)
        {
            foreach (Row row in DataGrid_Constructions.SelectedItems.Cast<Row>().ToList())
            {
                rows.Remove(row);
                constructionLibrary?.Remove(row.Construction);
            }
        }

        private void Button_Duplicate_Click(object sender, RoutedEventArgs e)
        {
            Row selected = DataGrid_Constructions.SelectedItems.Cast<Row>().FirstOrDefault();
            Construction construction = selected?.Construction;
            if (construction == null)
            {
                return;
            }

            string name = (string.IsNullOrWhiteSpace(construction.Name) ? string.Empty : construction.Name).Trim();
            string name_Temp = name;
            int index = 1;
            while (constructionLibrary?.GetConstructions()?.Find(x => x.Name == name_Temp) != null)
            {
                name_Temp = string.Format("{0} {1}", name, index.ToString());
                index++;
            }
            name = name_Temp;

            construction = new Construction(Guid.NewGuid(), construction, name);
            ConstructionWindow constructionWindow = new ConstructionWindow(materialLibrary, constructionLibrary, construction) { Owner = this };
            if (constructionWindow.ShowDialog() != true)
            {
                return;
            }

            construction = constructionWindow.Construction;
            if (construction == null)
            {
                return;
            }

            constructionLibrary?.Add(construction);
            Add(construction);
        }

        private void DataGrid_Constructions_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Row row = DataGrid_Constructions.SelectedItems.Cast<Row>().FirstOrDefault();
            Construction construction = row?.Construction;
            if (construction == null)
            {
                return;
            }

            ConstructionWindow constructionWindow = new ConstructionWindow(materialLibrary, constructionLibrary, construction)
            {
                Owner = this,
                Enabled = Button_Add.Visibility == Visibility.Visible
            };
            if (constructionWindow.ShowDialog() != true)
            {
                return;
            }

            construction = constructionWindow.Construction;
            row.Update(construction);
        }

        private void MenuItem_ChangeType_Click(object sender, RoutedEventArgs e)
        {
            List<Row> selected = DataGrid_Constructions.SelectedItems.Cast<Row>().ToList();
            if (selected.Count == 0)
            {
                return;
            }

            HashSet<PanelType> panelTypes = new HashSet<PanelType>();
            foreach (Row row in selected)
            {
                Construction construction = row.Construction;
                if (construction == null)
                {
                    continue;
                }

                if (!construction.TryGetValue(ConstructionParameter.DefaultPanelType, out string panelTypeString) || Core.Query.TryGetEnum(panelTypeString, out PanelType panelType_Temp))
                {
                    panelTypes.Add(PanelType.Undefined);
                }
                else
                {
                    panelTypes.Add(panelType_Temp);
                }

                if (panelTypes.Count > 1)
                {
                    break;
                }
            }

            PanelType panelType = panelTypes.Count > 1 ? PanelType.Undefined : panelTypes.First();
            ComboBoxWindow<PanelType> comboBoxWindow = new ComboBoxWindow<PanelType>("Panel Type", Enum.GetValues(typeof(PanelType)).Cast<PanelType>(), x => x == PanelType.Undefined ? string.Empty : Core.Query.Description(x))
            {
                Owner = this,
                SelectedItem = panelType
            };
            if (comboBoxWindow.ShowDialog() != true)
            {
                return;
            }

            panelType = comboBoxWindow.SelectedItem;

            foreach (Row row in selected)
            {
                Construction construction = row.Construction;
                if (construction == null)
                {
                    continue;
                }

                if (panelType == PanelType.Undefined)
                {
                    construction.RemoveValue(ConstructionParameter.DefaultPanelType);
                }
                else
                {
                    construction.SetValue(ConstructionParameter.DefaultPanelType, panelType.ToString());
                }

                row.Update(construction);
            }
        }

        private void Button_Materials_Click(object sender, RoutedEventArgs e)
        {
            MaterialLibraryWindow materialLibraryWindow = new MaterialLibraryWindow(materialLibrary, Core.Query.Enums(typeof(OpaqueMaterialParameter), typeof(TransparentMaterialParameter))) { Owner = this };
            if (materialLibraryWindow.ShowDialog() != true)
            {
                return;
            }

            materialLibrary = materialLibraryWindow.MaterialLibrary;
        }

        private void Button_Import_Click(object sender, RoutedEventArgs e)
        {
            ConstructionManager constructionManager = null;
            bool handled = false;

            if (ConstructionManagerImporting != null)
            {
                ConstructionManagerImportingEventArgs args = new ConstructionManagerImportingEventArgs();
                ConstructionManagerImporting.Invoke(this, args);
                if (args.Handled)
                {
                    handled = true;
                    constructionManager = args.ConstructionManager;
                }
            }

            if (!handled)
            {
                Func<IJSAMObject, bool> func = x => x is Material || x is Construction;
                constructionManager = Query.ImportConstructionManager(func, new ImportOptions(), this);
            }

            IEnumerable<Construction> constructions = constructionManager?.Constructions;
            if (constructions == null || constructions.Count() == 0)
            {
                MessageBox.Show("Constructions could not be imported.");
                return;
            }

            if (materialLibrary == null)
            {
                materialLibrary = new MaterialLibrary("MaterialLibrary");
            }

            constructionManager.Materials?.ForEach(x => materialLibrary.Add(x));

            if (constructionLibrary == null)
            {
                constructionLibrary = new ConstructionLibrary("ConstructionLibrary");
            }

            foreach (Construction construction in constructions)
            {
                constructionLibrary.Add(construction);
            }

            SetConstructionLibrary(constructionLibrary);
        }

        private void Button_Export_Click(object sender, RoutedEventArgs e)
        {
            List<Construction> constructions = GetConstructions(false);
            if (constructions == null || constructions.Count == 0)
            {
                return;
            }

            ConstructionLibrary constructionLibrary = new ConstructionLibrary("ConstructionLibrary");
            constructions.ForEach(x => constructionLibrary.Add(x));

            MaterialLibrary materialLibrary_Temp = materialLibrary == null ? null : new MaterialLibrary(materialLibrary);

            ConstructionManager constructionManager = new ConstructionManager(null, constructionLibrary, materialLibrary_Temp);

            if (ConstructionManagerExporting != null)
            {
                ConstructionManagerExportingEventArgs args = new ConstructionManagerExportingEventArgs { ConstructionManager = constructionManager };
                ConstructionManagerExporting.Invoke(this, args);
                if (args.Handled)
                {
                    return;
                }
            }

            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "json files (*.json)|*.json|All files (*.*)|*.*",
                FilterIndex = 1,
                RestoreDirectory = true,
                FileName = (materialLibrary == null || materialLibrary.GetMaterials() == null) ? "SAM_ConstructionLibrary_CustomVer00.json" : "SAM_ConstructionManager_CustomVer00.json"
            };

            if (saveFileDialog.ShowDialog(this) != true)
            {
                return;
            }

            bool result;
            if (materialLibrary == null || materialLibrary.GetMaterials() == null)
            {
                result = Core.Convert.ToFile(constructionLibrary, saveFileDialog.FileName);
            }
            else
            {
                result = Core.Convert.ToFile(constructionManager, saveFileDialog.FileName);
            }

            MessageBox.Show(result ? "Data exported successfully." : "Data could not be exported.");
        }

        private void DataGrid_Constructions_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            Button_SetUValue.IsEnabled = DataGrid_Constructions.SelectedItems.Count == 1;
            Button_SaveToMyConstructions.IsEnabled = DataGrid_Constructions.SelectedItems.Count == 1;
        }

        private void Button_SaveToMyConstructions_Click(object sender, RoutedEventArgs e)
        {
            Construction construction = GetConstructions(true)?.FirstOrDefault();
            if (construction == null || DataGrid_Constructions.SelectedItems.Count != 1)
            {
                return;
            }

            saveToMyConstructionsRequested?.Invoke(this, new SaveToMyConstructionsRequestedEventArgs(construction, MaterialLibrary));
        }

        private void Button_SetUValue_Click(object sender, RoutedEventArgs e)
        {
            Construction construction = GetConstructions(true)?.FirstOrDefault();
            if (construction == null || DataGrid_Constructions.SelectedItems.Count != 1)
            {
                return;
            }

            setUValueRequested?.Invoke(this, new SetUValueRequestedEventArgs(construction));
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            ConstructionLibrary.JsonForm(this, e);
        }

        private sealed class Row : INotifyPropertyChanged
        {
            private string name;
            private string description;
            private double thickness;
            private string type;

            public Row(Construction construction)
            {
                Update(construction);
            }

            public event PropertyChangedEventHandler PropertyChanged;

            public Construction Construction { get; private set; }

            public string Name
            {
                get => name;
                set { name = value; Raise(nameof(Name)); }
            }

            public string Description
            {
                get => description;
                set { description = value; Raise(nameof(Description)); }
            }

            public double Thickness
            {
                get => thickness;
                set { thickness = value; Raise(nameof(Thickness)); }
            }

            public string Type
            {
                get => type;
                set { type = value; Raise(nameof(Type)); }
            }

            public void Update(Construction construction)
            {
                Construction = construction;
                if (construction == null)
                {
                    return;
                }

                Name = construction.Name;
                Thickness = Math.Round(construction.GetThickness(), 3);

                Description = construction.TryGetValue(ConstructionParameter.Description, out string description_Temp) ? description_Temp : null;

                PanelType panelType = PanelType.Undefined;
                if (construction.TryGetValue(ConstructionParameter.DefaultPanelType, out string panelTypeString))
                {
                    panelType = Core.Query.Enum<PanelType>(panelTypeString);
                }

                Type = panelType == PanelType.Undefined ? string.Empty : Core.Query.Description(panelType);
            }

            private void Raise(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
