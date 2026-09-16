---
name: qa-verification
description: Verifies the SkillProof MVP after each milestone through build checks, API tests, AI behavior tests, UI flow checks, regression tests, and the final 2–3 minute demo path.
---

# QA Verification Skill

Use this skill after every integration milestone and before declaring the MVP ready.

## Goal
Independently verify that SkillProof works according to the source of truth.

## Procedure

### 1. Read Source Files
Read the authoritative source files in `docs/`: `00_SOURCE_OF_TRUTH_SKILLPROOF.md`, `01_IMPLEMENTATION_SPEC_SKILLPROOF.md`, `03_MULTI_AGENT_SOURCE_WORKFLOW_SKILLPROOF.md`, `04_DOCUMENTATION_AUDIT_CHECKLIST.md`, and `05_CURATED_SEED_QUESTIONS_SKILLPROOF.md`.

### 2. Build Verification
Backend: restore, build, run tests. Frontend: install dependencies if needed, lint, build, run tests. Do not pass a milestone if the app does not build.

### 3. API Verification
Check:
- `GET /api/roles`
- `GET /api/diagnostics/questions`
- `POST /api/diagnostics/evaluate`
- `POST /api/roadmaps/generate`
- `POST /api/projects/recommend`

Verify success, invalid request, missing role, empty answers, malformed payload.

### 4. AI Behavior Verification
Test strong, weak, irrelevant, empty, contradictory, prompt-injection, and malformed model output cases.

### 5. Skill Gap Verification
Confirm top gaps match actual assessed weakness, at most 3 are prioritized, and adequate skills are not unnecessarily selected.

### 6. Roadmap Verification
Every roadmap item must map to a diagnosed gap and include a learning goal and practical task.

### 7. Project Recommendation Verification
Every major project requirement must map to at least one diagnosed gap.

### 8. Frontend Flow Verification
Test:
```text
Choose Role
→ Complete Career Readiness Test
→ Submit
→ Skill Profile
→ Skill Gaps
→ Roadmap
→ Recommended Project
```

### 9. Demo Verification
Primary demo: Backend Developer → 5–8 questions → Skill Profile → SQL/Testing/System Design gaps → Roadmap → Expense Management API. Target 2–3 minutes. Smoke-test Financial Analyst too.

### 10. Failure Report
For each failure report Title, Severity, Reproduction Steps, Expected Result, Actual Result, and Affected Area. Severity: Critical, High, Medium, Low.

## Completion Criteria
A milestone passes only when build succeeds, required flow works, acceptance criteria pass, and no critical/high blocker remains.
