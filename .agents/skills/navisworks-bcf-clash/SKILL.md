---
name: navisworks-bcf-clash
description: >-
  Automates multi-disciplinary clash detection in Autodesk Navisworks Manage via the .NET API, intelligent clash grouping algorithms
  (by Grid, Level, or Subcontractor), and automated BCF 2.1 / 3.0 issue packaging for direct Revit synchronization.
---

# Navisworks Automation & BCF Clash Management

Enterprise coordination framework for orchestrating automated clash detection in Autodesk Navisworks Manage and synchronizing issues via BIM Collaboration Format (BCF).

## 1. Navisworks Manage .NET API Automation

Execute batch clash tests headlessly across linked NWC/NWD federated models:

```csharp
using System;
using System.Collections.Generic;
using Autodesk.Navisworks.Api;
using Autodesk.Navisworks.Api.Clash;

namespace ORTools.Coordination
{
    public static class NavisworksClashAutomation
    {
        public static void RunBatchClashTests(Document nwDoc)
        {
            DocumentClash docClash = nwDoc.GetClash();
            DocumentClashTests clashTests = docClash.TestsData;

            foreach (ClashTest test in clashTests.Tests)
            {
                // Verify test has valid selection sets
                if (test.SelectionA.IsEmpty || test.SelectionB.IsEmpty) continue;

                // Configure test tolerance (e.g., 25mm for MEP clearance)
                test.Tolerance = 0.025; // Meters

                // Execute clash test
                clashTests.TestsRunTest(test);
                Console.WriteLine($"[Executed] Test: {test.DisplayName} - Clashes Found: {test.Children.Count}");
            }
        }
    }
}
```

---

## 2. Intelligent Clash Grouping Algorithms

Avoid overwhelming contractors with 10,000 isolated clash points by grouping related intersections into singular coordination issues:

* **Grouping Strategy 1: By Nearest Structural Grid Intersection:**
  * Map clash $XYZ$ coordinates to the closest column grid intersection (e.g., `Grid_C-4`).
  * Clashes within a $3.0\text{ m}$ radius sharing identical disciplines are merged into a single group.
* **Grouping Strategy 2: By Main Equipment Host:**
  * When a continuous duct run clashes with multiple structural rebar or floor joists, group all clashes under the primary Duct Header ID.
* **Grouping Strategy 3: By Level / Zone:**
  * Partition issues by floor level to streamline trade-specific coordination meetings.

---

## 3. BCF (BIM Collaboration Format 2.1 / 3.0) Issue Packaging

Package clashes into open, vendor-neutral BCF `.bcfzip` archives readable by Revit, Solibri, and Revizto:

```text
Project_Clash_Report.bcfzip
├── bcf.version                 # Declares BCF version ("2.1" or "3.0")
├── project.bcfp                # Project metadata & extension schemas
└── [Issue-GUID-Folder]/
    ├── markup.bcf              # Topic title, status, assignee, comments XML
    ├── viewpoint.bcfv          # Camera position (XYZ), target, up-vector, clipping planes
    └── snapshot.png            # Visual 2D redline snapshot
```

### Viewpoint Camera Positioning in Revit:
* When clicking a BCF issue inside the Revit add-in, construct a 3D Section Box centered around the clash coordinate:
  ```csharp
  BoundingBoxXYZ sectionBox = new BoundingBoxXYZ();
  sectionBox.Min = new XYZ(clashX - 1.5, clashY - 1.5, clashZ - 1.0);
  sectionBox.Max = new XYZ(clashX + 1.5, clashY + 1.5, clashZ + 1.0);
  active3DView.SetSectionBox(sectionBox);
  ```
