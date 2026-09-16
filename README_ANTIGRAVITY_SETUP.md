# SkillProof — Antigravity Multi-Agent Setup

> **Core Message**: Assess → Learn → Practice → Build → Prove  
> **USP**: *From skill gaps to career proof.*  
> **Repository Governance**: All work is governed by [`docs/00_SOURCE_OF_TRUTH_SKILLPROOF.md`](file:///d:/Roy/SkillProof-Support-AI/docs/00_SOURCE_OF_TRUTH_SKILLPROOF.md).

---

## 1. Project Directory Structure

This repository is organized according to the Antigravity multi-agent workspace standards:

```text
skillproof/
├── .agents/
│   ├── agents/
│   │   ├── orchestrator/
│   │   │   └── agent.md          # Lead architect & sprint coordinator
│   │   ├── product-spec/
│   │   │   └── agent.md          # Requirements guardian & question curator
│   │   ├── backend/
│   │   │   └── agent.md          # ASP.NET Core 8 Web API & SQLite specialist
│   │   ├── frontend/
│   │   │   └── agent.md          # Next.js App Router & UI/UX specialist
│   │   ├── ai-prompt/
│   │   │   └── agent.md          # LLM prompt engineer & schema architect
│   │   └── qa/
│   │       └── agent.md          # Test automation & acceptance gatekeeper
│   │
│   └── skills/
│       ├── api-contract/
│       │   └── SKILL.md          # Frozen REST endpoint DTOs & schemas
│       ├── assessment-design/
│       │   └── SKILL.md          # Question design & rubric definitions
│       ├── llm-evaluation/
│       │   └── SKILL.md          # LLM scoring pipelines & guardrails
│       ├── qa-verification/
│       │   └── SKILL.md          # Verification, regression & demo gates
│       └── skillproof-build/
│           └── SKILL.md          # Run, build, test & demo runbook
│
├── backend/
│   ├── SkillProof.Api/           # ASP.NET Core 8 Web API
│   ├── SkillProof.Api.Tests/     # xUnit integration tests
│   └── SkillProof.slnx           # Solution file
│
├── frontend/                     # Next.js App Router application
│   ├── app/                      # App router pages
│   └── public/                   # Static assets
│
├── harness/                      # Deterministic validation harness
│   ├── fixtures/                 # Sample responses & seed data
│   ├── schemas/                  # Output JSON contracts
│   └── validators/               # Node.js contract validation scripts
│
├── docs/
│   ├── 00_SOURCE_OF_TRUTH_SKILLPROOF.md         # Absolute product authority & 22 agent rules
│   ├── 01_IMPLEMENTATION_SPEC_SKILLPROOF.md     # 3-day technical specification & data contracts
│   ├── 02_PRESENTATION_OVERVIEW_SKILLPROOF.md   # Pitch deck flow, demo narrative & value prop
│   ├── 03_MULTI_AGENT_SOURCE_WORKFLOW_SKILLPROOF.md # Multi-agent collaboration protocol & lifecycle
│   ├── 04_DOCUMENTATION_AUDIT_CHECKLIST.md      # Quality gate checklist & audit procedure
│   └── 05_CURATED_SEED_QUESTIONS_SKILLPROOF.md  # Curated seed questions & backend rubrics
│
└── README_ANTIGRAVITY_SETUP.md                  # This setup guide
```

---

## 2. Documentation Architecture

| Document | Authority | Key Focus |
|---|---|---|
| [`00_SOURCE_OF_TRUTH_SKILLPROOF.md`](file:///d:/Roy/SkillProof-Support-AI/docs/00_SOURCE_OF_TRUTH_SKILLPROOF.md) | **Supreme (Priority 1)** | Product vision, target roles, 22 agent rules, unverified claims tagging. |
| [`01_IMPLEMENTATION_SPEC_SKILLPROOF.md`](file:///d:/Roy/SkillProof-Support-AI/docs/01_IMPLEMENTATION_SPEC_SKILLPROOF.md) | **Technical (Priority 2)** | Modular monolith stack (Next.js + ASP.NET Core + SQLite), 4 main screens, 3-day plan, Section 17 acceptance criteria. |
| [`03_MULTI_AGENT_SOURCE_WORKFLOW_SKILLPROOF.md`](file:///d:/Roy/SkillProof-Support-AI/docs/03_MULTI_AGENT_SOURCE_WORKFLOW_SKILLPROOF.md) | **Operational (Priority 3)** | 6-agent responsibilities, inter-agent handoff contracts, phased lifecycle, guardrails. |
| [`.agents/agents/**/agent.md`](file:///d:/Roy/SkillProof-Support-AI/.agents/agents/) | **Agent Definitions (Priority 4)** | Specific roles, domains, system missions, and operational boundaries. |
| [`.agents/skills/**/SKILL.md`](file:///d:/Roy/SkillProof-Support-AI/.agents/skills/) | **Skill Runbooks (Priority 5)** | On-demand executable procedures, schemas, build and verification flows. |
| [`02_PRESENTATION_OVERVIEW_SKILLPROOF.md`](file:///d:/Roy/SkillProof-Support-AI/docs/02_PRESENTATION_OVERVIEW_SKILLPROOF.md) | **Narrative (Priority 6)** | Pitch deck flow, problem statement, core loop, demo script. Cannot override implementation. |
| [`04_DOCUMENTATION_AUDIT_CHECKLIST.md`](file:///d:/Roy/SkillProof-Support-AI/docs/04_DOCUMENTATION_AUDIT_CHECKLIST.md) | **Audit Standard** | Documentation gate checklist, blocker definitions, and pass criteria. |
| [`05_CURATED_SEED_QUESTIONS_SKILLPROOF.md`](file:///d:/Roy/SkillProof-Support-AI/docs/05_CURATED_SEED_QUESTIONS_SKILLPROOF.md) | **Assessment Content** | Exactly 6 curated seed questions and backend-only rubrics per prototype role. |

---

## 3. The 6 Specialized AI Agents

Customized agents are located in `.agents/agents/` and are automatically recognized by Antigravity:

1. **`orchestrator`** (`.agents/agents/orchestrator/agent.md`)
   - Directs the multi-agent team, manages sprint velocity, resolves cross-agent blockers, and performs final sign-off against acceptance criteria.

2. **`product-spec`** (`.agents/agents/product-spec/agent.md`)
   - Protects scope. Enforces that only the two designated roles (**Backend Developer** and **Financial Analyst**) are implemented. Verifies authentic interview question sources. Rejects feature creep.

3. **`backend`** (`.agents/agents/backend/agent.md`)
   - Builds and maintains the ASP.NET Core 8 Web API, SQLite persistence with EF Core, seed data initialization, REST endpoints, and resilient fallback mechanisms.

4. **`frontend`** (`.agents/agents/frontend/agent.md`)
   - Delivers the Next.js App Router user interface with premium visuals, responsive layouts, and interactive skill profile/roadmap flows.

5. **`ai-prompt`** (`.agents/agents/ai-prompt/agent.md`)
   - Crafts diagnostic evaluation prompts, skill gap extraction logic, and the core **gap-backward project recommendation prompt**. Enforces pure JSON output schemas and tags unverified claims.

6. **`qa`** (`.agents/agents/qa/agent.md`)
   - Executes automated API contract tests, verifies UI flows, validates offline fallback behavior, and ensures the end-to-end 2–3 minute demo is rock solid.

---

## 4. Antigravity Skills

The workspace includes 5 specialized skills located under `.agents/skills/`:

1. **`api-contract`** (`.agents/skills/api-contract/SKILL.md`):
   - Defines and freezes request/response DTOs, endpoint schemas, validation constraints, and error envelopes across all 5 canonical routes.
2. **`assessment-design`** (`.agents/skills/assessment-design/SKILL.md`):
   - Curates realistic interview-style questions, case studies, and 4-tier rubric evaluation criteria.
3. **`llm-evaluation`** (`.agents/skills/llm-evaluation/SKILL.md`):
   - Implements structured prompt templates, JSON schema validation, and guardrails preventing hallucinated candidate evidence.
4. **`qa-verification`** (`.agents/skills/qa-verification/SKILL.md`):
   - End-to-end testing procedures, regression sweeps, and the 2–3 minute live demo gate verification.
5. **`skillproof-build`** (`.agents/skills/skillproof-build/SKILL.md`):
   - Standardized command sequences for environment checks, running backend and frontend, and testing.

---

## 5. Architectural Invariant

> [!IMPORTANT]
> **Engineering Process ≠ Application Architecture**:
> - We utilize specialized **AI Agents in Antigravity** to accelerate development, review code, and verify quality.
> - The **SkillProof application itself is a simple, robust Modular Monolith** (ASP.NET Core Web API + Next.js + SQLite).
> - As mandated by Rule 6 of the Source of Truth: **Do not introduce microservices or multi-agent runtime inside the web application**.
