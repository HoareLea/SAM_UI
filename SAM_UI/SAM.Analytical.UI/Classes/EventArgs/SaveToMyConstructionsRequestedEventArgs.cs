// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;

namespace SAM.Analytical.UI
{
    /// <summary>
    /// Raised by ConstructionLibraryWindow's "Save to My constructions..." button: the host (SAM.Analytical.UI.WPF, which owns "My constructions")
    /// asks for a name and saves a NEW construction there. The window is not closed and its own library is not changed: saving to My constructions
    /// never edits the constructions being edited here, and never the model.
    /// </summary>
    public class SaveToMyConstructionsRequestedEventArgs
    {
        public SaveToMyConstructionsRequestedEventArgs(Construction construction, MaterialLibrary materialLibrary)
        {
            Construction = construction;
            MaterialLibrary = materialLibrary;
        }

        /// <summary>The construction selected in the library window, as it is there now (authored, edited or imported).</summary>
        public Construction Construction { get; }

        /// <summary>The window's material library: the materials the construction's layers name.</summary>
        public MaterialLibrary MaterialLibrary { get; }

        public bool Handled { get; set; } = false;
    }
}
