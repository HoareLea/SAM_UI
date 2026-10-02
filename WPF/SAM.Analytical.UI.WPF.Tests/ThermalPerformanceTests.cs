// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

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
    /// Thermal Stage B (read-only): what the Thermal Performance panel shows - the selected panels / apertures by construction,
    /// or the whole external envelope - with the performance STORED on the model, the counts and areas, and what a click on a row
    /// highlights. The panel never writes the model and never calls Tas.
    /// </summary>
    public class ThermalPerformanceTests
    {
        private static readonly Guid WindowGuid = new Guid("c0000000-0000-4000-8000-000000000001");
        private static readonly Guid DoorGuid = new Guid("c0000000-0000-4000-8000-000000000002");

        private sealed class Fixture
        {
            public AnalyticalModel Model;
            public Construction Wall;       // 14 external wall panels (stored U 0.26, one of them 0.30) + 2 roof panels (0.15) use it
            public Guid OddWall;            // the wall panel that stores 0.30
            public Construction Partition;  // no stored U, 3 internal wall panels
            public Construction Floor;      // no stored U, 4 slab-on-grade panels
            public List<Aperture> Windows;  // 6 windows (stored U 1.243, g 0.40, LT 0.80 on all of them)
            public List<Aperture> Doors;    // 2 doors (stored U 2.0 on one only)
        }

        private static Aperture Aperture(ApertureConstruction apertureConstruction, int index, Action<Aperture> configure)
        {
            double x = index * 5;
            Aperture aperture = Analytical.Create.Aperture(apertureConstruction, new Face3D(new Polygon3D(new List<Point3D>()
            {
                new Point3D(x + 1, 0, 0.75), new Point3D(x + 3, 0, 0.75), new Point3D(x + 3, 0, 2.25), new Point3D(x + 1, 0, 2.25),
            })));
            configure?.Invoke(aperture);
            return aperture;
        }

        private static Fixture Build()
        {
            Construction wall = UValueFixture.Wall();
            Construction partition = UValueFixture.Wall("PARTITION");
            Construction floor = UValueFixture.Wall("FLOOR");

            ApertureConstruction window = GlazingFixture.System(WindowGuid, "GLZ", ApertureType.Window, GlazingFixture.Clear);
            ApertureConstruction door = GlazingFixture.System(DoorGuid, "DOOR", ApertureType.Door, GlazingFixture.Clear);

            AdjacencyCluster adjacencyCluster = new AdjacencyCluster();
            int index = 0;
            Guid oddWall = Guid.Empty;

            // 12 walls, the first 6 with a window; 2 of them additionally with a door
            for (int i = 0; i < 12; i++)
            {
                // Tas.Modify.UpdateThermalParameters stores the U-value on each panel (not on the construction)
                Panel panel = UValueFixture.Panel(wall, PanelType.WallExternal, index);
                panel.SetValue(PanelParameter.ThermalTransmittance, i == 11 ? 0.30 : 0.26);
                if (i == 11)
                {
                    oddWall = panel.Guid;
                }

                if (i < 6)
                {
                    panel.AddAperture(Aperture(window, index, x =>
                    {
                        x.SetValue(ApertureParameter.ThermalTransmittance, 1.243);
                        x.SetValue(ApertureParameter.TotalSolarEnergyTransmittance, 0.4);
                        x.SetValue(ApertureParameter.LightTransmittance, 0.8);
                    }));
                }

                adjacencyCluster.AddObject(panel);
                index++;
            }

            // (an aperture must lie inside its panel, so it takes the panel's index)
            Panel doorPanel_1 = UValueFixture.Panel(wall, PanelType.WallExternal, index);
            doorPanel_1.AddAperture(Aperture(door, index++, x => x.SetValue(ApertureParameter.ThermalTransmittance, 2.0)));
            adjacencyCluster.AddObject(doorPanel_1);
            Panel doorPanel_2 = UValueFixture.Panel(wall, PanelType.WallExternal, index);
            doorPanel_2.AddAperture(Aperture(door, index++, null));
            adjacencyCluster.AddObject(doorPanel_2);

            for (int i = 0; i < 2; i++)
            {
                Panel roof = UValueFixture.Panel(wall, PanelType.Roof, index++);
                roof.SetValue(PanelParameter.ThermalTransmittance, 0.15);
                adjacencyCluster.AddObject(roof);
            }

            for (int i = 0; i < 3; i++)
            {
                adjacencyCluster.AddObject(UValueFixture.Panel(partition, PanelType.WallInternal, index++));
            }

            for (int i = 0; i < 4; i++)
            {
                adjacencyCluster.AddObject(UValueFixture.Panel(floor, PanelType.SlabOnGrade, index++));
            }

            MaterialLibrary materials = UValueFixture.Materials();
            foreach (IMaterial material in GlazingFixture.ModelMaterials().GetMaterials())
            {
                materials.Add(material);
            }

            AnalyticalModel analyticalModel = new AnalyticalModel("Thermal", null, null, null, adjacencyCluster, materials, new ProfileLibrary("Profiles"));
            AdjacencyCluster cluster = analyticalModel.AdjacencyCluster;
            return new Fixture()
            {
                Model = analyticalModel,
                Wall = wall,
                OddWall = oddWall,
                Partition = partition,
                Floor = floor,
                Windows = cluster.GetApertures().Where(x => x.TypeGuid == WindowGuid).ToList(),
                Doors = cluster.GetApertures().Where(x => x.TypeGuid == DoorGuid).ToList(),
            };
        }

        // The odd wall panel (U 0.30) is left out unless asked for, so the other tests do not depend on the model's panel order.
        private static List<SAMObject> Panels(Fixture fixture, Construction construction, PanelType panelType, int count, bool includeOdd = false)
        {
            return fixture.Model.AdjacencyCluster.GetPanels(construction).Where(x => x.PanelType == panelType && (includeOdd || x.Guid != fixture.OddWall)).Take(count).Cast<SAMObject>().ToList();
        }

        private static ThermalPerformanceRow Row(List<ThermalPerformanceGroup> groups, string title, string name)
        {
            return groups.First(x => x.Title == title).Rows.First(x => x.ConstructionName == name);
        }

        // ---- Selection mode ----------------------------------------------------------------------------------------

        [Fact]
        public void Selection_GroupsThePanelsUnderWallsRoofsFloors_ByConstruction_WithTheSelectedAndUsedByCounts()
        {
            Fixture fixture = Build();
            List<SAMObject> selected = Panels(fixture, fixture.Wall, PanelType.WallExternal, 3).Concat(Panels(fixture, fixture.Wall, PanelType.Roof, 1)).Concat(Panels(fixture, fixture.Floor, PanelType.SlabOnGrade, 2)).ToList();

            List<ThermalPerformanceGroup> groups = Query.ThermalPerformanceGroups(fixture.Model, selected, ThermalPerformanceMode.Selection);

            Assert.Equal(new[] { "Walls", "Roofs", "Floors" }, groups.Select(x => x.Title));

            ThermalPerformanceRow walls = Row(groups, "Walls", "SIM_EXT_SLD");
            Assert.False(walls.IsAperture);
            Assert.Equal(3, walls.SelectedCount);
            Assert.Equal(16, walls.UsedByCount); // 14 external walls + 2 roofs use it, whichever group is shown
            Assert.Equal("16 use it (3 selected)", walls.Detail);
            Assert.Equal("Panel (WallExternal)", walls.Title);

            Assert.Equal(1, Row(groups, "Roofs", "SIM_EXT_SLD").SelectedCount);
            Assert.Equal(2, Row(groups, "Floors", "FLOOR").SelectedCount);
        }

        [Fact]
        public void Selection_ShowsTheStoredU_OrSaysItIsNotCalculated()
        {
            Fixture fixture = Build();
            List<SAMObject> selected = Panels(fixture, fixture.Wall, PanelType.WallExternal, 1).Concat(Panels(fixture, fixture.Floor, PanelType.SlabOnGrade, 1)).ToList();

            List<ThermalPerformanceGroup> groups = Query.ThermalPerformanceGroups(fixture.Model, selected, ThermalPerformanceMode.Selection);

            Assert.Equal("U 0.260", Row(groups, "Walls", "SIM_EXT_SLD").PerformanceText);
            Assert.Equal("U not calculated", Row(groups, "Floors", "FLOOR").PerformanceText);
        }

        [Fact]
        public void TheStoredU_IsReadFromThePanels_PerRow_AndSaysVariesWhenTheyDisagree()
        {
            Fixture fixture = Build();
            List<SAMObject> walls = Panels(fixture, fixture.Wall, PanelType.WallExternal, 14, includeOdd: true);
            List<SAMObject> roofs = Panels(fixture, fixture.Wall, PanelType.Roof, 2);

            // The same construction, two rows: the roofs store another U (another heat-flow direction) than the walls.
            List<ThermalPerformanceGroup> groups = Query.ThermalPerformanceGroups(fixture.Model, walls.Concat(roofs).ToList(), ThermalPerformanceMode.Selection);
            Assert.Equal("U varies", Row(groups, "Walls", "SIM_EXT_SLD").PerformanceText); // 13 x 0.26 and the odd one 0.30
            Assert.Equal("U 0.150", Row(groups, "Roofs", "SIM_EXT_SLD").PerformanceText);

            // Without the odd panel the walls that store a value agree again ...
            List<SAMObject> agreeing = walls.Where(x => x.Guid != fixture.OddWall && ((Panel)x).TryGetValue(PanelParameter.ThermalTransmittance, out double _)).ToList();
            Assert.Equal(11, agreeing.Count);
            Assert.Equal("U 0.260", Row(Query.ThermalPerformanceGroups(fixture.Model, agreeing, ThermalPerformanceMode.Selection), "Walls", "SIM_EXT_SLD").PerformanceText);

            // ... and a wall panel that stores none (the two with a door here) makes the row vary rather than hide it.
            List<SAMObject> withoutValue = walls.Where(x => !((Panel)x).TryGetValue(PanelParameter.ThermalTransmittance, out double _)).ToList();
            Assert.Equal(2, withoutValue.Count);
            Assert.Equal("U not calculated", Row(Query.ThermalPerformanceGroups(fixture.Model, withoutValue, ThermalPerformanceMode.Selection), "Walls", "SIM_EXT_SLD").PerformanceText);
            Assert.Equal("U varies", Row(Query.ThermalPerformanceGroups(fixture.Model, withoutValue.Concat(agreeing).ToList(), ThermalPerformanceMode.Selection), "Walls", "SIM_EXT_SLD").PerformanceText);
        }

        [Fact]
        public void Selection_Apertures_ShowStoredUgAndLightTransmittance_AndLeaveOutWhatDoorsDoNotHave()
        {
            Fixture fixture = Build();
            List<SAMObject> selected = fixture.Windows.Take(2).Concat(fixture.Doors.Take(1)).Cast<SAMObject>().ToList();

            List<ThermalPerformanceGroup> groups = Query.ThermalPerformanceGroups(fixture.Model, selected, ThermalPerformanceMode.Selection);

            Assert.Equal(new[] { "Windows", "Doors" }, groups.Select(x => x.Title));
            ThermalPerformanceRow windows = Row(groups, "Windows", "GLZ");
            Assert.True(windows.IsAperture);
            Assert.Equal("U 1.243 · g 0.40 · LT 0.80", windows.PerformanceText);
            Assert.Equal(2, windows.SelectedCount);
            Assert.Equal(6, windows.UsedByCount);
            Assert.Equal("Aperture (Window)", windows.Title);

            ThermalPerformanceRow doors = Row(groups, "Doors", "DOOR");
            Assert.Equal(1, doors.SelectedCount);
            Assert.Equal(2, doors.UsedByCount);
        }

        [Fact]
        public void ApertureValues_ThatDiffer_OrAreMissingOnSome_AreSaidToVary_AndNoneIsNotCalculated()
        {
            Fixture fixture = Build();

            // The two doors: one stores U 2.0, the other nothing - one construction, no single stored value.
            List<ThermalPerformanceGroup> both = Query.ThermalPerformanceGroups(fixture.Model, fixture.Doors.Cast<SAMObject>().ToList(), ThermalPerformanceMode.Selection);
            Assert.Equal("U varies", Row(both, "Doors", "DOOR").PerformanceText);

            // The door that stores nothing, alone: nothing stored.
            Aperture without = fixture.Doors.First(x => !x.TryGetValue(ApertureParameter.ThermalTransmittance, out double _));
            List<ThermalPerformanceGroup> alone = Query.ThermalPerformanceGroups(fixture.Model, new List<SAMObject> { without }, ThermalPerformanceMode.Selection);
            Assert.Equal("not calculated", Row(alone, "Doors", "DOOR").PerformanceText);
        }

        [Fact]
        public void Selection_IgnoresOtherObjects_Duplicates_AndAMissingModel()
        {
            Fixture fixture = Build();
            List<SAMObject> selected = Panels(fixture, fixture.Wall, PanelType.WallExternal, 2);
            selected.AddRange(selected.ToList());
            selected.Add(new Space("room", new Point3D(0, 0, 0)));

            ThermalPerformanceRow row = Assert.Single(Query.ThermalPerformanceGroups(fixture.Model, selected, ThermalPerformanceMode.Selection).SelectMany(x => x.Rows));
            Assert.Equal(2, row.SelectedCount);

            Assert.Empty(Query.ThermalPerformanceGroups(fixture.Model, null, ThermalPerformanceMode.Selection));
            Assert.Empty(Query.ThermalPerformanceGroups(null, selected, ThermalPerformanceMode.Selection));
        }

        // ---- Whole envelope ----------------------------------------------------------------------------------------

        [Fact]
        public void WholeEnvelope_ListsTheExternalPanelsAndTheirApertures_NotInternalPartitions_WithCountsAndAreas()
        {
            Fixture fixture = Build();

            List<ThermalPerformanceGroup> groups = Query.ThermalPerformanceGroups(fixture.Model, null, ThermalPerformanceMode.WholeEnvelope);

            Assert.Equal(new[] { "Walls", "Roofs", "Floors", "Windows", "Doors" }, groups.Select(x => x.Title));
            Assert.DoesNotContain(groups.SelectMany(x => x.Rows), x => x.ConstructionName == "PARTITION");

            ThermalPerformanceRow walls = Row(groups, "Walls", "SIM_EXT_SLD");
            Assert.Equal(14, walls.ElementCount);
            Assert.Equal(14 * 12.0, walls.Area, 6); // 4 x 3 m panels
            Assert.Equal(0, walls.SelectedCount);
            Assert.Equal("14 elements · 168.0 m²", walls.Detail);
            Assert.Equal(16, walls.UsedByCount);

            Assert.Equal(2, Row(groups, "Roofs", "SIM_EXT_SLD").ElementCount);
            Assert.Equal(4, Row(groups, "Floors", "FLOOR").ElementCount);
            Assert.Equal(6, Row(groups, "Windows", "GLZ").ElementCount);
            Assert.Equal(2, Row(groups, "Doors", "DOOR").ElementCount);
        }

        [Fact]
        public void WholeEnvelope_DoesNotDependOnTheSelection()
        {
            Fixture fixture = Build();

            List<ThermalPerformanceGroup> none = Query.ThermalPerformanceGroups(fixture.Model, null, ThermalPerformanceMode.WholeEnvelope);
            List<ThermalPerformanceGroup> some = Query.ThermalPerformanceGroups(fixture.Model, Panels(fixture, fixture.Partition, PanelType.WallInternal, 2), ThermalPerformanceMode.WholeEnvelope);

            Assert.Equal(none.SelectMany(x => x.Rows).Select(x => x.ConstructionName + x.Detail), some.SelectMany(x => x.Rows).Select(x => x.ConstructionName + x.Detail));
        }

        // ---- Highlight ---------------------------------------------------------------------------------------------

        [Fact]
        public void ClickingARow_InTheSelectionMode_HighlightsEveryElementUsingTheConstruction()
        {
            Fixture fixture = Build();
            ThermalPerformanceViewModel viewModel = new ThermalPerformanceViewModel();
            viewModel.Update(fixture.Model, Panels(fixture, fixture.Wall, PanelType.WallExternal, 1));

            ThermalPerformanceRow row = viewModel.Groups.SelectMany(x => x.Rows).Single();
            List<SAMObject> objects = viewModel.HighlightObjects(row);

            Assert.Equal(16, objects.Count);
            Assert.All(objects, x => Assert.IsType<Panel>(x));
            Assert.Equal(fixture.Model.AdjacencyCluster.GetPanels(fixture.Wall).Select(x => x.Guid).OrderBy(x => x), objects.Select(x => x.Guid).OrderBy(x => x));
        }

        [Fact]
        public void ClickingARow_InTheWholeEnvelopeMode_HighlightsTheEnvelopeElementsOfTheRow()
        {
            Fixture fixture = Build();
            ThermalPerformanceViewModel viewModel = new ThermalPerformanceViewModel() { Mode = ThermalPerformanceMode.WholeEnvelope };
            viewModel.Update(fixture.Model, null);

            List<SAMObject> walls = viewModel.HighlightObjects(viewModel.Groups.First(x => x.Title == "Walls").Rows.Single());
            Assert.Equal(14, walls.Count);
            Assert.All(walls, x => Assert.Equal(PanelType.WallExternal, ((Panel)x).PanelType));

            List<SAMObject> windows = viewModel.HighlightObjects(viewModel.Groups.First(x => x.Title == "Windows").Rows.Single());
            Assert.Equal(6, windows.Count);
            Assert.All(windows, x => Assert.IsType<Aperture>(x));
        }

        // ---- The view-model ----------------------------------------------------------------------------------------

        [Fact]
        public void TheSummary_SaysWhatIsShown_ForEveryState()
        {
            Fixture fixture = Build();
            ThermalPerformanceViewModel viewModel = new ThermalPerformanceViewModel();

            Assert.Equal("No model.", viewModel.Summary);

            viewModel.Update(fixture.Model, null);
            Assert.Equal("Select panels or apertures in the view.", viewModel.Summary);
            Assert.False(viewModel.HasRows);

            viewModel.Update(fixture.Model, Panels(fixture, fixture.Wall, PanelType.WallExternal, 3).Concat(Panels(fixture, fixture.Floor, PanelType.SlabOnGrade, 1)).ToList());
            Assert.Equal("4 elements selected · 2 constructions", viewModel.Summary);
            Assert.True(viewModel.HasRows);

            viewModel.Update(fixture.Model, Panels(fixture, fixture.Wall, PanelType.WallExternal, 1));
            Assert.Equal("1 element selected · 1 construction", viewModel.Summary);

            viewModel.Mode = ThermalPerformanceMode.WholeEnvelope;
            Assert.Equal("External envelope · 5 constructions · 28 elements", viewModel.Summary);
        }

        [Fact]
        public void ChangingTheModeOrTheModel_RebuildsAndRaisesChanged()
        {
            Fixture fixture = Build();
            ThermalPerformanceViewModel viewModel = new ThermalPerformanceViewModel();
            int changed = 0;
            viewModel.Changed += (sender, e) => changed++;

            viewModel.Update(fixture.Model, Panels(fixture, fixture.Wall, PanelType.WallExternal, 1));
            viewModel.Mode = ThermalPerformanceMode.WholeEnvelope;
            viewModel.Mode = ThermalPerformanceMode.WholeEnvelope; // no change, no event
            viewModel.Mode = ThermalPerformanceMode.Selection;

            Assert.Equal(3, changed);

            // A different model: nothing of the previous one is kept.
            AnalyticalModel other = UValueFixture.Model(out Construction _, 2);
            viewModel.Update(other, null);
            Assert.Empty(viewModel.Groups);
        }

        // ---- Read-only ---------------------------------------------------------------------------------------------

        [Fact]
        public void TheQueries_NeverWriteTheModel_InEitherMode()
        {
            Fixture fixture = Build();
            string before = fixture.Model.ToJsonObject().ToJsonString();

            ThermalPerformanceViewModel viewModel = new ThermalPerformanceViewModel();
            viewModel.Update(fixture.Model, fixture.Windows.Cast<SAMObject>().Concat(Panels(fixture, fixture.Wall, PanelType.WallExternal, 2)).ToList());
            foreach (ThermalPerformanceRow row in viewModel.Groups.SelectMany(x => x.Rows))
            {
                viewModel.HighlightObjects(row);
            }

            viewModel.Mode = ThermalPerformanceMode.WholeEnvelope;
            foreach (ThermalPerformanceRow row in viewModel.Groups.SelectMany(x => x.Rows))
            {
                viewModel.HighlightObjects(row);
            }

            Assert.Equal(before, fixture.Model.ToJsonObject().ToJsonString());
        }
    }
}
