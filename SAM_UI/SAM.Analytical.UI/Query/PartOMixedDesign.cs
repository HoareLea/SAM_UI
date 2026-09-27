// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.UI
{
    public static partial class Query
    {
        /// <summary>
        /// The screening strategies in the least-intervention order (PR0 §D6). A later strategy is appended here and
        /// nowhere else; the screening window and the matrix columns are built from this list.
        /// </summary>
        public static List<PartOScreeningStrategy> PartOScreeningStrategies()
        {
            return
            [
                PartOScreeningStrategy.Natural,
                PartOScreeningStrategy.MechanicalBaseline,
                PartOScreeningStrategy.SelectedProduct,
                PartOScreeningStrategy.Optimised,
                PartOScreeningStrategy.ActiveCooling,
            ];
        }

        /// <summary>The engineering name - the primary label everywhere. Never an iteration number.</summary>
        public static string PartOScreeningStrategyLabel(PartOScreeningStrategy partOScreeningStrategy)
        {
            return partOScreeningStrategy == PartOScreeningStrategy.Undefined ? "—" : Core.Query.Description(partOScreeningStrategy);
        }

        /// <summary>
        /// Secondary, traceability-only text: which existing Part O case the strategy corresponds to. Shown as detail,
        /// never as the strategy's identity.
        /// </summary>
        public static string PartOScreeningStrategyDetail(PartOScreeningStrategy partOScreeningStrategy)
        {
            return partOScreeningStrategy switch
            {
                PartOScreeningStrategy.Natural => "Every screened dwelling naturally ventilated, as Iteration 1b.",
                PartOScreeningStrategy.MechanicalBaseline => "Every screened dwelling with MVHR at its Approved Document F requirement and a generic unit, as Iteration 1a.",
                PartOScreeningStrategy.SelectedProduct => "Every screened dwelling with MVHR and a product selected from the project's pool, as Iteration 2.",
                PartOScreeningStrategy.Optimised => "MVHR with an optimised, retained design airflow, as Iteration 2B.",
                PartOScreeningStrategy.ActiveCooling => "MVHR with active supply-air cooling, as Iteration 3.",
                _ => null,
            };
        }

        /// <summary>
        /// Why a strategy cannot be screened in this build or for this project, or null where it can.
        /// </summary>
        /// <param name="catalogueHasProducts">Whether the ventilation unit catalogue offers any selectable product.</param>
        public static string PartOScreeningStrategyUnavailable(PartOScreeningStrategy partOScreeningStrategy, bool catalogueHasProducts)
        {
            return partOScreeningStrategy switch
            {
                PartOScreeningStrategy.Natural => null,
                PartOScreeningStrategy.MechanicalBaseline => null,
                PartOScreeningStrategy.SelectedProduct => catalogueHasProducts ? null : "No selectable product is in the ventilation unit catalogue, so there is nothing to select.",
                PartOScreeningStrategy.Optimised => "Airflow optimisation is not yet connected to mixed-design screening. A retained design can still be selected for a dwelling whose baseline carries its design terminals.",
                PartOScreeningStrategy.ActiveCooling => "Available after the cooling workflow is enabled.",
                _ => "Not a screening strategy.",
            };
        }

        /// <summary>Whether a screening run of this strategy offers the product catalogue to the materialisation.</summary>
        public static bool PartOScreeningCatalogueOffered(PartOScreeningStrategy partOScreeningStrategy)
        {
            return partOScreeningStrategy == PartOScreeningStrategy.SelectedProduct;
        }

        /// <summary>
        /// The one strategy a screening run gives every dwelling it assesses, or null where the strategy is not
        /// screenable. MVHR baseline and selected-product MVHR state the same intent - MVHR, product from the pool -
        /// and differ only in whether the catalogue is offered (<see cref="PartOScreeningCatalogueOffered"/>), exactly
        /// as Iterations 1a and 2 differ.
        /// </summary>
        public static PartODwellingStrategy PartOScreeningDwellingStrategy(PartOScreeningStrategy partOScreeningStrategy, Guid guid_Zone)
        {
            return partOScreeningStrategy switch
            {
                PartOScreeningStrategy.Natural => new PartODwellingStrategy(guid_Zone, PartOVentilationMode.NaturalVentilation),
                PartOScreeningStrategy.MechanicalBaseline => new PartODwellingStrategy(guid_Zone, PartOVentilationMode.MVHR),
                PartOScreeningStrategy.SelectedProduct => new PartODwellingStrategy(guid_Zone, PartOVentilationMode.MVHR),
                _ => null,
            };
        }

        /// <summary>
        /// The homogeneous strategy set a screening run materialises: this strategy for every dwelling in scope, and
        /// nothing for any other.
        /// </summary>
        public static PartODwellingStrategySet PartOScreeningStrategySet(PartOScreeningStrategy partOScreeningStrategy, IEnumerable<Guid> guids_Zone)
        {
            PartODwellingStrategySet result = new();

            foreach (Guid guid_Zone in guids_Zone ?? [])
            {
                PartODwellingStrategy partODwellingStrategy = PartOScreeningDwellingStrategy(partOScreeningStrategy, guid_Zone);
                if (partODwellingStrategy is null)
                {
                    return null;
                }

                result.Set(partODwellingStrategy);
            }

            return result;
        }

        /// <summary>
        /// The baseline's model fingerprint with the selected strategy set left out - what screening evidence is bound
        /// to. Selecting or editing strategies therefore never makes screening stale; every other change to the model
        /// does. SAM's own digest (<see cref="SimulationResultProvenance.Fingerprint"/>) over a copy - the copy
        /// constructor clones the parameter sets, so removing from it cannot reach the baseline.
        /// </summary>
        public static string PartOScreeningDesignFingerprint(AnalyticalModel analyticalModel)
        {
            if (analyticalModel is null)
            {
                return null;
            }

            AnalyticalModel analyticalModel_Design = new(analyticalModel);
            analyticalModel_Design.RemoveValue(Analytical.AnalyticalModelParameter.PartODwellingStrategies);

            return SimulationResultProvenance.Fingerprint(analyticalModel_Design);
        }

        /// <summary>
        /// SAM's catalogue fingerprint for what a materialisation of this model would be offered - the descriptors plus
        /// the project test product the model states.
        /// </summary>
        public static string PartOMixedCatalogueFingerprint(AnalyticalModel analyticalModel, IEnumerable<VentilationUnitCapacityDescriptor> ventilationUnitCapacityDescriptors)
        {
            List<VentilationUnitCapacityDescriptor> ventilationUnitCapacityDescriptors_ProjectTest = analyticalModel?.GetValue<PartOProjectTestVentilationUnit>(Analytical.AnalyticalModelParameter.PartOProjectTestVentilationUnit)?.CapacityDescriptors();

            return Analytical.Query.PartOCatalogueFingerprint(ventilationUnitCapacityDescriptors, ventilationUnitCapacityDescriptors_ProjectTest);
        }

        /// <summary>
        /// A selected strategy in engineering words, for a row: "Natural ventilation", "MVHR · automatic product",
        /// "MVHR · &lt;product&gt;", "Optimised MVHR (retained design)", with cooling added where recorded.
        /// </summary>
        public static string PartODwellingStrategyText(PartODwellingStrategy partODwellingStrategy)
        {
            if (partODwellingStrategy is null)
            {
                return "Not selected";
            }

            if (!partODwellingStrategy.IsValid)
            {
                return "Invalid selection";
            }

            string result;
            if (partODwellingStrategy.VentilationMode == PartOVentilationMode.NaturalVentilation)
            {
                result = "Natural ventilation";
            }
            else
            {
                VentilationUnitReference ventilationUnitReference = partODwellingStrategy.VentilationUnitReference;

                string product = ventilationUnitReference is null ? "automatic product" : ventilationUnitReference.ToString();

                result = partODwellingStrategy.DesignAirFlowBasis == PartODesignAirFlowBasis.RetainedDesign
                    ? string.Format("Optimised MVHR (retained design) · {0}", product)
                    : string.Format("MVHR · {0}", product);
            }

            if (partODwellingStrategy.ActiveCooling == PartOActiveCooling.SupplyAirCooling)
            {
                result += " · active cooling (not yet available)";
            }

            return result;
        }

        /// <summary>
        /// The least-intervention suggestion for one dwelling (PR0 §D6), from current screening evidence and the
        /// project constraints.
        ///
        /// <para><b>The rule</b></para>
        /// <list type="number">
        /// <item>Walk <see cref="PartOScreeningStrategies"/> in order - Natural, MVHR baseline, selected-product MVHR,
        /// optimised MVHR, cooling.</item>
        /// <item>A strategy the constraints do not allow is passed over, and the reason says so where it had in fact
        /// passed. The constraints narrow the order; they never reorder it.</item>
        /// <item>The first strategy whose evidence is a simulated <see cref="PartODwellingOutcome.Pass"/> for this
        /// dwelling is suggested. NOT RUN, NOT ASSESSED and UNAVAILABLE are never read as a pass, and stale evidence is
        /// never handed in (the caller filters it with <see cref="PartOScreeningEvidence.IsCurrent"/>).</item>
        /// </list>
        /// <para>
        /// Not "lowest iteration": 1a and 1b are alternatives, and the order is intervention, not numbering.
        /// </para>
        /// </summary>
        /// <param name="evidence_Current">Screening evidence that is current for the baseline in use - never stale evidence.</param>
        public static PartODwellingSuggestion PartODwellingSuggestion(Guid guid_Zone, IEnumerable<PartOScreeningEvidence> evidence_Current, PartOMixedDesignConstraints partOMixedDesignConstraints)
        {
            partOMixedDesignConstraints ??= new PartOMixedDesignConstraints();

            Dictionary<PartOScreeningStrategy, PartOScreeningEvidence> dictionary = [];
            foreach (PartOScreeningEvidence partOScreeningEvidence in evidence_Current ?? [])
            {
                if (partOScreeningEvidence is not null)
                {
                    dictionary[partOScreeningEvidence.Strategy] = partOScreeningEvidence;
                }
            }

            if (dictionary.Count == 0)
            {
                return new PartODwellingSuggestion(PartOScreeningStrategy.Undefined, null, "Not screened.");
            }

            List<string> passedOver = [];
            bool assessed = false;

            foreach (PartOScreeningStrategy partOScreeningStrategy in PartOScreeningStrategies())
            {
                if (!dictionary.TryGetValue(partOScreeningStrategy, out PartOScreeningEvidence partOScreeningEvidence))
                {
                    continue;
                }

                PartODwellingOutcome partODwellingOutcome = partOScreeningEvidence.Outcome(guid_Zone);
                if (partODwellingOutcome == PartODwellingOutcome.Pass || partODwellingOutcome == PartODwellingOutcome.Fail || partODwellingOutcome == PartODwellingOutcome.NotAssessed)
                {
                    assessed = true;
                }

                if (partODwellingOutcome != PartODwellingOutcome.Pass)
                {
                    continue;
                }

                if (!partOMixedDesignConstraints.Allows(partOScreeningStrategy))
                {
                    passedOver.Add(string.Format("{0} passed but {1}", PartOScreeningStrategyLabel(partOScreeningStrategy), ConstraintText(partOScreeningStrategy)));
                    continue;
                }

                PartODwellingStrategy partODwellingStrategy = PartOScreeningDwellingStrategy(partOScreeningStrategy, guid_Zone);

                string reason = string.Format("{0} passed screening.", PartOScreeningStrategyLabel(partOScreeningStrategy));
                if (passedOver.Count != 0)
                {
                    reason = string.Format("{0}; {1}", string.Join("; ", passedOver), reason);
                }

                return new PartODwellingSuggestion(partOScreeningStrategy, partODwellingStrategy, reason);
            }

            if (passedOver.Count != 0)
            {
                return new PartODwellingSuggestion(PartOScreeningStrategy.Undefined, null, string.Format("{0}; no other screened strategy passed.", string.Join("; ", passedOver)));
            }

            return new PartODwellingSuggestion(PartOScreeningStrategy.Undefined, null, assessed ? "No screened strategy passed." : "Not screened.");
        }

        private static string ConstraintText(PartOScreeningStrategy partOScreeningStrategy)
        {
            return partOScreeningStrategy switch
            {
                PartOScreeningStrategy.Natural => "the project requires mechanical ventilation",
                PartOScreeningStrategy.Optimised => "the project does not allow an optimised design airflow",
                PartOScreeningStrategy.ActiveCooling => "active cooling is not yet available",
                _ => "the project does not allow it",
            };
        }
    }
}
