// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using SAM.Analytical.UI.WPF.Tests.Helpers;
using SAM.Core;
using SAM.Geometry.Object;
using SAM.Geometry.Planar;
using SAM.Geometry.Spatial;
using SAM.Geometry.UI;
using SAM.Geometry.UI.WPF;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// The Ventilation Design overlay's transfer tags on a real plan, alongside Approved Document F's own.
    /// <para>
    /// <b>The two transfer figures coexist; neither replaces the other.</b> Approved Document F's is what
    /// the regulation requires of the route and is fixed by Table 1.2; the design one is what the model is
    /// currently designed to move through it and is re-solved every Approved Document O round. A drawing
    /// that showed only one of them - whichever one - is the drawing that produced the acceptance finding
    /// these tests exist for. Both are shown, and neither may land on top of the other.
    /// </para>
    /// </summary>
    [Collection(WpfCollection.Name)]
    public class DesignTransferRendererTests
    {
        private static readonly Plane plane = Geometry.Spatial.Create.Plane(1.2);

        // -------------------------------------------------------------------------------------------
        // Fixture
        // -------------------------------------------------------------------------------------------

        /// <summary>Two rooms, one door between them, on a loaded 2D plan.</summary>
        private static (FloorPlan2DControl Control, PartFPlanModel Model) Build(double designTransfer_Lps)
        {
            PartFPlanModel model = new PartFPlanModel()
                .Room("Bedroom", 8)
                .Room("Hall", 4)
                .Partition("Bedroom", "Hall", "D01")
                .Zone("Flat 1", "Flats", true, "Bedroom", "Hall");

            SpaceAirMovement spaceAirMovement = new(
                "Bedroom to Hall",
                designTransfer_Lps / 1000.0,
                new ObjectReference(model.Space("Bedroom")).ToString(),
                new ObjectReference(model.Space("Hall")).ToString());

            model.AdjacencyCluster.AddObject(spaceAirMovement);

            FloorPlan2DControl control = new();

            GeometryObjectModel geometryObjectModel = new();
            geometryObjectModel.SetValue(GeometryObjectModelParameter.ViewSettings, new TwoDimensionalViewSettings(
                Guid.NewGuid(), "Level 0 [1.2m]", plane, null, [typeof(Space)], Geometry.Object.Query.DefaultTextAppearance(), null));

            control.Load(geometryObjectModel);

            return (control, model);
        }

        /// <summary>
        /// Approved Document F's own view of the same route, at its own Table 1.2 figure - deliberately a
        /// different number from the design one, which is the situation being tested.
        /// </summary>
        private static PartFComplianceResult ComplianceResult(PartFPlanModel model, double continuousTransfer_Lps)
        {
            Space space_Bedroom = model.Space("Bedroom");
            Space space_Hall = model.Space("Hall");

            return new PartFComplianceResult("Flat 1")
            {
                TransferPaths =
                [
                    new PartFDoorTransferData("D01")
                    {
                        ApertureGuid = model.ApertureGuid("D01"),
                        UpstreamSpaceGuid = space_Bedroom.Guid,
                        UpstreamSpaceName = space_Bedroom.Name,
                        DownstreamSpaceGuid = space_Hall.Guid,
                        DownstreamSpaceName = space_Hall.Name,
                        DwellingName = "Flat 1",
                        IsInternalDwellingDoor = true,
                        IsDoorRepresented = true,
                        RequiresTransferAirPath = true,
                        ContinuousDesignTransferFlowRate_Lps = continuousTransfer_Lps,
                        RouteStatus = PartFTransferRouteStatus.UniquelyDetermined,
                    },
                ],
            };
        }

        // -------------------------------------------------------------------------------------------
        // Coexistence
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// <b>The acceptance finding, end to end.</b> The same door carries an Approved Document F transfer
        /// requirement of 63 l/s and a design transfer of 150 l/s. Both tags are drawn, they say different
        /// things, and neither erases or overlaps the other.
        /// </summary>
        [WpfFact]
        public void BothTransferFigures_AreDrawn_AndDoNotOverlap()
        {
            (FloorPlan2DControl control, PartFPlanModel model) = Build(150.0);

            PartFAirflowRenderer partFRenderer = new(control)
            {
                ViewSettings = new PartFAirflowViewSettings { Enabled = true, ShowTransfer = true, ShowValues = true },
            };

            partFRenderer.Load(model.AdjacencyCluster, [ComplianceResult(model, 63.0)]);

            List<Rectangle2D> partFRectangle2Ds = partFRenderer.PlacedRectangle2Ds();
            Assert.NotEmpty(partFRectangle2Ds);

            DesignAirFlowRenderer designRenderer = new(control)
            {
                ViewSettings = new DesignAirFlowViewSettings { Enabled = true, ShowSupply = true, ShowExtract = true, ShowTransfer = true },
            };

            designRenderer.Load(model.AdjacencyCluster, null, partFRectangle2Ds, annotationScale: PartFTagPlacement.DefaultAnnotationScale);

            //Two transfer tags on one door, saying two different things.
            PartFOverlayMark mark_PartF = Assert.Single(partFRenderer.Marks.Where(x => x.IsTransfer));
            DesignAirFlowOverlayMark mark_Design = Assert.Single(designRenderer.Marks.Where(x => x.IsTransfer));

            Assert.Equal(63.0, mark_PartF.FlowRate_Lps.Value, 6);
            Assert.Equal(150.0, mark_Design.FlowRate_Lps.Value, 6);
            Assert.NotEqual(mark_PartF.Label, mark_Design.Label);

            Rectangle2D rectangle2D_Design = designRenderer.Placement(mark_Design)?.Rectangle2D;
            Assert.NotNull(rectangle2D_Design);

            foreach (Rectangle2D rectangle2D_PartF in partFRectangle2Ds)
            {
                Assert.False(
                    rectangle2D_PartF.InRange(rectangle2D_Design) || rectangle2D_Design.InRange(rectangle2D_PartF),
                    "The design transfer tag landed on Approved Document F's own transfer tag.");
            }

            //And neither renderer took the other's marks away.
            Assert.NotEmpty(partFRenderer.Marks);
            Assert.NotEmpty(designRenderer.Marks);
        }

        /// <summary>
        /// Approved Document F's transfer tag sits in exactly the same place whether or not the design
        /// overlay is drawing beside it. The regulatory figure does not move because somebody switched
        /// another overlay on - the same one-directional rule
        /// <c>DesignAirFlowRendererTests.PartFsPlacement_IsIdentical_WhetherOrNotDesignOverlayExists</c>
        /// pins for terminal tags, now with two transfer tags contesting one door.
        /// </summary>
        [WpfFact]
        public void PartFsTransferPlacement_IsIdentical_WhetherOrNotTheDesignOverlayExists()
        {
            (FloorPlan2DControl control_Alone, PartFPlanModel model_Alone) = Build(150.0);

            PartFAirflowRenderer partFRenderer_Alone = new(control_Alone)
            {
                ViewSettings = new PartFAirflowViewSettings { Enabled = true, ShowTransfer = true, ShowValues = true },
            };

            partFRenderer_Alone.Load(model_Alone.AdjacencyCluster, [ComplianceResult(model_Alone, 63.0)]);

            (FloorPlan2DControl control_WithDesign, PartFPlanModel model_WithDesign) = Build(150.0);

            PartFAirflowRenderer partFRenderer_WithDesign = new(control_WithDesign)
            {
                ViewSettings = new PartFAirflowViewSettings { Enabled = true, ShowTransfer = true, ShowValues = true },
            };

            partFRenderer_WithDesign.Load(model_WithDesign.AdjacencyCluster, [ComplianceResult(model_WithDesign, 63.0)]);

            DesignAirFlowRenderer designRenderer = new(control_WithDesign)
            {
                ViewSettings = new DesignAirFlowViewSettings { Enabled = true, ShowTransfer = true },
            };

            designRenderer.Load(model_WithDesign.AdjacencyCluster, null, partFRenderer_WithDesign.PlacedRectangle2Ds());

            Rectangle2D rectangle2D_Alone = Assert.Single(partFRenderer_Alone.PlacedRectangle2Ds());
            Rectangle2D rectangle2D_WithDesign = Assert.Single(partFRenderer_WithDesign.PlacedRectangle2Ds());

            Assert.Equal(rectangle2D_Alone.Origin.X, rectangle2D_WithDesign.Origin.X, 9);
            Assert.Equal(rectangle2D_Alone.Origin.Y, rectangle2D_WithDesign.Origin.Y, 9);
        }

        // -------------------------------------------------------------------------------------------
        // Visibility
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// A hidden transfer mark claims no space in the shared solve, and is not built at all - so
        /// switching it off cannot move the tags that ARE shown into worse positions than they would have
        /// had on their own.
        /// </summary>
        [WpfFact]
        public void ShowTransferOff_LeavesNoTransferMarkAndNoPlacement()
        {
            (FloorPlan2DControl control, PartFPlanModel model) = Build(150.0);

            DesignAirFlowRenderer designRenderer = new(control)
            {
                ViewSettings = new DesignAirFlowViewSettings { Enabled = true, ShowSupply = true, ShowTransfer = false },
            };

            designRenderer.Load(model.AdjacencyCluster);

            Assert.Empty(designRenderer.Marks.Where(x => x.IsTransfer));
        }

        /// <summary>
        /// A transfer tag is solved clear of the opening it annotates rather than being pinned to a room:
        /// it belongs to the door between two spaces and so to neither space's outline, matching Approved
        /// Document F's own rule for the same mark. Its engineering anchor stays ON the route.
        /// </summary>
        [WpfFact]
        public void ATransferTag_IsAnchoredOnTheRoute_AndBelongsToNeitherRoom()
        {
            (FloorPlan2DControl control, PartFPlanModel model) = Build(150.0);

            DesignAirFlowRenderer designRenderer = new(control)
            {
                ViewSettings = new DesignAirFlowViewSettings { Enabled = true, ShowTransfer = true },
            };

            designRenderer.Load(model.AdjacencyCluster);

            DesignAirFlowOverlayMark mark = Assert.Single(designRenderer.Marks.Where(x => x.IsTransfer));

            PartFTagPlacementResult result = designRenderer.Placement(mark);

            Assert.NotNull(result);

            //The tag may be displaced; the point it reports on may not.
            Assert.Equal(mark.End.X, result.Item.Anchor2D.X, 9);
            Assert.Equal(mark.End.Y, result.Item.Anchor2D.Y, 9);

            //And it is not constrained to either room's outline, so a tag on a door can sit on the side of
            //it that has room, rather than being forced into whichever space happens to own the mark.
            Assert.Null(result.Item.LimitArea);
        }
    }
}
