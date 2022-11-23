using Microsoft.Win32;
using SAM.Core;
using SAM.Core.UI.WPF;
using SAM.Geometry;
using SAM.Geometry.Spatial;
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

            RibbonButton_Tools_ViewGeometry.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_Space);
            RibbonButton_Tools_ViewGeometry.Click += RibbonButton_Tools_ViewGeometry_Click;


            RibbonButton_Results_AirHandlingUnitDiagram.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_AirHandlingUnitDiagram);
            RibbonButton_Results_AirHandlingUnitDiagram.Click += RibbonButton_Results_AirHandlingUnitDiagram_Click;

            RibbonButton_Results_SpaceDiagram.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_SpaceDiagram);
            RibbonButton_Results_SpaceDiagram.Click += RibbonButton_Results_SpaceDiagram_Click;


            RibbonButton_Help_Wiki.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_Wiki);
            RibbonButton_Help_Wiki.Click += RibbonButton_Help_Wiki_Click;


            AnalyticalModelControl.TreeView.SelectedItemChanged += TreeView_Main_SelectedItemChanged;


            ThreeDimensionalViewSettings threeDimensionalViewSettings = new ThreeDimensionalViewSettings(0, null, null);

            GeometryObjectModel geometryObjectModel = UI.Convert.ToSAM_GeometryObjectModel(uIAnalyticalModel?.JSAMObject, threeDimensionalViewSettings);

            viewportControl.UIGeometryObjectModel = new UIGeometryObjectModel(geometryObjectModel);
            viewportControl.Mode = threeDimensionalViewSettings.Mode();
            viewportControl.Loaded += ViewportControl_Loaded;
            viewportControl.ObjectHoovered += ViewportControl_ObjectHoovered;
            viewportControl.ObjectDoubleClicked += ViewportControl_ObjectDoubleClicked;
            viewportControl.ObjectContextMenuOpening += ViewControl_ObjectContextMenuOpening;

            uIAnalyticalModel = new UIAnalyticalModel();
            uIAnalyticalModel.Modified += UIAnalyticalModel_Modified;
            uIAnalyticalModel.Closed += UIAnalyticalModel_Closed;
            uIAnalyticalModel.Opened += UIAnalyticalModel_Opened;

            SetEnabled();
        }

        private void RibbonButton_Tools_ViewGeometry_Click(object sender, RoutedEventArgs e)
        {
            GeometryWindow geometryWindow = new GeometryWindow();
            geometryWindow.Show();
        }

        private void RibbonButton_View_Section_Click(object sender, RoutedEventArgs e)
        {
            if(uIAnalyticalModel == null)
            {
                return;
            }

            AnalyticalModel analyticalModel = uIAnalyticalModel.JSAMObject;
            if(analyticalModel == null)
            {
                return;
            }


            List<BoundingBox3D> boundingBox3Ds = analyticalModel.GetPanels()?.ConvertAll(x => x?.GetBoundingBox());
            if(boundingBox3Ds == null || boundingBox3Ds.Count == 0)
            {
                return;
            }

            boundingBox3Ds.RemoveAll(x => x == null || !x.IsValid());

            if(boundingBox3Ds == null || boundingBox3Ds.Count == 0)
            {
                return;
            }

            double elevation = new BoundingBox3D(boundingBox3Ds).Min.Z;
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

            TabItem tabItem = UpdateTabItem(tabControl, analyticalModel, new TwoDimensionalViewSettings(tabControl.Items.Count, Geometry.Spatial.Create.Plane(elevation), null, new Type[] { typeof(Space), typeof(Panel), typeof(Aperture)}));
            if(tabItem != null)
            {
                tabControl.SelectedItem = tabItem;
            }

            UpdateUIGeometrySettings(tabControl, analyticalModel);

            uIAnalyticalModel.Modified -= UIAnalyticalModel_Modified;
            uIAnalyticalModel.JSAMObject = analyticalModel;
            uIAnalyticalModel.Modified += UIAnalyticalModel_Modified;

            //TabItem tabItem = new TabItem();
            //tabItem.Header = string.Format("Plan View ({0}m)", elevation);

            //int id = tabControl.Items.Add(tabItem);

            //ViewportControl viewportControl = new ViewportControl();

            //tabItem.Content = viewportControl;

            //TwoDimensionalViewSettings twoDimensionalViewSettings = new TwoDimensionalViewSettings(id, Geometry.Spatial.Create.Plane(elevation), null, null);

            //GeometryObjectModel geometryObjectModel = uIAnalyticalModel?.JSAMObject.ToSAM_GeometryObjectModel(twoDimensionalViewSettings);

            //viewportControl.UIGeometryObjectModel = new UIGeometryObjectModel(geometryObjectModel);
            //viewportControl.Mode = twoDimensionalViewSettings.Mode();
            //viewportControl.Loaded += ViewportControl_Loaded;
            //viewportControl.ObjectHoovered += ViewportControl_ObjectHoovered;
            //viewportControl.ObjectDoubleClicked += ViewportControl_ObjectDoubleClicked;
            //viewportControl.ObjectContextMenuOpening += ViewControl_ObjectContextMenuOpening;

            //tabControl.SelectedItem = tabItem;
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



            IJSAMObject jSAMObject = null;
            Tag tag = ((ITaggable)visualGeometryObject.SAMGeometryObject).Tag;
            if (tag.Value is Panel)
            {
                jSAMObject = (Panel)tag.Value;
            }
            else if (tag.Value is Space)
            {
                jSAMObject = (Space)tag.Value;
            }

            if(jSAMObject == null)
            {
                return;
            }

            ContextMenu contextMenu = e.ContextMenu;

            MenuItem menuItem = new MenuItem();
            menuItem.Name = "MenuItem_Properties";
            menuItem.Header = "Properties";
            menuItem.Click += MenuItem_Properties_Click;
            menuItem.Tag = jSAMObject;
            contextMenu.Items.Add(menuItem);
        }

        private void MenuItem_Properties_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)sender;
            if (menuItem == null)
            {
                return;
            }

            IJSAMObject jSAMObject = menuItem.Tag as IJSAMObject;
            if (jSAMObject is Panel)
            {
                Panel panel = (Panel)jSAMObject;
                uIAnalyticalModel.EditPanel(panel, new Core.Windows.WindowHandle(this));
            }
            else if (jSAMObject is Space)
            {
                Space space = (Space)jSAMObject;
                uIAnalyticalModel.EditSpace(space, new Core.Windows.WindowHandle(this));
            }
        }

        private void ViewportControl_ObjectDoubleClicked(object sender, ObjectDoubleClickedEventArgs e)
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

            Tag tag = ((ITaggable)visualGeometryObject.SAMGeometryObject).Tag;
            if(tag == null)
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
        }

        private void ViewportControl_Loaded(object sender, RoutedEventArgs e)
        {
            ViewportControl viewportControl = sender as ViewportControl;
            if(viewportControl == null)
            {
                return;
            }
        }

        private void ViewportControl_ObjectHoovered(object sender, ObjectHooveredEventArgs e)
        {
            ViewportControl viewportControl = sender as ViewportControl;

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
            if(tag == null)
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
                if(internalCondition != null)
                {
                    //viewportControl.Hint += string.Format(", IC: {0}", internalCondition.Name);
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

            AnalyticalModel analyticalModel = uIAnalyticalModel.JSAMObject;

            UpdateUIGeometrySettings(tabControl, analyticalModel);

            uIAnalyticalModel.Modified -= UIAnalyticalModel_Modified;
            uIAnalyticalModel.JSAMObject = analyticalModel;
            uIAnalyticalModel.Modified += UIAnalyticalModel_Modified;

            uIAnalyticalModel.SaveAs();
        }

        private void RibbonButton_General_SaveAnalyticalModel_Click(object sender, RoutedEventArgs e)
        {
            if (uIAnalyticalModel == null)
            {
                return;
            }

            AnalyticalModel analyticalModel = uIAnalyticalModel.JSAMObject;

            UpdateUIGeometrySettings(tabControl, analyticalModel);

            uIAnalyticalModel.Modified -= UIAnalyticalModel_Modified;
            uIAnalyticalModel.JSAMObject = analyticalModel;
            uIAnalyticalModel.Modified += UIAnalyticalModel_Modified;

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

            uIAnalyticalModel = new UIAnalyticalModel();
            uIAnalyticalModel.Path = path;
            uIAnalyticalModel.Modified += UIAnalyticalModel_Modified;
            uIAnalyticalModel.Closed += UIAnalyticalModel_Closed;
            uIAnalyticalModel.Opened += UIAnalyticalModel_Opened;

            uIAnalyticalModel.Open();

            //Core.Windows.Forms.MarqueeProgressForm.Show("Opening AnalyticalModel", () => );
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
        }

        private void UIAnalyticalModel_Modified(object sender, EventArgs e)
        {
            Reload();
        }
        
        private void UIAnalyticalModel_Opened(object sender, EventArgs e)
        {
            SetViewSettings();
            Reload();
        }

        private void UIAnalyticalModel_Closed(object sender, EventArgs e)
        {
            Reload();
        }

        private TabItem UpdateTabItem(TabControl tabControl, AnalyticalModel analyticalModel, IViewSettings viewSettings = null)
        {
            if (tabControl == null || analyticalModel == null)
            {
                return null;
            }

            int id = -1;
            if (viewSettings == null)
            {
                id = tabControl.Items.Count;
                viewSettings = UI.Query.DefaultViewSettings(id);
            }

            id = viewSettings.Id;

            while (id > tabControl.Items.Count - 1)
            {
                tabControl.Items.Add(new TabItem() { Header = "???" });
            }

            TabItem tabItem = tabControl.Items[id] as TabItem;
            if (tabItem == null)
            {
                return null;
            }

            ViewportControl viewportControl = tabItem.Content as ViewportControl;
            if (viewportControl == null)
            {
                viewportControl = new ViewportControl();
                viewportControl.Loaded += ViewportControl_Loaded;
                viewportControl.ObjectHoovered += ViewportControl_ObjectHoovered;
                viewportControl.ObjectDoubleClicked += ViewportControl_ObjectDoubleClicked;
                viewportControl.ObjectContextMenuOpening += ViewControl_ObjectContextMenuOpening;
                tabItem.Content = viewportControl;
            }

            string header = viewSettings.Header();
            if (tabItem.Header?.ToString() != header)
            {
                tabItem.Header = header;
            }

            GeometryObjectModel geometryObjectModel = analyticalModel.ToSAM_GeometryObjectModel(viewSettings);
            viewportControl.UIGeometryObjectModel = new UIGeometryObjectModel(geometryObjectModel);
            viewportControl.Mode = viewSettings.Mode();

            return tabItem;
        }

        private List<TabItem> UpdateTabItems(TabControl tabControl, AnalyticalModel analyticalModel)
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
                        TabItem tabItem = UpdateTabItem(tabControl, analyticalModel, viewSettings);
                        if (tabItem != null)
                        {
                            result.Add(tabItem);
                        }

                        int id = tabControl.Items.IndexOf(tabItem);

                        if (id != -1 && id == uIGeometrySettings.Id)
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

        private UIGeometrySettings UpdateUIGeometrySettings(TabControl tabControl, AnalyticalModel analyticalModel)
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

                viewSettingsList.Add(viewSettings);

                if(tabControl.SelectedItem == tabItem)
                {
                    result.Id = i;
                }
            }

            result.SetViewSettings(viewSettingsList);

            return result;
        }

        private void SetViewSettings()
        {
            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if(analyticalModel == null)
            {
                return;
            }

            if (!analyticalModel.TryGetValue(AnalyticalModelParameter.UIGeometrySettings, out UIGeometrySettings uIGeometrySettings) || uIGeometrySettings == null)
            {
                uIGeometrySettings = new UIGeometrySettings();
            }

            List<IViewSettings> viewSettingsList = uIGeometrySettings.GetViewSettings<IViewSettings>();
            if(viewSettingsList == null || viewSettingsList.Count == 0)
            {
                uIGeometrySettings.AddViewSettings(UI.Query.DefaultViewSettings(tabControl.Items.Count));
                analyticalModel.SetValue(AnalyticalModelParameter.UIGeometrySettings, uIGeometrySettings);
                uIAnalyticalModel.Modified -= UIAnalyticalModel_Modified;
                uIAnalyticalModel.JSAMObject = analyticalModel;
                uIAnalyticalModel.Modified += UIAnalyticalModel_Modified;
            }
        }

        private void Reload()
        {
            SetEnabled();

            uIAnalyticalModel.Modified -= UIAnalyticalModel_Modified;

            AnalyticalModel analyticalModel = uIAnalyticalModel.JSAMObject;

            UpdateTabItems(tabControl, analyticalModel);

            UpdateUIGeometrySettings(tabControl, analyticalModel);

            //if(!analyticalModel.TryGetValue(AnalyticalModelParameter.UIGeometrySettings, out UIGeometrySettings uIGeometrySettings) || uIGeometrySettings == null)
            //{
            //    uIGeometrySettings = new UIGeometrySettings();
            //}

            //List<IViewSettings> viewSettingsList = new List<IViewSettings>();
            //for(int i =0; i < tabControl.Items.Count; i++)
            //{
            //    TabItem tabItem = tabControl.Items[i] as TabItem;

            //    ViewportControl viewportControl = tabItem?.Content as ViewportControl;
            //    if (viewportControl == null)
            //    {
            //        continue;
            //    }

            //    IViewSettings viewSettings = null;

            //    GeometryObjectModel geometryObjectModel = viewportControl.UIGeometryObjectModel?.JSAMObject;
            //    if(geometryObjectModel == null)
            //    {
            //        viewSettings = uIGeometrySettings.GetViewSettings(i);
            //        if(viewSettings == null)
            //        {
            //            viewSettings = UI.Query.DefaultViewSettings(i);
            //        }
            //    }

            //    if(viewSettings == null)
            //    {
            //        if (!geometryObjectModel.TryGetValue(GeometryObjectModelParameter.ViewSettings, out viewSettings) || viewSettings == null)
            //        {
            //            continue;
            //        }
            //    }

            //    geometryObjectModel = uIAnalyticalModel?.JSAMObject.ToSAM_GeometryObjectModel(viewSettings);
            //    viewportControl.UIGeometryObjectModel = new UIGeometryObjectModel(geometryObjectModel);

            //    viewSettingsList.Add(viewSettings);
            //}

            //uIGeometrySettings.SetViewSettings(viewSettingsList);

            //analyticalModel.SetValue(AnalyticalModelParameter.UIGeometrySettings, uIGeometrySettings);

            uIAnalyticalModel.JSAMObject = analyticalModel;

            uIAnalyticalModel.Modified += UIAnalyticalModel_Modified;
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
