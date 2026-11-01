---
name: embodied-carbon-lca-bim
description: Calculates upfront embodied carbon (A1-A5 lifecycle stages), matches BIM elements with Environmental Product Declarations (EPD), and performs Life Cycle Assessments (LCA) compliant with EN 15978, RICS, and LEED v4.1.
---

# Embodied Carbon & BIM Life Cycle Assessment (LCA)

Computational sustainability and carbon accounting skill for auditing embodied greenhouse gas emissions across building lifecycle stages ($A1-A5$), matching materials to Environmental Product Declarations (EPD), and meeting net-zero carbon targets.

---

## 1. Life Cycle Assessment (LCA) Framework (EN 15978 / ISO 14040)

```
[ Upfront Embodied Carbon (A1-A5) ] ──> [ Operational & Use (B1-B7) ] ──> [ End-of-Life (C1-C4) ] ──> [ Beyond Lifecycle (D) ]
A1: Raw material extraction              B1: Use / Refrigerants            C1: Deconstruction/Demolition D: Reuse & Recycling
A2: Transport to manufacturer            B2: Maintenance / Replacement     C2: Transport to waste           Potential
A3: Factory manufacturing                B6: Operational Energy            C3: Waste processing
A4: Transport to site                    B7: Operational Water             C4: Final disposal
A5: Construction installation
```

### Core Mathematical Formula:
$$\text{Total Upfront Carbon } (A1-A5) = \sum_{i=1}^{n} \left[ V_i \cdot \rho_i \cdot \text{ECF}_{A1-A3, i} \cdot (1 + W_i) \right] + E_{A4} + E_{A5}$$
- $V_i$: Net volume of material $i$ extracted from BIM model ($\text{m}^3$).
- $\rho_i$: Density of material ($\text{kg/m}^3$).
- $\text{ECF}_{A1-A3, i}$: Embodied Carbon Factor from certified EPD ($\text{kg CO}_2\text{e}/\text{kg}$).
- $W_i$: Site construction waste factor ($0.03 - 0.08$).
- $E_{A4}, E_{A5}$: Transport freight emissions and on-site fuel/electricity consumption.

---

## 2. Key Industry Carbon Benchmarks (RICS / LETI / Mostadam)

| Building Typology | Standard Baseline ($\text{kg CO}_2\text{e}/\text{m}^2\text{ GFA}$) | Best Practice 2025 Target | Net-Zero Exemplar ($A1-A5$) |
| :--- | :--- | :--- | :--- |
| **Commercial Office (High-Rise)** | $900 - 1200$ | $\le 600$ | $\le 350$ (Mass timber hybrid) |
| **Residential Multi-Family** | $700 - 950$ | $\le 500$ | $\le 300$ |
| **Schools & Public Buildings** | $600 - 800$ | $\le 450$ | $\le 250$ |

---

## 3. High-Impact Decarbonization Strategies

1. **Concrete Cement Replacement (CEM III / GGBS)**:
   - Traditional OPC concrete has $\approx 350\text{ kg CO}_2\text{e}/\text{m}^3$.
   - Replacing $50\% - 70\%$ cement with Ground Granulated Blast-furnace Slag (GGBS) or Fly Ash reduces carbon by **$40\% - 60\%$** ($\approx 150 - 180\text{ kg CO}_2\text{e}/\text{m}^3$).
2. **Electric Arc Furnace (EAF) Recycled Steel**:
   - Primary Blast Furnace steel: $\approx 2.4\text{ kg CO}_2\text{e}/\text{kg}$.
   - $95\%$ recycled scrap steel via EAF: $\approx 0.5 - 0.7\text{ kg CO}_2\text{e}/\text{kg}$ ($> 70\%$ reduction).
3. **Mass Timber (CLT / Glulam)**:
   - Acts as an active carbon sink, sequestering $\approx -750\text{ kg CO}_2\text{e}/\text{m}^3$ of biogenic carbon during tree growth.

---

## 4. Python Embodied Carbon QTO Calculator

```python
"""
BIM Material Quantity to Embodied Carbon Calculator
Compliant with EN 15978 and RICS Professional Statement
"""

EPD_DATABASE = {
    "c30_37_opc_concrete": {"density": 2400.0, "ecf": 0.135, "desc": "Standard Portland cement concrete"},
    "c30_37_ggbs50_concrete": {"density": 2400.0, "ecf": 0.068, "desc": "50% GGBS Low-Carbon concrete"},
    "reinforcing_steel_primary": {"density": 7850.0, "ecf": 2.100, "desc": "Virgin blast furnace rebar"},
    "reinforcing_steel_recycled": {"density": 7850.0, "ecf": 0.650, "desc": "95% Recycled EAF rebar"},
    "structural_clt_timber": {"density": 480.0, "ecf": 0.220, "desc": "CLT without biogenic storage"},
    "aluminum_extrusion_virgin": {"density": 2700.0, "ecf": 8.500, "desc": "Primary aluminum framing"},
    "aluminum_extrusion_hydro": {"density": 2700.0, "ecf": 3.800, "desc": "Low-carbon hydro-powered aluminum"}
}

def calculate_material_carbon(material_key: str, volume_m3: float, waste_factor: float = 0.05) -> dict:
    epd = EPD_DATABASE.get(material_key)
    if not epd:
        raise ValueError(f"Material {material_key} not found in EPD database.")
        
    mass_kg = volume_m3 * epd["density"]
    total_carbon_kg = mass_kg * epd["ecf"] * (1.0 + waste_factor)
    
    return {
        "material": epd["desc"],
        "volume_m3": volume_m3,
        "mass_tons": round(mass_kg / 1000.0, 1),
        "embodied_carbon_tons_co2e": round(total_carbon_kg / 1000.0, 2)
    }

if __name__ == "__main__":
    # Baseline vs Low-Carbon comparison for 1000 m3 concrete slab
    baseline = calculate_material_carbon("c30_37_opc_concrete", 1000.0)
    optimized = calculate_material_carbon("c30_37_ggbs50_concrete", 1000.0)
    
    print(f"Standard Concrete Carbon:   {baseline['embodied_carbon_tons_co2e']} tons CO2e")
    print(f"Low-Carbon Concrete Carbon: {optimized['embodied_carbon_tons_co2e']} tons CO2e")
    savings = baseline['embodied_carbon_tons_co2e'] - optimized['embodied_carbon_tons_co2e']
    print(f"Carbon Saved: {round(savings, 2)} tons CO2e ({round((savings / baseline['embodied_carbon_tons_co2e'])*100, 1)}% reduction!)")
```

---

## 5. Deliverables Checklist
1. **Whole-Building Carbon Intensity Report**: Breakdown of $\text{kg CO}_2\text{e}/\text{m}^2$ by building system (Substructure, Superstructure, Envelope, Finishes, Services).
2. **EPD Compliance Matrix**: Verified list of Type III Environmental Product Declarations compliant with ISO 14025.
