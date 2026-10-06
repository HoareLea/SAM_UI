// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical;
using SAM.Analytical.Enums;
using SAM.Analytical.UI.WPF.Tests.Helpers;
using System;
using System.Collections.Generic;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// The assessment the saved 2D views draw from, and the one assumption caching it rests on.
    /// <para>
    /// A Part F view stores no result - it stores how to present one - and the numbers are re-read from the
    /// model every time the view is drawn. Caching that re-read is the single place where an engineering
    /// value is held between draws, so the question "can a cached assessment outlive the model it was made
    /// from" has to be answered by a test rather than by an argument. It is answered without any revision
    /// counter or dirty flag: <c>UIAnalyticalModel</c> hands out a fresh <c>AnalyticalModel</c> on every
    /// read, the cache is keyed on that instance, and an edit therefore cannot be answered from it.
    /// </para>
    /// </summary>
    public class PartFAssessmentCacheTests
    {
        // ------------------------------------------------------------------
        // The cache is real
        // ------------------------------------------------------------------

        /// <summary>
        /// The same model and the same scope are answered from the cache. Stated first, because every
        /// invariant below would hold vacuously of something that never cached anything.
        /// </summary>
        [Fact]
        public void SameModelAndScope_IsAnsweredFromTheCache()
        {
            AnalyticalModel analyticalModel = Model();

            PartFAssessmentCache partFAssessmentCache = Cache();

            List<PartFComplianceResult> partFComplianceResults = partFAssessmentCache.Results(analyticalModel, ViewSettings());

            Assert.Equal(2, partFComplianceResults.Count);
            Assert.Same(partFComplianceResults, partFAssessmentCache.Results(analyticalModel, ViewSettings()));
        }

        /// <summary>
        /// A change of scope on one model is a different question and gets a different answer. The whole
        /// model as one dwelling is one assessment where the two flats were two.
        /// </summary>
        [Fact]
        public void ScopeChange_IsNotAnsweredFromTheCache()
        {
            AnalyticalModel analyticalModel = Model();

            PartFAssessmentCache partFAssessmentCache = Cache();

            Assert.Equal(2, partFAssessmentCache.Results(analyticalModel, ViewSettings()).Count);

            PartFAirflowViewSettings partFAirflowViewSettings = ViewSettings();

            partFAirflowViewSettings.DwellingScope = PartFDwellingScope.WholeModel;
            partFAirflowViewSettings.ZoneCategoryName = null;

            Assert.Single(partFAssessmentCache.Results(analyticalModel, partFAirflowViewSettings));
        }

        // ------------------------------------------------------------------
        // The invariant the cache rests on
        // ------------------------------------------------------------------

        /// <summary>
        /// <b>The assumption, stated on its own.</b> Every read of the model hands back a different
        /// instance, because the getter deep-clones. That is what makes an instance key a sound one: there
        /// is no way to edit the building and be handed the same object back, so there is no way to be
        /// answered from a cache made before the edit.
        /// <para>
        /// Cheap on purpose. The alternative - a revision number on the model, maintained by every edit
        /// path - would be a new framework to keep correct, for an invariant this assertion already holds
        /// in one line.
        /// </para>
        /// </summary>
        [Fact]
        public void UIAnalyticalModel_HandsOutANewModelOnEveryRead()
        {
            UIAnalyticalModel uIAnalyticalModel = new(Model());

            Assert.NotSame(uIAnalyticalModel.JSAMObject, uIAnalyticalModel.JSAMObject);
        }

        /// <summary>
        /// <b>The invariant itself, end to end.</b> A model edit that changes what Part F assesses is
        /// followed by an assessment that reflects the edit - through the real <c>UIAnalyticalModel</c>, the
        /// real cache and the real calculator, with the cache proved warm on the first model first.
        /// <para>
        /// The edit removes a dwelling zone, so the change is visible in the result and not only in a
        /// reference: two dwellings assessed before it, one after. A stale cache would report two.
        /// </para>
        /// </summary>
        [Fact]
        public void ModelEdit_CannotReuseTheCachedAssessment()
        {
            UIAnalyticalModel uIAnalyticalModel = new(Model());

            PartFAssessmentCache partFAssessmentCache = Cache();

            AnalyticalModel analyticalModel = uIAnalyticalModel.JSAMObject;

            List<PartFComplianceResult> partFComplianceResults = partFAssessmentCache.Results(analyticalModel, ViewSettings());

            Assert.Equal(2, partFComplianceResults.Count);

            //Warm: the assessment now sitting in the cache is the two-dwelling one.
            Assert.Same(partFComplianceResults, partFAssessmentCache.Results(analyticalModel, ViewSettings()));

            //The edit. One of the two flats stops being a dwelling zone, which is a Part F input by any
            //reading: it decides what is assessed and what each dwelling's rates are calculated over.
            AnalyticalModel analyticalModel_Edited = uIAnalyticalModel.JSAMObject;

            AdjacencyCluster adjacencyCluster = analyticalModel_Edited.AdjacencyCluster;

            Zone zone = adjacencyCluster.GetZones()?.Find(x => x is not null && x.Name == "Flat 2");

            Assert.NotNull(zone);
            Assert.True(adjacencyCluster.RemoveObject(zone));

            uIAnalyticalModel.JSAMObject = new AnalyticalModel(analyticalModel_Edited, adjacencyCluster);

            //And the drawing gets the edited building, not the one it was showing a moment ago.
            List<PartFComplianceResult> partFComplianceResults_Edited = partFAssessmentCache.Results(uIAnalyticalModel.JSAMObject, ViewSettings());

            Assert.Single(partFComplianceResults_Edited);
            Assert.NotSame(partFComplianceResults, partFComplianceResults_Edited);
        }

        // ------------------------------------------------------------------
        // The gate
        // ------------------------------------------------------------------

        /// <summary>
        /// An undecided scope is assessed as nothing, and nothing is cached either - so choosing a scope and
        /// asking again is answered by a calculation and not by the empty list left behind.
        /// </summary>
        [Fact]
        public void UndecidedScope_IsNotAssessedAndIsNotCached()
        {
            AnalyticalModel analyticalModel = Model();

            PartFAssessmentCache partFAssessmentCache = Cache();

            Assert.Empty(partFAssessmentCache.Results(analyticalModel, new PartFAirflowViewSettings() { Enabled = true }));

            Assert.Equal(2, partFAssessmentCache.Results(analyticalModel, ViewSettings()).Count);
        }

        /// <summary>
        /// A category scope with no category named is undecided too. It is not a combination the dialog
        /// produces, and reading it as whole-house would be the original ambiguity by another route.
        /// </summary>
        [Fact]
        public void CategoryScopeWithNoCategory_IsNotAssessed()
        {
            Assert.Empty(Cache().Results(Model(), new PartFAirflowViewSettings()
            {
                Enabled = true,
                DwellingScope = PartFDwellingScope.ZoneCategory,
                ZoneCategoryName = null,
            }));
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        /// <summary>Two flats and a communal corridor, the corridor in no dwelling zone.</summary>
        private static AnalyticalModel Model()
        {
            PartFPlanModel partFPlanModel = new PartFPlanModel()
                .Room("Studio", 8)
                .Room("Bathroom", 3)
                .Room("Corridor", 2)
                .Room("Bedroom", 7)
                .Room("Kitchen", 5)
                .Partition("Studio", "Bathroom", "D01")
                .Partition("Bathroom", "Corridor")
                .Partition("Corridor", "Bedroom")
                .Partition("Bedroom", "Kitchen", "D02")
                .Zone("Flat 1", "Flats", true, "Studio", "Bathroom")
                .Zone("Flat 2", "Flats", true, "Bedroom", "Kitchen")
                .LocalExtractMethod("Studio", PartFExtractMethod.MVHRContinuousTerminal)
                .LocalExtractMethod("Kitchen", PartFExtractMethod.MVHRContinuousTerminal);

            return new AnalyticalModel("Block", null, null, null, partFPlanModel.AdjacencyCluster);
        }

        /// <summary>A saved view scoped to the model's one dwelling category, as the preset leaves it.</summary>
        private static PartFAirflowViewSettings ViewSettings()
        {
            return new PartFAirflowViewSettings()
            {
                Enabled = true,
                DwellingScope = PartFDwellingScope.ZoneCategory,
                ZoneCategoryName = "Flats",
            };
        }

        /// <summary>
        /// The cache, calculating with the shipped rule set rather than the installed one, which a test
        /// machine need not have.
        /// </summary>
        private static PartFAssessmentCache Cache()
        {
            return new PartFAssessmentCache(() => new PartFCalculator(Analytical.Create.PartFData(RuleSetPath())));
        }

        /// <summary>
        /// The shipped Part F rule set, found relative to this repository rather than copied into the test
        /// output: a stale copy of a rule set is exactly the kind of drift these tests exist to catch.
        /// </summary>
        private static string RuleSetPath()
        {
            System.IO.DirectoryInfo directoryInfo = new(AppDomain.CurrentDomain.BaseDirectory);

            while (directoryInfo is not null)
            {
                string path = System.IO.Path.Combine(directoryInfo.FullName, "SAM", "files", "resources", "Analytical", "SAM_PartFSpaceRulesUKDwellingsMVHR.json");
                if (System.IO.File.Exists(path))
                {
                    return path;
                }

                directoryInfo = directoryInfo.Parent;
            }

            throw new System.IO.FileNotFoundException("The shipped Part F rule set was not found above the test output directory.");
        }
    }
}
