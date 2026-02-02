// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        public static void Solve(UIAnalyticalModel uIAnalyticalModel)
        {
            if(uIAnalyticalModel is null)
            {
                return;
            }

            SolverWindow solverWindow = new SolverWindow();
            bool? showDialogResult = solverWindow.ShowDialog();
            if(!(showDialogResult is not null && showDialogResult.Value))
            {
                return;
            }

            List<PanelType> panelTypes_Excluded = solverWindow.ExcludedPanelTypes;
            bool removePanelInternalEdges = solverWindow.RemovePanelInternalEdges;
            
            bool filterPanels = solverWindow.FilterPanels;
            double minArea = filterPanels ? solverWindow.MinArea : double.NaN;
            double minThinnessRatio = filterPanels ? solverWindow.MinThinnessRatio : double.NaN;
            double bucketSize_SingleLevel = solverWindow.BucketSize_SingleLevel;
            double bucketSize_MultipleLevel = solverWindow.BucketSize_MultipleLevel;
            double weight = solverWindow.Weight;
            double maxExtension = solverWindow.MaxExtension;
            double levelOffset = solverWindow.LevelOffset;

            List<Panel> panels = uIAnalyticalModel.JSAMObject.GetPanels().Where(panel => !panelTypes_Excluded.Contains(panel.PanelType)).ToList();
        }
    }
}
