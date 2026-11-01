---
name: dynamo-revit
description: >-
  Develops, debugs, and optimizes Dynamo for Revit visual programming scripts, Zero-Touch C# node packages,
  and embedded Python Script nodes using RevitServices and RevitAPI. Covers geometry conversions, transaction management, and Dynamo Player deployment.
---

# Dynamo for Revit (DynamoRevit)

Engineering manual for building, scripting, and compiling automation workflows for Autodesk Revit via the Dynamo visual programming ecosystem.

## 1. Dynamo Architecture in Revit
* **DynamoCore:** Platform-independent execution engine, geometry library (`ProtoGeometry.dll`), and node graph parser.
* **DynamoRevit:** The host integration bridge containing:
  * `RevitNodes.dll`: Wrappers around native Revit elements (e.g., `Revit.Elements.Wall`, `Revit.Elements.Floor`).
  * `RevitServices.dll`: Session document context (`DocumentManager`) and transaction management (`TransactionManager`).

---

## 2. Python Script Node Architecture (The Universal Pattern)

Every production Python node in Dynamo must follow this strict template:

```python
import clr

# 1. Reference Revit API
clr.AddReference('RevitAPI')
clr.AddReference('RevitAPIUI')
from Autodesk.Revit.DB import *

# 2. Reference Dynamo RevitServices
clr.AddReference('RevitServices')
import RevitServices
from RevitServices.Persistence import DocumentManager
from RevitServices.Transactions import TransactionManager

# 3. Reference Dynamo Geometry & Element Conversion Extensions
clr.AddReference('RevitNodes')
import Revit
clr.ImportExtensions(Revit.Elements)
clr.ImportExtensions(Revit.GeometryConversion)

# 4. Context Access
doc = DocumentManager.Instance.CurrentDBDocument
uidoc = DocumentManager.Instance.CurrentUIApplication.ActiveUIDocument

# 5. Ingest Inputs & Unwrap
input_elements = IN[0]
if not isinstance(input_elements, list):
    input_elements = [input_elements]

# Unwrap Dynamo wrapper elements into native Autodesk.Revit.DB.Element instances
revit_elements = [UnwrapElement(x) for x in input_elements if x is not None]

# 6. Execute Transaction
output_list = []
TransactionManager.Instance.EnsureInTransaction(doc)

try:
    for elem in revit_elements:
        param = elem.LookupParameter("Comments")
        if param and not param.IsReadOnly:
            param.Set("Processed by DynamoRevit")
            output_list.append(elem.Id.ToString())
            
    TransactionManager.Instance.TransactionTaskDone()
except Exception as ex:
    TransactionManager.Instance.ForceCloseTransaction()
    output_list = f"Error: {str(ex)}"

# 7. Output back to Dynamo
OUT = output_list
```

---

## 3. Geometry Conversions (ProtoGeometry vs. RevitAPI)

Always convert types explicitly using RevitNodes extension methods:
* **Points:**
  * Dynamo Point $\rightarrow$ Revit XYZ: `dynamoPoint.ToXyz()`
  * Revit XYZ $\rightarrow$ Dynamo Point: `revitXyz.ToPoint()`
* **Curves & Lines:**
  * Dynamo Curve $\rightarrow$ Revit Curve: `dynamoCurve.ToRevitType()`
  * Revit Curve $\rightarrow$ Dynamo Curve: `revitCurve.ToProtoType()`
* **Solids & Meshes:**
  * Dynamo Solid $\rightarrow$ Revit GeometryObject: `dynamoSolid.ToRevitType()`
  * Revit Solid $\rightarrow$ Dynamo Solid: `revitSolid.ToProtoType()`

---

## 4. Zero-Touch C# Node Development

Build compiled, high-performance node libraries directly in C#:
```csharp
using Autodesk.DesignScript.Runtime;
using Revit.Elements;

namespace MyDynamoNodes
{
    public static class WallAutomation
    {
        /// <summary>
        /// Calculates total gross perimeter and internal surface area for a list of walls.
        /// </summary>
        [MultiReturn(new[] { "TotalLength", "TotalArea" })]
        public static Dictionary<string, double> CalculateWallMetrics(List<Revit.Elements.Wall> walls)
        {
            double totalLength = 0;
            double totalArea = 0;

            foreach (var w in walls)
            {
                // Access internal native Revit element via InternalElement
                Autodesk.Revit.DB.Wall internalWall = (Autodesk.Revit.DB.Wall)w.InternalElement;
                totalLength += internalWall.get_Parameter(Autodesk.Revit.DB.BuiltInParameter.CURVE_ELEM_LENGTH).AsDouble();
            }

            return new Dictionary<string, double>
            {
                { "TotalLength", totalLength },
                { "TotalArea", totalArea }
            };
        }

        // Hide helper methods from appearing in the Dynamo library
        [IsVisibleInDynamoLibrary(false)]
        public static void InternalHelper() { }
    }
}
```

---

## 5. Dynamo Player Deployment
* Mark input nodes with **Is Input** (`Right-click -> Is Input`) to expose sliders, drop-down category lists, and file pickers in the Dynamo Player sidebar.
* Mark final result nodes with **Is Output** to display execution summaries (e.g., "145 sheets created successfully").
