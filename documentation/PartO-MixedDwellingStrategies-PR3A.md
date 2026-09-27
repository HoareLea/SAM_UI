<!-- SPDX-License-Identifier: LGPL-3.0-or-later -->
<!-- Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors -->

# Part O — mixed dwelling strategies: PR3A (active cooling architecture investigation)

**Status: investigation only (27 Sep 2026). No production code changed in any repo.** Owner direction during the
investigation: the PR3 cooling authority is **the MVHR cooling Iteration 3 already uses** - the selected product's
manufacturer-guidance cooling (`PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance`, "MG"), the mode
the production Hub runs (`WPF/.../Modify/RunPartOWorkflow.cs:89`). Not the IZAM plant-zone supply temperature.

Target: `Flat 1 Natural / Flat 2 MVHR / Flat 3 Optimised MVHR / Flat 4 MVHR + cooling / Flat 5 Optimised MVHR +
cooling` + an uncooled communal corridor, in ONE materialised model and ONE annual TAS simulation case, TM59 final.

## 1. Integration heads (fetched 27 Sep 2026)

| Repo | `sow/2026-Q3` | Notes |
|---|---|---|
| SAM | `6c255ad8` (SAM#159) | PR2 SAM#152 `be84d7b8`, SAM#157 `33eb00f3` included; PR2C reporting landed since |
| SAM_UI | `cbe1c076` (SAM_UI#127) | #126 `2a341255`; **PR2 closeout #128 MERGED `5ea27775`**; PR2D report UI landed since |
| SAM_Tas | `fedf34cd` (SAM_Tas#70) | PR2A peak-authority work landed since |
| SAM_Systems | `22133736` (#30) | unchanged since 24 Sep |
| SAM_Deploy | `e5cfeb1` (#54) | pins SAM_UI `4b773f3` (pass 6) - mixed PR1/PR2 not deployed yet (PR4) |

## 2. Legacy Iteration 3 cooling - the production path

Iteration 3 is an **A/B comparison hung off a session 1a run**, not a final-design route.

1. **Input**: the session `PartORun` of an Iteration 1a run - its prepared model, Reference A (IZAM route) TSD and
   BasePassive scenarios, and the session-only `Guids_VentilationSystem_Prepared` (`RunPartOIteration3.cs:143-199`).
2. **Scope**: `Query.PartOIteration3SystemScope` keeps the systems 1a built, drops authored systems (`:281-300`).
3. **Cooling resolution (SAM_UI orchestrates, SAM_Systems owns the numbers)**: for every scoped AHU,
   `Query.PartOIteration3GuidanceResolution` reads the AHU's selected product, checks the design duty/capacity and
   calls `SAM.Analytical.Systems.Query.MechanicalVentilationGuidanceSettings(VentilationUnitTemplate)` - the
   catalogue's `VentilationUnitOperatingStrategy` (cooling-stat 22 °C, bypass rule, exchanger-then-coil supply law,
   13 °C floor, **elevated airflow = product figure, else default 80 l/s, else range midpoint; range 60-120 l/s**,
   capped at unit capacity). **All-or-nothing**: one unit without a product/strategy refuses the run.
   (The older B4 mode - `PartOIteration3CoolingResolution`, recirculation branch from the published cooling table -
   still exists but is not the Hub's mode.)
4. **Materialisation - SAM_Systems** `Create.MechanicalVentilation(cluster, template, settings, spaces)`
   (`Create/MechanicalVentilation.cs:59`): ONE topology template per call - `MVRE.json` when any unit/guidance
   settings exist, else `MV.json` (`PartOIteration3Pipeline.cs:86`); one AirSystem per AHU in one plant room;
   MG inserts a supply DX coil after the exchanger (`Create/MechanicalVentilationGuidanceCooling.cs`), elevated
   flow split per room in design proportions, stat room = largest design supply. **No setpoint or capacity in
   SAM_Systems** - topology + a `MechanicalVentilationGuidanceCooling` record per AHU.
5. **Thermal source - SAM_Tas** `Create.NoIzamThermalSource` = Reference A's own pipeline with
   `AddIZAMs=false, RemoveIZAMs=true, RemoveMechanicalVentilationGains=true` (`NoIzamThermalSource.cs:81-86`) -
   **building-wide**: no MVHR plant zones, every IZAM removed, `ticV` zeroed everywhere; apertures and infiltration
   untouched. Survivor refusal if any IZAM remains.
6. **TPD route - SAM_Tas** `Create.SystemVentilationRoute`: convert graph → TPD, extract/transfer legs as damper
   duty carriers (**transfer air is kept**, as TPD legs, not IZAMs), `GroundGuidanceCooling` per guidance record:
   DX coil with a **100 kW numerical duty, not a rating** (`GroundGuidanceCooling.cs:338-347` - the supply law, not
   the duty, bounds cooling; the 2200 W figure in older SAM_Systems notes is superseded), exchanger state table,
   variable-speed fans at the elevated flow, three stat controllers on the stat room. Each air system simulated
   alone (`ISystem.Simulate`); plant never simulated, no refrigerant group linked.
7. **Bridge - SAM_Tas** `ThermostatBridge`: copy of the no-IZAM TBD, each **bound** room's TPD air temperature
   written into both thermostat limits, TBD re-simulated days 1-365 → bridge TSD. **Unbound rooms (NV, corridor)
   free-run** with their own ICs, apertures, no IZAM, `ticV=0`.
8. **TM59**: `PartOTM59Assessment.Assess(CandidateB model, bridge TSD, Reference A's BasePassive scenarios)`
   (`RunPartOIteration3.cs:852`); Candidate B persisted with those scenarios + provenance to the bridge TSD (`:972`).
9. **Provenance of cooling**: only the SAM_UI `PartOIteration3Record` (guidance evidence, operating-airflow CSV,
   resultant method). Scenario keys say BasePassive (no cooling). `PartODiagnosticLog` has no cooling field and
   takes the iteration from `scenarios[0]` (C9).

"One annual TAS simulation" on this route = one simulation **case** (one weather, one year) executed as three TAS
passes (no-IZAM TBD, TPD systems, bridge TBD); the bridge TSD is the single result TM59 reads.

## 3. Cooling authority today

| What | Where | Scope |
|---|---|---|
| Cooling intent | `PartODwellingStrategy.ActiveCooling {Undefined, None, SupplyAirCooling}` (SAM, persisted on baseline) | dwelling - **refused** by `MaterialisePartODwellingStrategies` (`CoolingGated`, `NaturalWithCooling`) |
| Engineering parameters | catalogue `VentilationUnitOperatingStrategy` → SAM_Systems `MechanicalVentilationGuidanceSettings` | **product** (per selected unit) |
| Topology | SAM_Systems graph (MVRE + DX) | per AHU; template per call |
| TAS control/capacity | SAM_Tas `GroundGuidanceCooling` | per air system |
| Route | SAM_Tas no-IZAM + bridge | **whole building** |
| Scenario identity | none - `PartOIteration.ActiveTrimCooling` exists but is refused everywhere (`PartOOperatingAssumptions.cs:125-131`), so no persisted key depends on it | - |
| IZAM-route supply conditioning | AHU `SummerSupplyTemperature` → plant-zone `ticUL` (`SAM AddAirMovementObjects.cs:256`, `SAM_Tas UpdateIZAMs.cs:267-275`) | per AHU - **a leak path, not the authority**; PR1 refuses it (`ConditionedReusedUnit`) |

## 4. Dwelling-scoped vs shared

- **Dwelling-scoped already**: PR1 builds exactly one generated MVHR system + unit per dwelling; transfer air
  never crosses dwellings/corridor; SAM_Systems and TPD one AirSystem per AHU; DX coil, fans, stat controllers per
  air system; per-zone scenarios; `VentilationTerminal.DesignFlowRate_Lps` per space.
- **Per call / global**: SAM_Systems topology template; the all-or-nothing rule for unit/cooling/guidance settings
  (`MechanicalVentilation.cs:283-381`); SAM_Systems' AHU set is the whole model's `VentilationSystem`s (`:228`);
  the no-IZAM strip and the bridge (building-wide booleans, `WorkflowSettings`); the catalogue strategy (per product,
  so every dwelling with that product gets the same cooling airflow/law).
- **Shared plant**: none effective - collections not linked to the new DX coils, plant never simulated.

## 5. Is the mixed cooled case representable today? **No.** Four hard blockers.

1. SAM `MaterialisePartODwellingStrategies` refuses `SupplyAirCooling` (`CoolingGated`).
2. SAM_Systems refuses cooled + uncooled AHUs in one call ("... has no manufacturer-guidance cooling unit while
   other units in this call do", `:370`) and has one template (MV or MVRE) per call.
3. There is no scenario identity for a cooled dwelling (`ActiveTrimCooling` uncharacterised).
4. The PR2 mixed run (`Modify.RunPartOStrategySet` → `SimulatePartOMaterialisation`) has only the IZAM route.

What is **not** blocking: SAM_Tas grounds cooling per `GuidanceCooling` record (`SystemVentilationRoute.cs:217-240`),
so one TPD document with cooled and uncooled air systems is expected to work unchanged once SAM_Systems emits
records only for the cooled units (to be pinned by a test in PR3B). NV rooms and the corridor are unbound and
free-run in the bridge, which the evidence in §11 supports.

## 6. Defects / risks found

| # | Finding | Consequence | Where |
|---|---|---|---|
| D1 | Any cooled dwelling puts the **whole building** on the TPD route; uncooled MVHR dwellings are then simulated on TPD too | Flat 2's result depends on whether Flat 4 is cooled; B0 evidence: room RMSE 0.5-1.1 K, wet-room >26 °C hours up to ×2.7 lower (Ensuite_8 677→246); TM59 outcome unchanged on that fixture | SAM_Tas no-IZAM, by design |
| D2 | Uncooled units in a cooled call would get the MVRE template (exchanger with template-default recovery) - a physics change from the IZAM route, which models no heat recovery (Part O extract is flattened to leave from the rooms, `SAM_Tas Query.DesignTerminalExtractFlattening`, so the plant zone sees outside air only) | must be MV (B0) for uncooled units | SAM_Systems template per call |
| D3 | MG elevated cooling airflow is a **product** figure (80 l/s default, 60-120 l/s); SAM_Systems refuses it below the dwelling's design total (`MechanicalVentilationOperatingFlows.cs:177-185`) | **cooled Optimised MVHR fails closed** where the accepted 2B design exceeds it - the PR2 Flat 3 design (143 l/s supply) is refused | SAM_Systems; policy decision |
| D4 | SAM_Systems membership follows **authored** transfer air (BFS, `:659-760`); a clean baseline may carry authored movements that PR1 carries through unchanged | an authored transfer to a corridor would make it a member of a cooled AHU → cooled/bound corridor | SAM_Systems + PR1 guard |
| D5 | `PartOMaterialisationRecord.Fingerprint_Catalogue` covers identity, max supply/extract, rank only | a changed cooling strategy (activation, supply law, elevated flow) would not stale a cooled result | SAM |
| D6 | Scenario keys cannot say "cooled" today; Iteration 3 persisted Candidate B under BasePassive keys asserting no cooling | PR3 must mint new keys, never reuse BasePassive | SAM |
| D7 | `PartODiagnosticLog` single iteration from `scenarios[0]`, no cooling field (C9) | mislabelled diagnostics in a mixed run | SAM_Tas (minor) |
| D8 | IZAM route turns a finite `SummerSupplyTemperature` into plant-zone cooling | authored conditioning must stay refused for every strategy, cooled or not, so only the product route cools | SAM (already refused) |

## 7. Smallest SAM / domain changes (PR3B)

SAM (`SAM.Analytical`):
- Lift `CoolingGated` for `MVHR + SupplyAirCooling` **with a selected-or-pooled product whose catalogue entry carries
  an `OperatingStrategy`**; refuse cooling with generic units (no product → no cooling authority), keep
  `NaturalWithCooling`, keep `ConditionedReusedUnit` for all strategies.
- `PartOMaterialisation` output: the set of cooled units (AHU guid per cooled dwelling), and the route
  (`Izam` when none cooled, `Systems` when any) - the route is decided in SAM from the strategy set, never in SAM_UI.
- Characterise `PartOIteration.ActiveTrimCooling` (new keys, nothing re-keyed): BasePassive's four assumptions +
  one identity assumption for the cooling provision (proposed `"Active Cooling" = "Manufacturer Guidance Supply Air"`)
  - **needs owner confirmation** (the enum's own comment says so). TM59 criterion stays `MVHR` (mechanical,
  TM59MechanicalVentilation) - **owner to confirm** for a cooled dwelling.
- Cooled dwellings: refuse where the product's elevated cooling airflow < the dwelling's design total (D3), with a
  refusal naming both figures, unless the owner chooses another rule (§12 Q2).
- Guard D4: a cooled dwelling's AHU membership must equal its own spaces (refuse any authored movement linking a
  cooled dwelling to a space outside it).
- `PartOMaterialisationRecord`: add a cooling fingerprint - canonical `MechanicalVentilationGuidanceSettings`
  identity (strategy fields + `SourceIdentifier`) of each cooled dwelling's product (D5). Schema `v2`, `v1` records
  read as "not current" for a cooled set.

SAM_Systems:
- Allow a **partial** `GuidanceSettings` (cooled subset) - a unit absent from the dictionary is uncooled - but keep
  "every key must be materialised" and keep MG and B4 mutually exclusive.
- **Per-AHU topology**: MV prototype for uncooled units (the B0 parity control), MVRE + DX for cooled units, in one
  energy centre (settings carry the second template; one prototype per class). Determinism unchanged (guid keys
  already include per-unit settings identity).

SAM_Tas: expected **none** for the route; one TM59-tests case pinning a TPD document with one guidance and two plain
air systems plus an unbound room. C9 diagnostic label fix optional in the same PR.

## 8. Smallest SAM_UI changes (PR3C)

- Enable the `checkBox_Cooling` constraint and a per-row **Active cooling: On/Off** (bulk bar: Cooling on / off).
  Refused where the row is Natural (SAM's `NaturalWithCooling`) or where the constraint forbids it.
- `SimulatePartOMaterialisation`: when SAM's materialisation says route `Systems`, run the existing Iteration 3
  `IPartOIteration3Pipeline` stages *without* the A/B comparison - `ThermalSource` → `Materialise` (SAM_Systems,
  guidance settings resolved for the cooled units only) → `Route` → `ResultantTemperatures` → TM59 over **the
  materialiser's own per-zone scenarios** (not BasePassive) → persist with provenance to the bridge TSD. No second
  implementation: a thin adapter over the pipeline that already has licensed acceptance.
- Evidence (`PartOMixedRunEvidence`): route, cooled dwellings, per-unit guidance operation summary (existing
  `GuidanceCoolingResults.ToCsv`), bridge/TPD file records; "ran as … on the TAS Systems route" per dwelling.
- Screening: `ActiveCooling` stays UNAVAILABLE in PR3 (a cooled screening run = one more whole-building Systems run;
  add later as one strategy row).
- No advanced cooling settings per row. Cooling parameters stay product-level (catalogue) - hundreds of rows never
  duplicate them.

## 9. Proposed persisted cooling intent/state

- **Intent (baseline, SAM)**: the existing `PartODwellingStrategy.ActiveCooling` - `None` / `SupplyAirCooling`.
  No new field; the UI's "Active cooling: On" writes `SupplyAirCooling`. Nothing numeric.
- **Engineering parameters**: the product's catalogue `OperatingStrategy` (as Iteration 3). If the owner wants a
  project override of the elevated cooling airflow (§12 Q2), it is a project setting on `AnalyticalModelParameter`
  beside `PartOEquipmentSelection` - never per row.
- **Airflow**: unchanged - `VentilationTerminal.DesignFlowRate_Lps` is design authority; the elevated cooling
  airflow is an *operating* airflow (the fourth concept), never written to terminals.
- **Constraint**: `PartOMixedDesignConstraints.CoolingAllowed` (sidecar, as the other two).
- **Materialised state**: only in run artefacts (SAM_Systems graph, TPD); the clean baseline never carries a DX
  coil, cooled unit or supply temperature.

## 10. Provenance / fingerprint changes

- Strategy fingerprint: already includes `ActiveCooling` (`PartODwellingStrategy.CanonicalText`) - toggling
  cooling stales the final run (case 7) with no new work.
- Record: + cooling fingerprint (D5) + route; `IsCurrent` reports "the cooling data of '<product>' changed".
- Scenario: new `ActiveTrimCooling` keys for cooled dwellings (SAM PR0 §G - a migration, never a reinterpretation).
- Evidence: the bridge TSD length/time (as the IZAM TSD today) plus the TPD file; the simulation-case key already
  covers weather + solar.
- Reconstruction (case 8): cooling lives only in the strategy; removing it rebuilds from the clean baseline with no
  cooled unit, and the route returns to IZAM when no dwelling is cooled - by construction (PR1 determinism).

## 11. Real-TAS evidence (this investigation - no new simulation)

TAS is licensed on this machine. The mixed cooled topology **cannot be generated without the PR3B code**, so an
annual run of the target now would not resolve anything the code reading has not. Instead the existing licensed
runs of 24 Sep (`C:\TasOut\parto-guidance-2026-09-24\` B0 and MG, owner's `SAM_zoningAM-CIBSEfutureZ1`, 3 flats +
corridor) were read back through the TSD COM interop (script `evidence/parto-mixed-pr3a/read-tsd.ps1.txt`; numbers
`evidence/parto-mixed-pr3a/route-comparison-2026-09-27.txt`):

- **D5(a) - unbound room on the TPD bridge vs the IZAM route** (Corridor_1, the only free-running room): bias
  **+0.08 K, RMSE 0.30 K**, max 1.23 K; >28 °C 11 h (A) vs 9 h (B0). The bridge matters: the no-IZAM source alone is
  -0.76 K / RMSE 0.89 K off; re-coupling to the pinned neighbours brings it back. First evidence that an NV dwelling
  can share the Systems route - **an NV flat with operable windows is still to be proven** (PR3B acceptance).
- **D5(b) - uncooled MVHR on TPD vs IZAM** (B0): habitable rooms bias +0.03..+0.48 K, RMSE 0.53-0.78 K, exceedance
  within a few hours; **wet rooms differ materially** (Ensuite_5/8 >26 °C 536/677 h → 247/246 h). The owner must
  accept (or reject) that an uncooled neighbour of a cooled dwelling is assessed on this route (§12 Q1).
- **Neighbour coupling is physical, not leakage**: with all flats on MG the corridor runs +0.51 K warmer annually
  (flats' heat recovery), >28 °C hours 11 → 9. Cooling equipment stays on its own air system (verified in code).

## 12. Owner decisions needed before PR3B

1. **Route for uncooled neighbours (D1)** - (A, recommended) any cooled dwelling → whole model on the Systems route,
   uncooled MVHR dwellings as B0 (MV, design airflow), NV/corridor unbound; the route is recorded and shown. SAM_Tas
   unchanged. (B) a hybrid - uncooled dwellings keep IZAMs/ticV in the no-IZAM source and bridge, only cooled units
   on TPD; keeps neighbours' results route-independent but changes the fail-closed no-IZAM and bridge contracts and
   needs its own proof.
2. **Cooled Optimised MVHR (D3)** - refuse where design total > product elevated airflow (recommended for PR3), or a
   project-level cooling airflow (≤ product maximum 120 l/s), or cooling at design airflow when above.
3. **Scenario identity** - `ActiveTrimCooling` assumptions and the TM59 criterion for a cooled dwelling (§7).
4. **UI** - per-dwelling `Active cooling: On/Off` only, parameters product-level (recommended).

## 13. Proposed PR sequence

- **PR3B - SAM + SAM_Systems (+ SAM_Tas test)**: §7. SAM first (strategy/route/scenario/record), then SAM_Systems
  (partial guidance, per-AHU MV/MVRE), then a SAM_Tas test-only PR. Tests: 5-flat unit fixture (NV / MVHR / 2B /
  MVHR+C / 2B+C / corridor), refusals (generic+cooling, NV+cooling, elevated<design, authored transfer from a cooled
  dwelling, reused conditioned unit), determinism, record staleness on strategy/cooling data. Gate: **one**
  licensed annual run on the PR2 clean fixture (Flat 1 NV, Flat 2 MVHR uncooled, Flat 3 MVHR + cooling with a
  moderate accepted design) - TPD shows DX only in Flat 3's system, bridge/TM59 complete, corridor unbound.
- **PR3C - SAM_UI**: §8 - toggle, Systems-route adapter over the Iteration 3 pipeline, evidence/staleness, fake-TAS
  tests (Systems route delegate), native owner walk-through incl. save/reopen/remove-cooling rebuild.
- **PR4 - acceptance + deploy** (unchanged from PR0): large-model mixed run incl. cooled dwellings, SAM_Deploy pins.
