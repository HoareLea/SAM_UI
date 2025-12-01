using Microsoft.Win32;
using SAM.Core;
using SAM.Core.UI;
using SAM.Core.UI.WPF;
using SAM.Core.Windows.Forms;
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
        private static string titlePrefix = "SAM Analytical";

        private ProgressBarWindowManager progressBarWindowManager = new ProgressBarWindowManager();

        private UIAnalyticalModel uIAnalyticalModel = null;
        private Core.Windows.WindowHandle windowHandle = null;

        private DoubleRangeWindow doubleRangeWindow = null;
        
        public AnalyticalWindow()
        {
            Initailize();
        }

        public AnalyticalWindow(StartupOptions startupOptions)
        {
            Initailize();

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

        private void Initailize()
        {
            InitializeComponent();

            Title = titlePrefix;

            windowHandle = new Core.Windows.WindowHandle(this);

            Icon = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM);

            RibbonButton_OpenAnalyticalModel.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_Open);
            RibbonButton_OpenAnalyticalModel.Click += RibbonButton_OpenAnalyticalModel_Click;

            RibbonButton_NewAnalyticalModel.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_New);
            RibbonButton_NewAnalyticalModel.Click += RibbonButton_NewAnalyticalModel_Click;

            RibbonButton_SaveAnalyticalModel.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_Save);
            RibbonButton_SaveAnalyticalModel.Click += RibbonButton_SaveAnalyticalModel_Click;

            RibbonButton_SaveAsAnalyticalModel.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_SaveAs);
            RibbonButton_SaveAsAnalyticalModel.Click += RibbonButton_SaveAsAnalyticalModel_Click;

            RibbonButton_CloseAnalyticalModel.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_Close);
            RibbonButton_CloseAnalyticalModel.Click += RibbonButton_CloseAnalyticalModel_Click;

            RibbonButton_ImportAnalyticalModel.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_Open);
            RibbonButton_ImportAnalyticalModel.Click += RibbonButton_ImportAnalyticalModel_Click;

            RibbonButton_ExportAnalyticalModel.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_Save);
            RibbonButton_ExportAnalyticalModel.Click += RibbonButton_ExportAnalyticalModel_Click;

            RibbonButton_NewSectionViews.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_Section);
            RibbonButton_NewSectionViews.Click += RibbonButton_NewSectionViews_Click;

            RibbonButton_NewSectionView.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_Section);
            RibbonButton_NewSectionView.Click += RibbonButton_NewSectionView_Click;

            RibbonButton_New3DView.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_Section);
            RibbonButton_New3DView.Click += RibbonButton_New3DView_Click;

            RibbonButton_ViewSettings.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_Section);
            RibbonButton_ViewSettings.Click += RibbonButton_ViewSettings_Click;

            RibbonButton_CloseView.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_Section);
            RibbonButton_CloseView.Click += RibbonButton_CloseView_Click;

            RibbonButton_AnalyticalModelLocation.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_Location);
            RibbonButton_AnalyticalModelLocation.Click += RibbonButton_AnalyticalModelLocation_Click;

            RibbonButton_AnalyticalModelProperties.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_AnalyticalModelProperties);
            RibbonButton_AnalyticalModelProperties.Click += RibbonButton_AnalyticalModelProperties_Click;

            RibbonButton_ImportObjects.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_Import);
            RibbonButton_ImportObjects.Click += RibbonButton_ImportObjects_Click;

            RibbonButton_AnalyticalModelCheck.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_ModelCheck);
            RibbonButton_AnalyticalModelCheck.Click += RibbonButton_AnalyticalModelCheck_Click;

            RibbonButton_EditMaterialLibrary.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_MaterialLibrary);
            RibbonButton_EditMaterialLibrary.Click += RibbonButton_EditMaterialLibrary_Click;

            RibbonButton_EditInternalConditionLibrary.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_InternalCondition);
            RibbonButton_EditInternalConditionLibrary.Click += RibbonButton_EditInternalConditionLibrary_Click;

            RibbonButton_EditProfileLibrary.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_ProfileLibrary);
            RibbonButton_EditProfileLibrary.Click += RibbonButton_EditProfileLibrary_Click;

            RibbonButton_EditSpaces.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_Space);
            RibbonButton_EditSpaces.Click += RibbonButton_EditSpaces_Click;

            RibbonButton_EditConstructions.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_ConstructionLibrary);
            RibbonButton_EditConstructions.Click += RibbonButton_EditConstructions_Click;

            RibbonButton_EditApertureConstructions.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_ApertureConstruction);
            RibbonButton_EditApertureConstructions.Click += RibbonButton_EditApertureConstructions_Click;

            RibbonButton_EditWeatherData.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_WeatherData);
            RibbonButton_EditWeatherData.Click += RibbonButton_EditWeatherData_Click;

            RibbonButton_ImportWeatherData.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_Import);
            RibbonButton_ImportWeatherData.Click += RibbonButton_ImportWeatherData_Click;

            RibbonButton_SolarSimulation.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_SolarCalculator);
            RibbonButton_SolarSimulation.Click += RibbonButton_SolarSimulation_Click;

            RibbonButton_EnergySimulation.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_EnergySimulation);
            RibbonButton_EnergySimulation.Click += RibbonButton_EnergySimulation_Click;

            RibbonButton_EditLibrary.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_EditLibrary);
            RibbonButton_EditLibrary.Click += RibbonButton_EditLibrary_Click;

            RibbonButton_AssignMechanicalSystems.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_EditLibrary);
            RibbonButton_AssignMechanicalSystems.Click += RibbonButton_AssignMechanicalSystems_Click;

            RibbonButton_OpenT3D.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_T3D);
            RibbonButton_OpenT3D.Click += RibbonButton_OpenT3D_Click;

            RibbonButton_OpenTBD.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_TBD);
            RibbonButton_OpenTBD.Click += RibbonButton_OpenTBD_Click;

            RibbonButton_OpenTSD.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_TSD);
            RibbonButton_OpenTSD.Click += RibbonButton_OpenTSD_Click;

            RibbonButton_OpenTPD.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_TPD);
            RibbonButton_OpenTPD.Click += RibbonButton_OpenTPD_Click;

            RibbonButton_ThermalTransmittanceCalculator.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_T3D);
            RibbonButton_ThermalTransmittanceCalculator.Click += RibbonButton_ThermalTransmittanceCalculator_Click;

            RibbonButton_GlazingCalculator.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_T3D);
            RibbonButton_GlazingCalculator.Click += RibbonButton_GlazingCalculator_Click;

            RibbonButton_CreateCases.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_CreateCases);
            RibbonButton_CreateCases.Click += RibbonButton_CreateCases_Click;

            RibbonButton_SimulateCases.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_CreateCases);
            RibbonButton_SimulateCases.Click += RibbonButton_SimulateCases_Click;

            RibbonMenuButton_PartL.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_PartL);

            RibbonButton_OpenPartL.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_PartL);
            RibbonButton_OpenPartL.Click += RibbonButton_OpenPartL_Click;

            RibbonButton_UpdateUKBRFile.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_PartL_UKBR);
            RibbonButton_UpdateUKBRFile.Click += RibbonButton_UpdateUKBRFile_Click;

            RibbonButton_Hydra.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_Hydra);
            RibbonButton_Hydra.Click += RibbonButton_Hydra_Click;

            RibbonButton_NCMNames.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_EditLibrary);
            RibbonButton_NCMNames.Click += RibbonButton_NCMNames_Click;

            RibbonButton_CleanAnalyticalModel.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_Clean);
            RibbonButton_CleanAnalyticalModel.Click += RibbonButton_CleanAnalyticalModel_Click;

            RibbonButton_RemoveAirMovementObjects.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_Clean);
            RibbonButton_RemoveAirMovementObjects.Click += RibbonButton_RemoveAirMovementObjects_Click;

            //RibbonButton_CreateTBD.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_TBD);
            //RibbonButton_CreateTBD.Click += RibbonButton_CreateTBD_Click;

            RibbonButton_AddMissingObjects.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_AddMissingObjects);
            RibbonButton_AddMissingObjects.Click += RibbonButton_AddMissingObjects_Click;

            RibbonButton_PrintRoomDataSheets.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_PrintRDS);
            RibbonButton_PrintRoomDataSheets.Click += RibbonButton_PrintRoomDataSheets_Click;

            RibbonButton_OpenMollierChart.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_MollierDiagram);
            RibbonButton_OpenMollierChart.Click += RibbonButton_OpenMollierChart_Click;

            RibbonButton_ViewGeometry.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_Space);
            RibbonButton_ViewGeometry.Click += RibbonButton_ViewGeometry_Click;

            RibbonButton_MapInternalConditions.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_Space);
            RibbonButton_MapInternalConditions.Click += RibbonButton_MapInternalConditions_Click;

            RibbonButton_MapInternalConditionsByTM59.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_Space);
            RibbonButton_MapInternalConditionsByTM59.Click += RibbonButton_MapInternalConditionsByTM59_Click;

            RibbonButton_EditInternalConditions.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_Space);
            RibbonButton_EditInternalConditions.Click += RibbonButton_EditInternalConditions_Click;

            RibbonButton_TextMap.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_PrintRDS);
            RibbonButton_TextMap.Click += RibbonButton_TextMap_Click;

            RibbonButton_SelectByFilter.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_PrintRDS);
            RibbonButton_SelectByFilter.Click += RibbonButton_SelectByFilter_Click;

            RibbonButton_SelectByGuid.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_PrintRDS);
            RibbonButton_SelectByGuid.Click += RibbonButton_SelectByGuid_Click;

            RibbonButton_RevealHidden.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_PrintRDS);
            RibbonButton_RevealHidden.Click += RibbonButton_RevealHidden_Click;

            RibbonButton_ViewRange.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_PrintRDS);
            RibbonButton_ViewRange.Click += RibbonButton_ViewRange_Click;

            RibbonButton_AirHandlingUnitDiagram.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_AirHandlingUnitDiagram);
            RibbonButton_AirHandlingUnitDiagram.Click += RibbonButton_AirHandlingUnitDiagram_Click;

            RibbonButton_SpaceDiagram.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_SpaceDiagram);
            RibbonButton_SpaceDiagram.Click += RibbonButton_SpaceDiagram_Click;

            RibbonButton_RemoveResults.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_Close);
            RibbonButton_RemoveResults.Click += RibbonButton_RemoveResults_Click;

            RibbonButton_Wiki.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_Wiki);
            RibbonButton_Wiki.Click += RibbonButton_Wiki_Click;

            RibbonButton_About.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_Wiki);
            RibbonButton_About.Click += RibbonButton_About_Click;

            RibbonButton_Test.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_Wiki);
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

        private void RibbonButton_SimulateCases_Click(object sender, RoutedEventArgs e)
        {
            if (uIAnalyticalModel?.JSAMObject is not AnalyticalModel analyticalModel)
            {
                return;
            }

            Modify.SimulateCases(uIAnalyticalModel);
        }

        private void RibbonButton_CreateCases_Click(object sender, RoutedEventArgs e)
        {
            if(uIAnalyticalModel?.JSAMObject is not AnalyticalModel analyticalModel)
            {
                return;
            }

            List<AnalyticalModel>? analyticalModel_Case = Create.AnalyticalModels(analyticalModel);
        }

        private void RibbonButton_RemoveAirMovementObjects_Click(object sender, RoutedEventArgs e)
        {
            Modify.RemoveAirMovementObjects(uIAnalyticalModel);
        }

        private void RibbonButton_GlazingCalculator_Click(object sender, RoutedEventArgs e)
        {
            Modify.CalculateGlazing(uIAnalyticalModel);
        }

        private void RibbonButton_ThermalTransmittanceCalculator_Click(object sender, RoutedEventArgs e)
        {
            Modify.ThermalTransmittanceCalculator_SingleConstruction(uIAnalyticalModel);
        }

        private void RibbonButton_UpdateUKBRFile_Click(object sender, RoutedEventArgs e)
        {
            bool result = Modify.UpdateUKBRFile(uIAnalyticalModel);
        }

        private void RibbonButton_NCMNames_Click(object sender, RoutedEventArgs e)
        {
            NCMNameCollectionWindow nCMNameCollectionWindow = new NCMNameCollectionWindow(Analytical.Query.DefaultNCMNameCollection(), new NCMNameCollectionOptions(false));
            nCMNameCollectionWindow.ShowDialog();
        }

        private void RibbonButton_AssignMechanicalSystems_Click(object sender, RoutedEventArgs e)
        {
            List<Space> spaces = GetActiveViewportControl()?.SelectedSAMObjects<Space>();
            if(spaces == null  || spaces.Count == 0)
            {
                spaces = null;
            }

            //uIAnalyticalModel?.AssignMechanicalSystems(spaces);
            uIAnalyticalModel?.AddMechanicalSystems(spaces);
        }

        private void RibbonButton_ViewRange_Click(object sender, RoutedEventArgs e)
        {
            ShowViewRange();
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

        private void DoubleRangeWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            doubleRangeWindow = null;
        }

        private void RefreshDoubleRangeWindow()
        {
            if(doubleRangeWindow == null)
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
                if(planes.Count == 1)
                {
                    Geometry.Spatial.Plane plane = planes[0];

                    if(plane.Normal.Z > 0)
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

        private void DoubleRangeWindow_RangeChanged(object sender, RangeChangedEventArgs<double> e)
        {
            Range<double> range = e.Range;
            if(range == null)
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
            if(threeDimensionalViewSettings == null)
            {
                return;
            }

            List<Geometry.Spatial.Plane> planes = new List<Geometry.Spatial.Plane>();

            if(doubleRangeWindow.Min != range.Min)
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

            if(planes.Count == 0)
            {
                planes = null;
            }

            threeDimensionalViewSettings.Planes = planes;

            uIGeometrySettings.AddViewSettings(threeDimensionalViewSettings);

            analyticalModel.SetValue(AnalyticalModelParameter.UIGeometrySettings, uIGeometrySettings);

            uIAnalyticalModel.SetJSAMObject(analyticalModel, new ViewSettingsModification(threeDimensionalViewSettings, true));
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

        private void EditMechanicalSystems(IEnumerable<Space> spaces, IEnumerable<Space> selectedSpaces = null)
        {
            Modify.EditMechanicalSystems(uIAnalyticalModel, spaces, MechanicalSystemCategory.Ventilation, selectedSpaces);
        }

        private void AssignMechanicalSystems(IEnumerable<Space> spaces = null)
        {
            Modify.AssignMechanicalSystems(uIAnalyticalModel, spaces);
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

            using (SearchForm<IUIFilter> searchForm = new SearchForm<IUIFilter>("Select Filter", uIFilters, x => x.Name))
            {
                searchForm.SelectionMode = System.Windows.Forms.SelectionMode.One;
                if (searchForm.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                {
                    return;
                }

                e.UIFilter = searchForm.SelectedItems.FirstOrDefault();
            }
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

        private void MenuItem_Legend_Click(object sender, RoutedEventArgs e)
        {
            EditLegend();
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

            Core.Windows.WindowHandle windowHandle = new Core.Windows.WindowHandle(this);

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
            else if(jSAMObject is Aperture)
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

        private void MenuItem_SelectByFilter_Click(object sender, RoutedEventArgs e)
        {
            SelectByFilter();
        }

        private void MenuItem_SelectByGuid_Click(object sender, RoutedEventArgs e)
        {
            SelectByGuid();
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

        private void Reload(ModifiedEventArgs modifiedEventArgs)
        {
            progressBarWindowManager.Show("Reloading", "Reloading...");

            SetEnabled();
            //SetActiveGuid();

            uIAnalyticalModel.Modified -= UIAnalyticalModel_Modified;
            tabControl.SelectionChanged -= tabControl_SelectionChanged;

            AnalyticalModel analyticalModel = uIAnalyticalModel.JSAMObject;

            UpdateTabItems(tabControl, analyticalModel, modifiedEventArgs);

            UpdateUIGeometrySettings(tabControl, analyticalModel, modifiedEventArgs);

            uIAnalyticalModel.SetJSAMObject(analyticalModel, modifiedEventArgs.Modifications);

            uIAnalyticalModel.Modified += UIAnalyticalModel_Modified;
            tabControl.SelectionChanged += tabControl_SelectionChanged;

            progressBarWindowManager.Close();
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

        private void RibbonButton_About_Click(object sender, RoutedEventArgs e)
        {
            List<AboutInfoType> abouInfoTypes = Enum.GetValues(typeof(AboutInfoType)).Cast<AboutInfoType>().ToList();

            using (ComboBoxForm<AboutInfoType> comboBoxForm = new ComboBoxForm<AboutInfoType>("Info", abouInfoTypes, x => x.Description()))
            {
                if(comboBoxForm.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    MessageBox.Show(Core.Query.AboutInfoTypeText(comboBoxForm.SelectedItem), "Info");
                }
            }
        }
        
        private void RibbonButton_AddMissingObjects_Click(object sender, RoutedEventArgs e)
        {
            uIAnalyticalModel?.AddMissingObjects(windowHandle);
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
            uIAnalyticalModel?.Simulate();
        }

        private void RibbonButton_ExportAnalyticalModel_Click(object sender, RoutedEventArgs e)
        {
            Modify.Export(uIAnalyticalModel);
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
            uIAnalyticalModel?.Import(windowHandle);
        }

        private void RibbonButton_ImportWeatherData_Click(object sender, RoutedEventArgs e)
        {
            uIAnalyticalModel?.ImportWeatherData(windowHandle);
        }

        private void RibbonButton_MapInternalConditions_Click(object sender, RoutedEventArgs e)
        {
            Modify.MapInternalConditions(uIAnalyticalModel);
        }

        private void RibbonButton_MapInternalConditionsByTM59_Click(object sender, RoutedEventArgs e)
        {
            Modify.MapInternalConditionsByTM59(uIAnalyticalModel);
        }

        private void RibbonButton_New3DView_Click(object sender, RoutedEventArgs e)
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

            ViewSettingsWindow viewSettingsWindow = new ViewSettingsWindow(twoDimensionalViewSettings, analyticalModel);
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
            openFileDialog.Filter = "json files (*.json)|*.json|All files (*.*)|*.*";
            openFileDialog.FilterIndex = 2;
            openFileDialog.RestoreDirectory = true;
            if (openFileDialog.ShowDialog(this) == false)
            {
                return;
            }
            path = openFileDialog.FileName;

            Open(path);
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

        private void RibbonButton_OpenMollierChart_Click(object sender, RoutedEventArgs e)
        {
            using (Core.Mollier.UI.MollierForm mollierForm = new Core.Mollier.UI.MollierForm())
            {
                if (mollierForm.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                {
                    return;
                }
            }
        }

        private void RibbonButton_OpenT3D_Click(object sender, RoutedEventArgs e)
        {
            string path = Core.Tas.Query.TAS3DPath();

            if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path))
            {
                return;
            }

            System.Diagnostics.Process.Start(path);
        }

        private void RibbonButton_OpenTBD_Click(object sender, RoutedEventArgs e)
        {
            string path = Core.Tas.Query.TBDPath();

            if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path))
            {
                return;
            }

            System.Diagnostics.Process.Start(path);
        }

        private void RibbonButton_OpenTPD_Click(object sender, RoutedEventArgs e)
        {
            string path = Core.Tas.Query.TPDPath();

            if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path))
            {
                return;
            }

            System.Diagnostics.Process.Start(path);
        }

        private void RibbonButton_OpenTSD_Click(object sender, RoutedEventArgs e)
        {
            string path = Core.Tas.Query.TSDPath();

            if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path))
            {
                return;
            }

            System.Diagnostics.Process.Start(path);
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

        private void RibbonButton_PrintRoomDataSheets_Click(object sender, RoutedEventArgs e)
        {
            uIAnalyticalModel?.PrintRoomDataSheets(windowHandle);
        }

        private void RibbonButton_RemoveResults_Click(object sender, RoutedEventArgs e)
        {
            uIAnalyticalModel.RemoveResults();
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
            if (uIAnalyticalModel == null)
            {
                return;
            }

            AnalyticalModel analyticalModel = uIAnalyticalModel.JSAMObject;

            SetUIGeometrySettings(tabControl, analyticalModel);

            uIAnalyticalModel.SaveAs();
        }

        private void RibbonButton_SelectByFilter_Click(object sender, RoutedEventArgs e)
        {
            SelectByFilter();
        }

        private void RibbonButton_SelectByGuid_Click(object sender, RoutedEventArgs e)
        {
            SelectByGuid();
        }

        private void RibbonButton_SolarSimulation_Click(object sender, RoutedEventArgs e)
        {
            uIAnalyticalModel?.SolarSimulation(windowHandle);
        }

        private void RibbonButton_SpaceDiagram_Click(object sender, RoutedEventArgs e)
        {
            uIAnalyticalModel?.SpaceDiagram(windowHandle);
        }

        private void RibbonButton_Test_Click(object sender, RoutedEventArgs e)
        {
            RelationClusterComplexReferenceWindow relationClusterComplexReferenceWindow = new RelationClusterComplexReferenceWindow();
            relationClusterComplexReferenceWindow.RelationCluster = new RelationCluster(uIAnalyticalModel?.JSAMObject?.AdjacencyCluster);

            bool? dialogResult = relationClusterComplexReferenceWindow.ShowDialog();
            if (dialogResult == null || !dialogResult.HasValue || !dialogResult.Value)
            {
                return;
            }

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

        private void RibbonButton_ViewGeometry_Click(object sender, RoutedEventArgs e)
        {
            GeometryWindow geometryWindow = new GeometryWindow();
            geometryWindow.Show();
        }

        private void RibbonButton_ViewSettings_Click(object sender, RoutedEventArgs e)
        {
            EditViewSettings();
        }

        private void RibbonButton_Wiki_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/HoareLea/SAM_UI/wiki");
        }

        private void SelectByFilter()
        {
            AdjacencyCluster adjacencyCluster = uIAnalyticalModel?.JSAMObject?.AdjacencyCluster;
            if (adjacencyCluster == null)
            {
                return;
            }

            IUIFilter uIFilter = ActiveManager.GetValue<IUIFilter>(Assembly.GetExecutingAssembly(), "UIFilter");
            if(uIFilter != null)
            {
                UI.Modify.AssignAdjacencyCluster(uIFilter, adjacencyCluster);
            }

            List<IUIFilter> uIFilters = ActiveManager.GetValue<SAMCollection<IUIFilter>>(Assembly.GetExecutingAssembly(), "UIFilters")?.Cast<IUIFilter>().ToList();
            if(uIFilters != null && uIFilters.Count != 0)
            {
                uIFilters.ForEach(x => UI.Modify.AssignAdjacencyCluster(x, adjacencyCluster));
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

            FilterWindow filterWindow = new FilterWindow() { Types = new List<Type>() { typeof(Space), typeof(Panel), typeof(Aperture) }, Type = typeof(Space), UIFilter = uIFilter, UIFilters = uIFilters, JSAMObjects = jSAMObjects, AdjacencyCluster = adjacencyCluster };
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

            using (TextBoxForm<string> textBoxControl = new TextBoxForm<string>("Guid", "Insert Guids"))
            {
                if (textBoxControl.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                {
                    return;
                }
                value = textBoxControl.Value;
            }

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

                List<AirHandlingUnit> airHandlingUnits = analyticalModel.AdjacencyCluster?.GetObjects<AirHandlingUnit>();
                if (airHandlingUnits != null && airHandlingUnits.Count != 0)
                {
                    RibbonButton_SpaceDiagram.IsEnabled = true;
                    RibbonButton_AirHandlingUnitDiagram.IsEnabled = true;
                }
            }
        }

        private void SetUIGeometrySettings(TabControl tabControl, AnalyticalModel analyticalModel)
        {
            UIGeometrySettings uIGeometrySettings = UpdateUIGeometrySettings(tabControl, analyticalModel, new ModifiedEventArgs());

            uIAnalyticalModel.Modified -= UIAnalyticalModel_Modified;
            uIAnalyticalModel.SetJSAMObject(analyticalModel, new ViewSettingsModification(uIGeometrySettings.GetViewSettings<IViewSettings>()));
            uIAnalyticalModel.Modified += UIAnalyticalModel_Modified;
        }

        private void tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Guid guid = GetActiveGuid();
            if (guid == Guid.Empty)
            {
                return;
            }

            uIAnalyticalModel.Modified -= UIAnalyticalModel_Modified;
            Modify.SetActiveGuid(uIAnalyticalModel, guid);
            uIAnalyticalModel.Modified += UIAnalyticalModel_Modified;
        }

        private void tabItem_ContextMenuOpening(object sender, ContextMenuEventArgs e)
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
            menuItem.Name = "MenuItem_Settings_TabItem";
            menuItem.Header = "Settings";
            menuItem.Click += MenuItem_Settings_TabItem_Click;
            contextMenu.Items.Add(menuItem);

            contextMenu.IsOpen = true;
        }

        private void UIAnalyticalModel_Closed(object sender, ClosedEventArgs e)
        {
            Reload(e);

            Title = titlePrefix;
        }

        private void UIAnalyticalModel_Modified(object sender, ModifiedEventArgs e)
        {
            Reload(e);
        }

        private void UIAnalyticalModel_Opened(object sender, OpenedEventArgs e)
        {
            SetDefaultViewSettings();
            Reload(e);

            Title = titlePrefix;

            string name = uIAnalyticalModel?.JSAMObject?.Name;
            if (name != null)
            {
                Title += string.Format(" [{0}]", name);
            }
        }

        private TabItem UpdateTabItem(TabControl tabControl, AnalyticalModel analyticalModel, ModifiedEventArgs modifiedEventArgs, IViewSettings viewSettings = null)
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
                tabItem.ContextMenuOpening += tabItem_ContextMenuOpening;
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
            if (!updateGeometry)
            {
                List<ViewSettingsModification> viewSettingsModifications = modifiedEventArgs.GetModifications<ViewSettingsModification>((x) => x.ViewSettings?.Find(y => y.Guid == viewSettings.Guid) != null);
                if(viewSettingsModifications != null && viewSettingsModifications.Count != 0)
                {

                }
            }

            if (!updateGeometry)
            {
                List<AnalyticalModelModification> analyticalModelModifications = modifiedEventArgs.GetModifications<AnalyticalModelModification>();
                HashSet<Guid> guids = analyticalModelModifications?.Guids();
                if (guids == null || guids.Count == 0 || viewportControl.ContainsAny<SAMObject>(guids))
                {
                    updateGeometry = true;
                }
            }

            if (updateGeometry)
            {
                if (progressBarWindowManager != null)
                {
                    progressBarWindowManager.Text = string.Format("View Regeneration [{0}]", string.IsNullOrWhiteSpace(name) ? "???" : name);
                }

                List<SAMObject> sAMObjects = viewportControl.SelectedSAMObjects<SAMObject>();

                GeometryObjectModel geometryObjectModel = analyticalModel.ToSAM_GeometryObjectModel(viewSettings);
                viewportControl.UIGeometryObjectModel = new UIGeometryObjectModel(geometryObjectModel);

                viewportControl.Select(sAMObjects);
            }

            Mode mode = viewSettings.Mode();
            if (viewportControl.Mode != mode)
            {
                viewportControl.Mode = mode;
            }

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

        private void ViewportControl_ObjectSelectionChanged(object sender, ObjectSelectionChangedEventArgs e)
        {
            
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
                    foreach (IViewSettings viewSettings in viewSettingsList)
                    {
                        if (viewSettings is ViewSettings)
                        {
                            if (!((ViewSettings)viewSettings).Enabled)
                            {
                                continue;
                            }
                        }

                        TabItem tabItem = UpdateTabItem(tabControl, analyticalModel, modifiedEventArgs, viewSettings);
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

            return result;
        }

        private UIGeometrySettings UpdateUIGeometrySettings(TabControl tabControl, AnalyticalModel analyticalModel, ModifiedEventArgs modifiedEventArgs)
        {
            if (analyticalModel == null || tabControl == null)
            {
                return null;
            }

            if (!analyticalModel.TryGetValue(AnalyticalModelParameter.UIGeometrySettings, out UIGeometrySettings result) || result == null)
            {
                result = new UIGeometrySettings();
            }

            List<IViewSettings> viewSettingsList = new List<IViewSettings>();
            Guid guid = Guid.Empty;
            for (int i = 0; i < tabControl.Items.Count; i++)
            {
                TabItem tabItem = tabControl.Items[i] as TabItem;

                ViewportControl viewportControl = tabItem?.Content as ViewportControl;
                if (viewportControl == null)
                {
                    continue;
                }

                GeometryObjectModel geometryObjectModel = viewportControl.UIGeometryObjectModel?.JSAMObject;
                if (geometryObjectModel == null)
                {
                    return null;
                }

                if (!geometryObjectModel.TryGetValue(GeometryObjectModelParameter.ViewSettings, out IViewSettings viewSettings) || viewSettings == null)
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
            menuItem.Header = "View Settings";
            menuItem.Click += MenuItem_ViewSettings_Click;
            contextMenu.Items.Add(menuItem);

            menuItem = new MenuItem();
            menuItem.Name = "MenuItem_LegendSettings";
            menuItem.Header = "Legend Settings";
            menuItem.Click += MenuItem_Legend_Click;
            menuItem.IsEnabled = uIAnalyticalModel.HasLegend(GetActiveGuid());
            contextMenu.Items.Add(menuItem);

            menuItem = new MenuItem();
            menuItem.Name = "MenuItem_Select";
            menuItem.Header = "Select";

            MenuItem menuItem_SelectByFilter = new MenuItem();
            menuItem_SelectByFilter.Name = "MenuItem_SelectByFilter";
            menuItem_SelectByFilter.Header = "By Filter";
            menuItem_SelectByFilter.Click += MenuItem_SelectByFilter_Click;
            menuItem.Items.Add(menuItem_SelectByFilter);

            MenuItem menuItem_SelectByGuid = new MenuItem();
            menuItem_SelectByGuid.Name = "MenuItem_SelectByGuid";
            menuItem_SelectByGuid.Header = "By Guid";
            menuItem_SelectByGuid.Click += MenuItem_SelectByGuid_Click;
            menuItem.Items.Add(menuItem_SelectByGuid);

            contextMenu.Items.Add(menuItem);

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

            MenuItem menuItem_Hide = new MenuItem();
            menuItem_Hide.Name = "MenuItem_Hide";
            menuItem_Hide.Header = "Hide";
            menuItem_Hide.Click += MenuItem_Hide_Click;
            menuItem_Hide.Tag = jSAMObjects;
            contextMenu.Items.Add(menuItem_Hide);


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
                menuItem.Header = "Properties";
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
                }
            }
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

        private void MenuItem_Hide_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)sender;
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

        private void RibbonButton_RevealHidden_Click(object sender, RoutedEventArgs e)
        {
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

            SetUIGeometrySettings(tabControl, uIAnalyticalModel.JSAMObject);
            UI.Modify.RemoveOverrides(uIAnalyticalModel, viewportControl.Guid);
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
                uIAnalyticalModel.EditPanel(panel, new Core.Windows.WindowHandle(this));
            }
            else if (tag.Value is Space)
            {
                Space space = (Space)tag.Value;
                uIAnalyticalModel.EditSpace(space, new Core.Windows.WindowHandle(this));
            }
            else if (tag.Value is Aperture)
            {
                Aperture aperture = (Aperture)tag.Value;
                uIAnalyticalModel.EditAperture(aperture, new Core.Windows.WindowHandle(this));
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

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F)
            {
                SelectByGuid();
                return;
            }

            if (e.Key == Key.F12)
            {
                ViewportControl viewportControl = GetActiveViewportControl();
                if (viewportControl != null)
                {
                    List<SAMObject> sAMObjects = viewportControl.SelectedSAMObjects<SAMObject>();
                    if (sAMObjects != null)
                    {
                        using (JsonForm<SAMObject> jsonForm = new JsonForm<SAMObject>(sAMObjects))
                        {
                            jsonForm.ShowDialog();

                        }
                    }

                }
                return;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if(doubleRangeWindow != null)
            {
                doubleRangeWindow.Close();
                doubleRangeWindow = null;
            }
        }
    }
}