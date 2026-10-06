// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.UI.WPF
{
    public static partial class Query
    {
        /// <summary>
        /// A production TM59 occupied-space status in the words every Part O surface uses: PASS, FAIL, and
        /// NOT ASSESSED for anything that is neither (the same reading as <see cref="PartOTM59ResultSummary"/>).
        /// A word only - nothing is decided here; the status is the production assessment's own.
        /// </summary>
        public static string PartOVerdictText(TM59ComplianceStatus tM59ComplianceStatus)
        {
            return tM59ComplianceStatus switch
            {
                TM59ComplianceStatus.Pass => "PASS",
                TM59ComplianceStatus.Fail => "FAIL",
                _ => "NOT ASSESSED",
            };
        }
    }
}
