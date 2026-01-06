// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SAM.Core.UI.WPF
{
    /// <summary>
    /// Interaction logic for NumberFilterControl.xaml
    /// </summary>
    public partial class NumberFilterControl : UserControl, IFilterControl
    {
        private UINumberFilter uINumberFilter;

        public event FilterChangedEventHandler FilterChanged;

        public event FilterRemovingEventHandler FilterRemoving;

        public NumberFilterControl()
        {
            InitializeComponent();

            Load();
        }

        public NumberFilterControl(UINumberFilter uINumberFilter)
        {
            InitializeComponent();

            Load();

            UINumberFilter = uINumberFilter;
        }

        public void Load()
        {
            Modify.Reload<NumberComparisonType>(comboBox_NumberComparisonType);
        }

        public UINumberFilter UINumberFilter
        {
            get
            {
                return GetUINumberFilter();
            }

            set
            {
                SetUINumberFilter(value);
            }
        }

        public IUIFilter UIFilter
        {
            get
            {
                return UINumberFilter;
            }
        }

        private UINumberFilter GetUINumberFilter(bool updated = true)
        {
            UINumberFilter result = uINumberFilter;
            if(result == null)
            {
                return null;
            }

            result = new UINumberFilter(result);
            if (!updated)
            {
                return result;
            }

            INumberFilter numberFilter = result.Filter;
            if(numberFilter == null)
            {
                return null;
            }

            numberFilter.Inverted = checkBox_Inverted.IsChecked != null && checkBox_Inverted.IsChecked.HasValue && checkBox_Inverted.IsChecked.Value;

            if(Core.Query.TryConvert(comboBox_Value.Text, out double value))
            {
                numberFilter.Value = value;
            }

            numberFilter.NumberComparisonType = Core.Query.Enum<NumberComparisonType>(comboBox_NumberComparisonType.SelectedItem.ToString());

            return result;
        }

        private void SetUINumberFilter(UINumberFilter uINumberFilter)
        {
            this.uINumberFilter = uINumberFilter;

            if (uINumberFilter == null)
            {
                return;
            }

            groupBox_Main.Header = uINumberFilter.Name;

            INumberFilter textFilter = uINumberFilter.Filter;
            if (textFilter == null)
            {
                return;
            }

            checkBox_Inverted.IsChecked = textFilter.Inverted;
            comboBox_Value.Text = textFilter.Value.ToString();
            comboBox_NumberComparisonType.SelectedItem = Core.Query.Description(textFilter.NumberComparisonType);
        }

        private void comboBox_NumberComparisonType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterChanged?.Invoke(this, new FilterChangedEventArgs(UIFilter));
        }

        private void checkBox_Inverted_Click(object sender, RoutedEventArgs e)
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

        public List<string> Values
        {
            get
            {
                List<string> result = new List<string>();
                foreach (object item in comboBox_Value.Items)
                {
                    result.Add(item?.ToString());
                }

                return result;
            }

            set
            {
                comboBox_Value.Items.Clear();
                if (value == null)
                {
                    return;
                }

                List<string> @strings = value.Distinct().ToList();
                strings.Sort();

                foreach (string @string in @strings)
                {
                    comboBox_Value.Items.Add(@string);
                }
            }
        }

        private void comboBox_Value_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            FilterChanged?.Invoke(this, new FilterChangedEventArgs(UIFilter));
        }

        private void comboBox_Value_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterChanged?.Invoke(this, new FilterChangedEventArgs(UIFilter));
        }

        private void comboBox_Value_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Windows.EventHandler.ControlText_NumberOnly(sender, e);
        }
    }
}
