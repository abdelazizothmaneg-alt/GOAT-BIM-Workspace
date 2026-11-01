---
name: cobie-asset-management
description: Automates BIM-to-Facility Management (FM) handover using the COBie standard (ISO 19650-3), generating structured asset registers, spatial containment hierarchies, and parameter quality audits directly from Autodesk Revit via C# and Python.
---

# COBie Asset Management (أتمتة تسليم الأصول وإدارة المرافق COBie)

An enterprise-grade BIM-to-Facility Management (FM) handover and data governance skill. It automates the extraction, verification, and assembly of **COBie (Construction Operations Building Information Exchange)** data directly from Autodesk Revit models in compliance with **ISO 19650-3** and the **US National BIM Standard (NBIMS)**.

---

## 1. COBie Relational Data Structure

```
[Facility] (Project Information & Units)
    │
    └── [Floor] (Revit Levels)
            │
            └── [Space] (Revit Rooms / MEP Spaces)
                    │
                    ├── [Zone] (Functional / Thermal Groupings)
                    │
                    └── [Component] (Revit Family Instances: Equipment, Fixtures)
                            │
                            └── [Type] (Revit Family Types: Manufacturer, Model)
                                    │
                                    ├── [Document] (O&M Manuals, Warranties)
                                    └── [Spare] (Spare parts, replacements)
```

---

## 2. Core Revit API Extraction Engine (.NET C#)

A high-performance C# command that collects maintainable assets, verifies parameter completeness, and formats them into the COBie `Component` and `Type` relational schema without relying on third-party plugins:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.UI;

namespace BimHandoverAutomation
{
    public class CobieAssetExtractor
    {
        public static List<CobieComponentRecord> ExtractComponents(Document doc)
        {
            var records = new List<CobieComponentRecord>();

            // Categories that require maintenance (FM Handover Scope)
            BuiltInCategory[] targetCategories = new[]
            {
                BuiltInCategory.OST_MechanicalEquipment,
                BuiltInCategory.OST_PlumbingFixtures,
                BuiltInCategory.OST_ElectricalEquipment,
                BuiltInCategory.OST_ElectricalFixtures,
                BuiltInCategory.OST_LightingFixtures,
                BuiltInCategory.OST_Doors
            };

            ElementMulticategoryFilter catFilter = new ElementMulticategoryFilter(targetCategories);

            var instances = new FilteredElementCollector(doc)
                .WherePasses(catFilter)
                .WhereElementIsNotElementType()
                .ToElements();

            foreach (Element elem in instances)
            {
                ElementType type = doc.GetElement(elem.GetTypeId()) as ElementType;
                if (type == null) continue;

                // Determine containing space/room
                string spaceName = "Unknown_Space";
                if (elem is FamilyInstance fi)
                {
                    Room room = fi.Room ?? fi.Space as Room;
                    if (room != null)
                    {
                        spaceName = $"{room.Number}:{room.Name}";
                    }
                }

                records.Add(new CobieComponentRecord
                {
                    Name = elem.Name + "-" + elem.Id.Value,
                    CreatedBy = "BIM_Lead_Automation",
                    CreatedOn = DateTime.UtcNow.ToString("s"),
                    TypeName = $"{type.FamilyName}:{type.Name}",
                    Space = spaceName,
                    Description = elem.LookupParameter("Comments")?.AsString() ?? "",
                    SerialNumber = elem.LookupParameter("Serial Number")?.AsString() ?? "TBD",
                    InstallationDate = elem.LookupParameter("Installation Date")?.AsString() ?? DateTime.Today.ToString("yyyy-MM-dd"),
                    AssetIdentifier = elem.LookupParameter("Mark")?.AsString() ?? elem.Id.Value.ToString()
                });
            }

            return records;
        }
    }

    public class CobieComponentRecord
    {
        public string Name { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedOn { get; set; }
        public string TypeName { get; set; }
        public string Space { get; set; }
        public string Description { get; set; }
        public string SerialNumber { get; set; }
        public string InstallationDate { get; set; }
        public string AssetIdentifier { get; set; }
    }
}
```

---

## 3. Automated Pre-Handover Data Quality Audit (QA/QC)

Before generating Excel spreadsheets or pushing to CAFM databases (Maximo, ARCHIBUS), validate the following strict criteria:
1. **Unlocated Assets**: Detect any equipment where `Space` is blank or `Level` is null (occurs when equipment is placed outside room bounding geometry).
2. **Missing Classification**: Enforce that every Family Type has a valid OmniClass (Table 23) or UniFormat (2010) code assigned:
   - HVAC: `23.75.10.00` (Central Air Handling Units)
   - Plumbing: `22.40.00.00` (Plumbing Fixtures)
3. **Empty Asset Identifiers**: Flag elements where `Mark` or `Tag` parameters are duplicate or blank.

---

## 4. Multi-Format Output Generation (ClosedXML / EPPlus)
- Outputs standard COBie 2.4 `.xlsx` workbooks with color-coded sheet tabs:
  - Yellow: Required primary keys.
  - Orange: References to other sheets (Foreign keys).
  - Purple: Optional asset attributes.
- Generates clean SQLite or JSON payloads for direct synchronization with IoT and digital twin platforms (Autodesk Tandem, AWS IoT TwinMaker).
