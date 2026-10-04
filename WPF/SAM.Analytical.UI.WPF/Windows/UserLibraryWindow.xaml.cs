// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Windows;
using System.Windows.Input;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// "My library" (PR2): an owned, MODAL window over <see cref="UserLibraryViewModel"/> listing the saved glazing systems with Rename, Remove
    /// (after a confirmation) and Open in Builder. It presents the view-model and holds no logic of its own; there is no analytical model behind it,
    /// so nothing it does can change a model or add an Undo step. The window disposes the view-model when it closes.
    /// </summary>
    public partial class UserLibraryWindow : System.Windows.Window
    {
        private readonly UserLibraryViewModel viewModel;

        public UserLibraryWindow(UserLibraryViewModel viewModel)
        {
            InitializeComponent();
            this.viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            DataContext = viewModel;
        }

        public UserLibraryViewModel ViewModel => viewModel;

        /// <summary>
        /// Asks the user to confirm a Remove (given the text to show); the default is a message box over this window. A test supplies its own.
        /// </summary>
        public Func<string, bool> Confirm { get; set; }

        /// <summary>Opens "My library" as the owner's dialog.</summary>
        public static void ShowModal(UserLibraryViewModel viewModel, System.Windows.Window owner)
        {
            UserLibraryWindow window = new UserLibraryWindow(viewModel) { Owner = owner };
            window.ShowDialog();
        }

        protected override void OnClosed(EventArgs e)
        {
            viewModel.Dispose();
            base.OnClosed(e);
        }

        private bool ConfirmWithMessageBox(string text)
        {
            return MessageBox.Show(this, text, "Remove from My library", MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.Cancel) == MessageBoxResult.OK;
        }

        private void button_Rename_Click(object sender, RoutedEventArgs e)
        {
            if (viewModel.BeginRename())
            {
                textBox_Rename.Focus();
                textBox_Rename.SelectAll();
            }
        }

        private void button_RenameOk_Click(object sender, RoutedEventArgs e)
        {
            viewModel.CommitRename();
        }

        private void button_RenameCancel_Click(object sender, RoutedEventArgs e)
        {
            viewModel.CancelRename();
        }

        private void textBox_Rename_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                viewModel.CommitRename();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                viewModel.CancelRename();
                listView_Systems.Focus();
                e.Handled = true;
            }
        }

        private void button_Remove_Click(object sender, RoutedEventArgs e)
        {
            viewModel.Remove(Confirm ?? ConfirmWithMessageBox);
        }

        private void button_OpenInBuilder_Click(object sender, RoutedEventArgs e)
        {
            viewModel.RequestOpenInBuilder();
        }

        private void listView_Systems_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F2 && viewModel.BeginRename())
            {
                textBox_Rename.Focus();
                textBox_Rename.SelectAll();
                e.Handled = true;
            }
            else if (e.Key == Key.Delete && viewModel.CanRemove)
            {
                viewModel.Remove(Confirm ?? ConfirmWithMessageBox);
                e.Handled = true;
            }
        }

        // A modal window reports its result; a window shown another way (a test) just closes.
        private void button_Close_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DialogResult = true;
            }
            catch (InvalidOperationException)
            {
                Close();
            }
        }
    }
}
