# Space Assumptions PDF in SAM_UI (SAM Documentation Framework, PR3)

Status: merged (SAM_UI#121, `7e7de033`) and shipped by SAM_Deploy#51 (`8e6740af`). Phase 1 is complete; see
SAM `documentation/Reporting-PDF.md` › *Phase 1 status*.

SAM_UI's one-click entry to the Phase 1 **Space Assumptions** report. SAM_UI only orchestrates. The data, units,
placeholders, document structure and layout all come from SAM:
- `SAM.Analytical.Reporting` (`Create.DocumentContext`, `Create.SpaceAssumptions`);
- `SAM.Core.Reporting` (`DocumentOptions`, `QuantityFormatter`);
- `SAM.Core.Reporting.Pdf` (`PdfRenderer`).

Renderer architecture: see `SAM/documentation/Reporting-PDF.md`. The legacy Excel-based **Print RDS** is unchanged
and still available.

## Where the command is
- **Ribbon:** Edit › **Reports** › **Space Assumptions PDF**. This is a separate group to the right of Analytical
  Model, which still holds the legacy *Print RDS*. It acts on the Space selected in the active 3D/2D view.
  - Enabled whenever a model is open.
  - Nothing selected: "Select one Space, then choose Space Assumptions PDF."
  - Several Spaces selected: "N Spaces are selected. The Space Assumptions PDF is created for one Space at a time:
    select a single Space."
- **3D/2D view context menu** (right-click a Space): **Space Assumptions PDF**.
- **Model tree context menu** (right-click a Space under Spaces or a Zone): **Space Assumptions PDF**.
- In both context menus, when more than one Space is selected or highlighted, the item is shown **disabled** with the
  tooltip "…one Space at a time: select a single Space." The first Space is never picked silently.

Code (shared with the Phase-2 Space Design Load Summary PDF since PR2D; see
`Reporting-SpaceDesignLoadSummaryPdf.md`):
- `SpaceReportPdf.SpaceAssumptions` (the report and its SAM entry point, `Create.SpaceAssumptions`);
- `Modify.CreateSpaceReportPdf` (UI flow);
- `Modify.WriteSpaceReportPdf` (the testable seam);
- `Query.SpaceReportPdfSpace` and `Query.SpaceReportPdfFileName`;
- `Create.MenuItem_SpaceReportPdf`;
- `SpaceReportPdfResult`.

## Workflow
1. Select a Space, then choose the command.
2. A Save dialog opens:
   - it starts in the model's folder;
   - the default name is `<Space name> - Space Assumptions.pdf`;
   - it offers the PDF filter only;
   - Windows asks for confirmation before overwriting.
   - Cancel does nothing and shows no message.
3. The PDF is written, with a wait cursor. The command then asks: "Space Assumptions PDF saved: <path> — Open it now?"
   Yes opens the PDF in the default viewer; No does nothing.

## Selection rules
- The Space is re-read by Guid from the current model. A selection that is stale after an edit reports the model's
  current Space. A Space that was removed is refused: "…no longer in the model."
- The same Space selected twice counts as one Space.

## Units
SAM_UI has no unit-system preference (only the Mollier chart's own default), and PR3 adds none. The command uses the
reporting framework's default, **SI**, with air flow in **L/s**. Every unit is formatted upstream by `QuantityFormatter`.
`WriteSpaceReportPdf` takes a `UnitStyle`, so a later preference or IP option is a one-line change. That path is
tested for IP: no SI unit appears.

## File name
- `<Space name> - Space Assumptions.pdf`.
- Characters Windows forbids, and control characters, become `_`.
- Leading and trailing spaces, and trailing dots, are removed.
- Reserved device names get a `_` prefix (for example `_CON`).
- The name is capped at 150 characters.
- With no usable name the fallback is `Space <guid> - Space Assumptions.pdf`, the same subject the report prints.
- No Space number is parsed out of the name.

## Failures
Expected missing engineering data is **not** an error. The report prints `—`, `n/a`, `not set` and notices, and the
reporting framework's diagnostics are kept in `SpaceReportPdfResult.Notes`.

A software failure stops the command and shows an error that names the stage:
- **Document:** the reporting library threw.
- **Rendering:** the renderer, fonts, a resource or the font resolver failed, or a PDFsharp assembly is missing.
- **Output:** the path is invalid, access is denied, or the file is locked in a viewer. The message asks the user to
  choose another folder or close the file.

In every case:
- the exception is kept on the result and written with `Trace.TraceError`;
- nothing is swallowed, and the app does not crash (SAM_UI has no global exception handler);
- the PDF is rendered in memory, written to `<path>.tmp`, then moved over the destination, so a failure never leaves
  a partial file or damages an existing PDF.

## Packaging
- `SAM.Analytical.UI.WPF.csproj` references `SAM.Analytical.Reporting`, `SAM.Core.Reporting`,
  `SAM.Core.Reporting.Pdf` and `SAM.Units` from `SAM\build`.
- `SAM.Core.Reporting.Pdf` is a netstandard2.0 library, and a library build does not copy its NuGet dependencies. So
  SAM_UI has **`PackageReference PDFsharp-MigraDoc 6.2.0`**, the same version SAM builds against and the PDFsharp that
  SAM_Revit uses. It puts these files into `SAM_UI\build`, and from there, through the `SAM Analytical` post-build
  xcopy, into `%APPDATA%\SAM`, the installer payload:
  - PdfSharp*.dll and MigraDoc*.dll (6.2.0.0, the net8.0 build);
  - Microsoft.Extensions.Logging / .Abstractions / DependencyInjection(.Abstractions) / Options / Primitives (8.0);
  - System.Security.Cryptography.Pkcs (8.0).
- Licences are copied to `licenses\NotoSans\OFL.txt` (taken from SAM) and `licenses\PDFsharp-MigraDoc\LICENSE.txt`
  (`files\licenses`).
- Noto Sans is embedded in `SAM.Core.Reporting.Pdf.dll`, so no font files are shipped.
- SAM_Revit's PDFsharp goes to `%APPDATA%\SAM\Revit 20xx`, a separate folder, so it does not collide.

## Font resolver
PDFsharp allows one global font resolver per process. In the SAM_UI process nothing else uses PDFsharp:
- Mollier PDF export uses OxyPlot.SkiaSharp;
- SAM_Revit's PDFsharp code draws images only and installs no resolver.

`NotoSansFontResolver.Register()` is idempotent. It is called by `PdfRenderer` on every render, never by SAM_UI.
The tests check that after a render the process resolver is `NotoSansFontResolver.Instance`, and that a second render
reuses it. A resolver conflict would surface as a **Rendering** failure with the renderer's own message.

## Threading
Generation is synchronous on the UI thread, behind a wait cursor. It was measured from the installed payload:
- the first PDF in a process takes 0.2–0.5 s, because PDFsharp and the fonts load;
- each later PDF takes 16–33 ms, including in a model with 1,608 Spaces.

A background task or progress window would add complexity with no benefit at that duration. The model is not
modified while the document is built.

## Branding
The renderer's built-in SAM mark is used. SAM_UI has no company-logo resource, so none is passed
(`DocumentStyle.CompanyLogoPng` stays null). No Tas or EDSL branding is used.

## Evidence
See `documentation/evidence/space-assumptions-pdf/ACCEPTANCE-2026-09-26.md`.
