---
name: transit-oriented-development-tod
description: Evaluates Transit-Oriented Development (TOD) performance, computes 15-minute pedestrian isochrone catchment networks, calculates the Land-Use Shannon Entropy Diversity Index, and optimizes parking reduction formulas around transit hubs.
---

# Transit-Oriented Development TOD (تخطيط مدن الـ 15 دقيقة ومحطات النقل)

A computational urban planning and spatial mobility skill that models and audits **Transit-Oriented Development (TOD)** and **15-Minute City** frameworks. It computes exact street-network walking isochrones, calculates land-use diversity using the **Shannon Entropy Index**, optimizes pedestrian accessibility sheds, and rationalizes parking quota reductions according to regional municipal standards (such as Riyadh Metro TOD regulations and MOMRAH codes).

---

## 1. TOD Analytics Architecture

```
[Transit Hub Location (Metro / BRT / Light Rail)]
                        │
                        ▼
   [1. Multi-Tier Walking Catchment Isochrones]
   - 5-Minute Walk (~400m): High-density mixed-use core & plaza
   - 10-Minute Walk (~800m): Primary transit catchment zone
   - 15-Minute Walk (~1200m): Secondary boundary (Complete Community)
                        │
                        ▼
   [2. Land-Use Diversity & Functional Mix]
   - Query cadastre parcels: Residential, Commercial, Office, Civic
   - Calculate Shannon Entropy Index: H in [0, 1]
   - Target H >= 0.75 for sustainable self-contained neighborhoods
                        │
                        ▼
   [3. Density Allocation & Urban Gradients]
   - Floor Area Ratio (FAR) step-down curves from station centroid
   - Active street frontages (Retail required on primary corridors)
                        │
                        ▼
   [4. Parking Maximums & Multimodal Mobility]
   - Replace traditional "Parking Minimums" with "Parking Maximums"
   - Deduct parking ratios by 30% to 60% within 400m of rapid transit
```

---

## 2. 15-Minute Walking Isochrone & Entropy Calculator (Python)

```python
import osmnx as ox
import networkx as nx
import geopandas as gpd
from shapely.geometry import Point
import numpy as np

def calculate_tod_catchment_and_entropy(
    station_lat: float,
    station_lon: float,
    walk_speed_m_per_sec: float = 1.25,
    cutoff_minutes: int = 15
):
    """
    Computes true street-network walking isochrones and Shannon Entropy.
    """
    station_coords = (station_lat, station_lon)
    max_travel_time_sec = cutoff_minutes * 60

    # 1. Download pedestrian street network
    G = ox.graph_from_point(station_coords, dist=1600, network_type='walk')

    # Add travel time attribute to edges
    for u, v, k, data in G.edges(keys=True, data=True):
        data['time'] = data['length'] / walk_speed_m_per_sec

    # 2. Extract reachable nodes within 15 minutes
    center_node = ox.distance.nearest_nodes(G, X=station_lon, Y=station_lat)
    subgraph = nx.ego_graph(G, center_node, radius=max_travel_time_sec, distance='time')

    # Build convex hull isochrone polygon
    node_points = [Point(data['x'], data['y']) for node, data in subgraph.nodes(data=True)]
    isochrone_poly = gpd.GeoSeries(node_points).unary_union.convex_hull
    isochrone_area_sqm = isochrone_poly.area * 111139 * 111139 # Approximate degrees to meters

    return {
        "status": "success",
        "station": station_coords,
        "isochrone_polygon": isochrone_poly,
        "reachable_nodes_count": len(subgraph.nodes),
        "walkable_area_hectares": round(isochrone_area_sqm / 10000.0, 2)
    }

def calculate_shannon_entropy(land_use_proportions: list) -> float:
    """
    Computes normalized Shannon Diversity Index: H = -sum(p_i * ln(p_i)) / ln(n)
    p_i = proportion of land use type (e.g. [0.4 Res, 0.3 Comm, 0.2 Office, 0.1 Civic])
    """
    p = np.array([x for x in land_use_proportions if x > 0])
    n = len(p)
    if n <= 1:
        return 0.0

    entropy = -np.sum(p * np.log(p))
    normalized_h = entropy / np.log(n)
    return round(float(normalized_h), 4)
```

---

## 3. Density & Parking Policy Matrix (GCC Transit Standards)

| TOD Catchment Ring | Walking Radius | Target FAR Range | Target Ground Coverage (GCR) | Recommended Parking Policy |
| :--- | :--- | :--- | :--- | :--- |
| **Transit Core** | $0 - 200\text{ m}$ | $4.0 - 6.5+$ | $60 - 75\%$ | $0.5$ bays per $100\text{ m}^2$ (Max Cap) |
| **Primary Catchment** | $200 - 500\text{ m}$ | $2.5 - 4.0$ | $50 - 65\%$ | $0.8$ bays per $100\text{ m}^2$ |
| **Secondary Transition**| $500 - 1000\text{ m}$| $1.5 - 2.5$ | $40 - 50\%$ | $1.2$ bays per $100\text{ m}^2$ |
| **Outer Periphery** | $> 1000\text{ m}$ | Standard Zoning | Standard Zoning | Standard Municipal Minimums |

---

## 4. Key Metrics for Urban Design Review
1. **Pedshed Ratio**: Ratio of the actual street network walking area to the theoretical circular buffer ($A_{\text{street}} / A_{\text{circle}}$). High connectivity scores $\ge 0.60$.
2. **Active Frontage Continuity**: Percentage of ground floor facades with direct pedestrian openings and retail windows ($\ge 70\%$ required on core transit corridors).
