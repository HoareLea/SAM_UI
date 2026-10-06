// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using SAM.Geometry.Planar;
using SAM.Geometry.Spatial;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.UI
{
    /// <summary>
    /// Where each Part F airflow mark belongs on a floor plan, in that plan's own 2D coordinates.
    /// <para>
    /// This class works out <b>positions</b> and nothing else. Every rate, direction, status and
    /// diagnostic it carries is read from the calculated <see cref="PartFComplianceResult"/> unchanged -
    /// no regulatory value is recomputed, rounded or re-derived here, so the floor plan and the text
    /// schematic can never disagree about a number.
    /// </para>
    /// <para>
    /// It is deliberately free of any user interface dependency: no WPF, no brushes, no screen
    /// coordinates. It answers "where, in the building, does this arrow go", which is the part worth
    /// testing, and leaves drawing to the view. Colours, line patterns and symbols come from
    /// <see cref="PartFAirflowAppearance"/>.
    /// </para>
    /// <para>
    /// The positions are real. A space mark sits inside that space's actual section outline on the plan,
    /// and a transfer mark crosses the actual internal opening or separating wall between the two spaces.
    /// Nothing here lays rooms out; where the model does not place something, it is reported through
    /// <see cref="Unplaced"/> rather than invented.
    /// </para>
    /// </summary>
    public class PartFFloorPlanOverlay
    {
        /// <summary>
        /// Length [m] of a terminal arrow on the plan, and half the length of a transfer arrow either
        /// side of the opening it crosses. A world-space length so the arrows keep their relationship to
        /// the rooms as the plan is zoomed; the view scales the head and the label, not this.
        /// </summary>
        public const double ArrowLength_M = 0.9;

        /// <summary>Spacing [m] between two marks anchored in the same space, so they do not overlap.</summary>
        public const double MarkSpacing_M = 0.55;

        private PartFFloorPlanOverlay(string dwellingName, PartFOperatingMode partFOperatingMode)
        {
            DwellingName = dwellingName;
            OperatingMode = partFOperatingMode;
        }

        /// <summary>The dwelling this overlay belongs to. Nothing outside it is ever marked.</summary>
        public string DwellingName { get; private set; }

        /// <summary>
        /// The operating condition every rate on every mark was read at. Changed by
        /// <see cref="Refresh"/> without disturbing any position.
        /// </summary>
        public PartFOperatingMode OperatingMode { get; private set; }

        /// <summary>Every mark to draw, in a stable order.</summary>
        public List<PartFOverlayMark> Marks { get; private set; } = [];

        /// <summary>
        /// What could not be placed on this plan, and why - a space that does not reach the plane, a
        /// transfer route with no separating element in the model.
        /// <para>
        /// Reported rather than approximated. A transfer arrow drawn between two room centres because the
        /// wall could not be found would look exactly like one crossing a real door, and would be telling
        /// the reader something the model does not say.
        /// </para>
        /// </summary>
        public List<string> Unplaced { get; private set; } = [];

        /// <summary>
        /// Builds the overlay for one dwelling on one floor plan.
        /// </summary>
        /// <param name="adjacencyCluster">The model the plan is drawn from.</param>
        /// <param name="partFComplianceResult">The calculated assessment. Every value is taken from it.</param>
        /// <param name="plane">The floor plan's section plane. Marks are in this plane's 2D coordinates.</param>
        /// <param name="partFOperatingMode">The condition to read rates at.</param>
        public static PartFFloorPlanOverlay Build(AdjacencyCluster adjacencyCluster, PartFComplianceResult partFComplianceResult, Plane plane, PartFOperatingMode partFOperatingMode = PartFOperatingMode.ContinuousDesign)
        {
            PartFFloorPlanOverlay result = new(partFComplianceResult?.DwellingName, partFOperatingMode);

            if (adjacencyCluster is null || partFComplianceResult is null || plane is null)
            {
                return result;
            }

            //Anchors are computed once per space and shared by every mark in it, so a room with a supply
            //terminal and a local kitchen extract gets both marks in the same place rather than two
            //independently derived positions that happen not to line up.
            Dictionary<Guid, Point2D> dictionary_Anchor = [];
            Dictionary<Guid, Space> dictionary_Space = [];

            foreach (Space space in adjacencyCluster.GetSpaces() ?? [])
            {
                if (space is null)
                {
                    continue;
                }

                dictionary_Space[space.Guid] = space;
            }

            result.BuildTerminalMarks(adjacencyCluster, partFComplianceResult, plane, dictionary_Space, dictionary_Anchor);
            result.BuildTransferMarks(adjacencyCluster, partFComplianceResult, plane, dictionary_Space, dictionary_Anchor);

            return result;
        }

        /// <summary>The marks belonging to one space, terminal marks first.</summary>
        public List<PartFOverlayMark> MarksOf(Guid spaceGuid)
        {
            return [.. Marks.Where(x => x.SpaceGuid == spaceGuid || x.DownstreamSpaceGuid == spaceGuid)];
        }

        /// <summary>
        /// Re-reads every mark's rate and label at a new operating condition, leaving every position
        /// exactly where it was.
        /// <para>
        /// Switching between continuous, high, setback and measured changes what the system is doing, not
        /// where the rooms are. Rebuilding the overlay would re-section every space and every partition to
        /// arrive at the same coordinates, which on a large model is the difference between an instant
        /// switch and a visible pause.
        /// </para>
        /// </summary>
        public void Refresh(PartFComplianceResult partFComplianceResult, PartFOperatingMode partFOperatingMode)
        {
            if (partFComplianceResult is null)
            {
                return;
            }

            OperatingMode = partFOperatingMode;

            foreach (PartFOverlayMark mark in Marks)
            {
                PartFAirflowAppearance appearance = PartFAirflowAppearance.Get(mark.AirType);

                if (mark.IsTransfer)
                {
                    PartFDoorTransferData partFDoorTransferData = (partFComplianceResult.TransferPaths ?? [])
                        .Find(x => x is not null && string.Equals(x.Name, mark.DoorName, StringComparison.Ordinal) && x.UpstreamSpaceGuid == mark.SpaceGuid);

                    if (partFDoorTransferData is null)
                    {
                        continue;
                    }

                    double? rate_Transfer = PartFSchematic.Rate(partFDoorTransferData, partFOperatingMode);

                    mark.FlowRate_Lps = rate_Transfer;
                    mark.Label = partFDoorTransferData.IsOpeningUnresolved
                        ? string.Concat(appearance.Label(rate_Transfer), " ?")
                        : appearance.Label(rate_Transfer);

                    continue;
                }

                PartFVentilationTerminalRequirement terminal = (partFComplianceResult.Terminals ?? [])
                    .Find(x => x is not null && x.SpaceGuid == mark.SpaceGuid && x.TerminalRole == mark.TerminalRole);

                if (terminal is null)
                {
                    continue;
                }

                double? rate = PartFSchematic.Rate(terminal, partFOperatingMode);

                mark.FlowRate_Lps = rate;
                mark.Label = appearance.Label(rate);
            }
        }

        // ------------------------------------------------------------------
        // Terminal marks
        // ------------------------------------------------------------------

        private void BuildTerminalMarks(AdjacencyCluster adjacencyCluster, PartFComplianceResult partFComplianceResult, Plane plane, Dictionary<Guid, Space> dictionary_Space, Dictionary<Guid, Point2D> dictionary_Anchor)
        {
            //Grouped by space so the marks of one room can be fanned out from its single anchor. A studio
            //carries both a supply and a local kitchen extract terminal, and both have to be visible.
            foreach (IGrouping<Guid, PartFVentilationTerminalRequirement> grouping in partFComplianceResult.Terminals
                .Where(x => x is not null)
                .GroupBy(x => x.SpaceGuid))
            {
                List<PartFVentilationTerminalRequirement> terminals = [.. grouping.OrderBy(x => x.TerminalRole)];

                Point2D point2D_Anchor = Anchor(adjacencyCluster, plane, dictionary_Space, dictionary_Anchor, grouping.Key);
                if (point2D_Anchor is null)
                {
                    Unplaced.Add(string.Format("'{0}' has no outline on this floor plan, so its {1} terminal mark(s) were not drawn. Check that the space reaches the plan's cut level.", terminals[0].SpaceName, terminals.Count));
                    continue;
                }

                for (int i = 0; i < terminals.Count; i++)
                {
                    PartFVentilationTerminalRequirement terminal = terminals[i];

                    //Fanned vertically about the anchor: deterministic, so the same model always draws the
                    //same plan, and no mark is ever placed on top of another.
                    double offset = (i - ((terminals.Count - 1) / 2.0)) * MarkSpacing_M;

                    Point2D point2D = new(point2D_Anchor.X, point2D_Anchor.Y + offset);

                    PartFAirflowAppearance appearance = PartFAirflowAppearance.Get(terminal.TerminalRole);
                    double? rate = PartFSchematic.Rate(terminal, OperatingMode);

                    //Supply air arrives at the terminal, extract leaves through it. Drawing both the same
                    //way and relying on colour alone to tell them apart would fail the moment the plan was
                    //printed in black and white.
                    //
                    //A single point, not a span: a terminal is a grille, and the view draws a short stub
                    //from here. Giving it length in world units would put a long arrow across the room and
                    //assert an in-room trajectory nothing has calculated.
                    bool inbound = terminal.TerminalRole == PartFTerminalRole.Supply;

                    Marks.Add(new PartFOverlayMark
                    {
                        AirType = appearance.Type,
                        TerminalRole = terminal.TerminalRole,
                        SpaceGuid = terminal.SpaceGuid,
                        SpaceName = terminal.SpaceName,

                        //NOT terminal.Guid: the calculator builds a terminal with Guid.NewGuid(), so its own
                        //guid changes on every recalculation and a label keyed on it would jump back to the
                        //middle of the plan. Derived from the space and the role instead, both of which are
                        //persistent model identities. See PartFAnnotationKey.
                        AnnotationGuid = PartFAnnotationKey.Terminal(terminal.SpaceGuid, terminal.TerminalRole),
                        Start = point2D,
                        End = point2D,
                        Direction = new Vector2D(inbound ? 1 : -1, 0),
                        FlowRate_Lps = rate,
                        Label = appearance.Label(rate),
                        Status = terminal.ComplianceStatus,

                        //A terminal SAM proposed but nobody has confirmed as installed is drawn, because
                        //the room does need one, and flagged, because nothing in the model says it exists.
                        IsUnresolved = terminal.IsExtract && !terminal.IsProvisionRecorded,
                    });
                }
            }
        }

        // ------------------------------------------------------------------
        // Transfer marks
        // ------------------------------------------------------------------

        private void BuildTransferMarks(AdjacencyCluster adjacencyCluster, PartFComplianceResult partFComplianceResult, Plane plane, Dictionary<Guid, Space> dictionary_Space, Dictionary<Guid, Point2D> dictionary_Anchor)
        {
            foreach (PartFDoorTransferData partFDoorTransferData in partFComplianceResult.TransferPaths ?? [])
            {
                //Only routes inside one dwelling. An entrance door onto a communal corridor, a door to a
                //neighbouring flat and an external door all carry no Part F internal transfer requirement,
                //and an arrow across one would say air moves where the assessment never claimed it does.
                if (partFDoorTransferData is null || !partFDoorTransferData.IsInternalDwellingDoor)
                {
                    continue;
                }

                Point2D point2D_Upstream = Anchor(adjacencyCluster, plane, dictionary_Space, dictionary_Anchor, partFDoorTransferData.UpstreamSpaceGuid);
                Point2D point2D_Downstream = Anchor(adjacencyCluster, plane, dictionary_Space, dictionary_Anchor, partFDoorTransferData.DownstreamSpaceGuid);

                if (point2D_Upstream is null || point2D_Downstream is null)
                {
                    Unplaced.Add(string.Format("The transfer route '{0}' was not drawn: {1} has no outline on this floor plan.", partFDoorTransferData.Name, point2D_Upstream is null ? partFDoorTransferData.UpstreamSpaceName : partFDoorTransferData.DownstreamSpaceName));
                    continue;
                }

                Point2D point2D_Opening = TransferRouteGeometry.OpeningPoint2D(adjacencyCluster, plane, partFDoorTransferData.ApertureGuid, partFDoorTransferData.UpstreamSpaceGuid, partFDoorTransferData.DownstreamSpaceGuid, out bool isDoor);
                if (point2D_Opening is null)
                {
                    //The two spaces adjoin in the assessment but nothing separating them appears on this
                    //plan. Reported, and left undrawn: an arrow between the two room centres would read as
                    //air crossing a wall the reader can see, at a place the model never put an opening.
                    Unplaced.Add(string.Format("The transfer route '{0}' ({1} to {2}) was not drawn: no door or separating wall between the two spaces appears on this floor plan. The route and its calculated flow are in the internal doors schedule.", partFDoorTransferData.Name, partFDoorTransferData.UpstreamSpaceName, partFDoorTransferData.DownstreamSpaceName));
                    continue;
                }

                //A mark placed on the shared partition because no opening was found is DIAGNOSTIC. It shows
                //the engineer where the air would have to cross; it does not assert that anywhere to cross
                //exists. The opening status carries that distinction to the view, which styles it as
                //unresolved and marks the label, so the fallback can never read as a confirmed route.
                PartFTransferOpeningStatus openingStatus = partFDoorTransferData.OpeningStatus;

                //Centred on the real opening and pointing the way the air was calculated to move, so the
                //arrow crosses the wall rather than running between two room centres.
                Vector2D vector2D = new(point2D_Downstream.X - point2D_Upstream.X, point2D_Downstream.Y - point2D_Upstream.Y);

                vector2D = vector2D.Length < Core.Tolerance.Distance ? new Vector2D(1, 0) : vector2D.Unit;

                double? rate = PartFSchematic.Rate(partFDoorTransferData, OperatingMode);

                PartFAirflowAppearance appearance = PartFAirflowAppearance.Get(PartFAirflowAppearance.AirType.TransferAir);

                Marks.Add(new PartFOverlayMark
                {
                    AirType = PartFAirflowAppearance.AirType.TransferAir,
                    TerminalRole = PartFTerminalRole.Undefined,
                    SpaceGuid = partFDoorTransferData.UpstreamSpaceGuid,
                    SpaceName = partFDoorTransferData.UpstreamSpaceName,
                    DownstreamSpaceGuid = partFDoorTransferData.DownstreamSpaceGuid,
                    DownstreamSpaceName = partFDoorTransferData.DownstreamSpaceName,
                    ApertureGuid = partFDoorTransferData.ApertureGuid,
                    DoorName = partFDoorTransferData.Name,
                    IsDoorRepresented = isDoor,

                    //NOT partFDoorTransferData.Guid, which the builder generates afresh on every
                    //recalculation. The modelled door where there is one, and otherwise the two spaces the
                    //route crosses between - persistent model identities either way. See PartFAnnotationKey.
                    AnnotationGuid = PartFAnnotationKey.Transfer(partFDoorTransferData.ApertureGuid, partFDoorTransferData.UpstreamSpaceGuid, partFDoorTransferData.DownstreamSpaceGuid),

                    //An established opening gets a real span in world units, centred on it, crossing the
                    //aperture. A route with NO opening gets no span at all: it collapses to a point on the
                    //shared partition and the view draws a short dashed warning marker there. A long arrow
                    //running room to room is the visual claim that the air has a way through, and that is
                    //exactly the claim this route cannot make.
                    Start = partFDoorTransferData.IsOpeningUnresolved
                        ? point2D_Opening
                        : new Point2D(point2D_Opening.X - (vector2D.X * ArrowLength_M), point2D_Opening.Y - (vector2D.Y * ArrowLength_M)),

                    End = partFDoorTransferData.IsOpeningUnresolved
                        ? point2D_Opening
                        : new Point2D(point2D_Opening.X + (vector2D.X * ArrowLength_M), point2D_Opening.Y + (vector2D.Y * ArrowLength_M)),

                    Direction = vector2D,
                    FlowRate_Lps = rate,
                    OpeningStatus = openingStatus,

                    //The trailing question mark is on the label itself, not only in the styling. A
                    //screenshot, a printout and a colour-blind reader all have to be able to tell a
                    //calculated route from an established one.
                    Label = partFDoorTransferData.IsOpeningUnresolved
                        ? string.Concat(appearance.Label(rate), " ?")
                        : appearance.Label(rate),

                    Caption = Caption(partFDoorTransferData, openingStatus),
                    Status = partFDoorTransferData.ComplianceStatus,
                    IsUnresolved = partFDoorTransferData.IsOpeningUnresolved
                        || partFDoorTransferData.ComplianceStatus == PartFComplianceStatus.CannotBeDetermined,
                });
            }
        }

        /// <summary>
        /// The second line under a transfer arrow, naming what the mark rests on. Only where that needs
        /// saying: a route through a real door with a recorded free area needs no caption, and captioning
        /// every arrow is how a plan becomes unreadable.
        /// </summary>
        private static string Caption(PartFDoorTransferData partFDoorTransferData, PartFTransferOpeningStatus partFTransferOpeningStatus)
        {
            return partFTransferOpeningStatus switch
            {
                PartFTransferOpeningStatus.MissingTransferOpening => "No modelled transfer opening identified",
                PartFTransferOpeningStatus.AmbiguousRoute => "Route not fixed by the dwelling's topology",
                PartFTransferOpeningStatus.CalculatedViaPermanentOpening => string.Format("Via {0}", Core.Query.Description(partFDoorTransferData.TransferDeviceType).ToLowerInvariant()),
                _ => null,
            };
        }

        // ------------------------------------------------------------------
        // Geometry
        // ------------------------------------------------------------------

        /// <summary>
        /// A point inside the space's own outline on this plan, or null where the space does not reach it.
        /// <para>
        /// The outline's internal point, not its centroid: the centroid of an L-shaped room falls outside
        /// the room, and a mark placed there sits in the corridor next door.
        /// </para>
        /// </summary>
        private static Point2D Anchor(AdjacencyCluster adjacencyCluster, Plane plane, Dictionary<Guid, Space> dictionary_Space, Dictionary<Guid, Point2D> dictionary_Anchor, Guid guid_Space)
        {
            if (dictionary_Anchor.TryGetValue(guid_Space, out Point2D result))
            {
                return result;
            }

            result = null;

            if (dictionary_Space.TryGetValue(guid_Space, out Space space))
            {
                List<Face2D> face2Ds = adjacencyCluster.SpaceSectionFace2Ds(space, plane);

                //The largest piece, so a room cut into a big part and a sliver anchors in the big part.
                Face2D face2D = face2Ds?.Where(x => x is not null).OrderByDescending(x => x.GetArea()).FirstOrDefault();

                result = face2D?.GetInternalPoint2D();
            }

            dictionary_Anchor[guid_Space] = result;

            return result;
        }
    }
}
