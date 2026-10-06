// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SAM.Analytical.UI.WPF.Tests.Helpers
{
    /// <summary>What is "remembered" for a test: nothing touches SAM's user settings.</summary>
    internal sealed class InMemoryThermalSourceStore : IThermalSourceStore
    {
        public InMemoryThermalSourceStore(params string[] remembered)
        {
            Paths = remembered.ToList();
        }

        public List<string> Paths { get; private set; }

        public int Saves { get; private set; }

        public IReadOnlyList<string> Load()
        {
            return Paths.ToList();
        }

        public void Save(IReadOnlyList<string> paths)
        {
            Paths = paths.ToList();
            Saves++;
        }
    }

    /// <summary>
    /// Sources for the Thermal Performance panel in tests: a stand-in reader that answers a path from a table (and counts the reads, can wait for
    /// the test to release it, can fail), and the catalogs built on it.
    /// </summary>
    internal sealed class FakeSourceReader
    {
        private readonly Dictionary<string, GlazingSource> sources = new Dictionary<string, GlazingSource>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, TaskCompletionSource<bool>> gates = new Dictionary<string, TaskCompletionSource<bool>>(StringComparer.OrdinalIgnoreCase);

        public List<string> Reads { get; } = new List<string>();

        public Dictionary<string, string> Failures { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public FakeSourceReader Add(string path, GlazingSource source)
        {
            sources[System.IO.Path.GetFullPath(path)] = source;
            return this;
        }

        /// <summary>The read of <paramref name="path"/> waits until <see cref="Release"/> is called.</summary>
        public FakeSourceReader Hold(string path)
        {
            gates[System.IO.Path.GetFullPath(path)] = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            return this;
        }

        public void Release(string path)
        {
            gates[System.IO.Path.GetFullPath(path)].TrySetResult(true);
        }

        public async Task<GlazingSource> Read(string path, IProgress<string> progress)
        {
            lock (Reads)
            {
                Reads.Add(path);
            }

            progress?.Report("Reading " + System.IO.Path.GetFileName(path) + "…");

            if (gates.TryGetValue(path, out TaskCompletionSource<bool> gate))
            {
                await gate.Task;
            }

            if (Failures.TryGetValue(path, out string failure))
            {
                throw new InvalidOperationException(failure);
            }

            return sources.TryGetValue(path, out GlazingSource source) ? source : new GlazingSource(GlazingSourceKind.Loaded, System.IO.Path.GetFileName(path), new ConstructionManager()) { Note = "The file could not be found." };
        }

        public ThermalSourceCatalog Catalog(InMemoryThermalSourceStore store = null)
        {
            return new ThermalSourceCatalog(store ?? new InMemoryThermalSourceStore(), Read);
        }

        public static string Path(string name)
        {
            return System.IO.Path.Combine(System.IO.Path.GetTempPath(), "SAM_ThermalSources", name);
        }
    }

    internal static class SourceFixture
    {
        public static readonly Guid LoadedThickGuid = new Guid("e1000000-0000-4000-8000-000000000001");
        public static readonly Guid LoadedAerogelGuid = new Guid("e1000000-0000-4000-8000-000000000002");
        public static readonly Guid LoadedClashGuid = new Guid("e1000000-0000-4000-8000-000000000003");
        public static readonly Guid LoadedWindowGuid = new Guid("e1000000-0000-4000-8000-000000000004");
        public static readonly Guid LoadedDoorGuid = new Guid("e1000000-0000-4000-8000-000000000005");

        /// <summary>
        /// A loaded database: two wall constructions at known U-values (LOADED_THICK 0.173, LOADED_AEROGEL 0.158 with an Aerogel material the model
        /// lacks), LOADED_CLASH (U 0.182), one window system and one door system. Their thicknesses differ from the library's and the model's on purpose: the U-value cache is keyed by content, so an identical construction would not be asked again.
        /// </summary>
        public static GlazingSource Loaded(string label = "database.tcd", bool differentWool = false)
        {
            List<Construction> constructions = new List<Construction>()
            {
                new Construction(LoadedThickGuid, AlternativesFixture.Wall("LOADED_THICK", 0.128), "LOADED_THICK"),
                new Construction(LoadedAerogelGuid, AlternativesFixture.Wall("LOADED_AEROGEL", 0.142, woolMaterial: AlternativesFixture.Aerogel), "LOADED_AEROGEL"),
                new Construction(LoadedClashGuid, AlternativesFixture.Wall("LOADED_CLASH", 0.121), "LOADED_CLASH"),
            };

            ApertureConstruction window = GlazingFixture.System(LoadedWindowGuid, "LOADED_WINDOW", ApertureType.Window, GlazingFixture.Clear, frame: false);
            ApertureConstruction door = GlazingFixture.System(LoadedDoorGuid, "LOADED_DOOR", ApertureType.Door, GlazingFixture.Clear, frame: false);

            MaterialLibrary materials = AlternativesFixture.LibraryMaterials(differentWool);
            foreach (IMaterial material in GlazingFixture.ModelMaterials().GetMaterials())
            {
                materials.Add(material);
            }

            return new GlazingSource(GlazingSourceKind.Loaded, label, new ConstructionManager(new[] { window, door }, constructions, materials));
        }

        public static ThermalEditServices Services(FakeSourceReader reader, InMemoryThermalSourceStore store = null, FakeConstructionUValueEvaluator evaluator = null)
        {
            return new ThermalEditServices(() => new ImmediateUValueEvaluator(), () => new FakeGlazingEvaluator(), () => GlazingFixture.Library(), () => evaluator ?? new FakeConstructionUValueEvaluator(), () => null, () => reader.Catalog(store));
        }
    }
}
