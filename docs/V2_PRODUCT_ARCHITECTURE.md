# SkillProof V2.0 — Approved Product & Target Architecture

> **Document Type**: Source of Truth & Target Architecture  
> **Status**: APPROVED V2 SPECIFICATION  
> **Milestone**: V2.0 Data & Provenance Audit  
> **Classification Key**:
> - `[CURRENT]`: Implemented and active in current codebase.
> - `[TRANSITION]`: In progress or bridging V1 to V2 foundations.
> - `[V2 TARGET]`: Approved product specification for subsequent V2 milestones.

---

## 1. Executive Vision & Core Value Proposition

SkillProof evaluates and validates career readiness for software engineers and data analysts through evidence-backed diagnostic assessments, canonical learning paths, curated project practice, and verified portfolio proof. 

SkillProof rejects arbitrary percentage scores, model-hallucinated citations, unverified company interview claims, and fabricated resume bullet points.

---

## 2. Status Classification Matrix

| Subsystem / Feature Area | State | Architectural Note |
|---|---|---|
| **Backend Developer Catalog** | `[CURRENT]` | 48 calibrated questions across 16 skills (6 Core, 2 Recommended, 8 Languages) backed by SQLite and JSON source of truth. |
| **Strict Provenance Gate** | `[CURRENT]` | 42 audited sources. VerificationStatus and AccessStatus enforced. Zero 404 links. |
| **Frontend Developer Role & Data** | `[V2 TARGET]` | Planned for V2.1+ using canonical `roadmap.sh/frontend`. |
| **Data Analyst Role & Data** | `[V2 TARGET]` | Planned for V2.1+ using canonical `roadmap.sh/data-analyst`. |
| **Assessment Selection (0 Preselected)** | `[V2 TARGET]` | Replaces Recommended vs Customize UX with clean, manual selection. |
| **Mandatory Fundamentals** | `[V2 TARGET]` | Core role fundamentals (OOP, SOLID, basic DSA, Big-O) assessed alongside applied cases. |
| **Post-Answer Explanations** | `[V2 TARGET]` | Immediate feedback after submission without exposing scoring rubrics. |
| **Multi-Page Navigation (30-min TTL)** | `[V2 TARGET]` | Sub-page navigation with browser/session state persistence surviving reloads. |
| **Visual Skill Matrix Profile** | `[V2 TARGET]` | Visual dimensions (Fundamentals, Applied, Reasoning) with qualitative tiers. |
| **Canonical Roadmap.sh Graph** | `[V2 TARGET]` | Personalized roadmap derived directly as a subgraph of canonical roadmap.sh role graph. |
| **Curated Practice Project Catalog** | `[V2 TARGET]` | Matched from approved catalog (e.g. `practical-tutorials/project-based-learning`), retiring runtime LLM generation. |
| **Multi-Channel Project Evaluation** | `[V2 TARGET]` | Deterministic build/test checks + browser/deployed behavior + AI semantic review. |
| **Verified Portfolio / CV Evidence Gate** | `[TRANSITION]` | Gates CV claims strictly behind demonstrated evidence; blocks fabricated metrics. |

---

## 3. Demo Roles & Canonical Role Framework

### Primary Demo Roles `[V2 TARGET]`
SkillProof V2 anchors on three primary career paths:
1. **Frontend Developer** (`frontend-developer`)
2. **Backend Developer** (`backend-developer`)
3. **Data Analyst** (`data-analyst`)

### Canonical Role Sources `[V2 TARGET]`
The foundational skill requirements and prerequisite learning topologies are strictly derived from official roadmap.sh guides:
- `https://roadmap.sh/frontend`
- `https://roadmap.sh/backend`
- `https://roadmap.sh/data-analyst`

### Framework Invariants `[FROZEN]`
- **No AI Invention of Roles/Skills**: The AI must never invent canonical role requirements or roadmap nodes. All canonical nodes must trace to real roadmap.sh elements. Internal slugs may be normalized.
- **Questions Map INTO Canonical Skills**: Available interview questions map into the canonical roadmap graph. The canonical framework must never mutate itself to accommodate arbitrary questions.
- **Role Extraction Belongs to V2.1**: Extraction and graph modeling of the roadmap.sh dataset is scheduled for Milestone V2.1.

---

## 4. Assessment UX & Selection Mechanics

### Skill Selection `[V2 TARGET]`
- **Zero Preselection**: Users begin with zero skills selected. No pre-ticked checkboxes or forced paths.
- **Elimination of "Recommended vs Customize"**: The legacy binary distinction is removed in favor of a single unified selection screen.
- **Minimal UI**: Selection interface highlights skill names and recognizable icons/logos, avoiding dense text paragraphs.

### Mandatory Fundamentals `[V2 TARGET]`
- Regardless of user skill selections, core role fundamentals must be assessed to ensure genuine engineering readiness:
  - **Developer Roles**: OOP concepts, SOLID principles, basic data structures (Arrays, Hash Tables, Lists, Trees), basic algorithms and Big-O complexity, runtime memory management (stack vs heap, GC, RAII, pointers/references).
  - **Data Analyst Role**: SQL querying, data cleaning, statistical fundamentals, basic Python/R, data visualization principles.
- Assessment coverage balances definitions, concepts, practical scenarios, debugging, design trade-offs, and code comprehension.

### Assessment Semantics `[FROZEN]`
- **Qualitative Levels Only**:
  - `Insufficient Evidence`: Answer is empty, evasive, irrelevant, or fails to address the core problem.
  - `Beginner`: Memorized definitions, basic syntax, but lacks depth or understanding of edge cases and internal mechanics.
  - `Intermediate`: Practical, production-ready solution explaining the "why" and trade-offs.
  - `Advanced`: Architectural depth, concurrency/memory trade-offs, edge case handling, and internal engine mechanics.
- **Not Assessed Is Distinct**: Skills not selected or evaluated remain strictly in the state `Not Assessed`. Under no circumstances may `Not Assessed` silently become `Beginner`, `failed`, `0`, or `0%`. Lack of assessment evidence is not evidence of lack of skill.

---

## 5. Post-Answer Explanations `[V2 TARGET]`

Immediately after a candidate submits an answer, the interface displays an informative feedback card:
- **What the Answer Covered**: Valid insights and recognized components from the submission.
- **What Could Be Stronger**: Gaps, unaddressed trade-offs, edge cases, or performance considerations.
- **Reference Explanation**: A concise, authoritative overview of the concept.
- **Privacy Barrier**: System prompts, private rubric descriptions, and `expectedSignals` remain internal server-side scoring guidance and are never displayed in the UI.

---

## 6. Multi-Page Navigation & State Persistence `[V2 TARGET]`

### Multi-Page Experience
The linear single-page flow transitions to a structured sub-page routing architecture:
```text
Role Selection
  → Skill Selection
  → Diagnostic Interview (Multi-turn)
  → Post-Answer Explanations
  → Career Readiness Profile & Skill Matrix
  → Personalized Learning Roadmap
  → Practice Projects (Curated Catalog)
  → Portfolio Project
  → Project Evaluation (Repo + Web + Semantic)
  → Portfolio / CV Evidence Proof
```

### State Persistence Requirements
- Smooth Forward and Back navigation without losing candidate inputs or diagnostic progress.
- Transient state survives page reloads and tab navigation for approximately **30 minutes** (via browser storage / session abstraction).
- Architecture designed to transition seamlessly to database-backed persistence without requiring frontend redesign.

---

## 7. Visual Skill Matrix Profile `[V2 TARGET]`

The Career Readiness Profile evolves from a text-heavy list into an intuitive **Visual Skill Matrix**:
- **Dimensions**: Evaluates skills along practical engineering axes:
  - *Fundamentals* (Concepts, OOP, Data Structures, Complexity)
  - *Applied* (Implementation, Debugging, API contracts, Frameworks)
  - *Reasoning* (Trade-offs, Architecture, Concurrency, Scaling)
- **Visual Clarity**: Color-coded qualitative bands with clear distinction for `Not Assessed`.
- Detailed rubric rationales and feedback expand on user interaction rather than cluttering the initial overview.

---

## 8. Personalized Roadmap Architecture `[V2 TARGET]`

### Canonical Derivation Flow
```text
CANONICAL ROADMAP.SH ROLE GRAPH
              +
CANDIDATE SKILL PROFILE (Demonstrated Levels)
              +
UNASSESSED ROLE REQUIREMENTS (Not Assessed)
              ↓
PERSONALIZED ROADMAP SUBGRAPH / PATH
```

### Unassessed Skills Representation
- Canonical role competencies that were not assessed are explicitly included in the personalized learning path with status `Not Assessed`.
- Roadmap notes indicate: *"Required for role; not evaluated in diagnostic; recommended for study/validation."*
- SkillProof provides a complete career curriculum without fabricating diagnostic judgments.

### Visual Presentation (Snake-and-Ladder Metaphor)
- Graph/path-based navigation representing milestones.
- **Ladders**: Represent demonstrated prerequisite competence allowing candidates to bypass introductory modules.
- **No Punitive "Snakes"**: The metaphor represents progression paths, not arbitrary penalties for diagnostic gaps.
- Node states: `Completed`, `Current`, `Available`, `Locked`, `Needs Development`, `Not Assessed`, `Optional`.

### Learning Resources Policy
- **YouTube Integration (Optional)**: If integrated, resources must strictly originate from verified channel/API catalogs. The AI must **never** hallucinate video titles, channel names, or video URLs. If reliable automated retrieval is unavailable, omit video links.

---

## 9. Career Toolkit `[V2 TARGET]`

Roadmap includes practical supporting engineering skills:
- Version Control: Git, GitHub workflows, PR reviews, documentation.
- AI-Assisted Workflows: Practical usage of developer CLI tools (e.g. Claude Code) where appropriate.
- Deployment & Cloud: Vercel, Supabase, Railway, Docker, CI/CD pipelines.
- Toolkit nodes are clearly classified as supporting tooling rather than assessed computer science competencies.

---

## 10. Practice & Portfolio Projects `[V2 TARGET]`

### Practice Projects (Curated Catalog)
- **Retirement of OpenAiProjectRecommender**: Runtime AI generation of project specs is being phased out.
- **Curated Catalog**: Practice projects are matched from an audited catalog (such as `practical-tutorials/project-based-learning`).
- **AI Role**: The AI matches, ranks, and explains why an approved catalog project addresses diagnosed skill gaps; it does not fabricate project specifications.

### Portfolio Projects
- Substantive, full-scope portfolio projects curated for demonstration of end-to-end engineering readiness.
- Separate from short practice exercises.

---

## 11. Multi-Channel Project Evaluation `[V2 TARGET]`

```text
PROJECT SUBMISSION
    ├── Git Repository URL
    ├── Live Deployed URL (where applicable)
    └── Optional Candidate Design Notes
            ↓
AUTOMATED / DETERMINISTIC VERIFICATION
  - Repository structure, package manifests, build scripts
  - Automated test execution (dotnet test, npm test, pytest)
            ↓
FUNCTIONAL / BROWSER VERIFICATION
  - HTTP status, responsive loading, interactive form flows, API checks
            ↓
AI SEMANTIC EVIDENCE REVIEW
  - Code architecture review against approved project criteria
            ↓
QUALITATIVE EVALUATION (Demonstrated / Partially Demonstrated / Insufficient Evidence)
```

### Evaluation Semantics
- Evaluated as `Demonstrated`, `Partially Demonstrated`, or `Insufficient Evidence`.
- Project evaluation does not mutate diagnostic skill levels without verified evidence.
- Data Analyst submissions accommodate SQL queries, Python notebooks, dashboards (Streamlit/BI), and data documentation without mandating traditional web deployments.

---

## 12. Verified Portfolio & CV Evidence Gate `[FROZEN]`

- **Evidence-Gated Claims**: Only skills evaluated as `Demonstrated` generate strong positive CV bullet points.
- `Partially Demonstrated` produces cautious learning notes.
- `Insufficient Evidence` or `Not Assessed` generates **zero** positive resume claims.
- **Strict Prohibition on Fabricated Metrics**: Never invent performance percentages (e.g. "improved throughput by 42%"), user counts, revenue metrics, or fake test statistics. Bullet points reflect verifiable implementation evidence.

---

## 13. Data & Provenance Invariants `[FROZEN]`

1. **Source Existence != Claim Verification**: A source ID in `sources.json` does not prove that the underlying content supports an attributed claim.
2. **Strict Provenance Gate**: Every claim-verified source requires accessible content, a documented locator, and a valid `verifiedAt` audit date.
3. **Discovery vs Proof**: Search engine snippets and titles are discovery evidence only; they never constitute claim verification.
4. **No Employer Attribution Without Direct Evidence**: Do not claim an employer asks a question without accessible, official interview guidance.
5. **No Model Memory Citations**: AI memory is never cited as evidence.
6. **Backward Compatibility**: Assessment runtime and database schema preserve full integrity across migration milestones.
