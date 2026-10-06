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

        /// <summary>
        /// "My glazing systems": the user's own predefined systems (<see cref="UserGlazingLibrary"/>), never written to the model until Apply. In the
        /// pool it comes after the model and the default library and before the loaded sources (<see cref="GlazingSource.Rank"/>). Also the kind of
        /// "My constructions" (<see cref="UserConstructionLibrary"/>), the opaque twin, which stands in the same place in the opaque pool.
        /// </summary>
        User,
    }
}
