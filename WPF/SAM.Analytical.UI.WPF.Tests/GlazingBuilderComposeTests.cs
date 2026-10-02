// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.UI.WPF.Tests.Helpers;
using SAM.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// Stage E0-1: the Glazing System Builder draft and its composition into a complete <see cref="ApertureConstruction"/> - the ONE
    /// outside → inside to inside → outside reversal (confirmed against real Tas in Gate 0), gaps as gas layers with derived heat transfer,
    /// reversed panes, frames, the parameters written (and never written), and material identity.
    /// </summary>
    public class GlazingBuilderComposeTests
    {
        private static List<string> PaneNames(GlazingComposition composition) => composition.ApertureConstruction.PaneConstructionLayers.Select(x => x.Name).ToList();

        // -------------------------------------------------------------------------------------------------
        // Order: the Builder lists OUTSIDE -> INSIDE, SAM stores INSIDE -> OUTSIDE (Gate 0)
        // -------------------------------------------------------------------------------------------------

        [Fact]
        public void Double_TheOutsidePaneIsTheLastSamLayer_AndTheInsidePaneTheFirst()
        {
            GlazingSystemDraft draft = new GlazingSystemDraft() { Name = "D" };
            draft.Add(BuilderFixture.Pane(BuilderFixture.TintPane()), BuilderFixture.Gap(), BuilderFixture.Pane(BuilderFixture.ClearPane()));

            GlazingComposition composition = BuilderFixture.Compose(draft);

            List<string> names = PaneNames(composition);
            Assert.Equal(3, names.Count);
            Assert.Equal(BuilderFixture.Clear, names[0]);   // inside
            Assert.StartsWith("Argon_16mm_", names[1]);
            Assert.Equal(BuilderFixture.Tint, names[2]);    // outside
        }

        [Fact]
        public void Triple_AndArbitraryStacks_ComposeEveryLayer_InReverse()
        {
            GlazingComposition triple = BuilderFixture.Compose(BuilderFixture.Triple());
            Assert.Equal(5, triple.ApertureConstruction.PaneConstructionLayers.Count);
            Assert.Equal(BuilderFixture.LowE, PaneNames(triple).First());
            Assert.Equal(BuilderFixture.Clear, PaneNames(triple).Last());

            GlazingSystemDraft draft = new GlazingSystemDraft() { Name = "Six" };
            List<double> widths = new List<double>() { 8, 10, 12, 14, 16 };
            draft.Add(BuilderFixture.Pane(BuilderFixture.TintPane()));
            foreach (double width in widths)
            {
                draft.Add(BuilderFixture.Gap(width), BuilderFixture.Pane(BuilderFixture.ClearPane()));
            }

            GlazingComposition six = BuilderFixture.Compose(draft);
            List<ConstructionLayer> layers = six.ApertureConstruction.PaneConstructionLayers;
            Assert.Equal(11, layers.Count);
            Assert.Equal(BuilderFixture.Tint, layers.Last().Name);
            // The gap next to the outside pane is the first gap of the draft (8 mm).
            Assert.Equal(0.008, layers[layers.Count - 2].Thickness, 6);
            Assert.Equal(0.016, layers[1].Thickness, 6);
            Assert.True(six.IsComplete);
        }

        [Fact]
        public void LayerOrder_IsOneReversal_AndItsOwnInverse()
        {
            List<int> outsideToInside = new List<int>() { 1, 2, 3, 4, 5 };

            List<int> sam = GlazingLayerOrder.ToSam(outsideToInside);

            Assert.Equal(new[] { 5, 4, 3, 2, 1 }, sam);
            Assert.Equal(outsideToInside, GlazingLayerOrder.FromSam(sam));
            Assert.Equal(4, GlazingLayerOrder.SamIndex(0, 5));
            Assert.Equal(0, GlazingLayerOrder.SamIndex(4, 5));
            Assert.Equal(2, GlazingLayerOrder.SamIndex(GlazingLayerOrder.SamIndex(2, 5), 5));
        }

        // -------------------------------------------------------------------------------------------------
        // Gate 0 orientation: External = the face towards the OUTSIDE; Reverse = swap External/Internal (IGDB convention)
        // -------------------------------------------------------------------------------------------------

        [Fact]
        public void Reverse_SwapsTheExternalAndInternalFaces_ExactlyAsIgdbsReversedEntry()
        {
            GlazingSystemDraft draft = new GlazingSystemDraft() { Name = "R" };
            draft.Add(BuilderFixture.Pane(BuilderFixture.LowEPane(), reversed: true), BuilderFixture.Gap(), BuilderFixture.Pane(BuilderFixture.ClearPane()));

            GlazingComposition composition = BuilderFixture.Compose(draft);

            Assert.Equal(BuilderFixture.LowE + " Reversed", PaneNames(composition).Last());
            TransparentMaterial reversed = (TransparentMaterial)composition.MaterialLibrary.GetMaterial(BuilderFixture.LowE + " Reversed");
            TransparentMaterial igdb = BuilderFixture.LowEPaneReversedEntry();
            foreach (TransparentMaterialParameter parameter in new[] { TransparentMaterialParameter.ExternalEmissivity, TransparentMaterialParameter.InternalEmissivity, TransparentMaterialParameter.ExternalSolarReflectance, TransparentMaterialParameter.InternalSolarReflectance, TransparentMaterialParameter.ExternalLightReflectance, TransparentMaterialParameter.InternalLightReflectance, TransparentMaterialParameter.SolarTransmittance, TransparentMaterialParameter.LightTransmittance })
            {
                Assert.Equal(igdb.GetValue<double>(parameter), reversed.GetValue<double>(parameter), 9);
            }

            Assert.Equal(0.84, reversed.GetValue<double>(TransparentMaterialParameter.ExternalEmissivity), 9);
            Assert.Equal(0.025, reversed.GetValue<double>(TransparentMaterialParameter.InternalEmissivity), 9);
            Assert.Equal(igdb.ThermalConductivity, reversed.ThermalConductivity);
            Assert.Equal(Strip(igdb), Strip(reversed));
            // The source pane itself is untouched, and the library holds only the reversed one (plus the gap and the clear pane).
            Assert.Null(composition.MaterialLibrary.GetMaterial(BuilderFixture.LowE));
            Assert.Equal(0.025, ((TransparentMaterial)draft.Panes.First().Material).GetValue<double>(TransparentMaterialParameter.ExternalEmissivity), 9);
            Assert.True(composition.Provenance.Panes.First().Reversed);
        }

        [Fact]
        public void A_LowEPaneAsTheInsidePane_HasItsCoatedExternalFaceTowardsTheCavity()
        {
            // Gate 0: the inside pane's External face is the one facing outwards = the cavity. With the coated face External (as IGDB stores
            // Pilkington low-e panes), "clear | gap | low-e" puts the coating on surface 3 without reversing anything.
            GlazingComposition composition = BuilderFixture.Compose(BuilderFixture.Double());

            ConstructionLayer inside = composition.ApertureConstruction.PaneConstructionLayers.First();
            TransparentMaterial insidePane = (TransparentMaterial)composition.MaterialLibrary.GetMaterial(inside.Name);
            Assert.Equal(BuilderFixture.LowE, inside.Name);
            Assert.Equal(0.025, insidePane.GetValue<double>(TransparentMaterialParameter.ExternalEmissivity), 9);
            Assert.False(composition.Provenance.Panes.Last().Reversed);
        }

        private static string Strip(IMaterial material)
        {
            string json = MaterialIdentity.Json(material);
            return Regex.Replace(json, "\"(Name|DisplayName|Description)\":\"[^\"]*\",?", string.Empty);
        }

        // -------------------------------------------------------------------------------------------------
        // Gaps
        // -------------------------------------------------------------------------------------------------

        [Fact]
        public void A_Gap_IsAnOrdinaryLayer_NamingADerivedGasMaterial_WithItsGasTypeAndHeatTransfer()
        {
            GlazingComposition composition = BuilderFixture.Compose(BuilderFixture.Double());

            ConstructionLayer gapLayer = composition.ApertureConstruction.PaneConstructionLayers[1];
            GasMaterial gas = Assert.IsType<GasMaterial>(composition.MaterialLibrary.GetMaterial(gapLayer.Name));
            double expected = Math.Round(Analytical.Query.HeatTransferCoefficient(BuilderFixture.Gas(DefaultGasType.Argon), 0.016, Math.PI / 2), 3);

            Assert.Equal(0.016, gapLayer.Thickness, 9);
            Assert.Equal(DefaultGasType.Argon, Analytical.Query.DefaultGasType(gas));
            Assert.Equal(expected, gas.GetValue<double>(GasMaterialParameter.HeatTransferCoefficient), 9);
            Assert.Equal(string.Format(System.Globalization.CultureInfo.InvariantCulture, "Argon_16mm_{0}W/m2K_90deg", expected), gapLayer.Name);
            Assert.Equal(0.016, gas.GetValue<double>(Core.MaterialParameter.DefaultThickness), 9);
            // EN 673 at 16 mm vertical with argon: about 1.16 W/m2K (Gate 0 measured 1.160 with SAM's default argon).
            Assert.InRange(expected, 1.1, 1.25);
        }

        [Fact]
        public void The_GasDefinition_IsNotChanged_ByComposing()
        {
            GasMaterial air = BuilderFixture.Gas(DefaultGasType.Air);
            string before = air.ToJsonObject().ToJsonString();
            GlazingSystemDraft draft = new GlazingSystemDraft() { Name = "A" };
            draft.Add(BuilderFixture.Pane(BuilderFixture.ClearPane()), BuilderFixture.Gap(12, DefaultGasType.Air), BuilderFixture.Pane(BuilderFixture.ClearPane()));

            GlazingComposition composition = draft.ComposeGlazingSystem(new GlazingComposeOptions() { GasSource = x => x == DefaultGasType.Air ? air : null });

            Assert.Equal(before, air.ToJsonObject().ToJsonString());
            GasMaterial gas = composition.MaterialLibrary.GetMaterials().OfType<GasMaterial>().Single();
            Assert.Equal(DefaultGasType.Air, Analytical.Query.DefaultGasType(gas));
            Assert.NotEqual(air.Guid, gas.Guid);
        }

        [Fact]
        public void Gap_HeatTransfer_ChangesWithWidth_AndWithTheIntendedUsesOrientation()
        {
            double Htc(double millimetres, PanelType panelType)
            {
                GlazingSystemDraft draft = new GlazingSystemDraft() { Name = "H", IntendedPanelType = panelType };
                draft.Add(BuilderFixture.Pane(BuilderFixture.ClearPane()), BuilderFixture.Gap(millimetres), BuilderFixture.Pane(BuilderFixture.LowEPane()));
                GlazingComposition composition = BuilderFixture.Compose(draft);
                return composition.Provenance.Gaps.Single().HeatTransferCoefficient;
            }

            double wall12 = Htc(12, PanelType.WallExternal);
            double wall16 = Htc(16, PanelType.WallExternal);
            double roof16 = Htc(16, PanelType.Roof);
            double undefined16 = Htc(16, PanelType.Undefined);

            Assert.True(wall12 > wall16, "a narrower vertical argon gap conducts more (EN 673, below the convection optimum)");
            Assert.True(roof16 > wall16, "a horizontal gap with heat flow up convects more than a vertical one");
            Assert.Equal(wall16, undefined16, 9);
            Assert.Equal(4.21, Htc(4, PanelType.WallExternal), 2); // conduction only (Nu = 1): λ / w = 0.01684 / 0.004

            GlazingSystemDraft roof = new GlazingSystemDraft() { Name = "Roof", IntendedPanelType = PanelType.Roof };
            roof.Add(BuilderFixture.Pane(BuilderFixture.ClearPane()), BuilderFixture.Gap(16), BuilderFixture.Pane(BuilderFixture.LowEPane()));
            GlazingComposition roofComposition = BuilderFixture.Compose(roof);
            Assert.EndsWith("_0deg", roofComposition.ApertureConstruction.PaneConstructionLayers[1].Name);
            Assert.Equal(0, roofComposition.Provenance.GapEvaluationTiltDegrees);
            Assert.Contains("roof", roofComposition.Provenance.GapHeatTransferBasis);
            Assert.Equal(0, roofComposition.Provenance.Gaps.Single().TiltDegrees);
        }

        [Fact]
        public void Identical_Gaps_AreOneMaterial_AndFractionalWidthsKeepTheirMillimetres()
        {
            GlazingSystemDraft draft = new GlazingSystemDraft() { Name = "T" };
            draft.Add(BuilderFixture.Pane(BuilderFixture.ClearPane()), BuilderFixture.Gap(12), BuilderFixture.Pane(BuilderFixture.ClearPane()), BuilderFixture.Gap(12), BuilderFixture.Pane(BuilderFixture.ClearPane()), BuilderFixture.Gap(15.5), BuilderFixture.Pane(BuilderFixture.ClearPane()));

            GlazingComposition composition = BuilderFixture.Compose(draft);

            List<GasMaterial> gases = composition.MaterialLibrary.GetMaterials().OfType<GasMaterial>().ToList();
            Assert.Equal(2, gases.Count);
            Assert.Contains(gases, x => x.Name.StartsWith("Argon_15.5mm_"));
            Assert.Single(composition.MaterialLibrary.GetMaterials().OfType<TransparentMaterial>());
        }

        // -------------------------------------------------------------------------------------------------
        // Frame and parameters
        // -------------------------------------------------------------------------------------------------

        [Fact]
        public void A_CopiedFrame_BringsItsLayersMaterialsAndAdditionalHeatTransfer_AndAnExplicitWidth()
        {
            GlazingSystemDraft draft = BuilderFixture.Double();
            draft.Frame.Width = 0.05;

            GlazingComposition composition = BuilderFixture.Compose(draft);
            ApertureConstruction apertureConstruction = composition.ApertureConstruction;

            Assert.Equal(new[] { BuilderFixture.FrameMaterial }, apertureConstruction.FrameConstructionLayers.Select(x => x.Name));
            Assert.Equal(0.07, apertureConstruction.FrameConstructionLayers.Single().Thickness, 9);
            Assert.IsType<OpaqueMaterial>(composition.MaterialLibrary.GetMaterial(BuilderFixture.FrameMaterial));
            Assert.Equal(0.05, apertureConstruction.GetValue<double>(ApertureConstructionParameter.DefaultFrameWidth), 9);
            Assert.Equal(10.0, apertureConstruction.GetValue<double>(ApertureConstructionParameter.FrameAdditionalHeatTransfer), 9);
            Assert.Equal("Copied", composition.Provenance.Frame);
            Assert.Equal(BuilderFixture.SeedGuid, composition.Provenance.FrameCopiedFromGuid);
            Assert.Equal(0.05, composition.Provenance.FrameWidth, 9);
        }

        [Fact]
        public void The_CopiedWidth_IsTheSeedsDefaultFrameWidth_AndAFrameWithoutWidthWritesNone()
        {
            Assert.Equal(0.06, DraftFrame.CopyFrom(BuilderFixture.Seed(), BuilderFixture.SeedMaterials()).Width, 9);

            GlazingSystemDraft draft = BuilderFixture.Double();
            draft.Frame = DraftFrame.CopyFrom(BuilderFixture.Seed(frameWidth: false), BuilderFixture.SeedMaterials());
            Assert.True(double.IsNaN(draft.Frame.Width));

            ApertureConstruction apertureConstruction = BuilderFixture.Compose(draft).ApertureConstruction;
            Assert.True(apertureConstruction.HasFrameConstructionLayers());
            Assert.False(apertureConstruction.TryGetValue(ApertureConstructionParameter.DefaultFrameWidth, out double _));
        }

        [Fact]
        public void No_Frame_MeansNoFrameLayers_NoFrameWidth_AndNoFrameMaterial()
        {
            GlazingSystemDraft draft = BuilderFixture.Double();
            draft.Frame = DraftFrame.None();

            GlazingComposition composition = BuilderFixture.Compose(draft);

            Assert.False(composition.ApertureConstruction.HasFrameConstructionLayers());
            Assert.False(composition.ApertureConstruction.TryGetValue(ApertureConstructionParameter.DefaultFrameWidth, out double _));
            Assert.Null(composition.MaterialLibrary.GetMaterial(BuilderFixture.FrameMaterial));
            Assert.Equal("None", composition.Provenance.Frame);
            Assert.True(DraftFrame.CopyFrom(new ApertureConstruction(Guid.NewGuid(), "pane only", ApertureType.Window, new[] { new ConstructionLayer(BuilderFixture.Clear, 0.004) }), BuilderFixture.SeedMaterials()).IsNone);
        }

        [Fact]
        public void The_System_IsAWindow_ForItsIntendedUse_WithABuildUpDescription_AndNoUgLtParameters()
        {
            ApertureConstruction apertureConstruction = BuilderFixture.Compose(BuilderFixture.Double()).ApertureConstruction;

            Assert.Equal(ApertureType.Window, apertureConstruction.ApertureType);
            Assert.Equal("E0 Double", apertureConstruction.Name);
            Assert.Equal(PanelType.WallExternal, Analytical.Query.PanelType(apertureConstruction));
            string description = apertureConstruction.GetValue<string>(ApertureConstructionParameter.Description);
            Assert.Contains("Outside to inside: " + BuilderFixture.Clear + " 4 mm | Argon 16 mm | " + BuilderFixture.LowE + " 4 mm", description);
            Assert.Contains("frame copied from SEED_GLZ", description);

            Assert.False(apertureConstruction.TryGetValue(ApertureConstructionParameter.ThermalTransmittance, out double _));
            Assert.False(apertureConstruction.TryGetValue(ApertureConstructionParameter.TotalSolarEnergyTransmittance, out double _));
            Assert.False(apertureConstruction.TryGetValue(ApertureConstructionParameter.LightTransmittance, out double _));
        }

        [Theory]
        [InlineData(PanelType.WallExternal)]
        [InlineData(PanelType.CurtainWall)]
        [InlineData(PanelType.Roof)]
        [InlineData(PanelType.WallInternal)]
        public void The_IntendedUse_RoundTripsThroughQueryPanelType(PanelType panelType)
        {
            GlazingSystemDraft draft = BuilderFixture.Double();
            draft.IntendedPanelType = panelType;

            ApertureConstruction apertureConstruction = BuilderFixture.Compose(draft).ApertureConstruction;
            ApertureConstruction roundTripped = new ApertureConstruction(apertureConstruction.ToJsonObject());

            Assert.Equal(panelType, Analytical.Query.PanelType(apertureConstruction));
            Assert.Equal(panelType, Analytical.Query.PanelType(roundTripped));
        }

        [Fact]
        public void Without_AnIntendedUse_NoDefaultPanelTypeIsWritten()
        {
            GlazingSystemDraft draft = BuilderFixture.Double();
            draft.IntendedPanelType = PanelType.Undefined;

            Assert.Equal(PanelType.Undefined, Analytical.Query.PanelType(BuilderFixture.Compose(draft).ApertureConstruction));
        }

        // -------------------------------------------------------------------------------------------------
        // Materials and identity
        // -------------------------------------------------------------------------------------------------

        [Fact]
        public void Only_TheMaterialsTheSystemNames_AreInItsLibrary()
        {
            GlazingSystemDraft draft = BuilderFixture.Double();

            GlazingComposition composition = BuilderFixture.Compose(draft);

            List<string> names = composition.MaterialLibrary.GetMaterials().Select(x => x.Name).OrderBy(x => x).ToList();
            List<string> layers = composition.ApertureConstruction.PaneConstructionLayers.Concat(composition.ApertureConstruction.FrameConstructionLayers).Select(x => x.Name).Distinct().OrderBy(x => x).ToList();
            Assert.Equal(layers, names);
            Assert.DoesNotContain("Seed air", names);  // the seed's other materials stay behind
            Assert.DoesNotContain(BuilderFixture.Tint, names);
            Assert.True(composition.IsComplete);
            Assert.NotNull(composition.ContentKey);
        }

        [Fact]
        public void A_MissingMaterial_LeavesTheCompositionIncomplete_WithNoContentKey()
        {
            GlazingSystemDraft missingPane = new GlazingSystemDraft() { Name = "M" };
            missingPane.Add(DraftPane.Missing("Gone pane", 0.004, "old.tcd"), BuilderFixture.Gap(), BuilderFixture.Pane(BuilderFixture.ClearPane()));

            GlazingComposition composition = BuilderFixture.Compose(missingPane);

            Assert.False(composition.IsComplete);
            Assert.Null(composition.ContentKey);
            Assert.Equal(new[] { "Gone pane" }, composition.MissingMaterials);

            // A frame material the seed's library does not have.
            GlazingSystemDraft missingFrame = BuilderFixture.Double();
            missingFrame.Frame = DraftFrame.CopyFrom(BuilderFixture.Seed(), new MaterialLibrary("empty"));
            GlazingComposition frameComposition = BuilderFixture.Compose(missingFrame);
            Assert.False(frameComposition.IsComplete);
            Assert.Equal(new[] { BuilderFixture.FrameMaterial }, frameComposition.MissingMaterials);
        }

        [Fact]
        public void Two_DifferentPanesOfOneName_AreBothKept_TheSecondRenamed_WithItsLayer()
        {
            GlazingSystemDraft draft = new GlazingSystemDraft() { Name = "V" };
            draft.Add(new DraftPane(BuilderFixture.ClearPane(), double.NaN, "IGDB v69.tcd"), BuilderFixture.Gap(), new DraftPane(BuilderFixture.ClearPane(conductivity: 0.9), double.NaN, "IGDB v76.tcd"));

            GlazingComposition composition = BuilderFixture.Compose(draft);

            List<string> names = PaneNames(composition);
            Assert.Equal(BuilderFixture.Clear, names.Last());                         // outside: the first one added keeps its name
            Assert.Equal(BuilderFixture.Clear + " (IGDB v76.tcd)", names.First());    // inside: renamed, and the layer with it
            Assert.Equal(0.9, ((TransparentMaterial)composition.MaterialLibrary.GetMaterial(names.First())).ThermalConductivity, 9);
            Assert.Equal(1.0, ((TransparentMaterial)composition.MaterialLibrary.GetMaterial(names.Last())).ThermalConductivity, 9);
            Assert.True(composition.IsComplete);
            Assert.Equal(names.First(), composition.Provenance.Panes.Last().Material);
        }

        [Fact]
        public void The_DraftKeepsASnapshotOfItsPaneMaterial()
        {
            TransparentMaterial clear = BuilderFixture.ClearPane();
            DraftPane pane = new DraftPane(clear, double.NaN, "x.tcd");

            clear.SetValue(TransparentMaterialParameter.ExternalEmissivity, 0.1);

            Assert.Equal(0.84, ((TransparentMaterial)pane.Material).GetValue<double>(TransparentMaterialParameter.ExternalEmissivity), 9);
            Assert.Equal(0.004, pane.Thickness, 9);  // the material's default thickness
        }

        // -------------------------------------------------------------------------------------------------
        // Identity: a stable evaluation Guid per draft; Save passes a new one
        // -------------------------------------------------------------------------------------------------

        [Fact]
        public void A_Draft_KeepsOneEvaluationGuid_AndAGuidOptionReplacesIt()
        {
            GlazingSystemDraft draft = BuilderFixture.Double();
            Guid first = BuilderFixture.Compose(draft).ApertureConstruction.Guid;
            draft.Layers.Add(BuilderFixture.Gap(12));
            draft.Layers.Add(BuilderFixture.Pane(BuilderFixture.ClearPane()));
            Guid second = BuilderFixture.Compose(draft).ApertureConstruction.Guid;
            Guid saved = Guid.NewGuid();

            Assert.Equal(draft.EvaluationGuid, first);
            Assert.Equal(first, second);
            Assert.Equal(saved, BuilderFixture.Compose(draft, saved).ApertureConstruction.Guid);
            Assert.NotEqual(new GlazingSystemDraft().EvaluationGuid, new GlazingSystemDraft().EvaluationGuid);
        }

        [Fact]
        public void The_ContentKey_IgnoresGuidAndName_ButFollowsEveryLayer()
        {
            GlazingSystemDraft a = BuilderFixture.Double("A");
            GlazingSystemDraft b = BuilderFixture.Double("B");
            GlazingSystemDraft c = BuilderFixture.Double("C");
            ((DraftGap)c.Layers[1]).Thickness = 0.014;
            GlazingSystemDraft d = BuilderFixture.Double("D");
            ((DraftPane)d.Layers[2]).Reversed = true;

            string key = BuilderFixture.Compose(a).ContentKey;

            Assert.Equal(key, BuilderFixture.Compose(b).ContentKey);
            Assert.Equal(key, BuilderFixture.Compose(a, Guid.NewGuid()).ContentKey);
            Assert.NotEqual(key, BuilderFixture.Compose(c).ContentKey);
            Assert.NotEqual(key, BuilderFixture.Compose(d).ContentKey);
        }
    }
}
