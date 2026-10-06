// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Geometry.Planar;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// <see cref="PartFTagPlacement.Lane"/>: the pure-geometry half of the Part F / Ventilation Design
    /// readability fix. A Part F tag's centre must stay ABOVE a space's shared reference row and a Design
    /// tag's centre must stay BELOW it, so the two authorities never land in the same visual band even
    /// though they anchor at the same point - see the renderer-level proof in
    /// <see cref="PartFDesignLaneTests"/> for the end-to-end contract this geometry makes possible.
    /// <para>
    /// Headless, matching <see cref="PartFTagPlacementTests"/>: this is a function of a shape and a number,
    /// with no WPF and no model involved.
    /// </para>
    /// </summary>
    public class PartFTagPlacementLaneTests
    {
        private static Face2D Room()
        {
            //An 8 x 5 m room, matching PartFPlanModel's own room footprint, with its own row at the
            //vertical mid-line.
            return new Face2D(new Rectangle2D(new Point2D(0, 0), 8, 5));
        }

        // ------------------------------------------------------------------
        // A room outline, clipped to its own half
        // ------------------------------------------------------------------

        [Fact]
        public void Lane_Above_KeepsOnlyThePartOfTheRoomAboveTheRow()
        {
            IClosed2D lane = PartFTagPlacement.Lane(Room(), row: 2.5, above: true);

            Assert.True(lane.Inside(new Point2D(4, 4)), "A point above the row, inside the room, must stay inside the Part F lane.");
            Assert.False(lane.Inside(new Point2D(4, 1)), "A point below the row must not be inside the Part F lane.");

            //Still bounded by the room itself in X - the lane narrows the room, it does not widen it.
            Assert.False(lane.Inside(new Point2D(20, 4)), "The lane must not extend beyond the room's own outline.");
        }

        [Fact]
        public void Lane_Below_KeepsOnlyThePartOfTheRoomBelowTheRow()
        {
            IClosed2D lane = PartFTagPlacement.Lane(Room(), row: 2.5, above: false);

            Assert.True(lane.Inside(new Point2D(4, 1)), "A point below the row, inside the room, must stay inside the Design lane.");
            Assert.False(lane.Inside(new Point2D(4, 4)), "A point above the row must not be inside the Design lane.");
        }

        /// <summary>The two lanes of the same room, at the same row, never both claim the same point.</summary>
        [Fact]
        public void Lane_AboveAndBelow_NeverOverlap()
        {
            Face2D room = Room();

            IClosed2D lane_Above = PartFTagPlacement.Lane(room, row: 2.5, above: true);
            IClosed2D lane_Below = PartFTagPlacement.Lane(room, row: 2.5, above: false);

            for (double y = 0.1; y < 5; y += 0.3)
            {
                Point2D point2D = new(4, y);

                Assert.False(lane_Above.Inside(point2D) && lane_Below.Inside(point2D),
                    string.Format("Point at Y={0:0.0} was inside BOTH lanes.", y));
            }
        }

        // ------------------------------------------------------------------
        // A transfer tag: no outline to clip, the half-plane alone
        // ------------------------------------------------------------------

        /// <summary>
        /// A transfer mark's <c>LimitArea</c> is null - it belongs to no single room - so the lane is the
        /// bare half-plane, not clipped against anything.
        /// </summary>
        [Fact]
        public void Lane_NullLimitArea_ReturnsTheBareHalfPlane()
        {
            IClosed2D lane_Above = PartFTagPlacement.Lane(null, row: 0, above: true);
            IClosed2D lane_Below = PartFTagPlacement.Lane(null, row: 0, above: false);

            Assert.True(lane_Above.Inside(new Point2D(1000, 5)));
            Assert.False(lane_Above.Inside(new Point2D(1000, -5)));

            Assert.True(lane_Below.Inside(new Point2D(1000, -5)));
            Assert.False(lane_Below.Inside(new Point2D(1000, 5)));
        }

        // ------------------------------------------------------------------
        // The harder rule survives: a room the row cuts off entirely
        // ------------------------------------------------------------------

        /// <summary>
        /// A row set far outside the room leaves nothing on the requested side - fewer than three points
        /// survive the clip - so the lane falls back to the UNCLIPPED outline rather than an empty region.
        /// The room boundary (never reading as the room next door's) is the harder rule and must not be
        /// given up just to keep a lane that this room's own geometry cannot support.
        /// </summary>
        [Fact]
        public void Lane_RowEntirelyOutsideTheRoom_FallsBackToTheWholeOutline()
        {
            Face2D room = Room();

            IClosed2D lane = PartFTagPlacement.Lane(room, row: 1000, above: true);

            //Still a real, usable region - the room itself - not null and not empty.
            Assert.NotNull(lane);
            Assert.True(lane.Inside(new Point2D(4, 2.5)));
            Assert.Equal(room.GetArea(), lane.GetArea(), 6);
        }
    }
}
