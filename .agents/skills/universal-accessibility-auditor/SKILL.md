---
name: universal-accessibility-auditor
description: Audits architectural BIM models for barrier-free universal accessibility compliance with ADA Standards 2010, ICC A117.1, ISO 21542, and Saudi Universal Accessibility codes, verifying door clearances, ramp slopes, turning spaces, and accessible restrooms.
---

# Universal Accessibility Auditor (تدقيق إمكانية الوصول الشامل لذوي الإعاقة)

An architectural auditing and barrier-free design compliance skill based on the **Americans with Disabilities Act (ADA Standards 2010)**, **ICC A117.1 (Accessible and Usable Buildings)**, **ISO 21542 (Building Construction - Accessibility)**, and the **Saudi Universal Accessibility Guidelines**. It verifies door opening clearances, wheelchair maneuvering spaces, ramp slope ratios, grab bar ergonomics, and accessible sanitary provisions directly from CAD/BIM elements.

---

## 1. Universal Design Audit Checklist

```
┌─────────────────────────────────────────────────────────────┐
│ 1. Accessible Routes & Corridors (ADA Chapter 4)            │
│ - Continuous clear corridor width >= 915mm (36 in)          │
│ - Passing spaces >= 1525 x 1525mm every 60 meters           │
├─────────────────────────────────────────────────────────────┤
│ 2. Doors & Entrances (ADA Section 404)                      │
│ - Minimum clear opening width >= 815mm (32 in)              │
│ - Maneuvering clearances (Latch-side pull clearance >= 455mm│
│ - Maximum door operating force <= 22.2 N (5 lbs)            │
├─────────────────────────────────────────────────────────────┤
│ 3. Ramps & Vertical Circulation (ADA Section 405)           │
│ - Maximum ramp slope ratio 1:12 (8.33%)                     │
│ - Maximum rise per single ramp run <= 760mm (30 in)         │
│ - Level landings at top & bottom >= 1525mm long             │
├─────────────────────────────────────────────────────────────┤
│ 4. Accessible Water Closets & Restrooms (ADA Chapter 6)     │
│ - Centerline of toilet: 405 - 455mm (16 - 18 in) from wall  │
│ - Toilet seat height: 430 - 485mm (17 - 19 in) above floor  │
│ - Grab bar heights: 840 - 915mm (33 - 36 in) above floor    │
│ - Clear turning circle >= 1525mm (60 in) diameter           │
└─────────────────────────────────────────────────────────────┘
```

---

## 2. Automated Door & Ramp Audit Engine (Python)

```python
def audit_door_accessibility(door_mark: str, clear_width_mm: float, latch_clearance_mm: float):
    """
    Validates door against ADA Section 404.2.3 and ISO 21542.
    """
    MIN_CLEAR_WIDTH = 815.0 # 32 inches
    MIN_LATCH_PULL = 455.0  # 18 inches
    
    width_pass = clear_width_mm >= MIN_CLEAR_WIDTH
    latch_pass = latch_clearance_mm >= MIN_LATCH_PULL

    issues = []
    if not width_pass:
        issues.append(f"Clear opening {clear_width_mm}mm < minimum {MIN_CLEAR_WIDTH}mm requirement.")
    if not latch_pass:
        issues.append(f"Latch-side maneuvering clearance {latch_clearance_mm}mm < minimum {MIN_LATCH_PULL}mm.")

    return {
        "door_id": door_mark,
        "is_accessible": width_pass and latch_pass,
        "findings": issues,
        "compliance_status": "COMPLIANT" if (width_pass and latch_pass) else "NON_COMPLIANT"
    }

def audit_ramp_slope(ramp_id: str, total_run_length_m: float, vertical_rise_m: float):
    """
    Validates ramp slope against 1:12 maximum threshold (ADA Section 405.2).
    """
    slope_ratio = vertical_rise_m / total_run_length_m
    max_allowable_slope = 1.0 / 12.0 # 0.0833 (8.33%)

    is_slope_valid = slope_ratio <= (max_allowable_slope + 0.001)
    is_rise_valid = vertical_rise_m <= 0.760 # Max 760mm between landings

    return {
        "ramp_id": ramp_id,
        "calculated_slope": f"1:{round(1.0/slope_ratio, 1)}",
        "is_slope_compliant": is_slope_valid,
        "requires_intermediate_landing": not is_rise_valid,
        "status": "PASS" if (is_slope_valid and is_rise_valid) else "FAIL"
    }
```

---

## 3. Saudi Universal Accessibility Alignment
- Ensure all braille signage is located between $1200\text{ mm}$ and $1500\text{ mm}$ above finished floor level.
- Mandatory tactical paving (Warning studs & Directional bars) at all stair tops and curb drop-offs.
