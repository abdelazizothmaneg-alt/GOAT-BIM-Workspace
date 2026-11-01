---
name: urban-calculator
description: >-
  Calculates critical urban design and planning metrics including Floor Area Ratio (FAR), Ground Coverage Ratio (GCR/Footprint),
  residential/employment densities, parking demand ratios, green space indices, and walkability metrics.
---

# Urban Calculator

Specialized engineering and calculation skill for urban planners, architects, and municipal analysts to quantify urban density, land-use distribution, and zoning capacity.

## Core Formulae & Metric Calculations

### 1. Land Use & Spatial Intensity
* **Floor Area Ratio (FAR / Plot Ratio):**
  $$\text{FAR} = \frac{\text{Gross Floor Area (GFA)}}{\text{Total Site Area}}$$
* **Ground Coverage Ratio (GCR / Site Coverage):**
  $$\text{GCR (\%)} = \left(\frac{\text{Building Footprint Area}}{\text{Total Site Area}}\right) \times 100$$
* **Open Space Ratio (OSR):**
  $$\text{OSR (\%)} = \left(\frac{\text{Unbuilt / Permeable Green Area}}{\text{Total Site Area}}\right) \times 100$$
* **Net Density vs. Gross Density:**
  * Net Density: Units or Population / Net Residential Land Area (excluding arterial roads/parks).
  * Gross Density: Units or Population / Total Master Plan Area.

### 2. Demographic & Infrastructure Demand
* **Population Capacity:**
  $$\text{Target Population} = \text{Total Residential Units} \times \text{Average Household Size (PPH)}$$
* **Parking Demand Ratio:**
  $$\text{Required Bays} = \sum (\text{Use GFA or Units} \times \text{Zoning Ratio})$$
  * Residential: $1.0 - 2.0$ bays / unit (or TOD discount $0.5 - 0.75$).
  * Commercial / Office: $1$ bay per $25 - 50\text{ m}^2$ GFA.
  * Retail / F&B: $1$ bay per $15 - 30\text{ m}^2$ GFA.
* **Water & Sewage Generation Demand:**
  $$\text{Domestic Demand} = \text{Population} \times \text{Per Capita Consumption (L/capita/day)}$$

### 3. Sustainable Site Metrics
* **Green Area Ratio (GAR / Biotope Area Factor):**
  Quantifies ecological performance by weighting surface types (e.g., intensive green roofs = 0.7, permeable pavers = 0.5, deep soil tree canopy = 1.0).
* **Pedestrian Catchment (Isochrone):**
  * 5-minute walk: $\approx 400\text{ m}$ radius.
  * 10-minute walk: $\approx 800\text{ m}$ radius.
