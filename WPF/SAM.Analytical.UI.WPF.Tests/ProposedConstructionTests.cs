// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.UI.WPF.Tests.Helpers;
using SAM.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// PR4: the generated U-value construction is made by ONE pure query, <c>Query.ProposedConstruction</c>, which <c>Modify.SetUValue</c> now calls
    /// (and the Thermal Performance panel previews / saves from) - with NO change in what Apply does. These tests pin that: the query is compared
    /// with a verbatim copy of the generation code <c>SetUValue</c> had before the extraction (<see cref="Legacy"/>) over every path of it (new / in
    /// place, typed / made / taken names, a material already adjusted, one the library has, rounding, the failures), the query is shown to be pure,
    /// and <c>SetUValue</c> is shown to apply exactly the query's construction, to report its errors in the order it always did, and to keep its
    /// result.
    /// </summary>
    public class ProposedConstructionTests
    {
        private const double Thickness_U030 = 0.06708333333;

        // ---- The reference: the generation code of Modify.SetUValue before PR4, unchanged except that it works on copies and returns instead of applying ----

        private sealed class LegacyResult
        {
            public string Error;
            public Construction Construction;
            public IMaterial Material;
            public bool MaterialAdded;
            public string SourceMaterialName;
            public double OldThickness;
            public double NewThickness;
        }

        private static LegacyResult Legacy(Construction source, MaterialLibrary materialLibrary_Model, SetUValueRequest request, IEnumerable<Construction> constructions_Model)
        {
            List<Construction> constructions = constructions_Model.ToList();
            MaterialLibrary materialLibrary = new MaterialLibrary(materialLibrary_Model);

            List<ConstructionLayer> constructionLayers = source.ConstructionLayers;
            if (constructionLayers == null || request.LayerIndex < 0 || request.LayerIndex >= constructionLayers.Count || constructionLayers[request.LayerIndex] == null)
            {
                return new LegacyResult() { Error = string.Format("{0} has no layer {1}.", source.Name, request.LayerIndex + 1) };
            }

            if (double.IsNaN(request.Thickness) || request.Thickness <= 0)
            {
                return new LegacyResult() { Error = "No layer thickness was calculated." };
            }

            UValueApplyMode mode = request.Mode;
            if (mode == UValueApplyMode.ModifyInPlace && constructions.Any(x => x != null && x.Guid != source.Guid && x.Name == source.Name))
            {
                return new LegacyResult() { Error = string.Format("Other constructions are also named {0}; keeping the name would change them too. Create a new construction instead.", source.Name) };
            }

            ConstructionLayer constructionLayer = constructionLayers[request.LayerIndex];
            double oldThickness = constructionLayer.Thickness;
            double thickness = Core.Query.Round(request.Thickness, Tolerance.MacroDistance);
            string materialName_Base = constructionLayer.Name;
            string materialName_Stripped = Regex.Replace(materialName_Base ?? string.Empty, @"_\d+(\.\d+)?m$", string.Empty, RegexOptions.CultureInvariant);
            if (materialName_Stripped != materialName_Base && materialLibrary.GetMaterial(materialName_Stripped) != null)
            {
                materialName_Base = materialName_Stripped;
            }

            string materialName = string.Format(CultureInfo.InvariantCulture, "{0}_{1}m", materialName_Base, thickness);

            bool materialAdded = false;
            IMaterial material = materialLibrary.GetMaterial(materialName);
            if (material == null)
            {
                Material material_Source = materialLibrary.GetMaterial(constructionLayer.Name) as Material;
                if (material_Source == null)
                {
                    return new LegacyResult() { Error = string.Format("Material {0} is not in the Material Library.", constructionLayer.Name) };
                }

                Material material_New = Core.Create.Material(material_Source, materialName, materialName, material_Source.Description);
                material_New.SetValue(Core.MaterialParameter.DefaultThickness, thickness);
                materialLibrary.Add(material_New);
                material = material_New;
                materialAdded = true;
            }

            constructionLayers[request.LayerIndex] = new ConstructionLayer(material.Name, thickness);

            Construction construction;
            if (mode == UValueApplyMode.ModifyInPlace)
            {
                construction = new Construction(source, constructionLayers);
            }
            else
            {
                string name = request.NewConstructionName?.Trim();
                if (string.IsNullOrWhiteSpace(name) || constructions.Any(x => x != null && string.Equals(x.Name?.Trim(), name, StringComparison.OrdinalIgnoreCase)))
                {
                    name = Query.UValueConstructionName(source.Name, request.CalculatedThermalTransmittance, constructions.Select(x => x?.Name));
                }

                construction = new Construction(new Construction(Guid.NewGuid(), source, name), constructionLayers);
            }

            if (construction.TryGetValue(ConstructionParameter.DefaultThickness, out double _))
            {
                construction.SetValue(ConstructionParameter.DefaultThickness, construction.GetThickness());
            }

            return new LegacyResult() { Construction = construction, Material = material, MaterialAdded = materialAdded, SourceMaterialName = constructionLayer.Name, OldThickness = oldThickness, NewThickness = thickness };
        }

        // ---- Helpers ---------------------------------------------------------------------------------------------------

        private static SetUValueRequest Request(Construction construction, UValueApplyMode mode = UValueApplyMode.NewConstruction, double thickness = Thickness_U030, int layer = UValueFixture.WoolIndex, string name = null, ThermalApplyScope scope = ThermalApplyScope.AllUsing, IEnumerable<Guid> selected = null)
        {
            return new SetUValueRequest()
            {
                ConstructionGuid = construction.Guid,
                LayerIndex = layer,
                Thickness = thickness,
                InitialThermalTransmittance = 0.26,
                CalculatedThermalTransmittance = 0.3,
                TargetThermalTransmittance = 0.3,
                HeatFlowDirection = HeatFlowDirection.Horizontal,
                Mode = mode,
                Scope = scope,
                SelectedPanelGuids = selected,
                NewConstructionName = name,
            };
        }

        private static ProposedConstructionResult Proposed(Construction source, MaterialLibrary materials, SetUValueRequest request, IEnumerable<Construction> constructions)
        {
            return Query.ProposedConstruction(source, materials, request.LayerIndex, request.Thickness, request.Mode, request.NewConstructionName, request.CalculatedThermalTransmittance, constructions);
        }

        private static string MaterialJson(IMaterial material)
        {
            JsonObject jsonObject = material.ToJsonObject();
            jsonObject.Remove("Guid");
            return jsonObject.ToJsonString();
        }

        private static string ConstructionJson(Construction construction, bool withGuid)
        {
            JsonObject jsonObject = construction.ToJsonObject();
            if (!withGuid)
            {
                jsonObject.Remove("Guid");
            }

            return jsonObject.ToJsonString();
        }

        // The query and the old code agree on everything they produce: the construction (name, layers, parameters; the Guid only where it is not random), the
        // adjusted material, whether it is new, the thicknesses - or the same error.
        private static void AssertSame(LegacyResult legacy, ProposedConstructionResult proposed, bool compareGuid)
        {
            if (legacy.Error != null)
            {
                Assert.False(proposed.Succeeded);
                Assert.Equal(legacy.Error, proposed.Error);
                return;
            }

            Assert.True(proposed.Succeeded, proposed.Error);
            Assert.Equal(ConstructionJson(legacy.Construction, compareGuid), ConstructionJson(proposed.Construction, compareGuid));
            Assert.Equal(MaterialJson(legacy.Material), MaterialJson(proposed.Material));
            Assert.Equal(legacy.MaterialAdded, proposed.MaterialAdded);
            Assert.Equal(legacy.SourceMaterialName, proposed.SourceMaterialName);
            Assert.Equal(legacy.OldThickness, proposed.OldThickness);
            Assert.Equal(legacy.NewThickness, proposed.NewThickness);
        }

        // ---- Parity with the code SetUValue had ---------------------------------------------------------------------------

        [Fact]
        public void A_new_construction_is_the_one_the_old_generation_made_with_the_made_name_and_a_new_guid()
        {
            AnalyticalModel model = UValueFixture.Model(out Construction source);
            List<Construction> constructions = model.AdjacencyCluster.GetConstructions();
            SetUValueRequest request = Request(source);

            ProposedConstructionResult proposed = Proposed(source, model.MaterialLibrary, request, constructions);

            AssertSame(Legacy(source, model.MaterialLibrary, request, constructions), proposed, compareGuid: false);
            Assert.Equal("SIM_EXT_SLD U0.30", proposed.Construction.Name);
            Assert.NotEqual(source.Guid, proposed.Construction.Guid);
            Assert.True(proposed.MaterialAdded);
            Assert.Equal("I01_Mineral Wool_0.067m", proposed.Material.Name);
            Assert.Equal(0.067, proposed.NewThickness, 6);
            Assert.Equal(0.08, proposed.OldThickness, 6);
        }

        [Fact]
        public void Every_path_of_the_old_generation_is_reproduced()
        {
            AnalyticalModel model = UValueFixture.Model(out Construction source, 12, 0, UValueFixture.Wall("OTHER"));
            List<Construction> constructions = model.AdjacencyCluster.GetConstructions();

            // A material the library already has under the adjusted name is used, not added.
            MaterialLibrary withAdjusted = model.MaterialLibrary;
            Material wool = (Material)withAdjusted.GetMaterial(UValueFixture.Wool);
            Material adjusted = Core.Create.Material(wool, "I01_Mineral Wool_0.067m", "I01_Mineral Wool_0.067m", wool.Description);
            adjusted.SetValue(Core.MaterialParameter.DefaultThickness, 0.067);
            withAdjusted.Add(adjusted);

            // A construction whose wool was adjusted once already: renamed from its base material, so the suffixes do not stack.
            Construction twice = new Construction(Guid.NewGuid(), "ADJUSTED", new List<ConstructionLayer>()
            {
                new ConstructionLayer(UValueFixture.Air, 0.05), new ConstructionLayer(UValueFixture.Board, 0.012), new ConstructionLayer("I01_Mineral Wool_0.067m", 0.067),
            });
            MaterialLibrary withBoth = model.MaterialLibrary;
            withBoth.Add(adjusted);

            // A construction carrying DefaultThickness, which follows the new thickness.
            Construction withDefaultThickness = new Construction(source);
            withDefaultThickness.SetValue(ConstructionParameter.DefaultThickness, source.GetThickness());

            List<(string label, Construction source, MaterialLibrary materials, SetUValueRequest request, bool compareGuid)> cases = new List<(string, Construction, MaterialLibrary, SetUValueRequest, bool)>
            {
                ("new, made name", source, model.MaterialLibrary, Request(source), false),
                ("new, typed name", source, model.MaterialLibrary, Request(source, name: "  My Wall  "), false),
                ("new, typed name taken", source, model.MaterialLibrary, Request(source, name: "other"), false),
                ("new, typed name is the source's own", source, model.MaterialLibrary, Request(source, name: source.Name), false),
                ("in place on a unique name", source, model.MaterialLibrary, Request(source, UValueApplyMode.ModifyInPlace), true),
                ("material the library already has", source, withAdjusted, Request(source), false),
                ("adjusted once already", twice, withBoth, Request(twice, thickness: 0.09, layer: 2), false),
                ("rounding to 1 mm", source, model.MaterialLibrary, Request(source, thickness: 0.0674999), false),
                ("another layer", source, model.MaterialLibrary, Request(source, thickness: 0.07, layer: 1), false),
                ("default thickness follows", withDefaultThickness, model.MaterialLibrary, Request(withDefaultThickness), false),
                ("no such layer", source, model.MaterialLibrary, Request(source, layer: 9), false),
                ("negative layer", source, model.MaterialLibrary, Request(source, layer: -1), false),
                ("thickness NaN", source, model.MaterialLibrary, Request(source, thickness: double.NaN), false),
                ("thickness zero", source, model.MaterialLibrary, Request(source, thickness: 0), false),
            };

            foreach ((string label, Construction src, MaterialLibrary materials, SetUValueRequest request, bool compareGuid) in cases)
            {
                try
                {
                    List<Construction> all = constructions.Concat(new[] { src }).GroupBy(x => x.Guid).Select(x => x.First()).ToList();
                    AssertSame(Legacy(src, materials, request, all), Proposed(src, materials, request, all), compareGuid);
                }
                catch (Exception exception)
                {
                    throw new Xunit.Sdk.XunitException("Case '" + label + "': " + exception.Message);
                }
            }

            // In place: the construction keeps its Guid and name; with a namesake it is refused, as before.
            ProposedConstructionResult inPlace = Proposed(source, model.MaterialLibrary, Request(source, UValueApplyMode.ModifyInPlace), constructions);
            Assert.Equal(source.Guid, inPlace.Construction.Guid);
            Assert.Equal(source.Name, inPlace.Construction.Name);

            List<Construction> withNamesake = constructions.Concat(new[] { UValueFixture.Wall() }).ToList();
            AssertSame(Legacy(source, model.MaterialLibrary, Request(source, UValueApplyMode.ModifyInPlace), withNamesake), Proposed(source, model.MaterialLibrary, Request(source, UValueApplyMode.ModifyInPlace), withNamesake), false);
            Assert.StartsWith("Other constructions are also named", Proposed(source, model.MaterialLibrary, Request(source, UValueApplyMode.ModifyInPlace), withNamesake).Error);

            // A layer whose material the library lacks.
            MaterialLibrary withoutWool = model.MaterialLibrary;
            withoutWool.Remove(withoutWool.GetMaterial(UValueFixture.Wool));
            AssertSame(Legacy(source, withoutWool, Request(source), constructions), Proposed(source, withoutWool, Request(source), constructions), false);
            Assert.Equal("Material I01_Mineral Wool is not in the Material Library.", Proposed(source, withoutWool, Request(source), constructions).Error);
        }

        [Fact]
        public void Without_the_models_constructions_the_name_is_made_against_the_source_alone()
        {
            AnalyticalModel model = UValueFixture.Model(out Construction source);

            ProposedConstructionResult proposed = Query.ProposedConstruction(source, model.MaterialLibrary, UValueFixture.WoolIndex, Thickness_U030, UValueApplyMode.NewConstruction, "Mine", 0.3);

            Assert.True(proposed.Succeeded);
            Assert.Equal("Mine", proposed.Construction.Name);
            Assert.False(Query.ProposedConstruction(null, model.MaterialLibrary, 0, 0.1).Succeeded);
        }

        // ---- Pure --------------------------------------------------------------------------------------------------------

        [Fact]
        public void The_query_changes_neither_the_source_nor_the_material_library_nor_the_models_constructions_and_adds_no_material()
        {
            AnalyticalModel model = UValueFixture.Model(out Construction source, 12, 0, UValueFixture.Wall("OTHER"));
            MaterialLibrary materials = model.MaterialLibrary;
            List<Construction> constructions = model.AdjacencyCluster.GetConstructions();
            string json_Source = source.ToJsonObject().ToJsonString();
            string json_Materials = materials.ToJsonObject().ToJsonString();
            string json_Constructions = string.Join("|", constructions.Select(x => x.ToJsonObject().ToJsonString()));
            string json_Model = model.ToJsonObject().ToJsonString();

            ProposedConstructionResult proposed = Proposed(source, materials, Request(source), constructions);

            Assert.True(proposed.Succeeded);
            Assert.True(proposed.MaterialAdded);
            Assert.Equal(json_Source, source.ToJsonObject().ToJsonString());
            Assert.Equal(json_Materials, materials.ToJsonObject().ToJsonString());
            Assert.Null(materials.GetMaterial(proposed.Material.Name));
            Assert.Equal(json_Constructions, string.Join("|", constructions.Select(x => x.ToJsonObject().ToJsonString())));
            Assert.Equal(json_Model, model.ToJsonObject().ToJsonString());

            // Asking twice gives the same construction (other than its new Guid) and never accumulates anything.
            ProposedConstructionResult again = Proposed(source, materials, Request(source), constructions);
            Assert.Equal(ConstructionJson(proposed.Construction, false), ConstructionJson(again.Construction, false));
        }

        // ---- SetUValue applies exactly the query's construction --------------------------------------------------------------

        [Fact]
        public void SetUValue_applies_the_construction_and_material_the_query_proposes_and_reports_what_it_always_did()
        {
            AnalyticalModel model = UValueFixture.Model(out Construction source);
            List<Construction> constructions = model.AdjacencyCluster.GetConstructions();
            SetUValueRequest request = Request(source, name: "Variant");
            ProposedConstructionResult proposed = Proposed(source, model.MaterialLibrary, request, constructions);
            Assert.True(proposed.Succeeded);

            AnalyticalModel changed = Modify.SetUValue(model, request, out SetUValueResult result);

            Assert.True(result.Succeeded, result.Error);
            Construction applied = changed.AdjacencyCluster.GetConstructions().Single(x => x.Guid == result.Construction.Guid);
            Assert.Equal(ConstructionJson(proposed.Construction, false), ConstructionJson(applied, false));
            Assert.Equal("Variant", applied.Name);
            Assert.NotNull(changed.MaterialLibrary.GetMaterial(proposed.Material.Name));
            Assert.Equal(MaterialJson(proposed.Material), MaterialJson(changed.MaterialLibrary.GetMaterial(proposed.Material.Name)));
            Assert.Equal(proposed.SourceMaterialName, result.SourceMaterialName);
            Assert.Equal(proposed.Material.Name, result.MaterialName);
            Assert.Equal(proposed.MaterialAdded, result.MaterialAdded);
            Assert.Equal(proposed.OldThickness, result.OldThickness);
            Assert.Equal(proposed.NewThickness, result.NewThickness);
            Assert.Equal(12, result.PanelCount);

            // The model it was applied to is untouched (the clone changed, not the original).
            Assert.Null(model.MaterialLibrary.GetMaterial(proposed.Material.Name));
        }

        [Fact]
        public void SetUValue_reports_the_requests_own_errors_before_the_scope_and_a_missing_material_after_it_as_it_always_did()
        {
            AnalyticalModel model = UValueFixture.Model(out Construction source);
            Guid unrelated = Guid.NewGuid();

            // The request is bad AND the scope matches no panel: the request's error comes first.
            Modify.SetUValue(model, Request(source, thickness: double.NaN, scope: ThermalApplyScope.SelectedOnly, selected: new[] { unrelated }), out SetUValueResult first);
            Assert.Equal("No layer thickness was calculated.", first.Error);
            Modify.SetUValue(model, Request(source, layer: 9, scope: ThermalApplyScope.SelectedOnly, selected: new[] { unrelated }), out SetUValueResult layer);
            Assert.Equal("SIM_EXT_SLD has no layer 10.", layer.Error);

            AnalyticalModel withNamesake = UValueFixture.Model(out Construction source_Namesake, 12, 0, UValueFixture.Wall());
            Modify.SetUValue(withNamesake, Request(source_Namesake, UValueApplyMode.ModifyInPlace), out SetUValueResult name);
            Assert.StartsWith("Other constructions are also named SIM_EXT_SLD", name.Error);

            // The material is missing AND the scope matches no panel: the scope's error comes first; with a scope that matches, the material's.
            MaterialLibrary withoutWool = model.MaterialLibrary;
            withoutWool.Remove(withoutWool.GetMaterial(UValueFixture.Wool));
            AnalyticalModel noWool = new AnalyticalModel(model, model.AdjacencyCluster, withoutWool, model.ProfileLibrary);
            Modify.SetUValue(noWool, Request(source, scope: ThermalApplyScope.SelectedOnly, selected: new[] { unrelated }), out SetUValueResult scope);
            Assert.Equal("None of the selected panels uses SIM_EXT_SLD.", scope.Error);
            Modify.SetUValue(noWool, Request(source), out SetUValueResult material);
            Assert.Equal("Material I01_Mineral Wool is not in the Material Library.", material.Error);
        }
    }
}
