using Microsoft.Win32;
using SAM.Core;
using SAM.Core.UI.WPF;
using SAM.Geometry;
using SAM.Geometry.UI;
using SAM.Geometry.UI.WPF;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace SAM.Analytical.UI.WPF.Windows
{
    /// <summary>
    /// Interaction logic for AnalyticalWindow.xaml
    /// </summary>
    public partial class AnalyticalWindow : System.Windows.Window
    {
        private Core.Windows.WindowHandle windowHandle;
        
        private UIAnalyticalModel uIAnalyticalModel;

        public AnalyticalWindow()
        {
            InitializeComponent();

            windowHandle = new Core.Windows.WindowHandle(this);

            Icon = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM);
            
            RibbonButton_General_OpenAnalyticalModel.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_Open);
            RibbonButton_General_OpenAnalyticalModel.Click += RibbonButton_General_OpenAnalyticalModel_Click;

            RibbonButton_General_NewAnalyticalModel.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_New);
            RibbonButton_General_NewAnalyticalModel.Click += RibbonButton_General_NewAnalyticalModel_Click;

            RibbonButton_General_SaveAnalyticalModel.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_Save);
            RibbonButton_General_SaveAnalyticalModel.Click += RibbonButton_General_SaveAnalyticalModel_Click;

            RibbonButton_General_SaveAsAnalyticalModel.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_SaveAs);
            RibbonButton_General_SaveAsAnalyticalModel.Click += RibbonButton_General_SaveAsAnalyticalModel_Click;

            RibbonButton_General_CloseAnalyticalModel.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_Close);
            RibbonButton_General_CloseAnalyticalModel.Click += RibbonButton_General_CloseAnalyticalModel_Click;


            RibbonButton_View_Section.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_Section);
            RibbonButton_View_Section.Click += RibbonButton_View_Section_Click;


            RibbonButton_Edit_Location.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_Location);
            RibbonButton_Edit_Location.Click += RibbonButton_Edit_Location_Click;

            RibbonButton_Edit_Properties.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_AnalyticalModelProperties);
            RibbonButton_Edit_Properties.Click += RibbonButton_Edit_Properties_Click;

            RibbonButton_Edit_Import.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_Import);
            RibbonButton_Edit_Import.Click += RibbonButton_Edit_Import_Click;

            RibbonButton_Edit_Check.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_ModelCheck);
            RibbonButton_Edit_Check.Click += RibbonButton_Edit_Check_Click;

            RibbonButton_Edit_Materials.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_MaterialLibrary);
            RibbonButton_Edit_Materials.Click += RibbonButton_Edit_Materials_Click;

            RibbonButton_Edit_InternalConditions.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_InternalCondition);
            RibbonButton_Edit_InternalConditions.Click += RibbonButton_Edit_InternalConditions_Click;

            RibbonButton_Edit_Profiles.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_ProfileLibrary);
            RibbonButton_Edit_Profiles.Click += RibbonButton_Edit_Profiles_Click;

            RibbonButton_Edit_Spaces.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_Space);
            RibbonButton_Edit_Spaces.Click += RibbonButton_Edit_Spaces_Click;

            RibbonButton_Edit_Constructions.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_ConstructionLibrary);
            RibbonButton_Edit_Constructions.Click += RibbonButton_Edit_Constructions_Click;

            RibbonButton_Edit_ApertureConstructions.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_ApertureConstruction);
            RibbonButton_Edit_ApertureConstructions.Click += RibbonButton_Edit_ApertureConstructions_Click;


            RibbonButton_Simulate_WeatherData.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_WeatherData);
            RibbonButton_Simulate_WeatherData.Click += RibbonButton_Simulate_WeatherData_Click;

            RibbonButton_Simulate_Import.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_Import);
            RibbonButton_Simulate_Import.Click += RibbonButton_Simulate_Import_Click;

            RibbonButton_Simulate_SolarSimulation.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_SolarCalculator);
            RibbonButton_Simulate_SolarSimulation.Click += RibbonButton_Simulate_SolarSimulation_Click;

            RibbonButton_Simulate_EnergySimulation.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_EnergySimulation);
            RibbonButton_Simulate_EnergySimulation.Click += RibbonButton_Simulate_EnergySimulation_Click;


            RibbonButton_Tools_EditLibrary.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_EditLibrary);
            RibbonButton_Tools_EditLibrary.Click += RibbonButton_Tools_EditLibrary_Click;

            RibbonButton_Tools_OpenT3D.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_T3D);
            RibbonButton_Tools_OpenT3D.Click += RibbonButton_Tools_OpenT3D_Click;

            RibbonButton_Tools_OpenTBD.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_TBD);
            RibbonButton_Tools_OpenTBD.Click += RibbonButton_Tools_OpenTBD_Click;

            RibbonButton_Tools_OpenTSD.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_TSD);
            RibbonButton_Tools_OpenTSD.Click += RibbonButton_Tools_OpenTSD_Click;

            RibbonButton_Tools_OpenTPD.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_TPD);
            RibbonButton_Tools_OpenTPD.Click += RibbonButton_Tools_OpenTPD_Click;

            RibbonButton_Tools_Hydra.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_Hydra);
            RibbonButton_Tools_Hydra.Click += RibbonButton_Tools_Hydra_Click;

            RibbonButton_Tools_Clean.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_Clean);
            RibbonButton_Tools_Clean.Click += RibbonButton_Tools_Clean_Click;

            RibbonButton_Tools_AddMissingObjects.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_AddMissingObjects);
            RibbonButton_Tools_AddMissingObjects.Click += RibbonButton_Tools_AddMissingObjects_Click;

            RibbonButton_Tools_PrintRoomDataSheets.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_PrintRDS);
            RibbonButton_Tools_PrintRoomDataSheets.Click += RibbonButton_Tools_PrintRoomDataSheets_Click;

            RibbonButton_Tools_OpenMollierChart.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_MollierDiagram);
            RibbonButton_Tools_OpenMollierChart.Click += RibbonButton_Tools_OpenMollierChart_Click;


            RibbonButton_Results_AirHandlingUnitDiagram.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_AirHandlingUnitDiagram);
            RibbonButton_Results_AirHandlingUnitDiagram.Click += RibbonButton_Results_AirHandlingUnitDiagram_Click;

            RibbonButton_Results_SpaceDiagram.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_SpaceDiagram);
            RibbonButton_Results_SpaceDiagram.Click += RibbonButton_Results_SpaceDiagram_Click;


            RibbonButton_Help_Wiki.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_Wiki);
            RibbonButton_Help_Wiki.Click += RibbonButton_Help_Wiki_Click;


            AnalyticalModelControl.TreeView.SelectedItemChanged += TreeView_Main_SelectedItemChanged;

            uIAnalyticalModel = new UIAnalyticalModel();
            uIAnalyticalModel.Modified += UIAnalyticalModel_Modified;

            View3DControl.UIAnalyticalModel = uIAnalyticalModel;
            AnalyticalModelControl.UIAnalyticalModel = uIAnalyticalModel;

            SetEnabled();
        }

        private void RibbonButton_View_Section_Click(object sender, RoutedEventArgs e)
        {
            double elevation = new Geometry.Spatial.BoundingBox3D(uIAnalyticalModel.JSAMObject.GetPanels().ConvertAll(x => x.GetBoundingBox())).Min.Z;
            elevation = Core.Query.Round(elevation, 0.01) + 0.1;
            using (Core.Windows.Forms.TextBoxForm<double> textBoxForm = new Core.Windows.Forms.TextBoxForm<double>("Height", "Insert Height"))
            {
                textBoxForm.Value = elevation;
                if(textBoxForm.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                {
                    return;
                }

                elevation = textBoxForm.Value;
            }

            if(double.IsNaN(elevation))
            {
                return;
            }

            TabItem tabItem = new TabItem();
            tabItem.Header = string.Format("Plan View ({0}m)", elevation);
            
            tabControl.Items.Add(tabItem);

            ViewControl viewControl = new ViewControl();

            tabItem.Content = viewControl;

            GeometryObjectModel geometryObjectModel = uIAnalyticalModel?.JSAMObject.ToSAM_GeometryObjectModel(Mode.TwoDimensional, Geometry.Spatial.Create.Plane(elevation));

            viewControl.UIGeometryObjectModel = new UIGeometryObjectModel(geometryObjectModel);
            viewControl.Mode = Mode.TwoDimensional;
            viewControl.Loaded += ViewControl_Loaded;
            viewControl.ObjectHoovered += ViewControl_ObjectHoovered;
            viewControl.ObjectDoubleClicked += ViewControl_ObjectDoubleClicked;
            viewControl.ObjectContextMenuOpening += ViewControl_ObjectContextMenuOpening;

            tabControl.SelectedItem = tabItem;
        }

        private void ViewControl_ObjectContextMenuOpening(object sender, ObjectContextMenuOpeningEventArgs e)
        {
            IVisualGeometryObject visualGeometryObject = e.VisualJSAMObject as IVisualGeometryObject;
            if (visualGeometryObject == null)
            {
                return;
            }

            if (!(visualGeometryObject.SAMGeometryObject is ITaggable))
            {
                return;
            }


            ContextMenu contextMenu = e.ContextMenu;

            //if (visualGeometryObject != null)
            //{
            //    menuItem = new MenuItem();
            //    menuItem.Name = "MenuItem_Properties";
            //    menuItem.Header = "Properties";
            //    menuItem.Click += MenuItem_Properties_Click;
            //    menuItem.Tag = hitTestResult;
            //    ContextMenu_Grid.Items.Add(menuItem);
            //}
        }

        private void ViewControl_ObjectDoubleClicked(object sender, ObjectDoubleClickedEventArgs e)
        {
            System.Windows.Window window =  (sender as ViewControl).Window();

            IVisualGeometryObject visualGeometryObject = e.VisualJSAMObject as IVisualGeometryObject;
            if (visualGeometryObject == null)
            {
                return;
            }

            if (!(visualGeometryObject.SAMGeometryObject is ITaggable))
            {
                return;
            }

            Tag tag = ((ITaggable)visualGeometryObject.SAMGeometryObject).Tag;
            if (tag.Value is Panel)
            {
                Panel panel = (Panel)tag.Value;
                uIAnalyticalModel.EditPanel(panel, new Core.Windows.WindowHandle(window));
            }
            else if (tag.Value is Space)
            {
                Space space = (Space)tag.Value;
                uIAnalyticalModel.EditSpace(space, new Core.Windows.WindowHandle(window));
            }
        }

        private void ViewControl_Loaded(object sender, RoutedEventArgs e)
        {
            ViewControl viewControl = sender as ViewControl;
            if(viewControl == null)
            {
                return;
            }

            viewControl.CenterView();
        }

        private void ViewControl_ObjectHoovered(object sender, ObjectHooveredEventArgs e)
        {
            ViewControl viewControl = sender as ViewControl;
            viewControl.Hint = string.Empty;

            IVisualGeometryObject visualGeometryObject = e.VisualJSAMObject as IVisualGeometryObject;
            if (visualGeometryObject == null)
            {
                return;
            }

            if (!(visualGeometryObject.SAMGeometryObject is ITaggable))
            {
                return;
            }

            Tag tag = ((ITaggable)visualGeometryObject.SAMGeometryObject).Tag;
            if (tag.Value is Panel)
            {
                Panel panel = (Panel)tag.Value;
                viewControl.Hint = string.Format("Panel {0}, Guid: {1}", panel.Name, panel.Guid);
            }
            else if (tag.Value is Space)
            {
                Space space = (Space)tag.Value;
                viewControl.Hint = string.Format("Space {0}, Guid: {1}", space.Name, space.Guid);

                InternalCondition internalCondition = space.InternalCondition;
                if(internalCondition != null)
                {
                    viewControl.Hint += string.Format(", IC: {0}", internalCondition.Name);
                }
            }
        }

        private void RibbonButton_Tools_OpenMollierChart_Click(object sender, RoutedEventArgs e)
        {
            using(Core.Mollier.UI.MollierForm mollierForm = new Core.Mollier.UI.MollierForm())
            {
                if(mollierForm.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                {
                    return;
                }
            }
        }

        private void RibbonButton_Results_SpaceDiagram_Click(object sender, RoutedEventArgs e)
        {
            uIAnalyticalModel?.SpaceDiagram(windowHandle);
        }

        private void RibbonButton_Results_AirHandlingUnitDiagram_Click(object sender, RoutedEventArgs e)
        {
            uIAnalyticalModel?.AirHandlingUnitDiagram(windowHandle);
        }

        private void RibbonButton_Help_Wiki_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/HoareLea/SAM/wiki/00-Home");
        }

        private void RibbonButton_Tools_PrintRoomDataSheets_Click(object sender, RoutedEventArgs e)
        {
            uIAnalyticalModel?.PrintRoomDataSheets(windowHandle);
        }

        private void RibbonButton_Tools_AddMissingObjects_Click(object sender, RoutedEventArgs e)
        {
            uIAnalyticalModel?.AddMissingObjects(windowHandle);
        }

        private void RibbonButton_Tools_Clean_Click(object sender, RoutedEventArgs e)
        {
            uIAnalyticalModel?.Clean(windowHandle);
        }

        private void RibbonButton_Tools_Hydra_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://hlhydra.azurewebsites.net/index.html");
        }

        private void RibbonButton_Tools_OpenTPD_Click(object sender, RoutedEventArgs e)
        {
            string path = Core.Tas.Query.TPDPath();

            if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path))
            {
                return;
            }

            System.Diagnostics.Process.Start(path);
        }

        private void RibbonButton_Tools_OpenTSD_Click(object sender, RoutedEventArgs e)
        {
            string path = Core.Tas.Query.TSDPath();

            if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path))
            {
                return;
            }

            System.Diagnostics.Process.Start(path);
        }

        private void RibbonButton_Tools_OpenTBD_Click(object sender, RoutedEventArgs e)
        {
            string path = Core.Tas.Query.TBDPath();

            if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path))
            {
                return;
            }

            System.Diagnostics.Process.Start(path);
        }

        private void RibbonButton_Tools_OpenT3D_Click(object sender, RoutedEventArgs e)
        {
            string path = Core.Tas.Query.TAS3DPath();

            if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path))
            {
                return;
            }

            System.Diagnostics.Process.Start(path);
        }

        private void RibbonButton_Tools_EditLibrary_Click(object sender, RoutedEventArgs e)
        {
            Modify.EditLibrary(windowHandle);
        }

        private void RibbonButton_Simulate_EnergySimulation_Click(object sender, RoutedEventArgs e)
        {
            uIAnalyticalModel?.EnergySimulation(windowHandle);
        }

        private void RibbonButton_Simulate_SolarSimulation_Click(object sender, RoutedEventArgs e)
        {
            uIAnalyticalModel?.SolarSimulation(windowHandle);
        }

        private void RibbonButton_Simulate_Import_Click(object sender, RoutedEventArgs e)
        {
            uIAnalyticalModel?.ImportWeatherData(windowHandle);
        }

        private void RibbonButton_Simulate_WeatherData_Click(object sender, RoutedEventArgs e)
        {
            uIAnalyticalModel?.EditWeatherData(windowHandle);
        }

        private void RibbonButton_Edit_ApertureConstructions_Click(object sender, RoutedEventArgs e)
        {
            uIAnalyticalModel?.EditApertureConstructions(windowHandle);
        }

        private void RibbonButton_Edit_Constructions_Click(object sender, RoutedEventArgs e)
        {
            uIAnalyticalModel?.EditConstructions(windowHandle);
        }

        private void RibbonButton_Edit_Spaces_Click(object sender, RoutedEventArgs e)
        {
            uIAnalyticalModel?.EditSpaces(windowHandle);
        }

        private void RibbonButton_Edit_Profiles_Click(object sender, RoutedEventArgs e)
        {
            uIAnalyticalModel?.EditProfileLibrary(windowHandle);
        }

        private void RibbonButton_Edit_InternalConditions_Click(object sender, RoutedEventArgs e)
        {
            uIAnalyticalModel?.EditInternalConditions(windowHandle);
        }

        private void RibbonButton_Edit_Materials_Click(object sender, RoutedEventArgs e)
        {
            uIAnalyticalModel?.EditMaterialLibrary(windowHandle);
        }

        private void RibbonButton_Edit_Check_Click(object sender, RoutedEventArgs e)
        {
            uIAnalyticalModel?.Check(windowHandle);
        }

        private void RibbonButton_Edit_Import_Click(object sender, RoutedEventArgs e)
        {
            uIAnalyticalModel?.Import(windowHandle);
        }

        private void RibbonButton_Edit_Properties_Click(object sender, RoutedEventArgs e)
        {
            uIAnalyticalModel?.EditProperties(windowHandle);
        }

        private void RibbonButton_Edit_Location_Click(object sender, RoutedEventArgs e)
        {
            uIAnalyticalModel?.EditAddressAndLocation();
        }

        private void RibbonButton_General_CloseAnalyticalModel_Click(object sender, RoutedEventArgs e)
        {
            if (uIAnalyticalModel == null)
            {
                return;
            }

            uIAnalyticalModel.Close();
        }

        private void RibbonButton_General_SaveAsAnalyticalModel_Click(object sender, RoutedEventArgs e)
        {
            if (uIAnalyticalModel == null)
            {
                return;
            }

            uIAnalyticalModel.SaveAs();
        }

        private void RibbonButton_General_SaveAnalyticalModel_Click(object sender, RoutedEventArgs e)
        {
            if (uIAnalyticalModel == null)
            {
                return;
            }

            if (uIAnalyticalModel.Save())
            {

            }
        }

        private void RibbonButton_General_NewAnalyticalModel_Click(object sender, RoutedEventArgs e)
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

        private void RibbonButton_General_OpenAnalyticalModel_Click(object sender, RoutedEventArgs e)
        {
            if(uIAnalyticalModel == null)
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


            if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path))
            {
                return;
            }

            UIAnalyticalModel uIAnalyticalModel_Temp = new UIAnalyticalModel();
            uIAnalyticalModel_Temp.Path = path;

            Core.Windows.Forms.MarqueeProgressForm.Show("Opening AnalyticalModel", () => uIAnalyticalModel_Temp.Open());

            uIAnalyticalModel.Path = path;
            uIAnalyticalModel.JSAMObject = uIAnalyticalModel_Temp?.JSAMObject;

            View3DControl.CenterView();
        }

        private void TreeView_Main_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            TreeView treeView_AnalyticalModel = AnalyticalModelControl.TreeView;

            List<IJSAMObject> jSAMObjects = new List<IJSAMObject>();
            TreeViewItem treeViewItem = treeView_AnalyticalModel.SelectedItem as TreeViewItem;
            if (treeViewItem != null)
            {
                IJSAMObject jSAMObject = treeViewItem.Tag as IJSAMObject;
                if (jSAMObject != null)
                {
                    jSAMObjects.Add(jSAMObject);
                }
            }

            View3DControl.Show(jSAMObjects);
        }

        private void UIAnalyticalModel_Modified(object sender, EventArgs e)
        {
            SetEnabled();

            foreach(TabItem tabItem in tabControl.Items)
            {
                ViewControl viewControl = tabItem?.Content as ViewControl;

                if (viewControl != null)
                {
                    GeometryObjectModel geometryObjectModel = viewControl.UIGeometryObjectModel.JSAMObject;
                    if(geometryObjectModel.TryGetValue(GeometryObjectModelParameter.SectionPlane, out Geometry.Spatial.Plane plane))
                    {
                        geometryObjectModel = uIAnalyticalModel?.JSAMObject.ToSAM_GeometryObjectModel(Mode.TwoDimensional, geometryObjectModel.GetValue<Geometry.Spatial.Plane>(GeometryObjectModelParameter.SectionPlane));
                        viewControl.UIGeometryObjectModel = new UIGeometryObjectModel(geometryObjectModel);
                    }
                }
            }
        }

        private void SetEnabled()
        {
            RibbonButton_Tools_OpenMollierChart.IsEnabled = false;
            RibbonButton_Results_SpaceDiagram.IsEnabled = false;
            RibbonButton_Results_AirHandlingUnitDiagram.IsEnabled = false;
            RibbonButton_Help_Wiki.IsEnabled = false;
            RibbonButton_Tools_PrintRoomDataSheets.IsEnabled = false;
            RibbonButton_Tools_AddMissingObjects.IsEnabled = false;
            RibbonButton_Tools_Clean.IsEnabled = false;
            RibbonButton_Tools_Hydra.IsEnabled = false;
            RibbonButton_Tools_OpenTPD.IsEnabled = false;
            RibbonButton_Tools_OpenTSD.IsEnabled = false;
            RibbonButton_Tools_OpenTBD.IsEnabled = false;
            RibbonButton_Tools_OpenT3D.IsEnabled = false;
            RibbonButton_Tools_EditLibrary.IsEnabled = false;
            RibbonButton_Simulate_EnergySimulation.IsEnabled = false;
            RibbonButton_Simulate_SolarSimulation.IsEnabled = false;
            RibbonButton_Simulate_Import.IsEnabled = false;
            RibbonButton_Simulate_WeatherData.IsEnabled = false;
            RibbonButton_Edit_ApertureConstructions.IsEnabled = false;
            RibbonButton_Edit_Constructions.IsEnabled = false;
            RibbonButton_Edit_Spaces.IsEnabled = false;
            RibbonButton_Edit_Profiles.IsEnabled = false;
            RibbonButton_Edit_InternalConditions.IsEnabled = false;
            RibbonButton_Edit_Materials.IsEnabled = false;
            RibbonButton_Edit_Check.IsEnabled = false;
            RibbonButton_Edit_Import.IsEnabled = false;
            RibbonButton_Tools_Hydra.IsEnabled = false;
            RibbonButton_Tools_OpenT3D.IsEnabled = false;
            RibbonButton_Tools_OpenTBD.IsEnabled = false;
            RibbonButton_Tools_OpenTPD.IsEnabled = false;
            RibbonButton_Tools_OpenTSD.IsEnabled = false;
            RibbonButton_Tools_EditLibrary.IsEnabled = false;
            RibbonButton_Simulate_EnergySimulation.IsEnabled = false;
            RibbonButton_Simulate_SolarSimulation.IsEnabled = false;
            RibbonButton_Simulate_Import.IsEnabled = false;
            RibbonButton_Simulate_WeatherData.IsEnabled = false;
            RibbonButton_Edit_ApertureConstructions.IsEnabled = false;
            RibbonButton_Edit_Constructions.IsEnabled = false;
            RibbonButton_Edit_Spaces.IsEnabled = false;
            RibbonButton_Edit_Profiles.IsEnabled = false;
            RibbonButton_Edit_InternalConditions.IsEnabled = false;
            RibbonButton_Edit_Materials.IsEnabled = false;
            RibbonButton_Edit_Check.IsEnabled = false;
            RibbonButton_Edit_Import.IsEnabled = false;
            RibbonButton_Edit_Properties.IsEnabled = false;
            RibbonButton_Edit_Location.IsEnabled = false;
            RibbonButton_General_CloseAnalyticalModel.IsEnabled = false;
            RibbonButton_General_SaveAsAnalyticalModel.IsEnabled = false;
            RibbonButton_General_SaveAnalyticalModel.IsEnabled = false;
            RibbonButton_General_NewAnalyticalModel.IsEnabled = false;
            RibbonButton_General_OpenAnalyticalModel.IsEnabled = false;

            RibbonButton_Tools_OpenMollierChart.IsEnabled = true;
            RibbonButton_Help_Wiki.IsEnabled = true;
            RibbonButton_Tools_Hydra.IsEnabled = true;
            RibbonButton_Tools_OpenTPD.IsEnabled = true;
            RibbonButton_Tools_OpenTSD.IsEnabled = true;
            RibbonButton_Tools_OpenTBD.IsEnabled = true;
            RibbonButton_Tools_OpenT3D.IsEnabled = true;
            RibbonButton_General_NewAnalyticalModel.IsEnabled = true;
            RibbonButton_General_OpenAnalyticalModel.IsEnabled = true;
            RibbonButton_Tools_EditLibrary.IsEnabled = true;

            AnalyticalModel analyticalModel = uIAnalyticalModel.JSAMObject;
            if (analyticalModel != null)
            {
                RibbonButton_Tools_PrintRoomDataSheets.IsEnabled = true;
                RibbonButton_Tools_AddMissingObjects.IsEnabled = true;
                RibbonButton_Tools_Clean.IsEnabled = true;
                RibbonButton_Simulate_EnergySimulation.IsEnabled = true;
                RibbonButton_Simulate_SolarSimulation.IsEnabled = true;
                RibbonButton_Simulate_Import.IsEnabled = true;
                RibbonButton_Simulate_WeatherData.IsEnabled = true;
                RibbonButton_Edit_ApertureConstructions.IsEnabled = true;
                RibbonButton_Edit_Constructions.IsEnabled = true;
                RibbonButton_Edit_Spaces.IsEnabled = true;
                RibbonButton_Edit_Profiles.IsEnabled = true;
                RibbonButton_Edit_InternalConditions.IsEnabled = true;
                RibbonButton_Edit_Materials.IsEnabled = true;
                RibbonButton_Edit_Check.IsEnabled = true;
                RibbonButton_Edit_Import.IsEnabled = true;
                RibbonButton_Edit_Properties.IsEnabled = true;
                RibbonButton_Edit_Location.IsEnabled = true;
                RibbonButton_General_CloseAnalyticalModel.IsEnabled = true;
                RibbonButton_General_SaveAsAnalyticalModel.IsEnabled = true;
                RibbonButton_General_SaveAnalyticalModel.IsEnabled = true;

                List<AirHandlingUnit> airHandlingUnits = analyticalModel.AdjacencyCluster?.GetObjects<AirHandlingUnit>();
                if(airHandlingUnits != null && airHandlingUnits.Count != 0)
                {
                    RibbonButton_Results_SpaceDiagram.IsEnabled = true;
                    RibbonButton_Results_AirHandlingUnitDiagram.IsEnabled = true;
                }

            }
        }
    }
}
