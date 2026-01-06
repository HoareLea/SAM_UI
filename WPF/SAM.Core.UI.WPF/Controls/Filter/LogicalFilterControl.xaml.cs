// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace SAM.Core.UI.WPF
{
    /// <summary>
    /// Interaction logic for LogicalFilterControl.xaml
    /// </summary>
    public partial class LogicalFilterControl : UserControl, IFilterControl
    {
        private UILogicalFilter uILogicalFilter;

        public event FilterChangedEventHandler FilterChanged;

        public event FilterRemovingEventHandler FilterRemoving;

        public LogicalFilterControl()
        {
            InitializeComponent();

            Load();
        }

        public LogicalFilterControl(UILogicalFilter uILogicalFilter)
        {
            InitializeComponent();

            Load();

            UILogicalFilter = uILogicalFilter;
        }

        private void Load()
        {
            Modify.Reload<FilterLogicalOperator>(comboBox_FilterLogicalOperator);
        }

        public FilterLogicalOperator FilterLogicalOperator
        {
            get
            {
                return Core.Query.Enum<FilterLogicalOperator>(comboBox_FilterLogicalOperator.SelectedItem?.ToString());
            }

            set
            {
                comboBox_FilterLogicalOperator.SelectedItem = Core.Query.Description(value);
            }
        }

        public UILogicalFilter UILogicalFilter
        {
            get
            {
                return GetUILogicalFilter();
            }

            set
            {
                SetUILogicalFilter(value);
            }
        }

        public IUIFilter UIFilter
        {
            get
            {
                return UILogicalFilter;
            }
        }

        private UILogicalFilter GetUILogicalFilter(bool updated = true)
        {
            if(!updated)
            {
                return uILogicalFilter;
            }

            if(uILogicalFilter == null)
            {
                return null;
            }

            List<IFilterControl> filterControls = Query.FilterControls(grid_Filters);

            FilterLogicalOperator filterLogicalOperator = Core.Query.Enum<FilterLogicalOperator>(comboBox_FilterLogicalOperator.SelectedItem.ToString());

            return new UILogicalFilter(uILogicalFilter.Name, uILogicalFilter.Type, new LogicalFilter(filterLogicalOperator, filterControls?.ConvertAll(x => x.UIFilter)));
        }

        private void SetUILogicalFilter(UILogicalFilter uILogicalFilter)
        {
            this.uILogicalFilter = uILogicalFilter;
            
            grid_Filters.Children.Clear();
            grid_Filters.RowDefinitions.Clear();

            if(uILogicalFilter.Filter != null)
            {
                LogicalFilter logicalFilter = uILogicalFilter.Filter;
                comboBox_FilterLogicalOperator.SelectedItem = Core.Query.Description(logicalFilter.FilterLogicalOperator);
            }

            List<IUIFilter> uIFilters = uILogicalFilter?.Filter?.Filters.FindAll(x => x is IUIFilter).ConvertAll(x => (IUIFilter)x);
            if(uIFilters == null)
            {
                return;
            }

            foreach(IUIFilter uIFilter in uIFilters)
            {
                Add(uIFilter);
            }
        }

        public void Add(IUIFilter uIFilter)
        {
            if(uIFilter == null)
            {
                return;
            }

            IFilterControl filterControl = Modify.AddFilterControl(grid_Filters, uIFilter, false);
            if(filterControl != null)
            {
                filterControl.FilterChanged += FilterControl_FilterChanged;
                filterControl.FilterRemoving += FilterControl_FilterRemoving;
                FilterChanged?.Invoke(this, new FilterChangedEventArgs(UIFilter));
            }
        }

        private void FilterControl_FilterRemoving(object sender, FilterRemovingEventArgs e)
        {
            grid_Filters.Children.Remove(e.FilterControl as UIElement);
            if(grid_Filters.Children.Count == 0)
            {
                FilterRemoving?.Invoke(this, new FilterRemovingEventArgs(this));
            }
        }

        private void FilterControl_FilterChanged(object sender, FilterChangedEventArgs e)
        {
            FilterChanged?.Invoke(this, new FilterChangedEventArgs(e.UIFilter));
        }

        private void comboBox_FilterLogicalOperator_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterChanged?.Invoke(this, new FilterChangedEventArgs(UIFilter));
        }

        private void Grid_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            ContextMenu = new ContextMenu();

            MenuItem menuItem = null;

            menuItem = new MenuItem();
            menuItem.Name = "MenuItem_Remove";
            menuItem.Header = "Remove";
            menuItem.Click += MenuItem_Remove_Click;
            ContextMenu.Items.Add(menuItem);
        }

        private void MenuItem_Remove_Click(object sender, RoutedEventArgs e)
        {
            FilterRemoving?.Invoke(this, new FilterRemovingEventArgs(this));
        }
    }
}
