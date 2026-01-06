// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Tas;
using SAM.Core.UI.WPF;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        public static void SimulateCases()
        {
            bool? dialogResult;

            MultipleCaseSimulationWindow multipleCaseSimulationWindow = new()
            {
                WorkflowSettings = Query.DefaultWorkflowSettings()
            };

            dialogResult = multipleCaseSimulationWindow.ShowDialog();
            if (dialogResult == null || !dialogResult.HasValue || !dialogResult.Value)
            {
                return;
            }

            bool parallel = multipleCaseSimulationWindow.Parallel;

            WorkflowSettings workflowSettings = multipleCaseSimulationWindow.WorkflowSettings;
            if(workflowSettings is null)
            {
                return;
            }

            List<string> paths = multipleCaseSimulationWindow.Paths;
            if(paths is null || paths.Count == 0)
            {
                return;
            }

            string? directory = multipleCaseSimulationWindow.Directory;
            if (string.IsNullOrEmpty(directory))
            {
                return;
            }

            List<AnalyticalModel> analyticalModels = [];
            foreach (string path in paths)
            {
                AnalyticalModel? analyticalModel = Core.Convert.ToSAM<AnalyticalModel>(path)?.FirstOrDefault();
                if (analyticalModel != null)
                {
                    analyticalModels.Add(analyticalModel);
                }
            }

            using ProgressBarWindowManager progressBarWindowManager = new();

            progressBarWindowManager.Show("Running", "Running...");
            Tas.Modify.RunWorkflow(analyticalModels, workflowSettings, directory, parallel, true);
            progressBarWindowManager.Close();
        }
    }
}
