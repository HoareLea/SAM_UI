<!-- SPDX-License-Identifier: LGPL-3.0-or-later -->
<!-- Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors -->

# Part O — mixed dwelling strategies: PR3C (per-dwelling active cooling in SAM_UI)

**Status (28 Sep 2026): implemented, tested, accepted on licensed TAS through the real `SAM Analytical.exe`.**
[SAM-BIM/SAM_UI#134](https://github.com/SAM-BIM/SAM_UI/pull/134), branch `feature/parto-mixed-cooling-pr3c-2026-09-28`
from `sow/2026-Q3` `8971cfb`. Builds against SAM `3d6fa80a`, SAM_Systems `005c4fe`, SAM_Tas `e7cc0ed` (no change in any
of them). Domain authority: SAM `documentation/PartO-MixedDwellingStrategies-PR3B.md` (PR3B CLOSED); PR3A §8 plan.

## 1. Before PR3C (verified against the code)

| Stage | Production Mixed Design before PR3C | PR3B gate harness |
|---|---|---|
| Selection | No cooling control; `PartOMixedDesignConstraints.CoolingAllowed => false`, `checkBox_Cooling` disabled | strategy set by hand |
| Materialisation | `MaterialisePartODwellingStrategies(baseline, descriptors, scope)` - **no templates**, so SAM refused any cooled dwelling (`CoolingWithoutProductGuidance`) | descriptors **and** templates |
| Route | always `SimulatePartOMaterialisation` (IZAM) | Systems when SAM's record says so |
| SAM_Systems | not called | the `PartOIteration3Pipeline.Materialise` settings + `GuidanceTemplate = MVRE`, guidance for the cooled unit only - a private copy in the test |
| TPD / bridge / TM59 | IZAM TSD, `PartORun` | `ThermalSource` → `Route` → `ResultantTemperatures` → `PartOTM59Assessment.Assess(source, bridge TSD, materialiser's scenarios)` |

Both known gaps (PR3B §5: no mixed SAM_Systems call in production; no templates to the materialiser) were real.

## 2. What changed

**UI (one strategy-editing architecture, cooling orthogonal).** No new strategy types.
- Project rule `checkBox_Cooling` "Active cooling is allowed for MVHR dwellings" - `CoolingAllowed` settable, **default
  true**, persisted in the `.partomixed.json` constraints (a state without the key reads as allowed).
- Matrix column **Active cooling**: a word per row (`On` / `Off` / `—` where there is no MVHR supply) - plain text, the
  grid stays row/column virtualised; the Dwelling/Selected columns were narrowed by the new column's width.
- Bulk bar (now a WrapPanel) **Cooling on** / **Cooling off** on the selected rows (`PartOMixedDesignSession.SetCooling`):
  - On: refused whole for a row that is not MVHR ("Active cooling is on the MVHR supply: select MVHR (or Optimised MVHR)
    first for …"), and where the project rule forbids it. Natural + cooling is never created.
  - Off: always allowed, never blocked by another project rule, touches only cooled rows.
  - `SetMvhr` / `AcceptDesign` keep the row's cooling (product / airflow-basis edits are ventilation edits); `SetNatural`
    on a cooled row is refused until cooling is turned off (never dropped unseen). *Apply suggestions* may replace a
    cooled strategy with an uncooled suggestion - shown From/To in its preview before anything changes.
- Readiness "N with active cooling (whole building on the TAS Systems route)"; Selected text "· active cooling"; Check /
  Build outcome names SAM's cooled dwellings, product and **SAM's recorded** cooling airflow; Final line
  "· TAS Systems route, N dwelling(s) cooled".
- Screening `ActiveCooling` stays UNAVAILABLE (reworded: cooling is chosen per dwelling, not screened).
- Corridors are not rows (SAM's dwelling rule), so no toggle can reach them.

**Materialisation (SAM authority).** The session holds the catalogue's `Templates` beside its `Descriptors`;
`TemplatesOffered` is non-null only when the catalogue is offered (a cooled product must be selected against the
catalogue in the same call - SAM's rule). Check design and Build & Run pass both to
`MaterialisePartODwellingStrategies`. Refusals are SAM's, grouped by reason, on the dwelling they name.

**Route (no hybrid).** `RunPartOStrategySet` asks SAM's record: `Route == Systems` → the new
`Modify.SimulatePartOMaterialisationSystems` for the **whole** model; otherwise the unchanged IZAM
`SimulatePartOMaterialisation`. Systems route = the PR3B gate sequence over the existing Iteration 3 pipeline stages:
1. `Query.PartOMixedSystemsCall` composes ONE SAM_Systems call from SAM's record: rooms of every MVHR dwelling
   (`VentilationSystemGuids`; natural dwellings and common spaces stay out and free-run), guidance settings only for
   `CooledDwellings` via SAM_Systems `template.MechanicalVentilationGuidanceSettings(designSupply, designExtract)` (which
   calls SAM's rule) with SAM's single template match (`Analytical.Query.PartOCoolingTemplate`).
2. `PartOIteration3Pipeline.MaterialiseMixed` - MV base topology + `GuidanceTemplate = MVRE`, same schedule/name/flags as
   `Materialise`. `Modify.PartOMixedSystemsMaterialisation` checks it against SAM's record before any TAS time: exactly
   the cooled units carry guidance, each at SAM's recorded `CoolingOperatingAirFlow_Lps` (a disagreement refuses).
3. No-IZAM thermal source → ONE TPD (`<project>.tpd`) → guidance read-back: complete, one record per cooled unit, each on
   that unit's own air system (`Query.PartOMixedGuidanceReadBackRefusals`); hourly CSV `<project>_GuidanceOperation.csv`.
4. Bridge `<project>_Bridge.tbd/.tsd` (lineage: refused unless this run wrote it) → `PartOTM59Assessment.Assess(source,
   bridge TSD, materialiser's scenarios)` - the existing mechanical criterion for `ActiveTrimCooling`; no new rule.
5. Source model stamped (scenarios + provenance to the bridge TSD) and persisted as `<project>_Bridge.sam` through
   `PersistPartORunModel` - "Open final TM59 result…" reopens it with `PartORun.Restore`. `RunModelsSafe` also guards
   the bridge run-model name.

**Evidence.** `PartOMixedRunEvidence`: `Route` (SAM's record's), `Path_TPD`, `GuidanceSummaries` (TAS read-back, one per
cooled unit), persisted in the sidecar; `IsCurrent(baseline, descriptors, templates)` = SAM's cooled-record rule
(guidance fingerprint + re-derived airflow). An uncooled record is still `PartOMaterialisation:v1` with no cooling keys,
so PR2 sidecars never go stale.

**Gate harness.** `PartOMixedCoolingGateTests.Mixed` now calls the production `MaterialiseMixed`.

**Not duplicated in SAM_UI:** Part F airflow, accepted DesignAirFlow, product capacity, cooling range, cooling operating
airflow - every figure is read from SAM's record or the product template (pinned by
`CoolingAirflow_FollowsTheProductTemplate_NotSamUi`: moving the template default moves the recorded airflow and the
SAM_Systems unit together).

## 3. Files

- `SAM_UI/SAM.Analytical.UI/Classes/PartO/Mixed/PartOMixedDesignConstraints.cs`, `PartOMixedRunEvidence.cs`,
  `Query/PartOMixedDesign.cs`
- `WPF/SAM.Analytical.UI.WPF/Classes/PartO/Mixed/PartOMixedDesignSession.cs`, `PartOMixedDwellingRow.cs`;
  `Classes/PartO/PartOIteration3Pipeline.cs` (`MaterialiseMixed`); `Modify/RunPartOStrategySet.cs`,
  `Modify/ScreenPartODwellingStrategies.cs` (`BuildAndRunPartOMixedDesign` templates), `Modify/RunPartOMixedDesignCommand.cs`,
  **new** `Modify/SimulatePartOMaterialisationSystems.cs`, **new** `Query/PartOMixedSystemsCall.cs`;
  `Windows/PartOMixedDesignWindow.xaml(.cs)`
- Tests: **new** `PartOMixedCoolingTests.cs` (17), **new** env-gated `PartOMixedCoolingAcceptanceInspection.cs`;
  `PartOMixedDesignScalingTests.cs` (window test inverted: cooling is a project rule), `PartOMixedCoolingGateTests.cs`
- Evidence: `documentation/evidence/parto-mixed-pr3c/`

## 4. Validation

- `SAM.sln`, `SAM_Systems.sln`, `SAM_Tas.sln` rebuilt Release at the heads above (SAM had moved: SAM#163 PR2F-1);
  `SAM_UI.sln` Release 0 errors.
- Focused (`PartOMixed*` + `PartOIteration3*`) **306/306** (before the last additions); `PartOMixedCoolingTests` 17/17.
- Full WPF suite **1384/1384** Release (1365 at the PR2F-2 closeout + the new tests).
- **Red first** (`evidence/parto-mixed-pr3c/`): the updated window test fails on the unchanged base code
  (`red-first-base.txt`: checkbox disabled); six mutations each restoring one pre-PR3C behaviour on this branch turn
  `PartOMixedCoolingTests` red (`red-first-mutations.txt`): M1 route always IZAM (3 fail), M2 no templates to SAM (4),
  M3 legacy SAM_Systems call without `GuidanceTemplate` (2), M4 cooling gated (7), M5 `SetMvhr` drops cooling (1),
  M6 airflow-agreement check removed (1 - that test was added after M6 first survived).
- CI (#134): build + SPDX green on `bbd533c`.

Cases: A valid MVHR + cooling (unit + licensed); B Optimised + cooling - no accepted Optimised design inside the
product's 60-120 l/s range exists in any fixture, so a valid B was **not** manufactured (limitation, as PR3B); C
beyond the range (unit + licensed 143 l/s); D authored transfer from a cooled dwelling → SAM
`AuthoredAirMovementConflict` (unit); E corridor strategy → SAM `NotADwelling`, and the corridor is not a row (unit);
F cooling off after on → rebuilt (unit + licensed).

## 5. Licensed acceptance - real `SAM Analytical.exe` (dev build of `bbd533c`), UI Automation, 28 Sep 2026

Fixture: copy of the PR2 clean baseline `C:\TasOut\parto-mixed-pr2-2026-09-27\fixtures\SAM_zoningAM-CIBSEfutureZ1-MixedBaseline.sam`
(SHA-256 `89A8AC7B…9446`, unchanged after everything). Case: Z1 DSY1 2050s HIGH90, TAS solar, full year. Files in
`C:\TasOut\parto-pr3c-acceptance-2026-09-28\` (local only); driver = the PR2 scripts (`evidence/.../acceptance/scripts`),
log `acceptance/drive.txt`, inspections `acceptance/inspect-*.txt`, screenshots `acceptance/shots/`.

1. Mixed Design: Flat 1 Natural, Flat 2 MVHR automatic, Flat 3 MVHR + Nuaire MRXBOXAB-ECO5-AECV (MR-ECO-COOL-V).
   Cooling on for Flat 1 + Flat 3 → refused whole: "Active cooling is on the MVHR supply: select MVHR (or Optimised
   MVHR) first for Flat 1." Cooling on for Flat 3 → `On`, readiness "1 with active cooling (whole building on the TAS
   Systems route)".
2. Check design → "SAM can build this mixed design … Flat 3 (Nuaire … at 80 l/s); the whole building runs on the TAS
   Systems route. Nothing was simulated."
3. Build & Run (6.7 min) → "Final mixed run: FAIL — 1 dwelling pass · 2 fail · 0 not assessed · communal corridor:
   significant risk · TAS Systems route, 1 dwelling cooled". One `.tpd`, one bridge, run model `…_Mixed_Bridge.sam`.
   Inspection (`inspect-cooled.txt`, 24 PASS): route Systems; exactly one cooled dwelling, Flat 3, design duty 63/63 l/s →
   **80 l/s**; one TPD; TAS guidance read-back for **1 unit** (MVHR Flat 3: 1056 h fully elevated, **DX 1505 h** - the
   PR3B gate's figure, law met in 1056/1056 h); run model restores against the bridge results; scenarios Flat 1
   `BaseNaturalVentilation`, Flat 2 `BasePassive`, Flat 3 `ActiveTrimCooling`, corridor `DwellingIndependent`; no unit
   carries a supply setpoint; nothing unassessed. TM59: Flat 1 PASS; Flat 2 FAIL (Bedroom 2_3, Kitchen_4); Flat 3 FAIL
   (Kitchen_7 only - its cooled bedroom passes). Open final TM59 result… opens the production TM59 window from the
   bridge TSD (5 spaces, 2 pass, 3 fail, corridor significant risk).
4. Ribbon Save, close the app, relaunch on the saved model → Mixed Design shows Flat 3 `On`, the final run current.
5. Cooling off (Flat 3) → final shown STALE, Flat 3 "ran as … · active cooling"; Build & Run (79 s) → IZAM route, no
   Systems line. Inspection (`inspect-uncooled.txt`, 23 PASS): route Izam, no cooled dwelling, no TPD in the evidence,
   Flat 3 `BasePassive`, no cooling on the model; Flat 3 now fails Bedroom 2_6 as well. The saved model is still a clean
   baseline carrying only the selection; the cooled model was never read or patched.
6. Accepted-2B fixture copy: Flat 3 Optimised MVHR (retained 143 l/s design) + Nuaire + Cooling on → Check design →
   SAM's message box and the row's attention: "Cooling Airflow Outside Guidance: … would cool at 143 l/s - the
   dwelling's design of 143 l/s - beyond the manufacturer's published cooling airflow range of 60-120 l/s, where no
   cooling data exists." Build disabled; closed without saving; fixture and copy byte-identical.

## 6. Limitations / follow-ups

- No valid Optimised + cooled case on TAS (no accepted Optimised design within 60-120 l/s) - as PR3B.
- DX coils per air system are proven by unit test (real SAM_Systems graph) and by TAS's read-back of exactly one guidance
  unit; the native TPD was not read back by COM in PR3C (the PR3B gate did).
- A Systems-route run leaves `<project>.tpd`/`_GuidanceOperation.csv` in the output folder after a later IZAM rebuild
  (unreferenced artefacts; the IZAM run rewrites `<project>.tsd`, so a stale Systems result is never shown as current).
- Pre-existing: the matrix columns overflow the default 1240 px window (Needs attention scrolls right); the build outcome
  line quotes up to three run notes, which on the Systems route are long TPD conversion notes.
- Screening of cooling stays unavailable (one whole-building Systems run per strategy - PR3A §8).
- Next programme stage (PR4: large-project acceptance incl. TPD scale, SAM_Deploy pins) only on the owner's go-ahead.
