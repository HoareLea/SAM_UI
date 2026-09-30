<!-- SPDX-License-Identifier: LGPL-3.0-or-later -->
<!-- Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors -->

# Part O output folders: one folder per case beneath the chosen root

**Status (30 Sep 2026): code, tests and licensed Mixed Design acceptance are done. The PR is open against
`sow/2026-Q3` and is NOT merged; the owner reviews the structure first.**

- Branch `feature/parto-output-folders-2026-09-30`, from `sow/2026-Q3` `788e647` (the SAM_UI#146 closeout).
- SAM_UI only. SAM (`83eb79a3`) and SAM_Tas (`057faf3`) are unchanged.
- Review pass 2 is in: the Iteration 3 1a-vs-2 qualifier, and marker-based layout recognition (the chosen root is used
  verbatim).

## The problem, as it was

Every Part O run wrote into the one output folder a person chose. The cases reuse file names - 1a, 1b and 2 all
write `<model>.tbd/.tsd/.sam/-TM59.txt` - so one case overwrote another. The worst case: a 1b run replaced the
Iteration 2 results an Iteration 3 pairing references (noted in the SAM_UI#145 closeout as "use a separate output
folder per iteration").

## The structure now

The person still chooses one folder, which is now the **Part O root**. SAM creates the rest:

```text
<root>/                     exactly the folder chosen - never re-rooted, whatever it is called
  Iteration1a/  Iteration1b/  Iteration2/  Iteration2B/  Iteration3/  MixedDesign/
    PartOCase.json  marker: {"Schema":"SAM.PartOOutputCase/1","Case":"<case>"} - what makes it SAM's
    tas/            .xml .t3d .tbd .tpd .tsd (+ bridge .tbd/.tsd), and the per-run .sam,
                    .partorun.json, .prepared.sam named from the TSD
    reports/        <run>-TM59.txt; Iteration 3 record <A>[-It1a]-Iteration3-<tag>.json and its
                    <A>[-It1a]-Iteration3-<tag>-Review.txt / .json
    diagnostics/    <name>.timing.csv, <tpd>.route.timing.csv, <B>-OperatingAirFlow.csv,
                    <tpd>_GuidanceOperation.csv (mixed)
```

File names are unchanged except for one qualifier: Iteration 3 against an Iteration 1a reference adds `-It1a`
(see below). The case meanings are unchanged:

- 1a: MVHR design duty (no manufacturer unit).
- 1b: natural ventilation.
- 2: MVHR with a manufacturer unit.
- 2B: ventilation optimisation (every round and the capacity envelope).
- 3: explicit system and cooling assessment.
- MixedDesign: every screening and the final run.

## Design

**One resolver: `SAM.Analytical.UI.PartOOutputPaths`** (`SAM_UI/SAM.Analytical.UI/Classes/PartO/PartOOutputPaths.cs`)
with `PartOOutputCase` (`Enums/PartOOutputCase.cs`).

- `Create(root, case)` uses the root **verbatim**. For any name, the case folder is created directly inside the chosen
  folder, never above or below it.
- `CreateDirectories()` / `TryCreateDirectories()` create the case folder, its three subfolders and the
  **`PartOCase.json` marker**. An existing marker is never rewritten.
- `Find(directory)` recognises the layout when both of these hold:
  - the directory is named `<root>/<case>/tas|reports|diagnostics`;
  - its case folder holds a readable marker of the current schema naming **that** case.

  Any other folder is a legacy flat folder: one named like the layout by a person, one renamed by hand, or one with
  a missing, corrupt or foreign marker.
- `Root(directory)` is the root of an **existing** run's results folder. It steps out of exactly one SAM-created case
  folder, never further. For a legacy folder it is the folder itself. It is used only where a run follows the run it
  starts from: 2B follows the baseline, Iteration 3 follows Reference A.
- `CaseOf(PartOPreparationContext)`:
  - `BaseNaturalVentilation` = 1b.
  - `BasePassive` with a catalogue offered = 2; without one = 1a.
  - Unknown (a resumed v1 record) = null, so the run writes into the chosen folder as before rather than guessing.
- `SimulationContext(context)` (instance): the same TAS case, writing into this case's `tas` (used by 2B).
- `Directory_Reports_ForResults` / `Directory_Diagnostics_ForFile`: the case's folder when the file is in the
  layout, or beside the file in a legacy folder.
- `FileDiagnostics(tas)`: moves SAM_Tas' `*.timing.csv` from a layout `tas` folder into `diagnostics`. It never acts
  on a legacy folder and never fails a run.

**Consumers (every one goes through the resolver):**

| Writer | Change |
|---|---|
| `Modify.Simulate` (1a/1b/2, dialog + Hub) | Guided Part O only. `SimulateInputs.PartOOutputCase` gives `Create(chosen, case)` + `TryCreateDirectories` (a failure is a refusal), and the context `OutputDirectory` = case `tas`. The remembered option stays the root. The expert Simulate command is unchanged. |
| `Modify.RunPartOSimulation` (every Part O TAS run) | Restores subfolders of a recognised case folder before the first write, and files the workflow timing CSV. |
| `Query.Path_TM59Report` (the one TM59 naming authority) | Layout results go to the case `reports`; legacy results keep the report beside the TSD. |
| `Modify.OptimisePartOTM59` (2B) | `Create(Root(baseline dir), Iteration2B)`, created (a failure is a refusal). Rounds and the envelope run there; the baseline is read where it is. |
| `PartOIteration3Paths` (3) | `Create(Root(Reference A dir), Iteration3)`. Candidate B's TAS files go in `tas`, OperatingAirFlow in `diagnostics`, the record and review in `reports`. The `-It1a` qualifier applies where the reference is Iteration 1a. |
| `Modify.RunPartOIteration3` | Creates Iteration 3's folders at attempt start, and files the route/TPD/bridge timing CSVs at its single exit. |
| Mixed Design (`Create.PartOMixedSimulationContext` / `PartOMixedOutputDirectory`) | `MixedDesign` inside the chosen root, created when the context is composed. Also the `RunModelsSafe` guard, the `_GuidanceOperation.csv` → diagnostics, and timing filed after each run. |
| UI | Tooltips on both output-folder boxes. The Mixed Design "Accept optimised airflow" dialog starts in `Iteration2B/tas` once it exists. |

**Writer audit (review pass 2).** Every Part O writer of `.sam .xml .t3d .tbd .tpd .tsd .txt .json .csv` gets its
folder from the resolver: a resolved `tas`, `reports` or `diagnostics` directory, `PartOIteration3Paths`, or a sibling
derivation from a TSD in a resolved `tas`. The TSD siblings are `Path_PartORunModel`, `PartORunResume`,
`Path_PartOWorkflowJson` and `Path_TM59Report`. SAM_Tas' own `.t3d/.tsd/.json/.timing.csv` are siblings of the
resolved TBD/TPD/bridge.

Three writers deliberately sit outside the resolver:

- `.partomixed.json`, which lives beside the open model;
- the expert-only export branches in `Simulate.cs` (room data sheets, SAP, Part L, DomOv, TPD), all locked off on the
  Part O route;
- the separate "simulate from path" command, which is not a Part O command.

## Iteration 3: the 1a-vs-2 reference collision (review pass 2)

**It is real.**

- **Both variants are legitimate.** Eligibility accepts any `BasePassive` preparation, 1a or 2. The route check (B0)
  runs against either; the product methods need a selected product per unit.
- **The names are the same.** The prepared model keeps the source model's name (only an isolated run gets a scope
  token), so a 1a run and an Iteration 2 run of one model write the same `Flat1.tsd`.
- **A user can run both without meaning to replace one.** The Hub runs Iteration 3 against whichever run is current.
  So a user who runs Iteration 3 after 1a, and later again after Iteration 2, wrote identical names into
  `Iteration3/`:
  - `.tbd/.tsd/.tpd`, bridge `.tbd/.tsd`, `.sam`;
  - `-Bridge-TM59.txt`, `-OperatingAirFlow.csv`, the timing CSVs;
  - the record `-Iteration3-<tag>.json` and its review `.txt/.json`.

**Decision:** `PartOIteration3Paths.Qualifier_ReferenceIteration1a = "-It1a"`, inserted after Reference A's name.

- **Where it applies:** only where Reference A's TSD is in a SAM-created `Iteration1a` case folder
  (`Qualifier_Reference(path_TSD)`, read off the reference's own folder).
- **What it covers:** every file the run owns. Candidate B = `<A>-It1a-It3B*`, so its TAS files, bridge, `.sam`, TM59
  report, histories and timing files are all covered. The record is `<A>-It1a-Iteration3-<tag>.json`, so its review is
  covered too.
- **Iteration 2 keeps the unqualified names.** It is the ordinary reference, and its names are the ones the owner's
  example lists.
- **Where it does not apply:** legacy flat references, which is how every pairing was named before.
- **Kept unqualified:** `ProjectName_ReferenceA`, Reference A's TSD path and Reference A's own TM59 report (in
  `Iteration1a/reports`).
- **Why it is safe on reopen:** the review derives the same record from the reference TSD alone, and the Candidate B
  paths are persisted absolute in the record. No new directory level, no copies.
- **Why "-It1a":** it echoes the existing `-It3B` Iteration 3 suffix style. It is storage only, never shown as UI
  wording. `PartOSimulationContext.Iteration_ProjectName` still reads Candidate B as 0.

## Decisions and assumptions

- **Per-run `.sam`, `.partorun.json`, `.prepared.sam` stay beside the TSD in `tas/`**, not at the case root. Every
  reader derives them from the TSD path (`Path_PartORunModel`, `PartORunResume`, `SimulationResultProvenance`'s
  fallback "TSD beside the .sam"), so moving them would add persistence risk. SAM_Tas's workflow `.json` is also a
  TBD sibling; it is still deleted after the `.sam` is written.
- **`.partomixed.json` is unchanged**: it sits beside the open model file, not in the output folder.
- **The Iteration 3 pairing record goes to `Iteration3/reports`.** It is the persisted result record, and there its
  `-Review.txt/.json` stay derived from it in the same folder. `Path_Report_ForRecord` is unchanged.
- **Iteration 3 references Iteration 2 (or 1a) where it is.** The record stores the actual `Iteration2/tas/<A>.tsd`.
  A review derives the record from Reference A's TSD alone: the root is the TSD folder's root, then
  `Iteration3/reports`. Nothing is copied.
- **A marker, not the name, identifies SAM's folders.** A name alone cannot tell a SAM case folder from a
  person's `...\Iteration2\tas`. Relying on the name would re-root output above a chosen folder, and it would move a
  legacy run's reports. The marker lives in the case folder, so the chosen root receives no file.
- **Timing CSVs are moved by SAM_UI after SAM_Tas writes them** (they are never read back). This avoids a SAM_Tas API
  change and a cross-repo PR.
- **Iteration 3 and 2B follow Reference A's / the baseline's root, not the Hub box.** Changing the Hub folder
  between runs does not split one pairing across two roots.

## Backward compatibility

- Old projects are read exactly as saved. Stored absolute paths are unchanged, nothing is moved, nothing is
  rewritten on open, and no migration is forced.
- A legacy flat TSD keeps its reports beside it, even in a hand-made `...\Iteration2\tas`, because such a folder has
  no marker.
- A per-method record beside a legacy TSD is still found. `Query.PartOIteration3RecordPath` looks in this order: the
  new location, then per-method beside the TSD, then the mode-independent legacy record, then the new path. A new
  record, once written, supersedes the old one.
- A **new** run started from a legacy run treats the flat folder as its root. For example, Iteration 3 against a
  legacy `C:\out\Flat1.tsd` writes `C:\out\Iteration3\...` and cannot overwrite the legacy files.

## Files

- New: `SAM_UI/SAM.Analytical.UI/Classes/PartO/PartOOutputPaths.cs`, `SAM_UI/SAM.Analytical.UI/Enums/PartOOutputCase.cs`.
- Changed:
  - `Modify/`: `Simulate.cs`, `RunPartOSimulation.cs`, `OptimisePartOTM59.cs`, `RunPartOIteration3.cs`,
    `ReviewPartOIteration3.cs`, `SavePartOTM59Report.cs`, `SavePartOIteration3Report.cs`, `RunPartOStrategySet.cs`,
    `RunPartOMixedDesignCommand.cs`, `SimulatePartOMaterialisationSystems.cs`;
  - `Query/`: `Path_TM59Report.cs`, `PartOIteration3RecordPath.cs`;
  - `Classes/PartO/PartOIteration3Paths.cs`;
  - `Windows/`: `PartOWorkflowWindow.xaml`, `PartOMixedDesignWindow.xaml(.cs)`.
- Tests:
  - `PartOOutputFolderTests.cs` (42) - mapping, adversarial folder names, markers, depth, collisions, 1a-vs-2
    Iteration 3, 2B, Mixed, reopen, legacy.
  - `PartOOutputFolderAcceptance.cs` (env-gated, licensed).
  - Two `PartOIteration3ReviewTests` runtime tests:
    - `A_reference_in_Iteration2_pairs_into_Iteration3_and_reopens_from_the_Iteration2_results_alone`;
    - `Iteration3_against_a_1a_and_a_2_reference_of_the_same_model_keeps_both_pairings_and_both_reopen`.
  - Iteration 3 tests updated from flat to per-case expectations.
  - The review fixture names Candidate B through `Qualifier_Reference`.
  - `PartOIteration3Fixture.Directory_Temp` pre-creates the Iteration 3 folders so a test can arrange files before a
    run.
- Evidence: `documentation/evidence/parto-output-folders-2026-09-30/` (two licensed acceptance logs).

## Evidence

- Build: `SAM.Analytical.UI.WPF.Tests` 0 errors.
- **Adversarial `Find()` tests** (the owner's review request). Chosen folders tried:
  - `Iteration1a`, `Iteration3`, `MixedDesign`;
  - `tas`, `reports`, `diagnostics`;
  - `Iteration2\tas`, `Iteration3\reports`, `Iteration1a\diagnostics`, `Iteration1a\Iteration3\tas`.

  None of them is recognised as the layout. For every case, the case folder is created directly inside the chosen
  folder, and `Root()` of the run's results is the chosen folder again (not its parent). Further checks:
  - A hand-made `...\Iteration2\tas` / `...\Iteration1a\tas` legacy run behaves as a flat folder: its report stays
    beside the results, nothing is filed out, a new run is rooted at the folder itself, and no `-It1a` is added.
  - A renamed case folder, and a corrupt, foreign-schema or missing marker, are all not recognised.
  - Recognition happens only at the layout's own depth. A SAM `tas` folder picked as a root gets a layout inside it,
    rooted there.
- **Iteration 3 1a vs 2:**
  - A path-level theory over all four methods shows disjoint owned files, `-It1a` throughout the 1a pairing, each
    reference's own TSD and report, and reopen derivation.
  - A runtime test runs 1a → Iteration 3, then 2 → Iteration 3, in one root. None of the 1a pairing's files changed
    (content and write time), both reviews reopen from their own reference, and no reference is copied.
- Focused Part O set (`PartOOutputFolder*`, `PartOIteration3*`, `PartOOptimisation*`, `PartOMixed*`,
  `PartOWorkflowSimplification*`): **451/451**.
- **Full WPF suite 1519/1519** on the committed code. The licensed acceptance is a no-op without its env vars.
- **Licensed TAS acceptance PASSED twice** (56 s, and 52 s after pass 2): a real full-year Mixed Design run (Flat 1
  natural, Flats 2-3 MVHR, IZAM route) into fresh scratch roots under `C:\TasOut\parto-output-folders-2026-09-30\`.
  - `MixedDesign/tas`: `.xml`, `.t3d`, `.tbd`, `.tsd`, `.sam`.
  - `MixedDesign/reports`: `-TM59.txt`.
  - `MixedDesign/diagnostics`: `.timing.csv`, with none left in `tas`.
  - `MixedDesign/PartOCase.json`: the marker.
  - Nothing else was written into the root, and no other case folder was created.

## Not verified / risks

- **No licensed Iteration 3 or 2B run.** The existing Iteration 3 licensed harness needs a disposable restorable
  Reference A, and a copied run resolves back to its original folder (the #146 trap), so running it could write into
  historical evidence.
  - The Iteration 3 and 2B folders, the `-It1a` qualifier and the timing filing are covered by unit and
    production-seam tests with fake TAS.
  - The TPD/bridge "folder must exist" precondition is met by the attempt-start `CreateDirectories`.
- **No native UI walk.** There are tooltips only; the Hub shows no output path during a run.
- **Choosing a folder inside an existing layout nests.** If a person chooses a SAM `tas` folder as the root (e.g. the
  Hub defaults to the folder of a per-run `.sam` opened from `Iteration2/tas`), the new layout is created inside it.
  This is by design, since the chosen folder is the root exactly; the Hub box shows the folder.
- **Pre-existing, unchanged:** the thermal source `<B>.tbd` and the TPD `<B>.tpd` share a base name, so the TPD's
  `<B>.timing.csv` replaces the workflow's (now in `diagnostics`).
- 2B tests whose fake baseline sits directly in `%TEMP%` now create `%TEMP%\Iteration2B\...` (with marker). This is
  harmless test residue.

## Deliberately deferred

Case selector, 2B UX, TPD performance, Iteration 3B, historical file migration, a SAM_Tas option to write timing
CSVs to a given folder.

## Next step

Owner reviews the structure on the PR. Then CI green → merge → `PROJECT_PROGRESS.md` closeout on `sow/2026-Q3`.
