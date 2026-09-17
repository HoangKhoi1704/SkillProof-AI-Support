# SkillProof V2: Final QA + Live AI + Demo Readiness Audit Report

**Author:** Antigravity AI  
**Date:** September 17, 2026  
**Status:** Product Acceptance Gate Completed  
**Milestone:** SkillProof V2 (Final Release)  
**Final Acceptance Classification:** **READY FOR DEMO WITH CAVEATS**

---

## Executive Summary

SkillProof V2 is an evidence-grounded career readiness diagnostic and portfolio proof platform. This final QA and readiness audit evaluated the complete product loop across the three primary demo roles:
$$\textbf{Frontend Developer} \quad \Big| \quad \textbf{Backend Developer} \quad \Big| \quad \textbf{Data Analyst}$$

The complete journey has been audited across all 11 stages:
$$\text{Role Selection} \longrightarrow \text{Skill Selection} \longrightarrow \text{Adaptive Assessment} \longrightarrow \text{Evidence Feedback} \longrightarrow \text{Diagnostic Skill Matrix} \longrightarrow \text{Personalized Visual Roadmap} \longrightarrow \text{Verified Learning Resources} \longrightarrow \text{Curated Project Matching} \longrightarrow \text{Structured Evidence Submission} \longrightarrow \text{Layered Evidence Verification} \longrightarrow \text{Evidence-Gated Portfolio Proof} \longrightarrow \text{Re-assessment Handoff}$$

---

## A. Final Architecture Summary

| Layer | Technology | Architectural Invariants & Role |
| :--- | :--- | :--- |
| **Backend API** | ASP.NET Core 10 (.NET 10 Web API) | Deterministic state machine, SQLite EF Core catalog, zero arbitrary candidate execution, SSRF URL defense, frozen backend baseline. |
| **Frontend Web App** | Next.js 16.3 (Turbopack, App Router, React 19) | Multi-page journey architecture, Vanilla CSS / Tailwind tokens, 30-min encrypted browser session storage, strictly no numeric match scores. |
| **Data Engine** | Data V3 Canonical Catalog | 3 primary demo roles, 59 canonical skills, 19 graph relationships, 48 frozen backend questions, 20 verified resources, 9 curated projects. |
| **Diagnostic Evaluation** | Dual-Tier Evaluator | Deterministic heuristic baseline as authoritative fallback; optional OpenAI (`gpt-5.4-mini`) structured semantic enhancement with strict schema validation. |
| **Project Verification** | Layered Verifier | Bounded static inspection (max 50 files, 64KB/file, 2MB total, .env secret redaction, GET/HEAD probes, Jupyter AST inspection, zero execution). |

---

## B. Production Data Counts & Audit

All 6 repository harness validators executed and passed with 100% compliance:

```
node harness/validators/validate-v3-catalog.mjs
node harness/validators/validate-v2.1-proposal.mjs
node harness/validators/validate-backend-assessment-data.mjs
node harness/validators/validate-v3-question-bank.mjs
node harness/validators/validate-v3-resources.mjs
node harness/validators/validate-v3-projects.mjs
```

- **Roles in UI**: 3 Primary Demo Roles (`frontend-developer`, `backend-developer`, `data-analyst`). Financial Analyst is preserved in legacy storage but excluded from V2 UI.
- **Canonical Skills**: 59 unique skills mapped across core, recommended, optional, and language classifications.
- **Roadmap Relationships**: 19 directional relationships governing prerequisites and progression.
- **Frozen Backend Question Bank**: Exactly 48 questions (16 assessable skills $\times$ 3 questions).
  - SHA-256 Checksum: `42a6b6b3a6b83f0fbb8c69163d23f178d0ad46c4b3ed1c81245ee04f27f23eb3` (Identical match to V2.0 baseline).
  - 18/18 protected Core fingerprints verified.
- **Learning Resources**: 20 catalogued resources across official documentation, guides, and tutorials.
- **Curated Projects**: 9 projects (6 practice, 3 portfolio) mapped to diagnosed gaps.

---

## C. Provenance Audit Result

1. **Learning Resources Mappings**:
   - `JWT.io / Auth0 Introduction to JSON Web Tokens` is classified as `isOfficial: false` (educational vendor guide).
   - Martin Fowler architectural resources are verified as `isOfficial: false` (authoritative community reference).
   - `isOfficial: true` is strictly reserved for authoritative standards and documentation bodies (MDN Web Docs, Microsoft Learn, PostgreSQL Docs, Python Software Foundation).
2. **Project-Based-Learning Provenance**:
   - All community project references resolve to `practical-tutorials/project-based-learning` at commit `fcd321e0503f8f2e21975e542ff543af6b5e0ef7`.
   - SkillProof-curated real-world projects (`proj-be-order-inventory`, `proj-fe-design-system`, `proj-da-bi-portfolio`) are explicitly catalogued with verifiable deliverables.

---

## D. Security & SSRF Audit

1. **Zero Candidate Code Execution**:
   - Verification of candidate repositories is strictly static.
   - **NO** `npm install`, `npm test`, `dotnet test`, `python` execution, Jupyter notebook execution, `docker build`, or arbitrary shell scripts are ever run against candidate submissions.
2. **Server-Side Request Forgery (SSRF) Defense**:
   - Whitelists only `https` in production.
   - Blocks loopback (`127.0.0.0/8`, `::1`, `localhost`).
   - Blocks private IPv4 blocks (`10.0.0.0/8`, `172.16.0.0/12`, `192.168.0.0/16`).
   - Blocks link-local addresses (`169.254.0.0/16`) and cloud metadata endpoints (`169.254.169.254`).
   - Validates redirect locations against SSRF filters (max 3 redirects).
3. **Secret Protection**:
   - Detects sensitive filenames (`.env`, `credentials.json`, `private.key`, `id_rsa`) and logs security warnings without reading or exposing file contents.
4. **Code Signing Audit**:
   - No certificates (`.pfx`, `.cer`), signing scripts, or Windows AppControl policy workarounds exist in the repository.

---

## E. Deterministic Regression Results

### 1. Backend Solution (`SkillProof.slnx`)
```
dotnet build backend/SkillProof.slnx
dotnet test backend/SkillProof.slnx
```
- **Build**: 0 Warnings, 0 Errors
- **Tests**: **266 Passed, 0 Failed, 0 Skipped (17.2s)**

### 2. Frontend Production Compilation
```
node ./frontend/node_modules/typescript/bin/tsc -p ./frontend/tsconfig.json --noEmit
node ./frontend/node_modules/next/dist/bin/next build ./frontend
```
- **TypeScript**: 0 Errors
- **Next.js Production Build**: 12/12 static and dynamic routes compiled successfully

---

## F. Full Playwright Suite Audit

Total tests run in Playwright test runner: **60 tests**
- **Passed: 23 / 23 modern V2 tests** (100% pass rate on active architecture):
  - `v2_6_project_verification.spec.ts`: 4/4 Passed (Frontend, Backend, Data Analyst, Invalid Evidence)
  - `v2_5_resources_projects.spec.ts`: 3/3 Passed (Roadmap $\rightarrow$ Resources $\rightarrow$ Projects $\rightarrow$ Portfolio)
  - `v2_4_canonical_roadmap.spec.ts`: 3/3 Passed (Roadmap Graph $\rightarrow$ Node Details $\rightarrow$ Navigation)
  - `v2_3_assessment.spec.ts`: 3/3 Passed (Skill Matrix $\rightarrow$ Explanations $\rightarrow$ Calibration)
  - `v2_2_journey.spec.ts`: 10/10 Passed (Session TTL, Route Guards, Role Selection, Fallbacks)
- **Failed: 37 legacy tests**:
  - Pre-V2 tests (`milestone1` through `milestoneI9`) were authored against the deprecated single-page architecture and legacy `Financial Analyst` home-screen entrypoint, superseded by the V2 multi-page journey.

---

## G. Three-Role Golden Journey Verification

| Stage | Frontend Developer | Backend Developer | Data Analyst |
| :--- | :--- | :--- | :--- |
| **1. Role Selection** | Selected from `/roles` card | Selected from `/roles` card | Selected from `/roles` card |
| **2. Skill Selection** | Customizable selection; honest question bank notice for unassessed nodes | Full customized selection + C#/Java/Python language selection | Customizable selection; honest question bank notice for unassessed nodes |
| **3. Diagnostic Interview** | Standard assessment with conceptual & architectural prompts | Calibrated 2-stage adaptive branch with immediate explanation panel | Standard assessment with analytical reasoning prompts |
| **4. Skill Matrix** | Explainable 4-tier matrix; clear distinction between Assessed and Role Coverage gaps | Full 4-tier matrix (`Beginner`, `Intermediate`, `Advanced`, `Insufficient Evidence`) | Neutral gap classification; no "failed" labels |
| **5. Canonical Roadmap** | Visual DAG; Completed / NeedsDevelopment / Locked nodes | Visual DAG derived deterministically from canonical V3 framework | Visual DAG; Deep Learning verified as `Optional` |
| **6. Learning Resources** | Official documentation & community guides | Verified vendor & community learning links | Specialized analytics & SQL documentation |
| **7. Curated Project** | Client Component Platform & Web Performance Audit | Resilient Order & Inventory Service | Executive Revenue Intelligence & Churn Analysis |
| **8. Evidence Submission** | GitHub Repo + Deployed Web App | GitHub Repo + Deployed API / OpenAPI | GitHub Repo + `.ipynb` Notebook + Streamlit Dashboard (No website required) |
| **9. Evidence Verification** | Verified HTML5/WAI-ARIA & component manifest | Verified transactional isolation, composite indexes & OpenAPI | Verified pandas/numpy imports, cell AST & dashboard reachability |
| **10. Portfolio Proof** | Evidence-gated portfolio bullets with claim traceability | Evidence-gated bullets; no overclaiming | Analyst-appropriate portfolio bullets; no deep learning overclaim |
| **11. Re-assessment** | Explicit "Re-assess Skill ↻" CTA | Explicit "Re-assess Skill ↻" CTA | Explicit "Re-assess Skill ↻" CTA |

---

## H. Negative Journey Verification

- **Blank / Incomplete Answers**: Assigned `Insufficient Evidence` with neutral rationale. Never labeled as "failed".
- **Unassessed Competencies**: Marked as `Not Assessed` / `Role Coverage Gap` (recommended validation, not student failure).
- **Invalid / Missing Project Evidence**: Evaluated as `Insufficient Evidence` or `Partially Demonstrated`. The system never outputs derogatory copy ("You failed", "Your project is bad").
- **Portfolio Gate**: When evidence is insufficient, **no strong CV bullets are generated**, upholding the core invariant: **No Evidence $\implies$ No Strong Claim**.

---

## I. Live AI Smoke Test Results

With live AI enabled in Development mode using `OpenAI:ApiKey` (`model: gpt-5.4-mini`):
1. **Runtime Inspection (`GET /api/dev/ai/runtime`)**:
   - `provider`: "OpenAI"
   - `model`: "gpt-5.4-mini"
   - `liveAiAvailable`: true
   - `activeEvaluator`: "OpenAiDiagnosticEvaluator"
2. **Diagnostic Evaluation (`POST /api/dev/ai/evaluate`)**:
   - Prompt evaluated: JWT authentication and revocation in web APIs (`q-be-auth-03`).
   - Duration: 3510 ms.
   - Model response parsed and validated against strict schema: Level = `Intermediate`.
   - Grounded observations extracted directly from candidate text.
   - Public API strictly conceals system prompts and internal rubric criteria.
   - Trace stored in `/dev/ai-inspector`: `trace-4e3b95a45a334ef7a565483efdd33802`.
3. **Deterministic Fallback**:
   - When live AI is disabled, the platform executes with 100% functionality with zero outbound network calls.

---

## J. API Key & Credential Security

- Scanned workspace: **0 matches** for unencrypted keys or `sk-` tokens.
- OpenAI key is stored strictly in `dotnet user-secrets` and runtime environment variables.
- Developer AI Inspector is restricted to Development (`ASPNETCORE_ENVIRONMENT=Development`); returns HTTP 404 in Production.

---

## K. External Dependency Risks

| External Dependency | Risk Level | Mitigation in SkillProof |
| :--- | :--- | :--- |
| **OpenAI API Outage / Quota** | Low | Deterministic evaluator and roadmap resolvers provide 100% offline fallback. |
| **Candidate GitHub API Rate Limiting** | Low | Deterministic fixtures registered for all demo roles; rate limits do not block demos. |
| **External Resource Link Changes** | Low | Stored URLs verified by catalog validator; offline resources remain navigable. |

---

## L. Known Prototype Limitations

1. **Full Question Bank Coverage**: Calibrated adaptive questions currently cover the Backend Developer role (48 questions); Frontend and Data Analyst utilize canonical roadmap nodes, verified learning resources, and curated project evidence.
2. **Session Persistence**: In-memory and browser session storage maintain state for 30 minutes; cross-device user accounts are slated for post-V2 architecture.
3. **Legacy Playwright Tests**: Pre-V2 milestone tests (M1–I9) expect legacy single-page UI elements that have been replaced by the modern multi-page journey.

---

## M. Demo Instructions

### 1. Minimal Startup (Deterministic Demo Mode — Recommended)

**Terminal 1 (Backend API):**
```powershell
dotnet run --project backend/SkillProof.Api/SkillProof.Api.csproj --no-launch-profile --urls "http://localhost:5068"
```

**Terminal 2 (Frontend Web App):**
```powershell
npm run dev --prefix frontend
```
Navigate browser to: **`http://localhost:3000`**

### 2. Live AI Demo Mode (OpenAI-Enhanced)

**Terminal 1 (Backend API):**
```powershell
$env:ASPNETCORE_ENVIRONMENT="Development"
dotnet run --project backend/SkillProof.Api/SkillProof.Api.csproj --no-launch-profile --urls "http://localhost:5068" --OpenAI:LiveEvaluationEnabled=true
```

---

## N. Hackathon Demo Script (3–5 Minutes)

1. **Role Selection (30s)**:
   - Open `http://localhost:3000/roles`. Highlight the 3 primary roles (Frontend, Backend, Data Analyst).
   - Select **Backend Developer**.
2. **Skill Selection (30s)**:
   - On `/assessment/skills`, note that 0 skills are preselected (no unearned assumptions).
   - Select **SQL & Relational Databases** and choose **C#** as primary language.
   - Click **Begin Diagnostic Assessment →**.
3. **Diagnostic Interview & Grounded Feedback (60s)**:
   - Answer Question 1 explaining composite indexing, EXPLAIN ANALYZE, and ACID isolation.
   - Click **Submit Answer →**.
   - Show the **Evidence-Grounded Feedback Panel**: what was covered, what could be stronger, and reference explanation.
   - Advance through question 2 to completion.
4. **Skill Matrix & Visual Roadmap (45s)**:
   - View `/assessment/result` showing the 4-tier qualitative Skill Matrix (`Intermediate` / `Assessed Gap`).
   - Click **Continue to Learning Roadmap →**.
   - Inspect the interactive personalized visual roadmap (`/roadmap`), showing `Completed`, `Current`, `Needs Development`, and `Available` nodes.
5. **Learning Resources & Curated Projects (30s)**:
   - Click the SQL node to show verified vendor documentation (`PostgreSQL Official Documentation`).
   - Click **Continue to Curated Projects →** (`/projects`) to view gap-matched projects.
6. **Project Evidence Submission & Gated Portfolio Proof (45s)**:
   - Select **Production-Grade Resilient Order & Inventory Service**.
   - On `/portfolio`, enter the verified demo fixture:
     - Repo: `https://github.com/skillproof-fixtures/be-order-service`
     - API: `https://api.skillproof.dev/orders`
   - Click **Submit for Verification →**.
   - Show the **Requirement Evidence Matrix** and the **Verified Portfolio Proof** with full claim traceability.
   - Point out the **"Re-assess Skill ↻"** button completing the continuous improvement loop.

---

## Final Acceptance Classification

### **READY FOR DEMO WITH CAVEATS**

**Actual Caveats:**
1. **Calibrated Question Bank Scope**: The 48 calibrated adaptive assessment questions currently cover Backend Developer competencies; Frontend and Data Analyst provide the canonical roadmap, curated resources, and project evidence verification pipeline.
2. **Legacy E2E Test Suite**: Legacy Playwright tests from Milestones 1–I9 target deprecated single-page layouts; all 23 modern V2.2–V2.6 journey tests pass with 100% success.
3. **Session Lifetime**: Diagnostic sessions reside in client-side session storage with a 30-minute time-to-live; multi-device persistent accounts are reserved for post-V2 roadmaps.
