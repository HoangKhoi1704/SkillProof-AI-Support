# 04 --- Documentation Audit Checklist

## Purpose

This file is the **Documentation Gate** for SkillProof. Before
implementation or a new milestone, the Orchestrator must verify that
product docs, implementation rules, agent instructions, and testing
responsibilities are consistent.

**Do not start application code while an unresolved BLOCKER remains.**

## 1. Documentation Authority

Use this priority when documents conflict:

1.  `00_SOURCE_OF_TRUTH_SKILLPROOF.md`
2.  `01_IMPLEMENTATION_SPEC_SKILLPROOF.md`
3.  `03_MULTI_AGENT_SOURCE_WORKFLOW_SKILLPROOF.md`
4.  `.agents/agents/**/agent.md`
5.  `.agents/skills/**/SKILL.md`
6.  `02_PRESENTATION_OVERVIEW_SKILLPROOF.md`

Rules: - `00_SOURCE_OF_TRUTH_SKILLPROOF.md` is authoritative. -
Lower-priority documents must not contradict higher-priority
documents. - `02_PRESENTATION_OVERVIEW_SKILLPROOF.md` is presentation
material and cannot override implementation. - If an important decision
is undefined, mark it **MISSING DECISION** instead of inventing it.

## 2. Product Scope

Verify: - \[ \] Product name is consistently **SkillProof**. - \[ \]
Positioning remains **AI Career Readiness Diagnostic & Project
Coach**. - \[ \] Core loop remains:
`Career Goal → Career Readiness Test → Current Skill Profile → Skill Gap → Personalized Roadmap → Practice → Gap-Based Real-World Project → Project Evaluation → Portfolio/CV Evidence → Re-assessment`. -
\[ \] MVP is not turned into a generic LMS/course platform. - \[ \] No
job marketplace, recruiter dashboard, LinkedIn integration, payments, or
social features. - \[ \] No runtime multi-agent architecture. - \[ \] No
complex RAG unless explicitly approved. - \[ \] Scope remains realistic
for the 3-day prototype.

## 3. Target Roles

Prototype roles: - `backend-developer` → Backend Developer -
`financial-analyst` → Financial Analyst

Verify: - \[ \] Both roles are consistent across docs/tests/agents. - \[
\] Role IDs use string slugs. - \[ \] No conflicting numeric role IDs. -
\[ \] No additional MVP role is silently introduced.

## 4. Career Readiness Assessment

Assessment is a **Career Readiness Test**, not only an academic quiz.

Question types: - Knowledge - Interview-style - Practical case -
Reasoning

Allowed levels only: - `Beginner` - `Intermediate` - `Advanced` -
`Insufficient Evidence`

Verify: - \[ \] No `Expert`. - \[ \] No arbitrary percentage or 0--100
readiness score. - \[ \] Empty/insufficient answers can produce
`Insufficient Evidence`. - \[ \] Lack of evidence is not automatically
lack of skill. - \[ \] AI provides reasoning/evidence. - \[ \]
Competency framework/rubrics remain the evaluation source of truth.

## 5. Sources and Claims

Verify: - \[ \] Realistic interview-style questions need not claim a
company source. - \[ \] A real company/interview source is claimed only
when verifiable. - \[ \] Unverified claims are explicitly tagged,
e.g. `[source not verified - unverified]`. - \[ \] Agents must not
invent interview/company sources. - \[ \] SkillProof outcome claims are
not stated as validated without evidence. - \[ \] Competitive
superiority is not stated as validated without evidence.

## 6. API Contract

Canonical MVP routes: - `GET /api/roles` -
`GET /api/diagnostics/questions` - `POST /api/diagnostics/evaluate` -
`POST /api/roadmaps/generate` - `POST /api/projects/recommend`

Verify: - \[ \] Canonical plural routes are used. - \[ \] No conflicting
active singular routes. - \[ \] Frontend and backend use the same
contracts. - \[ \] `roleId` uses the agreed slug. - \[ \]
Request/response DTO expectations are consistent. - \[ \] AI output can
be validated before becoming authoritative app data.

## 7. Architecture

Approved prototype architecture:

`Next.js Frontend → ASP.NET Core Web API → Services → SQLite / LLM API`

Verify: - \[ \] Frontend: Next.js. - \[ \] Backend: ASP.NET Core Web
API. - \[ \] Prototype database: SQLite. - \[ \] One LLM API. - \[ \]
LLM API key is not exposed to frontend. - \[ \] LLM calls go through
backend. - \[ \] Modular monolith architecture. - \[ \] No unnecessary
microservices. - \[ \] Antigravity development agents are not confused
with runtime architecture.

## 8. AI Guardrails

Verify: - \[ \] AI cannot invent student experience/evidence. - \[ \] AI
does not make hiring decisions. - \[ \] SkillProof is not formal
certification. - \[ \] Invalid/empty AI output is handled safely. - \[
\] Structured output is preferred where practical. - \[ \] Backend
validates model output. - \[ \] Raw LLM output is not automatically
authoritative. - \[ \] Roadmap maps to diagnosed gaps. - \[ \] Project
requirements map to diagnosed gaps.

## 9. Testing Responsibilities

### xUnit

-   [ ] Backend tests are under `backend/SkillProof.Api.Tests/`.
-   [ ] `WebApplicationFactory` is used when API integration testing is
    appropriate.
-   [ ] `dotnet build` passes.
-   [ ] `dotnet test` passes.

### Playwright

-   [ ] E2E tests are under `tests/e2e/`.
-   [ ] Tests target the Next.js application.
-   [ ] Smoke test passes.
-   [ ] Future tests cover the SkillProof happy path.

### Promptfoo

-   [ ] Used for LLM/prompt quality evaluation, not ordinary backend
    testing.
-   [ ] Evaluation cases are separate from production data.
-   [ ] Paid LLM evaluations are not run unintentionally.
-   [ ] Evaluation criteria follow SkillProof rubrics/guardrails.

### SkillProof Harness

-   [ ] `harness/fixtures/` contains test examples.
-   [ ] `harness/schemas/` contains output contracts.
-   [ ] `harness/validators/` contains deterministic validators.
-   [ ] Validators enforce the four allowed levels.
-   [ ] Malformed outputs are rejected.
-   [ ] Gap-to-roadmap/project traceability validators are added as
    those features are implemented.

## 10. Multi-Agent Ownership

  -----------------------------------------------------------------------
  Agent                               Primary responsibility
  ----------------------------------- -----------------------------------
  Orchestrator                        Planning, delegation, integration,
                                      scope control, milestone gates

  Product/Spec                        Competencies, questions, rubrics,
                                      product consistency

  Backend                             ASP.NET APIs, SQLite, validation,
                                      LLM integration

  Frontend                            Next.js UI and API integration

  AI/Prompt                           Prompts, structured output, LLM
                                      evaluation, guardrails

  QA                                  Independent API, UI, AI, E2E
                                      verification
  -----------------------------------------------------------------------

Verify: - \[ \] Responsibilities do not materially conflict. - \[ \]
Orchestrator does not silently change scope. - \[ \] Backend/Frontend
share the API contract. - \[ \] AI/Prompt does not redefine competency
rules. - \[ \] QA verifies instead of silently rewriting requirements.

## 11. Repository Hygiene

Search active `.md` files for: - \[ \] `Proofly` - \[ \] legacy paths
such as `D:\Roy\Proofly` - \[ \] obsolete project names - \[ \] numeric
assessment scores - \[ \] obsolete/singular API routes - \[ \] numeric
role IDs - \[ \] unsupported roles - \[ \] obsolete architecture
decisions - \[ \] contradictory requirements - \[ \] TODOs that block
the next milestone

Legacy references must be removed or explicitly marked
historical/non-authoritative.

## 12. Milestone 1 Readiness

M1 target:

`Choose Role → Load Career Readiness Questions → Enter Answers`

Verify: - \[ \] Backend Developer questions/rubrics are sufficiently
defined. - \[ \] Financial Analyst questions/rubrics are sufficiently
defined. - \[ \] Approximately 5--8 questions per role for the
prototype. - \[ \] `GET /api/roles` contract is clear. - \[ \]
`GET /api/diagnostics/questions` contract is clear. - \[ \]
Role-selection UI requirements are clear. - \[ \] Diagnostic-test UI
requirements are clear. - \[ \] M1 does not require an LLM. - \[ \] No
visual design is invented when none is specified. - \[ \] M1 exit
criteria are testable.

M1 exit criterion:

`Choose Role → Load Questions → Enter Answers`

## 13. Severity

**BLOCKER** --- contradiction/missing decision that can make
implementation diverge.

**NON-BLOCKER** --- documentation issue that does not prevent safe
implementation.

**MISSING DECISION** --- necessary product/implementation decision
absent from authoritative docs. Do not invent it; request approval.

## 14. Required Audit Report

Orchestrator must return:

  -------------------------------------------------------------------------------
  Severity    File        Section     Issue       Source-of-truth   Recommended
                                                  decision          fix
  ----------- ----------- ----------- ----------- ----------------- -------------

  -------------------------------------------------------------------------------

Then: - `BLOCKERS` - `NON-BLOCKERS` - `MISSING DECISIONS` -
`DOCUMENTATION READINESS: PASS` or `FAIL`

## 15. PASS Criteria

`DOCUMENTATION READINESS: PASS` is allowed only when: - \[ \] No
unresolved BLOCKER. - \[ \] No unresolved MISSING DECISION prevents the
next milestone. - \[ \] Product scope is consistent. - \[ \] Target
roles and role IDs are consistent. - \[ \] Assessment taxonomy is
consistent. - \[ \] API contracts are consistent enough for the next
milestone. - \[ \] Architecture and AI guardrails are consistent. - \[
\] Agent ownership is clear. - \[ \] Testing responsibilities are
documented. - \[ \] Legacy references do not affect active
instructions. - \[ \] Next milestone exit criteria are testable.

Only after PASS may implementation proceed.

## 16. Orchestrator Audit Prompt

``` text
Perform the SkillProof Documentation Gate defined in
docs/04_DOCUMENTATION_AUDIT_CHECKLIST.md.

Audit:
- docs/00_SOURCE_OF_TRUTH_SKILLPROOF.md
- docs/01_IMPLEMENTATION_SPEC_SKILLPROOF.md
- docs/02_PRESENTATION_OVERVIEW_SKILLPROOF.md
- docs/03_MULTI_AGENT_SOURCE_WORKFLOW_SKILLPROOF.md
- all .agents/agents/**/agent.md
- all .agents/skills/**/SKILL.md

Do not modify files.
Do not write application code.

Follow the documentation authority order defined in the checklist.

Report:
1. audit table
2. BLOCKERS
3. NON-BLOCKERS
4. MISSING DECISIONS
5. DOCUMENTATION READINESS: PASS or FAIL

If something is not defined by an authoritative document, classify it as
MISSING DECISION instead of inventing a requirement.

Wait for approval before applying fixes or starting Milestone 1.
```

## 17. Final Gate

`Documentation Audit → Resolve Approved Issues → Re-audit → PASS → Start Milestone 1`

Do not skip the re-audit after documentation changes.
