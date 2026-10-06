// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests.Helpers
{
    /// <summary>
    /// Stage E0-3 (Glazing System Builder UI) fixture: a seed system with a frame, a pane source shaped like an IGDB file (with categories), the Tas
    /// stand-in of E0-1 for the draft's performance, and a Builder view-model built on them with no debounce and no real Tas.
    /// </summary>
    internal static class BuilderUiFixture
    {
        public const string PaneSourceLabel = "IGDB-test.tcd";
        public const string GasName = "Test Air";

        public static readonly Guid SeedGuid = new Guid("e0300000-0000-4000-8000-000000000001");

        /// <summary>SEED_GLZ: clear 4 | Test Air 12 | clear 4 (SAM order, inside → outside) in a 70 mm frame, made for external walls; no stored frame width.</summary>
        public static ApertureConstruction Seed(PanelType panelType = PanelType.WallExternal, double? frameWidth = null)
        {
            ApertureConstruction result = new ApertureConstruction(SeedGuid, "SEED_GLZ", ApertureType.Window,
                new List<ConstructionLayer>() { new ConstructionLayer(BuilderFixture.Clear, 0.004), new ConstructionLayer(GasName, 0.012), new ConstructionLayer(BuilderFixture.Clear, 0.004) },
                new List<ConstructionLayer>() { new ConstructionLayer(BuilderFixture.FrameMaterial, 0.07) });
            result.SetValue(ApertureConstructionParameter.FrameAdditionalHeatTransfer, 10.0);
            if (panelType != PanelType.Undefined)
            {
                result.SetValue(ApertureConstructionParameter.DefaultPanelType, panelType.ToString());
            }

            if (frameWidth != null)
            {
                result.SetValue(ApertureConstructionParameter.DefaultFrameWidth, frameWidth.Value);
            }

            return result;
        }

        public static ConstructionManager Manager(MaterialLibrary materials)
        {
            return new ConstructionManager(new List<ApertureConstruction>(), new List<Construction>(), materials);
        }

        public static GlazingSource SeedSource(ApertureConstruction seed = null, GlazingSourceKind kind = GlazingSourceKind.Model, string label = "Model")
        {
            MaterialLibrary materials = BuilderFixture.SeedMaterials();
            return new GlazingSource(kind, label, new ConstructionManager(new List<ApertureConstruction>() { seed ?? Seed() }, null, materials));
        }

        public static TransparentMaterial WithCategory(TransparentMaterial material, string category)
        {
            material.SetValue(ParameterizedSAMObjectParameter.Category, new Category(category));
            return material;
        }

        /// <summary>An IGDB-shaped pane file: clear 4, tinted 6, a low-e 4 and its reversed twin, with product-family categories, plus <paramref name="extra"/> generated panes.</summary>
        public static GlazingSource PaneSource(string label = PaneSourceLabel, int extra = 0, GlazingSourceKind kind = GlazingSourceKind.Loaded)
        {
            MaterialLibrary materials = new MaterialLibrary(label);
            materials.Add(WithCategory(BuilderFixture.ClearPane(), "Material Root\\Pilkington\\Float"));
            materials.Add(WithCategory(BuilderFixture.TintPane(), "Material Root\\Pilkington\\Tinted"));
            materials.Add(WithCategory(BuilderFixture.LowEPane(), "Material Root\\Pilkington\\Low-e"));
            materials.Add(WithCategory(BuilderFixture.LowEPaneReversedEntry(), "Material Root\\Pilkington\\Low-e"));
            materials.Add(BuilderFixture.Gas(DefaultGasType.Argon));
            materials.Add(BuilderFixture.Frame());
            for (int i = 0; i < extra; i++)
            {
                string name = "Generated " + (i + 1).ToString("00000");
                materials.Add(WithCategory(Analytical.Create.TransparentMaterial(name, string.Empty, name, "Generated", 1, 0.003 + (i % 12) * 0.001, 9999, 0.3 + (i % 50) * 0.01, 0.5 + (i % 40) * 0.01, 0.07, 0.07, 0.08, 0.08, 0.84, 0.84, false), "Material Root\\Generated\\Family " + (i % 7)));
            }

            return new GlazingSource(kind, label, Manager(materials));
        }

        public static GlazingBuilderOptions Options(UserGlazingLibrary library, FakeDraftTas tas, ThermalSourceCatalog catalog = null, ApertureConstruction seed = null, bool withSeed = true, params GlazingSource[] sources)
        {
            ApertureConstruction system = withSeed ? seed ?? Seed() : null;
            List<GlazingSource> pool = new List<GlazingSource>(sources.Where(x => x != null));

            return new GlazingBuilderOptions()
            {
                Seed = system,
                SeedSource = withSeed ? SeedSource(system) : null,
                Sources = pool,
                Catalog = catalog,
                Library = library,
                Evaluator = new DraftGlazingEvaluator(tas ?? new FakeDraftTas(), TimeSpan.Zero, null, BuilderFixture.Options()),
                ComposeOptions = BuilderFixture.Options(),
                SearchDebounce = TimeSpan.Zero,
            };
        }

        /// <summary>A Builder with the IGDB-shaped pane source and no catalogue; its first evaluation is awaited.</summary>
        public static GlazingBuilderViewModel Builder(UserGlazingLibrary library, FakeDraftTas tas = null, ThermalSourceCatalog catalog = null, ApertureConstruction seed = null, bool withSeed = true, GlazingSource panes = null)
        {
            GlazingBuilderViewModel result = new GlazingBuilderViewModel(Options(library, tas, catalog, seed, withSeed, panes ?? PaneSource(kind: GlazingSourceKind.Model)));
            Settle(result);
            return result;
        }

        public static void Settle(GlazingBuilderViewModel viewModel)
        {
            Assert.True(viewModel.LastEvaluationTask.Wait(TimeSpan.FromSeconds(10)));
            Assert.True(viewModel.Panes.LastWork.Wait(TimeSpan.FromSeconds(10)));
        }

        public static GlazingPaneEntry Pane(GlazingBuilderViewModel viewModel, string name)
        {
            return viewModel.Panes.Entries.Single(x => x.Name == name);
        }
    }
}
