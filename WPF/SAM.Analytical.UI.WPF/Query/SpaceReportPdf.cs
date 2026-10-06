// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Query
    {
        private const int SpaceReportPdfNameMaxLength = 150;

        /// <summary>The longest file name a batch export writes (the NTFS component limit).</summary>
        private const int SpaceReportPdfFileNameMaxLength = 255;

        /// <summary>The longest path a batch export writes, its ".tmp" staging file included: MAX_PATH less its terminator.</summary>
        internal const int SpaceReportPdfPathMaxLength = 259;

        /// <summary>The shortest Space-name part a batch file name may be cut to before the folder is refused as too long.</summary>
        private const int SpaceReportPdfFileStemMinLength = 16;

        private static readonly HashSet<string> reservedFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "CON", "PRN", "AUX", "NUL",
            "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
            "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
        };

        /// <summary>
        /// Resolves the selection for a one-Space report PDF: exactly one Space. The Space is looked up by Guid in
        /// the current model, so a selection that went stale after an edit reports the model's current Space, and
        /// one that was removed is refused rather than reported from an old copy. Several Spaces are refused, never
        /// reduced to the first.
        /// </summary>
        /// <param name="refusal">Why no Space was resolved, for the user; null when one was.</param>
        public static Space? SpaceReportPdfSpace(AnalyticalModel? analyticalModel, IEnumerable<Space>? spaces, SpaceReportPdf spaceReportPdf, out string? refusal)
        {
            refusal = null;

            AdjacencyCluster? adjacencyCluster = analyticalModel?.AdjacencyCluster;
            if (adjacencyCluster == null)
            {
                refusal = "Open an analytical model first.";
                return null;
            }

            List<Space> spaces_Selected = spaces?.Where(x => x != null).GroupBy(x => x.Guid).Select(x => x.First()).ToList() ?? new List<Space>();
            if (spaces_Selected.Count == 0)
            {
                refusal = string.Format("Select one Space, then choose {0}.", spaceReportPdf?.Title);
                return null;
            }

            if (spaces_Selected.Count > 1)
            {
                refusal = string.Format("{0} Spaces are selected. The {1} is created for one Space at a time: select a single Space.", spaces_Selected.Count, spaceReportPdf?.Title);
                return null;
            }

            Space? space = adjacencyCluster.GetObject<Space>(spaces_Selected[0].Guid);
            if (space == null)
            {
                refusal = "The selected Space is no longer in the model. Select it again.";
                return null;
            }

            return space;
        }

        /// <summary>
        /// "&lt;Space name&gt; - &lt;report name&gt;.pdf", with characters Windows does not allow in a file name
        /// replaced by "_". A Space with no usable name falls back to its Guid, the same subject the report prints.
        /// </summary>
        public static string SpaceReportPdfFileName(Space? space, SpaceReportPdf spaceReportPdf)
        {
            return SpaceReportPdfFileStem(space) + " - " + spaceReportPdf?.Name + ".pdf";
        }

        /// <summary>
        /// The file names of a batch export (<see cref="SpaceReportPdfBatch"/>): one per Space and report, keyed by
        /// the Space's Guid and the report's <see cref="SpaceReportPdf.Id"/>.
        /// <para>
        /// Each name starts as <see cref="SpaceReportPdfFileName"/>. Its Space-name part is shortened further only
        /// when the name would not fit in <paramref name="directory"/> (a path, ".tmp" staging suffix included, of at
        /// most <see cref="SpaceReportPdfPathMaxLength"/> characters). Names that are then equal ignoring case -
        /// duplicate Space names, names equal after sanitising or truncation - ALL get " [first 8 hex digits of the
        /// Space Guid]", not only the second one met, so a name depends on the set of Spaces and never on their
        /// order. A name that still collides (two Guids sharing 8 hex digits, or a Space literally named like a
        /// suffixed one) gets the full 32-digit Guid instead.
        /// </para>
        /// </summary>
        /// <param name="directory">The output folder; null plans names without a path-length limit.</param>
        /// <exception cref="PathTooLongException">When <paramref name="directory"/> leaves no room for a readable name.</exception>
        public static Dictionary<(Guid SpaceGuid, string ReportId), string> SpaceReportPdfFileNames(IEnumerable<Space>? spaces, IEnumerable<SpaceReportPdf>? spaceReportPdfs, string? directory = null)
        {
            List<Space> spaces_Unique = spaces?.Where(x => x != null).GroupBy(x => x.Guid).Select(x => x.First()).ToList() ?? new List<Space>();
            List<SpaceReportPdf> spaceReportPdfs_Unique = spaceReportPdfs?.Where(x => x != null).GroupBy(x => x.Id).Select(x => x.First()).ToList() ?? new List<SpaceReportPdf>();

            //The whole path, staged as "<name>.tmp", stays within MAX_PATH.
            int nameMaxLength = SpaceReportPdfFileNameMaxLength;
            if (!string.IsNullOrEmpty(directory))
            {
                string directory_Full = Path.TrimEndingDirectorySeparator(Path.GetFullPath(directory));
                nameMaxLength = Math.Min(nameMaxLength, SpaceReportPdfPathMaxLength - directory_Full.Length - 1 - ".tmp".Length);
            }

            int suffixMaxLength = " [".Length + 32 + "]".Length;
            int reportMaxLength = spaceReportPdfs_Unique.Count == 0 ? 0 : spaceReportPdfs_Unique.Max(x => (" - " + x.Name + ".pdf").Length);

            int stemMaxLength = nameMaxLength - reportMaxLength - suffixMaxLength;
            if (stemMaxLength < SpaceReportPdfFileStemMinLength)
            {
                throw new PathTooLongException(string.Format("The folder path is too long for the report file names: choose a folder with a shorter path.\n\n{0}", directory));
            }

            Dictionary<Guid, string> stems = new Dictionary<Guid, string>();
            foreach (Space space in spaces_Unique)
            {
                string stem = SpaceReportPdfFileStem(space);
                if (stem.Length > stemMaxLength)
                {
                    stem = stem.Substring(0, stemMaxLength).TrimEnd('.', ' ');
                }

                stems[space.Guid] = stem;
            }

            List<(Guid SpaceGuid, SpaceReportPdf SpaceReportPdf)> keys = spaces_Unique.SelectMany(x => spaceReportPdfs_Unique.Select(y => (x.Guid, y))).ToList();

            //0: plain, 1: short Guid, 2: full Guid. Every member of a colliding group moves up together.
            Dictionary<(Guid, string), int> levels = keys.ToDictionary(x => (x.SpaceGuid, x.SpaceReportPdf.Id), _ => 0);

            string Name((Guid SpaceGuid, SpaceReportPdf SpaceReportPdf) key)
            {
                string suffix = levels[(key.SpaceGuid, key.SpaceReportPdf.Id)] switch
                {
                    0 => string.Empty,
                    1 => " [" + key.SpaceGuid.ToString("N").Substring(0, 8) + "]",
                    _ => " [" + key.SpaceGuid.ToString("N") + "]",
                };

                return stems[key.SpaceGuid] + " - " + key.SpaceReportPdf.Name + suffix + ".pdf";
            }

            for (int level = 1; level <= 2; level++)
            {
                List<(Guid SpaceGuid, SpaceReportPdf SpaceReportPdf)> colliding = keys
                    .GroupBy(Name, StringComparer.OrdinalIgnoreCase)
                    .Where(x => x.Count() > 1)
                    .SelectMany(x => x)
                    .ToList();

                if (colliding.Count == 0)
                {
                    break;
                }

                foreach ((Guid SpaceGuid, SpaceReportPdf SpaceReportPdf) key in colliding)
                {
                    levels[(key.SpaceGuid, key.SpaceReportPdf.Id)] = level;
                }
            }

            return keys.ToDictionary(x => (x.SpaceGuid, x.SpaceReportPdf.Id), Name);
        }

        /// <summary>
        /// The Space part of a report file name: the sanitised Space name, or "Space &lt;Guid&gt;" when it has none.
        /// </summary>
        private static string SpaceReportPdfFileStem(Space? space)
        {
            string? name = SafeFileName(space?.Name);
            if (string.IsNullOrEmpty(name))
            {
                name = space == null ? "Space" : "Space " + space.Guid.ToString("D");
            }

            return name;
        }

        private static string? SafeFileName(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            HashSet<char> invalidChars = new HashSet<char>(Path.GetInvalidFileNameChars());

            char[] chars = text.Trim().Select(x => invalidChars.Contains(x) || char.IsControl(x) ? '_' : x).ToArray();

            string result = new string(chars);
            if (result.Length > SpaceReportPdfNameMaxLength)
            {
                result = result.Substring(0, SpaceReportPdfNameMaxLength);
            }

            //Windows drops trailing dots and spaces from a file name.
            result = result.TrimEnd('.', ' ');

            if (string.IsNullOrEmpty(result) || result.All(x => x == '_'))
            {
                return null;
            }

            if (reservedFileNames.Contains(result))
            {
                result = "_" + result;
            }

            return result;
        }
    }
}
