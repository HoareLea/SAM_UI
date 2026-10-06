// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using SAM.Core.UI.WPF;
using SAM.Geometry.Object;
using SAM.Geometry.Object.Spatial;
using SAM.Geometry.Planar;
using SAM.Geometry.Spatial;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace SAM.Geometry.UI.WPF
{
    /// <summary>
    /// 2D renderer for floor plan (TwoDimensionalViewSettings) views. Draws the same
    /// GeometryObjectModel content as the Helix viewport, but as flat WPF DrawingVisuals:
    /// pan/zoom are a matrix transform (no 3D camera, no clip planes), hit-testing is
    /// WPF 2D hit-testing and only the affected visual is redrawn on hover/selection.
    ///
    /// Experimental - hosted by ViewportControl only when the SAM_UI_FLOORPLAN_2D
    /// environment variable is set (see ViewportControl.FloorPlan2DEnabled).
    ///
    /// Interoperability: each top level Geometry3DObjectCollection (space, panel) gets a
    /// detached stub ModelVisual3D carrying the same IJSAMObject attached property as the
    /// Helix visuals, so existing ObjectHoovered/ObjectDoubleClicked/ObjectContextMenuOpening
    /// consumers (AnalyticalWindow) keep working unchanged.
    /// </summary>
    public class FloorPlan2DControl : FrameworkElement
    {
        private class Item
        {
            public ISAMGeometryObject SAMGeometryObject;
            public SAMObject SAMObject;
            public Guid Guid;
            public DrawingVisual DrawingVisual;
            public Rect Bounds = Rect.Empty;
            public ModelVisual3D Stub;
        }

        private const double zoomFactor = 1.25;
        private const double minScale = 0.000001;
        private const double maxScale = 100000000;

        private readonly ContainerVisual containerVisual;
        private readonly ContainerVisual overlayVisual;

        private readonly List<Item> items = new List<Item>();
        private readonly Dictionary<DrawingVisual, Item> dictionary_Visual = new Dictionary<DrawingVisual, Item>();
        private readonly Dictionary<Guid, Item> dictionary_Guid = new Dictionary<Guid, Item>();
        private readonly HashSet<Item> selectedItems = new HashSet<Item>();
        private Item hooveredItem;

        private Spatial.Plane plane;
        private System.Windows.Media.Matrix worldToScreen = System.Windows.Media.Matrix.Identity;
        private bool zoomExtentsPending;

        // Camera waiting to be applied once the control has a plane and a real size (view-settings
        // restore can arrive before the tab is ever shown) - see SetCamera/GetCamera.
        private Camera pendingCamera;

        private bool panning;
        private System.Windows.Point panPoint;

        public event ObjectHooveredEventHandler ObjectHoovered;
        public event ObjectDoubleClickedEventHandler ObjectDoubleClicked;
        public event ObjectSelectionChangedEventHandler ObjectSelectionChanged;

        public FloorPlan2DControl()
        {
            containerVisual = new ContainerVisual();
            AddVisualChild(containerVisual);

            overlayVisual = new ContainerVisual();
            AddVisualChild(overlayVisual);

            Focusable = true;
            ClipToBounds = true;
        }

        /// <summary>
        /// A drawing surface on top of the plan, for callers that need to annotate it - the Part F
        /// airflow overlay is the first. Deliberately the ONLY hook this control offers them.
        /// <para>
        /// Contents are in <b>screen</b> coordinates, not world coordinates, and this control never
        /// transforms them. Annotations carry text and arrowheads, which have to stay a readable size at
        /// every zoom level; putting them under <see cref="worldToScreen"/> would scale them with the
        /// building. Subscribe to <see cref="ViewChanged"/> and redraw against
        /// <see cref="WorldToScreen"/> instead.
        /// </para>
        /// <para>
        /// Excluded from this control's own hit testing, so an annotation drawn over a space never
        /// swallows the click that selects it. A caller that wants its annotations clickable hit-tests
        /// this visual itself.
        /// </para>
        /// </summary>
        public ContainerVisual Overlay
        {
            get
            {
                return overlayVisual;
            }
        }

        /// <summary>
        /// The plane this floor plan is a section on, or null before anything is loaded. World
        /// coordinates in <see cref="WorldToScreen"/> are this plane's 2D coordinates.
        /// </summary>
        public Spatial.Plane Plane
        {
            get
            {
                return plane;
            }
        }

        /// <summary>The current world-to-screen transform. World Y points up, screen Y points down.</summary>
        public System.Windows.Media.Matrix WorldToScreen
        {
            get
            {
                return worldToScreen;
            }
        }

        /// <summary>
        /// Raised whenever <see cref="WorldToScreen"/> changes - pan, zoom, camera restore, resize - so an
        /// <see cref="Overlay"/> drawn in screen coordinates can be redrawn to match.
        /// </summary>
        public event EventHandler ViewChanged;

        protected override int VisualChildrenCount
        {
            get
            {
                return 2;
            }
        }

        protected override Visual GetVisualChild(int index)
        {
            //Order matters: the overlay is returned second so it renders on top of the plan.
            return index == 0 ? containerVisual : overlayVisual;
        }

        public void Load(GeometryObjectModel geometryObjectModel)
        {
            using (Core.UI.PerformanceLog.Measure("FloorPlan2DControl.Load"))
            {
                bool firstLoad = items.Count == 0;

                Clear();

                if (geometryObjectModel == null)
                {
                    plane = null;
                    return;
                }

                plane = null;
                if (geometryObjectModel.TryGetValue(GeometryObjectModelParameter.ViewSettings, out ViewSettings viewSettings))
                {
                    plane = (viewSettings as TwoDimensionalViewSettings)?.Plane;
                }

                if (plane == null)
                {
                    return;
                }

                List<ISAMGeometryObject> sAMGeometryObjects = geometryObjectModel.GetSAMGeometryObjects<ISAMGeometryObject>();
                if (sAMGeometryObjects == null)
                {
                    return;
                }

                foreach (ISAMGeometryObject sAMGeometryObject in sAMGeometryObjects)
                {
                    if (sAMGeometryObject == null)
                    {
                        continue;
                    }

                    Item item = new Item();
                    item.SAMGeometryObject = sAMGeometryObject;
                    item.SAMObject = (sAMGeometryObject as ITaggable)?.Tag?.Value as SAMObject;
                    item.Guid = item.SAMObject == null ? Guid.NewGuid() : item.SAMObject.Guid;
                    item.DrawingVisual = new DrawingVisual();

                    Render(item, false, false);

                    item.Stub = new ModelVisual3D();
                    if (sAMGeometryObject is IJSAMObject)
                    {
                        Core.UI.WPF.Modify.SetIJSAMObject(item.Stub, (IJSAMObject)sAMGeometryObject);
                    }

                    items.Add(item);
                    dictionary_Visual[item.DrawingVisual] = item;
                    dictionary_Guid[item.Guid] = item;
                    containerVisual.Children.Add(item.DrawingVisual);
                }

                // Keep the user's pan/zoom on regeneration; zoom to extents on first load only
                if (firstLoad)
                {
                    if (pendingCamera != null)
                    {
                        // A camera restored before the first geometry load (no plane yet) wins
                        Camera camera = pendingCamera;
                        pendingCamera = null;
                        SetCamera(camera);
                    }
                    else if (ActualWidth > 0 && ActualHeight > 0)
                    {
                        ZoomExtents();
                    }
                    else
                    {
                        zoomExtentsPending = true;
                    }
                }
            }
        }

        public void Clear()
        {
            containerVisual.Children.Clear();
            items.Clear();
            dictionary_Visual.Clear();
            dictionary_Guid.Clear();
            selectedItems.Clear();
            hooveredItem = null;
        }

        public bool Contains(Guid guid)
        {
            return dictionary_Guid.ContainsKey(guid);
        }

        public bool ContainsAny(IEnumerable<Guid> guids)
        {
            if (guids == null)
            {
                return false;
            }

            foreach (Guid guid in guids)
            {
                if (dictionary_Guid.ContainsKey(guid))
                {
                    return true;
                }
            }

            return false;
        }

        public ModelVisual3D GetStubVisual3D(Guid guid)
        {
            return dictionary_Guid.TryGetValue(guid, out Item item) ? item?.Stub : null;
        }

        public List<T> SAMObjects<T>() where T : SAMObject
        {
            List<T> result = new List<T>();
            foreach (Item item in items)
            {
                if (item.SAMObject is T)
                {
                    result.Add((T)item.SAMObject);
                }
            }

            return result;
        }

        public List<SAMObject> SelectedSAMObjects()
        {
            List<SAMObject> result = new List<SAMObject>();
            foreach (Item item in selectedItems)
            {
                if (item.SAMObject != null)
                {
                    result.Add(item.SAMObject);
                }
            }

            return result;
        }

        public bool Select(IEnumerable<Guid> guids)
        {
            List<Item> items_Selected = new List<Item>();
            if (guids != null)
            {
                foreach (Guid guid in guids)
                {
                    if (dictionary_Guid.TryGetValue(guid, out Item item) && item != null)
                    {
                        items_Selected.Add(item);
                    }
                }
            }

            HashSet<Item> selectedItems_Previous = new HashSet<Item>(selectedItems);

            List<Item> items_Redraw = new List<Item>(selectedItems);

            selectedItems.Clear();
            foreach (Item item in items_Selected)
            {
                selectedItems.Add(item);
                if (!items_Redraw.Contains(item))
                {
                    items_Redraw.Add(item);
                }
            }

            foreach (Item item in items_Redraw)
            {
                Render(item, item == hooveredItem, selectedItems.Contains(item));
            }

            // Selection changed also when an empty/nonmatching set cleared previous items - callers raise ObjectSelectionChanged from this
            return !selectedItems.SetEquals(selectedItems_Previous);
        }

        public bool ClearSelection()
        {
            if (selectedItems.Count == 0)
            {
                return false;
            }

            List<Item> items_Redraw = new List<Item>(selectedItems);
            selectedItems.Clear();

            foreach (Item item in items_Redraw)
            {
                Render(item, item == hooveredItem, false);
            }

            return true;
        }

        public void SelectByScreenRect(Rect rect, SelectionType selectionType)
        {
            if (rect == Rect.Empty || selectionType == SelectionType.Undefined)
            {
                return;
            }

            List<Guid> guids = new List<Guid>();
            foreach (Item item in items)
            {
                if (item.Bounds == Rect.Empty || !(item.SAMObject != null))
                {
                    continue;
                }

                Rect rect_Screen = Rect.Transform(item.Bounds, worldToScreen);

                bool selected = selectionType == SelectionType.Inside ? rect.Contains(rect_Screen) : rect.IntersectsWith(rect_Screen);
                if (selected)
                {
                    guids.Add(item.Guid);
                }
            }

            Select(guids);
        }

        /// <summary>
        /// The current pan/zoom expressed as a camera over the section plane, so floor-plan view
        /// positions persist through the existing ViewSettings.Camera plumbing (PR #30 review):
        /// the camera sits over the world point at the viewport center, looking down with the
        /// 2D look direction convention of ViewportControl.UpdateMode, and the height above the
        /// plane encodes the zoom level (height = world height visible in the viewport). Returns
        /// the still-pending camera while the view has not been shown yet (so a save round-trip
        /// does not lose it), or null when there is nothing to report (Helix fallback).
        /// </summary>
        public Camera GetCamera()
        {
            if (pendingCamera != null)
            {
                return pendingCamera;
            }

            double scale = worldToScreen.M11;
            if (plane == null || ActualWidth <= 0 || ActualHeight <= 0 || scale <= 0)
            {
                return null;
            }

            double x = (ActualWidth / 2 - worldToScreen.OffsetX) / scale;
            double y = (worldToScreen.OffsetY - ActualHeight / 2) / scale;

            Spatial.Point3D point3D = plane.Convert(new Point2D(x, y));
            if (point3D == null)
            {
                return null;
            }

            // Floor-plan planes are horizontal: encode the zoom as height straight above the plane
            double worldHeight = ActualHeight / scale;

            return new Camera(new Spatial.Point3D(point3D.X, point3D.Y, point3D.Z + worldHeight), new Spatial.Vector3D(0, 0.0001, -0.9999), new Spatial.Vector3D(0, 1, 0));
        }

        /// <summary>
        /// Applies a camera produced by GetCamera (or a legacy Helix orthographic one): pans to
        /// the camera location projected onto the section plane and zooms so the height above the
        /// plane spans the viewport. Stored as pending while the plane/size are not available yet.
        /// </summary>
        public void SetCamera(Camera camera)
        {
            if (camera?.Location == null)
            {
                return;
            }

            if (plane == null || ActualWidth <= 0 || ActualHeight <= 0)
            {
                pendingCamera = camera;
                return;
            }

            pendingCamera = null;

            Point2D point2D = Spatial.Query.Convert(plane, camera.Location);
            Spatial.Point3D point3D_OnPlane = point2D == null ? null : plane.Convert(point2D);
            if (point2D == null || point3D_OnPlane == null)
            {
                return;
            }

            // Legacy cameras (saved by the Helix orthographic path) carry an arbitrary height -
            // the clamp keeps the pan and falls back to a sane zoom instead of degenerating
            double worldHeight = camera.Location.Distance(point3D_OnPlane);
            double scale = worldHeight > 0 ? ActualHeight / worldHeight : double.MaxValue;
            if (scale < minScale)
            {
                scale = minScale;
            }

            if (scale > maxScale)
            {
                scale = maxScale;
            }

            worldToScreen = new System.Windows.Media.Matrix(scale, 0, 0, -scale, ActualWidth / 2 - scale * point2D.X, ActualHeight / 2 + scale * point2D.Y);
            ApplyTransform();
        }

        public void ZoomExtents()
        {
            pendingCamera = null;

            Rect bounds = Rect.Empty;
            foreach (Item item in items)
            {
                if (item.Bounds == Rect.Empty)
                {
                    continue;
                }

                bounds.Union(item.Bounds);
            }

            ZoomWorldRect(bounds);
        }

        public void Zoom(IEnumerable<Guid> guids)
        {
            if (guids == null)
            {
                return;
            }

            Rect bounds = Rect.Empty;
            foreach (Guid guid in guids)
            {
                if (dictionary_Guid.TryGetValue(guid, out Item item) && item != null && item.Bounds != Rect.Empty)
                {
                    bounds.Union(item.Bounds);
                }
            }

            ZoomWorldRect(bounds);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            // Transparent background so mouse events are received over empty areas (pan/zoom)
            drawingContext.DrawRectangle(Brushes.Transparent, null, new Rect(RenderSize));
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);

            if (ActualWidth <= 0 || ActualHeight <= 0)
            {
                return;
            }

            // A restored camera wins over the first-load zoom-extents
            if (pendingCamera != null && plane != null)
            {
                zoomExtentsPending = false;

                Camera camera = pendingCamera;
                pendingCamera = null;
                SetCamera(camera);
                return;
            }

            if (zoomExtentsPending)
            {
                zoomExtentsPending = false;
                ZoomExtents();
                return;
            }

            //A resize moves nothing in world space but changes where every world point lands on screen, so
            //a screen-space overlay still has to be redrawn. ZoomExtents and SetCamera above reach
            //ApplyTransform themselves.
            ViewChanged?.Invoke(this, EventArgs.Empty);
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);

            // Zoom around the cursor position
            System.Windows.Point point = e.GetPosition(this);
            double factor = e.Delta > 0 ? zoomFactor : 1 / zoomFactor;

            double scale = worldToScreen.M11 * factor;
            if (scale < minScale || scale > maxScale)
            {
                return;
            }

            System.Windows.Media.Matrix matrix = worldToScreen;
            matrix.ScaleAt(factor, factor, point.X, point.Y);
            worldToScreen = matrix;
            ApplyTransform();

            e.Handled = true;
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);

            Focus();

            bool pan = e.ChangedButton == MouseButton.Middle || (e.ChangedButton == MouseButton.Left && (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)));
            if (pan)
            {
                panning = true;
                panPoint = e.GetPosition(this);
                CaptureMouse();
                e.Handled = true;
                return;
            }

            if (e.ChangedButton != MouseButton.Left)
            {
                return;
            }

            if (e.ClickCount == 2)
            {
                Item item_DoubleClicked = HitTestItem(e.GetPosition(this));
                if (item_DoubleClicked != null)
                {
                    ObjectDoubleClicked?.Invoke(this, new ObjectDoubleClickedEventArgs(e, item_DoubleClicked.Stub));
                    e.Handled = true;
                }

                return;
            }

            Item item = HitTestItem(e.GetPosition(this));

            bool control = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);

            bool changed = false;
            if (item == null)
            {
                if (!control)
                {
                    changed = ClearSelection();
                }
            }
            else
            {
                List<Item> items_Redraw = new List<Item>();

                if (!control)
                {
                    items_Redraw.AddRange(selectedItems);
                    selectedItems.Clear();
                }

                if (selectedItems.Contains(item))
                {
                    selectedItems.Remove(item);
                }
                else
                {
                    selectedItems.Add(item);
                }

                if (!items_Redraw.Contains(item))
                {
                    items_Redraw.Add(item);
                }

                foreach (Item item_Redraw in items_Redraw)
                {
                    Render(item_Redraw, item_Redraw == hooveredItem, selectedItems.Contains(item_Redraw));
                }

                changed = true;
            }

            if (changed)
            {
                ObjectSelectionChanged?.Invoke(this, new ObjectSelectionChangedEventArgs());
            }
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);

            if (panning)
            {
                panning = false;
                ReleaseMouseCapture();
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            System.Windows.Point point = e.GetPosition(this);

            if (panning)
            {
                System.Windows.Media.Matrix matrix = worldToScreen;
                matrix.Translate(point.X - panPoint.X, point.Y - panPoint.Y);
                worldToScreen = matrix;
                panPoint = point;
                ApplyTransform();
                return;
            }

            Item item;
            using (Core.UI.PerformanceLog.Measure("FloorPlan2DControl.HoverHitTest", null, 25))
            {
                item = HitTestItem(point);
            }

            if (item != hooveredItem)
            {
                Item item_Previous = hooveredItem;
                hooveredItem = item;

                if (item_Previous != null)
                {
                    Render(item_Previous, false, selectedItems.Contains(item_Previous));
                }

                if (hooveredItem != null)
                {
                    Render(hooveredItem, true, selectedItems.Contains(hooveredItem));
                }
            }

            if (hooveredItem != null)
            {
                ObjectHoovered?.Invoke(this, new ObjectHooveredEventArgs(e, hooveredItem.Stub));
            }
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);

            if (hooveredItem != null)
            {
                Item item_Previous = hooveredItem;
                hooveredItem = null;
                Render(item_Previous, false, selectedItems.Contains(item_Previous));
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key == Key.Escape)
            {
                if (ClearSelection())
                {
                    ObjectSelectionChanged?.Invoke(this, new ObjectSelectionChangedEventArgs());
                }
            }
        }

        private void ApplyTransform()
        {
            containerVisual.Transform = new MatrixTransform(worldToScreen);

            //Every pan, zoom, camera restore and resize funnels through here, so this is the one place an
            //overlay has to be told the view moved.
            ViewChanged?.Invoke(this, EventArgs.Empty);
        }

        private Item HitTestItem(System.Windows.Point point)
        {
            Visual visual_Hit = null;

            //The overlay subtree is skipped rather than searched. An annotation drawn over a space would
            //otherwise be the topmost visual under the pointer, HitTestItem would fail to find it in
            //dictionary_Visual, and the click would be lost instead of selecting the space underneath.
            VisualTreeHelper.HitTest(
                this,
                x => x == overlayVisual ? HitTestFilterBehavior.ContinueSkipSelfAndChildren : HitTestFilterBehavior.Continue,
                x =>
                {
                    visual_Hit = x?.VisualHit as Visual;
                    return HitTestResultBehavior.Stop;
                },
                new PointHitTestParameters(point));

            DrawingVisual drawingVisual = visual_Hit as DrawingVisual;
            if (drawingVisual == null)
            {
                return null;
            }

            return dictionary_Visual.TryGetValue(drawingVisual, out Item item) ? item : null;
        }

        private void ZoomWorldRect(Rect rect)
        {
            if (rect == Rect.Empty || rect.Width == 0 || rect.Height == 0 || ActualWidth <= 0 || ActualHeight <= 0)
            {
                return;
            }

            double scale = System.Math.Min(ActualWidth / rect.Width, ActualHeight / rect.Height) / 1.05;
            if (scale < minScale)
            {
                scale = minScale;
            }

            if (scale > maxScale)
            {
                scale = maxScale;
            }

            // World Y points up, screen Y points down -> negative M22
            worldToScreen = new System.Windows.Media.Matrix(scale, 0, 0, -scale, ActualWidth / 2 - scale * (rect.X + rect.Width / 2), ActualHeight / 2 + scale * (rect.Y + rect.Height / 2));
            ApplyTransform();
        }

        private void Render(Item item, bool hoovered, bool selected)
        {
            if (item?.DrawingVisual == null)
            {
                return;
            }

            Rect bounds = Rect.Empty;
            using (DrawingContext drawingContext = item.DrawingVisual.RenderOpen())
            {
                Render(drawingContext, item.SAMGeometryObject, hoovered, selected, ref bounds);
            }

            item.Bounds = bounds;
        }

        private void Render(DrawingContext drawingContext, ISAMGeometryObject sAMGeometryObject, bool hoovered, bool selected, ref Rect bounds)
        {
            if (sAMGeometryObject == null)
            {
                return;
            }

            if (sAMGeometryObject is Geometry3DObjectCollection)
            {
                foreach (ISAMGeometry3DObject sAMGeometry3DObject in (Geometry3DObjectCollection)sAMGeometryObject)
                {
                    Render(drawingContext, sAMGeometry3DObject, hoovered, selected, ref bounds);
                }

                return;
            }

            if (sAMGeometryObject is Face3DObject)
            {
                Render(drawingContext, (Face3DObject)sAMGeometryObject, hoovered, selected, ref bounds);
            }
            else if (sAMGeometryObject is Segment3DObject)
            {
                Render(drawingContext, (Segment3DObject)sAMGeometryObject, hoovered, selected, ref bounds);
            }
            else if (sAMGeometryObject is Text3DObject)
            {
                Render(drawingContext, (Text3DObject)sAMGeometryObject, ref bounds);
            }
        }

        private void Render(DrawingContext drawingContext, Face3DObject face3DObject, bool hoovered, bool selected, ref Rect bounds)
        {
            SurfaceAppearance surfaceAppearance = face3DObject?.SurfaceAppearance;
            if (surfaceAppearance == null || !surfaceAppearance.Visible || surfaceAppearance.Opacity == 0)
            {
                return;
            }

            var face3D = face3DObject.Face3D;
            if (face3D == null)
            {
                return;
            }

            Face2D face2D = plane.Convert(face3D);
            if (face2D == null)
            {
                return;
            }

            StreamGeometry streamGeometry = new StreamGeometry() { FillRule = FillRule.EvenOdd };
            using (StreamGeometryContext streamGeometryContext = streamGeometry.Open())
            {
                foreach (var edge2D in face2D.Edge2Ds)
                {
                    List<Point2D> point2Ds = edge2D?.Polygon2D()?.Points;
                    if (point2Ds == null || point2Ds.Count < 3)
                    {
                        continue;
                    }

                    streamGeometryContext.BeginFigure(ToWindows(point2Ds[0]), true, true);
                    for (int i = 1; i < point2Ds.Count; i++)
                    {
                        streamGeometryContext.LineTo(ToWindows(point2Ds[i]), true, true);
                    }
                }
            }
            streamGeometry.Freeze();

            System.Drawing.Color color_Fill = selected ? System.Drawing.Color.FromArgb(125, 125, 255) : surfaceAppearance.Color;
            double opacity = selected ? 1 : surfaceAppearance.Opacity;

            Brush brush = ToBrush(color_Fill, opacity);

            Pen pen = null;
            CurveAppearance curveAppearance = surfaceAppearance.CurveAppearance;
            if (selected)
            {
                pen = ToPen(System.Drawing.Color.FromArgb(0, 0, 255), 1, 0.01);
            }
            else if (curveAppearance != null && curveAppearance.Thickness > 0)
            {
                double thickness = curveAppearance.Thickness;
                if (hoovered)
                {
                    thickness = thickness < 0.01 ? 0.02 : thickness * 2;
                }

                pen = ToPen(curveAppearance.Color, curveAppearance.Opacity, thickness);
            }
            else if (hoovered)
            {
                pen = ToPen(curveAppearance == null ? color_Fill : curveAppearance.Color, 1, 0.02);
            }

            drawingContext.DrawGeometry(brush, pen, streamGeometry);

            bounds.Union(streamGeometry.Bounds);
        }

        private void Render(DrawingContext drawingContext, Segment3DObject segment3DObject, bool hoovered, bool selected, ref Rect bounds)
        {
            CurveAppearance curveAppearance = segment3DObject?.CurveAppearance;
            if (curveAppearance == null || curveAppearance.Opacity == 0)
            {
                return;
            }

            var segment3D = segment3DObject.Segment3D;
            if (segment3D == null)
            {
                return;
            }

            Point2D point2D_1 = Spatial.Query.Convert(plane, segment3D[0]);
            Point2D point2D_2 = Spatial.Query.Convert(plane, segment3D[1]);
            if (point2D_1 == null || point2D_2 == null)
            {
                return;
            }

            double thickness = curveAppearance.Thickness <= 0 ? 0.01 : curveAppearance.Thickness;
            if (hoovered || selected)
            {
                thickness = thickness < 0.01 ? 0.02 : thickness * 2;
            }

            System.Drawing.Color color = selected ? System.Drawing.Color.FromArgb(0, 0, 255) : curveAppearance.Color;

            Pen pen = ToPen(color, curveAppearance.Opacity, thickness);

            System.Windows.Point point_1 = ToWindows(point2D_1);
            System.Windows.Point point_2 = ToWindows(point2D_2);

            drawingContext.DrawLine(pen, point_1, point_2);

            Rect rect = new Rect(point_1, point_2);
            rect.Inflate(thickness / 2, thickness / 2);
            bounds.Union(rect);
        }

        private void Render(DrawingContext drawingContext, Text3DObject text3DObject, ref Rect bounds)
        {
            TextAppearance textAppearance = text3DObject?.TextAppearance;
            if (textAppearance == null || textAppearance.Opacity == 0)
            {
                return;
            }

            string text = text3DObject.Text;
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            var plane_Text = text3DObject.Plane;
            if (plane_Text == null)
            {
                return;
            }

            Point2D point2D = Spatial.Query.Convert(plane, plane_Text.Origin);
            if (point2D == null)
            {
                return;
            }

            string fontFamilyName = string.IsNullOrWhiteSpace(textAppearance.FontFamilyName) ? "Arial" : textAppearance.FontFamilyName;

            FormattedText formattedText = new FormattedText(
                text,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface(fontFamilyName),
                textAppearance.Height,
                ToBrush(textAppearance.Color, textAppearance.Opacity),
                VisualTreeHelper.GetDpi(this).PixelsPerDip);

            System.Windows.Point point = ToWindows(point2D);

            // The container transform flips Y (world Y up -> screen Y down); flip back locally so text reads correctly
            drawingContext.PushTransform(new ScaleTransform(1, -1, point.X, point.Y));
            drawingContext.DrawText(formattedText, new System.Windows.Point(point.X - formattedText.Width / 2, point.Y - formattedText.Height / 2));
            drawingContext.Pop();

            Rect rect = new Rect(point.X - formattedText.Width / 2, point.Y - formattedText.Height / 2, formattedText.Width, formattedText.Height);
            bounds.Union(rect);
        }

        private static System.Windows.Point ToWindows(Point2D point2D)
        {
            return new System.Windows.Point(point2D.X, point2D.Y);
        }

        private static Brush ToBrush(System.Drawing.Color color, double opacity)
        {
            SolidColorBrush result = new SolidColorBrush(System.Windows.Media.Color.FromRgb(color.R, color.G, color.B));
            result.Opacity = opacity;
            result.Freeze();
            return result;
        }

        private static Pen ToPen(System.Drawing.Color color, double opacity, double thickness)
        {
            Pen result = new Pen(ToBrush(color, opacity), thickness);
            result.Freeze();
            return result;
        }
    }
}
