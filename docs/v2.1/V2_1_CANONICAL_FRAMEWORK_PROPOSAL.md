# SkillProof V2.1A — Canonical Role Framework Proposal

**Milestone**: V2.1A (Research, Extraction, Normalization & Framework Proposal)  
**Status**: DRAFT FOR HUMAN REVIEW (Non-Production Design Checkpoint)  
**Date**: 2026-09-16  
**Auditor**: Antigravity Autonomous Agent (Fast Execution + Source-of-Truth + No-Hallucination Mode)  

---

## 1. Executive Summary

This document proposes the canonical role-framework taxonomy for SkillProof V2, covering the three primary demo roles:
1. **Frontend Developer**
2. **Backend Developer**
3. **Data Analyst**

Every canonical learning topic and requirement is derived directly from the official **roadmap.sh** role roadmaps. This proposal establishes a stable, normalized identifier namespace that will anchor:
- Manual Skill Selection (starting at 0 preselected skills)
- Diagnostic Question Bank Mapping
- Skill Matrix & Evidence-Based Profile Generation
- Personalized Gap-Based Learning Roadmaps (subgraph derivation)
- Curated Practice Projects & Portfolio Projects

> [!IMPORTANT]
> **Human Approval Gate**: This document is a **design proposal only**. It does **NOT** modify production runtime, database tables, question baselines, or API contracts. Human approval of this taxonomy is required before Milestone V2.1B (Data V3 implementation).

---

## 2. Source Provenance

Extraction was conducted against official roadmap.sh assets using verifiable source locators:

| Role | Canonical Web URL | GitHub Source Path | Repository & Commit SHA | Extraction Date | Total Raw Topics |
|---|---|---|---|---|---|
| **Frontend Developer** | `https://roadmap.sh/frontend` | `roadmaps/frontend/content/` | [nilbuild/developer-roadmap](https://github.com/nilbuild/developer-roadmap) @ `3cba37fa7440b2f8fafb234561c9e48f85ee1c2f` | 2026-09-16 | **121** |
| **Backend Developer** | `https://roadmap.sh/backend` | `roadmaps/backend/content/` | [nilbuild/developer-roadmap](https://github.com/nilbuild/developer-roadmap) @ `3cba37fa7440b2f8fafb234561c9e48f85ee1c2f` | 2026-09-16 | **156** |
| **Data Analyst** | `https://roadmap.sh/data-analyst` | `roadmaps/data-analyst/content/` | [nilbuild/developer-roadmap](https://github.com/nilbuild/developer-roadmap) @ `3cba37fa7440b2f8fafb234561c9e48f85ee1c2f` | 2026-09-16 | **102** |

Raw snapshots preserving all 379 verbatim topic titles, filenames, and node IDs are archived in:
- [roadmap-frontend-raw.md](file:///d:/Roy/SkillProof-Support-AI/docs/v2.1/roadmap-frontend-raw.md)
- [roadmap-backend-raw.md](file:///d:/Roy/SkillProof-Support-AI/docs/v2.1/roadmap-backend-raw.md)
- [roadmap-data-analyst-raw.md](file:///d:/Roy/SkillProof-Support-AI/docs/v2.1/roadmap-data-analyst-raw.md)

---

## 3. Canonical Identity & Namespace Strategy

To avoid fragmentation across subsystems while preserving role-specific topology, SkillProof V2 adopts a **Hybrid Hierarchical Identity System**:

1. **Global Shared Concepts (`shared.<concept>`)**:
   Foundational technologies and protocols that exist identically across multiple roles:
   - `shared.internet-http` (Internet fundamentals, HTTP/HTTPS, DNS)
   - `shared.git` (Git version control, branching, PR workflows)
   - `shared.sql` (Relational querying, JOINs, aggregations, schema design)
   - `shared.python` (Python language syntax and core data structures)
   - `shared.javascript` (JavaScript core language semantics)
   - `shared.web-security-owasp` (OWASP security risks, CORS, HTTPS)
   - `shared.docker` (Container fundamentals)

2. **Role-Scoped Specializations (`<role>.<competency>`)**:
   Competencies that are either role-unique or have distinct depth/application requirements:
   - `frontend.html-css` vs `backend.frontend-basics`
   - `frontend.react` / `frontend.state-management`
   - `backend.distributed-caching` / `backend.message-brokers` / `backend.acid-transactions`
   - `data-analyst.eda` / `data-analyst.spreadsheets` / `data-analyst.bi-dashboards`

3. **Assessment Policy Extensions (`assessment-ext.<competency>`)**:
   Explicitly demarcated competencies that SkillProof adds for interview diagnostic quality (e.g. theoretical complexity, architectural patterns) when not isolated as a single roadmap.sh leaf node.

---

## 4. Node Classification Typology

Not every roadmap.sh node should be a separate 3-question interview skill. SkillProof classifies each roadmap node into one of seven distinct functional types:

1. **Assessable Competency**: A cohesive skill unit evaluated through diagnostic interview questions (e.g., SQL, REST APIs, Asynchronous Programming).
2. **Programming Language**: A concrete language choice evaluated when selected by candidate (e.g., Python, C#, TypeScript, Go, R).
3. **Framework / Library**: A concrete framework ecosystem (e.g., React, ASP.NET Core, Spring, Pandas, FastAPI).
4. **Technology / Infrastructure Tool**: Operational systems (e.g., PostgreSQL, Redis, Kafka, Docker, Tableau).
5. **Learning / Conceptual Topic**: Detailed roadmap concepts that inform learning paths but are sub-elements of an assessable competency (e.g., HTTP Caching, BCD vs Sliding Window, Normalization).
6. **Career Toolkit**: Essential developer tools and modern workflows that are learned and tracked but not graded as oral diagnostic interview questions (e.g., GitHub PRs, Claude Code CLI, Google Antigravity, Linters/Prettier).
7. **Optional / Advanced Specialization**: Explicitly elective branches (e.g., Deep Learning / Neural Networks for Data Analysts, WebAssembly, LXC).

---

## 5. Frontend Developer Framework Proposal

### 5.1 Overview
Derived from `https://roadmap.sh/frontend` (121 raw items).

| Canonical ID | Display Name | Roadmap.sh Locator | Node Typology | Requirement Layer | Assessment Eligible? | Mandatory Fundamental? |
|---|---|---|---|---|---|---|
| `shared.internet-http` | Internet & HTTP Fundamentals | `how-does-the-internet-work`, `what-is-http`, `dns-and-how-it-works` | Conceptual Topic | Role Core | YES | YES |
| `frontend.html-core` | HTML & Semantic Markup | `html`, `html-templates`, `accessibility` | Assessable Competency | Role Core | YES | YES |
| `frontend.css-styling` | CSS Architecture & Layout | `css`, `css-frameworks`, `tailwind` | Assessable Competency | Role Core | YES | NO (Elective) |
| `shared.javascript` | JavaScript Fundamentals & Asynchrony | `javascript`, `web-apis` | Programming Language | Role Core | YES | YES |
| `frontend.typescript` | TypeScript & Type Systems | `type-checkers` | Programming Language | Recommended | YES | NO (Choice) |
| `frontend.framework-react` | React Ecosystem | `react`, `react-router` | Framework / Library | Role Core (Choice) | YES | NO (Choice) |
| `frontend.framework-vue` | Vue.js Ecosystem | `vuejs` | Framework / Library | Role Core (Choice) | YES | NO (Choice) |
| `frontend.framework-angular` | Angular Ecosystem | `angular` | Framework / Library | Role Core (Choice) | YES | NO (Choice) |
| `frontend.framework-svelte` | Svelte Ecosystem | `svelte`, `sveltekit` | Framework / Library | Role Core (Choice) | YES | NO (Choice) |
| `frontend.build-tooling` | Build Tools & Bundlers | `module-bundlers`, `vite`, `webpack`, `swc` | Technology / Tool | Toolkit | NO | NO |
| `frontend.package-managers` | Package Managers | `package-managers`, `npm`, `pnpm`, `yarn` | Technology / Tool | Toolkit | NO | NO |
| `shared.git` | Version Control (Git & GitHub) | `version-control`, `git`, `github` | Technology / Tool | Toolkit | NO | NO |
| `frontend.testing` | Frontend Testing & Test Runners | `testing`, `jest`, `vitest`, `playwright`, `cypress` | Assessable Competency | Recommended | YES | NO |
| `frontend.web-security` | Web Security (CORS, CSP, Auth) | `web-security`, `cors`, `csp`, `auth-strategies`, `owasp-risks` | Assessable Competency | Role Core | YES | YES |
| `frontend.performance` | Web Performance & Vitals | `performance`, `lighthouse` | Assessable Competency | Recommended | YES | NO |
| `frontend.meta-frameworks` | SSR, SSG & Meta-Frameworks | `nextjs`, `remix`, `astro`, `ssr`, `ssg` | Framework / Library | Recommended | YES | NO |
| `toolkit.ai-coding-frontend` | AI-Assisted Development (Claude Code / Antigravity) | `ai-assisted-coding`, `claude-code`, `antigravity`, `cursor` | Career Toolkit | Toolkit | NO | NO |
| `frontend.mobile-desktop` | Cross-Platform (React Native, Electron) | `mobile-apps`, `desktop-apps`, `electron`, `tauri` | Framework / Library | Optional / Advanced | NO | NO |

### 5.2 Proposed Mandatory Fundamentals (Frontend)
1. **`shared.internet-http`**: Understanding client-server request/response, headers, status codes, and browser rendering pipeline is mandatory for all frontend practitioners.
2. **`frontend.html-core`**: Semantic HTML, accessibility (ARIA), and DOM hierarchy.
3. **`shared.javascript`**: Event loop, closures, promises/async-await, prototype chain, and DOM API.
4. **`frontend.web-security`**: Browser security boundary, CORS policies, XSS prevention, and token/cookie authentication handling.

---

## 6. Backend Developer Framework Proposal

### 6.1 Overview
Derived from `https://roadmap.sh/backend` (156 raw items) and mapped against existing SkillProof V2.0 catalog.

| Canonical ID | Display Name | Roadmap.sh Locator | Node Typology | Requirement Layer | Assessment Eligible? | Mandatory Fundamental? |
|---|---|---|---|---|---|---|
| `shared.internet-http` | Internet & Protocol Fundamentals | `how-does-the-internet-work`, `what-is-http`, `dns-and-how-it-works` | Conceptual Topic | Role Core | YES | YES |
| `backend.language-selection` | Backend Programming Language | `pick-a-backend-language` (C#, Java, Python, Go, Rust, Node, C++) | Programming Language | Role Core (Choice) | YES | YES (Pick 1) |
| `assessment-ext.programming-fundamentals` | Data Structures & Algorithms | `roadmap.sh/datastructures-and-algorithms` | Policy Extension | Role Core | YES | YES |
| `backend.rest-apis` | REST APIs & Interface Contracts | `rest`, `json-apis`, `open-api-specs` | Assessable Competency | Role Core | YES | YES |
| `backend.relational-databases` | Relational Databases & SQL | `relational-databases`, `postgresql`, `mysql`, `database-indexes`, `acid` | Assessable Competency | Role Core | YES | YES |
| `backend.testing` | Testing Practices (Unit, Integration) | `testing`, `unit-testing`, `integration-testing` | Assessable Competency | Role Core | YES | YES |
| `backend.authentication-security` | Authentication, Authorization & Security | `authentication`, `jwt`, `oauth`, `web-security`, `owasp-risks` | Assessable Competency | Role Core | YES | YES |
| `backend.system-design` | Architecture & Distributed System Design | `architectural-patterns`, `building-for-scale`, `microservices`, `cap-theorem` | Assessable Competency | Role Core | YES | YES |
| `backend.caching` | Caching Strategies & Stores | `caching`, `redis`, `memcached`, `http-caching` | Assessable Competency | Recommended | YES | NO |
| `backend.nosql-databases` | NoSQL & Specialized Data Stores | `nosql-databases`, `mongodb`, `cassandra`, `dynamodb` | Assessable Competency | Recommended | YES | NO |
| `backend.message-brokers` | Asynchronous Messaging & Event Streams | `message-brokers`, `rabbitmq`, `kafka` | Assessable Competency | Optional | YES | NO |
| `backend.resilience-patterns` | Resilient Distributed Systems | `throttling`, `backpressure`, `circuit-breaker`, `graceful-degradation` | Assessable Competency | Optional | YES | NO |
| `backend.observability` | Observability, Telemetry & Logging | `observability`, `monitoring`, `telemetry`, `instrumentation` | Assessable Competency | Optional | YES | NO |
| `shared.git` | Version Control (Git & GitHub) | `version-control-systems`, `git`, `github` | Technology / Tool | Toolkit | NO | NO |
| `shared.docker` | Containerization & Container Tooling | `roadmap.sh/docker`, `lxc` | Technology / Tool | Toolkit | NO | NO |
| `backend.ci-cd` | CI / CD Automation | `ci--cd` | Technology / Tool | Toolkit | NO | NO |
| `toolkit.ai-coding-backend` | AI-Assisted Development (Claude Code / Antigravity) | `ai-assisted-coding`, `claude-code`, `antigravity`, `cursor` | Career Toolkit | Toolkit | NO | NO |
| `backend.ai-integration` | AI Backend Integrations (RAG, Vectors, Tool Calling) | `embeddings`, `vectors`, `rags`, `function-calling`, `structured-outputs` | Conceptual / Advanced | Optional / Advanced | NO | NO |

### 6.2 Mandatory Fundamentals (Backend)
1. **`backend.language-selection`**: Fluency in at least one enterprise backend language.
2. **`assessment-ext.programming-fundamentals`**: O(1) vs O(N) complexities, hash tables, and core algorithms.
3. **`backend.rest-apis`**: HTTP verb semantics, status codes, idempotency, keyset pagination.
4. **`backend.relational-databases`**: SQL joins, ACID transactions, and index mechanics.
5. **`backend.testing`**: Unit testing, mocks/fakes/stubs, assertion practices.
6. **`backend.authentication-security`**: Stateless tokens, hashing, OWASP vulnerabilities.
7. **`backend.system-design`**: Horizontal vs vertical scaling, stateless services, cache stampede.

---

## 7. Data Analyst Framework Proposal

### 7.1 Overview
Derived from `https://roadmap.sh/data-analyst` (102 raw items).

| Canonical ID | Display Name | Roadmap.sh Locator | Node Typology | Requirement Layer | Assessment Eligible? | Mandatory Fundamental? |
|---|---|---|---|---|---|---|
| `data-analyst.foundations` | Core Concepts of Data Analysis | `what-is-data-analytics`, `types-of-data-analytics`, `key-concepts-of-data` | Conceptual Topic | Role Core | YES | YES |
| `data-analyst.spreadsheets` | Excel / Spreadsheets & Data Modeling | `analysis--reporting-with-excel`, `pivot-tables`, `learn-common-functions` | Assessable Competency | Role Core | YES | YES |
| `shared.sql` | SQL Querying & Relational Analysis | `databases`, `data-storage-solutions` | Assessable Competency | Role Core | YES | YES |
| `data-analyst.python-or-r` | Programming for Data Analysis (Python / R) | `learn-a-programming-lang`, `python`, `r` | Programming Language | Role Core (Choice) | YES | YES (Pick 1) |
| `data-analyst.data-wrangling` | Data Wrangling & Manipulation (Pandas/dplyr) | `data-manipulation-libraries`, `pandas`, `dplyr`, `data-cleanup`, `data-transformation` | Assessable Competency | Role Core | YES | YES |
| `data-analyst.eda` | Exploratory Data Analysis (EDA) & Data Quality | `exploration`, `handling-missing-data`, `removing-duplicates`, `finding-outliers` | Assessable Competency | Role Core | YES | YES |
| `data-analyst.statistics` | Applied Statistics & Hypothesis Testing | `statistical-analysis`, `central-tendency`, `dispersion`, `hypothesis-testing`, `correlation-analysis` | Assessable Competency | Role Core | YES | YES |
| `data-analyst.bi-dashboards` | Business Intelligence & Reporting (Tableau / Power BI) | `tableau`, `power-bi`, `charting` | Assessable Competency | Recommended | YES | NO (Choice) |
| `data-analyst.visualization` | Data Visualization Principles & Libraries | `visualisation`, `data-visualisation-libraries`, `matplotlib`, `seaborn`, `ggplot2` | Assessable Competency | Recommended | YES | NO |
| `data-analyst.data-collection` | Data Collection & Ingestion (APIs, Scraping) | `data-collection`, `csv-files`, `apis`, `web-scraping` | Assessable Competency | Recommended | YES | NO |
| `data-analyst.ml-fundamentals` | Machine Learning Fundamentals | `machine-learning`, `supervised-learning`, `unsupervised-learning` | Conceptual / Advanced | Optional / Advanced | NO | NO |
| `data-analyst.big-data` | Big Data Technologies (Spark, Hadoop) | `big-data-concepts`, `big-data-technologies`, `spark`, `hadoop` | Conceptual / Advanced | Optional / Advanced | NO | NO |
| `data-analyst.deep-learning` | Deep Learning & Neural Networks | `deep-learning-optional`, `neural-networks`, `tensorflow`, `pytorch` | Conceptual / Advanced | Optional / Advanced | NO | NO |

### 7.2 Strict Boundary: Data Analyst vs Data Scientist
Roadmap.sh contains nodes for `deep-learning-optional`, `neural-networks`, `tensorflow`, `pytorch`, `cnns`, `rnn`, and `reinforcement-learning`.
**Proposed Invariant**: These advanced modeling nodes are explicitly classified as `Optional / Advanced` and are **NOT** included in the core Data Analyst readiness assessment. This prevents a Data Analyst assessment from silently turning into an advanced Machine Learning / Data Scientist assessment.

### 7.3 Mandatory Fundamentals (Data Analyst)
1. **`data-analyst.spreadsheets`**: Lookup formulas (`VLOOKUP`, `XLOOKUP`), pivot tables, date transformations, data cleanup.
2. **`shared.sql`**: Multi-table aggregations, joins, window functions, and filtering.
3. **`data-analyst.python-or-r`**: Scripting with Python (or R) data structures.
4. **`data-analyst.data-wrangling`**: Filtering, grouping, missing value imputation in Pandas / dplyr.
5. **`data-analyst.statistics`**: Mean, median, standard deviation, p-values, correlation vs causation.
6. **`data-analyst.eda`**: Outlier detection, distribution skew, data validation.

---

## 8. Assessment Policy Extensions

To ensure strict provenance and eliminate hallucination, any assessment competency required for interview evaluation that is **not** an explicit leaf node in roadmap.sh is marked transparently as an **Assessment Policy Extension**:

| Proposed Extension ID | Role | Proposed Competency | Rationale | Suggested Question Formats |
|---|---|---|---|---|
| `assessment-ext.programming-fundamentals` | Backend Developer, Frontend Developer | Algorithms, Complexity & Data Structures | While roadmap.sh has a separate `/datastructures-and-algorithms` roadmap, backend engineers must be tested on O(1) vs O(N) lookups, hash collision strategies, and space/time tradeoffs. | Applied code analysis, knowledge reasoning |
| `assessment-ext.oop-solid` | Backend Developer | Object-Oriented Design & SOLID Principles | Key architectural benchmark across enterprise backend interviews (C#, Java, Python); implied by backend design patterns but not an isolated leaf node. | Scenario design, refactoring review |
| `assessment-ext.data-storytelling` | Data Analyst | Business Context & Data Translation | Ability to translate statistical findings into executive summaries and actionable business decisions; essential for commercial readiness. | Practical case study, reasoning prompt |

---

## 9. Career Toolkit Strategy

Roadmap.sh explicitly includes modern tools, version control, and AI-assisted workflows. SkillProof categorizes these as **Career Toolkit Nodes**—tracked for learning progress, portfolio verification, and candidate profile completeness, but not subjected to 3-question theoretical interview gates:

1. **Roadmap-Derived Toolkit**:
   - `shared.git`: Version control, branching strategies, commit cleanliness.
   - `shared.docker`: Basic container building and running.
   - `backend.ci-cd`: GitHub Actions / pipelines.
   - `frontend.package-managers`: npm / pnpm dependency resolution.
   - `frontend.build-tooling`: Vite / bundler configuration.
   - Modern AI Coding: `claude-code`, `antigravity`, `cursor`, `copilot` (directly extracted from roadmap.sh).

2. **SkillProof Toolkit Extensions**:
   - `toolkit.portfolio-readme`: Professional GitHub portfolio documentation.
   - `toolkit.ide-mastery`: Efficient debugging and environment configuration.

---

## 10. Personalized Roadmap & Graph Topology Invariant

1. **Topological Graph Derivation**:
   Future personalized roadmaps will derive directly from the canonical graph:
   $$\text{Canonical Roadmap Graph} + \text{Assessment State} \longrightarrow \text{Personalized Learning Subgraph}$$

2. **The "Not Assessed" Invariant**:
   If a canonical node is relevant to the candidate's chosen role/specialization but was **not selected or tested** during the diagnostic:
   - Evaluated State = `Not Assessed` (NOT `Beginner`).
   - The node appears in the personalized roadmap as a recommended learning milestone.
   - The system never penalizes the candidate's readiness score or assumes deficiency on unassessed nodes.

---

## 11. Human-Review Approval Tables

### 11.1 Frontend Developer Review Table
| Canonical Item | Source-Backed? | Classification | Assessment Eligible? | Mandatory? | Action Required |
|---|---|---|---|---|---|
| `shared.internet-http` | YES (`roadmap.sh/frontend`) | Conceptual Topic | YES | YES | APPROVE |
| `frontend.html-core` | YES (`roadmap.sh/frontend`) | Assessable Competency | YES | YES | APPROVE |
| `frontend.css-styling` | YES (`roadmap.sh/frontend`) | Assessable Competency | YES | NO | APPROVE |
| `shared.javascript` | YES (`roadmap.sh/frontend`) | Programming Language | YES | YES | APPROVE |
| `frontend.typescript` | YES (`roadmap.sh/frontend`) | Programming Language | YES | NO (Choice) | APPROVE |
| `frontend.framework-react` | YES (`roadmap.sh/frontend`) | Framework | YES | NO (Choice) | APPROVE |
| `frontend.framework-vue` | YES (`roadmap.sh/frontend`) | Framework | YES | NO (Choice) | APPROVE |
| `frontend.framework-angular`| YES (`roadmap.sh/frontend`) | Framework | YES | NO (Choice) | APPROVE |
| `frontend.web-security` | YES (`roadmap.sh/frontend`) | Assessable Competency | YES | YES | APPROVE |
| `frontend.testing` | YES (`roadmap.sh/frontend`) | Assessable Competency | YES | NO | APPROVE |
| `frontend.performance` | YES (`roadmap.sh/frontend`) | Assessable Competency | YES | NO | APPROVE |
| `frontend.meta-frameworks`| YES (`roadmap.sh/frontend`) | Framework | YES | NO | REVIEW |
| `shared.git` | YES (`roadmap.sh/frontend`) | Career Toolkit | NO | NO | APPROVE |
| `toolkit.ai-coding-frontend`| YES (`roadmap.sh/frontend`) | Career Toolkit | NO | NO | APPROVE |

### 11.2 Backend Developer Review Table
| Canonical Item | Source-Backed? | Classification | Assessment Eligible? | Mandatory? | Action Required |
|---|---|---|---|---|---|
| `backend.language-selection`| YES (`roadmap.sh/backend`) | Programming Language | YES | YES (Pick 1) | APPROVE |
| `assessment-ext.programming-fundamentals` | Policy Extension | Policy Extension | YES | YES | POLICY EXTENSION |
| `assessment-ext.oop-solid` | Policy Extension | Policy Extension | YES | NO | POLICY EXTENSION |
| `backend.rest-apis` | YES (`roadmap.sh/backend`) | Assessable Competency | YES | YES | APPROVE |
| `backend.relational-databases` | YES (`roadmap.sh/backend`) | Assessable Competency | YES | YES | APPROVE |
| `backend.testing` | YES (`roadmap.sh/backend`) | Assessable Competency | YES | YES | APPROVE |
| `backend.authentication-security` | YES (`roadmap.sh/backend`) | Assessable Competency | YES | YES | APPROVE |
| `backend.system-design` | YES (`roadmap.sh/backend`) | Assessable Competency | YES | YES | APPROVE |
| `backend.caching` | YES (`roadmap.sh/backend`) | Assessable Competency | YES | NO | APPROVE |
| `backend.nosql-databases` | YES (`roadmap.sh/backend`) | Assessable Competency | YES | NO | APPROVE |
| `backend.message-brokers` | YES (`roadmap.sh/backend`) | Assessable Competency | YES | NO | APPROVE |
| `backend.resilience-patterns` | YES (`roadmap.sh/backend`) | Assessable Competency | YES | NO | APPROVE |
| `backend.observability` | YES (`roadmap.sh/backend`) | Assessable Competency | YES | NO | APPROVE |
| `shared.docker` | YES (`roadmap.sh/backend`) | Career Toolkit | NO | NO | APPROVE |
| `shared.git` | YES (`roadmap.sh/backend`) | Career Toolkit | NO | NO | APPROVE |
| `toolkit.ai-coding-backend` | YES (`roadmap.sh/backend`) | Career Toolkit | NO | NO | APPROVE |

### 11.3 Data Analyst Review Table
| Canonical Item | Source-Backed? | Classification | Assessment Eligible? | Mandatory? | Action Required |
|---|---|---|---|---|---|
| `data-analyst.foundations` | YES (`roadmap.sh/data-analyst`) | Conceptual Topic | YES | YES | APPROVE |
| `data-analyst.spreadsheets`| YES (`roadmap.sh/data-analyst`) | Assessable Competency | YES | YES | APPROVE |
| `shared.sql` | YES (`roadmap.sh/data-analyst`) | Assessable Competency | YES | YES | APPROVE |
| `data-analyst.python-or-r` | YES (`roadmap.sh/data-analyst`) | Programming Language | YES | YES (Pick 1) | APPROVE |
| `data-analyst.data-wrangling` | YES (`roadmap.sh/data-analyst`) | Assessable Competency | YES | YES | APPROVE |
| `data-analyst.eda` | YES (`roadmap.sh/data-analyst`) | Assessable Competency | YES | YES | APPROVE |
| `data-analyst.statistics` | YES (`roadmap.sh/data-analyst`) | Assessable Competency | YES | YES | APPROVE |
| `data-analyst.bi-dashboards` | YES (`roadmap.sh/data-analyst`) | Assessable Competency | YES | NO (Choice) | APPROVE |
| `data-analyst.visualization` | YES (`roadmap.sh/data-analyst`) | Assessable Competency | YES | NO | APPROVE |
| `data-analyst.data-collection` | YES (`roadmap.sh/data-analyst`) | Assessable Competency | YES | NO | APPROVE |
| `assessment-ext.data-storytelling` | Policy Extension | Policy Extension | YES | NO | POLICY EXTENSION |
| `data-analyst.ml-fundamentals` | YES (`roadmap.sh/data-analyst`) | Optional / Advanced | NO | NO | APPROVE |
| `data-analyst.deep-learning` | YES (`roadmap.sh/data-analyst`) | Optional / Advanced | NO | NO | APPROVE (Excluded from assessment) |
| `data-analyst.big-data` | YES (`roadmap.sh/data-analyst`) | Optional / Advanced | NO | NO | APPROVE |

---

## 12. Explicit List of Items NOT Directly in Roadmap.sh

To maintain 100% provenance transparency, the following items are proposed by SkillProof and are **NOT** explicit standalone leaf nodes in the respective roadmap.sh diagrams:
1. `assessment-ext.programming-fundamentals`: Added for technical diagnostic rigor (complexity, array vs hash table, collision strategies).
2. `assessment-ext.oop-solid`: Added for object-oriented architecture evaluation in enterprise backend roles.
3. `assessment-ext.data-storytelling`: Added for business impact and translation evaluation in commercial data analysis.
4. `toolkit.portfolio-readme`: Added to evaluate candidate GitHub presentation.
