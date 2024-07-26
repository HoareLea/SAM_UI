using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SAM.Core.UI.WPF
{
    /// <summary>
    /// Interaction logic for FilterControl.xaml
    /// </summary>
    public partial class FilterControl : UserControl
    {
        public event FilterAddingEventHandler FilterAdding;

        public event FilterChangedEventHandler FilterChanged;

        public event FilterRemovingEventHandler FilterRemoving;

        public FilterControl()
        {
            InitializeComponent();

            comboBox_Type.SelectionChanged += ComboBox_Type_SelectionChanged;
        }

        private void ComboBox_Type_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            grid_Filter.Children.Clear();
            grid_Filter.RowDefinitions.Clear();
        }

        public List<Type> Types
        {
            get
            {
                return GetTypes();
            }

            set
            {
                SetTypes(value);
            }
        }

        public Type Type
        {
            get
            {
                return (comboBox_Type.SelectedItem as ComboBoxItem)?.Tag as Type;
            }

            set
            {
                foreach(ComboBoxItem comboBoxItem in comboBox_Type.Items)
                {
                    if(comboBoxItem?.Tag as Type == value)
                    {
                        comboBox_Type.SelectedItem = comboBoxItem;
                        break;
                    }
                }
            }
        }

        public IUIFilter UIFilter
        {
            get
            {
                return GetUIFilter();
            }

            set
            {
                SetUIFilter(value);
            }
        }

        public IUIFilter GetUIFilter(string name = null)
        {
            List<IUIFilter> uIFilters = new List<IUIFilter>();

            IUIFilter uIFilter = null;

            uIFilter = Query.FilterControls(grid_Filter)?.FirstOrDefault()?.UIFilter;
            if (uIFilter != null)
            {
                uIFilters.Add(uIFilter);
            }

            Type type = Type;
            if (type != null)
            {
                uIFilter = new UITypeFilter(name, type);
                uIFilters.Add(uIFilter);
            }

            if (uIFilters.Count == 0)
            {
                return null;
            }

            if (uIFilters.Count == 1)
            {
                return uIFilters.FirstOrDefault();
            }

            return new UILogicalFilter(name, uIFilter.Type, new LogicalFilter(FilterLogicalOperator.And, uIFilters));
        }

        private void SetUIFilter(IUIFilter uIFilter)
        {
            grid_Filter.Children.Clear();
            grid_Filter.RowDefinitions.Clear();

            if(uIFilter == null)
            {
                return;
            }

            if (uIFilter is UITypeFilter)
            {
                Type = ((UITypeFilter)uIFilter).Type;
                return;
            }

            if (uIFilter is UILogicalFilter)
            {
                UILogicalFilter uILogicalFilter = (UILogicalFilter)uIFilter;
                if(uILogicalFilter.Filter?.Filters != null && uILogicalFilter.Filter.FilterLogicalOperator == FilterLogicalOperator.And && uILogicalFilter.Filter?.Filters.Count == 2)
                {
                    List<IFilter> filters = new List<IFilter>(uILogicalFilter.Filter?.Filters);

                    UITypeFilter uITypeFilter = filters.Find(x => x is UITypeFilter) as UITypeFilter;
                    if(uITypeFilter != null)
                    {
                        filters.Remove(uITypeFilter);

                        Type = uITypeFilter.Type;
                        Add(filters.FirstOrDefault() as IUIFilter);
                        return;
                    }
                }
            }

            Add(uIFilter);
        }

        private void Button_Add_Click(object sender, RoutedEventArgs e)
        {
            FilterAddingEventArgs filterAddingEventArgs = new FilterAddingEventArgs(Type);

            FilterAdding?.Invoke(this, filterAddingEventArgs);

            if(!filterAddingEventArgs.Handled)
            {
                List<IUIFilter> uIFilters = UI.Query.IUIFilters(filterAddingEventArgs.Type);
                if (uIFilters == null || uIFilters.Count == 0)
                {
                    return;
                }

                using (Windows.Forms.SearchForm<IUIFilter> searchForm = new Windows.Forms.SearchForm<IUIFilter>("Select Filter", uIFilters, x => x.Name))
                {
                    searchForm.SelectionMode = System.Windows.Forms.SelectionMode.One;
                    if (searchForm.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                    {
                        return;
                    }

                    filterAddingEventArgs.UIFilter = searchForm.SelectedItems.FirstOrDefault();
                }
            }

            Add(filterAddingEventArgs.UIFilter);
        }

        private void Button_Clear_Click(object sender, RoutedEventArgs e)
        {
            foreach (IFilterControl filterControl in Query.FilterControls(grid_Filter))
            {
                FilterRemoving?.Invoke(this, new FilterRemovingEventArgs(filterControl));
            }

            grid_Filter.Children.Clear();
            grid_Filter.RowDefinitions.Clear();
        }

        private List<Type> GetTypes()
        {
            List<Type> result = new List<Type>();
            foreach(ComboBoxItem comboBoxItem in comboBox_Type.Items)
            {
                if(comboBoxItem?.Tag is Type)
                {
                    result.Add((Type)comboBoxItem.Tag);
                }
            }

            return result;
        }

        private void SetTypes(IEnumerable<Type> types)
        {
            ComboBoxItem comboBoxItem = comboBox_Type.SelectedItem as ComboBoxItem;
            comboBox_Type.Items.Clear();

            if(types == null || types.Count() == 0)
            {
                return;
            }

            foreach(Type type in types)
            {
                ComboBoxItem comboBoxItem_New = new ComboBoxItem() { Content = type.Name, Tag = type };
                comboBox_Type.Items.Add(comboBoxItem_New);

                if(comboBoxItem != null)
                {
                    if(comboBoxItem.Tag == comboBoxItem_New.Tag)
                    {
                        comboBoxItem = comboBoxItem_New;
                    }
                }
            }

            comboBox_Type.SelectedItem = comboBoxItem;
        }

        private void Add(IUIFilter uIFilter)
        {
            IFilterControl filterControl = Modify.AddFilterControl(grid_Filter, uIFilter);
            if(filterControl != null)
            {
                filterControl.FilterChanged += FilterControl_FilterChanged;
                filterControl.FilterRemoving += FilterControl_FilterRemoving;
                FilterChanged?.Invoke(this, new FilterChangedEventArgs(UIFilter));
            }

        }

        private void FilterControl_FilterRemoving(object sender, FilterRemovingEventArgs e)
        {
            grid_Filter.Children.Remove(e.FilterControl as UIElement);

            FilterRemoving?.Invoke(this, new FilterRemovingEventArgs(e.FilterControl));
        }

        private void FilterControl_FilterChanged(object sender, FilterChangedEventArgs e)
        {
            FilterChanged?.Invoke(this, new FilterChangedEventArgs(e.UIFilter));
        }
    }
}
