// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Query
    {
        /// <summary>
        /// What an engineer is shown for one Iteration 3 method.
        /// <para>
        /// <b>Presentation only, and deliberately separate from the enum's own description</b>, which is
        /// written into every persisted record and report and so cannot change without changing their bytes.
        /// The internal names - Parity, B0, B4, MG, A/B - stay in code and files; none of them is shown.
        /// </para>
        /// </summary>
        public static string PartOIteration3MethodLabel(PartOIteration3BehaviourMode partOIteration3BehaviourMode)
        {
            return partOIteration3BehaviourMode switch
            {
                PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance => "Selected product — manufacturer operating guidance",
                PartOIteration3BehaviourMode.SelectedProduct => "Selected product — certified efficiency and SFP",
                PartOIteration3BehaviourMode.SelectedProductCooling => "Selected product — published cooling table",
                PartOIteration3BehaviourMode.Parity => "Route check — design airflows, no product",
                _ => partOIteration3BehaviourMode.ToString(),
            };
        }

        /// <summary>One or two plain sentences: what the system case changes, and when to use it.</summary>
        public static string PartOIteration3MethodExplanation(PartOIteration3BehaviourMode partOIteration3BehaviourMode)
        {
            return partOIteration3BehaviourMode switch
            {
                PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance =>
                    "Each dwelling's selected unit is run the way its manufacturer says it operates: heat exchanger then cooling coil, cooling switched by the room stat, the higher cooling airflow and the stated minimum supply temperature. Manufacturer guidance - provisional, not certified performance.",
                PartOIteration3BehaviourMode.SelectedProduct =>
                    "Each dwelling's selected unit is run with its certified heat-recovery efficiency and specific fan power. No cooling.",
                PartOIteration3BehaviourMode.SelectedProductCooling =>
                    "Validation option. The route check plus the selected product's published cooling table, run as recirculation. For products with published cooling data but no manufacturer operating guidance.",
                PartOIteration3BehaviourMode.Parity =>
                    "Validation option. The reference case's own design airflows as an explicit system, with no product and no fan heat. Shows how closely the explicit system model reproduces the reference case.",
                _ => string.Empty,
            };
        }

        /// <summary>
        /// A product as an engineer reads it: the catalogue reference, plus the cooling module where the
        /// reference does not already name it (a reference such as "Nuaire X (MR-ECO-COOL-V)" does).
        /// </summary>
        public static string PartOIteration3ProductText(string reference, string coolingModuleModel)
        {
            if (string.IsNullOrWhiteSpace(coolingModuleModel))
            {
                return reference;
            }

            if (string.IsNullOrWhiteSpace(reference))
            {
                return coolingModuleModel;
            }

            return reference.IndexOf(coolingModuleModel, System.StringComparison.OrdinalIgnoreCase) >= 0
                ? reference
                : string.Format("{0} + {1}", reference, coolingModuleModel);
        }

        /// <summary>Whether the method is a validation option, offered under Advanced rather than first.</summary>
        public static bool IsPartOIteration3ValidationMethod(PartOIteration3BehaviourMode partOIteration3BehaviourMode)
        {
            return partOIteration3BehaviourMode == PartOIteration3BehaviourMode.Parity || partOIteration3BehaviourMode == PartOIteration3BehaviourMode.SelectedProductCooling;
        }

        /// <summary>Whether the method runs each unit's selected product, so needs one on every unit in scope.</summary>
        public static bool IsPartOIteration3ProductMethod(PartOIteration3BehaviourMode partOIteration3BehaviourMode)
        {
            return partOIteration3BehaviourMode != PartOIteration3BehaviourMode.Parity;
        }

        /// <summary>
        /// Which earlier iteration a run is, in the engineer's own terms - what the Iteration 3 reference case
        /// is called. An MVHR run prepared with the manufacturer catalogue is Iteration 2; without it, 1a.
        /// </summary>
        public static string PartOIterationText(PartORun partORun)
        {
            PartOPreparationContext partOPreparationContext = partORun?.PreparationContext;

            if (partOPreparationContext is null)
            {
                return "Earlier Part O iteration";
            }

            return partOPreparationContext.PartOIteration switch
            {
                //As the run records it; a saved run from before that was recorded cannot tell 1a from 2.
                PartOIteration.BasePassive => partOPreparationContext.VentilationUnitCatalogueOffered switch
                {
                    true => "Iteration 2 — MVHR with manufacturer unit",
                    false => "Iteration 1a — baseline",
                    null => "MVHR iteration (1a or 2, not recorded by this saved run)",
                },
                PartOIteration.BaseNaturalVentilation => "Iteration 1b — natural ventilation",
                _ => Core.Query.Description(partOPreparationContext.PartOIteration),
            };
        }
    }
}
