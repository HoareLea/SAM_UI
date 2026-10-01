<!-- SPDX-License-Identifier: LGPL-3.0-or-later -->
<!-- Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors -->

# Part O PR-5: a saved result says what it was derived from (`PartOBaselineReference`)

**Status (1 Oct 2026): implemented, tested and revised after the owner's design review. Two coordinated PRs are open against `sow/2026-Q3`, neither merged. Merge SAM first.**

1. **[SAM-BIM/SAM#173](https://github.com/SAM-BIM/SAM/pull/173)** (domain: the reference, stamping, resolution). Record: SAM `documentation/PartO-BaselineReference-PR5.md`.
2. **[SAM-BIM/SAM_UI#155](https://github.com/SAM-BIM/SAM_UI/pull/155)** (this record): stamps every case, exposes the reference, names the design in the refusal, and keeps
   Iteration 3 off an Iteration 2B round.

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
Other findings:

- The case type is not persisted on the model: it lives in the output folder plus `PartOCase.json`, file-name suffixes and the resume sidecar. A reopened 2B round reads as
  "Iteration 2".
- `.partomixed.json` is named from the design's file name, so Save As or a rename orphans it. (Not changed.)
- Path handling is inconsistent: `Restore` tolerates a moved folder; the Iteration 3 review requires absolute path equality. (Not changed.)
- The output root is a remembered global, not tied to the design model. (Not changed.)
- From the licensed Mixed acceptance (SAM_UI#154): reopening the design shows the prior run STALE until its selection is saved again; opening the run model says
  "open the design model" without saying which one. (The second is what this PR addresses.)

## Iteration 3: one authoritative source, and a 2B round is not it (design review, item 2)

**Source semantics, traced in `RunPartOIteration3`.** Iteration 3 reads exactly one run: `partORun.AnalyticalModel_Assessment` (Reference A's result and its provenance),
`partORun.Path_TSD` (A's results), and `partORun.AnalyticalModel_Prepared` (A's own prepared model, from which Candidate B's building is made). The prepared model is not a second source:
in a session it is A's own preparation, and in a reopened run it is `.prepared.sam` loaded by `TryResume` and verified against A's `.partorun.json` fingerprint, which is bound to A's
provenance. So Iteration 3 has **one authoritative source result, Reference A**, and one reference (`Source`) plus the design inherited through A is enough. Candidate B's
reference is stamped with exactly that.

**The finding was real.** After a 2B run the session's run holds the last 2B round (`AnalyticalModel_Assessment`, `Path_TSD`, the prepared model with the optimised airflows, and the
same preparation context). The eligibility gate only required `BasePassive`, so Iteration 3 was **offered over an optimisation round and would have taken it as Reference A** -
contradicting the frozen architecture, which derives Iteration 3 from the 1a/2 result and keeps the accepted 2B design under `PartO/Iteration2B`, not adopted. Both regression
tests failed before the fix (proved in place: `Iteration_3_is_offered_from_the_Iteration_2_result_but_never_from_an_Iteration_2B_round` and
`An_Iteration_2B_round_without_its_own_reference_is_recognised_by_its_case_folder`).

**The smallest fix.** `Query.PartOIteration3Eligibility` refuses a run that is a 2B round, said by the round itself (its `PartOBaselineReference` case) or, for a round written
before the reference existed, by the case folder SAM wrote it into (SAM's own `PartOCase.json` marker, never a name). An Iteration 1a or Iteration 2 result stays eligible;
the base-provision gate is unchanged (and its doc comment, which said "1a only" while the code has always accepted 2, now says what the code does). The production command
`RunPartOIteration3` refuses through the same eligibility, before it touches anything.

## Design (see the SAM record for the representation)

- **Case + design + (for 2B and Iteration 3) source result**, on the **result** model: identity first (guid, state fingerprint), a **relative** locator second, name for display only.
  **No absolute path is persisted** (design review, item 1) - the persisted reference carries `Kind`, `Guid`, `Fingerprint`, `Name` and `Path_Relative` only. Resolution takes an
  optional runtime `path_Hint`, which is never stored.
- **Where each case stamps** (each stamps the relative locator from the folder its result is written to):

  | Case | Stamped | Reference |
  |---|---|---|
  | 1a / 1b / 2 | `Simulate.cs`, on the working copy, from the open design at simulation time (after any confirmed input has been written to it), with the output folder | design |
  | 2B | `OptimisePartOTM59`: every round and the capacity envelope, on a **copy** of the round's prepared model | source = the Iteration 2 result (run 0, not the previous round); design inherited and rebased |
  | Iteration 3 | `RunPartOIteration3`, on Candidate B, before its provenance | source = Reference A's saved result; design inherited from A and rebased |
  | Mixed | SAM's materialiser (identity), then `RunPartOSimulation` adds the design locator from `PartOSimulationContext.Path_DesignModel` (session state, never persisted) | design |

- **Design fingerprint** is taken from the design as it stands when the case starts, so an input committed by the engineer is part of it. An unsaved design reads as `Changed` against
  its file until saved, and as `Resolved` after Save.
- **UI, deliberately small:** the refusal shown for an opened result (Prepare, the Hub, Mixed Design) says which case the result is and which design it was derived from, found by
  identity. **Nothing is opened or adopted, and there is no new prompt.** `PartORun.BaselineReference` exposes it. A "Run from design model?" action is left for the owner (UI work;
  PR-6 territory). The only other behaviour change is the Iteration 3 guard above.
- **Not done on purpose:** no calculation or classification change; no persisted 2B record; `.partomixed.json` unchanged.

## Files

- `SAM_UI/SAM.Analytical.UI/Classes/PartO/PartOSimulationContext.cs` (`Path_DesignModel`, session state, carried by `Copy`), `PartOOptimisationRun.cs`, `PartORun.cs` (`BaselineReference`);
  `Query/PartODesignModelRefusal.cs` (path-aware overloads and `PartODerivedFromSentence`); new `Query/PartODerivedCaseOf.cs`.
- `WPF/SAM.Analytical.UI.WPF/Modify/`: `Simulate.cs`, `RunPartOSimulation.cs`, `OptimisePartOTM59.cs`, `RunPartOIteration3.cs`, `RunPartOStrategySet.cs`,
  `PreparePartOIteration.cs`, `RunPartOWorkflow.cs`; `Classes/PartO/Mixed/PartOMixedDesignSession.cs` (blocker wording); `Query/PartOIteration3Eligibility.cs` (the 2B guard).
- Tests: `PartODesignModelProtectionTests.cs` (+4: the journey, the copied-tree/legacy test, and the two Iteration 3 guard tests), `PartOIteration3RunTests.cs` (+1), new
  `PartOBaselineReferenceMixedTests.cs` (+1).

## Expected behaviour, and where it is tested

1. 1a, 2. 1b, 3. 2 result references its design - `Each_result_saves_a_reference_...` (production `ConcludePartOReview` + `SimulatePartO`, TAS at the workflow seam).
4. 2B references the Iteration 2 result - the same test, the round stamped by the production `StampedPartOIteration2B`.
5. Iteration 3 - `Candidate_B_model_states_its_source_result_and_the_design_behind_it`; and never over a 2B round - the two guard tests.
6. Save/reopen preserves it - the journeys read the saved `.sam`; `PartORun.Restore` exposes it; SAM `NoAbsolutePathIsEverPersisted` inspects the inflated payload.
7. Reopening never makes a result the design - the refusal names the design; the design snapshot is unchanged on disk and in memory; Mixed still refuses it.
8. Missing/legacy - `A_legacy_result_is_refused_as_before_...` (refusal text identical to before), SAM `AModelWithNoReference_IsUnknown_NotGuessed`.
9. Copied/renamed files - a copied tree resolves its design through the relative locator after the original is deleted; a renamed design is found by identity (SAM tests).
10. The design is not mutated - snapshots through 1a/1b/2/2B and the saved file; the design never carries a reference.
- Mixed: `A_mixed_run_model_names_the_baseline_it_was_materialised_from`.

## Validation

- `SAM.Analytical.UI.WPF.Tests` **1569/1569** (1563 before PR-5; +6 across PR-5 and this review), on binaries rebuilt from clean sources. SAM `SAM.Tests` 2787/2787 (SAM record).
- **Mutations, all killed** (sources restored, sha1-checked): Simulate not stamping; Iteration 3 not stamping; the 2B round simulated unstamped; no locator completed for Mixed;
  the Iteration 3 guard removed; the guard without its case-folder rule; plus eleven SAM-side mutations (SAM record). One earlier planned mutation - stamping the design itself - did not
  compile and is not claimed (the open model's getter returns a clone).
- **Not covered by a full-loop test:** `Optimise` has no TAS seam, so the two call sites inside it (run-0 reference, stamp before each round and the envelope) are covered through the
  production helper they call, not through the loop.
- **Local paths:** a scan of every file this PR adds or changes, and of the evidence in #154, for user, OneDrive, company and drive-letter paths finds none. The reference persists none.
  (A result's existing `SimulationResultProvenance.Path_TSD` is absolute; see the SAM record.)

## Risks / unresolved

- Resolution may open up to 16 `.sam` files beside a recorded location, on the refusal path, on the UI thread.
- Not verified: how an older build reads a result carrying the new parameter (it keeps it as raw JSON).
- A 2B round written before the reference existed is recognised by its case folder, not by its model: copied out of its case folder it would look like an Iteration 2 result.

## Next step

Owner review -> merge SAM#173 -> confirm SAM_UI#155 CI -> merge -> `PROJECT_PROGRESS.md` closeouts in both repos (after merge only). Then PR-6.
