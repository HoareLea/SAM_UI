# User libraries - PR1: shared engine + glazing Rename / Remove (no UI)

Scope: `SAM_UI` only. No UI, no model code, no opaque library. Behaviour of Save is unchanged. Base: `sow/2026-Q3` = `4100a1e4`.
This PR is the first of three stacked PRs (PR1 engine -> PR2 "My library" manager window -> PR3 Builder edit / Save and replace).

## 1. What changed

| File | Change |
|---|---|
| `Classes/UserLibrary/UserLibraryFile.cs` (new, internal) | The persistence engine moved out of `UserGlazingLibrary`, same behaviour and error wording: lock file (`FileShare.None`, `DeleteOnClose`, timeout message unchanged), `Read()` (Missing / Ready / Unreadable), `Parse`, shared-read retry, `Write` (verify read-back, temp + `File.Replace`, `.bak`, temp cleanup), `Transact(edit)` = lock -> re-read -> refuse unreadable -> edit -> write -> release, `Notify` (isolated handlers, after release), `Companion(path)` (the same engine on a second file that is only written under this file's lock). Test seam: `BeforeWrite`. |
| `Classes/UserLibrary/UserLibraryArchive.cs` (new, internal) | `<name>.removed.json` path, and the by-Guid merge into the archive (see section 2). |
| `Classes/UserLibrary/LibraryMaterialMerge.cs` (renamed from `GlazingMaterialMerge`) | Same `Add`; new `Prune`, `ReferencedNames`, `RenameLayers`. |
| `Classes/GlazingBuilder/UserGlazingLibrary.cs` | `Read` / `Save` go through the engine; public API, `Changed`, `Shared`, `DefaultPath`, `BackupPath`, `LibraryName`, `UserGlazingLibraryState`, result types unchanged. New public `Rename(Guid, string)`, `Remove(Guid)`, `ArchivePath`, `UserGlazingEditResult`. |
| `Query/ComposeGlazingSystem.cs` | `GlazingMaterialMerge` -> `LibraryMaterialMerge` (mechanical). |

## 2. Identity rules (unchanged intent, now enforced by the API)

* A saved system is immutable; every Save is a new Guid.
* **Rename** changes `Name` only (`new ApertureConstruction(guid, entry, name)`): same Guid, layers, materials and provenance. Tested byte-identical except `Name`.
  The name is trimmed, non-empty and unique case-insensitively **excluding the system itself** (a case-only rename is allowed); checked under the lock.
  Renaming to the name it already has succeeds, writes nothing and raises nothing (`Modified == false`).
* **Remove** MOVES the entry to the archive; nothing is deleted. Models that already use it keep their own copy.
* `Changed` is raised once per successful change, after the lock is released, never on failure.

## 3. The archive failure contract (Step 0 - proven, not assumed)

> **Remove never loses an entry.** Either it fully succeeds (the entry is in the archive and gone from the library) or the entry is still in the library.

Remove writes two files under the library's one lock, **archive first, library second**:

| Failure | Result | Test |
|---|---|---|
| archive write fails (injected, or archive is read-only, or a folder sits on the archive's name) | nothing changed; entry still in the library; error says "Nothing was removed"; no temp / lock left; retry succeeds | `When_the_archive_cannot_be_written...`, `A_read_only_archive...`, `A_folder_on_the_archives_name...` |
| archive is unreadable | refused, **neither file written**, archive left as it is | `An_unreadable_archive_is_never_overwritten...` |
| library write fails after the archive succeeded | entry is in BOTH files (harmless: a Guid still in the library counts as not removed); `Changed` not raised; retry is idempotent | `When_the_library_cannot_be_written_after_the_archive...` |
| lock held / library unreadable | refused, nothing written (archive not created) | `Remove_never_overwrites_an_unreadable_library...` |

Main-first was rejected: a failed archive write would leave the entry only in `.bak`.

Decisions the tests led to (differences from the plan text, all adopted deliberately):

1. **The archive replaces an entry by Guid with the version being removed now** (not "keep the first"). After a failed Remove the user may rename the
   entry and retry; the archive then holds what was last removed, still exactly one entry per Guid. Test: `A_retry_after_a_failed_remove_archives_the_entry_as_it_is_now...`.
2. **The archived entry carries EVERY material it references, not only the pruned ones**, so the archive is a self-contained source (`Add source…` reads it and
   the candidate has no material issue). Test: `The_archive_is_a_self_contained_source...`.
3. **A different material of an archived name** (an earlier removed entry's `Clear4` 1.0 vs this one's `Clear4` 0.8 - possible because the library prunes `Clear4` when
   its only user is removed) is kept in the archive as `Clear4 2` and the archived entry's layers **and its Builder provenance material labels** follow. Test:
   `Two_removals_accumulate...`. Without this the archive would silently conflate two different materials under one name.
4. **Pruning is scoped to what the removed entry used.** `Prune(library, candidates, referenced)` removes only materials the removed entry named and no remaining
   entry references - a pane or frame layer of any remaining system, or a layer of any opaque `Construction` in the same file. A material nobody used before (an
   orphan in a hand-edited file) is never touched. (The plan's `Prune(MaterialLibrary, referencedNames)` signature would have swept orphans.) Test:
   `A_material_a_remaining_systems_frame_or_the_files_opaque_constructions_use_is_never_pruned...`.
5. The archive is only ever written under the library's lock, so it needs no lock of its own (`Companion` has its own, unused, lock path).

Not done / out of scope: no Restore UI (D1); no archive compaction; the archive is not read by any panel.

## 4. Tests

New (all pass): `UserLibraryFileTests` (23: states, never-overwrite-unreadable, `.bak`, no temp/lock, lock wait / timeout, concurrent transactions, edit under the lock,
injected write failure, companion, archive path, `Notify` isolation), `UserGlazingLibraryLifecycleTests` (31: Rename, Remove, material pruning rules, the failure contract,
write order, `Changed`, open `GlazingViewModel` refresh), `UserGlazingLibraryOpenListTests` (2 `[WpfFact]`: a Rename / Remove reaches the OPEN Thermal Performance list through
`Changed`; new name + same values + Tas not asked again; removed row leaves and a choice of it resets; model JSON, Modified, history and Undo untouched).

Existing `UserGlazingLibraryTests` / `UserGlazingCandidateTests` / Builder tests: **no assertion changed** (only `GlazingMaterialMerge` -> `LibraryMaterialMerge` and the new
types added to the Builder IL-scan type list, `BuilderSurface`, so the "holds no model" scan covers them).

A byte-for-byte golden file of the old format was not added: the unchanged `UserGlazingLibraryTests` (saved systems never change, atomic write, `.bak`, identical-material reuse,
round-tripped provenance) already pin the format, and the file is still plain `ConstructionManager` JSON read by `Add source…`.

## 5. Results

* Full WPF suite (`dotnet test WPF/SAM.Analytical.UI.WPF.Tests -c Debug`, one run at the gate): **2290 / 2290 passed** (2234 baseline + 56 new).
* Focused repetition of the concurrency / lifecycle / open-list / existing library+candidate tests (109 tests): **10 consecutive runs, all green**.
* Existing `UserGlazingLibraryTests`, `UserGlazingCandidateTests`, Builder, pane-browser and Builder-window tests: 184 passed with no assertion changed.
* Diff read for: no UI change, no model reference (the Builder IL scan now also covers the engine, archive and merge types), no path in provenance, error wording unchanged, file format unchanged.
