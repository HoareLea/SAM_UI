<!-- SPDX-License-Identifier: LGPL-3.0-or-later -->
<!-- Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors -->

# Part O — Prepare & Run

**Status: READY WITH DOCUMENTED LIMITATIONS** for normal engineering and project use. This guide covers the SAM_UI desktop workflow, including Iteration 2 and Iteration 3. Apply your project's Approved Document O and CIBSE TM59 methodology when choosing the design and weather.

## 1. What Prepare & Run does

**Prepare & Run** prepares a Part O case from the open **design model**, checks it, runs a full year in native TAS, and assesses the resulting room temperatures against TM59. The design model stays open. The prepared model and simulation results are separate saved outputs; a result model is evidence of a completed case, not automatically a clean starting point for another design.

For an Iteration 3 comparison, the completed earlier run is **Reference A**. **Candidate B** (called the *system case* on screen) uses the same assessment case with ventilation represented as an explicit TAS Mechanical System. The two cases and their results remain paired for later review.

## 2. Before you start

- Open a valid SAM analytical **design** model with sound geometry, constructions, zones and spaces. Identify the Part O dwellings and their TM59 room setup, including internal conditions and relevant Part F ventilation duties.
- Decide whether the dwellings use natural ventilation, MVHR design duty, or a selected manufacturer unit. For Iteration 2 and a product based Iteration 3, check the unit assignment and its design airflow against the dwelling duty. Configure cooling and the explicit control room for every cooled dwelling as described below.
- Choose the project weather and a writable, **fresh output root** for a new run. Part O simulates days 1–365; provide suitable full year weather. Check the weather, solar calculation and assessment scope before committing to a long TAS run.

> **IMPORTANT** — A previously simulated Part O result or prepared result is not a new design baseline. If **Prepare & Run** refuses it, reopen the original pre Part O design model. Alternatively use **Results → Part O → Remove Results...**, inspect the **Part O — Remove Results** window, select **Save cleaned copy...**, then **Open cleaned copy**. The original model and its TAS files are retained. Review the cleaned copy's baseline check and design inputs before starting a new case.

## 3. Choose the cooling control room

For **each cooled dwelling**, select the representative room containing the intended cooling control or stat from the project design. SAM cannot discover the real sensor position. The selected room must belong to that dwelling **and** be served by its ventilation unit. The choice is saved with the dwelling's design selection; it must still be present in the model used to prepare Reference A.

In the current UI, open **Simulate → Part O → Mixed Design** on the clean design model. Set the intended MVHR/product strategy for each cooled dwelling, select one dwelling, and use **Cooling on**. Choose its room in **Cooling control room**, then click **Confirm control room**. Repeat for every cooled dwelling. Check the **Cooling control room** column and click **Save selection**; save the design model to retain those choices across sessions. The **Check design** action reports design refusals without starting TAS. **Build & Run Mixed Design** is a separate workflow and is not required just to save the room selection.

> **IMPORTANT** — The Iteration 3 product operating guidance requires this explicit room selection. The current nominal Nuaire control uses **22°C**. DX cooling, supply fan and extract fan follow the same selected room. SAM does not silently substitute the largest supplied room. Choose from design knowledge, then verify the selected room and setpoint in the completed evidence.

## 4. Use Prepare & Run

1. With the design model open, click **Simulate → Part O → Prepare & Run**. The **Part O — Prepare & Run** window opens.
2. Choose **Scenario**: **Iteration 1a — MVHR design duty (no manufacturer unit)**, **Iteration 1b — Natural ventilation (no mechanical system)**, or **Iteration 2 — MVHR with manufacturer unit**. For the product route, review **Equipment selection** and the proposed dwelling assignments. A product's capacity is a ceiling; it does not replace the design airflow.
3. Set **Scope**. If selecting dwellings individually, tick them in the list; **Search**, **Select All** and **None** act on the visible matches. Use **Run selected dwellings in isolation (thermal model scope only)** only when that thermal scope matches your assessment plan.
4. Expand **Simulation case**. Select **Weather**, a fresh **Output folder** root, and **Solar calculation**. The window fixes the rest of the Part O case to a full year, days 1–365, with no sizing or exports.
5. Read **Readiness** and any red blocker. **Show details** expands the full explanation; the row tooltip also contains it. Resolve refusals in the model or selections before running.
6. Click **Prepare & Run**. In **Part O — Review iteration**, review the preparation tables and notes, including any warnings or refusals; **Show details** and **Copy All** expose the full diagnostics. Click **Accept & Run TAS** to proceed, or **Cancel** to leave the run unstarted.
7. Follow the progress window through preparation, native TAS and TM59 assessment. On completion use **Review Results** for the reference TM59 report. If cancelled or refused, read the last outcome and the detailed reason before retrying.
8. For Iteration 3, use the **Iteration 3 — Explicit system and cooling assessment** panel after Reference A has completed. Select the relevant **System case** method. For nominal Nuaire operating and cooling behaviour choose **Selected product — manufacturer operating guidance**. Review the reference description, unit summary, and preflight refusal or eligibility. Click **Run Iteration 3** only when enabled.
9. On completion click **Open result** in that panel to see **Part O — Iteration 3 comparison**. Use **Review Results** for Reference A, and the comparison window's **TM59 report — reference**, **TM59 report — system**, **Comparison report** and **Open folder** as needed.

> **NOTE** — The Iteration 3 panel becomes relevant after a completed eligible reference exists. Its **Advanced — validation methods** are separate from the selected product operating guidance route. A full year Iteration 2B optimisation result is not the Reference A for Iteration 3; reopen the Iteration 2 result or run Iteration 2 again.

## 5. Iterations and how to compare them

| Stage | User meaning |
| --- | --- |
| Iteration 1a | MVHR design duty without a manufacturer unit; can provide an eligible reference for applicable Iteration 3 methods. |
| Iteration 1b | Natural ventilation; it is not a mechanical Reference A for the product system comparison. |
| **Iteration 2 / Reference A** | MVHR with a selected manufacturer unit, prepared and assessed using the legacy IZAM approximation. Its TAS/TM59 result is the reference for the normal product comparison. |
| Iteration 2B | Optional TM59 ventilation airflow optimisation of a completed Iteration 2 design, started with **Optimise (2B)…**. It has its own confirmation and results; it does not replace Reference A. |
| **Iteration 3 / Candidate B** | A paired full year case using explicit TAS Mechanical Systems and selected product behaviour. The main cooling route is **Selected product — manufacturer operating guidance**. |

Use A/B primarily to compare **Part O/TM59 pass or fail and whether room criteria change**. Reference A and Candidate B use different modelling abstractions. Their component values and room temperatures are **not expected to converge numerically**; a temperature difference alone is not evidence of an error. Use Candidate B's operating diagnostics to inspect airflow, recovery or bypass, cooling state, supply air and control behaviour.

## 6. Weather

Use the weather required by your project methodology: normal project weather, an alternative file, or a modified **full year** file. Iteration 3 uses the selected case's runtime temperatures and airflow; it does not assume London Heathrow, a particular DSY, a weather filename, or fixed peaks such as 34.6°C, 38.1°C or 40.3°C. The completed evidence records the weather identity and calculated dry bulb peak for QA. Check that these match the intended case before issuing results.

## 7. Output folders and evidence safety

Choose a **new output root for each new run**. SAM creates case folders such as `Iteration2` and `Iteration3` beneath it, with `tas`, `reports` and `diagnostics` evidence. Keep the result model together with its associated output tree when archiving or moving work.

> **WARNING** — If the selected case folder already belongs to another run, or holds older unowned evidence, SAM refuses to overwrite it. Choose a fresh **Output folder** and retry. Do not routinely delete an existing evidence folder to clear this warning. A retry of the **same run and same case** may use its folder. Replacing an existing completed Iteration 3 result is an intentional action and requires explicit confirmation; use **Open result** for ordinary review.

## 8. Cancel, resume and reopen

Cancelling after TAS has completed does not necessarily discard those stages. Retry the same run and case from the current session: SAM checks what completed work can be reused and may report **“Reusing the completed TAS results”** instead of starting TAS again. A changed design, weather, case or result identity can make that reuse unavailable; follow the refusal and prepare a new case where required.

To reopen a completed run later, open its saved Reference A result `.sam` file and select **Simulate → Part O → Prepare & Run**. Use **Review Results** for its TM59 result and, where a completed pairing exists, **Open result** in the Iteration 3 panel. This review does **not** rerun TAS. A reopened result model remains a result model, so **Prepare & Run** for a new baseline is disabled there. A reopened run may lack the in-session preparation needed to start a *new* Candidate B, even though its completed pairing remains reviewable.

SAM checks the saved model and result file pairing before presenting results. If a TSD is **missing**, restore the correct file or archive; if **stale** or **mismatched**, use the matching saved model and result set or rerun from the design. A moved evidence tree can remain valid when its relative references and file identity still match. Keep all case files together and use the UI's specific refusal to identify what must be restored.

## 9. Understand the results

**Review Results** gives the Reference A TM59 assessment. The Iteration 3 comparison shows **Reference case — TM59** and **System case — TM59** PASS/FAIL, the number of **TM59 outcomes changed**, and room by room criteria with actual value, limit and changed status. Filter **Failures**, **Changed**, or **Largest**, search a room, and inspect the temperature difference statistics. Read the TM59 reports for the criteria behind a verdict; an unchanged outcome means the criterion's pass/fail state stayed the same, not that temperatures matched.

For Candidate B, inspect **Per unit** and the **Comparison report** / `diagnostics` output for the selected control room and 22°C setpoint, first observed cooling, supply and extract flow, recovery or bypass, cooling state, supply air behaviour, weather identity and peak, and available hourly resultant temperature series. The operating history is evidence for diagnosing control and airflow; assess compliance from the TM59 result.

> **IMPORTANT** — `UNAVAILABLE` means the available hourly evidence cannot establish a state or value unambiguously. It does **not** mean zero, off, failure, or a reconstructed value. Some hours also lack a distinguishable stat signal. Treat those hours as a diagnostic limitation and inspect the surrounding evidence rather than assigning a state.

## 10. Current Nuaire modelling assumptions

The current nominal representation distinguishes background and cooling states; raises **balanced supply and extract airflow** during cooling; controls the DX and both fans from the explicitly selected representative room at **22°C**; represents heat recovery and bypass; applies the current **airflow dependent cooling heat exchanger behaviour** and a **13°C cooling floor** at the coil. It does not invent high outdoor temperature DX derating. The operating and temperature evidence supports this generic representation for project use, but it is **not manufacturer certification** of every component or operating condition.

# Current documented limitations

**READY WITH DOCUMENTED LIMITATIONS.** The following assumptions remain in place pending further manufacturer evidence:

- **Background HX efficiency:** background recovery remains **0.80**. Nuaire supplied an alternative airflow dependent equation, but it is unresolved whether that means exchanger only sensible effectiveness or package supply temperature efficiency. Do not reinterpret the current value as the confirmed alternative.
- **Fan / DX physical order:** the model reproduces the supported final sensible supply behaviour. The exact position of the supply fan relative to the DX evaporator awaits confirmation. Do not infer more detailed component behaviour from the represented topology.
- **Latent cooling and extended performance curves:** the existing worked example does not establish additional latent or performance curves. Refinement needs measured manufacturer data.
- **Diagnostic ambiguity:** an individual hour may be `UNAVAILABLE` or lack a distinguishable stat/control signal. This limits attribution; it is not currently an established modelling defect.
- **A/B numerical difference:** Reference A is the legacy IZAM approximation; Candidate B is an explicit Mechanical Systems representation. Do not expect component or room temperature convergence.

## Troubleshooting

| Symptom | Why and what to do |
| --- | --- |
| **Run Iteration 3** disabled: no cooling control room | A cooled dwelling needs an explicit saved room. In **Mixed Design**, select the cooled dwelling, choose **Cooling control room**, click **Confirm control room**, then **Save selection** and save the design model. Prepare and run Reference A from that design. |
| Selected room is invalid or belongs to another dwelling | Choose a room belonging to, and served by, that dwelling; confirm and save it. Review the preflight unit summary again. |
| **Prepare & Run** disabled on an existing result | The open file is a result, not a clean design baseline. Reopen the pre Part O design or use **Results → Part O → Remove Results...** to save and open a checked cleaned copy. |
| Output folder occupied | The chosen case folder contains another run's evidence. Select a fresh output root. Retain the prior folder for review. |
| TAS result missing, stale or mismatched | Restore the corresponding saved result model and complete output tree, or start a fresh run from the design. Read the refusal for the specific mismatch. |
| `UNAVAILABLE` in operating history | That hour's evidence cannot prove the value/state. Inspect adjacent hours and the TM59 assessment; do not substitute zero or off. |
| Need to review an old run versus start a new one | Open the saved result and use **Review Results** / **Open result** for review. Open the clean design and choose a fresh output root for a new run. |
| Iteration 3 asks to replace a completed result | **Open result** if you only need to inspect it. Confirm replacement only when intentionally rerunning that method and prepared to replace its prior Candidate B evidence. |

## Quick Start

1. Open and check the clean SAM design model and Part O dwelling/space setup.
2. Confirm the ventilation design duties, selected units and cooling strategy.
3. In **Mixed Design**, set **Cooling control room** for every cooled dwelling; **Confirm control room**, **Save selection**, and save the model.
4. Open **Simulate → Part O → Prepare & Run**.
5. Choose **Iteration 2 — MVHR with manufacturer unit** for the normal product reference and check **Equipment selection**.
6. Set **Scope** and check the dwellings to assess.
7. Expand **Simulation case**; select full year **Weather**, **Solar calculation** and a fresh **Output folder**.
8. Resolve **Readiness** blockers; click **Prepare & Run**, review the preparation and click **Accept & Run TAS**.
9. Use **Review Results** to check Reference A's TM59 outcome.
10. In the Iteration 3 panel select **Selected product — manufacturer operating guidance**, inspect preflight, then click **Run Iteration 3**.
11. Click **Open result**; compare TM59 outcomes and changed criteria, then inspect Candidate B diagnostics and weather provenance.
12. Archive the result model with its complete output tree; reopen with **Review Results** / **Open result** when needed.
