// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Query
    {
        /// <summary>
        /// Which simulated spaces are the generated TAS plant zones of the design model's air handling
        /// units - the zones <c>SAM.Analytical.Tas.Modify.UpdateIZAMs</c> creates beside the building, one
        /// per unit that carries an <c>AirHandlingUnitAirMovement</c>, renames to the unit's own name, and
        /// simulates as part of the building. They are not rooms: they have no design space, no internal
        /// condition worth restoring, and nothing a TM59 assessment could say anything about.
        /// <para>
        /// <b>Identification is positive, and there are two conditions, both required.</b> A simulated space
        /// is a plant zone only where (1) it does not resolve to any design space through the
        /// <see cref="SimulationSpaceMap"/> - a resolved space is a room whatever it is called, and is never
        /// touched - and (2) its name is exactly the name of an air handling unit in the design model, which
        /// is what the export named the generated zone. Condition (1) alone is the unresolved state itself
        /// and must keep warning: a genuinely unmappable room is a defect worth saying. Condition (2) alone
        /// would be a name match, and a name is never an identity here.
        /// </para>
        /// <para>
        /// The comparison normalises the way the export's own zone lookup does - trimmed, case-insensitive
        /// (<c>UpdateIZAMs</c> matches zones with <c>Trim().ToUpper()</c> semantics) - because the identity
        /// being recognised is the TAS zone the export produced, not a string the user typed.
        /// </para>
        /// </summary>
        /// <param name="spaces_Simulated">The spaces of the model read back from the simulation results.</param>
        /// <param name="airHandlingUnits_Design">The air handling units of the model the simulation ran for.</param>
        /// <param name="simulationSpaceMap">The identity map between the two models.</param>
        internal static List<Space> PartOPlantZoneSpaces(IEnumerable<Space> spaces_Simulated, IEnumerable<AirHandlingUnit> airHandlingUnits_Design, SimulationSpaceMap simulationSpaceMap)
        {
            List<Space> result = [];

            if (spaces_Simulated is null || simulationSpaceMap is null)
            {
                return result;
            }

            HashSet<string> names_AirHandlingUnit = new(StringComparer.OrdinalIgnoreCase);

            foreach (AirHandlingUnit airHandlingUnit in airHandlingUnits_Design ?? [])
            {
                if (!string.IsNullOrWhiteSpace(airHandlingUnit?.Name))
                {
                    names_AirHandlingUnit.Add(airHandlingUnit.Name.Trim());
                }
            }

            if (names_AirHandlingUnit.Count == 0)
            {
                return result;
            }

            foreach (Space space in spaces_Simulated)
            {
                if (space is null || string.IsNullOrWhiteSpace(space.Name))
                {
                    continue;
                }

                //Condition (1): unresolved. A space the map resolved IS a design room - including a room that
                //happens to share a unit's name - and is never a plant zone.
                if (simulationSpaceMap.Design(space) is not null)
                {
                    continue;
                }

                //Condition (2): named after a design-model air handling unit.
                if (names_AirHandlingUnit.Contains(space.Name.Trim()))
                {
                    result.Add(space);
                }
            }

            return result;
        }
    }
}
