---
name: iso-9001-aec-auditor
description: Audits engineering, architectural, and BIM consulting workflows against ISO 9001:2015 Quality Management System (QMS) requirements, enforcing design stage-gate reviews, Non-Conformance Reports (NCR), Corrective Actions (CAPA), and document control registers.
---

# ISO 9001 AEC Auditor (تدقيق نظام إدارة الجودة للمكاتب الهندسية ISO 9001:2015)

A Quality Management System (QMS) auditing skill specialized for architectural practices, structural engineering consultancies, and BIM automation studios compliant with **ISO 9001:2015**. It standardizes design stage-gate verifications (Clause 8.3), audits document control registers (Clause 7.5), automates Non-Conformance Reporting (NCR Clause 8.7), and drives Corrective and Preventive Actions (CAPA Clause 10.2).

---

## 1. ISO 9001:2015 Design & Development Lifecycle (Clause 8.3)

```
[Design Project Kick-Off & Client Brief]
                   │
                   ▼
  [Stage-Gate 1: Concept Design Review (15-30%)]
  - Check: Client brief compliance, municipal zoning envelope
  - Sign-off: Lead Architect & Client Representative
                   │
                   ▼
  [Stage-Gate 2: Schematic & Preliminary Design (30-60%)]
  - Check: Structural grid coordination, IBC/SBC code clearance
  - Sign-off: Discipline Leads (Arch / Struct / MEP)
                   │
                   ▼
  [Stage-Gate 3: Detailed Design & BIM Coordination (60-90%)]
  - Check: Inter-disciplinary clash detection, Navisworks zero-clash report
  - Sign-off: BIM Manager & Technical Director
                   │
                   ▼
  [Stage-Gate 4: Issued for Construction (IFC / Tender 100%)]
  - Check: Constructability audit, BOQ quantity check, specification freeze
  - Sign-off: Principal Consultant & Quality Manager
```

---

## 2. Non-Conformance Report (NCR) Schema & Automation

When a drawing, model, or calculation fails internal QA/QC, an official NCR is generated to track corrective action:

```json
{
  "ncr_number": "NCR-2026-ARC-014",
  "project_code": "PRJ-RYD-042",
  "issuing_date": "2026-11-01",
  "issuing_auditor": "Quality Assurance Manager",
  "responsible_discipline": "Structural Engineering",
  "clause_reference": "ISO 9001:2015 Clause 8.3.4 (Design Controls)",
  "non_conformance_description": "Structural drop beam at Grid B-4 penetrates the main HVAC return duct with zero vertical clearance, violating design coordination protocol.",
  "severity_level": "MAJOR",
  "root_cause_analysis": "Structural model updated and published without executing intermediate Navisworks clash audit against MEP Rev03.",
  "corrective_action_plan": {
    "action": "Lower HVAC duct or introduce reinforced beam web sleeve per structural calculation S-CALC-88.",
    "responsible_engineer": "Senior Structural Lead",
    "target_closure_date": "2026-11-05"
  },
  "status": "OPEN"
}
```

---

## 3. Python Audit Script for Engineering Drawing Registers (Clause 7.5)

```python
import csv
from datetime import datetime

def audit_drawing_register_iso9001(register_csv_path: str):
    """
    Audits a project drawing register against ISO 9001 document control standards:
    - No empty revision numbers
    - No missing check/approval initials
    - No duplicated drawing numbers
    """
    findings = []
    drawing_numbers = set()

    with open(register_csv_path, mode='r', encoding='utf-8') as f:
        reader = csv.DictReader(f)
        for row_idx, row in enumerate(reader, start=2):
            dwg_no = row.get("Drawing_Number", "").strip()
            rev = row.get("Revision", "").strip()
            approved_by = row.get("Approved_By", "").strip()
            status = row.get("Status", "").strip()

            # 1. Duplicate check
            if dwg_no in drawing_numbers:
                findings.append(f"Row {row_idx}: Duplicate Drawing Number '{dwg_no}'.")
            drawing_numbers.add(dwg_no)

            # 2. Missing revision
            if not rev:
                findings.append(f"Row {row_idx} ({dwg_no}): Missing Revision index.")

            # 3. Missing approval on issued drawings
            if status in ["Tender", "IFC", "Approved"] and not approved_by:
                findings.append(f"Row {row_idx} ({dwg_no}): Status is '{status}' but lacks 'Approved_By' signature.")

    return {
        "total_drawings_audited": len(drawing_numbers),
        "audit_pass": len(findings) == 0,
        "total_discrepancies": len(findings),
        "audit_findings": findings
    }
```

---

## 4. Key ISO 9001 Quality Metrics for AEC
1. **First-Time Approval Ratio (FTAR)**: Percentage of consultant submittals approved by the client/municipality on first submittal ($\text{Target} \ge 85\%$).
2. **NCR Resolution Cycle Time**: Average days taken to close open Non-Conformance Reports ($\text{Target} \le 5\text{ working days}$).
3. **Design Revision Drift**: Number of unplanned drawing revisions after Stage-Gate 4 freeze ($\text{Target} \le 5\%$).
