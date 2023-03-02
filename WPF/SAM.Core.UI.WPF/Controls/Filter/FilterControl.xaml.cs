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

        private void Button_Click(object sender, RoutedEventArgs e)
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
            Modify.AddFilterControl(grid_Filter, uIFilter);
        }
    }
}
