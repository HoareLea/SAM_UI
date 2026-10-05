<!-- SPDX-License-Identifier: LGPL-3.0-or-later -->
<!-- Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors -->

# Part O: Results > Part O > Remove Results... (a clean Mixed Design baseline from a run model)

**Status (30 Sep 2026): implemented and tested. The PR is open against `sow/2026-Q3` and is NOT merged. It depends on
SAM-BIM/SAM#169 (`Modify.RemovePartORunState`, branch `feature/parto-remove-results-2026-09-30`, head `a66a1393`). SAM_UI CI stays red until that
PR merges into SAM `sow/2026-Q3`. Merge SAM first.**

- Branch `feature/parto-remove-results-2026-09-30`, from `sow/2026-Q3` `bbc3944` (the SAM_UI#148 closeout).
- The SAM half has its own record: SAM `documentation/PartO-RemoveRunState-PR.md`.

## Problem

Mixed Design refuses any model that has been through Prepare & Run. The 30 Sep final acceptance was refused on the
owner's source with six findings. The only way on was to find an older `.sam`. The refusal text said "nothing is
cleaned or undone".

## What was removed, and why (investigation first)

SAM's validator `Query.PartOBaselineFindings` was run on the owner's model
(`SAM_daily\2026-09-29 partOi\model\000000_SAM_AnalyticalModel.sam`).

| Finding | In the model | Decision |
|---|---|---|
| Overheating scenarios | 4 | remove (stated by the run) |
| Simulation result provenance | 1 (points at a TSD that no longer exists) | remove (the link only; no file is touched) |
| Result objects in the cluster | 18 space, 220 surface, 4 zone | remove |
| Design days in the cluster | 2 | remove (model-level design days are inputs and stay) |
| Part O MVHR systems | `MVHR 1-3`, units `MVHR-01..03`, 9 terminals, 17 + 3 movements | remove. Left in place, materialisation refuses MVHR flats with `AuthoredAirMovementConflict` |
| Per-space Part F internal conditions | 8 (`Studio - Studio 1_0`, ...) | restore **only on evidence** (below) |

Kept: authored `NV 1`/`UV 1`/`MV 1`/`AHU1`, heating and cooling systems, geometry, zones, constructions, `PartFSpaceData`,
weather and the equipment selection.

## Decisions (owner, 30 Sep 2026)

1. **The Part F condition restore needs evidence.** `ApplyPartFVentilationRates` replaced each condition with a clone
   and zeroed six airflow bases; what they held is not stored. A space is restored only when the model holds the base
   condition, it states no airflow, and the clone agrees with it on everything else. Otherwise the space keeps its
   Part F condition, the window lists it under "Left in the copy", and the check FAILs. This amends SAM's D1 for
   the provable case only.
2. **The rules live in SAM** (`Modify.RemovePartORunState`, beside the validator). The Part O MVHR type guid and the
   movement scope are internal to SAM. SAM_UI adds no removal or validation rule.
3. **Default action: Save cleaned copy..., never an overwrite.**
   - The open model is not changed.
   - The copy goes to a new file. The default is `<model>-Cleaned.sam` beside the model, and the model's own path
     is refused.
   - No TAS or result file is deleted.
4. **The check after saving is of the SAVED file**, read back from disk and validated by
   `Query.PartOBaselineFindings`, the same authority `PartOMixedDesignSession` asks. PASS or FAIL is shown with a
   glyph and text (never colour alone) and every finding, not truncated.
5. **Placement.** Results tab > Part O group > "Remove Results...". General > Remove, the existing selective
   result-type removal, is unchanged.

## Change

- `Classes/PartO/PartORemoveResults.cs`: calls SAM's cleaner and validator; `Save(path, path_Model)` refuses the
  open model's path, writes, reads back and re-checks; `DefaultPath`.
- `Windows/PartORemoveResultsWindow.xaml(.cs)`: Removed from the copy / Left in the copy / Mixed Design baseline check
  (PASS/FAIL, subject "the cleaned copy" and then "the saved copy"); `Save cleaned copy...` (default), then
  `Open cleaned copy` (default once saved) and `Close`.
- `Modify/RemovePartOResultsCommand.cs`: the command, the save dialog, and open-after-save through the window's File >
  Open path.
- `Windows/AnalyticalWindow.xaml(.cs)`: the ribbon button, enabled whenever a model is open.
- `PartOMixedDesignSession.Readiness` and `PartOMixedDesignWindow`: the refusal text now points at Remove Results
  instead of saying "nothing is cleaned or undone".

## Files

- `WPF/SAM.Analytical.UI.WPF/Classes/PartO/PartORemoveResults.cs` (new)
- `WPF/SAM.Analytical.UI.WPF/Windows/PartORemoveResultsWindow.xaml`, `.xaml.cs` (new)
- `WPF/SAM.Analytical.UI.WPF/Modify/RemovePartOResultsCommand.cs` (new)
- `WPF/SAM.Analytical.UI.WPF/Windows/AnalyticalWindow.xaml`, `.xaml.cs`
- `WPF/SAM.Analytical.UI.WPF/Classes/PartO/Mixed/PartOMixedDesignSession.cs`, `Windows/PartOMixedDesignWindow.xaml.cs`
  (wording)
- `WPF/SAM.Analytical.UI.WPF.Tests/PartORemoveResultsTests.cs` (new, 9 tests)
- `documentation/PartO-RemoveResults-PR.md` (this record)
- `documentation/evidence/parto-remove-results-2026-09-30/` (real-model log, native smoke)

## Evidence

- **Build.** `SAM.Analytical.UI.WPF` builds with 0 errors against the SAM branch.
- **`PartORemoveResultsTests`: 9/9.**
  - A run model is cleaned to PASS, the open model is byte-identical, and `PartOMixedDesignSession.IsCleanBaseline`
    is true on the copy.
  - A clean model has nothing to remove and PASSes.
  - An unprovable condition is kept, and the check FAILs with SAM's reason.
  - Save writes a new file, the check is of the read-back file, and the model file hash is unchanged.
  - Save refuses the open model's own path, case-insensitively.
  - An unsaved model has no default path.
  - Window: Save is the default, then PASS of the saved copy, and Open cleaned copy becomes the default.
  - Window: FAIL lists every finding with a glyph.
  - Window: a clean model has nothing to save.
- **Full WPF suite: 1539/1539** (1530 + 9 new). The repo's `WpfCollectionTests` guard caught a missing
  `[Collection(WpfCollection.Name)]` on the first run; it was added.
- **Mutation check.** With the open-model refusal removed from `Save`, `Save_NeverOverwritesTheOpenModel` fails.
  Restored, 9/9 pass.
- **Real model, headless:** 6 findings → 0, and still 0 after save and reopen. The source SHA-256 `25DD56B6…` is
  unchanged. Log: `evidence/parto-remove-results-2026-09-30/real-model-clean.txt`.
- **Native smoke: PASS** (`evidence/parto-remove-results-2026-09-30/SMOKE.md`, `journey-smokeRR.txt`, `shots/`). Real
  `SAM Analytical.exe` on a copy of the owner's model:
  - Results > Part O > Remove Results... shows PASS for the cleaned copy.
  - Save cleaned copy... uses the offered `-Cleaned.sam`, and the check then reads PASS for the saved copy.
  - Open cleaned copy, then Mixed Design, shows "Baseline: clean — 3 dwellings", with Check and Screen enabled.
  - The source is unchanged, nothing is deleted, and no TAS process ran.

## Risks / not verified

- The SAM-side residual risks are in SAM's record. The main one: a per-space airflow basis that the held base did
  not state is not recoverable.
- The main window title shows the model's internal name, which the copy keeps, so the title does not change when
  the cleaned copy is opened. This is existing behaviour and was not changed.
- Open cleaned copy replaces the open model exactly as File > Open does, with no unsaved-changes prompt, which is the
  existing File > Open behaviour.
- No licensed TAS Mixed Design run on the cleaned copy was done in this PR. The native smoke stops at Mixed Design
  accepting the baseline.

## Next step

Owner review. Merge SAM first, then re-run this PR's CI and merge it.
Optional follow-up: the 30 Sep acceptance's Mixed Design phase (Flat 1 NV, Flat 2 MVHR, Flat 3 MVHR + cooling) on the
cleaned copy.
