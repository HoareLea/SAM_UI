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
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// The Part F tag placement adapter. It has one job - hand the tags to the shared
    /// <see cref="Solver2D"/> in the right order, with the manual ones as obstacles, and hand back what it
    /// decided - so these tests assert exactly that, plus the two things a drawing depends on: that the
    /// same input always produces the same layout, and that a caller can always tell a placement that was
    /// solved from one that was not.
    /// <para>
    /// Headless. Placement correctness has to be checked on every build, not in a screenshot.
    /// </para>
    /// </summary>
    public class PartFTagPlacementTests
    {
        /// <summary>The plan is cut at 1.2 m, the same height the assessment window uses.</summary>
        private static readonly Plane plane = Geometry.Spatial.Create.Plane(1.2);

        // ------------------------------------------------------------------
        // The priority policy
        // ------------------------------------------------------------------

        /// <summary>
        /// The policy order, asserted rather than trusted: transfer air, then kitchen extract, then general
        /// extract, then supply. It is a policy precisely so that it can be checked here instead of being
        /// spread through a renderer as numbers.
        /// </summary>
        [Fact]
        public void Priority_FollowsThePolicyOrder()
        {
            Assert.True(PartFTagPlacement.Priority(Mark(PartFAirflowAppearance.AirType.TransferAir)) < PartFTagPlacement.Priority(Mark(PartFAirflowAppearance.AirType.LocalKitchenExtract)));
            Assert.True(PartFTagPlacement.Priority(Mark(PartFAirflowAppearance.AirType.LocalKitchenExtract)) < PartFTagPlacement.Priority(Mark(PartFAirflowAppearance.AirType.GeneralExtract)));
            Assert.True(PartFTagPlacement.Priority(Mark(PartFAirflowAppearance.AirType.GeneralExtract)) < PartFTagPlacement.Priority(Mark(PartFAirflowAppearance.AirType.Supply)));
            Assert.True(PartFTagPlacement.Priority(Mark(PartFAirflowAppearance.AirType.Supply)) < PartFTagPriority.Diagnostic);
        }

        /// <summary>
        /// A transfer route the model gives no established opening for must NOT be placed after an ordinary
        /// valid terminal label. It is the mark that says the dwelling's air path is unresolved, so it is
        /// the one that most needs to stay next to the partition it concerns and to stay legible.
        /// </summary>
        [Fact]
        public void Priority_UnresolvedTransfer_IsNeverBehindAValidTerminalLabel()
        {
            PartFOverlayMark mark_Unresolved = Mark(PartFAirflowAppearance.AirType.TransferAir);
            mark_Unresolved.IsUnresolved = true;
            mark_Unresolved.OpeningStatus = PartFTransferOpeningStatus.MissingTransferOpening;

            PartFTagPriority priority_Unresolved = PartFTagPlacement.Priority(mark_Unresolved);

            Assert.Equal(PartFTagPriority.TransferAir, priority_Unresolved);

            foreach (PartFAirflowAppearance.AirType airType in new[] { PartFAirflowAppearance.AirType.Supply, PartFAirflowAppearance.AirType.GeneralExtract, PartFAirflowAppearance.AirType.LocalKitchenExtract })
            {
                Assert.True(priority_Unresolved <= PartFTagPlacement.Priority(Mark(airType)));
            }
        }

        // ------------------------------------------------------------------
        // Automatic placement
        // ------------------------------------------------------------------

        /// <summary>
        /// Two tags on one anchor come apart. This is the failure the first screenshots showed - a studio's
        /// supply drawn on top of its kitchen extract - and the reason the placement engine was adopted.
        /// </summary>
        [Fact]
        public void Solve_TagsOnOneAnchor_AreSeparated()
        {
            List<PartFTagPlacementResult> results = PartFTagPlacement.Solve(
            [
                Item(new Point2D(0, 0), PartFTagPriority.Supply),
                Item(new Point2D(0, 0), PartFTagPriority.KitchenExtract),
                Item(new Point2D(0, 0), PartFTagPriority.Extract),
            ]);

            Assert.Equal(3, results.Count);
            Assert.All(results, x => Assert.Equal(Solver2DResultType.Solved, x.ResultType));

            AssertNoOverlap(results);
        }

        /// <summary>
        /// The higher-priority tag keeps the FIRST-choice position - the one the engine tries first, directly
        /// above the mark - and the lower-priority one is the one that has to go somewhere else. Whichever
        /// order they were handed over in: priority decides, not the caller's collection.
        /// <para>
        /// Note that both end up the same DISTANCE from the anchor, because the engine offsets radially and
        /// the second tag simply takes another of the eight directions at the same radius. Being displaced is
        /// about which direction you are sent in, not about being pushed further out.
        /// </para>
        /// </summary>
        [Fact]
        public void Solve_HigherPriorityTag_KeepsTheFirstChoicePosition()
        {
            PartFTagPlacementItem item_Supply = Item(new Point2D(0, 0), PartFTagPriority.Supply);
            PartFTagPlacementItem item_Kitchen = Item(new Point2D(0, 0), PartFTagPriority.KitchenExtract);

            //Supply offered first on purpose: the order tags are supplied in must not decide the layout.
            List<PartFTagPlacementResult> results = PartFTagPlacement.Solve([item_Supply, item_Kitchen]);

            Point2D point2D_Kitchen = results.Find(x => x.Item == item_Kitchen).Rectangle2D.GetCentroid();
            Point2D point2D_Supply = results.Find(x => x.Item == item_Supply).Rectangle2D.GetCentroid();

            //Directly above the mark: the position the engine offers first.
            Assert.Equal(0, point2D_Kitchen.X, 6);
            Assert.True(point2D_Kitchen.Y > 0);

            //And the supply tag is not there.
            Assert.True(point2D_Supply.Distance(point2D_Kitchen) > 0.1);
        }

        /// <summary>
        /// A tag whose centre cannot stay inside its room is kept on the drawing at its anchor and reported
        /// as unplaced - never silently dropped. A Part F tag carries a flow rate and a compliance status,
        /// and a rate that disappears because the plan was crowded is a regulatory figure lost from the
        /// drawing. The status is how a caller knows not to trust the position.
        /// </summary>
        [Fact]
        public void Solve_UnplaceableTag_IsKeptAtItsAnchorAndSaysSo()
        {
            PartFTagPlacementItem item = Item(new Point2D(0, 0), PartFTagPriority.Supply);

            //A limit area 40 m away, which no candidate position can reach.
            item.LimitArea = new Rectangle2D(new Point2D(40, 40), 1, 1);

            PartFTagPlacementResult result = Assert.Single(PartFTagPlacement.Solve([item]));

            Assert.Equal(Solver2DResultType.Unplaced, result.ResultType);
            Assert.True(result.IsOverlapPossible);

            //Still on the drawing, and on its own mark.
            Assert.NotNull(result.Rectangle2D);
            Assert.Equal(0, result.Rectangle2D.GetCentroid().X, 6);
            Assert.Equal(0, result.Rectangle2D.GetCentroid().Y, 6);
        }

        /// <summary>A solved tag says so, and says the position can be relied on.</summary>
        [Fact]
        public void Solve_PlacedTag_IsNotReportedAsPossiblyOverlapping()
        {
            PartFTagPlacementResult result = Assert.Single(PartFTagPlacement.Solve([Item(new Point2D(0, 0), PartFTagPriority.Supply)]));

            Assert.Equal(Solver2DResultType.Solved, result.ResultType);
            Assert.False(result.IsOverlapPossible);
            Assert.False(result.IsUserPositioned);
        }

        // ------------------------------------------------------------------
        // Manual tags as obstacles
        // ------------------------------------------------------------------

        /// <summary>
        /// A tag somebody moved by hand is entered into the solve as an OBSTACLE, not left out of it. Left
        /// out - which is all the Mollier chart does with a moved label - an automatic tag can be placed
        /// straight on top of it, and the work of tidying the drawing is undone by the next redraw.
        /// </summary>
        [Fact]
        public void Solve_ManualTag_IsAnObstacleAndIsNotOverlapped()
        {
            PartFTagPlacementItem item_Manual = Item(new Point2D(0, 0), PartFTagPriority.KitchenExtract);
            PartFTagPlacementItem item_Automatic = Item(new Point2D(0, 0), PartFTagPriority.Supply);

            //Moved by hand to just above the anchor - exactly where the automatic tag would otherwise go.
            List<PartFAnnotationOverride> partFAnnotationOverrides =
            [
                new PartFAnnotationOverride(item_Manual.ObjectGuid, item_Manual.AnnotationType, new Point2D(0, 0.2)),
            ];

            List<PartFTagPlacementResult> results = PartFTagPlacement.Solve([item_Manual, item_Automatic], partFAnnotationOverrides);

            PartFTagPlacementResult result_Manual = results.Find(x => x.Item == item_Manual);
            PartFTagPlacementResult result_Automatic = results.Find(x => x.Item == item_Automatic);

            //The manual tag is where the person put it, and is not reported as something the solver approved.
            Assert.True(result_Manual.IsUserPositioned);
            Assert.Equal(Solver2DResultType.Undefined, result_Manual.ResultType);
            Assert.Equal(0, result_Manual.Rectangle2D.GetCentroid().X, 6);
            Assert.Equal(0.2, result_Manual.Rectangle2D.GetCentroid().Y, 6);

            //And the automatic one went somewhere else.
            Assert.False(result_Automatic.IsUserPositioned);
            Assert.Equal(Solver2DResultType.Solved, result_Automatic.ResultType);
            Assert.False(result_Automatic.Rectangle2D.InRange(result_Manual.Rectangle2D));
            Assert.False(result_Manual.Rectangle2D.InRange(result_Automatic.Rectangle2D));
        }

        /// <summary>
        /// A manual position never changes what the tag reports on: the anchor is a statement about where
        /// the terminal is, and moving its label is a statement about the drawing.
        /// </summary>
        [Fact]
        public void Solve_ManualTag_LeavesTheEngineeringAnchorAlone()
        {
            PartFTagPlacementItem item = Item(new Point2D(3, 4), PartFTagPriority.Supply);

            List<PartFAnnotationOverride> partFAnnotationOverrides =
            [
                new PartFAnnotationOverride(item.ObjectGuid, item.AnnotationType, new Point2D(-9, -9)),
            ];

            PartFTagPlacementResult result = Assert.Single(PartFTagPlacement.Solve([item], partFAnnotationOverrides));

            Assert.Equal(3, result.Item.Anchor2D.X, 9);
            Assert.Equal(4, result.Item.Anchor2D.Y, 9);
        }

        /// <summary>
        /// An override for something not on this plan is ignored and never pruned. The object may simply be
        /// on another level or filtered out, and a position discarded because a tag was briefly absent is a
        /// person's work discarded.
        /// </summary>
        [Fact]
        public void Solve_StaleOverride_IsIgnoredRatherThanApplied()
        {
            PartFTagPlacementItem item = Item(new Point2D(0, 0), PartFTagPriority.Supply);

            List<PartFAnnotationOverride> partFAnnotationOverrides =
            [
                new PartFAnnotationOverride(Guid.NewGuid(), PartFAnnotationType.Terminal, new Point2D(50, 50)),
            ];

            PartFTagPlacementResult result = Assert.Single(PartFTagPlacement.Solve([item], partFAnnotationOverrides));

            Assert.False(result.IsUserPositioned);
            Assert.Equal(Solver2DResultType.Solved, result.ResultType);
            Assert.True(result.Rectangle2D.GetCentroid().Distance(new Point2D(50, 50)) > 1);
        }

        // ------------------------------------------------------------------
        // Obstacles the plan itself drew
        // ------------------------------------------------------------------

        /// <summary>
        /// The space names the plan draws are obstacles too, so a Part F tag is not placed over the room
        /// name it sits beside.
        /// </summary>
        [Fact]
        public void Solve_TagIsPlacedClearOfTheSpaceNames()
        {
            Rectangle2D rectangle2D_SpaceName = new(new Point2D(-1, 0.05), 2, 0.3);

            PartFTagPlacementResult result = Assert.Single(PartFTagPlacement.Solve([Item(new Point2D(0, 0), PartFTagPriority.Supply)], null, [rectangle2D_SpaceName]));

            Assert.Equal(Solver2DResultType.Solved, result.ResultType);
            Assert.False(rectangle2D_SpaceName.InRange(result.Rectangle2D));
        }

        // ------------------------------------------------------------------
        // Determinism
        // ------------------------------------------------------------------

        /// <summary>
        /// The same tags always produce the same layout, and the order they are handed over in does not
        /// change it. A saved drawing whose tags move when it is reopened is not a saved drawing, and the
        /// order a caller's collection enumerates in is not something a drawing may depend on.
        /// </summary>
        [Fact]
        public void Solve_IdenticalInput_ReturnsIdenticalPlacementWhateverTheOrderSupplied()
        {
            List<PartFTagPlacementItem> items = Items();

            List<PartFTagPlacementResult> results_1 = PartFTagPlacement.Solve(items);
            List<PartFTagPlacementResult> results_2 = PartFTagPlacement.Solve(items);

            //Reversed, and handed over through a set rather than a list, which is exactly the sort of thing
            //that silently reorders.
            List<PartFTagPlacementResult> results_3 = PartFTagPlacement.Solve(Enumerable.Reverse(items));
            List<PartFTagPlacementResult> results_4 = PartFTagPlacement.Solve(new HashSet<PartFTagPlacementItem>(items));

            AssertSamePlacement(results_1, results_2);
            AssertSamePlacement(results_1, results_3);
            AssertSamePlacement(results_1, results_4);
        }

        // ------------------------------------------------------------------
        // Annotation scale, and the camera's complete absence from placement
        // ------------------------------------------------------------------

        /// <summary>
        /// The conversion from measured text to building units is a function of the annotation scale and
        /// nothing else - no display metrics, no window size, no view transform. 1:50 is 96 / 0.0254 / 50.
        /// </summary>
        [Fact]
        public void PixelsPerMetre_DependsOnTheAnnotationScaleAlone()
        {
            Assert.Equal(96 / 0.0254 / 50, PartFTagPlacement.PixelsPerMetre(50), 9);
            Assert.Equal(PartFTagPlacement.PixelsPerMetre(50) / 2, PartFTagPlacement.PixelsPerMetre(100), 9);

            //A nonsense scale falls back to the default rather than dividing by zero.
            Assert.Equal(PartFTagPlacement.PixelsPerMetre(PartFTagPlacement.DefaultAnnotationScale), PartFTagPlacement.PixelsPerMetre(0), 9);
            Assert.Equal(PartFTagPlacement.PixelsPerMetre(PartFTagPlacement.DefaultAnnotationScale), PartFTagPlacement.PixelsPerMetre(-5), 9);
        }

        /// <summary>
        /// The annotation scale IS a layout input: a tag is a fixed size on the sheet, so at 1:100 it covers
        /// twice as much building as at 1:50 and the layout legitimately differs. This is the counterpart to
        /// the tests below - the scale changes the layout precisely so that the camera does not have to.
        /// </summary>
        [Fact]
        public void Solve_DifferentAnnotationScale_LaysTagsOutDifferently()
        {
            List<PartFTagPlacementResult> results_50 = PartFTagPlacement.Solve(Items(50));
            List<PartFTagPlacementResult> results_100 = PartFTagPlacement.Solve(Items(100));

            Assert.Equal(results_50.Count, results_100.Count);
            Assert.True(results_100[0].Rectangle2D.Width > results_50[0].Rectangle2D.Width);
        }

        /// <summary>
        /// The same tags at the same annotation scale lay out identically however many times they are solved,
        /// so nothing a viewer does between two redraws can move them.
        /// </summary>
        [Fact]
        public void Solve_SameAnnotationScale_LaysTagsOutIdentically()
        {
            AssertSamePlacement(PartFTagPlacement.Solve(Items(50)), PartFTagPlacement.Solve(Items(50)));
        }

        /// <summary>
        /// <b>The camera cannot reach the placement.</b> Asserted structurally, by reading the compiled body
        /// of <c>PartFAirflowRenderer.Place</c> and everything it calls, and proving that the view transform -
        /// <c>FloorPlan2DControl.WorldToScreen</c> - is never among them. The renderer is the single Part F
        /// renderer, so this one assertion covers the assessment window and the saved 2D view alike.
        /// <para>
        /// Structural rather than "zoom twice and compare", and deliberately so: a runtime test samples two
        /// zoom levels and this proves the dependency does not exist at any of them. The layout is a function
        /// of the model and the annotation scale, and a regression would have to reintroduce the viewport
        /// into that function - which is exactly what this fails on. It is also what the previous revision of
        /// this code did, so the regression is a real one and not a hypothetical.
        /// </para>
        /// </summary>
        [Fact]
        public void Place_NeverReadsTheViewTransform()
        {
            List<MethodInfo> methodInfos = Reachable(typeof(PartFAirflowRenderer).GetMethod("Place", BindingFlags.Instance | BindingFlags.Public));

            //Positive control first, so this cannot pass by the walker finding nothing at all: it must be
            //seeing the placement call, and - the tight part - it must be seeing a property read on the very
            //control whose transform the assertion below is about.
            Assert.Contains(typeof(PartFTagPlacement).GetMethod("Solve"), methodInfos);
            Assert.Contains(typeof(Geometry.UI.WPF.FloorPlan2DControl).GetProperty("Plane").GetGetMethod(), methodInfos);

            //And the transform itself is not reachable, at any depth, so no zoom can reach the layout.
            Assert.DoesNotContain(typeof(Geometry.UI.WPF.FloorPlan2DControl).GetProperty("WorldToScreen").GetGetMethod(), methodInfos);
        }

        /// <summary>
        /// A camera move redraws and does not place. Panning and zooming an engineering drawing must not
        /// rearrange its annotation, and the handler is where that could quietly be reintroduced.
        /// </summary>
        [Fact]
        public void FloorPlan_ViewChanged_RedrawsWithoutPlacing()
        {
            MethodInfo methodInfo_Place = typeof(PartFAirflowRenderer).GetMethod("Place", BindingFlags.Instance | BindingFlags.Public);
            MethodInfo methodInfo_Draw = typeof(PartFAirflowRenderer).GetMethod("Draw", BindingFlags.Instance | BindingFlags.Public);

            List<MethodInfo> methodInfos = Called(typeof(PartFAirflowRenderer).GetMethod("FloorPlan2DControl_ViewChanged", BindingFlags.Instance | BindingFlags.NonPublic));

            Assert.DoesNotContain(methodInfo_Place, methodInfos);
            Assert.Contains(methodInfo_Draw, methodInfos);
        }

        /// <summary>
        /// And the list of things that DO lay the tags out again is exactly the agreed one: loading the
        /// dwellings, a change of operating condition, and a change of view settings - which is how a
        /// visibility toggle, the annotation scale and a manual position all arrive. Written as an assertion
        /// because "which events re-solve" is the architecture, and it is the kind of thing that grows an
        /// extra caller by accident.
        /// </summary>
        [Fact]
        public void Place_IsCalledOnlyByTheAgreedInputChanges()
        {
            MethodInfo methodInfo_Place = typeof(PartFAirflowRenderer).GetMethod("Place", BindingFlags.Instance | BindingFlags.Public);

            List<string> names = [];

            foreach (MethodInfo methodInfo in typeof(PartFAirflowRenderer).GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                if (methodInfo != methodInfo_Place && Called(methodInfo).Contains(methodInfo_Place))
                {
                    names.Add(methodInfo.Name);
                }
            }

            names.Sort(StringComparer.Ordinal);

            Assert.Equal(new List<string> { "Load", "Refresh", "set_ViewSettings" }, names);
        }

        // ------------------------------------------------------------------
        // Leaders
        // ------------------------------------------------------------------

        /// <summary>
        /// A displaced tag gets a leader from the engineering anchor to the nearest point on the tag, and
        /// deriving it does not touch the anchor. <see cref="Point2D"/> is mutable, so a result that handed
        /// out its own anchor would let a renderer move where a terminal is by drawing a line.
        /// </summary>
        [Fact]
        public void Leader2D_RunsFromTheAnchorToTheTagAndLeavesTheAnchorAlone()
        {
            //Two tags on one anchor: the second has to move, so it needs a leader.
            List<PartFTagPlacementResult> results = PartFTagPlacement.Solve(
            [
                Item(new Point2D(0, 0), PartFTagPriority.KitchenExtract),
                Item(new Point2D(0, 0), PartFTagPriority.Supply),
            ]);

            PartFTagPlacementResult result = results.Find(x => x.Item.Priority == PartFTagPriority.Supply);

            Segment2D segment2D = result.Leader2D();

            Assert.NotNull(segment2D);
            Assert.Equal(0, segment2D[0].X, 6);
            Assert.Equal(0, segment2D[0].Y, 6);

            //It ends on the tag, not somewhere near it.
            Assert.True(result.Rectangle2D.InRange(segment2D[1]));

            //Moving the leader must not move the terminal.
            segment2D[0].Move(new Vector2D(100, 100));

            Assert.Equal(0, result.Item.Anchor2D.X, 9);
            Assert.Equal(0, result.Item.Anchor2D.Y, 9);
        }

        /// <summary>A tag still covering its own anchor needs no leader; a line inside a box is noise.</summary>
        [Fact]
        public void Leader2D_TagOverItsOwnAnchor_HasNone()
        {
            PartFTagPlacementItem item = Item(new Point2D(0, 0), PartFTagPriority.Supply);

            List<PartFAnnotationOverride> partFAnnotationOverrides =
            [
                new PartFAnnotationOverride(item.ObjectGuid, item.AnnotationType, new Point2D(0, 0)),
            ];

            PartFTagPlacementResult result = Assert.Single(PartFTagPlacement.Solve([item], partFAnnotationOverrides));

            Assert.Null(result.Leader2D());
        }

        // ------------------------------------------------------------------
        // A real flat
        // ------------------------------------------------------------------

        /// <summary>
        /// End to end on a real dwelling: the shipped rule set, real 3D geometry, the overlay's real marks.
        /// Every tag comes out placed, clear of the others, and inside the room it reports on.
        /// </summary>
        [Fact]
        public void Solve_RealFlat_PlacesEveryTagClearOfTheOthersAndInsideItsOwnRoom()
        {
            PartFPlanModel model = new PartFPlanModel()
                .Room("Bedroom", 8)
                .Room("Kitchen", 5)
                .Room("Ensuite", 3)
                .Partition("Bedroom", "Kitchen", "D01")
                .Partition("Kitchen", "Ensuite", "D02")
                .Zone("Flat 2", "Flats", true, "Bedroom", "Kitchen", "Ensuite")
                .LocalExtractMethod("Kitchen", PartFExtractMethod.MVHRContinuousTerminal);

            PartFCalculator partFCalculator = new(Analytical.Create.PartFData(RuleSetPath())) { AdjacencyCluster = model.AdjacencyCluster };

            Assert.True(partFCalculator.Calculate("Flats"));

            PartFComplianceResult partFComplianceResult = partFCalculator.DwellingResults[0].ComplianceResult;

            PartFFloorPlanOverlay partFFloorPlanOverlay = PartFFloorPlanOverlay.Build(model.AdjacencyCluster, partFComplianceResult, plane);

            List<PartFTagPlacementItem> items = [];

            foreach (PartFOverlayMark mark in partFFloorPlanOverlay.Marks)
            {
                //Sizes as the window measures them, converted to metres at a plausible plan scale.
                items.Add(new PartFTagPlacementItem()
                {
                    ObjectGuid = mark.AnnotationGuid,
                    AnnotationType = mark.AnnotationType,
                    Priority = PartFTagPlacement.Priority(mark),
                    Anchor2D = mark.End,
                    Width = 1.4,
                    Height = 0.25,
                    LimitArea = mark.IsTransfer ? null : LimitArea(model, mark.SpaceGuid),
                    Tag = mark,
                });
            }

            //A terminal in each of the three rooms, a local kitchen extract as well, and two routes.
            Assert.True(items.Count >= 5);

            List<PartFTagPlacementResult> results = PartFTagPlacement.Solve(items);

            Assert.Equal(items.Count, results.Count);
            Assert.All(results, x => Assert.Equal(Solver2DResultType.Solved, x.ResultType));

            AssertNoOverlap(results);

            //Every terminal tag's centre is inside the room it reports on, so it cannot read as the room
            //next door's.
            foreach (PartFTagPlacementResult result in results)
            {
                if (result.Item.Tag is not PartFOverlayMark mark || mark.IsTransfer)
                {
                    continue;
                }

                Assert.True(result.Item.LimitArea.Inside(result.Rectangle2D.GetCentroid()), string.Format("The {0} tag in {1} left its own room.", mark.AirType, mark.SpaceName));
            }
        }

        // ------------------------------------------------------------------
        // The temporary placer is gone
        // ------------------------------------------------------------------

        /// <summary>
        /// The assessment window's own label-nudging loop - <c>Place(Rect, List&lt;Rect&gt;)</c>, which
        /// stepped a label down and then up in fixed pixel increments - must not exist any more, and neither
        /// must any other placement in the window: it now has NO placement code at all and delegates to the
        /// shared renderer. Two placement paths behind different code entries is how a drawing starts to
        /// depend on which one ran.
        /// </summary>
        [Fact]
        public void PartFAssessmentWindow_HasNoPlacementOfItsOwn()
        {
            Assert.Empty(typeof(PartFAssessmentWindow)
                .GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                .Where(x => string.Equals(x.Name, "Place", StringComparison.Ordinal) || string.Equals(x.Name, "DrawMarks", StringComparison.Ordinal)));
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        private static PartFOverlayMark Mark(PartFAirflowAppearance.AirType airType)
        {
            return new PartFOverlayMark()
            {
                AirType = airType,
                TerminalRole = airType switch
                {
                    PartFAirflowAppearance.AirType.Supply => PartFTerminalRole.Supply,
                    PartFAirflowAppearance.AirType.GeneralExtract => PartFTerminalRole.GeneralExtract,
                    PartFAirflowAppearance.AirType.LocalKitchenExtract => PartFTerminalRole.LocalKitchenExtract,
                    _ => PartFTerminalRole.Undefined,
                },
                Start = new Point2D(0, 0),
                End = new Point2D(0, 0),
            };
        }

        private static PartFTagPlacementItem Item(Point2D point2D, PartFTagPriority partFTagPriority)
        {
            return new PartFTagPlacementItem()
            {
                ObjectGuid = Guid.NewGuid(),
                AnnotationType = PartFAnnotationType.Terminal,
                Priority = partFTagPriority,
                Anchor2D = point2D,
                Width = 1.4,
                Height = 0.25,
            };
        }

        /// <summary>
        /// A crowded set with fixed guids, so a determinism assertion is about the placement and not about
        /// freshly generated identities.
        /// </summary>
        /// <param name="annotationScale">
        /// The drawing scale to size the tags for. A tag measures 110 by 18 pixels on the sheet, as a rate
        /// label does, and how much building that covers is what the scale decides.
        /// </param>
        private static List<PartFTagPlacementItem> Items(double annotationScale = 50)
        {
            List<PartFTagPlacementItem> result = [];

            double scale = PartFTagPlacement.PixelsPerMetre(annotationScale);

            PartFTagPriority[] partFTagPriorities = [PartFTagPriority.TransferAir, PartFTagPriority.KitchenExtract, PartFTagPriority.Extract, PartFTagPriority.Supply, PartFTagPriority.Supply, PartFTagPriority.Extract];

            for (int i = 0; i < partFTagPriorities.Length; i++)
            {
                PartFTagPlacementItem item = Item(new Point2D(i % 2, 0), partFTagPriorities[i]);

                item.ObjectGuid = new Guid(i + 1, 0, 0, [0, 0, 0, 0, 0, 0, 0, 0]);
                item.Width = 110 / scale;
                item.Height = 18 / scale;

                result.Add(item);
            }

            return result;
        }

        /// <summary>
        /// Every method the given method calls, read from its compiled body. Only calls made directly by it -
        /// see <see cref="Reachable"/> for the transitive set.
        /// <para>
        /// Walking the intermediate language rather than the source, because the question being asked is
        /// "can this code path reach the camera", and only the compiled body answers that without a running
        /// user interface. Operand lengths come from <see cref="OpCode.OperandType"/>, so there is no
        /// hand-written opcode table to fall out of date.
        /// </para>
        /// </summary>
        private static List<MethodInfo> Called(MethodInfo methodInfo)
        {
            List<MethodInfo> result = [];

            byte[] il = methodInfo?.GetMethodBody()?.GetILAsByteArray();
            if (il is null)
            {
                return result;
            }

            Module module = methodInfo.Module;

            int i = 0;
            while (i < il.Length)
            {
                short value = il[i] == 0xFE && i + 1 < il.Length ? (short)(0xFE00 | il[i + 1]) : il[i];

                if (!opCodes.TryGetValue(value, out OpCode opCode))
                {
                    //An opcode this build of the runtime does not know: stop rather than misread the rest.
                    break;
                }

                i += opCode.Size;

                if (opCode.OperandType is OperandType.InlineMethod or OperandType.InlineTok && i + 4 <= il.Length)
                {
                    int token = BitConverter.ToInt32(il, i);

                    try
                    {
                        if (module.ResolveMember(token, methodInfo.DeclaringType?.GetGenericArguments(), null) is MethodInfo methodInfo_Called && !result.Contains(methodInfo_Called))
                        {
                            result.Add(methodInfo_Called);
                        }
                    }
                    catch
                    {
                        //Not a member token this module can resolve - a constructor or a type. Not a call for
                        //the purposes of this question.
                    }
                }

                i += Length(opCode, il, i);
            }

            return result;
        }

        /// <summary>
        /// Every method reachable from the given one WITHIN the assemblies this question is about - the
        /// window and the shared user-interface libraries - so a dependency hidden one call deeper is still
        /// found. Bounded to those assemblies so it does not walk the whole framework.
        /// </summary>
        private static List<MethodInfo> Reachable(MethodInfo methodInfo)
        {
            List<MethodInfo> result = [];

            Queue<MethodInfo> queue = new([methodInfo]);
            HashSet<MethodInfo> seen = [methodInfo];

            while (queue.Count != 0)
            {
                foreach (MethodInfo methodInfo_Called in Called(queue.Dequeue()))
                {
                    if (!result.Contains(methodInfo_Called))
                    {
                        result.Add(methodInfo_Called);
                    }

                    //Followed only into SAM's own user-interface assemblies: the framework cannot reach the
                    //view transform, and walking into it would take all day.
                    string name = methodInfo_Called.DeclaringType?.Assembly.GetName().Name;

                    if (name is not null && name.StartsWith("SAM.", StringComparison.Ordinal) && seen.Add(methodInfo_Called))
                    {
                        queue.Enqueue(methodInfo_Called);
                    }
                }
            }

            return result;
        }

        /// <summary>Operand length in bytes, from the opcode's own operand type.</summary>
        private static int Length(OpCode opCode, byte[] il, int i)
        {
            switch (opCode.OperandType)
            {
                case OperandType.InlineNone:
                    return 0;

                case OperandType.ShortInlineBrTarget:
                case OperandType.ShortInlineI:
                case OperandType.ShortInlineVar:
                    return 1;

                case OperandType.InlineVar:
                    return 2;

                case OperandType.InlineBrTarget:
                case OperandType.InlineField:
                case OperandType.InlineI:
                case OperandType.InlineMethod:
                case OperandType.InlineSig:
                case OperandType.InlineString:
                case OperandType.InlineTok:
                case OperandType.InlineType:
                case OperandType.ShortInlineR:
                    return 4;

                case OperandType.InlineI8:
                case OperandType.InlineR:
                    return 8;

                case OperandType.InlineSwitch:
                    //A jump table: a count, then that many four-byte targets.
                    return 4 + (4 * BitConverter.ToInt32(il, i));

                default:
                    return 0;
            }
        }

        private static readonly Dictionary<short, OpCode> opCodes = OpCodes();

        private static Dictionary<short, OpCode> OpCodes()
        {
            Dictionary<short, OpCode> result = [];

            foreach (FieldInfo fieldInfo in typeof(System.Reflection.Emit.OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (fieldInfo.GetValue(null) is OpCode opCode)
                {
                    result[opCode.Value] = opCode;
                }
            }

            return result;
        }

        private static IClosed2D LimitArea(PartFPlanModel model, Guid guid_Space)
        {
            Space space = model.AdjacencyCluster.GetSpaces()?.Find(x => x is not null && x.Guid == guid_Space);

            return space is null
                ? null
                : model.AdjacencyCluster.SpaceSectionFace2Ds(space, plane)?.Where(x => x is not null).OrderByDescending(x => x.GetArea()).FirstOrDefault();
        }

        private static void AssertNoOverlap(List<PartFTagPlacementResult> results)
        {
            for (int i = 0; i < results.Count; i++)
            {
                for (int j = i + 1; j < results.Count; j++)
                {
                    Rectangle2D rectangle2D_1 = results[i].Rectangle2D;
                    Rectangle2D rectangle2D_2 = results[j].Rectangle2D;

                    Assert.False(rectangle2D_1.InRange(rectangle2D_2) || rectangle2D_2.InRange(rectangle2D_1), string.Format("Tags {0} and {1} overlap.", i, j));
                }
            }
        }

        private static void AssertSamePlacement(List<PartFTagPlacementResult> results_1, List<PartFTagPlacementResult> results_2)
        {
            Assert.Equal(results_1.Count, results_2.Count);

            for (int i = 0; i < results_1.Count; i++)
            {
                Assert.Equal(results_1[i].ObjectGuid, results_2[i].ObjectGuid);
                Assert.Equal(results_1[i].AnnotationType, results_2[i].AnnotationType);
                Assert.Equal(results_1[i].ResultType, results_2[i].ResultType);

                Assert.Equal(results_1[i].Rectangle2D.Origin.X, results_2[i].Rectangle2D.Origin.X, 9);
                Assert.Equal(results_1[i].Rectangle2D.Origin.Y, results_2[i].Rectangle2D.Origin.Y, 9);
            }
        }

        /// <summary>
        /// The shipped Part F rule set, found relative to this repository rather than copied into the test
        /// output: a stale copy of a rule set is exactly the kind of drift these tests exist to catch.
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
    }
}
