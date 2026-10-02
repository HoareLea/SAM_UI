// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// What the editing rows need from outside: the calculations (real Tas by default, created on first use and owned here;
    /// tests pass stand-ins), the default glazing library, the batch U-value calculation of the existing constructions and the default
    /// construction library behind the opaque alternatives, and the session's U-value cache. Nothing is created until a row is edited, so a panel that is only
    /// looked at starts no Tas worker.
    /// </summary>
    public sealed class ThermalEditServices : IDisposable
    {
        private readonly Func<IUValueEvaluator> createUValueEvaluator;
        private readonly Func<IGlazingEvaluator> createGlazingEvaluator;
        private readonly Func<GlazingSource> createLibrary;
        private readonly Func<IConstructionUValueEvaluator> createConstructionEvaluator;
        private readonly Func<GlazingSource> createConstructionLibrary;
        private readonly Func<ThermalSourceCatalog> createSources;
        private readonly Func<UserGlazingLibrary> createUserGlazing;
        private IUValueEvaluator uValueEvaluator;
        private IGlazingEvaluator glazingEvaluator;
        private IConstructionUValueEvaluator constructionEvaluator;
        private ThermalSourceCatalog sources;
        private UserGlazingLibrary userGlazing;

        /// <param name="userGlazing">"My glazing systems" (the process's shared library by default; tests pass one on a temporary file).</param>
        public ThermalEditServices(Func<IUValueEvaluator> uValueEvaluator = null, Func<IGlazingEvaluator> glazingEvaluator = null, Func<GlazingSource> library = null, Func<IConstructionUValueEvaluator> constructionEvaluator = null, Func<GlazingSource> constructionLibrary = null, Func<ThermalSourceCatalog> sources = null, Func<UserGlazingLibrary> userGlazing = null)
        {
            createSources = sources ?? (() => new ThermalSourceCatalog());
            createUserGlazing = userGlazing ?? (() => UserGlazingLibrary.Shared);
            createUValueEvaluator = uValueEvaluator ?? (() => new TasUValueEvaluator());
            createGlazingEvaluator = glazingEvaluator ?? (() => new TasGlazingEvaluator());
            createLibrary = library ?? DefaultLibrary;
            createConstructionEvaluator = constructionEvaluator ?? (() => new TasConstructionUValueEvaluator());
            createConstructionLibrary = constructionLibrary ?? DefaultConstructionLibrary;
        }

        public IUValueEvaluator UValueEvaluator => uValueEvaluator ?? (uValueEvaluator = createUValueEvaluator());

        public IGlazingEvaluator GlazingEvaluator => glazingEvaluator ?? (glazingEvaluator = createGlazingEvaluator());

        public IConstructionUValueEvaluator ConstructionEvaluator => constructionEvaluator ?? (constructionEvaluator = createConstructionEvaluator());

        /// <summary>
        /// The sources the user added ("Add source..."), remembered between sessions: one list for opaque constructions and glazing systems. Created
        /// on first use (it reads SAM's user settings), so a panel that is only looked at in a test or a host without it reads nothing.
        /// </summary>
        public ThermalSourceCatalog Sources => this.sources ?? (this.sources = createSources());

        /// <summary>
        /// "My glazing systems" (<see cref="UserGlazingLibrary"/>): its complete systems are candidates of every glazing <c>Change…</c> list, read
        /// when a list opens and again when the library says it changed. Created on first use; reading it never writes it or any model.
        /// </summary>
        public UserGlazingLibrary UserGlazing => userGlazing ?? (userGlazing = createUserGlazing());

        /// <summary>The U-values of constructions already calculated this session (never asked of Tas twice).</summary>
        public ConstructionUValueCache ConstructionCache { get; } = new ConstructionUValueCache();

        /// <summary>The default glazing library as a source; null when it cannot be read (the model's own systems are still offered).</summary>
        public Func<GlazingSource> GlazingLibrary => createLibrary;

        /// <summary>The default construction library as a source for the opaque alternatives; null when it cannot be read (the model's own constructions are still offered).</summary>
        public Func<GlazingSource> ConstructionLibrary => createConstructionLibrary;

        public void Dispose()
        {
            (uValueEvaluator as IDisposable)?.Dispose();
            (glazingEvaluator as IDisposable)?.Dispose();
            (constructionEvaluator as IDisposable)?.Dispose();
            uValueEvaluator = null;
            glazingEvaluator = null;
            constructionEvaluator = null;
        }

        private static GlazingSource DefaultConstructionLibrary()
        {
            try
            {
                return GlazingSource.ConstructionsFromDefaultLibrary();
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static GlazingSource DefaultLibrary()
        {
            try
            {
                return GlazingSource.FromDefaultLibrary();
            }
            catch (Exception)
            {
                return null;
            }
        }
    }

    /// <summary>
    /// The editing session of the Thermal Performance panel (Stage C), free of WPF types so it is unit-testable. It owns the
    /// row editors of the rows on show and turns what they propose into ONE <see cref="ThermalChangeSet"/>:
    /// <list type="bullet">
    /// <item><b>Pinned.</b> As soon as a row is edited (<see cref="IsPending"/>) the panel stops following the selection: the
    /// rows, and the elements each change reaches, stay as they were when the edit started, until Apply or Discard.</item>
    /// <item><b>Invalidated.</b> A change of the model that did not come from this session's own Apply (an Undo, another editor)
    /// discards the pending edits and says so (<see cref="Notice"/>), because they were calculated for a model that is gone.</item>
    /// <item><b>Checked before Apply.</b> Every change of an edit rebuilds the PROPOSED model on a clone (no Tas, no write) and
    /// runs the existing scoped checks on it and on the current model; only what is new is reported (<see cref="Diff"/>).</item>
    /// <item><b>One Apply, one Undo.</b> <see cref="Apply"/> hands the whole set to the applier, which commits once.</item>
    /// </list>
    /// </summary>
    public sealed class ThermalEditSession : INotifyPropertyChanged, IDisposable
    {
        private readonly List<ThermalRowEditor> editors = new List<ThermalRowEditor>();
        private AnalyticalModel analyticalModel;

        public ThermalEditSession(ThermalEditServices services = null)
        {
            Services = services ?? new ThermalEditServices();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ThermalEditServices Services { get; }

        public IReadOnlyList<ThermalRowEditor> Editors => editors;

        /// <summary>True while a row is being edited: its scope is pinned, the panel does not rebuild, the mode cannot be switched.</summary>
        public bool IsPending => editors.Any(x => x.IsEdited);

        /// <summary>True while <see cref="Apply"/> runs: the model change it causes is this session's own and discards nothing.</summary>
        public bool IsApplying { get; private set; }

        /// <summary>A line about what happened to the edit (discarded because the model changed, could not be applied); null for none.</summary>
        public string Notice { get; private set; }

        /// <summary>The result of the last Apply (the concise line the panel shows afterwards); null until one, and cleared by the next edit.</summary>
        public ThermalChangeResult LastResult { get; private set; }

        /// <summary>The new warnings the pending change would introduce; null while there is no change to check.</summary>
        public ThermalCheckDiff Diff { get; private set; }

        /// <summary>Why the proposal could not be built (a conflict between rows, a failed core); null when it could, or when there is none.</summary>
        public string ProposalError { get; private set; }

        /// <summary>How many rows contribute a change.</summary>
        public int ChangeCount { get; private set; }

        /// <summary>How many elements those changes reach (each element once per change).</summary>
        public int ElementCount { get; private set; }

        /// <summary>Edited rows that cannot contribute a change yet (a target not typed, not reachable, still calculating).</summary>
        public int LeftOutCount { get; private set; }

        /// <summary>True while an edited row is still calculating.</summary>
        public bool IsBusy => editors.Any(x => x.IsEdited && x.IsBusy);

        /// <summary>Apply is possible: at least one change, a proposal that built, nothing still calculating.</summary>
        public bool CanApply => ChangeCount > 0 && ProposalError == null && !IsBusy && !IsApplying;

        /// <summary>The summary above Apply, e.g. "2 changes · 32 elements".</summary>
        public string SummaryText
        {
            get
            {
                if (ChangeCount == 0)
                {
                    return IsPending ? "No change to apply yet." : string.Empty;
                }

                string text = string.Format(CultureInfo.CurrentCulture, "{0} {1} · {2} {3}", ChangeCount, ChangeCount == 1 ? "change" : "changes", ElementCount, ElementCount == 1 ? "element" : "elements");
                return LeftOutCount > 0 ? text + string.Format(CultureInfo.CurrentCulture, " · {0} not ready", LeftOutCount) : text;
            }
        }

        /// <summary>The line of the check before Apply: "✓ No new warnings", "⚠ 3 new warnings"; null while there is nothing to check.</summary>
        public string CheckText => Diff == null ? null : Diff.Glyph + " " + Diff.Text;

        // ---- Rows -------------------------------------------------------------------------------------------------

        /// <summary>Replaces the rows on show (the previous ones, which are not edited, are dropped). Not while a row is edited.</summary>
        internal void SetRows(AnalyticalModel analyticalModel, IEnumerable<ThermalPerformanceRow> rows, ThermalPerformanceMode mode)
        {
            this.analyticalModel = analyticalModel;

            foreach (ThermalRowEditor editor in editors)
            {
                editor.Dispose();
            }

            editors.Clear();

            foreach (ThermalPerformanceRow row in rows ?? Enumerable.Empty<ThermalPerformanceRow>())
            {
                ThermalRowEditor editor = new ThermalRowEditor(this, row, analyticalModel, row.ElementGuids);
                row.Editor = editor;
                editors.Add(editor);
            }

            Reset();
        }

        /// <summary>A row's edit changed: rebuild the proposal and the check.</summary>
        internal void EditorChanged(ThermalRowEditor editor)
        {
            if (editor != null && editor.IsEdited)
            {
                // A new edit replaces the line of the last result and of any earlier notice.
                LastResult = null;
                Notice = null;
            }

            Rebuild();
        }

        /// <summary>The change set of every row that has a change, in the order of the rows; empty when there is none.</summary>
        public ThermalChangeSet BuildChangeSet()
        {
            ThermalChangeSet changeSet = new ThermalChangeSet();
            foreach (ThermalRowEditor editor in editors)
            {
                if (editor.IsEdited)
                {
                    editor.AddTo(changeSet);
                }
            }

            return changeSet;
        }

        // ---- Apply, Discard, Invalidate --------------------------------------------------------------------------

        /// <summary>
        /// Applies every pending change as one change set through <paramref name="applier"/> (which commits once: one Undo). On
        /// success the edits end and <see cref="LastResult"/> says what changed; on failure they stay, with the reason in
        /// <see cref="Notice"/>, so nothing the user typed is lost. Returns null when there was nothing to apply.
        /// </summary>
        public ThermalChangeResult Apply(Func<ThermalChangeSet, ThermalChangeResult> applier)
        {
            if (applier == null || !CanApply)
            {
                return null;
            }

            ThermalChangeSet changeSet = BuildChangeSet();
            if (changeSet.IsEmpty)
            {
                return null;
            }

            ThermalChangeResult result;
            IsApplying = true;
            try
            {
                result = applier(changeSet);
            }
            finally
            {
                IsApplying = false;
            }

            if (result == null || !result.Succeeded)
            {
                Notice = result?.Error ?? "The change could not be applied.";
                Raise();
                return result;
            }

            // The edits are done; the panel rebuilds from the new model (the host's next Update).
            foreach (ThermalRowEditor editor in editors)
            {
                editor.Dispose();
            }

            editors.Clear();
            Reset();
            LastResult = result;
            Raise();
            return result;
        }

        /// <summary>Applies the "Recalculate" action on its own (the stored thermal values refreshed, one Undo).</summary>
        public ThermalChangeResult Recalculate(Func<ThermalChangeSet, ThermalChangeResult> applier)
        {
            if (applier == null || IsPending)
            {
                return null;
            }

            ThermalChangeResult result;
            IsApplying = true;
            try
            {
                result = applier(new ThermalChangeSet() { RecalculateStoredValues = true });
            }
            finally
            {
                IsApplying = false;
            }

            Notice = result == null || !result.Succeeded ? result?.Error ?? "The values could not be recalculated." : null;
            LastResult = result != null && result.Succeeded ? result : null;
            Raise();
            return result;
        }

        /// <summary>Throws the pending edits away: the model and its history are untouched.</summary>
        public void Discard()
        {
            foreach (ThermalRowEditor editor in editors.ToList())
            {
                if (editor.IsEdited)
                {
                    editor.Clear();
                }
            }

            Notice = null;
            Reset();
        }

        /// <summary>Discards the pending edits because the model they were calculated for is gone, and says why.</summary>
        public void Invalidate(string reason)
        {
            bool pending = IsPending;
            foreach (ThermalRowEditor editor in editors.ToList())
            {
                if (editor.IsEdited)
                {
                    editor.Clear();
                }
            }

            Reset();
            LastResult = null;
            Notice = pending ? reason : null;
            Raise();
        }

        /// <summary>Forgets the line of the last result (the selection or the model moved on).</summary>
        internal void ClearResult()
        {
            if (LastResult != null || Notice != null)
            {
                LastResult = null;
                Notice = null;
                Raise();
            }
        }

        public void Dispose()
        {
            foreach (ThermalRowEditor editor in editors)
            {
                editor.Dispose();
            }

            editors.Clear();
            Services.Dispose();
        }

        // ---- Proposal and check ------------------------------------------------------------------------------------

        private void Reset()
        {
            Diff = null;
            ProposalError = null;
            ChangeCount = 0;
            ElementCount = 0;
            LeftOutCount = 0;
            Raise();
        }

        // Builds the proposed model from what the rows propose - on clones, with no Tas and no write - and checks it.
        private void Rebuild()
        {
            Diff = null;
            ProposalError = null;
            ChangeCount = 0;
            ElementCount = 0;

            List<ThermalRowEditor> edited = editors.Where(x => x.IsEdited).ToList();
            ThermalChangeSet changeSet = new ThermalChangeSet();
            List<ThermalRowEditor> contributing = edited.Where(x => x.AddTo(changeSet)).ToList();

            ChangeCount = contributing.Count;
            ElementCount = contributing.Sum(x => x.ElementCount);
            LeftOutCount = edited.Count - contributing.Count;

            if (changeSet.Count != 0 && analyticalModel != null)
            {
                AnalyticalModel proposed = Modify.ProposeThermalChange(analyticalModel, changeSet, null, out ThermalChangeResult result);
                if (proposed == null || result == null || !result.Succeeded)
                {
                    ProposalError = result?.Error ?? "The change could not be prepared.";
                }
                else
                {
                    Diff = Query.ThermalCheckDiff(analyticalModel, proposed, result);
                }
            }

            Raise();
        }

        private void Raise()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        }
    }
}
