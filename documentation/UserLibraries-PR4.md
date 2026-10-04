# User libraries - PR4: opaque user library "My constructions"

Base `sow/2026-Q3` = `7160fa2f` (PR1-PR3 merged: #184 `29059c2e`, #185 `513ec6ec`, #186 `7160fa2f`). SAM_UI only. One independently reviewable PR; PR5 (frame authoring) and PR6 (UI pass) are not started.

## 1. What changed

| Area | Change |
|---|---|
| `UserConstructionLibrary` (new, typed) | "My constructions": its own file `Documents\SAM\User Libraries\Constructions.json` (+ `Constructions.removed.json`), on the **same engine** as the glazing library (`UserLibraryFile` lock / re-read / verify / atomic replace / `.bak`; `LibraryMaterialMerge`; `UserLibraryArchive`). No generic `IUserLibrary<T>`, no shared file. `Save(construction, materials, name, provenance)`: opaque only (a transparent, gas-only, layerless or unclassified construction is **rejected**, `UserConstructionLibrary.Rejection`), every layer's material must exist, **new Guid on every save**, name trimmed and unique (case-insensitive), materials embedded (identical reused; a different one of the same name saved as `name (source)` / `name 2` with the layers following), immutable once saved, `Changed` raised once after a successful write only. `Rename` = label only (same Guid, layers, materials, provenance). `Remove` = archive first, then the library (never loses an entry; same contract as glazing). Failure leaves the active library as it was; an unreadable file is never overwritten. The library has no model: saving, renaming, removing add no Undo. |
| `UserLibraryArchive` | One generic core (`ArchiveCore<T>`) behind two overloads (`ApertureConstruction`, `Construction`). The glazing overload is behaviour-identical (all PR1-PR3 archive tests unchanged and green). |
| `UserConstructionProvenance` (new) | `ParameterSet` **"SAM User Construction"**, fixed Guid `9c2b6d3e-4f1a-4a7e-b3d8-2e5f7a1c9b60`, **schema 1**, written once at the Save. See section 3. |
| `Query.ProposedConstruction` (new, pure) | The generated U-value construction, extracted from `Modify.SetUValue` (section 4). `SetUValue` calls it; the panel previews and saves from the same object. |
| `ThermalEditServices.UserConstructions` | Lazy, like `UserGlazing` (default = `UserConstructionLibrary.Shared`; created on first use; tests inject one). |
| `ConstructionAlternatives` | `ConstructionAlternativeKind.User` ("**My constructions**"). Pool order **Model -> Default library -> My constructions -> Added sources**, first Guid wins; the existing +-10 % target window, 30-row limit, opaque guard, material-collision blocking and cache are untouched. Subscribes to `UserConstructionLibrary.Changed` on the **thread the list was created on** (same opening-context pattern as glazing) and rebuilds the candidates after a Save / Rename / Remove; a chosen construction that left the list falls back to the generated variant. `Notes` carries an unreadable-library note. |
| Panel (`ThermalRowEditor`, `ThermalPerformanceControl`) | **Save to My constructions...** button on every opaque row and in the context menu of every alternative. Saves the chosen alternative, else the generated variant (once its target is reached), else the **current construction**; a right-clicked alternative is saved **without choosing it**. A name is asked for (`UserConstructionNameWindow`, the library's rule shown as you type). Saving starts no edit, pins no scope, adds no pending change (a pending one is untouched), changes no model JSON and adds no Undo. |
| My library (`UserLibraryWindow`) | New **Constructions** tab next to Glazing systems (`UserConstructionLibraryViewModel`, model-free): Name, build-up, U-value at save, heat-flow basis, saved date, source / provenance, details pane; Rename (the library's rule while typing), Remove (confirmation, to the archive), empty state, unreadable note. **No opaque edit.** The tab is part of `UserLibraryViewModel.Constructions` (owned and disposed with it). |
| Classic Constructions editor | `ConstructionLibraryWindow` gains **Save to My constructions...** (event `SaveToMyConstructionsRequested`, shown only while a host handles it, like "Set U-value..."); `Modify.EditConstructions` handles it through `Modify.SaveToMyConstructions` (name prompt, new Guid, provenance "Constructions editor"). The editor edits a copy and stays open; neither it nor the model is changed. In-place editing semantics are untouched. |
| Reports | `CONSTRUCTION CHANGE` reports `My constructions (My constructions)` as the source, with the Guid, for `GlazingSourceKind.User`. |

## 2. Product journey

opaque row -> (generated variant | chosen alternative | current construction) -> **Save to My constructions...** -> name -> a NEW construction (new Guid) with its materials and provenance in `Constructions.json` -> offered as **My constructions** in the alternatives of every model and session -> choose + Apply = the existing construction change (one model change, one Undo).

## 3. Provenance schema (v1)

All keys are optional on read (a missing key = "not recorded"). Labels and **file names only - never a machine path** (`DraftPane.FileNameOnly` on the added source and the origin model name).

| Key | Meaning |
|---|---|
| `Schema Version` | 1 |
| `Created UTC` | ISO 8601, set by the library at the Save |
| `Saved From` | `GeneratedVariant`, `Model`, `DefaultLibrary`, `AddedSource`, `MyConstructions`, `ConstructionEditor` |
| `Saved From Source` | the added source's / library's **file name** |
| `Based On Name` / `Based On Guid` | the construction it was made from (a generated variant: the construction it adjusts) |
| `Origin Model Name` | the model's **name** (never its path) |
| `U-value At Save [W/m2K]`, `Target U-value [W/m2K]` | the value shown when saved; the target (generated variant only) |
| `Heat Flow Direction`, `Heat Flow Basis` | e.g. `Horizontal`, `Horizontal heat flow, external surfaces (WallExternal, from the panels)` |
| `Engine`, `Route`, `SAM_Tas Version` | `Tas TCD (SAM_Tas ThermalTransmittanceCalculator)`; e.g. `Thickness of layer 3 (I01_Mineral Wool) solved for the target U-value (Tas layer thickness calculation)`, `U-value of the construction as it is (Tas thermal transmittance)`, `U-value stored on the model's panels (Tas)` |

One set per object (SAM merges sets of a name, later values winning - SAM#146); a construction saved again from My constructions gets exactly one set, the new one.

## 4. Generated-construction extraction (no behavioural change)

`Query.ProposedConstruction(source, materialLibrary, layerIndex, thickness, mode, newName, calculatedU, constructions)` returns a `ProposedConstructionResult` (construction, adjusted material, whether the material is new, old / new thickness) and **changes nothing** (not the source, not the material library, not the model). `Modify.SetUValue` calls it where it used to generate inline and applies the result (adds the material, assigns, stores) exactly as before.

* **Parity** (`ProposedConstructionTests`): the query is compared with a **verbatim copy of the old generation code** over every path - new / in place, made / typed / taken names, a material already adjusted (suffixes do not stack), one the library already has, 1 mm rounding, `DefaultThickness` following, and every failure - for the construction JSON, the material JSON (Guid aside), `MaterialAdded`, thicknesses and error text. Purity test: source, material library, constructions and model JSON identical before / after; `SetUValue` applies exactly the query's construction and material.
* **Error order is unchanged**: the request's own errors (layer, thickness, name sharing) are reported before the scope's, a missing material after it - pinned by a test (`ProposedConstructionErrorKind`).
* The existing `SetUValueTests` (the Apply behaviour suite) pass unchanged.

## 5. Tests (new, all pass)

| Class | # | Covers |
|---|---|---|
| `UserConstructionLibraryTests` | 17 | save (new Guid, trimmed name, layers, default panel type, provenance), embedded materials, unique name, rejections (transparent / gas / layerless / missing material / empty name), input untouched, material collision inside the user library (renamed, layers follow, identical reused, no folder recorded), failed write, unreadable file, own file next to the glazing one (aperture constructions kept), Rename (label only, taken / empty / unknown, same name, case only), Remove (archive with materials, pruning, retry after a failed library write, failing / unreadable archive), provenance round trip, one set after a re-save, services lazy |
| `ProposedConstructionTests` | 6 | parity with the old generation, purity, `SetUValue` equivalence, error order |
| `UserConstructionCandidateTests` | 13 | listing and wording, precedence Model -> Default -> My -> Added with first Guid wins, +-10 % window / roof note / 30-row limit, opaque guard, material collision blocks Apply, unreadable note, refresh after Save / Rename / Remove (`[WpfFact]`), thread affinity, dispose, selected removed candidate resets |
| `UserConstructionPanelTests` | 10 | button on opaque rows only, current / generated / chosen / context-menu saves with provenance, no edit / no pending change / no model JSON change / no Undo, a pending change survives, right-click does not select, Rename / Remove with the panel open, **Apply of a saved construction = one change, one Undo, back to baseline**, report wording |
| `UserConstructionManagerTests` | 11 | rows and details, empty state, rename rule + label only, remove confirmation + archive, unreadable note, thread-safe follow + dispose, **no analytical model anywhere** (reflection), the real window's tab / controls / empty state, the panel's entry point |
| `UserConstructionEditorTests` | 4 | the classic editor's button (visibility, selection, event, window stays open and unchanged) and the host's handler (prompt, provenance, cancel / reject / taken) |

Full WPF suite **2402 / 2402** (Release; baseline 2341 on `7160fa2f`).

## 6. Material collisions (measurement for PR6 / future work - no scope change)

Owner decision unchanged: a same-name / different-material collision on Apply stays **blocking**; nothing is renamed on Apply. The practical rate was measured with a temporary harness (not in the PR) using `ConstructionCandidate`'s own rule (`MaterialIdentity.Same`), over shipped data only:

* **11 real SAM models** (3-6 constructions, 18-25 materials each; all built on the SAM default material library) x the **default library** (18 opaque constructions): **0 blocked** in every model (0 / 198); 7-11 per model need materials added, the rest are already in the model. Every one of the 18 shared names has the identical definition.
* The same 11 models x four **shipped Tas databases** as added sources (Constructions.tcd 27, ASHRAE 90.1 (2013) 684, NCM v6.1e 473, NCM v5.2.7 341 opaque constructions): **0 blocked** - the models and those databases share no material name at all.
* **My constructions saved from one model and reused in each of the other 10** (110 ordered pairs): **0 blocked, 0 missing**; 0-2 constructions per pair need a material added.
* **Database against database** (a model *built from* database X receiving a construction of Y; 9 sources): collisions concentrate where X and Y are **revisions of one family**: NCM v6.1e <-> v5.2.7 / v4.1: **41 % (193 / 473)** of v6.1e's and **64-65 % (206-223)** of the older ones blocked (78 materials of the same name redefined); ASHRAE 2016 -> a 2013 / 2010-built model **5.7 % (39 / 684)**, ASHRAE 2010 / 2013 -> the 2016-built **0**; across families **<= 12 %** (ASHRAE 2013 into a Constructions.tcd-built model 81 / 684; NCM <= 5 per revision).

Reading: for models made from the default library (the normal case here) the blocking rule never fires, for Tas-database sources and for My constructions alike; it fires for a model already built from an *older revision of the same database* (NCM above all). If PR6 revisits it, the cheapest option the data supports is a per-candidate "use the model's material" / "add as `name (My constructions)`" choice - not a global rename.

## 7. Native real-app acceptance (PASS)

The Release app + real Tas, driven through UI Automation over a copy of the representative model (baseline JSON hash `EA79562E984F`), then a **second, different model in a new process**. The user's library folder was backed up first and restored afterwards (glazing file hash unchanged).

| Step | Result |
|---|---|
| My library > Constructions, empty library | empty-state text, `0 saved constructions`, Rename / Remove disabled, tabs `Glazing systems | Constructions` |
| Wall selected, target 0.18 typed | list in 1.9 s (cold, real Tas): generated variant, `SIM_EXT_SLD_Roof`, `SIM_EXT_GRD_FLR ...`; the button's tooltip says what it would save (the current construction; after the target, the generated variant) |
| **Save the generated variant** (before Apply) | prompt "Save the generated variant SIM_EXT_SLD U0.18 (U 0.180 W/m2K, made from SIM_EXT_SLD)", suggested name, Save; message `Saved 'UC Generated 0.18' to My constructions.`; the pending change (`1 change · 12 elements`, Apply enabled) is untouched; file: schema 1, `GeneratedVariant`, U 0.18, target 0.18, direction Horizontal, route "Thickness of layer 5 (I01_Mineral Wool...) solved for the target U-value", the adjusted `...0.123m` material embedded; model hash = baseline, Undo disabled |
| **Save a chosen alternative** (`SIM_EXT_SLD_Roof`, existing model) | the choice and the pending change stay; `Model` provenance, U 0.1631, route "U-value of the construction as it is"; model hash = baseline |
| **Save the current construction** (change discarded first) | no edit started, no Apply bar; `Model`, U 0.26 (stored), route "U-value stored on the model's panels (Tas)"; model hash = baseline |
| My library > Constructions (3 saved) | rows, details (build-up, U-value at save, basis, route, engine, model name); Rename to a taken name: error shown, OK disabled; **Rename refreshes the open alternatives list** (the row under the modal dialog shows the new name); model hash = baseline |
| **Remove** with confirmation (Cancel, then OK) | text names the construction and its short id, says models keep their copy and that it is archived; after OK it **leaves the active candidates**, is in `Constructions.removed.json` with its materials, and the library lost the materials only it used; model hash = baseline |
| Classic Constructions editor (Edit > Constructions) | button present, disabled until one construction is selected; the prompt, then `Saved 'UC Editor wall' ...`; the editor stays open and is closed with Cancel; provenance `ConstructionEditor`; model hash = baseline |
| **Choose the saved construction, Apply** | preview `UC Generated 0.18 (My constructions)`; `12 panels now UC Generated 0.18. One Undo reverts it.`; the saved Guid is in the model and the adjusted material was added; report `Source: My constructions (My constructions)`, the Guid, `Materials: 1 added`; CHECK clean |
| **One Undo** | Undo disabled again, model hash = baseline `EA79562E984F` |
| **Reuse in another model and session** | a different model (20 walls on its own `SIM_EXT_SLD`, 2 MB) in a new process: My library lists the three constructions; the target-0.18 list offers `UC Generated 0.18 · U 0.180 · My constructions`; listing, saving-free browsing and the library window leave the model hash unchanged; Apply = `20 panels now UC Generated 0.18`, report as above; one Undo |

A control run on that second model (applying an existing *model* construction, My constructions not involved) shows the same after-Undo difference: the model JSON equals the baseline everywhere **except the `SAM.Analytical.UI` parameter set** (the per-view settings the app stores in the model: ~30 KB after the first save, ~1 KB after Undo). It is existing behaviour of Undo for a model whose view settings were first written by the save - **not** part of PR4; on the representative model (view settings already in the file) Undo returned the exact baseline hash.

Evidence (drivers, screenshots, logs) is kept locally and is not committed. The acceptance library files were removed afterwards.

## 8. Honest limits

* No opaque layer / material editor (a construction is authored in the classic Constructions editor and saved as a new one); no Restore UI (the archive is a file); no cross-process file watching (as glazing); the Constructions tab has no Open-in-editor action.
* "U-value at save" is the value on screen: the generated variant's calculated U, the candidate's U on the row's basis, or - for the current construction of an unedited row - the U the panels store (route text says which); it is "not recorded" when none exists.
* `ThermalEditServices` and `ConstructionAlternatives` gained an optional trailing constructor parameter (source-compatible; their only consumers are in SAM_UI).
* SAM and SAM_Tas unchanged; no Grasshopper change (`SAM.Analytical.UI` gained one public event args type and one event on the classic window).

## 9. Closeout review (independent) - two defects found and fixed in this PR

* **Stale provenance.** SAM merges a same-named `ParameterSet` into the one already on an object (later values win), so a construction saved from a *saved* construction (the chosen My-constructions alternative, or an applied one saved again from a model) kept every key the new provenance does not write - e.g. the old `Target U-value`, `Saved From Source`, `Route`, `Engine`. `UserConstructionLibrary.Save` now empties the old set on its own copy before writing the new one. Test: `Saving_a_construction_that_already_has_provenance_carries_no_value_over_from_the_old_set` (fails without the fix).
* **Mis-encoded text.** Two user-visible strings of the save prompt (`W/m²K`) and two XML comments (`…`) in `ThermalRowEditor.cs` were double-encoded (`W/mÃ‚Â²K`). Re-encoded; the panel tests now assert `W/m²K` and no `Â` in the subject descriptions.
* Verified unchanged: `ProposedConstruction` is behaviour-identical to the old inline code (`Construction.ConstructionLayers` and `AnalyticalModel.MaterialLibrary` return copies, so no input is mutated; error order identical); the archive generalisation keeps the archive-first contract for both libraries (the glazing PR1-PR3 archive tests and the construction ones pass unchanged); the after-Undo difference on the second model is the `SAM.Analytical.UI` view-settings parameter set and also occurs without PR4 (control run: same after-Undo size, no My-constructions code involved).
* Public API: additive only (new types, an optional trailing ctor parameter on `ThermalEditServices` / `ConstructionAlternatives`, one event on `SAM.Analytical.UI.ConstructionLibraryWindow` that is invisible unless a host attaches a handler). No consumer of these constructors exists outside SAM_UI; no Grasshopper rebuild was needed.
