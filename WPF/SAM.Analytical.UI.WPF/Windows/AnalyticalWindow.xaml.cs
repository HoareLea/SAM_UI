// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using HoneybeeSchema;
using Microsoft.Win32;
using SAM.Core;
using SAM.Core.UI;
using SAM.Core.UI.WPF;
using SAM.Geometry.Object;
using SAM.Geometry.UI;
using SAM.Geometry.UI.WPF;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Media3D;

namespace SAM.Analytical.UI.WPF.Windows
{
    /// <summary>
    /// Interaction logic for AnalyticalWindow.xaml
    /// </summary>
    public partial class AnalyticalWindow : System.Windows.Window
    {
        private const int chordTimeoutMilliseconds = 1000;
        private static string titlePrefix = "SAM Analytical";

        // View guids whose geometry regeneration was deferred because the tab was not active
        // when a modification arrived. Regenerated lazily on activation (see RegenerateIfDirty).
        private readonly HashSet<Guid> dirtyViewGuids = new HashSet<Guid>();

        private DoubleRangeWindow? doubleRangeWindow = null;
        // Two-letter chord state (Rhino-style "ZE"/"ZS"). A prefix key (Z) is remembered for a short
        // window; the next key completes the chord. Z has no single-key action of its own, so there is
        // nothing to defer - a lone Z, or Z followed by a non-chord key, simply does nothing for the Z.
        private Key? pendingChordKey;

        private int pendingChordTick;
        private ProgressBarWindowManager progressBarWindowManager = new();

        private UIAnalyticalModel? uIAnalyticalModel = null;
        private WindowHandle? windowHandle = null;

        /// <summary>
        /// The Approved Document O run in progress in this window: the preparation, and - once a TAS workflow
        /// has completed over it - the model that workflow returned and the results it wrote.
        /// <para>
        /// Session state, held here and never written into the model. It is the object that keeps the TM59
        /// assessment from being handed the preparation model or the currently loaded one, and it is dropped by
        /// <see cref="UIAnalyticalModel_Modified"/> whenever something other than a Part O command replaces the
        /// model. Readonly, so the invalidation wiring above can never be left pointing at a discarded
        /// instance.
        /// </para>
        /// </summary>
        private readonly PartORun partORun = new();
        
        public AnalyticalWindow()
        {
            InitializeWindow();
        }

        public AnalyticalWindow(StartupOptions startupOptions)
        {
            InitializeWindow();

            if (startupOptions != null)
            {
                string path = startupOptions.Path;
                if(!string.IsNullOrWhiteSpace(path) && System.IO.File.Exists(path))
                {
                    bool opened = Open(path);
                    if(startupOptions.TemporaryFile)
                    {
                        if(opened && uIAnalyticalModel != null)
                        {
                            uIAnalyticalModel.Path = null;
                        }

                        System.IO.File.Delete(path);
                    }
                }
            }
        }

        public void SaveAs()
        {
            if (uIAnalyticalModel == null)
            {
                return;
            }

            AnalyticalModel analyticalModel = uIAnalyticalModel.JSAMObject;

            SetUIGeometrySettings(tabControl, analyticalModel);

            uIAnalyticalModel.SaveAs();
        }

        private static bool IsDescendantOf(DependencyObject node, DependencyObject ancestor)
        {
            while (node != null)
            {
                if (node == ancestor)
                {
                    return true;
                }

                node = node is System.Windows.Media.Visual || node is System.Windows.Media.Media3D.Visual3D
                    ? System.Windows.Media.VisualTreeHelper.GetParent(node)
                    : System.Windows.LogicalTreeHelper.GetParent(node);
            }

            return false;
        }

        private void AnalyticalModelControl_SelectionRequested(object sender, SelectionRequestedEventArgs e)
        {
            List<SAMObject> sAMObjects = e.SAMObjects;
            if (sAMObjects == null || sAMObjects.Count == 0)
            {
                return;
            }

            ViewportControl viewportControl = GetActiveViewportControl();
            if (viewportControl == null)
            {
                return;
            }

            AdjacencyCluster? adjacencyCluster = uIAnalyticalModel?.JSAMObject?.AdjacencyCluster;
            if (adjacencyCluster != null)
            {
                for (int i = sAMObjects.Count - 1; i >= 0; i--)
                {
                    Zone? zone = sAMObjects[i] as Zone;
                    if (zone == null)
                    {
                        continue;
                    }

                    sAMObjects.RemoveAt(i);

                    List<Space> spaces = adjacencyCluster.GetSpaces(zone);
                    if (spaces == null || spaces.Count == 0)
                    {
                        return;
                    }

                    sAMObjects.AddRange(spaces);
                }
            }

            viewportControl.Select(sAMObjects);
        }

        private void AnalyticalModelControl_TreeViewItemDropped(object sender, TreeViewItemDroppedEventArgs e)
        {
            SetUIGeometrySettings(tabControl, uIAnalyticalModel.JSAMObject);
        }

        private void AnalyticalModelControl_ZoomRequested(object sender, ZoomRequestedEventArgs e)
        {
            List<SAMObject> sAMObjects = e.SAMObjects;
            if (sAMObjects == null || sAMObjects.Count == 0)
            {
                return;
            }

            ViewportControl viewportControl = GetActiveViewportControl();
            if (viewportControl == null)
            {
                return;
            }

            AdjacencyCluster adjacencyCluster = uIAnalyticalModel?.JSAMObject?.AdjacencyCluster;
            if (adjacencyCluster != null)
            {
                for (int i = sAMObjects.Count - 1; i >= 0; i--)
                {
                    Zone zone = sAMObjects[i] as Zone;
                    if (zone == null)
                    {
                        continue;
                    }

                    sAMObjects.RemoveAt(i);

                    List<Space> spaces = adjacencyCluster.GetSpaces(zone);
                    if (spaces == null || spaces.Count == 0)
                    {
                        return;
                    }

                    sAMObjects.AddRange(spaces);
                }
            }

            viewportControl.Zoom(sAMObjects);
        }

        private void AssignMechanicalSystems(IEnumerable<Space>? spaces = null)
        {
            Modify.AssignMechanicalSystems(uIAnalyticalModel, spaces);
        }

        private void CopyViewSettings(TabItem tabItem)
        {
            if (tabItem == null)
            {
                return;
            }

            ViewportControl viewportControl = tabItem.Content as ViewportControl;
            if (viewportControl == null)
            {
                return;
            }

            //SetActiveGuid();
            SetUIGeometrySettings(tabControl, uIAnalyticalModel.JSAMObject);
            Modify.CopyViewSettings(uIAnalyticalModel, viewportControl.Guid);
        }

        private void CopyViewSettingsCamera(TabItem tabItem)
        {
            if (tabItem == null)
            {
                return;
            }

            ViewportControl viewportControl = tabItem.Content as ViewportControl;
            if (viewportControl == null)
            {
                return;
            }

            //SetActiveGuid();
            SetUIGeometrySettings(tabControl, uIAnalyticalModel.JSAMObject);
            Modify.CopyViewSettingsCamera(uIAnalyticalModel, viewportControl.Guid);
        }

        private void Delete()
        {
            ViewportControl viewportControl = GetActiveViewportControl();
            if (viewportControl == null)
            {
                return;
            }

            List<Panel> panels = viewportControl.SelectedSAMObjects<Panel>();
            if(panels != null)
            {
                Modify.RemovePanels(uIAnalyticalModel, panels);
            }

            List<Aperture> apertures = viewportControl.SelectedSAMObjects<Aperture>();
            if (apertures != null)
            {
                Modify.RemoveApertures(uIAnalyticalModel, apertures);
            }
        }

        private void DoubleRangeWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            doubleRangeWindow = null;
        }

        private void DoubleRangeWindow_RangeChanged(object sender, RangeChangedEventArgs<double> e)
        {
            Range<double> range = e.Range;
            if (range == null)
            {
                return;
            }

            if (doubleRangeWindow is null)
            {
                return;
            }

            ViewportControl viewportControl = GetActiveViewportControl();
            if (viewportControl == null)
            {
                return;
            }

            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if (analyticalModel == null)
            {
                return;
            }

            if (!analyticalModel.TryGetValue(AnalyticalModelParameter.UIGeometrySettings, out UIGeometrySettings uIGeometrySettings) || uIGeometrySettings == null)
            {
                return;
            }

            ThreeDimensionalViewSettings threeDimensionalViewSettings = uIGeometrySettings.GetViewSettings(viewportControl.Guid) as ThreeDimensionalViewSettings;
            if (threeDimensionalViewSettings == null)
            {
                return;
            }

            List<Geometry.Spatial.Plane> planes = new List<Geometry.Spatial.Plane>();

            if (doubleRangeWindow.Min != range.Min)
            {
                Geometry.Spatial.Plane plane_Min = Geometry.Spatial.Plane.WorldXY.GetMoved(new Geometry.Spatial.Vector3D(0, 0, range.Min)) as Geometry.Spatial.Plane;

                planes.Add(plane_Min);
            }

            if (doubleRangeWindow.Max != range.Max)
            {
                Geometry.Spatial.Plane plane_Max = Geometry.Spatial.Plane.WorldXY.GetMoved(new Geometry.Spatial.Vector3D(0, 0, range.Max)) as Geometry.Spatial.Plane;
                plane_Max.Reverse();

                planes.Add(plane_Max);
            }

            if (planes.Count == 0)
            {
                planes = null;
            }

            threeDimensionalViewSettings.Planes = planes;

            uIGeometrySettings.AddViewSettings(threeDimensionalViewSettings);

            analyticalModel.SetValue(AnalyticalModelParameter.UIGeometrySettings, uIGeometrySettings);

            uIAnalyticalModel.SetJSAMObject(analyticalModel, new ViewSettingsModification(threeDimensionalViewSettings, true));
        }

        private void DuplicateViewSettings(TabItem tabItem)
        {
            if (tabItem == null)
            {
                return;
            }

            ViewportControl viewportControl = tabItem.Content as ViewportControl;
            if (viewportControl == null)
            {
                return;
            }

            //SetActiveGuid();
            SetUIGeometrySettings(tabControl, uIAnalyticalModel.JSAMObject);
            Modify.DuplicateViewSettings(uIAnalyticalModel, viewportControl.Guid);
        }
        
        private void EditLegend()
        {
            Guid guid = GetActiveGuid();
            if (guid == Guid.Empty)
            {
                return;
            }

            //SetActiveGuid();
            Modify.EditLegend(uIAnalyticalModel, guid);
        }

        private void EditMechanicalSystems(IEnumerable<Space> spaces, IEnumerable<Space> selectedSpaces = null)
        {
            Modify.EditMechanicalSystems(uIAnalyticalModel, spaces, MechanicalSystemCategory.Ventilation, selectedSpaces);
        }

        private void EditViewSettings()
        {
            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if (analyticalModel == null)
            {
                return;
            }

            TabItem tabItem = tabControl.SelectedItem as TabItem;
            if (tabItem == null)
            {
                return;
            }

            EditViewSettings(tabItem);
        }

        private void EditViewSettings(TabItem tabItem)
        {
            if (tabItem == null)
            {
                return;
            }

            ViewportControl viewportControl = tabItem.Content as ViewportControl;
            if (viewportControl == null)
            {
                return;
            }

            //SetActiveGuid();
            SetUIGeometrySettings(tabControl, uIAnalyticalModel.JSAMObject);
            Modify.EditViewSettings(uIAnalyticalModel, viewportControl.Guid);
        }

        private void EditZones(IEnumerable<Space> spaces, IEnumerable<Space> selectedSpaces = null)
        {
            string zoneCategory = null;

            ViewSettings viewSettings = Query.ViewSettings<ViewSettings>(uIAnalyticalModel, GetActiveGuid());
            if (viewSettings != null)
            {
                ZoneAppearanceSettings zoneAppearanceSettings = viewSettings.GetValueAppearanceSettings<SpaceAppearanceSettings>()?.FirstOrDefault()?.GetValueAppearanceSettings<ZoneAppearanceSettings>();
                if (zoneAppearanceSettings != null)
                {
                    zoneCategory = zoneAppearanceSettings.ZoneCategory;
                }
            }

            //SetActiveGuid();
            Modify.EditZones(uIAnalyticalModel, spaces, zoneCategory, selectedSpaces);
        }

        private void EnableViewSettings(bool enabled)
        {
            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if (analyticalModel == null)
            {
                return;
            }

            TabItem tabItem = tabControl.SelectedItem as TabItem;
            if (tabItem == null)
            {
                return;
            }

            EnableViewSettings(tabItem, enabled);
        }

        private void EnableViewSettings(TabItem tabItem, bool enabled)
        {
            if (tabItem == null)
            {
                return;
            }

            ViewportControl viewportControl = tabItem.Content as ViewportControl;
            if (viewportControl == null)
            {
                return;
            }

            //SetActiveGuid();
            SetUIGeometrySettings(tabControl, uIAnalyticalModel.JSAMObject);
            Modify.EnableViewSettings(uIAnalyticalModel, new Guid[] { viewportControl.Guid }, enabled);
        }

        private void EnableViewSettings(IEnumerable<TabItem> tabItems, bool enabled)
        {
            if (tabItems == null || tabItems.Count() == 0)
            {
                return;
            }

            //SetActiveGuid();
            SetUIGeometrySettings(tabControl, uIAnalyticalModel.JSAMObject);

            List<Guid> guids = new List<Guid>();
            foreach (TabItem tabItem in tabItems)
            {
                ViewportControl viewportControl = tabItem.Content as ViewportControl;
                if (viewportControl == null)
                {
                    continue;
                }

                guids.Add(viewportControl.Guid);
            }

            Modify.EnableViewSettings(uIAnalyticalModel, guids, enabled);
        }

        private void FilterWindow_FilterAdding(object sender, FilterAddingEventArgs e)
        {
            e.Handled = true;

            Type type = e.Type;
            List<IUIFilter> uIFilters = UI.Query.IUIFilters(type, uIAnalyticalModel?.JSAMObject?.AdjacencyCluster);
            if (uIFilters == null || uIFilters.Count == 0)
            {
                return;
            }

            SAM.Core.UI.WPF.SearchWindow searchWindow = new SAM.Core.UI.WPF.SearchWindow(uIFilters, x => (x as IUIFilter)?.Name)
            {
                Owner = this,
                Title = "Select Filter",
                SelectionMode = System.Windows.Controls.SelectionMode.Single
            };
            if (searchWindow.ShowDialog() != true)
            {
                return;
            }

            e.UIFilter = searchWindow.GetSelectedItems<IUIFilter>().FirstOrDefault();
        }

        private Guid GetActiveGuid()
        {
            ViewportControl viewportControl = GetActiveViewportControl();
            if (viewportControl == null)
            {
                return Guid.Empty;
            }

            return viewportControl.Guid;
        }

        private ViewportControl GetActiveViewportControl()
        {
            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if (analyticalModel == null)
            {
                return null;
            }

            TabItem tabItem = tabControl.SelectedItem as TabItem;
            if (tabItem == null)
            {
                return null;
            }

            return tabItem.Content as ViewportControl;
        }

        private IViewSettings GetActiveViewSettings()
        {
            ViewportControl viewportControl = GetActiveViewportControl();
            if (viewportControl == null)
            {
                return null;
            }

            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if (analyticalModel == null)
            {
                return null;
            }

            if (!analyticalModel.TryGetValue(AnalyticalModelParameter.UIGeometrySettings, out UIGeometrySettings uIGeometrySettings) || uIGeometrySettings == null)
            {
                return null;
            }

            return uIGeometrySettings.GetViewSettings(viewportControl.Guid);
        }

        private void Hide()
        {
            ViewportControl viewportControl = GetActiveViewportControl();
            if (viewportControl == null)
            {
                return;
            }

            List<SAMObject> jSAMObjects = viewportControl.SelectedSAMObjects<SAMObject>();

            SetUIGeometrySettings(tabControl, uIAnalyticalModel.JSAMObject);
            UI.Modify.Hide(uIAnalyticalModel, viewportControl.Guid, jSAMObjects);
        }

        private void Hide(MenuItem menuItem)
        {
            if (menuItem == null)
            {
                return;
            }

            TabItem tabItem = tabControl.SelectedItem as TabItem;
            if (tabItem == null)
            {
                return;
            }

            ViewportControl viewportControl = tabItem.Content as ViewportControl;
            if (viewportControl == null)
            {
                return;
            }

            List<IJSAMObject> jSAMObjects = null;
            if (menuItem.Tag is IJSAMObject)
            {
                jSAMObjects = new List<IJSAMObject>() { (IJSAMObject)menuItem.Tag };
            }
            else if (menuItem.Tag is IEnumerable)
            {
                jSAMObjects = new List<IJSAMObject>();
                foreach (object @object in (IEnumerable)menuItem.Tag)
                {
                    if (@object is IJSAMObject)
                    {
                        jSAMObjects.Add((IJSAMObject)@object);
                    }
                }
            }

            SetUIGeometrySettings(tabControl, uIAnalyticalModel.JSAMObject);
            UI.Modify.Hide(uIAnalyticalModel, viewportControl.Guid, jSAMObjects);
        }

        private void InitializeWindow()
        {
            InitializeComponent();

            Title = titlePrefix;

            windowHandle = new SAM.Core.UI.WPF.WindowHandle(this);

            Icon = SAM.Core.UI.WPF.Convert.ToBitmapSource(Properties.Resources.SAM);

            RibbonButton_OpenAnalyticalModel.LargeImageSource = SAM.Core.UI.WPF.Convert.ToBitmapSource(Properties.Resources.SAM_Open);
            RibbonButton_OpenAnalyticalModel.Click += RibbonButton_OpenAnalyticalModel_Click;

            RibbonButton_NewAnalyticalModel.LargeImageSource = SAM.Core.UI.WPF.Convert.ToBitmapSource(Properties.Resources.SAM_New);
            RibbonButton_NewAnalyticalModel.Click += RibbonButton_NewAnalyticalModel_Click;

            RibbonButton_SaveAnalyticalModel.LargeImageSource = SAM.Core.UI.WPF.Convert.ToBitmapSource(Properties.Resources.SAM_Save);
            RibbonButton_SaveAnalyticalModel.Click += RibbonButton_SaveAnalyticalModel_Click;

            RibbonButton_SaveAsAnalyticalModel.LargeImageSource = SAM.Core.UI.WPF.Convert.ToBitmapSource(Properties.Resources.SAM_SaveAs);
            RibbonButton_SaveAsAnalyticalModel.Click += RibbonButton_SaveAsAnalyticalModel_Click;

            RibbonButton_CloseAnalyticalModel.LargeImageSource = SAM.Core.UI.WPF.Convert.ToBitmapSource(Properties.Resources.SAM_Close);
            RibbonButton_CloseAnalyticalModel.Click += RibbonButton_CloseAnalyticalModel_Click;

            RibbonButton_ImportAnalyticalModel.LargeImageSource = SAM.Core.UI.WPF.Convert.ToBitmapSource(Properties.Resources.SAM_Open);
            RibbonButton_ImportAnalyticalModel.Click += RibbonButton_ImportAnalyticalModel_Click;

            RibbonButton_ExportAnalyticalModel.LargeImageSource = SAM.Core.UI.WPF.Convert.ToBitmapSource(Properties.Resources.SAM_Save);
            RibbonButton_ExportAnalyticalModel.Click += RibbonButton_ExportAnalyticalModel_Click;

            RibbonButton_NewSectionViews.LargeImageSource = SAM.Core.UI.WPF.Convert.ToBitmapSource(Properties.Resources.SAM_Section);
            RibbonButton_NewSectionViews.Click += RibbonButton_NewSectionViews_Click;

            RibbonButton_NewSectionView.LargeImageSource = SAM.Core.UI.WPF.Convert.ToBitmapSource(Properties.Resources.SAM_Section);
            RibbonButton_NewSectionView.Click += RibbonButton_NewSectionView_Click;

            RibbonButton_New3DView.LargeImageSource = SAM.Core.UI.WPF.Convert.ToBitmapSource(Properties.Resources.SAM_Section);
            RibbonButton_New3DView.Click += RibbonButton_New3DView_Click;

            RibbonButton_ViewSettings.LargeImageSource = SAM.Core.UI.WPF.Convert.ToBitmapSource(Properties.Resources.SAM_Section);
            RibbonButton_ViewSettings.Click += RibbonButton_ViewSettings_Click;

            RibbonButton_CloseView.LargeImageSource = SAM.Core.UI.WPF.Convert.ToBitmapSource(Properties.Resources.SAM_Section);
            RibbonButton_CloseView.Click += RibbonButton_CloseView_Click;

            RibbonButton_AnalyticalModelLocation.LargeImageSource = SAM.Core.UI.WPF.Convert.ToBitmapSource(Properties.Resources.SAM_Location);
            RibbonButton_AnalyticalModelLocation.Click += RibbonButton_AnalyticalModelLocation_Click;

            RibbonButton_AnalyticalModelProperties.LargeImageSource = SAM.Core.UI.WPF.Convert.ToBitmapSource(Properties.Resources.SAM_AnalyticalModelProperties);
            RibbonButton_AnalyticalModelProperties.Click += RibbonButton_AnalyticalModelProperties_Click;

            RibbonButton_ImportObjects.LargeImageSource = SAM.Core.UI.WPF.Convert.ToBitmapSource(Properties.Resources.SAM_Import);
            RibbonButton_ImportObjects.Click += RibbonButton_ImportObjects_Click;

            RibbonButton_AnalyticalModelCheck.LargeImageSource = SAM.Core.UI.WPF.Convert.ToBitmapSource(Properties.Resources.SAM_ModelCheck);
            RibbonButton_AnalyticalModelCheck.Click += RibbonButton_AnalyticalModelCheck_Click;

            RibbonButton_EditMaterialLibrary.LargeImageSource = SAM.Core.UI.WPF.Convert.ToBitmapSource(Properties.Resources.SAM_MaterialLibrary);
            RibbonButton_EditMaterialLibrary.Click += RibbonButton_EditMaterialLibrary_Click;

            RibbonButton_EditInternalConditionLibrary.LargeImageSource = SAM.Core.UI.WPF.Convert.ToBitmapSource(Properties.Resources.SAM_InternalCondition);
            RibbonButton_EditInternalConditionLibrary.Click += RibbonButton_EditInternalConditionLibrary_Click;

            RibbonButton_EditProfileLibrary.LargeImageSource = SAM.Core.UI.WPF.Convert.ToBitmapSource(Properties.Resources.SAM_ProfileLibrary);
            RibbonButton_EditProfileLibrary.Click += RibbonButton_EditProfileLibrary_Click;

            RibbonButton_EditSpaces.LargeImageSource = SAM.Core.UI.WPF.Convert.ToBitmapSource(Properties.Resources.SAM_Space);
            RibbonButton_EditSpaces.Click += RibbonButton_EditSpaces_Click;

            RibbonButton_EditConstructions.LargeImageSource = SAM.Core.UI.WPF.Convert.ToBitmapSource(Properties.Resources.SAM_ConstructionLibrary);
            RibbonButton_EditConstructions.Click += RibbonButton_EditConstructions_Click;

            RibbonButton_EditApertureConstructions.LargeImageSource = SAM.Core.UI.WPF.Convert.ToBitmapSource(Properties.Resources.SAM_ApertureConstruction);
            RibbonButton_EditApertureConstructions.Click += RibbonButton_EditApertureConstructions_Click;

            RibbonButton_EditWeatherData.LargeImageSource = SAM.Core.UI.WPF.Convert.ToBitmapSource(Properties.Resources.SAM_WeatherData);
            RibbonButton_EditWeatherData.Click += RibbonButton_EditWeatherData_Click;

            RibbonButton_ImportWeatherData.LargeImageSource = SAM.Core.UI.WPF.Convert.ToBitmapSource(Properties.Resources.SAM_Import);
            RibbonButton_ImportWeatherData.Click += RibbonButton_ImportWeatherData_Click;

            RibbonButton_SolarSimulation.LargeImageSource = SAM.Core.UI.WPF.Convert.ToBitmapSource(Properties.Resources.SAM_SolarCalculator);
            RibbonButton_SolarSimulation.Click += RibbonButton_SolarSimulation_Click;

            RibbonButton_EnergySimulation.LargeImageSource = SAM.Core.UI.WPF.Convert.ToBitmapSource(Properties.Resources.SAM_EnergySimulation);
            RibbonButton_EnergySimulation.Click += RibbonButton_EnergySimulation_Click;

            RibbonButton_EditLibrary.LargeImageSource = SAM.Core.UI.WPF.Convert.ToBitmapSource(Properties.Resources.SAM_EditLibrary);
            RibbonButton_EditLibrary.Click += RibbonButton_EditLibrary_Click;

            RibbonButton_SetDefaultLayers.LargeImageSource = SAM.Core.UI.WPF.Convert.ToBitmapSource(Properties.Resources.SAM_ConstructionLibrary);
            RibbonButton_SetDefaultLayers.Click += RibbonButton_SetDefaultLayers_Click;

            RibbonButton_AssignMechanicalSystems.LargeImageSource = SAM.Core.UI.WPF.Convert.ToBitmapSource(Properties.Resources.SAM_EditLibrary);
            RibbonButton_AssignMechanicalSystems.Click += RibbonButton_AssignMechanicalSystems_Click;

            RibbonButton_OpenT3D.LargeImageSource = SAM.Core.UI.WPF.Convert.ToBitmapSource(Properties.Resources.SAM_T3D);
            RibbonButton_OpenT3D.Click += RibbonButton_OpenT3D_Click;

            RibbonButton_OpenTBD.LargeImageSource = SAM.Core.UI.WPF.Convert.ToBitmapSource(Properties.Resources.SAM_TBD);
            RibbonButton_OpenTBD.Click += RibbonButton_OpenTBD_Click;

            RibbonButton_OpenTSD.LargeImageSource = SAM.Core.UI.WPF.Convert.ToBitmapSource(Properties.Resources.SAM_TSD);
            RibbonButton_OpenTSD.Click += RibbonButton_OpenTSD_Click;

            RibbonButton_OpenTPD.LargeImageSource = SAM.Core.UI.WPF.Convert.ToBitmapSource(Properties.Resources.SAM_TPD);
            RibbonButton_OpenTPD.Click += RibbonButton_OpenTPD_Click;

            RibbonButton_ThermalTransmittanceCalculator.LargeImageSource = SAM.Core.UI.WPF.Convert.ToBitmapSource(Properties.Resources.SAM_T3D);
            RibbonButton_ThermalTransmittanceCalculator.Click += RibbonButton_ThermalTransmittanceCalculator_Click;

            RibbonButton_GlazingCalculator.LargeImageSource = SAM.Core.UI.WPF.Convert.ToBitmapSource(Properties.Resources.SAM_T3D);
            RibbonButton_GlazingCalculator.Click += RibbonButton_GlazingCalculator_Click;

            RibbonButton_CreateCases.LargeImageSource = SAM.Core.UI.WPF.Convert.ToBitmapSource(Properties.Resources.SAM_CreateCases);
            RibbonButton_CreateCases.Click += RibbonButton_CreateCases_Click;

            RibbonButton_SimulateCases.LargeImageSource = SAM.Core.UI.WPF.Convert.ToBitmapSource(Properties.Resources.SAM_CreateCases);
            RibbonButton_SimulateCases.Click += RibbonButton_SimulateCases_Click;

            RibbonButton_CreateSimulateCases.LargeImageSource = SAM.Core.UI.WPF.Convert.ToBitmapSource(Properties.Resources.SAM_CreateCases);
            RibbonButton_CreateSimulateCases.Click += RibbonButton_CreateSimulateCases_Click;

            RibbonButton_Solve.LargeImageSource = SAM.Core.UI.WPF.Convert.ToBitmapSource(Properties.Resources.SAM_CreateCases);
            RibbonButton_Solve.Click += RibbonButton_Solve_Click;

            RibbonMenuButton_PartL.LargeImageSource = SAM.Core.UI.WPF.Convert.ToBitmapSource(Properties.Resources.SAM_PartL);

            RibbonButton_OpenPartL.LargeImageSource = SAM.Core.UI.WPF.Convert.ToBitmapSource(Properties.Resources.SAM_PartL);
            RibbonButton_OpenPartL.Click += RibbonButton_OpenPartL_Click;

            RibbonButton_UpdateUKBRFile.LargeImageSource = SAM.Core.UI.WPF.Convert.ToBitmapSource(Properties.Resources.SAM_PartL_UKBR);
            RibbonButton_UpdateUKBRFile.Click += RibbonButton_UpdateUKBRFile_Click;

            RibbonButton_Hydra.LargeImageSource = SAM.Core.UI.WPF.Convert.ToBitmapSource(Properties.Resources.SAM_Hydra);
            RibbonButton_Hydra.Click += RibbonButton_Hydra_Click;

            RibbonButton_NCMNames.LargeImageSource = SAM.Core.UI.WPF.Convert.ToBitmapSource(Properties.Resources.SAM_EditLibrary);
            RibbonButton_NCMNames.Click += RibbonButton_NCMNames_Click;

            RibbonButton_FixNames.LargeImageSource = SAM.Core.UI.WPF.Convert.ToBitmapSource(Properties.Resources.SAM_EditLibrary);
            RibbonButton_FixNames.Click += RibbonButton_FixNames_Click;

            RibbonButton_CleanAnalyticalModel.LargeImageSource = SAM.Core.UI.WPF.Convert.ToBitmapSource(Properties.Resources.SAM_Clean);
            RibbonButton_CleanAnalyticalModel.Click += RibbonButton_CleanAnalyticalModel_Click;

            RibbonButton_RemoveAirMovementObjects.LargeImageSource = SAM.Core.UI.WPF.Convert.ToBitmapSource(Properties.Resources.SAM_Clean);
            RibbonButton_RemoveAirMovementObjects.Click += RibbonButton_RemoveAirMovementObjects_Click;

            //RibbonButton_CreateTBD.LargeImageSource = SAM.Core.UI.WPF.Convert.ToBitmapSource(Properties.Resources.SAM_TBD);
            //RibbonButton_CreateTBD.Click += RibbonButton_CreateTBD_Click;

            RibbonButton_AddMissingObjects.LargeImageSource = SAM.Core.UI.WPF.Convert.ToBitmapSource(Properties.Resources.SAM_AddMissingObjects);
            RibbonButton_AddMissingObjects.Click += RibbonButton_AddMissingObjects_Click;

            RibbonButton_PrintRoomDataSheets.LargeImageSource = SAM.Core.UI.WPF.Convert.ToBitmapSource(Properties.Resources.SAM_PrintRDS);
            RibbonButton_PrintRoomDataSheets.Click += RibbonButton_PrintRoomDataSheets_Click;

            RibbonButton_SpaceAssumptionsPdf.LargeImageSource = SAM.Core.UI.WPF.Convert.ToBitmapSource(Properties.Resources.SAM_Space);
            RibbonButton_SpaceAssumptionsPdf.Click += RibbonButton_SpaceAssumptionsPdf_Click;

            RibbonButton_SpaceDesignLoadSummaryPdf.LargeImageSource = SAM.Core.UI.WPF.Convert.ToBitmapSource(Properties.Resources.SAM_Space);
            RibbonButton_SpaceDesignLoadSummaryPdf.Click += RibbonButton_SpaceDesignLoadSummaryPdf_Click;

            RibbonButton_OpenMollierChart.LargeImageSource = SAM.Core.UI.WPF.Convert.ToBitmapSource(Properties.Resources.SAM_MollierDiagram);
            RibbonButton_OpenMollierChart.Click += RibbonButton_OpenMollierChart_Click;

            RibbonButton_ViewGeometry.LargeImageSource = SAM.Core.UI.WPF.Convert.ToBitmapSource(Properties.Resources.SAM_Space);
            RibbonButton_ViewGeometry.Click += RibbonButton_ViewGeometry_Click;

            RibbonButton_MapInternalConditions.LargeImageSource = SAM.Core.UI.WPF.Convert.ToBitmapSource(Properties.Resources.SAM_Space);
            RibbonButton_MapInternalConditions.Click += RibbonButton_MapInternalConditions_Click;

            RibbonButton_MapInternalConditionsByTM59.LargeImageSource = SAM.Core.UI.WPF.Convert.ToBitmapSource(Properties.Resources.SAM_Space);
            RibbonButton_MapInternalConditionsByTM59.Click += RibbonButton_MapInternalConditionsByTM59_Click;

            RibbonButton_AddVentilationPartF.LargeImageSource = Core.UI.WPF.Convert.ToBitmapSource(Properties.Resources.SAM_Space);
            RibbonButton_AddVentilationPartF.Click += RibbonButton_AddVentilationPartF_Click;

            RibbonButton_PartOWorkflow.LargeImageSource = Core.UI.WPF.Convert.ToBitmapSource(Properties.Resources.SAM_EnergySimulation);
            RibbonButton_PartOWorkflow.Click += RibbonButton_PartOWorkflow_Click;
            RibbonButton_PartOWorkflow.ToolTipTitle = "Part O — Prepare & Run";
            RibbonButton_PartOWorkflow.ToolTipDescription = "Inspect what this model already provides for an Approved Document O run, then prepare, check, simulate and assess it in one command. Existing results are reviewed without simulating again.";

            RibbonButton_PreparePartOIteration.LargeImageSource = Core.UI.WPF.Convert.ToBitmapSource(Properties.Resources.SAM_Space);
            RibbonButton_PreparePartOIteration.Click += RibbonButton_PreparePartOIteration_Click;

            RibbonButton_AssessPartOTM59.LargeImageSource = Core.UI.WPF.Convert.ToBitmapSource(Properties.Resources.SAM_Space);
            RibbonButton_AssessPartOTM59.Click += RibbonButton_AssessPartOTM59_Click;

            RibbonButton_OptimisePartOTM59.LargeImageSource = Core.UI.WPF.Convert.ToBitmapSource(Properties.Resources.SAM_Space);
            RibbonButton_OptimisePartOTM59.Click += RibbonButton_OptimisePartOTM59_Click;

            //The two gates above follow the run itself, not whichever command last moved it. Every Part O
            //transition - prepared, completed, restored, dropped, cleared - raises this, so a run that
            //becomes assessable enables its commands at that moment rather than at the next refresh
            //somebody remembered to write. It is what makes the completion ordering in Modify.Simulate safe:
            //the run is completed AFTER the model replacement that triggers a reload, so the reload's own
            //refresh necessarily still sees a prepared run. Subscribed here rather than at the field, since
            //the handler writes ribbon controls InitializeComponent has to have created first.
            partORun.StateChanged += PartORun_StateChanged;

            RibbonButton_EditInternalConditions.LargeImageSource = SAM.Core.UI.WPF.Convert.ToBitmapSource(Properties.Resources.SAM_Space);
            RibbonButton_EditInternalConditions.Click += RibbonButton_EditInternalConditions_Click;

            RibbonButton_TextMap.LargeImageSource = SAM.Core.UI.WPF.Convert.ToBitmapSource(Properties.Resources.SAM_PrintRDS);
            RibbonButton_TextMap.Click += RibbonButton_TextMap_Click;

            RibbonButton_SelectByFilter.LargeImageSource = SAM.Core.UI.WPF.Convert.ToBitmapSource(Properties.Resources.SAM_PrintRDS);
            RibbonButton_SelectByFilter.Click += RibbonButton_SelectByFilter_Click;

            RibbonButton_SelectByGuid.LargeImageSource = SAM.Core.UI.WPF.Convert.ToBitmapSource(Properties.Resources.SAM_PrintRDS);
            RibbonButton_SelectByGuid.Click += RibbonButton_SelectByGuid_Click;

            RibbonButton_RevealHidden.LargeImageSource = SAM.Core.UI.WPF.Convert.ToBitmapSource(Properties.Resources.SAM_PrintRDS);
            RibbonButton_RevealHidden.Click += RibbonButton_RevealHidden_Click;

            RibbonButton_ViewRange.LargeImageSource = SAM.Core.UI.WPF.Convert.ToBitmapSource(Properties.Resources.SAM_PrintRDS);
            RibbonButton_ViewRange.Click += RibbonButton_ViewRange_Click;

            RibbonButton_AirHandlingUnitDiagram.LargeImageSource = SAM.Core.UI.WPF.Convert.ToBitmapSource(Properties.Resources.SAM_AirHandlingUnitDiagram);
            RibbonButton_AirHandlingUnitDiagram.Click += RibbonButton_AirHandlingUnitDiagram_Click;

            RibbonButton_SpaceDiagram.LargeImageSource = SAM.Core.UI.WPF.Convert.ToBitmapSource(Properties.Resources.SAM_SpaceDiagram);
            RibbonButton_SpaceDiagram.Click += RibbonButton_SpaceDiagram_Click;

            RibbonButton_RemoveResults.LargeImageSource = SAM.Core.UI.WPF.Convert.ToBitmapSource(Properties.Resources.SAM_Close);
            RibbonButton_RemoveResults.Click += RibbonButton_RemoveResults_Click;

            RibbonButton_Wiki.LargeImageSource = SAM.Core.UI.WPF.Convert.ToBitmapSource(Properties.Resources.SAM_Wiki);
            RibbonButton_Wiki.Click += RibbonButton_Wiki_Click;

            RibbonButton_KeyboardShortcuts.LargeImageSource = SAM.Core.UI.WPF.Convert.ToBitmapSource(Properties.Resources.SAM_Wiki);
            RibbonButton_KeyboardShortcuts.Click += RibbonButton_KeyboardShortcuts_Click;

            RibbonButton_Undo.LargeImageSource = SAM.Core.UI.WPF.Convert.ToBitmapSource(Properties.Resources.SAM_Wiki);
            RibbonButton_Undo.Click += RibbonButton_Undo_Click;
            RibbonButton_Undo.IsEnabled = false;

            RibbonButton_Redo.LargeImageSource = SAM.Core.UI.WPF.Convert.ToBitmapSource(Properties.Resources.SAM_Wiki);
            RibbonButton_Redo.Click += RibbonButton_Redo_Click;
            RibbonButton_Redo.IsEnabled = false;

            RibbonButton_About.LargeImageSource = SAM.Core.UI.WPF.Convert.ToBitmapSource(Properties.Resources.SAM_Wiki);
            RibbonButton_About.Click += RibbonButton_About_Click;

            RibbonButton_Test.LargeImageSource = SAM.Core.UI.WPF.Convert.ToBitmapSource(Properties.Resources.SAM_Wiki);
            RibbonButton_Test.Click += RibbonButton_Test_Click;

            AnalyticalModelControl.ZoomRequested += AnalyticalModelControl_ZoomRequested;
            AnalyticalModelControl.SelectionRequested += AnalyticalModelControl_SelectionRequested;
            AnalyticalModelControl.TreeViewItemDropped += AnalyticalModelControl_TreeViewItemDropped;

            ThreeDimensionalViewSettings threeDimensionalViewSettings = new ThreeDimensionalViewSettings("3D View", null, null, null);

            GeometryObjectModel geometryObjectModel = Convert.ToSAM_GeometryObjectModel(uIAnalyticalModel?.JSAMObject, threeDimensionalViewSettings);

            viewportControl.UIGeometryObjectModel = new UIGeometryObjectModel(geometryObjectModel);
            viewportControl.Mode = threeDimensionalViewSettings.Mode();
            viewportControl.Loaded += ViewportControl_Loaded;
            viewportControl.ObjectHoovered += ViewportControl_ObjectHoovered;
            viewportControl.ObjectDoubleClicked += ViewportControl_ObjectDoubleClicked;
            viewportControl.ObjectContextMenuOpening += ViewControl_ObjectContextMenuOpening;
            viewportControl.Focus();

            uIAnalyticalModel = new UIAnalyticalModel();
            AnalyticalModelControl.UIAnalyticalModel = uIAnalyticalModel;
            uIAnalyticalModel.Modified += UIAnalyticalModel_Modified;
            uIAnalyticalModel.Closed += UIAnalyticalModel_Closed;
            uIAnalyticalModel.Opened += UIAnalyticalModel_Opened;

            SetEnabled();
        }

        private void Isolate(MenuItem menuItem)
        {
            if (menuItem == null)
            {
                return;
            }

            TabItem tabItem = tabControl.SelectedItem as TabItem;
            if (tabItem == null)
            {
                return;
            }

            ViewportControl viewportControl = tabItem.Content as ViewportControl;
            if (viewportControl == null)
            {
                return;
            }

            List<IJSAMObject> jSAMObjects = null;
            if (menuItem.Tag is IJSAMObject)
            {
                jSAMObjects = new List<IJSAMObject>() { (IJSAMObject)menuItem.Tag };
            }
            else if (menuItem.Tag is IEnumerable)
            {
                jSAMObjects = new List<IJSAMObject>();
                foreach (object @object in (IEnumerable)menuItem.Tag)
                {
                    if (@object is IJSAMObject)
                    {
                        jSAMObjects.Add((IJSAMObject)@object);
                    }
                }
            }

            SetUIGeometrySettings(tabControl, uIAnalyticalModel.JSAMObject);
            UI.Modify.Isolate(uIAnalyticalModel, viewportControl.Guid, jSAMObjects);
        }

        private void Isolate()
        {
            ViewportControl viewportControl = GetActiveViewportControl();
            if (viewportControl == null)
            {
                return;
            }

            List<SAMObject> jSAMObjects = viewportControl.SelectedSAMObjects<SAMObject>();

            SetUIGeometrySettings(tabControl, uIAnalyticalModel.JSAMObject);
            UI.Modify.Isolate(uIAnalyticalModel, viewportControl.Guid, jSAMObjects);
        }

        private void MenuItem_AssignApertureConstruction_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)sender;
            if (menuItem == null)
            {
                return;
            }

            List<Aperture> apertures = null;
            if (menuItem.Tag is Aperture)
            {
                apertures = new List<Aperture>() { (Aperture)menuItem.Tag };
            }
            else if (menuItem.Tag is IEnumerable)
            {
                apertures = new List<Aperture>();
                foreach (object @object in (IEnumerable)menuItem.Tag)
                {
                    if (@object is Aperture)
                    {
                        apertures.Add((Aperture)@object);
                    }
                }
            }

            Modify.AssignApertureApertureConstruction(uIAnalyticalModel, apertures);
        }

        private void MenuItem_AssignApertureConstructionByThermalTransmittance_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)sender;
            if (menuItem == null)
            {
                return;
            }

            List<Aperture> apertures = null;
            if (menuItem.Tag is Aperture)
            {
                apertures = new List<Aperture>() { (Aperture)menuItem.Tag };
            }
            else if (menuItem.Tag is IEnumerable)
            {
                apertures = new List<Aperture>();
                foreach (object @object in (IEnumerable)menuItem.Tag)
                {
                    if (@object is Aperture)
                    {
                        apertures.Add((Aperture)@object);
                    }
                }
            }

            Modify.AssignApertureApertureConstructionByThermalTransmittance(uIAnalyticalModel, apertures);
        }

        private void MenuItem_AssignConstruction_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)sender;
            if (menuItem == null)
            {
                return;
            }

            List<Panel> panels = null;
            if (menuItem.Tag is Panel)
            {
                panels = new List<Panel>() { (Panel)menuItem.Tag };
            }
            else if (menuItem.Tag is IEnumerable)
            {
                panels = new List<Panel>();
                foreach (object @object in (IEnumerable)menuItem.Tag)
                {
                    if (@object is Panel)
                    {
                        panels.Add((Panel)@object);
                    }
                }
            }

            Modify.AssignPanelConstruction(uIAnalyticalModel, panels);
        }

        private void MenuItem_AssignConstructionByThermalTransmittance_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)sender;
            if (menuItem == null)
            {
                return;
            }

            List<Panel> panels = null;
            if (menuItem.Tag is Panel)
            {
                panels = new List<Panel>() { (Panel)menuItem.Tag };
            }
            else if (menuItem.Tag is IEnumerable)
            {
                panels = new List<Panel>();
                foreach (object @object in (IEnumerable)menuItem.Tag)
                {
                    if (@object is Panel)
                    {
                        panels.Add((Panel)@object);
                    }
                }
            }

            Modify.AssignPanelConstructionByThermalTransmittance(uIAnalyticalModel, panels);
        }

        private void MenuItem_AssignInternalCondition_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)sender;
            if (menuItem == null)
            {
                return;
            }

            List<Space> spaces = null;
            if (menuItem.Tag is Space)
            {
                spaces = new List<Space>() { (Space)menuItem.Tag };
            }
            else if (menuItem.Tag is IEnumerable)
            {
                spaces = new List<Space>();
                foreach (object @object in (IEnumerable)menuItem.Tag)
                {
                    if (@object is Space)
                    {
                        spaces.Add((Space)@object);
                    }
                }
            }

            Modify.AssignSpaceInternalCondition(uIAnalyticalModel, spaces);
        }

        private void MenuItem_Close_TabItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            if (menuItem == null)
            {
                return;
            }

            TabItem tabItem = (menuItem.Parent as ContextMenu)?.Tag as TabItem;
            if (tabItem == null)
            {
                return;
            }

            EnableViewSettings(tabItem, false);
        }

        private void MenuItem_CloseAllButThis_TabItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            if (menuItem == null)
            {
                return;
            }

            TabItem tabItem = (menuItem.Parent as ContextMenu)?.Tag as TabItem;
            if (tabItem == null)
            {
                return;
            }

            List<TabItem> tabItems = tabControl.Items.Cast<TabItem>().ToList();
            tabItems.Remove(tabItem);

            EnableViewSettings(tabItems, false);
        }

        private void MenuItem_CloseSelected_TabItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            if (menuItem == null)
            {
                return;
            }

            TabItem tabItem = (menuItem.Parent as ContextMenu)?.Tag as TabItem;
            if (tabItem == null)
            {
                return;
            }

            Modify.EnableViewSettings(uIAnalyticalModel, false);
        }

        private void MenuItem_Delete_Click(object sender, RoutedEventArgs e)
        {
            //MenuItem menuItem = (MenuItem)sender;
            //if (menuItem == null)
            //{
            //    return;
            //}

            //List<Panel>? panels = null;
            //if (menuItem.Tag is Panel)
            //{
            //    panels = [(Panel)menuItem.Tag];
            //}
            //else if (menuItem.Tag is IEnumerable)
            //{
            //    panels = [];
            //    foreach (object @object in (IEnumerable)menuItem.Tag)
            //    {
            //        if (@object is Panel)
            //        {
            //            panels.Add((Panel)@object);
            //        }
            //    }
            //}

            //Modify.RemovePanels(uIAnalyticalModel, panels);

            Delete();
        }

        private void MenuItem_Duplicate_TabItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            if (menuItem == null)
            {
                return;
            }

            TabItem tabItem = (menuItem.Parent as ContextMenu)?.Tag as TabItem;
            if (tabItem == null)
            {
                return;
            }

            DuplicateViewSettings(tabItem);
        }

        private void MenuItem_EditInternalConditions_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)sender;
            if (menuItem == null)
            {
                return;
            }

            List<Space> spaces = null;
            if (menuItem.Tag is Space)
            {
                spaces = new List<Space>() { (Space)menuItem.Tag };
            }
            else if (menuItem.Tag is IEnumerable)
            {
                spaces = new List<Space>();
                foreach (object @object in (IEnumerable)menuItem.Tag)
                {
                    if (@object is Space)
                    {
                        spaces.Add((Space)@object);
                    }
                }
            }

            Modify.EditInternalConditions(uIAnalyticalModel, spaces);
        }

        private void MenuItem_EditMCMDatas(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)sender;
            if (menuItem == null)
            {
                return;
            }

            List<Space> spaces = null;
            if (menuItem.Tag is Space)
            {
                spaces = new List<Space>() { (Space)menuItem.Tag };
            }
            else if (menuItem.Tag is IEnumerable)
            {
                spaces = new List<Space>();
                foreach (object @object in (IEnumerable)menuItem.Tag)
                {
                    if (@object is Space)
                    {
                        spaces.Add((Space)@object);
                    }
                }
            }

            Modify.EditNCMDatas(uIAnalyticalModel, spaces);
        }

        private void MenuItem_EditOpeningProperties_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)sender;
            if (menuItem == null)
            {
                return;
            }

            List<Aperture> apertures = null;
            if (menuItem.Tag is Aperture)
            {
                apertures = new List<Aperture>() { (Aperture)menuItem.Tag };
            }
            else if (menuItem.Tag is IEnumerable)
            {
                apertures = new List<Aperture>();
                foreach (object @object in (IEnumerable)menuItem.Tag)
                {
                    if (@object is Aperture)
                    {
                        apertures.Add((Aperture)@object);
                    }
                }
            }

            Modify.EditOpeningProperties(uIAnalyticalModel, apertures);
        }

        private void MenuItem_Hide_Click(object sender, RoutedEventArgs e)
        {
            Hide((MenuItem)sender);
        }

        private void MenuItem_Isolate_Click(object sender, RoutedEventArgs e)
        {
            Isolate((MenuItem)sender);
        }

        private void MenuItem_Legend_Click(object sender, RoutedEventArgs e)
        {
            EditLegend();
        }

        private void MenuItem_LoadCamera_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            if (menuItem == null)
            {
                return;
            }

            TabItem tabItem = (menuItem.Parent as ContextMenu)?.Tag as TabItem;
            if (tabItem == null)
            {
                return;
            }

            CopyViewSettingsCamera(tabItem);
        }

        private void MenuItem_LoadView_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            if (menuItem == null)
            {
                return;
            }

            TabItem tabItem = (menuItem.Parent as ContextMenu)?.Tag as TabItem;
            if (tabItem == null)
            {
                return;
            }

            CopyViewSettings(tabItem);
        }

        private void MenuItem_ManageMechanicalSystems_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)sender;
            if (menuItem == null)
            {
                return;
            }

            List<Space> spaces = null;
            if (menuItem.Tag is Space)
            {
                spaces = new List<Space>() { (Space)menuItem.Tag };
            }
            else if (menuItem.Tag is IEnumerable)
            {
                spaces = new List<Space>();
                foreach (object @object in (IEnumerable)menuItem.Tag)
                {
                    if (@object is Space)
                    {
                        spaces.Add((Space)@object);
                    }
                }
            }

            //EditMechanicalSystems(spaces, spaces);
            AssignMechanicalSystems(spaces);
        }

        private void MenuItem_ManageZones_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)sender;
            if (menuItem == null)
            {
                return;
            }

            List<Space> spaces = null;
            if (menuItem.Tag is Space)
            {
                spaces = new List<Space>() { (Space)menuItem.Tag };
            }
            else if (menuItem.Tag is IEnumerable)
            {
                spaces = new List<Space>();
                foreach (object @object in (IEnumerable)menuItem.Tag)
                {
                    if (@object is Space)
                    {
                        spaces.Add((Space)@object);
                    }
                }
            }

            EditZones(spaces, spaces);
        }

        private void MenuItem_MenuItem_MapInternalCondition_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)sender;
            if (menuItem == null)
            {
                return;
            }

            List<Space> spaces = null;
            if (menuItem.Tag is Space)
            {
                spaces = new List<Space>() { (Space)menuItem.Tag };
            }
            else if (menuItem.Tag is IEnumerable)
            {
                spaces = new List<Space>();
                foreach (object @object in (IEnumerable)menuItem.Tag)
                {
                    if (@object is Space)
                    {
                        spaces.Add((Space)@object);
                    }
                }
            }

            Modify.MapInternalConditions(uIAnalyticalModel, spaces);
        }

        private void MenuItem_Properties_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)sender;
            if (menuItem == null)
            {
                return;
            }

            SAM.Core.UI.WPF.WindowHandle windowHandle = new SAM.Core.UI.WPF.WindowHandle(this);

            IJSAMObject jSAMObject = menuItem.Tag as IJSAMObject;
            if (jSAMObject is Panel)
            {
                Panel panel = (Panel)jSAMObject;
                uIAnalyticalModel.EditPanel(panel, windowHandle);
            }
            else if (jSAMObject is Space)
            {
                Space space = (Space)jSAMObject;
                uIAnalyticalModel.EditSpace(space, windowHandle);
            }
            else if (jSAMObject is Aperture)
            {
                Aperture aperture = (Aperture)jSAMObject;
                uIAnalyticalModel.EditAperture(aperture, windowHandle);
            }
        }

        private void MenuItem_RenameSpaces_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)sender;
            if (menuItem == null)
            {
                return;
            }

            List<Space> spaces = null;
            if (menuItem.Tag is Space)
            {
                spaces = new List<Space>() { (Space)menuItem.Tag };
            }
            else if (menuItem.Tag is IEnumerable)
            {
                spaces = new List<Space>();
                foreach (object @object in (IEnumerable)menuItem.Tag)
                {
                    if (@object is Space)
                    {
                        spaces.Add((Space)@object);
                    }
                }
            }

            Modify.RenameSpaces(uIAnalyticalModel, spaces);

            //if(spaces != null && spaces.Count != 0)
            //{
            //    GetActiveViewportControl();
            //}
        }

        private void MenuItem_RevealHidden_Click(object sender, RoutedEventArgs e)
        {
            RevealHidden();
        }

        private void MenuItem_Reverse_Click(object sender, RoutedEventArgs e)
        {
            Reverse();
        }

        private void MenuItem_SelectByApertureConstructionName_Click(object sender, RoutedEventArgs e)
        {
            SelectByApertureConstructionName();
        }

        private void MenuItem_SelectByApertureType_Click(object sender, RoutedEventArgs e)
        {
            SelectByApertureType();
        }

        private void MenuItem_SelectByConstructionName_Click(object sender, RoutedEventArgs e)
        {
            SelectByConstructionName();
        }

        private void MenuItem_SelectByFilter_Click(object sender, RoutedEventArgs e)
        {
            SelectByFilter();
        }

        private void MenuItem_SelectByGuid_Click(object sender, RoutedEventArgs e)
        {
            SelectByGuid();
        }

        private void MenuItem_SelectByLegend_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)sender;
            if (menuItem == null)
            {
                return;
            }

            List<IJSAMObject>? jSAMObjects = menuItem.Tag as List<IJSAMObject>;
            if (jSAMObjects is null)
            {
                return;
            }

            if (GetActiveViewSettings() is not ViewSettings viewSettings)
            {
                return;
            }

            AdjacencyCluster? adjacencyCluster = uIAnalyticalModel?.JSAMObject?.AdjacencyCluster;
            if (adjacencyCluster is null)
            {
                return;
            }

            List<Tuple<Type, object, string, string>> tuples = new List<Tuple<Type, object, string, string>>();
            foreach (IJSAMObject jSAMObject in jSAMObjects)
            {
                object value = null;
                string text = null;
                string parameterName = null;

                if (jSAMObject is Panel panel && UI.Query.TryGetValue(panel, adjacencyCluster, viewSettings, out value, out text, out parameterName))
                {
                    tuples.Add(new Tuple<Type, object, string, string>(typeof(Panel), value, text, parameterName));
                }
                else if (jSAMObject is Space space && UI.Query.TryGetValue(space, adjacencyCluster, viewSettings, out value, out text, out parameterName))
                {
                    tuples.Add(new Tuple<Type, object, string, string>(typeof(Space), value, text, parameterName));
                }
                else if (jSAMObject is Aperture aperture && UI.Query.TryGetValue(aperture, adjacencyCluster, viewSettings, out value, out text, out parameterName))
                {
                    tuples.Add(new Tuple<Type, object, string, string>(typeof(Aperture), value, text, parameterName));
                }
            }

            if (tuples is null || tuples.Count == 0)
            {
                return;
            }

            List<IJSAMObject> jSAMObjects_Filtered = [];

            List<Tuple<Type, object, string, string>> tuples_Filtered = null;

            tuples_Filtered = tuples.FindAll(x => x.Item1 == typeof(Panel));
            if (tuples_Filtered is not null && tuples_Filtered.Count > 0)
            {
                List<Panel> panels = adjacencyCluster.GetPanels();
                if (panels is not null && panels.Count > 0)
                {
                    foreach (Panel panel in panels)
                    {
                        if (UI.Query.TryGetValue(panel, adjacencyCluster, viewSettings, out object value, out string text, out string parameterName))
                        {
                            if (tuples_Filtered.Find(x => x.Item3 == text && x.Item4 == parameterName) != null)
                            {
                                jSAMObjects_Filtered.Add(panel);
                            }
                        }
                    }
                }
            }

            tuples_Filtered = tuples.FindAll(x => x.Item1 == typeof(Space));
            if (tuples_Filtered is not null && tuples_Filtered.Count > 0)
            {
                List<Space> spaces = adjacencyCluster.GetSpaces();
                if (spaces is not null && spaces.Count > 0)
                {
                    foreach (Space space in spaces)
                    {
                        if (UI.Query.TryGetValue(space, adjacencyCluster, viewSettings, out object value, out string text, out string parameterName))
                        {
                            if (tuples_Filtered.Find(x => x.Item3 == text && x.Item4 == parameterName) != null)
                            {
                                jSAMObjects_Filtered.Add(space);
                            }
                        }
                    }
                }
            }

            tuples_Filtered = tuples.FindAll(x => x.Item1 == typeof(Aperture));
            if (tuples_Filtered is not null && tuples_Filtered.Count > 0)
            {
                List<Aperture> apertures = adjacencyCluster.GetApertures();
                if (apertures is not null && apertures.Count > 0)
                {
                    foreach (Aperture aperture in apertures)
                    {
                        if (UI.Query.TryGetValue(aperture, adjacencyCluster, viewSettings, out object value, out string text, out string parameterName))
                        {
                            if (tuples_Filtered.Find(x => x.Item3 == text && x.Item4 == parameterName) != null)
                            {
                                jSAMObjects_Filtered.Add(aperture);
                            }
                        }
                    }
                }
            }

            ViewportControl viewportControl = GetActiveViewportControl();
            if (viewportControl == null)
            {
                return;
            }

            viewportControl?.Select(jSAMObjects_Filtered?.FindAll(x => x is SAMObject)?.Cast<SAMObject>());

        }

        private void MenuItem_SelectByPanelGroup_Click(object sender, RoutedEventArgs e)
        {
            SelectByPanelGroup();
        }

        private void MenuItem_SelectByPanelType_Click(object sender, RoutedEventArgs e)
        {
            SelectByPanelType();
        }

        private void MenuItem_Settings_TabItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            if (menuItem == null)
            {
                return;
            }

            TabItem tabItem = (menuItem.Parent as ContextMenu)?.Tag as TabItem;
            if (tabItem == null)
            {
                return;
            }

            EditViewSettings(tabItem);
        }

        private void MenuItem_ViewSettings_Click(object sender, RoutedEventArgs e)
        {
            EditViewSettings();
        }

        private void New3DView()
        {
            if (uIAnalyticalModel == null)
            {
                return;
            }

            AnalyticalModel analyticalModel = uIAnalyticalModel.JSAMObject;
            if (analyticalModel == null)
            {
                return;
            }

            ThreeDimensionalViewSettings threeDimensionalViewSettings = UI.Query.DefaultViewSettings(analyticalModel) as ThreeDimensionalViewSettings;

            TabItem tabItem = UpdateTabItem(tabControl, analyticalModel, new ModifiedEventArgs(new ViewSettingsModification(threeDimensionalViewSettings)), threeDimensionalViewSettings);
            if (tabItem != null)
            {
                tabControl.SelectedItem = tabItem;
            }

            SetUIGeometrySettings(tabControl, analyticalModel);
        }

        private bool Open(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path))
            {
                return false;
            }

            uIAnalyticalModel = new UIAnalyticalModel();
            AnalyticalModelControl.UIAnalyticalModel = uIAnalyticalModel;
            uIAnalyticalModel.Path = path;
            uIAnalyticalModel.Modified += UIAnalyticalModel_Modified;
            uIAnalyticalModel.Closed += UIAnalyticalModel_Closed;
            uIAnalyticalModel.Opened += UIAnalyticalModel_Opened;

            return uIAnalyticalModel.Open();
        }

        private void Redo()
        {
            uIAnalyticalModel?.Redo();
        }

        private void RefreshDoubleRangeWindow()
        {
            if (doubleRangeWindow == null)
            {
                return;
            }

            ThreeDimensionalViewSettings threeDimensionalViewSettings = GetActiveViewSettings() as ThreeDimensionalViewSettings;
            if (threeDimensionalViewSettings == null)
            {
                return;
            }

            double max = 0;
            double min = 0;
            Range<double> range = uIAnalyticalModel?.JSAMObject?.GetElevationRange();
            if (range != null)
            {
                max = range.Max;
                min = range.Min;
            }

            range = new Range<double>(min, max);

            List<Geometry.Spatial.Plane> planes = threeDimensionalViewSettings.Planes;
            if (planes != null && planes.Count != 0)
            {
                if (planes.Count == 1)
                {
                    Geometry.Spatial.Plane plane = planes[0];

                    if (plane.Normal.Z > 0)
                    {
                        range = new Range<double>(plane.Origin.Z, max);
                    }
                    else
                    {
                        range = new Range<double>(plane.Origin.Z, min);
                    }
                }
                else
                {
                    List<double> values = planes.ConvertAll(x => x.Origin.Z);
                    range = new Range<double>(values);
                }
            }

            doubleRangeWindow.Range = range;
        }

        // Keep the ribbon Undo/Redo buttons enabled only when there is something to undo/redo. Called
        // after every model change (UIAnalyticalModel_Modified) and after open/new/close.
        private void RefreshHistoryButtons()
        {
            RibbonButton_Undo.IsEnabled = uIAnalyticalModel != null && uIAnalyticalModel.CanUndo;
            RibbonButton_Redo.IsEnabled = uIAnalyticalModel != null && uIAnalyticalModel.CanRedo;
        }

        // Regenerate a view tab whose geometry update was deferred while it was inactive.
        // No-op if the tab is not dirty, so activating an up-to-date tab stays free.
        private void RegenerateIfDirty(Guid guid)
        {
            if (guid == Guid.Empty || !dirtyViewGuids.Contains(guid))
            {
                return;
            }

            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if (analyticalModel == null)
            {
                return;
            }

            IViewSettings viewSettings = Query.ViewSettings<ViewSettings>(uIAnalyticalModel, guid);
            if (viewSettings == null)
            {
                dirtyViewGuids.Remove(guid);
                return;
            }

            uIAnalyticalModel.Modified -= UIAnalyticalModel_Modified;
            tabControl.SelectionChanged -= TabControl_SelectionChanged;

            progressBarWindowManager?.Show("Reloading", "Reloading...");

            // FullModification forces the geometry regeneration; UpdateTabItem clears the dirty flag.
            UpdateTabItem(tabControl, analyticalModel, new ModifiedEventArgs(new FullModification()), viewSettings, true);

            progressBarWindowManager?.Close();

            tabControl.SelectionChanged += TabControl_SelectionChanged;
            uIAnalyticalModel.Modified += UIAnalyticalModel_Modified;
        }

        private void Reload(ModifiedEventArgs modifiedEventArgs)
        {
            string modifications = modifiedEventArgs?.Modifications == null ? null : string.Join(",", modifiedEventArgs.Modifications.ConvertAll(x => x?.GetType().Name));

            using (Core.UI.PerformanceLog.Measure("AnalyticalWindow.Reload", modifications))
            {
                progressBarWindowManager.Show("Reloading", "Reloading...");

                SetEnabled();
                //SetActiveGuid();

                uIAnalyticalModel.Modified -= UIAnalyticalModel_Modified;
                tabControl.SelectionChanged -= TabControl_SelectionChanged;

                AnalyticalModel analyticalModel = uIAnalyticalModel.JSAMObject;

                UpdateTabItems(tabControl, analyticalModel, modifiedEventArgs);

                UpdateUIGeometrySettings(tabControl, analyticalModel, modifiedEventArgs);

                // Re-commit the (tab/geometry-settings-updated) model without adding an undo entry: the
                // triggering edit was already captured by its own SetJSAMObject, so capturing here too
                // would make undo require two clicks.
                uIAnalyticalModel.SetJSAMObject(analyticalModel, modifiedEventArgs.Modifications, false);

                uIAnalyticalModel.Modified += UIAnalyticalModel_Modified;
                tabControl.SelectionChanged += TabControl_SelectionChanged;

                progressBarWindowManager.Close();
            }
        }

        private void RemoveViewSettings()
        {
            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if (analyticalModel == null)
            {
                return;
            }

            TabItem tabItem = tabControl.SelectedItem as TabItem;
            if (tabItem == null)
            {
                return;
            }

            RemoveViewSettings(tabItem);
        }

        private void RemoveViewSettings(TabItem tabItem)
        {
            if (tabItem == null)
            {
                return;
            }

            ViewportControl viewportControl = tabItem.Content as ViewportControl;
            if (viewportControl == null)
            {
                return;
            }

            //SetActiveGuid();
            SetUIGeometrySettings(tabControl, uIAnalyticalModel.JSAMObject);
            Modify.RemoveViewSettings(uIAnalyticalModel, viewportControl.Guid);
        }

        private void RevealHidden()
        {
            //TabItem tabItem = tabControl.SelectedItem as TabItem;
            //if (tabItem == null)
            //{
            //    return;
            //}

            //ViewportControl viewportControl = tabItem.Content as ViewportControl;
            //if (viewportControl == null)
            //{
            //    return;
            //}

            ViewportControl viewportControl = GetActiveViewportControl();
            if (viewportControl == null)
            {
                return;
            }

            //if (viewportControl != null)
            //{
            //    List<SAMObject> sAMObjects_Selected = viewportControl.SelectedSAMObjects<SAMObject>();
            //    if (sAMObjects_Selected != null)
            //    {
            //        jSAMObjects.AddRange(sAMObjects_Selected);
            //    }
            //}

            SetUIGeometrySettings(tabControl, uIAnalyticalModel.JSAMObject);
            UI.Modify.RemoveOverrides(uIAnalyticalModel, viewportControl.Guid);
        }

        private void Reverse()
        {
            ViewportControl viewportControl = GetActiveViewportControl();
            if (viewportControl == null)
            {
                return;
            }

            AdjacencyCluster? adjacencyCluster = uIAnalyticalModel.JSAMObject?.AdjacencyCluster;

            List<Tuple<SAMObject, bool>> tuples = [];

            if (adjacencyCluster?.GetPanels() is List<Panel> panels)
            {
                foreach (Panel panel in panels)
                {
                    tuples.Add(new Tuple<SAMObject, bool>(panel, viewportControl.Contains<Panel>(panel.Guid)));

                    List<Aperture> apertures = panel.Apertures;
                    if (apertures != null)
                    {
                        foreach (Aperture aperture in apertures)
                        {
                            tuples.Add(new Tuple<SAMObject, bool>(aperture, viewportControl.Contains<Aperture>(aperture.Guid)));
                        }
                    }
                }
            }

            if (adjacencyCluster?.GetSpaces() is List<Space> spaces)
            {
                foreach (Space space in spaces)
                {
                    tuples.Add(new Tuple<SAMObject, bool>(space, viewportControl.Contains<Space>(space.Guid)));
                }
            }

            if (tuples is null || tuples.Count == 0)
            {
                return;
            }

            List<SAMObject> sAMObjects_Selected = viewportControl.SelectedSAMObjects<SAMObject>();
            if(sAMObjects_Selected is null || sAMObjects_Selected.Count == 0)
            {
                UI.Modify.Hide(uIAnalyticalModel, viewportControl.Guid, tuples.ConvertAll(x => x.Item1), tuples.ConvertAll(x => !x.Item2));
            }
            else
            {
                List<SAMObject> sAMObjects = tuples.FindAll(x => x.Item2).ConvertAll(x => x.Item1);
                foreach(SAMObject sAMObject_Selected in sAMObjects_Selected)
                {
                    int index = sAMObjects.FindIndex(x => x.Guid == sAMObject_Selected.Guid);
                    if(index != -1)
                    {
                        sAMObjects.RemoveAt(index);
                    }
                }

                viewportControl.Select(sAMObjects);
            }
        }
        
        private void RibbonButton_About_Click(object sender, RoutedEventArgs e)
        {
            List<AboutInfoType> abouInfoTypes = Enum.GetValues(typeof(AboutInfoType)).Cast<AboutInfoType>().ToList();

            SAM.Core.UI.WPF.ComboBoxWindow<AboutInfoType> comboBoxWindow = new SAM.Core.UI.WPF.ComboBoxWindow<AboutInfoType>("Info", abouInfoTypes, x => x.Description()) { Owner = this };
            if (comboBoxWindow.ShowDialog() == true)
            {
                MessageBox.Show(Core.Query.AboutInfoTypeText(comboBoxWindow.SelectedItem), "Info");
            }
        }

        private void RibbonButton_AddMissingObjects_Click(object sender, RoutedEventArgs e)
        {
            uIAnalyticalModel?.AddMissingObjects(windowHandle);
        }

        private void RibbonButton_AddVentilationPartF_Click(object sender, RoutedEventArgs e)
        {
            Modify.AddVentilationByPartF(uIAnalyticalModel, windowHandle);
        }

        /// <summary>
        /// The high-level Part O command. It owns no state of its own: the same <see cref="partORun"/> the
        /// expert commands use is handed to it, so a run prepared through the picker is visible here and a
        /// run produced here is assessable from the Results tab.
        /// </summary>
        private void RibbonButton_PartOWorkflow_Click(object sender, RoutedEventArgs e)
        {
            Modify.RunPartOWorkflow(uIAnalyticalModel, partORun, windowHandle);

            //The workflow can leave the run in any of its states - prepared, completed, or dropped - and a
            //cancelled dialog raises no modification, so the Results-tab gates are refreshed here for the
            //same reason the preparation command refreshes them.
            RefreshPartOButtons();
        }

        private void RibbonButton_PreparePartOIteration_Click(object sender, RoutedEventArgs e)
        {
            Modify.PreparePartOIteration(uIAnalyticalModel, partORun, windowHandle);

            //The assessment gate moves with the run, not with the model, so it is refreshed here rather than
            //left to the modification handler - a preparation the user cancelled raises no modification.
            RefreshPartOButtons();
        }

        private void RibbonButton_OptimisePartOTM59_Click(object sender, RoutedEventArgs e)
        {
            Modify.RunPartOOptimisation(uIAnalyticalModel, partORun, windowHandle);

            RefreshPartOButtons();
        }

        private void RibbonButton_AssessPartOTM59_Click(object sender, RoutedEventArgs e)
        {
            Modify.AssessPartOTM59(partORun, windowHandle);

            //A stale results file is discovered by IsAssessable at click time, so the gate can have closed.
            RefreshPartOButtons();
        }

        /// <summary>
        /// Enables the Part O assessment only for a run that has results to assess, and says through the
        /// tooltip why it is unavailable when it is not.
        /// <para>
        /// <b>Two ways a run can have results.</b> The session's own completed workflow, and a run
        /// <i>restored</i> from a reopened model that records the results it was produced from
        /// (<c>PartORun.Restore</c>) - the review path that needs no new simulation. Both are the same state
        /// here: <c>PartORun.CanAssess</c> is the only condition read, and it stays a pure state read. The
        /// command re-checks it - and re-checks the results file - so this is presentation, not the gate.
        /// </para>
        /// <para>
        /// <b>Optimise is deliberately stricter, and stays that way.</b> Reviewing needs a model and its
        /// results; optimising additionally needs how the run was prepared and which TAS case produced it -
        /// the session state a restored run provably does not carry - so a restored run can be reviewed but
        /// never resumed into Iteration 2B, and the tooltip says the two apart.
        /// </para>
        /// </summary>
        private void PartORun_StateChanged(object sender, EventArgs e)
        {
            RefreshPartOButtons();
        }

        private void RefreshPartOButtons()
        {
            bool canAssess = partORun.CanAssess;

            RibbonButton_AssessPartOTM59.IsEnabled = canAssess;

            RibbonButton_AssessPartOTM59.ToolTipDescription = canAssess
                ? partORun.IsRestored
                    ? "Review this run's CIBSE TM59 assessment from the results it records - the existing simulation results are read and reassessed; no new simulation is run."
                    : "Assess the completed Part O run against the CIBSE TM59 criteria, using the model the TAS workflow returned."
                : partORun.State == PartORunState.Prepared
                    ? "A Part O iteration is prepared but not simulated. Run the energy simulation first."
                    : partORun.InvalidationReason ?? "Prepare a Part O iteration and run the energy simulation first.";

            //The same pure state read as above, plus the one thing 2B additionally needs that is knowable
            //without touching the filesystem: a product selected for it to work within. Its settings are
            //confirmed when it starts, so they are not required here. Modify.CanOptimise is still the gate -
            //it re-checks the results file and the recorded TAS case - so this stays presentation.
            bool canOptimise = canAssess
                && (partORun.PreparationContext?.HasVentilationUnitCatalogue ?? false);

            RibbonButton_OptimisePartOTM59.IsEnabled = canOptimise;

            RibbonButton_OptimisePartOTM59.ToolTipDescription = canOptimise
                ? "Optimise ventilation (Iteration 2B): raise the design airflow of failing mechanically ventilated rooms by a fixed step, rebalance, re-prepare, re-simulate the same weather case and reassess - until every eligible space passes or the selected ventilation unit cannot carry another full step. You confirm the step and the round limit before it starts. The selected product is never changed."
                : canAssess && partORun.IsRestored
                    ? "This run was reopened from its saved results, which is enough to review its TM59 assessment but not to resume Iteration 2B: optimising repeats the recorded preparation and the same TAS case, and those belong to the session that produced them. Prepare the iteration again and re-run the simulation to optimise."
                    : !canAssess
                    ? "Iteration 2B optimises a completed Iteration 2 run. " + (partORun.InvalidationReason ?? "Prepare a Part O iteration, simulate it over the full year, and assess it first.")
                    : "This Part O run was prepared without a selected ventilation unit, so it is an Iteration 1a run and there is nothing for Iteration 2B to optimise within. Prepare the iteration again with a manufacturer ventilation unit selected.";
        }
        
        private void RibbonButton_AirHandlingUnitDiagram_Click(object sender, RoutedEventArgs e)
        {
            uIAnalyticalModel?.AirHandlingUnitDiagram(windowHandle);
        }

        private void RibbonButton_AnalyticalModelCheck_Click(object sender, RoutedEventArgs e)
        {
            uIAnalyticalModel?.Check(windowHandle);
        }

        private void RibbonButton_AnalyticalModelLocation_Click(object sender, RoutedEventArgs e)
        {
            uIAnalyticalModel?.EditAddressAndLocation();
        }

        private void RibbonButton_AnalyticalModelProperties_Click(object sender, RoutedEventArgs e)
        {
            uIAnalyticalModel?.EditProperties(windowHandle);
        }

        private void RibbonButton_AssignMechanicalSystems_Click(object sender, RoutedEventArgs e)
        {
            List<Space> spaces = GetActiveViewportControl()?.SelectedSAMObjects<Space>();
            if (spaces == null || spaces.Count == 0)
            {
                spaces = null;
            }

            //uIAnalyticalModel?.AssignMechanicalSystems(spaces);
            uIAnalyticalModel?.AddMechanicalSystems(spaces);
        }

        private void RibbonButton_CleanAnalyticalModel_Click(object sender, RoutedEventArgs e)
        {
            uIAnalyticalModel?.Clean(windowHandle);
        }

        private void RibbonButton_CloseAnalyticalModel_Click(object sender, RoutedEventArgs e)
        {
            if (uIAnalyticalModel == null)
            {
                return;
            }

            uIAnalyticalModel.Close();
        }

        private void RibbonButton_CloseView_Click(object sender, RoutedEventArgs e)
        {
            EnableViewSettings(false);
        }

        private void RibbonButton_CreateCases_Click(object sender, RoutedEventArgs e)
        {
            if (uIAnalyticalModel?.JSAMObject is not AnalyticalModel analyticalModel)
            {
                return;
            }

            ViewportControl viewportControl = GetActiveViewportControl();
            if (viewportControl == null)
            {
                return;
            }

            List<SAMObject> sAMObjects_Selected = viewportControl.SelectedSAMObjects<SAMObject>();


            List<AnalyticalModel>? analyticalModel_Case = Create.AnalyticalModels(analyticalModel, sAMObjects_Selected);
        }

        private void RibbonButton_CreateSimulateCases_Click(object sender, RoutedEventArgs e)
        {
            if (uIAnalyticalModel?.JSAMObject is not AnalyticalModel analyticalModel)
            {
                return;
            }

            Modify.CreateSimulateCases(uIAnalyticalModel);
        }

        private void RibbonButton_CreateTBD_Click(object sender, RoutedEventArgs e)
        {
            uIAnalyticalModel?.Simulate();
        }

        private void RibbonButton_EditApertureConstructions_Click(object sender, RoutedEventArgs e)
        {
            uIAnalyticalModel?.EditApertureConstructions(windowHandle);
        }

        private void RibbonButton_EditConstructions_Click(object sender, RoutedEventArgs e)
        {
            uIAnalyticalModel?.EditConstructions(windowHandle);
        }

        private void RibbonButton_EditInternalConditionLibrary_Click(object sender, RoutedEventArgs e)
        {
            uIAnalyticalModel?.EditInternalConditions(windowHandle);
        }

        private void RibbonButton_EditInternalConditions_Click(object sender, RoutedEventArgs e)
        {
            InternalConditionWithSpacesWindow internalConditionWindow = new InternalConditionWithSpacesWindow(uIAnalyticalModel);
            bool? result = internalConditionWindow.ShowDialog();

            if (result == null || !result.HasValue || !result.Value)
            {
                return;
            }
        }

        private void RibbonButton_EditLibrary_Click(object sender, RoutedEventArgs e)
        {
            UI.Modify.EditLibrary(windowHandle);
        }

        private void RibbonButton_EditMaterialLibrary_Click(object sender, RoutedEventArgs e)
        {
            uIAnalyticalModel?.EditMaterialLibrary(windowHandle);
        }

        private void RibbonButton_EditProfileLibrary_Click(object sender, RoutedEventArgs e)
        {
            uIAnalyticalModel?.EditProfileLibrary(windowHandle);
        }

        private void RibbonButton_EditSpaces_Click(object sender, RoutedEventArgs e)
        {
            uIAnalyticalModel?.EditSpaces(windowHandle);
        }

        private void RibbonButton_EditWeatherData_Click(object sender, RoutedEventArgs e)
        {
            uIAnalyticalModel?.EditWeatherData(windowHandle);
        }

        private void RibbonButton_EnergySimulation_Click(object sender, RoutedEventArgs e)
        {
            //uIAnalyticalModel?.EnergySimulation(windowHandle);
            //The Part O run goes in so a workflow over a prepared model can complete it. A run in any other
            //state is dropped by this simulation's own model replacement, which is the intended behaviour.
            uIAnalyticalModel?.Simulate(partORun);

            RefreshPartOButtons();
        }

        private void RibbonButton_ExportAnalyticalModel_Click(object sender, RoutedEventArgs e)
        {
            Modify.Export(uIAnalyticalModel);
        }

        private void RibbonButton_FixNames_Click(object sender, RoutedEventArgs e)
        {
            uIAnalyticalModel.ReplaceNameSpecialCharacters(windowHandle);
        }

        private void RibbonButton_GlazingCalculator_Click(object sender, RoutedEventArgs e)
        {
            Modify.CalculateGlazing(uIAnalyticalModel);
        }

        private void RibbonButton_Hydra_Click(object sender, RoutedEventArgs e)
        {
            Core.Query.StartProcess(Link.Hydra);
        }

        private void RibbonButton_ImportAnalyticalModel_Click(object sender, RoutedEventArgs e)
        {
            Modify.Import(uIAnalyticalModel);
        }

        private void RibbonButton_ImportObjects_Click(object sender, RoutedEventArgs e)
        {
            uIAnalyticalModel?.Import(this);

            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if (analyticalModel != null)
            {
                if (!analyticalModel.TryGetValue(AnalyticalModelParameter.UIGeometrySettings, out UIGeometrySettings uIGeometrySettings) || uIGeometrySettings == null || !uIGeometrySettings.HasAnyViewSettings())
                {
                    List<Panel> panels = analyticalModel.GetPanels();
                    if (panels != null && panels.Count != 0)
                    {
                        New3DView();
                    }
                }
            }
        }

        private void RibbonButton_ImportWeatherData_Click(object sender, RoutedEventArgs e)
        {
            uIAnalyticalModel?.ImportWeatherData(windowHandle);
        }

        private void RibbonButton_KeyboardShortcuts_Click(object sender, RoutedEventArgs e)
        {
            KeyboardShortcutsWindow keyboardShortcutsWindow = new KeyboardShortcutsWindow() { Owner = this };
            keyboardShortcutsWindow.ShowDialog();
        }

        private void RibbonButton_MapInternalConditions_Click(object sender, RoutedEventArgs e)
        {
            Modify.MapInternalConditions(uIAnalyticalModel);
        }

        private void RibbonButton_MapInternalConditionsByTM59_Click(object sender, RoutedEventArgs e)
        {
            Modify.MapInternalConditionsByTM59(uIAnalyticalModel);
        }

        private void RibbonButton_NCMNames_Click(object sender, RoutedEventArgs e)
        {
            NCMNameCollectionWindow nCMNameCollectionWindow = new NCMNameCollectionWindow(Analytical.Query.DefaultNCMNameCollection(), new NCMNameCollectionOptions(false));
            nCMNameCollectionWindow.ShowDialog();
        }

        private void RibbonButton_New3DView_Click(object sender, RoutedEventArgs e)
        {
            New3DView();
        }

        private void RibbonButton_NewAnalyticalModel_Click(object sender, RoutedEventArgs e)
        {
            if (uIAnalyticalModel == null)
            {
                return;
            }

            if (uIAnalyticalModel.New())
            {
                //Refresh_AnalyticalModel();
            }
        }

        private void RibbonButton_NewSectionView_Click(object sender, RoutedEventArgs e)
        {
            if (uIAnalyticalModel == null)
            {
                return;
            }

            AnalyticalModel analyticalModel = uIAnalyticalModel.JSAMObject;
            if (analyticalModel == null)
            {
                return;
            }

            TwoDimensionalViewSettings twoDimensionalViewSettings = new TwoDimensionalViewSettings(Guid.NewGuid(), "New Section View", Geometry.Spatial.Create.Plane(0.0), null, new Type[] { typeof(Space), typeof(Panel), typeof(Aperture) }, Geometry.Object.Query.DefaultTextAppearance(), null);
            twoDimensionalViewSettings.AddAppearanceSettings(new SpaceAppearanceSettings("Name"));

            //Creating, not editing: this is what lets choosing the Part F colour scheme initialise a usable
            //Part F drawing. Editing an existing view never does. See AnalyticalTwoDimensionalViewSettingsControl.
            ViewSettingsWindow viewSettingsWindow = new ViewSettingsWindow(twoDimensionalViewSettings, analyticalModel, true);
            bool? result = viewSettingsWindow.ShowDialog();
            if (result == null || !result.HasValue || !result.Value)
            {
                return;
            }

            ViewSettings viewSettings = viewSettingsWindow.ViewSettings;

            TabItem tabItem = UpdateTabItem(tabControl, analyticalModel, new ModifiedEventArgs(new ViewSettingsModification(viewSettings)), viewSettings);
            if (tabItem != null)
            {
                tabControl.SelectedItem = tabItem;
            }

            SetUIGeometrySettings(tabControl, analyticalModel);
        }

        private void RibbonButton_NewSectionViews_Click(object sender, RoutedEventArgs e)
        {
            if (uIAnalyticalModel == null)
            {
                return;
            }

            AnalyticalModel analyticalModel = uIAnalyticalModel.JSAMObject;
            if (analyticalModel == null)
            {
                return;
            }

            BatchCreateViewsWindow batchCreateViewsWindow = new BatchCreateViewsWindow(analyticalModel.AdjacencyCluster);
            bool? result = batchCreateViewsWindow.ShowDialog();
            if (result == null || !result.HasValue || !result.Value)
            {
                return;
            }

            List<TwoDimensionalViewSettings> twoDimensionalViewSettingsList = batchCreateViewsWindow.TwoDimensionalViewSettingsList;
            if (twoDimensionalViewSettingsList == null || twoDimensionalViewSettingsList.Count == 0)
            {
                return;
            }

            foreach (TwoDimensionalViewSettings twoDimensionalViewSettings in twoDimensionalViewSettingsList)
            {
                TabItem tabItem = UpdateTabItem(tabControl, analyticalModel, new ModifiedEventArgs(new ViewSettingsModification(twoDimensionalViewSettings)), twoDimensionalViewSettings);
                if (tabItem != null)
                {
                    tabControl.SelectedItem = tabItem;
                }
            }

            SetUIGeometrySettings(tabControl, analyticalModel);
        }

        private void RibbonButton_OpenAnalyticalModel_Click(object sender, RoutedEventArgs e)
        {
            if (uIAnalyticalModel == null)
            {
                return;
            }

            string path = null;
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "SAM files (*.sam)|*.sam|json files (*.json)|*.json|All files (*.*)|*.*";
            openFileDialog.FilterIndex = 1;
            openFileDialog.RestoreDirectory = true;
            if (openFileDialog.ShowDialog(this) == false)
            {
                return;
            }
            path = openFileDialog.FileName;

            Open(path);
        }

        private void RibbonButton_OpenMollierChart_Click(object sender, RoutedEventArgs e)
        {
            // MollierForm is now a WPF Window (not IDisposable; ShowDialog returns bool?).
            Core.Mollier.UI.MollierForm mollierForm = new Core.Mollier.UI.MollierForm();
            mollierForm.ShowDialog();
        }

        private void RibbonButton_OpenPartL_Click(object sender, RoutedEventArgs e)
        {
            string path = Core.Tas.Query.UKBRStudio2021Path();

            if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path))
            {
                return;
            }

            System.Diagnostics.Process.Start(path);
        }

        private void RibbonButton_OpenT3D_Click(object sender, RoutedEventArgs e)
        {
            if (TryOpen(".t3d"))
            {
                return;
            }

            string path = Core.Tas.Query.TAS3DPath();
            if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path))
            {
                MessageBox.Show("TAS3D executable not found.", "Error");
                return;
            }
            System.Diagnostics.Process.Start(path);
        }

        private void RibbonButton_OpenTBD_Click(object sender, RoutedEventArgs e)
        {
            if (TryOpen(".tbd"))
            {
                return;
            }

            string path = Core.Tas.Query.TBDPath();

            if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path))
            {
                return;
            }

            System.Diagnostics.Process.Start(path);
        }

        private void RibbonButton_OpenTPD_Click(object sender, RoutedEventArgs e)
        {
            if (TryOpen(".tpd"))
            {
                return;
            }

            string path = Core.Tas.Query.TPDPath();

            if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path))
            {
                return;
            }

            System.Diagnostics.Process.Start(path);
        }

        private void RibbonButton_OpenTSD_Click(object sender, RoutedEventArgs e)
        {
            if (TryOpen(".tsd"))
            {
                return;
            }

            string path = Core.Tas.Query.TSDPath();

            if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path))
            {
                return;
            }

            System.Diagnostics.Process.Start(path);
        }

        private void RibbonButton_PrintRoomDataSheets_Click(object sender, RoutedEventArgs e)
        {
            uIAnalyticalModel?.PrintRoomDataSheets(windowHandle);
        }

        private void RibbonButton_SpaceAssumptionsPdf_Click(object sender, RoutedEventArgs e)
        {
            //The Space selected in the active view; Modify.CreateSpaceReportPdf explains none or several.
            uIAnalyticalModel?.CreateSpaceReportPdf(GetActiveViewportControl()?.SelectedSAMObjects<Space>(), SpaceReportPdf.SpaceAssumptions, this);
        }

        private void MenuItem_SpaceAssumptionsPdf_Click(object sender, RoutedEventArgs e)
        {
            uIAnalyticalModel?.CreateSpaceReportPdf(((sender as MenuItem)?.Tag as IEnumerable<Space>), SpaceReportPdf.SpaceAssumptions, this);
        }

        private void RibbonButton_SpaceDesignLoadSummaryPdf_Click(object sender, RoutedEventArgs e)
        {
            //The Space selected in the active view; Modify.CreateSpaceReportPdf explains none or several.
            uIAnalyticalModel?.CreateSpaceReportPdf(GetActiveViewportControl()?.SelectedSAMObjects<Space>(), SpaceReportPdf.SpaceDesignLoadSummary, this);
        }

        private void MenuItem_SpaceDesignLoadSummaryPdf_Click(object sender, RoutedEventArgs e)
        {
            uIAnalyticalModel?.CreateSpaceReportPdf(((sender as MenuItem)?.Tag as IEnumerable<Space>), SpaceReportPdf.SpaceDesignLoadSummary, this);
        }

        private void RibbonButton_Redo_Click(object sender, RoutedEventArgs e)
        {
            Redo();
        }

        private void RibbonButton_RemoveAirMovementObjects_Click(object sender, RoutedEventArgs e)
        {
            Modify.RemoveAirMovementObjects(uIAnalyticalModel);
        }

        private void RibbonButton_RemoveResults_Click(object sender, RoutedEventArgs e)
        {
            uIAnalyticalModel.RemoveResults();
        }

        private void RibbonButton_RevealHidden_Click(object sender, RoutedEventArgs e)
        {
            RevealHidden();
        }

        private void RibbonButton_SaveAnalyticalModel_Click(object sender, RoutedEventArgs e)
        {
            if (uIAnalyticalModel == null)
            {
                return;
            }

            AnalyticalModel analyticalModel = uIAnalyticalModel.JSAMObject;

            SetUIGeometrySettings(tabControl, analyticalModel);

            if (uIAnalyticalModel.Save())
            {
            }
        }

        private void RibbonButton_SaveAsAnalyticalModel_Click(object sender, RoutedEventArgs e)
        {
            SaveAs();
        }

        private void RibbonButton_SelectByFilter_Click(object sender, RoutedEventArgs e)
        {
            SelectByFilter();
        }

        private void RibbonButton_SelectByGuid_Click(object sender, RoutedEventArgs e)
        {
            SelectByGuid();
        }

        private void RibbonButton_SetDefaultLayers_Click(object sender, RoutedEventArgs e)
        {
            uIAnalyticalModel.SetDefaultLayers(windowHandle);
        }

        private void RibbonButton_SimulateCases_Click(object sender, RoutedEventArgs e)
        {
            Modify.SimulateCases();
        }

        private void RibbonButton_SolarSimulation_Click(object sender, RoutedEventArgs e)
        {
            uIAnalyticalModel?.SolarSimulation(windowHandle);
        }

        private void RibbonButton_Solve_Click(object sender, RoutedEventArgs e)
        {
            Modify.Solve(uIAnalyticalModel);
        }

        private void RibbonButton_SpaceDiagram_Click(object sender, RoutedEventArgs e)
        {
            uIAnalyticalModel?.SpaceDiagram(windowHandle);
        }

        private void RibbonButton_Test_Click(object sender, RoutedEventArgs e)
        {
            Test();

            //RelationClusterComplexReferenceWindow relationClusterComplexReferenceWindow = new RelationClusterComplexReferenceWindow();
            //relationClusterComplexReferenceWindow.RelationCluster = new RelationCluster(uIAnalyticalModel?.JSAMObject?.AdjacencyCluster);

            //bool? dialogResult = relationClusterComplexReferenceWindow.ShowDialog();
            //if (dialogResult == null || !dialogResult.HasValue || !dialogResult.Value)
            //{
            //    return;
            //}

            //SearchWindow searchWindow = new SearchWindow(new string[] {"absss", "Kubas", "KLUB" });
            //searchWindow.ShowDialog();
        }

        private void RibbonButton_TextMap_Click(object sender, RoutedEventArgs e)
        {
            TextMapWindow textMapWindow = new TextMapWindow();
            if (textMapWindow.ShowDialog() != true)
            {
                return;
            }
        }

        private void RibbonButton_ThermalTransmittanceCalculator_Click(object sender, RoutedEventArgs e)
        {
            Modify.ThermalTransmittanceCalculator_SingleConstruction(uIAnalyticalModel);
        }

        private void RibbonButton_Undo_Click(object sender, RoutedEventArgs e)
        {
            Undo();
        }
        
        private void RibbonButton_UpdateUKBRFile_Click(object sender, RoutedEventArgs e)
        {
            bool result = Modify.UpdateUKBRFile(uIAnalyticalModel);
        }

        private void RibbonButton_ViewGeometry_Click(object sender, RoutedEventArgs e)
        {
            GeometryWindow geometryWindow = new GeometryWindow();
            geometryWindow.Show();
        }

        private void RibbonButton_ViewRange_Click(object sender, RoutedEventArgs e)
        {
            ShowViewRange();
        }

        private void RibbonButton_ViewSettings_Click(object sender, RoutedEventArgs e)
        {
            EditViewSettings();
        }

        private void RibbonButton_Wiki_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Core.Query.StartProcess("https://github.com/SAM-BIM/SAM_UI/wiki");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open Wiki: {ex.Message}", "Error");
            }
        }

        private void SelectByApertureConstructionName()
        {
            ViewportControl viewportControl = GetActiveViewportControl();
            if (viewportControl == null)
            {
                return;
            }

            List<Aperture> apertures = viewportControl.SelectedSAMObjects<Aperture>();
            if (apertures != null && apertures.Count != 0)
            {
                AdjacencyCluster? adjacencyCluster = uIAnalyticalModel?.JSAMObject?.AdjacencyCluster;
                if (adjacencyCluster == null)
                {
                    return;
                }

                HashSet<string> apertureConstructionNames = [];
                foreach (Aperture aperture in apertures)
                {
                    if (aperture is null)
                    {
                        continue;
                    }

                    apertureConstructionNames.Add(aperture?.ApertureConstruction?.Name ?? string.Empty);
                }

                apertures = [];

                List<Aperture> apertures_All = adjacencyCluster.GetApertures();
                foreach (Aperture aperture in apertures_All)
                {
                    if (aperture == null)
                    {
                        continue;
                    }

                    if (!apertureConstructionNames.Contains(aperture?.ApertureConstruction?.Name ?? string.Empty))
                    {
                        continue;
                    }

                    apertures.Add(aperture);
                }
            }

            viewportControl.Select(apertures);
        }

        private void SelectByApertureType()
        {
            ViewportControl viewportControl = GetActiveViewportControl();
            if (viewportControl == null)
            {
                return;
            }

            List<Aperture> apertures = viewportControl.SelectedSAMObjects<Aperture>();
            if (apertures != null && apertures.Count != 0)
            {
                AdjacencyCluster? adjacencyCluster = uIAnalyticalModel?.JSAMObject?.AdjacencyCluster;
                if (adjacencyCluster == null)
                {
                    return;
                }

                HashSet<ApertureType> apertureTypes = [];
                foreach (Aperture aperture in apertures)
                {
                    if (aperture is null)
                    {
                        continue;
                    }

                    apertureTypes.Add(aperture.ApertureType);
                }

                apertures = [];

                List<Aperture> apertures_All = adjacencyCluster.GetApertures();
                foreach (Aperture aperture in apertures_All)
                {
                    if (!apertureTypes.Contains(aperture.ApertureType))
                    {
                        continue;
                    }

                    apertures.Add(aperture);
                }
            }

            viewportControl.Select(apertures);
        }

        private void SelectByConstructionName()
        {
            ViewportControl viewportControl = GetActiveViewportControl();
            if (viewportControl == null)
            {
                return;
            }

            List<Panel> panels = viewportControl.SelectedSAMObjects<Panel>();
            if (panels != null && panels.Count != 0)
            {
                AdjacencyCluster? adjacencyCluster = uIAnalyticalModel?.JSAMObject?.AdjacencyCluster;
                if (adjacencyCluster == null)
                {
                    return;
                }

                HashSet<string> constructionNames = [];
                foreach (Panel panel in panels)
                {
                    if (panel is null)
                    {
                        continue;
                    }

                    constructionNames.Add(panel?.Construction?.Name ?? string.Empty);
                }

                panels = [];

                List<Panel> panels_All = adjacencyCluster.GetPanels();
                foreach (Panel panel in panels_All)
                {
                    if (panel == null)
                    {
                        continue;
                    }

                    if (!constructionNames.Contains(panel?.Construction?.Name ?? string.Empty))
                    {
                        continue;
                    }

                    panels.Add(panel);
                }
            }

            viewportControl.Select(panels);
        }

        private void SelectByFilter()
        {
            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if (analyticalModel == null)
            {
                return;
            }

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;
            if (adjacencyCluster == null)
            {
                return;
            }

            IUIFilter uIFilter = ActiveManager.GetValue<IUIFilter>(Assembly.GetExecutingAssembly(), "UIFilter");
            if (uIFilter != null)
            {
                UI.Modify.AssignAnalyticalModel(uIFilter, analyticalModel);
            }

            List<IUIFilter> uIFilters = ActiveManager.GetValue<SAMCollection<IUIFilter>>(Assembly.GetExecutingAssembly(), "UIFilters")?.Cast<IUIFilter>().ToList();
            if (uIFilters != null && uIFilters.Count != 0)
            {
                uIFilters.ForEach(x => UI.Modify.AssignAnalyticalModel(x, analyticalModel));
            }

            List<IJSAMObject> jSAMObjects = new List<IJSAMObject>();
            ViewportControl viewportControl = GetActiveViewportControl();
            if (viewportControl != null)
            {
                List<SAMObject> sAMObjects_Selected = viewportControl.SelectedSAMObjects<SAMObject>();
                if (sAMObjects_Selected != null)
                {
                    jSAMObjects.AddRange(sAMObjects_Selected);
                }
            }

            if (jSAMObjects == null || jSAMObjects.Count == 0)
            {
                jSAMObjects = Analytical.Query.FilteringSAMObjects(adjacencyCluster);
            }

            FilterWindow filterWindow = new FilterWindow() { Types = new List<Type>() { typeof(Space), typeof(Panel), typeof(Aperture) }, Type = typeof(Space), UIFilter = uIFilter, UIFilters = uIFilters, JSAMObjects = jSAMObjects, AnalyticalModel = analyticalModel };
            filterWindow.FilterAdding += FilterWindow_FilterAdding;
            bool? result = filterWindow.ShowDialog();
            if (result == null || !result.HasValue || !result.Value)
            {
                return;
            }

            uIFilter = filterWindow.UIFilter;
            uIFilters = filterWindow.UIFilters;

            ActiveManager.SetValue(Assembly.GetExecutingAssembly(), "UIFilter", uIFilter?.Clone());
            ActiveManager.SetValue(Assembly.GetExecutingAssembly(), "UIFilters", new SAMCollection<IUIFilter>(uIFilters));
            ActiveManager.Write();

            List<IJSAMObject> jSAMObjects_Filtered = filterWindow.FilteredJSAMObjects;

            viewportControl?.Select(jSAMObjects_Filtered?.FindAll(x => x is SAMObject)?.Cast<SAMObject>());
        }

        private void SelectByGuid()
        {
            string value = null;

            ViewportControl viewportControl = GetActiveViewportControl();
            if (viewportControl == null)
            {
                return;
            }

            List<SAMObject> sAMObjects_Selected = viewportControl.SelectedSAMObjects<SAMObject>();

            AdjacencyCluster adjacencyCluster = uIAnalyticalModel?.JSAMObject?.AdjacencyCluster;
            if (adjacencyCluster == null)
            {
                return;
            }

            SAM.Core.UI.WPF.TextBoxWindow textBoxWindow = new SAM.Core.UI.WPF.TextBoxWindow("Select By Guid", "Insert Guids") { Owner = this };
            if (textBoxWindow.ShowDialog() != true)
            {
                return;
            }
            value = textBoxWindow.Value;

            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            List<Guid> guids = null;
            if (!Core.Query.TryParseGuids(value, out guids) || guids == null || guids.Count == 0)
            {
                return;
            }

            List<SAMObject> sAMObjects = new List<SAMObject>();
            foreach (Guid guid in guids)
            {
                SAMObject sAMObject = null;
                if (sAMObjects_Selected != null && sAMObjects_Selected.Count != 0)
                {
                    sAMObject = sAMObjects_Selected.Find(x => x.Guid == guid);
                }
                else
                {
                    sAMObject = viewportControl.GetSAMObject<SAMObject>(guid);
                }

                if (sAMObject == null)
                {
                    continue;
                }

                sAMObjects.Add(sAMObject);
            }

            viewportControl.Select(sAMObjects);
        }

        private void SelectByPanelGroup()
        {
            ViewportControl viewportControl = GetActiveViewportControl();
            if (viewportControl == null)
            {
                return;
            }

            List<Panel> panels = viewportControl.SelectedSAMObjects<Panel>();
            if (panels != null && panels.Count != 0)
            {
                AdjacencyCluster? adjacencyCluster = uIAnalyticalModel?.JSAMObject?.AdjacencyCluster;
                if (adjacencyCluster == null)
                {
                    return;
                }

                HashSet<PanelGroup> panelGroups = [];
                foreach (Panel panel in panels)
                {
                    if (panel is null)
                    {
                        continue;
                    }

                    panelGroups.Add(panel.PanelGroup);
                }

                panels = [];

                List<Panel> panels_All = adjacencyCluster.GetPanels();
                foreach (Panel panel in panels_All)
                {
                    if (!panelGroups.Contains(panel.PanelGroup))
                    {
                        continue;
                    }

                    panels.Add(panel);
                }
            }

            viewportControl.Select(panels);
        }

        private void SelectByPanelType()
        {
            ViewportControl viewportControl = GetActiveViewportControl();
            if (viewportControl == null)
            {
                return;
            }

            List<Panel> panels = viewportControl.SelectedSAMObjects<Panel>();
            if (panels != null && panels.Count != 0)
            {
                AdjacencyCluster? adjacencyCluster = uIAnalyticalModel?.JSAMObject?.AdjacencyCluster;
                if (adjacencyCluster == null)
                {
                    return;
                }

                HashSet<PanelType> panelTypes = [];
                foreach (Panel panel in panels)
                {
                    if (panel is null)
                    {
                        continue;
                    }

                    panelTypes.Add(panel.PanelType);
                }

                panels = [];

                List<Panel> panels_All = adjacencyCluster.GetPanels();
                foreach (Panel panel in panels_All)
                {
                    if (!panelTypes.Contains(panel.PanelType))
                    {
                        continue;
                    }

                    panels.Add(panel);
                }
            }

            viewportControl.Select(panels);
        }

        private void SetActiveGuid()
        {
            Guid guid = GetActiveGuid();
            if (guid == Guid.Empty)
            {
                return;
            }

            viewportControl = GetActiveViewportControl();
            viewportControl.Focus();

            uIAnalyticalModel.Modified -= UIAnalyticalModel_Modified;
            Modify.SetActiveGuid(uIAnalyticalModel, guid);
            uIAnalyticalModel.Modified += UIAnalyticalModel_Modified;
        }

        private void SetDefaultViewSettings()
        {
            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if (analyticalModel == null)
            {
                return;
            }

            if (!analyticalModel.TryGetValue(AnalyticalModelParameter.UIGeometrySettings, out UIGeometrySettings uIGeometrySettings) || uIGeometrySettings == null)
            {
                uIGeometrySettings = new UIGeometrySettings();
            }

            List<IViewSettings> viewSettingsList = uIGeometrySettings.GetViewSettings<IViewSettings>();
            if (viewSettingsList == null || viewSettingsList.Count == 0)
            {
                ViewSettings viewSettings = UI.Query.DefaultViewSettings(analyticalModel);
                uIGeometrySettings.AddViewSettings(viewSettings);
                uIGeometrySettings.ActiveGuid = viewSettings.Guid;
                analyticalModel.SetValue(AnalyticalModelParameter.UIGeometrySettings, uIGeometrySettings);
                uIAnalyticalModel.Modified -= UIAnalyticalModel_Modified;
                uIAnalyticalModel.JSAMObject = analyticalModel;
                uIAnalyticalModel.Modified += UIAnalyticalModel_Modified;
            }
        }

        private void SetEnabled()
        {
            RibbonButton_OpenMollierChart.IsEnabled = false;
            RibbonButton_SpaceDiagram.IsEnabled = false;
            RibbonButton_AirHandlingUnitDiagram.IsEnabled = false;
            RibbonButton_Wiki.IsEnabled = false;
            RibbonButton_PrintRoomDataSheets.IsEnabled = false;
            RibbonButton_SpaceAssumptionsPdf.IsEnabled = false;
            RibbonButton_SpaceDesignLoadSummaryPdf.IsEnabled = false;
            RibbonButton_AddMissingObjects.IsEnabled = false;
            RibbonButton_CleanAnalyticalModel.IsEnabled = false;
            RibbonButton_Hydra.IsEnabled = false;
            RibbonButton_OpenTPD.IsEnabled = false;
            RibbonButton_OpenTSD.IsEnabled = false;
            RibbonButton_OpenTBD.IsEnabled = false;
            RibbonButton_OpenT3D.IsEnabled = false;
            RibbonButton_EditLibrary.IsEnabled = false;
            RibbonButton_EnergySimulation.IsEnabled = false;
            RibbonButton_SolarSimulation.IsEnabled = false;
            RibbonButton_ImportWeatherData.IsEnabled = false;
            RibbonButton_EditWeatherData.IsEnabled = false;
            RibbonButton_EditApertureConstructions.IsEnabled = false;
            RibbonButton_EditConstructions.IsEnabled = false;
            RibbonButton_EditSpaces.IsEnabled = false;
            RibbonButton_EditProfileLibrary.IsEnabled = false;
            RibbonButton_EditInternalConditionLibrary.IsEnabled = false;
            RibbonButton_EditMaterialLibrary.IsEnabled = false;
            RibbonButton_AnalyticalModelCheck.IsEnabled = false;
            RibbonButton_ImportObjects.IsEnabled = false;
            RibbonButton_Hydra.IsEnabled = false;
            RibbonButton_OpenT3D.IsEnabled = false;
            RibbonButton_OpenTBD.IsEnabled = false;
            RibbonButton_OpenTPD.IsEnabled = false;
            RibbonButton_OpenTSD.IsEnabled = false;
            //RibbonButton_CreateTBD.IsEnabled = false;
            RibbonButton_EditLibrary.IsEnabled = false;
            RibbonButton_EnergySimulation.IsEnabled = false;
            RibbonButton_SolarSimulation.IsEnabled = false;
            RibbonButton_ImportWeatherData.IsEnabled = false;
            RibbonButton_EditWeatherData.IsEnabled = false;
            RibbonButton_EditApertureConstructions.IsEnabled = false;
            RibbonButton_EditConstructions.IsEnabled = false;
            RibbonButton_EditSpaces.IsEnabled = false;
            RibbonButton_EditProfileLibrary.IsEnabled = false;
            RibbonButton_EditInternalConditionLibrary.IsEnabled = false;
            RibbonButton_EditMaterialLibrary.IsEnabled = false;
            RibbonButton_AnalyticalModelCheck.IsEnabled = false;
            RibbonButton_ImportObjects.IsEnabled = false;
            RibbonButton_AnalyticalModelProperties.IsEnabled = false;
            RibbonButton_AnalyticalModelLocation.IsEnabled = false;
            RibbonButton_CloseAnalyticalModel.IsEnabled = false;
            RibbonButton_SaveAsAnalyticalModel.IsEnabled = false;
            RibbonButton_SaveAnalyticalModel.IsEnabled = false;
            RibbonButton_NewAnalyticalModel.IsEnabled = false;
            RibbonButton_OpenAnalyticalModel.IsEnabled = false;
            RibbonButton_MapInternalConditions.IsEnabled = false;
            RibbonButton_MapInternalConditionsByTM59.IsEnabled = false;
            RibbonButton_ExportAnalyticalModel.IsEnabled = false;
            RibbonButton_RemoveResults.IsEnabled = false;
            RibbonButton_EditInternalConditions.IsEnabled = false;
            RibbonButton_AssignMechanicalSystems.IsEnabled = false;
            RibbonButton_RemoveAirMovementObjects.IsEnabled = false;
            RibbonButton_PartOWorkflow.IsEnabled = false;
            RibbonButton_PreparePartOIteration.IsEnabled = false;
            RibbonButton_AssessPartOTM59.IsEnabled = false;
            RibbonButton_OptimisePartOTM59.IsEnabled = false;

            RibbonButton_OpenMollierChart.IsEnabled = true;
            RibbonButton_Wiki.IsEnabled = true;
            RibbonButton_Hydra.IsEnabled = true;
            RibbonButton_OpenTPD.IsEnabled = true;
            RibbonButton_OpenTSD.IsEnabled = true;
            RibbonButton_OpenTBD.IsEnabled = true;
            RibbonButton_OpenT3D.IsEnabled = true;
            RibbonButton_NewAnalyticalModel.IsEnabled = true;
            RibbonButton_OpenAnalyticalModel.IsEnabled = true;
            RibbonButton_EditLibrary.IsEnabled = true;


            AnalyticalModel analyticalModel = uIAnalyticalModel.JSAMObject;
            if (analyticalModel != null)
            {
                RibbonButton_PrintRoomDataSheets.IsEnabled = true;
                RibbonButton_SpaceAssumptionsPdf.IsEnabled = true;
                RibbonButton_SpaceDesignLoadSummaryPdf.IsEnabled = true;
                RibbonButton_AddMissingObjects.IsEnabled = true;
                RibbonButton_CleanAnalyticalModel.IsEnabled = true;
                RibbonButton_MapInternalConditions.IsEnabled = true;
                RibbonButton_MapInternalConditionsByTM59.IsEnabled = true;
                RibbonButton_EnergySimulation.IsEnabled = true;
                RibbonButton_SolarSimulation.IsEnabled = true;
                RibbonButton_ImportWeatherData.IsEnabled = true;
                RibbonButton_EditWeatherData.IsEnabled = true;
                RibbonButton_EditApertureConstructions.IsEnabled = true;
                RibbonButton_EditConstructions.IsEnabled = true;
                RibbonButton_EditSpaces.IsEnabled = true;
                RibbonButton_EditProfileLibrary.IsEnabled = true;
                RibbonButton_EditInternalConditionLibrary.IsEnabled = true;
                RibbonButton_EditMaterialLibrary.IsEnabled = true;
                RibbonButton_AnalyticalModelCheck.IsEnabled = true;
                RibbonButton_ImportObjects.IsEnabled = true;
                RibbonButton_AnalyticalModelProperties.IsEnabled = true;
                RibbonButton_AnalyticalModelLocation.IsEnabled = true;
                RibbonButton_CloseAnalyticalModel.IsEnabled = true;
                RibbonButton_SaveAsAnalyticalModel.IsEnabled = true;
                RibbonButton_SaveAnalyticalModel.IsEnabled = true;
                RibbonButton_ExportAnalyticalModel.IsEnabled = true;
                //RibbonButton_CreateTBD.IsEnabled = true;
                RibbonButton_RemoveResults.IsEnabled = true;
                RibbonButton_EditInternalConditions.IsEnabled = true;
                RibbonButton_AssignMechanicalSystems.IsEnabled = true;
                RibbonButton_RemoveAirMovementObjects.IsEnabled = true;

                //Preparing needs only a model; assessing needs a completed run, which is a fact about the
                //session rather than about the model, so it is gated separately.
                RibbonButton_PartOWorkflow.IsEnabled = true;
                RibbonButton_PreparePartOIteration.IsEnabled = true;
                RefreshPartOButtons();

                List<AirHandlingUnit> airHandlingUnits = analyticalModel.AdjacencyCluster?.GetObjects<AirHandlingUnit>();
                if (airHandlingUnits != null && airHandlingUnits.Count != 0)
                {
                    RibbonButton_SpaceDiagram.IsEnabled = true;
                    RibbonButton_AirHandlingUnitDiagram.IsEnabled = true;
                }
            }
        }

        private void SetUIGeometrySettings(TabControl tabControl, AnalyticalModel? analyticalModel)
        {
            UIGeometrySettings uIGeometrySettings = UpdateUIGeometrySettings(tabControl, analyticalModel, new ModifiedEventArgs());

            uIAnalyticalModel.Modified -= UIAnalyticalModel_Modified;
            // Syncing the current viewport/camera state back into the model (e.g. before a save) is a
            // transient writeback, not a user edit, so skip the undo snapshot - otherwise saving pushes
            // a spurious history entry and clears redo.
            uIAnalyticalModel.SetJSAMObject(analyticalModel, new ViewSettingsModification(uIGeometrySettings.GetViewSettings<IViewSettings>()), false);
            uIAnalyticalModel.Modified += UIAnalyticalModel_Modified;
        }

        private void ShowProperties()
        {
            ViewportControl viewportControl = GetActiveViewportControl();
            if (viewportControl == null)
            {
                return;
            }

            List<SAMObject> jSAMObjects = viewportControl.SelectedSAMObjects<SAMObject>();
            if (jSAMObjects is null || jSAMObjects.Count != 1)
            {
                return;
            }

            SAMObject sAMObject = jSAMObjects[0];

            if (sAMObject is Panel)
            {
                Panel panel = (Panel)sAMObject;
                uIAnalyticalModel.EditPanel(panel, windowHandle);
            }
            else if (sAMObject is Space)
            {
                Space space = (Space)sAMObject;
                uIAnalyticalModel.EditSpace(space, windowHandle);
            }
            else if (sAMObject is Aperture)
            {
                Aperture aperture = (Aperture)sAMObject;
                uIAnalyticalModel.EditAperture(aperture, windowHandle);
            }
        }

        private void ShowViewRange()
        {
            if (doubleRangeWindow != null)
            {
                doubleRangeWindow.Close();
                return;
            }

            double max = 0;
            double min = 0;
            Range<double> range = uIAnalyticalModel?.JSAMObject?.GetElevationRange();
            if (range != null)
            {
                max = range.Max;
                min = range.Min;
            }

            doubleRangeWindow = new DoubleRangeWindow(max, min);
            doubleRangeWindow.ShowActivated = true;
            doubleRangeWindow.Topmost = true;
            doubleRangeWindow.RangeChanged += DoubleRangeWindow_RangeChanged;
            doubleRangeWindow.Closing += DoubleRangeWindow_Closing;

            RefreshDoubleRangeWindow();

            doubleRangeWindow.Show();
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Guid guid = GetActiveGuid();
            if (guid == Guid.Empty)
            {
                return;
            }

            uIAnalyticalModel.Modified -= UIAnalyticalModel_Modified;
            Modify.SetActiveGuid(uIAnalyticalModel, guid);
            uIAnalyticalModel.Modified += UIAnalyticalModel_Modified;

            RegenerateIfDirty(guid);
        }

        private void TabItem_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            TabItem tabItem = sender as TabItem;
            if (tabItem == null)
            {
                return;
            }

            ContextMenu contextMenu = tabItem.ContextMenu;
            if (contextMenu == null)
            {
                contextMenu = new ContextMenu();
                tabItem.ContextMenu = contextMenu;
            }

            contextMenu.Tag = tabItem;

            contextMenu.Items.Clear();

            MenuItem menuItem = null;

            menuItem = new MenuItem();
            menuItem.Name = "MenuItem_Close_TabItem";
            menuItem.Header = "Close";
            menuItem.Click += MenuItem_Close_TabItem_Click;
            contextMenu.Items.Add(menuItem);

            menuItem = new MenuItem();
            menuItem.Name = "MenuItem_CloseAllButThis_TabItem";
            menuItem.Header = "Close All But This";
            menuItem.Click += MenuItem_CloseAllButThis_TabItem_Click;
            contextMenu.Items.Add(menuItem);

            menuItem = new MenuItem();
            menuItem.Name = "MenuItem_CloseSelected_TabItem";
            menuItem.Header = "Close Selected";
            menuItem.Click += MenuItem_CloseSelected_TabItem_Click;
            contextMenu.Items.Add(menuItem);

            menuItem = new MenuItem();
            menuItem.Name = "MenuItem_Duplicate_TabItem";
            menuItem.Header = "Duplicate";
            menuItem.Click += MenuItem_Duplicate_TabItem_Click;
            contextMenu.Items.Add(menuItem);

            menuItem = new MenuItem();
            menuItem.Name = "MenuItem_LoadView_TabItem";
            menuItem.Header = "Load view";
            menuItem.Click += MenuItem_LoadView_Click;
            contextMenu.Items.Add(menuItem);

            menuItem = new MenuItem();
            menuItem.Name = "MenuItem_LoadCamera_TabItem";
            menuItem.Header = "Load camera";
            menuItem.Click += MenuItem_LoadCamera_Click;
            contextMenu.Items.Add(menuItem);

            menuItem = new MenuItem();
            menuItem.Name = "MenuItem_Settings_TabItem";
            menuItem.Header = "Settings";
            menuItem.Click += MenuItem_Settings_TabItem_Click;
            contextMenu.Items.Add(menuItem);

            contextMenu.IsOpen = true;
        }

        private void TabItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            TabItem? tabItem = sender as TabItem;
            if (tabItem == null)
            {
                return;
            }

            // Only a double-click on the tab header itself should open View Settings. A double-click on an
            // object inside the viewport opens that object's properties; that same double-click bubbles up
            // to the TabItem, so without this guard View Settings would also open the moment the properties
            // dialog is dismissed.
            if (tabItem.Content is DependencyObject content && e.OriginalSource is DependencyObject source && IsDescendantOf(source, content))
            {
                return;
            }

            EditViewSettings(tabItem);
        }

        private void Test()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            bool? dialogResult = openFileDialog.ShowDialog(this);

            if (dialogResult == null || !dialogResult.Value)
            {
                return;
            }

            string path = openFileDialog.FileName;
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            IJSAMObject? jSAMObject = Core.Convert.ToSAM<IJSAMObject>(path)?.FirstOrDefault();

        }

        private bool TryOpen(string? extension)
        {
            if (string.IsNullOrWhiteSpace(extension))
            {
                return false;
            }

            string? outputDirectory = System.IO.Path.GetDirectoryName(uIAnalyticalModel?.Path);
            string? projectName = uIAnalyticalModel?.JSAMObject?.Name;

            if (string.IsNullOrWhiteSpace(outputDirectory) || string.IsNullOrWhiteSpace(projectName) || !System.IO.Directory.Exists(outputDirectory))
            {
                return false;
            }

            string path = System.IO.Path.Combine(outputDirectory, projectName + extension);
            if (System.IO.File.Exists(path))
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }
        
        // In-place refresh of space colors + legend for attribute-only edits (see AttributeModification).
        // Returns false when the in-place result could differ from a full regeneration; the caller then
        // falls back to the regular ToSAM_GeometryObjectModel path.
        private bool TryRefreshSpaceAppearances(ViewportControl viewportControl, AnalyticalModel analyticalModel, IViewSettings viewSettings, string name)
        {
            TwoDimensionalViewSettings twoDimensionalViewSettings = viewSettings as TwoDimensionalViewSettings;
            ThreeDimensionalViewSettings threeDimensionalViewSettings = viewSettings as ThreeDimensionalViewSettings;
            if (twoDimensionalViewSettings == null && threeDimensionalViewSettings == null)
            {
                return false;
            }

            // The 3D in-place re-skin needs a renderer that can recolor per object (the SharpDX path);
            // the legacy Helix 3D renderer has no in-place re-skin, so 3D edits there regenerate as before
            // (issue #32). 2D always supports it.
            if (!viewportControl.SupportsInPlaceAppearanceRefresh)
            {
                return false;
            }

            GeometryObjectModel geometryObjectModel = viewportControl.UIGeometryObjectModel?.JSAMObject;
            if (geometryObjectModel == null)
            {
                return false;
            }

            using (Core.UI.PerformanceLog.Measure("AnalyticalWindow.ViewRegeneration.AttributeRefresh", string.Format("{0} [{1}]", name, viewSettings.GetType().Name)))
            {
                List<SAMObject> sAMObjects = viewportControl.SelectedSAMObjects<SAMObject>();

                HashSet<Guid> spaceGuids;
                bool refreshed = twoDimensionalViewSettings != null
                    ? UI.Modify.TryRefreshSpaceAppearances(geometryObjectModel, analyticalModel, twoDimensionalViewSettings, out spaceGuids)
                    : UI.Modify.TryRefreshSpaceAppearances(geometryObjectModel, analyticalModel, threeDimensionalViewSettings, out spaceGuids);

                if (!refreshed)
                {
                    return false;
                }

                if (!viewportControl.RefreshAppearances(spaceGuids))
                {
                    return false;
                }

                // Re-skinning replaces the Model3Ds selection visuals were painted on - re-apply the
                // selection, mirroring what the full regeneration path does after a scene rebuild.
                if (sAMObjects != null && sAMObjects.Count != 0)
                {
                    viewportControl.Select(sAMObjects);
                }
            }

            return true;
        }

        private void UIAnalyticalModel_Closed(object sender, ClosedEventArgs e)
        {
            //Cleared rather than invalidated: the run did not go stale, it stopped applying. Nothing to
            //explain, so no reason is retained.
            partORun.Reset();

            Reload(e);
            RefreshHistoryButtons();

            Title = titlePrefix;
        }

        private void UIAnalyticalModel_Modified(object sender, ModifiedEventArgs e)
        {
            //Every model replacement passes through here - an edit, an import, an undo, a redo, a simulation.
            //A Part O command arms the run first so its own write is recognised; anything else drops the run,
            //which is what stops one preparation's overheating scenarios being paired with another run's
            //results. See PartORun.
            //
            //...but only where the model actually CHANGED. SAM keeps view settings on the model, so hiding a
            //space, isolating one, activating a saved view, editing appearances or the legend, moving a
            //section plane and switching the active view all arrive here as replacements while leaving every
            //space, panel, aperture, airflow, zone and overheating scenario exactly as prepared. Dropping the
            //run for those made the expert path - Prepare Iteration, look at what was prepared, then Energy
            //Simulation - lose the run to the act of looking, silently, and told the user the model had
            //changed when it had not. Query.IsModelChange is where the two are told apart, and it answers
            //"changed" for anything it cannot prove is presentation-only.
            partORun.NotifyModified(UI.Query.IsModelChange(e?.Modifications));

            Reload(e);
            RefreshHistoryButtons();
        }

        private void UIAnalyticalModel_Opened(object sender, OpenedEventArgs e)
        {
            //A different model. Whatever was pending belonged to the previous one.
            partORun.Reset();

            SetDefaultViewSettings();
            Reload(e);

            //... and where the model just opened records the results it was produced from, reconnect the run
            //to them: the Overheating command can then review that run's TM59 assessment straight away, with
            //no new simulation. A model recording none - or whose results no longer validate against the
            //record - keeps the guidance the tooltip already had; a failed validation puts its reason there.
            //See PartORun.Restore. After Reload deliberately: the run is settled only once the model is.
            partORun.Restore(uIAnalyticalModel?.JSAMObject, uIAnalyticalModel?.Path, out string _);
            RefreshPartOButtons();

            // A freshly opened model starts with empty history - drop any entry created by the
            // open-time view-settings setup above.
            uIAnalyticalModel?.ClearHistory();
            RefreshHistoryButtons();

            Title = titlePrefix;

            string name = uIAnalyticalModel?.JSAMObject?.Name;
            if (name != null)
            {
                Title += string.Format(" [{0}]", name);
            }
        }

        private void Undo()
        {
            uIAnalyticalModel?.Undo();
        }

        private TabItem UpdateTabItem(TabControl tabControl, AnalyticalModel analyticalModel, ModifiedEventArgs modifiedEventArgs, IViewSettings viewSettings = null, bool active = true)
        {
            if (tabControl == null || analyticalModel == null)
            {
                return null;
            }

            if (viewSettings == null)
            {
                viewSettings = UI.Query.DefaultViewSettings(analyticalModel);
            }

            TabItem tabItem = null;
            foreach (TabItem tabItem_Temp in tabControl.Items)
            {
                ViewportControl viewportControl_Temp = tabItem_Temp.Content as ViewportControl;
                if (viewportControl_Temp != null)
                {
                    if (viewportControl_Temp.Guid == viewSettings.Guid)
                    {
                        tabItem = tabItem_Temp;
                    }
                }
            }

            if (tabItem == null)
            {
                tabItem = new TabItem() { Header = "???" };
                tabItem.ContextMenuOpening += TabItem_ContextMenuOpening;
                tabItem.MouseDoubleClick += TabItem_MouseDoubleClick;
                tabControl.Items.Add(tabItem);
            }

            ViewportControl viewportControl = tabItem.Content as ViewportControl;
            if (viewportControl == null)
            {
                viewportControl = new ViewportControl();
                viewportControl.Loaded += ViewportControl_Loaded;
                viewportControl.ObjectHoovered += ViewportControl_ObjectHoovered;
                viewportControl.ObjectDoubleClicked += ViewportControl_ObjectDoubleClicked;
                viewportControl.ObjectContextMenuOpening += ViewControl_ObjectContextMenuOpening;
                viewportControl.ObjectSelectionChanged += ViewportControl_ObjectSelectionChanged;
                viewportControl.UndefinedLegendItem = UI.Query.UndefinedLegendItem();
                tabItem.Content = viewportControl;

                if (viewSettings != null)
                {
                    viewportControl.Guid = viewSettings.Guid;
                }
            }

            string name = viewSettings.Name;
            if (string.IsNullOrEmpty(name))
            {
                name = viewSettings.DefaultName();
            }

            if (!string.IsNullOrEmpty(name) && tabItem.Header?.ToString() != name)
            {
                tabItem.Header = name;
            }

            UIGeometryObjectModel uIGeometryObjectModel = viewportControl.UIGeometryObjectModel;

            bool updateGeometry = uIGeometryObjectModel?.JSAMObject == null || modifiedEventArgs.Modifications.Find(x => x is FullModification) != null;

            List<ViewSettingsModification> viewSettingsModifications = modifiedEventArgs.GetModifications<ViewSettingsModification>((x) => x.ViewSettings?.Find(y => y.Guid == viewSettings.Guid) != null);
            if (!updateGeometry)
            {
                if(viewSettingsModifications != null && viewSettingsModifications.Count != 0)
                {
                    if(viewSettingsModifications.Any(x => x.UpdateCamera))
                    {
                        if (viewSettings is TwoDimensionalViewSettings twoDimensionalViewSettings)
                        {
                            viewportControl.Camera = twoDimensionalViewSettings.Camera;
                        }
                        else if (viewSettings is ThreeDimensionalViewSettings threeDimensionalViewSettings)
                        {
                            viewportControl.Camera = threeDimensionalViewSettings.Camera;
                        }
                    }

                    // A view-settings change flagged to update geometry (e.g. "Load view", which copies
                    // the whole source view settings) regenerates the scene as well as applying the camera.
                    if (viewSettingsModifications.Any(x => x.UpdateGeometry))
                    {
                        updateGeometry = true;
                    }
                }
            }

            if (!updateGeometry)
            {
                List<AnalyticalModelModification> analyticalModelModifications = modifiedEventArgs.GetModifications<AnalyticalModelModification>();
                HashSet<Guid> guids = analyticalModelModifications?.Guids();

                // A camera-only view-settings change ("Load camera") carries no geometry change, so don't
                // let the catch-all below force a full regeneration. This guard is precise: it only applies
                // when every matching view-settings modification is camera-only (UpdateCamera, not
                // UpdateGeometry) and there are no model edits to react to - the general view-settings edit
                // path (single-arg ViewSettingsModification, UpdateCamera false) still regenerates as before.
                bool cameraOnlyViewSettingsUpdate = viewSettingsModifications != null && viewSettingsModifications.Count != 0
                    && viewSettingsModifications.TrueForAll(x => x.UpdateCamera && !x.UpdateGeometry)
                    && (analyticalModelModifications == null || analyticalModelModifications.Count == 0);

                if (!cameraOnlyViewSettingsUpdate && (guids == null || guids.Count == 0 || viewportControl.ContainsAny<SAMObject>(guids)))
                {
                    updateGeometry = true;

                    // Attribute-only fast path (#11): when every model modification is an AttributeModification
                    // (attribute edits that cannot change geometry, visibility or label text - e.g. assigning an
                    // InternalCondition), the view only needs new space fill colors and a refreshed legend.
                    // Update those in place instead of regenerating sections, labels and the scene.
                    if (guids != null && guids.Count != 0
                        && analyticalModelModifications.TrueForAll(x => x is AttributeModification)
                        && TryRefreshSpaceAppearances(viewportControl, analyticalModel, viewSettings, name))
                    {
                        updateGeometry = false;
                    }
                }
            }

            if (updateGeometry && !active && uIGeometryObjectModel?.JSAMObject != null)
            {
                // Defer regeneration of this already-populated background tab until it is activated.
                // Keeps edit latency independent of the number of open view tabs (the tab keeps showing
                // its previous content meanwhile). A never-loaded tab is still generated eagerly below.
                dirtyViewGuids.Add(viewSettings.Guid);
                updateGeometry = false;
            }

            GeometryObjectModel geometryObjectModel_Updated = null;

            if (updateGeometry)
            {
                dirtyViewGuids.Remove(viewSettings.Guid);

                if (progressBarWindowManager != null)
                {
                    progressBarWindowManager.Text = string.Format("View Regeneration [{0}]", string.IsNullOrWhiteSpace(name) ? "???" : name);
                }

                List<SAMObject> sAMObjects = viewportControl.SelectedSAMObjects<SAMObject>();

                GeometryObjectModel geometryObjectModel;
                using (Core.UI.PerformanceLog.Measure("AnalyticalWindow.ViewRegeneration.GeometryObjectModel", string.Format("{0} [{1}]", name, viewSettings.GetType().Name)))
                {
                    geometryObjectModel = analyticalModel.ToSAM_GeometryObjectModel(viewSettings);
                }

                //Kept for the Part F annotation below, which reads the text this geometry carries so it can
                //keep its tags off the room names. Null where the view was not regenerated this time.
                geometryObjectModel_Updated = geometryObjectModel;

                using (Core.UI.PerformanceLog.Measure("AnalyticalWindow.ViewRegeneration.Viewport", string.Format("{0} [{1}]", name, viewSettings.GetType().Name)))
                {
                    viewportControl.UIGeometryObjectModel = new UIGeometryObjectModel(geometryObjectModel);
                }

                viewportControl.Select(sAMObjects);
            }

            Mode mode = viewSettings.Mode();
            if (viewportControl.Mode != mode)
            {
                viewportControl.Mode = mode;
            }

            //Part F annotation on the normal saved view, after the mode is set so the 2D plan this draws on
            //exists, and after the geometry so the room names it must keep clear of are there to measure.
            //A view without the Part F parameter takes this branch too, and it removes any annotation the
            //view used to carry. See AnalyticalWindow.PartF.cs.
            UpdatePartFAirflow(viewportControl, analyticalModel, viewSettings, geometryObjectModel_Updated);

            //Ventilation Design overlay - the model's current design airflow, read independently of Part F.
            //Called after Part F above so its already-solved tags can be read as obstacles. See
            //AnalyticalWindow.VentilationDesign.cs.
            UpdateVentilationDesignAirflow(viewportControl, analyticalModel, viewSettings, geometryObjectModel_Updated);

            if (viewSettings != null)
            {
                if (!analyticalModel.TryGetValue(AnalyticalModelParameter.UIGeometrySettings, out UIGeometrySettings uIGeometrySettings) || uIGeometrySettings == null)
                {
                    uIGeometrySettings = new UIGeometrySettings();
                }

                uIGeometrySettings.AddViewSettings(viewSettings);
                analyticalModel.SetValue(AnalyticalModelParameter.UIGeometrySettings, uIGeometrySettings);
            }

            return tabItem;
        }
        
        private List<TabItem> UpdateTabItems(TabControl tabControl, AnalyticalModel analyticalModel, ModifiedEventArgs modifiedEventArgs)
        {
            if (tabControl == null)
            {
                return null;
            }

            List<TabItem> result = new List<TabItem>();

            if (analyticalModel != null && analyticalModel.TryGetValue(AnalyticalModelParameter.UIGeometrySettings, out UIGeometrySettings uIGeometrySettings) && uIGeometrySettings != null)
            {
                List<IViewSettings> viewSettingsList = uIGeometrySettings.GetViewSettings<IViewSettings>();
                if (viewSettingsList != null)
                {
                    // Only the active tab regenerates its geometry now; others are deferred (see UpdateTabItem).
                    // Fall back to the selected tab when ActiveGuid is empty or points to a view that is not
                    // rendered here (disabled/removed - those paths do not clear ActiveGuid); otherwise every
                    // tab would be deferred. A final reconciliation below guarantees the visible tab is fresh.
                    Guid activeGuid = uIGeometrySettings.ActiveGuid;
                    bool activeGuidRendered = activeGuid != Guid.Empty && viewSettingsList.Any(x => x != null && x.Guid == activeGuid && !(x is ViewSettings viewSettings_Active && !viewSettings_Active.Enabled));
                    if (!activeGuidRendered)
                    {
                        activeGuid = (tabControl.SelectedItem as TabItem)?.Content is ViewportControl viewportControl_Active ? viewportControl_Active.Guid : Guid.Empty;
                    }

                    foreach (IViewSettings viewSettings in viewSettingsList)
                    {
                        if (viewSettings is ViewSettings)
                        {
                            if (!((ViewSettings)viewSettings).Enabled)
                            {
                                continue;
                            }
                        }

                        TabItem tabItem = UpdateTabItem(tabControl, analyticalModel, modifiedEventArgs, viewSettings, viewSettings.Guid == activeGuid);
                        if (tabItem != null)
                        {
                            result.Add(tabItem);
                        }

                        if (viewSettings.Guid == uIGeometrySettings.ActiveGuid)
                        {
                            tabControl.SelectedItem = tabItem;
                        }
                    }
                }
            }

            for (int i = tabControl.Items.Count - 1; i >= 0; i--)
            {
                if (result.Contains(tabControl.Items[i] as TabItem))
                {
                    continue;
                }

                tabControl.Items.RemoveAt(i);
            }

            // Reconcile: the tab that is actually visible must never be left stale. This covers the case
            // where the saved active view was removed/disabled and WPF selected a deferred tab during the
            // removal above. Runs inside Reload (event handlers already detached), so no extra plumbing.
            if (tabControl.SelectedItem is TabItem selectedTabItem && selectedTabItem.Content is ViewportControl selectedViewportControl && dirtyViewGuids.Contains(selectedViewportControl.Guid)
                && analyticalModel != null && analyticalModel.TryGetValue(AnalyticalModelParameter.UIGeometrySettings, out UIGeometrySettings uIGeometrySettings_Selected) && uIGeometrySettings_Selected != null)
            {
                IViewSettings viewSettings_Selected = uIGeometrySettings_Selected.GetViewSettings<IViewSettings>()?.Find(x => x != null && x.Guid == selectedViewportControl.Guid);
                if (viewSettings_Selected != null)
                {
                    UpdateTabItem(tabControl, analyticalModel, modifiedEventArgs, viewSettings_Selected, true);
                }
            }

            return result;
        }

        private UIGeometrySettings? UpdateUIGeometrySettings(TabControl tabControl, AnalyticalModel? analyticalModel, ModifiedEventArgs modifiedEventArgs)
        {
            if (analyticalModel == null || tabControl == null)
            {
                return null;
            }

            if (!analyticalModel.TryGetValue(AnalyticalModelParameter.UIGeometrySettings, out UIGeometrySettings? result) || result == null)
            {
                result = new UIGeometrySettings();
            }

            List<IViewSettings> viewSettingsList = new List<IViewSettings>();
            Guid guid = Guid.Empty;
            for (int i = 0; i < tabControl.Items.Count; i++)
            {
                TabItem? tabItem = tabControl.Items[i] as TabItem;

                ViewportControl? viewportControl = tabItem?.Content as ViewportControl;
                if (viewportControl == null)
                {
                    continue;
                }

                GeometryObjectModel? geometryObjectModel = viewportControl.UIGeometryObjectModel?.JSAMObject;
                if (geometryObjectModel == null)
                {
                    return null;
                }

                if (!geometryObjectModel.TryGetValue(GeometryObjectModelParameter.ViewSettings, out IViewSettings viewSettings) || viewSettings == null)
                {
                    continue;
                }

                // Skip tabs with deferred (stale) geometry: their current view settings were already
                // applied by UpdateTabItem, so reading the stale stored copy back here would revert the edit.
                if (dirtyViewGuids.Contains(viewSettings.Guid))
                {
                    continue;
                }

                if (viewSettings is ViewSettings)
                {
                    ViewSettings viewSettings_Temp = (ViewSettings)viewSettings;
                    if (modifiedEventArgs is OpenedEventArgs)
                    {
                        Geometry.UI.Camera camera = viewSettings_Temp.Camera;
                        if (camera != null)
                        {
                            viewportControl.Camera = camera;
                        }
                    }
                    else
                    {
                        viewSettings_Temp.Camera = viewportControl.Camera;
                    }
                }

                viewSettingsList.Add(viewSettings);

                if (tabControl.SelectedItem == tabItem)
                {
                    guid = viewSettings.Guid;
                }
            }

            viewSettingsList.ForEach(x => result.AddViewSettings(x));
            //result.SetViewSettings(viewSettingsList);
            result.ActiveGuid = guid;

            return result;
        }

        private void ViewControl_ObjectContextMenuOpening(object sender, ObjectContextMenuOpeningEventArgs e)
        {
            e.ContextMenuEventArgs.Handled = true;

            ContextMenu contextMenu = e.ContextMenu;

            MenuItem menuItem = null;

            menuItem = new MenuItem();
            menuItem.Name = "MenuItem_ViewSettings";
            menuItem.Header = "View Settings (V)";
            menuItem.Click += MenuItem_ViewSettings_Click;
            contextMenu.Items.Add(menuItem);

            menuItem = new MenuItem();
            menuItem.Name = "MenuItem_LegendSettings";
            menuItem.Header = "Legend Settings";
            menuItem.Click += MenuItem_Legend_Click;
            menuItem.IsEnabled = uIAnalyticalModel.HasLegend(GetActiveGuid());
            contextMenu.Items.Add(menuItem);

            MenuItem menuItem_Select = new MenuItem();
            menuItem_Select.Name = "MenuItem_Select";
            menuItem_Select.Header = "Select";

            MenuItem menuItem_SelectByFilter = new MenuItem();
            menuItem_SelectByFilter.Name = "MenuItem_SelectByFilter";
            menuItem_SelectByFilter.Header = "By Filter";
            menuItem_SelectByFilter.Click += MenuItem_SelectByFilter_Click;
            menuItem_Select.Items.Add(menuItem_SelectByFilter);

            MenuItem menuItem_SelectByGuid = new MenuItem();
            menuItem_SelectByGuid.Name = "MenuItem_SelectByGuid";
            menuItem_SelectByGuid.Header = "By Guid";
            menuItem_SelectByGuid.Click += MenuItem_SelectByGuid_Click;
            menuItem_Select.Items.Add(menuItem_SelectByGuid);
            contextMenu.Items.Add(menuItem_Select);


            MenuItem menuItem_RevealHidden = new MenuItem();
            menuItem_RevealHidden.Name = "MenuItem_RevealHidden";
            menuItem_RevealHidden.Header = "Unhide All (U)";
            menuItem_RevealHidden.Click += MenuItem_RevealHidden_Click;
            contextMenu.Items.Add(menuItem_RevealHidden);

            MenuItem menuItem_Reverse = new MenuItem();
            menuItem_Reverse.Name = "MenuItem_Reverse";
            menuItem_Reverse.Header = "Reverse (R)";
            menuItem_Reverse.Click += MenuItem_Reverse_Click;
            contextMenu.Items.Add(menuItem_Reverse);

            contextMenu.IsOpen = true;

            List<IJSAMObject> jSAMObjects = e.ModelVisual3Ds?.ConvertAll(x => Core.UI.WPF.Query.Tag<IJSAMObject>(x));
            if (jSAMObjects == null)
            {
                return;
            }

            jSAMObjects.RemoveAll(x => x == null);
            if (jSAMObjects.Count == 0)
            {
                return;
            }

            ViewSettings? viewSettings = GetActiveViewSettings() as ViewSettings;
            if (viewSettings is not null)
            {
                Legend? legend = viewSettings?.Legend;
                if (legend is not null)
                {
                    MenuItem menuItem_SelectByLegend = new MenuItem();
                    menuItem_SelectByLegend.Name = "MenuItem_SelectByLegend";
                    menuItem_SelectByLegend.Header = "By Legend Color";
                    menuItem_SelectByLegend.Click += MenuItem_SelectByLegend_Click;
                    menuItem_SelectByLegend.Tag = jSAMObjects;
                    menuItem_Select.Items.Add(menuItem_SelectByLegend);
                }
            }

            MenuItem menuItem_Hide = new MenuItem();
            menuItem_Hide.Name = "MenuItem_Hide";
            menuItem_Hide.Header = "Hide (H)";
            menuItem_Hide.Click += MenuItem_Hide_Click;
            menuItem_Hide.Tag = jSAMObjects;
            contextMenu.Items.Add(menuItem_Hide);

            MenuItem menuItem_Isolate = new MenuItem();
            menuItem_Isolate.Name = "MenuItem_Isolate";
            menuItem_Isolate.Header = "Isolate (I)";
            menuItem_Isolate.Click += MenuItem_Isolate_Click;
            menuItem_Isolate.Tag = jSAMObjects;
            contextMenu.Items.Add(menuItem_Isolate);


            contextMenu.Items.Add(new Separator());

            if (jSAMObjects.Count == 1)
            {
                IJSAMObject jSAMObject = jSAMObjects[0];

                if (jSAMObject is Panel)
                {
                    jSAMObject = (Panel)jSAMObject;
                }
                else if (jSAMObject is Space)
                {
                    jSAMObject = (Space)jSAMObject;
                }
                else if (jSAMObject is Aperture)
                {
                    jSAMObject = (Aperture)jSAMObject;
                }

                if (jSAMObject == null)
                {
                    return;
                }

                menuItem = new MenuItem();
                menuItem.Name = "MenuItem_Properties";
                menuItem.Header = "Properties (P)";
                menuItem.Click += MenuItem_Properties_Click;
                menuItem.Tag = jSAMObject;
                contextMenu.Items.Add(menuItem);
            }

            if (jSAMObjects.Count >= 1)
            {
                List<Space> spaces = jSAMObjects.FindAll(x => x is Space).ConvertAll(x => (Space)x);
                if (spaces != null && spaces.Count > 0)
                {
                    menuItem = new MenuItem();
                    menuItem.Name = "MenuItem_RenameSpaces";
                    menuItem.Header = "Rename";
                    menuItem.Click += MenuItem_RenameSpaces_Click;
                    menuItem.Tag = spaces;
                    contextMenu.Items.Add(menuItem);

                    menuItem = new MenuItem();
                    menuItem.Name = "MenuItem_EditInternalConditions";
                    menuItem.Header = "Modify IC";
                    menuItem.Click += MenuItem_EditInternalConditions_Click;
                    menuItem.Tag = spaces;
                    contextMenu.Items.Add(menuItem);

                    menuItem = new MenuItem();
                    menuItem.Name = "MenuItem_ModifyNCMData";
                    menuItem.Header = "Modify NCM";
                    menuItem.Click += MenuItem_EditMCMDatas;
                    menuItem.Tag = spaces;
                    contextMenu.Items.Add(menuItem);

                    menuItem = new MenuItem();
                    menuItem.Name = "MenuItem_AssignInternalCondition";
                    menuItem.Header = "Assign IC";
                    menuItem.Click += MenuItem_AssignInternalCondition_Click;
                    menuItem.Tag = spaces;
                    contextMenu.Items.Add(menuItem);

                    menuItem = new MenuItem();
                    menuItem.Name = "MenuItem_MapInternalCondition";
                    menuItem.Header = "Map IC";
                    menuItem.Click += MenuItem_MenuItem_MapInternalCondition_Click;
                    menuItem.Tag = spaces;
                    contextMenu.Items.Add(menuItem);

                    menuItem = new MenuItem();
                    menuItem.Name = "MenuItem_ManageZones";
                    menuItem.Header = "Manage Zones";
                    menuItem.Click += MenuItem_ManageZones_Click;
                    menuItem.Tag = spaces;
                    contextMenu.Items.Add(menuItem);

                    menuItem = new MenuItem();
                    menuItem.Name = "MenuItem_ManageMechanicalSystems";
                    menuItem.Header = "Manage Systems";
                    menuItem.Click += MenuItem_ManageMechanicalSystems_Click;
                    menuItem.Tag = spaces;
                    contextMenu.Items.Add(menuItem);

                    contextMenu.Items.Add(Create.MenuItem_SpaceReportPdf(spaces, SpaceReportPdf.SpaceAssumptions, MenuItem_SpaceAssumptionsPdf_Click));
                    contextMenu.Items.Add(Create.MenuItem_SpaceReportPdf(spaces, SpaceReportPdf.SpaceDesignLoadSummary, MenuItem_SpaceDesignLoadSummaryPdf_Click));
                }

                List<Aperture> apertures = jSAMObjects.FindAll(x => x is Aperture).ConvertAll(x => (Aperture)x);
                if (apertures != null && apertures.Count > 0)
                {
                    menuItem = new MenuItem();
                    menuItem.Name = "MenuItem_AssignApertureConstruction";
                    menuItem.Header = "Assign Aperture Construction";
                    menuItem.Click += MenuItem_AssignApertureConstruction_Click;
                    menuItem.Tag = apertures;
                    contextMenu.Items.Add(menuItem);

                    menuItem = new MenuItem();
                    menuItem.Name = "MenuItem_AssignApertureConstructionByThermalTransmittance";
                    menuItem.Header = "Assign Aperture Construction By gValue";
                    menuItem.Click += MenuItem_AssignApertureConstructionByThermalTransmittance_Click; ;
                    menuItem.Tag = apertures;
                    contextMenu.Items.Add(menuItem);

                    menuItem = new MenuItem();
                    menuItem.Name = "MenuItem_EditOpeningProperties";
                    menuItem.Header = "Opening Properties";
                    menuItem.Click += MenuItem_EditOpeningProperties_Click;
                    menuItem.Tag = apertures;
                    contextMenu.Items.Add(menuItem);

                    MenuItem menuItem_SelectByApertureConstructionName = new MenuItem();
                    menuItem_SelectByApertureConstructionName.Name = "MenuItem_SelectByApertureConstructionName";
                    menuItem_SelectByApertureConstructionName.Header = "By Aperture Construction Name";
                    menuItem_SelectByApertureConstructionName.Click += MenuItem_SelectByApertureConstructionName_Click;
                    menuItem_Select.Items.Add(menuItem_SelectByApertureConstructionName);

                    MenuItem menuItem_SelectByApertureType = new MenuItem();
                    menuItem_SelectByApertureType.Name = "MenuItem_SelectByApertureType";
                    menuItem_SelectByApertureType.Header = "By ApertureType";
                    menuItem_SelectByApertureType.Click += MenuItem_SelectByApertureType_Click;
                    menuItem_Select.Items.Add(menuItem_SelectByApertureType);
                }

                List<Panel> panels = jSAMObjects.FindAll(x => x is Panel).ConvertAll(x => (Panel)x);
                if (panels != null && panels.Count > 0)
                {
                    menuItem = new MenuItem();
                    menuItem.Name = "MenuItem_AssignConstruction";
                    menuItem.Header = "Assign Construction";
                    menuItem.Click += MenuItem_AssignConstruction_Click;
                    menuItem.Tag = panels;
                    contextMenu.Items.Add(menuItem);

                    menuItem = new MenuItem();
                    menuItem.Name = "MenuItem_AssignConstructionByThermalTransmittance";
                    menuItem.Header = "Assign Construction By UValue";
                    menuItem.Click += MenuItem_AssignConstructionByThermalTransmittance_Click;
                    menuItem.Tag = panels;
                    contextMenu.Items.Add(menuItem);

                    MenuItem menuItem_SelectByPanelType = new MenuItem();
                    menuItem_SelectByPanelType.Name = "MenuItem_SelectByPanelType";
                    menuItem_SelectByPanelType.Header = "By PanelType";
                    menuItem_SelectByPanelType.Click += MenuItem_SelectByPanelType_Click;
                    menuItem_Select.Items.Add(menuItem_SelectByPanelType);

                    MenuItem menuItem_SelectByConstructionName = new MenuItem();
                    menuItem_SelectByConstructionName.Name = "MenuItem_SelectByConstructionName";
                    menuItem_SelectByConstructionName.Header = "By Construction Name";
                    menuItem_SelectByConstructionName.Click += MenuItem_SelectByConstructionName_Click;
                    menuItem_Select.Items.Add(menuItem_SelectByConstructionName);

                    MenuItem menuItem_SelectByPanelGroup = new MenuItem();
                    menuItem_SelectByPanelGroup.Name = "MenuItem_SelectByPanelGroup";
                    menuItem_SelectByPanelGroup.Header = "By PanelGroup";
                    menuItem_SelectByPanelGroup.Click += MenuItem_SelectByPanelGroup_Click;
                    menuItem_Select.Items.Add(menuItem_SelectByPanelGroup);
                }
            }

            if (jSAMObjects != null && jSAMObjects.Count > 0)
            {
                menuItem = new MenuItem();
                menuItem.Name = "MenuItem_Delete";
                menuItem.Header = "Delete (Del)";
                menuItem.Click += MenuItem_Delete_Click;
                menuItem.Tag = jSAMObjects;
                contextMenu.Items.Add(menuItem);
            }
        }
        
        private void ViewportControl_Loaded(object sender, RoutedEventArgs e)
        {
            ViewportControl viewportControl = sender as ViewportControl;
            if (viewportControl == null)
            {
                return;
            }

            if (doubleRangeWindow != null)
            {
                RefreshDoubleRangeWindow();
            }
        }

        private void ViewportControl_ObjectDoubleClicked(object sender, ObjectDoubleClickedEventArgs e)
        {
            ModelVisual3D modelVisual3D = e.ModelVisual3D;
            if (modelVisual3D == null)
            {
                return;
            }

            IJSAMObject jSAMObject = Core.UI.WPF.Query.JSAMObject<IJSAMObject>(modelVisual3D);

            if (!(jSAMObject is ITaggable))
            {
                return;
            }

            Tag tag = ((ITaggable)jSAMObject).Tag;
            if (tag == null)
            {
                return;
            }

            if (tag.Value is Panel)
            {
                Panel panel = (Panel)tag.Value;
                uIAnalyticalModel.EditPanel(panel, new SAM.Core.UI.WPF.WindowHandle(this));
            }
            else if (tag.Value is Space)
            {
                Space space = (Space)tag.Value;
                uIAnalyticalModel.EditSpace(space, new SAM.Core.UI.WPF.WindowHandle(this));
            }
            else if (tag.Value is Aperture)
            {
                Aperture aperture = (Aperture)tag.Value;
                uIAnalyticalModel.EditAperture(aperture, new SAM.Core.UI.WPF.WindowHandle(this));
            }
        }

        private void ViewportControl_ObjectHoovered(object sender, ObjectHooveredEventArgs e)
        {
            ViewportControl viewportControl = sender as ViewportControl;

            ModelVisual3D modelVisual3D = e.ModelVisual3D;
            if (modelVisual3D == null)
            {
                return;
            }

            IJSAMObject jSAMObject = Core.UI.WPF.Query.JSAMObject<IJSAMObject>(modelVisual3D);

            if (!(jSAMObject is ITaggable))
            {
                return;
            }

            Tag tag = ((ITaggable)jSAMObject).Tag;
            if (tag == null)
            {
                return;
            }

            if (tag.Value is Panel)
            {
                Panel panel = (Panel)tag.Value;
                //viewportControl.Hint = string.Format("Panel {0}, Guid: {1}", panel.Name, panel.Guid);
            }
            else if (tag.Value is Space)
            {
                Space space = (Space)tag.Value;
                //viewportControl.Hint = string.Format("Space {0}, Guid: {1}", space.Name, space.Guid);

                InternalCondition internalCondition = space.InternalCondition;
                if (internalCondition != null)
                {
                    //viewportControl.Hint += string.Format(", IC: {0}", internalCondition.Name);
                }
            }
        }

        private void ViewportControl_ObjectSelectionChanged(object sender, ObjectSelectionChangedEventArgs e)
        {
            
        }
        
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (doubleRangeWindow != null)
            {
                doubleRangeWindow.Close();
                doubleRangeWindow = null;
            }
        }
        
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            // Undo / redo. Ctrl+Z undoes; Ctrl+Y or Ctrl+Shift+Z redoes. Checked before the Z chord
            // prefix below (that only starts on a modifier-free Z).
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Z)
            {
                Undo();
                return;
            }

            if ((Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Y)
                || (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift) && e.Key == Key.Z))
            {
                Redo();
                return;
            }

            // Complete a pending chord (e.g. Z then E). unchecked handles TickCount wraparound.
            if (pendingChordKey.HasValue)
            {
                Key prefix = pendingChordKey.Value;
                pendingChordKey = null;

                if (unchecked(Environment.TickCount - pendingChordTick) <= chordTimeoutMilliseconds && prefix == Key.Z)
                {
                    if (e.Key == Key.E)
                    {
                        ZoomExtents();
                        return;
                    }

                    if (e.Key == Key.S)
                    {
                        ZoomSelected();
                        return;
                    }

                    // Not a Z-chord - fall through and handle e.Key as a normal shortcut below.
                }
            }

            // Start the Z chord prefix; Z on its own does nothing.
            if (e.Key == Key.Z && Keyboard.Modifiers == ModifierKeys.None)
            {
                pendingChordKey = Key.Z;
                pendingChordTick = Environment.TickCount;
                return;
            }

            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.S)
            {
                SaveAs();
            }
            else if (e.Key == Key.G)
            {
                SelectByGuid();
            }
            else if (e.Key == Key.F)
            {
                SelectByFilter();
            }
            else if (e.Key == Key.F12)
            {
                ViewportControl viewportControl = GetActiveViewportControl();
                if (viewportControl != null)
                {
                    List<SAMObject> sAMObjects = viewportControl.SelectedSAMObjects<SAMObject>();
                    if (sAMObjects != null)
                    {
                        SAM.Core.UI.WPF.JsonWindow jsonWindow = new SAM.Core.UI.WPF.JsonWindow(sAMObjects) { Owner = this };
                        jsonWindow.ShowDialog();
                    }

                }
            }
            else if (e.Key == Key.I)
            {
                Isolate();
            }
            else if (e.Key == Key.H)
            {
                Hide();
            }
            else if (e.Key == Key.R)
            {
                Reverse();
            }
            else if (e.Key == Key.U)
            {
                RevealHidden();
            }
            else if (e.Key == Key.Delete)
            {
                Delete();
            }
            else if (e.Key == Key.V)
            {
                EditViewSettings();
            }
            else if (e.Key == Key.P)
            {
                ShowProperties();
            }
        }
        
        private void ZoomExtents()
        {
            GetActiveViewportControl()?.ZoomExtents();
        }

        private void ZoomSelected()
        {
            ViewportControl viewportControl = GetActiveViewportControl();
            if (viewportControl == null)
            {
                return;
            }

            List<SAMObject> sAMObjects = viewportControl.SelectedSAMObjects<SAMObject>();
            if (sAMObjects != null && sAMObjects.Count != 0)
            {
                viewportControl.Zoom(sAMObjects);
            }
        }
    }
}
