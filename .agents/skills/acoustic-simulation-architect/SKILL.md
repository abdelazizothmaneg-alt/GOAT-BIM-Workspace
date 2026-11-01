---
name: acoustic-simulation-architect
description: Calculates architectural room acoustics, reverberation time (RT60), speech transmission index (STI), and sound transmission class (STC) partition ratings for auditoriums, mosques, and residential units.
---

# Acoustic Simulation Architect (المحاكاة الصوتية المعمارية وتوزيع الصدى)

An architectural room acoustics and sound insulation skill based on **ISO 3382 (Acoustics - Room Acoustic Parameters)** and **ASTM E90 (Sound Transmission Loss)**. It computes Reverberation Times ($RT_{60}$ via Sabine and Eyring formulas), speech intelligibility (STI), echoes/flutter prevention, and specifies partition Sound Transmission Class (STC) ratings.

---

## 1. Reverberation Time Mathematical Framework (Sabine & Eyring)

For an enclosed space with volume $V\text{ (m}^3\text{)}$, total boundary surface area $S\text{ (m}^2\text{)}$, and mean sound absorption coefficient $\bar{\alpha}$:

$$\text{Sabine's Formula: } RT_{60} = \frac{0.161 \cdot V}{A} = \frac{0.161 \cdot V}{\sum S_i \alpha_i}$$

$$\text{Eyring's Formula (For high absorption spaces): } RT_{60} = \frac{0.161 \cdot V}{-S \ln(1 - \bar{\alpha}) + 4mV}$$

*Where $A$ is the total equivalent absorption area in sabins ($\text{m}^2$), and $m$ is the air attenuation coefficient.*

---

## 2. Target $RT_{60}$ Standards by Typology

| Space Typology | Recommended $RT_{60}$ at 500-1000 Hz | Target Speech Intelligibility (STI) |
| :--- | :--- | :--- |
| **Mosques / Prayer Halls** | $1.2 - 1.6\text{ seconds}$ | $\ge 0.55$ (Good speech clarity) |
| **Auditoriums / Theaters (Speech)**| $0.8 - 1.1\text{ seconds}$ | $\ge 0.65$ (Excellent clarity) |
| **Concert Halls (Symphonic Music)**| $1.8 - 2.2\text{ seconds}$ | $\ge 0.45$ (Warm acoustic blend) |
| **Classrooms / Conference Rooms** | $0.6 - 0.8\text{ seconds}$ | $\ge 0.70$ (Superior clarity) |
| **Recording Studios / Podcasts** | $0.2 - 0.4\text{ seconds}$ | $\ge 0.85$ (Dead room) |

---

## 3. Automated $RT_{60}$ Calculator (Python Recipe)

```python
def calculate_room_rt60(volume_m3: float, surfaces: list):
    """
    Computes Sabine RT60 at 1000 Hz.
    surfaces = [{"name": "Carpet", "area_sqm": 120.0, "nrc": 0.35}, ...]
    """
    total_absorption_metric_sabins = 0.0
    total_surface_area = 0.0

    for s in surfaces:
        area = s["area_sqm"]
        nrc = s["nrc"] # Noise Reduction Coefficient at target frequency
        total_absorption_metric_sabins += area * nrc
        total_surface_area += area

    if total_absorption_metric_sabins <= 0:
        return {"error": "Total absorption must be positive"}

    rt60_sabine = (0.161 * volume_m3) / total_absorption_metric_sabins
    mean_alpha = total_absorption_metric_sabins / total_surface_area

    return {
        "volume_m3": volume_m3,
        "total_absorption_area": round(total_absorption_metric_sabins, 2),
        "mean_absorption_coeff": round(mean_alpha, 3),
        "rt60_seconds": round(rt60_sabine, 2)
    }
```

---

## 4. Acoustic Defect Mitigation
1. **Flutter Echoes**: Occur between parallel reflective walls; introduce acoustic wall diffusion panels or tilt one wall by at least $5^\circ$ to $7^\circ$.
2. **Dome Focusing**: Spherical domes focus sound energy onto focal hotspots; treat dome perimeters with micro-perforated acoustic plaster or sound-absorbing liners.
