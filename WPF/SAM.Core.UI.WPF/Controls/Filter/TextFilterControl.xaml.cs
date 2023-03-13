using System;
using System.Windows.Controls;

namespace SAM.Core.UI.WPF
{
    /// <summary>
    /// Interaction logic for TextFilterControl.xaml
    /// </summary>
    public partial class TextFilterControl : UserControl, IFilterControl
    {
        private UITextFilter uITextFilter;

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

            TextFilter textFilter = result.Filter;
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

            TextFilter textFilter = uITextFilter.Filter;
            if (textFilter == null)
            {
                return;
            }

            checkBox_Inverted.IsChecked = textFilter.Inverted;
            checkBox_CaseSensitive.IsChecked = textFilter.CaseSensitive;
            textBox_Value.Text = textFilter.Value;
            comboBox_TextComparisonType.SelectedItem = Core.Query.Description(textFilter.TextComparisonType);
        }



    }
}
