// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.UI.WPF.Tests.Helpers;
using SAM.Core;
using SAM.Geometry.Planar;
using SAM.Geometry.Spatial;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// The Ventilation Design overlay's TRANSFER marks - the design air moving between two rooms.
    /// <para>
    /// <b>The gap these close.</b> Native acceptance testing found a floor plan showing a bedroom's design
    /// supply rising across Approved Document O rounds while the only transfer figure on the drawing still
    /// read the Approved Document F sizing it started from. Both numbers were right, and neither was wrong
    /// to stay where it was: Approved Document F's transfer requirement is fixed by Table 1.2 and must not
    /// follow an optimisation round. What was missing was the DESIGN transfer figure, which
    /// <c>Modify.AddPartFTransferAirMovements</c> re-solves every round and SAM_Tas already exports as an
    /// inter-zone air movement - nothing on the plan read it.
    /// </para>
    /// <para>
    /// <b>Flow and opening are independent.</b> A route can carry a fully determined design flow and still
    /// have no modelled opening: the flow says what the design needs to move, the opening status says
    /// whether the model shows anywhere for it to move through. Fixing one has never fixed the other, and
    /// these tests pin that it still does not - matching
    /// <c>PartFFloorPlanOverlayTests.MissingOpening_IsDrawnAsUnresolvedAndNeverAsConfirmed</c>.
    /// </para>
    /// </summary>
    public class DesignTransferOverlayTests
    {
        private static readonly Plane plane = Geometry.Spatial.Create.Plane(1.2);

        // -------------------------------------------------------------------------------------------
        // Fixture
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Two rooms sharing one partition, optionally carrying a door, with one design air movement
        /// between them. The movement is what <c>Modify.AddPartFTransferAirMovements</c> writes, added
        /// directly so the fixture states the design authority without running a whole Approved Document O
        /// preparation.
        /// </summary>
        private static PartFPlanModel Pair(double? transfer_Lps, string name_Door = "D01", bool reversed = false)
        {
            PartFPlanModel model = new PartFPlanModel()
                .Room("Bedroom", 8)
                .Room("Hall", 4)
                .Partition("Bedroom", "Hall", name_Door)
                .Zone("Flat 1", "Flats", true, "Bedroom", "Hall");

            if (transfer_Lps is not null)
            {
                AddTransfer(
                    model.AdjacencyCluster,
                    reversed ? model.Space("Hall") : model.Space("Bedroom"),
                    reversed ? model.Space("Bedroom") : model.Space("Hall"),
                    transfer_Lps.Value);
            }

            return model;
        }

        /// <summary>
        /// One design transfer leg, written exactly as <c>Modify.AddPartFTransferAirMovements</c> writes
        /// it: in the direction the air actually travels, with the rate in m3/s.
        /// </summary>
        private static void AddTransfer(AdjacencyCluster adjacencyCluster, Space space_From, Space space_To, double flowRate_Lps, string name = null)
        {
            SpaceAirMovement spaceAirMovement = new(
                name ?? string.Format("{0} to {1}", space_From.Name, space_To.Name),
                flowRate_Lps / 1000.0,
                new ObjectReference(space_From).ToString(),
                new ObjectReference(space_To).ToString());

            adjacencyCluster.AddObject(spaceAirMovement);
        }

        private static DesignAirFlowOverlayMark TransferMark(DesignAirFlowFloorPlanOverlay overlay)
        {
            return Assert.Single(overlay.Marks.Where(x => x.IsTransfer));
        }

        // -------------------------------------------------------------------------------------------
        // The figure itself
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// A design transfer between two rooms is drawn, reading the movement's own rate - not a rate
        /// re-solved here, and not Approved Document F's.
        /// </summary>
        [Fact]
        public void DesignTransfer_ShowsTheMovementsOwnFlow()
        {
            PartFPlanModel model = Pair(63.0);

            DesignAirFlowFloorPlanOverlay overlay = DesignAirFlowFloorPlanOverlay.Build(model.AdjacencyCluster, plane);

            DesignAirFlowOverlayMark mark = TransferMark(overlay);

            Assert.Equal(DesignAirFlowMarkType.Transfer, mark.MarkType);
            Assert.Equal(63.0, mark.FlowRate_Lps.Value, 6);
            Assert.Equal("TRA 63.0 l/s", mark.Label);
            Assert.Equal(model.Space("Bedroom").Guid, mark.SpaceGuid);
            Assert.Equal(model.Space("Hall").Guid, mark.DownstreamSpaceGuid);
        }

        /// <summary>
        /// <b>The acceptance finding, as a test.</b> An Approved Document O round that raises a room's
        /// design airflow re-solves the transfer air with it, and the tag follows - it does not stay at the
        /// figure the dwelling started from. Two models rather than one mutated in place, because
        /// <c>DesignAirFlowFloorPlanOverlay.Build</c> is a stateless factory and the point is that the
        /// SECOND model's plan reads the second model's air.
        /// </summary>
        [Fact]
        public void ARaisedDesignTransfer_ChangesTheTag()
        {
            DesignAirFlowOverlayMark mark_Baseline = TransferMark(DesignAirFlowFloorPlanOverlay.Build(Pair(63.0).AdjacencyCluster, plane));
            DesignAirFlowOverlayMark mark_Raised = TransferMark(DesignAirFlowFloorPlanOverlay.Build(Pair(150.0).AdjacencyCluster, plane));

            Assert.Equal("TRA 63.0 l/s", mark_Baseline.Label);
            Assert.Equal("TRA 150.0 l/s", mark_Raised.Label);

            Assert.NotEqual(mark_Baseline.FlowRate_Lps.Value, mark_Raised.FlowRate_Lps.Value, 6);
        }

        /// <summary>
        /// Two rooms the design transfers nothing between get NO transfer mark - not one reading
        /// "TRA 0.0 l/s". The same rule the supply and extract marks already follow: absence of a value and
        /// an authored zero are different answers, and only the model can tell them apart.
        /// </summary>
        [Fact]
        public void NoDesignTransfer_DrawsNoMark_AndNeverAnInventedZero()
        {
            PartFPlanModel model = Pair(null);

            DesignAirFlowFloorPlanOverlay overlay = DesignAirFlowFloorPlanOverlay.Build(model.AdjacencyCluster, plane);

            Assert.Empty(overlay.Marks.Where(x => x.IsTransfer));
            Assert.DoesNotContain(overlay.Marks, x => x.Label is not null && x.Label.StartsWith("TRA", StringComparison.Ordinal));
        }

        /// <summary>
        /// Direction is preserved, not reduced to an unsigned magnitude between two rooms. The mark reads
        /// FROM the room the design air leaves, whichever order the two rooms happen to sit in the model.
        /// </summary>
        [Fact]
        public void Direction_FollowsTheMovement_NotTheRoomOrder()
        {
            PartFPlanModel model_Outward = Pair(63.0);
            PartFPlanModel model_Inward = Pair(63.0, reversed: true);

            DesignAirFlowOverlayMark mark_Outward = TransferMark(DesignAirFlowFloorPlanOverlay.Build(model_Outward.AdjacencyCluster, plane));
            DesignAirFlowOverlayMark mark_Inward = TransferMark(DesignAirFlowFloorPlanOverlay.Build(model_Inward.AdjacencyCluster, plane));

            Assert.Equal(model_Outward.Space("Bedroom").Guid, mark_Outward.SpaceGuid);
            Assert.Equal(model_Outward.Space("Hall").Guid, mark_Outward.DownstreamSpaceGuid);

            Assert.Equal(model_Inward.Space("Hall").Guid, mark_Inward.SpaceGuid);
            Assert.Equal(model_Inward.Space("Bedroom").Guid, mark_Inward.DownstreamSpaceGuid);

            //And the arrow itself turns round with it, rather than only the guids.
            Assert.True((mark_Outward.Direction.X * mark_Inward.Direction.X) + (mark_Outward.Direction.Y * mark_Inward.Direction.Y) < 0);
        }

        // -------------------------------------------------------------------------------------------
        // The opening, which is a separate question from the flow
        // -------------------------------------------------------------------------------------------

        /// <summary>A route through one modelled door crosses that door, and claims nothing more.</summary>
        [Fact]
        public void OneModelledDoor_CrossesTheDoor_AndIsNotFlagged()
        {
            PartFPlanModel model = Pair(63.0);

            DesignAirFlowOverlayMark mark = TransferMark(DesignAirFlowFloorPlanOverlay.Build(model.AdjacencyCluster, plane));

            Assert.Equal(DesignTransferOpeningStatus.ModelledDoor, mark.OpeningStatus);
            Assert.True(mark.IsDoorRepresented);
            Assert.False(mark.IsUnresolved);
            Assert.Null(mark.Caption);
            Assert.Equal(model.ApertureGuid("D01"), mark.ApertureGuid);

            //A real span across the opening, centred on the door's own projected centre.
            Point3D point3D_Door = model.DoorCentroid("D01");
            Point2D point2D_Door = Geometry.Spatial.Query.Convert(plane, point3D_Door);

            Assert.NotEqual(mark.Start.X, mark.End.X, 6);

            Assert.Equal(point2D_Door.X, (mark.Start.X + mark.End.X) / 2, 6);
            Assert.Equal(point2D_Door.Y, (mark.Start.Y + mark.End.Y) / 2, 6);
        }

        /// <summary>
        /// <b>A missing opening does not hide the flow.</b> Where the two rooms adjoin through a partition
        /// with no modelled door, the design still needs that air to cross - so the figure is shown, with
        /// the diagnostic beside it, rather than the mark being dropped or silently reading as established.
        /// </summary>
        [Fact]
        public void NoModelledOpening_StillShowsTheDesignFlow_WithTheDiagnostic()
        {
            PartFPlanModel model = Pair(150.0, name_Door: null);

            DesignAirFlowOverlayMark mark = TransferMark(DesignAirFlowFloorPlanOverlay.Build(model.AdjacencyCluster, plane));

            Assert.Equal(DesignTransferOpeningStatus.NoModelledOpening, mark.OpeningStatus);
            Assert.False(mark.IsDoorRepresented);
            Assert.True(mark.IsUnresolved);

            //The flow is still the design's own, and the trailing marker is on the LABEL, so it survives a
            //printout and does not depend on colour.
            Assert.Equal(150.0, mark.FlowRate_Lps.Value, 6);
            Assert.Equal("TRA 150.0 l/s ?", mark.Label);
            Assert.Equal("No modelled transfer opening identified", mark.Caption);

            //And no span: there is nothing established to draw an arrow along.
            Assert.Equal(mark.Start.X, mark.End.X, 9);
            Assert.Equal(mark.Start.Y, mark.End.Y, 9);
        }

        /// <summary>
        /// A fully determined flow is not evidence of an opening. This is the design-side counterpart of
        /// <c>PartFFloorPlanOverlayTests.MissingOpening_IsDrawnAsUnresolvedAndNeverAsConfirmed</c>, and it
        /// is why raising a design duty can never "clear" an opening diagnostic.
        /// </summary>
        [Fact]
        public void RaisingTheDesignFlow_DoesNotResolveAMissingOpening()
        {
            DesignAirFlowOverlayMark mark_Low = TransferMark(DesignAirFlowFloorPlanOverlay.Build(Pair(8.0, name_Door: null).AdjacencyCluster, plane));
            DesignAirFlowOverlayMark mark_High = TransferMark(DesignAirFlowFloorPlanOverlay.Build(Pair(400.0, name_Door: null).AdjacencyCluster, plane));

            Assert.NotEqual(mark_Low.FlowRate_Lps.Value, mark_High.FlowRate_Lps.Value, 6);

            Assert.True(mark_Low.IsUnresolved);
            Assert.True(mark_High.IsUnresolved);

            Assert.Equal(mark_Low.OpeningStatus, mark_High.OpeningStatus);
            Assert.Equal(mark_Low.Caption, mark_High.Caption);
        }

        /// <summary>
        /// Two modelled doors between the same two rooms: the model does not say which of them carries the
        /// design air, so the mark stands on the partition and says so rather than committing to one door.
        /// </summary>
        [Fact]
        public void MoreThanOneModelledDoor_IsReported_RatherThanPickingOne()
        {
            PartFPlanModel model = new PartFPlanModel()
                .Room("Bedroom", 8)
                .Room("Hall", 4)
                .Partition("Bedroom", "Hall", "D01")
                .Partition("Bedroom", "Hall", "D02")
                .Zone("Flat 1", "Flats", true, "Bedroom", "Hall");

            AddTransfer(model.AdjacencyCluster, model.Space("Bedroom"), model.Space("Hall"), 63.0);

            DesignAirFlowOverlayMark mark = TransferMark(DesignAirFlowFloorPlanOverlay.Build(model.AdjacencyCluster, plane));

            Assert.Equal(DesignTransferOpeningStatus.MoreThanOneModelledOpening, mark.OpeningStatus);
            Assert.Equal(Guid.Empty, mark.ApertureGuid);
            Assert.True(mark.IsUnresolved);
            Assert.EndsWith(" ?", mark.Label, StringComparison.Ordinal);
            Assert.Equal("More than one modelled opening between these spaces", mark.Caption);
        }

        // -------------------------------------------------------------------------------------------
        // Independence from Approved Document F, and from the rest of the overlay
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Building the transfer marks touches no Approved Document F data, exactly as the terminal marks
        /// do not - see <c>DesignAirFlowFloorPlanOverlayTests.Build_DoesNotTouchPartFSpaceData</c>. The
        /// geometry helpers the two overlays now share carry no value in either direction, which is the
        /// distinction this asserts: a marker written onto the space survives untouched.
        /// </summary>
        [Fact]
        public void BuildingTransferMarks_DoesNotTouchPartFSpaceData()
        {
            PartFPlanModel model = Pair(63.0);

            Space space = model.Space("Bedroom");
            space.SetValue(SpaceParameter.PartFLocalExtractMethod, "unchanged-part-f-marker");
            model.AdjacencyCluster.AddObject(space);

            Assert.NotNull(TransferMark(DesignAirFlowFloorPlanOverlay.Build(model.AdjacencyCluster, plane)));

            Assert.Equal("unchanged-part-f-marker", model.Space("Bedroom").GetValue<string>(SpaceParameter.PartFLocalExtractMethod));
        }

        /// <summary>Switched off, no transfer mark is built at all - so a hidden mark costs no sectioning.</summary>
        [Fact]
        public void ShowTransferOff_BuildsNoTransferMarks()
        {
            PartFPlanModel model = Pair(63.0);

            DesignAirFlowFloorPlanOverlay overlay = DesignAirFlowFloorPlanOverlay.Build(model.AdjacencyCluster, plane, showNet: false, showTransfer: false);

            Assert.Empty(overlay.Marks.Where(x => x.IsTransfer));
        }

        /// <summary>
        /// A design air movement between two spaces the model does not show as adjoining cannot be drawn -
        /// there is no route to draw it on. It is REPORTED rather than dropped in silence, because the
        /// simulation will still move that air.
        /// </summary>
        [Fact]
        public void AMovementAcrossNoPartition_IsReportedRatherThanDropped()
        {
            PartFPlanModel model = new PartFPlanModel()
                .Room("Bedroom", 8)
                .Room("Hall", 4)
                .Room("Store", 3)
                .Partition("Bedroom", "Hall", "D01")
                .Zone("Flat 1", "Flats", true, "Bedroom", "Hall", "Store");

            //Bedroom and Store share no partition at all.
            AddTransfer(model.AdjacencyCluster, model.Space("Bedroom"), model.Space("Store"), 20.0, "Orphaned leg");

            DesignAirFlowFloorPlanOverlay overlay = DesignAirFlowFloorPlanOverlay.Build(model.AdjacencyCluster, plane);

            Assert.Empty(overlay.Marks.Where(x => x.IsTransfer));
            Assert.Contains(overlay.Unplaced, x => x.Contains("Orphaned leg", StringComparison.Ordinal));
        }

        /// <summary>
        /// A transfer route arriving at a room belongs to that room's marks too - it is a statement about
        /// the air the room receives as much as about the room it came from.
        /// </summary>
        [Fact]
        public void MarksOf_IncludesATransferRouteArrivingAtTheSpace()
        {
            PartFPlanModel model = Pair(63.0);

            DesignAirFlowFloorPlanOverlay overlay = DesignAirFlowFloorPlanOverlay.Build(model.AdjacencyCluster, plane);

            List<DesignAirFlowOverlayMark> marks = overlay.MarksOf(model.Space("Hall").Guid);

            Assert.Contains(marks, x => x.IsTransfer);
        }
    }
}
