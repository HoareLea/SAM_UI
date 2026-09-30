<!-- SPDX-License-Identifier: LGPL-3.0-or-later -->
<!-- Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors -->

# Part O Mixed Design - licensed real-UI acceptance on the cleaned model (30 Sep - 1 Oct 2026)

**Result: PASS**, after one environmental TPD hang on the first attempt (below). This is the "Mixed Design acceptance
gate" (step 5) of `documentation/PartO-ModelStateArchitecture.md`, run after PR-1..PR-4 merged. Local paths are
redacted: `<SAM-BIM>`, `<SAM_daily>`, `<evidence-run>`.

## What ran

- **Builds (all `sow/2026-Q3` tips, rebuilt locally in Debug, 0 errors):** SAM `137c0bcf`, SAM_Systems `09063b4`
  (PR-3 merge `1893e71`), SAM_Tas `057faf3`, SAM_UI `d9e30ca` (PR-3 merge `5ad0e47`). DLL hashes: `build-hashes.txt`.
  The previous `SAM.Analytical.dll` predated PR-2's final fix, so SAM was rebuilt.
- **Model:** `000000_SAM_AnalyticalModel-Cleaned.sam`, SHA256 `F561161F756A0217...0B78` (`design-sha-before.txt`). The
  real UI opened a hash-verified, byte-identical **copy** in a scratch folder. The owner's folder was snapshotted
  before and after: identical (`snap-owner-folder-*.txt`).
- **Nothing on the design was deleted or altered.** `NV`, `UV`, `MV` (`MV 1`) and `AHU1` stay on it, and the run model
  carries them too (`systems-inventory.txt`).
- **Real `SAM Analytical.exe`, licensed TAS.** Driven through UI Automation (Mixed Design window, real buttons).
- **Selections:** Flat 1 Natural ventilation; Flat 2 Nuaire XBC15, cooling Off; Flat 3 Nuaire MRXBOXAB-ECO5-AECV,
  cooling On.

## Confirmed

| Check | Result |
|---|---|
| Check design succeeds | PASS: "SAM can build this mixed design (3 dwellings - 1 Natural - 2 MVHR - 1 with active cooling ...). The TAS Systems ventilation can be prepared. Nothing was simulated." No refusal. |
| No false `SharedSystem` | PASS. `MV 1`/`AHU1` are not a refusal, in Check or in Build & Run. |
| No NV/UV missing-AHU refusal | PASS. This is the failure the 30 Sep sessions hit (`Ventilation system 'UV' names no air handling unit`); it did not recur. |
| Build & Run reaches the TAS workflow | PASS: thermal source (TAS3D/TBD), TPD conversion, Systems simulation, guidance read-back, bridge, TM59. Timings: `tpd-timing-pass.csv`, `route-timing-pass.csv`. |
| Only intended Part O systems participate | PASS. The Systems route simulated **2** air systems (`MVHR Flat 2`, `MVHR Flat 3`); guidance evidence covers `MVHR Flat 3` only, 8760 h (cooled unit, 80 l/s from guidance). Flat 1 is Natural and no unit-less system was handed to SAM_Systems. Not read directly: the TPD is a binary file, so the two names come from SAM's record and the route's own count. |
| Legacy systems remain on the design | PASS. Design model: `MV`, `NV`, `UV`, `AHU1` only. Run model: those plus `MVHR` x 2 and `MVHR Flat 2/3` units. |
| Design model not replaced or mutated | PASS. The design `.sam` hash is unchanged through Check, Build & Run, close and reopen. The run is a separate model (`..._Mixed_Bridge.sam`). |
| Outputs produced | PASS. `.t3d/.tbd/.tsd/.xml/.tpd`, bridge `.tbd/.tsd`, run model `.sam` (carrying the materialisation record, simulation provenance and its overheating scenarios), guidance operation CSV, timing CSVs. **Final mixed run: PASS - 3 dwellings pass, 0 fail, 0 not assessed, communal corridor acceptable** (`final-outcome-text.txt`). |

## The first Build & Run hung (environmental, not PR-3)

- Attempt 1: the TPD server sat idle for **7,420 s** in `Loading TSD data` (`energyCentre.AddTSDData`), before any
  SAM_Systems graph is read. It normally takes about 20 ms (this run's pass: 1.9 ms). TPD used under 1 s of CPU and had
  no dialog. The UI showed "Creating ventilation systems" for 2 h. I killed the TPD process; the run then failed cleanly
  with `COMException: The RPC server is unavailable (0x800706BA)` (`build1-hung-watch.log.txt`).
- Attempt 2, same inputs and build: **completed in 111 s** (`build2-pass-watch.log.txt`).
- **Cause not identified.** One observation: in attempt 1 a `TBD` process was still alive when TPD started. The retry also
  overlapped TBD processes and passed, so this is a hypothesis only. **Risk:** an intermittent TPD hang has no timeout
  and no cancel path (Cancel waits for the running TAS step). This is a SAM_Tas/TPD robustness item and is not part of
  PR-3 or PR-5.

## Reopen and review behaviour (recorded for PR-5)

1. **Reopen the design model and open Mixed Design.** The run's record is found (the folder and the model's
   `.partomixed.json` sidecar), and each row says "ran as ...", but the selection was never saved onto the design file,
   so "Final" reads **STALE**: "A selected dwelling strategy has changed since the model was materialised." It fails
   closed. Choosing the same strategies again and pressing **Save selection** makes the same result **current**
   ("PASS ... The final mixed run is current for this design"), with no re-run. So the link design -> run is
   re-derived from the path convention plus the fingerprint, not stored.
2. **Open the run model (`..._Mixed_Bridge.sam`) directly.** Mixed Design is blocked (PR-4 behaviour): "This is a Part O
   result. Part O cases run from a design model - open the design model ... reopen the pre-Part-O model." It says
   **which** design model to open nowhere. The result carries only `Fingerprint_Baseline`, a hash, and no reference to its
   design file. This is the gap PR-5's persisted `BaselineReference` closes. Opening it changed no file.
3. `reports/` in the case folder is empty after the run. The TM59 outcome is in the UI and in the run model; I did not
   compare this with earlier Mixed runs, so it may be by design.

## Status / next step

- **Status:** docs/evidence only, no production code. Open PR, not merged; `PROJECT_PROGRESS.md` is updated only after merge.
- **Unresolved:** the intermittent TPD `AddTSDData` hang (above); the design -> result link is not persisted (PR-5).
- **Next step:** PR-5 (BaselineReference / derived-from persistence). Do not start PR-6.

## Files

`ACCEPTANCE.md`, `drive.log.txt` (UI driver log: states, selections, outcomes), `build1-hung-watch.log.txt`,
`build2-pass-watch.log.txt`, `build-hashes.txt`, `design-sha-before.txt`, `snap-*.txt` (file hashes before and after),
`tpd-timing-pass.csv`, `route-timing-pass.csv`, `systems-inventory.txt` with `inspect_systems.py`,
`final-outcome-text.txt`. No screenshots are committed (they show local paths).
