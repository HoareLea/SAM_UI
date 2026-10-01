// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System.Collections.Generic;
using System.Windows;

namespace SAM.Analytical.UI.WPF.Windows
{
    // B0 docking spike (read-only): the Thermal Performance side panel as a column of the window's main Grid, beside the view
    // tabs, behind a GridSplitter. Shown and hidden by a ribbon toggle; follows the selection of the active view and the
    // model. It never writes the model. The hooks in the rest of the window are one call to RefreshThermalPanelSpike each.
    public partial class AnalyticalWindow
    {
        private const double thermalPanelSpikeDefaultWidth = 320;

        private double thermalPanelSpikeWidth = thermalPanelSpikeDefaultWidth;

        private void InitializeThermalPanelSpike()
        {
            RibbonToggleButton_ThermalPerformance.Checked += RibbonToggleButton_ThermalPerformance_Changed;
            RibbonToggleButton_ThermalPerformance.Unchecked += RibbonToggleButton_ThermalPerformance_Changed;
        }

        /// <summary>True while the panel is shown.</summary>
        internal bool ThermalPanelSpikeVisible => ThermalPerformanceControl.Visibility == Visibility.Visible;

        private void RibbonToggleButton_ThermalPerformance_Changed(object sender, RoutedEventArgs e)
        {
            SetThermalPanelSpikeVisible(RibbonToggleButton_ThermalPerformance.IsChecked == true);
        }

        internal void SetThermalPanelSpikeVisible(bool visible)
        {
            if (visible == ThermalPanelSpikeVisible)
            {
                return;
            }

            if (visible)
            {
                ColumnDefinition_ThermalPanel.Width = new GridLength(thermalPanelSpikeWidth);
                GridSplitter_ThermalPanel.Visibility = Visibility.Visible;
                ThermalPerformanceControl.Visibility = Visibility.Visible;
                RefreshThermalPanelSpike();
            }
            else
            {
                // Remember the width the user dragged to, so showing it again restores it.
                if (ColumnDefinition_ThermalPanel.ActualWidth > 0)
                {
                    thermalPanelSpikeWidth = ColumnDefinition_ThermalPanel.ActualWidth;
                }

                ThermalPerformanceControl.Visibility = Visibility.Collapsed;
                GridSplitter_ThermalPanel.Visibility = Visibility.Collapsed;
                ColumnDefinition_ThermalPanel.Width = new GridLength(0);
            }

            if (RibbonToggleButton_ThermalPerformance.IsChecked != visible)
            {
                RibbonToggleButton_ThermalPerformance.IsChecked = visible;
            }
        }

        // Follows the selection of the active view (3D or 2D) and the model; a no-op while the panel is hidden.
        private void RefreshThermalPanelSpike()
        {
            if (!ThermalPanelSpikeVisible)
            {
                return;
            }

            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            List<SAMObject> selected = analyticalModel == null ? null : GetActiveViewportControl()?.SelectedSAMObjects<SAMObject>();
            ThermalPerformanceControl.Update(analyticalModel, selected);
        }
    }
}
