# Thermal Stage F - Final Convergence (2 Oct 2026)

Base: `sow/2026-Q3` @ `33e148b9` (Stage E complete: E0-1 #175, E0-2 #176, E0-3 #177, E1 #178; WPF suite 2196/2196). Input: the E1 parity matrix
(`documentation/Thermal-StageE1.md`), treated as authoritative. Rule: **move useful candidate-selection capability into Thermal Performance; keep specialist
authoring; retire / redirect a duplicate assignment entry point only when its user journey is genuinely covered.** Stage F closes by a clear end state, not by
maximum deletion.

Two PRs along the real dependency boundary:

* **F1 - parity additions** (this document's sections 3 and 7.1): the candidate-selection abilities only the classic Set glazing window had, inside the panel's
  glazing `Change…` list. No entry point changed.
* **F2 - redirects + closeout** (sections 4 and 7.2): only after F1 passed real-app acceptance.

## 1. Final user journey

**Primary - change what an element is built of:**

`select element(s)` → **Thermal Performance** (ribbon toggle, or right-click `Set U-value...` / `Set glazing...`) → the row of its construction →
opaque: target U → preview + alternatives (generated / existing model / library / added sources) · glazing: `Change…` → complete systems, filtered (target Uw,
g, light, sources), sorted, compared; or `Create new…` → Builder → `Save as predefined` → **scope** (all using it / only the selected, pinned) → **check before
Apply** → **Apply** (one `SetJSAMObject`) → **one Undo**; change report written; colour-by refreshes.

**Specialist - author or manage constructions, not assign them to selected elements:** the construction-level Set windows (Tools ribbon), the classic Tas
calculators and *Assign ... By UValue / By gValue*, the Constructions / Aperture Constructions / Materials editors (Edit ribbon). Section 5 says why each stays.

## 2. Capability ownership matrix (the final parity decision)

Classification: **Move** into Thermal Performance · **Covered** already · **Retain** as specialist · **Redirect** (duplicate entry point retired).

| Capability (from E1) | Decision | Owner after Stage F | Evidence / reason |
|---|---|---|---|
| Set glazing: g filter (from / to), light-transmittance filter | **Move (F1)** | Panel `Change…` > Filters | Same `GlazingViewModel.MinGText / MaxGText / MinLightText` the window used; tests + acceptance §7.1 |
| Set glazing: comparison table (Current / Proposed / Target / Margin / Status, sortable columns) | **Move (F1)** | Panel `Change…` | Each candidate line already carries Uw · Ug · Uf · g · LT · source · meets / above target; F1 adds **Sort by** (Uw / g low / g high / light / name) and the line `Target Uw ≤ x: ✓ Meets target (margin +y)`; the preview gives Ug / g / light / Uw before → after + pane / frame change |
| Set glazing: include default library / loaded files | **Move (F1)** | Panel `Change…` > Filters > Show | The same `IncludeLibrary / IncludeLoaded`; the sources themselves stay the panel's one `ThermalSourceCatalog` (Add source… / Forget) - no second source framework |
| Set glazing: Load more glazing… | **Covered** | Panel `Add source…` (D2) | Remembered, shared with opaque rows and the Builder |
| Set glazing / Set U-value: check before Apply, change report | **Covered** | Panel | Same check; same `Save*ChangeReport` writers (`ApplyThermalChange.WriteReports`) |
| Set U-value: target U, preview, scope, Keep name, layer / range / heat-flow overrides | **Covered** | Panel row (Stage C/D) | Stage C/D acceptance; E1 smoke A |
| Set U-value: full before / after layer table | **Covered** | Panel row | Only one layer changes: the preview names it with before → after thickness; "Layer to adjust" lists every layer with its thickness |
| "Don't assign" (add to the model, assign to nothing) - glazing | **Covered for the element journey** by `Save as predefined`; **Retain** the model-only form | Panel `Create new…` → Save (keeps a system for later, no model change); Set glazing window (Tools) | Keeping a system for later is library authoring, not model mutation; the literal "add unassigned to this model" stays in the construction-level window |
| "Don't assign" - opaque | **Retain** | Set U-value window (Tools ribbon, Edit > Constructions hand-over) | The panel has no user construction library; not invented here |
| Set U-value / Set glazing for a construction with **no selected element** | **Retain** | Set windows (Tools ribbon; Edit > Constructions / Aperture Constructions hand-over) | No fake element context is invented in the panel |
| Generate a construction by criteria through Tas (classic U / glazing calculators, *Assign ... By UValue / By gValue*, g-value target) | **Retain** | Tools ribbon `(classic)` arrows; 3D right-click | Specialist Tas generation; not duplicated |
| Assign Construction / Assign Aperture Construction (pick any existing one, no calculation) | **Retain** | 3D right-click | Generic assignment without thermal semantics (opaque has no "any existing" list without a target) |
| Door / opaque-aperture authoring, frame-layer authoring, `.json` / `.tcd` import-export | **Retain** | Edit > Constructions / Aperture Constructions | Specialist library management |
| Material editing | **Retain** | Edit > Materials | By design |
| **Entry point** 3D right-click `Set U-value...` / `Set glazing...` (always has selected elements) | **Redirect (F2)** | Thermal Performance | After F1 nothing in that journey is unique to the window (section 4) |
| **Entry point** Tools ribbon `U Value Calculator` / `Glazing Calculator` main buttons | **Retain** | Set windows (construction level) | They open with no selection: the no-selection route and "don't assign" live there |

## 3. What moved (F1)

In the glazing `Change…` list of the panel, behind a **collapsed `⋯ Filters` toggle** next to `Target Uw ≤` (the persistent panel is not overloaded):

* **g from / to**, **Light at least** - the shared view-model's filters; a filtered list always says so under the count
  (`Filtered: g 0.30 – 0.45 · light ≥ 0.60 · without the default library`), whether the toggle is open or not.
* **Sort by** - `Uw, best first` (default, unchanged), `g, lowest first`, `g, highest first`, `Light, highest first`, `Name`. New `GlazingSortOrder` on
  `GlazingViewModel.SortOrder`; equal values fall back to the best Uw; a value not calculated is listed last. **The automatic choice against a target is
  always the best Uw that meets it, whatever order is shown.**
* **Show: Default library / Added sources** - the shared `IncludeLibrary / IncludeLoaded`; the model's systems and My glazing systems are always listed.
* **Comparison line** under the preview, e.g. `Target Uw ≤ 1.30: ✓ Meets target (margin +1.06)`.
* The filters belong to the open list: closing it (Cancel, Apply, Discard) drops them with the view-model; a new `Change…` starts unfiltered.
* A choice hidden by a filter is dropped (nothing to Apply) - the existing view-model rule, now reachable from the panel.

Files: `Enums/GlazingSortOrder.cs` (new), `Classes/Glazing/GlazingViewModel.cs` (`SortOrder`, order-aware compare, best-Uw automatic choice),
`Classes/Thermal/ThermalRowEditor.cs` (pass-throughs, `GlazingFiltersText`, `GlazingComparisonText`), `Controls/ThermalPerformanceControl.xaml` (Filters
block + AutomationIds `toggleButton_GlazingFilters`, `textBox_GlazingTarget / MinG / MaxG / MinLight`, `comboBox_GlazingSort`,
`checkBox_GlazingIncludeLibrary / IncludeAdded`, `textBlock_CandidateCount`, `textBlock_GlazingFilters`, `textBlock_GlazingComparison`).

Found and fixed on the way: two mis-encoded literals in the Apply bar (`âœ•` / `calculatingâ€¦` were shown instead of `✕` / `calculating…`); a race in the
E0-3 `GlazingPaneBrowserTests` helper (without a UI dispatcher a pane source is "ready" a moment before its list is filtered - the helper now also waits
for the projection task; product behaviour unchanged).

Seen, not fixed (pre-existing, outside Stage F): several window systems of the remembered NCM `.tcd` source (e.g. "4-12-4-12-4 triple glazing, low-e", frameless)
are listed with Ug / Uw 0.24 W/m²K, which is not plausible for those build-ups; the value is what the existing Tas evaluation of that source returns (D2 / E0-2
route), not something the filters change. Worth a separate look at how gaps of loaded NCM systems reach Tas.

Unchanged: no mutation before Apply; one Apply → one `SetJSAMObject` → one Undo; Builder Save ≠ model mutation; scope pinning; colour-by; reports; no
SAM / SAM_Tas change; no classic window changed.

## 4. What was retired / redirected (F2)

*Filled in by F2.*

## 5. What remains intentionally specialist, and why

| Tool | Reached from | Why it stays |
|---|---|---|
| **Set U-value window** (construction level) | Tools > `U Value Calculator`; Edit > Constructions > `Set U-value...` | Changes a construction with no element selected, including one no element uses; "don't assign" (create the variant only). |
| **Set glazing window** (construction level) | Tools > `Glazing Calculator`; Edit > Aperture Constructions > `Set glazing…` | The same for aperture constructions; adds a system to the model unassigned. |
| **Classic U Value / Glazing Calculators** | Tools > the `(classic)` arrows | Generate a construction from criteria through Tas. |
| **Assign Construction By UValue / Aperture Construction By gValue** | 3D right-click | Criteria generation (incl. a g-value target) for the selection. |
| **Assign Construction / Assign Aperture Construction** | 3D right-click | Any existing construction, no calculation. |
| **Constructions / Aperture Constructions libraries** | Edit ribbon | Library management: doors, opaque apertures, frame layers, copy / duplicate, `.json` / `.tcd` import-export. |
| **Materials** | Edit ribbon | Manual material editing. |

## 6. Final Thermal Performance architecture

Unchanged from Stage E, extended only inside the glazing `Change…` list: `ThermalPerformanceControl` (docked or floating, one control) → `ThermalPerformanceViewModel`
(rows by construction for the selection / the envelope) → `ThermalEditSession` (pending edits, pinned scope, check, Apply, Discard) → per row `ThermalRowEditor`
over the shared view-models (`UValueViewModel` + `ConstructionAlternatives` for opaque; `GlazingViewModel` for glazing, now with its filters, order and source
toggles exposed) → `ThermalChangeSet` → `Modify.ApplyThermalChange` (one model clone, one `SetJSAMObject`, one Undo, the same report writers). Candidates:
model → default library → My glazing systems → added sources (`ThermalSourceCatalog`), first source of a Guid wins. Builder: modal, model-free, saves only
My glazing systems.

## 7. Evidence

### 7.1 F1

**Tests** (`ThermalCandidateFilterTests`, 15): sort orders reorder only (count unchanged, back to the default); the automatic choice against a target is the
best usable Uw in every order (theory ×4); a choice hidden by a filter is dropped and nothing is applied; in the real panel XAML over the fakes - filters
folded away by default and shown by the toggle; g / light filters narrow the list, the filter line and the count say so, the target chooses the best system,
the comparison line, model untouched before Apply, Apply = one `Modified` / one history entry, one Undo restores the model; sorting reorders the realised
list only; source toggles hide the default library / the added source (and drop a hidden choice: Apply disabled); filters start empty when the list is
opened again; the filter line names only filters in force (theory ×4). Focused 15/15; **full WPF suite 2211/2211** (base 2196 + 15; repeated 9 times after
the pane-browser race fix: 8 green, one run with a single failure that was not captured and did not recur in the next 7 runs).

**Real-app acceptance** (Release build of the F1 head, the representative model - 20 windows `SIM_EXT_GLZ` U 1.243 - real Tas, the remembered NCM source;
driven through UI Automation; evidence and driver kept locally, not committed):

1. Baseline model JSON hash `EA79562E984F`. One window → `Change…`: list ready 6.7 s; `⋯ Filters` visible, filter boxes hidden, `Target Uw` visible.
2. Filters open: g 0.30 – 0.45, light ≥ 0.60 → **"Showing 9 of 109 systems."**, line "Filtered: g 0.30 – 0.45 · light ≥ 0.60".
3. Sort `g, highest first` → top rows g 0.45; `Light, highest first` → `SIM_EXT_GLZ` (LT 0.80) first; count unchanged.
4. Show: without added sources → "Showing 3 of 5"; also without the default library → "Showing 1 of 1" (the current system); both back → 9 of 109.
5. Target Uw ≤ 1.30 → best Uw meeting it chosen automatically; "Target Uw ≤ 1.30: ✓ Meets target (margin +1.06)"; preview "Ug 1.24 → 0.24 · g 0.40 → 0.45 ·
   light 0.80 → 0.63 · Uw 1.35 → 0.24 — pane build-up changes, no frame."; only the 1 selected; "Before apply: ⚠ 1 new warning"; "1 change · 1 element";
   **Undo disabled; saved model hash = baseline.**
6. Apply (3.4 s): "1 aperture now 4-12-4-12-4 triple glazing, low-e. One Undo reverts it." + GLAZING CHANGE report saved; Undo enabled; colour by g:
   legend 0.4 | 0.446. **One Undo**: Undo disabled; legend back to 0.4; **saved model hash = baseline `EA79562E984F`**.

### 7.2 F2

*Filled in by F2.*
