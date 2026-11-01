---
name: adaptive-reuse-heritage-bim
description: Models, documents, and audits historic structures and adaptive reuse projects using Heritage BIM (HBIM) from point clouds, managing masonry deformations and conservation charters.
---

# Adaptive Reuse & Heritage BIM (التوثيق وإعادة التأهيل للمباني التراثية)

An architectural preservation and computational conservation skill specialized in **Heritage Building Information Modeling (HBIM)** and adaptive reuse. It translates terrestrial laser scans and photogrammetry of historic masonry into non-standard parametric BIM objects, documents stone deterioration, and enforces conservation ethics per the **Venice Charter** and **Burra Charter**.

---

## 1. HBIM Conservation Workflow

```
[3D Laser Scan & Drone Photogrammetry of Heritage Monument]
                            │
                            ▼
     [1. Point Cloud Section Slicing & Orthophoto Mapping]
     - Extract true non-vertical stone wall deformation lines
     - Map historical mortar erosion and salt efflorescence
                            │
                            ▼
     [2. Non-Standard HBIM Geometric Modeling]
     - DirectShape / In-Place Families for deformed masonry
     - Irregular timber lintels, mud-brick layers, traditional arches
                            │
                            ▼
     [3. Condition Assessment & Pathology Database]
     - Custom shared parameters: Material_Epoch, Decay_Type, Risk_Level
     - Decay types: Spalling, Cracking, Moisture, Biogrowth
                            │
                            ▼
     [4. Adaptive Reuse Intervention Strategy]
     - Reversible modern structural interventions (Steel / Timber)
     - MEP services insertion with zero damage to historic fabric
```

---

## 2. Historic Building Pathology Schema (Revit Shared Parameters)

| Shared Parameter | Data Type | Permitted Values / Description |
| :--- | :--- | :--- |
| `HBIM_Construction_Epoch` | Text | `Pre-1900`, `Najdi_Mudbrick_1920s`, `Ottoman_Stone` |
| `HBIM_Deterioration_Class`| Text | `Structural_Crack`, `Mortar_Loss`, `Salt_Efflorescence` |
| `HBIM_Conservation_Action` | Text | `Consolidate`, `Repoint`, `Clean`, `Reconstruct` |
| `HBIM_Reversibility_Score` | Integer | $1\text{ (Permanent)}$ to $5\text{ (Fully Reversible modern fix)}$ |

---

## 3. Structural Reversibility Principle (Burra Charter)
- Any contemporary structural intervention (such as adding elevator shafts or structural steel tiebacks for earthquake resistance) must be **reversible** without destroying the original historic substrate.
- Traditional mudbrick (Adobe) and coral stone walls must never be sealed with non-breathable Portland cement plaster; enforce lime-based breathable mortars.
