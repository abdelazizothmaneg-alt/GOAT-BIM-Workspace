---
name: GOAT BIM Suite Architectural Design System
description: Precision Dark Luxury Architectural UI & Bilingual Computational BIM Command Center
colors:
  primary: "#C5A880"
  primary-deep: "#A38660"
  primary-light: "#DFC7A8"
  neutral-bg: "#0B0D10"
  surface-ground: "#0E1116"
  surface-card: "#14171F"
  surface-raised: "#1B202A"
  surface-border: "#202634"
  surface-border-subtle: "#181D27"
  text-primary: "#F8FAFC"
  text-secondary: "#CBD5E1"
  text-muted: "#94A3B8"
  accent-cyan: "#38BDF8"
  accent-success: "#10B981"
  accent-warning: "#F59E0B"
  accent-error: "#EF4444"
typography:
  display:
    fontFamily: "Plus Jakarta Sans, Cairo, sans-serif"
    fontSize: "3rem"
    fontWeight: 700
    lineHeight: 1.15
    letterSpacing: "-0.02em"
  headline:
    fontFamily: "Plus Jakarta Sans, Cairo, sans-serif"
    fontSize: "1.75rem"
    fontWeight: 600
    lineHeight: 1.25
  title:
    fontFamily: "Plus Jakarta Sans, Cairo, sans-serif"
    fontSize: "1.125rem"
    fontWeight: 500
    lineHeight: 1.4
  subtitle:
    fontFamily: "Plus Jakarta Sans, Cairo, sans-serif"
    fontSize: "1rem"
    fontWeight: 600
    lineHeight: 1.4
  body:
    fontFamily: "IBM Plex Sans Arabic, Plus Jakarta Sans, sans-serif"
    fontSize: "0.875rem"
    fontWeight: 400
    lineHeight: 1.6
  caption:
    fontFamily: "IBM Plex Sans Arabic, Plus Jakarta Sans, sans-serif"
    fontSize: "0.75rem"
    fontWeight: 400
    lineHeight: 1.4
  label:
    fontFamily: "IBM Plex Sans Arabic, Plus Jakarta Sans, sans-serif"
    fontSize: "0.6875rem"
    fontWeight: 500
    lineHeight: 1.3
  code:
    fontFamily: "JetBrains Mono, monospace"
    fontSize: "0.8125rem"
    fontWeight: 400
    lineHeight: 1.5
rounded:
  xs: "2px"
  sm: "4px"
  md: "6px"
  lg: "8px"
spacing:
  xs: "4px"
  sm: "8px"
  md: "16px"
  lg: "24px"
  xl: "32px"
  2xl: "48px"
components:
  button-primary:
    backgroundColor: "{colors.primary}"
    textColor: "{colors.neutral-bg}"
    rounded: "{rounded.sm}"
    padding: "8px 18px"
  button-primary-hover:
    backgroundColor: "{colors.primary-light}"
  button-secondary:
    backgroundColor: "{colors.surface-card}"
    textColor: "{colors.text-primary}"
    rounded: "{rounded.sm}"
    padding: "8px 18px"
  card-panel:
    backgroundColor: "{colors.surface-card}"
    rounded: "{rounded.md}"
    padding: "20px"
---

## Overview

The GOAT BIM Suite design system is a **Dark Luxury Architectural Command Center** engineered for high-density AEC tools, BIM coordinates, geometry workflows, and automated audit dashboards. It bridges the discipline of Swiss architectural drafting (rigid modular grids, hairline technical borders, high data density) with modern dark luxury aesthetics (obsidian slate `#0B0D10` base, brushed champagne bronze `#C5A880`, subtle frosted overlays).

It is strictly bilingual, supporting English LTR and Arabic RTL with zero typographic compromise or visual misalignment.

## Colors

- **Base Obsidian Slate (`#0B0D10`)**: The deep foundation for 3D viewports and application backdrops, preventing visual fatigue during extended modeling sessions.
- **Architectural Bronze Accent (`#C5A880` / `#A38660`)**: The signature interactive color representing luxury materials, active states, key CTAs, and selected element highlights.
- **Layered Surfaces (`#0E1116` / `#14171F` / `#1B202A`)**: Discreet visual elevation achieved through tonal steps rather than heavy drop shadows.
- **Hairline Structural Borders (`#202634` / `#181D27`)**: Crisp 1px drafting lines separating spatial viewports, toolbars, and property grids.
- **Engineering Dimension Cyan (`#38BDF8`)**: Used exclusively for telemetry, dimensions, snap markers, and real-time CAD coordinates.
- **Functional Semantics**:
  - Success / Audit Pass: Emerald `#10B981`
  - Warning / Geometric Tolerance Alert: Amber `#F59E0B`
  - Error / Hard Clash: Crimson `#EF4444`

## Typography

- **Arabic Headlines & Display (`Cairo`)**: Geometric, authoritative Arabic typeface with crisp letterforms that harmonizes with modern architectural blueprints.
- **Arabic Body & Parameter Grids (`IBM Plex Sans Arabic`)**: Engineered for technical clarity, compact data readability, and effortless scanning in multi-column schedules.
- **Latin & Interface Headers (`Plus Jakarta Sans`)**: Clean contemporary grotesque pairing with high legibility across all weights.
- **Telemetry, Coordinates & Logs (`JetBrains Mono`)**: Strict monospace numerals with `font-variant-numeric: tabular-nums` to guarantee vertical column alignment in quantity takeoffs and coordinate readouts.

## Layout

- **Architectural Precision Grid**: 8px modular baseline grid with strict Swiss alignment principles.
- **View-First Hierarchy**: In 3D exploration and dashboard views, the WebGL viewport commands maximum real estate with floating, translucent toolbars.
- **Bidirectional Mirroring (RTL/LTR)**:
  - Navigation docks and tree inspectors mirror logically without breaking layout proportions.
  - Numbers and dimensional units remain mathematically aligned with appropriate spacing.

## Elevation & Depth

- **Tonal Layering over Blur**: Depth is communicated primarily through precise luminance steps (`#0B0D10` ground -> `#14171F` cards -> `#1B202A` active panels).
- **Subtle Glassmorphism**: Floating HUD overlays and model property cards utilize `backdrop-filter: blur(12px)` over 3D scenes with an ultra-thin `border: 1px solid rgba(255,255,255,0.06)`.
- **Zero Heavy Shadows**: No muddy, unfocused dark drop-shadows; shadows are reserved for subtle elevation (`box-shadow: 0 4px 20px -2px rgba(0,0,0,0.5)`).

## Shapes

- **Architectural Chamfers & Restrained Radii**:
  - Radii are tightly controlled (`sm: 4px`, `md: 6px`) reflecting manufactured steel and stone edges.
  - No ballooned, cartoonish rounded-full pill buttons or soft card designs.
  - Strict square and razor-clean edges for technical tool palettes and coordinate meters.

## Components

- **Primary Buttons**: Bronze brushed background (`#C5A880`), dark text (`#0B0D10`), medium weight, 4px border radius.
- **Secondary / Ghost Buttons**: Subtle slate background with 1px border (`#202634`), light slate text, transitioning on hover to muted bronze glow.
- **Viewport HUD Overlays**: Floating frosted panels with discrete coordinate meters, snap mode toggles, and view cube orientation gizmos.
- **BIM Property Inspector**: High-density key-value pairs separated by hairline dividers with monospace values on the right (or left in RTL).

## Do's and Don'ts

### Do:
- Maintain strict high contrast between text (`#F8FAFC`) and dark backgrounds (`#14171F`).
- Use `tabular-nums` for all quantities, dimensions, levels, and coordinate readouts.
- Mirror the entire interface when switching to Arabic RTL while preserving numerical left-to-right conventions for coordinates (`X: 12.45, Y: -3.80`).
- Ensure all interactive elements give crisp, immediate micro-feedback (subtle border color shift or brightness increase).

### Don't:
- Never use generic neon gradients (e.g., purple/cyan "AI" aesthetics) that destroy professional credibility.
- Never use soft, bubbly 24px border radii or playful pastel colors.
- Never hardcode `left` or `right` CSS positioning; always utilize logical properties (`start`, `end`, `margin-inline`).
- Never compromise Arabic font rendering with low-quality system fallback typefaces.
