---
name: archviz-cinematic-color-grading-aces
description: Implements ACEScg color science, analog film stock emulation (Kodak 2383), highlight roll-off curves, split-toning, and subtle film grain for cinematic architectural imagery.
---

# ArchViz Cinematic Color Grading & ACES Film Look

Master color grading, color science, and cinematic look development skill for architectural visualization. Implements the ACEScg color space pipeline, analog film curve emulation, highlight roll-off control, and split-toning.

---

## 1. The Color Science Paradigm: sRGB vs. ACEScg

```
Standard 8-Bit sRGB Pipeline (Outdated):       Cinematic 32-Bit ACEScg Pipeline:
├── Narrow gamut (clips saturated colors)      ├── Ultra-wide color gamut (encompasses full human vision)
├── Hard white clipping at value 1.0 (burned)  ├── Smooth logarithmic highlight compression (roll-off)
└── Linear curves feel harsh and "3D"          ├── Rich film stock response (S-curve with deep, soft blacks)
```

### The Magic of Highlight Roll-Off:
In physical film and high-end cinema cameras (ARRI Alexa), bright light sources (the sun, a light bulb) never hit an abrupt digital ceiling. The luminance softly decelerates into a creamy, organic white without harsh pixel burn.

---

## 2. Cinematic Color Harmony: The Warm-Teal Split

A cornerstone of cinematic architecture:
- **Highlights & Midtones (Push Warm / Gold)**:
  - Add warm yellow/amber ($+5\text{ to }+8^\circ$) into highlights.
  - Interior architectural fixtures, wood textures, and sunlit stone glow with warmth.
- **Shadows & Ambient Occlusion (Push Cool / Slate-Teal)**:
  - Tint low shadows with subtle teal/slate blue ($190^\circ - 215^\circ$, $4\% - 8\%$ saturation).
  - Creates depth and separation between the building and the atmosphere.

---

## 3. Film Stock Emulation: Kodak 2383 Print Profile

To give 3D renders the weight, texture, and emotional authenticity of a real photograph:
1. **The S-Curve Transfer Function**:
   - Deepen shadows slightly while lifting the absolute black point by $2\% - 3\%$ (crushed blacks look digital; lifted blacks look filmic).
2. **Subtle Lens Halation**:
   - Soft red/orange diffusion halo bleeding around high-contrast edges (e.g., bright window mullions or exposed neon signage).
3. **Organic Film Grain**:
   - Add $2\% - 4\%$ monochromatic 35mm fine grain (Gaussian, overlay mode at $50\%$ opacity) to break digital plastic smoothness.

---

## 4. Python ACES LUT & Film Curve Simulator

```python
"""
Film Curve & Highlight Roll-Off Simulator
Reinhard & Filmic Tone Curve for 32-bit Float EXR
"""

import math

def apply_filmic_highlight_rolloff(luminance_float: float, shoulder_strength: float = 0.22, linear_strength: float = 0.30) -> float:
    """
    Applies cinematic filmic tonemapping curve (Hable/Uncharted 2 curve).
    Prevents blown-out white clamping while preserving dynamic contrast.
    """
    x = max(luminance_float, 0.0)
    a = shoulder_strength
    b = linear_strength
    c = 0.10
    d = 0.20
    e = 0.01
    f = 0.30
    
    # Filmic function: ((x*(a*x+c*b)+d*e)/(x*(a*x+b)+d*f)) - e/f
    numerator = x * (a * x + c * b) + d * e
    denominator = x * (a * x + b) + d * f
    return round((numerator / denominator) - (e / f), 3)

if __name__ == "__main__":
    # Test values from deep shadow (0.05) to super-bright light source (8.5)
    test_luminances = [0.05, 0.5, 1.0, 2.5, 8.5]
    print("Filmic Highlight Roll-off Results:")
    for val in test_luminances:
        mapped = apply_filmic_highlight_rolloff(val)
        print(f"  Input Float: {val:<5} -> Filmic Mapped: {mapped}")
```

---

## 5. Deliverables Checklist
1. **ACEScg Color Space Verification**: Confirm render output is saved as 32-bit float OpenEXR tagged with ACEScg color primaries.
2. **Histogram Dynamic Range Check**: Zero clipped pure white pixels ($RGB = 255, 255, 255$) in sky or sun spots.
3. **Film Emulation Grade**: Delivered `.cube` 3D LUT file ready for client review and video walkthrough grading.
