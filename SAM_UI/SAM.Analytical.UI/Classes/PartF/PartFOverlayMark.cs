// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using SAM.Geometry.Planar;
using System;

namespace SAM.Analytical.UI
{
    /// <summary>
    /// One Part F airflow mark on a floor plan: what kind of air it is, what it belongs to, where it goes
    /// and what it says.
    /// <para>
    /// A view model, not a calculation. Every value on it was read from the calculated
    /// <see cref="PartFComplianceResult"/> by <see cref="PartFFloorPlanOverlay"/>; nothing here is
    /// derived, and no drawing code may derive a rate of its own. The floor plan and the text schematic
    /// read the same numbers, so they cannot disagree.
    /// </para>
    /// </summary>
    public class PartFOverlayMark
    {
        /// <summary>
        /// Which air it is, which fixes its colour, line pattern and symbol through
        /// <see cref="PartFAirflowAppearance"/>. Never chosen in the drawing code.
        /// </summary>
        public PartFAirflowAppearance.AirType AirType { get; set; } = PartFAirflowAppearance.AirType.TransferAir;

        /// <summary>
        /// The terminal role this mark represents, or <see cref="PartFTerminalRole.Undefined"/> for a
        /// transfer mark.
        /// </summary>
        public PartFTerminalRole TerminalRole { get; set; } = PartFTerminalRole.Undefined;

        /// <summary>
        /// The space this mark belongs to. For a transfer mark, the space the air flows FROM.
        /// </summary>
        public Guid SpaceGuid { get; set; } = Guid.Empty;

        /// <summary>Name of <see cref="SpaceGuid"/>, so a label reads without resolving guids.</summary>
        public string SpaceName { get; set; }

        /// <summary>
        /// The stable identity a manual position for this mark's label is stored against, from
        /// <see cref="PartFAnnotationKey"/>.
        /// <para>
        /// <b>Not the terminal's or the route's own guid.</b> Both are generated afresh by every
        /// calculation, so a label keyed on one would lose its position the next time the model was
        /// recalculated. This is derived from the persistent model identities the label concerns - the space
        /// and role for a terminal, the aperture or the pair of spaces for a transfer route - so it is the
        /// same before and after a recalculation, a save and reopen, or a rebuild of the assessment from the
        /// same model. See <see cref="PartFAnnotationKey"/> and <see cref="PartFAnnotationOverride"/>.
        /// </para>
        /// </summary>
        public Guid AnnotationGuid { get; set; } = Guid.Empty;

        /// <summary>
        /// Which annotation on <see cref="AnnotationGuid"/> this mark's label is, so a manual position is
        /// keyed unambiguously where one object carries more than one movable label.
        /// </summary>
        public PartFAnnotationType AnnotationType
        {
            get { return IsTransfer ? PartFAnnotationType.Transfer : PartFAnnotationType.Terminal; }
        }

        /// <summary>Transfer marks only: the space the air flows TO.</summary>
        public Guid DownstreamSpaceGuid { get; set; } = Guid.Empty;

        /// <summary>Transfer marks only: name of <see cref="DownstreamSpaceGuid"/>.</summary>
        public string DownstreamSpaceName { get; set; }

        /// <summary>
        /// Transfer marks only: the door aperture crossed, or <see cref="Guid.Empty"/> where the two
        /// spaces adjoin through a partition that carries no modelled door.
        /// </summary>
        public Guid ApertureGuid { get; set; } = Guid.Empty;

        /// <summary>Transfer marks only: the transfer route's name, for selection and tooltips.</summary>
        public string DoorName { get; set; }

        /// <summary>
        /// Transfer marks only. True where this mark crosses an actual modelled door; false where it
        /// crosses the separating wall because the model carries no door aperture there. The distinction
        /// belongs on the mark so the view can show it, rather than implying a door that is not there.
        /// </summary>
        public bool IsDoorRepresented { get; set; }

        /// <summary>
        /// Tail of the arrow, in the floor plan's own 2D coordinates [m]. For a terminal mark this equals
        /// <see cref="End"/> - see <see cref="Direction"/>.
        /// </summary>
        public Point2D Start { get; set; }

        /// <summary>
        /// Head of the arrow, in the floor plan's own 2D coordinates [m]. For a terminal mark this equals
        /// <see cref="Start"/>.
        /// </summary>
        public Point2D End { get; set; }

        /// <summary>
        /// Which way the air moves at this mark: into the room for supply, out of it for extract, along
        /// the route for transfer.
        /// <para>
        /// A terminal mark is a POSITION plus a direction, not a span. Its <see cref="Start"/> and
        /// <see cref="End"/> are the same point, and the view draws a short fixed-length stub from it. A
        /// terminal is a grille in a ceiling; drawing it as a long arrow across the room would assert an
        /// in-room air trajectory that nothing here has calculated. Only <see cref="IsTransfer"/> marks
        /// span real distance, because only they connect two spaces.
        /// </para>
        /// </summary>
        public Vector2D Direction { get; set; } = new Vector2D(1, 0);

        /// <summary>
        /// The flow [l/s] at the overlay's operating condition, read from the assessment. Null where the
        /// terminal does not run at that condition - an intermittent cooker hood has no continuous rate.
        /// </summary>
        public double? FlowRate_Lps { get; set; }

        /// <summary>
        /// The short label to draw, e.g. "SUP 30 l/s". Built by <see cref="PartFAirflowAppearance"/>. A
        /// transfer mark whose opening is unresolved carries a trailing "?" in the label itself, so the
        /// distinction survives a printout and does not depend on colour.
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// A second, smaller line under the label where the mark needs qualifying - "No modelled transfer
        /// opening identified" - and null where it does not. Captioning every arrow would make the plan
        /// unreadable, which is its own failure.
        /// </summary>
        public string Caption { get; set; }

        /// <summary>
        /// Transfer marks only: what the model shows about the physical opening, which decides whether
        /// this mark may be drawn as an established route at all.
        /// </summary>
        public PartFTransferOpeningStatus OpeningStatus { get; set; } = PartFTransferOpeningStatus.NotAssessed;

        /// <summary>The assessed status of the terminal or route this mark stands for.</summary>
        public PartFComplianceStatus Status { get; set; } = PartFComplianceStatus.NotAssessed;

        /// <summary>
        /// True where the thing this mark stands for is not established: a terminal SAM proposed but
        /// nobody has recorded as provided, an undercut nobody recorded, a route the topology did not fix.
        /// <para>
        /// The view must draw these differently from a confirmed mark. Absence of evidence is not
        /// compliance, and a plan that drew a proposed terminal exactly like an installed one would be
        /// showing SAM's own suggestion back to the reader as a survey.
        /// </para>
        /// </summary>
        public bool IsUnresolved { get; set; }

        /// <summary>True where this mark stands for a transfer route rather than a terminal.</summary>
        public bool IsTransfer
        {
            get { return AirType == PartFAirflowAppearance.AirType.TransferAir; }
        }
    }
}
