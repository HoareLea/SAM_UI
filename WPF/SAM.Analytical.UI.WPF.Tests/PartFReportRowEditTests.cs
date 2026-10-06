// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical;
using SAM.Analytical.Enums;
using SAM.Analytical.UI.WPF;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// Regression tests for what the Part F report says about a row that is being edited.
    /// <para>
    /// Editing a cell writes to the row object only, and the report used to be built before the rows were
    /// applied - so Report and Copy All showed the values from before the edit. Both buttons now apply the
    /// rows first, and this holds the behaviour the buttons rely on: a value committed from a row edit is
    /// what the report text contains.
    /// </para>
    /// </summary>
    public class PartFReportRowEditTests
    {
        /// <summary>
        /// A note typed into a check row and applied to the row's check appears in the report built from
        /// that check's dwelling, so a report produced while the edit is still in the grid cannot show the
        /// pre-edit value.
        /// </summary>
        [Fact]
        public void Report_ShowsTheValueAppliedFromARowEdit()
        {
            PartFComplianceCheck check = new("System designed and installed to minimise noise", "source", "requirement")
            {
                CalculatedStatus = PartFComplianceStatus.CannotBeDetermined,
                Status = PartFComplianceStatus.CannotBeDetermined,
            };

            PartFComplianceResult complianceResult = new("Flat 1")
            {
                Checks = [check],
            };

            PartFDwellingResult dwellingResult = new("Flat 1")
            {
                ComplianceResult = complianceResult,
            };

            PartFCheckRow row = new(check);

            row.Notes = "Not checked on site";
            row.Apply();

            string report = PartFReport.Build([dwellingResult], PartFOperatingMode.ContinuousDesign);

            Assert.Contains("Not checked on site", report);
        }
    }
}
