using SAM.Analytical.Classes;
using SAM.Analytical.Tas;
using System.Collections.Generic;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        public static void SimulateCases(this UIAnalyticalModel uIAnalyticalModel)
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

            List<AnalyticalModel> analyticalModels = UI.Create.AnalyticalModels(analyticalModel, cases);

            CaseSimulationWindow caseSimulationWindow = new()
            {
                WorkflowSettings = Query.DefaultWorkflowSettings()
            };

            dialogResult = caseSimulationWindow.ShowDialog();
            if (dialogResult == null || !dialogResult.HasValue || !dialogResult.Value)
            {
                return;
            }

            string? directory = caseSimulationWindow.Directory;
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

            Tas.Modify.RunWorkflow(analyticalModels, workflowSettings, directory, parallel);
        }
    }
}