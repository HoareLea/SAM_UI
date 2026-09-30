<!-- SPDX-License-Identifier: LGPL-3.0-or-later -->
<!-- Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors -->

# Part O PR-5: a saved result says what it was derived from (`PartOBaselineReference`)

**Status (1 Oct 2026): implemented and tested. Two coordinated PRs are open against `sow/2026-Q3`, neither merged. Merge SAM first.**

1. **SAM PR-5** (domain: the reference, stamping, resolution). Record: SAM `documentation/PartO-BaselineReference-PR5.md`.
2. **SAM_UI PR-5** (this record): stamps every case, completes the locators, exposes the reference, names the design in the refusal.

Both use the branch `feature/parto-pr5-baseline-reference-2026-10-01`, from `sow/2026-Q3` (SAM `137c0bcf`, SAM_UI `d9e30ca`). SAM_UI's CI clones the
same-named SAM branch first, so it can go green before SAM merges; it still must not merge first. No SAM_Systems or SAM_Tas change. Architecture authority:
`documentation/PartO-ModelStateArchitecture.md` step 6. PR-6 is not started.

## Investigation (read-only; SAM_UI `d9e30ca`, SAM `137c0bcf`)

How each case is created, saved, reopened and linked **before** this PR:

| Case | Created from | Saved as | What linked it to its source |
|---|---|---|---|
| 1a / 1b / 2 | `PreparePartOIteration` over the open design; `Simulate.cs` runs a shallow copy | `<root>/<case>/tas/<project>.sam`, plus `.partorun.json` and `.prepared.sam` | nothing. The copy keeps the design's guid, and usually its name. |
| 2B | each round copies the last valid result; `RunPartOSimulation` | `<project>-OptNN.sam` under `Iteration2B` | only the `-OptNN` name prefix; no optimisation record is persisted |
| Iteration 3 | Candidate B from the prepared model via the no-IZAM source | `Iteration3/tas`, pairing record `...-Iteration3-<mode>.json` | the record names Reference A by path and fingerprints; the Candidate B model names nothing |
| Mixed | SAM `MaterialisePartODwellingStrategies` over the open baseline | `MixedDesign/tas/<name>_Mixed.sam`; forward pointer `<design>.partomixed.json` | `Fingerprint_Baseline`, a hash. There is no reverse pointer. |

Reopening (`PartORun.Restore`) trusts only `SimulationResultProvenance` (results file path, length, time, model and scenario fingerprints) - never a design.
Findings worth knowing (the investigation, including ones **not** fixed here):

- The case type is not persisted on the model: it lives in the output folder plus `PartOCase.json`, file-name suffixes and the resume sidecar. A reopened 2B
  round reads as "Iteration 2".
- **Observed, not verified, not changed:** Iteration 3 eligibility only requires `BasePassive`, so after a 2B run the session run holds the last 2B round;
  Iteration 3 would then take that round as Reference A. No test covers it.
- `.partomixed.json` is named from the design's file name, so Save As or a rename orphans it.
- Path handling is inconsistent: `Restore` tolerates a moved folder; the Iteration 3 review requires absolute path equality.
- The output root is a remembered global, not tied to the design model.
- From the licensed Mixed acceptance (SAM_UI#154): reopening the design shows the prior run STALE until its selection is saved again; opening the run model says
  "open the design model" without saying which one.

## Design (see the SAM record for the representation)

- **Case + design + (for 2B and Iteration 3) source result**, on the **result** model, identity first (guid, state fingerprint), locators second (relative then
  absolute path), name for display only.
- **Where each case stamps:**

  | Case | Stamped | Reference |
  |---|---|---|
  | 1a / 1b / 2 | `Simulate.cs`, on the working copy, from the open design at simulation time (after any confirmed input has been written to it) | design |
  | 2B | `OptimisePartOTM59`: every round and the capacity envelope, on a **copy** of the round's prepared model | source = the Iteration 2 result (run 0, not the previous round); design inherited |
  | Iteration 3 | `RunPartOIteration3`, on Candidate B, before its provenance | source = Reference A's saved result; design inherited from A |
  | Mixed | SAM's materialiser (identity), `PartOSimulationContext.Path_DesignModel` (file) | design |

- **Locators are completed in one place:** `RunPartOSimulation`, just before the provenance record, using the folder the `.sam` is written to. Iteration 3 does
  the same for Candidate B.
- **Design fingerprint** is taken from the design as it stands when the case starts, so an input committed by the engineer is part of it. An unsaved design
  reads as `Changed` against its file until saved, and as `Resolved` after Save.
- **UI, deliberately small:** the refusal shown for an opened result (Prepare, the Hub, Mixed Design) now also says which case the result is and which design
  it was derived from, found by identity. **Nothing is opened or adopted, and there is no new prompt.** `PartORun.BaselineReference` exposes it.
  A "Run from design model?" action is left for the owner (it is UI work; PR-6 territory).
- **Not done on purpose:** no calculation or classification change; no persisted 2B record; `.partomixed.json` unchanged.

## Files

- `SAM_UI/SAM.Analytical.UI/Classes/PartO/PartOSimulationContext.cs` (`Path_DesignModel`, carried by `Copy`), `PartOOptimisationRun.cs`, `PartORun.cs` (`BaselineReference`);
  `Query/PartODesignModelRefusal.cs` (path-aware overloads and `PartODerivedFromSentence`); new `Query/PartODerivedCaseOf.cs`.
- `WPF/SAM.Analytical.UI.WPF/Modify/`: `Simulate.cs`, `RunPartOSimulation.cs`, `OptimisePartOTM59.cs`, `RunPartOIteration3.cs`, `RunPartOStrategySet.cs`,
  `PreparePartOIteration.cs`, `RunPartOWorkflow.cs`; `Classes/PartO/Mixed/PartOMixedDesignSession.cs` (blocker wording).
- Tests: `PartODesignModelProtectionTests.cs` (+2 journeys), `PartOIteration3RunTests.cs` (+1), new `PartOBaselineReferenceMixedTests.cs` (+1).

## Expected behaviour, and where it is tested

1. 1a, 2. 1b, 3. 2 result references its design - `Each_result_saves_a_reference_...` (production `ConcludePartOReview` + `SimulatePartO`, TAS at the workflow seam).
4. 2B references the Iteration 2 result - the same test, the round stamped by the production `StampedPartOIteration2B`.
5. Iteration 3 - `Candidate_B_model_states_its_source_result_and_the_design_behind_it`.
6. Save/reopen preserves it - both journeys read the saved `.sam`; `PartORun.Restore` exposes it.
7. Reopening never makes a result the design - the refusal names the design; the design snapshot is unchanged on disk and in memory; Mixed still refuses it.
8. Missing/legacy - `A_legacy_result_is_refused_as_before_...` (refusal text identical to before), SAM `AModelWithNoReference_IsUnknown_NotGuessed`.
9. Copied/renamed files - a copied tree resolves its design through the relative locator after the original is deleted; a renamed design is found by identity (SAM tests).
10. The design is not mutated - snapshots through 1a/1b/2/2B and the saved file; the design never carries a reference.
- Mixed: `A_mixed_run_model_names_the_baseline_it_was_materialised_from`.

## Validation

- `SAM.Analytical.UI.WPF.Tests` **1567/1567** (1563 before; +4: two journeys, Iteration 3, Mixed). SAM `SAM.Tests` 2782/2782 (SAM record). Mutations killed (sources restored, sha1-checked): no locators completed; Simulate not
  stamping; Iteration 3 not stamping; the 2B round simulated unstamped; plus six SAM-side mutations (SAM record). One planned mutation - stamping the design itself -
  did not compile and is not claimed (the open model's getter returns a clone, so it cannot reach the design that way).
- **Not covered by a full-loop test:** `Optimise` has no TAS seam, so the two call sites inside it (run-0 reference, stamp before each round and the envelope)
  are covered through the production helper they call, not through the loop.

## Risks / unresolved

- The resolution may open up to 16 `.sam` files beside a recorded location, on the refusal path, on the UI thread.
- Not verified: how an older build reads a result carrying the new parameter (it keeps it as raw JSON).
- The Iteration 3 after 2B observation above.

## Next step

Owner review -> merge SAM PR-5 -> confirm SAM_UI PR-5 CI -> merge -> `PROJECT_PROGRESS.md` closeouts in both repos (after merge only). Then PR-6.
