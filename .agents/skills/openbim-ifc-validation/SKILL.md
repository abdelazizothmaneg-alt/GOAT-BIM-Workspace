---
name: openbim-ifc-validation
description: >-
  Audits, inspects, and validates OpenBIM IFC files (IFC2x3, IFC4, IFC4.3) using IfcOpenShell and buildingSMART IDS
  (Information Delivery Specification), verifying spatial containment, property sets (Psets), and quantity takeoffs (Qto).
---

# OpenBIM & IFC Validation Engine

Standardized auditing and scripting framework for analyzing Industry Foundation Classes (IFC) models via `IfcOpenShell` and automated Information Delivery Specifications (IDS).

## 1. Spatial Containment & Hierarchy Verification

Every valid IFC file must maintain a single, unbroken spatial tree:
```text
IfcProject ──> IfcSite ──> IfcBuilding ──> IfcBuildingStorey ──> IfcSpace / IfcProduct
```

### Python Script with `ifcopenshell`:
```python
import ifcopenshell
import ifcopenshell.util.element

def audit_ifc_spatial_hierarchy(file_path):
    """Verifies that all physical building elements are properly hosted to a building storey."""
    model = ifcopenshell.open(file_path)
    
    # Check schema
    schema = model.schema
    print(f"IFC Schema: {schema}")
    
    # Audit orphaned elements (not hosted to an IfcBuildingStorey)
    elements = model.by_type("IfcBuildingElement")
    orphans = []
    
    for elem in elements:
        storey = ifcopenshell.util.element.get_container(elem)
        if storey is None or storey.is_a("IfcBuildingStorey") is False:
            orphans.append({
                "id": elem.id(),
                "type": elem.is_a(),
                "name": elem.Name or "Unnamed",
                "guid": elem.GlobalId
            })
            
    return {
        "schema": schema,
        "total_elements": len(elements),
        "orphan_count": len(orphans),
        "orphans": orphans
    }
```

---

## 2. Property Set (Pset) & Quantity Extraction

Inspect standard and custom Property Sets without proprietary desktop software:

```python
import ifcopenshell
import ifcopenshell.util.element

def extract_element_psets_and_qto(elem):
    """Extracts all Psets and Quantities into a clean dictionary."""
    psets = ifcopenshell.util.element.get_psets(elem)
    
    # Standard Pset_WallCommon checks
    fire_rating = psets.get("Pset_WallCommon", {}).get("FireRating", "Not Specified")
    is_external = psets.get("Pset_WallCommon", {}).get("IsExternal", False)
    
    # Base Quantities
    gross_area = psets.get("Qto_WallBaseQuantities", {}).get("GrossSideArea", 0.0)
    net_volume = psets.get("Qto_WallBaseQuantities", {}).get("NetVolume", 0.0)
    
    return {
        "guid": elem.GlobalId,
        "fire_rating": fire_rating,
        "is_external": is_external,
        "gross_area_m2": gross_area,
        "net_volume_m3": net_volume,
        "all_psets": psets
    }
```

---

## 3. Automated IDS (Information Delivery Specification) Validation

buildingSMART IDS allows declarative XML-based contract checking:
* **Facet Requirements:**
  * Entity type matching (e.g., all `IfcWall` must have `Pset_WallCommon.LoadBearing`).
  * Attribute requirements (e.g., `Name` must follow ISO 19650 naming conventions).
  * Material layer thickness bounds ($\ge 10\text{mm}$).
* **Execution via IfcOpenShell IDS Validator:**
  ```bash
  python -m ifcopenshell.ids validate --ids project_requirements.ids --ifc target_model.ifc --output report.html
  ```

---

## 4. Schema Best Practices
* **IFC2x3:** Legacy coordination view (`CoordinationView_V2.0`), still common for backward compatibility.
* **IFC4:** Reference View (`ReferenceView_V1.2`) for multidisciplinary clash coordination; Design Transfer View for editable geometry.
* **IFC4.3:** Extends OpenBIM to linear civil infrastructure (roads, bridges, railways, ports).
