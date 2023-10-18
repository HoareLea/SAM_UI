using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SAM.Core.UI.WPF
{
    /// <summary>
    /// Interaction logic for RelationClusterComplexReferenceControl.xaml
    /// </summary>
    public partial class RelationClusterComplexReferenceControl : UserControl
    {
        private RelationCluster relationCluster;

        public RelationClusterComplexReferenceControl()
        {
            InitializeComponent();
        }

        private void LoadRelationCluster()
        {
            LoadTypes();
        }

        private void LoadTypes()
        {
            ComboBox_Type.Items.Clear();
            if (relationCluster == null)
            {
                return;
            }

            List<Type> types = relationCluster.GetTypes();
            if (types != null && types.Count > 0)
            {
                foreach(Type type in types)
                {
                    ComboBox_Type.Items.Add(new ComboBoxItem() { Content = type.Name, Name = type.Name, Tag = type });
                }
                ComboBox_Type.SelectedIndex = 0;
            }
        }

        private void LoadProperties()
        {
            TreeView_Property.Items.Clear();

            ComboBoxItem comboBoxItem = ComboBox_Type.SelectedItem as ComboBoxItem;
            if(comboBoxItem == null)
            {
                return;
            }

            Type type = comboBoxItem.Tag as Type;

            List<object> objects = type == null ? relationCluster.GetObjects() : relationCluster.GetObjects(type);
            if(objects == null || objects.Count == 0)
            {
                return;
            }

            TreeView_Property.Tag = new PathReference(new ObjectReference[] { new ObjectReference(type.FullName) });

            AddTreeViewItems(TreeView_Property, objects, true);

        }

        private void AddTreeViewItems(TreeViewItem treeViewItem, bool loadChilds = false)
        {
            IComplexReference complexReference = treeViewItem.Tag as IComplexReference;
            if (complexReference == null)
            {
                return;
            }

            if(treeViewItem.Items.Count != 0)
            {
                return;
            }

            List<object> objects = relationCluster?.GetValues(complexReference);
            if(objects == null || objects.Count == 0)
            {
                return;
            }

            AddTreeViewItems(treeViewItem, objects, loadChilds);
        }

        private void AddTreeViewItems(ItemsControl itemsControl, IEnumerable<object> objects, bool loadChilds = false)
        {
            if (itemsControl == null || objects == null || objects.Count() == 0)
            {
                return;
            }

            PathReference pathReference = itemsControl.Tag as PathReference;

            Dictionary<string, Type> dictionary = new Dictionary<string, Type>();
            HashSet<Type> types = new HashSet<Type>();

            foreach (object @object in objects)
            {
                List<string> propertyNames = Core.Query.UserFriendlyNames(@object);
                if (propertyNames != null && propertyNames.Count != 0)
                {
                    foreach (string propertyName in propertyNames)
                    {
                        if (!Core.Query.TryGetValue(@object, propertyName, out object value) || value == null)
                        {
                            continue;
                        }

                        dictionary[propertyName] = value.GetType();

                    }
                }

                List<object> objects_Related = relationCluster.GetRelatedObjects(@object);
                if (objects_Related != null && objects_Related.Count != 0)
                {
                    objects_Related.ForEach(x => types.Add(x.GetType()));
                }
            }

            if (dictionary != null && dictionary.Count > 0)
            {
                foreach (KeyValuePair<string, Type> keyValuePair in dictionary)
                {
                    PropertyReference propertyReference = new PropertyReference(keyValuePair.Key);

                    PathReference pathReference_Temp = new PathReference(pathReference, propertyReference);

                    if (typeof(IJSAMObject).IsAssignableFrom(keyValuePair.Value))
                    {
                        TreeViewItem treeViewItem = new TreeViewItem() { Header = keyValuePair.Key, Tag = pathReference_Temp };
                        itemsControl.Items.Add(treeViewItem);
                        if(loadChilds)
                        {
                            AddTreeViewItems(treeViewItem, false);
                            treeViewItem.Expanded += TreeViewItem_Expanded;
                        }
                    }
                    else
                    {
                        itemsControl.Items.Add(new TreeViewItem() { Header = keyValuePair.Key, Tag = pathReference_Temp });
                    }
                }
            }

            if (types != null && types.Count != 0)
            {
                foreach (Type type_Temp in types)
                {
                    PathReference pathReference_Temp = new PathReference(pathReference, new ObjectReference(type_Temp.FullName));
                    TreeViewItem treeViewItem = new TreeViewItem() { Header = type_Temp.Name, Tag = pathReference_Temp };
                    itemsControl.Items.Add(treeViewItem);
                    if (loadChilds)
                    {
                        AddTreeViewItems(treeViewItem, false);
                        treeViewItem.Expanded += TreeViewItem_Expanded;
                    }
                }
            }
        }

        private void TreeViewItem_Expanded(object sender, RoutedEventArgs e)
        {
            TreeViewItem treeViewItem = sender as TreeViewItem;

            if(treeViewItem == null)
            {
                return;
            }

            treeViewItem.Expanded -= TreeViewItem_Expanded;

            foreach(TreeViewItem treeViewItem_Temp in treeViewItem.Items)
            {
                PathReference pathReference = treeViewItem_Temp.Tag as PathReference;

                List<object> objects = relationCluster.GetValues(pathReference)?.FindAll(x => x is IJSAMObject);
                if (objects == null || objects.Count == 0)
                {
                    continue;
                }

                AddTreeViewItems(treeViewItem_Temp, objects, false);
            }

        }

        public RelationCluster RelationCluster
        {
            get
            {
                return relationCluster;
            }

            set
            {
                relationCluster = value;
                LoadRelationCluster();
            }
        }

        private void ComboBox_Type_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadProperties();
        }

        private void TreeView_Property_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            TextBox_Reference.Text = ComplexReference?.ToString();
        }

        public IComplexReference ComplexReference
        {
            get
            {
                return (TreeView_Property.SelectedItem as TreeViewItem)?.Tag as IComplexReference;
            }
        }

        public Type Type
        {
            get
            {
                return (ComboBox_Type.SelectedItem as ComboBoxItem)?.Tag as Type;
            }

            set
            {
                foreach(ComboBoxItem comboBoxItem in ComboBox_Type.Items)
                {
                    if(comboBoxItem.Tag as Type == value)
                    {
                        ComboBox_Type.SelectedItem = comboBoxItem;
                        return;
                    }
                }
            }
        }

        public bool TypesEnabled
        {
            get
            {
                return ComboBox_Type.IsEnabled;
            }

            set
            {
                ComboBox_Type.IsEnabled = value;
            }
        }
    }
}
