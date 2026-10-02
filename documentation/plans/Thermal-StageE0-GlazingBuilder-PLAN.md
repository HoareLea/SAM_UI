# Stage E0 — Glazing System Builder: planning document

**Status (2 Oct 2026): APPROVED by the owner.** The §19 decisions are resolved: (1) user library at `Documents\SAM\User Libraries\Glazing Systems.json`
(`ConstructionManager` JSON); (2) no in-place edit and no remove in E0 (saved systems are immutable); (3) frame = copy from an existing complete system or
none, with an explicit editable `DefaultFrameWidth` (no target-Uf synthesis); (4) owned modal Builder window; (5) pane `Reverse` only if the real-Tas probe
proves the semantics - **it did (E0-1 Gate 0): Reverse is enabled**; (6) only a clearly labelled example/reference Uw, never the stored glazing U-value.
Review corrections adopted: Cancel changes neither the model nor My Glazing Systems (`Add source…` keeps its D2 persistence); no absolute local pane-source
paths in model-travelling provenance; user-library writes prevent lost updates between SAM_UI instances; gap-HTC orientation provenance is persisted; a
material renamed on save renames its `ConstructionLayer` too. E0-1 (domain foundation) is recorded in `documentation/Thermal-StageE0-1.md`.
The text below is the plan as approved (investigation of 2 Oct 2026).

Base: `SAM_UI` `sow/2026-Q3` @ `a03940bc` (D0 #172, D1 #173, D2 #174 merged; WPF suite 1996/1996). Investigated 2 Oct 2026 across
`SAM` (core), `SAM_Tas`, `SAM_UI`. No code, branch, commit or PR was made. Paths: `WPF/` = `SAM_UI/WPF/SAM.Analytical.UI.WPF/`,
`T/` = `SAM_UI/WPF/SAM.Analytical.UI.WPF.Tests/`, `AUI/` = `SAM_UI/SAM_UI/SAM.Analytical.UI/`, `CORE/` = `SAM/SAM/SAM.Analytical/`,
`TAS/` = `SAM_Tas/SAM_Tas/SAM.Analytical.Tas/`.

## 0. Context

Thermal Performance (Stages A–D) consumes **complete** glazing systems (`ApertureConstruction`) from the model, the default library and
remembered sources (`.tcd`/JSON). IGDB is a **pane** library (v76: 11,664 panes, no systems), so today it contributes no candidates. Stage E0
adds a separate authoring surface — the **Glazing System Builder** — that turns panes + gaps (+ frame) into reusable, persisted, predefined
systems that then appear as ordinary candidates. Boundary: *pane/material sources → Builder → predefined `ApertureConstruction` →
`Change…` candidate → Apply*. The Builder never mutates the analytical model; Apply stays **one Apply → one `SetJSAMObject` → one Undo**.

---

## 1. Current architecture map

```
Sources (read-only pools)                         Thermal Performance panel (WPF/Controls/ThermalPerformanceControl)
  model ConstructionManager ─ GlazingSource.FromModel ─┐   ThermalPerformanceViewModel → ThermalEditSession → ThermalRowEditor (per row)
  default libs ─ GlazingSource.FromDefaultLibrary ─────┤     OpenChange(): new GlazingViewModel(model, current, scope, evaluator, library)
  .tcd/.json ─ ThermalSourceCatalog (D2, remembered    │       + AddReadySources() → glazing.AddSourceAsync(source)
     paths via ActiveManager "ThermalSourcePaths")     │     Candidates = GlazingViewModel.Rows (Guid-first-wins, Model→Library→Loaded)
     └ Query.ReadThermalSource[Async] (STA)            │     AddTo(ThermalChangeSet) ← GlazingViewModel.CreateRequest() (SetGlazingRequest)
        └ GlazingSourceCache (%LOCALAPPDATA%\SAM\cache\tcd)│   ThermalEditSession.Rebuild → Modify.ProposeThermalChange (clone, no Tas)
                                                       │                                 → Query.ThermalCheckDiff (pre-Apply warnings)
Evaluation: IGlazingEvaluator → TasGlazingEvaluator ───┘   Apply → Modify.ApplyThermalChangeWithReports → ProposeThermalChange
  (StaSingleFlightWorker; ThermalTransmittanceCalculator     → SetGlazing core per request (+ Tas Calculate of the chosen system)
   (source.ConstructionManager).CalculateGlazing)            → exactly ONE uIAnalyticalModel.SetJSAMObject(FullModification) → GLAZING CHANGE report
```

Key files: `WPF/Classes/Thermal/{ThermalEditSession,ThermalRowEditor,ThermalChangeSet,ThermalSourceCatalog,MaterialIdentity}.cs`,
`WPF/Classes/Glazing/{GlazingSource,GlazingCandidate,GlazingViewModel,GlazingCandidateRow,GlazingValues,IGlazingEvaluator,TasGlazingEvaluator,GlazingSourceCache}.cs`,
`WPF/Modify/{ApplyThermalChange,SetGlazing}.cs`, `WPF/Query/{GlazingUw,GlazingCheck,GlazingChangeReport,ThermalCheckDiff,ThermalSourceReader,GlazingSourceReader}.cs`,
`WPF/Classes/UValue/StaSingleFlightWorker.cs`.

## 2. Domain findings (verified in code)

### ApertureConstruction (`CORE/Classes/ApertureConstruction.cs`, assembly SAM.Analytical)
- `SAMType`; own fields only `apertureType` (Window/Door/Undefined), `paneConstructionLayers`, `frameConstructionLayers`; everything else
  (DefaultPanelType, Description, **DefaultFrameWidth**, Pane/FrameAdditionalHeatTransfer [%], U/g/LT) is a ParameterSet parameter
  (`ApertureConstructionParameter`). **One object = glazing + frame together.**
- **Layer order is INSIDE → OUTSIDE** ("Order of materials from inside to outside following the TAS approach", lines 15-20; default
  `SIM_EXT_GLZ` lists `_Glazing Inner Pane…` first, `…Outer Pane…` last; `Query.ExternalConstructionLayer` = `.Last()`). Note: the test
  fixture `T/Helpers/GlazingFixture.System(...)` names its *first* layer `outer` — inconsistent with the domain; do not copy that.
- Window vs door identity: `ApertureType`; candidates filter to the current system's type and transparency (`GlazingViewModel.Rebuild`).
- Panel-group compatibility: the `DefaultPanelType` **string parameter** → `Query.PanelType(object)` → `PanelGroup()`. Two writers use
  different forms (`Create.ApertureConstruction` writes `panelType.Text()`, `ConstructionManager.Add` the enum; default library stores
  `"WallExternal"`).
- Guid: copy ctor `(ApertureConstruction)` and `Core.Query.Clone` **keep** the Guid; `(ac, name)` ctor gives a **new** Guid (the de-facto
  "duplicate"); explicit `(Guid, name, type, pane, frame)` ctor deep-copies layers. Layer getters return copies (immutable-ish).
- Serialization: `_type`, `ParameterSets`, `Name`, `Guid`, `PaneConstructionLayers`, `FrameConstructionLayers`, `ApertureType`. A malformed
  Guid is silently replaced by a random one on load (`Core.Query.Guid`).
- `ConstructionLayer` = `{Name, Thickness}` only (no Guid); **a layer references its material by name** in a `MaterialLibrary` (keyed by name).
- `Aperture : SAMInstance<ApertureConstruction>` embeds the whole construction inline in JSON; `new Aperture(aperture, ac)` keeps the
  aperture Guid.

### Panes
- `TransparentMaterial` (SAM.Core). Thermal: fields `ThermalConductivity`, `Density`, `SpecificHeatCapacity`. Optical: parameters
  (`TransparentMaterialParameter`): Solar/Light Transmittance, **External/Internal** Solar & Light Reflectance, **External/Internal Emissivity**,
  IsBlind; `DefaultThickness`; VapourDiffusionFactor.
- IGDB → `TAS/Classes/TCDMaterialData.ToSAM` → `Create.TransparentMaterial`; 19 COM reads per material; kept: name (unique-ised "name n"
  in load order), `DisplayName` = original TCD name, `Description`, `Category` = TCD folder path, thickness (`width`), all optical/thermal
  values. **No IGDB ID, manufacturer or product field.** Guid random per conversion (stable only via the JSON cache).
- Imported panes are ordinary materials (mutable objects, cloned by `SAMLibrary` getters); nothing marks them as reference data.

### Gaps / cavities
- **A gap is an ordinary `ConstructionLayer` whose named material is a `GasMaterial`** (no cavity type). Thickness = layer thickness.
- `GasMaterial`: λ, ρ, cp, `DynamicViscosity`; parameters `HeatTransferCoefficient` (convective conductance, EDSL "Parameters for gas
  layers") and `DefaultGasType` (Air/Argon/Krypton/Xenon/SF6). TCD receives conductivity/density/cp/viscosity/`convectionCoefficient = HTC`;
  **gas type is not written** (`TAS/Modify/UpdateMaterial.cs:127-147`).
- HTC per width/tilt: `CORE/Query/HeatTransferCoefficient(FluidMaterial, width, angle)` (EN 673 Nusselt, ΔT 15 K, Tm 283 K) — needs
  viscosity + density, which the **default gas library** (`SAM_GasMaterialLibrary.JSON`, "Default Argon Gas" etc.) has; the main material
  library's gases have hard-coded HTC and no viscosity. Naming precedent: `Query.UpdateHeatTransferCoefficients` creates
  `"{Gas}_{mm}mm_{HTC}W/m2K_{deg}deg"` gas materials.
- Derived gas factory `Create.GasMaterial(gas, name, …, thickness, htc)` has a bug: sets `DefaultGasType` on the **source**, not the result
  (`CORE/Create/GasMaterial.cs:37-40`) → the Builder must set it on the result itself.

### Frames
- Frame = `FrameConstructionLayers` (opaque materials) + optional `DefaultFrameWidth` parameter. **No Uf, no Ψ, no frame-width field.**
- `Aperture.GetFrameArea/GetPaneArea`: geometry holes if present, else `DefaultFrameWidth`, else **the summed frame-layer *thickness* used as the
  face width** (`Aperture.cs:165`). Default `SIM_EXT_GLZ` has no `DefaultFrameWidth` → its 0.05 m frame layer depth becomes a 50 mm frame
  width. `Query.Area(Aperture,…)` uses `DefaultFrameWidth` only (inconsistent).
- Uf is **derived**: Tas 1-D U of the frame layers as an opaque construction (`" -frame"`, horizontal external). Not ISO 10077-2.
- Existing workflows obtain frame data only by copying an existing system (TCD-imported systems are frameless).
- Conclusion: frame information does belong to `ApertureConstruction` in SAM; it is just thinly modelled.

## 3. Performance provenance (do not let the UI mislabel these)

| Quantity | Meaning (as implemented) | Stored where | Calculated where | Geometry? | Tas? | Class |
|---|---|---|---|---|---|---|
| **Ug** | Tas TCD transparent-construction U (`GetUValue()[6]`, centre of pane, Tas conditions; heat-flow ignored) | `ApertureParameter.ThermalTransmittance` ("UValue") **on each Aperture** (written by `SetGlazing` / `Tas.Modify.UpdateThermalParameters`); `GlazingValues.Ug` in memory | `ThermalTransmittanceCalculator.CalculateGlazing` | No | Yes | authoritative-on-Apply stored; derived for drafts |
| Aperture "U-value" | **= Ug** for windows; whole-door U for opaque doors (D0 finding) | same parameter | same | No | Yes | stored (label must say Ug) |
| **Uw** | `(Ug·Ap + Uf·Af)/(Ap+Af)` per aperture; **no Ψg spacer term**; frameless → Ug; no geometry → labelled 80/20 approx | **not stored** | `WPF/Query/GlazingUw.cs` (on demand) | **Yes** | via Ug/Uf | derived, approximate (no Ψ; frame width may be a thickness proxy) |
| **Uf** | Tas 1-D U of frame layers (horizontal external) | not stored (`GlazingValues.Uf`, NaN if frameless) | `CalculateGlazing` ("-frame") | No | Yes | derived, approximate (not ISO 10077-2) |
| **g** | Tas total solar energy transmittance (`GetGlazingValues()[5]`), normal incidence | `ApertureParameter.TotalSolarEnergyTransmittance` on Aperture | `CalculateGlazing` | No | Yes | stored after Apply; derived for drafts |
| **LT** | Tas light transmittance (`GetGlazingValues()[0]`) | `ApertureParameter.LightTransmittance` on Aperture | same | No | Yes | stored after Apply; derived for drafts |
| Pane τsol/ρ/ε/λ | per-material inputs | `TransparentMaterial` parameters/fields | input data | No | No | authoritative input |
| Gap HTC | convective conductance of the gas layer | `GasMaterialParameter.HeatTransferCoefficient` | `Query.HeatTransferCoefficient` (EN 673) or hard-coded | No (tilt yes) | No | derived input |
| `ApertureConstructionParameter` U/g/LT | exist on the construction | written only by SAM_OpenStudio tests | — | — | — | **unused; Builder must NOT write them** (ambiguous Ug/Uw, possibly read by OpenStudio export) |
| SAM core `Glazing.cs` (EN 410 g/LT) | alternative non-Tas g/LT | — | Grasshopper only | No | No | not used in E0 (possible cross-check in tests) |

Rules for the Builder UI: show **"Ug (centre of pane, Tas)"**, **"Uf (frame layers, 1-D, Tas)"**, **"g"**, **"LT"**; any Uw must be
**"Uw, reference window 1.23 × 1.48 m, frame width X mm, no spacer Ψ"** and never presented as the value the model will get (the model's Uw is
computed per aperture at `Change…`/Apply by `GlazingUw`).

## 4. Tas calculation path — can a draft be evaluated before it is in the model? **Yes.**

- `ThermalTransmittanceCalculator(ConstructionManager)` looks systems up by Guid **in the given manager only**; materials come from its
  `MaterialLibrary`. Each call creates a throw-away `%TEMP%\SAM\<guid>.tcd`, converts the system (`ToTCD_Constructions` → "-pane"/"-frame"),
  calls TCD COM, deletes the file. No TBD, no model.
- Minimum transient representation: `new ConstructionManager(new[]{ draftApertureConstruction }, null, materialLibraryWithAllItsMaterials)`
  wrapped as a `GlazingSource` → `GlazingEvaluationBatch` → existing `TasGlazingEvaluator`. This is exactly how sources are evaluated today.
- Threading: TCD is an out-of-process COM server; callers use dedicated STA threads (`StaSingleFlightWorker`: newest submission supersedes
  the pending one; running call is not interruptible). ~0.3 s for one system.
- Hazards to guard in the Builder (not fix in Tas): a layer whose material is missing becomes a **blank TCD layer silently**
  (`Convert/ToTCD/Constructions.cs:66-72`) → compose must refuse missing materials; calculator **swallows exceptions** → empty result must be
  treated as failure; `values.Length >= 6` guard reading `[6]` (latent off-by-one).
- Needs an aperture in a model only for: area-weighted Uw (`GlazingUw`), host-panel checks, and writing Aperture parameters (Apply).

## 5. Source / IGDB findings

- `ThermalSourceCatalog` (D2) entries are **file-backed, read-only, remembered by path**, cannot be refreshed after the file changes
  (re-adding returns the existing entry) and have no in-memory entries. A pane-only file's source keeps its **entire `MaterialLibrary`**
  (`ThermalSourceReader.cs:59-84`), so an IGDB file added with `Add source…` is already a usable pane library.
- IGDB files on this machine (Tas Data `Databases` folder): `International Glazing Database_v38/_v59/_v63/_v69/_v76.tcd`.
  Cold import ~63-67 s (COM), warm from JSON cache ~0.5 s. Exact duplicates collapse (`UniqueId` over 19 properties); same name/different
  data → "name 2". **Across versions** the same product name gets the same SAM name with possibly different values and a different Guid →
  mixing versions clashes by name (blocked today by `MaterialIdentity.Same`).
- Candidate precedence: Guid first-wins over sources in list order (Model → Library → Loaded); material clash with the model blocks the
  candidate (`GlazingCandidate` / `MaterialIdentity`).
- Reuse for the Builder: the reader + cache + catalog (as **pane** providers), `MaterialIdentity`, `GlazingSource`, `TasGlazingEvaluator`,
  `GlazingCandidate` (materials-to-add logic). Keep candidate-selection-only: `GlazingViewModel` filters/target/scope/rows, `ThermalChangeSet`.

## 6. Proposed architecture

### 6.1 Draft model — recommend a new WPF-free `GlazingSystemDraft` (not `ApertureConstruction` as the draft)

| | `GlazingSystemDraft` (recommended) | `ApertureConstruction` as draft |
|---|---|---|
| Mutation safety | plain mutable VM-owned object, nothing shared | layer lists are copies; every edit rebuilds the object; materials in a parallel library keyed by name — easy to desync |
| Gap semantics | `Gap{GasType, Thickness}`; gas material derived on compose (HTC from width + tilt) | gap = arbitrary material name; must pre-create a gas material per thickness on every edit |
| Provenance per pane | carried on `DraftPane{Material snapshot, SourceLabel, SourcePath, DisplayName, Category, Reversed}` | nowhere to put it per layer |
| Temporary invalid states | natural (Pane→Pane→Gap is just a list) + explicit validation | possible but every invalid state is a "real" construction |
| Order | presented **outside → inside** (user mental model); composer reverses to SAM's inside→outside in ONE place | caller must remember the reversal everywhere |
| Undo | none against the model; optional in-Builder undo later | same |
| Serialization | **not persisted**; only the composed result is | — |
| Conversion | `Query.ComposeGlazingSystem(draft) → GlazingComposition{ApertureConstruction, MaterialLibrary (only its materials), Issues}` | — |
| Testability | pure unit tests on draft + composer | — |

Draft content: `Name`, `IntendedPanelType` (→ `DefaultPanelType`), ordered `Layers` (outside→inside) of `DraftPane | DraftGap`,
`DraftFrame{ None | CopiedFrom(system: frame layers + their materials + Frame/PaneAdditionalHeatTransfer), FrameWidth }`,
`BasedOn` (name + Guid of a seed system, for provenance).

### 6.2 New components (all in `WPF/`, no SAM core / SAM_Tas changes in E0)

| Component | Responsibility |
|---|---|
| `Classes/GlazingBuilder/GlazingSystemDraft.cs` (+ `DraftPane`, `DraftGap`, `DraftFrame`) | editable draft |
| `Query/ComposeGlazingSystem.cs` | draft → `ApertureConstruction` (new Guid per *save*, stable Guid per draft session for evaluation) + materials; gas material = default gas (Air/Argon/Krypton) + `Query.HeatTransferCoefficient(width, tilt from intended panel group)`, named per SAM's `UpdateHeatTransferCoefficients` convention so identical gaps dedupe; sets `DefaultGasType` on the result; writes `DefaultFrameWidth` explicitly; never writes `ApertureConstructionParameter` U/g/LT |
| `Query/GlazingDraftCheck.cs` | authoring validation (§9): wraps SAM `Create.Log(ac)`, `Create.Log(ac, lib)`, `Create.Log(IMaterial)` on the composed result + Builder structural rules |
| `Classes/GlazingBuilder/GlazingValuesCache.cs` | content-keyed Ug/g/LT/Uf cache (SHA-256 of pane+frame layer thickness + `MaterialIdentity.Json` + additional-heat-transfer), like `ConstructionUValueCache` |
| `Classes/GlazingBuilder/DraftGlazingEvaluator.cs` | wraps `IGlazingEvaluator`: debounce 300-400 ms, generation token, stale-drop, cache lookup |
| `Classes/GlazingBuilder/PaneCatalog.cs` | lightweight `PaneEntry` projection per pane source (name, display name, category, thickness, τsol, LT, εext, εint, source label/path), built once per source instance off the UI thread |
| `Classes/GlazingBuilder/UserGlazingLibrary.cs` + `IUserGlazingLibraryStore` + `FileUserGlazingLibraryStore` | persisted predefined systems (§12), `Source` snapshot (`GlazingSource`, kind `User`), `SaveAsync`, `Changed` event |
| `Classes/GlazingBuilder/GlazingBuilderViewModel.cs` | WPF-free Builder VM (layers, selection, validation, performance, Save/Cancel) |
| `Windows/GlazingSystemBuilderWindow.xaml(.cs)` | the host (§10) |
| `Enums/GlazingSourceKind.cs` | + `User` ("My glazing systems") |
| `ThermalEditServices` | + `UserGlazing` lazy factory (tests inject an in-memory store) |

## 7. User journey

1. Window row → `Change…` → candidate list (existing).
2. `Create new…` (header of the open list, next to Cancel) → Builder opens **seeded** from the selected candidate (or the current system):
   name "`<seed> (copy)`", layers + frame copied, status "New · based on SIM_EXT_GLZ · not saved". (Seeding covers "duplicate & edit"
   without ever editing a saved definition.)
3. User replaces/adds panes (pane browser over pane sources), adjusts gaps (gas + thickness), frame (copy from a system / none / width).
4. Performance block updates ~0.3-0.7 s after edits settle (Tas, async, stale results dropped); validation list updates instantly.
5. `Save as predefined` → user library file written atomically → Builder closes.
6. `UserGlazingLibrary.Changed` → every open `Change…` list adds the new snapshot source (`glazing.AddSourceAsync`) → new candidate appears,
   evaluated by the existing evaluator (one Tas call, cache hit possible).
7. The originating list selects it (`SelectedGuid = new`) and it is always shown (exempt from the target filter, like the current row);
   preview + `ThermalCheckDiff` run on clones as today. **Model untouched.**
8. Apply → existing `ThermalChangeSet` → `SetGlazing` (adds the system with its Guid + only missing materials) → one `SetJSAMObject` →
   GLAZING CHANGE report (extended, §13) → colour-by refreshes via the existing `UpdateTabItem` path. One Undo removes everything.
Cancel at any point: nothing written anywhere.

## 8. Mutation matrix (implementation invariant; each row gets a test)

| User action | Active model mutated? | User library mutated? | Undo entry? |
|---|---|---|---|
| Open Builder (Create new…) | No | No | No |
| Load / add pane source (IGDB) | No | No (catalog path setting only, existing D2 behaviour) | No |
| Search / select pane | No | No | No |
| Add / remove / reorder / replace layer, change gap gas/thickness, frame | No | No | No |
| Performance preview (Tas) | No (temp TCD only) | No | No |
| Save as predefined | **No** | **Yes** (append one system + missing materials) | No (library has no model Undo) |
| Cancel Builder | No | No | No |
| Candidate list refresh / select new candidate / preview & check | No (clones only) | No | No |
| Thermal Apply | **Yes, once** (`SetJSAMObject` ×1) | No | **Yes, one** |
| Thermal Undo | Restores previous model (system + added materials gone) | No (the predefined system stays in the library) | consumes one |

## 9. Validation ownership (no rule at two layers)

| Rule (source) | Layer | Owner |
|---|---|---|
| No pane layers; layer without name; thickness ≤ 0 (`Create.Log(ac)`) | authoring | `GlazingDraftCheck` (runs SAM rule on composed result) |
| First/last pane layer is gas; material missing in library; gas not recognised (`Create.Log(ac, lib)`) | authoring | `GlazingDraftCheck` |
| Material properties NaN (`Create.Log(IMaterial)`) | authoring (per pane, shown on the layer) | `GlazingDraftCheck` |
| **New**: Gap→Gap adjacent; Pane→Pane adjacent (warning: "in contact, no cavity"); opaque material in pane stack; gap width outside 4-30 mm (EN 673 validity, warning); > 4 panes (warning); no intended use; frame layers without width | authoring | `GlazingDraftCheck` |
| Empty / duplicate name **within the user library** | authoring (Save) | `GlazingDraftCheck` + `UserGlazingLibrary` |
| "No Frame ConstructionLayers" (frameless) | authoring = info; candidate = existing frameless warning when replacing a framed system | Builder shows info only; `GlazingViewModel.RowWarnings` unchanged |
| Calculation failure / Tas unavailable | status (not validation) | Builder performance block; candidate row (existing) |
| Panel-group mismatch vs host panels | candidate compatibility | `GlazingViewModel.RowWarnings` (existing) — Builder only sets intended use, defaulting from the row's host group |
| Material differs from / missing in model (blocks Apply) | candidate compatibility | `GlazingCandidate` / `MaterialIdentity` (existing) |
| New warnings the change would add (host panel type, panel group, frame) | pre-Apply | `ThermalCheckDiff` (existing) |
| Model name clash → `<name> 2` | Apply | `SetGlazing` (existing) |
| Full model rules | model-level | Edit > ModelCheck (existing) |
| Source clash (same Guid in several sources) | candidate pool | Guid first-wins (existing) |

## 10. UI proposal

### Host — recommend an owned **modal** window (`ShowDialog`, owner = `Window.GetWindow(panel)`)
- Modal removes a whole class of hazards for E0: the model cannot change and the originating row editor cannot close/invalidate while
  authoring; single instance; consistent with `SetGlazingWindow` / `ApertureConstructionWindow`. The Builder needs no 3D interaction.
- Size ~1100 × 760, resizable, remembered bounds (reuse `Query.ThermalFloatingBounds` for monitor clamping).
- Builder VM is host-independent, so moving to modeless later is cheap (then the `Changed`-event refresh already supports it).
- Drawer/docked options rejected: the panel is ~330 px wide, too small for a pane browser + stack + performance.

### Wireframe (necessary ■ / optional □)
```
Glazing System Builder                                                     [_][□][x]
 Name [ Double low-e argon 16 ]          Intended for [ External wall windows ▾ ]  ■
 New · based on SIM_EXT_GLZ · not saved                                           ■
┌ Build-up (outside → inside) ───────────────┐┌ Panes ─ Source [IGDB v76 ▾][Add source…] ■
│ OUTSIDE                                    ││ Search [ planibel           ]        ■
│ ▣ Pane 1  Planibel Clear 6 mm  IGDB v76    ││ Thickness [any▾] □  LT ≥ [  ] □ g/τ ≥ [ ] □
│    τsol 0.80 LT 0.89 ε 0.84/0.84  [Replace]││ Name            Cat.  mm  τsol  LT  εo/εi ■
│ ▢ Gap 1   [Argon ▾] [16] mm  HTC 1.38       ││ …virtualised list (11,664)…             ■
│ ▣ Pane 2  Planitherm 4 mm  ε 0.84/0.03     ││                  [Use as Pane 2] [Insert] ■
│ INSIDE                                     │└──────────────────────────────────────────┘
│ [+ Pane] [+ Gap] [Remove] [↑] [↓]  □[Reverse]│┌ Performance (Tas) ──────────────────────┐
├ Frame ─────────────────────────────────────┤│ Ug 1.12 W/m²K (centre of pane)          ■
│ (•) Copy frame of [SIM_EXT_GLZ ▾] ( ) None  ││ g 0.62   LT 0.80                          ■
│ Width [50] mm (was inferred from depth)    ││ Uf 2.20 (frame layers, 1-D)               ■
└────────────────────────────────────────────┘│ Uw ref 1.30 (1.23×1.48 m, no Ψ)           □
 Validation: ✓ ready / ⚠ 1 warning / ✗ 2 errors (list, click → layer)     ■ └───────────┘
                                         [Cancel]   [Save as predefined]          ■
```
Accessibility: AutomationProperties.Name on every control and layer row; lists with Tab navigation `Continue`; **no collapsed `Expander`
inside templates** (Stage C finding); keyboard reorder (Alt+↑/↓); glyph + words for status.

### Pane browser (minimum useful)
- Source combo = ready catalog entries containing `TransparentMaterial`s + "SAM default library" + "This model's panes"; `Add source…`
  reuses `ThermalSourceCatalog.AddAsync` (remembered, shared with the panel).
- One search box (substring over name, display name, category/folder — the closest thing to manufacturer), debounce 150 ms, virtualised
  ListView, sortable columns (name, mm, τsol, LT, εext/εint). Optional: thickness filter. Deferred: favourites/recent, faceted
  manufacturer list, spectral data. Candidate-list components are not reusable (different row type); reuse the `PartOStyles` grid styling.

## 11. Cache / threading design

| Operation | Cost (measured / estimated) | Where | Cache |
|---|---|---|---|
| IGDB first import | 63-67 s full v76 (COM, ~5.4 ms/material); Pilkington subset (162 KB vs 1.67 MB) est. ~6-8 s — measure in the probe | catalog STA reader (existing) | `GlazingSourceCache` JSON (existing) |
| IGDB warm load | ~0.5 s | same | same + catalog keeps the source in memory |
| `PaneEntry` projection (11,664) | one `GetMaterials` (clones once) ~0.1-0.5 s est. | background task | per `GlazingSource` instance (ConditionalWeakTable) |
| Search filter | < 20 ms est. | UI thread, debounced 150 ms | none |
| Compose + validate draft | ms | UI thread (pure) | none |
| Tas evaluation of a draft | ~0.3 s | `TasGlazingEvaluator` STA worker via `DraftGlazingEvaluator` | `GlazingValuesCache` content-keyed (session) |
| Candidate list after save | 1 Tas call for the new Guid | existing `GlazingViewModel.EvaluateAsync` | optional: pre-seed from `GlazingValuesCache` |
| Save library | small JSON write | background, atomic | — |

Rules: debounce 300-400 ms after the last edit; each request carries a draft **content key** + generation; results whose key ≠ current
draft are dropped (the single-flight worker already supersedes the pending item; a running item finishes and is discarded); structural
errors → no Tas call; performance block shows "Calculating…" with the last values greyed; failure shows "Not calculated: <reason>" (Tas
missing, empty result). Builder owns its own `TasGlazingEvaluator` instance (disposed on close) so a Builder burst never queues behind the
panel's list.

## 12. Persistence, save and edit semantics

### Options
| | A. Append to SAM default libraries (`Documents\SAM\<resources>\Analytical\SAM_ApertureConstructionLibrary.JSON`) | **B. Dedicated user glazing library file (recommended)** | C. File registered as a D2 source |
|---|---|---|---|
| Compatibility | same format | `ConstructionManager` JSON = format SAM already reads everywhere (Add source, Edit > Aperture Constructions import, cache) | same as B |
| Versioning | none; **installer overwrites resources** | schema version in each system's provenance set; file Name marker | none |
| Portability | poor (machine resources) | copy one file; also loadable on another machine via `Add source…` | same file |
| Provenance | none | per-system ParameterSet | label = file name only |
| Editing | mixes shipped + user data | append-only via the service | catalog is read-only, can't refresh after write |
| Duplicates | Guid-keyed lib allows dup names | service enforces unique names, Guid identity | Guid first-wins |
| Machine/Git friendliness | bad | `Documents\SAM\User Libraries\` — user-visible, backed up, diffable | ok |
| Migration risk | high | none (new file) | low |
| Complexity | low but wrong | medium | low, but needs catalog refresh/in-memory APIs + "Forget" would hide user systems |

**Recommendation: B**, stored as a `ConstructionManager` JSON (`ApertureConstructionLibrary` + `MaterialLibrary` with every material its
systems use) at `Documents\SAM\User Libraries\Glazing Systems.json` (`Core.Query.UserSAMDirectory()`); path overridable later via a
setting. Not in `ActiveManager` user settings (settings hold scalars/paths; also `Manager.Write` currently writes the whole settings list into
every per-name file). Writes: re-read → merge by Guid → write temp → replace, keep `Glazing Systems.json.bak`; **never overwrite a file that
failed to parse** (library shown as unavailable with the reason).

### Save as predefined
- **Guid**: new `Guid.NewGuid()` at save (the draft's evaluation Guid is discarded). Saved definitions are **immutable**: never re-saved
  under the same Guid.
- **Name**: required; unique within the user library (case-insensitive, trimmed) → block Save with "A system named X already exists"
  (suggest "X 2"). A clash with the model or default library is allowed (Apply's existing `<name> 2` rule handles the model).
- **ApertureType** Window; `DefaultPanelType` from Intended use (enum-name form as in the default library; round-trip tested through
  `Query.PanelType`); `Description` = human-readable build-up; `DefaultFrameWidth` explicit when framed.
- **Materials are embedded** (copied into the library's `MaterialLibrary`), so a system is reproducible after the source file moves and
  Tas can calculate it (`SetGlazing` calculates from `request.Source.ConstructionManager`). Pane names kept; if the library already holds a
  *different* material of that name (e.g. IGDB v69 vs v76) the incoming pane is saved as `"<name> (<source label>)"` (then " 2"); identical
  content reuses the existing entry (`MaterialIdentity.Same`). Gas materials use the deterministic SAM naming → identical gaps dedupe.
- **Provenance**: a named ParameterSet `"SAM Glazing System Builder"` (fixed name, fixed Guid constant) on the ApertureConstruction:
  schema version, created (UTC), based-on name+Guid, per-pane `{source label, source path, original display name, category, reversed}`,
  gap `{gas, mm, HTC, tilt}`, performance snapshot `{Ug, g, LT, Uf, engine "Tas TCD", SAM_Tas version}`. It travels into a model on Apply
  (generic ParameterSets: no schema change); must be verified against the PR2A-0 one-set-per-name merge rule.
- **Timing**: written when the user clicks Save (before the window closes); failure keeps the Builder open with the error.
- Saving never touches `uIAnalyticalModel` (asserted by test: model JSON + history unchanged).

### Edit predefined — recommend: **no in-place edit in E0; "Create new…" seeded from any system = clone → edit → save as new.**
In-place edits would change a definition whose copies already live (by Guid) in saved models, break reproducibility of reports, and make
Guid-first-wins candidate identity ambiguous. Removing a system from the user library is an owner decision (§16).

## 13. Reporting, colour-by, saved-model compatibility

- **Report** (`WPF/Query/GlazingChangeReport.cs`) already has: model, applied time, method, glazing old→new (+added/already), pane
  build-up, frame build-up, Ug, Uf (none if frameless), g, LT, Uw (+basis, target), scope, materials added, CHECK block.
  **Missing**: system Guid, source label/path ("My glazing systems — Documents\SAM\User Libraries\Glazing Systems.json"), Builder provenance
  ("Built 2026-.. from IGDB v76: A / Argon 16 mm / B; based on SIM_EXT_GLZ"), frame width used. E0 adds three lines `Guid`, `Source`,
  `Built from` (read from `SetGlazingRequest.Source` + the provenance set). Do not build the change log (Stage E later).
- **Colour-by**: no extra work. `SetGlazing` writes Aperture U/g/LT from a Tas `Calculate` of the chosen system; `FullModification`
  forces `UpdateTabItem` → `RenderViewSettings` re-colours; Undo the same (`AnalyticalWindow.xaml.cs:3589-3712`). Acceptance re-verifies.
- **Saved models**: zero migration. `.sam` gains only an ordinary `ApertureConstruction` (+ an extra named ParameterSet) and ordinary
  materials after an Apply. No change to `ApertureConstruction`, material classes, default libraries, D2 source paths or cache format.
  New file only: the user library. Explicit non-change: no `ApertureConstructionParameter` U/g/LT written.

## 14. Legacy parity matrix (input to E1; nothing retired in E0)

| Existing capability | Classic workflow | New Builder (E0) | Gap? |
|---|---|---|---|
| Create aperture construction from materials | Edit > Aperture Constructions > Add → `ApertureConstructionWindow` + `MaterialLayersControl` (pane & frame lists, thickness, up/down) | panes + gaps, outside→inside view | Builder doesn't author frame layers from materials (copy/none only) |
| Duplicate system (new Guid) | Library window Duplicate | Create new… seeded from any system | none |
| Edit system in place | `ApertureConstructionWindow` (keeps Guid) on a library **copy**, OK → `JSAMObject =` (extra Undo step) | not offered (by design) | intentional |
| Pick/replace pane from big DB | none (MaterialLibrary picker only for materials already in the library) | pane browser over IGDB/tcd/default/model | Builder better |
| Gas gap with correct HTC per width | none (pick a pre-made gas material) | derived from gas + width + tilt | Builder better |
| Performance preview while authoring | none | Ug/g/LT/Uf (+ref Uw) via Tas | Builder better |
| Import/export library `.json`/`.tcd` | Library window Import/Export | user library file is ConstructionManager JSON (portable via Add source) | no export/import UI in E0 |
| Create without assigning | Library window; Set glazing "Don't assign" | Save as predefined (library, not model) | different target (library vs model) |
| Rank existing systems by g/LT and assign | Glazing Calculator (classic) | candidate list (C/D) | covered by Change…, not Builder |
| Material editing (new pane data by hand) | MaterialLibraryWindow | not offered | gap (keep classic) |
| Doors / opaque apertures | ApertureConstructionWindow | windows only | gap |

## 15. Test plan (pyramid)

| Level | Tests (examples) | Tas? |
|---|---|---|
| Pure domain | compose double/triple/arbitrary; **outside→inside UI order maps to inside→outside layers**; gap material name/HTC/DefaultGasType on result; HTC changes with width and tilt; DefaultFrameWidth written; no U/g/LT construction parameters; missing material refused; validation of Pane→Pane, Gap→Gap, gas at edge, opaque in stack, widths; Guid new on save / stable per draft; provenance set round-trip incl. PR2A-0 merge; JSON round-trip of the library; name uniqueness; material rename on clash / reuse on identical; corrupt file never overwritten; concurrent-writer merge by Guid; .bak | No |
| Evaluator | fake evaluator: debounce, generation drop, cache hit by content (Guid differs, content same), failure/empty → "Not calculated"; reference-Uw arithmetic | No (fakes) |
| Real Tas probe (opt-in, `[Trait("Tas")]` or harness) | on panes from **`International Glazing Database_v76-Pilkington.tcd`**: orientation — asymmetric low-e unit (Pilkington clear + low-e) evaluated in both orders + reversed material → document which order reproduces Pilkington's published Ug/g/LT; Ug double ≈ 1.1-1.3, triple ≈ 0.6-0.8 with argon/low-e; HTC sensitivity; converted-vs-imported route parity (same panes, same values) | **Yes** |
| Sources | IGDB-like pane source (synthetic fixture with N transparent materials — **no manufacturer data committed** unless the owner approves a trimmed Pilkington fixture) → PaneEntry projection; `.tcd` cache shared; JSON source; remembered source pending→ready feeds the browser; user library file readable by `ReadThermalSource` | No (fake reader) |
| View-model | add/remove/replace/move layer; invalid sequences allowed + listed; Save disabled with errors; Save writes once; Cancel writes nothing; seeded from candidate; frame copy/none/width | No |
| Thermal integration | Save → open `Change…` list gains the system, selected, shown despite target filter; **model JSON + history unchanged after open/load/preview/save/cancel**; Apply = one Modified + one history step; only system + missing materials added; report has Guid/Source/Built from; Undo leaves user library intact | No (fake evaluator + real `SetGlazing` core) |
| Real WPF (STA, `WpfCollection`) | Builder window controls/automation names, keyboard reorder, smallest-size layout; `Create new…` button in the open list; refresh after save | No |
| Real-app acceptance | §16 journey with real Tas + IGDB | **Yes** |

Estimated new tests: E0-1 ~45, E0-2 ~20, E0-3 ~35. Full suite must stay green (baseline 1996).

## 16. Real-app acceptance plan (E0-3 gate; driver + evidence in a local, uncommitted folder)

App: copy of `SAM_UI\build`; model: fresh copy of the representative model (12 walls U 0.26, 20 windows `SIM_EXT_GLZ` U 1.243 / g 0.40 /
LT 0.804); real Tas. **Pane source (owner-specified test library):**
`International Glazing Database_v76-Pilkington.tcd` (Tas Data `Databases` folder) (162 KB, Pilkington subset of IGDB v76,
created 2 Oct 2026). Exercise **both** routes: (a) `Add source…` in the Builder/panel (SAM_Tas converter + `GlazingSourceCache`; record
cold/warm times and the pane count) and (b) convert/import into SAM (`Tas.Convert.ToSAM_ConstructionManager` → ConstructionManager JSON,
or Edit > Aperture Constructions > Import `.tcd`) and add the resulting `.json` as a source — both must give the same panes. Pick
Pilkington products for the build-ups (e.g. an Optifloat clear outer pane and an Optitherm/K Glass low-e inner pane, as listed in the file).
Scale check only (optional): the full `International Glazing Database_v76.tcd` (11,664 panes) for browser/search responsiveness.
1. Baseline: save model copy SHA; count aperture constructions (1) and materials (18); user library file absent.
2. Select one window → Thermal Performance → `Change…` → `Create new…` → Builder seeded "SIM_EXT_GLZ (copy)"; Undo disabled.
3. Pane source `International Glazing Database_v76-Pilkington.tcd` (Add source… if not remembered); search; replace outer and inner panes (one low-e); Argon 16 mm; frame copied, width
   50 mm → record Ug/g/LT/Uf and time to values; name "E0 Double". Save → file exists, 1 system, materials embedded, provenance present.
4. `Create new…` again → triple (pane-gap-pane-gap-pane, Krypton or Argon 12) → "E0 Triple" → Save. List shows both under "My glazing
   systems", the new one selected; **Undo still disabled; save-as model SHA = baseline**.
5. Close app; restart; open model; `Change…` → both systems listed (persistence), values recomputed equal to the snapshots (±0.005).
6. Choose "E0 Triple" → scope "Only the 1 selected" → preview/check → Apply: result line, report saved with Guid/Source/Built from;
   model: 2 aperture constructions, 18 + k materials (k = the triple's missing materials only).
7. Colour by g-value and LT: the applied window recolours with the new values (legend shows both values); colour by window U shows its Ug.
8. Full ModelCheck before/after (no new errors; panel-group warnings only if intended-use mismatched).
9. One Undo: window back to `SIM_EXT_GLZ`, 1 aperture construction, 18 materials, saved SHA = baseline; **user library still has 2 systems**.
10. Cancel path: Create new → edit → Cancel → library unchanged (file hash), model unchanged.
Evidence: screenshots, driver logs, timings, library file, reports, SHA table.

## 17. PR decomposition (by actual dependencies; each mergeable on its own, nothing user-visible until E0-3)

### E0-1 — Draft, composition, validation, evaluation, user library (no UI)
- Step 1 (gate, before product code): **real-Tas orientation/gap probe** (scratch harness, documented in the PR record) using panes from
  `International Glazing Database_v76-Pilkington.tcd` (Tas Data `Databases` folder), converted to SAM
  (`Tas.Convert.ToSAM_ConstructionManager` → JSON) and also via the SAM import route: layer order, external/internal side semantics, HTC
  effect, max layers, pane count / cold-warm conversion time of the subset. Decides whether `Reverse` is safe for E0.
- Purpose: §6.2 components except the window/VM; `GlazingSourceKind.User`.
- Touches: `WPF/Classes/GlazingBuilder/*` (new), `WPF/Query/{ComposeGlazingSystem,GlazingDraftCheck}.cs` (new), `WPF/Enums/GlazingSourceKind.cs`;
  tests `T/GlazingBuilder*Tests.cs`, `T/Helpers/BuilderFixture.cs`.
- Depends on: nothing new. Tests: pure domain, evaluator fakes, library store; opt-in Tas probe. Gate: full suite green + probe record.
- Rollback risk: very low (unreferenced code).

### E0-2 — Predefined systems as candidates (integration, read path + report)
- Purpose: `ThermalEditServices.UserGlazing`; `ThermalRowEditor.OpenChange` adds the user source after the library and follows `Changed`;
  `GlazingCandidateRow` source label "My glazing systems"; newly-created exemption from the target filter + select hook
  (`GlazingViewModel`); report lines Guid/Source/Built from (`GlazingChangeReport`, `SetGlazingResult` carries source label/provenance).
- Touches: `WPF/Classes/Thermal/{ThermalEditSession,ThermalRowEditor}.cs`, `WPF/Classes/Glazing/{GlazingViewModel,GlazingCandidateRow}.cs`,
  `WPF/Query/GlazingChangeReport.cs`, `WPF/Modify/SetGlazing.cs` (result fields only — no behaviour change), tests.
- Depends on: E0-1. Tests: integration (no model mutation, one Undo, report), source precedence. Gate: suite green + **real-app mini
  acceptance with a hand-written library file** (candidate appears, Apply, report, colour, Undo). Highest-risk area isolated in a small PR.
- Rollback risk: low-medium (touches the Apply path's inputs; guarded by existing Apply tests).

### E0-3 — Builder UI + `Create new…`
- Purpose: `GlazingBuilderViewModel`, `GlazingSystemBuilderWindow`, pane browser, `Create new…` in `ThermalPerformanceControl.xaml`
  (open-list header), select-after-save; full §16 acceptance; record `documentation/Thermal-StageE0.md`.
- Touches: `WPF/Classes/GlazingBuilder/GlazingBuilderViewModel.cs`, `WPF/Windows/GlazingSystemBuilderWindow.xaml(.cs)`,
  `WPF/Controls/ThermalPerformanceControl.xaml(.cs)` (+ `OpenBuilder` injectable like `PickSourceFile`), tests incl. STA.
- Depends on: E0-1, E0-2. Gate: suite green, STA tests, full real-app acceptance. Rollback risk: low (remove the button).

## 18. Risk register

| Risk | L | I | Mitigation |
|---|---|---|---|
| Pane orientation / coating side semantics wrong (inside→outside order; External/Internal ε) | M | H | E0-1 probe gate; one reversal point in composer; test asymmetric unit; `Reverse` only if probe confirms |
| Ug confused with Uw | H | H | labels per §3; reference Uw explicitly labelled; tests on label text (as D0) |
| Frame modelling thin (no Uf input, width from depth fallback, no Ψ) | H | M | copy-frame/none only; explicit width; labels "1-D"/"no Ψ"; Uf-target frame deferred (owner) |
| Serialization compatibility | L | M | no class changes; extra ParameterSet tested vs PR2A-0 merge; library = existing format |
| Material name collisions (IGDB versions, model) | M | M | rename-on-clash at save; existing `MaterialIdentity` block at candidate; report material names |
| Stale Tas results shown for a newer draft | M | M | content key + generation drop; greyed values while calculating |
| Large pane library performance | M | M | catalog JSON cache; one projection per source; virtualised list; debounced search |
| User-library corruption / concurrent instances | L | H | atomic write + .bak; never overwrite unparsable file; re-read-merge by Guid |
| Accidental model mutation | L | H | Builder has no model reference (only seed data); mutation-matrix tests compare model JSON + history |
| Duplicate names | M | L | unique within user library; model clash via existing `<name> 2` |
| Editing saved definitions referenced elsewhere | — | H | no in-place edit (immutable by Guid) |
| Source file moves → irreproducible | M | M | materials embedded; provenance keeps path/label as information only |
| Missing material → blank TCD layer silently | M | H | composer refuses; evaluator treats empty result as failure |
| SAM data defects (Xenon λ ×10, `Ar0UP_Air` 0.12 m, malformed Guid in default lib, `Create.GasMaterial` gas-type bug) | M | M | E0 offers Air/Argon/Krypton only; set gas type on result; report defects separately (not fixed in E0) |
| Gas HTC vs tilt for rooflights | M | L | tilt from intended use; documented |

## 19. Decisions requiring owner approval

1. **Persistence location** — A: `Documents\SAM\User Libraries\Glazing Systems.json` (ConstructionManager JSON). B: `%APPDATA%\SAM\…`
   (hidden, roaming). *Recommend A* (visible, portable, readable as a source elsewhere). Consequence: users can see/copy/delete it.
2. **Edit predefined in E0** — A: no in-place edit; `Create new…` seeded from any system; no delete. B: also "Remove from my library" in
   the list. *Recommend A for E0, B in E1*. Consequence of A: mistakes stay in the list until E1 (can be ignored, not applied).
3. **Frame authoring scope** — A: frame = copy from an existing system or none, editable width. B: also author frame layers from materials
   or synthesise from a target Uf. *Recommend A* (no invented data, as PR3). Consequence: Uf choice limited to existing frames.
4. **Builder host** — A: modal owned window. B: modeless single-instance window. *Recommend A* for E0 (no concurrency with model/row
   changes); B later if users need the 3D view while authoring.
5. **Pane reverse (coating orientation) in E0** — A: include `Reverse` (derived material with swapped External/Internal values, flagged
   in provenance) if the E0-1 probe confirms the semantics. B: defer; users choose panes as listed. *Recommend A conditional on the probe.*
6. **Reference Uw in the Builder** — A: show "Uw ref (1.23 × 1.48 m, no Ψ)". B: show no Uw (Ug/Uf only). *Recommend A*, clearly labelled.
   Layer count: unrestricted sequence with a > 4-pane warning (not an owner decision unless you want a hard cap).

## 20. Stage E0 acceptance criteria

1. From a window row's `Change…`, `Create new…` opens the Builder seeded from the selected/current system; Cancel changes nothing.
2. The Builder authors single/double/triple (arbitrary) pane–gap stacks with panes from `International Glazing Database_v76-Pilkington.tcd`
   (via Add source… and via a SAM-converted/imported JSON of it; other pane sources likewise), gas gaps with
   derived HTC, frame copy/none + explicit width, intended use; validation lists errors/warnings; Save is blocked on errors.
3. Performance shows Ug, g, LT, Uf (and labelled reference Uw) from real Tas within ~1 s of settled edits; UI never blocks; stale results never shown as current.
4. Save writes one immutable system (new Guid, embedded materials, provenance) to the user library atomically; the analytical model
   and its Undo history are unchanged (tested and verified in the real app by SHA).
5. The saved system appears in open and future `Change…` lists (also after restart) as "My glazing systems", selected after creation.
6. Apply uses the existing `ThermalChangeSet`/`SetGlazing` path: one `SetJSAMObject`, one Undo; adds only the system and missing
   materials; GLAZING CHANGE report includes Guid, source and build-up provenance; g/LT/U colouring refreshes; one Undo leaves no orphan
   system/material and keeps the user library.
7. No changes to SAM core, SAM_Tas, `.sam` schema, default libraries or classic tools; full WPF suite green (≥ 1996 + new).
8. Records: `documentation/Thermal-StageE0.md` with evidence table; PROJECT_PROGRESS updated.

## 21. Incidental defects found (out of scope; report separately, not fixed in E0)

- `WPF/Controls/ThermalPerformanceControl.xaml.cs:205,210` contain double-encoded text (`âœ•`, `calculatingâ€¦`) shown to users.
- `SAM.Core/Manager/Manager.cs:300-301` writes the whole settings list into every per-name settings file.
- `CORE/Create/GasMaterial.cs:37-40` sets `DefaultGasType` on the source gas; `CORE/Create/MaterialLibrary.cs:211` argument order.
- Default data: Xenon λ 0.0529 (~10× high); `Ar0UP_Air__12mm` used at 0.12 m; malformed Guid `4d00dd0-…` in the default aperture library.
- `TAS/ThermalTransmittanceCalculator.cs:466-468` length guard `>= 6` reading index 6; ToTCD adds a blank layer for a missing material.

## 22. After approval (housekeeping, no implementation)

Copy this document to `SAM_UI/documentation/plans/Thermal-StageE0-GlazingBuilder-PLAN.md` (local/untracked or committed per owner
choice) and add a Stage E0-planning entry to the local `PROJECT_PROGRESS.md` (untracked by owner decision). Start E0-1 only on explicit
instruction; first step is the real-Tas orientation probe. Remind: commit/push before switching machines.
