# The SAM progress-dialog pattern

The recommended way for SAM_UI to show a long-running operation. Use it for any operation that takes more than a
few seconds, has stages, or can be cancelled.

**Reference implementation: the Part O progress window.** Read these first:

| Part | Source |
|---|---|
| Window (XAML) | [`WPF/SAM.Analytical.UI.WPF/Windows/PartOProgressWindow.xaml`](../WPF/SAM.Analytical.UI.WPF/Windows/PartOProgressWindow.xaml) |
| Window (code-behind: 500 ms render timer, Cancel latch) | [`PartOProgressWindow.xaml.cs`](../WPF/SAM.Analytical.UI.WPF/Windows/PartOProgressWindow.xaml.cs) |
| State (stages, times, percentage only from a real count, note text) | [`Classes/PartO/PartOProgressState.cs`](../WPF/SAM.Analytical.UI.WPF/Classes/PartO/PartOProgressState.cs) |
| Host (own UI thread, cancellation token, `AllowCancel` scopes, nested steps report into it) | [`Classes/PartO/PartOProgressHost.cs`](../WPF/SAM.Analytical.UI.WPF/Classes/PartO/PartOProgressHost.cs) |
| Shared look (neutral, extracted unchanged from the Part O window) | [`WPF/SAM.Core.UI.WPF/Themes/ProgressStyles.xaml`](../WPF/SAM.Core.UI.WPF/Themes/ProgressStyles.xaml) |
| Stage row (glyph, name, time, accessible line, colour) | [`WPF/SAM.Core.UI.WPF/Classes/ProgressStageRow.cs`](../WPF/SAM.Core.UI.WPF/Classes/ProgressStageRow.cs), built by [`Create.ProgressStageRows`](../WPF/SAM.Analytical.UI.WPF/Create/ProgressStageRows.cs) |
| Tests | `PartOProgressConsistencyTests`, `ProgressDialogPatternTests` |

It is used by:

- every Part O operation: Prepare & Run, Review Results, Iteration 2B, Iteration 3, Build & Run Mixed Design;
- Export Space Reports ([`SpaceReportPdfBatchWindow`](../WPF/SAM.Analytical.UI.WPF/Windows/SpaceReportPdfBatchWindow.xaml));
- Print Room Data Sheets from the ribbon ([`Modify.PrintRoomDataSheetsWithProgress`](../WPF/SAM.Analytical.UI.WPF/Modify/PrintRoomDataSheetsWithProgress.cs)).


## Anatomy

```
Heading                         what is running, in the engineer's terms; at the end, how it ended
Run summary                     one line: the case, the scope, the counts ("4,995 Spaces × 2 reports = 9,990 PDFs")
✓ Stage one                 6s  completed      (green)
● Stage two · 822 of 9,990  9s  running now    (blue, semibold); "· activity" = a real count or pass, if any
○ Stage three                   upcoming       (grey); – = not needed; ✕ = did not complete (red)
    the step inside the running stage (italic)
▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬ thin bar
Elapsed 9s                  8%  percentage only where real
Note: what the number means and when Cancel takes effect.   [Cancel]
```

- **Times align right**, one per stage that ran. Never a time remaining: nothing here knows one.
- **Status is never carried by the glyph or its colour alone.** Each row's accessible name is the line in words:
  "Completed: TAS simulation (full year)".
- **Cancel sits bottom right, level with the last line of the note.** Once pressed it reads "Cancelling…" and is
  disabled. Where Cancel is withdrawn for a stretch, it stays in view, disabled, and the note says when it is offered.
- Styles: `SAM.Progress.Heading`, `.Subheading`, `.Stages` (with `.StageRowTemplate`), `.Detail`, `.Bar`,
  `.Status`, `.Note`, `.CancelButton`. Merge
  `<ResourceDictionary Source="/SAM.Core.UI.WPF;component/Themes/ProgressStyles.xaml"/>` and bind the stage list
  to `Create.ProgressStageRows(state)`. Do not copy the markup.

## Behaviour

### Known percentage

Show a percentage only where the work reports a real count: items done out of items there
(`PartOProgressState.Report(completed, total)`). Never derive one from time, from stage positions or from the number
of messages. The percent is rounded down, so 100% means all of it. It resets whenever a stage starts or ends.
*Example:* the Space report batch reports documents written out of documents planned.

### No percentage available

The bar is indeterminate, and the note says why: "No percentage is shown: TAS does not report progress inside a
simulation." Show what is known instead:

- which stage is running;
- how long each stage took;
- the step named inside the stage (`Detail`);
- the elapsed time.

*Examples:* every TAS-backed Part O stage; Print Room Data Sheets (Excel reports nothing); the batch's preparation stage.

### Multi-stage workflows

List every stage up front, in order, so the user sees what is coming.

- `Start(i)` completes any earlier running stage and marks unstarted earlier stages "not needed", so the list never
  shows two stages running.
- `Activity` adds a pass or count to the running stage's name ("round 2", "822 of 9,990"). It stays on the stage
  once the stage ends, so the list shows how far the stage got.
- `SkipUnstarted` marks stages that will not run when the operation ends early.

### Safe-point cancellation

Cancel is cooperative. The work observes the token only at safe points: between workflow steps, or between documents.
Anything already in flight finishes first, whether a TAS call or the PDF being written. The note must say exactly
that, in the operation's own terms:

- Part O: "Cancel takes effect at the next safe point between steps; a TAS step already running finishes first."
- Space reports: "Cancel takes effect at the next safe point, between documents: the PDF being written finishes and
  is kept, and the log is still written."

After Cancel is pressed, the note becomes "Cancel requested. …". An operation with stretches that never observe the
token offers Cancel only inside `PartOProgressHost.AllowCancel()` scopes, so Cancel is never accepted and then
ignored. An operation that cannot stop says "It cannot be cancelled." and shows no Cancel button.

### Completed, current and failed stages

| Glyph | Words | Colour | Time shown |
|---|---|---|---|
| ✓ | Completed | green | yes |
| ● | Running now | blue, semibold | yes, live |
| ✕ | Did not complete (failed or cancelled) | red | yes |
| – | Not needed | grey | no |
| ○ | Upcoming | grey | no |

### Final summary state

Where the window stays open after the work (the Space report batch):

- the heading says how it ended: "Space reports exported", "…, with failures", "Export cancelled" or
  "…could not be exported";
- the clock stops;
- the stage rows keep their times and final activity ("4,995 of 4,995", "stopped after 884 of 9,990");
- the bar stays where the work reached, based on the result's own count;
- the note gives the one thing to know next ("The log in the output folder lists every document.");
- the totals and the result actions (Open folder / Open log) sit under the progress, and Cancel becomes Close.

A window that exists only for the duration of the work (the Part O host) closes when the work ends, and the
operation's result window or message takes over.

### Threading and render cadence

- **The work holds the application's thread and must be cancellable** (TAS COM in Part O): host the window on its own
  UI thread with `PartOProgressHost`, so it keeps painting and Cancel keeps answering.
- **Adopting the pattern for an existing window is a style change, not a threading change.** Keep the window where
  the replaced one was. For example, Print Room Data Sheets keeps its window on the application's thread (not
  topmost, no owner, not in the taskbar), activated and repainted between stages exactly as its old
  `ProgressWindow` was. Moving it to its own thread is a behaviour change, and it needs its own acceptance (with
  Excel).
- **The work runs on a background task** (the Space report batch): the window lives on the application's thread and
  updates from `IProgress<T>`.
- **Either way, render on a timer (500 ms), never per progress tick.** The work updates the state object; the window
  reads it. Rebuilding the stage rows on every per-document tick (hundreds a second) flooded the dispatcher during
  this PR's first attempt: Cancel took about 10 s to answer instead of 0.06 s.

## Audit of SAM_UI reporting / export / print progress (September 2026)

| Surface | Before | Classification | Now |
|---|---|---|---|
| Part O progress (`PartOProgressWindow`/`State`/`Host`) | the pattern | already consistent (reference) | draws with the extracted shared resources; unchanged look |
| Export Space Reports (`SpaceReportPdfBatchWindow`) | status text + 6 px bar | standardise now | the pattern, with determinate percentage and the final summary state |
| Print Room Data Sheets, ribbon (`PrintRoomDataSheets`) | SAM `ProgressWindow("Print RDS", 4)` on the UI thread | standardise now (style only) | `PartOProgressWindow` in the shared style (4 stages, no percentage, not cancellable), driven as the old window was: same thread, not topmost, no owner, repainted between stages |
| Print RDS inside the TAS `Simulate` workflow; Grasshopper Print RDS / Print AHU | old window / none | not applicable here | unchanged: a nested step of another workflow, or a Grasshopper host |
| Space Assumptions PDF / Space Design Load Summary PDF (one Space) | message boxes; about a second | not applicable | unchanged |
| Part O TM59 / Iteration 3 report saves | file writes inside Part O flows | not applicable | unchanged |
| `ReportWindow` (text viewer) | a viewer, not progress | not applicable | unchanged |
| Model export (hbjson/tbd/gbXML/GEM), and the generic "Loading" window (`ProgressBarWindowManager`) shared with Import, U-value calculations and others | indeterminate busy window | not applicable in this PR | unchanged; see next steps |

## Recommended next steps (not done here)

1. **Neutral names.** `PartOProgressState`, `PartOProgressStageStatus` and `PartOProgressHost` are generic apart
   from their names, the TAS wording in `PartOProgressState.Note` and the host's Part O default title. A mechanical
   rename to `ProgressState` / `ProgressHost`, with a Part O note provider, belongs in its own PR. It touches every
   Part O command, so it was kept out of this UX change.
2. **`ProgressBarWindow`** (SAM.Core.UI.WPF) could render the pattern's single-stage form: heading, indeterminate
   bar, elapsed time, and a note saying it cannot be cancelled. That would bring model export, import and the other
   generic busy windows in line. It is shared by non-reporting commands, so change it deliberately.
3. **Print RDS off the UI thread.** Its window still freezes while Excel works, exactly as before. Hosting it on its
   own thread would keep it painting, but that is a behaviour change that needs an acceptance run with Excel.
4. **Print RDS inside `Simulate`** can adopt the stage callback
   (`PrintRoomDataSheets(model, directory, owner, stage)`) once the Simulate workflow shows its own progress in the
   pattern.
