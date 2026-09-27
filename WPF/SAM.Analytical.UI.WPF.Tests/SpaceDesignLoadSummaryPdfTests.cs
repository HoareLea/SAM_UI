// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using SAM.Core;
using SAM.Core.Reporting;
using SAM.Geometry.Spatial;
using SAM.Units;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Ribbon;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// The SAM_UI orchestration of the Space Design Load Summary PDF (SAM Documentation Framework Phase 2, PR2D). The
    /// command shares the Space Assumptions workflow (<see cref="Modify.CreateSpaceReportPdf"/>); these tests prove it
    /// is registered beside it, calls <c>Create.SpaceDesignLoadSummary</c> with the unit system unchanged and no
    /// result source, keeps the selection, cancel and failure behaviour, and writes a real PDF for every result state.
    /// Which peak is reported, and how, is SAM's and is tested there.
    /// </summary>
    [Collection(WpfCollection.Name)]
    public class SpaceDesignLoadSummaryPdfTests : IDisposable
    {
        private const string HeatingDesignDayName = "Leeds_TRY ANN HTG 100% CONDS DB";

        private readonly string directory = Path.Combine(Path.GetTempPath(), "SAM_UI_SpaceDesignLoadSummaryPdfTests_" + Guid.NewGuid().ToString("N"));

        public SpaceDesignLoadSummaryPdfTests()
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

        // ------------------------------------------------------------------ registration

        [WpfFact]
        public void TheRibbonButton_SitsInTheReportsGroup_RightAfterSpaceAssumptions()
        {
            Windows.AnalyticalWindow analyticalWindow = new Windows.AnalyticalWindow();
            try
            {
                RibbonGroup ribbonGroup = Assert.IsType<RibbonGroup>(analyticalWindow.FindName("RibbonGroup_Edit_Reports"));
                List<RibbonButton> ribbonButtons = ribbonGroup.Items.OfType<RibbonButton>().ToList();

                Assert.Equal(["RibbonButton_SpaceAssumptionsPdf", "RibbonButton_SpaceDesignLoadSummaryPdf"], ribbonButtons.Select(x => x.Name));
                Assert.Equal("Space Design Load Summary PDF", ribbonButtons[1].Label);

                //Like Space Assumptions, disabled until a model is open.
                Assert.False(ribbonButtons[1].IsEnabled);
            }
            finally
            {
                analyticalWindow.Close();
            }
        }

        [WpfFact]
        public void TheContextMenuItem_IsEnabledForOneSpace_AndDisabledWithAReasonForSeveral()
        {
            Space space_1 = new Space("One");
            Space space_2 = new Space("Two");

            MenuItem menuItem_One = Create.MenuItem_SpaceReportPdf([space_1], SpaceReportPdf.SpaceDesignLoadSummary, null);
            Assert.True(menuItem_One.IsEnabled);
            Assert.Equal("Space Design Load Summary PDF", menuItem_One.Header);
            Assert.Equal("MenuItem_SpaceDesignLoadSummaryPdf", menuItem_One.Name);

            MenuItem menuItem_Two = Create.MenuItem_SpaceReportPdf([space_1, space_2], SpaceReportPdf.SpaceDesignLoadSummary, null);
            Assert.False(menuItem_Two.IsEnabled);
            Assert.Equal("The Space Design Load Summary PDF is created for one Space at a time: select a single Space.", menuItem_Two.ToolTip as string);
            Assert.True(ToolTipService.GetShowOnDisabled(menuItem_Two));
        }

        [Fact]
        public void TheDefaultFileName_FollowsSpaceAssumptions()
        {
            Assert.Equal("00_011 Open Plan Office - Space Design Load Summary.pdf", Query.SpaceReportPdfFileName(new Space("00_011 Open Plan Office"), SpaceReportPdf.SpaceDesignLoadSummary));
            Assert.Equal("Plant_Store_ A_B_____ - Space Design Load Summary.pdf", Query.SpaceReportPdfFileName(new Space("Plant/Store: A*B?\"<>|"), SpaceReportPdf.SpaceDesignLoadSummary));
            Assert.Equal("_CON - Space Design Load Summary.pdf", Query.SpaceReportPdfFileName(new Space("CON"), SpaceReportPdf.SpaceDesignLoadSummary));
        }

        // ------------------------------------------------------------------ entry point and units

        [Fact]
        public void TheDocument_IsSAMsSpaceDesignLoadSummary_Unchanged()
        {
            AnalyticalModel analyticalModel = Model(out Space space, HeatedAndCooled());
            CapturingRenderer capturingRenderer = new CapturingRenderer();

            SpaceReportPdfResult result = Modify.WriteSpaceReportPdf(analyticalModel, space, Path.Combine(directory, "entry.pdf"), SpaceReportPdf.SpaceDesignLoadSummary, documentRenderer: capturingRenderer);

            Assert.True(result.Succeeded, result.Message);
            Assert.Equal(Analytical.Reporting.SpaceDocumentDefinitions.SpaceDesignLoadSummary.Id, capturingRenderer.Document.Id);

            //Exactly what the SAM entry point builds with no result source: SAM_UI adds, drops and picks nothing.
            Document expected = Analytical.Reporting.Create.SpaceDesignLoadSummary(Analytical.Reporting.Create.DocumentContext(analyticalModel, new DocumentOptions()), space);
            Assert.Equal(Texts(expected), Texts(capturingRenderer.Document));
        }

        [Theory]
        [InlineData(UnitStyle.SI, "W", "Btu/h")]
        [InlineData(UnitStyle.Imperial, "Btu/h", "W")]
        public void TheUnitSystem_IsPassedToTheReportingFramework(UnitStyle unitStyle, string expected, string unexpected)
        {
            AnalyticalModel analyticalModel = Model(out Space space, HeatedAndCooled());
            CapturingRenderer capturingRenderer = new CapturingRenderer();

            SpaceReportPdfResult result = Modify.WriteSpaceReportPdf(analyticalModel, space, Path.Combine(directory, unitStyle + ".pdf"), SpaceReportPdf.SpaceDesignLoadSummary, unitStyle, capturingRenderer);

            Assert.True(result.Succeeded, result.Message);

            List<string> units = capturingRenderer.Document.FormattedValues().Select(x => x.Unit).Where(x => !string.IsNullOrEmpty(x)).ToList();
            Assert.Contains(expected, units);
            Assert.DoesNotContain(unexpected, units);
        }

        [WpfFact]
        public void TheCommand_UsesTheReportingDefault_SI()
        {
            AnalyticalModel analyticalModel = Model(out Space space, HeatedAndCooled());
            CapturingRenderer capturingRenderer = new CapturingRenderer();
            Prompts prompts = new Prompts(Path.Combine(directory, "command.pdf"));

            SpaceReportPdfResult result = Modify.CreateSpaceReportPdf(new UIAnalyticalModel(analyticalModel), [space], SpaceReportPdf.SpaceDesignLoadSummary, prompts.Build(), capturingRenderer);

            Assert.True(result.Succeeded, result.Message);
            List<string> units = capturingRenderer.Document.FormattedValues().Select(x => x.Unit).ToList();
            Assert.Contains("W", units);
            Assert.DoesNotContain("Btu/h", units);
        }

        // ------------------------------------------------------------------ result states are reported, not refused

        [Fact]
        public void AnAmbiguousResultSource_IsReportedAsAmbiguous_NeverPicked()
        {
            //Heating from Tas and from OpenStudio, both with peaks and with different loads.
            SpaceSimulationResult openStudio = Result(LoadType.Heating, new SpaceLoadPeak(LoadPeakBasis.DesignDay, 1500.0) { DesignDayName = "OpenStudio heating DD", HourOfDay = 6 }, null, "OpenStudio");
            AnalyticalModel analyticalModel = Model(out Space space, [.. HeatedAndCooled(), openStudio]);
            CapturingRenderer capturingRenderer = new CapturingRenderer();

            SpaceReportPdfResult result = Modify.WriteSpaceReportPdf(analyticalModel, space, Path.Combine(directory, "ambiguous.pdf"), SpaceReportPdf.SpaceDesignLoadSummary, documentRenderer: capturingRenderer);

            Assert.True(result.Succeeded, result.Message);

            List<string> texts = Texts(capturingRenderer.Document);
            Assert.Contains(texts, x => x.StartsWith("Ambiguous: more than one heating result records peaks"));
            Assert.DoesNotContain(texts, x => x.Contains("1,500") || x.Contains("1,140"));
        }

        [Fact]
        public void NoSimulationResults_WriteAPdf_ThatSaysNotSimulated()
        {
            AnalyticalModel analyticalModel = Model(out Space space);
            string path = Path.Combine(directory, "not-simulated.pdf");
            CapturingRenderer capturingRenderer = new CapturingRenderer();

            Assert.True(Modify.WriteSpaceReportPdf(analyticalModel, space, path, SpaceReportPdf.SpaceDesignLoadSummary, documentRenderer: capturingRenderer).Succeeded);
            Assert.Contains(Texts(capturingRenderer.Document), x => x.StartsWith("Not simulated: this space has no heating simulation results"));

            //And with the real renderer.
            SpaceReportPdfResult result = Modify.WriteSpaceReportPdf(analyticalModel, space, path, SpaceReportPdf.SpaceDesignLoadSummary);
            Assert.True(result.Succeeded, result.Message);
            Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(File.ReadAllBytes(path), 0, 4));
        }

        [Fact]
        public void LegacyResultsWithoutPeaks_WriteAPdf_ThatSaysPeaksNotRecorded()
        {
            SpaceSimulationResult legacy = new SpaceSimulationResult("Studio 1_0", "Tas", "zone-guid");
            legacy.SetValue(SpaceSimulationResultParameter.LoadType, LoadType.Heating.Text());
            legacy.SetValue(SpaceSimulationResultParameter.Load, -1.0);

            AnalyticalModel analyticalModel = Model(out Space space, [legacy]);
            CapturingRenderer capturingRenderer = new CapturingRenderer();

            SpaceReportPdfResult result = Modify.WriteSpaceReportPdf(analyticalModel, space, Path.Combine(directory, "legacy.pdf"), SpaceReportPdf.SpaceDesignLoadSummary, documentRenderer: capturingRenderer);

            Assert.True(result.Succeeded, result.Message);
            Assert.Contains(Texts(capturingRenderer.Document), x => x.StartsWith("Peaks not recorded: the heating results"));
        }

        [Fact]
        public void TypedPeaks_WriteARealA4Pdf()
        {
            AnalyticalModel analyticalModel = Model(out Space space, HeatedAndCooled());
            string path = Path.Combine(directory, Query.SpaceReportPdfFileName(space, SpaceReportPdf.SpaceDesignLoadSummary));

            SpaceReportPdfResult result = Modify.WriteSpaceReportPdf(analyticalModel, space, path, SpaceReportPdf.SpaceDesignLoadSummary);

            Assert.True(result.Succeeded, result.Message);
            Assert.Equal(SpaceReportPdfFailure.None, result.Failure);
            Assert.Equal(new FileInfo(path).Length, result.Length);
            Assert.False(File.Exists(path + ".tmp"));

            using (PdfDocument pdfDocument = PdfReader.Open(path, PdfDocumentOpenMode.Import))
            {
                Assert.True(pdfDocument.PageCount >= 1);
                Assert.Equal(595, Math.Round(pdfDocument.Pages[0].Width.Point));
                Assert.Equal(842, Math.Round(pdfDocument.Pages[0].Height.Point));
            }
        }

        // ------------------------------------------------------------------ the shared command workflow

        [WpfFact]
        public void TheCommand_RefusesNoneOrSeveralSpaces_WithTheReportsOwnWording_AndAsksForNoFile()
        {
            AnalyticalModel analyticalModel = Model(out Space space, HeatedAndCooled());
            Space other = new Space("Other");
            analyticalModel.AdjacencyCluster.AddObject(other);
            UIAnalyticalModel uIAnalyticalModel = new UIAnalyticalModel(analyticalModel);

            Prompts prompts = new Prompts(Path.Combine(directory, "never.pdf"));
            Assert.Null(Modify.CreateSpaceReportPdf(uIAnalyticalModel, [], SpaceReportPdf.SpaceDesignLoadSummary, prompts.Build()));
            Assert.Null(Modify.CreateSpaceReportPdf(uIAnalyticalModel, [space, other], SpaceReportPdf.SpaceDesignLoadSummary, prompts.Build()));

            Assert.Equal(0, prompts.PathRequests);
            Assert.Equal(
                [
                    "Select one Space, then choose Space Design Load Summary PDF.",
                    "2 Spaces are selected. The Space Design Load Summary PDF is created for one Space at a time: select a single Space.",
                ],
                prompts.Messages.Select(x => x.Text));
            Assert.All(prompts.Messages, x => Assert.Equal("Space Design Load Summary PDF", x.Caption));
            Assert.False(File.Exists(Path.Combine(directory, "never.pdf")));
        }

        [WpfFact]
        public void CancellingTheSaveDialog_DoesNothing()
        {
            AnalyticalModel analyticalModel = Model(out Space space, HeatedAndCooled());
            Prompts prompts = new Prompts(null);

            SpaceReportPdfResult result = Modify.CreateSpaceReportPdf(new UIAnalyticalModel(analyticalModel), [space], SpaceReportPdf.SpaceDesignLoadSummary, prompts.Build());

            Assert.Null(result);
            Assert.Equal(1, prompts.PathRequests);
            Assert.Equal("Save Space Design Load Summary PDF", prompts.DialogTitle);
            Assert.Equal("Bathroom_2 - Space Design Load Summary.pdf", prompts.DefaultFileName);
            Assert.Empty(prompts.Messages);
            Assert.Empty(Directory.GetFiles(directory));
        }

        [WpfFact]
        public void ASavedPdf_OffersToOpen_AndOpensItOnYes()
        {
            AnalyticalModel analyticalModel = Model(out Space space, HeatedAndCooled());
            string path = Path.Combine(directory, "saved.pdf");
            Prompts prompts = new Prompts(path, MessageBoxResult.Yes);

            SpaceReportPdfResult result = Modify.CreateSpaceReportPdf(new UIAnalyticalModel(analyticalModel), [space], SpaceReportPdf.SpaceDesignLoadSummary, prompts.Build());

            Assert.True(result.Succeeded, result.Message);
            Assert.True(File.Exists(path));
            (string text, string caption, MessageBoxButton button, MessageBoxImage _) = Assert.Single(prompts.Messages);
            Assert.Equal("Space Design Load Summary PDF saved:\n" + path + "\n\nOpen it now?", text);
            Assert.Equal(MessageBoxButton.YesNo, button);
            Assert.Equal([path], prompts.Opened);
        }

        [WpfFact]
        public void ADocumentFailure_IsShownAsAnError_NamingTheReport()
        {
            AnalyticalModel analyticalModel = Model(out Space space);
            InvalidOperationException invalidOperationException = new InvalidOperationException("collector failed");
            SpaceReportPdf throwing = new SpaceReportPdf("Throwing", "Space Design Load Summary", (_, _) => throw invalidOperationException);
            string path = Path.Combine(directory, "failed.pdf");
            Prompts prompts = new Prompts(path);

            SpaceReportPdfResult result = Modify.CreateSpaceReportPdf(new UIAnalyticalModel(analyticalModel), [space], throwing, prompts.Build());

            Assert.False(result.Succeeded);
            Assert.Equal(SpaceReportPdfFailure.Document, result.Failure);
            Assert.Same(invalidOperationException, result.Exception);

            (string text, string _, MessageBoxButton button, MessageBoxImage image) = Assert.Single(prompts.Messages);
            Assert.Equal("The Space Design Load Summary report could not be built for this Space: collector failed", text);
            Assert.Equal(MessageBoxImage.Error, image);
            Assert.Equal(MessageBoxButton.OK, button);
            Assert.Empty(prompts.Opened);
            Assert.False(File.Exists(path));
        }

        [WpfFact]
        public void AnOutputFailure_IsShownAsAnError_AndLeavesAnExistingFileAlone()
        {
            AnalyticalModel analyticalModel = Model(out Space space, HeatedAndCooled());
            string path = Path.Combine(directory, "locked.pdf");
            File.WriteAllText(path, "previous");
            Prompts prompts = new Prompts(path);

            SpaceReportPdfResult result;
            using (new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                result = Modify.CreateSpaceReportPdf(new UIAnalyticalModel(analyticalModel), [space], SpaceReportPdf.SpaceDesignLoadSummary, prompts.Build());
            }

            Assert.Equal(SpaceReportPdfFailure.Output, result.Failure);
            Assert.Equal(MessageBoxImage.Error, Assert.Single(prompts.Messages).Image);
            Assert.Equal("previous", File.ReadAllText(path));
            Assert.False(File.Exists(path + ".tmp"));
        }

        // ------------------------------------------------------------------ fixtures

        /// <summary>
        /// Bathroom_2 with a heating set point and a heating design day, related to <paramref name="results"/>.
        /// </summary>
        private static AnalyticalModel Model(out Space space, params SpaceSimulationResult[] results)
        {
            ProfileLibrary profileLibrary = new ProfileLibrary("Profiles");
            profileLibrary.Add(new Profile("Heat 16", ProfileType.Heating, Enumerable.Repeat(16.0, 24)));

            InternalCondition internalCondition = new InternalCondition("TM59_Bathroom");
            internalCondition.SetValue(InternalConditionParameter.HeatingProfileName, "Heat 16");

            space = new Space(Guid.NewGuid(), "Bathroom_2", new Point3D(1, 1, 1.5));
            space.InternalCondition = internalCondition;
            space.SetValue(SpaceParameter.Area, 5.2);
            space.SetValue(SpaceParameter.Volume, 13.0);
            space.SetValue(SpaceParameter.DesignHeatingLoad, 1368.0);

            AdjacencyCluster adjacencyCluster = new AdjacencyCluster();
            adjacencyCluster.AddObject(space);
            foreach (SpaceSimulationResult result in results)
            {
                adjacencyCluster.AddObject(result);
                adjacencyCluster.AddRelation(space, result);
            }

            return new AnalyticalModel("Design loads", null, null, null, adjacencyCluster, null, profileLibrary);
        }

        /// <summary>
        /// Bathroom_2's Tas peaks (final1b/open.tsd, as SAM_Tas#69 stores them): heating 1,140 W on the design day and
        /// 104 W over the year; cooling a real zero.
        /// </summary>
        private static SpaceSimulationResult[] HeatedAndCooled()
        {
            SpaceLoadPeak heatingDesignDay = new SpaceLoadPeak(LoadPeakBasis.DesignDay, 1139.796143) { DesignDayName = HeatingDesignDayName, HourOfDay = 23, DryBulbTemperature = 16, ResultantTemperature = 13.870544 };
            heatingDesignDay.SetComponent(LoadPeakComponent.InfiltrationVentilation, -111.737091);
            heatingDesignDay.SetComponent(LoadPeakComponent.BuildingHeatTransfer, -1023.255859);

            SpaceLoadPeak heatingAnnual = new SpaceLoadPeak(LoadPeakBasis.AnnualSimulation, 104.009911) { HourOfYear = 8553, HourOfDay = 9, DryBulbTemperature = 16.000908 };
            heatingAnnual.SetComponent(LoadPeakComponent.InfiltrationVentilation, -93.373978);

            return
            [
                Result(LoadType.Heating, heatingDesignDay, heatingAnnual),
                Result(LoadType.Cooling, new SpaceLoadPeak(LoadPeakBasis.DesignDay, 0), new SpaceLoadPeak(LoadPeakBasis.AnnualSimulation, 0)),
            ];
        }

        private static SpaceSimulationResult Result(LoadType loadType, SpaceLoadPeak designDay, SpaceLoadPeak annual, string source = "Tas")
        {
            SpaceSimulationResult result = new SpaceSimulationResult("Bathroom_2", source, "zone-guid");
            result.SetValue(SpaceSimulationResultParameter.LoadType, loadType.Text());
            if (designDay != null)
            {
                result.SetValue(SpaceSimulationResultParameter.DesignDayPeak, designDay);
            }

            if (annual != null)
            {
                result.SetValue(SpaceSimulationResultParameter.AnnualPeak, annual);
            }

            return result;
        }

        /// <summary>
        /// Every text in the document's sections, in reading order: titles, notices, key/value and table cells.
        /// </summary>
        private static List<string> Texts(Document document)
        {
            List<string> result = [];
            foreach (DocumentSection documentSection in document.Sections)
            {
                result.Add(documentSection.Title);
                foreach (DocumentBlock documentBlock in documentSection.Blocks)
                {
                    if (documentBlock is NoticeBlock noticeBlock)
                    {
                        result.Add(noticeBlock.Text);
                    }
                    else if (documentBlock is KeyValueBlock keyValueBlock)
                    {
                        result.AddRange(keyValueBlock.Rows.SelectMany(x => new[] { x.Label, x.Value.Text }));
                    }
                    else if (documentBlock is TableBlock tableBlock)
                    {
                        result.AddRange(tableBlock.Rows.SelectMany(x => x.Cells).Select(x => x.Text));
                    }
                }
            }

            return result;
        }

        private sealed class Prompts(string path, MessageBoxResult answer = MessageBoxResult.No)
        {
            public int PathRequests { get; private set; }

            public string DialogTitle { get; private set; }

            public string DefaultFileName { get; private set; }

            public List<(string Text, string Caption, MessageBoxButton Button, MessageBoxImage Image)> Messages { get; } = [];

            public List<string> Opened { get; } = [];

            public SpaceReportPdfPrompts Build()
            {
                return new SpaceReportPdfPrompts()
                {
                    ChoosePath = (title, fileName, _) =>
                    {
                        PathRequests++;
                        DialogTitle = title;
                        DefaultFileName = fileName;
                        return path;
                    },
                    ShowMessage = (text, caption, button, image) =>
                    {
                        Messages.Add((text, caption, button, image));
                        return button == MessageBoxButton.YesNo ? answer : MessageBoxResult.OK;
                    },
                    Open = Opened.Add,
                };
            }
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
    }
}
