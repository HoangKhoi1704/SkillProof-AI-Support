# SkillProof V2.3 — Question Bank V3 + Assessment V2 + Skill Matrix Implementation Plan

This implementation plan details the end-to-end design and execution strategy for **SkillProof Milestone V2.3**, fulfilling production assessment for all three primary demo roles: **Frontend Developer**, **Backend Developer**, and **Data Analyst**.

---

## 1. Scope & Architecture Decisions

### A. Question Bank V3 Partitioning & Structure
In accordance with Section 37:
- `data/v3/questions/frontend.json`: Questions for Frontend Developer assessable competencies (mandatory fundamentals: `shared.internet-http`, `frontend.html-core`, `shared.javascript`, `frontend.web-security`; plus core/recommended assessable skills: `frontend.css-styling`, `frontend.typescript`, `frontend.framework-react`, `frontend.testing`, `frontend.performance`).
- `data/v3/questions/data-analyst.json`: Questions for Data Analyst assessable competencies (mandatory fundamentals: `data-analyst.spreadsheets`, `shared.sql`, `data-analyst.python-or-r`, `data-analyst.data-wrangling`, `data-analyst.statistics`, `data-analyst.eda`; plus core/recommended: `data-analyst.bi-dashboards`, `data-analyst.visualization`). Strictly excludes deep learning, CNNs, PyTorch/TensorFlow from core requirements.
- `data/v3/questions/backend-mappings.json`: Mappings from V3 canonical skills to the 48 frozen backend questions in `data/backend/questions.json`. Zero modification to the frozen 48 questions or baseline fingerprints.
- `data/v3/question-sources.json`: Provenance registry for all question sources with canonical URLs, commit SHAs, extraction dates, and verification status.

### B. Assessment V2 Composition
- When starting an assessment session, the server resolves `roleMandatoryFundamentalIds` from the trusted canonical framework (`data/v3/role-roadmap-nodes.json` where `mandatoryFundamental === true`).
- Combines `roleMandatoryFundamentalIds` + `userSelectedSkillIds`, deduplicated by canonical skill ID.
- Assesses each skill once with calibrated question progression (`applied` initially for applied skills, or `foundation`/`concept` for fundamental theory), branching to `foundation` or `advanced-reasoning` follow-ups.

### C. Post-Answer Explanation Contract
- Safe API DTO (`PostAnswerExplanationDto`):
  - `questionId: string`
  - `skillId: string`
  - `skillName: string`
  - `whatYouCovered: string[]` (derived from observed evidence)
  - `whatCouldBeStronger: string[]` (derived from missing signals / development areas)
  - `referenceExplanation: string` (derived from question reference explanation)
- Before submission: Public question DTO **never** exposes rubric, expectedSignals, or reference explanation.
- After submission: Returns explanation panel with "What you covered", "What could be stronger", "Reference explanation", and a "Next Question →" action. Question is not replaced until candidate clicks "Next Question".

### D. Visual Skill Matrix UI (`/assessment/result`)
- Replaces verbose card list with a clear tabular/matrix grid:
  - **Rows:** All role competencies (both assessed and unassessed).
  - **Columns / Dimensions:** Fundamentals, Applied, Reasoning, Overall Status.
  - **States:** `Advanced`, `Intermediate`, `Beginner`, `Insufficient Evidence`, `Not Assessed`.
  - **Gap Types:**
    - `ASSESSED GAP`: Evidence exists but advanced reasoning/competency not demonstrated.
    - `EVIDENCE GAP`: Insufficient evidence provided in interview.
    - `ROLE COVERAGE GAP`: Relevant role competency was not selected/assessed.
  - Clicking a row expands detailed evaluation: Level/Status, Evidence observed, Why this level, What to improve next, Gap type.
  - Zero percentages, zero radar scores, zero fake precision numbers.

### E. Question Bank Validator
- `harness/validators/validate-v3-question-bank.mjs`:
  - Enforces schema, unique IDs, valid roles, canonical skill IDs.
  - Validates question types (`definition`, `concept`, `scenario`, `debugging`, `design`, `reasoning`, `code-reading`).
  - Validates difficulties (`foundation`, `applied`, `advanced-reasoning`).
  - Validates all 4 qualitative rubric states (`Insufficient Evidence`, `Beginner`, `Intermediate`, `Advanced`).
  - Enforces reference explanations on all questions.
  - Verifies provenance integrity (valid source IDs, canonical URLs, verifiedAt).
  - Checks no mandatory Deep Learning for Data Analyst and no mandatory Meta-Framework for Frontend.
  - Ensures Backend 48 still resolve.

---

## 2. Proposed Changes

### Component 1: Question Bank V3 Data & Provenance
#### [NEW] [`data/v3/questions/frontend.json`](file:///d:/Roy/SkillProof-Support-AI/data/v3/questions/frontend.json)
- Curated questions across Frontend mandatory fundamentals and core assessable skills.
- Diverse question types (`concept`, `scenario`, `debugging`, `reasoning`, `code-reading`).
- Complete 4-tier rubrics, expected signals, reference explanations, and source citations to `roadmap.sh/frontend` content.

#### [NEW] [`data/v3/questions/data-analyst.json`](file:///d:/Roy/SkillProof-Support-AI/data/v3/questions/data-analyst.json)
- Curated questions across Data Analyst mandatory fundamentals (`spreadsheets`, `sql`, `python-or-r`, `data-wrangling`, `statistics`, `eda`) and core assessable skills (`bi-dashboards`, `visualization`).
- Analysis-focused, strictly excluding deep learning / neural networks.

#### [NEW] [`data/v3/questions/backend-mappings.json`](file:///d:/Roy/SkillProof-Support-AI/data/v3/questions/backend-mappings.json)
- Links canonical backend skill IDs to the 48 frozen backend questions.

#### [NEW] [`data/v3/question-sources.json`](file:///d:/Roy/SkillProof-Support-AI/data/v3/question-sources.json)
- Verified provenance records for question datasets.

#### [NEW] [`harness/validators/validate-v3-question-bank.mjs`](file:///d:/Roy/SkillProof-Support-AI/harness/validators/validate-v3-question-bank.mjs)
- Automated validator verifying all constraints specified in Section 38.

---

### Component 2: Backend Assessment V2 & Catalog Integration
#### [MODIFY] [`backend/SkillProof.Api/Data/Catalog/Entities.cs`](file:///d:/Roy/SkillProof-Support-AI/backend/SkillProof.Api/Data/Catalog/Entities.cs)
- Add `ReferenceExplanation` string to `Question` entity.

#### [MODIFY] [`backend/SkillProof.Api/Data/Catalog/CatalogSeeder.cs`](file:///d:/Roy/SkillProof-Support-AI/backend/SkillProof.Api/Data/Catalog/CatalogSeeder.cs)
- Seed V3 questions from `data/v3/questions/frontend.json`, `data/v3/questions/data-analyst.json`, and backend questions.
- Update `hasQuestionCoverage` flag in `RoleRoadmapNodes` to `true` for newly covered competencies.

#### [MODIFY] [`backend/SkillProof.Api/Models/AdaptiveDiagnosticModels.cs`](file:///d:/Roy/SkillProof-Support-AI/backend/SkillProof.Api/Models/AdaptiveDiagnosticModels.cs)
- Add `PostAnswerExplanationDto` record (`WhatYouCovered`, `WhatCouldBeStronger`, `ReferenceExplanation`).
- Add `LastExplanation` to `AdaptiveSessionResponse`.
- Add `GapType` (`AssessedGap`, `EvidenceGap`, `RoleCoverageGap`) to `AdaptiveSkillResultDto`.

#### [MODIFY] [`backend/SkillProof.Api/Services/AdaptiveDiagnosticService.cs`](file:///d:/Roy/SkillProof-Support-AI/backend/SkillProof.Api/Services/AdaptiveDiagnosticService.cs)
- Support V2 assessment composition: resolve mandatory fundamentals server-side, deduplicate with user selections.
- Support all three roles (`frontend-developer`, `backend-developer`, `data-analyst`).
- In `SubmitAnswerAsync`, construct safe `PostAnswerExplanationDto` without exposing rubric or prompt details.
- Support step advancement (`AdvanceToNextQuestionAsync` or returning explanation before moving `CurrentQuestion`).

#### [MODIFY] [`backend/SkillProof.Api/Services/DeterministicDiagnosticEvaluator.cs`](file:///d:/Roy/SkillProof-Support-AI/backend/SkillProof.Api/Services/DeterministicDiagnosticEvaluator.cs)
- Support deterministic offline evaluation for Frontend Developer and Data Analyst questions without OpenAI.

---

### Component 3: Frontend Assessment V2 & Skill Matrix
#### [MODIFY] [`frontend/app/types.ts`](file:///d:/Roy/SkillProof-Support-AI/frontend/app/types.ts)
- Add `PostAnswerExplanation` interface.
- Add `LastExplanation` to `AdaptiveSessionResponse`.
- Add `SkillMatrixItem`, `SkillMatrixDimension`, and `GapType` types.

#### [MODIFY] [`frontend/app/assessment/skills/page.tsx`](file:///d:/Roy/SkillProof-Support-AI/frontend/app/assessment/skills/page.tsx)
- Enable Frontend and Data Analyst skill selection to proceed to `/assessment/interview` now that questions exist.

#### [MODIFY] [`frontend/app/assessment/interview/page.tsx`](file:///d:/Roy/SkillProof-Support-AI/frontend/app/assessment/interview/page.tsx)
- Add Post-Answer Explanation flow:
  1. Candidate types answer and clicks "Submit Answer".
  2. Question card reveals the Explanation Panel ("What you covered", "What could be stronger", "Reference explanation").
  3. Candidate clicks "Next Question →" to advance.
- Maintain strict privacy: rubric, signals, and reference explanations are never shown prior to submission.

#### [MODIFY] [`frontend/app/assessment/result/page.tsx`](file:///d:/Roy/SkillProof-Support-AI/frontend/app/assessment/result/page.tsx)
- Implement Visual Skill Matrix:
  - Rows: All canonical skills for the role.
  - Columns: Fundamentals, Applied, Reasoning, Overall Status.
  - States: `Advanced`, `Intermediate`, `Beginner`, `Insufficient Evidence`, `Not Assessed`.
  - Distinct badge colors for `Not Assessed` (gray) vs `Beginner` (amber).
  - Explicit gap labels: `Assessed Gap`, `Evidence Gap`, `Role Coverage Gap`.
  - Expandable row details: Evidence observed, Reasoning, Next steps.

---

### Component 4: Tests & Documentation
#### [NEW] [`backend/SkillProof.Api.Tests/MilestoneV23AssessmentTests.cs`](file:///d:/Roy/SkillProof-Support-AI/backend/SkillProof.Api.Tests/MilestoneV23AssessmentTests.cs)
- Tests for V3 question seeding, mandatory composition, post-answer explanation safety, deterministic evaluation for Frontend/Data Analyst, and privacy invariants.

#### [NEW] [`tests/e2e/v2_3_assessment.spec.ts`](file:///d:/Roy/SkillProof-Support-AI/tests/e2e/v2_3_assessment.spec.ts)
- Playwright E2E tests for Frontend, Backend, and Data Analyst end-to-end flows:
  - Skill selection -> Interview -> Submit answer -> Explanation panel -> Next question -> Result -> Skill Matrix -> Unassessed state.

#### [NEW] [`docs/v2.3/V2_3_QUESTION_BANK_AUDIT.md`](file:///d:/Roy/SkillProof-Support-AI/docs/v2.3/V2_3_QUESTION_BANK_AUDIT.md)
- Complete question source coverage audit.

#### [NEW] [`docs/v2.3/V2_3_ASSESSMENT_IMPLEMENTATION_REPORT.md`](file:///d:/Roy/SkillProof-Support-AI/docs/v2.3/V2_3_ASSESSMENT_IMPLEMENTATION_REPORT.md)
- Milestone implementation and verification report.

---

## 3. Verification Plan

### Automated Validators & Tests
1. `node harness/validators/validate-v3-question-bank.mjs` (New V2.3 validator)
2. `node harness/validators/validate-v3-catalog.mjs`
3. `node harness/validators/validate-v2.1-proposal.mjs`
4. `node harness/validators/validate-backend-assessment-data.mjs`
5. `dotnet build backend/SkillProof.slnx`
6. `dotnet test backend/SkillProof.slnx` (All unit and integration tests)
7. `npm run build` in `frontend/` (Zero TypeScript or compilation errors)
8. `npx playwright test tests/e2e/v2_3_assessment.spec.ts` (Full E2E suite with 0 live OpenAI calls)

### Manual / Browser Verification
- Verify that post-answer explanation does not reveal rubric or signals prior to submission.
- Verify that Skill Matrix displays `Not Assessed` for unassessed competencies and distinguishes all three gap types.
