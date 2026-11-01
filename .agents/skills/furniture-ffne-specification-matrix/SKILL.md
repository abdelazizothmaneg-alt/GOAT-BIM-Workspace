---
name: furniture-ffne-specification-matrix
description: Develops structured Furniture, Fixtures & Equipment (FF&E) specification registers, evaluating contract fabric durability (Martindale >= 40,000 rubs), flame retardancy (Crib 5 / CAL 133), and procurement lead-time matrices.
---

# Furniture, Fixtures & Equipment (FF&E) Specification Matrix

Interior procurement and specification engineering skill for authoring detailed FF&E data sheets, verifying contract upholstery durability, fire rating compliance, supplier lead times, and budget cost matrices.

---

## 1. Commercial Contract Fabric Standards

Residential fabrics fail within months in hotel, commercial, or public projects. All specified textiles must meet commercial contract ratings:

### A. Abrasion Resistance Tests
- **Martindale Rub Test (BS EN ISO 12947-2)**:
  - Moderate Commercial (Executive offices, hotel guestrooms): $\ge 30,000\text{ rubs}$.
  - Severe Commercial (Hotel lobbies, restaurants, airport lounges): $\ge 50,000 - 100,000\text{ rubs}$.
- **Wyzenbeek Test (ASTM D4157)**:
  - Commercial minimum: $\ge 30,000\text{ double rubs}$ heavy-duty.

### B. Flame Retardancy Standards
- **UK / Middle East Standard**: **BS 5852: Source 5 (Crib 5)** - mandatory for public hotel upholstery and foam cushions.
- **US Standard**: **CAL 133 / NFPA 260** - full composite open flame ignition resistance.

---

## 2. FF&E Specification Code Hierarchy

Every interior asset is tagged with a unique alphanumeric code:
- `CH-XX`: Chairs, Armchairs, Barstools.
- `SO-XX`: Sofas, Sectionals, Banquettes.
- `TB-XX`: Dining Tables, Coffee Tables, Meeting Tables.
- `LT-XX`: Decorative Lamps, Pendants, Chandeliers (non-fixed architectural).
- `RU-XX`: Area Rugs, Hand-tufted Carpets.
- `AC-XX`: Decorative Accessories, Sculptures, Artwork.

---

## 3. FF&E Asset Specification Data Sheet (JSON Schema)

```json
{
  "item_code": "CH-02-LOUNGE",
  "category": "Seating",
  "item_description": "Curved Swivel Lounge Armchair",
  "room_location": "VIP Sky Lounge (Level 42)",
  "quantity": 16,
  "dimensions_mm": {
    "width": 840,
    "depth": 790,
    "height": 720,
    "seat_height": 420
  },
  "frame_and_base": {
    "structure": "Solid beech hardwood internal frame with high-resilience CNC moulded foam",
    "base": "360-degree swivel mechanism in brushed brass PVD coated stainless steel"
  },
  "upholstery_specification": {
    "fabric_name": "Kvadrat Steelcut Trio 3 (Color Code 0133)",
    "composition": "90% New Wool worsted, 10% Nylon",
    "abrasion_durability": "100,000 Martindale rubs",
    "flame_retardancy": "BS 5852 Crib 5 certified",
    "stain_treatment": "Fluorocarbon-free eco soil-resistant nanocoating"
  },
  "procurement": {
    "basis_of_design_manufacturer": "Minotti / Moroso or approved equal",
    "estimated_unit_cost_usd": 1850.00,
    "lead_time_weeks": 12,
    "submittal_requirements": "1x physical upholstery swatch (200x200mm) + foam sample"
  }
}
```

---

## 4. Deliverables Checklist
1. **Master FF&E Schedule Table**: Comprehensive Excel/CSV matrix tracking item codes, room assignments, quantities, lead times, and unpriced/priced budgets.
2. **Cut Sheet Book (Asset Data Sheets)**: 1-page PDF dossier per furniture item with dimensioned CAD line drawings, material finish swatches, and compliance test certificates.
