---
name: corona-physical-materials-shader
description: Constructs, tunes, and converts photorealistic CoronaPhysicalMtl shaders in 3ds Max, configuring Clearcoat, Sheen, Thin-Shell translucency, Subsurface Scattering (SSS), and batch material migration.
---

# Corona Physical Materials & PBR Shading

Master material creation, lookdev, and batch shader migration skill for `CoronaPhysicalMtl` in Autodesk 3ds Max. Specializes in advanced physical shading parameters, micro-imperfections, and automatic translation from legacy shaders.

---

## 1. CoronaPhysicalMtl Architecture & Parameter Rules

```
CoronaPhysicalMtl
├── Base Layer: Color / Metalness (0=Dielectric, 1=Metal) / Roughness / IOR
├── Clearcoat Layer: Separate uncoupled top lacquer coating (Varnished wood, car paint)
├── Sheen Layer: Cloth micro-fiber edge grazing glow (Velvet, cotton, satin)
├── Glass / Refraction: Absorption color, dispersion (Abbe number), volumetric fog
├── Thin-Shell Mode: Double-sided leaf/curtain translucency without geometric thickness
└── Subsurface Scattering (SSS): Depth absorption & scatter radius (Marble, wax, human skin)
```

---

## 2. Material Shader Presets & Recipe Functions (MAXScript)

```maxscript
-- Function to create Calacatta Gold Natural Marble
fn createCoronaMarbleMtl mtlName:"Calacatta_Marble" =
(
    local mtl = CoronaPhysicalMtl()
    mtl.name = mtlName
    mtl.metalnessMode = 0 -- Dielectric
    mtl.baseIor = 1.54
    mtl.baseRoughness = 0.12 -- Honed semi-gloss
    
    -- Subsurface Scattering (SSS) for authentic stone translucency
    mtl.scatteringModel = 1 -- SSS
    mtl.subsurfaceAmount = 0.35
    mtl.subsurfaceRadius = 8.0 -- 8mm light penetration
    mtl.subsurfaceScatterColor = color 245 240 235
    
    return mtl
)

-- Function to create Luxury Varnished Walnut Hardwood
fn createCoronaVarnishedWoodMtl mtlName:"Varnished_Walnut" =
(
    local mtl = CoronaPhysicalMtl()
    mtl.name = mtlName
    mtl.metalnessMode = 0
    mtl.baseIor = 1.50
    mtl.baseRoughness = 0.45 -- Wood substrate underneath
    
    -- Clearcoat layer simulating polyurethane varnish
    mtl.clearcoatAmount = 0.85
    mtl.clearcoatRoughness = 0.08 -- Very smooth glossy top coat
    mtl.clearcoatIor = 1.52
    
    return mtl
)

-- Function to create Architectural Sheen Velvet Fabric
fn createCoronaVelvetMtl mtlName:"Navy_Velvet" baseCol:(color 20 30 65) =
(
    local mtl = CoronaPhysicalMtl()
    mtl.name = mtlName
    mtl.baseColor = baseCol
    mtl.baseRoughness = 0.85 -- Diffuse rough fabric
    
    -- Sheen for micro-fiber sheen along grazing viewing angles
    mtl.sheenAmount = 0.90
    mtl.sheenRoughness = 0.35
    mtl.sheenColor = color 70 95 160 -- Slightly lighter edge grazing tint
    
    return mtl
)
```

---

## 3. Batch Legacy Material Converter to `CoronaPhysicalMtl`

```maxscript
fn batchUpgradeToCoronaPhysical =
(
    local convertedCount = 0
    for m in sceneMaterials do
    (
        -- Convert CoronaLegacyMtl to modern CoronaPhysicalMtl
        if classOf m == CoronaLegacyMtl then
        (
            local newPhys = CoronaPhysicalMtl()
            newPhys.name = m.name + "_Physical"
            
            -- Map Base Diffuse
            newPhys.baseColor = m.colorDiffuse
            newPhys.baseColorTexmap = m.texmapDiffuse
            
            -- Invert Glossiness to Roughness (Roughness = 1.0 - Glossiness)
            newPhys.baseRoughness = 1.0 - m.reflectionGlossiness
            newPhys.baseRoughnessTexmap = m.texmapReflectGlossiness
            
            -- IOR & Bump
            newPhys.baseIor = m.fresnelIor
            newPhys.baseBumpTexmap = m.texmapBump
            
            replaceInstances m newPhys
            convertedCount += 1
        )
    )
    format "Successfully converted % legacy materials to CoronaPhysicalMtl.\n" convertedCount
)
```

---

## 4. Normal Maps & Micro-Detail Best Practices
1. **Gamma 1.0 on Normal Maps**:
   - Always load normal map bitmaps via `CoronaBitmap` with gamma set explicitly to **`1.0`** (Linear), plugged into a `CoronaNormal` operator with "Add gamma to input" disabled.
2. **CoronaRoundEdges for Chamfer Simulation**:
   - Never render razor-sharp $90^\circ$ computer-modeled edges.
   - Plug `CoronaRoundEdges` (radius $2.0 - 4.0\text{ mm}$) into the bump channel to catch natural edge light glints without polycount inflation.
