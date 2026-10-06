// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;

namespace SAM.Analytical.UI
{
    /// <summary>
    /// One room's descriptive A/B resultant-temperature diagnostics, keyed by the <b>design</b> space.
    ///
    /// <para><b>Descriptive, and deliberately not a verdict</b></para>
    /// <para>
    /// Nothing here states whether the two routes agree. SAM #111 is explicit that bit-identical
    /// temperatures are not required merely because the design airflows are identical, so this orchestration
    /// does not invent a parity threshold anybody would then read as a pass mark. It reports how far apart
    /// the two series are and where; an engineer decides what that means.
    /// </para>
    /// <para>
    /// <b>Compliance is not here at all.</b> The TM59 verdicts are carried from each existing assessment
    /// verbatim - see <see cref="PartOIteration3CriterionComparison"/> - and are never re-derived from these
    /// numbers.
    /// </para>
    /// </summary>
    public class PartOIteration3RoomComparison
    {
        public PartOIteration3RoomComparison(
            Guid guid_Space,
            string name_Space,
            Guid guid_Dwelling,
            string name_Dwelling,
            int count,
            double mean_A,
            double mean_B,
            double meanBias,
            double rootMeanSquareError,
            double maximumAbsoluteDifference,
            int hour_MaximumAbsoluteDifference)
        {
            Guid_Space = guid_Space;
            Name_Space = name_Space;
            Guid_Dwelling = guid_Dwelling;
            Name_Dwelling = name_Dwelling;
            Count = count;
            Mean_A = mean_A;
            Mean_B = mean_B;
            MeanBias = meanBias;
            RootMeanSquareError = rootMeanSquareError;
            MaximumAbsoluteDifference = maximumAbsoluteDifference;
            Hour_MaximumAbsoluteDifference = hour_MaximumAbsoluteDifference;
        }

        /// <summary>The design space. The only key anything joins on.</summary>
        public Guid Guid_Space { get; }

        /// <summary>Display only. Three flats can hold three rooms with this name.</summary>
        public string Name_Space { get; }

        /// <summary>The dwelling zone this room belongs to, for grouping. <see cref="Guid.Empty"/> where none.</summary>
        public Guid Guid_Dwelling { get; }

        /// <summary>Display only.</summary>
        public string Name_Dwelling { get; }

        /// <summary>How many hours were compared. Both series carry exactly this many.</summary>
        public int Count { get; }

        /// <summary>Mean resultant temperature of Reference A over the compared hours [C].</summary>
        public double Mean_A { get; }

        /// <summary>Mean resultant temperature of Candidate B over the compared hours [C].</summary>
        public double Mean_B { get; }

        /// <summary>Mean of (B - A). Signed: a positive bias means the Systems route runs warmer [K].</summary>
        public double MeanBias { get; }

        /// <summary>Root mean square of (B - A) [K].</summary>
        public double RootMeanSquareError { get; }

        /// <summary>The largest absolute (B - A) over the compared hours [K].</summary>
        public double MaximumAbsoluteDifference { get; }

        /// <summary>
        /// The FIRST hour at which <see cref="MaximumAbsoluteDifference"/> occurs, 0-based.
        /// <para>
        /// First rather than last, deliberately: a tie is resolved the same way on every machine and every
        /// rerun, so an exported comparison of an unchanged run is byte-identical.
        /// </para>
        /// </summary>
        public int Hour_MaximumAbsoluteDifference { get; }

        public override string ToString()
        {
            return string.Format(
                "{0}: n={1}, A={2:0.###}, B={3:0.###}, bias={4:0.###}, RMSE={5:0.###}, max|B-A|={6:0.###} at hour {7}",
                Name_Space ?? Guid_Space.ToString(),
                Count,
                Mean_A,
                Mean_B,
                MeanBias,
                RootMeanSquareError,
                MaximumAbsoluteDifference,
                Hour_MaximumAbsoluteDifference);
        }
    }
}
