---
name: bim-visual-programming-automation
description: >-
  Develops visual programming automation scripts in Autodesk Dynamo and Rhino Grasshopper, automating repetitive BIM tasks,
  computational geometry generation, parameter batch population, sheet creation, and data extraction pipelines.
---

# BIM Visual Programming & Computational Automation

Automated workflows and algorithmic modeling leveraging visual programming tools (Dynamo, Grasshopper) and embedded Python scripts.

## 1. Visual Script Architecture (Dynamo & Grasshopper)
Organize node graphs into clean, color-coded functional groups:
* **Group 1: Input & Data Ingestion (Cyan):** User sliders, model selections, Excel/CSV file paths.
* **Group 2: Data Cleansing & List Management (Purple):** Flatten, lacing controls (`Shortest`, `Longest`, `Cross Product`), filtering nulls/empty lists.
* **Group 3: Computational Geometry & Core Logic (Orange):** Curve division, surface tessellation, intersection tests, vector calculations.
* **Group 4: BIM Transaction & Parameter Binding (Green):** Creating Revit/Archicad native elements, setting parameter values, committing transactions.
* **Group 5: Output & Verification (Grey):** Watch nodes, automated PDF/Excel export reports, execution time profiling.

## 2. High-Value Automation Patterns

### Pattern A: Automated Sheet & Viewport Generator
* Read Excel schedule containing View Names, Levels, Scales, and Target Sheet Numbers.
* Create duplicate floor plans/sections with assigned View Templates.
* Generate new Sheets and place Viewports aligned to standard title block coordinates.

### Pattern B: Batch Parameter Population & Re-numbering
* Select elements sequentially along a spline curve (e.g., parking bays, doors, room numbers).
* Re-number instances sequentially (`P-001`, `P-002`, `D-101`, `D-102`) based on spatial sorting ($X/Y$ coordinates or curve parameter $t$).

### Pattern C: Generative Facade Paneling (Grasshopper)
* Parametric surface subdivision based on attractor points (solar orientation or sightlines).
* Vary louver angles and panel perforation percentages adaptively to optimize daylight factors while minimizing solar heat gain coefficient (SHGC).

## 3. Embedded Python Node Scripting
```python
# Dynamo / Revit API Python Script template
import clr
clr.AddReference('RevitAPI')
from Autodesk.Revit.DB import *

clr.AddReference('RevitServices')
import RevitServices
from RevitServices.Persistence import DocumentManager
from RevitServices.Transactions import TransactionManager

doc = DocumentManager.Instance.CurrentDBDocument
elements = UnwrapElement(IN[0])
param_name = IN[1]
new_value = IN[2]

TransactionManager.Instance.EnsureInTransaction(doc)

for elem in elements:
    param = elem.LookupParameter(param_name)
    if param and not param.IsReadOnly:
        param.Set(new_value)

TransactionManager.Instance.TransactionTaskDone()

OUT = elements
```
