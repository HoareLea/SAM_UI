// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SAM.Analytical.UI.WPF.Tests.Helpers
{
    /// <summary>
    /// A stand-in for the batch Tas U-value calculation on the <see cref="UValueFixture"/> physics (U depends on the thickness of the layer at
    /// <see cref="UValueFixture.WoolIndex"/>): it records every request, so a test can prove ONE batch per pool and that a cached value is
    /// never asked twice, and it can be made to fail.
    /// </summary>
    internal sealed class FakeConstructionUValueEvaluator : IConstructionUValueEvaluator
    {
        private readonly TasConstructionUValueEvaluator inner;

        public FakeConstructionUValueEvaluator(FakeTas fakeTas = null)
        {
            FakeTas = fakeTas ?? new FakeTas();
            inner = new TasConstructionUValueEvaluator(FakeTas.ThermalTransmittances);
        }

        public FakeTas FakeTas { get; }

        public List<ConstructionUValueRequest> Requests { get; } = new List<ConstructionUValueRequest>();

        /// <summary>Constructions asked for, over all requests.</summary>
        public int ConstructionCount => Requests.Sum(x => x.Constructions.Count);

        public Task<IReadOnlyList<ConstructionUValue>> EvaluateAsync(ConstructionUValueRequest request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(inner.Evaluate(request));
        }
    }

    /// <summary>The opaque-alternatives fixture: the current wall, existing constructions in the model and in a library, at known U-values.</summary>
    internal static class AlternativesFixture
    {
        public const string Aerogel = "Aerogel";

        /// <summary>The fixture wall with the wool layer at <paramref name="wool"/> metres (U = <see cref="UValueFixture.U"/>).</summary>
        public static Construction Wall(string name, double wool, string defaultPanelType = "Wall", string woolMaterial = UValueFixture.Wool)
        {
            Construction result = new Construction(Guid.NewGuid(), name, new List<ConstructionLayer>()
            {
                new ConstructionLayer(UValueFixture.Air, 0.05),
                new ConstructionLayer(UValueFixture.Board, 0.012),
                new ConstructionLayer(woolMaterial, wool),
                new ConstructionLayer(UValueFixture.Air, 0.05),
                new ConstructionLayer(UValueFixture.Rainscreen, 0.003),
            });

            if (defaultPanelType != null)
            {
                result.SetValue(ConstructionParameter.DefaultPanelType, defaultPanelType);
            }

            return result;
        }

        /// <summary>The library's materials: the model's, plus Aerogel, plus a mineral wool of the same name as the model's but another conductivity.</summary>
        public static MaterialLibrary LibraryMaterials(bool differentWool = false)
        {
            MaterialLibrary result = new MaterialLibrary("Library");
            foreach (IMaterial material in UValueFixture.Materials().GetMaterials())
            {
                if (differentWool && material.Name == UValueFixture.Wool)
                {
                    result.Add(new OpaqueMaterial(Guid.NewGuid(), UValueFixture.Wool, UValueFixture.Wool, "Library", 0.04, 1030, 20));
                }
                else
                {
                    result.Add(material);
                }
            }

            result.Add(new OpaqueMaterial(Guid.NewGuid(), Aerogel, Aerogel, "Library", 0.015, 1000, 150));
            return result;
        }

        public static readonly Guid LibraryThickGuid = new Guid("d1000000-0000-4000-8000-000000000001");
        public static readonly Guid LibraryAerogelGuid = new Guid("d1000000-0000-4000-8000-000000000002");
        public static readonly Guid LibrarySameNameGuid = new Guid("d1000000-0000-4000-8000-000000000004");
        public static readonly Guid LibraryMissingGuid = new Guid("d1000000-0000-4000-8000-000000000005");
        public static readonly Guid LibraryRoofGuid = new Guid("d1000000-0000-4000-8000-000000000006");

        /// <summary>
        /// The library source. At target 0.18 these meet it: MODEL_THICK (a second construction named like a model one) 0.177, LIB_THICK 0.171, LIB_ROOF
        /// 0.165 (made for roofs), LIB_AEROGEL 0.160 (needs the Aerogel material); LIB_MISSING names a material the library lacks (no U-value).
        /// </summary>
        public static GlazingSource Library()
        {
            List<Construction> constructions = new List<Construction>();

            Construction thick = Wall("LIB_THICK", 0.13);
            constructions.Add(Copy(thick, LibraryThickGuid));

            constructions.Add(Copy(Wall("LIB_AEROGEL", 0.14, woolMaterial: Aerogel), LibraryAerogelGuid));
            constructions.Add(Copy(Wall("MODEL_THICK", 0.125), LibrarySameNameGuid));
            constructions.Add(Copy(Wall("LIB_MISSING", 0.13, woolMaterial: "Ghost"), LibraryMissingGuid));
            constructions.Add(Copy(Wall("LIB_ROOF", 0.135, "Roof"), LibraryRoofGuid));

            MaterialLibrary materials = LibraryMaterials(differentWool: false);
            return new GlazingSource(GlazingSourceKind.Library, "Default library", new ConstructionManager(null, constructions, materials));
        }

        /// <summary>The same library with the wool defined differently from the model's: every construction of it that uses the wool is blocked.</summary>
        public static GlazingSource LibraryWithDifferentWool()
        {
            GlazingSource source = Library();
            return new GlazingSource(GlazingSourceKind.Library, "Default library", new ConstructionManager(null, source.GetConstructions(), LibraryMaterials(differentWool: true)));
        }

        private static Construction Copy(Construction construction, Guid guid)
        {
            return new Construction(guid, construction, construction.Name);
        }

        /// <summary>The model: 6 external walls on the current construction (wool 80 mm, U 0.260), and one wall each on MODEL_THICK (U 0.1835), MODEL_MED (0.215) and MODEL_THIN (0.444).</summary>
        public static AnalyticalModel Model(out Construction current, out Construction thick, out Construction medium, out Construction thin, int walls = 6, int roofs = 0)
        {
            thick = Wall("MODEL_THICK", 0.12);
            medium = Wall("MODEL_MED", 0.10);
            thin = Wall("MODEL_THIN", 0.04);
            return UValueFixture.Model(out current, walls, roofs, thick, medium, thin);
        }

        public static ThermalEditServices Services(FakeConstructionUValueEvaluator evaluator = null, Func<GlazingSource> library = null)
        {
            return new ThermalEditServices(() => new ImmediateUValueEvaluator(), () => new FakeGlazingEvaluator(), () => GlazingFixture.Library(), () => evaluator ?? new FakeConstructionUValueEvaluator(), library ?? (() => Library()));
        }
    }
}
