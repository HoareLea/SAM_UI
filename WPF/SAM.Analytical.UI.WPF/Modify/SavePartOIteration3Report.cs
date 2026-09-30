// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.UI;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        /// <summary>
        /// Persists one <b>successful</b> Approved Document O Iteration 3 (A/B) review as a report beside
        /// the pairing record it belongs to: the readable <c>&lt;run&gt;-Iteration3-Review.txt</c> and its
        /// structured sibling <c>&lt;run&gt;-Iteration3-Review.json</c>.
        ///
        /// <para><b>Only a completed pairing writes, and that is the whole overwrite rule</b></para>
        /// <para>
        /// A refused review writes nothing at all. That is not tidiness: once Reference A's design state
        /// moves, live Review correctly refuses the pairing - and if a refusal still wrote a report, the
        /// last report that actually described a real A/B comparison would be replaced by one that
        /// describes a refusal. So the previous successful report survives every later refusal, and the
        /// window offers it explicitly as <b>historical</b> rather than as the current answer.
        /// </para>
        ///
        /// <para><b>It persists the review, and produces no engineering of its own</b></para>
        /// <para>
        /// Both files are composed from <see cref="PartOIteration3ReportText"/> and
        /// <see cref="PartOIteration3ReportJson"/> over the result the review already computed. Nothing
        /// here reads a model, a results file or a TAS document, and no statistic is recalculated.
        /// </para>
        ///
        /// <para><b>Deterministic</b></para>
        /// <para>
        /// Invariant culture, every row rather than whatever filter a window happened to hold, and the
        /// comparison's own identity ordering - so re-saving an unchanged completed pairing produces the
        /// same bytes.
        /// </para>
        ///
        /// <para><b>Best effort, and a failure says so</b></para>
        /// <para>
        /// A read-only directory or a locked file must not fail a review that already succeeded, so the
        /// failure is recorded on the result - which puts it in the window's notes - rather than thrown.
        /// </para>
        ///
        /// <para><b>All or nothing on disk, whichever file the failure lands on</b></para>
        /// <para>
        /// Both siblings are composed into temporary files first, so a disk-full or permission failure
        /// while writing <b>content</b> touches neither destination file. What is left is installing two
        /// files under one outcome: the previous <c>path_Report</c> and <c>path_Report_Json</c> (where
        /// they exist) are moved aside to their own backup names, the new pair is moved into place, and
        /// only then are the backups deleted. If installing the second file fails - a lock, a permission
        /// change, a path that has become a directory - whichever new file <b>did</b> make it into place is
        /// removed and the backups are moved straight back, so the pairing is left exactly as it was before
        /// this attempt, never with one sibling from the new attempt sitting beside one from an older one.
        /// A version that instead wrote straight over <c>path_Report</c> before attempting
        /// <c>path_Report_Json</c> - or that only staged the writes but moved the two files straight over
        /// their destinations - would let a failure on the second file leave the first one already replaced
        /// by this attempt's content, while the result (whose <c>RecordReport</c> was cleared in the catch)
        /// does not even admit that the first file changed.
        /// </para>
        /// </summary>
        /// <param name="partOIteration3Result">
        /// The review to persist. Its <c>RecordReport</c> is called either way, so the window can say
        /// where the report is or why there is none.
        /// </param>
        /// <returns>Whether a report was written.</returns>
        internal static bool SavePartOIteration3Report(PartOIteration3Result partOIteration3Result)
        {
            if (partOIteration3Result is null)
            {
                return false;
            }

            if (!partOIteration3Result.IsComplete)
            {
                //Deliberately silent. A refusal is not a failed save - there is nothing to save, and the
                //last successful report of this pairing is left exactly where it is.
                partOIteration3Result.RecordReport(null, null, null);

                return false;
            }

            string path_Report = PartOIteration3Paths.Path_Report_ForRecord(partOIteration3Result.Path_Record);
            string path_Report_Json = PartOIteration3Paths.Path_Report_ForRecord(partOIteration3Result.Path_Record, "json");

            if (string.IsNullOrWhiteSpace(path_Report))
            {
                partOIteration3Result.RecordReport(
                    null,
                    null,
                    "This pairing states no record path, so no report path could be derived from it and the A/B review was not saved.");

                return false;
            }

            List<PartOIteration3Row> rows = PartOIteration3Row.Rows(partOIteration3Result.Comparison);

            //Recorded BEFORE the write, so the report names itself: a file found on its own, a year
            //later, states which file it is as well as which pairing it describes. The paths are derived
            //from the record and do not vary between saves, so this costs no determinism.
            partOIteration3Result.RecordReport(path_Report, path_Report_Json, null);

            //Every path this attempt might touch, none of them the destination itself: the new content is
            //composed here, and whatever already exists at the destination is parked here if it has to
            //move aside. Same directory as the destination throughout, so every move below is a same-volume
            //rename rather than a copy.
            string path_Report_Temp = path_Report + ".tmp";
            string path_Report_Json_Temp = path_Report_Json + ".tmp";
            string path_Report_Backup = path_Report + ".bak";
            string path_Report_Json_Backup = path_Report_Json + ".bak";

            bool movedAside_Report = false;
            bool movedAside_Json = false;
            bool installed_Report = false;
            bool installed_Json = false;

            try
            {
                try
                {
                    PartOOutputPaths.EnsureDirectoryForFile(path_Report_Temp);

                    File.WriteAllText(path_Report_Temp, PartOIteration3ReportText.Text(partOIteration3Result, rows, CultureInfo.InvariantCulture, true));
                    File.WriteAllText(path_Report_Json_Temp, PartOIteration3ReportJson.Text(partOIteration3Result, rows));
                }
                catch (Exception exception)
                {
                    Fail(partOIteration3Result, path_Report, exception);

                    return false;
                }

                try
                {
                    //The previous pair (if any) is parked under its own name rather than deleted, so a
                    //failure below can put it straight back rather than having to reconstruct it.
                    if (File.Exists(path_Report))
                    {
                        File.Move(path_Report, path_Report_Backup, true);
                        movedAside_Report = true;
                    }

                    if (File.Exists(path_Report_Json))
                    {
                        File.Move(path_Report_Json, path_Report_Json_Backup, true);
                        movedAside_Json = true;
                    }

                    File.Move(path_Report_Temp, path_Report, true);
                    installed_Report = true;

                    File.Move(path_Report_Json_Temp, path_Report_Json, true);
                    installed_Json = true;
                }
                catch (Exception exception)
                {
                    //Whichever new file made it into place before the failure is removed, and whichever
                    //old file was moved aside is put straight back - so the pairing ends this attempt
                    //exactly as it started it, whichever of the four moves above is the one that failed.
                    if (installed_Json)
                    {
                        Delete(path_Report_Json);
                    }

                    if (installed_Report)
                    {
                        Delete(path_Report);
                    }

                    if (movedAside_Json)
                    {
                        Restore(path_Report_Json_Backup, path_Report_Json);
                    }

                    if (movedAside_Report)
                    {
                        Restore(path_Report_Backup, path_Report);
                    }

                    Fail(partOIteration3Result, path_Report, exception);

                    return false;
                }
            }
            finally
            {
                //Best-effort: nothing staged or parked aside by this attempt may linger once it is over,
                //on either the success or the failure path.
                Delete(path_Report_Temp);
                Delete(path_Report_Json_Temp);
                Delete(path_Report_Backup);
                Delete(path_Report_Json_Backup);
            }

            return true;
        }

        private static void Fail(PartOIteration3Result partOIteration3Result, string path_Report, Exception exception)
        {
            partOIteration3Result.RecordReport(
                null,
                null,
                string.Format("The Iteration 3 A/B review report could not be written to '{0}': {1}", path_Report, exception.Message));
        }

        /// <summary>Best-effort: moves a parked backup straight back over whatever now sits at <paramref name="path"/>.</summary>
        private static void Restore(string path_Backup, string path)
        {
            try
            {
                File.Move(path_Backup, path, true);
            }
            catch (Exception)
            {
                //A restore that itself cannot complete is the one corner this cannot recover from - the
                //original is still sitting, intact, under its backup name rather than lost, and the failure
                //already recorded on the result is what the window and this attempt's notes report.
            }
        }

        private static void Delete(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception)
            {
                //Best-effort cleanup of a temporary or backup file. Leaving one behind is a housekeeping
                //nuisance, not a correctness problem - unlike leaving the destination files inconsistent.
            }
        }
    }
}
