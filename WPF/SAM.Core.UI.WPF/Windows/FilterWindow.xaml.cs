using System;
using System.Collections.Generic;
using System.Windows;

namespace SAM.Core.UI.WPF
{
    /// <summary>
    /// Interaction logic for FilterWindow.xaml
    /// </summary>
    public partial class FilterWindow : Window
    {
        public event FilterAddingEventHandler FilterAdding;

        public FilterWindow()
        {
            InitializeComponent();

            filterControl.FilterAdding += FilterControl_FilterAdding;
        }

        private void FilterControl_FilterAdding(object sender, FilterAddingEventArgs e)
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
    }
}
