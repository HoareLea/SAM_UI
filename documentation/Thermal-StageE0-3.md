# Thermal Stage E0-3 - Glazing System Builder UI (2 Oct 2026)

Third and last step of Stage E0 (`documentation/plans/Thermal-StageE0-GlazingBuilder-PLAN.md`, approved; E0-1 domain in `documentation/Thermal-StageE0-1.md`,
E0-2 candidates in `documentation/Thermal-StageE0-2.md`). The user-visible Builder: **`Create new…`** in the open glazing list of the Thermal Performance
panel opens an owned, modal **Glazing System Builder**; `Save as predefined` writes the E0-1 user library, the E0-2 refresh brings the new system into the
open list and chooses it, and Apply is the existing `ThermalChangeSet` (one model change, one Undo). **No SAM / SAM_Tas change, no classic-tool change.**

`Thermal Performance → glazing Change… → Create new… → Glazing System Builder → Save as predefined → the new system is listed and chosen → preview / check →
Apply → one Undo`

## Boundary (unchanged from the plan)

The Builder edits a TEMPORARY `GlazingSystemDraft` and holds **no analytical model** (it is given snapshots: the seed system with its materials, the model's /
default library's / "My glazing systems" pools, the panel's source catalogue, the user library and its own Tas calculation). Opening it, editing, previewing,
saving and cancelling therefore cannot change a model or add an Undo step; the structural scan of E0-1 (`The_Builder_HoldsNoModel_AndCannotChangeOne`) now covers
the new Builder types too. Save writes ONLY "My glazing systems" (`UserGlazingLibrary.Save`); its own `Changed` event refreshes the open list (no row is added by
hand), and the editor then chooses the new Guid through the E0-2 hook (`ThermalRowEditor.SelectGlazing`).

## What was added (WPF assembly; existing files touched only as listed)

| Component | File | What |
|---|---|---|
| View-model | `Classes/GlazingBuilder/GlazingBuilderViewModel.cs` | WPF-free Builder: seed (`SeedDraft`: the chosen candidate else the current system; OUTSIDE → INSIDE via `GlazingLayerOrder`, gas layers → gaps, frame copied, intended use, additional heat transfer), default name `<seed> (copy)` (kept unique in the library: `(copy 2)`…), layer commands (add pane after the selection / at the inside end with an automatic gap when it would touch a pane, replace pane, add / remove gap, move, **Reverse** = the E0-1 Gate 0 semantics), frame (none, or copy the frame of a model / default / user system; explicit width, the depth proposed and SAID so when the copied frame stores none), intended use, check (E0-1 `CheckGlazingDraft`, with the library's names), performance, Save, `Saved` event. Edits are refused while a Save runs. |
| Rows | `Classes/GlazingBuilder/GlazingBuilderLayerRow.cs` | A line of the build-up (pane values, or gas + width boxes + the gap's HTC), its findings, an accessible name; `GlazingBuilderIssueRow` (severity in words). A gas / width edit changes the draft layer **in place** (no list rebuild, focus survives). |
| Pane browser | `Classes/GlazingBuilder/GlazingPaneBrowser.cs`, `GlazingPaneEntry.cs` | Panes of the model's materials, the default library and the **panel's own `ThermalSourceCatalog`** (remembered + added `.tcd` / JSON - no second source framework; `Add source…` adds to the same remembered list and the panel lists it too). Each source is projected ONCE on a worker thread; a remembered file is read only when chosen; a source with no panes, or unreadable, is not offered (a notice says why for a file just added). Search = all words in name / display name / category (150 ms debounce), sort by column, virtualised list. Pane sources are known by label and file NAME only. |
| Options / helpers | `Classes/GlazingBuilder/GlazingBuilderOptions.cs` | `GlazingBuilderOptions` (what the Builder is given), `GlazingIntendedUse`, `GlazingFrameChoice`, `GlazingReferenceWindow` (the labelled example Uw). |
| Window | `Windows/GlazingSystemBuilderWindow.xaml(.cs)` | Owned modal window, resizable, min 940 × 620; build-up list (OUTSIDE … INSIDE, no `Expander` in templates), buttons, frame, pane browser, performance block, validation list, `Cancel` / `Save as predefined` (closes only after a successful Save; disabled on blocking errors). AutomationIds + names everywhere; Alt+Up / Alt+Down reorder, Delete removes (typing in a gap's own boxes is left alone). Disposes the Builder's own Tas worker on close. |
| Panel | `Controls/ThermalPerformanceControl.xaml(.cs)` | `Create new…` in the open list's header (visible only while the list is open and "My glazing systems" is available); `ShowBuilder` injectable (a test supplies its own). |
| Row editor | `Classes/Thermal/ThermalRowEditor.cs` | `CanCreateNew`, `CreateBuilder()` (seed + snapshots + services; subscribes `Saved` → `SelectGlazing`). |
| Services | `Classes/Thermal/ThermalEditSession.cs` | `ThermalEditServices` + optional `builderEvaluator` / `builderComposeOptions` (tests inject stand-ins; the default is a new `DraftGlazingEvaluator` with its own Tas worker per Builder). |

## Performance block

`Ug - centre of pane - Tas`, `g`, `LT`, `Uf - frame layers - 1-D - Tas` from the E0-1 `DraftGlazingEvaluator` (350 ms debounce, content-keyed cache, generation numbers),
plus the labelled example **"Uw example x.xx W/m²K — 1.23 × 1.48 m, frame width X mm, no spacer Ψ"** (`GlazingReferenceWindow`; frameless: "= Ug"). The stored glazing
U-value is never called Uw. While Tas works the previous values are greyed ("Calculating…"); **an answer for an older build-up is dropped** (VM-level version +
the evaluator's generation; tested with a scripted Tas answering the newest request first). A build-up with errors asks Tas nothing ("Not calculated: correct the errors").

## Tests (all new; no existing test changed except the structural scan's type list)

| File | Tests | What |
|---|---|---|
| `GlazingBuilderViewModelTests` | 32 | seeding (order, gas, frame, width, panel type), unique default name, edits (add / replace / gaps / move / reverse / frame / use), command announcements, gating of Save, stale answers, failed Tas, no model, Save / Cancel / failed Save / race on the name / edits refused while saving / disposal |
| `GlazingPaneBrowserTests` | 20 | sources, search theory, sort, remembered-read-when-chosen, Add source through the shared catalogue (remembered once, chosen at once, notices), forgotten source, 11,664-pane projection / search / sort timing |
| `GlazingBuilderIntegrationTests` | 8 | `Create new…` availability, seeds, **Save refreshes the open list once and chooses the new system with the model JSON, history and Undo untouched**, Cancel, failed Save, Apply afterwards = the existing change set (one `Modified`, one Undo, only the system + its missing materials, one Undo restores the model exactly), two systems, a fresh panel offers what was saved |
| `GlazingSystemBuilderWindowTests` (STA / WPF) | 11 | named controls + minimum size + no `Expander`, build-up lines and gap boxes, virtualised pane list (11,668 panes, ≤ 200 realised), buttons, keyboard reorder, Save gating / success / failure, Cancel, disposal, and `Create new…` in the real panel → save → chosen → Apply → one change |

71 new tests; focused 71/71; **full WPF suite 2178/2178** (E0-2 base 2107 + 71). Fakes only for normal runs (E0-1 `FakeDraftTas`, E0-2 temporary library files, `FakeSourceReader`).

## Real-app acceptance (real Tas, Pilkington v76 subset; evidence kept locally, not committed)

App = a Release `SAM_UI.sln` build copy; model = the representative model (12 walls U 0.26, 20 windows `SIM_EXT_GLZ` stored U 1.243 / g 0.40 / LT 0.804); real Tas; pane source =
`International Glazing Database_v76-Pilkington.tcd` (Tas Data `Databases` folder, 1,066 panes) added with the Builder's `Add source…`. Driven through UI Automation in two app sessions
(drivers `e03_build.ps1` / `e03_apply.ps1`, kept with the local evidence). The model was saved (File > Save) and compared by JSON hash at every step.

**Phase A - build and save (one session)**

1. Baseline: library file absent; Undo disabled; model JSON hash `EA79562E984F`.
2. One window selected → `Change…` (list ready in 6.5 s, 109 systems) → **`Create new…`** → Builder open in 0.2 s, seeded "SIM_EXT_GLZ (copy)": real Tas **Ug 1.24 / g 0.40 / LT 0.80 / Uf 2.20**
   (= the model's stored values), "Uw example 1.38 W/m²K — 1.23 × 1.48 m, frame width 50 mm, no spacer Ψ"; the copied frame has no stored width, so its 50 mm depth is proposed and the note says so.
3. **`Add source…`** (Pilkington `.tcd`): chosen as soon as it was added, ready after 6.9 s ("International Glazing Database_v76-Pilkington.tcd · 1,066 panes"), remembered in the panel's source list too.
4. **E0 Double** = Optifloat Clear 4 mm | Argon 16 mm | Optitherm S1 Plus 4 mm (search "OptifloatClear4" / "OptithermS1Plus4", Replace pane ×2, gap Argon 16, frame width 50, name):
   Tas **Ug 1.05 / g 0.53 / LT 0.75 / Uf 2.20**, Uw example 1.21 - the same values as the E0-1 Gate 0 probe for this build-up. A gap-width edit showed its new Ug **0.8-1.0 s** after the edit (350 ms debounce + one Tas call + the driver's polling).
5. **Save as predefined**: the window closed in 0.4 s; the library file appeared (8,282 bytes); the open list went from 109 to 110 systems and **chose** the new one ("Ug 1.24 → 1.05 · g 0.40 → 0.53 · light 0.80 → 0.75 · Uw 1.35 → 1.18", "1 change · 20 elements");
   **Undo still disabled**.
6. **Create new…** again (seed = the chosen E0 Double, "New · based on E0 Double") → **E0 Triple** = Clear 4 | Argon 12 | Optitherm S1 Plus 4 | **Krypton** 12 | Clear 4: Tas **Ug 0.95 / g 0.49 / LT 0.69 / Uf 2.20**, Uw example 1.13. Saved: 111 systems, chosen
   ("Ug 1.24 → 0.95 · … · Uw 1.35 → 1.10").
7. After both Saves the model JSON hash was **`EA79562E984F` = the baseline**, Undo disabled.
8. **Cancel path**: `Create new…` → add a gap, rename → Cancel: library hash unchanged, list unchanged (111), model hash = baseline, Undo disabled.
9. The library file holds both systems and the file NAME of the pane source only: no drive path, no "Tas Data".

**Phase B - restart, apply, undo (a new app session)**

1. New session: `Change…` lists **both** systems (111 systems) - persisted. Chosen **E0 Triple**, "Only the 1 selected": preview "Ug 1.24 → 0.95 · g 0.40 → 0.49 · light 0.80 → 0.69 · Uw 1.35 → 1.09",
   "Before apply: ✓ No new warnings", "1 change · 1 element", "Adds E0 Triple and 4 materials to the model; SIM_EXT_GLZ stays unchanged"; model hash = baseline, Undo disabled.
2. **Apply** (3.6 s): "1 aperture now E0 Triple. One Undo reverts it."; Undo enabled; saved model: **1 window E0 Triple (stored U 0.953), 19 × SIM_EXT_GLZ; 2 aperture constructions; 22 materials = 18 + the 4 missing ones** (Optifloat Clear, Optitherm S1 Plus,
   Argon_12mm…, Krypton_12mm…) - nothing else entered.
3. **GLAZING CHANGE report**: `Guid: d2b1eade-…`, `Source: My glazing systems (user glazing library)`, `Built from: SAM Glazing System Builder, saved … based on E0 Double …` with the panes (outside → inside, "from International Glazing Database_v76-Pilkington.tcd" -
   a pane seeded from a Builder system keeps its original provenance), the gaps (Argon 12 mm HTC 1.403 / Krypton 12 mm HTC 0.958), the frame; no absolute source path (the `Model:` line is the model's own, unchanged).
4. Colour by: g **0.4 | 0.486**, light transmittance **0.689 | 0.804**, U (windows & doors) **0.953 | 1.243** - the applied window recolours.
5. **One Undo**: Undo disabled again; legend back to 1.243; the saved model JSON hash = baseline (`EA79562E984F`); 18 materials, 1 aperture construction.
6. The library file hash was unchanged by preview, Apply and Undo and still has both systems; a new `Change…` lists both again.

## Left out deliberately / found on the way (not fixed here)

* No remembered Builder window bounds (the plan mentioned `ThermalFloatingBounds`); `WindowStartupLocation=CenterOwner` is enough for a modal dialog.
* No in-place edit / remove of a saved system, no frame authoring, no target-Uf synthesis (plan §19 decisions).
* The pane browser has no thickness / g filters or favourites (plan: optional / deferred).
* A save by ANOTHER SAM_UI process is seen on the next `Change…` (no file watching, by design - E0-2).
* UI Automation note for drivers: the candidate `ListBox` and the pane `ListView` virtualise; find items with `ItemContainerPattern`, read the selection with `SelectionPattern`
  (it can be empty when the chosen row is not realised - the preview line and `N changes · M elements` show the choice).
