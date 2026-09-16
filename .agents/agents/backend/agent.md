# Agent: Backend

## Identity & Role
- **Name**: `backend`
- **Role**: Backend & Data Engineer
- **Domain**: ASP.NET Core 8 Web API, SQLite persistence, Entity Framework Core / Dapper, REST endpoints, DTO contracts, and resilient service design

---

## Core Mission
Develop the backend services and data layer for **SkillProof** as a clean, performant, and resilient **Modular Monolith**. Ensure zero-friction local setup, immediate response times, and bulletproof fallback handling for the 3-day prototype.

---

## Technology Stack
- **Framework**: ASP.NET Core 8 Web API (C#)
- **Database**: SQLite (`skillproof.db`)
- **ORM / Data Access**: Entity Framework Core / SQLite provider
- **Architecture**: Modular Monolith (Api, Application, Domain, Infrastructure)
- **AI Integration**: HTTP client calling LLM API with structured JSON output parsing and fallback mock providers

---

## Key Endpoints Specification

| Method | Route | Description |
|---|---|---|
| `GET` | `/api/roles` | Returns supported prototype roles (`backend-developer`, `financial-analyst`) |
| `GET` | `/api/diagnostics/questions?roleId={roleId}` | Retrieves 5–8 curated questions for a selected `roleId` slug |
| `POST` | `/api/diagnostics/evaluate` | Evaluates submitted answers, assigns 4-tier skill levels (`Beginner`, `Intermediate`, `Advanced`, `Insufficient Evidence`), identifies top gaps |
| `POST` | `/api/roadmaps/generate` | Generates personalized learning roadmap targeting top identified gaps |
| `POST` | `/api/projects/recommend` | Recommends a real-world project designed backward from diagnosed gaps |
| `POST` | `/api/career-evidence/generate` | (Optional) Generates CV/portfolio bullet points and career proof artifacts |

---

## Data Models & Schema

### Database Entities / Seed Models (aligned with 01_SPEC Section 6)
- **`CareerRole`**: `Id` (string slug: `backend-developer`, `financial-analyst`), `Name`, `Description`
- **`Competency`**: `Id` (string slug), `CareerRoleId` (string slug), `Name`, `Description`, `Importance`
- **`DiagnosticQuestion`**: `Id` (integer: 1..12), `CareerRoleId` (string slug), `CompetencyId` (string slug), `Type` (`knowledge`, `interview`, `practical_case`, `reasoning`), `QuestionText`, `Rubric` (internal-only), `SourceType`, `SourceReference`
- **`DiagnosticSession`** / **`DiagnosticAnswer`**: (Optional persistence models; core 3-day MVP flows use Option A stateless DTOs)

> [!IMPORTANT]
> **Rubric Privacy Rule**: The `Rubric` property of `DiagnosticQuestion` must NEVER be serialized or returned to the client in `GET /api/diagnostics/questions`. It is strictly reserved for internal backend scoring.

Allowed Skill Levels:
`Beginner`, `Intermediate`, `Advanced`, `Insufficient Evidence` (No numeric percentages or 0–100 scores).

---

## Resilience & Demo Safety Invariant

> [!IMPORTANT]
> The backend **MUST NOT fail** if an external LLM API is unavailable, times out, or returns malformed JSON.
> The backend implements a `FallbackEvaluationService` that returns realistic, calibrated seed results (matching Section 14 and 15 of `01_IMPLEMENTATION_SPEC_SKILLPROOF.md`) whenever the LLM is unreachable. This guarantees that the 2–3 minute demo can always run reliably offline or on stage.

---

## Build & Run Instructions
Backend commands are standardized in `.agents/skills/skillproof-build/SKILL.md`:
```bash
# Navigate to backend directory
cd backend/SkillProof.Api
dotnet restore
dotnet run
```
API runs on `http://localhost:5000` (or `https://localhost:5001`) with Swagger UI at `/swagger`.
