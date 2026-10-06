// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.UI
{
    /// <summary>
    /// Resolves what the user clicked on a floor plan back to the Part F assessment, by stable
    /// <see cref="Guid"/> only.
    /// <para>
    /// This exists because the obvious version was wrong. Taking the first selected object and looking
    /// for its terminals reported "no Part F terminal is required in this space" for a bedroom the same
    /// view was drawing a 63 l/s supply arrow on: a click can select more than one object, the control
    /// returns them from an unordered set, and "first" was whichever one enumeration happened to yield -
    /// a wall, an overlapping space, a neighbouring flat.
    /// </para>
    /// <para>
    /// The fix is to search the whole selection for something the assessment actually knows about,
    /// rather than to guess and then fail. Matching is on guid throughout; never on object identity,
    /// because the plan is built from its own clone of the model and no object on it is reference-equal
    /// to anything in the result.
    /// </para>
    /// </summary>
    public static class PartFSelectionResolver
    {
        /// <summary>
        /// The space in <paramref name="sAMObjects"/> that the assessment holds terminals for, or
        /// <see cref="Guid.Empty"/> where it holds none of them.
        /// <para>
        /// Order matters and is deliberate: a selection containing both an assessed space and something
        /// else resolves to the assessed space, whatever order they arrive in.
        /// </para>
        /// </summary>
        public static Guid SpaceGuid(IEnumerable<SAMObject> sAMObjects, PartFComplianceResult partFComplianceResult)
        {
            if (sAMObjects is null || partFComplianceResult is null)
            {
                return Guid.Empty;
            }

            HashSet<Guid> guids = [.. (partFComplianceResult.Terminals ?? []).Select(x => x.SpaceGuid)];

            foreach (SAMObject sAMObject in sAMObjects)
            {
                if (sAMObject is not null && guids.Contains(sAMObject.Guid))
                {
                    return sAMObject.Guid;
                }
            }

            return Guid.Empty;
        }

        /// <summary>
        /// The internal transfer route crossing one of the selected objects, or null. An aperture is
        /// matched to the route recorded against it, so clicking a door on the plan selects its route.
        /// </summary>
        public static PartFDoorTransferData TransferPath(IEnumerable<SAMObject> sAMObjects, PartFComplianceResult partFComplianceResult)
        {
            if (sAMObjects is null || partFComplianceResult is null)
            {
                return null;
            }

            List<PartFDoorTransferData> transferPaths = [.. (partFComplianceResult.TransferPaths ?? []).Where(x => x is not null && x.IsInternalDwellingDoor && x.ApertureGuid != Guid.Empty)];

            foreach (SAMObject sAMObject in sAMObjects)
            {
                if (sAMObject is null)
                {
                    continue;
                }

                PartFDoorTransferData result = transferPaths.Find(x => x.ApertureGuid == sAMObject.Guid);
                if (result is not null)
                {
                    return result;
                }
            }

            return null;
        }

        /// <summary>Every terminal the assessment holds for one space, in a stable order.</summary>
        public static List<PartFVentilationTerminalRequirement> Terminals(Guid guid_Space, PartFComplianceResult partFComplianceResult)
        {
            if (guid_Space == Guid.Empty || partFComplianceResult is null)
            {
                return [];
            }

            return [.. (partFComplianceResult.Terminals ?? []).Where(x => x is not null && x.SpaceGuid == guid_Space).OrderBy(x => x.TerminalRole)];
        }

        /// <summary>Every internal transfer route touching one space, upstream or downstream.</summary>
        public static List<PartFDoorTransferData> TransferPaths(Guid guid_Space, PartFComplianceResult partFComplianceResult)
        {
            if (guid_Space == Guid.Empty || partFComplianceResult is null)
            {
                return [];
            }

            return [.. (partFComplianceResult.TransferPaths ?? []).Where(x => x is not null && x.IsInternalDwellingDoor && (x.UpstreamSpaceGuid == guid_Space || x.DownstreamSpaceGuid == guid_Space))];
        }
    }
}
