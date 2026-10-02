# Thermal Stage D2 - "Add source…", remembered, for opaque constructions and glazing (2 Oct 2026)

Builds on D1 (#173, construction alternatives) and the glazing source/cache work of U-value PR3. The Thermal Performance panel can now take **more candidates
than the model and the default libraries**: the user adds a Tas construction database (`.tcd`) or a JSON file of constructions as a **source**; it feeds the
opaque alternatives (walls, roofs, floors) **and** the glazing `Change…` list, is remembered between sessions, and never touches a model until a candidate is chosen
and applied.

## What the user sees

* A **Sources** section at the top of the panel: `Sources  [Add source…]`, a hint ("Candidates come from the model and the default library." / "…, the default library and:"), and one entry per added file: its name, what it holds (`577 constructions · 104 window systems · 473 door systems`, "Loading…", "Remembered · read when needed", or the reason it failed / has nothing to offer) and a `✕` that **forgets** it (the file and the model are not touched).
* **Add source…** opens the file dialog (`.tcd`, `.json`). The file is read on its own STA thread through the existing importer and JSON cache; the entry shows its progress.
* A loaded construction appears in the Alternatives list marked by its **file name** (`Notional Roof 2 · U 0.150 · ncm.tcd`, "ncm.tcd · not in the model yet", "Adds 2 materials to the model: …"); a loaded window system appears in a window row's `Change…` list with the same label. Choosing one is the existing flow: **only that construction / system and the materials the model lacks enter the model on Apply**; one Undo removes them.

## Architecture (one shared source, no second framework)

| Part | Where | What |
|---|---|---|
| Reader | `Query/ThermalSourceReader.cs` (`ReadThermalSource[Async]`) | The glazing reader's import, kept whole: the SAM_Tas converter and the **same JSON cache** (`GlazingSourceCache`, so a database converted by the glazing window is instant here and vice versa), but ONE source per file holding its opaque constructions, its transparent constructions as window systems and the others as door systems, and the materials they name. Guids are kept (the same construction is the same candidate on every load). A pane library (IGDB) says it has no constructions or systems - **no pane+gap+pane composition**. Never throws: an empty source with a note. The existing `ReadGlazingSource` was refactored only to share `ReadTcdConstructionManager` / `ApertureConstructionsOf` (behaviour unchanged, its tests untouched). |
| Pool | `GlazingSource` (D1: also serves opaque constructions) | The pool type is shared by panels and apertures; it keeps its name (a rename would be churn in a D2 PR; noted for later). |
| Catalog | `Classes/Thermal/ThermalSourceCatalog.cs`, in `ThermalEditServices.Sources` | The list of entries (`Pending / Loading / Ready / Failed`), `AddAsync`, `Remove`, `EnsureLoadedAsync`, `SourcesChanged`. **Remembered** through `IThermalSourceStore`; the real store is `ActiveManagerThermalSourceStore` = SAM's own user-settings mechanism (`ActiveManager.SetValue/Write`, the one the model filters use), storing the file **paths** only (the converted copy is in the existing TCD cache). **Lazy**: a remembered source is listed at once and read only when a row first needs candidates (typing a target, opening `Change…`), so opening a model costs nothing; a missing file stays listed as failed until forgotten. |
| Opaque | `ConstructionAlternatives(…, catalog)` | Candidates of the ready sources join the pool (after the model and the default library, in the order added); the list is rebuilt as each source arrives or is forgotten ("Reading the added sources…" while loading). |
| Glazing | `ThermalRowEditor` (+ `GlazingViewModel.AddSourceAsync`, existing) | Sources already read join a new `Change…` list; ones that arrive later (or are added with the list open) are added to it; closing the list stops following. |
| UI | `ThermalPerformanceControl` | Sources section; `PickSourceFile` (the open-file dialog by default; tests/hosts can supply their own). |

## Safe duplicates and identity

* **The same file is one source** however it is spelled (full path, case) - read once, remembered once.
* **A construction in several sources is one candidate**: Guid identity, the first source wins (model → default library → sources in the order added); the same for window systems in a glazing pool (existing rule).
* **A name already in the model** is handled at Apply exactly as for D1 / glazing: the chosen construction is added under a numbered name and the note says so before.
* **A material that differs from the model's material of the same name blocks** the candidate (existing rule, `MaterialIdentity`).
* **Nothing is written to a model by adding, listing, choosing or forgetting a source**: tests compare the model JSON before and after; adding a source never calls `SetJSAMObject`.

## Found with the real source (changes to the D1 list)

The first real database (NCM Part L 2021: 577 constructions) showed two things, both fixed here:
* The list waited for the last of 15 Tas runs (~30 s). The pool is now asked **in chunks of 40, one request each, the constructions made for the panels' own group first**, so the list fills in as each chunk is calculated; still one Tas run per chunk, in order, every U-value cached (`A_big_pool_is_asked_in_chunks…`).
* Roofs and floors led a wall's list (closest U). Among equal "meets the target" the constructions **made for the panels' own group now come first** (`ConstructionAlternativeRow.MadeForOtherGroup`); still no ranking beyond that.

## Tests

`ThermalSourceTests` (21): reader (both kinds with Guids and materials, shared cache with the glazing reader, pane library note, missing / broken file, JSON, async), catalog (add / list / remember, same file once, remembered = pending until needed, failed stays listed and can be retried, empty source says why, forget updates list + memory + candidates, forgotten while loading never arrives), opaque candidates from a source (marked by file, model unchanged, material clash blocks, a construction in several sources once, remembered source read at the first target and the list follows, forgetting returns a chosen candidate to the generated variant), **Apply adds only the choice and what it lacks / one Undo removes them**, glazing list follows sources read before and after it opened and stops when closed, the control (Add source with a supplied file, list, forget). `ConstructionAlternativesTests` +1 (chunks, group first) and the ordering expectation. Existing `ThermalEditServices` call sites now pass an in-memory source store so no test reads or writes the user's settings. Full WPF suite **1990/1990**.

## Real-app acceptance (real Tas; `NCMConstructions_v6.1e (Part L 2021).tcd`, 577 constructions / 104 window systems / 473 door systems; model: 12 walls U 0.26 …)

Evidence (local): `C:\TasOut\uvalue\d2\run1…run4` (driver `d2_source.ps1`).

1. `Add source…` → Win32 file dialog → path → the entry appears: first import **30 s** (Tas conversion, runs on its own thread; the panel stays responsive), **5 s** from the JSON cache on the next add. Listed as `ncm.tcd · 577 constructions · 104 window systems · 473 door systems`.
2. Wall row, target 0.20 (12 walls): rows from the source (`Notional Top-Lit Ground Floor · U 0.195 · ncm.tcd`, `Pitched roof (E&W) 1995 Part L · U 0.185 · ncm.tcd`, …), "30 existing constructions meet U 0.2" (capped), each marked by `ncm.tcd`, "not in the model yet", "Adds N materials…". All 577 + the model's and the library's calculated by real Tas in **~30 s** (15 chunked runs; first rows appear after the first chunk), then cached for the session.
3. Choose a loaded construction → preview `U 0.260 → 0.195 W/m²K · … (ncm.tcd)`, "No new warnings", `1 change · 12 elements` → **Apply 3.9 s** → "12 panels now Notional Top-Lit Ground Floor. One Undo reverts it." → **one Undo** restores the wall row (`SIM_EXT_SLD U 0.260 · 12 use it`).
4. **Remembered**: a new session lists `ncm.tcd · Remembered · read when needed` at once and reads nothing; opening `Change…` on the window row reads it (cache) and the window row's list shows its systems (`ncm.tcd · …` lines in the glazing list, 5 realised of 104 in the virtualised list); typing a wall target afterwards offers its constructions.
5. **Forget** (`✕`): the entry disappears, the setting is cleared, and the open Alternatives list updates at once ("7 existing constructions meet U 0.2; 2 more are within 10 %": the model's and the library's only).

## Limitations / next

* The U-values of a big source are calculated once per session (≈50 ms each; 577 → ~30 s), not persisted between sessions; a persistent U-value cache (like the TCD conversion cache) is the next step if sources of that size are routine.
* The first import of a database is the existing importer's cost (30 s here; about a minute for IGDB-sized files); a progress line shows it.
* No search/filter box on the candidate lists; the opaque list shows at most 30 existing constructions.
* A model that stays closed over several sessions keeps remembering the path only; if the file moves it stays listed as failed ("The file could not be found.") until forgotten.
* No per-Apply text report for construction changes (D1); no IGDB composition; classic windows unchanged.
