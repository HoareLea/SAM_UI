<!-- SPDX-License-Identifier: LGPL-3.0-or-later -->
<!-- Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors -->

# Part O acceptance follow-ups: TM59 header, Mixed Design sidecar, prepared-model links

**Status (30 Sep 2026): implemented and tested. The PR is open against `sow/2026-Q3` for review and NOT merged.**

- Branch `fix/parto-acceptance-followups-2026-09-30`, from `sow/2026-Q3` `7619c41` (the SAM_UI#147 closeout).
  SAM_UI#147 was already merged as `4971a3f7`.
- SAM_UI only. SAM and SAM_Tas are unchanged.
- These are the three findings of the 30 Sep final real-app acceptance. Its evidence is on the local branch
  `docs/parto-final-acceptance-2026-09-30` (`4bb923c`), which is not pushed and is not part of this PR.
- The output-folder architecture is unchanged. Nothing touches the 2B UX, new cases, TPD performance or other
  reporting.

## A. TM59 Part O provenance header: code change

**Root cause.** `PartOIteration3ReportProvenance` headed the reference-case report with a
`Case: Iteration 3 — … · reference case` line placed ABOVE the run's own `Iteration / scenario: Iteration 1a …`.
Each line was true, but the order read as though the Iteration 1a TSD belonged to Iteration 3.

**Change.** Presentation only. The header now states what the results file physically is first, then any
assessment it serves:

```text
PART O CASE
Scenario:             Iteration 1a — MVHR design duty (no manufacturer unit)
Assessment context:   Iteration 3 — Reference case
Route: … Thermal model scope: … Weather: … Source TAS result: … TM59 method: …   (unchanged)
```

- `PartOTM59ResultSummary.ReportProvenance(run, report, lines_Context, scenario, path_TSD)` states the scenario first.
  This is the run's own, unless `scenario` gives another. `lines_Context` follows it directly.
- The label is now `Scenario`, the same label the TM59 result window uses (it was `Iteration / scenario`).
- A standalone report has no context lines, so it prints no `Assessment context`.
- The Iteration 3 system case (the bridge TSD, which IS Iteration 3's) keeps `Scenario: Iteration 3 — … system case: …`
  and `Reference case: <iteration>`. It needs no context line, because its scenario already says Iteration 3.
- The provenance model, the source TSD, the compliance calculations and the SAM report text are all unchanged.

## B. `.partomixed.json` written after a Mixed Design refusal: code change

**Lifecycle.**
- `RunPartOMixedDesign` writes `PartOMixedDesignState` (`<model>.partomixed.json`) each time the window closes,
  whatever the action. It also writes after Screen and after Build & Run.
- The file is project state for the mixed workflow: constraints, screening choice and mode, screening evidence,
  and final-run evidence. It is not a selection; the selection lives on the model.
- Where no sidecar existed, the session starts from a default state. So a refused open followed by a close CREATED a
  default sidecar beside a run-output model (acceptance: `Iteration2/tas/…partomixed.json`, 213 bytes).
- **No valid workflow needs it.** A refused model can never be built or screened: SAM refuses it, the buttons are
  disabled, and nothing is cleaned back. The sidecar is named from the model file, so it can never reach the clean
  baseline's own sidecar.

**Change.**
- `PartOMixedDesignSession.OpenedOnCleanBaseline` records whether the session was opened on a clean baseline.
- `Modify.WritePartOMixedDesignState` is the one write seam, and it writes only for such a session. A refused
  session creates no sidecar and leaves an existing one byte-for-byte unchanged.
- A clean-baseline session persists exactly as before: on close, after Screen, and after Build & Run.
- The flag is taken at open. So a clean session that later adopts an edited baseline still keeps its edits.
- The baseline validation (SAM `Query.PartOBaselineFindings`) is untouched.

## C. Previous-run links in `.prepared.sam`: investigated, no code change

**What is carried.**
- A `.prepared.sam` holds `PartORun.AnalyticalModel_Prepared`, written by `PersistPartORunResume`.
- Preparation copies the open model, and after a run the open model IS that run's output. So the prepared model
  carries the previous run's model-level `SimulationResultProvenance` (its `Path_TSD` names the previous case's TSD)
  and `OverheatingScenarios`.
- Verified on the acceptance files: the 1b `.prepared.sam` holds 1a's two parameters, byte-identical to the 1a
  `.sam`, and 2's holds 1b's. SAM's preparation neither strips nor re-stamps them.
- This is leftover session state, not engineering input.

**Every reader, traced.**

| Reader | What it uses | Effect of the carried link |
|---|---|---|
| Prepare & Run: `PartORun.Prepare` / `RunPartOSimulation` | The scenarios are held by `PartORun`. The workflow's model is stamped with the run's scenarios, and with fresh provenance when the run completes. | Replaced |
| Results / Review, and the Iteration 3 Reference A | `AnalyticalModel_Assessment`, the freshly stamped workflow model | None |
| Iteration 3 Candidate B | The TAS output of the prepared model, re-stamped (scenarios + bridge provenance) before it is persisted | Replaced |
| Resume (`TryResume`) | The prepared model's cluster, zones and systems, bound by `Fingerprint`, which EXCLUDES both parameters | Not read, and in no identity |
| Save / reopen (`PartORun.Restore`) | The model's record, accepted only if the TSD length/time, the design fingerprint and the scenario fingerprint all match | Fails closed |
| Mixed Design | SAM baseline findings | Flagged as run output, which is correct |

**Regression.**
- `PartOPreparedModelCarriedLinkTests` drives the real `RunPartOSimulation`, with TAS replaced at `PartOWorkflowRunner`,
  on a prepared model that carries a previous case's link.
- A completed run is linked only to its own TSD and scenarios. It reopens and resumes, and the resumed
  `.prepared.sam` still carries the old link, which is outside the fingerprint the resume is bound by.
- Opening the `.prepared.sam` itself is refused, never paired with the previous TSD.
- A run that does not complete (one day) returns the model Simulate adopts as the open model, still with the old
  link. Saved and reopened, it is refused.
- Nothing is written into the previous case's folder.

**Why nothing was cleared.** Clearing is a persisted-state change with no reader to fix. The fail-closed checks
already make the link inert. The only visible effect: after an incomplete run, a reopen refusal names the previous
case's TSD. If the owner wants cleaner files, the seam is `PreparePartOIteration.ConcludePartOReview`, removing both
parameters from the adopted copy before it becomes the open model. That belongs in its own PR. The fingerprints are
unaffected, since both parameters are excluded.

## Files

- `WPF/SAM.Analytical.UI.WPF/Classes/PartO/PartOTM59ResultSummary.cs`: `ReportProvenance` puts the scenario first,
  then the context (A).
- `WPF/SAM.Analytical.UI.WPF/Modify/PartOIteration3.cs`: reference and system case provenance (A).
- `WPF/SAM.Analytical.UI.WPF/Classes/PartO/Mixed/PartOMixedDesignSession.cs`: `OpenedOnCleanBaseline` (B).
- `WPF/SAM.Analytical.UI.WPF/Modify/RunPartOMixedDesignCommand.cs`: `WritePartOMixedDesignState` (B).
- Tests:
  - `PartOPresentationPolishTests.cs`: 4 new tests, 4 updated (A).
  - `PartOMixedDesignStatePersistenceTests.cs`: new, 4 tests (B).
  - `PartOPreparedModelCarriedLinkTests.cs`: new, 3 tests (C).

## Evidence

- Pre-fix proof, run in place:
  - A: with the two A files reverted to base, the 4 new A tests fail.
  - B: with the guard removed, the 2 refusal tests fail, and the clean-baseline and unsaved tests still pass.
  - Both pass with the change.
- The 3 C tests pass against the unchanged production code. They pin the behaviour, not a fix.
- Focused: `PartOPresentationPolishTests`, `PartOMixedDesignStatePersistenceTests`, `PartOPreparedModelCarriedLinkTests`
  all pass (18 + 4 + 3).
- Full WPF suite (Debug, local): 1530/1530 passed (base 1519 + 11 new).
- No licensed TAS rerun. No change touches simulation execution; A is text, B is a file write outside TAS, and C
  has no code change.

## Not verified / risks

- The real-app behaviour of A and B was not re-run in the GUI. Both are covered at their production seams (the
  report writer, and the command's one state-write helper).
- CI (the Windows build) runs on the PR.

## Next step

Owner review of this PR, then merge into `sow/2026-Q3`. After merge, add the `PROJECT_PROGRESS.md` closeout (with
the merge SHA) as a docs-only commit on `sow/2026-Q3`. Decide separately whether to push the acceptance evidence
branch `docs/parto-final-acceptance-2026-09-30`, and whether the optional C clean-up is wanted.
