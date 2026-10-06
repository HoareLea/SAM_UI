// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Geometry.Planar;
using System;

namespace SAM.Analytical.UI
{
    /// <summary>
    /// One drawn value on the Ventilation Design floor-plan overlay: a space's aggregated design supply,
    /// design extract, net, or the design air transferred between two spaces - positioned in a floor
    /// plan's own 2D coordinates.
    /// <para>
    /// Carries no engineering authority of its own. <see cref="FlowRate_Lps"/> is read unchanged from
    /// <c>Query.VentilationTerminalDesignDuty_Lps</c> for a terminal mark and from
    /// <c>Query.DesignTransferFlowRate_Lps</c> for a transfer mark; nothing here recomputes, rounds or
    /// re-derives it.
    /// </para>
    /// <para>
    /// <b>One class for both shapes</b>, matching <see cref="PartFOverlayMark"/>, which does the same: a
    /// terminal mark is a single point in one room, a transfer mark is a span between two, and
    /// <see cref="IsTransfer"/> says which this is. Splitting them into two types would mean two of every
    /// list, every visibility rule and every placement path for what the renderer treats as one family of
    /// tags.
    /// </para>
    /// </summary>
    public class DesignAirFlowOverlayMark
    {
        /// <summary>
        /// The space this mark belongs to. For a transfer mark, the space the design air flows FROM.
        /// </summary>
        public Guid SpaceGuid { get; set; }

        /// <summary>The space's name, kept alongside the guid so a mark can be labelled without a model lookup.</summary>
        public string SpaceName { get; set; }

        public DesignAirFlowMarkType MarkType { get; set; }

        /// <summary>
        /// The design flow [l/s], or null. For a terminal mark, null means no terminal in this direction
        /// serves the space - not that the duty is zero; see <c>Query.VentilationTerminalDesignDuty_Lps</c>.
        /// A transfer mark is never built at all where the design transfers nothing between the two spaces,
        /// so it never carries a null or an invented zero - see <c>Query.DesignTransferFlowRate_Lps</c>.
        /// </summary>
        public double? FlowRate_Lps { get; set; }

        /// <summary>
        /// Where the mark sits on the floor plan, in the plan's own 2D coordinates. For a terminal mark, a
        /// synthetic point inside the room; for a transfer mark, the real opening or partition the route
        /// crosses, which is a location the model actually establishes.
        /// </summary>
        public Point2D Position { get; set; }

        /// <summary>The formatted text a renderer draws - "SUP 45.0 l/s", "TRA 150.0 l/s ?", etc.</summary>
        public string Label { get; set; }

        /// <summary>
        /// A second, smaller line under the label where the mark needs qualifying - "No modelled transfer
        /// opening identified" - and null where it does not. Captioning every arrow would make the plan
        /// unreadable, which is its own failure. Matches <see cref="PartFOverlayMark.Caption"/>.
        /// </summary>
        public string Caption { get; set; }

        // -------------------------------------------------------------------------------------------
        // Transfer marks only
        // -------------------------------------------------------------------------------------------

        /// <summary>Transfer marks only: the space the design air flows TO.</summary>
        public Guid DownstreamSpaceGuid { get; set; } = Guid.Empty;

        /// <summary>Transfer marks only: name of <see cref="DownstreamSpaceGuid"/>.</summary>
        public string DownstreamSpaceName { get; set; }

        /// <summary>
        /// Transfer marks only: the door aperture crossed, or <see cref="Guid.Empty"/> where the two
        /// spaces adjoin through a partition carrying no single modelled door.
        /// </summary>
        public Guid ApertureGuid { get; set; } = Guid.Empty;

        /// <summary>
        /// Transfer marks only. True where this mark crosses an actual modelled door; false where it sits
        /// on the separating wall because the model carries no door aperture there. The distinction belongs
        /// on the mark so the view can show it, rather than implying a door that is not there.
        /// </summary>
        public bool IsDoorRepresented { get; set; }

        /// <summary>
        /// Transfer marks only: what the MODEL shows about the physical opening, which decides whether this
        /// mark may be drawn as an established route. Independent of <see cref="FlowRate_Lps"/> - see
        /// <see cref="DesignTransferOpeningStatus"/>.
        /// </summary>
        public DesignTransferOpeningStatus OpeningStatus { get; set; } = DesignTransferOpeningStatus.NotAssessed;

        /// <summary>
        /// Tail of the arrow, in the floor plan's own 2D coordinates [m]. Equals <see cref="End"/> for a
        /// terminal mark, and for a transfer route with no established opening - see <see cref="Direction"/>.
        /// </summary>
        public Point2D Start { get; set; }

        /// <summary>Head of the arrow, in the floor plan's own 2D coordinates [m].</summary>
        public Point2D End { get; set; }

        /// <summary>
        /// Which way the design air moves at this mark: along the route, from <see cref="SpaceGuid"/>
        /// towards <see cref="DownstreamSpaceGuid"/>.
        /// <para>
        /// A transfer mark keeps its direction rather than being reduced to an unsigned magnitude: "150 l/s
        /// between the bedroom and the hall" is a different statement from "150 l/s out of the bedroom",
        /// and only the second one is what the model says.
        /// </para>
        /// </summary>
        public Vector2D Direction { get; set; } = new Vector2D(1, 0);

        /// <summary>
        /// True where the thing this mark stands for is not established: a design transfer route the model
        /// gives no single modelled opening for.
        /// <para>
        /// The view must draw these differently from a confirmed mark. The design flow is still shown - the
        /// engineer needs to know how much air has nowhere established to go - but nothing about the route
        /// may read as confirmed.
        /// </para>
        /// </summary>
        public bool IsUnresolved { get; set; }

        /// <summary>True where this mark stands for a transfer route between two spaces rather than a terminal.</summary>
        public bool IsTransfer
        {
            get { return MarkType == DesignAirFlowMarkType.Transfer; }
        }
    }
}
