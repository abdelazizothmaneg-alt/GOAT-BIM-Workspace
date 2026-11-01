---
name: saudi-building-code-sbc
description: >-
  Validates architectural and urban designs against the Saudi Building Code (SBC 201 Architectural, SBC 801 Fire Safety, SBC 1001 Green),
  Balady / MOMRAH municipal zoning regulations, setbacks, FAR, building heights, and parking quota calculations.
---

# Saudi Building Code (SBC) & Balady Regulations Validator

Engineering compliance framework for auditing architectural designs, BIM models, and urban proposals against the Saudi Building Code (SBC) and Ministry of Municipal and Rural Affairs and Housing (MOMRAH / Balady) regulations.

## 1. Core SBC Code Structure
* **SBC 201:** Saudi Building Code - General Requirements (Architectural Design, Occupancy, Building Heights & Areas).
* **SBC 801:** Saudi Fire Code (Egress travel distance, fire separation, sprinkler requirements, emergency exits).
* **SBC 1001:** Saudi Green Building Code (Daylighting, thermal performance, water conservation, renewable integration).
* **MOMRAH / Balady Manuals:** Urban setbacks, Ground Coverage Ratio (GCR), Floor Area Ratio (FAR), and parking minimums.

---

## 2. Dimensional & Egress Verification (SBC 201 / SBC 801)

### A. Means of Egress & Travel Distances (SBC 801 Chapter 10)
* **Maximum Exit Access Travel Distance:**
  * Without Automatic Sprinkler System: $\le 60\text{ m}$ (200 ft).
  * With Approved NFPA 13 Sprinkler System: $\le 75\text{ m} - 90\text{ m}$ (250 - 300 ft depending on occupancy).
* **Common Path of Egress Travel:**
  * $\le 23\text{ m}$ (75 ft) before two independent exit choices become available.
* **Corridor & Door Clear Widths:**
  * Primary public corridors: Minimum clear width $\ge 1.80\text{ m} - 2.40\text{ m}$ (Educational / Healthcare) or $\ge 1.20\text{ m}$ (Commercial / Residential).
  * Clear door opening width: Minimum $900\text{mm}$ clear between face of door leaf and door stop.
* **Exit Separation Diagonal Rule:**
  * Distance between two exits $\ge 1/2$ of overall floor diagonal length (reduced to $1/3$ in fully sprinklered buildings).

### B. Occupancy Load Factors (SBC 201 Table 1004.5)
$$\text{Design Occupancy} = \frac{\text{Gross Floor Area (m}^2\text{)}}{\text{Load Factor (m}^2/\text{person)}}$$
* Classrooms: $1.9\text{ m}^2/\text{person}$ net.
* Business / Offices: $9.3\text{ m}^2/\text{person}$ gross.
* Mercantile / Retail (Ground floor): $2.8\text{ m}^2/\text{person}$ gross.
* Assembly (Unconcentrated tables & chairs): $1.4\text{ m}^2/\text{person}$ net.

---

## 3. Municipal Zoning & Balady Regulations

### A. Setbacks (الارتدادات النظامية)
* **Front Setback (شارع أمامي):**
  * Street width $< 15\text{ m}$: Minimum setback $= 1/5$ street width (minimum $3.0\text{ m}$).
  * Street width $\ge 15\text{ m}$: Minimum setback $= 4.0\text{ m} - 6.0\text{ m}$ based on local municipality master plan.
* **Side & Rear Setbacks (الارتداد الجانبي والخلفي):**
  * Residential villas: Minimum $2.0\text{ m}$ clear.
  * Commercial / Mixed-use: Minimum $3.0\text{ m} - 5.0\text{ m}$ or zero-lot line with approved fire wall where allowed.

### B. Building Height & Envelope (نسب البناء والارتفاعات)
* **Ground Coverage Ratio (GCR / نسبة التغطية):**
  * Residential villas: Maximum $60\%$ of total plot area.
  * Commercial / Mixed-use: Typically $60\% - 70\%$ ground floor, stepping back to $50\%$ on upper floors.
* **Floor Area Ratio (FAR / معامل البناء):**
  * Typically $1.8 - 2.4$ for residential, up to $3.0 - 5.0$ along designated commercial corridors.

### C. Parking Quota Matrix (معايير مواقف السيارات)
* **Residential:**
  * Villas: 1 car space inside boundary per residential unit.
  * Multi-family apartments: 1.5 car spaces per apartment unit $+ 10\%$ visitor parking.
* **Commercial Offices:** 1 space per $35\text{ m}^2 - 50\text{ m}^2$ Net Usable Area.
* **Retail & F&B:** 1 space per $20\text{ m}^2 - 25\text{ m}^2$ Gross Floor Area.
* **Schools & Education:** 1 space per 2 classrooms $+ 1$ space per 10 administrative employees $+ 1$ drop-off bay per 50 students.
