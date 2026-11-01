---
name: revit-advanced-patterns
description: >-
  Advanced Revit API architectural and engineering patterns: Dynamic Model Update (IUpdater), Extensible Storage schemas (DataStorage),
  silent failure preprocessors (IFailuresPreprocessor), Dockable Panes (IDockablePaneProvider), and custom Selection Filters (ISelectionFilter).
---

# Advanced Revit API Architectural Patterns

Production engineering guide for high-reliability, enterprise-grade Autodesk Revit C# add-ins (`GOAT BIM Suite`).

## 1. Dynamic Model Update (`IUpdater`)

Automatically execute business logic when specific model elements are added, modified, or deleted:

```csharp
using System;
using Autodesk.Revit.DB;

namespace ORTools.Updaters
{
    public class RoomFinishWatcherUpdater : IUpdater
    {
        private static UpdaterId _updaterId;

        public RoomFinishWatcherUpdater(AddInId addinId)
        {
            _updaterId = new UpdaterId(addinId, new Guid("8E14B3A1-7F22-4D90-9B21-44A5C1089D12"));
        }

        public void Execute(UpdaterData data)
        {
            Document doc = data.GetDocument();
            
            // Iterate over elements modified in the active transaction
            foreach (ElementId id in data.GetModifiedElementIds())
            {
                Element elem = doc.GetElement(id);
                if (elem is Wall wall)
                {
                    // Automatically synchronize wall finish parameter or invalidate cache
                }
            }
        }

        public UpdaterId GetUpdaterId() => _updaterId;
        public ChangePriority GetChangePriority() => ChangePriority.FloorsRoofsStructuralWalls;
        public string GetUpdaterName() => "GOAT Room Finish Watcher";
        public string GetAdditionalInformation() => "Watches walls and rooms to update finish schedules dynamically.";

        public static void Register(AddInId addinId, Document doc)
        {
            var updater = new RoomFinishWatcherUpdater(addinId);
            if (!UpdaterRegistry.IsUpdaterRegistered(updater.GetUpdaterId()))
            {
                UpdaterRegistry.RegisterUpdater(updater, true);
                
                // Add trigger for Wall modifications (parameter or geometry)
                ElementCategoryFilter wallFilter = new ElementCategoryFilter(BuiltInCategory.OST_Walls);
                UpdaterRegistry.AddTrigger(updater.GetUpdaterId(), wallFilter, Element.GetChangeTypeGeometry());
            }
        }
    }
}
```

---

## 2. Extensible Storage (`Schema` & `DataStorage`)

Store persistent, proprietary plugin metadata, license keys, or configuration JSON inside `.rvt` project files safely without project parameters:

```csharp
using System;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;

namespace ORTools.Storage
{
    public static class GoatProjectStorage
    {
        private static readonly Guid SchemaGuid = new Guid("4A2C11D0-9B81-4C3E-A528-71C60E2DF391");

        public static Schema GetOrCreateSchema()
        {
            Schema schema = Schema.Lookup(SchemaGuid);
            if (schema != null) return schema;

            SchemaBuilder builder = new SchemaBuilder(SchemaGuid);
            builder.SetSchemaName("GoatProjectConfig");
            builder.SetReadAccessLevel(AccessLevel.Public);
            builder.SetWriteAccessLevel(AccessLevel.Vendor);
            builder.SetVendorId("GOATBIM");

            // Define fields
            builder.AddSimpleField("ConfigJson", typeof(string));
            builder.AddSimpleField("LastSyncTimestamp", typeof(string));

            return builder.Finish();
        }

        public static void SaveConfig(Document doc, string jsonConfig)
        {
            Schema schema = GetOrCreateSchema();
            
            // Find existing DataStorage element or create new
            DataStorage storageElem = new FilteredElementCollector(doc)
                .OfClass(typeof(DataStorage))
                .Cast<DataStorage>()
                .Find(ds => ds.GetEntity(schema).IsValid());

            using (Transaction tx = new Transaction(doc, "Save GOAT Config"))
            {
                tx.Start();
                if (storageElem == null) storageElem = DataStorage.Create(doc);

                Entity entity = new Entity(schema);
                entity.Set("ConfigJson", jsonConfig);
                entity.Set("LastSyncTimestamp", DateTime.UtcNow.ToString("o"));

                storageElem.SetEntity(entity);
                tx.Commit();
            }
        }
    }
}
```

---

## 3. Silent Warning Handling (`IFailuresPreprocessor`)

Prevent blocking modal popups (such as duplicate Mark warnings, room overlap warnings, or unjoined wall alerts) from interrupting batch automation jobs:

```csharp
using Autodesk.Revit.DB;

namespace ORTools.Failures
{
    public class SilentFailuresPreprocessor : IFailuresPreprocessor
    {
        public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
        {
            var failureMessages = failuresAccessor.GetFailureMessages();
            foreach (FailureMessageAccessor msg in failureMessages)
            {
                FailureSeverity severity = msg.GetSeverity();
                
                // Automatically dismiss non-critical warnings
                if (severity == FailureSeverity.Warning)
                {
                    failuresAccessor.DeleteWarning(msg);
                }
                else if (severity == FailureSeverity.Error && msg.HasResolutions())
                {
                    // Attempt default resolution if available
                    failuresAccessor.ResolveFailure(msg);
                    return FailureProcessingResult.ProceedWithCommit;
                }
            }

            return FailureProcessingResult.Continue;
        }
    }
}
```

*Usage inside Transaction:*
```csharp
using (Transaction tx = new Transaction(doc, "Batch Wall Generation"))
{
    FailureHandlingOptions options = tx.GetFailureHandlingOptions();
    options.SetFailuresPreprocessor(new SilentFailuresPreprocessor());
    tx.SetFailureHandlingOptions(options);

    tx.Start();
    // Batch operations...
    tx.Commit();
}
```

---

## 4. Dockable Panes (`IDockablePaneProvider`)

Build permanent, dockable toolbars (like the Properties or Project Browser palettes) using WPF `Page`:

```csharp
public class GoatDockablePage : Page, IDockablePaneProvider
{
    public void SetupDockablePane(DockablePaneSetupData data)
    {
        data.FrameworkElement = this;
        data.InitialState = new DockablePaneState
        {
            DockPosition = DockPosition.Right,
            MinimumWidth = 320
        };
    }
}
```
*Register in `App.OnStartup`:*
```csharp
DockablePaneId paneId = new DockablePaneId(new Guid("F9B1E4A0-1122-43D4-8899-77AABBCCDDEE"));
app.RegisterDockablePane(paneId, "GOAT BIM Workspace", new GoatDockablePage());
```
