<!-- SPDX-License-Identifier: LGPL-3.0-or-later -->
<!-- Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors -->

# Part O PR-4: protect the design model from Part O run output

**Status (30 Sep 2026): implemented and tested. The PR is open against `sow/2026-Q3` and is NOT merged.**

It **depends on SAM-BIM/SAM#170** (`feature/parto-manual-equipment-selection-2026-09-30`), which adds
`PartOManualEquipmentSelection` and `PreparePartOIteration`'s manual parameter (record: SAM
`documentation/PartO-ManualEquipmentSelection-PR.md`). SAM_UI CI stays red until that SAM PR merges into SAM
`sow/2026-Q3`, so merge SAM first. The owner question raised by the first round is resolved (below).

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
| Accepted review (`ConcludePartOReview`) | The window adopted the prepared model | The prepared model goes to the run only. **Only the confirmed inputs** (`PartOEquipmentSelection`, `PartOProjectTestVentilationUnit` and, under Manual, the hand-picked products `PartOManualEquipmentSelection`) are written onto the design model, and only where they changed (`Modify.PersistPartOInputs`). The run expects the write and survives it. |
| Hand-picked products (Manual) | Lived on the prepared unit, and reached the next case only through result carry-forward | **Design input**, keyed by dwelling zone guid. A manual Iteration 2 prepared from the design gets them materialised onto its own new units by SAM. See "Hand-picked products" below. |
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

## Hand-picked products (owner decision, resolved in this PR)

**Found in the first round.** In Manual mode the Review window wrote a dwelling's product onto the Part O unit the
preparation built, and that unit is run output. The product reached the next case only because the next case was
prepared from the previous result (probe: `MVHR-01/02 … Hand-picked unit reused=True`). PR-4 removes that
carry-forward, so the product was lost.

**Owner decision.** Manual per-dwelling equipment selection is explicit Part O design input. It is persisted
independently of prepared or run systems and materialised by preparation. It is never copied from a result unit.

**Representation (SAM).** `PartOManualEquipmentSelection` is a new, additive
`AnalyticalModelParameter.PartOManualEquipmentSelection`:
- dwelling zone guid → `VentilationUnitReference` identity, never a name;
- canonical, and schema `v1`;
- absent means none.

Existing types were not reused. `PartOEquipmentSelection` is documented as "a candidate constraint, never an
assignment", and its `Matches` drives reuse. Mixed Design's `PartODwellingStrategySet` is Mixed Design's own
authority.

**Semantics.**
- **Read.** Only a product-selecting review under Manual reads the input (`Modify.ManualEquipmentSelection(request,
  selection, model)`). SAM's `PreparePartOIteration(…, partOManualEquipmentSelection)` assigns each dwelling's product
  to the unit it builds through `AssignVentilationUnit`. 1a and 1b never read it, an automatic rule is never
  overridden, and an unknown schema is warned about and not applied.
- **Written on Accept of a Manual review** (`Modify.ManualEquipmentSelection(table, preparation, existing)`, keyed
  through the new `PartOIterationPreparation.DwellingZoneGuids`):
  - an assigned row sets its dwelling;
  - an unassigned row clears it;
  - dwellings outside the review's scope keep theirs;
  - an empty result removes the parameter.

  Confirming the same choices writes nothing.
- **Changing to an automatic mode** (an accepted automatic review) clears every hand-picked product. The rule's
  answers are never stored as intent.
- **Changing back to Manual** starts from nothing dormant. "Convert to Manual" in a review keeps the rule's current
  answers as the starting choices, which the engineer then accepts or edits.
- **Clearing one dwelling.** The Review window has no control that blanks a row today; the table only assigns. The
  rule ("an unassigned row clears") is defined and pinned at the commit seam, and switching to an automatic mode
  clears all.
- **Legacy.** A model without the parameter behaves as before.
- **The prepared model** is stamped with the same selection, as it is with the mode and pool.

## Files

- `SAM_UI/SAM.Analytical.UI/Query/PartODesignModelRefusal.cs` (new): the refusal and its lead sentence.
- `SAM_UI/SAM.Analytical.UI/Classes/PartO/PartOWorkflowCapabilities.cs`, `PartOWorkflowInspection.cs`: `DesignModelRefusal`, which becomes the first Run blocker.
- `WPF/SAM.Analytical.UI.WPF/Modify/PreparePartOIteration.cs`: no adoption, `PersistPartOInputs` (now also the hand-picked products), the two entry guards, and the two `ManualEquipmentSelection` rules (read for preparation; build from an accepted table).
- `WPF/SAM.Analytical.UI.WPF/Modify/Simulate.cs`: the Part O case simulates the run's prepared model and adopts nothing; the runner seam.
- `WPF/SAM.Analytical.UI.WPF/Modify/RunPartOOptimisation.cs`: no adoption; the seams.
- `WPF/SAM.Analytical.UI.WPF/Modify/RunPartOWorkflow.cs`: `Capabilities(run, model, …)`.
- `WPF/SAM.Analytical.UI.WPF/Windows/PartOWorkflowWindow.xaml.cs`: carries `DesignModelRefusal` into its inspection. The native smoke found this gap.
- `WPF/SAM.Analytical.UI.WPF/Windows/PartOPreparationWindow.xaml.cs`, `Classes/PartO/PartOOptimisationSummary.cs`: wording ("your design model stays open"; 2B "kept under PartO/Iteration2B").
- `WPF/SAM.Analytical.UI.WPF/Classes/PartO/Mixed/PartOMixedDesignSession.cs`, `Windows/PartOMixedDesignWindow.xaml.cs`: `IsPartOResult`, and the lead sentence.
- `WPF/SAM.Analytical.UI.WPF/Windows/AnalyticalWindow.xaml.cs`: comment only (expert Energy Simulation).
- `WPF/SAM.Analytical.UI.WPF.Tests/PartODesignModelProtectionTests.cs` (new, 10 tests), and `PartOReviewIterationTests.cs` (2 tests updated from "replaced once" to "not replaced"), `PartOPreparedModelCarriedLinkTests.cs` (comment only).
- `documentation/PartO-ModelStateArchitecture.md` (new, the approved architecture), this record, and `documentation/evidence/parto-design-model-protection-2026-09-30/`.

## Evidence

- **Build.** `SAM.Analytical.UI.WPF` builds with 0 errors.
- **`PartODesignModelProtectionTests`: 10/10.** TAS is replaced only at `PartOWorkflowRunner`. The SAM preparation is
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
  - **Hand-picked products** (these replace the round-one test that pinned their loss):
    - **1.** Manual A for dwelling 1 and B for dwelling 2, committed. The design holds exactly `{zone1: A, zone2: B}`,
      written once, and no Part O output.
    - **2.** The run's units carry A/B.
    - **5.** After the commit, the Iteration 2 simulation and 1a/1b preparation and simulation leave the design
      byte-identical.
    - **3.** 1a units get no product. A later Iteration 2 with no edit materialises A/B onto new units. Those units'
      guids differ from the earlier run's, and the prepared model has no provenance, scenarios, results or earlier
      systems. Nothing is written.
    - **4.** Save, then reopen in a new session with a new run, then prepare: A/B again, and the design is unchanged.
    - **Semantics.** Changing one dwelling's product keeps the other dwelling's. An automatic review selects by the
      rule and clears the input. Back in Manual nothing is dormant. An unassigned row clears, and out-of-scope
      dwellings keep theirs.
    - **6/7.** Only a manual product-selecting review reads the input. 1a, both automatic modes and a legacy model
      without the parameter read none.
- **Mutation checks.** Each reintroduced behaviour is caught:
  - the result adopted in `Simulate` fails the journey and D;
  - the prepared model adopted in the review fails "1a preparation";
  - 2B adopting its design fails 2B;
  - the Hub blocker removed fails D;
  - the window dropping the field fails the Hub-window test.

  For hand-picked products, run one at a time on clean code:
  - SAM skips the assignment: 3 SAM_UI tests fail, plus SAM's own;
  - SAM applies the selection under a catalogue: the SAM test fails;
  - the input is read for 1a and automatic reviews: 3 tests fail;
  - the input is never read: 3 tests fail;
  - an automatic review keeps or stores choices: 3 tests fail;
  - an unassigned row does not clear: the clear test fails;
  - choices are never written to the design: 2 tests fail.
- **Full WPF suite: 1549/1549** (1546 in round one; this round adds 4 tests and replaces 1). **SAM.Tests: 2703/2703** on the SAM branch.
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

1. The owner reviews SAM-BIM/SAM#170 and this PR. Merge SAM first.
2. Optionally, a licensed native check: open a design model, Prepare & Run 1a, confirm the window is still the design,
   Review, Save, and confirm the saved `.sam` has no Part O results.
3. Merge. Then add the `PROJECT_PROGRESS.md` closeout on `sow/2026-Q3`, and start **PR-1** (Part O system scope,
   SAM + SAM_UI) in a fresh session.
