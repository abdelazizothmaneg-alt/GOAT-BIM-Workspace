---
name: revit-testing
description: >-
  Automated testing workflows for Autodesk Revit add-ins using NUnit/xUnit, Revit.TestRunner, and headless CI/CD execution
  to test business logic and geometry operations directly inside the Revit process.
---

# Revit Automated Testing

Methodology and tooling for executing automated unit and integration tests inside the Autodesk Revit runtime.

## 1. The In-Process Testing Challenge
Revit API methods can only be invoked from within the Revit process thread (`Revit.exe`). Standard test runners (like Visual Studio Test Explorer or `dotnet test`) fail unless coupled with an in-process bridge.

## 2. Test Execution Patterns

### Pattern A: In-Process Runner (e.g., Revit.TestRunner / Nice3point.Revit.TestRunner)
* Tests are compiled into an assembly.
* An external Revit add-in or CLI tool loads Revit headlessly (`Revit.exe /language ENU`), triggers an external command that loads the test runner DLL, executes tests via NUnit/xUnit, and outputs XML/JSON test results.

### Pattern B: Architectural Isolation (Mocking & Separation of Concerns)
* Decouple pure business logic (calculations, string formatting, export structures) from Revit API types.
* Isolate domain logic into standard .NET class libraries that can run fast unit tests under `dotnet test` in milliseconds without launching Revit.
* Reserve in-process Revit tests specifically for geometry generation, collector performance, and transaction rollback verification.

## 3. Test Assertion Example
```csharp
[Test]
public void CreatingWall_CalculatesCorrectVolume()
{
    // Arrange & Act
    Wall wall = null;
    RevitTestContext.TestDocument.Modify(doc =>
    {
        Line line = Line.CreateBound(new XYZ(0, 0, 0), new XYZ(10, 0, 0));
        wall = Wall.Create(doc, line, defaultLevelId, false);
    });

    // Assert
    Assert.IsNotNull(wall);
    double length = wall.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH).AsDouble();
    Assert.AreEqual(10.0, length, 1e-4);
}
```
