using SAM.Core;
using SAM.Core.Windows.Forms;
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
            RibbonTab_Edit.Enabled = true;
            RibbonTab_File.Enabled = true;
            RibbonTab_Help.Enabled = true;
            RibbonTab_Library.Enabled = true;
            RibbonTab_Tools.Enabled = true;
            RibbonTab_Simulate.Enabled = true;

            RibbonButton_File_SaveAs.Enabled = true;
            RibbonButton_File_Save.Enabled = true;
            RibbonButton_File_Close.Enabled = true;

            if (uIAnalyticalModel?.JSAMObject == null)
            {
                RibbonTab_Edit.Enabled = false;
                RibbonButton_File_SaveAs.Enabled = false;
                RibbonButton_File_Save.Enabled = false;
                RibbonButton_File_Close.Enabled = false;
                RibbonTab_Simulate.Enabled = false;
            }

            Refresh_TreeView();
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
            if(string.IsNullOrWhiteSpace(name))
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

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;
            if(adjacencyCluster != null)
            {
                List<Space> spaces = adjacencyCluster.GetSpaces();
                if(spaces != null)
                {
                    foreach(Space space in spaces)
                    {
                        TreeNode treeNode_Space = treeNode_Spaces.Nodes.Add(space.Name);
                        treeNode_Space.Tag = space;
                        List<Panel> panels_Space = adjacencyCluster.GetPanels(space);
                        if(panels_Space != null)
                        {
                            foreach(Panel panel in panels_Space)
                            {
                                TreeNode treeNode_Panel = treeNode_Space.Nodes.Add(panel.Name);
                                treeNode_Panel.Tag = panel;

                                List<Aperture> apertures = panel.Apertures;
                                if(apertures != null)
                                {
                                    foreach(Aperture aperture in apertures)
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
                if(panels != null)
                {
                    foreach(Panel panel in panels)
                    {
                        List<Space> spaces_Panel = adjacencyCluster.GetSpaces(panel);
                        if(spaces_Panel == null || spaces_Panel.Count == 0)
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

            }

            List<Profile> profiles = analyticalModel.ProfileLibrary?.GetProfiles();
            if(profiles != null)
            {
                foreach(Profile profile in profiles)
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
            if(expandedTags != null && expandedTags.Count != 0)
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

        private void AnalyticalForm_FormClosed(object sender, FormClosedEventArgs e)
        {
        }

        private void RibbonButton_File_Open_Click(object sender, EventArgs e)
        {
            string path = null;
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                string directory = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SAM");
                if (System.IO.Directory.Exists(directory))
                {
                    openFileDialog.InitialDirectory = directory;
                }
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

            uIAnalyticalModel.Path = path;

            uIAnalyticalModel.Open();
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

        private void RibbonButton_Library_WeatherData_Click(object sender, EventArgs e)
        {
            MessageBox.Show("To be implemented soon...");
        }

        private void RibbonButton_File_New_Click(object sender, EventArgs e)
        {
            if (uIAnalyticalModel == null)
            {
                uIAnalyticalModel = new UIAnalyticalModel();
            }

            if (uIAnalyticalModel.JSAMObject != null)
            {
                if (!uIAnalyticalModel.Close())
                {
                    return;
                }
            }

            string name = null;
            using (TextBoxForm<string> textBoxForm = new TextBoxForm<string>("Analytical Model", "Project Name"))
            {
                textBoxForm.Value = "New Project";
                if (textBoxForm.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                name = textBoxForm.Value;
            }

            AnalyticalModel analyticalModel = new AnalyticalModel(Guid.NewGuid(), name);

            uIAnalyticalModel.JSAMObject = analyticalModel;

            Refresh_AnalyticalModel();
        }

        private void RibbonButton_Edit_SAMImport_Click(object sender, EventArgs e)
        {
            uIAnalyticalModel.Import(this);
        }

        private void RibbonButton_Edit_Location_Click(object sender, EventArgs e)
        {
            MessageBox.Show("To be implemented soon...");
        }

        private void TreeView_AnalyticalModel_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            TreeNode treeNode = e.Node;

            if(treeNode.Tag == typeof(IMaterial))
            {
                uIAnalyticalModel.EditMaterialLibrary(this);
                return;
            }


            IJSAMObject jSAMObject = treeNode.Tag as IJSAMObject;
            if(jSAMObject == null)
            {
                return;
            }

            if(jSAMObject is Panel)
            {
                AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
                if(analyticalModel == null)
                {
                    return;
                }

                AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;
                if(adjacencyCluster == null)
                {
                    return;
                }

                MaterialLibrary materialLibrary = analyticalModel.MaterialLibrary;

                ConstructionLibrary constructionLibrary = null;

                List<Construction> constructions = adjacencyCluster?.GetConstructions();
                if(constructions != null)
                {
                    constructionLibrary = new ConstructionLibrary(analyticalModel.Name);
                    constructions.ForEach(x => constructionLibrary.Add(x));
                }

                Panel panel = (Panel)jSAMObject;

                using (Windows.Forms.PanelForm panelForm = new Windows.Forms.PanelForm(panel, materialLibrary, constructionLibrary, Core.Query.Enums(typeof(Panel))))
                {
                    if(panelForm.ShowDialog(this) != DialogResult.OK)
                    {
                        return;
                    }

                    panel = panelForm.Panel;
                    constructionLibrary = panelForm.ConstructionLibrary;
                }

                adjacencyCluster.AddObject(panel);

                adjacencyCluster.ReplaceConstructions(constructionLibrary);

                uIAnalyticalModel.JSAMObject = new AnalyticalModel(analyticalModel, adjacencyCluster);
            }

            if(jSAMObject is IMaterial)
            {
                uIAnalyticalModel.EditMaterial((IMaterial)jSAMObject, this);
            }

            if (jSAMObject is Aperture)
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

                MaterialLibrary materialLibrary = analyticalModel.MaterialLibrary;

                ApertureConstructionLibrary apertureConstructionLibrary = null;

                List<ApertureConstruction> apertureConstructions = adjacencyCluster?.GetApertureConstructions();
                if (apertureConstructions != null)
                {
                    apertureConstructionLibrary = new ApertureConstructionLibrary(analyticalModel.Name);
                    apertureConstructions.ForEach(x => apertureConstructionLibrary.Add(x));
                }

                Aperture aperture = (Aperture)jSAMObject;

                using (Windows.Forms.ApertureForm apertureForm = new Windows.Forms.ApertureForm(aperture, materialLibrary, apertureConstructionLibrary, Core.Query.Enums(typeof(Aperture))))
                {
                    if (apertureForm.ShowDialog(this) != DialogResult.OK)
                    {
                        return;
                    }

                    aperture = apertureForm.Aperture;
                    apertureConstructionLibrary = apertureForm.ApertureConstructionLibrary;
                }

                Panel panel = adjacencyCluster.GetPanel(aperture);
                if(panel != null)
                {
                    panel.RemoveAperture(aperture.Guid);
                    panel.AddAperture(aperture);
                    adjacencyCluster.AddObject(aperture);
                }

                adjacencyCluster.ReplaceApertureConstructions(apertureConstructionLibrary);

                uIAnalyticalModel.JSAMObject = new AnalyticalModel(analyticalModel, adjacencyCluster);
            }
        }

        private void RibbonButton_Simulate_ImportWeatherData_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "EPW files (*.epw)|*.epw|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 1;
                openFileDialog.RestoreDirectory = true;
                if (openFileDialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                if(openFileDialog.FileName.ToLower().EndsWith("epw"))
                {

                }

                //SAM.Weather.Convert.ToSAM();
            }
        }

        private void RibbonButton_Edit_ModelCheck_Click(object sender, EventArgs e)
        {
            MessageBox.Show("To be implemented soon...");
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
            MessageBox.Show("To be implemented soon...");
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
    }
}
