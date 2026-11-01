---
name: biophilic-landscape-scatter
description: Automates procedural botanical scattering and realistic landscape population (Forest Pack, Chaos Scatter, Geo-Scatter) using environmental distribution rules (slope, elevation, sunlight), featuring regional arid Middle Eastern vegetation palettes.
---

# Biophilic Landscape Scatter (أتمتة نثر الغطاء النباتي واللاندسكيب الواقعي)

An architectural landscape visualization and procedural scattering skill. It automates the distribution of hyper-realistic plant ecosystems, ground covers, and hardscape features using scattering engines (**Forest Pack Pro**, **Chaos Scatter**, **Geo-Scatter**) driven by environmental distribution rules (altitude thresholds, slope limits, solar exposure, moisture masks), with a specialized botanical library for arid GCC and Middle Eastern climates.

---

## 1. Procedural Distribution Logic

```
[Masterplan Landscape Terrain Surface]
                  │
                  ▼
   [1. Environmental Mask Generation]
   - Altitude Clipping (e.g. Riparian wadi plants only below +15m elevation)
   - Slope Angle Falloff (No large trees on slopes > 35 degrees)
   - Solar Exposure (Drought-tolerant succulents on South/West facing slopes)
                  │
                  ▼
   [2. Botanical Stratification (Canopy -> Shrub -> Groundcover)]
   ┌────────────────────────────────────────────────────────┐
   │ Canopy Layer: Date Palms, Acacia, Delonix Regia        │
   │ Shrub Layer: Bougainvillea, Lantana, Agave, Aloe Vera  │
   │ Ground Cover: Pennisetum fountain grass, gravel mulch  │
   │ Scatter Accent: Weathered desert boulders and rocks    │
   └────────────────────────────────────────────────────────┘
                  │
                  ▼
   [3. Anti-Patterning & Randomization Parameters]
   - Random Yaw Rotation (0° to 360°)
   - Proportional Scale Jitter (80% to 125% with Gaussian bell curve)
   - Color Variation Tinting (+/- 5% Hue & Saturation jitter per instance)
                  │
                  ▼
   [4. Edge Exclusion & Path Boundary Trimming]
   - Automatic spline distance falloff (Clear 1.5m offset from curbs & walkways)
```

---

## 2. Botanical Palette Reference (Arid Middle East / GCC)

| Layer | Botanical Name | Local Common Name | Landscape Role |
| :--- | :--- | :--- | :--- |
| **Primary Palms** | *Phoenix dactylifera* | نخلة التمر (Date Palm) | Formal avenues, entry focal points, courtyard oasis. |
| **Native Trees** | *Vachellia tortilis* | سمر / أكاسيا (Umbrella Thorn) | Natural wadi landscapes, desert perimeter, shade. |
| **Flowering Trees**| *Delonix regia* | بونسيانا (Poinciana) | Vivid seasonal color accents, plaza canopies. |
| **Structural Shrubs**| *Agave americana* / *Aloe* | صبار الأغاف (Desert Agave) | Rock gardens, xeriscaping borders. |
| **Ornamental Grass**| *Pennisetum setaceum* | حلفا حمراء (Fountain Grass) | Mass plantings, wind-blown movement, median strips. |
| **Ground Cover** | *Sesuvium portulacastrum*| سيسوفيوم (Sea Purslane) | Saline-tolerant green carpets, moisture retention. |

---

## 3. Chaos Scatter & Forest Pack Setup (MAXScript)

```maxscript
fn setupAridLandscapeScatter scatterObj terrainMesh plantModelsList =
(
    -- Assign surface
    scatterObj.surfaces = #(terrainMesh)
    
    -- Assign botanical models
    scatterObj.models = plantModelsList
    
    -- Configure distribution
    scatterObj.density = 0.45 -- 0.45 plants per square meter
    scatterObj.random_rotation_z = [0, 360]
    scatterObj.random_scale = [85, 120] -- 85% to 120% scale variation
    
    -- Slope falloff: Stop scattering above 32-degree slope
    scatterObj.slope_enable = true
    scatterObj.slope_min = 0.0
    scatterObj.slope_max = 32.0
    scatterObj.slope_curve = 5.0 -- Soft falloff transition
    
    -- Camera clipping: Don't compute unseen geometry outside camera frustum
    scatterObj.camera_clipping_enable = true
    
    format "Procedural Landscape Scatter created on '%'.\n" terrainMesh.name
)
```

---

## 4. Best Practices for Massive Masterplans
1. **Camera Frustum Culling**: Always enable camera clipping to discard millions of invisible instances behind the camera lens, saving 80% RAM during 8K render passes.
2. **Cluster Distribution**: Use Perlin noise distribution maps rather than purely uniform random scattering to mimic natural seed distribution clumps.
