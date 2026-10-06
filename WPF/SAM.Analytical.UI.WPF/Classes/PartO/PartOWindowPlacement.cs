// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Windows;
using System.Windows.Media;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Keeps a Part O window's action row inside the working area of the monitor it is on. Shared by the
    /// Hub and the Review iteration window: both carry their decision buttons at the bottom, and both were
    /// seen live opening with those buttons below the taskbar. The arithmetic is the pure
    /// <see cref="PartOWorkflowWindow.Placement"/>, tested there.
    /// </summary>
    internal static class PartOWindowPlacement
    {
        /// <summary>
        /// Lowers the window's height ceiling (and, where needed, its minimum) to fit the monitor it is on, then
        /// moves it up only as far as its bottom edge needs. Nothing is ever raised.
        /// </summary>
        internal static void KeepOnScreen(System.Windows.Window window)
        {
            if (window is null)
            {
                return;
            }

            System.IntPtr handle = new System.Windows.Interop.WindowInteropHelper(window).Handle;
            if (handle == System.IntPtr.Zero)
            {
                return;
            }

            System.Drawing.Rectangle rectangle = System.Windows.Forms.Screen.FromHandle(handle).WorkingArea;

            Matrix matrix = PresentationSource.FromVisual(window)?.CompositionTarget?.TransformFromDevice ?? Matrix.Identity;

            Point point_Top = matrix.Transform(new Point(rectangle.Left, rectangle.Top));
            Point point_Bottom = matrix.Transform(new Point(rectangle.Right, rectangle.Bottom));

            (double top, double minHeight, double maxHeight) = PartOWorkflowWindow.Placement(window.Top, window.ActualHeight, window.MinHeight, window.MaxHeight, point_Top.Y, point_Bottom.Y);

            //The minimum first: WPF resolves a ceiling below the floor by holding the floor.
            window.MinHeight = minHeight;
            window.MaxHeight = maxHeight;
            window.Top = top;
        }
    }
}
