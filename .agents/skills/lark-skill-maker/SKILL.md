---
name: lark-skill-maker
description: >-
  Meta-skill to help developers and AI agents author, test, and package custom Lark/Feishu CLI skills.
  Guides the discovery of Lark Open Platform APIs, schema inspection, command execution patterns, and SKILL.md template generation.
metadata:
  requires:
    bins: ["lark-cli"]
---

# Lark Skill Maker

Authoring guide and meta-generator for creating custom AI agent skills powered by the official Lark / Feishu CLI (`lark-cli`).

## 1. Skill Creation Workflow

When building a new Lark automation skill, follow this three-step lifecycle:

### Step 1: API & Command Discovery
1. **Explore Available Services:**
   ```bash
   lark-cli --help
   ```
2. **Explore Service Resources & Commands:**
   ```bash
   lark-cli <service> --help
   # Example: lark-cli im --help (Instant Messaging)
   # Example: lark-cli bitable --help (Lark Base / Multi-dimensional tables)
   # Example: lark-cli calendar --help (Calendar events)
   ```
3. **Inspect Schema & Parameter Invariants:**
   ```bash
   lark-cli schema <service>.<resource>.<method>
   # Example: lark-cli schema im.message.create
   ```

### Step 2: Command Precedence Rules
Always prioritize commands in this order:
1. **Shortcut Commands (`+verb`):** E.g., `lark-cli im +send --chat-id <id> --text "Hello"` (fastest, pre-packaged).
2. **Registered API Commands:** E.g., `lark-cli im.message.create --body '{...}'` (strictly typed).
3. **Raw API Fallback:** E.g., `lark-cli api POST /open-apis/im/v1/messages --data '{...}'` (for newly released endpoints).

### Step 3: Template Generation
Package the new workflow into a standard `SKILL.md` inside `.agents/skills/lark-<name>/SKILL.md`.

---

## 2. Standard Lark `SKILL.md` Template

```markdown
---
name: lark-<skill-name>
description: "<Clear, concise description of what the skill does and when the agent should trigger it>."
metadata:
  requires:
    bins: ["lark-cli"]
---

# Lark <Skill Title>

## 1. Overview & Use Case
Explain the workflow, input parameters, and expected outcome on Lark.

## 2. Required Scopes & Permissions
| Operation | Required Lark Open Platform Scope |
| :--- | :--- |
| Read Records | `bitable:app:read` |
| Send Messages | `im:message:send_as_bot` |
| Manage Calendar | `calendar:calendar:read`, `calendar:calendar:write` |

## 3. Command Execution
\`\`\`bash
# 1. Search or fetch target ID
lark-cli <service> <resource> <list_method> --query "..."

# 2. Execute mutation
lark-cli <service> <resource> <create_method> --data '{...}'
\`\`\`

## 4. Error Handling
- **Invalid Token / Expired Session:** Guide user to re-authenticate via `lark-cli auth login`.
- **Permission Denied (Code 99991663 / 99991664):** Prompt developer to request the required scope in the Lark Developer Console.
```

---

## 3. High-Value Automation Scenarios for AEC & Construction
* **Lark Base (Bitable) BIM Synchronization:**
  Automatically push QTO takeoffs from Excel (`dwg-to-excel`) into multi-dimensional Lark Base tables for project teams.
* **Automated Site Issue Alerts:**
  Broadcast critical BIM clash reports or site RFI notifications into dedicated project group chats.
* **Milestone Calendar Sync:**
  Convert CPM project schedules (`execution-planning`) into shared Lark calendar milestones.
