// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;

namespace SAM.Analytical.UI
{
    /// <summary>
    /// One TM59 criterion of one room, as each of the two existing assessments reported it.
    ///
    /// <para><b>Carried, never recomputed</b></para>
    /// <para>
    /// Every field on both sides comes straight off a <c>PartOTM59SpaceResult</c>, which in turn carries the
    /// production <c>TM59AssessmentReport</c>'s own Actual, Limit and <c>ComplianceStatus</c> verbatim. This
    /// orchestration does not hold a criterion, a threshold, an exceedance rule, a count, a status or a
    /// combined verdict of its own - so it cannot disagree with TM59, and there is nothing here to keep in
    /// step when TM59 changes.
    /// </para>
    /// <para>
    /// <b><see cref="Changed"/> is a presentation fact, not an engineering one.</b> It says the two
    /// assessments reported different statuses for this criterion, which is what a reader wants to filter
    /// on. It states nothing about which is right.
    /// </para>
    /// </summary>
    public class PartOIteration3CriterionComparison
    {
        public PartOIteration3CriterionComparison(
            Guid guid_Space,
            string name_Space,
            Guid guid_Dwelling,
            string name_Dwelling,
            string check,
            bool mechanical,
            int? actual_A,
            int? limit_A,
            TM59ComplianceStatus status_A,
            int? actual_B,
            int? limit_B,
            TM59ComplianceStatus status_B)
        {
            Guid_Space = guid_Space;
            Name_Space = name_Space;
            Guid_Dwelling = guid_Dwelling;
            Name_Dwelling = name_Dwelling;
            Check = check;
            Mechanical = mechanical;
            Actual_A = actual_A;
            Limit_A = limit_A;
            Status_A = status_A;
            Actual_B = actual_B;
            Limit_B = limit_B;
            Status_B = status_B;
        }

        /// <summary>The design space. The only key anything joins on.</summary>
        public Guid Guid_Space { get; }

        /// <summary>Display only.</summary>
        public string Name_Space { get; }

        /// <summary>The dwelling zone, for grouping. <see cref="Guid.Empty"/> where none.</summary>
        public Guid Guid_Dwelling { get; }

        /// <summary>Display only.</summary>
        public string Name_Dwelling { get; }

        /// <summary>The TM59 check, exactly as the report named it.</summary>
        public string Check { get; }

        /// <summary>Whether this is the mechanical-ventilation criterion. The report's own classification.</summary>
        public bool Mechanical { get; }

        /// <summary>Reference A's reported value.</summary>
        public int? Actual_A { get; }

        /// <summary>Reference A's reported limit.</summary>
        public int? Limit_A { get; }

        /// <summary>Reference A's production verdict, verbatim.</summary>
        public TM59ComplianceStatus Status_A { get; }

        /// <summary>Candidate B's reported value.</summary>
        public int? Actual_B { get; }

        /// <summary>Candidate B's reported limit.</summary>
        public int? Limit_B { get; }

        /// <summary>Candidate B's production verdict, verbatim.</summary>
        public TM59ComplianceStatus Status_B { get; }

        /// <summary>
        /// The signed movement in the reported value, B - A. Null where either assessment produced no
        /// count for this criterion - a missing count is not a zero movement.
        /// </summary>
        public int? Delta_Actual => Actual_A.HasValue && Actual_B.HasValue ? Actual_B.Value - Actual_A.Value : (int?)null;

        /// <summary>Whether the two assessments returned different statuses for this criterion.</summary>
        public bool Changed => Status_A != Status_B;

        public override string ToString()
        {
            return string.Format(
                "{0} - {1}: A {2}/{3} {4}, B {5}/{6} {7}",
                Name_Space ?? Guid_Space.ToString(),
                Check,
                Actual_A.HasValue ? Actual_A.Value.ToString() : "-",
                Limit_A.HasValue ? Limit_A.Value.ToString() : "-",
                Core.Query.Description(Status_A),
                Actual_B.HasValue ? Actual_B.Value.ToString() : "-",
                Limit_B.HasValue ? Limit_B.Value.ToString() : "-",
                Core.Query.Description(Status_B));
        }
    }
}
