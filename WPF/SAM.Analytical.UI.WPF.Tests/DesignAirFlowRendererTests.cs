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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// <see cref="DesignAirFlowRenderer"/>'s placement: that it is solved through the SAME shared
    /// <see cref="PartFTagPlacement"/> adapter Part F's own tags use (no second collision solver), that a
    /// crowded room's SUP/EXT/NET marks come apart, that this overlay's tags never land on a
    /// <see cref="PartFAirflowRenderer"/>'s already-solved ones on the same plan, and that the relationship
    /// is one-directional - Part F's own layout never depends on this overlay.
    /// <para>
    /// Headless, matching <see cref="PartFTagPlacementTests"/>: placement correctness has to be checked on
    /// every build, not in a screenshot.
    /// </para>
    /// </summary>
    [Collection(WpfCollection.Name)]
    public class DesignAirFlowRendererTests
    {
        private static readonly Plane plane = Geometry.Spatial.Create.Plane(1.2);

        // ------------------------------------------------------------------
        // Fixture
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

        private static void AddTerminal(AdjacencyCluster adjacencyCluster, Space space, FlowClassification flowClassification, double designFlowRate_Lps, string name)
        {
            VentilationTerminal ventilationTerminal = new(name, flowClassification, designFlowRate_Lps);

            adjacencyCluster.AddObject(ventilationTerminal);
            adjacencyCluster.AddRelation(ventilationTerminal, space);
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

        // ------------------------------------------------------------------
        // A crowded room - the same solver Part F uses, not a second one
        // ------------------------------------------------------------------

        /// <summary>
        /// A studio with a supply, an extract and a net mark - a coarse annotation scale so the tags are
        /// far larger, in plane units, than the overlay's own 0.55 m fan can separate on its own. If this
        /// overlay still had no placement solver (the pre-fix renderer drew every mark at a fixed screen
        /// offset from its own anchor), these three would overlap; through the shared solver they must not.
        /// </summary>
        [WpfFact]
        public void Place_SupplyExtractAndNet_InOneCrowdedRoom_DoNotOverlap()
        {
            (FloorPlan2DControl control, AdjacencyCluster adjacencyCluster, Space space) = Build();

            AddTerminal(adjacencyCluster, space, FlowClassification.Supply, 150.0, "Design Supply");
            AddTerminal(adjacencyCluster, space, FlowClassification.Extract, 82.5, "Design Extract");

            DesignAirFlowRenderer designRenderer = new(control)
            {
                ViewSettings = new DesignAirFlowViewSettings { Enabled = true, ShowSupply = true, ShowExtract = true, ShowNet = true },
            };

            //A coarser-than-default annotation scale - 1:150, against Part F's own 1:50 default - so a
            //tag's height in plane units (about 0.6 m here) exceeds the 0.55 m the overlay fans
            //identical-anchor marks by, without exceeding the room's own 8 x 5 m footprint, so the solver
            //has somewhere real to put every tag rather than being asked for the geometrically impossible.
            designRenderer.Load(adjacencyCluster, null, null, annotationScale: 150);

            List<Rectangle2D> rectangle2Ds = designRenderer.Marks.ConvertAll(x => designRenderer.Placement(x)?.Rectangle2D);

            Assert.Equal(3, rectangle2Ds.Count);
            Assert.All(rectangle2Ds, Assert.NotNull);

            AssertNoOverlap(rectangle2Ds);
        }

        // ------------------------------------------------------------------
        // Cross-overlay: the substantive readability fix
        // ------------------------------------------------------------------

        /// <summary>
        /// A studio carrying BOTH a Part F requirement and a Ventilation Design duty - anchored at the same
        /// point, since both overlays anchor a terminal mark at the space's own internal point. Naive
        /// placement (each overlay solved on its own, unaware of the other) would land the design tag
        /// straight on top of Part F's. Handing Part F's already-solved rectangle to this renderer as an
        /// obstacle - see <see cref="PartFAirflowRenderer.PlacedRectangle2Ds"/> - must keep them apart,
        /// without the two renderers merging into one and without a second collision solver.
        /// </summary>
        [WpfFact]
        public void Place_DesignTags_DoNotOverlapPartFsAlreadySolvedTagsOnTheSamePlan()
        {
            (FloorPlan2DControl control, AdjacencyCluster adjacencyCluster, Space space) = Build();

            AddTerminal(adjacencyCluster, space, FlowClassification.Supply, 150.0, "Design Supply");
            AddTerminal(adjacencyCluster, space, FlowClassification.Extract, 82.5, "Design Extract");

            PartFAirflowRenderer partFRenderer = new(control)
            {
                ViewSettings = new PartFAirflowViewSettings { Enabled = true, ShowSupply = true, ShowValues = true },
            };

            partFRenderer.Load(adjacencyCluster, [BuildComplianceResult(space, 38.8)]);

            List<Rectangle2D> partFRectangle2Ds = partFRenderer.PlacedRectangle2Ds();
            Assert.NotEmpty(partFRectangle2Ds);

            DesignAirFlowRenderer designRenderer = new(control)
            {
                ViewSettings = new DesignAirFlowViewSettings { Enabled = true, ShowSupply = true, ShowExtract = true, ShowNet = true },
            };

            //Part F's own default annotation scale - matching what AnalyticalWindow.VentilationDesign.cs
            //hands this renderer in production. The forcing condition here is not tag size but ANCHOR
            //COINCIDENCE: this space's Part F supply terminal and this overlay's own middle (Extract) mark
            //both anchor at the space's single internal point, so naive placement would land one directly
            //on the other regardless of how big either tag is.
            designRenderer.Load(adjacencyCluster, null, partFRectangle2Ds, annotationScale: PartFTagPlacement.DefaultAnnotationScale);

            List<Rectangle2D> designRectangle2Ds = designRenderer.Marks.ConvertAll(x => designRenderer.Placement(x)?.Rectangle2D);

            Assert.Equal(3, designRectangle2Ds.Count);
            Assert.All(designRectangle2Ds, Assert.NotNull);

            foreach (Rectangle2D partFRectangle2D in partFRectangle2Ds)
            {
                foreach (Rectangle2D designRectangle2D in designRectangle2Ds)
                {
                    Assert.False(
                        partFRectangle2D.InRange(designRectangle2D) || designRectangle2D.InRange(partFRectangle2D),
                        "A Ventilation Design tag overlapped one of Part F's already-solved tags.");
                }
            }

            //And neither renderer erased or altered the marks the other one owns.
            Assert.NotEmpty(partFRenderer.Marks);
            Assert.NotEmpty(designRenderer.Marks);
        }

        /// <summary>
        /// <b>The relationship is one-directional.</b> Part F's own solved position for a mark must be
        /// bit-for-bit identical whether or not a <see cref="DesignAirFlowRenderer"/> exists at all on the
        /// same plan - it is the regulatory figure, and it must not depend on which other overlays a person
        /// happens to have switched on. This is the behavioural counterpart to
        /// <see cref="PartFPlace_NeverReachesDesignAirflowRenderer"/> below.
        /// </summary>
        [WpfFact]
        public void PartFsPlacement_IsIdentical_WhetherOrNotDesignOverlayExists()
        {
            (FloorPlan2DControl control_Alone, AdjacencyCluster adjacencyCluster_Alone, Space space_Alone) = Build();
            AddTerminal(adjacencyCluster_Alone, space_Alone, FlowClassification.Supply, 150.0, "Design Supply");

            PartFAirflowRenderer partFRenderer_Alone = new(control_Alone) { ViewSettings = new PartFAirflowViewSettings { Enabled = true, ShowSupply = true, ShowValues = true } };
            partFRenderer_Alone.Load(adjacencyCluster_Alone, [BuildComplianceResult(space_Alone, 38.8)]);

            (FloorPlan2DControl control_WithDesign, AdjacencyCluster adjacencyCluster_WithDesign, Space space_WithDesign) = Build();
            AddTerminal(adjacencyCluster_WithDesign, space_WithDesign, FlowClassification.Supply, 150.0, "Design Supply");

            PartFAirflowRenderer partFRenderer_WithDesign = new(control_WithDesign) { ViewSettings = new PartFAirflowViewSettings { Enabled = true, ShowSupply = true, ShowValues = true } };
            partFRenderer_WithDesign.Load(adjacencyCluster_WithDesign, [BuildComplianceResult(space_WithDesign, 38.8)]);

            DesignAirFlowRenderer designRenderer = new(control_WithDesign) { ViewSettings = new DesignAirFlowViewSettings { Enabled = true, ShowSupply = true } };
            designRenderer.Load(adjacencyCluster_WithDesign, null, partFRenderer_WithDesign.PlacedRectangle2Ds());

            Rectangle2D rectangle2D_Alone = Assert.Single(partFRenderer_Alone.PlacedRectangle2Ds());
            Rectangle2D rectangle2D_WithDesign = Assert.Single(partFRenderer_WithDesign.PlacedRectangle2Ds());

            Assert.Equal(rectangle2D_Alone.Origin.X, rectangle2D_WithDesign.Origin.X, 9);
            Assert.Equal(rectangle2D_Alone.Origin.Y, rectangle2D_WithDesign.Origin.Y, 9);
        }

        // ------------------------------------------------------------------
        // Screen-space transfer direction
        // ------------------------------------------------------------------

        /// <summary>
        /// <b>The literal screen-coordinate proof, for a real, physically balanced three-room chain.</b> A
        /// native acceptance screenshot raised a concern that a transfer arrow might point the wrong way in
        /// a three-room chain, but that screenshot's own fixture was not physically balanced (SUP 150 / EXT
        /// 90, no visible middle-room terminal to absorb the 60 l/s difference) and could not be read as
        /// evidence either way. This test does not reuse those invalid numbers - it uses equal supply and
        /// extract duties, and proves its middle room is a genuine pass-through from the CALCULATED state
        /// rather than assuming it from a room name.
        /// <para>
        /// <see cref="PartFTransferAirChainDirectionTests"/> (SAM.Tests) pins the same shape at the data
        /// layer: <c>UpstreamSpaceGuid</c>/<c>DownstreamSpaceGuid</c> and <c>SpaceAirMovement.From</c>/<c>.To</c>.
        /// Neither that test nor any other in this assembly touches
        /// <see cref="FloorPlan2DControl.WorldToScreen"/> - the one transform
        /// <see cref="PartFAirflowRenderer.Draw"/> and <see cref="DesignAirFlowRenderer.Draw"/> actually
        /// apply before painting a pixel. This test pushes the SAME marks through that SAME matrix, on a
        /// real (measured and arranged) control, and asserts on the coordinates the arrowhead is drawn AT.
        /// </para>
        /// <para>
        /// <see cref="FloorPlan2DControl.WorldToScreen"/> is documented as flipping only Y ("World Y points
        /// up, screen Y points down"). Its own construction, <c>new Matrix(scale, 0, 0, -scale, ...)</c>,
        /// never negates X - so a correct world-space Start-to-End vector pointing toward increasing X can
        /// only reach the screen still pointing toward increasing X. This test does not take that on faith;
        /// it reads the control's actual matrix and asserts <c>M11 &gt; 0</c> before trusting anything built
        /// on it.
        /// </para>
        /// <para>
        /// Bedroom, Hall and Ensuite (<see cref="PartFPlanModel.Room"/>) sit strictly left to right; Bedroom
        /// supplies, Ensuite extracts by the same amount, and Hall - a circulation space Approved Document F
        /// gives no terminal to - is proved a genuine pass-through below rather than assumed, for both the
        /// Part F and the Design authority. Both authorities are checked here, from the SAME production
        /// terminal duties, through the SAME production transfer-air generation
        /// (<c>Modify.AddPartFTransferAirMovements</c>, <c>PartFCalculator.Calculate</c>).
        /// </para>
        /// </summary>
        [WpfFact]
        public void Chain_TransferMarks_EndSitsDownstreamOfStart_OnScreen_ForBothOverlays()
        {
            PartFPlanModel model = new PartFPlanModel()
                .Room("Bedroom", 8)
                .Room("Hall", 5)
                .Room("Ensuite", 3)
                .Partition("Bedroom", "Hall", "D01")
                .Partition("Hall", "Ensuite", "D02")
                .Zone("Flat 2", "Flats", true, "Bedroom", "Hall", "Ensuite");

            //The Design authority's own terminal duties - independent of, and numerically different from,
            //whatever Part F's Table 1.2 sizing calculates for the same rooms, but physically balanced
            //(equal supply and extract) so this fixture is itself a valid scenario, unlike the native
            //acceptance screenshot's own unbalanced SUP 150 / EXT 90.
            AddTerminal(model.AdjacencyCluster, model.Space("Bedroom"), FlowClassification.Supply, 150.0, "Design Supply");
            AddTerminal(model.AdjacencyCluster, model.Space("Ensuite"), FlowClassification.Extract, 150.0, "Design Extract");

            //The Design pass-through proof: Hall carries no Design supply or extract terminal of its own.
            Assert.Empty(model.AdjacencyCluster.VentilationTerminals(model.Space("Hall")));

            List<SpaceAirMovement> spaceAirMovements = model.AdjacencyCluster.AddPartFTransferAirMovements(
                null, model.AdjacencyCluster.GetSpaces(), out _, out List<string> refusals);

            Assert.True(refusals is null || refusals.Count == 0, string.Join(" ", refusals ?? []));
            Assert.NotEmpty(spaceAirMovements);

            PartFCalculator partFCalculator = new(Analytical.Create.PartFData(RuleSetPath())) { AdjacencyCluster = model.AdjacencyCluster };
            Assert.True(partFCalculator.Calculate("Flats"));
            PartFComplianceResult complianceResult = partFCalculator.DwellingResults[0].ComplianceResult;

            //The Part F pass-through proof, from the CALCULATED state: Hall - a circulation space - carries
            //no Part F terminal requirement either, so LocalExtractMethod was never needed here and Hall is
            //not merely assumed to be a pass-through because of its name.
            Assert.DoesNotContain(complianceResult.Terminals ?? [], x => x.SpaceGuid == model.Space("Hall").Guid);

            FloorPlan2DControl control = new() { Width = 800, Height = 600 };
            control.Measure(new System.Windows.Size(800, 600));
            control.Arrange(new System.Windows.Rect(0, 0, 800, 600));

            GeometryObjectModel geometryObjectModel = new();
            geometryObjectModel.SetValue(GeometryObjectModelParameter.ViewSettings, new TwoDimensionalViewSettings(
                Guid.NewGuid(), "Level 0 [1.2m]", plane, null, [typeof(Space)], Geometry.Object.Query.DefaultTextAppearance(), null));

            control.Load(geometryObjectModel);
            control.ZoomExtents();

            //Verified, not assumed: the transform this whole test rests on must not itself be the thing
            //flipping the picture.
            Assert.True(control.WorldToScreen.M11 > 0, "The world-to-screen transform unexpectedly flips X - every assertion below would be meaningless.");

            PartFAirflowRenderer partFRenderer = new(control) { ViewSettings = new PartFAirflowViewSettings { Enabled = true, ShowSupply = true, ShowValues = true } };
            partFRenderer.Load(model.AdjacencyCluster, [complianceResult]);

            DesignAirFlowRenderer designRenderer = new(control) { ViewSettings = new DesignAirFlowViewSettings { Enabled = true, ShowSupply = true, ShowExtract = true } };
            designRenderer.Load(model.AdjacencyCluster);

            List<PartFOverlayMark> partFTransferMarks = [.. partFRenderer.Marks.Where(x => x.IsTransfer)];
            List<DesignAirFlowOverlayMark> designTransferMarks = [.. designRenderer.Marks.Where(x => x.MarkType == DesignAirFlowMarkType.Transfer)];

            Assert.Equal(2, partFTransferMarks.Count);
            Assert.Equal(2, designTransferMarks.Count);

            foreach (PartFOverlayMark mark in partFTransferMarks)
            {
                System.Windows.Point screen_Start = control.WorldToScreen.Transform(new System.Windows.Point(mark.Start.X, mark.Start.Y));
                System.Windows.Point screen_End = control.WorldToScreen.Transform(new System.Windows.Point(mark.End.X, mark.End.Y));

                Assert.True(screen_End.X > screen_Start.X,
                    string.Format("Part F ({0}): the arrowhead paints at screen X={1:0.#}, to the LEFT of its own tail at X={2:0.#}.", mark.DoorName, screen_End.X, screen_Start.X));
            }

            foreach (DesignAirFlowOverlayMark mark in designTransferMarks)
            {
                System.Windows.Point screen_Start = control.WorldToScreen.Transform(new System.Windows.Point(mark.Start.X, mark.Start.Y));
                System.Windows.Point screen_End = control.WorldToScreen.Transform(new System.Windows.Point(mark.End.X, mark.End.Y));

                Assert.True(screen_End.X > screen_Start.X,
                    string.Format("Design: the arrowhead paints at screen X={0:0.#}, to the LEFT of its own tail at X={1:0.#}.", screen_End.X, screen_Start.X));
            }
        }

        /// <summary>
        /// The shipped Part F rule set, found relative to this repository rather than copied into the test
        /// output: a stale copy of a rule set is exactly the kind of drift these tests exist to catch
        /// elsewhere.
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

        // ------------------------------------------------------------------
        // Annotation scale
        // ------------------------------------------------------------------

        /// <summary>
        /// The annotation scale IS a layout input for this overlay too, matching
        /// <c>PartFTagPlacementTests.Solve_DifferentAnnotationScale_LaysTagsOutDifferently</c>: a tag is a
        /// fixed size on the sheet, so a coarser scale legitimately covers more of the plan.
        /// </summary>
        [WpfFact]
        public void Place_DifferentAnnotationScale_LaysTagsOutDifferently()
        {
            (FloorPlan2DControl control_50, AdjacencyCluster adjacencyCluster_50, Space space_50) = Build();
            AddTerminal(adjacencyCluster_50, space_50, FlowClassification.Supply, 45.0, "Design Supply");

            DesignAirFlowRenderer designRenderer_50 = new(control_50) { ViewSettings = new DesignAirFlowViewSettings { Enabled = true, ShowSupply = true } };
            designRenderer_50.Load(adjacencyCluster_50, null, null, annotationScale: 50);

            (FloorPlan2DControl control_100, AdjacencyCluster adjacencyCluster_100, Space space_100) = Build();
            AddTerminal(adjacencyCluster_100, space_100, FlowClassification.Supply, 45.0, "Design Supply");

            DesignAirFlowRenderer designRenderer_100 = new(control_100) { ViewSettings = new DesignAirFlowViewSettings { Enabled = true, ShowSupply = true } };
            designRenderer_100.Load(adjacencyCluster_100, null, null, annotationScale: 100);

            Rectangle2D rectangle2D_50 = designRenderer_50.Placement(Assert.Single(designRenderer_50.Marks)).Rectangle2D;
            Rectangle2D rectangle2D_100 = designRenderer_100.Placement(Assert.Single(designRenderer_100.Marks)).Rectangle2D;

            Assert.True(rectangle2D_100.Width > rectangle2D_50.Width);
        }

        // ------------------------------------------------------------------
        // Structural: the shared solve, and nothing reached that should not be
        // ------------------------------------------------------------------

        /// <summary>
        /// This overlay's placement never reads the view transform, matching
        /// <c>PartFTagPlacementTests.Place_NeverReadsTheViewTransform</c>: it is solved for the annotation
        /// scale, in the plane's own coordinates, and only <see cref="DesignAirFlowRenderer.Draw"/>
        /// transforms it for the current zoom.
        /// </summary>
        [Fact]
        public void Place_NeverReadsTheViewTransform()
        {
            List<MethodInfo> methodInfos = Reachable(typeof(DesignAirFlowRenderer).GetMethod("Place", BindingFlags.Instance | BindingFlags.Public));

            //Positive control: the walker must actually be seeing the shared solve and the control's plane.
            Assert.Contains(typeof(PartFTagPlacement).GetMethod("Solve"), methodInfos);
            Assert.Contains(typeof(Geometry.UI.WPF.FloorPlan2DControl).GetProperty("Plane").GetGetMethod(), methodInfos);

            Assert.DoesNotContain(typeof(Geometry.UI.WPF.FloorPlan2DControl).GetProperty("WorldToScreen").GetGetMethod(), methodInfos);
        }

        /// <summary>A camera move redraws and does not re-solve, matching Part F's own rule.</summary>
        [Fact]
        public void FloorPlan_ViewChanged_RedrawsWithoutPlacing()
        {
            MethodInfo methodInfo_Place = typeof(DesignAirFlowRenderer).GetMethod("Place", BindingFlags.Instance | BindingFlags.Public);
            MethodInfo methodInfo_Draw = typeof(DesignAirFlowRenderer).GetMethod("Draw", BindingFlags.Instance | BindingFlags.Public);

            List<MethodInfo> methodInfos = Called(typeof(DesignAirFlowRenderer).GetMethod("FloorPlan2DControl_ViewChanged", BindingFlags.Instance | BindingFlags.NonPublic));

            Assert.DoesNotContain(methodInfo_Place, methodInfos);
            Assert.Contains(methodInfo_Draw, methodInfos);
        }

        /// <summary>
        /// The list of things that lay this overlay's tags out again is exactly the agreed one: loading the
        /// model (and, with it, Part F's latest obstacles) and a change of view settings - which is how a
        /// visibility toggle arrives. There is no operating-mode concept here, so unlike Part F there is no
        /// third caller.
        /// </summary>
        [Fact]
        public void Place_IsCalledOnlyByTheAgreedInputChanges()
        {
            MethodInfo methodInfo_Place = typeof(DesignAirFlowRenderer).GetMethod("Place", BindingFlags.Instance | BindingFlags.Public);

            List<string> names = [];

            foreach (MethodInfo methodInfo in typeof(DesignAirFlowRenderer).GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                if (methodInfo != methodInfo_Place && Called(methodInfo).Contains(methodInfo_Place))
                {
                    names.Add(methodInfo.Name);
                }
            }

            names.Sort(StringComparer.Ordinal);

            Assert.Equal(new List<string> { "Load", "set_ViewSettings" }, names);
        }

        /// <summary>
        /// <b>Part F never reaches into this overlay.</b> Read by walking every method
        /// <c>PartFAirflowRenderer.Place</c> can reach, transitively, within SAM's own assemblies, and
        /// proving <see cref="DesignAirFlowRenderer"/> is never among the types it lands in. This is what
        /// makes the obstacle relationship structurally one-directional, not merely true today by
        /// convention - see <see cref="PartFsPlacement_IsIdentical_WhetherOrNotDesignOverlayExists"/> for the
        /// behavioural counterpart.
        /// </summary>
        [Fact]
        public void PartFPlace_NeverReachesDesignAirflowRenderer()
        {
            List<MethodInfo> methodInfos = Reachable(typeof(PartFAirflowRenderer).GetMethod("Place", BindingFlags.Instance | BindingFlags.Public));

            Assert.DoesNotContain(methodInfos, x => x.DeclaringType == typeof(DesignAirFlowRenderer));
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        private static void AssertNoOverlap(List<Rectangle2D> rectangle2Ds)
        {
            for (int i = 0; i < rectangle2Ds.Count; i++)
            {
                for (int j = i + 1; j < rectangle2Ds.Count; j++)
                {
                    Rectangle2D rectangle2D_1 = rectangle2Ds[i];
                    Rectangle2D rectangle2D_2 = rectangle2Ds[j];

                    Assert.False(rectangle2D_1.InRange(rectangle2D_2) || rectangle2D_2.InRange(rectangle2D_1), string.Format("Tags {0} and {1} overlap.", i, j));
                }
            }
        }

        /// <summary>
        /// Every method the given method calls, read from its compiled body - the same IL walk
        /// <c>PartFTagPlacementTests</c> uses, duplicated here rather than shared across test assemblies so
        /// each test file stays self-contained.
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
                        //Not a member token this module can resolve - a constructor or a type.
                    }
                }

                i += Length(opCode, il, i);
            }

            return result;
        }

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

                    string name = methodInfo_Called.DeclaringType?.Assembly.GetName().Name;

                    if (name is not null && name.StartsWith("SAM.", StringComparison.Ordinal) && seen.Add(methodInfo_Called))
                    {
                        queue.Enqueue(methodInfo_Called);
                    }
                }
            }

            return result;
        }

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
    }
}
