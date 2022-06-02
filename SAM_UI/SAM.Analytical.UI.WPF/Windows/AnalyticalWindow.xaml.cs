using Microsoft.Win32;
using SAM.Core;
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
        private UIAnalyticalModel uIAnalyticalModel;

        public AnalyticalWindow()
        {
            InitializeComponent();

            Icon = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM);

            RibbonButton_General_OpenAnalyticalModel.Click += RibbonButton_General_OpenAnalyticalModel_Click;
            RibbonButton_General_NewAnalyticalModel.Click += RibbonButton_General_NewAnalyticalModel_Click;
            RibbonButton_General_SaveAnalyticalModel.Click += RibbonButton_General_SaveAnalyticalModel_Click;
            RibbonButton_General_SaveAsAnalyticalModel.Click += RibbonButton_General_SaveAsAnalyticalModel_Click;
            RibbonButton_General_CloseAnalyticalModel.Click += RibbonButton_General_CloseAnalyticalModel_Click;

            RibbonButton_Edit_Location.Click += RibbonButton_Edit_Location_Click;
            RibbonButton_Edit_Properties.Click += RibbonButton_Edit_Properties_Click;
            RibbonButton_Edit_Import.Click += RibbonButton_Edit_Import_Click;
            RibbonButton_Edit_Check.Click += RibbonButton_Edit_Check_Click;
            RibbonButton_Edit_Materials.Click += RibbonButton_Edit_Materials_Click;
            RibbonButton_Edit_InternalConditions.Click += RibbonButton_Edit_InternalConditions_Click;
            RibbonButton_Edit_Profiles.Click += RibbonButton_Edit_Profiles_Click;
            RibbonButton_Edit_Spaces.Click += RibbonButton_Edit_Spaces_Click;
            RibbonButton_Edit_Constructions.Click += RibbonButton_Edit_Constructions_Click;
            RibbonButton_Edit_ApertureConstructions.Click += RibbonButton_Edit_ApertureConstructions_Click;

            RibbonButton_Simulate_WeatherData.Click += RibbonButton_Simulate_WeatherData_Click;
            RibbonButton_Simulate_Import.Click += RibbonButton_Simulate_Import_Click;
            RibbonButton_Simulate_SolarSimulation.Click += RibbonButton_Simulate_SolarSimulation_Click;
            RibbonButton_Simulate_EnergySimulation.Click += RibbonButton_Simulate_EnergySimulation_Click;

            RibbonButton_Tools_EditLibrary.Click += RibbonButton_Tools_EditLibrary_Click;
            RibbonButton_Tools_OpenT3D.Click += RibbonButton_Tools_OpenT3D_Click;
            RibbonButton_Tools_OpenTBD.Click += RibbonButton_Tools_OpenTBD_Click;
            RibbonButton_Tools_OpenTSD.Click += RibbonButton_Tools_OpenTSD_Click;
            RibbonButton_Tools_OpenTPD.Click += RibbonButton_Tools_OpenTPD_Click;
            RibbonButton_Tools_Hydra.Click += RibbonButton_Tools_Hydra_Click;
            RibbonButton_Tools_Clean.Click += RibbonButton_Tools_Clean_Click;
            RibbonButton_Tools_AddMissingObjects.Click += RibbonButton_Tools_AddMissingObjects_Click;
            RibbonButton_Tools_PrintRoomDataSheets.Click += RibbonButton_Tools_PrintRoomDataSheets_Click;

            RibbonButton_Help_Wiki.Click += RibbonButton_Help_Wiki_Click;

            AnalyticalModelControl.TreeView_Main.SelectedItemChanged += TreeView_Main_SelectedItemChanged;

            uIAnalyticalModel = new UIAnalyticalModel();
            uIAnalyticalModel.Modified += UIAnalyticalModel_Modified;

            View3DControl.UIAnalyticalModel = uIAnalyticalModel;
        }

        private void RibbonButton_Help_Wiki_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/HoareLea/SAM/wiki/00-Home");
        }

        private void RibbonButton_Tools_PrintRoomDataSheets_Click(object sender, RoutedEventArgs e)
        {
            uIAnalyticalModel?.PrintRoomDataSheets();
        }

        private void RibbonButton_Tools_AddMissingObjects_Click(object sender, RoutedEventArgs e)
        {
            uIAnalyticalModel?.AddMissingObjects();
        }

        private void RibbonButton_Tools_Clean_Click(object sender, RoutedEventArgs e)
        {
            uIAnalyticalModel?.Clean();
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
            UI.Modify.EditLibrary();
        }

        private void RibbonButton_Simulate_EnergySimulation_Click(object sender, RoutedEventArgs e)
        {
            uIAnalyticalModel?.EnergySimulation();
        }

        private void RibbonButton_Simulate_SolarSimulation_Click(object sender, RoutedEventArgs e)
        {
            uIAnalyticalModel?.SolarSimulation();
        }

        private void RibbonButton_Simulate_Import_Click(object sender, RoutedEventArgs e)
        {
            uIAnalyticalModel?.ImportWeatherData();
        }

        private void RibbonButton_Simulate_WeatherData_Click(object sender, RoutedEventArgs e)
        {
            uIAnalyticalModel?.EditWeatherData();
        }

        private void RibbonButton_Edit_ApertureConstructions_Click(object sender, RoutedEventArgs e)
        {
            uIAnalyticalModel?.EditApertureConstructions();
        }

        private void RibbonButton_Edit_Constructions_Click(object sender, RoutedEventArgs e)
        {
            uIAnalyticalModel?.EditConstructions();
        }

        private void RibbonButton_Edit_Spaces_Click(object sender, RoutedEventArgs e)
        {
            uIAnalyticalModel?.EditSpaces();
        }

        private void RibbonButton_Edit_Profiles_Click(object sender, RoutedEventArgs e)
        {
            uIAnalyticalModel?.EditProfileLibrary();
        }

        private void RibbonButton_Edit_InternalConditions_Click(object sender, RoutedEventArgs e)
        {
            uIAnalyticalModel?.EditInternalConditions();
        }

        private void RibbonButton_Edit_Materials_Click(object sender, RoutedEventArgs e)
        {
            uIAnalyticalModel?.EditMaterialLibrary();
        }

        private void RibbonButton_Edit_Check_Click(object sender, RoutedEventArgs e)
        {
            uIAnalyticalModel?.Check();
        }

        private void RibbonButton_Edit_Import_Click(object sender, RoutedEventArgs e)
        {
            uIAnalyticalModel?.Import();
        }

        private void RibbonButton_Edit_Properties_Click(object sender, RoutedEventArgs e)
        {
            uIAnalyticalModel?.EditProperties();
        }

        private void TreeView_Main_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            TreeView treeView_AnalyticalModel = AnalyticalModelControl.TreeView_Main;

            List<IJSAMObject> jSAMObjects = new List<IJSAMObject>();
            TreeViewItem treeViewItem = treeView_AnalyticalModel.SelectedItem as TreeViewItem;
            if(treeViewItem != null)
            {
                IJSAMObject jSAMObject = treeViewItem.Tag as IJSAMObject;
                if (jSAMObject != null)
                {
                    jSAMObjects.Add(jSAMObject);
                }
            }

            View3DControl.Show(jSAMObjects);
        }

        private void LoadAnalyticalModel(AnalyticalModel analyticalModel)
        {
            TreeView treeView_AnalyticalModel = AnalyticalModelControl.TreeView_Main;
            treeView_AnalyticalModel.Items.Clear();

            if (analyticalModel == null)
            {
                return;
            }

            string name = analyticalModel.Name;
            if (string.IsNullOrWhiteSpace(name))
            {
                name = "???";
            }

            TreeViewItem treeViewItem_AnalyticalModel = new TreeViewItem() { Header = new TextBlock() { Text = name }, Tag = analyticalModel };
            treeView_AnalyticalModel.Items.Add(treeViewItem_AnalyticalModel);

            TreeViewItem treeViewItem_Spaces = new TreeViewItem() { Header = new TextBlock() { Text = "Spaces" }, Tag = typeof(Space) };
            treeViewItem_AnalyticalModel.Items.Add(treeViewItem_Spaces);

            TreeViewItem treeViewItem_Shades = new TreeViewItem() { Header = new TextBlock() { Text = "Shades" }, Tag = typeof(Panel) };
            treeViewItem_AnalyticalModel.Items.Add(treeViewItem_Shades);

            TreeViewItem treeViewItem_Profiles = new TreeViewItem() { Header = new TextBlock() { Text = "Profiles" }, Tag = typeof(Profile) };
            treeViewItem_AnalyticalModel.Items.Add(treeViewItem_Profiles);

            TreeViewItem treeViewItem_Materials = new TreeViewItem() { Header = new TextBlock() { Text = "Materials" }, Tag = typeof(IMaterial) };
            treeViewItem_AnalyticalModel.Items.Add(treeViewItem_Materials);

            TreeViewItem treeViewItem_InternalConditions = new TreeViewItem() { Header = new TextBlock() { Text = "Internal Conditions" }, Tag = typeof(InternalCondition) };
            treeViewItem_AnalyticalModel.Items.Add(treeViewItem_InternalConditions);

            TreeViewItem treeViewItem_MechanicalSystems = new TreeViewItem() { Header = new TextBlock() { Text = "Mechanical Systems" }, Tag = typeof(MechanicalSystemType) };
            treeViewItem_AnalyticalModel.Items.Add(treeViewItem_MechanicalSystems);

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;
            if (adjacencyCluster != null)
            {
                List<Space> spaces = adjacencyCluster.GetSpaces();
                if (spaces != null)
                {
                    foreach (Space space in spaces)
                    {
                        TreeViewItem treeViewItem_Space = new TreeViewItem() { Header = new TextBlock() { Text = space.Name }, Tag = space };
                        treeViewItem_Spaces.Items.Add(treeViewItem_Space);
                        List<Panel> panels_Space = adjacencyCluster.GetPanels(space);
                        if (panels_Space != null)
                        {
                            foreach (Panel panel in panels_Space)
                            {
                                TreeViewItem treeViewItem_Panel = new TreeViewItem() { Header = new TextBlock() { Text = panel.Name }, Tag = panel };
                                treeViewItem_Space.Items.Add(treeViewItem_Panel);

                                List<Aperture> apertures = panel.Apertures;
                                if (apertures != null)
                                {
                                    foreach (Aperture aperture in apertures)
                                    {
                                        TreeViewItem treeViewItem_Aperture = new TreeViewItem() { Header = new TextBlock() { Text = aperture.Name }, Tag = aperture };
                                        treeViewItem_Panel.Items.Add(treeViewItem_Aperture);

                                        //if (expandedTags.Find(x => x is Aperture && ((Aperture)x).Guid == aperture.Guid) != null)
                                        //{
                                        //    treeNode_Aperture.Expand();
                                        //}
                                    }
                                }

                                //if (expandedTags.Find(x => x is Panel && ((Panel)x).Guid == panel.Guid) != null)
                                //{
                                //    treeNode_Panel.Expand();
                                //}
                            }
                        }

                        //if (expandedTags.Find(x => x is Space && ((Space)x).Guid == space.Guid) != null)
                        //{
                        //    treeNode_Space.Expand();
                        //}
                    }
                }
            }
        }

        private void UIAnalyticalModel_Modified(object sender, System.EventArgs e)
        {
            LoadAnalyticalModel(uIAnalyticalModel?.JSAMObject);
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
        }
    }
}
