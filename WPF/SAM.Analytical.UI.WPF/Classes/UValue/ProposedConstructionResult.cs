// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// The construction the U-value calculation generates for an evaluated layer thickness, built by <c>Query.ProposedConstruction</c> WITHOUT
    /// touching any model or material library: the generated construction (new Guid and name, or the source modified in place), the adjusted layer
    /// material (a copy of the layer's material at the new default thickness), and what changed. <c>Modify.SetUValue</c> applies it; the Thermal
    /// Performance panel previews and saves it to "My constructions" from the same object, so all three agree.
    /// </summary>
    public sealed class ProposedConstructionResult
    {
        internal ProposedConstructionResult(string error, ProposedConstructionErrorKind errorKind)
        {
            Error = error;
            ErrorKind = errorKind;
        }

        internal ProposedConstructionResult(Construction source, Construction construction, IMaterial material, bool materialAdded, string sourceMaterialName, double oldThickness, double newThickness, int layerIndex)
        {
            Source = source;
            Construction = construction;
            Material = material;
            MaterialAdded = materialAdded;
            SourceMaterialName = sourceMaterialName;
            OldThickness = oldThickness;
            NewThickness = newThickness;
            LayerIndex = layerIndex;
        }

        /// <summary>True when the construction was built.</summary>
        public bool Succeeded => Error == null;

        /// <summary>Why nothing was built; null on success.</summary>
        public string Error { get; }

        internal ProposedConstructionErrorKind ErrorKind { get; }

        /// <summary>The construction the variant is made from, as it is.</summary>
        public Construction Source { get; }

        /// <summary>The generated construction: new (new Guid, a name from the target) or the source modified in place.</summary>
        public Construction Construction { get; }

        /// <summary>The material of the adjusted layer at its new thickness: the library's own when it has one of that name, otherwise a new one (<see cref="MaterialAdded"/>).</summary>
        public IMaterial Material { get; }

        /// <summary>True when <see cref="Material"/> is not in the library given: whoever applies the construction adds it (this query adds nothing).</summary>
        public bool MaterialAdded { get; }

        /// <summary>The adjusted layer's material before the change.</summary>
        public string SourceMaterialName { get; }

        /// <summary>Thickness of the adjusted layer before and after [m] (after: rounded to 1 mm, as the legacy flow does).</summary>
        public double OldThickness { get; }

        public double NewThickness { get; }

        public int LayerIndex { get; }
    }

    /// <summary>Which part of <c>Query.ProposedConstruction</c> failed (<c>Modify.SetUValue</c> reports the input errors before it looks at the scope, the material one after).</summary>
    internal enum ProposedConstructionErrorKind
    {
        None,

        /// <summary>The layer, the thickness or the name: a problem of the request itself.</summary>
        Input,

        /// <summary>The adjusted layer's material is not in the material library.</summary>
        Material,
    }
}
