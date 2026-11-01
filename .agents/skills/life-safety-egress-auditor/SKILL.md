---
name: life-safety-egress-auditor
description: Audits architectural building designs for life safety and emergency evacuation compliance with IBC Chapter 10, NFPA 101, and SBC 801, calculating travel distances, exit separation rules, and corridor capacities.
---

# Life Safety Egress Auditor (تدقيق السلامة الحياتية ومسارات الهروب)

A deterministic building safety and regulatory compliance auditing skill based on the **International Building Code (IBC Chapter 10)**, **NFPA 101 (Life Safety Code)**, and the **Saudi Building Code (SBC 801 Fire Protection)**. It mathematically audits Exit Access Travel Distances, Common Paths of Egress, Exit Diagonal Separation ratios, and door/stairway capacity widths directly from Autodesk Revit models and spatial boundary networks.

---

## 1. Life Safety Code Compliance Matrix

```
[Architectural Floor Plan / Revit Model]
                   │
                   ▼
  [1. Occupancy Classification & Occupant Load Math]
  - Determine Occupancy Group (Business B, Assembly A, Residential R)
  - Occupant Load = Net/Gross Floor Area / Occupant Load Factor
                   │
                   ▼
  [2. Egress Path & Distance Verification]
  ┌────────────────────────────────────────────────────────┐
  │ Check 1: Exit Access Travel Distance                   │
  │ - Sprinklered: Max <= 76m (250 ft)                     │
  │ - Non-Sprinklered: Max <= 61m (200 ft)                 │
  ├────────────────────────────────────────────────────────┤
  │ Check 2: Common Path of Egress Travel                  │
  │ - Single exit travel limit before 2nd choice: <= 23m   │
  ├────────────────────────────────────────────────────────┤
  │ Check 3: Exit Diagonal Separation Rule                 │
  │ - Minimum separation >= 1/2 of overall space diagonal  │
  │ - Reduced to 1/3 diagonal for fully sprinklered bldgs  │
  ├────────────────────────────────────────────────────────┤
  │ Check 4: Capacity Widths                               │
  │ - Level corridors: 5.1 mm (0.2 in) per occupant        │
  │ - Stairways: 7.6 mm (0.3 in) per occupant              │
  └────────────────────────────────────────────────────────┘
                   │
                   ▼
  [3. Automated Discrepancy Reporting & Revit Color Coding]
  - Highlight non-compliant rooms with Red view filter overrides
  - Generate Life Safety Code Compliance Matrix for Municipal Approval
```

---

## 2. Occupancy Load Factors (IBC Table 1004.5 / SBC 801)

| Occupancy Function | Floor Area Factor ($m^2/\text{person}$) | Egress Width Factor (Doors) |
| :--- | :--- | :--- |
| **Business / Offices (B)** | $14.0\text{ m}^2$ Gross ($150\text{ sq.ft}$) | $5.1\text{ mm}$ ($0.2\text{ in}$) per person |
| **Assembly, Unconcentrated (A-3)**| $1.4\text{ m}^2$ Net ($15\text{ sq.ft}$) | $5.1\text{ mm}$ ($0.2\text{ in}$) per person |
| **Educational Classrooms (E)** | $1.9\text{ m}^2$ Net ($20\text{ sq.ft}$) | $5.1\text{ mm}$ ($0.2\text{ in}$) per person |
| **Storage / Mechanical (S)** | $28.0\text{ m}^2$ Gross ($300\text{ sq.ft}$) | $5.1\text{ mm}$ ($0.2\text{ in}$) per person |

---

## 3. Automated Diagonal Separation Check (Python Recipe)

```python
import numpy as np

def audit_exit_separation(room_corners: list, exit_door_1_pos: list, exit_door_2_pos: list, is_sprinklered: bool = True):
    """
    Evaluates the 1/2 or 1/3 Diagonal Exit Separation rule (IBC Section 1007.1.1).
    """
    pts = np.array(room_corners)
    # Calculate overall maximum diagonal dimension of the space
    max_diagonal = 0.0
    for i in range(len(pts)):
        for j in range(i + 1, len(pts)):
            dist = np.linalg.norm(pts[i] - pts[j])
            if dist > max_diagonal:
                max_diagonal = dist

    # Calculate actual distance between the two exit doors
    door1 = np.array(exit_door_1_pos)
    door2 = np.array(exit_door_2_pos)
    exit_distance = np.linalg.norm(door1 - door2)

    required_ratio = 1.0 / 3.0 if is_sprinklered else 0.5
    min_required_separation = max_diagonal * required_ratio
    is_compliant = exit_distance >= min_required_separation

    return {
        "overall_room_diagonal_m": round(max_diagonal, 2),
        "actual_exit_separation_m": round(exit_distance, 2),
        "min_required_separation_m": round(min_required_separation, 2),
        "is_compliant": is_compliant,
        "code_rule": "1/3 Diagonal Rule (Sprinklered)" if is_sprinklered else "1/2 Diagonal Rule (Non-Sprinklered)",
        "violation_severity": "PASS" if is_compliant else "CRITICAL_LIFE_SAFETY_VIOLATION"
    }
```

---

## 4. Minimum Clear Width Thresholds
- **Corridors**: Minimum clear width $1120\text{ mm}$ ($44\text{ in}$) serving $> 50$ persons; $1830\text{ mm}$ ($72\text{ in}$) in hospitals/healthcare.
- **Doors**: Minimum clear opening $815\text{ mm}$ ($32\text{ in}$); maximum leaf width $1220\text{ mm}$ ($48\text{ in}$).
- **Stairways**: Minimum clear width $1120\text{ mm}$ ($44\text{ in}$) serving $> 50$ persons.
