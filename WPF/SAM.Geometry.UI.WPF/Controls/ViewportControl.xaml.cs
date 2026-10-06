// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using SAM.Core.UI;
using SAM.Core.UI.WPF;
using SAM.Geometry.Object;
using SAM.Geometry.Object.Spatial;
using SAM.Geometry.Planar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Media3D;

namespace SAM.Geometry.UI.WPF
{
    /// <summary>
    /// Interaction logic for ViewportControl.xaml
    /// </summary>
    public partial class ViewportControl : UserControl
    {
        // 2D floor plan renderer (see FloorPlan2DControl) - now ON by default (issue #18). Set
        // SAM_UI_FLOORPLAN_2D=0 to fall back to the legacy Helix orthographic 2D path.
        private static readonly bool floorPlan2DEnabled = ResolveFloorPlan2DEnabled();

        // Default on; only an explicit "0" opts back out to the Helix orthographic 2D path.
        private static bool ResolveFloorPlan2DEnabled()
        {
            string value = Environment.GetEnvironmentVariable("SAM_UI_FLOORPLAN_2D");
            return string.IsNullOrWhiteSpace(value) || value.Trim() != "0";
        }

        // Record the resolved flag state once, so the performance log shows whether the
        // 2D floor plan path is live in this process (the env var is read only at startup)
        private static int floorPlan2DStateLogged = 0;

        private ActionManager actionManager;
        private Mode mode = Mode.ThreeDimensional;

        private RectangularSelector rectangularSelector;
        private UIGeometryObjectModel uIGeometryObjectModel;

        private FloorPlan2DControl floorPlan2DControl;

        // DirectX 11 renderer for the 3D view (issue #18 gate 3) - null unless
        // SAM_UI_VIEWPORT_SHARPDX is set; see SharpDXViewportControl. While active the Helix
        // viewport stays hidden and empty. Hover and selection (issue #32 Phase C) are handled
        // inside the control and surface through the same Object* events as the other renderers;
        // the camera, view chrome and orthographic-3D toggle (Ctrl+Shift+O) are Phase D (issue #37),
        // all handled inside the control.
        private readonly SharpDXViewportControl sharpDXViewportControl;

        // 2D (orthographic) floor-plan clip-plane tracking (issue #13). Helix zoom/pan moves the
        // camera while the near/far planes are otherwise set once (UpdateMode), so section geometry
        // can leave the fixed depth slab and "disappear". We cache the scene's world-Z extent on Load
        // and re-fit the near/far planes around it on every camera change so it always stays visible.
        private const double clipPlaneMargin = 100;
        private double sceneZMin = double.NaN;
        private double sceneZMax = double.NaN;
        private bool updatingClipPlanes;

        // Guid -> visual index for the 3D (Helix) scene, rebuilt on each Load. Replaces the O(N) visual-tree
        // walks (Core.UI.WPF.Query.Visual3D / ContainsAny) that back GetVisual3D, ContainsAny and the #11
        // attribute fast-path RefreshAppearances with O(1) lookups (issue #16). The 2D path keeps using
        // FloorPlan2DControl's own guid index. UI-thread only, so no locking.
        private readonly Dictionary<Guid, ModelVisual3D> dictionary_Visual3D = new Dictionary<Guid, ModelVisual3D>();

        // Hover hit-test throttle: a full ray hit-test on every mouse-move is costly on large 3D scenes
        // (issue #16). Process at most once per interval; intermediate moves keep the current highlight.
        private const int hoverHitTestThrottleMilliseconds = 30;
        private int lastHoverHitTestTick;

        public ViewportControl()
        {
            InitializeComponent();

            // Log the 2D floor plan flag state once per process (raw env var value + resolved bool)
            if (System.Threading.Interlocked.Exchange(ref floorPlan2DStateLogged, 1) == 0)
            {
                string value = Environment.GetEnvironmentVariable("SAM_UI_FLOORPLAN_2D");
                PerformanceLog.Write("ViewportControl.FloorPlan2DEnabled", string.Format("{0} [SAM_UI_FLOORPLAN_2D={1}]", floorPlan2DEnabled, value ?? "(null)"), 0);

                string value_SharpDX = Environment.GetEnvironmentVariable("SAM_UI_VIEWPORT_SHARPDX");
                PerformanceLog.Write("ViewportControl.SharpDXViewportEnabled", string.Format("{0} [SAM_UI_VIEWPORT_SHARPDX={1}]", SharpDXViewportControl.Enabled, value_SharpDX ?? "(null)"), 0);
            }

            helixViewport3D.PanGesture = new MouseGesture(MouseAction.LeftClick, ModifierKeys.Shift);
            helixViewport3D.RotateGesture = new MouseGesture(MouseAction.RightClick, ModifierKeys.Shift);
            uIGeometryObjectModel = new UIGeometryObjectModel();

            helixViewport3D.Loaded += helixViewport3D_Loaded;

            actionManager = new ActionManager();
            rectangularSelector = new RectangularSelector(grid);
            rectangularSelector.Selected += RectangularSelector_Selected;

            rectangularSelector.Selecting += RectangularSelector_Selecting;

            floorPlan2DControl = new FloorPlan2DControl() { Visibility = Visibility.Collapsed, ContextMenu = new ContextMenu() };
            grid.Children.Insert(grid.Children.IndexOf(helixViewport3D) + 1, floorPlan2DControl);
            floorPlan2DControl.ObjectHoovered += FloorPlan2DControl_ObjectHoovered;
            floorPlan2DControl.ObjectDoubleClicked += FloorPlan2DControl_ObjectDoubleClicked;
            floorPlan2DControl.ObjectSelectionChanged += FloorPlan2DControl_ObjectSelectionChanged;
            floorPlan2DControl.ContextMenuOpening += FloorPlan2DControl_ContextMenuOpening;

            if (SharpDXViewportControl.Enabled)
            {
                sharpDXViewportControl = new SharpDXViewportControl();
                grid.Children.Insert(grid.Children.IndexOf(floorPlan2DControl) + 1, sharpDXViewportControl);
                sharpDXViewportControl.ObjectHoovered += SharpDXViewportControl_ObjectHoovered;
                sharpDXViewportControl.ObjectDoubleClicked += SharpDXViewportControl_ObjectDoubleClicked;
                sharpDXViewportControl.ObjectSelectionChanged += SharpDXViewportControl_ObjectSelectionChanged;
                sharpDXViewportControl.ContextMenu = new ContextMenu();
                sharpDXViewportControl.ContextMenuOpening += SharpDXViewportControl_ContextMenuOpening;

                // Mode defaults to ThreeDimensional, so the SharpDX viewport starts active and
                // the Helix one hidden; UpdateMode keeps the visibilities in sync afterwards
                sharpDXViewportControl.Visibility = Visibility.Visible;
                helixViewport3D.Visibility = Visibility.Collapsed;
            }
        }

        public event ObjectContextMenuOpeningEventHandler ObjectContextMenuOpening;

        public event ObjectDoubleClickedEventHandler ObjectDoubleClicked;

        public event ObjectHooveredEventHandler ObjectHoovered;
        public event ObjectSelectionChangedEventHandler ObjectSelectionChanged;

        private bool Active2D
        {
            get
            {
                return floorPlan2DEnabled && mode == Mode.TwoDimensional;
            }
        }

        /// <summary>
        /// The 2D floor plan currently being drawn, or null where this view is not drawing one - it is in 3D,
        /// or the legacy orthographic 2D path is in force (SAM_UI_FLOORPLAN_2D=0).
        /// <para>
        /// Exposed so that a layer above can annotate the plan through the control's own
        /// <see cref="FloorPlan2DControl.Overlay"/>, in screen space, against
        /// <see cref="FloorPlan2DControl.WorldToScreen"/> and its <see cref="FloorPlan2DControl.Plane"/>. The
        /// Part F airflow annotation is the first caller. Deliberately read-only and deliberately null in
        /// every other mode: an annotation layer that cannot see the plan it would draw on should draw
        /// nothing rather than guess.
        /// </para>
        /// </summary>
        public FloorPlan2DControl FloorPlan2D
        {
            get
            {
                return Active2D ? floorPlan2DControl : null;
            }
        }

        private bool ActiveSharpDX3D
        {
            get
            {
                return sharpDXViewportControl != null && mode == Mode.ThreeDimensional;
            }
        }

        /// <summary>
        /// True when an attribute-only edit on this view can be refreshed in place (recolor + legend)
        /// instead of regenerating the whole scene. 2D always supports it (canvas reload / Helix
        /// per-visual re-skin); the 3D view supports it only on the SharpDX path (per-object re-skin) -
        /// the legacy Helix 3D renderer has no in-place re-skin, so 3D edits there fall back to full
        /// regeneration (issue #32).
        /// </summary>
        public bool SupportsInPlaceAppearanceRefresh
        {
            get
            {
                return mode == Mode.TwoDimensional || ActiveSharpDX3D;
            }
        }

        private void FloorPlan2DControl_ObjectHoovered(object sender, ObjectHooveredEventArgs e)
        {
            ObjectHoovered?.Invoke(this, e);
        }

        private void FloorPlan2DControl_ObjectDoubleClicked(object sender, ObjectDoubleClickedEventArgs e)
        {
            ObjectDoubleClicked?.Invoke(this, e);
        }

        private void FloorPlan2DControl_ObjectSelectionChanged(object sender, ObjectSelectionChangedEventArgs e)
        {
            ObjectSelectionChanged?.Invoke(this, e);
        }

        private void SharpDXViewportControl_ObjectHoovered(object sender, ObjectHooveredEventArgs e)
        {
            ObjectHoovered?.Invoke(this, e);
        }

        private void SharpDXViewportControl_ObjectDoubleClicked(object sender, ObjectDoubleClickedEventArgs e)
        {
            ObjectDoubleClicked?.Invoke(this, e);
        }

        private void SharpDXViewportControl_ObjectSelectionChanged(object sender, ObjectSelectionChangedEventArgs e)
        {
            ObjectSelectionChanged?.Invoke(this, e);
        }

        private void FloorPlan2DControl_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) ||
                Keyboard.IsKeyDown(Key.RightCtrl) ||
                Keyboard.IsKeyDown(Key.LeftAlt) ||
                Keyboard.IsKeyDown(Key.RightAlt) ||
                Keyboard.IsKeyDown(Key.LeftShift) ||
                Keyboard.IsKeyDown(Key.RightShift))
            {
                e.Handled = true;
                return;
            }

            floorPlan2DControl.ContextMenu = new ContextMenu();

            MenuItem menuItem = new MenuItem();
            menuItem.Name = "MenuItem_ZoomExtents";
            menuItem.Header = "Zoom Extents";
            menuItem.Click += MenuItem_ZoomExtents_Click;
            floorPlan2DControl.ContextMenu.Items.Add(menuItem);

            AddZoomSelectedMenuItem(floorPlan2DControl.ContextMenu);

            List<ModelVisual3D> modelVisual3Ds = new List<ModelVisual3D>();
            foreach (SAMObject sAMObject in floorPlan2DControl.SelectedSAMObjects())
            {
                ModelVisual3D modelVisual3D = floorPlan2DControl.GetStubVisual3D(sAMObject.Guid);
                if (modelVisual3D != null)
                {
                    modelVisual3Ds.Add(modelVisual3D);
                }
            }

            ObjectContextMenuOpening?.Invoke(this, new ObjectContextMenuOpeningEventArgs(floorPlan2DControl.ContextMenu, e, modelVisual3Ds));
        }

        // Right-click menu for the SharpDX 3D view (issue #32 item 4) - mirrors the 2D handler above
        // and helixViewport3D_ContextMenuOpening: builds the Zoom Extents / Zoom Selected items and
        // fires ObjectContextMenuOpening with stub ModelVisual3Ds for the selected objects, so the
        // AnalyticalWindow consumer (Hide/Unhide/Isolate/properties) works unchanged.
        private void SharpDXViewportControl_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) ||
                Keyboard.IsKeyDown(Key.RightCtrl) ||
                Keyboard.IsKeyDown(Key.LeftAlt) ||
                Keyboard.IsKeyDown(Key.RightAlt) ||
                Keyboard.IsKeyDown(Key.LeftShift) ||
                Keyboard.IsKeyDown(Key.RightShift))
            {
                e.Handled = true;
                return;
            }

            sharpDXViewportControl.ContextMenu = new ContextMenu();

            MenuItem menuItem = new MenuItem();
            menuItem.Name = "MenuItem_ZoomExtents";
            menuItem.Header = "Zoom Extents";
            menuItem.Click += MenuItem_ZoomExtents_Click;
            sharpDXViewportControl.ContextMenu.Items.Add(menuItem);

            AddZoomSelectedMenuItem(sharpDXViewportControl.ContextMenu);

            // Perspective <-> orthographic toggle (issue #37 Phase D) - a discoverable mouse path for
            // the same switch as the Ctrl+Shift+O gesture; the check mark shows the current projection.
            MenuItem menuItem_Orthographic = new MenuItem();
            menuItem_Orthographic.Name = "MenuItem_Orthographic";
            menuItem_Orthographic.Header = "Orthographic";
            menuItem_Orthographic.IsCheckable = true;
            menuItem_Orthographic.IsChecked = sharpDXViewportControl.Orthographic;
            menuItem_Orthographic.Click += MenuItem_Orthographic_Click;
            sharpDXViewportControl.ContextMenu.Items.Add(menuItem_Orthographic);

            List<ModelVisual3D> modelVisual3Ds = new List<ModelVisual3D>();
            foreach (SAMObject sAMObject in sharpDXViewportControl.SelectedSAMObjects())
            {
                ModelVisual3D modelVisual3D = sharpDXViewportControl.GetStubVisual3D(sAMObject.Guid);
                if (modelVisual3D != null)
                {
                    modelVisual3Ds.Add(modelVisual3D);
                }
            }

            ObjectContextMenuOpening?.Invoke(this, new ObjectContextMenuOpeningEventArgs(sharpDXViewportControl.ContextMenu, e, modelVisual3Ds));
        }

        public Camera Camera
        {
            get
            {
                return GetCamera();
            }

            set
            {
                SetCamera(value);
            }
        }

        public Guid Guid { get; set; }
        
        public Mode Mode
        {
            get
            {
                return mode;
            }

            set
            {
                if (mode != value)
                {
                    mode = value;
                    UpdateMode();
                }
            }
        }

        public UIGeometryObjectModel UIGeometryObjectModel
        {
            get
            {
                return uIGeometryObjectModel;
            }

            set
            {
                uIGeometryObjectModel = value;
                if (uIGeometryObjectModel != null)
                {
                    uIGeometryObjectModel.Modified -= UIGeometryObjectModel_Modified;
                    uIGeometryObjectModel.Modified += UIGeometryObjectModel_Modified;

                    uIGeometryObjectModel.Closed -= UIGeometryObjectModel_Closed;
                    uIGeometryObjectModel.Closed += UIGeometryObjectModel_Closed;

                    uIGeometryObjectModel.Opened -= UIGeometryObjectModel_Opened;
                    uIGeometryObjectModel.Opened += UIGeometryObjectModel_Opened;
                }

                Load(uIGeometryObjectModel?.JSAMObject);

                gridLinesVisual3D.Visible = uIGeometryObjectModel?.JSAMObject == null;
            }
        }

        public LegendItem UndefinedLegendItem
        {
            get
            {
                return legendControl.UndefinedLegendItem;
            }

            set
            {
                legendControl.UndefinedLegendItem = value;
            }
        }

        public bool ContainsAny<T>(IEnumerable<Guid> guids) where T : SAMObject
        {
            if (Active2D)
            {
                if (guids == null)
                {
                    return false;
                }

                List<T> sAMObjects = floorPlan2DControl.SAMObjects<T>();
                return sAMObjects != null && sAMObjects.Find(x => guids.Contains(x.Guid)) != null;
            }

            if (guids == null)
            {
                return false;
            }

            if (ActiveSharpDX3D)
            {
                return sharpDXViewportControl.ContainsAny<T>(guids);
            }

            // 3D path: O(1) per guid via the index instead of an O(N) visual-tree walk (issue #16).
            foreach (Guid guid in guids)
            {
                if (dictionary_Visual3D.TryGetValue(guid, out ModelVisual3D modelVisual3D) && Carries<T>(modelVisual3D))
                {
                    return true;
                }
            }

            return false;
        }

        public bool Contains<T>(Guid guid) where T : SAMObject
        {
            return ContainsAny<T>([guid]);
        }

        public T GetSAMObject<T>(Guid guid) where T : SAMObject
        {
            return Core.UI.WPF.Query.JSAMObject<T>(GetVisual3D<T>(guid));
        }

        public Visual3D GetVisual3D<T>(Guid guid) where T : SAMObject
        {
            if (Active2D)
            {
                ModelVisual3D modelVisual3D = floorPlan2DControl.GetStubVisual3D(guid);
                if (modelVisual3D == null || Core.UI.WPF.Query.JSAMObject<T>(modelVisual3D) == null)
                {
                    return null;
                }

                return modelVisual3D;
            }

            if (ActiveSharpDX3D)
            {
                // Same stub interop as the 2D branch: a detached ModelVisual3D carrying the object,
                // resolvable by every existing consumer (issue #32 Phase C)
                ModelVisual3D modelVisual3D_Stub = sharpDXViewportControl.GetStubVisual3D(guid);
                if (modelVisual3D_Stub == null || Core.UI.WPF.Query.JSAMObject<T>(modelVisual3D_Stub) == null)
                {
                    return null;
                }

                return modelVisual3D_Stub;
            }

            // 3D path: O(1) index lookup instead of an O(N) visual-tree walk (issue #16). The indexed
            // visual is the outermost per-object container (Tag = space/panel), which is exactly what
            // RefreshAppearance (recurses children) and selection operate on.
            if (dictionary_Visual3D.TryGetValue(guid, out ModelVisual3D modelVisual3D_Indexed) && Carries<T>(modelVisual3D_Indexed))
            {
                return modelVisual3D_Indexed;
            }

            return null;
        }

        // True when the visual carries a T (directly or via ITaggable.Tag.Value) on itself or its Content -
        // mirrors the type match in Core.UI.WPF.Query.Visual3D so the index preserves ContainsAny/GetVisual3D
        // type semantics.
        private static bool Carries<T>(ModelVisual3D modelVisual3D) where T : SAMObject
        {
            if (modelVisual3D == null)
            {
                return false;
            }

            if (Core.UI.WPF.Query.JSAMObject<T>(modelVisual3D) != null)
            {
                return true;
            }

            return modelVisual3D.Content != null && Core.UI.WPF.Query.JSAMObject<T>(modelVisual3D.Content) != null;
        }

        /// <summary>
        /// In-place appearance refresh for attribute-only edits: re-skins the visuals of the given
        /// objects from the current UIGeometryObjectModel (whose appearances were already updated -
        /// see Modify.RefreshAppearance) and refreshes the legend, without rebuilding the scene.
        /// The 2D path redraws from the model directly (cheap; pan/zoom preserved).
        /// </summary>
        public bool RefreshAppearances(IEnumerable<Guid> guids)
        {
            GeometryObjectModel geometryObjectModel = uIGeometryObjectModel?.JSAMObject;
            if (geometryObjectModel == null)
            {
                return false;
            }

            if (Active2D)
            {
                floorPlan2DControl.Load(geometryObjectModel);
            }
            else if (ActiveSharpDX3D)
            {
                // In-place re-skin from the (already appearance-updated) model objects - camera and other
                // objects untouched (issue #32 / #11). Per-object mode rebuilds each object's GroupModel3D;
                // batched mode extracts the recoloured objects into small per-object fill meshes (#53). Returns
                // false only when the batched scene can't recolor an object in place, so the caller falls back
                // to a full regeneration instead of leaving the rendered colors stale.
                if (!sharpDXViewportControl.RefreshAppearance(guids))
                {
                    return false;
                }
            }
            else if (guids != null)
            {
                foreach (Guid guid in guids)
                {
                    Visual3D visual3D = GetVisual3D<SAMObject>(guid);
                    if (visual3D != null)
                    {
                        Modify.RefreshAppearance(visual3D);
                    }
                }
            }

            RefreshLegend(geometryObjectModel);
            return true;
        }

        public List<T> SAMObjects<T>() where T : SAMObject
        {
            if (Active2D)
            {
                return floorPlan2DControl.SAMObjects<T>();
            }

            List<ModelVisual3D> visual3Ds = Core.UI.WPF.Query.Visual3Ds<ModelVisual3D>(helixViewport3D.Children, new Type[] { typeof(GeometryObjectModel) });
            if (visual3Ds is null)
            {
                return null;
            }

            List<T> result = [];

            foreach (ModelVisual3D visual3D in visual3Ds)
            {
                T t = Core.UI.WPF.Query.JSAMObject<T>(visual3D);
                if (t is null)
                {
                    continue;
                }

                result.Add(t);
            }

            return result;
        }

        public bool Select(SAMObject sAMObject)
        {
            if (Active2D)
            {
                bool result2D = sAMObject == null ? floorPlan2DControl.ClearSelection() : floorPlan2DControl.Select([sAMObject.Guid]);
                if (result2D)
                {
                    ObjectSelectionChanged?.Invoke(this, new ObjectSelectionChangedEventArgs());
                }

                return result2D;
            }

            if (ActiveSharpDX3D)
            {
                bool result_SharpDX = sAMObject == null ? sharpDXViewportControl.ClearSelection() : sharpDXViewportControl.Select([sAMObject.Guid]);
                if (result_SharpDX)
                {
                    ObjectSelectionChanged?.Invoke(this, new ObjectSelectionChangedEventArgs());
                }

                return result_SharpDX;
            }

            bool result = false;

            if (sAMObject == null)
            {
                result = actionManager.Cancel<SelectAction>();
                if (result)
                {
                    ObjectSelectionChanged?.Invoke(this, new ObjectSelectionChangedEventArgs());
                }
                return result;
            }

            Visual3D visual3D = GetVisual3D<SAMObject>(sAMObject.Guid);
            if (visual3D == null)
            {
                return result;
            }

            result = actionManager.Apply(new SelectAction(visual3D));
            if (result)
            {
                ObjectSelectionChanged?.Invoke(this, new ObjectSelectionChangedEventArgs());
            }

            return result;
        }

        public bool Select<T>(IEnumerable<T> sAMObjects) where T : SAMObject
        {
            if (Active2D)
            {
                bool result2D;
                if (sAMObjects == null)
                {
                    result2D = floorPlan2DControl.ClearSelection();
                }
                else
                {
                    result2D = floorPlan2DControl.Select(new List<T>(sAMObjects).FindAll(x => x != null).ConvertAll(x => x.Guid));
                }

                if (result2D)
                {
                    ObjectSelectionChanged?.Invoke(this, new ObjectSelectionChangedEventArgs());
                }

                return result2D;
            }

            if (ActiveSharpDX3D)
            {
                bool result_SharpDX;
                if (sAMObjects == null)
                {
                    result_SharpDX = sharpDXViewportControl.ClearSelection();
                }
                else
                {
                    result_SharpDX = sharpDXViewportControl.Select(new List<T>(sAMObjects).FindAll(x => x != null).ConvertAll(x => x.Guid));
                }

                if (result_SharpDX)
                {
                    ObjectSelectionChanged?.Invoke(this, new ObjectSelectionChangedEventArgs());
                }

                return result_SharpDX;
            }

            bool result = false;

            if (sAMObjects == null)
            {
                result = actionManager.Cancel<SelectAction>();
                if (result)
                {
                    ObjectSelectionChanged?.Invoke(this, new ObjectSelectionChangedEventArgs());
                }
                return result;
            }

            List<Visual3D> visual3Ds = new List<Visual3D>();
            foreach (T t in sAMObjects)
            {
                Visual3D visual3D = GetVisual3D<SAMObject>(t.Guid);
                if (visual3D == null)
                {
                    continue;
                }

                visual3Ds.Add(visual3D);
            }

            if (visual3Ds == null || visual3Ds.Count == 0)
            {
                result = actionManager.Cancel<SelectAction>();
                if (result)
                {
                    ObjectSelectionChanged?.Invoke(this, new ObjectSelectionChangedEventArgs());
                }

                return result;
            }

            result = actionManager.Apply(new SelectAction(visual3Ds));
            if (result)
            {
                ObjectSelectionChanged?.Invoke(this, new ObjectSelectionChangedEventArgs());
            }

            return result;
        }
        
        public List<T> SelectedSAMObjects<T>() where T : SAMObject
        {
            if (Active2D)
            {
                List<T> result2D = new List<T>();
                foreach (SAMObject sAMObject in floorPlan2DControl.SelectedSAMObjects())
                {
                    if (sAMObject is T)
                    {
                        result2D.Add((T)sAMObject);
                    }
                }

                return result2D;
            }

            if (ActiveSharpDX3D)
            {
                List<T> result_SharpDX = new List<T>();
                foreach (SAMObject sAMObject in sharpDXViewportControl.SelectedSAMObjects())
                {
                    if (sAMObject is T)
                    {
                        result_SharpDX.Add((T)sAMObject);
                    }
                }

                return result_SharpDX;
            }

            if (actionManager == null)
            {
                return null;
            }

            List<SelectAction> selectActions = actionManager.GetActions<SelectAction>();
            if (selectActions == null || selectActions.Count == 0)
            {
                return null;
            }

            List<T> result = new List<T>();
            foreach (SelectAction selectAction in selectActions)
            {
                List<Visual3D> visual3Ds = selectAction.Visual3Ds;
                if (visual3Ds != null)
                {
                    foreach (Visual3D visual3D in visual3Ds)
                    {
                        ITaggable taggable = visual3D?.JSAMObject<IJSAMObject>() as ITaggable;
                        if (taggable != null)
                        {
                            T t = taggable.Tag?.Value as T;
                            if (t != null)
                            {
                                result.Add(t);
                            }
                        }
                    }
                }
            }

            return result;
        }

        public bool Zoom(SAMObject sAMObject)
        {
            if (sAMObject == null)
            {
                return false;
            }

            if (Active2D)
            {
                if (!floorPlan2DControl.Contains(sAMObject.Guid))
                {
                    return false;
                }

                floorPlan2DControl.Zoom([sAMObject.Guid]);
                return true;
            }

            if (ActiveSharpDX3D)
            {
                return sharpDXViewportControl.Zoom([sAMObject.Guid]);
            }

            Visual3D visual3D = GetVisual3D<SAMObject>(sAMObject.Guid);
            if (visual3D == null)
            {
                return false;
            }

            Rect3D rect3D = visual3D.Bounds();
            if (rect3D == Rect3D.Empty)
            {
                return false;
            }

            helixViewport3D.ZoomExtents(rect3D);
            return true;
        }

        public bool Zoom<T>(IEnumerable<T> sAMObjects) where T : SAMObject
        {
            if (sAMObjects == null)
            {
                return false;
            }

            if (Active2D)
            {
                List<Guid> guids = new List<T>(sAMObjects).FindAll(x => x != null && floorPlan2DControl.Contains(x.Guid)).ConvertAll(x => x.Guid);
                if (guids.Count == 0)
                {
                    return false;
                }

                floorPlan2DControl.Zoom(guids);
                return true;
            }

            if (ActiveSharpDX3D)
            {
                return sharpDXViewportControl.Zoom(new List<T>(sAMObjects).FindAll(x => x != null).ConvertAll(x => x.Guid));
            }

            List<Rect3D> rect3Ds = new List<Rect3D>();
            foreach (SAMObject sAMObject in sAMObjects)
            {
                Visual3D visual3D = GetVisual3D<SAMObject>(sAMObject.Guid);
                if (visual3D == null)
                {
                    continue;
                }

                Rect3D rect3D = visual3D.Bounds();
                if (rect3D == Rect3D.Empty)
                {
                    continue;
                }

                rect3Ds.Add(rect3D);
            }


            Rect3D rect3D_Union = Rect3D.Empty;
            foreach (Rect3D rect3D_Temp in rect3Ds)
            {
                rect3D_Union.Union(rect3D_Temp);
            }

            if (rect3D_Union == Rect3D.Empty)
            {
                return false;
            }

            helixViewport3D.ZoomExtents(rect3D_Union);
            return true;
        }

        private Camera GetCamera()
        {
            if (Active2D)
            {
                // Floor-plan pan/zoom expressed as a camera, so view settings persist for the 2D
                // canvas (PR #30 review). Null (empty/never-shown view) falls back to the Helix
                // camera below - the same value the old orthographic path would have reported.
                Camera camera_FloorPlan = floorPlan2DControl.GetCamera();
                if (camera_FloorPlan != null)
                {
                    return camera_FloorPlan;
                }
            }

            if (ActiveSharpDX3D)
            {
                return sharpDXViewportControl.GetCamera();
            }

            ProjectionCamera projectionCamera = helixViewport3D.Camera;
            if (projectionCamera == null)
            {
                return null;
            }

            return new Camera(projectionCamera.Position.ToSAM(), projectionCamera.LookDirection.ToSAM(), projectionCamera.UpDirection.ToSAM());
        }

        private void helixViewport3D_CameraChanged(object sender, RoutedEventArgs e)
        {
            UpdateClipPlanes2D();
        }

        /// <summary>
        /// In orthographic floor-plan mode the camera moves on zoom/pan while the near/far planes are
        /// otherwise set once (UpdateMode), so the section geometry can leave the fixed depth slab and
        /// vanish (issue #13). Re-fit the planes around the cached scene depth-range on every camera
        /// change so the geometry stays inside the slab across the whole zoom range.
        /// </summary>
        private void UpdateClipPlanes2D()
        {
            // Only the Helix orthographic 2D path needs this; the 2D renderer has no clip planes
            if (updatingClipPlanes || mode != Mode.TwoDimensional || Active2D)
            {
                return;
            }

            if (double.IsNaN(sceneZMin) || double.IsNaN(sceneZMax))
            {
                return;
            }

            ProjectionCamera projectionCamera = helixViewport3D.Camera;
            if (projectionCamera == null)
            {
                return;
            }

            Vector3D lookDirection = projectionCamera.LookDirection;
            if (lookDirection.Length <= 0)
            {
                return;
            }
            lookDirection.Normalize();

            Point3D position = projectionCamera.Position;

            // Signed depth (distance along the look direction) of the two scene Z extremes
            double depth_1 = Vector3D.DotProduct(new Point3D(position.X, position.Y, sceneZMin) - position, lookDirection);
            double depth_2 = Vector3D.DotProduct(new Point3D(position.X, position.Y, sceneZMax) - position, lookDirection);

            double near = System.Math.Min(depth_1, depth_2) - clipPlaneMargin;
            double far = System.Math.Max(depth_1, depth_2) + clipPlaneMargin;

            // Avoid re-entrancy (assigning the planes raises CameraChanged again) and pointless churn
            if (System.Math.Abs(projectionCamera.NearPlaneDistance - near) < 1e-6 && System.Math.Abs(projectionCamera.FarPlaneDistance - far) < 1e-6)
            {
                return;
            }

            updatingClipPlanes = true;
            try
            {
                projectionCamera.NearPlaneDistance = near;
                projectionCamera.FarPlaneDistance = far;
            }
            finally
            {
                updatingClipPlanes = false;
            }
        }

        private void helixViewport3D_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) ||
                Keyboard.IsKeyDown(Key.RightCtrl) ||
                Keyboard.IsKeyDown(Key.LeftAlt) ||
                Keyboard.IsKeyDown(Key.RightAlt) ||
                Keyboard.IsKeyDown(Key.LeftShift) ||
                Keyboard.IsKeyDown(Key.RightShift))
            {
                e.Handled = true;
                return;
            }

            helixViewport3D.ContextMenu = new ContextMenu();

            MenuItem menuItem = new MenuItem();
            menuItem.Name = "MenuItem_ZoomExtents";
            menuItem.Header = "Zoom Extents";
            menuItem.Click += MenuItem_ZoomExtents_Click;
            helixViewport3D.ContextMenu.Items.Add(menuItem);

            AddZoomSelectedMenuItem(helixViewport3D.ContextMenu);

            ObjectContextMenuOpening?.Invoke(this, new ObjectContextMenuOpeningEventArgs(helixViewport3D.ContextMenu, e, actionManager.SelectedVisual3Ds()?.FindAll(x => x is ModelVisual3D)?.ConvertAll(x => x as ModelVisual3D)));
        }

        private void helixViewport3D_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                bool cancelled = actionManager.Cancel<SelectAction>();
                if (cancelled)
                {
                    ObjectSelectionChanged?.Invoke(this, new ObjectSelectionChangedEventArgs());
                }
            }
        }

        private void helixViewport3D_Loaded(object sender, RoutedEventArgs e)
        {
            GeometryObjectModel geometryObjectModel = uIGeometryObjectModel?.JSAMObject;

            if (geometryObjectModel != null && geometryObjectModel.TryGetValue(GeometryObjectModelParameter.ViewSettings, out ViewSettings viewSettings) && viewSettings != null)
            {
                Camera camera = viewSettings.Camera;
                if (camera != null)
                {
                    helixViewport3D.Camera.Position = camera.Location.ToMedia3D();
                    helixViewport3D.Camera.LookDirection = camera.LookDirection.ToMedia3D();

                    if(camera.UpDirection is not null)
                    {
                        helixViewport3D.Camera.UpDirection = camera.UpDirection.ToMedia3D(); //NEW 2026.03.16
                    }
                }
                else
                {
                    helixViewport3D.ZoomExtents();
                }
            }

            helixViewport3D.Loaded -= helixViewport3D_Loaded;
        }

        private void helixViewport3D_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point point_Current = e.GetPosition(helixViewport3D);

                RayMeshGeometry3DHitTestResult rayMeshGeometry3DHitTestResult = Core.UI.WPF.Query.RayMeshGeometry3DHitTestResult(helixViewport3D, point_Current, out ModelVisual3D modelVisual3D);
                if (rayMeshGeometry3DHitTestResult != null && modelVisual3D != null)
                {
                    ObjectDoubleClicked?.Invoke(this, new ObjectDoubleClickedEventArgs(e, modelVisual3D));
                }
            }
        }

        private void helixViewport3D_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point point_Current = e.GetPosition(helixViewport3D);

            RayMeshGeometry3DHitTestResult rayMeshGeometry3DHitTestResult = Core.UI.WPF.Query.RayMeshGeometry3DHitTestResult(helixViewport3D, point_Current, out ModelVisual3D modelVisual3D);

            bool cancelled = false;
            if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl))
            {
                cancelled = actionManager.Cancel<SelectAction>();
            }

            if (rayMeshGeometry3DHitTestResult == null && modelVisual3D == null)
            {
                e.Handled = true;

                if (cancelled)
                {
                    ObjectSelectionChanged?.Invoke(this, new ObjectSelectionChangedEventArgs());
                }

                return;
            }

            SelectAction selectAction = actionManager.GetAction<SelectAction>();
            if (selectAction == null)
            {
                selectAction = new SelectAction();
            }

            if (selectAction != null && selectAction.Contains(modelVisual3D))
            {
                selectAction.Remove(modelVisual3D);
            }
            else
            {
                selectAction.Add(modelVisual3D);
            }

            actionManager.Apply(selectAction);
            ObjectSelectionChanged?.Invoke(this, new ObjectSelectionChangedEventArgs());

        }

        private void helixViewport3D_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            // Leading-edge throttle: process at most one hit-test per interval. unchecked handles
            // Environment.TickCount wraparound. Intermediate moves keep the current highlight (issue #16).
            int tick = Environment.TickCount;
            if (unchecked(tick - lastHoverHitTestTick) < hoverHitTestThrottleMilliseconds)
            {
                return;
            }
            lastHoverHitTestTick = tick;

            actionManager.Cancel<HighlightAction>();

            Point point = e.GetPosition(helixViewport3D);

            RayMeshGeometry3DHitTestResult rayMeshGeometry3DHitTestResult;
            ModelVisual3D modelVisual3D;

            // Only slow hit-tests (>= 25 ms) are logged to avoid flooding the log on mouse move
            using (PerformanceLog.Measure("ViewportControl.HoverHitTest", mode.ToString(), 25))
            {
                rayMeshGeometry3DHitTestResult = Core.UI.WPF.Query.RayMeshGeometry3DHitTestResult(helixViewport3D, point, out modelVisual3D);
            }
            if (rayMeshGeometry3DHitTestResult != null && modelVisual3D != null)
            {
                actionManager.Apply(new HighlightAction(modelVisual3D));

                ObjectHoovered?.Invoke(this, new ObjectHooveredEventArgs(e, modelVisual3D));
            }
        }

        private void Load(GeometryObjectModel geometryObjectModel)
        {
            helixViewport3D.Lights.Children.Clear();
            helixViewport3D.Lights.Children.Add(new AmbientLight());

            bool cancelled = actionManager.Cancel();
            if (cancelled)
            {
                ObjectSelectionChanged?.Invoke(this, new ObjectSelectionChangedEventArgs());
            }

            int count = Core.UI.WPF.Query.Visual3Ds<ModelVisual3D>(helixViewport3D.Children, new Type[] { typeof(GeometryObjectModel) }).Count;
            if (count > 0)
            {
                Core.UI.WPF.Modify.Clear<ModelVisual3D>(helixViewport3D.Children, new Type[] { typeof(GeometryObjectModel) });
            }

            // The scene is being rebuilt - drop the stale guid index (issue #16); the 3D branch repopulates it.
            dictionary_Visual3D.Clear();

            // Flag-gated 2D floor plan path: render via FloorPlan2DControl, keep the Helix scene empty
            floorPlan2DControl.Load(Active2D ? geometryObjectModel : null);

            // Flag-gated SharpDX 3D path (issue #18 Phase B): render via SharpDXViewportControl,
            // keep the Helix scene empty. Timed for the direct comparison with ToMedia3D below.
            if (sharpDXViewportControl != null)
            {
                GeometryObjectModel geometryObjectModel_SharpDX = ActiveSharpDX3D ? geometryObjectModel : null;

                Camera camera = null;
                if (geometryObjectModel_SharpDX != null && geometryObjectModel_SharpDX.TryGetValue(GeometryObjectModelParameter.ViewSettings, out ViewSettings viewSettings_SharpDX) && viewSettings_SharpDX != null)
                {
                    camera = viewSettings_SharpDX.Camera;
                }

                using (geometryObjectModel_SharpDX == null ? null : PerformanceLog.Measure("ViewportControl.ToElement3D", mode.ToString()))
                {
                    sharpDXViewportControl.Load(geometryObjectModel_SharpDX, camera);
                }
            }

            if (geometryObjectModel == null)
            {
                return;
            }

            if (!Active2D && !ActiveSharpDX3D)
            {
                ModelVisual3D modelVisual3D;
                using (PerformanceLog.Measure("ViewportControl.ToMedia3D", mode.ToString()))
                {
                    modelVisual3D = Convert.ToMedia3D(geometryObjectModel);
                }

                if (modelVisual3D != null)
                {
                    helixViewport3D.Children.Add(modelVisual3D);
                    BuildVisual3DIndex(modelVisual3D);
                }

                // Cache the scene depth extent for 2D clip-plane tracking (issue #13)
                Rect3D bounds = helixViewport3D.Children.Bounds();
                if (bounds != Rect3D.Empty)
                {
                    sceneZMin = bounds.Z;
                    sceneZMax = bounds.Z + bounds.SizeZ;
                }
                else
                {
                    sceneZMin = double.NaN;
                    sceneZMax = double.NaN;
                }

                if (count == 0)
                {
                    helixViewport3D.ZoomExtents();
                }
            }

            RefreshLegend(geometryObjectModel);
        }

        // Walks the freshly built 3D scene and maps each object's guid to its visual. Pre-order +
        // first-wins, so the outermost per-object container (Tag = space/panel) is the indexed visual.
        // Resolves the object from the visual's own attached IJSAMObject first, then its Content - mirroring
        // Core.UI.WPF.Query.Visual3D - so the index finds everything the old O(N) walk would (issue #16).
        private void BuildVisual3DIndex(Visual3D visual3D)
        {
            if (!(visual3D is ModelVisual3D modelVisual3D))
            {
                return;
            }

            SAMObject sAMObject = Core.UI.WPF.Query.JSAMObject<SAMObject>(modelVisual3D);
            if (sAMObject == null && modelVisual3D.Content != null)
            {
                sAMObject = Core.UI.WPF.Query.JSAMObject<SAMObject>(modelVisual3D.Content);
            }

            if (sAMObject != null && !dictionary_Visual3D.ContainsKey(sAMObject.Guid))
            {
                dictionary_Visual3D[sAMObject.Guid] = modelVisual3D;
            }

            foreach (Visual3D visual3D_Child in modelVisual3D.Children)
            {
                BuildVisual3DIndex(visual3D_Child);
            }
        }

        private void RefreshLegend(GeometryObjectModel geometryObjectModel)
        {
            legendControl.Visibility = Visibility.Hidden;
            if (geometryObjectModel != null && geometryObjectModel.TryGetValue(GeometryObjectModelParameter.ViewSettings, out ViewSettings viewSettings))
            {
                Legend legend = viewSettings.Legend;
                List<LegendItem> legendItems = legend?.LegendItems;

                if (legend != null && legend.Visible && legendItems != null && legendItems.Count != 0)
                {
                    legendControl.Visibility = Visibility.Visible;
                    legendControl.Legend = legend;
                }
            }
        }

        private void MenuItem_Orthographic_Click(object sender, RoutedEventArgs e)
        {
            if (ActiveSharpDX3D)
            {
                sharpDXViewportControl.ToggleProjection();
            }
        }

        /// <summary>
        /// Zooms to the extents of the active view (2D canvas, SharpDX 3D or Helix 3D). Public so the
        /// window can bind it to a shortcut (Rhino-style "ZE"); also used by the context-menu handler.
        /// </summary>
        public void ZoomExtents()
        {
            if (Active2D)
            {
                floorPlan2DControl.ZoomExtents();
                return;
            }

            if (ActiveSharpDX3D)
            {
                sharpDXViewportControl.ZoomExtents();
                return;
            }

            helixViewport3D.ZoomExtents();
        }

        private void MenuItem_ZoomExtents_Click(object sender, RoutedEventArgs e)
        {
            ZoomExtents();
        }

        // Offer "Zoom Selected" only when something is selected (issue #13). Works in both the Helix
        // and 2D paths - SelectedSAMObjects/Zoom already branch on Active2D internally.
        private void AddZoomSelectedMenuItem(ContextMenu contextMenu)
        {
            if (contextMenu == null)
            {
                return;
            }

            List<SAMObject> sAMObjects = SelectedSAMObjects<SAMObject>();
            if (sAMObjects == null || sAMObjects.Count == 0)
            {
                return;
            }

            MenuItem menuItem = new MenuItem();
            menuItem.Name = "MenuItem_ZoomSelected";
            menuItem.Header = "Zoom Selected";
            menuItem.Click += MenuItem_ZoomSelected_Click;
            contextMenu.Items.Add(menuItem);
        }

        private void MenuItem_ZoomSelected_Click(object sender, RoutedEventArgs e)
        {
            List<SAMObject> sAMObjects = SelectedSAMObjects<SAMObject>();
            if (sAMObjects == null || sAMObjects.Count == 0)
            {
                return;
            }

            Zoom(sAMObjects);
        }

        private void RectangularSelector_Selected(object sender, EventArgs e)
        {
            actionManager.Cancel<HighlightAction>();

            if (rectangularSelector == null)
            {
                return;
            }

            Rect rect = rectangularSelector.Rect;
            if (rect == null || rect == Rect.Empty)
            {
                return;
            }

            SelectionType selectionType = rectangularSelector.SelectionType;
            if (selectionType == SelectionType.Undefined)
            {
                return;
            }

            Select(rect, selectionType);

            //List<Spatial.Point3D> point3Ds = new List<Spatial.Point3D>();

            //foreach (Point2D point2D in point2Ds)
            //{
            //    Point point = new Point(point2D.X, point2D.Y);

            //    //point = helixViewport3D.Viewport.PointFromScreen(point);

            //    HelixToolkit.Wpf.Viewport3DHelper.Point2DtoPoint3D(helixViewport3D.Viewport, point, out Point3D point3D_Near, out Point3D point3D_Far);

            //    Spatial.Point3D point3D_Near_SAM = point3D_Near.ToSAM();
            //    point3D_Near_SAM = new Spatial.Point3D(point3D_Near_SAM.X, point3D_Near.Y, 5);

            //    Spatial.Point3D point3D_Far_SAM = point3D_Far.ToSAM();
            //    point3D_Far_SAM = new Spatial.Point3D(point3D_Far_SAM.X, point3D_Far.Y, -10);

            //    point3Ds.Add(point3D_Near_SAM);
            //    point3Ds.Add(point3D_Far_SAM);
            //}

            //Spatial.BoundingBox3D boundingBox3D = new Spatial.BoundingBox3D(point3Ds);

            //List<Spatial.ISAMGeometry3DObject> sAMGeometry3DObjects = null;
            //switch (selectionType)
            //{
            //    case SelectionType.Inside:
            //        sAMGeometry3DObjects = uIGeometryObjectModel?.JSAMObject?.GetSAMGeometryObjects<Spatial.ISAMGeometry3DObject>(x => x is Spatial.ISAMGeometry3DObject && Spatial.Query.Inside(boundingBox3D, x, true));
            //        break;

            //    case SelectionType.InsideOrIntersect:
            //        sAMGeometry3DObjects = uIGeometryObjectModel?.JSAMObject?.GetSAMGeometryObjects<Spatial.ISAMGeometry3DObject>(x => x is Spatial.ISAMGeometry3DObject && Spatial.Query.InRange(boundingBox3D, x));
            //        break;
            //}

            //if (sAMGeometry3DObjects == null)
            //{
            //    return;
            //}

            //Select(sAMGeometry3DObjects.ConvertAll(x => x as ITaggable).FindAll(x => x != null).ConvertAll(x => x?.Tag?.Value as SAMObject));

        }

        private void RectangularSelector_Selecting(object sender, EventArgs e)
        {

        }

        private void Select(Rect rect, SelectionType selectionType)
        {
            actionManager.Cancel<HighlightAction>();

            if (rect == null || rect == Rect.Empty)
            {
                return;
            }

            if (selectionType == SelectionType.Undefined)
            {
                return;
            }

            if (Active2D)
            {
                floorPlan2DControl.SelectByScreenRect(rect, selectionType);
                ObjectSelectionChanged?.Invoke(this, new ObjectSelectionChangedEventArgs());
                return;
            }

            if (ActiveSharpDX3D)
            {
                // The RectangularSelector rect is in grid coordinates, which the SharpDX viewport
                // fills - same coordinate space (issue #32 Phase C)
                sharpDXViewportControl.SelectByScreenRect(rect, selectionType);
                ObjectSelectionChanged?.Invoke(this, new ObjectSelectionChangedEventArgs());
                return;
            }

            List<SAMObject> sAMObjects = new List<SAMObject>();
            IEnumerable<HelixToolkit.Wpf.Viewport3DHelper.RectangleHitResult> rectangleHitResults = HelixToolkit.Wpf.Viewport3DHelper.FindHits(helixViewport3D.Viewport, rect, selectionType == SelectionType.Inside ? HelixToolkit.Wpf.SelectionHitMode.Inside : HelixToolkit.Wpf.SelectionHitMode.Touch);
            if (rectangleHitResults != null)
            {
                foreach (HelixToolkit.Wpf.Viewport3DHelper.RectangleHitResult rectangleHitResult in rectangleHitResults)
                {
                    IJSAMObject jSAMObject_Model = Core.UI.WPF.Query.JSAMObject<IJSAMObject>(rectangleHitResult.Model);
                    IJSAMObject jSAMObject_Visual = Core.UI.WPF.Query.JSAMObject<IJSAMObject>(rectangleHitResult.Visual);

                    if (jSAMObject_Model is Text3DObject)
                    {
                        continue;
                    }

                    SAMObject sAMObject = (jSAMObject_Visual as ITaggable)?.Tag?.Value as SAMObject;
                    if (sAMObject != null)
                    {
                        Visual3D visual3D = GetVisual3D<SAMObject>(sAMObject.Guid);
                        if (jSAMObject_Model is ISAMGeometry3DObject && jSAMObject_Visual is IEnumerable<ISAMGeometry3DObject>)
                        {
                            ISAMGeometry3DObject sAMGeometry3DObject = (ISAMGeometry3DObject)jSAMObject_Model;
                            IEnumerable<ISAMGeometry3DObject> sAMGeometry3DObjects = (IEnumerable<ISAMGeometry3DObject>)jSAMObject_Visual;
                            if (!sAMGeometry3DObjects.Contains(sAMGeometry3DObject))
                            {
                                continue;
                            }
                        }

                        sAMObjects.Add(sAMObject);
                    }

                }
            }

            Select(sAMObjects);
        }
        
        private void SetCamera(Camera camera)
        {
            if (camera == null)
            {
                return;
            }

            if (Active2D)
            {
                floorPlan2DControl.SetCamera(camera);
                return;
            }

            if (ActiveSharpDX3D)
            {
                sharpDXViewportControl.SetCamera(camera);
                return;
            }

            ProjectionCamera projectionCamera = helixViewport3D.Camera;
            if (projectionCamera == null)
            {
                return;
            }

            Spatial.Vector3D lookDirection = camera.LookDirection;
            if (lookDirection.AlmostEqual(-Spatial.Vector3D.WorldZ, Tolerance.MacroDistance))
            {
                lookDirection = new Spatial.Vector3D(0, 0.0001, -0.9999);
            }
            else if (lookDirection.AlmostEqual(Spatial.Vector3D.WorldZ, Tolerance.MacroDistance))
            {
                lookDirection = new Spatial.Vector3D(0, 0.0001, 0.9999);
            }

            projectionCamera.LookDirection = lookDirection.ToMedia3D();
            projectionCamera.Position = camera.Location.ToMedia3D();

            if(camera.UpDirection is not null)
            {
                projectionCamera.UpDirection = camera.UpDirection.ToMedia3D(); //NEW 2026.03.16
            }
        }

        private void UIGeometryObjectModel_Closed(object sender, EventArgs e)
        {
            Load(uIGeometryObjectModel?.JSAMObject);
        }

        private void UIGeometryObjectModel_Modified(object sender, EventArgs e)
        {
            Load(uIGeometryObjectModel?.JSAMObject);
        }

        private void UIGeometryObjectModel_Opened(object sender, EventArgs e)
        {
            Load(uIGeometryObjectModel?.JSAMObject);
        }

        private void UpdateMode()
        {
            if(mode == Mode.ThreeDimensional)
            {
                helixViewport3D.Camera = new PerspectiveCamera();

                helixViewport3D.Orthographic = false;
                helixViewport3D.ShowViewCube = true;
                helixViewport3D.ShowCoordinateSystem = true;
                helixViewport3D.CameraMode = HelixToolkit.Wpf.CameraMode.Inspect;
                helixViewport3D.ZoomAroundMouseDownPoint = true;
                helixViewport3D.IsPanEnabled = true;
                helixViewport3D.IsRotationEnabled = true;
                //helixViewport3D.ShowCameraInfo = true;

            }
            else
            {
                helixViewport3D.Orthographic = true;
                helixViewport3D.ShowViewCube = false;
                helixViewport3D.ShowCoordinateSystem = false;
                helixViewport3D.Camera.LookDirection = new Vector3D(0, 0.0001, -0.9999);
                helixViewport3D.Camera.NearPlaneDistance = -1000;
                helixViewport3D.Camera.FarPlaneDistance = 1000;
                // Zoom toward the cursor in floor-plan mode (issue #13). Previously disabled because the
                // camera shift it causes pushed geometry out of the fixed near/far slab; that slab now
                // tracks the camera (UpdateClipPlanes2D), so zoom-to-cursor is safe.
                helixViewport3D.ZoomAroundMouseDownPoint = true;
                helixViewport3D.IsPanEnabled = true;
                helixViewport3D.IsRotationEnabled = false;
                //helixViewport3D.ShowCameraInfo = true;
            }

            helixViewport3D.ZoomExtents();

            if (floorPlan2DEnabled || sharpDXViewportControl != null)
            {
                bool active2D = Active2D;
                bool activeSharpDX3D = ActiveSharpDX3D;

                floorPlan2DControl.Visibility = active2D ? Visibility.Visible : Visibility.Collapsed;
                if (sharpDXViewportControl != null)
                {
                    sharpDXViewportControl.Visibility = activeSharpDX3D ? Visibility.Visible : Visibility.Collapsed;
                }
                helixViewport3D.Visibility = active2D || activeSharpDX3D ? Visibility.Collapsed : Visibility.Visible;

                // Mode is assigned after UIGeometryObjectModel (see AnalyticalWindow.UpdateTabItem) - reroute the loaded model to the renderer that just became active
                Load(uIGeometryObjectModel?.JSAMObject);
            }
        }
    }
}
