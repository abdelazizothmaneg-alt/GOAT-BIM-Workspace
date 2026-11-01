---
name: architectural-specification-writer
description: Authors and structures rigorous CSI MasterFormat 3-Part architectural specifications, linking Revit BIM element parameters, testing standards (ASTM, BS, EN, ISO, SASO), submittal protocols, and performance criteria.
---

# Architectural Specification Writer

Expert technical specification skill for developing CSI MasterFormat 3-Part Section specifications coordinated bidirectionally with BIM models, national standards, submittal registers, and QA/QC inspection matrices.

---

## 1. CSI MasterFormat 3-Part Architecture

Every specification section adheres strictly to the CSI/CSC SectionFormat standard:

```
SECTION [XX XX XX] - [SECTION TITLE]

PART 1 - GENERAL
  1.1 RELATED DOCUMENTS (Drawings, General Conditions)
  1.2 SUMMARY (Scope of work, products furnished but not installed)
  1.3 REFERENCES & STANDARDS (ASTM, BS EN, ISO, SASO, SBC)
  1.4 ADMINISTRATIVE REQUIREMENTS (Coordination, Pre-installation meetings)
  1.5 ACTION & INFORMATIONAL SUBMITTALS (Product Data, Shop Drawings, Samples, Leed Certs)
  1.6 CLOSEOUT SUBMITTALS (O&M Manuals, Warranties, Record Drawings)
  1.7 QUALITY ASSURANCE (Manufacturer qualifications, Installer certification, Mock-ups)
  1.8 DELIVERY, STORAGE, AND HANDLING
  1.9 FIELD CONDITIONS & AMBIENT LIMITATIONS
  1.10 WARRANTY (Manufacturer 10/20-year terms)

PART 2 - PRODUCTS
  2.1 SYSTEM MANUFACTURERS (Basis of Design, Approved Equals)
  2.2 PERFORMANCE CRITERIA (Wind loads, Thermal U-values, Acoustic STC, Fire ratings)
  2.3 MATERIALS & COMPONENTS (Physical alloys, thicknesses, core substrates)
  2.4 ACCESSORIES & FASTENERS (Corrosion resistance, anchors, gaskets)
  2.5 FABRICATION & SHOP FINISHING (Anodizing, PVDF coatings, factory tolerances)

PART 3 - EXECUTION
  3.1 EXAMINATION & SUBSTRATE VERIFICATION
  3.2 PREPARATION
  3.3 INSTALLATION (Sequence, tolerances, anchorage)
  3.4 FIELD QUALITY CONTROL (Site testing, mock-up water/air tests, third-party inspection)
  3.5 ADJUSTING & BALANCING
  3.6 CLEANING & PROTECTION
```

---

## 2. Key MasterFormat Divisions for Architecture

- **Division 07**: Thermal and Moisture Protection
  - `07 21 00` Thermal Insulation
  - `07 27 26` Fluid-Applied Membrane Air Barriers
  - `07 42 13` Metal Wall Panels
  - `07 52 00` Modified Bituminous Membrane Roofing
- **Division 08**: Openings
  - `08 11 13` Hollow Metal Doors and Frames
  - `08 44 13` Glazed Aluminum Curtain Walls
  - `08 80 00` Glazing (Insulated Glass Units, Acoustic Laminated)
- **Division 09**: Finishes
  - `09 22 16` Non-Structural Metal Framing
  - `09 29 00` Gypsum Board
  - `09 30 13` Ceramic Tiling
  - `09 51 13` Acoustical Panel Ceilings

---

## 3. BIM-to-Spec Synchronization Workflow

### Revit Shared Parameter Mapping
- `Spec_MasterFormat_Code` (e.g., `08 44 13`)
- `Spec_Manufacturer_BOD` (Basis of Design, e.g., "Schüco International")
- `Spec_Performance_UValue` (e.g., `1.3 W/m2K`)
- `Spec_Fire_Rating` (e.g., `90 minutes per ASTM E119`)

### C# Revit API Spec Parameter Validator
```csharp
using System.Collections.Generic;
using Autodesk.Revit.DB;

public class SpecParameterChecker
{
    public static List<string> AuditElementSpecs(Document doc)
    {
        List<string> unassignedElements = new List<string>();
        FilteredElementCollector collector = new FilteredElementCollector(doc)
            .WhereElementIsNotElementType()
            .OfCategory(BuiltInCategory.OST_Walls);

        foreach (Element elem in collector)
        {
            Parameter p = elem.LookupParameter("Spec_MasterFormat_Code");
            if (p == null || string.IsNullOrWhiteSpace(p.AsString()))
            {
                unassignedElements.Add($"Element ID: {elem.Id} ({elem.Name}) has no MasterFormat code!");
            }
        }
        return unassignedElements;
    }
}
```

---

## 4. Quality Rules: Eliminating Ambiguity
1. **Never use ambiguous language**:
   - ❌ "Or equal" without qualification $\rightarrow$ ✅ "Subject to compliance with requirements, approved manufacturers include: Manufacturer A, Manufacturer B, or comparable product pre-approved by Architect 14 days prior to tender."
   - ❌ "First class workmanship" $\rightarrow$ ✅ "Install plumb, level, and true to line within tolerances specified in ASTM C840, Section 8."
2. **Standard Coordination**: Always verify regional compliance (e.g., Saudi Building Code SBC 201/801/1001, SASO standards, and ASTM/BS equivalents).
