﻿using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for SelectConstructionWindow.xaml
    /// </summary>
    public partial class SelectConstructionWindow : System.Windows.Window
    {
        ConstructionManager constructionManager;

        public SelectConstructionWindow()
        {
            InitializeComponent();
        }

        public ConstructionManager ConstructionManager
        {
            get
            {
                return constructionManager;
            }

            set
            {
                SetConstructionManager(value);
            }
        }

        private void SetConstructionManager(ConstructionManager constructionManager)
        {
            this.constructionManager = constructionManager;

            TreeView_Main.Items.Clear();

            if(constructionManager == null)
            {
                return;
            }

            List<Construction> constructions = constructionManager.Constructions;
            if(constructions != null && constructions.Count != 0)
            {
                TreeViewItem treeViewItem_Constructions = new TreeViewItem() { Header = "Constructions" };

                foreach(Construction construction in constructions)
                {
                    if(construction == null)
                    {
                        continue;
                    }

                    string name = construction.Name;
                    if(string.IsNullOrEmpty(name))
                    {
                        name = "???";
                    }

                    TreeViewItem treeViewItem = new TreeViewItem() { Header = name, Tag = construction };
                    treeViewItem.MouseDoubleClick += TreeViewItem_MouseDoubleClick;

                    treeViewItem_Constructions.Items.Add(treeViewItem);
                }

                treeViewItem_Constructions.IsExpanded = true;

                TreeView_Main.Items.Add(treeViewItem_Constructions);
            }

            List<ApertureConstruction> apertureConstructions = constructionManager.ApertureConstructions;
            if (apertureConstructions != null && apertureConstructions.Count != 0)
            {
                TreeViewItem treeViewItem_Windows = new TreeViewItem() { Header = "Windows" };
                TreeViewItem treeViewItem_Doors = new TreeViewItem() { Header = "Doors" };

                foreach (ApertureConstruction apertureConstruction in apertureConstructions)
                {
                    if (apertureConstruction == null)
                    {
                        continue;
                    }

                    string name = apertureConstruction.Name;
                    if (string.IsNullOrEmpty(name))
                    {
                        name = "???";
                    }

                    TreeViewItem treeViewItem_Parent = apertureConstruction.ApertureType == ApertureType.Window ? treeViewItem_Windows : treeViewItem_Doors;

                    TreeViewItem treeViewItem = new TreeViewItem() { Header = name, Tag = apertureConstruction };
                    treeViewItem.MouseDoubleClick += TreeViewItem_MouseDoubleClick;

                    treeViewItem_Parent.Items.Add(treeViewItem);
                }

                treeViewItem_Windows.IsExpanded = true;
                treeViewItem_Doors.IsExpanded = true;

                TreeView_Main.Items.Add(treeViewItem_Doors);
                TreeView_Main.Items.Add(treeViewItem_Windows);
            }
        }

        private void TreeViewItem_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            IAnalyticalObject analyticalObject = GetSelectedAnalyticalObject();
            if(analyticalObject == null)
            {
                return;
            }

            DialogResult = true;

            Close();
        }

        private void Button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;

            Close();
        }

        private void Button_OK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;

            Close();
        }

        public IAnalyticalObject GetSelectedAnalyticalObject()
        {
            foreach (TreeViewItem treeViewItem_Temp in TreeView_Main.Items)
            {
                IAnalyticalObject analyticalObject = GetSelectedAnalyticalObject(treeViewItem_Temp);
                if (analyticalObject != null)
                {
                    return analyticalObject;
                }
            }

            return null;
        }

        private IAnalyticalObject GetSelectedAnalyticalObject(TreeViewItem treeViewItem)
        {
            if(treeViewItem == null)
            {
                return null;
            }

            if(treeViewItem.IsSelected)
            {
                return treeViewItem.Tag as IAnalyticalObject;
            }

            foreach(TreeViewItem treeViewItem_Temp in treeViewItem.Items)
            {
                IAnalyticalObject analyticalObject = GetSelectedAnalyticalObject(treeViewItem_Temp);
                if(analyticalObject != null)
                {
                    return analyticalObject;
                }
            }

            return null;
        }
    }
}
