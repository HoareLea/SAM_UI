// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using SAM.Analytical.UI;
using System;
using System.IO;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// The Part O output-folder structure: the person chooses the root, and every case writes into its own
    /// <c>&lt;root&gt;/&lt;case&gt;/tas|reports|diagnostics</c> folders through the one resolver,
    /// <see cref="PartOOutputPaths"/>. Legacy flat folders stay readable exactly as they were saved.
    /// </summary>
    public class PartOOutputFolderTests
    {
        private const string Root = "C:\\PartO";

        // ---- 1. Folder mapping ------------------------------------------------------------------------------------

        [Theory]
        [InlineData(PartOOutputCase.Iteration1a, "Iteration1a")]
        [InlineData(PartOOutputCase.Iteration1b, "Iteration1b")]
        [InlineData(PartOOutputCase.Iteration2, "Iteration2")]
        [InlineData(PartOOutputCase.Iteration2B, "Iteration2B")]
        [InlineData(PartOOutputCase.Iteration3, "Iteration3")]
        [InlineData(PartOOutputCase.MixedDesign, "MixedDesign")]
        public void EachCase_HasItsOwnFolder_WithTasReportsAndDiagnostics(PartOOutputCase partOOutputCase, string folder)
        {
            PartOOutputPaths partOOutputPaths = PartOOutputPaths.Create(Root, partOOutputCase);

            Assert.Equal(Root, partOOutputPaths.Directory_Root);
            Assert.Equal(Root + "\\" + folder, partOOutputPaths.Directory_Case);
            Assert.Equal(Root + "\\" + folder + "\\tas", partOOutputPaths.Directory_Tas);
            Assert.Equal(Root + "\\" + folder + "\\reports", partOOutputPaths.Directory_Reports);
            Assert.Equal(Root + "\\" + folder + "\\diagnostics", partOOutputPaths.Directory_Diagnostics);

            //Read back from any of its three folders, the layout names the same case and root.
            foreach (string directory in new[] { partOOutputPaths.Directory_Tas, partOOutputPaths.Directory_Reports, partOOutputPaths.Directory_Diagnostics })
            {
                PartOOutputPaths partOOutputPaths_Found = PartOOutputPaths.Find(directory);

                Assert.NotNull(partOOutputPaths_Found);
                Assert.Equal(partOOutputCase, partOOutputPaths_Found.Case);
                Assert.Equal(Root, partOOutputPaths_Found.Directory_Root);
            }
        }

        [Fact]
        public void APreparation_NamesItsCase_1a_1b_2()
        {
            Assert.Equal(PartOOutputCase.Iteration1a, PartOOutputPaths.CaseOf(PartOPreparationContext.Resumed(PartOIteration.BasePassive, [], false)));
            Assert.Equal(PartOOutputCase.Iteration2, PartOOutputPaths.CaseOf(PartOPreparationContext.Resumed(PartOIteration.BasePassive, [], true)));
            Assert.Equal(PartOOutputCase.Iteration1b, PartOOutputPaths.CaseOf(PartOPreparationContext.Resumed(PartOIteration.BaseNaturalVentilation, [], false)));

            //Not said is not guessed: an Iteration 2 must never be filed as 1a.
            Assert.Null(PartOOutputPaths.CaseOf(PartOPreparationContext.Resumed(PartOIteration.BasePassive, [], null)));
            Assert.Null(PartOOutputPaths.CaseOf(null));
        }

        [Fact]
        public void AFolderInsideTheLayout_IsNeverNested()
        {
            //A remembered or chosen folder that is already a case folder resolves to its root first.
            Assert.Equal(Root + "\\Iteration3\\tas", PartOOutputPaths.Create(Root + "\\Iteration2\\tas", PartOOutputCase.Iteration3).Directory_Tas);
            Assert.Equal(Root + "\\Iteration2B\\tas", PartOOutputPaths.Create(Root + "\\Iteration2B\\tas\\", PartOOutputCase.Iteration2B).Directory_Tas);
            Assert.Equal(Root, PartOOutputPaths.Root(Root + "\\Iteration1b\\reports"));
            Assert.Equal(Root, PartOOutputPaths.Root(Root));
        }

        // ---- 2-4. TAS files together; reports and diagnostics apart ----------------------------------------------

        [Fact]
        public void Iteration3_TasFilesAreTogether_ReportsAndDiagnosticsInTheirOwnFolders()
        {
            string path_TSD_ReferenceA = Root + "\\Iteration2\\tas\\Flat1.tsd";

            PartOIteration3Paths paths = PartOIteration3Paths.Create(PartOIteration3Fixture.SimulationContext(Root + "\\Iteration2\\tas", "Flat1"), path_TSD_ReferenceA, PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance);

            string tas = Root + "\\Iteration3\\tas\\";
            string reports = Root + "\\Iteration3\\reports\\";
            string diagnostics = Root + "\\Iteration3\\diagnostics\\";

            Assert.Equal(Root + "\\Iteration3\\tas", paths.OutputDirectory);
            Assert.Equal(tas + "Flat1-It3BMG.tbd", paths.Path_TBD_ThermalSource);
            Assert.Equal(tas + "Flat1-It3BMG.tsd", paths.Path_TSD_ThermalSource);
            Assert.Equal(tas + "Flat1-It3BMG.tpd", paths.Path_TPD);
            Assert.Equal(tas + "Flat1-It3BMG-Bridge.tbd", paths.Path_TBD_Bridge);
            Assert.Equal(tas + "Flat1-It3BMG-Bridge.tsd", paths.Path_TSD_Bridge);
            Assert.Equal(tas + "Flat1-It3BMG-Bridge.sam", paths.Path_Model_CandidateB);

            Assert.Equal(reports + "Flat1-It3BMG-Bridge-TM59.txt", paths.Path_TM59Report_CandidateB);
            Assert.Equal(reports + "Flat1-Iteration3-MG.json", paths.Path_Record);
            Assert.Equal(reports + "Flat1-Iteration3-MG-Review.txt", PartOIteration3Paths.Path_Report_ForRecord(paths.Path_Record));
            Assert.Equal(reports + "Flat1-Iteration3-MG-Review.json", PartOIteration3Paths.Path_Report_ForRecord(paths.Path_Record, "json"));

            Assert.Equal(diagnostics + "Flat1-It3BMG-OperatingAirFlow.csv", paths.Path_OperatingAirFlow);
        }

        [Theory]
        [InlineData(PartOOutputCase.Iteration1a)]
        [InlineData(PartOOutputCase.Iteration1b)]
        [InlineData(PartOOutputCase.Iteration2)]
        [InlineData(PartOOutputCase.Iteration2B)]
        [InlineData(PartOOutputCase.MixedDesign)]
        public void ARunsReportGoesToReports_ItsModelStaysBesideItsResultsInTas(PartOOutputCase partOOutputCase)
        {
            PartOOutputPaths partOOutputPaths = PartOOutputPaths.Create(Root, partOOutputCase);
            string path_TSD = Path.Combine(partOOutputPaths.Directory_Tas, "Flat1.tsd");

            Assert.Equal(Path.Combine(partOOutputPaths.Directory_Reports, "Flat1-TM59.txt"), Query.Path_TM59Report(path_TSD));
            Assert.Equal(Path.Combine(partOOutputPaths.Directory_Tas, "Flat1.sam"), Query.Path_PartORunModel(path_TSD));
            Assert.Equal(Path.Combine(partOOutputPaths.Directory_Tas, "Flat1.partorun.json"), UI.PartORunResume.Path_Resume(path_TSD));
        }

        // ---- 5. No collision -------------------------------------------------------------------------------------

        [Fact]
        public void TheSameFileName_ResolvesDifferently_In1bAnd2_TasReportsAndDiagnostics()
        {
            PartOOutputPaths paths_1b = PartOOutputPaths.Create(Root, PartOOutputCase.Iteration1b);
            PartOOutputPaths paths_2 = PartOOutputPaths.Create(Root, PartOOutputCase.Iteration2);

            string path_TSD_1b = Path.Combine(paths_1b.Directory_Tas, "Flat1.tsd");
            string path_TSD_2 = Path.Combine(paths_2.Directory_Tas, "Flat1.tsd");

            Assert.NotEqual(path_TSD_1b, path_TSD_2, StringComparer.OrdinalIgnoreCase);
            Assert.NotEqual(Query.Path_TM59Report(path_TSD_1b), Query.Path_TM59Report(path_TSD_2), StringComparer.OrdinalIgnoreCase);
            Assert.NotEqual(Query.Path_PartORunModel(path_TSD_1b), Query.Path_PartORunModel(path_TSD_2), StringComparer.OrdinalIgnoreCase);
            Assert.NotEqual(PartOOutputPaths.Directory_Diagnostics_ForFile(path_TSD_1b), PartOOutputPaths.Directory_Diagnostics_ForFile(path_TSD_2), StringComparer.OrdinalIgnoreCase);

            Assert.Equal(Root + "\\Iteration1b\\tas\\Flat1.tsd", path_TSD_1b);
            Assert.Equal(Root + "\\Iteration2\\tas\\Flat1.tsd", path_TSD_2);
        }

        [Fact]
        public void EveryCase_ResolvesTheSameFileNameToADifferentPath()
        {
            System.Collections.Generic.HashSet<string> paths = new(StringComparer.OrdinalIgnoreCase);

            foreach (PartOOutputCase partOOutputCase in Enum.GetValues(typeof(PartOOutputCase)))
            {
                PartOOutputPaths partOOutputPaths = PartOOutputPaths.Create(Root, partOOutputCase);

                Assert.True(paths.Add(Path.Combine(partOOutputPaths.Directory_Tas, "Flat1.tsd")));
                Assert.True(paths.Add(Path.Combine(partOOutputPaths.Directory_Reports, "Flat1-TM59.txt")));
                Assert.True(paths.Add(Path.Combine(partOOutputPaths.Directory_Diagnostics, "Flat1.timing.csv")));
            }
        }

        [Fact]
        public void Iteration2B_WritesIntoItsOwnFolder_NotItsBaselines_AndContinuesThere()
        {
            PartOSimulationContext partOSimulationContext_Baseline = PartOIteration3Fixture.SimulationContext(Root + "\\Iteration2\\tas", "Flat1");

            PartOSimulationContext partOSimulationContext_2B = PartOOutputPaths.SimulationContext(partOSimulationContext_Baseline, PartOOutputCase.Iteration2B);

            Assert.Equal(Root + "\\Iteration2B\\tas", partOSimulationContext_2B.OutputDirectory);
            Assert.Equal("Flat1", partOSimulationContext_2B.ProjectName);
            Assert.Same(partOSimulationContext_Baseline.WeatherData, partOSimulationContext_2B.WeatherData);

            //A second optimisation, from a round, stays in 2B's folder rather than nesting.
            Assert.Equal(Root + "\\Iteration2B\\tas", PartOOutputPaths.SimulationContext(partOSimulationContext_2B, PartOOutputCase.Iteration2B).OutputDirectory);
        }

        [Fact]
        public void MixedDesign_WritesIntoItsOwnFolder_AndItsGuidanceHistoryIsADiagnostic()
        {
            string directory = Create.PartOMixedOutputDirectory(new PartOSimulationCase { OutputDirectory = Root });

            Assert.Equal(Root + "\\MixedDesign\\tas", directory);
            Assert.Equal(Root + "\\MixedDesign\\tas\\Model_Mixed_Bridge.tbd", Query.Path_PartOMixedBridgeTBD(directory, "Model_Mixed"));
            Assert.Equal(Root + "\\MixedDesign\\diagnostics", PartOOutputPaths.Directory_Diagnostics_ForFile(Path.Combine(directory, "Model_Mixed.tpd")));
            Assert.Equal(Root + "\\MixedDesign\\reports\\Model_Mixed_Bridge-TM59.txt", Query.Path_TM59Report(Path.Combine(directory, "Model_Mixed_Bridge.tsd")));

            Assert.Null(Create.PartOMixedOutputDirectory(new PartOSimulationCase()));
        }

        // ---- 6-7. Iteration 3 keeps the Iteration 2 reference, and reopens from it alone -------------------------

        [Fact]
        public void Iteration3_ReferencesIteration2WhereItIs_AndNeverWritesThere()
        {
            string path_TSD_ReferenceA = Root + "\\Iteration2\\tas\\Flat1.tsd";

            PartOIteration3Paths paths = PartOIteration3Paths.Create(PartOIteration3Fixture.SimulationContext(Root + "\\Iteration2\\tas", "Flat1"), path_TSD_ReferenceA, PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance);

            Assert.Equal(path_TSD_ReferenceA, paths.Path_TSD_ReferenceA);
            Assert.Equal(Root + "\\Iteration2\\reports\\Flat1-TM59.txt", paths.Path_TM59Report_ReferenceA);

            foreach (string path in paths.Paths_CandidateB)
            {
                Assert.StartsWith(Root + "\\Iteration3\\", path, StringComparison.OrdinalIgnoreCase);
            }
        }

        [Fact]
        public void AReopenedIteration3_ResolvesTheSamePaths_FromTheReferenceResultsAlone()
        {
            string path_TSD_ReferenceA = Root + "\\Iteration2\\tas\\Flat1.tsd";

            PartOIteration3Paths paths_Run = PartOIteration3Paths.Create(PartOIteration3Fixture.SimulationContext(Root + "\\Iteration2\\tas", "Flat1"), path_TSD_ReferenceA, PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance);

            //A later session has only the reopened run's TSD. PartORun.TryResume rebuilds the case from it: the
            //output directory is the TSD's folder and the project name its file name.
            PartOSimulationContext partOSimulationContext_Resumed = PartOIteration3Fixture.SimulationContext(Path.GetDirectoryName(path_TSD_ReferenceA), Path.GetFileNameWithoutExtension(path_TSD_ReferenceA));
            PartOIteration3Paths paths_Resumed = PartOIteration3Paths.Create(partOSimulationContext_Resumed, path_TSD_ReferenceA, PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance);

            Assert.Equal(paths_Run.Paths_CandidateB, paths_Resumed.Paths_CandidateB);
            Assert.Equal(paths_Run.Path_Record, PartOIteration3Paths.Path_Record_ForResults(path_TSD_ReferenceA, PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance));
        }

        [Fact]
        public void APersistedIteration3Record_IsFoundFromTheReferenceResults_OnDisk()
        {
            string directory_Root = TempRoot();

            try
            {
                string path_TSD_ReferenceA = Path.Combine(directory_Root, "Iteration2", "tas", "Flat1.tsd");
                PartOIteration3Paths paths = PartOIteration3Paths.Create(PartOIteration3Fixture.SimulationContext(Path.GetDirectoryName(path_TSD_ReferenceA), "Flat1"), path_TSD_ReferenceA, PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance);

                //What the run's single exit does before writing the record.
                PartOOutputPaths.EnsureDirectoryForFile(paths.Path_Record);
                File.WriteAllText(paths.Path_Record, "{}");

                Assert.Equal(paths.Path_Record, Query.PartOIteration3RecordPath(path_TSD_ReferenceA, PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance, out bool legacy));
                Assert.False(legacy);
                Assert.Equal(Path.Combine(directory_Root, "Iteration3", "reports", "Flat1-Iteration3-MG.json"), paths.Path_Record);
            }
            finally
            {
                Directory.Delete(directory_Root, true);
            }
        }

        // ---- 8. Legacy flat folders stay readable ----------------------------------------------------------------

        [Fact]
        public void ALegacyFlatFolder_IsNotReadAsTheLayout_AndItsReportsStayBesideTheResults()
        {
            Assert.Null(PartOOutputPaths.Find("C:\\TasOut\\Project"));

            //A folder a person made by hand before this existed - named like a case, but flat - is legacy too.
            Assert.Null(PartOOutputPaths.Find("C:\\TasOut\\Iteration2"));
            Assert.Null(PartOOutputPaths.Find("C:\\TasOut\\tas"));

            Assert.Equal("C:\\TasOut\\Iteration2\\Flat1-TM59.txt", Query.Path_TM59Report("C:\\TasOut\\Iteration2\\Flat1.tsd"));
            Assert.Equal("C:\\out\\Flat1-TM59.txt", Query.Path_TM59Report("C:\\out\\Flat1.tsd"));
            Assert.Equal("C:\\out\\Flat1.sam", Query.Path_PartORunModel("C:\\out\\Flat1.tsd"));
            Assert.Equal("C:\\out", PartOOutputPaths.Directory_Diagnostics_ForFile("C:\\out\\Flat1.tpd"));

            //The legacy record paths beside the results are still derived exactly as before.
            Assert.Equal("C:\\out\\Flat1-Iteration3.json", PartOIteration3Paths.Path_Record_ForResults("C:\\out\\Flat1.tsd"));
            Assert.Equal("C:\\out\\Flat1-Iteration3-MG.json", PartOIteration3Paths.Path_Record_ForResults_Legacy("C:\\out\\Flat1.tsd", PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance));
            Assert.Equal("C:\\out\\Flat1-Iteration3-MG-Review.txt", PartOIteration3Paths.Path_Report_ForRecord("C:\\out\\Flat1-Iteration3-MG.json"));
        }

        [Fact]
        public void ALegacyPerMethodRecord_BesideTheResults_IsStillFound_AndANewOneSupersedesIt()
        {
            string directory_Root = TempRoot();

            try
            {
                Directory.CreateDirectory(directory_Root);

                string path_TSD = Path.Combine(directory_Root, "Flat1.tsd");
                string path_Legacy = Path.Combine(directory_Root, "Flat1-Iteration3-MG.json");
                File.WriteAllText(path_Legacy, "{}");

                Assert.Equal(path_Legacy, Query.PartOIteration3RecordPath(path_TSD, PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance, out bool legacy));
                Assert.False(legacy);

                //A new run against the same legacy results writes into Iteration 3's folder beneath the flat folder -
                //the legacy record is left untouched, and the new one is the one read from then on.
                string path_New = PartOIteration3Paths.Path_Record_ForResults(path_TSD, PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance);
                Assert.Equal(Path.Combine(directory_Root, "Iteration3", "reports", "Flat1-Iteration3-MG.json"), path_New);

                PartOOutputPaths.EnsureDirectoryForFile(path_New);
                File.WriteAllText(path_New, "{}");

                Assert.Equal(path_New, Query.PartOIteration3RecordPath(path_TSD, PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance, out bool _));
                Assert.True(File.Exists(path_Legacy));
            }
            finally
            {
                Directory.Delete(directory_Root, true);
            }
        }

        [Fact]
        public void ANewIteration3AgainstALegacyReference_WritesIntoIteration3BeneathTheFlatFolder()
        {
            PartOIteration3Paths paths = PartOIteration3Paths.Create(PartOIteration3Fixture.SimulationContext("C:\\out", "Flat1"), "C:\\out\\Flat1.tsd", PartOIteration3BehaviourMode.SelectedProduct);

            Assert.Equal("C:\\out\\Iteration3\\tas\\Flat1-It3BP.tpd", paths.Path_TPD);
            Assert.Equal("C:\\out\\Flat1.tsd", paths.Path_TSD_ReferenceA);
            Assert.Equal("C:\\out\\Flat1-TM59.txt", paths.Path_TM59Report_ReferenceA);
        }

        // ---- diagnostics filing ----------------------------------------------------------------------------------

        [Fact]
        public void TimingFiles_MoveFromTasToDiagnostics_InTheLayoutOnly()
        {
            string directory_Root = TempRoot();

            try
            {
                PartOOutputPaths partOOutputPaths = PartOOutputPaths.Create(directory_Root, PartOOutputCase.Iteration3);
                partOOutputPaths.CreateDirectories();

                string path_Timing = Path.Combine(partOOutputPaths.Directory_Tas, "Flat1-It3BMG.timing.csv");
                string path_RouteTiming = Path.Combine(partOOutputPaths.Directory_Tas, "Flat1-It3BMG.route.timing.csv");
                string path_TSD = Path.Combine(partOOutputPaths.Directory_Tas, "Flat1-It3BMG.tsd");
                File.WriteAllText(path_Timing, "step,milliseconds");
                File.WriteAllText(path_RouteTiming, "step,milliseconds");
                File.WriteAllText(path_TSD, "tsd");

                PartOOutputPaths.FileDiagnostics(partOOutputPaths.Directory_Tas);

                Assert.False(File.Exists(path_Timing));
                Assert.False(File.Exists(path_RouteTiming));
                Assert.True(File.Exists(Path.Combine(partOOutputPaths.Directory_Diagnostics, "Flat1-It3BMG.timing.csv")));
                Assert.True(File.Exists(Path.Combine(partOOutputPaths.Directory_Diagnostics, "Flat1-It3BMG.route.timing.csv")));
                Assert.True(File.Exists(path_TSD));

                //A legacy flat folder is left exactly as it is.
                string directory_Legacy = Path.Combine(directory_Root, "legacy");
                Directory.CreateDirectory(directory_Legacy);
                string path_Timing_Legacy = Path.Combine(directory_Legacy, "Flat1.timing.csv");
                File.WriteAllText(path_Timing_Legacy, "step,milliseconds");

                Assert.Empty(PartOOutputPaths.FileDiagnostics(directory_Legacy));
                Assert.True(File.Exists(path_Timing_Legacy));
            }
            finally
            {
                Directory.Delete(directory_Root, true);
            }
        }

        private static string TempRoot()
        {
            return Path.Combine(Path.GetTempPath(), "SAM_PartOOutput_" + Guid.NewGuid().ToString("N"));
        }
    }
}
