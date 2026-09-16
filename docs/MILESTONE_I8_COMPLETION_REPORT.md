# Milestone I8 Completion Report: Personalized Gap-Based Learning Roadmap

**Milestone Status**: COMPLETED & VERIFIED  
**Date**: September 16, 2026  
**Execution Mode**: FAST EXECUTION MODE (Autonomous)

---

## 1. Executive Summary

Milestone I8 successfully transforms the Milestone I7 `RoadmapHandoffContract` from the completed adaptive diagnostic session into a personalized, evidence-grounded **Learning and Practice Roadmap** without altering assessed skill levels, inventing fake gaps, or generating generic LMS courses.

### Core Principles Delivered
1. **Evidence-Grounded Philosophy**: The roadmap adheres strictly to:
   $$\text{Gap} \longrightarrow \text{Learning Objective} \longrightarrow \text{Focused Learning} \longrightarrow \text{Practice Task} \longrightarrow \text{Evidence Target}$$
2. **Strict Gap Categorization**:
   - **Category A (Development Gaps)**: The candidate demonstrated foundational or applied competency, but higher-level reasoning (e.g., query planning, concurrency isolation, distributed caching) was not yet demonstrated. The roadmap focuses on deeper architectural and operational capabilities.
   - **Category B (Evidence Gaps)**: Diagnostic signals were insufficient (`Insufficient Evidence`). The roadmap produces an **evidence-building activity** to demonstrate current capability. It is **never** labeled as "Weak", "Beginner", "Failed", or "0%".
3. **Deterministic Prioritization & Count Constraints**:
   - Order: Group 1 (Assessed Development Gaps: Beginner/Intermediate) $\to$ Group 2 (Evidence-Building Gaps: Insufficient Evidence) $\to$ Group 3 (Next-Level Development: Advanced).
   - Deterministic skill tie-breaking preserves stable, explainable ordering.
   - Constrained to **3–5 focused items maximum**. If fewer gaps exist, returns fewer items. **Never** injects fake or unassessed skills to inflate counts.
4. **Actionable & Observable Practice Targets**:
   - Every roadmap item contains an actionable `practiceTask`, observable `evidenceTarget` (e.g., ADR, benchmark analysis, query plan diagnosis, test suite), and bulleted `completionCriteria`.
5. **Trusted Server-Side Handoff**:
   - Resolves `RoadmapHandoffContract` server-side from the completed adaptive session via `POST /api/diagnostics/adaptive/sessions/{sessionId}/roadmap` or `sessionId` payload in `POST /api/roadmaps/generate`. Clients cannot inject arbitrary fake gaps or override skill levels.
6. **Strict Structured AI Output & Robust Fallback**:
   - OpenAI schema validation validates skill IDs against the handoff, checks levels, and enforces contract constraints.
   - If AI evaluation is disabled (`LiveEvaluationEnabled = false`), missing credentials, or fails schema validation, the deterministic generator (`DeterministicRoadmapGenerator`) produces high-quality, catalog-grounded roadmap items.
7. **Milestone I9 Handoff Prepared**:
   - Each roadmap response includes a structured `projectContext` (`ProjectGapContext`) containing role ID and target skills ready for downstream project recommendation in Milestone I9.
8. **Developer AI Inspector Integration**:
   - Extended Developer AI Inspector (`/dev/ai-inspector`) Section 4 to inspect the roadmap pipeline (input handoff, AI response vs fallback, validation status, final items, and I9 handoff) with zero API keys or prompt leaks.
9. **Zero Regression OpenAI Requests**:
   - All regression suites run with `LiveEvaluationEnabled = false`, incurring exactly **0** external OpenAI requests.

---

## 2. Architecture & Contracts

### 2.1 Backend Models (`backend/SkillProof.Api/Models/RoadmapModels.cs`)

```csharp
public record GenerateRoadmapRequest
{
    public string RoleId { get; init; } = string.Empty;
    public string? SessionId { get; init; }
    public List<string> FocusAreas { get; init; } = new();
    public List<SkillGapInput>? SkillGaps { get; init; }
}

public record RoadmapResponse
{
    public string RoleId { get; init; } = string.Empty;
    public string GeneratedAt { get; init; } = DateTime.UtcNow.ToString("o");
    public string? SourceSessionId { get; init; }
    public List<RoadmapItemDto> Items { get; init; } = new();
    public ProjectGapContext? ProjectContext { get; init; }
    public DevRoadmapInspectionDto? DevInspection { get; init; }
}

public record RoadmapItemDto
{
    public string? SkillId { get; init; }
    public string? CurrentLevel { get; init; }
    public string? GapType { get; init; } // "development" | "evidence"
    public string? TargetArea { get; init; }
    public string? WhyThisMatters { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public List<string>? LearningActions { get; init; }
    public string? PracticeTask { get; init; }
    public string? EvidenceTarget { get; init; }
    public List<string>? CompletionCriteria { get; init; }
    public string Priority { get; init; } = "Medium";
    public int EstimatedHours { get; init; } = 4;
}

public record ProjectGapContext
{
    public string RoleId { get; init; } = string.Empty;
    public List<ProjectGapTargetSkill> TargetSkills { get; init; } = new();
}

public record ProjectGapTargetSkill
{
    public string SkillId { get; init; } = string.Empty;
    public string CurrentLevel { get; init; } = string.Empty;
    public string TargetArea { get; init; } = string.Empty;
    public string PracticeTask { get; init; } = string.Empty;
    public string EvidenceTarget { get; init; } = string.Empty;
}
```

### 2.2 Generators
- **`DeterministicRoadmapGenerator`**: Built-in deterministic engineering guidance for all 16 assessable skills (6 Core, 2 Recommended, 8 Languages) across Development and Evidence gap categories.
- **`OpenAiRoadmapGenerator`**: Structured JSON Schema generation with strict validation against the trusted handoff and automatic deterministic fallback.

---

## 3. Verification & Regression Results

| Test Suite | Target | Result | Duration |
| :--- | :--- | :--- | :--- |
| **Dataset Validator** | 48 Questions, 18/18 Frozen v1.2 Core Baseline Fingerprints | **PASS** (18/18 match) | 8s |
| **Backend Unit & Integration** | `dotnet test -c Release backend/SkillProof.slnx` | **183 / 183 PASS** (14 new I8 tests) | 13s |
| **Frontend Production Build** | `npm run build` (Next.js 16.3.5 Turbopack) | **PASS** (0 errors) | 17s |
| **Playwright E2E Regression** | `npx playwright test --workers=1` (37 specs) | **37 / 37 PASS** (2 new I8 specs) | 42.6s |
| **Regression OpenAI Calls** | External HTTP requests during test runs | **0** | - |

### E2E Test Coverage Breakdown
- `milestone1.spec.ts`: 2 passed (Role selection & Diagnostic flow)
- `milestone2.spec.ts`: 2 passed (Evaluation, Profile, Insufficient Evidence legacy)
- `milestone4.spec.ts`: 1 passed (Learning Roadmap flow)
- `milestone5.spec.ts`: 2 passed (Gap-backward project recommendation)
- `milestoneI2.spec.ts`: 6 passed (Dynamic skill selection UI)
- `milestoneI3.spec.ts`: 5 passed (Dynamic question selection from SQLite)
- `milestoneI4.spec.ts`: 6 passed (Data Foundation v2 SQLite Dynamic Assessment)
- `milestoneI5.spec.ts`: 4 passed (Developer AI Inspector & Observability)
- `milestoneI6.spec.ts`: 4 passed (Adaptive Diagnostic E2E branching & privacy)
- `milestoneI7.spec.ts`: 2 passed (Career Readiness Profile & Insufficient Evidence neutrality)
- `milestoneI8.spec.ts`: 2 passed (Personalized Gap-Based Learning Roadmap & Evidence-building activity)
- `smoke.spec.ts`: 1 passed (Homepage sanity check)

---

## 4. Exit Criteria Verification

- [x] **1. I7 profile drives roadmap**: Generated directly from `RoadmapHandoffContract` of the adaptive session.
- [x] **2. Trusted server-side handoff used**: Client specifies `sessionId`; backend resolves handoff from session profile.
- [x] **3. Development Gap handled correctly**: Creates advanced practice task targeting next-level mastery.
- [x] **4. Evidence Gap handled correctly**: Generates neutral evidence-building activity.
- [x] **5. IE never silently becomes Beginner**: Insufficient Evidence produces an evidence-gathering activity, never remedial beginner lessons.
- [x] **6. Roadmap priority deterministic**: Assessed development gaps $\to$ evidence-building gaps $\to$ next-level areas with deterministic skill tie-breaking.
- [x] **7. Roadmap contains only assessed/profile skills**: Rejects or ignores unassessed/unknown skills.
- [x] **8. 3–5 focused items maximum**: Enforced at generator and schema validation layer.
- [x] **9. No fake gaps added to meet item count**: If fewer than 3 gaps exist, returns fewer items without padding.
- [x] **10. Every item has learning objective**: Structured title and clear target learning goal.
- [x] **11. Every item has practical task**: Concrete, realistic engineering scenario.
- [x] **12. Every item has evidence target**: Observable output (ADR, test suite, benchmark, PR).
- [x] **13. Every item has completion criteria**: Specific bullet points.
- [x] **14. Every item explains why it was recommended**: Grounded in candidate's observed assessment evidence.
- [x] **15. AI output structurally validated**: Validated against input handoff contract.
- [x] **16. Invalid AI output cannot corrupt roadmap**: Safely falls back to deterministic generator.
- [x] **17. Deterministic fallback works**: Full deterministic guidance for all 16 assessable skills.
- [x] **18. No numeric readiness score**: Zero percentages or composite scores.
- [x] **19. No rubric exposed**: Assessment rubrics kept confidential on server.
- [x] **20. No expectedSignals exposed**: Expected signals never sent to roadmap UI.
- [x] **21. No prompts exposed**: Prompt internals not leaked to candidate.
- [x] **22. I9 handoff prepared**: `ProjectGapContext` structured on roadmap response.
- [x] **23. Inspector shows roadmap pipeline**: Section 4 in `/dev/ai-inspector` exposes handoff, provider, validation, and final items.
- [x] **24. Financial Analyst unchanged**: Legacy flow preserved.
- [x] **25. Dataset unchanged**: 48 questions validated.
- [x] **26. Frozen baseline unchanged**: 18/18 Core questions SHA-256 fingerprint verified.
- [x] **27. Backend regression PASS**: 183/183 unit/integration tests pass.
- [x] **28. Frontend build PASS**: Next.js production build succeeds with 0 errors.
- [x] **29. Playwright PASS**: 37/37 E2E tests pass.
- [x] **30. Zero regression OpenAI requests**: 0 external API calls during regression.
