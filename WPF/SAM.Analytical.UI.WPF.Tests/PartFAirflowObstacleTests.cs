// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.UI.WPF;
using SAM.Geometry.Object;
using SAM.Geometry.Object.Spatial;
using SAM.Geometry.Planar;
using SAM.Geometry.Spatial;
using System.Collections.Generic;
using System.Drawing;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// Regression tests for the text obstacles a Part F layout keeps its tags clear of.
    /// <para>
    /// A camera-only or attribute-only update regenerates no geometry, and the plan the tags are laid out
    /// over still shows the room names of the previous load. Resolving the obstacles to an empty list in
    /// that case re-placed the tags on top of labels that were still drawn. The resolution now keeps the
    /// previous load's obstacles where no replacement geometry was supplied, and re-measures them from the
    /// supplied geometry where one was.
    /// </para>
    /// </summary>
    public class PartFAirflowObstacleTests
    {
        /// <summary>The plan is cut at 1.2 m, the same height the assessment window uses.</summary>
        private static readonly Plane plane = Geometry.Spatial.Create.Plane(1.2);

        /// <summary>
        /// No replacement geometry means the previous load's text layout still stands, and is kept: the tags
        /// must keep clear of the labels the plan is still drawing. Nothing previously measured resolves to
        /// nothing rather than to null.
        /// </summary>
        [Fact]
        public void NullGeometry_KeepsThePreviousLoadsObstacles()
        {
            Rectangle2D rectangle2D = new(new Point2D(1, 1), 2, 1);

            List<IClosed2D> result = PartFAirflowRenderer.ResolveTextObstacles(null, plane, [rectangle2D]);

            IClosed2D obstacle = Assert.Single(result);

            Assert.Same(rectangle2D, obstacle);

            Assert.Empty(PartFAirflowRenderer.ResolveTextObstacles(null, plane, null));
        }

        /// <summary>
        /// A replacement geometry is measured, and the previous load's obstacles are replaced by what the
        /// new plan actually draws - never kept alongside, and never preserved in place of the new text.
        /// </summary>
        [Fact]
        public void ValidGeometry_ReplacesThePreviousLoadsObstacles()
        {
            Text3DObject text3DObject = new(
                "Kitchen",
                new Plane(new Point3D(2, 3, 1.2), new Vector3D(1, 0, 0), new Vector3D(0, 1, 0)),
                new TextAppearance(Color.Black, 0.5, "Arial"));

            Geometry3DObjectCollection geometry3DObjectCollection = new();
            geometry3DObjectCollection.Add(text3DObject);

            GeometryObjectModel geometryObjectModel = new();
            geometryObjectModel.Add(geometry3DObjectCollection);

            Rectangle2D rectangle2D_Previous = new(new Point2D(0, 0), 1, 1);

            List<IClosed2D> result = PartFAirflowRenderer.ResolveTextObstacles(geometryObjectModel, plane, [rectangle2D_Previous]);

            Rectangle2D obstacle = Assert.IsType<Rectangle2D>(Assert.Single(result));

            Assert.NotSame(rectangle2D_Previous, obstacle);

            //Centred on the text the plan draws, so the box reserved is the box the words fill.
            Point2D centroid = obstacle.GetCentroid();

            Assert.Equal(2, centroid.X, 6);
            Assert.Equal(3, centroid.Y, 6);
        }
    }
}
