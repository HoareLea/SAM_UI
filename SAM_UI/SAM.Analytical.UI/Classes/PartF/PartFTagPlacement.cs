// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Geometry.Planar;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.UI
{
    /// <summary>
    /// Places Part F tags on a floor plan by handing them to the shared 2D placement engine,
    /// <see cref="Solver2D"/>.
    /// <para>
    /// <b>This is an adapter and not an algorithm.</b> It translates Part F tags into the engine's own
    /// terms and translates the results back; every decision about where a rectangle may go is the
    /// engine's. There is deliberately no second placement algorithm in SAM: the same engine places the
    /// floor plan's space labels and the Mollier chart's labels, and a Part F plan that pushed its tags
    /// around by its own rules would drift away from both of them and from any annotation added later.
    /// </para>
    /// <para>
    /// What the adapter contributes is the four things the engine has no opinion about:
    /// </para>
    /// <list type="number">
    /// <item>the <b>order</b> tags claim space in - <see cref="PartFTagPriority"/>, a named policy rather
    /// than numbers in a renderer;</item>
    /// <item><b>manually positioned tags become obstacles</b>, not omissions, so an automatic tag is never
    /// dropped on top of one a person placed deliberately;</item>
    /// <item>the <b>leader line</b>, built from the engineering anchor and the solved rectangle at
    /// <see cref="PartFTagPlacementResult.Leader2D"/> - annotation presentation, which is why it is here
    /// and not in <c>SAM.Geometry</c>;</item>
    /// <item>a tag that cannot be placed is <b>kept on the drawing at its anchor and flagged</b>, because a
    /// Part F tag carries a regulatory figure and must not silently vanish.</item>
    /// </list>
    /// <para>
    /// <b>Determinism.</b> The same tags always produce the same layout. The order items are placed in is
    /// derived from the tags themselves - priority, then annotation type, then the annotated object's guid -
    /// and never from the order a caller's collection happened to enumerate in, so a set assembled through
    /// a dictionary or a hash set still draws identically every time. The engine's own ordering and work
    /// budget are deterministic for the same reason. A saved drawing that redraws with its tags somewhere
    /// else is not a saved drawing.
    /// </para>
    /// <para>
    /// <b>When to call it.</b> On a change of model content, dwelling, level, visible annotation types,
    /// dwelling filter, operating mode (the text changes width), manual position or
    /// <see cref="PixelsPerMetre">annotation scale</see> - and on an explicit reset or auto-arrange.
    /// </para>
    /// <para>
    /// <b>Never on a pan, a zoom or a repaint.</b> Camera navigation is not an auto-arrange command. An
    /// engineering drawing whose annotation rearranged itself while somebody looked around the plan would be
    /// a drawing you could not check, and re-solving on every mouse move would crawl as well. The layout is
    /// solved for the view's annotation scale and then transformed; nothing here takes the view transform,
    /// which is what makes that guarantee structural rather than a matter of remembering.
    /// </para>
    /// <para>
    /// User-interface-free and unit tested, like <see cref="PartFFloorPlanOverlay"/>: no WPF, no brushes,
    /// no screen coordinates. Callers that measure text in pixels convert to plane units first.
    /// </para>
    /// </summary>
    public static class PartFTagPlacement
    {
        /// <summary>
        /// The drawing scale a Part F view lays its annotation out at unless told otherwise, as its
        /// denominator: 1:50, a normal scale for a dwelling plan, at which a rate tag is a little over a
        /// metre wide and fits inside a room.
        /// </summary>
        public const double DefaultAnnotationScale = 50;

        /// <summary>
        /// Device-independent pixels per inch, the unit WPF measures text in. Fixed, not read from the
        /// display: a layout that depended on the monitor would not be the same drawing on two machines.
        /// </summary>
        private const double dotsPerInch = 96;

        /// <summary>Metres per inch, for the sheet-to-building conversion.</summary>
        private const double metresPerInch = 0.0254;

        /// <summary>
        /// Clear space between a tag and its anchor, as a multiple of the tag's own height. The engine
        /// tries directly above the anchor first, so an uncrowded tag sits just above its mark by this much.
        /// </summary>
        private const double clearance_Factor = 0.5;

        /// <summary>
        /// How far a tag moves between attempts, as a multiple of its height. Small enough that a tag stays
        /// recognisably attached to its mark, large enough that consecutive attempts do not both fail on the
        /// same neighbour.
        /// </summary>
        private const double shift_Factor = 1.5;

        /// <summary>
        /// Attempts before the engine gives up on a tag. Twelve reaches about three and a half tag heights
        /// past the anchor at the default sizes, which is across a small room and no further: a tag pushed
        /// beyond that has stopped being obviously attached to anything, and its leader stops helping.
        /// </summary>
        private const double iterationCount = 12;

        /// <summary>
        /// Tag width or height [m] assumed where a caller supplies none, so a zero-sized tag cannot
        /// collapse the search to a single attempt at the anchor.
        /// </summary>
        private const double size_Default = 0.2;

        /// <summary>
        /// How many pixels of tag make one metre of building at the given annotation scale - the one
        /// conversion between a measured text size and the plane the tags are placed in.
        /// <para>
        /// <b>Nothing here may ever take the view transform instead.</b> That is the whole distinction
        /// between an annotation scale and a camera: a drawing's annotation layout is a property of the
        /// drawing, and if the viewport got a vote then panning and zooming would silently rearrange an
        /// engineering drawing's labels. A caller measures its text once, converts through this, and the
        /// layout it gets back is the same at every zoom.
        /// </para>
        /// <para>
        /// At 1:50 this is 96 / 0.0254 / 50 = 75.6 px/m, so a 110 px rate tag is 1.46 m of building.
        /// </para>
        /// </summary>
        /// <param name="annotationScale">The drawing scale's denominator: 50 for 1:50.</param>
        public static double PixelsPerMetre(double annotationScale)
        {
            double scale = annotationScale > 0 ? annotationScale : DefaultAnnotationScale;

            return dotsPerInch / metresPerInch / scale;
        }

        /// <summary>
        /// Which <see cref="PartFTagPriority"/> a mark's tag takes. The single place the policy is applied,
        /// so the drawing code never chooses a priority and the order can be asserted by tests.
        /// </summary>
        public static PartFTagPriority Priority(PartFOverlayMark partFOverlayMark)
        {
            if (partFOverlayMark is null)
            {
                return PartFTagPriority.Undefined;
            }

            //An unresolved transfer route keeps the transfer priority. It is the mark that says the
            //dwelling's air path is not established, so it must not be pushed behind an ordinary terminal
            //label - see PartFTagPriority.TransferAir.
            if (partFOverlayMark.IsTransfer)
            {
                return PartFTagPriority.TransferAir;
            }

            switch (partFOverlayMark.AirType)
            {
                case PartFAirflowAppearance.AirType.LocalKitchenExtract:
                    return PartFTagPriority.KitchenExtract;

                case PartFAirflowAppearance.AirType.GeneralExtract:
                    return PartFTagPriority.Extract;

                case PartFAirflowAppearance.AirType.Supply:
                    return PartFTagPriority.Supply;
            }

            //Outdoor and exhaust air are at the ventilation unit rather than in a room, so they carry no
            //room's Table 1.2 requirement and give way to every tag that does. A space's net airflow tag is
            //not built from a mark and sets its own priority.
            return PartFTagPriority.Diagnostic;
        }

        /// <summary>
        /// Which <see cref="PartFTagPriority"/> a Ventilation Design mark's tag takes, so its tags share the
        /// same placement policy - and so the same shared solve - as Part F's own, without ever outranking
        /// them. See <see cref="PartFTagPriority.DesignSupply"/>.
        /// </summary>
        public static PartFTagPriority Priority(DesignAirFlowMarkType designAirFlowMarkType)
        {
            return designAirFlowMarkType switch
            {
                DesignAirFlowMarkType.Supply => PartFTagPriority.DesignSupply,
                DesignAirFlowMarkType.Extract => PartFTagPriority.DesignExtract,
                DesignAirFlowMarkType.Net => PartFTagPriority.DesignNet,
                DesignAirFlowMarkType.Transfer => PartFTagPriority.DesignTransfer,
                _ => PartFTagPriority.DesignSupply,
            };
        }

        /// <summary>
        /// Which <see cref="PartFAnnotationType"/> key a Ventilation Design mark's tag is entered into the
        /// shared adapter under. See <see cref="PartFAnnotationType.DesignSupply"/>.
        /// </summary>
        public static PartFAnnotationType AnnotationType(DesignAirFlowMarkType designAirFlowMarkType)
        {
            return designAirFlowMarkType switch
            {
                DesignAirFlowMarkType.Supply => PartFAnnotationType.DesignSupply,
                DesignAirFlowMarkType.Extract => PartFAnnotationType.DesignExtract,
                DesignAirFlowMarkType.Net => PartFAnnotationType.DesignNet,
                DesignAirFlowMarkType.Transfer => PartFAnnotationType.DesignTransfer,
                _ => PartFAnnotationType.DesignSupply,
            };
        }

        /// <summary>
        /// How far [m] the half-plane <see cref="Lane"/> builds reaches beyond the row it is drawn about.
        /// Effectively unbounded at building scale - large enough that it is never itself the reason a
        /// candidate position is rejected - while staying a real, finite <see cref="Rectangle2D"/>.
        /// </summary>
        private const double laneHalfExtent_M = 1e6;

        /// <summary>
        /// Splits a tag's existing centre constraint into the Part F lane (above a shared reference row) or
        /// the Ventilation Design lane (below it), so a Part F tag and a Ventilation Design tag reporting on
        /// the same space or the same route can never land on the same side of the row and read as one
        /// interchangeable figure.
        /// <para>
        /// <b>Combined with the existing constraint, not substituted for it.</b> Where <paramref name="limitArea"/>
        /// is a room outline (a <see cref="Face2D"/>) this clips that outline to its own half by the row, so a
        /// terminal tag's centre still has to stay inside its own room - the harder rule, kept exactly as
        /// <see cref="PartFTagPlacementItem.LimitArea"/> already documents it - AND on its own side of the
        /// row. Where it is null - a transfer tag, which belongs to no single room - the half-plane alone is
        /// returned, so the lane still applies even though there is no outline to clip.
        /// </para>
        /// <para>
        /// A room the row cuts entirely off - the clip leaves fewer than three points - falls back to the
        /// UNCLIPPED outline rather than an empty region: the lane is a presentation preference, and a tag
        /// that cannot be placed at all is a regulatory or design figure lost from the drawing, which is the
        /// worse failure.
        /// </para>
        /// </summary>
        /// <param name="limitArea">The tag's existing centre constraint - a room outline, or null.</param>
        /// <param name="row">The reference row [m], in the view plane's own Y, the lane is drawn about.</param>
        /// <param name="above">True for the Part F lane (centre Y &#8805; row); false for the Design lane (centre Y &#8804; row).</param>
        public static IClosed2D Lane(IClosed2D limitArea, double row, bool above)
        {
            if (limitArea is not Face2D face2D)
            {
                return limitArea ?? HalfPlane(row, above);
            }

            List<Point2D> point2Ds = (face2D.ExternalEdge2D as ISegmentable2D)?.GetPoints();

            return (IClosed2D)Clip(point2Ds, row, above) ?? face2D;
        }

        private static Rectangle2D HalfPlane(double row, bool above)
        {
            double originY = above ? row : row - (2 * laneHalfExtent_M);

            return new Rectangle2D(new Point2D(-laneHalfExtent_M, originY), 2 * laneHalfExtent_M, 2 * laneHalfExtent_M);
        }

        /// <summary>
        /// A closed polyline's boundary clipped to one side of a horizontal row - Sutherland-Hodgman against
        /// the single edge Y = <paramref name="row"/> - or null where fewer than three points remain, i.e.
        /// the boundary does not reach that side at all.
        /// </summary>
        private static Polygon2D Clip(List<Point2D> point2Ds, double row, bool above)
        {
            if (point2Ds is null || point2Ds.Count < 3)
            {
                return null;
            }

            List<Point2D> result = [];

            int count = point2Ds.Count;

            for (int i = 0; i < count; i++)
            {
                Point2D current = point2Ds[i];
                Point2D previous = point2Ds[(i - 1 + count) % count];

                bool current_Inside = above ? current.Y >= row : current.Y <= row;
                bool previous_Inside = above ? previous.Y >= row : previous.Y <= row;

                if (current_Inside != previous_Inside)
                {
                    double t = (row - previous.Y) / (current.Y - previous.Y);

                    result.Add(new Point2D(previous.X + (t * (current.X - previous.X)), row));
                }

                if (current_Inside)
                {
                    result.Add(current);
                }
            }

            return result.Count < 3 ? null : new Polygon2D(result);
        }

        /// <summary>
        /// Places every tag, entering the manually positioned ones as obstacles rather than leaving them out.
        /// </summary>
        /// <param name="partFTagPlacementItems">
        /// Every tag to be drawn, automatic and manual alike. Their order is irrelevant - see the note on
        /// determinism.
        /// </param>
        /// <param name="partFAnnotationOverrides">
        /// Manual positions, matched to items by annotated guid and annotation type. An override matching no
        /// item is ignored and never pruned: the object may simply not be on this level or in this filter,
        /// and a position thrown away because a tag was momentarily absent is a person's work thrown away.
        /// </param>
        /// <param name="obstacle2Ds">
        /// Geometry already on the drawing that a tag must not cover - the space names the plan itself
        /// draws, most importantly. Model geometry, entered as obstacles for exactly the same reason manual
        /// tags are.
        /// </param>
        /// <returns>
        /// One result per item, in the deterministic order the tags were placed in. Never null, and no
        /// result carries a null rectangle.
        /// </returns>
        public static List<PartFTagPlacementResult> Solve(IEnumerable<PartFTagPlacementItem> partFTagPlacementItems, IEnumerable<PartFAnnotationOverride> partFAnnotationOverrides = null, IEnumerable<IClosed2D> obstacle2Ds = null)
        {
            List<PartFTagPlacementResult> result = [];

            List<PartFTagPlacementItem> items = Ordered(partFTagPlacementItems);
            if (items.Count == 0)
            {
                return result;
            }

            Dictionary<Tuple<Guid, PartFAnnotationType>, Point2D> dictionary_Override = Overrides(partFAnnotationOverrides);

            //Obstacles the caller gave, plus one for every manual tag. A manual tag is a rectangle on the
            //drawing that a person chose the position of, so leaving it out of the solve - which is all the
            //Mollier chart does with a moved label - lets an automatic tag be placed straight on top of it.
            List<IClosed2D> obstacles = [.. (obstacle2Ds ?? []).Where(x => x is not null)];

            List<PartFTagPlacementItem> items_Automatic = [];
            Dictionary<PartFTagPlacementItem, Rectangle2D> dictionary_Manual = [];

            foreach (PartFTagPlacementItem item in items)
            {
                Point2D point2D_Manual = Manual(item, dictionary_Override);

                if (point2D_Manual is null)
                {
                    items_Automatic.Add(item);
                    continue;
                }

                Rectangle2D rectangle2D = Rectangle2D(item, point2D_Manual);

                dictionary_Manual[item] = rectangle2D;
                obstacles.Add(rectangle2D);
            }

            Dictionary<PartFTagPlacementItem, Solver2DResult> dictionary_Solved = Solve(items_Automatic, obstacles);

            foreach (PartFTagPlacementItem item in items)
            {
                if (dictionary_Manual.TryGetValue(item, out Rectangle2D rectangle2D_Manual))
                {
                    //Not solved, and not the solver's to judge: Undefined says so rather than implying the
                    //engine approved it.
                    result.Add(new PartFTagPlacementResult(item, rectangle2D_Manual, Solver2DResultType.Undefined, true));
                    continue;
                }

                dictionary_Solved.TryGetValue(item, out Solver2DResult solver2DResult);

                Rectangle2D rectangle2D = solver2DResult?.Closed2D<Rectangle2D>();

                //An unplaced tag is drawn at its anchor rather than dropped, and says it was unplaced. A
                //rate that vanishes from a compliance drawing because the plan was crowded is worse than one
                //that overlaps: the reader can see an overlap.
                result.Add(new PartFTagPlacementResult(
                    item,
                    rectangle2D ?? Rectangle2D(item, item.Anchor2D),
                    solver2DResult is null ? Solver2DResultType.Unplaced : solver2DResult.ResultType,
                    false));
            }

            return result;
        }

        /// <summary>
        /// Runs the shared engine over the automatic tags. The only place in Part F that touches
        /// <see cref="Solver2D"/>.
        /// </summary>
        private static Dictionary<PartFTagPlacementItem, Solver2DResult> Solve(List<PartFTagPlacementItem> partFTagPlacementItems, List<IClosed2D> obstacle2Ds)
        {
            Dictionary<PartFTagPlacementItem, Solver2DResult> result = [];

            if (partFTagPlacementItems.Count == 0)
            {
                return result;
            }

            Solver2D solver2D = new(Area(partFTagPlacementItems, obstacle2Ds), obstacle2Ds);

            foreach (PartFTagPlacementItem item in partFTagPlacementItems)
            {
                double height = Height(item);

                Solver2DData solver2DData = new(Rectangle2D(item, item.Anchor2D), new Point2D(item.Anchor2D))
                {
                    Tag = item,
                    Priority = (int)item.Priority,
                    Solver2DSettings = new Solver2DSettings()
                    {
                        //The engine re-centres the tag on the anchor and then offsets it radially, trying
                        //straight up first, so half the height plus a clear gap is what puts an uncrowded
                        //tag immediately above its mark.
                        StartingDistance = height * (0.5 + clearance_Factor),
                        ShiftDistance = height * shift_Factor,
                        IterationCount = iterationCount,

                        //Centroid-inside, deliberately: the tag may overhang the room. See the property.
                        LimitArea = item.LimitArea,
                    },
                };

                solver2D.Add(solver2DData);
            }

            foreach (Solver2DResult solver2DResult in solver2D.Solve() ?? [])
            {
                if (solver2DResult?.Tag is PartFTagPlacementItem item)
                {
                    result[item] = solver2DResult;
                }
            }

            return result;
        }

        /// <summary>
        /// The order tags are placed in: priority, then annotation type, then the annotated object's guid,
        /// then the order supplied.
        /// <para>
        /// Every key is taken from the tag itself, so the layout does not depend on how the caller's
        /// collection happened to enumerate - a set built through a dictionary or a hash set still places
        /// identically. The supplied order is only the last resort, for two tags that are otherwise
        /// indistinguishable, and it keeps the comparison total so nothing rests on a sort being stable.
        /// </para>
        /// </summary>
        private static List<PartFTagPlacementItem> Ordered(IEnumerable<PartFTagPlacementItem> partFTagPlacementItems)
        {
            List<PartFTagPlacementItem> items = [.. (partFTagPlacementItems ?? []).Where(x => x is not null && x.Anchor2D is not null)];

            return [.. items
                .Select((x, i) => new { Item = x, Index = i })
                .OrderBy(x => (int)x.Item.Priority)
                .ThenBy(x => (int)x.Item.AnnotationType)
                .ThenBy(x => x.Item.ObjectGuid)
                .ThenBy(x => x.Index)
                .Select(x => x.Item)];
        }

        private static Dictionary<Tuple<Guid, PartFAnnotationType>, Point2D> Overrides(IEnumerable<PartFAnnotationOverride> partFAnnotationOverrides)
        {
            Dictionary<Tuple<Guid, PartFAnnotationType>, Point2D> result = [];

            foreach (PartFAnnotationOverride partFAnnotationOverride in partFAnnotationOverrides ?? [])
            {
                if (partFAnnotationOverride?.IsUserPositioned != true)
                {
                    continue;
                }

                result[Key(partFAnnotationOverride.ObjectGuid, partFAnnotationOverride.AnnotationType)] = partFAnnotationOverride.Position2D;
            }

            return result;
        }

        private static Point2D Manual(PartFTagPlacementItem partFTagPlacementItem, Dictionary<Tuple<Guid, PartFAnnotationType>, Point2D> dictionary_Override)
        {
            return dictionary_Override.TryGetValue(Key(partFTagPlacementItem.ObjectGuid, partFTagPlacementItem.AnnotationType), out Point2D result) ? result : null;
        }

        private static Tuple<Guid, PartFAnnotationType> Key(Guid guid, PartFAnnotationType partFAnnotationType)
        {
            return new Tuple<Guid, PartFAnnotationType>(guid, partFAnnotationType);
        }

        /// <summary>A tag's rectangle centred on a point, which is what both an anchor and a stored manual position are.</summary>
        private static Rectangle2D Rectangle2D(PartFTagPlacementItem partFTagPlacementItem, Point2D point2D)
        {
            double width = partFTagPlacementItem.Width > Core.Tolerance.MacroDistance ? partFTagPlacementItem.Width : size_Default;
            double height = Height(partFTagPlacementItem);

            return new Rectangle2D(new Point2D(point2D.X - (width / 2), point2D.Y - (height / 2)), width, height);
        }

        private static double Height(PartFTagPlacementItem partFTagPlacementItem)
        {
            return partFTagPlacementItem.Height > Core.Tolerance.MacroDistance ? partFTagPlacementItem.Height : size_Default;
        }

        /// <summary>
        /// The region a tag may be placed in: everything the tags and obstacles occupy, plus how far the
        /// engine can push a tag, plus a margin.
        /// <para>
        /// Generous on purpose. The engine requires a candidate to sit entirely inside this area, so an area
        /// drawn tight to the plan would reject the tags of the rooms on its edge - and the rooms on the
        /// edge of a plan are not less important than the ones in the middle. It is still bounded, and it is
        /// computed from the input, so it is deterministic.
        /// </para>
        /// </summary>
        private static Rectangle2D Area(List<PartFTagPlacementItem> partFTagPlacementItems, List<IClosed2D> obstacle2Ds)
        {
            List<Point2D> point2Ds = [];
            double margin = 0;

            foreach (PartFTagPlacementItem item in partFTagPlacementItems)
            {
                double height = Height(item);

                point2Ds.Add(item.Anchor2D);

                //Half the tag, plus everything the search can add to it.
                margin = System.Math.Max(margin, (System.Math.Max(item.Width, height) / 2) + (height * (0.5 + clearance_Factor)) + (height * shift_Factor * iterationCount));
            }

            foreach (IClosed2D obstacle2D in obstacle2Ds)
            {
                BoundingBox2D boundingBox2D = obstacle2D?.GetBoundingBox();
                if (boundingBox2D is null)
                {
                    continue;
                }

                point2Ds.Add(boundingBox2D.Min);
                point2Ds.Add(boundingBox2D.Max);
            }

            return new Rectangle2D(new BoundingBox2D(point2Ds, margin * 2));
        }
    }
}
