// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using SAM.Analytical.UI;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// The Part O output-folder structure: the person chooses the root, and every case writes into its own
    /// <c>&lt;root&gt;/&lt;case&gt;/tas|reports|diagnostics</c> folders through the one resolver,
    /// <see cref="PartOOutputPaths"/>. A folder is part of the layout only where SAM created it (its marker); a folder
    /// that is merely NAMED like one is a legacy flat folder, and legacy flat folders stay readable as saved.
    /// </summary>
    public class PartOOutputFolderTests : IDisposable
    {
        private const string Root = "C:\\PartO";

        private readonly string directory_Temp = Path.Combine(Path.GetTempPath(), "SAM_PartOOutput_" + Guid.NewGuid().ToString("N"));

        public PartOOutputFolderTests()
        {
            Directory.CreateDirectory(directory_Temp);
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(directory_Temp, true);
            }
            catch (IOException)
            {
            }
        }

        /// <summary>A case's folders beneath a root, created by SAM - marker included.</summary>
        private static PartOOutputPaths Created(string directory_Root, PartOOutputCase partOOutputCase)
        {
            PartOOutputPaths partOOutputPaths = PartOOutputPaths.Create(directory_Root, partOOutputCase);
            partOOutputPaths.CreateDirectories();

            return partOOutputPaths;
        }

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

            //Once SAM has created it, it is recognised from any of its three folders as the same case and root.
            PartOOutputPaths partOOutputPaths_Created = Created(directory_Temp, partOOutputCase);
            Assert.True(File.Exists(partOOutputPaths_Created.Path_Marker));

            foreach (string directory in new[] { partOOutputPaths_Created.Directory_Tas, partOOutputPaths_Created.Directory_Reports, partOOutputPaths_Created.Directory_Diagnostics })
            {
                PartOOutputPaths partOOutputPaths_Found = PartOOutputPaths.Find(directory);

                Assert.NotNull(partOOutputPaths_Found);
                Assert.Equal(partOOutputCase, partOOutputPaths_Found.Case);
                Assert.Equal(directory_Temp, partOOutputPaths_Found.Directory_Root);
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

        // ---- Adversarial: folders a person named like the layout ------------------------------------------------

        /// <summary>
        /// Whatever the chosen folder is called - a case name, <c>tas</c>, <c>reports</c>, <c>diagnostics</c>, or a
        /// whole <c>Iteration2\tas</c> made by hand - it is the root EXACTLY: never mistaken for the layout, and the
        /// case folder is created directly inside it, neither above nor deeper.
        /// </summary>
        [Theory]
        [InlineData("Iteration1a")]
        [InlineData("Iteration3")]
        [InlineData("MixedDesign")]
        [InlineData("tas")]
        [InlineData("reports")]
        [InlineData("diagnostics")]
        [InlineData("Iteration2\\tas")]
        [InlineData("Iteration3\\reports")]
        [InlineData("Iteration1a\\diagnostics")]
        [InlineData("Iteration1a\\Iteration3\\tas")]
        public void AChosenFolderNamedLikeTheLayout_IsTheRootExactly(string name)
        {
            string directory_Chosen = Path.Combine(directory_Temp, name);
            Directory.CreateDirectory(directory_Chosen);

            //Not the layout: SAM never created it.
            Assert.Null(PartOOutputPaths.Find(directory_Chosen));
            Assert.Equal(directory_Chosen, PartOOutputPaths.Root(directory_Chosen));

            foreach (PartOOutputCase partOOutputCase in Enum.GetValues(typeof(PartOOutputCase)))
            {
                PartOOutputPaths partOOutputPaths = Created(directory_Chosen, partOOutputCase);

                Assert.Equal(directory_Chosen, partOOutputPaths.Directory_Root);
                Assert.Equal(Path.Combine(directory_Chosen, PartOOutputPaths.Folder(partOOutputCase)), partOOutputPaths.Directory_Case);

                //Read back from the run's own results, the root is still the chosen folder - not its parent.
                Assert.Equal(directory_Chosen, PartOOutputPaths.Root(partOOutputPaths.Directory_Tas));
                Assert.Equal(directory_Chosen, PartOOutputPaths.Find(partOOutputPaths.Directory_Reports).Directory_Root);
            }

            //And the chosen folder itself is still not the layout after SAM created case folders inside it.
            Assert.Null(PartOOutputPaths.Find(directory_Chosen));
        }

        /// <summary>
        /// A legacy run a person wrote into a hand-made <c>...\Iteration2\tas</c> (or <c>Iteration1a\tas</c>) is a flat
        /// folder: its report stays beside its results, nothing is filed out of it, a run started from it is rooted AT
        /// it rather than above it, and it carries no Iteration 1a qualifier.
        /// </summary>
        [Theory]
        [InlineData("Iteration2")]
        [InlineData("Iteration1a")]
        public void AHandMadeFolderNamedLikeACaseFolder_IsALegacyFlatFolder(string folder_Case)
        {
            string directory_Legacy = Path.Combine(directory_Temp, "Project", folder_Case, "tas");
            Directory.CreateDirectory(directory_Legacy);
            Directory.CreateDirectory(Path.Combine(directory_Temp, "Project", folder_Case, "reports"));

            string path_TSD = Path.Combine(directory_Legacy, "Flat1.tsd");
            string path_Timing = Path.Combine(directory_Legacy, "Flat1.timing.csv");
            File.WriteAllText(path_Timing, "step,milliseconds");

            Assert.Null(PartOOutputPaths.FindForFile(path_TSD));
            Assert.Equal(Path.Combine(directory_Legacy, "Flat1-TM59.txt"), Query.Path_TM59Report(path_TSD));
            Assert.Equal(directory_Legacy, PartOOutputPaths.Directory_Diagnostics_ForFile(path_TSD));

            Assert.Empty(PartOOutputPaths.FileDiagnostics(directory_Legacy));
            Assert.True(File.Exists(path_Timing));

            PartOIteration3Paths paths = PartOIteration3Paths.Create(PartOIteration3Fixture.SimulationContext(directory_Legacy, "Flat1"), path_TSD, PartOIteration3BehaviourMode.Parity);
            Assert.Equal(Path.Combine(directory_Legacy, "Iteration3", "tas"), paths.OutputDirectory);
            Assert.Equal("Flat1-It3B", paths.ProjectName_CandidateB);
            Assert.Equal(Path.Combine(directory_Legacy, "Iteration3", "reports", "Flat1-Iteration3-B0.json"), paths.Path_Record);
        }

        [Fact]
        public void AMarkerThatIsMissing_ForAnotherCase_UnreadableOrForeign_IsNotTheLayout()
        {
            PartOOutputPaths partOOutputPaths = Created(directory_Temp, PartOOutputCase.Iteration2);
            Assert.NotNull(PartOOutputPaths.Find(partOOutputPaths.Directory_Tas));

            //A case folder renamed by hand keeps the marker of the case it was.
            string directory_Renamed = Path.Combine(directory_Temp, "Iteration1a");
            Directory.Move(partOOutputPaths.Directory_Case, directory_Renamed);
            Assert.Null(PartOOutputPaths.Find(Path.Combine(directory_Renamed, "tas")));

            PartOOutputPaths partOOutputPaths_3 = Created(directory_Temp, PartOOutputCase.Iteration3);

            File.WriteAllText(partOOutputPaths_3.Path_Marker, "not json {");
            Assert.Null(PartOOutputPaths.Find(partOOutputPaths_3.Directory_Tas));

            File.WriteAllText(partOOutputPaths_3.Path_Marker, "{\"Schema\":\"something else\",\"Case\":\"Iteration3\"}");
            Assert.Null(PartOOutputPaths.Find(partOOutputPaths_3.Directory_Tas));

            File.Delete(partOOutputPaths_3.Path_Marker);
            Assert.Null(PartOOutputPaths.Find(partOOutputPaths_3.Directory_Tas));

            //Recreated, it is recognised again; an existing marker is never rewritten.
            partOOutputPaths_3.CreateDirectories();
            Assert.NotNull(PartOOutputPaths.Find(partOOutputPaths_3.Directory_Tas));
        }

        [Fact]
        public void TheLayoutIsRecognised_OnlyAtItsOwnDepth_AndRootStepsOutOfExactlyOneCaseFolder()
        {
            PartOOutputPaths partOOutputPaths = Created(directory_Temp, PartOOutputCase.Iteration2);

            //The case folder and the root are not themselves inside the layout; a folder below tas is not either.
            Assert.Null(PartOOutputPaths.Find(partOOutputPaths.Directory_Case));
            Assert.Null(PartOOutputPaths.Find(directory_Temp));
            string directory_Below = Path.Combine(partOOutputPaths.Directory_Tas, "tas");
            Directory.CreateDirectory(directory_Below);
            Assert.Null(PartOOutputPaths.Find(directory_Below));

            //A person who picks a SAM tas folder as the root gets a layout inside it - rooted there, not above.
            PartOOutputPaths partOOutputPaths_Nested = Created(partOOutputPaths.Directory_Tas, PartOOutputCase.Iteration1a);
            Assert.Equal(partOOutputPaths.Directory_Tas, partOOutputPaths_Nested.Directory_Root);
            Assert.Equal(partOOutputPaths.Directory_Tas, PartOOutputPaths.Root(partOOutputPaths_Nested.Directory_Tas));

            //From an existing run's results, the root is exactly one case folder up.
            Assert.Equal(directory_Temp, PartOOutputPaths.Root(partOOutputPaths.Directory_Tas));
            Assert.Equal(directory_Temp, PartOOutputPaths.Root(partOOutputPaths.Directory_Tas + "\\"));
        }

        // ---- 2-4. TAS files together; reports and diagnostics apart ----------------------------------------------

        [Fact]
        public void Iteration3_TasFilesAreTogether_ReportsAndDiagnosticsInTheirOwnFolders()
        {
            PartOOutputPaths partOOutputPaths_2 = Created(directory_Temp, PartOOutputCase.Iteration2);
            string path_TSD_ReferenceA = Path.Combine(partOOutputPaths_2.Directory_Tas, "Flat1.tsd");

            PartOIteration3Paths paths = PartOIteration3Paths.Create(PartOIteration3Fixture.SimulationContext(partOOutputPaths_2.Directory_Tas, "Flat1"), path_TSD_ReferenceA, PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance);

            string tas = Path.Combine(directory_Temp, "Iteration3", "tas") + "\\";
            string reports = Path.Combine(directory_Temp, "Iteration3", "reports") + "\\";
            string diagnostics = Path.Combine(directory_Temp, "Iteration3", "diagnostics") + "\\";

            //Folders do not exist before the run creates them; derivation does not need them.
            paths.OutputPaths.CreateDirectories();

            Assert.Equal(Path.Combine(directory_Temp, "Iteration3", "tas"), paths.OutputDirectory);
            Assert.Equal(tas + "Flat1-It3BMG.tbd", paths.Path_TBD_ThermalSource);
            Assert.Equal(tas + "Flat1-It3BMG.tsd", paths.Path_TSD_ThermalSource);
            Assert.Equal(tas + "Flat1-It3BMG.tpd", paths.Path_TPD);
            Assert.Equal(tas + "Flat1-It3BMG-Bridge.tbd", paths.Path_TBD_Bridge);
            Assert.Equal(tas + "Flat1-It3BMG-Bridge.tsd", paths.Path_TSD_Bridge);
            Assert.Equal(tas + "Flat1-It3BMG-Bridge.sam", paths.Path_Model_CandidateB);

            Assert.Equal(reports + "Flat1-It3BMG-Bridge-TM59.txt", Query.Path_TM59Report(paths.Path_TSD_Bridge));
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
            PartOOutputPaths partOOutputPaths = Created(directory_Temp, partOOutputCase);
            string path_TSD = Path.Combine(partOOutputPaths.Directory_Tas, "Flat1.tsd");

            Assert.Equal(Path.Combine(partOOutputPaths.Directory_Reports, "Flat1-TM59.txt"), Query.Path_TM59Report(path_TSD));
            Assert.Equal(Path.Combine(partOOutputPaths.Directory_Tas, "Flat1.sam"), Query.Path_PartORunModel(path_TSD));
            Assert.Equal(Path.Combine(partOOutputPaths.Directory_Tas, "Flat1.partorun.json"), UI.PartORunResume.Path_Resume(path_TSD));
        }

        // ---- 5. No collision -------------------------------------------------------------------------------------

        [Fact]
        public void TheSameFileName_ResolvesDifferently_In1bAnd2_TasReportsAndDiagnostics()
        {
            PartOOutputPaths paths_1b = Created(directory_Temp, PartOOutputCase.Iteration1b);
            PartOOutputPaths paths_2 = Created(directory_Temp, PartOOutputCase.Iteration2);

            string path_TSD_1b = Path.Combine(paths_1b.Directory_Tas, "Flat1.tsd");
            string path_TSD_2 = Path.Combine(paths_2.Directory_Tas, "Flat1.tsd");

            Assert.NotEqual(path_TSD_1b, path_TSD_2, StringComparer.OrdinalIgnoreCase);
            Assert.NotEqual(Query.Path_TM59Report(path_TSD_1b), Query.Path_TM59Report(path_TSD_2), StringComparer.OrdinalIgnoreCase);
            Assert.NotEqual(Query.Path_PartORunModel(path_TSD_1b), Query.Path_PartORunModel(path_TSD_2), StringComparer.OrdinalIgnoreCase);
            Assert.NotEqual(PartOOutputPaths.Directory_Diagnostics_ForFile(path_TSD_1b), PartOOutputPaths.Directory_Diagnostics_ForFile(path_TSD_2), StringComparer.OrdinalIgnoreCase);

            Assert.Equal(Path.Combine(directory_Temp, "Iteration1b", "tas", "Flat1.tsd"), path_TSD_1b);
            Assert.Equal(Path.Combine(directory_Temp, "Iteration2", "tas", "Flat1.tsd"), path_TSD_2);
        }

        [Fact]
        public void EveryCase_ResolvesTheSameFileNameToADifferentPath()
        {
            HashSet<string> paths = new(StringComparer.OrdinalIgnoreCase);

            foreach (PartOOutputCase partOOutputCase in Enum.GetValues(typeof(PartOOutputCase)))
            {
                PartOOutputPaths partOOutputPaths = PartOOutputPaths.Create(Root, partOOutputCase);

                Assert.True(paths.Add(Path.Combine(partOOutputPaths.Directory_Tas, "Flat1.tsd")));
                Assert.True(paths.Add(Path.Combine(partOOutputPaths.Directory_Reports, "Flat1-TM59.txt")));
                Assert.True(paths.Add(Path.Combine(partOOutputPaths.Directory_Diagnostics, "Flat1.timing.csv")));
            }
        }

        /// <summary>
        /// Iteration 3 accepts an Iteration 1a reference and an Iteration 2 reference of the same model - same project
        /// name, same results file name. Every file each pairing owns (TAS, bridge, model, TM59, histories, record,
        /// review) is distinct, the 1a one carrying <c>-It1a</c> throughout; each keeps its own reference's path and
        /// report; and a review holding only its reference's results derives its own record.
        /// </summary>
        [Theory]
        [InlineData(PartOIteration3BehaviourMode.Parity)]
        [InlineData(PartOIteration3BehaviourMode.SelectedProduct)]
        [InlineData(PartOIteration3BehaviourMode.SelectedProductCooling)]
        [InlineData(PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance)]
        public void Iteration3_AgainstA1aAndA2ReferenceOfTheSameModel_NeverShareAFile(PartOIteration3BehaviourMode partOIteration3BehaviourMode)
        {
            PartOOutputPaths paths_1a = Created(directory_Temp, PartOOutputCase.Iteration1a);
            PartOOutputPaths paths_2 = Created(directory_Temp, PartOOutputCase.Iteration2);

            string path_TSD_1a = Path.Combine(paths_1a.Directory_Tas, "Flat1.tsd");
            string path_TSD_2 = Path.Combine(paths_2.Directory_Tas, "Flat1.tsd");

            PartOIteration3Paths paths_It3_1a = PartOIteration3Paths.Create(PartOIteration3Fixture.SimulationContext(paths_1a.Directory_Tas, "Flat1"), path_TSD_1a, partOIteration3BehaviourMode);
            PartOIteration3Paths paths_It3_2 = PartOIteration3Paths.Create(PartOIteration3Fixture.SimulationContext(paths_2.Directory_Tas, "Flat1"), path_TSD_2, partOIteration3BehaviourMode);

            List<string> Owned(PartOIteration3Paths paths) =>
            [
                .. paths.Paths_CandidateB,
                PartOIteration3Paths.Path_Report_ForRecord(paths.Path_Record),
                PartOIteration3Paths.Path_Report_ForRecord(paths.Path_Record, "json"),
                paths.Path_TBD_ThermalSource.Replace(".tbd", PartOOutputPaths.Suffix_Timing),
            ];

            List<string> owned_1a = Owned(paths_It3_1a);
            List<string> owned_2 = Owned(paths_It3_2);

            foreach (string path in owned_1a)
            {
                Assert.DoesNotContain(path, owned_2, StringComparer.OrdinalIgnoreCase);
                Assert.Contains("Flat1-It1a-", Path.GetFileName(path));
                Assert.StartsWith(Path.Combine(directory_Temp, "Iteration3"), path, StringComparison.OrdinalIgnoreCase);
            }

            //Iteration 2 - the ordinary reference - keeps the unqualified names.
            foreach (string path in owned_2)
            {
                Assert.DoesNotContain("-It1a", Path.GetFileName(path));
            }

            Assert.StartsWith("Flat1-It1a-It3B", paths_It3_1a.ProjectName_CandidateB);
            Assert.Equal("Flat1", paths_It3_1a.ProjectName_ReferenceA);
            Assert.Equal(0, PartOSimulationContext.Iteration_ProjectName(paths_It3_1a.ProjectName_CandidateB));

            //Each keeps its OWN reference - the real cross-case path and that case's own report.
            Assert.Equal(path_TSD_1a, paths_It3_1a.Path_TSD_ReferenceA);
            Assert.Equal(Path.Combine(paths_1a.Directory_Reports, "Flat1-TM59.txt"), paths_It3_1a.Path_TM59Report_ReferenceA);
            Assert.Equal(Path.Combine(paths_2.Directory_Reports, "Flat1-TM59.txt"), paths_It3_2.Path_TM59Report_ReferenceA);

            //Reopened from each reference's results alone, each derives its own record.
            Assert.Equal(paths_It3_1a.Path_Record, PartOIteration3Paths.Path_Record_ForResults(path_TSD_1a, partOIteration3BehaviourMode));
            Assert.Equal(paths_It3_2.Path_Record, PartOIteration3Paths.Path_Record_ForResults(path_TSD_2, partOIteration3BehaviourMode));
            Assert.Equal(Path.Combine(directory_Temp, "Iteration3", "reports", "Flat1-It1a-Iteration3-" + PartOIteration3Paths.Tag(partOIteration3BehaviourMode) + ".json"), paths_It3_1a.Path_Record);
        }

        [Fact]
        public void Iteration2B_WritesIntoItsOwnFolder_NotItsBaselines_AndContinuesThere()
        {
            PartOOutputPaths paths_2 = Created(directory_Temp, PartOOutputCase.Iteration2);
            PartOSimulationContext partOSimulationContext_Baseline = PartOIteration3Fixture.SimulationContext(paths_2.Directory_Tas, "Flat1");

            //As OptimisePartOTM59 resolves it: the baseline's root, then the 2B case.
            PartOOutputPaths paths_2B = Created(PartOOutputPaths.Root(partOSimulationContext_Baseline.OutputDirectory), PartOOutputCase.Iteration2B);
            PartOSimulationContext partOSimulationContext_2B = paths_2B.SimulationContext(partOSimulationContext_Baseline);

            Assert.Equal(Path.Combine(directory_Temp, "Iteration2B", "tas"), partOSimulationContext_2B.OutputDirectory);
            Assert.Equal("Flat1", partOSimulationContext_2B.ProjectName);
            Assert.Same(partOSimulationContext_Baseline.WeatherData, partOSimulationContext_2B.WeatherData);

            //A second optimisation, from a round, stays in 2B's folder rather than nesting.
            Assert.Equal(Path.Combine(directory_Temp, "Iteration2B", "tas"), PartOOutputPaths.Create(PartOOutputPaths.Root(partOSimulationContext_2B.OutputDirectory), PartOOutputCase.Iteration2B).Directory_Tas);
        }

        [Fact]
        public void MixedDesign_WritesIntoItsOwnFolder_AndItsGuidanceHistoryIsADiagnostic()
        {
            Assert.Equal(Root + "\\MixedDesign\\tas", Create.PartOMixedOutputDirectory(new PartOSimulationCase { OutputDirectory = Root }));
            Assert.Null(Create.PartOMixedOutputDirectory(new PartOSimulationCase()));

            Created(directory_Temp, PartOOutputCase.MixedDesign);
            string directory = Create.PartOMixedOutputDirectory(new PartOSimulationCase { OutputDirectory = directory_Temp });

            Assert.Equal(Path.Combine(directory_Temp, "MixedDesign", "tas"), directory);
            Assert.Equal(Path.Combine(directory, "Model_Mixed_Bridge.tbd"), Query.Path_PartOMixedBridgeTBD(directory, "Model_Mixed"));
            Assert.Equal(Path.Combine(directory_Temp, "MixedDesign", "diagnostics"), PartOOutputPaths.Directory_Diagnostics_ForFile(Path.Combine(directory, "Model_Mixed.tpd")));
            Assert.Equal(Path.Combine(directory_Temp, "MixedDesign", "reports", "Model_Mixed_Bridge-TM59.txt"), Query.Path_TM59Report(Path.Combine(directory, "Model_Mixed_Bridge.tsd")));
        }

        // ---- 6-7. Iteration 3 keeps the Iteration 2 reference, and reopens from it alone -------------------------

        [Fact]
        public void Iteration3_ReferencesIteration2WhereItIs_AndNeverWritesThere()
        {
            PartOOutputPaths paths_2 = Created(directory_Temp, PartOOutputCase.Iteration2);
            string path_TSD_ReferenceA = Path.Combine(paths_2.Directory_Tas, "Flat1.tsd");

            PartOIteration3Paths paths = PartOIteration3Paths.Create(PartOIteration3Fixture.SimulationContext(paths_2.Directory_Tas, "Flat1"), path_TSD_ReferenceA, PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance);

            Assert.Equal(path_TSD_ReferenceA, paths.Path_TSD_ReferenceA);
            Assert.Equal(Path.Combine(paths_2.Directory_Reports, "Flat1-TM59.txt"), paths.Path_TM59Report_ReferenceA);

            foreach (string path in paths.Paths_CandidateB)
            {
                Assert.StartsWith(Path.Combine(directory_Temp, "Iteration3") + "\\", path, StringComparison.OrdinalIgnoreCase);
            }
        }

        [Fact]
        public void AReopenedIteration3_ResolvesTheSamePaths_FromTheReferenceResultsAlone()
        {
            PartOOutputPaths paths_2 = Created(directory_Temp, PartOOutputCase.Iteration2);
            string path_TSD_ReferenceA = Path.Combine(paths_2.Directory_Tas, "Flat1.tsd");

            PartOIteration3Paths paths_Run = PartOIteration3Paths.Create(PartOIteration3Fixture.SimulationContext(paths_2.Directory_Tas, "Flat1"), path_TSD_ReferenceA, PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance);

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
            PartOOutputPaths paths_2 = Created(directory_Temp, PartOOutputCase.Iteration2);
            string path_TSD_ReferenceA = Path.Combine(paths_2.Directory_Tas, "Flat1.tsd");
            PartOIteration3Paths paths = PartOIteration3Paths.Create(PartOIteration3Fixture.SimulationContext(paths_2.Directory_Tas, "Flat1"), path_TSD_ReferenceA, PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance);

            //What the run does at attempt start, then at its single exit.
            paths.OutputPaths.CreateDirectories();
            File.WriteAllText(paths.Path_Record, "{}");

            Assert.Equal(paths.Path_Record, Query.PartOIteration3RecordPath(path_TSD_ReferenceA, PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance, out bool legacy));
            Assert.False(legacy);
            Assert.Equal(Path.Combine(directory_Temp, "Iteration3", "reports", "Flat1-Iteration3-MG.json"), paths.Path_Record);
        }

        // ---- 8. Legacy flat folders stay readable ----------------------------------------------------------------

        [Fact]
        public void ALegacyFlatFolder_IsNotReadAsTheLayout_AndItsReportsStayBesideTheResults()
        {
            Assert.Null(PartOOutputPaths.Find("C:\\TasOut\\Project"));

            //Named like the layout, but not created by SAM (these do not even exist).
            Assert.Null(PartOOutputPaths.Find("C:\\TasOut\\Iteration2"));
            Assert.Null(PartOOutputPaths.Find("C:\\TasOut\\tas"));
            Assert.Null(PartOOutputPaths.Find("C:\\TasOut\\Iteration2\\tas"));

            Assert.Equal("C:\\TasOut\\Iteration2\\tas\\Flat1-TM59.txt", Query.Path_TM59Report("C:\\TasOut\\Iteration2\\tas\\Flat1.tsd"));
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
            string path_TSD = Path.Combine(directory_Temp, "Flat1.tsd");
            string path_Legacy = Path.Combine(directory_Temp, "Flat1-Iteration3-MG.json");
            File.WriteAllText(path_Legacy, "{}");

            Assert.Equal(path_Legacy, Query.PartOIteration3RecordPath(path_TSD, PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance, out bool legacy));
            Assert.False(legacy);

            //A new run against the same legacy results writes into Iteration 3's folder beneath the flat folder -
            //the legacy record is left untouched, and the new one is the one read from then on.
            string path_New = PartOIteration3Paths.Path_Record_ForResults(path_TSD, PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance);
            Assert.Equal(Path.Combine(directory_Temp, "Iteration3", "reports", "Flat1-Iteration3-MG.json"), path_New);

            Created(directory_Temp, PartOOutputCase.Iteration3);
            File.WriteAllText(path_New, "{}");

            Assert.Equal(path_New, Query.PartOIteration3RecordPath(path_TSD, PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance, out bool _));
            Assert.True(File.Exists(path_Legacy));
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
            PartOOutputPaths partOOutputPaths = Created(directory_Temp, PartOOutputCase.Iteration3);

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
            string directory_Legacy = Path.Combine(directory_Temp, "legacy");
            Directory.CreateDirectory(directory_Legacy);
            string path_Timing_Legacy = Path.Combine(directory_Legacy, "Flat1.timing.csv");
            File.WriteAllText(path_Timing_Legacy, "step,milliseconds");

            Assert.Empty(PartOOutputPaths.FileDiagnostics(directory_Legacy));
            Assert.True(File.Exists(path_Timing_Legacy));
        }
    }
}
