// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.UI.WPF.Tests.Helpers;
using SAM.Core;
using System.Linq;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// Stage E0-1: the Builder's authoring check. SAM's own <c>Create.Log</c> rules run on the composed system (reported with the draft layer
    /// they concern); the Builder adds the rules SAM does not have. Any state can be represented while editing; errors only block calculation
    /// and Save.
    /// </summary>
    public class GlazingBuilderValidationTests
    {
        private static GlazingDraftIssue Single(GlazingDraftValidation validation, string code)
        {
            return Assert.Single(validation.Issues, x => x.Code == code);
        }

        [Fact]
        public void A_CleanDouble_HasNoErrorsOrWarnings()
        {
            GlazingSystemDraft draft = BuilderFixture.Double();
            draft.Frame.Width = 0.06;

            GlazingDraftValidation validation = BuilderFixture.Check(draft, new[] { "Another" });

            Assert.False(validation.HasErrors, string.Join("\n", validation.Issues));
            Assert.Empty(validation.Warnings);
        }

        [Fact]
        public void A_MissingPaneMaterial_IsOneErrorOnThatLayer()
        {
            GlazingSystemDraft draft = new GlazingSystemDraft() { Name = "M", IntendedPanelType = PanelType.WallExternal };
            draft.Add(BuilderFixture.Pane(BuilderFixture.ClearPane()), BuilderFixture.Gap(), DraftPane.Missing("Gone", 0.004));

            GlazingDraftValidation validation = BuilderFixture.Check(draft);

            Assert.Equal(2, Single(validation, GlazingDraftIssueCodes.MissingPaneMaterial).LayerIndex);
            Assert.False(validation.Has(GlazingDraftIssueCodes.MissingMaterial)); // not reported twice
            Assert.True(validation.HasErrors);
        }

        [Fact]
        public void A_MaterialMissingFromTheComposedLibrary_IsSamsError()
        {
            GlazingSystemDraft draft = BuilderFixture.Double();
            draft.Frame = DraftFrame.CopyFrom(BuilderFixture.Seed(), new MaterialLibrary("empty"));

            GlazingDraftIssue issue = Single(BuilderFixture.Check(draft), GlazingDraftIssueCodes.MissingMaterial);

            Assert.Equal(GlazingDraftIssueSeverity.Error, issue.Severity);
            Assert.True(issue.FromSam);
            Assert.StartsWith("Frame: ", issue.Message);
        }

        [Fact]
        public void An_EmptyLayerName_AndAThicknessOfZero_AreSamsErrors_OnTheirDraftLayers()
        {
            GlazingSystemDraft draft = new GlazingSystemDraft() { Name = "T", IntendedPanelType = PanelType.WallExternal };
            draft.Add(DraftPane.Missing(string.Empty, 0.004), BuilderFixture.Gap(), new DraftPane(BuilderFixture.ClearPane(), 0));

            GlazingDraftValidation validation = BuilderFixture.Check(draft);

            GlazingDraftIssue name = Single(validation, GlazingDraftIssueCodes.EmptyLayerName);
            Assert.Equal(0, name.LayerIndex);
            Assert.True(name.FromSam);
            GlazingDraftIssue thickness = Single(validation, GlazingDraftIssueCodes.Thickness);
            Assert.Equal(2, thickness.LayerIndex);
            Assert.Equal(GlazingDraftIssueSeverity.Error, thickness.Severity);
        }

        [Fact]
        public void A_GapAtTheOutsideOrInsideEdge_IsSamsError_OnThatEdge()
        {
            GlazingSystemDraft outside = new GlazingSystemDraft() { Name = "O", IntendedPanelType = PanelType.WallExternal };
            outside.Add(BuilderFixture.Gap(), BuilderFixture.Pane(BuilderFixture.ClearPane()));
            GlazingSystemDraft inside = new GlazingSystemDraft() { Name = "I", IntendedPanelType = PanelType.WallExternal };
            inside.Add(BuilderFixture.Pane(BuilderFixture.ClearPane()), BuilderFixture.Gap(), BuilderFixture.Pane(BuilderFixture.ClearPane()), BuilderFixture.Gap());

            GlazingDraftIssue atOutside = Single(BuilderFixture.Check(outside), GlazingDraftIssueCodes.GasAtEdge);
            GlazingDraftIssue atInside = Single(BuilderFixture.Check(inside), GlazingDraftIssueCodes.GasAtEdge);

            Assert.Equal(0, atOutside.LayerIndex);
            Assert.Equal(3, atInside.LayerIndex);
            Assert.Equal(GlazingDraftIssueSeverity.Error, atInside.Severity);
            Assert.True(atInside.FromSam);
        }

        [Fact]
        public void No_Panes_IsAnError()
        {
            GlazingSystemDraft draft = new GlazingSystemDraft() { Name = "E", IntendedPanelType = PanelType.WallExternal };

            GlazingDraftIssue issue = Single(BuilderFixture.Check(draft), GlazingDraftIssueCodes.NoPanes);

            Assert.Equal(GlazingDraftIssueSeverity.Error, issue.Severity);
        }

        [Fact]
        public void An_OpaqueMaterialInThePaneStack_IsAnError()
        {
            GlazingSystemDraft draft = new GlazingSystemDraft() { Name = "P", IntendedPanelType = PanelType.WallExternal };
            draft.Add(BuilderFixture.Pane(BuilderFixture.ClearPane()), BuilderFixture.Gap(), new DraftPane(BuilderFixture.Frame(), 0.004));

            GlazingDraftIssue issue = Single(BuilderFixture.Check(draft), GlazingDraftIssueCodes.NotGlass);

            Assert.Equal(GlazingDraftIssueSeverity.Error, issue.Severity);
            Assert.Equal(2, issue.LayerIndex);
        }

        [Fact]
        public void An_UnofferedOrMissingGas_IsAnError()
        {
            GlazingSystemDraft xenon = new GlazingSystemDraft() { Name = "X", IntendedPanelType = PanelType.WallExternal };
            xenon.Add(BuilderFixture.Pane(BuilderFixture.ClearPane()), BuilderFixture.Gap(12, DefaultGasType.Xenon), BuilderFixture.Pane(BuilderFixture.ClearPane()));
            GlazingSystemDraft none = new GlazingSystemDraft() { Name = "U", IntendedPanelType = PanelType.WallExternal };
            none.Add(BuilderFixture.Pane(BuilderFixture.ClearPane()), BuilderFixture.Gap(12, DefaultGasType.Undefined), BuilderFixture.Pane(BuilderFixture.ClearPane()));

            Assert.Equal(1, Single(BuilderFixture.Check(xenon), GlazingDraftIssueCodes.UnsupportedGas).LayerIndex);
            GlazingDraftValidation validation = BuilderFixture.Check(none);
            Assert.Equal(1, Single(validation, GlazingDraftIssueCodes.UnsupportedGas).LayerIndex);
            Assert.False(validation.Has(GlazingDraftIssueCodes.MissingMaterial));
        }

        [Fact]
        public void A_GasWithoutADefinition_IsAnError()
        {
            GlazingSystemDraft draft = BuilderFixture.Double();

            GlazingDraftValidation validation = draft.CheckGlazingDraft(draft.ComposeGlazingSystem(new GlazingComposeOptions() { GasSource = x => null }));

            Assert.Equal(1, Single(validation, GlazingDraftIssueCodes.GasUnavailable).LayerIndex);
        }

        [Fact]
        public void Save_NeedsAName_AndANameNotInTheLibrary_ButEditingDoesNot()
        {
            GlazingSystemDraft unnamed = BuilderFixture.Double("  ");
            GlazingSystemDraft taken = BuilderFixture.Double(" e0 DOUBLE ");

            Assert.False(BuilderFixture.Check(unnamed).Has(GlazingDraftIssueCodes.NameRequired));
            Assert.Equal(GlazingDraftIssueSeverity.Error, Single(BuilderFixture.Check(unnamed, new string[0]), GlazingDraftIssueCodes.NameRequired).Severity);
            Assert.Equal(GlazingDraftIssueSeverity.Error, Single(BuilderFixture.Check(taken, new[] { "Other", "E0 Double" }), GlazingDraftIssueCodes.DuplicateName).Severity);
            Assert.False(BuilderFixture.Check(taken, new[] { "E0 Double 2" }).Has(GlazingDraftIssueCodes.DuplicateName));
        }

        [Fact]
        public void Two_PanesInContact_AndTwoGapsInARow_AreWarnings()
        {
            GlazingSystemDraft draft = new GlazingSystemDraft() { Name = "W", IntendedPanelType = PanelType.WallExternal };
            draft.Add(BuilderFixture.Pane(BuilderFixture.ClearPane()), BuilderFixture.Pane(BuilderFixture.ClearPane()), BuilderFixture.Gap(8), BuilderFixture.Gap(8), BuilderFixture.Pane(BuilderFixture.ClearPane()));

            GlazingDraftValidation validation = BuilderFixture.Check(draft);

            GlazingDraftIssue paneOnPane = Single(validation, GlazingDraftIssueCodes.PaneOnPane);
            GlazingDraftIssue gapOnGap = Single(validation, GlazingDraftIssueCodes.GapOnGap);
            Assert.Equal(GlazingDraftIssueSeverity.Warning, paneOnPane.Severity);
            Assert.Equal(1, paneOnPane.LayerIndex);
            Assert.Equal(GlazingDraftIssueSeverity.Warning, gapOnGap.Severity);
            Assert.Equal(3, gapOnGap.LayerIndex);
            Assert.False(validation.HasErrors, string.Join("\n", validation.Issues));
        }

        [Theory]
        [InlineData(3, true)]
        [InlineData(4, false)]
        [InlineData(16, false)]
        [InlineData(30, false)]
        [InlineData(32, true)]
        public void Gap_WidthsOutside4To30mm_AreWarnings(double millimetres, bool warned)
        {
            GlazingSystemDraft draft = BuilderFixture.Double();
            draft.Layers[1] = BuilderFixture.Gap(millimetres);

            GlazingDraftValidation validation = BuilderFixture.Check(draft);

            Assert.Equal(warned, validation.Has(GlazingDraftIssueCodes.GapWidth));
            Assert.False(validation.HasErrors);
        }

        [Fact]
        public void More_ThanFourPanes_NoIntendedUse_AndAFrameWithoutWidth_AreWarnings_AndFramelessIsInformation()
        {
            GlazingSystemDraft draft = new GlazingSystemDraft() { Name = "Five" };
            draft.Add(BuilderFixture.Pane(BuilderFixture.ClearPane()));
            for (int i = 0; i < 4; i++)
            {
                draft.Add(BuilderFixture.Gap(10), BuilderFixture.Pane(BuilderFixture.ClearPane()));
            }

            GlazingDraftValidation validation = BuilderFixture.Check(draft);
            Assert.Equal(GlazingDraftIssueSeverity.Warning, Single(validation, GlazingDraftIssueCodes.ManyPanes).Severity);
            Assert.Equal(GlazingDraftIssueSeverity.Warning, Single(validation, GlazingDraftIssueCodes.NoIntendedUse).Severity);
            Assert.Equal(GlazingDraftIssueSeverity.Info, Single(validation, GlazingDraftIssueCodes.Frameless).Severity);
            Assert.False(validation.HasErrors, string.Join("\n", validation.Issues));

            draft.Frame = DraftFrame.CopyFrom(BuilderFixture.Seed(frameWidth: false), BuilderFixture.SeedMaterials());
            GlazingDraftValidation framed = BuilderFixture.Check(draft);
            Assert.Equal(GlazingDraftIssueSeverity.Warning, Single(framed, GlazingDraftIssueCodes.FrameWidthMissing).Severity);
            Assert.False(framed.Has(GlazingDraftIssueCodes.Frameless));
        }

        [Fact]
        public void Pane_MaterialProperties_AreCheckedBySam()
        {
            TransparentMaterial broken = Analytical.Create.TransparentMaterial("Broken", string.Empty, "Broken", "No internal emissivity", 1, 0.004, 9999, 0.85, 0.90, 0.076, 0.076, 0.082, 0.082, 0.84, double.NaN, false);
            GlazingSystemDraft draft = BuilderFixture.Double();
            draft.Layers[0] = BuilderFixture.Pane(broken);

            GlazingDraftIssue issue = Single(BuilderFixture.Check(draft), GlazingDraftIssueCodes.MaterialProperty);

            Assert.Equal(GlazingDraftIssueSeverity.Error, issue.Severity);
            Assert.Equal(0, issue.LayerIndex);
            Assert.Contains("Internal Emissivity", issue.Message);
        }

        [Fact]
        public void Errors_AreListedFirst()
        {
            GlazingSystemDraft draft = new GlazingSystemDraft();
            draft.Add(BuilderFixture.Gap(), DraftPane.Missing("Gone", 0.004));

            GlazingDraftValidation validation = BuilderFixture.Check(draft, new string[0]);

            int lastError = validation.Issues.ToList().FindLastIndex(x => x.Severity == GlazingDraftIssueSeverity.Error);
            int firstOther = validation.Issues.ToList().FindIndex(x => x.Severity != GlazingDraftIssueSeverity.Error);
            Assert.True(lastError < firstOther);
        }
    }
}
