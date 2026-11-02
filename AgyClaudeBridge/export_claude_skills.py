# -*- coding: utf-8 -*-
import os
import zipfile

WORKSPACE_DIR = r"C:\Users\faqih\.gemini\antigravity\scratch"
SKILLS_DIR = os.path.join(WORKSPACE_DIR, ".agents", "skills")
ZIP_OUT = os.path.join(WORKSPACE_DIR, "CLAUDE_SKILLS_PACK.zip")

count = 0
with zipfile.ZipFile(ZIP_OUT, "w", zipfile.ZIP_DEFLATED) as zipf:
    for root, dirs, files in os.walk(SKILLS_DIR):
        if "SKILL.md" in files:
            skill_name = os.path.basename(root)
            file_path = os.path.join(root, "SKILL.md")
            arcname = f"{skill_name}.md"
            zipf.write(file_path, arcname)
            count += 1

print(f"[OK] Packaged {count} skills into {ZIP_OUT} (Size: {os.path.getsize(ZIP_OUT) / 1024:.2f} KB)")
