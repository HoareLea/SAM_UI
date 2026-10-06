// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Geometry.Object;
using SAM.Geometry.Planar;
using SAM.Geometry.Spatial;
using SAM.Geometry.UI.WPF;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Draws the Ventilation Design floor-plan overlay over a 2D floor plan: each space's aggregated
    /// design supply/extract/net airflow, read from <c>VentilationTerminal.DesignFlowRate_Lps</c> through
    /// <see cref="DesignAirFlowFloorPlanOverlay"/>.
    /// <para>
    /// <b>Separate from <see cref="PartFAirflowRenderer"/> on purpose.</b> That renderer draws the Part F
    /// regulatory requirement from <c>PartFComplianceResult</c>; this one draws the model's current DESIGN
    /// airflow. The two read different authorities and can disagree after an optimisation round - that is
    /// the point, not a bug, and is why they stay two renderers rather than one with a mode switch.
    /// </para>
    /// <para>
    /// <b>Positioned through the same shared adapter Part F uses</b> - <see cref="PartFTagPlacement"/>, and
    /// so the same <c>Solver2D</c> - rather than a second, hand-made collision solver. Every tag this
    /// renderer draws is treated as an obstacle by every other tag it solves alongside, so a studio's SUP,
    /// EXT and NET marks come apart from one another exactly as Part F's own tags do; and where a
    /// <see cref="PartFAirflowRenderer"/> is also drawing on the SAME plan, its already-solved tag
    /// rectangles are read (never written to - see <see cref="PartFAirflowRenderer.PlacedRectangle2Ds"/>)
    /// and entered as obstacles here too, so a design tag is never solved on top of a Part F one.
    /// </para>
    /// <para>
    /// <b>Deliberately one-directional.</b> Part F never reads this overlay, and its own layout never
    /// changes because this overlay was switched on, off, or moved - it is the regulatory figure and keeps
    /// exactly the position it would have on its own. This overlay is the one that gives way, which is why
    /// combining the two into a single solve (rather than this one-directional obstacle relationship) was
    /// not done: it would make Part F's own tags depend on whether Ventilation Design happened to be
    /// switched on, which nothing before this overlay existed ever did and nothing should start doing.
    /// </para>
    /// <para>
    /// <b>Load is the expensive call</b> - it sections every space on the plan and solves every visible tag
    /// - so it runs only when the model, the plan or the view's visibility settings change.
    /// <see cref="Draw"/> is cheap and is what every pan, zoom and resize calls; it never re-reads the model
    /// and never re-solves - see <see cref="Place"/>.
    /// </para>
    /// </summary>
    public class DesignAirFlowRenderer
    {
        /// <summary>
        /// "D ", drawn before every Ventilation Design tag - matching
        /// <see cref="PartFAirflowRenderer.AuthorityPrefix"/> - so "SUP 150.0 l/s" cannot be mistaken for
        /// Part F's own requirement just because the two sit right beside it, in the same space or on the
        /// same route. The lane (see <see cref="PartFTagPlacement.Lane"/>) and this overlay's own colours
        /// reinforce the distinction; neither may be the ONLY thing that makes it, and the prefix is what
        /// survives a black-and-white printout or a colour-blind reader.
        /// <para>
        /// Not "Calculated": a Part F requirement, a design airflow, an operating airflow and an equipment
        /// capacity may all be calculated values, so that word would say nothing about WHICH authority this
        /// tag reports.
        /// </para>
        /// </summary>
        internal const string AuthorityPrefix = "D ";

        private static readonly Color supplyColor = Color.FromRgb(0x1B, 0x6E, 0xC2);
        private static readonly Color extractColor = Color.FromRgb(0xC2, 0x5B, 0x1B);
        private static readonly Color netColor = Color.FromRgb(0x4A, 0x4A, 0x4A);

        /// <summary>
        /// The design transfer colour. Deliberately NOT Approved Document F's own transfer-air colour: the
        /// two are different figures on the same drawing, and giving them the same colour would invite a
        /// reader to take one for the other.
        /// </summary>
        private static readonly Color transferColor = Color.FromRgb(0x2E, 0x7D, 0x53);

        private readonly FloorPlan2DControl floorPlan2DControl;

        /// <summary>
        /// This renderer's OWN child of <see cref="FloorPlan2DControl.Overlay"/>. <see cref="Draw"/> clears
        /// and rebuilds only this container's children, never <c>Overlay.Children</c> itself - that surface
        /// is shared with <see cref="PartFAirflowRenderer"/> (and any future overlay), and clearing it wipes
        /// whatever another renderer drew. Displaying the Part F requirement and the current design airflow
        /// at once is a normal, intended combination - the comparison between them is one of the reasons
        /// this overlay exists - so it must never be able to blank the other.
        /// </summary>
        private readonly ContainerVisual ownVisual = new();

        private AdjacencyCluster adjacencyCluster;
        private DesignAirFlowFloorPlanOverlay overlay = DesignAirFlowFloorPlanOverlay.Build(null, null);
        private Dictionary<DesignAirFlowOverlayMark, PartFTagPlacementResult> placements = [];

        /// <summary>
        /// Geometry already on the drawing that a tag must not cover - the space and room text the plan
        /// itself draws. Kept across a camera-only reload exactly as <see cref="PartFAirflowRenderer"/>
        /// keeps its own - see <see cref="PartFAirflowRenderer.ResolveTextObstacles"/>.
        /// </summary>
        private List<IClosed2D> textObstacle2Ds = [];

        /// <summary>
        /// Part F's own currently-solved tag rectangles on this same plan, or empty where there is none.
        /// Read-only: nothing here is ever written back to a <see cref="PartFAirflowRenderer"/>.
        /// </summary>
        private List<IClosed2D> partFObstacle2Ds = [];

        /// <summary>
        /// The drawing scale this overlay's tags are sized and solved at - the SAME convention Part F uses,
        /// see <see cref="PartFTagPlacement.PixelsPerMetre"/>. Matches the Part F view's own annotation
        /// scale where a <see cref="PartFAirflowRenderer"/> is also drawing on this plan, so the two read as
        /// one drawing annotation family rather than two systems with different proportions; otherwise the
        /// shared default.
        /// </summary>
        private double annotationScale = PartFTagPlacement.DefaultAnnotationScale;

        /// <summary>
        /// Attaches to a 2D floor plan, adding this renderer's own container to its shared
        /// <see cref="FloorPlan2DControl.Overlay"/>. The control's own <c>ViewChanged</c> only ever
        /// triggers a redraw - see <see cref="Draw"/>.
        /// </summary>
        public DesignAirFlowRenderer(FloorPlan2DControl floorPlan2DControl)
        {
            this.floorPlan2DControl = floorPlan2DControl;

            if (this.floorPlan2DControl is not null)
            {
                this.floorPlan2DControl.ViewChanged += FloorPlan2DControl_ViewChanged;
                this.floorPlan2DControl.Overlay.Children.Add(ownVisual);
            }
        }

        /// <summary>
        /// How the overlay is presented. Never null; assigning replaces it and lays the tags out again,
        /// because visibility decides which marks enter the shared solve - a hidden mark must not claim
        /// space, or act as an obstacle, for the ones that are shown.
        /// </summary>
        public DesignAirFlowViewSettings ViewSettings
        {
            get
            {
                return designAirFlowViewSettings;
            }

            set
            {
                designAirFlowViewSettings = value ?? new DesignAirFlowViewSettings();

                Place();
                Draw();
            }
        }

        private DesignAirFlowViewSettings designAirFlowViewSettings = new();

        /// <summary>Every mark drawn, in a stable order.</summary>
        public List<DesignAirFlowOverlayMark> Marks
        {
            get { return overlay.Marks; }
        }

        /// <summary>What could not be placed on this plan, and why.</summary>
        public List<string> Unplaced
        {
            get { return overlay.Unplaced; }
        }

        /// <summary>Where one mark's tag ended up, or null where it is not currently placed.</summary>
        public PartFTagPlacementResult Placement(DesignAirFlowOverlayMark mark)
        {
            return mark is not null && placements.TryGetValue(mark, out PartFTagPlacementResult result) ? result : null;
        }

        /// <summary>
        /// Reads every space's design duty and works out where its marks go. The expensive call: it
        /// sections every space and solves every visible tag, so it is made when the model, the plan or the
        /// visibility settings change - not when the view moves.
        /// </summary>
        /// <param name="adjacencyCluster">The model the plan is drawn from.</param>
        /// <param name="geometryObjectModel">
        /// The geometry the plan was loaded from, read for the text it has already drawn - room names,
        /// door labels - so the tags can be kept off it. Optional; where none is supplied, the previous
        /// load's obstacles are kept, matching <see cref="PartFAirflowRenderer.ResolveTextObstacles"/>.
        /// </param>
        /// <param name="partFObstacle2Ds">
        /// A <see cref="PartFAirflowRenderer"/> drawing on the SAME plan's currently solved tag rectangles,
        /// so this overlay's tags are placed clear of Part F's. Null or empty where there is none, or where
        /// it is not enabled - read-only, and never fed back to Part F.
        /// </param>
        /// <param name="annotationScale">
        /// The drawing scale to size and solve this overlay's tags at - <see cref="PartFAirflowViewSettings.AnnotationScale"/>
        /// of the Part F view on this same plan, where there is one, so the two overlays' tags read as one
        /// family; otherwise <see cref="PartFTagPlacement.DefaultAnnotationScale"/>.
        /// </param>
        public void Load(AdjacencyCluster adjacencyCluster, GeometryObjectModel geometryObjectModel = null, IEnumerable<IClosed2D> partFObstacle2Ds = null, double annotationScale = PartFTagPlacement.DefaultAnnotationScale)
        {
            this.adjacencyCluster = adjacencyCluster;
            this.annotationScale = annotationScale > 0 ? annotationScale : PartFTagPlacement.DefaultAnnotationScale;
            this.partFObstacle2Ds = [.. (partFObstacle2Ds ?? []).Where(x => x is not null)];

            Plane plane = floorPlan2DControl?.Plane;

            //Same resilience rule as PartFAirflowRenderer.Load: a camera-only or attribute-only update
            //regenerates no geometry, so the previous load's text obstacles are kept rather than cleared.
            textObstacle2Ds = PartFAirflowRenderer.ResolveTextObstacles(geometryObjectModel, plane, textObstacle2Ds);

            overlay = DesignAirFlowFloorPlanOverlay.Build(adjacencyCluster, plane, ViewSettings.ShowNet, ViewSettings.ShowTransfer);

            Place();
            Draw();
        }

        /// <summary>
        /// Lays every visible tag out, by handing them all to the shared engine through
        /// <see cref="PartFTagPlacement"/> - the same adapter, and so the same <c>Solver2D</c>, Part F's own
        /// tags are placed through. No second placement algorithm exists for this overlay.
        /// </summary>
        public void Place()
        {
            placements = [];

            if (adjacencyCluster is null || floorPlan2DControl?.Plane is null || overlay.Marks.Count == 0)
            {
                return;
            }

            //The ANNOTATION scale, never the view transform - see PartFAirflowRenderer.Place for why.
            double scale = PartFTagPlacement.PixelsPerMetre(annotationScale);

            Dictionary<Guid, IClosed2D> dictionary_LimitArea = [];
            Dictionary<Guid, double> dictionary_Row = [];

            List<PartFTagPlacementItem> items = [];

            foreach (DesignAirFlowOverlayMark mark in overlay.Marks)
            {
                if (!Visible(mark))
                {
                    continue;
                }

                Size(mark, out double width_Px, out double height_Px);

                //A design tag's centre stays in its own room, matching Part F's own terminal tags. A
                //transfer tag belongs to the opening between two spaces and so to neither outline, and
                //gets none - the same rule PartFAirflowRenderer.Place applies to its own.
                IClosed2D limitArea = mark.IsTransfer ? null : LimitArea(dictionary_LimitArea, mark.SpaceGuid);

                items.Add(new PartFTagPlacementItem()
                {
                    ObjectGuid = mark.SpaceGuid,
                    AnnotationType = PartFTagPlacement.AnnotationType(mark.MarkType),
                    Priority = PartFTagPlacement.Priority(mark.MarkType),
                    Anchor2D = mark.IsTransfer ? mark.End : mark.Position,
                    Width = width_Px / scale,
                    Height = height_Px / scale,

                    //The Design lane: this tag's centre stays BELOW the space's shared reference row, so a
                    //PartFAirflowRenderer tag for the same space - kept above the same row - can never land
                    //in the same visual band even though the two anchor at the same point. See
                    //PartFTagPlacement.Lane. A transfer tag has no room outline to clip and keeps none.
                    LimitArea = limitArea is null ? null : PartFTagPlacement.Lane(limitArea, Row(dictionary_Row, mark.SpaceGuid, limitArea), above: false),

                    Tag = mark,
                });
            }

            //Part F's own tags are read-only obstacles here - see partFObstacle2Ds - never manual overrides
            //of this overlay's own, which does not have any.
            List<IClosed2D> obstacles = [.. textObstacle2Ds, .. partFObstacle2Ds];

            foreach (PartFTagPlacementResult result in PartFTagPlacement.Solve(items, null, obstacles))
            {
                if (result.Tag is DesignAirFlowOverlayMark mark)
                {
                    placements[mark] = result;
                }
            }
        }

        /// <summary>
        /// Redraws the overlay against the current view transform. Cheap and called often - on every pan,
        /// zoom, resize and visibility toggle. Nothing here re-reads the model, re-sections a space, or
        /// re-solves a placement.
        /// </summary>
        public void Draw()
        {
            if (floorPlan2DControl is null)
            {
                return;
            }

            //Only this renderer's own container, never Overlay itself - see ownVisual.
            ownVisual.Children.Clear();

            if (!ViewSettings.Enabled || overlay.Marks.Count == 0)
            {
                return;
            }

            System.Windows.Media.Matrix matrix = floorPlan2DControl.WorldToScreen;

            foreach (DesignAirFlowOverlayMark mark in overlay.Marks)
            {
                if (!Visible(mark))
                {
                    continue;
                }

                DrawingVisual drawingVisual = new();

                using (DrawingContext drawingContext = drawingVisual.RenderOpen())
                {
                    //A transfer mark stands for something with real geometry on the plan, so its route is
                    //drawn as well as its tag. A terminal mark has only a tag - see DrawTag.
                    if (mark.IsTransfer)
                    {
                        DrawTransfer(drawingContext, mark, matrix);
                    }

                    //The layout was solved in the plane, on a change of input; this only transforms it.
                    DrawTag(drawingContext, mark, matrix, Placement(mark));
                }

                ownVisual.Children.Add(drawingVisual);
            }
        }

        /// <summary>Clears everything drawn and forgets the model.</summary>
        public void Clear()
        {
            adjacencyCluster = null;
            overlay = DesignAirFlowFloorPlanOverlay.Build(null, null);
            placements = [];
            textObstacle2Ds = [];
            partFObstacle2Ds = [];

            Draw();
        }

        /// <summary>
        /// Stops listening to the control and removes this renderer's own container from its Overlay.
        /// Call when the view it draws on goes away.
        /// </summary>
        public void Detach()
        {
            if (floorPlan2DControl is not null)
            {
                floorPlan2DControl.ViewChanged -= FloorPlan2DControl_ViewChanged;
                floorPlan2DControl.Overlay.Children.Remove(ownVisual);
            }
        }

        /// <summary>
        /// The camera moved - a pan, a zoom, a resize. <b>Redraw only</b>, matching
        /// <c>PartFAirflowRenderer.FloorPlan2DControl_ViewChanged</c>: the tags are placed for the
        /// view's annotation scale in the plane's own coordinates, so moving the camera cannot change where
        /// any of them belongs - only where that is on screen.
        /// </summary>
        private void FloorPlan2DControl_ViewChanged(object sender, EventArgs e)
        {
            Draw();
        }

        private bool Visible(DesignAirFlowOverlayMark mark)
        {
            return mark.MarkType switch
            {
                DesignAirFlowMarkType.Supply => ViewSettings.ShowSupply,
                DesignAirFlowMarkType.Extract => ViewSettings.ShowExtract,
                DesignAirFlowMarkType.Net => ViewSettings.ShowNet,
                DesignAirFlowMarkType.Transfer => ViewSettings.ShowTransfer,
                _ => true,
            };
        }

        /// <summary>
        /// The route geometry of a transfer mark: an arrow across the modelled door it crosses, or a
        /// dashed cross on the partition where the model establishes no single opening at all.
        /// <para>
        /// The same visual language <c>PartFAirflowRenderer.DrawTransfer</c> uses, in this overlay's own
        /// colour. A reader who has learnt what a dashed cross means on the Approved Document F overlay
        /// must not have to learn a second meaning for it here.
        /// </para>
        /// </summary>
        private static void DrawTransfer(DrawingContext drawingContext, DesignAirFlowOverlayMark mark, System.Windows.Media.Matrix matrix)
        {
            System.Windows.Point point_Start = matrix.Transform(new System.Windows.Point(mark.Start.X, mark.Start.Y));
            System.Windows.Point point_End = matrix.Transform(new System.Windows.Point(mark.End.X, mark.End.Y));

            Pen pen = RoutePen(mark.IsUnresolved);

            //No span: the model shows no single opening on this route, so there is nothing to draw an arrow
            //along and no direction that could honestly be pointed. A small dashed cross marks where the
            //air would have to cross, and the tag carries the rest. A long room-to-room arrow here would be
            //the visual claim that the design air has a way through, which is exactly the claim this route
            //cannot make.
            if (point_Start == point_End)
            {
                DrawWarningMarker(drawingContext, pen, point_End);
                return;
            }

            drawingContext.DrawLine(pen, point_Start, point_End);

            DrawHead(drawingContext, pen, point_Start, point_End);
        }

        /// <summary>A small dashed cross marking a route that has nowhere established to pass through.</summary>
        private static void DrawWarningMarker(DrawingContext drawingContext, Pen pen, System.Windows.Point point)
        {
            const double size = 6;

            drawingContext.DrawLine(pen, new System.Windows.Point(point.X - size, point.Y - size), new System.Windows.Point(point.X + size, point.Y + size));
            drawingContext.DrawLine(pen, new System.Windows.Point(point.X - size, point.Y + size), new System.Windows.Point(point.X + size, point.Y - size));
        }

        /// <summary>
        /// The arrowhead, built in SCREEN space so it stays the same size at every zoom - matching
        /// <c>PartFAirflowRenderer.DrawHead</c>. A head scaled with the building is a dot on a site plan
        /// and a wedge across a room.
        /// </summary>
        private static void DrawHead(DrawingContext drawingContext, Pen pen, System.Windows.Point point_Start, System.Windows.Point point_End)
        {
            Vector vector = point_End - point_Start;
            if (vector.Length <= 0)
            {
                return;
            }

            vector.Normalize();

            Vector vector_Normal = new(-vector.Y, vector.X);

            System.Windows.Point point_1 = point_End - (vector * PartFAirflowRenderer.arrowHead_Px) + (vector_Normal * (PartFAirflowRenderer.arrowHead_Px / 2.5));
            System.Windows.Point point_2 = point_End - (vector * PartFAirflowRenderer.arrowHead_Px) - (vector_Normal * (PartFAirflowRenderer.arrowHead_Px / 2.5));

            StreamGeometry streamGeometry = new();
            using (StreamGeometryContext streamGeometryContext = streamGeometry.Open())
            {
                streamGeometryContext.BeginFigure(point_End, true, true);
                streamGeometryContext.LineTo(point_1, true, false);
                streamGeometryContext.LineTo(point_2, true, false);
            }

            streamGeometry.Freeze();

            drawingContext.DrawGeometry(pen.Brush, null, streamGeometry);
        }

        /// <summary>
        /// Draws one tag where the placement engine put it, at the same padding, border and text size
        /// convention Part F's own tags use - see <see cref="PartFAirflowRenderer.tagPadding_Px"/> - so the
        /// two overlays read as one drawing annotation family.
        /// <para>
        /// <b>A leader only where the mark stands at a real coordinate.</b> A terminal mark's
        /// <see cref="DesignAirFlowOverlayMark.Position"/> is a synthetic room-level point, exactly as a
        /// Part F terminal's anchor is - see <c>PartFAirflowRenderer.HasPhysicalAnchor</c> - so no leader is
        /// drawn back to it: a leader from one synthetic point to another would assert a precision neither
        /// position has. A transfer mark is the opposite case: it sits on a modelled door or a modelled
        /// partition, both of which the plan draws, so it gets the leader for the same reason Approved
        /// Document F's own transfer marks do.
        /// </para>
        /// </summary>
        private void DrawTag(DrawingContext drawingContext, DesignAirFlowOverlayMark mark, System.Windows.Media.Matrix matrix, PartFTagPlacementResult partFTagPlacementResult)
        {
            Color color = mark.MarkType switch
            {
                DesignAirFlowMarkType.Supply => supplyColor,
                DesignAirFlowMarkType.Extract => extractColor,
                DesignAirFlowMarkType.Transfer => transferColor,
                _ => netColor,
            };

            Brush brush = new SolidColorBrush(color);
            brush.Freeze();

            //The tag was laid out for the annotation scale, so it is drawn at the size that scale implies at
            //the current zoom - see PartFAirflowRenderer.Factor.
            double factor = Factor(matrix);

            FormattedText formattedText = PartFAirflowRenderer.Text(Label(mark), brush, PartFAirflowRenderer.labelSize_Px * factor, true);

            FormattedText formattedText_Caption = string.IsNullOrWhiteSpace(mark.Caption)
                ? null
                : PartFAirflowRenderer.Text(mark.Caption, brush, PartFAirflowRenderer.captionSize_Px * factor, false);

            double width = System.Math.Max(formattedText.Width, formattedText_Caption?.Width ?? 0);
            double height = formattedText.Height + (formattedText_Caption?.Height ?? 0);

            System.Windows.Point point_Anchor = matrix.Transform(new System.Windows.Point(mark.Position.X, mark.Position.Y));

            //Nothing was placed for this mark - it became visible between the last layout and this repaint.
            //Drawn beside the anchor rather than dropped, matching PartFAirflowRenderer.DrawLabel.
            System.Windows.Point point_Text = PartFAirflowRenderer.Screen(partFTagPlacementResult?.Rectangle2D, matrix) is Rect rect
                ? rect.TopLeft
                : new System.Windows.Point(point_Anchor.X + 6, point_Anchor.Y - (height / 2));

            //Built from the engineering anchor and the solved rectangle, in the view layer - the shared
            //solver knows nothing about annotation and must not start to.
            if (mark.IsTransfer && partFTagPlacementResult?.Leader2D() is Segment2D segment2D)
            {
                drawingContext.DrawLine(
                    PartFAirflowRenderer.LeaderPen(brush),
                    matrix.Transform(new System.Windows.Point(segment2D[0].X, segment2D[0].Y)),
                    matrix.Transform(new System.Windows.Point(segment2D[1].X, segment2D[1].Y)));
            }

            Rect rect_Tag = new(
                point_Text.X - (PartFAirflowRenderer.tagPadding_Px * factor),
                point_Text.Y - (PartFAirflowRenderer.tagPadding_Px * factor / 2),
                width + (PartFAirflowRenderer.tagPadding_Px * factor * 2),
                height + (PartFAirflowRenderer.tagPadding_Px * factor));

            drawingContext.DrawRectangle(PartFAirflowRenderer.plateBrush, PartFAirflowRenderer.TagPen(factor), rect_Tag);

            drawingContext.DrawText(formattedText, point_Text);

            if (formattedText_Caption is not null)
            {
                drawingContext.DrawText(formattedText_Caption, new System.Windows.Point(point_Text.X, point_Text.Y + formattedText.Height));
            }
        }

        /// <summary>
        /// The route line. Dashed where the model establishes no single opening, on the same rule
        /// <c>PartFAirflowRenderer.Pen</c> applies: an unestablished route never looks more certain than an
        /// established one.
        /// </summary>
        private static Pen RoutePen(bool unresolved)
        {
            SolidColorBrush brush = new(transferColor);
            brush.Freeze();

            Pen result = new(brush, 1.4);

            if (unresolved)
            {
                result.DashStyle = new DashStyle([2, 2], 0);
            }

            result.Freeze();

            return result;
        }

        /// <summary>The tag's drawn text, with the explicit "D " identifier - see <see cref="AuthorityPrefix"/>.</summary>
        private static string Label(DesignAirFlowOverlayMark mark)
        {
            return string.Concat(AuthorityPrefix, mark.Label);
        }

        /// <summary>
        /// A tag's measured size in SCREEN pixels at the annotation scale, which is what the placement
        /// converts into plane units - matching <c>PartFAirflowRenderer.Size</c>. Measured exactly as it is
        /// drawn, caption included, so the box the engine reserves is the box the text fills.
        /// </summary>
        private static void Size(DesignAirFlowOverlayMark mark, out double width, out double height)
        {
            FormattedText formattedText = PartFAirflowRenderer.Text(Label(mark), Brushes.Black, PartFAirflowRenderer.labelSize_Px, true);

            FormattedText formattedText_Caption = string.IsNullOrWhiteSpace(mark.Caption)
                ? null
                : PartFAirflowRenderer.Text(mark.Caption, Brushes.Black, PartFAirflowRenderer.captionSize_Px, false);

            width = System.Math.Max(formattedText.Width, formattedText_Caption?.Width ?? 0);
            height = formattedText.Height + (formattedText_Caption?.Height ?? 0);
        }

        /// <summary>
        /// The space's own section outline on this plan, cached per space for the length of one layout -
        /// matching <c>PartFAirflowRenderer.LimitArea</c>.
        /// <para>
        /// The space itself is found through <see cref="AdjacencyCluster.GetObject{T}(Guid)"/> - a
        /// dictionary lookup keyed on the object's own type and guid, not a scan of every space in the
        /// model. <c>GetSpaces()?.Find(...)</c> would make this method, and so <see cref="Place"/>, one
        /// linear scan of the WHOLE space list per space with a visible mark; on a large model that is
        /// quadratic in the number of spaces, and the per-layout cache above only removes the repeat
        /// lookups for a second or third mark in the SAME space, not the first lookup of each new one.
        /// </para>
        /// </summary>
        private IClosed2D LimitArea(Dictionary<Guid, IClosed2D> dictionary_LimitArea, Guid guid_Space)
        {
            if (dictionary_LimitArea.TryGetValue(guid_Space, out IClosed2D result))
            {
                return result;
            }

            Space space = adjacencyCluster.GetObject<Space>(guid_Space);

            result = space is null
                ? null
                : adjacencyCluster.SpaceSectionFace2Ds(space, floorPlan2DControl.Plane)?.Where(x => x is not null).OrderByDescending(x => x.GetArea()).FirstOrDefault();

            dictionary_LimitArea[guid_Space] = result;

            return result;
        }

        /// <summary>
        /// The shared row [m] this space's Part F and Ventilation Design tags are split about - see
        /// <see cref="PartFTagPlacement.Lane"/> - cached per space for the length of one layout, matching
        /// <c>PartFAirflowRenderer.Row</c>. The room outline's own internal point: the same point either
        /// overlay's builder anchors an un-fanned terminal mark at, so the two agree on where the row is
        /// without either reading the other's marks.
        /// </summary>
        private static double Row(Dictionary<Guid, double> dictionary_Row, Guid guid_Space, IClosed2D limitArea)
        {
            if (dictionary_Row.TryGetValue(guid_Space, out double result))
            {
                return result;
            }

            result = limitArea?.GetInternalPoint2D()?.Y ?? 0;

            dictionary_Row[guid_Space] = result;

            return result;
        }

        /// <summary>
        /// How much bigger the drawing is than the sheet - matching <c>PartFAirflowRenderer.Factor</c>.
        /// </summary>
        private double Factor(System.Windows.Media.Matrix matrix)
        {
            return System.Math.Max(System.Math.Abs(matrix.M11), System.Math.Abs(matrix.M22)) / PartFTagPlacement.PixelsPerMetre(annotationScale);
        }
    }
}
