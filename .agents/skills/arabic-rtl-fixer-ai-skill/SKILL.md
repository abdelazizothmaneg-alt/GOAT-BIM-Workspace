---
name: arabic-rtl-fixer-ai-skill
description: >-
  Detects, repairs, and reformats corrupted or reversed Arabic text, bi-directional (BiDi) layout bugs,
  misplaced punctuation, inverted brackets, and mixed Arabic-English technical terminology in CAD, Revit, Word, and PDFs.
---

# Arabic RTL & BiDi Text Fixer

Specialized text engineering skill to repair reversed, isolated, or scrambled Arabic typography in engineering drawings, BIM parameters, and documents.

## 1. Core BiDi & Arabic Text Diagnostic Rules
* **Reversed / Disconnected Letters:**
  * Detect reversed character streams (e.g., `ة س د ن ه م` instead of `هندسة`).
  * Re-shape disconnected glyphs into correct positional ligatures (Initial, Medial, Final, Isolated).
* **Punctuation & Bracket Inversion:**
  * Fix misplaced brackets, colons, and periods that flip to the beginning of lines when mixed with Latin technical terms.
  * Correct: `(LOD 300) نسبة الإنجاز: 85%` vs. Corrupted: `%85 :LOD 300( نسبة الإنجاز`.
* **Technical Numbers & Units Formatting:**
  * Preserve correct order of engineering numbers and units:
    * Lengths: `12.50 م` or `12.50 m`
    * Areas: `350 م²` or `350 m²`
    * Angles & Dimensions: `90°` or `Ø 16 mm`

## 2. Platform-Specific Fixes

### A. AutoCAD & DWG Text Encoding
* When reading or writing CAD text entities (`TEXT`, `MTEXT`):
  * Check font encoding (Arabic SHX fonts like `x-arab`, `sos`, `ahmed` vs. Windows TrueType Unicode fonts like `Arial`, `Tahoma`, `Cairo`).
  * Translate legacy encoded Arabic character codes back into clean UTF-8 Unicode strings.

### B. Revit Parameter & Schedule Ingestion
* When injecting Arabic values into Revit Shared Parameters (`Room_Name`, `Department`, `Finish_Notes`):
  * Enforce standard UTF-8 string encoding to prevent question marks (`????`) or blank rectangles in schedules and tags.

### C. PDF & Print Export
* Ensure explicit Unicode directional control marks (`\u200E` Left-to-Right Mark / `\u200F` Right-to-Left Mark) around technical codes, file paths, and dates to guarantee flawless PDF rendering across viewers.
