// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Query
    {
        /// <summary>
        /// The heat-flow basis for a U-value change: from the affected panels' <see cref="PanelType"/> via
        /// <c>Tas.Query.ThermalTransmittance(PanelType, out direction, out external)</c>, falling back to the
        /// construction's Default Panel Type (which can be the generic <see cref="PanelType.Wall"/>) when no
        /// affected panel defines one.
        /// <para>
        /// When the panels span more than one basis (walls and roofs, or internal and external walls) the most
        /// common basis wins - ties go to the construction's Default Panel Type - and <see cref="UValueHeatFlowBasis.Mixed"/>
        /// is set so the window can warn.
        /// </para>
        /// </summary>
        public static UValueHeatFlowBasis UValueHeatFlowBasis(IEnumerable<Panel> panels, Construction construction)
        {
            Dictionary<PanelType, int> panelTypeCounts = new Dictionary<PanelType, int>();
            if (panels != null)
            {
                foreach (Panel panel in panels)
                {
                    if (panel == null)
                    {
                        continue;
                    }

                    panelTypeCounts.TryGetValue(panel.PanelType, out int count);
                    panelTypeCounts[panel.PanelType] = count + 1;
                }
            }

            PanelType defaultPanelType = DefaultPanelType(construction);

            // Group the panel types by the basis Tas calculates them on.
            var groups = panelTypeCounts
                .Select(x => new { PanelType = x.Key, Count = x.Value, Basis = Basis(x.Key) })
                .Where(x => x.Basis.Item1 != HeatFlowDirection.Undefined)
                .GroupBy(x => x.Basis)
                .Select(x => new
                {
                    Basis = x.Key,
                    Count = x.Sum(y => y.Count),
                    HasDefault = x.Any(y => y.PanelType == defaultPanelType),
                    PanelType = x.OrderByDescending(y => y.Count).ThenBy(y => y.PanelType).First().PanelType,
                })
                .OrderByDescending(x => x.Count)
                .ThenByDescending(x => x.HasDefault)
                .ThenBy(x => x.PanelType)
                .ToList();

            if (groups.Count != 0)
            {
                return new UValueHeatFlowBasis(groups[0].PanelType, groups[0].Basis.Item1, groups[0].Basis.Item2, true, groups.Count > 1, panelTypeCounts);
            }

            System.Tuple<HeatFlowDirection, bool> basis = Basis(defaultPanelType);
            return new UValueHeatFlowBasis(defaultPanelType, basis.Item1, basis.Item2, false, false, panelTypeCounts);
        }

        private static System.Tuple<HeatFlowDirection, bool> Basis(PanelType panelType)
        {
            double thermalTransmittance = Tas.Query.ThermalTransmittance(panelType, out HeatFlowDirection heatFlowDirection, out bool external);
            return new System.Tuple<HeatFlowDirection, bool>(double.IsNaN(thermalTransmittance) ? HeatFlowDirection.Undefined : heatFlowDirection, external);
        }

        private static PanelType DefaultPanelType(Construction construction)
        {
            if (construction != null && construction.TryGetValue(ConstructionParameter.DefaultPanelType, out string panelTypeText) && Core.Query.TryGetEnum(panelTypeText, out PanelType panelType))
            {
                return panelType;
            }

            return PanelType.Undefined;
        }
    }
}
