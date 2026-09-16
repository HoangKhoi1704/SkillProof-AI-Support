# SkillProof

> **AI Career Readiness Diagnostic & Project Coach**  
> *From skill gaps to career proof.*

SkillProof is an AI career-readiness platform that diagnoses a student's current skill level for a target career role, identifies top skill gaps, generates a personalized learning roadmap, and recommends a real-world project intentionally designed backward from those gaps to turn demonstrated work into verifiable career evidence.

---

## Core Product Loop

```text
Assess → Learn → Practice → Build → Prove
```

1. **Target Role Selection**: Choose between prototype target roles (`Backend Developer` or `Financial Analyst`).
2. **Career Readiness Diagnostic**: Answer 5–8 realistic interview-style, practical case, and reasoning questions.
3. **AI Skill Profile**: Review qualitative 4-tier skill levels (`Beginner`, `Intermediate`, `Advanced`, `Insufficient Evidence`) backed by concrete reasoning and evidence.
4. **Skill Gap Prioritization**: Identify top 3 critical skill gaps.
5. **Personalized Learning Roadmap**: Receive focused learning priorities and practical exercises.
6. **Gap-Backward Project Recommendation**: Receive a project engineered specifically around the diagnosed gaps (e.g. *Expense Management API* for Backend Developer).

---

## Prototype Technology Stack

- **Backend**: ASP.NET Core 8 Web API (`backend/SkillProof.Api`)
- **Frontend**: Next.js 14+ App Router (`frontend/app`)
- **Database**: SQLite (`skillproof.db`)
- **Testing**: xUnit (`backend/SkillProof.Api.Tests`), Playwright E2E (`tests/e2e`), Promptfoo (`promptfooconfig.yaml`)
- **Architecture**: Modular Monolith

---

## Quickstart

Detailed build, run, test, and live-demo commands are standardized in [`.agents/skills/skillproof-build/SKILL.md`](file:///d:/Roy/SkillProof-Support-AI/.agents/skills/skillproof-build/SKILL.md).

### Start Backend
```powershell
cd backend/SkillProof.Api
dotnet restore
dotnet run --urls="http://localhost:5000"
```
Swagger UI available at: `http://localhost:5000/swagger`

### Start Frontend
```powershell
cd frontend
npm install
npm run dev
```
Web client available at: `http://localhost:3000`

---

## Documentation & Governance

- [`docs/00_SOURCE_OF_TRUTH_SKILLPROOF.md`](file:///d:/Roy/SkillProof-Support-AI/docs/00_SOURCE_OF_TRUTH_SKILLPROOF.md) — Supreme product authority & agent rules
- [`docs/01_IMPLEMENTATION_SPEC_SKILLPROOF.md`](file:///d:/Roy/SkillProof-Support-AI/docs/01_IMPLEMENTATION_SPEC_SKILLPROOF.md) — 3-day technical specification & frozen API contracts
- [`docs/03_MULTI_AGENT_SOURCE_WORKFLOW_SKILLPROOF.md`](file:///d:/Roy/SkillProof-Support-AI/docs/03_MULTI_AGENT_SOURCE_WORKFLOW_SKILLPROOF.md) — Multi-agent development lifecycle
- [`docs/04_DOCUMENTATION_AUDIT_CHECKLIST.md`](file:///d:/Roy/SkillProof-Support-AI/docs/04_DOCUMENTATION_AUDIT_CHECKLIST.md) — Quality gates & audit checklist
- [`docs/05_CURATED_SEED_QUESTIONS_SKILLPROOF.md`](file:///d:/Roy/SkillProof-Support-AI/docs/05_CURATED_SEED_QUESTIONS_SKILLPROOF.md) — Curated assessment questions & backend rubrics
- [`README_ANTIGRAVITY_SETUP.md`](file:///d:/Roy/SkillProof-Support-AI/README_ANTIGRAVITY_SETUP.md) — Antigravity multi-agent workspace setup
