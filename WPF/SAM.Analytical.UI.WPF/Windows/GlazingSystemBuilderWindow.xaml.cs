// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// The Glazing System Builder window (Stage E0-3): an owned, MODAL window over <see cref="GlazingBuilderViewModel"/>. It presents the
    /// view-model and holds no logic of its own; the model cannot change while it is open (it is modal, and the Builder has no model).
    /// <c>Save as predefined</c> closes it only after a successful Save; Cancel and the close button leave everything as it was.
    /// The window disposes the view-model (and with it the Builder's own Tas worker) when it closes.
    /// </summary>
    public partial class GlazingSystemBuilderWindow : System.Windows.Window
    {
        private readonly GlazingBuilderViewModel viewModel;

        public GlazingSystemBuilderWindow(GlazingBuilderViewModel viewModel)
        {
            InitializeComponent();
            this.viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            DataContext = viewModel;

            viewModel.Panes.PickSourceFile = viewModel.Panes.PickSourceFile ?? PickSourceFileWithDialog;
        }

        public GlazingBuilderViewModel ViewModel => viewModel;

        /// <summary>Opens the Builder as the owner's dialog; true when a system was saved.</summary>
        public static bool? ShowModal(GlazingBuilderViewModel viewModel, System.Windows.Window owner)
        {
            GlazingSystemBuilderWindow window = new GlazingSystemBuilderWindow(viewModel) { Owner = owner };
            return window.ShowDialog();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            // Not while the system is being written: the Save finishes first.
            e.Cancel = viewModel.IsSaving;
            base.OnClosing(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            viewModel.Dispose();
            base.OnClosed(e);
        }

        private static string PickSourceFileWithDialog()
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog()
            {
                Title = "Add a source of panes",
                Filter = "Glazing databases (*.tcd;*.json)|*.tcd;*.json|All files (*.*)|*.*",
                CheckFileExists = true,
            };

            return openFileDialog.ShowDialog() == true ? openFileDialog.FileName : null;
        }

        // A modal window reports its result; a window shown another way (a test) just closes.
        private void Finish(bool result)
        {
            try
            {
                DialogResult = result;
            }
            catch (InvalidOperationException)
            {
                Close();
            }
        }

        private async void button_Save_Click(object sender, RoutedEventArgs e)
        {
            if (await viewModel.SaveAsync())
            {
                Finish(true);
            }
        }

        private async void button_SaveReplace_Click(object sender, RoutedEventArgs e)
        {
            if (await viewModel.SaveAndReplaceAsync())
            {
                Finish(true);
            }
        }

        private void button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            if (!viewModel.IsSaving)
            {
                Finish(false);
            }
        }

        private void button_AddPane_Click(object sender, RoutedEventArgs e)
        {
            viewModel.AddPane();
        }

        private void button_ReplacePane_Click(object sender, RoutedEventArgs e)
        {
            viewModel.ReplacePane();
        }

        private void button_AddGap_Click(object sender, RoutedEventArgs e)
        {
            viewModel.AddGap();
        }

        private void button_Remove_Click(object sender, RoutedEventArgs e)
        {
            viewModel.RemoveSelected();
            FocusLayers();
        }

        private void button_MoveUp_Click(object sender, RoutedEventArgs e)
        {
            viewModel.MoveSelected(-1);
        }

        private void button_MoveDown_Click(object sender, RoutedEventArgs e)
        {
            viewModel.MoveSelected(1);
        }

        private void button_Reverse_Click(object sender, RoutedEventArgs e)
        {
            viewModel.ToggleReverse();
        }

        private void button_AddFrameLayer_Click(object sender, RoutedEventArgs e)
        {
            viewModel.AddFrameLayer();
        }

        private void button_ReplaceFrameMaterial_Click(object sender, RoutedEventArgs e)
        {
            viewModel.ReplaceFrameMaterial();
        }

        private void button_RemoveFrameLayer_Click(object sender, RoutedEventArgs e)
        {
            viewModel.RemoveFrameLayer();
        }

        private void button_FrameLayerUp_Click(object sender, RoutedEventArgs e)
        {
            viewModel.MoveFrameLayer(-1);
        }

        private void button_FrameLayerDown_Click(object sender, RoutedEventArgs e)
        {
            viewModel.MoveFrameLayer(1);
        }

        private void button_AddSource_Click(object sender, RoutedEventArgs e)
        {
            _ = viewModel.Panes.AddSourceAsync();
        }

        private void listView_Panes_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Only a double click on a pane (not on the header or the scroll bar) adds it.
            if (viewModel.Panes.SelectedEntry != null && e.OriginalSource is DependencyObject source && FindAncestor<ListViewItem>(source) != null)
            {
                viewModel.AddPane();
            }
        }

        private void listView_Panes_HeaderClick(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is GridViewColumnHeader header && header.Tag is string tag)
            {
                GlazingPaneSortColumn? column = null;
                switch (tag)
                {
                    case "Name": column = GlazingPaneSortColumn.Name; break;
                    case "Category": column = GlazingPaneSortColumn.Category; break;
                    case "Thickness": column = GlazingPaneSortColumn.Thickness; break;
                    case "Solar": column = GlazingPaneSortColumn.SolarTransmittance; break;
                    case "Light": column = GlazingPaneSortColumn.LightTransmittance; break;
                    case "Emissivity": column = GlazingPaneSortColumn.Emissivity; break;
                }

                if (column != null)
                {
                    viewModel.Panes.SortBy(column.Value);
                }
            }
        }

        private void listBox_Issues_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listBox_Issues.SelectedItem is GlazingBuilderIssueRow issue)
            {
                viewModel.SelectIssue(issue);
                viewModel.SelectFrameIssue(issue);
            }
        }

        // Alt+Up / Alt+Down move the selected layer (a keyboard way to reorder); Delete removes it. Typing in a gap's boxes is left alone.
        private void listBox_Layers_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = HandleLayerKey(e.Key == Key.System ? e.SystemKey : e.Key, Keyboard.Modifiers, e.OriginalSource);
        }

        /// <summary>The keys of the build-up list; true when the key was used. Typing in a gap's own boxes is never taken.</summary>
        internal bool HandleLayerKey(Key key, ModifierKeys modifiers, object source)
        {
            if (source is TextBox || source is ComboBox || FindAncestor<ComboBox>(source as DependencyObject) != null)
            {
                return false;
            }

            if ((modifiers & ModifierKeys.Alt) == ModifierKeys.Alt && (key == Key.Up || key == Key.Down))
            {
                if (viewModel.MoveSelected(key == Key.Up ? -1 : 1))
                {
                    FocusLayers();
                }

                return true;
            }

            if (key == Key.Delete && viewModel.CanRemove)
            {
                viewModel.RemoveSelected();
                FocusLayers();
                return true;
            }

            return false;
        }

        // The list is rebuilt after a structural change; keep the keyboard on the selected line.
        private void FocusLayers()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (viewModel.SelectedLayer != null && listBox_Layers.ItemContainerGenerator.ContainerFromItem(viewModel.SelectedLayer) is ListBoxItem item)
                {
                    item.Focus();
                }
                else
                {
                    listBox_Layers.Focus();
                }
            }), System.Windows.Threading.DispatcherPriority.Input);
        }

        private static T FindAncestor<T>(DependencyObject element) where T : DependencyObject
        {
            while (element != null)
            {
                if (element is T match)
                {
                    return match;
                }

                element = System.Windows.Media.VisualTreeHelper.GetParent(element) ?? (element as FrameworkContentElement)?.Parent;
            }

            return null;
        }
    }
}
