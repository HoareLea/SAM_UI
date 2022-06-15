using SAM.Core;
using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for AnalyticalModelTreeViewControl.xaml
    /// </summary>
    public partial class AnalyticalModelControl : UserControl
    {
        private Core.Windows.WindowHandle windowHandle;

        private UIAnalyticalModel uIAnalyticalModel;

        public AnalyticalModelControl()
        {
             InitializeComponent();
        }

        private void TreeView_Main_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            ContextMenu.Items.Clear();

            TreeViewItem treeViewItem = (e.Source as TextBlock)?.Parent as TreeViewItem;
            if(treeViewItem == null)
            {
                e.Handled = true;
                return;
            }

            if (treeViewItem.Tag is Type)
            {
                if(treeViewItem.Tag == typeof(MechanicalSystemType))
                {
                    MenuItem menuItem = new MenuItem();
                    menuItem.Name = "MenuItem_Create";
                    menuItem.Header = "Create";
                    menuItem.Click += MenuItem_Create_Click; ;
                    menuItem.Tag = typeof(MechanicalSystemType);
                    ContextMenu.Items.Add(menuItem);
                }
            }

            IJSAMObject jSAMObject = treeViewItem.Tag as IJSAMObject;
            if(jSAMObject == null)
            {
                e.Handled = true;
                return;
            }

            if(jSAMObject is Space)
            {
                MenuItem menuItem = new MenuItem();
                menuItem.Name = "MenuItem_Edit";
                menuItem.Header = "Edit";
                menuItem.Click += MenuItem_Edit_Click;
                menuItem.Tag = jSAMObject;
                ContextMenu.Items.Add(menuItem);
            }
            else if(jSAMObject is Panel)
            {
                MenuItem menuItem = new MenuItem();
                menuItem.Name = "MenuItem_Edit";
                menuItem.Header = "Edit";
                menuItem.Click += MenuItem_Edit_Click;
                menuItem.Tag = jSAMObject;
                ContextMenu.Items.Add(menuItem);
            }
            else if (jSAMObject is Aperture)
            {
                MenuItem menuItem = new MenuItem();
                menuItem.Name = "MenuItem_Edit";
                menuItem.Header = "Edit";
                menuItem.Click += MenuItem_Edit_Click;
                menuItem.Tag = jSAMObject;
                ContextMenu.Items.Add(menuItem);
            }
            else if (jSAMObject is IMaterial)
            {
                MenuItem menuItem = null;

                menuItem = new MenuItem();
                menuItem.Name = "MenuItem_Edit";
                menuItem.Header = "Edit";
                menuItem.Click += MenuItem_Edit_Click;
                menuItem.Tag = jSAMObject;
                ContextMenu.Items.Add(menuItem);

                menuItem = new MenuItem();
                menuItem.Name = "MenuItem_Remove";
                menuItem.Header = "Remove";
                menuItem.Click += MenuItem_Remove_Click;
                menuItem.Tag = jSAMObject;
                ContextMenu.Items.Add(menuItem);

                menuItem = new MenuItem();
                menuItem.Name = "MenuItem_Duplicate";
                menuItem.Header = "Duplicate";
                menuItem.Click += MenuItem_Duplicate_Click;
                menuItem.Tag = jSAMObject;
                ContextMenu.Items.Add(menuItem);
            }
            else if (jSAMObject is Profile)
            {
                MenuItem menuItem = new MenuItem();
                menuItem.Name = "MenuItem_Edit";
                menuItem.Header = "Edit";
                menuItem.Click += MenuItem_Edit_Click;
                menuItem.Tag = jSAMObject;
                ContextMenu.Items.Add(menuItem);
            }
            else
            {
                e.Handled = true;
                return;
            }
        }

        private void MenuItem_Duplicate_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            if (menuItem == null)
            {
                return;
            }

            IJSAMObject jSAMObject = menuItem.Tag as IJSAMObject;
            if (jSAMObject == null)
            {
                return;
            }

            if (jSAMObject is IMaterial)
            {
                UI.Modify.DuplicateMaterial(uIAnalyticalModel, jSAMObject as dynamic);
            }
        }

        private void MenuItem_Remove_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            if (menuItem == null)
            {
                return;
            }

            IJSAMObject jSAMObject = menuItem.Tag as IJSAMObject;
            if (jSAMObject == null)
            {
                return;
            }

            if (jSAMObject is IMaterial)
            {
                UI.Modify.RemoveMaterial(uIAnalyticalModel, jSAMObject as dynamic);
            }
        }

        private void MenuItem_Create_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            if (menuItem == null)
            {
                return;
            }

            if (menuItem.Tag == typeof(MechanicalSystemType))
            {
                uIAnalyticalModel.CreateMechanicalSystem(null, windowHandle);
                return;
            }
        }

        private void MenuItem_Edit_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            if(menuItem == null)
            {
                return;
            }

            if (menuItem.Tag == typeof(IMaterial))
            {
                uIAnalyticalModel.EditMaterialLibrary(windowHandle);
                return;
            }

            if (menuItem.Tag == typeof(Profile))
            {
                uIAnalyticalModel.EditProfileLibrary(windowHandle);
                return;
            }

            if (menuItem.Tag == typeof(InternalCondition))
            {
                uIAnalyticalModel.EditInternalConditions(windowHandle);
                return;
            }

            IJSAMObject jSAMObject = menuItem.Tag as IJSAMObject;
            if(jSAMObject == null)
            {
                return;
            }

            if(jSAMObject is Space)
            {
                UI.Modify.EditSpace(uIAnalyticalModel, jSAMObject as dynamic, windowHandle);
            }
            else if(jSAMObject is Panel)
            {
                UI.Modify.EditPanel(uIAnalyticalModel, jSAMObject as dynamic, windowHandle);
            }
            else if (jSAMObject is IMaterial)
            {
                UI.Modify.EditMaterial(uIAnalyticalModel, jSAMObject as dynamic, windowHandle);
            }
            else if (jSAMObject is Profile)
            {
                UI.Modify.EditProfile(uIAnalyticalModel, jSAMObject as dynamic, windowHandle);
            }
            else if (jSAMObject is Aperture)
            {
                UI.Modify.EditAperture(uIAnalyticalModel, jSAMObject as dynamic, windowHandle);
            }


        }

        public UIAnalyticalModel UIAnalyticalModel
        {
            get
            {
                return uIAnalyticalModel;
            }

            set
            {
                uIAnalyticalModel = value;
                if (uIAnalyticalModel != null)
                {
                    uIAnalyticalModel.Modified -= UIAnalyticalModel_Modified;
                    uIAnalyticalModel.Modified += UIAnalyticalModel_Modified;

                    uIAnalyticalModel.Closed -= UIAnalyticalModel_Closed;
                    uIAnalyticalModel.Closed += UIAnalyticalModel_Closed;

                    uIAnalyticalModel.Opened -= UIAnalyticalModel_Opened;
                    uIAnalyticalModel.Opened += UIAnalyticalModel_Opened;
                }
            }
        }

        private void UIAnalyticalModel_Modified(object sender, System.EventArgs e)
        {
            LoadAnalyticalModel(uIAnalyticalModel?.JSAMObject);
        }

        private void UIAnalyticalModel_Opened(object sender, EventArgs e)
        {
            LoadAnalyticalModel(uIAnalyticalModel?.JSAMObject);
        }

        private void UIAnalyticalModel_Closed(object sender, EventArgs e)
        {
            LoadAnalyticalModel(uIAnalyticalModel?.JSAMObject);
        }

        private void LoadAnalyticalModel(AnalyticalModel analyticalModel)
        {
            TreeView treeView_AnalyticalModel = TreeView_Main;
            if (treeView_AnalyticalModel == null)
            {
                return;
            }

            List<object> expandedTags = GetExpandedTags(treeView_AnalyticalModel.Items);

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
                                        treeViewItem_Aperture.IsExpanded = expandedTags.Find(x => x is Aperture && ((Aperture)x).Guid == aperture.Guid) != null;
                                    }
                                }

                                treeViewItem_Panel.IsExpanded = expandedTags.Find(x => x is Panel && ((Panel)x).Guid == panel.Guid) != null;
                            }
                        }

                        treeViewItem_Space.IsExpanded = expandedTags.Find(x => x is Space && ((Space)x).Guid == space.Guid) != null;
                    }
                }

                List<Panel> panels = adjacencyCluster.GetPanels();
                if (panels != null)
                {
                    foreach (Panel panel in panels)
                    {
                        List<Space> spaces_Panel = adjacencyCluster.GetSpaces(panel);
                        if (spaces_Panel == null || spaces_Panel.Count == 0)
                        {
                            TreeViewItem treeViewItem_Shade = new TreeViewItem() { Header = new TextBlock() { Text = panel.Name }, Tag = panel };
                            treeViewItem_Shade.Tag = panel;
                            treeViewItem_Shade.IsExpanded = expandedTags.Find(x => x is Panel && ((Panel)x).Guid == panel.Guid) != null;

                            treeViewItem_Shades.Items.Add(treeViewItem_Shade);
                        }
                    }
                }

                IEnumerable<InternalCondition> internalConditions = adjacencyCluster.GetInternalConditions(false, true);
                if (internalConditions != null)
                {
                    foreach (InternalCondition internalCondition in internalConditions)
                    {
                        TreeViewItem treeViewItem_InternalCondition = new TreeViewItem() { Header = new TextBlock() { Text = internalCondition.Name }, Tag = internalCondition };
                        treeViewItem_InternalConditions.Items.Add(treeViewItem_InternalCondition);
                    }
                }

                List<MechanicalSystemType> mechanicalSystemTypes = adjacencyCluster.GetMechanicalSystemTypes<MechanicalSystemType>();
                if (mechanicalSystemTypes != null)
                {
                    foreach (MechanicalSystemType mechanicalSystemType in mechanicalSystemTypes)
                    {
                        TreeViewItem treeViewItem_MechanicalSystemType = new TreeViewItem() { Header = new TextBlock() { Text = mechanicalSystemType.Name }, Tag = mechanicalSystemType };
                        treeViewItem_MechanicalSystems.Items.Add(treeViewItem_MechanicalSystemType);

                        treeViewItem_MechanicalSystemType.IsExpanded = expandedTags.Find(x => x is MechanicalSystemType && ((MechanicalSystemType)x).Guid == mechanicalSystemType.Guid) != null;
                    }
                }
            }

            List<Profile> profiles = analyticalModel.ProfileLibrary?.GetProfiles();
            if (profiles != null)
            {
                foreach (Profile profile in profiles)
                {
                    TreeViewItem treeViewItem_Profile = new TreeViewItem() { Header = new TextBlock() { Text = profile.Name }, Tag = profile };
                    treeViewItem_Profiles.Items.Add(treeViewItem_Profile);
                }
            }

            List<IMaterial> materials = analyticalModel.MaterialLibrary?.GetMaterials();
            if (materials != null)
            {
                foreach (IMaterial material in materials)
                {
                    TreeViewItem treeViewItem_Material = new TreeViewItem() { Header = new TextBlock() { Text = material.Name }, Tag = material };
                    treeViewItem_Materials.Items.Add(treeViewItem_Material);
                }
            }

            treeViewItem_AnalyticalModel.IsExpanded = true;
            if (expandedTags != null && expandedTags.Count != 0)
            {
                treeViewItem_Spaces.IsExpanded = expandedTags.Contains(treeViewItem_Spaces.Tag);
                treeViewItem_Shades.IsExpanded = expandedTags.Contains(treeViewItem_Shades.Tag);
                treeViewItem_Profiles.IsExpanded = expandedTags.Contains(treeViewItem_Profiles.Tag);
                treeViewItem_Materials.IsExpanded = expandedTags.Contains(treeViewItem_Materials.Tag);
                treeViewItem_InternalConditions.IsExpanded = expandedTags.Contains(treeViewItem_InternalConditions.Tag);
                treeViewItem_MechanicalSystems.IsExpanded = expandedTags.Contains(treeViewItem_MechanicalSystems.Tag);
            }
        }

        private List<object> GetExpandedTags(ItemCollection itemCollection)
        {
            if (itemCollection == null)
            {
                return null;
            }

            List<object> result = new List<object>();

            foreach (TreeViewItem treeViewItem in itemCollection)
            {
                if (treeViewItem.IsExpanded)
                {
                    result.Add(treeViewItem.Tag);
                    List<object> expandedTags = GetExpandedTags(treeViewItem.Items);
                    if (expandedTags != null)
                    {
                        result.AddRange(expandedTags);
                    }
                }
            }

            return result;
        }

        private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            System.Windows.Window window = Query.Window(this);
            if(window != null)
            {
                windowHandle = new Core.Windows.WindowHandle(window);
            }
        }
    }
}
