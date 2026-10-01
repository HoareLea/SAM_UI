# Batch export of Space report PDFs in SAM_UI (SAM Documentation Framework PR2F-2)

**Export Space reports...** writes the Space Assumptions and/or Space Design Load Summary PDF for many Spaces (the
selection, or every Space in the model) into one folder, with one log. It exports the same two reports as the one-Space
commands (`documentation/Reporting-SpaceAssumptionsPdf.md`, `documentation/Reporting-SpaceDesignLoadSummaryPdf.md`).
Those commands are unchanged and are still the quick route for one Space.

SAM_UI only orchestrates. Documents, units (SI, the reporting default), notes and layout are SAM's
(`SAM.Analytical.Reporting`, `SAM.Core.Reporting.Pdf`). The design came from SAM
`documentation/Reporting-PR2F-Review.md` §6. The batch API is SAM#163 (`DocumentContext.WithNewDiagnostics()`).

## Where it is
- Ribbon: Edit › Reports › **Export Space Reports** (third button). It opens with the active view's Space selection.
  With nothing selected, the scope is All Spaces, so a user never has to select thousands of Spaces.
- Context menu of the 3D/2D view and of the model tree, for one or many Spaces: **Export Space reports...**. The scope
  defaults to Selected Spaces.
- The one-Space items (Space Assumptions PDF, Space Design Load Summary PDF) are still disabled for several Spaces.

## The window (`SpaceReportPdfBatchWindow`)
- Reports: Space Assumptions, Space Design Load Summary. At least one is required; neither is ticked by default.
- Spaces: Selected Spaces (n), or All Spaces (N). The Spaces are counted, never listed, so the window stays light at
  5,000 Spaces.
- Output folder: "&lt;model name&gt; Space reports" beside the saved model, or Browse... (`OpenFolderDialog`). The batch
  creates the folder.
- Export runs the batch on a background task. The progress follows the SAM progress-dialog pattern
  ([ProgressDialogPattern.md](ProgressDialogPattern.md)): a heading, the run summary ("4,995 Spaces × 2 reports (...)
  = 9,990 PDFs"), two stage rows with their times ("Prepare the model snapshot and plan the PDFs", "Write the PDFs
  and the log · 822 of 9,990"), the document in hand ("Space 412 / 4,995 · Space Assumptions · Bedroom 2_6 #45"), a
  bar with the real percentage, the elapsed time, and a note beside Cancel saying when Cancel takes effect. At the
  end the heading and the stage rows say how it ended (exported / exported, with failures / cancelled).
- Cancel stops between documents. Closing the window while it runs does the same, and the window closes once the
  log is written.
- At the end it shows totals, the first three failures and "...and N more failures: see the log", plus
  **Open folder** and **Open log**. Successful files are never listed.

## Architecture
| Piece | Role |
|---|---|
| `Modify.WriteSpaceReportPdf(DocumentContext, Space, path, SpaceReportPdf, renderer)` | New seam: build → render → stage `.tmp` → move. The existing `AnalyticalModel` overload now builds the context and delegates (single-Space behaviour unchanged). |
| `SpaceReportPdfBatch.Create(model, spaceGuids or null, reports, folder)` | Builds one shared `DocumentContext`, resolves Spaces from it (model order), plans the file names. |
| `SpaceReportPdfBatch.ExistingCount()` / `Run(policy, IProgress, CancellationToken, renderer)` | For each Space, for each report: `sharedContext.WithNewDiagnostics()` → one PDF on disk → one log line. Sequential by design. |
| `SpaceReportPdfBatchResult` / `SpaceReportPdfBatchItem` / `SpaceReportPdfBatchProgress` | Structured outcome: Created / Skipped / Failed / Not started, Cancelled, per-document path, stage, message, exception, notes. |
| `Query.SpaceReportPdfFileNames(spaces, reports, folder)` | Deterministic collision-safe names. |
| `Create.MenuItem_SpaceReportPdfs`, `Modify.ExportSpaceReportPdfs` | Menu entry and window launcher. |

**One shared context, not an isolated snapshot.** `Create.DocumentContext` reads `AnalyticalModel.AdjacencyCluster`
once. That read is a shallow copy: a fresh cluster wrapper, but the same Space/Panel object references as the live
model - so it shares the model's objects rather than isolating them. The batch does that once, and every document's
`WithNewDiagnostics()` reuses that one context - cluster, profile library, options, formatter and provenance - and
only starts a fresh, empty `Log`. So each document's notes are its own, and match what the one-Space command gives
for that Space (tested). This is safe here because **Export Space reports...** is a modal window: nothing else can
mutate the model while the batch runs. The batch does not itself defend against a concurrent mutation, and none is
expected while the window is open.

**No parallelism.** The PDFsharp font resolver is process-global. Every PDF is on disk before the next is built. Only
a small result per document is kept, never documents or bytes (tested with weak references).

**The output folder is required up front.** `Run` creates the output folder before writing anything. If that fails
(for example, a file already sits at that path, or it is not writable), `Run` throws immediately rather than writing
every document and recording an `Output` failure for each. The window's existing "stopped" handling for a failed
`Run` covers this the same way it covers any other exception from `Run`.

## File names
1. Start from the one-Space name `"<Space name> - <report>.pdf"` (sanitised, reserved names prefixed `_`, 150-character
   Space part, `"Space <Guid>"` when there is no usable name).
2. Shorten the Space part further only if the folder is long. The staged path (`<name>.tmp`) must stay within 259
   characters, with room reserved for a full-Guid suffix. A folder too long for a 16-character Space part is refused
   before anything is written.
3. Group names ignoring case. **Every** member of a colliding group gets `" [first 8 hex of Space Guid]"` before
   `.pdf`, e.g. `Office - Space Design Load Summary [a1b2c3d4].pdf`. This covers duplicate names, case-only duplicates,
   and names equal after sanitising or truncation.
4. A name that still collides (two Guids sharing 8 hex digits) gets the full 32-digit Guid.

The names depend only on the set of Spaces and never on their order, so they are the same across runs. Adding an
unrelated Space renames nothing. Unique names are exactly the one-Space command's names.

## Existing files
The batch asks once, and only when planned PDFs already exist: "N of the M PDFs are already in the output folder.
Yes: overwrite them. No: skip them and keep the existing PDFs. Cancel: do not export." The policy applies to the whole
run. When nothing existed at planning time, the policy is Skip, so a PDF that appears mid-run is never silently
overwritten. An overwrite is staged, so a failed one keeps the old PDF.

## Failures and cancellation
- A failure is recorded with its stage and the batch goes on. Stages: `Document` (SAM threw), `Rendering`,
  `Output` (locked or unwritable), and `Selection` (a selected Space is no longer in the model). A failing Space or
  report type never stops later ones.
- The writer removes its `.tmp` on any failure, and a failed document never leaves a final PDF.
- Cancellation is checked before each document. The document in hand finishes, completed PDFs are kept, and the
  log ends `CANCELLED after n of N documents`.
- A log that cannot be written never stops the PDFs. The summary then says why.

## Log (`Space reports yyyy-MM-dd HHmmss.log` in the output folder)
```
SAM Space report export
Run started:     2026-09-28 10:04:09
Model:           <model name>
Scope:           Selected Spaces
Spaces:          3
Report types:    Space Assumptions, Space Design Load Summary
Units:           SI
Output folder:   C:\...\bridge_peaks Space reports
Existing files:  Skip
Documents:       6

[1/6] Created | Studio 1_0 (<guid>) | Space Assumptions | Studio 1_0 - Space Assumptions.pdf
    note: <this document's reporting diagnostics>
[2/6] Skipped | ... | A PDF is already at this path.
[n/N] FAILED | ... | stage: Output / error: ... / exception: <type>: <message>
Run finished:    2026-09-28 10:04:10 (elapsed 00:00:00)
Result:          Completed | CANCELLED after n of N documents
Created: 3  Skipped: 3  Failed: 0  Not started: 0
```
The log holds exception types and messages, including inner exceptions, but no stack traces. Full exceptions go to
`Trace.TraceError`.

## Tests (`WPF/SAM.Analytical.UI.WPF.Tests/SpaceReportPdfBatchTests.cs`)
There are 40 tests, plus the opt-in scale harness `SpaceReportPdfBatchScaleHarness.cs`. They cover:
- the writer seam matches the model path for both reports;
- one or both report types;
- selected and all Spaces;
- a removed Space;
- a single shared context, with per-document diagnostics isolation;
- the output folder could not be created or accessed: `Run` fails fast, before any document, instead of one `Output`
  failure per document;
- a failing Space, a failing report type, and Rendering and Output stages with no `.tmp` left;
- overwrite and skip;
- cancellation mid-run and before the first document;
- log contents, and two logs in one second;
- the planner: duplicates, case, sanitising, reserved names, trailing dot or space, truncation, empty names, Guid-prefix
  escalation, order independence, stability, long folders, a folder that is too long;
- a scale regression: 400 Spaces through the real document build, nothing retained, flat time per document, timing
  logged and not asserted;
- the window: defaults, gating, an end-to-end export with one existing-files prompt, skip and cancel, open folder,
  summary text; the ribbon and menu items.

## Acceptance (28 Sep 2026, dev build `SAM_UI\build\SAM Analytical.exe`, driven by UI Automation)
Evidence is not included in this repository.
- **bridge_peaks.sam, All Spaces (9), both reports:** 18 PDFs in 0.8 s. Re-run with Skip: 0 created, 18 skipped.
  Overwrite: 18. Cancel at the prompt: nothing written, no new log. A PDF locked by another process: 17 created,
  1 FAILED (`stage: Output`), the locked file unchanged, no `.tmp`.
- **Tree multi-selection (3 Spaces):** the context menu has Export Space reports... enabled and both one-Space items
  disabled. Scope defaults to Selected Spaces (3). One report gives 3 PDFs. Both reports with Skip gives 3 created
  and 3 skipped.
- **One Space selected:** the one-Space commands are enabled and unchanged. Their PDF text equals the batch PDF's,
  line for line, except the "Generated" timestamp. Spot-checked PDFs render correctly (A4, no clipped text).
- **open_peaks.sam, All Spaces:** 18 PDFs.
- **5k model** (`bridge_peaks.sam` × 555 = 4,995 Spaces, 26 MB `.sam`, every 50th copy keeps the original names):
  - The window opens in 1.6 s showing All Spaces (4,995).
  - Progress updates live.
  - Cancel: the summary shows within 0.37 s ("Cancelled after 774 of 9,990"; log CANCELLED; no `.tmp`).
  - Closing mid-run: the window closes in 0.27 s and the log says CANCELLED.
  - Full run: 9,990 PDFs in 1:44 in the app. Working set is flat at about 4.05 GB during the run (the app's model is
    3.1 GB before) and returns to 3.1 GB after.
- **Scale harness** (no UI, real renderer, 4,995 Spaces × 2 reports):
  - setup (the shared context + 9,990 names) 42 ms;
  - 96.7 s total, 9.7 ms per document;
  - 4.6-5.0 s per 500 documents from start to end (linear);
  - managed heap 270-460 MB sawtooth (bounded);
  - 216 collision-suffixed names; 0 failed; 0 `.tmp`.

  For comparison, the PR2F audit measured about 23 s for the shared-context document build alone, without
  rendering or writing, and about 2 minutes for the build alone with per-Space contexts.
