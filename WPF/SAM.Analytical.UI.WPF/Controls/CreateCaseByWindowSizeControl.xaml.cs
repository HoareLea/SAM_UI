// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Classes;
using SAM.Core;
using SAM.Core.UI;
using SAM.Core.UI.WPF;
using SAM.Core.Windows.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for CreateCaseByWindowSize.xaml
    /// </summary>
    public partial class CreateCaseByWindowSizeControl : UserControl
    {
        private AnalyticalModel analyticalModel;

        public CreateCaseByWindowSizeControl()
        {
            InitializeComponent();

            DataContext = new CreateCaseViewModel<WindowSizeCase>();
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

        public IEnumerable<WindowSizeCase>? WindowSizeCases
        {
            get
            {
                if (DataContext is not CreateCaseViewModel<WindowSizeCase> createCaseViewModel)
                {
                    return null;
                }

                List<WindowSizeCase> result = [];
                foreach(WindowSizeCase windowSizeCase in createCaseViewModel.Items)
                {
                    result.Add(windowSizeCase);
                }

                return result;
            }

            set
            {
                if (DataContext is not CreateCaseViewModel<WindowSizeCase> createCaseViewModel)
                {
                    return;
                }

                createCaseViewModel.Items.Clear();

                if (value == null)
                {
                    return;
                }

                foreach(WindowSizeCase windowSizeCase in value)
                {
                    createCaseViewModel.Items.Add(windowSizeCase);
                }
            }
        }
        
        private void button_Selection_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            AdjacencyCluster? adjacencyCluster = analyticalModel?.AdjacencyCluster;
            if (adjacencyCluster == null)
            {
                return;
            }

            List<WindowSizeCase>? windowSizeCases = WindowSizeCases?.ToList();

            IUIFilter? uIFilter = null;
            if(windowSizeCases != null && windowSizeCases.Count != 0)
            {
                List<FilterSelection>? filterSelections = windowSizeCases.ConvertAll(x => x.CaseSelection as FilterSelection)?.Where(x => x != null)?.ToList();
                if(filterSelections != null && filterSelections.Count != 0)
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

            foreach(WindowSizeCase windowSizeCase in windowSizeCases)
            {
                windowSizeCase.CaseSelection = new FilterSelection(filterWindow.UIFilter);
            }

            WindowSizeCases = windowSizeCases;
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
