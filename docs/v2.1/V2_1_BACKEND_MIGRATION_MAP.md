# SkillProof V2.1A — Backend Catalog Migration Map

**Milestone**: V2.1A (Data Design Proposal)  
**Status**: DRAFT FOR HUMAN REVIEW (Non-Production Migration Analysis)  
**Date**: 2026-09-16  
**Auditor**: Antigravity Autonomous Agent  

---

## 1. Objective

This document maps all **22 current Backend catalog skills** and **8 programming languages** from the SkillProof V2.0 catalog into the proposed canonical **roadmap.sh** framework.

Each entry is assigned a definitive migration action:
- **KEEP**: Retain unchanged in concept.
- **RENAME**: Align identifier with canonical roadmap.sh terminology.
- **RECLASSIFY**: Adjust layer (e.g. from Assessable Competency to Career Toolkit).
- **SPLIT / MERGE**: Rationalize overlapping competency boundaries.
- **ADD**: New canonical competencies derived from the official Backend roadmap.
- **RETIRE LATER**: Legacy nodes flagged for eventual deprecation.

---

## 2. Comprehensive Migration Matrix

| Current V2.0 ID | Current Category | Proposed Canonical ID | Migration Action | Roadmap.sh Locator | Rationale & Backwards-Compatibility Plan |
|---|---|---|---|---|---|
| `programming-fundamentals` | Core | `assessment-ext.programming-fundamentals` | RENAME / POLICY EXTENSION | `roadmap.sh/datastructures-and-algorithms` | Implements core algorithms and array vs hash table complexity. Marked as Policy Extension to distinguish from roadmap.sh backend leaf nodes. Backwards-compatible alias preserved in V2.1B. |
| `rest-api` | Core | `backend.rest-apis` | RENAME | `rest`, `json-apis`, `open-api-specs` | Renamed to standard pluralized canonical namespace. 100% direct match. |
| `sql` | Core | `shared.sql` | RENAME / MERGE TO SHARED | `relational-databases`, `postgresql`, `mysql` | Elevated to global shared competency so Data Analyst role reuses the exact same relational foundation. |
| `testing` | Core | `backend.testing` | RENAME | `testing`, `unit-testing`, `integration-testing` | 100% direct match with roadmap.sh testing nodes. |
| `authentication-security` | Core | `backend.authentication-security` | RENAME | `authentication`, `jwt`, `oauth`, `web-security` | 100% direct match with roadmap.sh security & auth branch. |
| `system-design` | Core | `backend.system-design` | RENAME | `architectural-patterns`, `building-for-scale`, `microservices` | 100% direct match. |
| `nosql` | Recommended | `backend.nosql-databases` | RENAME | `nosql-databases`, `mongodb`, `cassandra` | 100% direct match with roadmap.sh NoSQL branch. |
| `caching` | Recommended | `backend.caching` | RENAME | `caching`, `redis`, `memcached` | 100% direct match. |
| `git` | Optional | `shared.git` | RECLASSIFY / TOOLKIT | `version-control-systems`, `git`, `github` | Reclassified to Career Toolkit. Shared across Frontend and Backend. Tracked for portfolio/readiness, not 3-question oral exam. |
| `docker` | Optional | `shared.docker` | RECLASSIFY / TOOLKIT | `roadmap.sh/docker`, `lxc` | Reclassified to Career Toolkit. |
| `cicd` | Optional | `backend.ci-cd` | RECLASSIFY / TOOLKIT | `ci--cd` | Reclassified to Career Toolkit. |
| `concurrency` | Optional | `backend.concurrency` | MERGE / SUB-TOPIC | Language runtimes & `scaling-databases` | Concurrency mechanics are inherently language-specific (e.g. goroutines in Go, async/await in C#, event loop in Node). Proposed to merge into respective language assessments and resilience patterns. |
| `messaging` | Optional | `backend.message-brokers` | RENAME | `message-brokers`, `rabbitmq`, `kafka` | 100% direct match. |
| `observability` | Optional | `backend.observability` | RENAME | `observability`, `monitoring`, `telemetry` | 100% direct match. |
| *(New Canonical Node)* | — | `backend.resilience-patterns` | ADD | `throttling`, `backpressure`, `circuit-breaker`, `graceful-degradation` | Added directly from official roadmap.sh building-for-scale branch. |
| *(New Canonical Node)* | — | `shared.internet-http` | ADD | `how-does-the-internet-work`, `what-is-http`, `dns-and-how-it-works` | Added from foundational roadmap.sh section. Shared across Frontend and Backend. |
| *(New Canonical Node)* | — | `toolkit.ai-coding-backend` | ADD / TOOLKIT | `ai-assisted-coding`, `claude-code`, `antigravity` | Added from modern roadmap.sh AI tooling branch. Classified as Career Toolkit. |

---

## 3. Programming Languages Migration

All 8 current backend languages are preserved 100%:

| Current Language ID | Proposed Canonical ID | Action | Roadmap.sh Support |
|---|---|---|---|
| `csharp` | `backend.lang-csharp` | RENAME | `roadmap.sh/aspnet-core` |
| `java` | `backend.lang-java` | RENAME | `java` (official backend language choice) |
| `python` | `shared.python` | RENAME / SHARED | `python` (shared across Backend & Data Analyst) |
| `cpp` | `backend.lang-cpp` | RENAME | `c` / `cpp` (official backend language choice) |
| `javascript` | `shared.javascript` | RENAME / SHARED | `javascript` (shared across Frontend & Backend) |
| `typescript` | `backend.lang-typescript` | RENAME | `roadmap.sh/typescript` |
| `go` | `backend.lang-go` | RENAME | `go` (official backend language choice) |
| `rust` | `backend.lang-rust` | RENAME | `rust` (official backend language choice) |

---

## 4. Backwards-Compatibility Guarantee

During Milestone V2.1B implementation:
1. Database entity identifiers can accept both legacy slugs (`sql`, `rest-api`) and new canonical slugs (`shared.sql`, `backend.rest-apis`) via an internal alias resolver.
2. Existing 48 question files will NOT be invalidated.
3. Zero breaking changes to existing endpoints `/api/roles/backend-developer/skills`.
