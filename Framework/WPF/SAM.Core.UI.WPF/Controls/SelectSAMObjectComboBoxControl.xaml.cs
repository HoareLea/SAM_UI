using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Forms;

namespace SAM.Core.UI.WPF
{
    /// <summary>
    /// Interaction logic for SelectSAMObjectComboBoxControl.xaml
    /// </summary>
    public partial class SelectSAMObjectComboBoxControl : System.Windows.Controls.UserControl
    {
        public event SelectionChangedEventHandler SelectionChanged;

        private class Item
        {
            public string Text { get; set; }
            public IJSAMObject JSAMObject { get; set; }

            public Item(string text, IJSAMObject jSAMObject)
            {
                Text = text;
                JSAMObject = jSAMObject;
            }

            public override string ToString()
            {
                return Text == null ? string.Empty : Text;
            }
        }

        public string SelectText { get; set; } = "Select...";
        
        public SelectSAMObjectComboBoxControl()
        {
            InitializeComponent();
            RefreshComboBox();
        }

        public Func<IJSAMObject, bool> ValidateFunc { get; set; } = new Func<IJSAMObject, bool>(x => x is IJSAMObject);

        public Func<string, IJSAMObject> ReadFunc { get; set; } = new Func<string, IJSAMObject>(x =>
        {
            if(string.IsNullOrEmpty(x) || !System.IO.File.Exists(x))
            {
                return null;
            }

            List<IJSAMObject> ts = Core.Convert.ToSAM<IJSAMObject>(x);
            if (ts != null && ts.Count != 0)
            {
                return ts[0];
            }

            return null;
        });

        public string DialogFilter { get; set; } = "json files (*.json)|*.json|All files (*.*)|*.*";

        public int DialogFilterIndex { get; set; } = 2;

        public int Add(string text, IJSAMObject jSAMObject)
        {
            if(text == null)
            {
                return -1;
            }

            for(int i = 0; i < comboBox.Items.Count; i++)
            {
                if(!(comboBox.Items[i] is Item))
                {
                    continue;
                }

                if(((Item)comboBox.Items[i]).Text == text)
                {
                    comboBox.Items[i] = new Item(text, jSAMObject);
                    return i;
                }
            }

            Item item = new Item(text, jSAMObject);
            comboBox.Items.Add(item);
            RefreshComboBox();

            return comboBox.Items.IndexOf(item);
        }

        public int Add(SAMObject sAMObject)
        {
            return Add(sAMObject?.Name, sAMObject);
        }

        public string SelectedText
        {
            get
            {
                if(comboBox.SelectedItem == null)
                {
                    return null;
                }

                if(comboBox.SelectedItem is string)
                {
                    return (string)comboBox.SelectedItem;
                }

                return comboBox.SelectedItem.ToString();
            }
            set
            {
                foreach(object @object in comboBox.Items)
                {
                    if(@object.ToString() == value)
                    {
                        comboBox.SelectedItem = @object;
                        return;
                    }
                }
            }
        }

        private void RefreshComboBox()
        {
            for(int i = comboBox.Items.Count - 1; i >=0 ; i--)
            {
                if(comboBox.Items[i] is string)
                {
                    comboBox.Items.RemoveAt(i);
                }
            }

            comboBox.Items.Add(SelectText);
        }

        public T GetJSAMObject<T>() where T : IJSAMObject
        {
            Item item = comboBox?.SelectedItem as Item;
            if(item == null)
            {
                return default(T);
            }

            if(!(item.JSAMObject is T))
            {
                return default(T);
            }

            return (T)item.JSAMObject;
        }

        public T GetJSAMObject<T>(string text) where T : IJSAMObject
        {
            Item item = null;
            foreach (object @object in comboBox.Items)
            {
                if (@object is Item)
                {
                    if (((Item)@object).Text == text)
                    {
                        item = (Item)@object;
                        break;
                    }
                }
            }

            if (item == null)
            {
                return default(T);
            }

            if (!(item.JSAMObject is T))
            {
                return default(T);
            }

            return (T)item.JSAMObject;
        }

        private void comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(comboBox.SelectedItem is string))
            {
                return;
            }

            string path = null;
            IJSAMObject jSAMObject = null;
            while(jSAMObject == null)
            {
                using (OpenFileDialog openFileDialog = new OpenFileDialog())
                {
                    openFileDialog.Filter = DialogFilter;
                    openFileDialog.FilterIndex = DialogFilterIndex;
                    openFileDialog.RestoreDirectory = true;
                    if (openFileDialog.ShowDialog() != DialogResult.OK)
                    {
                        Item item = null;
                        foreach (object @object in comboBox.Items)
                        {
                            if (@object is Item)
                            {
                                if (((Item)@object).Text == comboBox.Text)
                                {
                                    item = (Item)@object;
                                    break;
                                }
                            }
                        }

                        comboBox.SelectedItem = item;
                        return;
                    }

                    path = openFileDialog.FileName;
                }

                jSAMObject = ReadFunc?.Invoke(path);

                if(ValidateFunc != null)
                {
                    if(!ValidateFunc.Invoke(jSAMObject))
                    {
                        MessageBox.Show("Invalid object type. Select different file");
                        jSAMObject = null;
                    }
                }
            }

            int index = Add(path, jSAMObject);
            if(index == -1)
            {
                Item item = null;
                foreach (object @object in comboBox.Items)
                {
                    if (@object is Item)
                    {
                        if (((Item)@object).Text == comboBox.Text)
                        {
                            item = (Item)@object;
                            break;
                        }
                    }
                }

                comboBox.SelectedItem = item;
                return;
            }

            comboBox.SelectionChanged -= comboBox_SelectionChanged;
            comboBox.SelectedItem = comboBox.Items[index];
            comboBox.SelectionChanged += comboBox_SelectionChanged;

            SelectionChanged?.Invoke(this, e);
        }
    }
}
