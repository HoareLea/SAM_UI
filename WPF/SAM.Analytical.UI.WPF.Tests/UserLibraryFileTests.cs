// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.UI.WPF.Tests.Helpers;
using SAM.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// The shared persistence engine of the user libraries (<see cref="UserLibraryFile"/>), tested on its own with plain ConstructionManagers:
    /// missing / unreadable / ready; an unreadable file is never overwritten; the previous file is kept as .bak and no temporary or lock file is
    /// left; a second writer waits for the lock and a timeout fails explicitly and writes nothing; concurrent transactions are serialised and
    /// none is lost; the edit runs under the lock; a failed write (injected through the test seam) leaves the file as it was; a throwing change
    /// handler does not stop the others. Each test uses its own temporary folder.
    /// </summary>
    public sealed class UserLibraryFileTests : IDisposable
    {
        private readonly string directory = BuilderFixture.TempDirectory();

        public void Dispose()
        {
            try
            {
                Directory.Delete(directory, true);
            }
            catch (Exception)
            {
            }
        }

        private UserLibraryFile Engine(TimeSpan? lockTimeout = null) => new UserLibraryFile(Path.Combine(directory, "Test Library.json"), "Test library", "test library", lockTimeout ?? TimeSpan.FromSeconds(10));

        private static string Hash(string path) => System.Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));

        private static ConstructionManager Library(params string[] names)
        {
            return new ConstructionManager(names.Select(x => new ApertureConstruction(Guid.NewGuid(), x, ApertureType.Window)), null, new MaterialLibrary("Test library")) { Name = "Test library" };
        }

        private static List<string> Names(UserLibraryFileContent content) => Systems(content).Select(x => x.Name).OrderBy(x => x).ToList();

        // ConstructionManager answers null for a library without any aperture construction.
        private static List<ApertureConstruction> Systems(UserLibraryFileContent content) => content.ConstructionManager.ApertureConstructions ?? new List<ApertureConstruction>();

        // ---- Read ---------------------------------------------------------------------------------------------------

        [Fact]
        public void A_missing_file_is_an_empty_library_and_reading_creates_nothing()
        {
            UserLibraryFile file = Engine();

            UserLibraryFileContent content = file.Read();

            Assert.Equal(UserLibraryFileState.Missing, content.State);
            Assert.Null(content.Error);
            Assert.Empty(Systems(content));
            Assert.False(File.Exists(file.Path));
            Assert.Empty(Directory.GetFileSystemEntries(directory));
        }

        [Theory]
        [InlineData("{ this is not json", "is not a readable test library")]
        [InlineData("", "is not a readable test library (the file is empty)")]
        [InlineData("   ", "is not a readable test library (the file is empty)")]
        [InlineData("{\"_type\":\"SAM.Analytical.Panel,SAM.Analytical\"}", "(it is not a SAM construction manager)")]
        [InlineData("[1,2]", "is not a readable test library")]
        public void A_file_that_is_not_a_construction_manager_is_unreadable_and_says_why(string text, string expected)
        {
            UserLibraryFile file = Engine();
            File.WriteAllText(file.Path, text);

            UserLibraryFileContent content = file.Read();

            Assert.Equal(UserLibraryFileState.Unreadable, content.State);
            Assert.StartsWith("Test Library.json ", content.Error);
            Assert.Contains(expected, content.Error);
            Assert.Contains("it is left as it is.", content.Error);
            Assert.Empty(Systems(content));
        }

        [Fact]
        public void A_written_file_reads_back_as_ready_and_is_ordinary_construction_manager_json()
        {
            UserLibraryFile file = Engine();
            file.Write(Library("A", "B"));

            UserLibraryFileContent content = file.Read();

            Assert.Equal(UserLibraryFileState.Ready, content.State);
            Assert.Equal(new[] { "A", "B" }, Names(content));
            Assert.Contains("ConstructionManager", File.ReadAllText(file.Path));
        }

        [Fact]
        public void Parse_accepts_only_construction_manager_json()
        {
            Assert.NotNull(UserLibraryFile.Parse(Library("A").ToJsonObject().ToJsonString(), out string error));
            Assert.Null(error);
            Assert.Null(UserLibraryFile.Parse(null, out error));
            Assert.Equal("the file is empty", error);
            Assert.Null(UserLibraryFile.Parse("{\"x\":1}", out error));
            Assert.Equal("it is not a SAM construction manager", error);
        }

        // ---- Transact ---------------------------------------------------------------------------------------------------

        [Fact]
        public void A_transaction_on_a_missing_file_creates_it_and_leaves_no_bak_lock_or_temporary_file()
        {
            UserLibraryFile file = Engine();

            string error = file.Transact(content =>
            {
                Assert.Equal(UserLibraryFileState.Missing, content.State);
                return UserLibraryEdit.Write(Library("First"));
            });

            Assert.Null(error);
            Assert.Equal(new[] { "First" }, Names(file.Read()));
            Assert.False(File.Exists(file.BackupPath));
            Assert.False(File.Exists(file.LockPath));
            Assert.Empty(Directory.GetFiles(directory, "*.tmp"));
        }

        [Fact]
        public void Each_write_keeps_the_previous_file_as_bak()
        {
            UserLibraryFile file = Engine();
            Assert.Null(file.Transact(content => UserLibraryEdit.Write(Library("A"))));
            string first = File.ReadAllText(file.Path);

            Assert.Null(file.Transact(content => UserLibraryEdit.Write(Library("A", "B"))));

            Assert.Equal(first, File.ReadAllText(file.BackupPath));
            Assert.Equal(new[] { "A", "B" }, Names(file.Read()));
            Assert.Empty(Directory.GetFiles(directory, "*.tmp"));
            Assert.False(File.Exists(file.LockPath));
        }

        [Fact]
        public void An_unreadable_file_is_never_overwritten_and_the_edit_never_runs()
        {
            UserLibraryFile file = Engine();
            File.WriteAllText(file.Path, "{ this is not json");
            string hash = Hash(file.Path);
            bool ran = false;

            string error = file.Transact(content =>
            {
                ran = true;
                return UserLibraryEdit.Write(Library("X"));
            });

            Assert.False(ran);
            Assert.Contains("left as it is", error);
            Assert.Equal(hash, Hash(file.Path));
            Assert.False(File.Exists(file.BackupPath));
            Assert.False(File.Exists(file.LockPath));
        }

        [Fact]
        public void An_edit_that_fails_or_changes_nothing_writes_nothing()
        {
            UserLibraryFile file = Engine();
            Assert.Null(file.Transact(content => UserLibraryEdit.Write(Library("A"))));
            string hash = Hash(file.Path);

            string failed = file.Transact(content => UserLibraryEdit.Fail("No good."));
            string unchanged = file.Transact(content => UserLibraryEdit.NoChange());
            string nothing = file.Transact(content => null);

            Assert.Equal("No good.", failed);
            Assert.Null(unchanged);
            Assert.NotNull(nothing);
            Assert.Equal(hash, Hash(file.Path));
            Assert.False(File.Exists(file.BackupPath));
        }

        [Fact]
        public void The_edit_sees_the_file_as_it_is_under_the_lock_and_runs_with_the_lock_held()
        {
            UserLibraryFile file = Engine();
            Assert.Null(file.Transact(content => UserLibraryEdit.Write(Library("A"))));

            // Another instance writes after this one first read: the edit still sees it (re-read under the lock).
            UserLibraryFile other = Engine();
            UserLibraryFileContent early = file.Read();
            Assert.Null(other.Transact(content => UserLibraryEdit.Write(Library("A", "From other"))));

            bool locked = false;
            List<string> seen = null;
            Assert.Null(file.Transact(content =>
            {
                seen = Names(content);
                try
                {
                    using (new FileStream(file.LockPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                    {
                    }
                }
                catch (IOException)
                {
                    locked = true;
                }

                return UserLibraryEdit.NoChange();
            }));

            Assert.Equal(new[] { "A" }, Names(early));
            Assert.Equal(new[] { "A", "From other" }, seen);
            Assert.True(locked);
            Assert.False(File.Exists(file.LockPath));
        }

        [Fact]
        public void A_second_writer_waits_for_the_lock_and_then_goes_ahead()
        {
            UserLibraryFile file = Engine();
            Assert.Null(file.Transact(content => UserLibraryEdit.Write(Library("A"))));
            Task<string> second;
            using (new FileStream(file.LockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                second = Task.Run(() => file.Transact(content => UserLibraryEdit.Write(Library("A", "Second"))));
                Thread.Sleep(300);
                Assert.False(second.IsCompleted);
            }

            Assert.Null(second.Result);
            Assert.Equal(new[] { "A", "Second" }, Names(file.Read()));
        }

        [Fact]
        public void A_writer_that_cannot_get_the_lock_fails_explicitly_runs_nothing_and_writes_nothing()
        {
            UserLibraryFile file = Engine(TimeSpan.FromMilliseconds(200));
            Assert.Null(file.Transact(content => UserLibraryEdit.Write(Library("A"))));
            string hash = Hash(file.Path);
            bool ran = false;

            string error;
            using (new FileStream(file.LockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                error = file.Transact(content =>
                {
                    ran = true;
                    return UserLibraryEdit.Write(Library("B"));
                });
            }

            Assert.False(ran);
            Assert.Equal("Test Library.json is being saved by another SAM window; try again in a moment.", error);
            Assert.Equal(hash, Hash(file.Path));
            File.Delete(file.LockPath);
            Assert.Null(file.Transact(content => UserLibraryEdit.NoChange()));
        }

        [Fact]
        public async Task Concurrent_transactions_are_serialised_and_none_is_lost()
        {
            UserLibraryFile seed = Engine();
            List<Task<string>> tasks = Enumerable.Range(1, 8).Select(i => Task.Run(() => Engine().Transact(content =>
            {
                List<ApertureConstruction> systems = Systems(content);
                systems.Add(new ApertureConstruction(Guid.NewGuid(), "Parallel " + i, ApertureType.Window));
                return UserLibraryEdit.Write(new ConstructionManager(systems, null, content.ConstructionManager.MaterialLibrary));
            }))).ToList();

            string[] errors = await Task.WhenAll(tasks);

            Assert.All(errors, Assert.Null);
            Assert.Equal(Enumerable.Range(1, 8).Select(i => "Parallel " + i).OrderBy(x => x), Names(seed.Read()));
        }

        // ---- A failed write -----------------------------------------------------------------------------------------------

        [Fact]
        public void A_failed_write_is_reported_and_leaves_the_file_as_it_was_with_no_temporary_or_lock_file()
        {
            UserLibraryFile file = Engine();
            Assert.Null(file.Transact(content => UserLibraryEdit.Write(Library("A"))));
            string hash = Hash(file.Path);
            List<string> targets = new List<string>();
            file.BeforeWrite = path =>
            {
                targets.Add(path);
                throw new IOException("disk full");
            };

            string error = file.Transact(content => UserLibraryEdit.Write(Library("A", "B")));

            Assert.Equal(new[] { file.Path }, targets);
            Assert.Equal("Test Library.json could not be written: disk full", error);
            Assert.Equal(hash, Hash(file.Path));
            Assert.Empty(Directory.GetFiles(directory, "*.tmp"));
            Assert.False(File.Exists(file.LockPath));

            file.BeforeWrite = null;
            Assert.Null(file.Transact(content => UserLibraryEdit.Write(Library("A", "B"))));
            Assert.Equal(new[] { "A", "B" }, Names(file.Read()));
        }

        [Fact]
        public void A_companion_file_uses_the_same_engine_and_the_same_test_seam_but_never_the_libraries_lock()
        {
            UserLibraryFile file = Engine();
            List<string> targets = new List<string>();
            file.BeforeWrite = path => targets.Add(path);

            UserLibraryFile companion = file.Companion(UserLibraryArchive.PathFor(file.Path), "Removed", "test library");
            companion.Write(Library("Gone"));

            Assert.Equal(Path.Combine(directory, "Test Library.removed.json"), companion.Path);
            Assert.Equal(new[] { companion.Path }, targets);
            Assert.Equal(new[] { "Gone" }, Names(companion.Read()));
            Assert.NotEqual(file.LockPath, companion.LockPath);
        }

        [Theory]
        [InlineData(@"C:\Libraries\Glazing Systems.json", @"C:\Libraries\Glazing Systems.removed.json")]
        [InlineData(@"C:\Libraries\Constructions.json", @"C:\Libraries\Constructions.removed.json")]
        [InlineData(@"C:\Libraries\No Extension", @"C:\Libraries\No Extension.removed.json")]
        [InlineData(@"C:\Libraries\a.b.json", @"C:\Libraries\a.b.removed.json")]
        public void The_archive_sits_next_to_the_library_with_a_removed_suffix(string library, string archive)
        {
            Assert.Equal(archive, UserLibraryArchive.PathFor(library));
        }

        // ---- Notify -------------------------------------------------------------------------------------------------------

        [Fact]
        public void Notify_isolates_a_throwing_handler_and_tolerates_none()
        {
            int calls = 0;
            object sender = new object();
            object seen = null;
            EventHandler handler = null;
            handler += (s, e) => calls++;
            handler += (s, e) => throw new InvalidOperationException("a list that fails to refresh");
            handler += (s, e) =>
            {
                calls++;
                seen = s;
            };

            UserLibraryFile.Notify(handler, sender);
            UserLibraryFile.Notify(null, sender);

            Assert.Equal(2, calls);
            Assert.Same(sender, seen);
        }
    }
}
