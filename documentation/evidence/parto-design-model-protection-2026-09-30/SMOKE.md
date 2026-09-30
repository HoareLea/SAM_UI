<!-- SPDX-License-Identifier: LGPL-3.0-or-later -->
<!-- Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors -->

# PR-4 native smoke (30 Sep 2026): no TAS

The real `SAM_UI\build\SAM Analytical.exe` was driven through UI Automation by `scripts/smokePR4.ps1`. That script
reuses the SAM_UI#148 driver library (`lib.ps1`, `uia.ps1`, `win32.ps1` and `invoke.ps1` under
`C:\TasOut\parto-pr4-smoke-2026-09-30\scripts`).

The script only opens and closes windows. It runs nothing, and it presses nothing except the ribbon buttons and
Close. No TAS process ran in either part: the `TBD|TPD|TSD|Tas*` process list read "none" before and after.

| Part | Model | Hub | Mixed Design | File |
|---|---|---|---|---|
| **result** | The saved Iteration 1a run model from the #147 acceptance root (`…\Iteration1a\tas\000000_SAM_AnalyticalModel-It1a-futureZ1.sam`), opened with `/Path=` | **Prepare & Run disabled.** The blocker and its tooltip read "Run is unavailable: This is a Part O result. Part O cases run from a design model — open the design model. (It records the overheating scenarios or the simulation results of a Part O run; it carries systems or conditions a Part O preparation built.) Review Results still shows this result's assessment. …Remove Results…". **Review Results is enabled.** The outcome line reads "Saved Iteration 1a results reopened — ready to review". The Iteration 3 review stays available. | The baseline line starts "This is a Part O result. Part O cases run from a design model — open the design model." and then gives SAM's findings. | SHA-256 `7092797A…` before and after (unchanged) |
| **design** | A copy of the Remove Results smoke's `-Cleaned.sam`, saved as `model\Design.sam` | **Prepare & Run enabled**, with no blocker. Review Results is disabled ("No Part O run is available to assess"), which is correct because no run exists. | "Baseline: clean — 3 dwellings" | SHA-256 `6F1915F4…` before and after (unchanged) |

The files are `journey-result.log`, `journey-design.log` and `shots/` (PNG captures plus a UI Automation text dump of
each window).

## The defect the first run found

The first `result` run (15:08) showed the Hub with **Prepare & Run enabled and no blocker**, while Mixed Design already
refused correctly.

The cause: `PartOWorkflowWindow` rebuilds a `PartOWorkflowCapabilities` field by field for every inspection, and that
rebuild dropped the new `DesignModelRefusal`. The in-process tests had passed because they call
`PartOWorkflowInspection.Inspect` directly.

The fix:
- the window now carries the field;
- a window-level test was added
  (`The_Hub_window_blocks_Run_on_an_opened_result_and_still_offers_its_review`), and it fails with the line removed.

The second run (15:11), shown above, is the passing one.

## Not covered here

- A licensed TAS Prepare & Run followed by Save in the real UI was not done. The owner asked not to run TAS unless it
  is necessary.
- The same production path, from the accepted review through `SimulatePartO`, the 2B command and Save, is covered in
  process with TAS replaced at its one workflow seam (`PartODesignModelProtectionTests`).
