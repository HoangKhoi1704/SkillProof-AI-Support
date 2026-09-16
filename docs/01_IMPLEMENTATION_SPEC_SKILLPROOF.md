# 01 — IMPLEMENTATION SPEC — SKILLPROOF

> Read `00_SOURCE_OF_TRUTH_SKILLPROOF.md` before implementing anything.
> This document translates the product source of truth into a minimal 3-day technical specification.

## 1. Goal

Build a working fullstack prototype that demonstrates:

```text
Target Role
→ Diagnostic Assessment
→ AI Skill Profile
→ Skill Gaps
→ Personalized Roadmap
→ Recommended Real-World Project
```

Optional:
```text
Prepared Project Result
→ AI Career Evidence
```

---

## 2. Recommended Stack

```text
Frontend: Next.js
Backend: ASP.NET Core Web API
Database: SQLite
AI: One LLM provider
Architecture: Modular Monolith
```

Avoid unnecessary infrastructure.

---

## 3. Prototype Roles

### Backend Developer

Seed competencies:
- Programming Fundamentals
- REST API
- SQL / Database
- Git
- Testing
- Authentication
- System Design
- Deployment
- Problem Solving
- Communication

### Financial Analyst

Seed competencies:
- Accounting Fundamentals
- Financial Statements
- Excel / Spreadsheet Skills
- Financial Modeling
- Data Analysis
- Ratio Analysis
- Forecasting
- Business Reasoning
- Presentation
- Communication

For the prototype, select 4–6 competencies per role for the actual diagnostic to keep assessment short.

---

## 4. Main Screens

### Screen 1 — Target Role
User selects:
- Backend Developer
- Financial Analyst

### Screen 2 — Career Readiness Test
Show 5–8 questions per target role (see [`docs/05_CURATED_SEED_QUESTIONS_SKILLPROOF.md`](file:///d:/Roy/SkillProof-Support-AI/docs/05_CURATED_SEED_QUESTIONS_SKILLPROOF.md) and [`harness/fixtures/seed-questions.json`](file:///d:/Roy/SkillProof-Support-AI/harness/fixtures/seed-questions.json)).

The assessment should feel like a short interview preparation test, not a university exam.

Question types:
- knowledge,
- interview,
- practical case,
- reasoning.

Question source rules:
- Prefer realistic interview-style questions.
- Use manually curated questions for the prototype (6 questions per role provided in `docs/05_CURATED_SEED_QUESTIONS_SKILLPROOF.md`).
- If a question is presented as coming from a real interview/company, store its source URL or source reference.
- If the source cannot be verified, mark it as `[source not verified - unverified]`.

> [!IMPORTANT]
> **Rubric Privacy Invariant**: `GET /api/diagnostics/questions` MUST NOT expose scoring rubrics to the client browser. Rubrics remain internal backend-only evaluation data.


### Screen 3 — Skill Profile
Display:
- Beginner
- Intermediate
- Advanced
- Insufficient Evidence

For each skill show:
- level,
- short reasoning.

### Screen 4 — Personalized Roadmap
Display:
- top 3 gaps,
- learning priorities,
- short recommended practice.

### Screen 5 — Recommended Project
Display:
- project title,
- problem,
- requirements,
- which skill gaps each requirement targets.

### Optional Screen 6 — Future Outcome
Use prepared sample:
- completed project,
- evaluated skills,
- generated CV bullet / portfolio description.

---

## 5. Minimal Backend Modules

```text
Modules/
├── CareerRoles/
├── Diagnostics/
├── SkillAssessment/
├── Roadmaps/
├── Projects/
└── CareerEvidence/   # optional
```

Each module may contain:

```text
Controller
Service
DTOs
Models
```

Keep business logic in services, not controllers.

---

## 6. Minimal Data Model

### CareerRole
```text
Id (string slug: "backend-developer" | "financial-analyst")
Name (string)
Description (string)
```

### Competency
```text
Id (string slug)
CareerRoleId (string slug)
Name (string)
Description (string)
Importance (string / int)
```

### DiagnosticQuestion
```text
Id (int: 1..12)
CareerRoleId (string slug)
CompetencyId (string slug)
Type (string: "knowledge" | "interview" | "practical_case" | "reasoning")
QuestionText (string)
Rubric (object / text: strictly backend evaluation only)
SourceType (string: "team_curated" | "public_interview_resource" | "company_interview_source")
SourceReference (string)
```

`SourceType` examples:
```text
team_curated
public_interview_resource
company_interview_source
```
Do not label a question as a real-company interview question unless its source is verified.

### State Architecture Note (Option A: Stateless DTOs)
For the 3-day prototype, state flows between frontend screens via **Stateless DTOs**:
- `POST /api/diagnostics/evaluate` returns `{ roleId, skills, topGaps }`.
- `POST /api/roadmaps/generate` receives `{ roleId, topGaps, skills }`.
- `POST /api/projects/recommend` receives `{ roleId, topGaps, roadmap }`.
Database persistence models (`DiagnosticSession`, `DiagnosticAnswer`, etc.) are optional logging structures and do not block core API execution.

---

## 7. Minimal API

Canonical MVP routes:
- `GET /api/roles`
- `GET /api/diagnostics/questions`
- `POST /api/diagnostics/evaluate`
- `POST /api/roadmaps/generate`
- `POST /api/projects/recommend`

### 1. Roles
```http
GET /api/roles
```
Response (`200 OK`):
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

### 2. Diagnostic Questions
```http
GET /api/diagnostics/questions?roleId={roleId}
```
Query parameters:
- `roleId` (required, string slug: `backend-developer` | `financial-analyst`)

Response (`200 OK`):
> **Privacy Invariant**: Rubrics are strictly excluded from this public response.
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

### 3. Submit Diagnostic
```http
POST /api/diagnostics/evaluate
```
Request:
```json
{
  "roleId": "backend-developer",
  "answers": [
    {
      "questionId": 1,
      "answer": "PUT replaces the entire resource representation while PATCH performs a partial update..."
    }
  ]
}
```
Response (`200 OK`):
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
Allowed skill levels only: `Beginner`, `Intermediate`, `Advanced`, `Insufficient Evidence`.

### 4. Generate Roadmap
```http
POST /api/roadmaps/generate
```
Request (Stateless):
```json
{
  "roleId": "backend-developer",
  "topGaps": [
    "SQL / Database",
    "Testing",
    "System Design"
  ],
  "skills": [
    {
      "name": "SQL / Database",
      "level": "Beginner"
    },
    {
      "name": "Testing",
      "level": "Beginner"
    },
    {
      "name": "System Design",
      "level": "Beginner"
    }
  ]
}
```
Response (`200 OK`):
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

### 5. Recommend Project
```http
POST /api/projects/recommend
```
Request (Stateless):
```json
{
  "roleId": "backend-developer",
  "topGaps": [
    "SQL / Database",
    "Testing",
    "System Design"
  ],
  "roadmap": [
    {
      "skill": "SQL / Database",
      "priority": 1,
      "learningGoal": "Master table joins, indexing strategies, and query execution plan analysis."
    }
  ]
}
```
Response (`200 OK`):
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

### 6. Standard Error Format
All endpoints return consistent error responses on client or validation errors (`400 Bad Request`, `404 Not Found`, `500 Internal Server Error`):
```json
{
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "roleId must be one of: backend-developer, financial-analyst"
  }
}
```

### 7. Optional Career Evidence
```http
POST /api/career-evidence/generate
```


---

## 8. AI Prompt — Diagnostic Evaluation

Required structure:

```text
ROLE:
You are a career-readiness evaluator.

TASK:
Evaluate the student's answer against the provided competency and rubric.

CONTEXT:
- Target role
- Competency
- Question
- Rubric
- Student answer

RULES:
- Assess only from the student's answer and provided rubric.
- Do not invent knowledge or experience.
- If the answer is insufficient, use "Insufficient Evidence".
- Do not use arbitrary numeric scores.
- Explain the result briefly.

ALLOWED LEVELS:
Beginner
Intermediate
Advanced
Insufficient Evidence

OUTPUT:
Structured JSON only.
```

Suggested JSON:

```json
{
  "competency": "SQL",
  "level": "Beginner",
  "reason": "...",
  "evidence": ["..."]
}
```

---

## 9. AI Prompt — Roadmap

Input:
- target role,
- assessed skills,
- top gaps.

Rules:
- prioritize at most 3 skills,
- do not recommend relearning demonstrated skills,
- each item must include one learning goal and one practice activity,
- keep roadmap realistic and concise.

Output JSON.

---

## 10. AI Prompt — Project Recommendation

Input:
- target role,
- top skill gaps,
- roadmap.

Rules:
- project must require the student to use the missing skills,
- every major requirement must map to at least one gap,
- project should be feasible for a student,
- avoid random generic projects,
- explain why the project was chosen.

Output example:

```json
{
  "title": "Expense Management API",
  "reason": "This project directly requires SQL, testing, and basic system design.",
  "requirements": [
    {
      "text": "Use a relational database and write non-trivial queries.",
      "skill": "SQL"
    }
  ]
}
```

---

## 11. Validation

Backend must validate LLM output.

Allowed skill levels only:

```text
Beginner
Intermediate
Advanced
Insufficient Evidence
```

Reject or retry malformed JSON.

Do not render raw model output directly.

---

## 12. Guardrails

- no fabricated student experience,
- no hiring decisions,
- no formal-certification language,
- no fake percentages,
- no unsupported claims,
- no skill conclusion when evidence is insufficient,
- role competency framework is authoritative,
- diagnostic is guidance, not a professional certification.

---

## 13. Frontend Flow

```text
/roles
  ↓
/diagnostic
  ↓
/skill-profile
  ↓
/roadmap
  ↓
/project
  ↓
/result   # optional
```

Use clear loading and error states.

---

## 14. Seed Demo — Backend Developer

Diagnostic outcome:

```text
REST API       Intermediate
SQL            Beginner
Testing        Beginner
System Design  Beginner
```

Top gaps:

```text
1. SQL
2. Testing
3. System Design
```

Roadmap:
- SQL joins/indexing/query optimization
- unit testing/mocking/edge cases
- basic API architecture/database design

Project:
**Expense Management API**

---

## 15. Seed Demo — Financial Analyst

Possible diagnostic outcome:

```text
Financial Statements   Intermediate
Excel                   Intermediate
Financial Modeling      Beginner
Forecasting              Beginner
Data Visualization       Beginner
```

Top gaps:
1. Financial Modeling
2. Forecasting
3. Data Visualization

Project:
**Company Financial Health & 3-Year Outlook**

---

## 16. 3-Day Plan

### Day 1
- Next.js setup
- ASP.NET Core setup
- SQLite
- seed roles / competencies / questions
- role selection
- diagnostic UI
- working API connection

### Day 2
- LLM integration
- diagnostic evaluation
- structured output validation
- skill profile
- roadmap generation
- project recommendation

### Day 3
- UI polish
- guardrails
- error handling
- prepared demo scenario
- optional career-evidence generation
- deployment
- rehearse 2–3 minute demo

---

## 17. Acceptance Criteria

Prototype is complete when:

1. User can select Backend Developer or Financial Analyst.
2. User can complete a short diagnostic.
3. AI returns explainable skill levels.
4. System identifies top skill gaps.
5. System generates a short personalized roadmap.
6. System recommends one project tied directly to those gaps.
7. The full flow runs without manual editing.
8. Demo completes in 2–3 minutes.

---

## 18. Out of Scope

Do not implement:
- job matching,
- LinkedIn integration,
- recruiter portal,
- multi-agent runtime,
- complex RAG,
- full GitHub analysis,
- full Excel analysis,
- payments,
- social network,
- production-grade assessment science,
- official skill certification.

If an AI agent proposes these features, reject them unless explicitly approved by the project owner.
