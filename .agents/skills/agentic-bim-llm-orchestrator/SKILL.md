---
name: agentic-bim-llm-orchestrator
description: Architects autonomous LLM agent loops inside Autodesk Revit, translating natural language prompts into validated Revit API Transactions via Function Calling, ExternalEvent dispatching, and failure preprocessors.
---

# Agentic BIM & LLM Orchestrator

Cutting-edge agentic AI engineering skill for orchestrating LLM tool calling directly inside Autodesk Revit. Translates natural language requests into safe, multi-step Revit API transactions, ensuring thread safety, warning suppression, and execution validation.

---

## 1. System Architecture: The Agentic Revit Bridge

```
[ User Natural Language Prompt ]
              │
              ▼
   [ LLM Function Calling / Router ] ──(Generates structured JSON tool call payload)
              │
              ▼
   [ WebSocket / Named Pipe IPC ] ──(Sends JSON payload to Revit Add-in Process)
              │
              ▼
[ IExternalEventHandler.Execute() ] ──(Safely enters Revit Single-Threaded API Context)
              │
              ├── Step 1: Element Filtering (FilteredElementCollector)
              ├── Step 2: Dry-Run Validation & Transaction.Start()
              ├── Step 3: Modification & IFailuresPreprocessor Warning Suppression
              └── Step 4: Transaction.Commit()
              │
              ▼
   [ Structured Feedback / Diff ] ──(Returns success report + changed Element IDs to LLM)
```

---

## 2. Thread Safety & ExternalEvent Pattern (C#)

Revit's API is strictly single-threaded. Any asynchronous call from an LLM server or WebSocket listener will throw a fatal `Autodesk.Revit.Exceptions.InvalidOperationException` if called directly. All agent actions must be marshaled via `ExternalEvent`:

```csharp
using System;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace AgenticBim
{
    public class AgenticCommandHandler : IExternalEventHandler
    {
        public string ActionType { get; set; }
        public string JsonArguments { get; set; }
        public Action<string> OnExecutionComplete { get; set; }

        public void Execute(UIApplication app)
        {
            Document doc = app.ActiveUIDocument.Document;
            
            using (Transaction t = new Transaction(doc, "Agentic Action: " + ActionType))
            {
                FailureHandlingOptions failOpt = t.GetFailureHandlingOptions();
                failOpt.SetFailuresPreprocessor(new SilentWarningSuppressor());
                t.SetFailureHandlingOptions(failOpt);
                
                t.Start();
                try
                {
                    // Dispatch agent action
                    string resultSummary = ProcessAgentAction(doc, ActionType, JsonArguments);
                    t.Commit();
                    OnExecutionComplete?.Invoke(resultSummary);
                }
                catch (Exception ex)
                {
                    t.RollBack();
                    OnExecutionComplete?.Invoke("ERROR: " + ex.Message);
                }
            }
        }

        public string GetName() => "AgenticBimHandler";

        private string ProcessAgentAction(Document doc, string action, string args)
        {
            // Business logic for dynamic element modification
            return "SUCCESS";
        }
    }

    public class SilentWarningSuppressor : IFailuresPreprocessor
    {
        public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
        {
            failuresAccessor.DeleteAllWarnings();
            return FailureProcessingResult.Continue;
        }
    }
}
```

---

## 3. Tool Calling JSON Schemas for LLM Routing

### Tool Definition Example: `batch_update_parameter`
```json
{
  "name": "batch_update_parameter",
  "description": "Updates a specific parameter value across elements matching category and filter criteria in Revit.",
  "parameters": {
    "type": "object",
    "properties": {
      "category": {
        "type": "string",
        "enum": ["OST_Doors", "OST_Windows", "OST_Walls", "OST_Rooms", "OST_DuctCurves"]
      },
      "filter_parameter": {
        "type": "string",
        "description": "Parameter name used to select target elements (e.g. 'Level' or 'Comments')"
      },
      "filter_value": {
        "type": "string",
        "description": "Value to match"
      },
      "target_parameter": {
        "type": "string",
        "description": "Parameter to be updated (e.g. 'Fire Rating')"
      },
      "new_value": {
        "type": "string",
        "description": "New value to apply"
      }
    },
    "required": ["category", "target_parameter", "new_value"]
  }
}
```

---

## 4. Safety Guardrails & Rollback Safeguards
1. **Never Allow Destructive Mass Deletions Without Explicit Confirmation**:
   - If a prompt implies deleting $> 10$ elements, the agent must require user approval with a specific ElementId count summary.
2. **Transaction Rollback on Warning Severity**:
   - If a failure has severity `FailureSeverity.Error`, the transaction is rolled back immediately to protect document integrity.
3. **Audit Log Trail**:
   - Every agentic action is written to a local `.log` file detailing timestamp, prompt, changed Element IDs, and execution duration in milliseconds.
