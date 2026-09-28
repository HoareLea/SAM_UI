<!-- SPDX-License-Identifier: LGPL-3.0-or-later -->
<!-- Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors -->

# Part O — mixed dwelling strategies: PR4 (large-project acceptance and deploy)

**Status (28 Sep 2026): evidence complete; tests/harness/evidence only - no production code change.**
[SAM-BIM/SAM_UI#137](https://github.com/SAM-BIM/SAM_UI/pull/137), branch `feature/parto-mixed-pr4-2026-09-28` from
`sow/2026-Q3` `52217c3`, updated with `sow/2026-Q3` through `0580078` (SAM_UI#135, #136). Builds against SAM `bc85ba61`,
SAM_Systems `fbef48f`, SAM_Tas `5753ad2` (docs-only over the PR3C build SHAs `3d6fa80a` / `005c4fe` / `e7cc0ed`).
The SAM_Deploy pins follow in a separate SAM_Deploy PR (§2).

**Verdict.** Correct at scale; SAM, SAM_UI and SAM_Systems scale to 5,000 spaces in seconds; the TPD conversion is linear.
**The PR0 target of a full-year licensed run at ~5,000 spaces is NOT practical on the current TAS**: TAS's own TSD result
reads grow superlinearly with the project (§4.3), and every route reads them. Measured, not fixed here (§7).

## 1. Scope (from the records, not inferred)

- SAM `PartO-MixedDwellingStrategies-PR0.md` §F: *"PR4 — real TAS acceptance and deploy. Scope: a full-year mixed run on
  a large project (hundreds of dwellings, ~5,000 spaces); SAM_Deploy pins. Acceptance: the TM59 per-dwelling outcome
  matches the selected strategies; no `.sam` growth across re-materialisations; materialisation-time budget met at
  scale."* §H: "A PR4 budget should be measured, not assumed."
- SAM_UI `PartO-MixedDwellingStrategies-PR3A.md` §14.1/§8: the large run includes cooled dwellings; "scale behaviour of
  TPD with hundreds of air systems is a PR4 gate". "Systems route as the single final route" is a separate post-PR4
  proposal - **not PR4**.
- PR3C §6 limitations were triaged, not adopted as scope (§6).

## 2. Deployment gap found

SAM_Deploy `sow/2026-Q3` (`e162a25`) pins SAM `3d6fa80a`, SAM_Systems `005c4fe1`, SAM_Tas `e7cc0ed4` (the PR3C build
SHAs) but SAM_UI `8971cfb0`, which does **not** contain PR3C (`453ca94`), and SAM_Tas_Grasshopper `a4d4f73`, without
SAM_Tas_Grasshopper#7 (mixed-building diagnostic log name, PR3B follow-up). The shipped application has no per-dwelling
cooling today. The SAM_Deploy PR after this one moves SAM_UI to this PR's merge and SAM_Tas_Grasshopper to `9ddf8ff`
(SAM, SAM_Systems, SAM_Tas to their docs-only tips).

## 3. The large fixture

No large residential project exists on this machine (largest `.sam`: a 10,218-space office-cell model without
dwellings; Earls Court is panels only). The large project is the real PR2 clean baseline
(`SAM_zoningAM-CIBSEfutureZ1-MixedBaseline.sam`, 3 flats + corridor, 9 spaces, SHA `89A8AC7B…9446`) replicated on a grid
(`PartOMixedLargeProjectFixture.Replicate`): each block its own spaces, panels, apertures and zones with fresh guids and
names (`Flat 1 #12`), its Part F data renamed with it; constructions, internal conditions and heating / cooling systems
shared. Blocks are 150 m × 100 m apart, so none shades or adjoins another and the original block's TM59 outcome is every
block's reference. (Precedent: PR2F replicated a 9-space model to 4,995 spaces.)

## 4. Evidence by layer

### 4.1 No TAS, 5,000 spaces (`PartOMixedLargeProjectTests`, Release, this machine)

500 dwellings × 10 spaces + corridor; 167 Natural / 167 MVHR / 166 MVHR + cooling by bulk assignment:

| Layer | Measured |
|---|---|
| Session open / bulk assignment | 678 ms / 15 ms (window: 15 rows realised of 500 - PR2 §8, unchanged) |
| SAM mixed materialisation (with templates) | **4.1 s** |
| SAM_Systems: ONE graph, 333 air systems, 166 guidance-cooled | **7.8 s**; a DX coil on exactly the 166 cooled units' air systems, 0 elsewhere |
| Build & Run orchestration (TAS stand-in) | 3.2 s |
| Evidence `IsCurrent` (a reopened project) | 0.2 s |
| `.sam` growth | **none**: saved model 6,204,679 chars and materialised 11,099,410 chars identical after rebuild and cooling off → on |

Plus: scenarios truthful for all 500 dwellings + corridor; one simulation of one model; the clean source untouched.

### 4.2 Licensed TAS, production route (`PartOMixedLargeProjectAcceptance`, env-gated)

Production `BuildAndRunPartOMixedDesign` with the production simulators. Per block: Flat 1 Natural, Flat 2 MVHR
(automatic product), Flat 3 MVHR + Nuaire MRXBOXAB-ECO5-AECV (MR-ECO-COOL-V) + cooling on (off for the uncooled run);
installed catalogue (SHA `D3878908…58D8`); Z1 DSY1 2050s HIGH90, TAS solar, full year.

| Run | Dwellings / spaces | MVHR air systems / cooled | Thermal source | TPD | Bridge | TM59 | Total | Result |
|---|---|---|---|---|---|---|---|---|
| 1 block (PR3C) | 3 / 9 | 2 / 1 | | | | | 6.7 min | PASS (PR3C) |
| ×10 cooled | 30 / 90 | 20 / 10 | 5:40 | 12:15 | 9:31 | 0:26 | **27:54** | **PASSED** |
| ×30 cooled | 90 / 270 | 60 / 30 | 1:08:05 | 48:30 | 1:21:06 | 1:15:41 | **4:33:29** | **PASSED** |
| ×10 uncooled (IZAM) | 30 / 90 | - | 6:25 (shading 3:54, results 0:28) | - | - | 0:17 | **6:44** | **PASSED** |

Each PASSED run checked (logs `evidence/parto-mixed-pr4/Large_*.txt`): route as expected; cooled dwellings = exactly the
Flat 3s; ONE TPD; TAS guidance read-back for every cooled unit; no unit supply setpoint on the analytical model; every
Natural dwelling `BaseNaturalVentilation`, uncooled MVHR `BasePassive`, cooled `ActiveTrimCooling`, every corridor
`DwellingIndependent`; no space unassessed; **every dwelling has its block reference's TM59 outcome** (0 deviate of 30 /
90: Flat 1 Pass; Flat 2 Fail - Bedroom 2_3, Kitchen_4; Flat 3 Fail - Kitchen_7, the PR3C single-block result); run model
reopens and restores (1.0 / 1.3 s); re-materialised from the reopened baseline with the same size; the saved baseline not
written by the run; run current against it; source fixture unchanged.

The uncooled run (IZAM route, no TPD, no cooled dwelling, Flat 3 `BasePassive`) reproduces PR3C's uncooled result: Flat 3
fails Bedroom 2_6 as well as Kitchen_7 - in all 10 blocks.

Output sizes grow linearly (×10 → ×30): TSD 161 → 483 MB, bridge TSD 163 → 489 MB, bridge TBD 15.8 → 47 MB, guidance CSV
7.2 → 22 MB, run model 0.75 → 2.1 MB.

### 4.3 Where the time goes (×10 → ×30: 3× the project, 9.8× the time)

| Stage | Growth | Cause (measured) |
|---|---|---|
| TAS shading (T3D → TBD) | 198 → 708 s (3.6×) | TAS; ~linear |
| TAS building simulation | 34 → 115 s (3.4×) | TAS; linear |
| **Thermal source "Adding Results"** | **27 → 3,038 s (112×)** | SAM_Tas `Modify.AddResults` → TSD reads, below |
| TPD conversion "ventilation legs" | 425 → 1,226 s (2.9×) | SAM_Tas → TPD COM; **linear, ~21 s per air system** |
| TPD simulation + read-back | ~5 → ~28 min | TAS Systems; per air system |
| Bridge | 9:31 → 1:21:06 (8.5×) | TBD simulation + per-room TSD reads |
| **TM59** | **26 s → 1:15:41 (175×)** | per-space TSD reads of the bridge TSD |

**TSD read probe** (`evidence/parto-mixed-pr4/probes/`, read-only on copies of each run's TSD): per zone, SAM_Tas's
`Query.Overheating` (365 daily reads × 3 arrays) costs 192 ms at ×10 and **6,782 ms** at ×30; `GetPeakZoneGroupGains`
35 ms → **6,743 ms** per call; surface results stay ~55 ms. Replacing the 365 daily calls by ONE `GetAnnualZoneResult`
per array gives bit-identical values and is 4× faster at ×10 - but at ×30 **one annual call costs the same 22.4 s as 365
daily calls**: the cost is not the number of COM calls, it is each zone-series read, which TSD.exe (64-bit, v2.0.0.1)
performs CPU-bound on one core, growing with the file. There is no building-wide per-zone-series call in the TSD API
(`GetSumZoneResultForMultipleZones` sums; a batched `GetPeakZoneGroupGains` returns one column). RAM (10 GB free) and
disk (NVMe) are not the limit.

**Consequence.** Every full-year route reads per-zone series: the thermal-source results (both routes, every Part O
iteration), the bridge and TM59. Extrapolating the measured growth (T ∝ N^2.1: 9.8× for 3×) from 4.5 h at 270 spaces, a
single full-year run at ~5,000 spaces is of the order of 2,000 hours - a crude extrapolation, but not a practical run. The limit is not in the mixed-strategy design, SAM materialisation or
SAM_Systems; it applies equally to the pre-existing homogeneous Part O workflow.

**Practical guidance today:** a full-year mixed run is ~30 min at ~100 spaces and ~4.5 h at ~270 spaces on this machine.

## 5. Files

- Tests: **new** `PartOMixedLargeProjectTests.cs` (2 facts), **new** `PartOMixedLargeProjectFixture.cs`, **new** env-gated
  `PartOMixedLargeProjectAcceptance.cs` (`SAM_PARTO_PR4_DIR`, `SAM_PARTO_MIXED_BASELINE`, `SAM_PARTO_PR4_COPIES`,
  `SAM_PARTO_PR4_COOLING`); `PartOMixedCoolingTests.cs` (fixture product members `private` → `internal`).
- This record; evidence `documentation/evidence/parto-mixed-pr4/` (run logs, TAS timing CSVs, probe sources + results).

## 6. PR3C limitations - triage

| Item | Decision |
|---|---|
| No accepted Optimised + cooled fixture inside 60-120 l/s | Retained limitation (no such accepted design exists; not manufactured) |
| Native TPD DX coils not COM-read-back in PR3C | Acceptance only: DX coils per air system checked on the real SAM_Systems graph at 333 air systems (§4.1); TAS guidance read-back for every cooled unit at 10 / 30 (§4.2) |
| `.tpd` / `_GuidanceOperation.csv` left after a later IZAM rebuild | Retained known limitation (unreferenced; a stale Systems result is never shown as current) |
| Matrix columns overflow the default 1240 px window | Retained pre-existing limitation |
| No cooling during screening | By design (PR3A §8) |
| "Systems route for all dwellings" | Separate post-PR4 proposal (PR3A §14.1) |

## 7. Validation, open items, next step

- Focused `PartOMixed*` + `PartOIteration3*`: 311/311. Full WPF suite (Release, merged branch): **1,400/1,400**
  on the rerun; the first run had one failure, `PartOWorkflowSimplificationTests.The_progress_window_keeps_its_content_after_standing_aside_for_a_dialog`
  (a progress-window area SAM_UI#135 just changed), which passed 3/3 alone and in the rerun: load-sensitive, not this PR.
- CI #137: build + spdx green.
- **Not met: the ~5,000-space full-year licensed run (PR0 §F)** - blocked by TAS TSD per-zone read scaling (§4.3),
  outside SAM_UI. Owner decision on the follow-up, which is not part of this programme: raise TSD per-zone read
  performance with EDSL; and/or a SAM_Tas change so the Part O routes skip per-zone reads they do not use (at ×30,
  `Overheating` + zone-group peaks were ~43 of the 50 min of "Adding Results"; the bridge and TM59 reads remain).
- Next: merge #137 → SAM_Deploy pin PR (installer build + installed-product smoke on the mixed cooled route) → closeouts.
