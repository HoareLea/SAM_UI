// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.UI;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// The structured sibling of <see cref="PartOIteration3ReportText"/>: the same review, as JSON, for a
    /// reader that is a script rather than a person.
    ///
    /// <para><b>The record, plus what the window adds to it</b></para>
    /// <para>
    /// The pairing record already serialises its own identity, scope, bindings, files and stages, and it
    /// is embedded here <b>verbatim</b> rather than re-spelled - one authority, and a reader that already
    /// parses records needs no second parser. What this adds is the part of the window the record
    /// deliberately does not hold: the rebuilt A/B statistics and the per-room, per-criterion rows.
    /// </para>
    ///
    /// <para><b>It stores no engineering the review did not produce</b></para>
    /// <para>
    /// Every value is copied from the comparison the review computed. The record still holds no thermal
    /// statistic, exactly as before: a stored statistic inside the <i>record</i> would be a second answer
    /// a later review could disagree with. A statistic inside a <i>report</i> is different - the report is
    /// the answer, dated and named, not an input to the next one.
    /// </para>
    /// </summary>
    public static class PartOIteration3ReportJson
    {
        /// <summary>The report as a JSON object, or null where there is no result to describe.</summary>
        public static JsonObject ToJsonObject(PartOIteration3Result partOIteration3Result, IEnumerable<PartOIteration3Row> rows)
        {
            if (partOIteration3Result is null)
            {
                return null;
            }

            JsonObject result = new()
            {
                { "Schema", PartOIteration3ReportText.CurrentSchema },
                { "Outcome", PartOIteration3ReportText.Outcome(partOIteration3Result) },
                { "Summary", PartOIteration3ReportText.Summary(partOIteration3Result) },
                { "IsComplete", partOIteration3Result.IsComplete },
                { "IsRestored", partOIteration3Result.IsRestored },
                { "Path_Record", partOIteration3Result.Path_Record },
                { "Path_TM59Report_ReferenceA", partOIteration3Result.Path_TM59Report_ReferenceA },
                { "Path_TM59Report_CandidateB", partOIteration3Result.Path_TM59Report_CandidateB },
                { "Notes", Array(partOIteration3Result.Notes) },
            };

            if (!partOIteration3Result.IsComplete)
            {
                result.Add("Refusal", PartOIteration3ReportText.Refusal(partOIteration3Result));
                result.Add("Reasons", Array(partOIteration3Result.Reasons));
            }

            //The record IS the pairing identity. Embedded whole rather than summarised.
            result.Add("Record", partOIteration3Result.Record?.ToJsonObject());

            PartOIteration3Comparison partOIteration3Comparison = partOIteration3Result.Comparison;

            if (partOIteration3Comparison is null)
            {
                //Explicitly null rather than absent: "this pairing produced no comparison" is a statement,
                //and a reader must not have to guess whether the key was forgotten.
                result.Add("Comparison", null);

                return result;
            }

            JsonArray jsonArray_Dwellings = [];
            foreach (PartOIteration3DwellingStatistics partOIteration3DwellingStatistics in partOIteration3Comparison.Dwellings)
            {
                jsonArray_Dwellings.Add(new JsonObject
                {
                    { "Guid_Dwelling", partOIteration3DwellingStatistics.Guid_Dwelling.ToString() },
                    { "Name_Dwelling", partOIteration3DwellingStatistics.Name_Dwelling },
                    { "Statistics", ToJsonObject(partOIteration3DwellingStatistics.Statistics) },
                });
            }

            JsonArray jsonArray_Rooms = [];
            foreach (PartOIteration3RoomComparison partOIteration3RoomComparison in partOIteration3Comparison.Rooms)
            {
                jsonArray_Rooms.Add(new JsonObject
                {
                    { "Guid_Space", partOIteration3RoomComparison.Guid_Space.ToString() },
                    { "Name_Space", partOIteration3RoomComparison.Name_Space },
                    { "Guid_Dwelling", partOIteration3RoomComparison.Guid_Dwelling.ToString() },
                    { "Name_Dwelling", partOIteration3RoomComparison.Name_Dwelling },
                    { "Count", partOIteration3RoomComparison.Count },
                    { "Mean_A", partOIteration3RoomComparison.Mean_A },
                    { "Mean_B", partOIteration3RoomComparison.Mean_B },
                    { "MeanBias", partOIteration3RoomComparison.MeanBias },
                    { "RootMeanSquareError", partOIteration3RoomComparison.RootMeanSquareError },
                    { "MaximumAbsoluteDifference", partOIteration3RoomComparison.MaximumAbsoluteDifference },
                    { "Hour_MaximumAbsoluteDifference", partOIteration3RoomComparison.Hour_MaximumAbsoluteDifference },
                });
            }

            JsonArray jsonArray_Rows = [];
            foreach (PartOIteration3Row partOIteration3Row in rows ?? [])
            {
                jsonArray_Rows.Add(new JsonObject
                {
                    { "Guid_Space", partOIteration3Row.Guid_Space.ToString() },
                    { "Dwelling", partOIteration3Row.Dwelling },
                    { "Space", partOIteration3Row.Space },
                    { "Criterion", partOIteration3Row.Criterion },
                    { "Mechanical", partOIteration3Row.Mechanical },
                    { "Actual_A", partOIteration3Row.Actual_A },
                    { "Limit_A", partOIteration3Row.Limit_A },
                    { "Status_A", partOIteration3Row.Status_A },
                    { "Actual_B", partOIteration3Row.Actual_B },
                    { "Limit_B", partOIteration3Row.Limit_B },
                    { "Status_B", partOIteration3Row.Status_B },
                    { "Delta_Actual", partOIteration3Row.Delta_Actual },
                    { "Changed", partOIteration3Row.Changed },
                });
            }

            result.Add("Comparison", new JsonObject
            {
                { "Statistics", ToJsonObject(partOIteration3Comparison.Statistics) },
                { "Count_Changed", partOIteration3Comparison.Count_Changed },
                { "Dwellings", jsonArray_Dwellings },
                { "Rooms", jsonArray_Rooms },
                { "Criteria", jsonArray_Rows },
            });

            return result;
        }

        /// <summary>The report as indented JSON text, or null where there is no result to describe.</summary>
        public static string Text(PartOIteration3Result partOIteration3Result, IEnumerable<PartOIteration3Row> rows)
        {
            JsonObject jsonObject = ToJsonObject(partOIteration3Result, rows);

            return jsonObject?.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        }

        private static JsonObject ToJsonObject(PartOIteration3Statistics partOIteration3Statistics)
        {
            if (partOIteration3Statistics is null)
            {
                return null;
            }

            return new JsonObject
            {
                { "Count_Rooms", partOIteration3Statistics.Count_Rooms },
                { "Count_Values", partOIteration3Statistics.Count_Values },
                { "Mean_A", partOIteration3Statistics.Mean_A },
                { "Mean_B", partOIteration3Statistics.Mean_B },
                { "MeanBias", partOIteration3Statistics.MeanBias },
                { "RootMeanSquareError", partOIteration3Statistics.RootMeanSquareError },
                { "MaximumAbsoluteDifference", partOIteration3Statistics.MaximumAbsoluteDifference },
                { "Guid_Space_MaximumAbsoluteDifference", partOIteration3Statistics.Guid_Space_MaximumAbsoluteDifference.ToString() },
                { "Name_Space_MaximumAbsoluteDifference", partOIteration3Statistics.Name_Space_MaximumAbsoluteDifference },
                { "Hour_MaximumAbsoluteDifference", partOIteration3Statistics.Hour_MaximumAbsoluteDifference },
            };
        }

        private static JsonArray Array(IEnumerable<string> texts)
        {
            JsonArray result = [];

            foreach (string text in texts ?? [])
            {
                result.Add(JsonValue.Create(text));
            }

            return result;
        }
    }
}
