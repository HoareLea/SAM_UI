# User libraries - PR5: richer frame authoring in the Glazing System Builder

Base `sow/2026-Q3` = `710d8a5b` (PR1-PR4 merged; PR4 = #187). SAM_UI only (`SAM.Analytical.UI.WPF`); no SAM, SAM_Tas or Grasshopper change. One independently reviewable PR; PR6 (UI pass) is not started.

## 1. Problem

Until now the Builder could only choose "no frame" or COPY the frame of an existing system and type its width. A user could not say what the frame is made of: no layer could be added, replaced, resized or removed, and a copied frame's additional heat transfer was invisible. This PR lets a user author the frame that a glazing-system draft carries, with the thermal information the model supports, and keeps everything the earlier PRs established.

## 2. Authoring model (what the engineering model supports, nothing more)

SAM stores a frame as an ordered list of `ConstructionLayer` (material name + thickness) plus two parameters: `DefaultFrameWidth` (face width) and `FrameAdditionalHeatTransfer` [%]. Tas calculates Uf from the layers (1-D). So a frame is authored as:

* **Frame choice**: *None (glass only)* | **Own frame** (new) | the frame of an existing system (copied, as before). Selecting a frame starts from it; the layers of ANY frame (own or copied) can then be edited.
* **Layers**: add a layer of a chosen solid material (default thickness of the material, else 30 mm; after the selected layer or at the end), replace a layer's material (thickness stays), type the thickness in mm, move up/down, remove. Removing the last layer leaves no frame. A copied frame whose layers differ from the copy is "edited"; put back, it is a plain copy again.
* **Materials**: the solid (opaque) materials of the Builder's sources - the model, the default library, "My glazing systems" and added sources - listed once each by definition, with a search box and their conductivity and source. No second material editor: materials are chosen, not edited (the classic Materials editor stays the place for that).
* **Width**: as before (explicit, mm). A copied frame that stores none still gets its depth proposed (and says so; it follows the layers while it is the proposal). An own frame gets NO invented width: the existing warning "the frame has no width" stands until one is typed. A typed width that is not a positive number is an **error** (it used to be silently "no width").
* **Derived, read-only**: *Depth (sum of the layers)*, Uf (Tas, as before) and **Frame additional heat transfer** - a label (a `TextBlock`, UIA control type Text, no ValuePattern; named "... - read-only: ...") showing "10 %", "none" or "no frame", with a note: carried with the frame copied from X (also after its layers were edited), or "an own frame has none". There is no way to type it; no declared Uf makes a synthetic frame (D2: deferred, not started). A test pins that the type has no setter and no such method.

## 3. Validation (Save is disabled while there are errors)

Per frame layer, numbered ("Frame layer 2: ..."; choosing the finding selects the layer): thickness missing (error), not more than 0 (error; SAM's own duplicate record is not listed twice), a gas material (error), a glass material (warning: Tas calculates the frame as glazing); a missing material stays SAM's own record ("Frame: ... does not contain Material", unchanged); typed width not positive (error); no width (warning, as before).

## 4. Identity and save semantics (unchanged contract)

Frame edits are draft-only (the seed system, the library and every file are untouched until Save; test). Save as predefined / Save as new = a NEW Guid; Save and replace = new Guid + `Supersedes` + the old system archived with ITS frame (archive-first, as PR3). The frame is fully part of the saved `ApertureConstruction` (layers, materials embedded, `DefaultFrameWidth`, additional heat transfer only when copied), so reopening reproduces it.

## 5. Provenance - schema 3 (additive, backward compatible)

`GlazingBuilderProvenance.CurrentSchemaVersion` 2 -> 3. New: `Frame Layers` (position, material after any rename, original name, source label, source FILE NAME, thickness - no paths) and two new values of `Frame`: `CopiedEdited` (copied from X, then edited; origin name/Guid kept) and `Authored`. `Copied` / `None` are written as before. Readers take missing keys as none: schema-1/2 systems read and re-save exactly as before (test). Reports and "My library" details say "copied from X and edited" / "built in the Builder" and list the layers; "copied" keeps its old words. Reopening restores each layer's own source label and the frame origin.

## 6. Code

`GlazingSystemDraft.cs`: `DraftFrame` editable (`DraftFrameLayer`, `AddLayer`, `ReplaceMaterial`, `RemoveLayerAt`, `MoveLayer`, `IsAuthored`, `IsEdited`, `Depth`, `WidthInvalid`; the existing members keep their signatures). `ComposeGlazingSystem`, `CheckGlazingDraft`, `GlazingDraftIssue` (`FrameLayerNumber`; the old constructor is kept), `GlazingBuilderProvenance` (+ `GlazingBuilderFrameRecord`), `UserGlazingLibrary` (frame records follow a material renamed on save), `GlazingBuilderViewModel` (frame choice/rows/pool/commands/derived texts), new `GlazingBuilderFrameLayerRow`, `GlazingBuilderOptions` (`GlazingFrameChoice.Own`, `GlazingFrameMaterialChoice`), `GlazingChangeReport.GlazingBuiltFrom`, `GlazingSystemBuilderWindow.xaml(.cs)` (frame list, buttons, material search + picker, depth, read-only additional heat transfer row in the performance block).

## 7. Tests

New `GlazingFrameAuthoringTests` (18, plus 1 in `GlazingSystemBuilderWindowTests`; suite 2403 -> 2422): draft object (own frame, remove-last, edited/put-back, no setter), compose + provenance (`Authored`, `Copied` -> `CopiedEdited`), schema 2 -> 3 reading, report lines, per-layer validation, choices and material pool (solid only, once, search), full own-frame build through the view-model (Tas re-asked on every edit, Uf, depth, width, replace/move/remove), read-only additional heat transfer (copy "10 %", none for own, kept after edits), proposed width, invalid inputs block Save, draft-only editing, saved own frame reopens identically + Save as new (new Guid, no material added/renamed), Save and replace of an edited copied frame (new Guid, Supersedes, archive keeps the old frame, reopens as edited copy), a schema-2 system reopens and saves as before; the real-window test (named controls, a TextBlock and no editor for the additional heat transfer, Add layer/edit/remove in the real XAML). Existing tests changed on purpose: the schema is 3 (3 asserts), the frame choices are None/Own/copies (1 test).

## 8. Native acceptance (real app + real Tas, Release app copy, driver `fa_accept.ps1`) - PASS

Model hash baseline `EA79562E984F`; the user's library was removed first and restored afterwards (`3D481CE02046`).

| Step | Result |
|---|---|
| Change... > Create new... | Builder on SIM_EXT_GLZ with its copied frame (1 layer, 50 mm), width 50 proposed, Uf 2.20, additional heat transfer "none" (read-only label, UIA type Text, no ValuePattern, 0 editable boxes naming it) |
| Edit the copied frame's layer 50 -> 90 mm | Uf 2.20 -> 1.47 W/m2K (Tas), proposed width followed (90), note "were edited", additional heat transfer unchanged |
| Own frame; add 2 layers from the sources' materials (search "timber", "insul"); layer 2 -> 25 mm; width 70 | 120 solid materials offered; Uf 4.90 (two 1 mm layers) -> 1.20; depth 26 mm; additional heat transfer "none - an own frame has none"; no width until typed (warning shown) |
| Save as predefined | Builder closes, the new system is in the Change list and selected; library: schema 3, `frameKind=Authored`, width 0.07, frame layers + materials embedded, provenance records with source label "Default library"; model hash = baseline, Undo disabled |
| My library > Open in Builder | "Editing a copy of ...", Own frame selected, the same two rows (identical text), width 70, Uf 1.20; Cancel changes nothing |
| Save as new (layer 1 -> 45 mm, new name) | second system with another Guid; the first unchanged |
| Save and replace (layer 2 -> 31 mm) | new Guid, `supersedes='FA Own frame'`, layers 1 / 31 mm; archive holds the old Guid with its old frame (1 / 25 mm) |
| Model before Apply | hash = baseline; Undo disabled |
| Apply (only the selected window) | "1 aperture now FA Own frame. One Undo reverts it."; the model's aperture construction carries the frame (timber 1 mm + insulation 31 mm), width 0.07, no missing materials; the other 19 windows untouched |
| One Undo | Undo disabled, model hash = baseline `EA79562E984F` |

Driver observation (not a defect): the default library's timber stud declares a default thickness of 1 mm, which is what Add layer uses (the material's own default); the thickness is one click away from being edited.

## 9. Compatibility

Public API of `SAM.Analytical.UI.WPF` is additive (new types/members); no member was removed (the 5-parameter `GlazingDraftIssue` constructor was kept beside the new one); `GlazingBuilderProvenance.CurrentSchemaVersion` is now 3 (value of a public const; no consumer outside SAM_UI). `DraftFrame.Layers/Materials/AdditionalHeatTransfer` keep their types and meaning. The only consumers of SAM_UI assemblies are SAM_UI's own Grasshopper projects, which do not use the Builder; no Grasshopper rebuild was run (it would also overwrite the deployed plugins) and none is needed.

## 10. Deferred

Editable declared Uf -> synthetic frame (D2); a frame material editor; per-edge frame widths / spacer Psi; a "reset frame to the copy" button (choose another frame and back); PR6 UI polish.
