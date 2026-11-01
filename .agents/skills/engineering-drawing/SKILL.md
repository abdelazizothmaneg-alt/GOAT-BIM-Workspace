---
name: engineering-drawing
description: >-
  Generates and specifies standardized 2D engineering and technical shop drawings compliant with ISO 128 and ASME Y14.
  Covers orthographic projections, section cuts, Geometric Dimensioning and Tolerancing (GD&T), title blocks, and fabrication detailing.
---

# Engineering & Technical Shop Drawing Standards

Authoritative drafting standard for producing construction-ready, fabrication-level engineering drawings and shop details.

## 1. Orthographic Projection Systems
* **First-Angle Projection (Standard in Europe, Middle East, ISO):**
  * Top View placed *below* the Front View.
  * Right Side View placed to the *left* of the Front View.
* **Third-Angle Projection (Standard in North America, ANSI/ASME):**
  * Top View placed *above* the Front View.
  * Right Side View placed to the *right* of the Front View.
* Always indicate the projection method symbol in the title block.

---

## 2. Sectioning & Cut-Plane Standards
* **Section Line Conventions:**
  * Heavy dashed-dotted cutting plane line ($0.7\text{mm}$) with directional arrowheads indicating line of sight.
  * Identify cuts sequentially: `SECTION A-A`, `SECTION B-B`.
* **Section Types:**
  * **Full Section:** Cut passes completely through the object along a single plane.
  * **Offset / Stepped Section:** Cutting plane bends $90^\circ$ to capture features not along a single straight line.
  * **Revolved / Removed Section:** Section profile rotated $90^\circ$ directly on the member (ideal for structural beams, mullions, and handrails).
* **Cross-Hatching Standards:**
  * Thin continuous lines ($0.25\text{mm}$) inclined at $45^\circ$ to the main axis.
  * Differentiate adjacent parts by alternating hatch angles ($+45^\circ$ vs. $-45^\circ$) or pitch spacing.

---

## 3. Dimensioning & Tolerancing (GD&T)
* **Dimensioning Rules:**
  * Never cross dimension lines with extension lines.
  * Place shorter dimensions closer to the object, longer overall dimensions farthest out.
  * Dimensions must be placed outside the view whenever possible.
* **Geometric Dimensioning and Tolerancing (GD&T):**
  * Feature Control Frames: `[ Geometric Characteristic | Tolerance Value | Datum References ]`
  * Example: `[ ⌖ | Ø 0.2 Ⓜ | A | B | C ]` (Position tolerance $0.2\text{mm}$ at Maximum Material Condition relative to Datums A, B, C).

---

## 4. Sheet Sizes & Layout Margins (ISO 216 / ASME)
* **Standard Trimmed Sizes:**
  * **A0:** $841 \times 1189\text{ mm}$ (Master plans, full building sections)
  * **A1:** $594 \times 841\text{ mm}$ (Standard architectural & structural construction sets)
  * **A2:** $420 \times 594\text{ mm}$ (Detailed shop drawings, mechanical schematics)
  * **A3:** $297 \times 420\text{ mm}$ (Submittals, review prints, calculation sets)
* **Margins:** Left binding edge $\ge 20\text{mm}$; top, bottom, and right margins $\ge 10\text{mm}$.
