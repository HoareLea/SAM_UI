using SAM.Analytical.Tas;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        public static void SimulateCases(this UIAnalyticalModel uIAnalyticalModel)
        {
            if (uIAnalyticalModel == null)
            {
                return;
            }

            CaseSimulationWindow caseSimulationWindow = new CaseSimulationWindow();
            caseSimulationWindow.WorkflowSettings = Query.DefaultWorkflowSettings();

            bool? dialogResult = caseSimulationWindow.ShowDialog();
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
            if(workflowSettings is null)
            {
                return;
            }

            bool parallel = caseSimulationWindow.Parallel;

            throw new System.NotImplementedException();

            //Analytical.Grasshopper.Tas.Modify.RunWorkflow(uIAnalyticalModel.JSAMObject, workflowSettings, directory, parallel);

        }
    }
}