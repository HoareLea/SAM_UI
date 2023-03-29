using SAM.Core;
using SAM.Core.UI;
using System;
using System.Collections.Generic;
using System.Windows;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for FilterWindow.xaml
    /// </summary>
    public partial class FilterWindow : System.Windows.Window
    {
        private List<IJSAMObject> jSAMObjects = null;

        public event Core.UI.WPF.FilterAddingEventHandler FilterAdding;

        public FilterWindow()
        {
            InitializeComponent();

            filterControl.FilterAdding += FilterControl_FilterAdding;
            filtersControl.SelectionChanged += FiltersControl_SelectionChanged;
            filtersControl.FilterAdding += FiltersControl_FilterAdding;
        }

        private void FiltersControl_FilterAdding(object sender, Core.UI.WPF.FilterAddingEventArgs e)
        {
            e.Handled = true;

            using (Core.Windows.Forms.TextBoxForm<string> textBoxForm = new Core.Windows.Forms.TextBoxForm<string>("Filter", "Filter Name"))
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

        private void FilterControl_FilterAdding(object sender, Core.UI.WPF.FilterAddingEventArgs e)
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

        public List<IJSAMObject> JSAMObjects
        {
            get
            {
                return jSAMObjects;
            }

            set
            {
                jSAMObjects = value;
            }
        }

        public List<IJSAMObject> FilteredJSAMObjects
        {
            get
            {
                return null;
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
