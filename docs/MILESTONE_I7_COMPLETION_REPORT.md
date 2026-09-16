# Milestone I7 Completion Report: Evidence-Based Skill Profile & Gap Analysis

**Milestone Status**: COMPLETED & VERIFIED  
**Date**: September 16, 2026  
**Execution Mode**: FAST EXECUTION MODE (Autonomous)

---

## 1. Executive Summary

Milestone I7 successfully upgrades the SkillProof adaptive diagnostic pipeline to produce an explainable, evidence-based **Career Readiness Profile** and **Skill Gap Analysis** from completed adaptive sessions without redundant AI evaluations or arbitrary numeric scoring.

### Core Principles Delivered
1. **Evidence-Grounded Semantics**: Strengths and findings derive strictly from observed candidate answers and evaluator reasoning without hyperbole or unsupported claims.
2. **Strict Gap Categorization**: Clearly distinguishes **Category A (Development Gaps)** (demonstrated foundation/applied evidence, but lacking advanced query planning or concurrency reasoning) from **Category B (Evidence Gaps)** (insufficient diagnostic evidence collected).
3. **Neutral Insufficient Evidence**: "Insufficient Evidence" is never labeled as "Weak", "Failed", "Beginner", or "0%". It explicitly informs the candidate that the diagnostic lacked sufficient signals.
4. **Deterministic Next Development Areas**: Grounded directly in approved SQLite catalog subskills (`SkillSubskills` mapped to the assessed skill).
5. **No Synthetic Scores or Percentages**: Employs a neutral, count-based role readiness summary (e.g., `3 Intermediate, 2 Beginner, 1 Advanced, 1 Insufficient Evidence`) with zero arbitrary percentages or composite job-ready scores.
6. **Strict Server-Side Privacy**: Public profiles and UI expose only candidate-facing reasoning and assessed questions while keeping evaluator rubrics, `expectedSignals`, and system prompts confidential.
7. **Milestone I8 Roadmap Handoff Contract**: Implements `RoadmapHandoffContract` defining clean inputs (`RoleId`, `SkillGaps` with current level, development areas, and evidence gaps) for the upcoming I8 Roadmap generator.
8. **Observability**: Developer AI Inspector extended to inspect adaptive sessions with a clean delineation between AI evaluation evidence and backend-derived profile data.

---

## 2. Architecture & Data Contracts

### 2.1 Backend Models (`backend/SkillProof.Api/Models/AdaptiveDiagnosticModels.cs`)

```csharp
public record CareerReadinessProfile
{
    public string RoleId { get; init; } = string.Empty;
    public string AssessmentType { get; init; } = "adaptive";
    public DateTime CompletedAt { get; init; } = DateTime.UtcNow;
    public ProfileSummaryDto Summary { get; init; } = new();
    public List<SkillProfileItemDto> Skills { get; init; } = new();
    public RoadmapHandoffContract RoadmapInput { get; init; } = new();
}

public record ProfileSummaryDto
{
    public string RoleId { get; init; } = string.Empty;
    public string RoleTitle { get; init; } = string.Empty;
    public int TotalAssessedSkills { get; init; }
    public int AdvancedCount { get; init; }
    public int IntermediateCount { get; init; }
    public int BeginnerCount { get; init; }
    public int InsufficientEvidenceCount { get; init; }
}

public record SkillProfileItemDto
{
    public string SkillId { get; init; } = string.Empty;
    public string SkillName { get; init; } = string.Empty;
    public string FinalLevel { get; init; } = string.Empty;
    public List<string> Evidence { get; init; } = new();
    public List<string> Reasoning { get; init; } = new();
    public List<string> DemonstratedStrengths { get; init; } = new();
    public List<string> EvidenceGaps { get; init; } = new();
    public List<string> NextDevelopmentAreas { get; init; } = new();
    public List<string> QuestionsAnswered { get; init; } = new();
}

public record RoadmapHandoffContract
{
    public string RoleId { get; init; } = string.Empty;
    public List<RoadmapSkillGapInput> SkillGaps { get; init; } = new();
}
```

### 2.2 Profile Builder (`AdaptiveProfileBuilder.cs`)
- Deterministic, zero-LLM derivation engine.
- Extracts demonstrated strengths from deduplicated evaluator evidence.
- Flags development gaps for assessed levels below `Advanced`.
- Flags neutral evidence gaps for `Insufficient Evidence`.
- Fetches subskills from SQLite `SkillProofCatalogDbContext` for the assessed skill without inventing arbitrary topics.
- Assembles the normalized `RoadmapHandoffContract`.

### 2.3 Endpoints
- `GET /api/diagnostics/adaptive/sessions/{sessionId}/profile`: Public normalized profile.
- `GET /api/dev/ai/adaptive/sessions/{sessionId}`: Developer inspection detailing Applied Evaluation (AI), Follow-up Evaluation (AI), Adaptive Branch (Rule), Final Aggregated Level (Rule), and Backend-Derived Profile (Builder).

---

## 3. Verification & Regression Results

| Test Suite | Target | Result | Duration |
| :--- | :--- | :--- | :--- |
| **Dataset Validator** | 48 Questions, 18/18 Frozen v1.2 Core Baseline Fingerprints | **PASS** (18/18 match) | 8s |
| **Backend Unit & Integration** | `dotnet test -c Release backend/SkillProof.slnx` | **169 / 169 PASS** | 12s |
| **Frontend Production Build** | `npm run build` (Next.js 16.3.5 Turbopack) | **PASS** (0 errors) | 18s |
| **Playwright E2E Regression** | `npx playwright test --workers=1` (35 specs) | **35 / 35 PASS** | 39.9s |
| **Regression OpenAI Calls** | External HTTP requests during test runs | **0** | - |

### E2E Test Coverage Breakdown
- `milestone1.spec.ts`: 2 passed (Role selection & Diagnostic flow)
- `milestone2.spec.ts`: 2 passed (Evaluation, Profile, Insufficient Evidence legacy)
- `milestone4.spec.ts`: 1 passed (Learning Roadmap flow)
- `milestone5.spec.ts`: 2 passed (Gap-backward project recommendation)
- `milestoneI2.spec.ts`: 6 passed (Dynamic skill selection UI)
- `milestoneI3.spec.ts`: 5 passed (Dynamic question selection from SQLite)
- `milestoneI4.spec.ts`: 6 passed (Data Foundation v2 SQLite runtime)
- `milestoneI5.spec.ts`: 4 passed (Developer AI Inspector observability)
- `milestoneI6.spec.ts`: 4 passed (Adaptive career readiness diagnostic engine)
- `milestoneI7.spec.ts`: 2 passed (Explainable Career Readiness Profile & neutral Insufficient Evidence semantics)
- `smoke.spec.ts`: 1 passed (Homepage smoke test)

---

## 4. Exit Criteria Verification Checklist

- [x] **1. Adaptive results produce normalized Skill Profile**: Exposed via `session.Profile` and `GET /api/diagnostics/adaptive/sessions/{sessionId}/profile`.
- [x] **2. One final profile per assessed skill**: Exactly 1 item per assessed skill in `CareerReadinessProfile.Skills`.
- [x] **3. Evidence preserved**: Evaluator evidence from stages 1 and 2 aggregated faithfully.
- [x] **4. Evidence deduplicated**: Deduplicated by trimmed, lowercase canonical strings.
- [x] **5. Demonstrated strengths grounded in evidence**: Extracted directly from observed positive evidence without extrapolation. Empty when Insufficient Evidence.
- [x] **6. Development gaps supported**: Highlighted for Foundation and Applied levels without reaching Advanced.
- [x] **7. Evidence gaps distinguished from development gaps**: Category A vs Category B separation with distinct UI badges and semantics.
- [x] **8. Insufficient Evidence preserved semantically**: Never mapped to "Beginner", "Weak", "Failed", or "0%". Explains missing diagnostic data neutrally.
- [x] **9. Next development areas generated deterministically**: Sourced directly from approved catalog subskills in SQLite.
- [x] **10. No numeric readiness score**: Summary is purely count-based (`X Intermediate, Y Beginner, Z Advanced`).
- [x] **11. No arbitrary percentages**: Zero percentage readiness calculated or shown.
- [x] **12. Public profile hides rubric**: Scoring rubrics strictly private.
- [x] **13. Public profile hides expectedSignals**: `expectedSignals` omitted from public API and UI.
- [x] **14. Public profile hides prompts**: System instructions and evaluation prompts omitted.
- [x] **15. Roadmap handoff contract exists**: `RoadmapHandoffContract` generated cleanly for Milestone I8.
- [x] **16. Inspector distinguishes AI evidence from backend derivation**: Dev AI Inspector adaptive section provides clean distinction.
- [x] **17. I6 behavior unchanged**: Adaptive branching and deterministic synthesis preserved.
- [x] **18. Financial Analyst unchanged**: Legacy assessment flow preserved.
- [x] **19. Dataset unchanged**: 48 questions validated.
- [x] **20. Frozen baseline unchanged**: 18/18 Core questions baseline verified against SHA-256 fingerprint.
- [x] **21. Backend tests PASS**: 169/169 tests passed.
- [x] **22. Frontend build PASS**: Production build compiled with zero errors.
- [x] **23. Playwright PASS**: 35/35 tests passed.
- [x] **24. Zero regression OpenAI requests**: Deterministic evaluation utilized during regression (`LiveEvaluationEnabled = false`).
