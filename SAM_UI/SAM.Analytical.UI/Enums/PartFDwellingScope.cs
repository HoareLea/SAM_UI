// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.ComponentModel;

namespace SAM.Analytical.UI
{
    /// <summary>
    /// What a saved view treats as its dwellings, stated explicitly so that "nobody has decided yet" cannot
    /// be mistaken for "the whole building is one dwelling".
    /// <para>
    /// <b>Why this exists.</b> <c>PartFAirflowViewSettings.ZoneCategoryName</c> alone could not say the
    /// difference. A blank name reached <c>PartFCalculator.Calculate(string)</c>, which reads null or blank
    /// as single-house mode, so a view of a block of flats whose category had not been chosen yet would
    /// have been assessed and drawn as ONE dwelling - a confident, wrong engineering drawing produced while
    /// waiting for the user to answer a question. Three different situations shared that one blank: no
    /// dwelling-zone structure at all, several possible categories, and zones with no dwelling among them.
    /// Only the first of those is whole-house mode.
    /// </para>
    /// <para>
    /// A scope and not a regulatory value: it says which dwellings a drawing is about, exactly as
    /// <see cref="PartFDwellingFilter"/> does. Nothing calculated from it is stored in the view.
    /// </para>
    /// </summary>
    public enum PartFDwellingScope
    {
        /// <summary>
        /// Not decided. <b>Nothing is assessed and nothing is drawn</b> - see
        /// <c>PartFAirflowViewSettings.HasDwellingScope</c>. The default, which is the safe direction: a
        /// drawing that has not been told what it is about must not guess.
        /// </summary>
        [Description("Not chosen")] Undefined,

        /// <summary>
        /// The whole model is one dwelling - the calculation's single-house mode. Correct for a house that
        /// was never zoned, and it has to be CHOSEN, whether by the preset on a model with no zones at all
        /// or by a person in the dialog.
        /// </summary>
        [Description("Whole model as one dwelling")] WholeModel,

        /// <summary>
        /// The dwellings are the zones of <c>PartFAirflowViewSettings.ZoneCategoryName</c>. A blank name
        /// alongside this is still undecided, not whole-house.
        /// </summary>
        [Description("Dwelling zone category")] ZoneCategory,
    }
}
