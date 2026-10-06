// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using SAM.Geometry.Planar;
using System;
using System.Text.Json.Nodes;

namespace SAM.Analytical.UI
{
    /// <summary>
    /// Where a person has dragged one Part F annotation, in the view plane's own coordinates.
    /// <para>
    /// <b>Never screen pixels.</b> A pixel position is meaningless the moment the view is zoomed, panned
    /// or the window resized, and a saved drawing whose annotations move when it is reopened at a
    /// different window size is not a drawing. <see cref="Position2D"/> is in the same world-plane
    /// coordinates the overlay marks and the floor plan's own geometry use, so it is stable under every
    /// view transform.
    /// </para>
    /// <para>
    /// <b>Identity is the annotated object's own guid.</b> For a terminal that is the
    /// <c>PartFVentilationTerminalRequirement</c> guid, NOT the space guid and a role: the assessment
    /// deliberately allows more than one terminal of a role in one space - a studio carries supply and
    /// local kitchen extract, and nothing prevents two general extract terminals in one utility room -
    /// so a space-plus-role key would collide exactly where the drawing is most crowded. For a transfer
    /// annotation it is the aperture or route guid; for a space annotation, the space guid.
    /// </para>
    /// <para>
    /// The view guid is deliberately absent: an override belongs to the view that stores it. The
    /// operating mode is absent too - the anchor is the same terminal in continuous, high, setback and
    /// measured, only the text changes, so a label tidied in one mode stays tidy in the others.
    /// </para>
    /// </summary>
    public class PartFAnnotationOverride : IJSAMObject
    {
        public PartFAnnotationOverride()
        {
        }

        public PartFAnnotationOverride(Guid objectGuid, PartFAnnotationType partFAnnotationType, Point2D point2D)
        {
            ObjectGuid = objectGuid;
            AnnotationType = partFAnnotationType;
            Position2D = point2D;
        }

        public PartFAnnotationOverride(JsonObject jsonObject)
        {
            FromJsonObject(jsonObject);
        }

        public PartFAnnotationOverride(PartFAnnotationOverride partFAnnotationOverride)
        {
            if (partFAnnotationOverride is not null)
            {
                ObjectGuid = partFAnnotationOverride.ObjectGuid;
                AnnotationType = partFAnnotationOverride.AnnotationType;
                Position2D = partFAnnotationOverride.Position2D is null ? null : new Point2D(partFAnnotationOverride.Position2D);
            }
        }

        /// <summary>
        /// The guid of the thing annotated: a terminal, an aperture or a space. Stable across a
        /// recalculation, which a name or an index is not.
        /// </summary>
        public Guid ObjectGuid { get; set; } = Guid.Empty;

        /// <summary>Which annotation on that object this position belongs to.</summary>
        public PartFAnnotationType AnnotationType { get; set; } = PartFAnnotationType.Undefined;

        /// <summary>
        /// The label's position [m] in the view plane's own 2D coordinates.
        /// <para>
        /// The label's <b>centre</b>, not a corner - <see cref="PartFTagPlacement"/> reads it that way. The
        /// centre is what stays put when the text changes: switching the operating mode from continuous to
        /// high can turn "8 l/s" into "13 l/s", and a position held as a corner would slide the label every
        /// time its text grew or shrank, having been placed by hand precisely so that it would not move.
        /// </para>
        /// </summary>
        public Point2D Position2D { get; set; }

        /// <summary>
        /// True where this record represents a deliberate placement by a person, and so must be excluded
        /// from automatic layout.
        /// </summary>
        public bool IsUserPositioned
        {
            get { return ObjectGuid != Guid.Empty && Position2D is not null; }
        }

        public bool FromJsonObject(JsonObject jsonObject)
        {
            if (jsonObject is null)
            {
                return false;
            }

            ObjectGuid = PartFViewJson.Guid(jsonObject, "ObjectGuid");

            if (jsonObject.ContainsKey("AnnotationType"))
            {
                AnnotationType = Core.Query.Enum<PartFAnnotationType>(PartFViewJson.String(jsonObject, "AnnotationType"));
            }

            if (jsonObject["Position2D"] is JsonObject jsonObject_Position)
            {
                Position2D = new Point2D(jsonObject_Position);
            }

            return true;
        }

        public JsonObject ToJsonObject()
        {
            JsonObject result = new()
            {
                ["_type"] = Core.Query.FullTypeName(this),
                ["ObjectGuid"] = ObjectGuid.ToString(),
                ["AnnotationType"] = AnnotationType.ToString(),
            };

            if (Position2D is not null)
            {
                result["Position2D"] = Position2D.ToJsonObject();
            }

            return result;
        }
    }
}
