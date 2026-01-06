using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SAM.Core.UI.WPF
{
    public static partial class Query
    {
        public static IFilterControl FilterControl(this Grid grid, System.Type type)
        {
            if(grid?.Children == null || type == null)
            {
                return null;
            }


            foreach (UIElement uIElement in grid.Children)
            {
                IFilterControl filterControl = uIElement as IFilterControl;
                if (filterControl == null)
                {
                    continue;
                }

                IUIFilter uIFilter = filterControl.UIFilter;
                if (uIFilter == null)
                {
                    continue;
                }

                if (uIFilter.Type == type)
                {
                    return filterControl;
                }
            }

            return null;
        }

        public static IFilterControl FilterControl(this IEnumerable<IFilterControl> filterControls, IUIFilter uIFilter)
        {
            if(filterControls == null || uIFilter == null)
            {
                return null;
            }

            if(filterControls.Count() == 0)
            {
                return null;
            }

            List<IFilterControl> filterControls_Temp = new List<IFilterControl>();
            foreach(IFilterControl filterControl in filterControls)
            {
                if(filterControl == null)
                {
                    continue;
                }

                if(filterControl.UIFilter.Type != uIFilter.Type)
                {
                    continue;
                }

                return filterControl;
            }

            return null;
        }
    }
}