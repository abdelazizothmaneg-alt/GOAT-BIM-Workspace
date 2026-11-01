---
name: iso-19650-bim-auditor
description: Audits BIM models and OpenBIM IFC data against ISO 19650 container naming conventions, suitability status codes, and buildingSMART Information Delivery Specification (IDS) rules using IfcOpenShell and IfcTester in Python.
---

# ISO 19650 BIM Auditor (تدقيق حوكمة الـ BIM والبيانات طبقاً لـ ISO 19650 و IDS)

An enterprise-grade BIM governance, data quality, and Information Management auditing skill compliant with **ISO 19650-1/2/3** and **buildingSMART Information Delivery Specification (IDS)**. It validates Common Data Environment (CDE) container naming conventions, suitability status codes, and verifies property set completeness on IFC models directly using **IfcOpenShell** and **IfcTester**.

---

## 1. ISO 19650 Container Naming Convention Standard

Every model container, drawing, or document published to the CDE must strictly conform to the 7-field standard naming syntax:

$$\text{[Project] - [Originator] - [Volume/System] - [Level/Location] - [Type] - [Role] - [Number]}$$

### Permitted Field Values
1. **Project**: 2 to 6 alphanumeric characters identifying the project (e.g., `PRJ01`).
2. **Originator**: 3 to 6 characters identifying the producing organization (e.g., `ARCHI`, `STRUC`, `MEPCO`).
3. **Volume / System**: `ZZ` (All volumes), `01` (Volume 1), `B1` (Building 1).
4. **Level / Location**: `ZZ` (All levels), `00` (Ground Floor), `01` (Level 1), `M1` (Mezzanine), `RF` (Roof).
5. **Type**: `M3` (3D Model), `DR` (Drawing), `SH` (Schedule), `RP` (Report), `CA` (Calculation).
6. **Role**: `A` (Architect), `S` (Structural Engineer), `M` (Mechanical), `E` (Electrical), `P` (Plumbing), `K` (Client).
7. **Number**: 4 to 6 sequential digits (e.g., `00001`).

*Example*: `PRJ01-ARCHI-B1-01-M3-A-00001.rvt`

---

## 2. Suitability & Status Codes (ISO 19650-2 Table A.1)

| Status Code | Description | Standard Application |
| :--- | :--- | :--- |
| **S0** | Initial Work in Progress (WIP) | Internal discipline working state; not shared. |
| **S1** | Shared for Coordination | Suitable for multi-disciplinary clash review. |
| **S2** | Shared for Information | For client / external stakeholder review only. |
| **S3** | Shared for Review & Comment | Formal consultant milestone review. |
| **S4** | Shared for Stage Approval | Formal submission for client / authority sign-off. |
| **A1, A2...**| Published - Contractual | Authorized for construction, procurement, or fabrication. |
| **B1, B2...**| Published - Preliminary | Authorized for tender inquiry. |

---

## 3. Automated buildingSMART IDS Audit Engine (Python & IfcTester)

```python
import ifcopenshell
from ifctester import ids, reporter
import re

def audit_iso19650_filename(filename: str) -> dict:
    """
    Validates file naming against ISO 19650-2 National Annex standards.
    """
    pattern = r"^[A-Z0-9]{2,6}-[A-Z0-9]{3,6}-[A-Z0-9]{2}-[A-Z0-9]{2}-[A-Z0-9]{2}-[A-Z]{1}-[0-9]{4,6}(\.[a-zA-Z0-9]+)?$"
    is_valid = bool(re.match(pattern, filename))
    return {
        "filename": filename,
        "is_iso19650_compliant": is_valid,
        "error": None if is_valid else "Violates standard [Project]-[Originator]-[Volume]-[Level]-[Type]-[Role]-[Number] format"
    }

def audit_ifc_against_ids(ifc_file_path: str, ids_xml_path: str, output_html: str = "audit_report.html"):
    """
    Validates IFC building model against buildingSMART IDS specification.
    """
    # 1. Load IDS specifications
    specifications = ids.Ids().from_xml(ids_xml_path)

    # 2. Open IFC Model
    model = ifcopenshell.open(ifc_file_path)

    # 3. Execute deterministic validation audit
    specifications.validate(model)

    # 4. Generate structured HTML report
    html_rep = reporter.Html(specifications)
    html_rep.report()
    html_rep.to_file(output_html)

    return {
        "status": "complete",
        "report_generated": output_html,
        "total_specifications": len(specifications.specifications)
    }
```

---

## 4. Mandatory IFC Property Set (Pset) Audit Rules
When auditing architectural IFC exports:
- Every `IfcWall` must contain `Pset_WallCommon.IsExternal` (Boolean) and `LoadBearing` (Boolean).
- Every `IfcDoor` must contain `Pset_DoorCommon.FireRating` (String, e.g., `FR-60`) and `ClearWidth` (Length).
- Every `IfcSpace` must possess valid classification references (`OmniClass` Table 13 or `UniFormat` Table A).
