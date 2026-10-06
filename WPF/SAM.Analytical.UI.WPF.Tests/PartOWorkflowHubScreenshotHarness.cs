// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.IO;
using System.Linq;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// <b>Opt-in.</b> Renders the Prepare &amp; Run Hub in its two Iteration 1a states to PNG, for a before /
    /// after record of a presentation change: a FRESH model (no run), and the same project AFTER its
    /// Iteration 1a run, reopened from the saved run. No TAS is run and nothing is written beside the model.
    /// <para>
    /// <c>SAM_PARTO_HUB_FRESH</c> is the unprepared source <c>.sam</c>; <c>SAM_PARTO_HUB_RUN</c> is the folder
    /// holding a saved Iteration 1a run (<c>&lt;run&gt;.sam</c> + <c>.partorun.json</c>);
    /// <c>SAM_PARTO_SCREENSHOTS</c> is where the images go. Without them this passes having done nothing.
    /// </para>
    /// </summary>
    [Collection(WpfCollection.Name)]
    public class PartOWorkflowHubScreenshotHarness
    {
        [WpfFact]
        public void Render_the_hub_for_a_fresh_and_a_completed_iteration_1a()
        {
            string path_Fresh = Environment.GetEnvironmentVariable("SAM_PARTO_HUB_FRESH");
            string directory_Run = Environment.GetEnvironmentVariable("SAM_PARTO_HUB_RUN");
            string directory_Screenshots = Environment.GetEnvironmentVariable("SAM_PARTO_SCREENSHOTS");

            if (string.IsNullOrWhiteSpace(directory_Screenshots))
            {
                return;
            }

            Directory.CreateDirectory(directory_Screenshots);

            VentilationUnitCatalogue ventilationUnitCatalogue = VentilationUnitCatalogue.Read();

            if (!string.IsNullOrWhiteSpace(path_Fresh) && File.Exists(path_Fresh))
            {
                AnalyticalModel analyticalModel = Core.Convert.ToSAM<AnalyticalModel>(path_Fresh).First();

                PartORun partORun = new();

                PartOWorkflowCapabilities partOWorkflowCapabilities = Modify.Capabilities(partORun, out PartOIteration3Eligibility partOIteration3Eligibility);

                PartOWorkflowWindow partOWorkflowWindow = new()
                {
                    AnalyticalModel = analyticalModel,
                    PartORun = partORun,
                    VentilationUnitCatalogue = ventilationUnitCatalogue,
                    Capabilities = partOWorkflowCapabilities,
                    Iteration3Eligibility = partOIteration3Eligibility,
                    SimulationCase = PartOSimulationCase.Create(analyticalModel, path_Fresh, null),
                };

                partOWorkflowWindow.CompleteInitialisation();

                Render(partOWorkflowWindow, Path.Combine(directory_Screenshots, "hub-1a-fresh.png"));

                //The same state again with every row's complete explanation disclosed. Rendered only where
                //the window has the switch - the record is taken before and after a presentation change.
                PartOWorkflowWindow partOWorkflowWindow_Details = new()
                {
                    AnalyticalModel = analyticalModel,
                    PartORun = partORun,
                    VentilationUnitCatalogue = ventilationUnitCatalogue,
                    Capabilities = partOWorkflowCapabilities,
                    Iteration3Eligibility = partOIteration3Eligibility,
                    SimulationCase = PartOSimulationCase.Create(analyticalModel, path_Fresh, null),
                };

                partOWorkflowWindow_Details.CompleteInitialisation();

                System.Reflection.PropertyInfo propertyInfo = typeof(PartOWorkflowWindow).GetProperty("ShowStatusDetails");
                if (propertyInfo is not null)
                {
                    propertyInfo.SetValue(partOWorkflowWindow_Details, true);

                    Render(partOWorkflowWindow_Details, Path.Combine(directory_Screenshots, "hub-1a-fresh-details.png"));
                }
                else
                {
                    partOWorkflowWindow_Details.Close();
                }
            }

            if (!string.IsNullOrWhiteSpace(directory_Run) && Directory.Exists(directory_Run))
            {
                string path_Model = Directory.GetFiles(directory_Run, "*.partorun.json").Select(x => x.Substring(0, x.Length - ".partorun.json".Length) + ".sam").Single(File.Exists);

                AnalyticalModel analyticalModel = Core.Convert.ToSAM<AnalyticalModel>(path_Model).First();

                PartORun partORun = new();
                Assert.True(partORun.Restore(analyticalModel, path_Model, out string refusal_Restore), refusal_Restore);

                PartOWorkflowCapabilities partOWorkflowCapabilities = Modify.Capabilities(partORun, out PartOIteration3Eligibility partOIteration3Eligibility);

                PartOWorkflowWindow partOWorkflowWindow = new()
                {
                    AnalyticalModel = analyticalModel,
                    PartORun = partORun,
                    VentilationUnitCatalogue = ventilationUnitCatalogue,
                    Capabilities = partOWorkflowCapabilities,
                    Iteration3Eligibility = partOIteration3Eligibility,
                    SimulationCase = PartOSimulationCase.Create(analyticalModel, path_Model, null),
                    LastOutcome = new PartOWorkflowOutcome(PartOWorkflowOutcomeKind.Success, "✓ Iteration 1a — MVHR design duty (no manufacturer unit) complete · TAS simulation 53s · TM59 Fail"),
                };

                partOWorkflowWindow.CompleteInitialisation();

                Render(partOWorkflowWindow, Path.Combine(directory_Screenshots, "hub-1a-after-run.png"));
            }
        }

        /// <summary>Full content height, so nothing is hidden behind the scroller in the record.</summary>
        private static void Render(PartOWorkflowWindow partOWorkflowWindow, string path)
        {
            partOWorkflowWindow.MaxHeight = double.PositiveInfinity;

            PartOWorkflowEvidenceHarness.Render(partOWorkflowWindow, path, 800, 0);

            partOWorkflowWindow.Close();
        }
    }
}
