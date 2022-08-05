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
        private Core.Windows.WindowHandle windowHandle;
        
        private UIAnalyticalModel uIAnalyticalModel;

        public AnalyticalWindow()
        {
            InitializeComponent();

            windowHandle = new Core.Windows.WindowHandle(this);

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
            RibbonButton_Tools_OpenMollierChart.Click += RibbonButton_Tools_OpenMollierChart_Click;

            RibbonButton_Help_Wiki.Click += RibbonButton_Help_Wiki_Click;

            AnalyticalModelControl.TreeView_Main.SelectedItemChanged += TreeView_Main_SelectedItemChanged;

            uIAnalyticalModel = new UIAnalyticalModel();
            uIAnalyticalModel.Modified += UIAnalyticalModel_Modified;

            View3DControl.UIAnalyticalModel = uIAnalyticalModel;
            AnalyticalModelControl.UIAnalyticalModel = uIAnalyticalModel;
        }

        private void RibbonButton_Tools_OpenMollierChart_Click(object sender, RoutedEventArgs e)
        {
            
        }
        private void UIAnalyticalModel_Modified(object sender, System.EventArgs e)
        {

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
            UI.Modify.EditLibrary(windowHandle);
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
    }
}
