// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.UI
{
    /// <summary>
    /// The Part F assessment behind the saved 2D views: run through the same <c>PartFCalculator</c> the Part
    /// F command uses, and reused across the views that share a model and a scope.
    /// <para>
    /// <b>It is also the gate.</b> A drawing whose dwelling scope nobody has chosen is not assessed at all -
    /// see <see cref="PartFAirflowViewSettings.HasDwellingScope"/>. That check lives HERE, in the one place
    /// that turns a view's settings into an engineering result, rather than in each caller: a blank scope
    /// reaches <c>PartFCalculator.Calculate(string)</c> as single-house mode, so a caller that forgot would
    /// silently assess a whole block of flats as one dwelling and draw it.
    /// </para>
    /// <para>
    /// <b>The cache is keyed on the model INSTANCE.</b> Nothing is copied into a view - a view holds how to
    /// present an assessment, never the assessment - so every regeneration re-reads it; and because
    /// <c>UIAnalyticalModel</c> hands out a fresh <c>AnalyticalModel</c> clone on every read, no edit to the
    /// building can be answered from a cached result. That invariant is the whole safety argument for
    /// caching an engineering value at all, so it is asserted rather than assumed - see
    /// <c>PartFAssessmentCacheTests</c>.
    /// </para>
    /// </summary>
    public class PartFAssessmentCache
    {
        private readonly Func<PartFCalculator> func_PartFCalculator;

        private AnalyticalModel analyticalModel;

        private PartFDwellingScope partFDwellingScope;

        private string zoneCategoryName;

        private List<PartFComplianceResult> partFComplianceResults;

        /// <summary>Calculating with the shipped rule set, as the Part F command does.</summary>
        public PartFAssessmentCache()
            : this(null)
        {
        }

        /// <summary>
        /// Calculating with a supplied calculator. The factory is called once per calculation and never
        /// stored, so a caller cannot hand over a calculator that carries state between models.
        /// </summary>
        public PartFAssessmentCache(Func<PartFCalculator> func_PartFCalculator)
        {
            //Qualified: SAM.Analytical.UI has a Query of its own, which would win here.
            this.func_PartFCalculator = func_PartFCalculator ?? Analytical.Query.DefaultPartFCalculator;
        }

        /// <summary>
        /// The assessment of every dwelling in the scope this view reports on, or nothing at all where that
        /// scope has not been decided.
        /// </summary>
        public List<PartFComplianceResult> Results(AnalyticalModel analyticalModel, PartFAirflowViewSettings partFAirflowViewSettings)
        {
            //The gate. An undecided scope is not whole-house mode; it is a question waiting for an answer,
            //and a drawing produced while it waits would be a wrong drawing rather than an absent one.
            if (analyticalModel?.AdjacencyCluster is null || partFAirflowViewSettings is null || !partFAirflowViewSettings.HasDwellingScope)
            {
                return [];
            }

            PartFDwellingScope partFDwellingScope = partFAirflowViewSettings.DwellingScope;

            //Null ONLY for a scope somebody chose to be the whole model. This is the single point where a
            //null reaches the calculator, and it is reached from an explicit WholeModel and nothing else.
            string zoneCategoryName = partFDwellingScope == PartFDwellingScope.ZoneCategory ? partFAirflowViewSettings.ZoneCategoryName : null;

            if (ReferenceEquals(analyticalModel, this.analyticalModel)
                && partFDwellingScope == this.partFDwellingScope
                && string.Equals(zoneCategoryName, this.zoneCategoryName, StringComparison.Ordinal)
                && partFComplianceResults is not null)
            {
                return partFComplianceResults;
            }

            PartFCalculator partFCalculator = func_PartFCalculator?.Invoke();
            if (partFCalculator is null)
            {
                //No rule set: nothing calculated, and nothing cached either, so installing one and reopening
                //the view does not find an empty answer sitting here.
                return [];
            }

            partFCalculator.AdjacencyCluster = analyticalModel.AdjacencyCluster;

            using (Core.UI.PerformanceLog.Measure("PartFAssessmentCache.Calculate", zoneCategoryName ?? "(whole model)"))
            {
                partFCalculator.Calculate(zoneCategoryName);
            }

            this.analyticalModel = analyticalModel;
            this.partFDwellingScope = partFDwellingScope;
            this.zoneCategoryName = zoneCategoryName;

            partFComplianceResults = [.. (partFCalculator.DwellingResults ?? [])
                .Where(x => x?.ComplianceResult is not null)
                .Select(x => x.ComplianceResult)];

            return partFComplianceResults;
        }
    }
}
