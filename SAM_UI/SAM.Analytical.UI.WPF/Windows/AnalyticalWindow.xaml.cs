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

            uIAnalyticalModel = new UIAnalyticalModel();
            uIAnalyticalModel.Modified += UIAnalyticalModel_Modified;

            View3DControl.UIAnalyticalModel = uIAnalyticalModel;
        }

        private void UIAnalyticalModel_Modified(object sender, System.EventArgs e)
        {
            TreeView treeView_AnalyticalModel = AnalyticalModelControl.TreeView_Main;
            treeView_AnalyticalModel.Items.Clear();

            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
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
