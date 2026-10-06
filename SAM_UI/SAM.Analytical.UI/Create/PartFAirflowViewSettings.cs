// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using System.Collections.Generic;

namespace SAM.Analytical.UI
{
    public static partial class Create
    {
        /// <summary>
        /// The Part F airflow preset a NEW view gets when its colour scheme is set to Part F data: a usable
        /// Part F drawing straight away.
        /// <para>
        /// The point is that an engineer who asks for a Part F view should get one. Before this, they had to
        /// know that the colour scheme was only half of it and that nine more options were behind a separate
        /// dialog - so the obvious action produced a coloured plan with no airflow on it and no hint that
        /// anything was missing.
        /// </para>
        /// <para>
        /// <b>Only ever applied when a view is created.</b> It is never applied to a view that already exists,
        /// never re-applied when somebody reopens View Settings, and never applied to a duplicate - a
        /// duplicated view keeps the presentation of the view it came from, including any label somebody
        /// moved. This builds a preset; deciding that a preset is wanted is the caller's job.
        /// </para>
        /// </summary>
        /// <param name="adjacencyCluster">
        /// The model, for resolving which zone category holds the dwellings. See
        /// <see cref="Analytical.Query.PartFDwellingZoneCategories"/>.
        /// </param>
        /// <returns>
        /// Settings with the annotation on, everything visible, at 1:50 and the continuous design condition,
        /// scoped where the model says unambiguously what the drawing is about and left explicitly undecided
        /// where it does not. See <see cref="DwellingScope(AdjacencyCluster, out string)"/>.
        /// </returns>
        public static PartFAirflowViewSettings PartFAirflowViewSettings(AdjacencyCluster adjacencyCluster)
        {
            PartFDwellingScope partFDwellingScope = DwellingScope(adjacencyCluster, out string zoneCategoryName);

            return new PartFAirflowViewSettings()
            {
                //On even where the scope is undecided, and that is safe: the switch says this view WANTS Part
                //F annotation, and PartFAirflowViewSettings.HasDwellingScope says whether SAM knows enough to
                //calculate any. The scope gate is what prevents an assessment, not this. Keeping the switch
                //on is also what makes the preset worth having - choosing the dwellings is then the single
                //remaining action, rather than a choice followed by remembering to turn the overlay back on.
                Enabled = true,

                DwellingScope = partFDwellingScope,
                ZoneCategoryName = zoneCategoryName,

                //The Approved Document F sizing case, which is the condition a drawing is normally issued at.
                OperatingMode = PartFOperatingMode.ContinuousDesign,

                //A floor plan is a drawing of a floor: every dwelling on it, not one flat with the rest blank.
                DwellingFilter = PartFDwellingFilter.AllDwellingsOnLevel,
                DwellingGuid = System.Guid.Empty,

                AnnotationScale = PartFTagPlacement.DefaultAnnotationScale,

                //Everything on. A drawing that silently omitted a dwelling's extract would be worse than a
                //crowded one, and an engineer can always turn a layer off; they cannot turn on something they
                //do not know is there.
                ShowSupply = true,
                ShowGeneralExtract = true,
                ShowLocalKitchenExtract = true,
                ShowTransfer = true,
                ShowUnresolved = true,
                ShowValues = true,
                ShowCompliance = true,
                ShowDoorRequirements = true,
                ShowContextGeometry = true,
            };
        }

        /// <summary>
        /// What a new Part F view reports on, resolved from the model rather than assumed - nothing here
        /// knows the word "Flats". Four situations, and they are NOT the same answer:
        /// <list type="number">
        /// <item><b>Exactly one dwelling category</b> - selected automatically. The only thing a person
        /// could do here is retype what the model already says.</item>
        /// <item><b>No zones at all</b> - whole-model single-house mode. A house that was never zoned, and
        /// the choice is still recorded as a choice rather than left as a blank that reads like one.</item>
        /// <item><b>Several dwelling categories</b> - left undecided, ON PURPOSE. Which flats a drawing
        /// reports on is an engineering decision, and guessing it would produce a confident drawing of the
        /// wrong half of a mixed-use building.</item>
        /// <item><b>Zones, but no dwelling among them</b> - left undecided. Something is wrong with the
        /// model, typically no zone marked Is Dwelling; falling back to whole-house here would assess a
        /// whole block as one dwelling because of a missing parameter, and report it as a result.</item>
        /// </list>
        /// <para>
        /// Cases 3 and 4 are why this returns a scope rather than a name. Both once produced a null
        /// category, indistinguishable from case 2, which the calculation reads as single-house mode.
        /// </para>
        /// </summary>
        /// <param name="zoneCategoryName">
        /// The category resolved in case 1, otherwise null.
        /// </param>
        private static PartFDwellingScope DwellingScope(AdjacencyCluster adjacencyCluster, out string zoneCategoryName)
        {
            zoneCategoryName = null;

            List<string> names = adjacencyCluster?.PartFDwellingZoneCategories() ?? [];

            if (names.Count == 1)
            {
                zoneCategoryName = names[0];

                return PartFDwellingScope.ZoneCategory;
            }

            if (names.Count != 0)
            {
                return PartFDwellingScope.Undefined;
            }

            //No dwelling category. Whether that is a house or a broken model is decided by whether the model
            //is zoned at all: an unzoned model has nothing to say about dwellings and is one; a zoned model
            //has been asked and has answered that none of its zones is a dwelling, which is not a house.
            List<Zone> zones = adjacencyCluster?.GetZones();

            return zones is null || zones.Count == 0 ? PartFDwellingScope.WholeModel : PartFDwellingScope.Undefined;
        }
    }
}
