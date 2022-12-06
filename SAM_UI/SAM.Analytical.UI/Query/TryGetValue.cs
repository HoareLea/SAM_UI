using SAM.Core;
using SAM.Geometry.UI;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.UI
{
    public static partial class Query
    {
        public static bool TryGetValue(this Space space, AdjacencyCluster adjacencyCluster, ViewSettings viewSettings, out object value)
        {
            value = null;

            if(space == null)
            {
                return false;
            }

            if(viewSettings is IAnalyticalViewSettings)
            {
                IAnalyticalViewSettings analyticalViewSettings = (IAnalyticalViewSettings)viewSettings;
                SpaceAppearanceSettings spaceAppearanceSettings = analyticalViewSettings.SpaceAppearanceSettings;
                if(spaceAppearanceSettings != null)
                {
                    ParameterAppearanceSettings parameterAppearanceSettings = spaceAppearanceSettings.ParameterAppearanceSettings<ParameterAppearanceSettings>();
                    if(parameterAppearanceSettings != null)
                    {
                        if(parameterAppearanceSettings is InternalConditionAppearanceSettings)
                        {
                            if (Core.Query.TryGetValue(space.InternalCondition, parameterAppearanceSettings.ParameterName, out value))
                            {
                                return true;
                            }
                        }
                        else if(parameterAppearanceSettings is ZoneAppearanceSettings)
                        {
                            ZoneAppearanceSettings zoneAppearanceSettings = (ZoneAppearanceSettings)parameterAppearanceSettings;
                            List<Zone> zones = adjacencyCluster.GetZones(space, zoneAppearanceSettings.ZoneCategory);
                            if (Core.Query.TryGetValue(zones?.FirstOrDefault(), parameterAppearanceSettings.ParameterName, out value))
                            {
                                return true;
                            }
                        }
                        else
                        {
                            if (Core.Query.TryGetValue(space, parameterAppearanceSettings.ParameterName, out value))
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;

        }
    }
}