using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SAM.Core.UI.WPF
{
    /// <summary>
    /// Interaction logic for RelationFilterControl.xaml
    /// </summary>
    public partial class RelationFilterControl : UserControl, IFilterControl
    {
        private UIRelationFilter uIRelationFilter;

        public event FilterChangedEventHandler FilterChanged;

        public event FilterRemovingEventHandler FilterRemoving;

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

            Add(uIRelationFilter?.Filter.Filter as IUIFilter);

            //Modify.AddFilterControl(grid_Filters, uIRelationFilter?.Filter.Filter as IUIFilter);
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

            IFilterControl filterControl = Modify.AddFilterControl(grid_Filters, uIFilter, true);
            if(filterControl != null)
            {
                filterControl.FilterChanged += FilterControl_FilterChanged;
                filterControl.FilterRemoving += FilterControl_FilterRemoving;
                FilterChanged?.Invoke(this, new FilterChangedEventArgs(UIFilter));
            }
        }

        private void FilterControl_FilterRemoving(object sender, FilterRemovingEventArgs e)
        {
            grid_Filters.Children.Remove(e.FilterControl as UIElement);
            if (grid_Filters.Children.Count == 0)
            {
                FilterRemoving?.Invoke(this, new FilterRemovingEventArgs(this));
            }
        }

        private void FilterControl_FilterChanged(object sender, FilterChangedEventArgs e)
        {
            FilterChanged?.Invoke(this, new FilterChangedEventArgs(e.UIFilter));
        }

        private void grid_Filters_ContextMenuOpening(object sender, ContextMenuEventArgs e)
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
