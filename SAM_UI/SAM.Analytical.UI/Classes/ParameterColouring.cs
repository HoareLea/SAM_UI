// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System;

namespace SAM.Analytical.UI
{
    /// <summary>
    /// A request to colour the elements of one type in a view by one of their parameters with a palette - the same thing the
    /// View Settings window (Panel -> Parameter Name) and the Legend window (palette) do, as one value a caller can hand to
    /// <see cref="Create.ParameterColouredViewSettings"/>. It is VIEW state only: it names no model object, writes nothing to the
    /// model and knows nothing about what the parameter means (editing, U-values ...).
    /// </summary>
    public sealed class ParameterColouring
    {
        /// <param name="elementType"><see cref="Panel"/>, <see cref="Aperture"/> or <see cref="Space"/>: the elements that are coloured.</param>
        /// <param name="parameterName">The parameter name as the Parameter Name list of View Settings shows it, e.g. "UValue".</param>
        /// <param name="palette">The palette the legend is coloured with (a sequential palette maps the value onto its colours).</param>
        /// <param name="title">The legend heading; the parameter name when null.</param>
        public ParameterColouring(Type elementType, string parameterName, PaletteDefinition palette, string title = null)
        {
            ElementType = elementType ?? throw new ArgumentNullException(nameof(elementType));
            ParameterName = string.IsNullOrWhiteSpace(parameterName) ? throw new ArgumentException("A parameter name is required.", nameof(parameterName)) : parameterName;
            Palette = palette ?? throw new ArgumentNullException(nameof(palette));
            Title = string.IsNullOrWhiteSpace(title) ? parameterName : title;
        }

        public Type ElementType { get; }

        public string ParameterName { get; }

        public PaletteDefinition Palette { get; }

        public string Title { get; }

        /// <summary>The stored U-value of the panels (<see cref="PanelParameter.ThermalTransmittance"/>, parameter name "UValue") on the cool-to-warm palette.</summary>
        public static ParameterColouring PanelThermalTransmittance()
        {
            return new ParameterColouring(typeof(Panel), "UValue", PaletteDefinitions.SamThermal, "U-value [W/m²K]");
        }

        public override bool Equals(object obj)
        {
            return obj is ParameterColouring other && ElementType == other.ElementType && ParameterName == other.ParameterName && ReferenceEquals(Palette, other.Palette) && Title == other.Title;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ElementType, ParameterName, Title);
        }
    }
}
