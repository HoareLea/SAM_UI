// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical;
using SAM.Analytical.Enums;
using SAM.Analytical.UI.WPF.Tests.Helpers;
using SAM.Geometry.Planar;
using SAM.Geometry.Spatial;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// The airflow overlay must sit on the REAL building. These tests run it against a fixture with
    /// actual 3D geometry and assert coordinates, not just counts.
    /// <para>
    /// The failure this guards against is the one that made the previous view unusable: marks placed by a
    /// layout algorithm of the overlay's own, so that arrows crossed rooms that do not adjoin and no
    /// arrow was anywhere near the door it claimed to cross. A test that only counted marks would have
    /// passed throughout.
    /// </para>
    /// <para>
    /// Deliberately headless. Correctness belongs in a test that runs on every build, not in a screenshot
    /// somebody has to look at.
    /// </para>
    /// </summary>
    public class PartFFloorPlanOverlayTests
    {
        private const double tolerance = 1e-6;

        /// <summary>The plan is cut at 1.2 m, the same height the assessment window uses.</summary>
        private static readonly Plane plane = Geometry.Spatial.Create.Plane(1.2);

        // ------------------------------------------------------------------
        // Flat 1 - a studio and a bathroom, one door
        // ------------------------------------------------------------------

        /// <summary>
        /// A studio is a habitable room AND holds the cooking function, so it carries both a supply
        /// terminal and its own local kitchen extract, and both must appear on the plan. Representing one
        /// role per space is what previously left the kitchen extract undrawn.
        /// </summary>
        [Fact]
        public void Flat1_Studio_ShowsBothSupplyAndLocalKitchenExtract()
        {
            PartFFloorPlanOverlay overlay = Flat1(out PartFPlanModel model, out _);

            Guid guid = model.Space("Studio").Guid;

            List<PartFOverlayMark> marks = [.. overlay.Marks.Where(x => !x.IsTransfer && x.SpaceGuid == guid)];

            Assert.Contains(marks, x => x.AirType == PartFAirflowAppearance.AirType.Supply);
            Assert.Contains(marks, x => x.AirType == PartFAirflowAppearance.AirType.LocalKitchenExtract);
            Assert.Equal(2, marks.Count);

            //Both inside the studio's own outline, and not on top of one another.
            Assert.All(marks, x => Assert.True(Inside(model, "Studio", x.Start)));
            Assert.NotEqual(marks[0].Start.Y, marks[1].Start.Y, 6);
        }

        /// <summary>The bathroom takes general extract, inside the bathroom.</summary>
        [Fact]
        public void Flat1_Bathroom_ShowsGeneralExtract()
        {
            PartFFloorPlanOverlay overlay = Flat1(out PartFPlanModel model, out _);

            PartFOverlayMark mark = Assert.Single(overlay.Marks.Where(x => !x.IsTransfer && x.SpaceGuid == model.Space("Bathroom").Guid));

            Assert.Equal(PartFAirflowAppearance.AirType.GeneralExtract, mark.AirType);
            Assert.True(Inside(model, "Bathroom", mark.Start));
        }

        /// <summary>
        /// The transfer mark sits ON the door, not between the two room centres. This is the assertion
        /// that the whole change exists for.
        /// </summary>
        [Fact]
        public void Flat1_Transfer_IsOnTheActualDoor_NotBetweenRoomCentres()
        {
            PartFFloorPlanOverlay overlay = Flat1(out PartFPlanModel model, out _);

            PartFOverlayMark mark = Assert.Single(overlay.Marks.Where(x => x.IsTransfer));

            Assert.True(mark.IsDoorRepresented);
            Assert.Equal(model.ApertureGuid("D01"), mark.ApertureGuid);

            //The arrow's midpoint is the door's own centre, projected onto the plan.
            Point2D point2D_Door = Geometry.Spatial.Query.Convert(plane, model.DoorCentroid("D01"));
            Point2D point2D_Mid = new((mark.Start.X + mark.End.X) / 2, (mark.Start.Y + mark.End.Y) / 2);

            Assert.Equal(point2D_Door.X, point2D_Mid.X, 6);
            Assert.Equal(point2D_Door.Y, point2D_Mid.Y, 6);

            //And explicitly NOT the midpoint between the two room anchors, which is the fallback this
            //replaced. The rooms are 8 m and 4 m wide, so their centres are 6 m apart and the door is on
            //the wall between them - the two answers are far apart, and the door is the right one.
            Point2D point2D_Studio = Anchor(overlay, model, "Studio");
            Point2D point2D_Bathroom = Anchor(overlay, model, "Bathroom");

            Assert.NotEqual((point2D_Studio.X + point2D_Bathroom.X) / 2, point2D_Mid.X, 3);
        }

        /// <summary>
        /// A route through a modelled door whose undercut nobody has recorded is flagged - absence of
        /// evidence is never compliance - but its OPENING is established, and the plan says so.
        /// <para>
        /// This is the distinction the whole opening-status axis exists for, and it is easy to lose. The
        /// mark is styled as an open question either way; what must differ is the question being asked.
        /// Here it is "what is the undercut?", and the label carries no "?" and no caption. Where there
        /// is no opening at all, the question is "where does the air go?", and both appear.
        /// </para>
        /// </summary>
        [Fact]
        public void Flat1_Transfer_ThroughAModelledDoor_HasAnEstablishedOpening()
        {
            PartFFloorPlanOverlay overlay = Flat1(out _, out _);

            PartFOverlayMark mark = Assert.Single(overlay.Marks.Where(x => x.IsTransfer));

            Assert.Equal(PartFTransferOpeningStatus.CalculatedViaModelledDoor, mark.OpeningStatus);

            //The opening is established, so nothing marks it as an unanswered route.
            Assert.DoesNotContain("?", mark.Label);
            Assert.Null(mark.Caption);

            //The undercut is still unrecorded, so paragraph 1.25 remains open on it.
            Assert.Equal(PartFComplianceStatus.CannotBeDetermined, mark.Status);
            Assert.True(mark.IsUnresolved);
        }

        /// <summary>Nothing was left unplaced: every space reached the plan and every route found its wall.</summary>
        [Fact]
        public void Flat1_PlacesEveryMark()
        {
            Assert.Empty(Flat1(out _, out _).Unplaced);
        }

        // ------------------------------------------------------------------
        // Flat 2 - bedroom, kitchen, ensuite, two doors in series
        // ------------------------------------------------------------------

        /// <summary>
        /// Two routes in series must use their OWN doors. A single wrong lookup here would put both
        /// arrows on the same opening, which reads as a plan where the air never reaches the ensuite.
        /// </summary>
        [Fact]
        public void Flat2_EachTransfer_UsesItsOwnDoor()
        {
            PartFFloorPlanOverlay overlay = Flat2(out PartFPlanModel model, out _);

            List<PartFOverlayMark> marks = [.. overlay.Marks.Where(x => x.IsTransfer)];

            Assert.Equal(2, marks.Count);

            PartFOverlayMark mark_1 = Assert.Single(marks.Where(x => x.SpaceGuid == model.Space("Bedroom").Guid));
            PartFOverlayMark mark_2 = Assert.Single(marks.Where(x => x.SpaceGuid == model.Space("Kitchen").Guid));

            Assert.Equal(model.ApertureGuid("D01"), mark_1.ApertureGuid);
            Assert.Equal(model.ApertureGuid("D02"), mark_2.ApertureGuid);

            Assert.NotEqual(mark_1.ApertureGuid, mark_2.ApertureGuid);

            Assert.Equal(model.Space("Kitchen").Guid, mark_1.DownstreamSpaceGuid);
            Assert.Equal(model.Space("Ensuite").Guid, mark_2.DownstreamSpaceGuid);
        }

        /// <summary>Each transfer mark is centred on its own door's real geometry.</summary>
        [Theory]
        [InlineData("Bedroom", "D01")]
        [InlineData("Kitchen", "D02")]
        public void Flat2_TransferMark_SitsOnItsDoorGeometry(string name_Upstream, string name_Door)
        {
            PartFFloorPlanOverlay overlay = Flat2(out PartFPlanModel model, out _);

            PartFOverlayMark mark = Assert.Single(overlay.Marks.Where(x => x.IsTransfer && x.SpaceGuid == model.Space(name_Upstream).Guid));

            Point2D point2D_Door = Geometry.Spatial.Query.Convert(plane, model.DoorCentroid(name_Door));

            Assert.Equal(point2D_Door.X, (mark.Start.X + mark.End.X) / 2, 6);
            Assert.Equal(point2D_Door.Y, (mark.Start.Y + mark.End.Y) / 2, 6);
        }

        /// <summary>
        /// The three terminal roles land in the three right rooms: supply in the bedroom, local kitchen
        /// extract in the kitchen, general extract in the ensuite.
        /// </summary>
        [Fact]
        public void Flat2_TerminalMarks_AreInTheRightRooms()
        {
            PartFFloorPlanOverlay overlay = Flat2(out PartFPlanModel model, out _);

            Assert.Equal(PartFAirflowAppearance.AirType.Supply, Terminal(overlay, model, "Bedroom").AirType);
            Assert.Equal(PartFAirflowAppearance.AirType.LocalKitchenExtract, Terminal(overlay, model, "Kitchen").AirType);
            Assert.Equal(PartFAirflowAppearance.AirType.GeneralExtract, Terminal(overlay, model, "Ensuite").AirType);

            Assert.True(Inside(model, "Bedroom", Terminal(overlay, model, "Bedroom").Start));
            Assert.True(Inside(model, "Kitchen", Terminal(overlay, model, "Kitchen").Start));
            Assert.True(Inside(model, "Ensuite", Terminal(overlay, model, "Ensuite").Start));
        }

        /// <summary>
        /// The labels carry the calculated rates, and they are the SAME numbers the text schematic prints.
        /// Two views of one assessment must never be able to disagree about a value.
        /// </summary>
        [Fact]
        public void Flat2_Labels_MatchTheCalculatedRates()
        {
            PartFFloorPlanOverlay overlay = Flat2(out PartFPlanModel model, out PartFComplianceResult complianceResult);

            foreach (PartFOverlayMark mark in overlay.Marks.Where(x => !x.IsTransfer))
            {
                PartFVentilationTerminalRequirement terminal = complianceResult.Terminals
                    .Find(x => x.SpaceGuid == mark.SpaceGuid && x.TerminalRole == mark.TerminalRole);

                Assert.Equal(PartFSchematic.Rate(terminal, PartFOperatingMode.ContinuousDesign), mark.FlowRate_Lps);
            }

            foreach (PartFOverlayMark mark in overlay.Marks.Where(x => x.IsTransfer))
            {
                PartFDoorTransferData partFDoorTransferData = complianceResult.TransferPaths
                    .Find(x => x.Name == mark.DoorName && x.UpstreamSpaceGuid == mark.SpaceGuid);

                Assert.Equal(PartFSchematic.Rate(partFDoorTransferData, PartFOperatingMode.ContinuousDesign), mark.FlowRate_Lps);
            }
        }

        /// <summary>
        /// Switching operating condition changes every value and moves nothing. The topology is not
        /// recomputed, which is both a correctness property and the reason the switch is instant.
        /// </summary>
        [Fact]
        public void Refresh_ChangesValuesAndMovesNothing()
        {
            PartFFloorPlanOverlay overlay = Flat2(out _, out PartFComplianceResult complianceResult);

            List<Point2D> point2Ds_Before = [.. overlay.Marks.Select(x => x.Start)];
            List<Guid> guids_Before = [.. overlay.Marks.Select(x => x.SpaceGuid)];

            overlay.Refresh(complianceResult, PartFOperatingMode.Setback);

            Assert.Equal(PartFOperatingMode.Setback, overlay.OperatingMode);

            //Same marks, same order, same coordinates.
            Assert.Equal(guids_Before, overlay.Marks.ConvertAll(x => x.SpaceGuid));

            for (int i = 0; i < point2Ds_Before.Count; i++)
            {
                Assert.Equal(point2Ds_Before[i].X, overlay.Marks[i].Start.X, tolerance);
                Assert.Equal(point2Ds_Before[i].Y, overlay.Marks[i].Start.Y, tolerance);
            }

            //And every value now reads at the new condition.
            foreach (PartFOverlayMark mark in overlay.Marks.Where(x => !x.IsTransfer))
            {
                PartFVentilationTerminalRequirement terminal = complianceResult.Terminals
                    .Find(x => x.SpaceGuid == mark.SpaceGuid && x.TerminalRole == mark.TerminalRole);

                Assert.Equal(PartFSchematic.Rate(terminal, PartFOperatingMode.Setback), mark.FlowRate_Lps);
            }
        }

        // ------------------------------------------------------------------
        // A route with no opening
        // ------------------------------------------------------------------

        /// <summary>
        /// The case the whole opening-status distinction exists for: an exact calculated flow across two
        /// rooms with no door and no recorded transfer device. It is drawn, because the engineer needs to
        /// see where the air would have to cross - and it is drawn as an open question.
        /// </summary>
        [Fact]
        public void MissingOpening_IsDrawnAsUnresolvedAndNeverAsConfirmed()
        {
            //Same flat, but the partition carries no door aperture.
            PartFPlanModel model = new PartFPlanModel()
                .Room("Studio", 8)
                .Room("Bathroom", 4)
                .Partition("Studio", "Bathroom")
                .Zone("Flat 1", "Flats", true, "Studio", "Bathroom");

            PartFFloorPlanOverlay overlay = Build(model, out PartFComplianceResult complianceResult);

            PartFOverlayMark mark = Assert.Single(overlay.Marks.Where(x => x.IsTransfer));

            //A flow WAS calculated - that is what makes this dangerous.
            Assert.NotNull(mark.FlowRate_Lps);

            Assert.Equal(PartFTransferOpeningStatus.MissingTransferOpening, mark.OpeningStatus);
            Assert.True(mark.IsUnresolved);
            Assert.False(mark.IsDoorRepresented);
            Assert.Equal(Guid.Empty, mark.ApertureGuid);

            //Said in the text as well as in the styling, so it survives a printout.
            Assert.Contains("?", mark.Label);
            Assert.Equal("No modelled transfer opening identified", mark.Caption);

            PartFDoorTransferData partFDoorTransferData = Assert.Single(complianceResult.TransferPaths.Where(x => x.IsInternalDwellingDoor));

            Assert.True(partFDoorTransferData.IsOpeningUnresolved);
            Assert.False(partFDoorTransferData.IsCompliant);
        }

        // ------------------------------------------------------------------
        // Boundaries
        // ------------------------------------------------------------------

        /// <summary>
        /// A communal corridor and a neighbouring flat get no Part F marks. An arrow reaching into either
        /// would claim the dwelling ventilates through space it does not own.
        /// </summary>
        [Fact]
        public void OtherDwellingsAndCommunalSpace_GetNoMarks()
        {
            PartFPlanModel model = new PartFPlanModel()
                .Room("Studio", 8)
                .Room("Bathroom", 4)
                .Room("Corridor", 3)
                .Room("Neighbour Bedroom", 6)
                .Partition("Studio", "Bathroom", "D01")
                .Partition("Bathroom", "Corridor", "D02")
                .Partition("Corridor", "Neighbour Bedroom", "D03")
                .Zone("Flat 1", "Flats", true, "Studio", "Bathroom")
                .Zone("Flat 2", "Flats", true, "Neighbour Bedroom")
                .LocalExtractMethod("Studio", PartFExtractMethod.MVHRContinuousTerminal);

            PartFFloorPlanOverlay overlay = Build(model, out _);

            HashSet<Guid> guids_Allowed = [model.Space("Studio").Guid, model.Space("Bathroom").Guid];

            Assert.All(overlay.Marks, x => Assert.Contains(x.SpaceGuid, guids_Allowed));
            Assert.All(overlay.Marks.Where(y => y.IsTransfer), x => Assert.Contains(x.DownstreamSpaceGuid, guids_Allowed));

            //And in particular nothing crosses the corridor door or reaches the neighbour.
            Assert.DoesNotContain(overlay.Marks, x => x.ApertureGuid == model.ApertureGuid("D02"));
            Assert.DoesNotContain(overlay.Marks, x => x.ApertureGuid == model.ApertureGuid("D03"));
        }

        /// <summary>
        /// Every transfer mark is on an internal route within one dwelling. An external door carries no
        /// Part F internal transfer requirement and can never become one.
        /// </summary>
        [Fact]
        public void EveryTransferMark_IsInternalToOneDwelling()
        {
            PartFFloorPlanOverlay overlay = Flat2(out PartFPlanModel model, out PartFComplianceResult complianceResult);

            HashSet<Guid> guids = [.. complianceResult.Terminals.Select(x => x.SpaceGuid)];

            Assert.All(overlay.Marks.Where(x => x.IsTransfer), x =>
            {
                Assert.Contains(x.SpaceGuid, guids);
                Assert.Contains(x.DownstreamSpaceGuid, guids);
            });
        }

        // ------------------------------------------------------------------
        // Awkward geometry
        // ------------------------------------------------------------------

        /// <summary>
        /// An L-shaped room's centroid falls outside the room. The anchor must use the outline's internal
        /// point instead, or every mark in an L-shaped living room lands in the corridor next door.
        /// </summary>
        [Fact]
        public void LShapedSpace_AnchorsInsideItsOwnOutline()
        {
            PartFPlanModel model = new PartFPlanModel()
                .LRoom("Studio", 8)
                .Room("Bathroom", 4)
                .Partition("Studio", "Bathroom", "D01")
                .Zone("Flat 1", "Flats", true, "Studio", "Bathroom")
                .LocalExtractMethod("Studio", PartFExtractMethod.MVHRContinuousTerminal);

            PartFFloorPlanOverlay overlay = Build(model, out _);

            List<PartFOverlayMark> marks = [.. overlay.Marks.Where(x => !x.IsTransfer && x.SpaceGuid == model.Space("Studio").Guid)];

            Assert.NotEmpty(marks);
            Assert.All(marks, x => Assert.True(Inside(model, "Studio", x.Start), string.Format("A {0} mark at ({1:0.###}, {2:0.###}) fell outside the L-shaped room's own outline.", x.AirType, x.Start.X, x.Start.Y)));
        }

        // ------------------------------------------------------------------
        // Terminal marks are positions, not trajectories
        // ------------------------------------------------------------------

        /// <summary>
        /// A terminal mark carries no length: it is a point and a direction, and the view draws a short
        /// stub from it. Only transfer marks span real distance, because only they connect two spaces.
        /// </summary>
        [Fact]
        public void TerminalMarks_HaveNoWorldLength_AndTransferMarksDo()
        {
            PartFFloorPlanOverlay overlay = Flat2(out _, out _);

            Assert.All(overlay.Marks.Where(x => !x.IsTransfer), x =>
            {
                Assert.Equal(x.Start.X, x.End.X, tolerance);
                Assert.Equal(x.Start.Y, x.End.Y, tolerance);
                Assert.True(x.Direction.Length > 0);
            });

            Assert.All(overlay.Marks.Where(x => x.IsTransfer), x =>
                Assert.True(new Vector2D(x.End.X - x.Start.X, x.End.Y - x.Start.Y).Length > 0));
        }

        // ------------------------------------------------------------------
        // Fixtures
        // ------------------------------------------------------------------

        /// <summary>Flat 1: an 8 m studio and a 4 m bathroom, one door between them.</summary>
        private static PartFFloorPlanOverlay Flat1(out PartFPlanModel model, out PartFComplianceResult complianceResult)
        {
            model = new PartFPlanModel()
                .Room("Studio", 8)
                .Room("Bathroom", 4)
                .Partition("Studio", "Bathroom", "D01")
                .Zone("Flat 1", "Flats", true, "Studio", "Bathroom")
                .LocalExtractMethod("Studio", PartFExtractMethod.MVHRContinuousTerminal);

            return Build(model, out complianceResult);
        }

        /// <summary>Flat 2: bedroom, kitchen and ensuite in a row, two doors in series.</summary>
        private static PartFFloorPlanOverlay Flat2(out PartFPlanModel model, out PartFComplianceResult complianceResult)
        {
            model = new PartFPlanModel()
                .Room("Bedroom", 8)
                .Room("Kitchen", 5)
                .Room("Ensuite", 3)
                .Partition("Bedroom", "Kitchen", "D01")
                .Partition("Kitchen", "Ensuite", "D02")
                .Zone("Flat 2", "Flats", true, "Bedroom", "Kitchen", "Ensuite")
                .LocalExtractMethod("Kitchen", PartFExtractMethod.MVHRContinuousTerminal);

            return Build(model, out complianceResult);
        }

        private static PartFFloorPlanOverlay Build(PartFPlanModel model, out PartFComplianceResult complianceResult)
        {
            //The SHIPPED rule set, not a stub. The overlay's job is to place what the real calculation
            //produces, so a fixture with invented categories would be testing something else.
            PartFCalculator partFCalculator = new(Analytical.Create.PartFData(RuleSetPath())) { AdjacencyCluster = model.AdjacencyCluster };

            Assert.True(partFCalculator.Calculate("Flats"));

            PartFDwellingResult dwellingResult = partFCalculator.DwellingResults[0];

            complianceResult = dwellingResult.ComplianceResult;

            return PartFFloorPlanOverlay.Build(model.AdjacencyCluster, complianceResult, plane);
        }

        /// <summary>
        /// The shipped Part F rule set, found relative to this repository rather than copied into the
        /// test output: a stale copy of a rule set is exactly the kind of drift these tests exist to
        /// catch elsewhere.
        /// </summary>
        private static string RuleSetPath()
        {
            System.IO.DirectoryInfo directoryInfo = new(AppDomain.CurrentDomain.BaseDirectory);

            while (directoryInfo is not null)
            {
                string path = System.IO.Path.Combine(directoryInfo.FullName, "SAM", "files", "resources", "Analytical", "SAM_PartFSpaceRulesUKDwellingsMVHR.json");
                if (System.IO.File.Exists(path))
                {
                    return path;
                }

                directoryInfo = directoryInfo.Parent;
            }

            throw new System.IO.FileNotFoundException("The shipped Part F rule set was not found above the test output directory.");
        }

        private static PartFOverlayMark Terminal(PartFFloorPlanOverlay overlay, PartFPlanModel model, string name_Space)
        {
            return Assert.Single(overlay.Marks.Where(x => !x.IsTransfer && x.SpaceGuid == model.Space(name_Space).Guid));
        }

        private static Point2D Anchor(PartFFloorPlanOverlay overlay, PartFPlanModel model, string name_Space)
        {
            return overlay.Marks.Find(x => !x.IsTransfer && x.SpaceGuid == model.Space(name_Space).Guid).Start;
        }

        /// <summary>True where a point lies within the named space's own outline on the plan.</summary>
        private static bool Inside(PartFPlanModel model, string name_Space, Point2D point2D)
        {
            foreach (Face2D face2D in model.AdjacencyCluster.SpaceSectionFace2Ds(model.Space(name_Space), plane) ?? [])
            {
                if (face2D is not null && face2D.Inside(point2D))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
