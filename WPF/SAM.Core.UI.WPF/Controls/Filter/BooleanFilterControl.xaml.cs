using System.Windows;
using System.Windows.Controls;

namespace SAM.Core.UI.WPF
{
    /// <summary>
    /// Interaction logic for NumberFilterControl.xaml
    /// </summary>
    public partial class BooleanFilterControl : UserControl, IFilterControl
    {
        private UIBooleanFilter uIBooleanFilter;

        public event FilterChangedEventHandler FilterChanged;

        public event FilterRemovingEventHandler FilterRemoving;

        public BooleanFilterControl()
        {
            InitializeComponent();

            Load();
        }

        public BooleanFilterControl(UIBooleanFilter uIBooleanFilter)
        {
            InitializeComponent();

            Load();

            UIBooleanFilter = uIBooleanFilter;
        }

        public void Load()
        {
            comboBox_Value.Items.Add(true);
            comboBox_Value.Items.Add(false);

            //Modify.Reload<NumberComparisonType>(comboBox_NumberComparisonType);
        }

        public UIBooleanFilter UIBooleanFilter
        {
            get
            {
                return GetUIBooleanFilter();
            }

            set
            {
                SetUIBooleanFilter(value);
            }
        }

        public IUIFilter UIFilter
        {
            get
            {
                return UIBooleanFilter;
            }
        }

        private UIBooleanFilter GetUIBooleanFilter(bool updated = true)
        {
            UIBooleanFilter result = uIBooleanFilter;
            if(result == null)
            {
                return null;
            }

            result = new UIBooleanFilter(result);
            if (!updated)
            {
                return result;
            }

            IBooleanFilter booleanFilter = result.Filter;
            if(booleanFilter == null)
            {
                return null;
            }

            booleanFilter.Inverted = checkBox_Inverted.IsChecked != null && checkBox_Inverted.IsChecked.HasValue && checkBox_Inverted.IsChecked.Value;

            if(Core.Query.TryConvert(comboBox_Value.Text, out bool value))
            {
                booleanFilter.Value = value;
            }

            return result;
        }

        private void SetUIBooleanFilter(UIBooleanFilter uIBooleanFilter)
        {
            this.uIBooleanFilter = uIBooleanFilter;

            if (uIBooleanFilter == null)
            {
                return;
            }

            groupBox_Main.Header = uIBooleanFilter.Name;

            IBooleanFilter textFilter = uIBooleanFilter.Filter;
            if (textFilter == null)
            {
                return;
            }

            checkBox_Inverted.IsChecked = textFilter.Inverted;
            comboBox_Value.Text = textFilter.Value.ToString();
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

        //public List<string> Values
        //{
        //    get
        //    {
        //        List<string> result = new List<string>();
        //        foreach (object item in comboBox_Value.Items)
        //        {
        //            result.Add(item?.ToString());
        //        }

        //        return result;
        //    }

        //    set
        //    {
        //        comboBox_Value.Items.Clear();
        //        if (value == null)
        //        {
        //            return;
        //        }

        //        List<string> @strings = value.Distinct().ToList();
        //        strings.Sort();

        //        foreach (string @string in @strings)
        //        {
        //            comboBox_Value.Items.Add(@string);
        //        }
        //    }
        //}

        private void comboBox_Value_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            FilterChanged?.Invoke(this, new FilterChangedEventArgs(UIFilter));
        }

        private void comboBox_Value_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterChanged?.Invoke(this, new FilterChangedEventArgs(UIFilter));
        }

        //private void comboBox_Value_PreviewTextInput(object sender, TextCompositionEventArgs e)
        //{
        //    Windows.EventHandler.ControlText_NumberOnly(sender, e);
        //}
    }
}
