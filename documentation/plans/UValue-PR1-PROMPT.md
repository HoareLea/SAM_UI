Implement PR1 of the U-value workflow: fix the default layer picker so "Tools > U Value Calculator" works first time,
and give specific error messages. Two PRs: SAM_Tas (PR1a) first, then SAM_UI (PR1b).

## Read first
1. `SAM-BIM\AGENTS.md` and `SAM_UI\AGENTS.md` (continuity rules: PR record doc + PR description during the PR;
   `PROJECT_PROGRESS.md` only after merge, as a docs-only closeout commit on the base branch; never on a PR branch).
2. `SAM_UI\documentation\plans\UValue-Workflow-PLAN.md`: the approved plan. Follow it, especially "Root cause",
   "Execution model" (failure classification) and "PR boundaries".
3. `PROJECT_PROGRESS.md` in SAM_UI and SAM_Tas.
4. Memory notes on SAM_UI build traps, driving SAM Analytical.exe via UIA, and the installed-product smoke / MSIX trap.

The base branch for both repos is `sow/2026-Q3`. Fetch it first: local is behind (SAM_UI#158/#159 merged).
`UValue-Workflow-PLAN.md` and this prompt may be untracked in the SAM_UI working tree. Commit both on the PR1b branch.

## Facts already verified (do not re-derive)
- `SAM_Tas\SAM_Tas\SAM.Analytical.Tas\Create\LayerThicknessCalculationData.cs` picks the lowest-conductivity layer of
  10 mm or more. In `C:\TasOut\uvalue\model.sam` (a copy; never touch the user's original), wall `SIM_EXT_SLD` is:
  Air 50 mm (`Ar90Up_Air__50mm_1.25W/m2K`) / cement particleboard 12 mm / mineral wool 80 mm
  (`I01_Mineral Wool_20kg/m3_0.025W/mK`) / Air 50 mm / rainscreen 3 mm.
- The model's `GasMaterial`s have positive conductivity (0.024, 0.01622 W/mK), below the mineral wool's 0.025, so gas
  wins. NaN never wins.
- The fallback in `Classes\ThermalTransmittanceCalculator.cs` (`layerIndex == -1`, around lines 322-336) only skips
  conductivity <= 0, has no 10 mm filter, and would pick gas too.
- `Tas.Modify.Run` swallows all exceptions (`catch {}`). `Calculate_ByDivision` returns NaN when the target is not
  bracketed, which gives the generic "Could not calculate construction for given criteria."
- SAM_UI references SAM_Tas as a pre-built DLL (`..\..\..\SAM_Tas\build\SAM.Analytical.Tas.dll`).

## PR1a: SAM_Tas (branch `fix/uvalue-layer-picker-2026-10-01` off `sow/2026-Q3`)
- **Picker** in `Create.LayerThicknessCalculationData`:
  - exclude layers whose material is gas or transparent, by type (`Core.Query.MaterialType(IMaterial)` /
    `GasMaterial` / `TransparentMaterial`), not by conductivity;
  - keep the >= 10 mm rule, and skip conductivity NaN or <= 0 as a secondary guard;
  - when no layer qualifies, return `LayerIndex = -1` (no gas fallback).
  - Prefer one small shared helper (for example a `Query` method returning the adjustable layer index) over duplicated
    loops.
- **Calculator fallback** (`layerIndex == -1`) applies the same rule on the TCD side: skip `TBD.MaterialTypes`
  gas/transparent layers (see `Convert\ToSAM\Material.cs` for the enum use), keep the >= 10 mm rule and conductivity > 0.
  If nothing qualifies, return the existing controlled result (`LayerIndex -1`, NaN thickness) rather than throwing.
  Do not change the behaviour for an explicit, user-chosen `LayerIndex`.
- **Tests**:
  - prefer `SAM.Analytical.Tas.TM59.Tests` if it can reference the built `SAM.Analytical.Tas.dll` without COM build
    problems; otherwise put them in `SAM_UI\WPF\SAM.Analytical.UI.WPF.Tests` (which already references it) as part of
    PR1b, and say so in both records;
  - case: this exact wall with the model's conductivities chooses mineral wool;
  - case: gas and transparent layers are never chosen, even when they have the lowest conductivity;
  - case: an only-gas/glass construction gives `LayerIndex -1`;
  - case: layers under 10 mm are skipped.
- Create a PR record doc in SAM_Tas following existing conventions, open the PR against `sow/2026-Q3`, and wait for
  green CI.
- **Merge only with my explicit approval.**

## PR1b: SAM_UI (branch `fix/uvalue-calculator-messages-2026-10-01` off `sow/2026-Q3`, after PR1a merges)
- In `WPF\SAM.Analytical.UI.WPF\Modify\ThermalTransmittanceCalculator_SingleConstruction.cs`, replace the generic
  message with classified ones.
  - Before the call:
    - heat-flow direction undefined: the construction has no default panel type;
    - `LayerIndex == -1` from the picker: "No adjustable layer: all layers are gas, glass, or thinner than 10 mm".
  - After the call:
    - `InitialThermalTransmittance` NaN: the Tas calculation is unavailable (TCD could not run);
    - initial U valid but thickness/calculated U NaN, or achieved U off target beyond tolerance: "Target U not reachable
      by varying <layer> within <min-max> mm".
  - Verify this classification against real results before relying on it.
  - Keep the existing `MessageBox` / loop-back behaviour; PR2 replaces this window. No new windows, no Duplicate guard,
    no restyling.
- Rebuild in order (SAM, then SAM_Tas, then SAM_UI) and check for stale sibling `build/` DLLs. Run the full WPF test
  suite.
- **Real-app evidence** goes in `documentation/evidence/uvalue-workflow-2026-10-01/`: screenshots, UIA dumps, timings.
  Start from `C:\TasOut\uvalue\uia.ps1` and `documentation/evidence/parto-final-acceptance-2026-09-30/scripts/uia.ps1.txt`.
  - **Before** (current installed build), on a fresh copy of the model:
    - Tools > U Value Calculator on `SIM_EXT_SLD`, default layer, U=0.5: record the failure;
    - re-run with `I01_Mineral Wool` picked manually: record the result and the calculation time;
    - Select > By Construction Name, then Assign Construction: confirm every panel changed;
    - Edit > ModelCheck: record what it reports.
  - **After** (new build or installer containing the new SAM_Tas DLL):
    - U=0.5 succeeds first time on the default selection, and the record shows the chosen layer and thickness;
    - capture one classified failure message, for example an unreachable target like U=0.05.
- Create the PR record doc `documentation/UValue-LayerPicker-PR1.md`. State the dependency on the SAM_Tas PR (number
  and merge SHA). Open the PR against `sow/2026-Q3` and wait for green CI.
- **Merge only with my explicit approval.** After each merge, add the `PROJECT_PROGRESS.md` closeout in that repo on
  the base branch.

## Out of scope
The new U-value window, glazing, Constructions-list columns, restyling, and the Duplicate guard (PR2-PR4 in the plan).

## Stop and ask me if
- the fix appears to need a change in SAM (core);
- `Tas.Modify.Run` must change to classify failures;
- the real-app result contradicts the root cause above.
