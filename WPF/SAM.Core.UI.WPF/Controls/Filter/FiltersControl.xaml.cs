using Microsoft.Win32;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SAM.Core.UI.WPF
{
    /// <summary>
    /// Interaction logic for FiltersControl.xaml
    /// </summary>
    public partial class FiltersControl : UserControl
    {
        public event SelectionChangedEventHandler SelectionChanged;
        public event FilterAddingEventHandler FilterAdding;

        public FiltersControl()
        {
            InitializeComponent();
        }

        public List<IUIFilter> UIFilters
        {
            get
            {
                return GetUIFilters();
            }

            set
            {
                SetUIFilters(value);
            }
        }

        public IUIFilter SelectedUIFilter
        {
            get
            {
                return (listBox.SelectedItem as ListBoxItem)?.Tag as IUIFilter;
            }

            set
            {
                if(listBox.SelectionMode == SelectionMode.Single)
                {
                    listBox.SelectedItem = null;
                }
                else
                {
                    listBox.SelectedItems.Clear();
                }

                if(value == null)
                {
                    return;
                }

                foreach(ListBoxItem listBoxItem in listBox.Items)
                {
                    if(listBoxItem.Tag == value)
                    {
                        if (listBox.SelectionMode == SelectionMode.Single)
                        {
                            listBox.SelectedItem = listBoxItem;
                        }
                        else
                        {
                            listBox.SelectedItems.Add(listBoxItem);
                        }
                        break;
                    }
                }
            }
        }

        public List<IUIFilter> GetUIFilters()
        {
            if(listBox.Items == null)
            {
                return null;
            }

            List<IUIFilter> result = new List<IUIFilter>();
            foreach (ListBoxItem listBoxItem in listBox.Items)
            {
                IUIFilter uIFilter = listBoxItem?.Tag as IUIFilter;
                if (uIFilter == null)
                {
                    continue;
                }

                result.Add(uIFilter);
            }

            return result;
        }

        public void SetUIFilters(IEnumerable<IUIFilter> uIFilters)
        {
            List<IUIFilter> uIFilters_Selected = listBox?.SelectedItems.Cast<ListBoxItem>().ToList().ConvertAll(x => x.Tag as IUIFilter);
            uIFilters_Selected?.RemoveAll(x => x == null);

            listBox.Items.Clear();

            if(uIFilters == null)
            {
                return;
            }

            List<IUIFilter> uIFilters_Temp = new List<IUIFilter>(uIFilters);
            uIFilters_Temp.RemoveAll(x => x?.Name == null);

            uIFilters_Temp.Sort((x, y) => x.Name.CompareTo(y.Name));

            foreach (IUIFilter uIFilter in uIFilters)
            {
                if(uIFilter == null)
                {
                    continue;
                }

                ListBoxItem listBoxItem = new ListBoxItem() { Content = string.IsNullOrWhiteSpace(uIFilter.Name) ? "???" : uIFilter.Name, Tag = uIFilter };
                ContextMenu contextMenu = new ContextMenu();

                MenuItem menuItem = null;

                menuItem = new MenuItem();
                menuItem.Name = "MenuItem_Remove";
                menuItem.Header = "Remove";
                menuItem.Click += MenuItem_Remove_Click;
                menuItem.Tag = listBoxItem;
                contextMenu.Items.Add(menuItem);

                menuItem = new MenuItem();
                menuItem.Name = "MenuItem_Rename";
                menuItem.Header = "Rename";
                menuItem.Click += MenuItem_Rename_Click;
                menuItem.Tag = listBoxItem;
                contextMenu.Items.Add(menuItem);

                listBoxItem.ContextMenu = contextMenu;

                listBox.Items.Add(listBoxItem);

                if(uIFilters_Selected != null && uIFilters_Selected.Contains(uIFilter))
                {
                    if(listBox.SelectionMode == SelectionMode.Single)
                    {
                        listBox.SelectedItem = listBoxItem;
                    }
                    else
                    {
                        listBox.SelectedItems.Add(listBoxItem);
                    }
                }
            }
        }

        private void MenuItem_Rename_Click(object sender, RoutedEventArgs e)
        {
            ListBoxItem listBoxItem = (sender as MenuItem)?.Tag as ListBoxItem;
            if(listBoxItem == null)
            {
                return;
            }

            IUIFilter uIFilter = listBoxItem.Tag as IUIFilter;
            if(uIFilter == null)
            {
                return;
            }

            string name = null;
            using (Windows.Forms.TextBoxForm<string> textBoxForm = new Windows.Forms.TextBoxForm<string>("Filter", "Filter Name"))
            {
                textBoxForm.Value = uIFilter.Name;
                if (textBoxForm.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                {
                    return;
                }
                name = textBoxForm.Value;
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            uIFilter.Name = name;
            listBoxItem.Content = name;
        }

        private void MenuItem_Remove_Click(object sender, RoutedEventArgs e)
        {
            ListBoxItem listBoxItem = (sender as MenuItem)?.Tag as ListBoxItem;
            if (listBoxItem == null)
            {
                return;
            }

            if (MessageBox.Show("Are you sure to remove selected filter?", "Remove Filter", MessageBoxButton.OKCancel) != MessageBoxResult.OK)
            {
                return;
            }

            listBox.Items.Remove(listBoxItem);
        }

        private void listBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectionChanged?.Invoke(this, e);
        }

        private void button_Remove_Click(object sender, RoutedEventArgs e)
        {
            if(listBox.SelectedItems == null || listBox.SelectedItems.Count == 0)
            {
                return;
            }

            if(MessageBox.Show("Are you sure to remove selected filters?", "Remove Filters", MessageBoxButton.OKCancel) != MessageBoxResult.OK)
            {
                return;
            }

            List<ListBoxItem> listBoxItems = new List<ListBoxItem>();
            foreach(ListBoxItem listBoxItem in listBox.SelectedItems)
            {
                listBoxItems.Add(listBoxItem);
            }

            listBoxItems.ForEach(x => listBox.Items.Remove(x));
        }

        private void button_Add_Click(object sender, RoutedEventArgs e)
        {
            FilterAddingEventArgs filterAddingEventArgs = new FilterAddingEventArgs(null);

            FilterAdding?.Invoke(this, filterAddingEventArgs);

            if(!filterAddingEventArgs.Handled)
            {
                string name = null;
                using (Windows.Forms.TextBoxForm<string> textBoxForm = new Windows.Forms.TextBoxForm<string>("Filter", "Filter Name"))
                {
                    if(textBoxForm.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                    {
                        return;
                    }
                }

                if(string.IsNullOrWhiteSpace(name))
                {
                    return;
                }

                filterAddingEventArgs.UIFilter = new UILogicalFilter(name, null, new LogicalFilter(FilterLogicalOperator.Or));
            }

            Add(filterAddingEventArgs.UIFilter);
        }

        public bool Add(IUIFilter uIFilter)
        {
            if(uIFilter == null || string.IsNullOrWhiteSpace(uIFilter.Name))
            {
                return false;
            }

            List<IUIFilter> uIFilters = UIFilters;
            if(uIFilters == null)
            {
                uIFilters = new List<IUIFilter>();
            }

            uIFilters.RemoveAll(x => x.Name == uIFilter.Name);

            uIFilters.Add(uIFilter);

            UIFilters = uIFilters;
            return true;
        }

        private void button_Export_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "json files (*.json)|*.json|All files (*.*)|*.*";
            saveFileDialog.FilterIndex = 1;
            saveFileDialog.RestoreDirectory = true;
            saveFileDialog.FileName = "Filters";
            if (saveFileDialog.ShowDialog() == false)
            {
                return;
            }

            string path = saveFileDialog.FileName;
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            Core.Convert.ToFile(UIFilters, path);
        }

        private void button_Import_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "json files (*.json)|*.json|All files (*.*)|*.*";
            openFileDialog.FilterIndex = 1;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.FileName = "Filters";
            if (openFileDialog.ShowDialog() == false)
            {
                return;
            }

            string path = openFileDialog.FileName;
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            List<IUIFilter> filters_Temp = Core.Convert.ToSAM<IUIFilter>(path);
            if(filters_Temp == null || filters_Temp.Count == 0)
            {
                return;
            }

            List<IUIFilter> filters = UIFilters;
            if(filters == null)
            {
                filters = new List<IUIFilter>();
            }

            filters.AddRange(filters_Temp);

            UIFilters = filters;
        }
    }
}
