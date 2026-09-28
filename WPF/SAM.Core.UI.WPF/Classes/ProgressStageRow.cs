// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Windows;
using System.Windows.Media;

namespace SAM.Core.UI.WPF
{
    /// <summary>Where one stage of a long operation is, as a progress window shows it.</summary>
    public enum ProgressStageStatus
    {
        Pending,
        Running,
        Completed,
        Failed,
        Skipped,
    }

    /// <summary>
    /// One stage row of a SAM progress window, bound by <c>SAM.Progress.StageRowTemplate</c> in
    /// <c>Themes/ProgressStyles.xaml</c>: the status glyph, the stage's name, its time aligned right, and the whole
    /// line in words for a screen reader. Extracted from <c>PartOProgressWindow</c>, the reference implementation
    /// (see documentation/ProgressDialogPattern.md). The colours are that window's.
    /// </summary>
    public sealed class ProgressStageRow
    {
        public ProgressStageRow(string glyph, string name, string duration, string accessibleName, string statusText, ProgressStageStatus progressStageStatus)
        {
            Glyph = glyph;
            Name = name;
            Duration = duration;
            AccessibleName = accessibleName;
            StatusText = statusText;

            Foreground = progressStageStatus switch
            {
                ProgressStageStatus.Completed => Brushes.SeaGreen,
                ProgressStageStatus.Running => Brushes.RoyalBlue,
                ProgressStageStatus.Failed => Brushes.Firebrick,
                _ => Brushes.Gray,
            };

            FontWeight = progressStageStatus == ProgressStageStatus.Running ? FontWeights.SemiBold : FontWeights.Normal;
        }

        public string Glyph { get; }

        public string Name { get; }

        public string Duration { get; }

        public string AccessibleName { get; }

        public string StatusText { get; }

        public Brush Foreground { get; }

        public FontWeight FontWeight { get; }

        //What the ItemsControl's item peer is named by.
        public override string ToString()
        {
            return AccessibleName;
        }
    }
}
