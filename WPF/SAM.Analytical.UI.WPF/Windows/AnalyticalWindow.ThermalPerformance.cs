// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SAM.Analytical.UI.WPF.Windows
{
    // The Thermal Performance panel (Stage B, read-only). Its host is hybrid: DOCKED by default, as a column of the window's main
    // Grid beside the view tabs behind a GridSplitter (B0: Thermal-B0-DockingSpike.md), or FLOATING in a modeless
    // ThermalPerformanceWindow owned by this window (Thermal-StageB-Hosts.md). It is the one ThermalPerformanceControl either way:
    // this window hands it the model and the selection and handles its events, so nothing about the panel is duplicated between
    // hosts. Shown and hidden by a ribbon toggle; follows the selection of the active view and the model; a click on a row
    // highlights its elements in the view. It never writes the model. The hooks in the rest of the window are one call to
    // RefreshThermalPerformance each (selection, model modified, tab changed).
    public partial class AnalyticalWindow
    {
        private const double thermalPerformanceDefaultWidth = 320;

        private double thermalPerformanceWidth = thermalPerformanceDefaultWidth;
        private ThermalPerformanceHost thermalPerformanceHost = ThermalPerformanceHost.Hidden;
        private ThermalPerformanceHost thermalPerformanceLastHost = ThermalPerformanceHost.Docked;
        private ThermalPerformanceWindow thermalPerformanceWindow;
        private Rect? thermalPerformanceFloatBounds;
        private Grid thermalPerformanceGrid;
        private bool thermalPerformanceOwnerClosed;

        private void InitializeThermalPerformance()
        {
            thermalPerformanceGrid = ThermalPerformancePanel.Parent as Grid;

            RibbonToggleButton_ThermalPerformance.Checked += RibbonToggleButton_ThermalPerformance_Changed;
            RibbonToggleButton_ThermalPerformance.Unchecked += RibbonToggleButton_ThermalPerformance_Changed;
            ThermalPerformancePanel.HighlightRequested += ThermalPerformancePanel_HighlightRequested;
            ThermalPerformancePanel.HostRequested += ThermalPerformancePanel_HostRequested;
            ThermalPerformancePanel.ColourRequested += ThermalPerformancePanel_ColourRequested;
            ThermalPerformancePanel.Applier = x => uIAnalyticalModel?.ApplyThermalChangeWithReports(x);
            Closed += (sender, e) =>
            {
                thermalPerformanceOwnerClosed = true;
                ThermalPerformancePanel.Dispose();
            };
        }

        /// <summary>True while the panel is shown, docked or floating.</summary>
        internal bool ThermalPerformanceVisible => thermalPerformanceHost != ThermalPerformanceHost.Hidden;

        /// <summary>Where the panel is shown now.</summary>
        internal ThermalPerformanceHost ThermalPerformanceHostMode => thermalPerformanceHost;

        private void RibbonToggleButton_ThermalPerformance_Changed(object sender, RoutedEventArgs e)
        {
            SetThermalPerformanceVisible(RibbonToggleButton_ThermalPerformance.IsChecked == true);
        }

        /// <summary>Shows the panel in the host it was last in (docked the first time), or hides it.</summary>
        internal void SetThermalPerformanceVisible(bool visible)
        {
            SetThermalPerformanceHost(visible ? thermalPerformanceLastHost : ThermalPerformanceHost.Hidden);
        }

        /// <summary>Moves the panel to <paramref name="host"/>: the one control changes parent, nothing else.</summary>
        internal void SetThermalPerformanceHost(ThermalPerformanceHost host)
        {
            if (host == thermalPerformanceHost)
            {
                return;
            }

            LeaveThermalPerformanceHost();
            thermalPerformanceHost = host;

            switch (host)
            {
                case ThermalPerformanceHost.Docked:
                    ShowThermalPerformanceDocked();
                    break;

                case ThermalPerformanceHost.Floating:
                    ShowThermalPerformanceFloating();
                    break;
            }

            if (host != ThermalPerformanceHost.Hidden)
            {
                thermalPerformanceLastHost = host;
            }

            bool visible = host != ThermalPerformanceHost.Hidden;
            if (RibbonToggleButton_ThermalPerformance.IsChecked != visible)
            {
                RibbonToggleButton_ThermalPerformance.IsChecked = visible;
            }

            RefreshThermalPerformance();
        }

        // Takes the panel out of the host it is in: after this it sits, collapsed, in the main Grid (its home), whichever host it left.
        private void LeaveThermalPerformanceHost()
        {
            switch (thermalPerformanceHost)
            {
                case ThermalPerformanceHost.Docked:
                    // Remember the width the user dragged to, so docking it again restores it.
                    if (ColumnDefinition_ThermalPerformance.ActualWidth > 0)
                    {
                        thermalPerformanceWidth = ColumnDefinition_ThermalPerformance.ActualWidth;
                    }

                    break;

                case ThermalPerformanceHost.Floating:
                    ThermalPerformanceWindow window = thermalPerformanceWindow;
                    thermalPerformanceWindow = null;
                    if (window != null)
                    {
                        thermalPerformanceFloatBounds = window.CurrentBounds;
                        window.ClosingByHost = true;
                        window.Content = null;
                        if (!window.IsClosed)
                        {
                            window.Close();
                        }
                    }

                    ThermalPerformancePanel.IsFloating = false;
                    if (!thermalPerformanceGrid.Children.Contains(ThermalPerformancePanel))
                    {
                        thermalPerformanceGrid.Children.Add(ThermalPerformancePanel);
                    }

                    break;
            }

            ThermalPerformancePanel.Visibility = Visibility.Collapsed;
            GridSplitter_ThermalPerformance.Visibility = Visibility.Collapsed;
            ColumnDefinition_ThermalPerformance.Width = new GridLength(0);
        }

        private void ShowThermalPerformanceDocked()
        {
            ColumnDefinition_ThermalPerformance.Width = new GridLength(thermalPerformanceWidth);
            GridSplitter_ThermalPerformance.Visibility = Visibility.Visible;
            ThermalPerformancePanel.Visibility = Visibility.Visible;
        }

        private void ShowThermalPerformanceFloating()
        {
            // The viewport takes the column back: the control leaves the Grid before the window adopts it.
            thermalPerformanceGrid.Children.Remove(ThermalPerformancePanel);
            ThermalPerformancePanel.Visibility = Visibility.Visible;
            ThermalPerformancePanel.IsFloating = true;

            ThermalPerformanceWindow window = new ThermalPerformanceWindow(ThermalPerformancePanel) { Owner = this };
            Rect bounds = FloatingThermalPerformanceBounds(window);
            window.Left = bounds.Left;
            window.Top = bounds.Top;
            window.Width = bounds.Width;
            window.Height = bounds.Height;
            window.ClosedByUser += ThermalPerformanceWindow_ClosedByUser;

            thermalPerformanceWindow = window;
            window.Show();
        }

        // Where the floating window opens, in device-independent units: where it was last, if that is still on a monitor.
        private Rect FloatingThermalPerformanceBounds(System.Windows.Window window)
        {
            Matrix fromDevice = PresentationSource.FromVisual(this)?.CompositionTarget?.TransformFromDevice ?? Matrix.Identity;

            Point topLeft = fromDevice.Transform(PointToScreen(new Point(0, 0)));
            Rect owner = new Rect(topLeft.X, topLeft.Y, ActualWidth, ActualHeight);

            List<Rect> workAreas = System.Windows.Forms.Screen.AllScreens
                .OrderByDescending(x => x.Primary)
                .Select(x => new Rect(fromDevice.Transform(new Point(x.WorkingArea.Left, x.WorkingArea.Top)), fromDevice.Transform(new Point(x.WorkingArea.Right, x.WorkingArea.Bottom))))
                .ToList();

            return Query.ThermalFloatingBounds(thermalPerformanceFloatBounds, owner, workAreas, new Size(window.Width, Math.Max(360, Math.Min(window.Height, owner.Height - 200))));
        }

        // The window's own close button hides the panel (its toggle goes off); Dock puts it back beside the view tabs.
        private void ThermalPerformanceWindow_ClosedByUser(object sender, EventArgs e)
        {
            if (!(sender is ThermalPerformanceWindow window) || window != thermalPerformanceWindow)
            {
                return;
            }

            // Deferred: closing this window's owner closes it too, and then there is nothing to hide.
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (!thermalPerformanceOwnerClosed && window == thermalPerformanceWindow)
                {
                    SetThermalPerformanceHost(ThermalPerformanceHost.Hidden);
                }
            }));
        }

        private void ThermalPerformancePanel_HostRequested(object sender, ThermalHostRequestedEventArgs e)
        {
            SetThermalPerformanceHost(e.Host);
        }

        // Follows the selection of the active view (3D or 2D) and the model; a no-op while the panel is hidden.
        // modelChanged: the model was replaced by something that may have changed it (not just a selection, tab or view setting); a
        // pending edit in the panel is then discarded, unless the change is the panel's own Apply.
        private void RefreshThermalPerformance(bool modelChanged = false)
        {
            if (!ThermalPerformanceVisible)
            {
                return;
            }

            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            List<SAMObject> selected = analyticalModel == null ? null : GetActiveViewportControl()?.SelectedSAMObjects<SAMObject>();
            ThermalPerformancePanel.Update(analyticalModel, selected, modelChanged);
            RefreshThermalColourState();
        }

        // "Colour by": colours the active 3D view by a stored thermal property (ThermalColourOption). View state only (see
        // AnalyticalWindow.ParameterColouring.cs): no model write, no Undo entry. The state belongs to a view, so the selector follows the active tab.
        private void ThermalPerformancePanel_ColourRequested(object sender, ThermalColourRequestedEventArgs e)
        {
            Guid viewGuid = ActiveThreeDimensionalViewGuid(out Func<Type, bool> viewShows);
            if (viewGuid != Guid.Empty && e.Option.IsAvailableIn(viewShows))
            {
                SetParameterColouring(viewGuid, e.Option.Colouring);
            }

            RefreshThermalColourState();
        }

        private void RefreshThermalColourState()
        {
            if (!ThermalPerformanceVisible)
            {
                return;
            }

            Guid viewGuid = ActiveThreeDimensionalViewGuid(out Func<Type, bool> viewShows);
            bool available = viewGuid != Guid.Empty;
            ThermalPerformancePanel.SetColourState(available, available ? ThermalColourOption.Of(ParameterColouringOf(viewGuid)) : null, x => x.IsAvailableIn(viewShows), available ? null : "Colouring is available in a 3D view.");
        }

        // A click on a row: select its elements in the active view (the normal selection, so the panel then follows it).
        private void ThermalPerformancePanel_HighlightRequested(object sender, ThermalHighlightRequestedEventArgs e)
        {
            if (e?.Objects == null || e.Objects.Count == 0)
            {
                return;
            }

            GetActiveViewportControl()?.Select(e.Objects);
        }
    }
}
