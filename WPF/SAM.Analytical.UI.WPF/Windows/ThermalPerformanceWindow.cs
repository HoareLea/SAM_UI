// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Windows;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// The floating host of the Thermal Performance panel: a modeless window that holds a <see cref="ThermalPerformanceControl"/>
    /// and nothing else. It owns no state of the panel and no logic: the analytical window still hands the control the model
    /// and the selection and handles its events, exactly as when the control is docked. Owned by the analytical window, so it
    /// stays above it, minimises with it and closes with it; it is not shown in the task bar.
    /// </summary>
    public sealed class ThermalPerformanceWindow : System.Windows.Window
    {
        public ThermalPerformanceWindow(ThermalPerformanceControl control)
        {
            Title = "Thermal performance";
            Content = control ?? throw new ArgumentNullException(nameof(control));
            ShowInTaskbar = false;
            ShowActivated = false;
            ResizeMode = ResizeMode.CanResizeWithGrip;
            MinWidth = 260;
            MinHeight = 240;
            WindowStartupLocation = WindowStartupLocation.Manual;
            Width = 340;
            Height = 600;
        }

        /// <summary>Raised when the user closes the window with its own close button: the panel is then hidden, not docked.</summary>
        public event EventHandler ClosedByUser;

        /// <summary>True while the host closes the window itself (docking or hiding), so that is not taken for the user closing it.</summary>
        internal bool ClosingByHost { get; set; }

        /// <summary>The bounds the window has now, in device-independent units (the restored bounds when it is minimised).</summary>
        internal Rect CurrentBounds => WindowState == WindowState.Normal ? new Rect(Left, Top, ActualWidth > 0 ? ActualWidth : Width, ActualHeight > 0 ? ActualHeight : Height) : RestoreBounds;

        /// <summary>True once the window has closed (a closed window cannot be closed or shown again).</summary>
        internal bool IsClosed { get; private set; }

        protected override void OnClosed(EventArgs e)
        {
            IsClosed = true;
            base.OnClosed(e);

            if (!ClosingByHost)
            {
                ClosedByUser?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
