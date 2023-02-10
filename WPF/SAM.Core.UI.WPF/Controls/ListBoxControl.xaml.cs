using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SAM.Core.UI.WPF
{
    /// <summary>
    /// Interaction logic for ListBoxControl.xaml
    /// </summary>
    public partial class ListBoxControl : UserControl
    {
        public event SelectionChangedEventHandler SelectionChanged;

        public ListBoxControl()
        {
            InitializeComponent();
        }

        public void SetValues<T>(IEnumerable<T> values, Func<T, string> namesFunc = null)
        {
            listBox.Items.Clear();

            if(values == null)
            {
                return;
            }

            foreach(T value in values)
            {
                string text = namesFunc == null ? null : namesFunc.Invoke(value);
                listBox.Items.Add(new ListBoxItem() { Content = text, Tag = value});
            }
        }

        public void UpdateValue<T>(T value, Func<T, string> uniqueIdFunc)
        {
            if(value == null || uniqueIdFunc == null)
            {
                return;
            }

            string uniqueId = uniqueIdFunc(value);

            foreach (ListBoxItem listBoxItem in listBox.Items)
            {
                if (!(listBoxItem.Tag is T))
                {
                    continue;
                }

                T value_Old = (T)listBoxItem.Tag;

                if (uniqueId != uniqueIdFunc(value_Old))
                {
                    continue;
                }

                listBoxItem.Tag = value;
            }
        }

        public void UpdateValues<T>(IEnumerable<T> values, Func<T, string> uniqueIdFunc)
        {
            if(values == null || uniqueIdFunc == null)
            {
                return;
            }

            foreach(T value in values)
            {
                UpdateValue<T>(value, uniqueIdFunc);
            }
        }

        public List<T> GetValues<T>(bool selected = true)
        {
            System.Collections.IList list = selected ? listBox.SelectedItems : listBox.Items;

            List<T> result = new List<T>();
            foreach(ListBoxItem listBoxItem in list)
            {
                object @object = listBoxItem?.Tag;
                if(@object is T)
                {
                    result.Add((T)@object);
                }
            }

            return result;
        }

        private void Button_SelectNone_Click(object sender, RoutedEventArgs e)
        {
            SelectNone();
        }

        private void Button_SelectAll_Click(object sender, RoutedEventArgs e)
        {
            SelectAll();
        }

        public void SelectAll()
        {
            listBox.SelectionChanged -= listBox_SelectionChanged;
            for(int i =0; i < listBox.Items.Count - 1; i++)
            {
                listBox.SelectedItems.Add(listBox.Items[i]);
            }
            listBox.SelectionChanged += listBox_SelectionChanged;

            if(listBox.Items.Count != 0)
            {
                listBox.SelectedItems.Add(listBox.Items[listBox.Items.Count - 1]);
            }
        }

        public void SelectNone()
        {
            listBox.SelectedItems.Clear();
        }

        private void listBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectionChanged.Invoke(this, e);
        }

        public SelectionMode SelectionMode
        {
            get
            {
                return listBox.SelectionMode;
            }

            set
            {
                listBox.SelectionMode = value;
            }
        }
    }
}
