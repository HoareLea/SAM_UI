<!-- SPDX-License-Identifier: LGPL-3.0-or-later -->
<!-- Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors -->

# Part O output folders: one folder per case beneath the chosen root

**Status (30 Sep 2026): code, tests and one licensed acceptance done; PR open against `sow/2026-Q3`, NOT merged -
the owner reviews the structure first.** Branch `feature/parto-output-folders-2026-09-30` from `sow/2026-Q3`
`788e647` (SAM_UI#146 closeout). SAM_UI only - SAM (`83eb79a3`) and SAM_Tas (`057faf3`) are unchanged.

## The problem, as it was

Every Part O run wrote into the one output folder a person chose. The cases reuse file names - 1a, 1b and 2 all
write `<model>.tbd/.tsd/.sam/-TM59.txt` - so one case overwrote another. The worst case: a 1b run replaced the
Iteration 2 results an Iteration 3 pairing references (noted in the SAM_UI#145 closeout as "use a separate output
folder per iteration").

## The structure now

The person still chooses one folder - now the **Part O root**. SAM creates the rest:

```text
<root>/
  Iteration1a/  Iteration1b/  Iteration2/  Iteration2B/  Iteration3/  MixedDesign/
    tas/          .xml .t3d .tbd .tpd .tsd (+ bridge .tbd/.tsd), and the per-run .sam,
                  .partorun.json, .prepared.sam named from the TSD
    reports/      <run>-TM59.txt; Iteration 3 record <A>-Iteration3-<tag>.json and its
                  <A>-Iteration3-<tag>-Review.txt / .json
    diagnostics/  <name>.timing.csv, <tpd>.route.timing.csv, <B>-OperatingAirFlow.csv,
                  <tpd>_GuidanceOperation.csv (mixed)
```

File names are unchanged. Case meanings are unchanged: 1a MVHR design duty (no manufacturer unit), 1b natural
ventilation, 2 MVHR with manufacturer unit, 2B ventilation optimisation (every round and the capacity envelope),
3 explicit system and cooling assessment, MixedDesign (every screening and the final run).

## Design

**One resolver: `SAM.Analytical.UI.PartOOutputPaths`** (`SAM_UI/SAM.Analytical.UI/Classes/PartO/PartOOutputPaths.cs`)
with `PartOOutputCase` (`Enums/PartOOutputCase.cs`).

- `Create(root, case)` gives `Directory_Case/_Tas/_Reports/_Diagnostics`. A root that is already inside the layout
  resolves to its own root first (`Root`), so a case folder can never be nested.
- `Find(directory)` recognises the layout **by name only**: a directory is `<root>/<case>/tas|reports|diagnostics`.
  Anything else is a legacy flat folder. A hand-made flat `C:\TasOut\Iteration2` is legacy too - it is not
  `.../Iteration2/tas`.
- `CaseOf(PartOPreparationContext)`: `BaseNaturalVentilation` = 1b; `BasePassive` with a catalogue offered = 2,
  without = 1a; unknown (a resumed v1 record) = null, so the run writes into the chosen folder as before rather
  than guessing.
- `SimulationContext(context, case)`: the same TAS case writing into another case's `tas` (2B, and the root for 3).
- `Directory_Reports_ForResults` / `Directory_Diagnostics_ForFile`: the case's folder in the layout, beside the
  file in a legacy folder.
- `FileDiagnostics(tas)`: moves SAM_Tas' `*.timing.csv` from a layout `tas` folder into `diagnostics`. Never in a
  legacy folder, never fails a run.
- `CreateDirectories` / `EnsureDirectoryForFile`: SAM_Tas creates no folders; a legacy folder is never created.

**Consumers (every one goes through the resolver):**

| Writer | Change |
|---|---|
| `Modify.Simulate` (1a/1b/2, dialog + Hub) | Guided Part O only: `SimulateInputs.PartOOutputCase` → context `OutputDirectory` = case `tas`. The remembered option stays the root. The expert Simulate command is unchanged. |
| `Modify.RunPartOSimulation` (every Part O TAS run) | Creates the case folders before the first write; files the workflow timing CSV. |
| `Query.Path_TM59Report` (the one TM59 naming authority) | Layout results → case `reports`; legacy → beside the TSD. |
| `Modify.OptimisePartOTM59` (2B) | Rounds and envelope run with the baseline's case copied into `Iteration2B/tas`; the baseline is read where it is. |
| `PartOIteration3Paths` (3) | Candidate B TAS files in `Iteration3/tas`, OperatingAirFlow in `diagnostics`, record + review in `reports`. The root is read off Reference A's own folder. |
| `Modify.RunPartOIteration3` | Creates Iteration 3's folders at attempt start (it used to rely on the thermal source's folder); files route/TPD/bridge timing CSVs at its single exit. |
| Mixed Design (`Create.PartOMixedOutputDirectory`) | Context, the overwrite guard `RunModelsSafe`, `_GuidanceOperation.csv` → diagnostics, timing filed after each run. |
| UI | Tooltips on both output-folder boxes say it is the root and which folders SAM creates. The Mixed Design "Accept optimised airflow" dialog starts in `Iteration2B/tas` once it exists. |

## Decisions and assumptions

- **Per-run `.sam`, `.partorun.json`, `.prepared.sam` stay beside the TSD in `tas/`**, not at the case root. Every
  reader derives them from the TSD path (`Path_PartORunModel`, `PartORunResume`, `SimulationResultProvenance`'s
  fallback "TSD beside the .sam"), so moving them would add persistence risk. SAM_Tas's workflow `.json` is also a
  TBD sibling; it is still deleted after the `.sam` is written.
- **`.partomixed.json` is unchanged**: it sits beside the open model file, not in the output folder.
- **The Iteration 3 pairing record goes to `Iteration3/reports`.** It is the persisted result record, and there
  its `-Review.txt/.json` stay derived from it in the same folder. `Path_Report_ForRecord` is unchanged. The case root
  would have needed a name-based "is this a case folder?" guess, which a hand-made flat `Iteration3` folder breaks.
- **Iteration 3 references Iteration 2 where it is.** The record stores the actual `Iteration2/tas/<A>.tsd`. A review
  derives the record from Reference A's TSD alone: root = TSD folder's root → `Iteration3/reports`. Nothing is copied.
- **Timing CSVs are moved by SAM_UI after SAM_Tas writes them** (they are never read back). This avoids a SAM_Tas API
  change and a cross-repo PR.
- **Iteration 3 and 2B follow Reference A's / the baseline's root, not the Hub box.** Changing the Hub folder
  between runs does not split one pairing across two roots.

## Backward compatibility

- Old projects are read exactly as saved. Stored absolute paths are unchanged, nothing is moved, nothing is
  rewritten on open, and no migration is forced.
- A legacy flat TSD keeps its reports beside it. Its per-method record beside it is still found
  (`Query.PartOIteration3RecordPath`: new location → per-method beside the TSD → mode-independent legacy → new path).
  A new record, once written, supersedes it.
- A **new** run started from a legacy run treats the flat folder as its root. For example, Iteration 3 against a
  legacy `C:\out\Flat1.tsd` writes `C:\out\Iteration3\...` and cannot overwrite the legacy files.

## Files

- New: `SAM_UI/SAM.Analytical.UI/Classes/PartO/PartOOutputPaths.cs`, `SAM_UI/SAM.Analytical.UI/Enums/PartOOutputCase.cs`.
- Changed:
  - `Modify/Simulate.cs`, `RunPartOSimulation.cs`, `OptimisePartOTM59.cs`, `RunPartOIteration3.cs`,
    `ReviewPartOIteration3.cs`, `SavePartOTM59Report.cs`, `SavePartOIteration3Report.cs`, `RunPartOStrategySet.cs`,
    `RunPartOMixedDesignCommand.cs`, `SimulatePartOMaterialisationSystems.cs`;
  - `Query/Path_TM59Report.cs`, `Query/PartOIteration3RecordPath.cs`;
  - `Classes/PartO/PartOIteration3Paths.cs`;
  - `Windows/PartOWorkflowWindow.xaml`, `PartOMixedDesignWindow.xaml(.cs)`.
- Tests:
  - new `PartOOutputFolderTests.cs` (25) and `PartOOutputFolderAcceptance.cs` (env-gated, licensed);
  - new `ReviewTests.A_reference_in_Iteration2_pairs_into_Iteration3_and_reopens_from_the_Iteration2_results_alone`;
  - Iteration 3 tests updated from flat to per-case expectations. `PartOIteration3Fixture.Directory_Temp` pre-creates
    the Iteration 3 folders so a test can arrange files before a run.
- Evidence: `documentation/evidence/parto-output-folders-2026-09-30/mixeddesign-licensed-acceptance.txt`.

## Evidence

- Build: `SAM.Analytical.UI.WPF.Tests` 0 errors.
- Focused tests: `PartOOutputFolderTests` + all `PartOIteration3*`: 285/285.
  - Covers folder mapping for all six cases and TAS files together in `tas/`.
  - Covers reports in `reports/` and diagnostics in `diagnostics/`.
  - Covers the same file name in 1b vs 2, and across all cases.
  - Covers Iteration 3 keeping the Iteration 2 reference path, and reopening from it alone (paths + a runtime
    run → review).
  - Covers legacy flat paths.
- **Full WPF suite 1501/1501** on the committed code (the licensed acceptance is a no-op without its env vars).
- **Licensed TAS acceptance PASSED (56 s):** a real full-year Mixed Design run (Flat 1 natural, Flats 2-3 MVHR,
  IZAM route) into a scratch root `C:\TasOut\parto-output-folders-2026-09-30\PartO`.
  - `MixedDesign/tas`: `.xml`, `.t3d`, `.tbd`, `.tsd`, `.sam`.
  - `MixedDesign/reports`: `-TM59.txt`.
  - `MixedDesign/diagnostics`: `.timing.csv`, with none left in `tas`.
  - Nothing else was written into the root, and no other case folder was created.

## Not verified / risks

- No licensed Iteration 3 or 2B run. That TPD/bridge/route timing files land in `Iteration3/tas` and are filed into
  diagnostics is covered only by unit tests and the resolver. The TPD/bridge "folder must exist" precondition is
  met by the attempt-start `CreateDirectories`.
- No native UI walk: tooltips only; the Hub shows no output path during a run.
- **Known collision the layout does not remove:** an Iteration 3 run against an Iteration **1a** reference and one
  against an Iteration **2** reference of the same model share `Iteration3/` file names (`<A>-It3B*`, the record).
  Only the B0 route check can run against 1a. Pre-existing: in the flat layout 1a and 2 already collided with each
  other. Owner decision whether to qualify.
- Pre-existing, unchanged: the thermal source `<B>.tbd` and TPD `<B>.tpd` share a base name, so the TPD's
  `<B>.timing.csv` replaces the workflow's (now in `diagnostics`).

## Deliberately deferred

Case selector, 2B UX, TPD performance, Iteration 3B, historical file migration, the 1a-vs-2 Iteration 3 qualifier
above, a SAM_Tas option to write timing CSVs to a given folder.

## Next step

Owner reviews the structure on the PR. Then CI green → merge → `PROJECT_PROGRESS.md` closeout on `sow/2026-Q3`.
