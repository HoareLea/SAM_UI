<!-- SPDX-License-Identifier: LGPL-3.0-or-later -->
<!-- Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors -->

# Part O PR-1: one identity-based Systems scope for Iteration 3 and Mixed Design; Check runs Build's preflight

**Status (30 Sep 2026): implemented and tested. The PR is open (SAM-BIM/SAM_UI#151) against `sow/2026-Q3` and is NOT merged. It depends on
SAM PR-1 (SAM-BIM/SAM#171, `Query.PartOSystemsMaterialisationScope`), which must merge first.**

- Branch `feature/parto-systems-scope-2026-09-30`, from `sow/2026-Q3` `a3ae5df` (after SAM_UI#150 PR-4 and its
  closeout). SAM branch of the same name, from SAM `sow/2026-Q3` `ffb61972`.
- Architecture authority: `documentation/PartO-ModelStateArchitecture.md`, step 2 (PR-1). Nothing here contradicts it.
- SAM record: `SAM/documentation/PartO-SystemsScope-PR1.md`.

## Problem (F5, F6)

- **Mixed Design scope.** On the Systems route (any cooled dwelling) `PartOMixedSystemsMaterialisation` handed
  `PartOIteration3Pipeline.MaterialiseMixed` the WHOLE materialised cluster. SAM_Systems requires every ventilation
  system it is handed to name an AHU, so the template `NV 1`/`UV 1` refused a correct design: "Ventilation system 'UV'
  names no air handling unit". Iteration 3 already scoped by identity (`Query.PartOIteration3SystemScope`), but that
  rule lived in SAM_UI WPF and Mixed never used it.
- **Check vs Build.** Check design ran only SAM materialisation; the SAM_Systems graph was first built in Build & Run,
  so Check could pass and Build then refuse.

## Behaviour after this PR

- **The rule is SAM's.** `Analytical.Query.PartOSystemsMaterialisationScope(cluster, guids_Built, guids_Space_Dwelling)`:
  keep the systems Part O built (by guid); leave out authored systems with no effective duty (a stated, finite,
  non-zero terminal airflow); refuse where authored duty exists. Only ventilation systems are read.
- **Iteration 3 (behaviour-identical).** `Query.PartOIteration3SystemScope` is now a thin adapter: it calls the SAM
  query and formats the structured result in Iteration 3's existing words (its records persist these notes). It
  decides nothing. Callers (`RunPartOIteration3`, `PartOIteration3Preflight`, reconciliation) are unchanged.
- **Mixed Design.** `Modify.PartOMixedSystemsMaterialisation` is the ONE Systems preflight: compose the call from
  SAM's record → **scope** by `PartOMaterialisationRecord.VentilationSystemGuids` (`Query.PartOMixedSystemsScope`, the
  same SAM query; dwelling rooms = the record's `ZoneGuids_Assessed`) → build the ONE SAM_Systems graph over the scoped
  **working copy** → check it against SAM's record. SAM_Systems now sees only the Part O systems. The thermal model
  (no-IZAM source, TAS) still uses the full materialised model, so NV/UV/MV stay authored behaviour.
- **Check == Build.** Check design calls `Modify.CheckPartOMixedDesign(model, descriptors, templates)`: SAM
  materialisation, then - on the Systems route only - the very same `PartOMixedSystemsMaterialisation` that
  `SimulatePartOMaterialisationSystems` runs first. No TAS. A scope refusal shows as "SAM can build this mixed design
  …, but its TAS Systems ventilation cannot be prepared, so Build & Run would stop before TAS", with the refusal text.
  An IZAM-route design has no Systems preflight at either stage.
- **Notes.** The scope's "left out" notes go to `PartOStrategySetSimulation.Notes_SystemsScope` (and
  `PartOMixedDesignCheck.Notes_Systems`), not to the run's warnings, so the status line's first three run notes stay
  real warnings.
- **Invariant kept.** Every scope operation runs on derived calculation state; the design model and the materialised
  model are never modified (tests compare JSON before/after).

## Decisions and assumptions

- **Identity only.** Scope = `Record.VentilationSystemGuids.Values`; no names (`NV 1`, `MV 1`, `MVHR …`) anywhere.
- **Whole-building route unchanged** (one cooled dwelling → whole model on Systems). No hybrid IZAM/TPD.
- **NV/UV semantics unchanged.** No AHU is added; they are left out of the SAM_Systems input only.
- **Authored duty still refuses**, with Iteration 3's definition of effective duty, including in Mixed: an authored
  system with real duty in a room the materialiser does not judge (e.g. an unzoned plant room) is now refused on the
  Systems route, because the no-IZAM source would drop it model-wide.
- **Iteration 3 wording kept** in SAM_UI (formatting only); SAM's own messages are route-neutral engineer wording.
- **PR boundary.** Not changed: `AuthoredMechanicalSystems` (PR-2), SAM_Systems (PR-3), BaselineReference (PR-5),
  "Systems in this assessment" UI (PR-6), SAM_Deploy.

## Files

- `WPF/SAM.Analytical.UI.WPF/Query/PartOIteration3SystemScope.cs` - rule removed; adapter over SAM with It3 wording.
- `WPF/SAM.Analytical.UI.WPF/Classes/PartO/PartOIteration3SystemScope.cs` - summary note only.
- `WPF/SAM.Analytical.UI.WPF/Query/PartOMixedSystemsCall.cs` - `Query.PartOMixedSystemsScope`.
- `WPF/SAM.Analytical.UI.WPF/Modify/SimulatePartOMaterialisationSystems.cs` - scoped preflight (+ notes overload).
- `WPF/SAM.Analytical.UI.WPF/Modify/CheckPartOMixedDesign.cs` - new `PartOMixedDesignCheck` + `Modify.CheckPartOMixedDesign`.
- `WPF/SAM.Analytical.UI.WPF/Modify/RunPartOMixedDesignCommand.cs` - Check button uses it.
- `WPF/SAM.Analytical.UI.WPF/Modify/RunPartOStrategySet.cs` - `Notes_SystemsScope`.
- `WPF/SAM.Analytical.UI.WPF.Tests/PartOMixedSystemsScopeTests.cs` - 10 tests.
- `documentation/evidence/parto-pr1-systems-scope-2026-09-30/` - real-model replay output and tool source.

## Evidence

- **Iteration 3 pinned before the move** (commit `3eea329`): a temporary verbatim copy of the pre-move rule plus an
  equivalence test (fixed scenarios + 300 seeded random models: retained/removed order, notes and refusals character
  for character, working-copy JSON, source untouched). 573/573 It3 tests before the move; **573/573 after** (commit
  `786f1ed`). The oracle was then removed (`782c7b6`) so no second copy of the rule remains; the existing It3 scope,
  reconciliation and scaling tests stay as the permanent pins.
- **New Mixed tests (10/10)** on a real `Modify.AddMechanicalSystems` scaffold (`NV 1`, `UV 1` no unit; `MV 1` + `AHU1`
  in an unzoned plant room; cooling `AHU 1`/`FCU 1`; heating `RAD 1`) beside Flat 01 natural / Flat 02 MVHR / Flat 03
  MVHR cooled:
  - pre-fix proof: the whole cluster is refused by SAM_Systems ("names no air handling unit" on NV/UV);
  - the preflight leaves `NV 1`/`UV 1`/`MV 1` out and builds 2 air systems, 1 guidance-cooled; materialised model
    unchanged;
  - identity: a template system labelled `MVHR 1` is left out; the record's systems are kept;
  - cooling/heating templates neither consumed nor removed;
  - authored 30 l/s duty in the plant room refuses at Check and at Build with the identical text, before TAS;
  - valid scope: Check passes, Build passes the same preflight (stopped by cancellation before TAS) with identical
    scope notes; baseline JSON unchanged;
  - system-free cooled design still passes both; uncooled design has no Systems preflight; a SAM materialisation
    refusal skips it.
- **Full WPF suite: 1559/1559** on the final head (1549 before this PR, +10; the temporary oracle added 312 more while it existed, 1871/1871). Built against the SAM PR-1 branch.
- **Mutation checks (all killed, reverted clean):** U1 Mixed passes the whole cluster (2 fail); U2 Check skips the
  preflight (3 fail); U3 It3 note wording drift (34 fail, with the oracle); U4 Mixed scope = every system (4 fail);
  U5 It3 inside-dwelling wording lost (136 fail, with the oracle). SAM-side S1-S5: see the SAM record.
- **Real model, headless, no TAS, read-only** (`evidence/.../replay-output.txt`): owner's
  `000000_SAM_AnalyticalModel-Cleaned.sam`, SHA256 `F561161F…0B78` before and after, folder listing unchanged, nothing
  written.
  - Design model: `NV 1` (no unit, 0 terminals), `UV 1` (no unit, 0), `MV 1` (`AHU1`, 0 terminals, 6 spaces in
    Flats 2+3); cooling `AHU 1`, `UC 1`, `FCU 1`; heating `RAD 1`, `TRH 1`, `UH 1`, `UFH 1`.
  - A. Owner's selection (Flat 1 natural, Flat 2 XBC15, Flat 3 MRXBOX + cooling) through the real Check:
    **`SharedSystem` on `MV 1` and `AHU1`** - the known PR-2 refusal; the Systems preflight is not reached.
  - B. In-memory diagnostic (only Flat 1 assessed, MRXBOX + cooling, so MV 1/AHU1 are outside PR-2's rule; nothing
    deleted, nothing saved): Systems route; scope retains only `MVHR Flat 1`, leaves out `NV 1`, `UV 1`, `MV 1`;
    SAM_Systems input contains neither NV 1 nor UV 1.
  - C. Production preflight: SAM_Systems graph built, 1 air system, 1 guidance-cooled unit, no refusal.
  - D. Pre-PR-1 contrast (whole cluster, in memory): refused - "Ventilation system 'UV' names no air handling unit" -
    the owner's production refusal.

## Risks / not verified

- **The owner's real Mixed Check still refuses** on `MV 1`/`AHU1` `SharedSystem`. Expected: that is PR-2. Not
  weakened or bypassed here.
- SAM_Systems still ignores part of its caller's scope (PR-3); after this PR the caller no longer relies on it.
- No licensed TAS run. The licensed real Mixed acceptance happens after PR-1 + PR-2 + PR-3.
- CI runs the Windows build (~10 min); local builds/tests passed.

## Next step

Owner review → merge SAM PR-1 → revalidate this branch against merged SAM `sow/2026-Q3` (rebuild SAM, full WPF suite)
→ merge → SAM and SAM_UI `PROJECT_PROGRESS.md` closeouts on the base branch. Then PR-2 (SAM effective-duty
classification) and PR-3 (SAM_Systems D2 scope), then the licensed Mixed acceptance on `-Cleaned.sam`.
