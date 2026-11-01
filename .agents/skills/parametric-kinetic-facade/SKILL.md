---
name: parametric-kinetic-facade
description: Designs and automates adaptive kinetic building facades, solar-tracking louvers, and biomimetic shading screens using Grasshopper algorithms and native Revit Adaptive Components.
---

# Parametric Kinetic Facade (الواجهات الحركية والبارامترية الذكية)

A computational facade engineering and generative building envelope skill. It models, simulates, and programs adaptive kinetic facades, responsive solar-tracking louvers, and origami/biomimetic folding shading screens using **Grasshopper** and native **Autodesk Revit Adaptive Components**.

---

## 1. Kinetic Shading Architecture

```
[Real-Time Solar Azimuth & Altitude Coordinates]
                         │
                         ▼
   [1. Angle of Incidence (AOI) Calculation]
   - For each facade panel i with normal vector n_i:
     AOI_i = arccos(n_i . sun_vector)
                         │
                         ▼
   [2. Kinetic Actuation Function (Rotation / Translation)]
   - Direct Sunlight (AOI < 45°): Kinetic panel closes to 85% opacity
   - Diffuse / Shaded (AOI > 90°): Kinetic panel opens to 100% daylight view
                         │
                         ▼
   [3. Mechanical Hinge & Joint Geometric Detailing]
   - Scissor-hinge linkages, rotational torque limits, motor zones
                         │
                         ▼
   [4. Revit API Streaming (Rhino.Inside.Revit)]
   - Drive shared parameter "Opening_Angle" across 1,000+ Adaptive Panels
```

---

## 2. Kinetic Solar Tracking Algorithm (Python in Grasshopper)

```python
import math

def compute_kinetic_panel_angles(panel_normals: list, sun_azimuth_deg: float, sun_altitude_deg: float):
    """
    Computes rotation angles for adaptive louvers based on solar position.
    """
    # Convert solar spherical coordinates to Cartesian vector
    az_rad = math.radians(sun_azimuth_deg)
    alt_rad = math.radians(sun_altitude_deg)

    sun_x = math.cos(alt_rad) * math.sin(az_rad)
    sun_y = math.cos(alt_rad) * math.cos(az_rad)
    sun_z = math.sin(alt_rad)
    sun_vec = [sun_x, sun_y, sun_z]

    angles = []
    for norm in panel_normals:
        # Dot product: cos(theta) = n . s
        dot = norm[0] * sun_vec[0] + norm[1] * sun_vec[1] + norm[2] * sun_vec[2]
        dot = max(-1.0, min(1.0, dot))
        incidence_deg = math.degrees(math.acos(dot))

        # Actuation mapping: If direct sun, close louver (rotation = 75 deg)
        if incidence_deg < 60.0 and sun_z > 0.0:
            actuation_angle = 75.0 * (1.0 - (incidence_deg / 60.0))
        else:
            actuation_angle = 0.0 # Fully open for daylight and views

        angles.append(round(actuation_angle, 1))

    return angles
```

---

## 3. Revit Adaptive Component Parameter Driving (.NET C#)

```csharp
using Autodesk.Revit.DB;

namespace FacadeAutomation
{
    public static class KineticDriver
    {
        public static void UpdateLouverAngle(FamilyInstance adaptivePanel, double angleDegrees)
        {
            Parameter angleParam = adaptivePanel.LookupParameter("Louver_Angle");
            if (angleParam != null && !angleParam.IsReadOnly)
            {
                // Revit internal angle unit is Radians
                angleParam.Set(angleDegrees * (Math.PI / 180.0));
            }
        }
    }
}
```

---

## 4. Best Practices for Kinetic Building Envelopes
1. **Actuator Grouping**: Group kinetic panels into physical actuator zones (e.g., $10\text{ panels per motor}$) to minimize electrical cost and maintenance failure points.
2. **Wind Safety Lock**: Program an automatic $0^\circ$ flat safety lock when ambient wind gusts exceed $20\text{ m/s}$ (Beaufort 8).
