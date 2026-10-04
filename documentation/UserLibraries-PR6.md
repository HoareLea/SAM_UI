# User libraries - PR6: final UI refinement / acceptance cleanup

Base `sow/2026-Q3` = `636ac1b9` (PR1-PR5 merged; PR5 = #189). SAM_UI only (`SAM.Analytical.UI.WPF`). Not a feature PR: every change below solves something seen in the real windows. PR1-PR5 behaviour, identity, archive, Apply and Undo semantics are untouched.

## 1. How it was surveyed

The three native acceptance drivers of PR3 / PR4 / PR5 (`ul_accept`, `uc_accept`, `fa_accept`) were re-run on the post-PR5 base (they all passed: model = baseline before Apply, one Apply = one change, one Undo = baseline) and every window of the completed journeys was looked at: Thermal panel (opaque row, alternatives, Save to My constructions), name prompt, My library (both tabs: list, details, rename, remove confirmation, empty state), classic Constructions editor, Glazing System Builder (copied frame, own frame, edit mode, error states, minimum window size). Windows or states the happy-path drivers do not reach (failures, empty selection, minimum size) were rendered from the real XAML over the fake Tas (a temporary harness, not committed).

## 2. Concrete problems found and the smallest change for each

| # | Observed | Change |
|---|---|---|
| 1 | The Thermal panel's `My library...` tooltip still described only glazing systems; since PR4 the window also has a Constructions tab. | Tooltip names both and says only a glazing system can be opened in the Builder. |
| 2 | My library's **Details** pane showed a heading over blank space whenever nothing was selected (after a Remove, on opening, after a refresh). | A one-line hint ("Select a system / construction to see ...") while the list has entries and none is selected. An empty library keeps its own empty state (no second hint). Both tabs. |
| 3 | The result line of Rename / Remove (both tabs) looked the same for a failure as for a confirmation, and survived choosing another entry (a stale "Removed 'X'" or error under an unrelated selection). | `MessageIsError` -> the line is red when it says why nothing changed; the line is cleared when the person selects another entry. Both tabs. |
| 4 | The Thermal panel's `Saved 'X' to My constructions.` line was shown identically when the save was refused (name taken, material not in the library). | `UserConstructionMessageIsError` -> red on a refusal. |
| 5 | Details build-up numbers are millimetres but carry no unit ("4 LowE4 / 16 Argon ..."), and the glazing Details had two lines starting `Frame:` (the layers, then how it was built). | Labels: `Pane (thickness in mm):`, `Frame layers (thickness in mm):` (`Frame layers: no frame`), `Build-up (thickness in mm):`. The `Frame: copied from ...` provenance line is unchanged. |

New public members (additive): `UserLibraryViewModel` and `UserConstructionLibraryViewModel`: `MessageIsError`, `DetailsPlaceholderText`, `ShowDetailsPlaceholder`; `ThermalRowEditor.UserConstructionMessageIsError`.

## 3. Looked at, deliberately NOT changed

* **Material collisions stay blocking** ("Its material 'X' differs from the model's material of the same name."). No automatic resolution; the wording is pinned by existing tests and already shown as a blocking reason with Apply disabled.
* **Cancel / close of the Builder with an edited draft discards without asking.** That has been the accepted behaviour since E0-3; a confirmation is a behaviour change, not a cleanup. Deferred.
* The 1 mm default thickness of the default library's timber stud (PR5 note) - the material's own value.
* Per-row and summary duplication of a finding (the layer line and the issue list both show it) - the E0-3 design for panes and gaps, now equally for frame layers.
* The engine text in Details ("Tas TCD (SAM_Tas ThermalTransmittanceCalculator.CalculateGlazing)") is the persisted provenance string shown as saved.
* The panel's `Saved 'X' ...` line keeps its lifetime (it reports the last save of that row).
* No Restore, Duplicate, hard delete, generic library abstraction, combined file, editable declared Uf, material editors, Grasshopper / SAM / SAM_Tas change.

## 4. Tests

`UserLibraryAcceptanceCleanupTests` (5): glazing and construction Remove refused by the library = error, success = confirmation, result cleared on selecting another entry; details hint while nothing is selected (not for an empty library) and the millimetre labels; the real window shows the refused result in the danger colour and the hint, on both tabs. `UserConstructionPanelTests` (3 assertions added): the panel's save result is an error for a refusal / rejection, not for a success. Existing tests needed no change (their pinned strings are not the ones touched).

## 5. Results

* Focused (`UserLibrary|UserConstruction|UserGlazing`): 206/206.
* Full WPF suite (Release, `dotnet test WPF\SAM.Analytical.UI.WPF.Tests`): **2428/2428** (PR5 + #188 baseline 2423 + 5).
* Baseline-state flakes seen during PR5's closeout (not PR6): one native testhost crash (ntdll access violation) in 1 of 5 runs and one timing race in `ThermalSourceTests.A_source_with_nothing_to_offer_says_why_and_adds_no_candidates` (a plain `[Fact]` asserting before an async read posts back; D2-era). Neither involves these files.

## 5b. Final native acceptance of the whole programme (real app + real Tas, Release build of this branch, representative model)

Run as one session of three drivers (`ul_accept`, `fa_accept`, `uc_accept` - the PR3, PR5 and PR4 acceptance scripts) plus the reuse driver (`uc_reuse`), on the PR6 build. Evidence is local (`C:\TasOut\uvalue\pr6\final`). All PASS:

| Area | Result |
|---|---|
| Glazing library (`ul`) | My library empty / readable states; Rename (Guid kept, list refreshed); Remove -> confirmation -> archive; candidate context menu; Builder edit mode (reversed pane), Save as new, Save and replace (new Guid, `Supersedes`, old system archived); applied-model copy unchanged. |
| Builder frame (`fa`) | copied frame edited (Uf follows, additional heat transfer read-only), own frame (2 layers, width), Save, reopen identical, Save as new (new Guid), Save and replace (archive keeps the old frame). |
| Opaque (`uc`) | Save to My constructions for the generated variant, a chosen alternative and the current construction (name asked; model, pending change and Undo untouched); Constructions tab Rename (open alternatives list refreshed) and Remove -> archive; classic Constructions editor `Save to My constructions...`. |
| Reuse (`uc_reuse`) | a DIFFERENT model in a NEW process lists the constructions the first session saved ("My constructions" alternatives), applies one (20 panels), one Undo. |
| No change before Apply | model JSON hash = baseline `EA79562E984F` after every library / Builder action in all three drivers; Undo disabled throughout. |
| One Apply, one Undo | each driver: Apply -> exactly one change (report), Undo enabled; one Undo -> Undo disabled and hash = baseline `EA79562E984F` (the whole JSON, so no view-state difference on the representative model). |
| Second model (reuse) | after Undo the JSON is BYTE-IDENTICAL to the original model file. Its "baseline" snapshot taken after launch differs from both only in `GuidAppearanceSettings` (`SAM.Geometry.UI` view appearance, 26 626 chars) - view state, not analytical. |
| Persistence | `Glazing Systems.json`, `.bak`, `.removed.json`, `Constructions.json`, `.bak`, `.removed.json`: all valid JSON, 0 machine-path markers (drive letters, `\Users\`). |
| Clean-up | the user's library restored to its pre-session state (`Glazing Systems.json` sha `3D481CE02046`; no `Constructions*.json` existed before); acceptance copies of the app and models live under `C:\TasOut`. |

The PR6 changes were seen natively: the Details hint after a Remove, the `(thickness in mm)` labels ("3 3 mm clear glass" now reads as 3 mm of "3 mm clear glass"), the rename confirmation. The error colouring (a refused Remove / save) cannot be provoked from the real UI without a race, so it is covered by the window test.

## 6. API / downstream (cumulative PR1-PR6)

Additive only: PR1-PR3 added 84 public members to `SAM.Analytical.UI.WPF` (PR4 and PR5 more, all new types or members) and none was removed (the one changed const, `GlazingBuilderProvenance.CurrentSchemaVersion`, is 3 since PR5; no consumer outside SAM_UI); PR6 adds the five members above and changes only strings, two XAML files and one tooltip. No file under `Grasshopper`, `Application` or `SAM_UI` references any user-library / Builder / `ThermalRowEditor` type (searched), and the only consumers of SAM_UI assemblies are SAM_UI's own Grasshopper projects (verified for PR3, which got the full compatibility gate: SAM.sln, SAM_Tas_Grasshopper, load smoke, 0 unresolved members). Because PR4-PR6 are additive over that gate and touch no type those projects use, no Grasshopper rebuild was run (it would also overwrite the deployed plugins).

## 7. Deferred

Confirmation before discarding an edited Builder draft; Restore from the archive; Duplicate; editable declared Uf -> synthetic frame; frame / opaque material editors; a "reset frame to the copy" button; per-edge frame widths / spacer Psi; the NCM v6.1e gap data (external).
