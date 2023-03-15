using System.Windows.Controls;

namespace SAM.Core.UI.WPF
{
    /// <summary>
    /// Interaction logic for NumberFilterControl.xaml
    /// </summary>
    public partial class NumberFilterControl : UserControl, IFilterControl
    {
        private UINumberFilter uINumberFilter;

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

            NumberFilter numberFilter = result.Filter;
            if(numberFilter == null)
            {
                return null;
            }

            numberFilter.Inverted = checkBox_Inverted.IsChecked != null && checkBox_Inverted.IsChecked.HasValue && checkBox_Inverted.IsChecked.Value;

            if(Core.Query.TryConvert(textBox_Value.Text, out double value))
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

            NumberFilter textFilter = uINumberFilter.Filter;
            if (textFilter == null)
            {
                return;
            }

            checkBox_Inverted.IsChecked = textFilter.Inverted;
            textBox_Value.Text = textFilter.Value.ToString();
            comboBox_NumberComparisonType.SelectedItem = Core.Query.Description(textFilter.NumberComparisonType);
        }

        private void textBox_Value_TextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            Windows.EventHandler.ControlText_NumberOnly(sender, e);
        }
    }
}
