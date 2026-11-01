---
name: drywall-acoustic-partition-selector
description: Specifies and details high-performance lightweight drywall partition assemblies (Knauf/Gyproc), evaluating Sound Transmission Class (STC 45-65 dB), fire resistance (FR 60-120 min), and structural deflection heads.
---

# Drywall & Acoustic Partition Selector

Architectural fit-out and acoustic engineering skill for configuring, detailing, and scheduling drywall partition wall assemblies (Knauf, British Gypsum, Gyproc), acoustic insulation infill, perimeter sealing, and seismic/deflection head details.

---

## 1. Acoustic & Fire Performance Matrix (Knauf / Gyproc Standards)

| System Code | Board Composition | Stud Width | Mineral Wool Infill | Acoustic Rating ($STC / R_w$) | Fire Rating (ASTM E119 / BS 476) | Primary Application |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **K-STD-100** | $1 \times 12.5\text{ mm}$ Regular Gypsum each side | $70\text{ mm}$ C-Stud | None | $35 - 38\text{ dB}$ | 30 minutes | Residential storage, non-sensitive partitions |
| **K-ACO-125** | $2 \times 12.5\text{ mm}$ Acoustic Gypsum each side | $70\text{ mm}$ C-Stud | $50\text{ mm}$ Glasswool ($16\text{ kg/m}^3$) | $52 - 54\text{ dB}$ | 60 minutes | Standard hotel guestroom corridor |
| **K-PRO-150** | $2 \times 15.0\text{ mm}$ Fire-rated Gypsum each side | $90\text{ mm}$ C-Stud | $80\text{ mm}$ Rockwool ($40\text{ kg/m}^3$) | $58 - 60\text{ dB}$ | 120 minutes | Executive boardrooms, server rooms |
| **K-STU-220** | $2 \times 15.0\text{ mm}$ Soundshield on **Twin Independent Studs** | $2 \times 50\text{ mm}$ separated studs | $2 \times 50\text{ mm}$ Rockwool ($50\text{ kg/m}^3$) | $65 - 68\text{ dB}$ | 120 minutes | Cinema halls, recording studios, VIP suites |

---

## 2. Flanking Transmission & Perimeter Sealing Rules

Acoustic performance fails if flanking sound leaks through micro-gaps:
1. **Continuous Acoustic Sealant Beads**:
   - Apply continuous double beads of non-hardening acoustic sealant (Acoustic mastic) under the floor runner track and ceiling head channel prior to anchoring.
2. **Staggered Board Joints**:
   - Stagger vertical and horizontal joints between base layer and face layer boards by at least $300\text{ mm}$.
   - Never place electrical socket back-boxes back-to-back in the same stud cavity; maintain minimum $600\text{ mm}$ horizontal separation and line boxes with acoustic putty pads.

---

## 3. Structural Deflection Head Detail (فواصل هبوط الأسقف)

To absorb live load deflection of structural concrete slabs above without crushing the drywall:
- Install a **Deep Flange Head Track** ($50\text{ mm}$ leg depth).
- Studs cut short by $20 - 25\text{ mm}$ below the inside web of the top track.
- Gypsum boards stop $15 - 20\text{ mm}$ below the slab soffit.
- Seal the gap with fire-rated elastomeric intumescent mastic and mineral wool backing.

---

## 4. Python Drywall Wall Assembly Selector

```python
"""
Knauf / Gyproc Partition System Sizing Selector
"""

DRYWALL_DATABASE = [
    {"name": "Standard Office Divider", "stc": 42, "fire_min": 60, "thickness_mm": 100, "layers": "1x12.5mm each side", "stud": "70mm"},
    {"name": "Executive Meeting Room", "stc": 52, "fire_min": 60, "thickness_mm": 125, "layers": "2x12.5mm Acoustic each side", "stud": "70mm + 50mm wool"},
    {"name": "Hotel Inter-Guestroom Demising", "stc": 58, "fire_min": 90, "thickness_mm": 150, "layers": "2x15mm Fire/Acoustic each side", "stud": "90mm + 80mm rockwool"},
    {"name": "Cinema / VIP Acoustic Split", "stc": 66, "fire_min": 120, "thickness_mm": 220, "layers": "2x15mm on Twin Decoupled Studs", "stud": "Twin 50mm decoupled"}
]

def select_partition_system(target_stc: int, target_fire_minutes: int) -> dict:
    candidates = [p for p in DRYWALL_DATABASE if p["stc"] >= target_stc and p["fire_min"] >= target_fire_minutes]
    if not candidates:
        return {"error": "No standard single-stud assembly meets criteria. Specify custom masonry or resilient decoupled twin wall."}
        
    # Pick the most efficient candidate (minimum thickness)
    chosen = min(candidates, key=lambda x: x["thickness_mm"])
    return chosen

if __name__ == "__main__":
    res = select_partition_system(target_stc=55, target_fire_minutes=60)
    print("Selected Drywall Assembly:")
    for k, v in res.items():
        print(f"  {k}: {v}")
```

---

## 5. Deliverables Checklist
1. **Wall Type Legend Sheet**: Cross-section detail of every wall type (W1, W2, W3) showing stud size, board types, screws, and insulation density.
2. **Deflection Head Detail Drawing**: 1:5 scale section through slab deflection track, firestop mastic, and acoustic baffle.
