// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using SAM.Analytical.UI;
using SAM.Core;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// <b>One Part O run, one persisted model.</b>
    /// <para>
    /// A Part O run's authoritative model is the native <c>&lt;run&gt;.sam</c>. The TAS
    /// <c>WorkflowCalculator</c> also ends every run with a "Saving Model" step that writes the same model
    /// as plain JSON beside the TBD, so a Part O run was leaving the model twice - once as a compressed
    /// archive carrying the provenance a review is validated against, once as a very large text file
    /// carrying it too but never read. Keeping both defeats the reason the run artifact became <c>.sam</c>
    /// at all.
    /// </para>
    /// <para>
    /// <b>The seam.</b> <see cref="Modify.PersistPartORunModel"/> - the Part O orchestration step, not the
    /// workflow. Ordinary non-Part-O TAS runs in SAM are untouched and keep their
    /// <c>&lt;project&gt;.json</c>.
    /// </para>
    /// <para>
    /// <b>What is pinned here.</b> The successful run leaves a real <c>.sam</c> archive and no <c>.json</c>;
    /// the ordering never inverts, so a failed <c>.sam</c> write deletes nothing and the JSON survives as
    /// the only copy of the model; a JSON that cannot be deleted is a note and never a failed run; the file
    /// removed is exactly the one this run's TBD names and nothing else; and the removal changes nothing
    /// about reopening, restoring or validating the run.
    /// </para>
    /// </summary>
    public class PartORunModelPersistenceTests
    {
        private static AnalyticalModel Model(string name)
        {
            return new AnalyticalModel(name, null, null, null, new AdjacencyCluster(), null, null);
        }

        private static List<OverheatingScenario> Scenarios()
        {
            return [new OverheatingScenario(PartOAssessmentScope.Dwelling, Guid.NewGuid(), PartOIteration.BasePassive)];
        }

        /// <summary>Stamps a model exactly as <c>Modify.RunPartOSimulation</c> stamps the workflow's output.</summary>
        private static AnalyticalModel StampedModel(string path_TSD, List<OverheatingScenario> overheatingScenarios = null)
        {
            AnalyticalModel result = Model("run");

            result.SetValue(Analytical.AnalyticalModelParameter.OverheatingScenarios, new SAMCollection<OverheatingScenario>(overheatingScenarios ?? Scenarios()));
            result.SetValue(Analytical.AnalyticalModelParameter.SimulationResultProvenance, new SimulationResultProvenance(result, path_TSD));

            return result;
        }

        /// <summary>
        /// A run's own output folder, holding exactly what the TAS workflow leaves behind for one run: the
        /// TBD, the TSD, and the workflow's JSON export of the model.
        /// </summary>
        private sealed class RunDirectory : IDisposable
        {
            public RunDirectory(string projectName)
            {
                Directory_Output = Path.Combine(Path.GetTempPath(), string.Format("SAM_PartORunModelPersistenceTests_{0}", Guid.NewGuid()));
                Directory.CreateDirectory(Directory_Output);

                Path_TBD = Path.Combine(Directory_Output, projectName + ".tbd");

                //Exactly how Modify.RunPartOSimulation derives the results file from the run's TBD.
                Path_TSD = Path.ChangeExtension(Path_TBD, "tsd");

                File.WriteAllText(Path_TBD, "tbd");

                //A real file, because provenance reads length and write time rather than content.
                File.WriteAllText(Path_TSD, string.Format("results - {0}", Guid.NewGuid()));

                //Precisely what WorkflowCalculator's "Saving Model" step wrote for this run.
                File.WriteAllText(Path_WorkflowJson, "{ }");
            }

            public string Directory_Output { get; }

            public string Path_TBD { get; }

            public string Path_TSD { get; }

            /// <summary>Derived by the production authority, never spelled out here.</summary>
            public string Path_WorkflowJson => Query.Path_PartOWorkflowJson(Path_TBD);

            /// <summary>Derived by the production authority, never spelled out here.</summary>
            public string Path_Model => Query.Path_PartORunModel(Path_TSD);

            public void Dispose()
            {
                try
                {
                    Directory.Delete(Directory_Output, true);
                }
                catch
                {
                    //A test that cannot tidy its temp folder has still made its point.
                }
            }
        }

        /// <summary>
        /// <b>The successful run's evidence set.</b> The native <c>.sam</c> is written - a real archive, not
        /// JSON under another name - and the workflow's redundant <c>&lt;run&gt;.json</c> is gone. The TBD
        /// and the TSD, which are evidence rather than duplicates, are untouched.
        /// </summary>
        [Fact]
        public void ASuccessfulRun_LeavesTheSamAndNoJson()
        {
            using RunDirectory runDirectory = new("project-It1a");

            Assert.True(File.Exists(runDirectory.Path_WorkflowJson));

            Assert.True(Modify.PersistPartORunModel(StampedModel(runDirectory.Path_TSD), runDirectory.Path_TSD, runDirectory.Path_TBD, out string note));
            Assert.Null(note);

            //The authoritative artifact, and genuinely native: Core.Query.SAMFileType reads it as SAM, and
            //the first two bytes are a zip's - which is what tells a real archive from renamed JSON text.
            Assert.True(File.Exists(runDirectory.Path_Model));
            Assert.Equal(".sam", Path.GetExtension(runDirectory.Path_Model));
            Assert.Equal(SAMFileType.SAM, Core.Query.SAMFileType(runDirectory.Path_Model));
            Assert.Equal(new byte[] { 0x50, 0x4B }, File.ReadAllBytes(runDirectory.Path_Model)[0..2]);

            //The redundancy the change exists to remove.
            Assert.False(File.Exists(runDirectory.Path_WorkflowJson));

            //And nothing else was swept up: the run's other evidence is exactly where it was.
            Assert.True(File.Exists(runDirectory.Path_TBD));
            Assert.True(File.Exists(runDirectory.Path_TSD));

            List<string> extensions = new(Array.ConvertAll(Directory.GetFiles(runDirectory.Directory_Output), Path.GetExtension));
            extensions.Sort(StringComparer.Ordinal);

            Assert.Equal(new[] { ".sam", ".tbd", ".tsd" }, extensions);
        }

        /// <summary>
        /// <b>Baseline, each optimisation round and the capacity envelope are independent.</b> Each names its
        /// own model and its own workflow JSON from its own TBD, so removing one round's redundant file can
        /// never touch another's - and the JSON removed always shares the run's base name.
        /// </summary>
        [Fact]
        public void EachRun_NamesItsOwnWorkflowJson()
        {
            string directory = Path.GetTempPath();

            List<string> paths_Json = [];

            foreach (string projectName in new[] { "project-It1a", "project-It1a-Opt01", "project-It1a-Opt02", "project-It1a-OptMax" })
            {
                string path_TBD = Path.Combine(directory, projectName + ".tbd");
                string path_TSD = Path.ChangeExtension(path_TBD, "tsd");

                string path_Json = Query.Path_PartOWorkflowJson(path_TBD);

                //Exactly how WorkflowCalculator composes it: its Path_TBD's directory and base name.
                Assert.Equal(Path.Combine(directory, projectName + ".json"), path_Json);

                //And it is the sibling of this run's own model, never of another run's.
                Assert.Equal(
                    Path.GetFileNameWithoutExtension(Query.Path_PartORunModel(path_TSD)),
                    Path.GetFileNameWithoutExtension(path_Json));

                Assert.DoesNotContain(path_Json, paths_Json);

                paths_Json.Add(path_Json);
            }

            Assert.Null(Query.Path_PartOWorkflowJson(null));
            Assert.Null(Query.Path_PartOWorkflowJson("   "));
        }

        /// <summary>
        /// <b>Only the right file is removed.</b> A baseline and its Opt01 share an output folder;
        /// persisting the baseline leaves Opt01's workflow JSON completely alone. This is what "derive the
        /// path from this run, never scan the directory" buys.
        /// </summary>
        [Fact]
        public void RemovingOneRunsJson_LeavesAnotherRunsAlone()
        {
            using RunDirectory runDirectory = new("project-It1a");

            string path_TBD_Opt01 = Path.Combine(runDirectory.Directory_Output, "project-It1a-Opt01.tbd");
            string path_Json_Opt01 = Query.Path_PartOWorkflowJson(path_TBD_Opt01);

            File.WriteAllText(path_Json_Opt01, "{ }");

            Assert.True(Modify.PersistPartORunModel(StampedModel(runDirectory.Path_TSD), runDirectory.Path_TSD, runDirectory.Path_TBD, out string note));
            Assert.Null(note);

            Assert.False(File.Exists(runDirectory.Path_WorkflowJson));
            Assert.True(File.Exists(path_Json_Opt01));
        }

        /// <summary>
        /// <b>The removal changes nothing about reviewing the run.</b> With the redundant JSON gone, the
        /// <c>.sam</c> still reopens through the ordinary <c>Core.Convert.ToSAM</c> path, still restores into
        /// an assessable run holding its results and its scenarios, still validates both fingerprints, and
        /// still resolves its results after the whole folder moved. The JSON was never part of any of it -
        /// this is what proves it.
        /// </summary>
        [Fact]
        public void WithTheJsonRemoved_TheRunStillReopensRestoresAndValidates()
        {
            using RunDirectory runDirectory = new("project-It1a");

            List<OverheatingScenario> overheatingScenarios = Scenarios();

            AnalyticalModel analyticalModel = StampedModel(runDirectory.Path_TSD, overheatingScenarios);

            Assert.True(Modify.PersistPartORunModel(analyticalModel, runDirectory.Path_TSD, runDirectory.Path_TBD, out string _));
            Assert.False(File.Exists(runDirectory.Path_WorkflowJson));

            AnalyticalModel analyticalModel_Reopened = Core.Convert.ToSAM<AnalyticalModel>(runDirectory.Path_Model)?.Find(x => x is not null);

            Assert.NotNull(analyticalModel_Reopened);

            //Both digests survive the round trip, which is what every genuine review rests on.
            Assert.Equal(SimulationResultProvenance.Fingerprint(analyticalModel), SimulationResultProvenance.Fingerprint(analyticalModel_Reopened));
            Assert.Equal(SimulationResultProvenance.Fingerprint_Scenarios(analyticalModel), SimulationResultProvenance.Fingerprint_Scenarios(analyticalModel_Reopened));

            PartORun partORun = new();

            Assert.True(partORun.Restore(analyticalModel_Reopened, runDirectory.Path_Model, out string refusal));
            Assert.Null(refusal);

            //Results -> Overheating reads these two: the results file the assessment runs over, and the
            //scenarios that say which TM59 criterion applies where.
            Assert.True(partORun.IsAssessable(out string _));
            Assert.Equal(runDirectory.Path_TSD, partORun.Path_TSD);
            Assert.Equal(overheatingScenarios[0].Key, partORun.OverheatingScenarios[0].Key);

            //And the moved-folder resolution, which finds the results beside the model - a path the JSON
            //never took part in either.
            string directory_Moved = Path.Combine(Path.GetTempPath(), string.Format("SAM_PartORunModelPersistenceTests_{0}", Guid.NewGuid()));

            Directory.CreateDirectory(directory_Moved);

            try
            {
                string path_TSD_Moved = Path.Combine(directory_Moved, Path.GetFileName(runDirectory.Path_TSD));
                string path_Model_Moved = Path.Combine(directory_Moved, Path.GetFileName(runDirectory.Path_Model));

                //Content AND write time travel: a copy that re-stamps the time is a rewrite, and would
                //correctly refuse.
                File.Copy(runDirectory.Path_TSD, path_TSD_Moved);
                File.SetLastWriteTimeUtc(path_TSD_Moved, File.GetLastWriteTimeUtc(runDirectory.Path_TSD));
                File.Copy(runDirectory.Path_Model, path_Model_Moved);

                File.Delete(runDirectory.Path_TSD);

                PartORun partORun_Moved = new();

                Assert.True(partORun_Moved.Restore(analyticalModel_Reopened, path_Model_Moved, out string refusal_Moved));
                Assert.Null(refusal_Moved);
                Assert.Equal(path_TSD_Moved, partORun_Moved.Path_TSD);
            }
            finally
            {
                Directory.Delete(directory_Moved, true);
            }
        }

        /// <summary>
        /// <b>A failed <c>.sam</c> write must never become the loss of the run model.</b> The model is
        /// directed at a folder that does not exist, so the native write fails; the workflow's JSON is
        /// therefore left exactly where it is, as the only remaining copy of the model, and the problem is
        /// reported rather than swallowed.
        /// <para>
        /// This is the ordering the whole design rests on - deleting first and writing second would turn one
        /// unwritable path into a run with no persisted model at all.
        /// </para>
        /// </summary>
        [Fact]
        public void WhenTheSamCannotBeWritten_TheJsonIsKept()
        {
            using RunDirectory runDirectory = new("project-It1a");

            //A results path in a folder that does not exist: Path_PartORunModel names a .sam beside it, and
            //the native writer refuses a directory that is not there.
            string path_TSD_Unwritable = Path.Combine(runDirectory.Directory_Output, Guid.NewGuid().ToString(), "project-It1a.tsd");

            Assert.False(Modify.PersistPartORunModel(StampedModel(runDirectory.Path_TSD), path_TSD_Unwritable, runDirectory.Path_TBD, out string note));

            Assert.False(File.Exists(Query.Path_PartORunModel(path_TSD_Unwritable)));

            //The fallback survived, and it is still the model.
            Assert.True(File.Exists(runDirectory.Path_WorkflowJson));

            //Reported, and specific enough to act on.
            Assert.NotNull(note);
            Assert.Contains(Query.Path_PartORunModel(path_TSD_Unwritable), note);
        }

        /// <summary>
        /// <b>A JSON that cannot be removed does not invalidate a completed run.</b> The file is held open
        /// with no sharing, exactly as another process would hold it, so the deletion fails. The <c>.sam</c>
        /// is already written and remains authoritative, the call still reports success, and the cleanup
        /// failure is a note.
        /// <para>
        /// Failing a finished simulation and its assessment over a locked leftover file would be a far worse
        /// answer than leaving the leftover file.
        /// </para>
        /// </summary>
        [Fact]
        public void WhenTheJsonCannotBeRemoved_TheRunStillSucceeds()
        {
            using RunDirectory runDirectory = new("project-It1a");

            string note;

            using (FileStream fileStream = new(runDirectory.Path_WorkflowJson, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                Assert.True(Modify.PersistPartORunModel(StampedModel(runDirectory.Path_TSD), runDirectory.Path_TSD, runDirectory.Path_TBD, out note));
            }

            //Written, native, and readable back - the run's evidence is complete.
            Assert.True(File.Exists(runDirectory.Path_Model));
            Assert.Equal(SAMFileType.SAM, Core.Query.SAMFileType(runDirectory.Path_Model));
            Assert.NotNull(Core.Convert.ToSAM<AnalyticalModel>(runDirectory.Path_Model)?.Find(x => x is not null));

            //The leftover, and the note that names it.
            Assert.True(File.Exists(runDirectory.Path_WorkflowJson));
            Assert.NotNull(note);
            Assert.Contains(runDirectory.Path_WorkflowJson, note);
        }

        /// <summary>
        /// A run whose workflow wrote no JSON - there is simply nothing to clean up. The <c>.sam</c> is
        /// written and the call succeeds silently rather than reporting a failure to delete a file that was
        /// never there.
        /// </summary>
        [Fact]
        public void WithNoWorkflowJson_ThereIsNothingToReport()
        {
            using RunDirectory runDirectory = new("project-It1a");

            File.Delete(runDirectory.Path_WorkflowJson);

            Assert.True(Modify.PersistPartORunModel(StampedModel(runDirectory.Path_TSD), runDirectory.Path_TSD, runDirectory.Path_TBD, out string note));
            Assert.Null(note);
            Assert.True(File.Exists(runDirectory.Path_Model));
        }

        /// <summary>
        /// Nothing to persist, nothing removed. A call with no model deletes no JSON - the cleanup is only
        /// ever earned by a model that was actually written.
        /// </summary>
        [Fact]
        public void WithNoModel_NothingIsRemoved()
        {
            using RunDirectory runDirectory = new("project-It1a");

            Assert.False(Modify.PersistPartORunModel(null, runDirectory.Path_TSD, runDirectory.Path_TBD, out string note));
            Assert.Null(note);

            Assert.True(File.Exists(runDirectory.Path_WorkflowJson));
            Assert.False(File.Exists(runDirectory.Path_Model));
        }
    }
}
