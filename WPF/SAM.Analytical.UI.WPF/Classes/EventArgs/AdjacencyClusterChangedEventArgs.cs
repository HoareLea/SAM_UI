// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;

namespace SAM.Analytical.UI.WPF
{
    public class AdjacencyClusterChangedEventArgs : EventArgs
    {
        private AdjacencyCluster adjacencyCluster;

        public AdjacencyClusterChangedEventArgs(AdjacencyCluster adjacencyCluster)
        {
            this.adjacencyCluster = adjacencyCluster;
        }

        public AdjacencyCluster AdjacencyCluster
        {
            get
            {
                return adjacencyCluster;
            }
        }
    }
}
