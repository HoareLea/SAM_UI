<!-- SPDX-License-Identifier: LGPL-3.0-or-later -->
<!-- Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors -->

# Part O sidecar portability (Save As / move / copy / another machine)

**Status (1 Oct 2026): implemented and tested; SAM_UI-only PR against `sow/2026-Q3`, not merged.** Branch `fix/parto-sidecar-portability-2026-10-01`, from `sow/2026-Q3` at SAM_UI
`2c0798b` (SAM `c3890d5c`, SAM_Tas `057faf3`, SAM_Systems `09063b4`). Post-release follow-up; it does not reopen the Part O architecture. The `SimulationResultProvenance.Path_TSD`
fix (SAM#174) is not touched. No SAM, SAM_Tas or SAM_Systems change was needed.

## Finding

Every Part O sidecar was audited for persisted workstation paths.

| Sidecar | Before | After |
|---|---|---|
| `<model>.partomixed.json` (`FinalRun.Path_TSD/Path_RunModel/Path_TPD`, `Screening[].Path_TSD`) | absolute | `Locator_*`, relative to the sidecar |
| Iteration 3 pairing record (`Path_TSD_ReferenceA`, `Path_Model_ReferenceA`, `Files[].Path`) | absolute | `Locator_*`, relative to the record |
| `<run>.partorun.json` + `.prepared.sam`, `PartOCase.json`, `PartOMaterialisationRecord` | none | unchanged (already portable) |
| `SimulationResultProvenance` (`Locator_TSD`), `PartOBaselineReference` (`Path_Relative`) | relative | unchanged (SAM#174 / PR-5) |

Demonstrated failures (each reproduced by a test that failed on the old code):

1. **Folder moved:** the saved run read STALE ("results file ... no longer there") although its files sat beside the model; Mixed Design's review was blocked. Iteration 3's review
   refused ("recorded against the results at ...", every Candidate B file "no longer at ...").
2. **Folder copied, original left in place:** the copy validated the ORIGINAL's files and, for Iteration 3, loaded the original's Candidate B. Cross-project contamination, not
   merely staleness. (A copy whose original's files had been touched was refused.)
3. **Workstation paths** (user name, drive) shared in every sidecar.

## What changed

New `SAM_UI/SAM.Analytical.UI/Classes/PartO/PartOSidecarPaths.cs`. A file a sidecar names is written as `Locator_<name>` (relative to the sidecar's folder, by the same
`SimulationResultProvenance.Locator` rule SAM already uses - one rule) instead of `Path_<name>`.

- **Only inside the tree the sidecar travels with.** The tree is the sidecar's own folder (Mixed) or the Part O root of the case folder the record sits in (Iteration 3).
  A file beyond it (another drive; another project's folder that a copied legacy sidecar still names) stays an absolute `Path_<name>`, exactly as before - a relative path must
  never silently tie a copy to the project it was copied from. (The real-data run below found this: the first version wrote `../../..` out of a copy into the original.)
- **Reading** prefers the locator, resolved against where the sidecar IS now; a legacy sidecar's absolute `Path_*` is read as written. No schema bump: old sidecars read as before,
  and a build older than this one reading a new sidecar finds no `Path_*` and fails closed (stale / refused), never wrong.
- **Writing** passes the file's own path, so a legacy sidecar is upgraded the next time its state is saved (when its files are inside its tree).
- Length / write-time lineage checks, and every refusal, are unchanged. The Iteration 3 equality `record.Path_TSD_ReferenceA == run.Path_TSD` now compares the record's
  resolved path with the reopened run's, so it holds after a move.

Files: `PartOSidecarPaths.cs` (new); `Mixed/PartOMixedDesignState.cs`, `Mixed/PartOMixedRunEvidence.cs`, `Mixed/PartOScreeningEvidence.cs`; `PartOIteration3Record.cs`,
`PartOIteration3FileRecord.cs`; WPF `Modify/RunPartOIteration3.cs`, `Query/PartOIteration3Eligibility.cs` (pass the record's own path when writing / reading). Each `ToJsonObject` /
`Read` / `ToString` / `Parse` gained an overload taking the sidecar path; the old signatures are unchanged (no base path = absolute, as before).

## Decisions and assumptions

- **Save As to a new name does not carry the sidecar.** It is named from the model, so a model saved under another name starts with no saved evidence and fails closed (no run
  shown). Pinned by `SavingAs_ANewName_DoesNotInheritTheRunEvidence`. Carrying it would need a policy for orphaned / duplicate sidecars; left to the owner.
- **A legacy sidecar is not relocated by guessing.** Moved without its original, it reads STALE (as before); copied beside its original, it still names the original's run until a
  new run is saved in the copy. A length + write-time probe of path suffixes could repair this; it was not built (heuristic, outside "do not redesign").
- The ventilation-unit catalogue directory/path in the Iteration 3 record is not a project file (it is where the product library is installed) and stays absolute.

## Validation

- `SAM.Analytical.UI.WPF.Tests` **1605/1605** (baseline 1590 + 15 new; 13 + 2 from the review below): `PartOMixedDesignPortabilityTests` (9: new sidecar writes no workstation path; same folder; moved folder;
  copied folder reads its own run; sidecar moved without its results is stale; legacy absolute sidecar still read and upgraded on save; no relative form kept absolute; a file outside
  the sidecar's folder stays absolute; Save As pinned) and 3 in `PartOIteration3ReviewTests` (copied folder reviews its own pairing - the original's files deliberately damaged; legacy
  absolute record still reviews in place; new record names project files relative) + 1 env-gated real-data test. On the old code 5 of the 8 first Mixed tests and the Iteration 3
  copy/no-path tests failed, with the refusal texts quoted above.
- **Real data** (`PartOSidecarPortabilityRealDataAcceptance`, env `SAM_PARTO_SIDECAR_REAL_DIR`): the 35 MB project folder of the 30 Sep licensed Mixed acceptance (real model, real
  legacy `.partomixed.json`, real 16.5 MB `.tsd`, real run model). Copied twice with write times preserved: the legacy sidecar was read, upgraded (no absolute path left), copied
  with the first left in place (reads only its own files), and the whole folder moved (still finds the run's real files, length and write time unchanged). The source folder's SHA256
  listing was identical before and after. No licensed TAS was run.
- **Review (high effort) fixes, same PR:** the Iteration 3 review compared the record's now-normalised path with the run's verbatim path by string, so a results path spelt with forward slashes
  (or a `..` segment) was refused although it is the same file - now compared as normalised places (`SamePlace`, both the Reference A and the Candidate B comparison); and
  `PartOSidecarPaths.Read` was not total (a malformed locator made `Path.GetFullPath` throw, and the Iteration 3 reader only catches `IOException`) - it now treats such a locator as no
  location. Two tests, each failing first (`A_reopened_run_whose_results_path_is_spelt_differently_still_reviews_its_pairing`, `A_record_with_a_malformed_locator_reads_without_throwing`).
  Left as is, by decision: a `Read(JsonObject)` with no sidecar path cannot resolve a locator (production always passes it); the env-gated real-data test returns early when unset (the
  existing `PartOMixedLargeProjectAcceptance` precedent); the legacy-copy limitation above.
- PR CI: see the PR.

## Risks / not done

- A copy tool that does not preserve the results file's write time makes the run read STALE (fail-closed; unchanged rule; zip, some sync tools).
- The Iteration 3 stage ledger's own sentences and `Notes` still quote absolute paths for display (for example "Reference A is '...'"); nothing resolves or compares them.
  The Iteration 3 review report JSON embeds the record with absolute paths (an output, not read back).
- Not clicked through in the real WPF app; the saved state/record is exercised through the production read/write code and a real project folder.
- `Path_Model_ReferenceA` is only written relative when the model lies inside the Part O root; a design model elsewhere stays absolute (and is informational).

## Next step

Review and merge; then the closeout commit on `sow/2026-Q3`. Separate: the TPD `Loading TSD data` investigation record (docs-only PR).
