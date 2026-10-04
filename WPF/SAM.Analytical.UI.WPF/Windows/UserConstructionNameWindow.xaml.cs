// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Windows;
using System.Windows.Controls;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Asks for the name of a construction saved to "My constructions" (the Thermal Performance panel and the classic Constructions editor share it).
    /// The library's own naming rule is shown as you type and Save is enabled only for a name it accepts. The window knows nothing else: no model, no
    /// library - it returns the name, and the caller saves.
    /// </summary>
    public partial class UserConstructionNameWindow : System.Windows.Window
    {
        private readonly Func<string, string> problem;

        public UserConstructionNameWindow(string description, string suggestedName, Func<string, string> problem)
        {
            InitializeComponent();
            this.problem = problem;
            textBlock_Description.Text = string.IsNullOrWhiteSpace(description) ? "Save this construction." : "Save " + description + ".";
            textBox_Name.Text = suggestedName ?? string.Empty;
            textBox_Name.SelectAll();
            Loaded += (sender, e) => textBox_Name.Focus();
            Check();
        }

        /// <summary>The name typed (trimmed) once the window was closed with Save; null otherwise.</summary>
        public string ConstructionName { get; private set; }

        /// <summary>Shows the window over <paramref name="owner"/> and returns the name chosen, or null when cancelled.</summary>
        public static string Prompt(System.Windows.Window owner, string description, string suggestedName, Func<string, string> problem)
        {
            UserConstructionNameWindow window = new UserConstructionNameWindow(description, suggestedName, problem) { Owner = owner };
            return window.ShowDialog() == true ? window.ConstructionName : null;
        }

        private void textBox_Name_TextChanged(object sender, TextChangedEventArgs e)
        {
            Check();
        }

        private void Check()
        {
            if (button_Save == null || textBox_Name == null)
            {
                return;
            }

            string text = problem?.Invoke(textBox_Name.Text) ?? (string.IsNullOrWhiteSpace(textBox_Name.Text) ? "The construction needs a name." : null);
            textBlock_Problem.Text = text ?? string.Empty;
            button_Save.IsEnabled = text == null;
        }

        private void button_Save_Click(object sender, RoutedEventArgs e)
        {
            if (!button_Save.IsEnabled)
            {
                return;
            }

            ConstructionName = textBox_Name.Text.Trim();
            DialogResult = true;
        }

        private void button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
