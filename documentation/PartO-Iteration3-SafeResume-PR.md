<!-- SPDX-License-Identifier: LGPL-3.0-or-later -->
<!-- Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors -->

# Part O Iteration 3: safe retry without re-running TAS (Follow-up #3)

**Status (29 Sep 2026): code, tests and licensed acceptance complete; PR open against `sow/2026-Q3`, awaiting
CI/review. Not merged.** Branch `feature/parto-safe-resume-retry-2026-09-29` from `sow/2026-Q3` `4f981d81` (the
Follow-up #2 closeout; base had not moved). SAM_UI only - SAM, SAM_Tas and SAM_Systems are unchanged.

**Outcome B - safe same-process retry only.** A retry in the same SAM session reuses the Iteration 3 TAS work
that the earlier attempt completed, but only where the run can prove that work is still its own. A retry after
SAM is restarted runs TAS again, and says why.

## The problem, as it was

An Iteration 3 attempt runs its TAS work in this order: thermal source, then TAS Systems route (with the
manufacturer-guidance or cooling read-back), then resultant-temperature bridge. After that it runs Candidate B's
TM59, reconciliation, comparison and persistence. The outputs those later stages need are held only in memory
until the Persistence stage:

- the no-IZAM model carrying Candidate B's TAS zone identities;
- the route's room and connection bindings;
- the provider's resultant-temperature series.

So when a later stage failed, "Run Iteration 3 again" started from scratch. It materialised again, ran the thermal
source, the TAS Systems route and the bridge again, and only then retried TM59.

This was demonstrated before the fix. The three `A_retry_after_*` tests failed with the second attempt calling
`Materialise, ThermalSource, Route, ResultantTemperatures` again.

Iterations 1a/2 were already correct and are unchanged. TAS completes the run (`PartORun.CanAssess`), and a TM59
failure leaves it assessable. The Hub's Review Results re-assesses the existing TSD without TAS, both in session
and after reopening the `.sam` (`PartORun.Restore`).

## Resume contract

`Query.PartOIteration3ResumePlan(partORun, method)` is the only place reuse is decided. It is made once per
attempt, before the progress window opens.

- **SAFE (reused)** needs all four of these:
  1. **The run holds kept work for this method.** This is `PartORun.Iteration3Checkpoint`, cleared by every
     transition that drops or replaces the run: model change, re-prepare, restore, reset, and reference results
     rewritten or gone. The work is kept the moment the last TAS stage completes, before anything that can fail
     after it (refusal, cancel or exception).
  2. **What the attempt is built from is unchanged** (`PartOIteration3ResumeIdentity`): method; Reference A's
     results file (path, and current length + write time); Reference A's recorded design and scenario
     fingerprints; the TAS case string; the prepared design's fingerprint; dwelling-zone and prepared-system
     identities; and, for a product method, the catalogue SHA-256.
  3. **Every TAS file the attempt claimed is present with the recorded length and write time**
     (`PartOIteration3FileRecord.Current`, the rule the review already trusts): thermal-source TBD/TSD, TPD,
     guidance/cooling CSV, bridge TBD/TSD.
  4. **Every kept result is still complete.**
- **UNSAFE or UNKNOWN (TAS runs).** Anything else, including a value missing on either side: an identity with a
  hole never matches. The reason is shown in words. There is no partial reuse; the TAS stages feed each other.
- **Inside a resumed attempt**, the equipment resolution runs again and must match the kept work's (catalogue SHA
  and air-handling-unit sets). Each TAS file is verified again instead of being claimed. A mismatch refuses by name
  and discards the kept work, so the next attempt runs TAS.

## What a resumed attempt does

- **Runs as normal:** Input, Reference A re-read and re-assessed, scope, equipment resolution, then Candidate B TM59,
  reconciliation, comparison and persistence.
- **Reuses:** the materialisation, thermal source, route and resultant temperatures. No TAS pipeline member is
  called.
- **Records the reuse:** each reused stage's ledger detail starts "Reused from the attempt of <time>; TAS was not
  run again and its files are unchanged." The TAS artifacts are listed as reused, never as written.
- **Shows the reuse:**
  - the progress window lists Preparing the system case → Reusing the completed TAS results → Assessing TM59 →
    Comparing and saving results, and the subtitle says TAS is not run again;
  - the Hub's line says "TAS results reused from the attempt of …";
  - after a stop, the line says the TAS results are kept and will be reused.
- **Afterwards:** a completed pairing keeps nothing, so running a completed method again is a deliberate new run
  and runs TAS.

## Fresh process: why not, exactly

Proving the stages after TAS in a new process needs three things that are persisted only when a pairing completes:
Candidate B's no-IZAM workflow model (the only one carrying its TAS zone identities), the route's bindings, and the
provider's resultant-temperature series. A refused record names the TAS files but none of those three. Supporting a
fresh-process resume would mean persisting SAM_Tas result objects before TM59, which is a new persistence format,
not a narrow field.

So a later session never reuses from disk. Where it finds a refused record that got past its TAS work, it says so:
"…can be proven only in the SAM session that produced them…".

## Files changed

- `SAM_UI/SAM.Analytical.UI/Classes/PartO/PartORun.cs`: per-method kept-work slot (opaque; the WPF layer owns the
  type), cleared in `InvalidateCore`.
- `WPF/SAM.Analytical.UI.WPF/Classes/PartO/PartOIteration3Checkpoint.cs` (new): `PartOIteration3ResumeIdentity`
  and `PartOIteration3Checkpoint`.
- `WPF/SAM.Analytical.UI.WPF/Query/PartOIteration3ResumePlan.cs` (new): the resolver.
- `WPF/SAM.Analytical.UI.WPF/Modify/RunPartOIteration3.cs`: plan at Input; reuse at Materialisation, ThermalSource,
  route, CSV and ResultantTemperature; kept after the last TAS stage; dropped on completion. The public signature is
  unchanged and an internal overload takes the plan.
- `WPF/SAM.Analytical.UI.WPF/Modify/PartOIteration3.cs`: resumed phase list, mapping, details and announcer; Hub
  subtitle and outcome lines.
- `WPF/SAM.Analytical.UI.WPF/Classes/PartO/PartOProgressStages.cs`: "Reusing the completed TAS results".
- Tests:
  - `PartOIteration3ResumeTests.cs` (new; 18 facts plus a 5-case theory, 23 tests);
  - `PartOIteration3PipelineFake.cs` (assessment chosen by path, and an exception option);
  - `PartOIteration3RunTests.cs` (made `partial`);
  - `PartOIteration3ResumeAcceptance.cs` (new; licensed, env-gated, does nothing in CI).

## Validation

- Before the fix, the three `A_retry_after_*` regressions failed (TAS called again). After it they pass.
- `PartOIteration3RunTests` (with the resume tests): 53/53.
- Mutation check: comparing no identity made the changed-design and changed-TAS-case tests fail.
- Full `SAM.Analytical.UI.WPF.Tests` (Release): **1458/1458** (baseline 1434, +23 resume tests, +1 env-gated acceptance fact that does nothing in CI).
- `SAM_UI.sln` Release build: 0 errors. `git diff --check`: clean.
- **Licensed acceptance, production seam** (`PartOIteration3ResumeAcceptance`; the real 3-dwelling project,
  manufacturer guidance, Reference A restored from the Follow-up #2 run and not re-simulated): **0 failures**.
  - Attempt 1: real TAS in 195.3 s (thermal source 55.8 s, route 130.3 s, bridge 4.1 s; TBD ×7, TAS3D ×4,
    TPD ×5), then the controlled Candidate B TM59 refusal. The work was kept.
  - A changed TAS case, and a bridge TSD with a touched write time, each refused reuse (the file by name). Undone,
    the work was reusable again. The decision took 98 ms.
  - Retry: **6.9 s**, calls `Assess, Assess, Persist` only, TSD ×2 for the TM59 reads, **no TBD/TAS3D/TPD**. Every
    TAS file was unchanged and the pairing completed.
  - The resumed comparison is **identical** to Follow-up #2's normal run: 8 rooms, 70,080 values, bias 0.918 K,
    RMSE 1.405 K, max 3.854 K, the same per-flat figures, 0 TM59 outcomes differ, reference Fail / system Fail.
    The pairing reopened through the ordinary review (Assess only) to the same comparison.
  - After completion nothing was kept, and a new run materialised and headed for the thermal source (cancelled
    there).
- **Native UI acceptance: PASS.** Real `build\SAM Analytical.exe`, UIA-driven, same project and method. Evidence is
  in `C:\TasOut\parto-resume-2026-09-29\native`: `driver.log`, `progress-window.log`, `tas-processes.log`,
  `result-window-attempt{1,2}.txt` and `shots\`.
  - The injected failure is genuine: a directory stood where Candidate B's `.sam` is written, so the real
    Persistence stage failed.
  - Attempt 1: Run Iteration 3 took 4.7 min, starting TBD ×4, TAS3D ×4, TPD ×3 and TSD ×4. It was REFUSED at
    Persistence. The Hub line read "…stopped at persistence after 4m 37s. Its TAS results are kept for this
    session, so running it again reuses them instead of running TAS."
  - The obstacle was removed and Run Iteration 3 pressed again. **12 s**, TSD ×2 only.
    - None of the thermal-source, TPD or bridge files changed; only the model, the TM59 reports and the record
      were written.
    - The progress window showed "resuming: the TAS results of the attempt of 29 Sept 2026 21:09 are reused, TAS is
      not run again" and the four resumed stages.
    - The result window marked every TAS stage "Reused from the attempt of …" and its files "unchanged".
    - The Hub read "✓ Iteration 3 complete · … · 10s · TAS results reused from the attempt of 29 Sept 2026 21:09 ·
      reference FAIL / system FAIL".
    - The comparison is again identical: bias 0.918 K, RMSE 1.405 K, 0 outcomes differ.
  - Not observed natively: Cancel during a resumed run, and a stale condition through the UI (both covered by
    tests and the headless acceptance).
- **Known UX debt (not this change's defect):** after a stop, the method row under System case still reads "Last
  attempt did not complete (persistence) … — it can be run again". The Hub's outcome line above it states the
  reuse.

## Unresolved issues / risks

- **Fresh-process resume is unsupported by design** (above).
- The mixed-design Build & Run Systems route (`SimulatePartOMaterialisationSystems`) has its own TAS sequence and
  is not covered.
- The identity uses length + write time for files, the same standard as the rest of Part O. A byte-identical
  rewrite that restores both values is indistinguishable, as it is for the review.
- **Acceptance side effect.** The harness resolved Reference A to the original Follow-up #2 folder
  (`native-run3`), because provenance prefers the recorded results path when that file still exists. It therefore
  wrote Candidate B there. That deleted Follow-up #2's MG record JSON and Candidate B `.sam` (the A/B review report
  and TM59 reports were backed up first to `C:\TasOut\parto-resume-2026-09-29\native-run3-backup-at-2102`), and
  replaced its `-It3BMG` TAS outputs with equivalent ones. No repository data was touched. The harness now refuses
  unless Reference A resolves inside the disposable folder.
