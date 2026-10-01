# Thermal Stage B - the read-only Thermal Performance panel (1 Oct 2026)

Plan: `documentation/plans/Thermal-StageB-PLAN.md`; docking result: `documentation/plans/Thermal-B0-DockingSpike.md` (DOCKED).
This stage is **read-only**: the panel inspects, it never writes the model, calls Tas or opens an editor. No `ThermalChangeSet`,
no Apply, no replacement of the PR2 / PR3 windows or of any context-menu item.

## What it does

A docked panel (View > Panels > Thermal Performance) beside the view tabs, in two Grid columns behind a `GridSplitter`:

* **Selection** mode: the selected panels and apertures under *Walls / Roofs / Floors / Other panels / Windows / Doors*, one card
  per construction: type, construction name, **stored** performance, "N use it (M selected)". It follows the active view's
  selection (3D and 2D), the tab switch and every model change.
* **Whole envelope** mode: every construction of the external envelope (external panels, not shades or solar panels, and the
  apertures they carry) with element count and gross area.
* **Stored performance** is read from where Tas stores it: panels: `PanelParameter.ThermalTransmittance` ("U 0.260"; written
  per panel by `Tas.Modify.UpdateThermalParameters`, **not on the construction**); apertures: `ThermalTransmittance`,
  `TotalSolarEnergyTransmittance`, `LightTransmittance` ("U 1.243 · g 0.40 · LT 0.80", g / LT left out for doors). "not
  calculated" when the model has none, "varies" when the elements of one row store different values or only some store one. The
  aperture U is the pane value Tas calculates (PR3: 1.243 against Uw 1.35); the panel does not show Uw (Stage C).
* **Click a card** to highlight its elements in the view: Selection mode selects every element using the construction
  ("select all N"), Whole envelope selects the envelope elements of the row. It is the normal viewport selection, so the panel
  then shows "N of N selected".
* Hidden = column width 0; the tabs span the full width as before. The panel width is remembered while the window lives
  (persisting it across sessions needs a settings home and is not done).
* B0 follow-up done: the tab area now has one star column (the `461*` / `86*` pair is gone).

## Files

New: `Controls/ThermalPerformanceControl.xaml(.cs)`, `Classes/Thermal/ThermalPerformanceViewModel.cs`, `ThermalPerformanceRow.cs`,
`ThermalPerformanceGroup.cs`, `Enums/ThermalPerformanceMode.cs`, `Query/ThermalPerformanceGroups.cs`,
`Windows/AnalyticalWindow.ThermalPerformance.cs`. Changed: `AnalyticalWindow.xaml(.cs)` (columns, toggle, three one-line
refresh hooks + the first 3D view now also raises the selection event). The B0 spike files are renamed into these.

## Tests

`ThermalPerformanceTests` (13): grouping and heading order, counts, stored U read from the panels, "not calculated" / "varies",
apertures (U / g / LT, doors), other objects / duplicates / missing model ignored, Whole envelope (external only, not
partitions, counts, areas, independent of the selection), highlight objects in both modes, summaries, mode / model changes, and
that no query writes the model (JSON unchanged). Full `SAM.Analytical.UI.WPF.Tests`: **1827/1827** (1814 + 13; this branch
does not contain the A-lite PR, whose 40 tests are separate).

## Real-app acceptance (UI Automation, real model, evidence local `C:\TasOut\uvalue\alite\run3`)

| Check | Result |
|---|---|
| Hidden | tab area x 253-1920 (full width), as before |
| Toggle on | panel 320 px, tabs x 253-1597; Selection mode: "Select panels or apertures in the view." |
| Whole envelope | "External envelope · 4 constructions · 50 elements": Walls `SIM_EXT_SLD` U 0.260, 12 elements, 616.0 m²; Roofs `SIM_EXT_SLD_Roof` U 0.164, 9, 886.0 m²; Floors `SIM_EXT_GRD_FLR FLR01` U 0.145, 9, 886.0 m²; Windows `SIM_EXT_GLZ` U 1.243 · g 0.40 · LT 0.80, 20, 64.8 m² |
| Click a row (Roofs) | the 9 roof panels are selected and drawn highlighted in 3D; back in Selection mode the panel says "9 elements selected · 1 construction / 9 use it (9 selected)" |
| A model change from elsewhere | Set U-value 0.18 on `SIM_EXT_SLD`: the Walls card becomes `SIM_EXT_SLD U0.18`, **U 0.180**; one Undo: back to `SIM_EXT_SLD`, U 0.260 |
| Splitter / hide / show (B0) | drag 320 → 618 px, width restored on show (B0 run) |

## Findings

1. **Stored opaque U is per panel, not per construction.** The first version read the construction parameter and showed "U not
   calculated" even right after an Apply; reading `PanelParameter.ThermalTransmittance` fixed it (unit-tested).
2. **Stored values can be stale or shift.** After the Set U-value Apply the whole-model `UpdateThermalParameters` recalculated
   the *floor* constructions too: Floors U 0.145 → 0.161, then back to 0.145 after Undo. The panel reports what the model stores,
   so "Recalculate" (Stage C) and a visible source / date for a stored value matter (review risk 3).
3. **Aperture selection by Guid** in the 3D view did not select an aperture that is not visible from the default camera (B0
   run); the highlight of aperture rows (Windows) is implemented and unit-tested but its real-app effect was **not** exercised
   here. Decide in Stage C whether aperture rows highlight through their host panels.
4. Keyboard focus order into the panel, a minimum viewport width, and the cost of the refresh on a very large model were not
   exercised.

## Next step

Stage C: `ThermalChangeSet` and editing in the panel (target cell, scope, check before Apply), reusing `ThermalScope` from the
A-lite PR (SAM_UI#166) - so merge #166 first, then branch Stage C from the merged base together with this panel.
