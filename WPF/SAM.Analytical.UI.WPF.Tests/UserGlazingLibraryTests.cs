// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.UI.WPF.Tests.Helpers;
using SAM.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// Stage E0-1: "My glazing systems" - the user glazing library file. Every Save is a NEW immutable system (new Guid) with its materials
    /// embedded and its Builder provenance; names are unique; a different material of an existing name is renamed together with its layer;
    /// writes are locked, re-read, merged by Guid, atomic and backed up; an unreadable file is never overwritten; no machine path is stored.
    /// Each test uses its own temporary folder (never the user's Documents).
    /// </summary>
    public sealed class UserGlazingLibraryTests : IDisposable
    {
        private readonly string directory = BuilderFixture.TempDirectory();

        public void Dispose()
        {
            try
            {
                Directory.Delete(directory, true);
            }
            catch (Exception)
            {
            }
        }

        private UserGlazingLibrary Library(TimeSpan? lockTimeout = null) => BuilderFixture.Library(directory, lockTimeout);

        private static string Hash(string path) => System.Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));

        private static UserGlazingSaveResult Saved(UserGlazingSaveResult result)
        {
            Assert.True(result.Succeeded, result.Error);
            return result;
        }

        [Fact]
        public void The_DefaultLocation_IsDocumentsSamUserLibraries()
        {
            Assert.Equal(Path.Combine(Core.Query.UserSAMDirectory(), "User Libraries", "Glazing Systems.json"), UserGlazingLibrary.DefaultPath);
            Assert.Equal(UserGlazingLibrary.DefaultPath, new UserGlazingLibrary().Path);
        }

        [Fact]
        public void A_MissingFile_IsAnEmptyLibrary()
        {
            UserGlazingLibraryContent content = Library().Read();

            Assert.Equal(UserGlazingLibraryState.Missing, content.State);
            Assert.Empty(content.Systems);
        }

        [Fact]
        public void Save_WritesOneCompleteSystem_ThatReadsBack_AsOrdinaryConstructionManagerJson()
        {
            UserGlazingLibrary library = Library();
            GlazingSystemDraft draft = BuilderFixture.Double();
            draft.Frame.Width = 0.05;

            UserGlazingSaveResult result = Saved(library.Save(draft, new GlazingValues(1.05, 0.52, 0.75, 1.8), new DateTime(2026, 10, 2, 12, 0, 0, DateTimeKind.Utc)));

            UserGlazingLibraryContent content = library.Read();
            Assert.Equal(UserGlazingLibraryState.Ready, content.State);
            ApertureConstruction saved = Assert.Single(content.Systems);
            Assert.Equal(result.Saved.Guid, saved.Guid);
            Assert.Equal("E0 Double", saved.Name);
            Assert.Equal(ApertureType.Window, saved.ApertureType);
            Assert.Equal(PanelType.WallExternal, Analytical.Query.PanelType(saved));
            Assert.Contains("Outside to inside", saved.GetValue<string>(ApertureConstructionParameter.Description));
            Assert.Equal(0.05, saved.GetValue<double>(ApertureConstructionParameter.DefaultFrameWidth), 9);
            Assert.False(saved.TryGetValue(ApertureConstructionParameter.ThermalTransmittance, out double _));
            Assert.Equal(BuilderFixture.LowE, saved.PaneConstructionLayers.First().Name);   // inside first, as SAM stores it

            // Every material it names is embedded.
            MaterialLibrary materials = content.ConstructionManager.MaterialLibrary;
            foreach (ConstructionLayer layer in saved.PaneConstructionLayers.Concat(saved.FrameConstructionLayers))
            {
                Assert.NotNull(materials.GetMaterial(layer.Name));
            }

            // The same file is an ordinary source for "Add source…" (D2 reader), so the system can travel to another machine.
            GlazingSource source = Query.ReadThermalSource(library.Path);
            Assert.Contains(source.GetApertureConstructions(ApertureType.Window), x => x.Guid == saved.Guid);
            Assert.True(new GlazingCandidate(source.GetApertureConstructions(ApertureType.Window).Single(x => x.Guid == saved.Guid), source, null).MaterialIssue == null);
        }

        [Fact]
        public void Every_Save_IsANewSystemWithANewGuid_AndSavedSystemsNeverChange()
        {
            UserGlazingLibrary library = Library();
            GlazingSystemDraft draft = BuilderFixture.Double("First");

            ApertureConstruction first = Saved(library.Save(draft)).Saved;
            string firstJson = library.Read().Systems.Single().ToJsonObject().ToJsonString();
            draft.Name = "Second";
            ApertureConstruction second = Saved(library.Save(draft)).Saved;

            Assert.NotEqual(first.Guid, second.Guid);
            Assert.NotEqual(draft.EvaluationGuid, first.Guid);
            Assert.NotEqual(draft.EvaluationGuid, second.Guid);
            List<ApertureConstruction> systems = library.Read().Systems;
            Assert.Equal(2, systems.Count);
            Assert.Equal(firstJson, systems.Single(x => x.Guid == first.Guid).ToJsonObject().ToJsonString());
        }

        [Fact]
        public void Names_AreUniqueInTheLibrary_TrimmedAndIgnoringCase_AndARefusedSaveWritesNothing()
        {
            UserGlazingLibrary library = Library();
            Saved(library.Save(BuilderFixture.Double("E0 Double")));
            string hash = Hash(library.Path);

            UserGlazingSaveResult duplicate = library.Save(BuilderFixture.Double("  e0 double "));
            UserGlazingSaveResult unnamed = library.Save(BuilderFixture.Double(" "));

            Assert.False(duplicate.Succeeded);
            Assert.True(duplicate.Validation.Has(GlazingDraftIssueCodes.DuplicateName));
            Assert.False(unnamed.Succeeded);
            Assert.True(unnamed.Validation.Has(GlazingDraftIssueCodes.NameRequired));
            Assert.Equal(hash, Hash(library.Path));
        }

        [Fact]
        public void A_DraftWithErrorsOrMissingMaterials_IsNotSaved()
        {
            UserGlazingLibrary library = Library();
            GlazingSystemDraft gasAtEdge = BuilderFixture.Double();
            gasAtEdge.Layers.Add(BuilderFixture.Gap());
            GlazingSystemDraft missing = new GlazingSystemDraft() { Name = "Missing" };
            missing.Add(BuilderFixture.Pane(BuilderFixture.ClearPane()), BuilderFixture.Gap(), DraftPane.Missing("Gone", 0.004));

            UserGlazingSaveResult first = library.Save(gasAtEdge);
            UserGlazingSaveResult second = library.Save(missing);

            Assert.False(first.Succeeded);
            Assert.True(first.Validation.Has(GlazingDraftIssueCodes.GasAtEdge));
            Assert.False(second.Succeeded);
            Assert.Contains("Gone", second.Error);
            Assert.False(File.Exists(library.Path));
        }

        [Fact]
        public void An_IdenticalMaterial_IsReused_NotDuplicated()
        {
            UserGlazingLibrary library = Library();
            Saved(library.Save(BuilderFixture.Double("A")));
            int count = library.Read().ConstructionManager.MaterialLibrary.GetMaterials().Count;

            UserGlazingSaveResult result = Saved(library.Save(BuilderFixture.Double("B")));

            Assert.Empty(result.AddedMaterials);
            Assert.Empty(result.RenamedMaterials);
            Assert.Equal(count, library.Read().ConstructionManager.MaterialLibrary.GetMaterials().Count);
        }

        [Fact]
        public void A_DifferentMaterialOfAnExistingName_IsRenamed_AndItsLayerAndProvenanceFollow()
        {
            UserGlazingLibrary library = Library();
            ApertureConstruction first = Saved(library.Save(BuilderFixture.Double("v69"))).Saved;

            GlazingSystemDraft draft = BuilderFixture.Double("v76");
            draft.Layers[0] = new DraftPane(BuilderFixture.ClearPane(conductivity: 0.9), double.NaN, "IGDB v76.tcd", "IGDB v76.tcd");
            UserGlazingSaveResult result = Saved(library.Save(draft));

            string renamed = BuilderFixture.Clear + " (IGDB v76.tcd)";
            Assert.Equal(renamed, result.RenamedMaterials[BuilderFixture.Clear]);
            UserGlazingLibraryContent content = library.Read();
            ApertureConstruction second = content.Systems.Single(x => x.Guid == result.Saved.Guid);
            Assert.Equal(renamed, second.PaneConstructionLayers.Last().Name);            // the outside pane's layer is renamed with it
            Assert.Equal(BuilderFixture.Clear, content.Systems.Single(x => x.Guid == first.Guid).PaneConstructionLayers.Last().Name);
            Assert.Equal(0.9, ((TransparentMaterial)content.ConstructionManager.MaterialLibrary.GetMaterial(renamed)).ThermalConductivity, 9);
            Assert.Equal(1.0, ((TransparentMaterial)content.ConstructionManager.MaterialLibrary.GetMaterial(BuilderFixture.Clear)).ThermalConductivity, 9);
            Assert.Equal(renamed, GlazingBuilderProvenance.FromApertureConstruction(second).Panes.First().Material);
            Assert.Equal(BuilderFixture.Clear, GlazingBuilderProvenance.FromApertureConstruction(second).Panes.First().OriginalName);
            Assert.Contains(renamed, result.AddedMaterials);
        }

        [Fact]
        public void An_UnreadableFile_IsNeverOverwritten()
        {
            UserGlazingLibrary library = Library();
            File.WriteAllText(library.Path, "{ this is not json");
            string hash = Hash(library.Path);

            UserGlazingLibraryContent content = library.Read();
            UserGlazingSaveResult result = library.Save(BuilderFixture.Double());

            Assert.Equal(UserGlazingLibraryState.Unreadable, content.State);
            Assert.NotNull(content.Error);
            Assert.False(result.Succeeded);
            Assert.Contains("left as it is", result.Error);
            Assert.Equal(hash, Hash(library.Path));
            Assert.False(File.Exists(library.BackupPath));

            File.WriteAllText(library.Path, string.Empty);
            Assert.False(library.Save(BuilderFixture.Double()).Succeeded);
            Assert.Equal(0, new FileInfo(library.Path).Length);

            File.WriteAllText(library.Path, "{\"_type\":\"SAM.Analytical.Panel,SAM.Analytical\"}");
            Assert.Equal(UserGlazingLibraryState.Unreadable, library.Read().State);
            Assert.False(library.Save(BuilderFixture.Double()).Succeeded);
        }

        [Fact]
        public void Each_WriteKeepsThePreviousFileAsBak_AndLeavesNoTemporaryFiles()
        {
            UserGlazingLibrary library = Library();
            Saved(library.Save(BuilderFixture.Double("A")));
            string first = File.ReadAllText(library.Path);
            Assert.False(File.Exists(library.BackupPath));

            Saved(library.Save(BuilderFixture.Double("B")));

            Assert.Equal(first, File.ReadAllText(library.BackupPath));
            Assert.Empty(Directory.GetFiles(directory, "*.tmp"));
            Assert.False(File.Exists(library.LockPath));
        }

        [Fact]
        public void Two_WritersOnOneFile_NeverLoseASave()
        {
            // Two SAM_UI instances: each reads the library, then both save. The second re-reads under the lock, so the first survives.
            UserGlazingLibrary writerA = Library();
            UserGlazingLibrary writerB = Library();
            Assert.Equal(UserGlazingLibraryState.Missing, writerA.Read().State);
            Assert.Equal(UserGlazingLibraryState.Missing, writerB.Read().State);

            Guid a = Saved(writerA.Save(BuilderFixture.Double("From A"))).Saved.Guid;
            Guid b = Saved(writerB.Save(BuilderFixture.Double("From B"))).Saved.Guid;

            List<ApertureConstruction> systems = writerA.Read().Systems;
            Assert.Equal(new[] { a, b }.OrderBy(x => x), systems.Select(x => x.Guid).OrderBy(x => x));
        }

        [Fact]
        public async Task Concurrent_Saves_AreSerialised_AndAllKept()
        {
            List<Task<UserGlazingSaveResult>> saves = Enumerable.Range(1, 6).Select(i => Task.Run(() => Library().Save(BuilderFixture.Double("Parallel " + i)))).ToList();

            UserGlazingSaveResult[] results = await Task.WhenAll(saves);

            Assert.All(results, x => Assert.True(x.Succeeded, x.Error));
            Assert.Equal(6, Library().Read().Systems.Count);
        }

        [Fact]
        public void A_SaveThatCannotGetTheLock_FailsExplicitly_AndWritesNothing()
        {
            UserGlazingLibrary library = Library(TimeSpan.FromMilliseconds(200));
            Saved(library.Save(BuilderFixture.Double("A")));
            string hash = Hash(library.Path);

            UserGlazingSaveResult result;
            using (new FileStream(library.LockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                result = library.Save(BuilderFixture.Double("B"));
            }

            Assert.False(result.Succeeded);
            Assert.Contains("another SAM window", result.Error);
            Assert.Equal(hash, Hash(library.Path));
            File.Delete(library.LockPath);
            Assert.True(library.Save(BuilderFixture.Double("B")).Succeeded);
        }

        [Fact]
        public void The_Provenance_RoundTrips_AsOneNamedParameterSet()
        {
            UserGlazingLibrary library = Library();
            GlazingSystemDraft draft = BuilderFixture.Double();
            draft.BasedOnName = "SIM_EXT_GLZ";
            draft.BasedOnGuid = BuilderFixture.SeedGuid;
            ((DraftPane)draft.Layers[2]).Reversed = true;
            draft.Frame.Width = 0.05;
            DateTime created = new DateTime(2026, 10, 2, 9, 30, 0, DateTimeKind.Utc);

            Saved(library.Save(draft, new GlazingValues(1.047, 0.525, 0.751, 2.2), created));

            ApertureConstruction saved = library.Read().Systems.Single();
            Assert.Single(saved.GetParameterSets(), x => x.Name == GlazingBuilderProvenance.ParameterSetName);
            GlazingBuilderProvenance provenance = GlazingBuilderProvenance.FromApertureConstruction(saved);
            Assert.Equal(GlazingBuilderProvenance.CurrentSchemaVersion, provenance.SchemaVersion);
            Assert.Equal(created, provenance.CreatedUtc);
            Assert.Equal("SIM_EXT_GLZ", provenance.BasedOnName);
            Assert.Equal(BuilderFixture.SeedGuid, provenance.BasedOnGuid);
            Assert.Equal(PanelType.WallExternal, provenance.IntendedPanelType);
            Assert.Equal(90, provenance.GapEvaluationTiltDegrees);
            Assert.Contains("EN 673", provenance.GapHeatTransferBasis);

            Assert.Equal(2, provenance.Panes.Count);
            GlazingBuilderPaneRecord outside = provenance.Panes[0];
            GlazingBuilderPaneRecord inside = provenance.Panes[1];
            Assert.Equal(1, outside.Position);
            Assert.Equal(BuilderFixture.Clear, outside.Material);
            Assert.Equal(BuilderFixture.Source, outside.SourceLabel);
            Assert.Equal(BuilderFixture.Source, outside.SourceFile);
            Assert.False(outside.Reversed);
            Assert.True(inside.Reversed);
            Assert.Equal(BuilderFixture.LowE + " Reversed", inside.Material);
            Assert.Equal(BuilderFixture.LowE, inside.OriginalName);
            Assert.Equal(0.004, inside.Thickness, 9);

            GlazingBuilderGapRecord gap = Assert.Single(provenance.Gaps);
            Assert.Equal("Argon", gap.Gas);
            Assert.Equal(0.016, gap.Thickness, 9);
            Assert.Equal(90, gap.TiltDegrees);
            Assert.False(double.IsNaN(gap.HeatTransferCoefficient));
            Assert.Equal(saved.PaneConstructionLayers[1].Name, gap.Material);

            Assert.Equal("Copied", provenance.Frame);
            Assert.Equal("SEED_GLZ", provenance.FrameCopiedFromName);
            Assert.Equal(BuilderFixture.SeedGuid, provenance.FrameCopiedFromGuid);
            Assert.Equal(0.05, provenance.FrameWidth, 9);
            Assert.Equal(1.047, provenance.Performance.Ug, 9);
            Assert.Equal(0.525, provenance.Performance.G, 9);
            Assert.Equal(0.751, provenance.Performance.LightTransmittance, 9);
            Assert.Equal(2.2, provenance.Performance.Uf, 9);
            Assert.Contains("Tas", provenance.PerformanceEngine);
        }

        [Fact]
        public void Without_Performance_NoSnapshotIsStored_AndAFramelessSystemSaysNone()
        {
            UserGlazingLibrary library = Library();
            GlazingSystemDraft draft = BuilderFixture.Double();
            draft.Frame = DraftFrame.None();

            Saved(library.Save(draft));

            GlazingBuilderProvenance provenance = GlazingBuilderProvenance.FromApertureConstruction(library.Read().Systems.Single());
            Assert.Null(provenance.Performance);
            Assert.Null(provenance.PerformanceEngine);
            Assert.Equal("None", provenance.Frame);
            Assert.True(double.IsNaN(provenance.FrameWidth));
        }

        [Fact]
        public void The_ProvenanceSet_FollowsSamsOneSetPerNameRule()
        {
            // PR2A-0 (SAM#146): sets are one per name; a set of the same name merges into it, later values winning.
            ApertureConstruction apertureConstruction = BuilderFixture.Compose(BuilderFixture.Double()).ApertureConstruction;
            GlazingBuilderProvenance provenance = new GlazingBuilderProvenance() { BasedOnName = "first", CreatedUtc = DateTime.UtcNow };
            apertureConstruction.Add(provenance.ToParameterSet());

            ParameterSet later = new ParameterSet(Guid.NewGuid(), GlazingBuilderProvenance.ParameterSetName);
            later.Add("Based On Name", "later");
            apertureConstruction.Add(later);

            ApertureConstruction roundTripped = new ApertureConstruction(apertureConstruction.ToJsonObject());
            Assert.Single(roundTripped.GetParameterSets(), x => x.Name == GlazingBuilderProvenance.ParameterSetName);
            Assert.Equal("later", GlazingBuilderProvenance.FromApertureConstruction(roundTripped).BasedOnName);
            Assert.Equal(GlazingBuilderProvenance.ParameterSetGuid, roundTripped.GetParameterSet(GlazingBuilderProvenance.ParameterSetName).Guid);
        }

        [Fact]
        public void No_AbsoluteSourcePath_IsStored_InTheSystemThatTravelsIntoModels()
        {
            UserGlazingLibrary library = Library();
            GlazingSystemDraft draft = BuilderFixture.Double();
            draft.Layers[0] = new DraftPane(BuilderFixture.ClearPane(), double.NaN, @"C:\Users\Somebody\Secret Projects\IGDB v76.tcd", @"\\server\share\glazing\IGDB v76.tcd");
            draft.Layers[2] = new DraftPane(BuilderFixture.LowEPane(), double.NaN, "D:/data/pilkington.json", "/home/user/pilkington.json");
            draft.Frame = DraftFrame.CopyFrom(BuilderFixture.Seed(), BuilderFixture.SeedMaterials(), @"C:\Users\Somebody\frames.json");

            ApertureConstruction saved = Saved(library.Save(draft)).Saved;

            string json = saved.ToJsonObject().ToJsonString();
            Assert.DoesNotMatch(@"[A-Za-z]:(\\\\|/)", json);
            Assert.DoesNotContain("Somebody", json);
            Assert.DoesNotContain("server", json);
            Assert.DoesNotContain("/home", json);
            GlazingBuilderProvenance provenance = GlazingBuilderProvenance.FromApertureConstruction(saved);
            Assert.Equal("IGDB v76.tcd", provenance.Panes[0].SourceFile);
            Assert.Equal("IGDB v76.tcd", provenance.Panes[0].SourceLabel);
            Assert.Equal("pilkington.json", provenance.Panes[1].SourceFile);
            Assert.Equal("pilkington.json", provenance.Panes[1].SourceLabel);
        }

        [Fact]
        public async Task Composing_CheckingAndEvaluating_NeverWriteTheLibrary()
        {
            // The "Cancel" half of the mutation matrix: everything before Save leaves the library file alone.
            UserGlazingLibrary library = Library();
            Saved(library.Save(BuilderFixture.Double("Existing")));
            string hash = Hash(library.Path);
            DateTime written = File.GetLastWriteTimeUtc(library.Path);

            GlazingSystemDraft draft = BuilderFixture.Triple("Not saved");
            BuilderFixture.Check(draft, library.Read().Systems.Select(x => x.Name));
            using (DraftGlazingEvaluator evaluator = new DraftGlazingEvaluator(new FakeDraftTas(), TimeSpan.Zero, null, BuilderFixture.Options()))
            {
                await evaluator.EvaluateAsync(draft);
            }

            Assert.Equal(hash, Hash(library.Path));
            Assert.Equal(written, File.GetLastWriteTimeUtc(library.Path));
            Assert.Single(library.Read().Systems);
        }

        [Fact]
        public void The_Builder_HoldsNoModel_AndCannotChangeOne()
        {
            // Structural: no Builder type has a field, property, parameter or call that reaches a UIAnalyticalModel / AnalyticalModel,
            // SetJSAMObject or an Undo path. (E0-1 needs no model at all.)
            IReadOnlyList<string> findings = BuilderSurface.ModelReferences();

            Assert.True(findings.Count == 0, string.Join("\n", findings));
            Assert.True(BuilderSurface.Types().Count() >= 15);
        }

        [Fact]
        public void The_StructuralScan_WouldSeeAModelReference()
        {
            // The scan is not vacuous: a type known to call SetJSAMObject is caught.
            Assert.NotEmpty(BuilderSurface.ModelReferences(typeof(ThermalEditSession)));
        }

        [Fact]
        public void Material_Merge_RenamesOnlyWhenTheDefinitionDiffers()
        {
            MaterialLibrary materialLibrary = new MaterialLibrary("t");
            Assert.Equal(BuilderFixture.Clear, GlazingMaterialMerge.Add(materialLibrary, BuilderFixture.ClearPane(), "a.tcd"));
            Assert.Equal(BuilderFixture.Clear, GlazingMaterialMerge.Add(materialLibrary, BuilderFixture.ClearPane(), "b.tcd"));
            Assert.Equal(BuilderFixture.Clear + " (b.tcd)", GlazingMaterialMerge.Add(materialLibrary, BuilderFixture.ClearPane(conductivity: 0.8), "b.tcd"));
            Assert.Equal(BuilderFixture.Clear + " (b.tcd)", GlazingMaterialMerge.Add(materialLibrary, BuilderFixture.ClearPane(conductivity: 0.8), "b.tcd"));
            Assert.Equal(BuilderFixture.Clear + " 2", GlazingMaterialMerge.Add(materialLibrary, BuilderFixture.ClearPane(conductivity: 0.7), "b.tcd"));
            Assert.Equal(BuilderFixture.Clear + " 3", GlazingMaterialMerge.Add(materialLibrary, BuilderFixture.ClearPane(conductivity: 0.6), null));
            Assert.Equal(4, materialLibrary.GetMaterials().Count);
            Assert.True(Regex.IsMatch(string.Join(",", materialLibrary.GetMaterials().Select(x => x.Name)), "Clear4 2"));
        }
    }
}
