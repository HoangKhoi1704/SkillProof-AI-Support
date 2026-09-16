---
name: assessment-design
description: Designs SkillProof career-readiness assessments using role competencies, realistic interview-style questions, practical cases, reasoning prompts, rubrics, and source-verification rules.
---

# Assessment Design Skill

Use this skill when creating or reviewing SkillProof diagnostic tests.

## Goal
Create a career-readiness test that evaluates real job readiness rather than only academic recall.

## Core Flow
```text
Target Role
→ Competency
→ Interview / Case Question
→ Rubric
→ Expected Evidence
→ Skill Level
```

## Procedure

### 1. Read Source Files
Read the authoritative source files in `docs/`: `00_SOURCE_OF_TRUTH_SKILLPROOF.md`, `01_IMPLEMENTATION_SPEC_SKILLPROOF.md`, `03_MULTI_AGENT_SOURCE_WORKFLOW_SKILLPROOF.md`, `04_DOCUMENTATION_AUDIT_CHECKLIST.md`, and `05_CURATED_SEED_QUESTIONS_SKILLPROOF.md`.

### 2. Select Role Competencies
Prototype roles: Backend Developer (`backend-developer`) and Financial Analyst (`financial-analyst`). Only assess competencies defined in the project source of truth/spec.

### 3. Choose Question Type
Use a mix of Knowledge, Interview, Practical Case, and Reasoning questions.

### 4. Create Realistic Questions
Questions should reflect common hiring expectations, realistic workplace situations, practical problem solving, explanation ability, and role-specific reasoning. Avoid trivia.

### 5. Source Rules
If a question is claimed to come from a real company/interview/published dataset, store a verifiable source. Otherwise mark `[source not verified - unverified]`.

### 6. Define Rubric
Every question must have a backend evaluation rubric using ONLY: Beginner, Intermediate, Advanced, Insufficient Evidence.
> **Privacy Invariant**: Rubrics are backend-only evaluation data and must NEVER be exposed in public question DTOs.

Example:
```text
Competency: Financial Modeling
Question: How would you build a 3-year revenue forecast?
Beginner: generic answer, no clear assumptions or drivers.
Intermediate: uses historical trend, assumptions, and key revenue drivers.
Advanced: adds scenarios/sensitivity and links forecast to broader financial statements.
Insufficient Evidence: empty, irrelevant, or too vague.
```

### 7. Define Expected Evidence
Specify what evidence in the answer would support each level.

### 8. Keep the Prototype Short
Use 5–8 questions per role for the MVP (curated in `docs/05_CURATED_SEED_QUESTIONS_SKILLPROOF.md`).

### 9. Review Assessment Quality
Each question must map to one primary competency, have a clear rubric, and be job-relevant.

## Output Format
Prefer structured definitions with role, competency, type, question, rubric (backend only), sourceType, and sourceReference.

