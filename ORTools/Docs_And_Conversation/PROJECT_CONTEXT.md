# GOAT BIM Architectural Suite (Revit 2026 / 2027 — .NET 10.0 WPF)
**Lead Developer & Owner:** Eng. Abdelaziz Osman (**أبو ياسين**)

---

## 1. Project Overview & Identity
- **Brand Name:** `GOAT BIM` (Upgraded & rebranded from `OR Tools`)
- **Target Platforms:** Autodesk Revit 2026 & 2027 (`.NET 10.0-windows`, `C#`, `WPF`)
- **Theme Palette:** Dark Navy (`#0F172A`), Emerald (`#10B981`), Sky (`#38BDF8`)
- **Execution Architecture:**
  - All 15 tools run as **Modeless WPF Windows** via `IExternalEventHandler` (`ExternalEvent.Create(...)`).
  - All Revit model modifications are wrapped in **`TransactionGroup` + `Assimilate()` (`One Ctrl+Z`)**.
  - Zero external NuGet dependencies: uses the built-in self-contained OpenXML `.xlsx` engine (`GoatExcelEngine.cs`) and JSON settings manager (`GoatSettingsManager.cs` in `%AppData%\GOATBIM\`).
  - **Note:** The `About` button/window was intentionally removed per explicit owner instruction.

---

## 2. Ribbon Architecture (6 Panels — 15 Tools)

### Panel 1: `GOAT Setup`
1. **`LEVELS`** (`OrLevelWindow.xaml`) — Batch level creation, elevation editing, and automatic Floor Plan / Ceiling Plan view generation.
2. **`GRIDS`** (`OrGridWindow.xaml`) — Orthogonal X/Y grids, Radial grids, Rotated grids, bubble toggles, and auto-dimensioning.

### Panel 2: `GOAT Data` (DiRoots + CTC BIM Project Suite Inspired)
3. **`PARAMETER`** (`OrParameterWindow.xaml`, `OrParameterExternalEventHandler.cs`) — Bulk Instance/Type parameter search, case-sensitive value filter, Select in Revit, Prefix/Suffix, Bulk Overwrite/Clear, **🎨 Color Splash Active View**, **↺ Reset Colors**, and **🏠 Inject Host Room & Level**.
4. **`SHEETLINK`** (`OrSheetLinkWindow.xaml`, `OrSheetLinkViewModel.cs`, `OrSheetLinkExternalEventHandler.cs`) — Live bidirectional spreadsheet editor + Native `.xlsx` Export/Import with `(Instances)` and `(Types)` worksheets + `➕ Add New Sheet / Room Row` (`NEW` element creation).
5. **`RENUMBER`** (`GoatRenumberWindow.xaml`, `GoatRenumberWindow.xaml.cs`) — Smart spatial renumbering (`X then Y`, `Y then X`), Door-by-Room (`101A, 101B` or `101-1, 101-2`), Interactive Click-in-View, conflict-free two-pass `Auto-Swap`, and **direct editing of Room / Element Names (`ElementTitle`)** in the preview grid.
6. **`BOQ`** (`GoatBoqWindow.xaml`, `GoatBoqWindow.xaml.cs`) — Multi-category Quantity Takeoff (`Count`, `Length m`, `Area m²`, `Volume m³`) with unit pricing and native `.xlsx` BOQ export with Excel formulas.

### Panel 3: `GOAT Finishes`
7. **`FINISHES`** (`GoatFinishesWindow.xaml`, `GoatFinishesExternalEventHandler.cs`) — 6-Tab Room Finishes suite:
   - Tab 1: Wall Cladding (`JoinGeometry` with host walls to cut doors/windows automatically, column wrapping).
   - Tab 2: Skirting / Baseboards (Wall Type or Wall Sweep, auto-stops at door openings).
   - Tab 3: Floor Finishes (door threshold extension).
   - Tab 4: Ceilings (automatic replacement of any old ceiling in the room without duplication).
   - Tab 5: Room Data sync (read/write Room Finish parameters).
   - Tab 6: Detailed QTO `.xlsx` export.
8. **`ARCH`** (`GoatArchWindow.xaml`, `GoatArchExternalEventHandler.cs`) — 7 Parametric Architectural Arch Profiles (`Semicircular`, `Segmental`, `Pointed Gothic`, `Horseshoe`, `Tudor`, `Elliptical`, `Flat Jack`) with automatic host wall opening cut + Parametric Wardrobe & Joinery Builder.

### Panel 4: `GOAT Ceiling`
9. **`CEILING`** (`GoatCeilingWindow.xaml`, `GoatCeilingExternalEventHandler.cs`) — Replaces existing ceiling or creates missing ceiling in selected rooms, computes `600×600` / `600×1200` spatial grid array, places Lighting Fixtures, Air Terminals (Diffusers), and Sprinklers (`Face-Hosted`, `Ceiling-Hosted`, or `Level-Based` fallback), avoids structural columns, and replaces old room fixtures cleanly without duplication.

### Panel 5: `GOAT Documentation`
10. **`D VIEW`** (`OrOpeningWindow.xaml`) — Door Legend / Elevation & Plan table documentation.
11. **`W VIEW`** (`OrOpeningWindow.xaml`) — Window Legend / Elevation & Plan table documentation.
12. **`ROOM`** (`GoatRoomWindow.xaml`, `GoatRoomWindow.xaml.cs`) — Glyph-style Automated Room Documentation Bundle:
    - Direct editing of **Room Number (`No. ✎`)** and **Room Name (`Room Name ✎`)** in the room list.
    - Per-room **Fill Color Picker (`Fill Color`)** + **`🎨 Auto-Color By Name`** + **`💾 Apply Names, Numbers & Fill Colors to View Now`** (applies both `GOAT Room Color Scheme` and `OverrideGraphicSettings` in the view).
    - Generates Enlarged Floor Plan, Reflected Ceiling Plan (RCP), 4-Way or Wall-Parallel Interior Elevations, 3D Isometric Section Box, Auto-Dimensions, Auto-Tags, and dedicated packed Sheet per room.
13. **`DIM`** (`GoatDimWindow.xaml`, `GoatDocEngines.cs`) — Automated Interior Room Dimensions (with configurable **`Room Offset from Wall (mm)`** so dimensions never cross the center Room Tag, plus secondary door/window opening chains), Exterior Building Perimeter Chains, Elevation Level Chains, and optional **`Show 'mm' Unit Suffix Next to Dimension Numbers`**.
14. **`ANNOTATE`** (`GoatAnnotateWindow.xaml`, `GoatDocEngines.cs`) — Multi-category smart tagging with Spiral Anti-Collision Engine.

### Panel 6: `GOAT Delivery`
15. **`ALIGNER`** (`GoatAlignerWindow.xaml`, `GoatDocEngines.cs`) — Auto-Pack Active/Selected Sheets, Cross-Sheet Viewport/Legend/Schedule Position Aligner, and Duplicate Sheets with Views.
16. **`SHEETS`** (`OrSheetsWindow.xaml`) — Batch Exporter (`PDF`, `DWG`, `IFC`, `NWC`, `Images`) with `ISO 19650` naming builder and `DocRegister` `.xlsx` transmittal sheet.

---

## 3. Build & Installation Commands
- **One-Click Installer:** Run `Install.bat` (or `Install.ps1`).
- **Manual Build Command:**
  ```powershell
  dotnet build "Files\Source\ORTools.csproj" -c Release -o "Files\BuildOutput"
  ```
