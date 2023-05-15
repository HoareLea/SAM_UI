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

        public static bool TryGetValue(this Panel panel, AdjacencyCluster adjacencyCluster, ViewSettings viewSettings, out object value, out string text)
        {
            value = null;
            text = null;

            if (panel == null)
            {
                return false;
            }

            if (viewSettings is IAnalyticalViewSettings)
            {
                IAnalyticalViewSettings analyticalViewSettings = (IAnalyticalViewSettings)viewSettings;
                PanelAppearanceSettings panelAppearanceSettings = analyticalViewSettings.PanelAppearanceSettings;
                if (panelAppearanceSettings != null)
                {
                    ParameterAppearanceSettings parameterAppearanceSettings = panelAppearanceSettings.ParameterAppearanceSettings<ParameterAppearanceSettings>();
                    if (parameterAppearanceSettings != null)
                    {
                        if (parameterAppearanceSettings is ConstructionAppearanceSettings)
                        {
                            Construction construction = panel.Construction;
                            if (construction == null)
                            {
                                return false;
                            }

                            if (Core.Query.TryGetValue(construction, parameterAppearanceSettings.ParameterName, out value))
                            {
                                if (parameterAppearanceSettings.ParameterName == "Color")
                                {
                                    text = construction.Name;
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
                            if (Core.Query.TryGetValue(panel, parameterAppearanceSettings.ParameterName, out value))
                            {
                                if (parameterAppearanceSettings.ParameterName == "Color")
                                {
                                    text = panel.Name;
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

        public static bool TryGetValue(this Aperture aperture, AdjacencyCluster adjacencyCluster, ViewSettings viewSettings, out object value, out string text)
        {
            value = null;
            text = null;

            if (aperture == null)
            {
                return false;
            }

            if (viewSettings is IAnalyticalViewSettings)
            {
                IAnalyticalViewSettings analyticalViewSettings = (IAnalyticalViewSettings)viewSettings;
                PanelAppearanceSettings panelAppearanceSettings = analyticalViewSettings.PanelAppearanceSettings;
                if (panelAppearanceSettings != null)
                {
                    ParameterAppearanceSettings parameterAppearanceSettings = panelAppearanceSettings.ParameterAppearanceSettings<ParameterAppearanceSettings>();
                    if (parameterAppearanceSettings != null)
                    {
                        if (parameterAppearanceSettings is ApertureConstructionAppearanceSettings)
                        {
                            ApertureConstruction apertureConstruction = aperture.ApertureConstruction;
                            if (apertureConstruction == null)
                            {
                                return false;
                            }

                            if (Core.Query.TryGetValue(apertureConstruction, parameterAppearanceSettings.ParameterName, out value))
                            {
                                if (parameterAppearanceSettings.ParameterName == "Color")
                                {
                                    text = apertureConstruction.Name;
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
                            if (Core.Query.TryGetValue(aperture, parameterAppearanceSettings.ParameterName, out value))
                            {
                                if (parameterAppearanceSettings.ParameterName == "Color")
                                {
                                    text = aperture.Name;
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