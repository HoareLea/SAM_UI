<!-- SPDX-License-Identifier: LGPL-3.0-or-later -->
<!-- Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors -->

# Part O: "Systems in this assessment" outside Mixed Design - future UX enhancement

**Status (1 Oct 2026): inspected; NOT implemented, deliberately. Docs-only.** Read against `sow/2026-Q3` at SAM_UI `2c0798b` (PR-6, SAM_UI#156, merged). Brief: reuse the PR-6 section in
*Prepare & Run* and *Iteration 3* only if it is a small, presentation-only change (SAM authoritative; no duplicated scope / effective-duty logic; no name heuristics; no new workflow state;
existing behaviour preserved). It is not, so it is recorded here instead.

## Findings (by code)

**PR-6 as built.** The section is `PartOSystemsInAssessment` (display model) built by `Query.PartOSystemsInAssessment(PartOMaterialisation, PartOSystemsMaterialisationScope?, string?)`
from SAM's scope (`Guids_Retained`, `Exclusions`, `Refusals`) plus the materialisation record, produced only by `Modify.PartOMixedSystemsMaterialisation` (the one Mixed Systems
preflight) and held in `PartOMixedDesignSession`. It is filled imperatively in `PartOMixedDesignWindow`.

**Prepare & Run - no.** The window (`PartOWorkflowWindow`) has no `PartOMaterialisation` and never runs SAM's `PartOSystemsMaterialisationScope`; Prepare & Run runs through
`RunPartOSimulation` / the TBD workflow with no SAM_Systems materialisation. A section there could only say "N systems built" (the not-applicable branch of the model), which the Prepare window
already states (route text, "N dwelling systems", duty totals). A real "retained, not assessed" list would mean running SAM's scope in a workflow that does not run it - new logic, not
presentation.

**Iteration 3 - feasible, but plumbing.** SAM's scope *is* computed there (`PartOIteration3Preflight` -> `PartOIteration3ScopedCluster` -> `Query.PartOIteration3SystemScope` -> SAM's
`PartOSystemsMaterialisationScope`; the run calls it again and persists guids and notes in `PartOIteration3Record`). But:

1. the wrapper `PartOIteration3SystemScope` keeps only `Guids_Retained`, `Guids_Removed`, notes and refusal strings - it drops SAM's `Exclusions` (name, terminal count) and structured `Refusals`;
2. the PR-6 builder takes a `PartOMaterialisation` (route, record, model) that Iteration 3 does not have, so it needs a new overload over `AdjacencyCluster` + scope (+ an optional
   guid -> dwelling-name map for the "dwelling Flat 2" words, which has no Iteration 3 equivalent without a new lookup);
3. the XAML would be extracted into a shared `UserControl` (precedent: `PartOEquipmentSelectionControl`), the Mixed window's test accessors forwarded, and the "not asked yet" wording
   (Mixed-specific: "Check design asks SAM...") made a parameter;
4. a reopened (restored) run has no prepared model and no captured guids, so the section would have to say "not available" there; the persisted record holds guids only.

Estimate (code-read only): ~80-120 new lines, ~60 moved, 4-6 tests, low-to-moderate risk (preflight object, hub refresh, control extraction, Mixed window tests). Each of 1-3 is small; together
they are the "meaningful additional plumbing" the brief says not to build. The scope itself would be method-independent (it depends on the prepared model and captured guids, not the
Iteration 3 method), so no new workflow or persisted state would be needed.

## Recommendation (for the owner to decide)

- **Prepare & Run:** do not add. Keep the existing prepare summary.
- **Iteration 3 hub panel:** worth doing as its own small PR if engineers ask for it: carry SAM's raw scope through the preflight, add the cluster-based builder overload, extract the
  control. Do not add it to `PartOIteration3ResultWindow` (the persisted record has guids only; names would need the model).
- Whichever is chosen: SAM stays authoritative, names are display words only, nothing is classified by name, no new session or persisted state.
