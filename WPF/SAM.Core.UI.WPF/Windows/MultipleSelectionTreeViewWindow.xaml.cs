// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Windows;

namespace SAM.Core.UI.WPF
{
    /// <summary>
    /// Interaction logic for MultipleSelectionTreeViewWindow.xaml
    /// </summary>
    public partial class MultipleSelectionTreeViewWindow : Window
    {
        public event GettingTextEventHandler GettingText;
        public event GettingCategoryEventHandler GettingCategory;

        public MultipleSelectionTreeViewWindow()
        {
            InitializeComponent();

            MultipleSelectionTreeViewControl_Main.GettingCategory += TreeViewControl_Main_GettingCategory;
            MultipleSelectionTreeViewControl_Main.GettingText += TreeViewControl_Main_GettingText;
        }

        private void TreeViewControl_Main_GettingText(object sender, GettingTextEventArgs e)
        {
            GettingText?.Invoke(this, e);
        }

        private void TreeViewControl_Main_GettingCategory(object sender, GettingCategoryEventArgs e)
        {
            GettingCategory?.Invoke(this, e);
        }

        public string UndefinedText
        {
            get
            {
                return MultipleSelectionTreeViewControl_Main.UndefinedText;
            }

            set
            {
                MultipleSelectionTreeViewControl_Main.UndefinedText = value;
            }
        }

        public List<T> GetObjects<T>()
        {
            return MultipleSelectionTreeViewControl_Main.GetObjects<T>();
        }

        public void SetObjects<T>(IEnumerable<T> objects)
        {
            MultipleSelectionTreeViewControl_Main.SetObjects(objects);
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
    }
}
