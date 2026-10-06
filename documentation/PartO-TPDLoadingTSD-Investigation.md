<!-- SPDX-License-Identifier: LGPL-3.0-or-later -->
<!-- Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors -->

# Part O: the TPD `Loading TSD data` stall - investigation record

**Status (1 Oct 2026): investigated; NOT reproduced and NOT proven; intentionally left unfixed. Docs-only.** Read against `sow/2026-Q3` at SAM_UI `2c0798b`, SAM `c3890d5c`, SAM_Tas
`057faf3`, SAM_Systems `09063b4`. No production code or test was changed. The observation was made during the licensed Mixed acceptance run (SAM_UI#154).

## Observation (one occurrence)

Mixed Design *Build & Run* on the cleaned model: attempt 1 sat **7,420 s** with the TPD profiler step `Loading TSD data` open (normally ~20 ms; the passing retry took 1.9 ms). The TPD
server used under 1 s of CPU and showed no dialog. Killing it made the run fail cleanly with `COMException 0x800706BA (RPC server unavailable)`. Attempt 2, same inputs and build, finished
in **111 s**. The step is identified by the timing CSV the profiler writes in its `finally`; that is the only hard evidence, and it is a duration, not a cause.

## Where the time can be - by the code (demonstrated)

The whole delay is inside this span of `SAM.Analytical.Tas.TPD.Convert.ToTPD(SystemEnergyCentre, path_TPD, path_TSD, ...)` (SAM_Tas `Convert/ToTPD/TPD.cs`): `profiler.Step("Loading TSD data")`
-> `energyCentre.AddTSDData(path_TSD, 1)` -> `GetTSDData(1)`, i.e. **one synchronous COM call into the TPD (Plant & Distribution) out-of-process server** on the application's STA thread.
It is before the SAM_Systems graph is read, before any SAM-side loop, and before any simulation.

Call chain (Mixed, Systems route): `PartOMixedDesignWindow` Build -> `RunPartOMixedDesignCommand` (UI thread, synchronous) -> `RunPartOStrategySet` ->
`SimulatePartOMaterialisationSystems` -> `PartOIteration3Pipeline.ThermalSource` (TBD/TSD, `NoIzamThermalSource`) -> `PartOIteration3Pipeline.Route` ->
`SystemVentilationRoute` -> `ToTPD`.

What the code establishes:

| Question | Answer |
|---|---|
| Who writes the `.tsd`? | The TBD COM server, via a blocking `simulate`; then `Core.Query.WaitToUnlock(path_TSD)`, then the TBD/TSD documents are closed and `FinalReleaseComObject`'d in `using` blocks. Nothing copies, moves or reopens it before `ToTPD`. |
| What does `ToTPD` check first? | Only `File.Exists(path_TSD)`. No lock probe, no size check. |
| How is TPD started? | A fresh `new TPD.TPDDoc()` per document (`SAMTPDDocument`) on the caller's thread; no singleton, no `GetActiveObject`. Documents on one route are sequential. Whether the TPD *server* is shared across clients is COM registration, which the code cannot show. |
| Is there a timeout, kill, watchdog, token? | **None** anywhere on the path (nothing in SAM_Tas or SAM_UI calls `Kill`, `WaitForExit`, a timeout or a watchdog around TPD). The Route takes no `CancellationToken`; Cancel is "between stages", so a click during the hang is accepted and ignored. |
| Can SAM's own code deadlock here? | Not that was found. The progress callbacks fire on the calling thread and only set fields under short locks; the progress window never touches TPD objects and never marshals into the blocked thread. |
| Does any test cover it? | No: nothing covers `SAMTPDDocument`, `AddTSDData`, timeouts or hangs, and there is no seam at the COM call (`new TPD.TPDDoc()` and `energyCentre.AddTSDData` are concrete). The licensed `PartOMixedLargeProjectAcceptance` is env-gated. |

## What the evidence rules in or out

- **Against "the `.tsd` was still locked by TBD":** the route runs `WaitToUnlock(path_TSD)` after the TBD `simulate`, and it only returns once the file can be opened; had the file been
  held the run would have stalled there, in "Simulating Model", not in `Loading TSD data`. (The watch log also shows no TBD / TAS3D process in any sample after +139 s.) A *different* handle on the file (TBD/TSD server internals, a leaked RCW) is still possible and is not visible.
- **Against a SAM-side cause:** TPD CPU < 1 s, no dialog, and the call returned only when the RPC server died.
- **Not excludable:** TPD's own load blocking on server state (a previous run's TPD instance or document, the TSD reader, licence/RPC state). Both attempts overlapped TBD processes and only
  one hung, so the earlier hypothesis ("a TBD was alive when TPD started") is not supported as the discriminator.

## Separate defects seen on the way (not the cause)

1. **`SAM.Core.Query.WaitToUnlock` never increments its counter** (SAM `SAM.Core/Query/WaitToUnlock.cs`: `int i = 0; while (i <= count) { ... Sleep(waitTime); }`). A persistently locked file
   means an infinite 1 s sleep loop, not "up to `count` tries". It is called from SAM_Tas `Modify/Simulate.cs:84,147` (result discarded). It would show as a stall in `Simulating Model`,
   which is not what was observed; it is a genuine latent hang in a different step, left alone because the brief was the TPD stall and the fix lives in SAM.Core (shared).
2. `SAMTPDDocument.Dispose` skips `ReleaseCOMObject` if `Close()` throws, and a constructor that throws leaves the RCW to the CLR finalizer. Plausible after a dead RPC server, not a cause of one.
3. `Query.TBDDocument()` / `TSDDocument()` first `GetActiveObject("Document")` and `FinalReleaseComObject` whatever they find before creating their own - an attach to an ambiguous
   ProgID of unknown ownership. Nothing ties it to TPD.

## Decision

No fix: the stall is a single, unreproduced wait inside a vendor COM call, no SAM-side cause was demonstrated, and a timeout / process-kill would be a behaviour change (a kill
orphans or corrupts a TPD document and loses the run) justified only by a guess. Nothing was changed.

## If it recurs - the smallest useful next step

The missing evidence is *what else was alive and holding the file at that moment*. Without changing behaviour, `ToTPD` could write one diagnostic line next to its timing CSV just before
`AddTSDData`: whether the `.tsd` opens exclusively, its length, and the names/PIDs/start times of any TBD / TAS3D / TSD / TPD processes. A later, owner-approved step could add a bounded
wait with a clear refusal ("TPD did not load the results within N minutes; close TPD and retry") - but only once the diagnostic says what the hang is. Fixing `WaitToUnlock`'s counter is
independent and small. None of this was started.

## Status / next step

Closed as "not reproduced; evidence recorded". Re-open on the next occurrence, keeping the run's `*.timing.csv`, `*.route.timing.csv`, the process list and (if possible) a TPD dump
before killing it.
