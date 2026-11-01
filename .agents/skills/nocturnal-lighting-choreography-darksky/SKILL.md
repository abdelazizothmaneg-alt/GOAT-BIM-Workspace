---
name: nocturnal-lighting-choreography-darksky
description: Choreographs luxury nocturnal architectural scenes, multi-tier Kelvin temperature hierarchies (2200K-3000K), facade grazing, and Dark-Sky compliance preventing light pollution.
---

# Nocturnal Lighting Choreography & Dark-Sky Architecture

Master-level nocturnal lighting design and night architectural visualization skill. Choreographs multi-tiered Kelvin color temperature hierarchies, textured facade grazing, submerged pool illumination, and International Dark-Sky Association (IDA) compliance.

---

## 1. The Nocturnal Lighting Hierarchy (Kelvin Temperature Tiers)

A breathtaking night render is never lit with a single flat color temperature. It is a multi-layered symphony of contrasting warmths:

```
[ Tier 1: Fire & Hearth ]          ──> 1800K - 2200K (Outdoor fire pits, gas torches, glowing embers)
[ Tier 2: Interior Living Warmth ] ──> 2400K - 2700K (Warm pendant dining lights, floor lamps, bookshelf coves)
[ Tier 3: Architectural Facade ]   ──> 3000K (Grazing narrow-beam spotlights revealing limestone textures)
[ Tier 4: Landscape Vegetation ]   ──> 2700K - 3000K (Concealed uplights illuminating palm tree fronds)
[ Tier 5: Submerged Water ]        ──> 3500K - 4500K (Crisp turquoise underwater pool fixtures)
[ Tier 6: Ambient Night Sky ]      ──> 6500K - 8500K (Deep cobalt moonlight & star vault)
```

---

## 2. Facade Lighting Typologies & Optical Mechanics

1. **Grazing Uplight (الإضاءة المماسية)**:
   - Luminaire placed $150 - 300\text{ mm}$ from wall face with an ultra-narrow beam angle ($10^\circ - 15^\circ$).
   - Casts microscopic shadows beneath every chisel mark of rough-faced stone, emphasizing texture and relief.
2. **Column Silhouetting (التظليل العكسي للأعمدة)**:
   - Wash the wall *behind* a colonnade with uniform light, leaving the front face of structural columns in dramatic dark silhouette.
3. **Window Reveal Accents (إضاءة التجاويف)**:
   - Miniature linear LED strips embedded in deep window jambs, framing each aperture without glaring into bedrooms.

---

## 3. Dark-Sky Environmental Compliance (IDA Standards)

Uncontrolled lighting causes skyglow, disrupts nocturnal bird migration, and wastes energy:
- **BUG Rating (Backlight, Uplight, Glare)**:
  - Uplight rating must equal **$U0$** (Zero light emission above the horizontal $90^\circ$ plane).
- **Full Cut-Off Fixtures**: All exterior path and street luminaires must feature opaque flat glass lenses directing $100\%$ of luminous flux downwards.

---

## 4. Python Nocturnal Lighting Master Calculator

```python
"""
Nocturnal Kelvin Hierarchy & Contrast Balance Calculator
Verifies aesthetic harmony between interior spill and exterior landscape
"""

def evaluate_night_lighting_palette(lights_in_scene: list) -> dict:
    cct_values = [l["cct_kelvin"] for l in lights_in_scene]
    min_cct = min(cct_values)
    max_cct = max(cct_values)
    
    # Check for presence of key emotional contrast tiers
    has_hearth = any(k <= 2400 for k in cct_values)
    has_facade = any(2700 <= k <= 3200 for k in cct_values)
    has_sky_balance = any(k >= 6000 for k in cct_values)
    
    score = 100
    critiques = []
    
    if max_cct - min_cct < 1500:
        score -= 30
        critiques.append("Lighting is too monochromatic! Add contrasting cool ambient sky or warm firelight.")
        
    if not has_hearth:
        score -= 15
        critiques.append("Missing intimate warm hearth tier (2200K - 2400K) to draw viewer's eye inside.")
        
    return {
        "cct_range_kelvin": f"{min_cct}K to {max_cct}K",
        "nocturnal_harmony_score": max(score, 0),
        "tier_completeness": "EXEMPLARY" if score >= 85 else "NEEDS_CONTRAST",
        "artistic_critique": critiques if critiques else ["Perfect multi-tier nocturnal depth and temperature balance."]
    }

if __name__ == "__main__":
    scene_lighting = [
        {"name": "FirePit_Patio", "cct_kelvin": 2000},
        {"name": "LivingRoom_Chandelier", "cct_kelvin": 2600},
        {"name": "StoneWall_Grazers", "cct_kelvin": 3000},
        {"name": "Pool_Submerged_Light", "cct_kelvin": 4000},
        {"name": "Moonlight_Sky_Fill", "cct_kelvin": 7500}
    ]
    audit = evaluate_night_lighting_palette(scene_lighting)
    print("Nocturnal Lighting Audit:")
    for k, v in audit.items():
        print(f"  {k}: {v}")
```

---

## 5. Deliverables Checklist
1. **Nocturnal Light Plot Diagram**: Plan indicating fixture locations, beam angles, CCT Kelvin ratings, and lux intensities.
2. **Zero-Uplight Dark-Sky Certificate**: Verification that all outdoor architectural luminaires feature $0^\circ$ horizontal cut-offs.
