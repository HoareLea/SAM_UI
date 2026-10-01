# U-value workflow PR3: "Set glazing" window, entry points, check and report (PR record)

Branch `feature/uvalue-pr3-glazing-2026-10-01`, from `sow/2026-Q3` `4f3bdd0a`. **Not merged.**
Plan: [plans/UValue-Workflow-PLAN.md](plans/UValue-Workflow-PLAN.md);
earlier PRs: [UValue-SetUValue-PR2A.md](UValue-SetUValue-PR2A.md), [UValue-Window-PR2B.md](UValue-Window-PR2B.md).
Prerequisite PR3-0 (the SAM_Tas `.tcd` importer made linear) is merged as SAM_Tas#79 (`326770bd`).

## Status

Implemented, unit/window-tested (full WPF suite **1814 passed, 0 failed**: 1700 before + 114 new) and accepted in the
real app (UIA-driven `SAM Analytical.exe` from a copy of `SAM_UI\build`, real Tas, fresh model copy). The solution builds
(MSBuild, Release). SAM_Tas is unchanged by this PR. Owner decision applied: **Tools > Glazing Calculator opens the new
window; the legacy flow stays as "Glazing Calculator (classic)"**.

A closing pass (3D right-click route, full ModelCheck before/after, Undo/Redo, Part O style review) found **one real
defect, now fixed**: the scoped post-apply check missed the host-panel rules that Edit > ModelCheck reports (see
"Closing pass" below).

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
| Interactions (Tools route) | with Load more ~8 |

## Closing pass: 3D right-click, ModelCheck before/after, Undo/Redo (final build)

App: copy of `SAM_UI\build` at `13dff649` (`SAM.Analytical.UI.WPF.dll` md5 `e187a72c...`), fresh copy of the model, real Tas.
The 3D context menu is built from the viewport's current **selection**, not a hit test, so the aperture was selected with
right-click > Select > By Guid (the apertures are not visible from the default camera and injected drags do not orbit
it); the counted path starts from that selected aperture and uses real mouse clicks.

| Check | Result |
|---|---|
| **3D right-click route** | selected aperture, right-click in the viewport: the menu lists "Set glazing..." with the aperture items; clicking it opens the window with the aperture's system, "used by 20 apertures (1 selected)" and "Applies to 20 apertures using SIM_EXT_GLZ (1 selected)." The route works. |
| **Interactions to Apply** | (1) right-click, (2) "Set glazing...", (3) click the chosen system, (4) Apply = **4**; (5) Close = **5**. Selecting the aperture by clicking it in a real session adds one: **5 to Apply, within the 4-6 target**. No layer picking, no error recovery. Window open 0.1 s, Tas table 680 ms. |
| **Full ModelCheck BEFORE** | 5 information messages (gas recognised), **0 warnings, 0 errors** |
| Apply | chose the library `SIM_EXT_GLZ` (Ug 2.24); added as `SIM_EXT_GLZ 2` (name clash handled) + 1 material; all 20 apertures, including the selected one, now on it; U/g/LT 2.238 / 0.406 / 0.804 |
| **Full ModelCheck AFTER** | **40 warnings, 0 errors**, 5 messages: for each of the 20 apertures "ApertureConstruction ... different Default Panel Type than its SIM_EXT_SLD host panel" and "PanelType of SIM_EXT_SLD Panel does not match with assigned SIM_EXT_GLZ 2 ApertureConstruction". Cause: this library system's Default Panel Type is `Floor`, the hosts are `WallExternal`. |
| **Scoped check vs full ModelCheck** | scoped line "Check: 40 warnings for SIM_EXT_GLZ 2 and its 20 apertures."; its 40 records are **text-identical** to the 40 warnings of the full ModelCheck (0 only-in-scoped, 0 only-in-full) |
| No-regression case | applying the matching library twin (`SIM_EXT_GLZ`, same values, no new material): scoped "No errors or warnings for SIM_EXT_GLZ 2 and its 20 apertures."; full ModelCheck 0 warnings, 0 errors, 5 messages |
| **One Undo** | Undo disabled, Redo enabled after one click; 20 apertures back on `SIM_EXT_GLZ` (U/g/LT 1.243 / 0.4002 / 0.8036); **1 aperture construction, 18 materials** (the added construction and material are not left behind); full ModelCheck back to 0 warnings / 5 messages |
| **Redo** | 20 apertures on `SIM_EXT_GLZ 2`, 2 aperture constructions, 19 materials; Undo enabled, Redo disabled (one Apply = one history step) |

### Defect found and fixed in the closing pass

Applying a roof system (`SIM_EXT_GLZ_SKY`) to the wall apertures on the **pre-fix** build gave the scoped line "No errors or
warnings", while Edit > ModelCheck warned for every aperture. Cause: `GlazingCheck` ran `Create.Log(panel,
materialLibrary)` only, not the host-panel rules: `Create.Log(Panel)` (the aperture construction's Default Panel Type
against the host panel) and the panel-group rule that SAM only runs inside `Create.Log(AdjacencyCluster)`. Fix
(`Query/GlazingCheck.cs`): the scoped check also runs `Create.Log(panel)` and applies the panel-group rule, with SAM's
wording, to the changed apertures. Regression tests: `TheCheck_ReportsASystemMadeForAnotherPanelGroup_AsModelCheckDoes`
(a roof system on wall apertures: 10 warnings for 5 apertures, none before) and
`TheCheck_OfASystemForTheSamePanelGroup_HasNoHostPanelWarnings`. Re-run on the fixed build: the "Scoped check vs full
ModelCheck" and "No-regression case" rows above.

Evidence note: the Edit > ModelCheck `Log` grid is virtualised, so a plain UIA read returns only the realised rows (22 of
45 here); the counts above come from a scrolling reader, and SAM's own `Create.Log` on the saved model gives the same 40.

## Part O style review (SetGlazingWindow against the Part O / TM59 windows)

Compared: the new window's screenshots (empty, chosen, applied) with the captured Part O hub and TM59 result windows, and
`PartOStyles.xaml` plus the Part O result/preparation window XAML. The Part O screenshots predate later Part O polish.

| Element | Result |
|---|---|
| Section headings | **Consistent**: `PartO.SectionHeading` (bold) for "Glazing to replace" / "Target", as Part O's bold "Status" / "Current configuration" |
| Captions | **Consistent**: `PartO.Caption` muted grey for the facts line, "Showing 5 of 5 systems..." and the report line, as Part O's grey explanatory lines |
| Primary action | **Consistent**: Apply uses `PartO.PrimaryButton` (blue, default); Cancel/Close are plain buttons of the same 28 px height as the Part O results windows |
| Status glyphs | **Consistent** resource (`PartO.StatusGlyph`: green check, red cross/warning triangle, grey dash). Part O's hub states "READY / NEEDS PREPARATION" in words; this window pairs glyph and words |
| Table spacing / alignment | **Mostly consistent**: horizontal grid lines in the Part O border colour, numeric columns right-aligned, chosen row clearly marked. Differences: rows use the default height (~18 px) where the Part O Mixed Design grid sets 24 px, and the Pane / Frame text is truncated in the grid |
| Advanced section | **Consistent**: collapsed Expander headed "Advanced" with the same circle chevron as Part O's "Catalogue products" |
| Copy All / Details | **Partly different**: same size and grey style as Part O, but Copy All sits at the far left of the action bar (as in Set U-value) while the Part O result windows put it next to Close at the right. "Details" is a button in the result box (opens the existing LogWindow) where Part O uses "Technical details" expanders |
| Warning / error wording | **Consistent in tone** (plain sentences, counts, "One Undo reverts it."). **Different in colour**: warnings use the red Danger brush (as Set U-value) because `PartOStyles` has no amber; Part O's hub amber is a hard-coded colour |

Not changed in this PR (record only): grid row height and truncation, Copy All placement and an amber warning brush are
small follow-ups, natural for PR4 with the restyle of the remaining legacy windows.

## Found in the first real-app pass and fixed

1. The scoped check missed SAM's frame rule ("no Frame ConstructionLayers"): it now uses both `Create.Log` overloads.
2. Solid systems were offered as glazing: they no longer are.
3. Combo items now have a proper `ToString`.
4. The status glyph for a neutral state is now neutral.
5. Selected-row style fixed.
6. The report says "none" for Uf of a frameless system.
7. (Closing pass) The scoped check missed the host-panel rules of ModelCheck: see the defect section above.

## Tests (`SAM.Analytical.UI.WPF.Tests`, 114 new; full suite 1814 passed, 0 failed)

- `GlazingViewModelTests`: sources, candidate identity, filtering, target/margin/status, choosing, scope and Advanced
  options, blocked candidates and their reason, evaluator failure.
- `SetGlazingTests`: one history step, only the chosen system and missing materials added, name clash, assign scopes,
  parameters refreshed, source kept, failure leaves the model untouched.
- `GlazingSourceReaderTests` (reader, cache, pane-library note) and `GlazingReportTests` (check incl. the host-panel rules, report, save path).
- `SetGlazingWindowTests` (STA, `WpfCollection`): opened from apertures and from Tools; picker; target filters and
  chooses; row click; Tas failure shown; Apply changes the model once, shows the check and saves the report; unsaved model
  offers Copy All only; failed Apply leaves model and history alone; Advanced; selected-only disabled with no selection;
  action bar inside the smallest window.
- `UIJSAMObject.Undo` restores asynchronously, so unit tests assert one history step only; the restore itself is
  verified in the real app (table above).

## Deviations from the plan, stated plainly

1. **Legacy post-steps.** The plan says to keep `UpdateConstructions`, `UpdateApertureConstructions` and
   `Tas.Modify.UpdateThermalParameters`. `SetGlazing` calls **`UpdateApertureConstructions` only**.
   `UpdateConstructions` is not needed: panel constructions do not change when glazing is set.
2. **Aperture performance is recalculated directly.** `Tas.Modify.UpdateThermalParameters` does not apply to aperture
   constructions, so the chosen system is calculated with Tas (`CalculateGlazing`) and its U/g/LT parameters are written
   by `SetGlazing` itself. A later whole-model `UpdateThermalParameters` will not refresh them.
3. **Imported TCD glazing systems are frameless in the tested sources.** For those candidates `Uf` is blank and
   `Uw = Ug`; the table shows "no frame" and a warning says the apertures lose their frame. No frame data is invented or
   synthesised.
4. **IGDB v76 is pane-only** (11,664 panes, no glazing systems), so it provides no complete candidates and the window says
   so. Automatic pane + gap + pane composition remains explicitly **out of scope** (owner decision, a later PR).
5. The two acceptance gaps left after the first real-app pass, the 3D right-click route and the full ModelCheck before/after, are closed (see "Closing pass").

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
- **The table can offer systems made for another panel group.** Default-library systems carry a Default Panel Type (the
  second `SIM_EXT_GLZ` is `Floor`, `SIM_EXT_GLZ_SKY` a roof system); on wall apertures they apply fine but ModelCheck then
  warns for every aperture. The scoped check now says so after Apply (40 warnings in the case above); the window does not
  yet warn **before** Apply. Left for the owner to decide (a pre-apply warning or a status on the row).
- Pre-existing latent: an unknown TCD material type in a material folder throws `NullReferenceException` in the importer
  (documented in SAM_Tas#79).
- Whole-model `Tas.Modify.UpdateThermalParameters` ignores aperture constructions; `SetGlazing` calculates the chosen
  system itself, so a later whole-model update will not refresh them.
- The Edit > Aperture Constructions unsaved-edits prompt, "Tas unavailable" and the Copy All clipboard were not driven
  in the real app. Aperture selection in the 3D acceptance was set up with Select > By Guid, not by clicking the aperture.
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
