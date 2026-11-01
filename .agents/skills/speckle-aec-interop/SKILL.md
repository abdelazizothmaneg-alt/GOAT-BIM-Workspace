---
name: speckle-aec-interop
description: >-
  Architects and automates cross-platform AEC data streams using Speckle's open data platform, Core .NET SDK,
  and GraphQL API, enabling version-controlled BIM exchange between Revit, Rhino, AutoCAD, and Blender without software lock-in.
---

# Speckle AEC Data Interoperability

Cloud data stream architecture for version-controlled, multi-software BIM collaboration via the open-source Speckle platform.

## 1. Speckle Data Hierarchy
* **Server:** Host instance (e.g., `app.speckle.systems` or private enterprise deployment).
* **Project (Stream):** Central repository for a building or master plan.
* **Model (Branch):** Distinct workstreams (e.g., `main`, `facade-options`, `structural-rev02`).
* **Version (Commit):** Immutable point-in-time snapshot containing the root `Base` object tree.

---

## 2. Ingesting & Querying Speckle via GraphQL API

Query project elements and metadata headlessly using lightweight HTTP requests:

```graphql
query GetProjectStream($projectId: String!, $modelId: String!) {
  project(id: $projectId) {
    name
    description
    model(id: $modelId) {
      name
      updatedAt
      versions(limit: 5) {
        items {
          id
          message
          createdAt
          referencedObject
        }
      }
    }
  }
}
```

---

## 3. Speckle Core .NET SDK (C# Automation)

Send and receive native BIM geometry programmatically:

```csharp
using System;
using System.Threading.Tasks;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Models;

namespace ORTools.Interop
{
    public static class SpeckleStreamManager
    {
        public static async Task<string> CommitRevitModelData(string streamId, Base rootData, string commitMessage)
        {
            // Fetch default user account from local Speckle Manager
            Account account = AccountManager.GetDefaultAccount();
            if (account == null)
            {
                throw new InvalidOperationException("No authenticated Speckle account found on local machine.");
            }

            using (Client client = new Client(account))
            {
                // Send object tree to Speckle transport server
                string objectId = await Operations.Send(
                    rootData,
                    new List<ITransport> { new ServerTransport(account, streamId) }
                );

                // Create version commit
                string commitId = await client.CommitCreate(new CommitCreateInput
                {
                    streamId = streamId,
                    branchName = "main",
                    objectId = objectId,
                    message = commitMessage,
                    sourceApplication = "GOAT_BIM_Suite_2027"
                });

                return commitId;
            }
        }
    }
}
```

---

## 4. Multi-Platform Workflow Invariants
* **Revit to Blender / 3ds Max:** Stream architectural massing to Speckle $\rightarrow$ pull into Blender via Speckle connector for photorealistic Cycles rendering.
* **Rhino to Revit:** Compute complex parametric geometry in Grasshopper $\rightarrow$ stream to Speckle branch $\rightarrow$ receive in Revit mapped to native `DirectShape` or `Wall` families.
* **Model Diffing:** Compare object properties between commits to track material changes, area expansions, and parameter revisions automatically.
