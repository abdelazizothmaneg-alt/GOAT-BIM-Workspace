---
name: color-harmony-engine
description: >-
  Architectural and interior color formulation engine calculating harmonic color palettes, Light Reflectance Values (LRV),
  Kelvin lighting interactions, and cross-matching across RAL, Pantone, NCS, and major paint fan decks.
---

# Color Harmony Engine

Scientific and aesthetic chromatic formulation system for interior architecture and spatial psychology.

## 1. Palette Harmonic Structures
* **Monochromatic with Value Contrast:**
  * Single hue varied through tints (white added), shades (black added), and tones (grey added).
  * Requires high Light Reflectance Value (LRV) contrast (e.g., Wall LRV 75 vs. Millwork LRV 25) to avoid looking flat.
* **Analogous Warmth:**
  * 3 neighboring hues on the 12-part color wheel (e.g., Terracotta / Ochre / Warm Cream) creating serene, comfortable living zones.
* **Complementary / Split-Complementary Tension:**
  * Opposing hues (e.g., Deep Navy Blue & Cognac Leather, or Sage Green & Rust Burgundy) where the warm tone serves as an intentional $10\%$ focal accent.

## 2. Technical Color Metrics
* **Light Reflectance Value (LRV):**
  * Measures percentage of light a paint color reflects ($0\% = \text{Absolute Black}, 100\% = \text{Pure White}$).
  * Small / Dark rooms: Choose wall paint with $\text{LRV} \ge 65 - 75\%$ to bounce natural light.
  * Dramatic Dining / Powder rooms: Embrace low $\text{LRV} \le 15 - 25\%$ (Charcoal, Forest Green, Deep Aubergine) for intimate moody envelopes.
* **Correlated Color Temperature (CCT) Interaction:**
  * $2700\text{K}$ (Warm Incandescent): Enhances warm yellows, reds, and woods; dulls cool blues and greys.
  * $3000\text{K}$ (Warm White): The universal interior standard; preserves true neutral and beige tones.
  * $4000\text{K}$ (Cool White): Ideal for task kitchens, bathrooms, and commercial offices; prevents whites from looking yellow.

## 3. Commercial Color Codes Cross-Reference
* Format outputs with standardized industrial references:
  * Hex `#RRGGBB` / RGB
  * RAL Classic (e.g., RAL 9010 Pure White, RAL 7016 Anthracite Grey)
  * NCS (Natural Colour System) notation (e.g., S 1005-Y20R)
  * Major paint manufacturers (Benjamin Moore, Sherwin-Williams, Jotun).
