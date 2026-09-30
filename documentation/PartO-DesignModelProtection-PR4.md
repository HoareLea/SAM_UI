<!-- SPDX-License-Identifier: LGPL-3.0-or-later -->
<!-- Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors -->

# Part O PR-4: protect the design model from Part O run output

**Status (30 Sep 2026): implemented and tested. The PR is open against `sow/2026-Q3` and is NOT merged. It is SAM_UI
only, with no SAM change. One owner question is open (below).**

- Branch `feature/parto-design-model-protection-2026-09-30`, from `sow/2026-Q3` `92534d6`.
- Architecture: `documentation/PartO-ModelStateArchitecture.md`, which is the approved review. This PR is step 1 of it.

## Problem

Before this PR:
- Prepare & Run derived each case from whatever model was open.
- After preparation, after simulation and after Iteration 2B, SAM_UI called `SetJSAMObject` with the prepared or
  result model. `Path` stayed the design file's, so a plain Save wrote run output over the design `.sam`.
- 1b and 2 then derived from the previous case's result and carried its provenance and scenarios.

## Behaviour after this PR

| Step | Before | After |
|---|---|---|
| Accepted review (`ConcludePartOReview`) | The window adopted the prepared model | The prepared model goes to the run only. **Only the confirmed inputs** (`PartOEquipmentSelection`, `PartOProjectTestVentilationUnit`) are written onto the design model, and only where they changed (`Modify.PersistPartOInputs`). The run expects the write and survives it. |
| Guided Part O simulation (`SimulatePartO`, and the dialog with `partOWorkflow`) | Simulated the open model and adopted the result | Simulates `PartORun.AnalyticalModel_Prepared`, completes the run and **never replaces the open model** |
| Expert *Energy Simulation* | Completed a prepared Part O run when the open model was the prepared one | An ordinary simulation of the open model, adopted into the window as before. It never completes, stamps or persists a Part O run, and it drops a pending run. |
| Iteration 2B (`RunPartOOptimisation`) | Adopted the last valid design into the window | Not adopted. The kept design stays with the run and under `PartO/Iteration2B` (owner decision 3). The result window no longer says "loaded into the model". |
| 1b after 1a, 2 after either | Derived from the open result | Derived from the design model. No earlier provenance, scenarios, results or systems are carried (tested). |
| Part O result opened by hand | Prepare & Run ran from it | **Refused and redirected**, with no guessing: "This is a Part O result. Part O cases run from a design model — open the design model." The Hub blocks Run and keeps Review Results. The Prepare Iteration command and the shared preparation refuse. Mixed Design's existing refusal now leads with the same sentence. The message points at Remove Results for legacy files. |
| Iteration 3 | Reads `PartORun` | Unchanged |

**What counts as a Part O result** (`UI.Query.PartODesignModelRefusal`):
- `OverheatingScenarios`, or a `SimulationResultProvenance` (only a Part O run stamps either); or
- SAM's `PartOBaselineFindings` reporting `MaterialisedBaseline`: Part O MVHR systems, preparation Part F conditions,
  an isolation context, or a materialisation record.

Ordinary simulation results and TAS design days in the cluster do **not** refuse 1a/1b/2. Mixed Design's stricter
baseline check still refuses them. The Part O system type is internal to SAM, so the rule asks SAM rather than copying
the guid.

## Decisions and assumptions

- **Inputs are compared as their stored JSON, and nothing is written when nothing changed.** A 1a/1b review, or a 2
  review that confirms the project's existing choices, fires no model replacement. The write uses `FullModification`,
  the same modification Mixed Design uses for its saved selection.
- **The expert route changes.** "Prepare Iteration → Energy Simulation" no longer completes a Part O run, because the
  open model is the design. The Prepare Iteration review now says the preparation is kept in the session, and
  Prepare & Run reuses it for the same case (the existing `ReusePreparation`).
- **Test seams** follow the existing `confirm` pattern:
  - an internal `SimulatePartO(…, PartOWorkflowRunner)` overload, which is the seam `RunPartOSimulation` already has;
  - `optimise` and `showResult` parameters on `RunPartOOptimisationResult`.

  Null means production behaviour.

## Owner question (raised rather than decided)

**Hand-picked per-dwelling products no longer carry to the next case.**

In Manual mode, the Review window writes a dwelling's product (`VentilationUnitReference`) onto the **Part O unit the
preparation built**, and that unit is run output. Before PR-4 the next Iteration 2 reused those units, because it was
prepared from the previous run's output (probe: `MVHR-01/02 … Hand-picked unit reused=True`). Now it prepares from the
design, and its new units start with no product.

What does carry:
- the mode and pool (`PartOEquipmentSelection`) and the test product, which are model-level inputs;
- the hand-picked products within the run's own saved models, and within 2B and Iteration 3 of that run.

Automatic modes re-select deterministically, so they are unaffected.

The architecture lists "Part O equipment choices" as design inputs, but the design model has no place for a
per-dwelling choice before a preparation builds the units. Options:
- **(a)** accept this for now: re-pick per run, or use an automatic mode;
- **(b)** a follow-up PR that stores per-dwelling choices on the design, for example keyed by dwelling zone like
  Mixed Design's `PartODwellingStrategies` (SAM + SAM_UI);
- **(c)** something else.

It is pinned by `A_hand_picked_product_stays_with_its_run_and_the_next_case_starts_from_the_design`, which changes if
the owner picks (b).

## Files

- `SAM_UI/SAM.Analytical.UI/Query/PartODesignModelRefusal.cs` (new): the refusal and its lead sentence.
- `SAM_UI/SAM.Analytical.UI/Classes/PartO/PartOWorkflowCapabilities.cs`, `PartOWorkflowInspection.cs`: `DesignModelRefusal`, which becomes the first Run blocker.
- `WPF/SAM.Analytical.UI.WPF/Modify/PreparePartOIteration.cs`: no adoption, `PersistPartOInputs`, and the two entry guards.
- `WPF/SAM.Analytical.UI.WPF/Modify/Simulate.cs`: the Part O case simulates the run's prepared model and adopts nothing; the runner seam.
- `WPF/SAM.Analytical.UI.WPF/Modify/RunPartOOptimisation.cs`: no adoption; the seams.
- `WPF/SAM.Analytical.UI.WPF/Modify/RunPartOWorkflow.cs`: `Capabilities(run, model, …)`.
- `WPF/SAM.Analytical.UI.WPF/Windows/PartOWorkflowWindow.xaml.cs`: carries `DesignModelRefusal` into its inspection. The native smoke found this gap.
- `WPF/SAM.Analytical.UI.WPF/Windows/PartOPreparationWindow.xaml.cs`, `Classes/PartO/PartOOptimisationSummary.cs`: wording ("your design model stays open"; 2B "kept under PartO/Iteration2B").
- `WPF/SAM.Analytical.UI.WPF/Classes/PartO/Mixed/PartOMixedDesignSession.cs`, `Windows/PartOMixedDesignWindow.xaml.cs`: `IsPartOResult`, and the lead sentence.
- `WPF/SAM.Analytical.UI.WPF/Windows/AnalyticalWindow.xaml.cs`: comment only (expert Energy Simulation).
- `WPF/SAM.Analytical.UI.WPF.Tests/PartODesignModelProtectionTests.cs` (new, 7 tests), and `PartOReviewIterationTests.cs` (2 tests updated from "replaced once" to "not replaced"), `PartOPreparedModelCarriedLinkTests.cs` (comment only).
- `documentation/PartO-ModelStateArchitecture.md` (new, the approved architecture), this record, and `documentation/evidence/parto-design-model-protection-2026-09-30/`.

## Evidence

- **Build.** `SAM.Analytical.UI.WPF` builds with 0 errors.
- **`PartODesignModelProtectionTests`: 7/7.** TAS is replaced only at `PartOWorkflowRunner`. The SAM preparation is
  real, and the production decision point, simulation core and 2B command are used.
  - **A/C/E journey.** 1a → 1b → 2 → 2B → Save. The design JSON is identical after each preparation, simulation and
    2B step.
    - Iteration 2's input commit changes exactly the two inputs.
    - After Save, the file's model equals the captured design, has no baseline findings and no refusal, and carries
      the inputs. A later request inherits them.
    - Each run is reviewable: `IsAssessable`, the Hub's `ResultsAvailable`/`CanReviewResults`, and the case `.sam` on
      disk.
    - 2B's round is re-prepared from the Iteration 2 result and lands in `Iteration2B/tas`.
  - **B.** The 1b prepared model and its `.prepared.sam`, and the 2 prepared model, carry no provenance, scenarios,
    results or 1a systems.
  - **C.** Inputs are written only where changed: no write when unchanged, the test product removed when switched off,
    the mode written when changed. A declined review changes nothing.
  - **D.** The saved run model and the `.prepared.sam` are refused, with the lead sentence first among the Hub blockers
    and in Mixed Design. Restore and review still work. An ordinary simulation result is not refused.
  - **Hub window.** Run is blocked on an opened result and Review Results is offered. On the design model the refusal
    is absent.
  - **Pinned boundary.** Hand-picked products (see the owner question).
- **Mutation checks.** Each reintroduced behaviour is caught:
  - the result adopted in `Simulate` fails the journey and D;
  - the prepared model adopted in the review fails "1a preparation";
  - 2B adopting its design fails 2B;
  - the Hub blocker removed fails D;
  - the window dropping the field fails the Hub-window test.
- **Full WPF suite: 1546/1546** (1539 + 7 new).
- **Native smoke, no TAS: PASS** after one fix (`evidence/…/SMOKE.md`).
  - On an opened result, the real Hub blocks Run with the refusal and offers Review Results, and Mixed Design leads
    with the sentence.
  - On a design model, Run is enabled with no refusal and Mixed Design reads "Baseline: clean".
  - Neither file changed, and no TAS process ran.
  - The first run caught the window dropping `DesignModelRefusal`.

## Risks / not verified

- **No licensed TAS run in the real UI.** Prepare & Run, then Save, then reopening the design in SAM Analytical was not
  run with TAS. The same production path is covered in process.
- **After a run the window no longer holds Part O results.** Viewport result display and Results > General > Remove act
  on the design model. Results are reached through Review, the Results tab (which reads the run) or by opening the case
  `.sam`. This is the accepted consequence in the architecture.
- **Legacy result-as-design files are refused** for Prepare & Run. That includes the owner's current model. Remove
  Results is the recovery.
- **The expert route change** (above).
- **Undo.** Undoing the input write, like any other edit, drops a pending session run.

## Next step

1. The owner reviews this PR and answers the hand-picked-products question.
2. Optionally, a licensed native check: open a design model, Prepare & Run 1a, confirm the window is still the design,
   Review, Save, and confirm the saved `.sam` has no Part O results.
3. Merge. Then add the `PROJECT_PROGRESS.md` closeout on `sow/2026-Q3`, and start **PR-1** (Part O system scope,
   SAM + SAM_UI) in a fresh session.
