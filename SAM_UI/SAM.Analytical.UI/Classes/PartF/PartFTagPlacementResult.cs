// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Geometry.Planar;
using System;

namespace SAM.Analytical.UI
{
    /// <summary>
    /// Where one Part F tag ended up, and on what basis - so a caller can tell a placement the solver
    /// validated from one it gave up on, and from one a person put there by hand.
    /// <para>
    /// <b><see cref="Rectangle2D"/> is never null.</b> A Part F tag carries a flow rate and a compliance
    /// status, and a tag that silently disappears takes a regulatory figure off the drawing with it, so an
    /// unplaced tag is returned at its anchor instead of dropped. That is exactly why
    /// <see cref="ResultType"/> exists and must be read: geometry alone says nothing about whether the
    /// position is any good. The floor-plan space labels take the opposite decision on the same engine and
    /// blank their text, which is defensible for a room name and is not for a rate.
    /// </para>
    /// </summary>
    public class PartFTagPlacementResult
    {
        internal PartFTagPlacementResult(PartFTagPlacementItem partFTagPlacementItem, Rectangle2D rectangle2D, Solver2DResultType solver2DResultType, bool isUserPositioned)
        {
            Item = partFTagPlacementItem;
            Rectangle2D = rectangle2D;
            ResultType = solver2DResultType;
            IsUserPositioned = isUserPositioned;
        }

        /// <summary>The tag this result is for, including its untouched engineering anchor.</summary>
        public PartFTagPlacementItem Item { get; private set; }

        /// <summary>Where to draw the tag, in the view plane's 2D coordinates [m]. Never null.</summary>
        public Rectangle2D Rectangle2D { get; private set; }

        /// <summary>
        /// On what basis <see cref="Rectangle2D"/> was arrived at, straight from the shared solver and not
        /// translated, so nothing is lost between the engine and the drawing:
        /// <list type="bullet">
        /// <item><c>Solved</c> - a position the solver tested and accepted: clear of the obstacles, of the
        /// tags already placed and of the manual tags, with its centre inside its limit area.</item>
        /// <item><c>Fallback</c> - the solve spent its work budget and this tag was dropped at its anchor
        /// untested. It may overlap anything. Show it as unresolved, never as placed.</item>
        /// <item><c>Unplaced</c> - no position satisfied the rules, so the anchor was substituted here to
        /// keep the figure on the drawing. It may overlap anything.</item>
        /// <item><c>Undefined</c> - not solved at all, because <see cref="IsUserPositioned"/> is true and a
        /// person's placement is not the solver's to judge.</item>
        /// </list>
        /// </summary>
        public Solver2DResultType ResultType { get; private set; }

        /// <summary>
        /// True where this tag sits where somebody put it, from a <see cref="PartFAnnotationOverride"/>.
        /// Such a tag is not solved - and it was entered into the solve as an obstacle, so the automatic
        /// tags were placed around it rather than on top of it.
        /// </summary>
        public bool IsUserPositioned { get; private set; }

        /// <summary>Convenience: the guid of the object annotated.</summary>
        public Guid ObjectGuid
        {
            get { return Item is null ? Guid.Empty : Item.ObjectGuid; }
        }

        /// <summary>Convenience: which annotation on that object this is.</summary>
        public PartFAnnotationType AnnotationType
        {
            get { return Item is null ? PartFAnnotationType.Undefined : Item.AnnotationType; }
        }

        /// <summary>Convenience: the caller's own object, normally the <see cref="PartFOverlayMark"/>.</summary>
        public object Tag
        {
            get { return Item?.Tag; }
        }

        /// <summary>
        /// True where the position cannot be relied on not to overlap something - a budget fallback or an
        /// unplaced tag. The one question a renderer usually wants to ask.
        /// </summary>
        public bool IsOverlapPossible
        {
            get { return ResultType == Solver2DResultType.Fallback || ResultType == Solver2DResultType.Unplaced; }
        }

        /// <summary>
        /// The leader line from the engineering anchor to the nearest point on the tag, or null where the
        /// tag still covers its anchor and a leader would be a line inside a box.
        /// <para>
        /// Derived here, in the view layer, and NOT in <c>SAM.Geometry</c>: a leader is a drawing
        /// convention about how an annotation is attached to what it annotates, and the shared solver has
        /// no business knowing that annotations exist. Everything it needs is already in this result.
        /// </para>
        /// <para>
        /// The anchor returned is a copy. <see cref="Point2D"/> is mutable and this must not hand out a
        /// reference that a caller drawing a leader could move - the anchor is a statement about where the
        /// terminal is.
        /// </para>
        /// </summary>
        public Segment2D Leader2D()
        {
            Point2D point2D_Anchor = Item?.Anchor2D;
            if (point2D_Anchor is null || Rectangle2D is null)
            {
                return null;
            }

            //Inside or on the tag: there is nothing to lead to.
            if (Rectangle2D.InRange(point2D_Anchor))
            {
                return null;
            }

            Point2D point2D_Closest = null;
            double distance_Closest = double.MaxValue;

            foreach (Segment2D segment2D in Rectangle2D.GetSegments())
            {
                Point2D point2D = segment2D?.Closest(point2D_Anchor);
                if (point2D is null)
                {
                    continue;
                }

                double distance = point2D.Distance(point2D_Anchor);
                if (distance < distance_Closest)
                {
                    distance_Closest = distance;
                    point2D_Closest = point2D;
                }
            }

            if (point2D_Closest is null || distance_Closest < Core.Tolerance.Distance)
            {
                return null;
            }

            return new Segment2D(new Point2D(point2D_Anchor), point2D_Closest);
        }
    }
}
