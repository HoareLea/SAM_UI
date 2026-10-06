// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.ComponentModel;
using SAM.Core.Attributes;

namespace SAM.Analytical.UI
{
    /// <summary>
    /// Analytical parameters carried by a saved view.
    /// <para>
    /// A SECOND parameter enum associated with <see cref="Geometry.UI.ViewSettings"/>, alongside the one
    /// in <c>SAM.Geometry.UI</c>. That is deliberate and is the established pattern here:
    /// <see cref="AnalyticalModelParameter"/> already exists twice for exactly this reason, once in
    /// <c>SAM.Analytical</c> and once in this assembly, and it is how <c>UIGeometrySettings</c> attaches
    /// to an <c>AnalyticalModel</c>.
    /// </para>
    /// <para>
    /// It exists because the dependency runs one way. <c>SAM.Geometry.UI</c> knows nothing about
    /// <c>SAM.Analytical</c>, so a Part F settings type cannot be named from the geometry layer's own
    /// enum. Associating a second enum from this layer keeps the reference pointing the right way and
    /// leaves the geometry layer unaware of Part F, which is what allows the airflow overlay to be an
    /// optional analytical concern rather than something baked into every view.
    /// </para>
    /// <para>
    /// Parameters are stored by their <c>ParameterProperties</c> NAME, so a name here must not collide
    /// with one on the geometry layer's enum. "Part F Airflow" does not.
    /// </para>
    /// <para>
    /// The TYPE name deliberately does not mirror the geometry layer's <c>ViewSettingsParameter</c>, even
    /// though <see cref="AnalyticalModelParameter"/> mirrors its own counterpart. Both enums here are used
    /// heavily in the same files in <c>SAM.Analytical.UI.WPF</c>, which imports both namespaces, so an
    /// identical type name shadows the geometry one and silently breaks every existing <c>Group</c> and
    /// <c>UseDefaultName</c> call site. That was not theoretical - it broke seven of them.
    /// </para>
    /// </summary>
    [AssociatedTypes(typeof(Geometry.UI.ViewSettings)), Description("ViewSettings Parameter")]
    public enum AnalyticalViewSettingsParameter
    {
        /// <summary>
        /// How this view presents the Part F airflow overlay. Presentation only: no flow rate, no
        /// compliance status and no terminal is stored here. Absent means the overlay is off, which is
        /// what every view saved before the overlay existed will correctly report.
        /// </summary>
        [ParameterProperties("Part F Airflow", "Part F airflow overlay presentation settings for this view"), SAMObjectParameterValue(typeof(PartFAirflowViewSettings))] PartFAirflow,

        /// <summary>
        /// How this view presents the Ventilation Design floor-plan overlay - the design supply/extract
        /// duty read from <c>VentilationTerminal.DesignFlowRate_Lps</c>, separate from and orthogonal to
        /// <see cref="PartFAirflow"/>. Presentation only: no flow rate is stored here. Absent means the
        /// overlay is off.
        /// </summary>
        [ParameterProperties("Ventilation Design Airflow", "Ventilation Design floor-plan overlay presentation settings for this view"), SAMObjectParameterValue(typeof(DesignAirFlowViewSettings))] VentilationDesignAirflow,
    }
}
