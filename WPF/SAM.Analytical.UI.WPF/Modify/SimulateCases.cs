// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Tas;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

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

            // RunWorkflow shows its own cancellable, determinate dialog. The marquee window that used to wrap
            // this call stacked a second dialog on top of that one and reported nothing the inner one did not.
            Modify.RunWorkflow(analyticalModels, workflowSettings, directory, CancellationToken.None, out bool cancelled, parallel, null, true);

            if (cancelled)
            {
                System.Windows.MessageBox.Show("Simulation cancelled. Cases that had already finished were kept.");
            }
        }
    }
}
