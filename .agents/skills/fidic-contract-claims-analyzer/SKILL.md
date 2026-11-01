---
name: fidic-contract-claims-analyzer
description: Manages FIDIC engineering contracts, drafts Extension of Time (EOT) claims, analyzes critical path delays (Time Impact Analysis per SCL Protocol), and evaluates disruption quantum costs.
---

# FIDIC Contract Administration & Delay Claims Analyzer

Commercial and legal engineering skill for administering FIDIC conditions of contract (Red, Yellow, and Silver Books), managing contractual notices within mandatory time-bars, analyzing schedule delay causality (SCL Protocol), and quantifying disruption claims.

---

## 1. Key FIDIC Clauses & Mandatory Time-Bars (FIDIC 1999/2017)

```
Delay Event Occurs ──> [ Clause 20.1: Notice of Claim ] ──> [ Detailed Contemporary Records ] ──> [ Fully Detailed Claim (42 Days) ] ──> [ Engineer Determination (Clause 3.5) ]
                        (Strict 28-Day Deadline)              (Daily logs, photos, RFIs)             (TIA Delay + Cost Quantum)
```

### Critical Clauses (FIDIC Red Book):
- **Clause 8.4 [Extension of Time for Completion]**:
  Entitles contractor to EOT if completion is delayed by:
  a) Variation instructed by Engineer.
  b) Exceptionally adverse climatic conditions (exceeding 10-year historical return data).
  c) Unforeseeable physical conditions (Clause 4.12 - subsoil, rock, archaeological finds).
  d) Unforeseeable shortages of personnel or goods caused by epidemic or governmental actions.
  e) Any delay or impediment caused by Employer or Employer's other contractors.
- **Clause 20.1 [Contractor's Claims - The 28-Day Guillotine]**:
  Contractor must give notice **within 28 days** after becoming aware of the event. If the contractor fails to give notice within 28 days:
  $$\text{The Time for Completion shall NOT be extended, and the Employer is discharged from ALL liability!}$$

---

## 2. Delay Analysis Methodologies (SCL Delay & Disruption Protocol)

The Society of Construction Law (SCL) Protocol recognizes six core delay methodologies:

| Methodology | Nature | Best Application | Strengths & Weaknesses |
| :--- | :--- | :--- | :--- |
| **Time Impact Analysis (TIA)** | Prospective | Ongoing delay events during construction | **Gold Standard** in international arbitration; isolates delay events into updated CPM schedules. |
| **Windows Analysis** | Retrospective | Multi-event complex project completion | Analyzes evolution of the critical path over periodic baseline snapshots (e.g. monthly). |
| **As-Planned vs. As-Built** | Retrospective | Simple projects with few delay events | Fast, inexpensive; but fails to model dynamic critical path shifts. |
| **Collapsed As-Built** | Retrospective | Disputed delays where baseline schedule was poor | Subtracts employer delays from as-built schedule to see when project would have finished. |

---

## 3. Quantum Calculation: Head Office Overhead Formulas

When completion is extended due to compensable employer delay, the contractor is entitled to site prolongation costs and unabsorbed head office overhead:

### The Eichleay Formula:
$$\text{Overhead Allocable to Project} = \frac{\text{Contract Billings}}{\text{Total Company Billings across Period}} \times \text{Total Company Head Office Overhead}$$
$$\text{Daily Allocable Overhead Rate} = \frac{\text{Allocable Overhead}}{\text{Contract Period (Days)}}$$
$$\text{Total Compensable Overhead Claim} = \text{Daily Rate} \times \text{Days of Employer Delay}$$

---

## 4. Python Eichleay & EOT Delay Calculator

```python
"""
Delay Claim Quantum Calculator (Eichleay Formula)
Compliant with SCL Delay and Disruption Protocol
"""

def calculate_eichleay_overhead_claim(contract_value: float, 
                                      total_company_turnover: float, 
                                      total_head_office_overhead: float, 
                                      actual_contract_duration_days: int, 
                                      compensable_delay_days: int, 
                                      daily_site_preliminaries: float) -> dict:
    
    # 1. Allocable Overhead
    allocable_ratio = contract_value / total_company_turnover
    allocable_overhead = allocable_ratio * total_head_office_overhead
    
    # 2. Daily Rate
    daily_overhead_rate = allocable_overhead / actual_contract_duration_days
    
    # 3. Total Head Office Overhead Claim
    total_ho_claim = daily_overhead_rate * compensable_delay_days
    
    # 4. Total Site Preliminaries Prolongation
    total_site_claim = daily_site_preliminaries * compensable_delay_days
    
    grand_total_claim = total_ho_claim + total_site_claim
    
    return {
        "compensable_delay_days": compensable_delay_days,
        "daily_head_office_overhead_rate": round(daily_overhead_rate, 2),
        "head_office_overhead_claim": round(total_ho_claim, 2),
        "site_preliminaries_claim": round(total_site_claim, 2),
        "grand_total_prolongation_claim": round(grand_total_claim, 2)
    }

if __name__ == "__main__":
    # Project with 60 days compensable employer delay
    claim = calculate_eichleay_overhead_claim(
        contract_value=25000000.0,
        total_company_turnover=100000000.0,
        total_head_office_overhead=8000000.0,
        actual_contract_duration_days=730,
        compensable_delay_days=60,
        daily_site_preliminaries=3500.0
    )
    print("FIDIC Delay Claim Quantum Results:")
    for k, v in claim.items():
        print(f"  {k}: {v}")
```

---

## 5. Deliverables Checklist
1. **Contractual Notice of Delay (Clause 20.1)**: Formatted official letter referencing specific contract clauses, contemporary site records, and cause of delay.
2. **Fragnet CPM Delay Schedule**: Primavera P6 XER file with inserted delay activities proving critical path movement.
