---
name: urban-pedestrian-crowd-simulation
description: Simulates pedestrian agent behavior, bottleneck identification, crowd density heatmaps, Level of Service (Fruin LOS) ratings, and emergency evacuation egress times in public plazas, transit stations, and stadium precincts.
---

# Urban Pedestrian Crowd Simulation (محاكاة حركة الحشود والإخلاء في الساحات والمحطات)

An urban safety and pedestrian dynamics skill utilizing **Agent-Based Modeling (ABM)** and **Social Force Models (Helbing)**. It simulates the movement of thousands of autonomous pedestrian agents through public plazas, metro station concourses, airport terminals, and commercial promenades, identifying structural bottlenecks and verifying compliance with life-safety evacuation codes (SBC 801 / NFPA 101).

---

## 1. Crowd Simulation Workflow

```
[CAD / BIM Spatial Boundary Layout (Walls, Gates, Obstacles)]
                            │
                            ▼
     [1. Navigation Mesh & Vector Field Generation]
     - Discretize walkable floorspace into navmesh walkable polygons
     - Calculate shortest path Dijkstra / A* distance potential fields
                            │
                            ▼
     [2. Multi-Agent Social Force Engine]
     - Attractive Force: Desired destination velocity vector (v_0 ~ 1.3 m/s)
     - Repulsive Force: Personal space buffer avoiding other agents (d >= 0.8m)
     - Boundary Force: Distance buffer keeping agents away from walls/corners
                            │
                            ▼
     [3. Quantitative Metric Extraction]
     - Fruin Level of Service (LOS A through F)
     - Peak Density Heatmap (Persons per square meter - P/m2)
     - Flow Rate Capacity (Persons per meter width per minute - P/m/min)
                            │
                            ▼
     [4. Emergency Evacuation Time (Egress Audit)]
     - Evacuation clearance time (Total egress <= 8.0 minutes for stadiums)
     - Door width throat capacity checks
```

---

## 2. Fruin Level of Service (LOS) Standards for Walkways

| LOS Level | Space per Pedestrian ($m^2/\text{ped}$) | Flow Rate ($P/\text{min}/m$) | Environmental Condition |
| :--- | :--- | :--- | :--- |
| **LOS A** | $\ge 3.25$ | $\le 23$ | Free pedestrian flow, unhindered overtaking. |
| **LOS B** | $2.30 - 3.25$ | $23 - 33$ | Normal walking speed, occasional minor conflicts. |
| **LOS C** | $1.40 - 2.30$ | $33 - 49$ | Crowded, walking speed restricted, difficult overtaking. |
| **LOS D** | $0.95 - 1.40$ | $49 - 66$ | Severely restricted, shuffle gait, high collision risk. |
| **LOS E** | $0.50 - 0.95$ | $66 - 82$ | Intermittent stoppages, critical congestion. |
| **LOS F** | $< 0.50$ | Variable / Zero | Complete gridlock, stampede risk, crush potential. |

---

## 3. Python Social Force Simulation Script

```python
import numpy as np

class PedestrianAgent:
    def __init__(self, agent_id: int, pos: np.ndarray, goal: np.ndarray, desired_speed: float = 1.34):
        self.id = agent_id
        self.pos = np.array(pos, dtype=float)
        self.velocity = np.zeros(2, dtype=float)
        self.goal = np.array(goal, dtype=float)
        self.desired_speed = desired_speed
        self.radius = 0.25 # meters (body radius)
        self.mass = 75.0 # kg
        self.tau = 0.5 # relaxation time (seconds)

    def calculate_desired_force(self):
        direction = self.goal - self.pos
        dist = np.linalg.norm(direction)
        if dist < 0.2:
            return np.zeros(2)
        desired_vel = (direction / dist) * self.desired_speed
        return (desired_vel - self.velocity) / self.tau

    def calculate_agent_repulsion(self, other_agent):
        diff = self.pos - other_agent.pos
        dist = np.linalg.norm(diff)
        min_dist = self.radius + other_agent.radius
        if dist < 1e-4 or dist > 2.0:
            return np.zeros(2)
        # Exponential repulsive force (Helbing model)
        A = 2000.0 # Repulsion strength (Newtons)
        B = 0.08   # Interaction range (meters)
        force_magnitude = A * np.exp((min_dist - dist) / B)
        return (diff / dist) * force_magnitude

def compute_crowd_density(agent_positions: list, area_sqm: float) -> dict:
    density = len(agent_positions) / area_sqm
    los_grade = "LOS A" if density < 0.3 else "LOS C" if density < 0.7 else "LOS E" if density < 1.5 else "LOS F (CRITICAL)"
    return {
        "pedestrian_count": len(agent_positions),
        "density_p_per_sqm": round(density, 2),
        "fruin_los": los_grade,
        "hazard_flag": density > 1.5
    }
```

---

## 4. Key Design Recommendations for Plazas & Metro Stations
1. **Throat Widths at Entrances**: Clear walkway widths should provide at least $1.0\text{ m}$ of clear opening per $80\text{ persons/minute}$ anticipated peak load.
2. **Elimination of Sharp $90^\circ$ Choke Points**: Chamfer interior corridor corners with $45^\circ$ splays to reduce lateral shear turbulence in dense crowd waves.
