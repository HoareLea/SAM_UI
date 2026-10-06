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
    /// One saved construction as "My library" lists it on the Constructions tab: what it is made of, the U-value it had when it was saved and on which
    /// heat-flow basis, when it was saved and where it came from (from its provenance: labels and file names only). Identity is the Guid.
    /// </summary>
    public sealed class UserConstructionEntryRow
    {
        internal UserConstructionEntryRow(Construction construction)
        {
            Construction = construction;
            Provenance = UserConstructionProvenance.FromConstruction(construction);
        }

        public Construction Construction { get; }

        public UserConstructionProvenance Provenance { get; }

        public Guid Guid => Construction.Guid;

        public string Name => Construction.Name;

        /// <summary>The last 6 characters of the Guid (tells same-named constructions apart).</summary>
        public string ShortId => Construction.Guid.ToString().Substring(30);

        /// <summary>The build-up in the stored layer order, e.g. "50 Air / 12 Board / 80 Mineral Wool" (thickness in mm, then the material).</summary>
        public string BuildUp => string.Join(" / ", (Construction.ConstructionLayers ?? new List<ConstructionLayer>()).Where(x => x != null).Select(x => string.Format(CultureInfo.CurrentCulture, "{0:0.#} {1}", x.Thickness * 1000, x.Name)));

        /// <summary>"U 0.180 W/m²K" as calculated when it was saved; "U not recorded" when there is none.</summary>
        public string UValueText => Provenance == null || double.IsNaN(Provenance.ThermalTransmittance) ? "U not recorded" : string.Format(CultureInfo.CurrentCulture, "U {0:0.000} W/m²K", Provenance.ThermalTransmittance);

        /// <summary>The heat-flow basis the U-value is for, e.g. "Horizontal heat flow, external surfaces (WallExternal, from the panels)"; empty when none was recorded.</summary>
        public string HeatFlowBasisText => string.IsNullOrWhiteSpace(Provenance?.HeatFlowBasis) ? string.Empty : Provenance.HeatFlowBasis;

        /// <summary>When it was saved ("2026-10-04"); empty when unknown.</summary>
        public string SavedText => Provenance == null || Provenance.CreatedUtc == default ? string.Empty : Provenance.CreatedUtc.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        /// <summary>Where it was saved from ("generated variant", "model", "added source Constructions.tcd"…); "no provenance" when it has none.</summary>
        public string SourceText => Provenance == null ? "no provenance" : Provenance.SavedFromText + (string.IsNullOrWhiteSpace(Provenance.BasedOnName) ? string.Empty : " · based on " + Provenance.BasedOnName);

        /// <summary>Everything the details pane shows: build-up, the U-value at save and how it was obtained, and where it was saved from.</summary>
        public string DetailsText
        {
            get
            {
                List<string> lines = new List<string>()
                {
                    string.Format(CultureInfo.CurrentCulture, "{0}  [{1}]", Name, ShortId),
                    "Build-up (thickness in mm): " + (string.IsNullOrEmpty(BuildUp) ? "–" : BuildUp),
                };

                if (Provenance == null)
                {
                    lines.Add("Not saved by SAM's My constructions (no provenance).");
                }
                else
                {
                    if (!string.IsNullOrEmpty(SavedText))
                    {
                        lines.Add("Saved: " + SavedText);
                    }

                    lines.AddRange(Provenance.Lines());
                }

                return string.Join(Environment.NewLine, lines);
            }
        }

        public override string ToString() => Name;
    }

    /// <summary>
    /// The model-free view-model of the Constructions tab of "My library": the saved constructions of <see cref="UserConstructionLibrary"/> with Rename
    /// and Remove (after a confirmation, to the archive) and their details. It holds the library and nothing else: no analytical model, no Undo, nothing
    /// is ever applied to a model from here, and there is no opaque edit (a construction is authored in the Constructions editor and saved as a new one).
    /// It follows <see cref="UserConstructionLibrary.Changed"/> (a Save, Rename or Remove from anywhere in this process), marshalling the refresh to the
    /// thread it was created on, until it is disposed.
    /// </summary>
    public sealed class UserConstructionLibraryViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly UserConstructionLibrary library;
        private readonly SynchronizationContext context;
        private List<UserConstructionEntryRow> rows = new List<UserConstructionEntryRow>();
        private UserConstructionEntryRow selectedRow;
        private UserConstructionLibraryState state = UserConstructionLibraryState.Missing;
        private string note = string.Empty;
        private string message = string.Empty;
        private bool messageIsError;
        private bool isRenaming;
        private string renameText = string.Empty;
        private bool disposed;
        private bool refreshing;

        public UserConstructionLibraryViewModel(UserConstructionLibrary library)
        {
            this.library = library ?? throw new ArgumentNullException(nameof(library));
            context = SynchronizationContext.Current;
            library.Changed += Library_Changed;
            Refresh();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public UserConstructionLibrary Library => library;

        public IReadOnlyList<UserConstructionEntryRow> Rows => rows;

        public bool HasRows => rows.Count != 0;

        public UserConstructionLibraryState State => state;

        /// <summary>Why the library file cannot be used (it is left as it is and nothing can be changed); empty otherwise.</summary>
        public string Note => note;

        public bool HasNote => !string.IsNullOrEmpty(note);

        /// <summary>Changes are possible only while the file is readable.</summary>
        public bool CanChange => state != UserConstructionLibraryState.Unreadable;

        public string EmptyText => state == UserConstructionLibraryState.Unreadable
            ? string.Empty
            : "No constructions saved yet. Choose Save to My constructions… on an opaque row of the Thermal Performance panel, or in the Constructions editor.";

        public bool ShowEmptyText => rows.Count == 0 && state != UserConstructionLibraryState.Unreadable;

        public string CountText => rows.Count == 1 ? "1 saved construction" : rows.Count + " saved constructions";

        /// <summary>Where removed constructions are kept (shown as a file name only).</summary>
        public string ArchiveText => "Removed constructions are not deleted: they are kept in " + System.IO.Path.GetFileName(library.ArchivePath) + " next to the library.";

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

        public UserConstructionEntryRow SelectedRow
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
            }
        }

        public bool HasSelection => selectedRow != null;

        public string DetailsText => selectedRow?.DetailsText ?? string.Empty;

        /// <summary>What the details pane says while no construction is selected (instead of an empty pane).</summary>
        public string DetailsPlaceholderText => rows.Count == 0 ? string.Empty : "Select a construction to see what it is made of and where it was saved from.";

        public bool ShowDetailsPlaceholder => selectedRow == null && rows.Count != 0;

        public bool CanRename => selectedRow != null && CanChange;

        public bool CanRemove => selectedRow != null && CanChange;

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

        /// <summary>Why the typed name cannot be used (the library's own rule: not empty, unique among the other constructions, ignoring case and spaces at the ends); empty when it can.</summary>
        public string RenameError => isRenaming ? ValidateName(selectedRow, renameText) ?? string.Empty : string.Empty;

        public bool CanCommitRename => isRenaming && string.IsNullOrEmpty(RenameError);

        /// <summary>Starts renaming the selected construction (its name is the starting text).</summary>
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

        /// <summary>Renames the selected construction to <see cref="RenameText"/>: the label only - same Guid, layers, materials and provenance. False (with a <see cref="Message"/>) when nothing changed.</summary>
        public bool CommitRename()
        {
            if (!isRenaming || selectedRow == null || !CanCommitRename)
            {
                return false;
            }

            UserConstructionEditResult result = library.Rename(selectedRow.Guid, renameText);
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

        /// <summary>The library's naming rule applied to the constructions listed now (it checks again, under its lock, when it renames): null when the name can be used.</summary>
        public string ValidateName(UserConstructionEntryRow row, string text)
        {
            return UserConstructionLibrary.NameProblem(text, rows.Where(x => row == null || x.Guid != row.Guid).Select(x => x.Name));
        }

        // ---- Remove -----------------------------------------------------------------------------------------------------

        /// <summary>What the confirmation says: the construction's name and short id, that models keep their own copy, and that it is archived, not deleted.</summary>
        public string RemoveConfirmationText(UserConstructionEntryRow row)
        {
            return string.Format(
                CultureInfo.CurrentCulture,
                "Remove '{0}' [{1}] from {2}?{3}{3}Models that already use it keep their own copy. It is not deleted: it is moved to {4}, next to the library.",
                row?.Name,
                row?.ShortId,
                UserConstructionLibrary.LibraryName,
                Environment.NewLine,
                System.IO.Path.GetFileName(library.ArchivePath));
        }

        /// <summary>
        /// Removes <paramref name="row"/> (the selected one by default) to the archive, but only when <paramref name="confirm"/> - given the
        /// <see cref="RemoveConfirmationText"/> - says yes. False when it was not confirmed or the library refused (see <see cref="Message"/>).
        /// </summary>
        public bool Remove(Func<string, bool> confirm, UserConstructionEntryRow row = null)
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

            UserConstructionEditResult result = library.Remove(row.Guid);
            if (!result.Succeeded)
            {
                ShowMessage(result.Error, true);
                return false;
            }

            Refresh();
            ShowMessage(string.Format(CultureInfo.CurrentCulture, "Removed '{0}'; it is kept in the archive.", result.Entry.Name));
            return true;
        }

        // ---- Reading ----------------------------------------------------------------------------------------------------

        /// <summary>Reads the library as it is on disk now and keeps the selection (by Guid) when the construction is still there.</summary>
        public void Refresh()
        {
            if (disposed)
            {
                return;
            }

            UserConstructionLibraryContent content = library.Read();
            List<UserConstructionEntryRow> rows_New = content.Constructions.Where(x => x != null).OrderBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase).ThenBy(x => x.Guid).Select(x => new UserConstructionEntryRow(x)).ToList();

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

        private void RefreshCore(UserConstructionLibraryContent content, List<UserConstructionEntryRow> rows_New, Guid? selected)
        {
            rows = rows_New;
            state = content.State;
            note = content.State == UserConstructionLibraryState.Unreadable ? GlazingSource.UserConstructionsNote(content.Error) : string.Empty;

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
            Raise(nameof(IsRenaming));
            Raise(nameof(RenameError));
            Raise(nameof(CanCommitRename));
        }

        // A change may come from another thread: the refresh happens on the thread this was created on.
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
