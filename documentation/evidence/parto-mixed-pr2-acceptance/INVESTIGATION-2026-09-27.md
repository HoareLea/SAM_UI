<!-- SPDX-License-Identifier: LGPL-3.0-or-later -->
<!-- Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors -->

# Mixed Part O strategies — PR2 self-test and Optimised MVHR / 2B gap (27 Sep 2026)

PR SAM-BIM/SAM_UI#126, branch `feature/parto-mixed-strategies-pr2`. §1-§6 are the self-test on head `c5f59bf` (no
production code edited). **§7 is the correction pass the same day**: the §3 gap (Accept optimised airflow, SAM-BIM/SAM#152)
and the §5 Codex findings are fixed there; §8 lists workflow improvements. Not merged; PR3 cooling not started.

## 1. Execution modes (stated separately, never mixed)

| Mode | What | Where |
|---|---|---|
| **Native UI** | the real `SAM_UI\build\SAM Analytical.exe`, driven through Windows UI Automation (buttons invoked, DataGrid rows selected by `SelectionItemPattern`, windows captured with `PrintWindow`) | `scripts/` (driver), `logs/native-drive.log`, `shots/` |
| **Real licensed TAS** | every Build & Run and every screening strategy below is a genuine full-year TAS run (TBD/TAS3D processes observed, ~50-60 s each, `.tsd` 17 MB) | `logs/native-drive.log` |
| **Headless production seams** | env-gated xunit harness `PartOMixedDesignInvestigationTests` calling the real SAM / SAM_UI methods (no WPF, no TAS) | `logs/*.log` |

## 2. Test model

The owner's standard example `SAM_daily/2026-07-15 PartO/SAM_zoningAM-CIBSEfutureZ1.sam` (3 flats with their own
dwelling zones, 1 corridor zone, Part F data on 9 spaces, weather `Z1_DSY1_2050s_HIGH90_CIBSE_v1.1`).

**As saved it is not a clean baseline** and SAM refuses it (`logs/example-model-as-saved.log`, native `shots/36`):
478 result objects, 12 cluster design days, Part O systems `MVHR 1-3`, 8 Part-F-rewritten internal conditions, and an
authored shared `MV 1`/`AHU1` over Flats 2+3. Its corridor carries IC `Studio`. Every sibling variant in that folder is
also prepared and/or simulated; `SAM_zoningAM.sam` (the closest) has no Part F data and London TRY weather.

**Test fixture derived from it** (headless `Investigation_DeriveCleanBaseline`, `logs/derive-clean-baseline.log`) —
a TEST FIXTURE, not a user-facing sanitiser:
1. Map IC (TM59) exactly as the ribbon command's automatic mapping (TM59Manager, default text map/library, zone type
   `Flats`): corridor → `TM59_Communal Corridor (including pipework gains)`, bathrooms → `TM59_Bathroom`, Part F clones
   back to `Studio` / `Double Bedroom` / `1 Bed Apt. Kitchen`.
2. Remove all run output (18 space / 440 surface / 20 zone results, 12 design days) and all ventilation plant (6
   systems, 4 AHUs, 17 + 3 air movements, 9 terminals). Geometry, zones, Part F, weather, heating/cooling kept.
3. Result: `IsPartOCleanBaseline = true`; saved as a new file (the source is untouched).

Why a fixture: the UI has no way to reach a clean baseline from this model — **Results › Remove** clears space /
surface / model results only (not `ZoneSimulationResult`, not cluster design days), and nothing removes the Part O
MVHR systems. Local copies: `C:\TasOut\parto-mixed-pr2-2026-09-27\fixtures\` (not committed — project model data).

## 3. Optimised MVHR / Iteration 2B gap — exactly what is missing

Proven (`logs/optimised-gap-fixture.log`, `logs/accept-real-2b.log`, native run 3):

| Layer | State |
|---|---|
| SAM PR1 | **Complete.** `RetainedDesign` materialises from the baseline's `VentilationTerminal.DesignFlowRate_Lps`; terminal-less → `RetainedDesignStale` ("Accept the design onto the baseline's terminals first"); raised terminals under `PartFRequirement` → `DesignDiffersFromRequirement`; unbalanced raise → `MechanicalDesign`. Strategy JSON holds basis + fingerprint only. |
| SAM_UI PR2 selection | **Present.** "Retain baseline design airflow" records `MVHR + RetainedDesign + PartODwellingDesignFingerprint`; label "Optimised MVHR (retained design)"; refuses where no terminals ("…no design airflow to retain"). |
| The accept edit (PR0 D3) | **MISSING.** A clean baseline never carries terminals, so the retained design is unreachable in the UI. Nothing writes an accepted 2B airflow onto the baseline. |
| A 2B source in the mixed route | **MISSING.** 2B exists only on the legacy route (`OptimisePartOTM59`, re-`PreparePartOIteration` per round, overwrites the open model). Optimised screening = UNAVAILABLE. |

**The smallest correct seam — demonstrated with REAL 2B airflows**: source = the 26 Sep live Iteration 2B round model
`…-Opt10.prepared.sam` (same space guids). On a copy of the clean baseline:
1. `RealizePartFVentilationTerminals(spaces of Flat 3)` — terminals only, no ICs/systems;
2. per space and direction, `SetSpaceDesignFlowRate(space, dir, Σ source terminals)` — Bedroom 2_6 supply 63→143,
   Kitchen_7 extract 55→95, Ensuite_8 extract 8→48 l/s; no refusals (balanced, as 2B writes it);
3. baseline still clean; strategy := `MVHR + RetainedDesign + fingerprint`;
4. materialise Flat 1 NV / Flat 2 MVHR / Flat 3 Optimised → materialised, record current, terminals 143/95/48.

So a final design `Flat 01 Natural / 02 MVHR baseline / 03 Selected-product / 04 Optimised` **is supported by SAM
today**, with one airflow authority and no mutation of the clean source. What SAM_UI lacks is the explicit per-dwelling
**"Accept optimised airflow…"** design edit: pick a completed 2B result, match by space guid + flow direction (PR0 D3
says lineage `PartFTerminalReference`), write via the two SAM calls, record `RetainedDesign` + fingerprint, refuse on
unmatched / ambiguous / unbalanced. Recommended placement of the matching rule: SAM (`Modify.AcceptPartODwellingDesign`
or similar) so the lineage rule is engineering authority, with a thin SAM_UI command. **Optimised screening** (running
2B inside mixed screening) is a larger, separate follow-up; accept-from-an-existing-2B-result does not need it.

## 4. Native results (real exe, real TAS)

| # | Behaviour | Result | Evidence |
|---|---|---|---|
| 1 | Clean baseline recognised; 3 dwellings; corridor auto-included; screening NOT RUN / UNAVAILABLE | PASS | shots/01 |
| 2 | Manual design without screening (F1 NV, F2 MVHR auto, F3 MVHR + Nuaire MRXBOX); Build enabled only when all selected | PASS | shots/02 |
| 3 | UI refusal: retain design without terminals → clear message, selection unchanged | PASS | log 10:47:49 |
| 4 | Check design (materialise only) → "SAM can build this mixed design … Nothing was simulated" | PASS | log 10:48:03 |
| 5 | **Build & Run, one combined model, one annual TAS run, TM59** → FAIL (F1 PASS; F2, F3 FAIL bedroom+kitchen); strategy beside result; filter jumps to failing | PASS | shots/07; `_Mixed.sam` 2 MVHR units, 4 scenarios incl. corridor `DwellingIndependent`; one `.tsd` |
| 6 | Screening evidence not in final state | PASS | sidecar `Screening: []` after run 1 |
| 7 | Edit one dwelling → final STALE immediately with reason | PASS | shots/08 |
| 8 | **Rebuild from clean baseline** (F2 → NV): run-2 model has F2 with authored ICs back, no terminals/unit; F3 identical | PASS | cmp in log; impossible by patching (IC rewrite irreversible) |
| 9 | Open final TM59 (persisted run, no TAS): FAIL, 5 spaces, communal corridor "Significant risk" | PASS | shots/12 |
| 10 | Minimum screening, 3 real runs: NV 1/3 pass; MVHR baseline 0/2; product 0/2; F1 NOT RUN for the MVHR strategies; "selection is unchanged" | PASS | shots/17 |
| 11 | Suggested ≠ Selected; manual override survives; Apply suggestions preview (1 change, reason) → Cancel changes nothing → Apply changes only that row | PASS | shots/18, log 10:57 |
| 12 | Returning the selection to what ran → final current again (intent-based staleness) | PASS | log 10:57:23 |
| 13 | Multi-select bulk product assignment (2 rows → XBC15) | PASS | shots/21 |
| 14 | Save → `.sam` = clean baseline + strategy set only (after 2 mixed + 3 screening runs) | PASS | cmp in log |
| 15 | Restart → selection, final (current), screening restored; final TM59 reopens | PASS | shots/23, 27 |
| 16 | TSD rewritten → final STALE on reopen (screening still valid); restored → current | PASS | shots/25 |
| 17 | **SAM structured refusal** (MVHR at requirement over accepted 2B terminals): grouped message, attached to the row's Needs attention, Build disabled | PASS | shots/29, 30 |
| 18 | **Optimised MVHR in one combined run** (F1 NV / F2 XBC15 / F3 retained 2B 143/95/48) — real TAS | PASS (workflow); TM59 FAIL for F2/F3 | shots/34; `_Mixed.sam` terminals |
| 19 | Non-clean model (original example) → SAM findings listed, Build/Save disabled | PASS | shots/36 |
| 20 | Scale | not repeated (500 dwellings / 5,000 spaces already measured in PR2) | — |

Driver note: the first bulk attempt selected nothing (a `powershell -File` array-argument quirk in the driver) and
applied to the previously selected row; re-done with direct UIA selection — not a product defect.

## 5. Findings

Open Codex review on `c5f59bf` (unaddressed; confirmed against the code):
- **P1** `PartOMixedDesignWindow.CatalogueChanged` does not re-validate the final result: `PartOMixedRunEvidence.IsCurrent`
  uses the run's own `CatalogueOffered`, so unticking the catalogue leaves a result "current" that would now build
  generically.
- **P1** `PartOMixedRunEvidence.Overall` tallies dwellings only; an automatically assessed common space (corridor) is not
  in the run verdict. Observed live: the TM59 window shows the corridor "Significant risk" while the matrix summary
  never mentions it. Whether SAM's production overall status would read FAIL there needs a check against
  `TM59AssessmentReport` before fixing.
- **P2** project test ventilation unit not counted as an available product (catalogue off where only the test unit exists).
- **P2** an unreadable dwelling result in the sidecar is dropped instead of refusing the evidence.

Presentation (minor): screening column headers and cells truncated at the default 1240 px width ("MVHR baselir",
"UNAVAILABI"); the Show filter's UIA item names read `[All, All dwellings]`; `PartOMixedDwellingRow.HasDesignTerminals`
is always constructed `false` and unused (dead).

## 6. Recommendation

- Fix the four Codex findings on PR2 before the owner's manual test (the P1 corridor case is reachable on this model).
- Decide the "Accept optimised airflow" seam (§3) — SAM rule + SAM_UI command, a separate small PR before PR3 — or accept
  PR2 as-is with Optimised MVHR reachable only for baselines that already carry design terminals.

## 7. Correction pass (27 Sep 2026, same day)

**SAM:** SAM-BIM/SAM#152 `Modify.AcceptPartODwellingDesign` (branch `feature/parto-accept-dwelling-design`, `2c352d3e`).
**SAM_UI:** the four Codex findings fixed, **Accept optimised airflow…** added (PR2 record §11).

Regressions: `logs/codex-regressions-RED-on-c5f59bf.log`. All 7 correction regressions failed on the `c5f59bf` code;
the 6th/7th-finding test was re-checked against the `c5f59bf` session + command files after a clean rebuild. All pass after.
Real-data seam: `logs/accept-real-2b-via-sam.log`. SAM accepts the live Opt10 design for each flat; Flat 3's fingerprint
`24e136de…` is identical to the hand-composed seam that ran through real TAS in §4 #18.

### Native regression (real exe `WPF dll 11:37`, real licensed TAS) - `logs/native-drive.log` after "NATIVE REGRESSION"

| # | Required | Result | Evidence |
|---|---|---|---|
| 1 | Clean baseline opens | PASS | shots/38 |
| 2 | Natural + MVHR + Optimised selected through the real UI, no hand-made baseline: F1 Natural, F2 MVHR, F3 **Accept optimised airflow…** → real 2B file `Flat-2B-Opt10.sam` through the file dialog | PASS - "1 Natural · 1 MVHR · 1 Optimised MVHR" | shots/44, 45 |
| 3 | Only the intended dwelling changes: the confirmation lists F3's three airflows only (63→143, 8→48, 55→95 l/s); F1/F2 rows unchanged; the saved `.sam` carries terminals for F3 spaces only | PASS | shots/44; saved-file check in log |
| - | Wrong file (the clean baseline itself, pre-selected by the dialog) → SAM refusal "states no design terminal … A partial design is not accepted. Nothing was accepted." | PASS (safe) | shots/41 |
| 4 | Check design → "SAM can build this mixed design (… 1 Optimised MVHR). Nothing was simulated." | PASS | log 11:47:03 |
| 5 | One mixed Build & Run (real TAS, 62 s) | PASS | shots/48 |
| 6 | Corridor in the project result: "Final mixed run: FAIL — 1 dwelling pass · 2 fail · 0 not assessed · communal corridor: significant risk (Corridor_1)"; sidecar `OccupiedSpaceComplianceStatus: Fail`, `CorridorRiskStatus: SignificantRisk`; the corridor is not a row | PASS | shots/48 |
| 7 | Catalogue unticked → final STALE immediately, Open result disabled; re-ticked → current | PASS | shots/49, 50 |
| 8 | Save → restart → reopen: selection (incl. Optimised), final (current, with corridor), Open result enabled | PASS | shots/52 |
| 9 | Saved source `.sam`: no systems, units, scenarios, results, record; strategy set + F3's accepted terminals only | PASS | cmp in log |

Tests: SAM `SAM.Tests` **2545/2545**; SAM_UI `SAM_UI.sln` Release 0 errors; WPF suite **1299/1299** (1279 + 9 correction +
6 accept + 5 env-gated investigation harnesses, which are no-ops without their variables). A local high-effort review
found 3 more defects, fixed with regressions shown red on 573183a (`logs/review-regressions-RED-on-573183a.log`) plus one
SAM refusal (designer-added terminal). The native regression above ran on 573183a; the review fixes touch no path it
exercised except screening availability (not in that regression) and the accept guard (same single-preview flow).

## 8. Potential workflow improvements discovered while using PR2

Captured while implementing and driving the native regression. None of them is implemented, apart from the one-line
wording of the catalogue-setting stale reason (a clarity defect in this pass's own fix).

| # | User problem | Improvement | Why it helps | Where | SAM or UI | Size |
|---|---|---|---|---|---|---|
| A | After a run, the engineer must open the TM59 window to see *why* a dwelling failed (criterion, hours), then come back | Row detail/side panel: failing spaces with SAM's criterion (Crit 1/2 or hours), taken from the persisted run report. "Failing in the final run" is already the automatic filter after a build | Failure → reason → change → rebuild without leaving the matrix | Later (post-PR2) | UI-only (reads SAM report) | Medium |
| B | 2B results are accepted one dwelling at a time; 100+ dwellings need a batch | "Accept optimised airflow…" over selected rows: one 2B result file, SAM's accept chained per dwelling, one confirmation table, all-or-nothing | Scales without new authority (the SAM call is already per dwelling and composable) | Later (with 2B screening) | UI (SAM unchanged) | Small–Medium |
| C | Suggested reason is one sentence; it cannot yet say "Natural and MVHR failed; Optimised passed" | Reason built from each strategy's actual screening outcome + constraint, in least-intervention order | Explains the suggestion from evidence, never invented | Later (needs Optimised screening) | UI now; SAM when suggestion policy moves (PR0 D6) | Small |
| D | Row shows "Optimised MVHR (retained design)" but not the airflow it will run at | Compact per-row detail: requirement / design / unit capacity, read from Part F data, baseline terminals and SAM's selection - labelled distinctly, no stored copy | Current vs proposed at a glance; keeps the one authority | Later | UI-only (reads) | Medium |
| E | Refused baselines show SAM's full technical findings; engineer wants the gist | Headline "Baseline cannot be used - it contains previous Part O simulation state" with SAM's findings under "Details" (no cleaning function) | Faster diagnosis, no destructive action | PR2-size, but presentation-only → next UI pass | UI-only | Small |
| F | Large projects: "Needs attention" mixes SAM refusals and constraint conflicts | Readiness counts per reason (no strategy / refused / constraint) with click-to-filter | Locating unresolved dwellings on 500 rows | Later | UI-only (derived) | Small |
| G | Screening re-runs a strategy even when nothing changed | Skip a strategy whose evidence is current (fingerprints of baseline, catalogue, constraints, simulation case already bound) - offer "rerun anyway" | Saves whole-building TAS runs without weakening freshness | Later | UI (evidence already fingerprinted; add simulation-case fingerprint) | Small–Medium |
| H1 | The bulk bar has 7 controls; at scale it is crowded | Group into "Strategy ▾" (Natural, MVHR…, Optimised…) + product picker | Progressive disclosure | Later | UI-only | Small |
| H2 | Matrix columns truncate at 1240 px ("MVHR baselir", "UNAVAILABI", failing-space names) | Shorter headers / tooltip, wider default, "Screening" group header | Readability | Next UI pass | UI-only | Small |
| H3 | The accept confirmation is a message box; for many dwellings a table is better | Reuse the Apply-suggestions change window (dwelling / current / proposed / why) | Consistency, scale | With B | UI-only | Small |
| H4 | The Open dialog pre-selects the open project file, an easy wrong pick (SAM refuses it safely) | Start the dialog in the model folder's run subfolder / filter to 2B round files | Fewer wrong picks | Next UI pass | UI-only | Small |
