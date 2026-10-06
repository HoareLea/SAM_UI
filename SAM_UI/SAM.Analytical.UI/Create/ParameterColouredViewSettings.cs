// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using SAM.Core.UI;
using SAM.Geometry.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.UI
{
    public static partial class Create
    {
        /// <summary>
        /// A COPY of <paramref name="viewSettings"/> that colours the elements of <paramref name="colouring"/>.ElementType by the
        /// parameter, with the legend coloured by the palette: what ticking Parameter Name in View Settings and choosing a palette
        /// in the Legend window produce, in one step and without storing anything. The copy keeps the view's Guid, camera, section
        /// planes and the settings of the other element types; only the appearance of the coloured type and the legend change.
        /// <para>
        /// It reuses the existing pipeline end to end: the value of each element comes from <c>Query.TryGetValue</c> (the same call
        /// the view uses), the legend items from <c>Query.LegendItemDictionary</c>, the colours from
        /// <c>ColorPaletteGenerator</c> and the render from the unchanged <c>ToSAM_GeometryObjectModel</c> - which keeps the colours
        /// of the legend it is given. The original is not touched and nothing is written to the model, so the caller decides how
        /// long the copy lives (a view override) and there is no Undo entry for it. Elements that have no value stay in the
        /// default colour. The coloured type must be shown by the view (<c>ContainsType</c>); this does not change which types are shown.
        /// </para>
        /// </summary>
        /// <returns>The coloured copy; null when the model, the view or the request is missing.</returns>
        public static ThreeDimensionalViewSettings ParameterColouredViewSettings(this AnalyticalModel analyticalModel, ThreeDimensionalViewSettings viewSettings, ParameterColouring colouring)
        {
            if (analyticalModel == null || viewSettings == null || colouring == null)
            {
                return null;
            }

            ThreeDimensionalViewSettings result = new ThreeDimensionalViewSettings(viewSettings);

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;
            List<LegendItemData> legendItemDatas = new List<LegendItemData>();

            if (colouring.ElementType == typeof(Panel))
            {
                result.RemoveAppearanceSettings<PanelAppearanceSettings>();
                result.AddAppearanceSettings(new PanelAppearanceSettings(colouring.ParameterName));

                foreach (Panel panel in adjacencyCluster?.GetPanels() ?? new List<Panel>())
                {
                    if (Query.TryGetValue(panel, adjacencyCluster, result, out object value, out string text, out _))
                    {
                        legendItemDatas.Add(new LegendItemData(panel, value, text));
                    }
                }
            }
            else if (colouring.ElementType == typeof(Aperture))
            {
                result.RemoveAppearanceSettings<ApertureAppearanceSettings>();
                result.AddAppearanceSettings(new ApertureAppearanceSettings(colouring.ParameterName));

                foreach (Aperture aperture in adjacencyCluster?.GetApertures() ?? new List<Aperture>())
                {
                    if (Query.TryGetValue(aperture, adjacencyCluster, result, out object value, out string text, out _))
                    {
                        legendItemDatas.Add(new LegendItemData(aperture, value, text));
                    }
                }
            }
            else if (colouring.ElementType == typeof(Space))
            {
                result.RemoveAppearanceSettings<SpaceAppearanceSettings>();
                result.AddAppearanceSettings(new SpaceAppearanceSettings(colouring.ParameterName));

                foreach (Space space in adjacencyCluster?.GetSpaces() ?? new List<Space>())
                {
                    if (Query.TryGetValue(space, adjacencyCluster, result, out object value, out string text, out _))
                    {
                        legendItemDatas.Add(new LegendItemData(space, value, text));
                    }
                }
            }
            else
            {
                return null;
            }

            // The legend the view would build for this parameter, then coloured by the palette (as the Legend window's Palette button does).
            Dictionary<Guid, LegendItem> dictionary = Query.LegendItemDictionary(legendItemDatas, true, null);
            List<LegendItem> legendItems = dictionary?.Values.GroupBy(x => x.Text).Select(x => x.First()).ToList() ?? new List<LegendItem>();
            if (legendItems.Count == 0)
            {
                result.Legend = null;
                return result;
            }

            Core.UI.Modify.Sort(legendItems);

            List<System.Drawing.Color> colors = ColorPaletteGenerator.GetColors(colouring.Palette, legendItems.ConvertAll(x => x.Text));
            if (colors != null && colors.Count == legendItems.Count)
            {
                for (int i = 0; i < legendItems.Count; i++)
                {
                    legendItems[i] = new LegendItem(colors[i], legendItems[i].Text) { Editable = true };
                }
            }

            result.Legend = new Legend(colouring.Title, legendItems);
            return result;
        }
    }
}
