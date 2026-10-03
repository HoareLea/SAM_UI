// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using SAM.Core.UI.WPF;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// The Thermal Performance panel over <see cref="ThermalPerformanceViewModel"/>: it shows the selected elements (or the whole
    /// envelope) by construction with the stored performance, and each row is an editor over the existing row view-models
    /// (<see cref="ThermalRowEditor"/>). It holds no editing logic and does not know its host: the host hands it the model and the
    /// selection (<see cref="Update"/>), the function that commits a change set (<see cref="Applier"/>), and acts on
    /// <see cref="HighlightRequested"/>. Docked in the analytical window or floating in a window of its own, it is this one control.
    /// </summary>
    public partial class ThermalPerformanceControl : UserControl, IDisposable
    {
        private readonly ThermalPerformanceViewModel viewModel;
        private bool updating;
        private bool isFloating;
        private bool settingColourState;

        public ThermalPerformanceControl()
            : this(null)
        {
        }

        /// <param name="services">The calculations the editing rows use; null for the real Tas ones (created on first edit). Tests pass stand-ins.</param>
        public ThermalPerformanceControl(ThermalEditServices services)
        {
            InitializeComponent();
            viewModel = new ThermalPerformanceViewModel(services);
            viewModel.Changed += ViewModel_Changed;
            viewModel.Session.Services.Sources.PropertyChanged += Sources_PropertyChanged;
            RenderSources();
            Render();
        }

        /// <summary>
        /// Asks the user for the file of a new source (the path, or null when cancelled). The open-file dialog by default; a host or a test
        /// can supply its own.
        /// </summary>
        public Func<string> PickSourceFile { get; set; } = PickSourceFileWithDialog;

        private static string PickSourceFileWithDialog()
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog()
            {
                Title = "Add a source of constructions and glazing systems",
                Filter = "Construction databases (*.tcd;*.json)|*.tcd;*.json|All files (*.*)|*.*",
                CheckFileExists = true,
            };

            return openFileDialog.ShowDialog() == true ? openFileDialog.FileName : null;
        }

        /// <summary>Raised when a row is clicked: the host highlights <see cref="ThermalHighlightRequestedEventArgs.Objects"/> in the active view.</summary>
        public event EventHandler<ThermalHighlightRequestedEventArgs> HighlightRequested;

        /// <summary>Raised when the user asks for the other host: a window of its own (<see cref="ThermalPerformanceHost.Floating"/>) or docked again.</summary>
        public event EventHandler<ThermalHostRequestedEventArgs> HostRequested;

        /// <summary>Raised when the user chooses what the active 3D view is coloured by (or Off): the host colours (or restores) it.</summary>
        public event EventHandler<ThermalColourRequestedEventArgs> ColourRequested;

        public ThermalPerformanceViewModel ViewModel => viewModel;

        /// <summary>
        /// Shows what the active view is coloured by. <paramref name="available"/> is false where it cannot be (no 3D view active);
        /// <paramref name="reason"/> then says why. <paramref name="isAvailable"/> says, per option, whether the view shows what the option
        /// colours (an option that is not available is listed but cannot be chosen). Setting it never raises <see cref="ColourRequested"/>.
        /// </summary>
        public void SetColourState(bool available, ThermalColourOption selected, Func<ThermalColourOption, bool> isAvailable = null, string reason = null)
        {
            selected ??= ThermalColourOption.Off;

            settingColourState = true;
            try
            {
                List<ColourChoice> choices = ThermalColourOption.All.Select(x => new ColourChoice(x, available && (isAvailable == null || isAvailable(x)))).ToList();
                comboBox_ColourBy.ItemsSource = choices;
                comboBox_ColourBy.SelectedItem = available ? choices.Find(x => x.Option == selected) ?? choices[0] : choices[0];
                comboBox_ColourBy.IsEnabled = available;
            }
            finally
            {
                settingColourState = false;
            }
            comboBox_ColourBy.ToolTip = available || string.IsNullOrEmpty(reason) ? "Colour the 3D view by a stored thermal property, with a legend. The model is not changed." : reason;
        }

        /// <summary>
        /// Commits a change set to the model as ONE change (one Undo) and returns its result; the host supplies it
        /// (<c>Modify.ApplyThermalChangeWithReports</c>). Without one the panel is read-only: Apply stays disabled.
        /// </summary>
        public Func<ThermalChangeSet, ThermalChangeResult> Applier { get; set; }

        /// <summary>
        /// True while a window of its own holds the control. It only changes how the control looks (the host button, and the edge
        /// that borders the viewport when docked); nothing else about the control depends on its host.
        /// </summary>
        public bool IsFloating
        {
            get => isFloating;
            set
            {
                isFloating = value;
                button_Host.Content = value ? "Dock" : "Undock";
                button_Host.ToolTip = value ? "Put the panel back beside the view tabs." : "Move the panel into its own window, for example onto a second monitor.";
                border_Host.BorderThickness = value ? new Thickness(0) : new Thickness(1, 0, 0, 0);
            }
        }

        /// <summary>
        /// Shows the selected panels / apertures of <paramref name="analyticalModel"/> by construction, or the whole envelope. While a
        /// row is being edited the rows stay as they are (the edit's scope is pinned); <paramref name="modelChanged"/> says the model
        /// was replaced by something other than the selection or a view setting, which discards a pending edit.
        /// </summary>
        public void Update(AnalyticalModel analyticalModel, IEnumerable<SAMObject> selected, bool modelChanged = false)
        {
            viewModel.Update(analyticalModel, selected, modelChanged);
        }

        /// <summary>
        /// Starts editing the row of <paramref name="elementGuids"/> (Stage F: the route of the 3D right-click "Set U-value..." / "Set glazing..."): the
        /// panel shows the selection (<see cref="ThermalPerformanceMode.Selection"/>, unless an edit is pending - then its pinned rows stay), and when the
        /// elements belong to ONE row of it, an aperture row opens <c>Change…</c> and an opaque row puts the cursor in its target U. Elements of several
        /// constructions start nothing: the person chooses the row. Nothing is calculated for the model and nothing is written. Returns the row's editor, or null.
        /// </summary>
        public ThermalRowEditor BeginEdit(IEnumerable<Guid> elementGuids)
        {
            HashSet<Guid> guids = new HashSet<Guid>(elementGuids ?? Enumerable.Empty<Guid>());
            if (guids.Count == 0)
            {
                return null;
            }

            if (!viewModel.Session.IsPending)
            {
                viewModel.Mode = ThermalPerformanceMode.Selection;
            }

            List<ThermalPerformanceRow> rows = viewModel.Groups.SelectMany(x => x.Rows).Where(x => x.SelectedGuids.Any(guids.Contains)).ToList();
            if (rows.Count != 1)
            {
                return null;
            }

            ThermalRowEditor editor = rows[0].Editor;
            if (editor == null || !editor.CanEdit)
            {
                return null;
            }

            if (editor.IsAperture)
            {
                editor.OpenChange();
            }

            // Once the row is laid out: bring it into view, and for an opaque row put the cursor in its target U.
            Dispatcher.BeginInvoke(new Action(() => FocusRow(editor)), System.Windows.Threading.DispatcherPriority.Loaded);
            return editor;
        }

        private void FocusRow(ThermalRowEditor editor)
        {
            string id = editor.IsAperture ? "listBox_Candidates" : "textBox_Target";
            FrameworkElement element = Descendants(itemsControl_Groups).OfType<FrameworkElement>().FirstOrDefault(x => ReferenceEquals(x.DataContext, editor) && System.Windows.Automation.AutomationProperties.GetAutomationId(x) == id);
            if (element == null)
            {
                return;
            }

            element.BringIntoView();
            if (!editor.IsAperture)
            {
                element.Focus();
            }
        }

        private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(root, i);
                yield return child;
                foreach (DependencyObject descendant in Descendants(child))
                {
                    yield return descendant;
                }
            }
        }

        /// <summary>Ends the Tas workers the editing rows started.</summary>
        public void Dispose()
        {
            viewModel.Session.Services.Sources.PropertyChanged -= Sources_PropertyChanged;
            viewModel.Dispose();
        }

        private void ViewModel_Changed(object sender, EventArgs e)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action(Render));
                return;
            }

            Render();
        }

        private void Render()
        {
            textBlock_Summary.Text = viewModel.Summary;

            // Only a rebuild (a new selection, mode or model) replaces the rows: re-assigning the same list would rebuild every row,
            // and with it the text box the user is typing in.
            if (!ReferenceEquals(itemsControl_Groups.ItemsSource, viewModel.Groups))
            {
                itemsControl_Groups.ItemsSource = viewModel.Groups;
            }

            updating = true;
            try
            {
                radioButton_Selection.IsChecked = viewModel.Mode == ThermalPerformanceMode.Selection;
                radioButton_WholeEnvelope.IsChecked = viewModel.Mode == ThermalPerformanceMode.WholeEnvelope;
                radioButton_Selection.IsEnabled = radioButton_WholeEnvelope.IsEnabled = viewModel.ModeSwitchEnabled;
            }
            finally
            {
                updating = false;
            }

            RenderApplyBar();
        }

        // The check before Apply, the summary and Discard / Apply while a change is pending; the one-line result afterwards.
        private void RenderApplyBar()
        {
            ThermalEditSession session = viewModel.Session;
            bool pending = session.IsPending;
            ThermalChangeResult result = session.LastResult;

            textBlock_Notice.Text = session.Notice ?? string.Empty;
            textBlock_Notice.Visibility = session.Notice == null ? Visibility.Collapsed : Visibility.Visible;

            string resultText = result == null ? string.Empty : result.Text;
            if (result != null && result.ReportLines.Count != 0)
            {
                resultText += Environment.NewLine + string.Join(Environment.NewLine, result.ReportLines);
            }

            textBlock_Result.Text = resultText;
            textBlock_Result.Visibility = result == null ? Visibility.Collapsed : Visibility.Visible;

            stackPanel_Pending.Visibility = pending ? Visibility.Visible : Visibility.Collapsed;
            border_Apply.Visibility = pending || session.Notice != null || result != null ? Visibility.Visible : Visibility.Collapsed;
            if (!pending)
            {
                return;
            }

            ThermalCheckDiff diff = session.Diff;
            if (session.ProposalError != null)
            {
                textBlock_Check.Text = "✕ " + session.ProposalError;
                textBlock_Check.Foreground = (Brush)FindResource("PartO.Brush.Danger");
            }
            else if (diff == null)
            {
                textBlock_Check.Text = session.IsBusy ? "Before apply: calculating…" : "Before apply: nothing to check yet.";
                textBlock_Check.Foreground = (Brush)FindResource("PartO.Brush.Muted");
            }
            else
            {
                textBlock_Check.Text = "Before apply: " + session.CheckText;
                textBlock_Check.Foreground = (Brush)FindResource(diff.NewErrors > 0 ? "PartO.Brush.Danger" : diff.NewWarnings > 0 ? "PartO.Brush.Warning" : "PartO.Brush.Success");
            }

            button_Details.Visibility = diff != null && !diff.Passed ? Visibility.Visible : Visibility.Collapsed;
            textBlock_Pinned.Text = "The elements are pinned: changing the selection does not change what Apply will modify.";
            textBlock_ChangeSummary.Text = session.SummaryText;
            button_Apply.IsEnabled = session.CanApply && Applier != null;
        }

        private void radioButton_Mode_Checked(object sender, RoutedEventArgs e)
        {
            if (updating || !IsInitialized)
            {
                return;
            }

            viewModel.Mode = radioButton_WholeEnvelope.IsChecked == true ? ThermalPerformanceMode.WholeEnvelope : ThermalPerformanceMode.Selection;
        }

        // ---- Sources ----------------------------------------------------------------------------------------------------

        // The catalog tells when a source is added, read, fails or is forgotten (it may be on the thread that read the file).
        private void Sources_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (Dispatcher.CheckAccess())
            {
                RenderSources();
            }
            else
            {
                Dispatcher.BeginInvoke(new Action(RenderSources));
            }
        }

        private void RenderSources()
        {
            ThermalSourceCatalog sources = viewModel.Session.Services.Sources;
            IReadOnlyList<ThermalSourceEntry> entries = sources.Entries;
            itemsControl_Sources.ItemsSource = null;
            itemsControl_Sources.ItemsSource = entries;
            textBlock_SourcesHint.Text = entries.Count == 0
                ? "Candidates come from the model and the default library."
                : "Candidates come from the model, the default library and:";
        }

        private void button_AddSource_Click(object sender, RoutedEventArgs e)
        {
            string path = PickSourceFile?.Invoke();
            if (!string.IsNullOrWhiteSpace(path))
            {
                _ = viewModel.Session.Services.Sources.AddAsync(path);
            }
        }

        private void button_ForgetSource_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { Tag: ThermalSourceEntry entry })
            {
                viewModel.Session.Services.Sources.Remove(entry);
            }
        }

        private void comboBox_ColourBy_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (settingColourState || comboBox_ColourBy.SelectedItem is not ColourChoice choice)
            {
                return;
            }

            ColourRequested?.Invoke(this, new ThermalColourRequestedEventArgs(choice.Option));
        }

        // One line of the selector: the option, and whether the active view shows what it colours.
        internal sealed class ColourChoice
        {
            public ColourChoice(ThermalColourOption option, bool isAvailable)
            {
                Option = option;
                IsAvailable = isAvailable || option == ThermalColourOption.Off;
            }

            public ThermalColourOption Option { get; }

            public bool IsAvailable { get; }

            public string Label => Option.Label;

            public string ToolTip => IsAvailable ? Option.ToolTip : "This view does not show these elements.";

            public override string ToString()
            {
                return Option.Label;
            }
        }

        private void button_Host_Click(object sender, RoutedEventArgs e)
        {
            HostRequested?.Invoke(this, new ThermalHostRequestedEventArgs(isFloating ? ThermalPerformanceHost.Docked : ThermalPerformanceHost.Floating));
        }

        private void button_Row_Click(object sender, RoutedEventArgs e)
        {
            if (!(((FrameworkElement)sender).Tag is ThermalPerformanceRow row))
            {
                return;
            }

            HighlightRequested?.Invoke(this, new ThermalHighlightRequestedEventArgs(row, viewModel.HighlightObjects(row)));
        }

        // ---- Editing --------------------------------------------------------------------------------------------

        private void button_Change_Click(object sender, RoutedEventArgs e)
        {
            Editor(sender)?.OpenChange();
        }

        private void button_CloseChange_Click(object sender, RoutedEventArgs e)
        {
            Editor(sender)?.CloseChange();
        }

        /// <summary>
        /// Shows the Glazing System Builder over its view-model and returns when it is closed. A modal window owned by this panel's window by default
        /// (the model cannot change while it is open); a test supplies its own. The panel disposes the view-model afterwards.
        /// </summary>
        public Func<GlazingBuilderViewModel, bool?> ShowBuilder { get; set; }

        private bool? ShowBuilderWithWindow(GlazingBuilderViewModel builder)
        {
            return GlazingSystemBuilderWindow.ShowModal(builder, System.Windows.Window.GetWindow(this));
        }

        // Create new…: the Builder edits a draft and saves to My glazing systems; the open list refreshes and chooses the new system by itself.
        private void button_CreateNew_Click(object sender, RoutedEventArgs e)
        {
            GlazingBuilderViewModel builder = Editor(sender)?.CreateBuilder();
            if (builder == null)
            {
                return;
            }

            try
            {
                (ShowBuilder ?? ShowBuilderWithWindow)(builder);
            }
            finally
            {
                builder.Dispose();
            }
        }

        // ---- My library ---------------------------------------------------------------------------------------------------

        /// <summary>
        /// Shows "My library" over its view-model and returns when it is closed. A modal window owned by this panel's window by default (a test
        /// supplies its own). The panel has subscribed to <see cref="UserLibraryViewModel.OpenInBuilderRequested"/> before this is called and disposes the
        /// view-model afterwards.
        /// </summary>
        public Func<UserLibraryViewModel, bool?> ShowLibrary { get; set; }

        /// <summary>Asks the user to confirm removing a system from My library (given the text to show); a message box by default, a test supplies its own.</summary>
        public Func<string, bool> ConfirmRemove { get; set; }

        private System.Windows.Window libraryWindow;

        private bool? ShowLibraryWithWindow(UserLibraryViewModel library)
        {
            UserLibraryWindow window = new UserLibraryWindow(library) { Owner = System.Windows.Window.GetWindow(this) };
            window.Confirm = ConfirmRemove;
            libraryWindow = window;
            try
            {
                return window.ShowDialog();
            }
            finally
            {
                libraryWindow = null;
            }
        }

        private bool ConfirmWithMessageBox(string text)
        {
            return MessageBox.Show(System.Windows.Window.GetWindow(this), text, "Remove from My library", MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.Cancel) == MessageBoxResult.OK;
        }

        // The Builder over a window of its own: the panel's, or "My library" while that is open (the Builder is modal on it).
        private bool? ShowBuilderOver(GlazingBuilderViewModel builder, System.Windows.Window owner)
        {
            if (ShowBuilder != null)
            {
                return ShowBuilder(builder);
            }

            return GlazingSystemBuilderWindow.ShowModal(builder, owner ?? System.Windows.Window.GetWindow(this));
        }

        /// <summary>Opens My library (optionally with a system selected, and renaming it); false when the host has no user library.</summary>
        public bool OpenLibrary(Guid? select = null, bool rename = false)
        {
            UserLibraryViewModel library = viewModel.CreateUserLibrary();
            if (library == null)
            {
                return false;
            }

            library.OpenInBuilderRequested += (sender, row) =>
            {
                GlazingBuilderViewModel builder = viewModel.CreateBuilder(row.ApertureConstruction);
                if (builder == null)
                {
                    return;
                }

                try
                {
                    ShowBuilderOver(builder, libraryWindow);
                }
                finally
                {
                    builder.Dispose();
                }
            };

            if (select != null)
            {
                library.SelectedRow = library.Rows.FirstOrDefault(x => x.Guid == select.Value);
                if (rename)
                {
                    library.BeginRename();
                }
            }

            try
            {
                (ShowLibrary ?? ShowLibraryWithWindow)(library);
            }
            finally
            {
                library.Dispose();
            }

            return true;
        }

        private void button_MyLibrary_Click(object sender, RoutedEventArgs e)
        {
            OpenLibrary();
        }

        // The candidate and the open list a context menu was opened on.
        private bool TryContextTarget(object sender, out GlazingCandidateRow row, out ThermalRowEditor editor)
        {
            GlazingCandidateRow candidate = (sender as MenuItem)?.DataContext as GlazingCandidateRow;
            row = candidate;
            editor = candidate == null ? null : viewModel.Groups.SelectMany(x => x.Rows).Select(x => x.Editor).FirstOrDefault(x => x != null && x.Candidates.Contains(candidate));
            return row != null && editor != null;
        }

        // New system based on this…: the Builder starts from the right-clicked candidate; choosing it for the row is not needed.
        private void menuItem_NewBasedOn_Click(object sender, RoutedEventArgs e)
        {
            if (!TryContextTarget(sender, out GlazingCandidateRow row, out ThermalRowEditor editor))
            {
                return;
            }

            GlazingBuilderViewModel builder = editor.CreateBuilder(row.Candidate);
            if (builder == null)
            {
                return;
            }

            try
            {
                ShowBuilderOver(builder, null);
            }
            finally
            {
                builder.Dispose();
            }
        }

        private void menuItem_RenameUser_Click(object sender, RoutedEventArgs e)
        {
            if (TryContextTarget(sender, out GlazingCandidateRow row, out _) && row.IsUserSystem)
            {
                OpenLibrary(row.Guid, true);
            }
        }

        private void menuItem_RemoveUser_Click(object sender, RoutedEventArgs e)
        {
            if (!TryContextTarget(sender, out GlazingCandidateRow row, out _) || !row.IsUserSystem)
            {
                return;
            }

            // Straight to the confirmation: no window is needed to remove one system. The open list follows the library's Changed event.
            using (UserLibraryViewModel library = viewModel.CreateUserLibrary())
            {
                UserLibraryEntryRow entry = library?.Rows.FirstOrDefault(x => x.Guid == row.Guid);
                if (entry != null)
                {
                    library.Remove(ConfirmRemove ?? ConfirmWithMessageBox, entry);
                }
            }
        }

        private void button_Recalculate_Click(object sender, RoutedEventArgs e)
        {
            if (Applier == null)
            {
                return;
            }

            if (viewModel.Session.IsPending)
            {
                // Recalculating rewrites stored values the pending edit was calculated from: not while one is pending.
                MessageBox.Show(System.Windows.Window.GetWindow(this), "Apply or discard the pending change first.", "Recalculate", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Waiting(() => viewModel.Recalculate(Applier));
        }

        private void button_Apply_Click(object sender, RoutedEventArgs e)
        {
            if (Applier == null)
            {
                return;
            }

            Waiting(() => viewModel.Apply(Applier));
        }

        private void button_Discard_Click(object sender, RoutedEventArgs e)
        {
            viewModel.Discard();
        }

        private void button_Details_Click(object sender, RoutedEventArgs e)
        {
            ThermalCheckDiff diff = viewModel.Session.Diff;
            if (diff == null)
            {
                return;
            }

            LogWindow logWindow = new LogWindow(diff.Log) { Owner = System.Windows.Window.GetWindow(this), Title = "New warnings before Apply" };
            logWindow.ShowDialog();
        }

        private static ThermalRowEditor Editor(object sender)
        {
            return (sender as FrameworkElement)?.DataContext as ThermalRowEditor;
        }

        // Apply and Recalculate run a whole-model Tas calculation: the cursor says so.
        private static void Waiting(Action action)
        {
            Cursor cursor = Mouse.OverrideCursor;
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                action();
            }
            finally
            {
                Mouse.OverrideCursor = cursor;
            }
        }
    }

    /// <summary>The user asked for another host of the Thermal Performance panel.</summary>
    public sealed class ThermalHostRequestedEventArgs : EventArgs
    {
        internal ThermalHostRequestedEventArgs(ThermalPerformanceHost host)
        {
            Host = host;
        }

        public ThermalPerformanceHost Host { get; }
    }

    /// <summary>The user chose what the active 3D view is coloured by; <see cref="ThermalColourOption.Off"/> shows it as saved.</summary>
    public sealed class ThermalColourRequestedEventArgs : EventArgs
    {
        internal ThermalColourRequestedEventArgs(ThermalColourOption option)
        {
            Option = option ?? ThermalColourOption.Off;
        }

        public ThermalColourOption Option { get; }
    }

    /// <summary>A click on a Thermal Performance row: the elements to highlight.</summary>
    public sealed class ThermalHighlightRequestedEventArgs : EventArgs
    {
        internal ThermalHighlightRequestedEventArgs(ThermalPerformanceRow row, List<SAMObject> objects)
        {
            Row = row;
            Objects = objects;
        }

        public ThermalPerformanceRow Row { get; }

        public List<SAMObject> Objects { get; }
    }
}
