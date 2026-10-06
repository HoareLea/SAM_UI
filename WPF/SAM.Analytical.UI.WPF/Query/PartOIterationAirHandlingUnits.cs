// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Query
    {
        /// <summary>
        /// The air handling units that belong to a Part O run's equipment scope: the units of the systems
        /// the Part O preparation built (or reused) for the dwellings the run was prepared over.
        /// <para>
        /// <b>Scope by relation, never by name.</b> A unit is in scope where a ventilation system bound to
        /// it (through <c>VentilationSystemParameter.SupplyUnitName</c>, the binding every SAM workflow
        /// resolves by) is connected to at least one <b>design ventilation terminal</b> - a relation only
        /// the Part O preparation creates - and serves at least one space of the run's dwelling zones. The
        /// model's own authored systems fail the first condition (they are related to spaces, never to
        /// design terminals), and a Base MVHR system left over from a wider earlier preparation of the same
        /// model fails the second where none of its spaces is in this run's scope.
        /// </para>
        /// <para>
        /// Nothing is changed - this is a reporting scope, not a model edit. An excluded unit keeps its
        /// system, its relations and its state exactly as authored.
        /// </para>
        /// </summary>
        /// <param name="adjacencyCluster">The model the evidence is read from.</param>
        /// <param name="zones_Dwelling">
        /// The run's dwelling scope, matched against the model by guid. Null means unconstrained - any
        /// terminal-connected unit is in scope.
        /// </param>
        internal static List<AirHandlingUnit> PartOIterationAirHandlingUnits(AdjacencyCluster adjacencyCluster, IEnumerable<Zone> zones_Dwelling)
        {
            List<AirHandlingUnit> result = [];

            if (adjacencyCluster is null)
            {
                return result;
            }

            //The dwelling scope as space identities. Resolved once, by guid - the zones handed in may belong
            //to an earlier generation of the model than the cluster being read.
            HashSet<Guid> guids_Space_Scope = null;
            if (zones_Dwelling is not null)
            {
                guids_Space_Scope = PartODwellingSpaceGuids(adjacencyCluster, zones_Dwelling);
            }

            HashSet<Guid> guids_AirHandlingUnit = [];

            foreach (VentilationSystem ventilationSystem in adjacencyCluster.GetObjects<VentilationSystem>() ?? [])
            {
                if (ventilationSystem is null)
                {
                    continue;
                }

                //Condition 1: connected to a design ventilation terminal. Only the Part O preparation
                //relates systems to design terminals, so this is the positive identification of the
                //iteration's own plant - a system the model merely carries never has one.
                List<VentilationTerminal> ventilationTerminals = adjacencyCluster.GetRelatedObjects<VentilationTerminal>(ventilationSystem);
                if (ventilationTerminals is null || ventilationTerminals.Count == 0)
                {
                    continue;
                }

                //Condition 2: serves this run's dwelling scope.
                if (guids_Space_Scope is not null)
                {
                    bool serves = false;

                    foreach (Space space in adjacencyCluster.GetRelatedObjects<Space>(ventilationSystem) ?? [])
                    {
                        if (space is not null && guids_Space_Scope.Contains(space.Guid))
                        {
                            serves = true;
                            break;
                        }
                    }

                    if (!serves)
                    {
                        continue;
                    }
                }

                AirHandlingUnit airHandlingUnit = Analytical.Query.AirHandlingUnit(adjacencyCluster, ventilationSystem);
                if (airHandlingUnit is not null && guids_AirHandlingUnit.Add(airHandlingUnit.Guid))
                {
                    result.Add(airHandlingUnit);
                }
            }

            return result;
        }
    }
}
