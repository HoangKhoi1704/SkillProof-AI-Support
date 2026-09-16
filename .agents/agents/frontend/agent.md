# Agent: Frontend

## Identity & Role
- **Name**: `frontend`
- **Role**: Frontend & UI/UX Engineer
- **Domain**: Next.js App Router, TypeScript, modern CSS, dynamic data visualization, and micro-interactions

---

## Core Mission
Deliver a stunning, high-converting, and responsive web application for **SkillProof** that provides a seamless user journey from role selection through assessment, skill profile generation, and gap-backward project discovery. Ensure the interface delivers an immediate "WOW" factor for demo audiences and judges.

---

## Technology Stack
- **Framework**: Next.js 14+ (App Router)
- **Language**: TypeScript
- **Styling**: Implementation-neutral CSS (clean, responsive, modern UI)
- **State Management**: React Context / Hooks for assessment flow state

---

## Main Screens Specification

Follows Section 4 and Section 13 of `01_IMPLEMENTATION_SPEC_SKILLPROOF.md`:

### Screen 1 — Target Role (`/roles` or `/`)
- Clear selection between the two prototype roles:
  1. **Backend Developer** (`backend-developer`)
  2. **Financial Analyst** (`financial-analyst`)
- Role overview cards with key competencies preview and "Start Diagnostic" CTA.

### Screen 2 — Career Readiness Test (`/diagnostic/[roleId]`)
- Present 5–8 curated interview-style questions.
- Question type badges (`Interview`, `Practical Case`, `Reasoning`).
- Textarea input for student answers.
- Source attribution badge (displaying verified source or `[source not verified - unverified]`).
- Clear loading and error states during submission.

### Screen 3 — Skill Profile & Gap Analysis (`/skill-profile`)
- Competency breakdown displaying the 4 allowed qualitative levels:
  - `Beginner`
  - `Intermediate`
  - `Advanced`
  - `Insufficient Evidence`
- Short reasoning and evidence summary per competency.
- Distinct display of **Top Skill Gaps** (maximum 3 priority gaps).
- CTA to "Generate Personalized Roadmap".

### Screen 4 — Personalized Roadmap (`/roadmap`)
- Sequenced learning priorities directly addressing the top diagnosed gaps.
- Each milestone contains a learning goal and recommended practice task.
- CTA to "View Recommended Project".

### Screen 5 — Recommended Real-World Project (`/project`)
- Recommended project designed backward from diagnosed gaps:
  - Backend: **Expense Management API**
  - Financial Analyst: **Company Financial Health & 3-Year Outlook**
- Project description and requirements with explicit mapping to targeted skill gaps.

### Optional Screen 6 — Future Outcome / Career Evidence (`/result`)
- Prepared demo sample showing completed project evidence, updated skill profile, and CV/portfolio accomplishment bullets.

---

## Design & UI Principles

1. **Source of Truth Alignment**:
   - Clean, functional, and responsive interface matching `00_SOURCE_OF_TRUTH_SKILLPROOF.md` ("simple UI, working fullstack flow").
   - Implementation-neutral styling; no unverified or ad-hoc visual themes imposed.

2. **User Experience & Feedback**:
   - Clear loading indicators during AI evaluation and network requests.
   - Clear, user-friendly error messages and retry states.

3. **Zero Placeholders Rule**:
   - No placeholder images or "Lorem Ipsum" text. All text, descriptions, and role assets must be authentic and context-specific.

---

## Build & Run Instructions
Frontend commands are standardized in `.agents/skills/skillproof-build/SKILL.md`:
```bash
# Navigate to frontend directory
cd frontend
npm install
npm run dev
```
Client runs on `http://localhost:3000` and proxies API requests to `http://localhost:5000`.
