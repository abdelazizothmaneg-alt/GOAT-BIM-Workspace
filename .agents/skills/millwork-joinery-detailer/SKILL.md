---
name: millwork-joinery-detailer
description: Architects and specifies bespoke architectural millwork, joinery shop drawings, wood veneer cuts (bookmatch/slipmatch), Blum/Hettich concealed hardware, and substrate details conforming to AWI Standards.
---

# Millwork & Joinery Detailer

Interior architecture and bespoke wood craftsmanship skill for drafting production-grade millwork shop drawings, joinery fabrication assemblies, veneer matching, and architectural hardware schedules conforming to Architectural Woodwork Institute (AWI) Standards.

---

## 1. Wood Substrates & Core Materials (AWI Standards)

```
Moisture Resistant MDF (MR-MDF) ──> Interior painted cabinetry, CNC routed panelling, lacquered doors
Birch / Marine Plywood (B/BB)   ──> Structural carcasses, vanity cabinets, heavy-duty drawer boxes
Solid Hardwood (Oak, Walnut)    ──> Edge lippings (solid wood bandings), drawer dovetails, fluted slats
Compact Laminate (HPL)          ──> High-humidity wet zones, laboratory counters, public toilet cubicles
```

### Edge Banding / Lipping Rules:
- Never apply thin $0.4\text{ mm}$ melamine edge banding on luxury joinery.
- Always specify **$1.5\text{ mm} - 2.0\text{ mm}$ thick ABS/PVC edge tape** (impact resistance) or **$6\text{ mm} - 10\text{ mm}$ solid hardwood lipping** tongue-and-groove jointed prior to veneering.

---

## 2. Wood Veneer Slicing & Matching Typologies

| Cut / Match Style | Visual Grain Appearance | Best Application |
| :--- | :--- | :--- |
| **Quarter Sliced (قص ربعي)** | Straight, parallel, vertical striped grain lines. | Contemporary minimalist wall paneling. |
| **Plain Sliced / Crown (قص تاجي)** | Prominent cathedral loop patterns and rich figures. | Executive boardroom tables and feature doors. |
| **Book Match (تطابق دفتري)** | Adjacent leaves flipped like book pages creating symmetrical mirror patterns. | Hero reception desks and focal feature walls. |
| **Slip Match (تطابق انزلاقي)** | Leaves laid side-by-side without flipping, repeating grain sequence. | Continuous long wall paneling avoiding color variation. |

---

## 3. Concealed Hardware & Blum / Hettich Standards

1. **Concealed Hinges**:
   - 3D adjustable cup hinges ($35\text{ mm}$ boring), integrated soft-close Blumotion / Sensys.
   - Opening angles: $110^\circ$ standard, $155^\circ$ zero-protrusion for internal pull-out drawers.
2. **Concealed Undermount Drawer Runners**:
   - Full extension with integrated soft-close (Blum Movento / Hettich Quadro).
   - Load ratings: $40\text{ kg}$ standard, $70\text{ kg}$ heavy-duty for deep pan/file drawers.
3. **Plinth & Scribe Detailing**:
   - Always detail a recessed toe kick ($100\text{ mm}$ high $\times 50\text{ mm}$ recessed).
   - Add $20\text{ mm}$ scribe allowance strips at wall ends to absorb site plaster out-of-plumb deviations.

---

## 4. Joinery Shop Drawing Specification Template (JSON)

```json
{
  "millwork_item_id": "MW-04-CREDENZA",
  "room_location": "Executive Dining Room 102",
  "dimensions_mm": {
    "length": 2800,
    "depth": 600,
    "height": 850
  },
  "materials": {
    "carcass_substrate": "18mm Birch Plywood with matte gray melamine facing",
    "exposed_panels": "MDF veneered with American Black Walnut, Quarter Sliced, Book Matched",
    "countertop": "20mm Honed Calacatta Gold Marble with 10mm recessed brass shadow reveal",
    "finish_coating": "Dead-flat polyurethane lacquer (5% sheen level, anti-yellowing)"
  },
  "hardware_schedule": {
    "hinges": "Blum Clip-Top BLUMOTION 110 deg (3 hinges per door)",
    "runners": "Blum MOVENTO 760H full extension undermount, 40kg capacity",
    "handles": "Concealed J-pull routered profile with blackened aluminum backing",
    "plinth": "Waterproof marine plywood clad in brushed brass PVD stainless steel"
  }
}
```

---

## 5. Deliverables Checklist
1. **Joinery Shop Drawing Set**: Plans, front elevations, and 1:5 detail sections showing joinery carcase assembly, rebated backs ($9\text{ mm}$ back grooved into side panels), and hanging cleat French cleats.
2. **Cutting List & Grain Direction Schedule**: Sheet optimization cutting diagrams ensuring grain runs continuously across adjacent cabinet door leaves.
