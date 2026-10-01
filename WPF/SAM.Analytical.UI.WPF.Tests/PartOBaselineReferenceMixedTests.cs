// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using SAM.Analytical.Tas;
using SAM.Analytical.UI;
using SAM.Core;
using SAM.Weather;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// <b>PR-5, Mixed Design:</b> the run model a mixed run saves says it is a Mixed Design result and names the baseline it was materialised
    /// from. SAM's materialiser supplies the identity (guid, name, the record's own baseline fingerprint); the run supplies the baseline's
    /// file as a locator, because SAM does not know it. TAS is replaced at the one workflow seam.
    /// </summary>
    [Collection(WpfCollection.Name)]
    public class PartOBaselineReferenceMixedTests : IDisposable
    {
        private readonly string directory = Path.Combine(Path.GetTempPath(), "SAM_PartOBaselineMixed_" + Guid.NewGuid().ToString("N"));

        public PartOBaselineReferenceMixedTests()
        {
            Directory.CreateDirectory(directory);
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(directory, true);
            }
            catch
            {
            }
        }

        [Fact]
        public void A_mixed_run_model_names_the_baseline_it_was_materialised_from()
        {
            //The baseline: two flats and a corridor, every dwelling naturally ventilated, with a layered partition so the run gets through.
            AnalyticalModel analyticalModel = PartOMixedDesignFixture.Baseline(2);
            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;
            Construction construction = new(Guid.NewGuid(), "Internal Partition", [new ConstructionLayer("Plasterboard", 0.1)]);
            foreach (Panel panel in adjacencyCluster.GetPanels())
            {
                adjacencyCluster.AddObject(Analytical.Create.Panel(panel, construction));
            }

            MaterialLibrary materialLibrary = new("Materials");
            materialLibrary.Add(new OpaqueMaterial(Guid.NewGuid(), "Plasterboard", "Plasterboard", "Fixture", 0.25, 900, 1000));

            AnalyticalModel analyticalModel_Baseline = PartOMixedDesignFixture.WithStrategies(new AnalyticalModel(analyticalModel, adjacencyCluster, materialLibrary, analyticalModel.ProfileLibrary), PartOMixedDesignFixture.Natural);

            string path_Design = Path.Combine(directory, "model", "Block.sam");
            Directory.CreateDirectory(Path.GetDirectoryName(path_Design)!);
            Assert.True(Core.Convert.ToFile(analyticalModel_Baseline, path_Design, SAMFileType.SAM));
            string fingerprint_Baseline = SimulationResultProvenance.Fingerprint(analyticalModel_Baseline);

            PartOMaterialisation partOMaterialisation = Analytical.Modify.MaterialisePartODwellingStrategies(analyticalModel_Baseline, null);
            Assert.True(partOMaterialisation.IsMaterialised, partOMaterialisation.Refusal);

            //SAM names the baseline by identity; it knows no file.
            PartOBaselineReference partOBaselineReference_Materialised = partOMaterialisation.AnalyticalModel.GetValue<PartOBaselineReference>(Analytical.AnalyticalModelParameter.PartOBaselineReference);
            Assert.Equal(PartODerivedCase.MixedDesign, partOBaselineReference_Materialised.Case);
            Assert.Null(partOBaselineReference_Materialised.Design.Path_Relative);

            //The run: the same pipeline every Part O run uses, in a private run, exactly as SimulatePartOMaterialisation builds it.
            string directory_Tas = Path.Combine(directory, "PartO", "MixedDesign", "tas");
            Directory.CreateDirectory(directory_Tas);

            PartOSimulationContext partOSimulationContext = new(directory_Tas, "Block_Mixed", new WeatherData("Fixture", "Fixture", 51.5, -0.1, 25), SolarCalculationMethod.TAS, 1, 365)
            {
                Path_DesignModel = path_Design,
            };

            PartORun partORun = new();
            Assert.True(partORun.Prepare(partOMaterialisation.AnalyticalModel, partOMaterialisation.OverheatingScenarios), partORun.InvalidationReason);

            AnalyticalModel analyticalModel_Workflow = Modify.RunPartOSimulation(partOMaterialisation.AnalyticalModel, partOSimulationContext, partOSimulationContext.ProjectName, partORun, CancellationToken.None, out string _, out string path_TSD, out bool cancelled, out bool fullYear, out _, out string refusal, null, Runner);

            Assert.True(refusal is null, refusal);
            Assert.False(cancelled);
            Assert.True(fullYear);
            Assert.NotNull(analyticalModel_Workflow);

            string path_Result = Query.Path_PartORunModel(path_TSD);
            AnalyticalModel analyticalModel_Result = Core.Convert.ToSAM<AnalyticalModel>(path_Result).OfType<AnalyticalModel>().Single();

            Assert.True(analyticalModel_Result.TryGetValue(Analytical.AnalyticalModelParameter.PartOBaselineReference, out PartOBaselineReference partOBaselineReference));
            Assert.True(partOBaselineReference.IsValid);
            Assert.Equal(PartODerivedCase.MixedDesign, partOBaselineReference.Case);
            Assert.Null(partOBaselineReference.Source);
            Assert.Equal(analyticalModel_Baseline.Guid, partOBaselineReference.Design.Guid);
            Assert.Equal(fingerprint_Baseline, partOBaselineReference.Design.Fingerprint);
            Assert.Equal(partOMaterialisation.Record.Fingerprint_Baseline, partOBaselineReference.Design.Fingerprint);

            //The locator the run added: the way to the baseline's file from the folder the result is written to - and nothing absolute.
            Assert.DoesNotContain("Path_Absolute", partOBaselineReference.ToJsonObject().ToJsonString());
            Assert.DoesNotContain(directory, partOBaselineReference.ToJsonObject().ToJsonString(), StringComparison.OrdinalIgnoreCase);
            Assert.Equal(Path.Combine("..", "..", "..", "model", "Block.sam"), partOBaselineReference.Design.Path_Relative);
            Assert.Equal(PartOBaselineResolutionStatus.Resolved, Analytical.Query.PartOModelResolution(partOBaselineReference.Design, path_Result).Status);

            //Stamped and located before the provenance record, so the saved result still matches it.
            Assert.Equal(analyticalModel_Result.GetValue<SimulationResultProvenance>(Analytical.AnalyticalModelParameter.SimulationResultProvenance).Fingerprint_Model, SimulationResultProvenance.Fingerprint(analyticalModel_Result));

            //The baseline itself is neither marked nor changed, and the result is not a baseline.
            Assert.False(analyticalModel_Baseline.HasValue(Analytical.AnalyticalModelParameter.PartOBaselineReference));
            Assert.Equal(fingerprint_Baseline, SimulationResultProvenance.Fingerprint(Core.Convert.ToSAM<AnalyticalModel>(path_Design).OfType<AnalyticalModel>().Single()));
            Assert.NotNull(UI.Query.PartODesignModelRefusal(analyticalModel_Result, path_Result));
        }

        /// <summary>TAS, replaced: writes this run's own results file and hands the model back.</summary>
        private static AnalyticalModel? Runner(AnalyticalModel analyticalModel, WorkflowSettings workflowSettings, CancellationToken cancellationToken, out bool cancelled)
        {
            cancelled = false;

            File.WriteAllText(Path.ChangeExtension(workflowSettings.Path_TBD, "tsd"), string.Format("results - {0}", Guid.NewGuid()));

            return analyticalModel;
        }
    }
}
