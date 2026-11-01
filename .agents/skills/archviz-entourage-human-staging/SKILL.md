---
name: archviz-entourage-human-staging
description: Stages and integrates human entourage with cinematic realism, matching light direction, black levels, contact shadows, and authentic motion blur to eliminate the stiff 3D video game NPC look.
---

# ArchViz Entourage & Human Staging

Specialized entourage placement and post-production skill for populating architectural visualization plates with authentic, believable human life, dynamic motion blur, and seamless ground integration.

---

## 1. The 4 Fatal Flaws of 3D People (And How to Fix Them)

```
Fatal Flaw 1: "Floating People"          ──> Fix: Paint multi-layered contact shadows directly under shoes.
Fatal Flaw 2: "Wrong Light Angle"        ──> Fix: Audit sunlight azimuth; discard cutouts lit from opposing sides.
Fatal Flaw 3: "Black Level Mismatch"     ──> Fix: Sample scene darkest shadow and clip cutout black point to match.
Fatal Flaw 4: "Stiff Mannequin Look"     ──> Fix: Introduce natural motion blur on limbs (1/25s to 1/50s shutter).
```

---

## 2. Technical Integration Protocol (Photoshop / Compositing)

### Step 1: Eye-Level Perspective Matching
- All pedestrians standing on a flat grade must have their eyes aligned along the **camera's true horizon line**, regardless of whether they are $3\text{ m}$ away or $50\text{ m}$ away.

### Step 2: Contact & Cast Shadow Architecture
Every person requires two distinct shadow components:
1. **The Ambient Occlusion (Contact) Shadow**:
   - Razor-thin, pitch-black shadow directly beneath the shoe sole where it touches the ground. Prevents the "hovering" illusion.
2. **The Cast Shadow**:
   - Drawn along the exact sun ray vector matching building shadows.
   - Feathered softness increases with distance from the feet (penumbra effect).

### Step 3: Color Temperature & Luminance Matching
```
Scene Sunlit Side: 5000K - 5500K Sunlight ──> Match warm highlights on cutout shoulders/head
Scene Shadow Side: 7000K - 8500K Sky Fill ──> Tint cutout ambient shadows with subtle blue/slate
```

---

## 3. Motion Blur & Realism Script (Photoshop / Python)

```python
"""
Photoshop / Compositing Entourage Motion Blur Calculator
Computes directional blur pixels based on pedestrian velocity and focal length
"""

import math

def calculate_motion_blur_pixels(pedestrian_speed_kmh: float, 
                                 distance_to_camera_m: float, 
                                 shutter_speed_fraction: float, 
                                 focal_length_mm: float, 
                                 image_width_px: int = 3840) -> float:
    """
    Computes required motion blur filter radius in pixels.
    pedestrian_speed_kmh: 4.5 km/h is average human walk; 8.0 km/h is brisk walk/jog.
    shutter_speed_fraction: 1/30 (0.033s) or 1/60 (0.016s).
    """
    speed_m_s = pedestrian_speed_kmh / 3.6
    displacement_m = speed_m_s * shutter_speed_fraction
    
    # 35mm full frame sensor width = 36mm
    sensor_width_mm = 36.0
    field_of_view_m = (sensor_width_mm * distance_to_camera_m) / focal_length_mm
    
    # Pixel blur radius
    blur_px = (displacement_m / field_of_view_m) * image_width_px
    return round(blur_px, 1)

if __name__ == "__main__":
    # Person walking at 5 km/h, 8 meters from camera, shot at 1/30s shutter on 28mm lens
    blur = calculate_motion_blur_pixels(5.0, distance_to_camera_m=8.0, shutter_speed_fraction=1.0/30.0, focal_length_mm=28.0)
    print(f"Apply Directional Motion Blur: {blur} pixels along movement vector.")
```

---

## 4. Deliverables Checklist
1. **Horizon Alignment Verification**: Confirm all standing adult figures cross the horizon line at eye level ($1.5 - 1.6\text{ m}$).
2. **Contact Shadow Audit**: 100% zoom inspection verifying contact occlusion under every shoe sole.
3. **Cultural & Contextual Authenticity**: Entourage wardrobe and demographics matched strictly to project regional identity (e.g. GCC/Saudi thobe and abaya attire for Gulf projects).
