// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;

namespace SAM.Analytical.UI
{
    /// <summary>
    /// One dwelling's pooled A/B statistics, named so a reader can attribute them without a guid lookup.
    /// <para>
    /// A dwelling is the grouping an Approved Document O reader thinks in - the Part F sizing, the MVHR
    /// unit and the TM59 verdict are all per dwelling - so the diagnostics are offered that way as well as
    /// pooled over the whole assessment.
    /// </para>
    /// </summary>
    public class PartOIteration3DwellingStatistics
    {
        public PartOIteration3DwellingStatistics(Guid guid_Dwelling, string name_Dwelling, PartOIteration3Statistics partOIteration3Statistics)
        {
            Guid_Dwelling = guid_Dwelling;
            Name_Dwelling = name_Dwelling;
            Statistics = partOIteration3Statistics;
        }

        /// <summary>The dwelling zone. <see cref="Guid.Empty"/> for rooms in no dwelling zone.</summary>
        public Guid Guid_Dwelling { get; }

        /// <summary>Display only.</summary>
        public string Name_Dwelling { get; }

        /// <summary>The pooled statistics over this dwelling's rooms.</summary>
        public PartOIteration3Statistics Statistics { get; }

        public override string ToString()
        {
            return string.Format("{0}: {1}", Name_Dwelling ?? Guid_Dwelling.ToString(), Statistics);
        }
    }
}
