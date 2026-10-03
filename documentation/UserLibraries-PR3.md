# User libraries - PR3: Glazing Builder 2 - edit with fidelity + Save and replace

Stacked on PR2 (`feature/user-library-manager-2026-10-03`, SAM_UI#185), which is stacked on PR1 (SAM_UI#184). SAM_UI only; the Builder still holds no model.

## 1. What changed

| Area | Change |
|---|---|
| `UserGlazingLibrary.SaveReplacing(draft, oldGuid, ...)` | **One locked transaction**: the draft is saved as a NEW system (new Guid, exactly `Save`'s composition / check / material merge) and the saved system `oldGuid` is moved to the archive. The old name may be reused (the name check ignores the replaced system); another system's name may not. A replaced system that is no longer in the library is an error (nothing written). `UserGlazingSaveResult.Replaced` is the system as it was. `Changed` is raised once. |
| Failure contract (same as Remove) | Archive first, library second. Archive write fails / archive unreadable / read-only / lock held / library unreadable -> **both files untouched**, the old system still there, *"Nothing was saved or replaced"*. Library write fails after the archive -> the library is exactly as it was and the old system still in it (also in the archive, harmless); a retry replaces the archive entry by Guid, no duplicate. **The new system is never saved unless the old one is archived.** |
| Materials | The archive gets every material the old system used; the library loses only those no remaining system, the new system or an opaque construction of the file uses (same `Prune` rules as Remove). |
| Provenance **v2** | `Schema Version` 2; adds `Supersedes Guid` / `Supersedes Name` (written only for a replacement). A v1 system reads exactly as before (missing keys = none; `Rename`/`Remove`/listing unchanged - tested). "Built from" (report and My library) gains *"; replaces NAME (GUID)"* for a replacement. |
| Round-trip fidelity (`SeedDraft`) | A pane saved reversed (`<name> Reversed`) reopens as **the original pane with Reverse on** (`Query.Unreverse`, accepted only when reversing it again reproduces the saved material exactly; otherwise it is left as it was). A copied frame keeps where it really came from (the provenance's *Frame Copied From*), not the saved system. This applies to every copy of a Builder system ("New system based on this…" too). |
| Builder edit mode (`GlazingBuilderOptions.EditSeed`) | Opened on a saved system that the library still holds: draft under the system's **own name**; status *"Editing a copy of X · saving creates a new system"*; buttons **Save as new** (needs another name; a hint says so) and **Save and replace X**; the check shown is the one replacing is held to. Ignored (plain copy, name "X (copy)") when the seed is not in the library. The seed itself is never changed. |
| Entry points | My library → **Open in Builder…** and the candidate context menu **Open in Builder…** (user systems only) open edit mode. **New system based on this…** stays a plain copy. |

## 2. Round-trip identity (the claim and the proof)

Opening a Builder-made system and saving it unchanged under another name gives **the same pane and frame layers (names, thicknesses), the same materials (nothing added or renamed - the reversed pane is the same `<name> Reversed` material), the same gap heat transfer coefficients, panel type, description, frame width, pane and frame additional heat transfer, and the same provenance panes / gaps / frame width** - only Guid, name and the based-on / created fields differ.
Test: `Opening_a_saved_system_and_saving_it_unchanged_gives_the_same_...` (triple glazing with a reversed inside pane, frame width, both additional heat transfers).
That test found (and the fix removed) one real difference: the description / provenance said *"frame copied from Rich"* instead of the frame's true origin *SEED_GLZ*.

**Real Tas** (opt-in `SAM_E0_PILKINGTON_TCD`, Pilkington IGDB v76; run in this session): S1Plus *reversed* | Ar16 | clear4
`Ug 1.0470 g 0.4960 LT 0.7510` -> reopened for editing, unchanged `1.0470 / 0.4960 / 0.7510` (identical) -> edited to a 12 mm gap `Ug 1.2024` -> Save and replace, opened again `1.2024 / 0.4960 / 0.7510` (reproduces the edit).

## 3. Tests (new, all pass)

* `UserGlazingLibraryReplaceTests` (11): happy path (new Guid, old in the archive byte-identical, Supersedes, one `Changed`), materials (archive gets all, library loses only what nobody uses), name reuse only when replacing, errors / no name write nothing, replaced system gone, **archive-write failure, library-write failure + retry (no duplicate), unreadable and read-only archive, lock held, unreadable library**, v1 provenance reads, every new save is schema 2.
* `GlazingBuilderEditTests` (12, `[WpfFact]`): round-trip identity, un-reverse, `Unreverse` exactness, a plain copy still reverses back but offers no replace, edit-mode state / buttons / hint, another system's name blocks both, edit mode ignored when the library no longer holds the seed, Save and replace end to end (name kept, new Guid, archived old, `Saved` once, `Changed` once, provenance), replacing a system removed meanwhile (fails cleanly; Save as new still works), failed replace leaves the library as it was and the Builder retry-able, opening / cancelling never changes the seed, the real window's buttons (replace closes it; a plain copy has no replace button).
* `GlazingBuilderRealTasTests.EditedSystem_RoundTripAndReplace_ThroughTheBuilderRoute` (opt-in; no-op without the env var).
* `UserLibraryWindowTests`: the manager's Open in Builder expects edit mode; new context-menu test (Open in Builder edits, New system based on this copies, hidden for non-user systems).

## 4. Results

* Full WPF suite, final run at the gate: **2339 / 2339** (2313 after PR2 + 26 new, incl. the opt-in real-Tas test, which is a no-op without its env var and passed with it).
* The 267 library / manager / Builder / window tests repeated 8 x: all green.

## 5. Honest limits

* **Atomicity of "replace" is the Remove contract, not magic**: after a failed library write the old system is *also* in the archive (the library is unchanged). It is not "both files untouched" in that one case; every other failure leaves both files untouched.
* Material-name clashes between the new system's materials and the OLD system's materials are resolved like any save (the new one is kept under "name (source)" / "name 2" while the old one is still in the library, then the old one's exclusive materials are pruned).
* No real-app UIA acceptance (see PR2); the Builder window and manager are exercised as real XAML in WPF tests.
* Not done (out of scope): frame editing (PR5), opaque library (PR4), Restore from the archive, PR6 polish.
