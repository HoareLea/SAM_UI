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
    public sealed class ThermalPerformanceViewModel : IDisposable
    {
        private AnalyticalModel analyticalModel;
        private List<SAMObject> selected = new List<SAMObject>();
        private ThermalPerformanceMode mode = ThermalPerformanceMode.Selection;
        private bool rebuilding;

        public ThermalPerformanceViewModel()
            : this(null)
        {
        }

        /// <param name="services">The calculations the editing rows use (real Tas by default, created on first edit); tests pass stand-ins.</param>
        public ThermalPerformanceViewModel(ThermalEditServices services)
        {
            Session = new ThermalEditSession(services);
            Session.PropertyChanged += (sender, e) =>
            {
                if (!rebuilding)
                {
                    Changed?.Invoke(this, EventArgs.Empty);
                }
            };
        }

        /// <summary>Raised after <see cref="Groups"/> or the state of the editing <see cref="Session"/> changed.</summary>
        public event EventHandler Changed;

        /// <summary>The editing of the rows (Stage C): pending edits with their pinned scope, the check before Apply, Apply and Discard.</summary>
        public ThermalEditSession Session { get; }

        public ThermalPerformanceMode Mode
        {
            get => mode;
            set
            {
                // Switching the mode would rebuild the rows under an edit in progress; apply or discard it first.
                if (mode == value || Session.IsPending)
                {
                    return;
                }

                mode = value;
                Rebuild();
            }
        }

        /// <summary>The mode can be switched (it cannot while a row is being edited).</summary>
        public bool ModeSwitchEnabled => !Session.IsPending;

        /// <summary>The headings (Walls, Roofs, Floors, Windows, Doors ...) and their constructions for the current mode.</summary>
        public IReadOnlyList<ThermalPerformanceGroup> Groups { get; private set; } = new List<ThermalPerformanceGroup>();

        /// <summary>The line above the list, e.g. "3 elements selected · 2 constructions" or "Select panels or apertures in the view."</summary>
        public string Summary { get; private set; } = "No model.";

        public bool HasRows => Groups.Any(x => x.Rows.Count != 0);

        /// <summary>Ends the Tas workers the editing rows started (if any).</summary>
        public void Dispose()
        {
            Session.Dispose();
        }

        /// <summary>
        /// Hands the panel the model and the selection of the active view; rebuilds - unless a row is being edited. Then the
        /// selection is only remembered (the edit's scope is pinned and the rows stay as they are), and a model change that was
        /// not this session's own Apply (<paramref name="modelChanged"/>: an Undo, another editor) discards the pending edits first.
        /// </summary>
        /// <param name="modelChanged">True when the model was replaced by something that may have changed it (not just the selection or a view setting).</param>
        public void Update(AnalyticalModel analyticalModel, IEnumerable<SAMObject> selected, bool modelChanged = false)
        {
            this.analyticalModel = analyticalModel;
            this.selected = (selected ?? Enumerable.Empty<SAMObject>()).Where(x => x != null).ToList();

            if (Session.IsApplying)
            {
                // The model change this session's own Apply causes: remembered, rendered when Apply has finished.
                return;
            }

            if (Session.IsPending)
            {
                if (!modelChanged)
                {
                    return;
                }

                Session.Invalidate("The model changed outside the panel (for example an Undo), so the pending change was discarded.");
            }
            else if (modelChanged)
            {
                Session.ClearResult();
            }

            Rebuild();
        }

        /// <summary>
        /// Applies the pending edits as one change set through <paramref name="applier"/> (one commit, one Undo) and rebuilds the
        /// rows from the model it produced.
        /// </summary>
        public ThermalChangeResult Apply(Func<ThermalChangeSet, ThermalChangeResult> applier)
        {
            ThermalChangeResult result = Session.Apply(applier);
            if (result != null && result.Succeeded)
            {
                Rebuild();
            }

            return result;
        }

        /// <summary>Refreshes the stored thermal values (one Undo) and rebuilds the rows.</summary>
        public ThermalChangeResult Recalculate(Func<ThermalChangeSet, ThermalChangeResult> applier)
        {
            ThermalChangeResult result = Session.Recalculate(applier);
            if (result != null && result.Succeeded)
            {
                Rebuild();
            }

            return result;
        }

        /// <summary>Throws the pending edits away and rebuilds the rows for the current selection.</summary>
        public void Discard()
        {
            Session.Discard();
            Rebuild();
        }

        private void Rebuild()
        {
            Groups = Query.ThermalPerformanceGroups(analyticalModel, selected, mode);
            rebuilding = true;
            try
            {
                Session.SetRows(analyticalModel, Groups.SelectMany(x => x.Rows), mode);
            }
            finally
            {
                rebuilding = false;
            }

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
