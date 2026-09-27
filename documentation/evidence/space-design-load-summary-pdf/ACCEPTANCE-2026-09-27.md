# Space Design Load Summary PDF: acceptance (PR2D, 27 Sep 2026)

Base: SAM_UI `sow/2026-Q3` @ `c96ac19a`. SAM `sow/2026-Q3` @ `bb170cb8` (SAM#158, PR2C, merged), `SAM.sln` Release
rebuilt at it. SAM_Tas `fedf34cd` and SAM_Systems `2213373` at their merged heads; SAM_Tas rebuilt against that SAM.
No SAM or SAM_Tas change.

## 1. Automated tests
`SpaceDesignLoadSummaryPdfTests` (16, new):
- registration: an `AnalyticalWindow` is built and its Edit › Reports group holds exactly
  `RibbonButton_SpaceAssumptionsPdf`, `RibbonButton_SpaceDesignLoadSummaryPdf`, in that order, disabled with no model;
- the context-menu item: name, header, enabled for one Space, disabled with the reason for two;
- default file name `<Space> - Space Design Load Summary.pdf`, with the Phase-1 sanitising;
- entry point: the rendered document's id is `SpaceDocumentDefinitions.SpaceDesignLoadSummary.Id`, and its text is
  identical to `Create.SpaceDesignLoadSummary(context, space)` with no result source;
- units: SI gives W and no Btu/h, IP gives Btu/h and no W (the seam); the command itself passes the SI default;
- states: Ambiguous (Tas + OpenStudio heating) is reported as Ambiguous with neither load printed; no results →
  "Not simulated"; legacy `-1` results → "Peaks not recorded"; typed peaks → a real A4 PDF;
- the shared workflow, with the prompts replaced: none / two Spaces refused with this report's wording and no Save
  dialog; Save cancelled → nothing written, no message; success → "…saved: <path> — Open it now?" and Yes opens the
  file; a Document-stage failure is an error box naming the report; a locked destination is an Output error and the
  existing file is untouched.

`SpaceAssumptionsPdfTests` (27, Phase 1): assertions unchanged; only the calls moved to the shared names
(`WriteSpaceReportPdf(..., SpaceReportPdf.SpaceAssumptions)` etc.).

Full `SAM.Analytical.UI.WPF.Tests`: **1261/1261** (1245 before + 16).

## 2. Native UI (real `SAM_UI\build\SAM Analytical.exe`, real mouse, UIA/Win32 for dialogs)
Fixtures (`C:\TasOut\pr2d`, not committed): production SAM_Tas `Modify.AddResults` run on COPIES of
`C:\TasOut\final1b\open.tsd` (+ `open.sam`) and `C:\TasOut\pr3\final\bridge.tsd` (+ `pr3\a2\prepared.sam`), with the
PR2A fixture program (SAM `documentation/evidence/reporting-phase2-gate/pr2a/pr2a_fixture.cs.txt`). No-result model:
`C:\TasOut\rt\A1.sam`. Legacy: the unconverted `open.sam`.

| Step | Result |
|---|---|
| Edit › Reports shows *Space Assumptions PDF* then *Space Design Load Summary PDF* (`01`) | PASS |
| Tree context menu of a Space: new item directly under Space Assumptions (`02`) | PASS |
| Ribbon, nothing selected → "Select one Space, then choose Space Design Load Summary PDF." | PASS |
| View, two Spaces (Ctrl+click) → ribbon: "2 Spaces are selected. …one Space at a time…"; both report items disabled in the view menu (`03`) | PASS |
| Ribbon, one Space in the view → Save dialog "Save Space Design Load Summary PDF" → Cancel: no file, no message | PASS |
| **A** tree › Bathroom_2 (open.tsd) → save → "…saved … Open it now?" → Yes opens the PDF in the default viewer | PASS |
| A: PDF 1 page; heating 1,140 W design day (23:00–24:00) / 104 W full year (23 Dec 09:00–10:00); cooling 0 W both; source SAM.Analytical.Tas (`A1`) | PASS |
| A: ribbon › Studio 1_0 from the view selection → PDF is Studio 1_0 (genuine 0 W peaks reported) | PASS |
| A (cooled): tree › Studio 1_0 (bridge.tsd) → heating 2,268 / 802 W, cooling 1,973 / 1,972 W (3 Jul 19:00–20:00), 2 pages, as PR2C's case B (`A2`) | PASS |
| **B** no results → "Not simulated … This is not a zero load." for heating and cooling, status Not simulated, 1 page (`B`) | PASS |
| Legacy results → "Peaks not recorded … Re-run the simulation…", 1 page (`C`) | PASS |
| Regression: tree › Space Assumptions PDF → same flow, 1-page Space Assumptions | PASS |
| No runtime or dependency error in any step | PASS |

**SI/IP.** SAM_UI has no unit-system option (Phase 1 added none), so the command passes the reporting default, SI:
every native PDF is in W / °C. IP propagation through the shared seam is proven by the unit tests above.

Not re-checked natively (unchanged shared code, unit-tested): the locked-destination error box.

Known, not PR2D: the design-criteria set points print the thermostat sentinels (−50 / 150 °C) of these fixtures -
the Phase-1 set-point sentinel follow-up.
