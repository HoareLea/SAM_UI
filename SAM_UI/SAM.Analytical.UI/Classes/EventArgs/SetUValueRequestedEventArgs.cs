// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.UI
{
    /// <summary>
    /// Raised by ConstructionLibraryWindow's "Set U-value..." button: the host (SAM.Analytical.UI.WPF, which owns the
    /// "Set U-value" window) decides how to hand over, and closes the library window when it does.
    /// </summary>
    public class SetUValueRequestedEventArgs
    {
        public SetUValueRequestedEventArgs(Construction construction)
        {
            Construction = construction;
        }

        /// <summary>The construction selected in the library window.</summary>
        public Construction Construction { get; }

        public bool Handled { get; set; } = false;
    }
}
