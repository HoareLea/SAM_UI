// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Query
    {
        /// <summary>
        /// The construction the U-value calculation generates when one layer of <paramref name="source"/> takes the evaluated
        /// <paramref name="thickness"/>: a PURE query - it changes neither <paramref name="source"/>, <paramref name="materialLibrary"/> nor
        /// <paramref name="constructions"/>, and adds nothing anywhere. It is the one place the generated construction is made: <c>Modify.SetUValue</c>
        /// applies it, and the Thermal Performance panel previews it and saves it to "My constructions" before (or without) an Apply.
        /// <para>
        /// The adjusted material is a copy of the layer's material at the new default thickness named "&lt;material&gt;_&lt;thickness&gt;m" (a layer
        /// already adjusted once, "&lt;material&gt;_0.067m", is renamed from its base material so the suffixes do not stack); the library's own when it
        /// already has one of that name, otherwise a new one (<see cref="ProposedConstructionResult.MaterialAdded"/>). In
        /// <see cref="UValueApplyMode.NewConstruction"/> the construction has a new Guid and the typed name, or <see cref="UValueConstructionName"/> when the
        /// name is empty or taken (ignoring case) by one of <paramref name="constructions"/>; in <see cref="UValueApplyMode.ModifyInPlace"/> it keeps the
        /// source's Guid and name, which is refused while another construction shares the name.
        /// </para>
        /// </summary>
        /// <param name="source">The construction to adjust.</param>
        /// <param name="materialLibrary">The material library the layers' materials are in (the model's).</param>
        /// <param name="layerIndex">The layer whose thickness changes.</param>
        /// <param name="thickness">The evaluated thickness [m]; rounded to 1 mm.</param>
        /// <param name="mode">A new construction, or the source modified in place.</param>
        /// <param name="newConstructionName">The name for a new construction; when empty or taken a name is made from the U-value.</param>
        /// <param name="calculatedThermalTransmittance">The U-value reached [W/m²K], for the made name.</param>
        /// <param name="constructions">The model's constructions (names to keep unique); null for the source alone.</param>
        public static ProposedConstructionResult ProposedConstruction(Construction source, MaterialLibrary materialLibrary, int layerIndex, double thickness, UValueApplyMode mode = UValueApplyMode.NewConstruction, string newConstructionName = null, double calculatedThermalTransmittance = double.NaN, IEnumerable<Construction> constructions = null)
        {
            if (source == null)
            {
                return new ProposedConstructionResult("There is no construction to adjust.", ProposedConstructionErrorKind.Input);
            }

            List<Construction> constructions_All = constructions?.ToList() ?? new List<Construction>() { source };

            List<ConstructionLayer> constructionLayers = source.ConstructionLayers;
            if (constructionLayers == null || layerIndex < 0 || layerIndex >= constructionLayers.Count || constructionLayers[layerIndex] == null)
            {
                return new ProposedConstructionResult(string.Format("{0} has no layer {1}.", source.Name, layerIndex + 1), ProposedConstructionErrorKind.Input);
            }

            if (double.IsNaN(thickness) || thickness <= 0)
            {
                return new ProposedConstructionResult("No layer thickness was calculated.", ProposedConstructionErrorKind.Input);
            }

            // UpdateConstructions (a legacy post-step) matches by NAME: keeping the name (modifying in place) would also rewrite
            // other constructions that share the name, so that combination is refused.
            if (mode == UValueApplyMode.ModifyInPlace && constructions_All.Any(x => x != null && x.Guid != source.Guid && x.Name == source.Name))
            {
                return new ProposedConstructionResult(string.Format("Other constructions are also named {0}; keeping the name would change them too. Create a new construction instead.", source.Name), ProposedConstructionErrorKind.Input);
            }

            // The adjusted material: a copy of the source material at the new default thickness, named as the legacy flow names it.
            MaterialLibrary materialLibrary_Used = materialLibrary ?? new MaterialLibrary("Default MaterialLibrary");

            ConstructionLayer constructionLayer = constructionLayers[layerIndex];
            double oldThickness = constructionLayer.Thickness;
            double thickness_Rounded = Core.Query.Round(thickness, Tolerance.MacroDistance);

            // Named as the legacy flow names it, "<material>_<thickness>m"; a layer already adjusted once
            // ("<material>_0.067m") is renamed from its base material, so the suffixes do not stack.
            string materialName_Base = constructionLayer.Name;
            string materialName_Stripped = Regex.Replace(materialName_Base ?? string.Empty, @"_\d+(\.\d+)?m$", string.Empty, RegexOptions.CultureInvariant);
            if (materialName_Stripped != materialName_Base && materialLibrary_Used.GetMaterial(materialName_Stripped) != null)
            {
                materialName_Base = materialName_Stripped;
            }

            string materialName = string.Format(CultureInfo.InvariantCulture, "{0}_{1}m", materialName_Base, thickness_Rounded);

            bool materialAdded = false;
            IMaterial material = materialLibrary_Used.GetMaterial(materialName);
            if (material == null)
            {
                Material material_Source = materialLibrary_Used.GetMaterial(constructionLayer.Name) as Material;
                if (material_Source == null)
                {
                    return new ProposedConstructionResult(string.Format("Material {0} is not in the Material Library.", constructionLayer.Name), ProposedConstructionErrorKind.Material);
                }

                Material material_New = Core.Create.Material(material_Source, materialName, materialName, material_Source.Description);
                material_New.SetValue(Core.MaterialParameter.DefaultThickness, thickness_Rounded);
                material = material_New;
                materialAdded = true;
            }

            constructionLayers[layerIndex] = new ConstructionLayer(material.Name, thickness_Rounded);

            Construction construction;
            if (mode == UValueApplyMode.ModifyInPlace)
            {
                construction = new Construction(source, constructionLayers);
            }
            else
            {
                string name = newConstructionName?.Trim();
                if (string.IsNullOrWhiteSpace(name) || constructions_All.Any(x => x != null && string.Equals(x.Name?.Trim(), name, StringComparison.OrdinalIgnoreCase)))
                {
                    name = UValueConstructionName(source.Name, calculatedThermalTransmittance, constructions_All.Select(x => x?.Name));
                }

                construction = new Construction(new Construction(Guid.NewGuid(), source, name), constructionLayers);
            }

            if (construction.TryGetValue(ConstructionParameter.DefaultThickness, out double _))
            {
                construction.SetValue(ConstructionParameter.DefaultThickness, construction.GetThickness());
            }

            return new ProposedConstructionResult(source, construction, material, materialAdded, constructionLayer.Name, oldThickness, thickness_Rounded, layerIndex);
        }
    }
}
