using SAM.Core;
using SAM.Core.UI;
using SAM.Geometry.UI;
using SAM.Geometry.UI.WPF;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
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

        private TreeViewDragDropManager treeViewDragDropManager_Views;
        private TreeViewDragDropManager treeViewDragDropManager_Model;

        private TreeViewHighlightManager treeViewHighlightManager_Model;
        private TreeViewHighlightManager treeViewHighlightManager_Views;

        public event ZoomRequestedEventHandler ZoomRequested;
        public event SelectionRequestedEventHandler SelectionRequested;
        public event TreeViewItemDroppedEventHandler TreeViewItemDropped;


        public AnalyticalModelControl()
        {
             InitializeComponent();

            treeViewDragDropManager_Views = new TreeViewDragDropManager(treeView_Views);
            treeViewDragDropManager_Views.TreeViewItemDropped += DragDropManager_Views_TreeViewItemDropped;

            treeViewDragDropManager_Model = new TreeViewDragDropManager(treeView_Model);
            treeViewDragDropManager_Model.TreeViewItemDropped += DragDropManager_Model_TreeViewItemDropped;

            treeViewHighlightManager_Model = new TreeViewHighlightManager(treeView_Model);
            treeViewHighlightManager_Model.Enabled = true;
            treeViewHighlightManager_Model.TreeViewItemHighlighted += TreeViewHighlightManager_Model_TreeViewItemHighlighted;

            treeViewHighlightManager_Views = new TreeViewHighlightManager(treeView_Views);
            treeViewHighlightManager_Views.Enabled = true;
            treeViewHighlightManager_Views.TreeViewItemHighlighted += TreeViewHighlightManager_Views_TreeViewItemHighlighted; ;
        }

        private void TreeViewHighlightManager_Views_TreeViewItemHighlighted(object sender, TreeViewItemHighlightedEventArgs e)
        {
            Modify.AllowTreeViewItemByType(e, typeof(ViewSettings));
        }

        private void TreeViewHighlightManager_Model_TreeViewItemHighlighted(object sender, TreeViewItemHighlightedEventArgs e)
        {
            Modify.AllowTreeViewItemByType(e);
        }

        private void DragDropManager_Model_TreeViewItemDropped(object sender, TreeViewItemDroppedEventArgs e)
        {
            object targetObject = e.TargetTreeViewItem.Tag;
            object selectedObject = e.SelectedTreeViewItem.Tag;

            List<object> highlightedObjects = treeViewHighlightManager_Model?.HighlightedTreeViewItems?.ConvertAll(x => x.Tag);

            e.EventResult = EventResult.Canceled;

            if (targetObject == null || selectedObject == null)
            {
                return;
            }

            TreeViewItemDropped?.Invoke(this, e);

            if (selectedObject is Space && targetObject is Zone)
            {
                List<Space> spaces = new List<Space>() { (Space)selectedObject };
                List<Space> highlightedSpaces = highlightedObjects?.FindAll(x => x is Space).ConvertAll(x => (Space)x);
                if(highlightedSpaces != null)
                {
                    spaces.AddRange(highlightedSpaces);
                }

                Modify.AssignSpaceZone(uIAnalyticalModel, spaces, (Zone)targetObject);
                e.EventResult = EventResult.Succeeded;
            }

            if(selectedObject is InternalCondition && targetObject is Space)
            {
                Modify.AssignSpaceInternalCondition(uIAnalyticalModel, (Space)targetObject, (InternalCondition)selectedObject);
                e.EventResult = EventResult.Succeeded;
            }
        }

        private void DragDropManager_Views_TreeViewItemDropped(object sender, TreeViewItemDroppedEventArgs e)
        {
            ViewSettings viewSettings = e.SelectedTreeViewItem.Tag as ViewSettings;
            if(viewSettings == null)
            {
                return;
            }
            
            string group = e.TargetTreeViewItem.Tag as string;

            List<object> highlightedObjects = treeViewHighlightManager_Views?.HighlightedTreeViewItems?.ConvertAll(x => x.Tag);

            HashSet<Guid> guids = new HashSet<Guid>() { viewSettings.Guid };

            List<ViewSettings> highlightedViewSettings = highlightedObjects?.FindAll(x => x is ViewSettings).ConvertAll(x => (ViewSettings)x);
            highlightedViewSettings?.ForEach(x => guids.Add(x.Guid));

            Modify.SetGroup(uIAnalyticalModel, guids, group);
        }

        private void treeView_Model_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            contextMenu_Model.Items.Clear();

            TreeViewItem treeViewItem = e.Source as TreeViewItem;
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
                    menuItem.Click += MenuItem_Create_Click;
                    menuItem.Tag = typeof(MechanicalSystemType);
                    contextMenu_Model.Items.Add(menuItem);
                }
            }

            List<Tuple<TreeViewItem, IJSAMObject>> tuples = null;

            bool singleSelection = true;
            if (treeViewHighlightManager_Model != null && treeViewHighlightManager_Model.Enabled)
            {
                List<TreeViewItem> treeViewItems = treeViewHighlightManager_Model.HighlightedTreeViewItems;
                if (treeViewItems != null && treeViewItems.Count != 0)
                {
                    tuples = treeViewItems.FindAll(x => x?.Tag is IJSAMObject).ConvertAll(x => new Tuple<TreeViewItem, IJSAMObject>(x, x.Tag as IJSAMObject));
                    singleSelection = tuples != null && tuples.Count == 1;
                }
            }

            if(tuples == null || tuples.Count == 0)
            {
                IJSAMObject jSAMObject_Temp = treeViewItem.Tag as IJSAMObject;
                if(jSAMObject_Temp != null)
                {
                    tuples = new List<Tuple<TreeViewItem, IJSAMObject>>() { new Tuple<TreeViewItem, IJSAMObject>(treeViewItem, jSAMObject_Temp) };
                }
            }

            if (tuples == null || tuples.Count == 0)
            {
                TreeViewItem treeViewItem_Parent = treeViewItem.Parent as TreeViewItem;

                bool added = false;
                if (treeViewItem_Parent.Tag == typeof(Zone))
                {
                    if (singleSelection)
                    {
                        MenuItem menuItem = new MenuItem();
                        menuItem.Name = "MenuItem_EditZone";
                        menuItem.Header = "Edit Zone";
                        menuItem.Click += MenuItem_EditZone_Click;
                        menuItem.Tag = treeViewItem.Items == null || treeViewItem.Items.Count == 0 ? null : (treeViewItem.Items[0] as TreeViewItem)?.Tag;
                        contextMenu_Model.Items.Add(menuItem);
                        added = true;
                    }
                }

                if(!added)
                {
                    e.Handled = true;
                }

                return;
            }


            IJSAMObject jSAMObject = tuples[0].Item2;


            if (jSAMObject is Space)
            {
                MenuItem menuItem = new MenuItem();
                menuItem.Name = "MenuItem_Zoom";
                menuItem.Header = "Zoom";
                menuItem.Click += MenuItem_Zoom_Click;
                menuItem.Tag = tuples.ConvertAll(x => x.Item2);
                contextMenu_Model.Items.Add(menuItem);

                menuItem = new MenuItem();
                menuItem.Name = "MenuItem_Select";
                menuItem.Header = "Select";
                menuItem.Click += MenuItem_Select_Click;
                menuItem.Tag = tuples.ConvertAll(x => x.Item2);
                contextMenu_Model.Items.Add(menuItem);

                menuItem = new MenuItem();
                menuItem.Name = "MenuItem_EditInternalConditions";
                menuItem.Header = "Modify IC";
                menuItem.Click += MenuItem_EditInternalConditions_Click;
                menuItem.Tag = tuples.ConvertAll(x => x.Item2);
                contextMenu_Model.Items.Add(menuItem);

                if (singleSelection)
                {
                    menuItem = new MenuItem();
                    menuItem.Name = "MenuItem_Edit";
                    menuItem.Header = "Edit Space";
                    menuItem.Click += MenuItem_Edit_Click;
                    menuItem.Tag = jSAMObject;
                    contextMenu_Model.Items.Add(menuItem);

                    menuItem = new MenuItem();
                    menuItem.Name = "MenuItem_EditZone";
                    menuItem.Header = "Edit Zone";
                    menuItem.Click += MenuItem_EditSpaceZone_Click;
                    menuItem.Tag = tuples[0].Item1;
                    contextMenu_Model.Items.Add(menuItem);
                }

                List<Tuple<Zone, Space>> tuples_Zone = new List<Tuple<Zone, Space>>();
                foreach(Tuple<TreeViewItem, IJSAMObject> tuple in tuples)
                {
                    if(!(tuple.Item2 is Space))
                    {
                        continue;
                    }

                    TreeViewItem treeViewItem_Zone = treeViewItem.Parent as TreeViewItem;
                    if (treeViewItem_Zone != null)
                    {
                        Zone zone = treeViewItem_Zone.Tag as Zone;
                        if (zone != null)
                        {
                            tuples_Zone.Add(new Tuple<Zone, Space>(zone, (Space)tuple.Item2));
                        }
                    }
                }

                if(tuples_Zone != null && tuples_Zone.Count > 0)
                {
                    menuItem = new MenuItem();
                    menuItem.Name = "MenuItem_RemoveSpaceZone";
                    menuItem.Header = "Remove From Zone";
                    menuItem.Click += MenuItem_RemoveSpaceZone_Click;
                    menuItem.Tag = tuples_Zone;
                    contextMenu_Model.Items.Add(menuItem);
                }


            }
            else if(jSAMObject is Panel)
            {
                MenuItem menuItem = new MenuItem();
                menuItem.Name = "MenuItem_Zoom";
                menuItem.Header = "Zoom";
                menuItem.Click += MenuItem_Zoom_Click;
                menuItem.Tag = tuples.ConvertAll(x => x.Item2);
                contextMenu_Model.Items.Add(menuItem);

                menuItem = new MenuItem();
                menuItem.Name = "MenuItem_Select";
                menuItem.Header = "Select";
                menuItem.Click += MenuItem_Select_Click;
                menuItem.Tag = tuples.ConvertAll(x => x.Item2);
                contextMenu_Model.Items.Add(menuItem);

                if(singleSelection)
                {
                    menuItem = new MenuItem();
                    menuItem.Name = "MenuItem_Edit";
                    menuItem.Header = "Edit";
                    menuItem.Click += MenuItem_Edit_Click;
                    menuItem.Tag = jSAMObject;
                    contextMenu_Model.Items.Add(menuItem);
                }

            }
            else if (jSAMObject is Aperture)
            {
                MenuItem menuItem = new MenuItem();
                menuItem.Name = "MenuItem_Zoom";
                menuItem.Header = "Zoom";
                menuItem.Click += MenuItem_Zoom_Click;
                menuItem.Tag = tuples.ConvertAll(x => x.Item2);
                contextMenu_Model.Items.Add(menuItem);

                menuItem = new MenuItem();
                menuItem.Name = "MenuItem_Select";
                menuItem.Header = "Select";
                menuItem.Click += MenuItem_Select_Click;
                menuItem.Tag = tuples.ConvertAll(x => x.Item2);
                contextMenu_Model.Items.Add(menuItem);

                if(singleSelection)
                {
                    menuItem = new MenuItem();
                    menuItem.Name = "MenuItem_Edit";
                    menuItem.Header = "Edit";
                    menuItem.Click += MenuItem_Edit_Click;
                    menuItem.Tag = jSAMObject;
                    contextMenu_Model.Items.Add(menuItem);
                }

            }
            else if (jSAMObject is IMaterial)
            {
                MenuItem menuItem = null;

                if (singleSelection)
                {
                    menuItem = new MenuItem();
                    menuItem.Name = "MenuItem_Edit";
                    menuItem.Header = "Edit";
                    menuItem.Click += MenuItem_Edit_Click;
                    menuItem.Tag = jSAMObject;
                    contextMenu_Model.Items.Add(menuItem);
                }

                menuItem = new MenuItem();
                menuItem.Name = "MenuItem_Remove";
                menuItem.Header = "Remove";
                menuItem.Click += MenuItem_Remove_Click;
                menuItem.Tag = tuples.ConvertAll(x => x.Item2);
                contextMenu_Model.Items.Add(menuItem);

                if (singleSelection)
                {
                    menuItem = new MenuItem();
                    menuItem.Name = "MenuItem_Duplicate";
                    menuItem.Header = "Duplicate";
                    menuItem.Click += MenuItem_Duplicate_Click;
                    menuItem.Tag = jSAMObject;
                    contextMenu_Model.Items.Add(menuItem);
                }

            }
            else if (jSAMObject is Profile)
            {

                if (singleSelection)
                {
                    MenuItem menuItem = new MenuItem();
                    menuItem.Name = "MenuItem_Edit";
                    menuItem.Header = "Edit";
                    menuItem.Click += MenuItem_Edit_Click;
                    menuItem.Tag = jSAMObject;
                    contextMenu_Model.Items.Add(menuItem);
                }
            }
            else if (jSAMObject is Zone)
            {
                MenuItem menuItem = new MenuItem();
                menuItem.Name = "MenuItem_Zoom";
                menuItem.Header = "Zoom";
                menuItem.Click += MenuItem_Zoom_Click;
                menuItem.Tag = tuples.ConvertAll(x => x.Item2);
                contextMenu_Model.Items.Add(menuItem);

                menuItem = new MenuItem();
                menuItem.Name = "MenuItem_Select";
                menuItem.Header = "Select";
                menuItem.Click += MenuItem_Select_Click;
                menuItem.Tag = tuples.ConvertAll(x => x.Item2);
                contextMenu_Model.Items.Add(menuItem);

                if (singleSelection)
                {
                    menuItem = new MenuItem();
                    menuItem.Name = "MenuItem_Edit";
                    menuItem.Header = "Edit";
                    menuItem.Click += MenuItem_Edit_Click;
                    menuItem.Tag = jSAMObject;
                    contextMenu_Model.Items.Add(menuItem);
                }
            }
            else if(jSAMObject is InternalCondition)
            {
                if (singleSelection)
                {
                    MenuItem menuItem = new MenuItem();
                    menuItem.Name = "MenuItem_Edit";
                    menuItem.Header = "Edit";
                    menuItem.Click += MenuItem_Edit_Click;
                    menuItem.Tag = tuples.ConvertAll(x => x.Item2);
                    contextMenu_Model.Items.Add(menuItem);

                    menuItem = new MenuItem();
                    menuItem.Name = "MenuItem_Duplicate";
                    menuItem.Header = "Duplicate";
                    menuItem.Click += MenuItem_Duplicate_Click;
                    menuItem.Tag = jSAMObject;
                    contextMenu_Model.Items.Add(menuItem);

                }
            }
            else
            {
                e.Handled = true;
                return;
            }
        }

        private void MenuItem_EditInternalConditions_Click(object sender, RoutedEventArgs e)
        {
            List<IJSAMObject> jSAMObjects = GetSAMObjects(sender as MenuItem);
            if (jSAMObjects == null || jSAMObjects.Count == 0)
            {
                return;
            }

            Modify.EditInternalConditions(uIAnalyticalModel, jSAMObjects.FindAll(x => x is Space).Cast<Space>());
        }

        private void MenuItem_EditZone_Click(object sender, RoutedEventArgs e)
        {
            List<IJSAMObject> jSAMObjects = GetSAMObjects(sender as MenuItem);
            if(jSAMObjects == null || jSAMObjects.Count == 0)
            {
                return;
            }

            foreach(Zone zone in jSAMObjects.FindAll(x => x is Zone))
            {
                Modify.EditSpaceZone(uIAnalyticalModel, zone);
            }
        }

        private void treeView_Views_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            contextMenu_Views.Items.Clear();

            TreeViewItem treeViewItem = e.Source as TreeViewItem;
            if (treeViewItem == null)
            {
                e.Handled = true;
                return;
            }

            List<IJSAMObject> jSAMObjects = null;

            if(treeViewItem.Tag is ViewSettings)
            {
                jSAMObjects = new List<IJSAMObject>() { (ViewSettings)treeViewItem.Tag };
            }
            else if(treeViewItem.Tag is string)
            {
                jSAMObjects = new List<IJSAMObject>();
                foreach (object @object in treeViewItem.Items)
                {
                    ViewSettings viewSettings_Temp = (@object as TreeViewItem)?.Tag as ViewSettings;
                    if(viewSettings_Temp == null)
                    {
                        continue;
                    }

                    jSAMObjects.Add(viewSettings_Temp);
                }
            }

            if (jSAMObjects == null || jSAMObjects.Count == 0)
            {
                e.Handled = true;
                return;
            }

            List<TreeViewItem> treeViewItems = treeViewHighlightManager_Views != null && treeViewHighlightManager_Views.Enabled ? treeViewHighlightManager_Views.HighlightedTreeViewItems : null;
            if (treeViewItems != null && treeViewItems.Count != 0)
            {
                jSAMObjects = new List<IJSAMObject>(treeViewItems.FindAll(x => x?.Tag is ViewSettings).ConvertAll(x => x.Tag as ViewSettings));
            }

            MenuItem menuItem = null;

            menuItem = new MenuItem();
            menuItem.Name = "MenuItem_Open_TabItem";
            menuItem.Header = "Open";
            menuItem.Click += MenuItem_Open_TabItem_Click;
            menuItem.Tag = jSAMObjects;
            contextMenu_Views.Items.Add(menuItem);

            menuItem = new MenuItem();
            menuItem.Name = "MenuItem_Close_TabItem";
            menuItem.Header = "Close";
            menuItem.Click += MenuItem_Close_TabItem_Click;
            menuItem.Tag = jSAMObjects;
            contextMenu_Views.Items.Add(menuItem);

            menuItem = new MenuItem();
            menuItem.Name = "MenuItem_Remove_TabItem";
            menuItem.Header = "Remove";
            menuItem.Click += MenuItem_Remove_TabItem_Click;
            menuItem.Tag = jSAMObjects;
            contextMenu_Views.Items.Add(menuItem);

            if (jSAMObjects.Count == 1)
            {
                menuItem = new MenuItem();
                menuItem.Name = "MenuItem_Duplicate_TabItem";
                menuItem.Header = "Duplicate";
                menuItem.Click += MenuItem_Duplicate_TabItem_Click;
                menuItem.Tag = jSAMObjects;
                contextMenu_Views.Items.Add(menuItem);

                menuItem = new MenuItem();
                menuItem.Name = "MenuItem_Settings_TabItem";
                menuItem.Header = "Settings";
                menuItem.Click += MenuItem_Settings_TabItem_Click;
                menuItem.Tag = jSAMObjects;
                contextMenu_Views.Items.Add(menuItem);
            }

            contextMenu_Views.IsOpen = true;
        }

        private void MenuItem_Remove_TabItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            if (menuItem == null)
            {
                return;
            }

            List<ViewSettings> viewSettingsList = GetViewSettings(menuItem);
            if (viewSettingsList == null || viewSettingsList.Count() == 0)
            {
                return;
            }

            Modify.RemoveViewSettings(uIAnalyticalModel, viewSettingsList.ConvertAll(x => x.Guid));
        }

        private void MenuItem_Open_TabItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            if (menuItem == null)
            {
                return;
            }

            List<ViewSettings> viewSettingsList = GetViewSettings(menuItem);
            if (viewSettingsList == null || viewSettingsList.Count() == 0)
            {
                return;
            }

            Modify.EnableViewSettings(uIAnalyticalModel, viewSettingsList.ConvertAll(x => x.Guid), true);
            Modify.ActivateViewSettings(uIAnalyticalModel, viewSettingsList.First().Guid);
        }

        private void MenuItem_Settings_TabItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            if (menuItem == null)
            {
                return;
            }

            List<ViewSettings> viewSettingsList = GetViewSettings(menuItem);
            if (viewSettingsList == null || viewSettingsList.Count() != 1)
            {
                return;
            }

            Modify.EditViewSettings(uIAnalyticalModel, viewSettingsList[0].Guid);
        }

        private void MenuItem_Duplicate_TabItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            if (menuItem == null)
            {
                return;
            }

            ViewSettings viewSettings = menuItem.Tag as ViewSettings;
            if (viewSettings == null)
            {
                return;
            }

            Modify.DuplicateViewSettings(uIAnalyticalModel, viewSettings.Guid);
        }

        private void MenuItem_Close_TabItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            if (menuItem == null)
            {
                return;
            }

            List<ViewSettings> viewSettingsList = GetViewSettings(menuItem);
            if (viewSettingsList == null || viewSettingsList.Count() == 0)
            {
                return;
            }

            if (viewSettingsList == null || viewSettingsList.Count() == 0)
            {
                return;
            }

            Modify.EnableViewSettings(uIAnalyticalModel, viewSettingsList.ConvertAll(x => x.Guid), false);
        }

        private void MenuItem_Select_Click(object sender, RoutedEventArgs e)
        {
            List<IJSAMObject> jSAMObjects = GetSAMObjects(sender as MenuItem);
            if (jSAMObjects == null || jSAMObjects.Count == 0)
            {
                return;
            }

            SelectionRequested?.Invoke(this, new SelectionRequestedEventArgs(jSAMObjects?.FindAll(x => x is SAMObject).ConvertAll(x => (SAMObject)x)));
        }

        private void MenuItem_Zoom_Click(object sender, RoutedEventArgs e)
        {
            List<IJSAMObject> jSAMObjects = GetSAMObjects(sender as MenuItem);
            if (jSAMObjects == null || jSAMObjects.Count == 0)
            {
                return;
            }

            ZoomRequested?.Invoke(this, new ZoomRequestedEventArgs(jSAMObjects?.FindAll(x => x is SAMObject).ConvertAll(x => (SAMObject)x)));
        }

        private void MenuItem_RemoveSpaceZone_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            if (menuItem == null)
            {
                return;
            }

            IEnumerable enumerable = menuItem.Tag as IEnumerable;
            if (enumerable == null)
            {
                return;
            }

            foreach(object @object in enumerable)
            {
                Tuple<Zone, Space> tuple = @object as Tuple<Zone, Space>;
                if(tuple == null)
                {
                    continue;
                }

                Modify.RemoveSpaceZone(uIAnalyticalModel, tuple.Item2, tuple.Item1);
            }
        }

        private void MenuItem_EditSpaceZone_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            if (menuItem == null)
            {
                return;
            }

            TreeViewItem treeViewItem = menuItem.Tag as TreeViewItem;
            if (treeViewItem == null)
            {
                return;
            }

            if (treeViewItem.Tag is Space)
            {
                Space space = (Space)treeViewItem.Tag;
                string zoneCategory = ((treeViewItem.Parent as TreeViewItem)?.Parent as TreeViewItem)?.Tag?.ToString();

                Modify.EditSpaceZone(uIAnalyticalModel, space, zoneCategory);
            }
        }

        private void MenuItem_Duplicate_Click(object sender, RoutedEventArgs e)
        {
            List<IJSAMObject> jSAMObjects = GetSAMObjects(sender as MenuItem);
            if(jSAMObjects == null || jSAMObjects.Count == 0)
            {
                return;
            }

            foreach(IMaterial material in jSAMObjects.FindAll(x => x is IMaterial))
            {
                UI.Modify.DuplicateMaterial(uIAnalyticalModel, material);
            }

            foreach (InternalCondition internalCondition in jSAMObjects.FindAll(x => x is InternalCondition))
            {
                UI.Modify.DuplicateInternalCondition(uIAnalyticalModel, internalCondition);
            }
        }

        private void MenuItem_Remove_Click(object sender, RoutedEventArgs e)
        {
            List<IJSAMObject> jSAMObjects = GetSAMObjects(sender as MenuItem);
            if (jSAMObjects == null || jSAMObjects.Count == 0)
            {
                return;
            }

            foreach(IMaterial material in jSAMObjects.FindAll(x => x is IMaterial))
            {
                UI.Modify.RemoveMaterial(uIAnalyticalModel, material);
            }
        }

        private void MenuItem_Create_Click(object sender, RoutedEventArgs e)
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

        private void MenuItem_Edit_Click(object sender, RoutedEventArgs e)
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

            List<IJSAMObject> jSAMObjects = GetSAMObjects(menuItem);
            if(jSAMObjects == null || jSAMObjects.Count == 0)
            {
                return;
            }

            Edit(jSAMObjects[0]);


        }

        private void Edit(IJSAMObject jSAMObject)
        {
            if (jSAMObject is Space)
            {
                UI.Modify.EditSpace(uIAnalyticalModel, jSAMObject as dynamic, windowHandle);
            }
            else if (jSAMObject is Panel)
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
            else if (jSAMObject is Zone)
            {
                Modify.EditZone(uIAnalyticalModel, jSAMObject as dynamic);
            }
            else if (jSAMObject is InternalCondition)
            {
                UI.Modify.EditInternalCondition(uIAnalyticalModel, jSAMObject as dynamic);
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

        private void UIAnalyticalModel_Modified(object sender, ModifiedEventArgs e)
        {
            LoadAnalyticalModel(uIAnalyticalModel?.JSAMObject, e);
        }

        private void UIAnalyticalModel_Opened(object sender, OpenedEventArgs e)
        {
            LoadAnalyticalModel(uIAnalyticalModel?.JSAMObject, e);
        }

        private void UIAnalyticalModel_Closed(object sender, ClosedEventArgs e)
        {
            LoadAnalyticalModel(uIAnalyticalModel?.JSAMObject, e);
        }

        private void LoadAnalyticalModel(AnalyticalModel analyticalModel, ModifiedEventArgs modifiedEventArgs)
        {
            LoadModel(analyticalModel);
            LoadViews(analyticalModel);
        }


        private void LoadModel(AnalyticalModel analyticalModel)
        {
            TreeView treeView_AnalyticalModel = treeView_Model;
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

            TreeViewItem treeViewItem_AnalyticalModel = new TreeViewItem() { Header = name, Tag = analyticalModel, AllowDrop = false };
            treeView_AnalyticalModel.Items.Add(treeViewItem_AnalyticalModel);

            TreeViewItem treeViewItem_Spaces = new TreeViewItem() { Header = "Spaces", Tag = typeof(Space), AllowDrop = false };
            treeViewItem_AnalyticalModel.Items.Add(treeViewItem_Spaces);

            TreeViewItem treeViewItem_Shades = new TreeViewItem() { Header = "Shades", Tag = typeof(Panel), AllowDrop = false };
            treeViewItem_AnalyticalModel.Items.Add(treeViewItem_Shades);

            TreeViewItem treeViewItem_Profiles = new TreeViewItem() { Header = "Profiles", Tag = typeof(Profile), AllowDrop = false };
            treeViewItem_AnalyticalModel.Items.Add(treeViewItem_Profiles);

            TreeViewItem treeViewItem_Materials = new TreeViewItem() { Header = "Materials", Tag = typeof(IMaterial), AllowDrop = false };
            treeViewItem_AnalyticalModel.Items.Add(treeViewItem_Materials);

            TreeViewItem treeViewItem_InternalConditions = new TreeViewItem() { Header = "Internal Conditions", Tag = typeof(InternalCondition), AllowDrop = false };
            treeViewItem_AnalyticalModel.Items.Add(treeViewItem_InternalConditions);

            TreeViewItem treeViewItem_MechanicalSystems = new TreeViewItem() { Header = "Mechanical Systems", Tag = typeof(MechanicalSystemType), AllowDrop = false };
            treeViewItem_AnalyticalModel.Items.Add(treeViewItem_MechanicalSystems);

            TreeViewItem treeViewItem_Zones = new TreeViewItem() { Header = "Zones", Tag = typeof(Zone), AllowDrop = false };
            treeViewItem_AnalyticalModel.Items.Add(treeViewItem_Zones);

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;
            if (adjacencyCluster != null)
            {
                List<Space> spaces = adjacencyCluster.GetSpaces();
                if (spaces != null)
                {
                    foreach (Space space in spaces)
                    {
                        TreeViewItem treeViewItem_Space = new TreeViewItem() { Header = space.Name, Tag = space, AllowDrop = true };
                        treeViewItem_Spaces.Items.Add(treeViewItem_Space);
                        List<Panel> panels_Space = adjacencyCluster.GetPanels(space);
                        if (panels_Space != null)
                        {
                            foreach (Panel panel in panels_Space)
                            {
                                TreeViewItem treeViewItem_Panel = new TreeViewItem() { Header = panel.Name, Tag = panel, AllowDrop = false};
                                treeViewItem_Space.Items.Add(treeViewItem_Panel);

                                List<Aperture> apertures = panel.Apertures;
                                if (apertures != null)
                                {
                                    foreach (Aperture aperture in apertures)
                                    {
                                        TreeViewItem treeViewItem_Aperture = new TreeViewItem() { Header = aperture.Name, Tag = aperture, AllowDrop = false };
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
                            TreeViewItem treeViewItem_Shade = new TreeViewItem() { Header = panel.Name, Tag = panel, AllowDrop = false };
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
                        TreeViewItem treeViewItem_InternalCondition = new TreeViewItem() { Header = internalCondition.Name, Tag = internalCondition, AllowDrop = false };
                        treeViewItem_InternalCondition.MouseDoubleClick += TreeViewItem_MouseDoubleClick;
                        treeViewItem_InternalConditions.Items.Add(treeViewItem_InternalCondition);
                    }
                }

                List<MechanicalSystemType> mechanicalSystemTypes = adjacencyCluster.GetMechanicalSystemTypes<MechanicalSystemType>();
                if (mechanicalSystemTypes != null)
                {
                    foreach (MechanicalSystemType mechanicalSystemType in mechanicalSystemTypes)
                    {
                        TreeViewItem treeViewItem_MechanicalSystemType = new TreeViewItem() { Header = mechanicalSystemType.Name, Tag = mechanicalSystemType, AllowDrop = false};
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
                    TreeViewItem treeViewItem_Profile = new TreeViewItem() { Header = profile.Name, Tag = profile, AllowDrop = false };
                    treeViewItem_Profile.MouseDoubleClick += TreeViewItem_MouseDoubleClick;
                    treeViewItem_Profiles.Items.Add(treeViewItem_Profile);
                }
            }

            List<IMaterial> materials = analyticalModel.MaterialLibrary?.GetMaterials();
            if (materials != null)
            {
                foreach (IMaterial material in materials)
                {
                    TreeViewItem treeViewItem_Material = new TreeViewItem() { Header = material.Name, Tag = material, AllowDrop = false };
                    treeViewItem_Materials.Items.Add(treeViewItem_Material);
                }
            }

            List<Zone> zones = adjacencyCluster?.GetZones();
            if(zones != null)
            {
                SortedDictionary<string, List<Zone>> dictionary = new SortedDictionary<string, List<Zone>>();
                foreach(Zone zone in zones)
                {
                    if(zone == null)
                    {
                        continue;
                    }

                    if(!zone.TryGetValue(ZoneParameter.ZoneCategory, out string zoneCategory) || string.IsNullOrWhiteSpace(zoneCategory))
                    {
                        zoneCategory = "???";
                    }

                    if(!dictionary.TryGetValue(zoneCategory, out List<Zone> zones_Temp))
                    {
                        zones_Temp = new List<Zone>();
                        dictionary[zoneCategory] = zones_Temp;
                    }

                    zones_Temp.Add(zone);
                }

                foreach(KeyValuePair<string, List<Zone>> keyValuePair in dictionary)
                {
                    TreeViewItem treeViewItem_ZoneCategory = new TreeViewItem() { Header = keyValuePair.Key, Tag = keyValuePair.Key, AllowDrop = false };
                    treeViewItem_Zones.Items.Add(treeViewItem_ZoneCategory);

                    treeViewItem_ZoneCategory.IsExpanded = expandedTags.Find(x => x is string && (string)x == keyValuePair.Key) != null;

                    foreach (Zone zone in keyValuePair.Value)
                    {
                        TreeViewItem treeViewItem_Zone = new TreeViewItem() { Header = zone.Name, Tag = zone, AllowDrop = true };
                        treeViewItem_ZoneCategory.Items.Add(treeViewItem_Zone);

                        treeViewItem_Zone.IsExpanded = expandedTags.Find(x => x is Zone && ((Zone)x).Guid == zone.Guid) != null;

                        List<Space> spaces = adjacencyCluster.GetRelatedObjects<Space>(zone);
                        if(spaces != null && spaces.Count != 0)
                        {
                            foreach(Space space in spaces)
                            {
                                TreeViewItem treeViewItem_Space = new TreeViewItem() { Header = space.Name, Tag = space, AllowDrop = true };
                                treeViewItem_Zone.Items.Add(treeViewItem_Space);
                            }
                        }
                    }

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
                treeViewItem_Zones.IsExpanded = expandedTags.Contains(treeViewItem_Zones.Tag);
            }
        }

        private void TreeViewItem_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            TreeViewItem treeViewItem = sender as TreeViewItem;
            if(treeViewItem == null)
            {
                return;
            }

            IAnalyticalObject analyticalObject = treeViewItem.Tag as IAnalyticalObject;
            if(analyticalObject == null)
            {
                return;
            }

            if(analyticalObject is InternalCondition)
            {
                UI.Modify.EditInternalCondition(uIAnalyticalModel, (InternalCondition)analyticalObject);
            }
            else if(analyticalObject is Profile)
            {
                UI.Modify.EditProfile(uIAnalyticalModel, (Profile)analyticalObject);
            }
        }

        private void LoadViews(AnalyticalModel analyticalModel)
        {
            if (treeView_Views == null)
            {
                return;
            }

            treeView_Views.Items.Clear();

            if (analyticalModel == null)
            {
                return;
            }

            string name = analyticalModel.Name;
            if (string.IsNullOrWhiteSpace(name))
            {
                name = "???";
            }

            TreeViewItem treeViewItem_AnalyticalModel = new TreeViewItem() { Header = name, Tag = analyticalModel, AllowDrop = false };
            treeView_Views.Items.Add(treeViewItem_AnalyticalModel);

            TreeViewItem treeViewItem_Views = new TreeViewItem() { Header = "Views", Tag = typeof(ViewSettings), AllowDrop = true };
            treeViewItem_AnalyticalModel.Items.Add(treeViewItem_Views);

            if (!analyticalModel.TryGetValue(AnalyticalModelParameter.UIGeometrySettings, out UIGeometrySettings uIGeometrySettings) || uIGeometrySettings == null)
            {
                return;
            }

            Dictionary<string, TreeViewItem> dictionary = new Dictionary<string, TreeViewItem>();

            List<ViewSettings> viewSettingsList = uIGeometrySettings.GetViewSettings<ViewSettings>();
            foreach(ViewSettings viewSettings in viewSettingsList)
            {
                string name_Temp = viewSettings.Name;
                if(string.IsNullOrWhiteSpace(name_Temp))
                {
                    name_Temp = Query.DefaultName(viewSettings);
                }

                if(string.IsNullOrWhiteSpace(name_Temp))
                {
                    name_Temp = "???";
                }

                TreeViewItem treeViewItem_ViewSettings = new TreeViewItem() { Header = name_Temp, Tag = viewSettings, AllowDrop = false };
                treeViewItem_ViewSettings.PreviewMouseDoubleClick += TreeViewItem_ViewSettings_PreviewMouseDoubleClick;
                
                if(viewSettings.TryGetValue(ViewSettingsParameter.Group, out string group) && !string.IsNullOrWhiteSpace(group))
                {
                    if(!dictionary.TryGetValue(group, out TreeViewItem treeViewItem) || treeViewItem == null)
                    {
                        treeViewItem = new TreeViewItem() { Header = group, Tag = group, AllowDrop = true };
                        dictionary[group] = treeViewItem;
                        treeViewItem_Views.Items.Add(treeViewItem);
                    }

                    treeViewItem.Items.Add(treeViewItem_ViewSettings);
                }
                else
                {
                    treeViewItem_Views.Items.Add(treeViewItem_ViewSettings);
                }
            }

            treeViewItem_AnalyticalModel.ExpandSubtree();
        }

        private void TreeViewItem_ViewSettings_PreviewMouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            TreeViewItem treeViewItem = sender as TreeViewItem;
            if (treeViewItem == null)
            {
                return;
            }

            ViewSettings viewSettings = treeViewItem.Tag as ViewSettings;
            if (viewSettings == null)
            {
                return;
            }

            Modify.EnableViewSettings(uIAnalyticalModel, viewSettings.Guid, true);
            Modify.ActivateViewSettings(uIAnalyticalModel, viewSettings.Guid);
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

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            System.Windows.Window window = Geometry.UI.WPF.Query.Window(this);
            if(window != null)
            {
                windowHandle = new Core.Windows.WindowHandle(window);
            }
        }

        private static List<ViewSettings> GetViewSettings(MenuItem menuItem)
        {
            if(menuItem == null)
            {
                return null;
            }

            List<ViewSettings> result = new List<ViewSettings>();

            IEnumerable enumerable = menuItem.Tag as IEnumerable;
            if (enumerable != null)
            {
                result = enumerable.OfType<ViewSettings>()?.ToList();
            }

            if (result == null || result.Count() == 0)
            {
                ViewSettings viewSettings = menuItem.Tag as ViewSettings;
                if (viewSettings != null)
                {
                    result = new List<ViewSettings>() { viewSettings };
                }
            }

            return result;
        }

        private static List<IJSAMObject> GetSAMObjects(MenuItem menuItem)
        {
            if (menuItem == null)
            {
                return null;
            }

            List<IJSAMObject> result = null;
            if (menuItem.Tag is IEnumerable)
            {
                result = new List<IJSAMObject>();
                foreach (object @object in (IEnumerable)menuItem.Tag)
                {
                    if (@object is IJSAMObject)
                    {
                        result.Add((IJSAMObject)@object);
                    }
                }
            }
            else if (menuItem.Tag is IJSAMObject)
            {
                result = new List<IJSAMObject>() { (IJSAMObject)menuItem.Tag };
            }

            return result;
        }

        private void treeView_Model_PreviewMouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DependencyObject dependencyObject = Core.UI.WPF.Query.DependencyObject<DependencyObject>(treeView_Model, e.GetPosition(treeView_Model));
            if(dependencyObject == null)
            {
                return;
            }

            TreeViewItem treeViewItem = Core.UI.WPF.Query.ParentDependencyObject<TreeViewItem>(dependencyObject);

            object @object = treeViewItem?.Tag;
            if(@object == null || !(@object is ISAMObject))
            {
                return;
            }

            Edit(@object as ISAMObject);
        }
    }
}
