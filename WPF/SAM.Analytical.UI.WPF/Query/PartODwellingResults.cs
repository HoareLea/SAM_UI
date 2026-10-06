// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Query
    {
        /// <summary>
        /// One result per dwelling from a production TM59 assessment - the tally a mixed Part O design shows, one row
        /// per dwelling.
        /// </summary>
        /// <param name="partOTM59Assessment">The assessment of a completed run.</param>
        /// <param name="adjacencyCluster">The assessed model's cluster - which space belongs to which dwelling.</param>
        /// <param name="guids_Zone">The dwellings to report: the run's assessed scope.</param>
        internal static List<PartODwellingResult> PartODwellingResults(PartOTM59Assessment? partOTM59Assessment, AdjacencyCluster? adjacencyCluster, IEnumerable<Guid>? guids_Zone)
        {
            if (partOTM59Assessment is null || !partOTM59Assessment.IsAssessed)
            {
                List<PartODwellingResult> result = [];
                foreach (Guid guid_Zone in guids_Zone ?? [])
                {
                    result.Add(new PartODwellingResult(guid_Zone, PartODwellingOutcome.NotAssessed));
                }

                return result;
            }

            return PartODwellingResults(partOTM59Assessment.OccupiedSpaceStatuses, partOTM59Assessment.SpaceGuids_Unassessed, adjacencyCluster, guids_Zone);
        }

        /// <summary>
        /// The dwelling tally, from each space's own TM59 status.
        ///
        /// <para><b>Counted, never decided</b></para>
        /// <para>
        /// Each space's status is SAM's (<see cref="PartOTM59Assessment.OccupiedSpaceStatuses"/>). The dwelling rule is
        /// the TM59 result window's partial-assessment rule at dwelling scale, and nothing more:
        /// </para>
        /// <list type="bullet">
        /// <item><b>FAIL</b> where any occupied space of the dwelling failed - certain whatever else went unassessed;</item>
        /// <item><b>PASS</b> only where at least one occupied space passed, none failed, and no space of the dwelling is
        /// among those the assessment could not assess - a pass with a hole in it is not a pass;</item>
        /// <item><b>NOT ASSESSED</b> otherwise.</item>
        /// </list>
        /// <para>
        /// A space with no occupied-space status and not unassessed - a store, a circulation space with no TM59
        /// criterion - is neither counted nor a hole, exactly as in the whole-run counts.
        /// </para>
        /// </summary>
        internal static List<PartODwellingResult> PartODwellingResults(IReadOnlyDictionary<Guid, Analytical.TM59ComplianceStatus>? occupiedSpaceStatuses, IEnumerable<Guid>? spaceGuids_Unassessed, AdjacencyCluster? adjacencyCluster, IEnumerable<Guid>? guids_Zone)
        {
            List<PartODwellingResult> result = [];

            HashSet<Guid> guids_Unassessed = [.. spaceGuids_Unassessed ?? []];

            Dictionary<Guid, Zone> dictionary_Zone = [];
            foreach (Zone zone in adjacencyCluster?.GetZones() ?? [])
            {
                if (zone is not null)
                {
                    dictionary_Zone[zone.Guid] = zone;
                }
            }

            foreach (Guid guid_Zone in guids_Zone ?? [])
            {
                if (adjacencyCluster is null || !dictionary_Zone.TryGetValue(guid_Zone, out Zone? zone))
                {
                    result.Add(new PartODwellingResult(guid_Zone, PartODwellingOutcome.NotAssessed));
                    continue;
                }

                int pass = 0;
                int fail = 0;
                int notAssessed = 0;
                List<string> names_Fail = [];

                List<Space> spaces = adjacencyCluster.GetRelatedObjects<Space>(zone) ?? [];
                spaces.Sort((x, y) => string.Compare(x?.Name, y?.Name, StringComparison.OrdinalIgnoreCase));

                foreach (Space space in spaces)
                {
                    if (space is null)
                    {
                        continue;
                    }

                    if (guids_Unassessed.Contains(space.Guid))
                    {
                        notAssessed++;
                    }

                    if (occupiedSpaceStatuses is null || !occupiedSpaceStatuses.TryGetValue(space.Guid, out Analytical.TM59ComplianceStatus tM59ComplianceStatus))
                    {
                        continue;
                    }

                    if (tM59ComplianceStatus == Analytical.TM59ComplianceStatus.Fail)
                    {
                        fail++;

                        if (names_Fail.Count < PartODwellingResult.FailingSpaceNamesKept)
                        {
                            names_Fail.Add(space.Name ?? space.Guid.ToString());
                        }
                    }
                    else if (tM59ComplianceStatus == Analytical.TM59ComplianceStatus.Pass)
                    {
                        pass++;
                    }
                }

                PartODwellingOutcome partODwellingOutcome = fail != 0
                    ? PartODwellingOutcome.Fail
                    : pass != 0 && notAssessed == 0 ? PartODwellingOutcome.Pass : PartODwellingOutcome.NotAssessed;

                PartODwellingResult partODwellingResult = new(guid_Zone, partODwellingOutcome)
                {
                    SpaceCount_Pass = pass,
                    SpaceCount_Fail = fail,
                    SpaceCount_NotAssessed = notAssessed,
                };

                partODwellingResult.FailingSpaceNames.AddRange(names_Fail);

                result.Add(partODwellingResult);
            }

            return result;
        }
    }
}
