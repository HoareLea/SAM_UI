// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Windows.Controls;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// A compact horizontal workflow strip - <c>Configure → Prepare model → Check &amp; simulate → Review</c> -
    /// for any Part O window. It shows the steps it is given and decides nothing: the host derives them
    /// (the Hub from <see cref="PartOWorkflowProgress"/>).
    /// </summary>
    public partial class PartOWorkflowStepStripControl : UserControl
    {
        public PartOWorkflowStepStripControl()
        {
            InitializeComponent();
        }

        /// <summary>The steps, in order. Null clears the strip.</summary>
        public IReadOnlyList<PartOWorkflowStep>? Steps
        {
            get => itemsControl_Steps.ItemsSource as IReadOnlyList<PartOWorkflowStep>;
            set => itemsControl_Steps.ItemsSource = value;
        }
    }
}
