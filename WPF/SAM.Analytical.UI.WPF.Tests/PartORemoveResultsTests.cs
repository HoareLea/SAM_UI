// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using SAM.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Windows;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// Results &gt; Part O &gt; Remove Results: a clean baseline for Mixed Design from a model that has been through
    /// Prepare &amp; Run. The removal is SAM's (<c>Modify.RemovePartORunState</c>) and the verdict is the Mixed Design
    /// validator's (<c>Query.PartOBaselineFindings</c>); these tests pin that the UI carries both answers faithfully,
    /// never writes over the open model, and checks the SAVED file.
    /// </summary>
    [Collection(WpfCollection.Name)]
    public class PartORemoveResultsTests : IDisposable
    {
        private readonly string directory = Path.Combine(Path.GetTempPath(), "SAM_PartORemoveResults_" + Guid.NewGuid().ToString("N"));

        public PartORemoveResultsTests()
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
        public void RunModel_IsCleanedIntoABaseline_TheMixedDesignSessionAccepts()
        {
            AnalyticalModel run = RunModel(Held(PartOMixedDesignFixture.Baseline()));
            Assert.NotEmpty(Analytical.Query.PartOBaselineFindings(run));
            string json_Run = Core.Convert.ToString(run);

            PartORemoveResults partORemoveResults = PartORemoveResults.Create(run);

            Assert.True(partORemoveResults.HasChanges);
            Assert.Empty(partORemoveResults.Kept);
            Assert.True(partORemoveResults.IsClean, string.Join("\n", partORemoveResults.Findings.Select(x => x.Message)));
            Assert.Contains(partORemoveResults.Removed, x => x.Contains("Part O MVHR ventilation system"));
            Assert.Contains(partORemoveResults.Removed, x => x.Contains("simulation result object"));

            //The open model is not modified, and it is still refused.
            Assert.Equal(json_Run, Core.Convert.ToString(run));

            //The same verdict the Mixed Design window reaches.
            PartOMixedDesignSession partOMixedDesignSession = new(partORemoveResults.Cleaned!, null, null, null);
            Assert.True(partOMixedDesignSession.IsCleanBaseline);
        }

        [Fact]
        public void CleanBaseline_HasNothingToRemove_AndPasses()
        {
            PartORemoveResults partORemoveResults = PartORemoveResults.Create(PartOMixedDesignFixture.Baseline());

            Assert.False(partORemoveResults.HasChanges);
            Assert.True(partORemoveResults.IsClean);
        }

        [Fact]
        public void UnprovableCondition_IsLeft_AndTheCheckFails_WithSAMsReason()
        {
            //No held copy of the authored conditions: SAM cannot prove what the Part F rates replaced.
            PartORemoveResults partORemoveResults = PartORemoveResults.Create(RunModel(PartOMixedDesignFixture.Baseline()));

            Assert.True(partORemoveResults.HasChanges);
            Assert.False(partORemoveResults.IsClean);
            Assert.NotEmpty(partORemoveResults.Kept);
            PartOMaterialisationRefusal finding = Assert.Single(partORemoveResults.Findings);
            Assert.Contains("ApplyPartFVentilationRates", finding.Message);
        }

        [Fact]
        public void Save_WritesANewFile_AndChecksTheFileAsReadBack()
        {
            string path_Model = Path.Combine(directory, "Block.sam");
            AnalyticalModel run = RunModel(Held(PartOMixedDesignFixture.Baseline()));
            Assert.True(Core.Convert.ToFile(new IJSAMObject[] { run }, path_Model));
            string hash_Model = Hash(path_Model);

            PartORemoveResults partORemoveResults = PartORemoveResults.Create(run);
            string path = PartORemoveResults.DefaultPath(path_Model)!;
            Assert.Equal(Path.Combine(directory, "Block-Cleaned.sam"), path);

            Assert.Null(partORemoveResults.Save(path, path_Model));

            Assert.Equal(path, partORemoveResults.Path_Saved);
            Assert.True(partORemoveResults.IsSavedClean);
            Assert.True(Core.Convert.ToSAM<AnalyticalModel>(path).Single().IsPartOCleanBaseline(out _));
            Assert.Equal(hash_Model, Hash(path_Model));
        }

        [Fact]
        public void Save_NeverOverwritesTheOpenModel()
        {
            string path_Model = Path.Combine(directory, "Block.sam");
            AnalyticalModel run = RunModel(Held(PartOMixedDesignFixture.Baseline()));
            Assert.True(Core.Convert.ToFile(new IJSAMObject[] { run }, path_Model));
            string hash_Model = Hash(path_Model);

            PartORemoveResults partORemoveResults = PartORemoveResults.Create(run);

            string? refusal = partORemoveResults.Save(path_Model.ToUpperInvariant(), path_Model);

            Assert.NotNull(refusal);
            Assert.Contains("never overwrites", refusal);
            Assert.Null(partORemoveResults.Path_Saved);
            Assert.Equal(hash_Model, Hash(path_Model));
        }

        [Fact]
        public void DefaultPath_OfAnUnsavedModel_IsNull()
        {
            Assert.Null(PartORemoveResults.DefaultPath(null));
        }

        [WpfFact]
        public void Window_SaveIsTheDefault_ThenPassOfTheSavedFile_AndOpenIsOffered()
        {
            string path_Model = Path.Combine(directory, "Block.sam");
            PartORemoveResults partORemoveResults = PartORemoveResults.Create(RunModel(Held(PartOMixedDesignFixture.Baseline())));

            PartORemoveResultsWindow partORemoveResultsWindow = new(partORemoveResults, path_Model);

            Assert.True(partORemoveResultsWindow.button_Save.IsEnabled);
            Assert.True(partORemoveResultsWindow.button_Save.IsDefault);
            Assert.Equal("Save cleaned copy...", partORemoveResultsWindow.button_Save.Content);
            Assert.Equal(Visibility.Collapsed, partORemoveResultsWindow.button_Open.Visibility);
            Assert.StartsWith("PASS", partORemoveResultsWindow.textBlock_Check.Text);
            Assert.Equal("✓", partORemoveResultsWindow.textBlock_CheckGlyph.Text);
            Assert.Contains("cleaned copy", partORemoveResultsWindow.textBlock_Check.Text);

            string? path_Offered = null;
            partORemoveResultsWindow.ChooseFile = x => { path_Offered = x; return x; };
            partORemoveResultsWindow.Save();

            Assert.Equal(Path.Combine(directory, "Block-Cleaned.sam"), path_Offered);
            Assert.True(File.Exists(path_Offered));
            Assert.StartsWith("PASS", partORemoveResultsWindow.textBlock_Check.Text);
            Assert.Contains("saved copy", partORemoveResultsWindow.textBlock_Check.Text);
            Assert.False(partORemoveResultsWindow.button_Save.IsEnabled);
            Assert.Equal(Visibility.Visible, partORemoveResultsWindow.button_Open.Visibility);
            Assert.True(partORemoveResultsWindow.button_Open.IsDefault);
            Assert.Equal("Close", partORemoveResultsWindow.button_Close.Content);
        }

        [WpfFact]
        public void Window_Fail_ListsEveryFinding_WithAGlyphNotColourAlone()
        {
            PartORemoveResults partORemoveResults = PartORemoveResults.Create(RunModel(PartOMixedDesignFixture.Baseline()));

            PartORemoveResultsWindow partORemoveResultsWindow = new(partORemoveResults, null);

            Assert.StartsWith("FAIL", partORemoveResultsWindow.textBlock_Check.Text);
            Assert.Equal("✕", partORemoveResultsWindow.textBlock_CheckGlyph.Text);
            List<string> findings = ((IEnumerable<string>)partORemoveResultsWindow.itemsControl_Findings.ItemsSource).ToList();
            Assert.Equal(partORemoveResults.Findings.Count, findings.Count);
            Assert.Equal(Visibility.Visible, partORemoveResultsWindow.stackPanel_Kept.Visibility);
        }

        [WpfFact]
        public void Window_CleanModel_HasNothingToSave()
        {
            PartORemoveResultsWindow partORemoveResultsWindow = new(PartORemoveResults.Create(PartOMixedDesignFixture.Baseline()), null);

            Assert.False(partORemoveResultsWindow.button_Save.IsEnabled);
            Assert.Equal(Visibility.Visible, partORemoveResultsWindow.textBlock_NothingRemoved.Visibility);
            Assert.StartsWith("PASS", partORemoveResultsWindow.textBlock_Check.Text);
        }

        /// <summary>
        /// The baseline as Map IC (TM59) leaves it: each space's condition is also held in the cluster, which is where
        /// SAM proves what a Part F rewrite replaced.
        /// </summary>
        private static AnalyticalModel Held(AnalyticalModel analyticalModel)
        {
            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;
            foreach (Space space in adjacencyCluster.GetSpaces())
            {
                if (space.InternalCondition is not null)
                {
                    adjacencyCluster.AddObject(new InternalCondition(space.InternalCondition));
                }
            }

            return new AnalyticalModel(analyticalModel, adjacencyCluster);
        }

        /// <summary>What Prepare &amp; Run leaves open: an Iteration 1a preparation of every dwelling, its scenarios and TAS's output.</summary>
        private static AnalyticalModel RunModel(AnalyticalModel baseline)
        {
            List<Zone> dwellings = PartOMixedDesignFixture.Dwellings(baseline);
            PartOIterationPreparation preparation = baseline.PreparePartOIteration(PartOIteration.BasePassive, dwellings, dwellings.ToDictionary(x => x.Guid, x => "MVHR"));
            Assert.Null(preparation.Refusal);

            AnalyticalModel result = new(preparation.AnalyticalModel);
            result.SetValue(Analytical.AnalyticalModelParameter.OverheatingScenarios, new SAMCollection<OverheatingScenario>(preparation.OverheatingScenarios));
            result.SetValue(Analytical.AnalyticalModelParameter.SimulationResultProvenance, new SimulationResultProvenance(result, null));

            AdjacencyCluster adjacencyCluster = result.AdjacencyCluster;
            foreach (Space space in adjacencyCluster.GetSpaces())
            {
                SpaceSimulationResult spaceSimulationResult = new(space.Name, "Tas", space.Guid.ToString());
                adjacencyCluster.AddObject(spaceSimulationResult);
                adjacencyCluster.AddRelation(space, spaceSimulationResult);
            }

            adjacencyCluster.AddObject(new DesignDay(new DesignDay("London ANN CLG", 2018, 7, 1), LoadType.Cooling));

            return new AnalyticalModel(result, adjacencyCluster);
        }

        private static string Hash(string path)
        {
            using SHA256 sHA256 = SHA256.Create();
            using FileStream fileStream = File.OpenRead(path);

            return System.Convert.ToHexString(sHA256.ComputeHash(fileStream));
        }
    }
}
