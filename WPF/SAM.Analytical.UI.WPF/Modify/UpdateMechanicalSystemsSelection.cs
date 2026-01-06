// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        public static void UpdateMechanicalSystemsSelection(this MechanicalSystemsControl mechanicalSystemsControl, AdjacencyCluster adjacencyCluster, IEnumerable<Space> spaces)
        {
            if (spaces == null || spaces.Count() == 0 || mechanicalSystemsControl == null || adjacencyCluster == null)
            {
                return;
            }

            List<MechanicalSystem> mechanicalSystems = mechanicalSystemsControl.MechanicalSystems;
            if (mechanicalSystems == null || mechanicalSystems.Count == 0)
            {
                return;
            }

            foreach (Space space in spaces)
            {
                List<MechanicalSystem> mechanicalSystems_temp = adjacencyCluster.MechanicalSystems(space);
                if (mechanicalSystems_temp == null || mechanicalSystems_temp.Count == 0)
                {
                    mechanicalSystems = null;
                    break;
                }

                for (int i = mechanicalSystems.Count - 1; i >= 0; i--)
                {
                    if (mechanicalSystems_temp.Find(x => x.Guid == mechanicalSystems[i].Guid) == null)
                    {
                        mechanicalSystems.RemoveAt(i);
                    }
                }

                if (mechanicalSystems.Count == 0)
                {
                    break;
                }
            }

            mechanicalSystemsControl.SelectedMechanicalSystems = mechanicalSystems;
        }
    }
}
