// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;

namespace SAM.Analytical.UI
{
    /// <summary>
    /// One room's running A/B totals while the comparison walks its two series.
    ///
    /// <para><b>Why the raw sums are kept and not only the finished statistics</b></para>
    /// <para>
    /// A pooled RMSE is not the mean of its rooms' RMSEs, and a pooled bias is not the mean of their
    /// biases unless every room has the same number of hours. Keeping the accumulators lets the pooled
    /// figure, every per-dwelling figure and every per-room figure all be produced from one pass over the
    /// series, and lets the series themselves be released immediately afterwards.
    /// </para>
    /// <para>
    /// <b>It never holds a series.</b> Six numbers per room, whatever the year length - which is what makes
    /// a five-thousand-room comparison a comparison rather than eighty million retained doubles.
    /// </para>
    /// </summary>
    internal class PartOIteration3Accumulator
    {
        internal PartOIteration3Accumulator(Guid guid_Space, string name_Space, Guid guid_Dwelling, string name_Dwelling)
        {
            Guid_Space = guid_Space;
            Name_Space = name_Space;
            Guid_Dwelling = guid_Dwelling;
            Name_Dwelling = name_Dwelling;
        }

        internal Guid Guid_Space { get; }

        internal string Name_Space { get; }

        internal Guid Guid_Dwelling { get; }

        internal string Name_Dwelling { get; }

        internal int Count { get; private set; }

        internal double Sum_A { get; private set; }

        internal double Sum_B { get; private set; }

        internal double Sum_Difference { get; private set; }

        internal double Sum_DifferenceSquared { get; private set; }

        internal double MaximumAbsoluteDifference { get; private set; } = double.NegativeInfinity;

        internal int Hour_MaximumAbsoluteDifference { get; private set; } = -1;

        /// <summary>Adds one hour. The caller has already established both values are finite.</summary>
        internal void Add(int hour, double value_A, double value_B)
        {
            double difference = value_B - value_A;
            double absolute = Math.Abs(difference);

            Count++;
            Sum_A += value_A;
            Sum_B += value_B;
            Sum_Difference += difference;
            Sum_DifferenceSquared += difference * difference;

            //Strictly greater: the FIRST hour at which the maximum occurs wins, so a tie resolves the same
            //way on every machine and an unchanged run exports byte-identically.
            if (absolute > MaximumAbsoluteDifference)
            {
                MaximumAbsoluteDifference = absolute;
                Hour_MaximumAbsoluteDifference = hour;
            }
        }

        /// <summary>This room's own finished diagnostics.</summary>
        internal PartOIteration3RoomComparison RoomComparison()
        {
            return Count == 0
                ? new PartOIteration3RoomComparison(Guid_Space, Name_Space, Guid_Dwelling, Name_Dwelling, 0, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, -1)
                : new PartOIteration3RoomComparison(
                    Guid_Space,
                    Name_Space,
                    Guid_Dwelling,
                    Name_Dwelling,
                    Count,
                    Sum_A / Count,
                    Sum_B / Count,
                    Sum_Difference / Count,
                    Math.Sqrt(Sum_DifferenceSquared / Count),
                    MaximumAbsoluteDifference,
                    Hour_MaximumAbsoluteDifference);
        }
    }
}
