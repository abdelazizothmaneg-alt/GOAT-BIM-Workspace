---
name: space-syntax-urban-analytics
description: Performs mathematical urban network analysis using Space Syntax theory and depthmapX principles, calculating Integration, Normalized Angular Choice (NACH), isovist visual fields, and pedestrian movement forecasting in street networks and public plazas.
---

# Space Syntax Urban Analytics (تحليل الفضاء الحضري وتدفقات المشاة)

An advanced computational urban planning and spatial science skill based on **Space Syntax** theory and **depthmapX** methodologies. It mathematically analyzes the topological and geometric configurations of urban street networks and public realms to forecast pedestrian volume, socio-spatial integration, visual exposure (Isovist fields), and optimal retail locations.

---

## 1. Space Syntax Core Metrics

| Metric | Full Name | Mathematical / Spatial Meaning | Urban Application |
| :--- | :--- | :--- | :--- |
| **Integration (HH / NAIN)** | Normalized Angular Integration | Closeness centrality: How accessible a street is from all other streets. | Predicts destination potential ("To-Movement"), high social encounter, commercial frontage. |
| **Choice (NACH)** | Normalized Angular Choice | Betweenness centrality: How often a segment lies on the shortest angular path. | Predicts transit volume ("Through-Movement"), arterial pedestrian flow corridors. |
| **Isovist Area** | Visual Catchment Field | 2D polygon of all points visible from a single viewpoint ($360^\circ$). | Public safety, surveillance (Jane Jacobs' "Eyes on the Street"), plaza openness. |
| **Connectivity** | Local Degree Centrality | Number of immediately adjacent street intersections directly linked. | Wayfinding, permeability, grid legibility. |

---

## 2. Urban Network Graph Calculation (Python & NetworkX / OSMnx)

```python
import networkx as nx
import numpy as np

def compute_urban_space_syntax_metrics(street_network_graph):
    """
    Computes closeness (Integration) and betweenness (Choice) centrality
    on a street centerline graph.
    """
    G = street_network_graph

    # 1. Integration proxy (Closeness Centrality weighted by street length)
    integration_dict = nx.closeness_centrality(G, distance='length')

    # 2. Choice proxy (Betweenness Centrality weighted by street length)
    choice_dict = nx.betweenness_centrality(G, weight='length', normalized=True)

    # 3. Classify segments into high/medium/low pedestrian potential
    results = {}
    for node in G.nodes():
        results[node] = {
            "integration_score": round(integration_dict[node], 4),
            "choice_score": round(choice_dict[node], 6),
            "pedestrian_vitality_rank": "HIGH" if integration_dict[node] > 0.65 else "MEDIUM"
        }

    return results
```

---

## 3. Urban Planning Best Practices
1. **Pedestrian Catchment Radii**: Analyze metric Choice at local radii ($R = 400\text{ m}$ / 5-minute walking distance) to plan neighborhood retail, and at global radii ($R = n$) for major transit nodes.
2. **Isovist Visual Fields in Public Plazas**: Design urban seating clusters at points with high isovist visual control over entry thresholds.
3. **Active Frontage Activation**: Align zoning ordinances so that food/beverage and commercial active ground floors are mandated exclusively on streets scoring in the top $15\%$ of NACH Choice.
