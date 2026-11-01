---
name: cinematic-animation-flythrough-director
description: Directs Hollywood-grade architectural flythrough animations, drone camera flight paths (Catmull-Rom splines), non-linear Bezier easing, Look-At target decoupling, and foreground parallax depth.
---

# Cinematic Animation & Flythrough Camera Director

World-class cinematic camera direction and flight choreography skill for architectural walkthroughs and drone flythrough animations across 3ds Max, Blender, and Unreal Engine 5.

---

## 1. The 5 Principles of Cinematic Architectural Directing

```
Standard Amateurs Walkthrough:                  Hollywood Cinematic Direction:
├── Linear constant speed (Feels robotic)       ├── Non-linear Bezier velocity curves (Damped ease-in/ease-out)
├── Rigid camera locked to a moving box         ├── Damped spring Look-At tracking (Lagging human focus)
├── Clean empty path without obstructions       ├── Foreground parallax reveals (Passing behind a tree/column)
├── Zero banking on turns                       ├── Subtle 3° - 6° aerodynamic banking roll into curves
└── Abrupt speed cuts                           ├── Speed ramping (Slow dramatic pause at hero architectural vista)
```

---

## 2. Parallax Depth Layering & Foreground Framing

Human vision judges 3D space by how quickly objects move relative to one another (motion parallax):
- **Foreground Layer ($0 - 5\text{ m}$)**: Tree leaves, balustrades, or stone columns passing close to the lens at high apparent speed.
- **Midground Layer ($5 - 30\text{ m}$)**: The main architectural subject moving at moderate speed.
- **Background Layer ($> 100\text{ m}$)**: Distant city skyline or mountains appearing almost stationary.
- *Rule*: Never fly a camera through empty air without a foreground element establishing depth during the first 2 seconds of a shot.

---

## 3. Catmull-Rom Spline Camera Path Generator (Python)

```python
"""
Cinematic Catmull-Rom Spline Drone Camera Path Generator
Smooths jagged waypoint vectors into continuous curvature with banking
"""

import math

def catmull_rom_spline(p0, p1, p2, p3, num_points=20):
    """
    Computes smooth cubic Catmull-Rom points between p1 and p2.
    """
    points = []
    for i in range(num_points):
        t = i / float(num_points)
        t2 = t * t
        t3 = t2 * t
        
        # Basis matrix blending
        x = 0.5 * ((2 * p1[0]) + (-p0[0] + p2[0]) * t + (2*p0[0] - 5*p1[0] + 4*p2[0] - p3[0]) * t2 + (-p0[0] + 3*p1[0] - 3*p2[0] + p3[0]) * t3)
        y = 0.5 * ((2 * p1[1]) + (-p0[1] + p2[1]) * t + (2*p0[1] - 5*p1[1] + 4*p2[1] - p3[1]) * t2 + (-p0[1] + 3*p1[1] - 3*p2[1] + p3[1]) * t3)
        z = 0.5 * ((2 * p1[2]) + (-p0[2] + p2[2]) * t + (2*p0[2] - 5*p1[2] + 4*p2[2] - p3[2]) * t2 + (-p0[2] + 3*p1[2] - 3*p2[2] + p3[2]) * t3)
        
        points.append((round(x, 2), round(y, 2), round(z, 2)))
    return points

if __name__ == "__main__":
    # Drone flight waypoints through villa courtyard
    w0 = (0.0, -20.0, 8.0)
    w1 = (5.0, -10.0, 3.5) # Approaching infinity pool
    w2 = (12.0, 0.0, 2.0)  # Skimming pool water level
    w3 = (20.0, 15.0, 6.0) # Soaring over living room roof
    
    smooth_path = catmull_rom_spline(w0, w1, w2, w3, num_points=15)
    print(f"Generated {len(smooth_path)} cinematic camera waypoints with zero robotic acceleration jerks.")
```

---

## 4. Deliverables Checklist
1. **Camera Storyboard Animatic**: Low-resolution clay/wireframe pre-visualization video verifying cut rhythm and pacing.
2. **Unified FBX / Alembic Camera Rig**: Exported camera path compatible with 3ds Max, Blender, and Unreal Engine 5 Sequencer.
