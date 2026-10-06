// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Query
    {
        /// <summary>
        /// The scoped check after "Set U-value": SAM's per-object model-check rules (<c>Create.Log(Construction |
        /// Panel, MaterialLibrary)</c>, the rules behind Edit > ModelCheck) over the changed construction and the
        /// panels the change assigned it to - never the whole model. Records are distinct by text (a construction
        /// rule repeats on every panel using it). Reads the model only.
        /// </summary>
        public static UValueCheckSummary UValueCheckSummary(AnalyticalModel analyticalModel, SetUValueResult result)
        {
            Log log = new Log();
            if (analyticalModel == null || result == null || !result.Succeeded)
            {
                return new UValueCheckSummary(log, 0, 0, 0, "Nothing was checked: the U-value change was not applied.");
            }

            MaterialLibrary materialLibrary = analyticalModel.MaterialLibrary ?? new MaterialLibrary("Default MaterialLibrary");
            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;

            Construction construction = adjacencyCluster?.GetConstructions()?.Find(x => x != null && x.Guid == result.Construction.Guid) ?? result.Construction;

            List<LogRecord> logRecords = new List<LogRecord>();
            logRecords.AddRange(Analytical.Create.Log(construction, materialLibrary) ?? new Log());

            int panels = 0;
            foreach (Guid guid in result.PanelGuids)
            {
                Panel panel = adjacencyCluster?.GetObject<Panel>(guid);
                if (panel == null)
                {
                    continue;
                }

                panels++;
                logRecords.AddRange(Analytical.Create.Log(panel, materialLibrary) ?? new Log());
            }

            HashSet<string> texts = new HashSet<string>();
            foreach (LogRecord logRecord in logRecords)
            {
                if (logRecord != null && texts.Add(logRecord.LogRecordType + "|" + logRecord.Text))
                {
                    log.Add(logRecord);
                }
            }

            List<LogRecord> records = log.ToList();
            int errors = records.Count(x => x.LogRecordType == LogRecordType.Error);
            int warnings = records.Count(x => x.LogRecordType == LogRecordType.Warning);
            int messages = records.Count - errors - warnings;

            string subject = panels == 0
                ? string.Format(CultureInfo.CurrentCulture, "{0} (assigned to no panel)", construction.Name)
                : string.Format(CultureInfo.CurrentCulture, "{0} and its {1} {2}", construction.Name, panels, panels == 1 ? "panel" : "panels");

            string text;
            if (errors == 0 && warnings == 0)
            {
                text = string.Format(CultureInfo.CurrentCulture, "No errors or warnings for {0}.", subject);
            }
            else
            {
                List<string> parts = new List<string>();
                if (errors > 0)
                {
                    parts.Add(string.Format(CultureInfo.CurrentCulture, "{0} {1}", errors, errors == 1 ? "error" : "errors"));
                }

                if (warnings > 0)
                {
                    parts.Add(string.Format(CultureInfo.CurrentCulture, "{0} {1}", warnings, warnings == 1 ? "warning" : "warnings"));
                }

                text = string.Format(CultureInfo.CurrentCulture, "{0} for {1}.", string.Join(" and ", parts), subject);
            }

            return new UValueCheckSummary(log, errors, warnings, messages, text);
        }
    }
}
