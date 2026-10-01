// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.UI
{
    /// <summary>
    /// Raised by ApertureConstructionLibraryWindow's "Set glazing..." button: the host (SAM.Analytical.UI.WPF, which owns the
    /// "Set glazing" window) decides how to hand over, and closes the library window when it does.
    /// </summary>
    public class SetGlazingRequestedEventArgs
    {
        public SetGlazingRequestedEventArgs(ApertureConstruction apertureConstruction)
        {
            ApertureConstruction = apertureConstruction;
        }

        /// <summary>The aperture construction selected in the library window.</summary>
        public ApertureConstruction ApertureConstruction { get; }

        public bool Handled { get; set; } = false;
    }
}
