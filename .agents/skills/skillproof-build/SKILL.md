---
name: skillproof-build
description: Step-by-step procedures, commands, and verification workflows to scaffold, build, run, test, and demo the SkillProof fullstack prototype (ASP.NET Core 8 Web API + Next.js + SQLite).
---

# SkillProof Build & Execution Guide

This skill provides standardized commands and workflows for building, running, verifying, and presenting the **SkillProof** fullstack application.

---

## 1. Environment Prerequisites

Before running the application, verify the developer environment has the necessary tooling installed:

```powershell
# Check .NET 8+ SDK
dotnet --version

# Check Node.js 18+ and npm
node -v
npm -v
```

---

## 2. Directory Layout & Architecture

The application is structured as a **Modular Monolith**:

```text
skillproof/
├── backend/
│   ├── SkillProof.Api/            # ASP.NET Core 8 Web API
│   │   ├── Controllers/           # REST endpoints
│   │   ├── Models/                # DTOs and Database Entities
│   │   ├── Services/              # Business logic & AI prompt client
│   │   ├── Data/                  # EF Core DbContext & SQLite migrations
│   │   ├── appsettings.json       # Config & connection strings
│   │   └── Program.cs
│   └── SkillProof.Api.Tests/      # Automated xUnit test suite
│
├── frontend/                      # Next.js App Router project
│   ├── app/                       # App Router pages (/roles, /diagnostic, /profile, /roadmap)
│   ├── public/                    # Static assets
│   ├── package.json
│   └── tsconfig.json
│
├── .agents/                       # Multi-agent definitions & skills
└── docs/                          # Source of Truth & Specs
```

---

## 3. Quickstart: Building & Running

### Step 1: Start Backend (ASP.NET Core + SQLite)

```powershell
# From the project root:
cd backend/SkillProof.Api

# Restore dependencies and run
dotnet restore
dotnet run --urls="http://localhost:5000"
```

- API Base URL: `http://localhost:5000`
- Swagger UI Documentation: `http://localhost:5000/swagger`
- Database: Creates local `skillproof.db` automatically with seeded questions and competencies on first run.

### Step 2: Start Frontend (Next.js)

Open a second terminal window:

```powershell
# From the project root:
cd frontend

# Install packages (first time only)
npm install

# Start Next.js dev server
npm run dev
```

- Web Client: `http://localhost:3000`

---

## 4. Testing & Verification Workflows

### Run Backend Unit & Integration Tests
```powershell
cd backend/SkillProof.Api.Tests
dotnet test --logger "console;verbosity=detailed"
```

### Smoke Test API Endpoints via PowerShell
```powershell
# 1. Health check & roles
Invoke-RestMethod -Uri "http://localhost:5000/api/roles" -Method Get

# 2. Get questions for Backend Developer
Invoke-RestMethod -Uri "http://localhost:5000/api/diagnostics/questions?roleId=backend-developer" -Method Get
```

### Run Frontend Lint & Build Check
```powershell
cd frontend
npm run lint
npm run build
```

---

## 5. Offline Fallback & Demo Mode

If running in an environment without internet access or without an OpenAI/Gemini API key:
- Ensure `AI:UseMockFallback` is set to `true` in `backend/SkillProof.Api/appsettings.json`.
- The backend will immediately return calibrated seed results from Section 14 (`Backend Developer`) and Section 15 (`Financial Analyst`) of `01_IMPLEMENTATION_SPEC_SKILLPROOF.md`.
- This ensures zero latency and zero failure risk during live presentations.

---

## 6. End-to-End Demo Script (2–3 Minutes)

Follow this script to demonstrate the core value proposition to judges:

1. **Step 1 (0:00 - 0:30) — The Problem & Role Selection**:
   - Open `http://localhost:3000`.
   - Explain the core insight: *"Students learn academic subjects but lack clarity on real-world job readiness."*
   - Select **Backend Developer**.

2. **Step 2 (0:30 - 1:15) — Career Readiness Diagnostic**:
   - Answer the 5–8 interview-style questions.
   - Highlight that questions test reasoning and practical application, not rote memorization.
   - Click **Submit Assessment**.

3. **Step 3 (1:15 - 1:50) — AI Skill Profile & Gap Analysis**:
   - Review the skill profile cards and level badges (`REST API: Intermediate`, `SQL: Beginner`, `Testing: Beginner`, `System Design: Beginner`).
   - Highlight the **Top Skill Gaps** identified by AI.

4. **Step 4 (1:50 - 2:30) — Backward-Engineered Project & Career Proof**:
   - Click **View Roadmap & Project**.
   - Show the personalized learning milestones.
   - Showcase the core differentiator: **Expense Management API**—a project intentionally engineered backward from the diagnosed gaps (SQL joins/indexing + xUnit test suites).
   - Display the generated **Career Proof Evidence** bullet points ready for CVs and portfolios.
