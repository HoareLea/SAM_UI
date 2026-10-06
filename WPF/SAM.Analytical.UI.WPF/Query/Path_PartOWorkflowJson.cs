// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.IO;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Query
    {
        /// <summary>
        /// Where the TAS workflow's own JSON export of a run's model lands: <c>&lt;run&gt;.json</c>, beside
        /// the run's TBD and named from it.
        ///
        /// <para><b>Why this exists as its own authority</b></para>
        /// <para>
        /// <c>WorkflowCalculator</c> finishes every TAS run with a "Saving Model" step that writes the model
        /// it is about to return as plain JSON, at the directory and base name of
        /// <c>WorkflowSettings.Path_TBD</c> - and this reproduces <b>that</b> derivation from <b>that</b>
        /// same path, which is the run's TBD that <see cref="Modify.RunPartOSimulation"/> composed and handed
        /// to the workflow. So the file named here is the one the workflow actually wrote for this run,
        /// never a guess and never another run's.
        /// </para>
        /// <para>
        /// That matters because the only thing done with this path is a deletion. A Part O run's
        /// authoritative persisted model is the native <c>&lt;run&gt;.sam</c> at
        /// <see cref="Path_PartORunModel(string)"/>; once it is written, the workflow's JSON beside it is the
        /// same model again as plain text - on a real project a very large file, kept for every round of an
        /// optimisation, carrying none of the provenance a review is validated against. Removing it is a
        /// cleanup of one exactly-known file, which is why it is derived here rather than by scanning a
        /// directory for things that look like run models.
        /// </para>
        /// <para>
        /// <b>Only Part O removes it.</b> Ordinary TAS workflows in SAM keep writing and keeping their
        /// <c>&lt;project&gt;.json</c> exactly as before; nothing here changes what the workflow does.
        /// </para>
        /// </summary>
        /// <param name="path_TBD">The run's own TBD - the very path handed to the workflow as <c>Path_TBD</c>.</param>
        /// <returns>The workflow's JSON export path, or null where there is no TBD path to derive one from.</returns>
        internal static string Path_PartOWorkflowJson(string path_TBD)
        {
            if (string.IsNullOrWhiteSpace(path_TBD))
            {
                return null;
            }

            string directory = Path.GetDirectoryName(path_TBD);
            string fileName = Path.GetFileNameWithoutExtension(path_TBD);

            if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(fileName))
            {
                return null;
            }

            //Composed exactly as WorkflowCalculator composes it: Path.Combine over the TBD's directory and
            //base name, not ChangeExtension - so a base name containing a dot resolves to the same file
            //there and here.
            return Path.Combine(directory, fileName + ".json");
        }
    }
}
