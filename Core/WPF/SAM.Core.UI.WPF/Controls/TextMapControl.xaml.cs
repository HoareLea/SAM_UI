using Microsoft.Win32;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SAM.Core.UI.WPF
{
    /// <summary>
    /// Interaction logic for TextMapControl.xaml
    /// </summary>
    public partial class TextMapControl : UserControl
    {
        private TextMap textMap;

        public TextMapControl()
        {
            InitializeComponent();
        }

        public TextMapControl(TextMap textMap)
        {
            InitializeComponent();

            SetTextMap(textMap);
        }

        public TextMap TextMap
        {
            get
            {
                return textMap;
            }

            set
            {
                SetTextMap(value);
            }
        }

        public string Text
        {
            get
            {
                return textBox_Text.Text;
            }

            set
            {
                textBox_Text.Text = value;
            }
        }

        private void SetTextMap(TextMap textMap)
        {
            this.textMap = textMap == null ? null : Core.Create.TextMap(textMap);
            textBox_Name.Text = textMap?.Name;

            Filter(this.textMap);
            Test(textBox_Text.Text);
        }

        private TextMap GetTextMap()
        {
            TextMap result = textMap == null ? Core.Create.TextMap(textBox_Text.Text) : Core.Create.TextMap(textBox_Text.Text, textMap);
            return result;
        }

        public void Test(string text)
        {
            listBox_Values.Items.Clear();

            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            HashSet<string> values = textMap?.GetSortedKeys(text);
            if (values == null)
            {
                return;
            }

            foreach (string value in values)
            {
                listBox_Values.Items.Add(value);
            }
        }

        public void Filter(TextMap textMap)
        {
            HashSet<string> headers = new HashSet<string>();
            foreach(TreeViewItem treeViewItem in treeView.Items)
            {
                if(treeViewItem.IsExpanded)
                {
                    headers.Add(treeViewItem.Header as string);
                }
            }

            treeView.Items.Clear();

            if (textMap == null)
            {
                return;
            }

            List<string> keys = textMap.Keys?.ToList();
            if (keys == null)
            {
                return;
            }

            keys.Sort();

            string keyword = textBox_Keyword.Text.Trim().ToLower();

            foreach (string key in keys)
            {
                if (!string.IsNullOrEmpty(keyword))
                {
                    if (!key.ToLower().Trim().Contains(keyword))
                    {
                        continue;
                    }
                }

                TreeViewItem treeViewItem_Key = new TreeViewItem() { Header = key };
                treeView.Items.Add(treeViewItem_Key);
                treeViewItem_Key.ContextMenuOpening += TreeViewItem_Key_ContextMenuOpening;
                treeViewItem_Key.MouseDoubleClick += TreeViewItem_Key_MouseDoubleClick;

                List<string> values = textMap.GetValues(key);
                if (values == null)
                {
                    continue;
                }

                foreach (string value in values)
                {
                    TreeViewItem treeViewItem_Value = new TreeViewItem() { Header = value };
                    treeViewItem_Key.Items.Add(treeViewItem_Value);

                    treeViewItem_Value.ContextMenuOpening += TreeViewItem_Value_ContextMenuOpening;
                    treeViewItem_Value.MouseDoubleClick += TreeViewItem_Value_MouseDoubleClick;
                }

                if(headers != null && headers.Contains(key))
                {
                    treeViewItem_Key.IsExpanded = true;
                }
            }
        }

        private void TreeViewItem_Key_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            TextBlock textBlock = e.OriginalSource as TextBlock;
            if(textBlock == null)
            {
                return;
            }

            TreeViewItem treeViewItem = sender as TreeViewItem;
            if (treeViewItem == null)
            {
                return;
            }

            string key_Old = treeViewItem.Header as string;
            if (key_Old == null)
            {
                return;
            }

            if(textBlock.Text != key_Old)
            {
                return;
            }

            string key_New = null;

            using (Windows.Forms.TextBoxForm<string> textBoxForm = new Windows.Forms.TextBoxForm<string>("Keyword", "Keyword:"))
            {
                textBoxForm.Value = key_Old;
                if (textBoxForm.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                {
                    return;
                }

                key_New = textBoxForm.Value;
            }

            if(string.IsNullOrEmpty(key_New))
            {
                return;
            }

            bool updated = textMap.UpdateKey(key_Old, key_New);
            if (updated)
            {
                SetTextMap(textMap);
            }


        }

        private void TreeViewItem_Value_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            TreeViewItem treeViewItem = sender as TreeViewItem;
            if (treeViewItem == null)
            {
                return;
            }

            string value_Old = treeViewItem.Header as string;
            if (value_Old == null)
            {
                return;
            }

            string key = (treeViewItem.Parent as TreeViewItem)?.Header as string;
            if (key == null)
            {
                return;
            }

            string value_New = null;

            using (Windows.Forms.TextBoxForm<string> textBoxForm = new Windows.Forms.TextBoxForm<string>("Value", "Value:"))
            {
                textBoxForm.Value = value_Old;
                if (textBoxForm.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                {
                    return;
                }

                value_New = textBoxForm.Value;
            }

            if (string.IsNullOrWhiteSpace(value_New) || value_New == value_Old)
            {
                return;
            }

            if (textMap == null)
            {
                textMap = Core.Create.TextMap(textBox_Name.Text);
            }

            if (textMap == null)
            {
                return;
            }

            bool updated = textMap.UpdateValue(key, value_Old, value_New);
            if (updated)
            {
                SetTextMap(textMap);
            }
        }

        private void TreeViewItem_Value_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            TreeViewItem treeViewItem = sender as TreeViewItem;
            if (treeViewItem == null)
            {
                return;
            }

            treeViewItem.ContextMenu = new ContextMenu();

            MenuItem menuItem = new MenuItem();
            menuItem.Name = "MenuItem_RemoveValue";
            menuItem.Header = "Remove";
            menuItem.Click += MenuItem_RemoveValue_Click;
            menuItem.Tag = treeViewItem;
            treeViewItem.ContextMenu.Items.Add(menuItem);

            treeViewItem.ContextMenu.IsOpen = true;

            e.Handled = true;
        }

        private void TreeViewItem_Key_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            TreeViewItem treeViewItem = sender as TreeViewItem;
            if (treeViewItem == null)
            {
                return;
            }

            treeViewItem.ContextMenu = new ContextMenu();

            MenuItem menuItem = null;

            menuItem = new MenuItem();
            menuItem.Name = "MenuItem_RemoveKey";
            menuItem.Header = "Remove";
            menuItem.Click += MenuItem_RemoveKey_Click;
            menuItem.Tag = treeViewItem;
            treeViewItem.ContextMenu.Items.Add(menuItem);

            menuItem = new MenuItem();
            menuItem.Name = "MenuItem_AddValue";
            menuItem.Header = "Add value";
            menuItem.Click += MenuItem_AddValue_Click;
            menuItem.Tag = treeViewItem;
            treeViewItem.ContextMenu.Items.Add(menuItem);

            treeViewItem.ContextMenu.IsOpen = true;

            e.Handled = true;
        }

        private void treeView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            treeView.ContextMenu = new ContextMenu();

            MenuItem menuItem = new MenuItem();
            menuItem.Name = "MenuItem_AddKey";
            menuItem.Header = "Add";
            menuItem.Click += MenuItem_AddKey_Click;
            treeView.ContextMenu.Items.Add(menuItem);

            treeView.ContextMenu.IsOpen = true;

            e.Handled = true;
        }

        private void MenuItem_AddKey_Click(object sender, RoutedEventArgs e)
        {
            string key = null;

            using (Windows.Forms.TextBoxForm<string> textBoxForm = new Windows.Forms.TextBoxForm<string>("Keyword", "Keyword:"))
            {
                if (textBoxForm.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                {
                    return;
                }

                key = textBoxForm.Value;
            }

            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            if (textMap == null)
            {
                textMap = Core.Create.TextMap(textBox_Name.Text);
            }

            if (textMap == null)
            {
                return;
            }

            bool added = textMap.AddKey(key);

            if (added)
            {
                SetTextMap(textMap);
            }
        }

        private void MenuItem_RemoveKey_Click(object sender, RoutedEventArgs e)
        {
            if (textMap == null)
            {
                return;
            }

            MenuItem menuItem = sender as MenuItem;
            if (menuItem == null)
            {
                return;
            }

            string key = (menuItem.Tag as TreeViewItem).Header as string;
            if (key == null)
            {
                return;
            }

            bool removed = textMap.RemoveKey(key);
            if (removed)
            {
                SetTextMap(textMap);
            }
        }

        private void MenuItem_RemoveValue_Click(object sender, RoutedEventArgs e)
        {
            if (textMap == null)
            {
                return;
            }

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

            string value = treeViewItem.Header as string;
            if (value == null)
            {
                return;
            }

            string key = (treeViewItem.Parent as TreeViewItem)?.Header as string;
            if (key == null)
            {
                return;
            }

            bool removed = textMap.RemoveValue(key, value);
            if (removed)
            {
                SetTextMap(textMap);
            }
        }

        private void MenuItem_AddValue_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            if (menuItem == null)
            {
                return;
            }

            string key = (menuItem.Tag as TreeViewItem).Header as string;
            if (key == null)
            {
                return;
            }

            string value = null;

            using (Windows.Forms.TextBoxForm<string> textBoxForm = new Windows.Forms.TextBoxForm<string>("Value", "Value:"))
            {
                if (textBoxForm.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                {
                    return;
                }

                value = textBoxForm.Value;
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            if (textMap == null)
            {
                textMap = Core.Create.TextMap(textBox_Name.Text);
            }

            if (textMap == null)
            {
                return;
            }

            bool added = textMap.Add(key, value) != null;

            if (added)
            {
                SetTextMap(textMap);
            }
        }

        private void textBox_Text_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            Test(textBox_Text.Text);
        }

        private void textBox_Keyword_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            Filter(textMap);
        }

        private void button_Open_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "json files (*.json)|*.json|All files (*.*)|*.*";
            openFileDialog.FilterIndex = 2;
            openFileDialog.RestoreDirectory = true;
            if (openFileDialog.ShowDialog() == false)
            {
                return;
            }

            string path = openFileDialog.FileName;
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }


            TextMap textMap = Core.Convert.ToSAM<TextMap>(path)?.FirstOrDefault();
            if (textMap == null)
            {
                MessageBox.Show("Could not load file.");
                return;
            }

            SetTextMap(textMap);
        }

        private void button_Save_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "json files (*.json)|*.json|All files (*.*)|*.*";
            saveFileDialog.FilterIndex = 1;
            saveFileDialog.RestoreDirectory = true;
            saveFileDialog.FileName = textBox_Name.Text;
            if (saveFileDialog.ShowDialog() == false)
            {
                return;
            }

            string path = saveFileDialog.FileName;
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            Core.Convert.ToFile(GetTextMap(), path);
        }

        private void button_Import_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "csv files (*.csv)|*.csv|Microsoft Excel files (*.xlsm, *.xlsx)|*.xlsm;*.xlsx|All files (*.*)|*.*";
            openFileDialog.FilterIndex = 1;
            openFileDialog.RestoreDirectory = true;
            if (openFileDialog.ShowDialog() == false)
            {
                return;
            }

            string path = openFileDialog.FileName;
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            List<DelimitedFileRow> delimitedFileRows = null;
            string extension = System.IO.Path.GetExtension(path);
            if (extension.ToLower().EndsWith("csv"))
            {
                delimitedFileRows = Core.Create.DelimitedFileRows(DelimitedFileType.Csv, path);

            }
            else if (extension.ToLower().EndsWith("xlsm") || extension.ToLower().EndsWith("xlsx"))
            {
                Dictionary<string, object[,]> dictionary = Excel.Query.Values(path);
                if (dictionary != null)
                {
                    object[,] values = null;
                    using (Windows.Forms.ComboBoxForm<string> comboBoxForm = new Windows.Forms.ComboBoxForm<string>("Select worksheet name", dictionary.Keys))
                    {
                        if (comboBoxForm.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                        {
                            return;
                        }

                        if (!dictionary.TryGetValue(comboBoxForm.SelectedItem, out values))
                        {
                            return;
                        }
                    }

                    delimitedFileRows = Core.Create.DelimitedFileRows(values);
                }
            }

            if (delimitedFileRows == null || delimitedFileRows.Count == 0)
            {
                return;
            }

            TextMap textMap = Core.Create.TextMap(textBox_Name.Text);
            foreach (DelimitedFileRow delimitedFileRow in delimitedFileRows)
            {
                if (delimitedFileRow == null || delimitedFileRow.Count == 0)
                {
                    continue;
                }

                string key = delimitedFileRow[0]?.Trim();
                delimitedFileRow.RemoveAt(0);

                textMap.Add(key, delimitedFileRow.ToList().ConvertAll(x => x?.Trim()).ToArray());
            }

            if (textMap == null)
            {
                return;
            }

            if (this.textMap != null)
            {
                if (MessageBox.Show("Do you want to append opened Text Map?", "Text Map", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    this.textMap.AddRange(textMap);
                    textMap = this.textMap;
                }
            }

            SetTextMap(textMap);
        }

        private void button_Export_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "csv files (*.csv)|*.csv|All files (*.*)|*.*";
            saveFileDialog.FilterIndex = 1;
            saveFileDialog.RestoreDirectory = true;
            saveFileDialog.FileName = textBox_Name.Text;
            if (saveFileDialog.ShowDialog() == false)
            {
                return;
            }

            string path = saveFileDialog.FileName;
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            TextMap textMap = GetTextMap();

            IEnumerable<string> keys = textMap?.Keys;
            if(keys == null)
            {
                return;
            }

            List<DelimitedFileRow> delimitedFileRows = new List<DelimitedFileRow>();
            foreach(string key in keys)
            {
                DelimitedFileRow delimitedFileRow = new DelimitedFileRow();
                delimitedFileRow.Add(key);

                List<string> values = textMap.GetValues(key);
                if(values != null)
                {
                    values.FindAll(x => x != null).ForEach(x => delimitedFileRow.Add(x));
                }

                delimitedFileRows.Add(delimitedFileRow);
            }

            Core.Convert.ToFile(delimitedFileRows, DelimitedFileType.Csv, path);
        }
    }
}
