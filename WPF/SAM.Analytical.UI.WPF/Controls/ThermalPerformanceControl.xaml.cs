// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// The Thermal Performance panel (Stage B, read-only) over <see cref="ThermalPerformanceViewModel"/>. It only displays: the
    /// host hands it the model and the selection (<see cref="Update"/>) and acts on <see cref="HighlightRequested"/>. Hosted
    /// docked in the analytical window; a modeless tool window can host the same control.
    /// </summary>
    public partial class ThermalPerformanceControl : UserControl
    {
        private readonly ThermalPerformanceViewModel viewModel = new ThermalPerformanceViewModel();
        private bool updating;
        private bool isFloating;

        public ThermalPerformanceControl()
        {
            InitializeComponent();
            viewModel.Changed += ViewModel_Changed;
            Render();
        }

        /// <summary>Raised when a row is clicked: the host highlights <see cref="ThermalHighlightRequestedEventArgs.Objects"/> in the active view.</summary>
        public event EventHandler<ThermalHighlightRequestedEventArgs> HighlightRequested;

        /// <summary>Raised when the user asks for the other host: a window of its own (<see cref="ThermalPerformanceHost.Floating"/>) or docked again.</summary>
        public event EventHandler<ThermalHostRequestedEventArgs> HostRequested;

        public ThermalPerformanceViewModel ViewModel => viewModel;

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

        /// <summary>Shows the selected panels / apertures of <paramref name="analyticalModel"/> by construction, or the whole envelope.</summary>
        public void Update(AnalyticalModel analyticalModel, IEnumerable<SAMObject> selected)
        {
            viewModel.Update(analyticalModel, selected);
        }

        private void ViewModel_Changed(object sender, EventArgs e)
        {
            Render();
        }

        private void Render()
        {
            textBlock_Summary.Text = viewModel.Summary;
            itemsControl_Groups.ItemsSource = viewModel.Groups;

            updating = true;
            try
            {
                radioButton_Selection.IsChecked = viewModel.Mode == ThermalPerformanceMode.Selection;
                radioButton_WholeEnvelope.IsChecked = viewModel.Mode == ThermalPerformanceMode.WholeEnvelope;
            }
            finally
            {
                updating = false;
            }
        }

        private void radioButton_Mode_Checked(object sender, RoutedEventArgs e)
        {
            if (updating || !IsInitialized)
            {
                return;
            }

            viewModel.Mode = radioButton_WholeEnvelope.IsChecked == true ? ThermalPerformanceMode.WholeEnvelope : ThermalPerformanceMode.Selection;
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
