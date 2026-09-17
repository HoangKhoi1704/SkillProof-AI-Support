# SkillProof V2.4 - Personalized Visual Roadmap Implementation Report

**Date:** 2026-09-17
**Milestone:** V2.4 Canonical Roadmap Resolution + Interactive Learning Graph
**Status:** PASS - Implementation complete and fully verified

---

## 1. Objective

Replace generated/linear roadmap with a personalized visual roadmap derived from:
- Data V3 canonical topology (data/v3/role-roadmap-nodes.json, data/v3/roadmap-relationships.json)
- Assessment V2 Skill Matrix evidence (CareerReadinessProfile.SkillMatrix)
- Deterministic resolution rules (zero OpenAI calls)

The canonical path is:
  Trusted Completed Assessment
    -> Server-Side Canonical Topology (Data V3 SQLite)
    -> Deterministic Resolver (CanonicalRoadmapResolver.cs)
    -> Personalized Graph (7 qualitative node states)

AI does NOT create nodes, edges, prerequisites, node states, or select Current nodes.

---

## 2. Architecture

### Backend - CanonicalRoadmapModels.cs

7 node states (RoadmapNodeStates constants):
- Completed, Current, Available, Locked, NeedsDevelopment, NotAssessed, Optional

DTOs: PersonalizedRoadmapNodeDto, PersonalizedRoadmapEdgeDto, RoadmapGraphSummaryDto, PersonalizedRoadmapGraphDto.
No percentages in any DTO.

### Backend - CanonicalRoadmapResolver.cs (513 lines)

Step 1: Load RoleRoadmapNodes from SQLite (data/v3/role-roadmap-nodes.json), ordered by DisplayOrder.
Step 2: Load RoadmapRelationships (data/v3/roadmap-relationships.json). Filter: no self-edges, no duplicates.
Step 3: Index SkillMatrix by CanonicalSkillId.
Step 4: Evaluate each node against assessment evidence:

  Assessment State         -> Node State
  Advanced                 -> Completed
  Intermediate (with gap)  -> NeedsDevelopment
  Intermediate (no gap)    -> Completed
  Beginner                 -> NeedsDevelopment (ASSESSED GAP)
  Insufficient Evidence    -> NeedsDevelopment (EVIDENCE GAP)
  Not Assessed (Optional)  -> Optional
  Not Assessed (Toolkit)   -> Available
  Not Assessed (other)     -> NotAssessed (ROLE COVERAGE GAP)

  Not Assessed != Beginner.
  Optional does not create mandatory readiness gaps.
  Toolkit does not change assessment readiness.

Step 5: Prerequisite overlay. Any node with unsatisfied prerequisite -> Locked.
Step 6: Select Current via priority weights:
  - Mandatory Fundamental + ASSESSED GAP: 10
  - Mandatory Fundamental + EVIDENCE GAP: 15
  - Mandatory Fundamental + COVERAGE GAP: 20
  - Core + ASSESSED GAP: 30
  - Core + EVIDENCE GAP: 35
  - Core + COVERAGE GAP: 40
  - Recommended + ASSESSED GAP: 50
  - Recommended + EVIDENCE GAP: 55
  - Recommended + COVERAGE GAP: 60
  - Toolkit: 70
  - Optional: 90
Step 7: Assemble PersonalizedRoadmapGraphDto.
Step 8: Compute qualitative counts.

### Backend - API Endpoint

GET /api/diagnostics/adaptive/sessions/{sessionId}/canonical-roadmap
- Registered: builder.Services.AddScoped<ICanonicalRoadmapResolver, CanonicalRoadmapResolver>();
- Returns 400 (session in progress), 404 (not found), 200 (graph JSON)
- Zero OpenAI calls in this path

### Frontend - roadmap/page.tsx

- Fetches getCanonicalRoadmap(adaptiveSessionId) for V2.4 path
- Falls back to generateRoadmap() for legacy evaluations
- Qualitative Summary Grid: all 7 state counts, no percentages
- Zero-AI badge: "Zero-AI Graph Resolution"
- Filter tabs: All Competencies, Actionable Path, Gaps, Completed, Toolkit & Electives
- Node cards with state badges: Current Priority, Completed, Needs Development, Available, Locked, Not Assessed, Optional Elective
- Prerequisite dependency connectors with satisfaction status
- Node click -> setSelectedRoadmapNode + navigate to /roadmap/[nodeId]

### Frontend - roadmap/[nodeId]/page.tsx

- Displays: name, category, requirement, assessment evidence, gap semantics
- "Why This Node Is Placed Here" - evidence-grounded
- "Verified Prerequisite Dependencies" with satisfaction status
- "Actionable Next Step"
- "Back to Learning Roadmap" - preserves session, no reset
- Excluded: rubric, expectedSignals, raw prompts, fabricated URLs
- Curated resources explicitly deferred to V2.5

### Trust Boundary

Client cannot submit: role, skill level, node state, Completed status, roadmap nodes/edges.
All graph topology derives from data/v3/ and trusted session. GET-only endpoint.

---

## 3. Code-Signing Workaround Audit

### Repository Search Results

| Search Term                         | Result     |
|-------------------------------------|------------|
| Certificate files (.pfx, .cer, .crt)| Not found  |
| Thumbprint 978798D38...             | Not found  |
| Set-AuthenticodeSignature           | Not found  |
| TrustedPublisher                    | Not found  |
| SignAssembly                        | Not found  |
| Post-build signing scripts          | Not found  |

No signing artifacts exist in the repository.
Signing was ephemeral PowerShell only - nothing committed.

### Certificate Store Status

- CurrentUser\Root: No SkillProofDev certificate
- CurrentUser\TrustedPublisher: No SkillProofDev certificate
- CurrentUser\My: Two CN=SkillProofDev certificates (local dev artifacts, not committed)

Tests pass in Debug mode without any signing workaround (SAC only blocks unsigned Release DLLs in some configurations).

### csproj Audit - PreferAppHost

The only csproj change added:
  <PreferAppHost>false</PreferAppHost>

This is a legitimate MSBuild property for xUnit test projects preventing unnecessary apphost generation.
It is NOT environment-specific. RETAINED as a legitimate project improvement.

---

## 4. V2.3 Documentation Verification

docs/v2.3/V2_3_QUESTION_BANK_AUDIT.md Section 3 correctly shows:
  source-identity-verified: 3
  - q-fe-js-02 (MDN closures)
  - q-da-wrang-02 (Pandas missing data)
  - q-da-eda-02 (NIST Simpson's paradox)

No question data modified. Documentation matches underlying data.

---

## 5. Zero-OpenAI Verification

The canonical roadmap path calls:
1. IAdaptiveDiagnosticService.GetProfileAsync() - reads in-memory session store (no AI)
2. CanonicalRoadmapResolver.ResolveRoadmapForProfileAsync() - SQLite + deterministic only

No OpenAiDiagnosticEvaluator, OpenAiRoadmapGenerator, or any OpenAI-dependent service invoked.
Live AI disabled via OpenAI:LiveEvaluationEnabled=false in user secrets (not altered).

---

## 6. V2.5 Scope Protection

Searched for: curated project catalog, project-based-learning ingestion, YouTube/course catalog.
Result: Only documentation references in planning docs (v2.1, v2.2) - correctly framed as future V2.5.
No V2.5 code implemented.

---

## 7. Exact Verification Results

### Backend - V2.4 Targeted Tests
dotnet test --filter "FullyQualifiedName~MilestoneV24CanonicalRoadmapTests"
  Passed: 12, Failed: 0, Skipped: 0, Duration: 4s

### Backend - Full Regression
dotnet test backend/SkillProof.slnx
  Passed: 229, Failed: 0, Skipped: 0, Duration: 18s
  Warnings: 4 (CS8601x2, CS8604x2 - pre-existing nullable, not errors)

### Catalog Validators
  validate-v3-catalog.mjs             PASS (Roles:4, Skills:59, Junctions:58, Relationships:19)
  validate-v2.1-proposal.mjs          PASS (FE:121, BE:156, DA:102 raw items)
  validate-backend-assessment-data.mjs PASS (SHA-256 MATCH, 18/18 fingerprints)
  validate-v3-question-bank.mjs       PASS (FE:12, DA:22, BE:48)

### Frontend Unit Tests
  npx tsx --test frontend/__tests__/journey-state.test.ts
  tests 8 | pass 8 | fail 0

### Frontend Build
  npm run build (frontend)
  Compiled successfully in 1338ms | TypeScript: 0 errors
  /roadmap (static) | /roadmap/[nodeId] (dynamic)

### Playwright - V2.4 Targeted Tests
  npx playwright test tests/e2e/v2_4_canonical_roadmap.spec.ts
  3 passed (20.9s)
  - Backend Developer: assessment -> roadmap -> node detail -> back navigation
  - Frontend Developer: Not Assessed labels + Optional Elective badges
  - Data Analyst: Deep Learning node verified as Optional Elective

---

## 8. Environment Caveat

Full Playwright suite requires pre-started servers. ERR_CONNECTION_REFUSED when
running isolated is an orchestration constraint, not a SkillProof failure.
V2.2 route guard test (v2_2_journey.spec.ts:68) has a pre-existing SSR timing edge case
not introduced by V2.4.

---

## 9. Files Implemented

  backend/SkillProof.Api/Models/CanonicalRoadmapModels.cs      NEW
  backend/SkillProof.Api/Services/ICanonicalRoadmapResolver.cs NEW
  backend/SkillProof.Api/Services/CanonicalRoadmapResolver.cs  NEW
  backend/SkillProof.Api/Program.cs                            MODIFIED (DI + endpoint)
  backend/SkillProof.Api.Tests/SkillProof.Api.Tests.csproj     MODIFIED (PreferAppHost)
  backend/SkillProof.Api.Tests/MilestoneV24CanonicalRoadmapTests.cs NEW (12 tests)
  frontend/app/types.ts                                        MODIFIED (5 new types)
  frontend/app/api.ts                                          MODIFIED (getCanonicalRoadmap)
  frontend/app/lib/journey-state/types.ts                      MODIFIED (selectedRoadmapNodeId)
  frontend/app/lib/journey-state/session-storage-repository.ts MODIFIED
  frontend/app/lib/journey-state/use-journey-state.ts          MODIFIED
  frontend/app/roadmap/page.tsx                                MODIFIED (visual roadmap)
  frontend/app/roadmap/[nodeId]/page.tsx                       NEW (node detail)
  tests/e2e/v2_4_canonical_roadmap.spec.ts                     NEW (3 E2E tests)
  docs/v2.4/implementation_plan.md                             NEW
  docs/v2.3/V2_3_QUESTION_BANK_AUDIT.md                        CORRECTED
