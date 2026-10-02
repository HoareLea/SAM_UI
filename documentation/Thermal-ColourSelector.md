# Thermal Stage D0 - "Colour by" selector (2 Oct 2026)

Replaces the single `Colour by U-value` toggle of the Thermal Performance panel (#171) with a compact selector, `Colour by: [ Off ▾ ]`, whose
options are the stored thermal properties the panel already shows in its rows (`U 1.243 · g 0.40 · LT 0.80`). It is **view state only**: no
analytical-model write, no `SetJSAMObject`, no Undo entry, per 3D view, not saved with the model. The colouring pipeline is #171's generic
`ParameterColouring` + `Create.ParameterColouredViewSettings` (existing `Query.TryGetValue`, `LegendItemDictionary`, `ColorPaletteGenerator`, renderer
and legend); D0 adds only the option catalogue and the selector.

## UI label -> SAM element -> parameter / value source -> units

| UI label | SAM element | Parameter (enum -> name used by `Query.TryGetValue`) | Value written by | Units | Palette |
|---|---|---|---|---|---|
| U-value (panels) | `Panel` | `PanelParameter.ThermalTransmittance` -> `"UValue"` | `Tas.Modify.UpdateThermalParameters` (Panel U from the Tas construction), `SetUValue` | W/m²K | `SamThermal` (existing) |
| U-value (windows & doors) | `Aperture` | `ApertureParameter.ThermalTransmittance` -> `"UValue"` | Tas: `Query.ThermalTransmittance(tbd aperture construction, CurtainWall for a transparent one)`; `SetGlazing`: `result.GetTransparentThermalTransmittance()` or the comparison's **Ug**; opaque aperture (door): `GetThermalTransmittance(panelType)` | W/m²K | `SamThermal` |
| g-value (windows) | `Aperture` | `ApertureParameter.TotalSolarEnergyTransmittance` -> `"GValue"` | Tas `Query.GlazingValues(...)`; `SetGlazing` (`result.TotalSolarEnergyTransmittance`, or `GlazingValues.G`) | 0-1 | `SamEnergy` |
| Light transmittance (windows) | `Aperture` | `ApertureParameter.LightTransmittance` -> `"Light Transmittance"` | Tas `Query.GlazingValues(...)`; `SetGlazing` | 0-1 | `SamSpectrumAnalytical` |

The parameter names are taken from the enums (`Core.Query.Name(Enum)`), not typed. `ThermalColourOption.cs` is the single place that lists them.

## Semantics that are deliberately NOT blurred

* **The window "U" is the stored glazing value, not Uw.** The aperture's `ThermalTransmittance` is what Tas reports for the transparent construction
  (`Ug`: the real model shows 1.243 for `SIM_EXT_GLZ` whose Ug is 1.24, while its Uw is 1.35). **Uw (pane + frame, area weighted) is calculated
  on demand** by the glazing workflow (`Query.GlazingUw`) and is **not stored on the element**, so it cannot be coloured from the model. There is no "Uw" option;
  the label says "U-value (windows & doors)" and the tooltip says it is the glazing value (Ug), not the whole-window Uw. `Uf` is a property of the
  aperture construction's frame layer, not of the element - not offered.
* A **door's** stored U is the whole-door value (`GetThermalTransmittance(panelType)`), so "windows & doors" is one honest option for the same
  parameter; g and LT exist on windows only - doors keep the default colour for those.
* Panel U and window U are **separate options with separate legends**: the same quantity, but different element type and meaning (a panel stores its
  construction's U; the glazing value is a centre-pane value). One legend mixing them would hide that.
* Nothing is derived from a construction to fake an element value: an element without a stored value stays in the default colour and is not in the legend
  (`Recalculate` in the panel fills the U-values).

## Behaviour

* Off first; the selector follows the active tab: disabled with a reason in a non-3D view; an option is listed but disabled when the view does not show
  that element type (`ThreeDimensionalViewSettings.ContainsType`). Choosing replaces the view's colouring; the active view is regenerated, nothing else.
* View Settings / Legend Settings behave as in #171: opening View Settings ends that view's transient colouring first (the dialogs edit the *saved*
  settings). **Legend Settings is greyed while a view is only transiently coloured** (`HasLegend` reads the saved view, which has none) - unchanged from #171.
* Apply / Undo refresh the colouring (existing `UpdateTabItem` path).

## Design seam for a different range (not implemented, no palette invented)

`ColorPaletteGenerator.GetColors(palette, values)` derives min/max from the legend values it is given, so today the scale is proportional over **every
element with a value** (internal partitions at U 1.775 compress the 0.145-0.26 envelope values). The two places a change would go, both inside
`Create.ParameterColouredViewSettings`:

1. *Envelope-only*: filter which elements feed `legendItemDatas` (e.g. a `Func<Panel,bool>` / `IsEnvelope` on `ParameterColouring`); internal panels then stay default.
2. *Fixed range / bands*: pass explicit bounds to a `GetColors` overload (or add the bounds as pseudo-values), or bucket the value text into bands before building the legend.

Both would be optional members of `ParameterColouring`; the selector and the options would not change.

## Tests

`ThermalColourOptionTests` (12): option -> element type/parameter name (and equal to the enum's name); Off first, readable labels, no raw names; the window-U
tooltip says Ug/Uw; `Of`/`IsAvailableIn`; windows' legend = the stored U/g/LT the panel row shows; an aperture option leaves the view's panel appearance alone;
switching between all options leaves model and saved view unchanged; the renderer keeps aperture legend colours; repeatable; the selector control raises
`ColourRequested` only for a user choice, never for `SetColourState`; disabled/unavailable states. `ParameterColouringTests` (7; the toggle test moved to the selector).
Full WPF suite 1938/1938.

## Real-app acceptance (real Tas, model: 12 external walls U 0.26, 9 roofs 0.164, 9 floors 0.145, 20 partitions 1.775, 20 windows U 1.243 / g 0.4 / LT 0.804)

Evidence (local): `C:\TasOut\uvalue\d0\run1`, `run2` (drivers `d0_colour.ps1`, `d0_windows.ps1`, `d0_cube.ps1`, `d0_south.ps1`, `d0_indep.ps1`, `d0_timing.ps1`).

1. Selector lists `Off | U-value (panels) | U-value (windows & doors) | g-value (windows) | Light transmittance (windows)`; baseline Off, no legend.
2. U-value (panels): legend `U-value [W/m²K]` 0.145 / 0.164 / 0.26 / 1.775 (= #171).
3. g-value: legend `g-value [0-1]` 0.4; on the south façade (reached with the ViewCube) **all windows recoloured** in the palette colour; panels keep their normal colours.
4. Light transmittance: legend 0.804; U-value (windows & doors): legend 1.243; back to U-value (panels): the panel legend again.
5. Selection highlight (Select By Guid wall) stays visible over a coloured view (selected wall and its windows in the selection blue).
6. Second 3D view: starts Off; choosing Light transmittance there leaves the first view on g-value; switching tabs restores each state.
7. 2D tab: selector disabled with "Colouring is available in a 3D view."; back on the 3D tab the choice is still shown.
8. Off: normal rendering, no legend. View Settings opens (Cancel) and ends that view's colouring.
9. **Undo button stays disabled through every choice** (checked after each step); saved model while coloured has no `"ParameterName":"UValue"/"GValue"/"Light Transmittance"`.
10. Switching takes ~1.1-1.3 s for this model (50 panels, 20 windows) measured as UIA select -> legend changed, UIA overhead included.

## Not in D0 / limitations

* No Uw, Uf, Ug-vs-Uw split, construction-level or compliance colouring; no new palette or range (seam above).
* Doors/opaque apertures have no g / LT, so those options show them in the default colour.
* A model whose elements store no value gives no legend (`Recalculate` first).
* Legend Settings stays greyed while a view is transiently coloured (unchanged from #171).
* The 3D camera cannot be turned by injected mouse drags in the VM; the ViewCube was used to reach the window façades.
