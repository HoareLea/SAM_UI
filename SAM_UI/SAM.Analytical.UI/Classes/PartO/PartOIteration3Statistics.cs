// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;

namespace SAM.Analytical.UI
{
    /// <summary>
    /// Descriptive A/B resultant-temperature statistics over a set of rooms - the whole comparison, or one
    /// dwelling of it.
    ///
    /// <para><b>Pooled over hours, not averaged over rooms</b></para>
    /// <para>
    /// <see cref="MeanBias"/> and <see cref="RootMeanSquareError"/> are computed over every (room, hour)
    /// pair together. Averaging each room's own RMSE would be a different and wrong number: a room with a
    /// short series would carry the same weight as a room with a full year, and the pooled RMSE would no
    /// longer be the RMSE of anything. The per-room figures are reported separately, on
    /// <see cref="PartOIteration3RoomComparison"/>.
    /// </para>
    /// <para>
    /// <b>Accumulated in one pass.</b> The builder walks each (room, hour) once and keeps six running
    /// numbers, so the cost is linear in the compared values and nothing holds a second copy of a series.
    /// At five thousand rooms and a full year that distinction is the difference between a comparison and
    /// an out-of-memory failure.
    /// </para>
    /// </summary>
    public class PartOIteration3Statistics
    {
        public PartOIteration3Statistics(
            int count_Rooms,
            long count_Values,
            double mean_A,
            double mean_B,
            double meanBias,
            double rootMeanSquareError,
            double maximumAbsoluteDifference,
            Guid guid_Space_MaximumAbsoluteDifference,
            string name_Space_MaximumAbsoluteDifference,
            int hour_MaximumAbsoluteDifference)
        {
            Count_Rooms = count_Rooms;
            Count_Values = count_Values;
            Mean_A = mean_A;
            Mean_B = mean_B;
            MeanBias = meanBias;
            RootMeanSquareError = rootMeanSquareError;
            MaximumAbsoluteDifference = maximumAbsoluteDifference;
            Guid_Space_MaximumAbsoluteDifference = guid_Space_MaximumAbsoluteDifference;
            Name_Space_MaximumAbsoluteDifference = name_Space_MaximumAbsoluteDifference;
            Hour_MaximumAbsoluteDifference = hour_MaximumAbsoluteDifference;
        }

        /// <summary>How many rooms contributed.</summary>
        public int Count_Rooms { get; }

        /// <summary>How many (room, hour) values contributed.</summary>
        public long Count_Values { get; }

        /// <summary>Pooled mean of Reference A [C].</summary>
        public double Mean_A { get; }

        /// <summary>Pooled mean of Candidate B [C].</summary>
        public double Mean_B { get; }

        /// <summary>Pooled mean of (B - A) [K].</summary>
        public double MeanBias { get; }

        /// <summary>Pooled root mean square of (B - A) [K].</summary>
        public double RootMeanSquareError { get; }

        /// <summary>The largest absolute (B - A) anywhere in the set [K].</summary>
        public double MaximumAbsoluteDifference { get; }

        /// <summary>The room it occurred in.</summary>
        public Guid Guid_Space_MaximumAbsoluteDifference { get; }

        /// <summary>Display only.</summary>
        public string Name_Space_MaximumAbsoluteDifference { get; }

        /// <summary>The first hour it occurred at, 0-based. First, so a rerun exports identically.</summary>
        public int Hour_MaximumAbsoluteDifference { get; }

        /// <summary>
        /// The pooled statistics of a set of room comparisons, recomputed from their own accumulators.
        /// <para>
        /// The room comparisons carry means and RMSEs, which cannot simply be averaged - so this takes the
        /// raw accumulators alongside them. It is how <c>PartOIteration3Comparison</c> produces both the
        /// pooled figure and one per dwelling from a single walk of the series.
        /// </para>
        /// </summary>
        internal static PartOIteration3Statistics Create(IEnumerable<PartOIteration3Accumulator> partOIteration3Accumulators)
        {
            int count_Rooms = 0;
            long count_Values = 0;
            double sum_A = 0;
            double sum_B = 0;
            double sum_Difference = 0;
            double sum_DifferenceSquared = 0;

            double maximumAbsoluteDifference = double.NegativeInfinity;
            Guid guid_Space = Guid.Empty;
            string name_Space = null;
            int hour = -1;

            foreach (PartOIteration3Accumulator partOIteration3Accumulator in partOIteration3Accumulators ?? [])
            {
                if (partOIteration3Accumulator is null || partOIteration3Accumulator.Count == 0)
                {
                    continue;
                }

                count_Rooms++;
                count_Values += partOIteration3Accumulator.Count;
                sum_A += partOIteration3Accumulator.Sum_A;
                sum_B += partOIteration3Accumulator.Sum_B;
                sum_Difference += partOIteration3Accumulator.Sum_Difference;
                sum_DifferenceSquared += partOIteration3Accumulator.Sum_DifferenceSquared;

                //Strictly greater, so the FIRST room in the walk's own order wins a tie - and the walk is
                //ordered by guid, so the answer does not depend on dictionary order.
                if (partOIteration3Accumulator.MaximumAbsoluteDifference > maximumAbsoluteDifference)
                {
                    maximumAbsoluteDifference = partOIteration3Accumulator.MaximumAbsoluteDifference;
                    guid_Space = partOIteration3Accumulator.Guid_Space;
                    name_Space = partOIteration3Accumulator.Name_Space;
                    hour = partOIteration3Accumulator.Hour_MaximumAbsoluteDifference;
                }
            }

            if (count_Values == 0)
            {
                return new PartOIteration3Statistics(0, 0, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, Guid.Empty, null, -1);
            }

            return new PartOIteration3Statistics(
                count_Rooms,
                count_Values,
                sum_A / count_Values,
                sum_B / count_Values,
                sum_Difference / count_Values,
                Math.Sqrt(sum_DifferenceSquared / count_Values),
                maximumAbsoluteDifference,
                guid_Space,
                name_Space,
                hour);
        }

        public override string ToString()
        {
            return string.Format(
                "{0} room(s), {1} value(s): bias={2:0.###} K, RMSE={3:0.###} K, max|B-A|={4:0.###} K in '{5}' at hour {6}",
                Count_Rooms,
                Count_Values,
                MeanBias,
                RootMeanSquareError,
                MaximumAbsoluteDifference,
                Name_Space_MaximumAbsoluteDifference ?? "-",
                Hour_MaximumAbsoluteDifference);
        }
    }
}
