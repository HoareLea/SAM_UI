// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SAM.Analytical.UI.WPF.Tests.Helpers
{
    /// <summary>
    /// PR4 ("My constructions") fixture: a library on a temporary file, opaque walls on the <see cref="UValueFixture"/> physics, a glazing construction
    /// and a gas-only construction (which are never opaque), and a helper that writes a library file by hand (to give a construction a chosen Guid, a
    /// material of another definition, or content the library itself would never save).
    /// </summary>
    internal static class UserConstructionFixture
    {
        public static string TempDirectory()
        {
            string result = Path.Combine(Path.GetTempPath(), "SAM-PR4-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(result);
            return result;
        }

        public static UserConstructionLibrary Library(string directory, TimeSpan? lockTimeout = null)
        {
            return new UserConstructionLibrary(Path.Combine(directory, "Constructions.json"), lockTimeout);
        }

        public static void Delete(string directory)
        {
            try
            {
                foreach (string path in Directory.GetFiles(directory, "*", SearchOption.AllDirectories))
                {
                    File.SetAttributes(path, FileAttributes.Normal);
                }

                Directory.Delete(directory, true);
            }
            catch (Exception)
            {
            }
        }

        /// <summary>The fixture wall (Air 50 / board 12 / wool at <paramref name="wool"/> / Air 50 / rainscreen 3), U = <see cref="UValueFixture.U"/>.</summary>
        public static Construction Wall(string name, double wool = 0.13)
        {
            return AlternativesFixture.Wall(name, wool);
        }

        /// <summary>The fixture's materials: the model's (gas, board, wool, rainscreen).</summary>
        public static MaterialLibrary Materials()
        {
            return UValueFixture.Materials();
        }

        /// <summary>A glazing construction stored as a construction: a transparent pane, a gas gap, a transparent pane.</summary>
        public static Construction Glass(out MaterialLibrary materials)
        {
            materials = new MaterialLibrary("Glass");
            materials.Add(BuilderFixture.ClearPane());
            materials.Add(BuilderFixture.Gas(DefaultGasType.Air));
            return new Construction(Guid.NewGuid(), "GLZ", new List<ConstructionLayer>()
            {
                new ConstructionLayer(BuilderFixture.Clear, 0.004),
                new ConstructionLayer(BuilderFixture.Gas(DefaultGasType.Air).Name, 0.012),
                new ConstructionLayer(BuilderFixture.Clear, 0.004),
            });
        }

        /// <summary>A construction of one gas layer only.</summary>
        public static Construction GasOnly(out MaterialLibrary materials)
        {
            materials = new MaterialLibrary("Gas");
            materials.Add(BuilderFixture.Gas(DefaultGasType.Air));
            return new Construction(Guid.NewGuid(), "GAS", new List<ConstructionLayer>() { new ConstructionLayer(BuilderFixture.Gas(DefaultGasType.Air).Name, 0.05) });
        }

        /// <summary>The same wool, defined with another conductivity: a material of the same name and another definition.</summary>
        public static OpaqueMaterial OtherWool()
        {
            return new OpaqueMaterial(Guid.NewGuid(), UValueFixture.Wool, UValueFixture.Wool, "Other", 0.04, 1030, 20);
        }

        public static UserConstructionProvenance Provenance(UserConstructionOrigin origin = UserConstructionOrigin.Model, string name = "SIM_EXT_SLD", Guid? guid = null)
        {
            return new UserConstructionProvenance()
            {
                SavedFrom = origin,
                BasedOnName = name,
                BasedOnGuid = guid ?? Guid.NewGuid(),
                OriginModelName = "Project X",
                ThermalTransmittance = 0.18,
                TargetThermalTransmittance = 0.18,
                HeatFlowDirection = "Horizontal",
                HeatFlowBasis = "Horizontal heat flow, external surfaces (WallExternal, from the panels)",
                Engine = UserConstructionProvenance.TasEngine,
                Route = "Test route",
            };
        }

        /// <summary>A library file written by hand: the constructions, the aperture constructions and every material given.</summary>
        public static void WriteLibrary(UserConstructionLibrary library, IEnumerable<IMaterial> materials, IEnumerable<Construction> constructions, IEnumerable<ApertureConstruction> apertureConstructions = null)
        {
            MaterialLibrary materialLibrary = new MaterialLibrary("User");
            foreach (IMaterial material in materials)
            {
                materialLibrary.Add(material);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(library.Path));
            File.WriteAllText(library.Path, new ConstructionManager(apertureConstructions, constructions, materialLibrary) { Name = UserConstructionLibrary.LibraryName }.ToJsonObject().ToJsonString());
        }

        public static ConstructionManager Read(string path)
        {
            ConstructionManager result = UserLibraryFile.Parse(File.ReadAllText(path), out string error);
            if (result == null)
            {
                throw new InvalidOperationException(error);
            }

            return result;
        }

        public static List<Construction> Constructions(ConstructionManager constructionManager) => constructionManager.Constructions ?? new List<Construction>();

        public static List<string> MaterialNames(ConstructionManager constructionManager) => (constructionManager.MaterialLibrary?.GetMaterials() ?? new List<IMaterial>()).Select(x => x.Name).OrderBy(x => x).ToList();

        public static int Subscribers(UserConstructionLibrary library)
        {
            FieldInfo field = typeof(UserConstructionLibrary).GetField(nameof(UserConstructionLibrary.Changed), BindingFlags.Instance | BindingFlags.NonPublic);
            return (field.GetValue(library) as Delegate)?.GetInvocationList().Length ?? 0;
        }

        /// <summary>The Thermal Performance services over the fixture's fakes with "My constructions" on <paramref name="user"/>.</summary>
        public static ThermalEditServices Services(UserConstructionLibrary user, FakeConstructionUValueEvaluator evaluator = null, Func<GlazingSource> library = null, ThermalSourceCatalog catalog = null)
        {
            return new ThermalEditServices(
                () => new ImmediateUValueEvaluator(),
                () => new FakeGlazingEvaluator(),
                () => GlazingFixture.Library(),
                () => evaluator ?? new FakeConstructionUValueEvaluator(),
                library ?? (() => AlternativesFixture.Library()),
                () => catalog ?? new ThermalSourceCatalog(new InMemoryThermalSourceStore()),
                userConstructions: () => user);
        }
    }
}
