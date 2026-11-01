---
name: archviz-matte-painting-photomontage
description: Seamlessly blends 3D architectural renders into real-world drone and site survey photography using perspective matching, aerial haze blending, and matte painting techniques.
---

# ArchViz Matte Painting & Drone Photomontage

High-end post-production and digital matte painting skill for embedding 3D building models seamlessly into aerial drone plates, helicopter survey footage, and ground-level site photography.

---

## 1. Camera Perspective Matching (Photogrammetry Alignment)

To place a 3D building into a real-world photo without perspective distortion:

```
Real-World Drone Plate (EXIF Data: 24mm, Altitude: 45m, Pitch: -22°)
                       │
                       ▼
[ Camera Match / Perspective Calibration ] ──> Match 3 vanishing points to street grid
                       │
                       ▼
3D Virtual Camera Locked to Identical Sensor, Focal Length & Tilt
```

### Critical Photomontage Alignment Rules:
1. **Horizon Line Coincidence**: The virtual 3D horizon must overlay the physical photograph's horizon line with zero pixel divergence.
2. **Sun Altitude & Azimuth Sync**: Check shadow angles of existing trees or light poles in the photo; align the 3D Sun light to cast shadows in the exact same vector.
3. **Lens Distortion Correction**: Remove optical barrel/pincushion distortion from the drone photo *before* camera matching, then reapply identical distortion to the composite plate.

---

## 2. Aerial Perspective & Atmospheric Haze Blending

In aerial and wide landscape views, contrast decreases and blue haze increases with distance:
- Use the **Z-Depth Pass** as a layer mask in Photoshop:
  - Near Foreground ($0 - 50\text{ m}$): $100\%$ contrast, full black point, zero haze.
  - Midground ($50 - 300\text{ m}$): Subtle color temperature cooling ($+5\%$ blue fill).
  - Background ($> 300\text{ m}$): Lift black levels by $15\% - 25\%$, blend with the ambient sky color sampled directly from the drone photo's horizon.

---

## 3. Digital Matte Painting (DMP) Integration Workflow

1. **Foreground Overlap & Edge Feathering**:
   - Never use sharp, laser-cut alpha edges where the building meets existing ground.
   - Paint-in existing foreground grass blades, gravel, and curb edges overlapping the bottom edge of the 3D slab using custom foliage brushes.
2. **Reflected Context in Glazing**:
   - High-end glass cannot reflect empty sky; project a panorama of the actual neighboring buildings onto the 3D curtain wall reflection map.
3. **Ambient Light Spill**:
   - Softly brush an ambient color bleed from the surrounding green trees or desert sand onto the building’s lower perimeter walls.

---

## 4. Python Drone Metadata (EXIF) Extractor for Camera Matching

```python
"""
Drone Photo Metadata Extractor for 3D Camera Matching
Extracts GPS Altitude, Lens Focal Length, and Camera Model
"""

from typing import Dict, Any

def extract_drone_camera_parameters(exif_data: Dict[str, Any]) -> dict:
    focal_length_35mm = exif_data.get("FocalLengthIn35mmFilm", 24.0)
    altitude_m = exif_data.get("GPSAltitude", 50.0)
    drone_model = exif_data.get("Model", "DJI Mavic 3 Enterprise")
    
    # Calculate recommended 3ds Max / Blender camera settings
    return {
        "hardware_camera": drone_model,
        "focal_length_mm": focal_length_35mm,
        "recommended_sensor_width_mm": 36.0, # Full frame equivalent
        "estimated_flight_altitude_m": round(altitude_m, 1),
        "fov_horizontal_deg": round(2 * math.degrees(math.atan(18.0 / focal_length_35mm)), 1)
    }

if __name__ == "__main__":
    import math
    mock_exif = {"FocalLengthIn35mmFilm": 28.0, "GPSAltitude": 78.5, "Model": "DJI Inspire 3"}
    cam_specs = extract_drone_camera_parameters(mock_exif)
    print("Virtual Camera Matching Parameters:")
    for k, v in cam_specs.items():
        print(f"  {k}: {v}")
```

---

## 5. Deliverables Checklist
1. **Camera Match Verification Overlay**: Wireframe 3D grid overlay on the raw drone plate proving road intersections align.
2. **Multi-Layer PSD Master File**: Organized layers: `01_Base_Drone_Plate`, `02_3D_Render_Beauty`, `03_Reflection_Patches`, `04_Foliage_Overpaint`, `05_Global_Color_Grade`.
