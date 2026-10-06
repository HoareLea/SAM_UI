// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.UI.WPF
{
    public enum PartOWorkflowOutcomeKind
    {
        Information,
        Success,
        Warning,

        /// <summary>The action completed and its engineering answer is a failure - "Iteration 1a completed — TM59 FAIL".</summary>
        Fail,
    }

    /// <summary>
    /// The Hub's line about the most recent meaningful thing: "✕ Iteration 1a completed — TM59 FAIL", with one
    /// compact caption under it and the long explanation behind the tooltip and Show details.
    ///
    /// <para><b>Two sources, never mixed</b></para>
    /// <list type="bullet">
    /// <item>What the last Hub action did - a session-only record, carried to the next showing of the Hub and
    /// no further. Nothing persists it, so closing and reopening the Hub cannot bring back a cancellation.</item>
    /// <item>Where there is no such record, what the run itself says now (<c>Modify.StandingOutcome</c>):
    /// prepared, reopened, or no longer valid - read off <see cref="PartORun"/>, never remembered.</item>
    /// </list>
    ///
    /// <para><b>A record never outlives the state it describes.</b> <see cref="RunState"/> is the run state
    /// the line's claim depends on; where the run has since left it, the Hub shows the run's own state instead
    /// (<c>Modify.HubOutcome</c>). A completed-with-results line is never shown over results that are gone.</para>
    /// </summary>
    public class PartOWorkflowOutcome
    {
        /// <summary>A single line, glyph included in the text where it has one.</summary>
        public PartOWorkflowOutcome(PartOWorkflowOutcomeKind kind, string text)
            : this(kind, null, text, null)
        {
        }

        public PartOWorkflowOutcome(PartOWorkflowOutcomeKind kind, string? glyph, string headline, string? detail, string? toolTip = null)
        {
            Kind = kind;
            Glyph = string.IsNullOrWhiteSpace(glyph) ? null : glyph;
            Headline = headline;
            Detail = string.IsNullOrWhiteSpace(detail) ? null : detail;
            ToolTip = string.IsNullOrWhiteSpace(toolTip) ? null : toolTip;
        }

        public PartOWorkflowOutcomeKind Kind { get; }

        /// <summary>✓ ✕ ○ – ! - so the line never depends on colour alone. Null where the headline carries its own.</summary>
        public string? Glyph { get; }

        /// <summary>What happened, in a few words: "Iteration 2 prepared — waiting for the full-year TAS run".</summary>
        public string Headline { get; }

        /// <summary>The one compact caption under it: durations, counts, what was kept.</summary>
        public string? Detail { get; }

        /// <summary>The complete explanation - a refusal, a stop description - behind the tooltip and Show details.</summary>
        public string? ToolTip { get; }

        /// <summary>
        /// The run state this line's claim depends on, or null where it claims nothing about the run (a review
        /// that was cancelled left the run as it was, whatever that was). For
        /// <see cref="PartORunState.WorkflowCompleted"/> the results must also still be reviewable.
        /// </summary>
        public PartORunState? RunState { get; set; }

        /// <summary>The whole line as one string - what Copy, tests and the tooltip header read.</summary>
        public string Text
        {
            get
            {
                string text = Glyph is null ? Headline : string.Format("{0} {1}", Glyph, Headline);

                return Detail is null ? text : string.Format("{0} · {1}", text, Detail);
            }
        }

        public override string ToString()
        {
            return Text;
        }
    }
}
