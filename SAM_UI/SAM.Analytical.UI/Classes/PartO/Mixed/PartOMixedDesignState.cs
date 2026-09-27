// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;

namespace SAM.Analytical.UI
{
    /// <summary>
    /// What the mixed Part O design workflow keeps for a project beside the selected strategies: the project
    /// constraints, the latest screening evidence per strategy, and the latest final mixed-run result.
    ///
    /// <para><b>Why a sidecar beside the model, and not a parameter on it</b></para>
    /// <para>
    /// The <b>selected strategies</b> are SAM authority and live on the baseline itself
    /// (<c>AnalyticalModelParameter.PartODwellingStrategies</c>). Nothing else may: a materialisation record binds a
    /// result to <c>SimulationResultProvenance.Fingerprint(baseline)</c>, which digests every model parameter, so
    /// writing a result - or a pointer to one - onto the baseline after the run would move the very fingerprint the
    /// result is bound to and make it stale the moment it was saved. The record lives on the run's own model beside
    /// its results; this file is only how a reopened baseline finds it again. It is named from the model file
    /// (<c>&lt;model&gt;.partomixed.json</c>), so it needs no path stored anywhere.
    /// </para>
    ///
    /// <para><b>Never the source of a selected strategy</b></para>
    /// <para>
    /// A strategy is read from the model, never from here: the final run records the strategies it was built from
    /// only so its result can say what each dwelling ran as, and that snapshot is never written back as the
    /// selection. Everything in this file is re-validated against the model when it is read (see
    /// <see cref="PartOScreeningEvidence.IsCurrent"/> and <see cref="PartOMixedRunEvidence.IsCurrent"/>); a
    /// missing or unreadable file only means there is no saved evidence.
    /// </para>
    /// </summary>
    public class PartOMixedDesignState
    {
        public const string Schema_Current = "PartOMixedDesign:v1";

        public const string Suffix = ".partomixed.json";

        private readonly Dictionary<PartOScreeningStrategy, PartOScreeningEvidence> screening = [];

        public PartOMixedDesignConstraints Constraints { get; set; } = new();

        /// <summary>The screening strategies last chosen, so the next screening opens with them.</summary>
        public List<PartOScreeningStrategy> Strategies_Screening { get; } = [];

        public PartOScreeningMode ScreeningMode { get; set; } = PartOScreeningMode.Minimum;

        public PartOMixedRunEvidence FinalRun { get; set; }

        /// <summary>The latest evidence per strategy, in the least-intervention order.</summary>
        public List<PartOScreeningEvidence> Screening
        {
            get
            {
                List<PartOScreeningEvidence> result = [.. screening.Values];
                result.Sort((x, y) => x.Strategy.CompareTo(y.Strategy));
                return result;
            }
        }

        public PartOScreeningEvidence ScreeningEvidence(PartOScreeningStrategy partOScreeningStrategy)
        {
            return screening.TryGetValue(partOScreeningStrategy, out PartOScreeningEvidence partOScreeningEvidence) ? partOScreeningEvidence : null;
        }

        /// <summary>Replaces this strategy's evidence with a newer run's. A strategy has one current evidence, never two.</summary>
        public void SetScreeningEvidence(PartOScreeningEvidence partOScreeningEvidence)
        {
            if (partOScreeningEvidence is not null && partOScreeningEvidence.Strategy != PartOScreeningStrategy.Undefined)
            {
                screening[partOScreeningEvidence.Strategy] = partOScreeningEvidence;
            }
        }

        public bool RemoveScreeningEvidence(PartOScreeningStrategy partOScreeningStrategy)
        {
            return screening.Remove(partOScreeningStrategy);
        }

        /// <summary>This state's file for a model path, or null where the model has not been saved.</summary>
        public static string Path_State(string path_Model)
        {
            if (string.IsNullOrWhiteSpace(path_Model))
            {
                return null;
            }

            string directory = Path.GetDirectoryName(path_Model);
            string name = Path.GetFileNameWithoutExtension(path_Model);

            return string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(name) ? null : Path.Combine(directory, name + Suffix);
        }

        public JsonObject ToJsonObject()
        {
            JsonArray jsonArray_Screening = [];
            foreach (PartOScreeningEvidence partOScreeningEvidence in Screening)
            {
                jsonArray_Screening.Add(partOScreeningEvidence.ToJsonObject());
            }

            JsonArray jsonArray_Strategies = [];
            Strategies_Screening.ForEach(x => jsonArray_Strategies.Add(x.ToString()));

            return new JsonObject
            {
                ["Schema"] = Schema_Current,
                ["Constraints"] = Constraints?.ToJsonObject(),
                ["Strategies_Screening"] = jsonArray_Strategies,
                ["ScreeningMode"] = ScreeningMode.ToString(),
                ["Screening"] = jsonArray_Screening,
                ["FinalRun"] = FinalRun?.ToJsonObject(),
            };
        }

        public static PartOMixedDesignState Read(JsonObject jsonObject)
        {
            if (jsonObject is null || (string)jsonObject["Schema"] != Schema_Current)
            {
                return null;
            }

            PartOMixedDesignState result = new()
            {
                Constraints = PartOMixedDesignConstraints.Read(jsonObject["Constraints"] as JsonObject),
                FinalRun = PartOMixedRunEvidence.Read(jsonObject["FinalRun"] as JsonObject),
            };

            if (Enum.TryParse((string)jsonObject["ScreeningMode"], false, out PartOScreeningMode partOScreeningMode) && Enum.IsDefined(typeof(PartOScreeningMode), partOScreeningMode) && partOScreeningMode != PartOScreeningMode.Undefined)
            {
                result.ScreeningMode = partOScreeningMode;
            }

            if (jsonObject["Strategies_Screening"] is JsonArray jsonArray_Strategies)
            {
                foreach (JsonNode jsonNode in jsonArray_Strategies)
                {
                    if (Enum.TryParse((string)jsonNode, false, out PartOScreeningStrategy partOScreeningStrategy) && Enum.IsDefined(typeof(PartOScreeningStrategy), partOScreeningStrategy) && partOScreeningStrategy != PartOScreeningStrategy.Undefined && !result.Strategies_Screening.Contains(partOScreeningStrategy))
                    {
                        result.Strategies_Screening.Add(partOScreeningStrategy);
                    }
                }
            }

            if (jsonObject["Screening"] is JsonArray jsonArray_Screening)
            {
                foreach (JsonNode jsonNode in jsonArray_Screening)
                {
                    result.SetScreeningEvidence(PartOScreeningEvidence.Read(jsonNode as JsonObject));
                }
            }

            return result;
        }

        /// <summary>The state saved beside a model, or null where there is none or it is not one this build reads.</summary>
        public static PartOMixedDesignState Read(string path_State)
        {
            if (string.IsNullOrWhiteSpace(path_State) || !File.Exists(path_State))
            {
                return null;
            }

            try
            {
                return Read(JsonNode.Parse(File.ReadAllText(path_State)) as JsonObject);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Writes the state. A failure is returned as a note; it never fails the run that produced it.</summary>
        public bool Write(string path_State, out string note)
        {
            note = null;

            if (string.IsNullOrWhiteSpace(path_State))
            {
                note = "The model has not been saved yet, so the mixed-design screening and results are kept for this session only. Save the model to keep them.";
                return false;
            }

            try
            {
                File.WriteAllText(path_State, ToJsonObject().ToJsonString());
            }
            catch (Exception exception)
            {
                note = string.Format("The mixed-design state could not be written to '{0}' ({1}), so it is kept for this session only.", path_State, exception.Message);
                return false;
            }

            return true;
        }
    }
}
