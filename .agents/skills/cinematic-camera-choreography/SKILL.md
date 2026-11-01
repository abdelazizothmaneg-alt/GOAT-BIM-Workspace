---
name: cinematic-camera-choreography
description: Designs cinematic camera movements, physical rig behaviors (Cranes, Dollies, Drones), non-linear easing curves (Bezier velocity damping), Look-At target decoupling, and parallax pacing for architectural walkthroughs and flythrough animations.
---

# Cinematic Camera Choreography (تصميم وتوجيه حركة الكاميرا السينمائية للرندرات)

A cinematic directing and camera motion skill tailored for architectural flythroughs and walkthrough animations. It eliminates artificial, robotic camera motions by enforcing physical cinematography rigs (Steadicam, Jib crane, Cablecam, Drone), implementing Bezier easing curves with inertial velocity damping, decoupling navigation paths from focal points, and orchestrating parallax layers to convey spatial depth.

---

## 1. Principles of Architectural Camera Movement

```
┌─────────────────────────────────┬─────────────────────────────────┐
│ 1. Parallax Staging             │ 2. Dual-Spline Decoupling       │
│ (Foreground elements shift      │ (Camera Translation Spline is   │
│  against background facade)     │  separated from Look-At Target) │
├─────────────────────────────────┼─────────────────────────────────┤
│ 3. Bezier Velocity Easing       │ 4. Anthropomorphic Scale        │
│ (Soft Ease-In & Ease-Out S-Curve│ (Eye-level 1.60m - 1.65m for    │
│  eliminates abrupt starts/stops)│  all human-scale walkthroughs)  │
└─────────────────────────────────┴─────────────────────────────────┘
```

---

## 2. 3ds Max Camera Path Rig Automation (MAXScript)

This script builds a decoupled camera rig with smooth Bezier easing along a trajectory:

```maxscript
fn createCinematicCameraRig camName pathSpline targetSpline totalFrames =
(
    -- 1. Create Physical Camera
    local physCam = Physical_Camera name:camName
    physCam.focal_length = 24.0 -- Wide architectural focal length
    physCam.sensor_width = 36.0 -- Full-frame sensor
    
    -- 2. Attach Position Controller (Path Constraint)
    local posConstraint = Path_Constraint()
    posConstraint.appendTarget pathSpline 100.0
    posConstraint.follow = false
    physCam.pos.controller = posConstraint
    
    -- 3. Attach Target to Secondary Spline or Point Helper
    if targetSpline != undefined then
    (
        local lookTarget = Point pos:[0,0,1600] name:(camName + "_Target")
        local targetConstraint = Path_Constraint()
        targetConstraint.appendTarget targetSpline 100.0
        lookTarget.pos.controller = targetConstraint
        physCam.target = lookTarget
    )
    
    -- 4. Set Bezier Velocity Easing on Keys (Smooth S-Curve)
    setRetimeToolMode false
    animate on
    (
        at time 0f posConstraint.percent = 0.0
        at time totalFrames posConstraint.percent = 100.0
    )
    
    -- Smooth tangent handles in Track View
    local keys = posConstraint.percent.controller.keys
    for k in keys do (
        setKeyInTangentType posConstraint.percent.controller 1 #bezier
        setKeyOutTangentType posConstraint.percent.controller 1 #bezier
    )
    
    format "Cinematic Camera '%' configured over % frames.\n" camName totalFrames
    return physCam
)
```

---

## 3. Pacing & Timing Rules
- **Establishing Shots (Aerial / Wide Facade)**: Minimum $8 - 12\text{ seconds}$ duration to let the viewer register the landscape, sun angle, and massing hierarchy.
- **Transitional Thresholds (Entering Doors / Atriums)**: Decelerate the camera slightly ($20\%$ slower) as the lens crosses from exterior into interior to emphasize spatial compression and release.
- **Focal Length Guidelines**:
  - Exterior Bird's-Eye & Hero Facades: $28 - 35\text{ mm}$ (minimizes edge stretching).
  - Interior Master Suites / Living: $20 - 24\text{ mm}$ (captures ceiling and floor details).
  - Tectonic & Material Close-ups: $50 - 85\text{ mm}$ with shallow Depth of Field ($f/2.8$).
