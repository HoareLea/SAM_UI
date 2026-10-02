# Thermal Performance panel - host choice: docked, floating, or both (2 Oct 2026)

Question (owner): the docked panel works, but is a docked-only host the right choice, given the wish for more workspace
control? Keep `ThermalPerformanceControl` and its view-model independent of the host, prototype a modeless floating host with
the same control, compare, and prefer a hybrid if it is clean.

## Result: hybrid, and it is clean

**Docked by default; `Undock` in the panel header moves the same control into a modeless window; `Dock` in that window puts it
back; the window's own close button hides the panel (the ribbon toggle goes off); the toggle reopens it in the host it was last
in.** One interaction each way.

## What is shared and what is host-specific

| | Shared (one copy) | Host-specific |
|---|---|---|
| `ThermalPerformanceViewModel`, `Query.ThermalPerformanceGroups`, `ThermalPerformanceControl` (rows, modes, highlight) | everything | nothing |
| Feeding it the model and the selection, handling `HighlightRequested` | `AnalyticalWindow.ThermalPerformance.cs`, unchanged whichever host holds the control | - |
| Docked | - | a Grid column and a `GridSplitter` (B0) |
| Floating | - | `ThermalPerformanceWindow`: a window with `Content = control`, `Owner = AnalyticalWindow`, no task-bar button, no logic |

The control knows one thing about hosts: a header button that raises `HostRequested(Docked | Floating)` and an `IsFloating` flag
(button wording, and the left border that only makes sense beside the viewport). Moving it is `Children.Remove` / `Content = null`
and re-parenting; the view-model instance and its mode survive (tested). Stage C's editing logic will live in the control and its
view-models, so it needs nothing per host.

## Comparison (real app, one monitor 1920x1080, evidence local `C:\TasOut\uvalue\hosts`)

| Criterion | Docked | Floating | Verdict |
|---|---|---|---|
| Viewport space | tabs 1 667 -> **1 344 px** wide (-323 px, 19 %) | tabs stay **1 667 px**; the 340 x 600 window covers part of the view unless moved | Floating wins for the picture, docked wins for "nothing covers the model" |
| Resize | splitter drag (B0: 320 -> 618 px) | window edges / grip: 340 x 640 -> 520 x 420; viewport unaffected | Floating is freer (height and width) |
| Selection following | row click (`select all N`) -> panel "9 elements selected" | identical: Roofs -> 9, Walls -> 12 selected, panel updates in the floating window | Same code path, same result |
| Focus | stays in the main window | opens **without** stealing focus (main stays foreground); a click in it activates it; a click in the main window puts the main window foreground and the floating window **stays visible above it** (owned) | Both fine |
| Keyboard | - | UIA `SetFocus` on a radio in the floating window: it has keyboard focus and Space acts on it (stand-in for typing in Stage C; the panel has no text box yet) | Typing will reach the floating window |
| Multi-monitor | cannot leave the main window | draggable onto another monitor; **position and size are remembered** and reused on the next `Undock` / reopen while the title strip is still reachable on some monitor (`Query.ThermalFloatingBounds`, 6 unit tests incl. monitor gone, straddling, taller than the monitor) | Floating is the only host that uses a second monitor |
| Dock again | - | 1 click: panel back in its column at its remembered width (320 px), mode kept ("Whole envelope" survived the round trip) | Clean |
| Hide / show | toggle | toggle; closing the window with `x` = toggle off; toggle on reopens it in the last host | Consistent |
| Cost | existing | ~150 lines in the host partial + a 60-line window | Low |

Limits: this VM has one monitor, so a real second monitor, per-monitor DPI differences and a monitor being unplugged were not
exercised in the app (the placement rule is unit-tested instead; UIA `Move` to x = 2600 was clamped by Windows to the primary
monitor). Work areas are converted with the main window's DPI. Persisting the host and the bounds **across sessions** needs a
settings home and is not done (remembered while the window lives).

## Recommendation

Keep the hybrid. Docked stays the default (nothing covers the model, one place to look); floating is one click away for
multi-monitor or a taller editing surface in Stage C, with no second implementation. Stage C builds its editing UI inside
`ThermalPerformanceControl`; if the panel needs more room than a 320 px column, the user floats it instead of the control
growing a second layout.

## Tests

`ThermalPerformanceHostTests` (9): placement (first open beside the owner, second monitor kept, monitor gone, partly off-screen
but grabbable, taller than the monitor, no monitor information), the same control moving between a grid and the floating window
keeping view-model and mode, the host button asking for the other host and its wording, user close vs host close.
