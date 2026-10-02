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

        public ThermalPerformanceControl()
        {
            InitializeComponent();
            viewModel.Changed += ViewModel_Changed;
            Render();
        }

        /// <summary>Raised when a row is clicked: the host highlights <see cref="ThermalHighlightRequestedEventArgs.Objects"/> in the active view.</summary>
        public event EventHandler<ThermalHighlightRequestedEventArgs> HighlightRequested;

        public ThermalPerformanceViewModel ViewModel => viewModel;

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

        private void button_Row_Click(object sender, RoutedEventArgs e)
        {
            if (!(((FrameworkElement)sender).Tag is ThermalPerformanceRow row))
            {
                return;
            }

            HighlightRequested?.Invoke(this, new ThermalHighlightRequestedEventArgs(row, viewModel.HighlightObjects(row)));
        }
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
