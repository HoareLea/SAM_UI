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
            string? name = SafeFileName(space?.Name);
            if (string.IsNullOrEmpty(name))
            {
                name = space == null ? "Space" : "Space " + space.Guid.ToString("D");
            }

            return name + " - " + spaceReportPdf?.Name + ".pdf";
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
