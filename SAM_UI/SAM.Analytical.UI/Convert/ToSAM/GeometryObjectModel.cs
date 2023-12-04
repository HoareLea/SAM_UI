using SAM.Core;
using SAM.Core.UI;
using SAM.Geometry;
using SAM.Geometry.Planar;
using SAM.Geometry.Spatial;
using SAM.Geometry.UI;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Windows.Media;

namespace SAM.Analytical.UI
{
    public static partial class Convert
    {
        public static GeometryObjectModel ToSAM_GeometryObjectModel(this AnalyticalModel analyticalModel, IViewSettings viewSettings)
        {
            if (analyticalModel == null || viewSettings == null)
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

            Legend legend = threeDimensionalViewSettings.Legend;

            List<Plane> planes = threeDimensionalViewSettings.Planes;

            //Range<double> range = analyticalModel.GetElevationRange();
            //if(range != null)
            //{
            //    Plane plane = Plane.WorldXY.GetMoved(new Vector3D(0, 0, (range.Min + range.Max) / 2)) as Plane;
            //    planes = new List<Plane>() { plane };
            //}

            GeometryObjectModel result = new GeometryObjectModel();

            bool legendUpdated = false;

            bool showSpaces = threeDimensionalViewSettings.IsValid(typeof(Space));
            Dictionary<Guid, GeometryObjectCollection> dictionary_Spaces = new Dictionary<Guid, GeometryObjectCollection>();
            if (showSpaces)
            {
                List<Space> spaces = adjacencyCluster.GetSpaces();
                if (spaces != null && spaces.Count != 0)
                {
                    List<LegendItemData> legendItemDatas = new List<LegendItemData>();
                    foreach (Space space in spaces)
                    {
                        if (Query.TryGetValue(space, adjacencyCluster, threeDimensionalViewSettings, out object value, out string text))
                        {
                            legendItemDatas.Add(new LegendItemData(space, value, text));
                        }
                    }

                    bool editable = Query.Editable<SpaceAppearanceSettings>(threeDimensionalViewSettings);

                    Dictionary<Guid, LegendItem> dictionary_LegendItem = Query.LegendItemDictionary(legendItemDatas, editable, Query.UndefinedLegendItem());
                    if (legend != null)
                    {
                        if (dictionary_LegendItem != null && dictionary_LegendItem.Count != 0)
                        {
                            legend.Refresh(dictionary_LegendItem.Values, true, true);
                        }
                        else
                        {
                            legend = null;
                        }
                    }

                    foreach (Space space in spaces)
                    {
                        Color? color = null;

                        if (dictionary_LegendItem.TryGetValue(space.Guid, out LegendItem legendItem) && legendItem != null)
                        {
                            if (legend != null)
                            {
                                legendItem = legend.Find(legendItem?.Text);
                            }

                            color = Color.FromRgb(legendItem.Color.R, legendItem.Color.G, legendItem.Color.B);
                        }

                        if (color == null || !color.HasValue)
                        {
                            color = System.Drawing.Color.FromKnownColor(System.Drawing.KnownColor.LightGray).ToMedia();
                        }

                        SurfaceAppearance surfaceAppearance = Query.SurfaceAppearance(space, threeDimensionalViewSettings, new SurfaceAppearance(color.Value, ControlPaint.Dark(color.Value.ToDrawing()).ToMedia(), 0));
                        if (surfaceAppearance == null || surfaceAppearance.Opacity == 0 || !surfaceAppearance.Visible)
                        {
                            continue;
                        }

                        Shell shell = adjacencyCluster.Shell(space);
                        if (shell == null)
                        {
                            continue;
                        }

                        List<Shell> shells = null;
                        if (planes != null && planes.Count != 0)
                        {
                            shells = shell.Cut(planes);
                            shells = shells.FindAll(x => planes.TrueForAll(y => Geometry.Spatial.Query.Above(y, x.InternalPoint3D(), 0)));
                        }
                        else
                        {
                            shells = new List<Shell>() { shell };
                        }

                        if (shells == null || shells.Count == 0)
                        {
                            continue;
                        }

                        GeometryObjectCollection geometryObjectCollection_Space = new GeometryObjectCollection() { Tag = space };
                        foreach (Shell shell_Temp in shells)
                        {
                            geometryObjectCollection_Space.Add(new ShellObject(shell_Temp, surfaceAppearance) { Tag = space });
                        }

                        dictionary_Spaces[space.Guid] = geometryObjectCollection_Space;
                    }

                    if (!legendUpdated)
                    {
                        if (legend != null)
                        {
                            threeDimensionalViewSettings.Legend = legend;
                            legendUpdated = true;
                        }
                        else
                        {
                            if (dictionary_LegendItem != null && dictionary_LegendItem.Count != 0)
                            {
                                threeDimensionalViewSettings.Legend = new Legend(Query.LegendName<Space>(threeDimensionalViewSettings), dictionary_LegendItem.Values);
                                legendUpdated = true;
                            }
                            else
                            {
                                threeDimensionalViewSettings.Legend = null;
                            }
                        }
                    }
                }
            }

            bool showPanels = threeDimensionalViewSettings.IsValid(typeof(Panel));
            Dictionary<Guid, GeometryObjectCollection> dictionary_Panels = new Dictionary<Guid, GeometryObjectCollection>();
            if (showPanels)
            {
                List<Panel> panels = adjacencyCluster.GetPanels();
                if (panels != null && panels.Count != 0)
                {
                    List<LegendItemData> legendItemDatas = new List<LegendItemData>();
                    foreach (Panel panel in panels)
                    {
                        if (Query.TryGetValue(panel, adjacencyCluster, threeDimensionalViewSettings, out object value, out string text))
                        {
                            legendItemDatas.Add(new LegendItemData(panel, value, text));
                        }
                    }

                    bool editable = Query.Editable<PanelAppearanceSettings>(threeDimensionalViewSettings);

                    Dictionary<Guid, LegendItem> dictionary_LegendItem = Query.LegendItemDictionary(legendItemDatas, editable, Query.UndefinedLegendItem());
                    if (legend != null)
                    {
                        if (dictionary_LegendItem != null && dictionary_LegendItem.Count != 0)
                        {
                            legend.Refresh(dictionary_LegendItem.Values, true, true);
                        }
                        else
                        {
                            legend = null;
                        }
                    }

                    foreach (Panel panel in panels)
                    {
                        GeometryObjectCollection geometryObjectCollection_Panel = new GeometryObjectCollection() { Tag = panel };

                        Color? color = null;

                        if (dictionary_LegendItem.TryGetValue(panel.Guid, out LegendItem legendItem) && legendItem != null)
                        {
                            if (legend != null)
                            {
                                legendItem = legend.Find(legendItem?.Text);
                            }

                            color = Color.FromRgb(legendItem.Color.R, legendItem.Color.G, legendItem.Color.B);
                        }

                        SurfaceAppearance surfaceAppearance = Query.SurfaceAppearance(panel, threeDimensionalViewSettings, color);
                        if (surfaceAppearance == null || surfaceAppearance.Opacity == 0 || !surfaceAppearance.Visible)
                        {
                            continue;
                        }

                        List<Face3D> face3Ds = panel.GetFace3Ds(true);
                        if (face3Ds == null || face3Ds.Count == 0)
                        {
                            continue;
                        }

                        for (int i = 0; i < face3Ds.Count; i++)
                        {
                            List<Face3D> face3Ds_FixEdges = face3Ds[i].FixEdges();
                            if (face3Ds_FixEdges != null && face3Ds_FixEdges.Count != 0)
                            {
                                if (face3Ds_FixEdges.Count != 1)
                                {
                                    face3Ds_FixEdges.Sort((x, y) => y.GetArea().CompareTo(x.GetArea()));
                                }

                                face3Ds[i] = face3Ds_FixEdges.Find(x => x.IsValid());
                            }
                        }

                        if (planes != null && planes.Count != 0)
                        {
                            List<Face3D> face3Ds_Temp = new List<Face3D>();
                            foreach (Face3D face3D in face3Ds)
                            {
                                List<Face3D> face3Ds_Cut = face3D.Cut(planes);
                                face3Ds_Cut = face3Ds_Cut?.FindAll(x => planes.TrueForAll(y => Geometry.Spatial.Query.Above(y, x.GetCentroid(), 0)));
                                if (face3Ds_Cut != null && face3Ds_Cut.Count != 0)
                                {
                                    face3Ds_Temp.AddRange(face3Ds_Cut);
                                }
                            }

                            face3Ds = face3Ds_Temp;
                        }

                        if (face3Ds == null || face3Ds.Count == 0)
                        {
                            continue;
                        }

                        foreach (Face3D face3D_Temp in face3Ds)
                        {
                            geometryObjectCollection_Panel.Add(new Face3DObject(face3D_Temp, surfaceAppearance));
                        }

                        dictionary_Panels[panel.Guid] = geometryObjectCollection_Panel;
                    }

                    if (!legendUpdated)
                    {
                        if (legend != null)
                        {
                            threeDimensionalViewSettings.Legend = legend;
                            legendUpdated = true;
                        }
                        else
                        {
                            if (dictionary_LegendItem != null && dictionary_LegendItem.Count != 0)
                            {
                                threeDimensionalViewSettings.Legend = new Legend(Query.LegendName<Panel>(threeDimensionalViewSettings), dictionary_LegendItem.Values);
                                legendUpdated = true;
                            }
                            else
                            {
                                threeDimensionalViewSettings.Legend = null;
                            }
                        }

                    }
                }
            }

            bool showApertures = threeDimensionalViewSettings.IsValid(typeof(Aperture));
            Dictionary<Guid, GeometryObjectCollection> dictionary_Apertures = new Dictionary<Guid, GeometryObjectCollection>();
            if (showApertures)
            {
                List<Aperture> apertures = adjacencyCluster.GetApertures();
                if (apertures != null && apertures.Count != 0)
                {
                    List<LegendItemData> legendItemDatas = new List<LegendItemData>();
                    foreach (Aperture aperture in apertures)
                    {
                        if (Query.TryGetValue(aperture, adjacencyCluster, threeDimensionalViewSettings, out object value, out string text))
                        {
                            legendItemDatas.Add(new LegendItemData(aperture, value, text));
                        }
                    }

                    bool editable = Query.Editable<PanelAppearanceSettings>(threeDimensionalViewSettings);

                    Dictionary<Guid, LegendItem> dictionary_LegendItem = Query.LegendItemDictionary(legendItemDatas, editable, Query.UndefinedLegendItem());
                    if (legend != null)
                    {
                        if (dictionary_LegendItem != null && dictionary_LegendItem.Count != 0)
                        {
                            legend.Refresh(dictionary_LegendItem.Values, true, true);
                        }
                        else
                        {
                            legend = null;
                        }
                    }

                    foreach (Aperture aperture in apertures)
                    {
                        GeometryObjectCollection geometryObjectCollection_Aperture = new GeometryObjectCollection() { Tag = aperture };

                        Color? color = null;

                        if (dictionary_LegendItem.TryGetValue(aperture.Guid, out LegendItem legendItem) && legendItem != null)
                        {
                            if (legend != null)
                            {
                                legendItem = legend.Find(legendItem?.Text);
                            }

                            color = Color.FromRgb(legendItem.Color.R, legendItem.Color.G, legendItem.Color.B);
                        }

                        SurfaceAppearance surfaceAppearance_Frame = Query.SurfaceAppearance(aperture, AperturePart.Frame, threeDimensionalViewSettings, color);
                        if (surfaceAppearance_Frame == null || surfaceAppearance_Frame.Opacity == 0 || !surfaceAppearance_Frame.Visible)
                        {
                            surfaceAppearance_Frame = null;
                        }

                        SurfaceAppearance surfaceAppearance_Pane = Query.SurfaceAppearance(aperture, AperturePart.Pane, threeDimensionalViewSettings, color);
                        if (surfaceAppearance_Pane == null || surfaceAppearance_Pane.Opacity == 0 || !surfaceAppearance_Pane.Visible)
                        {
                            surfaceAppearance_Pane = null;
                        }

                        if (surfaceAppearance_Frame == null && surfaceAppearance_Pane == null)
                        {
                            continue;
                        }


                        AperturePart aperturePart = AperturePart.Undefined;
                        List<Face3D> face3Ds = null;

                        aperturePart = AperturePart.Frame;
                        if (surfaceAppearance_Frame != null)
                        {
                            face3Ds = aperture.GetFace3Ds(aperturePart);
                            if (face3Ds != null && face3Ds.Count != 0)
                            {
                                if (planes != null && planes.Count != 0)
                                {
                                    face3Ds = face3Ds.Cut(planes);
                                    face3Ds = face3Ds.FindAll(x => planes.TrueForAll(y => Geometry.Spatial.Query.Above(y, x.GetCentroid(), 0)));

                                }

                                face3Ds.ForEach(x => geometryObjectCollection_Aperture.Add(new Face3DObject(x, surfaceAppearance_Frame)));
                            }
                        }

                        aperturePart = AperturePart.Pane;
                        if (surfaceAppearance_Pane != null)
                        {
                            face3Ds = aperture.GetFace3Ds(aperturePart);
                            if (face3Ds != null && face3Ds.Count != 0)
                            {
                                if (planes != null && planes.Count != 0)
                                {
                                    face3Ds = face3Ds.Cut(planes);
                                    face3Ds = face3Ds.FindAll(x => planes.TrueForAll(y => Geometry.Spatial.Query.Above(y, x.GetCentroid(), 0)));
                                }

                                face3Ds.ForEach(x => geometryObjectCollection_Aperture.Add(new Face3DObject(x, surfaceAppearance_Pane)));
                            }
                        }

                        dictionary_Apertures[aperture.Guid] = geometryObjectCollection_Aperture;
                    }

                    if (!legendUpdated)
                    {
                        if (legend != null)
                        {
                            threeDimensionalViewSettings.Legend = legend;
                            legendUpdated = true;
                        }
                        else
                        {
                            if (dictionary_LegendItem != null && dictionary_LegendItem.Count != 0)
                            {
                                threeDimensionalViewSettings.Legend = new Legend(Query.LegendName<Aperture>(threeDimensionalViewSettings), dictionary_LegendItem.Values);
                                legendUpdated = true;
                            }
                            else
                            {
                                threeDimensionalViewSettings.Legend = null;
                            }
                        }
                    }
                }
            }

            if (dictionary_Panels != null && dictionary_Panels.Count != 0)
            {
                if (dictionary_Apertures != null && dictionary_Apertures.Count != 0)
                {
                    HashSet<Guid> guids = new HashSet<Guid>();
                    foreach (KeyValuePair<Guid, GeometryObjectCollection> keyValuePair in dictionary_Apertures)
                    {
                        Aperture aperture = adjacencyCluster.GetAperture(keyValuePair.Key);
                        if (aperture == null)
                        {
                            continue;
                        }

                        Panel panel = adjacencyCluster.GetPanel(aperture);
                        if (panel == null)
                        {
                            continue;
                        }

                        if (!dictionary_Panels.TryGetValue(panel.Guid, out GeometryObjectCollection geometryObjectCollection) || geometryObjectCollection == null)
                        {
                            continue;
                        }

                        geometryObjectCollection.Add(keyValuePair.Value);
                        guids.Add(keyValuePair.Key);
                    }

                    foreach (Guid guid in guids)
                    {
                        dictionary_Apertures.Remove(guid);
                    }
                }
            }

            foreach (GeometryObjectCollection geometryObjectCollection in dictionary_Apertures.Values)
            {
                result.Add(geometryObjectCollection);
            }

            foreach (GeometryObjectCollection geometryObjectCollection in dictionary_Panels.Values)
            {
                result.Add(geometryObjectCollection);
            }

            foreach (GeometryObjectCollection geometryObjectCollection in dictionary_Spaces.Values)
            {
                result.Add(geometryObjectCollection);
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

            Legend legend = twoDimensionalViewSettings.Legend;

            AdjacencyCluster adjacencyCluster = analyticalModel_Temp.AdjacencyCluster;

            bool showPanels = twoDimensionalViewSettings.IsValid(typeof(Panel));
            bool showApertures = twoDimensionalViewSettings.IsValid(typeof(Aperture));
            bool showSpaces = twoDimensionalViewSettings.IsValid(typeof(Space));

            GeometryObjectModel result = new GeometryObjectModel();

            bool legendUpdated = false;

            if (showPanels || showApertures)
            {
                Dictionary<Panel, List<ISegmentable3D>> dictionary = Analytical.Query.SectionDictionary<ISegmentable3D>(adjacencyCluster.GetPanels(), plane);
                if (dictionary == null)
                {
                    return null;
                }

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

                    if (showPanels)
                    {
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
                }
            }

            if (showSpaces)
            {
                List<Space> spaces = adjacencyCluster.GetSpaces();

                if (spaces != null)
                {
                    Dictionary<Space, List<Face2D>> dictionary_Space = new Dictionary<Space, List<Face2D>>();
                    foreach (Space space in spaces)
                    {
                        Shell shell = adjacencyCluster?.Shell(space);
                        List<Face3D> face3Ds = shell?.Section(plane);
                        if (face3Ds == null || face3Ds.Count == 0)
                        {
                            continue;
                        }

                        dictionary_Space[space] = face3Ds.ConvertAll(x => plane.Convert(x));
                    }

                    List<LegendItemData> legendItemDatas = new List<LegendItemData>();
                    foreach (Space space in dictionary_Space.Keys)
                    {
                        if (Query.TryGetValue(space, adjacencyCluster, twoDimensionalViewSettings, out object value, out string text))
                        {
                            legendItemDatas.Add(new LegendItemData(space, value, text));
                        }
                    }

                    bool editable = Query.Editable<SpaceAppearanceSettings>(twoDimensionalViewSettings);

                    //Dictionary<System.Guid, LegendItem> dictionary_LegendItem = Query.LegendItemDictionary(dictionary_Space.Keys, adjacencyCluster, twoDimensionalViewSettings, Query.UndefinedLegendItem());
                    Dictionary<Guid, LegendItem> dictionary_LegendItem = Query.LegendItemDictionary(legendItemDatas, editable, Query.UndefinedLegendItem());
                    if (legend != null)
                    {
                        if (dictionary_LegendItem != null && dictionary_LegendItem.Count != 0)
                        {
                            legend.Refresh(dictionary_LegendItem.Values, true, true);
                        }
                        else
                        {
                            legend = null;
                        }
                    }


                    double spaceEdgeOffset = twoDimensionalViewSettings.SpaceEdgeOffset;


                    Dictionary<Space, List<Face2D>> dictionary_Space_Temp = new Dictionary<Space, List<Face2D>>();
                    foreach (KeyValuePair<Space, List<Face2D>> keyValuePair in dictionary_Space)
                    {
                        Space space = keyValuePair.Key;
                        List<Face2D> face2Ds = keyValuePair.Value;

                        if (face2Ds == null || face2Ds.Count == 0)
                        {
                            continue;
                        }

                        List<Face2D> face2Ds_Offset = new List<Face2D>();
                        foreach (Face2D face2D in face2Ds)
                        {
                            if (face2D == null || !face2D.IsValid())
                            {
                                continue;
                            }

                            List<Face2D> face2Ds_Offset_Temp = face2D.Offset(spaceEdgeOffset);
                            if (face2Ds_Offset_Temp == null || face2Ds_Offset_Temp.Count == 0)
                            {
                                face2Ds_Offset_Temp = new List<Face2D>() { face2D };
                            }
                            else if (face2Ds_Offset_Temp == null || face2Ds_Offset_Temp.Count == 0)
                            {
                                continue;
                            }

                            face2Ds_Offset.AddRange(face2Ds_Offset_Temp);
                        }

                        face2Ds = face2Ds_Offset;
                        if (face2Ds == null || face2Ds.Count == 0)
                        {
                            continue;
                        }

                        dictionary_Space_Temp[space] = face2Ds;
                    }

                    dictionary_Space = dictionary_Space_Temp;


                    //TODO: Currently we use InternalPoint form Shell section. However we should be able to define x,y location of space in UI
                    //and store this data as space.Location this will allow more control over where tags are placed
                    //Point3D point3D = plane.Project(space.Location);
                    List<Tuple<Space, List<Face2D>, string, Point2D>> tuples = new List<Tuple<Space, List<Face2D>, string, Point2D>>();

                    List<Solver2DData> solver2DDatas = new List<Solver2DData>();
                    List<Point2D> points = new List<Point2D>();

                    foreach (KeyValuePair<Space, List<Face2D>> keyValuePair in dictionary_Space)
                    {
                        string name = keyValuePair.Key.Name;
                        Space space = keyValuePair.Key;

                        TextAppearance textAppearance = Query.TextAppearance(space, twoDimensionalViewSettings);
                        double height = textAppearance.Height;
                        double width = Core.Windows.Query.Width(name, new System.Drawing.Font(textAppearance.FontFamilyName, System.Convert.ToSingle(height)), height);

                        List<Face2D> face2Ds = keyValuePair.Value;

                        Point2D point2D = null;
                        if (point2D == null)
                        {
                            if (face2Ds.Count > 1)
                            {
                                face2Ds.Sort((x, y) => y.GetArea().CompareTo(x.GetArea()));
                            }

                            point2D = face2Ds[0].InternalPoint2D();
                        }

                        double farthestPointDistance = 0;
                        foreach (Point2D pointsTemp in face2Ds[0].Edge2Ds[0].Polygon2D().Points)
                        {
                            points.Add(pointsTemp);
                            double distance = pointsTemp.Distance(point2D);

                            farthestPointDistance = Math.Max(distance, farthestPointDistance);
                        }
                        Rectangle2D rectangle2D = new Rectangle2D(new Point2D(point2D.X + width / 2, point2D.Y + height / 2), width, height);

                        Solver2DData solver2DData = new Solver2DData(rectangle2D, point2D);
                        solver2DData.Tag = new Tuple<Space, List<Face2D>, string, Point2D>(keyValuePair.Key, keyValuePair.Value, name, point2D);

                        Solver2DSettings solver2DSettings = new Solver2DSettings();
                        solver2DSettings.IterationCount = 100;
                        solver2DSettings.ShiftDistance = (farthestPointDistance - solver2DSettings.StartingDistance) / solver2DSettings.IterationCount;
                        solver2DSettings.LimitArea = face2Ds[0];
                        solver2DData.Solver2DSettings = solver2DSettings;

                        solver2DDatas.Add(solver2DData);
                    }

                    // Maybe we should create bigger area
                    Rectangle2D area = Geometry.Planar.Create.Rectangle2D(points);

                    // Set the area as infinite so that the labels can extend beyond the faces
                    area = new Rectangle2D(new Point2D(double.MinValue / 2, double.MinValue / 2), double.MaxValue, double.MaxValue);
                    Solver2D solver2D = new Solver2D(area, new List<IClosed2D>());
                    solver2D.AddRange(solver2DDatas);

                    List<Solver2DResult> solver2DResults = solver2D.Solve();

                    // Check if solver2DResults is null
                    // Add shifted labels from solver2DResults to tuples
                    if (solver2DResults != null)
                    {
                        foreach (Solver2DResult solver2DResult in solver2DResults)
                        {
                            Solver2DData solver2DData = solver2DResult.Solver2DData;
                            Tuple<Space, List<Face2D>, string, Point2D> tuple = solver2DData.Tag as Tuple<Space, List<Face2D>, string, Point2D>;

                            if (tuple == null)
                            {
                                continue;
                            }

                            Rectangle2D rectangle2D = solver2DResult.Closed2D<Rectangle2D>();
                            if (rectangle2D != null)
                            {
                                tuple = new Tuple<Space, List<Face2D>, string, Point2D>(tuple.Item1, tuple.Item2, tuple.Item3, rectangle2D.GetCentroid());
                            }
                            else
                            {
                                tuple = new Tuple<Space, List<Face2D>, string, Point2D>(tuple.Item1, tuple.Item2, "", tuple.Item4);
                            }
                            tuples.Add(tuple);
                        }
                    }


                    foreach (Tuple<Space, List<Face2D>, string, Point2D> tuple in tuples)
                    {
                        Space space = tuple.Item1;
                        List<Face2D> face2Ds = tuple.Item2;

                        Point3D point3D = plane.Convert(tuple.Item4);

                        Color? color = null;

                        string text = tuple.Item3;

                        if (dictionary_LegendItem.TryGetValue(space.Guid, out LegendItem legendItem))
                        {
                            if (legend != null)
                            {
                                legendItem = legend.Find(legendItem?.Text);
                            }

                            color = Core.UI.Convert.ToMedia(legendItem.Color);
                        }

                        if (color == null || !color.HasValue)
                        {
                            color = System.Drawing.Color.FromKnownColor(System.Drawing.KnownColor.LightGray).ToMedia();
                        }

                        GeometryObjectCollection geometryObjectCollection_Space = new GeometryObjectCollection() { Tag = space };

                        SurfaceAppearance surfaceAppearance = Query.SurfaceAppearance(space, twoDimensionalViewSettings, color);

                        face2Ds.ForEach(x => geometryObjectCollection_Space.Add(new Face3DObject(plane.Convert(x), surfaceAppearance)));

                        Plane plane_Temp = new Plane(plane, point3D.GetMoved(new Vector3D(0, 0, 0.1)) as Point3D);

                        if (twoDimensionalViewSettings.TextAppearance == null || twoDimensionalViewSettings.TextAppearance.Opacity != 0)
                        {
                            geometryObjectCollection_Space.Add(new Text3DObject(text, plane_Temp, Query.TextAppearance(space, twoDimensionalViewSettings)) { Tag = space });
                        }

                        result.Add(geometryObjectCollection_Space);
                    }


                    if (!legendUpdated)
                    {
                        if (legend != null)
                        {
                            twoDimensionalViewSettings.Legend = legend;
                            legendUpdated = true;
                        }
                        else
                        {
                            if (dictionary_LegendItem != null && dictionary_LegendItem.Count != 0)
                            {
                                twoDimensionalViewSettings.Legend = new Legend(Query.LegendName<Space>(twoDimensionalViewSettings), dictionary_LegendItem.Values);
                                legendUpdated = true;
                            }
                            else
                            {
                                twoDimensionalViewSettings.Legend = null;
                            }
                        }
                    }
                }
            }

            result.SetValue(GeometryObjectModelParameter.ViewSettings, twoDimensionalViewSettings);

            return result;
        }
    }
}
