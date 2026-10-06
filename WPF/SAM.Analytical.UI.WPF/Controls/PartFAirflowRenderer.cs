// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using SAM.Core.UI.WPF;
using SAM.Geometry.Object;
using SAM.Geometry.Object.Spatial;
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
    /// Draws the Approved Document F airflow annotation over a 2D floor plan.
    /// <para>
    /// <b>The only Part F renderer.</b> It serves the assessment window's airflow tab and the normal saved
    /// 2D Section/Floor Plan view, and it must stay that way: the assessment window is for checking and
    /// debugging the assessment, the saved view is the drawing an engineer issues, and two renderers would
    /// mean the thing being checked was not the thing being issued. Everything it draws is positioned by the
    /// shared <see cref="PartFTagPlacement"/>, which is positioned by the shared
    /// <c>SAM.Geometry.Planar.Solver2D</c>, so there is no placement algorithm here either.
    /// </para>
    /// <para>
    /// <b>Driven entirely by <see cref="PartFAirflowViewSettings"/></b> - what is visible, at which operating
    /// condition, at what annotation scale, with which labels moved by hand. That is what makes one renderer
    /// possible: the window turns its checkboxes into those settings, a saved view stores them, and neither
    /// has any drawing rules of its own.
    /// </para>
    /// <para>
    /// <b>It computes no regulatory value.</b> Every rate, status and diagnostic is read from the calculated
    /// <c>PartFComplianceResult</c> through <see cref="PartFFloorPlanOverlay"/>. The numeric value on a tag is
    /// the CALCULATED design value; the ✓ or ? beside it is the provision or compliance status, and the two
    /// are deliberately separate - a rate SAM has allocated is not evidence that anything was installed.
    /// </para>
    /// <para>
    /// One <see cref="PartFFloorPlanOverlay"/> per dwelling, never one for the floor. A whole-floor overlay
    /// could route transfer air between two flats, which is not a thing that happens.
    /// </para>
    /// </summary>
    public class PartFAirflowRenderer
    {
        /// <summary>Screen length [px] of an arrowhead, held constant so it stays legible at every zoom.</summary>
        internal const double arrowHead_Px = 9;

        /// <summary>
        /// Padding [px] inside a tag, at the annotation scale. Compact: a tag is a label on a drawing, not a
        /// panel, and it has to sit inside a room without covering it.
        /// <para>
        /// <b>Internal, not private.</b> <see cref="DesignAirFlowRenderer"/> draws its own tags at this same
        /// padding, so the two overlays read as one drawing annotation family rather than two systems with
        /// different proportions - see the sizing convention shared through <see cref="TagPen"/>,
        /// <see cref="Text"/> and <see cref="Screen"/> below.
        /// </para>
        /// </summary>
        internal const double tagPadding_Px = 3;

        /// <summary>
        /// Tag text size [px] <b>on the sheet</b> - the size it is measured at, and the size it draws at when
        /// the view is at the annotation scale. Shared with <see cref="DesignAirFlowRenderer"/>; see
        /// <see cref="tagPadding_Px"/>.
        /// </summary>
        internal const double labelSize_Px = 11.5;

        /// <summary>Caption text size [px] on the sheet, smaller so a caption reads as a qualifier.</summary>
        internal const double captionSize_Px = 9.5;

        /// <summary>
        /// The tag's white background, and its border. See <see cref="Plate"/>. Internal, not private, so
        /// <see cref="DesignAirFlowRenderer"/> can paint its own tags identically - see
        /// <see cref="tagPadding_Px"/>.
        /// </summary>
        internal static readonly Brush plateBrush = Plate();

        internal static readonly Brush tagBorderBrush = TagBorder();

        private static readonly Brush veilBrush = Veil();

        private readonly FloorPlan2DControl floorPlan2DControl;

        /// <summary>
        /// This renderer's OWN child of <see cref="FloorPlan2DControl.Overlay"/>. <see cref="Draw"/> clears
        /// and rebuilds only this container's children, never <c>Overlay.Children</c> itself - that surface
        /// is shared with other overlays (<see cref="DesignAirFlowRenderer"/> among them), and clearing it
        /// wipes whatever another renderer drew. Showing the Part F requirement alongside the current
        /// design airflow is a normal, intended combination, so this renderer must never be able to blank
        /// another one.
        /// </summary>
        private readonly ContainerVisual ownVisual = new();

        private AdjacencyCluster adjacencyCluster;
        private List<PartFComplianceResult> partFComplianceResults = [];
        private List<PartFFloorPlanOverlay> overlays = [];
        private List<IClosed2D> textObstacle2Ds = [];
        private Dictionary<PartFOverlayMark, PartFTagPlacementResult> placements = [];

        /// <summary>
        /// Attaches to a 2D floor plan, adding this renderer's own container to its shared
        /// <see cref="FloorPlan2DControl.Overlay"/>. The control's own <c>ViewChanged</c> only ever
        /// triggers a redraw - see <see cref="Draw"/>.
        /// </summary>
        public PartFAirflowRenderer(FloorPlan2DControl floorPlan2DControl)
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
        /// because visibility and the annotation scale both change what has to fit on the plan.
        /// </summary>
        public PartFAirflowViewSettings ViewSettings
        {
            get
            {
                return partFAirflowViewSettings;
            }

            set
            {
                partFAirflowViewSettings = value ?? new PartFAirflowViewSettings();

                Place();
                Draw();
            }
        }

        private PartFAirflowViewSettings partFAirflowViewSettings = new();

        /// <summary>Every mark drawn, across every dwelling, in a stable order.</summary>
        public List<PartFOverlayMark> Marks
        {
            get { return [.. overlays.SelectMany(x => x.Marks)]; }
        }

        /// <summary>What could not be placed on this plan, and why, across every dwelling.</summary>
        public List<string> Unplaced
        {
            get { return [.. overlays.SelectMany(x => x.Unplaced)]; }
        }

        /// <summary>Where one mark's tag ended up, or null where it is not currently placed.</summary>
        public PartFTagPlacementResult Placement(PartFOverlayMark mark)
        {
            return mark is not null && placements.TryGetValue(mark, out PartFTagPlacementResult result) ? result : null;
        }

        /// <summary>
        /// Every tag rectangle this renderer currently has solved, in the view plane's own coordinates - so
        /// another overlay on the SAME plan can keep its own tags clear of Part F's without the two
        /// renderers merging into one and without this renderer needing to know the other overlay exists.
        /// <see cref="DesignAirFlowRenderer"/> reads this as an obstacle list; nothing here is written back
        /// to, or read from, that overlay.
        /// <para>
        /// Read-only and a snapshot: it reflects whatever this renderer last solved in <see cref="Place"/>,
        /// and calling it triggers no placement of its own.
        /// </para>
        /// </summary>
        public List<Rectangle2D> PlacedRectangle2Ds()
        {
            return [.. placements.Values.Select(x => x?.Rectangle2D).Where(x => x is not null)];
        }

        /// <summary>
        /// Reads the dwellings to draw and works out where every mark goes. The expensive call: it sections
        /// each space and each separating panel, so it is made when the model, the plan or the dwelling
        /// selection changes - not when the view moves and not when the operating condition changes.
        /// </summary>
        /// <param name="adjacencyCluster">The model the plan is drawn from.</param>
        /// <param name="partFComplianceResults">
        /// The calculated assessment of each dwelling to draw. One overlay is built per result, so transfer
        /// air can never be drawn between two dwellings.
        /// </param>
        /// <param name="geometryObjectModel">
        /// The geometry the plan was loaded from, read for the text it has already drawn - room names, door
        /// labels - so the tags can be kept off it. Optional.
        /// </param>
        public void Load(AdjacencyCluster adjacencyCluster, IEnumerable<PartFComplianceResult> partFComplianceResults, GeometryObjectModel geometryObjectModel = null)
        {
            this.adjacencyCluster = adjacencyCluster;
            this.partFComplianceResults = [.. (partFComplianceResults ?? []).Where(x => x is not null)];

            overlays = [];
            placements = [];

            Plane plane = floorPlan2DControl?.Plane;

            if (adjacencyCluster is null || plane is null)
            {
                textObstacle2Ds = [];
                Draw();
                return;
            }

            //A camera-only or attribute-only update regenerates no geometry, and the plan this load draws
            //over still carries the text of the previous one - so where no replacement geometry was
            //supplied, the previous load's text obstacles are kept rather than cleared, or the tags would
            //be re-placed on top of the room names the plan is still showing.
            textObstacle2Ds = ResolveTextObstacles(geometryObjectModel, plane, textObstacle2Ds);

            foreach (PartFComplianceResult partFComplianceResult in Filtered())
            {
                overlays.Add(PartFFloorPlanOverlay.Build(adjacencyCluster, partFComplianceResult, plane, ViewSettings.OperatingMode));
            }

            Place();
            Draw();
        }

        /// <summary>
        /// Re-reads every rate at the current operating condition and redraws, leaving every ANCHOR where it
        /// was. Switching between continuous, high, setback and measured changes what the system is doing,
        /// not where the rooms are; the tags are laid out again only because the text changes width.
        /// </summary>
        public void Refresh()
        {
            for (int i = 0; i < overlays.Count && i < partFComplianceResults.Count; i++)
            {
                overlays[i].Refresh(partFComplianceResults[i], ViewSettings.OperatingMode);
            }

            Place();
            Draw();
        }

        /// <summary>
        /// Lays every visible tag out, by handing them all to the shared engine through
        /// <see cref="PartFTagPlacement"/>.
        /// <para>
        /// Placed ACROSS dwellings in one solve, deliberately: two flats' tags can be next to each other on
        /// the same sheet even though their air cannot mix, and one solve is what keeps them from colliding.
        /// The assessment they are read from is still strictly per dwelling.
        /// </para>
        /// </summary>
        public void Place()
        {
            placements = [];

            if (adjacencyCluster is null || floorPlan2DControl?.Plane is null || overlays.Count == 0)
            {
                return;
            }

            //The ANNOTATION scale, never the view transform. A tag is a fixed size on the sheet, so
            //something has to say how much building it covers - and if that were the current zoom then
            //panning and zooming would rearrange an engineering drawing's annotation, which is an
            //auto-arrange command nobody issued. See PartFAirflowViewSettings.AnnotationScale.
            double scale = PartFTagPlacement.PixelsPerMetre(ViewSettings.AnnotationScale);

            Dictionary<Guid, IClosed2D> dictionary_LimitArea = [];
            Dictionary<Guid, double> dictionary_Row = [];

            List<PartFTagPlacementItem> partFTagPlacementItems = [];

            foreach (PartFOverlayMark mark in Marks)
            {
                if (!Visible(mark))
                {
                    continue;
                }

                Size(mark, out double width_Px, out double height_Px);

                //A terminal tag's centre stays in its own room, so it cannot end up reading as the room
                //next door's. A transfer tag belongs to the opening between two spaces and so to neither
                //outline, and gets none.
                IClosed2D limitArea = mark.IsTransfer ? null : LimitArea(dictionary_LimitArea, mark.SpaceGuid);

                partFTagPlacementItems.Add(new PartFTagPlacementItem()
                {
                    ObjectGuid = mark.AnnotationGuid,
                    AnnotationType = mark.AnnotationType,
                    Priority = PartFTagPlacement.Priority(mark),
                    Anchor2D = mark.End,

                    //Measured in pixels and solved in metres, at the annotation scale: a layout held in
                    //pixels would move when the window was resized, and one held at the viewport's scale
                    //would move when somebody zoomed.
                    Width = width_Px / scale,
                    Height = height_Px / scale,

                    //The Part F lane: this tag's centre stays ABOVE the space's shared reference row, so a
                    //DesignAirFlowRenderer tag for the same space - kept below the same row - can never land
                    //in the same visual band even though the two anchor at the same point. See
                    //PartFTagPlacement.Lane. A transfer tag has no room outline to clip and keeps none.
                    LimitArea = limitArea is null ? null : PartFTagPlacement.Lane(limitArea, Row(dictionary_Row, mark.SpaceGuid, limitArea), above: true),

                    Tag = mark,
                });
            }

            foreach (PartFTagPlacementResult partFTagPlacementResult in PartFTagPlacement.Solve(partFTagPlacementItems, ViewSettings.AnnotationOverrides, textObstacle2Ds))
            {
                if (partFTagPlacementResult.Tag is PartFOverlayMark mark)
                {
                    placements[mark] = partFTagPlacementResult;
                }
            }
        }

        /// <summary>
        /// Redraws the overlay against the current view transform. Cheap and called often - on every pan,
        /// zoom, resize and toggle. Nothing here decides where anything goes.
        /// </summary>
        public void Draw()
        {
            if (floorPlan2DControl is null)
            {
                return;
            }

            //Only this renderer's own container, never Overlay itself - see ownVisual.
            ownVisual.Children.Clear();

            if (!ViewSettings.Enabled || overlays.Count == 0)
            {
                return;
            }

            System.Windows.Media.Matrix matrix = floorPlan2DControl.WorldToScreen;

            DrawContextVeil(matrix);

            foreach (PartFOverlayMark mark in Marks)
            {
                if (!Visible(mark))
                {
                    continue;
                }

                DrawingVisual drawingVisual = new();

                using (DrawingContext drawingContext = drawingVisual.RenderOpen())
                {
                    //The layout was solved in the plane, on a change of input; this only transforms it.
                    Draw(drawingContext, mark, matrix, Placement(mark));
                }

                ownVisual.Children.Add(drawingVisual);
            }
        }

        /// <summary>Clears everything drawn and forgets the dwellings.</summary>
        public void Clear()
        {
            adjacencyCluster = null;
            partFComplianceResults = [];
            overlays = [];
            placements = [];
            textObstacle2Ds = [];

            Draw();
        }

        /// <summary>Stops listening to the control. Call when the view it draws on goes away.</summary>
        public void Detach()
        {
            if (floorPlan2DControl is not null)
            {
                floorPlan2DControl.ViewChanged -= FloorPlan2DControl_ViewChanged;
                floorPlan2DControl.Overlay.Children.Remove(ownVisual);
            }
        }

        /// <summary>
        /// The camera moved - a pan, a zoom, a resize. <b>Redraw only.</b>
        /// <para>
        /// Deliberately does not lay anything out, and must not start to. The tags are placed for the view's
        /// annotation scale in the plane's own coordinates, so moving the camera cannot change where any of
        /// them belongs - only where that is on screen. Laying them out here would turn ordinary navigation
        /// into an implicit auto-arrange, and would crawl: this fires on every mouse move of a drag.
        /// </para>
        /// </summary>
        private void FloorPlan2DControl_ViewChanged(object sender, EventArgs e)
        {
            Draw();
        }

        // ------------------------------------------------------------------
        // What is drawn
        // ------------------------------------------------------------------

        /// <summary>
        /// Draws one mark. What gets drawn depends on whether there is a real coordinate behind it.
        /// <para>
        /// <b>A terminal is a tag and nothing else.</b> It used to carry a short directional stub and an
        /// arrowhead, and both were claims the assessment cannot support: SAM does not know where the grille
        /// is, only that the room requires one, so the mark sits at a synthetic room-level point and the
        /// arrow implied a direction of air movement at a location nobody had established.
        /// </para>
        /// <para>
        /// <b>A transfer route keeps its geometry</b>, because it has real geometry to keep: it crosses a
        /// modelled door or a modelled partition, both of which are on the plan. A route the model gives no
        /// opening for gets no span and no arrowhead - a small dashed cross where the air would have to
        /// cross, and a tag saying so.
        /// </para>
        /// </summary>
        private void Draw(DrawingContext drawingContext, PartFOverlayMark mark, System.Windows.Media.Matrix matrix, PartFTagPlacementResult partFTagPlacementResult)
        {
            PartFAirflowAppearance appearance = PartFAirflowAppearance.Get(mark.AirType);

            if (mark.IsTransfer)
            {
                DrawTransfer(drawingContext, mark, appearance, matrix);
            }

            //Always drawn. The tag IS the terminal graphic now that the arrow is gone, so gating it on the
            //value settings would make a terminal disappear from the plan entirely. ShowValues chooses
            //between "SUP 63.0 l/s" and "SUP".
            DrawLabel(drawingContext, mark, appearance, matrix, partFTagPlacementResult);
        }

        /// <summary>
        /// The route geometry of a transfer mark: an arrow across the opening it crosses, or a dashed cross
        /// on the partition where the model establishes no opening at all.
        /// </summary>
        private static void DrawTransfer(DrawingContext drawingContext, PartFOverlayMark mark, PartFAirflowAppearance appearance, System.Windows.Media.Matrix matrix)
        {
            System.Windows.Point point_Start = matrix.Transform(new System.Windows.Point(mark.Start.X, mark.Start.Y));
            System.Windows.Point point_End = matrix.Transform(new System.Windows.Point(mark.End.X, mark.End.Y));

            Pen pen = Pen(appearance, mark.IsUnresolved);

            //No span: the route has no established opening, so there is nothing to draw an arrow along and
            //no direction that could honestly be pointed. A small dashed cross marks where the air would
            //have to cross, and the tag carries the rest. A long room-to-room arrow here would be the visual
            //claim that the air has a way through, which is exactly the claim this route cannot make.
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

        private static void DrawHead(DrawingContext drawingContext, Pen pen, System.Windows.Point point_Start, System.Windows.Point point_End)
        {
            Vector vector = point_End - point_Start;
            if (vector.Length <= 0)
            {
                return;
            }

            vector.Normalize();

            Vector vector_Normal = new(-vector.Y, vector.X);

            //Built in SCREEN space so it stays the same size at every zoom. A head scaled with the building
            //is a dot on a site plan and a wedge across a room.
            System.Windows.Point point_1 = point_End - (vector * arrowHead_Px) + (vector_Normal * (arrowHead_Px / 2.5));
            System.Windows.Point point_2 = point_End - (vector * arrowHead_Px) - (vector_Normal * (arrowHead_Px / 2.5));

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
        /// Draws one tag where the placement engine put it: a white box with a hairline border, the way a
        /// drawing tag reads, with the text in the air type's own colour so supply, extract and local kitchen
        /// extract stay distinguishable and the legend keeps meaning something.
        /// <para>
        /// A leader only where there is a real coordinate to lead back to - see
        /// <see cref="HasPhysicalAnchor"/>.
        /// </para>
        /// </summary>
        private void DrawLabel(DrawingContext drawingContext, PartFOverlayMark mark, PartFAirflowAppearance appearance, System.Windows.Media.Matrix matrix, PartFTagPlacementResult partFTagPlacementResult)
        {
            Brush brush = new SolidColorBrush(Color.FromRgb(appearance.Red, appearance.Green, appearance.Blue));
            brush.Freeze();

            //The tag was laid out for the annotation scale, so it is drawn at the size that scale implies at
            //the current zoom: exactly the measured size when the view is at the annotation scale, and
            //proportionally larger or smaller elsewhere. Fixing the text at its measured pixel size instead
            //would let tags visibly collide at every zoom except one, and the drawing would stop matching
            //the layout that was actually solved.
            double factor = Factor(matrix);

            FormattedText formattedText = Text(Label(mark), brush, labelSize_Px * factor, true);

            FormattedText formattedText_Caption = Caption(mark) is string caption ? Text(caption, brush, captionSize_Px * factor, false) : null;

            double width = System.Math.Max(formattedText.Width, formattedText_Caption?.Width ?? 0);
            double height = formattedText.Height + (formattedText_Caption?.Height ?? 0);

            System.Windows.Point point_Anchor = matrix.Transform(new System.Windows.Point(mark.End.X, mark.End.Y));

            //Nothing was placed for this mark - it became visible between the last layout and this repaint.
            //Drawn beside the anchor rather than dropped: a tag carries a flow rate, and the next layout
            //will tidy it.
            System.Windows.Point point_Text = Screen(partFTagPlacementResult?.Rectangle2D, matrix) is Rect rect
                ? rect.TopLeft
                : new System.Windows.Point(point_Anchor.X + 6, point_Anchor.Y - (height / 2));

            //A leader only where the tag is attached to something the model actually locates. Built from the
            //engineering anchor and the solved rectangle, in the view layer - the shared solver knows
            //nothing about annotation and must not start to.
            if (HasPhysicalAnchor(mark) && partFTagPlacementResult?.Leader2D() is Segment2D segment2D)
            {
                drawingContext.DrawLine(
                    LeaderPen(brush),
                    matrix.Transform(new System.Windows.Point(segment2D[0].X, segment2D[0].Y)),
                    matrix.Transform(new System.Windows.Point(segment2D[1].X, segment2D[1].Y)));
            }

            //The tag: a white box with a hairline border and compact padding, drawn behind the text.
            Rect rect_Tag = new(
                point_Text.X - (tagPadding_Px * factor),
                point_Text.Y - (tagPadding_Px * factor / 2),
                width + (tagPadding_Px * factor * 2),
                height + (tagPadding_Px * factor));

            drawingContext.DrawRectangle(plateBrush, TagPen(factor), rect_Tag);

            drawingContext.DrawText(formattedText, point_Text);

            if (formattedText_Caption is not null)
            {
                drawingContext.DrawText(formattedText_Caption, new System.Windows.Point(point_Text.X, point_Text.Y + formattedText.Height));
            }
        }

        /// <summary>
        /// Fades the plan outside the dwellings being assessed, so the flats being reported read first and
        /// the building around them stays as context rather than disappearing.
        /// <para>
        /// A translucent veil over the other spaces, drawn on the overlay, rather than a filtered model.
        /// Filtering would mean building a second, reduced model for every dwelling and every level, and it
        /// would take the neighbouring walls away - which is exactly the context an engineer needs to
        /// recognise where a flat sits.
        /// </para>
        /// </summary>
        private void DrawContextVeil(System.Windows.Media.Matrix matrix)
        {
            if (!ViewSettings.ShowContextGeometry || adjacencyCluster is null || floorPlan2DControl?.Plane is null)
            {
                return;
            }

            HashSet<Guid> guids = [.. Filtered().SelectMany(x => x.Terminals ?? []).Where(x => x is not null).Select(x => x.SpaceGuid)];
            if (guids.Count == 0)
            {
                return;
            }

            DrawingVisual drawingVisual = new();

            using (DrawingContext drawingContext = drawingVisual.RenderOpen())
            {
                foreach (Space space in adjacencyCluster.GetSpaces() ?? [])
                {
                    if (space is null || guids.Contains(space.Guid))
                    {
                        continue;
                    }

                    foreach (Face2D face2D in adjacencyCluster.SpaceSectionFace2Ds(space, floorPlan2DControl.Plane) ?? [])
                    {
                        List<Point2D> point2Ds = (face2D?.ExternalEdge2D as ISegmentable2D)?.GetPoints();
                        if (point2Ds is null || point2Ds.Count < 3)
                        {
                            continue;
                        }

                        StreamGeometry streamGeometry = new();
                        using (StreamGeometryContext streamGeometryContext = streamGeometry.Open())
                        {
                            streamGeometryContext.BeginFigure(matrix.Transform(new System.Windows.Point(point2Ds[0].X, point2Ds[0].Y)), true, true);

                            for (int i = 1; i < point2Ds.Count; i++)
                            {
                                streamGeometryContext.LineTo(matrix.Transform(new System.Windows.Point(point2Ds[i].X, point2Ds[i].Y)), false, false);
                            }
                        }

                        streamGeometry.Freeze();

                        drawingContext.DrawGeometry(veilBrush, null, streamGeometry);
                    }
                }
            }

            ownVisual.Children.Add(drawingVisual);
        }

        // ------------------------------------------------------------------
        // What is visible, and what it says
        // ------------------------------------------------------------------

        /// <summary>
        /// The dwellings this view draws, after its own dwelling filter.
        /// <para>
        /// The filter stores the dwelling ZONE's guid, so renaming a flat does not silently change what a
        /// saved view shows. The assessment identifies its dwelling by name, because that is what an
        /// assessment is about, so the guid is resolved to a zone name here rather than being held as a name
        /// in the view settings. A guid that matches no zone draws nothing, which is the honest answer: the
        /// dwelling this view was made for is not in this model any more.
        /// </para>
        /// </summary>
        private List<PartFComplianceResult> Filtered()
        {
            if (ViewSettings.DwellingFilter != PartFDwellingFilter.SelectedDwelling || ViewSettings.DwellingGuid == Guid.Empty)
            {
                return partFComplianceResults;
            }

            string name = adjacencyCluster?.GetZones()?.Find(x => x is not null && x.Guid == ViewSettings.DwellingGuid)?.Name;

            return string.IsNullOrWhiteSpace(name)
                ? []
                : [.. partFComplianceResults.Where(x => string.Equals(x.DwellingName, name, StringComparison.Ordinal))];
        }

        /// <summary>
        /// Whether a mark is drawn at all, from the view's own visibility settings.
        /// </summary>
        private bool Visible(PartFOverlayMark mark)
        {
            if (mark.IsTransfer)
            {
                if (!ViewSettings.ShowTransfer)
                {
                    return false;
                }

                //An unresolved route can be hidden, but hiding it is a deliberate act: absence of evidence
                //is not compliance, so it is shown by default.
                return !mark.IsUnresolved || ViewSettings.ShowUnresolved;
            }

            return mark.AirType switch
            {
                PartFAirflowAppearance.AirType.Supply => ViewSettings.ShowSupply,
                PartFAirflowAppearance.AirType.GeneralExtract => ViewSettings.ShowGeneralExtract,
                PartFAirflowAppearance.AirType.LocalKitchenExtract => ViewSettings.ShowLocalKitchenExtract,
                PartFAirflowAppearance.AirType.OutdoorAir or PartFAirflowAppearance.AirType.ExhaustAir => ViewSettings.ShowOutdoorAndExhaust,
                _ => true,
            };
        }

        /// <summary>
        /// The tag's text: the semantic abbreviation, the rate, and a status symbol where it adds something -
        /// "SUP 63.0 l/s ✓". The abbreviation carries the air type in words, so the tag reads on a black and
        /// white printout and to a colour-blind reader without relying on the colour of the text.
        /// <para>
        /// <b>The number and the symbol say different things, and must keep doing so.</b> The number is the
        /// CALCULATED design value - what Approved Document F requires and SAM has allocated. The ✓ or ? is
        /// the provision or compliance status. A kitchen with a calculated 55 l/s and no recorded extract
        /// method reads "KEX 55.0 l/s ?": the rate is right and nothing has been established about the
        /// installation. Merging the two - dropping the symbol, or blanking the rate - would turn SAM's own
        /// proposal into a survey.
        /// </para>
        /// </summary>
        /// <summary>
        /// "F ", drawn before every Part F tag so it reads as the Approved Document F requirement even
        /// where a <see cref="DesignAirFlowRenderer"/> tag for the same space or the same route sits right
        /// beside it - see that renderer's own <c>AuthorityPrefix</c>. Position (the lane, see
        /// <see cref="PartFTagPlacement.Lane"/>) and colour tell the two apart too, but neither may be the
        /// ONLY thing that does: printed in black and white, or read by someone who cannot see colour, the
        /// prefix is what is left. Not "Calculated" - a Part F requirement, a design airflow, an operating
        /// airflow and an equipment capacity may all be calculated values, so that word would say nothing
        /// about WHICH authority this tag reports.
        /// </summary>
        internal const string AuthorityPrefix = "F ";

        private string Label(PartFOverlayMark mark)
        {
            PartFAirflowAppearance appearance = PartFAirflowAppearance.Get(mark.AirType);

            string result = ViewSettings.ShowValues ? mark.Label : appearance.Abbreviation;

            if (ViewSettings.ShowCompliance)
            {
                string symbol = PartFAirflowAppearance.Status(mark.Status).Symbol;

                //An unresolved transfer route already carries a trailing "?" in its own label, so adding the
                //cannot-be-determined symbol produced "TRA 63.0 l/s ? ?". One question mark is the message.
                if (!string.IsNullOrWhiteSpace(symbol) && !result.TrimEnd().EndsWith(symbol, StringComparison.Ordinal))
                {
                    result = string.Concat(result, " ", symbol);
                }
            }

            return string.Concat(AuthorityPrefix, result);
        }

        /// <summary>The tag's second line, or null where the mark needs no qualifying.</summary>
        private string Caption(PartFOverlayMark mark)
        {
            return ViewSettings.ShowDoorRequirements && !string.IsNullOrWhiteSpace(mark.Caption) ? mark.Caption : null;
        }

        /// <summary>
        /// A tag's measured size in SCREEN pixels at the annotation scale, which is what the placement
        /// converts into plane units. Measured exactly as it is drawn, so the box the engine reserves is the
        /// box the text fills.
        /// </summary>
        private void Size(PartFOverlayMark mark, out double width, out double height)
        {
            FormattedText formattedText = Text(Label(mark), Brushes.Black, labelSize_Px, true);

            FormattedText formattedText_Caption = Caption(mark) is string caption ? Text(caption, Brushes.Black, captionSize_Px, false) : null;

            width = System.Math.Max(formattedText.Width, formattedText_Caption?.Width ?? 0);
            height = formattedText.Height + (formattedText_Caption?.Height ?? 0);
        }

        /// <summary>
        /// Whether this mark stands for something the model puts at an actual coordinate, which is what
        /// decides whether a leader from the tag would be informative or a fabrication.
        /// <para>
        /// A transfer route does: it crosses a modelled door, or a modelled partition between two spaces, and
        /// both are drawn on the plan. A terminal does not. Approved Document F requires a room to have
        /// extract or supply; it does not say where in the ceiling the grille goes, and nothing in the
        /// analytical model does either - so the terminal mark sits at a synthetic point inside the room, and
        /// a leader drawn to it would point confidently at a location nobody has established.
        /// </para>
        /// <para>
        /// The placement layer still computes leaders for every tag, and this is the only thing suppressing
        /// them - so the day a real terminal object with a coordinate exists, this returns true for it and
        /// its leader appears. That is why the capability is kept rather than deleted.
        /// </para>
        /// </summary>
        private static bool HasPhysicalAnchor(PartFOverlayMark mark)
        {
            return mark.IsTransfer;
        }

        // ------------------------------------------------------------------
        // Geometry and brushes
        // ------------------------------------------------------------------

        /// <summary>
        /// The space's own section outline on this plan, cached per space for the length of one layout.
        /// <para>
        /// The space itself is found through <see cref="AdjacencyCluster.GetObject{T}(Guid)"/> - a
        /// dictionary lookup keyed on the object's own type and guid, not a scan of every space in the
        /// model. <c>GetSpaces()?.Find(...)</c> would make this method, and so <see cref="Place"/>, one
        /// linear scan of the WHOLE space list per space with a visible mark; on a large model that is
        /// quadratic in the number of spaces, and the per-layout cache only removes the repeat lookups for
        /// a second or third mark in the SAME space, not the first lookup of each new one.
        /// </para>
        /// </summary>
        private IClosed2D LimitArea(Dictionary<Guid, IClosed2D> dictionary_LimitArea, Guid guid_Space)
        {
            if (dictionary_LimitArea.TryGetValue(guid_Space, out IClosed2D result))
            {
                return result;
            }

            Space space = adjacencyCluster.GetObject<Space>(guid_Space);

            //The largest piece, matching the anchor: a room cut into a big part and a sliver is tagged in the
            //big part, so constraining the tag to the sliver would leave it unplaceable.
            result = space is null
                ? null
                : adjacencyCluster.SpaceSectionFace2Ds(space, floorPlan2DControl.Plane)?.Where(x => x is not null).OrderByDescending(x => x.GetArea()).FirstOrDefault();

            dictionary_LimitArea[guid_Space] = result;

            return result;
        }

        /// <summary>
        /// The shared row [m] this space's Part F and Ventilation Design tags are split about - see
        /// <see cref="PartFTagPlacement.Lane"/> - cached per space for the length of one layout. The room
        /// outline's own internal point: the same point <see cref="PartFFloorPlanOverlay"/> anchors an
        /// un-fanned terminal mark at, so the two overlays agree on where the row is without either reading
        /// the other's marks.
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
        /// The text obstacles a load lays the tags out against:
        /// <list type="bullet">
        /// <item><b>a replacement geometry was supplied</b> - the obstacles are measured from it, so a tag
        /// is never placed over the room names the plan is now showing;</item>
        /// <item><b>none was supplied</b> - the previous load's obstacles are kept, because a camera-only or
        /// attribute-only update has not regenerated the plan and its text is still there to keep clear
        /// of. Clearing them here would re-place the tags onto labels that are still drawn.</item>
        /// </list>
        /// </summary>
        internal static List<IClosed2D> ResolveTextObstacles(GeometryObjectModel geometryObjectModel, Plane plane, List<IClosed2D> previous)
        {
            return geometryObjectModel is null ? previous ?? [] : TextObstacle2Ds(geometryObjectModel, plane);
        }

        /// <summary>
        /// The bounds of every piece of text the plan itself has drawn - room names, door labels - in the
        /// plan's own 2D coordinates.
        /// <para>
        /// <b>Protected geometry.</b> A room name is the primary annotation of a space and a Part F tag is
        /// engineering annotation near it; the tag gives way, never the other way round.
        /// </para>
        /// <para>
        /// Measured, not assumed. The positions come from the loaded geometry, because the plan's own labels
        /// were placed by the same shared solver moments earlier and can sit a long way from a space's
        /// anchor. The widths come from the text and its own <c>TextAppearance</c>, through the same
        /// measurement the plan used, so the box a tag is kept out of is the box the words actually fill.
        /// </para>
        /// </summary>
        private static List<IClosed2D> TextObstacle2Ds(GeometryObjectModel geometryObjectModel, Plane plane)
        {
            List<IClosed2D> result = [];

            if (geometryObjectModel is null || plane is null)
            {
                return result;
            }

            foreach (Geometry3DObjectCollection geometry3DObjectCollection in geometryObjectModel.GetSAMGeometryObjects<Geometry3DObjectCollection>() ?? [])
            {
                foreach (ISAMGeometry3DObject sAMGeometry3DObject in geometry3DObjectCollection ?? [])
                {
                    //Text the plan blanked is text its own label solve could not place; there is nothing
                    //drawn to keep clear of.
                    if (sAMGeometry3DObject is not Text3DObject text3DObject || string.IsNullOrWhiteSpace(text3DObject.Text))
                    {
                        continue;
                    }

                    Point2D point2D = Geometry.Spatial.Query.Convert(plane, text3DObject.Plane?.Origin);

                    double height = text3DObject.TextAppearance?.Height ?? 0;

                    if (point2D is null || height <= 0)
                    {
                        continue;
                    }

                    //The same call the plan makes to size its own label, so the two agree.
                    double width = SAM.Core.UI.WPF.Query.Width(text3DObject.Text, new System.Drawing.Font(text3DObject.TextAppearance.FontFamilyName, System.Convert.ToSingle(height)), height);
                    if (width <= 0)
                    {
                        continue;
                    }

                    //Centred on the drawn position: the plan's label solver returns its rectangle's centroid,
                    //and that centroid is what became this text's origin.
                    result.Add(new Rectangle2D(new Point2D(point2D.X - (width / 2), point2D.Y - (height / 2)), width, height));
                }
            }

            return result;
        }

        /// <summary>
        /// How much bigger the drawing is than the sheet: the view's own scale over the annotation scale. One
        /// at the annotation scale.
        /// </summary>
        private double Factor(System.Windows.Media.Matrix matrix)
        {
            return System.Math.Max(System.Math.Abs(matrix.M11), System.Math.Abs(matrix.M22)) / PartFTagPlacement.PixelsPerMetre(ViewSettings.AnnotationScale);
        }

        /// <summary>
        /// A plane rectangle as a screen rectangle: the bounding box of its transformed corners, so it is
        /// right whichever way the view flips the axes.
        /// </summary>
        internal static Rect? Screen(Rectangle2D rectangle2D, System.Windows.Media.Matrix matrix)
        {
            List<Point2D> point2Ds = rectangle2D?.GetPoints();
            if (point2Ds is null || point2Ds.Count == 0)
            {
                return null;
            }

            double x_Min = double.MaxValue;
            double y_Min = double.MaxValue;
            double x_Max = double.MinValue;
            double y_Max = double.MinValue;

            foreach (Point2D point2D in point2Ds)
            {
                System.Windows.Point point = matrix.Transform(new System.Windows.Point(point2D.X, point2D.Y));

                x_Min = System.Math.Min(x_Min, point.X);
                y_Min = System.Math.Min(y_Min, point.Y);
                x_Max = System.Math.Max(x_Max, point.X);
                y_Max = System.Math.Max(y_Max, point.Y);
            }

            return new Rect(new System.Windows.Point(x_Min, y_Min), new System.Windows.Point(x_Max, y_Max));
        }

        internal static FormattedText Text(string text, Brush brush, double size, bool bold)
        {
            return new FormattedText(
                text ?? string.Empty,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, bold ? FontWeights.Bold : FontWeights.Normal, FontStretches.Normal),
                System.Math.Max(1, size),
                brush,
                96);
        }

        /// <summary>
        /// The tag's background: white, and nearly opaque. Not a translucent wash - a tag has to read as
        /// annotation laid ON the drawing, and a wash over a hatched wall or a coloured space fill turns the
        /// text into part of the pattern behind it. A little transparency is kept so the geometry underneath
        /// can still be made out.
        /// </summary>
        private static Brush Plate()
        {
            SolidColorBrush result = new(Color.FromArgb(0xF2, 0xFF, 0xFF, 0xFF));

            result.Freeze();

            return result;
        }

        /// <summary>
        /// The tag's border: a neutral grey, deliberately not the air type's colour. The border's job is to
        /// separate the tag from what it sits on; the air type is already said by the abbreviation and by the
        /// colour of the text, and a coloured outline as well would make a small tag shout.
        /// </summary>
        private static Brush TagBorder()
        {
            SolidColorBrush result = new(Color.FromArgb(0xFF, 0x9A, 0x9A, 0x9A));

            result.Freeze();

            return result;
        }

        private static Brush Veil()
        {
            SolidColorBrush result = new(Color.FromArgb(0xB0, 0xFF, 0xFF, 0xFF));

            result.Freeze();

            return result;
        }

        /// <summary>
        /// A hairline border, scaled with the annotation so it stays a hairline at every zoom. Internal, not
        /// private - see <see cref="tagPadding_Px"/>.
        /// </summary>
        internal static Pen TagPen(double factor)
        {
            Pen result = new(tagBorderBrush, System.Math.Max(0.6, 0.7 * factor));

            result.Freeze();

            return result;
        }

        /// <summary>
        /// A leader is a thin hairline in the mark's own colour: it has to connect the tag to the mark
        /// without competing with either.
        /// </summary>
        internal static Pen LeaderPen(Brush brush)
        {
            Pen result = new(brush, 0.7);

            result.Freeze();

            return result;
        }

        private static Pen Pen(PartFAirflowAppearance partFAirflowAppearance, bool unresolved)
        {
            SolidColorBrush brush = new(Color.FromRgb(partFAirflowAppearance.Red, partFAirflowAppearance.Green, partFAirflowAppearance.Blue));
            brush.Freeze();

            Pen result = new(brush, partFAirflowAppearance.Thickness);

            //An unresolved mark is always dashed, whatever its air type's own pattern says. It is the one
            //case where the appearance is overridden here, and it is overridden in the safe direction: a
            //route that has not been established never looks more certain than one that has.
            if (unresolved || partFAirflowAppearance.LinePattern == PartFAirflowAppearance.Pattern.Dashed)
            {
                result.DashStyle = unresolved ? new DashStyle([2, 2], 0) : new DashStyle([4, 3], 0);
            }

            result.Freeze();

            return result;
        }
    }
}
