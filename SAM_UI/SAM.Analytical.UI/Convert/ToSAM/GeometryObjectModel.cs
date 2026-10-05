// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using SAM.Core.UI;
using SAM.Geometry.Object;
using SAM.Geometry.Object.Spatial;
using SAM.Geometry.Planar;
using SAM.Geometry.Spatial;
using SAM.Geometry.UI;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Forms;
using System.Windows.Media;

namespace SAM.Analytical.UI
{
    public static partial class Convert
    {
        // Per-view memoization of the floor-plan label solver (Solver2D.Solve, the dominant cost in a
        // floor-plan regeneration). Keyed by view guid; the stored signature captures every solver input
        // (per space: name, anchor, label size, limit-area boundary), so any change re-solves. Holds only
        // solved label positions (keyed by space guid) - never object references - to avoid stale state.
        private static readonly Dictionary<Guid, Tuple<string, Dictionary<Guid, Tuple<Point2D, bool>>>> labelSolveCache
            = new Dictionary<Guid, Tuple<string, Dictionary<Guid, Tuple<Point2D, bool>>>>();
        private static readonly object labelSolveCacheLock = new object();

        // Round to mm for the cache signature so float noise does not cause spurious misses.
        private static string Sig(double value)
        {
            return Math.Round(value, 3).ToString(CultureInfo.InvariantCulture);
        }

        // Per-space cache of the (expensive, topological) AdjacencyCluster.Shell(space) build, shared by the
        // 2D floor-plan and 3D paths. Keyed by space guid; the stored signature captures the geometry of the
        // space's bounding panels, so a geometry edit re-builds while an attribute-only edit is a cache hit.
        private static readonly Dictionary<Guid, Tuple<string, Shell>> shellCache = new Dictionary<Guid, Tuple<string, Shell>>();
        private static readonly object shellCacheLock = new object();

        // Per-space cache of the sectioned-and-converted floor-plan Face2Ds. Keyed by space guid; the signature
        // adds the cut plane to the shell signature, so a hit skips both the shell build AND the shell.Section
        // call. Holds geometry only - the per-space visibility gate and edge offset are applied at the use site
        // because they are attribute/setting driven, not geometric.
        private static readonly Dictionary<Guid, Tuple<string, List<Face2D>>> sectionCache = new Dictionary<Guid, Tuple<string, List<Face2D>>>();
        private static readonly object sectionCacheLock = new object();

        // Per-panel cache of the edge-fixed Face3Ds (panel.GetFace3Ds(true) + per-face FixEdges). FixEdges is
        // the dominant 3D-panel cost (View3D.Panels.FixEdges ~4.7 s on a 10k model) and is purely geometric, so
        // it is memoized like the space shell / section caches: keyed by panel guid with a face-geometry
        // signature, a geometry edit re-builds while attribute / view-settings edits are cache hits.
        private static readonly Dictionary<Guid, Tuple<string, List<Face3D>>> panelFaceCache = new Dictionary<Guid, Tuple<string, List<Face3D>>>();
        private static readonly object panelFaceCacheLock = new object();

        // Per-panel cache of the section-cut Face3Ds (the edge-fixed faces cut by the view's section planes and
        // filtered to the kept side). View3D.Panels.Cut is ~1.8 s on a 10k model when View Range section planes are
        // active and was re-cut on every regeneration; it is purely geometric, so it is memoized exactly like the
        // 2D sectionCache: keyed by panel guid with a signature combining the cut input geometry AND the planes, so
        // a geometry edit or a section-plane change re-cuts while attribute / view-settings edits are cache hits.
        // Without section planes the cut step does not run, so this cache is only populated for sectioned views.
        private static readonly Dictionary<Guid, Tuple<string, List<Face3D>>> panelCutCache = new Dictionary<Guid, Tuple<string, List<Face3D>>>();
        private static readonly object panelCutCacheLock = new object();

        // All of these caches are static and keyed per object guid, so a long-running session that opens many
        // large models (each with fresh guids) would otherwise grow without bound. Cap the entry count and clear
        // when a new key would exceed it - the caps are well above any single model's space / panel count, so the
        // active model stays fully cached and only cross-model accumulation is bounded.
        private const int maxCachedSpaces = 50000;
        private const int maxCachedPanels = 250000;

        // Signature of a space's shell-input geometry: the guids and vertices of its bounding panels (the same
        // panels AdjacencyCluster.Shell builds from). O(vertices) - cheap relative to the shell topology build.
        private static string SpaceGeometrySignature(AdjacencyCluster adjacencyCluster, Space space)
        {
            if (adjacencyCluster == null || space == null)
            {
                return null;
            }

            List<Panel> panels = adjacencyCluster.GetPanels(space);
            if (panels == null || panels.Count == 0)
            {
                return null;
            }

            List<string> parts = new List<string>(panels.Count);
            foreach (Panel panel in panels)
            {
                if (panel == null)
                {
                    continue;
                }

                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append(panel.Guid).Append('|');

                List<IClosedPlanar3D> edge3Ds = panel.GetFace3D()?.GetEdge3Ds();
                if (edge3Ds != null)
                {
                    foreach (IClosedPlanar3D edge3D in edge3Ds)
                    {
                        List<Point3D> point3Ds = (edge3D as ISegmentable3D)?.GetPoints();
                        if (point3Ds == null)
                        {
                            continue;
                        }

                        foreach (Point3D point3D in point3Ds)
                        {
                            stringBuilder.Append(Sig(point3D.X)).Append(',').Append(Sig(point3D.Y)).Append(',').Append(Sig(point3D.Z)).Append(';');
                        }

                        stringBuilder.Append('#');
                    }
                }

                parts.Add(stringBuilder.ToString());
            }

            // Sort so the signature is independent of the panel enumeration order.
            parts.Sort(StringComparer.Ordinal);
            return string.Join("\n", parts);
        }

        // Signature of a cut plane (origin + axes + normal), rounded so float noise does not cause misses.
        private static string PlaneSignature(Plane plane)
        {
            if (plane == null)
            {
                return string.Empty;
            }

            StringBuilder stringBuilder = new StringBuilder();
            AppendVectorSignature(stringBuilder, plane.Origin?.X, plane.Origin?.Y, plane.Origin?.Z);
            stringBuilder.Append('|');
            AppendVectorSignature(stringBuilder, plane.AxisX?.X, plane.AxisX?.Y, plane.AxisX?.Z);
            stringBuilder.Append('|');
            AppendVectorSignature(stringBuilder, plane.AxisY?.X, plane.AxisY?.Y, plane.AxisY?.Z);
            stringBuilder.Append('|');
            AppendVectorSignature(stringBuilder, plane.Normal?.X, plane.Normal?.Y, plane.Normal?.Z);
            return stringBuilder.ToString();
        }

        // Signature of an ordered set of cut planes - the cut result depends on every plane, so concatenate them.
        private static string PlanesSignature(List<Plane> planes)
        {
            if (planes == null || planes.Count == 0)
            {
                return string.Empty;
            }

            StringBuilder stringBuilder = new StringBuilder();
            foreach (Plane plane in planes)
            {
                stringBuilder.Append(PlaneSignature(plane)).Append('@');
            }

            return stringBuilder.ToString();
        }

        private static void AppendVectorSignature(StringBuilder stringBuilder, double? x, double? y, double? z)
        {
            if (x == null || y == null || z == null)
            {
                return;
            }

            stringBuilder.Append(Sig(x.Value)).Append(',').Append(Sig(y.Value)).Append(',').Append(Sig(z.Value));
        }

        // Returns the space's Shell, reusing a cached instance when the bounding-panel geometry is unchanged.
        // The Shell is consumed read-only downstream (Section / Cut), so sharing an instance built from an
        // earlier model clone is safe.
        private static Shell GetShell(AdjacencyCluster adjacencyCluster, Space space, out bool cacheHit)
        {
            cacheHit = false;
            if (adjacencyCluster == null || space == null)
            {
                return null;
            }

            string signature = SpaceGeometrySignature(adjacencyCluster, space);

            if (signature != null)
            {
                lock (shellCacheLock)
                {
                    if (shellCache.TryGetValue(space.Guid, out Tuple<string, Shell> cached) && cached != null && string.Equals(cached.Item1, signature, StringComparison.Ordinal))
                    {
                        cacheHit = true;
                        return cached.Item2;
                    }
                }
            }

            Shell shell = adjacencyCluster.Shell(space);

            if (signature != null)
            {
                lock (shellCacheLock)
                {
                    if (shellCache.Count >= maxCachedSpaces && !shellCache.ContainsKey(space.Guid))
                    {
                        shellCache.Clear();
                    }

                    shellCache[space.Guid] = new Tuple<string, Shell>(signature, shell);
                }
            }

            return shell;
        }

        // Signature of a Face3D list: every edge vertex, rounded to mm so float noise does not cause misses.
        // Order-sensitive - panel.GetFace3Ds(true) returns a stable order for a given panel.
        private static string Face3DsSignature(List<Face3D> face3Ds)
        {
            if (face3Ds == null || face3Ds.Count == 0)
            {
                return null;
            }

            StringBuilder stringBuilder = new StringBuilder();
            foreach (Face3D face3D in face3Ds)
            {
                if (face3D == null)
                {
                    stringBuilder.Append("null");
                    stringBuilder.Append('@');
                    continue;
                }

                List<IClosedPlanar3D> edge3Ds = face3D.GetEdge3Ds();
                if (edge3Ds != null)
                {
                    foreach (IClosedPlanar3D edge3D in edge3Ds)
                    {
                        List<Point3D> point3Ds = (edge3D as ISegmentable3D)?.GetPoints();
                        if (point3Ds == null)
                        {
                            continue;
                        }

                        foreach (Point3D point3D in point3Ds)
                        {
                            stringBuilder.Append(Sig(point3D.X)).Append(',').Append(Sig(point3D.Y)).Append(',').Append(Sig(point3D.Z)).Append(';');
                        }

                        stringBuilder.Append('#');
                    }
                }

                stringBuilder.Append('@');
            }

            return stringBuilder.ToString();
        }

        // Returns the panel's edge-fixed Face3Ds, reusing a cached result when the panel's face geometry is
        // unchanged. FixEdges per face is the dominant 3D-panel cost and is purely geometric, so it is memoized
        // like the space shell / section caches. The cached faces are consumed read-only downstream (Cut /
        // Face3DObject), so sharing an instance built from an earlier model clone is safe. Returns null when the
        // panel carries no faces.
        private static List<Face3D> GetPanelFace3Ds(Panel panel, out bool cacheHit)
        {
            cacheHit = false;
            if (panel == null)
            {
                return null;
            }

            List<Face3D> face3Ds = panel.GetFace3Ds(true);
            if (face3Ds == null || face3Ds.Count == 0)
            {
                return null;
            }

            string signature = Face3DsSignature(face3Ds);

            if (signature != null)
            {
                lock (panelFaceCacheLock)
                {
                    if (panelFaceCache.TryGetValue(panel.Guid, out Tuple<string, List<Face3D>> cached) && cached != null && string.Equals(cached.Item1, signature, StringComparison.Ordinal))
                    {
                        cacheHit = true;
                        return cached.Item2;
                    }
                }
            }

            // Cache miss: run FixEdges per face (the expensive step), keeping the existing semantics - replace
            // each face with the largest valid edge-fixed face (or null if none is valid, as before).
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

            if (signature != null)
            {
                lock (panelFaceCacheLock)
                {
                    if (panelFaceCache.Count >= maxCachedPanels && !panelFaceCache.ContainsKey(panel.Guid))
                    {
                        panelFaceCache.Clear();
                    }

                    panelFaceCache[panel.Guid] = new Tuple<string, List<Face3D>>(signature, face3Ds);
                }
            }

            return face3Ds;
        }

        // Returns the panel's faces cut by the view's section planes (and filtered to the kept side), reusing a
        // cached result when the input faces AND the planes are unchanged. face.Cut(planes) is the dominant 3D-panel
        // cost when section planes are active (View3D.Panels.Cut ~1.8 s on a 10k model) and is purely geometric, so
        // it is memoized like the 2D sectionCache. The signature combines the cut input geometry and the planes, so
        // a geometry edit OR a section-plane change re-cuts while attribute / view-settings edits are cache hits.
        // The cached faces are consumed read-only downstream (Face3DObject), so sharing an instance is safe.
        private static List<Face3D> GetPanelCutFace3Ds(Panel panel, List<Face3D> face3Ds, List<Plane> planes, out bool cacheHit)
        {
            cacheHit = false;
            if (panel == null || face3Ds == null || face3Ds.Count == 0 || planes == null || planes.Count == 0)
            {
                return face3Ds;
            }

            string face3DsSignature = Face3DsSignature(face3Ds);
            string signature = face3DsSignature == null ? null : string.Concat(face3DsSignature, "\n@planes:", PlanesSignature(planes));

            if (signature != null)
            {
                lock (panelCutCacheLock)
                {
                    if (panelCutCache.TryGetValue(panel.Guid, out Tuple<string, List<Face3D>> cached) && cached != null && string.Equals(cached.Item1, signature, StringComparison.Ordinal))
                    {
                        cacheHit = true;
                        return cached.Item2;
                    }
                }
            }

            List<Face3D> face3Ds_Cut_All = new List<Face3D>();
            foreach (Face3D face3D in face3Ds)
            {
                List<Face3D> face3Ds_Cut = face3D.Cut(planes);
                face3Ds_Cut = face3Ds_Cut?.FindAll(x => planes.TrueForAll(y => Geometry.Spatial.Query.Above(y, x.GetCentroid(), 0)));
                if (face3Ds_Cut != null && face3Ds_Cut.Count != 0)
                {
                    face3Ds_Cut_All.AddRange(face3Ds_Cut);
                }
            }

            if (signature != null)
            {
                lock (panelCutCacheLock)
                {
                    if (panelCutCache.Count >= maxCachedPanels && !panelCutCache.ContainsKey(panel.Guid))
                    {
                        panelCutCache.Clear();
                    }

                    panelCutCache[panel.Guid] = new Tuple<string, List<Face3D>>(signature, face3Ds_Cut_All);
                }
            }

            return face3Ds_Cut_All;
        }

        // Returns the 2D floor-plan section faces for a space in the given plane, reusing cached geometry when
        // the space geometry AND the cut plane are unchanged. A hit avoids both the shell build and the section.
        /// <summary>
        /// The 2D outline of one space where it meets a floor plan's section plane, in that plane's own
        /// coordinates, or null where the space does not reach the plane.
        /// <para>
        /// The public face of the cached section this converter already builds for every floor plan.
        /// Exposed so an annotation layer - the Part F airflow overlay - can place its marks inside the
        /// real room outline instead of inventing a layout, and so it shares the cache rather than paying
        /// for the shell section a second time.
        /// </para>
        /// </summary>
        public static List<Face2D> SpaceSectionFace2Ds(this AdjacencyCluster adjacencyCluster, Space space, Plane plane)
        {
            return GetSectionFace2Ds(adjacencyCluster, space, plane, out _);
        }

        private static List<Face2D> GetSectionFace2Ds(AdjacencyCluster adjacencyCluster, Space space, Plane plane, out bool cacheHit)
        {
            cacheHit = false;
            if (adjacencyCluster == null || space == null || plane == null)
            {
                return null;
            }

            string geometrySignature = SpaceGeometrySignature(adjacencyCluster, space);
            string signature = geometrySignature == null ? null : string.Concat(geometrySignature, "\n@plane:", PlaneSignature(plane));

            if (signature != null)
            {
                lock (sectionCacheLock)
                {
                    if (sectionCache.TryGetValue(space.Guid, out Tuple<string, List<Face2D>> cached) && cached != null && string.Equals(cached.Item1, signature, StringComparison.Ordinal))
                    {
                        cacheHit = true;
                        return cached.Item2;
                    }
                }
            }

            // Section-cache miss: build via the shell cache (so a plane-only change still reuses the shell).
            Shell shell = GetShell(adjacencyCluster, space, out _);
            List<Face3D> face3Ds = shell?.Section(plane);
            if (face3Ds == null || face3Ds.Count == 0)
            {
                return null;
            }

            List<Face2D> result = face3Ds.ConvertAll(x => plane.Convert(x));

            if (signature != null)
            {
                lock (sectionCacheLock)
                {
                    if (sectionCache.Count >= maxCachedSpaces && !sectionCache.ContainsKey(space.Guid))
                    {
                        sectionCache.Clear();
                    }

                    sectionCache[space.Guid] = new Tuple<string, List<Face2D>>(signature, result);
                }
            }

            return result;
        }
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

            // AnalyticalModel.AdjacencyCluster already returns a fresh, isolated clone, so reading it straight
            // off the source model gives the same isolated cluster this regen needs - without also deep-cloning
            // the whole model (panels/spaces/apertures + material/profile libraries + relations) every regen.
            // The cluster was the only thing taken from the (previously cloned) model here.
            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;

            Legend legend = threeDimensionalViewSettings.Legend;

            Legend legend_Temp = legend is null ? null : new Legend(legend);

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
            Dictionary<Guid, Geometry3DObjectCollection> dictionary_Spaces = new Dictionary<Guid, Geometry3DObjectCollection>();
            if (showSpaces)
            {
                Legend legend_Spaces = legend_Temp is null ? null : new Legend(legend_Temp);

                List<Space> spaces = adjacencyCluster.GetSpaces();
                if (spaces != null && spaces.Count != 0)
                {
                    List<LegendItemData> legendItemDatas = new List<LegendItemData>();
                    foreach (Space space in spaces)
                    {
                        if (Query.TryGetValue(space, adjacencyCluster, threeDimensionalViewSettings, out object value, out string text, out _))
                        {
                            legendItemDatas.Add(new LegendItemData(space, value, text));
                        }
                    }

                    bool editable = Query.Editable<SpaceAppearanceSettings>(threeDimensionalViewSettings);

                    Dictionary<Guid, LegendItem> dictionary_LegendItem = Query.LegendItemDictionary(legendItemDatas, editable, Query.UndefinedLegendItem());
                    if (legend_Spaces != null)
                    {
                        if (dictionary_LegendItem != null && dictionary_LegendItem.Count != 0)
                        {
                            legend_Spaces.Refresh(dictionary_LegendItem.Values, true, true);
                        }
                        else
                        {
                            legend_Spaces = null;
                        }
                    }

                    System.Diagnostics.Stopwatch spaceShellsStopwatch = PerformanceLog.Enabled ? System.Diagnostics.Stopwatch.StartNew() : null;
                    int spaceShellCacheHits = 0;

                    // Split the SpaceShells aggregate into the topological shell build vs the cut step so a
                    // large-model run shows which dominates (shell build -> shell caching / triangulation
                    // cache #33; cut -> the per-shell geometry overlay ops). Diagnostic only - allocations
                    // and timing happen solely when the performance log is enabled.
                    double shellBuildMilliseconds = 0;
                    double shellCutMilliseconds = 0;

                    foreach (Space space in spaces)
                    {
                        Color? color = null;

                        if (dictionary_LegendItem.TryGetValue(space.Guid, out LegendItem legendItem) && legendItem != null)
                        {
                            if (legend_Spaces != null)
                            {
                                legendItem = legend_Spaces.Find(legendItem?.Text);
                            }

                            color = Color.FromRgb(legendItem.Color.R, legendItem.Color.G, legendItem.Color.B);
                        }

                        if (color == null || !color.HasValue)
                        {
                            color = System.Drawing.Color.FromKnownColor(System.Drawing.KnownColor.LightGray).ToMedia();
                        }

                        SurfaceAppearance surfaceAppearance = Query.SurfaceAppearance(space, threeDimensionalViewSettings, new SurfaceAppearance(color.Value.ToDrawing(), ControlPaint.Dark(color.Value.ToDrawing()), 0));
                        if (surfaceAppearance == null || surfaceAppearance.Opacity == 0 || !surfaceAppearance.Visible)
                        {
                            continue;
                        }

                        System.Diagnostics.Stopwatch shellBuildStopwatch = PerformanceLog.Enabled ? System.Diagnostics.Stopwatch.StartNew() : null;
                        Shell shell = GetShell(adjacencyCluster, space, out bool shellCacheHit);
                        if (shellBuildStopwatch != null)
                        {
                            shellBuildMilliseconds += shellBuildStopwatch.Elapsed.TotalMilliseconds;
                        }

                        if (shellCacheHit)
                        {
                            spaceShellCacheHits++;
                        }

                        if (shell == null)
                        {
                            continue;
                        }

                        List<Shell> shells = null;
                        if (planes != null && planes.Count != 0)
                        {
                            System.Diagnostics.Stopwatch shellCutStopwatch = PerformanceLog.Enabled ? System.Diagnostics.Stopwatch.StartNew() : null;
                            shells = shell.Cut(planes);
                            shells = shells.FindAll(x => planes.TrueForAll(y => Geometry.Spatial.Query.Above(y, x.InternalPoint3D(), 0)));
                            if (shellCutStopwatch != null)
                            {
                                shellCutMilliseconds += shellCutStopwatch.Elapsed.TotalMilliseconds;
                            }
                        }
                        else
                        {
                            shells = new List<Shell>() { shell };
                        }

                        if (shells == null || shells.Count == 0)
                        {
                            continue;
                        }

                        Geometry3DObjectCollection geometry3DObjectCollection_Space = new Geometry3DObjectCollection() { Tag = space };
                        foreach (Shell shell_Temp in shells)
                        {
                            geometry3DObjectCollection_Space.Add(new ShellObject(shell_Temp, surfaceAppearance) { Tag = space });
                        }

                        dictionary_Spaces[space.Guid] = geometry3DObjectCollection_Space;
                    }

                    if (spaceShellsStopwatch != null)
                    {
                        spaceShellsStopwatch.Stop();
                        PerformanceLog.Write("View3D.SpaceShells", string.Format("{0} [{1} spaces] [{2} cached]", threeDimensionalViewSettings.Name, spaces.Count, spaceShellCacheHits), spaceShellsStopwatch.Elapsed.TotalMilliseconds);
                        PerformanceLog.Write("View3D.SpaceShells.ShellBuild", string.Format("{0} [{1} spaces] [{2} cached]", threeDimensionalViewSettings.Name, spaces.Count, spaceShellCacheHits), shellBuildMilliseconds);
                        PerformanceLog.Write("View3D.SpaceShells.Cut", string.Format("{0} [{1} spaces]", threeDimensionalViewSettings.Name, spaces.Count), shellCutMilliseconds);
                    }

                    if (!legendUpdated)
                    {
                        if (legend_Spaces != null)
                        {
                            threeDimensionalViewSettings.Legend = legend_Spaces;
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
            Dictionary<Guid, Geometry3DObjectCollection> dictionary_Panels = new Dictionary<Guid, Geometry3DObjectCollection>();
            if (showPanels)
            {
                System.Diagnostics.Stopwatch panelsStopwatch = PerformanceLog.Enabled ? System.Diagnostics.Stopwatch.StartNew() : null;

                // Sub-split the panel loop: FixEdges (per-face edge cleanup, now cached in GetPanelFace3Ds) vs
                // Cut (per-face cut by the view's section planes). FixEdges time is incurred only on a cache miss.
                double panelFixEdgesMilliseconds = 0;
                double panelCutMilliseconds = 0;
                int panelFaceCacheHits = 0;
                int panelCutCacheHits = 0;

                Legend legend_Panels = legend_Temp is null ? null : new Legend(legend_Temp);

                List<Panel> panels = adjacencyCluster.GetPanels();
                if (panels != null && panels.Count != 0)
                {
                    List<LegendItemData> legendItemDatas = new List<LegendItemData>();
                    foreach (Panel panel in panels)
                    {
                        if (Query.TryGetValue(panel, adjacencyCluster, threeDimensionalViewSettings, out object value, out string text, out _))
                        {
                            legendItemDatas.Add(new LegendItemData(panel, value, text));
                        }
                    }

                    bool editable = Query.Editable<PanelAppearanceSettings>(threeDimensionalViewSettings);

                    Dictionary<Guid, LegendItem> dictionary_LegendItem = Query.LegendItemDictionary(legendItemDatas, editable, Query.UndefinedLegendItem());
                    if (legend_Panels != null)
                    {
                        if (dictionary_LegendItem != null && dictionary_LegendItem.Count != 0)
                        {
                            legend_Panels.Refresh(dictionary_LegendItem.Values, true, true);
                        }
                        else
                        {
                            legend_Panels = null;
                        }
                    }

                    foreach (Panel panel in panels)
                    {
                        Geometry3DObjectCollection geometry3DObjectCollection_Panel = new Geometry3DObjectCollection() { Tag = panel };

                        Color? color = null;

                        if (dictionary_LegendItem.TryGetValue(panel.Guid, out LegendItem legendItem) && legendItem != null)
                        {
                            if (legend_Panels != null)
                            {
                                legendItem = legend_Panels.Find(legendItem?.Text);
                            }

                            color = Color.FromRgb(legendItem.Color.R, legendItem.Color.G, legendItem.Color.B);
                        }

                        SurfaceAppearance surfaceAppearance = Query.SurfaceAppearance(panel, threeDimensionalViewSettings, color);
                        if (surfaceAppearance == null || surfaceAppearance.Opacity == 0 || !surfaceAppearance.Visible)
                        {
                            continue;
                        }

                        System.Diagnostics.Stopwatch panelFixEdgesStopwatch = PerformanceLog.Enabled ? System.Diagnostics.Stopwatch.StartNew() : null;
                        List<Face3D> face3Ds = GetPanelFace3Ds(panel, out bool panelFaceCacheHit);
                        if (panelFixEdgesStopwatch != null)
                        {
                            panelFixEdgesMilliseconds += panelFixEdgesStopwatch.Elapsed.TotalMilliseconds;
                        }

                        if (panelFaceCacheHit)
                        {
                            panelFaceCacheHits++;
                        }

                        if (face3Ds == null || face3Ds.Count == 0)
                        {
                            continue;
                        }

                        if (planes != null && planes.Count != 0)
                        {
                            System.Diagnostics.Stopwatch panelCutStopwatch = PerformanceLog.Enabled ? System.Diagnostics.Stopwatch.StartNew() : null;
                            face3Ds = GetPanelCutFace3Ds(panel, face3Ds, planes, out bool panelCutCacheHit);
                            if (panelCutCacheHit)
                            {
                                panelCutCacheHits++;
                            }

                            if (panelCutStopwatch != null)
                            {
                                panelCutMilliseconds += panelCutStopwatch.Elapsed.TotalMilliseconds;
                            }
                        }

                        if (face3Ds == null || face3Ds.Count == 0)
                        {
                            continue;
                        }

                        foreach (Face3D face3D_Temp in face3Ds)
                        {
                            geometry3DObjectCollection_Panel.Add(new Face3DObject(face3D_Temp, surfaceAppearance));
                        }

                        dictionary_Panels[panel.Guid] = geometry3DObjectCollection_Panel;
                    }

                    if (!legendUpdated)
                    {
                        if (legend_Panels != null)
                        {
                            threeDimensionalViewSettings.Legend = legend_Panels;
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

                if (panelsStopwatch != null)
                {
                    panelsStopwatch.Stop();
                    PerformanceLog.Write("View3D.Panels", string.Format("{0} [{1} cached]", threeDimensionalViewSettings.Name, panelFaceCacheHits), panelsStopwatch.Elapsed.TotalMilliseconds);
                    PerformanceLog.Write("View3D.Panels.FixEdges", string.Format("{0} [{1} cached]", threeDimensionalViewSettings.Name, panelFaceCacheHits), panelFixEdgesMilliseconds);
                    PerformanceLog.Write("View3D.Panels.Cut", string.Format("{0} [{1} cached]", threeDimensionalViewSettings.Name, panelCutCacheHits), panelCutMilliseconds);
                }
            }

            bool showApertures = threeDimensionalViewSettings.IsValid(typeof(Aperture));
            Dictionary<Guid, Geometry3DObjectCollection> dictionary_Apertures = new Dictionary<Guid, Geometry3DObjectCollection>();
            if (showApertures)
            {
                System.Diagnostics.Stopwatch aperturesStopwatch = PerformanceLog.Enabled ? System.Diagnostics.Stopwatch.StartNew() : null;

                Legend legend_Apertures = legend_Temp is null ? null : new Legend(legend_Temp);

                List<Aperture> apertures = adjacencyCluster.GetApertures();
                if (apertures != null && apertures.Count != 0)
                {
                    List<LegendItemData> legendItemDatas = new List<LegendItemData>();
                    foreach (Aperture aperture in apertures)
                    {
                        if (Query.TryGetValue(aperture, adjacencyCluster, threeDimensionalViewSettings, out object value, out string text, out _))
                        {
                            legendItemDatas.Add(new LegendItemData(aperture, value, text));
                        }
                    }

                    bool editable = Query.Editable<PanelAppearanceSettings>(threeDimensionalViewSettings);

                    Dictionary<Guid, LegendItem> dictionary_LegendItem = Query.LegendItemDictionary(legendItemDatas, editable, Query.UndefinedLegendItem());
                    if (legend_Apertures != null)
                    {
                        if (dictionary_LegendItem != null && dictionary_LegendItem.Count != 0)
                        {
                            legend_Apertures.Refresh(dictionary_LegendItem.Values, true, true);
                        }
                        else
                        {
                            legend_Apertures = null;
                        }
                    }

                    foreach (Aperture aperture in apertures)
                    {
                        Geometry3DObjectCollection geometry3DObjectCollection_Aperture = new Geometry3DObjectCollection() { Tag = aperture };

                        Color? color = null;

                        if (dictionary_LegendItem.TryGetValue(aperture.Guid, out LegendItem legendItem) && legendItem != null)
                        {
                            if (legend_Apertures != null)
                            {
                                legendItem = legend_Apertures.Find(legendItem?.Text);
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

                                face3Ds.ForEach(x => geometry3DObjectCollection_Aperture.Add(new Face3DObject(x, surfaceAppearance_Frame)));
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

                                face3Ds.ForEach(x => geometry3DObjectCollection_Aperture.Add(new Face3DObject(x, surfaceAppearance_Pane)));
                            }
                        }

                        dictionary_Apertures[aperture.Guid] = geometry3DObjectCollection_Aperture;
                    }

                    if (!legendUpdated)
                    {
                        if (legend_Apertures != null)
                        {
                            threeDimensionalViewSettings.Legend = legend_Apertures;
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

                if (aperturesStopwatch != null)
                {
                    aperturesStopwatch.Stop();
                    PerformanceLog.Write("View3D.Apertures", threeDimensionalViewSettings.Name, aperturesStopwatch.Elapsed.TotalMilliseconds);
                }
            }

            if (dictionary_Panels != null && dictionary_Panels.Count != 0)
            {
                if (dictionary_Apertures != null && dictionary_Apertures.Count != 0)
                {
                    HashSet<Guid> guids = new HashSet<Guid>();
                    foreach (KeyValuePair<Guid, Geometry3DObjectCollection> keyValuePair in dictionary_Apertures)
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

                        if (!dictionary_Panels.TryGetValue(panel.Guid, out Geometry3DObjectCollection geometry3DObjectCollection) || geometry3DObjectCollection == null)
                        {
                            continue;
                        }

                        geometry3DObjectCollection.Add(keyValuePair.Value);
                        guids.Add(keyValuePair.Key);
                    }

                    foreach (Guid guid in guids)
                    {
                        dictionary_Apertures.Remove(guid);
                    }
                }
            }

            foreach (Geometry3DObjectCollection geometry3DObjectCollection in dictionary_Apertures.Values)
            {
                result.Add(geometry3DObjectCollection);
            }

            foreach (Geometry3DObjectCollection geometry3DObjectCollection in dictionary_Panels.Values)
            {
                result.Add(geometry3DObjectCollection);
            }

            foreach (Geometry3DObjectCollection geometry3DObjectCollection in dictionary_Spaces.Values)
            {
                result.Add(geometry3DObjectCollection);
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

            Legend legend = twoDimensionalViewSettings.Legend;

            // AnalyticalModel.AdjacencyCluster already returns a fresh, isolated clone, so reading it straight
            // off the source model avoids deep-cloning the whole model every regen (see the 3D path above).
            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;

            bool showPanels = twoDimensionalViewSettings.IsValid(typeof(Panel));
            bool showApertures = twoDimensionalViewSettings.IsValid(typeof(Aperture));
            bool showSpaces = twoDimensionalViewSettings.IsValid(typeof(Space));

            GeometryObjectModel result = new GeometryObjectModel();

            bool legendUpdated = false;

            if (showPanels || showApertures)
            {
                Dictionary<Panel, List<ISegmentable3D>> dictionary;
                using (PerformanceLog.Measure("FloorPlan.SectionPanels", twoDimensionalViewSettings.Name))
                {
                    dictionary = Analytical.Query.SectionDictionary<ISegmentable3D>(adjacencyCluster.GetPanels(), plane);
                }

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
                        Geometry3DObjectCollection geometry3DObjectCollection_Panel = new Geometry3DObjectCollection() { Tag = keyValuePair.Key };
                        foreach (ISegmentable3D segmentable3D in keyValuePair.Value)
                        {
                            List<Segment3D> segment3Ds = segmentable3D?.GetSegments();
                            if (segment3Ds == null)
                            {
                                continue;
                            }

                            segment3Ds.ForEach(x => geometry3DObjectCollection_Panel.Add(new Segment3DObject(x, Query.CurveAppearance(keyValuePair.Key, twoDimensionalViewSettings))));
                        }
                        result.Add(geometry3DObjectCollection_Panel);
                    }
                }
            }

            if (showSpaces)
            {
                List<Space> spaces = adjacencyCluster.GetSpaces();

                if (spaces != null)
                {
                    Dictionary<Space, List<Face2D>> dictionary_Space = new Dictionary<Space, List<Face2D>>();
                    System.Diagnostics.Stopwatch sectionStopwatch = PerformanceLog.Enabled ? System.Diagnostics.Stopwatch.StartNew() : null;
                    int sectionCacheHits = 0;

                    foreach (Space space in spaces)
                    {
                        List<Face2D> face2Ds = GetSectionFace2Ds(adjacencyCluster, space, plane, out bool sectionCacheHit);
                        if (sectionCacheHit)
                        {
                            sectionCacheHits++;
                        }

                        if (face2Ds == null || face2Ds.Count == 0)
                        {
                            continue;
                        }

                        dictionary_Space[space] = face2Ds;
                    }

                    if (sectionStopwatch != null)
                    {
                        sectionStopwatch.Stop();
                        PerformanceLog.Write("FloorPlan.SectionSpaces", string.Format("{0} [{1} spaces] [{2} cached]", twoDimensionalViewSettings.Name, spaces.Count, sectionCacheHits), sectionStopwatch.Elapsed.TotalMilliseconds);
                    }

                    List<LegendItemData> legendItemDatas = new List<LegendItemData>();
                    foreach (Space space in dictionary_Space.Keys)
                    {
                        if (Query.TryGetValue(space, adjacencyCluster, twoDimensionalViewSettings, out object value, out string text, out _))
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

                        // Skip hidden spaces here - before the label solver - so a hidden space's tag
                        // does not participate in collision resolution and shift/blank visible labels.
                        // Mirrors the 3D path's visibility early-out; covers both the face and the label.
                        SurfaceAppearance surfaceAppearance = Query.SurfaceAppearance(space, twoDimensionalViewSettings);
                        if (surfaceAppearance == null || surfaceAppearance.Opacity == 0 || !surfaceAppearance.Visible)
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
                    List<string> labelSignatureParts = new List<string>();
                    int degenerateSectionCount = 0;

                    foreach (KeyValuePair<Space, List<Face2D>> keyValuePair in dictionary_Space)
                    {
                        string name = keyValuePair.Key.Name;
                        Space space = keyValuePair.Key;

                        TextAppearance textAppearance = Query.TextAppearance(space, twoDimensionalViewSettings);
                        double height = textAppearance.Height;
                        double width = SAM.Core.UI.WPF.Query.Width(name, new System.Drawing.Font(textAppearance.FontFamilyName, System.Convert.ToSingle(height)), height);

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
                            double distance = pointsTemp.Distance(point2D);

                            farthestPointDistance = Math.Max(distance, farthestPointDistance);
                        }

                        // Signature must capture the FULL limit area (face2Ds[0], passed as LimitArea below),
                        // including any inner loops/holes - not just the outer edge - so a change to a hole
                        // forces a re-solve rather than a false cache hit.
                        StringBuilder boundaryBuilder = new StringBuilder();
                        foreach (var edge2D in face2Ds[0].Edge2Ds)
                        {
                            List<Point2D> edgePoints = edge2D?.Polygon2D()?.Points;
                            if (edgePoints == null)
                            {
                                continue;
                            }

                            foreach (Point2D edgePoint in edgePoints)
                            {
                                boundaryBuilder.Append(Sig(edgePoint.X)).Append(',').Append(Sig(edgePoint.Y)).Append(';');
                            }

                            boundaryBuilder.Append('#');
                        }
                        Rectangle2D rectangle2D = new Rectangle2D(new Point2D(point2D.X + width / 2, point2D.Y + height / 2), width, height);

                        Solver2DData solver2DData = new Solver2DData(rectangle2D, point2D);
                        solver2DData.Tag = new Tuple<Space, List<Face2D>, string, Point2D>(keyValuePair.Key, keyValuePair.Value, name, point2D);

                        Solver2DSettings solver2DSettings = new Solver2DSettings();

                        // Degenerate section guard. A floor-plan plane taken at the wrong elevation (e.g. grazing the
                        // tops of the spaces) collapses each space's section to a sliver - near-zero area, often long
                        // and thin. The solver constrains a label's centre to LimitArea (this section), so for a sliver
                        // almost no candidate qualifies and the label exhausts the full IterationCount * 8 sweep before
                        // failing; across every space at once this turned the solve into a multi-minute hang on a 10k
                        // model. A sliver is identified by its *minimum* extent (the bounding-box short side - a long
                        // thin sliver still has a large farthestPointDistance, so reach alone misses it): when the
                        // section is thinner than the label in its short dimension, a label cannot meaningfully sit in
                        // it, so run a single iteration (place at the anchor) instead of the futile sweep. Real rooms
                        // are metres across, far larger than a label, so this never triggers for them.
                        BoundingBox2D sectionBoundingBox2D = face2Ds[0].GetBoundingBox();
                        double sectionMinExtent = sectionBoundingBox2D == null ? 0 : System.Math.Min(sectionBoundingBox2D.Width, sectionBoundingBox2D.Height);
                        bool degenerateSection = sectionMinExtent < System.Math.Min(width, height);
                        if (degenerateSection)
                        {
                            degenerateSectionCount++;
                        }
                        solver2DSettings.IterationCount = degenerateSection ? 1 : 100;
                        solver2DSettings.ShiftDistance = (farthestPointDistance - solver2DSettings.StartingDistance) / solver2DSettings.IterationCount;
                        solver2DSettings.LimitArea = face2Ds[0];
                        solver2DData.Solver2DSettings = solver2DSettings;

                        solver2DDatas.Add(solver2DData);

                        // Per-space contribution to the label-solver cache signature (see labelSolveCache).
                        labelSignatureParts.Add(string.Concat(space.Guid.ToString(), "|", name, "|", Sig(point2D.X), "|", Sig(point2D.Y), "|", Sig(width), "|", Sig(height), "|", Sig(farthestPointDistance), "|", boundaryBuilder.ToString()));
                    }

                    labelSignatureParts.Sort(StringComparer.Ordinal);
                    string labelSignature = string.Join("\n", labelSignatureParts);

                    Guid labelViewGuid = twoDimensionalViewSettings.Guid;
                    Dictionary<Guid, Tuple<Point2D, bool>> labelPositions = null;

                    lock (labelSolveCacheLock)
                    {
                        if (labelSolveCache.TryGetValue(labelViewGuid, out Tuple<string, Dictionary<Guid, Tuple<Point2D, bool>>> cached) && cached != null && string.Equals(cached.Item1, labelSignature, StringComparison.Ordinal))
                        {
                            labelPositions = cached.Item2;
                        }
                    }

                    if (labelPositions != null)
                    {
                        // Cache hit: identical solver inputs - reuse solved label positions, skip Solver2D.Solve().
                        PerformanceLog.Write("FloorPlan.LabelSolver", string.Format("{0} [{1} labels] [cached]", twoDimensionalViewSettings.Name, solver2DDatas.Count), 0);

                        foreach (Solver2DData solver2DData in solver2DDatas)
                        {
                            Tuple<Space, List<Face2D>, string, Point2D> tuple = solver2DData.Tag as Tuple<Space, List<Face2D>, string, Point2D>;
                            if (tuple == null)
                            {
                                continue;
                            }

                            if (labelPositions.TryGetValue(tuple.Item1.Guid, out Tuple<Point2D, bool> position) && position != null)
                            {
                                tuples.Add(new Tuple<Space, List<Face2D>, string, Point2D>(tuple.Item1, tuple.Item2, position.Item2 ? tuple.Item3 : "", position.Item1));
                            }
                            else
                            {
                                tuples.Add(tuple);
                            }
                        }
                    }
                    else
                    {
                        // Set the area as infinite so that the labels can extend beyond the faces
                        Rectangle2D area = new Rectangle2D(new Point2D(double.MinValue / 2, double.MinValue / 2), double.MaxValue, double.MaxValue);
                        Solver2D solver2D = new Solver2D(area, new List<IClosed2D>());
                        solver2D.AddRange(solver2DDatas);

                        List<Solver2DResult> solver2DResults;
                        using (PerformanceLog.Measure("FloorPlan.LabelSolver", string.Format("{0} [{1} labels]", twoDimensionalViewSettings.Name, solver2DDatas.Count)))
                        {
                            solver2DResults = solver2D.Solve();
                        }

                        // Diagnostic: how many labels the solver actually placed vs left at their anchor, and how
                        // many sections were flagged degenerate. Reveals which way a slow solve is failing (all
                        // placed but expensively, vs all unplaceable) so the bound can be targeted, not guessed.
                        //
                        // Counted from Solver2DResult.ResultType, not from "did it return geometry". A label the
                        // solver dropped at its anchor once it ran out of work budget also returns geometry, so
                        // counting non-null rectangles reported those as placed and hid the exact condition this
                        // line exists to reveal - the one where the solve gave up early.
                        if (PerformanceLog.Enabled)
                        {
                            int solvedCount = solver2DResults == null ? 0 : solver2DResults.FindAll(x => x?.ResultType == Solver2DResultType.Solved).Count;
                            int fallbackCount = solver2DResults == null ? 0 : solver2DResults.FindAll(x => x?.ResultType == Solver2DResultType.Fallback).Count;
                            int unplacedCount = solver2DResults == null ? solver2DDatas.Count : solver2DResults.FindAll(x => x?.ResultType == Solver2DResultType.Unplaced).Count;
                            PerformanceLog.Write("FloorPlan.LabelSolver.Placement", string.Format("{0} [{1} solved / {2} over budget / {3} unplaced of {4}] [{5} degenerate sections] [{6} work units of {7}]", twoDimensionalViewSettings.Name, solvedCount, fallbackCount, unplacedCount, solver2DDatas.Count, degenerateSectionCount, solver2D.WorkUnits, solver2D.WorkBudget), 0);
                        }

                        labelPositions = new Dictionary<Guid, Tuple<Point2D, bool>>();

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
                                    Point2D centroid = rectangle2D.GetCentroid();
                                    labelPositions[tuple.Item1.Guid] = new Tuple<Point2D, bool>(centroid, true);
                                    tuple = new Tuple<Space, List<Face2D>, string, Point2D>(tuple.Item1, tuple.Item2, tuple.Item3, centroid);
                                }
                                else
                                {
                                    labelPositions[tuple.Item1.Guid] = new Tuple<Point2D, bool>(tuple.Item4, false);
                                    tuple = new Tuple<Space, List<Face2D>, string, Point2D>(tuple.Item1, tuple.Item2, "", tuple.Item4);
                                }
                                tuples.Add(tuple);
                            }

                            // Only cache a real solve. If Solve() returned null the uncached path produced no
                            // labels; caching that empty state would make the next identical regen take the
                            // hit path and render every label at its anchor instead - so leave it uncached.
                            lock (labelSolveCacheLock)
                            {
                                labelSolveCache[labelViewGuid] = new Tuple<string, Dictionary<Guid, Tuple<Point2D, bool>>>(labelSignature, labelPositions);
                            }
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

                        Geometry3DObjectCollection geometry3DObjectCollection_Space = new Geometry3DObjectCollection() { Tag = space };

                        SurfaceAppearance surfaceAppearance = Query.SurfaceAppearance(space, twoDimensionalViewSettings, color);

                        face2Ds.ForEach(x => geometry3DObjectCollection_Space.Add(new Face3DObject(plane.Convert(x), surfaceAppearance)));

                        Plane plane_Temp = new Plane(plane, point3D.GetMoved(new Vector3D(0, 0, 0.1)) as Point3D);

                        if (twoDimensionalViewSettings.TextAppearance == null || twoDimensionalViewSettings.TextAppearance.Opacity != 0)
                        {
                            geometry3DObjectCollection_Space.Add(new Text3DObject(text, plane_Temp, Query.TextAppearance(space, twoDimensionalViewSettings)) { Tag = space });
                        }

                        result.Add(geometry3DObjectCollection_Space);
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
