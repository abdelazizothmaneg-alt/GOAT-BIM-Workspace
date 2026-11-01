---
description: Core context, architecture, owner preferences, and full conversation reference for the GOAT BIM Revit 2026/2027 (.NET 10 WPF) Add-in Suite.
globs: "**/*"
alwaysApply: true
---

# GOAT BIM Project Memory & Rules (Auto-Loaded by Antigravity)

- **Owner & Lead Engineer:** Eng. Abdelaziz Osman (**أبو ياسين** — always address him as **أبو ياسين** in Egyptian Arabic when communicating in Arabic).
- **Project Root:** `D:\_Course\GOAT`
- **Source Code Root:** `D:\_Course\GOAT\Files\Source` (`ORTools.csproj`)
- **Full Previous Conversation Reference:**
  - Conversation ID: `22df1f0e-29bf-4651-ae1f-4aab0e92268a`
  - Full Transcript in Project: `D:\_Course\GOAT\Full_Conversation_History.md`
  - Master Plan in Project: `D:\_Course\GOAT\GOAT_BIM_Master_Plan.md`
  - Architecture & Tools Summary: `D:\_Course\GOAT\PROJECT_CONTEXT.md`

## Core Architecture & Constraints
1. **Brand:** `GOAT BIM` (`#0F172A` Dark Navy, `#10B981` Emerald, `#38BDF8` Sky). No `About` button/window.
2. **6 Ribbon Panels, 15 Tools:**
   - `GOAT Setup`: `LEVELS`, `GRIDS`
   - `GOAT Data`: `PARAMETER`, `SHEETLINK`, `RENUMBER`, `BOQ`
   - `GOAT Finishes`: `FINISHES`, `ARCH`
   - `GOAT Ceiling`: `CEILING`
   - `GOAT Documentation`: `D VIEW`, `W VIEW`, `ROOM`, `DIM`, `ANNOTATE`
   - `GOAT Delivery`: `ALIGNER`, `SHEETS`
3. **Modeless & Undo-Friendly:** Every tool runs via `ExternalEvent` (`IExternalEventHandler`) and wraps model changes in `TransactionGroup` + `Assimilate()` (`1x Ctrl+Z`).
4. **Smart Replacement:** Creating ceilings, finishes, or ceiling fixtures in a room must replace/delete any old ceiling or finish in that room rather than duplicating.
