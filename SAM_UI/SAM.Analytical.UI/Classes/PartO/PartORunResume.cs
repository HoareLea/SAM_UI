// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;

namespace SAM.Analytical.UI
{
    /// <summary>
    /// What a completed full-year Part O run needs, beyond its run model and results, for Iteration 3 to be
    /// started from it in a LATER session without re-running Prepare &amp; Run: how it was prepared (iteration,
    /// dwelling zones, the ventilation systems the preparation built), the exact TAS case it ran as, and the
    /// prepared model Candidate B's thermal source is built from.
    /// <para>
    /// <b>Bound to its own results.</b> It records the length and write time of the TSD it belongs to and the
    /// fingerprint of the prepared model saved beside it; a resume that finds either changed is refused, so a
    /// sidecar can never lend its preparation to different results. Written beside the run model as
    /// <c>&lt;project&gt;.partorun.json</c>, with the prepared model as <c>&lt;project&gt;.prepared.sam</c>.
    /// </para>
    /// </summary>
    public class PartORunResume
    {
        /// <summary>v2 adds <see cref="VentilationUnitCatalogueOffered"/>.</summary>
        public const string Schema_Current = "PartORunResume:v2";

        /// <summary>Still read: everything v2 holds except whether a catalogue was offered, which it reads as unknown.</summary>
        public const string Schema_V1 = "PartORunResume:v1";

        public const string Suffix_Resume = ".partorun.json";

        public const string Suffix_PreparedModel = ".prepared.sam";

        public string Schema { get; set; } = Schema_Current;

        public PartOIteration PartOIteration { get; set; }

        /// <summary>
        /// Whether the preparation was offered a product catalogue - what tells Iteration 2 from Iteration 1a
        /// when the run is named. Null where the sidecar predates it (v1).
        /// </summary>
        public bool? VentilationUnitCatalogueOffered { get; set; }

        public List<Guid> Guids_Zone { get; set; } = [];

        public List<Guid> Guids_VentilationSystem { get; set; } = [];

        public string SolarCalculationMethod { get; set; }

        public int SimulateFrom { get; set; }

        public int SimulateTo { get; set; }

        public bool UnmetHours { get; set; }

        public bool Sizing { get; set; }

        public bool UseWidths { get; set; }

        public bool UpdateConstructionLayersByPanelType { get; set; }

        public long Length_TSD { get; set; }

        public long Timestamp_TSD { get; set; }

        public string Fingerprint_PreparedModel { get; set; }

        /// <summary>The sidecar's path for a run's results file.</summary>
        public static string Path_Resume(string path_TSD)
        {
            return Path_Beside(path_TSD, Suffix_Resume);
        }

        /// <summary>The prepared model's path for a run's results file.</summary>
        public static string Path_PreparedModel(string path_TSD)
        {
            return Path_Beside(path_TSD, Suffix_PreparedModel);
        }

        private static string Path_Beside(string path_TSD, string suffix)
        {
            if (string.IsNullOrWhiteSpace(path_TSD))
            {
                return null;
            }

            string directory = Path.GetDirectoryName(path_TSD);
            string name = Path.GetFileNameWithoutExtension(path_TSD);

            return string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(name) ? null : Path.Combine(directory, name + suffix);
        }

        public JsonObject ToJsonObject()
        {
            JsonArray zones = [];
            Guids_Zone.ForEach(x => zones.Add(x.ToString()));

            JsonArray systems = [];
            Guids_VentilationSystem.ForEach(x => systems.Add(x.ToString()));

            return new JsonObject
            {
                ["Schema"] = Schema,
                ["PartOIteration"] = PartOIteration.ToString(),
                ["VentilationUnitCatalogueOffered"] = VentilationUnitCatalogueOffered,
                ["Guids_Zone"] = zones,
                ["Guids_VentilationSystem"] = systems,
                ["SolarCalculationMethod"] = SolarCalculationMethod,
                ["SimulateFrom"] = SimulateFrom,
                ["SimulateTo"] = SimulateTo,
                ["UnmetHours"] = UnmetHours,
                ["Sizing"] = Sizing,
                ["UseWidths"] = UseWidths,
                ["UpdateConstructionLayersByPanelType"] = UpdateConstructionLayersByPanelType,
                ["Length_TSD"] = Length_TSD,
                ["Timestamp_TSD"] = Timestamp_TSD,
                ["Fingerprint_PreparedModel"] = Fingerprint_PreparedModel,
            };
        }

        /// <summary>The sidecar read from a file, or null where there is none or it is not one this build reads.</summary>
        public static PartORunResume Read(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return null;
            }

            try
            {
                if (JsonNode.Parse(File.ReadAllText(path)) is not JsonObject jsonObject)
                {
                    return null;
                }

                string schema = (string)jsonObject["Schema"];
                if (schema != Schema_Current && schema != Schema_V1)
                {
                    return null;
                }

                PartORunResume result = new()
                {
                    Schema = (string)jsonObject["Schema"],
                    PartOIteration = Enum.TryParse((string)jsonObject["PartOIteration"], out PartOIteration partOIteration) ? partOIteration : default,
                    VentilationUnitCatalogueOffered = schema == Schema_V1 ? null : (bool?)jsonObject["VentilationUnitCatalogueOffered"],
                    SolarCalculationMethod = (string)jsonObject["SolarCalculationMethod"],
                    SimulateFrom = (int)jsonObject["SimulateFrom"],
                    SimulateTo = (int)jsonObject["SimulateTo"],
                    UnmetHours = (bool)jsonObject["UnmetHours"],
                    Sizing = (bool)jsonObject["Sizing"],
                    UseWidths = (bool)jsonObject["UseWidths"],
                    UpdateConstructionLayersByPanelType = (bool)jsonObject["UpdateConstructionLayersByPanelType"],
                    Length_TSD = (long)jsonObject["Length_TSD"],
                    Timestamp_TSD = (long)jsonObject["Timestamp_TSD"],
                    Fingerprint_PreparedModel = (string)jsonObject["Fingerprint_PreparedModel"],
                };

                foreach (JsonNode jsonNode in jsonObject["Guids_Zone"] as JsonArray ?? [])
                {
                    result.Guids_Zone.Add(Guid.Parse((string)jsonNode));
                }

                foreach (JsonNode jsonNode in jsonObject["Guids_VentilationSystem"] as JsonArray ?? [])
                {
                    result.Guids_VentilationSystem.Add(Guid.Parse((string)jsonNode));
                }

                return result;
            }
            catch
            {
                return null;
            }
        }
    }
}
