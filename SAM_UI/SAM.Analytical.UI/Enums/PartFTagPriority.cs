// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.ComponentModel;

namespace SAM.Analytical.UI
{
    /// <summary>
    /// The order Part F tags claim space on a floor plan: the lower the value, the earlier the tag is
    /// placed, and so the closer it stays to what it annotates. Everything after it moves around it.
    /// <para>
    /// A named policy rather than numbers written into the drawing code. The order is an editorial
    /// judgement about which airflow information an engineer must be able to read first, it is asserted by
    /// tests, and it has to be the same in every view that draws these tags - none of which survives
    /// numbers spread through a renderer. It is mapped onto <c>Solver2DData.Priority</c> by
    /// <see cref="PartFTagPlacement"/> and nowhere else.
    /// </para>
    /// <para>
    /// The reasoning, in order:
    /// </para>
    /// <list type="number">
    /// <item><b>Transfer air</b> first. It is the only mark that spans two spaces, so it is the one the eye
    /// follows across the dwelling to see where the air actually goes, and it is anchored on an opening
    /// that a displaced label stops identifying. This includes a route whose opening the model does not
    /// establish - see the note on <see cref="TransferAir"/>.</item>
    /// <item><b>Kitchen extract</b>, then <b>general extract</b>. Table 1.2 sets a minimum high rate room
    /// by room and the kitchen carries the largest of them, so an extract figure that has been pushed away
    /// from its room is the one most likely to be misread against the wrong room.</item>
    /// <item><b>Supply</b>. Sized from the whole dwelling rather than the room, so a supply figure read
    /// against a neighbouring room misleads less than an extract figure does.</item>
    /// <item><b>Net airflow</b> per space, which is derived from the terminals above it.</item>
    /// <item><b>Diagnostics</b> last, among Part F's own tags. They qualify a mark that is already on the
    /// drawing, so where the plan runs out of room these are the ones that should be displaced or left
    /// out.</item>
    /// <item><b>Ventilation Design tags</b> last of all - see <see cref="DesignSupply"/>.</item>
    /// </list>
    /// </summary>
    public enum PartFTagPriority
    {
        [Description("Undefined")] Undefined = 0,

        /// <summary>
        /// A transfer-air route label.
        /// <para>
        /// A route the model gives no established opening for is still a transfer tag and takes this same
        /// priority. It must not be demoted below an ordinary terminal label: it is the mark that tells an
        /// engineer the dwelling's air path is not resolved, so it is the one that most needs to be legible
        /// and next to the partition it concerns. Absence of evidence is not compliance, and it is not a
        /// reason to make the evidence harder to read either.
        /// </para>
        /// </summary>
        [Description("Transfer Air")] TransferAir = 1,

        /// <summary>A local kitchen extract label - the largest Table 1.2 minimum in the dwelling.</summary>
        [Description("Kitchen Extract")] KitchenExtract = 2,

        /// <summary>A general extract label: bathroom, sanitary accommodation, utility room.</summary>
        [Description("Extract")] Extract = 3,

        /// <summary>A supply label.</summary>
        [Description("Supply")] Supply = 4,

        /// <summary>A space's net airflow label, derived from the terminals in it.</summary>
        [Description("Net Airflow")] NetAirflow = 5,

        /// <summary>
        /// A caption or warning qualifying a mark that is already drawn. Placed last among Part F's own
        /// tags, so it is what gives way when the plan is crowded.
        /// </summary>
        [Description("Diagnostic")] Diagnostic = 6,

        /// <summary>
        /// The Ventilation Design overlay's transfer mark - the design air moving between two rooms.
        /// First among the design overlay's own tags, for the same reason
        /// <see cref="TransferAir"/> is first among Approved Document F's: it is the mark that says
        /// whether the dwelling's air path can carry what the design now asks of it, so it is the one that
        /// most needs to stay legible and next to the opening it concerns. It is still placed after every
        /// Approved Document F priority - see <see cref="DesignSupply"/>.
        /// </summary>
        [Description("Design Transfer")] DesignTransfer = 7,

        /// <summary>
        /// The Ventilation Design overlay's design supply mark - "SUP 150.0 l/s" - sharing this same
        /// placement policy, and so the same shared <see cref="PartFTagPlacement"/> solve, as Part F's own
        /// tags. Placed AFTER every Part F priority, including <see cref="Diagnostic"/>: Part F carries a
        /// regulatory figure and must keep the position it would have on its own regardless of which other
        /// overlays happen to be switched on, so nothing here is allowed to outrank it. The design value is
        /// still solved clear of every Part F tag and of every other design tag - see the tests in
        /// <c>DesignAirFlowRenderer</c> - it simply never wins a contested first-choice position over Part F.
        /// </summary>
        [Description("Design Supply")] DesignSupply = 8,

        /// <summary>The design overlay's design extract mark. See <see cref="DesignSupply"/>.</summary>
        [Description("Design Extract")] DesignExtract = 9,

        /// <summary>The design overlay's net (supply minus extract) mark. See <see cref="DesignSupply"/>.</summary>
        [Description("Design Net")] DesignNet = 10,
    }
}
