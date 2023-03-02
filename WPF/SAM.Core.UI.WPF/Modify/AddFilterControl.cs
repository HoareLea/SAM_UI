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
                    IFilterControl filterControl_Temp = Query.FilterControl(filterControls, uIFilter);
                    if (filterControl_Temp is RelationFilterControl)
                    {
                        RelationFilterControl RelationFilterControl = (RelationFilterControl)filterControl_Temp;
                        filterControl = uIFilter.IFilterControl();

                        RelationFilterControl.Add((uIFilter as UIRelationFilter).Filter.Filter as IUIFilter);
                        return filterControl;
                    }

                    List<IUIFilter> uIFilters = new List<IUIFilter>();
                    if (filterControl_Temp == null)
                    {

                        filterControl_Temp = filterControls[0];
                        uIFilters.Add(filterControl_Temp.UIFilter);

                    }
                    else
                    {
                        if (filterControl_Temp is LogicalFilterControl)
                        {
                            LogicalFilterControl logicalFilterControl_Temp = (LogicalFilterControl)filterControl_Temp;
                            uIFilters.AddRange(logicalFilterControl_Temp.UILogicalFilter.Filter.Filters.FindAll(x => x is IUIFilter).ConvertAll(x => (IUIFilter)x));
                        }
                        else
                        {
                            uIFilters.Add(filterControl_Temp.UIFilter);
                        }
                    }

                    uIFilters.Add(uIFilter);

                    if (filterControl_Temp == null)
                    {
                        return null;
                    }

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

                    LogicalFilterControl logicalFilterControl = new LogicalFilterControl() { UILogicalFilter = new UILogicalFilter(null, uIFilter.Type, new LogicalFilter(FilterLogicalOperator.Or, uIFilters)) };

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
    }
}