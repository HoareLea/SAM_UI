// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Windows.Controls;

namespace SAM.Core.UI.WPF
{
    /// <summary>
    /// Interaction logic for JsonControl.xaml
    /// </summary>
    public partial class JsonControl : UserControl
    {
        private System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();

        public event TextChangedEventHandler TextChanged;

        public int TextChangedInterval
        {
            get
            {
                return timer.Interval;
            }

            set
            {
                timer.Interval = value;
            }
        }

        public JsonControl()
        {
            InitializeComponent();

            timer.Interval = 3000;
            timer.Tick += Timer_Tick;
            text.TextChanged += Text_TextChanged;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            timer.Enabled = false;
            timer.Stop();

            TextChanged?.Invoke(this, new TextChangedEventArgs(text.Text));
        }

        private void Text_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {

            timer.Enabled = true;
            timer.Start();
        }

        public string Text
        {
            get
            {
                return text.Text;
            }

            set
            {
                text.Text = value;
                TextChanged?.Invoke(this, new TextChangedEventArgs(text.Text));
            }
        }
    }
}
