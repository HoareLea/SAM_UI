// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Query
    {
        /// <summary>
        /// Every design space inside the Approved Document O dwelling scope, with the dwelling it belongs
        /// to and both names, indexed by space guid.
        ///
        /// <para><b>One walk, one dictionary, and the names are for reading only</b></para>
        /// <para>
        /// The Iteration 3 comparison needs three things about a room - is it in scope, which dwelling
        /// groups it, and what to call it in a grid - and needs them for every room on a model that may
        /// carry five thousand. Asking them separately meant three walks and, worse, invited a
        /// <c>Find</c> by name per room, which is quadratic and wrong: three flats hold three rooms
        /// called "Bedroom 2". So they are gathered once, here, and every later stage is a dictionary
        /// lookup.
        /// </para>
        /// <para>
        /// The zones are matched <b>by guid</b> against the cluster's own object dictionary rather than
        /// reused as objects: the model being compared is a later generation than the one the scope was
        /// chosen on, and only the identity survives that. Same rule, and the same O(1) authority, as
        /// <see cref="PartODwellingSpaceGuids"/>.
        /// </para>
        /// <para>
        /// A space related to two dwelling zones keeps the first by zone guid order, so the grouping is
        /// the same on every machine. That is a model the dwelling scope itself would not accept, and
        /// choosing deterministically is better than choosing by dictionary order.
        /// </para>
        /// </summary>
        internal static Dictionary<Guid, PartOIteration3Room> PartOIteration3Rooms(AdjacencyCluster adjacencyCluster, IEnumerable<Zone> zones_Dwelling)
        {
            Dictionary<Guid, PartOIteration3Room> result = [];

            if (adjacencyCluster is null)
            {
                return result;
            }

            List<Zone> zones = [];
            foreach (Zone zone_Dwelling in zones_Dwelling ?? [])
            {
                Zone zone = zone_Dwelling is null ? null : adjacencyCluster.GetObject<Zone>(zone_Dwelling.Guid);

                if (zone is not null)
                {
                    zones.Add(zone);
                }
            }

            zones.Sort((x, y) => x.Guid.CompareTo(y.Guid));

            foreach (Zone zone in zones)
            {
                foreach (Space space in adjacencyCluster.GetRelatedObjects<Space>(zone) ?? [])
                {
                    if (space is null || result.ContainsKey(space.Guid))
                    {
                        continue;
                    }

                    result[space.Guid] = new PartOIteration3Room(space.Guid, space.Name, zone.Guid, zone.Name);
                }
            }

            return result;
        }
    }
}
