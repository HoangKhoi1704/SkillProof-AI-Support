---
name: api-contract
description: Defines and freezes API contracts between SkillProof backend and frontend, including endpoints, request/response DTOs, validation rules, error formats, and contract-change procedures.
---

# API Contract Skill

Use this skill whenever a SkillProof feature requires communication between frontend and backend.

## Goal
Prevent frontend/backend mismatch by defining and freezing stable contracts before integration, distinguishing current implementations from approved V2 target architectures.

---

## 1. Implementation Status & Invariants

### CURRENT IMPLEMENTATION (V1.2 / Data Foundation)
- **Roles**: `backend-developer` (active catalog with 48 questions across 16 assessable skills, SQLite backed).
- **Question IDs**: String IDs (e.g. `"q-be-prog-01"`).
- **Rubric Privacy**: Diagnostic endpoints **MUST NOT** expose scoring rubrics or `expectedSignals` to client browsers. Rubrics remain server-side evaluation data.
- **Skill Level Enums**: Exactly 4 allowed qualitative tiers: `"Beginner"`, `"Intermediate"`, `"Advanced"`, `"Insufficient Evidence"`.
- **Not Assessed State**: Competencies not assessed remain in the distinct state `"Not Assessed"`, never silently collapsed to `"Beginner"`.

### APPROVED V2 TARGET ARCHITECTURE (Planned)
- **Three Demo Roles**: `frontend-developer`, `backend-developer`, `data-analyst`, anchored in canonical `roadmap.sh` role graphs.
- **Assessment Selection**: Manual skill selection starting from 0 preselected skills.
- **Multi-Page Flow**: Role Selection → Skill Selection → Interview → Post-Answer Explanation → Skill Matrix Profile → Personalized Roadmap → Practice Projects → Portfolio Project → Evaluation → CV Proof.
- **State Persistence**: 30-minute transient session persistence across navigation and reloads.
- **Post-Answer Explanation**: Returns structured explanation (coverage, improvements, reference) without exposing private rubrics.

---

## 2. Active Canonical Endpoints

### 1. `GET /api/roles`
- **Purpose**: Retrieve supported career roles.
- **Response (`200 OK`)**:
  ```json
  [
    {
      "id": "backend-developer",
      "name": "Backend Developer",
      "description": "Designs, builds, tests, and maintains scalable server-side systems, APIs, and data architectures."
    }
  ]
  ```

### 2. `GET /api/catalog/skills?roleId={roleId}`
- **Purpose**: Retrieve skill taxonomy for role (Core competencies, Recommended competencies, and separated Programming Languages).

### 3. `GET /api/diagnostics/questions?roleId={roleId}`
- **Purpose**: Retrieve diagnostic questions for assessment. Excludes rubrics and expected signals.

### 4. `POST /api/diagnostics/evaluate`
- **Purpose**: Evaluate candidate responses, assign qualitative skill levels with evidence, and identify top skill gaps.
- **Response**: Qualitative evaluations with reasons, evidence, and prioritized gaps.

### 5. `POST /api/roadmaps/generate`
- **Purpose**: Generate personalized learning roadmap based on diagnosed gaps.
- *(V2 Target: Graph personalization derived from canonical roadmap.sh role graph).*

### 6. `POST /api/projects/recommend`
- **Purpose**: Provide practical projects mapped to skill gaps.
- *(V2 Target: Matches against approved curated catalog).*
