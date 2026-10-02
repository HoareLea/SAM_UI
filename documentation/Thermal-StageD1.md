# Thermal Stage D1 - opaque construction alternatives (2 Oct 2026)

Plan: `documentation/plans/Thermal-Performance-Review.md` (Stage D). Builds on Stage C (#169, edit in the panel), D0 (#172, colour by property) and the
existing U-value engine (PR1-PR2). Target journey: **select wall → target U → alternatives**, without a second calculation engine and without touching the
model until Apply.

## What the user sees

Typing a target U in an opaque row now shows, under the preview line, an **Alternatives** list:

* **Generated** - the thickness variant the existing calculation produces (unchanged; still the row's change by default).
* **Existing model** - constructions already in the model, with how many panels use them.
* **Library** - constructions from the default construction library, "not in the model yet".

Each line shows the construction name, its **U-value on the row's heat-flow basis**, "meets the target by 0.019" / "0.003 above the target", where it comes from
and compatibility notes (made for another panel group, a name the model already has - it is added under a numbered name -, "Adds 4 materials to the model: ...",
a material that differs from the model's - which blocks it). The list contains the constructions that **meet the target or are within 10 % above it**, ordered
meeting-first, those made for the panels' own group before the others, then by closeness (the least over-insulated first), then near misses the same way, the model's own before a library's; at most 30. **Nothing is chosen for the
user**, however close a construction is: the generated variant stays selected until the user picks another line. Choosing an existing construction replaces
the generated variant as the row's change (preview "U 0.260 → 0.181 W/m²K · SIM_EXT_SLD_FLR Exposed (Library)", the scope, the check before Apply) and
**Apply = one commit = one Undo**, composing with the other rows (glazing, other walls) in the same change set.

## Architecture (reuse, nothing duplicated)

| Part | Where | What |
|---|---|---|
| Batch U-values | `Interfaces/IConstructionUValueEvaluator.cs`, `Classes/Thermal/TasConstructionUValueEvaluator.cs` | The existing Tas `ThermalTransmittanceCalculator.Calculate(guids)` already takes any number of constructions in one TCD run (the target evaluator uses it for "as is / min / max"); the evaluator sends a **pool** in one run per chunk of 40, on one STA worker, read on the row's heat-flow basis. Requests are served **in order, none dropped** (the target evaluator's single-flight "newer supersedes older" would cancel another row's batch). A construction with a material missing from the pool is reported "not calculated", never sent to Tas. The worker thread starts with the first request. |
| Cache | `Classes/Thermal/ConstructionUValueCache.cs` (in `ThermalEditServices`) | Session cache keyed by the **content** that decides U (each layer's thickness and material definition, heat-flow direction, external) - not Guid or name - so another target, another row or the same construction in two pools never asks Tas again; a changed material or layer is a different key. Failures are not cached. (Analogue of `GlazingSourceCache`, in memory.) |
| Pool / source | `GlazingSource.ConstructionsFromModel`, `ConstructionsFromDefaultLibrary`, `GetConstructions()` | `GlazingSource` already holds a `ConstructionManager` (constructions **and** aperture constructions + materials); it now also serves the opaque ones, so a source is one thing for panels and apertures (D2 builds on this; the type keeps its name for now). |
| Candidate | `ConstructionCandidate` | Mirrors `GlazingCandidate`: **Guid identity**, `MaterialsToAdd`, `MaterialIssue` (missing from its source / differs from the model's same-named material). The "same material" rule moved to `MaterialIdentity` (shared, behaviour unchanged). |
| List logic | `ConstructionAlternatives` (+ `ConstructionAlternativeRow`) | WPF-free. Asks the evaluator once per pool for the uncached U-values (async, stale results harmless because the cache is keyed by basis), builds the rows, holds the choice, builds the `SetConstructionRequest`. Created on the **first valid target** of a row, so a row that is only looked at, or whose target is not a number, asks nothing. |
| Apply | `Modify/SetConstruction.cs`, `SetConstructionRequest/Result`, `ThermalChangeSet.ConstructionRequests` | Model-only core in the style of `SetUValue`/`SetGlazing` on one clone: scope explicit (all using / selected only; "don't assign" refused - an existing construction is always assigned), only the chosen construction (its Guid kept; a clashing name gets a numbered suffix) and the materials the model lacks enter; the source stays as a stored object when its last panel moves (no silent delete); `UpdateConstructions` post-step. `ApplyThermalChange` runs it between the generated (thickness) changes and glazing, runs the whole-model Tas refresh once, `SetJSAMObject` once. Conflict rule: one opaque change per source construction. |
| Check before Apply | `Query.ThermalCheckDiff` | The new request is checked through the same scoped panel rules as `SetUValue`. |

## Decisions and semantics

* The U of a candidate is calculated on **the row's own basis** (direction / external from the affected panels), the same basis as the generated variant, so the numbers are comparable.
* "Closely matches" = up to **10 %** above the target (`ConstructionAlternatives.CloseFactor`). Simple ordering only; no ranking.
* A candidate made for another panel group is marked, **not hidden** and not blocked (it can be a legitimate choice). Note: the opaque model check has **no rule** comparing a construction's Default Panel Type with its panels (only the aperture check has), so the note does not claim ModelCheck will warn.
* A material that differs from the model's same-named material **blocks** the candidate (materials are matched by name everywhere in SAM; applying it would silently take the model's definition and the shown U would be wrong).
* Loaded candidates stay outside the model: listing, choosing and discarding change nothing (`Listing_the_alternatives_changes_nothing_in_the_model`, Discard test).
* D1 sources are the **model and the default library**. Loaded sources (`Add source...`) are D2.

## Tests

* `ConstructionAlternativesTests` (18): list content and order (meets / near / excluded), line text, notes (panel group, name clash, materials), differing material blocks, unreachable generated target still lists existing ones, **one batch per pool**, cache reuse across targets and rows, changed basis asks again, content-based key, model JSON unchanged, Tas unavailable, no target = nothing asked, nothing chosen automatically + request content, scope / Keep name, a vanished choice returns to the generated variant, the real evaluator (chunking 45 → 2 runs, missing material, failed run, STA, cancelled request dropped, order kept).
* `SetConstructionTests` (11): core (model construction, library construction + materials, name clash, selected only, refusals with reasons, input untouched), one commit / one Undo (UI model), failed change commits nothing, change-set conflict, **existing construction + glazing = one commit / one Undo**, Discard, no evaluator call for a row that is only looked at.
* `ThermalPerformanceEditingControlTests` (+1): the real XAML list, selection, Apply.
* Existing tests: `ThermalEditServices` call sites pass a fake batch evaluator; one control test selects the glazing list by AutomationId.
* Full WPF suite **1968/1968** (1938 + 30).

## Per-Apply report (parity with the U-value path)

`Modify.ApplyThermalChangeWithReports` now also writes a **CONSTRUCTION CHANGE** report for each existing construction assigned, through the existing path: same folder and naming
(`<model>_ConstructionChange_<yyyyMMdd-HHmmss>.txt`, never overwrites), same `SaveChangeReport`, same scoped check (`UValueCheckSummary` over the assigned construction and its panels), same
text layout. It records the construction chosen and its Guid, the construction it replaced, the **source / provenance** (name and kind: existing model / default library / added source), the
build-up, **U before -> after** on the heat-flow basis with the target, scope and count, the materials added, the **notes shown before Apply** (panel group, numbered name, materials) and the **check
result**; the result line shows "Report saved: ...". No new framework: `Query/ConstructionChangeReport.cs` mirrors `UValueChangeReport.cs`; the report step was split out (`WriteReports`) so it is testable without Tas.

## Real-app acceptance (real Tas, real default library; model: 12 walls SIM_EXT_SLD U 0.26, 9 roofs, 9 floors, 20 partitions, 20 windows)

Evidence (local): `C:\TasOut\uvalue\d1\run1`, `run2`, `run3` (driver `C:\TasOut\uvalue\alite\drive\d1_alt.ps1`).

1. Select a wall (Select By Guid), type target **0.18**: the list is ready in **1.7-2.6 s cold** (model + default library, 21 library constructions + the model's; real Tas, two batches), "6 existing constructions meet U 0.18; 1 more is within 10 %". Generated first: `SIM_EXT_SLD U0.18 · U 0.180 · Generated`, then e.g. `SIM_EXT_SLD_Roof · U 0.163 · Existing model` (used by 9 panels), `SIM_EXT_GRD_FLR FLR02 · U 0.162 · Library`. The model's roofs and floors are offered with the note "made for roofs ... but 12 of the 12 panels sit in walls".
2. **Another target (0.20 / 0.18): no "Calculating", no new Tas run** - the U-values are cached; the list re-sorts at once.
3. Nothing is chosen by default; Undo stays disabled and the model is unchanged while the list is shown.
4. Choose the **library** construction `SIM_EXT_SLD_FLR Exposed` (U 0.181): preview `U 0.260 → 0.181 W/m²K · SIM_EXT_SLD_FLR Exposed (Library)`, note "Adds 4 materials to the model: ...", "It is added to the model with the materials it lacks.", check "No new warnings", "1 change · 12 elements".
5. **Apply 3.8-4.0 s** (whole-model Tas refresh): "12 panels now SIM_EXT_SLD_FLR Exposed. One Undo reverts it."; the saved model has 12 panels on it with stored U 0.181; Undo is then enabled.
6. **One Undo** (Edit > Undo): the wall row is back to `SIM_EXT_SLD U 0.260 · 12 use it`; the saved model has the original length (900,824) and no trace of the library construction (0 mentions of its name; 24 while applied) and the panels' stored values are the original ones. (A model-in-model choice, `SIM_EXT_SLD_Roof`, was applied and undone the same way in `run1`.)

## Limitations / next

* Library = the default construction library; loaded / remembered sources are D2. Candidates are a flat list (no search/filter box), capped at 30.
* The cold evaluation of a very large pool is chunked (40 per Tas run) but not measured beyond 21 library constructions; progress is a count, not a bar.
* An alternative whose Default Panel Type differs from the panels is only marked (see above).
