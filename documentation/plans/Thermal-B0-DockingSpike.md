# Thermal Performance - B0 docking feasibility spike (1 Oct 2026)

> Historical note (5 Oct 2026): the spike files listed below were replaced by Stage B (`ea39b810`, #167) and no longer
> exist; the hybrid docked/floating host followed in #168. This document is retained only as the B0 decision record.

Question (from `Thermal-Performance-Review.md`, Stage B0): can `AnalyticalWindow` host the future Thermal Performance surface
as a persistent side panel next to the view tabs, with the existing WPF shell, without disturbing the viewport, the tree or
the ribbon?

## Recommendation: **DOCKED**

No docking framework and no shell restructuring are needed. The main layout of `AnalyticalWindow` is already a plain `Grid`
(model tree | `GridSplitter` | view `TabControl`), so a panel is two more columns and a second `GridSplitter`. The fallback
(the same control in a modeless tool window) is not needed; the control is host-agnostic (`Update(model, selection)`), so it
stays possible.

## What the spike proves (real app, UI Automation, real model)

| Requirement | Result |
|---|---|
| Side panel beside the 3D / view area | Two Grid columns added after the tab area (`ColumnDefinition_ThermalPanel`, `GridSplitter_ThermalPanel`, `ThermalPerformanceControl`); at 1920 px the tabs keep x 253-1597 and the panel x 1600-1920 (320 px). The 3D viewport re-fits to the narrower area and keeps rendering (screenshots `b0-1-shown`, `b0-5-reshown`) |
| Show / hide toggle | `View > Panels > Thermal Performance` (a `RibbonToggleButton`); hidden = column width 0 and both elements collapsed, so the layout is exactly the existing one (tabs span the full width) |
| Resizable splitter | A mouse drag of the splitter 300 px left widened the panel 320 → 618 px and the viewport followed |
| Width remembered | Hide, then show: 618 px again (kept in the window for the session; **persisting across sessions is not done**, it needs a settings home) |
| Follows the selection | Select by Guid on a wall: "1 element selected · 1 construction / Panel (WallExternal) / SIM_EXT_SLD / 12 use it (1 selected)". Selection changes arrive through the existing `ViewportControl.ObjectSelectionChanged` (3D and 2D views); the tab switch and every `UIAnalyticalModel.Modified` also refresh it |
| Displays type, construction, count | As above, grouped by construction (unit-tested for panels, apertures, several constructions, duplicates, other objects) |
| No editing, Tas, writes, candidate loading | None: `Query.ThermalSelectionRows` only reads; a test proves the model JSON is unchanged |

## Findings that matter for Stage B

1. **Layout.** The `TabControl` spans Grid columns 2-3 (a `461*` / `86*` pair, a relic: column 3 holds nothing else). The
   splitter therefore resizes column 3 against the panel; it behaves correctly, but Stage B should replace the pair by one star
   column so the splitter has a clean neighbour.
2. **Selection plumbing.** `GetActiveViewportControl().SelectedSAMObjects<T>()` and the `ObjectSelectionChanged` event are the
   hooks; the main window attached the event only to views created later (with an empty handler), so the spike also attaches it
   to the first 3D view. Selecting by Guid on an *aperture* that is not visible from the default camera selected nothing in this
   run (the same Guid route was used in PR3 with the right-click menu); the panel code handles apertures (tested) but the
   real-app aperture selection was not shown here.
3. **Ribbon.** A toggle fits the View tab; the spike has no icon. The final entry is the "Thermal performance" toggle the review
   specifies, and the 3D right-click item opens/shows the panel.
4. **Cost.** The hook code is one `RefreshThermalPanelSpike()` call in each of four existing handlers (selection, model
   modified, tab changed) plus one partial-class file; no change to the viewport or the tree.
5. **Open.** Keyboard focus and tab order into the panel, and a minimum width for the viewport, were not exercised.

## Files (spike branch only, kept apart from Stage A-lite)

`Controls/ThermalPerformanceSpikeControl.xaml(.cs)`, `Windows/AnalyticalWindow.ThermalPanelSpike.cs`,
`Classes/Thermal/Spike/ThermalSelectionRow.cs`, `Query/ThermalSelectionRows.cs`, the Grid / ribbon lines in
`Windows/AnalyticalWindow.xaml` and the hooks in `AnalyticalWindow.xaml.cs`; tests in `ThermalSelectionRowsTests.cs`
(4 tests). Evidence is local (`C:\TasOut\uvalue\alite\run2`).
