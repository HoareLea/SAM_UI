// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Results &gt; Part O &gt; Remove Results: a cleaned COPY of the open model for Mixed Design, and the Mixed Design
    /// baseline check of that copy.
    ///
    /// <para><b>No rule of its own</b></para>
    /// <para>
    /// What is removed is <c>SAM.Analytical.Modify.RemovePartORunState</c>'s decision, and whether the copy is a clean
    /// baseline is <c>SAM.Analytical.Query.PartOBaselineFindings</c>'s - the validator Mixed Design itself asks. This
    /// class only carries the two answers to the window and writes the copy.
    /// </para>
    ///
    /// <para><b>Never destructive</b></para>
    /// <para>
    /// The open model is not changed: the cleaner returns a copy. The copy is written only to a NEW file - the open
    /// model's own file is refused - and no TAS or result file is deleted from disk. The check shown after saving is
    /// the check of the file as read back from disk, not of the copy in memory.
    /// </para>
    /// </summary>
    public sealed class PartORemoveResults
    {
        private PartORemoveResults(AnalyticalModel? cleaned, List<string> removed, List<string> kept, List<PartOMaterialisationRefusal> findings)
        {
            Cleaned = cleaned;
            Removed = removed;
            Kept = kept;
            Findings = findings;
        }

        /// <summary>The cleaned copy, or null where there was no model.</summary>
        public AnalyticalModel? Cleaned { get; }

        /// <summary>What SAM removed from the copy, one line per kind.</summary>
        public List<string> Removed { get; }

        /// <summary>Part O state SAM deliberately left in the copy, and why.</summary>
        public List<string> Kept { get; }

        /// <summary>The Mixed Design baseline check of the cleaned copy; empty is PASS.</summary>
        public List<PartOMaterialisationRefusal> Findings { get; }

        public bool IsClean => Cleaned is not null && Findings.Count == 0;

        /// <summary>Whether the copy differs from the open model at all - nothing to save otherwise.</summary>
        public bool HasChanges => Removed.Count != 0;

        /// <summary>Where the copy was saved, once it has been.</summary>
        public string? Path_Saved { get; private set; }

        /// <summary>The Mixed Design baseline check of the saved file, read back from disk; null until saved.</summary>
        public List<PartOMaterialisationRefusal>? Findings_Saved { get; private set; }

        public bool IsSavedClean => Findings_Saved is not null && Findings_Saved.Count == 0;

        public static PartORemoveResults Create(AnalyticalModel? analyticalModel)
        {
            if (analyticalModel is null)
            {
                return new PartORemoveResults(null, [], [], []);
            }

            AnalyticalModel cleaned = analyticalModel.RemovePartORunState(out List<string> removed, out List<string> kept);

            return new PartORemoveResults(cleaned, removed ?? [], kept ?? [], Analytical.Query.PartOBaselineFindings(cleaned) ?? []);
        }

        /// <summary>
        /// The file name offered for the copy: beside the open model, <c>&lt;model&gt;-Cleaned.sam</c>. Null where the
        /// model has never been saved, so the dialog starts wherever it last was.
        /// </summary>
        public static string? DefaultPath(string? path_Model)
        {
            if (string.IsNullOrWhiteSpace(path_Model))
            {
                return null;
            }

            string? directory = Path.GetDirectoryName(path_Model);
            string name = Path.GetFileNameWithoutExtension(path_Model);

            return Path.Combine(directory ?? string.Empty, string.Format("{0}-Cleaned.sam", name));
        }

        /// <summary>
        /// Writes the cleaned copy to <paramref name="path"/> and checks the file as read back. Returns why it was not
        /// written, or null.
        /// </summary>
        /// <param name="path">The new file.</param>
        /// <param name="path_Model">The open model's own file, which is never overwritten.</param>
        public string? Save(string? path, string? path_Model)
        {
            if (Cleaned is null)
            {
                return "There is no model to save.";
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                return "No file was chosen.";
            }

            if (!string.IsNullOrWhiteSpace(path_Model) && string.Equals(Path.GetFullPath(path), Path.GetFullPath(path_Model), StringComparison.OrdinalIgnoreCase))
            {
                return "That is the open model's own file. Remove Results never overwrites it: choose a new file name.";
            }

            if (!Core.Convert.ToFile(new Core.IJSAMObject[] { Cleaned }, path))
            {
                return string.Format("The cleaned copy could not be written to '{0}'.", path);
            }

            AnalyticalModel? reopened = Core.Convert.ToSAM<AnalyticalModel>(path)?.FirstOrDefault(x => x is not null);
            if (reopened is null)
            {
                return string.Format("The cleaned copy was written to '{0}' but could not be read back.", path);
            }

            Path_Saved = path;
            Findings_Saved = Analytical.Query.PartOBaselineFindings(reopened) ?? [];

            return null;
        }
    }
}
