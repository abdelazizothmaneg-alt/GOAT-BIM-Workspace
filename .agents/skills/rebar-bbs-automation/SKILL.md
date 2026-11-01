---
name: rebar-bbs-automation
description: Automates structural rebar extraction, bend deduction calculations, and Bar Bending Schedule (BBS) generation according to BS 8666 and ACI 318 using the Autodesk Revit API in C# .NET.
---

# Rebar BBS Automation (تفريد حديد التسليح وحساب جداول الـ BBS)

A high-precision structural BIM engineering skill specialized in the automated extraction, calculation, and documentation of reinforced concrete reinforcement. It parses native Revit `Rebar` elements, computes cut lengths and bend allowances in accordance with **BS 8666** and **ACI 318**, generates **Bar Bending Schedules (BBS)**, and automates schematic bending details directly on structural sheets.

---

## 1. Structural Detailing Pipeline

```
[Revit Structural Model (Columns, Beams, Slabs, Footings)]
                         │
                         ▼
   [1. Filtered Element Collector & Rebar Harvest]
   - Collect All Rebar & RebarInSystem instances
   - Filter by Host Category (OST_StructuralFraming, OST_Floors, etc.)
                         │
                         ▼
   [2. Centerline Curve & Geometry Decomposition]
   - Extract centerline segments (IList<Curve>)
   - Classify Standard Shape Codes (Shape 00, 11, 21, 51, etc.)
   - Compute bend radius & hook allowances (A, B, C, D dimensions)
                         │
                         ▼
   [3. Cut Length & Tonnage Calculations]
   - Total Cut Length = Sum(Segment Lengths) - Bend Deductions
   - Nominal bar unit weight: w = (d^2 / 162.2) kg/m
   - Total Weight (Metric Tonnes) by Diameter (T10, T12, T16, T20, T25, T32)
                         │
                         ▼
   [4. Native Revit Output & External Documentation]
   - Programmatic Rebar Schedule View creation (ViewSchedule)
   - RebarBendingDetail element placement on shop drawing sheets
   - Excel / CSV export with formatted BBS matrix
```

---

## 2. Rebar Geometry & Cut Length Calculator (C# .NET)

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;

namespace StructuralRebarAutomation
{
    public class RebarBbsProcessor
    {
        public static List<BbsRow> ProcessDocumentRebars(Document doc)
        {
            var bbsTable = new List<BbsRow>();

            var rebars = new FilteredElementCollector(doc)
                .OfClass(typeof(Rebar))
                .Cast<Rebar>()
                .ToList();

            foreach (Rebar rebar in rebars)
            {
                RebarBarType barType = doc.GetElement(rebar.GetTypeId()) as RebarBarType;
                RebarShape shape = doc.GetElement(rebar.GetShapeId()) as RebarShape;

                double barDiameterMm = UnitUtils.ConvertFromInternalUnits(barType.BarDiameter, UnitTypeId.Millimeters);
                double totalLengthMm = UnitUtils.ConvertFromInternalUnits(rebar.GetTotalLength(), UnitTypeId.Millimeters);

                // Compute nominal unit weight: kg/m = d^2 / 162.2
                double unitWeightKgPerM = Math.Pow(barDiameterMm, 2) / 162.198;
                double totalWeightKg = (totalLengthMm / 1000.0) * unitWeightKgPerM * rebar.Quantity;

                // Extract segments A, B, C, D
                var segments = ExtractSegmentDimensions(rebar);

                bbsTable.Add(new BbsRow
                {
                    BarMark = rebar.LookupParameter("Mark")?.AsString() ?? "RM",
                    ShapeCode = shape?.Name ?? "Shape_00",
                    Diameter = (int)Math.Round(barDiameterMm),
                    Quantity = rebar.Quantity,
                    CutLengthMm = Math.Round(totalLengthMm),
                    TotalWeightKg = Math.Round(totalWeightKg, 2),
                    SegmentA = segments.GetValueOrDefault("A", 0),
                    SegmentB = segments.GetValueOrDefault("B", 0),
                    SegmentC = segments.GetValueOrDefault("C", 0)
                });
            }

            return bbsTable;
        }

        private static Dictionary<string, double> ExtractSegmentDimensions(Rebar rebar)
        {
            var dict = new Dictionary<string, double>();
            string[] paramNames = new[] { "A", "B", "C", "D", "E" };
            foreach (var pName in paramNames)
            {
                Parameter p = rebar.LookupParameter(pName);
                if (p != null && p.StorageType == StorageType.Double)
                {
                    dict[pName] = UnitUtils.ConvertFromInternalUnits(p.AsDouble(), UnitTypeId.Millimeters);
                }
            }
            return dict;
        }
    }

    public class BbsRow
    {
        public string BarMark { get; set; }
        public string ShapeCode { get; set; }
        public int Diameter { get; set; }
        public int Quantity { get; set; }
        public double CutLengthMm { get; set; }
        public double TotalWeightKg { get; set; }
        public double SegmentA { get; set; }
        public double SegmentB { get; set; }
        public double SegmentC { get; set; }
    }
}
```

---

## 3. Standard Shape Codes & Bend Deduction Rules (BS 8666)

| Shape Code | Description | Cut Length Formula |
| :--- | :--- | :--- |
| **00** | Straight Bar | $L = A$ |
| **11** | L-Bar (One $90^\circ$ bend) | $L = A + B - 0.5r - d$ |
| **21** | U-Bar (Two $90^\circ$ bends) | $L = A + B + C - r - 2d$ |
| **51** | Column Stirrup / Tie | $L = 2A + 2B + 150\text{ mm}$ (Hook allowance) |
| **67** | Closed Link / Beam Stirrup | $L = 2A + 2B + 20d$ |

*Where $r$ is the former bend radius, and $d$ is the nominal bar diameter.*

---

## 4. Native Revit Rebar Bending Detail Placement (Revit 2025+)

In Revit 2025 and 2026, schematic bending details can be placed automatically on view annotations:

```csharp
// Placing a schematic bending detail directly on a structural sheet
using (Transaction t = new Transaction(doc, "Place Rebar Bending Details"))
{
    t.Start();
    
    // Automatically aligns with dimension witness lines
    // RebarBendingDetail.Create(doc, view, rebarId, placementPoint);
    
    t.Commit();
}
```
