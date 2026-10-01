// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.IO;
using System.Text.Json.Nodes;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// The portability fix on a REAL project folder from a licensed run: a model, its legacy
    /// <c>.partomixed.json</c> (absolute paths, as an earlier build wrote it) and the run's real <c>.tsd</c> and
    /// run model. Environment-gated, like <c>PartOMixedLargeProjectAcceptance</c>: set <c>SAM_PARTO_SIDECAR_REAL_DIR</c> to
    /// such a folder (the one holding the model and its <c>.partomixed.json</c>) and it silently returns where it is
    /// not set.
    /// <para>
    /// The folder is copied twice, with <c>File.Copy</c> so the results' write times survive, and the originals are
    /// never touched. The legacy sidecar is read in place, re-saved (which writes it relative), the whole folder is
    /// moved, and the moved sidecar must find the run's real files - each still matching the length and write time
    /// the run recorded - while a second copy, with the first left in place, reads only its own.
    /// </para>
    /// </summary>
    public class PartOSidecarPortabilityRealDataAcceptance : IDisposable
    {
        private readonly string directory = Path.Combine(Path.GetTempPath(), "SAM_PartOSidecarReal_" + Guid.NewGuid().ToString("N"));

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }
            catch (IOException)
            {
            }
        }

        private static void CopyFolder(string from, string to)
        {
            foreach (string path in Directory.GetFiles(from, "*", SearchOption.AllDirectories))
            {
                string path_To = Path.Combine(to, Path.GetRelativePath(from, path));
                Directory.CreateDirectory(Path.GetDirectoryName(path_To));
                File.Copy(path, path_To, true);
            }
        }

        private static void AssertLineage(PartOMixedRunEvidence evidence, string folder)
        {
            Assert.StartsWith(folder, evidence.Path_TSD, StringComparison.OrdinalIgnoreCase);
            Assert.StartsWith(folder, evidence.Path_RunModel, StringComparison.OrdinalIgnoreCase);

            //The run's own lineage check, on the real file.
            FileInfo fileInfo = new(evidence.Path_TSD);
            Assert.True(fileInfo.Exists, evidence.Path_TSD);
            Assert.Equal(evidence.Length_TSD, fileInfo.Length);
            Assert.Equal(evidence.Timestamp_TSD, fileInfo.LastWriteTimeUtc.Ticks);
            Assert.True(File.Exists(evidence.Path_RunModel), evidence.Path_RunModel);
        }

        [Fact]
        public void ARealLegacySidecar_SurvivesAMoveAndACopy()
        {
            string directory_Real = Environment.GetEnvironmentVariable("SAM_PARTO_SIDECAR_REAL_DIR");
            if (string.IsNullOrWhiteSpace(directory_Real) || !Directory.Exists(directory_Real))
            {
                return;
            }

            string path_State_Real = Directory.GetFiles(directory_Real, "*" + PartOMixedDesignState.Suffix)[0];
            string text_Real_Before = File.ReadAllText(path_State_Real);

            string folder_A = Path.Combine(directory, "A");
            CopyFolder(directory_Real, folder_A);
            string path_State_A = Path.Combine(folder_A, Path.GetFileName(path_State_Real));

            // ---- The legacy sidecar, as an earlier build wrote it: absolute paths ---------------------------------
            //The copy of a legacy sidecar still names the ORIGINAL's run - that cannot be improved on without guessing, and
            //it is never rewritten into a relative path out of the copy. What an earlier build would have written had the
            //project lived in folder A is the same file with A's paths, which is what is used from here on.
            string text_Legacy = File.ReadAllText(path_State_A);
            Assert.Contains(directory_Real.Replace("\\", "\\\\"), text_Legacy, StringComparison.OrdinalIgnoreCase);
            File.WriteAllText(path_State_A, text_Legacy.Replace(directory_Real.Replace("\\", "\\\\"), folder_A.Replace("\\", "\\\\"), StringComparison.OrdinalIgnoreCase));

            PartOMixedDesignState state_Legacy = PartOMixedDesignState.Read(path_State_A);
            Assert.NotNull(state_Legacy?.FinalRun);
            AssertLineage(state_Legacy.FinalRun, folder_A);

            // ---- Saved again: relative, and no absolute path left ---------------------------------------------------
            Assert.True(state_Legacy.Write(path_State_A, out string note), note);
            Assert.DoesNotContain(folder_A.Replace("\\", "\\\\"), File.ReadAllText(path_State_A), StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(Path.GetTempPath().Replace("\\", "\\\\"), File.ReadAllText(path_State_A), StringComparison.OrdinalIgnoreCase);
            Assert.NotNull((string)((JsonObject)((JsonObject)JsonNode.Parse(File.ReadAllText(path_State_A)))["FinalRun"])["Locator_TSD"]);

            // ---- Copied with the first left in place: it reads its OWN run -----------------------------------------
            string folder_B = Path.Combine(directory, "B");
            CopyFolder(folder_A, folder_B);
            PartOMixedDesignState state_Copy = PartOMixedDesignState.Read(Path.Combine(folder_B, Path.GetFileName(path_State_Real)));
            AssertLineage(state_Copy.FinalRun, folder_B);

            // ---- Moved: the whole folder, somewhere else --------------------------------------------------------------
            string folder_Moved = Path.Combine(directory, "Elsewhere", "Moved");
            Directory.CreateDirectory(Path.GetDirectoryName(folder_Moved));
            Directory.Move(folder_A, folder_Moved);
            PartOMixedDesignState state_Moved = PartOMixedDesignState.Read(Path.Combine(folder_Moved, Path.GetFileName(path_State_Real)));
            AssertLineage(state_Moved.FinalRun, folder_Moved);

            // ---- The real folder was never touched ------------------------------------------------------------------
            Assert.Equal(text_Real_Before, File.ReadAllText(path_State_Real));
        }
    }
}
