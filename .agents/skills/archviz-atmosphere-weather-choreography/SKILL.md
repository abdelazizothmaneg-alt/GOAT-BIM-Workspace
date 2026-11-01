---
name: archviz-atmosphere-weather-choreography
description: Masters poetic atmospheric archviz in the style of MIR Bergen, creating volumetric fog, crepuscular light rays, wet rain ground reflections, dramatic overcast skies, and seasonal mood choreography.
---

# ArchViz Atmosphere & Weather Choreography (The MIR Way)

World-class architectural visualization lighting and atmosphere skill inspired by the evocative philosophy of **MIR Bergen**. Moves beyond sterile midday sunny renders into emotional, painterly, and atmospheric visual storytelling.

---

## 1. The MIR Philosophy: "Portraits of Unbuilt Architecture"

```
Standard Commercial 3D Render:               World-Class Atmospheric Visual:
├── Blue sky, midday harsh sun (flat)        ├── Mood-driven weather (Overcast, rain, dawn mist)
├── Every light turned on at 100%             ├── Subtle, poetic lighting balance (Warm hearth vs cool sky)
├── Clean, dry, sterile asphalt               ├── Wet reflective ground catching specular street highlights
└── Structure screams for attention          └── Architecture emerges organically from its natural "terroir"
```

---

## 2. Atmospheric Volumetrics & Fog Shaders

### A. Corona Volume Material (`CoronaVolumeMtl`)
For physical aerial haze and crepuscular rays (God Rays):
```maxscript
fn setupAtmosphericFog scatteringDist:150000.0 absorptionDist:200000.0 =
(
    -- Assign global volume material to render environment
    local volMtl = CoronaVolumeMtl()
    volMtl.name = "Env_AtmosphericFog"
    
    -- Scattering: creates light shafts when hit by directional sunlight
    volMtl.colorScattering = color 235 240 248 -- Soft cool morning haze
    volMtl.distanceScattering = scatteringDist -- 150 meters penetration
    
    -- Directionality (Henyey-Greenstein g): 0.5 to 0.75 for strong forward god rays
    volMtl.directionality = 0.65
    
    -- Absorption: prevents infinite fog accumulation
    volMtl.colorAbsorption = color 240 240 245
    volMtl.distanceAbsorption = absorptionDist
    
    -- Set to global environment volume
    useEnvironmentMap = true
    renderers.current.volumetric_environment = volMtl
    format "Atmospheric volumetric fog applied successfully.\n"
)
```

### B. Wet Ground & Rain Reflection Shader Recipe
To create realistic wet asphalt or polished granite after rain:
1. **Base Layer**: Rough asphalt (`Roughness = 0.65 - 0.75`).
2. **Puddle Mask**: High-contrast procedural noise or 4K puddle moisture texture plugged into the **Clearcoat Amount** or inverted **Roughness**.
3. **Puddles**: In wet zones, `Roughness drops to 0.02 - 0.05` with `IOR = 1.33` (water), reflecting illuminated interior windows and streetlights like mirrors.

---

## 3. Light Balance: The Warm-Cool Dialogue

In atmospheric archviz, light is a dialogue between two opposing color temperatures:
- **The Ambient Environment (Cool / Muted)**: Overcast sky, blue hour twilight, or misty mountain fog ($6000\text{K} - 7500\text{K}$).
- **The Architectural Heart (Warm / Inviting)**: Interior lighting, glowing fireplace, or pendant downlights spilling through glass ($2400\text{K} - 2800\text{K}$).
- *Emotional Effect*: The viewer’s eye is naturally drawn toward the warm, sheltering refuge within the cold, sublime landscape.

---

## 4. Deliverables Checklist
1. **Z-Depth / Mist Pass (16-bit Float)**: Depth gradient element for fine-tuning aerial haze falloff in post-production.
2. **God Rays Layer**: Volumetric light shaft beauty pass isolated for contrast adjustment in Photoshop / After Effects.
3. **Atmospheric Concept Board**: Reference mood board establishing the specific weather condition (e.g. "Dusk after a November rainstorm").
