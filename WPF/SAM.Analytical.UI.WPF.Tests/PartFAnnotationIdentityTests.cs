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
using System.Text.Json.Nodes;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// A label somebody moved must still be on the same thing afterwards.
    /// <para>
    /// The failure these guard against is quiet and infuriating: an engineer drags "TRA 8 l/s" out of the
    /// middle of a doorway, recalculates the dwelling, and every label they tidied is back where it started
    /// with nothing on screen to say why. It happens because a terminal and a transfer route are
    /// <c>SAMObject</c>s the calculator builds with <c>Guid.NewGuid()</c>, so their own identities last
    /// exactly as long as one calculation. Annotation is therefore keyed on
    /// <see cref="PartFAnnotationKey"/>, derived from the persistent model identities.
    /// </para>
    /// <para>
    /// These tests run the real calculator over the same model twice, as recalculating does.
    /// </para>
    /// </summary>
    public class PartFAnnotationIdentityTests
    {
        /// <summary>The plan is cut at 1.2 m, the same height the assessment window uses.</summary>
        private static readonly Plane plane = Geometry.Spatial.Create.Plane(1.2);

        // ------------------------------------------------------------------
        // The guarantee, end to end
        // ------------------------------------------------------------------

        /// <summary>
        /// The whole journey: calculate, move a transfer label, save and reopen the view,
        /// recalculate the assessment from the same model, and find the label still on the same route.
        /// <para>
        /// Run for BOTH kinds of transfer route - one through a modelled door, one across a partition the
        /// model gives no door at all, which is the common case in an analytical model and the one with no
        /// aperture to key on.
        /// </para>
        /// </summary>
        [Fact]
        public void ManualTransferPosition_SurvivesSaveReopenAndRecalculation()
        {
            PartFPlanModel model = Model();

            PartFFloorPlanOverlay overlay_Before = Overlay(model);

            PartFOverlayMark mark_Door = TransferMark(overlay_Before, model, "Bedroom", "Kitchen");
            PartFOverlayMark mark_Partition = TransferMark(overlay_Before, model, "Kitchen", "Ensuite");

            //The two routes really are the two different cases.
            Assert.NotEqual(Guid.Empty, mark_Door.ApertureGuid);
            Assert.Equal(Guid.Empty, mark_Partition.ApertureGuid);

            //Somebody tidies both labels.
            PartFAirflowViewSettings partFAirflowViewSettings = new()
            {
                AnnotationOverrides =
                [
                    new PartFAnnotationOverride(mark_Door.AnnotationGuid, PartFAnnotationType.Transfer, new Point2D(1.5, 4.25)),
                    new PartFAnnotationOverride(mark_Partition.AnnotationGuid, PartFAnnotationType.Transfer, new Point2D(11.5, 0.75)),
                ],
            };

            //Saved and reopened. The full journey through the model's own JSON is covered by
            //PartFAirflowViewSettingsTests; what matters here is that the KEYS come back intact.
            PartFAirflowViewSettings partFAirflowViewSettings_Reopened = new(partFAirflowViewSettings.ToJsonObject());

            Assert.Equal(2, partFAirflowViewSettings_Reopened.AnnotationOverrides.Count);

            //And the assessment recalculated from the same model, which is what an engineer does after
            //changing a door or a room name.
            PartFFloorPlanOverlay overlay_After = Overlay(model);

            PartFOverlayMark mark_Door_After = TransferMark(overlay_After, model, "Bedroom", "Kitchen");
            PartFOverlayMark mark_Partition_After = TransferMark(overlay_After, model, "Kitchen", "Ensuite");

            //The keys are the same, so the saved positions still find their routes.
            Assert.Equal(mark_Door.AnnotationGuid, mark_Door_After.AnnotationGuid);
            Assert.Equal(mark_Partition.AnnotationGuid, mark_Partition_After.AnnotationGuid);

            Assert.NotNull(partFAirflowViewSettings_Reopened.Override(mark_Door_After.AnnotationGuid, PartFAnnotationType.Transfer));
            Assert.NotNull(partFAirflowViewSettings_Reopened.Override(mark_Partition_After.AnnotationGuid, PartFAnnotationType.Transfer));

            //And the placement puts them back where the person left them, rather than laying them out again.
            List<PartFTagPlacementResult> results = PartFTagPlacement.Solve(
                [Item(mark_Door_After), Item(mark_Partition_After)],
                partFAirflowViewSettings_Reopened.AnnotationOverrides);

            PartFTagPlacementResult result_Door = results.Find(x => x.ObjectGuid == mark_Door_After.AnnotationGuid);

            Assert.True(result_Door.IsUserPositioned);
            Assert.Equal(1.5, result_Door.Rectangle2D.GetCentroid().X, 6);
            Assert.Equal(4.25, result_Door.Rectangle2D.GetCentroid().Y, 6);

            Assert.True(results.Find(x => x.ObjectGuid == mark_Partition_After.AnnotationGuid).IsUserPositioned);
        }

        /// <summary>A terminal label survives the same journey, keyed on its space and role.</summary>
        [Fact]
        public void ManualTerminalPosition_SurvivesRecalculation()
        {
            PartFPlanModel model = Model();

            PartFOverlayMark mark_Before = TerminalMark(Overlay(model), model, "Kitchen", PartFAirflowAppearance.AirType.LocalKitchenExtract);
            PartFOverlayMark mark_After = TerminalMark(Overlay(model), model, "Kitchen", PartFAirflowAppearance.AirType.LocalKitchenExtract);

            Assert.Equal(mark_Before.AnnotationGuid, mark_After.AnnotationGuid);
            Assert.Equal(PartFAnnotationKey.Terminal(model.Space("Kitchen").Guid, PartFTerminalRole.LocalKitchenExtract), mark_After.AnnotationGuid);

            //A studio-style space carries two terminals; each keeps its own key, so tidying one does not move
            //the other.
            PartFOverlayMark mark_Supply = TerminalMark(Overlay(model), model, "Bedroom", PartFAirflowAppearance.AirType.Supply);

            Assert.NotEqual(mark_After.AnnotationGuid, mark_Supply.AnnotationGuid);
        }

        // ------------------------------------------------------------------
        // Why the derivation is needed
        // ------------------------------------------------------------------

        /// <summary>
        /// The reason <see cref="PartFAnnotationKey"/> exists: a recalculation produces the same logical
        /// routes and terminals with entirely new guids of their own, because the calculator builds them with
        /// <c>Guid.NewGuid()</c>.
        /// <para>
        /// If this test ever fails, those identities have become stable - which would be an improvement, not
        /// a defect. The derived keys stay correct either way; only the reason for them changes.
        /// </para>
        /// </summary>
        [Fact]
        public void RouteAndTerminalOwnGuids_AreNotStableAcrossRecalculation()
        {
            PartFPlanModel model = Model();

            PartFComplianceResult partFComplianceResult_1 = Calculate(model);
            PartFComplianceResult partFComplianceResult_2 = Calculate(model);

            PartFDoorTransferData partFDoorTransferData_1 = partFComplianceResult_1.TransferPaths[0];
            PartFDoorTransferData partFDoorTransferData_2 = partFComplianceResult_2.TransferPaths.Find(x => x.UpstreamSpaceGuid == partFDoorTransferData_1.UpstreamSpaceGuid && x.DownstreamSpaceGuid == partFDoorTransferData_1.DownstreamSpaceGuid);

            Assert.NotNull(partFDoorTransferData_2);
            Assert.NotEqual(partFDoorTransferData_1.Guid, partFDoorTransferData_2.Guid);

            PartFVentilationTerminalRequirement terminal_1 = partFComplianceResult_1.Terminals[0];
            PartFVentilationTerminalRequirement terminal_2 = partFComplianceResult_2.Terminals.Find(x => x.SpaceGuid == terminal_1.SpaceGuid && x.TerminalRole == terminal_1.TerminalRole);

            Assert.NotNull(terminal_2);
            Assert.NotEqual(terminal_1.Guid, terminal_2.Guid);
        }

        // ------------------------------------------------------------------
        // The derivation itself
        // ------------------------------------------------------------------

        /// <summary>
        /// A route with no modelled opening is keyed on its two spaces in a canonical order, so the key
        /// survives the route being reported the other way round. Which end is upstream is a calculated
        /// result and can legitimately flip when the model is edited; the partition between two rooms is the
        /// same partition either way, and a label a person put on it belongs there still.
        /// </summary>
        [Fact]
        public void TransferKey_WithoutAnAperture_DoesNotDependOnDirection()
        {
            Guid guid_1 = Guid.NewGuid();
            Guid guid_2 = Guid.NewGuid();

            Assert.Equal(
                PartFAnnotationKey.Transfer(Guid.Empty, guid_1, guid_2),
                PartFAnnotationKey.Transfer(Guid.Empty, guid_2, guid_1));
        }

        /// <summary>
        /// Two doors between the same two rooms are two routes, and the aperture is what tells their labels
        /// apart - which is why a modelled opening is used in preference to the pair of spaces.
        /// </summary>
        [Fact]
        public void TransferKey_TwoDoorsBetweenTheSameSpaces_AreDistinct()
        {
            Guid guid_Space_1 = Guid.NewGuid();
            Guid guid_Space_2 = Guid.NewGuid();

            Assert.NotEqual(
                PartFAnnotationKey.Transfer(Guid.NewGuid(), guid_Space_1, guid_Space_2),
                PartFAnnotationKey.Transfer(Guid.NewGuid(), guid_Space_1, guid_Space_2));
        }

        /// <summary>
        /// Keys are derived, so they must not collide across kinds either: a space's terminal label, its net
        /// airflow label and a transfer route that happens to involve it are different annotations.
        /// </summary>
        [Fact]
        public void Keys_OfDifferentAnnotations_AreDistinct()
        {
            Guid guid_Space = Guid.NewGuid();

            List<Guid> guids =
            [
                PartFAnnotationKey.Terminal(guid_Space, PartFTerminalRole.Supply),
                PartFAnnotationKey.Terminal(guid_Space, PartFTerminalRole.GeneralExtract),
                PartFAnnotationKey.Terminal(guid_Space, PartFTerminalRole.LocalKitchenExtract),
                PartFAnnotationKey.SpaceNetAirflow(guid_Space),
                PartFAnnotationKey.Transfer(Guid.Empty, guid_Space, Guid.NewGuid()),
                PartFAnnotationKey.Transfer(guid_Space, Guid.Empty, Guid.Empty),
            ];

            Assert.Equal(guids.Count, guids.Distinct().Count());
            Assert.DoesNotContain(Guid.Empty, guids);

            //Derived, not borrowed: a key is never the model identity it came from, so one can never be
            //mistaken for the other.
            Assert.DoesNotContain(guid_Space, guids);
        }

        /// <summary>
        /// The derivation is a pure function of the identities, so it is the same on any machine and in any
        /// process - which is what lets a key be recomputed instead of stored.
        /// </summary>
        [Fact]
        public void Keys_AreReproducible()
        {
            Guid guid_Space = new("11111111-2222-3333-4444-555555555555");

            Assert.Equal(PartFAnnotationKey.Terminal(guid_Space, PartFTerminalRole.Supply), PartFAnnotationKey.Terminal(guid_Space, PartFTerminalRole.Supply));
            Assert.Equal(PartFAnnotationKey.SpaceNetAirflow(guid_Space), PartFAnnotationKey.SpaceNetAirflow(guid_Space));
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        /// <summary>
        /// Bedroom, kitchen and ensuite in a row. The bedroom-to-kitchen partition carries a modelled door;
        /// the kitchen-to-ensuite one deliberately does not, which is the case with no aperture to key on.
        /// </summary>
        private static PartFPlanModel Model()
        {
            return new PartFPlanModel()
                .Room("Bedroom", 8)
                .Room("Kitchen", 5)
                .Room("Ensuite", 3)
                .Partition("Bedroom", "Kitchen", "D01")
                .Partition("Kitchen", "Ensuite")
                .Zone("Flat 2", "Flats", true, "Bedroom", "Kitchen", "Ensuite")
                .LocalExtractMethod("Kitchen", PartFExtractMethod.MVHRContinuousTerminal);
        }

        private static PartFComplianceResult Calculate(PartFPlanModel model)
        {
            //The SHIPPED rule set, and a fresh calculator each time - which is what recalculating is.
            PartFCalculator partFCalculator = new(Analytical.Create.PartFData(RuleSetPath())) { AdjacencyCluster = model.AdjacencyCluster };

            Assert.True(partFCalculator.Calculate("Flats"));

            return partFCalculator.DwellingResults[0].ComplianceResult;
        }

        private static PartFFloorPlanOverlay Overlay(PartFPlanModel model)
        {
            return PartFFloorPlanOverlay.Build(model.AdjacencyCluster, Calculate(model), plane);
        }

        private static PartFOverlayMark TransferMark(PartFFloorPlanOverlay overlay, PartFPlanModel model, string name_Upstream, string name_Downstream)
        {
            Guid guid_1 = model.Space(name_Upstream).Guid;
            Guid guid_2 = model.Space(name_Downstream).Guid;

            //Either direction: which end the calculation calls upstream is not this test's business.
            return Assert.Single(overlay.Marks.Where(x => x.IsTransfer
                && ((x.SpaceGuid == guid_1 && x.DownstreamSpaceGuid == guid_2) || (x.SpaceGuid == guid_2 && x.DownstreamSpaceGuid == guid_1))));
        }

        private static PartFOverlayMark TerminalMark(PartFFloorPlanOverlay overlay, PartFPlanModel model, string name_Space, PartFAirflowAppearance.AirType airType)
        {
            return Assert.Single(overlay.Marks.Where(x => !x.IsTransfer && x.SpaceGuid == model.Space(name_Space).Guid && x.AirType == airType));
        }

        private static PartFTagPlacementItem Item(PartFOverlayMark mark)
        {
            return new PartFTagPlacementItem()
            {
                ObjectGuid = mark.AnnotationGuid,
                AnnotationType = mark.AnnotationType,
                Priority = PartFTagPlacement.Priority(mark),
                Anchor2D = mark.End,
                Width = 1.4,
                Height = 0.25,
                Tag = mark,
            };
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
