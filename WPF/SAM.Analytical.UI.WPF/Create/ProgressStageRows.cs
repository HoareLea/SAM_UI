// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.UI.WPF;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Create
    {
        /// <summary>
        /// The stage rows of a progress window, as <see cref="PartOProgressWindow"/> shows them: the glyph, the
        /// stage's label, its time (for a stage that started) and the line in words. Shared by every window that
        /// follows the SAM progress-dialog pattern (documentation/ProgressDialogPattern.md).
        /// </summary>
        public static List<ProgressStageRow> ProgressStageRows(PartOProgressState partOProgressState)
        {
            List<ProgressStageRow> result = [];

            if (partOProgressState is null)
            {
                return result;
            }

            for (int i = 0; i < partOProgressState.Count; i++)
            {
                PartOProgressStageStatus partOProgressStageStatus = partOProgressState.Status(i);
                TimeSpan? duration = partOProgressState.Duration(i);

                result.Add(new ProgressStageRow(
                    PartOProgressState.Glyph(partOProgressStageStatus),
                    partOProgressState.Label(i),
                    duration.HasValue && partOProgressStageStatus != PartOProgressStageStatus.Pending && partOProgressStageStatus != PartOProgressStageStatus.Skipped ? PartOProgressState.Format(duration.Value) : string.Empty,
                    partOProgressState.AccessibleLine(i, false),
                    PartOProgressState.StatusText(partOProgressStageStatus),
                    partOProgressStageStatus switch
                    {
                        PartOProgressStageStatus.Running => ProgressStageStatus.Running,
                        PartOProgressStageStatus.Completed => ProgressStageStatus.Completed,
                        PartOProgressStageStatus.Failed => ProgressStageStatus.Failed,
                        PartOProgressStageStatus.Skipped => ProgressStageStatus.Skipped,
                        _ => ProgressStageStatus.Pending,
                    }));
            }

            return result;
        }
    }
}
