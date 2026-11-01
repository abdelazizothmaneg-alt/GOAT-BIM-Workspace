---
name: mep-clash-auto-rerouter
description: Automatically resolves and reroutes clashing MEP pipes, ducts, and cable trays around structural obstacles (drop beams, columns) using 3D A* pathfinding and algorithmic fitting insertion in the Revit API.
---

# MEP Clash Auto-Rerouter (إعادة التوجيه الآلي لتعارضات الـ MEP)

A computational MEP coordination and algorithmic pathfinding skill. Rather than simply detecting clashes, it autonomously resolves geometric collisions between mechanical/electrical/plumbing distribution runs and structural elements (drop beams, concrete columns, shear walls) using **3D A* Search** and automatically instantiates clean detour curves and fittings via the **Autodesk Revit API**.

---

## 1. Autonomous Rerouting Architecture

```
[Clash Interference Detected (Pipe / Duct intersects Drop Beam)]
                                │
                                ▼
    [1. Spatial Discretization & Obstacle Voxelization]
    - Bounding Box zone around collision: [X_min, Y_min, Z_min] to [X_max...]
    - Subdivide into 3D grid cells (e.g., 50mm resolution)
    - Mark structural geometry cells as BLOCKED (Infinite cost)
                                │
                                ▼
    [2. 3D A* Pathfinding with Engineering Cost Function]
    - Heuristic: Euclidean distance to reconnection target
    - Penalty: Direction changes (minimize elbow counts)
    - Constraint: Minimum gravity slope for drainage (e.g. 1% to 2% fall)
                                │
                                ▼
    [3. Path Smoothing & Orthogonal Snap]
    - Clamp waypoint angles to standard MEP fittings (45° and 90° elbows)
                                │
                                ▼
    [4. Revit API Geometry Modification (.NET C#)]
    - Split original Pipe/Duct curve at clearance boundaries
    - Create detour straight segments via Pipe.Create / Duct.Create
    - Insert standard elbows via doc.Create.NewElbowFitting()
```

---

## 2. 3D A* Pathfinding Engine (Python Prototype)

```python
import heapq
import numpy as np

class Node:
    def __init__(self, pos, parent=None, g=0, h=0):
        self.pos = pos
        self.parent = parent
        self.g = g # Cost from start
        self.h = h # Heuristic to goal
        self.f = g + h

    def __lt__(self, other):
        return self.f < other.f

def astar_3d_mep_route(start_xyz, goal_xyz, blocked_grid, grid_res: float = 0.10):
    """
    Finds collision-free path for pipe/duct around structural obstacles.
    """
    open_set = []
    closed_set = set()

    start_node = Node(start_xyz, g=0, h=np.linalg.norm(np.array(goal_xyz) - np.array(start_xyz)))
    heapq.heappush(open_set, start_node)

    # 6-directional orthogonal movements: +/- X, +/- Y, +/- Z
    directions = [
        (1, 0, 0), (-1, 0, 0),
        (0, 1, 0), (0, -1, 0),
        (0, 0, 1), (0, 0, -1)
    ]

    while open_set:
        current = heapq.heappop(open_set)

        # Check goal reached (within 1 grid step)
        if np.linalg.norm(np.array(current.pos) - np.array(goal_xyz)) < grid_res:
            path = []
            while current:
                path.append(current.pos)
                current = current.parent
            return path[::-1] # Return start-to-goal trajectory

        closed_set.add(tuple(current.pos))

        for dx, dy, dz in directions:
            neighbor_pos = (
                round(current.pos[0] + dx * grid_res, 3),
                round(current.pos[1] + dy * grid_res, 3),
                round(current.pos[2] + dz * grid_res, 3)
            )

            if neighbor_pos in closed_set or neighbor_pos in blocked_grid:
                continue

            # Additional turn penalty: Penalize direction changes to prevent snake-like paths
            turn_penalty = 0.5 if current.parent and (np.array(neighbor_pos) - np.array(current.pos)).tolist() != (np.array(current.pos) - np.array(current.parent.pos)).tolist() else 0.0

            g_cost = current.g + grid_res + turn_penalty
            h_cost = np.linalg.norm(np.array(neighbor_pos) - np.array(goal_xyz))
            neighbor_node = Node(neighbor_pos, current, g_cost, h_cost)

            heapq.heappush(open_set, neighbor_node)

    return None # No feasible path without structural penetration
```

---

## 3. Revit API Transaction Code Recipe (.NET C#)

Snippet illustrating splitting a clashing pipe and placing detour elbows:

```csharp
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;

namespace MepClashAutomation
{
    public static class RerouteApplier
    {
        public static void ApplyPipeDetour(
            Document doc,
            Pipe originalPipe,
            XYZ detourStartPoint,
            XYZ detourMidPoint,
            XYZ detourEndPoint)
        {
            using (Transaction t = new Transaction(doc, "Auto-Reroute Clashing Pipe"))
            {
                t.Start();

                ElementId sysTypeId = originalPipe.MEPSystem.GetTypeId();
                ElementId pipeTypeId = originalPipe.GetTypeId();
                ElementId levelId = originalPipe.ReferenceLevel.Id;

                // 1. Create detour segments below beam soffit
                Pipe detourSeg1 = Pipe.Create(doc, sysTypeId, pipeTypeId, levelId, detourStartPoint, detourMidPoint);
                Pipe detourSeg2 = Pipe.Create(doc, sysTypeId, pipeTypeId, levelId, detourMidPoint, detourEndPoint);

                // 2. Connect fittings
                Connector c1 = GetClosestConnector(detourSeg1, detourMidPoint);
                Connector c2 = GetClosestConnector(detourSeg2, detourMidPoint);

                if (c1 != null && c2 != null)
                {
                    // Inserts native Revit elbow
                    FamilyInstance elbow = doc.Create.NewElbowFitting(c1, c2);
                }

                t.Commit();
            }
        }

        private static Connector GetClosestConnector(Pipe pipe, XYZ pt)
        {
            Connector closest = null;
            double minDist = double.MaxValue;
            foreach (Connector c in pipe.ConnectorManager.Connectors)
            {
                double d = c.Origin.DistanceTo(pt);
                if (d < minDist) { minDist = d; closest = c; }
            }
            return closest;
        }
    }
}
```

---

## 4. Engineering Constraints & Rules
- **Minimum Pipe Center-to-Center Clearances**: Enforce at least $150\text{ mm}$ clear between hot water supply and chilled water return to accommodate fiberglass insulation jackets.
- **Gravity Invert Elevations**: Never route drainage pipes upward; if a beam blocks a sanitary run, flag for a structural web sleeve instead of an impossible upward siphon trap.
