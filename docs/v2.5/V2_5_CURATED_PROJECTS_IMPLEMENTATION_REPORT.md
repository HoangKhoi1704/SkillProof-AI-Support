# SkillProof V2.5 — Curated Projects Implementation Report

**Milestone:** V2.5 Curated Learning Resources + Project Catalog  
**Status:** Completed  
**Date:** September 17, 2026  
**Catalog Files:**
- `data/v3/learning-resources.json` (20 verified resources)
- `data/v3/projects.json` (9 curated benchmark projects)  
**Backend Services:**
- `backend/SkillProof.Api/Services/LearningResourceService.cs`
- `backend/SkillProof.Api/Services/CuratedProjectMatcher.cs`

---

## 1. Executive Summary

Milestone V2.5 extends the V2.4 personalized canonical roadmap by introducing **verified canonical node learning resources** and a **curated gap-based project catalog**.

### 1.1 Core Principles Adherence
1. **Zero Invented URLs:** All 20 learning resources resolve strictly to primary official documentation (MDN, WHATWG, Postgres, Python, OWASP, Pandas). YouTube was intentionally and completely omitted to prevent link rot and video hallucination.
2. **Zero Invented Projects:** Practice and portfolio projects are fixed catalog benchmarks with strict deliverable definitions and verifiable evidence standards. The LLM is never invoked to invent project titles or scenarios.
3. **Deterministic Zero-OpenAI Matching:** Project recommendations are derived directly from the user's diagnosed skill gaps stored in the session profile. Even when AI runtime mode is active, candidate selection is 100% deterministic and bound to the verified catalog.
4. **Qualitative Evidence Standards:** Numeric percentages (e.g. "87% match") are strictly disallowed across all UI surfaces and APIs. Match rationale is presented via qualitative gap-targeting explanations.
5. **Clear Scope Boundary:** V2.6 GitHub repo cloning and automated rubric code verification is explicitly deferred and has **NOT** been implemented in this release.

---

## 2. Curation Pipeline & Selection Criteria

### 2.1 Project Origin & License Provenance
Projects were selected following a dual-tier strategy:
1. **External Open-Source Tutorials (Practice Exercises):**
   - Source: `practical-tutorials/project-based-learning` (Commit: `fcd321e`).
   - Rationale: High-quality, step-by-step tutorials that guide learners through foundational and applied concepts (e.g., Kanban boards, in-memory key-value caching daemons, storage engine prototypes).
2. **SkillProof-Curated Benchmark Assets (Portfolio Projects):**
   - Rationale: Multi-competency, enterprise-grade scenarios designed to produce tangible GitHub repository artifacts that can be audited against rigorous technical criteria.

### 2.2 Practice vs. Portfolio Project Breakdown

| Project ID | Role(s) | Type | Difficulty | Scope | Source / Locator | Deliverables | Evidence Req |
| :--- | :--- | :--- | :--- | :--- | :--- | :---: | :---: |
| `proj-fe-weather-practice` | `frontend-developer` | practice | foundation | 4–6 hours | `practical-tutorials/project-based-learning` (`commit:fcd321e/## JavaScript`) | 3 | 3 |
| `proj-fe-todo-practice` | `frontend-developer` | practice | foundation | 3–5 hours | `practical-tutorials/project-based-learning` (`commit:fcd321e/## JavaScript`) | 3 | 3 |
| `proj-fe-platform-portfolio` | `frontend-developer` | portfolio | intermediate | 20–30 hours | `practical-tutorials/project-based-learning` (`commit:fcd321e/#### React`) | 4 | 3 |
| `proj-be-database-practice` | `backend-developer` | practice | intermediate | 8–12 hours | `practical-tutorials/project-based-learning` (`commit:fcd321e/## C/C++`) | 3 | 3 |
| `proj-be-cache-practice` | `backend-developer` | practice | intermediate | 6–10 hours | `practical-tutorials/project-based-learning` (`commit:fcd321e/## C/C++`) | 3 | 3 |
| `proj-be-resilient-portfolio` | `backend-developer` | portfolio | intermediate | 25–35 hours | `skillproof-curated` (`skillproof/backend/portfolio-01`) | 5 | 3 |
| `proj-da-eda-practice` | `data-analyst` | practice | foundation | 5–8 hours | `skillproof-curated` (`skillproof/data-analyst/practice-01`) | 3 | 3 |
| `proj-da-sql-practice` | `data-analyst` | practice | foundation | 4–6 hours | `skillproof-curated` (`skillproof/data-analyst/practice-02`) | 3 | 3 |
| `proj-da-bi-portfolio` | `data-analyst` | portfolio | intermediate | 20–30 hours | `skillproof-curated` (`skillproof/data-analyst/portfolio-01`) | 4 | 3 |

*\*Provenance Note: In V2.6 audit reconciliation, Table 2.2 was reconciled directly against the authoritative `data/v3/projects.json` catalog and its verified Git commit SHA locators.*

**Totals:** 9 Curated Projects (6 Practice exercises, 3 Portfolio benchmark assets).

---

## 3. Zero-OpenAI Evidence & Deterministic Matching Engine

### 3.1 Deterministic Matcher Logic (`CuratedProjectMatcher`)
The matching engine operates strictly on verified session data:
1. Retrieves the trusted `CareerReadinessProfile` from `AdaptiveSessionState`.
2. Identifies diagnosed skill gaps categorized as `NeedsDevelopment` or `Prioritized`.
3. Filters catalog projects matching the candidate's target role.
4. Computes skill overlap score:
   $$\text{Overlap} = |\text{ProjectCanonicalSkills} \cap \text{DiagnosedGaps}|$$
5. Sorts candidates by overlap score (descending), mandatory fundamental coverage, and difficulty appropriateness.
6. Returns separated lists: `PracticeProjects` (focused, 1–2 gap reinforcement) and `PortfolioProjects` (comprehensive multi-gap evidence).
7. Generates deterministic, qualitative explanations for why each project targets the candidate's specific needs without mentioning match percentages.

### 3.2 Live AI Guardrails & Bypassing
- In `POST /api/diagnostics/adaptive/sessions/{sessionId}/project/recommend`, the endpoint checks for curated projects via `curatedProjectMatcher.MatchForSessionAsync(...)`.
- If matched, the curated benchmark is formatted directly into `GapBasedProjectDto` and returned immediately.
- Zero OpenAI tokens are consumed for project ideation, structure generation, or resource discovery.

---

## 4. SQLite Schema Reconciliation & Data Persistence

### 4.1 Schema Definitions (`Entities.cs` & `CatalogDbContext.cs`)
Two new entities were integrated into the SQLite database:
1. **`LearningResource` Table:**
   - Columns: `Id` (PK), `Title`, `SourceName`, `SourceUrl`, `ResourceType`, `Level`, `IsOfficial`, `VerificationStatus`, `VerifiedAt`, `Locator`.
   - Joined to canonical skills via `LearningResourceSkills` (`ResourceId`, `SkillId`) and roles via `LearningResourceRoles` (`ResourceId`, `RoleId`).
2. **`CuratedProject` Table:**
   - Columns: `Id` (PK), `Title`, `ProjectType`, `Difficulty`, `EstimatedScope`, `Description`, `SourceUrl`, `VerificationStatus`, `VerifiedAt`, `DeliverablesJson`, `EvidenceRequirementsJson`.
   - Joined to skills via `CuratedProjectSkills` (`ProjectId`, `SkillId`) and roles via `CuratedProjectRoles` (`ProjectId`, `RoleId`).

### 4.2 SQLite Migration Handling in `CatalogSeeder.cs`
Because EF Core's `Database.EnsureCreatedAsync()` ignores new entities when a SQLite database file already exists, `CatalogSeeder.InitializeAsync` executes raw DDL statements (`CREATE TABLE IF NOT EXISTS`) before running idempotent seed loops:
- `data/v3/learning-resources.json` → 20 records seeded/upserted.
- `data/v3/projects.json` → 9 records seeded/upserted.

---

## 5. End-to-End User Experience & UI Integration

### 5.1 Roadmap Node Learning Resources (`/roadmap/[nodeId]`)
- When a user clicks any node in the interactive roadmap graph, `/roadmap/[nodeId]` calls `GET /api/v3/roadmap/nodes/{nodeId}/resources`.
- Displays a dedicated "Verified Learning Resources" section showing official source badges, level indicators (Foundation, Applied, Advanced), and direct external links.
- Graceful empty state displayed if a custom node has no attached resources.

### 5.2 Curated Projects Hub (`/projects`)
- Replaced previous ad-hoc project cards with two structured sections:
  1. **Targeted Practice Projects:** Designed for quick 4–12 hour gap remediation.
  2. **Portfolio Evidence Projects:** Flagged as high-value portfolio assets.
- Explicit qualitative explanations displayed: *"Directly targets diagnosed focus areas: [Skill Name] (Needs Development)"*.
- All numeric match scores and percentages have been stripped.
- Added a dedicated "Data Analyst Evidence Flexibility" notice confirming that deployed full-stack web applications are not mandatory for data roles (Jupyter notebooks, executive BI dashboards, and documented SQL scripts are accepted).

### 5.3 Project Specification Detail (`/projects/[projectId]`)
- Displays comprehensive technical breakdown:
  - Practiced Canonical Competencies tags.
  - Required Technical Deliverables checklist.
  - Verifiable Evidence Standards.
  - External Tutorial Links (for practice exercises).
  - "Select for Portfolio Evidence" button that smoothly binds the project to the user's active session and transitions to `/portfolio`.

---

## 6. Scope Boundary: Remaining V2.6 Work

The following features were intentionally excluded from V2.5 and remain scheduled for Milestone V2.6:
1. **GitHub Repository Ingestion & File Tree Inspection:** Cloning or querying GitHub Octokit APIs to inspect actual user commit history and file structure.
2. **Automated Evidence Evaluation Engine:** Running automated test suites against user repos or evaluating Jupyter notebook AST outputs.
3. **Rubric-Scored Verification & Certification:** Generating tamper-evident cryptographic certificates or verifiable credentials.
