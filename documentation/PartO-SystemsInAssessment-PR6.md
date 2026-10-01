<!-- SPDX-License-Identifier: LGPL-3.0-or-later -->
<!-- Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors -->

# Part O PR-6: "Systems in this assessment" (SAM's scope, displayed)

**Status (1 Oct 2026): implemented, tested, real-model gate passed. SAM_UI-only PR against `sow/2026-Q3`, not merged.** Branch
`feature/parto-pr6-systems-in-assessment-2026-10-01`, from `sow/2026-Q3` at SAM_UI `ee25a5d` and SAM `c3890d5c`. The last functional Part O PR; after its
merge and closeout the next step is SAM_Deploy and final release/regression validation. **No SAM change was needed.**

Start gate, confirmed before any change: PR-5 merged and closed out (SAM_UI#155 `a65825d`, SAM#173 `bce2c05d`); SAM#174 (`SimulationResultProvenance.Path_TSD`
relative locator) merged and closed out (`ab2b3be2`, closeout `c3890d5c`). Both repositories were fast-forwarded to `origin/sow/2026-Q3`, clean.

## What the engineer sees

Mixed Design gains one collapsible section, between *Project constraints and products* and *Simulation case*:

```text
Systems in this assessment — 2 included · 3 retained on the design, not assessed
  Included in this assessment · 2          Retained on the design, not assessed · 3
  MVHR Flat 2 — dwelling Flat 2            MV 1 — names air handling unit AHU1 · no design terminal
  MVHR Flat 3 — dwelling Flat 3            NV 1 — no design terminal
                                           UV 1 — no design terminal
  Retained systems stay on the design and in the thermal model exactly as authored. They are not assessed here, and nothing has been removed.
```

Four states, each its own sentence, and never a blank: **not checked yet** (SAM has not been asked), **out of date** (the design, selection or catalogue offered changed since SAM
answered - the old answer is withdrawn, not shown), **refused** (SAM's own refusal text, a bounded few plus a count, and *no* list: a refused scope names no system), and
**answered** (included / retained, either may be empty - "None"). A design with no ventilation system says so plainly. Screenshot:
`documentation/evidence/parto-pr6-systems-in-assessment-2026-10-01/mixed-design-after-check.png`.

Why this location: Mixed Design is where the engineer picks the design and presses Check design / Build & Run, so the answer sits beside the action that produces it. It is one
expander, collapsed to a one-line summary when nothing is known, so it adds no workflow and no new window. Nothing in it is editable: there are no AHU edit/remove controls.

## SAM data reused (nothing re-decided)

- `Analytical.Query.PartOSystemsMaterialisationScope` (SAM PR-1/PR-2): `Guids_Retained` is the *included* set, `Exclusions` (guid, full name, terminal count) the *retained* set,
  `Refusals` the refusals. Membership is by identity only; names are display words.
- `PartOMaterialisationRecord.VentilationSystemGuids` (zone → system) for the dwelling a built system serves, and the IZAM-route included list.
- Words only, read from the materialised model by guid: the air handling unit a system names (`SupplyUnitName`, else `ExhaustUnitName`) - "MVHR Flat 2" is the unit Part O
  named for the dwelling - and the dwelling name. No system is classified by name; no duty is read; PR-1/PR-2 logic is not duplicated.

## Check and Build & Run are one answer

`Modify.PartOMixedSystemsMaterialisation` is the ONE Systems preflight both already ran (PR-1). It gained an `out PartOSystemsInAssessment` built from the very scope it hands
SAM_Systems, set on every return (a refusal included); the old overload delegates to it, so existing callers see no change. Check design reads it into
`PartOMixedDesignCheck.SystemsInAssessment`; Build & Run into `PartOStrategySetSimulation.SystemsInAssessment`; the command hands either to the session
(`Modify.RecordSystemsInAssessment`). A test asserts the two are textually identical for the same design, including for a refusal.

## Decisions

- **Included label = the unit's name** ("MVHR Flat 2"), with the dwelling and system as detail - the label the owner named and the one on the run model. The Part O systems
  themselves are all called "MVHR", so their full name would not tell flats apart.
- **Fresh identities per materialisation.** SAM builds new system objects every time, so Check and Build & Run agree on what they *say* (and on the authored retained systems'
  guids), not on the built systems' guids. The comparison test is written that way.
- **IZAM route (no cooled dwelling): no scope is claimed.** No SAM_Systems input exists there, so nothing is "left out of" one. The section lists the systems Part O built (SAM's
  record), shows "Not applicable on this route" for retained, and says no Systems scope was taken. (A refusal that exists only on the Systems route is therefore never shown for a
  design that would not refuse.)
- **Session-only, keyed.** The answer is derived, not state: it is stored with a key of (baseline design fingerprint, catalogue offered, draft selection) and shown only while it
  matches. It is not persisted and there is no new UI-only state to reopen.
- **Large projects.** Two virtualised lists capped at ~112 px each; refusals bounded to four plus a count. The builder is linear (one pass over systems, dictionary lookups).
- **Not done (scope limit).** Prepare & Run / Iteration 3 do not show the section: they have their own established scope notes and are unchanged ("existing Iteration 3/Mixed
  workflows remain unchanged"). Recorded as a follow-up, not folded in.

## Files

`SAM_UI/WPF/SAM.Analytical.UI.WPF/`: new `Classes/PartO/Mixed/PartOSystemsInAssessment.cs` (display model + `Query.PartOSystemsInAssessment` mapping); `Modify/SimulatePartOMaterialisationSystems.cs`
(output on the ONE preflight), `Modify/CheckPartOMixedDesign.cs`, `Modify/RunPartOStrategySet.cs`, `Modify/RunPartOMixedDesignCommand.cs` (hand-off helpers),
`Classes/PartO/Mixed/PartOMixedDesignSession.cs` (keyed answer), `Windows/PartOMixedDesignWindow.xaml(.cs)` (the section). Tests: new `PartOSystemsInAssessmentTests.cs` (21);
`PartOMixedSystemsScopeTests.cs` helpers widened from private to internal (reuse of the production-shape fixture, no logic change).

## Validation

- Focused: 21 new tests (included / retained by identity, inert `MV 1`/`AHU1` never a participant, refusal surfaced, Check == Build for answer and refusal, colliding names incl. 1000
  same-named systems, empty / IZAM / refused-to-materialise, session staleness, window states, 5000 systems = one virtualised list).
- Full `SAM.Analytical.UI.WPF.Tests`: **1590/1590** (1569 + 21).
- Mutations, each killed (red): included from the removed list; Build not carrying the answer; Check dropping it; session key ignoring the selection; a refused scope still listing
  systems; retained not from SAM's exclusions; IZAM claiming a scope; the window hiding a refusal; the Check hand-off dropped; the Build hand-off dropped; a refusal not
  withdrawing the answer.
- A test left a dirty session open and hung the host on the window's "save changes?" prompt; the window test now changes a non-dirtying build input instead.

## Real-model gate (read-only; no TAS run)

Real `SAM Analytical.exe` built from this branch, UI Automation, on a hash-verified **copy** of `000000_SAM_AnalyticalModel-Cleaned.sam` (SHA256 `F561161F...0B78`; the source's hash
is unchanged afterwards). Selection: Flat 1 Natural; Flat 2 Nuaire XBC15, cooling Off; Flat 3 Nuaire MRXBOXAB-ECO5-AECV, cooling On. Before Check the section read "not checked yet".
After **Check design** (outcome text unchanged: "SAM can build this mixed design ... The TAS Systems ventilation can be prepared. Nothing was simulated."):

```text
Systems in this assessment - 2 included · 3 retained on the design, not assessed
Included: MVHR Flat 2 - dwelling Flat 2 | MVHR Flat 3 - dwelling Flat 3
Retained: MV 1 - names air handling unit AHU1 · no design terminal | NV 1 - no design terminal | UV 1 - no design terminal
```

This matches the actual scope (and the licensed acceptance of SAM_UI#154: two air systems, `MVHR Flat 2` / `MVHR Flat 3`). Evidence: `documentation/evidence/parto-pr6-systems-in-assessment-2026-10-01/`
(no local paths). **Not run:** Build & Run in the real app (it needs licensed TAS, and the brief did not require another run); its answer comes from the same preflight and is covered by
the tests above, not by a native click-through.

## Risks and follow-ups

- Prepare & Run / Iteration 3 do not show the section (see above).
- The included label depends on the unit a system names; a built system with no unit name would fall back to its full name.
- On a model with many authored systems the retained list is long by nature; it scrolls inside its own region. No search/filter was added to it.
- Unrelated and unchanged: the intermittent TPD `AddTSDData` hang recorded in SAM_UI#154.

## Next step

Review and merge this PR (SAM_UI only; nothing to merge first), then the `PROJECT_PROGRESS.md` closeout as a docs-only commit on `sow/2026-Q3`; then SAM_Deploy and the final
release/regression validation.
