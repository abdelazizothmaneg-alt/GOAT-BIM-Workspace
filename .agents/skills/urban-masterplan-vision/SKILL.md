---
name: urban-masterplan-vision
description: Synthesizes and analyzes high-fidelity urban masterplan visualizations, bird's-eye aerial perspectives, complete streetscape cross-sections, and pedestrian public realm scenes from 2D CAD/GIS boundary layouts, enforcing bioclimatic shading, transit orientation, and Middle East urban planning typologies.
---

# Urban Masterplan Vision (رؤية وتوليد المخططات الحضرية والمشاهد العامة)

A specialized computational urban design and generative vision skill. It bridges 2D spatial layouts (AutoCAD DWG, GIS shapefiles, zoning plots) with high-fidelity 3D aerial masterplan renderings, pedestrian-scale streetscape visualizations, and public realm simulations tailored to regional environmental and zoning parameters.

---

## 1. Urban Generation & Control Pipeline

```
[2.5D CAD / GIS Layout (Plots, Roads, Green Networks)]
                         │
                         ▼
   [1. ControlNet Conditioning Pre-Processing]
   - Color-coded zoning boundary map (Semantic Segmentation mask)
   - Height-field elevation map (Depth conditioning for building massing)
   - Road network vector lines (LineArt / Canny conditioning)
                         │
                         ▼
   [2. Generative Diffusion with Controlled Prompting]
   - Aerial Bird's-Eye Perspective / Drone oblique view
   - Regional Bioclimatic styling (Salmani / Modern Gulf / Mediterranean)
   - Realistic daylighting & solar shadow cast (calculated azimuth & altitude)
                         │
                         ▼
   [3. Streetscape & Public Realm Simulation]
   - Complete Streets cross-sections (Pedestrian / Micromobility / Vehicular)
   - Native arid landscape integration (Date Palms, Acacia, Xeriscaping)
   - Shaded colonnades, pergolas, and active pedestrian ground-floor edges
                         │
                         ▼
   [4. Urban Metric Cross-Validation]
   - Ground Coverage Ratio (GCR) & Floor Area Ratio (FAR) verification
   - Walkability radius (400m / 5-minute isochrone)
```

---

## 2. Conditioning Vector Maps for AI Synthesis (Python)

To generate an aerial masterplan that respects exact plot boundaries without drifting, create a multi-layer conditioning raster:

```python
import cv2
import numpy as np

def create_urban_control_mask(width: int = 2048, height: int = 2048):
    """
    Generate a standardized semantic segmentation map from urban CAD vectors:
    - Road asphalt: Dark Grey [60, 60, 60]
    - Sidewalks / Plazas: Light Sand/Grey [180, 170, 150]
    - Green Open Spaces / Parks: Green [40, 150, 40]
    - Residential Parcels: Warm Terracotta [190, 90, 60]
    - Commercial / Mixed-Use: Vibrant Blue/Indigo [50, 90, 190]
    - Water Bodies: Deep Blue [30, 80, 180]
    """
    canvas = np.zeros((height, width, 3), dtype=np.uint8)
    canvas[:] = (230, 225, 215) # Default sandy terrain base

    # Example: Defining road network (asphalt)
    roads = [
        np.array([[0, 950], [width, 950], [width, 1100], [0, 1100]]),
        np.array([[950, 0], [1100, 0], [1100, height], [950, height]])
    ]
    cv2.fillPoly(canvas, roads, (60, 60, 60))

    # Example: Central Urban Park / Green Spine
    park = np.array([[200, 200], [800, 200], [800, 800], [200, 800]])
    cv2.fillPoly(canvas, [park], (40, 150, 40))

    # Example: High-density mixed-use blocks
    commercial_block = np.array([[1200, 200], [1800, 200], [1800, 800], [1200, 800]])
    cv2.fillPoly(canvas, [commercial_block], (50, 90, 190))

    cv2.imwrite("urban_conditioning_mask.png", canvas)
    return "urban_conditioning_mask.png"
```

---

## 3. High-Precision Prompt Construction Framework

When generating masterplan or streetscape visualizations, enforce strict architectural vocabulary:

### A. Aerial Masterplan Rendering
```
Prompt:
"Ultra-photorealistic drone oblique aerial view of a sustainable mixed-use masterplan in Riyadh, Saudi Arabia, 
featuring mid-rise contemporary Salmani architectural blocks, local Riyadh limestone and cream sandstone facades, 
lush central green wadi park with native date palms and acacia tortilis trees, wide shaded pedestrian boulevards, 
clear geometric road grid with pedestrian zebra crossings, accurate late afternoon golden hour sun casting long crisp shadows, 
8k resolution, architectural photography, Hasselblad H6D-100c, sharp focus, zero distortion"
```

### B. Complete Street & Pedestrian Realm (Human Scale)
```
Prompt:
"Eye-level architectural perspective of a lively pedestrianized complete street in a GCC smart city, 
active ground-floor retail with outdoor cafes, kinetic timber mashrabiya facade shading systems on upper residential floors, 
permeable light-colored stone pavers, integrated bioswales and drought-tolerant planting, modern tensile fabric canopy walkways, 
people walking and cycling, soft daylighting, clean lens, photorealistic"
```

---

## 4. Regional GCC & Arid Urban Guidelines
- **Thermal Comfort & Shading**: Prioritize self-shading building masses (narrow street canyons $H/W \ge 1.5$), continuous shaded arcades, and light-reflecting hardscapes (Solar Reflectance Index $\text{SRI} \ge 78$).
- **Planting Selection**: Enforce regionally accurate vegetation: *Phoenix dactylifera* (Date Palm), *Vachellia tortilis* (Samr / Acacia), *Ziziphus spina-christi* (Sidr), avoiding water-intensive European lawns.
- **Architectural Identity**: Adhere to local vernaculars—such as the Salmani Architecture Code in Riyadh (triangular decorative openings, textured limestone cladding, modern interpretations of Najdi crenellations).

---

## 5. Automated Urban Metric Verification

When evaluating an urban design visual:
1. **Building Footprint Coverage**: Ensure building footprint matches targeted Ground Coverage Ratio ($GCR \le 40 - 60\%$).
2. **Right-of-Way (ROW) Cross-Section**: Verify standard corridor allocations:
   - Sidewalk clear path: $\ge 2.5\text{ m}$.
   - Protected bike lane: $\ge 2.0\text{ m}$ (two-way).
   - Planting & furnishing buffer: $\ge 1.8\text{ m}$.
   - Transit / Travel lanes: $3.2 - 3.5\text{ m}$ per lane.
