// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Text;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// The notes, warnings and refusals a Part O preparation produced, arranged so that a person can see how
    /// many there are before reading them - and can still read every one of them.
    ///
    /// <para><b>It aggregates presentation; it does not interpret warnings</b></para>
    /// <para>
    /// Nothing here parses a warning, classifies it, ranks it, assigns it a severity or decides that one
    /// warning stands for another. The only thing it does is count, and collapse <b>warning</b> lines that
    /// are <b>character-for-character identical</b> into one line with a <c>× N</c> after it. Two warnings
    /// that differ by so much as a space are two lines. No space name is stripped to manufacture a match,
    /// and nothing is dropped: <see cref="Text"/> is every original line in its original order, which is
    /// what Copy All copies and what the window shows the moment a person asks for every line.
    /// </para>
    /// <para>
    /// <b>Refusals and notes are never collapsed</b>, however identical two of them are. A refusal is the
    /// most consequential line this window can carry, and a person counting them on screen has to be
    /// counting refusals rather than distinct texts.
    /// </para>
    ///
    /// <para><b>Refusals stay first and stay counted</b></para>
    /// <para>
    /// The three kinds keep the order the window has always used - refusals, then warnings, then notes - and
    /// the header names each kind that is present with its own count, so a refusal cannot hide behind a
    /// warning count.
    /// </para>
    /// </summary>
    public class PartODiagnosticSummary
    {
        private const string Label_Refusal = "REFUSAL";

        private const string Label_Warning = "WARNING";

        private const string Label_Note = "NOTE";

        private readonly List<string> refusals;

        private readonly List<string> warnings;

        private readonly List<string> notes;

        public PartODiagnosticSummary(IEnumerable<string>? notes, IEnumerable<string>? warnings, IEnumerable<string>? refusals)
        {
            this.refusals = Clean(refusals);
            this.warnings = Clean(warnings);
            this.notes = Clean(notes);
        }

        /// <summary>How many refusals, as produced. Never de-duplicated.</summary>
        public int RefusalCount => refusals.Count;

        /// <summary>How many warnings, as produced - the count before any identical lines are collapsed.</summary>
        public int WarningCount => warnings.Count;

        /// <summary>How many notes, as produced.</summary>
        public int NoteCount => notes.Count;

        /// <summary>
        /// The header, naming each kind present with its own count - <c>Warnings (6)</c> where warnings are
        /// all there is, and every present kind where there is more than one.
        /// </summary>
        public string Header
        {
            get
            {
                List<string> parts = [];

                if (RefusalCount != 0)
                {
                    parts.Add(string.Format("Refusals ({0})", RefusalCount));
                }

                if (WarningCount != 0)
                {
                    parts.Add(string.Format("Warnings ({0})", WarningCount));
                }

                if (NoteCount != 0)
                {
                    parts.Add(string.Format("Notes ({0})", NoteCount));
                }

                return parts.Count == 0 ? "Notes, warnings and refusals (none)" : string.Join(" · ", parts);
            }
        }

        /// <summary>
        /// Every line, exactly as produced and in the order produced. The complete record: what Copy All
        /// copies, and what the window shows when a person asks to see every line.
        /// </summary>
        public string Text
        {
            get
            {
                StringBuilder stringBuilder = new();

                Append(stringBuilder, Label_Refusal, refusals);
                Append(stringBuilder, Label_Warning, warnings);
                Append(stringBuilder, Label_Note, notes);

                return stringBuilder.ToString();
            }
        }

        /// <summary>
        /// The same lines with character-for-character duplicate WARNINGS collapsed - one line, followed
        /// by <c>× N</c> where a warning occurred more than once. Refusals and notes are listed in full.
        /// <para>
        /// Every distinct line survives, in first-occurrence order. Where no warning is duplicated this is
        /// <see cref="Text"/>.
        /// </para>
        /// </summary>
        public string GroupedText
        {
            get
            {
                StringBuilder stringBuilder = new();

                Append(stringBuilder, Label_Refusal, refusals);
                AppendGrouped(stringBuilder, Label_Warning, warnings);
                Append(stringBuilder, Label_Note, notes);

                return stringBuilder.ToString();
            }
        }

        /// <summary>
        /// Whether collapsing identical warnings actually removed a line - the only case in which the
        /// grouped view and the complete record differ, and so the only case in which the toggle has
        /// anything to do.
        /// </summary>
        public bool IsGrouped => !string.Equals(GroupedText, Text, System.StringComparison.Ordinal);

        private static List<string> Clean(IEnumerable<string>? descriptions)
        {
            List<string> result = [];

            foreach (string description in descriptions ?? [])
            {
                //The same emptiness filter the window has always applied, and nothing else: a line that
                //says something is kept exactly as it says it.
                if (!string.IsNullOrWhiteSpace(description))
                {
                    result.Add(description);
                }
            }

            return result;
        }

        private static void Append(StringBuilder stringBuilder, string label, List<string> descriptions)
        {
            foreach (string description in descriptions)
            {
                stringBuilder.AppendLine(string.Format("{0}: {1}", label, description));
            }
        }

        private static void AppendGrouped(StringBuilder stringBuilder, string label, List<string> descriptions)
        {
            List<string> order = [];

            Dictionary<string, int> counts = [];

            foreach (string description in descriptions)
            {
                if (counts.TryGetValue(description, out int count))
                {
                    counts[description] = count + 1;

                    continue;
                }

                counts[description] = 1;

                order.Add(description);
            }

            foreach (string description in order)
            {
                int count = counts[description];

                stringBuilder.AppendLine(count == 1
                    ? string.Format("{0}: {1}", label, description)
                    : string.Format("{0}: {1}  × {2}", label, description, count));
            }
        }
    }
}
