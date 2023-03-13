using SAM.Core.UI;
using System.Collections.Generic;

namespace SAM.Analytical.UI
{
    public static partial class Query
    {
        public static List<IUIFilter> IUIFilters(this System.Type type)
        {
            if (type == null)
            {
                return null;
            }

            List<IUIFilter> result = Core.UI.Query.IUIFilters(type);
            if (result == null)
            {
                result = new List<IUIFilter>();
            }

            if (type.IsAssignableFrom(typeof(Space)))
            {
                result.Add(new UINumberFilter(string.Format("{0} Elevation", type.Name), type, new SpaceElevationFilter(Core.NumberComparisonType.Greater, 0)));
                result.Add(new UINumberFilter(string.Format("{0} Area", type.Name), type, new SpaceAreaFilter(Core.NumberComparisonType.Greater, 0)));
                result.Add(new UINumberFilter(string.Format("{0} Volume", type.Name), type, new SpaceVolumeFilter(Core.NumberComparisonType.Greater, 0)));

                IUIFilters(typeof(Panel))?.ForEach(x => result.Add(new UIRelationFilter(x.Name, x.Type, new SpacePanelsFilter(x))));
                IUIFilters(typeof(InternalCondition))?.ForEach(x => result.Add(new UIRelationFilter(x.Name, x.Type, new SpaceInternalConditionFilter(x))));
                IUIFilters(typeof(Zone))?.ForEach(x => result.Add(new UIRelationFilter(x.Name, x.Type, new SpaceZonesFilter(x))));
            }
            else if (type.IsAssignableFrom(typeof(Panel)))
            {
                result.Add(new UIEnumFilter(string.Format("{0} Boundary Type", type.Name), type, new PanelBoundaryTypeFilter(BoundaryType.Exposed)));
                result.Add(new UIEnumFilter(string.Format("{0} Panel Type", type.Name), type, new PanelPanelTypeFilter(PanelType.Wall)));
                result.Add(new UIEnumFilter(string.Format("{0} Group", type.Name), type, new PanelPanelGroupFilter(PanelGroup.Wall)));
                result.Add(new UINumberFilter(string.Format("{0} Azimuth", type.Name), type, new PanelAzimuthFilter(Core.NumberComparisonType.Equals, 0)));
                result.Add(new UINumberFilter(string.Format("{0} Tilt", type.Name), type, new PanelTiltFilter(Core.NumberComparisonType.Equals, 0)));
                result.Add(new UINumberFilter(string.Format("{0} Area", type.Name), type, new PanelAreaFilter(Core.NumberComparisonType.Equals, 0)));

                IUIFilters(typeof(Aperture))?.ForEach(x => result.Add(new UIRelationFilter(x.Name, x.Type, new PanelAperturesFilter(x))));
                IUIFilters(typeof(Construction))?.ForEach(x => result.Add(new UIRelationFilter(x.Name, x.Type, new PanelConstructionFilter(x))));
            }
            else if (type.IsAssignableFrom(typeof(Aperture)))
            {
                result.Add(new UIEnumFilter(string.Format("{0} Aperture Type", type.Name), type, new ApertureApertureTypeFilter(ApertureType.Window)));
                result.Add(new UINumberFilter(string.Format("{0} Pane Area", type.Name), type, new AperturePaneAreaFilter(Core.NumberComparisonType.Greater, 0)));
                result.Add(new UINumberFilter(string.Format("{0} Frame Area", type.Name), type, new ApertureFrameAreaFilter(Core.NumberComparisonType.Greater, 0)));

                IUIFilters(typeof(ApertureConstruction))?.ForEach(x => result.Add(new UIRelationFilter(x.Name, x.Type, new ApertureApertureConstructionFilter(x))));
            }

            result.Sort((x, y) => x.Name.CompareTo(y.Name));

            return result;
        }
    }
}