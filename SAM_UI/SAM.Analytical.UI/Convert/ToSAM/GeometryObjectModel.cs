using SAM.Geometry;
using SAM.Geometry.Spatial;
using SAM.Geometry.UI;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Windows.Media;

namespace SAM.Analytical.UI
{
    public static partial class Convert
    {
        public static GeometryObjectModel ToSAM_GeometryObjectModel(this AnalyticalModel analyticalModel, IViewSettings viewSettings)
        {
            if(analyticalModel == null || viewSettings == null)
            {
                return null;
            }

            return ToSAM_GeometryObjectModel(analyticalModel, viewSettings as dynamic);
        }

        public static GeometryObjectModel ToSAM_GeometryObjectModel(this AnalyticalModel analyticalModel, ThreeDimensionalViewSettings threeDimensionalViewSettings)
        {
            if (analyticalModel == null || threeDimensionalViewSettings == null)
            {
                return null;
            }

            AnalyticalModel analyticalModel_Temp = new AnalyticalModel(analyticalModel);

            AdjacencyCluster adjacencyCluster = analyticalModel_Temp.AdjacencyCluster;

            GeometryObjectModel result = new GeometryObjectModel();

            List<Panel> panels = adjacencyCluster.GetPanels();

            bool showPanels = threeDimensionalViewSettings.IsValid(typeof(Panel));
            bool showApertures = threeDimensionalViewSettings.IsValid(typeof(Aperture));
            bool showSpaces = threeDimensionalViewSettings.IsValid(typeof(Space));

            if (showPanels)
            {
                foreach (Panel panel in panels)
                {
                    GeometryObjectCollection geometryObjectCollection_Panel = new GeometryObjectCollection() { Tag = panel };

                    Face3D face3D = panel.GetFace3D(true);

                    geometryObjectCollection_Panel.Add(new Face3DObject(face3D, Query.SurfaceAppearance(panel, threeDimensionalViewSettings)));

                    if(showApertures)
                    {
                        List<Aperture> apertures = panel.Apertures;
                        if (apertures != null && apertures.Count != 0)
                        {
                            foreach (Aperture aperture in apertures)
                            {
                                GeometryObjectCollection geometryObjectCollection_Aperture = new GeometryObjectCollection() { Tag = aperture };

                                AperturePart aperturePart = AperturePart.Undefined;
                                List<Face3D> face3Ds = null;

                                aperturePart = AperturePart.Frame;
                                face3Ds = aperture.GetFace3Ds(aperturePart);
                                if (face3Ds != null && face3Ds.Count != 0)
                                {
                                    face3Ds.ForEach(x => geometryObjectCollection_Aperture.Add(new Face3DObject(x, Query.SurfaceAppearance(aperture, aperturePart, threeDimensionalViewSettings))));
                                }

                                aperturePart = AperturePart.Pane;
                                face3Ds = aperture.GetFace3Ds(aperturePart);
                                if (face3Ds != null && face3Ds.Count != 0)
                                {
                                    face3Ds.ForEach(x => geometryObjectCollection_Aperture.Add(new Face3DObject(x, Query.SurfaceAppearance(aperture, aperturePart, threeDimensionalViewSettings))));
                                }

                                geometryObjectCollection_Panel.Add(geometryObjectCollection_Aperture);
                            }
                        }
                    }

                    result.Add(geometryObjectCollection_Panel);
                }
            }

            if(showApertures && !showPanels)
            {
                foreach (Panel panel in panels)
                {
                    List<Aperture> apertures = panel.Apertures;
                    if (apertures != null && apertures.Count != 0)
                    {
                        foreach (Aperture aperture in apertures)
                        {
                            GeometryObjectCollection GeometryObjectCollection_Aperture = new GeometryObjectCollection() { Tag = aperture };

                            AperturePart aperturePart = AperturePart.Undefined;
                            List<Face3D> face3Ds = null;

                            aperturePart = AperturePart.Frame;
                            face3Ds = aperture.GetFace3Ds(aperturePart);
                            if (face3Ds != null && face3Ds.Count != 0)
                            {
                                face3Ds.ForEach(x => GeometryObjectCollection_Aperture.Add(new Face3DObject(x, Query.SurfaceAppearance(aperture, aperturePart, threeDimensionalViewSettings))));
                            }

                            aperturePart = AperturePart.Pane;
                            face3Ds = aperture.GetFace3Ds(aperturePart);
                            if (face3Ds != null && face3Ds.Count != 0)
                            {
                                face3Ds.ForEach(x => GeometryObjectCollection_Aperture.Add(new Face3DObject(x, Query.SurfaceAppearance(aperture, aperturePart, threeDimensionalViewSettings))));
                            }

                            result.Add(GeometryObjectCollection_Aperture);
                        }
                    }
                }
            }

            List<Space> spaces = adjacencyCluster.GetSpaces();
            if (showSpaces)
            {
                foreach (Space space in spaces)
                {
                    Shell shell = adjacencyCluster.Shell(space);

                    GeometryObjectCollection geometryObjectCollection_Space = new GeometryObjectCollection() { Tag = space };
                    geometryObjectCollection_Space.Add(new ShellObject(shell, Query.SurfaceAppearance(space, threeDimensionalViewSettings)) { Tag = space });

                    result.Add(geometryObjectCollection_Space);
                }
            }

            result.SetValue(GeometryObjectModelParameter.ViewSettings, threeDimensionalViewSettings);

            return result;
        }

        public static GeometryObjectModel ToSAM_GeometryObjectModel(this AnalyticalModel analyticalModel, TwoDimensionalViewSettings twoDimensionalViewSettings)
        {
            if (analyticalModel == null || twoDimensionalViewSettings == null)
            {
                return null;
            }

            Plane plane = twoDimensionalViewSettings.Plane;

            AnalyticalModel analyticalModel_Temp = new AnalyticalModel(analyticalModel);

            AdjacencyCluster adjacencyCluster = analyticalModel_Temp.AdjacencyCluster;

            Dictionary<Panel, List<ISegmentable3D>> dictionary = Analytical.Query.SectionDictionary<ISegmentable3D>(adjacencyCluster.GetPanels(), plane);
            if (dictionary == null)
            {
                return null;
            }

            GeometryObjectModel result = new GeometryObjectModel();
            foreach (KeyValuePair<Panel, List<ISegmentable3D>> keyValuePair in dictionary)
            {
                if (keyValuePair.Key == null)
                {
                    continue;
                }

                if (keyValuePair.Value == null)
                {
                    continue;
                }

                GeometryObjectCollection geometryObjectCollection_Panel = new GeometryObjectCollection() { Tag = keyValuePair.Key };
                foreach (ISegmentable3D segmentable3D in keyValuePair.Value)
                {
                    List<Segment3D> segment3Ds = segmentable3D?.GetSegments();
                    if (segment3Ds == null)
                    {
                        continue;
                    }

                    segment3Ds.ForEach(x => geometryObjectCollection_Panel.Add(new Segment3DObject(x, Query.CurveAppearance(keyValuePair.Key, twoDimensionalViewSettings))));
                }
                result.Add(geometryObjectCollection_Panel);

            }

            //Dictionary<System.Guid, System.Drawing.Color> dictionary_SpaceColor = Analytical.Modify.AssignSpaceColors(adjacencyCluster);
            List<Space> spaces = adjacencyCluster.GetSpaces();

            if (spaces != null)
            {
                foreach (Space space in spaces)
                {
                    Shell shell = adjacencyCluster.Shell(space);
                    List<Face3D> face3Ds = shell.Section(plane);

                    if (face3Ds == null || face3Ds.Count == 0)
                    {
                        continue;
                    }

                    List<Face3D> face3Ds_Offset = new List<Face3D>();
                    foreach (Face3D face3D in face3Ds)
                    {
                        List<Face3D> face3Ds_Offset_Temp = face3D.Offset(-0.08);
                        if (face3Ds_Offset_Temp == null || face3Ds_Offset_Temp.Count == 0)
                        {
                            continue;
                        }

                        face3Ds_Offset.AddRange(face3Ds_Offset_Temp);
                    }

                    face3Ds = face3Ds_Offset;

                    //System.Drawing.Color color = dictionary_SpaceColor[space.Guid];

                    System.Drawing.Color color = System.Drawing.Color.FromKnownColor(System.Drawing.KnownColor.LightGray);
                    if (space.TryGetValue(SpaceParameter.Color, out System.Drawing.Color color_Temp))
                    {
                        color = color_Temp;
                    }

                    System.Drawing.Color color_Darker = ControlPaint.Dark(color);

                    GeometryObjectCollection geometryObjectCollection_Space = new GeometryObjectCollection() { Tag = space };

                    SurfaceAppearance surfaceAppearance = Query.SurfaceAppearance(space, twoDimensionalViewSettings, new SurfaceAppearance(Color.FromRgb(color.R, color.G, color.B), Color.FromRgb(color_Darker.R, color_Darker.G, color_Darker.B), 0.02));

                    face3Ds.ForEach(x => geometryObjectCollection_Space.Add(new Face3DObject(x, surfaceAppearance)));

                    Point3D point3D = plane.Project(space.Location);

                    Plane plane_Temp = new Plane(plane, point3D.GetMoved(new Vector3D(0, 0, 0.1)) as Point3D);

                    geometryObjectCollection_Space.Add(new Text3DObject(space.Name, plane_Temp, Query.TextAppearance(space, twoDimensionalViewSettings)) { Tag = space });

                    result.Add(geometryObjectCollection_Space);
                }
            }

            result.SetValue(GeometryObjectModelParameter.ViewSettings, twoDimensionalViewSettings);

            return result;
        }
    }
}
