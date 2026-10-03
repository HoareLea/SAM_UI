// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.UI.WPF.Tests.Helpers;
using SAM.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// User-library PR1: what can be done to a SAVED glazing system besides saving - <see cref="UserGlazingLibrary.Rename"/> (the label only:
    /// same Guid, layers, materials and provenance) and <see cref="UserGlazingLibrary.Remove"/> (a MOVE to the archive, never a delete) - and the
    /// archive's failure contract: <b>Remove never loses an entry</b>. Either it fully succeeds (the entry is in the archive and gone from the
    /// library) or the entry is still in the library; every failure of either file, an unreadable archive, a read-only archive and a folder on the
    /// archive's name are shown to leave the library exactly as it was, and a retry is idempotent. Each test uses its own temporary folder.
    /// </summary>
    public sealed class UserGlazingLibraryLifecycleTests : IDisposable
    {
        private readonly string directory = BuilderFixture.TempDirectory();
        private readonly UserGlazingLibrary library;
        private int changed;

        public UserGlazingLibraryLifecycleTests()
        {
            library = BuilderFixture.Library(directory);
            library.Changed += (sender, e) => changed++;
        }

        public void Dispose()
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

        // ---- Helpers ---------------------------------------------------------------------------------------------------

        private static string Hash(string path) => File.Exists(path) ? System.Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))) : "absent";

        private ApertureConstruction Save(GlazingSystemDraft draft)
        {
            UserGlazingSaveResult result = library.Save(draft, new GlazingValues(1.05, 0.52, 0.75, 1.8), new DateTime(2026, 10, 2, 12, 0, 0, DateTimeKind.Utc));
            Assert.True(result.Succeeded, result.Error);
            changed = 0;
            return result.Saved;
        }

        private static GlazingSystemDraft Tinted(string name = "Tinted")
        {
            GlazingSystemDraft result = BuilderFixture.Double(name);
            result.Layers[0] = BuilderFixture.Pane(BuilderFixture.TintPane());
            return result;
        }

        private static UserGlazingEditResult Succeeded(UserGlazingEditResult result)
        {
            Assert.True(result.Succeeded, result.Error);
            return result;
        }

        private static ConstructionManager Read(string path)
        {
            ConstructionManager result = UserLibraryFile.Parse(File.ReadAllText(path), out string error);
            Assert.True(result != null, error);
            return result;
        }

        private static List<ApertureConstruction> Systems(ConstructionManager constructionManager) => constructionManager.ApertureConstructions ?? new List<ApertureConstruction>();

        private static List<string> MaterialNames(ConstructionManager constructionManager) => (constructionManager.MaterialLibrary?.GetMaterials() ?? new List<IMaterial>()).Select(x => x.Name).OrderBy(x => x).ToList();

        private ConstructionManager Main() => Read(library.Path);

        private ConstructionManager Archive() => Read(library.ArchivePath);

        private static IEnumerable<string> LayerNames(ApertureConstruction system) => LibraryMaterialMerge.ReferencedNames(system).Distinct().OrderBy(x => x);

        // A library written by hand: the systems, the opaque constructions and every material given.
        private void WriteLibrary(IEnumerable<IMaterial> materials, IEnumerable<ApertureConstruction> systems, IEnumerable<Construction> constructions = null)
        {
            MaterialLibrary materialLibrary = new MaterialLibrary("User");
            foreach (IMaterial material in materials)
            {
                materialLibrary.Add(material);
            }

            File.WriteAllText(library.Path, new ConstructionManager(systems, constructions, materialLibrary) { Name = UserGlazingLibrary.LibraryName }.ToJsonObject().ToJsonString());
        }

        // ---- Rename ----------------------------------------------------------------------------------------------------

        [Fact]
        public void Rename_changes_the_name_only_the_guid_layers_materials_and_provenance_are_exactly_as_they_were()
        {
            GlazingSystemDraft draft = BuilderFixture.Double("Old name");
            draft.Frame.Width = 0.05;
            draft.BasedOnName = "SEED_GLZ";
            draft.BasedOnGuid = BuilderFixture.SeedGuid;
            ApertureConstruction saved = Save(draft);
            ApertureConstruction other = Save(BuilderFixture.Triple());
            ConstructionManager before = Main();
            JsonObject json_Before = Systems(before).Single(x => x.Guid == saved.Guid).ToJsonObject();

            UserGlazingEditResult result = Succeeded(library.Rename(saved.Guid, "New name"));

            ConstructionManager after = Main();
            ApertureConstruction renamed = Systems(after).Single(x => x.Guid == saved.Guid);
            Assert.Equal("New name", renamed.Name);
            Assert.Equal(saved.Guid, renamed.Guid);
            Assert.True(result.Modified);
            Assert.Equal(saved.Guid, result.Entry.Guid);
            json_Before["Name"] = "New name";
            Assert.Equal(json_Before.ToJsonString(), renamed.ToJsonObject().ToJsonString());
            Assert.Equal(GlazingBuilderProvenance.FromApertureConstruction(Systems(before).Single(x => x.Guid == saved.Guid)).Panes.Count, GlazingBuilderProvenance.FromApertureConstruction(renamed).Panes.Count);
            Assert.Single(renamed.GetParameterSets(), x => x.Name == GlazingBuilderProvenance.ParameterSetName);

            // Nothing else moved: the other system, the materials and the file's own description.
            Assert.Equal(Systems(before).Single(x => x.Guid == other.Guid).ToJsonObject().ToJsonString(), Systems(after).Single(x => x.Guid == other.Guid).ToJsonObject().ToJsonString());
            Assert.Equal(MaterialNames(before), MaterialNames(after));
            Assert.Equal(before.MaterialLibrary.GetMaterials().Select(x => x.ToJsonObject().ToJsonString()).OrderBy(x => x), after.MaterialLibrary.GetMaterials().Select(x => x.ToJsonObject().ToJsonString()).OrderBy(x => x));
            Assert.Equal(before.Description, after.Description);
            Assert.Equal(1, changed);
            Assert.False(File.Exists(library.ArchivePath));
        }

        [Fact]
        public void Rename_trims_the_name_and_keeps_the_previous_file_as_bak()
        {
            ApertureConstruction saved = Save(BuilderFixture.Double("A"));
            string previous = File.ReadAllText(library.Path);

            Succeeded(library.Rename(saved.Guid, "   Padded   "));

            Assert.Equal("Padded", library.Read().Systems.Single().Name);
            Assert.Equal(previous, File.ReadAllText(library.BackupPath));
            Assert.Empty(Directory.GetFiles(directory, "*.tmp"));
            Assert.False(File.Exists(library.LockPath));
        }

        [Fact]
        public void Rename_may_change_only_the_case_but_renaming_to_the_same_name_writes_and_announces_nothing()
        {
            ApertureConstruction saved = Save(BuilderFixture.Double("My window"));

            UserGlazingEditResult caseOnly = Succeeded(library.Rename(saved.Guid, "MY WINDOW"));
            Assert.True(caseOnly.Modified);
            Assert.Equal("MY WINDOW", library.Read().Systems.Single().Name);
            Assert.Equal(1, changed);

            string hash = Hash(library.Path);
            string bak = Hash(library.BackupPath);
            UserGlazingEditResult same = Succeeded(library.Rename(saved.Guid, "  MY WINDOW "));

            Assert.False(same.Modified);
            Assert.Equal("MY WINDOW", same.Entry.Name);
            Assert.Equal(hash, Hash(library.Path));
            Assert.Equal(bak, Hash(library.BackupPath));
            Assert.Equal(1, changed);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Rename_to_no_name_is_refused_and_writes_nothing(string name)
        {
            ApertureConstruction saved = Save(BuilderFixture.Double("A"));
            string hash = Hash(library.Path);

            UserGlazingEditResult result = library.Rename(saved.Guid, name);

            Assert.False(result.Succeeded);
            Assert.Equal("The system needs a name.", result.Error);
            Assert.Equal(hash, Hash(library.Path));
            Assert.False(File.Exists(library.BackupPath));
            Assert.Equal(0, changed);
        }

        [Theory]
        [InlineData("Second")]
        [InlineData("  second ")]
        [InlineData("SECOND")]
        public void Rename_to_the_name_of_another_system_is_refused_trimmed_and_ignoring_case_and_writes_nothing(string name)
        {
            ApertureConstruction first = Save(BuilderFixture.Double("First"));
            Save(BuilderFixture.Triple("Second"));
            string hash = Hash(library.Path);

            UserGlazingEditResult result = library.Rename(first.Guid, name);

            Assert.False(result.Succeeded);
            Assert.Contains("is already in My glazing systems", result.Error);
            Assert.Equal(hash, Hash(library.Path));
            Assert.Equal(0, changed);
            Assert.Equal("First", library.Read().Systems.Single(x => x.Guid == first.Guid).Name);
        }

        [Fact]
        public void Rename_of_an_unknown_guid_is_refused_and_writes_nothing()
        {
            Save(BuilderFixture.Double("A"));
            string hash = Hash(library.Path);

            UserGlazingEditResult result = library.Rename(Guid.NewGuid(), "B");

            Assert.False(result.Succeeded);
            Assert.Contains("no saved system with this Guid", result.Error);
            Assert.Equal(hash, Hash(library.Path));
            Assert.Equal(0, changed);

            // A missing file has no systems either, and stays missing.
            UserGlazingLibrary none = BuilderFixture.Library(BuilderFixture.TempDirectory());
            Assert.False(none.Rename(Guid.NewGuid(), "B").Succeeded);
            Assert.False(File.Exists(none.Path));
        }

        [Fact]
        public void Rename_never_overwrites_an_unreadable_file_and_fails_explicitly_when_the_lock_is_held()
        {
            ApertureConstruction saved = Save(BuilderFixture.Double("A"));
            UserGlazingLibrary quick = BuilderFixture.Library(directory, TimeSpan.FromMilliseconds(200));
            string hash = Hash(library.Path);

            UserGlazingEditResult locked;
            using (new FileStream(library.LockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                locked = quick.Rename(saved.Guid, "B");
            }

            Assert.False(locked.Succeeded);
            Assert.Contains("another SAM window", locked.Error);
            Assert.Equal(hash, Hash(library.Path));
            File.Delete(library.LockPath);

            File.WriteAllText(library.Path, "{ this is not json");
            string garbage = Hash(library.Path);
            UserGlazingEditResult unreadable = library.Rename(saved.Guid, "B");

            Assert.False(unreadable.Succeeded);
            Assert.Contains("left as it is", unreadable.Error);
            Assert.Equal(garbage, Hash(library.Path));
            Assert.Equal(0, changed);
        }

        [Fact]
        public void A_renamed_system_frees_its_old_name_for_a_new_save()
        {
            ApertureConstruction saved = Save(BuilderFixture.Double("Mine"));
            Succeeded(library.Rename(saved.Guid, "Mine, old"));

            UserGlazingSaveResult again = library.Save(BuilderFixture.Triple("Mine"));

            Assert.True(again.Succeeded, again.Error);
            Assert.Equal(new[] { "Mine", "Mine, old" }, library.Read().Systems.Select(x => x.Name).OrderBy(x => x));
        }

        // ---- Remove: the happy path ------------------------------------------------------------------------------------

        [Fact]
        public void Remove_moves_the_entry_and_every_material_it_used_to_the_archive_and_leaves_the_library_without_them()
        {
            ApertureConstruction saved = Save(BuilderFixture.Double("Only"));
            ApertureConstruction entry = library.Read().Systems.Single();
            string json = entry.ToJsonObject().ToJsonString();
            List<string> materials = MaterialNames(Main());
            Assert.NotEmpty(materials);

            UserGlazingEditResult result = Succeeded(library.Remove(saved.Guid));

            Assert.Equal(saved.Guid, result.Entry.Guid);
            Assert.True(result.Modified);
            Assert.Equal(materials, result.PrunedMaterials.OrderBy(x => x));
            ConstructionManager main = Main();
            Assert.Empty(Systems(main));
            Assert.Empty(MaterialNames(main));
            ConstructionManager archive = Archive();
            ApertureConstruction archived = Assert.Single(Systems(archive));
            Assert.Equal(json, archived.ToJsonObject().ToJsonString());                // moved, not altered
            Assert.Equal(materials, MaterialNames(archive));
            Assert.Equal(1, changed);
            Assert.Equal(Path.Combine(directory, "Glazing Systems.removed.json"), library.ArchivePath);
            Assert.Empty(Directory.GetFiles(directory, "*.tmp"));
            Assert.False(File.Exists(library.LockPath));
        }

        [Fact]
        public void The_archive_is_a_self_contained_source_that_add_source_can_read()
        {
            ApertureConstruction saved = Save(BuilderFixture.Double("Recoverable"));
            Succeeded(library.Remove(saved.Guid));

            GlazingSource source = Query.ReadThermalSource(library.ArchivePath);

            ApertureConstruction archived = Assert.Single(source.GetApertureConstructions(ApertureType.Window));
            Assert.Equal(saved.Guid, archived.Guid);
            Assert.All(LayerNames(archived), x => Assert.True(source.GetMaterials().ContainsKey(x), x));
            Assert.Null(new GlazingCandidate(archived, source, null).MaterialIssue);
        }

        [Fact]
        public void Remove_keeps_materials_another_system_still_uses_and_archives_all_of_the_removed_ones()
        {
            ApertureConstruction doubled = Save(BuilderFixture.Double("Double"));
            ApertureConstruction triple = Save(BuilderFixture.Triple());

            // The triple's 12 mm gas is its own; every pane and the frame are shared with the double.
            List<string> own = LayerNames(triple).Except(LayerNames(doubled)).ToList();
            Assert.Single(own);

            UserGlazingEditResult result = Succeeded(library.Remove(triple.Guid));

            Assert.Equal(own, result.PrunedMaterials);
            Assert.Equal(LayerNames(doubled), MaterialNames(Main()));
            Assert.Equal(new[] { "Double" }, library.Read().Systems.Select(x => x.Name));
            Assert.Equal(LayerNames(triple), MaterialNames(Archive()));                // the archive holds ALL of the triple's materials, pruned or not
        }

        [Fact]
        public void Remove_prunes_only_the_materials_no_remaining_system_uses()
        {
            ApertureConstruction keep = Save(BuilderFixture.Double("Keeps clear"));
            ApertureConstruction tinted = Save(Tinted());
            Assert.Contains(BuilderFixture.Tint, MaterialNames(Main()));

            UserGlazingEditResult result = Succeeded(library.Remove(tinted.Guid));

            Assert.Equal(new[] { BuilderFixture.Tint }, result.PrunedMaterials);
            ConstructionManager main = Main();
            Assert.DoesNotContain(BuilderFixture.Tint, MaterialNames(main));
            Assert.Equal(LayerNames(keep), MaterialNames(main));
            Assert.Contains(BuilderFixture.Tint, MaterialNames(Archive()));
            Assert.Equal(LayerNames(tinted), MaterialNames(Archive()));
        }

        [Fact]
        public void A_material_a_remaining_systems_frame_or_the_files_opaque_constructions_use_is_never_pruned_and_unrelated_materials_are_never_touched()
        {
            IMaterial frameB = new OpaqueMaterial(Guid.NewGuid(), "Frame B", "Frame B", "Frame", 0.2, 900, 600);
            IMaterial orphan = new OpaqueMaterial(Guid.NewGuid(), "Orphan", "Orphan", "Nobody uses me", 0.2, 900, 600);
            IMaterial wallOnly = new OpaqueMaterial(Guid.NewGuid(), "Wall only", "Wall only", "Wall", 0.2, 900, 600);
            Guid first = new Guid("e1000000-0000-4000-8000-000000000001");
            Guid second = new Guid("e1000000-0000-4000-8000-000000000002");
            ApertureConstruction removed = new ApertureConstruction(first, "Removed", ApertureType.Window,
                new List<ConstructionLayer>() { new ConstructionLayer(GlazingFixture.LowE, 0.006), new ConstructionLayer(GlazingFixture.Argon, 0.012), new ConstructionLayer(GlazingFixture.Clear, 0.006) },
                new List<ConstructionLayer>() { new ConstructionLayer("Frame B", 0.05), new ConstructionLayer("Wall only", 0.02) });
            ApertureConstruction remaining = new ApertureConstruction(second, "Remaining", ApertureType.Window,
                new List<ConstructionLayer>() { new ConstructionLayer(GlazingFixture.Clear, 0.006) },
                new List<ConstructionLayer>() { new ConstructionLayer("Frame B", 0.05) });                           // Frame B only in a FRAME layer
            Construction wall = new Construction(Guid.NewGuid(), "Opaque wall", new[] { new ConstructionLayer("Wall only", 0.1) });
            WriteLibrary(new[] { GlazingFixture.ClearGlass(), GlazingFixture.LowEGlass(), GlazingFixture.ArgonGas(), frameB, orphan, wallOnly }, new[] { removed, remaining }, new[] { wall });

            UserGlazingEditResult result = Succeeded(library.Remove(first));

            // Only LowE (and the gas) were used by the removed system alone. Frame B stays (a remaining system's frame), Wall only stays (an
            // opaque construction of the file), Clear stays (a remaining pane), Orphan was nobody's before and is not touched now.
            Assert.Equal(new[] { GlazingFixture.Argon, GlazingFixture.LowE }, result.PrunedMaterials.OrderBy(x => x));
            ConstructionManager main = Main();
            Assert.Equal(new[] { GlazingFixture.Clear, "Frame B", "Orphan", "Wall only" }, MaterialNames(main));
            Assert.Equal(new[] { "Remaining" }, Systems(main).Select(x => x.Name));
            Assert.Equal(new[] { "Opaque wall" }, (main.Constructions ?? new List<Construction>()).Select(x => x.Name));
            Assert.Equal(new[] { GlazingFixture.Argon, GlazingFixture.Clear, GlazingFixture.LowE, "Frame B", "Wall only" }.OrderBy(x => x), MaterialNames(Archive()));
        }

        [Fact]
        public void Remove_writes_the_archive_first_and_the_library_second_and_keeps_a_bak_of_each()
        {
            ApertureConstruction first = Save(BuilderFixture.Double("First"));
            ApertureConstruction second = Save(BuilderFixture.Triple("Second"));
            List<string> order = new List<string>();
            library.BeforeWrite = path => order.Add(Path.GetFileName(path));

            Succeeded(library.Remove(first.Guid));
            string main_AfterFirst = File.ReadAllText(library.Path);
            string archive_AfterFirst = File.ReadAllText(library.ArchivePath);
            Succeeded(library.Remove(second.Guid));

            Assert.Equal(new[] { "Glazing Systems.removed.json", "Glazing Systems.json", "Glazing Systems.removed.json", "Glazing Systems.json" }, order);
            Assert.Equal(main_AfterFirst, File.ReadAllText(library.BackupPath));
            Assert.Equal(archive_AfterFirst, File.ReadAllText(library.ArchivePath + ".bak"));
            Assert.Equal(new[] { "First", "Second" }, Systems(Archive()).Select(x => x.Name).OrderBy(x => x));
            Assert.Empty(Directory.GetFiles(directory, "*.tmp"));
        }

        [Fact]
        public void Remove_of_an_unknown_guid_is_refused_and_creates_no_archive()
        {
            Save(BuilderFixture.Double("A"));
            string hash = Hash(library.Path);

            UserGlazingEditResult result = library.Remove(Guid.NewGuid());

            Assert.False(result.Succeeded);
            Assert.Contains("no saved system with this Guid", result.Error);
            Assert.Equal(hash, Hash(library.Path));
            Assert.False(File.Exists(library.ArchivePath));
            Assert.Equal(0, changed);
        }

        [Fact]
        public void A_save_after_a_remove_reuses_the_freed_name_and_the_archive_keeps_the_old_system()
        {
            ApertureConstruction old = Save(BuilderFixture.Double("Mine"));
            Succeeded(library.Remove(old.Guid));

            ApertureConstruction again = Save(BuilderFixture.Triple("Mine"));

            Assert.NotEqual(old.Guid, again.Guid);
            Assert.Equal(new[] { again.Guid }, library.Read().Systems.Select(x => x.Guid));
            Assert.Equal(new[] { old.Guid }, Systems(Archive()).Select(x => x.Guid));
        }

        [Fact]
        public void Two_removals_accumulate_in_the_archive_and_a_different_material_of_an_archived_name_is_kept_under_a_new_name_with_its_layer_and_provenance()
        {
            ApertureConstruction a = Save(BuilderFixture.Double("A"));
            Succeeded(library.Remove(a.Guid));                                          // "Clear4" (1.0) leaves the library with it

            GlazingSystemDraft draft = BuilderFixture.Double("B");
            draft.Layers[0] = BuilderFixture.Pane(BuilderFixture.ClearPane(conductivity: 0.8));    // a DIFFERENT "Clear4"
            ApertureConstruction b = Save(draft);
            Assert.Equal(0.8, ((TransparentMaterial)Main().MaterialLibrary.GetMaterial(BuilderFixture.Clear)).ThermalConductivity, 9);

            Succeeded(library.Remove(b.Guid));

            ConstructionManager archive = Archive();
            Assert.Equal(new[] { "A", "B" }, Systems(archive).Select(x => x.Name).OrderBy(x => x));
            ApertureConstruction archivedA = Systems(archive).Single(x => x.Guid == a.Guid);
            ApertureConstruction archivedB = Systems(archive).Single(x => x.Guid == b.Guid);
            string renamed = BuilderFixture.Clear + " 2";
            Assert.Equal(BuilderFixture.Clear, archivedA.PaneConstructionLayers.Last().Name);
            Assert.Equal(renamed, archivedB.PaneConstructionLayers.Last().Name);
            Assert.Equal(1.0, ((TransparentMaterial)archive.MaterialLibrary.GetMaterial(BuilderFixture.Clear)).ThermalConductivity, 9);
            Assert.Equal(0.8, ((TransparentMaterial)archive.MaterialLibrary.GetMaterial(renamed)).ThermalConductivity, 9);
            Assert.Equal(renamed, GlazingBuilderProvenance.FromApertureConstruction(archivedB).Panes.First().Material);
            Assert.Equal(BuilderFixture.Clear, GlazingBuilderProvenance.FromApertureConstruction(archivedA).Panes.First().Material);
            Assert.Equal(BuilderFixture.Clear, GlazingBuilderProvenance.FromApertureConstruction(archivedB).Panes.First().OriginalName);
        }

        // ---- Remove: the failure contract ("Remove never loses an entry") ------------------------------------------------

        [Fact]
        public void When_the_archive_cannot_be_written_nothing_changes_and_a_retry_succeeds()
        {
            ApertureConstruction saved = Save(BuilderFixture.Double("A"));
            string main_Hash = Hash(library.Path);
            library.BeforeWrite = path =>
            {
                if (path == library.ArchivePath)
                {
                    throw new IOException("archive disk full");
                }
            };

            UserGlazingEditResult result = library.Remove(saved.Guid);

            Assert.False(result.Succeeded);
            Assert.Contains("Nothing was removed", result.Error);
            Assert.Contains("Glazing Systems.removed.json could not be written: archive disk full", result.Error);
            Assert.Equal(main_Hash, Hash(library.Path));
            Assert.Equal(new[] { saved.Guid }, library.Read().Systems.Select(x => x.Guid));
            Assert.False(File.Exists(library.ArchivePath));
            Assert.Empty(Directory.GetFiles(directory, "*.tmp"));
            Assert.False(File.Exists(library.LockPath));
            Assert.Equal(0, changed);

            library.BeforeWrite = null;
            Succeeded(library.Remove(saved.Guid));
            Assert.Empty(library.Read().Systems);
            Assert.Single(Systems(Archive()));
        }

        [Fact]
        public void When_the_library_cannot_be_written_after_the_archive_the_entry_is_still_in_the_library_and_a_retry_is_idempotent()
        {
            ApertureConstruction saved = Save(BuilderFixture.Double("A"));
            string main_Hash = Hash(library.Path);
            library.BeforeWrite = path =>
            {
                if (path == library.Path)
                {
                    throw new IOException("library disk full");
                }
            };

            UserGlazingEditResult result = library.Remove(saved.Guid);

            // The entry is in BOTH files: harmless - a Guid still in the library counts as not removed.
            Assert.False(result.Succeeded);
            Assert.Contains("Glazing Systems.json could not be written: library disk full", result.Error);
            Assert.Equal(main_Hash, Hash(library.Path));
            Assert.Equal(new[] { saved.Guid }, library.Read().Systems.Select(x => x.Guid));
            Assert.Equal(new[] { saved.Guid }, Systems(Archive()).Select(x => x.Guid));
            Assert.Equal(0, changed);
            Assert.False(File.Exists(library.LockPath));

            // Retry: the archive replaces the entry by Guid, so there is no duplicate.
            library.BeforeWrite = null;
            Succeeded(library.Remove(saved.Guid));
            Assert.Empty(library.Read().Systems);
            Assert.Equal(new[] { saved.Guid }, Systems(Archive()).Select(x => x.Guid));
            Assert.Equal(MaterialNames(Archive()).Distinct(), MaterialNames(Archive()));
            Assert.Equal(1, changed);
        }

        [Fact]
        public void A_retry_after_a_failed_remove_archives_the_entry_as_it_is_now_not_as_it_was()
        {
            ApertureConstruction saved = Save(BuilderFixture.Double("Before"));
            library.BeforeWrite = path =>
            {
                if (path == library.Path)
                {
                    throw new IOException("library disk full");
                }
            };
            Assert.False(library.Remove(saved.Guid).Succeeded);
            Assert.Equal("Before", Systems(Archive()).Single().Name);

            library.BeforeWrite = null;
            Succeeded(library.Rename(saved.Guid, "After"));
            Succeeded(library.Remove(saved.Guid));

            Assert.Empty(library.Read().Systems);
            Assert.Equal("After", Systems(Archive()).Single().Name);
        }

        [Fact]
        public void An_unreadable_archive_is_never_overwritten_and_nothing_is_removed()
        {
            ApertureConstruction saved = Save(BuilderFixture.Double("A"));
            File.WriteAllText(library.ArchivePath, "{ this is not json");
            string main_Hash = Hash(library.Path);
            string archive_Hash = Hash(library.ArchivePath);

            UserGlazingEditResult result = library.Remove(saved.Guid);

            Assert.False(result.Succeeded);
            Assert.Contains("Nothing was removed", result.Error);
            Assert.Contains("archive of removed entries cannot be used", result.Error);
            Assert.Contains("left as it is", result.Error);
            Assert.Equal(main_Hash, Hash(library.Path));
            Assert.Equal(archive_Hash, Hash(library.ArchivePath));
            Assert.False(File.Exists(library.ArchivePath + ".bak"));
            Assert.Equal(new[] { saved.Guid }, library.Read().Systems.Select(x => x.Guid));
            Assert.Equal(0, changed);
        }

        [Fact]
        public void A_read_only_archive_fails_the_remove_and_leaves_the_library_as_it_was()
        {
            ApertureConstruction first = Save(BuilderFixture.Double("First"));
            ApertureConstruction second = Save(BuilderFixture.Triple("Second"));
            Succeeded(library.Remove(first.Guid));
            changed = 0;
            File.SetAttributes(library.ArchivePath, FileAttributes.ReadOnly);
            string main_Hash = Hash(library.Path);
            string archive_Hash = Hash(library.ArchivePath);

            UserGlazingEditResult result = library.Remove(second.Guid);

            Assert.False(result.Succeeded);
            Assert.Contains("Nothing was removed", result.Error);
            Assert.Equal(main_Hash, Hash(library.Path));
            Assert.Equal(archive_Hash, Hash(library.ArchivePath));
            Assert.Equal(new[] { second.Guid }, library.Read().Systems.Select(x => x.Guid));
            Assert.Empty(Directory.GetFiles(directory, "*.tmp"));
            Assert.False(File.Exists(library.LockPath));
            Assert.Equal(0, changed);

            File.SetAttributes(library.ArchivePath, FileAttributes.Normal);
            Succeeded(library.Remove(second.Guid));
            Assert.Equal(2, Systems(Archive()).Count);
        }

        [Fact]
        public void A_folder_on_the_archives_name_fails_the_remove_and_leaves_the_library_as_it_was()
        {
            ApertureConstruction saved = Save(BuilderFixture.Double("A"));
            Directory.CreateDirectory(library.ArchivePath);
            string main_Hash = Hash(library.Path);

            UserGlazingEditResult result = library.Remove(saved.Guid);

            Assert.False(result.Succeeded);
            Assert.Contains("Nothing was removed", result.Error);
            Assert.Equal(main_Hash, Hash(library.Path));
            Assert.Equal(new[] { saved.Guid }, library.Read().Systems.Select(x => x.Guid));
            Assert.True(Directory.Exists(library.ArchivePath));
            Assert.Empty(Directory.GetFiles(directory, "*.tmp"));
            Assert.Empty(Directory.GetFiles(library.ArchivePath));
            Assert.False(File.Exists(library.LockPath));
            Assert.Equal(0, changed);
        }

        [Fact]
        public void Remove_never_overwrites_an_unreadable_library_and_fails_explicitly_when_the_lock_is_held()
        {
            ApertureConstruction saved = Save(BuilderFixture.Double("A"));
            UserGlazingLibrary quick = BuilderFixture.Library(directory, TimeSpan.FromMilliseconds(200));
            string hash = Hash(library.Path);

            UserGlazingEditResult locked;
            using (new FileStream(library.LockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                locked = quick.Remove(saved.Guid);
            }

            Assert.False(locked.Succeeded);
            Assert.Contains("another SAM window", locked.Error);
            Assert.Equal(hash, Hash(library.Path));
            Assert.False(File.Exists(library.ArchivePath));
            File.Delete(library.LockPath);

            File.WriteAllText(library.Path, "{ this is not json");
            string garbage = Hash(library.Path);
            UserGlazingEditResult unreadable = library.Remove(saved.Guid);

            Assert.False(unreadable.Succeeded);
            Assert.Equal(garbage, Hash(library.Path));
            Assert.False(File.Exists(library.ArchivePath));
            Assert.Equal(0, changed);
        }

        // ---- Changed ----------------------------------------------------------------------------------------------------

        [Fact]
        public void Changed_is_raised_once_per_successful_change_after_the_lock_is_released_and_a_failing_handler_changes_nothing()
        {
            ApertureConstruction first = Save(BuilderFixture.Double("First"));
            ApertureConstruction second = Save(BuilderFixture.Triple("Second"));
            bool lockFree = false;
            library.Changed += (sender, e) => lockFree = !File.Exists(library.LockPath);
            library.Changed += (sender, e) => throw new InvalidOperationException("a list that fails to refresh");
            int after = 0;
            library.Changed += (sender, e) => after++;

            Succeeded(library.Rename(first.Guid, "Renamed"));
            Assert.Equal(1, changed);
            Assert.True(lockFree);

            lockFree = false;
            Succeeded(library.Remove(second.Guid));
            Assert.Equal(2, changed);
            Assert.True(lockFree);
            Assert.Equal(2, after);
            Assert.Equal(new[] { "Renamed" }, library.Read().Systems.Select(x => x.Name));
        }

        // ---- An open candidate list --------------------------------------------------------------------------------------

        private GlazingViewModel OpenList()
        {
            GlazingViewModel viewModel = new GlazingViewModel(GlazingFixture.Model(2), GlazingFixture.CurrentGuid, null, new FakeGlazingEvaluator(), GlazingFixture.Library(), GlazingSource.FromUserLibrary(library));
            viewModel.InitializeAsync().Wait();
            return viewModel;
        }

        [Fact]
        public void An_open_list_shows_the_new_name_of_a_renamed_system_with_the_same_values()
        {
            ApertureConstruction saved = Save(BuilderFixture.Double("Old name"));
            using (GlazingViewModel viewModel = OpenList())
            {
                GlazingCandidateRow before = Assert.Single(viewModel.Rows, x => x.Guid == saved.Guid);
                Assert.Equal("Old name", before.Name);
                Assert.NotNull(before.Values);
                string ug = before.UgText;
                string uf = before.UfText;

                Succeeded(library.Rename(saved.Guid, "New name"));
                viewModel.SetUserSourceAsync(GlazingSource.FromUserLibrary(library)).Wait();

                GlazingCandidateRow after = Assert.Single(viewModel.Rows, x => x.Guid == saved.Guid);
                Assert.Equal("New name", after.Name);
                Assert.Equal(GlazingSourceKind.User, after.Candidate.Kind);
                Assert.NotNull(after.Values);
                Assert.Equal(ug, after.UgText);
                Assert.Equal(uf, after.UfText);
                Assert.Equal(before.Candidate.Guid, after.Candidate.Guid);
            }
        }

        [Fact]
        public void An_open_list_drops_a_removed_system_and_resets_a_choice_of_it()
        {
            ApertureConstruction saved = Save(BuilderFixture.Double("Doomed"));
            ApertureConstruction stays = Save(BuilderFixture.Triple("Stays"));
            using (GlazingViewModel viewModel = OpenList())
            {
                viewModel.SelectedGuid = saved.Guid;
                Assert.Equal(saved.Guid, viewModel.SelectedGuid);

                Succeeded(library.Remove(saved.Guid));
                viewModel.SetUserSourceAsync(GlazingSource.FromUserLibrary(library)).Wait();

                Assert.DoesNotContain(viewModel.Rows, x => x.Guid == saved.Guid);
                Assert.Single(viewModel.Rows, x => x.Guid == stays.Guid);
                Assert.Null(viewModel.SelectedGuid);
            }
        }
    }
}
