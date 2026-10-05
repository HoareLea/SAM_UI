// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.UI.WPF.Tests.Helpers;
using SAM.Core.UI;
using SAM.Geometry.Object;
using SAM.Geometry.Object.Spatial;
using SAM.Geometry.Spatial;
using SAM.Geometry.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// The floor plan's space labels on a plan whose sections crowd into one small patch - the shape of the
    /// <c>Level 0 [20.1m]</c> view behind SAM_UI #58 - solved end to end through the real caller,
    /// <c>ToSAM_GeometryObjectModel</c>, from real box-shaped spaces sectioned on the plan's plane.
    /// <para>
    /// The label solver used to spend its whole work budget on such a plan and then drop the remaining labels
    /// untested at their anchors, on top of one another and of the labels it had placed. That is what these
    /// tests catch: every label the plan draws must have been genuinely placed, so no two of them overlap. The
    /// cost itself is locked in SAM (Solver2DTests); this is the proof that the floor plan, as built, gets the
    /// benefit.
    /// </para>
    /// </summary>
    [Collection(WpfCollection.Name)]
    public class FloorPlanClusteredLabelTests
    {
        private const double Height_M = 3;

        private static readonly string[] words =
        [
            "WC",
            "Store",
            "Office",
            "Kitchen",
            "Meeting Room",
            "Plant Room North",
            "Circulation Corridor",
            "Open Plan Office West Wing",
        ];

        private readonly ITestOutputHelper testOutputHelper;

        public FloorPlanClusteredLabelTests(ITestOutputHelper testOutputHelper)
        {
            this.testOutputHelper = testOutputHelper;
        }

        /// <summary>
        /// 400 rooms of 20 m whose centres all fall inside a 2 m square: no label the plan draws overlaps
        /// another, because every one of them was placed by the search rather than dropped at its anchor.
        /// </summary>
        [Fact]
        public void A_tightly_clustered_plan_draws_no_label_on_top_of_another()
        {
            AnalyticalModel analyticalModel = Model(400);

            List<Label> labels = Labels(analyticalModel, out TwoDimensionalViewSettings twoDimensionalViewSettings);
            List<Label> labels_Drawn = labels.FindAll(x => !string.IsNullOrEmpty(x.Text));

            testOutputHelper.WriteLine(string.Format("{0} spaces, {1} labels, {2} drawn", 400, labels.Count, labels_Drawn.Count));

            Assert.Equal(400, labels.Count);
            Assert.NotEmpty(labels_Drawn);

            List<Space> spaces = analyticalModel.AdjacencyCluster.GetSpaces();
            List<(Label Label, double MinX, double MinY, double MaxX, double MaxY)> boxes = labels_Drawn.ConvertAll(x => Box(x, spaces.Find(y => y.Guid == x.Guid), twoDimensionalViewSettings));

            for (int i = 0; i < boxes.Count; i++)
            {
                for (int j = i + 1; j < boxes.Count; j++)
                {
                    Assert.False(Overlap(boxes[i], boxes[j]), string.Format("'{0}' at ({1:F3}, {2:F3}) is drawn on top of '{3}' at ({4:F3}, {5:F3})", boxes[i].Label.Text, boxes[i].Label.X, boxes[i].Label.Y, boxes[j].Label.Text, boxes[j].Label.X, boxes[j].Label.Y));
                }
            }
        }

        /// <summary>
        /// The same clustered plan drawn by two separate views - so neither can answer from the other's label
        /// cache - puts every label in the same place with the same text.
        /// </summary>
        [Fact]
        public void A_tightly_clustered_plan_draws_its_labels_in_the_same_places_every_time()
        {
            AnalyticalModel analyticalModel = Model(400);

            List<Label> labels_1 = Labels(analyticalModel, out _);
            List<Label> labels_2 = Labels(analyticalModel, out _);

            Assert.Equal(labels_1.Count, labels_2.Count);

            Dictionary<Guid, Label> dictionary = labels_2.ToDictionary(x => x.Guid);
            foreach (Label label_1 in labels_1)
            {
                Label label_2 = dictionary[label_1.Guid];

                Assert.Equal(label_1.Text, label_2.Text);
                Assert.Equal(label_1.X, label_2.X, 9);
                Assert.Equal(label_1.Y, label_2.Y, 9);
            }
        }

        private sealed class Label
        {
            public Guid Guid { get; set; }

            public string Text { get; set; }

            public double X { get; set; }

            public double Y { get; set; }
        }

        /// <summary>The label of every space, read back from what the floor plan built.</summary>
        private static List<Label> Labels(AnalyticalModel analyticalModel, out TwoDimensionalViewSettings twoDimensionalViewSettings)
        {
            //A new view each time: the floor plan caches solved labels per view, and a cached answer would not
            //run the solver at all.
            twoDimensionalViewSettings = new TwoDimensionalViewSettings(Guid.NewGuid(), "Clustered Level", Geometry.Spatial.Create.Plane(Height_M / 2), null, [typeof(Space)], Geometry.Object.Query.DefaultTextAppearance(), null);

            GeometryObjectModel geometryObjectModel = analyticalModel.ToSAM_GeometryObjectModel(twoDimensionalViewSettings);

            List<Label> result = [];
            foreach (Geometry3DObjectCollection geometry3DObjectCollection in geometryObjectModel.GetSAMGeometryObjects<Geometry3DObjectCollection>())
            {
                foreach (Text3DObject text3DObject in geometry3DObjectCollection.OfType<Text3DObject>())
                {
                    if (text3DObject.Tag?.Value is not Space space)
                    {
                        continue;
                    }

                    Point3D point3D = text3DObject.Plane.Origin;

                    result.Add(new Label() { Guid = space.Guid, Text = text3DObject.Text, X = point3D.X, Y = point3D.Y });
                }
            }

            return result;
        }

        /// <summary>
        /// The label's rectangle as the floor plan sizes it - the space name measured in its text appearance -
        /// centred where the floor plan drew it, shrunk by a millimetre so labels that merely touch, which the
        /// solver allows no closer than, do not count as overlapping.
        /// </summary>
        private static (Label Label, double MinX, double MinY, double MaxX, double MaxY) Box(Label label, Space space, TwoDimensionalViewSettings twoDimensionalViewSettings)
        {
            Geometry.Object.TextAppearance textAppearance = SAM.Analytical.UI.Query.TextAppearance(space, twoDimensionalViewSettings);

            double height = textAppearance.Height;
            double width = Core.UI.WPF.Query.Width(space.Name, new System.Drawing.Font(textAppearance.FontFamilyName, System.Convert.ToSingle(height)), height);

            const double shrink = 0.001;

            return (label, label.X - (width / 2) + shrink, label.Y - (height / 2) + shrink, label.X + (width / 2) - shrink, label.Y + (height / 2) - shrink);
        }

        private static bool Overlap((Label Label, double MinX, double MinY, double MaxX, double MaxY) box_1, (Label Label, double MinX, double MinY, double MaxX, double MaxY) box_2)
        {
            return box_1.MinX < box_2.MaxX && box_2.MinX < box_1.MaxX && box_1.MinY < box_2.MaxY && box_2.MinY < box_1.MaxY;
        }

        /// <summary>
        /// Rooms of 20 m by 20 m, each a closed box of its own, with their centres spread deterministically over
        /// a 2 m square, and names of mixed length so the labels are of mixed width, as on a real plan.
        /// </summary>
        private static AnalyticalModel Model(int count)
        {
            AdjacencyCluster adjacencyCluster = new();

            for (int i = 0; i < count; i++)
            {
                double x = Spread(i, 0.6180339887);
                double y = Spread(i, 0.7548776662);

                Space space = new(string.Format("{0} {1:000}", words[i % words.Length], i), new Point3D(x, y, Height_M / 2));
                adjacencyCluster.AddObject(space);

                double x0 = x - 10;
                double x1 = x + 10;
                double y0 = y - 10;
                double y1 = y + 10;

                AddPanel(adjacencyCluster, space, PanelType.Floor, new Polygon3D([new Point3D(x0, y0, 0), new Point3D(x1, y0, 0), new Point3D(x1, y1, 0), new Point3D(x0, y1, 0)]));
                AddPanel(adjacencyCluster, space, PanelType.Roof, new Polygon3D([new Point3D(x0, y0, Height_M), new Point3D(x1, y0, Height_M), new Point3D(x1, y1, Height_M), new Point3D(x0, y1, Height_M)]));
                AddPanel(adjacencyCluster, space, PanelType.WallExternal, new Polygon3D([new Point3D(x0, y0, 0), new Point3D(x1, y0, 0), new Point3D(x1, y0, Height_M), new Point3D(x0, y0, Height_M)]));
                AddPanel(adjacencyCluster, space, PanelType.WallExternal, new Polygon3D([new Point3D(x1, y0, 0), new Point3D(x1, y1, 0), new Point3D(x1, y1, Height_M), new Point3D(x1, y0, Height_M)]));
                AddPanel(adjacencyCluster, space, PanelType.WallExternal, new Polygon3D([new Point3D(x1, y1, 0), new Point3D(x0, y1, 0), new Point3D(x0, y1, Height_M), new Point3D(x1, y1, Height_M)]));
                AddPanel(adjacencyCluster, space, PanelType.WallExternal, new Polygon3D([new Point3D(x0, y1, 0), new Point3D(x0, y0, 0), new Point3D(x0, y0, Height_M), new Point3D(x0, y1, Height_M)]));
            }

            return new AnalyticalModel("Clustered", null, null, null, adjacencyCluster, new Core.MaterialLibrary("Materials"), new ProfileLibrary("Profiles"));
        }

        private static void AddPanel(AdjacencyCluster adjacencyCluster, Space space, PanelType panelType, Polygon3D polygon3D)
        {
            Panel panel = Analytical.Create.Panel(new Construction(Guid.NewGuid(), panelType.ToString()), panelType, new Face3D(polygon3D));

            adjacencyCluster.AddObject(panel);
            adjacencyCluster.AddRelation(space, panel);
        }

        /// <summary>A deterministic spread in [-1, 1].</summary>
        private static double Spread(int index, double factor)
        {
            double value = index * factor;

            return (2 * (value - Math.Floor(value))) - 1;
        }
    }
}
