---
name: autocad-dwg-redraw
description: >-
  Reconstructs, cleans, and redraws messy or legacy AutoCAD DWG drawings, and converts technical PDF blueprints
  into clean, standard architectural geometry (closed Polylines, standardized layers, blocks) using automated AutoLISP/Script generation.
---

# AutoCAD DWG Redraw & Reconstruction Engine

Automated CAD geometry reconstruction engine designed to clean corrupted, fragmented, or legacy drawings and vectorize PDF blueprints into standardized AutoCAD assets.

## 1. Source DWG Cleansing & Geometry Redraw
When processing messy or legacy DWG floor plans:
1. **Geometric Overkill & Deduplication:**
   * Remove identical overlapping line segments and coincident collinear lines.
   * Eliminate zero-length lines and orphaned points.
2. **Polyline Boundary Healing (Gap Closing):**
   * Detect micro-gaps ($< 5\text{mm}$) between wall segments and automatically heal vertices to form water-tight closed polylines (`LWPOLYLINE`).
   * Group parallel wall faces and compute wall thickness for automated layer assignment.
3. **Layer Standardization & Remapping:**
   * Remap arbitrary consultant layers into AIA CAD standards (`A-WALL-FULL`, `A-DOOR`, `A-GLAZ`, `S-COLS`).

---

## 2. PDF-to-DWG Vector Reconstruction
* **Vector Path Translation:**
  * Parse vector PDF linework, curves, and text strings into native CAD coordinates.
  * Separate thick architectural cut-lines (walls) from thin surface finish patterns.
* **Block Substitution:**
  * Detect repeated door swing arcs and window mullion groupings, replacing exploded line clusters with clean, parametric AutoCAD block definitions.

---

## 3. Automated Script Generation (AutoLISP / SCR)
Generate executable AutoLISP `.lsp` or AutoCAD script `.scr` routines to perform batch redraws inside AutoCAD headlessly or interactively:
```lisp
;; AutoLISP Clean & Redraw Routine
(defun c:CleanAndRedrawWalls (/ ss i ent lay)
  (vl-load-com)
  (setq ss (ssget "X" '((0 . "LINE,LWPOLYLINE") (8 . "A-WALL*"))))
  (if ss
    (progn
      ;; Perform PEDIT join with 5.0mm fuzz distance
      (command "_.pedit" "_M" ss "" "_J" 5.0 "")
      (princ "\n[Success] Wall polylines joined and healed.")
    )
  )
  (princ)
)
```
