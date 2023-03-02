using SAM.Core.UI;
using System.Collections.Generic;

namespace SAM.Analytical.UI
{
    public static partial class Query
    {
        public static List<IUIFilter> IUIFilters(this System.Type type)
        {
            if(type == null)
            {
                return null;
            }

            List<IUIFilter> result = Core.UI.Query.IUIFilters(type);
            if(result == null)
            {
                result = new List<IUIFilter>();
            }

            if(type.IsAssignableFrom(typeof(Space)))
            {
                result.Add(new UINumberFilter(string.Format("{0} Elevation", type.Name), type, new SpaceElevationFilter(Core.NumberComparisonType.Equals, 0)));
                List<IUIFilter> uIFilters = IUIFilters(typeof(Panel));
                if(uIFilters != null)
                {
                    foreach(IUIFilter uIFilter in uIFilters)
                    {
                        result.Add(new UIRelationFilter(uIFilter.Name, uIFilter.Type, new SpacePanelsFilter(uIFilter)));
                    }
                }
            }
            else if(type.IsAssignableFrom(typeof(Panel)))
            {
                result.Add(new UIEnumFilter(string.Format("{0} Boundary Type", type.Name), type, new PanelBoundaryTypeFilter(BoundaryType.Exposed)));
                result.Add(new UINumberFilter(string.Format("{0} Azimuth", type.Name), type, new PanelAzimuthFilter(Core.NumberComparisonType.Equals, 0)));
                result.Add(new UINumberFilter(string.Format("{0} Tilt", type.Name), type, new PanelTiltFilter(Core.NumberComparisonType.Equals, 0)));
            }

            result.Sort((x, y) => x.Name.CompareTo(y.Name));

            return result;
        }
    }
}