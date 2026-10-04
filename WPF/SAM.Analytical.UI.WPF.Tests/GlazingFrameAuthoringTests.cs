// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.UI.WPF.Tests.Helpers;
using SAM.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Threading;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// User-library PR5: richer frame authoring in the Glazing System Builder. A frame is no frame, an OWN frame (layers added from the solid materials of
    /// the sources) or the frame of an existing system (copied) - and the layers of either can be edited: material per layer, thickness, order, add and
    /// remove, with the face width as before. The frame's additional heat transfer is shown READ-ONLY: it is the value carried with a copied frame and an
    /// own frame has none; nothing makes a frame from a declared Uf. Everything is draft-only until Save; Save as new / Save and replace keep their
    /// meaning (new Guid; Supersedes + archive), and a saved system reopens with the frame it was saved with.
    /// </summary>
    [Collection(WpfCollection.Name)]
    public sealed class GlazingFrameAuthoringTests : IDisposable
    {
        private const string Aluminium = "Aluminium profile";
        private const string Break = "Polyamide break";

        private readonly string directory = BuilderFixture.TempDirectory();
        private readonly UserGlazingLibrary library;
        private int changed;

        public GlazingFrameAuthoringTests()
        {
            library = BuilderFixture.Library(directory);
            library.Changed += (sender, e) => changed++;
        }

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

        // ---- Fixtures -----------------------------------------------------------------------------------------------------

        private static OpaqueMaterial Solid(string name, double conductivity, double? thickness)
        {
            OpaqueMaterial result = new OpaqueMaterial(Guid.NewGuid(), name, name, name, conductivity, 1000, 1000);
            if (thickness != null)
            {
                result.SetValue(Core.MaterialParameter.DefaultThickness, thickness.Value);
            }

            return result;
        }

        // The materials a frame can be built from, beside the model's timber: aluminium (default 40 mm), a polyamide break (no default thickness) and a gas.
        private static GlazingSource FrameSource()
        {
            MaterialLibrary materials = new MaterialLibrary("Frames");
            materials.Add(Solid(Aluminium, 160, 0.04));
            materials.Add(Solid(Break, 0.25, null));
            materials.Add(BuilderFixture.Gas(DefaultGasType.Argon));
            return new GlazingSource(GlazingSourceKind.Loaded, "Frames-test.json", BuilderUiFixture.Manager(materials));
        }

        private GlazingBuilderOptions Options(FakeDraftTas tas, ApertureConstruction seed = null, bool withSeed = true)
        {
            return BuilderUiFixture.Options(library, tas, null, seed, withSeed, BuilderUiFixture.PaneSource(kind: GlazingSourceKind.Model), FrameSource());
        }

        private GlazingBuilderViewModel Builder(FakeDraftTas tas = null, bool withSeed = true)
        {
            GlazingBuilderViewModel result = new GlazingBuilderViewModel(Options(tas ?? new FakeDraftTas(), null, withSeed));
            BuilderUiFixture.Settle(result);
            return result;
        }

        private static GlazingFrameMaterialChoice Material(GlazingBuilderViewModel builder, string name) => builder.FrameMaterials.Single(x => x.Name == name);

        private static void Flush()
        {
            Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Background);
        }

        private static async Task Pump(Func<bool> condition, string what)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            while (!condition())
            {
                Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(20), "Timed out waiting for " + what);
                await Task.Delay(10);
                Flush();
            }
        }

        private static async Task Settle(GlazingBuilderViewModel builder)
        {
            Task evaluation = builder.LastEvaluationTask;
            Task panes = builder.Panes.LastWork;
            await Pump(() => evaluation.IsCompleted && panes.IsCompleted, "the Builder to settle");
            await evaluation;
            await panes;
        }

        private async Task<GlazingBuilderViewModel> OpenSaved(ApertureConstruction saved, bool edit = true)
        {
            GlazingSource user = GlazingSource.FromUserLibrary(library);
            ApertureConstruction seed = user.GetApertureConstructions(ApertureType.Window).Single(x => x.Guid == saved.Guid);
            GlazingBuilderOptions options = Options(new FakeDraftTas(), seed);
            options.SeedSource = user;
            options.EditSeed = edit;
            GlazingBuilderViewModel result = new GlazingBuilderViewModel(options);
            await Settle(result);
            return result;
        }

        private static IEnumerable<(string, double)> Layers(IEnumerable<ConstructionLayer> layers) => (layers ?? new List<ConstructionLayer>()).Select(x => (x.Name, x.Thickness));

        private static string Json(ApertureConstruction system) => system.ToJsonObject().ToJsonString();

        // ---- The frame as a draft object ---------------------------------------------------------------------------------

        [Fact]
        public void Adding_a_layer_to_no_frame_starts_an_own_frame_that_has_no_additional_heat_transfer_and_removing_the_last_layer_leaves_no_frame()
        {
            DraftFrame frame = DraftFrame.None();
            Assert.True(frame.IsNone);
            Assert.False(frame.IsAuthored);

            frame.AddLayer(Solid(Aluminium, 160, 0.04), 0.04, "Frames-test.json");
            frame.AddLayer(Solid(Break, 0.25, null), 0.02, "Frames-test.json");

            Assert.False(frame.IsNone);
            Assert.True(frame.IsAuthored);
            Assert.False(frame.IsEdited);
            Assert.True(double.IsNaN(frame.AdditionalHeatTransfer));
            Assert.Null(frame.CopiedFromName);
            Assert.Equal(new[] { Aluminium, Break }, frame.EditableLayers.Select(x => x.Name));
            Assert.Equal(0.06, frame.Depth, 9);

            Assert.True(frame.MoveLayer(1, 0));
            Assert.Equal(new[] { Break, Aluminium }, frame.EditableLayers.Select(x => x.Name));
            Assert.False(frame.MoveLayer(0, 5));

            Assert.True(frame.RemoveLayerAt(0));
            Assert.True(frame.RemoveLayerAt(0));
            Assert.True(frame.IsNone);
            Assert.False(frame.IsAuthored);
            Assert.False(frame.RemoveLayerAt(0));
        }

        [Fact]
        public void A_copied_frame_is_edited_when_its_layers_differ_from_the_copy_and_not_again_once_they_are_put_back_and_keeps_its_additional_heat_transfer()
        {
            DraftFrame frame = DraftFrame.CopyFrom(BuilderFixture.Seed(), BuilderFixture.SeedMaterials(), "Default library");
            Assert.False(frame.IsEdited);
            Assert.False(frame.IsAuthored);
            Assert.Equal(10, frame.AdditionalHeatTransfer, 9);

            frame.EditableLayers[0].Thickness = 0.05;
            Assert.True(frame.IsEdited);
            Assert.Equal(10, frame.AdditionalHeatTransfer, 9);       // read-only: editing the layers does not touch it
            Assert.Equal("SEED_GLZ", frame.CopiedFromName);

            frame.EditableLayers[0].Thickness = 0.07;
            Assert.False(frame.IsEdited);

            frame.ReplaceMaterial(0, Solid(Aluminium, 160, 0.04), "Frames-test.json");
            Assert.True(frame.IsEdited);
            Assert.Equal(0.07, frame.EditableLayers[0].Thickness, 9);   // the thickness stays when a material is replaced
        }

        [Fact]
        public void The_frame_has_no_way_to_set_its_additional_heat_transfer()
        {
            // D2: shown, never typed - no setter on the draft frame, no setter on the Builder's text, no method that takes a declared Uf.
            Assert.Null(typeof(DraftFrame).GetProperty(nameof(DraftFrame.AdditionalHeatTransfer)).SetMethod);
            foreach (PropertyInfo property in typeof(GlazingBuilderViewModel).GetProperties().Where(x => x.Name.Contains("AdditionalHeatTransfer")))
            {
                Assert.Null(property.SetMethod);
            }

            Assert.DoesNotContain(typeof(DraftFrame).GetMethods(BindingFlags.Public | BindingFlags.Instance).Concat(typeof(GlazingBuilderViewModel).GetMethods(BindingFlags.Public | BindingFlags.Instance)), x => x.Name.Contains("DeclaredUf") || x.Name.Contains("EquivalentFrame") || x.Name.StartsWith("SetFrameAdditional") || x.Name.StartsWith("SetAdditionalHeatTransfer"));
        }

        // ---- Compose / provenance ------------------------------------------------------------------------------------------

        [Fact]
        public void An_own_frame_composes_its_layers_in_order_with_their_materials_and_says_so_in_the_provenance()
        {
            GlazingSystemDraft draft = BuilderFixture.Double("Own");
            DraftFrame frame = DraftFrame.None();
            frame.AddLayer(Solid(Aluminium, 160, 0.04), 0.04, "Frames-test.json", "Frames-test.json");
            frame.AddLayer(Solid(Break, 0.25, null), 0.02, "Frames-test.json", "Frames-test.json");
            frame.Width = 0.065;
            draft.Frame = frame;

            GlazingComposition composition = BuilderFixture.Compose(draft);

            Assert.Equal(new[] { (Aluminium, 0.04), (Break, 0.02) }, Layers(composition.ApertureConstruction.FrameConstructionLayers));
            Assert.NotNull(composition.MaterialLibrary.GetMaterial(Aluminium));
            Assert.NotNull(composition.MaterialLibrary.GetMaterial(Break));
            Assert.Equal(0.065, composition.ApertureConstruction.GetValue<double>(ApertureConstructionParameter.DefaultFrameWidth), 9);
            Assert.False(composition.ApertureConstruction.TryGetValue(ApertureConstructionParameter.FrameAdditionalHeatTransfer, out double _));
            Assert.True(composition.IsComplete);

            GlazingBuilderProvenance provenance = composition.Provenance;
            Assert.Equal("Authored", provenance.Frame);
            Assert.Null(provenance.FrameCopiedFromName);
            Assert.Equal(new[] { (1, Aluminium, 0.04, "Frames-test.json"), (2, Break, 0.02, "Frames-test.json") }, provenance.FrameLayers.Select(x => (x.Position, x.Material, x.Thickness, x.SourceFile)));
            Assert.Contains("frame built in the Builder", composition.ApertureConstruction.GetValue<string>(ApertureConstructionParameter.Description));
        }

        [Fact]
        public void A_copied_frame_is_Copied_until_its_layers_are_edited_then_CopiedEdited_and_keeps_the_origin_and_the_additional_heat_transfer()
        {
            GlazingSystemDraft draft = BuilderFixture.Double("Copy");
            Assert.Equal("Copied", BuilderFixture.Compose(draft).Provenance.Frame);

            draft.Frame.EditableLayers[0].Thickness = 0.05;
            GlazingComposition composition = BuilderFixture.Compose(draft);

            Assert.Equal("CopiedEdited", composition.Provenance.Frame);
            Assert.Equal("SEED_GLZ", composition.Provenance.FrameCopiedFromName);
            Assert.Equal(BuilderFixture.SeedGuid, composition.Provenance.FrameCopiedFromGuid);
            Assert.Equal(0.05, composition.ApertureConstruction.FrameConstructionLayers.Single().Thickness, 9);
            Assert.Equal(10, composition.ApertureConstruction.GetValue<double>(ApertureConstructionParameter.FrameAdditionalHeatTransfer), 9);
            Assert.Contains("copied from SEED_GLZ and edited", composition.ApertureConstruction.GetValue<string>(ApertureConstructionParameter.Description));
        }

        [Fact]
        public void The_provenance_schema_is_three_adds_the_frame_layers_and_reads_the_older_schemas_as_they_were()
        {
            Assert.Equal(3, GlazingBuilderProvenance.CurrentSchemaVersion);

            // A schema-2 set (no frame layers, "Copied") reads exactly as before.
            GlazingBuilderProvenance v2 = new GlazingBuilderProvenance() { SchemaVersion = 2, Frame = "Copied", FrameCopiedFromName = "SEED_GLZ", FrameWidth = 0.06 };
            ParameterSet set = v2.ToParameterSet();
            Assert.False(set.Contains("Frame Layers"));
            GlazingBuilderProvenance read = GlazingBuilderProvenance.FromParameterSet(set);
            Assert.Equal(2, read.SchemaVersion);
            Assert.Equal("Copied", read.Frame);
            Assert.Empty(read.FrameLayers);

            // Schema 3: the layers round-trip, with a file name only (never a path).
            GlazingBuilderProvenance v3 = new GlazingBuilderProvenance()
            {
                Frame = "Authored",
                FrameLayers = new List<GlazingBuilderFrameRecord>() { new GlazingBuilderFrameRecord() { Position = 1, Material = Aluminium, OriginalName = Aluminium, SourceLabel = "Frames-test.json", SourceFile = "Frames-test.json", Thickness = 0.04 } },
            };
            GlazingBuilderFrameRecord record = GlazingBuilderProvenance.FromParameterSet(v3.ToParameterSet()).FrameLayers.Single();
            Assert.Equal((1, Aluminium, "Frames-test.json", 0.04), (record.Position, record.Material, record.SourceFile, record.Thickness));
        }

        [Fact]
        public void The_report_and_My_library_say_what_the_frame_is_and_list_the_layers_of_an_own_or_an_edited_frame()
        {
            GlazingSystemDraft draft = BuilderFixture.Double("Report");
            string Frame(GlazingSystemDraft d) => Query.GlazingBuiltFrom(BuilderFixture.Compose(d).Provenance).Single(x => x.StartsWith("Frame:"));

            // A copy keeps the words it always had.
            Assert.Equal("Frame: copied from SEED_GLZ, width 60 mm", Frame(draft));

            draft.Frame.EditableLayers[0].Thickness = 0.05;
            string edited = Frame(draft);
            Assert.StartsWith("Frame: copied from SEED_GLZ and edited, width 60 mm; layers: 1. " + BuilderFixture.FrameMaterial + " [50 mm, from Default library]", edited);

            DraftFrame own = DraftFrame.None();
            own.AddLayer(Solid(Aluminium, 160, 0.04), 0.04, "Frames-test.json", "Frames-test.json");
            own.Width = 0.055;
            draft.Frame = own;
            string built = Frame(draft);
            Assert.Equal("Frame: built in the Builder, width 55 mm; layers: 1. " + Aluminium + " [40 mm, from Frames-test.json]", built);
            Assert.DoesNotContain(directory, built, StringComparison.OrdinalIgnoreCase);

            draft.Frame = DraftFrame.None();
            Assert.Equal("Frame: none", Frame(draft));
        }

        // ---- The check -------------------------------------------------------------------------------------------------------

        [Fact]
        public void Invalid_frame_inputs_are_errors_with_the_layer_they_belong_to_and_a_glass_material_only_a_warning()
        {
            GlazingSystemDraft draft = BuilderFixture.Double("Checks");
            DraftFrame frame = DraftFrame.None();
            frame.AddLayer(Solid(Aluminium, 160, 0.04), 0.04, "Frames-test.json");
            frame.AddLayer(Solid(Break, 0.25, null), double.NaN, "Frames-test.json");
            frame.AddLayer(Solid("Zero", 0.2, null), 0, "Frames-test.json");
            frame.AddLayer(BuilderFixture.Gas(DefaultGasType.Argon), 0.01, "Frames-test.json");
            frame.AddLayer(BuilderFixture.ClearPane(), 0.004, "Frames-test.json");
            frame.Width = 0.05;
            draft.Frame = frame;

            GlazingDraftValidation validation = BuilderFixture.Check(draft);

            Assert.True(validation.HasErrors);
            Assert.Equal(new[] { 2, 3 }, validation.Issues.Where(x => x.Code == GlazingDraftIssueCodes.FrameLayerThickness).Select(x => x.FrameLayerNumber.Value).OrderBy(x => x));
            Assert.Equal(GlazingDraftIssueSeverity.Error, validation.Issues.Single(x => x.Code == GlazingDraftIssueCodes.FrameMaterialNotSolid && x.FrameLayerNumber == 4).Severity);
            Assert.Equal(GlazingDraftIssueSeverity.Warning, validation.Issues.Single(x => x.Code == GlazingDraftIssueCodes.FrameMaterialNotSolid && x.FrameLayerNumber == 5).Severity);
            Assert.Equal(1, validation.Issues.Count(x => x.Code == GlazingDraftIssueCodes.FrameLayerThickness && x.FrameLayerNumber == 3));   // SAM's own "thickness ≤ 0" is not listed twice

            frame.WidthInvalid = true;
            frame.Width = double.NaN;
            Assert.True(BuilderFixture.Check(draft).Has(GlazingDraftIssueCodes.FrameWidthInvalid));
            Assert.False(BuilderFixture.Check(draft).Has(GlazingDraftIssueCodes.FrameWidthMissing));
        }

        // ---- The Builder -------------------------------------------------------------------------------------------------

        [Fact]
        public void The_frame_choices_are_none_own_and_the_frames_to_copy_and_the_materials_are_the_solid_ones_of_every_source_once()
        {
            using (GlazingBuilderViewModel builder = Builder())
            {
                Assert.True(builder.FrameChoices[0].IsNone);
                Assert.True(builder.FrameChoices[1].IsOwn);
                Assert.Contains("SEED_GLZ", builder.FrameChoices[2].Label);

                // Solid materials only (no gas, no glass), once each by definition: the model's timber that three sources share is listed once.
                Assert.Equal(new[] { Aluminium, Break, BuilderFixture.FrameMaterial }, builder.FrameMaterials.Select(x => x.Name).OrderBy(x => x, StringComparer.Ordinal));
                Assert.Equal("3 solid materials", builder.FrameMaterialCountText);

                builder.FrameMaterialSearchText = "poly";
                Assert.Equal(new[] { Break }, builder.FrameMaterials.Select(x => x.Name));
                Assert.Equal("1 of 3 solid materials", builder.FrameMaterialCountText);
                builder.FrameMaterialSearchText = "frames-test";
                Assert.Equal(new[] { Aluminium, Break }, builder.FrameMaterials.Select(x => x.Name).OrderBy(x => x));
            }
        }

        [Fact]
        public void An_own_frame_is_built_layer_by_layer_and_everything_derived_follows_each_edit()
        {
            FakeDraftTas tas = new FakeDraftTas();
            using (GlazingBuilderViewModel builder = Builder(tas))
            {
                builder.SelectedFrame = builder.FrameChoices[1];
                BuilderUiFixture.Settle(builder);
                Assert.False(builder.HasFrame);
                Assert.True(builder.ShowFrameEditor);
                Assert.Equal("no frame", builder.UfText);
                Assert.Equal("no frame", builder.FrameAdditionalHeatTransferText);
                Assert.Contains("Own frame", builder.FrameNote);

                builder.SelectedFrameMaterial = Material(builder, Aluminium);
                Assert.True(builder.CanAddFrameLayer);
                int calls = tas.Calls;
                Assert.True(builder.AddFrameLayer());
                BuilderUiFixture.Settle(builder);

                // The layer comes with the material's default thickness; the frame is an own one with no additional heat transfer.
                Assert.True(builder.HasFrame);
                Assert.True(builder.Draft.Frame.IsAuthored);
                Assert.True(builder.FrameChoices[1].IsOwn && ReferenceEquals(builder.SelectedFrame, builder.FrameChoices[1]));
                GlazingBuilderFrameLayerRow first = Assert.Single(builder.FrameLayers);
                Assert.Equal("40", first.ThicknessText);
                Assert.Same(first, builder.SelectedFrameLayer);
                Assert.Equal("none", builder.FrameAdditionalHeatTransferText);
                Assert.Contains("only its layers carry heat", builder.FrameAdditionalHeatTransferNote);
                Assert.Equal("1.80 W/m²K", builder.UfText);                         // Tas was asked again for the frame
                Assert.True(tas.Calls > calls);
                Assert.Equal(string.Empty, builder.FrameWidthText);                   // no width is invented for an own frame
                Assert.True(builder.Validation.Has(GlazingDraftIssueCodes.FrameWidthMissing));
                Assert.Contains("40 mm", builder.FrameDepthText);

                // A second layer (a material without a default thickness takes 30 mm) goes after the selected one; its thickness is typed.
                builder.SelectedFrameMaterial = Material(builder, Break);
                Assert.True(builder.AddFrameLayer());
                Assert.Equal(new[] { Aluminium, Break }, builder.FrameLayers.Select(x => x.Layer.Name));
                Assert.Equal("30", builder.FrameLayers[1].ThicknessText);
                calls = tas.Calls;
                builder.FrameLayers[1].ThicknessText = "20";
                BuilderUiFixture.Settle(builder);
                Assert.Equal(0.02, builder.Draft.Frame.EditableLayers[1].Thickness, 9);
                Assert.Contains("60 mm", builder.FrameDepthText);
                Assert.True(tas.Calls > calls);

                // Replace the material of the first layer, keep its thickness; move it down; width typed.
                builder.SelectedFrameLayer = builder.FrameLayers[0];
                builder.SelectedFrameMaterial = Material(builder, BuilderFixture.FrameMaterial);
                Assert.True(builder.ReplaceFrameMaterial());
                Assert.Equal(new[] { BuilderFixture.FrameMaterial, Break }, builder.FrameLayers.Select(x => x.Layer.Name));
                Assert.Equal(0.04, builder.Draft.Frame.EditableLayers[0].Thickness, 9);
                Assert.True(builder.MoveFrameLayer(1));
                Assert.Equal(new[] { Break, BuilderFixture.FrameMaterial }, builder.FrameLayers.Select(x => x.Layer.Name));
                Assert.Same(builder.FrameLayers[1], builder.SelectedFrameLayer);
                builder.FrameWidthText = "65";
                Assert.Equal(0.065, builder.Draft.Frame.Width, 9);
                Assert.False(builder.Validation.Has(GlazingDraftIssueCodes.FrameWidthMissing));

                // Remove both: the system has no frame again; the choice is still the own frame (waiting for its first layer).
                Assert.True(builder.RemoveFrameLayer());
                Assert.True(builder.RemoveFrameLayer());
                Assert.False(builder.HasFrame);
                Assert.Equal("no frame", builder.UfText);
                Assert.Equal(string.Empty, builder.FrameWidthText);
                Assert.True(builder.SelectedFrame.IsOwn);
                Assert.True(builder.Validation.Has(GlazingDraftIssueCodes.Frameless));
            }
        }

        [Fact]
        public void Choosing_a_frame_to_copy_shows_its_additional_heat_transfer_read_only_and_editing_its_layers_keeps_it()
        {
            using (GlazingBuilderViewModel builder = Builder())
            {
                // The seed's frame (70 mm timber, 10 % additional heat transfer) is the starting frame.
                Assert.Equal("10 %", builder.FrameAdditionalHeatTransferText);
                Assert.Contains("Read-only", builder.FrameAdditionalHeatTransferNote);
                Assert.Contains("SEED_GLZ", builder.FrameAdditionalHeatTransferNote);

                builder.SelectedFrame = builder.FrameChoices[0];
                Assert.Equal("no frame", builder.FrameAdditionalHeatTransferText);
                builder.SelectedFrame = builder.FrameChoices[1];
                Assert.Equal("no frame", builder.FrameAdditionalHeatTransferText);
                builder.SelectedFrame = builder.FrameChoices[2];
                Assert.Equal("10 %", builder.FrameAdditionalHeatTransferText);

                builder.FrameLayers[0].ThicknessText = "50";
                Assert.Equal("10 %", builder.FrameAdditionalHeatTransferText);
                Assert.True(builder.Draft.Frame.IsEdited);
                Assert.Contains("were edited", builder.FrameNote);
                Assert.Contains("also after the layers were edited", builder.FrameAdditionalHeatTransferNote);
            }
        }

        [Fact]
        public void A_proposed_width_follows_the_layers_until_a_width_is_typed()
        {
            using (GlazingBuilderViewModel builder = Builder())
            {
                // The seed stores no width: its depth (70 mm) is proposed.
                Assert.Equal("70", builder.FrameWidthText);
                builder.FrameLayers[0].ThicknessText = "50";
                Assert.Equal("50", builder.FrameWidthText);
                Assert.Equal(0.05, builder.Draft.Frame.Width, 9);

                builder.FrameWidthText = "80";
                builder.FrameLayers[0].ThicknessText = "60";
                Assert.Equal("80", builder.FrameWidthText);
            }
        }

        [Fact]
        public void An_invalid_width_or_thickness_blocks_Save_with_a_message_and_a_valid_one_releases_it()
        {
            using (GlazingBuilderViewModel builder = Builder())
            {
                Assert.True(builder.CanSave);

                builder.FrameWidthText = "wide";
                Assert.False(builder.CanSave);
                Assert.True(builder.Validation.Has(GlazingDraftIssueCodes.FrameWidthInvalid));
                Assert.Contains(builder.Issues, x => x.Text.Contains("frame width is not a positive number"));

                builder.FrameWidthText = "-5";
                Assert.False(builder.CanSave);

                builder.FrameWidthText = "55";
                Assert.True(builder.CanSave);

                builder.FrameLayers[0].ThicknessText = string.Empty;
                Assert.False(builder.CanSave);
                Assert.Contains("no thickness", builder.FrameLayers[0].IssueText);
                Assert.True(builder.FrameLayers[0].HasIssue);

                builder.FrameLayers[0].ThicknessText = "0";
                Assert.False(builder.CanSave);

                // Choosing the finding selects the layer it belongs to.
                builder.SelectedFrameLayer = null;
                builder.SelectFrameIssue(builder.Issues.First(x => x.Issue.FrameLayerNumber == 1));
                Assert.Same(builder.FrameLayers[0], builder.SelectedFrameLayer);

                builder.FrameLayers[0].ThicknessText = "45";
                Assert.True(builder.CanSave);
            }
        }

        [Fact]
        public void Editing_a_frame_changes_only_the_draft_not_the_seed_the_library_or_any_file()
        {
            ApertureConstruction seed = BuilderUiFixture.Seed();
            string seedJson = Json(seed);
            using (GlazingBuilderViewModel builder = new GlazingBuilderViewModel(Options(new FakeDraftTas(), seed)))
            {
                BuilderUiFixture.Settle(builder);
                builder.FrameLayers[0].ThicknessText = "10";
                builder.SelectedFrameMaterial = Material(builder, Aluminium);
                builder.AddFrameLayer();
                builder.SelectedFrame = builder.FrameChoices[1];
                builder.AddFrameLayer();
                builder.FrameWidthText = "33";
            }

            Assert.Equal(seedJson, Json(seed));
            Assert.False(File.Exists(library.Path));
            Assert.False(File.Exists(library.ArchivePath));
            Assert.Equal(0, changed);
        }

        // ---- Save as new / Save and replace / reopen ----------------------------------------------------------------------------

        [WpfFact]
        public async Task A_saved_own_frame_reopens_with_its_layers_materials_thicknesses_width_and_origin_and_Save_as_new_keeps_it_with_a_new_guid()
        {
            GlazingBuilderViewModel builder = new GlazingBuilderViewModel(Options(new FakeDraftTas()));
            await Settle(builder);
            ApertureConstruction first;
            using (builder)
            {
                builder.SelectedFrame = builder.FrameChoices[1];
                builder.SelectedFrameMaterial = Material(builder, Aluminium);
                builder.AddFrameLayer();
                builder.SelectedFrameMaterial = Material(builder, Break);
                builder.AddFrameLayer();
                builder.FrameLayers[1].ThicknessText = "22";
                builder.FrameWidthText = "62";
                builder.Name = "Own frame system";
                await Settle(builder);

                Assert.True(await builder.SaveAsync(), builder.SaveError);
                first = library.Read().Systems.Single(x => x.Guid == builder.SavedSystem.Guid);
            }

            Assert.Equal(new[] { (Aluminium, 0.04), (Break, 0.022) }, Layers(first.FrameConstructionLayers));
            Assert.Equal(0.062, first.GetValue<double>(ApertureConstructionParameter.DefaultFrameWidth), 9);
            Assert.False(first.TryGetValue(ApertureConstructionParameter.FrameAdditionalHeatTransfer, out double _));
            Assert.NotNull(library.Read().ConstructionManager.MaterialLibrary.GetMaterial(Aluminium));
            Assert.NotNull(library.Read().ConstructionManager.MaterialLibrary.GetMaterial(Break));
            GlazingBuilderProvenance provenance = GlazingBuilderProvenance.FromApertureConstruction(first);
            Assert.Equal(3, provenance.SchemaVersion);
            Assert.Equal("Authored", provenance.Frame);
            Assert.Equal(new[] { Aluminium, Break }, provenance.FrameLayers.OrderBy(x => x.Position).Select(x => x.Material));
            Assert.All(provenance.FrameLayers, x => Assert.Equal("Frames-test.json", x.SourceFile));
            Assert.DoesNotContain(directory, first.GetParameterSet(GlazingBuilderProvenance.ParameterSetName).ToJsonObject().ToJsonString(), StringComparison.OrdinalIgnoreCase);

            // Reopened: the same frame, an own one, nothing invented (the saved width stays; the layers' sources are the recorded ones).
            using (GlazingBuilderViewModel reopened = await OpenSaved(first))
            {
                Assert.True(reopened.Draft.Frame.IsAuthored);
                Assert.True(reopened.SelectedFrame.IsOwn);
                Assert.Equal(new[] { (Aluminium, 0.04), (Break, 0.022) }, reopened.Draft.Frame.Layers.Select(x => (x.Name, x.Thickness)));
                Assert.Equal("62", reopened.FrameWidthText);
                Assert.Equal("none", reopened.FrameAdditionalHeatTransferText);
                Assert.Equal(new[] { "40", "22" }, reopened.FrameLayers.Select(x => x.ThicknessText));
                Assert.All(reopened.Draft.Frame.EditableLayers, x => Assert.Equal("Frames-test.json", x.SourceFileName));

                // Saved again unchanged under another name: the same frame (layers, width, materials as the same definitions), a NEW Guid.
                reopened.Name = "Own frame system, again";
                await Settle(reopened);
                Assert.True(await reopened.SaveAsync(), reopened.SaveError);
                ApertureConstruction again = library.Read().Systems.Single(x => x.Guid == reopened.SavedSystem.Guid);
                Assert.NotEqual(first.Guid, again.Guid);
                Assert.Equal(Layers(first.FrameConstructionLayers), Layers(again.FrameConstructionLayers));
                Assert.Equal(0.062, again.GetValue<double>(ApertureConstructionParameter.DefaultFrameWidth), 9);
                Assert.Empty(reopened.SaveResult.AddedMaterials);
                Assert.Empty(reopened.SaveResult.RenamedMaterials);
                Assert.Equal("Authored", GlazingBuilderProvenance.FromApertureConstruction(again).Frame);
            }

            // The first system is exactly as it was saved.
            Assert.Equal(Json(first), Json(library.Read().Systems.Single(x => x.Guid == first.Guid)));
        }

        [WpfFact]
        public async Task Save_and_replace_of_an_edited_copied_frame_saves_the_new_frame_supersedes_the_old_and_archives_it_with_the_old_frame()
        {
            ApertureConstruction saved;
            using (GlazingBuilderViewModel builder = new GlazingBuilderViewModel(Options(new FakeDraftTas())))
            {
                await Settle(builder);
                builder.Name = "Copied frame";
                await Settle(builder);
                Assert.True(await builder.SaveAsync(), builder.SaveError);
                saved = library.Read().Systems.Single(x => x.Guid == builder.SavedSystem.Guid);
            }

            Assert.Equal("Copied", GlazingBuilderProvenance.FromApertureConstruction(saved).Frame);
            string oldJson = Json(saved);
            changed = 0;

            using (GlazingBuilderViewModel builder = await OpenSaved(saved))
            {
                Assert.True(builder.IsEditing);
                Assert.False(builder.Draft.Frame.IsEdited);
                Assert.Equal("10 %", builder.FrameAdditionalHeatTransferText);

                builder.SelectedFrameMaterial = Material(builder, Aluminium);
                builder.SelectedFrameLayer = builder.FrameLayers[0];
                Assert.True(builder.ReplaceFrameMaterial());
                builder.FrameLayers[0].ThicknessText = "45";
                builder.SelectedFrameMaterial = Material(builder, Break);
                builder.AddFrameLayer();
                await Settle(builder);
                Assert.True(builder.Draft.Frame.IsEdited);

                // The saved definition behind the Guid is untouched until Save.
                Assert.Equal(oldJson, Json(library.Read().Systems.Single(x => x.Guid == saved.Guid)));
                Assert.Equal(0, changed);

                Assert.True(await builder.SaveAndReplaceAsync(), builder.SaveError);
                ApertureConstruction replacement = builder.SavedSystem;

                Assert.Equal("Copied frame", replacement.Name);
                Assert.NotEqual(saved.Guid, replacement.Guid);
                Assert.Equal(1, changed);
                Assert.Equal(new[] { (Aluminium, 0.045), (Break, 0.03) }, Layers(replacement.FrameConstructionLayers));
                Assert.Equal(10, replacement.GetValue<double>(ApertureConstructionParameter.FrameAdditionalHeatTransfer), 9);
                GlazingBuilderProvenance provenance = GlazingBuilderProvenance.FromApertureConstruction(replacement);
                Assert.Equal("CopiedEdited", provenance.Frame);
                Assert.Equal("SEED_GLZ", provenance.FrameCopiedFromName);
                Assert.Equal(saved.Guid, provenance.SupersedesGuid);

                // The library holds the new system, the archive the old one with ITS frame.
                Assert.DoesNotContain(library.Read().Systems, x => x.Guid == saved.Guid);
                ConstructionManager archive = UserLibraryFile.Parse(File.ReadAllText(library.ArchivePath), out _);
                ApertureConstruction archived = archive.ApertureConstructions.Single(x => x.Guid == saved.Guid);
                Assert.Equal(new[] { (BuilderFixture.FrameMaterial, 0.07) }, Layers(archived.FrameConstructionLayers));
                Assert.NotNull(archive.MaterialLibrary.GetMaterial(BuilderFixture.FrameMaterial));
            }

            // And the replacement reopens as an EDITED copy of the same frame origin, with the edited layers.
            ApertureConstruction current = library.Read().Systems.Single();
            using (GlazingBuilderViewModel reopened = await OpenSaved(current))
            {
                Assert.True(reopened.Draft.Frame.IsEdited);
                Assert.False(reopened.Draft.Frame.IsAuthored);
                Assert.Equal("SEED_GLZ", reopened.Draft.Frame.CopiedFromName);
                Assert.Equal(new[] { (Aluminium, 0.045), (Break, 0.03) }, reopened.Draft.Frame.Layers.Select(x => (x.Name, x.Thickness)));
                Assert.Equal("10 %", reopened.FrameAdditionalHeatTransferText);
            }
        }

        [WpfFact]
        public async Task A_system_saved_before_the_frame_could_be_edited_reopens_and_saves_as_it_always_did()
        {
            // Schema 2 as PR3 wrote it: a copied frame, no frame layer records.
            GlazingSystemDraft draft = BuilderFixture.Triple("Old");
            UserGlazingSaveResult result = library.Save(draft, new GlazingValues(1.05, 0.52, 0.75, 1.8), new DateTime(2026, 10, 2, 12, 0, 0, DateTimeKind.Utc));
            Assert.True(result.Succeeded, result.Error);
            GlazingBuilderProvenance written = GlazingBuilderProvenance.FromApertureConstruction(result.Saved);
            written.SchemaVersion = 2;
            written.FrameLayers = new List<GlazingBuilderFrameRecord>();
            ApertureConstruction old = new ApertureConstruction(result.Saved);
            old.Add(written.ToParameterSet());
            ParameterSet set = old.GetParameterSet(GlazingBuilderProvenance.ParameterSetName);
            set.Remove("Frame Layers");

            UserGlazingLibrary legacy = BuilderFixture.Library(Path.Combine(directory, "legacy"));
            Directory.CreateDirectory(Path.Combine(directory, "legacy"));
            File.WriteAllText(legacy.Path, new ConstructionManager(new List<ApertureConstruction>() { old }, null, library.Read().ConstructionManager.MaterialLibrary).ToJsonObject().ToJsonString());

            GlazingSource user = GlazingSource.FromUserLibrary(legacy);
            ApertureConstruction seed = user.GetApertureConstructions(ApertureType.Window).Single();
            GlazingBuilderOptions options = BuilderUiFixture.Options(legacy, new FakeDraftTas(), null, seed, true, BuilderUiFixture.PaneSource(kind: GlazingSourceKind.Model), FrameSource());
            options.SeedSource = user;
            options.EditSeed = true;
            using (GlazingBuilderViewModel builder = new GlazingBuilderViewModel(options))
            {
                await Settle(builder);
                Assert.False(builder.Draft.Frame.IsEdited);
                Assert.False(builder.Draft.Frame.IsAuthored);
                Assert.Equal("SEED_GLZ", builder.Draft.Frame.CopiedFromName);
                Assert.Equal(new[] { (BuilderFixture.FrameMaterial, 0.07) }, builder.Draft.Frame.Layers.Select(x => (x.Name, x.Thickness)));

                Assert.True(await builder.SaveAndReplaceAsync(), builder.SaveError);
                GlazingBuilderProvenance again = GlazingBuilderProvenance.FromApertureConstruction(builder.SavedSystem);
                Assert.Equal(3, again.SchemaVersion);
                Assert.Equal("Copied", again.Frame);
                Assert.Single(again.FrameLayers);
            }
        }

        [Fact]
        public void The_new_frame_types_reach_no_analytical_model()
        {
            // The Builder's surface scan (BuilderSurface) is the guard for the model invariant; the frame types are part of it.
            string[] names = BuilderSurface.Types().Select(x => x.Name).ToArray();
            Assert.Contains(nameof(DraftFrameLayer), names);
            Assert.Contains(nameof(GlazingBuilderFrameLayerRow), names);
            Assert.Contains(nameof(GlazingBuilderFrameRecord), names);
            Assert.Contains(nameof(GlazingFrameMaterialChoice), names);
            Assert.Empty(BuilderSurface.ModelReferences());
        }
    }
}
