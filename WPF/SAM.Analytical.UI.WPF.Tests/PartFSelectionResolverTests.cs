// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical;
using SAM.Analytical.Enums;
using SAM.Analytical.UI.WPF.Tests.Helpers;
using SAM.Core;
using SAM.Geometry.Spatial;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// Regression tests for the floor-plan selection bug.
    /// <para>
    /// Selecting Bedroom 2_3 reported "No Part F terminal is required in this space" while the same view
    /// was drawing SUP 63 l/s on that very room and the Terminals tab listed its supply terminal. The
    /// cause was taking the FIRST selected object: a click selects more than one object, the control
    /// returns them from an unordered set, and "first" was whichever one enumeration happened to yield.
    /// </para>
    /// <para>
    /// Everything here matches on stable <see cref="Guid"/>. Nothing may match on object identity: the
    /// plan is built from its own clone of the model, so no object on it is reference-equal to anything
    /// in the assessment.
    /// </para>
    /// </summary>
    public class PartFSelectionResolverTests
    {
        /// <summary>
        /// The bug, reproduced: the assessed space arrives SECOND in the selection, behind a wall.
        /// Resolving must find it anyway.
        /// </summary>
        [Fact]
        public void AssessedSpace_IsFoundWhereverItSitsInTheSelection()
        {
            PartFComplianceResult complianceResult = Flat2(out PartFPlanModel model);

            Space space = model.Space("Bedroom");
            Panel panel = model.AdjacencyCluster.GetPanels()[0];

            Assert.NotEqual(Guid.Empty, PartFSelectionResolver.SpaceGuid([panel, space], complianceResult));
            Assert.Equal(space.Guid, PartFSelectionResolver.SpaceGuid([panel, space], complianceResult));

            //And in the other order, so the result does not depend on enumeration order at all.
            Assert.Equal(space.Guid, PartFSelectionResolver.SpaceGuid([space, panel], complianceResult));
        }

        /// <summary>
        /// The exact reported symptom: a space with a supply terminal must never resolve to no terminals.
        /// </summary>
        [Fact]
        public void SpaceWithASupplyTerminal_ResolvesToThatTerminal()
        {
            PartFComplianceResult complianceResult = Flat2(out PartFPlanModel model);

            Space space = model.Space("Bedroom");

            Guid guid = PartFSelectionResolver.SpaceGuid([space], complianceResult);

            List<PartFVentilationTerminalRequirement> terminals = PartFSelectionResolver.Terminals(guid, complianceResult);

            Assert.NotEmpty(terminals);
            Assert.Contains(terminals, x => x.TerminalRole == PartFTerminalRole.Supply);
            Assert.All(terminals, x => Assert.Equal(space.Guid, x.SpaceGuid));
        }

        /// <summary>
        /// Matching is by guid, never by reference. A clone of the space - which is what the floor plan
        /// hands back - must resolve exactly as the original does.
        /// </summary>
        [Fact]
        public void AClonedSpace_ResolvesTheSameAsTheOriginal()
        {
            PartFComplianceResult complianceResult = Flat2(out PartFPlanModel model);

            Space space = model.Space("Kitchen");
            Space space_Clone = new(space);

            Assert.NotSame(space, space_Clone);
            Assert.Equal(space.Guid, space_Clone.Guid);

            Assert.Equal(space.Guid, PartFSelectionResolver.SpaceGuid([space_Clone], complianceResult));
            Assert.NotEmpty(PartFSelectionResolver.Terminals(PartFSelectionResolver.SpaceGuid([space_Clone], complianceResult), complianceResult));
        }

        /// <summary>A studio's two terminals both come back, in a stable order.</summary>
        [Fact]
        public void SpaceWithTwoTerminals_ReturnsBoth()
        {
            PartFComplianceResult complianceResult = Flat1(out PartFPlanModel model);

            List<PartFVentilationTerminalRequirement> terminals = PartFSelectionResolver.Terminals(model.Space("Studio").Guid, complianceResult);

            Assert.Equal(2, terminals.Count);
            Assert.Contains(terminals, x => x.TerminalRole == PartFTerminalRole.Supply);
            Assert.Contains(terminals, x => x.TerminalRole == PartFTerminalRole.LocalKitchenExtract);
        }

        /// <summary>
        /// A space the assessment holds nothing for resolves to nothing, so the panel can say so honestly
        /// rather than showing another room's data.
        /// </summary>
        [Fact]
        public void UnassessedSpace_ResolvesToNothing()
        {
            PartFComplianceResult complianceResult = Flat1(out PartFPlanModel model);

            Assert.Equal(Guid.Empty, PartFSelectionResolver.SpaceGuid([model.Space("Corridor")], complianceResult));
            Assert.Empty(PartFSelectionResolver.Terminals(model.Space("Corridor").Guid, complianceResult));
        }

        /// <summary>Nothing selected, and nothing invented.</summary>
        [Fact]
        public void EmptySelection_ResolvesToNothing()
        {
            PartFComplianceResult complianceResult = Flat1(out _);

            Assert.Equal(Guid.Empty, PartFSelectionResolver.SpaceGuid([], complianceResult));
            Assert.Null(PartFSelectionResolver.TransferPath([], complianceResult));
            Assert.Equal(Guid.Empty, PartFSelectionResolver.SpaceGuid(null, complianceResult));
        }

        /// <summary>Clicking a door resolves to the route recorded against that aperture.</summary>
        [Fact]
        public void SelectedAperture_ResolvesToItsTransferRoute()
        {
            PartFComplianceResult complianceResult = Flat2(out PartFPlanModel model);

            Guid guid = model.ApertureGuid("D02");

            Aperture aperture = model.AdjacencyCluster.GetPanels()
                .SelectMany(x => x.Apertures ?? [])
                .First(x => x.Guid == guid);

            PartFDoorTransferData partFDoorTransferData = PartFSelectionResolver.TransferPath([aperture], complianceResult);

            Assert.NotNull(partFDoorTransferData);
            Assert.Equal(guid, partFDoorTransferData.ApertureGuid);
            Assert.Equal("Kitchen", partFDoorTransferData.UpstreamSpaceName);
            Assert.Equal("Ensuite", partFDoorTransferData.DownstreamSpaceName);
        }

        /// <summary>Both routes touching a space come back, whichever end of them it is.</summary>
        [Fact]
        public void MiddleSpace_ReturnsBothOfItsRoutes()
        {
            PartFComplianceResult complianceResult = Flat2(out PartFPlanModel model);

            List<PartFDoorTransferData> transferPaths = PartFSelectionResolver.TransferPaths(model.Space("Kitchen").Guid, complianceResult);

            Assert.Equal(2, transferPaths.Count);
            Assert.Contains(transferPaths, x => x.DownstreamSpaceName == "Kitchen");
            Assert.Contains(transferPaths, x => x.UpstreamSpaceName == "Kitchen");
        }

        // ------------------------------------------------------------------
        // Fixtures
        // ------------------------------------------------------------------

        private static PartFComplianceResult Flat1(out PartFPlanModel model)
        {
            model = new PartFPlanModel()
                .Room("Studio", 8)
                .Room("Bathroom", 4)
                .Room("Corridor", 3)
                .Partition("Studio", "Bathroom", "D01")
                .Partition("Bathroom", "Corridor", "D02")
                .Zone("Flat 1", "Flats", true, "Studio", "Bathroom")
                .LocalExtractMethod("Studio", Analytical.Enums.PartFExtractMethod.MVHRContinuousTerminal);

            return Build(model);
        }

        private static PartFComplianceResult Flat2(out PartFPlanModel model)
        {
            model = new PartFPlanModel()
                .Room("Bedroom", 8)
                .Room("Kitchen", 5)
                .Room("Ensuite", 3)
                .Partition("Bedroom", "Kitchen", "D01")
                .Partition("Kitchen", "Ensuite", "D02")
                .Zone("Flat 2", "Flats", true, "Bedroom", "Kitchen", "Ensuite")
                .LocalExtractMethod("Kitchen", Analytical.Enums.PartFExtractMethod.MVHRContinuousTerminal);

            return Build(model);
        }

        private static PartFComplianceResult Build(PartFPlanModel model)
        {
            PartFCalculator partFCalculator = new(Analytical.Create.PartFData(RuleSetPath())) { AdjacencyCluster = model.AdjacencyCluster };

            Assert.True(partFCalculator.Calculate("Flats"));

            return partFCalculator.DwellingResults[0].ComplianceResult;
        }

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
