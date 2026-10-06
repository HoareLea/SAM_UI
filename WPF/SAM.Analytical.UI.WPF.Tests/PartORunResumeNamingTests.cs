// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using SAM.Analytical.UI;
using SAM.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// A reopened run is named as the run it was, not as its resumed context happens to look.
    /// <para>
    /// A resumed <see cref="PartOPreparationContext"/> carries no capacity descriptors - they are Iteration 2B's
    /// capability lookup, and 2B never starts from a restored run - so <c>HasVentilationUnitCatalogue</c> is
    /// false on every resumed run. Naming read that flag, so a reopened <b>Iteration 2</b> run was called
    /// "Iteration 1a" by the TM59 window and by Iteration 3's reference case. The sidecar now records whether a
    /// catalogue was offered (v2); a v1 sidecar cannot say, and the run is then not named rather than guessed.
    /// </para>
    /// <para>
    /// End to end through the production path: prepare, announce and write the results, complete,
    /// <c>Modify.PersistPartORunResume</c>, then <see cref="PartORun.Restore"/> in a fresh run.
    /// </para>
    /// </summary>
    public class PartORunResumeNamingTests
    {
        [Theory]
        [InlineData(true, PartOWorkflowScenario.Text_Iteration2, "Iteration 2 — MVHR with manufacturer unit")]
        [InlineData(false, PartOWorkflowScenario.Text_Iteration1a, "Iteration 1a — baseline")]
        public void AReopenedRun_IsNamedAsTheRunItWas(bool catalogue, string scenario, string iteration3Reference)
        {
            using Saved saved = Save(catalogue);

            PartORun partORun = saved.Reopen();

            Assert.True(partORun.IsRestored);
            Assert.True(partORun.CanResumeIteration3, partORun.ResumeRefusal);
            Assert.True(partORun.PreparationContext.IsResumed);

            //Naming follows the record...
            Assert.Equal(catalogue, partORun.PreparationContext.VentilationUnitCatalogueOffered);
            Assert.Equal(scenario, PartOWorkflowScenario.Find(partORun.PreparationContext)?.Text);
            Assert.Equal(iteration3Reference, Query.PartOIterationText(partORun));
            Assert.Contains(PartOTM59ResultSummary.RunFacts(partORun, null, null, null), x => x.Label == "Scenario" && x.Value == scenario);

            //...and nothing that acts on the catalogue is lent descriptors the resumed run does not have.
            Assert.False(partORun.PreparationContext.HasVentilationUnitCatalogue);
            Assert.Null(partORun.PreparationContext.VentilationUnitCapacityDescriptors);
        }

        /// <summary>A sidecar saved before the record existed still resumes, but the run is not named 1a or 2.</summary>
        [Fact]
        public void AV1Sidecar_StillResumes_ButTheRunIsNotNamed()
        {
            using Saved saved = Save(true);

            //What an older build wrote: the same sidecar with the v1 schema and no catalogue field.
            JsonObject jsonObject = (JsonObject)JsonNode.Parse(File.ReadAllText(saved.Path_Resume))!;
            jsonObject["Schema"] = PartORunResume.Schema_V1;
            jsonObject.Remove("VentilationUnitCatalogueOffered");
            File.WriteAllText(saved.Path_Resume, jsonObject.ToJsonString());

            PartORun partORun = saved.Reopen();

            Assert.True(partORun.CanResumeIteration3, partORun.ResumeRefusal);
            Assert.Null(partORun.PreparationContext.VentilationUnitCatalogueOffered);
            Assert.Null(PartOWorkflowScenario.Find(partORun.PreparationContext));
            Assert.Equal("MVHR iteration (1a or 2, not recorded by this saved run)", Query.PartOIterationText(partORun));
            Assert.DoesNotContain(PartOTM59ResultSummary.RunFacts(partORun, null, null, null), x => x.Label == "Scenario");
        }

        /// <summary>
        /// The Hub names a reopened run from the same record the TM59 window does - never from its resumed context
        /// alone - and an older (v1) sidecar leaves it unnamed, as before.
        /// </summary>
        [Theory]
        [InlineData(true, false, "Saved Iteration 2 results")]
        [InlineData(false, false, "Saved Iteration 1a results")]
        [InlineData(true, true, "Saved results")]
        public void TheHub_NamesAReopenedRunFromItsSavedRecordOnly(bool catalogue, bool v1, string subject)
        {
            using Saved saved = Save(catalogue);

            if (v1)
            {
                JsonObject jsonObject = (JsonObject)JsonNode.Parse(File.ReadAllText(saved.Path_Resume))!;
                jsonObject["Schema"] = PartORunResume.Schema_V1;
                jsonObject.Remove("VentilationUnitCatalogueOffered");
                File.WriteAllText(saved.Path_Resume, jsonObject.ToJsonString());
            }

            PartORun partORun = saved.Reopen();

            PartOWorkflowOutcome? partOWorkflowOutcome = Modify.HubOutcome(null, partORun, Modify.Capabilities(partORun, out _));

            Assert.NotNull(partOWorkflowOutcome);
            Assert.Equal(string.Format("{0} reopened — ready to review", subject), partOWorkflowOutcome!.Headline);
            Assert.DoesNotContain("PASS", partOWorkflowOutcome.Text);
            Assert.DoesNotContain("FAIL", partOWorkflowOutcome.Text);

            //Reviewed: the same subject, with whatever verdict the assessment gave.
            Assert.Equal(string.Format("{0} reviewed — TM59 UNAVAILABLE", subject), Modify.ReviewOutcome(PartOTM59ResultSummary.Unavailable("No results.", partORun), partORun)!.Headline);
        }

        [Fact]
        public void TheSidecar_RecordsWhetherACatalogueWasOffered()
        {
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + PartORunResume.Suffix_Resume);

            try
            {
                foreach (bool? value in new bool?[] { true, false })
                {
                    File.WriteAllText(path, new PartORunResume { VentilationUnitCatalogueOffered = value, SolarCalculationMethod = "TAS" }.ToJsonObject().ToJsonString());

                    PartORunResume? read = PartORunResume.Read(path);

                    Assert.NotNull(read);
                    Assert.Equal(PartORunResume.Schema_Current, read!.Schema);
                    Assert.Equal(value, read.VentilationUnitCatalogueOffered);
                }
            }
            finally
            {
                File.Delete(path);
            }
        }

        /// <summary>A live preparation is unaffected: it answers from its own descriptors, as before.</summary>
        [Fact]
        public void ALivePreparation_AnswersFromItsDescriptors()
        {
            Assert.True(Context(true).VentilationUnitCatalogueOffered);
            Assert.False(Context(false).VentilationUnitCatalogueOffered);
            Assert.False(Context(true).IsResumed);
            Assert.Equal(PartOWorkflowScenario.Text_Iteration2, PartOWorkflowScenario.Find(Context(true))?.Text);
            Assert.Equal(PartOWorkflowScenario.Text_Iteration1a, PartOWorkflowScenario.Find(Context(false))?.Text);
        }

        // ----- fixtures ---------------------------------------------------------------------------------

        private sealed class Saved : IDisposable
        {
            public string Path_TSD { get; init; } = string.Empty;

            public AnalyticalModel AnalyticalModel_Workflow { get; init; } = null!;

            public string Path_Resume => PartORunResume.Path_Resume(Path_TSD);

            /// <summary>A fresh session opening the saved run model.</summary>
            public PartORun Reopen()
            {
                PartORun result = new();

                Assert.True(result.Restore(AnalyticalModel_Workflow, null, out string refusal), refusal);

                return result;
            }

            public void Dispose()
            {
                foreach (string path in new[] { Path_TSD, Path_Resume, PartORunResume.Path_PreparedModel(Path_TSD) })
                {
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }
                }
            }
        }

        private static Saved Save(bool catalogue)
        {
            Zone zone = new("Flat 1");

            AdjacencyCluster adjacencyCluster = new();
            adjacencyCluster.AddObject(zone);

            AnalyticalModel analyticalModel_Prepared = new("prepared", null, null, null, adjacencyCluster, null, null);

            PartOPreparationContext partOPreparationContext = new(PartOIteration.BasePassive, [zone], [], catalogue ? Descriptors() : null);

            PartORun partORun = new();

            Assert.True(partORun.Prepare(analyticalModel_Prepared, Scenarios(), partOPreparationContext));

            string path_TSD = Path.Combine(Path.GetTempPath(), string.Format("SAM_PartORunResumeNamingTests_{0}.tsd", Guid.NewGuid()));

            Assert.True(partORun.ExpectResults(path_TSD));

            File.WriteAllText(path_TSD, string.Format("results - {0}", Guid.NewGuid()));

            PartOSimulationContext partOSimulationContext = new(Path.GetTempPath(), "Fixture", null, SolarCalculationMethod.SAM, 1, 365);

            AnalyticalModel analyticalModel_Workflow = new("workflow", null, null, null, new AdjacencyCluster(), null, null);

            Assert.True(partORun.Complete(analyticalModel_Workflow, path_TSD, partOSimulationContext, out string refusal), refusal);

            //What RunPartOSimulation stamps on the saved run model, and the sidecar it writes beside the results.
            List<OverheatingScenario> overheatingScenarios = partORun.OverheatingScenarios;
            analyticalModel_Workflow.SetValue(Analytical.AnalyticalModelParameter.OverheatingScenarios, new SAMCollection<OverheatingScenario>(overheatingScenarios));

            SimulationResultProvenance simulationResultProvenance = new(analyticalModel_Workflow, path_TSD);
            analyticalModel_Workflow.SetValue(Analytical.AnalyticalModelParameter.SimulationResultProvenance, simulationResultProvenance);

            Assert.True(Modify.PersistPartORunResume(partORun, partOSimulationContext, simulationResultProvenance, path_TSD, out string note), note);

            return new Saved { Path_TSD = path_TSD, AnalyticalModel_Workflow = analyticalModel_Workflow };
        }

        private static PartOPreparationContext Context(bool catalogue)
        {
            return new PartOPreparationContext(PartOIteration.BasePassive, [new Zone("Flat 1")], [], catalogue ? Descriptors() : null);
        }

        private static List<VentilationUnitCapacityDescriptor> Descriptors()
        {
            return [new VentilationUnitCapacityDescriptor(new VentilationUnitReference("Test", "Model", "TEST-1"), 150, 150, 10)];
        }

        private static List<OverheatingScenario> Scenarios()
        {
            return [new OverheatingScenario(PartOAssessmentScope.Dwelling, Guid.NewGuid(), PartOIteration.BasePassive)];
        }
    }
}
