// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.UI;
using System;
using System.Collections.Generic;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// <b>The A/B statistics, against hand-calculated answers.</b>
    /// <para>
    /// Every figure here is worked out on paper in the test, not reproduced from the implementation.
    /// A statistic that is only ever compared with itself is not tested, and a pooled RMSE in particular
    /// is easy to get subtly wrong - averaging the rooms' own RMSEs gives a different number that looks
    /// entirely plausible.
    /// </para>
    /// <para>
    /// The other half is what the comparison <b>refuses</b>: a short series, a missing one and a
    /// non-finite value each produce a refusal rather than a statistic computed over whatever survived.
    /// </para>
    /// </summary>
    public class PartOIteration3ComparisonTests
    {
        private static readonly Guid guid_Dwelling_1 = new("11111111-1111-1111-1111-111111111111");

        private static readonly Guid guid_Dwelling_2 = new("22222222-2222-2222-2222-222222222222");

        private static readonly Guid guid_Room_1 = new("aaaaaaaa-0000-0000-0000-000000000001");

        private static readonly Guid guid_Room_2 = new("bbbbbbbb-0000-0000-0000-000000000002");

        private static List<PartOIteration3Room> Rooms()
        {
            return
            [
                new PartOIteration3Room(guid_Room_1, "Bedroom 2", guid_Dwelling_1, "Flat 1"),
                new PartOIteration3Room(guid_Room_2, "Bedroom 2", guid_Dwelling_2, "Flat 2"),
            ];
        }

        /// <summary>A minimal completed comparison, for tests elsewhere that only need one to exist.</summary>
        internal static PartOIteration3Comparison Comparison()
        {
            Dictionary<Guid, double[]> series_A = new() { { guid_Room_1, [20, 21] } };
            Dictionary<Guid, double[]> series_B = new() { { guid_Room_1, [21, 22] } };

            return PartOIteration3Comparison.Create(
                [new PartOIteration3Room(guid_Room_1, "Bedroom 2", guid_Dwelling_1, "Flat 1")],
                series_A,
                series_B,
                [],
                out List<string> _);
        }

        /// <summary>
        /// One room, four hours, differences +1, -3, +2, +2.
        /// <para>
        /// mean A = (20+22+24+26)/4 = 23; mean B = (21+19+26+28)/4 = 23.5; bias = 0.5;
        /// RMSE = sqrt((1 + 9 + 4 + 4)/4) = sqrt(4.5); max |d| = 3 at hour 1.
        /// </para>
        /// </summary>
        [Fact]
        public void One_room_produces_the_hand_calculated_bias_rmse_maximum_and_argmax()
        {
            Dictionary<Guid, double[]> series_A = new() { { guid_Room_1, [20, 22, 24, 26] } };
            Dictionary<Guid, double[]> series_B = new() { { guid_Room_1, [21, 19, 26, 28] } };

            PartOIteration3Comparison partOIteration3Comparison = PartOIteration3Comparison.Create(
                [new PartOIteration3Room(guid_Room_1, "Bedroom 2", guid_Dwelling_1, "Flat 1")],
                series_A,
                series_B,
                [],
                out List<string> refusals);

            Assert.Empty(refusals);
            Assert.NotNull(partOIteration3Comparison);

            PartOIteration3RoomComparison partOIteration3RoomComparison = Assert.Single(partOIteration3Comparison.Rooms);

            Assert.Equal(4, partOIteration3RoomComparison.Count);
            Assert.Equal(23.0, partOIteration3RoomComparison.Mean_A, 12);
            Assert.Equal(23.5, partOIteration3RoomComparison.Mean_B, 12);
            Assert.Equal(0.5, partOIteration3RoomComparison.MeanBias, 12);
            Assert.Equal(Math.Sqrt(4.5), partOIteration3RoomComparison.RootMeanSquareError, 12);
            Assert.Equal(3.0, partOIteration3RoomComparison.MaximumAbsoluteDifference, 12);
            Assert.Equal(1, partOIteration3RoomComparison.Hour_MaximumAbsoluteDifference);
        }

        /// <summary>
        /// Two rooms, two hours each. Room 1 differences +1, +1; room 2 differences -2, +4.
        /// <para>
        /// Pooled over four values: bias = (1 + 1 - 2 + 4)/4 = 1;
        /// RMSE = sqrt((1 + 1 + 4 + 16)/4) = sqrt(5.5); max = 4, in room 2 at hour 1.
        /// </para>
        /// <para>
        /// <b>And the pooled RMSE is NOT the mean of the rooms' RMSEs</b>, which would be
        /// (sqrt(1) + sqrt(10))/2 = 2.081..., a quite different and quite plausible number. Asserted
        /// explicitly, because that is the mistake this pooling exists to avoid.
        /// </para>
        /// </summary>
        [Fact]
        public void Pooled_statistics_are_over_every_value_and_not_the_mean_of_the_rooms()
        {
            Dictionary<Guid, double[]> series_A = new()
            {
                { guid_Room_1, [20, 20] },
                { guid_Room_2, [24, 24] },
            };

            Dictionary<Guid, double[]> series_B = new()
            {
                { guid_Room_1, [21, 21] },
                { guid_Room_2, [22, 28] },
            };

            PartOIteration3Comparison partOIteration3Comparison = PartOIteration3Comparison.Create(Rooms(), series_A, series_B, [], out List<string> refusals);

            Assert.Empty(refusals);

            PartOIteration3Statistics partOIteration3Statistics = partOIteration3Comparison.Statistics;

            Assert.Equal(2, partOIteration3Statistics.Count_Rooms);
            Assert.Equal(4L, partOIteration3Statistics.Count_Values);
            Assert.Equal(1.0, partOIteration3Statistics.MeanBias, 12);
            Assert.Equal(Math.Sqrt(5.5), partOIteration3Statistics.RootMeanSquareError, 12);
            Assert.Equal(4.0, partOIteration3Statistics.MaximumAbsoluteDifference, 12);
            Assert.Equal(guid_Room_2, partOIteration3Statistics.Guid_Space_MaximumAbsoluteDifference);
            Assert.Equal(1, partOIteration3Statistics.Hour_MaximumAbsoluteDifference);

            Assert.NotEqual((1.0 + Math.Sqrt(10.0)) / 2.0, partOIteration3Statistics.RootMeanSquareError, 6);
        }

        [Fact]
        public void Per_dwelling_statistics_are_pooled_within_each_dwelling()
        {
            Dictionary<Guid, double[]> series_A = new()
            {
                { guid_Room_1, [20, 20] },
                { guid_Room_2, [24, 24] },
            };

            Dictionary<Guid, double[]> series_B = new()
            {
                { guid_Room_1, [21, 21] },
                { guid_Room_2, [22, 28] },
            };

            PartOIteration3Comparison partOIteration3Comparison = PartOIteration3Comparison.Create(Rooms(), series_A, series_B, [], out List<string> _);

            Assert.Equal(2, partOIteration3Comparison.Dwellings.Count);

            PartOIteration3DwellingStatistics partOIteration3DwellingStatistics_1 = partOIteration3Comparison.Dwellings[0];
            PartOIteration3DwellingStatistics partOIteration3DwellingStatistics_2 = partOIteration3Comparison.Dwellings[1];

            Assert.Equal("Flat 1", partOIteration3DwellingStatistics_1.Name_Dwelling);
            Assert.Equal(1.0, partOIteration3DwellingStatistics_1.Statistics.MeanBias, 12);
            Assert.Equal(1.0, partOIteration3DwellingStatistics_1.Statistics.RootMeanSquareError, 12);

            Assert.Equal("Flat 2", partOIteration3DwellingStatistics_2.Name_Dwelling);
            Assert.Equal(1.0, partOIteration3DwellingStatistics_2.Statistics.MeanBias, 12);
            Assert.Equal(Math.Sqrt(10.0), partOIteration3DwellingStatistics_2.Statistics.RootMeanSquareError, 12);
        }

        /// <summary>
        /// The FIRST hour at which the maximum occurs wins a tie, so an unchanged pairing exports the
        /// same bytes on every machine and every rerun.
        /// </summary>
        [Fact]
        public void A_tied_maximum_resolves_to_the_first_hour()
        {
            Dictionary<Guid, double[]> series_A = new() { { guid_Room_1, [20, 20, 20, 20] } };
            Dictionary<Guid, double[]> series_B = new() { { guid_Room_1, [20, 23, 21, 23] } };

            PartOIteration3Comparison partOIteration3Comparison = PartOIteration3Comparison.Create(
                [new PartOIteration3Room(guid_Room_1, "Bedroom 2", guid_Dwelling_1, "Flat 1")],
                series_A,
                series_B,
                [],
                out List<string> _);

            Assert.Equal(1, partOIteration3Comparison.Rooms[0].Hour_MaximumAbsoluteDifference);
        }

        [Fact]
        public void A_room_with_no_series_on_one_side_refuses_by_name()
        {
            Dictionary<Guid, double[]> series_A = new() { { guid_Room_1, [20, 20] } };
            Dictionary<Guid, double[]> series_B = [];

            PartOIteration3Comparison partOIteration3Comparison = PartOIteration3Comparison.Create(
                [new PartOIteration3Room(guid_Room_1, "Bedroom 2", guid_Dwelling_1, "Flat 1")],
                series_A,
                series_B,
                [],
                out List<string> refusals);

            Assert.Null(partOIteration3Comparison);
            Assert.Contains(refusals, x => x.Contains("Bedroom 2") && x.Contains("Candidate B"));
        }

        [Fact]
        public void Series_of_different_lengths_refuse_rather_than_being_compared_over_the_hours_they_share()
        {
            Dictionary<Guid, double[]> series_A = new() { { guid_Room_1, [20, 20, 20] } };
            Dictionary<Guid, double[]> series_B = new() { { guid_Room_1, [21, 21] } };

            PartOIteration3Comparison partOIteration3Comparison = PartOIteration3Comparison.Create(
                [new PartOIteration3Room(guid_Room_1, "Bedroom 2", guid_Dwelling_1, "Flat 1")],
                series_A,
                series_B,
                [],
                out List<string> refusals);

            Assert.Null(partOIteration3Comparison);
            Assert.Contains(refusals, x => x.Contains("truncated"));
        }

        [Theory]
        [InlineData(double.NaN)]
        [InlineData(double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity)]
        public void A_value_that_is_not_finite_refuses_the_whole_comparison(double value)
        {
            Dictionary<Guid, double[]> series_A = new() { { guid_Room_1, [20, value, 20] } };
            Dictionary<Guid, double[]> series_B = new() { { guid_Room_1, [21, 21, 21] } };

            PartOIteration3Comparison partOIteration3Comparison = PartOIteration3Comparison.Create(
                [new PartOIteration3Room(guid_Room_1, "Bedroom 2", guid_Dwelling_1, "Flat 1")],
                series_A,
                series_B,
                [],
                out List<string> refusals);

            Assert.Null(partOIteration3Comparison);
            Assert.Contains(refusals, x => x.Contains("not finite") && x.Contains("hour 1"));
        }

        [Fact]
        public void No_comparable_room_refuses()
        {
            PartOIteration3Comparison partOIteration3Comparison = PartOIteration3Comparison.Create([], new Dictionary<Guid, double[]>(), new Dictionary<Guid, double[]>(), [], out List<string> refusals);

            Assert.Null(partOIteration3Comparison);
            Assert.Contains(refusals, x => x.Contains("nothing to compare"));
        }

        [Fact]
        public void A_room_listed_twice_refuses()
        {
            PartOIteration3Room partOIteration3Room = new(guid_Room_1, "Bedroom 2", guid_Dwelling_1, "Flat 1");

            PartOIteration3Comparison partOIteration3Comparison = PartOIteration3Comparison.Create(
                [partOIteration3Room, partOIteration3Room],
                new Dictionary<Guid, double[]> { { guid_Room_1, [20] } },
                new Dictionary<Guid, double[]> { { guid_Room_1, [21] } },
                [],
                out List<string> refusals);

            Assert.Null(partOIteration3Comparison);
            Assert.Contains(refusals, x => x.Contains("comparable twice"));
        }

        /// <summary>
        /// TM59 statuses are carried, never re-derived - including the case that would catch a
        /// re-derivation: a room exactly on its limit, which the criteria disagree about.
        /// </summary>
        [Fact]
        public void TM59_statuses_are_carried_verbatim_including_a_room_on_its_limit()
        {
            PartOIteration3CriterionComparison partOIteration3CriterionComparison = new(
                guid_Room_1,
                "Bedroom 2",
                guid_Dwelling_1,
                "Flat 1",
                "TM59 Criterion A",
                true,
                32,
                32,
                TM59ComplianceStatus.Pass,
                32,
                32,
                TM59ComplianceStatus.Fail);

            PartOIteration3Comparison partOIteration3Comparison = PartOIteration3Comparison.Create(
                [new PartOIteration3Room(guid_Room_1, "Bedroom 2", guid_Dwelling_1, "Flat 1")],
                new Dictionary<Guid, double[]> { { guid_Room_1, [20] } },
                new Dictionary<Guid, double[]> { { guid_Room_1, [21] } },
                [partOIteration3CriterionComparison],
                out List<string> _);

            PartOIteration3CriterionComparison result = Assert.Single(partOIteration3Comparison.Criteria);

            //Identical Actual and Limit on both sides, opposite verdicts. Anything that re-derived a
            //status from the two numbers would have to make them agree.
            Assert.Equal(TM59ComplianceStatus.Pass, result.Status_A);
            Assert.Equal(TM59ComplianceStatus.Fail, result.Status_B);
            Assert.True(result.Changed);
            Assert.Equal(0, result.Delta_Actual);
            Assert.Equal(1, partOIteration3Comparison.Count_Changed);
        }

        [Fact]
        public void A_criterion_with_no_count_on_one_side_has_no_delta_rather_than_a_zero_one()
        {
            PartOIteration3CriterionComparison partOIteration3CriterionComparison = new(
                guid_Room_1, "Bedroom 2", guid_Dwelling_1, "Flat 1", "TM59 Criterion A", true, 5, 10, TM59ComplianceStatus.Pass, null, 10, TM59ComplianceStatus.Undefined);

            Assert.Null(partOIteration3CriterionComparison.Delta_Actual);
        }
    }
}
