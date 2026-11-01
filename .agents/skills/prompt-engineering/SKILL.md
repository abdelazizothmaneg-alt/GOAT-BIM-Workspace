---
name: prompt-engineering
description: >-
  Designs, refines, and evaluates high-precision system prompts, few-shot demonstrations, chain-of-thought instructions,
  and constraint enforcement templates for LLMs and agentic workflows.
---

# Prompt Engineering

Systematic methodology for crafting robust, deterministic, and highly aligned instructions for Large Language Models and AI Agents.

## 1. Structural Prompt Architecture
A production-grade prompt consists of five distinct layers:
1. **Persona & Role Grounding:** Identity, expertise level, tone, and ethical boundaries.
2. **Context & Domain Data:** Technical constraints, schema definitions, and background knowledge.
3. **Execution Instructions:** Step-by-step reasoning or algorithmic procedures.
4. **Few-Shot Demonstrations:** Paired input-reasoning-output examples showcasing edge cases.
5. **Output Constraints & Formatting:** Strict schema requirements (e.g., Markdown, JSON, XML tags, LaTeX math syntax).

## 2. Advanced Cognitive Patterns
* **Chain-of-Thought (CoT):** Explicitly prompting the model to reason through intermediate steps before producing conclusions.
* **Progressive Disclosure:** Breaking complex multi-phase tasks into sequential verification checkpoints.
* **Negative Constraints:** Explicit "DO NOT" rules paired with positive alternatives to eliminate hallucinations.
* **Self-Critique & Verification Loop:** Instructing the model to validate output against invariants before returning the final response.
