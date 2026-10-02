// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.IO;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        /// <summary>
        /// Saves the "Set U-value" report next to the model (<see cref="Query.Path_UValueChangeReport"/>). Best effort,
        /// like <c>SavePartOTM59Report</c>: the change is already applied, so a missing path or a write failure is
        /// handed back as <paramref name="refusal"/> rather than thrown. Never writes to the model. An existing file of
        /// the same name is not overwritten: " (2)", " (3)"... is appended.
        /// </summary>
        public static bool SaveUValueChangeReport(string path_Model, DateTime appliedAt, string text, out string path_Report, out string refusal)
        {
            return SaveChangeReport(Query.Path_UValueChangeReport(path_Model, appliedAt), text, out path_Report, out refusal);
        }

        /// <summary>
        /// Saves the "Set glazing" report next to the model (<see cref="Query.Path_GlazingChangeReport"/>), with the
        /// same rules as <see cref="SaveUValueChangeReport"/>: best effort, never overwrites, never writes to the model.
        /// </summary>
        public static bool SaveGlazingChangeReport(string path_Model, DateTime appliedAt, string text, out string path_Report, out string refusal)
        {
            return SaveChangeReport(Query.Path_GlazingChangeReport(path_Model, appliedAt), text, out path_Report, out refusal);
        }

        /// <summary>
        /// Saves the report of an existing construction assigned in the Thermal Performance panel next to the model
        /// (<see cref="Query.Path_ConstructionChangeReport"/>), with the same rules as <see cref="SaveUValueChangeReport"/>: best effort,
        /// never overwrites, never writes to the model.
        /// </summary>
        public static bool SaveConstructionChangeReport(string path_Model, DateTime appliedAt, string text, out string path_Report, out string refusal)
        {
            return SaveChangeReport(Query.Path_ConstructionChangeReport(path_Model, appliedAt), text, out path_Report, out refusal);
        }

        private static bool SaveChangeReport(string path_Planned, string text, out string path_Report, out string refusal)
        {
            refusal = null;
            path_Report = path_Planned;
            if (path_Report == null)
            {
                refusal = "The model has not been saved, so the report was not written to a file; use Copy All.";
                return false;
            }

            try
            {
                string directory = Path.GetDirectoryName(path_Report);
                if (!Directory.Exists(directory))
                {
                    refusal = string.Format("The model's folder '{0}' does not exist, so the report was not written; use Copy All.", directory);
                    return false;
                }

                string path = path_Report;
                for (int index = 2; File.Exists(path); index++)
                {
                    path = Path.Combine(directory, string.Format("{0} ({1}){2}", Path.GetFileNameWithoutExtension(path_Report), index, Path.GetExtension(path_Report)));
                }

                path_Report = path;
                File.WriteAllText(path_Report, text ?? string.Empty);
                return true;
            }
            catch (Exception exception)
            {
                refusal = string.Format("The report could not be written to '{0}': {1}", path_Report, exception.Message);
                return false;
            }
        }
    }
}
