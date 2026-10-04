// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// One saved glazing system as "My library" lists it: what it is made of, the values Tas gave it when it was saved, and how it was built
    /// (from its Builder provenance: labels and file names only). Identity is the Guid.
    /// </summary>
    public sealed class UserLibraryEntryRow
    {
        internal UserLibraryEntryRow(ApertureConstruction apertureConstruction, GlazingSource source)
        {
            ApertureConstruction = apertureConstruction;
            Candidate = new GlazingCandidate(apertureConstruction, source, null);
            Provenance = GlazingBuilderProvenance.FromApertureConstruction(apertureConstruction);
        }

        public ApertureConstruction ApertureConstruction { get; }

        internal GlazingCandidate Candidate { get; }

        public GlazingBuilderProvenance Provenance { get; }

        public Guid Guid => ApertureConstruction.Guid;

        public string Name => ApertureConstruction.Name;

        /// <summary>The last 6 characters of the Guid (tells same-named systems apart).</summary>
        public string ShortId => Candidate.ShortId;

        public string PaneBuildUp => Candidate.PaneBuildUp;

        public string FrameText => Candidate.HasFrame ? Candidate.FrameBuildUp : "no frame";

        /// <summary>"Ug 1.05 · g 0.52 · LT 0.75 · Uf 1.80" as calculated when it was saved; "not recorded" when it has no provenance.</summary>
        public string ValuesText
        {
            get
            {
                GlazingValues values = Provenance?.Performance;
                return values == null
                    ? "values not recorded"
                    : string.Format(CultureInfo.CurrentCulture, "Ug {0} · g {1} · LT {2} · Uf {3}", Format(values.Ug), Format(values.G), Format(values.LightTransmittance), Format(values.Uf));
            }
        }

        /// <summary>When it was saved ("2026-10-02"); empty when unknown.</summary>
        public string SavedText => Provenance == null || Provenance.CreatedUtc == default ? string.Empty : Provenance.CreatedUtc.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        /// <summary>What it was based on ("based on SIM_EXT_GLZ"), or "not based on another system"; empty without provenance.</summary>
        public string BasedOnText => Provenance == null ? string.Empty : string.IsNullOrWhiteSpace(Provenance.BasedOnName) ? "not based on another system" : "based on " + Provenance.BasedOnName;

        /// <summary>Everything the details pane shows: build-up, frame, values at save and how it was built.</summary>
        public string DetailsText
        {
            get
            {
                List<string> lines = new List<string>()
                {
                    string.Format(CultureInfo.CurrentCulture, "{0}  [{1}]", Name, ShortId),
                    // Each layer's thickness is in millimetres (the label says so); the frame's layers are also told apart from the "Frame:" line of how it was built.
                    "Pane (thickness in mm): " + (string.IsNullOrEmpty(PaneBuildUp) ? "–" : PaneBuildUp),
                    Candidate.HasFrame ? "Frame layers (thickness in mm): " + FrameText : "Frame layers: no frame",
                    "At save: " + ValuesText + (string.IsNullOrWhiteSpace(Provenance?.PerformanceEngine) ? string.Empty : " (" + Provenance.PerformanceEngine + ")"),
                };

                if (Provenance == null)
                {
                    lines.Add("Not made with the Glazing System Builder (no Builder provenance).");
                }
                else
                {
                    lines.AddRange(Query.GlazingBuiltFrom(Provenance));
                }

                return string.Join(Environment.NewLine, lines);
            }
        }

        public override string ToString() => Name;

        private static string Format(double value) => double.IsNaN(value) || double.IsInfinity(value) ? "–" : value.ToString("0.00", CultureInfo.CurrentCulture);
    }

    /// <summary>
    /// The model-free view-model of "My library": the saved glazing systems of <see cref="UserGlazingLibrary"/> with Rename, Remove (after a
    /// confirmation, to the archive) and a request to open one in the Glazing System Builder. It holds the library and nothing else: no analytical
    /// model, no Undo, nothing is ever applied to a model from here. It follows <see cref="UserGlazingLibrary.Changed"/> (a Save, Rename or Remove from
    /// anywhere in this process), marshalling the refresh to the thread it was created on, until it is disposed.
    /// </summary>
    public sealed class UserLibraryViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly UserGlazingLibrary library;
        private readonly SynchronizationContext context;
        private List<UserLibraryEntryRow> rows = new List<UserLibraryEntryRow>();
        private UserLibraryEntryRow selectedRow;
        private UserGlazingLibraryState state = UserGlazingLibraryState.Missing;
        private string note = string.Empty;
        private string message = string.Empty;
        private bool messageIsError;
        private bool isRenaming;
        private string renameText = string.Empty;
        private bool disposed;
        private bool refreshing;

        public UserLibraryViewModel(UserGlazingLibrary library)
        {
            this.library = library ?? throw new ArgumentNullException(nameof(library));
            context = SynchronizationContext.Current;
            library.Changed += Library_Changed;
            Refresh();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>Raised when the user asks to open <see cref="SelectedRow"/> (or a given row) in the Glazing System Builder; the host shows it.</summary>
        public event EventHandler<UserLibraryEntryRow> OpenInBuilderRequested;

        public UserGlazingLibrary Library => library;

        /// <summary>
        /// The Constructions tab of "My library" (<see cref="UserConstructionLibraryViewModel"/>), when the host has "My constructions"; null leaves the
        /// tab out. It belongs to this view-model: it is disposed with it.
        /// </summary>
        public UserConstructionLibraryViewModel Constructions { get; set; }

        public IReadOnlyList<UserLibraryEntryRow> Rows => rows;

        public bool HasRows => rows.Count != 0;

        public UserGlazingLibraryState State => state;

        /// <summary>Why the library file cannot be used (it is left as it is and nothing can be changed); empty otherwise.</summary>
        public string Note => note;

        public bool HasNote => !string.IsNullOrEmpty(note);

        /// <summary>Changes are possible only while the file is readable.</summary>
        public bool CanChange => state != UserGlazingLibraryState.Unreadable;

        public string EmptyText => state == UserGlazingLibraryState.Unreadable
            ? string.Empty
            : "No glazing systems saved yet. Open Create new… in a window's Change… list, build a system and choose Save as predefined.";

        public bool ShowEmptyText => rows.Count == 0 && state != UserGlazingLibraryState.Unreadable;

        public string CountText => rows.Count == 1 ? "1 saved system" : rows.Count + " saved systems";

        /// <summary>Where removed systems are kept (shown as a file name only).</summary>
        public string ArchiveText => "Removed systems are not deleted: they are kept in " + System.IO.Path.GetFileName(library.ArchivePath) + " next to the library.";

        /// <summary>The result of the last Rename / Remove (an error says why nothing changed); empty otherwise.</summary>
        public string Message
        {
            get => message;
            private set => Set(ref message, value ?? string.Empty);
        }

        /// <summary>True when <see cref="Message"/> says why something did NOT happen (shown as an error, not as a confirmation).</summary>
        public bool MessageIsError => messageIsError;

        private void ShowMessage(string text, bool isError = false)
        {
            bool wasError = messageIsError;
            messageIsError = isError && !string.IsNullOrEmpty(text);
            if (wasError != messageIsError)
            {
                Raise(nameof(MessageIsError));
            }

            Message = text;
        }

        public UserLibraryEntryRow SelectedRow
        {
            get => selectedRow;
            set
            {
                // The list clears its selection while it is given new rows; Refresh puts the selection (by Guid) back.
                if (selectedRow == value || (refreshing && value == null))
                {
                    return;
                }

                if (isRenaming)
                {
                    CancelRename();
                }

                selectedRow = value;
                ShowMessage(string.Empty);
                Raise();
                Raise(nameof(DetailsText));
                Raise(nameof(ShowDetailsPlaceholder));
                Raise(nameof(HasSelection));
                Raise(nameof(CanRename));
                Raise(nameof(CanRemove));
                Raise(nameof(CanOpenInBuilder));
            }
        }

        public bool HasSelection => selectedRow != null;

        public string DetailsText => selectedRow?.DetailsText ?? string.Empty;

        /// <summary>What the details pane says while no system is selected (instead of an empty pane).</summary>
        public string DetailsPlaceholderText => rows.Count == 0 ? string.Empty : "Select a system to see what it is made of and how it was built.";

        public bool ShowDetailsPlaceholder => selectedRow == null && rows.Count != 0;

        public bool CanRename => selectedRow != null && CanChange;

        public bool CanRemove => selectedRow != null && CanChange;

        public bool CanOpenInBuilder => selectedRow != null && CanChange;

        // ---- Rename -----------------------------------------------------------------------------------------------------

        public bool IsRenaming => isRenaming;

        /// <summary>The name being typed while <see cref="IsRenaming"/>.</summary>
        public string RenameText
        {
            get => renameText;
            set
            {
                if (Set(ref renameText, value ?? string.Empty))
                {
                    Raise(nameof(RenameError));
                    Raise(nameof(CanCommitRename));
                }
            }
        }

        /// <summary>Why the typed name cannot be used (the library's own rule: not empty, unique among the other systems, ignoring case and spaces at the ends); empty when it can.</summary>
        public string RenameError => isRenaming ? ValidateName(selectedRow, renameText) ?? string.Empty : string.Empty;

        public bool CanCommitRename => isRenaming && string.IsNullOrEmpty(RenameError);

        /// <summary>Starts renaming the selected system (its name is the starting text).</summary>
        public bool BeginRename()
        {
            if (!CanRename)
            {
                return false;
            }

            isRenaming = true;
            ShowMessage(string.Empty);
            renameText = selectedRow.Name;
            Raise(nameof(IsRenaming));
            Raise(nameof(RenameText));
            Raise(nameof(RenameError));
            Raise(nameof(CanCommitRename));
            return true;
        }

        public void CancelRename()
        {
            if (!isRenaming)
            {
                return;
            }

            isRenaming = false;
            Raise(nameof(IsRenaming));
            Raise(nameof(RenameError));
            Raise(nameof(CanCommitRename));
        }

        /// <summary>Renames the selected system to <see cref="RenameText"/>: the label only - same Guid, layers, materials and provenance. False (with a <see cref="Message"/>) when nothing changed.</summary>
        public bool CommitRename()
        {
            if (!isRenaming || selectedRow == null)
            {
                return false;
            }

            if (!CanCommitRename)
            {
                return false;
            }

            Guid guid = selectedRow.Guid;
            UserGlazingEditResult result = library.Rename(guid, renameText);
            if (!result.Succeeded)
            {
                ShowMessage(result.Error, true);
                return false;
            }

            isRenaming = false;
            Raise(nameof(IsRenaming));
            Refresh();
            ShowMessage(result.Modified ? string.Format(CultureInfo.CurrentCulture, "Renamed to '{0}'.", result.Entry.Name) : string.Empty);
            return true;
        }

        /// <summary>The library's naming rule applied to the systems listed now (it checks again, under its lock, when it renames): null when the name can be used.</summary>
        public string ValidateName(UserLibraryEntryRow row, string text)
        {
            string name = text?.Trim();
            if (string.IsNullOrEmpty(name))
            {
                return "The system needs a name.";
            }

            if (rows.Any(x => (row == null || x.Guid != row.Guid) && string.Equals(x.Name?.Trim(), name, StringComparison.OrdinalIgnoreCase)))
            {
                return string.Format(CultureInfo.CurrentCulture, "A system named '{0}' is already in {1}.", name, UserGlazingLibrary.LibraryName);
            }

            return null;
        }

        // ---- Remove -----------------------------------------------------------------------------------------------------

        /// <summary>What the confirmation says: the system's name and short id, that models keep their own copy, and that it is archived, not deleted.</summary>
        public string RemoveConfirmationText(UserLibraryEntryRow row)
        {
            return string.Format(
                CultureInfo.CurrentCulture,
                "Remove '{0}' [{1}] from {2}?{3}{3}Models that already use it keep their own copy. It is not deleted: it is moved to {4}, next to the library.",
                row?.Name,
                row?.ShortId,
                UserGlazingLibrary.LibraryName,
                Environment.NewLine,
                System.IO.Path.GetFileName(library.ArchivePath));
        }

        /// <summary>
        /// Removes <paramref name="row"/> (the selected one by default) to the archive, but only when <paramref name="confirm"/> - given the
        /// <see cref="RemoveConfirmationText"/> - says yes. False when it was not confirmed or the library refused (see <see cref="Message"/>).
        /// </summary>
        public bool Remove(Func<string, bool> confirm, UserLibraryEntryRow row = null)
        {
            row = row ?? selectedRow;
            if (row == null || !CanChange)
            {
                return false;
            }

            if (confirm == null || !confirm(RemoveConfirmationText(row)))
            {
                return false;
            }

            UserGlazingEditResult result = library.Remove(row.Guid);
            if (!result.Succeeded)
            {
                ShowMessage(result.Error, true);
                return false;
            }

            Refresh();
            ShowMessage(string.Format(CultureInfo.CurrentCulture, "Removed '{0}'; it is kept in the archive.", result.Entry.Name));
            return true;
        }

        // ---- Open in Builder ----------------------------------------------------------------------------------------------

        /// <summary>Asks the host to open <paramref name="row"/> (the selected one by default) in the Glazing System Builder. Nothing else happens here.</summary>
        public bool RequestOpenInBuilder(UserLibraryEntryRow row = null)
        {
            row = row ?? selectedRow;
            if (row == null || !CanChange || OpenInBuilderRequested == null)
            {
                return false;
            }

            OpenInBuilderRequested(this, row);
            return true;
        }

        // ---- Reading ----------------------------------------------------------------------------------------------------

        /// <summary>Reads the library as it is on disk now and keeps the selection (by Guid) when the system is still there.</summary>
        public void Refresh()
        {
            if (disposed)
            {
                return;
            }

            UserGlazingLibraryContent content = library.Read();
            GlazingSource source = new GlazingSource(GlazingSourceKind.User, UserGlazingLibrary.LibraryName, content.ConstructionManager);
            List<UserLibraryEntryRow> rows_New = content.Systems.Where(x => x != null).OrderBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase).ThenBy(x => x.Guid).Select(x => new UserLibraryEntryRow(x, source)).ToList();

            Guid? selected = selectedRow?.Guid;
            refreshing = true;
            try
            {
                RefreshCore(content, rows_New, selected);
            }
            finally
            {
                refreshing = false;
            }
        }

        private void RefreshCore(UserGlazingLibraryContent content, List<UserLibraryEntryRow> rows_New, Guid? selected)
        {
            rows = rows_New;
            state = content.State;
            note = content.State == UserGlazingLibraryState.Unreadable ? GlazingSource.UserNote(content.Error) : string.Empty;

            selectedRow = selected == null ? null : rows.FirstOrDefault(x => x.Guid == selected.Value);
            if (isRenaming && selectedRow == null)
            {
                isRenaming = false;
            }

            Raise(nameof(Rows));
            Raise(nameof(HasRows));
            Raise(nameof(State));
            Raise(nameof(Note));
            Raise(nameof(HasNote));
            Raise(nameof(CanChange));
            Raise(nameof(EmptyText));
            Raise(nameof(ShowEmptyText));
            Raise(nameof(CountText));
            Raise(nameof(SelectedRow));
            Raise(nameof(DetailsText));
            Raise(nameof(ShowDetailsPlaceholder));
            Raise(nameof(HasSelection));
            Raise(nameof(CanRename));
            Raise(nameof(CanRemove));
            Raise(nameof(CanOpenInBuilder));
            Raise(nameof(IsRenaming));
            Raise(nameof(RenameError));
            Raise(nameof(CanCommitRename));
        }

        // A change may come from another thread (a Builder saving off the UI thread): the refresh happens on the thread this was created on.
        private void Library_Changed(object sender, EventArgs e)
        {
            if (disposed)
            {
                return;
            }

            if (context == null || context == SynchronizationContext.Current)
            {
                Refresh();
                return;
            }

            context.Post(_ => Refresh(), null);
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            library.Changed -= Library_Changed;
            Constructions?.Dispose();
        }

        private bool Set<T>(ref T field, T value, [CallerMemberName] string name = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }

            field = value;
            Raise(name);
            return true;
        }

        private void Raise([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
