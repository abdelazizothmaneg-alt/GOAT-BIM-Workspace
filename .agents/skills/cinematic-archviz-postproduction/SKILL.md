---
name: cinematic-archviz-postproduction
description: Masters cinematic architectural visualization post-production, multi-pass compositing using 32-bit EXR render elements (Cryptomatte, AO, Z-Depth, Raw Light), atmosphere/god-rays, photographic color grading, and entourage integration.
---

# Cinematic ArchViz Post-Production (الإخراج السينمائي للرندرات والمعالجة الرقمية)

A master-level architectural visualization post-processing and digital matte compositing skill. It elevates raw 3D engine renders (3ds Max V-Ray/Corona, Unreal Engine 5, Blender Cycles) into emotionally resonant, cinematic architectural imagery using 32-bit float multi-pass compositing, Cryptomatte object isolation, atmospheric depth grading, and photographic lighting theory.

---

## 1. Multi-Pass Compositing Layer Stack

```
[Photoshop / Affinity / Fusion 32-Bit Linear Compositing Stack]
┌─────────────────────────────────────────────────────────────┐
│ 9. Global Film Emulation LUT & Vignette (Soft Contrast)     │
├─────────────────────────────────────────────────────────────┤
│ 8. Bloom, Glare, Lens Flare & Atmospheric Hazing (Z-Depth)  │
├─────────────────────────────────────────────────────────────┤
│ 7. 2D Entourage (Scale figures, high-res foreground foliage)│
├─────────────────────────────────────────────────────────────┤
│ 6. Cryptomatte Layer (Selective saturation & color balance) │
├─────────────────────────────────────────────────────────────┤
│ 5. Refraction Pass (Screen / Add mode - Boost glass clarity)│
├─────────────────────────────────────────────────────────────┤
│ 4. Reflection Pass (Screen / Add mode - Wet stone gleam)    │
├─────────────────────────────────────────────────────────────┤
│ 3. Ambient Occlusion (Multiply mode @ 35% opacity)          │
├─────────────────────────────────────────────────────────────┤
│ 2. Direct / Raw Light Passes (LightMix adjustment)          │
├─────────────────────────────────────────────────────────────┤
│ 1. Raw Beauty Pass (32-bit EXR Base)                        │
└─────────────────────────────────────────────────────────────┘
```

---

## 2. Multi-Pass Automation with Python & OpenCV

```python
import cv2
import numpy as np

def composite_archviz_passes(beauty_path: str, ao_path: str, zdepth_path: str, output_path: str):
    """
    Composites 3D render passes to ground shadows and generate atmospheric depth haze.
    """
    # Load 16-bit or 32-bit images
    beauty = cv2.imread(beauty_path, cv2.IMREAD_UNCHANGED).astype(np.float32) / 255.0
    ao = cv2.imread(ao_path, cv2.IMREAD_GRAYSCALE).astype(np.float32) / 255.0
    zdepth = cv2.imread(zdepth_path, cv2.IMREAD_GRAYSCALE).astype(np.float32) / 255.0

    # 1. Blend Ambient Occlusion (Multiply blend mode) to ground contact edges
    ao_3ch = cv2.merge([ao, ao, ao])
    ao_factor = 0.40 # 40% AO strength
    grounded = beauty * ((1.0 - ao_factor) + (ao_3ch * ao_factor))

    # 2. Add Atmospheric Depth Haze using normalized Z-Depth
    # Distant pixels become slightly cooler and hazier (Rayleigh scattering)
    sky_haze_color = np.array([0.75, 0.70, 0.65]) # Soft warm atmospheric tint
    haze_intensity = 0.25
    depth_factor = cv2.merge([zdepth, zdepth, zdepth]) * haze_intensity
    
    cinematic_result = grounded * (1.0 - depth_factor) + (sky_haze_color * depth_factor)

    # 3. Tone-mapping & 8-bit export
    final_output = np.clip(cinematic_result * 255.0, 0, 255).astype(np.uint8)
    cv2.imwrite(output_path, final_output)
    return output_path
```

---

## 3. Photographic Composition Rules for Architects
1. **Vertical Lens Shift (Two-Point Perspective)**: Architectural verticals must never converge unless deliberately taking a dizzying worm's-eye shot of a supertall skyscraper.
2. **Color Temperature Contrast**: Balance warm interior artificial lights ($2700 - 3000\text{ K}$) with cool dusk ambient skylight ($6500 - 8000\text{ K}$) to create emotional tension and depth.
3. **Contact Shadows (Ambient Occlusion)**: Without ambient occlusion passes, furniture and building bases appear to "float" above floor slabs. Multiply AO tightly around contact vertices.
