---
name: revit-multi-version-configuration
description: >-
  Configures multi-version targeting across Autodesk Revit releases (Revit 2021-2024 on .NET Framework 4.8 vs. Revit 2025-2026+ on .NET 8),
  managing conditional compilation, API breaking changes, and build configurations.
---

# Revit Multi-Version Configuration

Architectural guidelines for building and publishing a single Revit add-in codebase that targets multiple major Revit versions simultaneously.

## 1. The .NET Framework vs. .NET 8 Paradigm Shift
* **Revit 2021 - 2024:** Runs on `.NET Framework 4.8`.
* **Revit 2025+:** Migrated to `.NET 8 (Core)`.
* **Strategy:** Use conditional compilation symbols (`REVIT2021`, `REVIT2022`, ..., `REVIT2025_OR_GREATER`).

## 2. Conditional Compilation Configuration (`Directory.Build.props`)
```xml
<Project>
  <PropertyGroup>
    <Configurations>Debug R21;Debug R22;Debug R23;Debug R24;Debug R25;Debug R26;Release R21;Release R22;Release R23;Release R24;Release R25;Release R26</Configurations>
  </PropertyGroup>

  <!-- Revit 2024 configuration -->
  <PropertyGroup Condition="$(Configuration.Contains('R24'))">
    <RevitVersion>2024</RevitVersion>
    <TargetFramework>net48</TargetFramework>
    <DefineConstants>$(DefineConstants);REVIT2024</DefineConstants>
  </PropertyGroup>

  <!-- Revit 2025 configuration -->
  <PropertyGroup Condition="$(Configuration.Contains('R25'))">
    <RevitVersion>2025</RevitVersion>
    <TargetFramework>net8.0-windows</TargetFramework>
    <DefineConstants>$(DefineConstants);REVIT2025;REVIT2025_OR_GREATER</DefineConstants>
  </PropertyGroup>
</Project>
```

## 3. Handling Breaking API Changes in Code
```csharp
// Example: UnitTypeId vs. DisplayUnitType (Revit 2021+ unit changes)
#if REVIT2021_OR_GREATER
    ForgeTypeId unitTypeId = UnitTypeId.Millimeters;
    double val = UnitUtils.ConvertFromInternalUnits(internalVal, unitTypeId);
#else
    DisplayUnitType dut = DisplayUnitType.DUT_MILLIMETERS;
    double val = UnitUtils.ConvertFromInternalUnits(internalVal, dut);
#endif

// Example: Parameter lookup changes in .NET 8 / Revit 2025
#if REVIT2025_OR_GREATER
    // .NET 8 specific language features and API calls
#endif
```
