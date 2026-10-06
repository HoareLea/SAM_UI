// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.IO;

namespace SAM.Analytical.UI
{
    /// <summary>
    /// Which of Candidate B's files <b>this attempt</b> actually created or updated.
    ///
    /// <para><b>Why this has to exist</b></para>
    /// <para>
    /// Candidate B writes to deterministic paths derived from the run's own project name, which is what
    /// makes a run reopenable. It also means that when an attempt fails - a locked file, a refused
    /// conversion, a cancelled simulation - the previous attempt's <c>.tbd</c>, <c>.tsd</c>, <c>.tpd</c>,
    /// TM59 report and pairing record are still sitting at exactly the paths the next attempt would use.
    /// Reporting "the thermal source produced <c>Flat1-It3B.tsd</c>" because that file happens to exist is
    /// then a lie told with a real filename, which is the most convincing kind.
    /// </para>
    ///
    /// <para><b>How it decides</b></para>
    /// <para>
    /// Every fixed Candidate B path is <see cref="Snapshot"/>ed before a single byte is written: whether it
    /// existed, and if so its length and last write time. A file is claimed as this attempt's only where it
    /// exists now AND either did not exist at the snapshot or differs from it. A file that exists and is
    /// byte-for-byte where the snapshot left it is an earlier attempt's, and claiming it
    /// <b>refuses</b> - the caller fails the stage rather than presenting a stale artifact as evidence.
    /// </para>
    /// <para>
    /// <b>An unsnapshotted path also refuses.</b> If a path was never tracked there is no way to tell a
    /// file this attempt wrote from one it inherited, and guessing in the safe-looking direction is how a
    /// stale artifact gets through. Fail closed.
    /// </para>
    /// <para>
    /// This is deliberately separate from, and does not weaken, the stale-output deletion the no-IZAM
    /// source and the bridge already do for themselves. Those make a file be this run's; this proves it.
    /// </para>
    /// </summary>
    public class PartOIteration3Artifacts
    {
        private readonly Dictionary<string, Fingerprint> fingerprints = new(StringComparer.OrdinalIgnoreCase);

        private readonly List<string> claimed = [];

        public PartOIteration3Artifacts(Guid guid_Run)
        {
            Guid_Run = guid_Run == Guid.Empty ? Guid.NewGuid() : guid_Run;
        }

        /// <summary>This attempt's identity. Every claimed artifact belongs to it and to no earlier one.</summary>
        public Guid Guid_Run { get; }

        /// <summary>Every artifact claimed so far, in claim order.</summary>
        public List<string> Claimed => [.. claimed];

        /// <summary>
        /// Records what is at each fixed path before the attempt writes anything. Call once, at attempt
        /// start, for every path Candidate B may write.
        /// </summary>
        public void Snapshot(IEnumerable<string> paths)
        {
            foreach (string path in paths ?? [])
            {
                if (string.IsNullOrWhiteSpace(path))
                {
                    continue;
                }

                fingerprints[path] = Fingerprint.Read(path);
            }
        }

        /// <summary>Whether a path was snapshotted at attempt start.</summary>
        public bool IsTracked(string path)
        {
            return !string.IsNullOrWhiteSpace(path) && fingerprints.ContainsKey(path);
        }

        /// <summary>
        /// Claims a path as an artifact of this attempt, or says why it is not one.
        /// </summary>
        /// <param name="path">The fixed path a stage was expected to write.</param>
        /// <param name="artifact">The ledger entry, where the claim succeeded.</param>
        /// <param name="refusal">Why it is not this attempt's, where it is not.</param>
        public bool TryClaim(string path, out string artifact, out string refusal)
        {
            artifact = null;

            if (!IsWritten(path, out refusal))
            {
                return false;
            }

            artifact = string.Format("{0} ({1})", path, fingerprints[path].Exists ? "updated" : "created");

            claimed.Add(artifact);

            return true;
        }

        /// <summary>
        /// Whether this attempt demonstrably wrote a path - the same rule <see cref="TryClaim(string, out string, out string)"/>
        /// applies, without claiming the file as an artifact. For a file this attempt rewrites but does
        /// not own, such as Reference A's TM59 report beside Reference A's own results.
        /// </summary>
        public bool IsWritten(string path, out string refusal)
        {
            refusal = null;

            if (string.IsNullOrWhiteSpace(path))
            {
                refusal = "No path was stated, so no artifact of this attempt could be identified.";

                return false;
            }

            if (!fingerprints.TryGetValue(path, out Fingerprint fingerprint))
            {
                refusal = string.Format(
                    "'{0}' was not recorded before this attempt started, so a file this attempt wrote cannot be told apart from one left by an earlier attempt. It is not reported as evidence.",
                    path);

                return false;
            }

            Fingerprint fingerprint_Now = Fingerprint.Read(path);

            if (!fingerprint_Now.Exists)
            {
                refusal = string.Format("'{0}' does not exist, so this attempt did not produce it.", path);

                return false;
            }

            if (fingerprint.Exists && fingerprint.Length == fingerprint_Now.Length && fingerprint.Ticks == fingerprint_Now.Ticks)
            {
                refusal = string.Format(
                    "'{0}' is unchanged since this attempt started, so it was left by an earlier attempt and is not evidence of this one.",
                    path);

                return false;
            }

            return true;
        }

        /// <summary>
        /// Claims every stated path, or refuses the whole set. Used where a stage is expected to leave more
        /// than one file and any missing or stale one is a failure of that stage.
        /// </summary>
        public bool TryClaim(IEnumerable<string> paths, out List<string> artifacts, out List<string> refusals)
        {
            artifacts = [];
            refusals = [];

            foreach (string path in paths ?? [])
            {
                if (TryClaim(path, out string artifact, out string refusal))
                {
                    artifacts.Add(artifact);
                }
                else
                {
                    refusals.Add(refusal);
                }
            }

            return refusals.Count == 0;
        }

        /// <summary>What is at one path right now - existence, length and last write time, or nothing.</summary>
        public static bool TryRead(string path, out long length, out long ticks_Utc)
        {
            Fingerprint fingerprint = Fingerprint.Read(path);

            length = fingerprint.Length;
            ticks_Utc = fingerprint.Ticks;

            return fingerprint.Exists;
        }

        private readonly struct Fingerprint
        {
            private Fingerprint(bool exists, long length, long ticks)
            {
                Exists = exists;
                Length = length;
                Ticks = ticks;
            }

            internal bool Exists { get; }

            internal long Length { get; }

            internal long Ticks { get; }

            internal static Fingerprint Read(string path)
            {
                if (string.IsNullOrWhiteSpace(path))
                {
                    return new Fingerprint(false, -1, -1);
                }

                try
                {
                    FileInfo fileInfo = new(path);

                    //Refreshed deliberately: FileInfo caches, and this type is read twice for the same path
                    //in one attempt - once at the snapshot and once at the claim.
                    fileInfo.Refresh();

                    return fileInfo.Exists
                        ? new Fingerprint(true, fileInfo.Length, fileInfo.LastWriteTimeUtc.Ticks)
                        : new Fingerprint(false, -1, -1);
                }
                catch
                {
                    //A path that cannot be stat'ed is treated as absent, which claims nothing. Failing
                    //closed here costs an artifact line; failing open would report a file nobody could read.
                    return new Fingerprint(false, -1, -1);
                }
            }
        }
    }
}
