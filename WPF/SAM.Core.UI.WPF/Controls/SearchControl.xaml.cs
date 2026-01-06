using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Input;

namespace SAM.Core.UI.WPF
{
    /// <summary>
    /// Interaction logic for SearchControl.xaml
    /// </summary>
    public partial class SearchControl : UserControl
    {
        private SearchObjectWrapper searchObjectWrapper;

        public new MouseEventHandler MouseDoubleClick;

        public EventHandler SelectedIndexChanged;

        public SearchControl()
        {
            InitializeComponent();
        }

        public SearchControl(IEnumerable<object> items, Func<object, string> text, bool caseSensitive = false)
        {
            InitializeComponent();

            searchObjectWrapper = new SearchObjectWrapper(items, text, caseSensitive);

            Search();
        }

        public SearchObjectWrapper SearchObjectWrapper
        {
            get
            {
                return searchObjectWrapper;
            }
            set
            {
                searchObjectWrapper = value;
                Search();
            }
        }

        private void Search()
        {

            List<string> selectedTexts = null;
            if (ListBox_Main.SelectedItems != null && ListBox_Main.SelectedItems.Count != 0)
            {
                selectedTexts = new List<string>();
                foreach (string selectedText in ListBox_Main.SelectedItems)
                {
                    selectedTexts.Add(selectedText);
                }
            }

            ListBox_Main.Items.Clear();

            if (searchObjectWrapper == null)
            {
                return;
            }

            List<string> texts = null;
            if (string.IsNullOrEmpty(TextBox_Text.Text) || TextBox_Text.Text.Length < 3)
            {
                IEnumerable<string> texts_Temp = searchObjectWrapper.Texts;
                if (texts_Temp != null)
                {
                    texts = new List<string>(texts_Temp);
                }
            }
            else
            {
                texts = searchObjectWrapper.SearchTexts(TextBox_Text.Text, true);
            }

            if (texts == null || texts.Count == 0)
            {
                return;
            }

            foreach (string text in texts)
            {
                ListBox_Main.Items.Add(text);
            }

            if (selectedTexts != null && selectedTexts.Count != 0)
            {
                if(selectedTexts.Count == 1)
                {
                    ListBox_Main.SelectedItem = selectedTexts[0];
                }
                else
                {
                    foreach (string selectedText in selectedTexts)
                    {
                        ListBox_Main.SelectedItems.Add(selectedText);
                    }
                }
            }
        }

        [Description("Search Text"), Category("Data")]
        public string SearchText
        {
            get
            {
                return TextBox_Text.Text;
            }
            set
            {
                TextBox_Text.Text = value;
                TextBox_Text.SelectAll();
            }
        }

        [Description("Selection Mode"), Category("Behaviour")]
        public SelectionMode SelectionMode
        {
            get
            {
                return ListBox_Main.SelectionMode;
            }

            set
            {
                ListBox_Main.SelectionMode = value;
            }
        }

        public List<object> SelectedItems
        {
            get
            {
                if (ListBox_Main.SelectedItems == null)
                {
                    return null;
                }

                List<object> result = new List<object>();
                foreach (string text in ListBox_Main.SelectedItems)
                {
                    object item = searchObjectWrapper.GetItem<object>(text);
                    if (item != null)
                    {
                        result.Add(item);
                    }
                }

                return result;
            }
        }

        public List<T> GetSelectedItems<T>()
        {
            List<object> selectedItems = SelectedItems;
            if (selectedItems == null)
            {
                return null;
            }

            return selectedItems.FindAll(x => x is T).ConvertAll(x => (T)x);
        }

        private void ListBox_Main_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            MouseEventHandler mouseEventHandler = MouseDoubleClick;
            if (mouseEventHandler != null)
            {
                mouseEventHandler(this, e);
            }
        }

        private void ListBox_Main_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            EventHandler eventHandler = SelectedIndexChanged;
            if (eventHandler != null)
            {
                eventHandler(this, e);
            }
        }

        private void TextBox_Text_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            Search();
        }

        public new void Focus()
        {
            TextBox_Text.Focus();
        }
    }
}
