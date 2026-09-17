# SkillProof V2.4 — Personalized Visual Roadmap Implementation Plan

## Objective & Core Principle

Transform the roadmap experience from generated linear cards into a **personalized canonical visual roadmap** derived deterministically from:
1. **Data V3 Canonical Framework**: Persisted canonical learning topology (`data/v3/role-roadmap-nodes.json`, `data/v3/roadmap-relationships.json`, `data/v3/canonical-skills.json`).
2. **Assessment V2 Skill Matrix**: Trusted server-side competency evidence (`SkillMatrixItemDto`, `OverallStatus`, `GapType`).
3. **Deterministic Graph Resolution Rules**: Graph traversal resolving prerequisite satisfaction, node states, and priority learning targets without AI hallucination.

**Core Invariants:**
- **Zero OpenAI dependency:** AI does **not** invent roadmap nodes, prerequisites, or ordering. Resolution runs with 0 OpenAI calls.
- **Zero numeric percentages:** Competency states remain qualitative (`Completed`, `Current`, `Available`, `Locked`, `NeedsDevelopment`, `NotAssessed`, `Optional`).
- **Not Assessed != Beginner:** Unassessed skills represent learning/validation recommendations, not deficient scores.
- **Toolkit isolation:** Toolkits (Git, Docker, etc.) do not alter readiness levels or generate assessed gaps.
- **Non-mandatory bounds preserved:** Data Analyst Deep Learning and Frontend Meta-Frameworks remain optional/elective.
- **Scope limitation:** Curated projects and external course links are deferred to V2.5.

---

## User Review Required

> [!IMPORTANT]
> **Deterministic Node State Policy:**
> - `Completed`: Candidate assessed as `Advanced`, or `Intermediate` with `GapType == "NONE"`.
> - `NeedsDevelopment`: Candidate assessed as `Beginner`, `Insufficient Evidence`, or `Intermediate` with `GapType == "ASSESSED GAP"`.
> - `NotAssessed`: Role-relevant competency not assessed during interview (`RoleCoverageGap`).
> - `Optional`: Competency flagged as `isOptional: true` (e.g. Deep Learning in Data Analyst).
> - `Locked`: Any node with an unresolved incoming `prerequisite` relationship.
> - `Current`: The single highest-priority actionable node (or top parallel node) whose prerequisites are satisfied and requires development or validation.
> - `Available`: Relevant competency whose prerequisites are satisfied but is not currently the top priority.

> [!NOTE]
> **Legacy Roadmap Isolation:**
> `OpenAiRoadmapGenerator` and `DeterministicRoadmapGenerator` remain untouched for backward compatibility with existing tests and legacy endpoints. The V2 roadmap flow exclusively invokes `CanonicalRoadmapResolver`.

---

## Proposed Changes

### Component 1: Backend Canonical Roadmap Resolution Engine

#### [NEW] [`backend/SkillProof.Api/Models/CanonicalRoadmapModels.cs`](file:///d:/Roy/SkillProof-Support-AI/backend/SkillProof.Api/Models/CanonicalRoadmapModels.cs)
- Define `RoadmapNodeState` enum / strings: `Completed`, `Current`, `Available`, `Locked`, `NeedsDevelopment`, `NotAssessed`, `Optional`.
- Define DTOs:
  - `PersonalizedRoadmapNodeDto`: `CanonicalSkillId`, `Name`, `Classification`, `Requirement`, `NodeState`, `GapType`, `AssessmentState`, `WhyThisNode`, `NextAction`, `DisplayOrder`, `IsToolkit`, `IsOptional`.
  - `PersonalizedRoadmapEdgeDto`: `From`, `To`, `RelationshipType`, `Rationale`.
  - `RoadmapGraphSummaryDto`: `CurrentNodeIds`, `CompletedCount`, `NeedsDevelopmentCount`, `NotAssessedCount`, `AvailableCount`, `LockedCount`, `OptionalCount`, `TotalNodes`.
  - `PersonalizedRoadmapGraphDto`: `RoleId`, `RoleTitle`, `SessionId`, `Nodes`, `Edges`, `Summary`.

#### [NEW] [`backend/SkillProof.Api/Services/ICanonicalRoadmapResolver.cs`](file:///d:/Roy/SkillProof-Support-AI/backend/SkillProof.Api/Services/ICanonicalRoadmapResolver.cs)
- Interface declaring:
  - `Task<PersonalizedRoadmapGraphDto> ResolveRoadmapAsync(string sessionId, CancellationToken cancellationToken = default);`
  - `Task<PersonalizedRoadmapGraphDto> ResolveRoadmapForProfileAsync(string roleId, CareerReadinessProfile profile, string? sessionId = null, CancellationToken cancellationToken = default);`

#### [NEW] [`backend/SkillProof.Api/Services/CanonicalRoadmapResolver.cs`](file:///d:/Roy/SkillProof-Support-AI/backend/SkillProof.Api/Services/CanonicalRoadmapResolver.cs)
- Implementation:
  1. Loads role's canonical nodes (`RoleRoadmapNodes`) and verified relationships (`RoadmapRelationships`) from SQLite.
  2. Resolves assessment evidence from `CareerReadinessProfile.SkillMatrix` (or `SkillProfiles`).
  3. Evaluates incoming prerequisite edges:
     - If prerequisite source node is not `Completed`, dependent node is marked `Locked`.
  4. Applies qualitative node states (`Completed`, `NeedsDevelopment`, `NotAssessed`, `Optional`).
  5. Deterministic priority ranking:
     - Priority 1: Unresolved mandatory fundamental with satisfied prerequisites.
     - Priority 2: Assessed core gap (`NeedsDevelopment` + `ASSESSED GAP`).
     - Priority 3: Evidence gap (`NeedsDevelopment` + `EVIDENCE GAP`).
     - Priority 4: Role coverage gap (`NotAssessed` core competency).
     - Priority 5: Recommended / toolkit competency.
     - Priority 6: Optional competency.
     - Tie-breaker: canonical `displayOrder`.
  6. Top priority actionable node designated `Current`.
  7. Generates evidence-grounded `WhyThisNode` and concise `NextAction` (e.g. "Review concept", "Practice skill", "Validate knowledge", "Satisfy prerequisite").
  8. Computes counts summary without numeric readiness percentages.

#### [MODIFY] [`backend/SkillProof.Api/Program.cs`](file:///d:/Roy/SkillProof-Support-AI/backend/SkillProof.Api/Program.cs)
- Register `ICanonicalRoadmapResolver` as scoped service.
- Map `GET /api/diagnostics/adaptive/sessions/{sessionId}/canonical-roadmap`.

---

### Component 2: Frontend Visual Roadmap & Accessible Detail

#### [MODIFY] [`frontend/app/types.ts`](file:///d:/Roy/SkillProof-Support-AI/frontend/app/types.ts)
- Add TypeScript interfaces:
  - `RoadmapNodeState`: `'Completed' | 'Current' | 'Available' | 'Locked' | 'NeedsDevelopment' | 'NotAssessed' | 'Optional'`
  - `PersonalizedRoadmapNode`
  - `PersonalizedRoadmapEdge`
  - `RoadmapGraphSummary`
  - `PersonalizedRoadmapGraph`

#### [MODIFY] [`frontend/app/api.ts`](file:///d:/Roy/SkillProof-Support-AI/frontend/app/api.ts)
- Add `getCanonicalRoadmap(sessionId: string): Promise<PersonalizedRoadmapGraphDto>`.

#### [MODIFY] [`frontend/app/lib/journey-state/types.ts`](file:///d:/Roy/SkillProof-Support-AI/frontend/app/lib/journey-state/types.ts) & session repo
- Add `canonicalRoadmap?: PersonalizedRoadmapGraph | null` and `setCanonicalRoadmap` to journey state for fast navigation.

#### [MODIFY] [`frontend/app/roadmap/page.tsx`](file:///d:/Roy/SkillProof-Support-AI/frontend/app/roadmap/page.tsx)
- Redesign `/roadmap` into a clear visual learning graph inspired by the clarity of roadmap.sh using SkillProof aesthetics:
  - Vertical / gently branching learning path with connected nodes.
  - High-visibility state styling:
    - **Current**: Cyan pulsing beacon, glowing card border, "Current Priority" tag.
    - **Completed**: Emerald badges with checkmarks.
    - **Needs Development**: Amber alert indicators highlighting assessed gaps.
    - **Available**: Sky blue indicators showing unlocked next milestones.
    - **Locked**: Muted slate cards with lock icons showing prerequisite dependencies.
    - **Not Assessed**: Dashed neutral borders indicating unassessed role competencies.
    - **Optional**: Violet badge indicating elective / non-mandatory areas.
  - Summary metric bar: Nodes count, completed, needs development, unassessed (0% numeric scores).
  - Filter tabs: All Nodes, Actionable Path, Gaps Only.
  - Clicking any node navigates to `/roadmap/[nodeId]`.

#### [NEW] [`frontend/app/roadmap/[nodeId]/page.tsx`](file:///d:/Roy/SkillProof-Support-AI/frontend/app/roadmap/[nodeId]/page.tsx)
- Dedicated accessible node detail page:
  - Node name & classification (Mandatory / Recommended / Optional / Toolkit).
  - Roadmap state & assessment status.
  - Gap type analysis.
  - "Why this node is placed here" (evidence-grounded explanation).
  - Prerequisites list (with completion status).
  - Next recommended action.
  - "← Back to Roadmap" button preserving session and scroll state without resetting journey.

---

### Component 3: Backend & Frontend Automated Tests

#### [NEW] [`backend/SkillProof.Api.Tests/MilestoneV24CanonicalRoadmapTests.cs`](file:///d:/Roy/SkillProof-Support-AI/backend/SkillProof.Api.Tests/MilestoneV24CanonicalRoadmapTests.cs)
- Unit & integration tests:
  - Assessed Advanced -> `Completed`.
  - Assessed Intermediate with gap -> `NeedsDevelopment`; without gap -> `Completed`.
  - Assessed Beginner & Insufficient Evidence -> `NeedsDevelopment`.
  - Unassessed skill -> `NotAssessed`.
  - Prerequisite satisfied -> `Available` / `Current`.
  - Prerequisite unsatisfied -> `Locked`.
  - Optional skill behavior (Data Analyst Deep Learning remains optional).
  - Toolkit behavior (Git/Docker does not affect readiness).
  - Priority ranking determinism (Mandatory unresolved -> Assessed gap -> Evidence gap -> Role coverage gap).
  - Security / Trust verification: client cannot inject node states or override assessment levels; server authoritative resolution.
  - API endpoint verification: `GET /api/diagnostics/adaptive/sessions/{sessionId}/canonical-roadmap` requires completed session, returns role nodes, zero OpenAI calls.

#### [NEW] [`tests/e2e/v2_4_canonical_roadmap.spec.ts`](file:///d:/Roy/SkillProof-Support-AI/tests/e2e/v2_4_canonical_roadmap.spec.ts)
- Playwright E2E testing:
  - Assessment Result -> Continue to Roadmap -> Graph renders.
  - All node states visible and labeled.
  - Click node -> navigates to `/roadmap/[nodeId]`.
  - Node detail renders grounded explanation and prerequisites.
  - Back to roadmap button preserves session.
  - Multi-role verification: Frontend Developer, Backend Developer, Data Analyst.

---

## Verification Plan

### Automated Tests
1. Catalog & Question Bank Validators:
   ```bash
   node harness/validators/validate-v3-catalog.mjs
   node harness/validators/validate-v2.1-proposal.mjs
   node harness/validators/validate-backend-assessment-data.mjs
   node harness/validators/validate-v3-question-bank.mjs
   ```
2. .NET Solution Build & Tests:
   ```bash
   dotnet build backend/SkillProof.slnx
   dotnet test backend/SkillProof.slnx
   ```
3. Frontend Tests & Build:
   ```bash
   npx tsx --test frontend/__tests__/journey-state.test.ts
   npm --prefix frontend run build
   ```
4. Playwright End-to-End Suite:
   ```bash
   npx playwright test tests/e2e/v2_4_canonical_roadmap.spec.ts
   ```

### Manual Verification
- Visual inspection via browser subagent of `/roadmap` and `/roadmap/[nodeId]` on both desktop and mobile viewports.
- Confirm zero OpenAI API calls during roadmap resolution.
