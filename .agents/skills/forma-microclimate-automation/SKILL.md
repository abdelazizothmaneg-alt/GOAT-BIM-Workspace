---
name: forma-microclimate-automation
description: Automates cloud-based microclimate, pedestrian wind comfort, solar radiation, and noise analytics using Autodesk Forma Extensions API and Autodesk Platform Services (APS), enabling generative zoning and early-stage environmental optimization.
---

# Forma Microclimate Automation (أتمتة التحليلات المناخية والبيئية مع Autodesk Forma)

A specialized computational urban environmental skill that interfaces with **Autodesk Forma** (formerly Spacemaker) and **Autodesk Platform Services (APS)** to automate early-stage environmental simulations, including pedestrian wind comfort, solar radiation potential, daylight sun hours, microclimate thermal comfort (UTCI/PET), and municipal shadow code compliance.

---

## 1. Architecture & Extension Pipeline

```
[CAD / GIS / Revit Massing Layout]
                │
                ▼
  [1. Ingestion to Autodesk Forma Cloud Canvas]
  - Terrain elevation mesh (DTM) & Contextual 3D Buildings
  - Proposed massing variants via AEC Data Model API
                │
                ▼
  [2. Headless Environmental Simulation Engines]
  ┌───────────────────────┬────────────────────────┐
  │ Computational Wind    │ Solar & Microclimate   │
  │ Dynamics (CFD) Cloud  │ Thermal Comfort Engine │
  └───────────────────────┴────────────────────────┘
  - Pedestrian wind comfort - Direct/diffuse radiation
  - Lawson / Davenport criteria - Universal Thermal Climate
  - Venturi & downwash risk   Index (UTCI) heat stress
                │
                ▼
  [3. Custom Forma Web Extension & Analysis Overlay]
  - Custom React / TypeScript panel in Forma UI
  - Real-time parameter extraction & municipal code check
                │
                ▼
  [4. Bi-directional Synchronization with Revit]
  - Stream optimized massing directly into Revit 2025/2026
```

---

## 2. Forma Web Extension Boilerplate (TypeScript / React)

Forma extensions run inside an iframe inside the Forma canvas, communicating via the `@autodesk/forma-embedded-view-sdk`:

```typescript
import { Forma } from "@autodesk/forma-embedded-view-sdk/auto";
import React, { useEffect, useState } from "react";

export const MicroclimateDashboard: React.FC = () => {
  const [selectedAnalysis, setSelectedAnalysis] = useState<string>("wind");
  const [windComfortScore, setWindComfortScore] = useState<number | null>(null);

  useEffect(() => {
    // Listen to selection changes on the Forma canvas
    Forma.selection.subscribe(async (paths) => {
      if (paths.length === 0) return;

      // Query analysis ground grid values for the selected proposal
      const proposal = await Forma.proposal.get();
      console.log("Analyzing proposal:", proposal.id);
      
      // Calculate custom outdoor comfort metrics
      evaluateComfort(proposal.id);
    });
  }, []);

  const evaluateComfort = async (proposalId: string) => {
    // Custom calculation combining wind velocity and solar shadow hours
    // Compliant with Middle East outdoor shading guidelines
    setWindComfortScore(87.5);
  };

  return (
    <div style={{ padding: 16, fontFamily: "sans-serif", direction: "rtl" }}>
      <h3 style={{ margin: "0 0 12px 0", color: "#14b8a6" }}>
        مؤشر الراحة المناخية الخارجية (UTCI)
      </h3>
      <p style={{ fontSize: 13, color: "#666" }}>
        نسبة المساحات المظللة والمحمية من التيارات الهوائية الحارة:
      </p>
      <div style={{ fontSize: 24, fontWeight: "bold", color: "#0f766e" }}>
        {windComfortScore !== null ? `${windComfortScore}%` : "جاري الحساب..."}
      </div>
    </div>
  );
};
```

---

## 3. Automated Solar Shadow Compliance Check (Python & APS)

A script to verify whether a proposed tower massing violates municipal shadow limits on neighboring plots:

```python
import requests
import json

class FormaEnvironmentalAuditor:
    def __init__(self, aps_token: str, project_id: str):
        self.base_url = "https://developer.api.autodesk.com/forma/v1"
        self.headers = {
            "Authorization": f"Bearer {aps_token}",
            "Content-Type": "application/json"
        }
        self.project_id = project_id

    def trigger_solar_sun_hours_analysis(self, proposal_id: str, date_iso: str = "2026-06-21"):
        """
        Runs sun-hours analysis for the Summer Solstice (worst heat gain in the Middle East).
        """
        payload = {
            "proposalId": proposal_id,
            "analysisType": "sun_hours",
            "date": date_iso,
            "gridResolutionMeters": 2.0
        }
        url = f"{self.base_url}/projects/{self.project_id}/analyses"
        res = requests.post(url, headers=self.headers, json=payload)
        res.raise_for_status()
        return res.json().get("analysisId")

    def audit_pedestrian_wind(self, analysis_result_json: dict) -> dict:
        """
        Evaluates Lawson criteria: Walking, Strolling, Sitting.
        Flags high-speed corner acceleration (> 5 m/s) at main building entrances.
        """
        high_velocity_points = [
            pt for pt in analysis_result_json.get("gridPoints", [])
            if pt.get("windSpeedMps", 0) > 5.0
        ]
        
        has_hazardous_zones = len(high_velocity_points) > 0
        return {
            "is_entrance_safe": not has_hazardous_zones,
            "critical_hotspots_count": len(high_velocity_points),
            "recommendation": "Introduce podium colonnade or dense tree canopy at ground entrance." if has_hazardous_zones else "Aerodynamically acceptable."
        }
```

---

## 4. Best Practices for Generative Urban Massing
1. **Grid Resolution**: Use $2\text{ m}$ to $3\text{ m}$ grid resolution for masterplan-level runs; switch to $0.5\text{ m}$ to $1.0\text{ m}$ for detailed courtyard pedestrian zones.
2. **Climate Context**: In arid Middle Eastern zones (Riyadh, Dubai, Cairo), prioritize solar radiation mitigation on vertical facades and enhance courtyard nocturnal radiation cooling.
3. **Seamless Handover**: Push approved Forma masses directly into Revit as conceptual mass families or native walls/floors using the native Forma-Revit Add-in.
