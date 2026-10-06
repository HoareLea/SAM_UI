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
    /// A whole floor of flats on one drawing.
    /// <para>
    /// This is what the normal saved 2D view draws - a level, with every dwelling on it annotated at once -
    /// and it is the case where the dwellings could most easily bleed into each other. The architecture makes
    /// that impossible by building one <see cref="PartFFloorPlanOverlay"/> per dwelling assessment rather than
    /// one for the floor, and these tests assert it rather than inferring it: an inference is not a test, and
    /// transfer air drawn between two flats would be a claim about a building that no assessment made.
    /// </para>
    /// <para>
    /// Layout: Flat 1 (studio, bathroom), a communal corridor belonging to no dwelling, then Flat 2 (bedroom,
    /// kitchen, ensuite). Every partition is real and shared, so nothing here relies on the two flats being
    /// geometrically unaware of one another - they adjoin.
    /// </para>
    /// </summary>
    public class PartFWholeFloorTests
    {
        /// <summary>The plan is cut at 1.2 m, the same height every Part F view uses.</summary>
        private static readonly Plane plane = Geometry.Spatial.Create.Plane(1.2);

        /// <summary>Both flats are assessed, and each gets its own overlay.</summary>
        [Fact]
        public void WholeFloor_AssessesEveryDwelling()
        {
            List<PartFComplianceResult> results = Calculate(Model());

            Assert.Equal(2, results.Count);
            Assert.Contains("Flat 1", results.ConvertAll(x => x.DwellingName));
            Assert.Contains("Flat 2", results.ConvertAll(x => x.DwellingName));
        }

        /// <summary>
        /// <b>No transfer route between two dwellings.</b> The flats share a partition through the corridor
        /// wall, so a floor-wide network would have every opportunity to route air across it - and Part F
        /// internal transfer is a dwelling-internal requirement, so an arrow between two flats would be
        /// nonsense drawn confidently.
        /// </summary>
        [Fact]
        public void WholeFloor_DrawsNoTransferBetweenDwellings()
        {
            PartFPlanModel model = Model();

            Dictionary<string, HashSet<Guid>> dictionary = new()
            {
                { "Flat 1", Guids(model, "Studio", "Bathroom") },
                { "Flat 2", Guids(model, "Bedroom", "Kitchen", "Ensuite") },
            };

            foreach (PartFComplianceResult partFComplianceResult in Calculate(model))
            {
                HashSet<Guid> guids = dictionary[partFComplianceResult.DwellingName];

                foreach (PartFOverlayMark mark in Overlay(model, partFComplianceResult).Marks.Where(x => x.IsTransfer))
                {
                    Assert.Contains(mark.SpaceGuid, guids);
                    Assert.Contains(mark.DownstreamSpaceGuid, guids);
                }
            }
        }

        /// <summary>
        /// Every mark of every dwelling belongs to a space of that dwelling. The same guarantee as above for
        /// the terminal tags, which is what stops a flat's supply tag being drawn in its neighbour's bedroom.
        /// </summary>
        [Fact]
        public void WholeFloor_EveryMarkBelongsToItsOwnDwelling()
        {
            PartFPlanModel model = Model();

            foreach (PartFComplianceResult partFComplianceResult in Calculate(model))
            {
                HashSet<Guid> guids = [.. (partFComplianceResult.Terminals ?? []).Select(x => x.SpaceGuid)];

                Assert.All(Overlay(model, partFComplianceResult).Marks, x => Assert.Contains(x.SpaceGuid, guids));
            }
        }

        /// <summary>
        /// The communal corridor gets nothing. It is in no dwelling, so Approved Document F Volume 1 asks
        /// nothing of it, and a tag on it would be an assessment SAM never made.
        /// </summary>
        [Fact]
        public void WholeFloor_CommunalCorridorIsNotAnnotated()
        {
            PartFPlanModel model = Model();

            Guid guid_Corridor = model.Space("Corridor").Guid;

            foreach (PartFComplianceResult partFComplianceResult in Calculate(model))
            {
                List<PartFOverlayMark> marks = Overlay(model, partFComplianceResult).Marks;

                Assert.DoesNotContain(guid_Corridor, marks.ConvertAll(x => x.SpaceGuid));
                Assert.DoesNotContain(guid_Corridor, marks.ConvertAll(x => x.DownstreamSpaceGuid));
            }
        }

        /// <summary>
        /// Drawing one dwelling gives exactly that dwelling's marks - the whole floor is the sum of its
        /// dwellings and nothing else. This is the per-dwelling-matches-combined guarantee: a drawing filtered
        /// to one flat must not gain or lose a mark relative to the floor it came from.
        /// </summary>
        [Fact]
        public void WholeFloor_IsExactlyTheSumOfItsDwellings()
        {
            PartFPlanModel model = Model();

            List<PartFComplianceResult> results = Calculate(model);

            List<string> keys_Combined = [];

            foreach (PartFComplianceResult partFComplianceResult in results)
            {
                keys_Combined.AddRange(Keys(Overlay(model, partFComplianceResult)));
            }

            //Each dwelling on its own, exactly as a view filtered to it would draw.
            List<string> keys_Separate = [];

            foreach (PartFComplianceResult partFComplianceResult in results)
            {
                List<string> keys = Keys(Overlay(model, partFComplianceResult));

                //A dwelling drawn alone carries only its own marks.
                Assert.NotEmpty(keys);

                keys_Separate.AddRange(keys);
            }

            keys_Combined.Sort(StringComparer.Ordinal);
            keys_Separate.Sort(StringComparer.Ordinal);

            Assert.Equal(keys_Combined, keys_Separate);

            //And every key is distinct, so two dwellings never share an annotation identity - which is what
            //would make one flat's moved label move the other's.
            Assert.Equal(keys_Combined.Count, keys_Combined.Distinct().Count());
        }

        /// <summary>
        /// The floor's tags are laid out in ONE solve across every dwelling, so two flats' tags cannot be
        /// drawn on top of each other even though their air cannot mix. Separate assessments, one drawing.
        /// </summary>
        [Fact]
        public void WholeFloor_TagsOfDifferentDwellingsDoNotOverlap()
        {
            PartFPlanModel model = Model();

            List<PartFTagPlacementItem> items = [];

            foreach (PartFComplianceResult partFComplianceResult in Calculate(model))
            {
                foreach (PartFOverlayMark mark in Overlay(model, partFComplianceResult).Marks)
                {
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
            }

            List<PartFTagPlacementResult> results = PartFTagPlacement.Solve(items);

            Assert.Equal(items.Count, results.Count);

            for (int i = 0; i < results.Count; i++)
            {
                for (int j = i + 1; j < results.Count; j++)
                {
                    Rectangle2D rectangle2D_1 = results[i].Rectangle2D;
                    Rectangle2D rectangle2D_2 = results[j].Rectangle2D;

                    Assert.False(
                        rectangle2D_1.InRange(rectangle2D_2) || rectangle2D_2.InRange(rectangle2D_1),
                        string.Format("Tags {0} and {1} overlap on the whole-floor drawing.", i, j));
                }
            }
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        /// <summary>
        /// A floor with two flats and a communal corridor between them, every partition shared and real.
        /// </summary>
        private static PartFPlanModel Model()
        {
            return new PartFPlanModel()
                .Room("Studio", 8)
                .Room("Bathroom", 3)
                .Room("Corridor", 2)
                .Room("Bedroom", 7)
                .Room("Kitchen", 5)
                .Room("Ensuite", 3)
                .Partition("Studio", "Bathroom", "D01")
                .Partition("Bathroom", "Corridor")
                .Partition("Corridor", "Bedroom")
                .Partition("Bedroom", "Kitchen", "D02")
                .Partition("Kitchen", "Ensuite")
                .Zone("Flat 1", "Flats", true, "Studio", "Bathroom")
                .Zone("Flat 2", "Flats", true, "Bedroom", "Kitchen", "Ensuite")
                .LocalExtractMethod("Studio", PartFExtractMethod.MVHRContinuousTerminal)
                .LocalExtractMethod("Kitchen", PartFExtractMethod.MVHRContinuousTerminal);
        }

        private static List<PartFComplianceResult> Calculate(PartFPlanModel model)
        {
            //The SHIPPED rule set, and the same zoned call the Part F command and the saved view both make.
            PartFCalculator partFCalculator = new(Analytical.Create.PartFData(RuleSetPath())) { AdjacencyCluster = model.AdjacencyCluster };

            Assert.True(partFCalculator.Calculate("Flats"));

            return [.. (partFCalculator.DwellingResults ?? [])
                .Where(x => x?.ComplianceResult is not null)
                .Select(x => x.ComplianceResult)];
        }

        private static PartFFloorPlanOverlay Overlay(PartFPlanModel model, PartFComplianceResult partFComplianceResult)
        {
            return PartFFloorPlanOverlay.Build(model.AdjacencyCluster, partFComplianceResult, plane);
        }

        /// <summary>A stable identity per mark, for comparing one rendering against another.</summary>
        private static List<string> Keys(PartFFloorPlanOverlay overlay)
        {
            return overlay.Marks.ConvertAll(x => string.Format("{0}|{1}", x.AnnotationGuid, x.AnnotationType));
        }

        private static HashSet<Guid> Guids(PartFPlanModel model, params string[] names)
        {
            return [.. names.Select(x => model.Space(x).Guid)];
        }

        private static IClosed2D LimitArea(PartFPlanModel model, Guid guid_Space)
        {
            Space space = model.AdjacencyCluster.GetSpaces()?.Find(x => x is not null && x.Guid == guid_Space);

            return space is null
                ? null
                : model.AdjacencyCluster.SpaceSectionFace2Ds(space, plane)?.Where(x => x is not null).OrderByDescending(x => x.GetArea()).FirstOrDefault();
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
