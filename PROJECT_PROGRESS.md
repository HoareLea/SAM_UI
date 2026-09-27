# Project Progress

## Current: SAM Documentation Framework Phase 2 - PR2D Space Design Load Summary PDF command (27 Sep 2026) - PR open

**Status.** [SAM-BIM/SAM_UI#127](https://github.com/SAM-BIM/SAM_UI/pull/127), branch
`feature/pr2d-space-design-load-summary-ui-2026-09-27` from `sow/2026-Q3` `c96ac19a`. PR2A/PR2B/PR2C are complete in
SAM/SAM_Tas (SAM#153, SAM_Tas#69, SAM#156, SAM#158 merged; SAM `bb170cb8`). No SAM or SAM_Tas change in PR2D.
Merge is done by the owner if the auto-mode classifier blocks `gh pr merge` (it has for every recent PR).

**What.** Edit › Reports › **Space Design Load Summary PDF** (ribbon, view and tree context menus), next to Space
Assumptions PDF. It calls `Create.SpaceDesignLoadSummary(context, space)` with **no result source** and the existing
`PdfRenderer`; Ambiguous stays Ambiguous, NotSimulated / PeaksNotRecorded / 0 W still produce a PDF.
- One shared workflow, no copy: `SpaceReportPdf` (report + SAM entry point: `SpaceAssumptions`,
  `SpaceDesignLoadSummary`), `Modify.CreateSpaceReportPdf` / `WriteSpaceReportPdf`, `Query.SpaceReportPdfSpace` /
  `SpaceReportPdfFileName`, `Create.MenuItem_SpaceReportPdf`, `SpaceReportPdfResult` (renamed from `SpaceAssumptions*`),
  internal `SpaceReportPdfPrompts` (Save dialog / messages / open, replaced in tests).
- Units: SI default, as Phase 1 (SAM_UI has no unit option). Default name `<Space> - Space Design Load Summary.pdf`.
- No provenance stamping; the reporting command does not modify the model (checked: nothing in the path writes).

**Files.** `Classes/Reporting/SpaceReportPdf.cs`, `SpaceReportPdfPrompts.cs` (new), `SpaceReportPdfResult.cs`,
`Modify/SpaceReportPdf.cs`, `Query/SpaceReportPdf.cs`, `Create/MenuItem_SpaceReportPdf.cs` (renamed from the
`SpaceAssumptionsPdf` files), `Windows/AnalyticalWindow.xaml(.cs)`, `Controls/AnalyticalModelControl.xaml.cs`; tests
`SpaceDesignLoadSummaryPdfTests.cs` (new, 16), `SpaceAssumptionsPdfTests.cs` (calls renamed only), tests csproj
(`SAM.Analytical.Reporting` reference); docs `documentation/Reporting-SpaceDesignLoadSummaryPdf.md` (new),
`Reporting-SpaceAssumptionsPdf.md` (code names); evidence `documentation/evidence/space-design-load-summary-pdf/`.

**Validation.** `SAM.sln` Release rebuilt at `bb170cb8`, `SAM_Tas.sln` at `fedf34cd`, `SAM_UI.sln` Release 0 errors, no
new warnings. Focused 43/43; WPF **1261/1261**. Native UI on the real exe (record `ACCEPTANCE-2026-09-27.md`): placement,
nothing selected, two Spaces, Save cancel, peaks (Bathroom_2 1,140/104 W; cooled Studio 1_0 from bridge.tsd), no
results → Not simulated, legacy → Peaks not recorded, Yes opens the PDF, Space Assumptions regression - all PASS. IP is
proven at the `WriteSpaceReportPdf` seam only (no UI selector). Fixtures in `C:\TasOut\pr2d` (not committed; recipe
in the acceptance record).

**Known, not PR2D.** Design-criteria set points print thermostat sentinels (−50 / 150 °C) - the Phase-1 set-point
sentinel follow-up. SAM#138, SAM#154, source picker, freshness/provenance: separate.

**Next step.** After #127 merges: PR2E - SAM_Deploy pointer move to the merged SAM_UI (and SAM `bb170cb8`) +
installed-product smoke test of both report commands.

## Parallel programme: Mixed Part O dwelling strategies - PR2 SAM_UI dwelling strategies + mixed-model workflow (27 Sep 2026) - MERGED (SAM_UI#126 `2a341255`)

**Branch** `feature/parto-mixed-strategies-pr2` from `sow/2026-Q3` `c96ac19` (PR0 merged). Builds against SAM
`sow/2026-Q3` `0f866ec6` (PR1 = SAM#150 merged `3de02102`; closeout SAM#151). **SAM must be built at `0f866ec6` or
later** (`SAM.sln` Release) - SAM_UI now uses `MaterialisePartODwellingStrategies`, `PartODwellingStrategySet` etc.
SAM_Tas / SAM_Systems unchanged. Full record: `documentation/PartO-MixedDwellingStrategies-PR2.md`.

- **What:** Simulate › Part O › **Mixed Design** (new ribbon button; Prepare & Run and 1a/1b/2/2B/3 untouched). One
  window: virtualised matrix, one row per dwelling (SAM's dwelling rule; common zones not rows); screening columns
  (evidence), Suggested, Selected (authority), Final TM59; search / filter / group; bulk bar (Natural, MVHR +
  product, retain baseline design, clear, Apply suggestions… with a preview whose default is Cancel); project
  constraints; simulation case; Screen strategies… (optional), Check design, Open final TM59 result…, Save selection,
  Build & Run Mixed Design.
- **Key decisions:**
  - Screening AND the final run both go through SAM PR1 `MaterialisePartODwellingStrategies` (screening = a copy with
    a homogeneous set: Natural=1b, MVHR baseline=1a generic, Selected-product=2 with catalogue). No second Part O
    implementation; no legacy `PreparePartOIteration` in the mixed route. Optimised (2B) screening = UNAVAILABLE in
    PR2; cooling = gated.
  - Minimum screening skips only whole strategies (not permitted / nothing left); out-of-scope cells are NOT RUN.
  - Open model = baseline + selection; its ONLY write is Save selection. Runs use private `PartORun`s over
    materialised copies (capacity-envelope pattern); run model named `<model>_Mixed` / `<model>_Screen_<strategy>`.
  - Persistence: strategies on the model (SAM). Everything else in sidecar `<model>.partomixed.json`
    (`PartOMixedDesign:v1`) - NOT on the model, because the record's baseline fingerprint digests every model
    parameter. Deviation from PR0 F's "`PartORunResume:v3`" - documented in the PR2 record §7.
  - Final staleness = SAM `PartOMaterialisationRecord.IsCurrent` + TSD length/write time + unsaved draft. Screening
    staleness = `Query.PartOScreeningDesignFingerprint` (SAM digest minus the strategy set) + catalogue fingerprint.
  - Constraints (natural allowed, optimisation allowed) filter the suggestion and gate assignment; stored in the
    sidecar; product pool = existing `PartOEquipmentSelection`, read-only here.
- **Changed existing code (additive):** `PartOTM59Assessment.OccupiedSpaceStatuses` (+ optional ctor arg);
  `RunPartOSimulation` skips `PersistPartORunResume` for a model with a `PartOMaterialisationRecord`;
  `AnalyticalWindow` ribbon button.
- **New files:** SAM.Analytical.UI - `Enums/PartOScreeningStrategy|PartODwellingOutcome|PartOScreeningMode.cs`,
  `Classes/PartO/Mixed/PartODwellingResult|PartOScreeningEvidence|PartOMixedRunEvidence|PartOMixedDesignConstraints|
  PartOMixedDesignState|PartODwellingSuggestion.cs`, `Query/PartOMixedDesign.cs`; WPF -
  `Classes/PartO/Mixed/PartOMixedDwellingRow|PartOMixedDesignSession.cs`, `Modify/RunPartOStrategySet|
  ScreenPartODwellingStrategies|RunPartOMixedDesignCommand.cs`, `Query/PartODwellingResults.cs`,
  `Windows/PartOMixedDesignWindow|PartOScreeningWindow|PartOMixedChangesWindow.xaml(.cs)`; tests
  `PartOMixedDesignFixture|RunTests|SessionTests|ScreeningTests|ScalingTests.cs` (34 tests, fake TAS).
- **Validation (27 Sep, this machine):** SAM `SAM.sln` Release at `0f866ec6` 0 errors; `SAM_UI.sln` Release 0 errors;
  mixed tests 34/34; full WPF suite **1279/1279** (1245 before + 34). One earlier full run had a single timing flake in
  untouched code (`The_progress_window_keeps_its_content_after_standing_aside_for_a_dialog`) that passed alone and on
  the re-run. Scale (500 dwellings, 5,000 spaces, no TAS): open 0.42 s, window 15 rows realised of 500 (14
  grouped), SAM materialisation 3.4 s.
- **Self-test 27 Sep (no production code changed; head still `c5f59bf`)** - record
  `documentation/evidence/parto-mixed-pr2-acceptance/INVESTIGATION-2026-09-27.md` (shots, logs, UIA driver scripts):
  - **Native UI + real licensed TAS on this machine**: 19 behaviours PASS - manual design, Check, Build & Run (one
    combined model, one annual run, TM59), edit→STALE, rebuild from the clean baseline (proved: NV dwelling gets its
    authored ICs back), final TM59 reopen, minimum screening (3 real runs; NOT RUN where skipped; selection unchanged),
    Suggested vs Selected + Apply preview/Cancel/Apply, bulk multi-select, Save = baseline + strategies only, restart
    restore, TSD-rewrite STALE, SAM refusal (DesignDiffersFromRequirement) on the row, **Optimised MVHR in one combined
    run** (F1 NV / F2 XBC15 / F3 retained real 2B 143/95/48 l/s), dirty model refused.
  - **Test model**: owner's `SAM_daily/2026-07-15 PartO/SAM_zoningAM-CIBSEfutureZ1.sam` is NOT clean (results, design
    days, MVHR 1-3, Part F IC clones, shared MV 1/AHU1; corridor IC `Studio`). Test fixture derived headlessly (Map IC
    TM59 + remove run output/plant) → `C:\TasOut\parto-mixed-pr2-2026-09-27\fixtures\` (local, not committed);
    reproducible with env-gated `PartOMixedDesignInvestigationTests` (`SAM_PARTO_MIXED_INVESTIGATION`, `_MODEL`,
    `_BASELINE`, `_2B`). The UI cannot clean it: Results›Remove leaves ZoneSimulationResults + cluster design days.
  - **2B gap**: SAM PR1 complete for `RetainedDesign`; PR2 can select it; MISSING = the explicit "accept optimised
    airflow for a dwelling" edit (clean baselines have no terminals) and any 2B source in the mixed route. Smallest seam
    proven with real 2B data: `RealizePartFVentilationTerminals`(dwelling) + `SetSpaceDesignFlowRate` per space/direction
    from a 2B result model (matched by space guid) → strategy `RetainedDesign` + fingerprint.
  - **Codex review on c5f59bf: 2 P1 + 2 P2 unaddressed** (catalogue toggle doesn't stale final; run verdict ignores
    auto-assessed corridor; project test unit not a product; unreadable sidecar result dropped). CI build + spdx green.
- **Correction pass 27 Sep (after the owner's review of the self-test):**
  - **SAM PR [SAM-BIM/SAM#152](https://github.com/SAM-BIM/SAM/pull/152)** `feature/parto-accept-dwelling-design` `b8dd64dd`
    (from `sow/2026-Q3` `0f866ec6`): `Modify.AcceptPartODwellingDesign` (lineage by `PartFTerminalReference.Matches`,
    `RealizePartFVentilationTerminals` for that dwelling, `SetSpaceDesignFlowRate` per space/direction; nothing in the
    strategy; non-clean baseline refused). 9 tests; SAM.Tests 2544/2544. **Must merge before SAM_UI#126 CI can pass**
    (SAM_UI CI builds against the SAM `sow/2026-Q3` tip). Local builds need SAM built from that branch.
  - **SAM_UI:** Accept optimised airflow… (bulk bar, one dwelling, file dialog → SAM preview → Yes/No default No →
    pending baseline edit + MVHR/RetainedDesign/fingerprint). Codex fixes: catalogue setting re-validates the final
    (`DescriptorsOffered`); run verdict = SAM `OccupiedSpaceComplianceStatus`, corridor `CorridorRiskStatus` shown beside it
    (not a row, not a failure - SAM's rule); `AllowedProducts` asks SAM's `AllowedDescriptors` (test unit only where SAM
    makes it eligible); sidecar `ReadRefusal` fails closed. Fake TAS returns a real SAM report. Tests
    `PartOMixedDesignCorrectionTests` (7, all red on c5f59bf - `logs/codex-regressions-RED-on-c5f59bf.txt`),
    `PartOMixedDesignAcceptTests` (5). Local high review → 3 more fixes (screening uses the shared product rule; stale
    acceptance refused; duplicate sidecar result fails closed) + SAM designer-terminal refusal (`b8dd64dd`). WPF
    **1299/1299**; SAM.Tests 2545/2545; `SAM_UI.sln` Release 0 errors.
  - **Native regression (real exe + real TAS):** 9/9 required items PASS (evidence record §7), incl. accepting the real
    26 Sep 2B `-Opt10` design for Flat 3 through the dialog, one mixed run with the corridor in the project result, catalogue
    staleness, save/reopen, clean saved baseline. Improvements recorded in evidence §8 (not implemented).
  - Files: SAM_UI `Classes/PartO/Mixed/PartOMixedRunEvidence.cs` (SAM_UI project), WPF `PartOMixedDesignSession.cs`,
    `Modify/ScreenPartODwellingStrategies.cs`, `Modify/RunPartOMixedDesignCommand.cs`, `Windows/PartOMixedDesignWindow.xaml(.cs)`,
    tests `PartOMixedDesignFixture.cs`, `PartOMixedDesignCorrectionTests.cs`, `PartOMixedDesignAcceptTests.cs`,
    `PartOMixedDesignInvestigationTests.cs` (env-gated); docs PR2 record §11, evidence §7-§8.
- **Closeout (27 Sep, later):** Codex on SAM_UI a2de99b - 5 findings fixed (unassessed common space not a pass; evidence
  bound to the simulation case; cancelled rerun re-validates; run notes surfaced); Accept dialog starts in the output folder
  with an empty name and answers plainly for the open baseline (native check, no TAS). SAM#152 Codex rounds fixed up to
  `e558db01` (merged up with sow `8a22c82b`, SAM#153). WPF **1307/1307**, SAM.Tests 2568/2568; Codex on cf67983 (screening notes, explicit product under unticked catalogue) fixed.
- **SAM#152 MERGED 27 Sep as `be84d7b8`** (head `204bfea9`, after 7 Codex rounds + a local high-effort review standing in
  when Codex hit its usage limit - owner-approved). Local SAM fast-forwarded and rebuilt; SAM.Tests 2569/2569; SAM_UI
  against it: WPF 1307/1307, mixed 62/62. Codex also at its limit for SAM_UI's last heads (local high review stood in).
- **Not done / risks:** Optimised *screening* (2B inside screening) deferred;
  the 2B result file is chosen by the engineer (a capacity-envelope `-OptMax` file is indistinguishable by state - the
  confirmation shows every airflow); product pool not editable here; suggestion policy in SAM_UI (PR0 D6 later).
- **Owner manual acceptance: PASS (Michal, 27 Sep 2026)** - evidence record §11 (Natural + XBC15 MVHR + accepted 2B
  Optimised, one real TAS run, corridor risk in the project result, save/reopen current, source baseline clean).
- **Next step:** owner merges SAM_UI#126 (and SAM docs PR #157); Codex re-review optional when its limit resets. Then PR3
  (cooling) may start in a fresh session - not before #126 is merged
  (procedure in the self-test report; now with Accept optimised airflow instead of the pre-accepted fixture). Do NOT start
  PR3 cooling before that.

## Previous: Mixed Part O dwelling strategies - PR0 architecture investigation (26 Sep 2026) - MERGED (SAM#149, SAM_UI#125); PR1 MERGED (SAM#150 `3de02102`)

**Owner approved PR0 (26 Sep 2026).** Binding decisions are recorded at the top of the SAM report:
1. A clean baseline is mandatory; there is no undo/adopt, and materialisation fails explicitly on a
   non-baseline model.
2. Assessed common/corridor spaces are included automatically. They are not grid rows, and they are
   classified from state, never from names.
3. Project-wide constraints (such as all-MVHR or product pools) are project settings.
4. The 2B airflow stays only on `VentilationTerminal`; the strategy holds a reference or fingerprint only.
5. Cooling is recorded and refused in PR1.

The order is PR1 SAM → PR2 SAM_UI → PR3 cooling + licensed proof → PR4 acceptance/deploy.
PR1 starts only after the PR0 PRs are merged.

**New programme** (separate from the closed Part O UX programme). Goal: different final Part O strategies per
dwelling (NV / MVHR + product / retained design airflow / cooling) in ONE analytical model, ONE annual TAS run,
TM59 as final authority. **PR0 = investigation only. No production code in any repo.**

- **Report (authoritative):** SAM `documentation/PartO-MixedDwellingStrategies-PR0.md`. It contains the current
  mutation map, 21 composability verdicts, blockers C1-C11, the recommended architecture, the authority model,
  the PR sequence with gates, migration notes and open questions. It is linked from SAM
  `documentation/PartO-ARCHITECTURE.md` §9.
- **Evidence:** SAM `SAM/SAM.Tests/PartOMixedStrategyProofTests.cs`, 13 disposable proof tests
  (`Category=PR0Investigation`) asserting today's behaviour. **13/13 pass.** The SAM Part O/Part F suite is
  **1238/1238** green including them.
  - P12 shows that a reused authored conditioned unit keeps its supply temperature, and its movement then
    carries cooling. PR1 must refuse that under `ActiveCooling = None`.
- **PRs:**
  - [SAM-BIM/SAM#149](https://github.com/SAM-BIM/SAM/pull/149): branch `investigation/parto-mixed-strategies-pr0`,
    from `sow/2026-Q3` `00db4b85`; report, proof tests and SAM `PROJECT_PROGRESS.md`. CI green.
  - [SAM-BIM/SAM_UI#125](https://github.com/SAM-BIM/SAM_UI/pull/125): branch
    `docs/parto-mixed-strategies-pr0-2026-09-26`, from `sow/2026-Q3` `7ab24f8`; this file only.
  - Merge order: SAM first, then SAM_UI.
- **Key findings:**
  - A mixed route in one `PreparePartOIteration` call is refused.
  - Per-dwelling calls are safe for MVHR next to MVHR. But preparing ANY MVHR dwelling writes Part F rates and
    unconnected terminals onto EVERY sized space, including NV and unassessed flats. The rewrite of internal
    conditions is irreversible (P1, P11).
  - A dwelling cannot go from MVHR back to NV: the design is kept under an NV scenario (P4).
  - An automatic unit selection re-selects a manual product (P3).
  - Unit names follow call order (P2).
  - SAM_Tas and TM59 are already per space and per AHU, with one weather per simulation.
  - Cooling has no model or scenario identity. Any cooling forces the TPD / no-IZAM route on the whole
    building.
- **Decisions recorded:**
  - Mixed materialisation starts from a CLEAN pre-Part-O baseline; sanitising back is rejected (P11).
  - `PartODwellingStrategy` is persisted INTENT only: mode, optional product, cooling (gated),
    `DesignAirFlowBasis` + terminal-set fingerprint.
  - A retained 2B airflow lives ONLY on the baseline's `VentilationTerminal`s, through an explicit
    per-dwelling "accept" design edit. No second airflow store.
  - Materialisation is one deterministic SAM call from baseline + intent. The previous mixed model is never
    mutated.
  - Cooled mixed operation is UNRESOLVED until licensed TPD proof; PR1 records cooling and refuses it.
- **Proposed PRs:**
  - PR1 SAM: authority + materialisation.
  - PR2 SAM_UI: dwelling grid + mixed workflow on the IZAM route.
  - PR3 SAM + SAM_Tas: cooling authority + licensed proof.
  - PR4: real TAS acceptance + deploy.
- **Open owner questions:** none. All three are resolved by the decisions above:
  - legacy materialised projects reopen their pre-Part-O source (decision 1);
  - common spaces are included automatically (decision 2);
  - constraints are project settings (decision 3).
- **Next step:** once SAM#149 and SAM_UI#125 are merged, start **PR1 (SAM)** in a fresh session from the merged
  `sow/2026-Q3`. Follow SAM report §D-§F and the owner decisions. No owner review is outstanding.

## Previous: SAM Documentation Framework Phase 1 - COMPLETE (closeout 26 Sep 2026)

```text
SAM Documentation Framework — Phase 1
Status: COMPLETE
```

- **SAM_UI#121 merged as `7e7de033`.** The Space Assumptions PDF is available from SAM_UI: select one Space ›
  Edit › Reports › Space Assumptions PDF › save. The legacy **Print RDS** remains, and is retained until Phase 2
  reaches sufficient parity.
- UI integration acceptance passed (below). Phase 1 UI work is complete; no new UI functionality was started.
- Deployed by SAM_Deploy#51 (`8e6740af`), which pins SAM `22f9c743` and SAM_UI `7e7de033`. The real installed
  `SAM Analytical.exe` was smoke-tested and produced a 1-page PDF with `L/s`.
- SI only in the UI: SAM_UI has no global unit-system preference yet, so no IP selector was added. The SAM core
  supports IP.
- Completion record, including what is outside Phase 1 and the SAM#138 follow-up (still open): SAM
  `documentation/Reporting-PDF.md` › *Phase 1 status*.
- Next step (superseded): Phase 2 prerequisites were done in SAM/SAM_Tas (PR2A-PR2C); the SAM_UI command is PR2D above.

## Previous: Part O UX pass 6 - final consistency and end-to-end acceptance (26 Sep 2026) - MERGED (#122, 4b773f3e) and deployed; programme CLOSED

**Part O UX programme: CLOSED / FROZEN.** Pass 6 merged into `sow/2026-Q3` as SAM_UI#122 (`4b773f3`); final
validation WPF 1245/1245, Release 0 errors, build CI and SPDX green, Codex re-review no major issues (the multi-monitor
placement correction `dc214b6` included). Deployed by SAM_Deploy#53 (merge `5450341e`, rebased over #52; validate
green, Codex no findings): `sow/2026-Q3` pins SAM `22f9c743`, SAM_Tas `b32c0808`, SAM_UI `4b773f3e`; no other gitlink
moved. The "Left unchanged" items below are follow-ups, not gates. Originally: branch `feature/parto-pass6-acceptance-2026-09-26` from `sow/2026-Q3` `7e7de03` (#121 merged). SAM_UI only;
presentation fixes + acceptance. Pass 5 (#118) merged; the 2B `.sam` growth fix (SAM#142 / SAM_Tas#67 / SAM_UI#119)
merged and deployed (SAM_Deploy#50). Engineering logic, `CanOptimise`, stop rules, cancellation points, provenance,
saved-run format and TM59 rules unchanged.
Full record: `documentation/evidence/parto-final-acceptance/ACCEPTANCE-2026-09-26.md`.

**Environment note.** This machine's SAM was at `ba343bfb` with no `SAM.Core.Reporting.Pdf` build, so SAM_UI (with
#121) failed CS0234. SAM fast-forwarded to `22f9c743` and `SAM.sln` Release rebuilt (0 errors).

**Fixed (all presentation).**
1. Progress window reappearing after "Simulation cancelled" (Pass 4 known issue): `Modify/RunPartOWorkflow.cs`
   `PrepareAndRun` now re-shows the host after the attention box only where TM59 still follows (`partORun.CanAssess`).
   Reproduced live on the base build, gone after (logs in the evidence `live/`).
2. 2B result window: content in a ScrollViewer, Copy All / Close outside it; height capped by
   `PartOWindowPlacement.KeepOnScreen` on the monitor the window is on, at first render and on growth (Codex P2 on
   #122: the first version used the primary monitor's `SystemParameters.WorkArea`).
3. Iteration 3 verdict words PASS / FAIL / NOT ASSESSED (tiles, Hub Iteration 3 row, Hub outcome line) via new
   `Query/PartOVerdictText.cs`; a saved record with no status still "—".
4. TM59 window draws its verdict band and facts box only once `ResultSummary` is set - Iteration 3's "TM59 report"
   (report text only; no verdict may be parsed from it) no longer shows an empty band.
5. Twin wording: Prepare Iteration's 2B pre-set "Round limit" / "between rounds" (as Start Iteration 2B); its dwelling
   count uses the counted noun (as the Hub); step strip "Optimise (2B)"; Hub glossary Iteration 3 "1a or Iteration 2".
6. Captions on the nine captionless Part O message boxes; ribbon tooltip em dash; Iteration 3 "Copy All".

**Live acceptance (real exe, real TAS).** Prepare & Run cancel (before/after the fix); reopened no-sidecar 1a run →
Review Results → TM59 → Hub (scenario not guessed, window and Hub agree); Iteration 3 Open result; Iteration 2 run →
Hub → Start Iteration 2B (pre-fill, invalid step, Cancel) → 2B at a 3-round limit (not 10) → result (detail open,
buttons on screen) → Hub → second 2B → save → restart → reopen ("Saved Iteration 2 results reopened", no 2B history,
Optimise "Needs a live run"). 2B Cancel was not re-exercised live (rounds now ~14 s, shorter than the driver's delay).

**Files.** `Modify/RunPartOWorkflow.cs`, `Modify/PartOIteration3.cs`, `Modify/PreparePartOIteration.cs`,
`Modify/RunPartOOptimisation.cs`, `Query/PartOVerdictText.cs` (new), `Classes/PartO/PartOWorkflowStep.cs`,
`Windows/PartOOptimisationResultWindow.xaml(.cs)`, `Windows/PartOTM59ResultWindow.xaml(.cs)`,
`Windows/PartOIteration3ResultWindow.xaml(.cs)`, `Windows/PartOIterationWindow.xaml(.cs)`,
`Windows/PartOPreparationWindow.xaml.cs`, `Windows/PartOWorkflowWindow.xaml`, `Windows/PartOWorkflowWindow.Iteration3.cs`,
`Windows/AnalyticalWindow.xaml.cs`; tests `PartOFinalConsistencyTests.cs` (new, 8), `PartOWorkflowSimplificationTests.cs`
(Iteration 3 pins → PASS/FAIL); evidence `documentation/evidence/parto-final-acceptance/`.

**Validation.** `SAM_UI.sln` Release 0 errors; WPF tests **1245/1245** (1243 at the first push `d2ce1cb`; the Codex
correction replaced one test, added two); `git diff --check` clean. No TAS rerun for the correction (placement only). Corridor tests (2)
and model-growth tests (2) pass.

**Left unchanged (separate).** Iteration 3 refusal pane internal wording ("REFUSED at", "Candidate B" - ledger text,
also persisted in the Iteration 3 report JSON); 2B "Run" vs "round" in Engineering detail; Part O `l/s` vs reporting
`L/s` (cross-repo decision); remaining "(s)" in the Iteration 3 window / equipment control; 2B cancel-then-refusal
precedence; unreachable host-less "Preparing Model" fallback; session-only 2B history; output-path portability.

**Next step.** None for the Part O UX programme (closed). Any new Part O work - e.g. the mixed-dwelling-strategy
programme (not started) - begins in a fresh session from `sow/2026-Q3` `4b773f3` or later, as a separate programme.

## Previous: SAM Documentation Framework PR3 - Space Assumptions PDF in SAM_UI (26 Sep 2026) - MERGED (#121, 7e7de033)

**Status.** Merged as `7e7de033` from branch `feature/reporting-pr3-space-assumptions-pdf`.
- Base: `sow/2026-Q3` `d7f042f7` (#118 merged); `sow/2026-Q3` `cb61241d` (#119/#120, Part O .sam growth) merged in
  at closeout (only this file conflicted).
- Consumes SAM `sow/2026-Q3` `22f9c743` (SAM#141 PR2 renderer + SAM#143 `L/s` symbol). No other SAM change.
- Merged into `sow/2026-Q3` as #121 (`7e7de03`). Building SAM_UI now needs SAM built at `22f9c743` or later
  (`SAM.Core.Reporting.Pdf`); a machine with an older SAM build fails with CS0234 in `SpaceAssumptionsPdf.cs`.
- Phase 1 only. Not in scope: batch reports, Building/Design Load summaries, HTML/Excel output, or an IP option in
  the UI.
- Full description: `documentation/Reporting-SpaceAssumptionsPdf.md`.
- Evidence: `documentation/evidence/space-assumptions-pdf/ACCEPTANCE-2026-09-26.md`.

**What was built (orchestration only; no reporting logic in SAM_UI).**
- Command "Space Assumptions PDF", in three places:
  - Ribbon Edit › **Reports** (a new group, to the right of Analytical Model / legacy Print RDS). It uses the active
    view's selection.
  - The 3D/2D view's context menu.
  - The model tree's context menu for a Space.
- Selection:
  - exactly one Space is required;
  - with none, an info message;
  - with several, an info message, or in a context menu the item is disabled with a tooltip;
  - a stale Space is re-read by Guid; a removed one is refused.
- Save: WPF `SaveFileDialog`.
  - Default name `<Space name> - Space Assumptions.pdf`. Forbidden characters become `_`; reserved names are
    prefixed; the name is capped at 150 characters; with no name the fallback is `Space <guid>`.
  - It starts in the model folder, with the overwrite prompt on.
  - Cancel does nothing.
- On success: "saved: <path> — Open it now?" Yes shell-opens the PDF.
- On failure: an error message naming the stage (Document / Rendering / Output), plus `Trace.TraceError`.
  - The PDF is rendered in memory, then written to `.tmp` and moved into place, so an existing PDF is never damaged.
- Units: SI, with air flow in L/s (the reporting default; SAM#143 changed the central symbol from `l/s`). SAM_UI has no unit preference, and none was added.
  `Modify.WriteSpaceAssumptionsPdf(..., UnitStyle)` is the seam for a later option.
- Packaging (`SAM.Analytical.UI.WPF.csproj`):
  - HintPath references to SAM.Analytical.Reporting, SAM.Core.Reporting, SAM.Core.Reporting.Pdf and SAM.Units.
  - `PackageReference PDFsharp-MigraDoc 6.2.0`. It is needed because a netstandard library build does not copy its
    NuGet dependencies into `SAM\build`. It brings PdfSharp*/MigraDoc* 6.2.0 (net8.0), Microsoft.Extensions.* 8.0 and
    Pkcs 8.0 into `SAM_UI\build`, and from there into `%APPDATA%\SAM`, the installer payload.
  - Licences: `licenses\NotoSans\OFL.txt` (linked from SAM) and `licenses\PDFsharp-MigraDoc\LICENSE.txt`
    (`files/licenses`).
- Font resolver: nothing else in the SAM_UI process uses PDFsharp.
  - Mollier export uses OxyPlot.SkiaSharp.
  - SAM_Revit's PDFsharp deploys to `%APPDATA%\SAM\Revit 20xx`, draws images only, and installs no resolver.
  - `PdfRenderer` registers `NotoSansFontResolver` idempotently; SAM_UI never sets a resolver.

**Files.**
- New: `Modify/SpaceAssumptionsPdf.cs`, `Query/SpaceAssumptionsPdf.cs`, `Create/MenuItem_SpaceAssumptionsPdf.cs`,
  `Classes/Reporting/SpaceAssumptionsPdfResult.cs`.
- Changed: `Windows/AnalyticalWindow.xaml(.cs)`, `Controls/AnalyticalModelControl.xaml.cs`,
  `SAM.Analytical.UI.WPF.csproj`.
- New: `files/licenses/PDFsharp-MigraDoc/LICENSE.txt`.
- Tests: `SpaceAssumptionsPdfTests.cs` (new, in `WpfCollection`) and the tests csproj (reporting references).
- Docs and evidence, as listed above.

**Validation.**
- Builds: `SAM.sln` Release rebuilt at `ba343bfb`. SAM_Systems and SAM_Tas were fast-forwarded and rebuilt; they were
  stale, and SAM_UI does not compile against the old ones. `SAM_UI.sln` Release (VS 18 MSBuild `-restore`): 0 errors.
- `SpaceAssumptionsPdfTests`: 27/27. Full `SAM.Analytical.UI.WPF.Tests`: **1235/1235**.
- Production seam: a scratch harness loads only from `%APPDATA%\SAM`.
  - Real Part O model: 4 PDFs (SI and IP), 1 page each.
  - 1,608-Space model: 2 PDFs, 1 page each.
  - First call 0.2–0.5 s, later calls 16–33 ms.
  - Negative control: a payload with no PdfSharp gives a controlled Rendering failure.
- Native: UI Automation of the real `%APPDATA%\SAM\SAM Analytical.exe` (real mouse and keys). Steps passed:
  - ribbon with nothing selected;
  - tree single Space and save;
  - two Spaces, item disabled;
  - Save dialog cancelled;
  - sparse Space;
  - locked destination, error shown with the original intact;
  - ribbon using the view's selection;
  - view context menu, then Yes, opens the PDF.
- The PDF made by the app was inspected: A4, 1/1 page, SAM mark, Noto Sans, footer, SI units, no clipping.
- `git diff --check` clean.
- Closeout (26 Sep): SAM#143 (`22f9c743`) moved the central airflow symbol `l/s` -> `L/s` (approved convention);
  `sow/2026-Q3` `cb61241d` merged into this branch. `SAM.sln` rebuilt at `22f9c743`, `SAM_UI.sln` Release 0 errors,
  WPF tests **1237/1237** (1235 + #119's 2). The earlier acceptance evidence shows `l/s`, the symbol at that time.

**Not done / risks.**
- No IP choice in the UI (by design for Phase 1).
- No company logo: SAM_UI has no logo resource.
- Resolved: SAM_Deploy#51 added an installer gate that checks the PDFsharp/MigraDoc files and licences in the payload.
- A PDF locked in a viewer is reported by Windows as "Access to the path is denied". The message also says to close
  the file.

**Outcome.** Merged on green CI; SAM_Deploy#51 (`8e6740af`) moved the pointers and verified the installer payload.

## Previous: Part O 2B per-round `.sam` growth (26 Sep 2026) - MERGED (SAM#142, SAM_Tas#67, SAM_UI#119) and deployed

**Status.** MERGED in dependency order and deployed: SAM#142 `78a57466` -> SAM_Tas#67 `b32c0808` -> SAM_UI#119
`5a0b9bf6`; SAM_Deploy#50 `7fcd79a7` pins SAM `78a57466`, SAM_Tas `b32c0808`, SAM_UI `5a0b9bf6`. Each downstream repo
was re-validated against the merged upstream build with unchanged results (SAM 2441, SAM_Tas TM59 947, WPF 1210; all
three Release rebuilds exit 0; merged trees identical to the reviewed heads). Two Codex P2s on #119's evidence script
(`compare_runs.py`: ignore the TM59 `Source:` line; key airflows by space Guid) fixed in `faf8c50`/`2c53aa4` - script
only, results unchanged. Originally: SAM_UI branch
`feature/parto-2b-sam-growth-2026-09-26` from `sow/2026-Q3` `d7f042f` (#118 merged). SAM branch
`fix/deepclone-guidless-objects-2026-09-26` (from `3ec76eca`), SAM_Tas branch `fix/parto-replace-run-records-2026-09-26`
(from `39828c6`). Full record: `documentation/evidence/parto-2b-sam-growth/GROWTH.md`.

**What grew.** Only the adjacency cluster's `DesignDay` records: `2n + 2` per TAS run (12 -> 26 -> 54 ... 28,670 at
Opt10, 57,342 at OptMax; 1.9 MB -> 92 MB JSON, 172 KB -> 3.7 MB `.sam`), plus `ZoneSimulationResult` +1 per zone per
run. Spaces, zones, panels, ICs, space/surface results, relations: flat; space/IC/zone NAMES unchanged. `OptNN.prepared`
= `Opt(N-1).sam` structurally - the growth is inside each TAS run, not in SAM_UI's round adoption.

**Root cause (SAM / SAM_Tas, not SAM_UI).**
1. `2n`: SAM.Core `SAMObjectRelationCluster(cluster, deepClone: true)` re-added each clone via `AddObject`; a
   `DesignDay` (a `WeatherDay`, no Guid, no value `Equals`) got a new key BESIDE its original. `RunPartOSimulation`'s
   ownership deep copy (SAM `bdcfe5df`) runs once per TAS run.
2. `+2`: `WorkflowCalculator` APPENDED the run's design days to the cluster (the TBD itself is cleared and rewritten).
   Also why the model carried stale **London** design days beside its CIBSE Z1 ones (mixed weather in one `.sam`).
3. `ZoneSimulationResult`: `AddResults` replaced space/surface results but appended zone results.

**Classification: performance/storage defect; engineering results unaffected.** Nothing sizes/simulates from the
cluster design days (`DesignDays_Authoritative` reads settings/weather/model PARAMETERS; SAM_UI passes the run
weather's pair; `AddDesignDays` clears the TBD); no Part O/TM59/optimiser code reads `DesignDay` or
`ZoneSimulationResult`. Exponential in runs (20 runs would be ~15M design days - OOM on any project); "Saving Model"
0.19 s -> 14.0 s by OptMax.

**Fix.** SAM: deep clone stores each clone in the original's own slot (`RelationCluster.ReplaceObjects`). SAM_Tas:
`Modify.ReplaceDesignDays` (cluster records exactly the TBD's design days) + `AddResults` replaces a zone's cooling
result like the space results. SAM_UI: no production change - regression `PartORunModelGrowthTests` (2) drives the
real `RunPartOSimulation` for 6 rounds via `PartOWorkflowRunner`; pre-fix SAM.Core fails it ("Round 1: 24 design
days, not 12"). Not changed: optimiser, targeting, balancing, rounds, stop reasons, Part F, TM59, TAS inputs, unit,
envelope, Pass 5 UX.

**Validation.** SAM tests 2441/2441 (new `DeepCopy_OfObjectsWithNoGuid_NeitherDuplicatesNorSharesThem`; pre-fix 2 ->
64). SAM_Tas TM59 tests 947/947 (+3 `DesignDayRecordReplacementTests`). WPF 1210/1210 (+2). `SAM.sln`, `SAM_Tas.sln`,
`SAM_UI.sln` Release (VS 18 MSBuild): exit 0, 0 errors. `git diff --check` clean in all three.
Live (same fresh model, driver `C:\TasOut\parto-2b-journey-2026-09-26\scripts\journey2b.ps1 -Out
C:\TasOut\parto-2b-sam-growth-2026-09-26`): every round `.sam` 166 KB (was 170 KB -> 3.6 MB), 2 design days (Z1 pair
only), 4 zone results; all 12 TM59 reports identical to the Pass 5 run except the `Source:` path; per-space airflows
identical; same stop (round limit, 10), kept design run 10, 8 spaces changed (6/2); 2B 243 s -> 160 s.

**Merge order.** SAM PR first, then SAM_Tas (its build uses `..\SAM\build`), then SAM_UI (test only needs both
built). Then bump SAM_Deploy's SAM / SAM_Tas / SAM_UI pointers.

**Not done / follow-ups.** Existing saved models keep accumulated records until next simulated (then replaced).
`WorkflowCalculator`'s Adding Design Days step and `AddResults` zone replacement are exercised live only (TAS COM).

**Next step.** Start Part O UX Pass 6 (final Part O consistency and end-to-end acceptance) in a fresh session from
`sow/2026-Q3` `5a0b9bf6` or later. SAM `sow/2026-Q3` has since moved on with SAM#141 (reporting PDF renderer, not
Part O); deploy it in a separate bump.

## Previous: Part O UX pass 5 - Iteration 2B journey (26 Sep 2026) - MERGED (#118, d7f042f)

**Status.** Branch `feature/parto-2b-journey-2026-09-26` from `sow/2026-Q3` `3b29e41` (#117 merged). SAM_UI only.
Presentation and orchestration only: the optimiser, its eligibility (`Modify.CanOptimise`), stop rules, rounds,
capacity envelope, cancellation points, TAS behaviour, provenance and saved-run format are unchanged. Merged as #118
into `sow/2026-Q3`. Full record: `documentation/evidence/parto-2b-journey/JOURNEY.md`.

**Housekeeping (separate).** SAM_Deploy PR #49 (`chore/bump-sam-ui-parto-pass4-2026-09-26`): SAM_UI `90b42e0e` ->
`3b29e41f`, SAM `80b01052` -> `7dbeb2e4` (the SAM#137 merge SAM_UI#114 needs; SAM deliberately not moved to its tip -
reporting PRs #136/#139/#140 not needed). Not merged at time of writing.

**Owner decision (26 Sep): 2B settings are confirmed at the start, not at preparation.**
- Before: the Hub had a collapsed "Follow-on optimisation" expander; a completed Iteration 2 run prepared without the
  tick was refused 2B ("prepare and run again with the follow-on ticked") - a full-year TAS re-run for two numbers the
  preparation never reads (`PartORun.AdoptOptimisationSettings` documents that).
- Now: `Modify.Capabilities` and the ribbon gate use `CanOptimise(run, null)` only. The Hub expander and its
  properties (`OptimisationSettings`, `Optimise`, `OptimisationRefusal`, `AirFlowStepText`, `MaximumIterationsText`,
  `OptimiseChecked`) are removed; `PartOWorkflowWindow.Restore` lost its settings parameter; the Hub request records
  no 2B settings.
- `Optimise (2B)…` (Hub) and Results - Optimise (2B) (ribbon) open **Start Iteration 2B**
  (`Windows/PartOOptimisationStartWindow`, `Classes/PartO/PartOOptimisationStart`): purpose, starting run, May change /
  Never changes, settings pre-filled (recorded on the run -> last confirmed this session -> defaults; says which),
  validated by `PartOOptimisationSettings.IsValid`. Confirmed settings go straight to `OptimisePartOTM59` and are kept
  on `PartOOptimisationRun.Settings`. Cancel runs nothing and leaves the Hub line as it was.
- `RunPartOOptimisationResult(ui, run, owner, sessionSettings, out confirmed, confirm = null)` - `confirm` is a test
  seam. Public `RunPartOOptimisation` signature kept.
- The Prepare Iteration window keeps its 2B tick, reworded as an optional pre-set (it only pre-fills). The
  `PartOPreparationContext.OptimisationSettings` / `AdoptOptimisationSettings` / `ReuseWithCurrentOptimisation`
  plumbing is kept (it is the "recorded" pre-fill source); a follow-up could remove the tick and that plumbing.

**Result window (`PartOOptimisationResultWindow`, `Classes/PartO/PartOOptimisationSummary`).**
- Top: verdict band - PASS only on a `Passed` stop, FAIL where the kept design's production status is Fail, NOT ASSESSED
  otherwise (e.g. partial-scope refusal), UNAVAILABLE with no kept design (same rule as the Hub's
  `OptimisationOutcome`); a distinct headline + meaning + "Next:" for each of the 9 stop reasons (baseline-already-
  passes has its own); facts: rounds (run/completed, limit, step), starting vs kept design (status + spaces with a
  failing TM59 check), design airflow changed (spaces from COMPLETED rounds, targeted vs balancing), capacity envelope
  (only if asked for; "diagnostic, not adopted").
- Below: "Engineering detail" expander, collapsed - tabs for the airflow history, unit duty by round and notes (the
  run's full `Description` first). Grids virtualise. Copy All keeps everything.
- Cancel edge case: the command reads `PartOProgressHost.IsCancellationRequested`; where Cancel was requested but the
  run stopped for another reason (e.g. a refusal after the click), the band says so. Stop precedence unchanged.

**Final acceptance pass (26 Sep, owner request - real app, real TAS; full record in JOURNEY.md "Live acceptance").**
- Real Iteration 2 (full year, 54 s) with NO 2B tick -> Hub offers Optimise (2B)… on `CanOptimise` alone; no re-run.
- Start window: correct source run/results/dwellings/weather; defaults 5 l/s / 10 rounds / envelope on; `0` and `abc`
  disable Start with the reason; Cancel starts nothing and leaves the Hub line.
- Real 2B: one Part O window, stages assess -> "Optimisation rounds · round 1..10" (no total, no %) -> capacity envelope;
  Cancel only during TAS steps. Stopped **IterationLimitReached** after 10 rounds (243 s incl. envelope); result
  TM59 FAIL, "Stopped: round limit reached (10 rounds)", 8 spaces changed (6 targeted, 2 balancing), envelope
  calculated (diagnostic); Engineering detail collapsed (108 / 36 rows, 863 notes).
- Second 2B from the kept design, pre-filled from the session, named "design kept by an earlier 2B run (round 10)",
  written as `-Opt11`; cancelled in round 1's TAS -> "Cancelled", Next = needs a new Iteration 2 run; Hub agrees.
- Save -> restart -> reopen: "Saved Iteration 2 results reopened — ready to review"; no 2B history shown or inferred.
- Fixed from this pass: Codex P2 x2 (next step only offers continuing where the run survives - `canContinue`, read off
  the run; session pre-fill on every route - `Modify.partOOptimisationSettings_LastConfirmed`); live: a reopened
  Iteration 2 run said "Iteration 2 only" beside Optimise (pre-existing precedence) -> now the authority's refusal and
  "Needs a live run" where a run with results exists (re-checked live); Hub "iteration limit" -> "round limit".
- Codex P2 on 5b0c039 (fixed after the live run): a Hub Prepare & Run that REUSED a preparation made by the Prepare
  Iteration window cleared its 2B preset (`ReuseWithCurrentOptimisation` adopted the request's null). Now a request
  stating no settings keeps the recorded preset; the reuse condition is unchanged. Test C renamed
  `AReusedPreparation_KeepsARecordedPresetWhenTheRequestStatesNone`.
- Codex P2 x2 on 91085dc (fixed): (a) a 2B started from an earlier `-Opt10` said "numbering continues", but only the
  result files continue - the new run's rounds show 1..N. The Start window now says "saved as round 10 (-Opt10)" and
  explains that its rounds count from 1 while files continue from -Opt11; the optimiser's numbering is unchanged.
  (b) the Start window is now capped to the work area with its content in a ScrollViewer and Start/Cancel outside it.
- Communal corridor: verified, no defect. SAM `TM59AssessmentReport` classifies by the exact InternalCondition; SAM_UI
  presents `CorridorChecks` / `SupplementaryChecks` as given. New `PartOTM59CorridorReportingTests` pins it (the live
  model has no corridor IC).
- Observed, not changed: per-round `.sam` roughly doubles in size (172 KB -> 1.9 MB at round 10) - root-caused and fixed in the *Current* entry above.

**Unchanged on purpose.** Pass 4 progress window (stages, "round n" with no total, no percentage, Cancel only while
observed); Pass 3 Hub outcome wording; `CanOptimise` including the restored-run refusal.

**Persistence limitation (documented, not changed).** `PartOOptimisationRun` is session-only: after closing the result
window or restarting SAM_UI the rounds, stop reason and envelope are gone. Each round's TSD/TBD/TM59 report and a
`PartORunResume` sidecar (scenario = Iteration 2; round only in the `-OptNN` file name) persist. A reopened kept design
can be reviewed, not continued into 2B. Also not available: SAM's per-space overall status per round (a step keeps
check rows, not the report) and before/after airflow totals - not computed in the UI.

**Files.** New: `Classes/PartO/PartOOptimisationSummary.cs`, `Classes/PartO/PartOOptimisationStart.cs`,
`Windows/PartOOptimisationStartWindow.xaml(.cs)`, tests `PartOIteration2BJourneyTests.cs`,
`PartOTM59CorridorReportingTests.cs`, evidence `documentation/evidence/parto-2b-journey/` (JOURNEY.md, 7 synthetic PNGs,
`live/` captures and driver logs). Changed: `Modify/RunPartOOptimisation.cs`, `Modify/PartOHubOutcome.cs` (round limit),
`Modify/RunPartOWorkflow.cs`, `Windows/PartOOptimisationResultWindow.xaml(.cs)`, `Windows/PartOWorkflowWindow.xaml(.cs)`,
`Windows/PartOIterationWindow.xaml(.cs)` (wording), `Windows/AnalyticalWindow.xaml.cs` (ribbon gate/tooltips); tests
`PartOWorkflowTests`, `PartOWorkflowRefreshTests`, `PartOWorkflowInitialisationTests` (9 Hub-tick tests retired, one
rewritten, the workflow-input test now uses the Simulation case folder) and four files for the `Restore` signature.

**Validation.**
- `SAM.Analytical.UI.WPF.Tests`: **1208/1208** (was 1196 before Pass 5; -9 retired Hub-tick tests, +19 in
  `PartOIteration2BJourneyTests` incl. the env-gated evidence test, +2 in `PartOTM59CorridorReportingTests`).
- `SAM_UI.sln` Release (VS 18 MSBuild): exit 0, 0 errors; no new warnings in the touched files. `git diff --check` clean.
- Rendered evidence reviewed; live acceptance above (driver `C:\TasOut\parto-2b-journey-2026-09-26\scripts\journey2b.ps1`,
  parts `run` / `reopen`; the run folder is on this machine only).

**Next step.** Done: merged as #118 (`d7f042f`). Bump SAM_Deploy's SAM_UI pointer (after SAM_Deploy #49). The
`.sam` growth follow-up is the *Current* entry; optional: remove the Prepare Iteration 2B pre-set and its plumbing.

## Previous: Part O UX pass 4 - shared progress window consistency (25 Sep 2026) - MERGED (#117, 3b29e41)

**Status.** Branch `feature/parto-progress-consistency-2026-09-25` from `sow/2026-Q3` `91aeb43` (#116 merged).
SAM_UI only. Merged into `sow/2026-Q3` as #117 (merge `3b29e41`, head `3fb7bd0`); CI, build and review green. There are no engineering,
TAS, calculation, provenance or saved-run changes, and no public signature changed. The Iteration 2B journey
redesign is Pass 5 and is not started.

**Inventory (every Part O progress surface).**
- Prepare & Run: `PartOProgressHost` (the shared Part O window), 3 stages, Cancel.
  - Nested `RunPartOSimulation` / `RunWorkflow` report into it as detail and link its token.
- Iteration 3 run: the shared window, 6 phases, Cancel between ledger stages. Iteration 3 Open result: the
  shared window, 4 stages, no Cancel.
- Review Results (Hub, no host): **was** the generic `ProgressBarWindowManager("Part O TM59")`. It is
  indeterminate, has no stages, no elapsed time and no text in UIA, and it set "Assessing..." only after the
  assessment had finished.
- Iteration 2B: **was** two generic `ProgressWindowHost` dialogs per round (SAM_Windows).
  - "Preparing Model (proj)" had max **8**, but only 4 `step()` calls exist, and those only on the SAM-solar
    path. So its bar showed at most 50% and then closed. That is a fabricated denominator.
  - "Tas Workflow" is determinate by workflow step count, which is a count of steps, not of time.
  - The baseline and per-round TM59 assessments showed nothing.
- SAM Check gate (pre-simulation): no progress UI of its own. Its LogWindow modal already hides the host.
- Legacy non-Part-O Simulate dialog (`Simulate.cs` "Preparing Model", 8): out of scope, unchanged.

**What changed (the shared pattern).**
- `PartOProgressState` (pure, clock injected):
  - `Report(completed, total)`: the only way a percentage exists. It is ignored with no running stage or no
    total, cleared on every Start/Complete/Fail, and rounded down (`Percent` "63%", never 100% early).
  - `Activity(text)`: labels a repeating stage ("Optimisation rounds · round 2"). No total is implied.
  - `ElapsedText` "Elapsed 7m 48s", never a time remaining. `IsFinished`.
  - `StatusText` / `AccessibleLine`: "Completed / Running now / Upcoming / Not needed / Did not complete".
  - `Note(determinate, cancellable, cancelRequested)`: one wording, which says whether the number is real and
    what Cancel really does (the next safe point; a running TAS step finishes first).
- **No Part O operation calls `Report` today**, because none has an authoritative count. Every Part O window
  is indeterminate, and says why.
- `PartOProgressWindow`:
  - the bar is indeterminate unless there is a fraction, with the percentage text only then;
  - the note is always shown, so a non-cancellable window says "It cannot be cancelled";
  - "Cancelling…" disabled plus the cancel note;
  - each row's UIA name is the status in words (on the name TextBlock, plus the row's `ToString`; a Grid has no
    automation peer), and the duration is not repeated.
- Review Results: when no host is current, `ReviewPartOTM59` opens its own shared window: "Checking TM59
  results", subheading `PartOIterationText` + "no TAS simulation is run", one stage "TM59 assessment", no Cancel.
  Inside Prepare & Run it still reports into the outer host.
- Iteration 2B: `RunPartOOptimisationResult` wraps `OptimisePartOTM59` in ONE host.
  - Stages come from `Modify.PartOOptimisationPhases`: "Assess the Iteration 2 results (TM59)", "Optimisation
    rounds", and "Capacity envelope (diagnostic)" only where it was asked for.
  - The subheading comes from `Modify.PartOOptimisationProgressSubheading`: "The limit is N rounds; how many run
    depends on the results".
  - The optimiser only calls `PartOProgressHost.Current?.Start/Activity/Detail`. With a host current, the
    per-round generic dialogs no longer open.

**Cancellation (reviewed; semantics not changed).**
- Prepare & Run / Iteration 3: the token is latched and observed between workflow steps / ledger stages. A TAS
  COM call in flight finishes first. Live: stopped about 11 s after the click, at the next step.
- Review Results / Iteration 3 Open result: not cancellable (single reads), and now said so.
- 2B: the same observation points as before, in each round's `RunPartOSimulation` `step()` and `RunWorkflow`,
  through the host token they already link. The optimiser loop was not given a new observation point (that
  would change stop logic, which is Pass 5).
- **2B Cancel is offered only where it is observed** (owner review correction, 25 Sep).
  - The 2B host is built with `cancelOnlyWhileObserved: true`.
  - Cancel is offered only inside `PartOProgressHost.AllowCancel()` scopes. `RunPartOSimulation` opens one
    around its preparation steps, and `RunWorkflow` (Part O branch) opens one around `Calculate`.
  - Each scope is disposed in the finally, BEFORE that code's existing final check. `SetCancelAvailable`
    waits for the window's thread, so a click either finished first (and the final check sees it), or finds
    Cancel withdrawn. The click handler also refuses while Cancel is withdrawn.
  - During the baseline assessment, the rebalancing and each round's TM59 assessment, Cancel is visible,
    disabled, with a tooltip. The note says "Cancel is offered only while a TAS simulation is being prepared or
    run; this step does not stop for it."
  - A request already made keeps "Cancelling…" and its note.
  - A default host (Prepare & Run, Iteration 3) is unchanged: a scope restores what was offered before.
  - Remaining gap, documented: a click inside a scope that is followed by a refusal (e.g. the TBD cannot be
    overwritten) is not acted on as a cancel; the round stops with that refusal instead.
- **2B stage list ends from the run's own record** (Codex P2 on #117). `Modify.PartOOptimisationProgressEnd`:
  - Passed / CapacityReached / IterationLimitReached / NoEligibleTargets complete, the same four the Hub calls
    completed. Anything else fails, Cancelled included.
  - A running envelope completes only if its own step `IsCompleted`. A null run fails.
  - Before, `Complete()` was unconditional, so a cancelled round could flash "✓" beside "Cancelling…".
  - Codex P2 on d6a8cf8: a COMPLETED run marks the stages it never started "Not needed"
    (`PartOProgressState.SkipUnstarted`). That covers a baseline that passes or has no targets, and an
    envelope declined before it simulated. After a cancel or failure they are left as they are, because "not
    needed" would not be true.

**Files.** `Classes/PartO/PartOProgressState.cs`, `Classes/PartO/PartOProgressHost.cs`,
`Windows/PartOProgressWindow.xaml(.cs)`, `Modify/AssessPartOTM59.cs`, `Modify/OptimisePartOTM59.cs`,
`Modify/RunPartOOptimisation.cs`, `Modify/RunPartOSimulation.cs` and `Modify/RunWorkflow.cs` (the cancel
scopes only); tests `PartOProgressConsistencyTests.cs` (new), `PartOWorkflowSimplificationTests.cs` (elapsed
wording pins); evidence `documentation/evidence/parto-progress-consistency/`.

**Validation.**
- `SAM.Analytical.UI.WPF.Tests`: 1196/1196 (was 1159; 1173 at the first push, 1192 at d6a8cf8).
  - The correction's tests: the 2B offer through a whole sequence, the latched request, nested scopes,
    Prepare & Run unchanged, the note, the window refusing a click, the shown window already disabled when the
    scope returns (UIA, no wait), and the end state for every stop reason and for the envelope.
- `SAM_UI.sln` Release (VS 18 MSBuild `-restore`): exit 0, 0 errors.
- `git diff --check` clean.
- Live, real exe (`LIVE-ACCEPTANCE-2026-09-25.md`):
  - before/after Review Results;
  - Prepare & Run with TAS started, then cancelled (stage hierarchy, elapsed, Cancelling…, Hub "TAS run
    cancelled"; TAS3D exited);
  - Iteration 3 Open result.
  - Iteration 3 mid-run and a 2B round were rendered through the env-gated test
    `Evidence_renders_the_iteration_3_and_iteration_2B_windows_mid_run` (`SAM_PARTO_PROGRESS_EVIDENCE`).
- 2B was NOT run live: no saved Iteration 2 run exists, and making one needs full-year TAS.

**Deferred / separate.**
- Pass 5: the whole Iteration 2B journey (where the choice is made, preparation, decision flow, results,
  diagnostics, Hub 2B logic, stop rules, and whether the loop should observe Cancel between rounds).
- Separate defect, not fixed here: `RunPartOSimulation`'s host-less fallback still opens "Preparing Model" with
  max 8 against at most 4 steps. No Part O route reaches it now.
- Pre-existing: after a cancelled Prepare & Run, the host is shown again briefly after the "Simulation cancelled"
  box before it closes.
- Live acceptance of 2B needs a completed Iteration 2 run.

**Next step.** Done: merged. SAM_Deploy pointer bump to `3b29e41` is SAM_Deploy PR #49 (separate). The 2B journey
is Pass 5 (above).

## Previous: Part O reopened-run naming - check + Hub line (25 Sep 2026) - MERGED (#116)

**Status.** Branch `fix/parto-reopened-run-scenario-2026-09-25` from `sow/2026-Q3` `ba9bf48` (#115 merged). SAM_UI
only. Merged as #116 (91aeb43).

**The reported defect is already fixed on `sow/2026-Q3`.** The report said `PartOTM59ResultSummary.RunFacts` names
a reopened Iteration 2 run "Iteration 1a". That was true before b224453 (the #115 follow-up, merged). The fix is the
persisted authority the report asked for:
- the sidecar is `PartORunResume:v2` with `VentilationUnitCatalogueOffered`, and v1 files still read (value null);
- `TryResume` builds `PartOPreparationContext.Resumed(...)` with the saved value;
- `PartOWorkflowScenario.Find` returns null where the value is null, so a v1 run has no Scenario fact. Nothing is
  inferred from text or from the reconstructed context.

**Proof, this pass.**
- New `PartOTM59RunFactsReopenTests`: prepare with a catalogue, complete, `Modify.PersistPartORunResume`, `Restore`
  in a fresh run, then assert the `RunFacts` Scenario. It uses only members that predate the fix, so the same test
  compiles on both versions.
- Run in place with the six production files b224453 touched checked out at `fd379b4`, and
  `PartORunResumeNamingTests.cs` set aside: **FAIL**, Scenario = "Iteration 1a — MVHR design duty (no manufacturer
  unit)".
- Restored to HEAD: **PASS**. The working tree was verified back to HEAD before any other edit. (A `git worktree` at
  fd379b4 is not usable here: path length, and the sibling-repo references.)

**Rule revisited: the Hub now names a reopened run from its saved record** (`Modify/PartOHubOutcome.cs`).
- New helper `SavedResults(run)`: "Saved Iteration 2 results" where `Find` names the run, otherwise "Saved results".
- Standing line: "○ Saved Iteration 2 results reopened — ready to review".
- Review line: "Saved Iteration 2 results reviewed — TM59 FAIL".
- A v1 sidecar, or a run with no sidecar, is unchanged: "Saved results reopened" / "Saved results reviewed".
- A live, non-restored run is worded exactly as before. Still no verdict before an assessment runs.
- Tests: `PartORunResumeNamingTests.TheHub_NamesAReopenedRunFromItsSavedRecordOnly` (v2 Iteration 2, v2 1a, v1).
  `PartOHubOutcomeTests.AReopenedRun_IsReadyToReview_WithNoVerdictAndNoName` is unchanged in behaviour (it has no
  sidecar); only its comment was corrected.

**Files.** `WPF/SAM.Analytical.UI.WPF/Modify/PartOHubOutcome.cs`; tests `PartOTM59RunFactsReopenTests.cs` (new),
`PartORunResumeNamingTests.cs`, `PartOHubOutcomeTests.cs` (comment).

**Validation.**
- `SAM.Analytical.UI.WPF.Tests`: 1159/1159 (was 1155).
- `SAM_UI.sln` Release (VS 18 MSBuild `-restore`): exit 0, 0 errors.
- `git diff --check` clean.
- Not run live: the change is to Hub wording only, and the existing live smoke run has a v1 sidecar, so it still
  reads "Saved results reopened".

**Next step.** The owner reviews the PR. Before merge, check CI is green. Then move SAM_Deploy's SAM_UI pointer.
A live check of the named Hub line needs a run saved by a v2 build; any new Prepare & Run produces one.

## Previous: Part O UX pass 3 - Hub outcome / completed-state line (25 Sep 2026) - MERGED (#115)

**Status.** Branch `feature/parto-hub-outcome-2026-09-25` from `sow/2026-Q3` `a13ba4c` (#114 merged). SAM_UI only;
no SAM change is needed. A PR is open against `sow/2026-Q3`; the owner said to stop before merge. Engineering and
simulation pathways are unchanged. No public signature changed: owner review asked to keep the public
`void Modify.RunPartOOptimisation(...)`. It is now a wrapper over the internal
`Modify.RunPartOOptimisationResult(...)`, the same body, which returns the `PartOOptimisationRun?` it already built.
Only the Hub calls the internal method, to word the 2B line.

**States found, and where each comes from.**
- Authoritative and persistent (read off `PartORun` / `Modify.Capabilities` at every showing):
  - None: nothing prepared (no line);
  - Prepared;
  - WorkflowCompleted with reviewable results, either this session or `IsRestored`;
  - None + `InvalidationReason`: stale or invalidated results.
- Authoritative, per action, not persisted:
  - TM59 verdict and counts: the `PartOTM59ResultSummary` the result window is given;
  - `PartOSimulationOutcome` (Cancelled / Refusal / Note_PartORun);
  - 2B `PartOOptimisationStopReason`, plus the last valid step's `OccupiedSpaceComplianceStatus`.
- Transient, session-only (the Hub loop's `partOWorkflowOutcome`, never persisted):
  - review cancelled (`PartOPreparationResult.Declined`);
  - TAS cancelled;
  - "completed — TM59 X" after a run or a review.

**What changed.**
- `Modify.HubOutcome(last, run, capabilities)` (new file `Modify/PartOHubOutcome.cs`) chooses what the Hub shows:
  - the session record of the last action, while the run still holds what it claims (`PartOWorkflowOutcome.RunState`);
  - otherwise `Modify.StandingOutcome`, which says what the run is now. It never gives a verdict and never
    reports an event.
  - A superseded record stays on the tooltip as "Earlier in this session: …". So a completed-with-results line is
    never shown over results that have gone, and "prepared iteration kept" is never shown after the preparation
    was dropped.
- Wordings, all in `PartOHubOutcome.cs`:
  - `CompletedOutcome` ("✕ Iteration 1a completed — TM59 FAIL" + "TAS simulation 53s · <counts>");
  - `ReviewOutcome` ("Iteration 1a results reviewed — TM59 PASS"; a reopened run reads "Saved results reviewed");
  - `DeclinedOutcome`, `SimulationCancelledOutcome`, `NotCompletedOutcome`, `OptimisationOutcome`.
  - These replace the old inline strings and `TM59OutcomeSuffix`, which was removed.
  - Glyph and word come from the summary (✓ ✕ – !). A new kind, `Fail`, gives a red tint; the kind follows the verdict.
- Standing lines:
  - "○ Iteration 2 prepared — waiting for the full-year TAS run";
  - "○ Saved results reopened — ready to review" (or "<name> completed — ready to review");
  - "! Previous Part O run is no longer valid — no results to review", with the run's reason as caption and tooltip.
- A reopened run was never named here. That was superseded by the follow-up below and by the naming pass above.
- `PartOWorkflowOutcome` moved to `Classes/PartO/PartOWorkflowOutcome.cs`. It now holds Glyph / Headline / Detail /
  ToolTip / RunState, and `Text` joins them. The 2-argument constructor still works.
- The Hub's line is rendered as glyph + semibold headline + caption. The full explanation is on the tooltip and,
  with the Hub's one Show details switch, under the line. It re-renders when the run or the capabilities are set
  (no inspection, no filesystem access).
- `PartOWorkflowScenario.Name` / `ShortName`: "Iteration 1a", the part of the Text before its dash.
- The Iteration 3 lines only gained glyphs ("! Iteration 3 did not complete", "○ Opened the saved Iteration 3 result").

**Validation.**
- `SAM.Analytical.UI.WPF.Tests`: 1155/1155 (was 1129; +21 in `PartOHubOutcomeTests`, +5 in `PartORunResumeNamingTests`).
  The Hub outcome tests cover:
  - prepared not simulated; fresh run gives no line;
  - stale results (TSD deleted, through `Modify.Capabilities`); a reopened run is unnamed and has no verdict;
  - review cancelled; TAS cancelled held only while Prepared; not completed;
  - completed, with 4 verdicts matching the summary's glyph and word; review; a completed line superseded when
    its results go;
  - 2B Passed / capacity + Fail / stopped without a stated fail / cancelled / refused;
  - window: reopening the Hub does not bring back a cancellation; Show details.
- Updated pins: `PartOTM59ResultTests` (one-summary theory), `PartOReviewIterationTests` (declined wording).
- `SAM_UI.sln` Release (VS 18 MSBuild `-restore`): exit 0, 0 errors. `git diff --check` clean.
- Live, real exe, no TAS, 0 message boxes, 0 TAS processes:
  - fresh Hub (no line) -> Review > Cancel (declined line) -> Hub reopened (no line) -> legacy Accept Preparation
    -> "Iteration 2 prepared — waiting…";
  - reopened 1a run: "Saved results reopened — ready to review" -> Review (FAIL, 8/2/6/1, same as the window) ->
    "✕ Saved results reviewed — TM59 FAIL" -> Hub reopened (standing line again).
  - TM59 report SHA-256 unchanged and its time restored.
  - Record + before/after: `documentation/evidence/parto-hub-outcome/LIVE-ACCEPTANCE-2026-09-25.md`.
  - Driver outside git: `C:\TasOut\parto-hub-outcome-2026-09-25\scripts\hub.ps1`.

**Not represented, deliberately (the architecture cannot support it reliably).**
- A TM59 verdict after the Hub is closed and reopened. The verdict exists only once the assessment runs, so the
  standing line says "ready to review" rather than caching one.
- "Cancelled before TAS" during the SAM Check phase, as distinct from a cancel during TAS:
  `PartOSimulationOutcome` has one `Cancelled` flag. Both read "TAS run cancelled".
- A refused or not-adopted preparation returns no record (its dialog already said why), so the run's standing state
  shows.
- A reopened 2B result is just "Saved results reopened". 2B rounds are not persisted on the run.
- Live: completed PASS/FAIL after Prepare & Run, TAS cancel, 2B outcomes and stale results need TAS to produce, so
  they are unit-tested only.

**Follow-up in the same PR (owner asked for it to be fixed before merge): reopened runs named correctly.**
- The bug: `PartORun.TryResume` built the resumed context with no descriptors, so `HasVentilationUnitCatalogue` was
  false. `PartOWorkflowScenario.Find` then named a reopened Iteration 2 run "Iteration 1a" in two places:
  - the TM59 window's Scenario fact (`RunFacts`);
  - Iteration 3's reference case (`Query.PartOIterationText`).
  - It was reproduced on the unfixed code by a throwaway test.
- The fix:
  - The sidecar is now `PartORunResume:v2` with `VentilationUnitCatalogueOffered`. `Read` still accepts
    `Schema_V1`, where the value is null.
  - `PersistPartORunResume` writes it.
  - `PartOPreparationContext.Resumed(...)` records `IsResumed` and the saved value.
  - `VentilationUnitCatalogueOffered` (bool?) is the naming answer: descriptors on a live preparation, the saved
    value on a resumed one.
  - `Find` returns null and `PartOIterationText` says "MVHR iteration (1a or 2, not recorded by this saved run)"
    where it is null. No guessing.
  - `HasVentilationUnitCatalogue` and the descriptors are unchanged, so 2B gating and engineering are unaffected.
- Tests: `PartORunResumeNamingTests` (+5): end to end, Iteration 2 and Iteration 1a reopen named exactly; a v1
  sidecar resumes unnamed; the v2 round-trip; live contexts unchanged.
- Live: the saved 1a smoke run has a v1 sidecar.
  - The TM59 window now omits Scenario/Route; everything else is unchanged.
  - The Iteration 3 reference reads the "1a or 2" text.
  - Hub lines unchanged, report SHA-256 unchanged and its time restored.
- The Hub line did not name reopened runs at merge. That was superseded: the naming pass above names them from v2 sidecars.
- Progress-dialog pass: 2B still opens a generic progress window per round (`OptimisePartOTM59.cs`); the ribbon
  Review Results route still uses `ProgressBarWindowManager` (M5).
- 2B pass (H4): the 2B choice is still made before preparing. This pass only adds the Hub line after 2B.

**Next step.** The owner reviews the PR; expect Codex review rounds. Before merge, check CI is green. Then move
SAM_Deploy's SAM_UI pointer. After that, the progress-dialog pass or proposal item 3 (2B in the Part O language; needs one
short TAS acceptance run, ask first).

## Previous: Part O UX pass 2 - TM59 / Overheating result window (25 Sep 2026) - MERGED

**Status.** MERGED into `sow/2026-Q3` as SAM-BIM/SAM_UI#114, on the owner's merge order:
1. SAM-BIM/SAM#137 merged first (`7dbeb2e4`).
2. Local SAM `sow/2026-Q3` updated to it and rebuilt.
3. #114 re-checked against that merged SAM build: clean `SAM_UI.sln` Rebuild, 0 errors; focused TM59 tests 85/85;
   full WPF suite 1129/1129; the test binaries carry the merged `SAM.Analytical.dll`.
4. #114 merged.

Branched from `sow/2026-Q3` `d123f3d` (#113 merged). SAM_UI only. This is proposal item 2 of the journey
review (H3, m4, m5). M5, Review Results progress, is **not** in it: the Hub route already reports into the progress host.

The session started with 30 staged changes that exactly reverted #113 (an accident). The owner confirmed they should
be discarded, and they were (`git restore --staged --worktree :/`) before branching.

**Authority seam.** Nothing is parsed out of text and no TM59 rule is restated.
- Verdict: SAM's `TM59AssessmentReport.OccupiedSpaceComplianceStatus`. A pass goes through the existing
  `Modify.PartialAssessment` guard, the one 2B stops on: a pass over part of the dwelling scope becomes NOT ASSESSED
  with that guard's own sentence. A failure stays FAIL.
- Counts (**owner correction, round 2**): SAM_UI only TALLIES SAM's new structured per-space status,
  `TM59AssessmentReport.OccupiedSpaces[].ComplianceStatus` (SAM-BIM/SAM#137, same branch name). It is the value the
  report's "Overall" column prints; the formatter now reads it too, so the combining rule exists once, in SAM. SAM_UI
  holds no per-space rule.
- Not assessed: new additive `PartOTM59Assessment.SpaceGuids_NoResult`, the design spaces with no TM59 result of any
  kind, computed in `Assess`'s existing design-space loop. It counts spaces, not reason sentences. It decides nothing:
  `SpaceGuids_Unassessed` is still what a pass is guarded on.
- Corridor and supplementary >28 C rows are context facts (risk / information only), never an occupied-space pass or fail.
- One state, two readers. `Modify.ReviewPartOTM59` builds the summary from the assessment BEFORE any window exists.
  `Modify.ResultWindow` gives the window that instance, and the Hub's line (`Modify.ReviewOutcome`,
  `Modify.TM59OutcomeSuffix`) is worded from the same instance. Nothing is read back off the window.

**What changed.**
- `PartOTM59ResultSummary` (new, Classes/PartO) and `PartOTM59Verdict` (new enum: Unavailable / NotAssessed / Pass /
  Fail), each with a glyph (✓ ✕ – !) and a word.
  - Heading: "TM59 assessment — FAIL". Counts line: "8 spaces assessed · 2 pass · 6 fail · 1 not assessed".
  - Reason: shown only where there is no verdict.
  - Caveat: the formatter's own `Caveat`.
  - Facts: Scenario, Route, Thermal model, Weather, Results, Report saved, Method. A fact the run does not hold is omitted.
- `PartOTM59ResultWindow` layout:
  1. verdict band;
  2. facts;
  3. a "Not assessed · N reasons" bar, collapsed behind Show details (opens itself only when there is no verdict);
  4. "Detailed report", the verbatim production text;
  5. Copy All / Close.
  It merges `PartOStyles.xaml` and uses `KeepOnScreen`. `CopyAllText` is a seam.
- `Modify.ReviewPartOTM59` (internal) returns the summary. Public `AssessPartOTM59` is a wrapper with the same return
  (the production status or null). A gate refusal, or an assessment that produced nothing, now opens the window as
  UNAVAILABLE with the reason; before, it was a message box.
- Hub lines (`RunPartOWorkflow`) use `summary.VerdictWord`: Pass/Fail wording unchanged, plus "Not assessed" /
  "Unavailable" as a Warning.
- `PartOWorkflowScenario.Find(PartOPreparationContext)`: a run named as the Hub names it (iteration + catalogue offered).
- `Query.PartOThermalModelScopeText`: the one scope spelling. The Review window's `Modify.Summary` now calls it too.

**Validation.**
- `SAM.Analytical.UI.WPF.Tests`: 1129/1129 (was 1105; +24 in `PartOTM59ResultTests`). Round 2 added:
  - counts are a tally of `OccupiedSpaces`: a bedroom that passes C1 and fails C2 counts as one failing space;
  - the Hub and the window read one summary instance and cannot disagree, a 4-verdict theory;
  - no run gives no Hub line.
- SAM (#137): `SAM.Tests` 2368/2368 (+7), including that Overall equals the structured status. SAM_Tas TM59 tests
  944/944 against it.
- Round 2 live re-run on the saved FAIL run: the UI was unchanged, and the report was byte-identical again.
- `SAM_UI.sln` Release (VS 18 MSBuild `-restore`): 0 errors. `git diff --check` clean.
- Live, real exe, reopened 1a smoke run, no TAS (Hub > Review Results, and the ribbon): FAIL, 8/2/6/1 matching the
  report, 0 message boxes, Hub line unchanged. The TM59 report it rewrote was byte-identical (same SHA-256) and its time
  was restored. Record and screenshots: `documentation/evidence/parto-tm59-result-ux/LIVE-ACCEPTANCE-2026-09-25.md`.
  Driver outside git: `C:\TasOut\parto-tm59-result-ux-2026-09-25\scripts\tm59.ps1`.

**Known / not done.**
- PASS, NOT ASSESSED and UNAVAILABLE were tested by unit tests only; no saved run produces them without TAS.
- M5 (Review Results progress on the ribbon route) is still the old `ProgressBarWindowManager`.
- `Modify.PartialAssessment`'s sentence still says "space(s)" (the m1 sweep).

**Local build dependency.** SAM_UI builds against `..\SAM\build` (HintPath), so it needs SAM `sow/2026-Q3` at or after
`7dbeb2e4` (#137) for `TM59AssessmentReport.OccupiedSpaces`. Build SAM before SAM_UI. `SAM.sln`'s build also refreshes `%APPDATA%\SAM`.

**Next step.** Done: merged as `a13ba4c`. Move SAM_Deploy's SAM pointer to `7dbeb2e4` and its SAM_UI pointer to the
#114 merge commit, if not already done. Pass 3 (Hub outcome line) is the Current entry above.

## Previous: Part O UX pass 1 - Review iteration window (25 Sep 2026) - MERGED

**Status.** MERGED into `sow/2026-Q3` as SAM-BIM/SAM_UI#113 -> merge commit `d123f3d`. Implemented on
`feature/parto-review-iteration-2026-09-25` (from `sow/2026-Q3` `a3b38ee`). SAM_UI only. This is proposal item 1 of the journey review (H1, H2,
M1-M3). Presentation plus one return value: no change to preparation, engineering quantities, validation, blockers,
provenance, model mutation, or what happens after acceptance.

**What changed.**
- Heading (H2). `PartOWorkflowScenario.Find(option, selectVentilationUnit)` (SAM.Analytical.UI) returns the Hub's
  scenario; the review is headed with its text. Iteration 2 no longer says "Iteration 1a" (it printed the engine
  option's text).
- Summary. `Modify.Summary` (PreparePartOIteration.cs) now returns `PartOReviewSummary` (new,
  Classes/PartO): scenario, scope (+ isolated consequence, shown), route, design duty, equipment, overheating. Its
  `Text` is what Copy All puts first. On 1a/1b the equipment line no longer leads with the catalogue; the catalogue
  sentence is its tooltip. `PartOEquipmentAssignmentSet.AssignmentSummary` is the counts half of `Description`;
  "dwelling(s)" became a real plural (in `Description` too, not pinned by any test).
- Decision (H1). "OK" -> a primary action worded by the caller's `PartOReviewIntent` (new enum; wording only):
  Hub `PrepareAndRun` -> **Accept & Run TAS**; legacy Edit > Prepare Iteration `PrepareOnly` (also the default) ->
  **Accept Preparation**, caption "No TAS simulation is started". NOT IsDefault, so Enter never starts TAS; Cancel
  secondary, IsCancel. Entry-point constants `Modify.ReviewIntent_PrepareAndRun` / `ReviewIntent_PrepareIteration`;
  the Hub's gate before `SimulatePartO` is `Modify.ContinuesToSimulation(intent, result)` (true only for
  PrepareAndRun + Adopted). The legacy command still ends after adoption (Codex P1 round 2: the shared label had
  promised TAS there).
- Layout: scenario + run summary -> dwelling assignments -> spaces -> diagnostics -> decision row. Both tables keep
  every column. Merges `Themes/PartOStyles.xaml` (wrapped tooltips, palette, section headings).
- Diagnostics (M1). One bar "Diagnostics  Warnings (8) · Notes (38)" with "Show details" (same switch as the Hub) and
  Copy All. Box collapsed by default; opens by default only when there are refusals. Every line still in the box and
  Copy All. `PartODiagnosticSummary.Header` separator is now " · ". The switch listens to Checked/Unchecked (a
  Click-only handler did not answer a UIA toggle - found live).
- M2: Convert to Manual, Assign suggested and the bulk row are not drawn when there is no assignment set (1a/1b);
  enable rules unchanged. 1b's empty table is replaced by one sentence.
- M3: 1a rows name the dwelling (Flat 1) like Iteration 2. `Modify.DwellingNames_AirHandlingUnit` is the one
  resolution; `PartOEquipmentRow` ctor gained an optional `dwellingName`.
- Cancel -> Hub. `PrepareAndReviewPartOIteration` returns `PartOPreparationResult` (NotPrepared / Declined /
  Adopted); the public bool `PreparePartOIteration` wraps it. On Declined, `PrepareAndRun` returns
  `Modify.DeclinedOutcome`: "○ <scenario>: review cancelled before TAS · no simulation was run and the model is
  unchanged". That is the Hub's existing last-outcome line (session-only, not persisted) - no new state.
- The post-dialog code moved unchanged into `Modify.ConcludePartOReview(accepted, ...)`, so Cancel/Accept are
  testable without a dialog.
- Safe placement: `PartOWindowPlacement.KeepOnScreen` (new) is the Hub's KeepOnScreen, shared; the Review window
  calls it on first render. The Hub now delegates to it (same behaviour).

**Validation.**
- `SAM.Analytical.UI.WPF.Tests`: 1105/1105 (was 1077; +28 in `PartOReviewIterationTests`, of which round 2 added
  wording per entry point, unconfigured default, the continuation truth table, prepare-only acceptance adopts but
  does not continue, Hub acceptance continues, Cancel continues neither; round 1: headings 1a/1b/2 and no
  "(s)", Accept & Run TAS wording / not default / Esc cancels, modal Cancel->false Accept->true, Cancel adopts nothing
  and leaves the run unprepared, Accept adopts via the production path, Hub declined line, diagnostics collapsed with
  every line reachable, refusal opens them, UIA toggle, equipment controls only for Iteration 2).
- `SAM_UI.sln` Release (VS 18 MSBuild `-restore`): 0 errors.
- Live, real exe, 1a/1b/2 each to Review -> Cancel, no TAS: 0 message boxes, headings correct, counts match lines,
  Hub line shown; re-run after round 2: still "Accept & Run TAS". Legacy Prepare Iteration: "Accept Preparation"
  shown, ACCEPTED, run prepared (ribbon "prepared but not simulated", Hub "Prepared · waiting for the full-year TAS
  run"), no TAS process, 0 message boxes. Record + before/after screenshots: `documentation/evidence/parto-review-iteration/`.

**Known / not done.**
- The Hub's "Accept & Run TAS" was not pressed live (it would start TAS); covered by the adoption + gate tests.
- Safe positioning not forced live; isolated scope and refusals only unit-tested; non-100% DPI not tested.
- Catalogue description still says "product(s)" (other windows share it) - consistency sweep item.

**Next step.** Done: merged as `d123f3d`. Proposal item 2 (TM59 result) is the Current entry above.

## Previous: Part O journey review - observe only (25 Sep 2026)

**Status.** Research only. No code was changed and no TAS simulation was started. It walked the four Part O cases
(1a, 1b, 2 with 2B as its follow-on, and 3) in the real exe on the merged #111 build (`90b42e0`), using UI Automation +
PrintWindow. The record, findings and proposal are in
`documentation/evidence/parto-journey-review/JOURNEY-REVIEW-2026-09-25.md`, with 15 live screenshots. The full report
is a private artifact: https://claude.ai/artifact/CV9V9fjuMwjuRZaij5yUd1.

**Coverage.**
- Live: the Hub (fresh 1a/1b/2); Review iteration for 1a/1b/2, each declined before TAS; the legacy picker.
- Live, on the reopened 1a smoke run: TM59 Review; Iteration 3 Open result, comparison and Technical details; the
  Run again confirmation (answered No); the pre-flight for all four methods.
- Earlier evidence or code: the TAS progress, the 2B rounds and result, the Hub after 2B, and the attention box.
- Result: 0 unexpected message boxes, no TAS process.
- The driver is `C:\TasOut\parto-journey-review-2026-09-25\scripts\journey.ps1`, outside git.

**Top findings.** These are presentation findings, awaiting owner review.
- H1: Review iteration's unlabelled OK starts the full-year TAS run; Cancel leaves no Hub line.
- H2: the Iteration 2 review summary heads with "Iteration 1a - MVHR design duty (no manufacturer unit)".
- H3: the TM59 verdict is only on report line 7; there is no summary.
- H4: 2B is outside the Part O language:
  - the choice is made before preparing;
  - each round opens its own generic progress window (`OptimisePartOTM59.cs:304`, no `PartOProgressHost`);
  - the Hub shows no outcome after 2B (`RunPartOWorkflow.cs:188`).
- Plus 9 medium and 10 minor findings, listed in the record.

**Trap.** Reviewing a copied run writes its TM59 and Iteration 3 review reports at the run's recorded ABSOLUTE paths,
that is beside the original run. Preserve file times when copying runs (`cp -p`): `.partorun.json` checks the TSD
timestamp. The bytes here were identical, and the original times were restored.

**Next step.** The owner reviews the proposal. The proposed first implementation PR is "Review iteration: decision
and identity" (H1, H2, M1-M3): presentation only, with a live acceptance pass like #111. The 2B step (H4) needs one
short TAS acceptance run (Iteration 2 about 1 min + 2B about 10-12 min); ask before launching it.

## Previous: Part O Prepare & Run Hub - presentation pass (25 Sep 2026) - MERGED

**Status.** MERGED into `sow/2026-Q3` on 2026-09-25 as SAM-BIM/SAM_UI#111 -> merge commit `90b42e0`
(head `2fd47a9`). CI was green (build, spdx). The feature branch `feature/parto-hub-presentation-2026-09-25` is deleted.
It was taken from `sow/2026-Q3` `6230d7d`, after a live acceptance pass in the real exe. SAM_UI only.

Presentation-only, by owner brief. There is no change to the workflow, scenarios, validation, preparation,
simulation, provenance, the inspection's stage statuses, or any enable rule.

**What changed.**
- Layout. `PartOWorkflowWindow.xaml` now runs top to bottom:
  1. scenario, with one route line;
  2. scope, with a compact text beside it;
  3. the workflow strip;
  4. the last-outcome line;
  5. "Readiness" (compact rows, with one "Show details" switch);
  6. blockers;
  7. equipment (Iteration 2 only), simulation case, 2B (only where `SupportsOptimisation`), Iteration 3 and
     "Advanced / Details";
  8. a fixed bottom region with the "Next: ..." line, the Prepare & Run primary button, and captions under the
     disabled Review ("No results yet") and Optimise ("Iteration 2 only" / "After an Iteration 2 run").
- Strip. `PartOWorkflowProgress` + `PartOWorkflowStep` (Classes/PartO/PartOWorkflowStep.cs) is a pure mapping
  from the inspection's statuses.
  - Steps: Configure -> Prepare model -> **Check & simulate** -> Review (+ Optimise 2B).
  - Check and simulate are ONE step on purpose: Model check is always Pending in the inspection, and there is
    no recorded check outcome (owner guardrail: no UI-only state).
  - Every state is shown as a glyph and a word, not only a colour.
  - `PartOWorkflowStepStripControl` is the reusable control.
- Shared styles. `Themes/PartOStyles.xaml` is a small shared dictionary: palette, PrimaryButton, Caption,
  SectionHeading, StatusGlyph, BooleanToVisibility. Other Part O windows can merge it.
- Status rows.
  - `PartOWorkflowStatusRow.StatusLabel` is sentence case, per stage and status: Waiting, Not prepared,
    Not available, ...
  - `StatusGlyph` is ✓ ✕ ○ –.
  - `StatusText` is unchanged.
  - `ShortDetail` = host summary, else `PartOWorkflowStageState.Summary` (new, optional, set by the
    inspection from its own counts: "3 dwellings · 8 spaces", "8/8 spaces mapped", "8/8 spaces defined"),
    else the first-sentence cut.
  - The per-row "Why?" expanders are replaced by one "Show details" switch
    (`ShowStatusDetails` DependencyProperty); the full detail is also on each row's tooltip.
- Plurals. `UI.Query.PartOCount/PartONoun`. Every `(s)` is gone from the inspection details and the Hub's
  scope and selection text. `Detail` is UI-only; nothing persists it.
- Equipment is said once, as the route line ("MVHR · Design duty only · No manufacturer unit required").
  - On 1a/1b the equipment section is collapsed; the control is still compact behind it, so its tests are
    unchanged.
  - The N/A Equipment row is not DRAWN (`RenderedStatusGroups`), but it stays in `StatusRows`/`StatusGroups`.
  - A pointer sits in Advanced / Details.
- Iteration 3. The whole panel is collapsed unless eligibility `CanRun` or a record exists (the same rule
  that already drove its inner panel).
- Simulation case. The header shows weather and solar method only; the output path and the full case are on
  the tooltip. Fixed: the setter's per-control writes judged a half-written case and auto-expanded the section
  on every open. They are now judged once, when complete.
- Fixes from the live pass:
  - Mixed state (reopened run): the design row's short line becomes "Rebuilt for the next Prepare & Run · the
    existing results are reviewable as they are" (window `Summary`, only when ResultsAvailable and the design is
    Prepare). The Prepare-model step's tooltip says the same. The status is unchanged.
  - The Optimise tooltip on 1a/1b gives the scenario reason (the 2B section's sentence).
  - Strip steps are hit-testable (transparent background), so their tooltips appear.
  - An implicit wrapping ToolTip style (MaxWidth 480) in `PartOStyles.xaml`.
  - `KeepOnScreen`: the Hub is moved up if its bottom is below its monitor's working area. Live, a reopened
    run opened at y=208 with a height of 910 on a 1080 px display. It runs on first render and again when
    content-driven growth makes the Hub taller (`OnGrown`; not after a grip resize, `SizeToContent = Manual`).
    Codex review on #111 found three edge cases, all fixed: `MaxHeight` is capped to 92% of the ACTIVE
    monitor (it was read from the primary); `MinHeight` comes down with it on a working area shorter than 520;
    and growth after placement is handled. The arithmetic is the pure `PartOWorkflowWindow.Placement`.
  - Codex review on #111: with reviewable results open, a blocked Prepare & Run no longer hides Review. The strip
    marks Review as the current step, and the next-step line leads with Review and states the blocker beside it.

**Validation.**
- `SAM_UI.sln` Release (VS 18 MSBuild `-restore`): 0 errors.
- `SAM.Analytical.UI.WPF.Tests`: 1077/1077. The previous count was 1057 (1056 + the new opt-in harness).
- Live acceptance in the real exe (UI Automation + PrintWindow): 0 message boxes; fresh 1a, 2 and 1b; Show
  details; Simulation case; tooltips; captions; 700/1200 px widths; move to a second monitor; the reopened 1a
  run with Iteration 3 shown. Record: `documentation/evidence/parto-hub-presentation/live/LIVE-ACCEPTANCE-2026-09-25.md`.
  All monitors are at 96 DPI; no non-100% scale was tested (the system setting was not changed).
  - 7 pins were updated for the wording: plurals, and the template test now asserts label + glyph +
    ShortDetail + FullDetail.
  - New: `PartOHubPresentationTests` (20 cases, including 6 placement-theory rows, a real-window growth test, and Review staying next while Run is blocked).
    Plus one assertion that the Iteration 3 panel is visible with a recorded result.
- Before/after renders (fresh 1a, and 1a after its run): `documentation/evidence/parto-hub-presentation/`, via
  the opt-in `PartOWorkflowHubScreenshotHarness` (see its README).

**Known / not done.**
- Other Part O windows still use `(s)` wording: the Part F report, the project test product, the result
  reopen summary, and the Iteration 3 pre-flight refusal. Out of scope for this pass.
- The Iteration 2 route line carries the whole mode description (3 sentences), which tests pin. It could be
  trimmed in a later pass.
- A non-100% DPI scale was not tested live.

**Next step.** Done: merged as `90b42e0`. SAM_Deploy's SAM_UI pointer moves `6230d7d1` -> `90b42e0e`; the other
three pointers were already at their tips via SAM_Deploy#47.

## Previous: Part O / TM59 workflow simplification (24-25 Sep 2026) - MERGED

**Status.** MERGED into `sow/2026-Q3` on 2026-09-25: SAM-BIM/SAM_UI#109 -> merge commit `75d7b7cd` (implementation
`2ab49f1`, live smoke evidence + regression test `4588412`, note `b9acc83`). CI green (build, spdx); Codex review
completed with no findings. Feature branch deleted. SAM_UI only, from `sow/2026-Q3` `c7281ca`.
SAM / SAM_Systems / SAM_Tas unchanged (engineering behaviour frozen after the merged Nuaire work).

**Environment.** All four repos at `origin/sow/2026-Q3` (SAM `80b01052`, SAM_Systems `2213373`, SAM_Tas `39828c6`,
SAM_UI `c7281ca`), rebuilt in order with VS 18 MSBuild Release `-restore` (0 errors); `SAM_UI\build` refreshed.

**Owner decisions (24 Sep).** (1) Iteration 3 results stored per Reference A + method, legacy `-Iteration3.json`
read by the mode inside it; a refused attempt never locks Review. (2) The Part O Simulate dialog folds into a Hub
"Simulation case" (weather, output folder, solar method). (3) B4 kept, under Advanced. Plus: pre-flight per unit
before Run; fix grouped-grid virtualisation; honest stage-level progress; no success OK box. UX rule: engineer-facing
text uses Part O iteration language - "Iteration 3 comparison", Reference case / System case; no A/B, B0, B4, MG,
"pairing" in the UI (they stay in code/files). Product methods first; route check (B0) and published cooling table
(B4) are "Advanced - validation methods".

**What changed (all SAM_UI).**
- Storage: `PartOIteration3Paths` - per-method record `<run>-Iteration3-<B0|BP|B4|MG>.json`, report
  `<record>-Review.txt/json`; SelectedProduct gets its own Candidate B suffix `-It3BP` (was shared with B0).
  `Query.PartOIteration3RecordPath/PairingStatus(es)` resolve per-method first, then the legacy file only if its
  recorded mode matches. `PartOIteration3Eligibility.PairingStatuses`; Review = a COMPLETED record only.
  `ReviewPartOIteration3(run, pipeline, mode?, step?)` (null mode = legacy file, else first per-method record).
- Record: additive `Guidance` list (`PartOIteration3GuidanceEvidence`, SAM_UI core) written by the MG run; older
  records read as none. `Query.PartOIteration3GuidanceEvidence` re-reads it for an old record ONLY if the catalogue
  SHA-256 equals the recorded one, else falls back (says so). Note text and report bytes unchanged.
- Run: `RunPartOIteration3(..., stageStarting)` announces each ledger stage; cancel honoured before ThermalSource,
  SystemsConversion (Route), ResultantTemperature, CandidateBTM59 (refusal "Cancelled", method re-runnable).
- Pre-flight: `Query.PartOIteration3Preflight` = eligibility + system scope + the run's own resolution query for the
  method (no TAS); per-unit rows, grouped summary, refusals shown before Run is enabled.
- Progress: `PartOProgressState` (pure, injectable clock) + `PartOProgressHost`/`PartOProgressWindow` (own STA
  thread, topmost, 0.5 s poll, indeterminate bar, elapsed, Cancel between stages). While `PartOProgressHost.Current`
  is set, `RunWorkflow` and `RunPartOSimulation` report steps into it instead of their own dialogs; modals hide it.
- Simulate: `Modify.SimulatePartO(ui, run, PartOSimulationCase)` builds the same inputs the locked dialog returned
  (SimulateOptions_PartO + the 3 fields; days 1-365) and calls the shared core; returns `PartOSimulationOutcome`
  instead of the "Time elapsed" box. Dialog path (`Simulate(ui, run, bool)`, Energy Simulation) unchanged.
- Hub (`PartOWorkflowWindow` + `.Iteration3.cs`): Simulation case expander, inline last-outcome line, Iteration 3
  panel (reference case text, per-method status, explanation, pre-flight, Open result / Show last attempt, Run
  system case / Run again with a replace confirmation). Old bottom Iteration 3 button and method combo removed.
  `PartOWorkflowAction.Iteration3Review` appended. `RunPartOWorkflow` carries method + case + outcome across showings.
- Comparison window: title "Iteration 3 comparison"; header in engineer terms; stage line; tiles (reference /
  system TM59, mean diff, RMS, largest diff, outcomes changed); system card (guidance as key/value + per-unit
  grid; products for other methods); chips Failures/Changed/Largest differences/All + search; dwelling groups as
  collapsed Expanders with per-dwelling stats, `IsVirtualizingWhenGrouping=True`, auto-open when a filter leaves
  <= 300 rows; Technical details holds the verbatim outcome/summary/stage ledger/notes; Copy All unchanged.
  Column MinWidth pins widths (WPF leaves them unresolved when no row is realised).
- `AssessPartOTM59` now returns the verdict and uses the shared progress window when one is current.

**Validation.** `SAM_UI.sln` Release: 0 errors. `SAM.Analytical.UI.WPF.Tests`: 1056/1056 (1031 before; 22 updated
for the per-method names/labels and the completed-only Review rule; new: `PartOWorkflowSimplificationTests`, run
tests for per-method record / stage order / cancel-before-TAS, eligibility per-method/legacy/refused).
5,000-room test: 1000 dwellings x 5 rooms x 3 criteria = 15 000 rows open with <100 rows realised, groups collapsed.
Real data, no TAS (`PartOWorkflowEvidenceHarness`, opt-in via `SAM_PARTO_EVIDENCE` + `SAM_PARTO_SCREENSHOTS`) on
`C:\TasOut\parto-guidance-2026-09-24\03-Resume`: 1a reopened (0.8 s, resume OK); legacy MG record seen as MG
completed; pre-flight MG ready (3 x Nuaire), SelectedProduct refused before TAS (template states no heat recovery);
review 8.8 s, bias 0.567 K / RMSE 1.477 K / max 4.121 K (= acceptance); guidance card falls back because the
catalogue SHA changed since that run. Report files the review rewrote were restored. Screenshots in the session
scratchpad only (not in git).

**Live smoke tests (25 Sep, real exe + TAS, UI Automation).** Record:
`documentation/evidence/parto-workflow-simplification/LIVE-SMOKE-2026-09-25.md` (+ 6 screenshots); raw logs and
scripts outside git in `C:\TasOut\parto-workflow-smoke-2026-09-25\`.
- Prepare & Run (1a, acceptance model a7e09a25): Simulation case in the Hub, Review iteration shown, no Simulate
  dialog, one progress window through the whole TAS run (window watcher + its own pixels), TAS 50-54 s, TM59
  opened, inline Hub line, 0 message boxes (5 passes).
- Iteration 3 guidance from that 1a: reference case named, guidance default, pre-flight 3 x Nuaire, no picker,
  6.7 min with the progress window updating throughout (moved/minimised/restored), comparison +0.52 K / 1.41 K /
  4.14 K, reopened in-session (12 s) and in a fresh process (14.7 s, TSD reader only, no TBD/TPD).
- Fixed/added after the smoke: regression test that the progress window keeps its content after Hide/Show.

**Known follow-ups (not blocking).** UI Automation exposes no elements for the progress window after it
re-shows (pixels correct - accessibility only). Main window still busy while TAS holds the UI thread. No sub-step
text inside the Iteration 3 TAS calls. Pre-existing 'Reloading' flash on model adoption. 2B history grid builds
all rows eagerly.

**Next step.** Bump SAM_Deploy's pointers - all four are behind `sow/2026-Q3` (SAM_Deploy `83b2c37`, PR #46,
predates the Nuaire-reply merges): SAM `875655fa`->`80b01052`, SAM_Systems `df5dd332`->`22133736`,
SAM_Tas `f7d39351`->`39828c69`, SAM_UI `4460dc3a`->`75d7b7cd` (or later, to include this note). Separately: the
UI Automation accessibility follow-up on the progress window (non-blocking).

## Previous: Nuaire reply (24 Sep 2026) - exchanger then DX drop, 13 C floor

**Status.** MERGED into `sow/2026-Q3` on 2026-09-24, in order: SAM-BIM/SAM#133 (`54c43438`) -> SAM-BIM/SAM_Systems#29 (`c88c9b37`) -> SAM-BIM/SAM_Tas#65 (`1b659756`) -> SAM-BIM/SAM_UI#107 (`8f58144c`). CI green and Codex review clean (all findings fixed and answered) on every PR. The `feature/parto-nuaire-reply-2026-09-24` branches are deleted.
The full cross-repo record (evidence, decisions, TAS probes, MG
acceptance, residual uncertainties) is in SAM's `PROJECT_PROGRESS.md`, *Current* entry.

**What Nuaire's reply (A. Nash, 24 Sep 2026) changed.**
- The cooling supply is no longer `to - X`, which Nuaire called a simplified IES work-around valid at 32 C only.
  It is now the exchanger (bypass, or recovery at eta(Q)), then the DX drop less the supply-motor heat, never
  below 13 C. A coil does not heat.
- The bypass is to >= 12, to < extract, extract >= 19 C. The MVHR decides it independently of the cooling-stat.
- The cooling airflow is 60-120 l/s, with an 80 l/s default. A dwelling's own figure overrides it.
- The brochure's 2.2 kW is a combined coolth-recovery + sensible figure, not a DX duty, and is no longer used.

**This repo.**
- `WPF/.../Query/PartOIteration3GuidanceResolution.cs`: the MG resolution note text only. It describes the
  exchanger-then-coil rule (fraction, net drop, 13 C minimum at the resolved airflow) and that bypass is
  independent of the cooling-stat.
- No behaviour change here. The MG results change comes from SAM, SAM_Systems and SAM_Tas.

**Validation.** Tests: SAM 2252, SAM_Systems 256, TM59 944, WPF 1031, all passing. Framework MSBuild: 0 errors
in all four repos.
- Representative MG annual run (closeout): COMPLETE, bias 0.53 K, RMSE 1.413 K.
  - The law and the exchanger state are exact in every full-flow hour.
  - 0 hours cooled below 13 C by the coil.
- The review fixes after that run (inclusive bypass minimums, extract axis beyond 45 C, floorless summary
  text) were not rerun annually, by owner decision: they affect only exact-threshold hours (intake exactly
  12.0 C, background hours) and extracts above 45 C (none in the MG).
- This is manufacturer modelling guidance; Nuaire has not called it certified or approved.

**Next step.** Bump the SAM_Deploy pointers to the merge commits above. Remaining manufacturer questions
are listed in SAM's `PROJECT_PROGRESS.md`.


## Previous: SAM#123 manufacturer guidance - Iteration 3 mode "Selected product - manufacturer guidance" (2026-09-24)

**Final integration review (2026-09-24, before merge; the merges followed).** One consolidated review of all four branches
against `sow/2026-Q3`; no blockers, no code changed at review.
- Each branch merges into current `sow/2026-Q3` without conflicts. The diffs are limited to the #123 guidance
  scope. The new public surface is additive, and a template, settings or route without guidance serialises
  and materialises as before.
- Local tests on the branch heads: SAM.Tests 2218/2218, SAM.Analytical.Systems.Tests 251/251,
  SAM.Analytical.Tas.TM59.Tests 938/938 (7 guidance-cooling), SAM.Analytical.UI.WPF.Tests 1031/1031.
  SAM#125 and SAM_Systems#25 CI green.
- Evidence re-checked on disk:
  - B0 TM59 reports equal the 2026-09-23 ones except the `Source:` path line.
  - The Resume MG TM59 report equals the in-session MG one except the path line. The hourly comparison agrees
    to 3 d.p. (bias 0.5672 vs 0.5674 K). It is not bit-identical, because Resume ran a fresh 1a.
- Non-blocking findings (JSON edge cases, read-back strictness, stale comments, test gaps) are in
  [SAM#130](https://github.com/SAM-BIM/SAM/issues/130). They were deliberately not fixed, so that the merged code is
  the accepted code.
- The DisplacementVent wet-room issue is [SAM#129](https://github.com/SAM-BIM/SAM/issues/129). It is not changed here.
- Wording: "certified" appears only in negations; values stay PROVISIONAL pending Nuaire.

**Status.** MERGED into `sow/2026-Q3` on 2026-09-24, in dependency order:
- [SAM#125](https://github.com/SAM-BIM/SAM/pull/125) -> `1f9a5b95`
- [SAM_Systems#25](https://github.com/SAM-BIM/SAM_Systems/pull/25) -> `6c042609`
- [SAM_Tas#63](https://github.com/SAM-BIM/SAM_Tas/pull/63) -> `83653aaa`
- [SAM_UI#105](https://github.com/SAM-BIM/SAM_UI/pull/105) -> `620aa701`

All four PRs had green CI. The `feature/parto-nuaire-manufacturer-guidance` branches are deleted. All product
values are still PROVISIONAL Nuaire guidance: Nuaire was emailed and has not yet confirmed.
[SAM#123](https://github.com/SAM-BIM/SAM/issues/123) stays open.

**Where it came from.**
- The Stage 11 TAS prototype passed. Its evidence is under
  `C:\TasOut\nuaire-guidance\REVIEW2\resume-2026-09-23\evidence\stage11\`.
- Stage 11b, the production correction, was found on the real model:
  - Controlled dampers do not converge when a supplied room's only outlet is a transfer damper
    (MVHR-02/03: more than 20 min for one day, against 3 s with the controllers pinned).
  - The elevated airflow is now carried on the fans, with the dampers uncontrolled at their elevated share.
    Room-stat controllers drive the supply and extract fans with Min = (design/elevated)^2, because a
    controlled fan's airflow goes as the square root of its signal.

**Per repo.**
- **SAM:**
  - `SupplyTemperatureRule.IntakeOffset`: `to - X(Q)`, Refuse outside the stated airflows, with reported
    domain use.
  - `CoolingActivationSignal` (room / extract).
  - A room-stat bypass keeps the rule's extract <= activation condition.
- **SAM_Systems:**
  - The Nuaire entry uses IntakeOffset 70/80/90/100/110 -> 15/14.5/14/13.5/13 K. There is no 16 C floor
    (Nuaire, 13 Aug 2025), activation is on the room stat, and the entry is marked PROVISIONAL.
  - `GuidanceSettings` materialisation: MVRE plus a supply DX coil after the exchanger.
  - `MechanicalVentilationGuidanceCooling` is the record the TAS grounding reads.
  - `Query.MechanicalVentilationGuidanceSettings`: the elevated airflow is the midpoint of the stated range
    (80 l/s), refused above unit capacity.
- **SAM_Tas:**
  - `Modify.GroundGuidanceCooling` writes and reads back:
    - DX room-stat controller: Studio/Bedroom zone, `SensorArc1`, 22.05 / 0.1 K;
    - fan controllers;
    - uncontrolled exchanger with an (ODB, EDB2, EFlow) state table;
    - DX: finite 2200 W (the table's max combined capacity), no gates, `MinimumOffcoil = to - 14.5`.
  - A disagreement refuses the grounding.
  - `Modify.GuidanceCoolingResults` gives the hourly read-back.
- **SAM_UI:**
  - New mode `SelectedProductManufacturerGuidance`, with `-It3BMG` files, a resolution step with no product
    constants, and the operation CSV and summaries persisted in the record.
  - Review-consistency branch for the mode.
  - Separately: Iteration 3 can start from a completed 1a reopened in a later session.
    `<project>.prepared.sam` and `<project>.partorun.json` are bound to the TSD, so there is no need to
    re-run Prepare & Run.

**Acceptance (2026-09-24, real `SAM Analytical.exe`, `SAM_zoningAM-CIBSEfutureZ1.sam` a7e09a25, DSY1 2050s).**
Evidence is in `C:\TasOut\parto-guidance-2026-09-24\`, outside git.
- **B0 on the new binaries:** COMPLETE. Both TM59 reports are byte-identical to 2026-09-23; bias 0.05 K,
  RMSE 0.736 K. So B0 is unchanged.
- **MG:** COMPLETE in 11.7 min, A Fail / B Fail. TM59 >26 C hours against A:
  - Studio -68 and bedrooms -101 / -96, where B0 is -6 / +8 / +12;
  - kitchens -38;
  - wet rooms -284 to -471 (Bathroom_2 becomes a Pass).
  - Annual mean bias +0.57 K, RMSE 1.48 K, from heat recovery outside cooling. The DisplacementVent-inflated
    extract (3.5-6 kh extract > 22 C while room <= 22 C) suppresses bypass. It is a model-hygiene decision
    and not changed here.
- **MG TAS read-back, per unit (design -> elevated):**
  - MVHR-01: 30 -> 80 l/s, supply = extract (< 0.02 l/s). `to - 14.5` is exact in 849/849 full-flow cooling
    hours that are not capacity-limited. 122 h are at the 2.2 kW total-duty bound.
  - MVHR-02/03: 63 -> 80 l/s. The law holds in 699/700 and 692/693 such hours; 85 h are capacity-limited.
  - All units: minimum supply 7.3 C; the stat room peaks at 36.9-39.3 C in a 40 C intake heatwave, with the
    supply at exactly `to - 14.5`.
- **Reopen:** a new session opened the saved 1a run and reviewed the MG pairing in 10.8 s with no simulation.
- **Resume:** a fresh 1a wrote its sidecar. A new session then opened that saved 1a run and ran Iteration 3
  (MG) with no Prepare & Run, COMPLETE in 14.0 min. The comparison matches the in-session MG run to 3 d.p.
  (bias 0.567 K, RMSE 1.477 K, max 4.121 K). Evidence: `C:\TasOut\parto-guidance-2026-09-24\03-Resume\`.
- **Installed state changed on this machine:**
  - `Documents\SAM\resources\...\VentilationUnitCatalogue.JSON` is now the v3 feature catalogue. The v1
    backup is at `C:\TasOut\parto-guidance-2026-09-24\catalogue-backup\documents-SAM-before.json`. Restore it
    before running `sow` binaries.
  - `%APPDATA%\SAM\SAM.Analytical.dll`/`SAM.Core.dll` and `SAM.ghlink` were overwritten by a `SAM.sln`
    build on 2026-09-24 09:44. Redeploy from `sow` to restore Grasshopper.

**Exact next step.**
1. On a clean checkout of merged `sow/2026-Q3` (all four repos), run the minimal merged-state acceptance.
   Reuse the saved runs; no new long simulation is needed.
   - Build SAM, SAM_Systems, SAM_Tas (Framework MSBuild) and SAM_UI from `sow/2026-Q3`, and run the four test
     projects.
   - Deploy those binaries. The v3 `VentilationUnitCatalogue.JSON` must be in `Documents\SAM\resources`.
   - In `SAM Analytical.exe`, reopen `C:\TasOut\parto-guidance-2026-09-24\03-Resume\` (1a with sidecar) and
     `02-Iteration3-MG`. Confirm review-only reopen of the MG pairing (A Fail / B Fail, bias 0.567 K) and that
     the reopened 1a reports that Iteration 3 can start.
   - Optionally re-run only B0 from the reopened 1a. Its TM59 reports must equal
     `01-Iteration3-B0` except the `Source:` line.
2. Bump the SAM_Deploy submodule pointers to the four merge commits.
3. Hold any "certified" wording until Nuaire replies (stat location, X vs airflow, low-ambient behaviour,
   30 l/s).
4. Decide the DisplacementVent wet-room issue ([SAM#129](https://github.com/SAM-BIM/SAM/issues/129)) and the
   non-blocking hardening ([SAM#130](https://github.com/SAM-BIM/SAM/issues/130)) separately.

## Branch
`build/netfx-cleanup-closeout`, based on `sow/2026-Q3` `9f515c4` (PR5B, #103, is merged). See the *Latest* entry
immediately below; the PR5B SAM_UI slice entry after it is merged history.

PR #98 (`feature/parto-equipment-selection-ux`) is merged and the Approved Document O iteration
programme is **FROZEN** - see `PART O ITERATIONS 1a / 1b / 2 / 2B - FROZEN` below, which remains the
authority for the engineering state.

Everything below the *Latest* entry is superseded history retained for context, and its forward-looking
claims (branch names, "next step" lists) are historical rather than current.

## LATEST - BUILD: .NET FRAMEWORK LEFTOVER CLEANUP CLOSEOUT (2026-09-22)

Branch `build/netfx-cleanup-closeout`, PR against `sow/2026-Q3`. This closes out the repo-family .NET Framework leftover cleanup ([SAM#126](https://github.com/SAM-BIM/SAM/pull/126) + 17 siblings, merged 2026-09-22).

- Dropped bare framework references from 13 net8.0-windows projects (`Application/*`, `Grasshopper/*`, `SAM_UI/*`, `WPF/*`):
  - `<Reference Include="System.Data.DataSetExtensions" />` (13 projects) and `<Reference Include="Microsoft.CSharp" />` (13).
  - `<Reference Include="System.IO.Compression" />` from `SAM_UI/SAM.Geometry.UI` and `WPF/SAM.Analytical.UI.WPF`.
  - `<Reference Include="PresentationFramework.Aero2" />` from `WPF/SAM.Core.UI.WPF` (`UseWPF` supplies it).

  All of these come from the .NET 8 shared framework, so the bare references were redundant. They caused every MSB3243
  "no way to resolve conflict" warning in SAM_UI in the full BuildAlls.
- **Kept on purpose:** the 4 `Application/*/App.config` files. They belong to WinExe applications, the only place an app config is read. The matching `SAM Analytical.dll.config`-style files in `%APPDATA%\SAM` are expected.
- Validation: full `BuildAlls_v4.bat` clean rebuild with the closeout branches after the `System.Data.DataSetExtensions` /
  `System.IO.Compression` removal - exit 0, 0 errors, MSB3243 30 -> 15. The remaining 15 were exactly the
  `Microsoft.CSharp` / `Aero2` references removed afterwards. Re-run after that second removal: exit 0, 0 errors, **0 MSB3243**.
- Next step: none for this cleanup. The Part O state below is unchanged.

## PREVIOUS - PART O ITERATION 3 PR5B SAM_UI SELECTED-PRODUCT COOLING MODE (B4) (2026-09-15)

Branch `feature/parto-pr5b-recirculation-cooling` @ `0d1833ed` (+ this docs commit), PR against
`sow/2026-Q3`, **not merged**. It depends on the SAM_Systems and SAM_Tas PR5B slices, which have the same
branch name; merge order is SAM_Systems -> SAM_Tas -> SAM_UI. SAM needs no change.

- `PartOIteration3BehaviourMode.SelectedProductCooling` = **B4 = B0 Parity ventilation (`MV.json`, no unit
  settings, `ClearToZero`) + the selected product's cooling module**, so B4 - B0 is the cooling layer alone.
  - This is deliberate: the ventilation layers (E1/E2, EDSL) stay evidence-blocked, and the frozen branch
    no longer sits on the MVRE path.
  - `documentation/evidence/PR5B-PRODUCTION-ACCEPTANCE.md` §2 has the reconciliation.
- `Query.PartOIteration3CoolingResolution` resolves the catalogue `PerformanceTable` +
  `FlowFractionByControlTemperature` into these values:
  - ceiling = the table's airflow-axis maximum, refused above capacity;
  - gate = the law's lower temperature.
  - It is all-or-nothing and never reselects.
- `PartOIteration3CoolingBindings` binds each row to one branch by AHU guid; `PartOIteration3CoolingOutcomes`
  records each outcome verbatim from the route's checked evidence.
- A per-unit `PartOIteration3CoolingEvidence` row is kept in the record, and the report prints it.
- The `-It3B4` path suffix and an OperatingAirFlow CSV preserve B0's documents.
- Review refuses: contradictory records, missing provenance, rows not one-per-system, and refused behaviour.
- The mode is added to the UI picker.
- Tests: `SAM.Analytical.UI.WPF.Tests` 1027/1027 (+15).
- Licensed paired annual acceptance through SAM_UI's production resolution:
  - B0/B4 TM59 8/8 Pass, 0/8 outcome changes;
  - B0 reproduces PR4's frozen per-room hours;
  - 0 h cooling below the 22 C gate, 0 h heating;
  - OperatingAirFlow 36..120 l/s;
  - B4 vs re-keyed B4 bit-identical.
  - Full results are in the evidence doc.

## PREVIOUS - PART O ITERATION 3 PR5A SAM_UI GENERIC/FAIL-CLOSED SLICE (2026-09-12)

### Current status

Branch `codex/parto-pr5a-ui`, ready to commit/open against `sow/2026-Q3`. SAM #111's ordered lower
layers are already merged: SAM #117 at merge `b4a1283f` (reviewed head `f6b0d3f9`, 2151/2151),
SAM_Systems #23 at merge `5213ba9c` (head `3772fc3`, 176/176), and SAM_Tas #56 at merge `0f7f59e0`
(head `de7a7a9`, 890/890). No repository other than SAM_UI is changed by this checkpoint.

The generic PR5A path is implemented and deliberately fail-closed. Parity remains the default frozen B0
and sends no unit settings with `ClearToZero`. Selected product reads Iteration 2's authoritative AHU
selection, resolves every scoped unit or refuses the whole attempt, rechecks capacity without reselection,
uses MVRE materialisation with per-AHU settings, and routes fan heat as `FromSystemsGraph`.

The v2 pairing record/report persists behaviour, exact catalogue directory/file/schema/SHA-256,
selected identity/source/capacity, separate design and Part F duties, operating basis, resolved HR/SFP,
generic fan mappings, declared assumptions and deterministic AHU-to-AirSystem lineage. Review remains
simulation-free and refuses unsupported modes, contradictory parity evidence, incomplete provenance,
incomplete equipment rows or duplicate physical lineage. The result window and persisted report share
the same text authority; no new equipment selection UI or authority was added.

Review amendment: pre-PR5A `PartOIteration3Record:v1` pairings (the PR4/PR5A acceptance pairings) stay
reviewable as the historical Parity/B0 route. v2 is still the only schema written; a v1 record carrying
selected-product evidence refuses. Phase 0 current state (Selected product = B2-style, no B1 yet, B3 =
exchanger Setpoint not BypassFactor, displacement normalised in SAM_Systems #23, plant-room read-back
still a gate) is noted in the plan companion and the evidence doc. The automated review of the amended
head found three defects in this slice, fixed: resolution now covers only units a retained ventilation
system names (SAM #114 scope), one air system bound by two units refuses before simulation, and an
evidence row is complete only with its lookup airflow and both bases. Amendment validation: full
`SAM.Analytical.UI.WPF.Tests` **1012/1012** (+10), `SAM_UI.sln` Release 0 errors, `git diff --check` clean.

### Files changed

- Part O record/equipment schema, behaviour enum and pipeline stage in `SAM_UI/SAM.Analytical.UI`.
- Iteration 3 command, orchestration, pipeline, resolver/binding queries, review gate and report text in
  `WPF/SAM.Analytical.UI.WPF`.
- Iteration 3 resolver, run, record, review, ledger and presentation tests.
- `documentation/plans/PR5-MANUFACTURER-AWARE-PLAN.md` and
  `documentation/evidence/PR5A-SAMUI-FAIL-CLOSED.md`.

### Validation

- Focused equipment resolver 15/15; record 8/8; review 24/24; presentation 8/8.
- Full `SAM.Analytical.UI.WPF.Tests`: **1002/1002 passed**.
- Visual Studio MSBuild `SAM_UI.sln` Release restore/build: **0 errors** (existing dependency/nullability/
  architecture warnings remain).
- `git diff --check`: clean; new C# files carry SPDX/copyright headers.

### Evidence boundary, risks and exact next step

Certified E1/E2 curves for Nuaire `MRXBOXAB-ECO5-AECV` (`MR-ECO-COOL-V`) remain unsourced. No product
figures were invented, and no real selected-product annual run was attempted. SAM_Tas proved the merged
`ExchCalcType` and fan-HGF policies on licensed native objects, but the complete frozen operating-point
A-D matrix and annual B1/B2 exit gate remain blocked on E1/E2; B3 supply-limit semantics remain
uninterpreted. #113/#115 remain independent. PR5B has not started.

Exact next step: commit/push this SAM_UI slice, open/review/merge its PR and update SAM #111. Then obtain
traceable certified E1/E2 data, review/transcribe it into catalogue v2, and run the frozen licensed
operating-point plus annual B0/B1/B2 acceptance. Do not start PR5B.

## LATEST - PART O ITERATION 3 (A/B) FOUNDATION - PR4 (2026-09-11)

### Current status

Branch `feature/part-o-iteration3-orchestration`, open against `sow/2026-Q3`. SAM-BIM/SAM#111 PR4:
`SAM_UI` orchestrates the Iteration 3 A/B pairing and presents and persists the evidence. **SAM,
SAM_Systems and SAM_Tas carry no production change** and were verified clean at `413215cc`,
`89cf1399` and `1d62f380` (which includes #52, the yearly-profile alignment fix; SAM_Tas was rebuilt
from that source before SAM_UI, so no stale HintPath DLL could reintroduce the one-hour shift).

Validation: Release build 0 errors; `SAM.Analytical.UI.WPF.Tests` **949/949** (810 baseline unchanged
+ 139 new). Licensed TAS acceptance run on the canonical fixture - see
`Documentation/evidence/PR4-ITERATION3-FOUNDATION.md` and `PR4-comparison.tsv`.

### What it is

SAM_UI sequences four existing authorities and holds none of its own. SAM keeps the analytical design
and **TM59**; SAM_Systems the materialisation; SAM_Tas the no-IZAM source, the TPD conversion, the
Systems simulation and the `IResultantTemperatureProvider`. SAM_UI decides sequencing, scope, identity
and presentation, and computes **descriptive** A/B statistics with no parity threshold anywhere.

Fourteen ordered stages. The ledger - not the caller - enforces the pipeline rule: a refusal fixes the
stage and rejects every later completion, so a refused pairing has no comparison object at all and the
window shows no Candidate B number anywhere.

### SAM #114, resolved by identity in the caller

`PartORun` captures `PartOIterationPreparation.VentilationSystems`' identities at the moment the run
adopts the preparation - the only moment the answer is known - and clears them on reset, invalidate and
restore. The scope keeps exactly those, drops an authored system carrying no effective mechanical duty
from a **copy** of the cluster with an evidence note, and refuses one that does. The design is never
rewritten and no NV, UV, opening or infiltration leaves the thermal model.

The comparability gate PR4 adds is both halves: the scope refuses an unrelated system with duty on
**any** thermally participating room (the no-IZAM sweep is model-wide, and an unassessed room is
thermally coupled to the assessed ones), and the reconciliation refuses a retained system's duty
Candidate B did not reinstate. On the canonical fixture the three MVHR systems are retained and `NV`,
`UV` and `MV` are each excluded with the recorded reason that they carry no design ventilation terminal.

### Licensed acceptance - what passed

Canonical fixture SHA-256 verified. Three physical MVHR air systems, eight served rooms, `Corridor_1`
not bound, fourteen directed legs (3 supply / 6 extract / 5 transfer), **maximum airflow delta 0 l/s**
against the prepared design's own terminals, 8 x 8760 finite `ZoneTemperature`, 8 x 8760 finite
`ResultantTemperature`, and the provider's series **identical to what TM59 read back from the same
file over 70 080 values**. Both cases through the same unchanged TM59 path: **A PASS, B PASS**, 0 of 8
criterion outcomes differ. Pooled bias +0.373 K, RMSE 0.834 K, max 2.909 K.

The planned refusal (a locked bridge file) refused at exactly `ResultantTemperature`, left every later
stage `NOT RUN`, presented no Candidate B number, reported **only** the files that attempt wrote - the
previous attempt's 16 MB bridge TSD sitting at the same fixed path was correctly not claimed - and had
already deleted the previous Candidate B `.sam`. Rerunning after removing the lock reproduced the
statistics bit-identically and the comparison TSV byte-identically.

Reopening the pairing started **no TAS process** and changed **no TAS artifact's** length or write
time; touching the bridge TSD made the review refuse **by name** with no comparison.

### The one gate that could not be demonstrated, and why it is not this PR's

`SimulationResultProvenance.Fingerprint(AnalyticalModel)` is **not stable across a `.sam` save and
reload** for a model the TAS workflow returned, so `PartORun.Restore` refuses a persisted Part O run on
this fixture. Isolated three ways: the raw fixture round-trips stably; it reproduces with no Iteration 3
anywhere near it; and **it reproduces on the untouched baseline build at `af7535db`**, which contains no
Iteration 3 code. It blocks the pre-existing "reopen a saved run and Review Results" feature
independently of this PR, and it surfaces once more on PR4's own side when the review reloads Candidate
B's model - where it refuses correctly, by name, and fail-closed.

**Follow-up, in the SAM repository and outside PR4:** make that fingerprint stable for a model carrying
TAS result series, or exclude those series from it. One change fixes both reviews.

### Closeout (2026-09-11) — SAM #116 landed, the gate now demonstrates

SAM-BIM/SAM#116 fixed both root causes above (`GroundTemperature` NaN/"not stated" round trip; SAM_UI's
own "UI Geometry Settings" excluded from the fingerprint beside `CaseDescription`), reviewed at head
`d43a4ff92f7ce2996175a5ab2939f4078e34daab` (`SAM.Tests` 2120/2120), merged into `sow/2026-Q3` at
`553957dc61db876926a5599154770d7aa22f959f`.

SAM_UI rebuilt against the merged SAM (`SAM_Systems` `89cf1399`, `SAM_Tas` `1d62f380`, both unchanged):
`SAM.Analytical.UI.WPF.Tests` **961/961**, Release build 0 errors, `SAM Analytical.deps.json` confirmed
to carry `SAM.Analytical.Tas.TPD`. The previously-undemonstrable gate was then run for real against the
rebuilt app on the existing `C:\TasOut\pr5a` pairing: Reference A reopened with no provenance refusal,
`Review It. 3 (A/B)` completed (A Pass, B Pass, 0/8 differ, bias 0.373 K, RMSE 0.834 K, max 2.909 K), and
repeated identically after killing and relaunching the process from the same `.sam`. Both runs were
polled continuously for TAS-family processes — only `TSD` readers ever appeared — and the
comparison-defining artifacts' file timestamps were unchanged by either Review while both TM59 `.txt`
reports were freshly rewritten each time, exactly as designed. Full detail:
`Documentation/evidence/PR4-ITERATION3-FOUNDATION.md` §5.6.1.

### Programme state

```text
PR1 MERGED   PR2 MERGED   PR3 MERGED   #52 MERGED   #116 MERGED   PR4 this branch, ready to merge
```

SAM #111 stays **open** for PR5 manufacturer-aware behaviour. SAM #114's production resolution is
implemented and demonstrated on the canonical fixture above; close it once this PR merges.

---

## PART O UI CONSISTENCY POLISH, PRESENTATION ONLY (2026-09-08)

### Current status

**MERGED into `sow/2026-Q3`.** PR #99, ten commits, reviewed head
`d5c39cc4d979ccba641f7631cb27be4dd31e3c1c`, merge commit
`1f2beb55868cd54180f401893a46d3d6d6a6c199` (2026-09-08). Working tree clean. `SAM`, `SAM_Systems` and
`SAM_Tas` are **untouched** and clean, at the frozen SHAs above - this pass needed no change in any of
them.

```text
15c47de  the presentation pass - the five approved areas
a37c862  the 1a / 1b catalogue readable again, and the route stated once
2768145  "Show every line" hidden where it has nothing to do
bf0c06b  the simulate dialog says which route it is, and why its project name is locked
e24ae94  the stale ProjectName_Isolated note corrected
7602573  this checkpoint, and the Part F tooltip no longer judging the space
7c39ccf  the assignment row fits the window it opens in
e0ef654  the run heading no longer claims a run that has not happened
12cceb9  the manual simulate title claims only the conversion
(this)   Copy All renders a missing value as the grid does
```

CI was green on the merged head `d5c39cc` (`build` SUCCESS, `spdx` SUCCESS), and on every head
checked before it. **Seven** Codex review findings were raised and all seven were resolved and answered
on the PR before merging: the compact 1a / 1b catalogue showing only a count, the Part F tooltip inferring a space type, this
file not being updated, the assignment columns not fitting the window, the run heading claiming an
existing run before anything had run, the manual simulate title promising a simulation that route can
skip, and Copy All pasting `NaN` where the grid showed an em dash.

**Two failure modes account for six of the seven, and both are worth carrying forward.**

*Wording that claimed more than the code behind it knew* - four of them: a group heading, a cell
tooltip, a permanently disabled tick and a window title. Renaming something is cheap and re-reading
what backs the name is not. **Check every new string against the condition that produces it.**

*A presentation rule applied to one surface and not its twin* - two of them: the catalogue grid styled
while its 1a / 1b reference view showed only a count, and the cells routed through the airflow converter
while Copy All still formatted the raw doubles. **Whenever a table changes, ask what else renders the
same rows** - the export, the clipboard, a second read-only view. `CopyAllText` is now a seam so the
export is assertable against the converter rather than by eye.

### What this is, and the line it does not cross

Presentation only. **No** airflow, equipment-selection algorithm, capacity logic, manual-assignment
authority, Iteration 2B behaviour, TAS behaviour, persistence, project-test persistence, data authority,
simulation orchestration, scenario applicability, warning generation or engineering status value is
changed. The frozen authority split is restated by nothing new:

```text
PartFRequiredAirFlow  !=  DesignAirFlow  !=  SelectedEquipmentCapacity  !=  OperatingAirFlow
```

This is the work the freeze entry below recorded as *"presentation-only ideas were raised (tooltips,
spacing, hierarchy, disabled sections, status wording) and are explicitly not blockers"*.

### Work completed

1. **Iteration 1a / 1b equipment section is a compact summary**, not a screenful of greyed controls. Two
   concept lines in the same place as Iteration 2's section, plus a **collapsed** disclosure holding a
   read-only reference table over the SAME `PartOCatalogueProductRow` instances the Iteration 2 grid
   uses - Origin, Manufacturer, Model, Variant, Max SUP (l/s), Max EXT (l/s). No `Use` column exists in
   it, so it cannot tick, edit a pool, assign, or change applicability. Iteration 2 is untouched.
2. **The Prepare & Run status list is divided into two presentation groups** - `Current configuration`
   and `Existing run / results` - so `Ventilation design: NEEDS PREPARATION` beside `Results: READY`
   reads as the legitimate mix it is. Rows are one line, with the inspection's complete sentence on the
   tooltip and behind a per-row disclosure. The Iteration 1b Part F N/A explanation moves there in full.
3. **Diagnostics header counts each kind present** and carries `Copy All`, moved off the OK/Cancel row.
   Byte-identical **warnings** collapse with a `x N`; refusals and notes never do.
4. **One spelling per concept** - the four scenario names as shared constants, `Max SUP / Max EXT`,
   `Design SUP / Design EXT`, `Part F required`, unit-in-header, `TAS`, `Project test`, and
   `test (project test)` for the project test product.
5. **Table hygiene** - right-aligned numerics, a separator rule at each of the three paired concepts,
   a live `3 of 412 dwellings selected`, an inline bulk-assignment confirmation, and a missing figure
   shown as an em dash instead of `NaN`.
6. **High-value tooltips** on the engineering distinctions, and `Convert to Tas TBD` becomes
   `Convert to TAS and simulate`.

### Important decisions and assumptions

- **A two-tier grouped DataGrid header was deferred.** It needs a custom column-header component, which
  is disproportionate for a presentation pass. The three paired concepts are marked instead by a
  `CellStyle` + `HeaderStyle` left border on each group's first column, asserted to add only
  `BorderThickness` and `BorderBrush` - an explicit `CellStyle` replaces the implicit style, and one that
  also set `Template` would blank every cell in three columns.
- **No stale-result detection was added, and no new status token exists.** `PartOWorkflowStatusGroup` is
  a heading over rows: it hashes nothing, reads and writes no timestamp, compares nothing between the
  groups. Every row still carries exactly the `PartOWorkflowStageStatus` the inspection assigned it.
- **The run heading says "Existing run / results" only where results exist**, and the neutral
  "Run / results" otherwise. Unconditionally it asserted an existing run directly above three rows
  reading `Model check PENDING`, `Simulation NOT RUN`, `Results NOT RUN` - the very ambiguity the split
  exists to remove. The word is chosen from `PartOWorkflowCapabilities.ResultsAvailable`, a fact the
  application already publishes; nothing is compared or stored and no row's status moves.
  `TheRunHeading_ClaimsAnExistingRunOnlyWhenThereIsOne` asserts that only the heading differs between
  the two cases.
- **A shortened status row is a PREFIX of the inspection's own sentence** (cut at the first full stop),
  so a row can only say less than the inspection said, never something different. The single supplied
  summary - the Iteration 1b Part F line - is chosen from the scenario's ventilation mode, never by
  parsing the detail text.
- **Warning grouping is warnings-only and byte-exact.** Refusals and notes are never collapsed, because
  a person counting refusals on screen has to be counting refusals. Nothing is stripped to manufacture a
  match: collapsing on less than the complete raw string would mean deciding one warning stands for
  another.
- **`Show every line` is HIDDEN where nothing was collapsed, not greyed** - and on today's Part O
  warnings that is every run. Every warning this window can receive names its space
  (`Modify.AddPartOBaseMVHRSystem` names the space and the system;
  `Query.ReconcileVentilationSystemDesignDuty` names the space, the direction and both airflows), so no
  two are ever byte-identical. A permanently disabled tick advertises a capability that never arrives -
  which is exactly what native acceptance reported.
- **The Part O simulate dialog's project name stays LOCKED, and that is deliberate.** A user asked to
  edit it. `Simulate - Energy Simulation` already leaves it editable; only `Modify.Simulate`'s Part O
  path calls `SimulateControl.LockPartOSettings`. There the name is the run's identity: every artifact
  is named from it, an isolated run carries `Query.ProjectName_Isolated`'s scope token in it, and
  `PartOSimulationContext.Iteration_ProjectName` **parses the Iteration 2B round back out of it** - so
  retyping `Flat1-Opt07` as `Flat1` restarts numbering at `-Opt01` and overwrites the earlier
  optimisation's evidence, with nothing refusing it. **Do not unlock it.** The real defect was that both
  routes opened the same window with the same title, so the lock read as a fault; fixed by retitling the
  Part O one `Part O - Convert to TAS and simulate` and putting the three reasons on the box's tooltip
  (which needs `ToolTipService.ShowOnDisabled`, or WPF never shows a tooltip on a disabled control). A
  different name is set where the name comes from: the model, through **Edit - Properties**, before the
  iteration is prepared.
- **The Part F cell tooltip reports an absent RECORD, not a judgement about the space.** It reads "No
  continuous Part F requirement is recorded for this space." The approved package specified "No Part F
  requirement for this space type", which says more than the code knows - nothing here examines a space
  type - and would make a habitable room that `AddVent PartF` was never run over read as a deliberate
  exemption. Deliberate deviation from the brief, on the same principle as the Dwelling / Zone column's
  em dash.
- **The assignment table has ONE flexible column and a default width that fits the row.** Moving the
  unit into each heading widened four columns, and the fixed total went from 1145px to 1205px inside a
  1140px window - so the review opened horizontally scrolled with `Status`, the column that says whether
  a dwelling's product will do, off the right edge. `Assigned product` is now star-sized with a 250px
  minimum, every other column stays sized for its own header, and the window opens at 1260px.
  `TheAssignmentRow_FitsTheDefaultWindowWidth` asserts the arithmetic - fixed total plus the star
  minimum plus an allowance for window chrome, the grid margins and a vertical scrollbar - so widening a
  column past the default again fails a test rather than reaching a user. Asserted arithmetically
  because a `DataGrid` will not lay a row out offscreen.
- **The airflow formatting has one implementation, and the export calls it.**
  `PartOAirFlowConverter.Text` is the whole of it; the cells reach it through the converter and
  `CopyAllText` calls it directly. Formatted independently the two disagreed at once - an em dash on
  screen, `NaN` in the pasted report, up to four times per Iteration 1a / 1b equipment row. Copy All was
  also split out of its click handler so the text is assertable without a clipboard the test host may
  not own.
- **The two simulate titles claim only what their own route does.** `Convert to TAS` on the ordinary
  route, because `Modify.Simulate` supports an unticked Simulate box alongside SAP or the
  domestic-overheating XML - it returns early only when all three are off - so a conversion and export
  with no simulation is a supported outcome. `Part O - Convert to TAS and simulate` on the guided route,
  where the claim is a fact: `Create.SimulateOptions_PartO` sets `Simulate = true` and
  `LockPartOSettings` disables the box. The approved package offered both spellings and asked for the
  accurate one; the first attempt took the wrong one.
- **`Query.PartOVentilationRouteText` states the route once** by reading the canonical word back through
  `Analytical.Query.PartOVentilationMode`, printing one word where the two agree - which
  `Modify.PreparePartOIteration` guarantees by refusing a disagreeing pairing - and both where they do
  not, because a header that hid that disagreement would be the worse failure.

### Files changed

New in `WPF/SAM.Analytical.UI.WPF`:

```text
Classes/PartO/PartOWorkflowStatusGroup.cs      the two status headings
Classes/PartO/PartODiagnosticSummary.cs        counts, warning-only byte-exact grouping, complete record
Query/PartOProductLabel.cs                     "test (project test)" instead of "Project test test"
Query/PartOVentilationRouteText.cs             the route stated once
Converters/PartOAirFlowConverter.cs            NaN -> em dash, display only
Converters/PartOProductLabelConverter.cs       the label, for a picker ItemTemplate
```

Modified: the equipment-selection control (XAML + code-behind), the Prepare & Run, Prepare Iteration,
Review, TM59 and 2B windows, `SimulateWindow` and `SimulateControl`, `Modify/PreparePartOIteration.cs`
(the header's one `string.Format`), `Query/ProjectName_Isolated.cs` (comment only), the four row classes,
and `PartOWorkflowScenario` / `PartOVentilationStrategyOption` in `SAM_UI/SAM.Analytical.UI`.

Also `PROJECT_PROGRESS.md`, which `AGENTS.md` requires and which this pass initially failed to update.

Tests: `PartOConsistencyPolishTests.cs` (new, 37 tests), plus `PartOPresentationTests.cs`,
`PartOProjectTestProductTests.cs` and `PartOSimulateDefaultsTests.cs` updated/extended.

### Validation performed

```text
SAM_UI.sln Release                               0 errors
SAM.Analytical.UI.WPF.Tests                       810 / 810   (frozen baseline 771 / 771)
working tree (SAM_UI)                            clean
working trees (SAM, SAM_Systems, SAM_Tas)        clean, untouched
```

The frozen baseline was **re-measured on this machine** before the work started: 771 passed, 0 failed.

Two existing tests were updated for deliberate changes: `Origin` is now `Project test` rather than
`PROJECT TEST`, and both product pickers render the label through an `ItemTemplate` converter instead of
`DisplayMemberPath`.

### Unresolved issues, risks and blockers

- **Not exercised in the real application.** Every assertion is over presentation seams and view models;
  the windows are constructed in tests but never shown. Worth opening the Part O family by hand once -
  Prepare Iteration on 1a and on 2, Review iteration, Prepare & Run, and the Part O simulate dialog -
  before release. Native acceptance screenshots were accepted for the earlier commits.
- **The `DataGridCell` group-separator style is asserted structurally, not by rendering.** A `DataGrid`
  will not realise a row offscreen without a `PresentationSource`, so the test asserts the style adds
  only the two border setters rather than laying a cell out. That is the invariant that matters, but it
  is not a rendered check.
- **Deferred by instruction and not started:** search/filter, paging/virtualisation architecture, result
  persistence, run history, last-run timestamps, stale-result detection, warning semantic classification,
  severity redesign, new engineering statuses, new Part O workflow behaviour, Iteration 3, and a
  wholesale redesign of the review-window header/fact grid.
- **A Part O-aware rename does not exist.** If a different run name is wanted without going through
  Edit - Properties, the safe shape is editing the name *stem* with the scope token and `-OptNN` suffix
  preserved and re-derived. That is an engineering change to run identity and persistence, so it belongs
  in its own PR against the unfrozen programme - not here.

### Next step

1. **Open the Part O family by hand once, before release.** This is the one outstanding item and the
   first risk above is the reason: every assertion in this pass is over presentation seams and view
   models, and the windows are constructed in tests but never shown. The column-width defect - the
   assignment table opening horizontally scrolled with `Status` off the right edge - was invisible to all
   810 tests and to CI, and was found by review reading the numbers rather than by anything running. Worth
   one pass each through Prepare Iteration (1a and 2), Review iteration, Prepare & Run, and both simulate
   dialogs (`Convert to TAS` and `Part O - Convert to TAS and simulate`).
2. Nothing else is pending. The working tree is clean and no further Part O UI work is scheduled;
   everything on the deferred list above remains deliberately out of scope.

## PART O ITERATIONS 1a / 1b / 2 / 2B - FROZEN (2026-09-08)

Native acceptance PASSED in SAM_UI, and the equipment-selection closeout is merged into `sow/2026-Q3` in
dependency order - `SAM` first, because `SAM_UI` consumes `..\..\..\SAM\build\SAM.Analytical.dll`.

| Repository | PR | Reviewed head | Merge commit | Frozen integration SHA |
| --- | --- | --- | --- | --- |
| `SAM` | #110 | `0f6dbaf7f9d5c490203e62ebc507ba6a8370a900` | `85d7b70d9d33cb0349be77f27a1c53b8679d09c5` | `85d7b70d9d33cb0349be77f27a1c53b8679d09c5` |
| `SAM_UI` | #98 | `f5c6ef49631a56decf3cc08ff02b5ecdeb84564f` | `68e314a5f62dd3cd78fd832b92b1911fb4fbeaf2` | `68e314a5f62dd3cd78fd832b92b1911fb4fbeaf2` |
| `SAM_Systems` | - | untouched | - | `9e1cd06f6031852a3c9395be476b01d8c44a1a4c` |
| `SAM_Tas` | - | untouched | - | `ec7f50543e123f8a734b6e27b2b16c0cf1f1edde` |

This documentation commit is a descendant of the frozen `SAM_UI` SHA and changes no code. `SAM_Systems`
and `SAM_Tas` carry **no production change**: both are at the same SHA they were at when the work
started, with clean trees.

Post-merge verification on those exact heads, `SAM_UI` rebuilt and retested against the **merged** `SAM`
before its own merge as well as after:

```text
SAM.sln Release                                  0 errors
SAM_UI.sln Release                               0 errors
SAM.Tests                                        2114 / 2114
SAM.Analytical.UI.WPF.Tests                       771 /  771
SAM.Analytical.Systems.Tests                       90 /   90
git diff --check (all four repositories)         clean
working trees (all four repositories)            clean
```

### Native acceptance (recorded 2026-09-08)

Confirmed in SAM_UI, with no TAS rerun: Iteration 1a correct with no manufacturer equipment selection;
Iteration 1b correct with no mechanical equipment selection; Iteration 2 *Automatic - selected pool*
works; Prepare & Run exposes and respects the same configuration; catalogue visibility works with MRXBOX
and XBC15 at their correct capacities; the project test product is visible, usable and its capacity
editable; *Manual per dwelling* and *Convert to Manual* work; multi-row bulk selection and assignment
works; the Dwelling / Zone column works including Flat and Corridor attribution; the engineer-facing
picker no longer exposes Rank; design supply/extract remains separate from equipment maximum capacity;
and **no unexpected airflow mutation was observed**.

Presentation-only ideas were raised (tooltips, spacing, hierarchy, disabled sections, status wording) and
are explicitly **not** blockers. They were deliberately not implemented here.

### The authority split this freeze protects

```text
PartFRequiredAirFlow  !=  DesignAirFlow  !=  SelectedEquipmentCapacity  !=  OperatingAirFlow
```

Confirmed on the merged trees, by reading them rather than by adding anything: Iteration 1a is still the
base mechanical MVHR design; Iteration 1b invents no mechanical topology or continuous mechanical
airflow; Iteration 2 selects and assigns without moving airflow; *Automatic - all* works and means the
shipped manufacturer catalogue; *Automatic - selected pool* has no fallback; *Manual per dwelling* stays
authoritative, holding and reporting an insufficient assignment rather than accommodating it; the project
test product is project-scoped and never joins *Automatic - all*; selected identity stays on
`AirHandlingUnitParameter.VentilationUnitReference`; Iteration 2B locks that identity and treats capacity
as a ceiling only; there is no silent MRXBOX / XBC15 / test-product reselection - every dwelling round
reports `Kept`; and saved, reopened and restored result workflows trigger no reselection.

**Iteration 1a FROZEN. Iteration 1b FROZEN. Iteration 2 FROZEN. Iteration 2B FROZEN.**

A presentation-only UI consistency review follows separately, covering Iteration 1a, Iteration 1b,
Iteration 2 Automatic, Iteration 2 selected pool, the Manual workflow, Prepare & Run and the
preparation/review windows. It must not reopen the engineering authority closed here.
**Iteration 3 has not been started.**

## Latest (2026-09-08): bulk assignment, the project test product, a dwelling column, and no more rank

**Status: implemented, tested, natively accepted, merged, frozen.**
PR #98, branch `feature/parto-equipment-selection-ux`, off `sow/2026-Q3` at **`ad813c9`**.

The four usability findings from native testing, before freeze.

### Bulk manual equipment assignment

A project of a hundred or a thousand flats cannot be authored one row at a time. The assignment grid now
takes `SelectionMode="Extended"` - so click / Ctrl+click / Shift+click / Ctrl+A are native DataGrid
behaviour and needed no code - and gained an explicit **Apply to selected** beside a live count of what it
will touch.

**Deliberately a named action rather than "editing a cell edits every highlighted row."** That
alternative is triggered by an ordinary mis-click and leaves nothing behind to notice. Single-row editing
through the Assigned product cell is unchanged and still changes that row alone.

Every selected dwelling is re-evaluated against its **own** duty, so one product applied to twelve flats
can be sufficient for eleven and reported INSUFFICIENT for the twelfth - where it stays assigned, that
dwelling's design airflow is not reduced, and nothing larger is substituted. A suggestion remains a value,
never a mutation. Still staged: `Commit` is the one write and Cancel discards by never reaching it.

`PartOEquipmentAssignmentSet` gained a `Dictionary<Guid, PartOEquipmentAssignment>` and a `HashSet` of
allowed identity keys, so finding a row, resolving its capability and asking whether a product is
permitted are each one probe. Assigning *k* rows is **O(k)** and a whole-table refresh is O(D + P) rather
than the O(D x P) the class comment already claimed. A committed cell edit now refreshes that row instead
of the table.

### The project test product, in the shared control

Added to `PartOEquipmentSelectionControl`, so Prepare Iteration and Prepare & Run state the same one -
as they already do the mode and the pool. Framed and marked **PROJECT TEST** in a new Origin column,
because everything else in that grid is a transcription of somebody's published document and this is not.

**It cannot leave a stale identity behind, by construction.** The permitted pool is *derived* from the
catalogue rows and the test product is one of those rows: renaming it replaces the row so the pool holds
the new identity and not the old one in the same step, with the engineer's tick carried across
explicitly; disabling it removes the row and its permission together. A project reopened with a pool
naming a project-test identity nothing states any more ticks nothing and drops it. A pool held as its own
list, kept in step by hand, is what would eventually have left an invisible permission for a product that
no longer exists.

While dwellings hold it, **Enable and the Name are locked** - the name *is* the identity those dwellings
carry - and the section says how many and what to do. Its **capacities stay editable**, because re-rating
a what-if is the whole point of it: the identity never moves, no dwelling is reassigned, and the ceiling
simply re-resolves.

### Dwelling / Zone on the space table

Resolved once from the model's own zone-space relations, and never from a space's name, its prefix, an
index or the row order - each of which looks right on a demonstration model and is wrong on a real one.

Part O dwelling membership has **absolute precedence**. The fallback is gated **twice**: the zone must be
in the same `ZoneParameter.ZoneCategory` as the dwellings in scope **and** be a
`Query.PartOClassifyAssessmentZones` common-space zone of it. So a communal corridor reads `Corridor`,
while a fire, thermal, system-grouping or reporting zone fails both gates and cannot reach the column at
all - "Fire compartment 3" under a heading saying "Dwelling / Zone" would be a confident wrong answer,
which is worse than no answer because it reads as a statement about the model. Absence **and**
multi-membership ambiguity both render an em dash; nothing is invented and nothing is concatenated.

One `GetZones` plus one indexed relation lookup per zone, then O(1) per row. The map now feeds the
equipment table too, replacing a second build of the same thing. The name is repeated on every row rather
than merged or blanked, because this is a table engineers filter, sort and paste into a spreadsheet.

**Known limitation, deliberate:** a space in two qualifying common-space zones of the assessment category
shows an em dash. Both names would be defensible and neither is authoritative.

### Rank is out of the picker

Both the per-dwelling picker and the new bulk selector bind
`VentilationUnitCapacityDescriptor.Label` - identity and both maximum airflows, no rank. Asserted by
loading the real column template and reading the ComboBox's `DisplayMemberPath`, so the binding cannot
quietly revert to `ToString()`.

### Reuse also compares the project test product

`PartOWorkflowInspection.Reusable` gained a test-product match beside the existing mode/pool comparison,
which is untouched. The rating is not a selection input - it is a capability - but it **is** the ceiling
Iteration 2B stops at for any dwelling assigned to the product, so reusing a preparation made at 165 l/s
for a request that now says 175 would optimise against the old ceiling while the dialog reported the new
one.

### Files changed
- `SAM_UI/SAM.Analytical.UI/Classes/PartO/PartOEquipmentAssignmentSet.cs` (guid index, allowed-key set, bulk `Assign`, `AllowedCandidates`)
- `SAM_UI/SAM.Analytical.UI/Classes/PartO/PartOPreparationContext.cs`, `PartOWorkflowRequest.cs`, `PartOWorkflowInspection.cs`
- `WPF/SAM.Analytical.UI.WPF/Controls/PartOEquipmentSelectionControl.xaml{,.cs}`
- `WPF/SAM.Analytical.UI.WPF/Query/PartOProjectTestVentilationUnit.cs` (new)
- `WPF/SAM.Analytical.UI.WPF/Classes/PartO/PartOCatalogueProductRow.cs`, `PartOEquipmentRow.cs`, `PartOSpaceRow.cs`
- `WPF/SAM.Analytical.UI.WPF/Windows/PartOPreparationWindow.xaml{,.cs}`, `PartOIterationWindow.xaml.cs`, `PartOWorkflowWindow.xaml.cs`
- `WPF/SAM.Analytical.UI.WPF/Modify/PreparePartOIteration.cs`, `RunPartOWorkflow.cs`
- `WPF/SAM.Analytical.UI.WPF.Tests/PartOEquipmentBulkAssignmentTests.cs` (new, 13 tests)
- `WPF/SAM.Analytical.UI.WPF.Tests/PartOProjectTestProductTests.cs` (new, 26 tests)
- `WPF/SAM.Analytical.UI.WPF.Tests/PartOSpaceRowTests.cs` (new, 15 tests)
- `WPF/SAM.Analytical.UI.WPF.Tests/PartOPresentationTests.cs` (+3 picker-label tests)
- `SAM_UI/PROJECT_PROGRESS.md` (this file)

### Validation
- Focused Part O runs: 175/175 passed on the merged tree; reopen/lineage/warm-start/envelope 125/125.
- Full `SAM.Analytical.UI.WPF.Tests` Release: **771 passed, 0 failed** (was 714; +57 new).
- `SAM_UI.sln` Release: 0 errors. `git diff --check`: clean.

## XBC15 CLOSEOUT COMPLETE - READY FOR FINAL EQUIPMENT-SELECTION UX

Merged into `sow/2026-Q3` on 2026-09-07, in dependency order, after native Iteration 2 acceptance PASSED:

| Repository | PR | Merge commit | `sow/2026-Q3` head |
| --- | --- | --- | --- |
| `SAM_Systems` | #19 | `61cedcf52f78ef4b029a5f6ef9980759ab329b9e` | `61cedcf52f78ef4b029a5f6ef9980759ab329b9e` |
| `SAM` | #109 | `a08c9df7c1a1513a61e17618539e037c8e6bf75c` | `a08c9df7c1a1513a61e17618539e037c8e6bf75c` |
| `SAM_UI` | #97 | `ec0d0f624c066719e219bbf2e5e30e392aaf07f0` | `ec0d0f624c066719e219bbf2e5e30e392aaf07f0` |
| `SAM_Tas` | - | untouched | `ec7f50543e123f8a734b6e27b2b16c0cf1f1edde` |

Post-merge verification on those exact heads, rebuilt in dependency order `SAM` -> `SAM_Systems` -> `SAM_UI`:

```text
SAM.sln / SAM_Systems.sln / SAM_UI.sln Release   0 errors each
SAM.Tests                                        2024 / 2024
SAM.Analytical.Systems.Tests                       90 /   90
SAM.Analytical.UI.WPF.Tests                       645 /  645
git diff --check (all four repositories)         clean
```

The catalogue deploys with both products: the installed copy at
`%APPDATA%\SAM\resources\Analytical\Systems\VentilationUnit\VentilationUnitCatalogue.JSON` is
byte-identical to the merged repository copy and carries `MRXBOXAB-ECO5-AECV` (150/150 l/s) and `XBC15`
(190/190 l/s). That is also what proves the "skip where the catalogue is not installed" guard in
`PartOPresentationTests` does not trip on this machine, so the two-product assertions really ran.

**Nothing is FROZEN.** Iterations 1a / 1b / 2 / 2B are deliberately not marked frozen: the freeze gate is
the equipment-selection UX task that follows - allowed product pool, convert-to-manual, per-dwelling manual
assignment - which adds the very product-list surface the native Iteration 2 check could not exercise.

## Latest (2026-09-07): XBC15's 190 l/s as a ceiling, through the production Iteration 2B path

**Status: implemented, tested, and natively accepted.**
Branch `test/parto-xbc15-capacity-ceiling`, off `sow/2026-Q3` at **`8219416`** (the PR #96 acceptance record).

Baseline it was branched from, measured before any edit:

```text
SAM_UI.sln Release                      0 errors
SAM.Analytical.UI.WPF.Tests             640/640
```

### What this is

`SAM_Systems` now ships a second real product - **Nuaire XBOXER XBC15, 190/190 l/s** beside the MRXBOX's
150/150. This branch proves the Approved Document O closeout case **through the production optimisation
path** in this repository: the XBC15 explicitly selected on a ~150 l/s design, its 190 l/s acting only as a
ceiling, and Iteration 2B never silently reselecting.

Tests plus one documentation comment. **No behaviour changed.**

### Why the production path, and how it is reachable without TAS

`Modify.CapacityEnvelope` (`WPF/SAM.Analytical.UI.WPF/Modify/OptimisePartOTM59.cs`) is the Iteration 2B
envelope orchestration, called from `OptimisePartOTM59`. It is called here exactly as `OptimisePartOTM59`
calls it, through the existing `InternalsVisibleTo` seam - not the `SAM.Analytical` primitive underneath it.

Everything the closeout asserts is settled by the orchestrator **before** it reaches `RunPartOSimulation`,
which is the only step needing a licensed TAS: the eligibility guards, the target vector,
`EvaluateDesignAirFlowCapacityEnvelope`, the step construction, the targeted and derived adjustments, the
notes and the user-facing description. The fixture states no ventilation route, so the run records the whole
envelope and then stops with its own refusal at the re-preparation stage and **no simulation is attempted** -
the same seam every existing test in `PartOCapacityEnvelopeTests` uses.

The production path also has a second, independent reason the plant cannot be swapped, and the tests assert
it: the re-preparation over the envelope design is handed a **null catalogue** on purpose
(`OptimisePartOTM59.cs`, the `PreparePartOIteration` call inside `CapacityEnvelope`), so preparation has
nothing to select from either.

### `PartOCapacityEnvelopeTests` - four tests

A new `FailingOnTheRealLadder` fixture: a run stopped on capacity with one failing room, over a dwelling
designed at 150/150 l/s, whose unit is selected as the **XBC15** - with the MRXBOX offered **first** in the
list, so anything taking the head of the list or re-running the selection rule would visibly swap the plant.

- `XBC15sRating_IsTheCeilingTheProductionEnvelopeGrowsWithin` - scale `190/150`; duty 150 -> **190/190 with
  zero headroom, never past it**; the 40 l/s spent as design airflow; the grown design read room by room off
  the model the orchestrator built; and the sentence the user reads contains `190/190 l/s of 190/190 l/s`
  and `DIAGNOSTIC ONLY`.
- `TheProductionEnvelope_KeepsXBC15Selected_AndNeverReselects` - the premise asserted first (the automatic
  rule *would* choose the MRXBOX at 150/150), then the group's product is the XBC15 by identity, the
  selection is stable on the last valid model **and** on the envelope's model, every dwelling round reports
  `Kept`, and the description says `No product was reselected`.
- `TheProductionEnvelopeOverXBC15_MovesOnlyDesignAirflow` - the authority separation:
  `PartFRequiredAirFlow != DesignAirFlow != SelectedEquipmentCapacity != OperatingAirFlow`. Requirements
  bit-identical (13 l/s stays 13 l/s beside a 190 l/s design), no air movement created/removed/re-rated, the
  accepted design untouched by value and by reference, and the envelope not counted as a round.
- `ADesignAlreadyAtXBC15sRating_ReportsNoUsefulHeadroomAndSimulatesNothing` - at 190/190 the `NoHeadroom`
  outcome fires with its explicit reason, no step is appended, **no simulation is spent**, the XBC15 is still
  selected and the 500 l/s product on offer was never reached for. No capacity rule was relaxed.

### `PartOPresentationTests` - one test, and it is the one the freeze rests on

`TheShippedCatalogue_OffersTwoProducts_AndStillSelectsTheMRXBOXAtTheProjectsDuties`. Selection is
smallest-capable by `Size_Lps = supply + extract`, and the XBC15 is size 380 against the MRXBOX's 300, so at
every duty the MRXBOX can also carry the answer is unchanged. Asserted on the **installed** catalogue: two
selectable products, nothing unselectable, the description reading `2 selectable ventilation unit product(s)
available.`, and at 30/30, 63/63 **and 150/150** l/s the MRXBOX still selected with the XBC15 offered and not
taken - then the XBC15 selected at 160/160, the band it was added for.

That is what makes the existing licensed Iteration 2 and 2B acceptance still **valid** rather than merely
still plausible. It carries the same "skip where the catalogue is not installed" guard as
`TheShippedCatalogue_OffersTheNuaireProductAt150Maximum`; verified on this machine that the guard does not
trip and the assertions really run.

### Documentation

`SAM_UI/WPF/SAM.Analytical.UI.WPF/Classes/PartO/VentilationUnitCatalogue.cs` - the class comment said "The
one product this repository ships states 150 l/s". Reworded for two. No behaviour change; the class still
reads and reports and decides nothing.

### Validation

| Suite / build | Result |
| --- | --- |
| `PartOCapacityEnvelopeTests` (focused) | **33 / 33** (was 29) |
| `SAM.Analytical.UI.WPF.Tests` (full) | **645 / 645** (was 640) |
| `dotnet build SAM_UI.sln -c Release` | **0 errors** |
| `git diff --check` | clean |

Sibling repositories: `SAM_Systems` `feature/parto-catalogue-xbc15` (the catalogue entry - the substantive
change), `SAM` `test/parto-real-selection-ladder` (the same closeout at library level, **2024/2024**),
`SAM_Tas` `ec7f505` unchanged. Neither companion branch is a build dependency of this one.

### Native acceptance

- **Iterations 1a / 1b** - nothing owed. Neither reads the catalogue.
- **Iteration 2** - **PASSED** on 2026-09-07, observed in SAM_UI on the combined PR heads, with no TAS
  rerun. All three dwellings still automatically select `Nuaire MRXBOXAB-ECO5-AECV`; design duties remain
  30/30, 63/63 and 63/63 l/s; selected equipment capacity remains 150/150 l/s; headroom remains 120/120,
  87/87 and 87/87 l/s; Approved Document F and Design airflow are still shown as separate quantities and
  remain consistent with the previously accepted model; and no equipment-selection operation changed a
  design duty.

  The third assertion originally written here - that the Iteration window *reports two selectable
  products* - is **NOT APPLICABLE to the current interface**. This repository's SAM_UI does not expose the
  catalogue product list anywhere: `PartOIterationWindow` offers only the `SelectVentilationUnit` on/off
  checkbox, so there is no surface on which a product count could be read. That the shipped catalogue holds
  two selectable products is pinned in CI instead, by
  `PartOPresentationTests.TheShippedCatalogue_OffersTwoProducts_AndStillSelectsTheMRXBOXAtTheProjectsDuties`
  and by `SAM_Systems`' `TheShippedCatalogue_HoldsBothNuaireProducts`. The missing surface is an explicit
  **usability gap carried into the next task** (see the closing section of this entry), not a defect of this
  closeout and not a blocker for it. The 2026-09-01 licensed
  evidence (three dwellings at 30/30, 63/63 and 63/63 l/s; 105,120 hourly TAS values bit-identical with and
  without a product selected) continues to apply, because the XBC15 is larger than the MRXBOX on both sides
  and is therefore never the smallest capable unit at those duties - which the new
  `PartOPresentationTests` test pins in CI.
- **Iteration 2B** - the existing native 2B workflow acceptance **stands unchanged**: the orchestration is
  untouched and the recorded runs remain reproducible from the staged fixture. Re-running it would spend a
  full-year simulation to re-observe behaviour nothing in this change touches.
- **Iteration 2B with XBC15 explicitly selected** - covered through the production optimisation path above
  rather than by native observation, because this repository has no explicit product picker (see below). The
  simulation step is the only part not exercised, and it is the part this change cannot affect: every fact
  under test is settled before `RunPartOSimulation` is called.
- **Saved TM59 / results reopen behaviour** - unchanged and already covered.

### Status

**XBC15 CLOSEOUT COMPLETE - READY FOR FINAL EQUIPMENT-SELECTION UX.**

All automated acceptance is complete, every solution builds clean, and the native Iteration 2 confirmation
above has PASSED. Nothing further is owed by this closeout.

**Nothing is FROZEN.** Iterations 1a / 1b / 2 / 2B are deliberately **not** marked frozen here. The freeze
gate is the equipment-selection UX task that follows this one - allowed product pool, convert-to-manual, and
per-dwelling manual assignment - and the product picker that task adds is precisely the interface the native
Iteration 2 check above could not exercise. Freezing before that surface exists would freeze a workflow
nobody can yet drive.

### Recorded as a future usability enhancement, not an open gate

This repository has **no explicit ventilation-unit product picker**. `PartOIterationWindow` offers only the
`SelectVentilationUnit` on/off checkbox, and selection is automatic smallest-capable, so a specific catalogue
product cannot be chosen on a real model from the interface at all. That is why the XBC15 Iteration 2B case
is proved through the production optimisation path in test rather than natively.

The agreed shape of the enhancement, deliberately **not** built here: a user-defined **allowed pool**;
Automatic assigns the smallest capable product from that pool to every dwelling; **Convert to Manual** freezes
those assignments as the initial per-dwelling selections, after which individual dwellings may be overridden;
and Manual assignments remain **authoritative during Iteration 2B** - validate capacity and suggest
alternatives when insufficient, but **never silently reselect**. That last clause is the invariant
`Modify.CapacityEnvelope` already honours for automatic selections, and the four tests above are what would
keep it honest for manual ones.

### Next step

- The native Iteration 2 visual confirmation, then mark Iterations 1a / 1b / 2 / 2B **READY TO FREEZE**.
- Human review of the three PRs, then merge. **Do not merge automatically.** Record `FROZEN` and the SHAs
  after the merges.
- Not Iteration 3. The equipment-selection UX above is its own task.

## Superseded (2026-09-07): Part F / Design floor-plan overlay readability - merged as PR #96

**Status: root-caused, implemented, tested, native-accepted, and merged (PR #96,
`77b45283e5d2e19f0f382eb3b1294f0c20ea6c69`).**

### Native acceptance

PASSED. Verified visually with both overlays enabled:

```text
Studio 1_0
F EX 22.0 l/s ?
F SUP 30.0 l/s ✓
D SUP 150.0 l/s
D EXT 82.5 l/s

Bathroom 2
F EX 8.0 l/s ?
D EXT 67.5 l/s

Transfer
F TRA 8.0 l/s ?
D TRA 67.5 l/s ?
```

Part F is visually grouped above the space tag and Design below it. `F`/`D` authority prefixes are clear.
Existing unresolved-transfer `? / No modelled transfer opening identified` behaviour is preserved.

Nothing here changes Part F or Ventilation Design engineering. It changes how the two overlays' tags are
laid out and labelled relative to each other on the same drawing.

### The problem

With both overlays enabled, a space's Part F requirement and its Ventilation Design duty anchor at (very
nearly) the same point - each overlay computes the room's own internal point independently, and both
computations are deterministic functions of the same outline, so they coincide. The shared placement engine
(`Solver2D`, via `PartFTagPlacement`) then fans both authorities' tags out from that one point using the same
8-direction search, with no rule saying which authority a displaced tag belongs to. The result, exactly as
native acceptance showed it: `EXT 82.5`, `EX 22.0 ?`, `SUP 150.0`, the space's own name tag, `SUP 30.0 ✓` -
Part F and Design interleaved with no way to tell them apart except position, which the search had already
scrambled.

### The fix

**1. Two stable lanes, not a second solver.** `PartFTagPlacement.Lane(IClosed2D limitArea, double row, bool
above)` is a new pure-geometry method on the EXISTING shared adapter - no change to `Solver2D` (which lives
in `SAM.Geometry`, out of scope for this task) and no second placement engine. It clips a tag's existing
centre constraint (its room outline, or nothing for a transfer tag) to the half of the room on its own side
of a shared reference row: `PartFAirflowRenderer.Place` builds every terminal tag's `LimitArea` with
`above: true`, `DesignAirFlowRenderer.Place` with `above: false`. The row itself is each space's own outline
internal point - the same point either overlay already anchors an un-fanned mark at - so the two renderers
agree on where the line is without either one reading the other's marks, keeping the existing one-directional
architecture (Part F never depends on Design) intact.
<br>A room the row would cut off entirely (fewer than three boundary points survive the clip) falls back to
the unclipped outline rather than an empty region: the lane is a presentation preference, and a tag that
cannot be placed at all is a regulatory or design figure lost from the drawing, which is the worse failure. A
transfer tag has no room outline to begin with, so it keeps its existing behaviour (no `LimitArea`, arrow and
anchor untouched) and relies on the label change below alone.

**2. An explicit textual identifier on every tag.** `PartFAirflowRenderer.AuthorityPrefix` ("F ") and
`DesignAirFlowRenderer.AuthorityPrefix` ("D ") are prepended to a tag's DRAWN text only - `PartFOverlayMark.Label`
/ `DesignAirFlowOverlayMark.Label` and the pure overlay-builder classes are untouched, so nothing that reads
those values elsewhere (schedules, the text schematic, the existing `mark.Label` unit tests) changed. Neither
authority is ever called "Calculated": a Part F requirement, a design airflow, an operating airflow and an
equipment capacity may all be calculated values, so that word would say nothing about which this tag is.

**3. Both distinctions apply everywhere both authorities are drawn**, terminal and transfer alike - "F TRA
8.0 l/s ?" is never mistakable for "D TRA 67.5 l/s ?" - without recomputing any transfer air, inventing a
value where none exists, or touching the existing "no modelled transfer opening" `?` behaviour, the transfer
arrow's geometry, or `SpaceAirMovement` direction.

### Why this could not regress existing scalability or the one-directional invariant

`Lane` runs inside `Place()` - the already-expensive, already-cached per-space call, not a per-redraw or
per-mark one - and does one small polygon clip (a handful of boundary points) per unique space, exactly the
same cost order as the outline lookup it sits next to. It never adds a global obstacle (which would have
been checked against every candidate position for every tag on the plan and would have reintroduced the
O(N²) shape the existing scaling tests guard against); it only tightens the per-item `LimitArea` centroid
check the engine already made. `DesignAirFlowRendererScalingTests.Load_AllocatesLinearlyWithTheModel_UpToTwoThousandSpaces`
stays green with this change in place. Part F's own placement method calls the new method exactly the way it
already called the outline lookup, so `PartFTagPlacementTests.Place_NeverReadsTheViewTransform` and
`DesignAirFlowRendererTests.PartFPlace_NeverReachesDesignAirflowRenderer` (both IL-reachability assertions)
remain true unmodified.

### Production files changed

- `SAM_UI/SAM.Analytical.UI/Classes/PartF/PartFTagPlacement.cs` - `Lane`, `HalfPlane`, `Clip`.
- `WPF/SAM.Analytical.UI.WPF/Controls/PartFAirflowRenderer.cs` - `Place` wraps its terminal `LimitArea` in
  `Lane(..., above: true)`; new `Row` cache; `Label` gains the `AuthorityPrefix` ("F ") constant.
- `WPF/SAM.Analytical.UI.WPF/Controls/DesignAirFlowRenderer.cs` - `Place` wraps its terminal `LimitArea` in
  `Lane(..., above: false)`; new `Row` cache; new `AuthorityPrefix` ("D ") constant and `Label` helper, used
  by both `Size` and `DrawTag` so the measured box matches the drawn text.

### Tests added

- `WPF/SAM.Analytical.UI.WPF.Tests/PartFTagPlacementLaneTests.cs` (new) - `Lane` in isolation: clips to the
  requested half, the two lanes of one room never overlap, a transfer tag's null `LimitArea` still gets the
  bare half-plane, and a room the row cuts off entirely falls back to its whole outline rather than nothing.
- `WPF/SAM.Analytical.UI.WPF.Tests/PartFDesignLaneTests.cs` (new) - the end-to-end contract through the real
  renderers, on native acceptance's own numbers (Part F SUP 30 / Design SUP 150 on one space): Part F above
  the row, Design below it, both distinguishable by label even when the underlying rate text coincides; the
  lane holds at a coarser annotation scale; each lane is unaffected by the OTHER overlay not existing at all;
  the transfer label distinction and the existing "?" behaviour survive together. Deliberately does not
  repeat what `OverlayOwnershipTests` and `DesignAirFlowRendererTests` (in particular
  `PartFsPlacement_IsIdentical_WhetherOrNotDesignOverlayExists` and
  `PartFPlace_NeverReachesDesignAirflowRenderer`) already prove.

### Validation

```text
dotnet test WPF/SAM.Analytical.UI.WPF.Tests/SAM.Analytical.UI.WPF.Tests.csproj -c Release --no-build
  -> Passed! Failed: 0, Passed: 640, Skipped: 0, Total: 640

dotnet build SAM_UI.sln -c Release
  -> 0 Error(s)

git diff --check
  -> clean
```

Baseline was 630/630; ten focused tests were added (630 + 10 = 640), none removed or changed.

Post-merge, on `sow/2026-Q3` at `77b45283e5d2e19f0f382eb3b1294f0c20ea6c69`:

```text
SAM.Analytical.UI.WPF.Tests             640/640
SAM_UI.sln Release                      0 errors
git diff --check                        clean
```

### Native acceptance

PASSED - see the note at the top of this entry. PR #96 merged into `sow/2026-Q3`
(`77b45283e5d2e19f0f382eb3b1294f0c20ea6c69`).

## Latest (2026-09-07): the manual Part O workflow reaches its results

**Status: root-caused, implemented, tested, native-accepted, and merged (PR #95,
`0eb9d12e76c5f481b978d41d1338794aec24d1fc`).**

Nothing here changes Part O engineering. It changes which model replacements are treated as edits.

### The defect

Reported and reproduced: `Prepare & Run` reaches `Results > Overheating (TM59)`, but the expert sequence

```text
Edit > Part O > Prepare Iteration
Simulate > Energy Simulation      (full year, completes successfully)
Results > Part O > Overheating (TM59)
```

leaves the Part O result commands **disabled** over results that exist and are valid, with no message
saying why.

### Root cause: SAM keeps view settings on the model, so looking at it counted as editing it

`AnalyticalWindow.UIAnalyticalModel_Modified` handed **every** model replacement to
`PartORun.NotifyModified()`, which drops a prepared or completed run as an unannounced outside edit. That
is right for an edit, an import, an undo, a redo or a second simulation. It is wrong for a **view** change -
and a view change is a model replacement in SAM, because view settings live on the model as
`AnalyticalModelParameter.UIGeometrySettings`.

Thirteen production writes announce themselves with `ViewSettingsModification`, and every one of them sets
that parameter and nothing else: `Modify.Hide`, `Isolate`, `RemoveOverrides`, `ActivateViewSettings`,
`EditViewSettings`, `EnableViewSettings`, `EditLegend`, `SetGroup`, `CopyViewSettings`,
`CopyViewSettingsCamera`, `DuplicateViewSettings`, `RemoveViewSettings`, `SetActiveGuid`, and the
section-plane range in `AnalyticalWindow`. Not one of them moves a space, a panel, an aperture, an airflow,
a zone or an overheating scenario.

So: prepare the iteration, then switch view, isolate the dwelling you just prepared, or turn the Part F /
Ventilation Design overlay on to check it - and the run was gone. Silently, because `Modify.Simulate`
writes its "the Part O run was not completed" note **only for a run still in `Prepared`**; a run already
dropped gets no note at all. The full-year TAS run then completed normally, wrote its `.tsd` and its
per-run `.sam`, and the ribbon stayed disabled with a tooltip - the only place the reason appeared - saying
the model had changed when nothing about the model had.

### Why `Prepare & Run` was immune

It prepares, simulates and assesses inside **one** gesture: `RunPartOWorkflow` -> `PreparePartOIteration`
-> `Simulate` -> `AssessPartOTM59`, with the hub dialog modal over the whole thing. There is no point at
which a view can be touched, so the extra transition never fired. The expert path exists precisely to put
the user in front of the model between the two steps, which is why only it met the defect.

**The completion machinery itself was already shared and is unchanged.** Both paths run the same
`Modify.Simulate` -> `Modify.RunPartOSimulation` -> `PartORun.ExpectResults` -> `PartORun.Complete`
sequence, with the same full-year gate (`Query.IsPartOFullYearSimulation`) and the same lineage rule
(`PartORun.IsResultsOfThisRun`). No second definition of a valid run was created here, and none existed.

### The fix

**1. One place decides what a replacement was.** New pure query
`SAM.Analytical.UI.Query.IsModelChange(IEnumerable<IModification>)`: presentation-only means every
modification in the set is a `ViewSettingsModification`, and **anything it cannot prove** - a null set, an
empty set, a `FullModification`, a mixed set, a type added later - is a model change. Dropping a run that
did not need dropping costs a preparation; keeping one that did would pair a preparation's scenarios with a
different model, so the safe way to be wrong is to drop.

It is deliberately **not** read off `IModification.Undoable`, which answers a different question: an
appearance edit is undoable and presentation-only, a camera move is not undoable and also
presentation-only.

**2. `PartORun.NotifyModified(bool modelChanged)`.** A presentation-only replacement is not an event at
all: it neither drops the run nor consumes an armed `ExpectModification`. The second half matters on its
own - a view change between a Part O command's arming and its own write used to spend the one-shot
expectation, so the command's write was then read as somebody else's edit.

**3. `PartORun.StateChanged`, so availability follows the run and not the caller.** `Modify.Simulate`
completes the run **after** the model replacement it belongs to, so the reload that replacement triggers
necessarily refreshes the ribbon while the run is still `Prepared`. Every caller therefore had to refresh a
second time afterwards, and a caller that forgot left a completed run with unavailable results. The run now
announces each transition once and `AnalyticalWindow` refreshes on it, which is what makes "Overheating is
available the moment the simulation finishes, with nothing to refresh by hand" structural rather than a
convention. Existing explicit `RefreshPartOButtons()` calls are left in place - they also cover the
tooltip-only reads where `IsAssessable` drops a run as the gate.

Exactly one announcement per public transition: `Prepare` and `Restore` clear the run before they know what
they can build, so they use a non-announcing `ResetCore` / `RestoreCore` and announce their own outcome.

### Invariants preserved

Unchanged, and pinned by the tests below: the workflow-model lineage rule, the full-year requirement, the
results fingerprint, the refusal of a second simulation's results, the drop on a real edit (prepared and
completed alike), a restored run being reviewable but never resumable, `PartFRequiredAirFlow` /
`PartFTransferRequirement` and every other Part F, transfer-air, terminal-duty, Iteration 1b, equipment,
2B, TM59 and TAS behaviour. No Part O engineering was touched.

### Files changed

Production:

- `SAM_UI/SAM.Analytical.UI/Query/IsModelChange.cs` **(new)**
- `SAM_UI/SAM.Analytical.UI/Classes/PartO/PartORun.cs` - `NotifyModified(bool)`, `StateChanged`,
  `OnStateChanged`, `InvalidateCore`, `ResetCore`, `RestoreCore`
- `WPF/SAM.Analytical.UI.WPF/Windows/AnalyticalWindow.xaml.cs` - classify the replacement, subscribe
  `StateChanged`

Tests:

- `WPF/SAM.Analytical.UI.WPF.Tests/PartORunLineageTests.cs` - +10 (extended, not a parallel file: it
  already owns the production arm-write-complete sequence through `CompleteThroughAFullYearWorkflow`)
- `WPF/SAM.Analytical.UI.WPF.Tests/PartOResultReopenTests.cs` - +1, the restored-run half

### Tests, builds and validation

```text
SAM.Analytical.UI.WPF.Tests   630/630   (619 before, +11)
SAM_UI.sln Release            0 errors
git diff --check              clean
```

**The 11 new tests were reproduced against the old behaviour**, not merely written after the fix: with
`Query.IsModelChange` forced to answer "the model changed" for everything, all 11 fail and all 619
pre-existing tests still pass. They are driven through the production classifier rather than a hand-written
boolean, so they would fail again if the window started classifying a view change as an edit.

Covered: the manual path end to end; looking at a prepared model; a real edit still dropping it; a failed
simulation; a cancelled simulation; an unrelated results file and an unannounced one; `Prepare & Run`
unchanged plus its arming surviving a view change; a completed run surviving a view change; the
`StateChanged` refresh link asserted on what a refresh would read inside the handler; `IsModelChange`
itself; and a restored run surviving a view change.

### Unresolved / risks

- **Native SAM_UI acceptance - PASSED (2026-09-07).** Ran on the acceptance model:
  `Prepare Iteration` -> presentation/view changes including overlays/isolation ->
  `Energy Simulation` with Full Year enabled -> successful completion ->
  `Results > Overheating (TM59)` immediately available. No reopen and no TAS rerun required.
- **The manual Simulate dialog still opens with `FullYearSimulation` off**, because
  `SimulateOptions.FullYearSimulation` defaults to false and only `Prepare & Run` gets
  `Create.SimulateOptions_PartO`. That is intentional (the expert command must keep sizing-only and export
  runs available), and `Modify.Simulate` does say so afterwards - but it says so *after* the TAS time has
  been spent. Worth revisiting as a pre-run warning; not done here.
- **A run dropped by a real edit before a successful simulation is still not narrated.**
  `Modify.Simulate` notes the refusal only for a run still in `Prepared`; a run already dropped leaves the
  reason in the ribbon tooltip only. Deliberately not changed: `InvalidationReason` survives the whole
  session, so appending it to every simulation's message box would put a stale note on ordinary expert runs.
- **The manual path still takes its project name and output directory from the remembered manual
  `SimulateOptions`**, which overwrite the model-derived values `Modify.Simulate` sets first. Self-
  consistent within a run - so it does not affect completion - but it can put a Part O run's evidence under
  another project's name. Recorded, not acted on; the Part O preset already derives and locks both.

### Exact recommended next step

Native SAM_UI acceptance PASSED and PR #95 merged. Per the standing list and in this order:
overlay Part F vs Design visual grouping, the second ~180 l/s MVHR, and only then Iteration 3.

## Previous (2026-09-05, latest): the Part O defaults closeout

**Status: root-caused, implemented, tested, and accepted against a real completed run. Not merged.**

Nothing here changes Part O engineering. It changes what a person is asked, and what they are asked to
confirm twice.

### The defect: the one setting a Part O run cannot do without was off by default

`Prepare & Run` reaches the Simulate dialog through the ordinary `Modify.Simulate`, which seeds itself from
the manual command's remembered `SimulateOptions` and, failing those, from `UI.Create.SimulateOptions`.
`SimulateOptions.FullYearSimulation` is **`false`**. So the normal path was: prepare the iteration, press
Run, accept a dialog that looked reasonable, wait out a TAS run, and be told the run could not be completed
because it was not a full year.

Days 1 to 365 is not a preference. `Query.IsPartOFullYearSimulation` refuses every other range outright, so
there is no completable Part O run with any other value - the range was a **deterministic Part O value**
being presented as an unticked box.

The observed "Sizing on, Simulation off" is exactly this: sizing is on by default, the **annual** simulation
box is off by default.

### The fix: a Part O preset feeding the existing authority

`Create.SimulateOptions_PartO` builds the dialog's state for a Part O run. `Modify.Simulate` takes a new
`partOWorkflow` flag - **passed by `RunPartOWorkflow`, defaulted false for everybody else** - and on that
path seeds from the preset and locks what a Part O run does not leave open.

The authority is unchanged: `SimulateWindow` still collects the settings, `PartOSimulationContext` still
carries them, `RunPartOSimulation` still runs them. The preset only decides what the dialog opens with.

| setting | value | why |
|---|---|---|
| `Simulate` | **on**, locked | no TSD without it |
| `FullYearSimulation` (days 1-365) | **on**, locked | `IsPartOFullYearSimulation` refuses anything else |
| `Sizing` | **on**, locked | see below |
| `UnmetHours`, `UseWidths`, `UpdateConstructionLayersByPanelType` | fixed, locked | part of the case `PartOCanonicalTBD` fingerprints |
| RDS / SAP / Part L / TPD / Domestic Overheating XML | **off**, locked | other workflows' deliverables |
| project name | **derived from the model**, locked | it is the run's identity, not a label - see below |
| weather | project's own; previous run's only where the model states none | genuine input |
| output directory | previous run's while it still exists; else the model's | genuine input |
| solar calculation method | carried | genuine input |

**The project name is derived and locked, not prepopulated.** Every artifact a Part O run is judged by
derives from it - `<project>.tbd`, `.tsd`, the per-run `.sam` and `<project>-TM59.txt`. On an isolated run it
carries the scope token `Query.ProjectName_Isolated` put there so that run's evidence cannot land on a full
run's or on another selection's, and `PartOSimulationContext.Iteration_ProjectName` reads the optimisation
round back out of it - so a hand-edited name can restart Iteration 2B's numbering at `-Opt01` and overwrite a
previous optimisation's evidence. Nothing downstream refuses an edited name; it is simply believed. It is
therefore a **deterministic, derived Part O input and not a user decision**. This costs nobody anything:
*where* the evidence is written is still theirs to redirect, because moving a run's files changes nothing
about what the run is.

**Sizing stays on, deliberately.** Nothing in the assessment or in Iteration 2B reads a design load, so it is
not a Part O deliverable - but it is not free to turn off either. `Tas.Query.Sizing` runs `sizing(0)` over
the TBD that is about to be simulated and writes the sized plant capacities into it, so the annual run is a
**different thermal case** with it off. On is what every Part O run to date was produced with. Changing it is
Part O engineering, which this pass was told not to do.

### The manual Simulate command is untouched

Two things guarantee it, neither of which is a convention somebody has to remember:

- **A settings key of its own.** `AnalyticalSettingParameter.SimulateOptions_PartO` sits beside
  `SimulateOptions`, so the manual command's remembered options cannot decide a Part O run's case, and a
  Part O run cannot retune the expert dialog behind an engineer's back.
- **Opt-in, defaulted off.** The ribbon's own Simulate button hands `Modify.Simulate` the session's Part O
  run - it has to, so a workflow over a prepared model can complete it - so "a run is prepared" is *not*
  enough to mean "this is the Part O command". Only `RunPartOWorkflow` passes `partOWorkflow: true`.

`TheManualSimulateDefaults_AreUnchanged` and `TheManualSimulateCommand_IsNotOptedIn` pin both.

### Staleness is a property, not a rule

Only the weather, the output directory and the solar method are read back out of the remembered Part O state,
and the first two are re-validated against the model and the filesystem. Everything else is re-derived. So a
change of scenario, of dwelling scope, of model or of machine cannot carry a stale setting into the next run,
because there is no path by which one could. `PartO_ReadsNoFixedSettingOutOfTheRememberedState` pins it.

### A reopened run now says why it cannot continue into Iteration 2B

**The restriction is kept.** The investigation is recorded because the reason is not the obvious one.

It is not the results and it is not the design: `PartORun.Restore` validates both against
`SimulationResultProvenance`, and an optimisation **never re-simulates its baseline** anyway
(`Modify.OptimisePartOTM59`, run 0, starts from `AnalyticalModel_Assessment` and `Path_TSD` - both of which a
restored run has). The blocker is the **preparation**. Every 2B round re-prepares the design it has just
changed, and is handed `PartOPreparationContext.VentilationUnitCapacityDescriptors` - the manufacturer
catalogue *as it was* when the baseline selected its units, carried rather than re-read precisely so nothing
can change what a selected unit is rated at part-way through a run.

That snapshot is the one thing a file cannot hand back:

- `VentilationUnitCapacityDescriptor` is deliberately **not** an `IJSAMObject` - `SAM.Analytical` owns the
  selection rule and not the product list - so persisting it would mean fingerprinting a catalogue into the
  model file, which this SOW **excludes from scope**.
- Re-reading today's catalogue instead would let a round be checked against capacities the baseline was never
  selected under. `guids_AtCapacity` - the dwellings 2B stops at because they sit on their unit's ceiling -
  is computed from exactly those capacities, so a moved ceiling silently moves the answer.

Re-preparing is therefore the honest route back in, and it is one gesture: the reopened model **is** the
prepared design, so `Prepare & Run` over the same scenario and scope reaches a run carrying both contexts
again. `Modify.CanOptimise` now refuses a restored run with that reason ahead of the two generic null checks,
instead of reporting it as a run that was never recorded.

### Performance

No new model inspection. The Part O preset deliberately does **not** read the zone category list: that list
feeds one combo box, and the combo is enabled only while the SAP or domestic-overheating export is ticked,
both of which are now fixed off and locked. The ordinary Simulate command still reads it, because there those
boxes are a person's to tick. Opening the Part O hub is untouched and remains the one inspection SAM_UI#88
established.

### Click path

| step | before | after |
|---|---|---|
| Part O -> Prepare & Run | hub: scenario, scope, dwellings, 2B | unchanged |
| | preparation summary -> OK | unchanged |
| | Simulate dialog: **tick Full Year Simulation**, check Sizing, untick whatever the last manual run left on, pick weather, check output directory -> OK | Simulate dialog: **weather and output directory only** -> OK |
| | full-year TAS run *(or a wasted run, and "not a full year")* | full-year TAS run |
| | TM59 result | unchanged |

### Tests

**554 passing, 528 before** (+26). New: `PartOSimulateDefaultsTests` (25). Updated:
`PartOResultReopenTests.ARestoredRun_CanBeReviewed_ButNotResumed` now pins the restored-run reason rather
than the generic one.

### Manual acceptance

Against a real completed run - `000000_SAM_AnalyticalModel-It1a-futureZ1-ISO-e77fa00a-Opt02.sam` from
`SAM_daily/2026-07-15 PartO`, a 5-space / 2-zone isolated Iteration 1a:

- reopen -> `Restore` = true, `WorkflowCompleted`, `IsRestored`, `IsAssessable` = true, 2 scenarios, the real
  `.tsd` resolved;
- review -> `CanOptimise` = false with the **new** restored-run reason;
- the Part O Simulate preset over the same model opens with the project's own weather
  (`Z1_DSY1_2050s_HIGH90_CIBSE_v1.1`), the run's own output directory, full year on, sizing on, every export
  off, zone categories not read.

No TAS run was repeated: the existing results were sufficient to exercise the seam. The dialog itself was
not clicked through in a live SAM_UI session in this pass - the seam was driven through the same public API
the dialog uses.

### Unrelated findings (recorded, not acted on)

- **`SimulateControl.SetSimulateOptions` self-assigns `CreatePartL`** (`CreatePartL = CreatePartL;`), so a
  remembered "Create Part L" tick is never restored into the manual dialog. Left alone: fixing it changes the
  manual command's behaviour, which this pass was told to preserve.
- **The From/To day boxes are editable exactly when they are ignored.** `EnableFullYearSimulation` enables
  them while "Full Year Simulation" is *un*ticked, and `Modify.Simulate` passes `-1` for the range in that
  case - so a partial-range run cannot be asked for through this dialog at all. Irrelevant to Part O, which
  is always 1-365.
- **Runs saved before the 2026-09-03 provenance work cannot be restored.** A pre-`b4420960` `.sam` records a
  digest computed by the older fingerprint, so `Restore` refuses it as "the model has changed since the
  simulation results were produced from it". Confirmed on
  `000000_SAM_AnalyticalModel-It1a-futureZ1-Opt06.sam`. Correct behaviour for a fingerprint change, but the
  message names the wrong cause. Not fixed here: the invalidation rules are not to be weakened.

## Previous (2026-09-05, later): large-model lookup closeout - the SAM_UI half

**Status: root-caused, implemented, tested and measured. Not merged.**

Two things, neither of which changes what anything reports. The lookups find the same objects a cheaper way;
the hub asks for the same inspection, once instead of nine times.

### PF3 and PF4 - the failing-space and dwelling-zone lookups

`AdjacencyCluster.GetSpaces()` and `GetZones()` **rebuild** their whole list from the relation cluster on
every call. Three sites did that inside a loop:

- `Query.PartOOptimisationTargets` resolved each **failing** room with
  `(adjacencyCluster.GetSpaces() ?? []).Find(...)`, so a round that failed widely on a block rebuilt the
  model's space list once per failure.
- `Query.PartODwellingSpaceGuids` walked the whole zone list once per **requested dwelling** - quadratic on a
  block, where the dwelling count grows with the room count.
- `Modify.PartialAssessment` named each unassessed in-scope room with a linear `Find`.

All three now ask `AdjacencyCluster.GetObject<Space>(guid)` / `GetObject<Zone>(guid)`, which is a lookup in
the cluster's live object dictionary. **Nothing is snapshotted**, so there is no picture of the model being
optimised that could go stale, and no parallel lookup system beside the cluster's own.

Equivalence is asserted **by reference** over every space and every zone of models of 100 / 500 / 1,000 /
5,000 rooms, plus an unknown guid, `Guid.Empty` and a guid belonging to an object of the other type. What
the round *decides* with those rooms is unchanged and stays pinned in `PartOOptimisationTests`.

### The Part O hub inspected the model nine times to open once

Opening the hub is one gesture, and it moved seven inspection inputs one at a time - the constructor, then
the model, the run, the catalogue and the session capabilities, then `Restore` putting back the scenario, the
scope and the saved dwelling scope, then `Restore`'s own closing refresh. Every one was a correct response to
a genuine change; all but the last were responses to a state nobody would ever see. On a five thousand space
project each of them walks the dwelling scope.

A ninth came from re-entrancy that predates all of this: `UpdateOptimiseControls` clears the Iteration 2B
tick where the scenario cannot carry one, clearing it raises `Unchecked`, and the lightweight
`RefreshWorkflowInput` that answers it fell back to a **full** refresh whenever there was no inspection to
reuse yet - from inside the refresh that was about to produce one.

**The fix is a deferral, not a cache.** Nothing is remembered, compared or reused: while the dialog is being
set up a refresh records that an inspection is *owed*, and when it is paid it is a full inspection of
whatever the window then holds, asking every authority exactly what it asked before. There is no stored
engineering answer anywhere in WPF and no attempt to decide whether an input "really" moved - that would be a
second opinion about the model, which this window is not allowed to have.

Initialisation ends at `PartOWorkflowWindow.CompleteInitialisation()` - which `Modify.RunPartOWorkflow` calls
once it has finished setting the dialog up - and, as a safety net, at the first moment the answer is actually
needed: the window being shown (`OnSourceInitialized`), or any derived state being read. **After it ends the
window is eager again**, and every genuine change of scenario, scope, dwelling selection, model, run,
catalogue or capability inspects immediately, as it always did. A status list that updated only when somebody
happened to read it would be a window showing the scope the user came from.

### Operation counts, before and after

| operation | before | after |
|---|---|---|
| opening the hub (any model size) | **9 inspections** | **1** |
| `PartOOptimisationTargets`, f failures over n rooms, d requested dwellings over z zones | f space-list rebuilds + d x z zone comparisons | f + d hash lookups |
| `PartialAssessment`, u unassessed rooms | 1 rebuild + u x n comparisons | u hash lookups |

### Scaling evidence (structural; allocated bytes, no timing thresholds)

Doubling ratios from `GC.GetAllocatedBytesForCurrentThread`; the test asserts only `< 2.6`.

| measurement | after | the code it replaced |
|---|---|---|
| Iteration 2B target selection | x2.02, x2.02 | x3.81, x3.90 |

Local wall clock, from a `[Trait("Category", "Benchmark")]` test that asserts nothing about time: target
selection over **5,000 rooms** is **32.5 ms**, against **1,529.6 ms** for the resolutions it replaced.

The hub count is asserted directly at **500 dwellings**: `OpeningTheHubOnABlock_IsStillOneInspection`. The
**nine** is measured rather than counted off the source - the fixture exercises all nine triggers, and
removing the deferral makes that same test report exactly nine.

### Files changed

`WPF/SAM.Analytical.UI.WPF/Query/PartOOptimisationTargets.cs` (the failing-space and dwelling-zone lookups),
`WPF/SAM.Analytical.UI.WPF/Modify/OptimisePartOTM59.cs` (the subset-pass guard's name lookup),
`WPF/SAM.Analytical.UI.WPF/Windows/PartOWorkflowWindow.xaml.cs` (the deferral, the re-entrancy guard and an
`InspectionCount` exposed for tests), `WPF/SAM.Analytical.UI.WPF/Modify/RunPartOWorkflow.cs` (one
`CompleteInitialisation()` after the restore).

New tests: `WPF/SAM.Analytical.UI.WPF.Tests/PartOFailingSpaceLookupScalingTests.cs`,
`WPF/SAM.Analytical.UI.WPF.Tests/PartOWorkflowInitialisationTests.cs`.

**No engineering cache was added to `PartOWorkflowWindow`, `PartOWorkflowInspection` or anywhere else in
WPF**, and the workflow state semantics from PR #85 and the refresh split from PR #86 are untouched -
`PartOWorkflowRefreshTests` passes unchanged.

### Validation

| suite | result |
| --- | --- |
| `SAM.Analytical.UI.WPF.Tests` | **528 passed, 0 failed** (510 pre-existing, all passing, + 18 new) |
| `SAM.Tests` | **1974 passed, 0 failed** |
| `SAM.Analytical.Tas.TM59.Tests` | **690 passed, 0 failed** |

`SAM_UI.sln` builds against the rebuilt `SAM.Analytical.dll` with 0 errors. `SAM.sln` builds with 0 errors.
`git diff --check` clean in both.

No licensed TAS run was made and none is owed: nothing here changes an engineering result.

### Remaining risks

- The deferral is safe only because nothing external reads the window's derived state without going through
  a property. Every member a `RefreshCore` writes now ends initialisation before answering, and
  `ReadingTheStatusWithoutCompletingInitialisation_PaysTheDeferredInspection` pins that a caller which never
  calls `CompleteInitialisation` still sees a real status list.

### Remaining pre-Iteration-3 list

Part O Sizing / Simulation defaults and the minimum-click workflow; weather / range / output restoration UX;
resume 2B from a restored run; re-isolation and re-cutting; the Grasshopper variable-output updater;
catalogue identity drift; final UI acceptance; then freeze Iterations 1-2.

## Previous (2026-09-05): Iteration 2B correctness closeout - the Part O half

**Status: implemented and tested. Not merged. Blocked on SAM-BIM/SAM#100.**

### F1 / F3 - the two boundaries that already promised isolation

`Simulate` (both overloads) and `RunPartOSimulation` each take a copy and each say why: the model is renamed
and re-materialled in place, and a cancelled or failed run must leave the instance the user still has open
exactly as it was.

The copy was shallow, so it isolated the model's **name** and its **libraries** and nothing else. The
cluster objects were shared, and the writes below are in-place mutations rather than same-guid replacements
- `UpdateConstructionLayersByPanelType` re-materials the shared panels, and the TAS conversion stamps
identity onto the shared spaces, panels and apertures.

On the optimisation path the caller is the retained last-valid design of the previous round, so this is what
stops a round that later failed or was cancelled handing back a design whose persisted
`SimulationResultProvenance` no longer matches the results it was produced from.

### One deep copy per run, not three

`Modify.RunPartOSimulation` is now stated as **the** ownership boundary of a Part O TAS run and is the only
place on that path that clones.

`Simulate(UIAnalyticalModel, PartORun)` takes a **shallow** copy for the rename - `Name` is a field of
`AnalyticalModel` itself, so a new instance isolates it - and tracks `analyticalModel_Owned`. Adopting the
workflow's model adopts its ownership. The export block, which is the one place this method mutates a model
itself (`Tas.Convert.ToTBD` with `updateGuids`, and `RestampSimulationZoneIdentity` writing `ZoneGuid` onto
the live spaces), takes the deep copy there and only where no deep copy has been made yet - an export-only
run, or a workflow that produced nothing.

`Simulate(AnalyticalModel, string, IWin32Window)` converts the model itself before running the workflow, so
its copy cannot be deferred; it keeps its deep copy and tells the workflow the model is already owned.
`RunWorkflow` threads `analyticalModel_Owned` through, defaulting to false so every other caller is
unchanged.

Clone count per entry path, before -> after:

| Entry path | Before | After |
| --- | --- | --- |
| Part O run via `Simulate`, workflow succeeds | 3 | **1** |
| Export-only run (Simulate unticked) | 1 | 1 |
| Simulate ticked, run cancelled | 2 | 1 |
| Simulate ticked, workflow returned nothing | 2 | 2 (two independent mutable phases) |
| `Simulate(AnalyticalModel, path)` overload | 2 | 1 |
| Optimisation round / capacity envelope | 2 | 1 |
| `RunWorkflow` / Grasshopper / benchmark CLI | 1 | 1 |

### F2 - the envelope a second optimisation overwrote

Rounds already carry the iteration baseline the session started from -
`ProjectName_Iteration(iteration_Baseline + iteration)`, so a second optimisation numbers from `-Opt06`
rather than restarting at `-Opt01`. The capacity envelope was the one case that ignored it:
`{ProjectName}-OptMax`, unconditionally. Optimise `Flat1`, optimise again from its result, and the second
envelope overwrote the first's `.tbd`, `.tsd`, `.sam` and `-TM59.txt` - every one of which derives from this
one name.

Now `Flat1-Opt05-Max`, read off the run's own `Step_Baseline` - **the same iteration-baseline authority the
rounds use, not a second counter**. Iteration 0 keeps `-OptMax`, so a first optimisation and every
already-saved run still write and reopen the historic name. Either spelling still reads back as no
iteration, so an optimisation started from an envelope design still cannot renumber onto a round's files.
Two optimisations started from the *same* round collide here exactly as their rounds already do - a property
of the iteration baseline itself, documented rather than papered over.

### F4 - Part O states the full year, and the file does not get a vote

Traced `TSD -> Convert.ToSAM -> zoneData.ToSAM -> Query.AnnualZoneResult -> TM59AssessmentCalculator ->
TMOverheatingCalculator`: `AnnualZoneResult` returns whatever TAS hands over, with no length guard anywhere
on the path. Part O's own check (`PartOSimulationContext.IsFullYear`, and the `fullYear` flag
`RunPartOSimulation` returns) is over the simulation's **nominal date range** - what was asked of TAS, not
what the results file contains.

The first fix counted the requirement from the weather year the **TSD itself** carries. Codex was right to
refuse that: a file damaged to a third of its length loses its weather and its results together, so the
requirement falls to match the truncated series and the partial year is assessed and reported as an ordinary
verdict. **A results file may not decide how much of a year it was supposed to contain.**

The requirement is now `PartOSimulationContext.HourCount_FullYear` - derived from `Day_First_FullYear` and
`Day_Last_FullYear`, the same two days `IsFullYear` and `Query.IsPartOFullYearSimulation` test against, and
entirely independent of the payload being validated. It is **static** deliberately: a restored run - the
reopened-results path, the one most likely to meet a damaged file - carries no `PartOSimulationContext` at
all, because `PartORun.Restore` nulls it to keep a restored run out of Iteration 2B. It does not need one,
since a run can only have completed, and so only be restorable, if it was a full-year simulation.

A refused room now counts as **unassessed**, mapped back to its design space. Without that it simply
vanished from the verdict and `PartialAssessment` - which exists to refuse a pass whose dwelling scope has a
hole in it - never saw the hole, so the rooms whose data happened to survive were reported as a pass over
all of them.

### Invariants preserved

`PartFRequiredAirFlow` / `DesignAirFlow` / `SelectedEquipmentCapacity` / `OperatingAirFlow` remain distinct;
no Part F calculation, TM59 criterion or catalogue selection is touched; no round reselects equipment.

### Changed files

- `SAM_UI/SAM.Analytical.UI/Classes/PartO/PartOSimulationContext.cs` - the envelope name and the full-year
  hour authority
- `WPF/SAM.Analytical.UI.WPF/Modify/Simulate.cs`, `RunPartOSimulation.cs`, `RunWorkflow.cs` - ownership and
  the one copy
- `WPF/SAM.Analytical.UI.WPF/Modify/OptimisePartOTM59.cs` - the envelope's lineage
- `WPF/SAM.Analytical.UI.WPF/Classes/PartO/PartOTM59Assessment.cs` - the full-year requirement and refused
  rooms counting as unassessed
- `WPF/SAM.Analytical.UI.WPF.Tests/PartOCapacityEnvelopeTests.cs`, `PartOFullYearSeriesTests.cs`

### Merge order

1. **SAM-BIM/SAM#100** - the ownership constructor, the deep-clone completeness fix, and the TM59 series
   rules. Nothing else compiles without it.
2. **SAM-BIM/SAM_Tas#48** - the workflow's owned-model overload. Needs #100.
3. **SAM-BIM/SAM_UI#87** - the Part O boundaries, the capacity-envelope name, and the full-year authority.
   Needs #100. Independent of #48 to compile; both are needed for the F1/F3 invariant to hold end to end.

`SAM_Systems` is untouched.

### Validation

| Suite | Result |
| --- | --- |
| `SAM.Tests` | 1934 passed, 0 failed |
| `SAM.Analytical.Tas.TM59.Tests` | 690 passed, 0 failed |
| `SAM.Analytical.UI.WPF.Tests` | 510 passed, 0 failed |

`SAM.sln`, `SAM_Tas.sln` (MSBuild - COM references) and `SAM_UI.sln` all build with 0 errors.
`git diff --check` is clean in all three repositories.

Deep-clone cost, measured Release on 5,000 spaces / 30,000 panels: **250.7 ms, 136.8 MB**, against
36.5 ms / 13.8 MB for the shallow copy. Paid **once** per Part O TAS run.

### Remaining risks and next task

- No licensed TAS run was made. Every invariant here is established by deterministic tests over production
  code; what is not covered is TAS's own behaviour, which none of these changes touch.
- `SAM.Weather.Query.RunningMeanDryBulbTemperatures` still throws on a weather year shorter than the one the
  running mean needs, so a TSD with a damaged weather record fails loudly rather than being refused with a
  diagnostic. Pre-existing, characterised by test, and a fix reaches wider than Part O.
- Next: the pre-Iteration-3 list is unchanged - PF3-PF7 and `SetSpaceDesignFlowRate` indexing, the Part O
  defaults / minimum-click audit, re-isolation, the Grasshopper variable-output updater, catalogue identity
  drift, final UI acceptance, then freeze Iterations 1-2.

## Previous (2026-09-04, later): Part F large-model scaling - the SAM_UI half

**Status: implemented, tested and measured. Not merged. Blocked on SAM-BIM/SAM#99.**

### What was slow

A Part O inspection reads every room of the dwelling scope **three** times - once to place it, and once for
each of the two Approved Document F directions - and every one of those reads re-resolved the room against
the model's whole space list. `AdjacencyCluster.GetSpaces()` rebuilds that list on every call, so inspecting
a five thousand space project was quadratic in the model. `OptimisePartOTM59.DesignAirFlowStates` did the
same, once per room-direction of the run.

**The cost was never in SAM_UI's own code.** The PR #85 scalability fixes stand and were not reopened:
restoring a dwelling selection is batched, search does not inspect the model, UI-only optimisation inputs do
not inspect the model, genuine model/scope/scenario changes still do, and no engineering calculation is
cached or duplicated in WPF.

### What changed here

The fix is entirely SAM's. `PartFIndex` is one request-scoped snapshot of the model's space identities that
holds **no rate** and lives for one call. `PartOWorkflowInspection.Inspect` builds one at the top and
threads it through the two stages that need it.

`Spaces_Scope` used to build its own `Guid -> Space` dictionary over the model. That is now
`PartFIndex.Spaces_Zones` - one place, in SAM, that turns dwelling zones into the space instances the model
currently carries - so this file keeps no lookup of its own that could drift from the model. Behaviour is
identical: first occurrence wins, zone order then relation order, and a related space the model no longer
carries is dropped.

`Inspect` gains an optional trailing `PartFIndex partFIndex_Scope = null`. Production passes null and gets a
snapshot built for the call; the tests pass a counting subclass. No existing call site changed.

**No cache was added to `PartOWorkflowWindow`, to `PartOWorkflowInspection`, or anywhere in WPF**, and the
workflow state semantics PR #85 established are untouched. `TwoInspections_CostExactlyTwiceOne` pins that: a
cache appearing in the dialog would show up as the second inspection asking less.

### Files changed

`SAM_UI/SAM.Analytical.UI/Classes/PartO/PartOWorkflowInspection.cs`,
`WPF/SAM.Analytical.UI.WPF/Modify/OptimisePartOTM59.cs`, new test
`WPF/SAM.Analytical.UI.WPF.Tests/PartOWorkflowInspectionScalingTests.cs`, and this file.

### Validation

`SAM_UI.sln` builds against the `SAM.Analytical.dll` rebuilt from SAM `perf/partf-bulk-context` - 0 errors.
`SAM.Analytical.UI.WPF.Tests` **498 passed / 0 failed** (491 pre-existing all passing, + 7 new).
`git diff --check` clean.

Counts asserted exactly at 60 / 300 / 1200 spaces: one snapshot, `2n` requirement calls and `3n` identity
resolutions for `n` rooms in scope. **No test here has a time limit** - those counts are properties of the
algorithm and are the same on every machine, which a duration is not. The measured effect is recorded in
SAM's own `PROJECT_PROGRESS.md`: at 5000 spaces the aggregation goes from 2221 ms to 46 ms locally, and
allocated bytes per doubling of the model from x3.8 to x2.0.

Licensed TAS was not run: the results are bit-identical to `Query.PartFRequiredFlowRate_Lps`, so there is no
engineering change for a simulation to validate.

### Unresolved / risks

The project reference is the built `..\..\..\SAM\build\SAM.Analytical.dll`, so a machine that has not built
SAM `perf/partf-bulk-context` will fail to compile this branch with "PartFIndex could not be found". That is
the dependency, not a defect.

### Recommended next step

Merge SAM-BIM/SAM#99, then review and merge SAM_UI#86. Do not merge this one first.

## Previous (2026-09-04): the Part O workflow, as one command

**Status: implemented, tested, and accepted against three licensed TAS runs of the real fixture.**

### Why

The normal Part O workflow required knowing eight expert commands and the order they go in - Open, Assign
Zones, Map IC (TM59), AddVent PartF, Prepare Iteration, Energy Simulation, Overheating (TM59), Optimise
(2B) - and the only way to find out which had already been run over a model was to run the next one and
read its refusal. The commands are individually correct; collectively they were unusable by anyone who had
not read them. This is the one command that answers all of it up front.

**Nothing was removed.** Every expert command is exactly where it was, and the new dialog names them.

### The orchestration chain, and who owns each stage

The high-level command **calls existing methods and adds no engineering of its own**:

| Stage | Authority | Layer |
| --- | --- | --- |
| What a dwelling is | `Query.PartFDwellingZones` | `SAM.Analytical` |
| TM59 classification | `TM59Manager.TM59SpaceApplications(InternalCondition\|Space, TextMap)` - the pair `TMOverheatingCalculator` classifies a simulated space with | `SAM.Analytical` |
| Part F requirement | `Query.PartFRequiredFlowRate_Lps(cluster, space, FlowClassification)` | `SAM.Analytical` |
| Whether Part F applies to a route | `Query.PartOIterationVentilationMode` + `Query.PartOPartFAirflowApplication` | `SAM.Analytical` |
| Preparation, isolation, scenarios | `Analytical.Modify.PreparePartOIteration` | `SAM.Analytical` |
| Equipment catalogue | `VentilationUnitCatalogue.Read` | `SAM.Analytical.UI.WPF` |
| Pre-simulation gate | `PartOPreSimulationCheck.Gate` inside `Modify.RunPartOSimulation` | `SAM.Analytical.UI(.WPF)` |
| TAS case, run, persistence | `Modify.Simulate` -> `Modify.RunPartOSimulation` -> `PersistPartORunModel` | `SAM.Analytical.UI.WPF` |
| Results availability | `PartORun.IsAssessable` | `SAM.Analytical.UI` |
| Assessment | `Modify.AssessPartOTM59` -> `PartOTM59Assessment.Assess` | `SAM.Analytical.UI(.WPF)` |
| Iteration 2B availability | `Modify.CanOptimise` | `SAM.Analytical.UI.WPF` |
| Iteration 2B | `Modify.RunPartOOptimisation` | `SAM.Analytical.UI.WPF` |

`Modify.RunPartOWorkflow` -> `PreparePartOIteration(request)` -> `Simulate` -> `AssessPartOTM59`, each step
gated on the previous one's own outcome read off `PartORun`. There is no second Part O implementation, no
calculation in WPF, and no giant replacement method.

### The one new seam

`Modify.PreparePartOIteration` gained a **request overload** -
`PreparePartOIteration(uIAnalyticalModel, partORun, PartOWorkflowRequest, VentilationUnitCatalogue, owner)`
- which is the whole of the old method from the point the picker dialog closed. The picker now builds a
request from its own controls and calls it; so does the new command. **One implementation, two ways in.**
It returns `bool` so the caller can tell an adopted preparation from a refusal or a decline.

No `SAM` change was needed. No other production method changed behaviour.

### State validity - a match against the model, never a UI cache

`PartOWorkflowInspection.Inspect(model, request, run, capabilities, textMap)` reports eight stages, each
`READY` / `REUSED` / `NEEDS PREPARATION` / `REQUIRED` / `N/A` / `PENDING` / `NOT RUN`, with one sentence
each. **Only `REQUIRED` stops Run**, and it is used exactly where the production chain would refuse
anyway: no dwelling zone, nothing TM59 can classify, no continuous Part F requirement on the mechanical
route, a requested product selection with no catalogue. No warning is promoted - a Part O model carries
intentional warnings on every run.

**Reuse is a match, not a cache.** `ReusePreparation` compares the request against the run's own
`PartOPreparationContext` - iteration, dwelling scope by guid, the route word per zone, whether a catalogue
was offered, isolation - and skips the preparation only where all of them agree. A restored run is never
reusable, because it carries no preparation context at all. Nothing is remembered between one showing of
the dialog and the next.

**A model that is already an isolated extract says so**, read from the `PartOIsolationContext` the
preparation stamped on it and the `.sam` carries, in both the status line and the scope note.

**The SAM Check gate is not duplicated.** It judges the normalized model TAS is given, which does not exist
until the run builds it, so the stage reports `PENDING` and says where it runs.

### The dialog

One compact `PartOWorkflowWindow`: scenario, scope, dwelling selection, a collapsed 2B block, the status
list, the blocker line, and three actions.

- **Scenario** - Iteration 1a (mechanical / design MVHR), 1b (natural ventilation), 2 (1a + a selected
  manufacturer unit). `PartOWorkflowScenario` states the 1a/2 relationship once; **2B is deliberately not a
  scenario** and is offered as a follow-on only, on Iteration 2 only, cleared rather than greyed elsewhere.
- **Scope** - all dwellings / selected dwellings / selected dwellings in isolation. On "all dwellings" the
  request is SAM's own answer unmodified and the dwelling list is hidden.
- **Scalable selection** - the existing `PartODwellingSelection`: one row per DWELLING ZONE, never one per
  space, virtualized, searched in place, selection held on the record. Typing in the search box updates the
  selection line only - it changes no stage - so a 5,000-space model does not re-inspect per keystroke. The
  TM59 text map is read once per dialog rather than per rebuild.
- **Actions** - Prepare & Run, Review Results, Optimise (2B), each enabled by its own authority with that
  authority's refusal as the disabled tooltip. The dialog reopens after each action with its status
  rebuilt, so it is a hub rather than a wizard; the choices carry across, the state does not.

Ribbon: **Simulate > Part O > Prepare & Run** (`RibbonButton_PartOWorkflow`). Edit > Part O > Prepare
Iteration and Results > Part O are untouched, and share the same `partORun`.

### Files

Added - `SAM_UI/SAM.Analytical.UI`:
- `Enums/PartOWorkflowScope.cs`, `Enums/PartOWorkflowStage.cs`, `Enums/PartOWorkflowStageStatus.cs`,
  `Enums/PartOWorkflowAction.cs`
- `Classes/PartO/PartOWorkflowRequest.cs`, `PartOWorkflowScenario.cs`, `PartOWorkflowCapabilities.cs`,
  `PartOWorkflowStageState.cs`, `PartOWorkflowInspection.cs`

Added - `WPF/SAM.Analytical.UI.WPF`:
- `Classes/PartO/PartOWorkflowStatusRow.cs`
- `Modify/RunPartOWorkflow.cs`
- `Windows/PartOWorkflowWindow.xaml{,.cs}`

Modified:
- `WPF/SAM.Analytical.UI.WPF/Modify/PreparePartOIteration.cs` (the request overload; picker delegates to it)
- `WPF/SAM.Analytical.UI.WPF/Windows/AnalyticalWindow.xaml{,.cs}` (the ribbon button and its handler)
- `SAM_UI/SAM.Analytical.UI/Classes/PartO/PartORun.cs` (`AdoptOptimisationSettings` - the review round;
  the only change to an existing merged class, and it adds a transition rather than altering one)

Added - tests: `WPF/SAM.Analytical.UI.WPF.Tests/PartOWorkflowTests.cs` (52).

### Review round on PR #85 - two correctness findings, both fixed

**1. Iteration 2 silently became Iteration 1a where no catalogue could be read.**
`PartOWorkflowWindow.Request` built `SelectVentilationUnit` as
`scenario.SelectVentilationUnit && catalogue.HasSelectableProducts` - folding a **capability** into the
**intent**. A user who chose Iteration 2 on a machine with no readable catalogue got an Iteration 1a
request: no equipment selection, no Iteration 2B after it, and a run reported as successful. The request
now states the scenario alone; the missing catalogue is reported by the Equipment stage as the blocker it
already was, in the catalogue reader's own words, and Run is refused. The Prepare Iteration picker is
unaffected and unchanged - its tick is disabled and cleared without a catalogue, so no intent exists there
to lose.

**2. The Iteration 2B choice did not reach a reused preparation.**
`ReusePreparation` matches the ENGINEERING case and deliberately excludes
`PartOPreparationContext.OptimisationSettings` - the one field the analytical preparation neither reads
nor is affected by. But the reuse path skipped the preparation that used to record it, while
`Modify.CanOptimise` still read it off the run afterwards: ticking, unticking or retuning 2B and pressing
Run left the choice made at the earlier preparation.

Fixed with the **smallest correct write**, not by widening the match:
`PartORun.AdoptOptimisationSettings` (new) records the current choice on a `Prepared` run that has a
context, and `Modify.ReuseWithCurrentOptimisation` calls it on the reuse path only. Null clears it, so an
unticked 2B leaves nothing active. Widening `Reusable` to include the settings would rebuild the whole
analytical preparation - and on an isolated run re-derive its geometry and re-cut its adiabatic interfaces
- to change two numbers nothing in that preparation looks at. Where the run will not take the record it was
not a reuse target after all and the caller prepares again: correctness before saving a preparation.

**Nothing is written during inspection.** The status list reports what the run currently carries; the
record is written at the point the user commits to the request.

**3. A ticked Iteration 2B with unusable settings silently became no 2B.**
`PartOWorkflowWindow.OptimisationSettings` returns null for an unparseable step, an unparseable iteration
limit, or a pair `PartOOptimisationSettings.IsValid` refuses - and a null there is indistinguishable from
"the user did not ask for an optimisation". Nothing gated Run on it, so `Optimise == true` could coexist
with `OptimisationSettings == null`: the baseline ran a full-year TAS simulation having discarded the 2B
setup the user could still see ticked, and the follow-on was then unavailable on the run that had just paid
for it.

The window now carries `OptimisationRefusal` - the same property, with the same wording and the same
authority (`PartOOptimisationSettings.IsValid`), that the Prepare Iteration picker has always used to
refuse OK. It blocks Run in its own right and is stated twice: in the blocker line under the status list,
and beside the 2B fields themselves. **It is kept distinct from the model blockers**: the inspection stages
report what the model and the run provide, and a mistyped number in this dialog is neither, so the
inspection is unchanged and still reports the model as runnable. Nothing is silently unticked and nothing
invalid is silently converted to "no optimisation"; an unticked 2B is not validated at all and the baseline
runs.

**None of the three fixes changes analytical preparation or simulation behaviour**, so the three licensed
acceptance runs were not repeated. Fix 1 only alters a request that cannot reach a run (Run is blocked in
exactly that state) and is identical on every path that could previously execute; fix 2 writes
orchestration metadata that `PreparePartOIteration` does not read; fix 3 only prevents an invalid UI
request from starting a run at all.

### Tests and build

- `SAM.Analytical.UI.WPF.Tests`: **474 passed, 0 failed** (422 baseline + 52). The new class joins
  `WpfCollection`, as the guard test requires of any class with STA tests.
- The review round added **10**: three through `PartOWorkflowWindow` for the intent/capability split
  (Iteration 2 without a catalogue, Iteration 2 with one, 1a still distinct from 2), the three
  reuse/2B cases A/B/C, and four invariants - a changed 2B choice does not rebuild the preparation,
  inspecting writes nothing, only a prepared run with a context takes the record, and a preparation that is
  not reused is not written to. Each was confirmed **red** against the original code before the fix: the
  intent test on the folded expression, and A/B/C against a reuse path that records nothing.
- The 2B-validation round added **6**, all through `PartOWorkflowWindow`: an unparseable step, an
  unparseable iteration limit, a pair `IsValid` refuses (asserted against that authority's own sentence),
  the unticked case that must not be validated, the valid case that must still run and carry its settings,
  and the separation invariant - an unusable 2B leaves every model stage exactly as it was. Five of the six
  were confirmed **red** against head `7fdde247`; the sixth is the valid case, which was never blocked.
- `SAM_UI.sln` Debug (VS 18 MSBuild): **0 errors**, no new warnings in the changed files.
- `SAM`, `SAM_Tas`, `SAM_Systems`: untouched, not rebuilt.

### Licensed acceptance (2026-09-04) - PASSED

The named fixture `.../2026-07-15 PartO/000000_SAM_AnalyticalModel-It1a-futureZ1.sam` is on another
machine. The same model is present here as
`Documents/SAM_daily/2026-08-05-PartO/SAM_zoningAM-CIBSEfutureZ1.sam` - it loads with
`Name = 000000_SAM_AnalyticalModel-It1a-futureZ1`, 4 zones, 3 dwellings (Flat 1/2/3), 9 spaces, weather
`Z1_DSY1_2050s_HIGH90_CIBSE_v1.1`. Driven by a throwaway console harness (not in the repository) that calls
the production seams the command orchestrates, in the same order; the three modal dialogs were not clicked
by mouse, and `PartOWorkflowWindow` was instantiated and its reported state read.

| Run | Scenario / scope | TAS | TSD | TM59 | Optimise 2B after |
| --- | --- | --- | --- | --- | --- |
| A | 1a / all dwellings | full year, **1min56** | 18,356,724 B | assessed, 9 processed, 8 mechanical, **Fail** (future weather) | no (1a records no settings) |
| B | 1a / **Flat 1 in isolation** | full year, **1min13** | 4,884,006 B | assessed, **2 spaces** | no |
| C | **Iteration 2** + 2B ticked / all dwellings | full year, **1min44** | 18,361,878 B | assessed, 9 processed, 8 mechanical | **yes - `CanOptimise` becomes true** |

- **Status before the run**, run A: dwelling scope / TM59 mapping / Part F requirements all `READY`
  (8 of 8 spaces classify, 8 of 8 carry a continuous requirement), ventilation design
  `NEEDS PREPARATION`, model check `PENDING`, simulation and results `NOT RUN`, `CanRun=True`.
- **Reuse**: with the run prepared for the same request, ventilation design reports `REUSED` and
  `ReusePreparation=True`; after the workflow completes it is `NEEDS PREPARATION` again.
- **Isolation**: the prepared model is 2 spaces, one system, 30 l/s, named
  `...-ISO-8239b328`, with `PartOIsolationContext` naming Flat 1. Reopened, that `.sam` reports
  "This model is ALREADY the isolated thermal model of Flat 1".
- **Reopen without rerun**: each run's `<run>.sam` restored (`PartORun.Restore` = true) and reported
  simulation and results `READY` with "No new simulation is needed to review it". The redundant TAS
  `<run>.json` was removed on all three, as before.
- **Source model**: byte-identical afterwards on all three runs (length and last-write time), and the
  in-memory source model unchanged - checked for the isolated run in particular.
- **Restored runs correctly report `Optimise=False`**: reviewing is enough, resuming 2B is not.

### Issues, risks and remaining gaps

- **The three modal dialogs were not clicked by hand.** The workflow window, the preparation summary and
  the Simulate window are shown by the command; the harness drove the seams beneath them. Clicking the new
  ribbon button once by hand, at 100% and at 150% DPI, is worth doing before sign-off.
- **The 2B rounds themselves were not re-run** on this branch (10 full-year TAS runs). Nothing in the
  optimisation changed; `CanOptimise` becoming true after an Iteration 2 baseline was verified live.
- **Re-isolating an already-isolated model.** The hub loop makes it one click easier to reach the open
  Copilot comment from PR #82 (a duplicate isolation suffix when re-preparing an adopted result). Not
  fixed here - it is SAM's rule - but the dialog and the status line now say the loaded model is already an
  isolated extract, which is the warning that was missing.
- Out of scope by instruction and untouched: roof Adiabatic provenance (#95), `PanelType.Air` as an
  isolation cut, TAS HVAC ZoneGroup authority, historical duplicate TAS plant-zone cleanup.

### Exact recommended next step

Review the PR, then click **Simulate > Part O > Prepare & Run** by hand once on the acceptance fixture.
**Do not merge unattended.**

---

## Previous (2026-09-03, later still): the run summary states what EXISTS, not only what changed

**Status: root-caused, fixed and tested.** Reported from isolated-mode testing of Flat 1: the simulation
and the ventilation network were right, but the Results table was misleading.

### What was reported

Flat 1 has `Studio 1_0` at 30 l/s supply and 22 l/s extract, and `Bathroom_2` at 8 l/s extract. An
ordinary Iteration 2B round targets the studio's supply and the bathroom's extract. The detailed run
report confirmed the studio's 22 l/s extract was still present and the network balanced - but the Results
table for BASELINE and for the ordinary rounds listed only

```
Studio 1_0  | Supply
Bathroom_2  | Extract
```

so the studio's extract appeared to have disappeared. At MAX the table was already complete.

### Root cause

`PartOOptimisationAirFlowRow.Rows` built its rows from `TargetedAdjustments` and `DerivedAdjustments`
alone, and **an adjustment exists only where something changed.** A space and direction no round moved
contributed no adjustment, so it was absent from the step - and from the run - entirely. The reporting
layer could not print it because the run had never recorded it.

MAX was complete only incidentally: the capacity envelope grows the *whole* design vector proportionally,
so every space and direction gets an adjustment there. That is why the same table was right at MAX and
wrong everywhere else.

The baseline block had the same cause at one remove: it was synthesised from the union of every later
round's adjustments, so it could only ever show rooms some round eventually touched.

### The fix - the run records the vector, and the table prints it

`PartODesignAirFlowState` (new) is one space, one direction, its design airflow and its Approved Document
F requirement. `PartOOptimisationStep.DesignAirFlowStates` carries the complete set for that iteration,
and `Modify.Record` - the single place every step's evidence is written, for the baseline, every round and
the envelope alike - fills it from the model that step was assessed on.

It is scoped exactly as the unit table is, through the same `Query.PartOIterationAirHandlingUnits`: this
iteration's own units and the systems they supply. A room-direction with no terminal has no design airflow
to state and contributes no row - printing a zero would invent one.

`Rows` now walks that vector and classifies each entry against the step's adjustments:

| state | meaning |
|---|---|
| `TARGETED` | the round asked this room for a step |
| `DERIVED` | it moved to keep its dwelling balanced |
| `SCALED` | capacity envelope only - the whole vector grown proportionally |
| **`UNCHANGED`** | **it exists in the design and this step did not move it** |
| `BASELINE` | the baseline's own recorded vector |

On an `UNCHANGED` row Design before and Achieved are the same figure and Requested is a dash - nobody
asked. **An unchanged direction is never labelled TARGETED**, which would claim an engineering decision
that was never made.

### One representation, not two

The Results grid and its "Copy all" export read the same list - the export iterates
`dataGrid_AirFlow.ItemsSource`, which is `PartOOptimisationAirFlowRow.Rows(...)`. There was never a second
airflow representation to reconcile. The per-run file `Record` also saves is a `TM59AssessmentReport` - the
overheating compliance artifact - which is a different thing and carries no airflow vector.

### Backward safe

A step with no recorded vector - a run from before this existed, or one whose model could not be read -
still prints its adjustments exactly as before. Completing the table can never empty it. Pinned by
`ARunWithNoRecordedVector_StillPrintsItsAdjustments`, which also documents the old behaviour permanently:
in that run the studio's extract row is genuinely absent, and in the new one it is present.

### Not changed

Part F requirements, `DesignAirFlow`, equipment selection, `OperatingAirFlow`, the optimisation targeting
logic and the TAS air movements. This adds a read-only record and a presentation rule; it decides nothing.

### Files changed

- `SAM_UI/SAM.Analytical.UI/Classes/PartO/PartODesignAirFlowState.cs` - **new.**
- `SAM_UI/SAM.Analytical.UI/Classes/PartO/PartOOptimisationStep.cs` - `DesignAirFlowStates`.
- `WPF/SAM.Analytical.UI.WPF/Modify/OptimisePartOTM59.cs` - `DesignAirFlowStates(...)`, called from `Record`.
- `WPF/SAM.Analytical.UI.WPF/Classes/PartO/PartOOptimisationAirFlowRow.cs` - the vector-driven rows.
- `WPF/SAM.Analytical.UI.WPF.Tests/PartOResultsVectorTests.cs` - **new**, 11 tests.

### Validation

- `dotnet test WPF/SAM.Analytical.UI.WPF.Tests` - **421 passed** (410 baseline + 11).
- `dotnet build SAM_UI.sln -c Debug` - **0 errors**.

### Not performed

No licensed TAS run and no run on the real Flat 1 model. The fixture reproduces the reported figures
exactly, but the acceptance check on the real project is still worth doing.

## Previous (2026-09-03, later still): the intermittent WPF test-host failures

**Status: reproduced, root-caused, fixed and validated over 200 repeated suite runs.** Test infrastructure
only - no production code changed, and nothing about the Part O isolation workflow is touched.

### What was happening

Repeated runs of the SAM_UI suite failed intermittently - roughly one run in six - always with the same
exception and never on a rerun of the same test:

```
System.NullReferenceException
  at System.IO.Packaging.PackagePart.IsStreamClosed(Stream s)
  at System.IO.Packaging.PackagePart.CleanUpRequestedStreamsList()
  at System.IO.Packaging.PackagePart.GetStream(FileMode mode, FileAccess access)
  at System.Windows.Application.LoadComponent(Object component, Uri resourceLocator)
  at SAM.Analytical.UI.WPF.PartOIterationWindow.InitializeComponent()
```

The failing tests spanned `PartOIsolationScopeTests`, `PartODwellingSelectionTests` and
`PartOPresentationTests`, in varying combinations of one to seven tests per bad run.

### Root cause

The exception is thrown inside WPF's own component loader, before any SAM code on the stack runs.

`Application.LoadComponent` reads the compiled XAML through one `System.IO.Packaging.PackagePart` **per
resource URI**. That part records the streams it has handed out in a plain `List<Stream>`, and both
`GetStream` and the `CleanUpRequestedStreamsList` it calls mutate and enumerate that list with no lock.
Two threads inside it at once leave one of them reading a slot the other has already removed, so
`IsStreamClosed` is handed a null and dereferences it.

Two threads get in there because of xUnit's default: **one collection per test class, and collections run
in parallel** - on this 16-core machine, up to 16 at a time. Nothing in the repository overrode it; there
was no `xunit.runner.json`, no assembly-level `CollectionBehavior`, and no `[Collection]` attribute
anywhere in the suite.

Three separate test classes each construct a `PartOIterationWindow`, so three collections could call
`LoadComponent` for **the same** compiled XAML - therefore the same `PackagePart` - simultaneously.

The distribution of failures is the confirmation. Every single failure across every reproduction was
`PartOIterationWindow`; `ZoneControl` - loaded by `ZoneDwellingTests`, and by no other class - never
failed once. A control shared by three collections races; a control owned by one class cannot, because
tests within a class already run sequentially.

### It is a test-host defect, not a product one

Production loads each component once, from the single STA thread that owns the UI:
`Modify.PreparePartOIteration` is the only construction site of `PartOIterationWindow` in the codebase.
The one piece of production parallelism nearby, the `Parallel.For` in `Modify.RunWorkflow`, runs
calculations and marshals progress text; it loads no components. There is no production path on which two
threads load one component, so nothing here needed fixing outside the test project.

### The fix

`WPF/SAM.Analytical.UI.WPF.Tests/WpfCollection.cs` declares one xUnit collection,
`"WPF component loading"`, and the four classes that instantiate a WPF component join it:
`PartOIsolationScopeTests`, `PartODwellingSelectionTests`, `PartOPresentationTests` and
`ZoneDwellingTests`.

Tests in one collection never run concurrently with each other, so no two component loads can overlap.
Every other class in the assembly keeps running in parallel exactly as before - the suite is ~0.15 s
slower. `ZoneDwellingTests` joins although it has never failed: it is immune only by the accident of
being the sole loader of its control, which is not a property worth relying on.

Rejected alternatives: assembly-wide `CollectionBehavior` parallelism control (serialises 27 unrelated
classes to fix 4), and any form of retry, sleep or delay (would hide the race rather than remove it).

`WpfCollectionTests.EveryClassWithStaTests_IsInTheWpfCollection` keeps the membership honest. A class
that instantiates a WPF component must carry a StaFact-family attribute to get an STA thread, so that
attribute is a sound marker for "this class loads BAML"; the test reflects over the assembly and fails,
naming the class, if such a class is not in the collection. It was confirmed to fail by removing the
attribute from `ZoneDwellingTests`, and the guard named exactly that class.

### Evidence

The smallest useful group - the four WPF classes alone - **passed 40/40** and did not reproduce it: with
only four collections they reach their WPF tests at different moments. The race needs the scheduling of
the full suite.

| Configuration | Failed runs |
|---|---|
| Full suite, default parallelism, **before** the fix | **5 / 60** (bad runs failed 1, 1, 1, 7 and 7 tests) |
| Full suite, `xUnit.ParallelizeTestCollections=false`, before the fix | **0 / 60** |
| Full suite, default parallelism, **after** the fix | **0 / 200** |

The serialised control run is what proves parallel execution is necessary to the failure rather than
merely correlated with it. 200 clean runs at the observed 8.3% per-run rate is not a proof of zero
flakiness, but it is a ~4e-8 outcome if the race were still present at its previous rate.

### Files changed

- `WPF/SAM.Analytical.UI.WPF.Tests/WpfCollection.cs` - **new.** The collection definition and its guard test.
- `WPF/SAM.Analytical.UI.WPF.Tests/PartOIsolationScopeTests.cs` - `[Collection(WpfCollection.Name)]`.
- `WPF/SAM.Analytical.UI.WPF.Tests/PartODwellingSelectionTests.cs` - `[Collection(WpfCollection.Name)]`.
- `WPF/SAM.Analytical.UI.WPF.Tests/PartOPresentationTests.cs` - `[Collection(WpfCollection.Name)]`.
- `WPF/SAM.Analytical.UI.WPF.Tests/ZoneDwellingTests.cs` - `[Collection(WpfCollection.Name)]`.

### Validation

- `dotnet test WPF/SAM.Analytical.UI.WPF.Tests` - **Passed 411, Failed 0, Skipped 0** (410 + the guard).
- The 200-run repetition above, all green.
- `dotnet build SAM_UI.sln -c Debug` - **0 errors**.

---


## Previous (2026-09-03, later): SAM Check as a mandatory Part O pre-simulation gate

**Status: implemented, tested, and validated on the licensed acceptance model.** The intended order is
now **prepared model → `SAMAnalytical.Check` → TAS conversion → TAS simulation**; this is the Check step.

### Why

TAS runs its own pre-simulation check and refuses models that fail it. Several of the things it refuses
for are properties of the SAM model, settled long before any TBD exists. Discovering them in TAS means
waiting through a geometry conversion - 40 s on the licensed acceptance model, far longer on a block of
flats - to be told something knowable before it started, in TAS's words rather than in terms of the SAM
object at fault. On the run that prompted this, TAS's words were `Simulation Failed` and nothing else.

Two such source-model defects are fixed in `SAM` `feature/parto-isolated-dwellings` (a transposed
humidistat pair, and an isolated unit that lost the relations to the air it moves) and both are now
`Create.Log` rules. This is the gate that runs them. See that repository's `PROJECT_PROGRESS.md`.

### Where the gate is

`Modify.RunPartOSimulation`. **Every** Part O TAS simulation comes through that one method, so one place
is the whole gate and no Part O path can be added later that quietly skips it: a full run, an isolated
run, Iteration 1a, 1b and 2, every Iteration 2B round, and the capacity envelope diagnostic.

**There is no second validation framework.** `PartOPreSimulationCheck` runs
`SAM.Analytical.Create.Log` - the same authority behind the `SAMAnalytical.Check` component and the Check
command - and makes one decision from it.

### It judges the normalized model (Codex P1 on PR #82)

The gate first ran on `analyticalModel` as handed in, which is **not** what TAS is given. The default
material repair fixes exactly the "Material Library does not contain Material X" state `Create.Log`
reports as an **Error**, so gating before it would refuse models the pipeline was about to make valid -
and a defect that step or `UpdateConstructionLayersByPanelType` introduced would have been invisible.

Both normalization steps were therefore **hoisted out of the progress dialog** to immediately after the
private simulation copy, and the gate runs after them and before the gbXML/TBD. Neither is a long COM
call, which is what the dialog exists to keep responsive.

### Scoped to a prepared Part O run, deliberately

`PartOPreSimulationCheck.Gate` returns a check only for a run in `PartORunState.Prepared` - exactly "the
first TAS simulation of a prepared Part O run" and every re-prepared one after it. `Simulate` arms this
with the session's run, and both optimisation call sites call `PartORun.Prepare` immediately before
simulating.

**Not** the ordinary Simulate command, which reaches the same method with no run or an unprepared one.
Making SAM Check a hard gate on *every* TAS simulation in SAM is a larger change than the Part O
contract: `Create.Log` reports Errors on states a long-standing model may well carry, and a model that
simulates today must not stop simulating because the Part O path grew a gate. The Check command remains
available to any model on demand.

### Errors stop the run; warnings do not

`LogRecordType.Error` is the only fatal level, exactly as `Create.Log` already assigns it.

- **Fatal** → the `LogWindow` shows the whole log (type, name and Guid of every record), the refusal the
  caller shows says the pre-simulation validation failed and that TAS was not started, and the list is
  capped so a model with hundreds of defects still produces a readable message. No gbXML, no TBD, no
  workflow.
- **Warnings** → carried into the run's `notes`, deliberately not shown as a dialog: a Part O model has
  intentional warnings on every run, and a dialog per run trains people to dismiss the one that mattered.

Nothing promotes a warning, and that is load-bearing. A Part O model deliberately leaves the generated
MVHR plant zone inactive on the HDD and CDD design daytypes, and TAS says so
(`Zone 'MVHR-01' is missing internal conditions on some daytypes`). Pinned on the SAM_Tas side in
**SAM-BIM/SAM_Tas#46**.

### Important decisions and assumptions

- Passing means the model carries no model-validity defect this Check knows how to detect. **Not** that
  TAS will run: licensing, file I/O, the solver and weather data can still fail, and TAS's own check knows
  rules this one does not. Where TAS refuses for a deterministic problem SAM could have seen, the answer
  is to fix the source-model defect and add the missing `Create.Log` rule - not to weaken the gate.
- On an isolated run the model checked is the **derived** isolated one, because
  `Analytical.Modify.PreparePartOIteration` applied the isolation and the run adopted its output. The gate
  never looks for another model than the one it is handed - pinned in both directions.
- A model that could not be checked at all (none supplied) is **not** valid: "nothing was checked" must
  never read as "nothing was wrong".

### Files changed

- `SAM_UI/SAM.Analytical.UI/Classes/PartO/PartOPreSimulationCheck.cs` - new. The check, the `Gate`
  decision, and the refusal report.
- `WPF/SAM.Analytical.UI.WPF/Modify/RunPartOSimulation.cs` - normalization hoisted ahead of the gbXML,
  then the gate.
- `WPF/SAM.Analytical.UI.WPF.Tests/PartOPreSimulationCheckTests.cs` - new, 18 tests.

### Tests, builds and validation

- Full `SAM.Analytical.UI.WPF.Tests` **410 passed**, 0 failed. `SAM_UI.sln` builds clean (VS 18 MSBuild).
- **Licensed TAS acceptance run**, Flat 1 isolated out of
  `000000_SAM_AnalyticalModel-It1a-futureZ1.sam`: the gate reports **0 errors and 0 warnings** on the
  derived isolated model, the full year runs (TSD **4,691,076 bytes**, against a 22,414-byte stub and
  `Simulation Failed` before), only Flat 1's two spaces are simulated, and the source `.sam` is
  byte-identical afterwards - timestamp included.

### Unresolved issues, risks and blockers

- **Depends on `SAM` `feature/parto-isolated-dwellings`; merge `SAM` first.** The gate is only useful with
  the `Create.Log` rules that PR adds, and the acceptance case needs its `IsolateSpaces` fixes.
- The gate is Part O only. Whether SAM Check should gate *every* TAS simulation is an open product
  question, deliberately not decided here; it is a one-line change to `PartOPreSimulationCheck.Gate`.
- Copilot's PR #82 comment about a duplicate isolation suffix when re-preparing an adopted optimisation
  result is **open and unaddressed**.

### Exact recommended next step

Wait for the re-requested Codex review on **SAM-BIM/SAM_UI#82**, then decide on the open Copilot comment.
**Do not merge** - merge order is SAM #92, then this, then SAM_Tas #46 (order free).

---

## Previous (2026-09-03): running selected dwellings in isolation

**Status: implemented and tested. Iteration 2B engineering/orchestration unchanged.**

The engineering - the extraction, the adiabatic cut, the shading context, the plant refusals and the
persisted isolation record - is `SAM`'s; see that repository's `PROJECT_PROGRESS.md`. What changed here
is the workflow around it.

### 1. The control

`PartOIterationWindow` gains **"Run selected dwellings in isolation (thermal model scope only)"**, with
the dwelling scope controls, **off by default**. The whole-building simulation is the reference case;
trading it for speed is a deliberate act.

The note under it states the assumption where the choice is actually made: interfaces to excluded spaces
become adiabatic, surrounding external geometry is retained as shading context, results may therefore
differ from a whole-building simulation, and none of it changes the Part O criteria or the Part F
requirements.

**Offered, not disabled, when every dwelling is ticked.** A large building is not only its dwellings -
selecting all of them still excludes corridors, cores, plant and commercial areas, which on a 5,000-space
model is a real reduction, so disabling the tick would refuse a genuine saving on a false premise. The
note says where the remaining saving would come from instead.

Selection stays guid-based and there is still exactly one row per dwelling, never one per space.

### 2. Orchestration

`Modify.PreparePartOIteration` passes the tick straight through to
`Analytical.Modify.PreparePartOIteration`, which is where all the engineering happens. It chooses nothing
of its own. A refusal - shared system, shared unit, cross-cut air movement - comes back as the
preparation's `Refusal` and is shown, and the run dropped, **before** any conversion starts.

`PartOPreparationContext.Isolated` records that the run was asked to isolate. It is deliberately
*recorded and not re-applied*: an Iteration 2B round re-prepares the model the previous round left
behind, and that model is already isolated. Re-isolating would rebuild the cut and the shading context on
a model that already has them, the geometry would no longer be what the canonical TBD was converted from,
and the warm start would switch off for every round.

### 3. Artifact naming

`Query.ProjectName_Isolated` (new) - `<project>-ISO-<token>` - is the one naming authority. Every Part O
artifact derives from the project name, which the Simulate dialog defaults from the model's name, so
applying the suffix to the prepared model's name carries the isolated identity to the TBD, the TSD, the
`.sam` and the TM59 report through the path that already existed.

The token is the scope token from `PartOIsolationContext`, a function of the selected space guids, so a
full run and an isolated run cannot overwrite one another, two different selections cannot either, and
two dwellings sharing a display name are still told apart. Applied once, so a 2B round's name does not
grow a suffix per round.

**Naming only.** Nothing reads isolation state back out of a path; the stamped context is the authority.

### 4. Warm start

Already correct by construction, and now proven. A canonical TBD is created by run 0 of one optimisation
and used only by that optimisation's own rounds - it is never written to disk as reusable state - so a
full-building canonical can never reach an isolated run, and one isolated scope's canonical can never
reach another's. `PartOCanonicalTBD`'s fingerprint independently catches it: it enumerates every space,
zone, panel and aperture identity, and those differ between scopes.

One real gap closed: the fingerprint did **not** cover `PanelParameter.Adiabatic`, which the conversion
*does* read (`SAM_Tas Modify.UpdateAdiabatic` nulls the matching TBD surface link). Two models identical
but for that flag convert to different TBDs, and the isolation cut is expressed entirely through it. It
is now in the fingerprint. An isolated baseline is still reused by its own rounds, because design airflow
remains deliberately absent from it.

### 5. Reporting and persistence

`PartOTM59Assessment` reads the isolation context off the design model and sets
`TM59AssessmentReport.ThermalModelScope`, so the report header states
`Thermal model scope: ISOLATED. Selected dwellings: ...` together with the adiabatic caveat - and a
restored review states the same scope as the run that produced it, without either consulting a filename.
A whole-building run prints `WHOLE BUILDING`.

The preparation summary states the scope first, before the route and the duty, so it is read before a
long run is started rather than after.

The evidence family is unchanged: `<run>.sam`, `<run>.tbd`, `<run>.tsd`, `<run>-TM59.txt`, no redundant
workflow JSON. The isolation context rides in the `.sam` with the provenance and the scenarios, so
Results -> Overheating on a reopened isolated run works without a TAS rerun under exactly the same
provenance rule as before, shows only the dwellings that were simulated, and remains review-only.

### Tests

`PartOIsolationScopeTests` - 14 tests: the tick's default and its wording, the naming authority
(full vs isolated, scope A vs scope B, idempotence, untouched full-run name), the three warm-start rules
plus the adiabatic-flag gap, the isolation context surviving a real `.sam` archive written under a
deliberately unrelated filename, a full run carrying no context at all, and the preparation context flag.

Full `SAM.Analytical.UI.WPF.Tests`: **392 passed, 0 failed**. `SAM_UI.sln` MSBuild (VS 18): **0 errors**.

## Previous: result reopening, report persistence, command enablement

**Status: implemented and tested. Core Iteration 2B engineering/orchestration unchanged (accepted).**

The engineering seams for the naming defect and the "TM59 Application = `-`" defect were both in `SAM`,
not here - see that repository's `PROJECT_PROGRESS.md`. What changed here is the workflow around them.

### 1. Reviewing a saved run without re-simulating

`Modify.RunPartOSimulation` now stamps the model the workflow returns with the overheating scenarios it
was prepared with and with the results it was produced from (`SimulationResultProvenance`, constructed
*after* the scenarios so it fingerprints both), and writes that model beside the TBD as the run's own
`<project>.sam`. Only the full-year path stamps: a partial or sizing-only run writes nothing, exactly as
it cannot complete a Part O run.

**The per-run model artifact is `.sam`, not `.json`.** `Query.Path_PartORunModel` (new) is the one naming
authority - `<the run's own TSD>` -> `<same name>.sam`, beside it - and `RunPartOSimulation` writes
through it with `Core.Convert.ToFile(..., SAMFileType.SAM)`, SAM's **native** model writer (a compressed
archive), the same one Save As uses. Not JSON text under a `.sam` name: the test asserts the file's `PK`
archive signature. It reads back through `Core.Convert.ToSAM`, which the Open command already uses and
which already offers `*.sam` first, so nothing in the reopen path needed a special case. Only the file at
`Path_PartORunModel` carries the provenance a review validates against; the TAS workflow's own
`<project>.json` is now removed on the Part O path once that file is written - see section 5. Run evidence
is `<run>.sam` / `<run>.tbd` / `<run>.tsd` / `<run>-TM59.txt` plus the timings, for the baseline, each
`-OptNN` round and `-OptMax` alike, and no second copy of the model as plain text.

`PartORun.Restore` (new) is the second way into `WorkflowCompleted`, and deliberately narrower than
`Complete`. It reconnects a reopened model to the results it records, validated by the provenance rule -
never by filename, never by an `Opt01`/`OptMax` pattern, never by a nearby TSD.
`AnalyticalWindow.UIAnalyticalModel_Opened` calls it once, after `Reload`.

**The scenarios are read, never regenerated.** What `Restore` loads is exactly the collection persisted
with that run, and `SimulationResultProvenance.Fingerprint_OverheatingScenarios` is what proves it is the
collection the results were assessed under - the final-review blocker. Nothing infers a scenario from a
room name, a zone layout or anything else on the reopened model. An incomplete record - missing the TSD
length, the write time, the design fingerprint or the scenario fingerprint - is refused wholesale rather
than validated on the fields it happens to carry.

**Review, never resume.** A restored run holds no `PreparationContext` and no `SimulationContext` - those
describe how *this session* prepared and ran the case, and a file cannot reproduce them. `IsRestored`
marks it, and `Modify.CanOptimise` refuses it on the missing preparation context, not on the flag. So a
reopened run can be assessed but never resumed into Iteration 2B. That distinction is the point.

### 2. A durable TM59 report per run

`Query.Path_TM59Report` is the **one naming authority**: `<the run's own TSD>` -> `<same name>-TM59.txt`,
beside it. It inherits the per-iteration TSD naming
(`PartOSimulationContext.ProjectName_Iteration`), so baseline, `-Opt01`, `-Opt02` ... `-OptMax` each land
at their own path and none can overwrite another. `Modify.SavePartOTM59Report` writes it best-effort - a
locked or read-only path is reported, never thrown, because the assessment already succeeded.

Both writers go through it: `Modify.AssessPartOTM59` (the interactive command) and
`Modify.OptimisePartOTM59.Record`, which is called at all three points that assess - baseline, each round,
and the capacity envelope.

### 3. Results-tab command enablement, made explicit

| Command | Previously | Now |
| --- | --- | --- |
| Results -> Overheating | `PartORun.CanAssess`, i.e. a workflow completed **in this session** | `PartORun.CanAssess` - now reachable either by this session's workflow **or** by `Restore` from a reopened model with valid provenance |
| Results -> Optimise (2B) | `CanAssess` + prepared with optimisation settings + a ventilation-unit catalogue | unchanged, and a restored run fails it on the null `PreparationContext` |

`RefreshPartOButtons` stays a **pure state read** - no filesystem, no deserialization - on every ribbon
refresh. `Modify.CanOptimise` and `PartORun.IsAssessable` remain the real gates and re-check the results
file at invocation. Tooltips now distinguish the restored case in both directions.

### 4. Report wording

`Modify.Summary` said "Assessed 9 space(s) ... 8 mechanical ventilation result(s)" of a run where one of
the nine (a corridor) was processed but explicitly not assessed. It now reads "Processed 9 simulated
space(s) ... 8 assessed (...), 1 not assessed", counting the assessed by distinct result reference rather
than assuming one result per space. Extracted to an internal overload so the wording is pinned by test
without constructing a `TM59AssessmentResult`.

### 5. One persisted run model per run - the redundant TAS JSON is removed

The `.sam` change above was made to stop keeping the run model twice, but on its own it did not: TAS's
`WorkflowCalculator` ends **every** run with a "Saving Model" step that writes the returned model as plain
JSON at its `Path_TBD`'s directory and base name, so a Part O run was producing both `<run>.json` and
`<run>.sam` - the same model, once as a compressed archive carrying the provenance a review is validated
against, once as a very large text file carrying it too but never read, for the baseline and for every
optimisation round.

`Modify.PersistPartORunModel` (new) is the Part O seam that fixes it, and it owns the whole rule rather
than half of it, because **the ordering is the safety property**:

1. write the stamped model to `Query.Path_PartORunModel` through `Core.Convert.ToFile(..., SAMFileType.SAM)`;
2. **if that fails, delete nothing** - the workflow's JSON stays as the only remaining copy of the model,
   and the failure is a note. A persistence failure must never become the loss of the run model;
3. only then remove `Query.Path_PartOWorkflowJson` (new) - the one exactly-known file, derived from *this
   run's own TBD* exactly as `WorkflowCalculator` derives it. No directory sweep, no filename scanning, so
   a baseline's cleanup cannot touch `-Opt01`'s file;
4. **if that removal fails** - the file is locked, read-only - the run is still a success. The `.sam` is
   written and authoritative; the leftover is reported as a note and nothing else changes. Failing a
   completed simulation and its assessment over a locked leftover would be the far worse answer.

`WorkflowCalculator` itself is **not modified**. Ordinary non-Part-O TAS runs in SAM write and keep their
`<project>.json` exactly as before; the removal happens only in the Part O orchestration, only after the
native write has succeeded, and only to that run's own file.

Artifact set for a successful Part O run, baseline and each `OptNN` / `OptMax` independently:
`<run>.sam`, `<run>.tbd`, `<run>.tsd`, `<run>-TM59.txt`, timings - and **no** `<run>.json`.

### Files

| File | Change |
| --- | --- |
| `SAM_UI/SAM.Analytical.UI/Classes/PartO/PartORun.cs` | `Restore`, `IsRestored`. |
| `WPF/SAM.Analytical.UI.WPF/Modify/RunPartOSimulation.cs` | Stamps scenarios + provenance; persists the run model through `PersistPartORunModel`. |
| `WPF/SAM.Analytical.UI.WPF/Modify/PersistPartORunModel.cs` | New - writes the native `.sam`, then removes the redundant TAS JSON; owns the fail-safe ordering. |
| `WPF/SAM.Analytical.UI.WPF/Query/Path_PartORunModel.cs` | New - the one run-model naming authority; states the `.sam` extension. |
| `WPF/SAM.Analytical.UI.WPF/Query/Path_PartOWorkflowJson.cs` | New - the one authority for the TAS workflow's own `<run>.json`, derived from the run's TBD. |
| `WPF/SAM.Analytical.UI.WPF/Query/Path_TM59Report.cs` | New - the one report-naming authority. |
| `WPF/SAM.Analytical.UI.WPF/Modify/SavePartOTM59Report.cs` | New - best-effort persistence. |
| `WPF/SAM.Analytical.UI.WPF/Modify/AssessPartOTM59.cs` | Saves the report; processed/assessed wording. |
| `WPF/SAM.Analytical.UI.WPF/Modify/OptimisePartOTM59.cs` | `Record` saves each step's report; `UnitStates` scoped. |
| `WPF/SAM.Analytical.UI.WPF/Windows/AnalyticalWindow.xaml.cs` | Restore on open; enablement tooltips. |
| `WPF/SAM.Analytical.UI.WPF.Tests/PartOResultReopenTests.cs` | New - 17 tests. |
| `WPF/SAM.Analytical.UI.WPF.Tests/PartORunModelPersistenceTests.cs` | New - 8 tests, pinned at the Part O orchestration seam rather than over TAS internals. |

### Validation

- `SAM.Analytical.UI.WPF.Tests`: **373 passed, 0 failed** (`PartOResultReopenTests`: 17;
  `PartORunModelPersistenceTests`: 8).
- Full `SAM_UI.sln` Debug build: 0 errors.
- `SAM.Tests`: 1750 passed, 0 failed; `SAM.sln` Debug: 0 errors. **`SAM` is unchanged by the JSON-cleanup
  round** - the seam is entirely in `SAM_UI`.

`PartORunModelPersistenceTests` pins: the successful run leaves a real `.sam` archive (`PK` signature) and
no `.json`, with the TBD and TSD untouched and nothing else swept up; baseline / `Opt01` / `Opt02` /
`OptMax` each name their own JSON, sibling of their own model; a baseline's cleanup leaves `Opt01`'s file
alone; with the JSON gone the model still reopens, restores, validates both fingerprints, reaches Results
-> Overheating and resolves after a folder move; a `.sam` write that fails keeps the JSON and reports;
a JSON that cannot be deleted (held with `FileShare.None`) still returns success with the `.sam`
authoritative and a note; no JSON and no model are each handled without a spurious complaint.

Tests added in the final-review round: `ScenariosSwappedUnderUnchangedResults_IsRefused` (the blocker -
the design fingerprint and the results file are assertion-checked as *unchanged*, so the refusal is
attributable to the scenario half alone), `AnIncompleteRecord_IsRefused` (each required field removed in
turn, at the `Restore` seam), `EachRun_ModelIsItsOwnSamFile` and
`EachRun_ResolvesItsOwnCompleteArtifactSet` (baseline / `Opt01` / `Opt02` / `OptMax` each resolve their
own `.sam`, `.tsd` and `-TM59.txt`), and `TheRecord_SurvivesTheModelBeingSavedAndReopened` reworked to
round-trip a real `.sam` archive and assert both fingerprints deterministic across it. The folder-move
and lookalike tests now open `.sam` models.
- One pre-existing flake fixed: `ARewrittenResultsFile_IsRefused` simulated a rewrite by writing twice in
  quick succession, and the two writes can land in one filesystem timestamp tick with the same length -
  indistinguishable to the rule under test. The rewrite is now stated explicitly (different length, later
  write time). A rewrite that genuinely matches both recorded values is the documented limit of the rule.
- The licensed 10-round future-weather acceptance was NOT re-run: no engineering semantics changed.

### Not done / watch items

- **A model saved before this round cannot be reviewed**, and this is deliberate. Verified against the
  user's own `SAM_daily/2026-08-05-PartO` output: the baseline, `Opt01`, `Opt02` and `OptMax` JSONs all
  report `NONE (legacy)` for both provenance and scenarios. Without the recorded scenarios there is no
  authoritative ventilation strategy per space, and supplying one would be the guess the design forbids.
  Those files keep the ordinary "prepare and simulate" guidance. **Session-2 review must be retested with
  a run produced by this build.**
- The preparation summary window's space table still lists every space of the prepared model. Display
  scope only; deliberately left as-is.
- Manual UI check of the dialog at 125%/150% DPI is worth one look before sign-off.

## Previous (2026-09-02): Iteration 2B user-testing fix round

**Status: implemented and tested. Core Iteration 2B engineering/orchestration unchanged (accepted).**

Four user-testing findings fixed in one pass, all in `WPF/SAM.Analytical.UI.WPF`:

1. **"Dwellings in scope" is now a real selector.** The list showed the policy's answer but selection was
   cosmetic. Now: `PartODwellingSelection` (new, `Classes/PartO/`) holds one lightweight record per eligible
   dwelling zone - discovered ONCE through `Query.PartFDwellingZones` (unchanged authority), all selected by
   default, identity by zone `Guid` so duplicate names stay distinct. The window's `Zones_Dwelling` now
   returns the SELECTED zones, which flows unchanged into `Modify.PreparePartOIteration` and thereby into
   `PartOPreparationContext.Zones` - so preparation, Iteration 2 selection and 2B targeting all run over the
   user's subset. Checkbox list (virtualized `ListBox`, state on the record not the row), search box
   (case-insensitive substring over the already-discovered names; filtering never loses selection state),
   Select All / None acting on the search-matched set, OK disabled on an empty selection. No per-space
   controls; no model traversal per UI event.
2. **Dialog layout.** Was fixed 580px / `ResizeMode=NoResize`, cramped when 2B expands. Now
   `SizeToContent="Height"` (auto-grows when 2B options are enabled), `ResizeMode="CanResizeWithGrip"`,
   MinHeight/MinWidth, `MaxHeight = 92% of the work area` (set in code, DPI-safe), content in a
   `ScrollViewer` with OK/Cancel in a fixed row outside it - always reachable.
3. **MVHR plant-zone warnings excluded.** `SAM.Analytical.Tas.Modify.UpdateIZAMs` builds one TAS zone per
   AHU (named after it) as the unit's simulated plant zone; it comes back in the TSD and resolved to no
   design space, producing a "does not resolve to exactly one design space" refusal every round. New
   `Query.PartOPlantZoneSpaces` identifies them POSITIVELY (unresolved through the `SimulationSpaceMap` AND
   named after a design-model AHU, normalised as the export's own zone lookup is);
   `PartOTM59Assessment.WithoutPlantZoneSpaces` removes them from the converted TSD model copy before the
   calculator runs. They are no longer restored or assessed and no longer warn. A genuinely unresolved room
   still refuses - pinned by tests over the production seam. Both callers (interactive assessment and 2B)
   go through `PartOTM59Assessment.Assess`, so both are covered. No change in SAM or SAM_Tas.
4. **Equipment evidence scoped.** `Modify.OptimisePartOTM59.UnitStates` reported every AHU in the model,
   including the fixture's authored legacy `AHU1 | MV 1` system (NaN/NaN, no product). New
   `Query.PartOIterationAirHandlingUnits` scopes by RELATION, never name: a unit is in scope iff a system
   bound to it (`SupplyUnitName`, the standard binding) is connected to at least one design
   `VentilationTerminal` (a relation only the Part O preparation creates) AND serves a space of the run's
   dwelling zones (by guid). The model is not edited; the report is scoped.

### Files

| File | Change |
| --- | --- |
| `WPF/SAM.Analytical.UI.WPF/Classes/PartO/PartODwellingSelection.cs` | New - the selection model. |
| `WPF/SAM.Analytical.UI.WPF/Windows/PartOIterationWindow.xaml{,.cs}` | Selector UI + layout rebuild. |
| `WPF/SAM.Analytical.UI.WPF/Query/PartOPlantZoneSpaces.cs` | New - positive plant-zone identification. |
| `WPF/SAM.Analytical.UI.WPF/Classes/PartO/PartOTM59Assessment.cs` | Exclusion seam (one map, built once). |
| `WPF/SAM.Analytical.UI.WPF/Query/PartOIterationAirHandlingUnits.cs` | New - relational equipment scope. |
| `WPF/SAM.Analytical.UI.WPF/Modify/OptimisePartOTM59.cs` | `UnitStates` scoped. |
| `WPF/SAM.Analytical.UI.WPF.Tests/PartODwellingSelectionTests.cs` | New - 11 tests. |
| `WPF/SAM.Analytical.UI.WPF.Tests/PartOPlantZoneTests.cs` | New - 8 tests. |
| `WPF/SAM.Analytical.UI.WPF.Tests/PartOIterationEquipmentTests.cs` | New - 4 tests. |

### Validation

- `SAM.Analytical.UI.WPF.Tests`: **348 passed, 0 failed** (was 325; +23 new). No TAS licence needed.
- Full `SAM_UI.sln` Debug build: 0 errors.
- Environment note: the SAM and SAM_Tas `build/` DLLs were stale against their sources (post-merge);
  rebuilt SAM.Analytical with `dotnet build` and SAM_Tas.sln with Framework MSBuild (COM refs). No source
  changes in either repo; both working trees remain clean.
- The licensed 10-round future-weather acceptance was NOT re-run: no engineering semantics changed, and the
  new seams are pinned by the regression tests above.

### Not done / watch items

- The preparation summary window's space table still lists every space of the prepared model (including
  unselected dwellings' rooms, whose terminals are realized whole-model and stay inert). Display scope only;
  the engineering scope is the selected dwellings. Deliberately left as-is.
- Manual UI check of the dialog at 125%/150% DPI is worth one look on the real model before sign-off.

## Previous: warm starting each round from the canonical TBD (2026-09-02, merged)

**Status: implemented, tested, and proven equivalent to the full path on the licensed model.**

### Why - measured, not assumed

Between 2B rounds only the ventilation state changes. The licensed acceptance run's own timing CSV shows
what re-converting the rest costs: of a **64.2 s** round on `SAM_zoningAM-CIBSEfutureZ1.sam`, the
gbXML/T3D/shading conversion is **41.6 s** and the full-year TAS simulation is **3.6 s**. Ten more rounds
of conversion is most of the wall clock of a 2B run, and none of it is physics.

### Measured result

| | full conversion | warm start |
| --- | --- | --- |
| run 0 (the conversion itself) | 71.1 s | 71.1 s - unchanged, it *is* the baseline |
| each later round | ~64 s | **21.0 / 23.2 / 21.9 s** |
| of which full-year simulation | 3.6 s | **4.0 s - still a real one** |
| copy of the canonical TBD | - | 4.1 ms |

A warm round's step list is the full one **minus exactly nine conversion steps** - `Opening TBD file`,
`Updating Weather Data`, `Updating HDD and CDD Day Types`, `Opening T3D file`, `Importing gbXML`,
`Updating T3D file`, `T3D to TBD -> Shading`, `Reusing Aperture Definitions`, `Updating Aperture Types` -
and nothing else. `Updating Ids`, `Updating Zones`, `Add IZAMs`, `Simulating Model` and `Adding Results` all
still run.

### Architecture

```
run 0     full production conversion -> canonical TBD + its own TSD   (unchanged)
round N   canonical TBD -> copy to <project>-OptNN.tbd
          -> the current round's complete ventilation state re-applied
          -> full-year TAS -> <project>-OptNN.tsd
          -> workflow-returned AnalyticalModel -> production TM59
```

- **Always cloned from run 0, never from the previous round.** `Run0 -> Opt01`, `Run0 -> Opt02`. Chaining
  would accumulate stale state, which is the whole failure mode being avoided.
- **The clone is `SAM_Tas`'s job, not the UI's.** `WorkflowSettings.Path_TBD_Canonical` makes
  `WorkflowCalculator` copy and skip the conversion block; SAM_UI decides *whether* to, and never touches a
  TBD itself. No low-level TBD mutation was added to the UI.
- **No gbXML is written on the warm path.** The export exists to be imported and converted, and a canonical
  TBD is the product of having done that.
- **`UpdateZones` is forced on** for a warm-started run whatever the solar method: the zones carry the
  internal conditions, and re-deriving them from the current model is half of what makes the round the
  current design rather than the baseline's.

### `PartOCanonicalTBD` - the invalidation authority

**Not a cache.** It is created by run 0 of one optimisation, used only by that optimisation's rounds, and
never persisted or found again. The classic failure of a reused conversion is a stale one surviving a model
edit; a baseline that cannot outlive the run that made it cannot go stale that way at all.

A **fingerprint** over what the conversion reads - space and zone identities *and names* (TAS matches a
zone to a space by name), zone topology, panel and aperture identities, types, constructions and a
millimetre-rounded geometry digest, plus the solar method, weather, day range, sizing, unmet hours,
aperture widths and construction-layer update. **Design airflow is deliberately absent**: it is the thing
that changes every round and the thing the warm-started run re-applies, so including it would turn the warm
start off entirely.

Re-checked **every round**, plus the file's length and write time, so a baseline replaced underneath a
running optimisation is caught. Any mismatch **falls back to the full conversion** and names the category
that changed. A fallback is a note, never a refusal - the full path is always available and always
authoritative.

The digest is FNV-1a rather than `string.GetHashCode`, which is randomized per process and would have made
the comparison work in one place and not another.

### Files

| File | Change |
| --- | --- |
| `SAM_UI/SAM.Analytical.UI/Classes/PartO/PartOCanonicalTBD.cs` | New - the baseline and its fingerprint. |
| `SAM_UI/SAM.Analytical.UI/Classes/PartO/PartOOptimisationSettings.cs` | `WarmStart`. |
| `SAM_UI/SAM.Analytical.UI/Classes/PartO/PartOOptimisationStep.cs` | `WarmStarted`, per iteration. |
| `SAM_UI/SAM.Analytical.UI/Classes/PartO/PartOOptimisationRun.cs` | `CanonicalTBD`, its refusal, and the derived `WarmStarted` count. |
| `WPF/SAM.Analytical.UI.WPF/Modify/RunPartOSimulation.cs` | The optional canonical; no gbXML, no SAM-side TBD build, `UpdateZones` on. |
| `WPF/SAM.Analytical.UI.WPF/Modify/OptimisePartOTM59.cs` | Adopt once, check every round, and the same for the envelope. |
| `WPF/SAM.Analytical.UI.WPF/Windows/PartOIterationWindow.xaml(.cs)` | The tick, so the full path stays available as the reference. |
| `WPF/SAM.Analytical.UI.WPF/Windows/PartOOptimisationResultWindow.xaml.cs` | The count, beside the notes. |
| `WPF/SAM.Analytical.UI.WPF.Tests/PartOWarmStartTests.cs` | New - 26 tests. |

### Validation

- `SAM_UI.sln` builds clean.
- `WPF/SAM.Analytical.UI.WPF.Tests`: **325 passed, 0 failed** (299 + 26 new).
- `SAM_Tas`: **655 passed, 0 failed** (649 + 6 new).
- SAM: **1731 passed, 0 failed**, unchanged by this branch.

### Licensed A/B equivalence - ACCEPTED

`SAM_zoningAM-CIBSEfutureZ1.sam`, weather `Z1_DSY1_2050s_HIGH90_CIBSE_v1.1`, 3 rounds plus the capacity
envelope, run twice through the production commands - once with the tick off (the full-conversion
**reference**) and once with it on (the warm-start **candidate**). Identical model, identical design
airflows, identical weather, identical workflow settings; only the output directory differed.

**108 comparable lines, every one identical** - 40 production TM59 rows (Actual, Limit, Margin, PASS/FAIL,
mechanical, per space per step), 24 targeted and 8 derived design airflows, 20 ventilation-unit
duty/maximum/headroom/outcome rows, 3 capacity-envelope groups (scale, movement, duties, binding side), and
every run-level verdict including `STOP_REASON`, `ROUNDS`, `ENVELOPE_OUTCOME` and `COMMAND_WOULD_ARM`.
Paths, project names and the `warm=` flag were excluded from the comparison; nothing else was.

**Canonical immutability, from the production code itself.** `AB.tbd` was written at 16:37:26 and never
again, while the four rounds wrote their own TBDs at 16:37:55, 16:38:19, 16:38:45 and 16:39:09 - and
`PartOCanonicalTBD` re-verified its length and write time before every one of them, all four of which warm
started. The baseline step correctly did **not** warm start: it is the conversion the others start from.

### Issues / blockers

- **One bounded residual risk, stated rather than hidden.** `SAM_Tas`'s `Modify.UpdateIZAMs` removes
  internal conditions and IZAMs by the names the *current* cluster's air movements produce, and
  `UpdateZones` removes internal conditions by the current space names. Both then re-add. So a canonical
  entry could survive only if its name is one the current round does not produce. Between 2B rounds the
  movement names derive from space and unit names (invariant) and the movement *set* from the transfer-air
  topology, which is rebuilt from the same Part F requirements (invariant) - and the A/B equivalence above
  confirms it empirically on the licensed model. Cloning always from run 0 rather than chaining bounds this
  to run 0's own entries instead of letting anything accumulate, which is why that rule is not negotiable.
  A model whose round-to-round movement *names* could differ would need `UpdateIZAMs` to remove everything
  it owns before writing; that is not this change.

## Superseded (2026-09-02): the capacity envelope

**Status: implemented and tested; PR open against `sow/2026-Q3`.**

### Why it was needed

The ordinary Iteration 2B loop stops on `CapacityReached` or on its iteration guard with eligible rooms
still failing TM59, hands back the last valid design, and says why. What it cannot say is how close the
ventilation unit **already bought** can get - and that is the thing an engineer needs in order to decide
between changing the fabric, changing the equipment, and accepting the result.

### What was added

One optional final **diagnostic** stage, after the ordinary optimisation has reached its terminal
condition:

```
last ACCEPTED ordinary design
  -> the same deliberate target vector the +5 policy would next have asked for
  -> Modify.EvaluateDesignAirFlowCapacityEnvelope    one coherent scale per equipment group
  -> re-prepare Part O (NO catalogue)                transfer air, network and duties rebuilt
  -> full-year TAS, its own -OptMax TBD/TSD          the SAME weather case
  -> production TM59, on the model the workflow RETURNED
```

Every guarantee the rounds keep is kept here: the preparation is offered no catalogue so the Iteration 2
product survives; the TAS case is the baseline's, verbatim; the assessment reads the model the workflow
returned and never the preparation output; and the results file is the envelope's own.

### The separation, which is the whole design

An envelope is prepared, simulated over the full year and assessed **exactly** as a round is, and it
completes. Nothing about its lifecycle distinguishes it from a round - so everything that could read it as
one was changed to ask what kind of step it is:

- `PartOOptimisationStepKind` - `Baseline` / `OptimisationRound` / `CapacityEnvelope`, stated rather than
  inferred from an iteration number.
- `PartOOptimisationRun.Step_LastValid` excludes the envelope. **This is the most important line in the
  change**: without it the run would hand back, and the command would adopt, a partial step the
  optimiser's own all-or-nothing policy refuses.
- `PartOOptimisationRun.Rounds` excludes it, so it is never reported as another successful +5 step.
- Its model, TSD and scenarios live in their own properties -
  `AnalyticalModel_CapacityEnvelope`, `Path_TSD_CapacityEnvelope`, `OverheatingScenarios_CapacityEnvelope`
  - and `CapacityEnvelope` carries SAM's own per-equipment scales and reasons.
- It runs on its **own private `PartORun`**, so the session's run keeps holding the last accepted ordinary
  design and its results. Driving it through the session's run would have left the user assessing the
  diagnostic's results against the accepted design's model.
- `-OptMax`, named rather than numbered: `-Opt`*nn* would put it in the rounds' sequence where the last one
  is the answer. `Iteration_ProjectName` reads it back as no iteration at all.
- Presentation: a `Stage` column in both histories (BASELINE / OPTIMISATION / CAPACITY ENVELOPE), a `MAX`
  run label, its own line above the grids, and its own labelled clause in `Description`. A room whose first
  movement is in the envelope contributes **no** synthesised baseline row, because the envelope's "before"
  is the last accepted design's airflow and not the baseline's.

### Every "no" is recorded

Not asked for; the run passed; a stop reason an envelope does not answer; no failing verdict; nothing
eligible left to target; no useful headroom; an unresolvable capacity; a vector that cannot be formed. Each
is written to `CapacityEnvelopeDescription` in its own words, and for a non-scaled envelope each equipment
group's own reason is carried up onto that line - "this unit is already at its rating" and "its capacity is
not in the catalogue offered" are different findings and neither may be buried. **No TAS run is spent on
any of them**: every no-run decision is settled before the simulation.

### Optional, and on by default

A new `PartOOptimisationSettings.CapacityEnvelope`, exposed as its own tick under the 2B controls rather
than as another number beside the step and the limit - it is a diagnostic, not a further optimisation
parameter. On by default, because the case it answers is exactly the case in which the run on its own does
not tell an engineer what to do next; it costs one more full-year simulation, and nothing at all on a run
that passes.

### Files

| File | Change |
| --- | --- |
| `SAM_UI/SAM.Analytical.UI/Enums/PartOOptimisationStepKind.cs` | New - the three kinds of step. |
| `SAM_UI/SAM.Analytical.UI/Classes/PartO/PartOOptimisationStep.cs` | `Kind`, `IsOptimisationRound`, `IsCapacityEnvelope`. |
| `SAM_UI/SAM.Analytical.UI/Classes/PartO/PartOOptimisationRun.cs` | Envelope model/TSD/scenarios/description; `Rounds` and `Step_LastValid` exclude the envelope. |
| `SAM_UI/SAM.Analytical.UI/Classes/PartO/PartOOptimisationSettings.cs` | `CapacityEnvelope`. |
| `SAM_UI/SAM.Analytical.UI/Classes/PartO/PartOSimulationContext.cs` | `ProjectName_CapacityEnvelope()` - `-OptMax`. |
| `WPF/SAM.Analytical.UI.WPF/Modify/OptimisePartOTM59.cs` | The envelope stage; the loop split out unchanged as `Optimise`. |
| `WPF/SAM.Analytical.UI.WPF/Classes/PartO/PartOOptimisationAirFlowRow.cs` | `Stage`; `Run` is a string; envelope excluded from baseline synthesis. |
| `WPF/SAM.Analytical.UI.WPF/Classes/PartO/PartOOptimisationUnitRow.cs` | `Stage`; `Run` is a string. |
| `WPF/SAM.Analytical.UI.WPF/Windows/PartOOptimisationResultWindow.xaml(.cs)` | Envelope line, Stage columns, kind-labelled diagnostics, Copy All. |
| `WPF/SAM.Analytical.UI.WPF/Windows/PartOIterationWindow.xaml(.cs)` | The envelope tick and its explanation. |
| `WPF/SAM.Analytical.UI.WPF.Tests/PartOCapacityEnvelopeTests.cs` | New - 22 tests. |
| `WPF/SAM.Analytical.UI.WPF.Tests/PartOOptimisationTests.cs` | `Run` comparisons follow the string column. |

### Validation

- `SAM_UI.sln` builds clean.
- `WPF/SAM.Analytical.UI.WPF.Tests`: **299 passed, 0 failed** (277 post-#78 baseline + 22 new).
- SAM side: `SAM.Tests` **1727 passed, 0 failed** (1703 + 24 new).
- The 22 new tests cover: a completed envelope is not the last valid design, by object identity; it is not
  counted as a round; the description keeps it apart; its production TM59 is stored separately and can pass
  while the run's answer still fails; `-OptMax` is distinct from every round name and reads back as no
  iteration; the three stages are distinguishable in both grids; a room only the envelope moves gets no
  synthesised baseline row; and each no-run decision - not asked for, passed, every non-answering stop
  reason, no failing verdict, no eligible target, passing rooms only, and no useful headroom - reaches no
  simulation and appends no step.

### Issues / blockers

- None known.

### Next step

- Merge SAM `feature/parto-iteration2b-capacity-envelope` first, then this PR.
- Task 2, the canonical-TBD TAS warm-start, is a separate deliverable and a separate PR.

## Superseded (2026-09-02): pinning the subset-pass guard and the round count - merged as PR #78

**Status: implemented and tested; PR open against `sow/2026-Q3`.**

`81d9117` fixed two findings - a TM59 pass over a subset is not a pass (P1), and an unattempted round is
not a round (P2) - with no regression tests, the only review fixes on either PR shipped that way. This
change pins them.

- `Modify.PartialAssessment` and the `PartOTM59Assessment` constructor are **internal** rather than
  private, so the guard is testable through the existing `InternalsVisibleTo` - the same seam
  `PartODwellingSpaceGuids` was given in `81d9117`. The production route to an assessment remains
  `Assess`, which needs a real TSD.
- New tests in `PartOOptimisationTests`: an in-scope room the assessment never looked at turns the pass
  into a refusal naming that room; unassessed rooms outside the dwelling scope (corridor, AHU simulation
  zone) do not; a fully assessed pass is untouched. Verified the first fails with the guard neutralised.
- The phantom-step fix's recording side sits behind the TAS seam and is not unit-testable here; what is
  pinned is the run-level contract it restored - a baseline-only run reports **0 rounds** and the baseline
  as the last valid design.

**277** tests in `WPF/SAM.Analytical.UI.WPF.Tests` pass (273 + 4 new).

### Next step

- Merge the PR.

## Superseded (2026-09-02): Iteration 2B automatic optimisation - merged as PR #77

## Current status (at the #77 merge)
Iteration 2B is implemented end to end in SAM_UI and accepted on the licensed future-weather fixture.
From a completed Iteration 2 run it raises the design airflow of every eligible failing mechanically
ventilated room by a fixed step, rebalances through the new SAM round, rebuilds the Part O state,
re-simulates the **same** weather case under its own project name, reassesses with production TM59, and
repeats until an explicit stop reason is reached.

Merged as PR #77 (`d056af7`); **273** tests in `WPF/SAM.Analytical.UI.WPF.Tests` passed at merge.

## Integration state
Sibling repos, and what changed:

- `SAM` @ `sow/2026-Q3` `0ae4b929` + **PR #88** - the new `Modify.EvaluateTargetedDesignAirFlows` round.
  **This is a required companion change.** (Merged.)
- `SAM_Systems` @ `fea5055` - **unchanged**. Nuaire `MRXBOXAB-ECO5-AECV + MR-ECO-COOL-V`, 150/150 l/s.
- `SAM_Tas` @ `78f7afb` - **unchanged**.

### What was built

- `PartOOptimisationSettings` - the fixed step (5 l/s default) and the mandatory iteration guard.
- `PartOPreparationContext` / `PartOSimulationContext`, both carried on `PartORun` - how the run was
  prepared and which TAS case produced it, so a round repeats **both** rather than re-asking. Cleared when
  a run is dropped.
- `Modify.RunPartOSimulation` - `Simulate`'s TAS pipeline, extracted so the optimiser repeats the exact
  same case under its own project name (and therefore its own TBD/TSD) without a second copy existing to
  drift. `Simulate` now calls it.
- `PartOTM59Assessment` - the assessment sequence, extracted for the same reason, and resolving every
  result to its **design** space by identity through `SimulationSpaceMap`.
- `Query.PartOOptimisationTargets` - the target policy: production TM59 mechanical failures, inside the
  Part O dwelling scope, supply/extract/both-means-supply, and an explicit *not automatically optimisable*
  record where a room has no design terminal. Scope excludes the corridor and the AHU simulation zones
  **by scope, not by name**.
- `Modify.OptimisePartOTM59` - the loop. Each round is a complete `PartORun` lifecycle, so every PR #76
  guarantee holds rather than being worked around.
- `PartOOptimisationRun` / `PartOOptimisationStep` / `PartOOptimisationStopReason` - the history, with the
  baseline as run 0.
- UI: the 2B tick with step and iteration limit on the preparation window (**not** in the base-provision
  dropdown), an *Optimise (2B)* ribbon button, and a results window with both histories.

### Two settled judgement calls

1. **Per-dwelling capacity.** A dwelling whose selected unit refuses a round leaves the optimisation and
   its last valid design is preserved; independently served dwellings continue. No partial or clamped step
   is ever adopted for the blocked dwelling.
2. **Run 0 keeps the Iteration 2 workflow's own TSD** rather than being re-simulated as `-Opt00.tsd`. It
   retains its original TSD, weather and workflow identity in the history.

### Equipment is fixed for the whole optimisation

The re-preparation is given **no catalogue**. `PreparePartOIteration` with one re-runs smallest-capable
selection against the realized duty, so every round the design grew it would quietly buy the next product
up and capacity would keep moving. With null it reuses the existing system and unit through their design
terminals, so the Iteration 2 selection survives. The catalogue is still used - by the round, to read what
the selected product is rated at.

### Licensed acceptance - ACCEPTED

`SAM_zoningAM-CIBSEfutureZ1.sam`, weather `Z1_DSY1_2050s_HIGH90_CIBSE_v1.1`, 12 full-year TAS runs, 13 min.

- Iteration 2 Run 0 reproduces the Iteration 1a baseline **exactly**; product selection caused **no**
  thermal change (8 results identical, 0 different).
- Round 1 produced the intended targets: Flat 1 Studio 1_0 supply 30->35 and Bathroom_2 extract 8->13 with
  **no** derived change (they balance directly); Flats 2 and 3 two extract targets each deriving
  Bedroom 2_3 / 2_6 supply 63->**73**.
- TM59 improved monotonically every round (Bathroom_2 731->375, Ensuite_8 677->331, Studio 1_0 388->361).
  Passing rooms were never targeted.
- MVHR-02/03 reached 143/143 l/s (7 l/s headroom) by round 8; at round 9 the round would have designed
  153/153 against 150/150 and was **refused**, those flats left the optimisation, and Flat 1 continued
  alone to 80/80. Product `Kept` on every row, never `Reselected`.
- Final stop `IterationLimitReached` at the guard of 10, last valid design = run 10.

### Review findings addressed (Codex, PR #77)

Round 1 (`dd6b24a`):
1. **P1** - a round whose assessment failed left `PartORun` in `WorkflowCompleted` holding that round's
   model and TSD while the previous design was restored. Now invalidated, and the command arms
   `ExpectModification` only where the run holds the very model being adopted.
2. **P2** - 2B was offered on the natural-ventilation route. Now gated on the selected option's
   ventilation mode and refreshed when the base provision changes.
3. **P2** - the airflow history began at run 1. Baseline rows are now synthesised from each room's first
   adjustment plus the baseline step's own production verdict.

Round 2:
4. **P1** - `Simulate` built the simulation context and then completed the run through the **context-less**
   `Complete` overload, so every baseline produced through the window had a null `SimulationContext` and
   was refused by `CanOptimise`. The Optimise button could never have started. Fixed; a test pins the
   invariant.
5. **P1** - `OccupiedSpaceComplianceStatus != Fail` treated `Undefined` / `NotApplicable` as a pass, so an
   assessment that reached no verdict would have been reported as every eligible space passing. Only an
   explicit `Pass` now ends a run as passed; anything else stops as `AssessmentFailed`.

Round 3:
6. **P2** - an optimisation run a second time over the design a previous one left behind restarted its
   round numbering at `-Opt01`, overwriting the first run's TBD and TSD. The numbering now continues from
   the iteration read back off the results file the starting design came from, so `-Opt11` follows
   `-Opt10` and nothing is written twice; the baseline step is also labelled with the run that actually
   produced it rather than the context's base name.

Round 4:
7. **P1** - a TM59 report can return an explicit `Pass` over a SUBSET: `PartOTM59Assessment` excludes a
   space whose simulated counterpart does not resolve and records a warning, and the remaining rooms then
   all pass. The run would have announced that every eligible occupied space passes on the strength of an
   assessment that never looked at one of them. A pass is now believed only where no space inside the Part
   O dwelling scope was excluded; `PartOTM59Assessment.SpaceGuids_Unassessed` exposes that as identities
   rather than prose.
8. **P2** - a step was appended before the zero-target check, so a run that stopped at
   `NoEligibleTargets` reported a round that was never attempted and inflated `Rounds`. The step is now
   recorded only once there is a round to attempt, and the target-selection reasons go on the step that
   produced the design they were derived from.

### Validation

- `SAM_UI.sln` builds clean.
- `WPF/SAM.Analytical.UI.WPF.Tests`: **273 passed, 0 failed** (237 baseline + 36 new).
- `PartORunLineageTests` + `PartOPresentationTests` + `SimulationZoneIdentityTests`: 47/47 - no lineage
  regression.
- SAM side: `SAM.Tests` **1694 passed, 0 failed**.

### Issues / blockers

- `Build (Windows)` on PR #77 fails until SAM #88 merges - see "Branch". Expected, not a defect here.
- One Codex P2 on SAM #88 was **declined with reasons**: validating terminal quantities across every room
  of a touched system, not only the rooms a round writes. `ApplyTargetedDesignAirFlow` validates the same
  narrow set, and widening only the round would make it refuse dwellings a manual edit accepts - the exact
  drift the shared-helper design prevents. Raised as a separate follow-up against both seams.

### Next step

- Merge `SAM-BIM/SAM` #88 first, then this PR.
- The parked manual-seam defect (`ApplyTargetedDesignAirFlow` ventilation-unit reselection leaking onto the
  caller's `AnalyticalModel`) remains untouched and outstanding.

## Completed

### 1. BLOCKER 1 - the `Modify.Simulate` zone-identity corruption (fixed)

The post-workflow TBD block re-read the `.tbd` and copied `SpaceParameter.ZoneGuid` back onto the model
by matching space **name**. Two independent defects, both silent:

1. **Duplicate room names collapsed identities.** `spaces_TBD.Find(x => x?.Name == space?.Name)` returns
   the *first* match, and the write was unconditional, so three flats each containing "Bedroom 2" all
   received one flat's zone guid - overwriting the strong identity `WorkflowCalculator.Calculate` had
   just written via `SAM.Analytical.Tas.Modify.UpdateIds`.
2. **The value was re-spelled even when the match was right.** The read was
   `TryGetValue(ZoneGuid, out Guid)` against a parameter declared `ParameterType.String`
   (`SAM.Analytical.Tas.SpaceParameter`), so the raw TAS string was parsed to a `Guid` and converted back
   by `ParameterValue.TryConvert` on the way in - losing braces and case.
   `Tas.Query.SimulationSpaceKey` compares the stored strings ordinally, so a re-spelt stamp stops
   matching the TSD side.

**Concrete dependency found - removal was NOT safe. Correction 2026-09-01: the earlier justification for
this was wrong on the mechanism, and only the DomOv export is a dependent.**

The claim recorded here previously was that `Tas.TM59.Convert.ToXml` *refuses* a space whose `ZoneGuid` is
absent or empty. **It does not.**
`Tas.TM59.Convert.ToTM59(Space, TM59Manager, SystemType)`
(`SAM.Analytical.Tas.TM59/Convert/ToTM59/Zone.cs:39-43`) reads the stamp and, when it is absent or empty,
**falls back to `space.Guid`** and exports the zone anyway. No refusal, no note, no failed export.

What makes the seam necessary is that the fallback value is the wrong identity:

- With **Simulate unticked** and "Domestic Overheating" ticked, the `.tbd` is written by
  `Tas.Convert.ToTBD(analyticalModel, path_TBD, null, null, null, true)`. That last argument *is*
  `updateGuids: true`, but `AnalyticalModel.AdjacencyCluster`
  (`SAM/SAM.Analytical/Classes/AnalyticalModel.cs:179-186`) returns `new AdjacencyCluster(...)` - a copy -
  so the zone guids `Tas.Modify.Update` stamps land on a throwaway cluster and never reach the model
  `Modify.Simulate` holds. Nothing on that path stamps the model, so it reaches the export either
  unstamped or - if it was saved after an earlier simulation - carrying a *stale* stamp. **Measured on the
  acceptance fixture: `SAM_zoningAM.sam` carries nine saved stamps with zero overlap with the zones of the
  `.tbd` the export writes.** See round 2's P1 under "Review findings addressed".
- A SAM `space.Guid` is not a TAS zone guid. A TBD zone's guid is minted by `building.AddZone()`
  (`SAM.Analytical.Tas/Modify/Update.cs:577`) and bears no relation to the space it was written from, so
  the two values differ by construction.
- Therefore, without the fill, the DomOv XML is written **successfully** with `DomOverheatZoneItem/GUID`
  set to the SAM space guid - naming zones the external TAS TM59 tool cannot find, in a document that
  reports success. That is worse than a refusal, not better.

Pinned by `SimulationZoneIdentityTests.AnUnstampedSpace_ExportsTheSAMSpaceGuid_UntilTheFillGivesItTheTasIdentity`
and `...TheDomOvXmlNamesTheTasZone_OnlyAfterTheFill`, which assert the exported identity on both sides of
the fill rather than the refusal that does not happen.

**SAP and Part L do not depend on this seam** (previously claimed; not supported by their call paths):

- `createSAP` calls `Tas.SAP.Convert.ToFile(analyticalModel_TBD, ...)` - the model read back **out of the
  `.tbd`**, whose spaces `Tas.Convert.ToSAM` stamps itself (`Convert/ToSAM/Space.cs:98`). It never sees the
  restamped design model. (`ToSAP` has the same `space.Guid` fallback, but is never reached with an
  unstamped space on this path.)
- `createPartL` calls `Tas.Create.TBD_ByPartL(analyticalModel, ...)`, whose three writers
  (`UpdateInternalConditionByPartL`, `UpdateZoneGroupsByPartL`, `UpdateZoneGroups`) read no `ZoneGuid` at
  all.

Per the brief, the *removal* was stopped and the (now correctly stated) dependency recorded here.

**What was done instead** - new internal seam `Modify.RestampSimulationZoneIdentity`, and no new matching
algorithm:

- a space carrying a `ZoneGuid` **written for the `.tbd` being exported** is left untouched (the
  workflow's stamp is authoritative *and* current). Whether that is so is the caller's
  `workflowCompleted`, passed in as `stampsWrittenForThisTBD` - see round 2's P1 below, which corrected an
  earlier version of this rule that trusted any stamp;
- on the non-workflow path every space is re-derived from the newly read `.tbd`: an unambiguous name match
  replaces the stamp, anything less discards it with a note;
- an ambiguous name **refuses with a reason** instead of taking the first hit. Two simulated spaces
  stating the *same* guid are one answer, not a conflict (the rule `VentilationStrategyMap` already
  applies to a repeated claim);
- the value is copied **as a string, verbatim** - no `Guid` round trip;
- spaces left unstamped are reported in the completion dialog (capped at 5 with the remainder counted).

The precedence rule mirrors `SAM.Analytical.Tas.Query.ResolvedZone` ("guid first, exact name only as the
compatibility fallback, no match is a refusal, never a guess") rather than inventing one.

### 2. BLOCKER 2 - stateful, stale-safe Part O run context

`PartORun` (in `SAM.Analytical.UI`) with an explicit lifecycle `None -> Prepared -> WorkflowCompleted`:

- **Prepared** owns the prepared model and its `OverheatingScenario` set.
- **WorkflowCompleted** additionally owns the model the TAS workflow *returned* and the corresponding
  `Path_TSD`.
- `AnalyticalModel_Assessment` is non-null **only** in `WorkflowCompleted`. There is no code path on
  which it can be the preparation output, the loaded model, or a later run's model.
- **`WorkflowCompleted` means "this prepared run produced the full-year results being assessed"** - not
  "a TSD exists". `Complete` is legal **only from `Prepared`** and requires, in addition to a non-null
  workflow model and a TSD that exists:
  - **the simulation to have been the full annual run** (days 1-365), which
    `Query.IsPartOFullYearSimulation` decides from the `WorkflowSettings` actually handed to
    `WorkflowCalculator` - not from the "Full Year Simulation" tick box, whose day range still comes from
    the two text boxes beside it and which `shadingUpdated` can turn into a one-day run;
  - **the results file to have been created or rewritten by this workflow**, measured against a
    fingerprint (`exists` + length + write time) captured by `PartORun.ExpectResults` **before** the
    workflow ran.

  Both are enforced through the same arming: `Modify.Simulate` calls `ExpectResults` only where the
  settings describe a full-year run, and `Complete` refuses anything unarmed or announced for a different
  path. So a partial, one-day or sizing-only workflow cannot complete a run even if `Complete` is reached,
  and an earlier session's `<project>.tsd` left in the output directory cannot be adopted as this run's.
- `IsAssessable` re-checks the file's existence and write time **after** completion, so results rewritten
  by another session later are refused. That is a different problem from the one above and both checks are
  kept.
- **Staleness is rejected, not detected.** `ExpectModification()` / `NotifyModified()`: Part O commands
  arm one expectation immediately before their own `SetJSAMObject`; every other model replacement (edit,
  import, undo, redo, a second or unrelated simulation) arrives unarmed and drops the run with a reason.
  Wired in `AnalyticalWindow.UIAnalyticalModel_Modified`; `_Closed`/`_Opened` call `Reset()`.
- Session state only. Nothing is written into the model - `OverheatingScenario` has no persistence seam
  in `SAM.Analytical`, and inventing one in the UI would move engineering state into the UI's ownership.

### 3. BLOCKER 3 - SAM_Systems dependency placed in the WPF layer

`SAM.Analytical.Systems` is referenced **only** from `WPF/SAM.Analytical.UI.WPF.csproj`;
`SAM.Analytical.UI` stays free of it. `VentilationUnitCatalogue` (in `SAM.Analytical.UI.WPF`) calls
`SAM.Analytical.Systems.Query.VentilationUnitTemplates` and then `SAM.Analytical.Query.CapacityDescriptors`
/ `UnselectableVentilationUnitTemplates`. No schema parsing and no selection rule in SAM_UI. Three
distinct states: `Unavailable`, `NoneSelectable`, `Selectable`. The `Unavailable` description explicitly
says it is *not* a statement that no product could serve the dwellings.

The catalogue resolves at runtime from the installed resources
(`%APPDATA%\SAM\resources\Analytical\Systems\VentilationUnit\VentilationUnitCatalogue.JSON`), which the
SAM_Deploy installer already places; no deployment change was needed.

### 4. Part O preparation UI

- `PartOVentilationStrategyOption` - the picker. Offers only the two base provisions, each carrying the
  canonical word (`NV`, `MVHR`) and the iteration SAM says that route is defined over
  (`Query.PartOIterationVentilationMode`). **No free text anywhere reaches the API**, so the
  `"NaturalVentilation"` synonym - which prepares successfully then refuses every space at assessment - is
  unreachable from the UI. No analytical vocabulary was changed.
- `PartOIterationWindow` - base provision, dwelling scope, catalogue toggle. Scope comes from
  `Query.PartFDwellingZones` (the single source of that policy, including the legacy all-unmarked case);
  out-of-scope zones are derived **by difference from what the policy returned** and reported as
  "Outside current Part O dwelling preparation scope". No `UV` is assigned; no common-space criterion.
- `Modify.PreparePartOIteration` (UI command) - builds the strategy dictionary, reads the catalogue, makes
  **one** call to `SAM.Analytical.Modify.PreparePartOIteration`, and adopts the returned prepared model.
  Passes `null` (not an empty list) when no selection is wanted, so the preparation reads it as
  "no catalogue offered" = Iteration 1a.
- `PartOPreparationWindow` - two grids. Equipment (per AHU): design supply/extract duty, selected product,
  maximum supply/extract, supply/extract headroom, selection outcome - eight separate columns. Spaces:
  Part F required vs design supply/extract. Values read from `Query.AirHandlingUnitDesignDuty`,
  `Query.SelectedVentilationUnitCapacityDescriptor`, `Query.CalculatedSupplyAirFlow` and
  `PartFSpaceData.ContinuousDesignFlowRate_Lps`. The whole-run duty total is labelled as a total across
  dwellings, not as any one dwelling's duty.

### 5. TAS -> TM59 lineage

`Modify.AssessPartOTM59(PartORun)` runs the same sequence as the accepted `Tas.TSDQueryTM59Results`
component - `Convert.ToSAM(TSD)`, `Create.TM59AssessmentCalculator`, `OverheatingScenarioMap`,
`RestoreDesignInternalConditions`, `Spaces(null, null)`, `Calculate`, `TM59AssessmentReport` - and reads
its model from `PartORun.AnalyticalModel_Assessment` only. `PartOTM59ResultWindow` shows the production
report text verbatim plus the spaces that produced no result. No TM59 criterion, limit or verdict is
computed or reformatted in WPF.

`Modify.Simulate` gained a `PartORun` overload; it sets `workflowCompleted` only where
`WorkflowCalculator` actually returned a model, and completes the run with **that** model and
`Path.ChangeExtension(path_TBD, "tsd")`.

### 6. Ribbon

- Edit tab, new `Part O` group: **Prepare Iteration**.
- Results tab, new `Part O` group: **Overheating (TM59)** - enabled only for `CanAssess`, with
  `ToolTipService.ShowOnDisabled` so the reason is visible while disabled.
- The simulation window's "Domestic Overheating" tick (TAS DomOv XML) is untouched and kept conceptually
  separate.

## Review findings addressed (PR #76, 2026-09-01)

Four findings over two review rounds. Round 1: two P1 on the completion predicate and the results file.
Round 2: one P1 on stale stamps and one P2 on the assessment gate.

Two P1 findings on the first push (`1437d88`), both accepted and both about the same invariant: what
`PartORunState.WorkflowCompleted` is allowed to mean. Restated at the top of `PartORun`'s own
documentation - **"this prepared run produced the full-year results being assessed"**, never "a TSD
exists". No architecture change, no Iteration 2B.

### P1-A - a Part O run may only be completed by a full-year simulation

`completePartORun` required only that `WorkflowCalculator` returned a model. It returns one for a sizing
run too, and `Modify.Simulate` can produce a *fresh one-day* TSD when `shadingUpdated` forces a
simulation over an unticked Full Year box. Any of those promoted the run, and the TM59 command then
assessed criteria and verdicts over an incomplete hourly series.

New pure query `Query.IsPartOFullYearSimulation(WorkflowSettings)` - `Simulate && SimulateFrom == 1 &&
SimulateTo == 365`, read off the settings **actually handed to `WorkflowCalculator`** rather than off the
tick box, because the day range still comes from the two text boxes beside it. `Modify.Simulate` now
carries `workflowSimulatedFullYear` beside `workflowCompleted` and ANDs it into the predicate. Nothing
else reads it, so a normal non-Part-O simulation is unchanged.

A prepared run this simulation cannot complete is now dropped **with the reason it was actually refused
for**, before the model replacement that would otherwise report it as an outside edit, and that sentence
is appended to the completion dialog.

### P1-B - the TSD must be the one this workflow wrote

`Complete` accepted `<project>.tsd` because it existed and then recorded its *already old* write time as
this run's, so `IsAssessable` afterwards approved an earlier session's results against the newly prepared
model. `Modify.Simulate` deletes only the TBD, so a non-simulating workflow leaves the old TSD in place.

New `PartORun.ExpectResults(path_TSD)`, called **before** the workflow, fingerprints whatever is at that
path (`exists` + length + write time). `Complete` now refuses unless a fingerprint was armed for exactly
that path and the file has since been created or changed. Length as well as write time, so a rewrite
inside the filesystem's timestamp granularity is still seen; where both match, the file is treated as
untouched - refusing a genuine rerun is the safe way to be wrong.

Deleting the prior TSD was considered and rejected: it would destroy a user's previous results whenever a
Part O run failed, and it changes the existing workflow contract. The fingerprint changes nothing outside
the Part O promotion.

**Arming is where both fixes meet.** `ExpectResults` is armed only where the settings describe a
full-year run, so a partial, one-day or sizing-only workflow leaves the run unarmed and `Complete`
refuses it *even if reached* - the invariant is held by the state machine, not by the caller remembering
to check. `IsAssessable`'s post-completion stale-file check is kept unchanged: it solves the different
problem of a rewrite after a legitimate completion.

Ten regressions added to `PartORunLineageTests` (11 -> 21), and every successful completion in that file
now goes through a `CompleteThroughAFullYearWorkflow` helper that performs the production
arm -> write -> complete sequence, so a test cannot pass by calling `Complete` the way no caller does.

### Round 2, P1 - a stamp is authoritative only if the current run wrote it

`RestampSimulationZoneIdentity` treated **any** present `ZoneGuid` as authoritative. On the workflow path
that is right. On the Simulate-unticked DomOv path it is wrong: `Tas.Convert.ToTBD` deletes any existing
`.tbd` and mints new zone guids, so a stamp the model was already carrying names a zone in an *earlier*
file. The early exit preserved it and `Tas.TM59.Convert.ToXml` then exported GUIDs from the previous TBD,
which the TAS tool cannot associate with the TBD beside them.

**Not hypothetical, and not rare.** The acceptance fixture itself carries nine saved stamps, spelled
unbraced/lower-case (the fingerprint of the old `Guid` round trip), with **zero overlap** with the zones of
the `.tbd` the export writes.

The fix is a mode, because "is this stamp current?" is a fact about the run and not about the stamp:

```csharp
RestampSimulationZoneIdentity(spaces_Design, spaces_Simulation, workflowCompleted, out notes)
```

- `true` - `WorkflowCalculator` wrote `path_TBD` and stamped this model against it. Existing stamps are
  authoritative and current: **untouched**, fill only where absent. Unchanged from before.
- `false` - no workflow ran, so every space is re-derived from the newly read `.tbd`. An unambiguous name
  match **replaces** the stamp; anything less **discards** it with a note.

**The duplicate-name fix is intact.** Ambiguity is refused in both modes - the mode decides whether a
*stale* stamp survives, never whether a name may be guessed at. Three flats each with a "Bedroom 2" are
still refused, and on the non-workflow path their stale stamps are dropped rather than exported.

**Discarding, not keeping, an unreplaceable stale stamp** follows `Tas.Modify.UpdateIds`'s own rule: it
clears every stamp before re-resolving, so a failed resolution leaves the space unstamped. Absent beats
wrong - every consumer already handles and reports absent (`Query.ResolvedZone` falls back to the name,
`SimulationSpaceKey` reads null, the DomOv exporter falls back to `space.Guid` and the note says so),
whereas a stamp naming a zone in a discarded `.tbd` looks exactly like a good one.

### Round 2, P2 - a completed run whose results have gone is dropped, not just refused

`IsAssessable` refused the click but left `State` at `WorkflowCompleted`, so `RefreshPartOButtons`
re-enabled the button with the success tooltip as soon as the dialog closed - offering a click known to
fail, indefinitely.

`IsAssessable`'s two results checks now `Invalidate` the run with their own reason. The state check does
**not**: a `Prepared` run is live and waiting for its simulation, and `None` has already been explained.
So one click gives a dialog, a disabled button, and the exact reason in the tooltip.

`CanAssess` stays a pure state read - a property the ribbon evaluates on every refresh must not touch the
filesystem, and must not drop a run as a side effect of being looked at. The command reads `IsAssessable`,
which is the real gate. The window between the file changing and the next click is unavoidable without
polling, and the command re-checks at click time by design.

## Decisions / assumptions
- `PartORun` holds model + scenarios directly rather than a `PartOIterationPreparation`, because that
  type's setters are `internal` to `SAM.Analytical` and so could not be constructed by a test. The
  `Prepare(PartOIterationPreparation)` overload remains as the production entry point.
- Rows are keyed on the `AirHandlingUnit`, not paired positionally with
  `PartOIterationPreparation.VentilationUnitSelections`, which is documented as **not** item-for-item with
  `AirHandlingUnits`. Headroom is stated exactly as `VentilationUnitSelection.SupplyHeadroom_Lps` defines
  it, over the two values already on the row.
- `RibbonButton_CreateTBD_Click` (line ~2076) still calls the parameterless `Simulate()`. Its ribbon
  button is commented out in XAML, so it is unreachable; if re-enabled, its model replacement would
  correctly drop a pending run. Left alone deliberately.
- A note is emitted for a workflow-path space the workflow itself could not resolve. Harmless (the name
  fallback cannot invent a stamp there, because TBD space names come from zone names), and informative.

## Files changed
Modified:
- `WPF/SAM.Analytical.UI.WPF/Modify/Simulate.cs`
- `WPF/SAM.Analytical.UI.WPF/SAM.Analytical.UI.WPF.csproj` (+`SAM.Analytical.Systems`)
- `WPF/SAM.Analytical.UI.WPF/Windows/AnalyticalWindow.xaml`
- `WPF/SAM.Analytical.UI.WPF/Windows/AnalyticalWindow.xaml.cs`
- `WPF/SAM.Analytical.UI.WPF.Tests/SAM.Analytical.UI.WPF.Tests.csproj` (+`SAM.Analytical.Tas`)

Added:
- `SAM_UI/SAM.Analytical.UI/Enums/PartORunState.cs`
- `SAM_UI/SAM.Analytical.UI/Classes/PartO/PartORun.cs`
- `SAM_UI/SAM.Analytical.UI/Classes/PartO/PartOVentilationStrategyOption.cs`
- `WPF/SAM.Analytical.UI.WPF/Enums/VentilationUnitCatalogueState.cs`
- `WPF/SAM.Analytical.UI.WPF/Classes/PartO/VentilationUnitCatalogue.cs`
- `WPF/SAM.Analytical.UI.WPF/Classes/PartO/PartOEquipmentRow.cs`
- `WPF/SAM.Analytical.UI.WPF/Classes/PartO/PartOSpaceRow.cs`
- `WPF/SAM.Analytical.UI.WPF/Modify/RestampSimulationZoneIdentity.cs`
- `WPF/SAM.Analytical.UI.WPF/Query/IsPartOFullYearSimulation.cs`
- `WPF/SAM.Analytical.UI.WPF/Modify/PreparePartOIteration.cs`
- `WPF/SAM.Analytical.UI.WPF/Modify/AssessPartOTM59.cs`
- `WPF/SAM.Analytical.UI.WPF/Windows/PartOIterationWindow.xaml{,.cs}`
- `WPF/SAM.Analytical.UI.WPF/Windows/PartOPreparationWindow.xaml{,.cs}`
- `WPF/SAM.Analytical.UI.WPF/Windows/PartOTM59ResultWindow.xaml{,.cs}`
- `WPF/SAM.Analytical.UI.WPF.Tests/SimulationZoneIdentityTests.cs` (11)
- `WPF/SAM.Analytical.UI.WPF.Tests/PartORunLineageTests.cs` (24)
- `WPF/SAM.Analytical.UI.WPF.Tests/PartOPresentationTests.cs` (12)

## Validation
- `dotnet build SAM_UI.sln -c Debug` - succeeded, 0 errors (the pre-existing MSB3245/MSB3270 warnings for
  `System.Data.DataSetExtensions`, `Microsoft.CSharp`, `PresentationFramework.Aero2` and the Interop
  architecture mismatch are unchanged).
- `dotnet test WPF/SAM.Analytical.UI.WPF.Tests` - **Passed 234, Failed 0, Skipped 0** (187 baseline + 47).
- BLOCKER 1's regression was written first and confirmed red (5 x CS0117, missing seam) before the fix.
- Two genuine defects in this work were caught by its own tests and fixed:
  1. `PartOIterationWindow` reported unmarked zones as out of scope even where `PartFDwellingZones` had
     put them **in** scope (the legacy all-unmarked model). Out-of-scope is now derived by difference from
     the policy's own return value instead of by a second reading of `IsDwelling`.
  2. The `NoneSelectable` test fixture was itself invalid (`VentilationUnitTemplate.IsValid` requires a
     `Source`); the reader had correctly returned `Unavailable`. Fixture fixed, production code was right.
- Authority-leakage sweep over the new files: no call to `SelectSmallestCapableVentilationUnit`,
  `CapableVentilationUnits` or `Modify.SelectVentilationUnit`; no TM59 criterion, limit or
  `MaxExceedableHours` arithmetic. Re-run after the 2026-09-01 correction - still clean; the only hit is
  a doc comment in `VentilationUnitCatalogue` saying why the selection rule is *not* called here.

### Licensed UI acceptance (2026-09-01) - PASSED

Run on the acceptance fixture
`OneDrive - Tetra Tech, Inc/Documents/SAM_daily/2026-07-15 PartO/SAM_zoningAM.sam` (9 spaces, 4 zones,
Flat 1/2/3 `IsDwelling = true`, `Corridor` false), weather `CIBSE Weather 2021.twd`. Three full-year
licensed runs of the same chain, all three PASS and agreeing on every assessed number:

| Run | Output | Sizing | "Domestic Overheating" |
|---|---|---|---|
| A | `C:\TasOut\pui` | on | off |
| B | `C:\TasOut\pui2` | on | on |
| **C (headline)** | `C:\TasOut\pui3` | **off** | on |

Run C is the one to quote: the standing instruction for Part O licensed runs is `Sizing = false`, since the
assessment reads free-running hourly temperatures. On this free-running fixture it made no difference - all
three runs report the same eight exceedance counts (13 / 4 / 3 / 4 / 0 / 2 / 4 / 0 hours against limits
262 / 262 / 262 / 142 / 262 / 262 / 142 / 262) and the same
`TM59 OCCUPIED-SPACE ASSESSMENT: PASS` - which is itself worth recording, since these are internal
conditions with no cooling setpoint.

**How it was driven.** A console harness (`C:\TasOut\poui`, not part of the repository) hosts a WPF
`Application`, calls the production commands - `Modify.AddVentilationByPartF`,
`Modify.PreparePartOIteration`, `Modify.Simulate(uIAnalyticalModel, partORun)`, `Modify.AssessPartOTM59`
- and completes each real dialog from a `Window.Loaded` class handler instead of by mouse, reading the
values straight off the windows' own controls. The one piece of `AnalyticalWindow` wiring it reproduces
is that window's single-line handler `partORun.NotifyModified()` on `UIAnalyticalModel.Modified`. **Not**
covered by this run: the ribbon controls themselves (their enabled state and tooltip are the expression
`RefreshPartOButtons` evaluates over `PartORun.CanAssess` / `State` / `InvalidationReason`, which the
harness logs, and `PartORunLineageTests` covers).

| Check | Result |
|---|---|
| Dwelling scope | `3 dwelling zone(s) in scope`; `'Corridor' (marked not a dwelling)` named as outside it |
| Catalogue | `1 selectable ventilation unit product(s) available`, offered and ticked |
| Flat 1 design duty (`MVHR-01`/`MVHR 1`) | **30.0 / 30.0 l/s** |
| Flat 2 design duty (`MVHR-02`/`MVHR 2`) | **63.0 / 63.0 l/s** |
| Flat 3 design duty (`MVHR-03`/`MVHR 3`) | **63.0 / 63.0 l/s** |
| Selected product maximum | **150.0 / 150.0 l/s**, in its own `Maximum supply`/`Maximum extract` columns, headroom 120/120 and 87/87 |
| Equipment capacity overwriting design airflow | None. Design columns are 30/63/63 with the same 150/150 product on all three |
| TAS workflow | Licensed full year, days 1-365, `Model successfuly converted`, 1min18sec |
| Model assessed | `partORun.AnalyticalModel_Assessment`, the workflow output - the result window states it, and `PartORun` has no other source for it |
| Zone identity on the assessed model | 9 of 9 spaces stamped, 0 unstamped |
| TM59 assessment | `Assessed 9 space(s)`, 8 mechanical results, **PASS** |

**The eight mechanical results are correct for the UI's dwelling scope, and are not the nine the earlier
Grasshopper/harness acceptance reported.** `Corridor_1` is in the `Corridor` zone, which
`Query.PartFDwellingZones` puts outside the Part O dwelling scope, so no `OverheatingScenario` covers it
and the report says so by name under `SPACES NOT ASSESSED`. The SAM-repo acceptance drove
`PreparePartOIteration` over a scope that included it. Eight assessed dwelling spaces + one reported
common space = the nine the model has.

**Simulation-space mapping: no identity-lineage refusal.** Every one of the nine design spaces resolved.
The only `SimulationSpaceMap` refusals are the three MVHR **plant** zones (`MVHR-01/02/03`), which TAS
carries as zones and which have no design space at all - "does not resolve to exactly one design space",
correctly left out rather than name-matched. `Corridor_1` mapped and was refused for the separate reason
that nothing states its ventilation strategy.

Cross-check against the SAM-repo licensed acceptance of the same fixture: annual occupied hours
8,760 for the residential conditions and 4,745 for the kitchens, limits 262 and 142. Identical.

**The restamp seam, measured on the licensed runs.** Runs B and C had "Domestic Overheating" ticked, so
both went through `Modify.RestampSimulationZoneIdentity` on the workflow output. Both produced **no**
zone-identity note in the completion dialog: the seam is a complete no-op on the workflow path, because
`Modify.UpdateIds` had already stamped all nine spaces. And the DomOv XML each wrote carries nine
`DomOverheatZoneItem/GUID` values that are **byte-for-byte the nine TAS `ZoneGuid` stamps** on the
workflow model - checked pairwise, 9/9 match in run C
(`C:\TasOut\pui3\Report XMLs\PartOUI3DomOv.xml`). That is the identity the external TAS tool needs, and
the thing the fill exists to preserve on the Simulate-unticked path.

Those runs also settle the "differ by construction" point empirically: the same fixture, exported three
times, produced three entirely different sets of TAS zone guids (`Studio 1_0` was `{5F14C5BC-...}`,
`{B65C5A8D-...}`, `{1722CE03-...}`) while the SAM `space.Guid` values are stored in the `.sam` and did not
change. A SAM space guid therefore cannot be the TAS zone identity, and the exporter's silent fallback to
it cannot be harmless. The same observation is a second proof that the assessed model is the workflow
output: the stamps on it are freshly minted per run, which neither the loaded model nor the preparation
output could carry.

**Stale state - PASSED.** Prepare (`State=Prepared`, assessment disabled with "A Part O iteration is
prepared but not simulated"), then an unrelated space rename adopted through `SetJSAMObject` with no
`ExpectModification`: the run drops to `State=None` with

> The model changed after the Part O iteration was prepared, so the preparation and its overheating
> scenarios no longer describe it. Prepare the iteration again before simulating.

which is what the ribbon tooltip shows, and the production `Modify.AssessPartOTM59` then refuses with the
same sentence in its own message box. The stale run is not silently used. The reason is readable while the
button is unavailable because `RibbonButton_AssessPartOTM59` declares
`ToolTipService.ShowOnDisabled="True"`.

## Issues / blockers
- **Not empirically reproduced:** the duplicate-name collapse (defect 1) is confirmed by inspection
  (`List.Find` first-match + unconditional write); the old code was replaced rather than characterised in
  a test, and deliberately not re-created in one. The re-spelling half (defect 2) *is* demonstrated - a
  braced upper-case guid now survives verbatim, which the `out Guid` path could not have produced - and
  the *consequence* of having no stamp at all is now demonstrated end to end through the exporter
  (`AnUnstampedSpace_ExportsTheSAMSpaceGuid_UntilTheFillGivesItTheTasIdentity`).
- **The ribbon controls themselves are not exercised by the licensed run.** `AnalyticalWindow` was not
  instantiated; the enabled state and tooltip are the expression `RefreshPartOButtons` evaluates, which
  the harness logs from the same three `PartORun` reads, and `PartORunLineageTests` covers the run states
  behind it. Clicking the two ribbon buttons by hand once is still worth doing before release.
- Out of scope by instruction and not started: Seam 2 targeted design-airflow editing, Iteration 3,
  generic `AirHandlingUnitTemplate`, common-space `UV`, production ventilation-vocabulary normalisation.
- Still open elsewhere (not this repo): `Tas.TSDQueryTM59Results`'s `_analyticalModel` input is documented
  only as "SAM Analytical Model" - the workflow-model requirement is not stated at the seam. A one-line
  description fix in `SAM_Tas_Grasshopper`, deliberately not bundled here.

## Next step
1. Review PR #76 - the GitHub diff, both P1 resolutions and CI - then merge. Nothing is pending in this
   working tree.
2. Before release, click the two new ribbon buttons by hand once in the real application - the licensed
   runs drove the commands and their windows, not the ribbon.
