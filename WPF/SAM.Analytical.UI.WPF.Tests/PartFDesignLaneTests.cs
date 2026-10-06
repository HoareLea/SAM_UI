// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using SAM.Analytical.UI.WPF.Tests.Helpers;
using SAM.Geometry.Object;
using SAM.Geometry.Planar;
using SAM.Geometry.Spatial;
using SAM.Geometry.UI;
using SAM.Geometry.UI.WPF;
using System;
using System.Linq;
using System.Reflection;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// The presentation contract for showing Part F and Ventilation Design annotations together: a Part F
    /// tag and a Design tag reporting on the same space or the same route must be visually distinguishable
    /// two ways at once - a stable lane (Part F above the space's shared reference row, Design below it, see
    /// <see cref="PartFTagPlacement.Lane"/>) and an explicit textual identifier (<c>PartFAirflowRenderer.AuthorityPrefix</c>
    /// / <c>DesignAirFlowRenderer.AuthorityPrefix</c>) - neither of which is allowed to be the only distinction.
    /// <para>
    /// The pure geometry of the lane itself is proved without any renderer in
    /// <see cref="PartFTagPlacementLaneTests"/>; this file proves the end-to-end contract through the real
    /// renderers, on the same fixtures <see cref="DesignAirFlowRendererTests"/> and
    /// <see cref="OverlayOwnershipTests"/> already use. It does not repeat what those files already assert -
    /// that the two renderers never overlap or erase each other's tags, and that Part F's own placement is
    /// bit-identical whether or not a <see cref="DesignAirFlowRenderer"/> exists at all
    /// (<c>PartFsPlacement_IsIdentical_WhetherOrNotDesignOverlayExists</c>) - only the lane and label
    /// guarantees this task adds on top of them.
    /// </para>
    /// </summary>
    [Collection(WpfCollection.Name)]
    public class PartFDesignLaneTests
    {
        private static readonly Plane plane = Geometry.Spatial.Create.Plane(1.2);

        // ------------------------------------------------------------------
        // Fixture: one space, both authorities
        // ------------------------------------------------------------------

        private static (FloorPlan2DControl Control, AdjacencyCluster AdjacencyCluster, Space Space) Build(string spaceName = "Studio")
        {
            PartFPlanModel model = new PartFPlanModel().Room(spaceName, 8).Close();

            Space space = model.Space(spaceName);

            FloorPlan2DControl control = new();

            GeometryObjectModel geometryObjectModel = new();
            geometryObjectModel.SetValue(GeometryObjectModelParameter.ViewSettings, new TwoDimensionalViewSettings(
                Guid.NewGuid(), "Level 0 [1.2m]", plane, null, [typeof(Space)], Geometry.Object.Query.DefaultTextAppearance(), null));

            control.Load(geometryObjectModel);

            return (control, model.AdjacencyCluster, space);
        }

        private static PartFComplianceResult BuildComplianceResult(Space space, double continuousDesignFlowRate_Lps)
        {
            return new PartFComplianceResult("Flat 1")
            {
                Terminals =
                [
                    new PartFVentilationTerminalRequirement(space.Name + " Supply", space.Guid, PartFTerminalRole.Supply)
                    {
                        SpaceName = space.Name,
                        ContinuousDesignFlowRate_Lps = continuousDesignFlowRate_Lps,
                        IsRequired = true,
                    },
                ],
            };
        }

        private static void AddDesignSupply(AdjacencyCluster adjacencyCluster, Space space, double designFlowRate_Lps)
        {
            VentilationTerminal ventilationTerminal = new(space.Name + " Design Supply", FlowClassification.Supply, designFlowRate_Lps);

            adjacencyCluster.AddObject(ventilationTerminal);
            adjacencyCluster.AddRelation(ventilationTerminal, space);
        }

        /// <summary>The same reference row the renderers compute, from the space's own section outline.</summary>
        private static double Row(AdjacencyCluster adjacencyCluster, Space space)
        {
            Face2D face2D = adjacencyCluster.SpaceSectionFace2Ds(space, plane)?.Where(x => x is not null).OrderByDescending(x => x.GetArea()).FirstOrDefault();

            return face2D.GetInternalPoint2D().Y;
        }

        // ------------------------------------------------------------------
        // The lane: same space, both authorities, native acceptance's own numbers
        // ------------------------------------------------------------------

        /// <summary>
        /// The native acceptance scenario itself: a studio with a Part F requirement of 30 l/s and a
        /// Ventilation Design duty of 150 l/s. Both tags anchor at the space's single internal point, so
        /// naive placement is exactly what produced the unreadable interleaving the task describes. Proves
        /// both halves of the fix at once: the Part F tag's centre sits on its own side of the shared row and
        /// the Design tag's on the other, and - independently of position - the two labels carry different
        /// explicit identifiers even though the underlying rate text ("SUP ... l/s") is otherwise identical.
        /// </summary>
        [WpfFact]
        public void SameSpace_PartFTagIsAboveTheRow_DesignTagIsBelowIt_AndLabelsAreDistinguishable()
        {
            (FloorPlan2DControl control, AdjacencyCluster adjacencyCluster, Space space) = Build();

            AddDesignSupply(adjacencyCluster, space, 150.0);

            PartFAirflowRenderer partFRenderer = new(control) { ViewSettings = new PartFAirflowViewSettings { Enabled = true, ShowSupply = true, ShowValues = true } };
            partFRenderer.Load(adjacencyCluster, [BuildComplianceResult(space, 30.0)]);

            DesignAirFlowRenderer designRenderer = new(control) { ViewSettings = new DesignAirFlowViewSettings { Enabled = true, ShowSupply = true } };
            designRenderer.Load(adjacencyCluster, null, partFRenderer.PlacedRectangle2Ds());

            PartFOverlayMark partFMark = Assert.Single(partFRenderer.Marks);
            DesignAirFlowOverlayMark designMark = Assert.Single(designRenderer.Marks);

            PartFTagPlacementResult partFResult = partFRenderer.Placement(partFMark);
            PartFTagPlacementResult designResult = designRenderer.Placement(designMark);

            Assert.Equal(Solver2DResultType.Solved, partFResult.ResultType);
            Assert.Equal(Solver2DResultType.Solved, designResult.ResultType);

            double row = Row(adjacencyCluster, space);

            //The lane: Part F strictly above the row, Design strictly below it - so the two tags can never
            //trade places, whatever the collision search does within each lane.
            Assert.True(partFResult.Rectangle2D.GetCentroid().Y >= row,
                string.Format("Part F tag centred at Y={0:0.###} did not stay in the Part F lane (row {1:0.###}).", partFResult.Rectangle2D.GetCentroid().Y, row));

            Assert.True(designResult.Rectangle2D.GetCentroid().Y <= row,
                string.Format("Design tag centred at Y={0:0.###} did not stay in the Design lane (row {1:0.###}).", designResult.Rectangle2D.GetCentroid().Y, row));

            Assert.True(partFResult.Rectangle2D.GetCentroid().Y > designResult.Rectangle2D.GetCentroid().Y,
                "The Part F tag must sit visually above the Design tag for the same space.");

            //The label: an explicit identifier on each, independent of the lane.
            string partFLabel = InvokeLabel(partFRenderer, partFMark);
            string designLabel = InvokeLabel(designMark);

            Assert.StartsWith("F ", partFLabel, StringComparison.Ordinal);
            Assert.StartsWith("D ", designLabel, StringComparison.Ordinal);
            Assert.NotEqual(partFLabel, designLabel);

            //Neither authority is ever called "Calculated" - see the task's own note that a Part F
            //requirement, a design airflow, an operating airflow and an equipment capacity may all be
            //calculated values, so that word says nothing about which this is.
            Assert.DoesNotContain("Calculated", partFLabel, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Calculated", designLabel, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// The lane survives a coarser annotation scale. A tag is bigger, in plane units, at 1:150 than at
        /// 1:50 - see <c>DesignAirFlowRendererTests.Place_DifferentAnnotationScale_LaysTagsOutDifferently</c> -
        /// and the lane constraint has to hold at whatever size the tag actually solves at, not just the
        /// default.
        /// </summary>
        [WpfFact]
        public void Lane_HoldsAtACoarserAnnotationScale()
        {
            (FloorPlan2DControl control, AdjacencyCluster adjacencyCluster, Space space) = Build();

            AddDesignSupply(adjacencyCluster, space, 150.0);

            PartFAirflowRenderer partFRenderer = new(control) { ViewSettings = new PartFAirflowViewSettings { Enabled = true, ShowSupply = true, ShowValues = true, AnnotationScale = 150 } };
            partFRenderer.Load(adjacencyCluster, [BuildComplianceResult(space, 30.0)]);

            DesignAirFlowRenderer designRenderer = new(control) { ViewSettings = new DesignAirFlowViewSettings { Enabled = true, ShowSupply = true } };
            designRenderer.Load(adjacencyCluster, null, partFRenderer.PlacedRectangle2Ds(), annotationScale: 150);

            double row = Row(adjacencyCluster, space);

            Rectangle2D partFRectangle2D = partFRenderer.Placement(Assert.Single(partFRenderer.Marks)).Rectangle2D;
            Rectangle2D designRectangle2D = designRenderer.Placement(Assert.Single(designRenderer.Marks)).Rectangle2D;

            Assert.True(partFRectangle2D.GetCentroid().Y >= row);
            Assert.True(designRectangle2D.GetCentroid().Y <= row);
        }

        // ------------------------------------------------------------------
        // Each lane is stable on its own - the other overlay toggled off entirely
        // ------------------------------------------------------------------

        /// <summary>
        /// Part F's own lane does not depend on a <see cref="DesignAirFlowRenderer"/> existing at all - no
        /// renderer, no obstacles, nothing. The behavioural bit-identical-position proof already lives in
        /// <c>DesignAirFlowRendererTests.PartFsPlacement_IsIdentical_WhetherOrNotDesignOverlayExists</c>; this
        /// adds the lane-membership angle that test does not check.
        /// </summary>
        [WpfFact]
        public void PartFLane_IsUnaffected_ByDesignOverlayNotExisting()
        {
            (FloorPlan2DControl control, AdjacencyCluster adjacencyCluster, Space space) = Build();

            PartFAirflowRenderer partFRenderer = new(control) { ViewSettings = new PartFAirflowViewSettings { Enabled = true, ShowSupply = true, ShowValues = true } };
            partFRenderer.Load(adjacencyCluster, [BuildComplianceResult(space, 30.0)]);

            double row = Row(adjacencyCluster, space);

            Rectangle2D rectangle2D = partFRenderer.Placement(Assert.Single(partFRenderer.Marks)).Rectangle2D;

            Assert.True(rectangle2D.GetCentroid().Y >= row);
        }

        /// <summary>
        /// The Design lane does not depend on a Part F overlay existing at all - no renderer, no obstacles,
        /// nothing supplied to <see cref="DesignAirFlowRenderer.Load"/>. Design must not "move into" the
        /// space Part F would otherwise have occupied merely because Part F is not there.
        /// </summary>
        [WpfFact]
        public void DesignLane_IsUnaffected_ByPartFOverlayNotExisting()
        {
            (FloorPlan2DControl control, AdjacencyCluster adjacencyCluster, Space space) = Build();

            AddDesignSupply(adjacencyCluster, space, 45.0);

            DesignAirFlowRenderer designRenderer = new(control) { ViewSettings = new DesignAirFlowViewSettings { Enabled = true, ShowSupply = true } };
            designRenderer.Load(adjacencyCluster);

            double row = Row(adjacencyCluster, space);

            Rectangle2D rectangle2D = designRenderer.Placement(Assert.Single(designRenderer.Marks)).Rectangle2D;

            Assert.True(rectangle2D.GetCentroid().Y <= row);
        }

        // ------------------------------------------------------------------
        // Transfer: the label distinction, without touching the arrow or the anchor
        // ------------------------------------------------------------------

        /// <summary>
        /// "F TRA ... != D TRA ...", proved directly against the two renderers' own label-building code
        /// rather than through a full transfer-air calculation - the labelling rule does not depend on how
        /// the rate was arrived at, and this is the one place a coincidence in the underlying text (both
        /// authorities reporting the very same rate on the very same route, the worst case for
        /// distinguishability) can be forced deliberately. Neither renderer's placement, anchor or arrow
        /// geometry is touched by this task - see the task's own note - so this test asserts only the text.
        /// </summary>
        [WpfFact]
        public void TransferLabels_CarryDistinctAuthorityIdentifiers_EvenWhenTheUnderlyingRateTextCoincides()
        {
            PartFOverlayMark partFMark = new()
            {
                AirType = PartFAirflowAppearance.AirType.TransferAir,
                Start = new Point2D(0, 0),
                End = new Point2D(1, 0),
                FlowRate_Lps = 8.0,
                Label = "TRA 8.0 l/s ?",
                IsUnresolved = true,
                OpeningStatus = PartFTransferOpeningStatus.MissingTransferOpening,

                //Matching how PartFFloorPlanOverlay actually sets an unresolved route's status: the same
                //"?" as the label already carries, so Label()'s own dedup applies and the symbol is not
                //doubled - see the "? ?" assertion below.
                Status = PartFComplianceStatus.CannotBeDetermined,
            };

            DesignAirFlowOverlayMark designMark = new()
            {
                MarkType = DesignAirFlowMarkType.Transfer,
                Label = "TRA 8.0 l/s ?",
                IsUnresolved = true,
            };

            (FloorPlan2DControl control, _, _) = Build();

            PartFAirflowRenderer partFRenderer = new(control) { ViewSettings = new PartFAirflowViewSettings { Enabled = true, ShowTransfer = true, ShowValues = true, ShowUnresolved = true } };

            string partFLabel = InvokeLabel(partFRenderer, partFMark);
            string designLabel = InvokeLabel(designMark);

            Assert.NotEqual(partFLabel, designLabel);
            Assert.StartsWith("F ", partFLabel, StringComparison.Ordinal);
            Assert.StartsWith("D ", designLabel, StringComparison.Ordinal);

            //The existing "?" - no modelled opening - behaviour is untouched: it still appears, exactly
            //once, on both.
            Assert.EndsWith("?", partFLabel, StringComparison.Ordinal);
            Assert.EndsWith("?", designLabel, StringComparison.Ordinal);
            Assert.DoesNotContain("? ?", partFLabel, StringComparison.Ordinal);
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        private static string InvokeLabel(PartFAirflowRenderer partFAirflowRenderer, PartFOverlayMark mark)
        {
            MethodInfo methodInfo = typeof(PartFAirflowRenderer).GetMethod("Label", BindingFlags.Instance | BindingFlags.NonPublic);

            return (string)methodInfo.Invoke(partFAirflowRenderer, [mark]);
        }

        private static string InvokeLabel(DesignAirFlowOverlayMark mark)
        {
            MethodInfo methodInfo = typeof(DesignAirFlowRenderer).GetMethod("Label", BindingFlags.Static | BindingFlags.NonPublic);

            return (string)methodInfo.Invoke(null, [mark]);
        }
    }
}
