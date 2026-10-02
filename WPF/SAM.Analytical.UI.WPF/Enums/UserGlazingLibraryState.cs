// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.UI.WPF
{
    /// <summary>What reading the user glazing library ("My glazing systems") found.</summary>
    public enum UserGlazingLibraryState
    {
        /// <summary>No library file yet (nothing has been saved): an empty library.</summary>
        Missing,

        /// <summary>The file was read.</summary>
        Ready,

        /// <summary>The file exists but is not a readable glazing library; it is never overwritten.</summary>
        Unreadable,
    }
}
