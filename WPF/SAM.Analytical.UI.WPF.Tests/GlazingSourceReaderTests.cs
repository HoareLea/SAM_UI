// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.UI.WPF.Tests.Helpers;
using SAM.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// U-value PR3: <c>Query.ReadGlazingSource</c> ("Load more glazing...") and the JSON cache of converted .tcd files.
    /// The .tcd conversion itself (SAM_Tas, needs TCD.exe) is not run here: a .tcd that has a cache entry is read from the
    /// cache, which is the same path every second load takes, so the tests pre-populate the cache with a converted
    /// <see cref="ConstructionManager"/> and read a dummy .tcd file. Real .tcd imports are in the PR record.
    /// </summary>
    [Collection(WpfCollection.Name)]
    public class GlazingSourceReaderTests : IDisposable
    {
        private readonly string directory = Path.Combine(Path.GetTempPath(), "SAM_GlazingSourceReaderTests_" + Guid.NewGuid().ToString("N"));

        public GlazingSourceReaderTests()
        {
            Directory.CreateDirectory(directory);
            GlazingSourceCache.Directory = Path.Combine(directory, "cache");
        }

        public void Dispose()
        {
            GlazingSourceCache.Directory = null;
            try
            {
                Directory.Delete(directory, true);
            }
            catch (IOException)
            {
            }
        }

        private static readonly Guid DoubleGuid = new Guid("c0000000-0000-4000-8000-000000000001");
        private static readonly Guid SingleGuid = new Guid("c0000000-0000-4000-8000-000000000002");
        private static readonly Guid DoorGuid = new Guid("c0000000-0000-4000-8000-000000000003");

        // What the SAM_Tas importer returns for a database with two glazing systems, a timber door and an empty construction.
        private static ConstructionManager Converted()
        {
            ConstructionManager constructionManager = new ConstructionManager();
            constructionManager.Add(GlazingFixture.ClearGlass());
            constructionManager.Add(GlazingFixture.ArgonGas());
            constructionManager.Add(new OpaqueMaterial(Guid.NewGuid(), "Timber", "Timber", "Timber", 0.13, 1200, 500));

            Construction glazing = new Construction(DoubleGuid, "opti\\4", new List<ConstructionLayer>() { new ConstructionLayer(GlazingFixture.Clear, 0.006), new ConstructionLayer(GlazingFixture.Argon, 0.012), new ConstructionLayer(GlazingFixture.Clear, 0.006) });
            glazing.SetValue(ConstructionParameter.Description, "Low-e double");
            glazing.SetValue(Tas.ConstructionParameter.AdditionalHeatTransfer, 0.35);
            constructionManager.Add(glazing);
            constructionManager.Add(new Construction(SingleGuid, "single", new List<ConstructionLayer>() { new ConstructionLayer(GlazingFixture.Clear, 0.006) }));
            constructionManager.Add(new Construction(DoorGuid, "timber door", new List<ConstructionLayer>() { new ConstructionLayer("Timber", 0.044) }));
            constructionManager.Add(new Construction(Guid.NewGuid(), "empty", new List<ConstructionLayer>()));
            return constructionManager;
        }

        private string Database(string name, ConstructionManager cached)
        {
            string path = Path.Combine(directory, name);
            File.WriteAllText(path, "not a real database; the cache answers for it");
            if (cached != null)
            {
                GlazingSourceCache.Write(path, cached);
            }

            return path;
        }

        // ---- A .tcd, from the cache ------------------------------------------------------------------------------

        [Fact]
        public void ATcd_BecomesWindowGlazingSystems_KeepingTheConstructionGuidAndDescription()
        {
            string path = Database("systems.tcd", Converted());
            List<string> messages = new List<string>();

            GlazingSource source = Query.ReadGlazingSource(path, ApertureType.Window, new Progress<string>(messages.Add));

            Assert.Equal(GlazingSourceKind.Loaded, source.Kind);
            Assert.Equal("systems.tcd", source.Label);
            Assert.Null(source.Note);

            List<ApertureConstruction> systems = source.GetApertureConstructions(ApertureType.Window);
            Assert.Equal(new[] { DoubleGuid, SingleGuid }.OrderBy(x => x), systems.Select(x => x.Guid).OrderBy(x => x));

            ApertureConstruction glazing = systems.Single(x => x.Guid == DoubleGuid);
            Assert.Equal("opti\\4", glazing.Name);
            Assert.Equal(new[] { GlazingFixture.Clear, GlazingFixture.Argon, GlazingFixture.Clear }, glazing.PaneConstructionLayers.Select(x => x.Name).ToArray());
            Assert.False(glazing.HasFrameConstructionLayers());
            Assert.True(glazing.TryGetValue(ApertureConstructionParameter.Description, out string description));
            Assert.Equal("Low-e double", description);
            Assert.True(glazing.TryGetValue(ApertureConstructionParameter.PaneAdditionalHeatTransfer, out double additionalHeatTransfer));
            Assert.Equal(0.35, additionalHeatTransfer);
        }

        [Fact]
        public void TheSameFileLoadedTwice_GivesTheSameCandidates()
        {
            string path = Database("systems.tcd", Converted());

            GlazingSource first = Query.ReadGlazingSource(path, ApertureType.Window);
            GlazingSource second = Query.ReadGlazingSource(path, ApertureType.Window);

            Assert.Equal(first.GetApertureConstructions(ApertureType.Window).Select(x => x.Guid).OrderBy(x => x), second.GetApertureConstructions(ApertureType.Window).Select(x => x.Guid).OrderBy(x => x));
        }

        [Fact]
        public void ADoorTakesTheOpaqueConstructions()
        {
            string path = Database("systems.tcd", Converted());

            GlazingSource source = Query.ReadGlazingSource(path, ApertureType.Door);

            Assert.Equal(new[] { DoorGuid }, source.GetApertureConstructions(ApertureType.Door).Select(x => x.Guid).ToArray());
        }

        [Fact]
        public void TheMaterialsComeWithTheSystems_ForTheTasCalculation()
        {
            string path = Database("systems.tcd", Converted());

            GlazingSource source = Query.ReadGlazingSource(path, ApertureType.Window);

            Assert.Contains(GlazingFixture.Clear, source.GetMaterials().Keys);
            Assert.Contains(GlazingFixture.Argon, source.GetMaterials().Keys);
        }

        [Fact]
        public void APaneLibrary_SaysPlainlyThatItHasNoGlazingSystems()
        {
            ConstructionManager panes = new ConstructionManager();
            panes.Add(GlazingFixture.ClearGlass());
            panes.Add(GlazingFixture.LowEGlass());
            panes.Add(Analytical.Create.TransparentMaterial("BRONZE_3", string.Empty, "BRONZE_3", "pane", 1, 0.003, 1, 0.6, 0.5, 0.1, 0.1, 0.1, 0.1, 0.84, 0.84, false));
            string path = Database("igdb.tcd", panes);

            GlazingSource source = Query.ReadGlazingSource(path, ApertureType.Window);

            Assert.Empty(source.GetApertureConstructions(ApertureType.Window));
            Assert.Equal("igdb.tcd contains 3 panes and no glazing systems; it is a library of single panes, which do not define a Ug on their own.", source.Note);
        }

        [Fact]
        public void ADatabaseWithNoMatchingSystems_SaysSo()
        {
            ConstructionManager opaque = new ConstructionManager();
            opaque.Add(new OpaqueMaterial(Guid.NewGuid(), "Timber", "Timber", "Timber", 0.13, 1200, 500));
            opaque.Add(new Construction(Guid.NewGuid(), "wall", new List<ConstructionLayer>() { new ConstructionLayer("Timber", 0.1) }));
            string path = Database("walls.tcd", opaque);

            GlazingSource source = Query.ReadGlazingSource(path, ApertureType.Window);

            Assert.Empty(source.GetApertureConstructions(ApertureType.Window));
            Assert.Equal("walls.tcd contains no window glazing systems.", source.Note);
        }

        [Fact]
        public void ReadingFromTheCache_IsReportedAsSuch()
        {
            string path = Database("systems.tcd", Converted());
            List<string> messages = new List<string>();
            Query.ReadGlazingSource(path, ApertureType.Window, new SynchronousProgress(messages));

            Assert.Equal(new[] { "Reading systems.tcd from the cache…" }, messages.ToArray());
        }

        private sealed class SynchronousProgress : IProgress<string>
        {
            private readonly List<string> messages;

            public SynchronousProgress(List<string> messages)
            {
                this.messages = messages;
            }

            public void Report(string value) => messages.Add(value);
        }

        [Fact]
        public async System.Threading.Tasks.Task TheAsyncRead_GivesTheSameSource_WithoutBlockingTheCaller()
        {
            string path = Database("systems.tcd", Converted());

            GlazingSource source = await Query.ReadGlazingSourceAsync(path, ApertureType.Window, new SynchronousProgress(new List<string>()));

            Assert.Equal(2, source.GetApertureConstructions(ApertureType.Window).Count);
        }

        // ---- Missing and unreadable files -----------------------------------------------------------------------

        [Fact]
        public void AMissingFile_GivesAnEmptySourceWithANote_NotAnException()
        {
            GlazingSource source = Query.ReadGlazingSource(Path.Combine(directory, "nothing.tcd"), ApertureType.Window);

            Assert.Empty(source.GetApertureConstructions(ApertureType.Window));
            Assert.Equal("The file could not be found.", source.Note);
        }

        [Fact]
        public void ABlankPath_GivesAnEmptySource()
        {
            GlazingSource source = Query.ReadGlazingSource(string.Empty, ApertureType.Window);

            Assert.Equal("The file could not be found.", source.Note);
        }

        // ---- A .json ---------------------------------------------------------------------------------------------

        [Fact]
        public void AJsonFile_OfApertureConstructions_IsReadWithItsMaterials()
        {
            ConstructionManager constructionManager = GlazingFixture.Library().ConstructionManager;
            string path = Path.Combine(directory, "glazing.json");
            Assert.True(SAM.Core.Convert.ToFile(constructionManager, path));

            GlazingSource source = Query.ReadGlazingSource(path, ApertureType.Window);

            Assert.Equal("glazing.json", source.Label);
            Assert.Null(source.Note);
            Assert.Contains(GlazingFixture.BetterGuid, source.GetApertureConstructions(ApertureType.Window).Select(x => x.Guid));
            Assert.Contains(GlazingFixture.LowE, source.GetMaterials().Keys);
        }

        [Fact]
        public void AJsonFileWithNoSystemsOfTheType_SaysSo()
        {
            ConstructionManager constructionManager = new ConstructionManager(new[] { GlazingFixture.System(DoorGuid, "Door", ApertureType.Door, GlazingFixture.Clear) }, null, GlazingFixture.ModelMaterials());
            string path = Path.Combine(directory, "doors.json");
            SAM.Core.Convert.ToFile(constructionManager, path);

            GlazingSource source = Query.ReadGlazingSource(path, ApertureType.Window);

            Assert.Equal("doors.json contains no window glazing systems.", source.Note);
        }

        [Fact]
        public void AFileThatIsNotJson_IsNotAnException()
        {
            string path = Path.Combine(directory, "broken.json");
            File.WriteAllText(path, "{ this is not json");

            GlazingSource source = Query.ReadGlazingSource(path, ApertureType.Window);

            Assert.Empty(source.GetApertureConstructions(ApertureType.Window));
            Assert.NotNull(source.Note);
        }

        // ---- The cache -------------------------------------------------------------------------------------------

        [Fact]
        public void TheCacheKey_FollowsTheFile_ItsSizeAndItsTime()
        {
            string path = Database("db.tcd", null);

            string key = GlazingSourceCache.PathOf(path);
            Assert.StartsWith(Path.Combine(directory, "cache", "db_"), key);
            Assert.EndsWith(".json", key);
            Assert.Equal(key, GlazingSourceCache.PathOf(path));

            File.SetLastWriteTimeUtc(path, File.GetLastWriteTimeUtc(path).AddMinutes(1));
            string key_Time = GlazingSourceCache.PathOf(path);
            Assert.NotEqual(key, key_Time);

            File.AppendAllText(path, "more");
            Assert.NotEqual(key_Time, GlazingSourceCache.PathOf(path));
        }

        [Fact]
        public void ADifferentFileWithTheSameName_HasAnotherKey()
        {
            string path_1 = Database("db.tcd", null);
            Directory.CreateDirectory(Path.Combine(directory, "other"));
            string path_2 = Path.Combine(directory, "other", "db.tcd");
            File.Copy(path_1, path_2);

            Assert.NotEqual(GlazingSourceCache.PathOf(path_1), GlazingSourceCache.PathOf(path_2));
        }

        [Fact]
        public void AChangedFile_IsNeverAnsweredFromTheOldCopy()
        {
            string path = Database("db.tcd", Converted());
            Assert.NotNull(GlazingSourceCache.Read(path));

            File.AppendAllText(path, " edited");

            Assert.Null(GlazingSourceCache.Read(path));
        }

        [Fact]
        public void ACorruptCacheFile_IsAMiss_NotAnError()
        {
            string path = Database("db.tcd", Converted());
            File.WriteAllText(GlazingSourceCache.PathOf(path), "{ corrupt");

            Assert.Null(GlazingSourceCache.Read(path));
        }

        [Fact]
        public void TheCache_RoundTripsAConvertedDatabase()
        {
            string path = Database("db.tcd", null);
            ConstructionManager converted = Converted();

            GlazingSourceCache.Write(path, converted);
            ConstructionManager read = GlazingSourceCache.Read(path);

            Assert.Equal(converted.Constructions.Count, read.Constructions.Count);
            Assert.Equal(converted.MaterialLibrary.Count, read.MaterialLibrary.Count);
            Assert.Empty(Directory.GetFiles(Path.Combine(directory, "cache"), "*.tmp"));
        }

        [Fact]
        public void ACacheThatCannotBeWritten_DoesNotStopTheImport()
        {
            string path = Database("db.tcd", null);
            File.WriteAllText(Path.Combine(directory, "cache"), "a file where the cache folder should be");

            GlazingSourceCache.Write(path, Converted());

            Assert.Null(GlazingSourceCache.Read(path));
        }

        [Fact]
        public void ThereIsNoKeyForAMissingFile()
        {
            Assert.Null(GlazingSourceCache.PathOf(Path.Combine(directory, "nothing.tcd")));
            Assert.Null(GlazingSourceCache.Read(Path.Combine(directory, "nothing.tcd")));
        }
    }
}
