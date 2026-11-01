---
name: chaos-scatter-vegetation-biomes
description: Automates procedural botanical scattering and realistic landscape population using Chaos Scatter and Corona Scatter in 3ds Max, defining slope limits, elevation falloffs, and regional arid vegetation biomes.
---

# Chaos Scatter & Corona Vegetation Biomes

Landscape and procedural environment skill for automating large-scale botanical population, turf lawns, desert gravel, and forest biomes using `ChaosScatter` and `CoronaScatter` in Autodesk 3ds Max.

---

## 1. Procedural Scatter Architecture & Rules

```
Terrain Surface Node ──> [ Chaos / Corona Scatter Engine ] <── Instanced Models (Trees, Rocks, Grass)
                                   │
      ┌────────────────────────────┼────────────────────────────┐
      ▼                            ▼                            ▼
Slope Angle Limits          Scale & Rotation Jitter       Collision Avoidance
(0° to 35° on flat,         (Z-rotation 0-360°,           (Minimum 2.5m radius
 excluded on cliffs)         scale 80% to 125%)            between tree trunks)
```

---

## 2. MAXScript Biome Generator

```maxscript
fn createVegetationBiome biomeType:#desertWadi terrainMesh:undefined modelList:#() =
(
    if terrainMesh == undefined or modelList.count == 0 do
    (
        messageBox "Invalid terrain mesh or empty model list!"
        return false
    )
    
    -- Create Scatter Object
    local sc = CoronaScatter()
    sc.name = "Scatter_" + (biomeType as string)
    
    -- Assign Distribution Surface and Scattered Models
    sc.distributionNodes = #(terrainMesh)
    sc.scatteredNodes = modelList
    
    -- Common Transformations
    sc.rotXMin = -5.0; sc.rotXMax = 5.0
    sc.rotYMin = -5.0; sc.rotYMax = 5.0
    sc.rotZMin = 0.0;  sc.rotZMax = 360.0 -- Full random 360 rotation
    
    case biomeType of
    (
        #desertWadi:
        (
            -- Sparse desert vegetation with rocks and drought-resistant shrubs
            sc.density = 2.5 -- low density
            sc.scaleMin = 0.75
            sc.scaleMax = 1.35
            sc.slopeAngleMin = 0.0
            sc.slopeAngleMax = 25.0 -- Don't grow on steep gullies
            sc.alignToNormal = 0.4 -- Grow mostly upward against gravity
        )
        #lushLawn:
        (
            -- Ultra-dense turf grass carpet
            sc.density = 1500.0
            sc.scaleMin = 0.85
            sc.scaleMax = 1.15
            sc.slopeAngleMin = 0.0
            sc.slopeAngleMax = 45.0
            sc.alignToNormal = 0.8 -- Follow terrain slope contours
        )
        #canopyForest:
        (
            -- Tall architectural trees with collision avoidance
            sc.density = 8.0
            sc.scaleMin = 0.90
            sc.scaleMax = 1.25
            sc.slopeAngleMin = 0.0
            sc.slopeAngleMax = 32.0
            sc.alignToNormal = 0.15 -- Trees grow vertical towards the sky
        )
    )
    
    -- Optimize Viewport Display Performance
    sc.displayLimitMode = 1 -- Limit display percentage
    sc.displayCountMax = 5000 -- Only display 5000 instances in viewport
    sc.displayType = 2 -- Display as Point Cloud / Boxes to prevent viewport lag
    
    format "Created % with % scattered models successfully.\n" sc.name modelList.count
    return sc
)
```

---

## 3. Spline Masking & Road Exclusion

To prevent vegetation from scattering onto pedestrian paths, roads, or building footprints:
1. Draw closed 2D splines representing pedestrian walkways or building slabs.
2. In the Scatter modifier / rollout, assign the splines as **Exclusion Spline Areas**.
3. Set **Falloff Distance ($0.5\text{ m} - 1.0\text{ m}$)** to smoothly taper plant sizes near sidewalk edges.

---

## 4. Deliverables Checklist
1. **Viewport Optimization Check**: Verify all scatter instances are displayed as Bounding Boxes or Wireframe Point Clouds in 3ds Max viewport.
2. **Instance Collision Audit**: Confirm tree trunks do not penetrate building overhangs or clash with structural columns.
