# SAM_UI Part O PR2 progress

Base: `sow/2026-Q3` at `a16ff93`. Working branch: `codex/part-o-pr2-diagnostics`.

## Completed
Iteration 3 record carries QA weather identity, coordinates and calculated dry-bulb peak from Reference A's provenanced result model. Selected cooling-stat room is recorded with existing guidance settings. Text report presents these facts, the operating-history file, and explicit UNAVAILABLE labels. Existing result/restore and Iteration 3 file validation remain the authority; no physics changed.

## Files changed
PartOIteration3Record, PartOIteration3GuidanceEvidence, PartOIteration3WeatherEvidence, RunPartOIteration3, PartOIteration3GuidanceResolution, PartOIteration3ReportText, PartOIteration3RecordTests; this file.

## Validation
WPF project builds with VS MSBuild. Iteration 3/result reopen suite: 290 passed; final record/report tests: 13 passed. SAM SimulationResultProvenance tests: 23 passed, including changed weather. Existing missing, wrong, stale and moved TSD tests passed in the UI suite. Diff review found no physics edits.

## Next step
Commit, open PR, wait for CI/review, merge, then update local `sow/2026-Q3`. Native TAS COM is not covered by deterministic tests.
