// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.UI.WPF
{
    /// <summary>Where a glazing candidate comes from. Candidates are always complete aperture constructions.</summary>
    public enum GlazingSourceKind
    {
        /// <summary>An aperture construction already in the model.</summary>
        Model,

        /// <summary>The default aperture construction library (Edit > Aperture Constructions defaults).</summary>
        Library,

        /// <summary>Loaded for this window only ("Load more glazing..."); never written to the model until Apply.</summary>
        Loaded,
    }
}
