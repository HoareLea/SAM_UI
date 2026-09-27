// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using System.Text.Json.Nodes;

namespace SAM.Analytical.UI
{
    /// <summary>
    /// The project's rules for which strategies a dwelling may have, stated once for the whole project rather than
    /// per dwelling (owner decision 3, PR0).
    ///
    /// <para><b>What they govern - and what they do not</b></para>
    /// <list type="bullet">
    /// <item>They filter the <b>suggestion</b>: a strategy the project does not allow is never suggested, however
    /// well it screened. The filter narrows the least-intervention order; it never reorders it (PR0 §D6).</item>
    /// <item>They gate <b>assignment</b>: a bulk or row assignment of a disallowed strategy is refused, and a
    /// selection made before a rule changed is flagged as needing attention rather than silently rewritten.</item>
    /// <item>They are <b>not</b> SAM authority. The materialisation decides what is buildable; these only decide
    /// what this project permits. The product pool is not duplicated here - it is the project's existing
    /// <c>PartOEquipmentSelection</c>, which the materialisation itself reads.</item>
    /// </list>
    /// </summary>
    public class PartOMixedDesignConstraints
    {
        public PartOMixedDesignConstraints()
        {
        }

        public PartOMixedDesignConstraints(PartOMixedDesignConstraints partOMixedDesignConstraints)
        {
            if (partOMixedDesignConstraints is not null)
            {
                NaturalVentilationAllowed = partOMixedDesignConstraints.NaturalVentilationAllowed;
                OptimisationAllowed = partOMixedDesignConstraints.OptimisationAllowed;
            }
        }

        /// <summary>
        /// Whether a dwelling may be naturally ventilated. False is "every dwelling must be mechanically ventilated".
        /// </summary>
        public bool NaturalVentilationAllowed { get; set; } = true;

        /// <summary>Whether a dwelling may retain an optimised (raised) design airflow.</summary>
        public bool OptimisationAllowed { get; set; } = true;

        /// <summary>
        /// Whether active cooling is allowed. Always false in this build: cooling is recorded and refused by SAM
        /// until the cooling workflow (PR3) - so it is not a setting a person can switch on here.
        /// </summary>
        public bool CoolingAllowed => false;

        /// <summary>Why a strategy is not permitted by these rules, or null where it is.</summary>
        public string Refusal(PartODwellingStrategy partODwellingStrategy)
        {
            if (partODwellingStrategy is null)
            {
                return null;
            }

            if (partODwellingStrategy.VentilationMode == PartOVentilationMode.NaturalVentilation && !NaturalVentilationAllowed)
            {
                return "The project requires mechanical ventilation, so natural ventilation is not allowed.";
            }

            if (partODwellingStrategy.DesignAirFlowBasis == PartODesignAirFlowBasis.RetainedDesign && !OptimisationAllowed)
            {
                return "The project does not allow an optimised (retained) design airflow.";
            }

            if (partODwellingStrategy.ActiveCooling == PartOActiveCooling.SupplyAirCooling && !CoolingAllowed)
            {
                return "Active cooling is not available until the cooling workflow is enabled.";
            }

            return null;
        }

        /// <summary>Whether a screening strategy may be suggested under these rules.</summary>
        public bool Allows(PartOScreeningStrategy partOScreeningStrategy)
        {
            return partOScreeningStrategy switch
            {
                PartOScreeningStrategy.Natural => NaturalVentilationAllowed,
                PartOScreeningStrategy.MechanicalBaseline => true,
                PartOScreeningStrategy.SelectedProduct => true,
                PartOScreeningStrategy.Optimised => OptimisationAllowed,
                PartOScreeningStrategy.ActiveCooling => CoolingAllowed,
                _ => false,
            };
        }

        public JsonObject ToJsonObject()
        {
            return new JsonObject
            {
                ["NaturalVentilationAllowed"] = NaturalVentilationAllowed,
                ["OptimisationAllowed"] = OptimisationAllowed,
            };
        }

        public static PartOMixedDesignConstraints Read(JsonObject jsonObject)
        {
            PartOMixedDesignConstraints result = new();

            if (jsonObject is not null)
            {
                result.NaturalVentilationAllowed = (bool?)jsonObject["NaturalVentilationAllowed"] ?? true;
                result.OptimisationAllowed = (bool?)jsonObject["OptimisationAllowed"] ?? true;
            }

            return result;
        }
    }
}
