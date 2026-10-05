<!-- SPDX-License-Identifier: LGPL-3.0-or-later -->
<!-- Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors -->

# Part O TM59: require a full-year TSD (enable SAM_Tas#73's guard)

**Status (29 Sep 2026): code + tests complete; PR open against `sow/2026-Q3`, awaiting CI/review.** Branch
`fix/parto-tm59-require-full-year-2026-09-29` from `sow/2026-Q3` `6d44ec98`. Depends on SAM_Tas#73 (merged
`7b84dd92` into SAM_Tas `sow/2026-Q3`), which CI clones for this base.

## Why

SAM_Tas#73 measured that TSD answers 8760 hours for any simulated day range and pads the days it did not simulate
with -1. So the length check `TM59AssessmentCalculator.HourCount_Expected` cannot see a part-year simulation: a
days 1..364 TSD reached the Part O TM59 assessment with its last day read as -1 °C. #73 added an opt-in check of the
file's stated day range (`TSDConversionSettings.RequireFullYear`, `firstDay == 1 && lastDay == 365`). It defaults to
off because a part-year TSD is legitimate for generic and Grasshopper conversion. Part O's dynamic method is defined
over a whole year, so this PR switches the check on for Part O only.

## Work completed

- `PartOTM59Assessment`: new internal `PartOTSDConversionSettings()`, which gives the two TM59 series, zones, weather
  and **`RequireFullYear = true`** (a fresh instance per call). `Assess` converts through SAM_Tas's
  `Convert.ToSAM(path, settings, out refusal)` and returns the refusal ("The simulation results at '…' were refused:
  …") before anything is mapped, restored or assessed. An unreadable file keeps its existing message. The public
  `Assess` signature is unchanged; an internal overload takes the conversion, so tests run without TAS COM.
- A comment at `HourCount_Expected` records that the length check still refuses short series but cannot detect a
  part-year simulation.
- Decisions: no day-range logic in SAM_UI; the rule is SAM_Tas's (`Query.FullYearRefusal`). No TM59 rule, generic
  SAM_Tas default, bridge or TSD-performance change. Every Part O TM59 route (`AssessPartOTM59`,
  `OptimisePartOTM59`, `PartOIteration3Pipeline`, `RunPartOStrategySet`) goes through `PartOTM59Assessment.Assess`.

## Files changed

- `WPF/SAM.Analytical.UI.WPF/Classes/PartO/PartOTM59Assessment.cs`
- `WPF/SAM.Analytical.UI.WPF.Tests/PartOTM59FullYearGuardTests.cs` (new, 5 tests)
- `documentation/PartO-TM59-RequireFullYear-PR.md` (this record)

## Validation

- `dotnet test WPF/SAM.Analytical.UI.WPF.Tests -c Debug` against the local SAM_Tas build of #73: **1405 / 1405**.
- New tests (5/5):
  - Part O requests full-year validation and the two series.
  - A full-year TSD is assessed normally: no refusal, a result, and the conversion asked with `RequireFullYear`.
  - A part-year TSD is refused before assessment with SAM_Tas's own `FullYearRefusal(1, 364)` text: no result, no
    report, no space rows. The stand-in would have converted it had Part O not asked.
  - An unreadable TSD keeps its existing message.
  - The generic `TSDConversionSettings` default still accepts a part year and is serialised unchanged.
- **Mutation check:** with `RequireFullYear = false` in the Part O factory, 3 of the 5 fail (request, full-year,
  part-year); restored, 5/5.

## Unresolved / separate

- Damaged or incomplete TSDs that state 1..365 (and TSD.exe hangs) remain a separate robustness item.
- The surface-result day-major investigation and the unanswered EDSL API/cache questions are SAM_Tas items, not
  part of this PR.

## Next step

Merge after green CI.
