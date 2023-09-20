using SAM.Core;
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
                result.Add(new UINumberFilter(string.Format("{0} Elevation", type.Name), type, new SpaceElevationFilter(NumberComparisonType.Greater, 0)));
                result.Add(new UINumberFilter(string.Format("{0} Area", type.Name), type, new SpaceAreaFilter(NumberComparisonType.Greater, 0)));
                result.Add(new UINumberFilter(string.Format("{0} Volume", type.Name), type, new SpaceVolumeFilter(NumberComparisonType.Greater, 0)));
                result.Add(new UITextFilter("Ventilation System Type Name", type, new SpaceVentilationSystemTypeNameFilter(TextComparisonType.StartsWith, string.Empty)));
                result.Add(new UITextFilter("Ventilation System Full Name", type, new SpaceVentilationSystemFullNameFilter(TextComparisonType.StartsWith, string.Empty)));
                result.Add(new UITextFilter("Heating System Type Name", type, new SpaceHeatingSystemTypeNameFilter(TextComparisonType.StartsWith, string.Empty)));
                result.Add(new UITextFilter("Heating System Full Name", type, new SpaceHeatingSystemFullNameFilter(TextComparisonType.StartsWith, string.Empty)));
                result.Add(new UITextFilter("Cooling System Type Name", type, new SpaceCoolingSystemTypeNameFilter(TextComparisonType.StartsWith, string.Empty)));
                result.Add(new UITextFilter("Cooling System Full Name",type, new SpaceCoolingSystemFullNameFilter(TextComparisonType.StartsWith, string.Empty)));

                IUIFilters(typeof(Panel))?.ForEach(x => result.Add(new UIRelationFilter(x.Name, x.Type, new SpacePanelsFilter(x))));
                IUIFilters(typeof(InternalCondition))?.ForEach(x => result.Add(new UIRelationFilter(x.Name, x.Type, new SpaceInternalConditionFilter(x))));
                IUIFilters(typeof(Zone))?.ForEach(x => result.Add(new UIRelationFilter(x.Name, x.Type, new SpaceZonesFilter(x))));
            }
            else if (type.IsAssignableFrom(typeof(Panel)))
            {
                result.Add(new UIEnumFilter(string.Format("{0} Boundary Type", type.Name), type, new PanelBoundaryTypeFilter(BoundaryType.Exposed)));
                result.Add(new UIEnumFilter(string.Format("{0} Panel Type", type.Name), type, new PanelPanelTypeFilter(PanelType.Wall)));
                result.Add(new UIEnumFilter(string.Format("{0} Group", type.Name), type, new PanelPanelGroupFilter(PanelGroup.Wall)));
                result.Add(new UINumberFilter(string.Format("{0} Azimuth", type.Name), type, new PanelAzimuthFilter(NumberComparisonType.Equals, 0)));
                result.Add(new UINumberFilter(string.Format("{0} Tilt", type.Name), type, new PanelTiltFilter(NumberComparisonType.Equals, 0)));
                result.Add(new UINumberFilter(string.Format("{0} Area", type.Name), type, new PanelAreaFilter(NumberComparisonType.Equals, 0)));
                result.Add(new UINumberFilter(string.Format("{0} Min Elevation", type.Name), type, new PanelMinElevationFilter(NumberComparisonType.Equals, 0)));
                result.Add(new UINumberFilter(string.Format("{0} Max Elevation", type.Name), type, new PanelMaxElevationFilter(NumberComparisonType.Equals, 0)));
                result.Add(new UINumberFilter(string.Format("Aperture Count", type.Name), type, new PanelApertureCountFilter(NumberComparisonType.Greater, 0)));

                IUIFilters(typeof(Aperture))?.ForEach(x => result.Add(new UIRelationFilter(x.Name, x.Type, new PanelAperturesFilter(x))));
                IUIFilters(typeof(Construction))?.ForEach(x => result.Add(new UIRelationFilter(x.Name, x.Type, new PanelConstructionFilter(x))));
            }
            else if (type.IsAssignableFrom(typeof(Aperture)))
            {
                result.Add(new UIEnumFilter(string.Format("{0} Aperture Type", type.Name), type, new ApertureApertureTypeFilter(ApertureType.Window)));
                result.Add(new UINumberFilter(string.Format("{0} Pane Area", type.Name), type, new AperturePaneAreaFilter(NumberComparisonType.Greater, 0)));
                result.Add(new UINumberFilter(string.Format("{0} Frame Area", type.Name), type, new ApertureFrameAreaFilter(NumberComparisonType.Greater, 0)));
                result.Add(new UINumberFilter(string.Format("{0} Azimuth", type.Name), type, new ApertureAzimuthFilter(NumberComparisonType.Equals, 0)));

                IUIFilters(typeof(ApertureConstruction))?.ForEach(x => result.Add(new UIRelationFilter(x.Name, x.Type, new ApertureApertureConstructionFilter(x))));
            }

            result.RemoveAll(x => x is UILogicalFilter);

            if (result.Count > 0)
            {
                //result.Add(new UILogicalFilter(string.Format("{0} Logical And/Or", type.Name), type, new LogicalFilter(FilterLogicalOperator.Or)));
            }

            result.Sort((x, y) => x.Name.CompareTo(y.Name));

            return result;
        }
    }
}