// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using SAM.Core;
using SAM.Geometry.Object;
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
    /// <see cref="DesignAirFlowRendererScalingTests"/> measures the renderer on a model of independent
    /// rooms, which carries no design transfer air at all - so it does not exercise the transfer path added
    /// on top of it: <c>DesignAirFlowFloorPlanOverlay.BuildTransferMarks</c>, the
    /// <c>PartFAirflowNetwork</c> topology it looks openings up in, and
    /// <see cref="TransferRouteGeometry"/>. A quadratic regression in any of those would pass unnoticed
    /// there.
    /// <para>
    /// <b>The defect shape this guards against.</b> Every step of the transfer path has a tempting
    /// whole-model scan in it - resolving a space from a guid, finding an aperture from a guid, finding the
    /// panels two rooms share. <see cref="TransferRouteGeometry"/> resolves spaces through
    /// <see cref="AdjacencyCluster.GetObject{T}(Guid)"/> and takes the aperture OBJECT the topology already
    /// found, rather than re-finding either by scanning; going back to a scan would make the work quadratic
    /// in the number of rooms. Same convention as the file above: a CI-safe allocation-ratio regression,
    /// and a separate disposable wall-clock <see cref="Benchmark"/> that asserts nothing.
    /// </para>
    /// </summary>
    [Collection(WpfCollection.Name)]
    public class DesignTransferScalingTests
    {
        private readonly ITestOutputHelper output;

        public DesignTransferScalingTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        // -------------------------------------------------------------------------------------------------
        // CI-safe: the work is linear in the model, not a wall-clock bound
        // -------------------------------------------------------------------------------------------------

        /// <summary>
        /// Doubling a chain of adjoining rooms - and so doubling both the transfer routes and the rooms any
        /// scan would walk - roughly doubles what <see cref="DesignAirFlowRenderer.Load"/> allocates.
        /// Measured as a growth RATIO rather than an absolute time, so it is machine-independent; quadratic
        /// work would show up near the SQUARE of the input's growth factor however fast the machine is.
        /// </summary>
        [WpfFact]
        public void LoadWithTransfer_AllocatesLinearlyWithTheModel()
        {
            int[] counts = [250, 500, 1000];

            List<long> allocated = [];

            foreach (int count in counts)
            {
                (FloorPlan2DControl control, AdjacencyCluster adjacencyCluster) = Build(count);

                DesignAirFlowRenderer designAirFlowRenderer = new(control)
                {
                    ViewSettings = new DesignAirFlowViewSettings { Enabled = true, ShowSupply = true, ShowTransfer = true },
                };

                //Warmed, so the first size measured is not paying for the JIT of every method below it.
                designAirFlowRenderer.Load(adjacencyCluster);

                Assert.NotEmpty(designAirFlowRenderer.Marks.FindAll(x => x.IsTransfer));

                allocated.Add(Allocated(() => designAirFlowRenderer.Load(adjacencyCluster)));
            }

            for (int i = 0; i < counts.Length; i++)
            {
                output.WriteLine("rooms={0,5}  Load/Place={1,14:N0} bytes", counts[i], allocated[i]);
            }

            for (int i = 1; i < counts.Length; i++)
            {
                double ratio = (double)allocated[i] / allocated[i - 1];

                output.WriteLine("{0} -> {1}: x{2:0.00}", counts[i - 1], counts[i], ratio);

                Assert.True(
                    ratio < 2.6,
                    string.Format("Doubling the chain from {0} to {1} rooms multiplied DesignAirFlowRenderer.Load's allocation by {2:0.00}. Linear work sits near 2 and quadratic work near 4, so something on the transfer path is scanning the whole model per route again.", counts[i - 1], counts[i], ratio));
            }
        }

        // -------------------------------------------------------------------------------------------------
        // Disposable local evidence - not asserted, not part of the pass/fail gate
        // -------------------------------------------------------------------------------------------------

        /// <summary>
        /// Local wall clock for the full transfer path at 1,000 and 2,500 adjoining rooms, for the report.
        /// Asserts nothing about time.
        /// </summary>
        [WpfFact]
        [Trait("Category", "Benchmark")]
        public void Benchmark()
        {
            output.WriteLine("{0,6} {1,16} {2,10} {3,10}", "rooms", "Load/Place (ms)", "marks", "transfer");

            foreach (int count in new[] { 1000, 2500 })
            {
                (FloorPlan2DControl control, AdjacencyCluster adjacencyCluster) = Build(count);

                DesignAirFlowRenderer designAirFlowRenderer = new(control)
                {
                    ViewSettings = new DesignAirFlowViewSettings { Enabled = true, ShowSupply = true, ShowTransfer = true },
                };

                designAirFlowRenderer.Load(adjacencyCluster);

                Stopwatch stopwatch = Stopwatch.StartNew();
                designAirFlowRenderer.Load(adjacencyCluster);
                stopwatch.Stop();

                output.WriteLine(
                    "{0,6} {1,16:0.0} {2,10} {3,10}",
                    count,
                    stopwatch.Elapsed.TotalMilliseconds,
                    designAirFlowRenderer.Marks.Count,
                    designAirFlowRenderer.Marks.FindAll(x => x.IsTransfer).Count);
            }
        }

        // -------------------------------------------------------------------------------------------------
        // Fixture
        // -------------------------------------------------------------------------------------------------

        /// <summary>
        /// A chain of <paramref name="roomCount"/> rooms in a row, each SHARING one partition panel with
        /// the next and carrying a design air movement into it - so the model has one transfer route per
        /// adjacency, which is the work being measured.
        /// <para>
        /// Built directly rather than through <c>PartFPlanModel</c>, for the reason
        /// <see cref="DesignAirFlowRendererScalingTests"/> gives for the same choice: that helper's
        /// <c>Partition</c> resolves each space by name with its own O(n) list scan, which would put a
        /// quadratic term in the FIXTURE and confound what this measures.
        /// </para>
        /// </summary>
        private static (FloorPlan2DControl Control, AdjacencyCluster AdjacencyCluster) Build(int roomCount)
        {
            const double width_M = 3;
            const double depth_M = 5;
            const double height_M = 3;

            AdjacencyCluster adjacencyCluster = new();

            List<Space> spaces = [];

            for (int i = 0; i < roomCount; i++)
            {
                double x0 = i * width_M;
                double x1 = x0 + width_M;

                Space space = new(string.Format("Room {0:00000}", i), new Point3D((x0 + x1) / 2, depth_M / 2, height_M / 2));

                space.SetValue(SpaceParameter.Area, width_M * depth_M);
                space.SetValue(SpaceParameter.Volume, width_M * depth_M * height_M);

                adjacencyCluster.AddObject(space);
                spaces.Add(space);

                AddPanel(adjacencyCluster, space, PanelType.Floor, Horizontal(x0, x1, 0, depth_M));
                AddPanel(adjacencyCluster, space, PanelType.Roof, Horizontal(x0, x1, height_M, depth_M));
                AddPanel(adjacencyCluster, space, PanelType.WallExternal, WallY(x0, x1, 0, height_M));
                AddPanel(adjacencyCluster, space, PanelType.WallExternal, WallY(x0, x1, depth_M, height_M));

                if (i == 0)
                {
                    AddPanel(adjacencyCluster, space, PanelType.WallExternal, WallX(x0, depth_M, height_M));
                }

                if (i == roomCount - 1)
                {
                    AddPanel(adjacencyCluster, space, PanelType.WallExternal, WallX(x1, depth_M, height_M));
                }

                VentilationTerminal ventilationTerminal = new(string.Concat(space.Name, " Supply"), FlowClassification.Supply, 30.0);

                adjacencyCluster.AddObject(ventilationTerminal);
                adjacencyCluster.AddRelation(ventilationTerminal, space);
            }

            //ONE panel object related to both neighbours, which is what makes them adjacent, plus the
            //design air movement across it.
            for (int i = 1; i < roomCount; i++)
            {
                Panel panel = Analytical.Create.Panel(new Construction(Guid.NewGuid(), "Internal Partition"), PanelType.WallInternal, WallX(i * width_M, depth_M, height_M));

                adjacencyCluster.AddObject(panel);
                adjacencyCluster.AddRelation(spaces[i - 1], panel);
                adjacencyCluster.AddRelation(spaces[i], panel);

                adjacencyCluster.AddObject(new SpaceAirMovement(
                    string.Format("Transfer {0:00000}", i),
                    0.03,
                    new ObjectReference(spaces[i - 1]).ToString(),
                    new ObjectReference(spaces[i]).ToString()));
            }

            FloorPlan2DControl control = new();

            GeometryObjectModel geometryObjectModel = new();
            geometryObjectModel.SetValue(GeometryObjectModelParameter.ViewSettings, new TwoDimensionalViewSettings(
                Guid.NewGuid(), "Level 0 [1.2m]", Geometry.Spatial.Create.Plane(1.2), null, [typeof(Space)], Geometry.Object.Query.DefaultTextAppearance(), null));

            control.Load(geometryObjectModel);

            return (control, adjacencyCluster);
        }

        private static void AddPanel(AdjacencyCluster adjacencyCluster, Space space, PanelType panelType, Face3D face3D)
        {
            Panel panel = Analytical.Create.Panel(new Construction(Guid.NewGuid(), panelType.ToString()), panelType, face3D);

            adjacencyCluster.AddObject(panel);
            adjacencyCluster.AddRelation(space, panel);
        }

        private static Face3D Horizontal(double x0, double x1, double z, double depth_M)
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

        /// <summary>
        /// Bytes allocated by one call, on this thread. A proxy for the work done that does not depend on
        /// how fast the machine is - the same measure <c>PartOFailingSpaceLookupScalingTests</c> uses.
        /// </summary>
        private static long Allocated(Action action)
        {
            long before = GC.GetAllocatedBytesForCurrentThread();

            action();

            return GC.GetAllocatedBytesForCurrentThread() - before;
        }
    }
}
