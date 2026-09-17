# SkillProof Backend Assessment Data Foundation (v2.0)

This directory establishes the audited competency taxonomy, strict provenance model, and calibrated 48-question assessment bank for the **Backend Developer** career role, encompassing 6 Core competencies, 2 Recommended competencies (NoSQL, Caching), and 8 separated Programming Languages.

---

## 1. Directory Structure

```text
data/backend/
├── skill-catalog.json   # 14 competencies (Core, Recommended, Optional) + 8 Separated Programming Languages
├── sources.json         # 42 audited sources with strict accessStatus, verificationStatus, and verifiedAt metadata
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

## 3. Strict Provenance Model & Taxonomy (`sources.json`)

SkillProof enforces a strict distinction between:
- **Structural Validation**: Does the schema and referential structure parse and validate?
- **Source Verification**: Does the source exist at an accessible, canonical URL?
- **Claim Verification**: Was the accessible content directly inspected and confirmed to support the attributed claim?

### Provenance Gate

```text
SOURCE CANDIDATE
       ↓
Does resource exist?
  NO → REJECT
  YES
       ↓
Can actual relevant content be inspected?
  NO → REFERENCE-ONLY / INACCESSIBLE
  YES
       ↓
Does content support this use?
  NO → REMOVE ASSOCIATION
  YES
       ↓
CLAIM-VERIFIED
```

### Source Classification

| Source Type | Description | Audited Catalog Examples |
|---|---|---|
| `career-framework` | Industry/community competency taxonomies answering *"Why does this competency matter?"* | `src-cf-roadmap-backend`, `src-cf-sfia-9-software-dev` |
| `official-standard` | Formally ratified open standards and RFCs. | `src-doc-rfc9110-http`, `src-doc-rfc7519-jwt`, `src-doc-openjdk-virtual-threads` |
| `official-documentation` | Official documentation from authoritative technology stewards and engineering blogs. | Microsoft Learn, Oracle Java, Python.org, Redis.io, MongoDB, CppReference, Node.js, TypeScript, Go.dev, Rust-lang.org, Stripe Engineering, Slack Engineering |
| `professional-reference` | Peer-reviewed engineering papers and recognized textbooks. | `src-ref-xunit-test-patterns`, `src-ref-google-sre-book`, `src-ref-ddia-kleppmann` (reference-only), `src-cpig-meta-scaling-memcache` (USENIX NSDI '13) |
| `interview-preparation-resource` | Reputable educational resources dedicated to technical interview preparation. | `src-ipr-system-design-primer`, `src-ipr-leetcode-175-combine-two-tables`, `src-ipr-bytebytego-system-design` |
| `company-published-interview-guidance` | Official career/hiring preparation resources published by tech employers. | `src-cpig-google-tech-dev-guide` (Google Build Your Future) |
| `candidate-reported-interview` | Unverified candidate interview reports. | *(Zero permitted in v2)* |

### Provenance Audit Invariants
1. **Never claim employer attribution without direct proof**: Never state *"Company X asked this question"* merely because an engineering blog discussed the topic.
2. **Engineering blogs are not interview guidance**: Blogs by Stripe and Slack are classified as `official-documentation`, and USENIX papers as `professional-reference`.
3. **Unavailable sources rejected**: Broken 404 links (such as legacy Amazon tech interview topics and Google testing blogs) are classified as `rejected` and removed from question evidence.
4. **Reference-only textbooks**: Recognized textbooks like DDIA are classified as `reference-only`; they provide general context in `frameworkSourceIds` but do not generate claim-verified interview evidence.
5. **Truthful absence**: 37 questions have no accessible interview practice source and are truthfully classified as `framework-supported-only`. 11 questions have verified interview practice evidence.

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
- Frozen Core v2.0 baseline integrity: SHA-256 file checksum and 18 canonical question fingerprints.
- Strict provenance gate: All sources have valid `accessStatus`, `verificationStatus`, and `verifiedAt`.
- Unavailable or rejected sources cannot be referenced as valid evidence.
- Reference-only sources cannot produce claim-verified interview evidence.
- Questions with interview evidence have traceable `canonicalUrl`, `locator`, and `verifiedAt`.
- All subskills belong to the declared skill in `skill-catalog.json`.
- Language questions map to language skills rather than competencies.
- Exactly 4 approved qualitative rubric levels.
- Every question has substantive `expectedSignals`.
- Referential integrity: all framework and interview source IDs exist in `sources.json`.
