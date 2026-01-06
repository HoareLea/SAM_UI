// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using Microsoft.Win32;
using SAM.Analytical.Tas;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for MultipleCaseSimulationControl.xaml
    /// </summary>
    public partial class MultipleCaseSimulationControl : UserControl
    {
        private string? directory = null;

        private WorkflowSettings workflowSettings = Query.DefaultWorkflowSettings();

        public MultipleCaseSimulationControl()
        {
            InitializeComponent();
        }

        public string? Directory
        {
            get
            {
                return directory;
            }
            set
            {
                directory = value;
                TextBox_Directory.Text = directory;

                //TextBox_Directory.Focus();
                //TextBox_Directory.CaretIndex = TextBox_Directory.Text.Length;
                //TextBox_Directory.ScrollToEnd();

            }
        }

        public bool Parallel
        {
            get
            {
                return checkBox_Parallel.IsChecked.HasValue && checkBox_Parallel.IsChecked.Value;
            }
            set
            {
                checkBox_Parallel.IsChecked = value;
            }
        }

        public List<string> Paths
        {
            get
            {
                if (ListBox_Main.Items == null)
                {
                    return null;
                }

                List<string> result = [];
                foreach (object @object in ListBox_Main.Items)
                {
                    if (@object is not string path || string.IsNullOrWhiteSpace(path))
                    {
                        continue;
                    }

                    result.Add(path);
                }

                return result;
            }

            set
            {
                ListBox_Main.Items.Clear();

                if (value == null || value.Count == 0)
                {
                    return;
                }

                foreach (string path in value)
                {
                    ListBox_Main.Items.Add(path);
                }

            }
        }

        public WorkflowSettings WorkflowSettings
        {
            get
            {
                if (workflowSettings is null)
                {
                    workflowSettings = Query.DefaultWorkflowSettings();
                }

                workflowSettings.AddIZAMs = checkBox_AddIZAMs.IsChecked.Value;
                workflowSettings.Sizing = checkBox_Sizing.IsChecked.Value;
                workflowSettings.Simulate = checkBox_Simulate.IsChecked.Value;
                workflowSettings.UseWidths = checkBox_UseBEThickness.IsChecked.Value;
                workflowSettings.UnmetHours = checkBox_RunUnmetHours.IsChecked.Value;
                workflowSettings.RemoveExistingTBD = checkBox_RemoveTBD.IsChecked.Value;

                return workflowSettings;
            }

            set
            {
                if (value == null)
                {
                    return;
                }

                workflowSettings = value;
                if (workflowSettings is not null)
                {
                    checkBox_AddIZAMs.IsChecked = workflowSettings.AddIZAMs;
                    checkBox_Sizing.IsChecked = WorkflowSettings.Sizing;
                    checkBox_Simulate.IsChecked = WorkflowSettings.Simulate;
                    checkBox_UseBEThickness.IsChecked = WorkflowSettings.UseWidths;
                    checkBox_RunUnmetHours.IsChecked = WorkflowSettings.UnmetHours;
                    checkBox_RemoveTBD.IsChecked = WorkflowSettings.RemoveExistingTBD;
                }
            }
        }

        private void button_Browse_Click(object sender, RoutedEventArgs e)
        {
            OpenFolderDialog openFolderDialog = new();
            bool? dialogResult = openFolderDialog.ShowDialog();
            if (dialogResult == null || !dialogResult.HasValue || !dialogResult.Value)
            {
                return;
            }

            directory = openFolderDialog.FolderName;
            TextBox_Directory.Text = directory;
        }

        private void button_Directory_Click(object sender, RoutedEventArgs e)
        {
            string directory = null;

            using (System.Windows.Forms.FolderBrowserDialog folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                folderBrowserDialog.Description = "Select Directory";
                folderBrowserDialog.ShowNewFolderButton = false;
                if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    directory = folderBrowserDialog.SelectedPath;
                }
            }

            if (string.IsNullOrWhiteSpace(directory))
            {
                return;
            }

            List<string> paths_AnalyticalModel = [];

            string[] paths = System.IO.Directory.GetFiles(directory, "*.json");
            foreach (string path in paths)
            {
                AnalyticalModel? analyticalModel = Core.Convert.ToSAM<AnalyticalModel>(path)?.FirstOrDefault();
                if (analyticalModel != null)
                {
                    paths_AnalyticalModel.Add(path);
                }
            }

            foreach (string path_AnalyticalModel in paths_AnalyticalModel)
            {
                ListBox_Main.Items.Add(path_AnalyticalModel);
            }
        }

        private void button_Files_Click(object sender, RoutedEventArgs e)
        {
            string path = null;
            using (System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog())
            {
                string directory = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SAM", "resources", "Analytical");
                if (System.IO.Directory.Exists(directory))
                {
                    openFileDialog.InitialDirectory = directory;
                }

                openFileDialog.Filter = "json files (*.json)|*.json|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;
                if (openFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                {
                    return;
                }
                path = openFileDialog.FileName;
            }

            if(string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            ListBox_Main.Items.Add(path);
        }
        
        private void button_Remove_Click(object sender, RoutedEventArgs e)
        {
            if(ListBox_Main.SelectionMode == SelectionMode.Single)
            {
                ListBox_Main.Items.Remove(ListBox_Main.SelectedItem);
            }
            else
            {
                if(ListBox_Main.SelectedItems is not null)
                {
                    if(ListBox_Main?.SelectedItems?.Cast<object>().ToList() is IEnumerable<object> items)
                    {
                        foreach (object item in items)
                        {
                            ListBox_Main.Items.Remove(item);
                        }
                    }
                }
                
            }


        }
    }
}
