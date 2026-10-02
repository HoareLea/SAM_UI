// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>One place the Glazing System Builder's pane browser lists panes from: the model's materials, the default library, or a remembered / added file.</summary>
    public sealed class GlazingPaneSource : INotifyPropertyChanged
    {
        private IReadOnlyList<GlazingPaneEntry> panes;

        internal GlazingPaneSource(string label, string fileName, ThermalSourceEntry entry, GlazingSource source)
        {
            Label = label ?? string.Empty;
            FileName = fileName;
            Entry = entry;
            Source = source;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public string Label { get; }

        /// <summary>The file name for a file source (recorded as the pane's provenance); null for the model and the default library.</summary>
        public string FileName { get; }

        /// <summary>The catalogue entry for a file source (shared with the Thermal Performance panel's source list); null for the model and the default library.</summary>
        public ThermalSourceEntry Entry { get; }

        /// <summary>The pool the panes are read from; null while a file is not read yet.</summary>
        internal GlazingSource Source { get; set; }

        internal bool Projecting { get; set; }

        /// <summary>The panes, sorted by name; null until the source is read and projected.</summary>
        public IReadOnlyList<GlazingPaneEntry> Panes
        {
            get => panes;
            internal set
            {
                panes = value;
                Raise();
            }
        }

        public bool IsFile => Entry != null;

        public bool IsReady => panes != null;

        /// <summary>True when the source is read and has no panes (a construction database, a wrong file): it is not offered.</summary>
        public bool HasNoPanes => panes != null && panes.Count == 0;

        /// <summary>The line of the source list, e.g. "International Glazing Database_v76-Pilkington.tcd · 1,066 panes".</summary>
        public string Display
        {
            get
            {
                if (panes != null)
                {
                    return string.Format(CultureInfo.CurrentCulture, "{0} · {1:N0} {2}", Label, panes.Count, panes.Count == 1 ? "pane" : "panes");
                }

                switch (Entry?.State)
                {
                    case ThermalSourceState.Pending:
                        return Label + " · remembered, read when chosen";

                    case ThermalSourceState.Failed:
                        return Label + " · could not be read";

                    default:
                        return Label + " · reading…";
                }
            }
        }

        public override string ToString() => Display;

        internal void Raise()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        }
    }

    /// <summary>The columns the pane list can be sorted by.</summary>
    public enum GlazingPaneSortColumn
    {
        Name,
        Thickness,
        SolarTransmittance,
        LightTransmittance,
        Emissivity,
        Category,
    }

    /// <summary>
    /// The pane browser of the Glazing System Builder: the panes of the model, the default library and the remembered / added sources, with a
    /// search over name, display name and category, and the values needed to choose (thickness, solar and light transmittance, emissivity). It
    /// reuses the Thermal Performance panel's source catalogue (<see cref="ThermalSourceCatalog"/>: the same remembered list, the same TCD import
    /// and JSON cache) - there is no second source-management framework - and projects each source's panes ONCE, off the UI thread. A remembered
    /// file is read only when chosen. Reading panes writes nothing; <see cref="AddSourceAsync"/> only adds to the catalogue's remembered list.
    /// </summary>
    public sealed class GlazingPaneBrowser : INotifyPropertyChanged, IDisposable
    {
        public static readonly TimeSpan DefaultSearchDebounce = TimeSpan.FromMilliseconds(150);

        private readonly object gate = new object();
        private readonly ThermalSourceCatalog catalog;
        private readonly SynchronizationContext context = SynchronizationContext.Current;
        private readonly TimeSpan searchDebounce;
        private readonly List<GlazingPaneSource> all = new List<GlazingPaneSource>();
        private IReadOnlyList<GlazingPaneSource> sources = new List<GlazingPaneSource>();
        private IReadOnlyList<GlazingPaneEntry> entries = new List<GlazingPaneEntry>();
        private GlazingPaneSource selectedSource;
        private GlazingPaneEntry selectedEntry;
        private string searchText = string.Empty;
        private GlazingPaneSortColumn sortColumn = GlazingPaneSortColumn.Name;
        private bool sortDescending;
        private int filterVersion;
        private int totalCount;
        private bool disposed;

        /// <param name="fixedSources">The model's materials and the default library (the first Model-kind source is "This model's panes", a Library-kind one "SAM default library").</param>
        /// <param name="catalog">The shared source catalogue; null for none (only the fixed sources).</param>
        /// <param name="pickSourceFile">Asks for the file of a new source (null when cancelled); null: the Add source button does nothing.</param>
        /// <param name="searchDebounce">How long typing must settle before the list is filtered; null for <see cref="DefaultSearchDebounce"/>, zero filters at once.</param>
        public GlazingPaneBrowser(IEnumerable<GlazingSource> fixedSources, ThermalSourceCatalog catalog, Func<string> pickSourceFile = null, TimeSpan? searchDebounce = null)
        {
            this.catalog = catalog;
            PickSourceFile = pickSourceFile;
            this.searchDebounce = searchDebounce ?? DefaultSearchDebounce;

            lock (gate)
            {
                foreach (GlazingSource source in fixedSources ?? Enumerable.Empty<GlazingSource>())
                {
                    if (source == null || source.Kind == GlazingSourceKind.User)
                    {
                        continue;
                    }

                    string label = source.Kind == GlazingSourceKind.Model ? "This model's panes" : source.Kind == GlazingSourceKind.Library ? "SAM default library" : source.Label;
                    GlazingPaneSource paneSource = new GlazingPaneSource(label, null, null, source);
                    all.Add(paneSource);
                    Project(paneSource);
                }

                if (catalog != null)
                {
                    catalog.PropertyChanged += Catalog_Changed;
                    Synchronise();
                }

                RefreshSources();
                SelectedSourceCore(DefaultSource(), false);
                ApplyFilter();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>Asks for the file of a new source (the path, or null when cancelled); the Builder window supplies the open-file dialog.</summary>
        public Func<string> PickSourceFile { get; set; }

        /// <summary>The sources panes can be listed from now (a source without panes is not offered).</summary>
        public IReadOnlyList<GlazingPaneSource> Sources => sources;

        public GlazingPaneSource SelectedSource
        {
            get => selectedSource;
            set => SelectedSourceCore(value, true);
        }

        /// <summary>The panes of the chosen source that match the search, in the chosen order. Virtualise the list: a full IGDB has 11,664.</summary>
        public IReadOnlyList<GlazingPaneEntry> Entries => entries;

        public GlazingPaneEntry SelectedEntry
        {
            get => selectedEntry;
            set
            {
                if (!ReferenceEquals(selectedEntry, value))
                {
                    selectedEntry = value;
                    Raise(nameof(SelectedEntry));
                }
            }
        }

        /// <summary>Words to find (all of them) in a pane's name, display name or category, e.g. "optitherm 4".</summary>
        public string SearchText
        {
            get => searchText;
            set
            {
                value = value ?? string.Empty;
                if (searchText == value)
                {
                    return;
                }

                searchText = value;
                Raise(nameof(SearchText));
                ScheduleFilter();
            }
        }

        public GlazingPaneSortColumn SortColumn => sortColumn;

        public bool SortDescending => sortDescending;

        /// <summary>"Showing 12 of 1,066 panes." / "Reading the file…" / "No pane matches."</summary>
        public string CountText
        {
            get
            {
                GlazingPaneSource source = selectedSource;
                if (source == null)
                {
                    return "No pane source is available. Add a Tas glazing database (.tcd) or a JSON file.";
                }

                if (!source.IsReady)
                {
                    return source.Entry?.State == ThermalSourceState.Failed ? (source.Entry.Message ?? "The file could not be read.") : (source.Entry?.Message ?? "Reading the file…");
                }

                if (totalCount == 0)
                {
                    return "This source has no panes.";
                }

                return entries.Count == 0 ? "No pane matches." : string.Format(CultureInfo.CurrentCulture, "Showing {0:N0} of {1:N0} {2}.", entries.Count, totalCount, totalCount == 1 ? "pane" : "panes");
            }
        }

        private string notice;

        /// <summary>Why a file the user just added offers no panes (null otherwise); shown under the count.</summary>
        public string Notice
        {
            get => notice;
            private set
            {
                if (notice != value)
                {
                    notice = value;
                    Raise(nameof(Notice));
                    Raise(nameof(HasNotice));
                }
            }
        }

        public bool HasNotice => !string.IsNullOrEmpty(notice);

        /// <summary>The task of the newest read / projection / filter, for tests.</summary>
        internal Task LastWork { get; private set; } = Task.CompletedTask;

        /// <summary>Sorts the list by a column; the same column again reverses it.</summary>
        public void SortBy(GlazingPaneSortColumn column)
        {
            if (sortColumn == column)
            {
                sortDescending = !sortDescending;
            }
            else
            {
                sortColumn = column;
                sortDescending = false;
            }

            Raise(nameof(SortColumn));
            ApplyFilter();
        }

        /// <summary>
        /// Adds a file (a Tas glazing database or a JSON file) to the catalogue the Thermal Performance panel shares - remembered between sessions -
        /// and chooses it once it is read. Nothing but the catalogue's remembered list is written; never faults.
        /// </summary>
        public Task AddSourceAsync(string path)
        {
            if (catalog == null || string.IsNullOrWhiteSpace(path))
            {
                return Task.CompletedTask;
            }

            return LastWork = AddAndSelectAsync(path);
        }

        /// <summary>Asks for a file with <see cref="PickSourceFile"/> and adds it.</summary>
        public Task AddSourceAsync()
        {
            return AddSourceAsync(PickSourceFile?.Invoke());
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            if (catalog != null)
            {
                catalog.PropertyChanged -= Catalog_Changed;
            }

            Interlocked.Increment(ref filterVersion);
        }

        private async Task AddAndSelectAsync(string path)
        {
            // The entry exists as soon as AddAsync is called, so the browser chooses it at once (it says it is reading); the read completes later.
            Task read = catalog.AddAsync(path);
            string full;
            try
            {
                full = System.IO.Path.GetFullPath(path.Trim().Trim('"'));
            }
            catch (Exception)
            {
                await read.ConfigureAwait(false);
                return;
            }

            Post(() =>
            {
                Synchronise();
                GlazingPaneSource added = all.Find(x => x.Entry != null && string.Equals(x.Entry.Path, full, StringComparison.OrdinalIgnoreCase));
                if (added != null)
                {
                    Notice = null;
                    pendingSelection = added;
                    SelectedSourceCore(added, false);
                }
            });

            await read.ConfigureAwait(false);
            Post(() =>
            {
                Synchronise();
                ChoosePendingWhenReady();
            });
        }

        private GlazingPaneSource pendingSelection;

        // The source the user just added stays chosen while it is read; when it turns out to have no panes, or cannot be read, the browser says so.
        private void ChoosePendingWhenReady()
        {
            GlazingPaneSource pending = pendingSelection;
            if (pending == null)
            {
                return;
            }

            if (pending.IsReady && !pending.HasNoPanes)
            {
                pendingSelection = null;
                SelectedSourceCore(pending, false);
            }
            else if (pending.HasNoPanes)
            {
                pendingSelection = null;
                Notice = string.Format(CultureInfo.CurrentCulture, "{0} has no panes to offer (it may be a database of constructions).", pending.Label);
            }
            else if (pending.Entry?.State == ThermalSourceState.Failed)
            {
                pendingSelection = null;
                Notice = string.Format(CultureInfo.CurrentCulture, "{0} could not be read: {1}", pending.Label, pending.Entry.Message ?? "unknown reason.");
            }
        }

        private GlazingPaneSource DefaultSource()
        {
            // A file already read (the user's own database) first; else the model's panes, whether or not they are projected yet (the list fills in when they are).
            return sources.FirstOrDefault(x => x.IsFile && x.Entry.State == ThermalSourceState.Ready && !x.HasNoPanes) ?? sources.FirstOrDefault(x => !x.IsFile) ?? sources.FirstOrDefault();
        }

        private void SelectedSourceCore(GlazingPaneSource value, bool byUser)
        {
            if (byUser)
            {
                pendingSelection = null;
                Notice = null;
            }

            if (ReferenceEquals(selectedSource, value))
            {
                return;
            }

            selectedSource = value;
            SelectedEntry = null;
            Raise(nameof(SelectedSource));

            // A remembered file is read when it is chosen (adding the same path again reads it once; a source already read is not read again).
            if (value?.Entry != null && value.Entry.State == ThermalSourceState.Pending && catalog != null)
            {
                LastWork = catalog.AddAsync(value.Entry.Path);
            }

            ApplyFilter();
        }

        private void Catalog_Changed(object sender, PropertyChangedEventArgs e)
        {
            Post(() =>
            {
                if (!disposed)
                {
                    Synchronise();
                    ChoosePendingWhenReady();
                }
            });
        }

        // The catalogue's entries become file sources (once each); a read one is projected (once); a forgotten one disappears.
        private void Synchronise()
        {
            if (catalog == null)
            {
                return;
            }

            List<ThermalSourceEntry> entries_Catalog = catalog.Entries.ToList();
            lock (gate)
            {
                all.RemoveAll(x => x.Entry != null && !entries_Catalog.Contains(x.Entry));
                foreach (ThermalSourceEntry entry in entries_Catalog)
                {
                    if (!all.Any(x => ReferenceEquals(x.Entry, entry)))
                    {
                        all.Add(new GlazingPaneSource(entry.Label, entry.Label, entry, null));
                    }
                }
            }

            foreach (GlazingPaneSource paneSource in all.ToList())
            {
                if (paneSource.Entry != null && paneSource.Entry.State == ThermalSourceState.Ready && paneSource.Entry.Source != null && paneSource.Panes == null && !paneSource.Projecting)
                {
                    paneSource.Source = paneSource.Entry.Source;
                    Project(paneSource);
                }

                paneSource.Raise();
            }

            RefreshSources();
        }

        // Panes are projected once per source, on a worker thread (a full IGDB is 11,664 materials).
        private void Project(GlazingPaneSource paneSource)
        {
            paneSource.Projecting = true;
            GlazingSource source = paneSource.Source;
            Task task = Task.Run(() =>
            {
                List<GlazingPaneEntry> result = source.GetMaterials().Values.OfType<TransparentMaterial>()
                    .Select(x => new GlazingPaneEntry(x, paneSource.Label, paneSource.FileName))
                    .OrderBy(x => x.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                    .ThenBy(x => x.Name, StringComparer.Ordinal)
                    .ToList();
                return result;
            }).ContinueWith(t =>
            {
                paneSource.Projecting = false;
                IReadOnlyList<GlazingPaneEntry> result = t.IsFaulted ? new List<GlazingPaneEntry>() : t.Result;
                Post(() =>
                {
                    paneSource.Panes = result;
                    RefreshSources();
                    ChoosePendingWhenReady();
                    if (selectedSource == null || ReferenceEquals(selectedSource, paneSource))
                    {
                        if (selectedSource == null)
                        {
                            SelectedSourceCore(DefaultSource(), false);
                        }

                        ApplyFilter();
                    }
                });
            }, TaskScheduler.Default);

            LastWork = task;
        }

        private void RefreshSources()
        {
            List<GlazingPaneSource> visible;
            lock (gate)
            {
                // A source is offered while it is being read and once it has panes; one with none (or that failed) is not.
                visible = all.Where(x => !x.HasNoPanes && x.Entry?.State != ThermalSourceState.Failed).ToList();
            }

            sources = visible;
            if (selectedSource != null && !visible.Contains(selectedSource))
            {
                SelectedSourceCore(DefaultSource(), false);
            }

            Raise(nameof(Sources));
            Raise(nameof(CountText));
        }

        private void ScheduleFilter()
        {
            int version = Interlocked.Increment(ref filterVersion);
            if (searchDebounce <= TimeSpan.Zero)
            {
                ApplyFilter();
                return;
            }

            LastWork = Task.Delay(searchDebounce).ContinueWith(_ =>
            {
                if (version == Volatile.Read(ref filterVersion) && !disposed)
                {
                    Post(ApplyFilter);
                }
            }, TaskScheduler.Default);
        }

        /// <summary>Filters and sorts the chosen source's panes now.</summary>
        internal void ApplyFilter()
        {
            Interlocked.Increment(ref filterVersion);
            IReadOnlyList<GlazingPaneEntry> panes = selectedSource?.Panes;
            totalCount = panes?.Count ?? 0;

            List<GlazingPaneEntry> result;
            if (panes == null)
            {
                result = new List<GlazingPaneEntry>();
            }
            else
            {
                string[] words = searchText.ToLowerInvariant().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                result = words.Length == 0 ? panes.ToList() : panes.Where(x => x.Matches(words)).ToList();
                result = Sort(result);
            }

            entries = result;
            if (selectedEntry != null && !result.Contains(selectedEntry))
            {
                SelectedEntry = null;
            }

            Raise(nameof(Entries));
            Raise(nameof(CountText));
        }

        private List<GlazingPaneEntry> Sort(List<GlazingPaneEntry> list)
        {
            if (sortColumn == GlazingPaneSortColumn.Name && !sortDescending)
            {
                return list;
            }

            IOrderedEnumerable<GlazingPaneEntry> ordered;
            switch (sortColumn)
            {
                case GlazingPaneSortColumn.Thickness:
                    ordered = Order(list, x => x.ThicknessMillimetres);
                    break;
                case GlazingPaneSortColumn.SolarTransmittance:
                    ordered = Order(list, x => x.SolarTransmittance);
                    break;
                case GlazingPaneSortColumn.LightTransmittance:
                    ordered = Order(list, x => x.LightTransmittance);
                    break;
                case GlazingPaneSortColumn.Emissivity:
                    ordered = Order(list, x => x.ExternalEmissivity);
                    break;
                case GlazingPaneSortColumn.Category:
                    ordered = sortDescending ? list.OrderByDescending(x => x.Category, StringComparer.CurrentCultureIgnoreCase) : list.OrderBy(x => x.Category, StringComparer.CurrentCultureIgnoreCase);
                    break;
                default:
                    ordered = sortDescending ? list.OrderByDescending(x => x.DisplayName, StringComparer.CurrentCultureIgnoreCase) : list.OrderBy(x => x.DisplayName, StringComparer.CurrentCultureIgnoreCase);
                    break;
            }

            return ordered.ToList();
        }

        // A value that is missing (NaN) sorts last either way.
        private IOrderedEnumerable<GlazingPaneEntry> Order(List<GlazingPaneEntry> list, Func<GlazingPaneEntry, double> key)
        {
            IOrderedEnumerable<GlazingPaneEntry> result = list.OrderBy(x => double.IsNaN(key(x)));
            return sortDescending ? result.ThenByDescending(key) : result.ThenBy(key);
        }

        // State changes run on the context the browser was made on (the UI thread); without one (a test) on whatever thread comes, serialised by the lock.
        private void Post(Action action)
        {
            if (context == null || SynchronizationContext.Current == context)
            {
                lock (gate)
                {
                    action();
                }

                return;
            }

            context.Post(_ =>
            {
                lock (gate)
                {
                    action();
                }
            }, null);
        }

        private void Raise(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
