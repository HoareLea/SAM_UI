using SAM.Core;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace SAM.Analytical.UI
{
    public partial class AnalyticalForm : Form
    {
        private UIAnalyticalModel uIAnalyticalModel;

        public AnalyticalForm()
        {
            InitializeComponent();
        }

        private void AnalyticalForm_Load(object sender, EventArgs e)
        {
            RibbonPanel_Tools_Hydra.Visible = false;

            uIAnalyticalModel = new UIAnalyticalModel();
            uIAnalyticalModel.Opened += UIAnalyticalModel_Refresh;
            uIAnalyticalModel.Closed += UIAnalyticalModel_Refresh;
            uIAnalyticalModel.Modified += UIAnalyticalModel_Modified;

            Refresh_AnalyticalModel();
        }

        
        private void UIAnalyticalModel_Modified(object sender, EventArgs e)
        {
            Refresh_TreeView();
        }

        private void UIAnalyticalModel_Refresh(object sender, EventArgs e)
        {
            Refresh_AnalyticalModel();
        }

        
        private void Refresh_AnalyticalModel()
        {
            RibbonButton_AddMissingElements.Enabled = false;
            RibbonButton_AnalyticalModelProperies.Enabled = false;
            RibbonButton_Edit_ApertureConstructions.Enabled = false;
            RibbonButton_Edit_Constructions.Enabled = false;
            RibbonButton_Edit_InternalConditionLibrary.Enabled = false;
            RibbonButton_Edit_Location.Enabled = false;
            RibbonButton_Edit_MaterialLibrary.Enabled = false;
            RibbonButton_Edit_ModelCheck.Enabled = false;
            RibbonButton_Edit_SAMImport.Enabled = false;
            RibbonButton_Edit_Spaces.Enabled = false;
            RibbonButton_EnergySimulation.Enabled = false;
            RibbonButton_File_Close.Enabled = false;
            RibbonButton_File_New.Enabled = false;
            RibbonButton_File_Open.Enabled = false;
            RibbonButton_File_Save.Enabled = false;
            RibbonButton_File_SaveAs.Enabled = false;
            RibbonButton_Help_Wiki.Enabled = false;
            RibbonButton_PrintRDS.Enabled = false;
            RibbonButton_ProfileLibrary.Enabled = false;
            RibbonButton_Results_AirHandlingUnitDiagram.Enabled = false;
            RibbonButton_Simulate_ImportWeatherData.Enabled = false;
            RibbonButton_Simulate_WeatherData.Enabled = false;
            RibbonButton_SolarSimulation.Enabled = false;
            RibbonButton_Tools_Clean.Enabled = false;
            RibbonButton_Tools_EditLibrary.Enabled = false;
            RibbonButton_Tools_Hydra.Enabled = false;
            RibbonButton_Tools_OpenT3D.Enabled = false;
            RibbonButton_Tools_OpenTBD.Enabled = false;
            RibbonButton_Tools_OpenTPD.Enabled = false;
            RibbonButton_Tools_OpenTSD.Enabled = false;

            RibbonButton_File_New.Enabled = true;
            RibbonButton_File_Open.Enabled = true;
            RibbonButton_Help_Wiki.Enabled = true;
            RibbonButton_Tools_Hydra.Enabled = true;
            RibbonButton_Tools_OpenT3D.Enabled = true;
            RibbonButton_Tools_OpenTBD.Enabled = true;
            RibbonButton_Tools_OpenTPD.Enabled = true;
            RibbonButton_Tools_OpenTSD.Enabled = true;
            RibbonButton_Tools_EditLibrary.Enabled = true;

            AnalyticalModel analyticalModel = uIAnalyticalModel.JSAMObject;
            if (uIAnalyticalModel != null)
            {
                RibbonButton_AddMissingElements.Enabled = true;
                RibbonButton_AnalyticalModelProperies.Enabled = true;
                RibbonButton_Edit_ApertureConstructions.Enabled = true;
                RibbonButton_Edit_Constructions.Enabled = true;
                RibbonButton_Edit_InternalConditionLibrary.Enabled = true;
                RibbonButton_Edit_Location.Enabled = true;
                RibbonButton_Edit_MaterialLibrary.Enabled = true;
                RibbonButton_Edit_ModelCheck.Enabled = true;
                RibbonButton_Edit_SAMImport.Enabled = true;
                RibbonButton_Edit_Spaces.Enabled = true;
                RibbonButton_EnergySimulation.Enabled = true;
                RibbonButton_File_Close.Enabled = true;
                RibbonButton_File_Save.Enabled = true;
                RibbonButton_File_SaveAs.Enabled = true;
                RibbonButton_PrintRDS.Enabled = true;
                RibbonButton_ProfileLibrary.Enabled = true;
                RibbonButton_Results_AirHandlingUnitDiagram.Enabled = true;
                RibbonButton_Simulate_ImportWeatherData.Enabled = true;
                RibbonButton_Simulate_WeatherData.Enabled = true;
                RibbonButton_SolarSimulation.Enabled = true;
                RibbonButton_Tools_Clean.Enabled = true;
            }

            Refresh_TreeView();
        }

        private void Refresh_TreeView()
        {
            List<object> expandedTags = GetExpandedTags(TreeView_AnalyticalModel.Nodes);

            TreeView_AnalyticalModel.Nodes.Clear();

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

            TreeNode treeNode_AnalyticalModel = TreeView_AnalyticalModel.Nodes.Add(name);
            treeNode_AnalyticalModel.Tag = analyticalModel;

            TreeNode treeNode_Spaces = treeNode_AnalyticalModel.Nodes.Add("Spaces");
            treeNode_Spaces.Tag = typeof(Space);
            TreeNode treeNode_Shades = treeNode_AnalyticalModel.Nodes.Add("Shades");
            treeNode_Shades.Tag = typeof(Panel);
            TreeNode treeNode_Profiles = treeNode_AnalyticalModel.Nodes.Add("Profiles");
            treeNode_Profiles.Tag = typeof(Profile);
            TreeNode treeNode_Materials = treeNode_AnalyticalModel.Nodes.Add("Materials");
            treeNode_Materials.Tag = typeof(IMaterial);
            TreeNode treeNode_InternalConditions = treeNode_AnalyticalModel.Nodes.Add("Internal Conditions");
            treeNode_InternalConditions.Tag = typeof(InternalCondition);
            TreeNode treeNode_MechanicalSystems = treeNode_AnalyticalModel.Nodes.Add("Mechanical Systems");
            treeNode_MechanicalSystems.Tag = typeof(MechanicalSystemType);

            ContextMenuStrip contextMenuStrip_MechanicalSystems = new ContextMenuStrip();

            ToolStripMenuItem toolStripMenuItem_MechanicalSystem_Create = new ToolStripMenuItem() { Text = "Create System" };
            toolStripMenuItem_MechanicalSystem_Create.Click += ToolStripMenuItem_MechanicalSystem_Create_Click;
            contextMenuStrip_MechanicalSystems.Items.Add(toolStripMenuItem_MechanicalSystem_Create);
            treeNode_MechanicalSystems.ContextMenuStrip = contextMenuStrip_MechanicalSystems;

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;
            if (adjacencyCluster != null)
            {
                List<Space> spaces = adjacencyCluster.GetSpaces();
                if (spaces != null)
                {
                    foreach (Space space in spaces)
                    {
                        TreeNode treeNode_Space = treeNode_Spaces.Nodes.Add(space.Name);
                        treeNode_Space.Tag = space;
                        List<Panel> panels_Space = adjacencyCluster.GetPanels(space);
                        if (panels_Space != null)
                        {
                            foreach (Panel panel in panels_Space)
                            {
                                TreeNode treeNode_Panel = treeNode_Space.Nodes.Add(panel.Name);
                                treeNode_Panel.Tag = panel;

                                List<Aperture> apertures = panel.Apertures;
                                if (apertures != null)
                                {
                                    foreach (Aperture aperture in apertures)
                                    {
                                        TreeNode treeNode_Aperture = treeNode_Panel.Nodes.Add(aperture.Name);
                                        treeNode_Aperture.Tag = aperture;

                                        if (expandedTags.Find(x => x is Aperture && ((Aperture)x).Guid == aperture.Guid) != null)
                                        {
                                            treeNode_Aperture.Expand();
                                        }
                                    }
                                }

                                if (expandedTags.Find(x => x is Panel && ((Panel)x).Guid == panel.Guid) != null)
                                {
                                    treeNode_Panel.Expand();
                                }
                            }
                        }

                        if (expandedTags.Find(x => x is Space && ((Space)x).Guid == space.Guid) != null)
                        {
                            treeNode_Space.Expand();
                        }
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
                            TreeNode treeNode_Shade = treeNode_Shades.Nodes.Add(panel.Name);
                            treeNode_Shade.Tag = panel;

                            if (expandedTags.Find(x => x is Panel && ((Panel)x).Guid == panel.Guid) != null)
                            {
                                treeNode_Shade.Expand();
                            }
                        }
                    }
                }

                IEnumerable<InternalCondition> internalConditions = adjacencyCluster.GetInternalConditions(false, true);
                if (internalConditions != null)
                {
                    foreach (InternalCondition internalCondition in internalConditions)
                    {
                        TreeNode treeNode_InternalCondition = treeNode_InternalConditions.Nodes.Add(internalCondition.Name);
                        treeNode_InternalCondition.Tag = internalCondition;

                        if (expandedTags.Find(x => x is InternalCondition && ((InternalCondition)x).Guid == internalCondition.Guid) != null)
                        {
                            treeNode_InternalCondition.Expand();
                        }
                    }
                }

                List<MechanicalSystemType> mechanicalSystemTypes = adjacencyCluster.GetMechanicalSystemTypes<MechanicalSystemType>();
                if(mechanicalSystemTypes != null)
                {
                    foreach (MechanicalSystemType mechanicalSystemType in mechanicalSystemTypes)
                    {
                        ContextMenuStrip contextMenuStrip_MechanicalSystemType = new ContextMenuStrip();
                        contextMenuStrip_MechanicalSystemType.Tag = mechanicalSystemType;
                        contextMenuStrip_MechanicalSystemType.Items.Add(toolStripMenuItem_MechanicalSystem_Create);

                        TreeNode treeNode_MechanicalSystemType = treeNode_MechanicalSystems.Nodes.Add(mechanicalSystemType.Name);
                        treeNode_MechanicalSystemType.Tag = mechanicalSystemType;
                        treeNode_MechanicalSystemType.ContextMenuStrip = contextMenuStrip_MechanicalSystemType;

                        if (expandedTags.Find(x => x is MechanicalSystemType && ((MechanicalSystemType)x).Guid == mechanicalSystemType.Guid) != null)
                        {
                            treeNode_MechanicalSystemType.Expand();
                        }
                    }
                }

            }

            List<Profile> profiles = analyticalModel.ProfileLibrary?.GetProfiles();
            if (profiles != null)
            {
                foreach (Profile profile in profiles)
                {
                    TreeNode treeNode_Profile = treeNode_Profiles.Nodes.Add(profile.Name);
                    treeNode_Profile.Tag = profile;
                }
            }

            List<IMaterial> materials = analyticalModel.MaterialLibrary?.GetMaterials();
            if (materials != null)
            {
                foreach (IMaterial material in materials)
                {
                    TreeNode treeNode_Material = treeNode_Materials.Nodes.Add(material.Name);
                    treeNode_Material.Tag = material;

                    ContextMenuStrip contextMenuStrip_Material = new ContextMenuStrip();
                    contextMenuStrip_Material.Tag = material;

                    ToolStripMenuItem toolStripMenuItem_Edit = new ToolStripMenuItem() { Text = "Edit" };
                    toolStripMenuItem_Edit.Click += ToolStripMenuItem_Material_Edit_Click;
                    contextMenuStrip_Material.Items.Add(toolStripMenuItem_Edit);

                    ToolStripMenuItem toolStripMenuItem_Remove = new ToolStripMenuItem() { Text = "Remove" };
                    toolStripMenuItem_Remove.Click += ToolStripMenuItem_Material_Remove_Click;
                    contextMenuStrip_Material.Items.Add(toolStripMenuItem_Remove);

                    ToolStripMenuItem toolStripMenuItem_Duplicate = new ToolStripMenuItem() { Text = "Duplicate" };
                    toolStripMenuItem_Duplicate.Click += ToolStripMenuItem_Material_Duplicate_Click;
                    contextMenuStrip_Material.Items.Add(toolStripMenuItem_Duplicate);

                    treeNode_Material.ContextMenuStrip = contextMenuStrip_Material;
                }
            }

            treeNode_AnalyticalModel.Expand();
            if (expandedTags != null && expandedTags.Count != 0)
            {
                if (expandedTags.Contains(treeNode_Spaces.Tag))
                {
                    treeNode_Spaces.Expand();
                }

                if (expandedTags.Contains(treeNode_Shades.Tag))
                {
                    treeNode_Shades.Expand();
                }

                if (expandedTags.Contains(treeNode_Profiles.Tag))
                {
                    treeNode_Profiles.Expand();
                }

                if (expandedTags.Contains(treeNode_Materials.Tag))
                {
                    treeNode_Materials.Expand();
                }
            }
        }

        private void ToolStripMenuItem_MechanicalSystem_Create_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem toolStripMenuItem = sender as ToolStripMenuItem;

            MechanicalSystemType mechanicalSystemType = toolStripMenuItem?.Owner?.Tag as MechanicalSystemType;

            uIAnalyticalModel.CreateMechanicalSystem(mechanicalSystemType);
        }

        private List<object> GetExpandedTags(TreeNodeCollection treeNodeCollection)
        {
            if (treeNodeCollection == null)
            {
                return null;
            }

            List<object> result = new List<object>();

            foreach (TreeNode treeNode in treeNodeCollection)
            {
                if (treeNode.IsExpanded)
                {
                    result.Add(treeNode.Tag);
                    List<object> expandedTags = GetExpandedTags(treeNode.Nodes);
                    if (expandedTags != null)
                    {
                        result.AddRange(expandedTags);
                    }
                }
            }

            return result;
        }

        private void TreeView_AnalyticalModel_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            TreeNode treeNode = e.Node;

            if (treeNode.Tag == typeof(IMaterial))
            {
                uIAnalyticalModel.EditMaterialLibrary(this);
                return;
            }

            if (treeNode.Tag == typeof(Profile))
            {
                uIAnalyticalModel.EditProfileLibrary(this);
                return;
            }

            if (treeNode.Tag == typeof(InternalCondition))
            {
                uIAnalyticalModel.EditInternalConditions(this);
                return;
            }


            IJSAMObject jSAMObject = treeNode.Tag as IJSAMObject;
            if (jSAMObject == null)
            {
                return;
            }

            if (jSAMObject is Panel)
            {
                uIAnalyticalModel.EditPanel((Panel)jSAMObject, this);
            }
            else if (jSAMObject is IMaterial)
            {
                uIAnalyticalModel.EditMaterial((IMaterial)jSAMObject, this);
            }
            else if (jSAMObject is Aperture)
            {
                uIAnalyticalModel.EditAperture((Aperture)jSAMObject, this);
            }
            else if (jSAMObject is Space)
            {
                uIAnalyticalModel.EditSpace((Space)jSAMObject, this);
            }
            else if (jSAMObject is InternalCondition)
            {
                uIAnalyticalModel.EditInternalCondition((InternalCondition)jSAMObject, this);
            }
        }

        private void ToolStripMenuItem_Material_Edit_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem toolStripMenuItem = (ToolStripMenuItem)sender;
            IMaterial material = toolStripMenuItem.Owner.Tag as IMaterial;
            if(material == null)
            {
                return;
            }

            uIAnalyticalModel.EditMaterial(material, this);
        }

        private void ToolStripMenuItem_Material_Remove_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem toolStripMenuItem = (ToolStripMenuItem)sender;
            IMaterial material = toolStripMenuItem.Owner.Tag as IMaterial;
            if (material == null)
            {
                return;
            }

            DialogResult dialogResult = MessageBox.Show(string.Format("Are you sure you want to remove {0} material?", material.Name), "Remove Material", MessageBoxButtons.YesNo);
            if(dialogResult != DialogResult.Yes)
            {
                return;
            }

            uIAnalyticalModel.RemoveMaterial(material);
        }

        private void ToolStripMenuItem_Material_Duplicate_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem toolStripMenuItem = (ToolStripMenuItem)sender;
            IMaterial material = toolStripMenuItem.Owner.Tag as IMaterial;
            if (material == null)
            {
                return;
            }

            uIAnalyticalModel.DuplicateMaterial(material, this);
        }

        private void RibbonButton_File_Open_Click(object sender, EventArgs e)
        {
            string path = null;
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                //string directory = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SAM");
                //if (System.IO.Directory.Exists(directory))
                //{
                //    openFileDialog.InitialDirectory = directory;
                //}
                openFileDialog.Filter = "json files (*.json)|*.json|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;
                if (openFileDialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }
                path = openFileDialog.FileName;
            }

            if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path))
            {
                return;
            }

            UIAnalyticalModel uIAnalyticalModel_Temp = new UIAnalyticalModel();
            uIAnalyticalModel_Temp.Path = path;

            uIAnalyticalModel_Temp.Open();
            //Core.Windows.Forms.MarqueeProgressForm.Show("Opening AnalyticalModel", () => uIAnalyticalModel_Temp.Open());

            uIAnalyticalModel.Path = path;
            uIAnalyticalModel.JSAMObject = uIAnalyticalModel_Temp?.JSAMObject;
        }

        private void RibbonButton_Tools_OpenTSD_Click(object sender, EventArgs e)
        {
            string path = Core.Tas.Query.TSDPath();

            if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path))
            {
                return;
            }

            System.Diagnostics.Process.Start(path);
        }

        private void RibbonButton_Tools_OpenTBD_Click(object sender, EventArgs e)
        {
            string path = Core.Tas.Query.TBDPath();

            if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path))
            {
                return;
            }

            System.Diagnostics.Process.Start(path);
        }

        private void RibbonButton_Tools_OpenT3D_Click(object sender, EventArgs e)
        {
            string path = Core.Tas.Query.TAS3DPath();

            if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path))
            {
                return;
            }

            System.Diagnostics.Process.Start(path);
        }

        private void RibbonButton_Tools_OpenTPD_Click(object sender, EventArgs e)
        {
            string path = Core.Tas.Query.TPDPath();

            if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path))
            {
                return;
            }

            System.Diagnostics.Process.Start(path);
        }

        private void RibbonButton_File_Close_Click(object sender, EventArgs e)
        {
            uIAnalyticalModel?.Close();
        }

        private void RibbonButton_File_SaveAs_Click(object sender, EventArgs e)
        {
            uIAnalyticalModel.SaveAs(this);
        }

        private void AnalyticalForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if(uIAnalyticalModel != null && !uIAnalyticalModel.Close())
            {
                e.Cancel = true;
            }
        }

        private void RibbonButton_Help_Wiki_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/HoareLea/SAM/wiki/00-Home");
        }

        private void RibbonButton_Tools_Hydra_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://hlhydra.azurewebsites.net/index.html");
        }

        private void RibbonButton_File_New_Click(object sender, EventArgs e)
        {
            if(uIAnalyticalModel.New(this))
            {
                Refresh_AnalyticalModel();
            }
        }

        private void RibbonButton_Edit_SAMImport_Click(object sender, EventArgs e)
        {
            uIAnalyticalModel.Import(this);
        }

        private void RibbonButton_Edit_Location_Click(object sender, EventArgs e)
        {
            uIAnalyticalModel.EditAddressAndLocation(this);
        }

        private void RibbonButton_Simulate_ImportWeatherData_Click(object sender, EventArgs e)
        {
            uIAnalyticalModel?.ImportWeatherData(this);
        }

        private void RibbonButton_Edit_ModelCheck_Click(object sender, EventArgs e)
        {
            uIAnalyticalModel?.Check(this);
        }

        private void RibbonPanel_Tools_Tas_Click(object sender, EventArgs e)
        {

        }

        private void RibbonButton_Edit_MaterialLibrary_Click(object sender, EventArgs e)
        {
            uIAnalyticalModel.EditMaterialLibrary(this);
        }

        private void RibbonButton_Edit_InternalConditionLibrary_Click(object sender, EventArgs e)
        {
            uIAnalyticalModel.EditInternalConditions(this);
        }

        private void RibbonButton_Edit_Constructions_Click(object sender, EventArgs e)
        {
            uIAnalyticalModel.EditConstructions(this);
        }

        private void RibbonButton_Edit_ApertureConstructions_Click(object sender, EventArgs e)
        {
            uIAnalyticalModel.EditApertureConstructions(this);
        }

        private void RibbonButton_Library_EditLibrary_Click(object sender, EventArgs e)
        {
            Modify.EditLibrary(this);
        }

        private void RibbonButton_ProfileLibrary_Click(object sender, EventArgs e)
        {
            uIAnalyticalModel?.EditProfileLibrary(this);
        }

        private void RibbonButton_Edit_Spaces_Click(object sender, EventArgs e)
        {
            uIAnalyticalModel?.EditSpaces(this);
        }

        private void RibbonButton_SolarSimulation_Click(object sender, EventArgs e)
        {
            uIAnalyticalModel?.SolarSimulation(this);
        }

        private void RibbonButton_EnergySimulation_Click(object sender, EventArgs e)
        {
            uIAnalyticalModel?.EnergySimulation(this);
        }

        private void RibbonButton_Tools_Clean_Click(object sender, EventArgs e)
        {
            uIAnalyticalModel?.Clean(this);
        }

        private void RibbonButton_AddMissingElements_Click(object sender, EventArgs e)
        {
            uIAnalyticalModel?.AddMissingObjects(this);
        }

        private void RibbonButton_AnalyticalModelProperies_Click(object sender, EventArgs e)
        {
            uIAnalyticalModel.EditProperties(this);
        }

        private void RibbonButton_PrintRDS_Click(object sender, EventArgs e)
        {
            uIAnalyticalModel?.PrintRoomDataSheets(this);
        }

        private void RibbonButton_Simulate_WeatherData_Click(object sender, EventArgs e)
        {
            uIAnalyticalModel.EditWeatherData(this);
        }

        private void RibbonButton_File_Save_Click(object sender, EventArgs e)
        {
            uIAnalyticalModel?.Save();
        }

        private void RibbonButton_Results_AirHandlingUnitDiagram_Click(object sender, EventArgs e)
        {
            Query.AirHandlingUnitDiagram(uIAnalyticalModel, this);
        }
    }
}
