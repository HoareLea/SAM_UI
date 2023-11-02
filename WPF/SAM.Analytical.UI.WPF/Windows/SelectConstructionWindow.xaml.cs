using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

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

                    treeViewItem_Constructions.Items.Add(new TreeViewItem() { Header = name, Tag = construction });
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

                    TreeViewItem treeViewItem = apertureConstruction.ApertureType == ApertureType.Window ? treeViewItem_Windows : treeViewItem_Doors;

                    treeViewItem.Items.Add(new TreeViewItem() { Header = name, Tag = apertureConstruction });
                }

                treeViewItem_Windows.IsExpanded = true;
                treeViewItem_Doors.IsExpanded = true;

                TreeView_Main.Items.Add(treeViewItem_Doors);
                TreeView_Main.Items.Add(treeViewItem_Windows);
            }
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
