---
name: render-compliance-auditor
description: Evaluates architectural renders and AI-generated concept visualizations for structural feasibility, physical plausibility, building code conformance (SBC/IBC clearances, balustrade heights, solar shading), and photographic composition quality.
---

# Render Compliance Auditor (تدقيق الرندرات والتصميمات المعمارية)

An architectural auditing and computer vision critique skill that inspects 3D renders, concept boards, and AI-generated architectural imagery to ensure they meet basic laws of structural physics, building codes (such as Saudi Building Code SBC 201/801), and high photographic standards before client or municipal presentation.

---

## 1. Multi-Tier Audit Matrix

```
[Architectural Render / Concept Image]
                   │
                   ▼
  ┌─────────────────────────────────────────────────────────┐
  │ 1. Structural & Physical Plausibility                   │
  │ - Overextended cantilevers without back-spans or trusses│
  │ - Floating slabs lacking column / shear wall support    │
  │ - Slender columns exceeding buckling slenderness limits │
  │ - Glass curtain walls without intermediate transoms/wind│
  ├─────────────────────────────────────────────────────────┤
  │ 2. Building Code & Regulatory Checks (SBC / IBC)        │
  │ - Balustrade / Parapet minimum heights (>= 1.05m)       │
  │ - Emergency egress / clear stair widths                 │
  │ - Solar exposure & south/west shading logic             │
  │ - Window-to-Wall Ratio (WWR <= 40% for energy codes)    │
  ├─────────────────────────────────────────────────────────┤
  │ 3. Photographic & Rendering Quality Evaluation          │
  │ - 2-Point perspective (vertical lines perfectly upright)│
  │ - Rule of Thirds & focal hierarchy                      │
  │ - Overexposed highlights / underexposed shadow clipping │
  │ - AI artifacts, repeating noise, impossible reflections │
  └─────────────────────────────────────────────────────────┘
```

---

## 2. Common Structural Redlines in AI Visuals

1. **Unrealistic Cantilever Ratios**:
   - For standard reinforced concrete slabs, cantilever span $L_c$ should generally not exceed $\frac{1}{3}$ to $\frac{1}{4}$ of the back-span $L_b$, with minimum depth $h \ge \frac{L_c}{10}$.
   - If an AI render shows an $8\text{ m}$ concrete cantilever with a $200\text{ mm}$ slab and no post-tensioning profile, flag as **Critical Engineering Risk**.
2. **Missing Glass Mullions / Structural Silicone**:
   - Floor-to-ceiling glass exceeding $4.0\text{ m}$ in height requires structural glass fins, steel mullions, or cable-net bracing against wind load ($q_z \approx 1.2 - 1.8\text{ kPa}$).
3. **Floating Staircases**:
   - Cantilevered treads from drywall or light partitions without an embedded steel stringer or reinforced concrete shear wall spine.

---

## 3. Automated Photographic & Perspective Quality Check (Python)

Architectural photography strictly requires true two-point perspective (vertical edges must be parallel to the frame sides without vertical keystoning or converging vanishing points):

```python
import cv2
import numpy as np

def audit_vertical_perspective(image_path: str, max_tilt_degrees: float = 2.0):
    """
    Check if vertical architectural lines tilt significantly (keystoning).
    """
    img = cv2.imread(image_path)
    gray = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)
    edges = cv2.Canny(gray, 50, 150, apertureSize=3)

    lines = cv2.HoughLinesP(edges, 1, np.pi / 180, 100, minLineLength=100, maxLineGap=20)
    if lines is None:
        return {"status": "inconclusive", "reason": "insufficient vertical lines detected"}

    vertical_deviations = []
    for line in lines:
        x1, y1, x2, y2 = line[0]
        dx = x2 - x1
        dy = y2 - y1
        if abs(dy) > abs(dx) * 2.5: # Mostly vertical line
            angle_from_vertical = np.degrees(np.arctan2(abs(dx), abs(dy)))
            vertical_deviations.append(angle_from_vertical)

    if not vertical_deviations:
        return {"status": "pass", "avg_tilt_degrees": 0.0}

    avg_tilt = float(np.mean(vertical_deviations))
    has_tilt_keystoning = avg_tilt > max_tilt_degrees

    return {
        "status": "warning" if has_tilt_keystoning else "pass",
        "avg_vertical_tilt_degrees": round(avg_tilt, 2),
        "recommendation": "Shift camera lens pitch to zero (Two-Point Perspective) in 3ds Max/V-Ray" if has_tilt_keystoning else "Clean vertical alignment"
    }
```

---

## 4. Audit Report Schema Output

```json
{
  "project_name": "Al-Nakheel Commercial Complex",
  "image_inspected": "render_view_01_south.jpg",
  "overall_compliance_score": 88,
  "findings": [
    {
      "category": "Code & Safety",
      "severity": "CRITICAL",
      "issue": "Balustrade height on the 2nd floor terrace appears to be under 800mm, violating SBC 201 Section 1015 (min 1067mm requirement).",
      "recommendation": "Raise terrace glass balustrade to minimum 1100mm."
    },
    {
      "category": "Structural Logic",
      "severity": "WARNING",
      "issue": "Corner glass curtain wall lacks intermediate wind mullions over a 6m span.",
      "recommendation": "Introduce internal structural glass fins or steel RHS vertical mullions."
    },
    {
      "category": "Photographic Quality",
      "severity": "PASS",
      "issue": "Two-point perspective is clean with 0.8 deg average tilt.",
      "recommendation": "None."
    }
  ]
}
```
