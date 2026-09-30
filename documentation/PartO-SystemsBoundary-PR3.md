<!-- SPDX-License-Identifier: LGPL-3.0-or-later -->
<!-- Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors -->

# Part O PR-3: SAM_Systems processes only the Systems scope SAM supplies

**Status (30 Sep 2026): implemented and tested. Two coordinated PRs are open against `sow/2026-Q3`, neither is
merged:**

1. **SAM_Systems PR-3, [SAM-BIM/SAM_Systems#34](https://github.com/SAM-BIM/SAM_Systems/pull/34)** (merge FIRST): the scoped `Create.MechanicalVentilation` overload. Record:
   SAM_Systems `docs/PartO-SystemsBoundary-PR3.md`.
2. **SAM_UI PR-3, [SAM-BIM/SAM_UI#153](https://github.com/SAM-BIM/SAM_UI/pull/153)** (this record, merge SECOND): Mixed Design and Iteration 3 state SAM's scope to SAM_Systems. Its CI
   clones SAM_Systems' default branch, so it cannot go green until (1) is merged. Re-run it then.

- Branch `feature/parto-pr3-systems-scope-2026-09-30` in both repos, from SAM_Systems `sow/2026-Q3` `aadb1b1` and
  SAM_UI `sow/2026-Q3` `90dff25`.
- Preconditions: SAM PR-2 (SAM#172, `5ffe6a10`) is merged, and its SAM closeout `137c0bcf` is on `sow/2026-Q3`.
  SAM PR-1 (SAM#171) and SAM_UI PR-1 (SAM_UI#151) are merged. No SAM change.
- Architecture authority: `documentation/PartO-ModelStateArchitecture.md`, step 4 (PR-3). Nothing in production code
  contradicted it, so the frozen architecture was not reopened.

## Root cause

**SAM decides what participates** (`Analytical.Query.PartOSystemsMaterialisationScope`, PR-1 and PR-2). SAM_Systems
could not be told. It read every `VentilationSystem` of whatever cluster it was handed:

- **D2** resolved every system's unit. The whole real model therefore refuses on the unit-less `UV 1`/`NV 1`, and any
  malformed system anywhere refuses.
- **D10**'s duty cross-check (SAM's `AirHandlingUnitDesignDuty`) sums every system naming a unit, over the whole
  model. So an excluded system naming a scoped unit would add its duty back.

PR-1 protected the production callers by handing over SAM's working copy (other systems removed). The boundary lived
only in the caller.

## Production call chain (traced)

```text
Check design / Build & Run (Mixed Design)
  → Modify.CheckPartOMixedDesign / SimulatePartOMaterialisationSystems
  → SAM Modify.MaterialisePartODwellingStrategies           (PR-2 effective-duty classification)
  → Modify.PartOMixedSystemsMaterialisation  (the ONE Systems preflight)
      Query.PartOMixedSystemsCall        → rooms of the MVHR dwellings + cooled units' guidance settings
      Query.PartOMixedSystemsScope       → SAM Query.PartOSystemsMaterialisationScope (PR-1): working copy + Guids_Retained
      PartOIteration3Pipeline.MaterialiseMixed(workingCopy, rooms, guidance, Guids_Retained)   ← PR-3 states the scope
  → SAM_Systems Create.MechanicalVentilation(cluster, MV.json, settings{GuidanceTemplate=MVRE}, rooms, scope)

Iteration 3 (RunPartOIteration3)
  → Query.PartOIteration3SystemScope (adapter over the same SAM query)
  → IPartOIteration3Pipeline.Materialise(workingCopy, rooms, unit/cooling/guidance settings, Guids_Retained)  ← PR-3
  → SAM_Systems Create.MechanicalVentilation(...)
```

These are the only production callers of `Create.MechanicalVentilation` in SAM, SAM_Systems, SAM_Tas and SAM_UI;
SAM_Tas calls it only from tests. Downstream of SAM_Systems, SAM_Tas' TPD conversion reads the materialisation
result, not the cluster's ventilation systems. So nothing downstream can reintroduce an excluded system.

## Chosen defensive design

- **SAM_Systems** gains a 5-parameter overload with `IEnumerable<Guid> guids_VentilationSystem`:
  - `null` is the legacy call;
  - stated, only those systems are read, in D2 and in D10's cross-check;
  - an empty scope, `Guid.Empty`, or a guid that is not a ventilation system of the model refuses;
  - a repeated guid counts once.
  Every existing refusal of an included system is unchanged.
- **SAM_UI** states SAM's `Guids_Retained` in both production calls, and **keeps** handing SAM's working copy. There
  are two independent guards: SAM removes the systems from the input, and SAM_Systems reads only the stated ones.
  Stating the scope over the working copy materialises byte-identically (tested). So Iteration 3 and Mixed output is
  unchanged. It only stops SAM_Systems reading anything SAM did not include.
- No Part O, dwelling or effective-duty logic was added to SAM_Systems.

## Files (SAM_UI)

- `WPF/SAM.Analytical.UI.WPF/Interfaces/IPartOIteration3Pipeline.cs`: a scoped `Materialise` member with a default body; the original member is unchanged.
- `WPF/SAM.Analytical.UI.WPF/Classes/PartO/PartOIteration3Pipeline.cs`:
  - `Materialise` and `MaterialiseMixed` keep their signatures and gain scoped overloads that forward the scope;
  - internal `ScopeObserver` (test visibility only; no `virtual`).
- `WPF/SAM.Analytical.UI.WPF/Modify/SimulatePartOMaterialisationSystems.cs`: Mixed states
  `partOSystemsMaterialisationScope.Guids_Retained`.
- `WPF/SAM.Analytical.UI.WPF/Modify/RunPartOIteration3.cs`: Iteration 3 states `partOIteration3SystemScope.Guids_Retained`.
- Tests:
  - `PartOMixedSystemsScopeTests.cs`: +3 tests, plus an Iteration 3 `Materialise` assertion;
  - `PartOIteration3RunTests.cs`: +1 test;
  - the three test doubles take the new parameter (`PartOIteration3PipelineFake` records it, `…ReviewTests`,
    `…ResumeAcceptance`).
- Evidence: `documentation/evidence/parto-pr3-systems-boundary-2026-09-30/` (replay source, csproj, output; paths
  redacted).

## Tests and validation

- **SAM_Systems:** 34 new cases, 7 mutations all killed, 303/303, Mollier 123/123. See its record for the case table.
- **SAM_UI new tests:**
  - `TheWholeCluster_WithSamsScope_MaterialisesOnlyThePartOSystems_ExactlyAsTheWorkingCopy`: the whole materialised
    production-shaped cluster (with `NV 1`, `UV 1`, `MV 1 → AHU1`) materialises the two Part O units only, and never
    `AHU1`. It is JSON- and binding-identical to the working copy. Iteration 3's `Materialise` gives the same result:
    it refuses unscoped and is identical when scoped.
  - `ThePreflight_TellsSamSystemsExactlyTheSystemsPartOBuilt`: a capturing pipeline sees exactly
    `Record.VentilationSystemGuids`.
  - `The_materialisation_is_told_the_retained_system_scope_which_is_exactly_the_working_copys_systems` (Iteration 3).
- **SAM_UI mutations.** All were killed, and the sources were restored byte-identical:

  | Mutation | Failed |
  |---|---|
  | U1 the Mixed preflight drops the scope | 1 / 66 focused |
  | U2 the Iteration 3 run drops the scope | 1 / 66 |
  | U3 `MaterialiseMixed` does not forward it | 1 / 66 |
  | U4 `Materialise` does not forward it | 1 / 66 |

- **Full suites:**
  - `SAM.Analytical.UI.WPF.Tests` **1562/1562** (1559 before, +3), against the PR-3 SAM_Systems build;
  - SAM_Tas (unchanged) `SAM.Analytical.Tas.TM59.Tests`, rebuilt against it: 0 errors, **1023/1023**.
- **Iteration 3 unchanged:**
  - its working copy holds only the stated systems;
  - SAM_Systems proves "every system stated == legacy" for B0, B1+ (MVRE unit settings) and Mixed;
  - the whole Iteration 3 suite (run, resume, review, preflight, reconciliation) passes unchanged.
- **Real model, headless, no TAS, read-only** (`evidence/.../replay-output.txt`): `000000_SAM_AnalyticalModel-Cleaned.sam`.
  - The selection: Flat 1 natural; Flat 2 Nuaire XBC15 with cooling off; Flat 3 Nuaire MRXBOXAB-ECO5-AECV with cooling
    on.
  - PR-2: `NV 1`, `UV 1`, `MV 1` and `AHU1` are all inert. Check materialises on the Systems route with 0
    `SharedSystem` and passes.
  - PR-1: 2 retained (`MVHR Flat 2`, `MVHR Flat 3`), 3 removed (`MV 1`, `UV 1`, `NV 1`).
  - PR-3, the **whole** materialised model handed to SAM_Systems:
    - unscoped, it refuses: "Ventilation system 'UV' names no air handling unit";
    - with SAM's scope, it materialises 2 air systems, processing units `MVHR Flat 2` and `MVHR Flat 3`, 6 rooms, and
      Flat 3 guidance-cooled at 80 l/s;
    - `NV 1`/`UV 1`/`MV 1`/`AHU1` stay on the model and are not bound;
    - the graph is identical to PR-1's working-copy graph, and the materialised model is unchanged.
  - The production preflight (working copy + stated scope) builds 2 air systems.
  - SHA256 `F561161F…0B78` before and after. The folder listing is identical and nothing was written. **PR-3 gate:
    PASS.**
- No licensed TAS run (not required for PR-3).

## API compatibility

- **SAM_Systems:** additive only. The old signature is unchanged, and binary and source compatible.
- **SAM_UI (API hygiene review):** no existing public signature is replaced. The scope is added as overloads:
  - `PartOIteration3Pipeline.Materialise(..., guidanceSettings)` and `MaterialiseMixed(adjacencyCluster, spaces,
    guidanceSettings)` keep their original CLR signatures and delegate with a null (unstated) scope. Each gains a
    scoped overload with a required trailing `IEnumerable<Guid> guids_VentilationSystem`.
  - `IPartOIteration3Pipeline.Materialise` keeps its original member. The scoped member is added with a default body
    (C# 8 default interface member): an implementation that predates the scope still compiles and binds, serves an
    unstated scope as before, and refuses a stated scope it cannot honour (`NotSupportedException`) instead of quietly
    processing every system.
  - Nothing in production is `virtual`. The test that observes what the preflight hands SAM_Systems uses an `internal`
    `PartOIteration3Pipeline.ScopeObserver` (visible to the test assembly only; unset in production).
  - Behaviour is unchanged: the same scope reaches SAM_Systems on both routes. The test doubles implement both members.
  - Covered by `TheScopeIsAddedNotSubstituted_PublicSignaturesAreUnchanged`.

## Unresolved / risks / belongs elsewhere

- SAM_Systems' model-wide identity preconditions (a `Guid.Empty` space, unit or transfer movement) stay model-wide, on
  purpose.
- Engineer wording: SAM_Systems messages name systems by `Name` (`'UV'`), not `FullName` (`'UV 1'`). This is pinned
  legacy text, and a candidate for **PR-6**. The 6 "Space … is still related to ventilation system 'MV 1'" Check
  warnings (PR-2 note) are also PR-6 presentation.
- Nothing found belongs to PR-5 (BaselineReference).

## Exact next step

Owner review → merge SAM_Systems PR-3 → re-run SAM_UI PR-3 CI → merge SAM_UI PR-3 → post-merge `PROJECT_PROGRESS.md`
closeouts on `sow/2026-Q3` in SAM_Systems and SAM_UI. Then the licensed real-UI Mixed Design acceptance run on
`-Cleaned.sam` with nothing deleted (architecture step 5).
