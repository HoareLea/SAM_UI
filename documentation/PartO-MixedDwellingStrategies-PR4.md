<!-- SPDX-License-Identifier: LGPL-3.0-or-later -->
<!-- Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors -->

# Part O — mixed dwelling strategies: PR4 (large-project acceptance and deploy)

**Status (28 Sep 2026): DRAFT - large-project evidence in progress.** Branch `feature/parto-mixed-pr4-2026-09-28` from
`sow/2026-Q3` `52217c3`. Builds against SAM `bc85ba61`, SAM_Systems `fbef48f`, SAM_Tas `5753ad2` (docs-only over the
PR3C build SHAs `3d6fa80a` / `005c4fe` / `e7cc0ed`). No production code change.

## 1. Scope (from the records, not inferred)

- SAM `PartO-MixedDwellingStrategies-PR0.md` §F: *"PR4 — real TAS acceptance and deploy. Scope: a full-year mixed run on
  a large project (hundreds of dwellings, ~5,000 spaces); SAM_Deploy pins. Acceptance: the TM59 per-dwelling outcome
  matches the selected strategies; no `.sam` growth across re-materialisations; materialisation-time budget met at
  scale."* §H: "A PR4 budget should be measured, not assumed."
- SAM_UI `PartO-MixedDwellingStrategies-PR3A.md` §14.1/§8: the large run includes cooled dwellings; "scale behaviour of
  TPD with hundreds of air systems is a PR4 gate". "Systems route as the single final route" is a separate post-PR4
  proposal - **not PR4**.
- PR3C §6 limitations were triaged, not adopted as scope (§6 below).

PR4 therefore closes: evidence that the completed mixed workflow (clean baseline + strategies + accepted design +
cooling intent → deterministic materialisation → ONE mixed model → ONE route → TM59) stays correct and practical at
large-project scale, each layer measured separately; and the SAM_Deploy pins that ship PR3B/PR3C.

## 2. Deployment gap found

SAM_Deploy `sow/2026-Q3` (`e162a25`) pins SAM `3d6fa80a`, SAM_Systems `005c4fe1`, SAM_Tas `e7cc0ed4` - the PR3C build
SHAs - but SAM_UI `8971cfb0`, which does **not** contain PR3C (`453ca94`), and SAM_Tas_Grasshopper `a4d4f73`, which does
not contain SAM_Tas_Grasshopper#7 (mixed-building diagnostic log name, PR3B follow-up). The shipped application today
has no per-dwelling cooling. Closed by the SAM_Deploy PR that follows this one.

## 3. The large fixture

No large residential project exists on this machine (largest `.sam`: a 10,218-space office-cell model without
dwellings; Earls Court is panels only). The large project is the real PR2 clean baseline
(`SAM_zoningAM-CIBSEfutureZ1-MixedBaseline.sam`, 3 flats + corridor, 9 spaces, SHA `89A8AC7B…9446`) replicated on a
grid (`PartOMixedLargeProjectFixture.Replicate`): each block its own spaces, panels, apertures and zones with fresh
guids and names (`Flat 1 #12`), its Part F data renamed with it; constructions, internal conditions and the heating /
cooling systems shared. Blocks are 150 m × 100 m apart, so no block shades or adjoins another and the original block's
TM59 outcome is every block's reference. (Precedent: PR2F replicated a 9-space model to 4,995 spaces.)

## 4. Evidence by layer

### 4.1 No TAS, 5,000 spaces (`PartOMixedLargeProjectTests`, Release, this machine)

500 dwellings × 10 spaces + corridor; 167 Natural / 167 MVHR / 166 MVHR + cooling by bulk assignment:

| Layer | Measured |
|---|---|
| Session open / bulk assignment | 678 ms / 15 ms (window: 15 rows realised of 500 - PR2 §8, unchanged) |
| SAM mixed materialisation (with templates) | 4.1 s |
| SAM_Systems: ONE graph, 333 air systems, 166 guidance-cooled | 7.8 s; DX coil on exactly the 166 cooled units' air systems, 0 elsewhere (71 ms check) |
| Build & Run orchestration (TAS stand-in) | 3.2 s |
| Evidence `IsCurrent` (a reopened project) | 0.2 s |
| `.sam` growth | none: saved model 6,204,679 chars and materialised 11,099,410 chars identical after rebuild, cooling off → on |

Plus scenarios truthful for all 500 dwellings + corridor, the clean source untouched, one simulation of one model.

### 4.2 Licensed TAS, production route (`PartOMixedLargeProjectAcceptance`, env-gated)

Production `BuildAndRunPartOMixedDesign` with the production simulators; per block Flat 1 Natural, Flat 2 MVHR
(automatic product), Flat 3 MVHR + Nuaire MRXBOXAB-ECO5-AECV (MR-ECO-COOL-V) + cooling on; installed catalogue
(SHA `D3878908…58D8`); Z1 DSY1 2050s HIGH90, TAS solar, full year.

| Blocks | Dwellings / spaces | MVHR air systems / cooled | Thermal source | TPD (convert + simulate + read-back) | Bridge | TM59 | Total |
|---|---|---|---|---|---|---|---|
| 1 (PR3C) | 3 / 9 | 2 / 1 | | | | | 6.7 min |
| 10 | 30 / 90 | 20 / 10 | 5:40 (shading 3:18) | 12:15 | 9:31 | 0:26 | 27:54 |
| 30 | | | | | | | |

×10 result: ACCEPTANCE PASSED - route Systems; 10 cooled dwellings, only Flat 3s; ONE TPD; TAS guidance read-back for
10 units; no unit supply setpoint on the analytical model; every Natural dwelling `BaseNaturalVentilation`, uncooled
MVHR `BasePassive`, cooled `ActiveTrimCooling`, 10 corridors `DwellingIndependent`; no space unassessed; **all 30
dwellings have their block reference's TM59 outcome** (Flat 1 Pass; Flat 2 Fail - Bedroom 2_3, Kitchen_4; Flat 3 Fail -
Kitchen_7: the PR3C single-block result); run model reopens and restores in 1.0 s; re-materialised from the reopened
baseline with the same size; baseline not written by the run; run current against it; source fixture unchanged.
Output: TSD 161 MB, bridge TSD 163 MB, bridge TBD 15.8 MB, TPD 0.7 MB, guidance CSV 7.2 MB, run model 0.75 MB.

## 5. Files

- Tests: **new** `PartOMixedLargeProjectTests.cs` (2 facts), **new** `PartOMixedLargeProjectFixture.cs`, **new** env-gated
  `PartOMixedLargeProjectAcceptance.cs`; `PartOMixedCoolingTests.cs` (fixture product members `private` → `internal`).
- This record; evidence logs `documentation/evidence/parto-mixed-pr4/`.

## 6. PR3C limitations - triage

| Item | Decision |
|---|---|
| No accepted Optimised + cooled fixture inside 60-120 l/s | Retained limitation (no such accepted design exists; not manufactured) |
| Native TPD DX coils not COM-read-back in PR3C | Acceptance only: DX coils per air system checked on the real SAM_Systems graph at 333 air systems (§4.1); TAS read-back of every guidance unit at scale (§4.2) |
| `.tpd` / `_GuidanceOperation.csv` left after a later IZAM rebuild | Retained known limitation (unreferenced; a stale Systems result is never shown as current) - follow-up |
| Matrix columns overflow the default 1240 px window | Retained pre-existing limitation - follow-up |
| No cooling during screening | By design (PR3A §8) |
| "Systems route for all dwellings" | Separate post-PR4 proposal (PR3A §14.1) |

## 7. Next step

Complete the licensed scale evidence, open the PR, then the SAM_Deploy pin PR.
