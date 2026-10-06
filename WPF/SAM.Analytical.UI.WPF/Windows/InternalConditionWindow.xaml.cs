// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// WPF replacement for the WinForms SAM.Analytical.Windows.Forms.InternalConditionForm: a modal
    /// wrapper around the existing <see cref="InternalConditionControl"/> for editing a single
    /// InternalCondition (or the InternalCondition of a single Space). Mirrors the original public
    /// surface (the (AnalyticalModel, InternalCondition) / (AnalyticalModel, Space) constructors and
    /// the read-only InternalCondition / Space getters). The control needs the AnalyticalModel for
    /// its profile-library / adjacency look-ups, so it is supplied rather than a bare ProfileLibrary.
    /// </summary>
    public partial class InternalConditionWindow : System.Windows.Window
    {
        public InternalConditionWindow()
        {
            InitializeComponent();

            // The single-object editor is modal OK/Cancel; the bulk-editor Apply button is not used here.
            button_Apply.Visibility = Visibility.Collapsed;
        }

        public InternalConditionWindow(AnalyticalModel analyticalModel, InternalCondition internalCondition)
            : this()
        {
            internalConditionControl.AnalyticalModel = analyticalModel;
            internalConditionControl.InternalConditions = new List<InternalCondition> { internalCondition };
        }

        public InternalConditionWindow(AnalyticalModel analyticalModel, Space space)
            : this()
        {
            internalConditionControl.AnalyticalModel = analyticalModel;
            internalConditionControl.Spaces = new List<Space> { space };
        }

        public InternalCondition InternalCondition
        {
            get
            {
                return internalConditionControl.InternalConditionDatas?.FirstOrDefault()?.InternalCondition;
            }
        }

        public Space Space
        {
            get
            {
                return internalConditionControl.InternalConditionDatas?.FirstOrDefault()?.Space;
            }
        }

        // internalConditionControl.AnalyticalModel is reassigned internally whenever the user edits a
        // profile or picks from the internal-condition library (see InternalConditionControl's
        // SetProfile / button_Select_Click), so these reflect any mid-edit change - not just what was
        // passed into the constructor. Mirrors the WinForms InternalConditionForm's ProfileLibrary /
        // AdjacencyCluster getters, which read the same way off its control.
        public ProfileLibrary ProfileLibrary
        {
            get
            {
                return internalConditionControl.AnalyticalModel?.ProfileLibrary;
            }
        }

        public AdjacencyCluster AdjacencyCluster
        {
            get
            {
                return internalConditionControl.AnalyticalModel?.AdjacencyCluster;
            }
        }

        private void button_OK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void button_Apply_Click(object sender, RoutedEventArgs e)
        {
        }

        private void button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
