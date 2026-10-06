// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical;
using SAM.Analytical.UI.WPF.Tests.Helpers;
using SAM.Geometry.Planar;
using SAM.Geometry.Spatial;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// <see cref="DesignAirFlowFloorPlanOverlay"/> reads a completely different authority from
    /// <see cref="PartFFloorPlanOverlay"/> - <c>VentilationTerminal.DesignFlowRate_Lps</c> through
    /// <c>Query.VentilationTerminalDesignDuty_Lps</c>, not a <c>PartFComplianceResult</c> - so these tests
    /// build their own fixtures with real ventilation terminals rather than a Part F assessment.
    /// <para>
    /// Reuses <see cref="PartFPlanModel"/> for the geometry (real rooms on a real plane, which is what the
    /// overlay sections to find an anchor) while adding terminals directly, since a design duty is
    /// deliberately unrelated to any Part F requirement.
    /// </para>
    /// </summary>
    public class DesignAirFlowFloorPlanOverlayTests
    {
        private static readonly Plane plane = Geometry.Spatial.Create.Plane(1.2);

        private readonly Xunit.Abstractions.ITestOutputHelper output;

        public DesignAirFlowFloorPlanOverlayTests(Xunit.Abstractions.ITestOutputHelper output)
        {
            this.output = output;
        }

        private static VentilationTerminal AddTerminal(PartFPlanModel model, string spaceName, FlowClassification flowClassification, double? designFlowRate_Lps, string name = null)
        {
            Space space = model.Space(spaceName);

            VentilationTerminal ventilationTerminal = new(name ?? string.Concat(spaceName, " ", flowClassification), flowClassification, designFlowRate_Lps);

            model.AdjacencyCluster.AddObject(ventilationTerminal);
            model.AdjacencyCluster.AddRelation(ventilationTerminal, space);

            return ventilationTerminal;
        }

        /// <summary>A studio with one supply terminal shows a single supply mark, inside the room.</summary>
        [Fact]
        public void OneSpace_OneSupplyTerminal_ShowsSupplyMark()
        {
            PartFPlanModel model = new PartFPlanModel().Room("Studio", 8).Close();

            AddTerminal(model, "Studio", FlowClassification.Supply, 45.0);

            DesignAirFlowFloorPlanOverlay overlay = DesignAirFlowFloorPlanOverlay.Build(model.AdjacencyCluster, plane);

            DesignAirFlowOverlayMark mark = Assert.Single(overlay.Marks);

            Assert.Equal(DesignAirFlowMarkType.Supply, mark.MarkType);
            Assert.Equal(45.0, mark.FlowRate_Lps);
            Assert.Equal(model.Space("Studio").Guid, mark.SpaceGuid);
        }

        /// <summary>A bathroom with one extract terminal shows a single extract mark, and no supply mark.</summary>
        [Fact]
        public void OneSpace_OneExtractTerminal_ShowsExtractMark()
        {
            PartFPlanModel model = new PartFPlanModel().Room("Bathroom", 3).Close();

            AddTerminal(model, "Bathroom", FlowClassification.Extract, 8.0);

            DesignAirFlowFloorPlanOverlay overlay = DesignAirFlowFloorPlanOverlay.Build(model.AdjacencyCluster, plane);

            DesignAirFlowOverlayMark mark = Assert.Single(overlay.Marks);

            Assert.Equal(DesignAirFlowMarkType.Extract, mark.MarkType);
            Assert.Equal(8.0, mark.FlowRate_Lps);
        }

        /// <summary>
        /// A studio with both a supply and an extract terminal shows both marks, fanned to distinct
        /// positions so neither sits on top of the other - matching <see cref="PartFFloorPlanOverlay"/>'s
        /// own convention for two marks in one space.
        /// </summary>
        [Fact]
        public void OneSpace_SupplyAndExtract_ShowsBothMarksAtDistinctPositions()
        {
            PartFPlanModel model = new PartFPlanModel().Room("Studio", 8).Close();

            AddTerminal(model, "Studio", FlowClassification.Supply, 30.0);
            AddTerminal(model, "Studio", FlowClassification.Extract, 22.0);

            DesignAirFlowFloorPlanOverlay overlay = DesignAirFlowFloorPlanOverlay.Build(model.AdjacencyCluster, plane);

            Assert.Equal(2, overlay.Marks.Count);

            DesignAirFlowOverlayMark mark_Supply = Assert.Single(overlay.Marks.Where(x => x.MarkType == DesignAirFlowMarkType.Supply));
            DesignAirFlowOverlayMark mark_Extract = Assert.Single(overlay.Marks.Where(x => x.MarkType == DesignAirFlowMarkType.Extract));

            Assert.Equal(30.0, mark_Supply.FlowRate_Lps);
            Assert.Equal(22.0, mark_Extract.FlowRate_Lps);

            Assert.NotEqual(mark_Supply.Position.Y, mark_Extract.Position.Y, 6);
            Assert.Equal(mark_Supply.Position.X, mark_Extract.Position.X, 6);
        }

        /// <summary>
        /// Two supply terminals in one space aggregate to their sum - the space's design duty is the sum
        /// of its terminals, never the count of them, matching <c>VentilationTerminal</c>'s own doc comment.
        /// </summary>
        [Fact]
        public void OneSpace_TwoSupplyTerminals_AggregateToTheirSum()
        {
            PartFPlanModel model = new PartFPlanModel().Room("Studio", 8).Close();

            AddTerminal(model, "Studio", FlowClassification.Supply, 20.0, "Supply A");
            AddTerminal(model, "Studio", FlowClassification.Supply, 25.0, "Supply B");

            DesignAirFlowFloorPlanOverlay overlay = DesignAirFlowFloorPlanOverlay.Build(model.AdjacencyCluster, plane);

            DesignAirFlowOverlayMark mark = Assert.Single(overlay.Marks);

            Assert.Equal(DesignAirFlowMarkType.Supply, mark.MarkType);
            Assert.Equal(45.0, mark.FlowRate_Lps);
        }

        /// <summary>
        /// A space with a supply terminal and no extract terminal gets NO extract mark at all - never a
        /// mark reading "0 l/s", which would claim the design has established an extract duty of zero.
        /// </summary>
        [Fact]
        public void NoTerminalInADirection_ProducesNoMark_NotAnInventedZero()
        {
            PartFPlanModel model = new PartFPlanModel().Room("Studio", 8).Close();

            AddTerminal(model, "Studio", FlowClassification.Supply, 30.0);

            DesignAirFlowFloorPlanOverlay overlay = DesignAirFlowFloorPlanOverlay.Build(model.AdjacencyCluster, plane);

            Assert.DoesNotContain(overlay.Marks, x => x.MarkType == DesignAirFlowMarkType.Extract);
            Assert.DoesNotContain(overlay.Marks, x => x.FlowRate_Lps == 0);
        }

        /// <summary>
        /// Net is built only where BOTH directions are established - a space with only an extract terminal
        /// gets no net mark, because the missing supply side is not a known zero.
        /// </summary>
        [Fact]
        public void Net_NotBuilt_WhenOnlyOneDirectionIsEstablished()
        {
            PartFPlanModel model = new PartFPlanModel().Room("Bathroom", 3).Close();

            AddTerminal(model, "Bathroom", FlowClassification.Extract, 8.0);

            DesignAirFlowFloorPlanOverlay overlay = DesignAirFlowFloorPlanOverlay.Build(model.AdjacencyCluster, plane, showNet: true);

            Assert.DoesNotContain(overlay.Marks, x => x.MarkType == DesignAirFlowMarkType.Net);
        }

        /// <summary>Net is supply minus extract, and positive means net supplied.</summary>
        [Fact]
        public void Net_IsSupplyMinusExtract()
        {
            PartFPlanModel model = new PartFPlanModel().Room("Studio", 8).Close();

            AddTerminal(model, "Studio", FlowClassification.Supply, 45.0);
            AddTerminal(model, "Studio", FlowClassification.Extract, 22.0);

            DesignAirFlowFloorPlanOverlay overlay = DesignAirFlowFloorPlanOverlay.Build(model.AdjacencyCluster, plane, showNet: true);

            DesignAirFlowOverlayMark mark_Net = Assert.Single(overlay.Marks.Where(x => x.MarkType == DesignAirFlowMarkType.Net));

            Assert.Equal(23.0, mark_Net.FlowRate_Lps);
        }

        /// <summary>
        /// Building the Ventilation Design overlay must not read or write a single Part F value - the two
        /// overlays are independent authorities and are meant to be able to disagree.
        /// </summary>
        [Fact]
        public void Build_DoesNotTouchPartFSpaceData()
        {
            PartFPlanModel model = new PartFPlanModel().Room("Studio", 8).Close();

            Space space = model.Space("Studio");

            //A Part F record, set independently of anything the design airflow overlay touches.
            space.SetValue(SpaceParameter.PartFLocalExtractMethod, "unchanged-part-f-marker");
            model.AdjacencyCluster.AddObject(space);

            AddTerminal(model, "Studio", FlowClassification.Supply, 150.0);

            DesignAirFlowFloorPlanOverlay.Build(model.AdjacencyCluster, plane);

            Assert.Equal("unchanged-part-f-marker", model.Space("Studio").GetValue<string>(SpaceParameter.PartFLocalExtractMethod));
        }

        /// <summary>
        /// Reopening a different model produces marks from THAT model only - nothing from the previous
        /// model's spaces or terminals survives, because <see cref="DesignAirFlowFloorPlanOverlay.Build"/>
        /// is a fresh, stateless factory call every time.
        /// </summary>
        [Fact]
        public void DifferentModel_ProducesIndependentMarks_NoStaleValues()
        {
            PartFPlanModel model_1 = new PartFPlanModel().Room("Studio", 8).Close();
            AddTerminal(model_1, "Studio", FlowClassification.Supply, 30.0);

            PartFPlanModel model_2 = new PartFPlanModel().Room("Bedroom", 4).Close();
            AddTerminal(model_2, "Bedroom", FlowClassification.Supply, 99.0);

            DesignAirFlowFloorPlanOverlay overlay_1 = DesignAirFlowFloorPlanOverlay.Build(model_1.AdjacencyCluster, plane);
            DesignAirFlowFloorPlanOverlay overlay_2 = DesignAirFlowFloorPlanOverlay.Build(model_2.AdjacencyCluster, plane);

            DesignAirFlowOverlayMark mark_1 = Assert.Single(overlay_1.Marks);
            DesignAirFlowOverlayMark mark_2 = Assert.Single(overlay_2.Marks);

            Assert.Equal(30.0, mark_1.FlowRate_Lps);
            Assert.Equal(99.0, mark_2.FlowRate_Lps);
            Assert.NotEqual(mark_1.SpaceGuid, mark_2.SpaceGuid);
        }

        // -------------------------------------------------------------------------------------------------
        // Scale - the work is linear in the model, not a wall-clock bound
        // -------------------------------------------------------------------------------------------------

        /// <summary>
        /// Doubling the model roughly doubles what building the overlay allocates, up to and including a
        /// 2,000-space model. Measured as a growth RATIO rather than an absolute time, matching the
        /// convention in <c>PartOFailingSpaceLookupScalingTests.SelectingTheRoundsTargets_AllocatesLinearlyWithTheBlock</c>:
        /// a machine-dependent wall-clock assertion is fragile in CI, where the same code can take
        /// different absolute time on different hardware, but a ratio close to the input's own growth
        /// factor is not - quadratic work would show up as roughly the SQUARE of that factor regardless of
        /// how fast or slow the machine is.
        /// </summary>
        [Fact]
        public void Build_AllocatesLinearlyWithTheModel_UpToTwoThousandSpaces()
        {
            int[] counts = [500, 1000, 2000];

            List<long> allocated = [];

            foreach (int count in counts)
            {
                AdjacencyCluster adjacencyCluster = LargeModel(count);

                //Warmed, so the first size measured is not paying for the JIT of every method below it.
                DesignAirFlowFloorPlanOverlay.Build(adjacencyCluster, plane);

                allocated.Add(Allocated(() => DesignAirFlowFloorPlanOverlay.Build(adjacencyCluster, plane)));
            }

            for (int i = 1; i < counts.Length; i++)
            {
                double ratio = (double)allocated[i] / allocated[i - 1];

                Assert.True(
                    ratio < 2.6,
                    string.Format("Doubling the model from {0} to {1} spaces multiplied Build's allocation by {2:0.00}. Linear work sits near 2 and quadratic work near 4, so something is scanning the whole model per space again.", counts[i - 1], counts[i], ratio));
            }
        }

        /// <summary>
        /// Confirms the shape of the model above is actually exercising the overlay: every space in a
        /// 2,000-space model produces its own mark, none unplaced.
        /// </summary>
        [Fact]
        public void Build_TwoThousandSpaces_EveryOneProducesAMark()
        {
            const int roomCount = 2000;

            AdjacencyCluster adjacencyCluster = LargeModel(roomCount);

            DesignAirFlowFloorPlanOverlay overlay = DesignAirFlowFloorPlanOverlay.Build(adjacencyCluster, plane);

            Assert.Equal(roomCount, overlay.Marks.Count);
            Assert.Empty(overlay.Unplaced);
        }

        /// <summary>
        /// Local wall clock at 2,000 and 5,000 spaces, for the report. Asserts nothing about time - see
        /// <see cref="Build_AllocatesLinearlyWithTheModel_UpToTwoThousandSpaces"/> for the CI-safe version
        /// of this measurement.
        /// </summary>
        [Fact]
        [Trait("Category", "Benchmark")]
        public void Benchmark()
        {
            output.WriteLine("{0,6} {1,14} {2,10}", "spaces", "Build (ms)", "marks");

            foreach (int count in new[] { 2000, 5000 })
            {
                AdjacencyCluster adjacencyCluster = LargeModel(count);

                //Warmed, so the first size measured is not paying for the JIT.
                DesignAirFlowFloorPlanOverlay.Build(adjacencyCluster, plane);

                Stopwatch stopwatch = Stopwatch.StartNew();
                DesignAirFlowFloorPlanOverlay overlay = DesignAirFlowFloorPlanOverlay.Build(adjacencyCluster, plane);
                stopwatch.Stop();

                output.WriteLine("{0,6} {1,14:0.0} {2,10}", count, stopwatch.Elapsed.TotalMilliseconds, overlay.Marks.Count);
            }
        }

        private static long Allocated(Action action)
        {
            //Warmed first, so the measurement is the work and not the JIT.
            action();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long before = GC.GetAllocatedBytesForCurrentThread();

            action();

            return GC.GetAllocatedBytesForCurrentThread() - before;
        }

        /// <summary>
        /// <paramref name="roomCount"/> fully independent closed rooms, spread out along X so none adjoin -
        /// each with its own floor, roof and four walls, and its own single design supply terminal. Built
        /// directly (not through <see cref="PartFPlanModel"/>, whose <c>Room</c>/<c>Partition</c> pair is
        /// for a shared-wall block and carries its own O(n) fixture-side list scan) so the only thing this
        /// measurement times is <see cref="DesignAirFlowFloorPlanOverlay.Build"/> itself.
        /// </summary>
        private static AdjacencyCluster LargeModel(int roomCount)
        {
            const double width_M = 3;
            const double depth_M = 5;
            const double height_M = 3;
            const double gap_M = 1;

            AdjacencyCluster adjacencyCluster = new();

            for (int i = 0; i < roomCount; i++)
            {
                double x0 = i * (width_M + gap_M);
                double x1 = x0 + width_M;

                string name = string.Format("Room {0:00000}", i);

                Space space = new(name, new Point3D((x0 + x1) / 2, depth_M / 2, height_M / 2));

                space.SetValue(SpaceParameter.Area, width_M * depth_M);
                space.SetValue(SpaceParameter.Volume, width_M * depth_M * height_M);

                adjacencyCluster.AddObject(space);

                AddBoxPanel(adjacencyCluster, space, Horizontal(x0, x1, depth_M, 0));
                AddBoxPanel(adjacencyCluster, space, Horizontal(x0, x1, depth_M, height_M));
                AddBoxPanel(adjacencyCluster, space, WallY(x0, x1, 0, height_M));
                AddBoxPanel(adjacencyCluster, space, WallY(x0, x1, depth_M, height_M));
                AddBoxPanel(adjacencyCluster, space, WallX(x0, depth_M, height_M));
                AddBoxPanel(adjacencyCluster, space, WallX(x1, depth_M, height_M));

                VentilationTerminal ventilationTerminal = new(name + " Supply", FlowClassification.Supply, 10 + (i % 50));

                adjacencyCluster.AddObject(ventilationTerminal);
                adjacencyCluster.AddRelation(ventilationTerminal, space);
            }

            return adjacencyCluster;
        }

        private static void AddBoxPanel(AdjacencyCluster adjacencyCluster, Space space, Face3D face3D)
        {
            Panel panel = Analytical.Create.Panel(new Construction(Guid.NewGuid(), "External Wall"), PanelType.WallExternal, face3D);

            adjacencyCluster.AddObject(panel);
            adjacencyCluster.AddRelation(space, panel);
        }

        private static Face3D Horizontal(double x0, double x1, double depth_M, double z)
        {
            return new Face3D(new Polygon3D(
            [
                new Point3D(x0, 0, z),
                new Point3D(x1, 0, z),
                new Point3D(x1, depth_M, z),
                new Point3D(x0, depth_M, z),
            ]));
        }

        private static Face3D WallY(double x0, double x1, double y, double height_M)
        {
            return new Face3D(new Polygon3D(
            [
                new Point3D(x0, y, 0),
                new Point3D(x1, y, 0),
                new Point3D(x1, y, height_M),
                new Point3D(x0, y, height_M),
            ]));
        }

        private static Face3D WallX(double x, double depth_M, double height_M)
        {
            return new Face3D(new Polygon3D(
            [
                new Point3D(x, 0, 0),
                new Point3D(x, depth_M, 0),
                new Point3D(x, depth_M, height_M),
                new Point3D(x, 0, height_M),
            ]));
        }
    }
}
