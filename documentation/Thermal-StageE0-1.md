# Thermal Stage E0-1 - Glazing System Builder: domain foundation (2 Oct 2026)

First step of Stage E0 (`documentation/plans/Thermal-StageE0-GlazingBuilder-PLAN.md`, approved). **No user-visible change**: no Builder window, no
`Create new…`, no candidate-list integration, no change to Thermal Apply. This PR adds the WPF-free domain the Builder will sit on -

`pane/material sources → GlazingSystemDraft → Query.ComposeGlazingSystem → complete ApertureConstruction (+ only its materials) → My glazing systems`

- and proves the one physical assumption it rests on (layer order and pane orientation) against real Tas first. Nothing here holds or mutates an
analytical model.

## Gate 0 - real Tas orientation / gap probe (before any product code)

Pane source: the Pilkington subset of IGDB v76 (`International Glazing Database_v76-Pilkington.tcd`, Tas Data `Databases` folder): **1,066 panes,
0 constructions** (every coated product also present as `"<name> Reversed"`). Systems built in SAM, converted by SAM_Tas (`ToTCD_Constructions`) and
calculated by `ThermalTransmittanceCalculator.CalculateGlazing` (the route the panel and the Builder use). Argon 16 mm, EN 673 vertical HTC 1.160.
SAM layer order is written as listed: SAM layer 0 → TCD material 1 (`ToTCD_Constructions` appends in order; `ToSAM_ConstructionLayers` reads in order).

| SAM layers (index 0 first) | Ug | g | LT | Reading |
|---|---|---|---|---|
| tint6 / Ar16 / clear6 | 2.585 | **0.607** | 0.480 | tinted pane at layer 0 |
| clear6 / Ar16 / tint6 | 2.585 | **0.380** | 0.480 | tinted pane last → lower g ⇒ **last layer = OUTSIDE** |
| S1Plus4 / Ar16 / clear4 | **1.047** | 0.525 | 0.751 | low-e (External ε 0.025, Internal 0.84) at layer 0 (inside): its External face faces the cavity |
| clear4 / Ar16 / S1Plus4 | **2.614** | 0.488 | 0.751 | same pane outside: its Internal (0.84) faces the cavity ⇒ **External = face towards OUTSIDE** |
| S1Plus4 Reversed (IGDB) / Ar16 / clear4 | 2.614 | 0.531 | 0.751 | reversed = opposite in both positions |
| clear4 / Ar16 / S1Plus4 Reversed (IGDB) | 1.047 | 0.496 | 0.751 | coating on surface 2: lower g than on surface 3 |
| clear4 / Ar16 / S1Plus4 swapped in SAM | 1.047 | 0.496 | 0.751 | **identical to IGDB's reversed entry** |
| Suncool 60/31 on surface 2 (Reversed outside) | 1.058 | 0.304 | 0.584 | solar-control coating behaves the same way |
| default `SIM_EXT_GLZ` as stored / layers reversed | 2.090 / 3.057 | 0.405 / 0.447 | 0.804 | its outer pane (last, ε_int 0.04) faces the cavity: the shipped data agrees |

Findings:
1. **`ApertureConstruction` pane layers are INSIDE → OUTSIDE** through the whole Tas route (confirmed absolutely by absorption, not only relatively).
2. A pane's **`External*` values belong to the face towards the OUTSIDE** of the built-up system.
3. Swapping the SAM layer order gives the expected difference (above).
4. **Reverse = swap External/Internal solar reflectance, light reflectance and emissivity** (transmittances and thermal properties are direction-free). The
   SAM-swapped material is JSON-identical (except name / Guid) to IGDB's own `"<name> Reversed"` entry and gives identical Tas results ⇒ **Reverse is
   enabled** in E0.
5. Gap HTC (EN 673, `Query.HeatTransferCoefficient`, ΔT 15 K, Tm 283 K) is used by Tas as the gas conductance (Ug follows it). Argon, low-e double:
   vertical 6/12/16/20/30 mm → HTC 2.807/1.403/**1.160**/1.197/1.266 → Ug 1.93/1.20/**1.05**/1.07/1.12; horizontal (heat flow up) 16 mm → HTC 2.139,
   Ug 1.61; 45° 1.755 / 1.41. Air vertical 16 mm 1.614 (Ug 1.33); krypton vertical optimum ~10 mm (0.934, Ug 0.89). ≤ 6 mm is conduction only (Nu = 1).
6. **No layer-count limit** met: 1-15 panes (29 layers) all calculated (clear 4 / Ar 12: 5.75, 2.68, 1.75, 1.30, 1.03, 0.86 … 0.34). A single pane is
   5.747 whatever its emissivity: Tas uses fixed surface resistances (1/(0.13+0.04+d/λ)).
7. Timing: 128 systems in one TCD session 3.15 s; one system ≈ 245 ms (warm). Pilkington subset: raw `ToSAM_ConstructionManager` 6.4 s;
   `Query.ReadThermalSource` (Add-source route) cold 6.9 s, warm 76 ms (JSON cache), converted-JSON route 177 ms.
8. Route parity: panes from the `.tcd` (Add-source) route and from the converted/imported JSON route are **1,066 / 1,066 identical** (definition JSON
   without Guid).

The same orientation checks run through the product route (draft → compose → `DraftGlazingEvaluator` → real Tas) as the opt-in test
`GlazingBuilderRealTasTests` (`SAM_E0_PILKINGTON_TCD=<path to the .tcd>`): tint outside g 0.380 / inside 0.607; coating on surface 3 Ug 1.047, on
surface 1 2.615; Builder Reverse = IGDB reversed entry (Ug 1.047, g 0.496, LT 0.751). Passed. The probe harness, its raw output and the product-route
output are kept locally (`documentation/evidence/thermal-e0-1-2026-10-02/gate0/`, untracked); the probe's cache file was removed afterwards.

## What was added (all in `WPF/SAM.Analytical.UI.WPF`, no SAM / SAM_Tas change)

| Component | File | What |
|---|---|---|
| Draft | `Classes/GlazingBuilder/GlazingSystemDraft.cs` | `GlazingSystemDraft` (Name, IntendedPanelType, Layers **outside → inside**, Frame, PaneAdditionalHeatTransfer, BasedOn name + Guid, stable `EvaluationGuid`), `DraftPane` (material snapshot, thickness, Reversed, source label + file NAME only, display name, category), `DraftGap` (gas + width; Air / Argon / Krypton offered), `DraftFrame` (None, or `CopyFrom(system, materials)` with an editable `Width`). Any invalid state is representable. Not persisted. |
| Order | `Classes/GlazingBuilder/GlazingLayerOrder.cs` | **The one reversal point** (outside → inside ↔ SAM inside → outside), with the Gate 0 evidence in its doc. |
| Compose | `Query/ComposeGlazingSystem.cs`, `Classes/GlazingBuilder/GlazingComposition.cs`, `GlazingMaterialMerge.cs` | Draft → complete window `ApertureConstruction` + a `MaterialLibrary` of only its materials + issues + provenance. Gaps = ordinary layers naming a derived `GasMaterial` (HTC from width + orientation, `DefaultGasType` set on the result, SAM's name pattern `Argon_16mm_1.16W/m2K_90deg` so identical gaps are one material). Reversed pane = `"<name> Reversed"`. DefaultPanelType (enum name, round-trips via `Query.PanelType`), Description (build-up), DefaultFrameWidth when entered, additional heat transfer copied. **Never** writes the construction's U / g / LT. A missing material leaves the composition incomplete (never calculated or saved). Two different panes of one name: the second is renamed `"name (source)"` together with its layer. |
| Validation | `Query/CheckGlazingDraft.cs`, `Classes/GlazingBuilder/GlazingDraftIssue.cs` | See below. |
| Evaluation | `Classes/GlazingBuilder/DraftGlazingEvaluator.cs`, `GlazingValuesCache.cs` | Through the existing `IGlazingEvaluator` / `TasGlazingEvaluator` on a transient one-system `ConstructionManager`. Snapshot per request, 350 ms debounce, content-keyed cache (layers + material definitions + additional heat transfer; not Guid / name), generation numbers (an answer that is not the newest is `Superseded`; its values are still cached for their content), errors → no Tas call, every failure (exception, Tas error, empty result, Ug ≤ 0 / NaN - Tas swallows failures and leaves 0) → `NotCalculated` with the reason. **Owns its own `TasGlazingEvaluator`** (own STA worker), so it never queues behind the panel's list. |
| User library | `Classes/GlazingBuilder/UserGlazingLibrary.cs` | "My glazing systems" (below). |
| Provenance | `Classes/GlazingBuilder/GlazingBuilderProvenance.cs` | One named ParameterSet on each saved system (below). |
| Enums | `Enums/GlazingDraftIssueSeverity.cs`, `DraftGlazingEvaluationState.cs`, `UserGlazingLibraryState.cs` | |

Gap evaluation orientation (persisted): Roof group → 0° (horizontal, heat flow up); every other use, and no intended use → 90° (vertical reference);
floor glazing → vertical reference (heat flow down is not covered by SAM's correlation; the vertical value is the higher, conservative conductance).

## Validation (SAM's rules reused, not re-implemented)

| Rule | Severity | Owner |
|---|---|---|
| No pane layers (raised from SAM's warning), layer without name, thickness ≤ 0, material missing from the composed library, gas as first / last pane layer, gas not recognised, pane material property NaN | as SAM (Error / Warning) | SAM `Create.Log(ac)`, `Create.Log(ac, lib)`, `Create.Log(material)` on the composed system - pane stack and frame logged separately so each record maps to its **draft** layer (SAM index → outside → inside index via `GlazingLayerOrder`). Frame materials are not re-judged (copied from an existing system). |
| Pane material missing; not glass in the pane stack; no thickness; gas not offered (Xenon, SF6, none); gas without definition | Error | Builder |
| Name empty / already in My glazing systems (trimmed, case-insensitive) | Error, **only when saving** | Builder |
| Pane → Pane (no cavity); Gap → Gap; gap outside 4-30 mm; > 4 panes; no intended use; frame without width | Warning | Builder |
| No frame | Info | Builder |
| Tas failure / unavailable | a **status** of the evaluation, never an issue | evaluator |

One problem → one error (a layer with its own error does not also list "not in the library"). Errors first.

## My glazing systems - persistence and concurrency

* File: `Documents\SAM\User Libraries\Glazing Systems.json` (`Core.Query.UserSAMDirectory()`), an ordinary `ConstructionManager` JSON (systems +
  every material they use): the D2 `Add source…` reader reads it as a source (tested) and it can be copied to another machine.
* **Save = a new immutable system**: composed with a **new Guid** (never the draft's evaluation Guid), never replaces or removes a system, refuses a Guid
  already present; ApertureType Window, DefaultPanelType, Description, DefaultFrameWidth when framed, provenance.
* Materials embedded: identical definition → reused; same name, different definition → saved as `"name (source label)"`, then `"name 2"`, … and the
  system's **`ConstructionLayer.Name` and provenance are rewritten with it** (`RenamedMaterials` reported).
* Writes: exclusive lock file (`Glazing Systems.json.lock`, `FileShare.None`, delete-on-close, waits ≤ 10 s, then an explicit "being saved by another
  SAM window" failure) around **re-read → validate (names) → merge by Guid → write temp → `File.Replace` (atomic, keeps `Glazing Systems.json.bak`)**;
  the serialised JSON is parsed back before it is written. A file that exists but is not a readable `ConstructionManager` (corrupt, empty, another type)
  is **never overwritten** (state `Unreadable`, with the reason). Every failure is returned in `UserGlazingSaveResult.Error`; nothing is written on failure.
* Tests: two writers on one file keep both systems; 6 parallel saves → 6 systems; a held lock fails explicitly and writes nothing; `.bak` holds the
  previous file; no `.tmp` / lock file left; corrupt / empty / wrong-type files untouched.

## Provenance (`ParameterSet` "SAM Glazing System Builder", fixed Guid, one per system)

Schema Version (1) · Created UTC · Based On Name / Guid · Intended Panel Type · Gap Evaluation Tilt [deg] · Gap Heat Transfer Basis (method + orientation
in words) · Panes (outermost first: Position, Material (saved name), OriginalName, DisplayName, Category, SourceLabel, SourceFile, Reversed, Thickness) ·
Gaps (Position, Gas, Thickness, HeatTransferCoefficient, TiltDegrees, Material) · Frame (None / Copied) + Copied From Name / Guid · Frame Width [m] ·
Performance Ug / g / LT / Uf (the values Tas gave the draft, when known) · Performance Engine · SAM_Tas Version.
**Only labels and file names** - a source given as a path keeps its file name only (tested with drive, UNC and POSIX paths: no path in the saved JSON).
Checked against SAM's one-set-per-name rule (PR2A-0): a second set of that name merges into it, later values winning, and round-trips as one set.

## Mutation invariant

The Builder code has no `UIAnalyticalModel`, `AnalyticalModel` or `AdjacencyCluster` field, property or parameter, and its IL (with its closures / state
machines and everything in the WPF assembly it calls) references no model type and no `SetJSAMObject` / `Undo` / `Redo`
(`The_Builder_HoldsNoModel_AndCannotChangeOne`; the scan finds `ThermalEditSession`'s model references, so it is not vacuous). Composing, checking and
evaluating never write the user library (hash + timestamp unchanged).

## Tests

79 new (`GlazingBuilderComposeTests` 24, `GlazingBuilderValidationTests` 19, `GlazingBuilderEvaluatorTests` 14, `UserGlazingLibraryTests` 21,
`GlazingBuilderRealTasTests` 1 opt-in), fakes only for normal runs (`Helpers/BuilderFixture.cs`: IGDB-shaped panes, default-gas properties, a seed with a
frame, `FakeDraftTas`; `Helpers/BuilderSurface.cs`: the structural scan). Full WPF suite **2075/2075** (base `a03940bc` 1996 + 79).

## Left out deliberately / found on the way (not fixed here)

* SAM `Create.Log(IMaterial)` flags only values that are NaN; a parameter that is absent reads as 0 and passes (pane properties missing from a source are
  not caught). Builder relies on SAM here by design.
* SAM `Create.GasMaterial(gas, …)` writes the gas type onto its source (plan §21) - worked around (set on the result), not fixed.
* `ThermalTransmittanceCalculator` swallows exceptions and leaves Ug 0 (`values.Length >= 6` guard reading index 6) - the evaluator treats Ug ≤ 0 as
  "Not calculated".
* SAM's `UpdateHeatTransferCoefficients` has no floor (heat flow down) case (a 180° tilt gives NaN); the Builder uses the vertical reference for floors.
* Classification of SAM log records relies on SAM's message texts (e.g. "recogionzed"); the tests would fail if SAM changed them.

## Next: E0-2 (recommendation)

`ThermalEditServices.UserGlazing` (a `UserGlazingLibrary` factory; tests inject a temp path); a `GlazingSourceKind.User` source ("My glazing systems") built
from `UserGlazingLibrary.Read()` and added to every `Change…` list after the default library (Guid-first-wins unchanged; an `Unreadable` library is shown
as a source note, never overwritten); the GLAZING CHANGE report gains `Guid`, `Source` and `Built from` lines read from the provenance; mini real-app
acceptance with a library file written by `UserGlazingLibrary.Save` (candidate appears, Apply = one `SetJSAMObject` / one Undo, report lines, colour-by,
the library keeps the system after Undo). Still no Builder window (E0-3).
