// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;

namespace SAM.Analytical.UI
{
    /// <summary>
    /// The descriptive A/B answer of one completed Iteration 3 pairing: per-room diagnostics, the TM59
    /// criteria as each assessment reported them, and pooled and per-dwelling statistics.
    ///
    /// <para><b>It exists only where the whole chain completed</b></para>
    /// <para>
    /// <see cref="Create"/> refuses rather than returning a partial object, and
    /// <c>PartOIteration3Result</c> carries one only where the ledger is complete. A refused run therefore
    /// has no comparison at all, so a presentation layer cannot render an empty grid that looks like an
    /// answer.
    /// </para>
    ///
    /// <para><b>One pass, guid joins, no retained series</b></para>
    /// <para>
    /// The two series dictionaries are walked once, in guid order, into per-room accumulators of six
    /// numbers each. Nothing here holds a copy of an annual series, and nothing scans a list by name -
    /// which is what makes this the same shape of work at nine rooms and at five thousand.
    /// </para>
    ///
    /// <para><b>No parity threshold</b></para>
    /// <para>
    /// SAM #111 states that bit-identical temperatures are not required merely because the design airflows
    /// are identical. So there is no tolerance here, no pass mark and no verdict on the pairing: the
    /// statistics say how far apart the routes are, and an engineer decides what that means.
    /// </para>
    /// </summary>
    public class PartOIteration3Comparison
    {
        private readonly List<PartOIteration3RoomComparison> rooms = [];

        private readonly List<PartOIteration3CriterionComparison> criteria = [];

        private readonly List<PartOIteration3DwellingStatistics> dwellings = [];

        private PartOIteration3Comparison(
            IEnumerable<PartOIteration3RoomComparison> rooms,
            IEnumerable<PartOIteration3CriterionComparison> criteria,
            IEnumerable<PartOIteration3DwellingStatistics> dwellings,
            PartOIteration3Statistics partOIteration3Statistics)
        {
            this.rooms.AddRange(rooms ?? []);
            this.criteria.AddRange(criteria ?? []);
            this.dwellings.AddRange(dwellings ?? []);

            Statistics = partOIteration3Statistics;
        }

        /// <summary>Every comparable room's diagnostics, ordered by dwelling name then room name then guid.</summary>
        public List<PartOIteration3RoomComparison> Rooms => [.. rooms];

        /// <summary>Every TM59 criterion of every comparable room, both sides, carried verbatim.</summary>
        public List<PartOIteration3CriterionComparison> Criteria => [.. criteria];

        /// <summary>Pooled statistics per dwelling, ordered by dwelling name then guid.</summary>
        public List<PartOIteration3DwellingStatistics> Dwellings => [.. dwellings];

        /// <summary>Pooled statistics over every comparable room.</summary>
        public PartOIteration3Statistics Statistics { get; }

        /// <summary>How many criteria the two assessments disagreed on. A presentation fact, not a verdict.</summary>
        public int Count_Changed
        {
            get
            {
                int result = 0;

                foreach (PartOIteration3CriterionComparison partOIteration3CriterionComparison in criteria)
                {
                    if (partOIteration3CriterionComparison.Changed)
                    {
                        result++;
                    }
                }

                return result;
            }
        }

        /// <summary>
        /// Builds the comparison, or refuses.
        ///
        /// <para><b>What it refuses on, and why each is fatal rather than skipped</b></para>
        /// <list type="bullet">
        /// <item>A comparable room with no series on one side: the room was declared comparable and is not,
        /// so the declaration and the data disagree.</item>
        /// <item>Series of different lengths: which one is truncated is not knowable, and comparing the
        /// hours they happen to share is a statement about a different room.</item>
        /// <item>A non-finite value on either side: an hour nothing is known about is not an hour of
        /// agreement, and letting it through makes every pooled figure NaN or, worse, silently biased.</item>
        /// <item>No comparable room at all: a comparison over nothing is not a comparison.</item>
        /// </list>
        /// <para>
        /// These are the same rules the reconciliation stage applies before this is reached. Stated here
        /// too because this is where the values are actually walked, and a rule that is only checked
        /// upstream is a rule that stops being checked the day a second caller appears.
        /// </para>
        /// </summary>
        /// <param name="rooms">The comparable rooms, by identity.</param>
        /// <param name="series_A">Reference A's resultant temperature per design space guid.</param>
        /// <param name="series_B">Candidate B's resultant temperature per design space guid.</param>
        /// <param name="criteria">The TM59 criteria of those rooms, both sides, already carried verbatim.</param>
        /// <param name="refusals">Why no comparison was produced. Empty where one was.</param>
        public static PartOIteration3Comparison Create(
            IEnumerable<PartOIteration3Room> rooms,
            IDictionary<Guid, double[]> series_A,
            IDictionary<Guid, double[]> series_B,
            IEnumerable<PartOIteration3CriterionComparison> criteria,
            out List<string> refusals)
        {
            refusals = [];

            List<PartOIteration3Room> rooms_Temp = [];
            HashSet<Guid> guids = [];

            foreach (PartOIteration3Room partOIteration3Room in rooms ?? [])
            {
                if (partOIteration3Room is null || partOIteration3Room.Guid_Space == Guid.Empty)
                {
                    refusals.Add("A comparable room carries no identity, so its two series could not be joined.");

                    continue;
                }

                if (!guids.Add(partOIteration3Room.Guid_Space))
                {
                    refusals.Add(string.Format("Room {0} is listed as comparable twice.", partOIteration3Room.Guid_Space));

                    continue;
                }

                rooms_Temp.Add(partOIteration3Room);
            }

            if (rooms_Temp.Count == 0)
            {
                refusals.Add("No room was comparable between Reference A and Candidate B, so there is nothing to compare.");
            }

            if (refusals.Count != 0)
            {
                return null;
            }

            //Ordered by guid before the walk, so the pooled maximum's tie-break and the accumulation order
            //are the same on every machine and every rerun.
            rooms_Temp.Sort((x, y) => x.Guid_Space.CompareTo(y.Guid_Space));

            List<PartOIteration3Accumulator> partOIteration3Accumulators = [];

            foreach (PartOIteration3Room partOIteration3Room in rooms_Temp)
            {
                Guid guid_Space = partOIteration3Room.Guid_Space;

                double[] values_A = Values(series_A, guid_Space);
                double[] values_B = Values(series_B, guid_Space);

                if (values_A is null || values_B is null)
                {
                    refusals.Add(string.Format(
                        "Room '{0}' ({1}) was declared comparable but carries no resultant temperature series on the {2} side.",
                        partOIteration3Room.Name_Space,
                        guid_Space,
                        values_A is null && values_B is null ? "Reference A or the Candidate B" : values_A is null ? "Reference A" : "Candidate B"));

                    continue;
                }

                if (values_A.Length == 0)
                {
                    refusals.Add(string.Format("Room '{0}' ({1}) carries an empty resultant temperature series, so there is nothing to compare.", partOIteration3Room.Name_Space, guid_Space));

                    continue;
                }

                if (values_A.Length != values_B.Length)
                {
                    refusals.Add(string.Format(
                        "Room '{0}' ({1}) carries {2} Reference A value(s) and {3} Candidate B value(s). One of them is truncated and which is not knowable, so it was refused rather than compared over the {4} hours they share.",
                        partOIteration3Room.Name_Space,
                        guid_Space,
                        values_A.Length,
                        values_B.Length,
                        Math.Min(values_A.Length, values_B.Length)));

                    continue;
                }

                PartOIteration3Accumulator partOIteration3Accumulator = new(
                    guid_Space,
                    partOIteration3Room.Name_Space,
                    partOIteration3Room.Guid_Dwelling,
                    partOIteration3Room.Name_Dwelling);

                bool usable = true;

                for (int i = 0; i < values_A.Length; i++)
                {
                    double value_A = values_A[i];
                    double value_B = values_B[i];

                    if (double.IsNaN(value_A) || double.IsInfinity(value_A) || double.IsNaN(value_B) || double.IsInfinity(value_B))
                    {
                        refusals.Add(string.Format(
                            "Room '{0}' ({1}) carries a value that is not finite at hour {2} - Reference A {3}, Candidate B {4}. An hour nothing is known about is not an hour of agreement, so the comparison was refused rather than computed over the hours that happened to survive.",
                            partOIteration3Room.Name_Space,
                            guid_Space,
                            i,
                            value_A,
                            value_B));

                        usable = false;

                        break;
                    }

                    partOIteration3Accumulator.Add(i, value_A, value_B);
                }

                if (usable)
                {
                    partOIteration3Accumulators.Add(partOIteration3Accumulator);
                }
            }

            if (refusals.Count != 0)
            {
                return null;
            }

            List<PartOIteration3RoomComparison> roomComparisons = [];
            Dictionary<Guid, List<PartOIteration3Accumulator>> dictionary_Dwelling = [];
            Dictionary<Guid, string> dictionary_DwellingName = [];

            foreach (PartOIteration3Accumulator partOIteration3Accumulator in partOIteration3Accumulators)
            {
                roomComparisons.Add(partOIteration3Accumulator.RoomComparison());

                Guid guid_Dwelling = partOIteration3Accumulator.Guid_Dwelling;

                if (!dictionary_Dwelling.TryGetValue(guid_Dwelling, out List<PartOIteration3Accumulator> list))
                {
                    list = [];
                    dictionary_Dwelling[guid_Dwelling] = list;
                    dictionary_DwellingName[guid_Dwelling] = partOIteration3Accumulator.Name_Dwelling;
                }

                list.Add(partOIteration3Accumulator);
            }

            List<PartOIteration3DwellingStatistics> dwellings = [];

            foreach (KeyValuePair<Guid, List<PartOIteration3Accumulator>> keyValuePair in dictionary_Dwelling)
            {
                dwellings.Add(new PartOIteration3DwellingStatistics(keyValuePair.Key, dictionary_DwellingName[keyValuePair.Key], PartOIteration3Statistics.Create(keyValuePair.Value)));
            }

            dwellings.Sort(CompareDwellings);

            roomComparisons.Sort(CompareRooms);

            List<PartOIteration3CriterionComparison> criteria_Temp = [.. criteria ?? []];
            criteria_Temp.Sort(CompareCriteria);

            return new PartOIteration3Comparison(roomComparisons, criteria_Temp, dwellings, PartOIteration3Statistics.Create(partOIteration3Accumulators));
        }

        private static double[] Values(IDictionary<Guid, double[]> dictionary, Guid guid)
        {
            return dictionary is not null && dictionary.TryGetValue(guid, out double[] result) ? result : null;
        }

        private static int CompareRooms(PartOIteration3RoomComparison x, PartOIteration3RoomComparison y)
        {
            int result = string.Compare(x.Name_Dwelling, y.Name_Dwelling, StringComparison.Ordinal);
            if (result != 0)
            {
                return result;
            }

            result = string.Compare(x.Name_Space, y.Name_Space, StringComparison.Ordinal);

            return result != 0 ? result : x.Guid_Space.CompareTo(y.Guid_Space);
        }

        private static int CompareDwellings(PartOIteration3DwellingStatistics x, PartOIteration3DwellingStatistics y)
        {
            int result = string.Compare(x.Name_Dwelling, y.Name_Dwelling, StringComparison.Ordinal);

            return result != 0 ? result : x.Guid_Dwelling.CompareTo(y.Guid_Dwelling);
        }

        private static int CompareCriteria(PartOIteration3CriterionComparison x, PartOIteration3CriterionComparison y)
        {
            int result = string.Compare(x.Name_Dwelling, y.Name_Dwelling, StringComparison.Ordinal);
            if (result != 0)
            {
                return result;
            }

            result = string.Compare(x.Name_Space, y.Name_Space, StringComparison.Ordinal);
            if (result != 0)
            {
                return result;
            }

            result = x.Guid_Space.CompareTo(y.Guid_Space);

            return result != 0 ? result : string.Compare(x.Check, y.Check, StringComparison.Ordinal);
        }

        public override string ToString()
        {
            return string.Format("Iteration 3 comparison: {0}", Statistics);
        }
    }
}
