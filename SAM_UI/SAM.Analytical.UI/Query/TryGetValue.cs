using SAM.Core;
using SAM.Geometry.UI;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.UI
{
    public static partial class Query
    {
        public static bool TryGetValue(this Space space, AdjacencyCluster adjacencyCluster, ViewSettings viewSettings, out object value, out string text)
        {
            value = null;
            text = null;

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
                            InternalCondition internalCondition = space.InternalCondition;
                            if(internalCondition == null)
                            {
                                return false;
                            }

                            if (Core.Query.TryGetValue(internalCondition, parameterAppearanceSettings.ParameterName, out value))
                            {
                                if (parameterAppearanceSettings.ParameterName == "Color")
                                {
                                    text = internalCondition.Name;
                                }
                                else
                                {
                                    text = value?.ToString();
                                }

                                return true;
                            }
                        }
                        else if(parameterAppearanceSettings is ZoneAppearanceSettings)
                        {
                            ZoneAppearanceSettings zoneAppearanceSettings = (ZoneAppearanceSettings)parameterAppearanceSettings;
                            List<Zone> zones = adjacencyCluster.GetZones(space, zoneAppearanceSettings.ZoneCategory);

                            Zone zone = zones?.FirstOrDefault();
                            if(zone == null)
                            {
                                return false;
                            }

                            if (Core.Query.TryGetValue(zone, parameterAppearanceSettings.ParameterName, out value))
                            {
                                if (parameterAppearanceSettings.ParameterName == "Color")
                                {
                                    text = zone.Name;
                                }
                                else
                                {
                                    text = value?.ToString();
                                }

                                return true;
                            }
                        }
                        else
                        {
                            if (Core.Query.TryGetValue(space, parameterAppearanceSettings.ParameterName, out value))
                            {
                                if (parameterAppearanceSettings.ParameterName == "Color")
                                {
                                    text = space.Name;
                                }
                                else
                                {
                                    text = value?.ToString();
                                }

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