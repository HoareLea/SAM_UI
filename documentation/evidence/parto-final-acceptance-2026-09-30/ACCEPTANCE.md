<!-- SPDX-License-Identifier: LGPL-3.0-or-later -->
<!-- Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors -->

# Part O final real-app acceptance before presentation (30 Sep 2026)

**Status: complete. The Part O output folders PASS for 1a, 1b, 2 and Iteration 3 (against both 1a and 2), including reopen.
Mixed Design was refused, correctly, because the owner's model is not a clean baseline. Nothing was merged. `PROJECT_PROGRESS.md`
is untouched.**

## Setup

- **App.** The real `SAM Analytical.exe` dev build (`SAM_UI\build`, 2026-09-30 09:22), built from `fe6ddd2`. That is the code SAM_UI#147 merged as
  `4971a3f`: `git diff fe6ddd2 4971a3f` is empty outside `documentation/`. SAM is at `83eb79a3` and SAM_Tas at `057faf3`. TAS is licensed.
- **UI driving.** UI Automation drove the app (drivers in `scripts/`). Every Hub action was a real button press, and only one-button boxes were
  answered.
- **Source.** `<SAM_daily>\2026-09-29 partOi\000000_SAM_AnalyticalModel.sam` (SHA-256 `25DD56B6…E572`, saved 29 Sep 23:12). All work
  used a byte-identical copy in `model\`.
- **Part O root.** `…\2026-09-29 partOi\PartO`, empty at the start and typed into the Hub. When the Hub opened, its box held the model folder.
- **Snapshots.** Taken before, between and after every phase, then compared (`snapshot-diffs.txt`):
  - the working area, with SHA-256;
  - the other 169 SAM_daily files, with SHA-256;
  - 9,180 `C:\TasOut` files, by size and write time.

### What the source model is

The source is the saved output of an earlier Part O run, not a clean baseline (`baselinescan.txt`, `runstate.txt`). It carries:
- 4 overheating scenarios (Flats 1-3 `BasePassive - MVHR`, corridor `DwellingIndependent - UV`);
- a `SimulationResultProvenance` pointing at `…\2026-09-29 partOi\000000_SAM_AnalyticalModel.tsd`, a file that no longer exists;
- 242 simulation results and 2 cluster design days;
- 3 Part O MVHR ventilation systems;
- per-space Part F internal conditions.

Its only absolute link points into its own folder, not into any historical folder, so it was safe to run the normal cases from a copy. When the Hub
opened, it said "Previous Part O run is no longer valid — no results to review". Review Results was disabled, and its tooltip named the missing TSD
(`shots/01`).

## Phase 2: Prepare & Run

The runs were made in one session, in this order:

| # | Case (Hub scenario) | Time | Result |
|---|---|---|---|
| 1 | Iteration 1a — MVHR design duty (no manufacturer unit) | 1.3 min | TM59 PASS |
| 2 | Iteration 3 against 1a, method "Selected product — manufacturer operating guidance" | 3.2 min | reference PASS / system PASS, no refusal |
| 3 | Iteration 1b — Natural ventilation | 1.2 min | TM59 PASS, no model-check refusal |
| 4 | Iteration 2 — MVHR with manufacturer unit (automatic, all catalogue products) | 1.4 min | TM59 PASS |
| 5 | Iteration 3 against 2, same method | 3.0 min | reference PASS / system PASS, no refusal |

Iteration 2B was not run, and no `Iteration2B/` folder was created.

Iteration 3 runs against the Hub's current run, which is why it ran straight after 1a. Both Iteration 3 runs used the Hub's default method. The
driver's method-radio lookup found no radios; the comparison headings confirm the method. No reference TSD was copied: each record points at its
reference where it is.

**Folder result (`crosscheck-runs.txt`, `pathscan-runs.txt`):**

| Check | Result |
|---|---|
| Each stage creates files only in its own case folder: 1a → `Iteration1a/`, It3 → `Iteration3/`, 1b → `Iteration1b/`, 2 → `Iteration2/`, It3 → `Iteration3/` | PASS |
| Placement. `tas/` holds `.xml .t3d .tbd .tsd .sam .partorun.json .prepared.sam`, plus `.tpd` and the bridge `.tbd/.tsd/.sam` for Iteration 3. `reports/` holds `-TM59.txt` and the Iteration 3 record and review `.txt/.json`. `diagnostics/` holds every `*.timing.csv` and `-OperatingAirFlow.csv`. No timing CSV is left in `tas/`. | PASS (0 misplaced) |
| Nothing is loose in the root, which holds only the 4 case folders | PASS |
| `PartOCase.json` is exactly `{"Schema":"SAM.PartOOutputCase/1","Case":"<case>"}` | PASS |
| `-It1a` separation. The 1a pairing is `…-It1a-It3BMG*` / `…-It1a-Iteration3-MG*`, and the 2 pairing is unqualified. The two pairings' 16-17 files are disjoint. Each record references only its own reference case and `Iteration3`. | PASS |
| Later cases leave earlier artefacts alone. Only two changes: each Iteration 3 re-assessed its own reference and rewrote that reference's `-TM59.txt`, whose header now reads "Iteration 3 … · reference case" (same source TSD, same PASS). This is documented behaviour. | PASS |
| No absolute path outside the root, except the read-only product catalogue and the source model's own stale TSD link, which was copied into 1a's `.prepared.sam` | PASS |

## Reopen: a new session per case

Each session opened `<case>/tas/000000_SAM_AnalyticalModel.sam`, went Hub → Review Results → Open result (Iteration 3), with a TAS-process watch
running.

| Case | Hub | Review Results | Iteration 3 Open result | TAS processes seen | Result |
|---|---|---|---|---|---|
| Iteration1a | "Saved Iteration 1a results reopened" | TM59 PASS, from `Iteration1a\tas` | "reopened … no TAS simulation was run", `-It1a` system case | TSD ×3 | PASS |
| Iteration1b | "Saved Iteration 1b results reopened" | TM59 PASS, from `Iteration1b\tas` | n/a (no pairing) | TSD ×1 | PASS |
| Iteration2 | "Saved Iteration 2 results reopened" | TM59 PASS, from `Iteration2\tas` | "reopened … no TAS simulation was run", unqualified | TSD ×3 | PASS |

- No TAS simulation ran on reopen: only TSD (the results reader) was seen, with no TBD, TAS3D or TPD.
- All `tas/` files were unchanged.
- The reopen rewrites were exactly the documented ones:
  - the TM59 reports, byte-identical (only the write time moved);
  - each pairing re-saved its own review `.txt/.json` in the reopened "review of a persisted pairing" form (45 KB → 26 KB).

## Mixed Design: attempted last

Mixed Design was tried on the resulting model (`Iteration2/tas/000000_SAM_AnalyticalModel.sam`, `shots/08`). Earlier it was tried on the source copy
(`shots/07`). Both times SAM refused it: "The open model is NOT a clean Part O baseline". Check design, Save and Build & Run were disabled.

Nothing was bypassed and no persistence rule was changed. The six signals, from `SAM/SAM.Analytical/Query/PartOBaselineFindings.cs`:

| Group | Signal in this model |
|---|---|
| Run output | overheating scenarios (4) |
| Run output | a `SimulationResultProvenance` record |
| Run output | 242 simulation result objects in the cluster (`SpaceSimulationResult` etc.) |
| Run output | 2 design-day records in the cluster |
| Materialised | 3 Part O MVHR ventilation systems |
| Materialised | spaces carrying the per-space Part F internal condition `<condition> - <space>` |

The last two reasons are hidden behind "…and 2 more" in the window.

**What Mixed Design requires:** the pre-Part-O design model, with none of the six signals. That model still has its geometry, constructions, openings,
authored internal conditions, dwelling zones, Part F requirements, design terminals, weather and model-level design days. The PR2 fixture
`SAM_zoningAM-CIBSEfutureZ1-MixedBaseline.sam` is such a model: it has zero of each signal (`baselinescan.txt`). A materialised or simulated model is
never cleaned back; Mixed Design needs the model saved before the first Prepare & Run.

**By design, closing Mixed Design writes `<model>.partomixed.json` beside the open model**, even when the baseline is refused
(`RunPartOMixedDesignCommand.cs`). This probe therefore left one 213-byte file, `Iteration2/tas/000000_SAM_AnalyticalModel.partomixed.json`. It is
default constraints only, with no paths. It is the only file outside the case-owned set, and it was left in place as evidence.

## Outside the root

No existing file outside `PartO` changed:
- 0 changes in the other SAM_daily files;
- 0 changes in `C:\TasOut`;
- the source `.sam` and its copy are hash-identical to the start.

The app's remembered Part O folder (user settings) is now this root.

## Verdicts

| Workflow | Verdict |
|---|---|
| Iteration 1a | PASS |
| Iteration 1b | PASS |
| Iteration 2 | PASS |
| Iteration 3 against 1a (`-It1a`) | PASS |
| Iteration 3 against 2 | PASS |
| Reopen (all three) | PASS |
| Mixed Design output folders / per-flat selections | INCONCLUSIVE: not runnable. The model is not a clean baseline, and the refusal is correct. |
| Iteration 2B | not run (out of scope) |

## Risks and observations (none a folder defect)

1. **Presentation model.** Mixed Design needs the clean pre-Part-O model. Every case here also inherited the source's run state: its 3 MVHR
   systems are in every run model, including 1b (`runstate.txt`). Results from this model are therefore not results from a clean source.
2. **`.prepared.sam` carries the previous run's scenarios and result link.** For example, 1b's links to `Iteration1a\tas\….tsd` and 2's to
   `Iteration1b\tas\….tsd`, because preparation copies the open model. No write crossed cases, and the final `.sam` of each case points at its own
   TSD. Whether any reader trusts a `.prepared.sam`'s provenance was not verified; it is worth a look.
3. **The Mixed Design refusal banner truncates** ("…and 2 more"), and the two materialisation reasons cannot be read in the UI.
4. **Opening Mixed Design on a result model drops a `.partomixed.json` into that case's `tas/`** (point above). It is harmless but visible in a demo.
5. **The reopened Iteration 3 review report replaces the in-session one** with the shorter review-of-record form.

## Next step

Replace the source with the clean pre-Part-O model and run Phase 1 (Mixed Design into `PartO/MixedDesign/`) with the prepared drivers
(`scripts/m*.ps1`, `mixedprobe.ps1`):
- Flat 1: Natural;
- Flat 2: MVHR + product, cooling off;
- Flat 3: MVHR + Nuaire MRXBOXAB-ECO5-AECV, cooling on.

Then reopen it.
