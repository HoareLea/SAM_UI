using System.Windows;
using System.Windows.Controls;

namespace SAM.Core.UI.WPF
{
    /// <summary>
    /// Interaction logic for TextFilterControl.xaml
    /// </summary>
    public partial class TextFilterControl : UserControl, IFilterControl
    {
        private UITextFilter uITextFilter;

        public event FilterChangedEventHandler FilterChanged;

        public event FilterRemovingEventHandler FilterRemoving;

        public TextFilterControl()
        {
            InitializeComponent();

            Load();
        }

        public TextFilterControl(UITextFilter uITextFilter)
        {
            InitializeComponent();

            Load();

            UITextFilter = uITextFilter;
        }

        public void Load()
        {
            Modify.Reload<TextComparisonType>(comboBox_TextComparisonType);
        }

        public UITextFilter UITextFilter
        {
            get
            {
                return GetUITextFilter();
            }

            set
            {
                SetUITextFilter(value);
            }
        }

        public IUIFilter UIFilter
        {
            get
            {
                return UITextFilter;
            }
        }

        private UITextFilter GetUITextFilter(bool updated = true)
        {
            UITextFilter result = uITextFilter;
            if(result == null)
            {
                return null;
            }

            result = new UITextFilter(result);
            if (!updated)
            {
                return result;
            }

            ITextFilter textFilter = result.Filter;
            if(textFilter == null)
            {
                return null;
            }

            textFilter.Inverted = checkBox_Inverted.IsChecked != null && checkBox_Inverted.IsChecked.HasValue && checkBox_Inverted.IsChecked.Value;
            textFilter.CaseSensitive = checkBox_CaseSensitive.IsChecked != null && checkBox_CaseSensitive.IsChecked.HasValue && checkBox_CaseSensitive.IsChecked.Value;
            textFilter.Value = textBox_Value.Text;
            textFilter.TextComparisonType = Core.Query.Enum<TextComparisonType>(comboBox_TextComparisonType.SelectedItem.ToString());

            return result;
        }

        private void SetUITextFilter(UITextFilter uITextFilter)
        {
            this.uITextFilter = uITextFilter;

            if (uITextFilter == null)
            {
                return;
            }

            groupBox_Main.Header = uITextFilter.Name;

            ITextFilter textFilter = uITextFilter.Filter;
            if (textFilter == null)
            {
                return;
            }

            checkBox_Inverted.IsChecked = textFilter.Inverted;
            checkBox_CaseSensitive.IsChecked = textFilter.CaseSensitive;
            textBox_Value.Text = textFilter.Value;
            comboBox_TextComparisonType.SelectedItem = Core.Query.Description(textFilter.TextComparisonType);
        }

        private void comboBox_TextComparisonType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterChanged?.Invoke(this, new FilterChangedEventArgs(UIFilter));
        }

        private void checkBox_CaseSensitive_Click(object sender, RoutedEventArgs e)
        {
            FilterChanged?.Invoke(this, new FilterChangedEventArgs(UIFilter));
        }

        private void textBox_Value_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
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
    }
}
