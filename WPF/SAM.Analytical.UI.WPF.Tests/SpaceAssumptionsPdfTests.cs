// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using PdfSharp.Fonts;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using SAM.Analytical.UI.WPF.Tests.Helpers;
using SAM.Core.Reporting;
using SAM.Core.Reporting.Pdf;
using SAM.Geometry.Spatial;
using SAM.Units;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// The SAM_UI orchestration of the Space Assumptions PDF (SAM Documentation Framework PR3): selection rules,
    /// file naming, the unit system handed to the reporting framework, and the split between expected missing data
    /// (printed in the PDF) and software failures (a controlled, staged error). Report content, units and layout are
    /// SAM's and are tested there; these tests prove SAM_UI calls it correctly and ships a working renderer.
    /// </summary>
    [Collection(WpfCollection.Name)]
    public class SpaceAssumptionsPdfTests : IDisposable
    {
        private readonly string directory = Path.Combine(Path.GetTempPath(), "SAM_UI_SpaceAssumptionsPdfTests_" + Guid.NewGuid().ToString("N"));

        public SpaceAssumptionsPdfTests()
        {
            Directory.CreateDirectory(directory);
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(directory, true);
            }
            catch (IOException)
            {
            }
        }

        // ------------------------------------------------------------------ selection

        [Fact]
        public void ExactlyOneSelectedSpace_IsResolvedFromTheModel()
        {
            AnalyticalModel analyticalModel = Model(out Space office, out _);

            Space space = Query.SpaceReportPdfSpace(analyticalModel, [office], SpaceReportPdf.SpaceAssumptions, out string refusal);

            Assert.NotNull(space);
            Assert.Null(refusal);
            Assert.Equal(office.Guid, space.Guid);
        }

        [Fact]
        public void NoSpaceSelected_IsRefusedWithAMessage()
        {
            AnalyticalModel analyticalModel = Model(out _, out _);

            Assert.Null(Query.SpaceReportPdfSpace(analyticalModel, [], SpaceReportPdf.SpaceAssumptions, out string refusal_Empty));
            Assert.Contains("Select one Space", refusal_Empty);

            Assert.Null(Query.SpaceReportPdfSpace(analyticalModel, null, SpaceReportPdf.SpaceAssumptions, out string refusal_Null));
            Assert.Contains("Select one Space", refusal_Null);
        }

        [Fact]
        public void MultipleSpacesSelected_AreRefused_NotReducedToTheFirst()
        {
            AnalyticalModel analyticalModel = Model(out Space office, out Space store);

            Space space = Query.SpaceReportPdfSpace(analyticalModel, [office, store], SpaceReportPdf.SpaceAssumptions, out string refusal);

            Assert.Null(space);
            Assert.Contains("2 Spaces are selected", refusal);
            Assert.Contains("one Space at a time", refusal);
        }

        [Fact]
        public void TheSameSpaceSelectedTwice_IsOneSpace()
        {
            AnalyticalModel analyticalModel = Model(out Space office, out _);

            Space space = Query.SpaceReportPdfSpace(analyticalModel, [office, office], SpaceReportPdf.SpaceAssumptions, out string refusal);

            Assert.NotNull(space);
            Assert.Null(refusal);
        }

        [Fact]
        public void AStaleSelection_ReportsTheModelsCurrentSpace_OrIsRefusedWhenRemoved()
        {
            AnalyticalModel analyticalModel = Model(out Space office, out _);

            //A selection captured before an edit: same Guid, old name.
            Space stale = new Space(office.Guid, "Old name", new Point3D(0, 0, 0));

            Space space = Query.SpaceReportPdfSpace(analyticalModel, [stale], SpaceReportPdf.SpaceAssumptions, out _);
            Assert.Equal(office.Name, space.Name);

            Space removed = new Space(Guid.NewGuid(), "Removed", new Point3D(0, 0, 0));
            Assert.Null(Query.SpaceReportPdfSpace(analyticalModel, [removed], SpaceReportPdf.SpaceAssumptions, out string refusal));
            Assert.Contains("no longer in the model", refusal);
        }

        [Fact]
        public void NoModel_IsRefused()
        {
            Assert.Null(Query.SpaceReportPdfSpace(null, [new Space("A")], SpaceReportPdf.SpaceAssumptions, out string refusal));
            Assert.Contains("Open an analytical model", refusal);
        }

        [WpfFact]
        public void TheContextMenuItem_IsEnabledForOneSpace_AndDisabledWithAReasonForSeveral()
        {
            Space space_1 = new Space("One");
            Space space_2 = new Space("Two");

            MenuItem menuItem_One = Create.MenuItem_SpaceReportPdf([space_1], SpaceReportPdf.SpaceAssumptions, null);
            Assert.True(menuItem_One.IsEnabled);
            Assert.Equal("Space Assumptions PDF", menuItem_One.Header);

            MenuItem menuItem_Two = Create.MenuItem_SpaceReportPdf([space_1, space_2], SpaceReportPdf.SpaceAssumptions, null);
            Assert.False(menuItem_Two.IsEnabled);
            Assert.Contains("one Space at a time", menuItem_Two.ToolTip as string);
            Assert.True(ToolTipService.GetShowOnDisabled(menuItem_Two));
        }

        // ------------------------------------------------------------------ file name

        [Theory]
        [InlineData("Open Plan Office", "Open Plan Office - Space Assumptions.pdf")]
        [InlineData("1.01 Office", "1.01 Office - Space Assumptions.pdf")]
        [InlineData("00_011 Open Plan Office", "00_011 Open Plan Office - Space Assumptions.pdf")]
        [InlineData("Plant/Store: A*B?\"<>|", "Plant_Store_ A_B_____ - Space Assumptions.pdf")]
        [InlineData("  Lobby.  ", "Lobby - Space Assumptions.pdf")]
        [InlineData("CON", "_CON - Space Assumptions.pdf")]
        public void TheDefaultFileName_IsTheSpaceNameMadeSafe(string name, string expected)
        {
            Assert.Equal(expected, Query.SpaceReportPdfFileName(new Space(name), SpaceReportPdf.SpaceAssumptions));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("???")]
        public void ASpaceWithNoUsableName_FallsBackToItsGuid(string name)
        {
            Guid guid = new Guid("00000000-0000-0000-0000-000000000042");
            Space space = new Space(guid, name, new Point3D(0, 0, 0));

            Assert.Equal("Space 00000000-0000-0000-0000-000000000042 - Space Assumptions.pdf", Query.SpaceReportPdfFileName(space, SpaceReportPdf.SpaceAssumptions));
        }

        [Fact]
        public void AVeryLongName_IsShortened()
        {
            string fileName = Query.SpaceReportPdfFileName(new Space(new string('A', 400)), SpaceReportPdf.SpaceAssumptions);

            Assert.True(fileName.Length < 200);
            Assert.EndsWith(" - Space Assumptions.pdf", fileName);
        }

        // ------------------------------------------------------------------ generation

        [Fact]
        public void OneSpace_WritesANonEmptyOnePageA4Pdf()
        {
            AnalyticalModel analyticalModel = Model(out Space office, out _);
            string path = Path.Combine(directory, Query.SpaceReportPdfFileName(office, SpaceReportPdf.SpaceAssumptions));

            SpaceReportPdfResult result = Modify.WriteSpaceReportPdf(analyticalModel, office, path, SpaceReportPdf.SpaceAssumptions);

            Assert.True(result.Succeeded, result.Message);
            Assert.Equal(SpaceReportPdfFailure.None, result.Failure);
            Assert.Equal(path, result.Path);
            Assert.True(File.Exists(path));
            Assert.Equal(new FileInfo(path).Length, result.Length);
            Assert.True(result.Length > 1000);
            Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(File.ReadAllBytes(path), 0, 4));
            Assert.False(File.Exists(path + ".tmp"));

            using (PdfDocument pdfDocument = PdfReader.Open(path, PdfDocumentOpenMode.Import))
            {
                Assert.Equal(1, pdfDocument.PageCount);
                Assert.Equal(595, Math.Round(pdfDocument.Pages[0].Width.Point));
                Assert.Equal(842, Math.Round(pdfDocument.Pages[0].Height.Point));
            }

            //The one process-wide PDFsharp resolver is the renderer's; a second run reuses it.
            Assert.Same(NotoSansFontResolver.Instance, GlobalFontSettings.FontResolver);
            Assert.True(Modify.WriteSpaceReportPdf(analyticalModel, office, Path.Combine(directory, "again.pdf"), SpaceReportPdf.SpaceAssumptions).Succeeded);
        }

        [Fact]
        public void SparseSpaceData_IsNotAUIError_ThePdfIsStillWritten()
        {
            //No internal condition, no geometry, no flows, no design loads, no systems.
            Space sparse = new Space(Guid.NewGuid(), "Bare Store", new Point3D(0, 0, 0));
            AdjacencyCluster adjacencyCluster = new AdjacencyCluster();
            adjacencyCluster.AddObject(sparse);
            AnalyticalModel analyticalModel = new AnalyticalModel("Sparse", null, null, null, adjacencyCluster);

            string path = Path.Combine(directory, "sparse.pdf");
            SpaceReportPdfResult result = Modify.WriteSpaceReportPdf(analyticalModel, sparse, path, SpaceReportPdf.SpaceAssumptions);

            Assert.True(result.Succeeded, result.Message);
            Assert.NotEmpty(result.Notes);
            Assert.True(new FileInfo(path).Length > 0);
        }

        [Theory]
        [InlineData(UnitStyle.SI, "m²", "ft²")]
        [InlineData(UnitStyle.Imperial, "ft²", "m²")]
        public void TheUnitSystem_IsPassedToTheReportingFramework(UnitStyle unitStyle, string expected, string unexpected)
        {
            AnalyticalModel analyticalModel = Model(out Space office, out _);
            CapturingRenderer capturingRenderer = new CapturingRenderer();

            SpaceReportPdfResult result = Modify.WriteSpaceReportPdf(analyticalModel, office, Path.Combine(directory, unitStyle + ".pdf"), SpaceReportPdf.SpaceAssumptions, unitStyle, capturingRenderer);

            Assert.True(result.Succeeded, result.Message);

            List<string> units = capturingRenderer.Document.FormattedValues().Select(x => x.Unit).Where(x => !string.IsNullOrEmpty(x)).ToList();
            Assert.Contains(expected, units);
            Assert.DoesNotContain(unexpected, units);
        }

        [Fact]
        public void TheDefault_IsSI_WithAirFlowInLitresPerSecond()
        {
            AnalyticalModel analyticalModel = Model(out Space office, out _);
            CapturingRenderer capturingRenderer = new CapturingRenderer();

            Modify.WriteSpaceReportPdf(analyticalModel, office, Path.Combine(directory, "default.pdf"), SpaceReportPdf.SpaceAssumptions, documentRenderer: capturingRenderer);

            List<string> units = capturingRenderer.Document.FormattedValues().Select(x => x.Unit).ToList();
            Assert.Contains("m²", units);
            Assert.Contains(units, x => x != null && x.Equals("l/s", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain("cfm", units);
        }

        [Fact]
        public void ARendererException_IsAControlledRenderingFailure_AndLeavesAnExistingFileAlone()
        {
            AnalyticalModel analyticalModel = Model(out Space office, out _);
            string path = Path.Combine(directory, "existing.pdf");
            File.WriteAllText(path, "previous");

            InvalidOperationException invalidOperationException = new InvalidOperationException("font resolver conflict");
            SpaceReportPdfResult result = Modify.WriteSpaceReportPdf(analyticalModel, office, path, SpaceReportPdf.SpaceAssumptions, documentRenderer: new ThrowingRenderer(invalidOperationException));

            Assert.False(result.Succeeded);
            Assert.Equal(SpaceReportPdfFailure.Rendering, result.Failure);
            Assert.Same(invalidOperationException, result.Exception);
            Assert.Contains("could not be rendered", result.Message);
            Assert.Contains("font resolver conflict", result.Message);
            Assert.Equal("previous", File.ReadAllText(path));
            Assert.False(File.Exists(path + ".tmp"));
        }

        [Fact]
        public void AnUnwritablePath_IsAControlledOutputFailure()
        {
            AnalyticalModel analyticalModel = Model(out Space office, out _);
            string path = Path.Combine(directory, "no such folder", "x.pdf");

            SpaceReportPdfResult result = Modify.WriteSpaceReportPdf(analyticalModel, office, path, SpaceReportPdf.SpaceAssumptions);

            Assert.Equal(SpaceReportPdfFailure.Output, result.Failure);
            Assert.NotNull(result.Exception);
            Assert.Contains("could not be saved", result.Message);
            Assert.False(File.Exists(path));
        }

        [Fact]
        public void ADestinationLockedByAViewer_IsAnOutputFailure_AndIsNotDamaged()
        {
            AnalyticalModel analyticalModel = Model(out Space office, out _);
            string path = Path.Combine(directory, "open-in-viewer.pdf");
            File.WriteAllText(path, "previous");

            SpaceReportPdfResult result;
            using (new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                result = Modify.WriteSpaceReportPdf(analyticalModel, office, path, SpaceReportPdf.SpaceAssumptions);
            }

            Assert.Equal(SpaceReportPdfFailure.Output, result.Failure);
            Assert.Contains("close the file", result.Message);
            Assert.Equal("previous", File.ReadAllText(path));
            Assert.False(File.Exists(path + ".tmp"));
        }

        [Fact]
        public void AnExistingFile_IsReplaced_OnceTheUserConfirmedTheOverwrite()
        {
            AnalyticalModel analyticalModel = Model(out Space office, out _);
            string path = Path.Combine(directory, "replace.pdf");
            File.WriteAllText(path, "previous");

            Assert.True(Modify.WriteSpaceReportPdf(analyticalModel, office, path, SpaceReportPdf.SpaceAssumptions).Succeeded);
            Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(File.ReadAllBytes(path), 0, 4));
        }

        // ------------------------------------------------------------------ fixtures

        /// <summary>
        /// A representative office with real geometry, an internal condition, flows and design loads, beside a
        /// store with nothing assigned.
        /// </summary>
        private static AnalyticalModel Model(out Space office, out Space store)
        {
            PartFPlanModel partFPlanModel = new PartFPlanModel().Room("00_011 Open Plan Office", 8).Room("00_012 Store", 3);

            InternalCondition internalCondition = new InternalCondition("S39_Office_OpenPlan_NCM");
            internalCondition.SetValue(InternalConditionParameter.AreaPerPerson, 10.0);
            internalCondition.SetValue(InternalConditionParameter.OccupancySensibleGainPerPerson, 75.0);
            internalCondition.SetValue(InternalConditionParameter.OccupancyLatentGainPerPerson, 55.0);
            internalCondition.SetValue(InternalConditionParameter.LightingGainPerArea, 8.0);
            internalCondition.SetValue(InternalConditionParameter.EquipmentSensibleGainPerArea, 15.0);
            internalCondition.SetValue(InternalConditionParameter.InfiltrationAirChangesPerHour, 0.25);

            office = partFPlanModel.Space("00_011 Open Plan Office");
            office.InternalCondition = internalCondition;
            office.SetValue(SpaceParameter.SupplyAirFlow, 0.198);
            office.SetValue(SpaceParameter.ExhaustAirFlow, 0.180);
            office.SetValue(SpaceParameter.OutsideSupplyAirFlow, 0.040);
            office.SetValue(SpaceParameter.DesignHeatingLoad, 779.0);
            office.SetValue(SpaceParameter.DesignCoolingLoad, 1429.0);
            partFPlanModel.AdjacencyCluster.AddObject(office);

            store = partFPlanModel.Space("00_012 Store");

            return new AnalyticalModel("Representative", null, null, null, partFPlanModel.AdjacencyCluster);
        }

        private sealed class CapturingRenderer : IDocumentRenderer
        {
            public Document Document { get; private set; }

            public string FileExtension => ".pdf";

            public void Render(Document document, Stream stream)
            {
                Document = document;
                stream.Write(System.Text.Encoding.ASCII.GetBytes("%PDF-stub"));
            }
        }

        private sealed class ThrowingRenderer(Exception exception) : IDocumentRenderer
        {
            public string FileExtension => ".pdf";

            public void Render(Document document, Stream stream)
            {
                stream.Write(System.Text.Encoding.ASCII.GetBytes("%PDF-partial"));

                throw exception;
            }
        }
    }
}
