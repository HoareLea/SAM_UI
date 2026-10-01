# U-value workflow PR3: "Set glazing" window, entry points, check and report (PR record)

Branch `feature/uvalue-pr3-glazing-2026-10-01`, from `sow/2026-Q3` `4f3bdd0a`. **Not merged.**
Plan: [plans/UValue-Workflow-PLAN.md](plans/UValue-Workflow-PLAN.md);
earlier PRs: [UValue-SetUValue-PR2A.md](UValue-SetUValue-PR2A.md), [UValue-Window-PR2B.md](UValue-Window-PR2B.md).
Prerequisite PR3-0 (the SAM_Tas `.tcd` importer made linear) is merged as SAM_Tas#79 (`326770bd`).

## Status

Implemented, unit/window-tested (full WPF suite **1812 passed, 0 failed**: 1700 before + 112 new) and accepted in the
real app (UIA-driven `SAM Analytical.exe` from a copy of `SAM_UI\build`, real Tas, fresh model copy). The solution builds
(MSBuild, Release). SAM_Tas is unchanged by this PR. Owner decision applied: **Tools > Glazing Calculator opens the new
window; the legacy flow stays as "Glazing Calculator (classic)"**.

## What is added

All in `WPF/SAM.Analytical.UI.WPF` unless noted.

- **`Classes/Glazing/`**
  - `GlazingSource` (Model / Library / Loaded; `FromModel`, `FromDefaultLibrary`) and `GlazingCandidate`. A candidate's
    identity is its **Guid** (several default systems share the name `SIM_EXT_GLZ`); `ShortId` is the last 6 of the Guid.
    A candidate knows the materials it would add and any `MaterialIssue`, including "differs from the model's
    material" (JSON compare ignoring Guid and valueless parameters).
  - `IGlazingEvaluator` + `TasGlazingEvaluator`: one STA worker, one `ThermalTransmittanceCalculator.CalculateGlazing`
    per source batch.
  - `GlazingViewModel`, `GlazingCandidateRow`, `SetGlazingRequest` / `SetGlazingResult`, `GlazingValues`.
  - `GlazingSourceCache`: JSON cache of the converted `.tcd` under `%LOCALAPPDATA%\SAM\cache\tcd`, keyed by
    path + size + mtime + SAM_Tas assembly version/build time (removes the 19 COM reads per material on a repeat load).
- **`Query/`**
  - `GlazingUw`: Uw weighted by pane/frame areas of `new Aperture(aperture, candidate)`; a frameless candidate gets
    Uw = Ug; an aperture without geometry gets a labelled 80/20 approximation.
  - `GlazingSourceReader`: dialog-free `ReadGlazingSource[Async]`; TCD constructions become aperture constructions that
    keep the construction Guid; windows take transparent systems, doors opaque ones; a pane library gets the note
    "N panes and no glazing systems".
  - `GlazingCheck`: the scoped check, built from **both** `Create.Log(ApertureConstruction)` and
    `Create.Log(ApertureConstruction, MaterialLibrary)` plus the apertures and panels it touches (never the whole model).
  - `GlazingChangeReport`: plain-text `GLAZING CHANGE` (Uf "none" for a frameless system).
- **`Modify/SetGlazing.cs`**: one model clone, exactly one `SetJSAMObject` (one Undo step); adds only the chosen system and
  its missing materials; a name clash with another model aperture construction gets a unique suffix (`<name> 2`); the
  source stays stored. Aperture U/g/LT parameters come from a Tas `Calculate` of the chosen system, because the whole-model
  `Tas.Modify.UpdateThermalParameters` does not cover aperture constructions.
  `OpenSetGlazingWindow.cs`; `SaveGlazingChangeReport` shares a helper with `SaveUValueChangeReport`.
- **`Windows/SetGlazingWindow.xaml(.cs)`** (1100 x 780, `PartOStyles`): candidate DataGrid; a
  `Current | Proposed | Target | Margin | Status` block; inline apply scope; **Advanced** (scope all / selected / don't
  assign, include library / loaded systems); "Load more glazing..." loads on a worker thread (window stays responsive);
  after Apply the check line with **Details**, the saved report and **Copy All**.
- **Entry points** (all open the same window)
  - 3D right-click on apertures > **"Set glazing..."** (`AnalyticalWindow`).
  - **Tools > Glazing Calculator** is now a `RibbonSplitButton`: the button opens the new window with a picker; the
    drop-down keeps **"Glazing Calculator (classic)"** (x:Name `RibbonMenuItem_GlazingCalculator_Classic`).
  - **Edit > Aperture Constructions**: opt-in **"Set glazing..."** button on `ApertureConstructionLibraryWindow`
    (`SetGlazingRequested`, `SAM_UI/SAM.Analytical.UI`). The list edits a copy, so it closes first; unsaved edits prompt
    like PR2b.

## Real-app acceptance

App: copy of `SAM_UI\build`; model: fresh copy; driver and evidence are local (`C:\TasOut\uvalue\pr3`, not committed).

| Check | Result |
|---|---|
| Tools > Glazing Calculator | window in 0.6 s; picker, then a table of 5 glazed systems in 650 ms (Tas). Current `SIM_EXT_GLZ`: Ug 1.24, Uf 2.20, g 0.40, LT 0.80, Uw 1.35 |
| Parity with the classic calculator | classic Glazing Calculator on `SIM_EXT_GLZ`: g 0.4, LT 0.804, U 1.243, identical to the new table |
| Load more: `Constructions.tcd` (cold) | window stays responsive; 171 systems; Tas 2.9-3.7 s for the pool; from the cache < 1 s. Import 6.5 s cold / 49 ms warm |
| Load more: IGDB (cold) | 67 s with UIA reads ~0 ms (UI not blocked), then the note "contains 11,664 panes and no glazing systems..."; warm reload 2.7 s incl. dialog (import 66.7 s cold / 478 ms warm) |
| Target Uw <= 1.3 | `sgcoolk\16` chosen (pane-only, Uw 1.27) |
| Apply | 20 apertures reassigned; 3 materials added (18 to 21); U/g/LT parameters refreshed (U 1.243 to 1.274, g 0.4002 to 0.341, LT 0.8036 to 0.797); scoped check "1 warning ... no Frame ConstructionLayers" (matches the Edit > ModelCheck record); report saved beside the model |
| One Undo | 20 apertures back on `SIM_EXT_GLZ`; 1 aperture construction; 18 materials; original values. Redo reapplies |
| Don't assign | `SIM_EXT_GLZ_SKY` + 1 material added, no aperture changed; Undo restores |
| Load more then Cancel | Undo disabled; saved model identical to the original |
| Edit > Aperture Constructions > row > Set glazing... | list closes, window opens |
| Interactions | Tools route with Load more ~8; without Load more ~5-6 |

**Not driven in the real app: the 3D right-click entry.** The 3D viewport did not rotate/zoom under injected mouse input,
apertures are not visible from the default camera and the plan tabs show none. That menu item is verified by code review
only (it calls the same `OpenSetGlazingWindow` the other routes use; the window itself is covered by window tests).

## Found in the first real-app pass and fixed

1. The scoped check missed SAM's frame rule ("no Frame ConstructionLayers"): it now uses both `Create.Log` overloads.
2. Solid systems were offered as glazing: they no longer are.
3. Combo items now have a proper `ToString`.
4. The status glyph for a neutral state is now neutral.
5. Selected-row style fixed.
6. The report says "none" for Uf of a frameless system.

## Tests (`SAM.Analytical.UI.WPF.Tests`, 112 new; full suite 1812 passed, 0 failed)

- `GlazingViewModelTests`: sources, candidate identity, filtering, target/margin/status, choosing, scope and Advanced
  options, blocked candidates and their reason, evaluator failure.
- `SetGlazingTests`: one history step, only the chosen system and missing materials added, name clash, assign scopes,
  parameters refreshed, source kept, failure leaves the model untouched.
- `GlazingSourceReaderTests` (reader, cache, pane-library note) and `GlazingReportTests` (check, report, save path).
- `SetGlazingWindowTests` (STA, `WpfCollection`): opened from apertures and from Tools; picker; target filters and
  chooses; row click; Tas failure shown; Apply changes the model once, shows the check and saves the report; unsaved model
  offers Copy All only; failed Apply leaves model and history alone; Advanced; selected-only disabled with no selection;
  action bar inside the smallest window.
- `UIJSAMObject.Undo` restores asynchronously, so unit tests assert one history step only; the restore itself is
  verified in the real app (table above).

## Decisions and assumptions

- Candidate identity is the Guid, not the name.
- The main Glazing Calculator opens the new workflow; the old one stays as "(classic)".
- IGDB is a **pane library** (11,664 panes, no glazing systems). No pane + gap + pane composition in PR3 (owner decision;
  separate future scope).
- A chosen system whose name clashes with another model aperture construction is added as `<name> 2` (legacy post-steps
  match by name).
- Opaque windows/doors: windows take transparent candidates, doors opaque ones; an opaque current system keeps opaque
  candidates available.
- Inline busy state and a wait cursor, no progress dialog; Load more runs on a worker thread.

## Risks / not covered

- **Frameless candidates** (every TCD-imported system) get Uw = Ug and a warning that the apertures lose their frame; the
  table labels them "no frame". There is no preference for framed candidates beyond that label.
- No automatic pane + gap + pane composition from IGDB panes.
- Pre-existing latent: an unknown TCD material type in a material folder throws `NullReferenceException` in the importer
  (documented in SAM_Tas#79).
- Whole-model `Tas.Modify.UpdateThermalParameters` ignores aperture constructions; `SetGlazing` calculates the chosen
  system itself, so a later whole-model update will not refresh them.
- The 3D right-click entry, the Edit > Aperture Constructions unsaved-edits prompt, "Tas unavailable" and the Copy All
  clipboard were not driven in the real app.
- Evidence uses a folder copy of `SAM_UI\build`, not an installer.

## Files changed

- New: `Classes/Glazing/*` (10 files), `Enums/Glazing{ApplyScope,PreviewStatus,SourceKind,UwBasis}.cs`,
  `Modify/SetGlazing.cs`, `Modify/OpenSetGlazingWindow.cs`, `Query/Glazing{Uw,SourceReader,Check,ChangeReport}.cs`,
  `Windows/SetGlazingWindow.xaml(.cs)`, `SAM_UI/SAM.Analytical.UI/Classes/EventArgs/SetGlazingRequestedEventArgs.cs`.
- Changed: `Windows/AnalyticalWindow.xaml(.cs)` (context-menu item, Tools split button), `Modify/EditApertureConstructions.cs`
  (hand-over), `Modify/SaveUValueChangeReport.cs` (shared helper),
  `SAM_UI/SAM.Analytical.UI/Windows/ApertureConstructionLibraryWindow.xaml(.cs)` (opt-in button + event).
- Tests: `GlazingViewModelTests.cs`, `SetGlazingTests.cs`, `GlazingSourceReaderTests.cs`, `GlazingReportTests.cs`,
  `SetGlazingWindowTests.cs`, `Helpers/GlazingFixture.cs`.
- Docs: this record.

## Next step

Wait for green CI and explicit merge approval. PR4 (U-value / Used-by columns, restyling, and whether the classic
calculators stay) remains optional.
