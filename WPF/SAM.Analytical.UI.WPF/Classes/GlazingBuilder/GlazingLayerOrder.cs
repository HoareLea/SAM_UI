// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// THE one place where the Glazing System Builder's layer order meets SAM's. The Builder lists a stack OUTSIDE → INSIDE; an
    /// <see cref="ApertureConstruction"/> stores its pane layers INSIDE → OUTSIDE ("following the TAS approach"), and SAM_Tas writes them to
    /// TCD in that order. Stage E0-1 Gate 0 confirmed it against real Tas with the Pilkington IGDB subset: a tinted pane as the LAST SAM
    /// layer gives the lower g (it is outside: g 0.380 vs 0.607 as the first layer), and a pane's <c>External*</c> values are the face
    /// towards the outside (a low-e coating as External of the first, inside, layer faces the cavity: Ug 1.05; of the last layer it faces
    /// outdoors: Ug 2.61). Nothing else in the Builder reverses a list.
    /// </summary>
    internal static class GlazingLayerOrder
    {
        /// <summary>Builder order (outside → inside) to SAM order (inside → outside).</summary>
        internal static List<T> ToSam<T>(IEnumerable<T> outsideToInside)
        {
            List<T> result = (outsideToInside ?? Enumerable.Empty<T>()).ToList();
            result.Reverse();
            return result;
        }

        /// <summary>SAM order (inside → outside) to Builder order (outside → inside): for seeding a draft from an existing system.</summary>
        internal static List<T> FromSam<T>(IEnumerable<T> insideToOutside)
        {
            return ToSam(insideToOutside);
        }

        /// <summary>The SAM layer index of a Builder layer index in a stack of <paramref name="count"/> layers (and back: the map is its own inverse).</summary>
        internal static int SamIndex(int builderIndex, int count)
        {
            return count - 1 - builderIndex;
        }
    }
}
