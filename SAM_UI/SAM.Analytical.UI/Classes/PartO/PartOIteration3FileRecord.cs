// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Text.Json.Nodes;

namespace SAM.Analytical.UI
{
    /// <summary>
    /// One file of a Candidate B run, with the fingerprint that proves a later session is looking at the
    /// same file: its length and its last write time.
    ///
    /// <para><b>Why length and write time, and not a hash</b></para>
    /// <para>
    /// This is the rule <c>SimulationResultProvenance</c> already applies to Reference A's own TSD, and the
    /// review path applies it to Candidate B's artifacts for the same reason: it is cheap enough to run on
    /// a multi-gigabyte results file every time the model is reopened, and it is what makes "this file has
    /// been rewritten since the run produced it" answerable without reading the file. A content hash would
    /// be stronger and would not be run, which is weaker.
    /// </para>
    /// <para>
    /// <b><see cref="Current"/> is the whole check.</b> A missing file, a different length or a different
    /// write time all refuse, by name - see <c>Modify.ReviewPartOIteration3</c>.
    /// </para>
    /// </summary>
    public class PartOIteration3FileRecord
    {
        public PartOIteration3FileRecord(string role, string path, long length, long ticks_Utc)
        {
            Role = role;
            Path = path;
            Length = length;
            Ticks_Utc = ticks_Utc;
        }

        /// <summary>What this file is - the thermal source TSD, the TPD, the bridge TSD, the B model.</summary>
        public string Role { get; }

        /// <summary>Where it was written.</summary>
        public string Path { get; }

        /// <summary>Its length in bytes when the run recorded it. -1 where it was not present.</summary>
        public long Length { get; }

        /// <summary>Its UTC last write time in ticks when the run recorded it. -1 where not present.</summary>
        public long Ticks_Utc { get; }

        /// <summary>Whether the file on disk right now is still the one this record describes.</summary>
        public bool Current(out string refusal)
        {
            refusal = null;

            if (string.IsNullOrWhiteSpace(Path))
            {
                refusal = string.Format("The Iteration 3 record names no path for its {0}, so it cannot be validated.", Role);

                return false;
            }

            if (!PartOIteration3Artifacts.TryRead(Path, out long length, out long ticks))
            {
                refusal = string.Format("The Iteration 3 {0} is no longer at '{1}'.", Role, Path);

                return false;
            }

            if (length != Length || ticks != Ticks_Utc)
            {
                refusal = string.Format(
                    "The Iteration 3 {0} at '{1}' has been rewritten since the run produced it, so it is no longer the file this comparison was built from.",
                    Role,
                    Path);

                return false;
            }

            return true;
        }

        public JsonObject ToJsonObject()
        {
            return ToJsonObject(null);
        }

        /// <param name="path_Record">
        /// The pairing record this is written into: the path is then written relative to its folder
        /// (<see cref="PartOSidecarPaths"/>). Null writes it absolute, as given.
        /// </param>
        public JsonObject ToJsonObject(string path_Record)
        {
            JsonObject result = new()
            {
                { "Role", Role },
            };

            PartOSidecarPaths.Write(result, "Path", "Locator", Path, path_Record, PartOIteration3Record.Directory_Root(path_Record));

            result.Add("Length", Length);
            result.Add("Ticks_Utc", Ticks_Utc);

            return result;
        }

        public static PartOIteration3FileRecord FromJsonObject(JsonObject jsonObject)
        {
            return FromJsonObject(jsonObject, null);
        }

        /// <param name="path_Record">Where the record is NOW: a locator is resolved against its folder.</param>
        public static PartOIteration3FileRecord FromJsonObject(JsonObject jsonObject, string path_Record)
        {
            return jsonObject is null
                ? null
                : new PartOIteration3FileRecord(
                    PartOIteration3Json.Text(jsonObject, "Role"),
                    PartOSidecarPaths.Read(jsonObject, "Path", "Locator", path_Record),
                    PartOIteration3Json.Integer(jsonObject, "Length", -1),
                    PartOIteration3Json.Integer(jsonObject, "Ticks_Utc", -1));
        }

        public override string ToString()
        {
            return string.Format("{0}: {1} ({2} bytes)", Role, Path, Length);
        }
    }
}
