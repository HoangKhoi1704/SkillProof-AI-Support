# 03 — MULTI-AGENT SOURCE WORKFLOW — SKILLPROOF

## Purpose

This file defines the development workflow for multiple AI agents working on SkillProof.

All agents must read:

1. `docs/00_SOURCE_OF_TRUTH_SKILLPROOF.md`
2. `docs/01_IMPLEMENTATION_SPEC_SKILLPROOF.md`
3. This workflow file

`00_SOURCE_OF_TRUTH_SKILLPROOF.md` has the highest priority.

If requirements conflict, follow the source of truth and report the conflict.

---

## Core Rule

Do not build the entire product at once.

The implementation order is:

```text
FOUNDATION
→ DIAGNOSTIC FLOW
→ AI ASSESSMENT
→ SKILL GAP
→ ROADMAP
→ PROJECT RECOMMENDATION
→ QA / DEMO
```

---

## Agent Team

### 1. Orchestrator

Owns:
- milestone planning,
- task delegation,
- integration order,
- scope control,
- conflict resolution.

Must NOT:
- redesign product requirements,
- silently change API contracts,
- allow scope creep.

### 2. Product / Spec Agent

Owns:
- competency framework,
- assessment structure,
- interview-style question design,
- rubrics,
- acceptance criteria,
- product consistency.

Writes primarily:
- `docs/`
- seed competency/question definitions when explicitly assigned.

### 3. Backend Agent

Owns:
- ASP.NET Core Web API,
- SQLite / persistence,
- DTOs,
- services,
- validation,
- API contracts,
- LLM service integration.

Writes primarily:
- `backend/`

### 4. Frontend Agent

Owns:
- Next.js UI,
- role selection,
- career-readiness test,
- skill profile,
- roadmap,
- project recommendation UI,
- API integration.

Writes primarily:
- `frontend/`

### 5. AI / Prompt Agent

Owns:
- assessment prompts,
- structured JSON schemas,
- rubric application,
- roadmap generation prompt,
- gap-based project recommendation prompt,
- AI guardrails.

Writes primarily:
- `backend/**/AI/`
- `prompts/`
- AI-related tests.

### 6. QA Agent

Owns:
- API tests,
- AI evaluation cases,
- end-to-end checks,
- guardrail verification,
- demo checklist,
- regression checks.

Writes primarily:
- `tests/`

---

## Shared Product Flow

Every agent must preserve this flow:

```text
Choose Target Role
        ↓
Career Readiness Test
        ↓
AI Skill Profile
        ↓
Skill Gap Analysis
        ↓
Personalized Roadmap
        ↓
Gap-Based Real-World Project
        ↓
Career Evidence
```

For the 3-day MVP, the required functional path ends at the recommended project.

---

## Milestone 1 — Foundation

Owner:
- Backend Agent
- Frontend Agent

Backend:
- create ASP.NET Core API,
- add `GET /api/roles`,
- add `GET /api/diagnostics/questions`,
- seed Backend Developer and Financial Analyst.

Frontend:
- create Next.js app,
- role-selection page,
- diagnostic page,
- connect to role/question APIs.

Exit criteria:

```text
User can:
Choose role
→ receive questions
→ enter answers
```

No LLM required yet.

---

## Milestone 2 — End-to-End Diagnostic Skeleton

Owner:
- Backend Agent
- Frontend Agent

Add:

```text
POST /api/diagnostics/evaluate
```

Initially a deterministic/mock response is allowed.

Frontend must display:

```text
Skill
Level
Reason
```

Allowed levels:

```text
Beginner
Intermediate
Advanced
Insufficient Evidence
```

Exit criteria:

```text
Choose Role
→ Complete Test
→ Submit
→ Skill Profile
```

works end-to-end.

---

## Milestone 3 — AI Assessment

Owner:
- AI / Prompt Agent
- Backend Agent
- Product Agent for rubric verification

Input:

```text
Target Role
+ Competency
+ Interview / Case Question
+ Rubric
+ Student Answer
```

Output:

```json
{
  "competency": "SQL",
  "level": "Beginner",
  "reason": "...",
  "evidence": ["..."]
}
```

Rules:
- structured JSON only,
- no unsupported skill inference,
- no arbitrary percentage score,
- insufficient information = `Insufficient Evidence`,
- LLM output must be validated by backend.

Exit criteria:
- same sample answers produce sensible and explainable levels,
- malformed output is handled safely.

---

## Milestone 4 — Skill Gap & Roadmap

Owner:
- Product Agent
- AI / Prompt Agent
- Backend Agent
- Frontend Agent

System selects at most 3 priority gaps.

Generate:

```text
Gap
→ Learning Goal
→ Practice Task
```

Do not recommend relearning skills already demonstrated at an adequate level.

Exit criteria:
- user sees top gaps,
- user sees a concise prioritized roadmap.

---

## Milestone 5 — Gap-Based Project

Owner:
- AI / Prompt Agent
- Product Agent
- Backend Agent
- Frontend Agent

The recommended project must be derived from diagnosed gaps.

Every major project requirement must map to a gap.

Example:

```text
Gap: SQL
→ Project requirement: relational model + non-trivial queries

Gap: Testing
→ Project requirement: unit tests for success/failure paths
```

Exit criteria:
- project title,
- reason,
- requirements,
- visible skill-to-requirement mapping.

---

## Milestone 6 — QA & Demo

Owner:
- QA Agent

Must test:
- both roles,
- empty answers,
- malformed AI output,
- `Insufficient Evidence`,
- roadmap prioritization,
- project-gap mapping,
- API errors,
- loading states.

Demo path:

```text
Backend Developer
→ 5–8 questions
→ Skill Profile
→ Top 3 Gaps
→ Roadmap
→ Expense Management API
```

Target demo time:
2–3 minutes.

---

## Parallelization Rules

Safe to run in parallel:

```text
Backend API skeleton
||
Frontend visual skeleton
||
Product rubric/question preparation
```

After API contracts are frozen:

```text
Frontend API integration
||
AI prompt implementation
```

Run QA after each integration milestone.

Do NOT let two agents edit the same file at the same time.

---

## Change Control

Any change to these requires Orchestrator approval:

- target roles,
- skill-level taxonomy,
- core API contracts,
- core product flow,
- architecture,
- MVP scope.

Agents must report ambiguity instead of guessing.

---

## Completion Definition

The MVP is complete when:

```text
Role
→ Test
→ AI Skill Profile
→ Gaps
→ Roadmap
→ Gap-Based Project
```

runs without manual intervention for both:
- Backend Developer
- Financial Analyst
