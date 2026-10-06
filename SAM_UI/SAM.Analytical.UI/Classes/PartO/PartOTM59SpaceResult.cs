// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;

namespace SAM.Analytical.UI
{
    /// <summary>
    /// One production TM59 criterion outcome for one <b>design</b> space of one optimisation iteration.
    /// <para>
    /// <b>Every value is the assessment's own.</b> <see cref="ComplianceStatus"/> is
    /// <c>TM59AssessmentReportCheck.ComplianceStatus</c> verbatim, and it is what decides whether the room
    /// is a target next round. It is <b>never</b> re-derived here from <see cref="Actual"/> and
    /// <see cref="Limit"/>: the criteria differ in whether a zero margin passes, and re-deriving would
    /// quietly overrule the calculation for every room sitting exactly on its limit - the reason
    /// <c>TM59AssessmentReportCheck</c> refuses to do it either.
    /// </para>
    /// <para>
    /// <b>The design space is what makes it usable.</b> A TM59 result is produced for the <i>simulated</i>
    /// space; an optimisation has to move a design terminal, which belongs to the design space. The
    /// translation is <c>SimulationSpaceMap</c>'s, by identity, and a result that does not resolve is
    /// reported rather than matched to a same-named room in another flat.
    /// </para>
    /// </summary>
    public class PartOTM59SpaceResult
    {
        public PartOTM59SpaceResult(Guid spaceGuid_Design, string spaceName, string check, int? actual, int? limit, TM59ComplianceStatus tM59ComplianceStatus, bool mechanical)
        {
            SpaceGuid_Design = spaceGuid_Design;
            SpaceName = spaceName;
            Check = check;
            Actual = actual;
            Limit = limit;
            ComplianceStatus = tM59ComplianceStatus;
            Mechanical = mechanical;
        }

        /// <summary>The design space this result was resolved to - the identity an optimisation target uses.</summary>
        public Guid SpaceGuid_Design { get; }

        /// <summary>The room's name, so a report reads without resolving the guid.</summary>
        public string SpaceName { get; }

        /// <summary>Which TM59 criterion this row states, as the production report names it.</summary>
        public string Check { get; }

        /// <summary>What the assessment counted. Null where the criterion produced no count.</summary>
        public int? Actual { get; }

        /// <summary>What the criterion allows, as the assessment derived it.</summary>
        public int? Limit { get; }

        /// <summary><see cref="Limit"/> - <see cref="Actual"/>. Null where either is unknown.</summary>
        public int? Margin => Actual.HasValue && Limit.HasValue ? Limit.Value - Actual.Value : (int?)null;

        /// <summary>The production verdict, used and never recomputed.</summary>
        public TM59ComplianceStatus ComplianceStatus { get; }

        /// <summary>
        /// Whether this came from the mechanical-ventilation criterion. Only mechanical results are
        /// Iteration 2B optimisation candidates - natural ventilation is not a mechanical airflow problem,
        /// and raising a design airflow is not how a naturally ventilated room is fixed.
        /// </summary>
        public bool Mechanical { get; }

        /// <summary>Whether the production assessment failed this room on this criterion.</summary>
        public bool IsFail => ComplianceStatus == TM59ComplianceStatus.Fail;

        public override string ToString()
        {
            return string.Format(
                "{0} {1}: {2} / {3} {4}",
                SpaceName,
                Check,
                Actual.HasValue ? Actual.Value.ToString() : "-",
                Limit.HasValue ? Limit.Value.ToString() : "-",
                Core.Query.Description(ComplianceStatus));
        }
    }
}
