// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// <b>A file left by an earlier attempt is never reported as this attempt's.</b>
    /// <para>
    /// Candidate B writes to deterministic paths, which is what makes a pairing reopenable and also
    /// means a failed attempt leaves its <c>.tbd</c>, <c>.tsd</c>, <c>.tpd</c> and reports exactly where
    /// the next attempt will look. "The thermal source produced Flat-It3B.tsd" said because that file
    /// happens to exist is a lie told with a real filename, and nothing downstream could catch it.
    /// </para>
    /// </summary>
    public class PartOIteration3ArtifactTests
    {
        private static string Write(string directory, string name, string content)
        {
            string result = Path.Combine(directory, name);

            File.WriteAllText(result, content);

            return result;
        }

        [Fact]
        public void A_file_that_did_not_exist_and_now_does_is_claimed_as_created()
        {
            string directory = PartOIteration3Fixture.Directory_Temp();

            try
            {
                string path = Path.Combine(directory, "Flat-It3B.tsd");

                PartOIteration3Artifacts partOIteration3Artifacts = new(Guid.NewGuid());
                partOIteration3Artifacts.Snapshot([path]);

                File.WriteAllText(path, "this attempt wrote this");

                Assert.True(partOIteration3Artifacts.TryClaim(path, out string artifact, out string refusal));
                Assert.Null(refusal);
                Assert.Contains("created", artifact);
                Assert.Contains("Flat-It3B.tsd", artifact);
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        [Fact]
        public void A_file_that_existed_and_has_changed_is_claimed_as_updated()
        {
            string directory = PartOIteration3Fixture.Directory_Temp();

            try
            {
                string path = Write(directory, "Flat-It3B.tsd", "an earlier attempt");

                PartOIteration3Artifacts partOIteration3Artifacts = new(Guid.NewGuid());
                partOIteration3Artifacts.Snapshot([path]);

                //A different length, so the change is visible whatever the filesystem's timestamp
                //resolution happens to be on this machine.
                File.WriteAllText(path, "this attempt wrote this instead, and it is longer");

                Assert.True(partOIteration3Artifacts.TryClaim(path, out string artifact, out string refusal));
                Assert.Null(refusal);
                Assert.Contains("updated", artifact);
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        /// <summary>
        /// The case the whole type exists for: the file is there, it looks right, and this attempt did
        /// not write it.
        /// </summary>
        [Fact]
        public void A_file_left_by_an_earlier_attempt_is_refused_rather_than_claimed()
        {
            string directory = PartOIteration3Fixture.Directory_Temp();

            try
            {
                string path = Write(directory, "Flat-It3B.tsd", "an earlier attempt's results");

                PartOIteration3Artifacts partOIteration3Artifacts = new(Guid.NewGuid());
                partOIteration3Artifacts.Snapshot([path]);

                Assert.False(partOIteration3Artifacts.TryClaim(path, out string artifact, out string refusal));
                Assert.Null(artifact);
                Assert.Contains("unchanged since this attempt started", refusal);
                Assert.Contains("earlier attempt", refusal);
                Assert.Empty(partOIteration3Artifacts.Claimed);
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        [Fact]
        public void A_file_that_does_not_exist_is_refused()
        {
            string directory = PartOIteration3Fixture.Directory_Temp();

            try
            {
                string path = Path.Combine(directory, "Flat-It3B.tsd");

                PartOIteration3Artifacts partOIteration3Artifacts = new(Guid.NewGuid());
                partOIteration3Artifacts.Snapshot([path]);

                Assert.False(partOIteration3Artifacts.TryClaim(path, out string _, out string refusal));
                Assert.Contains("does not exist", refusal);
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        /// <summary>
        /// Fail closed: a path nobody fingerprinted cannot be told apart from stale state, so guessing
        /// in the reassuring direction is exactly what is not done.
        /// </summary>
        [Fact]
        public void A_path_that_was_never_snapshotted_is_refused_even_when_the_file_is_there()
        {
            string directory = PartOIteration3Fixture.Directory_Temp();

            try
            {
                string path = Write(directory, "Flat-It3B.tsd", "who wrote this?");

                PartOIteration3Artifacts partOIteration3Artifacts = new(Guid.NewGuid());

                Assert.False(partOIteration3Artifacts.IsTracked(path));
                Assert.False(partOIteration3Artifacts.TryClaim(path, out string _, out string refusal));
                Assert.Contains("not recorded before this attempt started", refusal);
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        [Fact]
        public void Claiming_a_set_refuses_the_whole_set_where_any_member_is_stale()
        {
            string directory = PartOIteration3Fixture.Directory_Temp();

            try
            {
                string path_Fresh = Path.Combine(directory, "Flat-It3B.tbd");
                string path_Stale = Write(directory, "Flat-It3B.tsd", "an earlier attempt's results");

                PartOIteration3Artifacts partOIteration3Artifacts = new(Guid.NewGuid());
                partOIteration3Artifacts.Snapshot([path_Fresh, path_Stale]);

                File.WriteAllText(path_Fresh, "this attempt");

                Assert.False(partOIteration3Artifacts.TryClaim(new[] { path_Fresh, path_Stale }, out List<string> artifacts, out List<string> refusals));

                Assert.Single(artifacts);
                Assert.Single(refusals);
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        /// <summary>
        /// A file whose write time moved but whose length did not is still this attempt's. Its own
        /// timestamp is the only thing that changed, and that is exactly what "touched" means.
        /// </summary>
        [Fact]
        public void A_file_rewritten_to_the_same_length_is_still_recognised_as_this_attempts()
        {
            string directory = PartOIteration3Fixture.Directory_Temp();

            try
            {
                string path = Write(directory, "Flat-It3B.tsd", "0123456789");

                PartOIteration3Artifacts partOIteration3Artifacts = new(Guid.NewGuid());
                partOIteration3Artifacts.Snapshot([path]);

                Thread.Sleep(20);

                File.WriteAllText(path, "9876543210");
                File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddMinutes(1));

                Assert.True(partOIteration3Artifacts.TryClaim(path, out string artifact, out string _));
                Assert.Contains("updated", artifact);
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        //-------------------------------------------------------------------------------------------------
        //The same fingerprint, on the other side: what a REVIEW validates a recorded file by.
        //-------------------------------------------------------------------------------------------------

        [Fact]
        public void A_recorded_file_that_is_unchanged_is_current()
        {
            string directory = PartOIteration3Fixture.Directory_Temp();

            try
            {
                string path = Write(directory, "Flat-It3B-Bridge.tsd", "results");

                PartOIteration3Artifacts.TryRead(path, out long length, out long ticks);

                PartOIteration3FileRecord partOIteration3FileRecord = new(PartOIteration3Roles.Bridge_TSD, path, length, ticks);

                Assert.True(partOIteration3FileRecord.Current(out string refusal));
                Assert.Null(refusal);
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        [Fact]
        public void A_recorded_file_that_has_been_touched_refuses_by_name()
        {
            string directory = PartOIteration3Fixture.Directory_Temp();

            try
            {
                string path = Write(directory, "Flat-It3B-Bridge.tsd", "results");

                PartOIteration3Artifacts.TryRead(path, out long length, out long ticks);

                PartOIteration3FileRecord partOIteration3FileRecord = new(PartOIteration3Roles.Bridge_TSD, path, length, ticks);

                File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddHours(1));

                Assert.False(partOIteration3FileRecord.Current(out string refusal));
                Assert.Contains(PartOIteration3Roles.Bridge_TSD, refusal);
                Assert.Contains("rewritten", refusal);
                Assert.Contains("Flat-It3B-Bridge.tsd", refusal);
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        [Fact]
        public void A_recorded_file_that_has_gone_refuses_by_name()
        {
            string directory = PartOIteration3Fixture.Directory_Temp();

            try
            {
                string path = Write(directory, "Flat-It3B-Bridge.tsd", "results");

                PartOIteration3Artifacts.TryRead(path, out long length, out long ticks);

                PartOIteration3FileRecord partOIteration3FileRecord = new(PartOIteration3Roles.Bridge_TSD, path, length, ticks);

                File.Delete(path);

                Assert.False(partOIteration3FileRecord.Current(out string refusal));
                Assert.Contains(PartOIteration3Roles.Bridge_TSD, refusal);
                Assert.Contains("no longer at", refusal);
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }
    }
}
