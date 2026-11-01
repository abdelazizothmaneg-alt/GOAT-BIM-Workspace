---
name: arabic-presentations
description: >-
  Designs, formats, and validates Arabic-first and bilingual (Arabic-English) pitch decks, PowerPoint presentations,
  and executive slides, ensuring strict RTL alignment, typographic hierarchy, and visual balance.
---

# Arabic Presentations & Pitch Decks

Professional slide design and layout framework tailored for Arabic-language corporate proposals, architectural pitches, and stakeholder presentations.

## 1. Structural RTL Slide Layout Rules
* **Visual Reading Flow:**
  * Arabic reading eye flow travels from **Top-Right to Bottom-Left**.
  * Place primary titles, major KPI numbers, and focal logos anchored to the **Top-Right**.
  * Progressions, timelines, and Gantt charts flow right-to-left:
    ```text
    [المرحلة 1: المفهوم] ──> [المرحلة 2: التطوير] ──> [المرحلة 3: التسليم]
    (يمين)                                                 (يسار)
    ```
* **Icon & Image Mirroring:**
  * Directional icons (arrows, back/forward buttons, progressive chevron steps) must be horizontally flipped (`scaleX: -1`).
  * Asymmetrical architectural plans, maps, and universal symbols (e.g., search magnifying glass, clock hands) must **never** be mirrored.

## 2. Typographic Scale & Pairing
* **Font Hierarchy (16:9 Slide Dimensions):**
  * Main Slide Title: $36\text{pt} - 44\text{pt}$ (Bold / Black weight).
  * Section Subtitle: $20\text{pt} - 24\text{pt}$ (Medium weight).
  * Body Text: $14\text{pt} - 16\text{pt}$ (Regular weight, Line height $1.4 - 1.5$).
  * Captions & Footnotes: $10\text{pt} - 12\text{pt}$ (Light weight).
* **Recommended Font Pairings:**
  * Modern Tech / BIM: **Cairo** (Titles) + **Almarai** (Body).
  * Formal Corporate / Government: **GE SS Two** or **Tajawal** (Titles) + **Frutiger Arabic** / **Segoe UI** (Body).

## 3. Executive Slide Types
* **The Problem-Solution Split:** 2-column layout (Right: المشكلة والتحدي; Left: الحل المعماري والتقني).
* **Metrics & KPI Dashboard:** 3 or 4 horizontal summary cards showcasing key metrics with bold numbers and concise Arabic microcopy.
* **Architecture Portfolio Showcase:** Full-bleed render slide with a dark translucent gradient card ($40\%$ opacity) on the right hosting project description and scope data.
