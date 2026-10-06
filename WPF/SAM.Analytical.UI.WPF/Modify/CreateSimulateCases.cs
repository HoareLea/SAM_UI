// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Classes;
using SAM.Analytical.Tas;
using System.Collections.Generic;
using System.Threading;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        public static void CreateSimulateCases(this UIAnalyticalModel uIAnalyticalModel)
        {
            if (uIAnalyticalModel?.JSAMObject is not AnalyticalModel analyticalModel)
            {
                return;
            }

            bool? dialogResult;

            CreateCasesWindow createCasesWindow = new()
            {
                AnalyticalModel = analyticalModel
            };

            dialogResult = createCasesWindow.ShowDialog();
            if (dialogResult == null || !dialogResult.HasValue || !dialogResult.Value)
            {
                return;
            }

            List<Cases> cases = createCasesWindow.Cases;
            if(cases is null)
            {
                return;
            }

            string? directory = uIAnalyticalModel.Path != null ? System.IO.Path.GetDirectoryName(uIAnalyticalModel.Path) : null;

            List<AnalyticalModel> analyticalModels = UI.Create.AnalyticalModels(analyticalModel, cases);

            CaseSimulationWindow caseSimulationWindow = new()
            {
                WorkflowSettings = Query.DefaultWorkflowSettings()
            };

            if(!string.IsNullOrWhiteSpace(directory))
            {
                caseSimulationWindow.Directory = System.IO.Path.Combine(directory, "cases");
            }

            dialogResult = caseSimulationWindow.ShowDialog();
            if (dialogResult == null || !dialogResult.HasValue || !dialogResult.Value)
            {
                return;
            }

            directory = caseSimulationWindow.Directory;
            if (string.IsNullOrEmpty(directory))
            {
                return;
            }

            WorkflowSettings workflowSettings = caseSimulationWindow.WorkflowSettings;
            if (workflowSettings is null)
            {
                return;
            }

            bool parallel = caseSimulationWindow.Parallel;

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
