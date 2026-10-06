// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Windows;
using System.Windows.Media;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// One row of the Prepare and Run status list, as the view binds it.
    /// <para>
    /// <b>Presentation only.</b> Every value is read off the <see cref="PartOWorkflowStageState"/> it wraps;
    /// the only things added are the colour and where the sentence is cut for a one-line row, and even the
    /// colour is a function of the status the inspection assigned. No row can say anything the inspection
    /// did not, and no row can change a status.
    /// </para>
    /// </summary>
    public class PartOWorkflowStatusRow
    {
        /// <summary>
        /// A row whose one-line explanation is the inspection's own sentence, shortened where it does not
        /// fit a line.
        /// </summary>
        public PartOWorkflowStatusRow(PartOWorkflowStageState partOWorkflowStageState)
            : this(partOWorkflowStageState, null)
        {
        }

        /// <param name="summary">
        /// A shorter engineer-facing sentence for the one-line row, where the host has one that reads better
        /// than the first sentence of <see cref="Detail"/>. The full detail is never discarded - it stays on
        /// <see cref="FullDetail"/>, in the tooltip, and behind the row's own disclosure.
        /// </param>
        public PartOWorkflowStatusRow(PartOWorkflowStageState partOWorkflowStageState, string? summary)
        {
            State = partOWorkflowStageState;
            Summary = string.IsNullOrWhiteSpace(summary) ? null : summary.Trim();
        }

        public PartOWorkflowStageState State { get; }

        /// <summary>The host's shorter sentence for this row, or null where it offered none.</summary>
        public string? Summary { get; }

        public string Name => State.Name;

        public string StatusText => State.StatusText;

        /// <summary>
        /// The status as the row shows it, in sentence case and in the stage's own terms - "Waiting" for a
        /// check that runs later, "Not available" for results that do not exist yet.
        /// <para>
        /// <b>A label, never a new state.</b> It is chosen from the stage and the status the inspection
        /// assigned, and nothing else: two rows with the same stage and status always read the same.
        /// <see cref="StatusText"/> remains the status's own name.
        /// </para>
        /// </summary>
        public string StatusLabel => Label(State.Stage, State.Status);

        /// <summary>
        /// A glyph beside the label, so the state never depends on colour alone: ✓ ready, ✕ blocked,
        /// ○ still to happen, – not part of this scenario.
        /// </summary>
        public string StatusGlyph => Glyph(State.Status);

        /// <summary>The label for a stage in a status. Exposed so the mapping is testable on its own.</summary>
        public static string Label(PartOWorkflowStage partOWorkflowStage, PartOWorkflowStageStatus partOWorkflowStageStatus)
        {
            return partOWorkflowStageStatus switch
            {
                PartOWorkflowStageStatus.Ready => "Ready",
                PartOWorkflowStageStatus.Reused => "Reused",
                PartOWorkflowStageStatus.Blocked => "Blocked",
                PartOWorkflowStageStatus.NotApplicable => "N/A",
                PartOWorkflowStageStatus.Prepare => "Not prepared",
                PartOWorkflowStageStatus.Pending => partOWorkflowStage == PartOWorkflowStage.ModelCheck ? "Waiting" : "Not checked",
                PartOWorkflowStageStatus.NotRun => partOWorkflowStage == PartOWorkflowStage.Results ? "Not available" : "Not run",
                _ => Core.Query.Description(partOWorkflowStageStatus),
            };
        }

        /// <summary>The glyph for a status. Exposed so the mapping is testable on its own.</summary>
        public static string Glyph(PartOWorkflowStageStatus partOWorkflowStageStatus)
        {
            return partOWorkflowStageStatus switch
            {
                PartOWorkflowStageStatus.Ready => "✓",
                PartOWorkflowStageStatus.Reused => "✓",
                PartOWorkflowStageStatus.Blocked => "✕",
                PartOWorkflowStageStatus.NotApplicable => "–",
                _ => "○",
            };
        }

        /// <summary>The inspection's sentence, complete and unaltered. Nothing consumes a shortened one.</summary>
        public string Detail => State.Detail;

        /// <summary>The same sentence, named for what the disclosure and the tooltip show.</summary>
        public string FullDetail => State.Detail;

        /// <summary>
        /// What the one-line row shows: the host's shorter sentence where it gave one, otherwise the first
        /// sentence of the inspection's own detail.
        /// <para>
        /// <b>Cut, never rewritten.</b> Where no summary was supplied this is a prefix of
        /// <see cref="FullDetail"/> - the detail up to and including its first full stop - so a row can only
        /// ever say less than the inspection said, never something different from it.
        /// </para>
        /// </summary>
        public string ShortDetail
        {
            get
            {
                if (Summary is not null)
                {
                    return Summary;
                }

                //The inspection's own compact line, from the same counts as its detail.
                if (!string.IsNullOrWhiteSpace(State?.Summary))
                {
                    return State!.Summary;
                }

                string detail = State?.Detail ?? string.Empty;

                int index = detail.IndexOf(". ", System.StringComparison.Ordinal);

                return index < 0 ? detail : detail.Substring(0, index + 1);
            }
        }

        /// <summary>Whether the full detail says more than the one-line row does.</summary>
        public bool HasMoreDetail => !string.Equals(ShortDetail?.Trim(), (State?.Detail ?? string.Empty).Trim(), System.StringComparison.Ordinal);

        /// <summary>The disclosure, shown only where there is more to disclose.</summary>
        public Visibility DisclosureVisibility => HasMoreDetail ? Visibility.Visible : Visibility.Collapsed;

        /// <summary>
        /// What the disclosure is called. A stage that is not part of this scenario is the one an engineer
        /// most often wants the reasoning for, so it asks their question rather than saying "more".
        /// </summary>
        public string DisclosureHeader => State?.Status == PartOWorkflowStageStatus.NotApplicable ? "Why is this N/A?" : "Why?";

        /// <summary>
        /// The status colour. Red is reserved for the one status that stops Run, so a person scanning the
        /// list is never alarmed by a stage that is merely going to be built.
        /// </summary>
        public Brush Foreground
        {
            get
            {
                return State.Status switch
                {
                    PartOWorkflowStageStatus.Blocked => Brushes.Firebrick,
                    PartOWorkflowStageStatus.Ready => Brushes.DarkGreen,
                    PartOWorkflowStageStatus.Reused => Brushes.DarkGreen,
                    PartOWorkflowStageStatus.Prepare => Brushes.DarkGoldenrod,
                    _ => Brushes.DimGray,
                };
            }
        }

        public override string ToString()
        {
            return State.ToString();
        }
    }
}
