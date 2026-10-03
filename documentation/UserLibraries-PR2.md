# User libraries - PR2: "My library" manager window + glazing lifecycle UI

Stacked on PR1 (`feature/user-library-engine-2026-10-03`, SAM_UI#184). SAM_UI only; no model code; Apply is still the only thing that mutates a model.

## 1. What changed

| Area | Change |
|---|---|
| `Classes/UserLibrary/UserLibraryViewModel.cs` (new) | Model-free view-model of "My library": rows (name, build-up, Ug/g/LT/Uf **as recorded at save**, saved date, based-on, details from the Builder provenance), inline **Rename** with the library's own naming rule shown while typing, **Remove** only after a confirmation (text built here: name, short Guid, "models keep their own copy", "not deleted: moved to *Glazing Systems.removed.json*"), a request to **Open in Builder**, an unreadable-library note (nothing can be changed), and it follows `UserGlazingLibrary.Changed` (posted to the thread it was created on) until disposed. The list's selection follows the Guid across refreshes. |
| `Windows/UserLibraryWindow.xaml(.cs)` (new) | Owned **modal** window (named AutomationIds, min size 720x460, virtualised list, F2 = rename, Delete = remove after confirmation). No model behind it. |
| `Controls/ThermalPerformanceControl` | **My library…** button next to *Add source…*. Candidate list **context menu**: *New system based on this…* (every candidate), *Rename…* and *Remove…* (only "My glazing systems" rows). Test hooks `ShowLibrary`, `ConfirmRemove` (and the existing `ShowBuilder`). |
| `ThermalRowEditor.CreateBuilder(GlazingCandidate seed)` | Seeding the Builder no longer requires *choosing* the candidate (the row's pending change is untouched). `CreateBuilder()` is unchanged in behaviour. |
| `ThermalPerformanceViewModel` | `CreateUserLibrary()` and `CreateBuilder(ApertureConstruction seed)` (Builder from a saved system with no open list: model / default library / user systems as pane and frame sources). |
| `GlazingCandidateRow.IsUserSystem`, `Query.GlazingBuiltFrom` | The report's "Built from / Panes / Gaps / Frame" lines were extracted unchanged so the report and the manager share one provenance text (the GLAZING CHANGE report tests are untouched and pass). |

Decisions:

* **Rename… from the candidate context menu opens the manager on that system, ready to rename** (one place owns the naming UI); **Remove…** goes straight to the confirmation (no window needed for one system). The open list follows through `Changed`.
* **"Open in Builder…" and "New system based on this…" both save as a NEW system in this PR** (both seed the Builder). PR3 gives Open in Builder its edit mode (round-trip fidelity, *Save and replace*); the button tooltip says "the selected one is not changed".
* The window has one section ("Glazing systems"); the opaque *Constructions* tab arrives with the opaque library (PR4), not as an empty placeholder.
* Modal-on-modal is used as the Builder already is (the manager is modal over the panel's window; the Builder is modal over the manager). Modeless is a PR6 question.
* The list's selection must survive a refresh: `ListView` clears its selection when it is given new rows and writes `null` back through the two-way binding. The view-model ignores a `null` while it is refreshing and puts the selection (by Guid) back - found by the window test, not by the view-model test.

## 2. Tests (23 new, all pass)

* `UserLibraryViewModelTests` (13, `[WpfFact]`): empty / missing, rows with provenance values, no-provenance system, unreadable library (nothing changeable, file untouched), Rename rule while typing + label-only result, cancel, a rename the library refuses under its lock (stale list), Remove asks first / only a yes removes / failure is a message and the system stays, Open in Builder is only a request, the list follows another thread's Save / Rename / Remove and stops when disposed, and the structural **"holds no analytical model"** scan over the view-model, row, window, library, engine and archive (with a non-vacuity check on `ThermalPerformanceViewModel`).
* `UserLibraryWindowTests` (10, `[WpfFact]`): the real XAML (named controls, list, details, inline rename with the rule, Remove with the confirmation hook, empty / unreadable states, follows a save from another thread, Open in Builder request) and the real panel (**My library…** button, Open in Builder from the manager seeds the Builder from that system and saves nothing, context menu *New system based on this…* seeds from a non-chosen candidate **without choosing it**, *Rename…* / *Remove…* only on user rows, the open list follows). Every panel test asserts model JSON, `Modified`, history and `CanUndo` unchanged.

## 3. Results

* Full WPF suite, one run at the gate: **2313 / 2313** (2290 after PR1 + 23).
* The 152 library / manager / Builder / integration tests repeated 8 x: all green.
* Visual check: the window was rendered to an image with a throwaway test (not committed): list, details, inline rename, archive note and commands lay out as intended at the default size.

## 4. Not done / honest limits

* **No real-app UIA acceptance** (the plan listed it): the licensed Tas app was not driven in this overnight run. The real XAML is exercised in the WPF tests above over a fake Tas; the manager and its context-menu entry points need a short manual pass in the app before relying on them (open My library…, rename, remove, open in Builder, right-click a user system).
* No search / sort / filter in the manager (PR6, only if long lists appear); no Restore from the archive (D1).
