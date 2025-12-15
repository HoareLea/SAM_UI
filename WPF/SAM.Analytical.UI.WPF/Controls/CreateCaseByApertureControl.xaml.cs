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
    /// Interaction logic for CreateCaseByApertureControl.xaml
    /// </summary>
    public partial class CreateCaseByApertureControl : UserControl
    {
        private AnalyticalModel analyticalModel;

        public CreateCaseByApertureControl()
        {
            InitializeComponent();
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

        public IEnumerable<ApertureCase>? ApertureCases
        {
            get
            {
                if (DataContext is not CreateCaseViewModel<ApertureCase> createCaseViewModel)
                {
                    return null;
                }

                List<ApertureCase> result = [];
                foreach (ApertureCase apertureCase in createCaseViewModel.Items)
                {
                    result.Add(apertureCase);
                }

                return result;
            }

            set
            {
                if (DataContext is not CreateCaseViewModel<ApertureCase> createCaseViewModel)
                {
                    return;
                }

                createCaseViewModel.Items.Clear();

                if (value == null)
                {
                    return;
                }

                foreach (ApertureCase apertureCase in value)
                {
                    createCaseViewModel.Items.Add(apertureCase);
                }
            }
        }

        private void button_Selection_Click(object sender, RoutedEventArgs e)
        {
            AdjacencyCluster? adjacencyCluster = analyticalModel?.AdjacencyCluster;
            if (adjacencyCluster == null)
            {
                return;
            }

            List<ApertureCase>? apertureCases = ApertureCases?.ToList();

            IUIFilter? uIFilter = null;
            if (apertureCases != null && apertureCases.Count != 0)
            {
                List<FilterSelection>? filterSelections = apertureCases.ConvertAll(x => x.CaseSelection as FilterSelection)?.Where(x => x != null)?.ToList();
                if (filterSelections != null && filterSelections.Count != 0)
                {
                    uIFilter = filterSelections.Find(x => x.Filter is IUIFilter)?.Filter as IUIFilter;
                }
            }

            List<IJSAMObject>? jSAMObjects = adjacencyCluster?.GetPanels()?.ConvertAll(x => x as IJSAMObject);

            FilterWindow filterWindow = new FilterWindow() { Types = [typeof(Panel)], Type = typeof(Panel), UIFilter = uIFilter, UIFilters = null, JSAMObjects = jSAMObjects, AdjacencyCluster = adjacencyCluster };
            filterWindow.FilterAdding += FilterWindow_FilterAdding;
            bool? result = filterWindow.ShowDialog();
            if (result == null || !result.HasValue || !result.Value)
            {
                return;
            }

            foreach (ApertureCase apertureCase in apertureCases)
            {
                apertureCase.CaseSelection = new FilterSelection(filterWindow.UIFilter);
            }

            ApertureCases = apertureCases;
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
    }
}
