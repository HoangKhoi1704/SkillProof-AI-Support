---
name: api-contract
description: Defines and freezes API contracts between SkillProof backend and frontend, including endpoints, request/response DTOs, validation rules, error formats, and contract-change procedures.
---

# API Contract Skill

Use this skill whenever a SkillProof feature requires communication between frontend and backend.

## Goal
Prevent frontend/backend mismatch by defining and freezing stable contracts before integration.

## Authoritative Canonical Endpoints

The 5 canonical MVP routes defined in `docs/04_DOCUMENTATION_AUDIT_CHECKLIST.md` and `docs/01_IMPLEMENTATION_SPEC_SKILLPROOF.md`:

```text
GET  /api/roles
GET  /api/roles/{roleId}/skills
GET  /api/diagnostics/questions?roleId={roleId}
POST /api/diagnostics/evaluate
POST /api/roadmaps/generate
POST /api/projects/recommend
```

---

## 1. Core Contract Invariants

1. **Role Slugs**: `roleId` is strictly a string slug: `"backend-developer"` | `"financial-analyst"`.
2. **Question IDs**: `questionId` is strictly an integer (`int`).
3. **Rubric Privacy**: `GET /api/diagnostics/questions` **MUST NOT** expose scoring rubrics to the client browser. Rubrics remain backend-only evaluation data.
4. **Stateless State Architecture (Option A)**: State between screens flows through request/response payloads (`roleId`, `skills`, `topGaps`, `roadmap`), with no mandatory session dependency for the 3-day MVP.
5. **Skill Level Enums**: ONLY 4 allowed qualitative tiers: `"Beginner"`, `"Intermediate"`, `"Advanced"`, `"Insufficient Evidence"`. No arbitrary numeric scores or percentages.

---

## 2. Frozen Endpoint Specifications

### 1. `GET /api/roles`
- **Purpose**: Retrieve supported prototype career roles.
- **Request**: No parameters.
- **Response (`200 OK`)**:
  ```json
  [
    {
      "id": "backend-developer",
      "name": "Backend Developer",
      "description": "Designs, builds, and maintains server-side logic, APIs, and databases."
    },
    {
      "id": "financial-analyst",
      "name": "Financial Analyst",
      "description": "Analyzes financial data, builds financial models, and forecasts business performance."
    }
  ]
  ```

### 2. `GET /api/diagnostics/questions?roleId={roleId}`
- **Purpose**: Retrieve 5–8 curated career-readiness questions for the selected role.
- **Query Parameter**: `roleId` (string slug, required).
- **Validation**: If `roleId` is missing or invalid, return `400 Bad Request`.
- **Response (`200 OK`)**:
  ```json
  [
    {
      "id": 1,
      "careerRoleId": "backend-developer",
      "competency": "REST API",
      "type": "interview",
      "questionText": "Explain the difference between PUT and PATCH in REST API design...",
      "sourceType": "team_curated",
      "sourceReference": "[standard interview pattern - unverified]"
    }
  ]
  ```
  *(Note: `rubric` is excluded by design).*

### 3. `POST /api/diagnostics/evaluate`
- **Purpose**: Evaluate student responses, assign 4-tier skill levels with evidence, and identify top skill gaps.
- **Request Body**:
  ```json
  {
    "roleId": "backend-developer",
    "answers": [
      {
        "questionId": 1,
        "answer": "PUT replaces the resource entirely while PATCH modifies specific attributes..."
      }
    ]
  }
  ```
- **Validation**: `roleId` must be supported, `answers` array must not be empty.
- **Response (`200 OK`)**:
  ```json
  {
    "roleId": "backend-developer",
    "skills": [
      {
        "name": "REST API",
        "level": "Intermediate",
        "reason": "Accurately distinguishes PUT vs PATCH semantics and idempotency considerations.",
        "evidence": [
          "Explains complete resource replacement vs partial update",
          "Mentions idempotency guarantees"
        ]
      }
    ],
    "topGaps": [
      "SQL / Database",
      "Testing",
      "System Design"
    ]
  }
  ```

### 4. `POST /api/roadmaps/generate`
- **Purpose**: Generate a prioritized learning roadmap addressing the top diagnosed gaps.
- **Request Body (Stateless)**:
  ```json
  {
    "roleId": "backend-developer",
    "topGaps": ["SQL / Database", "Testing", "System Design"],
    "skills": [
      { "name": "SQL / Database", "level": "Beginner" },
      { "name": "Testing", "level": "Beginner" },
      { "name": "System Design", "level": "Beginner" }
    ]
  }
  ```
- **Validation**: `roleId` supported, `topGaps` 1–3 items.
- **Response (`200 OK`)**:
  ```json
  {
    "roleId": "backend-developer",
    "items": [
      {
        "skill": "SQL / Database",
        "priority": 1,
        "learningGoal": "Master table joins, indexing strategies, and query execution plan analysis.",
        "practiceTask": "Analyze and optimize 3 slow SQL queries using realistic multi-table schemas."
      },
      {
        "skill": "Testing",
        "priority": 2,
        "learningGoal": "Learn unit testing fundamentals, test assertions, and mock dependency isolation.",
        "practiceTask": "Write an automated test suite covering success, validation failure, and edge cases for an API service."
      },
      {
        "skill": "System Design",
        "priority": 3,
        "learningGoal": "Understand caching patterns, stateless service design, and database read/write scaling.",
        "practiceTask": "Design the architecture for a high-traffic rate-limiting or session service."
      }
    ]
  }
  ```

### 5. `POST /api/projects/recommend`
- **Purpose**: Recommend a real-world project engineered backward from the user's diagnosed skill gaps.
- **Request Body (Stateless)**:
  ```json
  {
    "roleId": "backend-developer",
    "topGaps": ["SQL / Database", "Testing", "System Design"],
    "roadmap": [
      {
        "skill": "SQL / Database",
        "priority": 1,
        "learningGoal": "Master table joins, indexing strategies, and query execution plan analysis."
      }
    ]
  }
  ```
- **Response (`200 OK`)**:
  ```json
  {
    "title": "Expense Management API",
    "description": "A RESTful expense tracking and reporting backend engineered specifically to build and demonstrate relational database indexing, complex SQL aggregation, and automated test coverage.",
    "reason": "Directly forces implementation of relational modeling, query optimization, and test automation, addressing the diagnosed SQL and Testing gaps.",
    "requirements": [
      {
        "requirement": "Design a relational schema with foreign key constraints, indexes on query filters, and aggregated reporting queries.",
        "targetsSkill": "SQL / Database"
      },
      {
        "requirement": "Implement a full automated xUnit test suite covering happy paths, input validation, and boundary error conditions.",
        "targetsSkill": "Testing"
      },
      {
        "requirement": "Implement in-memory caching for expense categories and design clean separation of concerns.",
        "targetsSkill": "System Design"
      }
    ]
  }
  ```

### 6. `GET /api/roles/{roleId}/skills`
- **Purpose**: Retrieve role skills and separated programming languages for catalog browsing and skill selection (Milestone I1).
- **Path Parameter**: `roleId` (string slug, e.g. `backend-developer`, required).
- **Validation**: If `roleId` is unknown, return `404 Not Found` with standard error format.
- **Privacy Invariant**: **MUST NOT** expose scoring rubrics, `expectedSignals`, or internal evaluation metadata. Exposes UI-safe metadata only (`id`, `name`, `category`, `skillType`, `description`, `subskills`).
- **Response (`200 OK`)**:
  ```json
  {
    "roleId": "backend-developer",
    "core": [
      {
        "id": "programming-fundamentals",
        "name": "Programming Fundamentals & Clean Code",
        "category": "core",
        "skillType": "competency",
        "description": "...",
        "subskills": [
          { "id": "clean-code-refactoring", "name": "Clean Code & Refactoring", "description": "..." }
        ]
      }
    ],
    "recommended": [...],
    "optional": [...],
    "languages": [
      {
        "id": "csharp",
        "name": "C# / .NET",
        "category": "language",
        "skillType": "language",
        "description": null,
        "subskills": [...]
      }
    ]
  }
  ```

### 7. `POST /api/diagnostics/questions/select`
- **Purpose**: Dynamically select diagnostic questions from SQLite based on user competency and language selection (Milestone I3).
- **Privacy Invariant**: **MUST NOT** expose scoring rubrics, `expectedSignals`, or internal evaluation notes. Exposes public question data only (`id`, `competencyId`, `competency`, `difficulty`, `questionType`, `questionText`, `question`).
- **Request Body**:
  ```json
  {
    "roleId": "backend-developer",
    "selectedSkillIds": [
      "programming-fundamentals",
      "sql",
      "testing"
    ],
    "primaryLanguageId": "csharp"
  }
  ```
- **Validation**:
  - `roleId` must exist in catalog (`404 Not Found` if unknown).
  - `selectedSkillIds` must not be empty, all IDs must exist in catalog and belong to the role (`400 Bad Request` if invalid).
  - Programming languages cannot be passed in `selectedSkillIds` (`400 Bad Request`).
  - If `primaryLanguageId` is provided, it must exist in catalog, belong to the role, and have `skillType = language` (`400 Bad Request`).
  - Unsupported skills (recommended/optional skills without question coverage in v1.2) are returned in `assessmentCoverage.unsupportedSkillIds`, not invented.
- **Response (`200 OK`)**:
  ```json
  {
    "roleId": "backend-developer",
    "questions": [
      {
        "id": "q-be-prog-02",
        "competencyId": "programming-fundamentals",
        "competency": "Programming Fundamentals & Clean Code",
        "difficulty": "applied",
        "questionType": "scenario_diagnostic",
        "questionText": "A critical backend service occasionally deadlocks...",
        "question": "A critical backend service occasionally deadlocks..."
      },
      {
        "id": "q-be-sql-02",
        "competencyId": "sql",
        "competency": "SQL & Relational Data Modeling",
        "difficulty": "applied",
        "questionType": "scenario_diagnostic",
        "questionText": "A reporting query joining `orders` and `order_items`...",
        "question": "A reporting query joining `orders` and `order_items`..."
      }
    ],
    "assessmentCoverage": {
      "requestedSkillIds": ["programming-fundamentals", "sql"],
      "assessedSkillIds": ["programming-fundamentals", "sql"],
      "unsupportedSkillIds": []
    }
  }
  ```

---


## 3. Standard Error Contract

All error responses adhere to:
```json
{
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "roleId must be one of: backend-developer, financial-analyst"
  }
}
```

---

## 4. Change Control & Verification
- Do not silently change field names, types, enum values, or response shapes.
- Any change requires Orchestrator sign-off.
- Backend verifies deserialization and validation rules; frontend verifies error parsing and loading states.
