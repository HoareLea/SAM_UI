# SAM User Libraries and Builder

User guide for **My library**, the **Glazing System Builder** (called the Builder below) and **My constructions** in the SAM Thermal Performance panel.

*Documents the behaviour of SAM_UI branch `sow/2026-Q3` at `a16ff93f` (User Libraries / Builder programme, PR1–PR6).*

---

## 1. Overview

### What My library is

**My library** holds the reusable thermal definitions you create yourself. It has two kinds of content:

| Library | What it stores | Where it appears in SAM |
|---|---|---|
| **My glazing systems** | Complete glazing systems (panes, gas gaps and an optional frame) you built in the Glazing System Builder | As candidates in a window's **Change…** list |
| **My constructions** | Opaque constructions (walls, roofs, floors) you saved from the Thermal Performance panel or the Constructions editor | As **My constructions** alternatives on an opaque row |

Both libraries are stored in your user profile (`Documents\SAM\User Libraries`), not in any model. Removed items are kept in a separate archive file next to each library.

### Why it exists

A construction or glazing system you develop in one model is normally trapped in that model. My library lets you keep it and use it in other models and in later SAM sessions, together with a record of where it came from and what U-values it had when you saved it.

### Model content versus library content

| | Model (project) content | My library content |
|---|---|---|
| Belongs to | One analytical model | You, across all models |
| Changed by | **Apply** (one Undo step) | **Save**, **Rename**, **Remove** |
| Appears in Undo history | Yes | **No** |

**Library operations never change the analytical model.** Saving, renaming, removing, opening the Builder and editing a draft do not touch the model and add no Undo step. A model changes only when you select an item in the Thermal Performance panel and click **Apply**. When you apply a library item, the model receives its own copy; the library item and the model are independent afterwards.

---

## 2. Opening My library

In the **Thermal performance** panel, under **Sources**, click **My library…**.

My library opens as a dialog over the panel. It has two tabs:

- **Glazing systems**
- **Constructions**

Each tab shows a list on the left and a **Details** pane on the right. Select an entry to see its details; until you select one, the pane shows a hint.

[Screenshot: My library — Glazing systems tab]
[Screenshot: My library — Constructions tab]

### Glazing systems tab

Each entry shows:

- **Name**, and the **date saved** (right-aligned)
- the **pane build-up** (thickness in mm, per layer)
- the values recorded when it was saved — `Ug · g · LT · Uf` — and the **frame** (frame layers, or "no frame")

The **Details** pane adds the system's short ID (the last 6 characters of its identity, which tells same-named systems apart), the full pane and frame layers (thicknesses in mm), the calculation engine used at save, and how it was built (see [section 9](#9-understanding-provenance)).

A system that was not made with the Glazing System Builder is shown as having no Builder provenance.

### Constructions tab

Each entry shows:

- **Name** and **date saved**
- the **build-up** (thickness in mm, then material)
- **U-value at save** (W/m²K)
- the **heat-flow basis** the U-value was calculated for
- the **source** — where it was saved from

The **Details** pane adds the short ID, the date saved and the provenance lines (saved from, based on, model name, U-value at save, target, heat-flow basis, route, engine).

### Rename

1. Select an entry and click **Rename…** (or press **F2**).
2. Type the new name. The naming rule is checked as you type: the name must not be empty and must not already be used by another entry (case and leading/trailing spaces are ignored).
3. Click **Rename** (or press Enter). **Cancel** (or Esc) abandons it.

Rename changes **only the name** shown in the library. The saved definition (layers, materials, values, provenance) and the item's identity are unchanged. Models that already use the item are not affected, and an open candidate list refreshes with the new name.

### Remove

1. Select an entry and click **Remove…** (or press **Delete**).
2. Confirm in the **Remove from My library** dialog.

Remove takes the item out of the active library. It is **archived, not deleted**: the definition is moved to a separate archive file (`Glazing Systems.removed.json` or `Constructions.removed.json`) next to the library, and the tab says so. Removed items no longer appear in My library or in the normal candidate lists. Models that already use the item keep their own copy.

> There is no Restore command (see [Current limitations](#11-current-limitations)).

If a library file cannot be read, My library shows a warning, leaves the file untouched and disables changes until the problem is fixed.

---

## 3. Managing saved glazing systems

Besides Rename and Remove (section 2), the **Glazing systems** tab has **Open in Builder…**, which opens the selected system for editing (section 4).

You can also reach these commands from the Thermal performance panel: open a glazing row's **Change…** list and right-click a candidate that comes from My glazing systems. The context menu offers **New system based on this…**, **Open in Builder…**, **Rename…** and **Remove…** (the last three only for systems of My glazing systems; **New system based on this…** is offered for every candidate).

Right-clicking a candidate opens its menu **without choosing it**, so it does not become the pending change.

---

## 4. Creating and editing glazing systems

The **Glazing System Builder** edits a temporary **draft**. It has no analytical model behind it: nothing you do in it changes a model until you save a system, choose it in the panel and click **Apply**.

[Screenshot: Glazing System Builder — whole window]

### Three ways in

| Entry point | Where | What you get |
|---|---|---|
| **Create new…** | A glazing row → **Change…** → **Create new…** | A new draft that starts from the system currently chosen in the list |
| **New system based on this…** | Right-click any candidate in the **Change…** list | An **independent copy** of that system as a new draft. Saving it adds a new system to My glazing systems; the system you copied is not touched |
| **Open in Builder…** | My library (Glazing systems tab) or right-click a My glazing systems candidate | An **editable draft based on the saved definition**. See "Editing a saved system" below |

A status line under the **Name** box shows where the draft stands, for example *New · based on SIM_EXT_GLZ · not saved* or *Editing a copy of X · saving creates a new system*.

### Building the system

- **Name** and **Intended use** (what the system is for; it decides the orientation used for gap heat transfer) are at the top.
- The **Build-up (outside → inside)** list holds panes and gas gaps. Use **Add pane**, **Replace pane**, **Add gap**, **Remove**, **↑ Outwards**, **↓ Inwards** and **Reverse pane**. Keyboard: Alt+Up / Alt+Down move a layer, Delete removes it.
- Choose panes from the **Panes** browser on the right (select a source, search by name or category, then use **Add pane** or **Replace pane**). **Add source…** adds a Tas glazing database (`.tcd`) or a JSON file of panes.
- Gap width is entered in **mm**; press Tab to apply. Gas can be air, argon or krypton.
- The **Performance (Tas)** box shows **Ug**, **g**, **LT** and **Uf**, and a labelled example **Uw** (for comparing builds — not the value the model will get; the model's own Uw is calculated from each window's areas when you apply a system).
- The validation summary and issue list under the build-up show errors and warnings. **Save is disabled while there are errors.**

### Draft only until you save

Everything you change in the Builder — layers, frame, name — stays in the draft. **Cancel** closes the Builder and discards the draft; nothing is saved and nothing in the model changes. (The Builder does not currently ask for confirmation before discarding an edited draft.)

### Editing a saved system (Open in Builder…)

Editing never changes the saved definition directly. The Builder opens a copy of it as a draft. When you finish, two save buttons are offered.

#### Save as new

Use when you want to **keep the existing saved definition** and add a variation.

- The draft is saved as a **new** system in My glazing systems.
- The original stays exactly as it was.
- The name must be different from the original (the Builder shows a hint if it is the same).

When you start from **Create new…** or **New system based on this…**, the same button is labelled **Save as predefined**; it does the same thing: it adds a new system to My glazing systems.

#### Save and replace *name*

Use when you intentionally want to **supersede** the saved definition.

- SAM saves your edited result as a **new definition** and **archives the one you opened**, in one operation.
- The name may stay the same.
- Models that already use the previous definition are **not silently changed**; they keep their own copy.
- The new definition records which one it superseded (see section 9).

Because the replacement is a new definition, the previous version stays in the archive rather than being overwritten. SAM keeps a stable identity for each saved definition internally; you do not need to manage it.

After a successful save, an open **Change…** list picks up the new system automatically. The Builder reports "Saved to My glazing systems as *name*" (or "…, replacing *name* (kept in the archive)").

---

## 5. Authoring frames

The **Frame** section of the Builder (below the build-up) controls the frame of the system.

[Screenshot: Glazing System Builder — Frame authoring]

### Frame choices

Open the **Frame** drop-down and choose one of:

| Choice | Meaning |
|---|---|
| **None (glass only)** | No frame; the system is glass only (Uw = Ug) |
| **Own frame (add the layers below)** | A frame you build; it starts with no layers |
| **Frame of *system*: *layers* (*source*)** | The frame of an existing system, **copied** into your draft. The existing system is not changed |

### Editing frame layers

When the frame is **Own** or copied, the frame editor appears. Each layer shows its material and its thickness.

| To… | Do this |
|---|---|
| Add a layer | Choose a material (below), then **Add layer**. It is added after the selected layer, or at the end when none is selected |
| Change a material | Select the layer, choose a material, click **Replace material**. The thickness stays |
| Change a thickness | Type in the layer's thickness box (**mm**) and press Tab |
| Reorder | Select the layer and click **↑ Up** / **↓ Down** |
| Remove | Select the layer and click **Remove layer** |

Removing the last layer leaves the system without a frame.

### Material search

Type words in the search box above the material drop-down to find a material by name or source (for example `timber`). The list contains the **solid** materials of the model, the libraries and the added sources; a count (for example "3 of 120 solid materials") is shown below.

### Frame width

Type the frame's face width in **mm** (press Tab) in **Frame width**. It is saved as the system's default frame width. The Builder also shows the frame **depth** (the sum of the layer thicknesses).

If a copied frame stores no width, its depth is proposed as the width; change it if the face is different.

### Frame thermal result

- **Uf — frame layers, 1-D, Tas** is calculated by Tas from the layers as you edit them.
- **Frame additional heat transfer** is **read-only**. It shows the value carried by a copied frame (for example `20 %`) or `none`. An own frame never has one. Tas applies it on top of the frame layers, also after you edit the layers of a copied frame.

You cannot type a declared Uf, and you cannot edit the additional heat transfer.

### Frame validation

| Condition | Result |
|---|---|
| Layer thickness shown/entered in **mm** | — |
| Layer thickness missing, not a number, or not greater than 0 | **Error** — Save is disabled |
| Gas material in a frame layer | **Error** — Save is disabled |
| Glass material in a frame layer | **Warning** — can be saved; Tas will calculate the frame as glazing |
| Frame width given but not a positive number of mm | **Error** — Save is disabled |
| Frame width left empty | **Warning** — SAM would use the frame layers' depth as the width |

---

## 6. Saving opaque constructions

An opaque construction can be saved to **My constructions** as a **new, independent construction**. Only opaque constructions can be saved; transparent, gas-only and layerless constructions are refused with a message.

### From the Thermal Performance panel

When working with an opaque construction in the Thermal workflow, use its row in the Thermal Performance panel and click **Save to My constructions…**. SAM saves, in this order of preference:

1. the **alternative you have chosen** in the Alternatives list;
2. otherwise the **generated U-value variant** (once its target U-value is reached);
3. otherwise the **current construction** of the row.

The button's tooltip states exactly which of these will be saved.

You can also right-click any alternative in the Alternatives list and choose **Save to My constructions…**. That saves the alternative you right-clicked **without choosing it**.

[Screenshot: Thermal Performance panel — opaque row with Save to My constructions…]

### The name prompt

A **Save to My constructions** window asks for a **name**. Names must be unique in My constructions (not empty, and not already used — case and surrounding spaces are ignored); the problem is shown as you type. Click **Save**.

The panel then shows "Saved '*name*' to My constructions." If the save is refused (for example the name is taken, or a material is not in the material library), the message is shown in red and nothing is saved.

Saving does **not** change the model, start an edit, choose anything, or affect a pending change.

### From the classic Construction editor

The existing **Constructions** editor also provides **Save to My constructions…**. Select a construction, click the button and give it a name. The editor stays open; neither it nor the model is changed. There is no separate opaque layer editor in My library — author a construction in the Constructions editor (or by generating a variant) and save it as a new one.

---

## 7. Reusing My constructions

Once saved, a construction is offered as a **My constructions** alternative on opaque rows in **every** model and session.

The Alternatives list collects candidates in this source order; if the same construction appears more than once, the first occurrence is shown:

1. **Model** — constructions already in the model
2. **Default library**
3. **My constructions**
4. **Added sources** — databases added with **Add source…**

To use one: select it in the Alternatives list and click **Apply** (section 8).

Saved glazing systems are reused the same way: **Change…** on a glazing row lists them, with the model's systems, the default library and added sources.

### Generated U-value constructions

When you type a **Target U** for an opaque row, SAM generates a construction by adjusting a layer's thickness to reach it (the preview line shows, for example, `U 0.260 → 0.180 · Mineral wool 80 → 124 mm`). You can:

- **preview** it,
- **save** it to My constructions with **Save to My constructions…** — this does not apply it and does not change the model, and
- **apply** it to the model with **Apply**.

Saving and applying are independent: you may do either, both, or neither.

---

## 8. Applying changes to the model

| Action | Changes the model? | Adds an Undo step? |
|---|---|---|
| Open My library | No | No |
| Rename, Remove | No | No |
| Open in Builder, edit a draft, Cancel | No | No |
| Save as new / Save as predefined / Save and replace | No | No |
| Save to My constructions… | No | No |
| **Apply** | **Yes** | **Yes — one step** |

When you choose a construction or glazing system and click **Apply**:

- the panel first shows a check (**Before apply:**) and a one-line summary of the change;
- **Apply** creates **one intended model change**;
- **one Undo** reverts that change. The panel notes "One Undo reverts it."

**Discard** drops the pending choice without changing the model.

When a system or construction from My library is applied, any material it needs that the model lacks is added to the model (the summary says "Adds *n* materials to the model").

### Reusing items in other models and after removal

- Saved items persist: they are stored in your user profile, so they are available in other models and in later SAM sessions.
- **Removing** an item from My library does **not** retroactively change models that already use it: each model has its own copy.

### Material conflicts

If a library item needs a material whose name matches a material already in the model but whose **properties differ**, **Apply is blocked** with a message such as *Its material 'X' differs from the model's material of the same name.* This is intentional: SAM does not silently substitute a different material. SAM does not resolve the conflict automatically.

If you hit this: review the material definitions, and either bring the model's material in line with the one the item needs, or choose a different item. If a material is simply missing from its source, the message reads *Its material 'X' is not in …*.

---

## 9. Understanding provenance

When you save to My library, SAM records where the definition came from, so you can judge it later. The **Details** pane of each My library entry shows it.

**Glazing systems** record, where applicable:

- when it was saved;
- what it was **based on** (the system you copied or edited), or that it is not based on another system;
- the origin of each pane and of the frame (own frame, frame copied from a named system, or a copied frame you then edited);
- the **Ug / g / LT / Uf** calculated when it was saved, and the engine that calculated them;
- for **Save and replace**, which definition it **superseded**.

**Constructions** record, where applicable:

- when it was saved;
- **where it was saved from** — generated variant, model, default library, an added source (by file name), My constructions, or the Constructions editor;
- the construction it was based on, and the name of the model it was saved from;
- the **U-value at save** and, for a generated variant, the **target** U-value;
- the **heat-flow basis** (for example *Horizontal heat flow, external surfaces*);
- how it was obtained (the route), and the engine that calculated it.

Provenance travels with the definition into any model it is applied to. It contains names and file names only, never folder paths. Items that have no provenance (for example systems not built in the Builder) say so.

---

## 10. Common validation messages

### Glazing System Builder

| Message (abridged) | Severity | What to do |
|---|---|---|
| The system needs a name. | Error | Enter a name |
| A system named '…' is already in My glazing systems; choose another name. | Error | Use another name (**Save and replace** may keep the name) |
| Pane *n*: it has no thickness. / Gap *n*: it has no width. | Error | Set a thickness or width |
| Pane *n*: '…' is not a glass pane. | Error | Choose a glass pane |
| Pane *n*: its material '…' is not available. | Error | Replace the pane |
| Gap *n*: no gas is chosen. / … is not offered; use air, argon or krypton. | Error | Choose a supported gas |
| Gap *n*: … mm is outside 4–30 mm, where the gas heat transfer correlation (EN 673) is used here. | Warning | Check the gap width |
| Pane *n* touches the pane before it / Gap *n* follows another gap | Warning | Check the build-up |
| No intended use is chosen … | Warning | Choose an **Intended use** |
| Frame layer *n*: it has no thickness. / its thickness must be more than 0. | Error | Enter a positive thickness in mm |
| Frame layer *n*: '…' is a gas; a frame layer needs a solid material. | Error | Choose a solid material |
| Frame layer *n*: '…' is a glass material; Tas will calculate the frame as glazing. | Warning | Usually choose a different material |
| The frame width is not a positive number of millimetres. | Error | Enter a positive width or leave it empty |
| The frame has no width: SAM would use the frame layers' depth as the frame width. | Warning | Enter a width |
| No frame: the system is glass only (Uw = Ug). | Info | — |

A summary line shows *✓ Ready to save*, *⚠ n warnings: it can be saved* or *✕ n errors: it cannot be saved yet*.

### My library and Save to My constructions

| Message | Meaning |
|---|---|
| A system named '…' is already in My glazing systems. | Rename target is taken |
| The construction needs a name. / A construction named '…' is already in My constructions; choose another name. | Name rule |
| … cannot be saved: its material '…' is not in the material library. | The construction's material is unavailable |
| … cannot be saved to My constructions: only opaque constructions can … | Transparent/gas constructions belong with glazing systems |
| The generated variant has no U-value yet: type a target U-value the construction can reach. | Enter a reachable target first |

A refused Rename, Remove or Save is shown in red; the library is unchanged.

---

## 11. Current limitations

The following functionality is **not currently provided**:

- **No Restore** for archived items. Removed systems and constructions stay in the archive files but there is no command to bring them back.
- **No Duplicate** command in My library. (Use **New system based on this…** or **Save as new** for glazing; save an alternative again under a new name for constructions.)
- **No hard-delete** workflow. Remove always archives.
- **No declared frame Uf / synthetic equivalent frame.** Frame additional heat transfer is read-only; you cannot type a Uf.
- **No dedicated frame material editor.** Frame layers use existing solid materials.
- **No dedicated opaque material or layer editor in My library.** Constructions are authored in the Constructions editor or generated from a target U-value, then saved.
- **No "reset frame to the original copy"** command. There is currently no **Reset frame to original copy** command. To discard the current draft changes, Cancel and reopen the Builder; reopening a saved system only opens a fresh draft and changes nothing in the library.
- **No confirmation when you cancel or close** the Builder with an edited draft; the draft is discarded.
- **No automatic resolution of material conflicts** (section 8).
- Only **opaque** constructions can be saved to My constructions; glazing systems are saved through the Builder.

---

## 12. Quick workflow examples

### Example A — Create a glazing variation

1. In the Thermal performance panel, open a glazing row's **Change…** list.
2. Right-click the existing system → **New system based on this…**.
3. Edit the panes, gaps or frame in the Glazing System Builder; give it a new name.
4. Click **Save as predefined** (when editing a saved system from My library the button reads **Save as new**).
5. Back in the **Change…** list, select the new system. The model has not changed yet.
6. Click **Apply**. One Undo reverts it.

### Example B — Replace a library glazing definition

1. Click **My library…** → **Glazing systems** tab → select the system → **Open in Builder…**.
2. Edit the draft (the saved definition is not touched).
3. Click **Save and replace *name***.
4. The previous definition is moved to the archive; the edited result is now the available definition in My glazing systems. Models already using the old one are unchanged.

### Example C — Save a generated opaque construction

1. In the Thermal Performance panel, find the opaque construction's row.
2. Type a **Target U** (W/m²K). The preview line shows the generated construction.
3. Click **Save to My constructions…** and enter a unique name → **Save**. The model has not changed.
4. Optionally click **Apply** to use the generated construction now (one Undo reverts it).
5. Later, in any model, the construction appears under **My constructions** in the alternatives list.

### Example D — Tidy up the library

1. **My library…** → pick a tab → select an entry.
2. **Rename…** to fix a label, or **Remove…** to archive it. Neither changes any model.

---

## Appendix: Screenshots

This guide does not embed screenshots. The bracketed placeholders mark where one would help:

- My library — Glazing systems tab; My library — Constructions tab
- Glazing System Builder — whole window; Glazing System Builder — Frame authoring
- Thermal Performance panel — opaque row with Save to My constructions…
