// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// The heat-flow direction a U-value is calculated for, and where it came from (built by
    /// <c>Query.UValueHeatFlowBasis</c>): the affected panels' <see cref="Analytical.PanelType"/>, or the
    /// construction's Default Panel Type when no panel is affected.
    /// </summary>
    public sealed class UValueHeatFlowBasis
    {
        internal UValueHeatFlowBasis(PanelType panelType, HeatFlowDirection heatFlowDirection, bool external, bool fromPanels, bool mixed, IReadOnlyDictionary<PanelType, int> panelTypeCounts)
        {
            Mixed = mixed;
            PanelType = panelType;
            HeatFlowDirection = heatFlowDirection;
            External = external;
            FromPanels = fromPanels;
            PanelTypeCounts = panelTypeCounts ?? new Dictionary<PanelType, int>();
        }

        /// <summary>The panel type the basis stands for (the most common one among the affected panels).</summary>
        public PanelType PanelType { get; }

        /// <summary><see cref="HeatFlowDirection.Undefined"/> when neither the panels nor the construction define one.</summary>
        public HeatFlowDirection HeatFlowDirection { get; }

        public bool External { get; }

        /// <summary>True when taken from the panels, false when from the construction's Default Panel Type.</summary>
        public bool FromPanels { get; }

        /// <summary>How many affected panels have each panel type.</summary>
        public IReadOnlyDictionary<PanelType, int> PanelTypeCounts { get; }

        /// <summary>True when the affected panels would be calculated on more than one basis (e.g. walls and roofs).</summary>
        public bool Mixed { get; }
    }
}
