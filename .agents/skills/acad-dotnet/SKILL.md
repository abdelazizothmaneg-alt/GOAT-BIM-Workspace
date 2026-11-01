---
name: acad-dotnet
description: >-
  Develops compiled C# / .NET plugins for Autodesk AutoCAD using the managed AutoCAD .NET API (AcCoreMgd, AcDbMgd, AcMgd),
  registering [CommandMethod] commands, managing Database transactions, block references, and editor prompts.
---

# AutoCAD .NET API Development (acad-dotnet)

Engineering guide for creating high-performance, compiled C# add-ins and automation commands for Autodesk AutoCAD.

## 1. Solution Architecture & References
* **Required DLL References** (Set `Copy Local = False`):
  * `AcCoreMgd.dll` (Core application and command engine)
  * `AcDbMgd.dll` (Database services, entities, tables, transactions)
  * `AcMgd.dll` (Main UI, palettes, dialogs)
* **Target Framework:**
  * AutoCAD 2021-2024: `.NET Framework 4.8`
  * AutoCAD 2025+: `.NET 8.0-windows`

## 2. Command Implementation Template
```csharp
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;

[assembly: CommandClass(typeof(MyAutoCADPlugin.Commands))]

namespace MyAutoCADPlugin
{
    public class Commands
    {
        [CommandMethod("CreateLayerAndCircle", CommandFlags.Modal)]
        public void CreateLayerAndCircle()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            using (Transaction tx = db.TransactionManager.StartTransaction())
            {
                // Open BlockTable and ModelSpace for write
                BlockTable bt = (BlockTable)tx.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord btr = (BlockTableRecord)tx.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                // Create geometric entity
                Circle circle = new Circle(new Point3d(0, 0, 0), Vector3d.ZAxis, 500.0);
                
                // Add entity to BlockTableRecord and Transaction
                btr.AppendEntity(circle);
                tx.AddNewlyCreatedDBObject(circle, true);

                // Commit transaction
                tx.Commit();
                ed.WriteMessage("\n[Success] Circle of radius 500 created at origin.\n");
            }
        }
    }
}
```

## 3. Database Invariants
* Always open objects for Read first, upgrading to Write only when modification is guaranteed.
* Dispose transactions cleanly using C# `using` blocks; uncommitted transactions automatically abort, preventing drawing corruption.
