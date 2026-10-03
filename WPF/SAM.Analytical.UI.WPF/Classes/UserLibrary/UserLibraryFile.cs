// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>What <see cref="UserLibraryFile.Read"/> found.</summary>
    internal enum UserLibraryFileState
    {
        Missing,
        Ready,
        Unreadable,
    }

    /// <summary>The content of a library file as it is on disk now. A missing or unreadable file has an empty <see cref="ConstructionManager"/>.</summary>
    internal sealed class UserLibraryFileContent
    {
        internal UserLibraryFileContent(UserLibraryFileState state, ConstructionManager constructionManager, string error, string libraryName)
        {
            State = state;
            ConstructionManager = constructionManager ?? new ConstructionManager(new List<ApertureConstruction>(), null, new MaterialLibrary(libraryName));
            Error = error;
        }

        public UserLibraryFileState State { get; }

        public ConstructionManager ConstructionManager { get; }

        /// <summary>Why the file could not be read; null otherwise.</summary>
        public string Error { get; }
    }

    /// <summary>
    /// What a <see cref="UserLibraryFile.Transact"/> edit decided: write <see cref="Result"/> as the new content of the file, change nothing
    /// (<see cref="NoChange"/>), or stop with <see cref="Error"/>. Only the first writes anything.
    /// </summary>
    internal sealed class UserLibraryEdit
    {
        private UserLibraryEdit(ConstructionManager result, string error)
        {
            Result = result;
            Error = error;
        }

        public ConstructionManager Result { get; }

        public string Error { get; }

        /// <summary>True when the edit found nothing to change: nothing is written and the transaction succeeds.</summary>
        public bool IsNoChange => Result == null && Error == null;

        public static UserLibraryEdit Write(ConstructionManager result) => new UserLibraryEdit(result ?? throw new ArgumentNullException(nameof(result)), null);

        public static UserLibraryEdit NoChange() => new UserLibraryEdit(null, null);

        public static UserLibraryEdit Fail(string error) => new UserLibraryEdit(null, string.IsNullOrWhiteSpace(error) ? "The library could not be changed." : error);
    }

    /// <summary>
    /// The persistence engine of a "user library": ONE ordinary SAM <see cref="ConstructionManager"/> JSON file that several SAM windows or
    /// processes may change. It owns everything that makes that safe, so every typed library (glazing systems today, constructions later) gets
    /// the same behaviour and none copies it:
    /// <list type="bullet">
    /// <item><description>an exclusive lock file (<c>.lock</c>, <see cref="FileShare.None"/>, deleted on close; a second writer waits up to the
    /// timeout, then fails with a message and writes nothing) around the whole read-edit-write, so no update is lost;</description></item>
    /// <item><description>the file is re-read UNDER the lock, and a file that exists but cannot be read is never overwritten;</description></item>
    /// <item><description>a write is verified (it must read back), goes to a temporary file and is swapped in atomically
    /// (<see cref="File.Replace(string, string, string, bool)"/>), keeping the previous file as <c>.bak</c>; temporary files are always removed;</description></item>
    /// <item><description>readers share with a concurrent atomic replace (a brief sharing violation is retried);</description></item>
    /// <item><description>change notifications are raised after the lock is released, each handler isolated (<see cref="Notify"/>).</description></item>
    /// </list>
    /// It has no model and never touches one. Every failure is returned, nothing is swallowed.
    /// </summary>
    internal sealed class UserLibraryFile
    {
        private readonly string libraryName;
        private readonly string noun;
        private readonly TimeSpan lockTimeout;

        /// <param name="path">The library file (a full path).</param>
        /// <param name="libraryName">The name given to the library an empty/new file holds.</param>
        /// <param name="noun">What the file is called in messages ("glazing library").</param>
        /// <param name="lockTimeout">How long a writer waits for another one.</param>
        internal UserLibraryFile(string path, string libraryName, string noun, TimeSpan lockTimeout)
        {
            Path = path;
            this.libraryName = libraryName;
            this.noun = noun;
            this.lockTimeout = lockTimeout;
        }

        public string Path { get; }

        public string BackupPath => Path + ".bak";

        public string LockPath => Path + ".lock";

        public string FileName => System.IO.Path.GetFileName(Path);

        /// <summary>
        /// TESTS ONLY: called with the target path just before anything is written to it (after the content was verified); a throw simulates
        /// a failed write of that file. Never set outside tests.
        /// </summary>
        internal Action<string> BeforeWrite { get; set; }

        /// <summary>The same engine on another file that is only ever changed while this file's lock is held (the archive).</summary>
        internal UserLibraryFile Companion(string path, string libraryName, string noun)
        {
            return new UserLibraryFile(path, libraryName, noun, lockTimeout) { BeforeWrite = BeforeWrite };
        }

        /// <summary>Reads the file as it is on disk now. A missing file is an empty library; an unreadable one says why.</summary>
        public UserLibraryFileContent Read()
        {
            if (!File.Exists(Path))
            {
                return new UserLibraryFileContent(UserLibraryFileState.Missing, null, null, libraryName);
            }

            string text;
            try
            {
                text = ReadAllTextShared(Path);
            }
            catch (Exception exception)
            {
                return new UserLibraryFileContent(UserLibraryFileState.Unreadable, null, string.Format(CultureInfo.CurrentCulture, "{0} could not be read: {1}", FileName, exception.Message), libraryName);
            }

            ConstructionManager constructionManager = Parse(text, out string error);
            return constructionManager == null
                ? new UserLibraryFileContent(UserLibraryFileState.Unreadable, null, string.Format(CultureInfo.CurrentCulture, "{0} is not a readable {1} ({2}); it is left as it is.", FileName, noun, error), libraryName)
                : new UserLibraryFileContent(UserLibraryFileState.Ready, constructionManager, null, libraryName);
        }

        /// <summary>
        /// One locked read-edit-write: lock → re-read → refuse an unreadable file → <paramref name="edit"/> → write → release. Returns null when the
        /// edit's result was written, otherwise why not (and nothing was written by this call; an edit that writes a COMPANION file itself, as
        /// Remove does, owns that file's failure contract). The edit runs under the lock, on the calling thread.
        /// </summary>
        public string Transact(Func<UserLibraryFileContent, UserLibraryEdit> edit)
        {
            FileStream lockStream;
            try
            {
                lockStream = AcquireLock();
            }
            catch (Exception exception)
            {
                return exception.Message;
            }

            using (lockStream)
            {
                // Re-read under the lock: whatever another instance saved meanwhile is kept.
                UserLibraryFileContent content = Read();
                if (content.State == UserLibraryFileState.Unreadable)
                {
                    return content.Error;
                }

                UserLibraryEdit userLibraryEdit = edit(content);
                if (userLibraryEdit == null)
                {
                    return "The library could not be changed.";
                }

                if (userLibraryEdit.IsNoChange)
                {
                    return null;
                }

                if (userLibraryEdit.Result == null)
                {
                    return userLibraryEdit.Error;
                }

                try
                {
                    Write(userLibraryEdit.Result);
                }
                catch (Exception exception)
                {
                    return string.Format(CultureInfo.CurrentCulture, "{0} could not be written: {1}", FileName, exception.Message);
                }

                return null;
            }
        }

        /// <summary>
        /// Writes <paramref name="constructionManager"/> as the whole file: verified to read back, temporary file + atomic replace, previous file
        /// kept as <c>.bak</c>, temporary file always removed. Throws on failure (the file is then as it was). Call it under the lock.
        /// </summary>
        public void Write(ConstructionManager constructionManager)
        {
            string json = constructionManager.ToJsonObject()?.ToJsonString() ?? throw new InvalidOperationException("The library could not be serialised.");

            // Never write something that would not read back.
            if (Parse(json, out string error) == null)
            {
                throw new InvalidOperationException("The library would not read back: " + error);
            }

            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path));
            BeforeWrite?.Invoke(Path);

            string path_Temp = Path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllText(path_Temp, json);
                if (File.Exists(Path))
                {
                    File.Replace(path_Temp, Path, BackupPath, true);
                }
                else
                {
                    File.Move(path_Temp, Path);
                }
            }
            finally
            {
                if (File.Exists(path_Temp))
                {
                    File.Delete(path_Temp);
                }
            }
        }

        /// <summary>
        /// Raises <paramref name="handler"/> for every subscriber, each isolated: one that throws neither stops the others nor turns the
        /// successful change into a failure. Call it AFTER the lock is released.
        /// </summary>
        public static void Notify(EventHandler handler, object sender)
        {
            foreach (EventHandler subscriber in handler?.GetInvocationList().Cast<EventHandler>() ?? Enumerable.Empty<EventHandler>())
            {
                try
                {
                    subscriber(sender, EventArgs.Empty);
                }
                catch (Exception)
                {
                }
            }
        }

        internal static ConstructionManager Parse(string text, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(text))
            {
                error = "the file is empty";
                return null;
            }

            try
            {
                JsonObject jsonObject = JsonNode.Parse(text) as JsonObject;
                string type = (string)jsonObject?["_type"];
                if (jsonObject == null || type == null || !type.Contains(nameof(ConstructionManager)))
                {
                    error = "it is not a SAM construction manager";
                    return null;
                }

                return new ConstructionManager(jsonObject);
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return null;
            }
        }

        // Exclusive lock for the read-edit-write. DeleteOnClose removes it when released (also when the process ends).
        private FileStream AcquireLock()
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path));
            Stopwatch stopwatch = Stopwatch.StartNew();
            while (true)
            {
                try
                {
                    return new FileStream(LockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 1, FileOptions.DeleteOnClose);
                }
                catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
                {
                    if (stopwatch.Elapsed > lockTimeout)
                    {
                        throw new IOException(string.Format(CultureInfo.CurrentCulture, "{0} is being saved by another SAM window; try again in a moment.", FileName), exception);
                    }

                    Thread.Sleep(50);
                }
            }
        }

        // A reader shares with a concurrent atomic replace; a brief sharing violation is retried.
        private static string ReadAllTextShared(string path)
        {
            for (int attempt = 0; ; attempt++)
            {
                try
                {
                    using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                    using (StreamReader streamReader = new StreamReader(fileStream))
                    {
                        return streamReader.ReadToEnd();
                    }
                }
                catch (IOException) when (attempt < 20)
                {
                    Thread.Sleep(25);
                }
            }
        }
    }
}
