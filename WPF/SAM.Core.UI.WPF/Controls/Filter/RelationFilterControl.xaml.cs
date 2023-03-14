using System.Linq;
using System.Windows.Controls;

namespace SAM.Core.UI.WPF
{
    /// <summary>
    /// Interaction logic for RelationFilterControl.xaml
    /// </summary>
    public partial class RelationFilterControl : UserControl, IFilterControl
    {
        private UIRelationFilter uIRelationFilter;

        public RelationFilterControl()
        {
            InitializeComponent();
        }

        public RelationFilterControl(UIRelationFilter uIRelationFilter)
        {
            InitializeComponent();

            UIRelationFilter = uIRelationFilter;
        }

        public UIRelationFilter UIRelationFilter
        {
            get
            {
                return GetUIRelationFilter();
            }

            set
            {
                SetUIRelationFilter(value);
            }
        }

        private void SetUIRelationFilter(UIRelationFilter uIRelationFilter)
        {
            this.uIRelationFilter = uIRelationFilter;

            grid_Filters.Children.Clear();
            grid_Filters.RowDefinitions.Clear();

            if(uIRelationFilter == null)
            {
                return;
            }

            //groupBox_Name.Header = uIRelationFilter.Type.Name;

            Modify.AddFilterControl(grid_Filters, uIRelationFilter?.Filter.Filter as IUIFilter);
        }

        private UIRelationFilter GetUIRelationFilter()
        {
            UIRelationFilter result = uIRelationFilter?.Clone();
            if(result == null)
            {
                return result;
            }

            IFilterControl filterControl = Query.FilterControls(grid_Filters).FirstOrDefault();
            result.Filter.Filter = filterControl.UIFilter; 

            return result;
        }

        public IUIFilter UIFilter
        {
            get
            {
                return UIRelationFilter;
            }
        }

        public void Add(IUIFilter uIFilter)
        {
            if (uIFilter == null)
            {
                return;
            }

            Modify.AddFilterControl(grid_Filters, uIFilter, true);
        }
    }
}
