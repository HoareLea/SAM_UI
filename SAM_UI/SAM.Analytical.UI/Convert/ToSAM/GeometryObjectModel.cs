using SAM.Core.UI;
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
            //adjacencyCluster.UpdateNormals(true, true);
            //adjacencyCluster.Normalize(true, Geometry.Orientation.CounterClockwise);

            Legend legend = threeDimensionalViewSettings.Legend;

            GeometryObjectModel result = new GeometryObjectModel();

            List<Panel> panels = adjacencyCluster.GetPanels();

            bool showPanels = threeDimensionalViewSettings.IsValid(typeof(Panel));
            bool showApertures = threeDimensionalViewSettings.IsValid(typeof(Aperture));
            bool showSpaces = threeDimensionalViewSettings.IsValid(typeof(Space));

            if (showPanels && panels != null)
            {
                foreach (Panel panel in panels)
                {
                    GeometryObjectCollection geometryObjectCollection_Panel = new GeometryObjectCollection() { Tag = panel };

                    Face3D face3D = panel.GetFace3D(true);

                    List<Face3D> face3Ds_FixEdges = face3D.FixEdges();
                    if (face3Ds_FixEdges != null && face3Ds_FixEdges.Count != 0)
                    {
                        if (face3Ds_FixEdges.Count != 1)
                        {
                            face3Ds_FixEdges.Sort((x, y) => y.GetArea().CompareTo(x.GetArea()));
                        }

                        face3D = face3Ds_FixEdges.Find(x => x.IsValid());
                    }

                    if(face3D == null || !face3D.IsValid())
                    {
                        continue;
                    }

                    geometryObjectCollection_Panel.Add(new Face3DObject(face3D, Query.SurfaceAppearance(panel, threeDimensionalViewSettings)));

                    if (showApertures)
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
                                    foreach (Face3D face3D_Temp in face3Ds)
                                    {
                                        if (face3D_Temp == null)
                                        {
                                            continue;
                                        }

                                        //face3D_Temp.Normalize(Geometry.Orientation.CounterClockwise);

                                        SurfaceAppearance surfaceAppearance = Query.SurfaceAppearance(aperture, aperturePart, threeDimensionalViewSettings);

                                        geometryObjectCollection_Aperture.Add(new Face3DObject(face3D_Temp, surfaceAppearance));
                                    }
                                }

                                aperturePart = AperturePart.Pane;
                                face3Ds = aperture.GetFace3Ds(aperturePart);
                                if (face3Ds != null && face3Ds.Count != 0)
                                {
                                    foreach (Face3D face3D_Temp in face3Ds)
                                    {
                                        if (face3D_Temp == null)
                                        {
                                            continue;
                                        }

                                        SurfaceAppearance surfaceAppearance = Query.SurfaceAppearance(aperture, aperturePart, threeDimensionalViewSettings);

                                        //face3D_Temp.Normalize(Geometry.Orientation.CounterClockwise);

                                        geometryObjectCollection_Aperture.Add(new Face3DObject(face3D_Temp, surfaceAppearance));
                                    }
                                }

                                geometryObjectCollection_Panel.Add(geometryObjectCollection_Aperture);
                            }
                        }
                    }

                    result.Add(geometryObjectCollection_Panel);
                }
            }

            if (showApertures && !showPanels)
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
                if(spaces != null && spaces.Count != 0)
                {
                    List<LegendItemData> legendItemDatas = new List<LegendItemData>();
                    foreach(Space space in spaces)
                    {
                        if (Query.TryGetValue(space, adjacencyCluster, threeDimensionalViewSettings, out object value, out string text))
                        {
                            legendItemDatas.Add(new LegendItemData(space, value, text));
                        }
                    }
                    
                    bool editable = Query.Editable<SpaceAppearanceSettings>(threeDimensionalViewSettings);

                    //Dictionary<System.Guid, LegendItem> dictionary_LegendItem = Query.LegendItemDictionary(spaces, adjacencyCluster, threeDimensionalViewSettings, Query.UndefinedLegendItem());
                    Dictionary<System.Guid, LegendItem> dictionary_LegendItem = Query.LegendItemDictionary(legendItemDatas, editable, Query.UndefinedLegendItem());
                    if (legend != null && dictionary_LegendItem != null && dictionary_LegendItem.Count != 0)
                    {
                        legend.Refresh(dictionary_LegendItem.Values, true, true);
                    }

                    foreach (Space space in spaces)
                    {
                        Shell shell = adjacencyCluster.Shell(space);

                        Color? color = null;

                        if (dictionary_LegendItem.TryGetValue(space.Guid, out LegendItem legendItem) && legendItem != null)
                        {
                            if(legend != null)
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

                        GeometryObjectCollection geometryObjectCollection_Space = new GeometryObjectCollection() { Tag = space };
                        geometryObjectCollection_Space.Add(new ShellObject(shell, surfaceAppearance) { Tag = space });

                        result.Add(geometryObjectCollection_Space);
                    }

                    if(legend != null)
                    {
                        threeDimensionalViewSettings.Legend = legend;
                    }
                    else
                    {
                        threeDimensionalViewSettings.Legend = dictionary_LegendItem != null && dictionary_LegendItem.Count != 0 ? new Legend(Query.LegendName(threeDimensionalViewSettings), dictionary_LegendItem.Values) : null;
                    }
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

            Legend legend = twoDimensionalViewSettings.Legend;

            AdjacencyCluster adjacencyCluster = analyticalModel_Temp.AdjacencyCluster;

            bool showPanels = twoDimensionalViewSettings.IsValid(typeof(Panel));
            bool showApertures = twoDimensionalViewSettings.IsValid(typeof(Aperture));
            bool showSpaces = twoDimensionalViewSettings.IsValid(typeof(Space));

            GeometryObjectModel result = new GeometryObjectModel();

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
                    Dictionary<Space, List<Face3D>> dictionary_Space = new Dictionary<Space, List<Face3D>>();
                    foreach (Space space in spaces)
                    {
                        Shell shell = adjacencyCluster?.Shell(space);
                        List<Face3D> face3Ds = shell?.Section(plane);
                        if (face3Ds == null || face3Ds.Count == 0)
                        {
                            continue;
                        }

                        dictionary_Space[space] = face3Ds;
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
                    Dictionary<System.Guid, LegendItem> dictionary_LegendItem = Query.LegendItemDictionary(legendItemDatas, editable, Query.UndefinedLegendItem());
                    if (legend != null)
                    {
                        legend.Refresh(dictionary_LegendItem.Values, true, true);
                    }


                    foreach (KeyValuePair<Space, List<Face3D>> keyValuePair in dictionary_Space)
                    {
                        Space space = keyValuePair.Key;
                        List<Face3D> face3Ds = keyValuePair.Value;

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
                        if (face3Ds == null || face3Ds.Count == 0)
                        {
                            continue;
                        }

                        Point3D point3D = plane.Project(space.Location);
                        if(point3D == null)
                        {
                            if(face3Ds.Count > 1)
                            {
                                face3Ds.Sort((x, y) => y.GetArea().CompareTo(x.GetArea()));
                            }

                            point3D = face3Ds[0].InternalPoint3D();
                        }

                        Color? color = null;

                        if (dictionary_LegendItem.TryGetValue(space.Guid, out LegendItem legendItem))
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

                        System.Drawing.Color color_Darker = ControlPaint.Dark(color.Value.ToDrawing());


                        GeometryObjectCollection geometryObjectCollection_Space = new GeometryObjectCollection() { Tag = space };

                        SurfaceAppearance surfaceAppearance = Query.SurfaceAppearance(space, twoDimensionalViewSettings, new SurfaceAppearance(color.Value, color_Darker.ToMedia(), 0.02));

                        face3Ds.ForEach(x => geometryObjectCollection_Space.Add(new Face3DObject(x, surfaceAppearance)));

                        Plane plane_Temp = new Plane(plane, point3D.GetMoved(new Vector3D(0, 0, 0.1)) as Point3D);

                        if(twoDimensionalViewSettings.TextAppearance == null || twoDimensionalViewSettings.TextAppearance.Opacity != 0)
                        {
                            geometryObjectCollection_Space.Add(new Text3DObject(space.Name, plane_Temp, Query.TextAppearance(space, twoDimensionalViewSettings)) { Tag = space });
                        }

                        result.Add(geometryObjectCollection_Space);
                    }

                    if (legend != null)
                    {
                        twoDimensionalViewSettings.Legend = legend;
                    }
                    else
                    {
                        twoDimensionalViewSettings.Legend = dictionary_LegendItem != null && dictionary_LegendItem.Count != 0 ? new Legend(Query.LegendName(twoDimensionalViewSettings), dictionary_LegendItem.Values) : null;
                    }
                }
            }

            result.SetValue(GeometryObjectModelParameter.ViewSettings, twoDimensionalViewSettings);

            return result;
        }
    }
}
