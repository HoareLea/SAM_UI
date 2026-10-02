// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System.Collections.Generic;
using System.Windows;

namespace SAM.Analytical.UI.WPF.Windows
{
    // The Thermal Performance panel (Stage B, read-only), docked as a column of the window's main Grid beside the view tabs,
    // behind a GridSplitter (B0: Thermal-B0-DockingSpike.md). Shown and hidden by a ribbon toggle; follows the selection of the
    // active view and the model; a click on a row highlights its elements in the view. It never writes the model. The hooks in
    // the rest of the window are one call to RefreshThermalPerformance each (selection, model modified, tab changed).
    public partial class AnalyticalWindow
    {
        private const double thermalPerformanceDefaultWidth = 320;

        private double thermalPerformanceWidth = thermalPerformanceDefaultWidth;

        private void InitializeThermalPerformance()
        {
            RibbonToggleButton_ThermalPerformance.Checked += RibbonToggleButton_ThermalPerformance_Changed;
            RibbonToggleButton_ThermalPerformance.Unchecked += RibbonToggleButton_ThermalPerformance_Changed;
            ThermalPerformancePanel.HighlightRequested += ThermalPerformancePanel_HighlightRequested;
        }

        /// <summary>True while the panel is shown.</summary>
        internal bool ThermalPerformanceVisible => ThermalPerformancePanel.Visibility == Visibility.Visible;

        private void RibbonToggleButton_ThermalPerformance_Changed(object sender, RoutedEventArgs e)
        {
            SetThermalPerformanceVisible(RibbonToggleButton_ThermalPerformance.IsChecked == true);
        }

        internal void SetThermalPerformanceVisible(bool visible)
        {
            if (visible == ThermalPerformanceVisible)
            {
                return;
            }

            if (visible)
            {
                ColumnDefinition_ThermalPerformance.Width = new GridLength(thermalPerformanceWidth);
                GridSplitter_ThermalPerformance.Visibility = Visibility.Visible;
                ThermalPerformancePanel.Visibility = Visibility.Visible;
                RefreshThermalPerformance();
            }
            else
            {
                // Remember the width the user dragged to, so showing it again restores it.
                if (ColumnDefinition_ThermalPerformance.ActualWidth > 0)
                {
                    thermalPerformanceWidth = ColumnDefinition_ThermalPerformance.ActualWidth;
                }

                ThermalPerformancePanel.Visibility = Visibility.Collapsed;
                GridSplitter_ThermalPerformance.Visibility = Visibility.Collapsed;
                ColumnDefinition_ThermalPerformance.Width = new GridLength(0);
            }

            if (RibbonToggleButton_ThermalPerformance.IsChecked != visible)
            {
                RibbonToggleButton_ThermalPerformance.IsChecked = visible;
            }
        }

        // Follows the selection of the active view (3D or 2D) and the model; a no-op while the panel is hidden.
        private void RefreshThermalPerformance()
        {
            if (!ThermalPerformanceVisible)
            {
                return;
            }

            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            List<SAMObject> selected = analyticalModel == null ? null : GetActiveViewportControl()?.SelectedSAMObjects<SAMObject>();
            ThermalPerformancePanel.Update(analyticalModel, selected);
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
