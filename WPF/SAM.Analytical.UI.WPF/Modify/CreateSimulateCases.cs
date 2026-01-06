// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Classes;
using SAM.Analytical.Tas;
using SAM.Core.UI.WPF;
using System.Collections.Generic;

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

            using (ProgressBarWindowManager progressBarWindowManager = new ProgressBarWindowManager())
            {
                progressBarWindowManager.Show("Running", "Running...");
                Tas.Modify.RunWorkflow(analyticalModels, workflowSettings, directory, parallel, true);
                progressBarWindowManager.Close();
            }
        }
    }
}
