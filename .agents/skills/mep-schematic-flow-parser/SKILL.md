---
name: mep-schematic-flow-parser
description: Parses 2D MEP single-line diagrams (SLD), riser diagrams, and piping/instrumentation flowcharts (P&ID), constructing connected graph networks for Revit MEP system generation and schematic verification.
---

# MEP Schematic Flow Parser (قراءة وتحليل مخططات الـ MEP الأحادية والرايزرات)

An MEP systems engineering and computer vision skill that ingests 2D single-line diagrams (SLD), riser diagrams, and piping/instrumentation flowcharts (P&ID). It traces connected circuits, equipment hierarchies, and riser penetrations, constructing a directed topological graph suitable for automated generation or verification of native Autodesk Revit `MEPSystem` instances.

---

## 1. Flow Extraction Pipeline

```
[2D MEP Schematic (DWG / PDF / SLD)]
                  │
                  ▼
   [1. Legend & Symbol Recognition]
   - Chillers, Pumps, AHUs, FCUs, VAV boxes, Air Terminals
   - Electrical Switchboards: MDB, SMDB, DB, Transformers
   - Valves: Balancing valves, 2-way/3-way control valves
                  │
                  ▼
   [2. Connectivity Tracing & Graph Construction]
   - Line tracing with crossing jump suppression (arc bridges)
   - Directed flow graph (Source -> Distribution -> Terminal)
   - Graph representation via NetworkX (Nodes & Edges)
                  │
                  ▼
   [3. Riser Continuity Across Multi-Story Drawings]
   - Match riser tags (e.g., "R-CHW-01", "R-ELE-02") across floor sheets
   - Calculate vertical floor-to-floor elevation offsets
                  │
                  ▼
   [4. Revit MEP System Instantiation (.NET C#)]
   - MechanicalSystem / PipingSystem creation
   - Populate electrical distribution board loads
```

---

## 2. Topological Graph Representation (Python / NetworkX)

```python
import networkx as nx

def build_chilled_water_schematic_graph():
    """
    Constructs a directed flow graph for a central chilled water system.
    """
    G = nx.DiGraph()

    # Define primary plant equipment (Sources)
    G.add_node("CH-01", type="Chiller", capacity_tons=450, supply_temp_c=6.5)
    G.add_node("CH-02", type="Chiller", capacity_tons=450, supply_temp_c=6.5)
    G.add_node("CHWP-01", type="Primary_Pump", flow_gpm=1080)
    G.add_node("CHWP-02", type="Primary_Pump", flow_gpm=1080)
    G.add_node("HEADER-SUPPLY", type="Distribution_Header")

    # Connect plant loops
    G.add_edge("CH-01", "HEADER-SUPPLY", fluid="CHW_Supply", pipe_diameter_mm=250)
    G.add_edge("CH-02", "HEADER-SUPPLY", fluid="CHW_Supply", pipe_diameter_mm=250)

    # Risers to upper floors
    G.add_node("RISER-CHW-01", type="Vertical_Riser", served_floors=["L1", "L2", "L3"])
    G.add_edge("HEADER-SUPPLY", "RISER-CHW-01", fluid="CHW_Supply", pipe_diameter_mm=200)

    # Floor Air Handlers (Terminals)
    G.add_node("AHU-L1-01", type="Air_Handling_Unit", airflow_cfm=12000, cooling_load_kw=140)
    G.add_edge("RISER-CHW-01", "AHU-L1-01", fluid="CHW_Supply", pipe_diameter_mm=100)

    return G
```

---

## 3. Revit MEP System Creation (.NET C#)

```csharp
using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;

namespace MepAutomation
{
    public static class PipingSystemBuilder
    {
        public static void CreateHydronicSupplySystem(
            Document doc,
            PipingSystemType systemType,
            Connector baseEquipmentConnector)
        {
            using (Transaction t = new Transaction(doc, "Create CHW Supply System"))
            {
                t.Start();

                // Create the native PipingSystem
                PipingSystem pipeSys = doc.Create.NewPipingSystem(
                    systemType.Id,
                    "CHW_Supply_System_01",
                    baseEquipmentConnector
                );

                t.Commit();
            }
        }
    }
}
```
