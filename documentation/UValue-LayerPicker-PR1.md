# U-value workflow PR1b - specific calculator messages (PR record)

Branch `fix/uvalue-calculator-messages-2026-10-01`, from `sow/2026-Q3` `7c17eab2`. **Not merged.**
`PROJECT_PROGRESS.md` is not touched on this branch (closeout after merge, per `AGENTS.md`).
Plan: [plans/UValue-Workflow-PLAN.md](plans/UValue-Workflow-PLAN.md); brief: [plans/UValue-PR1-PROMPT.md](plans/UValue-PR1-PROMPT.md).

**Depends on SAM_Tas PR1a: [SAM-BIM/SAM_Tas#78](https://github.com/SAM-BIM/SAM_Tas/pull/78)** (type-based layer
picker; `Tas.Query.AdjustableLayerIndex`, which this PR calls). Merge SHA: *pending - to be filled when #78 merges*.
This PR does not compile against a SAM_Tas build older than #78.

## Status

Implemented, unit-tested (full WPF suite green) and accepted in the real app on the pre-fix and post-fix builds.
The PR is not opened until SAM_Tas#78 is merged (CI builds against the base branch of SAM_Tas).

## Problem

`Tools > U Value Calculator` on `SIM_EXT_SLD` failed first time with the generic "Could not calculate construction
for given criteria." for every cause: the default layer was a gas gap (fixed in SAM_Tas#78), an undefined heat-flow
direction, an unreachable target, or Tas not running all gave the same words.

## Change

- New `Query.UValueCalculationMessage` (`WPF/SAM.Analytical.UI.WPF/Query/UValueCalculationMessage.cs`), two
  overloads, both pure and unit-tested:
  - **before the call**, from the confirmed `LayerThicknessCalculationData`:
    - heat-flow direction `Undefined` -> "The heat-flow direction is undefined: <construction> has no default panel
      type. Choose a Heat Flow Direction and try again.";
    - `LayerIndex == -1` **and** `Tas.Query.AdjustableLayerIndex` is -1 -> "No adjustable layer: all layers are
      gas, glass, or thinner than 10 mm." (a cleared selection with an adjustable layer present is left to the
      calculator, which picks one with the same rule);
  - **after the call**, from `LayerThicknessCalculationResult`:
    - no result or `InitialThermalTransmittance` NaN -> "The Tas thermal transmittance calculation is
      unavailable: TCD could not run, so no U-value could be calculated.";
    - `LayerIndex == -1` -> the no-adjustable-layer message;
    - thickness NaN, achieved U NaN, or achieved U more than 0.01 W/m2K from the target -> "Target U <t> W/m2K is
      not reachable by varying <layer name> within <min>-<max> mm."
- `ThermalTransmittanceCalculator_SingleConstruction.cs` shows those texts in the existing `MessageBox` and loops
  back exactly as before. The off-target branch is new: a result whose achieved U is valid but off target used to
  open the result window. Aperture and multi-construction flows keep the generic text.
- No new windows, no Duplicate guard, no restyling (PR2+).

## Classification verified against real results

Real TCD, model copy `SIM_EXT_SLD` (`evidence/.../probe/probe-after.txt`, `probe-before.txt`, harness in `probe/`):

| Case | initial U | thickness | achieved U | Class |
|---|---|---|---|---|
| default layer, U=0.5, **old picker** (layer 2 = air gap) | 0.26 | NaN | NaN | unreachable (the bug), 1.5 s |
| default layer, U=0.5, **new picker** (layer 4 = mineral wool) | 0.26 | 33.8 mm | 0.5 | ok, 0.25 s |
| `LayerIndex = -1` forced (calculator fallback, real COM) | 0.26 | 33.8 mm | 0.5 | ok - fallback also picks mineral wool |
| U=0.05, range 1-1000 mm | 0.26 | 469 mm | **0.051** | ok - hence the 0.01 tolerance |
| U=0.05, range 1-500 mm | 0.26 | 484 mm | 0.05 | ok |
| U=5 (too high) | 0.26 | NaN | NaN | unreachable, 1.5 s |
| explicit layer 0-3, 5, 6, U=0.5 | 0.26 | NaN | NaN | unreachable |
| heat flow `Undefined` | **NaN** | NaN | NaN | looks like "Tas unavailable" -> must be caught before the call |

The last row is why the heat-flow check runs before the call: an undefined direction is otherwise
indistinguishable from TCD not running. "TCD could not run" itself could not be provoked on this machine; it is
covered by the unit tests only.

## Real-app evidence (`documentation/evidence/uvalue-workflow-2026-10-01/`)

Installed-equivalent copies of `SAM_UI\build` under `C:\TasOut\uvalue\app-before` (Tas DLL md5 `0cf4535c...`, the
build in `%APPDATA%\SAM`) and `app-after` (md5 `30f981e7...`, the SAM_Tas#78 build plus this PR), launched on fresh
copies of the model with `/Path=`, driven by UIA (`scripts/`). `before/`, `after/`: `drive.log` (timestamps),
`shots/` (PNG + UIA dump per window).

### Before (pre-fix build)

| Step | Result |
|---|---|
| Tools > U Value Calculator > `SIM_EXT_SLD`, default layer, U=0.5 | Pre-selected layer is **Air 50 mm** (`03-calcdata-u05`). OK -> "Could not calculate construction for given criteria." (`04-failure-box`). On the duplicate `SIM_EXT_SLD 1`: same message after 4.6 s (`13-dup-failure-box`). |
| Re-run with `I01_Mineral Wool` picked | Succeeds in **1.7 s** (both runs): initial U 0.26, mineral wool 80 -> 34 mm, construction 208 -> 162 mm, achieved 0.5 (`14-dup-result`). |
| Apply, then Select > By Construction Name | Selects the 12 `WallExternal` panels (`18-after-bycons`). The saved model still has 12/12 panels on `SIM_EXT_SLD`; the new `SIM_EXT_SLD 1` exists but is unused (`before/model-after-manual-run-summary.txt`). |
| Assign Construction | **Blocked: the "Select Construction" (SearchWindow) OK button does nothing** - OK click, UIA Invoke, Enter and double-click on the item all leave the dialog open (`24`, `27`, `28`); only Cancel closes it. See "Found, out of scope". So "every panel changed" could **not** be confirmed through the UI. |
| Edit > ModelCheck | One **Error**: "Material Library does not contain Material I01_Mineral Wool_20kg/m3_0.025W/mK_0.034m for SIM_EXT_SLD 1 (Construction Layer Index: 4)", plus 7 informational gas-recognition messages (`before/modelcheck.txt`, `29-modelcheck`). The legacy apply adds a construction whose adjusted layer material is not in the model's Material Library. |

### After (new build)

| Step | Result |
|---|---|
| Tools > U Value Calculator > `SIM_EXT_SLD`, **default selection**, U=0.5 | Default layer is now **I01_Mineral Wool** (`01-calcdata-default`). OK -> result window in 2.1 s (incl. ~0.4 s harness overhead): initial 0.26, mineral wool **80 -> 34 mm**, construction 208 -> 162 mm, achieved 0.5 (`03-result`). No layer picking, no error. |
| Unreachable: U=0.05, Max Thickness 0.1 | Message box (4.7 s): "Target U 0.05 W/m2K is not reachable by varying I01_Mineral Wool_20kg/m3_0.025W/mK within 1-100 mm." (`05-unreachable-box`). Dismissing returns to the data window. |
| Heat flow undefined (model copy with `Default Panel Type` = Undefined) | "The heat-flow direction is undefined: SIM_EXT_SLD has no default panel type. Choose a Heat Flow Direction and try again." (`09-undefined-heatflow-box`). Choosing Horizontal and OK then calculates (`12-result-after-choose`, 1.7 s). |
| Glass / argon / glass construction (model copy) | "No adjustable layer: all layers are gas, glass, or thinner than 10 mm." (`16-no-adjustable-layer-box`). |
| Gas-only construction (model copy) | The existing pre-check blocks first: "Calculations interrupted! Gas Material ... recognized as Air" (`14-no-adjustable-layer-box`) - see below. |

The "Tas unavailable" message could not be provoked in the real app (unit tests only).

## Found, out of scope (not changed here)

1. **`SAM.Core.UI.WPF.SearchWindow` OK button has no handler**, and the `MouseDoubleClick` hook is wired to nothing for
   the callers, so `Assign Construction` (and every other `SearchWindow` caller: apertures, internal conditions,
   filters) cannot be confirmed with OK. `button_OK` has no `Click` handler anywhere in the history of `SearchWindow.xaml(.cs)`
   (`git log -S` finds none). Needs its own PR; PR2's apply path replaces the dialog for U-values but not for general assignment.
2. The pre-existing log pre-check in `ThermalTransmittanceCalculator_SingleConstruction` shows the **first** log record
   when any record is an error, so an informational "recognized as Air" line was shown instead of the error
   (`14-no-adjustable-layer-box`). Not touched (message-selection change in a flow PR2 replaces).
3. The legacy apply leaves the adjusted material out of the Material Library (ModelCheck error above) - input for PR2's
   `Modify.SetUValue`, which must add it in the same single Undo step.
4. The 3D context menu already has "Assign Construction By UValue" - relevant to PR2's entry points.

## Tests

- `UValueCalculationMessageTests` (13, `SAM.Analytical.UI.WPF.Tests`): every message above, tolerance edge (0.051
  vs 0.05 is a hit), unknown layer fallback, order of checks, a cleared selection with an adjustable layer present.
- Full `SAM.Analytical.UI.WPF.Tests` (Release): **1618 passed, 0 failed**.
- SAM_Tas PR1a tests (15 new, 1038 total) are recorded in the SAM_Tas PR.
- Build order SAM -> SAM_Tas -> SAM_UI.sln (VS MSBuild, Release); `SAM_UI\build\SAM.Analytical.Tas.dll` md5 equals
  `SAM_Tas\build\` (no stale sibling DLL).

## Files changed

- `WPF/SAM.Analytical.UI.WPF/Query/UValueCalculationMessage.cs` (new)
- `WPF/SAM.Analytical.UI.WPF/Modify/ThermalTransmittanceCalculator_SingleConstruction.cs`
- `WPF/SAM.Analytical.UI.WPF.Tests/UValueCalculationMessageTests.cs` (new)
- `documentation/UValue-LayerPicker-PR1.md` (this record), `documentation/plans/UValue-Workflow-PLAN.md`,
  `documentation/plans/UValue-PR1-PROMPT.md` (committed here as instructed),
  `documentation/evidence/uvalue-workflow-2026-10-01/` (shots, logs, probe, scripts).

## Risks

- A target that Tas achieves only to within 0.01 W/m2K of the request (0.051 for 0.05) is accepted; tighter would
  reject real results.
- Install the new SAM_Tas DLL together with this build: the installer must contain both (installed-app acceptance
  needs an installer containing the new SAM_Tas DLL; this evidence uses folder copies).

## Next step

Merge SAM_Tas#78 (explicit approval), rebuild, fill in its merge SHA above, open this PR against `sow/2026-Q3`,
wait for green CI and for explicit merge approval, then add the `PROJECT_PROGRESS.md` closeout commits in both
repos on `sow/2026-Q3`. PR2 (new U-value window) follows the plan.
