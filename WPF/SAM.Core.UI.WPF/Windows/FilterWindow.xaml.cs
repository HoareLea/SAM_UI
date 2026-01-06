using System;
using System.Collections.Generic;
using System.Windows;

namespace SAM.Core.UI.WPF
{
    /// <summary>
    /// Interaction logic for FilterWindow.xaml
    /// </summary>
    public partial class FilterWindow : Window
    {
        public event FilterAddingEventHandler FilterAdding;

        public event FilterChangedEventHandler FilterChanged;

        public FilterWindow()
        {
            InitializeComponent();

            filterControl.FilterAdding += FilterControl_FilterAdding;
            filterControl.FilterChanged += FilterControl_FilterChanged;
            
            filtersControl.SelectionChanged += FiltersControl_SelectionChanged;
            filtersControl.FilterAdding += FiltersControl_FilterAdding;

        }

        private void FilterControl_FilterChanged(object sender, FilterChangedEventArgs e)
        {
            FilterChanged?.Invoke(this, new FilterChangedEventArgs(e.UIFilter));
        }

        private void FiltersControl_FilterAdding(object sender, FilterAddingEventArgs e)
        {
            e.Handled = true;

            using (Windows.Forms.TextBoxForm<string> textBoxForm = new Windows.Forms.TextBoxForm<string>("Filter", "Filter Name"))
            {
                if (textBoxForm.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                {
                    return;
                }

                string name = textBoxForm.Value;

                if(string.IsNullOrWhiteSpace(name))
                {
                    return;
                }

                e.UIFilter = filterControl.GetUIFilter(name);
            }
        }

        private void FiltersControl_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            filterControl.UIFilter = filtersControl.SelectedUIFilter;
        }

        private void FilterControl_FilterAdding(object sender, FilterAddingEventArgs e)
        {
            FilterAdding?.Invoke(this, e);
        }

        public List<Type> Types
        {
            get
            {
                return filterControl.Types;
            }

            set
            {
                filterControl.Types = value;
            }
        }

        public IUIFilter UIFilter
        {
            get
            {
                return filterControl.UIFilter;
            }

            set
            {
                filterControl.UIFilter = value;
                filtersControl.SelectedUIFilter = null;
            }
        }

        public List<IUIFilter> UIFilters
        {
            get
            {
                return filtersControl.UIFilters;
            }

            set
            {
                filtersControl.UIFilters = value;
            }
        }

        public IUIFilter SelectedUIFilters
        {
            get
            {
                return filtersControl.SelectedUIFilter;
            }

            set
            {
                filtersControl.SelectedUIFilter = value;
            }
        }

        public Type Type
        {
            get
            {
                return filterControl.Type;
            }

            set
            {
                filterControl.Type = value;
            }
        }

        private void button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void button_OK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
