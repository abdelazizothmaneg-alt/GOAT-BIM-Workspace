---
name: corona-decal-pattern-detailing
description: Implements micro-scale architectural imperfections using CoronaDecal for road markings, weathering, and cracks, and leverages CoronaPattern for zero-RAM procedural repeating facade meshes in 3ds Max.
---

# Corona Decal & Pattern Detailing

High-end surface imperfection and procedural geometric repetition skill for Autodesk 3ds Max and Chaos Corona Renderer. Leverages `CoronaDecal` for non-destructive surface detailing and `CoronaPattern` for ultra-lightweight repeating architectural geometry.

---

## 1. CoronaDecal: Surface Imperfections Without UV Remapping

`CoronaDecal` projects a localized material onto underlying scene geometry without modifying the base object's topology or UV unwrapping:

```
[ CoronaDecal Box Projection ]
              │
              ▼ (Projects vertically or along local Z)
    [ Asphalt Road / Concrete Wall ]
              │
              ├── Color: Painted Road Stripes / Water Stain
              ├── Roughness: Glossy wet water puddle
              ├── Bump/Displacement: Pavement crack indentation
              └── Opacity: Feathered alpha edge mask
```

### Key Parameter Settings:
- **Projection Depth**: Restrict the Z-depth box to prevent the decal from accidentally projecting onto the underside of slabs or neighboring curbs.
- **Angle Falloff**: Tapers the decal opacity when the underlying surface normal deviates by $> 60^\circ$.

---

## 2. Automated Road Marking Decal Setup (MAXScript)

```maxscript
fn createRoadMarkingDecal pos:[0,0,0] width:2000 height:4000 decalMtl:undefined =
(
    -- Create CoronaDecal Helper
    local decal = CoronaDecal()
    decal.name = "Decal_RoadMarking"
    decal.pos = pos
    
    -- Dimensions (mm)
    decal.width = width
    decal.height = height
    decal.depth = 150.0 -- Shallow 150mm depth to affect asphalt only
    
    if decalMtl != undefined do
    (
        decal.material = decalMtl
    )
    
    return decal
)
```

---

## 3. CoronaPattern: Geometry Repetition with Zero RAM Overhead

`CoronaPattern` tiles a 3D geometric module (e.g., perforated sheet metal, diamond metal grating, woven wicker mesh) seamlessly across a host surface:
- **Why CoronaPattern over Displace/Scatter?**
  - Unlike polygon subdivision or heavy displacement maps, `CoronaPattern` generates virtual instances at render time with virtually zero RAM footprint.

### MAXScript Pattern Configuration:
```maxscript
fn setupFacadePattern hostWall:undefined patternModule:undefined uvTiling:[10, 10] =
(
    if hostWall == undefined or patternModule == undefined do
    (
        messageBox "Specify valid host surface and pattern module!"
        return false
    )
    
    -- CoronaPattern is applied via modifier or specialized material node
    local cPatternMod = CoronaPatternMod()
    cPatternMod.patternObject = patternModule
    cPatternMod.uTile = uvTiling.x
    cPatternMod.vTile = uvTiling.y
    cPatternMod.heightScale = 1.0
    
    addModifier hostWall cPatternMod
    format "CoronaPattern modifier applied to % with module %.\n" hostWall.name patternModule.name
)
```

---

## 4. Deliverables Checklist
1. **Decal Bounding Box Verification**: Confirm decals are positioned tightly against target surfaces with bounded projection depths.
2. **Procedural Perforated Facade Render**: Full-scale test rendering of woven/perforated architectural screens using CoronaPattern.
