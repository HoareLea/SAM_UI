# Reporting hardening: DocumentContext terminology and output-folder fail-fast — PR record

Branch `feature/reporting-hardening-m1-m2-2026-09-28`, from `sow/2026-Q3` (`fc6e0861`).

**Status:** implemented, tested. PR open against `sow/2026-Q3`.

## Scope

A small, focused fix for two final-review findings (M1, M2) on `SpaceReportPdfBatch` /
`Analytical.Reporting.DocumentContext` (PR2F-2 batch Space report export). No architectural redesign, no change to
report content or batch semantics. M3 and M4 are out of scope.

## M1 — misleading "snapshot/model copy" terminology

**Finding.** `SpaceReportPdfBatch`'s doc comments and `documentation/Reporting-SpaceReportPdfBatch.md` described
`Create.DocumentContext` as taking a "snapshot" and "copying the model", which reads as an isolated copy. In fact
`AnalyticalModel.AdjacencyCluster` (read once, by `Create.DocumentContext`) is a shallow copy: a fresh cluster
wrapper, but the same `Space`/`Panel` object references as the live model. `WithNewDiagnostics()` reuses that one
`DocumentContext` for every document; it never re-reads the model. Verified against the SAM core source
(`SAM.Analytical.Reporting.DocumentContext`, `AnalyticalModel.AdjacencyCluster`,
`SAMObjectRelationCluster`/`RelationCluster` copy constructors): the copy is shallow all the way down unless the
`deepClone: true` overload is used, which this path does not use.

**Fix.** Reworded the class doc comment, the `Create` method doc comment, the `Spaces` property doc comment and the
inline comment in `Create` in `Classes/Reporting/SpaceReportPdfBatch.cs`, and the "One snapshot" section and related
text in `documentation/Reporting-SpaceReportPdfBatch.md`, to say plainly: one shared `DocumentContext`, built once,
reused by every document; the underlying `AdjacencyCluster` copy is shallow (new wrapper, same Space/Panel
references); and why that is safe here — **Export Space reports...** is a modal window, so nothing else can mutate
the model while the batch runs, and the class itself does not defend against a concurrent mutation. No code
behaviour changed. User-facing UI copy ("Prepare the model snapshot...", the progress-dialog stage name) is
unchanged: it is a plain-language label for end users, not a technical claim, and changing it is out of scope for a
docs-accuracy fix.

## M2 — one Output failure per document when the output folder cannot be created

**Finding.** `SpaceReportPdfBatch.Run` wrapped `Directory.CreateDirectory` and opening the log's `StreamWriter` in
one try/catch. If `CreateDirectory` failed (a file already at that path, no permission, etc.), the exception was
swallowed into `logError` and the method went on to attempt every document anyway; each one then failed at the
`Output` stage trying to write into a folder that does not exist, so a batch of N documents produced N near-identical
`Output` failures instead of one clear reason nothing could be written.

**Fix.** Split `Directory.CreateDirectory(Directory)` into its own try/catch, before the log file is opened and
before any document is attempted. On failure it now logs via `Trace.TraceError` and throws an `IOException` naming
the folder and the underlying error, instead of returning a result with many failed items. This reuses the window's
existing handling for a `Run` failure — `SpaceReportPdfBatchWindow.ExportAsync`'s existing catch around
`spaceReportPdfBatch.Run(...)` already shows the "Space report export stopped" state (one of the pattern's existing
final states: exported / with failures / cancelled / could not start / **stopped**) for any exception from `Run`; no
window code changed. Opening the log file (once the folder exists) is unaffected and keeps its previous behaviour:
a log that cannot be written does not stop the PDFs (per the existing "the log never stops the PDFs" design).

## Files changed

- `WPF/SAM.Analytical.UI.WPF/Classes/Reporting/SpaceReportPdfBatch.cs` — doc-comment wording (M1); split the
  directory/log try-catch and fail fast on a directory failure (M2).
- `documentation/Reporting-SpaceReportPdfBatch.md` — wording (M1); documented the fail-fast behaviour (M2).
- `WPF/SAM.Analytical.UI.WPF.Tests/SpaceReportPdfBatchTests.cs` — two new regression tests (below).
- `documentation/Reporting-SpaceReportPdfBatch-Hardening-PR.md` (new, this file).

## Tests

Two new tests, both passing alongside the existing 40 (batch) + window tests:

- `SpaceReportPdfBatchTests.TheOutputFolder_CannotBeCreated_FailsFast_BeforeAnyDocument` — a file sits where the
  output folder needs to be, so `CreateDirectory` fails; asserts `Run` throws `IOException` naming the path, the
  renderer was never invoked, no PDF or log was written, and the blocking file is untouched.
- `SpaceReportPdfBatchWindowTests.Export_WhenTheOutputFolderCannotBeCreated_StopsBeforeAnyDocument_WithOneMessage` —
  drives the window end to end: `Create`/`ExistingCount` both succeed (`Directory.Exists` is false for a file, so
  there is no existing-files prompt), the failure surfaces from `Run`, and the window ends in the existing "Space
  report export stopped" state with exactly one message box naming the folder; `window.Result` stays null.

## Validation

- `SAM_UI.sln` Debug and Release: 0 errors.
- `SpaceReportPdfBatchTests` + `SpaceReportPdfBatchWindowTests`: 42/42 passed (was 40; +2 new).
- Full WPF test project (`SAM.Analytical.UI.WPF.Tests`), Debug: **1397/1397** passed (was 1395; +2 new), 0 failed.

## Risks / unresolved

- None known. The change is additive (a fail-fast path that previously did not exist) and does not alter behaviour
  for any case that was already covered by tests (directory succeeds; log open fails; documents fail individually).

## Next step

Open the PR against `sow/2026-Q3`, wait for CI, merge when CI and review are clean.
