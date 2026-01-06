// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Drawing;

namespace SAM.Core.Mollier.UI
{
    public static partial class Query
    {
        /// <summary>
        /// Creates rectangle by 2 corner points
        /// </summary>
        /// <param name="p1">Point 1</param>
        /// <param name="p2">Point 2</param>
        /// <returns>Rectangle</returns>
        public static Rectangle Rectangle(Point p1, Point p2)
        {
            return new Rectangle(System.Math.Min(p1.X, p2.X), System.Math.Min(p1.Y, p2.Y), System.Math.Abs(p1.X - p2.X), System.Math.Abs(p1.Y - p2.Y));
        }

    }
}
