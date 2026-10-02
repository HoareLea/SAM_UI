// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Query
    {
        // The strip of a floating window that must stay reachable on some monitor so the user can still grab and move it.
        private const double floatingTitleStripHeight = 32;
        private const double floatingMinVisibleWidth = 80;

        /// <summary>
        /// Where the floating Thermal Performance window opens (device-independent units, all rectangles in one coordinate
        /// space): the <paramref name="remembered"/> bounds when their title strip is still reachable on a monitor - so a window
        /// left on a second monitor reopens there, and one left on a monitor that has since been unplugged does not open
        /// off-screen - otherwise a default beside the right edge of the <paramref name="owner"/>, inside the monitor that
        /// holds most of it. Pure, so it is tested without a window.
        /// </summary>
        /// <param name="remembered">The bounds the window had when it was last closed or docked; null the first time.</param>
        /// <param name="owner">The bounds of the analytical window.</param>
        /// <param name="workAreas">The working area of every monitor (the first is the primary one).</param>
        /// <param name="defaultSize">The size of a first-time window.</param>
        public static Rect ThermalFloatingBounds(Rect? remembered, Rect owner, IReadOnlyList<Rect> workAreas, Size defaultSize)
        {
            List<Rect> areas = (workAreas ?? new List<Rect>()).Where(x => !x.IsEmpty && x.Width > 0 && x.Height > 0).ToList();
            if (areas.Count == 0)
            {
                areas.Add(owner);
            }

            if (remembered.HasValue && !remembered.Value.IsEmpty && remembered.Value.Width > 0 && remembered.Value.Height > 0)
            {
                Rect strip = new Rect(remembered.Value.Left, remembered.Value.Top, remembered.Value.Width, Math.Min(floatingTitleStripHeight, remembered.Value.Height));
                if (areas.Any(x => Visible(strip, x)))
                {
                    return remembered.Value;
                }
            }

            // The monitor that holds most of the owner; the primary one when the owner is on none.
            Rect area = areas.OrderByDescending(x => Overlap(owner, x)).First();
            if (Overlap(owner, area) <= 0)
            {
                area = areas[0];
            }

            double width = Math.Min(remembered?.Width > 0 ? remembered.Value.Width : defaultSize.Width, area.Width);
            double height = Math.Min(remembered?.Height > 0 ? remembered.Value.Height : defaultSize.Height, area.Height);

            double left = Math.Min(Math.Max(owner.Right - width - 24, area.Left), area.Right - width);
            double top = Math.Min(Math.Max(owner.Top + 140, area.Top), area.Bottom - height);

            return new Rect(left, top, width, height);
        }

        private static bool Visible(Rect strip, Rect area)
        {
            Rect intersection = Rect.Intersect(strip, area);
            return !intersection.IsEmpty && intersection.Width >= floatingMinVisibleWidth && intersection.Height >= Math.Min(strip.Height, 20);
        }

        private static double Overlap(Rect a, Rect b)
        {
            Rect intersection = Rect.Intersect(a, b);
            return intersection.IsEmpty ? 0 : intersection.Width * intersection.Height;
        }
    }
}
