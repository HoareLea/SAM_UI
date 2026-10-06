// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.UI
{
    /// <summary>
    /// The names Candidate B's files are recorded and validated under.
    /// <para>
    /// Stated once, because a role is written by the run and read by the review, and the two agreeing by
    /// coincidence is how a review silently stops validating a file. A typo in one of these is a review
    /// that passes because it looked for something that was never there.
    /// </para>
    /// </summary>
    public static class PartOIteration3Roles
    {
        /// <summary>Candidate B's dedicated no-IZAM building.</summary>
        public const string ThermalSource_TBD = "Candidate B no-IZAM TBD";

        /// <summary>Its results - the load source the TAS Systems conversion reads.</summary>
        public const string ThermalSource_TSD = "Candidate B no-IZAM TSD";

        /// <summary>The TAS Systems document the explicit ventilation was converted into.</summary>
        public const string Systems_TPD = "Candidate B TPD";

        /// <summary>The thermostat bridge's copy of the same no-IZAM building.</summary>
        public const string Bridge_TBD = "Candidate B bridge TBD";

        /// <summary>The bridge's results - where Candidate B's resultant temperatures are read from.</summary>
        public const string Bridge_TSD = "Candidate B bridge TSD";

        /// <summary>Candidate B's reopenable analytical model, provenanced to the bridge TSD.</summary>
        public const string CandidateB_Model = "Candidate B model";

        /// <summary>Reference A's TM59 report.</summary>
        public const string ReferenceA_TM59Report = "Reference A TM59 report";

        /// <summary>Candidate B's TM59 report.</summary>
        public const string CandidateB_TM59Report = "Candidate B TM59 report";

        /// <summary>The pairing record itself.</summary>
        public const string Record = "Iteration 3 pairing record";

        /// <summary>PR5B: a cooling pairing's hourly recirculation OperatingAirFlow history, per unit.</summary>
        public const string OperatingAirFlow = "Candidate B OperatingAirFlow history";

        /// <summary>SAM#123: a manufacturer-guidance pairing's hourly operation read back from TAS, per unit.</summary>
        public const string GuidanceOperation = "Candidate B manufacturer-guidance operation history";

        /// <summary>
        /// Whether a role names a file that every assessment of its results rewrites - the two TM59
        /// reports.
        /// <para>
        /// Such a file is lineage, not comparison authority. A review reassesses both results files
        /// through the production TM59 path, and that path rewrites both reports, so holding a review to
        /// the reports' recorded fingerprints makes the next review of an unchanged pairing refuse as
        /// stale. What defines the comparison is the simulation artifacts and Candidate B's model, and
        /// every one of those stays validated.
        /// </para>
        /// </summary>
        public static bool IsRegeneratedByAssessment(string role)
        {
            return string.Equals(role, ReferenceA_TM59Report, System.StringComparison.Ordinal)
                || string.Equals(role, CandidateB_TM59Report, System.StringComparison.Ordinal);
        }
    }
}
