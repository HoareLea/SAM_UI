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
        /// construction, the apertures it was assigned to and the panels carrying them (including the host-panel rules: the
        /// aperture construction's Default Panel Type and panel group against the panel) - never the whole model. Records
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
                if (panel == null)
                {
                    continue;
                }

                logRecords.AddRange(Analytical.Create.Log(panel, materialLibrary) ?? new Log());

                // The host-panel rules of Edit > ModelCheck: the apertures' Default Panel Type against this panel
                // (Create.Log(Panel)), and the panel group of the panel against the group of the assigned aperture
                // construction (a rule SAM only runs in the whole-model Log(AdjacencyCluster) loop, so it is applied here
                // to the changed apertures with the same wording).
                logRecords.AddRange(Analytical.Create.Log(panel) ?? new Log());
                logRecords.AddRange(PanelGroupRecords(panel, result.ApertureGuids));
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

        /// <summary>
        /// SAM's whole-model rule "PanelType of {panel} does not match with assigned {aperture construction}", restricted to
        /// the changed apertures of one panel (same wording and severity as <c>Create.Log(AdjacencyCluster)</c>).
        /// </summary>
        private static IEnumerable<LogRecord> PanelGroupRecords(Panel panel, IEnumerable<Guid> apertureGuids)
        {
            List<LogRecord> result = new List<LogRecord>();

            PanelGroup panelGroup_Panel = panel.PanelType.PanelGroup();
            if (panelGroup_Panel == PanelGroup.Undefined || panel.Apertures == null)
            {
                return result;
            }

            HashSet<Guid> guids = new HashSet<Guid>(apertureGuids ?? Enumerable.Empty<Guid>());
            foreach (Aperture aperture in panel.Apertures)
            {
                ApertureConstruction apertureConstruction = aperture?.ApertureConstruction;
                if (apertureConstruction == null || !guids.Contains(aperture.Guid))
                {
                    continue;
                }

                PanelGroup panelGroup_ApertureConstruction = apertureConstruction.PanelType().PanelGroup();
                if (panelGroup_ApertureConstruction == PanelGroup.Undefined || panelGroup_ApertureConstruction == panelGroup_Panel)
                {
                    continue;
                }

                string apertureName = string.IsNullOrEmpty(aperture.Name) ? "???" : aperture.Name;
                string apertureConstructionName = string.IsNullOrEmpty(apertureConstruction.Name) ? "???" : apertureConstruction.Name;

                result.Add(new LogRecord("PanelType of {0} Panel (Guid: {1}) does not match with assigned {2} ApertureConstruction (Guid: {3}) for {4} Aperture (Guid: {5}).", LogRecordType.Warning,
                    panel.Name, panel.Guid, apertureConstructionName, apertureConstruction.Guid, apertureName, aperture.Guid));
            }

            return result;
        }
    }
}
