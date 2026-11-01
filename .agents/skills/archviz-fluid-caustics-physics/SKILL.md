---
name: archviz-fluid-caustics-physics
description: Simulates physical water dynamics, photon pool caustics, infinity edge spillway overflow, and wind-blown wave displacement for photorealistic architectural visualization.
---

# ArchViz Fluid Dynamics & Light Caustics

Advanced architectural water and fluid physics skill. Specializes in true photon caustics, infinity edge pool overflow spillways, Gerstner wave surface displacement, and underwater volumetric absorption in Chaos Corona, V-Ray, and Cycles.

---

## 1. Physical Water Optics & Caustics Physics

```
Direct Sunlight Ray ──> [ Uneven Wave Crest (Curved Lens) ] ──> Focuses Photons into High-Intensity Bands
                                                                (Dynamic "Caustic Web" on pool tiles & walls)
```

### Optical Parameters of Architectural Water:
- **Index of Refraction (IOR)**: Exactly **$1.333$** ($20^\circ\text{C}$ pure water).
- **Abbe Dispersion Number**: $55.0$ (Simulates subtle prismatic rainbow splitting at sharp wave edges).
- **Volumetric Absorption & Scatter**:
  - Pure water does not absorb all colors equally; red wavelengths are absorbed first.
  - Set absorption color to pale cyan/turquoise (`#14b8a6`) with distance $= 4.0 - 8.0\text{ m}$ to create natural depth darkening.

---

## 2. Corona Caustics Solver Configuration (MAXScript)

In Chaos Corona, fast physical photon caustics are enabled via the dedicated Caustics Solver:

```maxscript
fn configurePoolCaustics waterMesh:undefined poolLight:undefined =
(
    -- 1. Enable Global Caustics Solver
    renderers.current.caustics_enable = true
    
    -- 2. Configure Dedicated Pool Water Material
    local waterMtl = CoronaPhysicalMtl()
    waterMtl.name = "Phys_PoolWater"
    waterMtl.metalnessMode = 0
    waterMtl.baseIor = 1.333
    waterMtl.baseRoughness = 0.01 -- Extremely smooth specular surface
    waterMtl.refractionAmount = 1.0
    
    -- Volumetric Absorption (Deep Mediterranean Turquoise)
    waterMtl.attenuationColor = color 180 230 240
    waterMtl.attenuationDistance = 5000.0 -- 5 meters
    
    -- Enable Caustics on Material
    waterMtl.caustics_generate = true
    
    if waterMesh != undefined do
    (
        waterMesh.material = waterMtl
    )
    format "Corona Physical Caustics configured successfully.\n"
)
```

---

## 3. Infinity Pool Spillway & Gerstner Wave Vectors

### A. The Infinity Edge Knife-Edge Detail
- The pool coping wall must slope outward at $15^\circ - 30^\circ$ away from the water surface, with a razor-thin top edge ($5\text{ mm}$ radius) allowing a uniform $3\text{ mm}$ sheet of water to spill over into the concealed perimeter gutter.

### B. Gerstner Wave Surface Formula:
$$z = \sum_{i=1}^{n} A_i \cdot \cos\left( k_i \cdot (x \cdot \cos\theta_i + y \cdot \sin\theta_i) - \omega_i \cdot t \right)$$
- Combines $3 - 5$ wave vectors of varying wavelengths ($1.5\text{ m}$ gentle swell $+ 0.2\text{ m}$ wind ripples) to create natural, non-repeating swimming pool surface turbulence.

---

## 4. Deliverables Checklist
1. **Caustics Beauty Pass**: Isolated reflection/refraction caustic pass confirming absence of noise fireflies.
2. **Water Surface Displacement Shader**: 4K 32-bit floating point EXR vector displacement map configured for pool mesh tessellation.
