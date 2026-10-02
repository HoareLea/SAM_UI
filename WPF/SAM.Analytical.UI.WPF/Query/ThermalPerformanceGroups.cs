// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Query
    {
        private static readonly string[] thermalPerformanceGroupOrder = new[] { "Walls", "Roofs", "Floors", "Other panels", "Windows", "Doors", "Other apertures" };

        /// <summary>
        /// The Thermal Performance panel's content (read-only): panels and apertures grouped under Walls, Roofs, Floors,
        /// Windows, Doors, each group by construction, with the performance STORED on the model (no Tas call, no write).
        /// <para>
        /// <see cref="ThermalPerformanceMode.Selection"/>: the selected panels and apertures (looked up in the model by Guid;
        /// other objects are ignored). <see cref="ThermalPerformanceMode.WholeEnvelope"/>: every external panel (not shades or
        /// solar panels) and the apertures they carry. "Used by" always counts every element using the construction in the model.
        /// </para>
        /// </summary>
        public static List<ThermalPerformanceGroup> ThermalPerformanceGroups(AnalyticalModel analyticalModel, IEnumerable<SAMObject> selected, ThermalPerformanceMode mode)
        {
            AdjacencyCluster adjacencyCluster = analyticalModel?.AdjacencyCluster;
            if (adjacencyCluster == null)
            {
                return new List<ThermalPerformanceGroup>();
            }

            // The elements the panel is about, as the model's own objects.
            List<Panel> panels = new List<Panel>();
            List<Aperture> apertures = new List<Aperture>();
            if (mode == ThermalPerformanceMode.WholeEnvelope)
            {
                foreach (Panel panel in adjacencyCluster.GetPanels() ?? new List<Panel>())
                {
                    if (panel == null || !IsEnvelope(panel.PanelType))
                    {
                        continue;
                    }

                    panels.Add(panel);
                    apertures.AddRange((panel.Apertures ?? new List<Aperture>()).Where(x => x != null));
                }
            }
            else
            {
                HashSet<Guid> guids = new HashSet<Guid>();
                foreach (SAMObject sAMObject in selected ?? Enumerable.Empty<SAMObject>())
                {
                    if (sAMObject is Panel panel && guids.Add(panel.Guid))
                    {
                        panels.Add(adjacencyCluster.GetObject<Panel>(panel.Guid) ?? panel);
                    }
                    else if (sAMObject is Aperture aperture && guids.Add(aperture.Guid))
                    {
                        apertures.Add(adjacencyCluster.GetAperture(aperture.Guid) ?? aperture);
                    }
                }
            }

            Dictionary<string, List<ThermalPerformanceRow>> rows = new Dictionary<string, List<ThermalPerformanceRow>>();

            foreach (IGrouping<(string Title, Guid Guid), Panel> grouping in panels.Where(x => x.Construction != null).GroupBy(x => (PanelGroupTitle(x.PanelType), x.Construction.Guid)))
            {
                Construction construction = grouping.First().Construction;
                List<Panel> panels_Using = adjacencyCluster.GetPanels(construction) ?? new List<Panel>();

                double area = grouping.Select(x => x.GetArea()).Where(x => !double.IsNaN(x)).DefaultIfEmpty(double.NaN).Sum();
                List<Guid> highlight = (mode == ThermalPerformanceMode.Selection ? panels_Using : grouping.ToList()).Select(x => x.Guid).Distinct().ToList();

                Add(rows, grouping.Key.Title, new ThermalPerformanceRow(false, string.Join(", ", grouping.Select(x => x.PanelType.ToString()).Distinct().OrderBy(x => x)), construction.Guid, construction.Name, PanelPerformanceText(grouping), mode == ThermalPerformanceMode.Selection ? grouping.Count() : 0, panels_Using.Count, grouping.Count(), area, highlight, mode));
            }

            foreach (IGrouping<(string Title, Guid Guid), Aperture> grouping in apertures.Where(x => x.ApertureConstruction != null).GroupBy(x => (ApertureTypeTitle(x.ApertureType), x.ApertureConstruction.Guid)))
            {
                ApertureConstruction apertureConstruction = grouping.First().ApertureConstruction;
                List<Aperture> apertures_Using = adjacencyCluster.GetApertures(apertureConstruction) ?? new List<Aperture>();

                double area = grouping.Select(x => x.GetArea()).Where(x => !double.IsNaN(x)).DefaultIfEmpty(double.NaN).Sum();
                List<Guid> highlight = (mode == ThermalPerformanceMode.Selection ? apertures_Using : grouping.ToList()).Select(x => x.Guid).Distinct().ToList();

                Add(rows, grouping.Key.Title, new ThermalPerformanceRow(true, string.Join(", ", grouping.Select(x => x.ApertureType.ToString()).Distinct().OrderBy(x => x)), apertureConstruction.Guid, apertureConstruction.Name, AperturePerformanceText(grouping), mode == ThermalPerformanceMode.Selection ? grouping.Count() : 0, apertures_Using.Count, grouping.Count(), area, highlight, mode));
            }

            return thermalPerformanceGroupOrder
                .Where(rows.ContainsKey)
                .Select(x => new ThermalPerformanceGroup(x, rows[x].OrderBy(y => y.ConstructionName, StringComparer.CurrentCultureIgnoreCase).ThenBy(y => y.ConstructionGuid).ToList()))
                .ToList();
        }

        private static void Add(Dictionary<string, List<ThermalPerformanceRow>> rows, string title, ThermalPerformanceRow row)
        {
            if (!rows.TryGetValue(title, out List<ThermalPerformanceRow> list))
            {
                rows[title] = list = new List<ThermalPerformanceRow>();
            }

            list.Add(row);
        }

        // The external envelope: external panels, except shades and solar panels.
        private static bool IsEnvelope(PanelType panelType)
        {
            return panelType.External() && panelType != PanelType.Shade && panelType != PanelType.SolarPanel;
        }

        private static string PanelGroupTitle(PanelType panelType)
        {
            switch (panelType.PanelGroup())
            {
                case PanelGroup.Wall:
                    return "Walls";

                case PanelGroup.Roof:
                    return "Roofs";

                case PanelGroup.Floor:
                    return "Floors";

                default:
                    return "Other panels";
            }
        }

        private static string ApertureTypeTitle(ApertureType apertureType)
        {
            switch (apertureType)
            {
                case ApertureType.Window:
                    return "Windows";

                case ApertureType.Door:
                    return "Doors";

                default:
                    return "Other apertures";
            }
        }

        // The U-value stored on the panels (Tas.Modify.UpdateThermalParameters writes PanelParameter.ThermalTransmittance on each
        // panel, not on the construction): one value when they agree, "varies" when they do not, "not calculated" when none has it.
        private static string PanelPerformanceText(IEnumerable<Panel> panels)
        {
            string u = StoredValue(panels.ToList(), x => x.TryGetValue(PanelParameter.ThermalTransmittance, out double value) ? value : double.NaN, "0.000");
            return "U " + (u ?? "not calculated");
        }

        // U, g and light transmittance as stored on the apertures: one value when they agree, "varies" when they do not,
        // "not calculated" when the model has none. g and light transmittance are left out where no aperture has them (doors).
        private static string AperturePerformanceText(IEnumerable<Aperture> apertures)
        {
            List<Aperture> list = apertures.ToList();

            string u = StoredValue(list, x => Read(x, ApertureParameter.ThermalTransmittance), "0.000");
            string g = StoredValue(list, x => Read(x, ApertureParameter.TotalSolarEnergyTransmittance), "0.00");
            string lt = StoredValue(list, x => Read(x, ApertureParameter.LightTransmittance), "0.00");

            if (u == null && g == null && lt == null)
            {
                return "not calculated";
            }

            List<string> parts = new List<string> { "U " + (u ?? "not calculated") };
            if (g != null)
            {
                parts.Add("g " + g);
            }

            if (lt != null)
            {
                parts.Add("LT " + lt);
            }

            return string.Join(" · ", parts);
        }

        private static double Read(Aperture aperture, ApertureParameter apertureParameter)
        {
            return aperture.TryGetValue(apertureParameter, out double value) ? value : double.NaN;
        }

        // The value the elements store: null when none does, "varies" when they differ or only some have one, else the value.
        private static string StoredValue<T>(List<T> elements, Func<T, double> read, string format)
        {
            List<double> values = elements.Select(read).Where(x => !double.IsNaN(x)).Select(x => Math.Round(x, 3)).ToList();
            if (values.Count == 0)
            {
                return null;
            }

            return values.Distinct().Count() == 1 && values.Count == elements.Count ? values[0].ToString(format, CultureInfo.CurrentCulture) : "varies";
        }
    }
}
