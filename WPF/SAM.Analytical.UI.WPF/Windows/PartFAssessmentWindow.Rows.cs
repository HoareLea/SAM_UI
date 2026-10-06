// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using System;
using System.Globalization;
using System.Reflection;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Decides whether a Part F grid column is editable, from the row type it binds to.
    /// <para>
    /// Kept out of the window and made internally visible so a test can hold the rule without having to
    /// construct a WPF window on an STA thread. The rule matters: a two-way binding onto a read-only
    /// property throws when WPF attaches it, and WPF attaches it when the cell is first realised - which
    /// is when the tab is first shown, not when the window opens. That is a crash a headless test suite
    /// will never see and a user hits immediately.
    /// </para>
    /// </summary>
    internal static class PartFGridColumn
    {
        /// <summary>
        /// True where <paramref name="path"/> names a property of <paramref name="rowType"/> that has no
        /// public setter, and so must be bound one-way and read-only.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// <paramref name="path"/> names no public instance property of <paramref name="rowType"/>. Thrown
        /// rather than defaulting, because a column bound to nothing renders as an empty cell and reads as
        /// missing data.
        /// </exception>
        public static bool IsReadOnly(Type rowType, string path)
        {
            PropertyInfo propertyInfo = rowType?.GetProperty(path, BindingFlags.Public | BindingFlags.Instance)
                ?? throw new ArgumentException(string.Format("{0} has no public property '{1}' for a grid column to bind to.", rowType?.Name, path), nameof(path));

            return propertyInfo.SetMethod is null || !propertyInfo.SetMethod.IsPublic;
        }
    }

    /// <summary>
    /// Reading and writing the values an engineer types into the Part F assessment grids.
    /// <para>
    /// Kept in one place so that every grid parses a number, a yes/no and an enum the same way. A grid
    /// that silently discarded a value because the decimal separator did not match the machine's locale
    /// would look exactly like a value that had never been entered.
    /// </para>
    /// </summary>
    internal static class PartFGridValue
    {
        /// <summary>Formats an optional number for a grid cell, leaving an unset value blank.</summary>
        public static string Number(double? value)
        {
            return value is null ? string.Empty : value.Value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Parses a number typed into a grid cell. Both the invariant and the machine's own format are
        /// accepted, because an engineer uses whichever their keyboard gives them. A blank cell means "not
        /// recorded", which for a door undercut is a different answer from zero.
        /// </summary>
        public static double? Parse(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            if (double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out double result) ||
                double.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out result))
            {
                return double.IsNaN(result) || double.IsInfinity(result) ? null : result;
            }

            return null;
        }

        /// <summary>
        /// Parses a yes/no typed into a grid cell, tri-state on purpose: for a door undercut, "not
        /// recorded" selects neither the 10mm paragraph 1.25a datum nor the 20mm paragraph 1.25b one, and
        /// is a different answer from "not fitted".
        /// </summary>
        public static bool? ParseBoolean(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            if (bool.TryParse(text, out bool result))
            {
                return result;
            }

            string text_Trimmed = text.Trim();

            return text_Trimmed.StartsWith("y", StringComparison.OrdinalIgnoreCase) ? true
                : text_Trimmed.StartsWith("n", StringComparison.OrdinalIgnoreCase) ? false
                : null;
        }

        /// <summary>
        /// Parses an enum typed or chosen in a grid cell. The cells show the human-readable description,
        /// which carries spaces, so those are removed before matching. An unrecognised value leaves the
        /// existing one alone rather than resetting it to the first member of the enum.
        /// </summary>
        public static T ParseEnum<T>(string text, T @default) where T : struct
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return @default;
            }

            if (Enum.TryParse(text.Replace(" ", string.Empty), true, out T result))
            {
                return result;
            }

            //Fall back to matching the description the cell actually displays, so a value round trips
            //through the grid unchanged even where the description and the member name differ.
            foreach (T value in Enum.GetValues(typeof(T)))
            {
                if (string.Equals(Core.Query.Description(value as Enum), text.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return value;
                }
            }

            return @default;
        }

        /// <summary>A status as it appears in a grid: the symbol first, so it reads without colour.</summary>
        public static string Status(PartFComplianceStatus partFComplianceStatus)
        {
            return string.Format("{0} {1}", PartFAirflowAppearance.Status(partFComplianceStatus).Symbol, Core.Query.Description(partFComplianceStatus));
        }
    }

    /// <summary>
    /// One terminal as a grid row. Read-only throughout: every value here is calculated from the Approved
    /// Document and none of it is an engineering choice. The extract METHOD is an engineering input, but
    /// it belongs to the space, and is set on the space rather than typed into this grid.
    /// <para>
    /// Because every property here is get-only, every column bound to this row must be built read-only.
    /// A two-way binding onto a read-only property throws when WPF attaches it, which is when the cell is
    /// first realised - so the window would open normally and then fall over the moment the Terminals tab
    /// was shown.
    /// </para>
    /// <para>
    /// <see cref="Required"/>, <see cref="Proposed"/> and <see cref="Provided"/> are three separate
    /// columns on purpose. A reader has to be able to see whether the method behind a rate was stated by
    /// the design or supplied by SAM, and a proposed terminal is never evidence that one is installed.
    /// </para>
    /// </summary>
    internal sealed class PartFTerminalRow
    {
        private readonly PartFVentilationTerminalRequirement terminal;

        public PartFTerminalRow(PartFVentilationTerminalRequirement terminal)
        {
            this.terminal = terminal;
        }

        public string SpaceName => terminal.SpaceName;

        public string Role => Core.Query.Description(terminal.TerminalRole);

        /// <summary>The rate the Approved Document requires of this terminal at the high condition.</summary>
        public string Required => PartFGridValue.Number(terminal.RequiredHighFlowRate_Lps);

        public string Method => terminal.TerminalRole == PartFTerminalRole.Supply ? string.Empty : Core.Query.Description(terminal.ExtractMethod);

        /// <summary>What SAM proposed for this location, from the system type and the room.</summary>
        public string Proposed => terminal.TerminalRole == PartFTerminalRole.Supply ? string.Empty : Core.Query.Description(terminal.ProposedExtractMethod);

        /// <summary>What the design actually records, which is blank until somebody records it.</summary>
        public string Provided => terminal.TerminalRole == PartFTerminalRole.Supply ? string.Empty : Core.Query.Description(terminal.ProvidedExtractMethod);

        /// <summary>Whether a provision has been established at all, separate from whether it is adequate.</summary>
        public string Provision => terminal.TerminalRole == PartFTerminalRole.Supply ? string.Empty : PartFGridValue.Status(terminal.ProvisionStatus);

        public string Continuous => PartFGridValue.Number(terminal.ContinuousDesignFlowRate_Lps);

        public string High => PartFGridValue.Number(terminal.HighFlowRate_Lps);

        public string Setback => PartFGridValue.Number(terminal.SetbackFlowRate_Lps);

        public string Status => PartFGridValue.Status(terminal.ComplianceStatus);

        public string Source => terminal.SourceReference;

        public string Note => terminal.Diagnostic;
    }

    /// <summary>
    /// One internal door as a grid row. The paragraph 1.25 requirement and the calculated transfer flow
    /// are read-only; the provided undercut, the provided free area, the clear width, the floor finish
    /// state, the transfer device and any transfer flow override are editable, because they are the values
    /// SAM cannot derive from an analytical model.
    /// </summary>
    internal sealed class PartFDoorRow
    {
        private readonly PartFDoorTransferData partFDoorTransferData;

        public PartFDoorRow(PartFDoorTransferData partFDoorTransferData)
        {
            this.partFDoorTransferData = partFDoorTransferData;

            ProvidedUndercut = PartFGridValue.Number(partFDoorTransferData.ProvidedUndercutHeight_mm);
            ProvidedArea = PartFGridValue.Number(partFDoorTransferData.ProvidedFreeArea_mm2);
            ClearWidth = PartFGridValue.Number(partFDoorTransferData.ClearDoorWidth_mm);
            FloorFinishFitted = partFDoorTransferData.IsFloorFinishFitted is null ? string.Empty : partFDoorTransferData.IsFloorFinishFitted.Value ? "yes" : "no";
            Device = Core.Query.Description(partFDoorTransferData.TransferDeviceType);
            Override = PartFGridValue.Number(partFDoorTransferData.TransferFlowRateOverride_Lps);
        }

        /// <summary>The record this row edits, so the window can re-read it after applying.</summary>
        public PartFDoorTransferData PartFDoorTransferData => partFDoorTransferData;

        public string Name => partFDoorTransferData.Name;

        public string From => partFDoorTransferData.UpstreamSpaceName;

        public string To => partFDoorTransferData.DownstreamSpaceName;

        public string IsInternal => partFDoorTransferData.IsInternalDwellingDoor ? "yes" : "no";

        public string Required => partFDoorTransferData.RequiresTransferAirPath ? "yes" : "no";

        public string Continuous => PartFGridValue.Number(partFDoorTransferData.ContinuousDesignTransferFlowRate_Lps);

        public string High => PartFGridValue.Number(partFDoorTransferData.HighTransferFlowRate_Lps);

        public string Setback => PartFGridValue.Number(partFDoorTransferData.SetbackTransferFlowRate_Lps);

        public string RequiredArea => PartFGridValue.Number(partFDoorTransferData.MinimumRequiredFreeArea_mm2);

        public string RequiredUndercut => string.Format("{0} / {1}", PartFGridValue.Number(partFDoorTransferData.RequiredUndercutHeightFinished_mm), PartFGridValue.Number(partFDoorTransferData.RequiredUndercutHeightBeforeFloorFinish_mm));

        public string Route => Core.Query.Description(partFDoorTransferData.RouteStatus);

        public string Status => PartFGridValue.Status(partFDoorTransferData.ComplianceStatus);

        public string Diagnostic => partFDoorTransferData.Diagnostic;

        // Editable
        public string ProvidedUndercut { get; set; }

        public string ProvidedArea { get; set; }

        public string ClearWidth { get; set; }

        public string FloorFinishFitted { get; set; }

        public string Device { get; set; }

        public string Override { get; set; }

        /// <summary>
        /// Writes the edited inputs back and re-runs the paragraph 1.25 assessment on them, so the status
        /// shown next reflects what was just entered rather than the previous run.
        /// </summary>
        public void Apply()
        {
            partFDoorTransferData.ProvidedUndercutHeight_mm = PartFGridValue.Parse(ProvidedUndercut);
            partFDoorTransferData.ProvidedFreeArea_mm2 = PartFGridValue.Parse(ProvidedArea);
            partFDoorTransferData.ClearDoorWidth_mm = PartFGridValue.Parse(ClearWidth);
            partFDoorTransferData.IsFloorFinishFitted = PartFGridValue.ParseBoolean(FloorFinishFitted);
            partFDoorTransferData.TransferDeviceType = PartFGridValue.ParseEnum(Device, partFDoorTransferData.TransferDeviceType);
            partFDoorTransferData.TransferFlowRateOverride_Lps = PartFGridValue.Parse(Override);

            PartFTransferPathBuilder.Assess(partFDoorTransferData);
        }
    }

    /// <summary>
    /// One habitable room's purge assessment as a grid row. The requirement is read-only; the purge
    /// method, opening type, opening angle, openable areas and mechanical capacity are editable, because
    /// Table 1.4 is about the area of the OPENING and no analytical model carries it.
    /// </summary>
    internal sealed class PartFPurgeRow
    {
        private readonly PartFPurgeVentilationData partFPurgeVentilationData;

        public PartFPurgeRow(PartFPurgeVentilationData partFPurgeVentilationData)
        {
            this.partFPurgeVentilationData = partFPurgeVentilationData;

            Method = Core.Query.Description(partFPurgeVentilationData.PurgeMethod);
            OpeningType = Core.Query.Description(partFPurgeVentilationData.OpeningType);
            Angle = PartFGridValue.Number(partFPurgeVentilationData.OpeningAngle_Degrees);
            OpenableWindow = PartFGridValue.Number(partFPurgeVentilationData.OpenableWindowArea_M2);
            ExternalDoor = PartFGridValue.Number(partFPurgeVentilationData.ExternalDoorOpeningArea_M2);
            Mechanical = PartFGridValue.Number(partFPurgeVentilationData.MechanicalPurgeCapacity_Lps);
        }

        public string SpaceName => partFPurgeVentilationData.SpaceName;

        public string Volume => PartFGridValue.Number(partFPurgeVentilationData.RoomVolume_M3);

        public string Area => PartFGridValue.Number(partFPurgeVentilationData.RoomFloorArea_M2);

        public string Required => PartFGridValue.Number(partFPurgeVentilationData.RequiredPurgeRate_Lps);

        public string RequiredArea => PartFGridValue.Number(partFPurgeVentilationData.RequiredOpeningArea_M2);

        public string ModelWindowArea => PartFGridValue.Number(partFPurgeVentilationData.ExternalApertureArea_M2);

        public string DirectlyOutside => partFPurgeVentilationData.IsPurgeRouteDirectlyOutside ? "yes" : "no";

        public string Status => PartFGridValue.Status(partFPurgeVentilationData.ComplianceStatus);

        public string Diagnostic => partFPurgeVentilationData.Diagnostic;

        // Editable
        public string Method { get; set; }

        public string OpeningType { get; set; }

        public string Angle { get; set; }

        public string OpenableWindow { get; set; }

        public string ExternalDoor { get; set; }

        public string Mechanical { get; set; }

        /// <summary>
        /// Writes the edited inputs back onto the purge record. The assessment itself is re-run by the next
        /// calculation, which reads these values and never overwrites them.
        /// </summary>
        public void Apply()
        {
            partFPurgeVentilationData.PurgeMethod = PartFGridValue.ParseEnum(Method, partFPurgeVentilationData.PurgeMethod);
            partFPurgeVentilationData.OpeningType = PartFGridValue.ParseEnum(OpeningType, partFPurgeVentilationData.OpeningType);
            partFPurgeVentilationData.OpeningAngle_Degrees = PartFGridValue.Parse(Angle);
            partFPurgeVentilationData.OpenableWindowArea_M2 = PartFGridValue.Parse(OpenableWindow);
            partFPurgeVentilationData.ExternalDoorOpeningArea_M2 = PartFGridValue.Parse(ExternalDoor);
            partFPurgeVentilationData.MechanicalPurgeCapacity_Lps = PartFGridValue.Parse(Mechanical);
        }
    }

    /// <summary>
    /// One clause-level check as a grid row.
    /// <para>
    /// Ticking Confirm on a check SAM calculated as FAILED does not turn it into a pass. The tick is
    /// still recorded, along with the evidence, the alternative compliance method and the reason, and
    /// <see cref="PartFComplianceCheck.ApplyUserResolution"/> then decides what the check may report:
    /// an alternative solution pending approval where one has been recorded, and engineering review
    /// otherwise. The calculated result stays on the check either way, which is what
    /// <see cref="Calculated"/> shows.
    /// </para>
    /// </summary>
    internal sealed class PartFCheckRow
    {
        private readonly PartFComplianceCheck check;

        public PartFCheckRow(PartFComplianceCheck check)
        {
            this.check = check;

            Confirmed = check.Status == PartFComplianceStatus.UserConfirmed;
            ResponsiblePerson = check.ConfirmedBy;
            Date = check.ConfirmationDate;
            Notes = check.Notes;
            UserEvidence = check.UserEvidence;
            AlternativeComplianceMethod = check.AlternativeComplianceMethod;
            OverrideReason = check.OverrideReason;
        }

        public string Category => check.Category;

        public string Name => check.Name;

        /// <summary>What SAM calculated, which no entry in this grid can change.</summary>
        public string Calculated => PartFGridValue.Status(check.CalculatedStatus);

        /// <summary>What the assessment reports, after any recorded resolution.</summary>
        public string Status => PartFGridValue.Status(check.FinalAssessmentStatus);

        public string Source => check.SourceReference;

        public string Requirement => check.Requirement;

        public string Evidence => check.Evidence;

        /// <summary>
        /// True where a person may record an answer against this check. A calculated failure is included:
        /// recording the evidence and the alternative method against it is exactly what should happen. It
        /// is the RESULT of doing so that is constrained, not the recording.
        /// </summary>
        public bool CanConfirm
        {
            get
            {
                return check.CalculatedStatus != PartFComplianceStatus.Pass
                    && check.CalculatedStatus != PartFComplianceStatus.NotApplicable;
            }
        }

        // Editable
        public bool Confirmed { get; set; }

        public string ResponsiblePerson { get; set; }

        public string Date { get; set; }

        public string Notes { get; set; }

        public string UserEvidence { get; set; }

        public string AlternativeComplianceMethod { get; set; }

        public string OverrideReason { get; set; }

        /// <summary>
        /// Records the resolution, or withdraws it. A calculated result is never overwritten, and a
        /// calculated failure can never come out of this as a pass.
        /// </summary>
        public void Apply()
        {
            if (!CanConfirm)
            {
                return;
            }

            if (!Confirmed)
            {
                //Withdrawing a confirmation returns the check to what SAM calculated, not to a pass. The
                //supporting record is kept: the evidence a person gathered is still true after they
                //untick the box, and losing it would punish them for correcting themselves.
                check.FinalAssessmentStatus = check.CalculatedStatus;
                check.ConfirmedBy = ResponsiblePerson;
                check.ConfirmationDate = Date;
                check.Notes = Notes;
                check.UserEvidence = UserEvidence;
                check.AlternativeComplianceMethod = AlternativeComplianceMethod;
                check.OverrideReason = OverrideReason;

                return;
            }

            check.ApplyUserResolution(new PartFComplianceCheck(check.Name, check.SourceReference, check.Requirement)
            {
                Status = PartFComplianceStatus.UserConfirmed,
                ConfirmedBy = ResponsiblePerson,
                ConfirmationDate = Date,
                Notes = Notes,
                UserEvidence = UserEvidence,
                AlternativeComplianceMethod = AlternativeComplianceMethod,
                OverrideReason = OverrideReason,
            });
        }
    }
}
