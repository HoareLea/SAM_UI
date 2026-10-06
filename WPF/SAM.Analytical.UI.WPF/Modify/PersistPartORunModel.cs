// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System;
using System.IO;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        /// <summary>
        /// Writes one Part O run's authoritative persisted model - the native <c>&lt;run&gt;.sam</c> - and,
        /// <b>only once that has succeeded</b>, removes the redundant <c>&lt;run&gt;.json</c> the TAS
        /// workflow wrote for the same run.
        ///
        /// <para><b>Why the two belong in one place</b></para>
        /// <para>
        /// They are one rule, not two steps: a Part O run leaves exactly one reviewable model artifact
        /// behind. Splitting the write from the cleanup is how the ordering between them stops being
        /// guaranteed - and the ordering is the whole safety property, because the JSON is the only copy of
        /// the run model that exists until the <c>.sam</c> is on disk.
        /// </para>
        ///
        /// <para><b>The order, and what each failure costs</b></para>
        /// <list type="number">
        /// <item>
        /// The stamped model is written to <see cref="Query.Path_PartORunModel(string)"/> through
        /// <c>Core.Convert.ToFile</c> under <c>SAMFileType.SAM</c> - SAM's own writer, the one Save As uses,
        /// so the file reopens through the ordinary Open path.
        /// </item>
        /// <item>
        /// <b>If that write fails, nothing is deleted.</b> The workflow's JSON stays exactly where it is and
        /// remains the fallback copy of the run model. A persistence failure must never become the loss of
        /// the model altogether, so the failing case is strictly the pre-existing behaviour plus a note.
        /// </item>
        /// <item>
        /// Only after a successful write is <see cref="Query.Path_PartOWorkflowJson(string)"/> removed - one
        /// exactly-known file, derived from this run's own TBD, never a directory sweep.
        /// </item>
        /// <item>
        /// <b>If that deletion fails, the run is still a success.</b> The <c>.sam</c> is written and
        /// authoritative; a JSON that could not be removed is a stale extra file, not a wrong answer. It is
        /// reported as a note and nothing else changes - failing a completed simulation over a file that
        /// happened to be locked would be the far worse outcome.
        /// </item>
        /// </list>
        ///
        /// <para><b>Scope</b></para>
        /// <para>
        /// This is the Part O seam and only the Part O seam. <c>WorkflowCalculator</c> is untouched and
        /// keeps writing its <c>&lt;project&gt;.json</c> for every TAS run in SAM; ordinary non-Part-O
        /// workflows keep theirs.
        /// </para>
        /// </summary>
        /// <param name="analyticalModel">The stamped model to persist - scenarios and provenance already on it.</param>
        /// <param name="path_TSD">The run's results file, which names the model. Not read.</param>
        /// <param name="path_TBD">The run's TBD, which names the workflow's redundant JSON. Not read.</param>
        /// <param name="note">What went wrong, or null where the model was persisted and the JSON removed.</param>
        /// <returns>Whether the authoritative <c>.sam</c> was written.</returns>
        internal static bool PersistPartORunModel(AnalyticalModel analyticalModel, string path_TSD, string path_TBD, out string note)
        {
            note = null;

            if (analyticalModel is null)
            {
                return false;
            }

            string path_Model = Query.Path_PartORunModel(path_TSD);
            if (string.IsNullOrWhiteSpace(path_Model))
            {
                return false;
            }

            bool persisted;

            try
            {
                persisted = Core.Convert.ToFile(analyticalModel, path_Model, SAMFileType.SAM);
            }
            catch (Exception exception)
            {
                note = string.Format("The analytical model with its simulation-result provenance could not be written to '{0}', so reopening that file later will not offer a review of these results. This session is unaffected. ({1})", path_Model, exception.Message);

                return false;
            }

            if (!persisted)
            {
                note = string.Format("The analytical model with its simulation-result provenance could not be written to '{0}', so reopening that file later will not offer a review of these results. This session is unaffected.", path_Model);

                return false;
            }

            //Past this line the run model is safely on disk, so the workflow's plain-text copy of it is
            //redundant - and everything below is cleanup that cannot change the outcome.
            string path_Json = Query.Path_PartOWorkflowJson(path_TBD);
            if (string.IsNullOrWhiteSpace(path_Json) || !File.Exists(path_Json))
            {
                return true;
            }

            try
            {
                File.Delete(path_Json);
            }
            catch (Exception exception)
            {
                note = string.Format("The redundant workflow model file '{0}' could not be removed, so it remains beside the run. The model at '{1}' is the authoritative one and this run is otherwise unaffected. ({2})", path_Json, path_Model, exception.Message);
            }

            return true;
        }
    }
}
