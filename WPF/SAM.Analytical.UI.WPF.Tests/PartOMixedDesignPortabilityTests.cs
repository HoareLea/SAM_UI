// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.UI;
using System;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// Whether a model's <c>.partomixed.json</c> still describes its run after the project folder is moved or copied,
    /// opened somewhere else, or the model is saved under another name.
    /// <para>
    /// Before this the sidecar persisted the absolute <c>Path_TSD</c>, <c>Path_RunModel</c> and <c>Path_TPD</c> of the
    /// run (and each screening's <c>Path_TSD</c>). After a folder move the old paths were gone and the run read STALE
    /// although its files sat beside the model; after a copy with the original left in place the copy read, and
    /// reviewed, the ORIGINAL's run. The paths are now written relative to the sidecar's folder, an older sidecar's
    /// absolute paths are still read, and no workstation path is written where a relative form exists.
    /// </para>
    /// </summary>
    public class PartOMixedDesignPortabilityTests : IDisposable
    {
        private readonly string directory = Path.Combine(Path.GetTempPath(), "SAM_PartOMixedPortable_" + Guid.NewGuid().ToString("N"));

        public PartOMixedDesignPortabilityTests()
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

        /// <summary>
        /// A project folder as a real run leaves it: the model, and beside it <c>MixedDesign/tas</c> holding the
        /// results, the run model and the TPD - with the state written beside the model.
        /// </summary>
        private (AnalyticalModel Baseline, string Path_Model, PartOMixedRunEvidence Evidence) Project(string folder)
        {
            Directory.CreateDirectory(folder);
            string path_Model = Path.Combine(folder, "Block.sam");

            AnalyticalModel baseline = PartOMixedDesignFixture.WithStrategies(PartOMixedDesignFixture.Baseline(3), PartOMixedDesignFixture.Natural);

            PartOMixedDesignFixture.FakeSimulator fakeSimulator = new();
            PartOMixedRunEvidence evidence = Modify.BuildAndRunPartOMixedDesign(baseline, false, null, PartOMixedDesignFixture.Context(), CancellationToken.None, out _, fakeSimulator.Simulate);
            Assert.NotNull(evidence);

            //Put the run's files where a real run puts them: under the model's own folder.
            string folder_Tas = Path.Combine(folder, "MixedDesign", "tas");
            Directory.CreateDirectory(folder_Tas);

            string path_TSD = Path.Combine(folder_Tas, "Block.tsd");
            File.Copy(evidence.Path_TSD, path_TSD, true);
            FileInfo fileInfo = new(path_TSD);
            evidence.Path_TSD = path_TSD;
            evidence.Length_TSD = fileInfo.Length;
            evidence.Timestamp_TSD = fileInfo.LastWriteTimeUtc.Ticks;

            evidence.Path_RunModel = Path.Combine(folder_Tas, "Block_Mixed.sam");
            File.WriteAllText(evidence.Path_RunModel, "{}");
            evidence.Path_TPD = Path.Combine(folder_Tas, "Block_Mixed.tpd");
            File.WriteAllText(evidence.Path_TPD, "tpd");

            return (baseline, path_Model, evidence);
        }

        private static PartOMixedDesignSession Session(AnalyticalModel baseline, string path_Model, PartOMixedRunEvidence evidence, bool write)
        {
            PartOMixedDesignSession partOMixedDesignSession = new(baseline, path_Model, null, null);
            partOMixedDesignSession.SimulationCaseKey = PartOMixedDesignFixture.CaseKey;
            partOMixedDesignSession.ApplyScreening([PartOMixedDesignSessionTests.Evidence(partOMixedDesignSession, PartOScreeningStrategy.Natural, (partOMixedDesignSession.Rows[0], PartODwellingOutcome.Pass))]);

            //The screening's own results file, beside the model as a real one is.
            PartOScreeningEvidence screening = partOMixedDesignSession.State.ScreeningEvidence(PartOScreeningStrategy.Natural);
            string path_Screening = Path.Combine(Path.GetDirectoryName(evidence.Path_TSD), "Block_Natural.tsd");
            File.WriteAllText(path_Screening, "screening");
            screening.Path_TSD = path_Screening;

            partOMixedDesignSession.ApplyFinal(evidence);
            Assert.True(partOMixedDesignSession.FinalCurrent, partOMixedDesignSession.FinalStale);

            if (write)
            {
                Assert.True(partOMixedDesignSession.State.Write(PartOMixedDesignState.Path_State(path_Model), out string note), note);
            }

            return partOMixedDesignSession;
        }

        private static PartOMixedDesignSession Reopen(AnalyticalModel baseline, string path_Model)
        {
            PartOMixedDesignSession result = new(new AnalyticalModel(baseline.ToJsonObject()), path_Model, null, PartOMixedDesignState.Read(PartOMixedDesignState.Path_State(path_Model)));
            result.SimulationCaseKey = PartOMixedDesignFixture.CaseKey;

            return result;
        }

        private static void CopyFolder(string from, string to)
        {
            foreach (string path in Directory.GetFiles(from, "*", SearchOption.AllDirectories))
            {
                string path_To = Path.Combine(to, Path.GetRelativePath(from, path));
                Directory.CreateDirectory(Path.GetDirectoryName(path_To));

                //File.Copy keeps the write time, as Explorer and robocopy do - the results file's lineage is its length and write time.
                File.Copy(path, path_To, true);
            }
        }

        [Fact]
        public void ANewSidecar_PersistsNoWorkstationPath()
        {
            string folder = Path.Combine(directory, "A");
            (AnalyticalModel baseline, string path_Model, PartOMixedRunEvidence evidence) = Project(folder);
            Session(baseline, path_Model, evidence, true);

            string text = File.ReadAllText(PartOMixedDesignState.Path_State(path_Model));

            Assert.DoesNotContain(directory.Replace("\\", "\\\\"), text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(Path.GetTempPath().Replace("\\", "\\\\"), text, StringComparison.OrdinalIgnoreCase);

            JsonObject jsonObject = (JsonObject)JsonNode.Parse(text);
            JsonObject finalRun = (JsonObject)jsonObject["FinalRun"];
            Assert.Null((string)finalRun["Path_TSD"]);
            Assert.Null((string)finalRun["Path_RunModel"]);
            Assert.Null((string)finalRun["Path_TPD"]);
            Assert.Equal("MixedDesign/tas/Block.tsd", (string)finalRun["Locator_TSD"]);
            Assert.Equal("MixedDesign/tas/Block_Mixed.sam", (string)finalRun["Locator_RunModel"]);
            Assert.Equal("MixedDesign/tas/Block_Mixed.tpd", (string)finalRun["Locator_TPD"]);

            JsonObject screening = (JsonObject)((JsonArray)jsonObject["Screening"]).Single();
            Assert.Null((string)screening["Path_TSD"]);
            Assert.Equal("MixedDesign/tas/Block_Natural.tsd", (string)screening["Locator_TSD"]);
        }

        [Fact]
        public void TheSameFolder_ReadsBackTheSamePaths()
        {
            string folder = Path.Combine(directory, "A");
            (AnalyticalModel baseline, string path_Model, PartOMixedRunEvidence evidence) = Project(folder);
            Session(baseline, path_Model, evidence, true);

            PartOMixedDesignSession reopened = Reopen(baseline, path_Model);

            Assert.True(reopened.FinalCurrent, reopened.FinalStale);
            Assert.Equal(evidence.Path_TSD, reopened.State.FinalRun.Path_TSD, ignoreCase: true);
            Assert.Equal(evidence.Path_RunModel, reopened.State.FinalRun.Path_RunModel, ignoreCase: true);
            Assert.Equal(evidence.Path_TPD, reopened.State.FinalRun.Path_TPD, ignoreCase: true);
            Assert.Equal("PASS", reopened.Rows[0].Screening_Natural);
        }

        [Fact]
        public void AMovedProjectFolder_StillHasItsRun()
        {
            string folder_From = Path.Combine(directory, "A");
            (AnalyticalModel baseline, string path_Model, PartOMixedRunEvidence evidence) = Project(folder_From);
            Session(baseline, path_Model, evidence, true);

            string folder_To = Path.Combine(directory, "Elsewhere", "B");
            Directory.CreateDirectory(Path.GetDirectoryName(folder_To));
            Directory.Move(folder_From, folder_To);
            string path_Model_Moved = Path.Combine(folder_To, "Block.sam");

            PartOMixedDesignSession reopened = Reopen(baseline, path_Model_Moved);

            Assert.True(reopened.FinalCurrent, reopened.FinalStale);
            Assert.StartsWith(folder_To, reopened.State.FinalRun.Path_TSD, StringComparison.OrdinalIgnoreCase);
            Assert.StartsWith(folder_To, reopened.State.FinalRun.Path_RunModel, StringComparison.OrdinalIgnoreCase);
            Assert.True(File.Exists(reopened.State.FinalRun.Path_RunModel));
            Assert.Equal("PASS", reopened.Rows[0].Screening_Natural);
        }

        [Fact]
        public void ACopiedFolder_ReadsItsOwnRun_NeverTheOriginals()
        {
            string folder_From = Path.Combine(directory, "A");
            (AnalyticalModel baseline, string path_Model, PartOMixedRunEvidence evidence) = Project(folder_From);
            Session(baseline, path_Model, evidence, true);

            string folder_Copy = Path.Combine(directory, "Copy");
            CopyFolder(folder_From, folder_Copy);

            //The original stays in place, unchanged - the case where an absolute path used to keep "working".
            PartOMixedDesignSession reopened = Reopen(baseline, Path.Combine(folder_Copy, "Block.sam"));

            Assert.True(reopened.FinalCurrent, reopened.FinalStale);
            Assert.StartsWith(folder_Copy, reopened.State.FinalRun.Path_TSD, StringComparison.OrdinalIgnoreCase);
            Assert.StartsWith(folder_Copy, reopened.State.FinalRun.Path_RunModel, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(folder_From, reopened.State.FinalRun.Path_TSD, StringComparison.OrdinalIgnoreCase);

            //And it stays its own: the copy's results file rewritten makes the COPY stale, not the original.
            File.AppendAllText(reopened.State.FinalRun.Path_TSD, "rewritten");
            Assert.False(Reopen(baseline, Path.Combine(folder_Copy, "Block.sam")).FinalCurrent);
            Assert.True(Reopen(baseline, path_Model).FinalCurrent);
        }

        [Fact]
        public void AMovedFolderWhoseResultsWereNotMoved_IsStale_NotSilentlyPointedElsewhere()
        {
            string folder_From = Path.Combine(directory, "A");
            (AnalyticalModel baseline, string path_Model, PartOMixedRunEvidence evidence) = Project(folder_From);
            Session(baseline, path_Model, evidence, true);

            //Only the sidecar travels beside its model; the run's folder does not.
            string folder_To = Path.Combine(directory, "B");
            Directory.CreateDirectory(folder_To);
            File.Copy(PartOMixedDesignState.Path_State(path_Model), PartOMixedDesignState.Path_State(Path.Combine(folder_To, "Block.sam")), true);

            PartOMixedDesignSession reopened = Reopen(baseline, Path.Combine(folder_To, "Block.sam"));

            Assert.False(reopened.FinalCurrent);
            Assert.Contains("no longer there", reopened.FinalStale);
        }

        [Fact]
        public void ALegacySidecarWithAbsolutePaths_IsStillRead()
        {
            string folder = Path.Combine(directory, "A");
            (AnalyticalModel baseline, string path_Model, PartOMixedRunEvidence evidence) = Project(folder);
            PartOMixedDesignSession written = Session(baseline, path_Model, evidence, false);

            //The sidecar exactly as an earlier build wrote it: absolute Path_TSD / Path_RunModel / Path_TPD, no locator.
            JsonObject jsonObject = written.State.ToJsonObject();
            JsonObject finalRun = (JsonObject)jsonObject["FinalRun"];
            Assert.Equal(evidence.Path_TSD, (string)finalRun["Path_TSD"]);
            Assert.Null(finalRun["Locator_TSD"]);
            File.WriteAllText(PartOMixedDesignState.Path_State(path_Model), jsonObject.ToJsonString());

            PartOMixedDesignSession reopened = Reopen(baseline, path_Model);

            Assert.True(reopened.FinalCurrent, reopened.FinalStale);
            Assert.Equal(evidence.Path_TSD, reopened.State.FinalRun.Path_TSD);
            Assert.Equal(evidence.Path_RunModel, reopened.State.FinalRun.Path_RunModel);
            Assert.Equal("PASS", reopened.Rows[0].Screening_Natural);

            //Saving it again upgrades it: relative from then on, no absolute path left.
            Assert.True(reopened.State.Write(PartOMixedDesignState.Path_State(path_Model), out string note), note);
            string text = File.ReadAllText(PartOMixedDesignState.Path_State(path_Model));
            Assert.DoesNotContain(directory.Replace("\\", "\\\\"), text, StringComparison.OrdinalIgnoreCase);
            Assert.True(Reopen(baseline, path_Model).FinalCurrent);
        }

        [Fact]
        public void APathWithNoRelativeForm_IsKeptAbsolute_ButNeverLosesTheResult()
        {
            //Another drive has no relative form; the evidence must still round-trip rather than be dropped.
            string drive = string.Equals(Path.GetPathRoot(directory), @"Z:\", StringComparison.OrdinalIgnoreCase) ? "Y" : "Z";
            string path_TSD = drive + @":\NotHere\Run.tsd";
            string path_RunModel = drive + @":\NotHere\Run.sam";

            PartOMixedDesignState state = new() { FinalRun = new PartOMixedRunEvidence { Path_TSD = path_TSD, Path_RunModel = path_RunModel } };
            string path_State = Path.Combine(directory, "Block.partomixed.json");
            Assert.True(state.Write(path_State, out string note), note);

            JsonObject finalRun = (JsonObject)((JsonObject)JsonNode.Parse(File.ReadAllText(path_State)))["FinalRun"];
            Assert.Equal(path_TSD, (string)finalRun["Path_TSD"]);
            Assert.Null(finalRun["Locator_TSD"]);

            PartOMixedDesignState read = PartOMixedDesignState.Read(path_State);
            Assert.Equal(path_TSD, read.FinalRun.Path_TSD);
            Assert.Equal(path_RunModel, read.FinalRun.Path_RunModel);
        }

        [Fact]
        public void AFileOutsideTheSidecarsFolder_StaysAbsolute_SoACopyIsNeverTiedToItsOriginal()
        {
            //A legacy sidecar copied with its folder still names the ORIGINAL's run. Re-saving it must not turn that into
            //a relative path out of the copy and into the original - it stays what it was, an absolute path.
            string folder_From = Path.Combine(directory, "A");
            (AnalyticalModel baseline, string path_Model, PartOMixedRunEvidence evidence) = Project(folder_From);

            string folder_Copy = Path.Combine(directory, "Copy");
            Directory.CreateDirectory(folder_Copy);
            string path_State_Copy = PartOMixedDesignState.Path_State(Path.Combine(folder_Copy, "Block.sam"));

            PartOMixedDesignState state = new() { FinalRun = evidence };
            Assert.True(state.Write(path_State_Copy, out string note), note);

            JsonObject finalRun = (JsonObject)((JsonObject)JsonNode.Parse(File.ReadAllText(path_State_Copy)))["FinalRun"];
            Assert.Equal(evidence.Path_TSD, (string)finalRun["Path_TSD"]);
            Assert.Null(finalRun["Locator_TSD"]);
        }

        [Fact]
        public void SavingAs_ANewName_DoesNotInheritTheRunEvidence()
        {
            string folder = Path.Combine(directory, "A");
            (AnalyticalModel baseline, string path_Model, PartOMixedRunEvidence evidence) = Project(folder);
            Session(baseline, path_Model, evidence, true);

            //The sidecar is named from the model, so a model saved under another name starts with no saved evidence -
            //it fails closed (no run shown) rather than showing a run that was made for a different file.
            string path_Model_SavedAs = Path.Combine(folder, "Block copy.sam");
            Assert.NotEqual(PartOMixedDesignState.Path_State(path_Model), PartOMixedDesignState.Path_State(path_Model_SavedAs));
            Assert.Null(PartOMixedDesignState.Read(PartOMixedDesignState.Path_State(path_Model_SavedAs)));

            PartOMixedDesignSession reopened = Reopen(baseline, path_Model_SavedAs);
            Assert.False(reopened.FinalCurrent);
        }
    }
}
