# CLAUDE.md — Master Architectural & BIM Context for Claude

> **Welcome Claude!** This workspace is shared live with **Antigravity (Gemini)** and is owned by **Eng. Abdelaziz Osman (أبو ياسين)**, Senior Architectural & BIM Automation Developer.
> You and Antigravity operate on the **exact same physical filesystem**. Any file you create or edit here is immediately visible to Antigravity, and vice-versa.

---

## 👤 User Profile & Architectural Domain
- **Developer & Owner:** Eng. Abdelaziz Osman (**أبو ياسين** / `faqih`).
- **Specialization:** Senior Architectural Engineer, BIM Automation Specialist, Computational BIM Developer.
- **Key Domains:** Autodesk Revit (.NET 8.0 & 10.0 C# / WPF), AutoCAD .NET / AutoLISP, 3ds Max Interop (MAXScript / Python), pyRevit, Computational Geometry, Saudi Building Code (SBC), and GCC Regional Architecture.
- **Language & Formatting:** High-level architectural Arabic (Modern Standard / Fusha) with strict Right-to-Left (RTL) formatting and English technical terminology.

---

## 🏗️ Active Projects Directory Map
All active projects are located in this directory (`C:\Users\faqih\.gemini\antigravity\scratch\`):

| Project Folder | Core Stack | Description |
| :--- | :--- | :--- |
| **`GOAT_BIM_Suite_2026_2027`** / **`ORTools`** | C# .NET 10.0 WPF, Revit API 2026/2027 | Flagship 15-tool architectural Revit add-in suite (Levels, Grids, Parameter, SheetLink, Renumber, BOQ, Finishes, Arch, Ceiling, Openings, Room, Dim, Annotate, Aligner, Sheets). Zero NuGet dependencies. |
| **`MaxToRevitConverter`** | MAXScript, C# Revit 2026 Bridge Add-in | Automated bridge converting 3ds Max models to native Revit `.rfa` families and `.rvt` project models with automatic alignment, material mapping, and batch processing. |
| **`CAD-AI`** | Python 3, `pyautocad`, Mistral API | SET Architecture (Standards, Engine, Tools) for deterministic AutoCAD automation and AI drawing inspection. |
| **`SbcBaladyChecker`** | C# .NET 8.0, Revit API | Automated code compliance validator checking architectural models against the Saudi Building Code (SBC 201/801) and Balady municipal zoning rules. |
| **`goat-bim-web`** | Next.js, React, Three.js, Tailwind CSS | Web command center and interactive 3D WebGL viewer for GOAT BIM Suite with Swiss/Bauhaus dark luxury aesthetic. |
| **`zan_yanbu_proposal`** | Word / HTML / PowerShell | Full commercial, technical, and 32-week program proposals for the ZAN Yanbu architectural project. |
| **`school_quotation`** | Word / HTML | Comprehensive architectural design quotation and BOQ for Sheikh Essam School. |
| **`abdelaziz_osman_cv`** | LaTeX, HTML, Word, Markdown | Executive CV and portfolio summary for Eng. Abdelaziz Osman in multiple formats. |
| **`Revit_Door_Family`** | Python, DXF, Wavefront OBJ | Parametric door families and mesh generators. |
| **`.agents/skills/`** | Markdown / Scripts | Over 160 specialized AEC, BIM, 3ds Max, and computational automation skills. |

---

## ⚡ Technical Standards & Development Rules

### 1. Revit API Rules (Strictly Enforced)
- **Target Frameworks:** .NET 10.0 for Revit 2026/2027; .NET 8.0 for Revit 2025.
- **Transactions:** 
  - Never modify the Revit document outside an active, properly scoped `Transaction` or `TransactionGroup`.
  - In `GOAT BIM Suite`, wrap multi-step operations in `TransactionGroup.Start()` and `Assimilate()` so the user gets **One Ctrl+Z**.
  - Always implement proper error handling with `RollBack()` on failure.
  - Set failure handling options (`IFailuresPreprocessor`) to handle warnings gracefully without blocking Revit UI.
- **Threading & UI Context:**
  - Strictly adhere to Revit's single-threaded API requirement.
  - Modeless WPF windows **must** communicate with Revit through `IExternalEventHandler` and `ExternalEvent.Create()`. Never call `doc.Create` or modify parameters directly from WPF click handlers!
- **Filtering & Performance:**
  - Always optimize `FilteredElementCollector` queries using Quick Filters (`OfClass`, `OfCategory`, `WhereElementIsNotElementType`) before applying Slow Filters or LINQ in-memory filtering.

### 2. Arabic & Regional Architectural Standards
- All user-facing documentation, proposals, and schedules must support Arabic RTL formatting and layout conventions.
- When generating reports, quotation templates, or technical specifications, adhere to GCC / Middle East architectural codes (SBC, MOMRAH / Balady, Saudi Civil Defense).

### 3. Agent Interop & Synchronization Protocol (Parallel Safety)
- **Live Sync Channel (`LIVE_AGENT_SYNC.md`):**
  - **MANDATORY:** Before answering any prompt, inspect `LIVE_AGENT_SYNC.md` at the workspace root to see the latest actions, thoughts, and handoff notes from Antigravity.
  - Whenever you finish editing or testing code, post an update so Antigravity hears you:
    Run: `python AgyClaudeBridge/bridge.py post claude "وصف ما قمت به وما الخطوة القادمة"`
- **Git Version Control & Safety Net:** 
  - This repository is tracked by Git. Always check `git status` when beginning a session.
  - After completing tasks, either commit directly (`git commit -m "..."`) or ask the user to run `commit_session.bat`.
  - Never overwrite files blindly if there are uncommitted changes from Antigravity.
- **Task & Project Partitioning:**
  - If working concurrently with Antigravity, divide work by project subfolders (e.g., Claude works on `SbcBaladyChecker` or `CAD-AI`, while Antigravity works on `GOAT_BIM_Suite_2026_2027` or `goat-bim-web`) to avoid write collision.
- **Shared Filesystem:** Always write changes directly to this workspace. Do not store project code in temp folders.
- **Logging Changes:** In addition to the live bridge, update `PROJECTS_SHARED_CHANGELOG.md` for permanent milestone records.
- **Preserve Existing Integrity:** Never overwrite or delete existing documentation, docstrings, or architectural setups without explicit user approval.


@AGENTS.md
