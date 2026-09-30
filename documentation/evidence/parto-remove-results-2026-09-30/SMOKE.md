<!-- SPDX-License-Identifier: LGPL-3.0-or-later -->
<!-- Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors -->

# Remove Results: native smoke (30 Sep 2026): PASS

- **App.** The real `SAM Analytical.exe`, `SAM_UI\build`:
  - `SAM.Analytical.UI.WPF.dll` built 12:54 from this branch;
  - `SAM.Analytical.dll` built 12:48 from the SAM branch `feature/parto-remove-results-2026-09-30`.
- **Driving.** UI Automation drove the app, reusing the #148 driver library. The driver is local in
  `C:\TasOut\parto-remove-results-smoke-2026-09-30\scripts\smokeRR.ps1`.
- **Model.** A byte-identical copy of the owner's run-output model, `SAM_daily\2026-09-29 partOi\model\000000_SAM_AnalyticalModel.sam`
  (SHA-256 `25DD56B6…`), in the disposable folder `C:\TasOut\parto-remove-results-smoke-2026-09-30\model`.
- **No TAS process was started.**

| Step | Observed | Verdict |
|---|---|---|
| Results tab | `Remove Results...` enabled, with its tooltip | PASS |
| Open the window (`shots/02`) | Heading, 9 removed lines (scenarios, provenance, 242 results, 2 design days, `MVHR 1-3`, `MVHR-01..03`, 9 terminals, 20 movements, 8 conditions restored), nothing kept, `✓ PASS — the cleaned copy is a clean Part O baseline`. `Save cleaned copy...` is the default, beside Cancel | PASS |
| Save (dialog captured locally only; it shows internal network drives) | Dialog "Save cleaned copy" in the model's folder, offered name `000000_SAM_AnalyticalModel-Cleaned.sam`, accepted | PASS |
| After save (`shots/04`) | `✓ PASS — the saved copy is a clean Part O baseline` (the file read back), `Saved to …-Cleaned.sam`. Save is disabled; Open cleaned copy and Close are offered | PASS |
| Open cleaned copy, then Simulate > Mixed Design (`shots/05`) | `Baseline: clean — 3 dwellings; 1 other zone…`; Check design and Screen strategies enabled | PASS |
| Files | Before: the model only. After: the model (unchanged, `25DD56B6…`) plus `-Cleaned.sam` (123,004 bytes). Nothing deleted, no sidecar (the app was ended without closing Mixed Design) | PASS |

The first attempt at 13:02 stopped at the save dialog because of a driver defect: UIA found no Invoke pattern on
control id `1`, and a helper was missing. Before stopping, it showed Mixed Design on the uncleaned model with the new
wording: "…save a cleaned copy with Results > Part O > Remove Results and open that, or reopen the pre-Part-O model".
The driver was fixed to send `IDOK` to the dialog, and the rerun above passed. `journey-smokeRR.txt` is the rerun's log.

**Observation, not changed.** The main window title shows the model's internal name (`[000000_SAM_AnalyticalModel]`),
which the copy keeps. So the title does not change when the cleaned copy is opened. This is existing behaviour, the
same as Save As.
