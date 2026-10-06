// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.IO;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Query
    {
        /// <summary>
        /// Where one Part O run's persisted analytical model lives: beside its own results file, named from
        /// it - <c>&lt;project&gt;.sam</c> for a baseline, <c>&lt;project&gt;-Opt01.sam</c> for a round,
        /// <c>&lt;project&gt;-OptMax.sam</c> for the capacity envelope.
        ///
        /// <para><b>The one naming authority</b></para>
        /// <para>
        /// Every writer and every reader of the per-run model derives its path here, from the run's own TSD
        /// and nothing else - exactly as <see cref="Path_TM59Report(string)"/> does for the report. So a run
        /// model always lands beside the results it was produced from, no run can overwrite another's, and
        /// the extension is stated in one place rather than assumed at each call site. The per-iteration
        /// naming (<see cref="PartOSimulationContext.ProjectName_Iteration(int)"/>) is what keeps the TSDs
        /// apart, and this inherits it, giving each run the matching set:
        /// <c>&lt;run&gt;.sam</c>, <c>&lt;run&gt;.tbd</c>, <c>&lt;run&gt;.tsd</c>,
        /// <c>&lt;run&gt;-TM59.txt</c>.
        /// </para>
        ///
        /// <para><b>.sam, and written by the native writer</b></para>
        /// <para>
        /// <c>.sam</c> is SAM's own persisted model form - a compressed archive, produced by
        /// <c>Core.Convert.ToFile</c> under <c>SAMFileType.SAM</c>, which
        /// <c>Core.Query.SAMFileType(string)</c> selects from this very extension. It is what the Open and
        /// Save As dialogs already offer first, and what <c>Core.Convert.ToSAM</c> reads back, so a run model
        /// written here reopens through the ordinary production path with no special case anywhere.
        /// </para>
        /// <para>
        /// The run model was previously <c>&lt;project&gt;.json</c>: the same content as plain text, which on
        /// a real project is a very large file to keep beside every round of an optimisation. Nothing about
        /// the model changed with the extension - the provenance and the scenarios stamped on it are the
        /// same, and it is the model itself that carries them, not its filename.
        /// </para>
        /// <para>
        /// <b>Not the TAS workflow's own <c>&lt;project&gt;.json</c>.</b> <c>WorkflowCalculator</c> writes one
        /// of those for every TAS run in SAM, Part O or not, and that behaviour is unchanged. This is the
        /// Part O run's own evidence, and only the file at this path carries the provenance a later session
        /// reviews from - which is why, on a Part O run and only there, the workflow's copy is removed once
        /// the file named here has been written: see <see cref="Modify.PersistPartORunModel"/> for the
        /// ordering that keeps that safe, and <see cref="Path_PartOWorkflowJson(string)"/> for exactly which
        /// file is removed. So a completed Part O run leaves <c>&lt;run&gt;.sam</c>, <c>&lt;run&gt;.tbd</c>,
        /// <c>&lt;run&gt;.tsd</c> and <c>&lt;run&gt;-TM59.txt</c> - and no second copy of the model as plain
        /// text.
        /// </para>
        /// </summary>
        /// <param name="path_TSD">The run's own results file.</param>
        /// <returns>The run model's path, or null where there is no results path to derive one from.</returns>
        internal static string Path_PartORunModel(string path_TSD)
        {
            if (string.IsNullOrWhiteSpace(path_TSD))
            {
                return null;
            }

            string directory = Path.GetDirectoryName(path_TSD);
            string fileName = Path.GetFileNameWithoutExtension(path_TSD);

            if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(fileName))
            {
                return null;
            }

            return Path.Combine(directory, fileName + ".sam");
        }
    }
}
