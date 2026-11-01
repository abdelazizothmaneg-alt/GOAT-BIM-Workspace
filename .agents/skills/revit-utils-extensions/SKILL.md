---
name: revit-utils-extensions
description: >-
  Leverages modern Revit API extension libraries (Nice3point.Revit.Extensions & Toolkit) for fluent element filtering,
  LINQ integration, unit conversion, and streamlined transaction management.
---

# Revit Utils & Extensions (Nice3point Ecosystem)

Accelerates Revit development by eliminating boilerplate code through modern C# extension methods and fluent wrappers.

## 1. Document & Transaction Extensions
Instead of verbose `Transaction` blocks, use fluent extension methods:
```csharp
using Nice3point.Revit.Extensions;

// Automatic transaction management with rollback on error
document.Modify(doc =>
{
    wall.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS).Set("Updated");
});

// Grouped transactions
document.ModifyGroup(doc =>
{
    // Multiple modifications under a single undo group
});
```

## 2. Element & Parameter Extensions
```csharp
// Safe parameter fetching
string comment = element.GetParameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS).AsString();

// Direct Cast and Type Resolution
var wallType = wall.GetTypeId().ToElement<WallType>(document);

// Fluent Collector Queries
var doors = document.GetInstances(BuiltInCategory.OST_Doors)
    .Where(door => door.LevelId == activeLevel.Id)
    .ToList();
```

## 3. Unit Conversions (UnitUtils Extensions)
Avoid manual conversion calculations:
```csharp
// Convert metric millimeters to internal Revit feet
double lengthInFeet = 3000.0.FromMillimeters();

// Convert internal Revit feet to millimeters
double lengthInMm = wallLength.ToMillimeters();

// Area & Volume conversions
double sqm = internalArea.ToSquareMeters();
```
