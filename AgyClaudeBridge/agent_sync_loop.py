"""
agent_sync_loop.py — Autonomous Continuous Collaboration Loop
Between Antigravity (Local Builder) and Claude (Cloud Logic Designer)
"""

import os
import sys
import time
import subprocess
from datetime import datetime

WORKSPACE_DIR = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
BRAIN_MD_PATH = os.path.join(WORKSPACE_DIR, "AI_SHARED_BRAIN.md")


def run_cmd(cmd, cwd=WORKSPACE_DIR):
    res = subprocess.run(cmd, shell=True, cwd=cwd, capture_output=True, text=True)
    return res.returncode, res.stdout.strip(), res.stderr.strip()


def check_and_build():
    print(f"[{datetime.now().strftime('%H:%M:%S')}] Checking remote GitHub for Claude updates...")
    
    # 1. Fetch
    code, out, err = run_cmd("git fetch origin main")
    if code != 0:
        return

    # 2. Check if behind
    code, status_out, _ = run_cmd("git status -uno")
    if "Your branch is behind" in status_out or "can be fast-forwarded" in status_out:
        print("[!] New updates detected from GitHub! Pulling...")
        code, pull_out, _ = run_cmd("git pull origin main")
        print(f"[+] Pulled:\n{pull_out}")

        # 3. Detect changes and trigger build
        build_report = ""
        build_success = True

        # Check SbcBaladyChecker
        code, diff_files, _ = run_cmd("git diff --name-only HEAD~1 HEAD")
        print(f"[*] Modified files: {diff_files}")

        if "SbcBaladyChecker" in diff_files:
            print("[*] Triggering dotnet build for SbcBaladyChecker...")
            b_code, b_out, b_err = run_cmd("dotnet build SbcBaladyChecker/SbcBaladyChecker.csproj -c Debug")
            if b_code == 0:
                build_report = "✅ Build Succeeded! SbcBaladyChecker compiled with zero errors."
                print(f"[+] {build_report}")
            else:
                build_success = False
                error_lines = [line for line in (b_out + "\n" + b_err).splitlines() if "error" in line.lower() or "warning" in line.lower()][:10]
                build_report = "❌ Build Failed with errors:\n" + "\n".join(error_lines)
                print(f"[-] {build_report}")

        # 4. Update AI_SHARED_BRAIN.md with the build result and push back
        if build_report:
            timestamp = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
            report_md = f"\n\n### 🤖 [Antigravity Local Build Result] — *{timestamp}*\n> {build_report}\n"
            
            with open(BRAIN_MD_PATH, "r", encoding="utf-8") as f:
                content = f.read()

            with open(BRAIN_MD_PATH, "w", encoding="utf-8") as f:
                f.write(content + report_md)

            run_cmd("git add AI_SHARED_BRAIN.md")
            run_cmd(f'git commit -m "ci(antigravity): automated build report [{timestamp}]"')
            run_cmd("git push origin main")
            print("[+] Pushed build report back to GitHub for Claude to inspect.")


def daemon_loop(interval=10):
    print("==========================================================")
    print("   Antigravity ⇄ Claude Autonomous Git Collaboration Daemon")
    print("   Workspace: " + WORKSPACE_DIR)
    print("   Checking GitHub every " + str(interval) + " seconds...")
    print("==========================================================")
    try:
        while True:
            check_and_build()
            time.sleep(interval)
    except KeyboardInterrupt:
        print("\n[*] Daemon stopped.")


if __name__ == "__main__":
    interval = int(sys.argv[1]) if len(sys.argv) > 1 else 10
    daemon_loop(interval)
