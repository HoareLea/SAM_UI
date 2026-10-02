# Thermal Stage E0-2 - "My glazing systems" as `Change…` candidates + report lines (2 Oct 2026)

Second step of Stage E0 (`documentation/plans/Thermal-StageE0-GlazingBuilder-PLAN.md`, approved; E0-1 in `documentation/Thermal-StageE0-1.md`).
The E0-1 user library (`Documents\SAM\User Libraries\Glazing Systems.json`) becomes a source of every glazing `Change…` list in the Thermal
Performance panel, and the GLAZING CHANGE report names the system's Guid, its source and how a Builder system was built. **No Builder window, no
`Create new…`, no E0-3 work; no SAM / SAM_Tas change.** The mutation boundary is unchanged:

`My glazing systems (read only)` → existing `GlazingViewModel` / `GlazingCandidate` / evaluator → `CreateRequest` → `ThermalChangeSet` → proposed clone + check
→ `SetGlazing` → one `SetJSAMObject` → one Undo.

## What changed (existing files, smallest surfaces)

| File | Change |
|---|---|
| `Enums/GlazingSourceKind.cs` | `+ User` (appended, so existing values keep their numbers). |
| `Classes/Glazing/GlazingSource.cs` | `FromUserLibrary(UserGlazingLibrary)`: kind `User`, label **"My glazing systems"**, the library's `ConstructionManager` as read (systems + their materials). Missing file → empty, no note. Unreadable (corrupt, empty, wrong type, IO error) → empty source whose `Note` says "My glazing systems could not be used: … The other sources still work." `Rank(kind)`: Model 0, Library 1, User 2, Loaded 3. |
| `Classes/GlazingBuilder/UserGlazingLibrary.cs` | `event Changed` raised after a successful `Save` (after the lock is released, on the saving thread; a throwing listener neither stops the others nor fails the Save; not raised on failure). `static Shared` (lazy, `DefaultPath`): the one library of the process, so a Save through it reaches every open list. No change to reading / writing. |
| `Classes/Thermal/ThermalEditSession.cs` (`ThermalEditServices`) | `UserGlazing` (lazy; default `UserGlazingLibrary.Shared`; optional 7th constructor argument for tests). |
| `Classes/Glazing/GlazingViewModel.cs` | Optional `userSource` constructor argument; sources are **placed by rank** (`Place`); `SetUserSourceAsync(source)` replaces the user source in place (or inserts it at its rank); `Rebuild` resolves every Guid to the FIRST source in pool order (rank-correct whatever the arrival order), keeps a candidate (and its values) when its owner is unchanged or is the same source read again, and re-calculates a Guid whose owner changed; `SelectWhenAvailable(Guid)` + `PinnedGuid`. |
| `Classes/Thermal/ThermalRowEditor.cs` | `OpenChange` reads the user library into the pool (after the default library, before `AddReadySources`) and subscribes to `Changed`; `RefreshUserGlazingAsync(Guid? select)`, `SelectGlazing(Guid)`; `Changed` is handled on the context the list was opened on (posted when it arrives from another thread); unsubscribed in `DisposeViewModels` (Cancel, Discard, Apply, invalidation, dispose). `GlazingNotesText` / `HasGlazingNotes`. |
| `Controls/ThermalPerformanceControl.xaml` | One warning-styled `TextBlock` (`textBlock_GlazingNotes`) under the candidate count, collapsed unless a source has a note. |
| `Classes/Glazing/SetGlazingResult.cs` | `SourceLabel`, `SourceKind`, `BuilderProvenance` (from the request's `Source` and the chosen system's provenance set) - result fields only, no behaviour change. |
| `Query/GlazingChangeReport.cs` | Three lines after `Glazing:` (below). Everything else unchanged. |

New: `T/UserGlazingCandidateTests.cs` (32 tests), `T/Helpers/TestIsolation.cs` (module initializer: `UserGlazingLibrary.Shared` points at a file that never
exists, so a user library on the machine running the tests never reaches them).

## Source precedence and identity (unchanged contract, extended)

Pool order: **Model → Default library → My glazing systems → loaded / remembered sources (in the order added)**; the first source of a Guid wins; one
row per Guid; names and labels never define identity. A loaded source that arrived before the user library loses a shared Guid to it (re-evaluated
from the user source). Material safety is the existing `GlazingCandidate` / `MaterialIdentity` rule: same name + same content → reused (not added),
same name + different content → the candidate is blocked ("differs from the model's material").

## Refresh and the select-by-Guid hook (for E0-3)

* `UserGlazingLibrary.Changed` → `ThermalRowEditor.RefreshUserGlazingAsync()` → `GlazingViewModel.SetUserSourceAsync(GlazingSource.FromUserLibrary(lib))`:
  the user source is replaced in place; an already-listed system keeps its row and values (saved systems are immutable); a new one is added and
  only it is calculated; repeated refreshes add nothing. No polling, no file watcher (another process's save is seen on the next `Change…`).
* `ThermalRowEditor.SelectGlazing(guid)` / `RefreshUserGlazingAsync(select: guid)` → `GlazingViewModel.SelectWhenAvailable(guid)`: chosen now if listed,
  otherwise as soon as a refresh brings it; the chosen system is **pinned** (shown even if the target / g / LT filters would hide it) only while it
  stays the choice - choosing another system or none ends it and the filters apply again. Unused, the list filters and auto-chooses exactly as before
  (tested side by side with a list without the user source).
* E0-3 recipe: Builder `Save` through `session.Services.UserGlazing` (raises `Changed`, the open list refreshes) → `editor.SelectGlazing(saved.Guid)`.

## GLAZING CHANGE report (extended, nothing removed)

```
Glazing:      SIM_EXT_GLZ -> E0-2 Pilkington Double (added to the model; SIM_EXT_GLZ unchanged)
Guid:         0113b95d-fbb2-444e-b9db-ba8dc87b65b3
Source:       My glazing systems (user glazing library)
Built from:   SAM Glazing System Builder, saved 2026-10-02 15:09 UTC; based on SIM_EXT_GLZ (5ad2e36d-…); intended for WallExternal
              Panes (outside -> inside): 1. OptifloatClear4mm.NSG [4 mm, from International Glazing Database_v76-Pilkington.tcd] | 2. OptithermS1Plus4mm.NSG [3.9 mm, from …]
              Gaps (outside -> inside): 1. Argon 16 mm (HTC 1.160 W/m2K at 90 deg)
              Frame: copied from SIM_EXT_GLZ, width 50 mm
```

Source kinds: `Model (existing model system)`, `Default library (SAM default library)`, `My glazing systems (user glazing library)`,
`<file name> (added source)`. A system without Builder provenance: `Built from: not made with the Glazing System Builder (no Builder provenance)`.
Read from `SetGlazingResult` (request source + the provenance set the system carries) - no file is read while reporting. Any label given as a path is
cut to its file name in the report too (tested with drive, UNC and POSIX paths); a pane renamed on save shows `saved as <name>`.

## Tests

32 new (`UserGlazingCandidateTests`): source (absent / empty / one / several / corrupt ×3 kept byte-identical / label + kind / shared default);
precedence (Model > user, Library > user regardless of name, user > loaded even when loaded came first, duplicate in the library, material reuse vs
block); `Change…` integration (listed + evaluated, refresh ×3 no duplicates and no new Tas work, `Changed` while open adds exactly the new system and
calculates only it, save on another thread, unsubscribe on Cancel / Discard / Apply); select-by-Guid (via refresh, before arrival, already listed,
pinned despite target and released by another choice, unchanged behaviour without a request); mutation invariant (open / refresh / select / preview
/ check: model JSON identical, no `Modified`, no `HistoryChanged`, `CanUndo` false, library hash unchanged); Apply (same `ThermalChangeSet`, one
`Modified`, one Undo step, only the chosen system + its 4 missing materials, only the selected window, provenance set travels; one Undo restores the
model JSON exactly, `CanUndo` false again, no orphan system / material, library unchanged and still offered); report (Builder, path-sanitised,
non-Builder ×3, unknown source). Existing glazing / thermal tests unchanged (335 → all pass). **Full WPF suite 2107/2107** (2075 + 32).

## Mini real-app acceptance (real Tas; evidence kept locally, not committed)

Library written by the E0-1 route (scratch harness: Pilkington `.tcd` via `Query.ReadThermalSource` → draft clear 4 | Ar 16 | Optitherm S1 Plus 4
(coating surface 3), frame copied from `SIM_EXT_GLZ`, width 50 mm → `DraftGlazingEvaluator` real Tas Ug 1.047 / g 0.525 / LT 0.751 / Uf 2.202 →
`UserGlazingLibrary.Save`); the pane source file was given as a full path - the saved JSON holds the file name only. App = `SAM_UI\build` (Release) copy,
fresh representative model (20 windows `SIM_EXT_GLZ` U 1.243).

1. One window selected → `Change…` → list ready in 4.0 s: **"E0-2 Pilkington Double · Uw 1.18 · Ug 1.05 · Uf 2.20 · g 0.53 · LT 0.75 · My glazing systems"** (5 rows; no note).
2. Chosen + "Only the 1 selected": preview `Ug 1.24 → 1.05 · g 0.40 → 0.53 · light 0.80 → 0.75 · Uw 1.35 → 1.18`; check "✓ No new warnings"; `1 change · 1 element`;
   **Undo disabled**; saved model identical in content (only the stored active-view tab id differs from the pre-panel save - the driver switched to the 3D tab).
3. Apply (3.4 s): "1 aperture now E0-2 Pilkington Double. One Undo reverts it." **Undo enabled (one step)**; saved model: 19 × `SIM_EXT_GLZ` + 1 × `E0-2 Pilkington Double`
   (stored U 1.047); 3 materials added (the frame material was already in the model - reused); report saved with `Guid` / `Source` / `Built from`, no absolute source path.
4. Colour by g-value (legend 0.4 | 0.525), light transmittance (0.751 | 0.804), U windows (1.047 | 1.243).
5. One Undo: **Undo disabled again**; saved model JSON identical to the one before Apply (same SHA); 20 × `SIM_EXT_GLZ`; legend back to 1.243.
6. Library file SHA unchanged after preview, Apply and Undo.
7. New app session on the undone model → `Change…` → the system is still listed (once, with the same values) among 110 rows (model, default library, My glazing
   systems, and a remembered NCM `.tcd` source of 104 window systems).

The library file created for the acceptance was removed from `Documents\SAM\User Libraries` afterwards (copy kept with the local evidence).

## Left out / found on the way (not fixed here)

* The classic glazing window (`SetGlazingWindow`, "Glazing Calculator") still offers Model + Default library + "Load more glazing…" only; it was not asked
  for and is not part of the Thermal Performance `Change…` flow. Adding the user source there is one constructor argument if wanted.
* A save by ANOTHER SAM_UI process is seen on the next `Change…` (no file watching, by design).
* The candidate `ListBox` virtualises its rows: with a large remembered source, a user system ranked low by Uw is in the list but not realised for UI Automation
  until scrolled (drivers must use `ItemContainerPattern`); not a product defect.
* Report `Model:` still shows the model's own path (existing behaviour, unchanged).

## Next: E0-3

`GlazingBuilderViewModel` + `GlazingSystemBuilderWindow` (modal) + pane browser + `Create new…` in the open list; Save through `ThermalEditServices.UserGlazing`
then `ThermalRowEditor.SelectGlazing(saved.Guid)` (both prepared here); full §16 real-app acceptance.
