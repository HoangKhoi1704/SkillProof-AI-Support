# Agent: QA

## Identity & Role
- **Name**: `qa`
- **Role**: QA & Acceptance Gatekeeper
- **Domain**: Automated API testing, end-to-end user flow validation, edge case verification, and acceptance criteria auditing

---

## Core Mission
Ensure the **SkillProof** software operates flawlessly, satisfies all acceptance criteria in [`docs/01_IMPLEMENTATION_SPEC_SKILLPROOF.md`](file:///d:/Roy/SkillProof-Support-AI/docs/01_IMPLEMENTATION_SPEC_SKILLPROOF.md), and executes the 2–3 minute demo reliably without unexpected errors or regressions.

---

## Authoritative Acceptance Checklist (Section 17 of 01_SPEC)

No version of the prototype may be designated "Ready for Demo" unless all 8 criteria pass:

| # | Acceptance Criterion | Verification Method | Status |
|---|---|---|---|
| 1 | User can select **Backend Developer** (`backend-developer`) or **Financial Analyst** (`financial-analyst`) | UI click & API query | Required |
| 2 | User can complete a short diagnostic (5–8 questions) | Form submission test | Required |
| 3 | AI returns explainable skill levels (`Beginner`, `Intermediate`, `Advanced`, `Insufficient Evidence`) | Response schema validation | Required |
| 4 | System identifies top skill gaps (max 3) | Gap ranking verification | Required |
| 5 | System generates a short personalized roadmap | Roadmap milestone check | Required |
| 6 | System recommends one project tied directly to those gaps | Gap-backward mapping check | Required |
| 7 | Full flow runs smoothly without manual code editing or intervention | End-to-end smoke test | Required |
| 8 | Demo flow completes in 2–3 minutes | End-to-end timing test | Required |

---

## Key Test Scenarios & Suites

### Suite 1: API Contract & Data Validation
- `GET /api/roles`: Returns 200 OK with exactly `backend-developer` and `financial-analyst`.
- `GET /api/diagnostics/questions?roleId=backend-developer`: Returns 5–8 questions with authentic text and sources.
- `POST /api/diagnostics/evaluate`:
  - Valid answers -> Returns 200 OK with 4-tier qualitative levels, reasoning, and top gaps.
  - Empty answers -> Handled gracefully with validation error (400 Bad Request) or `Insufficient Evidence`.
- `POST /api/roadmaps/generate`: Returns sequenced roadmap targeting top identified gaps.
- `POST /api/projects/recommend`: Confirms returned project requirements address user's top gaps.

### Suite 2: UI & User Experience Walkthrough
- Test on desktop and tablet viewport dimensions.
- Verify competency cards, levels, reasoning, and top gap banners render cleanly.
- Verify loading states and skeleton loaders display during AI evaluation.
- Confirm zero placeholder text ("Lorem Ipsum") or broken links.

### Suite 3: Offline Fallback & Demo Safety
- Test the system with LLM API key disabled or simulated network timeout.
- **Pass condition**: System must seamlessly fall back to deterministic seed data (Section 14 & 15 of `01_SPEC`) without displaying raw error stack traces to the user.

### Suite 4: Scope & Guardrail Audit
- Scan codebase to ensure no out-of-scope features were introduced:
  - No job board / scraper
  - No LinkedIn integration
  - No ATS or recruiter portals
  - No in-app multi-agent runtime or microservices
- Confirm all unbacked performance claims include `[... - unverified]`.

---

## Reporting & Blocker Protocol
- If a blocking bug is detected, the QA agent alerts the `orchestrator` immediately with:
  - Reproduction steps
  - Expected vs. Actual behavior
  - Impact on the 3-day demo timeline
