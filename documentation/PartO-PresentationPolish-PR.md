<!-- SPDX-License-Identifier: LGPL-3.0-or-later -->
<!-- Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors -->

# Part O / TM59 presentation polish (29 Sep 2026)

**Status (29 Sep 2026): code and focused/full automated tests complete; PR open against `sow/2026-Q3`, awaiting
CI. Not merged.** Branch `feature/parto-presentation-polish-2026-09-29` from `sow/2026-Q3` `f16976a`. Companion SAM
PR [SAM-BIM/SAM#168](https://github.com/SAM-BIM/SAM/pull/168) (branch `feature/tm59-report-margin-columns-2026-09-29` from SAM `sow/2026-Q3` `d9a8497b`) carries item 4 only.
The two PRs are independent at compile time: no public API changed. SAM_Tas and SAM_Systems are unchanged.

A short pass for a presentation. It changes presentation only, plus one real defect fix (item 6). No TM59 figure,
verdict, airflow or engineering calculation is changed.

## What changed

1. **Natural ventilation review (Iteration 1b).**
   - `PartOReviewSummary.HasMechanicalDesignDuty` is new. It is false when the preparation has no mechanical
     design duty (`DesignSupplyDuty_Lps` is NaN, the same test that already printed "No mechanical design duty").
   - When it is false, the review window's space table hides the Design SUP/EXT columns and keeps
     `Part F required (l/s)` as a reference. Copy All matches. The caption says this is natural ventilation with
     no mechanical design airflow.
   - The mechanical routes are unchanged.
2. **TM59 report provenance.** Each saved plain-text `*-TM59.txt` now has a `PART O CASE` block directly under the
   `CIBSE TM59:2017 OVERHEATING ASSESSMENT` title. It states:
   - iteration / scenario;
   - route;
   - thermal model scope, with the isolated-scope consequence;
   - weather, or "not recorded in this session" when the run was reopened;
   - the full path of the source TAS result;
   - the TM59 method and TM52 category.

   The lines come from `PartOTM59ResultSummary.RunFacts`, the facts the result window already shows. There is no
   second source of truth. SAM's report text follows unchanged.
   - Iteration 3 reference-case report: "Iteration 3 — Explicit system and cooling assessment · reference case"
     plus the reference iteration's own facts.
   - Iteration 3 system-case report: "Iteration 3 — … · system case: explicit TAS/TPD system (method)", the
     reference case, and the bridge results path. This goes through the new
     `PartOIteration3Pipeline.ReportProvenance`.
   - Iteration 2B round reports are unchanged (no provenance yet; see follow-ups).
3. **Transient second window.** The traced cause was the separate "Checking TM59 results" progress window that
   Hub **Review Results** opens around a TSD re-read (`Modify/AssessPartOTM59.cs`). Reopening an Iteration 3
   result does the same (`ReviewPartOIteration3Case`).
   - The Hub cannot host progress: it closes on every action (modal loop in `RunPartOWorkflow`).
   - Conservative fix: `PartOProgressHost` has a new optional `showDelay`, used only by these two read-only
     reviews (`PartOProgressHost.ShowDelay_Review`, 1.5 s).
   - A review that finishes sooner shows no second window. A slow read on a large model still shows its progress.
   - The default (0) is the previous behaviour, so every run, 2B and Iteration 3 progress window is unchanged.
4. **One margin convention (SAM).** The natural-ventilation table in `TM59AssessmentReportFormatter` now states
   each criterion as `C1 Actual | C1 Limit | C1 Margin | C1 Status | C2 … | Overall`. That is the same
   `Actual | Limit | Margin | Status` convention as the mechanical and >28 C sections, with Margin = Limit - Actual.
   Previously each criterion was one cell reading "37/110 (+73) PASS".
   - One row per space and the Overall column are kept.
   - The legend explains C1/C2.
   - No figure is recomputed.
5. **Result action matches the visible case.** After an Iteration 3 run or opened result, the Hub reopens
   "working in Iteration 3". Choosing an Iteration 3 method also counts.
   - In that state, if the selected method has a completed result, the primary action reads **Review Iteration 3
     result** and opens the Iteration 3 comparison. Previously it silently opened the Iteration 2 TM59.
   - Choosing another scenario, Prepare & Run, Optimise or a plain Review returns it to the scenario's own
     results.
   - The reference/system TM59 reports stay available inside the comparison.
   - Files: `PartOWorkflowWindow.Iteration3.cs` (`Iteration3InFocus`, `UpdateReviewTarget`), `RunPartOWorkflow.cs`.
6. **Iteration 3 saved-result reopen (root cause found and fixed).**
   - **Symptom.** A completed pairing reopened as "This saved result can no longer be shown for the model in front
     of you" / "This Iteration 3 pairing no longer reconciles", with rooms "now producing a TM59 result in
     Candidate B only".
   - **Root cause.** The review-side reconciler (`Query/PartOIteration3ReviewReconciliationRefusals.cs`) never
     received the information-only rule that SAM_UI#140 added to the run-side reconciler.
     - A served bathroom or ensuite has no occupied-space criterion on either side, and both sides report it as
       supplementary >28 C information only.
     - The run reconciles such a room; the reopen refused it, every time.
     - The "Candidate B only" wording came from `assessed_A ? "Reference A" : "Candidate B"`, printed when
       *neither* side was assessed.
   - **Ruled out.** Model mutation, identity/mapping, TM59 settings and stale caches. Both paths assess through the
     same `PartOTM59Assessment.Assess`, with the record's own bindings.
   - **Fix.** The review reconciler applies the same rule: it compares temperatures only and still refuses if
     either side lacks a series. The wording states what each case now calls the room.
7. **Iteration 3 wording (Priority B, minimal).**
   - Panel heading: "Iteration 3 — Explicit system and cooling assessment".
   - New system-case line: "Explicit TAS/TPD system with the selected product's operating and cooling behaviour.
     Validation methods are separate, under Advanced."
   - The glossary line says the same.
   - The reference line already read "Reference case: Iteration 2 — MVHR with manufacturer unit".
   - Iteration 3 is not called a validation workflow. The validation methods stay under "Advanced — validation
     methods".

## Files

- SAM_UI:
  - `Classes/PartO/PartOReviewSummary.cs`, `Modify/PreparePartOIteration.cs`, `Windows/PartOPreparationWindow.xaml(.cs)`;
  - `Modify/SavePartOTM59Report.cs`, `Classes/PartO/PartOTM59ResultSummary.cs`, `Modify/AssessPartOTM59.cs`,
    `Classes/PartO/PartOIteration3Pipeline.cs`, `Modify/PartOIteration3.cs`;
  - `Classes/PartO/PartOProgressHost.cs`;
  - `Windows/PartOWorkflowWindow.xaml(.cs)`, `Windows/PartOWorkflowWindow.Iteration3.cs`, `Modify/RunPartOWorkflow.cs`;
  - `Query/PartOIteration3ReviewReconciliationRefusals.cs`;
  - tests: `PartOIteration3ReviewTests.cs` (+2), `PartOPresentationPolishTests.cs` (new, 10).
- SAM: `SAM.Analytical/Classes/TM59AssessmentReportFormatter.cs`, `SAM.Tests/TM59AssessmentReportTests.cs`.

## Validation

- SAM `TM59` tests: **214/214**.
- SAM_UI focused Part O filter (presentation, Iteration 3, review, TM59, workflow/Hub, progress, result,
  consistency): **609/609**.
- Full SAM_UI WPF suite: **1470/1470** (1458 before this PR, plus 12 new tests), built against local SAM with SAM#168.
- **Mutation check (item 6):** with the pre-fix reconciler restored, `A_pairing_with_information_only_rooms_reopens_without_refusing`
  fails. With the fix restored, it passes.
- **Not done:** a licensed/native presentation-route walk (1b review → 1b TM59 → Iteration 2 TM59 → Iteration 3
  run → close/reopen). The owner should run it in Visual Studio before the presentation.

## Unresolved / follow-ups (after the presentation)

- **Priority B, left for later:** a persistent case selector/header (1a · 1b · 2 · 2B · 3) and moving Iteration 3
  higher up the page. `Optimise (2B)…` is still a global bottom action.
- The Iteration 2B round TM59 reports have no provenance block yet (`OptimisePartOTM59.Record`).
- The Iteration 3 comparison grid still shows "actual/limit" cells per case. That is a comparison table, not a
  TM59 report, so it was left as it is.
- **The two reconcilers should share one per-room helper** so they cannot drift apart again.
- **TPD/system performance (separate investigation).** From a light code read only:
  - Air systems are created one by one over COM (`SAM_Tas …/Convert/ToTPD/TPD.cs`, the `foreach (AirSystem …)`
    in the plant room, reporting "Air system n of m").
  - The whole TPD document is simulated in one `tPDDoc.Simulate` call.
  - The guidance/cooling evidence adds a `PlantRoom.SimulateEx` pass per plant room.
  - Open questions:
    - Can one configured air system be cloned as a template, with only zone assignments changed?
    - Can a complete system definition be materialised from structured data/JSON in one operation?
    - Would TAS/TPD simulate the energy centre faster as one plant room or many?

## Next step

Merge the SAM PR and this PR after CI is green, then add the post-merge `PROJECT_PROGRESS.md` closeout on
`sow/2026-Q3`. Before the presentation, walk the route above in the real app.
