# Thermal Stage C - editing in the Thermal Performance panel (2 Oct 2026)

Plan: `documentation/plans/Thermal-Performance-Review.md` (Stage C). Builds on the merged read-only panel (#167) and the hybrid host (#168).
The panel is now the editing surface: **select wall -> see U -> type target -> preview -> Apply** and **select window -> see
Uw/g/LT -> Change... -> choose a complete system -> preview -> Apply**, with no separate calculator window on the normal path.
The existing PR1-PR3 engines do all the calculation; nothing was duplicated.

## What was built

| Part | Where | What |
|---|---|---|
| C1 `ThermalChangeSet` | `Classes/Thermal/ThermalChangeSet.cs`, `Modify/ApplyThermalChange.cs` | A set of opaque (`SetUValueRequest`) and glazing (`SetGlazingRequest`) changes, each with its own pinned scope. `Modify.ProposeThermalChange` chains the existing `SetUValue` / `SetGlazing` model cores on clones; `ApplyThermalChange` adds the whole-model refresh **once** (only when an opaque change or Recalculate is in the set) and calls `SetJSAMObject(..., FullModification)` **exactly once**: one Apply = one Undo. A failing, conflicting (two changes to one construction) or empty set commits nothing. `ApplyThermalChangeWithReports` adds the existing per-Apply reports (reports stay per Apply). |
| C5 check before Apply | `Query/ThermalCheckDiff.cs` | Runs the existing scoped `UValueCheckSummary` / `GlazingCheckSummary` on the proposed clone and on the current model (the construction being replaced, same elements) and reports only records the proposal adds, comparing text with the new construction's name/Guid read as the old one. No Tas, no write. |
| C2/C3 row editors | `Classes/Thermal/ThermalRowEditor.cs`, `ThermalEditSession.cs` | WPF-free wrappers over `UValueViewModel` / `GlazingViewModel` per row: target U + live preview + scope + Keep name + overrides (layer, range, heat flow), `Change...` list of complete systems (Uw/Ug/Uf/g/LT/Source + the A-lite candidate warnings). View-models are created on the first edit, so merely looking calls no Tas. |
| C4 scope | same | Scope is the shared `ThermalScope`, inline ("All N using it" / "Only the M selected"), **pinned** from the row's elements when the edit starts. While a row is edited the panel does not follow the selection or switch mode. An outside model change (Undo, other editor; `IsModelChange` from the Modified event) discards the pending edit with a notice. The panel's own Apply is recognised and discards nothing. |
| C6 | `ThermalPerformanceControl` | Bar: "Before apply: ✓ No new warnings / ⚠ N new warnings [Details]", "N changes · M elements", Discard, Apply, "One Undo reverts it"; after Apply a one-line result + report path. Provenance: each row says "Stored on the model" / "Not calculated" / "The elements store different values" / "Stored U x, calculated now y: out of date", with **Recalculate** (the whole-model refresh as one Undo-able change set). |
| Host | `AnalyticalWindow.ThermalPerformance.cs` | Supplies the `Applier`, passes `modelChanged`, disposes the Tas workers on close. |

## Tests

New: `ThermalChangeSetTests` (13), `ThermalEditSessionTests` (17), `ThermalPerformanceEditingControlTests` (5, real XAML over fake calculations).
Covered: one opaque change; one glazing change; wall + window in one Apply; two walls; two changes to one construction refused; a failing
change commits nothing (not even the earlier ones); empty set; Recalculate alone; exactly one `Modified` and one history step per Apply;
refresh once; pinned scope against later selection; proposal invalidated by an outside change; Discard leaves model and history untouched;
pre-Apply warning diff (none / "made for roofs"); check changes neither model; library system only enters the model on Apply; missing /
varying / stale stored values; whole envelope. Full `SAM.Analytical.UI.WPF.Tests`: **1911/1911** (1876 on the hybrid-host branch + 35).

## Real-app acceptance (UI Automation, real Tas, fresh copy of the model each run; evidence local `C:\TasOut\uvalue\stagec`)

Interaction = one user action (a click, a typed value, a selection). Selecting by Guid stands in for a click in the 3D view, which an
injected click cannot pick.

| Journey | Interactions | Result |
|---|---|---|
| **Opaque**: select wall (1 of 12 panels) -> panel shows `SIM_EXT_SLD U 0.260 · 12 use it (1 selected) · Stored on the model` -> click target (1) -> type 0.18 (1) -> Apply (1) | **4**, + 1 Undo | Preview in ~3 s: `U 0.260 → 0.180 W/m²K · wool 80 → 123 mm`, `Before apply: ✓ No new warnings`, `1 change · 12 elements`, scope "Applies to 12 panels using SIM_EXT_SLD (1 selected)". Apply 3.5 s (whole-model Tas refresh): `12 panels now SIM_EXT_SLD U0.18. One Undo reverts it.` + report. One Undo: `SIM_EXT_SLD` U 0.260 again, **floor U 0.161 -> 0.145** (the stale-value shift of the B finding is undone), material `..._0.123m` and construction `SIM_EXT_SLD U0.18` gone from the saved file. |
| **Glazing**: select window -> `SIM_EXT_GLZ U 1.243 · g 0.40 · LT 0.80` -> Change... (1) -> choose `SIM_EXT_GLZ_SKY` (1) -> scope "Only the 1 selected" (1) -> Apply (1) | **4** (5 with the scope click), + 1 Undo | List of 5 complete systems in 1.3 s with Uw/Ug/Uf/g/LT/Source and the A-lite row warnings (`⚠ made for roofs`, `⚠ made for floors`). Preview `Uw 1.35 → 1.75 — pane build-up changes, same frame`, `Adds SIM_EXT_GLZ_SKY and 1 material to the model`; check `⚠ 40 new warnings` for all 20 windows, `⚠ 2` for the 1 selected; scope text updates. Apply 3.3 s: `1 aperture now SIM_EXT_GLZ_SKY` (saved file: 19 + 1 apertures). One Undo: 20 x `SIM_EXT_GLZ`, no `SIM_EXT_GLZ_SKY` anywhere in the saved file. |
| **Mixed** (Whole envelope: no selection needed): Whole envelope (1) -> wall target (2) -> Change... (1) -> choose system (1) -> Apply (1) | **6** for two changes (8 as two separate journeys), + 1 Undo | `2 changes · 32 elements`, `⚠ 40 new warnings [Details]` (Details opens the log listing each new warning), Apply 3.8 s: `12 panels now SIM_EXT_SLD U0.18 · 20 apertures now SIM_EXT_GLZ_SKY. One Undo reverts it.` + two reports. **One Undo restored both** (saved file identical to the other runs' undone state, 900 824 bytes, SHA `EA79562E984F`). |
| **Invalidate** | - | Typed a target, then Redo from the ribbon: `The model changed outside the panel (for example an Undo), so the pending change was discarded.`; the panel shows the redone model. |
| **Discard** | - | Typed a target, Discard: model unchanged, ribbon Redo still enabled (history untouched). |

## Findings

1. **A collapsed `Expander` in the row template hides every later group from UI Automation** (screen readers, drivers): only the
   first group exposed children, although the others rendered and clicked fine. Bisected in the real app (editor block -> opaque block
   -> Expander); replaced by a "⋯ More" toggle button. `ItemsControl` also defaulted to Tab navigation "Once"; the lists now use
   "Continue". Not unit-testable here (an in-process UIA client deadlocks in the test host), so the acceptance driver is the guard.
2. **The preview is built synchronously on the UI thread** (clone + two `SetUValue`/`SetGlazing` cores + the checks) at every settled
   edit. Instant on the 50-element test model; not measured on a large model.
3. The proposal is built **without** the whole-model refresh and with the Ug/g/LT of the request for glazing, so the checks see the
   structure the Apply will have, not Tas' refreshed numbers.
4. `Whole envelope` rows reach every element using the construction (the scope sentence says how many), including internal ones that
   share it; the row's own count is the envelope elements only.
5. The scoped glazing check counts one warning per rule per aperture: "40 new warnings" for 20 windows is 2 per window (host panel
   type and panel group), the same two ModelCheck would report.

## Gaps / not done (by design or deferred)

- No "Add source..." in the panel: the candidate pool is the model plus the default library. Loading a file source, remembered sources
  and pane-only file flags are Stage D. (The classic windows still offer "Load more...".)
- Opaque alternative constructions that meet the target (Stage D). IGDB pane + gap + pane composition (separate scope).
- The classic calculators, the 3D right-click items and the Tools split buttons are **unchanged and still reachable**; they were not
  moved under a "Legacy" heading (Stage E retires them after parity tests).
- Reports remain one file per opaque change and per glazing change, per Apply (a combined change log is Stage E).
- The panel cannot switch Selection / Whole envelope while a row is being edited (apply or discard first), by design.
- Real second monitor / per-monitor DPI remain untested (see `Thermal-StageB-Hosts.md`).
