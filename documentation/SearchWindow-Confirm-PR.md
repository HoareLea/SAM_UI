# SearchWindow can be confirmed - OK / double-click / Enter (PR record)

Branch `fix/searchwindow-ok-confirm-2026-10-01`, from `sow/2026-Q3` `cdaf1b2d`. **Not merged.**
`PROJECT_PROGRESS.md` is not touched on this branch (closeout after merge, per `AGENTS.md`).
Independent of the U-value PR1b (SAM_UI#160, since merged); it was found while accepting that work.

## Status

Implemented, unit-tested (full WPF suite green) and verified with real mouse and keyboard input. Open for review.

## Problem

`SearchWindow` - the "Select Construction" / "Select Filter" / ... picker - could not be confirmed. `button_OK` had
no `Click` handler and no `IsDefault`; `SearchWindow.MouseDoubleClick` only forwards to
`SearchControl_Main.MouseDoubleClick`, a delegate nothing raises (`ListBox_Main_MouseDoubleClick` is never wired in
`SearchControl.xaml`) and no caller sets. So every `ShowDialog() != true` caller could only close the dialog with
Cancel / X and its choice was never applied. Observed in the real app: right-click > Assign Construction opens
"Select Construction"; OK click, UIA Invoke, Enter and double-click on an item all left it open. `git log -S"OK_Click"`
finds no handler anywhere in the file's history; `git log origin/sow/2026-Q3` and the open PRs had no fix.

Callers affected: `Modify/AssignPanelConstruction`, `AssignApertureApertureConstruction`,
`AssignSpaceInternalCondition`, `CreateCaseBy{Aperture,ApertureConstruction,FinShade,WindowSize}Control`,
`AnalyticalWindow` (~l.498), `FilterControl`.

## Change

- `SearchWindow.xaml`: OK is `IsDefault="True"`, starts `IsEnabled="False"`, has `Click="button_OK_Click"`.
  Cancel is unchanged (`IsCancel`).
- `SearchWindow.xaml.cs`: OK is enabled exactly while the list has a selected item (listens to the bubbling
  `Selector.SelectionChanged`, so a filter that removes the selected item disables it again). OK, double-click on an
  item, and Enter (via `IsDefault`, from the list or the search box) all `Confirm()` -> `DialogResult = true`, only
  when something is selected.
- `SearchControl.xaml(.cs)`: new bubbling routed event `ItemDoubleClick`, raised from an `EventSetter` on the list's
  `ItemContainerStyle`. Needed because `Control.MouseDoubleClick` is a **Direct** routed event - it reaches only the
  element clicked, so the window cannot listen for it on the control, and the old `ListBox_Main_MouseDoubleClick`
  could never have worked as a hook. A double-click on the search box, scroll bar or empty list area does not raise it.
- The existing public `MouseDoubleClick` / `SelectedIndexChanged` delegates are left as they are: they are replaceable
  by callers, so the window's own behaviour does not depend on them. No caller sets either.
- No caller changes: all of them already test `ShowDialog() != true`.

## Tests

`WPF/SAM.Analytical.UI.WPF.Tests/SearchWindowConfirmTests.cs` (8 tests, `[WpfFact]`, in `WpfCollection`): each opens a
real modal `SearchWindow` and drives it from the dispatcher after `ContentRendered`; a 20 s watchdog closes the window
so a regression fails an assertion instead of hanging; assertion failures inside the callback are captured and
rethrown on the test thread.

| Test | Asserts |
|---|---|
| `SelectingAnItemAndClickingOK_...` | select + OK (UIA Invoke -> `Click`): `ShowDialog() == true`, `GetSelectedItems` returns that item |
| `OK_IsEnabledOnlyWhileAnItemIsSelected` | disabled -> selected enables -> cleared disables |
| `FilteringAwayTheSelectedItem_DisablesOK` | filter removes the selected item: OK disabled |
| `DoubleClickingAnItem_...` | `MouseDoubleClick` raised on the `ListBoxItem`: closes `true`, returns the item |
| `DoubleClickingOutsideAnItem_DoesNotConfirm` | double-click on the list itself with a selection: stays open |
| `OK_IsTheDefaultButton_...` | OK `IsDefault`, Cancel `IsCancel` (Enter itself cannot be injected from a test; see below) |
| `Cancel_ClosesWithoutTrue_...` | Cancel with a selection: not `true` |
| `MultiSelect_ConfirmsWithAllSelectedItems` | `SelectionMode.Multiple`: both items returned |

Against the **pre-fix** product code (stashed) 6 of the 8 fail (the two that pass are the Cancel and
outside-double-click guards, which hold either way). With the fix: 8/8.

## Validation

- Full WPF suite, Release: **1626 passed, 0 failed**.
- Real input, throw-away harness (not committed) hosting the built `SearchWindow`, driven by `SetCursorPos` /
  `mouse_event` / `keybd_event` on a foreground window (`DialogResult` / selected item):

| Scenario | Result |
|---|---|
| double-click "Concrete" | `true`, Concrete |
| single-click "Concrete", Enter | `true`, Concrete |
| single-click "Concrete", click OK | `true`, Concrete (OK enabled only after the click) |
| click disabled OK, press Enter, nothing selected | stays open |
| double-click in the search box | stays open |

  This is the path the unit tests cannot cover (WPF's own double-click detection and the Enter -> default-button
  route). The 3D-view right-click > Assign Construction flow itself was **not** re-driven in the full app.

## Risks / not covered

- Behaviour change for every `SearchWindow` caller, by design: Enter and double-click now confirm, and OK is disabled
  until something is selected. `AssignPanelConstruction` pre-fills `SearchText` with the panels' current construction
  name, which filters the list but selects nothing, so the user still has to pick an item.
- Other `SearchControl` hosts (e.g. `NCMNameCollectionControl`) are unaffected: the new event is opt-in.

## Next step

Review and merge into `sow/2026-Q3`; then the `PROJECT_PROGRESS.md` closeout commit on the base branch. Optionally
re-run right-click > Assign Construction in the licensed app to close the end-to-end loop on the U-value acceptance.
