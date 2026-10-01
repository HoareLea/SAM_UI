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
        /// The scoped check after "Set glazing": SAM's per-object model-check rules (<c>Create.Log(ApertureConstruction |
        /// Aperture | Panel, MaterialLibrary)</c>, the rules behind Edit > ModelCheck) over the applied aperture
        /// construction, the apertures it was assigned to and the panels carrying them - never the whole model. Records
        /// are distinct by text. Reads the model only.
        /// </summary>
        public static UValueCheckSummary GlazingCheckSummary(AnalyticalModel analyticalModel, SetGlazingResult result)
        {
            Log log = new Log();
            if (analyticalModel == null || result == null || !result.Succeeded)
            {
                return new UValueCheckSummary(log, 0, 0, 0, "Nothing was checked: the glazing change was not applied.");
            }

            MaterialLibrary materialLibrary = analyticalModel.MaterialLibrary ?? new MaterialLibrary("Default MaterialLibrary");
            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;

            ApertureConstruction apertureConstruction = adjacencyCluster?.GetApertureConstructions()?.Find(x => x != null && x.Guid == result.ApertureConstruction.Guid) ?? result.ApertureConstruction;

            // Both rule sets of the aperture construction, as Edit > ModelCheck runs them: its layers (a pane or a frame
            // missing) and its materials (a gas layer outside).
            List<LogRecord> logRecords = new List<LogRecord>();
            logRecords.AddRange(Analytical.Create.Log(apertureConstruction) ?? new Log());
            logRecords.AddRange(Analytical.Create.Log(apertureConstruction, materialLibrary) ?? new Log());

            int apertures = 0;
            foreach (Guid guid in result.ApertureGuids)
            {
                Aperture aperture = adjacencyCluster?.GetAperture(guid);
                if (aperture == null)
                {
                    continue;
                }

                apertures++;
                logRecords.AddRange(Analytical.Create.Log(aperture) ?? new Log());
            }

            foreach (Guid guid in result.PanelGuids)
            {
                Panel panel = adjacencyCluster?.GetObject<Panel>(guid);
                if (panel != null)
                {
                    logRecords.AddRange(Analytical.Create.Log(panel, materialLibrary) ?? new Log());
                }
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

            string subject = apertures == 0
                ? string.Format(CultureInfo.CurrentCulture, "{0} (assigned to no aperture)", apertureConstruction.Name)
                : string.Format(CultureInfo.CurrentCulture, "{0} and its {1} {2}", apertureConstruction.Name, apertures, apertures == 1 ? "aperture" : "apertures");

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
