using SAM.Analytical.Classes;
using SAM.Core;
using SAM.Core.UI;
using SAM.Core.UI.WPF;
using SAM.Core.Windows.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for CreateCaseByApertureConstructionControl.xaml
    /// </summary>
    public partial class CreateCaseByApertureConstructionControl : UserControl
    {
        private AnalyticalModel analyticalModel;
        private CaseSelection caseSelection;

        public CreateCaseByApertureConstructionControl()
        {
            InitializeComponent();

            Add();
        }

        public AnalyticalModel? AnalyticalModel
        {
            get
            {
                return analyticalModel;
            }

            set
            {
                analyticalModel = value;
            }
        }

        public IEnumerable<ApertureConstructionCase>? ApertureConstructionCases
        {
            get
            {
                List<ApertureConstructionCase> result = [];
                foreach (object children in StackPanel_Main.Children)
                {
                    if (children is not SelectSAMObjectComboBoxControl selectSAMObjectComboBoxControl)
                    {
                        continue;
                    }

                    if (selectSAMObjectComboBoxControl.GetJSAMObject<ApertureConstruction>() is ApertureConstruction apertureConstruction)
                    {
                        result.Add(new ApertureConstructionCase(apertureConstruction, caseSelection));
                    }
                }

                return result;
            }

            set
            {
                StackPanel_Main.Children.Clear();

                if (value == null)
                {
                    return;
                }

                foreach (ApertureConstructionCase apertureConstructionCase in value)
                {
                    if (apertureConstructionCase.ApertureConstruction is not ApertureConstruction apertureConstruction)
                    {
                        continue;
                    }

                    SelectSAMObjectComboBoxControl selectSAMObjectComboBoxControl = Add();
                    if (selectSAMObjectComboBoxControl is null)
                    {
                        continue;
                    }

                    selectSAMObjectComboBoxControl.Add(apertureConstructionCase.ApertureConstruction?.Name, apertureConstructionCase.ApertureConstruction);
                    selectSAMObjectComboBoxControl.SelectedText = apertureConstructionCase.ApertureConstruction?.Name;

                    caseSelection = apertureConstructionCase.CaseSelection;
                }
            }
        }

        private SelectSAMObjectComboBoxControl Add()
        {
            SelectSAMObjectComboBoxControl result = new()
            {
                Margin = new Thickness(0, 5, 0, 5),
                Width = 200
            };

            result.ValidateFunc = new Func<IJSAMObject, bool>(x => x is ApertureConstruction);
            result.ReadFunc = new Func<string, IJSAMObject>(x =>
            {
                if(string.IsNullOrWhiteSpace(x))
                {
                    return null;
                }

                string json = System.IO.File.ReadAllText(x);
                if(string.IsNullOrWhiteSpace(json))
                {
                    return null;
                }

                List<IJSAMObject> jSAMObjects = Core.Create.IJSAMObjects<IJSAMObject>(json);
                if(jSAMObjects == null || !jSAMObjects.Any())
                {
                    return null;
                }

                List<ApertureConstruction> apertureConstructions = [];
                foreach (IJSAMObject jSAMObject in jSAMObjects)
                {
                    if(jSAMObject is ApertureConstruction apertureConstruction)
                    {
                        apertureConstructions.Add(apertureConstruction);
                    }
                    else if(jSAMObject is AnalyticalModel analyticalModel)
                    {
                        analyticalModel.AdjacencyCluster?.ApertureConstructions()?.ForEach(x => apertureConstructions.Add(x));
                    }
                    else if(jSAMObject is ApertureConstructionLibrary apertureConstructionLibrary)
                    {
                        apertureConstructionLibrary.GetApertureConstructions()?.ForEach(x => apertureConstructions.Add(x));

                    }
                }

                if(apertureConstructions.Count == 0)
                {
                    return null;
                }

                if(apertureConstructions.Count == 1)
                {
                    return apertureConstructions[0];
                }

                SearchForm<ApertureConstruction> searchForm = new SearchForm<ApertureConstruction>("Aperture Constructions", apertureConstructions, x => x?.Name, false);
                if(searchForm.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                {
                    return null;
                }

                return searchForm.SelectedItems?.FirstOrDefault();

            });

            result.SelectionChanged += SelectSAMObjectComboBoxControl_SelectionChanged;

            StackPanel_Main.Children.Add(result);

            return result;
        }

        private void button_Selection_Click(object sender, RoutedEventArgs e)
        {
            AdjacencyCluster? adjacencyCluster = analyticalModel?.AdjacencyCluster;
            if (adjacencyCluster == null)
            {
                return;
            }

            List<ApertureConstructionCase>? apertureConstructionCases = ApertureConstructionCases?.ToList();

            IUIFilter? uIFilter = null;
            if (apertureConstructionCases != null && apertureConstructionCases.Count != 0)
            {
                List<FilterSelection>? filterSelections = apertureConstructionCases.ConvertAll(x => x.CaseSelection as FilterSelection)?.Where(x => x != null)?.ToList();
                if (filterSelections != null && filterSelections.Count != 0)
                {
                    uIFilter = filterSelections.Find(x => x.Filter is IUIFilter)?.Filter as IUIFilter;
                }
            }

            List<IJSAMObject>? jSAMObjects = adjacencyCluster?.GetApertures()?.ConvertAll(x => x as IJSAMObject);

            FilterWindow filterWindow = new FilterWindow() { Types = [typeof(Aperture)], Type = typeof(Aperture), UIFilter = uIFilter, UIFilters = null, JSAMObjects = jSAMObjects, AdjacencyCluster = adjacencyCluster };
            filterWindow.FilterAdding += FilterWindow_FilterAdding;
            bool? result = filterWindow.ShowDialog();
            if (result == null || !result.HasValue || !result.Value)
            {
                return;
            }

            caseSelection = new FilterSelection(filterWindow.UIFilter);
        }

        private void FilterWindow_FilterAdding(object sender, FilterAddingEventArgs e)
        {
            e.Handled = true;

            Type type = e.Type;
            List<IUIFilter> uIFilters = UI.Query.IUIFilters(type, analyticalModel?.AdjacencyCluster);
            if (uIFilters == null || uIFilters.Count == 0)
            {
                return;
            }

            using (SearchForm<IUIFilter> searchForm = new SearchForm<IUIFilter>("Select Filter", uIFilters, x => x.Name))
            {
                searchForm.SelectionMode = System.Windows.Forms.SelectionMode.One;
                if (searchForm.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                {
                    return;
                }

                e.UIFilter = searchForm.SelectedItems.FirstOrDefault();
            }
        }

        private void SelectSAMObjectComboBoxControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Core.UI.WPF.SelectSAMObjectComboBoxControl? selectSAMObjectComboBoxControl = sender as SelectSAMObjectComboBoxControl;
            if (selectSAMObjectComboBoxControl == null)
            {
                return;
            }

            // Check if this is the last ComboBox in the panel
            if (StackPanel_Main.Children[StackPanel_Main.Children.Count - 1] == selectSAMObjectComboBoxControl)
            {
                // Only add if a value is selected
                if (selectSAMObjectComboBoxControl.GetJSAMObject<ApertureConstruction>() != null)
                {
                    Add();
                }
            }
        }
    }
}
