using SAM.Core;
using SAM.Core.UI;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for FilterWindow.xaml
    /// </summary>
    public partial class FilterWindow : System.Windows.Window
    {
        private AdjacencyCluster adjacencyCluster = null;
        private List<IJSAMObject> jSAMObjects = null;

        public event Core.UI.WPF.FilterAddingEventHandler FilterAdding;
        public event Core.UI.WPF.FilterChangedEventHandler FilterChanged;

        public FilterWindow()
        {
            InitializeComponent();

            filterControl.FilterAdding += FilterControl_FilterAdding;
            filterControl.FilterChanged += FilterControl_FilterChanged;

            filtersControl.SelectionChanged += FiltersControl_SelectionChanged;
            filtersControl.FilterAdding += FiltersControl_FilterAdding;
        }

        private void FilterControl_FilterChanged(object sender, Core.UI.WPF.FilterChangedEventArgs e)
        {
            Refresh();

            FilterChanged?.Invoke(this, new Core.UI.WPF.FilterChangedEventArgs(e.UIFilter));
        }

        private void FiltersControl_FilterAdding(object sender, Core.UI.WPF.FilterAddingEventArgs e)
        {
            e.Handled = true;

            using (Core.Windows.Forms.TextBoxForm<string> textBoxForm = new Core.Windows.Forms.TextBoxForm<string>("Filter", "Filter Name"))
            {
                if (textBoxForm.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                {
                    return;
                }

                string name = textBoxForm.Value;

                if(string.IsNullOrWhiteSpace(name))
                {
                    return;
                }

                e.UIFilter = filterControl.GetUIFilter(name);
            }
        }

        private void FiltersControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            IUIFilter uIFilter = filtersControl.SelectedUIFilter;

            UI.Modify.AssignAdjacencyCluster(uIFilter, adjacencyCluster);

            filterControl.UIFilter = uIFilter;
        }

        private void FilterControl_FilterAdding(object sender, Core.UI.WPF.FilterAddingEventArgs e)
        {
            FilterAdding?.Invoke(this, e);
        }

        public List<Type> Types
        {
            get
            {
                return filterControl.Types;
            }

            set
            {
                filterControl.Types = value;
            }
        }

        public IUIFilter UIFilter
        {
            get
            {
                return filterControl.UIFilter;
            }

            set
            {
                filterControl.UIFilter = value;
                filtersControl.SelectedUIFilter = null;
            }
        }

        public List<IUIFilter> UIFilters
        {
            get
            {
                return filtersControl.UIFilters;
            }

            set
            {
                filtersControl.UIFilters = value;
            }
        }

        public IUIFilter SelectedUIFilters
        {
            get
            {
                return filtersControl.SelectedUIFilter;
            }

            set
            {
                filtersControl.SelectedUIFilter = value;
            }
        }

        public List<IJSAMObject> JSAMObjects
        {
            get
            {
                return jSAMObjects;
            }

            set
            {
                jSAMObjects = value;
                Refresh();
            }
        }

        public AdjacencyCluster AdjacencyCluster
        {
            get
            {
                return adjacencyCluster;
            }

            set
            {
                adjacencyCluster = value;
                Refresh();
            }
        }

        public List<IJSAMObject> FilteredJSAMObjects
        {
            get
            {
                return GetFilteredSAMObjects();
            }
        }

        private List<IJSAMObject>  GetFilteredSAMObjects()
        {
            Core.IFilter filter = UIFilter.Transform();

            UI.Modify.AssignAdjacencyCluster(filter, adjacencyCluster);

            return adjacencyCluster.Filter<IJSAMObject>(filter, jSAMObjects);
        }

        public Type Type
        {
            get
            {
                return filterControl.Type;
            }

            set
            {
                filterControl.Type = value;
            }
        }

        private void button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void button_OK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void Refresh()
        {
            listBox_Before.Items.Clear();
            listBox_After.Items.Clear();
            
            List<IJSAMObject> jSAMObjects = this.jSAMObjects != null && this.jSAMObjects.Count != 0 ? new List<IJSAMObject>(this.jSAMObjects) : Analytical.Query.FilteringSAMObjects(adjacencyCluster);
            jSAMObjects?.RemoveAll(x => x == null || !x.GetType().IsAssignableFrom(Type));

            if (jSAMObjects != null)
            {
                foreach (IJSAMObject jSAMObject in jSAMObjects)
                {
                    string name = (jSAMObject as SAMObject)?.Name;
                    if(string.IsNullOrWhiteSpace(name))
                    {
                        continue;
                    }

                    ListBoxItem listBoxItem = new ListBoxItem() { Tag = jSAMObject, Content = name };
                    listBox_Before.Items.Add(listBoxItem);
                }
            }


            jSAMObjects = GetFilteredSAMObjects();
            if (jSAMObjects != null)
            {
                foreach (IJSAMObject jSAMObject in jSAMObjects)
                {
                    string name = (jSAMObject as SAMObject)?.Name;
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        continue;
                    }

                    ListBoxItem listBoxItem = new ListBoxItem() { Tag = jSAMObject, Content = name };
                    listBox_After.Items.Add(listBoxItem);
                }
            }
        }
    }
}
