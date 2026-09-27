# Space Design Load Summary PDF in SAM_UI (SAM Documentation Framework Phase 2, PR2D)

SAM_UI's one-click entry to the Phase 2 **Space Design Load Summary** report: the simulated heating and cooling
peaks of one Space, design day and full year side by side. SAM_UI only orchestrates. The result reads, peak selection,
states, units and layout all come from SAM:
- `SAM.Analytical.Reporting` (`Create.DocumentContext`, `Create.SpaceDesignLoadSummary`);
- `SAM.Core.Reporting.Pdf` (`PdfRenderer`).

Report contract: SAM `documentation/Reporting-Phase2-ResultAuthority.md` §9.1.

## One workflow for both reports
The command is the Phase-1 **Space Assumptions PDF** workflow with a different report
(`documentation/Reporting-SpaceAssumptionsPdf.md`). Both go through the same code:
- `SpaceReportPdf` names a report and its SAM entry point: `SpaceReportPdf.SpaceAssumptions`,
  `SpaceReportPdf.SpaceDesignLoadSummary`;
- `Modify.CreateSpaceReportPdf` (UI flow) and `Modify.WriteSpaceReportPdf` (the testable seam);
- `Query.SpaceReportPdfSpace` and `Query.SpaceReportPdfFileName`;
- `Create.MenuItem_SpaceReportPdf`;
- `SpaceReportPdfResult` / `SpaceReportPdfFailure`;
- `SpaceReportPdfPrompts` (internal): the Save dialog, message boxes and "open", replaceable in tests.

So selection, multiple selection, the Save dialog (model folder, overwrite prompt, cancel), file naming, units, the
staged write, the error stages and "Open it now?" are identical, and only the report's name changes in the text.

## Where the command is
Next to Space Assumptions PDF in the same three places:
- Ribbon: Edit › **Reports** › **Space Design Load Summary PDF** (the active view's selection);
- the 3D/2D view's context menu;
- the model tree's context menu for a Space.

## Result sources
The command passes **no result source**. Every source is read, and when more than one records peaks the report says
**Ambiguous** and prints no load; SAM_UI never picks a source (not the first, the largest, Tas or OpenStudio).
Choosing a source explicitly is a possible later UX step, through `Create.SpaceDesignLoadSummary`'s `resultSource`.

## Missing results are not errors
No simulation, legacy results without peaks, genuine zero loads and ambiguous sources all produce a PDF: the report
prints *Not simulated*, *Peaks not recorded*, `0 W` or *Ambiguous*. Only a software failure (Document / Rendering /
Output) stops the command with an error.

## Units
As Space Assumptions: SAM_UI has no unit preference, so the reporting default (SI) is used. `WriteSpaceReportPdf`
takes a `UnitStyle` for a later option; IP is tested there.

## Not in PR2D
No provenance stamping or freshness workflow ("Matches current model" prints what the data carries, today "not
recorded"); no source picker; no batch reports. Nothing in the report command modifies the model.

## Evidence
`documentation/evidence/space-design-load-summary-pdf/ACCEPTANCE-2026-09-27.md`.
