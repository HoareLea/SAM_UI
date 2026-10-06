// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Geometry.Planar;
using System;

namespace SAM.Analytical.UI
{
    /// <summary>
    /// One Part F tag offered to <see cref="PartFTagPlacement"/>: what it belongs to, where the thing it
    /// annotates actually is, how big the tag is, and how early it should claim its space.
    /// <para>
    /// Sizes are in the <b>view plane's own units [m]</b>, not pixels, because the placement is solved in
    /// the plane and has to be stable under every view transform - see
    /// <see cref="PartFAnnotationOverride"/> for the same reasoning about stored positions. A caller that
    /// measures text in pixels converts by dividing by the view scale before building this.
    /// </para>
    /// </summary>
    public class PartFTagPlacementItem
    {
        /// <summary>
        /// The guid of the object annotated - the terminal, the transfer route, the space. The identity a
        /// manual position is stored against, so this is what pairs an item with its
        /// <see cref="PartFAnnotationOverride"/>. Never a name and never an index.
        /// </summary>
        public Guid ObjectGuid { get; set; } = Guid.Empty;

        /// <summary>Which annotation on that object this is, so one object can carry several.</summary>
        public PartFAnnotationType AnnotationType { get; set; } = PartFAnnotationType.Undefined;

        /// <summary>How early the tag claims its space. See <see cref="PartFTagPriority"/>.</summary>
        public PartFTagPriority Priority { get; set; } = PartFTagPriority.Undefined;

        /// <summary>
        /// The engineering anchor: where the terminal, opening or space this tag reports on actually is, in
        /// the view plane's 2D coordinates [m].
        /// <para>
        /// <b>Never moved by placement.</b> The tag is displaced and a leader is drawn back to this point;
        /// the point itself is a statement about the building, not about the drawing.
        /// </para>
        /// </summary>
        public Point2D Anchor2D { get; set; }

        /// <summary>Tag width [m] in the view plane.</summary>
        public double Width { get; set; } = 0;

        /// <summary>Tag height [m] in the view plane.</summary>
        public double Height { get; set; } = 0;

        /// <summary>
        /// Where the tag's CENTRE must stay - normally the annotated space's own section outline, so a
        /// room's tag cannot end up reading as the room next door's. Null where there is no such
        /// constraint, which is the case for a transfer tag: it belongs to the opening between two spaces
        /// and so belongs to neither outline.
        /// <para>
        /// Only the centre is constrained, and the tag may overhang. That is the shared solver's existing
        /// meaning and it is used here deliberately - an ensuite or a WC cannot contain a whole text box,
        /// and demanding that it did would leave the smallest rooms untagged. See
        /// <c>Solver2DSettings.LimitArea</c>.
        /// </para>
        /// </summary>
        public IClosed2D LimitArea { get; set; } = null;

        /// <summary>
        /// The caller's own object for this tag - normally the <see cref="PartFOverlayMark"/> it was built
        /// from. Carried through to the result untouched so the drawing code can get back to it without a
        /// second lookup. Placement never reads it.
        /// </summary>
        public object Tag { get; set; } = null;
    }
}
