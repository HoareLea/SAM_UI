using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace SAM.Core.UI.WPF
{
    public static partial class Modify
    {
        public static IFilterControl AddFilterControl(this Grid grid, IUIFilter uIFilter, bool generateLogicalFilters = true)
        {
            if (uIFilter == null)
            {
                return null;
            }

            int rowIndex = -1;
            IFilterControl filterControl = null;

            if(!generateLogicalFilters)
            {
                filterControl = uIFilter.IFilterControl();
            }
            else
            {
                List<IFilterControl> filterControls = Query.FilterControls(grid);
                if (filterControls != null && filterControls.Count == 0)
                {
                    filterControl = uIFilter.IFilterControl();
                }
                else
                {
                    List<IUIFilter> uIFilters = new List<IUIFilter>();
                    FilterLogicalOperator filterLogicalOperator = FilterLogicalOperator.Or;

                    LogicalFilterControl logicalFilterControl = filterControls.Find(x => x is LogicalFilterControl) as LogicalFilterControl;
                    if(logicalFilterControl != null)
                    {
                        logicalFilterControl.UILogicalFilter?.Filter?.Filters.FindAll(x => x is IUIFilter).ConvertAll(x => (IUIFilter)x).ForEach(x => uIFilters.Add(x));
                        filterLogicalOperator = logicalFilterControl.FilterLogicalOperator;
                    }
                    else
                    {
                        filterControls.ForEach(x => uIFilters.Add(x.UIFilter));
                    }

                    uIFilters.Add(uIFilter);

                    foreach(IFilterControl filterControl_Temp in filterControls)
                    {
                        rowIndex = Grid.GetRow((UserControl)filterControl_Temp);
                        if (rowIndex != -1)
                        {
                            grid.RowDefinitions.RemoveAt(rowIndex);
                            foreach (UIElement uIElement in grid.Children)
                            {
                                if (Grid.GetRow(uIElement) == rowIndex)
                                {
                                    grid.Children.Remove(uIElement);
                                    break;
                                }
                            }
                        }
                    }

                    logicalFilterControl = new LogicalFilterControl() { UILogicalFilter = new UILogicalFilter(null, uIFilter.Type, new LogicalFilter(FilterLogicalOperator.Or, uIFilters)) };

                    filterControl = logicalFilterControl;
                }
            }

            if(filterControl == null)
            {
                return null;
            }

            rowIndex = grid.RowDefinitions.Count;

            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(0, GridUnitType.Auto) });

            grid.Children.Add((UserControl)filterControl);
            Grid.SetRow((UserControl)filterControl, rowIndex);
            return filterControl;
        }

        public static IFilterControl AddFilterControl(this IFilterControl filterControl, IUIFilter uIFilter)
        {
            if(uIFilter == null || filterControl == null)
            {
                return null;
            }

            if(filterControl is LogicalFilterControl)
            {
                LogicalFilterControl logicalFilterControl = (LogicalFilterControl)filterControl;
                logicalFilterControl.Add(uIFilter);


            }
            else if(filterControl is RelationFilterControl)
            {
                RelationFilterControl relationFilterControl = (RelationFilterControl)filterControl;
                relationFilterControl.Add(uIFilter);
            }

            return null;
        }
    }
}