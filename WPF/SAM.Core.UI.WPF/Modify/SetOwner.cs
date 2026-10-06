// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Windows;
using System.Windows.Interop;

namespace SAM.Core.UI.WPF
{
    public static partial class Modify
    {
        /// <summary>
        /// Safely assigns <paramref name="owner"/> as the owner of <paramref name="window"/>.
        /// WPF throws InvalidOperationException ("Cannot set Owner property to a Window that has
        /// been closed") when the owner has already been closed; a closed WPF window has a native
        /// handle of IntPtr.Zero, so we skip the assignment in that case. Returns true when the
        /// owner was set.
        /// </summary>
        public static bool SetOwner(this Window window, Window owner)
        {
            if (window == null || owner == null)
            {
                return false;
            }

            // A window that has been closed (or not yet shown) has no live native handle.
            if (new WindowInteropHelper(owner).Handle == IntPtr.Zero)
            {
                return false;
            }

            try
            {
                window.Owner = owner;
                return true;
            }
            catch (InvalidOperationException)
            {
                // Owner was closed between the handle check and the assignment.
                return false;
            }
        }
    }
}
