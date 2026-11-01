---
name: complex-typology-flow-orchestrator
description: Architects and audits hyper-complex building typologies (Hospitals, Airport Terminals, Luxury Hotels), enforcing infection control clean/dirty zoning (FGI), IATA security gates, and FOH/BOH separation.
---

# Complex Typology Flow Orchestrator

Specialized master architectural skill for designing and auditing hyper-complex functional typologies: Healthcare facilities (FGI Guidelines), International Airport Terminals (IATA ADRM), and Luxury Hotels. Enforces non-intersecting circulation flows, sterile zoning, and security boundaries.

---

## 1. Healthcare Circulation & Infection Control (FGI Guidelines)

In modern hospital architecture, the primary goal is **Zero Cross-Contamination**:

```
[ Unrestricted Zone ] ──> [ Semi-Restricted ] ──> [ Restricted Sterile Core ]
(Public Corridors,        (Clean scrubs,         (Operating Rooms, Clean Supply)
 Family Waiting)           Pre-op holding)        Positive Pressure (+2.5 Pa to +5.0 Pa)
                                │
                                ▼
                     [ Soiled / Dirty Corridor ] ──> Biohazard Disposal, Dirty Utility
                     Negative Pressure (-2.5 Pa)
```

### Strict Spatial Rules:
1. **Operating Rooms (OR Suites)**: Must maintain positive air pressure ($+2.5\text{ Pa}$ minimum) relative to semi-restricted scrub corridors to prevent airborne microbe entry.
2. **Airborne Infectious Isolation Rooms (AIIR)**: Negative air pressure ($-2.5\text{ Pa}$) with dedicated anteroom airlocks.
3. **Dual Corridor System**: Clean surgical supplies and patients enter through the "Clean Core"; contaminated instruments, soiled linens, and waste exit through a completely isolated "Dirty Corridor" leading to decontamination elevators.

---

## 2. Airport Terminal Topology (IATA ADRM Standards)

```
[ City / Landside Curb ] ──> [ Check-In & Bag Drop ] ──> [ Security Screening (K-Point) ] ──> [ Airside Concourse / Duty Free ]
                                                                                                   │
                                            ┌──────────────────────────────────────────────────────┴───────────────────────┐
                                            ▼                                                                              ▼
                           [ Domestic Departure Gates ]                                                   [ Emigration / International Gates ]
```

### Separation Protocols:
- **Arriving vs. Departing Passenger Flows**: Arriving international passengers must never cross paths with departing passengers without passing border control / immigration.
- **Baggage Handling System (BHS)**: Underground high-speed sorting loops connected to explosive detection system (EDS) screening matrix.

---

## 3. Luxury Hospitality: FOH vs. BOH Isolation

In 5-star and 6-star luxury hotels (Four Seasons, Ritz-Carlton, Aman):
- **Front of House (FOH)**: Pristine guest experience (Lobby, Guest Corridors, Fine Dining, Ballrooms).
- **Back of House (BOH)**: Completely invisible service operations (Housekeeping pantries, room service staging, laundry chutes, staff dining, engineering workshops).
- **Rule of Invisibility**: No service trolley, soiled linen bin, or maintenance technician may ever transit through a primary FOH guest corridor.

---

## 4. Python Topological Flow & Adjacency Auditor

```python
"""
Hospital & Airport Spatial Flow Cross-Contamination Auditor
Evaluates architectural room adjacency graphs for prohibited path crossings.
"""

from typing import Dict, List, Set

PROHIBITED_TRANSITIONS = {
    ("Soiled_Utility", "Clean_Core"),
    ("Dirty_Corridor", "Sterile_OR"),
    ("Landside_Public", "Airside_Sterile"),
    ("BOH_Kitchen_Service", "FOH_Executive_Lounge")
}

def audit_circulation_graph(room_connections: Dict[str, List[str]], room_types: Dict[str, str]) -> List[str]:
    violations = []
    
    for origin_room, neighbors in room_connections.items():
        type_a = room_types.get(origin_room, "Unknown")
        for destination_room in neighbors:
            type_b = room_types.get(destination_room, "Unknown")
            
            # Check direct prohibited transitions
            if (type_a, type_b) in PROHIBITED_TRANSITIONS or (type_b, type_a) in PROHIBITED_TRANSITIONS:
                violations.append(f"CRITICAL CONTAMINATION: Doorway connecting '{origin_room}' ({type_a}) directly to '{destination_room}' ({type_b}) violates architectural separation protocol!")
                
    return violations

if __name__ == "__main__":
    # Test hospital layout with intentional error
    rooms = {
        "OR_Suite_01": "Sterile_OR",
        "Clean_Corridor_A": "Clean_Core",
        "Janitor_Soiled_Room": "Soiled_Utility"
    }
    connections = {
        "OR_Suite_01": ["Clean_Corridor_A"],
        "Janitor_Soiled_Room": ["Clean_Corridor_A"] # Error! Direct connection to clean corridor
    }
    
    audit_results = audit_circulation_graph(connections, rooms)
    print(f"Audited Hospital Circulation: {len(audit_results)} violations found.")
    for v in audit_results:
        print("  -> " + v)
```

---

## 5. Deliverables Checklist
1. **Flow Diagram Matrix**: Color-coded circulation diagrams tracing clean vs. dirty, landside vs. airside, and FOH vs. BOH vectors.
2. **Pressure Cascade Schedule**: Room-by-room differential air pressure schedule ($+ / - / \text{Neutral}$) coordinated with MEP mechanical engineers.
