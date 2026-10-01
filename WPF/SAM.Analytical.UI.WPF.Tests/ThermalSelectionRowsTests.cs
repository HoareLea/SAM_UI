// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.UI.WPF.Tests.Helpers;
using SAM.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>B0 docking spike: what the read-only Thermal Performance panel shows for a selection, and that it never writes the model.</summary>
    public class ThermalSelectionRowsTests
    {
        private static List<SAMObject> Panels(AnalyticalModel analyticalModel, Construction construction, int count)
        {
            return analyticalModel.AdjacencyCluster.GetPanels(construction).Take(count).Cast<SAMObject>().ToList();
        }

        [Fact]
        public void SelectedPanels_AreGroupedByConstruction_WithTheTypeTheSelectedCountAndTheUsedByCount()
        {
            Construction other = UValueFixture.Wall("OTHER");
            AnalyticalModel analyticalModel = UValueFixture.Model(out Construction construction, 12, 2, other);

            List<SAMObject> selected = Panels(analyticalModel, construction, 3).Concat(Panels(analyticalModel, other, 1)).ToList();
            List<ThermalSelectionRow> rows = Query.ThermalSelectionRows(analyticalModel, selected);

            Assert.Equal(2, rows.Count);
            ThermalSelectionRow row = rows.First(x => x.ConstructionName == "SIM_EXT_SLD");
            Assert.Equal("Panel", row.Kind);
            Assert.Equal(construction.Guid, row.ConstructionGuid);
            Assert.Equal(3, row.SelectedCount);
            Assert.Equal(14, row.UsedByCount); // 12 walls and 2 roofs use it
            Assert.Equal("Panel (" + row.Type + ")", row.Title);
            Assert.Equal("14 use it (3 selected)", row.Detail);
            Assert.Equal(1, rows.First(x => x.ConstructionName == "OTHER").SelectedCount);
        }

        [Fact]
        public void SelectedApertures_AreGroupedByApertureConstruction()
        {
            AnalyticalModel analyticalModel = GlazingFixture.Model();
            List<SAMObject> selected = analyticalModel.AdjacencyCluster.GetApertures().Take(4).Cast<SAMObject>().ToList();

            List<ThermalSelectionRow> rows = Query.ThermalSelectionRows(analyticalModel, selected);

            ThermalSelectionRow row = Assert.Single(rows);
            Assert.Equal("Aperture", row.Kind);
            Assert.Equal("GLZ", row.ConstructionName);
            Assert.Equal(4, row.SelectedCount);
            Assert.Equal(20, row.UsedByCount);
            Assert.Equal("Aperture (Window)", row.Title);
        }

        [Fact]
        public void NothingSelected_OtherObjects_AndNoModel_GiveNoRows()
        {
            AnalyticalModel analyticalModel = UValueFixture.Model(out Construction _);

            Assert.Empty(Query.ThermalSelectionRows(analyticalModel, new List<SAMObject>()));
            Assert.Empty(Query.ThermalSelectionRows(analyticalModel, null));
            Assert.Empty(Query.ThermalSelectionRows(null, Panels(analyticalModel, analyticalModel.AdjacencyCluster.GetConstructions().First(), 1)));
            Assert.Empty(Query.ThermalSelectionRows(analyticalModel, new List<SAMObject> { new Space("room", new Geometry.Spatial.Point3D(0, 0, 0)) }));
        }

        [Fact]
        public void TheSelectedCount_DoesNotCountAnElementTwice_AndNeverWritesTheModel()
        {
            AnalyticalModel analyticalModel = UValueFixture.Model(out Construction construction);
            string before = analyticalModel.ToJsonObject().ToJsonString();
            List<SAMObject> selected = Panels(analyticalModel, construction, 2);
            selected.AddRange(selected.ToList());

            ThermalSelectionRow row = Assert.Single(Query.ThermalSelectionRows(analyticalModel, selected));

            Assert.Equal(2, row.SelectedCount);
            Assert.Equal(before, analyticalModel.ToJsonObject().ToJsonString());
        }
    }
}
