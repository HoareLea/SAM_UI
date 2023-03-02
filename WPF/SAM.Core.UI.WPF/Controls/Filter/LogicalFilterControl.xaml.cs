using System.Collections.Generic;
using System.Windows.Controls;

namespace SAM.Core.UI.WPF
{
    /// <summary>
    /// Interaction logic for LogicalFilterControl.xaml
    /// </summary>
    public partial class LogicalFilterControl : UserControl, IFilterControl
    {
        private UILogicalFilter uILogicalFilter;

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

            return new UILogicalFilter(uILogicalFilter.Name, uILogicalFilter.Type, new LogicalFilter(uILogicalFilter.Filter.FilterLogicalOperator, filterControls?.ConvertAll(x => x.UIFilter)));
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
                Modify.AddFilterControl(grid_Filters, uIFilter, false);
            }
        }

        public void Add(IUIFilter uIFilter)
        {
            if(uIFilter == null)
            {
                return;
            }

            Modify.AddFilterControl(grid_Filters, uIFilter, false);
        }
    }
}
