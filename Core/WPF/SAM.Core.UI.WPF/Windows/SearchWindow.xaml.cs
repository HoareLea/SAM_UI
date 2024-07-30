using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SAM.Core.UI.WPF
{
    /// <summary>
    /// Interaction logic for SearchWindow.xaml
    /// </summary>
    public partial class SearchWindow : Window
    {
        public MouseEventHandler MouseDoubleClick
        {
            get
            {
                return SearchControl_Main.MouseDoubleClick;
            }

            set
            {
                SearchControl_Main.MouseDoubleClick = value;
            }
        }

        public EventHandler SelectedIndexChanged
        {
            get
            {
                return SearchControl_Main.SelectedIndexChanged;
            }

            set
            {
                SearchControl_Main.SelectedIndexChanged = value;
            }
        }

        public SearchWindow(IEnumerable<string> items, bool caseSensitive = false)
        {
            InitializeComponent();

            SearchControl_Main.SearchObjectWrapper = new SearchObjectWrapper(items, x => x?.ToString(), caseSensitive);

            MouseDoubleClick = SearchControl_Main.MouseDoubleClick;
            SelectedIndexChanged = SearchControl_Main.SelectedIndexChanged;
        }

        public SearchWindow(IEnumerable<object> items, Func<object, string> text, bool caseSensitive = false)
        {
            InitializeComponent();

            SearchControl_Main.SearchObjectWrapper = new SearchObjectWrapper(items, text, caseSensitive);

            MouseDoubleClick = SearchControl_Main.MouseDoubleClick;
            SelectedIndexChanged = SearchControl_Main.SelectedIndexChanged;
        }

        [Description("Search Text"), Category("Data")]
        public string SearchText
        {
            get
            {
                return SearchControl_Main.SearchText;
            }
            set
            {
                SearchControl_Main.SearchText = value;
            }
        }

        [Description("Selection Mode"), Category("Behaviour")]
        public SelectionMode SelectionMode
        {
            get
            {
                return SearchControl_Main.SelectionMode;
            }

            set
            {
                SearchControl_Main.SelectionMode = value;
            }
        }

        public List<T> GetSelectedItems<T>()
        {
            return SearchControl_Main.GetSelectedItems<T>();
        }
    }
}
