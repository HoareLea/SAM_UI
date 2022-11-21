using SAM.Geometry;
using SAM.Geometry.Spatial;
using SAM.Geometry.UI;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Windows.Media;

namespace SAM.Analytical.UI
{
    public static partial class Create
    {
        public static GeometryObjectModel ToSAM_GeometryObjectModel(this AnalyticalModel analyticalModel, Mode mode, Plane plane = null)
        {
            if(analyticalModel == null)
            {
                return null;
            }

            switch(mode)
            {
                case Mode.ThreeDimensional:
                    return ToSAM_GeometryObjectModel_3D(analyticalModel, plane);

                case Mode.TwoDimensional:
                    return ToSAM_GeometryObjectModel_2D(analyticalModel, plane);
            }

            return null;
        }

        private static GeometryObjectModel ToSAM_GeometryObjectModel_3D(this AnalyticalModel analyticalModel, Plane plane = null)
        {
            if (analyticalModel == null)
            {
                return null;
            }

            AnalyticalModel analyticalModel_Temp = new AnalyticalModel(analyticalModel);
            //analyticalModel_Temp.Normalize();

            List<Panel> panels = analyticalModel_Temp.GetPanels();

            GeometryObjectModel result = new GeometryObjectModel();
            foreach (Panel panel in panels)
            {
                VisualPanel visualPanel = new VisualPanel(panel);
                visualPanel.Add(new Face3DObject(panel.GetFace3D(true), Query.SurfaceAppearance(panel)) { Tag = panel});

                List<Aperture> apertures = panel.Apertures;
                if (apertures != null && apertures.Count != 0)
                {
                    foreach (Aperture aperture in apertures)
                    {
                        VisualAperture visualAperture = new VisualAperture(aperture);

                        AperturePart aperturePart = AperturePart.Undefined;
                        List<Face3D> face3Ds = null;

                        aperturePart = AperturePart.Frame;
                        face3Ds = aperture.GetFace3Ds(aperturePart);
                        if (face3Ds != null && face3Ds.Count != 0)
                        {
                            face3Ds.ForEach(x => visualAperture.Add(new Face3DObject(x, Query.SurfaceAppearance(aperture, aperturePart)) { Tag = aperture }));
                        }

                        aperturePart = AperturePart.Pane;
                        face3Ds = aperture.GetFace3Ds(aperturePart);
                        if (face3Ds != null && face3Ds.Count != 0)
                        {
                            face3Ds.ForEach(x => visualAperture.Add(new Face3DObject(x, Query.SurfaceAppearance(aperture, aperturePart)) { Tag = aperture}));
                        }

                        visualPanel.Add(visualAperture);
                    }
                }

                result.Add(visualPanel);
            }


            return result;
        }

        private static GeometryObjectModel ToSAM_GeometryObjectModel_2D(this AnalyticalModel analyticalModel, Plane plane)
        {
            if (analyticalModel == null || plane == null)
            {
                return null;
            }

            AnalyticalModel analyticalModel_Temp = new AnalyticalModel(analyticalModel);

            AdjacencyCluster adjacencyCluster = analyticalModel_Temp.AdjacencyCluster;

            Dictionary<Panel, List<ISegmentable3D>> dictionary = Analytical.Query.SectionDictionary<ISegmentable3D>(adjacencyCluster.GetPanels(), plane);
            if(dictionary == null)
            {
                return null;
            }

            GeometryObjectModel result = new GeometryObjectModel();
            foreach (KeyValuePair<Panel, List<ISegmentable3D>> keyValuePair in dictionary)
            {
                if(keyValuePair.Key == null)
                {
                    continue;
                }

                if(keyValuePair.Value == null)
                {
                    continue;
                }

                VisualPanel visualPanel = new VisualPanel(keyValuePair.Key);
                foreach(ISegmentable3D segmentable3D in keyValuePair.Value)
                {
                    List<Segment3D> segment3Ds = segmentable3D?.GetSegments();
                    if(segment3Ds == null)
                    {
                        continue;
                    }

                    segment3Ds.ForEach(x => visualPanel.Add(new Segment3DObject(x, new CurveAppearance(Color.FromRgb(105, 105, 105), 0.04)) { Tag = keyValuePair.Key }));
                }
                result.Add(visualPanel);

            }

            //Dictionary<System.Guid, System.Drawing.Color> dictionary_SpaceColor = Analytical.Modify.AssignSpaceColors(adjacencyCluster);
            List<Space> spaces = adjacencyCluster.GetSpaces();

            if(spaces != null)
            {
                foreach(Space space in spaces)
                {
                    Shell shell = adjacencyCluster.Shell(space);
                    List<Face3D> face3Ds = shell.Section(plane);

                    if(face3Ds == null || face3Ds.Count == 0)
                    {
                        continue;
                    }

                    List<Face3D> face3Ds_Offset = new List<Face3D>();
                    foreach(Face3D face3D in face3Ds)
                    {
                        List<Face3D> face3Ds_Offset_Temp = face3D.Offset(-0.05);
                        if(face3Ds_Offset_Temp == null || face3Ds_Offset_Temp.Count == 0)
                        {
                            continue;
                        }

                        face3Ds_Offset.AddRange(face3Ds_Offset_Temp);
                    }

                    face3Ds = face3Ds_Offset;

                    //System.Drawing.Color color = dictionary_SpaceColor[space.Guid];

                    System.Drawing.Color color = System.Drawing.Color.FromKnownColor(System.Drawing.KnownColor.LightGray);
                    if(space.TryGetValue(SpaceParameter.Color, out System.Drawing.Color color_Temp))
                    {
                        color = color_Temp;
                    }

                    System.Drawing.Color color_Darker = ControlPaint.Dark(color);

                    VisualSpace visualSpace = new VisualSpace(space);
                    face3Ds.ForEach(x => visualSpace.Add(new Face3DObject(x, new SurfaceAppearance(Color.FromRgb(color.R, color.G, color.B), Color.FromRgb(color_Darker.R, color_Darker.G, color_Darker.B), 0.02)) { Tag = space}));

                    Point3D point3D = plane.Project(space.Location);

                    Plane plane_Temp = new Plane(plane, point3D.GetMoved(new Vector3D(0,0, 0.1)) as Point3D);

                    visualSpace.Add(new Text3DObject(space.Name, plane_Temp, new TextAppearance(Color.FromRgb(0, 0, 0), 1, "Segoe UI")));
                    
                    result.Add(visualSpace);
                }
            }

            result.SetValue(GeometryObjectModelParameter.SectionPlane , plane);

            return result;

        }

        private static GeometryObjectModel ToSAM_GeometryObjectModel(this AnalyticalModel analyticalModel, ViewSettings viewSettings)
        {
            if(analyticalModel == null || viewSettings == null)
            {
                return null;
            }

            return ToSAM_GeometryObjectModel(analyticalModel, viewSettings as dynamic);
        }

        private static GeometryObjectModel ToSAM_GeometryObjectModel(this AnalyticalModel analyticalModel, ThreeDimensionalViewSettings threeDimensionalViewSettings)
        {
            if (analyticalModel == null)
            {
                return null;
            }

            AnalyticalModel analyticalModel_Temp = new AnalyticalModel(analyticalModel);

            List<Panel> panels = analyticalModel_Temp.GetPanels();

            GeometryObjectModel result = new GeometryObjectModel();
            foreach (Panel panel in panels)
            {
                VisualPanel visualPanel = new VisualPanel(panel);
                visualPanel.Add(new Face3DObject(panel.GetFace3D(true), Query.SurfaceAppearance(panel)) { Tag = panel });

                List<Aperture> apertures = panel.Apertures;
                if (apertures != null && apertures.Count != 0)
                {
                    foreach (Aperture aperture in apertures)
                    {
                        VisualAperture visualAperture = new VisualAperture(aperture);

                        AperturePart aperturePart = AperturePart.Undefined;
                        List<Face3D> face3Ds = null;

                        aperturePart = AperturePart.Frame;
                        face3Ds = aperture.GetFace3Ds(aperturePart);
                        if (face3Ds != null && face3Ds.Count != 0)
                        {
                            face3Ds.ForEach(x => visualAperture.Add(new Face3DObject(x, Query.SurfaceAppearance(aperture, aperturePart)) { Tag = aperture }));
                        }

                        aperturePart = AperturePart.Pane;
                        face3Ds = aperture.GetFace3Ds(aperturePart);
                        if (face3Ds != null && face3Ds.Count != 0)
                        {
                            face3Ds.ForEach(x => visualAperture.Add(new Face3DObject(x, Query.SurfaceAppearance(aperture, aperturePart)) { Tag = aperture }));
                        }

                        visualPanel.Add(visualAperture);
                    }
                }

                result.Add(visualPanel);
            }


            return result;
        }

        private static GeometryObjectModel ToSAM_GeometryObjectModel(this AnalyticalModel analyticalModel, TwoDimensionalViewSettings twoDimensionalViewSettings)
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

                VisualPanel visualPanel = new VisualPanel(keyValuePair.Key);
                foreach (ISegmentable3D segmentable3D in keyValuePair.Value)
                {
                    List<Segment3D> segment3Ds = segmentable3D?.GetSegments();
                    if (segment3Ds == null)
                    {
                        continue;
                    }

                    segment3Ds.ForEach(x => visualPanel.Add(new Segment3DObject(x, new CurveAppearance(Color.FromRgb(105, 105, 105), 0.04)) { Tag = keyValuePair.Key }));
                }
                result.Add(visualPanel);

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
                        List<Face3D> face3Ds_Offset_Temp = face3D.Offset(-0.05);
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

                    VisualSpace visualSpace = new VisualSpace(space);
                    face3Ds.ForEach(x => visualSpace.Add(new Face3DObject(x, new SurfaceAppearance(Color.FromRgb(color.R, color.G, color.B), Color.FromRgb(color_Darker.R, color_Darker.G, color_Darker.B), 0.02)) { Tag = space }));

                    Point3D point3D = plane.Project(space.Location);

                    Plane plane_Temp = new Plane(plane, point3D.GetMoved(new Vector3D(0, 0, 0.1)) as Point3D);

                    visualSpace.Add(new Text3DObject(space.Name, plane_Temp, new TextAppearance(Color.FromRgb(0, 0, 0), 1, "Segoe UI")));

                    result.Add(visualSpace);
                }
            }

            result.SetValue(GeometryObjectModelParameter.SectionPlane, plane);

            return result;
        }
    }
}
