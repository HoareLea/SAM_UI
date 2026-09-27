<!-- SPDX-License-Identifier: LGPL-3.0-or-later -->
<!-- Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors -->

# Part O — mixed dwelling strategies: PR2 (SAM_UI dwelling strategies + mixed-model workflow)

**Status: implemented + correction pass (27 Sep), awaiting review.** Builds on SAM PR1 (SAM#150, `sow/2026-Q3` `3de02102`; closeout SAM#151
`0f866ec6`). Specification: SAM `documentation/PartO-MixedDwellingStrategies-PR0.md` §D–§F and
`PartO-MixedDwellingStrategies-PR1.md`. SAM, SAM_Tas and SAM_Systems are unchanged. No cooling (PR3), no deploy (PR4).

```text
open model = clean baseline + selected PartODwellingStrategySet (SAM authority, saved by the user)
   |                                  \
   | (optional) Screen strategies…      \  Build & Run Mixed Design
   v                                     v
copy + homogeneous set per strategy   SAM MaterialisePartODwellingStrategies(baseline)   -- PR1, pure
   -> SAM materialise -> private run      -> private PartORun -> RunPartOSimulation (full year) -> TM59
   -> TAS -> TM59 -> screening evidence   -> final mixed result (bound to SAM's PartOMaterialisationRecord)
```

## 1. User workflow

Simulate › Part O › **Mixed Design** (a new ribbon button beside Prepare & Run, which is unchanged). One window:

- **Summary**: the baseline's state (SAM's `PartOBaselineFindings`; a non-clean model is shown with SAM's reasons and
  nothing can be built), the selected design (`125 dwellings · 42 Natural · 71 MVHR · 12 Optimised MVHR · 2 need
  attention`), the final result, and screening - three separate lines for three separate authorities.
- **Project constraints and products** (collapsed): natural ventilation allowed / mechanical required; optimised
  airflow allowed; active cooling shown disabled ("Available after the cooling workflow is enabled"); whether MVHR
  products are selected from the catalogue, and the project pool it will use (the existing `PartOEquipmentSelection`).
- **Simulation case** (collapsed): weather, output folder, solar method - as the Hub.
- **Matrix**: one row per dwelling. Search, a filter (All / Needs attention / Failing in the final run / No strategy
  selected / Suggestion differs), group by zone category, select all shown.
- **Bulk bar** acting on the selected rows (one or hundreds): Natural ventilation, MVHR + product (automatic from the
  pool or one permitted product), Retain baseline design airflow, Clear, Apply suggestions….
- **Actions**: Screen strategies… (optional), Check design, Open final TM59 result…, Save selection, **Build & Run
  Mixed Design** (primary), Close.

Manual/guided: select strategies directly and build - screening is never required
(`Screening_IsOptional_ADesignCanBeBuiltWithoutIt`). The loop after a run: filter "Failing in the final run", change
those dwellings, build again; the whole model is rebuilt from the baseline with the full current selection.

## 2. Automatic screening

Screen strategies… opens a configuration listing `Query.PartOScreeningStrategies()` in the least-intervention order,
by engineering name (iteration numbers only in the detail line): Natural ventilation (1b), MVHR baseline (1a),
Selected-product MVHR (2), Optimised MVHR (2B - **unavailable in PR2**), Active cooling (3 - **gated for PR3**).
Cooling or a later strategy is one enum member plus one row in that list.

- **Each strategy = one full-year TAS run of the whole building**, built by SAM's PR1 materialiser from a COPY of the
  baseline carrying a homogeneous strategy set for the screened dwellings (selected-product MVHR = MVHR with the
  catalogue offered, as Iteration 2; MVHR baseline = generic units, as 1a). So screening and the final run are the
  same SAM call and differ only in the strategy set; there is no second implementation of any Part O rule, and a
  baseline SAM would refuse to mix is refused at screening with the same structured reasons.
- **Minimum screening**: after each strategy only dwellings with no passing, permitted strategy are screened by the
  next; a strategy the constraints forbid is not run; once none remain the rest are not run. **Full comparison**:
  every chosen screenable strategy for every dwelling. A TAS run is the whole building whatever it assesses, so the
  only saving is a whole strategy's run - that is exactly and only what is skipped. A dwelling outside a run's scope
  reads **NOT RUN**; no PASS is ever inferred.
- States: PASS, FAIL (a simulated verdict), NOT ASSESSED (simulated, no verdict), NOT RUN, UNAVAILABLE, STALE.
- Cancel keeps the completed strategies' evidence, discards the running one, and never touches the selection.
- Evidence is bound to `Query.PartOScreeningDesignFingerprint` (SAM's model digest of the baseline with the strategy
  set removed - so editing the selection does not stale screening, any building or project-setting change does) and,
  for product selection, SAM's `PartOCatalogueFingerprint`.

## 3. Suggested vs Selected

- **Suggested** = `UI.Query.PartODwellingSuggestion(zone, current evidence, constraints)`: the first strategy in the
  least-intervention order with a simulated PASS that the project permits (PR0 §D6); the reason names any passing
  strategy passed over by a constraint ("Natural ventilation passed but the project requires mechanical
  ventilation; MVHR baseline passed screening."). Derived every refresh, never persisted.
- **Selected** = the row's `PartODwellingStrategy`, the only thing built. Screening never writes it. **Apply
  suggestions…** lists every change (dwelling, selected now, suggested, why) in a window whose default button is
  Cancel; only Apply writes them, and only to the rows shown/selected. A manual override is untouched unless it is in
  the applied list.

## 4. Project constraints

`PartOMixedDesignConstraints` (NaturalVentilationAllowed, OptimisationAllowed; cooling always false) filter the
suggestion and gate assignment: an assignment of a disallowed strategy is refused whole (nothing half-applied); a
selection made before a rule changed is flagged "needs attention", never rewritten. They are not SAM authority and
are stored in the sidecar (§7), not on the model - they decide what the project permits, not what is buildable. The
product pool is the existing model setting, read, not duplicated; editing it stays in Prepare & Run.

## 5. Baseline preservation

The open model's only write is **Save selection** (also done automatically at the start of Build & Run):
`session.WithSelection()` → `SetJSAMObject` (a genuine model change - a legacy prepared run drops, as it should).
Screening and the final run materialise copies (the PR1 call is pure), simulate them in PRIVATE `PartORun`s (the
Iteration 2B capacity-envelope pattern) and never call `SetJSAMObject`. A run whose `<project>.sam` would land on the
open model's file is refused. Pinned: `MixedRun_CallsSam_SimulatesTheMaterialisedCopy_AndNeverWritesTheBaseline`
(fingerprint and JSON byte-identical), cancelled run, cancelled screening. Clean-baseline validation is SAM's
(`PartOBaselineFindings`) - there is no UI definition of "clean".

## 6. Materialisation and run path

`Modify.RunPartOStrategySet`: `MaterialisePartODwellingStrategies(baseline, catalogue?, scope)` → refusals returned
as SAM's `PartOMaterialisationRefusal`s (reason + zone + subject + message) → `SimulatePartOMaterialisation`: private
run `Prepare` → `RunPartOSimulation` (pre-simulation check, TAS workflow, results lineage, persisted run model with
provenance + scenarios + the materialisation record) → `Complete` → `PartOTM59Assessment.Assess` →
`Query.PartODwellingResults`. The TAS half is a delegate, so every test runs without TAS.

Refusals: grouped by reason in a message, attached to their dwellings in the "Needs attention" column (the filter is
switched to them), and those naming no dwelling listed above the grid. Editing a dwelling clears its own stale
refusal. **Check design** runs the materialisation only (5,000 spaces: 3.4 s here).

Progress: the existing `PartOProgressHost` (stages "Materialise the mixed model from the baseline", "TAS simulation
(full year) and TM59 assessment"; one stage per screened strategy). No invented percentage; cancellation is the host's
own token, which `RunPartOSimulation` already links.

Two small changes to existing code, both additive:
- `PartOTM59Assessment.OccupiedSpaceStatuses` - SAM's per-space overall status (`TM59AssessmentReportSpace
  .ComplianceStatus`) keyed by design space, built in `Assess`; nothing existing reads it.
- `RunPartOSimulation` skips `PersistPartORunResume` (the legacy Iteration 3 resume sidecar) for a model carrying a
  `PartOMaterialisationRecord`; legacy preparations never carry one, so legacy runs are unchanged.

**Dwelling tally** (`Query.PartODwellingResults`): FAIL if any occupied space failed; PASS only if ≥1 passed, none
failed and no space of the dwelling is unassessed; else NOT ASSESSED - the TM59 window's partial-assessment rule at
dwelling scale. **The run verdict is SAM's** production `TM59AssessmentReport.OccupiedSpaceComplianceStatus` (stored on
the evidence; it covers every occupied space judged, one in no dwelling row included): FAIL where it (or a dwelling)
failed, PASS only where it passed and every dwelling passed with no hole, else NOT ASSESSED. The communal corridor is
not a row: SAM's `CorridorRiskStatus` is shown beside the verdict ("· communal corridor: significant risk (Corridor_1)"),
never folded into it - SAM's own rule.

## 7. Result, reopen and staleness

- Final result per dwelling: TM59 word, failing spaces (count + first 12 names), and "ran as …" when the selection
  has since changed. **Open final TM59 result…** reopens the run's persisted model through `PartORun.Restore` and shows
  the existing TM59 result window (space-level evidence on demand, no TAS).
- **Sidecar** `<model>.partomixed.json` (schema `PartOMixedDesign:v1`, `PartOMixedDesignState`): constraints, last
  screening choice, the latest evidence per strategy, and the final run (`PartOMixedRunEvidence`: SAM's
  `PartOMaterialisationRecord`, the strategy set it ran as - SAM serialisation, TSD path/length/write time, run model
  path, per-dwelling results). **Why a sidecar, not a model parameter / `PartORunResume:v3` (PR0 F):** the record
  binds the result to `SimulationResultProvenance.Fingerprint(baseline)`, which digests every model parameter, so any
  pointer written to the baseline after the run would stale the result it points to; and the legacy resume sidecar
  sits beside a TSD that a reopened baseline has no way to find.
- On opening, everything is re-validated: final = SAM's `PartOMaterialisationRecord.IsCurrent(baseline, catalogue)`
  (strategies, catalogue, baseline) + the TSD's length/write time; an edited-but-unsaved selection also makes it
  "not current". A non-current final shows **STALE** per row and "Previous mixed run … is STALE and is not the current
  result: <SAM's reason>". Screening = fingerprints above → STALE cells, and never suggests.
- Selected strategies are always read from the model; the sidecar's "ran as" snapshot is never written back
  (`Reopen_RestoresEvidence_AndReportsStalenessHonestly_AndNeverInfersASelection`). Unknown schema → no state; an
  unknown outcome name → unreadable, never PASS. An unsaved model keeps the state for the session only (said so).

## 8. Scale

Rows are plain `INotifyPropertyChanged` objects, one per dwelling; the DataGrid is row- and column-virtualised,
recycling, `IsVirtualizingWhenGrouping`. Measured (Release, this machine; `PartOMixedDesignScalingTests`, 500
dwellings × 10 spaces = 5,000 spaces, no TAS): session open 0.42 s; filter < 1 ms; two 500/250-row bulk assignments
8 ms; screening refresh 4 ms; window shown in 0.41 s with **15 rows realised (14 grouped)** of 500; SAM mixed
materialisation 3.4 s. The expensive checks (SAM's baseline digest for the final record, screening fingerprints) run
on open/rebase/new result only, never per edit.

## 9. Tests

`PartOMixedDesignRunTests` (8), `PartOMixedDesignSessionTests` (13), `PartOMixedDesignScreeningTests` (9),
`PartOMixedDesignScalingTests` (4) - 34, over a fixture SAM really materialises (flats of a supplied bedroom + extract
bathroom joined by a partition, a communal-corridor zone). Covers every group in the PR2 brief §20.

## 10. Not in PR2 / follow-ups

1. **Optimised MVHR screening** (running 2B inside screening). Accepting an existing 2B result IS in PR2 (§11). Legacy
   2B still runs from Prepare & Run, on the legacy (overwrite) path - run it on a copy of the project.
2. Editing the product pool from this window (read-only here; edit in Prepare & Run).
3. Suggestion policy lives in SAM_UI (`Query.PartODwellingSuggestion`); PR0 D6 puts it in SAM eventually.
4. Cooling (PR3): the gate is visible and SAM refuses it.
5. No licensed TAS run of the mixed route yet (PR4 acceptance); SAM_Tas C9 diagnostic label unchanged.

## 11. Correction pass (27 Sep 2026)

After the owner's self-test review. Evidence: `documentation/evidence/parto-mixed-pr2-acceptance/INVESTIGATION-2026-09-27.md` §7.

**Accept optimised airflow…** (bulk bar; one selected dwelling; disabled where optimisation is not allowed or the
baseline is not clean): choose a completed Iteration 2B result model → `session.PreviewAcceptDesign` asks SAM
`Modify.AcceptPartODwellingDesign` (SAM-BIM/SAM#152: lineage by `PartFTerminalReference`, terminals realised for that
dwelling only, `SetSpaceDesignFlowRate` per space/direction, AD F floor, non-clean baseline refused) → SAM's refusals are
shown as they are, or every change "space direction: current → accepted l/s" in a Yes/No box whose default is No →
`session.AcceptDesign` adopts SAM's model as a pending baseline edit (`IsDirty`; Save selection writes it) and selects the
dwelling MVHR + `RetainedDesign` + SAM's fingerprint (a product already chosen is kept). No airflow in the strategy, no
rule in SAM_UI, other dwellings untouched, the whole design rebuilt from the baseline at the next build; screening and the
final run go stale because the building changed. Bulk acceptance later = the same SAM call chained per dwelling.

**Codex findings on c5f59bf (each with a regression that failed on c5f59bf):**
1. P1 catalogue setting: `CatalogueOffered` is a build input - its setter re-asks SAM's record with the catalogue as the
   next build offers it (`DescriptorsOffered`); the stale reason says which way the setting moved.
2. P1 run verdict: SAM's `OccupiedSpaceComplianceStatus` + `CorridorRiskStatus` + corridors persisted on the evidence (above).
3. P2 project test unit: `AllowedProducts` now asks SAM's `PartOEquipmentSelection.AllowedDescriptors` (default selection
   where none is set) - the test unit is offered exactly where SAM makes it eligible (ticked into a selected pool, or manual),
   and no longer offered under "all catalogue products", where SAM refused it; `CatalogueHasProducts` counts it.
4. P2 sidecar: an unreadable or missing dwelling result, an unknown verdict/corridor value, or a result set that does not
   match the assessed dwellings sets `ReadRefusal` - never current, never a pass, nothing dropped silently.

**Local high-effort review (same day), fixed with regressions red on 573183a:** Selected-product screening now uses the same
product rule (`UI.Query.PartOMixedProductsOffered`, shared with the session) so a test-unit-only project screens it; an
acceptance previewed against an earlier baseline is refused (`ConditionalWeakTable` of preview → baseline) instead of
dropping a later accepted edit; a sidecar listing a dwelling twice fails closed; the product check is hoisted out of the
per-row refresh; the close prompt names an accepted design. SAM side: a designer-added terminal refuses acceptance.

**Closeout:** the Accept file dialog starts in the simulation case's output folder (model folder when unset), with an empty
file name, and the open baseline itself is answered plainly before SAM is asked - no new persisted state.

Tests: `PartOMixedDesignCorrectionTests` (9), `PartOMixedDesignAcceptTests` (7); the fake TAS now returns a real SAM
`TM59AssessmentReport` (mechanical + corridor results) so the run verdict is SAM's in every test.
