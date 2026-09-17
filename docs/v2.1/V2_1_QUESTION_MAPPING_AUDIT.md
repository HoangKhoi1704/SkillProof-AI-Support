# SkillProof V2.1A — Question Mapping Audit

**Milestone**: V2.1A (Data Design Proposal)  
**Status**: AUDIT COMPLETE (Non-Production Mapping Analysis)  
**Date**: 2026-09-16  
**Auditor**: Antigravity Autonomous Agent  

---

## 1. Executive Summary

This document performs a comprehensive mapping of all **48 current Backend assessment questions** from `data/backend/questions.json` into the proposed canonical **roadmap.sh** framework.

### Integrity Rules Followed:
1. **Zero Question Rewrites**: Question texts, expected signals, and rubrics were **NOT** modified.
2. **Zero Baseline Mutations**: The frozen V2.0 baseline (`harness/baselines/v2.0-core-questions-baseline.json`) and SHA-256 checksums remain intact and pass validation 100%.
3. **Traceability**: Every question is mapped to a concrete roadmap.sh node or an explicitly documented Assessment Policy Extension.

---

## 2. Comprehensive 48-Question Mapping Table

| Question ID | Current Skill ID | Proposed Canonical Skill ID | Canonical Roadmap.sh Node / Locator | Mapping Confidence |
|---|---|---|---|---|
| `q-be-prog-01` | `programming-fundamentals` | `assessment-ext.programming-fundamentals` | `roadmap.sh/datastructures-and-algorithms#hash-tables` | policy-extension |
| `q-be-prog-02` | `programming-fundamentals` | `assessment-ext.programming-fundamentals` | `roadmap.sh/datastructures-and-algorithms#hash-tables` | policy-extension |
| `q-be-prog-03` | `programming-fundamentals` | `assessment-ext.programming-fundamentals` | `roadmap.sh/datastructures-and-algorithms#dynamic-programming` | policy-extension |
| `q-be-rest-01` | `rest-api` | `backend.rest-apis` | `rest`, `what-is-http#methods` | direct |
| `q-be-rest-02` | `rest-api` | `backend.rest-apis` | `json-apis`, `open-api-specs` | direct |
| `q-be-rest-03` | `rest-api` | `backend.rest-apis` | `rest#idempotency`, `what-is-http#status-codes` | direct |
| `q-be-sql-01` | `sql` | `shared.sql` | `relational-databases`, `databases#joins` | direct |
| `q-be-sql-02` | `sql` | `shared.sql` | `database-indexes`, `relational-databases#explain` | direct |
| `q-be-sql-03` | `sql` | `shared.sql` | `acid`, `transactions` | direct |
| `q-be-test-01` | `testing` | `backend.testing` | `unit-testing`, `testing#mocks-stubs` | direct |
| `q-be-test-02` | `testing` | `backend.testing` | `integration-testing` | direct |
| `q-be-test-03` | `testing` | `backend.testing` | `functional-testing`, `testing#flaky-mitigation` | normalized |
| `q-be-auth-01` | `authentication-security` | `backend.authentication-security` | `jwt`, `token-authentication` | direct |
| `q-be-auth-02` | `authentication-security` | `backend.authentication-security` | `web-security`, `owasp-risks#injection` | direct |
| `q-be-auth-03` | `authentication-security` | `backend.authentication-security` | `authentication`, `oauth` | direct |
| `q-be-sys-01` | `system-design` | `backend.system-design` | `building-for-scale`, `architectural-patterns#horizontal-scaling` | direct |
| `q-be-sys-02` | `system-design` | `backend.system-design` | `caching#cache-stampede`, `building-for-scale#leases` | direct |
| `q-be-sys-03` | `system-design` | `backend.system-design` | `throttling`, `building-for-scale#rate-limiting` | direct |
| `q-be-nosql-01` | `nosql` | `backend.nosql-databases` | `nosql-databases`, `mongodb#document-store` | direct |
| `q-be-nosql-02` | `nosql` | `backend.nosql-databases` | `nosql-databases`, `redis#key-value` | direct |
| `q-be-nosql-03` | `nosql` | `backend.nosql-databases` | `cap-theorem`, `data-replication` | direct |
| `q-be-cache-01` | `caching` | `backend.caching` | `caching`, `memcached`, `redis` | direct |
| `q-be-cache-02` | `caching` | `backend.caching` | `caching#eviction-policies`, `redis#maxmemory` | direct |
| `q-be-cache-03` | `caching` | `backend.caching` | `caching#stampede`, `building-for-scale` | direct |
| `q-be-cs-01` | `csharp` | `backend.lang-csharp` | `roadmap.sh/aspnet-core#async-await` | direct |
| `q-be-cs-02` | `csharp` | `backend.lang-csharp` | `roadmap.sh/aspnet-core#dependency-injection` | direct |
| `q-be-cs-03` | `csharp` | `backend.lang-csharp` | `roadmap.sh/aspnet-core#memory-spans` | direct |
| `q-be-java-01` | `java` | `backend.lang-java` | `java#concurrency-threads` | direct |
| `q-be-java-02` | `java` | `backend.lang-java` | `java#spring-framework-aop` | direct |
| `q-be-java-03` | `java` | `backend.lang-java` | `java#virtual-threads-jep444` | direct |
| `q-be-py-01` | `python` | `shared.python` | `python#data-model` | direct |
| `q-be-py-02` | `python` | `shared.python` | `python#asyncio-event-loop` | direct |
| `q-be-py-03` | `python` | `shared.python` | `python#fastapi-concurrency` | direct |
| `q-be-cpp-01` | `cpp` | `backend.lang-cpp` | `c`, `cpp#raii-smart-pointers` | direct |
| `q-be-cpp-02` | `cpp` | `backend.lang-cpp` | `c`, `cpp#memory-model-concurrency` | direct |
| `q-be-cpp-03` | `cpp` | `backend.lang-cpp` | `c`, `cpp#move-semantics-rvalues` | direct |
| `q-be-js-01` | `javascript` | `shared.javascript` | `javascript#event-loop` | direct |
| `q-be-js-02` | `javascript` | `shared.javascript` | `javascript#streams-backpressure` | direct |
| `q-be-js-03` | `javascript` | `shared.javascript` | `javascript#promises-async` | direct |
| `q-be-ts-01` | `typescript` | `backend.lang-typescript` | `roadmap.sh/typescript#discriminated-unions` | direct |
| `q-be-ts-02` | `typescript` | `backend.lang-typescript` | `roadmap.sh/typescript#generics-type-narrowing` | direct |
| `q-be-ts-03` | `typescript` | `backend.lang-typescript` | `roadmap.sh/typescript#runtime-boundary-validation` | direct |
| `q-be-go-01` | `go` | `backend.lang-go` | `go#interfaces-slice-internals` | direct |
| `q-be-go-02` | `go` | `backend.lang-go` | `go#goroutines-channels-select` | direct |
| `q-be-go-03` | `go` | `backend.lang-go` | `go#context-cancellation-leaks` | direct |
| `q-be-rust-01` | `rust` | `backend.lang-rust` | `rust#ownership-borrow-checker` | direct |
| `q-be-rust-02` | `rust` | `backend.lang-rust` | `rust#tokio-async-runtime` | direct |
| `q-be-rust-03` | `rust` | `backend.lang-rust` | `rust#traits-dynamic-dispatch` | direct |

---

## 3. Mapping Confidence Distribution

- **Direct Match**: **44 / 48** (91.7%)
- **Normalized Match**: **1 / 48** (2.1%)
- **Policy Extension**: **3 / 48** (6.3% — Core programming fundamentals)
- **Unresolved**: **0 / 48** (0.0%)

All 48 questions have clear, unambiguous target destinations in the canonical roadmap.sh framework.
