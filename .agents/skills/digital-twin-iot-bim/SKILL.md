---
name: digital-twin-iot-bim
description: Bridges BIM models into operational Digital Twins, integrating real-time IoT sensor telemetry (BACnet, MQTT, Azure Digital Twins) with Revit/IFC assets for predictive maintenance and energy analytics.
---

# Digital Twin & IoT BIM Operations

Cutting-edge smart building skill for bridging static BIM design models into dynamic, operational Digital Twins. Integrates live IoT sensor streams, building management systems (BMS), predictive maintenance alerts, and spatial asset telemetry.

---

## 1. Digital Twin Architecture & Protocols (ISO 23247)

```
[ Physical Building Layer ]          [ Connectivity & Edge ]         [ Semantic Digital Twin ]         [ Operational Applications ]
Chillers, AHUs, Pumps,      ──>     BACnet/IP, Modbus,       ──>    BIM Asset GUID Binding,    ──>     3D Spatial Heatmaps,
Energy Meters, CO2 Sensors          MQTT Broker, LoRaWAN            Azure Digital Twins, IFC           Predictive Fault Detection
```

### Key Communication Protocols:
1. **BACnet/IP & Modbus**: Industrial building automation protocols connecting chillers, boilers, variable frequency drives (VFDs), and fan coil units (FCUs).
2. **MQTT (Message Queuing Telemetry Transport)**: Ultra-lightweight publish/subscribe protocol for wireless IoT sensors (ambient temperature, humidity, indoor air quality, PIR occupancy).
3. **Azure Digital Twins / AWS IoT TwinMaker**: Graph-based digital representations linking spatial topologies (Building $\rightarrow$ Floor $\rightarrow$ Zone $\rightarrow$ Room $\rightarrow$ Asset).

---

## 2. BIM-to-Twin Semantic Binding (Revit / IFC GUIDs)

Every physical device must correspond to a permanent global unique identifier in the BIM model:
- `Twin_Asset_GUID`: Matches Revit `UniqueId` or IFC `GlobalId` (e.g. `2O2Fr$t4X7Zf8NOew3FL9m`).
- `Twin_BMS_Tag`: Point name in the BMS server (e.g. `AHU-04.SupplyAirTemp`).
- `Twin_Telemetry_Topic`: MQTT topic stream (e.g. `sensors/building_a/level_03/room_302/co2`).

---

## 3. Python IoT Telemetry & Anomaly Detection Pipeline

```python
"""
IoT Telemetry Processor & Anomaly Detection for BIM Digital Twins
"""

import json
from datetime import datetime

class DigitalTwinAsset:
    def __init__(self, asset_guid: str, equipment_tag: str, temp_setpoint: float = 22.0):
        self.guid = asset_guid
        self.tag = equipment_tag
        self.temp_setpoint = temp_setpoint
        self.history = []

    def ingest_telemetry(self, sensor_payload: dict):
        # payload: {"timestamp": ..., "temp": 24.5, "vibration_rms": 2.1, "power_kw": 18.4}
        temp = sensor_payload.get("temp", 0.0)
        vibration = sensor_payload.get("vibration_rms", 0.0)
        
        status = "NORMAL"
        alert_msg = None
        
        # Predictive maintenance rule 1: Bearing vibration spike (> 4.5 mm/s)
        if vibration > 4.5:
            status = "CRITICAL_MAINTENANCE_REQUIRED"
            alert_msg = f"Motor bearing vibration {vibration} mm/s exceeds ISO 10816 limit!"
        # Rule 2: Cooling failure (Temp exceeds setpoint by > 3 deg C)
        elif temp > (self.temp_setpoint + 3.0):
            status = "WARNING_TEMPERATURE_OVERHEAT"
            alert_msg = f"Supply air temperature {temp} C significantly above setpoint {self.temp_setpoint} C!"
            
        record = {
            "timestamp": sensor_payload.get("timestamp", datetime.utcnow().isoformat()),
            "telemetry": sensor_payload,
            "status": status,
            "alert": alert_msg
        }
        self.history.append(record)
        return record

if __name__ == "__main__":
    chiller_twin = DigitalTwinAsset("3d891bca-22e1-456f-9988-123456789abc", "CHILLER-01", temp_setpoint=7.0)
    
    # Ingest abnormal telemetry packet
    alert_event = chiller_twin.ingest_telemetry({
        "timestamp": datetime.utcnow().isoformat(),
        "temp": 7.2,
        "vibration_rms": 5.8, # Anomaly!
        "power_kw": 220.0
    })
    
    print("Digital Twin Ingestion Result:")
    print(json.dumps(alert_event, indent=2))
```

---

## 4. Deliverables Checklist
1. **Asset Telemetry Registry**: Schema linking every MEP asset GUID to its IP/BACnet address, engineering units, and normal operating thresholds.
2. **Interactive 3D Web Viewer Configuration**: Data dictionary for WebGL/APS Viewer coloring 3D elements dynamically (Green = Normal, Amber = Alert, Red = Critical Fault).
