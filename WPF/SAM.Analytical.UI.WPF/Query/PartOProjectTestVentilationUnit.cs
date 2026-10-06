// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Query
    {
        /// <summary>
        /// The project test ventilation unit a preparation should actually run with: what the request
        /// states, or failing that what the <b>project</b> states, or failing that none.
        /// <para>
        /// The same resolution order as <see cref="PartOEquipmentSelection"/> and for the same reason - a
        /// caller that says nothing about equipment must inherit the project's own statement rather than
        /// fall through to a default. Here the consequence of getting it wrong is narrower but still bad: a
        /// preparation that dropped the project's what-if would report every dwelling assigned to it as
        /// "capacity unknown", and Iteration 2B would lose those dwellings' ceilings.
        /// </para>
        /// <para>
        /// Null is a legal and ordinary answer - most projects state no test product - so this returns
        /// null rather than an empty statement. An unusable statement is left as it is rather than
        /// discarded, so the dialog that owns it can say what is wrong with it.
        /// </para>
        /// </summary>
        internal static PartOProjectTestVentilationUnit? PartOProjectTestVentilationUnit(PartOWorkflowRequest? partOWorkflowRequest, AnalyticalModel? analyticalModel)
        {
            return partOWorkflowRequest?.ProjectTestVentilationUnit
                ?? analyticalModel?.GetValue<PartOProjectTestVentilationUnit>(Analytical.AnalyticalModelParameter.PartOProjectTestVentilationUnit);
        }

        /// <summary>
        /// How many of a model's air handling units are currently fitted with one product.
        ///
        /// <para><b>What it is for</b></para>
        /// <para>
        /// A project test product must never disappear from underneath the dwellings assigned to it: its
        /// name IS its identity, so renaming or disabling it while dwellings carry it would orphan those
        /// assignments. So the dialog that edits it needs to know, when it opens, whether anything is
        /// assigned - and this is that count. The dialog then locks the name and the enable tick and says
        /// how many dwellings are involved, rather than silently mutating an assignment.
        /// </para>
        ///
        /// <para><b>Read once, and off the model rather than the table</b></para>
        /// <para>
        /// One pass over the model's air handling units, reading each one's identity through
        /// <c>Analytical.Query.SelectedVentilationUnitReference</c> - the same accessor every other reader
        /// uses. It is a fact about the SAVED project, not about a staged edit in some other dialog, which
        /// is what makes it the right thing to lock on: the assignments that would be orphaned are the ones
        /// already written.
        /// </para>
        /// </summary>
        /// <param name="analyticalModel">The project.</param>
        /// <param name="ventilationUnitReference">The product's identity. Null or unusable counts nothing.</param>
        internal static int PartOVentilationUnitAssignmentCount(AnalyticalModel? analyticalModel, VentilationUnitReference? ventilationUnitReference)
        {
            if (analyticalModel is null || ventilationUnitReference is null || !ventilationUnitReference.IsValid)
            {
                return 0;
            }

            //Taken once: AnalyticalModel.AdjacencyCluster hands back a fresh copy on every access.
            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;

            List<AirHandlingUnit>? airHandlingUnits = adjacencyCluster?.GetObjects<AirHandlingUnit>();

            int result = 0;

            foreach (AirHandlingUnit airHandlingUnit in airHandlingUnits ?? [])
            {
                if (airHandlingUnit is null)
                {
                    continue;
                }

                //Identity, never guid: a reference is minted fresh every time one is read.
                if (ventilationUnitReference.Matches(Analytical.Query.SelectedVentilationUnitReference(airHandlingUnit)))
                {
                    result++;
                }
            }

            return result;
        }
    }
}
