// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using SAM.Geometry.Object;
using SAM.Geometry.Planar;
using SAM.Geometry.Spatial;
using SAM.Geometry.UI;
using SAM.Geometry.UI.WPF;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// <see cref="DesignAirFlowFloorPlanOverlayTests"/>'s own scaling benchmark measures
    /// <see cref="DesignAirFlowFloorPlanOverlay.Build"/> alone. It does not exercise the renderer path this
    /// PR added on top of it - <see cref="DesignAirFlowRenderer.Load"/>, <c>Place</c>,
    /// <c>DesignAirFlowRenderer.LimitArea</c> and <see cref="PartFTagPlacement.Solve"/> - so a quadratic
    /// regression introduced anywhere in that chain would pass unnoticed.
    /// <para>
    /// <b>The defect this guards against, exactly.</b> <c>DesignAirFlowRenderer.LimitArea</c> used to
    /// resolve each mark's space with <c>adjacencyCluster.GetSpaces()?.Find(x =&gt; x.Guid == guid_Space)</c>
    /// - a scan of the WHOLE space list. The per-layout dictionary above it removes the repeat lookups for
    /// a second or third mark in the SAME space, but the FIRST lookup of every new space still walked the
    /// list, which is quadratic in the number of spaces. It now uses
    /// <see cref="AdjacencyCluster.GetObject{T}(Guid)"/> - the cluster's own O(1) lookup, the same fix
    /// <c>PartOFailingSpaceLookupScalingTests</c> made for the equivalent Part O defect - see that file for
    /// the established convention this one follows: a CI-safe allocation-ratio regression, and a separate
    /// disposable wall-clock <see cref="Benchmark"/> that asserts nothing.
    /// </para>
    /// </summary>
    [Collection(WpfCollection.Name)]
    public class DesignAirFlowRendererScalingTests
    {
        private readonly ITestOutputHelper output;

        public DesignAirFlowRendererScalingTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        // -------------------------------------------------------------------------------------------------
        // CI-safe: the work is linear in the model, not a wall-clock bound
        // -------------------------------------------------------------------------------------------------

        /// <summary>
        /// Doubling the model roughly doubles what <see cref="DesignAirFlowRenderer.Load"/> allocates, up to
        /// and including 2,000 spaces - measured as a growth RATIO rather than an absolute time, matching
        /// <c>DesignAirFlowFloorPlanOverlayTests.Build_AllocatesLinearlyWithTheModel_UpToTwoThousandSpaces</c>
        /// and <c>PartOFailingSpaceLookupScalingTests.SelectingTheRoundsTargets_AllocatesLinearlyWithTheBlock</c>.
        /// A ratio close to the input's own growth factor (2) is machine-independent in a way an absolute
        /// wall-clock assertion in CI is not; quadratic work would show up as roughly the SQUARE of that
        /// factor (4) regardless of how fast or slow the machine is.
        /// </summary>
        [WpfFact]
        public void Load_AllocatesLinearlyWithTheModel_UpToTwoThousandSpaces()
        {
            int[] counts = [500, 1000, 2000];

            List<long> allocated = [];

            foreach (int count in counts)
            {
                (FloorPlan2DControl control, AdjacencyCluster adjacencyCluster) = Build(count);

                DesignAirFlowRenderer designAirFlowRenderer = new(control) { ViewSettings = new DesignAirFlowViewSettings { Enabled = true, ShowSupply = true } };

                //Warmed, so the first size measured is not paying for the JIT of every method below it.
                designAirFlowRenderer.Load(adjacencyCluster);

                allocated.Add(Allocated(() => designAirFlowRenderer.Load(adjacencyCluster)));
            }

            for (int i = 0; i < counts.Length; i++)
            {
                output.WriteLine("spaces={0,5}  Load/Place={1,14:N0} bytes", counts[i], allocated[i]);
            }

            for (int i = 1; i < counts.Length; i++)
            {
                double ratio = (double)allocated[i] / allocated[i - 1];

                output.WriteLine("{0} -> {1}: x{2:0.00}", counts[i - 1], counts[i], ratio);

                Assert.True(
                    ratio < 2.6,
                    string.Format("Doubling the model from {0} to {1} spaces multiplied DesignAirFlowRenderer.Load's allocation by {2:0.00}. Linear work sits near 2 and quadratic work near 4, so something in Load/Place/LimitArea is scanning the whole model per space again.", counts[i - 1], counts[i], ratio));
            }
        }

        // -------------------------------------------------------------------------------------------------
        // Disposable local evidence - not asserted, not part of the pass/fail gate
        // -------------------------------------------------------------------------------------------------

        /// <summary>
        /// Local wall clock for the FULL renderer path - <c>Load</c> through <c>Place</c>,
        /// <c>LimitArea</c> and <see cref="PartFTagPlacement.Solve"/> - at 2,000 and 5,000 spaces, for the
        /// report. Asserts nothing about time; see
        /// <see cref="Load_AllocatesLinearlyWithTheModel_UpToTwoThousandSpaces"/> for the CI-safe version of
        /// this measurement.
        /// </summary>
        [WpfFact]
        [Trait("Category", "Benchmark")]
        public void Benchmark()
        {
            output.WriteLine("{0,6} {1,14} {2,8} {3,12} {4,14}", "spaces", "Load/Place (ms)", "marks", "placed", "all placed?");

            long previousElapsed = 0;

            foreach (int count in new[] { 2000, 5000 })
            {
                (FloorPlan2DControl control, AdjacencyCluster adjacencyCluster) = Build(count);

                DesignAirFlowRenderer designAirFlowRenderer = new(control) { ViewSettings = new DesignAirFlowViewSettings { Enabled = true, ShowSupply = true } };

                //Warmed, so the first size measured is not paying for the JIT.
                designAirFlowRenderer.Load(adjacencyCluster);

                Stopwatch stopwatch = Stopwatch.StartNew();
                designAirFlowRenderer.Load(adjacencyCluster);
                stopwatch.Stop();

                int markCount = designAirFlowRenderer.Marks.Count;
                int placedCount = designAirFlowRenderer.Marks.FindAll(x => designAirFlowRenderer.Placement(x) is not null).Count;
                bool allPlaced = placedCount == markCount && designAirFlowRenderer.Unplaced.Count == 0;

                output.WriteLine("{0,6} {1,14:0.0} {2,8} {3,12} {4,14}", count, stopwatch.Elapsed.TotalMilliseconds, markCount, placedCount, allPlaced);

                long elapsed = stopwatch.ElapsedMilliseconds;

                if (previousElapsed > 0)
                {
                    double ratio = elapsed > 0 && previousElapsed > 0 ? (double)elapsed / previousElapsed : double.NaN;

                    output.WriteLine("scaling ratio 2000 -> 5000: x{0:0.00} (model itself grows x2.5)", ratio);
                }

                previousElapsed = elapsed;
            }
        }

        // -------------------------------------------------------------------------------------------------
        // Fixture
        // -------------------------------------------------------------------------------------------------

        /// <summary>
        /// <paramref name="roomCount"/> fully independent closed rooms, spread out along X so none adjoin -
        /// each with its own floor, roof and four walls, and its own single design supply terminal.
        /// Matches <c>DesignAirFlowFloorPlanOverlayTests.LargeModel</c>, duplicated here rather than shared
        /// so this file stays self-contained; built directly rather than through <c>PartFPlanModel</c> for
        /// the same reason that file gives - its <c>Room</c>/<c>Partition</c> pair is for a shared-wall
        /// block and carries its own O(n) fixture-side list scan, which would confound what this measures.
        /// </summary>
        private static (FloorPlan2DControl Control, AdjacencyCluster AdjacencyCluster) Build(int roomCount)
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

            Plane plane = Geometry.Spatial.Create.Plane(1.2);

            FloorPlan2DControl control = new();

            GeometryObjectModel geometryObjectModel = new();
            geometryObjectModel.SetValue(GeometryObjectModelParameter.ViewSettings, new TwoDimensionalViewSettings(
                Guid.NewGuid(), "Level 0 [1.2m]", plane, null, [typeof(Space)], Geometry.Object.Query.DefaultTextAppearance(), null));

            control.Load(geometryObjectModel);

            return (control, adjacencyCluster);
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
    }
}
