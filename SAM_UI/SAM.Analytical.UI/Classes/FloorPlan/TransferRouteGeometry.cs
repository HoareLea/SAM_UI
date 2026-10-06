// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Geometry.Planar;
using SAM.Geometry.Spatial;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.UI
{
    /// <summary>
    /// Where a transfer route between two spaces physically crosses, on one floor plan.
    /// <para>
    /// <b>Geometry, and only geometry.</b> Everything here answers "where, on this plan, is the thing the
    /// two spaces are separated by", from the model's panels and apertures. Nothing here reads a flow
    /// rate, a requirement or a compliance status, and nothing here takes an Approved Document F type as a
    /// parameter or returns one - which is exactly what lets it be shared.
    /// </para>
    /// <para>
    /// <b>Why it is shared.</b> <see cref="PartFFloorPlanOverlay"/> and
    /// <see cref="DesignAirFlowFloorPlanOverlay"/> read two independent airflow authorities that are meant
    /// to be able to disagree - Approved Document F's Table 1.2 sizing, and the model's current design
    /// duty - and neither may read the other's numbers. But there is only one wall between two rooms, and
    /// only one place on the plan an arrow between them can honestly cross. Duplicating the sectioning to
    /// keep the two overlays apart would not be independence; it would be two chances to put the same
    /// arrow in two different places on the same drawing.
    /// </para>
    /// </summary>
    internal static class TransferRouteGeometry
    {
        /// <summary>
        /// Where the route between two spaces crosses on this plan: the modelled door where one is named,
        /// and otherwise the separating wall. Null where neither appears on the plan.
        /// </summary>
        /// <param name="adjacencyCluster">The model the plan is drawn from.</param>
        /// <param name="plane">The floor plan's section plane.</param>
        /// <param name="guid_Aperture">
        /// The door aperture the route crosses, or <see cref="Guid.Empty"/> where the caller has none. An
        /// aperture that is named but does not reach this plan falls back to the partition, rather than
        /// reporting the route unplaceable.
        /// </param>
        /// <param name="guid_Space_1">One of the two spaces.</param>
        /// <param name="guid_Space_2">The other space.</param>
        /// <param name="isDoor">True where the point returned is a modelled door's own centre.</param>
        internal static Point2D OpeningPoint2D(AdjacencyCluster adjacencyCluster, Plane plane, Guid guid_Aperture, Guid guid_Space_1, Guid guid_Space_2, out bool isDoor)
        {
            return OpeningPoint2D(adjacencyCluster, plane, guid_Aperture == Guid.Empty ? null : Aperture(adjacencyCluster, guid_Aperture), guid_Space_1, guid_Space_2, out isDoor);
        }

        /// <summary>
        /// The same answer, for a caller that already HAS the aperture object.
        /// <para>
        /// Preferred wherever it is available, because the guid overload above has to scan the model's
        /// panels to find the aperture again - once per route, which on a large model with many internal
        /// doors is a scan of every panel per door.
        /// </para>
        /// </summary>
        internal static Point2D OpeningPoint2D(AdjacencyCluster adjacencyCluster, Plane plane, Aperture aperture, Guid guid_Space_1, Guid guid_Space_2, out bool isDoor)
        {
            isDoor = false;

            //A modelled door is the best answer and is used wherever there is one.
            if (aperture is not null)
            {
                Point2D result_Aperture = AperturePoint2D(plane, aperture);
                if (result_Aperture is not null)
                {
                    isDoor = true;
                    return result_Aperture;
                }
            }

            //No door aperture in the model. The two spaces still adjoin through a real separating panel,
            //and that panel is drawn on the plan, so the arrow crosses the wall the reader can see. This
            //is the common case: many analytical models carry no internal door apertures at all.
            return PartitionPoint2D(adjacencyCluster, plane, guid_Space_1, guid_Space_2);
        }

        /// <summary>
        /// A door's own centre projected onto the plan, or null where it has no face. A door reaches the
        /// floor, so its centre is above the cut level rather than on it; projecting is what puts the mark
        /// in the opening.
        /// </summary>
        internal static Point2D AperturePoint2D(Plane plane, Aperture aperture)
        {
            Face3D face3D = aperture?.GetFace3D();

            Point3D point3D = face3D?.GetCentroid();

            return point3D is null ? null : Geometry.Spatial.Query.Convert(plane, point3D);
        }

        /// <summary>The aperture behind a guid, or null where the model does not carry it.</summary>
        private static Aperture Aperture(AdjacencyCluster adjacencyCluster, Guid guid_Aperture)
        {
            foreach (Panel panel in adjacencyCluster?.GetPanels() ?? [])
            {
                Aperture result = panel?.Apertures?.Find(x => x is not null && x.Guid == guid_Aperture);
                if (result is not null)
                {
                    return result;
                }
            }

            return null;
        }

        /// <summary>
        /// The midpoint of the longest cut run of the wall the two spaces share - the middle of the widest
        /// run of partition between them, which is where a door would be if one were modelled. Null where
        /// they share no panel this plan cuts.
        /// </summary>
        internal static Point2D PartitionPoint2D(AdjacencyCluster adjacencyCluster, Plane plane, Guid guid_Space_1, Guid guid_Space_2)
        {
            List<Panel> panels_1 = Panels(adjacencyCluster, guid_Space_1);
            List<Panel> panels_2 = Panels(adjacencyCluster, guid_Space_2);

            if (panels_1 is null || panels_2 is null)
            {
                return null;
            }

            HashSet<Guid> guids_2 = [.. panels_2.Where(x => x is not null).Select(x => x.Guid)];

            List<Panel> panels_Shared = [.. panels_1.Where(x => x is not null && guids_2.Contains(x.Guid))];
            if (panels_Shared.Count == 0)
            {
                return null;
            }

            Dictionary<Panel, List<ISegmentable3D>> dictionary = Analytical.Query.SectionDictionary<ISegmentable3D>(panels_Shared, plane);
            if (dictionary is null)
            {
                return null;
            }

            Segment2D segment2D_Longest = null;

            foreach (KeyValuePair<Panel, List<ISegmentable3D>> keyValuePair in dictionary)
            {
                foreach (ISegmentable3D segmentable3D in keyValuePair.Value ?? [])
                {
                    foreach (Segment3D segment3D in segmentable3D?.GetSegments() ?? [])
                    {
                        Point2D point2D_1 = Geometry.Spatial.Query.Convert(plane, segment3D?[0]);
                        Point2D point2D_2 = Geometry.Spatial.Query.Convert(plane, segment3D?[1]);

                        if (point2D_1 is null || point2D_2 is null)
                        {
                            continue;
                        }

                        Segment2D segment2D = new(point2D_1, point2D_2);

                        if (segment2D_Longest is null || segment2D.GetLength() > segment2D_Longest.GetLength())
                        {
                            segment2D_Longest = segment2D;
                        }
                    }
                }
            }

            return segment2D_Longest?.Mid();
        }

        /// <summary>
        /// The panels bounding one space.
        /// <para>
        /// The space itself is found through <see cref="AdjacencyCluster.GetObject{T}(Guid)"/> - a
        /// dictionary lookup keyed on the object's own type and guid, not a scan of every space in the
        /// model. <c>GetSpaces()?.Find(...)</c> here would make each route's geometry a linear scan of the
        /// WHOLE space list, twice, which on a large model is quadratic in the number of spaces.
        /// </para>
        /// </summary>
        private static List<Panel> Panels(AdjacencyCluster adjacencyCluster, Guid guid_Space)
        {
            Space space = adjacencyCluster?.GetObject<Space>(guid_Space);

            return space is null ? null : adjacencyCluster.GetPanels(space);
        }
    }
}
