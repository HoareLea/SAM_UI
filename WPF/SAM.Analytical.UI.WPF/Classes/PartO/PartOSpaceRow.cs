// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// One space's row in the Part O preparation window: what Approved Document F requires of it, and what
    /// the prepared model will actually put through it.
    /// <para>
    /// <b>The two are separate columns because they are separate quantities.</b> The Part F figure is the
    /// requirement; the design airflow is what the realized terminal network moves. They coincide on a
    /// dwelling designed to the minimum and diverge the moment anything is designed above it, so a single
    /// column would hide the only thing worth looking at.
    /// </para>
    /// <para>
    /// <b>Read back through the queries the simulation uses</b>, not off the values that were written -
    /// <c>Query.CalculatedSupplyAirFlow</c> and the internal condition's exhaust airflow, converted from
    /// m3/s to l/s for display and otherwise untouched. This is the same pair the accepted Grasshopper
    /// component reports, for the same reason: what is shown here is what the export will see.
    /// </para>
    /// <para>
    /// <b><see cref="Dwelling"/> is looked up, never inferred.</b> It is handed in, resolved once from the
    /// model's actual zone-space relationships - never from a space's name, its prefix, its index or its
    /// position in the table, all of which happen to look right on a demonstration model and are wrong on
    /// a real one. See <c>Modify.PreparePartOIteration</c>, which builds that map.
    /// </para>
    /// </summary>
    public class PartOSpaceRow
    {
        /// <summary>The column's answer where nothing resolved. An em dash, and never a blank or a guess.</summary>
        internal const string Unresolved = "—";

        /// <summary>
        /// Builds the row for one space of the prepared model.
        /// </summary>
        /// <param name="space">The space.</param>
        /// <param name="dwelling">
        /// What to call the dwelling or zone this space belongs to, as the caller resolved it from the
        /// model. Null or blank reads as <see cref="Unresolved"/> - an absence is shown as an absence.
        /// </param>
        public PartOSpaceRow(Space space, string? dwelling = null)
        {
            Name = space?.Name;

            Dwelling = string.IsNullOrWhiteSpace(dwelling) ? Unresolved : dwelling;

            //Qualified: SAM.Analytical.UI.WPF declares a Query of its own.
            PartFRequired_Lps = space?.GetValue<PartFSpaceData>(SpaceParameter.PartFSpaceData)?.ContinuousDesignFlowRate_Lps ?? double.NaN;

            DesignSupply_Lps = ToLps(Analytical.Query.CalculatedSupplyAirFlow(space));

            DesignExtract_Lps = space?.InternalCondition is not null && space.InternalCondition.TryGetValue(InternalConditionParameter.ExhaustAirFlow, out double exhaustAirFlow)
                ? ToLps(exhaustAirFlow)
                : 0;
        }

        /// <summary>The space.</summary>
        public string Name { get; }

        /// <summary>
        /// The Part O dwelling this space belongs to, or failing that the relevant zone that groups it -
        /// a communal corridor, a stair, a landlord area - or <see cref="Unresolved"/>.
        /// <para>
        /// <b>Repeated on every row on purpose.</b> Merged or blanked repeats read more tidily and filter,
        /// sort, copy and export worse, and this table is one an engineer pastes into a spreadsheet.
        /// </para>
        /// <para>
        /// <b>Presentation only.</b> Naming a dwelling here changes no zone membership, no Part O scope, no
        /// Approved Document F requirement and no airflow of any kind.
        /// </para>
        /// </summary>
        public string Dwelling { get; }

        /// <summary>
        /// What Approved Document F requires of this space [l/s], from its own
        /// <c>PartFSpaceData</c>. <see cref="double.NaN"/> where the space carries none - which is what a
        /// space that was never sized looks like, and is not zero.
        /// </summary>
        public double PartFRequired_Lps { get; }

        /// <summary>
        /// What the Part F column means for THIS space, in the words an engineer reads - and, where the
        /// space carries no requirement at all, why the cell is empty rather than zero.
        ///
        /// <para><b>It reports an absent RECORD, never a judgement about the space</b></para>
        /// <para>
        /// The only thing behind this wording is whether <see cref="PartFRequired_Lps"/> resolved - the
        /// same fact the cell itself shows - and that is the absence of a <c>PartFSpaceData</c> continuous
        /// rate on this space. It is <b>not</b> a statement that this kind of space needs no continuous
        /// mechanical airflow, because nothing here examines the space type and SAM was never asked.
        /// </para>
        /// <para>
        /// This sentence used to read "No Part F requirement for this space type", which said the second
        /// thing while only knowing the first - and the two differ exactly where it matters: a habitable
        /// room that <c>AddVent PartF</c> was never run over reaches this tooltip too, and telling an
        /// engineer its <i>type</i> has no requirement would make an accidental omission read as a
        /// deliberate exemption. The same trap the Dwelling / Zone column refuses by showing an em dash
        /// rather than a guess.
        /// </para>
        /// </summary>
        public string PartFRequiredDescription => double.IsNaN(PartFRequired_Lps) || double.IsInfinity(PartFRequired_Lps)
            ? "No continuous Part F requirement is recorded for this space."
            : string.Concat("Minimum continuous mechanical airflow required by the Part F data.", System.Environment.NewLine, "Equipment selection does not change it.");

        /// <summary>The design supply airflow [l/s] the prepared model will simulate.</summary>
        public double DesignSupply_Lps { get; }

        /// <summary>The design extract airflow [l/s] the prepared model will simulate.</summary>
        public double DesignExtract_Lps { get; }

        private static double ToLps(double value_M3s)
        {
            return double.IsNaN(value_M3s) ? double.NaN : value_M3s * 1000;
        }
    }
}
