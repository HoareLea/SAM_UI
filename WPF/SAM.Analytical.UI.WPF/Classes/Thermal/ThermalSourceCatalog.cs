// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>Where a source is in its life: remembered but not read yet, being read, usable, or unusable (missing file, unreadable).</summary>
    public enum ThermalSourceState
    {
        /// <summary>Remembered from an earlier session; read when a row first needs candidates.</summary>
        Pending,

        Loading,

        Ready,

        Failed,
    }

    /// <summary>What is remembered between sessions: the file paths of the sources the user added.</summary>
    public interface IThermalSourceStore
    {
        IReadOnlyList<string> Load();

        void Save(IReadOnlyList<string> paths);
    }

    /// <summary>
    /// The store behind the panel: SAM's own user settings (<see cref="ActiveManager"/>, the mechanism the model filters already use), under
    /// this assembly. Only the PATHS are remembered - the file stays where it is, and its converted copy is in the existing TCD cache
    /// (<see cref="GlazingSourceCache"/>), so a remembered database is read quickly the next time. Best effort: a settings file that cannot be
    /// read or written only means nothing is remembered.
    /// </summary>
    public sealed class ActiveManagerThermalSourceStore : IThermalSourceStore
    {
        private const string Name = "ThermalSourcePaths";

        public IReadOnlyList<string> Load()
        {
            try
            {
                string json = ActiveManager.GetValue<string>(Assembly.GetExecutingAssembly(), Name);
                return string.IsNullOrWhiteSpace(json) ? new List<string>() : JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            }
            catch (Exception)
            {
                return new List<string>();
            }
        }

        public void Save(IReadOnlyList<string> paths)
        {
            try
            {
                ActiveManager.SetValue(Assembly.GetExecutingAssembly(), Name, JsonSerializer.Serialize(paths ?? new List<string>()));
                ActiveManager.Write();
            }
            catch (Exception)
            {
                // Not remembered; the source still works for this session.
            }
        }
    }

    /// <summary>One source in the panel: a file the user added (now or earlier), what state it is in and what it holds.</summary>
    public sealed class ThermalSourceEntry : INotifyPropertyChanged
    {
        private ThermalSourceState state = ThermalSourceState.Pending;
        private string message;
        private GlazingSource source;
        private int constructions;
        private int windows;
        private int doors;

        internal ThermalSourceEntry(string path)
        {
            Path = path;
            Label = System.IO.Path.GetFileName(path);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>The full path (the identity of the entry; the same file is never listed twice).</summary>
        public string Path { get; }

        /// <summary>The file name, as the candidates show it ("Constructions.tcd").</summary>
        public string Label { get; }

        public ThermalSourceState State
        {
            get => state;
            internal set
            {
                state = value;
                Raise();
            }
        }

        /// <summary>The progress while loading, the reason when it failed or has nothing to offer, else null.</summary>
        public string Message
        {
            get => message;
            internal set
            {
                message = value;
                Raise();
            }
        }

        /// <summary>The pool, once <see cref="ThermalSourceState.Ready"/>; null before.</summary>
        public GlazingSource Source => source;

        /// <summary>How many opaque constructions / window systems / door systems the pool holds (counted once when it was read).</summary>
        public int ConstructionCount => constructions;

        public int WindowCount => windows;

        public int DoorCount => doors;

        /// <summary>True when the source has something to offer: constructions or glazing systems.</summary>
        public bool HasContent => source != null && constructions + windows + doors != 0;

        // The pool arrives (the counts read the construction manager, which copies what it hands out, so they are taken once).
        internal void SetSource(GlazingSource value)
        {
            source = value;
            constructions = value?.GetConstructions().Count ?? 0;
            List<ApertureConstruction> apertureConstructions = value?.ConstructionManager?.ApertureConstructions ?? new List<ApertureConstruction>();
            windows = apertureConstructions.Count(x => x.ApertureType == ApertureType.Window);
            doors = apertureConstructions.Count(x => x.ApertureType == ApertureType.Door);
            Raise();
        }

        /// <summary>The one line the panel shows, e.g. "112 constructions · 18 windows · 3 doors", "Loading…", "The file could not be found."</summary>
        public string StatusText
        {
            get
            {
                switch (state)
                {
                    case ThermalSourceState.Pending:
                        return "Remembered · read when needed";

                    case ThermalSourceState.Loading:
                        return message ?? "Loading…";

                    case ThermalSourceState.Failed:
                        return message ?? "Could not be read.";
                }

                if (!HasContent)
                {
                    return message ?? "Nothing to offer.";
                }

                return string.Format(CultureInfo.CurrentCulture, "{0:N0} {1} · {2:N0} {3} · {4:N0} {5}", constructions, constructions == 1 ? "construction" : "constructions", windows, windows == 1 ? "window system" : "window systems", doors, doors == 1 ? "door system" : "door systems");
            }
        }

        public string ToolTip => Path;

        private void Raise()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        }
    }

    /// <summary>
    /// The sources the Thermal Performance panel offers candidates from, beyond the model and the default libraries: ONE list for opaque
    /// constructions and glazing systems (a file is read once and serves both), remembered between sessions (<see cref="IThermalSourceStore"/>).
    /// <list type="bullet">
    /// <item><b>A pool, never the model.</b> Adding a source reads it (the existing import and JSON cache) and lists its candidates; nothing is
    /// written to any model. Only a construction or system the user chooses - and the materials it lacks - enters the model, on Apply.</item>
    /// <item><b>Remembered, read when needed.</b> A remembered source is listed at once but read only when a row first needs candidates
    /// (<see cref="EnsureLoadedAsync"/>), so opening a model costs nothing; a missing file stays listed as failed until the user removes it.</item>
    /// <item><b>Safe duplicates.</b> The same file is one entry; a construction that appears in several sources (same Guid) is one candidate, the
    /// first source winning (model, then library, then in the order added); a name already in the model is handled at Apply (numbered suffix).</item>
    /// </list>
    /// </summary>
    public sealed class ThermalSourceCatalog : INotifyPropertyChanged
    {
        private readonly object gate = new object();
        private readonly List<ThermalSourceEntry> entries = new List<ThermalSourceEntry>();
        private readonly Dictionary<string, Task> loads = new Dictionary<string, Task>(StringComparer.OrdinalIgnoreCase);
        private readonly IThermalSourceStore store;
        private readonly Func<string, IProgress<string>, Task<GlazingSource>> reader;

        /// <param name="store">What is remembered (SAM's user settings by default; tests pass their own).</param>
        /// <param name="reader">Reads one file into a pool (the existing importer by default; tests pass a stand-in).</param>
        public ThermalSourceCatalog(IThermalSourceStore store = null, Func<string, IProgress<string>, Task<GlazingSource>> reader = null)
        {
            this.store = store ?? new ActiveManagerThermalSourceStore();
            this.reader = reader ?? ((path, progress) => Query.ReadThermalSourceAsync(path, progress));

            foreach (string path in this.store.Load() ?? new List<string>())
            {
                string full = Normalise(path);
                if (full != null && !entries.Any(x => string.Equals(x.Path, full, StringComparison.OrdinalIgnoreCase)))
                {
                    entries.Add(new ThermalSourceEntry(full));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>Raised when what the candidates can come from changed: a source became usable or was removed.</summary>
        public event EventHandler SourcesChanged;

        /// <summary>The sources in the order they were added (remembered ones first).</summary>
        public IReadOnlyList<ThermalSourceEntry> Entries
        {
            get
            {
                lock (gate)
                {
                    return entries.ToList();
                }
            }
        }

        /// <summary>The pools that have something to offer, in the order they were added: the first of a Guid wins.</summary>
        public IReadOnlyList<GlazingSource> ReadySources
        {
            get
            {
                lock (gate)
                {
                    return entries.Where(x => x.State == ThermalSourceState.Ready && x.HasContent).Select(x => x.Source).ToList();
                }
            }
        }

        /// <summary>True while a source is being read.</summary>
        public bool IsLoading
        {
            get
            {
                lock (gate)
                {
                    return entries.Any(x => x.State == ThermalSourceState.Loading);
                }
            }
        }

        /// <summary>
        /// Adds a file to the sources (once; the same file is the same entry), remembers it and reads it. The task completes when it is read; it
        /// never faults - an unreadable file ends as a failed entry with the reason.
        /// </summary>
        public Task AddAsync(string path)
        {
            string full = Normalise(path);
            if (full == null)
            {
                return Task.CompletedTask;
            }

            ThermalSourceEntry entry;
            lock (gate)
            {
                entry = entries.Find(x => string.Equals(x.Path, full, StringComparison.OrdinalIgnoreCase));
                if (entry == null)
                {
                    entry = new ThermalSourceEntry(full);
                    entries.Add(entry);
                    Remember();
                }
            }

            Raise();
            return Load(entry);
        }

        /// <summary>Forgets a source: it is no longer listed, remembered or offered. The file and the model are untouched.</summary>
        public void Remove(ThermalSourceEntry entry)
        {
            if (entry == null)
            {
                return;
            }

            bool removed;
            lock (gate)
            {
                removed = entries.Remove(entry);
                loads.Remove(entry.Path);
                if (removed)
                {
                    Remember();
                }
            }

            if (removed)
            {
                Raise();
                SourcesChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>Starts reading every remembered source that is not read yet; completes when all are. Cheap when there is nothing to read.</summary>
        public Task EnsureLoadedAsync()
        {
            List<Task> tasks = new List<Task>();
            foreach (ThermalSourceEntry entry in Entries)
            {
                if (entry.State == ThermalSourceState.Pending)
                {
                    tasks.Add(Load(entry));
                }
                else if (entry.State == ThermalSourceState.Loading)
                {
                    lock (gate)
                    {
                        if (loads.TryGetValue(entry.Path, out Task task))
                        {
                            tasks.Add(task);
                        }
                    }
                }
            }

            return tasks.Count == 0 ? Task.CompletedTask : Task.WhenAll(tasks);
        }

        private Task Load(ThermalSourceEntry entry)
        {
            TaskCompletionSource<bool> completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (gate)
            {
                if (loads.TryGetValue(entry.Path, out Task running) && entry.State != ThermalSourceState.Failed)
                {
                    return running;
                }

                entry.State = ThermalSourceState.Loading;
                entry.Message = "Loading…";
                loads[entry.Path] = completion.Task;
            }

            _ = RunAsync(entry, completion);
            return completion.Task;
        }

        private async Task RunAsync(ThermalSourceEntry entry, TaskCompletionSource<bool> completion)
        {
            try
            {
                await LoadAsync(entry);
            }
            finally
            {
                completion.TrySetResult(true);
            }
        }

        private async Task LoadAsync(ThermalSourceEntry entry)
        {
            Raise();

            GlazingSource source = null;
            string failure = null;
            try
            {
                // Progress is posted to the context that asked, so a late report must not overwrite the outcome.
                source = await reader(entry.Path, new Progress<string>(x =>
                {
                    if (entry.State == ThermalSourceState.Loading)
                    {
                        entry.Message = x;
                    }
                }));
            }
            catch (Exception exception)
            {
                failure = exception.Message;
            }

            lock (gate)
            {
                if (!entries.Contains(entry))
                {
                    // Removed while it was being read: nobody wants it.
                    return;
                }
            }

            if (source == null || failure != null)
            {
                entry.Message = failure ?? "Could not be read.";
                entry.State = ThermalSourceState.Failed;
            }
            else
            {
                // Read. One with nothing to offer (a pane library, a wrong file) says why in the reader's note.
                entry.SetSource(source);
                entry.Message = entry.HasContent ? null : source.Note;
                entry.State = ThermalSourceState.Ready;
            }

            Raise();
            SourcesChanged?.Invoke(this, EventArgs.Empty);
        }

        private void Remember()
        {
            store.Save(entries.Select(x => x.Path).ToList());
        }

        private void Raise()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        }

        private static string Normalise(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            try
            {
                return Path.GetFullPath(path.Trim().Trim('"'));
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
