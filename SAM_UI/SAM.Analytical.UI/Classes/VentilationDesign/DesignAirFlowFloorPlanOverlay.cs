// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Geometry.Planar;
using SAM.Geometry.Spatial;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SAM.Analytical.UI
{
    /// <summary>
    /// Where each Ventilation Design mark belongs on a floor plan, in that plan's own 2D coordinates.
    /// <para>
    /// This class works out <b>positions</b> and nothing else. Every rate it carries is read unchanged
    /// from the DESIGN airflow authority: a space's own duty from <c>Query.VentilationTerminals</c> /
    /// <c>Query.VentilationTerminalDesignDuty_Lps</c> - <c>VentilationTerminal.DesignFlowRate_Lps</c>, which
    /// Iteration 2B writes to - and the air moving between two spaces from
    /// <c>Query.DesignTransferFlowRate_Lps</c>, which reads the <c>SpaceAirMovement</c> objects
    /// <c>Modify.AddPartFTransferAirMovements</c> writes on every Approved Document O round and SAM_Tas
    /// exports as inter-zone air movements.
    /// </para>
    /// <para>
    /// <b>No Approved Document F value is read or written here</b>, and none of this is written back onto
    /// <c>Space</c>. <see cref="PartFFloorPlanOverlay"/> and its <c>PartFComplianceResult</c> are a
    /// completely separate data path, so both overlays can disagree after an optimisation round and both be
    /// right: one shows what Approved Document F requires, this shows what the model is currently designed
    /// to move. That applies to the transfer marks in particular - Approved Document F's transfer figure is
    /// sized once from Table 1.2 and correctly does not follow a raised design duty, and this one is
    /// re-solved against the new duty every round.
    /// </para>
    /// <para>
    /// <b>The one thing the two overlays do share is geometry</b> - <see cref="TransferRouteGeometry"/>,
    /// where on the plan the wall between two rooms is. There is only one such place, and computing it
    /// twice would be two chances to draw the same route in two different positions on the same drawing.
    /// No value crosses; see that type's own remarks.
    /// </para>
    /// <para>
    /// It is deliberately free of any user interface dependency: no WPF, no brushes, no screen coordinates.
    /// It answers "where, in the building, does this design airflow value go", which is the part worth
    /// testing, and leaves drawing to the renderer.
    /// </para>
    /// <para>
    /// A space with no terminal in a direction gets no mark in that direction, and two spaces the design
    /// transfers nothing between get no transfer mark - not a mark reading "0 l/s". Absence and an authored
    /// zero are different answers, and only the model can tell them apart.
    /// </para>
    /// </summary>
    public class DesignAirFlowFloorPlanOverlay
    {
        /// <summary>Spacing [m] between two marks anchored in the same space, so they do not overlap.</summary>
        public const double MarkSpacing_M = 0.55;

        /// <summary>
        /// Half the length [m] of a transfer arrow, either side of the opening it crosses. The same length
        /// Approved Document F's own transfer arrows use - the two overlays draw the same kind of thing on
        /// the same drawing, and letting the lengths drift apart would read as a difference in meaning.
        /// </summary>
        public const double TransferArrowLength_M = PartFFloorPlanOverlay.ArrowLength_M;

        private DesignAirFlowFloorPlanOverlay()
        {
        }

        /// <summary>Every mark to draw, in a stable order.</summary>
        public List<DesignAirFlowOverlayMark> Marks { get; private set; } = [];

        /// <summary>
        /// What could not be placed on this plan, and why - a space with a design duty but no outline on
        /// this plan, a transfer route with no separating element in the model. Reported rather than
        /// approximated, matching <see cref="PartFFloorPlanOverlay.Unplaced"/>.
        /// </summary>
        public List<string> Unplaced { get; private set; } = [];

        /// <summary>
        /// Builds the overlay for every space in the model that has an established design supply or
        /// extract duty, and for every pair of spaces the design transfers air between.
        /// </summary>
        /// <param name="adjacencyCluster">The model the plan is drawn from.</param>
        /// <param name="plane">The floor plan's section plane. Marks are in this plane's 2D coordinates.</param>
        /// <param name="showNet">Whether a net mark is built for a space that has both a supply and an extract duty.</param>
        /// <param name="showTransfer">
        /// Whether transfer marks are built. On by default: the design air moving between two rooms is a
        /// design duty like the two it connects, not a derived reading like the net - and a plan showing a
        /// room's raised supply with no sign of where that air then goes is the exact gap this overlay was
        /// extended to close. Building them is skipped entirely when off, so a hidden mark costs no
        /// sectioning.
        /// </param>
        public static DesignAirFlowFloorPlanOverlay Build(AdjacencyCluster adjacencyCluster, Plane plane, bool showNet = false, bool showTransfer = true)
        {
            DesignAirFlowFloorPlanOverlay result = new();

            if (adjacencyCluster is null || plane is null)
            {
                return result;
            }

            //Anchors are computed once per space and shared by every mark in it - a room's supply, its
            //extract and the tail of a transfer route leaving it all read the same point.
            Dictionary<Guid, Point2D> dictionary_Anchor = [];

            result.BuildTerminalMarks(adjacencyCluster, plane, dictionary_Anchor, showNet);

            if (showTransfer)
            {
                result.BuildTransferMarks(adjacencyCluster, plane, dictionary_Anchor);
            }

            return result;
        }

        /// <summary>
        /// The marks belonging to one space - including a transfer route that ARRIVES at it, which is a
        /// statement about that room as much as about the room the air came from. Matches
        /// <see cref="PartFFloorPlanOverlay.MarksOf"/>.
        /// </summary>
        public List<DesignAirFlowOverlayMark> MarksOf(Guid spaceGuid)
        {
            return [.. Marks.Where(x => x.SpaceGuid == spaceGuid || x.DownstreamSpaceGuid == spaceGuid)];
        }

        // ------------------------------------------------------------------
        // Terminal marks
        // ------------------------------------------------------------------

        private void BuildTerminalMarks(AdjacencyCluster adjacencyCluster, Plane plane, Dictionary<Guid, Point2D> dictionary_Anchor, bool showNet)
        {
            foreach (Space space in adjacencyCluster.GetSpaces() ?? [])
            {
                if (space is null)
                {
                    continue;
                }

                List<VentilationTerminal> ventilationTerminals = adjacencyCluster.VentilationTerminals(space);

                double? supply_Lps = ventilationTerminals?.VentilationTerminalDesignDuty_Lps(FlowClassification.Supply);
                double? extract_Lps = ventilationTerminals?.VentilationTerminalDesignDuty_Lps(FlowClassification.Extract);

                //Nothing serves this space in either direction: no mark, not a mark reading "0 l/s".
                if (supply_Lps is null && extract_Lps is null)
                {
                    continue;
                }

                Point2D point2D_Anchor = Anchor(adjacencyCluster, plane, dictionary_Anchor, space);
                if (point2D_Anchor is null)
                {
                    Unplaced.Add(string.Format("'{0}' has no outline on this floor plan, so its design airflow mark(s) were not drawn. Check that the space reaches the plan's cut level.", space.Name));
                    continue;
                }

                List<(DesignAirFlowMarkType MarkType, double? FlowRate_Lps)> entries = [];

                if (supply_Lps is not null)
                {
                    entries.Add((DesignAirFlowMarkType.Supply, supply_Lps));
                }

                if (extract_Lps is not null)
                {
                    entries.Add((DesignAirFlowMarkType.Extract, extract_Lps));
                }

                //Net is only meaningful where BOTH directions are established. A space with an extract duty
                //and no supply terminal at all has no "net" the model has actually said anything about -
                //treating the missing side as zero would be inventing a value the same way a bare "0 l/s"
                //terminal mark would.
                if (showNet && supply_Lps is not null && extract_Lps is not null)
                {
                    entries.Add((DesignAirFlowMarkType.Net, supply_Lps.Value - extract_Lps.Value));
                }

                for (int i = 0; i < entries.Count; i++)
                {
                    (DesignAirFlowMarkType markType, double? flowRate_Lps) = entries[i];

                    //Fanned vertically about the anchor, matching PartFFloorPlanOverlay: deterministic, so
                    //the same model always draws the same plan, and no mark ever sits on top of another.
                    double offset = (i - ((entries.Count - 1) / 2.0)) * MarkSpacing_M;

                    Point2D point2D = new(point2D_Anchor.X, point2D_Anchor.Y + offset);

                    Marks.Add(new DesignAirFlowOverlayMark
                    {
                        SpaceGuid = space.Guid,
                        SpaceName = space.Name,
                        MarkType = markType,
                        FlowRate_Lps = flowRate_Lps,
                        Position = point2D,

                        //A terminal is a grille, not a trajectory: a single point, exactly as
                        //PartFOverlayMark's own terminal marks are. See DesignAirFlowOverlayMark.Direction.
                        Start = point2D,
                        End = point2D,
                        Label = Label(markType, flowRate_Lps),
                    });
                }
            }
        }

        // ------------------------------------------------------------------
        // Transfer marks
        // ------------------------------------------------------------------

        /// <summary>
        /// One mark per pair of spaces the design actually transfers air between.
        /// <para>
        /// The routes are not rediscovered here and the flows are not re-solved. The design transfer air
        /// is already in the model as <c>SpaceAirMovement</c> objects, written by
        /// <c>Modify.AddPartFTransferAirMovements</c> over the node-balance solve every Approved Document O
        /// round performs, and read back by <c>Query.DesignTransferSpaceAirMovements</c> in a single pass.
        /// A second solve here could disagree with what the model will actually export to TAS, and a floor
        /// plan that disagrees with the simulation is worse than no floor plan.
        /// </para>
        /// <para>
        /// <c>PartFAirflowNetwork</c> supplies only the TOPOLOGY - which spaces adjoin, and which door
        /// apertures are modelled in the partitions between them. Despite its name it computes no Approved
        /// Document F requirement and reads no Approved Document F data; it is the same adjacency graph the
        /// design movements were solved over, which is exactly why it is the right thing to look the
        /// openings up in.
        /// </para>
        /// </summary>
        private void BuildTransferMarks(AdjacencyCluster adjacencyCluster, Plane plane, Dictionary<Guid, Point2D> dictionary_Anchor)
        {
            Dictionary<(Guid, Guid), Analytical.Query.DesignTransferAirMovement> dictionary_SpaceAirMovement = adjacencyCluster.DesignTransferSpaceAirMovements();
            if (dictionary_SpaceAirMovement.Count == 0)
            {
                return;
            }

            PartFAirflowNetwork partFAirflowNetwork = new(adjacencyCluster, adjacencyCluster.GetSpaces());

            HashSet<(Guid, Guid)> drawn = [];

            //Connections come back in deterministic order, so the same model always produces the same list.
            foreach ((Guid, Guid) connection in partFAirflowNetwork.Connections)
            {
                double? flowRate_Lps = adjacencyCluster.DesignTransferFlowRate_Lps(connection.Item1, connection.Item2, out Guid guid_From, out Guid guid_To, dictionary_SpaceAirMovement);

                //The design moves nothing between these two rooms. No mark, and certainly not a "TRA 0.0".
                if (flowRate_Lps is null)
                {
                    continue;
                }

                Space space_From = partFAirflowNetwork.Space(guid_From);
                Space space_To = partFAirflowNetwork.Space(guid_To);

                if (space_From is null || space_To is null)
                {
                    continue;
                }

                Point2D point2D_From = Anchor(adjacencyCluster, plane, dictionary_Anchor, space_From);
                Point2D point2D_To = Anchor(adjacencyCluster, plane, dictionary_Anchor, space_To);

                if (point2D_From is null || point2D_To is null)
                {
                    Unplaced.Add(string.Format("The design transfer between '{0}' and '{1}' was not drawn: {2} has no outline on this floor plan.", space_From.Name, space_To.Name, point2D_From is null ? space_From.Name : space_To.Name));
                    drawn.Add(Key(guid_From, guid_To));
                    continue;
                }

                //One modelled door is a route the plan can point at. None, or several, is not: with several
                //the model does not say which of them carries the air, and choosing one would assert a
                //split nothing has calculated.
                List<Aperture> apertures = partFAirflowNetwork.Apertures(connection);

                DesignTransferOpeningStatus openingStatus = apertures.Count switch
                {
                    1 => DesignTransferOpeningStatus.ModelledDoor,
                    0 => DesignTransferOpeningStatus.NoModelledOpening,
                    _ => DesignTransferOpeningStatus.MoreThanOneModelledOpening,
                };

                Aperture aperture = openingStatus == DesignTransferOpeningStatus.ModelledDoor ? apertures[0] : null;

                //The aperture OBJECT, not its guid: the network already found it, so re-finding it by guid
                //would be a scan of every panel in the model, once per route.
                Point2D point2D_Opening = TransferRouteGeometry.OpeningPoint2D(adjacencyCluster, plane, aperture, guid_From, guid_To, out bool isDoor);

                if (point2D_Opening is null)
                {
                    //The two spaces adjoin in the model but nothing separating them appears on this plan.
                    //Reported, and left undrawn: an arrow between the two room centres would read as air
                    //crossing a wall the reader can see, at a place the model never put an opening.
                    Unplaced.Add(string.Format("The design transfer between '{0}' and '{1}' was not drawn: no door or separating wall between the two spaces appears on this floor plan.", space_From.Name, space_To.Name));
                    drawn.Add(Key(guid_From, guid_To));
                    continue;
                }

                bool isUnresolved = openingStatus != DesignTransferOpeningStatus.ModelledDoor;

                //Pointing the way the design air was solved to move, so the arrow crosses the wall rather
                //than running between two room centres.
                Vector2D vector2D = new(point2D_To.X - point2D_From.X, point2D_To.Y - point2D_From.Y);

                vector2D = vector2D.Length < Core.Tolerance.Distance ? new Vector2D(1, 0) : vector2D.Unit;

                Marks.Add(new DesignAirFlowOverlayMark
                {
                    MarkType = DesignAirFlowMarkType.Transfer,
                    SpaceGuid = guid_From,
                    SpaceName = space_From.Name,
                    DownstreamSpaceGuid = guid_To,
                    DownstreamSpaceName = space_To.Name,
                    ApertureGuid = aperture?.Guid ?? Guid.Empty,
                    IsDoorRepresented = isDoor,
                    OpeningStatus = openingStatus,
                    FlowRate_Lps = flowRate_Lps,
                    Position = point2D_Opening,

                    //An established opening gets a real span in world units, centred on it, crossing the
                    //aperture. A route with no single modelled opening gets no span at all: it collapses to
                    //a point on the shared partition and the view draws a warning marker there. A long
                    //arrow is the visual claim that the air has a way through, and that is exactly the
                    //claim this route cannot make - the same rule PartFFloorPlanOverlay applies.
                    Start = isUnresolved
                        ? point2D_Opening
                        : new Point2D(point2D_Opening.X - (vector2D.X * TransferArrowLength_M), point2D_Opening.Y - (vector2D.Y * TransferArrowLength_M)),

                    End = isUnresolved
                        ? point2D_Opening
                        : new Point2D(point2D_Opening.X + (vector2D.X * TransferArrowLength_M), point2D_Opening.Y + (vector2D.Y * TransferArrowLength_M)),

                    Direction = vector2D,

                    //The trailing question mark is on the label itself, not only in the styling. A
                    //screenshot, a printout and a colour-blind reader all have to be able to tell a route
                    //with nowhere established to pass from one that crosses a real door.
                    Label = isUnresolved
                        ? string.Concat(Label(DesignAirFlowMarkType.Transfer, flowRate_Lps), " ?")
                        : Label(DesignAirFlowMarkType.Transfer, flowRate_Lps),

                    Caption = Caption(openingStatus),
                    IsUnresolved = isUnresolved,
                });

                drawn.Add(Key(guid_From, guid_To));
            }

            //A design air movement between two spaces the model does not show as adjoining - hand-authored,
            //or left behind by an edit that removed the partition. It cannot be drawn, because there is no
            //route to draw it on, and saying nothing would hide air the simulation will still move.
            foreach (KeyValuePair<(Guid, Guid), Analytical.Query.DesignTransferAirMovement> keyValuePair in dictionary_SpaceAirMovement)
            {
                if (drawn.Contains(keyValuePair.Key))
                {
                    continue;
                }

                Unplaced.Add(string.Format("The design air movement '{0}' was not drawn: the two spaces it connects do not adjoin through any partition in the model, so there is nowhere on the plan for it to cross.", keyValuePair.Value.SpaceAirMovement.Name));
            }
        }

        /// <summary>The two spaces' guids ordered so the same pair always produces the same key.</summary>
        private static (Guid, Guid) Key(Guid guid_1, Guid guid_2)
        {
            return guid_1.CompareTo(guid_2) <= 0 ? (guid_1, guid_2) : (guid_2, guid_1);
        }

        /// <summary>
        /// The second line under a transfer arrow, naming what the mark rests on - and only where that
        /// needs saying. A route through a single modelled door needs no caption, and captioning every
        /// arrow is how a plan becomes unreadable. Mirrors <see cref="PartFFloorPlanOverlay"/>'s own
        /// convention, and its wording where the two say the same thing.
        /// </summary>
        private static string Caption(DesignTransferOpeningStatus designTransferOpeningStatus)
        {
            return designTransferOpeningStatus switch
            {
                DesignTransferOpeningStatus.NoModelledOpening => "No modelled transfer opening identified",
                DesignTransferOpeningStatus.MoreThanOneModelledOpening => "More than one modelled opening between these spaces",
                _ => null,
            };
        }

        /// <summary>
        /// The tag text for one mark: the direction's abbreviation and its rate, or an explicit statement
        /// that no duty has been established - never a bare "0 l/s" standing in for "no terminal".
        /// </summary>
        private static string Label(DesignAirFlowMarkType markType, double? flowRate_Lps)
        {
            string abbreviation = markType switch
            {
                DesignAirFlowMarkType.Supply => "SUP",
                DesignAirFlowMarkType.Extract => "EXT",
                DesignAirFlowMarkType.Net => "NET",
                DesignAirFlowMarkType.Transfer => "TRA",
                _ => string.Empty,
            };

            if (flowRate_Lps is null)
            {
                return string.Format(CultureInfo.InvariantCulture, "{0} not established", abbreviation);
            }

            return markType == DesignAirFlowMarkType.Net
                ? string.Format(CultureInfo.InvariantCulture, "{0} {1}{2:0.0} l/s", abbreviation, flowRate_Lps.Value >= 0 ? "+" : string.Empty, flowRate_Lps.Value)
                : string.Format(CultureInfo.InvariantCulture, "{0} {1:0.0} l/s", abbreviation, flowRate_Lps.Value);
        }

        /// <summary>
        /// A point inside the space's own outline on this plan, or null where the space does not reach it.
        /// Same rule as <see cref="PartFFloorPlanOverlay"/>'s own anchor: the outline's internal point, not
        /// its centroid, so an L-shaped room is anchored inside itself.
        /// </summary>
        private static Point2D Anchor(AdjacencyCluster adjacencyCluster, Plane plane, Dictionary<Guid, Point2D> dictionary_Anchor, Space space)
        {
            if (dictionary_Anchor.TryGetValue(space.Guid, out Point2D result))
            {
                return result;
            }

            List<Face2D> face2Ds = adjacencyCluster.SpaceSectionFace2Ds(space, plane);

            Face2D face2D = face2Ds?.Where(x => x is not null).OrderByDescending(x => x.GetArea()).FirstOrDefault();

            result = face2D?.GetInternalPoint2D();

            dictionary_Anchor[space.Guid] = result;

            return result;
        }
    }
}
