// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.UI.WPF.Tests.Helpers;
using SAM.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// User-library PR3: <see cref="UserGlazingLibrary.SaveReplacing"/> ("Save and replace") - ONE locked transaction that saves the draft as a NEW system
    /// (new Guid, Schema-2 provenance saying what it supersedes) and moves the replaced system to the archive. The replaced system's name may be reused
    /// but only when replacing; a replaced system that is gone is an error; and the failure contract of Remove holds: the library is left exactly as it was
    /// and the replaced system is still in it (the new one is NOT saved unless the old one is archived). Version-1 provenance still reads. Each test uses
    /// its own temporary folder.
    /// </summary>
    public sealed class UserGlazingLibraryReplaceTests : IDisposable
    {
        private readonly string directory = BuilderFixture.TempDirectory();
        private readonly UserGlazingLibrary library;
        private int changed;

        public UserGlazingLibraryReplaceTests()
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

        private static string Hash(string path) => File.Exists(path) ? System.Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))) : "absent";

        private ApertureConstruction Save(GlazingSystemDraft draft)
        {
            UserGlazingSaveResult result = library.Save(draft, new GlazingValues(1.05, 0.52, 0.75, 1.8), new DateTime(2026, 10, 2, 12, 0, 0, DateTimeKind.Utc));
            Assert.True(result.Succeeded, result.Error);
            changed = 0;
            return result.Saved;
        }

        private static UserGlazingSaveResult Replaced(UserGlazingSaveResult result)
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

        private static IEnumerable<string> LayerNames(ApertureConstruction system) => LibraryMaterialMerge.ReferencedNames(system).Distinct().OrderBy(x => x);

        private static GlazingSystemDraft Tinted(string name)
        {
            GlazingSystemDraft result = BuilderFixture.Double(name);
            result.Layers[0] = BuilderFixture.Pane(BuilderFixture.TintPane());
            return result;
        }

        // ---- The happy path ----------------------------------------------------------------------------------------------

        [Fact]
        public void Save_and_replace_saves_a_new_system_and_moves_the_old_one_to_the_archive_in_one_change()
        {
            ApertureConstruction old = Save(BuilderFixture.Double("Window A"));
            ApertureConstruction other = Save(BuilderFixture.Triple("Window B"));
            string oldJson = library.Read().Systems.Single(x => x.Guid == old.Guid).ToJsonObject().ToJsonString();

            GlazingSystemDraft draft = Tinted("Window A");                       // the OLD name, reused
            draft.BasedOnName = old.Name;
            draft.BasedOnGuid = old.Guid;
            UserGlazingSaveResult result = Replaced(library.SaveReplacing(draft, old.Guid, new GlazingValues(0.9, 0.4, 0.7, 1.5), new DateTime(2026, 10, 3, 8, 0, 0, DateTimeKind.Utc)));

            ApertureConstruction saved = result.Saved;
            Assert.NotEqual(old.Guid, saved.Guid);
            Assert.Equal("Window A", saved.Name);
            Assert.Equal(old.Guid, result.Replaced.Guid);
            Assert.Equal(1, changed);

            List<ApertureConstruction> main = library.Read().Systems;
            Assert.Equal(new[] { saved.Guid, other.Guid }.OrderBy(x => x), main.Select(x => x.Guid).OrderBy(x => x));
            Assert.Equal(new[] { "Window A", "Window B" }, main.Select(x => x.Name).OrderBy(x => x));

            // The old system is in the archive exactly as it was.
            ApertureConstruction archived = Assert.Single(Systems(Read(library.ArchivePath)));
            Assert.Equal(old.Guid, archived.Guid);
            Assert.Equal(oldJson, archived.ToJsonObject().ToJsonString());

            // The new system says what it replaced.
            GlazingBuilderProvenance provenance = GlazingBuilderProvenance.FromApertureConstruction(saved);
            Assert.Equal(2, provenance.SchemaVersion);
            Assert.Equal(old.Guid, provenance.SupersedesGuid);
            Assert.Equal("Window A", provenance.SupersedesName);
            Assert.Equal(old.Guid, provenance.BasedOnGuid);
            Assert.Equal(new DateTime(2026, 10, 3, 8, 0, 0, DateTimeKind.Utc), provenance.CreatedUtc);
            Assert.Equal(0.9, provenance.Performance.Ug, 9);
            Assert.Contains("; replaces Window A (" + old.Guid + ")", Query.GlazingBuiltFrom(provenance)[0]);
            Assert.DoesNotContain(directory, string.Join(Environment.NewLine, Query.GlazingBuiltFrom(provenance)));

            Assert.Empty(Directory.GetFiles(directory, "*.tmp"));
            Assert.False(File.Exists(library.LockPath));
        }

        [Fact]
        public void The_materials_follow_the_replaced_system_into_the_archive_and_only_what_nobody_uses_any_more_leaves_the_library()
        {
            ApertureConstruction old = Save(BuilderFixture.Double("Clear and low-e"));
            ApertureConstruction other = Save(BuilderFixture.Triple("Triple"));

            // The replacement uses a tinted pane and a 20 mm gap; the triple still uses clear, low-e, 12 mm gaps and the frame.
            GlazingSystemDraft draft = Tinted("Tinted and low-e");
            draft.Layers[1] = BuilderFixture.Gap(20);
            UserGlazingSaveResult result = Replaced(library.SaveReplacing(draft, old.Guid));

            ConstructionManager main = Read(library.Path);
            List<string> expected = LayerNames(other).Concat(LayerNames(result.Saved)).Distinct().OrderBy(x => x).ToList();
            Assert.Equal(expected, MaterialNames(main));                                      // exactly what the remaining systems use
            Assert.Contains(BuilderFixture.Tint, MaterialNames(main));
            Assert.Equal(LayerNames(old), MaterialNames(Read(library.ArchivePath)));          // everything the old one used is archived with it

            // Only the old system's own 16 mm gas left the library (not clear / low-e / frame, which the triple uses).
            List<string> gone = LayerNames(old).Except(MaterialNames(main)).ToList();
            Assert.Single(gone);
            Assert.StartsWith("Argon_16mm", gone[0]);
        }

        [Fact]
        public void A_replacement_may_reuse_the_old_name_but_not_the_name_of_another_system_and_a_plain_save_may_not_reuse_either()
        {
            ApertureConstruction old = Save(BuilderFixture.Double("Window A"));
            Save(BuilderFixture.Triple("Window B"));
            string hash = Hash(library.Path);

            UserGlazingSaveResult plain = library.Save(BuilderFixture.Double("Window A"));
            UserGlazingSaveResult stolen = library.SaveReplacing(BuilderFixture.Double("  window b "), old.Guid);

            Assert.False(plain.Succeeded);
            Assert.True(plain.Validation.Has(GlazingDraftIssueCodes.DuplicateName));
            Assert.False(stolen.Succeeded);
            Assert.True(stolen.Validation.Has(GlazingDraftIssueCodes.DuplicateName));
            Assert.Equal(hash, Hash(library.Path));
            Assert.False(File.Exists(library.ArchivePath));
            Assert.Equal(0, changed);

            Assert.True(library.SaveReplacing(BuilderFixture.Double("window a"), old.Guid).Succeeded);   // the old name, any case
        }

        [Fact]
        public void A_replacement_with_errors_or_without_a_name_writes_nothing()
        {
            ApertureConstruction old = Save(BuilderFixture.Double("Window A"));
            string hash = Hash(library.Path);
            GlazingSystemDraft gasAtEdge = BuilderFixture.Double("Gas at the edge");
            gasAtEdge.Layers.Add(BuilderFixture.Gap());

            UserGlazingSaveResult first = library.SaveReplacing(gasAtEdge, old.Guid);
            UserGlazingSaveResult second = library.SaveReplacing(BuilderFixture.Double(" "), old.Guid);
            UserGlazingSaveResult third = library.SaveReplacing(null, old.Guid);

            Assert.False(first.Succeeded);
            Assert.True(first.Validation.Has(GlazingDraftIssueCodes.GasAtEdge));
            Assert.False(second.Succeeded);
            Assert.True(second.Validation.Has(GlazingDraftIssueCodes.NameRequired));
            Assert.False(third.Succeeded);
            Assert.Equal(hash, Hash(library.Path));
            Assert.False(File.Exists(library.ArchivePath));
            Assert.Equal(0, changed);
        }

        [Fact]
        public void A_system_that_is_no_longer_in_the_library_cannot_be_replaced_and_nothing_is_written()
        {
            ApertureConstruction old = Save(BuilderFixture.Double("Window A"));
            Assert.True(library.Remove(old.Guid).Succeeded);
            changed = 0;
            string main_Hash = Hash(library.Path);
            string archive_Hash = Hash(library.ArchivePath);

            UserGlazingSaveResult result = library.SaveReplacing(BuilderFixture.Triple("Window A"), old.Guid);
            UserGlazingSaveResult unknown = library.SaveReplacing(BuilderFixture.Triple("Window A"), Guid.NewGuid());

            Assert.False(result.Succeeded);
            Assert.Contains("no longer in My glazing systems", result.Error);
            Assert.Contains("save this one as a new system instead", result.Error);
            Assert.False(unknown.Succeeded);
            Assert.Equal(main_Hash, Hash(library.Path));
            Assert.Equal(archive_Hash, Hash(library.ArchivePath));
            Assert.Equal(0, changed);
        }

        // ---- The failure contract ------------------------------------------------------------------------------------------

        [Fact]
        public void When_the_archive_cannot_be_written_both_files_are_untouched_and_the_old_system_stays()
        {
            ApertureConstruction old = Save(BuilderFixture.Double("Window A"));
            string hash = Hash(library.Path);
            library.BeforeWrite = path =>
            {
                if (path == library.ArchivePath)
                {
                    throw new IOException("archive disk full");
                }
            };

            UserGlazingSaveResult result = library.SaveReplacing(Tinted("Window A"), old.Guid);

            Assert.False(result.Succeeded);
            Assert.Contains("Nothing was saved or replaced", result.Error);
            Assert.Contains("archive disk full", result.Error);
            Assert.Equal(hash, Hash(library.Path));
            Assert.False(File.Exists(library.ArchivePath));
            Assert.Equal(new[] { old.Guid }, library.Read().Systems.Select(x => x.Guid));
            Assert.Empty(Directory.GetFiles(directory, "*.tmp"));
            Assert.False(File.Exists(library.LockPath));
            Assert.Equal(0, changed);

            library.BeforeWrite = null;
            Assert.True(library.SaveReplacing(Tinted("Window A"), old.Guid).Succeeded);
        }

        [Fact]
        public void When_the_library_cannot_be_written_after_the_archive_the_old_system_is_still_there_the_new_one_is_not_and_a_retry_succeeds_without_duplicates()
        {
            ApertureConstruction old = Save(BuilderFixture.Double("Window A"));
            string hash = Hash(library.Path);
            library.BeforeWrite = path =>
            {
                if (path == library.Path)
                {
                    throw new IOException("library disk full");
                }
            };

            UserGlazingSaveResult result = library.SaveReplacing(Tinted("Window A"), old.Guid);

            Assert.False(result.Succeeded);
            Assert.Contains("Glazing Systems.json could not be written: library disk full", result.Error);
            Assert.Equal(hash, Hash(library.Path));                                           // the library is exactly as it was
            Assert.Equal(new[] { old.Guid }, library.Read().Systems.Select(x => x.Guid));
            Assert.Equal(new[] { old.Guid }, Systems(Read(library.ArchivePath)).Select(x => x.Guid));   // harmless: archived, still listed
            Assert.Equal(0, changed);

            library.BeforeWrite = null;
            UserGlazingSaveResult retry = Replaced(library.SaveReplacing(Tinted("Window A"), old.Guid));
            Assert.Equal(new[] { retry.Saved.Guid }, library.Read().Systems.Select(x => x.Guid));
            Assert.Equal(new[] { old.Guid }, Systems(Read(library.ArchivePath)).Select(x => x.Guid));
            Assert.Equal(1, changed);
        }

        [Fact]
        public void An_unreadable_or_read_only_archive_refuses_the_replacement_and_nothing_is_written()
        {
            ApertureConstruction old = Save(BuilderFixture.Double("Window A"));
            string hash = Hash(library.Path);

            File.WriteAllText(library.ArchivePath, "{ this is not json");
            string garbage = Hash(library.ArchivePath);
            UserGlazingSaveResult unreadable = library.SaveReplacing(Tinted("Window A"), old.Guid);
            Assert.False(unreadable.Succeeded);
            Assert.Contains("Nothing was saved or replaced", unreadable.Error);
            Assert.Contains("left as it is", unreadable.Error);
            Assert.Equal(hash, Hash(library.Path));
            Assert.Equal(garbage, Hash(library.ArchivePath));

            File.Delete(library.ArchivePath);
            ApertureConstruction other = Save(BuilderFixture.Triple("Window B"));
            Assert.True(library.Remove(other.Guid).Succeeded);                                   // creates a valid archive
            changed = 0;
            File.SetAttributes(library.ArchivePath, FileAttributes.ReadOnly);
            string main_Hash = Hash(library.Path);
            string archive_Hash = Hash(library.ArchivePath);

            UserGlazingSaveResult readOnly = library.SaveReplacing(Tinted("Window A"), old.Guid);

            Assert.False(readOnly.Succeeded);
            Assert.Equal(main_Hash, Hash(library.Path));
            Assert.Equal(archive_Hash, Hash(library.ArchivePath));
            Assert.Equal(0, changed);
        }

        [Fact]
        public void Save_and_replace_never_overwrites_an_unreadable_library_and_fails_explicitly_when_the_lock_is_held()
        {
            ApertureConstruction old = Save(BuilderFixture.Double("Window A"));
            UserGlazingLibrary quick = BuilderFixture.Library(directory, TimeSpan.FromMilliseconds(200));
            string hash = Hash(library.Path);

            UserGlazingSaveResult locked;
            using (new FileStream(library.LockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                locked = quick.SaveReplacing(Tinted("Window A"), old.Guid);
            }

            Assert.False(locked.Succeeded);
            Assert.Contains("another SAM window", locked.Error);
            Assert.Equal(hash, Hash(library.Path));
            File.Delete(library.LockPath);

            File.WriteAllText(library.Path, "{ this is not json");
            Assert.False(library.SaveReplacing(Tinted("Window A"), old.Guid).Succeeded);
            Assert.Equal("{ this is not json", File.ReadAllText(library.Path));
            Assert.False(File.Exists(library.ArchivePath));
        }

        // ---- Provenance: version 2 reads version 1 ------------------------------------------------------------------------

        [Fact]
        public void Version_one_provenance_still_reads_and_says_nothing_about_replacing()
        {
            GlazingBuilderProvenance v1 = new GlazingBuilderProvenance()
            {
                SchemaVersion = 1,
                CreatedUtc = new DateTime(2026, 10, 1, 9, 0, 0, DateTimeKind.Utc),
                BasedOnName = "SIM_EXT_GLZ",
                Panes = new List<GlazingBuilderPaneRecord>() { new GlazingBuilderPaneRecord() { Position = 1, Material = "Clear4", OriginalName = "Clear4", Thickness = 0.004 } },
                Frame = "None",
            };
            SAM.Core.ParameterSet set = v1.ToParameterSet();
            Assert.False(set.Contains("Supersedes Guid"));
            Assert.False(set.Contains("Supersedes Name"));

            ApertureConstruction system = BuilderFixture.Seed(false);
            system.Add(set);
            GlazingBuilderProvenance read = GlazingBuilderProvenance.FromApertureConstruction(system);

            Assert.Equal(1, read.SchemaVersion);
            Assert.Null(read.SupersedesGuid);
            Assert.Null(read.SupersedesName);
            Assert.Equal("SIM_EXT_GLZ", read.BasedOnName);
            Assert.DoesNotContain("replaces", Query.GlazingBuiltFrom(read)[0]);

            // A system saved before this version is renamed, removed and listed exactly as before.
            string path = library.Path;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            MaterialLibrary materials = BuilderFixture.SeedMaterials();
            File.WriteAllText(path, new ConstructionManager(new[] { system }, null, materials) { Name = UserGlazingLibrary.LibraryName }.ToJsonObject().ToJsonString());
            Assert.True(library.Rename(system.Guid, "Renamed v1").Succeeded);
            GlazingBuilderProvenance afterRename = GlazingBuilderProvenance.FromApertureConstruction(library.Read().Systems.Single());
            Assert.Equal(1, afterRename.SchemaVersion);
        }

        [Fact]
        public void Every_new_save_is_schema_two_and_a_plain_save_supersedes_nothing()
        {
            ApertureConstruction saved = Save(BuilderFixture.Double("Plain"));

            GlazingBuilderProvenance provenance = GlazingBuilderProvenance.FromApertureConstruction(library.Read().Systems.Single(x => x.Guid == saved.Guid));

            Assert.Equal(GlazingBuilderProvenance.CurrentSchemaVersion, provenance.SchemaVersion);
            Assert.Equal(2, provenance.SchemaVersion);
            Assert.Null(provenance.SupersedesGuid);
            Assert.False(library.Read().Systems.Single().GetParameterSet(GlazingBuilderProvenance.ParameterSetName).Contains("Supersedes Guid"));
        }
    }
}
