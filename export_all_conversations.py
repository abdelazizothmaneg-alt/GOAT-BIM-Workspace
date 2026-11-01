import os
import glob
import json
import re
from datetime import datetime

BRAIN_DIR = os.path.expanduser(r"~\.gemini\antigravity\brain")
OUTPUT_DIR = os.path.join(os.path.dirname(__file__), "CONVERSATIONS_ARCHIVE")

os.makedirs(OUTPUT_DIR, exist_ok=True)

conv_dirs = [d for d in glob.glob(os.path.join(BRAIN_DIR, "*")) if os.path.isdir(d) and os.path.basename(d) != "tempmediaStorage"]
conv_dirs.sort(key=os.path.getmtime)  # Chronological order

index_entries = []

print(f"Processing {len(conv_dirs)} conversations...")

for idx, cdir in enumerate(conv_dirs, start=1):
    cid = os.path.basename(cdir)
    tpath = os.path.join(cdir, ".system_generated", "logs", "transcript.jsonl")
    if not os.path.exists(tpath):
        continue

    turns = []
    first_user_prompt = ""
    start_time = None

    try:
        with open(tpath, "r", encoding="utf-8", errors="ignore") as f:
            for line in f:
                line = line.strip()
                if not line:
                    continue
                try:
                    entry = json.loads(line)
                except Exception:
                    continue

                ts = entry.get("created_at", "")
                if not start_time and ts:
                    start_time = ts

                src = entry.get("source", "")
                typ = entry.get("type", "")
                content = entry.get("content", "")

                if src == "USER_EXPLICIT" and typ == "USER_INPUT":
                    clean_content = content.strip()
                    # Strip user request tags if present
                    clean_content = re.sub(r"</?USER_REQUEST>", "", clean_content).strip()
                    if clean_content and not first_user_prompt:
                        first_user_prompt = clean_content[:120].replace("\n", " ").strip()
                    turns.append({"role": "User", "content": clean_content, "time": ts})

                elif typ == "PLANNER_RESPONSE" and content:
                    clean_resp = content.strip()
                    tool_calls = entry.get("tool_calls", [])
                    tools_used = [tc.get("tool") or tc.get("name") or "tool" for tc in tool_calls if isinstance(tc, dict)]
                    turns.append({
                        "role": "Antigravity (Gemini)",
                        "content": clean_resp,
                        "tools": tools_used,
                        "time": ts
                    })
    except Exception as e:
        print(f"Error reading {cid}: {e}")
        continue

    if not turns:
        continue

    # Clean title
    topic = first_user_prompt or "General Architectural & BIM Task"
    topic_single_line = " ".join(topic.split())
    safe_topic = re.sub(r'[\r\n\\/*?:"<>|]', "", topic_single_line)[:40].strip()
    safe_topic = safe_topic or f"Session_{cid[:8]}"

    date_str = start_time[:10] if start_time else "Unknown_Date"
    filename = f"{idx:02d}_{date_str}_{safe_topic.replace(' ', '_')}.md"
    filepath = os.path.join(OUTPUT_DIR, filename)

    # Write Markdown
    with open(filepath, "w", encoding="utf-8") as out:
        out.write(f"# محادثة رقم {idx}: {topic}\n")
        out.write(f"- **Conversation ID:** `{cid}`\n")
        out.write(f"- **Start Date:** `{start_time}`\n")
        out.write(f"- **Total Turns:** {len(turns)}\n\n")
        out.write("---\n\n")

        for turn in turns:
            role = turn["role"]
            t_time = turn.get("time", "")
            t_content = turn.get("content", "")
            tools = turn.get("tools", [])

            out.write(f"### 👤 {role} ({t_time})\n")
            if tools:
                out.write(f"*الأدوات المنفذة: `{', '.join(tools)}`*\n\n")
            out.write(f"{t_content}\n\n")
            out.write("---\n\n")

    index_entries.append({
        "index": idx,
        "date": date_str,
        "cid": cid,
        "topic": topic,
        "turns": len(turns),
        "filename": filename
    })

# Write Master Index
index_path = os.path.join(OUTPUT_DIR, "README_MASTER_CONVERSATIONS.md")
with open(index_path, "w", encoding="utf-8") as out:
    out.write("# 📚 الأرشيف الشامل لكافة المحادثات السابقة — All Conversation Transcripts\n")
    out.write("> **المالك والمطور:** المهندس عبد العزيز عثمان (**أبو ياسين**)\n")
    out.write(f"> **إجمالي المحادثات المؤرشفة:** {len(index_entries)} جلسة عمل وتطوير معمارية وبرمجية.\n\n")
    out.write("هذا المجلد يحتوي على التفريغ الكامل لكل حوار، وقرار معماري، وتطوير برمجي تم بين المهندس عبد العزيز و Antigravity.\n\n")
    out.write("| # | التاريخ | موضوع المحادثة والهدف | عدد التبادلات | رابط ملف المحادثة |\n")
    out.write("| :---: | :---: | :--- | :---: | :--- |\n")
    for item in index_entries:
        out.write(f"| {item['index']} | `{item['date']}` | {item['topic'][:80]} | {item['turns']} | [{item['filename']}](./{item['filename']}) |\n")

print(f"[SUCCESS] Exported {len(index_entries)} conversations to {OUTPUT_DIR}")
