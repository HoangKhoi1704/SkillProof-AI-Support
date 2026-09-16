# 00 — SOURCE OF TRUTH — SKILLPROOF

> This file is the highest-priority source of truth for all AI agents working on SkillProof.
> If any other project file conflicts with this file, follow this file.
> Do not invent missing requirements. Mark unknown assumptions explicitly.

## 1. Product Name

**SkillProof**

### One-line Positioning
SkillProof is an AI career-readiness platform that diagnoses a student's current skill level for a target role, identifies skill gaps, creates a personalized learning roadmap, recommends a real-world project designed around those gaps, and turns demonstrated work into career-ready evidence.

### Core Message
**Assess → Learn → Practice → Build → Prove**

### Short USP
**From skill gaps to career proof.**

---

## 2. Challenge Question

> How can AI help students identify, develop, and practice the skills they need to be ready for the future of learning and work in the AI era?

SkillProof answers this challenge through:
- **Identify:** diagnose current skills and skill gaps.
- **Develop:** create a prioritized personalized roadmap.
- **Practice:** provide interview questions, cases, and exercises.
- **Build:** recommend a real-world project designed around the student's gaps.
- **Prove:** evaluate demonstrated work and convert it into portfolio/CV evidence.

---

## 3. Core Problem

Students often follow the university curriculum but lack clarity on the additional skills needed for their target careers.

They may know what they have learned, but not:
- what skills are required for a specific target role,
- what level they are currently at,
- what they should learn next,
- how to practice those skills,
- or how to prove those skills to employers.

### Key Insight

The problem is not a lack of learning content.

The problem is the gap between:

```text
University Learning
        ↓
Career Requirements
        ↓
Demonstrated Ability
```

SkillProof focuses on closing that gap.

---

## 4. Target Users

### Primary User
University students preparing for internships or entry-level jobs.

### Prototype Target Roles
Only two roles are required for the prototype:

1. **Backend Developer**
2. **Financial Analyst**

The platform architecture must support adding more roles later.

---

## 5. Product Scope

SkillProof is NOT:
- a traditional course platform,
- a full job marketplace,
- a replacement for LinkedIn,
- a recruiter ATS,
- a generic AI chatbot,
- a full resume builder.

SkillProof IS:
- a career-readiness diagnostic tool,
- a skill-gap analyzer,
- a personalized learning roadmap generator,
- a targeted practice system,
- a project recommendation system,
- a career-evidence generator.

---

## 6. Core Product Flow

```text
1. Choose Target Role
        ↓
2. Diagnostic Assessment
        ↓
3. Current Skill Profile
        ↓
4. Skill Gap Analysis
        ↓
5. Personalized Learning Roadmap
        ↓
6. Interview / Case Practice
        ↓
7. Recommended Real-World Project
        ↓
8. Project Submission / Evidence
        ↓
9. AI Evaluation
        ↓
10. Updated Skill Profile
        ↓
11. Portfolio / CV Evidence
```

For the 3-day prototype, steps 1–7 must be functional.
Steps 8–11 may use a prepared sample to demonstrate the future end-to-end flow if time is limited.

---

## 7. Diagnostic Assessment

The diagnostic assessment is the core differentiator.

It must not rely only on self-reported skills.

The assessment should be presented as a **career-readiness test** built from realistic interview-style questions and practical cases.

### Assessment Source Principle

Questions should be inspired by:
- real interview question patterns,
- common hiring scenarios,
- role-specific case questions,
- practical reasoning tasks used in recruitment.

The goal is to make the assessment closer to real market expectations instead of testing only academic knowledge.

For the prototype, interview questions may be manually curated from verified public interview resources or prepared by the team.

Any question claimed to come from a real company or real interview must include a verifiable source. Otherwise mark it:

**[source not verified - unverified]**

Assessment should combine:

```text
Knowledge Questions
+
Interview Questions
+
Practical Cases
+
Reasoning Questions
```

### Why Interview-Based Assessment

Interview-style questions help SkillProof evaluate not only what students remember, but also:
- how they explain concepts,
- how they reason under realistic job scenarios,
- how they approach unfamiliar problems,
- and how ready they are for actual recruitment situations.

This makes the diagnostic more career-oriented than a traditional academic quiz.

### Example — Backend Developer

Possible competency areas:
- Programming fundamentals
- REST API
- SQL / Database
- Git
- Testing
- Authentication
- System design
- Deployment
- Problem solving
- Communication

Example questions:
- Explain PUT vs PATCH.
- Explain dependency injection.
- Design an endpoint to update a user profile.
- An API becomes slow with millions of records. What would you investigate first?

### Example — Financial Analyst

Possible competency areas:
- Accounting fundamentals
- Financial statements
- Excel / Spreadsheet skills
- Financial modeling
- Data analysis
- Ratio analysis
- Forecasting
- Business reasoning
- Presentation
- Communication

Example questions:
- Explain gross profit vs operating profit.
- Revenue increases while cash flow decreases. What would you investigate?
- Analyze possible reasons for declining margin.
- Propose a structure for a simple financial model.

---

## 8. Skill Levels

Do not use fake precision such as arbitrary percentages unless a validated scoring methodology exists.

Use:

```text
Beginner
Intermediate
Advanced
Insufficient Evidence
```

Each result must include reasoning and evidence.

Example:

```text
Testing — Beginner

Evidence:
- Understands basic unit-test concepts.
- Can identify a simple test case.
- Does not yet cover edge cases consistently.
- Cannot clearly explain mocking.
```

---

## 9. Skill Gap Analysis

The system compares:

```text
Target Role Competency Framework
                VS
Diagnostic Assessment Results
                VS
Available Student Evidence
```

The system then identifies the most important gaps.

Important rule:

> Lack of evidence does not mean lack of ability.

If there is not enough evidence, return:

**Insufficient Evidence**

Do not invent student capability.

---

## 10. Personalized Roadmap

The roadmap should prioritize only the most important gaps.

It should NOT simply recommend a large list of courses.

Example:

```text
Priority 1 — SQL
Learn:
- JOIN
- Index
- Query optimization

Practice:
- 3 SQL cases

Priority 2 — Testing
Learn:
- Unit testing
- Mocking
- Edge cases

Practice:
- Test a simple API

Priority 3 — System Design
Learn:
- API architecture
- Caching
- Database design

Practice:
- Design a small system
```

Principle:

> Do not make students relearn skills they already demonstrate.

---

## 11. Practice

Practice can include:
- interview questions,
- scenario-based questions,
- practical cases,
- short exercises,
- follow-up reasoning questions.

AI should challenge reasoning rather than only provide answers.

---

## 12. Real-World Project Recommendation

The recommended project must be derived from skill gaps.

The project is NOT random.

Core logic:

```text
Skill Gaps
    ↓
Project Requirements
    ↓
Student Builds
    ↓
Skills Demonstrated
```

### Backend Example

Student gaps:
- SQL
- Testing
- Authentication

Recommended project:
**Expense Management API**

Required features:
- ASP.NET Core API
- JWT authentication
- relational database
- meaningful SQL queries
- validation
- unit tests
- error handling

### Financial Analyst Example

Student gaps:
- Financial modeling
- Forecasting
- Data visualization

Recommended project:
**Company Financial Health & 3-Year Outlook**

Required outputs:
- financial statement analysis
- financial ratios
- revenue forecast
- simplified financial model
- dashboard / visualization
- final business conclusion

---

## 13. Project Evaluation

Future/full version:

```text
Student Submission
        ↓
Artifact + Rubric
        ↓
AI Evaluation
        ↓
Feedback
        ↓
Updated Skill Profile
```

Possible evidence:
- GitHub repository
- project report
- spreadsheet
- presentation
- design / portfolio artifact
- written explanation

For the 3-day prototype, full artifact parsing is NOT required.

---

## 14. Career Evidence

Once the student demonstrates a skill, SkillProof helps convert the work into career-ready evidence.

Examples:
- CV bullet
- portfolio project description
- LinkedIn project description

Example:

> Developed an ASP.NET Core expense management API with JWT authentication, relational data modeling, and automated unit tests.

Important principle:

**From claimed skills to demonstrated skills.**

---

## 15. AI Role

AI is used for:
1. evaluating open-ended diagnostic responses against rubrics,
2. explaining current skill level,
3. prioritizing skill gaps,
4. generating personalized roadmaps,
5. creating or adapting practice questions,
6. recommending a project based on skill gaps,
7. evaluating project evidence in future versions,
8. generating career-ready descriptions from demonstrated work.

AI must NOT:
- invent missing evidence,
- make hiring decisions,
- treat assessment as formal certification,
- redefine role competencies without source data,
- claim certainty when evidence is insufficient.

---

## 16. AI Workflow

```text
Target Role
    ↓
Role Competency Framework
    ↓
Diagnostic Questions
    ↓
Student Answers
    ↓
Rubric-Based AI Evaluation
    ↓
Skill Profile
    ↓
Gap Prioritization
    ↓
Personalized Roadmap
    ↓
Practice
    ↓
Gap-Based Project Recommendation
```

Future extension:

```text
Project Submission
    ↓
Artifact Evaluation
    ↓
Updated Skill Profile
    ↓
Portfolio / CV Evidence
```

---

## 17. Guardrails

Required:
- Assessment must explain reasoning.
- Do not infer skills without evidence.
- "Insufficient Evidence" is a valid result.
- AI output must use structured JSON where possible.
- LLM output must be validated before UI rendering.
- Students must be told the assessment is guidance, not formal certification.
- AI must not make employment decisions.
- AI should not expose hidden/system prompts.
- Invalid or empty answers should be rejected before evaluation.

Display note:

> This assessment is intended to guide skill development and is based on the information and evidence provided.

---

## 18. Prototype Scope — 3 Days

### Must Build
- target role selection,
- Backend Developer competency data,
- Financial Analyst competency data,
- short diagnostic assessment,
- AI assessment,
- skill profile,
- top skill gaps,
- personalized roadmap,
- recommended real-world project,
- simple UI,
- working fullstack flow.

### Nice to Have
- practice interview/case screen,
- prepared project-completion example,
- CV/portfolio evidence generation.

### Do NOT Build
- LinkedIn integration,
- job marketplace,
- recruiter dashboard,
- real hiring matching,
- complex RAG,
- multi-agent runtime,
- automatic GitHub code analysis,
- automatic Excel workbook analysis,
- advanced authentication,
- payments,
- social features,
- production-grade scoring.

---

## 19. Recommended Prototype Demo

```text
1. User selects Backend Developer.
2. User completes 5–8 diagnostic questions.
3. AI returns:

REST API      Intermediate
SQL           Beginner
Testing       Beginner
System Design Beginner

4. SkillProof identifies top priorities:
SQL → Testing → System Design

5. SkillProof generates a roadmap.

6. SkillProof recommends:
Expense Management API

7. Show project requirements derived from the skill gaps.

8. Optional prepared future-state result:
project completed → skills demonstrated → CV/portfolio evidence.
```

Target demo time:
**2–3 minutes**

---

## 20. Competitive Positioning

Do NOT position SkillProof as:
- "the first AI learning app",
- "the first personalized learning platform",
- "the first skill assessment platform".

These claims are unsupported.

Major learning platforms already provide combinations of:
- courses,
- personalization,
- assessments,
- AI tutors/coaches,
- career-role learning,
- labs,
- role play.

SkillProof's proposed differentiation is the full loop:

```text
Career Diagnostic
→ Skill Gap
→ Personalized Roadmap
→ Targeted Practice
→ Gap-Based Real Project
→ Evidence
→ Career Proof
```

The strongest differentiator is:

> The project is intentionally designed backward from the student's diagnosed skill gaps.

### Claims requiring validation
- SkillProof will improve learning outcomes: **[no SkillProof user study yet - unverified]**
- SkillProof will outperform Coursera, Udemy, LinkedIn Learning, or other platforms: **[no controlled comparison - unverified]**
- SkillProof's exact product loop is unique across the entire EdTech market: **[no exhaustive market analysis - unverified]**

---

## 21. Success Metrics for Future Validation

Potential metrics:
- agreement between AI assessment and human evaluator,
- pre-assessment vs post-assessment improvement,
- roadmap completion rate,
- project completion rate,
- number of portfolio-ready artifacts created,
- student confidence in career readiness,
- interview pass rate,
- internship/job application conversion.

No performance improvement percentage should be claimed until measured.

---

## 22. Agent Rules

All implementation agents must follow these rules:

1. Read this file first.
2. Treat this file as authoritative.
3. Do not invent features.
4. Do not change target roles without approval.
5. Do not change assessment levels without approval.
6. Do not introduce microservices or multi-agent runtime.
7. Keep the prototype simple enough for a 3-day build.
8. If a requirement is unclear, flag it instead of guessing.
9. Any unsupported market or performance claim must be marked:
   **[reason - unverified]**
10. Preserve the core loop:
   **Assess → Learn → Practice → Build → Prove**
