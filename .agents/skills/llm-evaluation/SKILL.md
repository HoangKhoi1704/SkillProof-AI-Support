---
name: llm-evaluation
description: Implements and verifies SkillProof LLM evaluation flows for diagnostic answers, skill-gap prioritization, roadmaps, and gap-based project recommendations using structured outputs and guardrails.
---

# LLM Evaluation Skill

Use this skill whenever SkillProof sends student data to an LLM for assessment or recommendation.

## Goal
Produce explainable, structured, evidence-based AI outputs that the backend can safely validate.

## Core Flow
```text
Question + Competency + Rubric + Student Answer
→ Prompt
→ LLM
→ Structured JSON
→ Schema Validation
→ Skill Assessment
```

## Rules
The LLM must never invent student experience, infer ability without evidence, use arbitrary numeric percentages, make hiring decisions, or redefine role competencies. If evidence is insufficient, return `Insufficient Evidence`.

## Procedure

### 1. Read Source Files
Read the authoritative source files in `docs/`: `00_SOURCE_OF_TRUTH_SKILLPROOF.md`, `01_IMPLEMENTATION_SPEC_SKILLPROOF.md`, `03_MULTI_AGENT_SOURCE_WORKFLOW_SKILLPROOF.md`, `04_DOCUMENTATION_AUDIT_CHECKLIST.md`, and `05_CURATED_SEED_QUESTIONS_SKILLPROOF.md`.

### 2. Separate Instructions from Student Input
Use explicit sections: SYSTEM/ROLE, TASK, ROLE CONTEXT, COMPETENCY, QUESTION, RUBRIC, STUDENT ANSWER, OUTPUT SCHEMA, RULES.

### 3. Require Structured Output
Example:
```json
{
  "competency": "SQL",
  "level": "Beginner",
  "reason": "The student identifies basic database checks but does not discuss indexing or execution plans.",
  "evidence": ["Mentions checking slow queries"]
}
```

### 4. Validate Output
Validate JSON, required fields, allowed enums, and reasonable string lengths. Retry malformed output once if appropriate; otherwise fail safely.

### 5. Skill Gap Prioritization
Use target role, skill assessments, and competency importance. Prioritize at most 3 gaps for the MVP.

### 6. Roadmap Generation
Each item must include Skill, Priority, Learning Goal, and Practice Task. Avoid generic course dumps.

### 7. Gap-Based Project Recommendation
Every major project requirement must map to at least one diagnosed gap.

### 8. Evaluation Cases
Maintain test cases for strong, weak, irrelevant, empty, prompt-injection-style answers, and malformed model responses.

## Completion Checklist
- prompt uses explicit rubric
- output schema is defined
- enums are validated
- insufficient evidence is handled
- reasoning is explainable
- roadmap maps to gaps
- project requirements map to gaps
