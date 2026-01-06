// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;

namespace SAM.Core.UI.WPF
{
    public class TextChangedEventArgs : EventArgs
    {
        public string Text { get; }

        public TextChangedEventArgs(string text)
        {
            Text = text;
        }
    }
}
