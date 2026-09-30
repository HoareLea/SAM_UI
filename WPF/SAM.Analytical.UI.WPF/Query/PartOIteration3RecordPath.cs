// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.IO;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Query
    {
        /// <summary>
        /// The Iteration 3 methods in the order an engineer is offered them, and the order every per-method
        /// listing uses. The product methods first; the route check and the published-table method are
        /// validation options.
        /// </summary>
        public static readonly IReadOnlyList<PartOIteration3BehaviourMode> PartOIteration3BehaviourModes =
        [
            PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance,
            PartOIteration3BehaviourMode.SelectedProduct,
            PartOIteration3BehaviourMode.Parity,
            PartOIteration3BehaviourMode.SelectedProductCooling,
        ];

        /// <summary>
        /// Which record file holds one method's pairing for a results file.
        /// <list type="number">
        /// <item>The method's own record, <c>&lt;run&gt;-Iteration3-&lt;tag&gt;.json</c>, in Iteration 3's
        /// reports folder, wherever it exists - where a run writes it.</item>
        /// <item>Otherwise the method's own record BESIDE the results, where every run wrote it before Iteration 3
        /// had its own folder. Read, never written; a legacy project is read as it was saved.</item>
        /// <item>Otherwise the mode-independent <c>&lt;run&gt;-Iteration3.json</c> written before per-method
        /// records existed - but only where the mode recorded inside it IS this method. It is read, never
        /// written, and a method's own record always supersedes it.</item>
        /// <item>Otherwise the method's own path, which does not exist yet.</item>
        /// </list>
        /// </summary>
        public static string PartOIteration3RecordPath(string path_TSD, PartOIteration3BehaviourMode partOIteration3BehaviourMode, out bool legacy)
        {
            legacy = false;

            string path = PartOIteration3Paths.Path_Record_ForResults(path_TSD, partOIteration3BehaviourMode);

            if (string.IsNullOrWhiteSpace(path) || File.Exists(path))
            {
                return path;
            }

            //Not "legacy" in the sense below: this is the method's own record, only in the old place.
            string path_Beside = PartOIteration3Paths.Path_Record_ForResults_Legacy(path_TSD, partOIteration3BehaviourMode);

            if (!string.IsNullOrWhiteSpace(path_Beside) && File.Exists(path_Beside))
            {
                return path_Beside;
            }

            string path_Legacy = PartOIteration3Paths.Path_Record_ForResults(path_TSD);

            if (!string.IsNullOrWhiteSpace(path_Legacy) && File.Exists(path_Legacy))
            {
                PartOIteration3Record partOIteration3Record = Read(path_Legacy);

                if (partOIteration3Record is not null && partOIteration3Record.BehaviourMode == partOIteration3BehaviourMode)
                {
                    legacy = true;

                    return path_Legacy;
                }
            }

            return path;
        }

        /// <summary>
        /// What is recorded for one method against these results. Total: every failure is a status with a
        /// reason, never an exception, because this is read while the hub is being shown.
        /// </summary>
        public static PartOIteration3PairingStatus PartOIteration3PairingStatus(string path_TSD, PartOIteration3BehaviourMode partOIteration3BehaviourMode)
        {
            string path_Record = PartOIteration3RecordPath(path_TSD, partOIteration3BehaviourMode, out bool legacy);

            if (string.IsNullOrWhiteSpace(path_Record) || !File.Exists(path_Record))
            {
                return new PartOIteration3PairingStatus(partOIteration3BehaviourMode, path_Record, false, null, null);
            }

            PartOIteration3Record partOIteration3Record = Read(path_Record);

            if (partOIteration3Record is null)
            {
                return new PartOIteration3PairingStatus(partOIteration3BehaviourMode, path_Record, legacy, null, string.Format("The Iteration 3 record at '{0}' could not be read.", path_Record));
            }

            if (!UI.PartOIteration3Record.IsReadableSchema(partOIteration3Record.Schema))
            {
                return new PartOIteration3PairingStatus(
                    partOIteration3BehaviourMode,
                    path_Record,
                    legacy,
                    null,
                    string.Format(
                        "The Iteration 3 record at '{0}' states schema '{1}' and this build reads only '{2}' or '{3}', so it cannot be read as one.",
                        path_Record,
                        partOIteration3Record.Schema ?? "<none>",
                        UI.PartOIteration3Record.CurrentSchema,
                        UI.PartOIteration3Record.LegacySchema_V1));
            }

            //A method's own file that names another method is not this method's result, whatever it says.
            if (partOIteration3Record.BehaviourMode != partOIteration3BehaviourMode)
            {
                return new PartOIteration3PairingStatus(
                    partOIteration3BehaviourMode,
                    path_Record,
                    legacy,
                    null,
                    string.Format("The Iteration 3 record at '{0}' was written for another method ('{1}'), so it is not this method's result.", path_Record, partOIteration3Record.BehaviourMode));
            }

            return new PartOIteration3PairingStatus(partOIteration3BehaviourMode, path_Record, legacy, partOIteration3Record, null);
        }

        /// <summary>Every method's status against these results, in <see cref="PartOIteration3BehaviourModes"/> order.</summary>
        public static List<PartOIteration3PairingStatus> PartOIteration3PairingStatuses(string path_TSD)
        {
            List<PartOIteration3PairingStatus> result = [];

            foreach (PartOIteration3BehaviourMode partOIteration3BehaviourMode in PartOIteration3BehaviourModes)
            {
                result.Add(PartOIteration3PairingStatus(path_TSD, partOIteration3BehaviourMode));
            }

            return result;
        }
    }
}
