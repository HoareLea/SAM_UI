// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// The state of the Thermal Performance panel (Stage B, read-only), free of WPF types so it is unit-testable. The host
    /// (<c>AnalyticalWindow</c>, or a modeless tool window) hands it the model and the selection through
    /// <see cref="Update"/>; the panel renders <see cref="Groups"/>. Every update rebuilds from the model it is given: nothing
    /// is kept across a change of the model, so an Undo or an edit from elsewhere is picked up by the next update. It reads
    /// stored values only - no Tas call, no write.
    /// </summary>
    public sealed class ThermalPerformanceViewModel
    {
        private AnalyticalModel analyticalModel;
        private List<SAMObject> selected = new List<SAMObject>();
        private ThermalPerformanceMode mode = ThermalPerformanceMode.Selection;

        /// <summary>Raised after <see cref="Groups"/> changed.</summary>
        public event EventHandler Changed;

        public ThermalPerformanceMode Mode
        {
            get => mode;
            set
            {
                if (mode == value)
                {
                    return;
                }

                mode = value;
                Rebuild();
            }
        }

        /// <summary>The headings (Walls, Roofs, Floors, Windows, Doors ...) and their constructions for the current mode.</summary>
        public IReadOnlyList<ThermalPerformanceGroup> Groups { get; private set; } = new List<ThermalPerformanceGroup>();

        /// <summary>The line above the list, e.g. "3 elements selected · 2 constructions" or "Select panels or apertures in the view."</summary>
        public string Summary { get; private set; } = "No model.";

        public bool HasRows => Groups.Any(x => x.Rows.Count != 0);

        /// <summary>Hands the panel the model and the selection of the active view; rebuilds.</summary>
        public void Update(AnalyticalModel analyticalModel, IEnumerable<SAMObject> selected)
        {
            this.analyticalModel = analyticalModel;
            this.selected = (selected ?? Enumerable.Empty<SAMObject>()).Where(x => x != null).ToList();
            Rebuild();
        }

        private void Rebuild()
        {
            Groups = Query.ThermalPerformanceGroups(analyticalModel, selected, mode);

            List<ThermalPerformanceRow> rows = Groups.SelectMany(x => x.Rows).ToList();
            if (analyticalModel == null)
            {
                Summary = "No model.";
            }
            else if (mode == ThermalPerformanceMode.WholeEnvelope)
            {
                int elements = rows.Sum(x => x.ElementCount);
                Summary = rows.Count == 0
                    ? "The model has no external envelope."
                    : string.Format(CultureInfo.CurrentCulture, "External envelope · {0} {1} · {2} {3}", rows.Count, rows.Count == 1 ? "construction" : "constructions", elements, elements == 1 ? "element" : "elements");
            }
            else
            {
                int elements = rows.Sum(x => x.SelectedCount);
                Summary = elements == 0
                    ? "Select panels or apertures in the view."
                    : string.Format(CultureInfo.CurrentCulture, "{0} {1} selected · {2} {3}", elements, elements == 1 ? "element" : "elements", rows.Count, rows.Count == 1 ? "construction" : "constructions");
            }

            Changed?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>The panels and apertures a click on <paramref name="row"/> highlights, as the model's own objects.</summary>
        public List<SAMObject> HighlightObjects(ThermalPerformanceRow row)
        {
            List<SAMObject> result = new List<SAMObject>();

            AdjacencyCluster adjacencyCluster = analyticalModel?.AdjacencyCluster;
            if (adjacencyCluster == null || row == null)
            {
                return result;
            }

            foreach (Guid guid in row.HighlightGuids)
            {
                SAMObject sAMObject = row.IsAperture ? (SAMObject)adjacencyCluster.GetAperture(guid) : adjacencyCluster.GetObject<Panel>(guid);
                if (sAMObject != null)
                {
                    result.Add(sAMObject);
                }
            }

            return result;
        }
    }
}
