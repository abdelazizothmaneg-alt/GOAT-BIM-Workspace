---
name: creating-revit-plugin
description: >-
  Guides the end-to-end creation of Autodesk Revit C# plugins (.NET), including solution scaffolding, .addin manifest
  configuration, Ribbon UI construction (tabs, panels, push buttons), WPF MVVM integration, and deployment packaging.
---

# Creating Revit Plugins (C# / .NET)

Complete guide for scaffolding, coding, and deploying compiled Autodesk Revit add-ins.

## 1. Project Scaffolding & Manifest (.addin)

### Add-in Manifest Structure
Placed in `%APPDATA%\Autodesk\Revit\Addins\<Year>\MyPlugin.addin`:
```xml
<?xml version="1.0" encoding="utf-8"?>
<RevitAddIns>
  <AddIn Type="Application">
    <Name>MyRevitPlugin</Name>
    <Assembly>MyRevitPlugin\MyRevitPlugin.dll</Assembly>
    <FullClassName>MyRevitPlugin.App</FullClassName>
    <ClientId>A1B2C3D4-E5F6-4A5B-8C9D-0E1F2A3B4C5D</ClientId>
    <VendorId>MYCOMPANY</VendorId>
    <VendorDescription>My Company, https://mycompany.com</VendorDescription>
  </AddIn>
</RevitAddIns>
```

## 2. Ribbon UI Construction (IExternalApplication)
```csharp
public class App : IExternalApplication
{
    public Result OnStartup(UIControlledApplication application)
    {
        string tabName = "Custom Tools";
        application.CreateRibbonTab(tabName);
        RibbonPanel panel = application.CreateRibbonPanel(tabName, "BIM Automation");

        string assemblyPath = Assembly.GetExecutingAssembly().Location;
        PushButtonData buttonData = new PushButtonData(
            "cmdSync",
            "Batch Sync",
            assemblyPath,
            "MyRevitPlugin.Commands.SyncCommand")
        {
            ToolTip = "Batch synchronize models and purge unused families.",
            LargeImage = new BitmapImage(new Uri("pack://application:,,,/MyRevitPlugin;component/Resources/icon32.png"))
        };

        panel.AddItem(buttonData);
        return Result.Succeeded;
    }

    public Result OnShutdown(UIControlledApplication application)
    {
        return Result.Succeeded;
    }
}
```

## 3. Asynchronous Operations & ExternalEvents
When running background calculations, network requests, or modeless WPF windows:
* Never modify the Revit database outside the main GUI thread.
* Use `IExternalEventHandler` and `ExternalEvent.Create(handler)` to marshal execution back into the Revit execution context safely.
