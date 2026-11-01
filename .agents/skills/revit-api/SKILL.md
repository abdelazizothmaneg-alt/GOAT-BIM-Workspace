---
name: revit-api
description: >-
  Core Autodesk Revit API programming skill covering Autodesk.Revit.DB and Autodesk.Revit.UI, FilteredElementCollectors,
  Transaction lifecycle, ElementId references, geometry inspection, parameter access, and failure handling.
---

# Revit API Core Architecture

Essential developer reference for interacting directly with the native Autodesk Revit .NET API (`RevitAPI.dll` & `RevitAPIUI.dll`).

## 1. Execution Context & Interfaces

### IExternalCommand
```csharp
[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public class MyCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        UIApplication uiApp = commandData.Application;
        UIDocument uiDoc = uiApp.ActiveUIDocument;
        Document doc = uiDoc.Document;

        using (Transaction tx = new Transaction(doc, "Execute MyCommand"))
        {
            tx.Start();
            // Perform modifications
            tx.Commit();
        }

        return Result.Succeeded;
    }
}
```

### IExternalApplication
Initializes ribbon panels, push buttons, and external event hooks during Revit startup (`OnStartup`) and cleanup (`OnShutdown`).

## 2. High-Performance Filtering (FilteredElementCollector)
Always use quick filters first to avoid full element memory instantiation:
```csharp
// Fast collector using Class and Category quick filters
var walls = new FilteredElementCollector(doc)
    .OfClass(typeof(Wall))
    .OfCategory(BuiltInCategory.OST_Walls)
    .WhereElementIsNotElementType()
    .Cast<Wall>()
    .ToList();
```

## 3. Parameter Manipulation
* Prefer `get_Parameter(BuiltInParameter)` over `LookupParameter(string)` for culture-invariant, high-performance lookups.
* Check `.IsReadOnly` before invoking `.Set()`.
* Respect Revit internal units:
  * Length: Decimal Feet
  * Angle: Radians
  * Area: Square Feet
  * Use `UnitUtils.ConvertFromInternalUnits()` and `UnitUtils.ConvertToInternalUnits()` for conversions.

## 4. Geometric & Spatial Extraction
* Set `Options` when calling `element.get_Geometry(options)`:
  * `ComputeReferences = true` (if dimensions or tags will attach to geometry).
  * `DetailLevel = ViewDetailLevel.Fine`.
