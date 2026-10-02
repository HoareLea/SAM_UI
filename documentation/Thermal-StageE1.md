# Thermal Stage E1 - consolidation / legacy parity review (2 Oct 2026)

Base: `sow/2026-Q3` @ `a622d8fc` (Stage E0 complete: E0-1 #175, E0-2 #176, E0-3 #177 merged). Rule applied: **retire only capabilities proven redundant; do not remove a classic tool
merely because one of its workflows moved to Thermal Performance; if a decision is ambiguous, keep and document.**

## Outcome

**Nothing was proven redundant, so no entry point or editor was removed or redirected.** No product code changed in E1. What E1 adds: this parity matrix with the evidence behind
every decision, the criteria a later retirement would have to meet, a focused real-app smoke on the post-E0 base, and `ThermalConsolidationParityTests` (a guard so the classic
entry points and the capabilities the panel lacks cannot disappear by accident - removing one must update this record and those tests in the same commit).

## What exists after Stage E0 (inspected in code, not assumed)

| # | Surface | Where | What it does |
|---|---|---|---|
| 1 | **Thermal Performance panel** (docked / floating) | `Controls/ThermalPerformanceControl`, `Classes/Thermal/*` | Rows by construction for the selection or whole envelope; opaque: target U, live preview, alternatives (Generated / Existing model / Library / Add source…), scope, Keep name, layer / range / heat-flow overrides; glazing: `Change…` list of complete systems (model, default library, My glazing systems, added sources), target Uw filter, scope; **`Create new…` Builder** (E0-3); check before Apply; ONE Apply / ONE Undo; colour by; stored-value provenance + Recalculate |
| 2 | **Set U-value window** | Ribbon Tools > `U Value Calculator` (main button), 3D right-click on panels `Set U-value...`, Edit > Constructions hand-over | One-window target-U flow for a chosen construction |
| 3 | **Set glazing window** | Ribbon Tools > `Glazing Calculator` (main button), 3D right-click on apertures `Set glazing...`, Edit > Aperture Constructions `Set glazing…` hand-over | One-window comparison table of complete systems with Uw / g / light filters |
| 4 | **Classic U Value Calculator** | Ribbon Tools > `U Value Calculator` arrow | Select a construction → calculation-data window → Tas calculation → result → create / replace construction (also apertures) |
| 5 | **Classic Glazing Calculator** | Ribbon Tools > `Glazing Calculator` arrow (`Modify.CalculateGlazing`) | Glazing calculation data (criteria) → Tas → result window → add / replace construction |
| 6 | **Assign … By UValue / By gValue** | 3D right-click (panels / apertures) | Tas calculation by criteria → result window → assign |
| 7 | **Assign Construction / Assign Aperture Construction** | 3D right-click | Pick any existing construction and assign it (no calculation) |
| 8 | **Constructions / Aperture Constructions libraries** | Edit > Constructions, Edit > Aperture Constructions | Edit the model's construction / aperture-construction libraries (copy, add, duplicate, edit layers, import / export `.json` / `.tcd`) - includes **doors and opaque apertures** and **frame layers** |
| 9 | **Materials library** | Edit > Materials | Manual material editing |
| 10 | Reports | `Modify.SaveUValueChangeReport` / `SaveGlazingChangeReport` / `SaveConstructionChangeReport` | ONE writer per report type, called by the Set windows AND by the panel's Apply (`ApplyThermalChange.WriteReports`) |

## Parity matrix

| Capability | Panel + Builder covers it? | Evidence | Decision |
|---|---|---|---|
| Change one opaque construction to a target U, scoped to all / selected | **Yes** (rows, preview, scope, Keep name, overrides, alternatives) | Stage C / D acceptance, E1 smoke A | Panel is the primary route; **Set U-value window kept** (see below) |
| Set U-value for a construction **with no selected elements / not in the current rows** | No - rows exist only for the selection or the envelope in use | `ThermalPerformanceViewModel` builds rows from elements | Keep Set U-value |
| "Don't assign" (add the result to the model only) | No - the panel offers all-using / only-selected | `ThermalApplyScope.DontAssign` exists in the VM; the control has two radios; `SetUValueWindow` / `SetGlazingWindow` expose `checkBox_DontAssign` | Keep both Set windows |
| Glazing: filter by **g (from / to) and light transmittance**, sort / compare full table with Check column | No - the panel exposes the target Uw only | `GlazingViewModel.MinGText / MaxGText / MinLightText`; `SetGlazingWindow` has the boxes and the grid | Keep Set glazing |
| Glazing: include / exclude default library and loaded files | Partly (sources are listed; no per-source exclusion) | `IncludeLibrary` / `IncludeLoaded` | Keep Set glazing |
| Pick an existing complete glazing system for the selected windows | **Yes** (`Change…`) | E0-2 / E0-3 / E1 smoke B, C | Panel primary |
| **Author** a new glazing system from panes and gaps | **Yes - Builder** (E0-3); classic editor authors layers by material only | Stage E0 plan §14 | Builder is the primary route; classic aperture editor kept (frames, doors) |
| **Generate** a construction by criteria through Tas (classic U / glazing calculators, *By UValue* / *By gValue*) | **No** - the panel selects among existing / generated-by-thickness variants; it does not run the criteria calculators or target a g-value | `Modify.CalculateGlazing`, `ThermalTransmittanceCalculator_SingleConstruction`, `Assign*ByThermalTransmittance` | **Keep** |
| Door / opaque-aperture authoring, frame-layer authoring, `.tcd` / `.json` library import-export | No (Builder: windows only, frame = copy / none) | Edit > Aperture Constructions; Plan §19 decisions | **Keep** |
| Manual material editing | No (by design) | Edit > Materials | **Keep** |
| Change reports | Same writers | `ApplyThermalChange.WriteReports` calls the same `Save*ChangeReport` the Set windows use | Nothing to merge |
| Entry points that duplicate the panel's *workflow* (right-click `Set U-value...` / `Set glazing...`, the two ribbon main buttons) | They open the Set windows, which keep unique capabilities (above) | Code + this table | **Keep**; ambiguous to redirect - redirecting would silently drop g / light filters, "don't assign" and the no-selection route |

## Why no redirect either

A redirect of `Set U-value...` / `Set glazing...` (or of the two ribbon buttons) to the panel would be the obvious consolidation, but each Set window has at least one capability the panel
does not (table above). Redirecting them is therefore a *product* decision, not a mechanical cleanup, and stays with the owner.

## What would have to be true before a retirement (criteria for a later stage)

1. Panel: g / light-transmittance filters for glazing candidates; a "don't assign" choice; a way to edit a construction that has no selected element (e.g. a search / "from the model" row list).
2. Panel / Builder: a g-value or U-value target route that replaces the classic *By gValue* / *By UValue* calculators, or an explicit owner decision to drop them.
3. Door and frame authoring have another home (or are explicitly out of scope).
4. Tests that exercise each moved capability through the panel, and a changed `ThermalConsolidationParityTests`.

## Real-app smoke on the post-E0 base (real Tas; model = the representative model; evidence local, not committed)

Release build of the merged tree, driven through UI Automation; the saved model JSON hash was compared with the baseline (`EA79562E984F`) after each Undo.

| Check | Result |
|---|---|
| **A** Thermal Performance opaque edit: wall → target 0.18 → preview "U 0.260 → 0.180 · Mineral Wool 80 → 123 mm" → check "✓ No new warnings" → Apply (4.1 s, "12 panels now SIM_EXT_SLD U0.18", report saved) → one Undo | Undo disabled again; **model hash = baseline** |
| **B** Glazing, **existing candidate** (default-library "Rooflight, dome, triple skin", only the 1 selected window; check "⚠ 1 new warning", shown before Apply) → Apply (3.3 s) → one Undo | **model hash = baseline** |
| **C** Glazing, **Builder-created candidate** (E0 Triple from My glazing systems, only the 1 selected) → preview "Ug 1.24 → 0.95 · g 0.40 → 0.49" → Apply (3.3 s) → one Undo | **model hash = baseline** |
| **D** Retained classic editors open and close without touching the model: `Glazing Calculator` main button → "Set glazing"; `U Value Calculator` → "Set U-value"; Edit > Aperture Constructions; Edit > Constructions | all four opened and closed; Undo stays disabled; **model hash = baseline** |

Driver notes: step B was run on its own as well (a driver quirk, not a product defect: right after an Undo the persisted selection makes the first *Select By Guid* a no-op, so a sequence
A → B needs a second select); the classic split-button arrows (4, 5) were not driven (they open Tas COM dialogs) - their handlers and implementations are asserted present by the parity tests.

## Tests

`ThermalConsolidationParityTests` (new, 4 test methods / 16 cases): the ribbon entry points, the context-menu / classic handlers, the classic implementations and windows, and the capabilities the
panel lacks (g / light filters, don't-assign, load-more) are all still present. Full WPF suite **2194/2194** (E0-3 base 2178 + 16).
