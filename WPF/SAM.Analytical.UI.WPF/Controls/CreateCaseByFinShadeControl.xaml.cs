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
using System.Windows;
using System.Windows.Controls;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for CreateCaseByFinShadeControl.xaml
    /// </summary>
    public partial class CreateCaseByFinShadeControl : UserControl
    {
        private AnalyticalModel analyticalModel;

        public CreateCaseByFinShadeControl()
        {
            InitializeComponent();

            DataContext = new CreateCaseViewModel<FinShadeCase>();
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

        public IEnumerable<FinShadeCase>? FinShadeCases
        {
            get
            {
                if (DataContext is not CreateCaseViewModel<FinShadeCase> createCaseViewModel)
                {
                    return null;
                }

                List<FinShadeCase> result = [];
                foreach (FinShadeCase finShadeCase in createCaseViewModel.Items)
                {
                    result.Add(finShadeCase);
                }

                return result;
            }

            set
            {
                if (DataContext is not CreateCaseViewModel<FinShadeCase> createCaseViewModel)
                {
                    return;
                }

                createCaseViewModel.Items.Clear();

                if (value == null)
                {
                    return;
                }

                foreach (FinShadeCase finShadeCase in value)
                {
                    createCaseViewModel.Items.Add(finShadeCase);
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

            List<FinShadeCase>? finShadeCases = FinShadeCases?.ToList();

            IUIFilter? uIFilter = null;
            if (finShadeCases != null && finShadeCases.Count != 0)
            {
                List<FilterSelection>? filterSelections = finShadeCases.ConvertAll(x => x.CaseSelection as FilterSelection)?.Where(x => x != null)?.ToList();
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

            foreach (FinShadeCase finShadeCase in finShadeCases)
            {
                finShadeCase.CaseSelection = new FilterSelection(filterWindow.UIFilter);
            }

            FinShadeCases = finShadeCases;
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
