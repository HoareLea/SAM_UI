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
    /// PR4: <see cref="UserConstructionLibrary"/> - "My constructions", the opaque twin of "My glazing systems" on the same engine. A Save adds a NEW
    /// construction (new Guid, trimmed unique name, every material embedded, provenance attached) and rejects what is not an opaque construction whose
    /// materials are all known; a saved construction is immutable (Rename = label only, Remove = archive-first move, never a delete); a failed write
    /// leaves the library as it was and raises nothing; the file is its own (<c>Constructions.json</c>), never the glazing file. Each test uses its own
    /// temporary folder.
    /// </summary>
    public sealed class UserConstructionLibraryTests : IDisposable
    {
        private readonly string directory = UserConstructionFixture.TempDirectory();
        private readonly UserConstructionLibrary library;
        private int changed;

        public UserConstructionLibraryTests()
        {
            library = UserConstructionFixture.Library(directory);
            library.Changed += (sender, e) => changed++;
        }

        public void Dispose()
        {
            UserConstructionFixture.Delete(directory);
        }

        private static string Hash(string path) => File.Exists(path) ? System.Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))) : "absent";

        private Construction Save(string name = "My wall", Construction construction = null, MaterialLibrary materials = null, UserConstructionProvenance provenance = null)
        {
            UserConstructionSaveResult result = library.Save(construction ?? UserConstructionFixture.Wall("Source"), materials ?? UserConstructionFixture.Materials(), name, provenance ?? UserConstructionFixture.Provenance(), new DateTime(2026, 10, 4, 12, 0, 0, DateTimeKind.Utc));
            Assert.True(result.Succeeded, result.Error);
            changed = 0;
            return result.Saved;
        }

        private ConstructionManager Main() => UserConstructionFixture.Read(library.Path);

        private ConstructionManager Archive() => UserConstructionFixture.Read(library.ArchivePath);

        private static IEnumerable<string> LayerNames(Construction construction) => LibraryMaterialMerge.ReferencedNames(construction).Distinct().OrderBy(x => x);

        // ---- Save ------------------------------------------------------------------------------------------------------

        [Fact]
        public void Save_adds_a_new_construction_with_a_new_guid_the_trimmed_name_the_same_layers_and_the_provenance()
        {
            Construction source = UserConstructionFixture.Wall("Source", 0.13);
            source.SetValue(ConstructionParameter.DefaultPanelType, "Wall");

            UserConstructionSaveResult result = library.Save(source, UserConstructionFixture.Materials(), "  My wall  ", UserConstructionFixture.Provenance(), new DateTime(2026, 10, 4, 12, 0, 0, DateTimeKind.Utc));

            Assert.True(result.Succeeded, result.Error);
            Assert.Equal("My wall", result.Saved.Name);
            Assert.NotEqual(source.Guid, result.Saved.Guid);
            Assert.NotEqual(Guid.Empty, result.Saved.Guid);
            Assert.Equal(1, changed);

            ConstructionManager file = Main();
            Construction saved = Assert.Single(UserConstructionFixture.Constructions(file));
            Assert.Equal(result.Saved.Guid, saved.Guid);
            Assert.Equal("My wall", saved.Name);
            Assert.Equal(source.ConstructionLayers.Select(x => x.Name + "|" + x.Thickness), saved.ConstructionLayers.Select(x => x.Name + "|" + x.Thickness));
            Assert.True(saved.TryGetValue(ConstructionParameter.DefaultPanelType, out string panelType));
            Assert.Equal("Wall", panelType);

            UserConstructionProvenance provenance = UserConstructionProvenance.FromConstruction(saved);
            Assert.NotNull(provenance);
            Assert.Equal(1, provenance.SchemaVersion);
            Assert.Equal(new DateTime(2026, 10, 4, 12, 0, 0, DateTimeKind.Utc), provenance.CreatedUtc);
            Assert.Equal(0.18, provenance.ThermalTransmittance, 6);
            Assert.Equal("Project X", provenance.OriginModelName);
            Assert.Equal(UserConstructionProvenance.TasEngine, provenance.Engine);
            Assert.False(string.IsNullOrWhiteSpace(provenance.SamTasVersion));
        }

        [Fact]
        public void Save_embeds_every_material_the_construction_uses_and_only_those()
        {
            MaterialLibrary materials = UserConstructionFixture.Materials();
            materials.Add(new OpaqueMaterial(Guid.NewGuid(), "Unused", "Unused", "Fixture", 0.5, 1000, 1000));

            Save("My wall", materials: materials);

            ConstructionManager file = Main();
            Assert.Equal(new[] { UserConstructionFixture.Materials().GetMaterial(UValueFixture.Air).Name, UValueFixture.Board, UValueFixture.Rainscreen, UValueFixture.Wool }.OrderBy(x => x), UserConstructionFixture.MaterialNames(file));
            foreach (string name in UserConstructionFixture.MaterialNames(file))
            {
                Assert.True(MaterialIdentity.Same(materials.GetMaterial(name), file.MaterialLibrary.GetMaterial(name)), name);
            }
        }

        [Fact]
        public void Every_save_is_a_new_construction_even_of_the_same_source_and_a_name_is_unique_ignoring_case_and_spaces()
        {
            Construction a = Save("My wall");
            Construction source = UserConstructionFixture.Wall("Source");

            UserConstructionSaveResult clash = library.Save(source, UserConstructionFixture.Materials(), "  MY WALL ", UserConstructionFixture.Provenance());
            Assert.False(clash.Succeeded);
            Assert.Contains("already in My constructions", clash.Error);
            Assert.Equal(0, changed);

            Construction b = Save("My wall 2", source);
            Construction c = Save("My wall 3", source);
            Assert.Equal(3, new[] { a.Guid, b.Guid, c.Guid }.Distinct().Count());
            Assert.Equal(3, UserConstructionFixture.Constructions(Main()).Count);
        }

        [Fact]
        public void Save_rejects_a_transparent_a_gas_only_a_layerless_and_a_missing_material_construction_and_writes_nothing()
        {
            Construction glass = UserConstructionFixture.Glass(out MaterialLibrary glassMaterials);
            UserConstructionSaveResult r1 = library.Save(glass, glassMaterials, "Glass", UserConstructionFixture.Provenance());
            Assert.False(r1.Succeeded);
            Assert.Contains("only opaque constructions", r1.Error);
            Assert.Contains("transparent", r1.Error);

            Construction gas = UserConstructionFixture.GasOnly(out MaterialLibrary gasMaterials);
            UserConstructionSaveResult r2 = library.Save(gas, gasMaterials, "Gas", UserConstructionFixture.Provenance());
            Assert.False(r2.Succeeded);
            Assert.Contains("gas", r2.Error);

            MaterialLibrary withoutWool = UserConstructionFixture.Materials();
            withoutWool.Remove(withoutWool.GetMaterial(UValueFixture.Wool));
            UserConstructionSaveResult r3 = library.Save(UserConstructionFixture.Wall("Source"), withoutWool, "Wall", UserConstructionFixture.Provenance());
            Assert.False(r3.Succeeded);
            Assert.Contains(UValueFixture.Wool, r3.Error);
            Assert.Contains("not in the material library", r3.Error);

            UserConstructionSaveResult r4 = library.Save(new Construction("Empty"), UserConstructionFixture.Materials(), "Empty", UserConstructionFixture.Provenance());
            Assert.False(r4.Succeeded);
            UserConstructionSaveResult r5 = library.Save(null, UserConstructionFixture.Materials(), "Null", UserConstructionFixture.Provenance());
            Assert.False(r5.Succeeded);
            UserConstructionSaveResult r6 = library.Save(UserConstructionFixture.Wall("Source"), UserConstructionFixture.Materials(), "   ", UserConstructionFixture.Provenance());
            Assert.False(r6.Succeeded);
            Assert.Contains("needs a name", r6.Error);

            Assert.False(File.Exists(library.Path));
            Assert.Equal(0, changed);
            Assert.Equal(UserConstructionLibrary.Rejection(glass, glassMaterials), r1.Error);
            Assert.Null(UserConstructionLibrary.Rejection(UserConstructionFixture.Wall("Source"), UserConstructionFixture.Materials()));
        }

        [Fact]
        public void Save_never_changes_the_construction_the_materials_or_the_provenance_it_was_given()
        {
            Construction source = UserConstructionFixture.Wall("Source");
            MaterialLibrary materials = UserConstructionFixture.Materials();
            UserConstructionProvenance provenance = UserConstructionFixture.Provenance();
            string json_Construction = source.ToJsonObject().ToJsonString();
            string json_Materials = materials.ToJsonObject().ToJsonString();
            DateTime created = provenance.CreatedUtc;

            Assert.True(library.Save(source, materials, "My wall", provenance).Succeeded);

            Assert.Equal(json_Construction, source.ToJsonObject().ToJsonString());
            Assert.Equal(json_Materials, materials.ToJsonObject().ToJsonString());
            Assert.Equal(created, provenance.CreatedUtc);
            Assert.Null(source.GetParameterSet(UserConstructionProvenance.ParameterSetName));
        }

        [Fact]
        public void A_different_material_of_the_same_name_is_saved_under_a_new_name_with_the_layers_following_and_an_identical_one_is_reused()
        {
            Construction first = Save("First");
            Assert.Contains(UValueFixture.Wool, LayerNames(first));

            // A second wall whose wool is another definition under the same name.
            MaterialLibrary other = UserConstructionFixture.Materials();
            other.Remove(other.GetMaterial(UValueFixture.Wool));
            other.Add(UserConstructionFixture.OtherWool());
            UserConstructionProvenance provenance = UserConstructionFixture.Provenance(UserConstructionOrigin.AddedSource);
            provenance.SavedFromSource = @"C:\Users\someone\Docs\Source Library.tcd";

            UserConstructionSaveResult result = library.Save(UserConstructionFixture.Wall("Second"), other, "Second", provenance);

            Assert.True(result.Succeeded, result.Error);
            string renamed = Assert.Single(result.RenamedMaterials).Value;
            Assert.Equal(UValueFixture.Wool + " (Source Library.tcd)", renamed);
            Assert.Contains(renamed, LayerNames(result.Saved));
            Assert.DoesNotContain(UValueFixture.Wool, LayerNames(result.Saved));
            Assert.Contains(renamed, result.AddedMaterials);

            ConstructionManager file = Main();
            Assert.Contains(UValueFixture.Wool, UserConstructionFixture.MaterialNames(file));
            Assert.Contains(renamed, UserConstructionFixture.MaterialNames(file));
            // The first construction still has its own wool; the identical materials (gas, board, rainscreen) were reused, not duplicated.
            Assert.Contains(UValueFixture.Wool, LayerNames(UserConstructionFixture.Constructions(file).Single(x => x.Guid == first.Guid)));
            Assert.Equal(UserConstructionFixture.MaterialNames(file).Count, UserConstructionFixture.MaterialNames(file).Distinct().Count());
            Assert.Equal(5, UserConstructionFixture.MaterialNames(file).Count);

            // The folder of the source is never recorded.
            UserConstructionProvenance saved = UserConstructionProvenance.FromConstruction(UserConstructionFixture.Constructions(file).Single(x => x.Guid == result.Saved.Guid));
            Assert.Equal("Source Library.tcd", saved.SavedFromSource);
            Assert.DoesNotContain("someone", File.ReadAllText(library.Path));
        }

        [Fact]
        public void A_save_that_fails_leaves_the_library_as_it_was_and_raises_nothing()
        {
            Save("Existing");
            string before = Hash(library.Path);

            library.BeforeWrite = path => throw new IOException("disk full");
            UserConstructionSaveResult result = library.Save(UserConstructionFixture.Wall("Source", 0.2), UserConstructionFixture.Materials(), "New", UserConstructionFixture.Provenance());
            library.BeforeWrite = null;

            Assert.False(result.Succeeded);
            Assert.Contains("disk full", result.Error);
            Assert.Equal(before, Hash(library.Path));
            Assert.Equal(0, changed);
            Assert.Single(UserConstructionFixture.Constructions(Main()));
            Assert.Empty(Directory.GetFiles(directory, "*.tmp"));
        }

        [Fact]
        public void An_unreadable_library_is_never_overwritten()
        {
            File.WriteAllText(library.Path, "this is not a library");
            string before = Hash(library.Path);

            Assert.Equal(UserConstructionLibraryState.Unreadable, library.Read().State);
            UserConstructionSaveResult result = library.Save(UserConstructionFixture.Wall("Source"), UserConstructionFixture.Materials(), "New", UserConstructionFixture.Provenance());

            Assert.False(result.Succeeded);
            Assert.Contains("is not a readable construction library", result.Error);
            Assert.Equal(before, Hash(library.Path));
            Assert.Equal(0, changed);
        }

        // ---- The panel's services ----------------------------------------------------------------------------------------

        [Fact]
        public void The_services_create_the_library_on_first_use_only_once_and_the_process_default_never_reads_the_users_file_in_tests()
        {
            int created = 0;
            ThermalEditServices services = new ThermalEditServices(userConstructions: () => { created++; return library; });

            Assert.Equal(0, created);                       // a panel that is only looked at creates nothing
            Assert.Same(library, services.UserConstructions);
            Assert.Same(library, services.UserConstructions);
            Assert.Equal(1, created);

            // Without an injected library the services use the process's shared one, which the tests point at a temporary file.
            Assert.Same(UserConstructionLibrary.Shared, new ThermalEditServices().UserConstructions);
            Assert.NotEqual(Path.GetFullPath(UserConstructionLibrary.DefaultPath), Path.GetFullPath(UserConstructionLibrary.Shared.Path));
        }

        // ---- Own file --------------------------------------------------------------------------------------------------

        [Fact]
        public void The_library_has_its_own_file_next_to_the_glazing_one_and_keeps_aperture_constructions_it_finds_as_they_are()
        {
            Assert.EndsWith(Path.Combine("User Libraries", "Constructions.json"), UserConstructionLibrary.DefaultPath);
            Assert.NotEqual(UserGlazingLibrary.DefaultPath, UserConstructionLibrary.DefaultPath);
            Assert.Equal("Constructions.removed.json", Path.GetFileName(library.ArchivePath));
            Assert.Equal("My constructions", UserConstructionLibrary.LibraryName);

            // A file that also holds a glazing system (hand-made): saving and removing never drops it.
            ApertureConstruction system = BuilderFixture.Seed();
            MaterialLibrary glazingMaterials = new MaterialLibrary("User");
            foreach (IMaterial material in BuilderFixture.SeedMaterials().GetMaterials())
            {
                glazingMaterials.Add(material);
            }

            File.WriteAllText(library.Path, new ConstructionManager(new[] { system }, null, glazingMaterials) { Name = UserConstructionLibrary.LibraryName }.ToJsonObject().ToJsonString());

            Construction saved = Save("My wall");
            Assert.Single(Main().ApertureConstructions, x => x.Guid == system.Guid);
            Assert.True(library.Remove(saved.Guid).Succeeded);
            Assert.Single(Main().ApertureConstructions, x => x.Guid == system.Guid);
            Assert.Contains(BuilderFixture.Clear, UserConstructionFixture.MaterialNames(Main()));
        }

        // ---- Rename ----------------------------------------------------------------------------------------------------

        [Fact]
        public void Rename_changes_the_name_only_the_guid_layers_materials_and_provenance_are_exactly_as_they_were()
        {
            Construction saved = Save("Old name");
            Construction other = Save("Other", UserConstructionFixture.Wall("Source", 0.2));
            ConstructionManager before = Main();
            Construction json_Before = UserConstructionFixture.Constructions(before).Single(x => x.Guid == saved.Guid);

            UserConstructionEditResult result = library.Rename(saved.Guid, "  New name ");

            Assert.True(result.Succeeded, result.Error);
            Assert.True(result.Modified);
            Assert.Equal("New name", result.Entry.Name);
            Assert.Equal(saved.Guid, result.Entry.Guid);
            Assert.Equal(1, changed);

            ConstructionManager after = Main();
            Construction renamed = UserConstructionFixture.Constructions(after).Single(x => x.Guid == saved.Guid);
            string json = json_Before.ToJsonObject().ToJsonString().Replace("\"Old name\"", "\"New name\"");
            Assert.Equal(json, renamed.ToJsonObject().ToJsonString());
            Assert.Single(renamed.GetParameterSets(), x => x.Name == UserConstructionProvenance.ParameterSetName);
            Assert.Equal(UserConstructionFixture.Constructions(before).Single(x => x.Guid == other.Guid).ToJsonObject().ToJsonString(), UserConstructionFixture.Constructions(after).Single(x => x.Guid == other.Guid).ToJsonObject().ToJsonString());
            Assert.Equal(UserConstructionFixture.MaterialNames(before), UserConstructionFixture.MaterialNames(after));
            Assert.False(File.Exists(library.ArchivePath));
        }

        [Fact]
        public void Rename_refuses_an_empty_a_taken_and_an_unknown_name_or_guid_and_writes_nothing_and_the_same_name_or_only_the_case_is_fine()
        {
            Construction a = Save("Alpha");
            Construction b = Save("Beta", UserConstructionFixture.Wall("Source", 0.2));
            string before = Hash(library.Path);

            Assert.False(library.Rename(a.Guid, "  ").Succeeded);
            UserConstructionEditResult taken = library.Rename(a.Guid, " beta ");
            Assert.False(taken.Succeeded);
            Assert.Contains("already in My constructions", taken.Error);
            Assert.False(library.Rename(Guid.NewGuid(), "Gamma").Succeeded);
            Assert.Equal(before, Hash(library.Path));
            Assert.Equal(0, changed);

            // The same name: succeeds, nothing is written, nothing is raised.
            UserConstructionEditResult same = library.Rename(a.Guid, "Alpha");
            Assert.True(same.Succeeded);
            Assert.False(same.Modified);
            Assert.Equal(before, Hash(library.Path));
            Assert.Equal(0, changed);

            // Only the case: allowed (the construction is excluded from its own name check).
            Assert.True(library.Rename(a.Guid, "ALPHA").Succeeded);
            Assert.Equal(1, changed);
            Assert.Equal("ALPHA", UserConstructionFixture.Constructions(Main()).Single(x => x.Guid == a.Guid).Name);
            Assert.Equal("Beta", UserConstructionFixture.Constructions(Main()).Single(x => x.Guid == b.Guid).Name);
        }

        // ---- Remove ----------------------------------------------------------------------------------------------------

        [Fact]
        public void Remove_moves_the_construction_with_its_materials_to_the_archive_and_never_deletes_it()
        {
            Construction doomed = Save("Doomed");
            MaterialLibrary other = UserConstructionFixture.Materials();
            other.Remove(other.GetMaterial(UValueFixture.Wool));
            other.Add(UserConstructionFixture.OtherWool());
            Construction stays = library.Save(UserConstructionFixture.Wall("Source"), other, "Stays", UserConstructionFixture.Provenance(UserConstructionOrigin.AddedSource)).Saved;
            changed = 0;

            UserConstructionEditResult result = library.Remove(doomed.Guid);

            Assert.True(result.Succeeded, result.Error);
            Assert.Equal(1, changed);
            Assert.Equal("Doomed", result.Entry.Name);

            ConstructionManager main = Main();
            Assert.DoesNotContain(UserConstructionFixture.Constructions(main), x => x.Guid == doomed.Guid);
            Assert.Single(UserConstructionFixture.Constructions(main), x => x.Guid == stays.Guid);

            // The archive has the construction as it was and every material it names; the wool only it used left the library with it.
            ConstructionManager archive = Archive();
            Construction archived = Assert.Single(UserConstructionFixture.Constructions(archive), x => x.Guid == doomed.Guid);
            Assert.Equal(doomed.ToJsonObject().ToJsonString(), archived.ToJsonObject().ToJsonString());
            foreach (string name in LayerNames(doomed))
            {
                Assert.NotNull(archive.MaterialLibrary.GetMaterial(name));
            }

            Assert.Contains(UValueFixture.Wool, result.PrunedMaterials);
            Assert.DoesNotContain(UValueFixture.Wool, UserConstructionFixture.MaterialNames(main));
            Assert.Contains(UValueFixture.Wool + " 2", UserConstructionFixture.MaterialNames(main));
            // What the remaining construction still uses stayed.
            foreach (string name in LayerNames(UserConstructionFixture.Constructions(main).Single(x => x.Guid == stays.Guid)))
            {
                Assert.NotNull(main.MaterialLibrary.GetMaterial(name));
            }
        }

        [Fact]
        public void Remove_of_an_unknown_guid_is_an_error_and_a_retry_after_a_failed_library_write_is_idempotent()
        {
            Construction doomed = Save("Doomed");
            Save("Stays", UserConstructionFixture.Wall("Source", 0.2));

            Assert.False(library.Remove(Guid.NewGuid()).Succeeded);
            Assert.Equal(0, changed);

            // The library write fails after the archive was written: the construction is still in the library (and also in the archive).
            library.BeforeWrite = path => { if (path == library.Path) throw new IOException("disk full"); };
            UserConstructionEditResult failed = library.Remove(doomed.Guid);
            library.BeforeWrite = null;

            Assert.False(failed.Succeeded);
            Assert.Equal(0, changed);
            Assert.Single(UserConstructionFixture.Constructions(Main()), x => x.Guid == doomed.Guid);
            Assert.Single(UserConstructionFixture.Constructions(Archive()), x => x.Guid == doomed.Guid);

            // The retry succeeds and the archive still holds it once.
            Assert.True(library.Remove(doomed.Guid).Succeeded);
            Assert.Equal(1, changed);
            Assert.DoesNotContain(UserConstructionFixture.Constructions(Main()), x => x.Guid == doomed.Guid);
            Assert.Single(UserConstructionFixture.Constructions(Archive()), x => x.Guid == doomed.Guid);
        }

        [Fact]
        public void A_failing_archive_leaves_the_library_exactly_as_it_was()
        {
            Construction doomed = Save("Doomed");
            string before = Hash(library.Path);

            library.BeforeWrite = path => { if (path == library.ArchivePath) throw new IOException("archive locked"); };
            UserConstructionEditResult failed = library.Remove(doomed.Guid);
            library.BeforeWrite = null;

            Assert.False(failed.Succeeded);
            Assert.Contains("Nothing was removed", failed.Error);
            Assert.Equal(before, Hash(library.Path));
            Assert.Equal(0, changed);

            // An unreadable archive is never overwritten either.
            File.WriteAllText(library.ArchivePath, "not an archive");
            string archive = Hash(library.ArchivePath);
            Assert.False(library.Remove(doomed.Guid).Succeeded);
            Assert.Equal(archive, Hash(library.ArchivePath));
            Assert.Equal(before, Hash(library.Path));
        }

        // ---- Provenance ------------------------------------------------------------------------------------------------

        [Fact]
        public void The_provenance_has_a_fixed_identity_schema_1_and_round_trips_every_field_without_a_machine_path()
        {
            Assert.Equal("SAM User Construction", UserConstructionProvenance.ParameterSetName);
            Assert.Equal(new Guid("9c2b6d3e-4f1a-4a7e-b3d8-2e5f7a1c9b60"), UserConstructionProvenance.ParameterSetGuid);
            Assert.Equal(1, UserConstructionProvenance.CurrentSchemaVersion);

            Guid basedOn = Guid.NewGuid();
            UserConstructionProvenance written = new UserConstructionProvenance()
            {
                SavedFrom = UserConstructionOrigin.AddedSource,
                SavedFromSource = @"D:\Libraries\NCM\Constructions.tcd",
                BasedOnName = "NCM wall",
                BasedOnGuid = basedOn,
                OriginModelName = @"C:\Projects\Tower\Tower.sam",
                ThermalTransmittance = 0.171,
                TargetThermalTransmittance = 0.18,
                HeatFlowDirection = "Horizontal",
                HeatFlowBasis = "Horizontal heat flow, external surfaces (WallExternal, from the panels)",
                Engine = UserConstructionProvenance.TasEngine,
                Route = "U-value of the construction as it is (Tas thermal transmittance)",
                SamTasVersion = "1.2.3.4",
                CreatedUtc = new DateTime(2026, 10, 4, 8, 30, 0, DateTimeKind.Utc),
            };

            Construction construction = new Construction(UserConstructionFixture.Wall("X"));
            construction.Add(written.ToParameterSet());
            UserConstructionProvenance read = UserConstructionProvenance.FromConstruction(new Construction(construction.ToJsonObject()));

            Assert.Equal(UserConstructionOrigin.AddedSource, read.SavedFrom);
            Assert.Equal("Constructions.tcd", read.SavedFromSource);
            Assert.Equal("NCM wall", read.BasedOnName);
            Assert.Equal(basedOn, read.BasedOnGuid);
            Assert.Equal("Tower.sam", read.OriginModelName);
            Assert.Equal(0.171, read.ThermalTransmittance, 6);
            Assert.Equal(0.18, read.TargetThermalTransmittance, 6);
            Assert.Equal("Horizontal", read.HeatFlowDirection);
            Assert.Equal(written.HeatFlowBasis, read.HeatFlowBasis);
            Assert.Equal(written.Engine, read.Engine);
            Assert.Equal(written.Route, read.Route);
            Assert.Equal("1.2.3.4", read.SamTasVersion);
            Assert.Equal(written.CreatedUtc, read.CreatedUtc);
            Assert.Equal(1, read.SchemaVersion);

            string json = construction.ToJsonObject().ToJsonString();
            Assert.DoesNotContain(@"D:\\Libraries", json);
            Assert.DoesNotContain(@"C:\\Projects", json);

            // A value that was not known is not written, and reads back as "not recorded".
            UserConstructionProvenance bare = UserConstructionProvenance.FromParameterSet(new UserConstructionProvenance().ToParameterSet());
            Assert.True(double.IsNaN(bare.ThermalTransmittance));
            Assert.True(double.IsNaN(bare.TargetThermalTransmittance));
            Assert.Null(bare.BasedOnGuid);
            Assert.Null(UserConstructionProvenance.FromConstruction(UserConstructionFixture.Wall("No provenance")));
        }

        [Fact]
        public void Saving_a_construction_that_already_has_provenance_leaves_exactly_one_set_the_new_one()
        {
            Construction first = Save("First", provenance: UserConstructionFixture.Provenance(UserConstructionOrigin.GeneratedVariant));
            UserConstructionProvenance firstProvenance = UserConstructionProvenance.FromConstruction(first);
            Assert.Equal(UserConstructionOrigin.GeneratedVariant, firstProvenance.SavedFrom);

            // Saved again from My constructions (the source carries the first set): the new set replaces it, never joins it.
            UserConstructionProvenance again = UserConstructionFixture.Provenance(UserConstructionOrigin.MyConstructions, "First", first.Guid);
            Construction second = Save("Second", first, UserConstructionFixture.Materials(), again);

            Assert.Single(second.GetParameterSets(), x => x.Name == UserConstructionProvenance.ParameterSetName);
            UserConstructionProvenance read = UserConstructionProvenance.FromConstruction(second);
            Assert.Equal(UserConstructionOrigin.MyConstructions, read.SavedFrom);
            Assert.Equal(first.Guid, read.BasedOnGuid);
            Assert.NotEqual(first.Guid, second.Guid);
        }

        [Fact]
        public void Saving_a_construction_that_already_has_provenance_carries_no_value_over_from_the_old_set()
        {
            // The first save knew a target, an added source, a route and an engine; the second (the same construction saved again, e.g. as it sits in a
            // model) knows none of them. SAM merges same-named sets with later values winning, so a key the new set does not write would survive.
            UserConstructionProvenance rich = UserConstructionFixture.Provenance(UserConstructionOrigin.AddedSource);
            rich.SavedFromSource = "Constructions.tcd";
            rich.TargetThermalTransmittance = 0.15;
            rich.SamTasVersion = "1.2.3";
            Construction first = Save("First", provenance: rich);
            Assert.Equal(0.15, UserConstructionProvenance.FromConstruction(first).TargetThermalTransmittance);

            Construction second = Save("Second", first, UserConstructionFixture.Materials(), new UserConstructionProvenance() { SavedFrom = UserConstructionOrigin.Model });

            UserConstructionProvenance read = UserConstructionProvenance.FromConstruction(second);
            Assert.Equal(UserConstructionOrigin.Model, read.SavedFrom);
            Assert.True(double.IsNaN(read.TargetThermalTransmittance));
            Assert.True(double.IsNaN(read.ThermalTransmittance));
            Assert.Null(read.SavedFromSource);
            Assert.Null(read.BasedOnName);
            Assert.Null(read.BasedOnGuid);
            Assert.Null(read.OriginModelName);
            Assert.Null(read.Route);
            Assert.Null(read.Engine);
            Assert.Null(read.SamTasVersion);
            Assert.Null(read.HeatFlowBasis);

            // The construction it was saved from keeps its own set.
            Assert.Equal(0.15, UserConstructionProvenance.FromConstruction(first).TargetThermalTransmittance);

            // And the same once it has been through the file.
            UserConstructionProvenance stored = UserConstructionProvenance.FromConstruction(Main().Constructions.Find(x => x.Guid == second.Guid));
            Assert.True(double.IsNaN(stored.TargetThermalTransmittance));
            Assert.Null(stored.Route);
        }
    }
}
