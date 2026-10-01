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
    /// WPF replacement for the WinForms SAM.Analytical.Windows.Forms.ApertureConstructionLibraryForm:
    /// a searchable aperture-construction-library manager. Mirrors the original public API
    /// (constructors, the ConstructionManagerImporting/Exporting events, MultiSelect,
    /// MaterialsButtonVisible, Enabled, the ApertureConstructionLibrary and MaterialLibrary properties
    /// and GetApertureConstructions). Companion to <see cref="ConstructionLibraryWindow"/>.
    /// </summary>
    public partial class ApertureConstructionLibraryWindow : System.Windows.Window
    {
        public event EventHandler<ConstructionManagerExportingEventArgs> ConstructionManagerExporting;
        public event EventHandler<ConstructionManagerImportingEventArgs> ConstructionManagerImporting;

        /// <summary>
        /// Raised by the "Set glazing..." button (shown only while a handler is attached) with the one selected
        /// aperture construction. The handler hands over to the "Set glazing" window and closes this one.
        /// </summary>
        public event EventHandler<SetGlazingRequestedEventArgs> SetGlazingRequested
        {
            add
            {
                setGlazingRequested += value;
                Button_SetGlazing.Visibility = setGlazingRequested == null ? Visibility.Collapsed : Visibility.Visible;
            }

            remove
            {
                setGlazingRequested -= value;
                Button_SetGlazing.Visibility = setGlazingRequested == null ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        private EventHandler<SetGlazingRequestedEventArgs> setGlazingRequested;

        private MaterialLibrary materialLibrary;
        private ApertureConstructionLibrary apertureConstructionLibrary;
        private ApertureConstruction apertureConstruction_Selected;

        private readonly ObservableCollection<Row> rows = new ObservableCollection<Row>();

        public ApertureConstructionLibraryWindow()
        {
            InitializeComponent();
            DataGrid_Constructions.ItemsSource = rows;

            List<string> apertureTypes = new List<string>();
            foreach (ApertureType apertureType in Enum.GetValues(typeof(ApertureType)))
            {
                if (apertureType == ApertureType.Undefined)
                {
                    continue;
                }

                apertureTypes.Add(Core.Query.Description(apertureType));
            }

            Column_Type.ItemsSource = apertureTypes;
        }

        public ApertureConstructionLibraryWindow(MaterialLibrary materialLibrary, ApertureConstructionLibrary apertureConstructionLibrary)
            : this()
        {
            this.materialLibrary = materialLibrary;
            SetApertureConstructionLibrary(apertureConstructionLibrary);
        }

        public ApertureConstructionLibraryWindow(MaterialLibrary materialLibrary, ApertureConstructionLibrary apertureConstructionLibrary, ApertureConstruction apertureConstruction)
            : this()
        {
            this.materialLibrary = materialLibrary;
            apertureConstruction_Selected = apertureConstruction;
            SetApertureConstructionLibrary(apertureConstructionLibrary);
        }

        private void SetApertureConstructionLibrary(ApertureConstructionLibrary apertureConstructionLibrary)
        {
            this.apertureConstructionLibrary = apertureConstructionLibrary ?? new ApertureConstructionLibrary("Aperture Construction Library");

            string uniqueId = this.apertureConstructionLibrary?.GetUniqueId(apertureConstruction_Selected);

            rows.Clear();

            List<ApertureConstruction> apertureConstructions = this.apertureConstructionLibrary?.GetApertureConstructions();
            if (apertureConstructions != null)
            {
                Row selectedRow = null;
                foreach (ApertureConstruction apertureConstruction in apertureConstructions)
                {
                    Row row = Add(apertureConstruction);
                    if (uniqueId != null && uniqueId.Equals(this.apertureConstructionLibrary?.GetUniqueId(apertureConstruction)))
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

        private Row Add(ApertureConstruction apertureConstruction)
        {
            if (apertureConstruction == null)
            {
                return null;
            }

            Row row = new Row(apertureConstruction);
            rows.Add(row);
            return row;
        }

        private void FillRows(string search)
        {
            if (apertureConstructionLibrary == null)
            {
                return;
            }

            List<string> selectedIds = DataGrid_Constructions.SelectedItems.Cast<Row>()
                .Select(x => apertureConstructionLibrary.GetUniqueId(x.ApertureConstruction))
                .Where(x => !string.IsNullOrWhiteSpace(x)).ToList();

            rows.Clear();

            List<ApertureConstruction> apertureConstructions = apertureConstructionLibrary.GetApertureConstructions();
            if (apertureConstructions == null || apertureConstructions.Count == 0)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                apertureConstructions = apertureConstructions.Search(search, (ApertureConstruction apertureConstruction) =>
                {
                    if (apertureConstruction == null)
                    {
                        return null;
                    }

                    string result = apertureConstruction.Name;
                    ApertureType apertureType = apertureConstruction.ApertureType;
                    if (apertureType != ApertureType.Undefined)
                    {
                        result = result == null ? Core.Query.Description(apertureType) : string.Format("{0} {1}", result, Core.Query.Description(apertureType));
                    }

                    return result;
                });
            }

            if (apertureConstructions == null)
            {
                return;
            }

            foreach (ApertureConstruction apertureConstruction in apertureConstructions)
            {
                Row row = Add(apertureConstruction);
                string uniqueId = apertureConstructionLibrary.GetUniqueId(apertureConstruction);
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

        public List<ApertureConstruction> GetApertureConstructions(bool selected = true)
        {
            IEnumerable<Row> source = selected ? DataGrid_Constructions.SelectedItems.Cast<Row>() : rows;
            if (source == null)
            {
                return null;
            }

            List<ApertureConstruction> result = new List<ApertureConstruction>();
            foreach (Row row in source)
            {
                ApertureConstruction apertureConstruction = row.ApertureConstruction;
                if (apertureConstruction == null)
                {
                    continue;
                }

                ApertureType apertureType = Core.Query.Enum<ApertureType>(row.Type);
                apertureConstruction = new ApertureConstruction(apertureConstruction, apertureType);

                if (string.IsNullOrEmpty(row.Description))
                {
                    apertureConstruction.RemoveValue(ApertureConstructionParameter.Description);
                }
                else
                {
                    apertureConstruction.SetValue(ApertureConstructionParameter.Description, row.Description);
                }

                result.Add(apertureConstruction);
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

        public ApertureConstructionLibrary ApertureConstructionLibrary
        {
            get
            {
                if (apertureConstructionLibrary == null)
                {
                    return null;
                }

                ApertureConstructionLibrary result = new ApertureConstructionLibrary(apertureConstructionLibrary);
                apertureConstructionLibrary.GetApertureConstructions().ForEach(x => result.Remove(x));

                GetApertureConstructions(false)?.ForEach(x => result.Add(x));
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
            ApertureConstructionWindow apertureConstructionWindow = new ApertureConstructionWindow(materialLibrary, apertureConstructionLibrary) { Owner = this };
            if (apertureConstructionWindow.ShowDialog() != true)
            {
                return;
            }

            ApertureConstruction apertureConstruction = apertureConstructionWindow.ApertureConstruction;
            if (apertureConstruction == null)
            {
                return;
            }

            apertureConstructionLibrary?.Add(apertureConstruction);
            Add(apertureConstruction);
        }

        private void Button_Remove_Click(object sender, RoutedEventArgs e)
        {
            foreach (Row row in DataGrid_Constructions.SelectedItems.Cast<Row>().ToList())
            {
                rows.Remove(row);
                apertureConstructionLibrary?.Remove(row.ApertureConstruction);
            }
        }

        private void Button_Duplicate_Click(object sender, RoutedEventArgs e)
        {
            Row selectedRow = DataGrid_Constructions.SelectedItems.Cast<Row>().FirstOrDefault();
            ApertureConstruction apertureConstruction = selectedRow?.ApertureConstruction;
            if (apertureConstruction == null)
            {
                return;
            }

            string name = (string.IsNullOrWhiteSpace(apertureConstruction.Name) ? string.Empty : apertureConstruction.Name).Trim();
            string name_Temp = name;
            int index = 1;
            while (apertureConstructionLibrary?.GetApertureConstructions()?.Find(x => x.Name == name_Temp) != null)
            {
                name_Temp = string.Format("{0} {1}", name, index.ToString());
                index++;
            }
            name = name_Temp;

            apertureConstruction = new ApertureConstruction(Guid.NewGuid(), apertureConstruction, name);
            ApertureConstructionWindow apertureConstructionWindow = new ApertureConstructionWindow(materialLibrary, apertureConstructionLibrary, apertureConstruction) { Owner = this };
            if (apertureConstructionWindow.ShowDialog() != true)
            {
                return;
            }

            apertureConstruction = apertureConstructionWindow.ApertureConstruction;
            if (apertureConstruction == null)
            {
                return;
            }

            apertureConstructionLibrary?.Add(apertureConstruction);
            Add(apertureConstruction);
        }

        private void DataGrid_Constructions_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            Button_SetGlazing.IsEnabled = DataGrid_Constructions.SelectedItems.Count == 1;
        }

        private void Button_SetGlazing_Click(object sender, RoutedEventArgs e)
        {
            ApertureConstruction apertureConstruction = GetApertureConstructions(true)?.FirstOrDefault();
            if (apertureConstruction == null || DataGrid_Constructions.SelectedItems.Count != 1)
            {
                return;
            }

            setGlazingRequested?.Invoke(this, new SetGlazingRequestedEventArgs(apertureConstruction));
        }

        private void DataGrid_Constructions_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Row row = DataGrid_Constructions.SelectedItems.Cast<Row>().FirstOrDefault();
            ApertureConstruction apertureConstruction = row?.ApertureConstruction;
            if (apertureConstruction == null)
            {
                return;
            }

            ApertureConstructionWindow apertureConstructionWindow = new ApertureConstructionWindow(materialLibrary, apertureConstructionLibrary, apertureConstruction)
            {
                Owner = this,
                Enabled = Button_Add.Visibility == Visibility.Visible
            };
            if (apertureConstructionWindow.ShowDialog() != true)
            {
                return;
            }

            apertureConstruction = apertureConstructionWindow.ApertureConstruction;
            row.Update(apertureConstruction);
        }

        private void MenuItem_ChangeType_Click(object sender, RoutedEventArgs e)
        {
            List<Row> selected = DataGrid_Constructions.SelectedItems.Cast<Row>().ToList();
            if (selected.Count == 0)
            {
                return;
            }

            HashSet<ApertureType> apertureTypes = new HashSet<ApertureType>();
            foreach (Row row in selected)
            {
                ApertureConstruction apertureConstruction = row.ApertureConstruction;
                if (apertureConstruction == null)
                {
                    continue;
                }

                apertureTypes.Add(apertureConstruction.ApertureType);
                if (apertureTypes.Count > 1)
                {
                    break;
                }
            }

            ApertureType apertureType = apertureTypes.Count > 1 ? ApertureType.Undefined : apertureTypes.First();
            ComboBoxWindow<ApertureType> comboBoxWindow = new ComboBoxWindow<ApertureType>("Aperture Type", Enum.GetValues(typeof(ApertureType)).Cast<ApertureType>(), x => x == ApertureType.Undefined ? string.Empty : Core.Query.Description(x))
            {
                Owner = this,
                SelectedItem = apertureType
            };
            if (comboBoxWindow.ShowDialog() != true)
            {
                return;
            }

            apertureType = comboBoxWindow.SelectedItem;

            foreach (Row row in selected)
            {
                ApertureConstruction apertureConstruction = row.ApertureConstruction;
                if (apertureConstruction == null)
                {
                    continue;
                }

                apertureConstruction = new ApertureConstruction(apertureConstruction, apertureType);
                row.Update(apertureConstruction);
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
                Func<IJSAMObject, bool> func = x => x is Material || x is ApertureConstruction;
                constructionManager = Query.ImportConstructionManager(func, new ImportOptions(), this);
            }

            IEnumerable<ApertureConstruction> apertureConstructions = constructionManager?.ApertureConstructions;
            if (apertureConstructions == null || apertureConstructions.Count() == 0)
            {
                MessageBox.Show("Constructions could not be imported.");
                return;
            }

            if (materialLibrary == null)
            {
                materialLibrary = new MaterialLibrary("MaterialLibrary");
            }

            constructionManager.Materials?.ForEach(x => materialLibrary.Add(x));

            if (apertureConstructionLibrary == null)
            {
                apertureConstructionLibrary = new ApertureConstructionLibrary("ApertureConstructionLibrary");
            }

            foreach (ApertureConstruction apertureConstruction in apertureConstructions)
            {
                apertureConstructionLibrary.Add(apertureConstruction);
            }

            SetApertureConstructionLibrary(apertureConstructionLibrary);
        }

        private void Button_Export_Click(object sender, RoutedEventArgs e)
        {
            List<ApertureConstruction> apertureConstructions = GetApertureConstructions(false);
            if (apertureConstructions == null || apertureConstructions.Count == 0)
            {
                return;
            }

            ApertureConstructionLibrary apertureConstructionLibrary = new ApertureConstructionLibrary("ApertureConstructionLibrary");
            apertureConstructions.ForEach(x => apertureConstructionLibrary.Add(x));

            MaterialLibrary materialLibrary_Temp = materialLibrary == null ? null : new MaterialLibrary(materialLibrary);

            ConstructionManager constructionManager = new ConstructionManager(apertureConstructionLibrary, null, materialLibrary_Temp);

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
                FileName = (materialLibrary == null || materialLibrary.GetMaterials() == null) ? "SAM_ApertureConstructionLibrary_CustomVer00.json" : "SAM_ConstructionManager_CustomVer00.json"
            };

            if (saveFileDialog.ShowDialog(this) != true)
            {
                return;
            }

            bool result;
            if (materialLibrary == null || materialLibrary.GetMaterials() == null)
            {
                result = Core.Convert.ToFile(apertureConstructionLibrary, saveFileDialog.FileName);
            }
            else
            {
                result = Core.Convert.ToFile(constructionManager, saveFileDialog.FileName);
            }

            MessageBox.Show(result ? "Data exported successfully." : "Data could not be exported.");
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            ApertureConstructionLibrary.JsonForm(this, e);
        }

        private sealed class Row : INotifyPropertyChanged
        {
            private string name;
            private string description;
            private double thickness;
            private string type;

            public Row(ApertureConstruction apertureConstruction)
            {
                Update(apertureConstruction);
            }

            public event PropertyChangedEventHandler PropertyChanged;

            public ApertureConstruction ApertureConstruction { get; private set; }

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

            public void Update(ApertureConstruction apertureConstruction)
            {
                ApertureConstruction = apertureConstruction;
                if (apertureConstruction == null)
                {
                    return;
                }

                Name = apertureConstruction.Name;
                Thickness = Math.Round(apertureConstruction.MaxThickness(), 3);

                Description = apertureConstruction.TryGetValue(ApertureConstructionParameter.Description, out string description_Temp) ? description_Temp : null;

                Type = Core.Query.Description(apertureConstruction.ApertureType);
            }

            private void Raise(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
