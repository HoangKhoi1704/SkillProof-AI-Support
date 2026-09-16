# SkillProof Backend Assessment Data Foundation (v2.0)

This directory establishes the extended competency taxonomy, source provenance model, and calibrated 48-question assessment bank for the **Backend Developer** career role, encompassing Core competencies, Recommended competencies (NoSQL, Caching), and 8 separated Programming Languages.

> **Isolation Notice**: This dataset represents a data foundation milestone. It does not alter the existing runtime flow, API contracts, SQLite database seeding, or seed data in `backend/SkillProof.Api/Data/SeedData.cs`.

---

## 1. Directory Structure

```text
data/backend/
├── skill-catalog.json   # 14 competencies (Core, Recommended, Optional) + 8 Separated Programming Languages
├── sources.json         # 42 authoritative frameworks, standards, engineering references, and interview resources
├── questions.json       # Exactly 48 questions (18 Core + 6 Recommended + 24 Languages) with 1/1/1 difficulty calibration
└── README.md            # Architectural documentation, provenance model, and validation instructions
```

---

## 2. Skill Catalog Specification (`skill-catalog.json`)

Competencies reflect role capabilities required to design, implement, and operate backend services. **Programming languages are explicitly separated into their own category** rather than mixed into conceptual competencies like SQL or System Design.

### Competency Categories
1. **Core Competencies (6)**:
   - `programming-fundamentals`: Data structures, Big-O complexity, OOP/SOLID principles, error handling.
   - `rest-api`: HTTP semantics, idempotency, resource contracts, pagination, concurrency (ETags).
   - `sql`: Relational modeling, multi-table joins, execution plan analysis (EXPLAIN), indexing, transactions/ACID.
   - `testing`: Unit testing, test doubles (mocks/stubs/fakes), integration testing, test determinism.
   - `authentication-security`: JWT token lifecycle, RBAC/ABAC authorization, revocation, OWASP API risks (BOLA).
   - `system-design`: Stateless scaling, distributed rate limiting, caching strategies, fault tolerance.
2. **Recommended Competencies (2)**:
   - `nosql`: Document/key-value stores, CAP theorem, eventual consistency.
   - `caching`: Redis data structures, eviction policies (LRU/LFU), memory pressure, cache stampede mitigations.
3. **Optional Competencies (6)**:
   - `git`: Gitflow, branching, merge/rebase conflict resolution.
   - `docker`: Multi-stage Dockerfiles, compose orchestration.
   - `cicd`: Automated build/test pipelines, deployment strategies.
   - `concurrency`: Asynchronous non-blocking I/O, thread safety, locks.
   - `messaging`: Pub/sub message queues, idempotent consumer patterns.
   - `observability`: Structured JSON logging, metrics, OpenTelemetry distributed tracing.

### Separated Language Category (8)
Languages provide runtime execution syntax but are semantically distinct from foundational competencies:
- `csharp` (C# / .NET)
- `java` (Java / Spring Boot / JVM)
- `python` (Python / FastAPI / AsyncIO)
- `cpp` (Modern C++ / RAII)
- `javascript` (Node.js / Event Loop)
- `typescript` (TypeScript / Generics)
- `go` (Golang / Goroutines & Channels)
- `rust` (Rust / Ownership & Lifetimes)

---

## 3. Refined Source Taxonomy & Provenance Model (`sources.json`)

To preserve academic and provenance rigor without fabricated claims, sources are categorized into a 7-type taxonomy:

| Source Type | Description | Examples in Catalog |
|---|---|---|
| `career-framework` | Industry/community competency taxonomies answering *"Why does this competency matter?"* | `src-cf-roadmap-backend`, `src-cf-sfia-9-software-dev` |
| `official-standard` | Formally ratified open standards. | `src-doc-rfc9110-http` (HTTP Semantics), `src-doc-rfc7519-jwt` (JWT), `src-doc-openjdk-virtual-threads` |
| `official-documentation` | Official documentation from authoritative technology stewards. | Microsoft Learn, Oracle Java, Python.org, Redis.io, MongoDB, CppReference, Node.js, TypeScript, Go.dev, Rust-lang.org |
| `professional-reference` | Seminal textbooks and industry engineering references. | `src-ref-ddia-kleppmann`, `src-ref-xunit-test-patterns`, `src-ref-effective-modern-cpp`, `src-ref-google-sre-book` |
| `interview-preparation-resource` | Reputable educational resources dedicated to technical interview preparation. | `src-ipr-system-design-primer`, `src-ipr-leetcode-175-combine-two-tables`, `src-ipr-bytebytego-system-design` |
| `company-published-interview-guidance` | Official engineering publications and hiring guides published by tech employers. | Amazon Technical Topics, Google Tech Dev Guide, Stripe Rate Limiters, Slack Pagination, Google Test Doubles, Meta Memcache |
| `candidate-reported-interview` | Specific candidate-reported interview experiences unverified by the employer. | *(Zero used in v2; reserved for specific permalinked reports)* |

### Provenance Rules
1. **Never claim employer attribution**: Never state *"Company X asked this SkillProof question"*. All assessment questions remain `provenance: "skillproof-curated"`.
2. **Interview evidence meaning**: Evidence indicates that the topic or question pattern is documented in industry interview practice.
3. **Truthful absence**: Questions without documented interview practice remain `interviewEvidenceIds: []` and `verificationStatus: "framework-supported-only"`.

---

## 4. Calibrated Question Bank (48 Questions)

Every assessable skill contains **exactly 3 questions** calibrated across difficulty tiers:
- **Foundation**: Demonstrates clear conceptual understanding without senior-level assumptions.
- **Applied**: Evaluates practical workplace engineering implementation and problem solving.
- **Advanced-Reasoning**: Evaluates architectural trade-offs, internal engine behavior, and distributed complexity.

### Summary Inventory

| Category | Skills | Questions / Skill | Subtotal |
|---|---|---|---|
| **Core Competencies** | `programming-fundamentals`, `rest-api`, `sql`, `testing`, `authentication-security`, `system-design` | 3 (1F, 1A, 1AR) | **18** |
| **Recommended Competencies** | `nosql`, `caching` | 3 (1F, 1A, 1AR) | **6** |
| **Programming Languages** | `csharp`, `java`, `python`, `cpp`, `javascript`, `typescript`, `go`, `rust` | 3 (1F, 1A, 1AR) | **24** |
| **Total Assessment Bank** | **16 Assessable Skills** | **3 each** | **48** |

---

## 5. Dataset Validation

Run the validator from the project root:

```bash
node harness/validators/validate-backend-assessment-data.mjs
```

The validator verifies:
- Exactly 48 questions across 16 assessable skills (6 Core, 2 Recommended, 8 Languages).
- Existing 18 Core questions remain frozen and unchanged.
- Exact 1 Foundation, 1 Applied, and 1 Advanced-Reasoning question per assessable skill.
- All subskills belong to the declared skill in `skill-catalog.json`.
- Language questions map to language skills rather than competencies.
- Exactly 4 approved qualitative rubric levels.
- Every question has substantive `expectedSignals`.
- Referential integrity: all framework and interview source IDs exist in `sources.json`.
