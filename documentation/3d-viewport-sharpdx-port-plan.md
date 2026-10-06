# 3D viewport — port to HelixToolkit.Wpf.SharpDX v3 (#18 gate 3 / supersedes #16 PR 2)

Status: **plan / to be prototyped on a `sow/*` branch.** Decision recorded on issue #18 (gate 3):
the Phase-0 perf data shows `ViewportControl.ToMedia3D (ThreeDimensional)` dominates the 3D view
(**~3–5 s** full build on a 625-space model, run-to-run variance 2.5–5.6 s), so the chosen path is
to port the 3D viewport from the WPF `Media3D` renderer (`HelixToolkit.Wpf` 2.27.0) to the DirectX 11
renderer (`HelixToolkit.Wpf.SharpDX` 3.1.x). This supersedes the per-object mesh-merge that was
queued as #16 PR 2 — SharpDX wants few geometry models anyway, so the consolidation happens
naturally inside the new conversion layer.

**Floor plans are out of scope** — they now default to `FloorPlan2DControl` (`SAM_UI_FLOORPLAN_2D`
on by default, #18). The legacy Helix orthographic 2D path is therefore *retired* by this work, not
ported: the SharpDX viewport only renders the **3D perspective/orthographic-3D** view.

---

## Why SharpDX (recap of the #18 decision)

| | Today: `HelixToolkit.Wpf` 2.27.0 | Target: `HelixToolkit.Wpf.SharpDX` 3.1.x |
| --- | --- | --- |
| Renderer | WPF `Viewport3D`/`Media3D` (DX9-era retained, heavy per-`Visual3D` CPU cost) | DirectX 11 engine hosted in WPF |
| Hit-testing | CPU ray-mesh walk over the tree (#16 hover) | Built-in **octree** picking |
| Text | Meshed 3D text (`Text3DObject`, #15 cost) | Billboard text |
| Big scenes | No instancing/culling | Instancing + per-node frustum culling |
| Frameworks | net8.0-windows | net8.0-windows (single target; the net48 build mode was retired) |

Expected to attack `ToMedia3D` (fewer, GPU-resident geometry models), `HoverHitTest` (octree), and
the #15 text tessellation cost in one move.

---

## API mapping (current `ViewportControl` surface → SharpDX)

| Current (`HelixToolkit.Wpf` / `Media3D`) | SharpDX (`HelixToolkit.Wpf.SharpDX`) |
| --- | --- |
| `HelixViewport3D helixViewport3D` (templated in `ViewportControl.xaml`) | `Viewport3DX` (needs an `EffectsManager` + `RenderTechnique`) |
| `ModelVisual3D` per object; `Children` | `Element3D` / `GroupModel3D`; `Items` |
| `GeometryModel3D` + `Media3D.MeshGeometry3D` | `MeshGeometryModel3D` + `HelixToolkit.SharpDX.Core.MeshGeometry3D` (Vector3 Positions, int Indices, Normals) |
| Edge segments as thin meshes | `LineGeometryModel3D` + `LineGeometry3D` (native thick lines) |
| Points | `PointGeometryModel3D` |
| `Media3D.Material` (Diffuse/brush) | `PhongMaterial` / `DiffuseMaterial` (frozen-equivalent: SharpDX shares GPU resources) |
| `AmbientLight` in `Lights` | `AmbientLight3D` / `DirectionalLight3D` scene elements |
| `ProjectionCamera` / `Orthographic` toggle | `HelixToolkit.Wpf.SharpDX.PerspectiveCamera` / `OrthographicCamera` |
| `ZoomExtents()` / `ZoomExtents(rect3D)` | `Viewport3DX.ZoomExtents(...)` (present in v3) |
| Attached `IJSAMObject` on `ModelVisual3D` (guid index, selection) | `SceneNode.Tag` / `Element3D.DataContext`-style tag; keep the `Guid → Element3D` dictionary |
| `Viewport3DHelper.FindHits` (rect select) | `Viewport3DX.FindHitsInFrustum(...)` / `FindHits(point)` |
| ray hit-test on mouse-move (#16, throttled) | `Viewport3DX.FindHits(point)` → octree (throttle likely unneeded) |
| `Text3DObject` → meshed text | `BillboardTextModel3D` + `BillboardText3D` |
| `RectangularSelector` | reuse the WPF overlay adorner; only the hit query swaps |

Key new lifecycle concern: `EffectsManager` is `IDisposable` and owns the DX device — must be
created once and disposed on control unload; multiple `ViewportControl`s (one per tab) can share a
single static `EffectsManager`.

---

## What this touches (contained to `SAM.Geometry.UI.WPF`)

- `Controls/ViewportControl.xaml` + `.xaml.cs` — the viewport host, camera, selection, hover,
  context menu, zoom, the #16 guid index, the #11 `RefreshAppearances` fast-path.
- A new conversion layer paralleling `Convert/ToMedia3D/*` and `Create/Model3D.cs` →
  `Convert/ToElement3D/*` / `Create/Element3D.cs`, building **one merged mesh per material per
  object** (the #16 consolidation, done natively here).
- `Create/Material.cs`, `Convert/ToMedia3D/MeshGeometry3D.cs` — SharpDX equivalents (the #29
  `Freeze()` calls become no-ops / are dropped; SharpDX manages GPU resource sharing).
- `.csproj` — add `HelixToolkit.Wpf.SharpDX` 3.1.x (+ `HelixToolkit.SharpDX.Core`) and verify the
  native SharpDX dependencies copy for net8.0-windows.

Consumers in `AnalyticalWindow` (`ObjectHoovered` / `ObjectDoubleClicked` /
`ObjectContextMenuOpening` / `ObjectSelectionChanged`, and the `GetVisual3D`/`ContainsAny`/
`RefreshAppearances` API) **must keep their current signatures** — the port is behind the existing
public surface. Where they currently hand back `ModelVisual3D`, decide early whether to (a) keep
returning a `Visual3D`-shaped abstraction, or (b) widen the API to `Element3D` (touches
`AnalyticalWindow`). Prefer (a): adapt internally, keep the window untouched.

---

## Phased plan (each phase builds + runs on the real model; re-measure with `SAM_UI_PERFORMANCE_LOG`)

**Phase A — stand up the device (de-risk).** Add the package. Behind a new flag
`SAM_UI_VIEWPORT_SHARPDX` (off by default), host a `Viewport3DX` alongside the Helix one. Render a
trivial scene; confirm the DX11 device initializes and disposes cleanly on both TFMs, on the target
machines (integrated GPUs, RDP/VM — SharpDX needs a real or WARP device). **Gate:** no device/airspace
issues, clean tab open/close/dispose.

> **Phase A status: done** (PR #30). The trivial-scene spike validated on a dev machine: device
> created once (~220 ms) and shared by 10 viewports, airspace composition over live WPF content OK,
> tab open/close/switch/duplicate clean (a `ZoomExtentsWhenLoaded` zero-size race in duplicated
> tabs was found and fixed - never auto-zoom an unloaded viewport). The DX11 device lives in a
> single process-wide `DefaultEffectsManager`, created lazily (timed as
> `ViewportControl.SharpDX.CreateDevice`) and disposed on dispatcher shutdown — deliberately
> **not** per control, because WPF unloads tab content on every tab switch. Still outstanding
> (now testable with the real scene, below): integrated GPU, RDP/VM.
> The spike file was replaced by the Phase B `Controls/SharpDXViewportControl.cs`.

**Phase B — conversion + scene build.** Implement `Convert.ToElement3D(GeometryObjectModel)` /
`Create.Element3D(...)`, merging per object by material into one `MeshGeometryModel3D` (+ one
`LineGeometryModel3D` for edges). Rebuild the `Guid → Element3D` index (mirror `BuildVisual3DIndex`).
Instrument `ViewportControl.ToElement3D` for direct comparison against `ToMedia3D`. **Gate:** full
build well under 1 s on the 625-space model; visual parity with the Helix scene.

> **Phase B status: implemented; build-time measured (gate met, warm case). Visual parity + hardware matrix still open (#31).**
>
> **Measured (625-space model, `SAM_UI_PERFORMANCE_LOG`):** warm rebuild (space shells cached)
> `ToElement3D` ~110–280 ms vs Helix `ToMedia3D` ~2300 ms — **~15–20×**; full warm 3D pipeline
> ~340 ms, well under the 1 s gate. Cold/heavy first-build ~2–2.5 s, where `ToElement3D` tracks an
> expensive `GeometryObjectModel` build — that cost is **upstream of the renderer** (per-face
> `Spatial.Create.Mesh3D` triangulation, paid by both `ToMedia3D` and `ToElement3D`, uncached) and is
> tracked as a separate shared follow-up (#33: cache triangulated `Mesh3D` / background generation),
> **not** a port blocker. Build-time gate: **GO** (warm/steady-state).
> With `SAM_UI_VIEWPORT_SHARPDX` set, the 3D view renders full-size through
> `Controls/SharpDXViewportControl.cs`; the Helix viewport stays hidden and empty (same pattern as
> the 2D floor plan flag). `Convert.ToElement3Ds` walks the model exactly like `Create.Model3D`
> but merges per object by material via `Classes/SharpDXSceneBuilder.cs`: one `MeshGeometryModel3D`
> per fill color (CullMode.None replaces the doubled triangles), one `LineGeometryModel3D` per
> curve color, one `BillboardTextModel3D` per text color (world-sized billboards — scale to be
> tuned on first visual check). Scene build is timed as `ViewportControl.ToElement3D` next to the
> Helix `ViewportControl.ToMedia3D` for the A/B. Guid → `Element3D` index in place; view-settings
> camera applied on first load, user camera preserved on reloads.
> **Phase B limitations (by design):** hover/selection/context menu and "zoom selected" are
> no-ops in the SharpDX 3D view until Phase C (hover/selection since landed — see the Phase C
> status below); orthographic-3D camera and view chrome are Phase D.

**Phase C — interaction.** Port hover (octree `FindHits`), single/rectangle selection, highlight
(`HighlightAction` → swap material/overlay), the #11 `RefreshAppearances` fast-path (recolor the
affected object's `MeshGeometryModel3D.Material`), and context-menu plumbing. **Gate:** hover/select
< 50 ms; rectangle select parity.

> **Phase C status:** the recolor fast-path (#32 item 1), hover (item 2) and selection (item 3) are
> in. Hover picks via `FindHits` over per-geometry static octrees (built in `SharpDXSceneBuilder`),
> unthrottled — `ViewportControl.HoverHitTest` logs ≥ 25 ms occurrences with mode `SharpDX` for the
> gate check. Hover doubles the object's edge thickness (`HighlightAction` parity), selection swaps
> fill to RGB(125,125,255)/blue edges (`SelectionSurfaceAppearance` parity); single/Ctrl/double
> click, Escape and rectangle selection (projected-geometry tests with the classic Helix
> `FindHits(rect, mode)` Inside/Touch semantics, fed by the existing `RectangularSelector` overlay)
> all raise the existing `ViewportControl` events, with detached stub `ModelVisual3D`s as payloads
> (the `FloorPlan2DControl` interop pattern). Programmatic `Select`/`SelectedSAMObjects`/
> `GetVisual3D` route to the SharpDX scene. Lines and text billboards are not pickable (meshes
> define the pickable footprint; curve-only objects are still rectangle-selectable). Context-menu
> plumbing (item 4) is in (`ObjectContextMenuOpening` from a right-click hit). **All four #32 items
> implemented; the gate (hover/select < 50 ms, rectangle-select parity, recolor-without-rebuild) is
> still to be measured under #31.**

**Phase D — camera & chrome.** Perspective + orthographic-3D cameras, `ZoomExtents`, "Zoom Selected",
view-cube/coordinate system, `SetCamera`/`GetCamera` round-trip, view-settings application. Confirm
the legacy Helix orthographic **2D** path can be deleted (floor plans already on `FloorPlan2DControl`).
**Gate:** camera save/restore parity; no 2D regressions.

> **Phase D status: done (issue #37).** Done: `ZoomExtents` and "Zoom Selected" route to the
> SharpDX view (Zoom Selected re-aims the camera at the selection bounds centre rather than only
> dollying along the old look direction); `ZoomExtents` re-levels the camera up to world Z so
> isolating + rotating a small object no longer leaves the view tilted; selection-aware rotation
> pivot (FixedRotationPoint on the selection centroid, cursor-point fallback); view cube +
> coordinate system enabled.
>
> **Orthographic-3D camera (issue #37):** `SharpDXViewportControl.ToggleProjection()` /
> `Orthographic` swaps the viewport camera between `PerspectiveCamera` and `OrthographicCamera`,
> bound to **Ctrl+Shift+O** (parity with the Helix `OrthographicToggleGesture` default). The swap
> carries Position/LookDirection/UpDirection and the clip planes across, derives the orthographic
> `Width` from the perspective field of view and the look-at distance so the on-screen scale is
> continuous, and restores the remembered field of view on the way back. `FrameCamera`
> (ZoomExtents / Zoom Selected) fits the bounding sphere by `Width` when orthographic.
>
> **View-settings application + `Get`/`SetCamera` round-trip:** the view-settings camera is applied
> to the SharpDX camera on view activation (`ViewportControl.Load` -> `SharpDXViewportControl.Load`,
> first non-empty load) and saved back via `ViewportControl.Camera` (-> `GetCamera`); `SetCamera`/
> `GetCamera` round-trip Position/look/up identically to the Helix path, including the same
> ±world-Z look-direction pole nudge. Projection is **not** persisted in the `Camera`/`ViewSettings`
> model - the Helix 3D path doesn't persist it either, so a reloaded view opens in perspective
> (parity, not a regression).
>
> **Legacy Helix orthographic 2D path - confirmed deletable (removal deferred to Phase E):** floor
> plans render on `FloorPlan2DControl` by default (`SAM_UI_FLOORPLAN_2D` on). The Helix ortho-2D
> path (`ViewportControl.UpdateMode` else-branch, `UpdateClipPlanes2D`/`sceneZMin`/`sceneZMax`/
> `helixViewport3D_CameraChanged`) is reached only with `SAM_UI_FLOORPLAN_2D=0`; nothing else
> depends on it. It is **not** removed here because the surrounding Helix mouse/rect-select handlers
> still serve the Helix **3D** path (active when `SAM_UI_VIEWPORT_SHARPDX` is off), whose removal is
> Phase E - the two Helix removals land together there.

**Phase E — flip & remove.** Make SharpDX the default for 3D, keep `SAM_UI_VIEWPORT_SHARPDX=0` as the
escape hatch for one release, then remove the `HelixToolkit.Wpf` 3D code path and the old
`ToMedia3D`/`Create.Model3D` 3D conversion. **Gate:** sign-off on real models; PR.

> **Phase E status: flip DONE (#53, this PR).** `SharpDXViewportControl.ResolveEnabled()` now defaults
> ON — the SharpDX viewport is the default 3D renderer; `SAM_UI_VIEWPORT_SHARPDX=0` is the escape
> hatch that falls back to the intact legacy Helix 3D path for one release. Gate evidence: the Phase B
> A/B above (`ToElement3D` ~15–20× faster than `ToMedia3D`, full warm 3D well under 1 s) plus
> functional sign-off on a large (10k-space) model. **Removal deferred:** the legacy Helix 3D + Helix
> ortho-2D paths and the `ToMedia3D`/`Create.Model3D` conversion layer are removed in a follow-up PR
> after the flipped default proves stable (so the `=0` escape hatch stays usable until then).
>
> **B (Phase D leftovers) — verified, no change:** saved-camera application is already at Helix parity
> — both renderers apply `ViewSettings.Camera` once on first scene load (Helix `helixViewport3D_Loaded`;
> SharpDX `Load` `wasEmpty` branch) and preserve the live camera afterward; the explicit "Load camera"
> action round-trips position/look/up through `ViewportControl.Camera` → `SharpDXViewportControl.SetCamera`.
> Projection is intentionally not persisted by either path, so SharpDX matches Helix there too.

---

## Risks / open questions

- **GPU availability.** SharpDX needs DX11 (or WARP fallback). Verify on the lowest-spec target
  hardware and over RDP/virtualized sessions early (Phase A) — this is the main go/no-go risk.
- **Deployment size & native deps.** SharpDX ships native DLLs; confirm they flow through the
  installer for both TFMs.
- **Selection granularity.** Merging to one mesh per object drops per-face sub-selection (same
  trade as #16 PR 2; UI doesn't expose it). Confirm no workflow relies on it.
- **`Text3DObject` semantics.** Billboard text always faces the camera (size in screen space) vs.
  today's plane-anchored meshed text — confirm acceptable for 3D-view labels.
- **Packaging.** Confirm `HelixToolkit.Wpf.SharpDX` 3.1.x packages cleanly for net8.0-windows (the
  installer target).

---

## Relationship to other issues

- **Supersedes** #16 PR 2 (per-object mesh-merge on Helix WPF) — do not ship that separately;
  the merge lands inside Phase B.
- **Keeps** #16 PR 1 (#29) freezing/index/hover-throttle as the interim Helix improvement until the
  port lands.
- **Closes** the 3D half of the #18 decision gate; the floor-plan half is closed by the
  `SAM_UI_FLOORPLAN_2D` default flip.
