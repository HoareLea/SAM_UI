# Thermal Performance - Stage B plan: the read-only panel (1 Oct 2026)

Status: **implemented read-only** on `feature/thermal-stage-b-readonly-2026-10-01`; record `documentation/Thermal-StageB-ReadOnly.md`.
Adjustment found in implementation: the stored opaque U is on the **panels** (`PanelParameter.ThermalTransmittance`), not on the
construction (see the record, finding 1).

Follows `Thermal-Performance-Review.md` and the B0 result (`Thermal-B0-DockingSpike.md`: **DOCKED**, plain Grid /
GridSplitter). Stage B ships the Thermal Performance surface **read-only**: it inspects, it never writes the model. Editing is
Stage C.

## Scope of Stage B

In:

1. The docked panel (the B0 spike promoted to production names): `ThermalPerformanceControl`, `ThermalPerformanceViewModel`,
   toggle in the View ribbon, hosted by `AnalyticalWindow` in two Grid columns behind a `GridSplitter`; the layout
   clean-up B0 asked for (one star column for the tabs instead of the `461*` / `86*` pair).
2. **Selection adapter.** The host hands the panel the model and the active view's selection (`ViewportControl` selection
   event, tab switch, model modified); the panel reads only.
3. **Two modes:** *Selection* (the selected panels / apertures by construction) and *Whole envelope* (every construction of
   the external envelope under Walls, Roofs, Floors, Windows, Doors).
4. Per construction: name; **stored** performance (opaque: U, read from the panels; apertures: U, g, LT, each "not calculated" when the model has no
   value; a value that differs between the apertures of one construction shows "varies"); selected count and used-by count
   (Selection) or element count and area (Whole envelope).
5. **Click a row to highlight its elements** in the active view (selects every element using the construction through
   `ViewportControl.Select`; the review's "Select all N").

Out (later stages, per the review): target editing, Apply, `ThermalChangeSet`, the candidate list, Tas calls (stored values
only; "Recalculate" is Stage C), replacing `SetUValueWindow` / `SetGlazingWindow`, the 3D right-click item, Legacy calculators,
the change log, opaque alternatives, remembered sources, persisting the panel width across sessions.

## Decisions

* **Stored U is shown as stored.** The aperture parameter `ThermalTransmittance` is the pane value Tas calculates (the
  PR3 acceptance shows 1.243 stored against Uw 1.35); the panel labels it "U" and does not show Uw, which needs Ug, Uf and the
  geometry (Stage C, `Query.GlazingOverallThermalTransmittance`). The panel never calls Tas.
* **Envelope = external panels** (`PanelType.External()`, without shades and solar panels) and the apertures they carry.
  Internal partitions are not envelope.
* **Used by** is always model-wide (every element using the construction), so the number matches Set U-value / Set glazing.
* The panel is **stateless about the model**: every refresh rebuilds from the model and the selection; nothing is cached across
  an Undo, so a change from elsewhere is picked up by the existing `Modified` hook.
* Highlight selects (it does not hide / isolate); it fires the normal selection event, so Selection mode then shows "N of N".

## Files

New: `Controls/ThermalPerformanceControl.xaml(.cs)`, `Classes/Thermal/ThermalPerformanceViewModel.cs`,
`ThermalPerformanceRow.cs`, `ThermalPerformanceGroup.cs`, `Enums/ThermalPerformanceMode.cs`,
`Query/ThermalPerformanceGroups.cs`, `Windows/AnalyticalWindow.ThermalPerformance.cs`. Changed: `AnalyticalWindow.xaml(.cs)`
(layout, toggle, four one-line hooks). The B0 spike files are renamed, not duplicated.

## Tests and acceptance

* View-model / query tests: grouping and ordering, both modes, the envelope rule, stored-value text incl. "not calculated" and
  "varies", counts and areas, the highlight Guids, no model write (JSON unchanged), no Tas call.
* Real app (UIA): toggle show / hide, splitter, follows a panel selection, Whole envelope lists the walls / roofs / floors /
  windows of the PR3 model with counts and areas, a click on a row selects its elements and the panel follows, Undo of an
  unrelated edit refreshes, the 3D view and tree unchanged when the panel is hidden.
* Full `SAM.Analytical.UI.WPF.Tests` green; the classic and PR2b / PR3 windows untouched.

## Risks

1. Selecting many apertures through the viewport (several thousand) may be slow; measure on the large model before Stage C.
2. Aperture selection by Guid did not select in the B0 run for an aperture hidden from the default camera; if the 3D view cannot
   select it, apertures highlight through the hosting panel instead (decide in the implementation, record the result).
3. The panel adds a refresh to every `Modified`; for a large model the rebuild must stay out of the hot path (measure; debounce if
   needed).

## Next step after Stage B

Stage C: `ThermalChangeSet` and editing in the panel (see the review).
