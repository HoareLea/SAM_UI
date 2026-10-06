// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical;
using SAM.Geometry.Spatial;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.UI.WPF.Tests.Helpers
{
    /// <summary>
    /// Builds a small dwelling with REAL 3D geometry, so the floor-plan overlay can be tested on actual
    /// coordinates rather than on a stub.
    /// <para>
    /// The existing Part F test helper in SAM.Tests gives spaces an area and a volume and no shell, which
    /// is all the rate calculation needs. The overlay needs more: it sections each space on the plan's
    /// plane to find where to put a mark, and sections the separating panels to find where a route
    /// crosses. A fixture without walls and floors would section to nothing, every mark would land in
    /// <see cref="PartFFloorPlanOverlay.Unplaced"/>, and the tests would pass while proving nothing.
    /// </para>
    /// <para>
    /// Rooms are laid out along the X axis as closed boxes sharing their partition walls, which is enough
    /// topology to reproduce the example flats: Flat 1 as studio-then-bathroom, Flat 2 as
    /// bedroom-then-kitchen-then-ensuite. Every coordinate is fixed, so a placement assertion is exact.
    /// </para>
    /// </summary>
    public class PartFPlanModel
    {
        /// <summary>Height [m] of every room, so the plan's 1.2 m cut always lands in the walls.</summary>
        public const double Height_M = 3;

        /// <summary>Depth [m] of the block of rooms, along Y.</summary>
        public const double Depth_M = 5;

        private readonly List<(string Name, double X0, double X1)> rooms = [];
        private readonly HashSet<string> lRooms = [];

        private bool closed;

        public AdjacencyCluster AdjacencyCluster { get; private set; } = new();

        /// <summary>Adds a room of the given width, immediately to the right of the previous one.</summary>
        public PartFPlanModel Room(string name, double width_M)
        {
            double x0 = rooms.Count == 0 ? 0 : rooms[^1].X1;
            double x1 = x0 + width_M;

            rooms.Add((name, x0, x1));

            Space space = new(name, new Point3D((x0 + x1) / 2, Depth_M / 2, Height_M / 2));

            space.SetValue(SpaceParameter.Area, width_M * Depth_M);
            space.SetValue(SpaceParameter.Volume, width_M * Depth_M * Height_M);

            AdjacencyCluster.AddObject(space);

            //Floor, ceiling and the two long walls. The partition walls between rooms are added by
            //Partition, so that two rooms genuinely SHARE one panel object - which is how the overlay
            //finds the wall between them.
            AddPanel(space, PanelType.Floor, Horizontal(x0, x1, 0));
            AddPanel(space, PanelType.Roof, Horizontal(x0, x1, Height_M));
            AddPanel(space, PanelType.WallExternal, WallY(x0, x1, 0));
            AddPanel(space, PanelType.WallExternal, WallY(x0, x1, Depth_M));

            //The far ends of the block.
            if (rooms.Count == 1)
            {
                AddPanel(space, PanelType.WallExternal, WallX(x0));
            }

            return this;
        }

        /// <summary>Adds an L-shaped room, whose centroid falls OUTSIDE its own outline.</summary>
        public PartFPlanModel LRoom(string name, double width_M)
        {
            double x0 = rooms.Count == 0 ? 0 : rooms[^1].X1;
            double x1 = x0 + width_M;

            rooms.Add((name, x0, x1));
            lRooms.Add(name);

            Space space = new(name, new Point3D((x0 + x1) / 2, Depth_M / 2, Height_M / 2));

            space.SetValue(SpaceParameter.Area, width_M * Depth_M * 0.75);
            space.SetValue(SpaceParameter.Volume, width_M * Depth_M * Height_M * 0.75);

            AdjacencyCluster.AddObject(space);

            //An L: the block with its top-right quarter removed. The centroid of this shape sits in the
            //missing quarter, so a mark placed at the centroid would land outside the room.
            double x_Mid = (x0 + x1) / 2;
            double y_Mid = Depth_M / 2;

            List<Point3D> point3Ds =
            [
                new Point3D(x0, 0, 0),
                new Point3D(x1, 0, 0),
                new Point3D(x1, y_Mid, 0),
                new Point3D(x_Mid, y_Mid, 0),
                new Point3D(x_Mid, Depth_M, 0),
                new Point3D(x0, Depth_M, 0),
            ];

            AddPanel(space, PanelType.Floor, new Face3D(new Polygon3D(point3Ds)));
            AddPanel(space, PanelType.Roof, new Face3D(new Polygon3D(point3Ds.ConvertAll(x => new Point3D(x.X, x.Y, Height_M)))));

            for (int i = 0; i < point3Ds.Count; i++)
            {
                Point3D point3D_1 = point3Ds[i];
                Point3D point3D_2 = point3Ds[(i + 1) % point3Ds.Count];

                AddPanel(space, PanelType.WallExternal, new Face3D(new Polygon3D(
                [
                    point3D_1,
                    point3D_2,
                    new Point3D(point3D_2.X, point3D_2.Y, Height_M),
                    new Point3D(point3D_1.X, point3D_1.Y, Height_M),
                ])));
            }

            return this;
        }

        /// <summary>
        /// Puts a shared partition between two adjacent rooms, optionally carrying a door aperture.
        /// One panel object related to both spaces, which is what makes them adjacent.
        /// </summary>
        public PartFPlanModel Partition(string name_1, string name_2, string name_Door = null)
        {
            Space space_1 = Space(name_1);
            Space space_2 = Space(name_2);

            double x = rooms.Find(y => y.Name == name_2).X0;

            Panel panel = Analytical.Create.Panel(new Construction(Guid.NewGuid(), "Internal Partition"), PanelType.WallInternal, WallX(x));

            if (!string.IsNullOrWhiteSpace(name_Door))
            {
                //Centred on the wall and reaching the floor, as a door does. The overlay projects its
                //centre onto the plan, which is what puts a transfer mark in the opening.
                panel.AddAperture(Analytical.Create.Aperture(
                    new ApertureConstruction(name_Door, ApertureType.Door),
                    new Face3D(new Polygon3D(
                    [
                        new Point3D(x, (Depth_M / 2) - 0.45, 0),
                        new Point3D(x, (Depth_M / 2) + 0.45, 0),
                        new Point3D(x, (Depth_M / 2) + 0.45, 2.1),
                        new Point3D(x, (Depth_M / 2) - 0.45, 2.1),
                    ]))));
            }

            AdjacencyCluster.AddObject(panel);
            AdjacencyCluster.AddRelation(space_1, panel);
            AdjacencyCluster.AddRelation(space_2, panel);

            return this;
        }

        /// <summary>Puts the named spaces in a dwelling zone.</summary>
        public PartFPlanModel Zone(string name_Zone, string zoneCategory, bool isDwelling, params string[] names_Space)
        {
            //Every fixture zones its rooms once they are all in place, so this is where the block gets its
            //far end. Without it the last room's shell is open, it sections to nothing, and every mark in
            //it lands in Unplaced - which looks exactly like a bug in the overlay rather than in the
            //fixture. Idempotent, because a fixture with two dwellings zones twice.
            Close();

            Zone zone = new(name_Zone);

            zone.SetValue(ZoneParameter.ZoneCategory, zoneCategory);
            zone.SetValue(ZoneParameter.IsDwelling, isDwelling);

            AdjacencyCluster.AddObject(zone);

            foreach (string name_Space in names_Space)
            {
                AdjacencyCluster.AddRelation(zone, Space(name_Space));
            }

            return this;
        }

        /// <summary>
        /// Adds a zone that carries NO Is Dwelling parameter, as a model built before that parameter existed
        /// does. The calculation treats such a category as all dwellings rather than sizing nothing, and the
        /// dwelling-category resolution has to agree with it.
        /// </summary>
        public PartFPlanModel ZoneWithoutIsDwelling(string name_Zone, string zoneCategory, params string[] names_Space)
        {
            Close();

            Zone zone = new(name_Zone);

            zone.SetValue(ZoneParameter.ZoneCategory, zoneCategory);

            AdjacencyCluster.AddObject(zone);

            foreach (string name_Space in names_Space)
            {
                AdjacencyCluster.AddRelation(zone, Space(name_Space));
            }

            return this;
        }

        /// <summary>Records how a cooking space's local extract is provided.</summary>
        public PartFPlanModel LocalExtractMethod(string name, Analytical.Enums.PartFExtractMethod partFExtractMethod)
        {
            Space space = Space(name);

            space.SetValue(SpaceParameter.PartFLocalExtractMethod, partFExtractMethod.ToString());

            AdjacencyCluster.AddObject(space);

            return this;
        }

        /// <summary>Caps the far end of the block, so the last room is a closed volume.</summary>
        public PartFPlanModel Close()
        {
            if (closed || rooms.Count == 0)
            {
                return this;
            }

            closed = true;

            //An L-shaped room already carries all four of its own walls.
            if (!lRooms.Contains(rooms[^1].Name))
            {
                AddPanel(Space(rooms[^1].Name), PanelType.WallExternal, WallX(rooms[^1].X1));
            }

            return this;
        }

        public Space Space(string name)
        {
            return AdjacencyCluster.GetSpaces()?.Find(x => x is not null && x.Name == name);
        }

        public Guid ApertureGuid(string name_Door)
        {
            foreach (Panel panel in AdjacencyCluster.GetPanels() ?? [])
            {
                Aperture aperture = panel?.Apertures?.Find(x => x is not null && x.Name == name_Door);
                if (aperture is not null)
                {
                    return aperture.Guid;
                }
            }

            return Guid.Empty;
        }

        /// <summary>The centre of a door on the plan, so a test can assert a mark actually sits on it.</summary>
        public Point3D DoorCentroid(string name_Door)
        {
            foreach (Panel panel in AdjacencyCluster.GetPanels() ?? [])
            {
                Aperture aperture = panel?.Apertures?.Find(x => x is not null && x.Name == name_Door);
                if (aperture is not null)
                {
                    return aperture.GetFace3D()?.GetCentroid();
                }
            }

            return null;
        }

        private void AddPanel(Space space, PanelType panelType, Face3D face3D)
        {
            Panel panel = Analytical.Create.Panel(new Construction(Guid.NewGuid(), panelType.ToString()), panelType, face3D);

            AdjacencyCluster.AddObject(panel);
            AdjacencyCluster.AddRelation(space, panel);
        }

        private static Face3D Horizontal(double x0, double x1, double z)
        {
            return new Face3D(new Polygon3D(
            [
                new Point3D(x0, 0, z),
                new Point3D(x1, 0, z),
                new Point3D(x1, Depth_M, z),
                new Point3D(x0, Depth_M, z),
            ]));
        }

        private static Face3D WallY(double x0, double x1, double y)
        {
            return new Face3D(new Polygon3D(
            [
                new Point3D(x0, y, 0),
                new Point3D(x1, y, 0),
                new Point3D(x1, y, Height_M),
                new Point3D(x0, y, Height_M),
            ]));
        }

        private static Face3D WallX(double x)
        {
            return new Face3D(new Polygon3D(
            [
                new Point3D(x, 0, 0),
                new Point3D(x, Depth_M, 0),
                new Point3D(x, Depth_M, Height_M),
                new Point3D(x, 0, Height_M),
            ]));
        }
    }
}
