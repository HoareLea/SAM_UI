// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.UI.WPF
{
    /// <summary>What a construction saved to "My constructions" was saved from (<see cref="UserConstructionProvenance.SavedFrom"/>).</summary>
    public enum UserConstructionOrigin
    {
        /// <summary>The thickness variant the U-value calculation generates for a target (not in any model yet).</summary>
        GeneratedVariant,

        /// <summary>A construction of the model: the row's current construction, or an existing model construction chosen as an alternative.</summary>
        Model,

        /// <summary>A construction of the SAM default library.</summary>
        DefaultLibrary,

        /// <summary>A construction of a source added with "Add source…" (the file name is recorded, never its folder).</summary>
        AddedSource,

        /// <summary>A construction already in "My constructions" (saved again as a new one).</summary>
        MyConstructions,

        /// <summary>A construction of the classic Constructions editor (an authored or imported construction).</summary>
        ConstructionEditor,
    }
}
