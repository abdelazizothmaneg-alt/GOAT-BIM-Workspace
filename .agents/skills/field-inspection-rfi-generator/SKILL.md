---
name: field-inspection-rfi-generator
description: Generates, formats, and tracks formal Requests for Information (RFIs) and Non-Conformance Reports (NCRs) from field site inspections, linking BIM element GUIDs, photographic evidence, and schedule impact assessments.
---

# Field Inspection & RFI Generator

Construction site administration and field engineering skill for drafting high-precision Requests for Information (RFIs), Work Inspection Requests (WIR), and Non-Conformance Reports (NCR), bound directly to BIM models and contractual specifications.

---

## 1. RFI Structure & Best Practices (CSI Standards)

A professional RFI must eliminate contractual ambiguity and prevent contractor delay claims:

```
RFI NUMBER: [PROJ-RFI-00XX]
TO: [Lead Architect / Structural Engineer]
DATE OF ISSUE: [YYYY-MM-DD] | REQUIRED RESPONSE DATE: [7 Days from issue]
DRAWING REFERENCE: [A-102 Rev C, Detail 4/A-501]
SPECIFICATION SECTION: [08 44 13 Section 2.3.A]
BIM ELEMENT GUID: [3d58a8f1-4b2a-4318-b80c-80a97c8d92ef]

SUBJECT: Clarification on Spandrel Beam Cladding Anchor Clash with MEP Duct

1. DESCRIPTION OF ISSUE / SITE CONDITION:
   During installation of the 4th-floor perimeter HVAC supply duct (Drawing M-201), a direct 
   physical interference was encountered with the cast-in channel anchor for the unitized 
   curtain wall transom (Drawing A-501). The cast-in channel sits 120mm lower than modeled.

2. CONTRACTOR'S PROPOSED SOLUTION / RECOMMENDATION:
   Reroute the horizontal supply duct 150mm inward and transition to a flat-oval profile (400x200mm) 
   to maintain clear airflow without compromising the structural facade anchor.

3. SCHEDULE & COST IMPACT ASSESSMENT:
   • Cost Impact: No additional cost if approved within 5 working days.
   • Schedule Impact: Critical path affected if response exceeds required response date.
```

---

## 2. Non-Conformance Report (NCR) Lifecycle

1. **Detection & Issuance**: Field engineer flags deviation from approved shop drawings or specified tolerances.
2. **Containment**: Tagging affected area on site with physical warning signs to stop subsequent finishing trades.
3. **Root Cause Analysis**: Determining whether failure was material defect, poor workmanship, or coordination clash.
4. **Proposed Disposition**:
   - *Rework / Repair*: Restoring element to comply with specification.
   - *Accept as is (Concession)*: Subject to structural calculation approval and cost deduction.
   - *Reject and Replace*: Complete demolition and re-casting.

---

## 3. Python Automated RFI & NCR Formatter

```python
"""
Field RFI & NCR Generator Engine
Generates structured markdown and JSON payloads for Procore / Autodesk Construction Cloud
"""

import datetime
from typing import Optional

def generate_rfi_payload(rfi_id: str, subject: str, discipline: str, 
                         dwg_ref: str, spec_ref: str, bim_guid: str, 
                         question_text: str, proposed_solution: str, 
                         cost_impact: bool, schedule_impact_days: int) -> dict:
    
    today = datetime.date.today().isoformat()
    due_date = (datetime.date.today() + datetime.timedelta(days=7)).isoformat()
    
    return {
        "rfi_number": rfi_id,
        "subject": subject,
        "discipline": discipline,
        "date_created": today,
        "response_due_date": due_date,
        "references": {
            "drawings": dwg_ref,
            "spec_section": spec_ref,
            "bim_guid": bim_guid
        },
        "inquiry": question_text,
        "contractor_recommendation": proposed_solution,
        "impact_evaluation": {
            "cost_impact_flag": cost_impact,
            "critical_path_delay_days": schedule_impact_days
        },
        "status": "OPEN_SUBMITTED_TO_ARCHITECT"
    }

if __name__ == "__main__":
    rfi = generate_rfi_payload(
        rfi_id="RFI-STR-014",
        subject="Rebar Congestion at Beam-Column Joint Level 6",
        discipline="Structural",
        dwg_ref="S-201 Rev 2",
        spec_ref="03 30 00 Cast-in-Place Concrete",
        bim_guid="4f61b2e8-99c1-41e9-ba11-3094892c90e1",
        question_text="Bundled column rebar leaves insufficient space for 32mm aggregate vibrator penetration.",
        proposed_solution="Permit use of self-consolidating concrete (SCC) mix for this joint or mechanically splice bottom rebar.",
        cost_impact=False,
        schedule_impact_days=0
    )
    print("Formatted RFI Payload:")
    for k, v in rfi.items():
        print(f"  {k}: {v}")
```

---

## 4. Deliverables Checklist
1. **RFI Log**: Live register tracking open, answered, and closed RFIs with response turnaround time metrics.
2. **Annotated Field Photos**: Markup photos with dimension callouts and gridline references attached to submittal.
