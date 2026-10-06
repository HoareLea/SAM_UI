// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Systems;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Query
    {
        /// <summary>
        /// Binds each resolved analytical AHU evidence row to the single AirSystem lineage row produced
        /// by SAM_Systems. Empty means the mapping is complete; any returned reason makes the caller
        /// refuse the materialisation stage.
        /// </summary>
        internal static List<string> PartOIteration3EquipmentBindings(
            IEnumerable<PartOIteration3EquipmentEvidence> equipment,
            IEnumerable<MechanicalVentilationBinding> bindings)
        {
            Dictionary<Guid, Guid> airSystemByAirHandlingUnit = [];
            Dictionary<Guid, Guid> airHandlingUnitByAirSystem = [];
            List<string> result = [];

            foreach (MechanicalVentilationBinding mechanicalVentilationBinding in bindings ?? [])
            {
                if (mechanicalVentilationBinding is null
                    || mechanicalVentilationBinding.BindingType != MechanicalVentilationBindingType.AirSystem)
                {
                    continue;
                }

                if (airSystemByAirHandlingUnit.TryGetValue(mechanicalVentilationBinding.Guid_Analytical, out Guid guid_AirSystem)
                    && guid_AirSystem != mechanicalVentilationBinding.Guid_Systems)
                {
                    result.Add(string.Format(
                        "Air handling unit {0} was bound to more than one materialised air system, so its equipment evidence has no single target.",
                        mechanicalVentilationBinding.Guid_Analytical));

                    continue;
                }

                //The reverse, too: one physical air system carrying two units' equipment evidence is not one
                //row per physical unit, and is refused here - before any simulation - rather than only on a
                //later review.
                if (airHandlingUnitByAirSystem.TryGetValue(mechanicalVentilationBinding.Guid_Systems, out Guid guid_AirHandlingUnit)
                    && guid_AirHandlingUnit != mechanicalVentilationBinding.Guid_Analytical)
                {
                    result.Add(string.Format(
                        "Materialised air system {0} was bound to more than one air handling unit ({1} and {2}), so it cannot carry either unit's equipment evidence.",
                        mechanicalVentilationBinding.Guid_Systems,
                        guid_AirHandlingUnit,
                        mechanicalVentilationBinding.Guid_Analytical));

                    continue;
                }

                airSystemByAirHandlingUnit[mechanicalVentilationBinding.Guid_Analytical] = mechanicalVentilationBinding.Guid_Systems;
                airHandlingUnitByAirSystem[mechanicalVentilationBinding.Guid_Systems] = mechanicalVentilationBinding.Guid_Analytical;
            }

            foreach (PartOIteration3EquipmentEvidence partOIteration3EquipmentEvidence in equipment ?? [])
            {
                if (partOIteration3EquipmentEvidence is null)
                {
                    result.Add("A resolved equipment row is missing, so it cannot be bound to a materialised air system.");
                    continue;
                }

                if (!airSystemByAirHandlingUnit.TryGetValue(partOIteration3EquipmentEvidence.Guid_AirHandlingUnit, out Guid guid_AirSystem)
                    || !partOIteration3EquipmentEvidence.BindAirSystem(guid_AirSystem))
                {
                    result.Add(string.Format(
                        "Air handling unit '{0}' ({1}) resolved equipment behaviour but has no single materialised air-system binding.",
                        partOIteration3EquipmentEvidence.Name_AirHandlingUnit,
                        partOIteration3EquipmentEvidence.Guid_AirHandlingUnit));
                }
            }

            return result;
        }
    }
}
