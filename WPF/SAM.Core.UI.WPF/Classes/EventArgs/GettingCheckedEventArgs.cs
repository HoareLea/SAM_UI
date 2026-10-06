// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;

namespace SAM.Core.UI.WPF
{
    /// <summary>
    /// Asks the consumer whether an object should start out ticked, when a
    /// <see cref="MultipleSelectionTreeViewControl"/> is populated.
    /// <para>
    /// The counterpart of the WinForms <c>TreeViewForm</c>'s <c>Func&lt;T, bool&gt; @checked</c>
    /// constructor argument. Handlers set <see cref="Checked"/>; leaving it alone means unticked,
    /// which is the behaviour of a control with no handler attached.
    /// </para>
    /// </summary>
    public class GettingCheckedEventArgs : EventArgs
    {
        private object @object;

        /// <summary>
        /// Whether <see cref="Object"/> should start out ticked. Defaults to false, so a consumer that
        /// only handles some objects leaves the rest unticked.
        /// </summary>
        public bool Checked { get; set; } = false;

        public GettingCheckedEventArgs(object @object)
        {
            this.@object = @object;
        }

        public object Object
        {
            get
            {
                return @object;
            }
        }
    }
}
