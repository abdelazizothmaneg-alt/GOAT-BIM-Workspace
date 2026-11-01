import os
import docx
from docx.shared import Inches, Pt, RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.enum.style import WD_STYLE_TYPE
from docx.oxml import OxmlElement
from docx.oxml.ns import qn

def set_cell_margins(cell, top=100, bottom=100, left=150, right=150):
    tcPr = cell._tc.get_or_add_tcPr()
    tcMar = OxmlElement('w:tcMar')
    for m, val in [('top', top), ('bottom', bottom), ('left', left), ('right', right)]:
        node = OxmlElement(f'w:{m}')
        node.set(qn('w:w'), str(val))
        node.set(qn('w:type'), 'dxa')
        tcMar.append(node)
    tcPr.append(tcMar)

def add_bottom_border(paragraph, color_hex="0F2B48", size="12"):
    pPr = paragraph._element.get_or_add_pPr()
    pBdr = OxmlElement('w:pBdr')
    bottom = OxmlElement('w:bottom')
    bottom.set(qn('w:val'), 'single')
    bottom.set(qn('w:sz'), size)
    bottom.set(qn('w:space'), '4')
    bottom.set(qn('w:color'), color_hex)
    pBdr.append(bottom)
    pPr.append(pBdr)

def build_resume():
    doc = docx.Document()
    
    # Page setup - Margins (14mm approx 0.55 in)
    sections = doc.sections
    for section in sections:
        section.top_margin = Inches(0.5)
        section.bottom_margin = Inches(0.5)
        section.left_margin = Inches(0.55)
        section.right_margin = Inches(0.55)
        
    primary_color = RGBColor(15, 43, 72)     # #0F2B48 Navy
    secondary_color = RGBColor(26, 82, 118) # #1A5276 Dark Blue
    text_color = RGBColor(44, 62, 80)       # #2C3E50 Charcoal
    muted_color = RGBColor(86, 101, 115)    # #566573 Gray
    
    # Base font
    normal_style = doc.styles['Normal']
    normal_style.font.name = 'Calibri'
    normal_style.font.size = Pt(9.5)
    normal_style.font.color.rgb = text_color
    
    # --- HEADER ---
    p_name = doc.add_paragraph()
    p_name.paragraph_format.space_before = Pt(0)
    p_name.paragraph_format.space_after = Pt(2)
    p_name.paragraph_format.alignment = WD_ALIGN_PARAGRAPH.CENTER
    r_name = p_name.add_run("ABDELAZIZ OSMAN")
    r_name.font.size = Pt(20)
    r_name.font.bold = True
    r_name.font.color.rgb = primary_color
    
    p_title = doc.add_paragraph()
    p_title.paragraph_format.space_before = Pt(0)
    p_title.paragraph_format.space_after = Pt(2)
    p_title.paragraph_format.alignment = WD_ALIGN_PARAGRAPH.CENTER
    r_title = p_title.add_run("Senior Architectural Project Manager | Architect Team Leader")
    r_title.font.size = Pt(11)
    r_title.font.bold = True
    r_title.font.color.rgb = secondary_color
    
    p_sub = doc.add_paragraph()
    p_sub.paragraph_format.space_before = Pt(0)
    p_sub.paragraph_format.space_after = Pt(4)
    p_sub.paragraph_format.alignment = WD_ALIGN_PARAGRAPH.CENTER
    r_sub = p_sub.add_run("Architectural Design Leadership • PMP® Project Governance • BIM-Enabled Delivery")
    r_sub.font.size = Pt(9)
    r_sub.font.italic = True
    r_sub.font.color.rgb = muted_color
    
    p_contact = doc.add_paragraph()
    p_contact.paragraph_format.space_before = Pt(0)
    p_contact.paragraph_format.space_after = Pt(2)
    p_contact.paragraph_format.alignment = WD_ALIGN_PARAGRAPH.CENTER
    r_cnt = p_contact.add_run("Jazan, Saudi Arabia  |  +966 50 125 5227  |  aothman.ksa@gmail.com  |  LinkedIn  |  Behance")
    r_cnt.font.size = Pt(8.5)
    r_cnt.font.color.rgb = text_color
    
    p_lang = doc.add_paragraph()
    p_lang.paragraph_format.space_before = Pt(0)
    p_lang.paragraph_format.space_after = Pt(8)
    p_lang.paragraph_format.alignment = WD_ALIGN_PARAGRAPH.CENTER
    add_bottom_border(p_lang, color_hex="0F2B48", size="12")
    r_l1 = p_lang.add_run("Languages: ")
    r_l1.bold = True
    r_l1.font.size = Pt(8.5)
    r_l2 = p_lang.add_run("Arabic (Native - Executive & Government Presentations)  |  English (Full Professional Proficiency)")
    r_l2.font.size = Pt(8.5)
    
    def add_section_header(title):
        p = doc.add_paragraph()
        p.paragraph_format.space_before = Pt(8)
        p.paragraph_format.space_after = Pt(4)
        add_bottom_border(p, color_hex="1A5276", size="6")
        r = p.add_run(title.upper())
        r.font.size = Pt(10)
        r.font.bold = True
        r.font.color.rgb = primary_color
        return p

    def add_bullet(p_or_doc, bold_prefix, text):
        p = doc.add_paragraph(style='List Bullet')
        p.paragraph_format.space_before = Pt(1)
        p.paragraph_format.space_after = Pt(2)
        p.paragraph_format.line_spacing = 1.15
        r_b = p.add_run(bold_prefix + ": ")
        r_b.bold = True
        r_b.font.size = Pt(8.8)
        r_b.font.color.rgb = primary_color
        r_t = p.add_run(text)
        r_t.font.size = Pt(8.8)
        return p

    # --- PROFESSIONAL SUMMARY ---
    add_section_header("Professional Summary")
    p_sum = doc.add_paragraph()
    p_sum.paragraph_format.space_before = Pt(2)
    p_sum.paragraph_format.space_after = Pt(6)
    p_sum.paragraph_format.line_spacing = 1.15
    r_sum = p_sum.add_run(
        "Accomplished, PMP®-certified Senior Architectural Project Manager and LEED Green Associate™ with over 12 years of specialized leadership across architectural design, multidisciplinary coordination, and consultant-side delivery within prominent engineering consultancies. Recognized technical authority in the Saudi Building Code (SBC 201/801/601), NFPA 101 Life Safety regulations, and municipal permitting platforms (Balady and Salamah). Proven track record directing cross-functional teams to deliver over 100,000+ sqm BUA valued at 150M+ SAR—including the Residence of H.R.H. the Governor of Jazan Region, tier-1 retail brands (Jarir, LuLu, Al-Nahdi), master-planned compounds, and urban waterfronts. Expert in bridging conceptual architectural vision with PMI frameworks, Design Review Checklists, and BIM-enabled coordination workflows (Revit & Navisworks Manage). Adept at value engineering, active scope control, and fast-track statutory approvals."
    )
    r_sum.font.size = Pt(9)

    # --- CORE COMPETENCIES & TECHNICAL TOOLKIT ---
    add_section_header("Core Competencies & Technical Toolkit")
    add_bullet(doc, "Project Leadership & Governance", "Team Mentorship, Full Project Lifecycle (Initiation to Handover), PMI Frameworks, Scope Change Control, Work Breakdown Structures (WBS), Value Engineering (VE), Risk Mitigation, Change Order Prevention.")
    add_bullet(doc, "BIM & Technical Coordination", "Autodesk Revit Architecture (Practical Modeling & Component Applications), Navisworks Manage (Clash Review & Visual Model Audit), Multi-Trade BIM Coordination Workflows (Continuous Professional Upskilling), Automated Quantity Takeoff (QTO), Coordinated Shop Drawings, AutoCAD.")
    add_bullet(doc, "Codes, Regulations & Governance", "Saudi Building Code (SBC 201/801/601/602), NFPA 101 Life Safety Code, Balady Municipal Permitting, Civil Defense (Salamah), Urban Visual Identity Guidelines, Traffic Impact Studies (TIS), ADA Accessibility.")
    add_bullet(doc, "Sustainability & Green Building", "LEED Green Associate™ Standards, Mostadam (Saudi Green Building Framework), Energy-Efficient Envelope Design (SBC 601/602), Sustainable Material Selection.")
    add_bullet(doc, "Digital Platforms & Software", "3ds Max, V-Ray, Corona Renderer, Adobe Photoshop, MS Project, Primavera P6 (Working Knowledge).")

    # --- PROFESSIONAL EXPERIENCE ---
    add_section_header("Professional Experience")

    def add_job_header(role, company, location, dates):
        p = doc.add_paragraph()
        p.paragraph_format.space_before = Pt(6)
        p.paragraph_format.space_after = Pt(2)
        r_r = p.add_run(role + " | ")
        r_r.bold = True
        r_r.font.size = Pt(9.5)
        r_r.font.color.rgb = primary_color
        
        r_c = p.add_run(company)
        r_c.bold = True
        r_c.font.size = Pt(9)
        r_c.font.color.rgb = secondary_color
        
        # Right aligned meta via spaces/tabs or separate right-aligned run
        r_m = p.add_run(f"  ({location}  |  {dates})")
        r_m.font.size = Pt(8.5)
        r_m.font.italic = True
        r_m.font.color.rgb = muted_color

    # Job 1
    add_job_header("Architect Team Leader", "Hassan Mohammad Faqih Consulting Engineers", "Jazan, KSA", "Jun 2026 – Present")
    add_bullet(doc, "Strategic & Elite Projects Leadership", "Directed the architectural design and multidisciplinary technical delivery for high-profile regional developments, most notably the Residence of H.R.H. the Governor of Jazan Region, safeguarding strict privacy, ultra-luxury aesthetic benchmarks, and architectural excellence.")
    add_bullet(doc, "Team Leadership & Quality Governance", "Mentored and directed a multidisciplinary team of 8 architects; instituted standardized Design Review Checklists and PMP-aligned Work Breakdown Structures (WBS), optimizing workload distribution and cutting internal design revision cycles by 30% across concurrent projects exceeding 100,000+ sqm BUA in Jazan and Abu Arish.")
    add_bullet(doc, "Stakeholder Governance & Scope Control", "Established a structured Scope Change Control process for high-profile investors and VIP stakeholders, mitigating scope creep and preserving schedule/budget baselines while maintaining exceptional client satisfaction.")
    add_bullet(doc, "Value Engineering & Cost Optimization", "Initiated targeted Value Engineering (VE) workshops on facade envelopes, structural interfaces, and imported finishing specifications, achieving an estimated 10% cost optimization (multi-million SAR) without compromising architectural integrity.")
    add_bullet(doc, "Statutory Permitting & 100% First-Pass Approval", "Accelerated statutory municipal permitting cycles by 25% via Balady and Salamah platforms; maintained a 100% first-pass approval record for building licenses and occupancy certificates through rigorous pre-submission SBC compliance audits.")
    add_bullet(doc, "BIM-Enabled Interdisciplinary Coordination", "Leveraged Revit and Navisworks workflows to facilitate weekly interdisciplinary coordination sessions, proactively auditing models and resolving over 200+ multi-trade clashes prior to IFC issuance, eliminating costly site reworks.")

    # Job 2
    add_job_header("Senior Architect & Project Management Engineer", "Environmental Means Co.", "Jazan, KSA", "Jun 2022 – Jun 2026")
    add_bullet(doc, "Commercial & Tier-1 Brand Delivery", "Spearheaded the end-to-end architectural project management for marquee retail developments—including LuLu Hypermarket, Jarir Bookstore, Al-Nahdi Care, and an integrated Strip Mall—delivering all assets 15% ahead of target schedules while adhering strictly to international corporate brand manuals.")
    add_bullet(doc, "Urban Waterfront Transformation", "Led the architectural design and consultant-side execution for the \"Sunset Walk\" (Mamsha Al-Ghoroub) waterfront in Jazan (1.5+ km promenade), creating a signature urban landmark that enhanced the city's visual and public realm.")
    add_bullet(doc, "BIM-Driven Detailing & Automated QTO", "Supervised the production of coordinated LOD 350 architectural execution packages and material schedules directly from federated Revit models, cutting site RFIs by 40% and preventing budget overruns.")
    add_bullet(doc, "Statutory Approvals & Urban Visual Identity", "Secured all required municipal endorsements and statutory approvals from Amanat Jazan, ensuring 100% adherence to local visual identity guidelines, Traffic Impact Studies (TIS), and Civil Defense safety requirements.")
    add_bullet(doc, "Sustainable Design Integration (LEED / Mostadam)", "Prepared architectural technical packages and sustainability documentation aligning commercial projects with LEED and Mostadam green building criteria, optimizing thermal performance and water efficiency.")
    add_bullet(doc, "Site Supervision & Quality Assurance", "Provided proactive consultant-side technical support, managed contractor material submittals, verified on-site mock-ups, and resolved critical site discrepancies to ensure uncompromised quality.")

    # Job 3
    add_job_header("Senior Architect", "MHG CONSULTING", "Cairo, Egypt", "Apr 2019 – May 2022")
    add_bullet(doc, "Mega-Projects Leadership", "Directed full-lifecycle architectural packages for large-scale mixed-use developments in collaboration with international consultancies (including HDRIC), delivering prominent developments such as the Vivienda Compound and Pearl Des Rois.")
    add_bullet(doc, "GCC Technical Coordination", "Led technical alignment and drawing production for Gulf-market developments in partnership with UAE engineering firms, ensuring full compliance with regional municipal regulations and GCC design standards.")
    add_bullet(doc, "Contractor & Submittal Governance", "Acted as primary technical authority on-site; evaluated material submittals, supervised mock-up reviews, and resolved on-site structural/architectural discrepancies, ensuring zero milestone delays.")

    # Job 4
    add_job_header("Co-Founder & Design Lead", "Flamingo Studio", "Cairo, Egypt", "May 2019 – Feb 2022")
    add_bullet(doc, "Boutique Practice Leadership", "Co-founded and scaled an independent design boutique, managing client acquisition, design scope, contracts, and financial budgets for 20+ luxury residential and commercial fit-out projects.")
    add_bullet(doc, "Turnkey Delivery", "Directed internal design teams from concept design and photorealistic visualization through complete tender packages and on-site fit-out handover.")

    # --- EARLY CAREER HIGHLIGHTS ---
    add_section_header("Early Career Highlights (2014 – 2019)")
    def add_early_bullet(role, company, dates, desc):
        p = doc.add_paragraph(style='List Bullet')
        p.paragraph_format.space_before = Pt(1)
        p.paragraph_format.space_after = Pt(2)
        r_r = p.add_run(f"{role} | {company} ({dates}): ")
        r_r.bold = True
        r_r.font.size = Pt(8.5)
        r_r.font.color.rgb = primary_color
        r_d = p.add_run(desc)
        r_d.font.size = Pt(8.5)

    add_early_bullet("Senior Architectural Visualizer", "Vanilla Studio", "2018 – 2019", "Promoted to Senior within 5 months. Led high-end photorealistic rendering pipelines (3ds Max, Corona, V-Ray) for landmark developments by major Egyptian developers (Armonia New Capital, Emaar Mivida Boulevard, Obelisko Capitale).")
    add_early_bullet("Mid-Level Architect", "TCG - Touch Consultants Group", "2016 – 2018", "Produced detailed architectural working drawings, execution shop drawings, and interdisciplinary coordination for UAE-based projects.")
    add_early_bullet("Architect", "Royal Palace Interior Decoration LLC", "2014 – 2016", "Remotely managed luxury UAE interior fit-out projects from Cairo; prepared full FF&E selections, interior details, and custom joinery shop drawings.")
    add_early_bullet("Junior Architect", "Shosha Kamal Design House", "2014 – 2015", "Contributed to prominent national projects, including the interior renovation of The Greek Campus (Tahrir Square) and patient spaces within 57357 Children's Cancer Hospital.")

    # --- EDUCATION ---
    add_section_header("Education")
    p_edu = doc.add_paragraph()
    p_edu.paragraph_format.space_before = Pt(2)
    p_edu.paragraph_format.space_after = Pt(4)
    r_ed1 = p_edu.add_run("Bachelor of Architectural Engineering")
    r_ed1.bold = True
    r_ed1.font.size = Pt(9)
    r_ed2 = p_edu.add_run(" — Delta Higher Institute of Engineering & Technology, Egypt (July 2014)")
    r_ed2.font.size = Pt(9)

    # --- CERTIFICATIONS & AFFILIATIONS ---
    add_section_header("Certifications & Professional Affiliations")
    certs = [
        "Professional Accredited Architect — Saudi Council of Engineers (SCE #864653)",
        "Project Management Professional (PMP®) — Project Management Institute (PMI Cert. #4322897)",
        "LEED Green Associate™ — USGBC / GBCI (Credential ID: 11676431-GREEN-ASSOCIATE)",
        "NFPA 101: Life Safety Code Essentials — National Fire Protection Association",
        "Architectural Association Mittelmeerland 5 Global School — AA School of Architecture, London",
        "BIM & Revit Architecture Professional Track — In Progress / Continuous Professional Development"
    ]
    for c in certs:
        p_c = doc.add_paragraph(style='List Bullet')
        p_c.paragraph_format.space_before = Pt(1)
        p_c.paragraph_format.space_after = Pt(2)
        r_c = p_c.add_run(c)
        r_c.font.size = Pt(8.5)
        
    output_path = r"C:\Users\faqih\.gemini\antigravity\scratch\abdelaziz_osman_cv\CV_Abdelaziz_Osman.docx"
    doc.save(output_path)
    print(f"Successfully generated DOCX at: {output_path}")

if __name__ == "__main__":
    build_resume()
