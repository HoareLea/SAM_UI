# SAM_UI Part O PR2 progress

Base: `sow/2026-Q3`. PR2 merged as SAM-BIM/SAM_UI#192 at `a551d321a94bd2d93fc7a1401c8a9d287f07e9b2` on 2026-10-04.

## Completed
Iteration 3 record carries QA weather identity, coordinates and calculated dry-bulb peak from Reference A's provenanced result model. Selected cooling-stat room is recorded with existing guidance settings. Text report presents these facts, the operating-history file, and explicit UNAVAILABLE labels. Existing result/restore and Iteration 3 file validation remain the authority; no physics changed.

## Files changed
PartOIteration3Record, PartOIteration3GuidanceEvidence, PartOIteration3WeatherEvidence, RunPartOIteration3, PartOIteration3GuidanceResolution, PartOIteration3ReportText, PartOIteration3RecordTests; this file.

## Validation
WPF project builds with VS MSBuild. Iteration 3/result reopen suite: 290 passed; final record/report tests: 13 passed. SAM SimulationResultProvenance tests: 23 passed, including changed weather. Existing missing, wrong, stale and moved TSD tests passed in the UI suite. PR Windows build and SPDX passed. Diff review found no physics edits.

## Native acceptance result (2026-10-04): FAIL, stopped at defect
Full evidence and findings: `C:\TasOut\parto-nuaire-acceptance-2026-10-04\ACCEPTANCE_REPORT.md`. The supplied model was an earlier Part O result; SAM_UI correctly refused it as a design baseline. The UI saved a clean copy. The source remained unchanged (SHA-256 `25DD56B6...70E572`). The current app at `sow/2026-Q3` `8d3deab` ran native TAS/COM.

Fetched `sow/2026-Q3` in SAM_UI, SAM, SAM_Systems and SAM_Tas; each local HEAD matched its origin tip and all worktrees were clean before this report update. Dependency tips: SAM `64a735f`, SAM_Systems `5cd9ee1`, SAM_Tas `1ea4b5d`.

Mixed Design UI required an explicit room before Build. With Nuaire units assigned to three flats and Flat 1 cooling stat set to `Studio 1_0`, the native TAS Systems mixed run completed: 3 dwelling PASS; separate significant corridor risk. The 8,760-hour operating CSV shows background 30/30 l/s, first DX at hour 2605 with 80/80 l/s, bypass and recovery, 13 °C cooling supply minimum, and actual 38.1 °C outdoor peak at hour 4935. Room choices are TEST ASSUMPTIONS only.

Two separate Iteration 2 Prepare & Run reference runs completed through native TAS and TM59 PASS. Iteration 3 refused before Candidate B TAS in both attempts. After the first refusal, all three valid cooling rooms (`Studio 1_0`, `Bedroom 2_3`, `Bedroom 2_6`) were confirmed and saved. The second prepared and result models contain all three GUIDs, yet the Iteration 3 record resolves all three `Guid_CoolingStatSpace` fields to empty and materialisation refuses `MVHR-03`. `PartOIteration3GuidanceResolution` reads fresh catalogue settings without the saved dwelling strategy room; the Mixed route has the required transfer. No Candidate B file was produced. Weather identity, coordinates and 38.1 °C peak were recorded from the actual reference run; complete PR2 A/B diagnostics and moved Candidate B provenance could not be verified.

No code or physics was changed. Only this progress document changed in Git. Next step: propose a targeted Iteration 3 guidance binding fix for owner review, apply it only after authorization, then repeat the native acceptance. Do not start PR3.
