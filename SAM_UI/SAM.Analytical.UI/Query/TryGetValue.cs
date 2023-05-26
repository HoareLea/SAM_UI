using SAM.Core.UI;
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

            SpaceAppearanceSettings spaceAppearanceSettings = viewSettings?.GetValueAppearanceSettings<SpaceAppearanceSettings>()?.FirstOrDefault();
            if (spaceAppearanceSettings != null)
            {
                IAppearanceSettings appearanceSettings = spaceAppearanceSettings.GetValueAppearanceSettings<ValueAppearanceSettings>();
                if (appearanceSettings is ParameterAppearanceSettings)
                {
                    ParameterAppearanceSettings parameterAppearanceSettings = (ParameterAppearanceSettings)appearanceSettings;

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
                else if (appearanceSettings is ITypeAppearanceSettings)
                {
                    string parameterName = ((ITypeAppearanceSettings)appearanceSettings).GetValueAppearanceSettings<ParameterAppearanceSettings>()?.ParameterName;

                    if (appearanceSettings is InternalConditionAppearanceSettings)
                    {
                        InternalCondition internalCondition = space.InternalCondition;
                        if (internalCondition == null)
                        {
                            return false;
                        }

                        if (Core.Query.TryGetValue(internalCondition, parameterName, out value))
                        {
                            if (parameterName == "Color")
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
                    else if (appearanceSettings is ZoneAppearanceSettings)
                    {
                        ZoneAppearanceSettings zoneAppearanceSettings = (ZoneAppearanceSettings)appearanceSettings;
                        List<Zone> zones = adjacencyCluster.GetZones(space, zoneAppearanceSettings.ZoneCategory);

                        Zone zone = zones?.FirstOrDefault();
                        if (zone == null)
                        {
                            return false;
                        }

                        if (Core.Query.TryGetValue(zone, parameterName, out value))
                        {
                            if (parameterName == "Color")
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
                    else if(appearanceSettings is VentilationSystemAppearanceSettings)
                    {
                        List<VentilationSystem> ventilationSystems = adjacencyCluster.GetRelatedObjects<VentilationSystem>(space);

                        VentilationSystem ventilationSystem = ventilationSystems?.FirstOrDefault();
                        if (ventilationSystem == null)
                        {
                            return false;
                        }

                        if (Core.Query.TryGetValue(ventilationSystem, parameterName, out value))
                        {
                            if (parameterName == "Color")
                            {
                                text = ventilationSystem.Name;
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

            ValueAppearanceSettings valueAppearanceSettings = viewSettings?.GetValueAppearanceSettings<ValueAppearanceSettings>(panel)?.FirstOrDefault();
            if(valueAppearanceSettings != null)
            {
                if (valueAppearanceSettings.TryGetValue(panel, out object value_Temp))
                {
                    value = value_Temp;
                    text = value_Temp?.ToString();
                    return true;
                }
            }

            PanelAppearanceSettings panelAppearanceSettings = viewSettings?.GetValueAppearanceSettings<PanelAppearanceSettings>()?.FirstOrDefault();
            if (panelAppearanceSettings != null)
            {
                IAppearanceSettings appearanceSettings = panelAppearanceSettings.GetValueAppearanceSettings<ValueAppearanceSettings>();
                if (appearanceSettings is ParameterAppearanceSettings)
                {
                    ParameterAppearanceSettings parameterAppearanceSettings = (ParameterAppearanceSettings)appearanceSettings;
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
                else if (appearanceSettings is ITypeAppearanceSettings)
                {
                    string parameterName = ((ITypeAppearanceSettings)appearanceSettings).GetValueAppearanceSettings<ParameterAppearanceSettings>()?.ParameterName;

                    if (appearanceSettings is ConstructionAppearanceSettings)
                    {
                        Construction construction = panel.Construction;
                        if (construction == null)
                        {
                            return false;
                        }

                        if (Core.Query.TryGetValue(construction, parameterName, out value))
                        {
                            if (parameterName == "Color")
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

            ApertureAppearanceSettings apertureAppearanceSettings = viewSettings?.GetValueAppearanceSettings<ApertureAppearanceSettings>()?.FirstOrDefault();
            if (apertureAppearanceSettings != null)
            {
                IAppearanceSettings appearanceSettings = apertureAppearanceSettings.GetValueAppearanceSettings<ValueAppearanceSettings>();
                if (appearanceSettings is ParameterAppearanceSettings)
                {
                    ParameterAppearanceSettings parameterAppearanceSettings = (ParameterAppearanceSettings)appearanceSettings;
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
                else if (appearanceSettings is ITypeAppearanceSettings)
                {
                    string parameterName = ((ITypeAppearanceSettings)appearanceSettings).GetValueAppearanceSettings<ParameterAppearanceSettings>()?.ParameterName;

                    if (appearanceSettings is ApertureConstructionAppearanceSettings)
                    {
                        ApertureConstruction apertureConstruction = aperture.ApertureConstruction;
                        if (apertureConstruction == null)
                        {
                            return false;
                        }

                        if (Core.Query.TryGetValue(apertureConstruction, parameterName, out value))
                        {
                            if (parameterName == "Color")
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
                }
            }

            return false;

        }
    }
}