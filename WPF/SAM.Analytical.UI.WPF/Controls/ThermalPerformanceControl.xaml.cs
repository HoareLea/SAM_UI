// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using SAM.Core.UI.WPF;
using System;
using System.Collections.Generic;
using System.Globalization;
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
            Render();
        }

        /// <summary>Raised when a row is clicked: the host highlights <see cref="ThermalHighlightRequestedEventArgs.Objects"/> in the active view.</summary>
        public event EventHandler<ThermalHighlightRequestedEventArgs> HighlightRequested;

        /// <summary>Raised when the user asks for the other host: a window of its own (<see cref="ThermalPerformanceHost.Floating"/>) or docked again.</summary>
        public event EventHandler<ThermalHostRequestedEventArgs> HostRequested;

        /// <summary>Raised when the user turns "Colour by U-value" on or off: the host colours (or restores) the active 3D view.</summary>
        public event EventHandler<ThermalColourRequestedEventArgs> ColourRequested;

        public ThermalPerformanceViewModel ViewModel => viewModel;

        /// <summary>
        /// Shows whether the active view is coloured by U-value. <paramref name="available"/> is false where it cannot be (no 3D view
        /// active, or it does not show panels); <paramref name="reason"/> then says why. Setting it never raises <see cref="ColourRequested"/>.
        /// </summary>
        public void SetColourState(bool available, bool coloured, string reason = null)
        {
            settingColourState = true;
            try
            {
                toggleButton_ColourByU.IsEnabled = available;
                toggleButton_ColourByU.IsChecked = available && coloured;
            }
            finally
            {
                settingColourState = false;
            }
            toggleButton_ColourByU.ToolTip = available || string.IsNullOrEmpty(reason) ? "Colour the panels of the 3D view by their U-value, with a legend. The model is not changed." : reason;
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

        /// <summary>Ends the Tas workers the editing rows started.</summary>
        public void Dispose()
        {
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
                textBlock_Check.Text = "âœ• " + session.ProposalError;
                textBlock_Check.Foreground = (Brush)FindResource("PartO.Brush.Danger");
            }
            else if (diff == null)
            {
                textBlock_Check.Text = session.IsBusy ? "Before apply: calculatingâ€¦" : "Before apply: nothing to check yet.";
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

        // Checked / Unchecked rather than Click, so the request comes from the mouse, the keyboard and UI Automation alike.
        private void toggleButton_ColourByU_Changed(object sender, RoutedEventArgs e)
        {
            if (settingColourState)
            {
                return;
            }

            ColourRequested?.Invoke(this, new ThermalColourRequestedEventArgs(toggleButton_ColourByU.IsChecked == true));
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

    /// <summary>The user turned "Colour by U-value" on or off.</summary>
    public sealed class ThermalColourRequestedEventArgs : EventArgs
    {
        internal ThermalColourRequestedEventArgs(bool on)
        {
            On = on;
        }

        public bool On { get; }
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
