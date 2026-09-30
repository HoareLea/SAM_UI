<!-- SPDX-License-Identifier: LGPL-3.0-or-later -->
<!-- Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors -->

# Part O model-state architecture (decision record)

**Status: APPROVED by the owner, 30 Sep 2026.** This record is the durable copy of the end-to-end workflow and
model-state review. The review read SAM `9fbe8353`, SAM_UI `92534d6`, SAM_Systems `aadb1b1` and SAM_Tas `057faf3`,
all on `sow/2026-Q3`. It was investigation only. The implementation happens in the PRs listed below, one at a time.

## Invariant

> **Part O may modify the design model only through an explicit user action that changes Part O input or design
> intent. Preparation, materialisation, simulation and result creation must never mutate the design model.**

Tests capture the design model after each explicit input commit and require it to stay byte-identical through every
calculation and result step.

**Explicit design inputs.** These may change the design model:
- Part F and zoning edits;
- Part O equipment choices (the Review window's selection mode and pool, `PartOEquipmentSelection`);
- the project test unit (`PartOProjectTestVentilationUnit`);
- the Mixed Design strategy selection (`PartODwellingStrategies`).

**Part O outputs.** These are never written into the design model:
- prepared systems;
- overheating scenarios;
- simulation provenance (`SimulationResultProvenance`);
- simulation results;
- materialised Part O systems;
- optimisation output;
- the accepted Iteration 2B result.

## Findings that led here

| # | Finding |
|---|---|
| F1 | The open model was progressively transformed. Prepare & Run derived from whatever was open. After a run the window adopted the result through `SetJSAMObject` without changing `Path`, so a plain Save wrote run output over the design `.sam`. The owner's "real model" was byte-identical to a run output. Because each case derived from what was open, 1b's `.prepared.sam` carried 1a's provenance. |
| F2 | Nothing records the design baseline. The only link is `PartOMaterialisationRecord.Fingerprint_Baseline`, which is a hash. |
| F3 | `NV 1`/`UV 1`/`MV 1`/`AHU1` are `Modify.AddMechanicalSystems` template scaffolding. They are not engineered plant and not Part O output. |
| F4 | Three stages treat that scaffolding differently. 1a calls it metadata. Iteration 3 excludes systems with no effective terminal duty. Mixed counts a named AHU as plant merely because it exists, and refuses `SharedSystem`. |
| F5 | Iteration 3 scopes the SAM_Systems input by identity (`PartOIteration3SystemScope`). Mixed passes the whole cluster, and SAM_Systems' D2 loop then refuses a unit-less NV/UV system. |
| F6 | Check design runs only SAM materialisation. Build & Run first builds the SAM_Systems graph, so the two can disagree. |
| F7 | No fixture has authored NV/UV next to Part O, or an `AddMechanicalSystems`-shaped MV+AHU. |

## Target architecture

```
Design model (engineer's .sam: design intent + Part O INPUTS, never Part O OUTPUTS)
   │  every case derives an in-memory case model from an explicit source
   ├─► Iteration 1a ─┐
   ├─► Iteration 1b  │
   ├─► Iteration 2 ──┼─► Iteration 2B (source: the Iteration 2 result)
   │                 └─► Iteration 3  (source: the 1a / 2 result)
   └─► Mixed Design  (source: the design model directly; no Remove Results)
```

1. The design model holds design intent and Part O inputs, never Part O outputs.
2. Every case derives from an explicit source model, never from whatever happens to be open.
3. After a Part O run the window stays on the design model, as Mixed Design already does. Results are reached
   separately, through Review or by opening the result.
4. A persisted `BaselineReference` (relative path first, absolute fallback, design fingerprint) is added to each case
   output in PR-5.
5. "Active for this assessment" is decided by identity plus effective duty, through one SAM query shared by 1a,
   Mixed and Iteration 3. Names are never used.
6. Opening a historical result never makes it a design baseline.
   - Before PR-5, Part O refuses and redirects to the design model.
   - After PR-5, it resolves the `BaselineReference` and offers to run from that design model.
   - A legacy result with no resolvable baseline is recovered with Remove Results.

**Remove Results** keeps its contract: remove Part O run state and keep authored design intent. It is a legacy
recovery tool, not a workflow step. It never removes authored or template systems.

## Owner decisions (closed)

1. **AHU / unit duty.** An AHU is inert scaffolding when all of these hold:
   - it has no finite stated design airflow;
   - it has no product or equipment selection;
   - it has no relevant `SpaceAirMovement`;
   - it has no effective connected terminal duty.

   An inert unit or system is preserved on the design, noted, and excluded from the assessment. A unit with real
   effective duty keeps the existing refusal semantics.
2. **After a Part O run the SAM_UI window stays on the design model.** The open design model is never replaced
   automatically by a prepared or result model.
3. **The accepted Iteration 2B design stays under `PartO/Iteration2B`.** It is not written back into the design
   model. A future explicit "Adopt accepted 2B design" action may promote it, but that is outside this plan.

## PR sequence (agreed order)

| Step | PR | Repos | Scope |
|---|---|---|---|
| 1 | **PR-4** Protect the design model | SAM_UI | The window stays on the design model. 1a/1b/2 derive from it. The explicit inputs are persisted to it deliberately. Prepare & Run is refused on a manually opened Part O result. Record: `PartO-DesignModelProtection-PR4.md`. |
| 2 | **PR-1** Part O system scope | SAM + SAM_UI | Move the Iteration 3 scope into a public SAM query. Scope Mixed by `Record.VentilationSystemGuids`. Check runs the same preflight. |
| 3 | **PR-2** Effective-duty classification | SAM | `AuthoredMechanicalSystems` applies decision 1. Inert units are noted, and duty-bearing ones keep the refusals. |
| 4 | **PR-3** Defensive SAM_Systems scoping | SAM_Systems | D2 unit resolution applies only to systems in the caller's scope. It may run in parallel with PR-1/PR-2. |
| 5 | Mixed Design acceptance gate | — | A licensed real-UI run on the existing `000000_SAM_AnalyticalModel-Cleaned.sam`, deleting nothing (`MV 1`/`AHU1`/`NV 1`/`UV 1` stay). Pass means Check PASS with notes, Build & Run on the Systems route, and the baseline byte-identical. |
| 6 | **PR-5** Persisted `BaselineReference` | SAM + SAM_UI | Relative-first resolution with fingerprint check, the "derived from" line, and "Run from design model?". |
| 7 | **PR-6** "Systems in this assessment" | SAM_UI | A read-only list fed by the PR-2 classification, with actionable refusals. |
| 8 | SAM_Deploy | — | Only after steps 1–7. |

**Deferred** until a real model needs it:
- a retain/supersede choice for duty-bearing authored systems;
- AHU view and remove UI, and removing a system also removing its orphaned AHU;
- "Adopt accepted 2B design".

## Compatibility

Changes are additive only. Existing `.partorun.json`, `.prepared.sam`, `PartOCase.json` and `.partomixed.json`
files and old absolute paths stay readable. The multi-repo order is SAM → SAM_Systems → SAM_Tas → SAM_UI.

The Part O MVHR type guid is internal to SAM.Analytical. Any classification SAM_UI needs must therefore come from a
public SAM query, never from a copied literal.

## Contradiction rule

If implementation evidence contradicts this architecture, that part of the PR stops. The contradiction is raised
with the owner, and this record is changed only after the owner decides.
