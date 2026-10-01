// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.UI.WPF.Tests.Helpers;
using SAM.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// U-value PR2b: after Apply, the scoped check (SAM's per-object model-check rules over the changed construction
    /// and its panels only, summarised in one line) and the saved U-VALUE CHANGE report (provenance, "may since have
    /// been undone" stamp, the change, the check). Neither writes to the model.
    /// </summary>
    public class UValueReportTests : IDisposable
    {
        private readonly string directory = Path.Combine(Path.GetTempPath(), "SAM_UValueReportTests_" + Guid.NewGuid().ToString("N"));

        public UValueReportTests()
        {
            Directory.CreateDirectory(directory);
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(directory, true);
            }
            catch (IOException)
            {
            }
        }

        private static SetUValueRequest Request(Construction construction, UValueApplyMode mode = UValueApplyMode.NewConstruction, UValueApplyScope scope = UValueApplyScope.AllPanels)
        {
            return new SetUValueRequest()
            {
                ConstructionGuid = construction.Guid,
                LayerIndex = UValueFixture.WoolIndex,
                Thickness = 0.0670833,
                InitialThermalTransmittance = 0.2597,
                CalculatedThermalTransmittance = 0.3,
                TargetThermalTransmittance = 0.3,
                HeatFlowDirection = HeatFlowDirection.Horizontal,
                Mode = mode,
                Scope = scope,
            };
        }

        private static AnalyticalModel Applied(out SetUValueResult result, UValueApplyMode mode = UValueApplyMode.NewConstruction, UValueApplyScope scope = UValueApplyScope.AllPanels)
        {
            AnalyticalModel analyticalModel = UValueFixture.Model(out Construction source);
            AnalyticalModel changed = Modify.SetUValue(analyticalModel, Request(source, mode, scope), out result);
            Assert.True(result.Succeeded, result.Error);
            return changed;
        }

        // -------------------------------------------------------------------------------------------------
        // Scoped check
        // -------------------------------------------------------------------------------------------------

        [Fact]
        public void Check_CoversTheChangedConstructionAndItsPanels_AndSummarisesInOneLine()
        {
            AnalyticalModel changed = Applied(out SetUValueResult result);

            UValueCheckSummary summary = Query.UValueCheckSummary(changed, result);

            // The fixture wall, like the real one, starts with an air gap: SAM flags that as an Error. No missing material.
            Assert.DoesNotContain(summary.Log, x => x.Text.Contains("Material Library does not contain"));
            Assert.Equal(summary.Log.Count(x => x.LogRecordType == LogRecordType.Error), summary.Errors);
            Assert.Equal(summary.Log.Count(x => x.LogRecordType == LogRecordType.Warning), summary.Warnings);
            Assert.EndsWith("for SIM_EXT_SLD U0.30 and its 12 panels.", summary.Text);
            Assert.Equal(summary.Log.Count(), summary.Log.Select(x => x.LogRecordType + x.Text).Distinct().Count());
        }

        [Fact]
        public void Check_WithNoErrorsOrWarnings_Passes()
        {
            // A wall whose first and last layers are solid: nothing for SAM's rules to flag.
            Construction construction = new Construction(Guid.NewGuid(), "Solid", new List<ConstructionLayer>() { new ConstructionLayer(UValueFixture.Board, 0.012), new ConstructionLayer(UValueFixture.Wool, 0.08), new ConstructionLayer(UValueFixture.Board, 0.012) });
            AdjacencyCluster adjacencyCluster = new AdjacencyCluster();
            adjacencyCluster.AddObject(UValueFixture.Panel(construction, PanelType.WallExternal, 0));
            AnalyticalModel analyticalModel = new AnalyticalModel("UValue", null, null, null, adjacencyCluster, UValueFixture.Materials(), new ProfileLibrary("Profiles"));
            SetUValueRequest request = Request(construction);
            request.LayerIndex = 1;
            AnalyticalModel changed = Modify.SetUValue(analyticalModel, request, out SetUValueResult result);

            UValueCheckSummary summary = Query.UValueCheckSummary(changed, result);

            Assert.True(summary.Passed, string.Join(Environment.NewLine, summary.Log.Select(x => x.Text)));
            Assert.Equal("\u2713", summary.Glyph);
            Assert.Equal("No errors or warnings for Solid U0.30 and its 1 panel.", summary.Text);
        }

        [Fact]
        public void Check_SummaryCountsErrorsAndWarnings_WithTheRightGlyph()
        {
            AnalyticalModel changed = Applied(out SetUValueResult result);
            UValueCheckSummary summary = Query.UValueCheckSummary(changed, result);
            Assert.True(summary.Errors > 0);

            Assert.False(summary.Passed);
            Assert.Equal("\u2715", summary.Glyph);
            Assert.StartsWith(string.Format("{0} error", summary.Errors), summary.Text);
        }

        [Fact]
        public void Check_DontAssign_NamesTheUnassignedConstruction()
        {
            AnalyticalModel changed = Applied(out SetUValueResult result, scope: UValueApplyScope.DontAssign);

            UValueCheckSummary summary = Query.UValueCheckSummary(changed, result);

            Assert.EndsWith("for SIM_EXT_SLD U0.30 (assigned to no panel).", summary.Text);
        }

        [Fact]
        public void Check_DoesNotModifyTheModel()
        {
            AnalyticalModel changed = Applied(out SetUValueResult result);
            string before = changed.ToJsonObject().ToJsonString();

            Query.UValueCheckSummary(changed, result);

            Assert.Equal(before, changed.ToJsonObject().ToJsonString());
        }

        // -------------------------------------------------------------------------------------------------
        // Report
        // -------------------------------------------------------------------------------------------------

        [Fact]
        public void ReportText_CarriesProvenanceTheStampTheChangeAndTheCheck()
        {
            AnalyticalModel changed = Applied(out SetUValueResult result);
            UValueCheckSummary summary = Query.UValueCheckSummary(changed, result);

            string text = Query.UValueChangeReportText(result, summary, @"C:\Models\model.sam");

            Assert.StartsWith("U-VALUE CHANGE" + Environment.NewLine + "==============" + Environment.NewLine, text);
            Assert.Contains(@"Model:        C:\Models\model.sam", text);
            Assert.Contains(string.Format("applied at {0:yyyy-MM-dd HH:mm:ss}; may since have been undone", result.AppliedAt), text);
            Assert.Contains("Construction: SIM_EXT_SLD -> SIM_EXT_SLD U0.30 (new construction; SIM_EXT_SLD unchanged)", text);
            Assert.Contains("Layer:        3: I01_Mineral Wool -> I01_Mineral Wool_0.067m", text);
            Assert.Contains("Thickness:    80 mm -> 67 mm", text);
            Assert.Contains("U-value:      0.260 -> 0.300 W/m2K (target 0.300; horizontal heat flow)", text);
            Assert.Contains("Scope:        12 panels (all that used SIM_EXT_SLD)", text);
            Assert.Contains("Material:     I01_Mineral Wool_0.067m added to the Material Library", text);
            Assert.Contains(summary.Text, text);
            foreach (LogRecord logRecord in summary.Log)
            {
                Assert.Contains(logRecord.Text, text);
            }
        }

        [Fact]
        public void ReportText_InPlaceAndSelectedScopes_AreWorded()
        {
            Applied(out SetUValueResult inPlace, UValueApplyMode.ModifyInPlace);
            Assert.Contains("Construction: SIM_EXT_SLD (modified in place)", Query.UValueChangeReportText(inPlace, null, null));
            Assert.Contains("Model:        (not saved)", Query.UValueChangeReportText(inPlace, null, null));

            Applied(out SetUValueResult dontAssign, scope: UValueApplyScope.DontAssign);
            Assert.Contains("Scope:        not assigned to any panel", Query.UValueChangeReportText(dontAssign, null, null));
        }

        [Fact]
        public void ReportPath_IsBesideTheModel_NamedByModelAndTime()
        {
            DateTime appliedAt = new DateTime(2026, 10, 1, 14, 5, 9);

            Assert.Equal(@"C:\Models\Block A_UValueChange_20261001-140509.txt", Query.Path_UValueChangeReport(@"C:\Models\Block A.sam", appliedAt));
            Assert.Null(Query.Path_UValueChangeReport(null, appliedAt));
            Assert.Null(Query.Path_UValueChangeReport("  ", appliedAt));
        }

        [Fact]
        public void SaveReport_WritesBesideTheModel_AndNeverOverwrites()
        {
            DateTime appliedAt = new DateTime(2026, 10, 1, 14, 5, 9);
            string path_Model = Path.Combine(directory, "model.sam");

            Assert.True(Modify.SaveUValueChangeReport(path_Model, appliedAt, "first", out string path_First, out string refusal_First));
            Assert.True(Modify.SaveUValueChangeReport(path_Model, appliedAt, "second", out string path_Second, out string _));

            Assert.Null(refusal_First);
            Assert.Equal(Path.Combine(directory, "model_UValueChange_20261001-140509.txt"), path_First);
            Assert.Equal(Path.Combine(directory, "model_UValueChange_20261001-140509 (2).txt"), path_Second);
            Assert.Equal("first", File.ReadAllText(path_First));
            Assert.Equal("second", File.ReadAllText(path_Second));
        }

        [Fact]
        public void SaveReport_ForAnUnsavedModel_RefusesAndPointsToCopyAll()
        {
            Assert.False(Modify.SaveUValueChangeReport(null, DateTime.Now, "text", out string path, out string refusal));

            Assert.Null(path);
            Assert.Contains("Copy All", refusal);
        }

        [Fact]
        public void SaveReport_IntoAMissingFolder_Refuses()
        {
            Assert.False(Modify.SaveUValueChangeReport(Path.Combine(directory, "missing", "model.sam"), DateTime.Now, "text", out string _, out string refusal));

            Assert.Contains("does not exist", refusal);
        }
    }
}
