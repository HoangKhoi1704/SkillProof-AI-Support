# 05 — CURATED SEED QUESTIONS & RUBRICS — SKILLPROOF

> **Authority**: Governed by [`docs/00_SOURCE_OF_TRUTH_SKILLPROOF.md`](file:///d:/Roy/SkillProof-Support-AI/docs/00_SOURCE_OF_TRUTH_SKILLPROOF.md) and [`docs/01_IMPLEMENTATION_SPEC_SKILLPROOF.md`](file:///d:/Roy/SkillProof-Support-AI/docs/01_IMPLEMENTATION_SPEC_SKILLPROOF.md).
> **Privacy Invariant**: Rubrics are strictly **backend-only evaluation data**. They must NEVER be returned by `GET /api/diagnostics/questions` to the frontend client.

---

## 1. Specification Overview

- **Target Roles**:
  1. `backend-developer` (Backend Developer)
  2. `financial-analyst` (Financial Analyst)
- **Question Count**: Exactly 6 curated questions per role (satisfies the 5–8 question prototype requirement).
- **Question ID**: Standardized as an integer (`int`), unique across the platform:
  - Backend Developer: IDs `1` through `6`
  - Financial Analyst: IDs `7` through `12`
- **Question Types**: Knowledge, Interview, Practical Case, Reasoning.
- **Evaluation Tiers**: ONLY `Beginner`, `Intermediate`, `Advanced`, `Insufficient Evidence`.
- **Source Verification Rule**: Curated interview patterns without verified company citations are explicitly marked `[standard interview pattern - unverified]`.

---

## 2. Role 1: Backend Developer (`backend-developer`)

### Question 1 (ID: 1)
- **Competency**: `REST API`
- **Type**: `interview`
- **Question Text**:
  "Explain the difference between PUT and PATCH in REST API design. In what real-world scenarios would you choose one over the other, and what idempotency considerations apply?"
- **Source Type**: `team_curated`
- **Source Reference**: `[standard interview pattern - unverified]`
- **Backend Rubric**:
  - `Beginner`: Mentions that PUT updates data and PATCH modifies data, but cannot explain idempotency or partial vs. complete resource replacement.
  - `Intermediate`: Explains that PUT replaces the entire resource (and is idempotent) while PATCH applies partial modifications (and can be non-idempotent depending on payload). Gives a valid API example.
  - `Advanced`: Thoroughly analyzes idempotency, HTTP specification semantics, edge-case implications (handling missing fields, concurrency control with ETags / `If-Match`), and payload formats like JSON Merge Patch.
  - `Insufficient Evidence`: Blank, irrelevant, or vague answer that shows no understanding of HTTP verbs.

---

### Question 2 (ID: 2)
- **Competency**: `SQL / Database`
- **Type**: `practical_case`
- **Question Text**:
  "A production relational database query joining an `Orders` table (10 million rows) and an `OrderItems` table (40 million rows) has become extremely slow when filtering by `CustomerId` and `OrderDate`. How would you diagnose the bottleneck and what indexing or schema optimizations would you investigate?"
- **Source Type**: `team_curated`
- **Source Reference**: `[standard interview pattern - unverified]`
- **Backend Rubric**:
  - `Beginner`: Suggests adding an index on `CustomerId` without explaining execution plans, composite indexes, or join mechanics.
  - `Intermediate`: Identifies the need to inspect the query execution plan (e.g., `EXPLAIN`), evaluate composite indexing (e.g., `(CustomerId, OrderDate)`), and check join condition indexes on foreign keys.
  - `Advanced`: Discusses index selectivity, covering indexes, execution plan operator costs (index scan vs. index seek, hash match vs. nested loop), partition pruning, statistics updates, and potential trade-offs with write latency.
  - `Insufficient Evidence`: No database concepts mentioned, or completely irrelevant advice.

---

### Question 3 (ID: 3)
- **Competency**: `Testing`
- **Type**: `interview`
- **Question Text**:
  "When writing automated unit tests for an API service that charges customer payments through a third-party gateway, how do you isolate dependencies, and what positive, negative, and edge test cases would you write?"
- **Source Type**: `team_curated`
- **Source Reference**: `[standard interview pattern - unverified]`
- **Backend Rubric**:
  - `Beginner`: Understands the concept of running a test to check if payment works, but suggests calling the actual payment provider or lacks knowledge of mocking.
  - `Intermediate`: Explains using test doubles/mocks (e.g., interfaces, Moq, NSubstitute) to isolate the payment gateway. Lists positive scenarios (success 200), validation errors, and gateway rejection (declined card).
  - `Advanced`: Discusses dependency inversion, mock verification (ensuring double-charge prevention), handling transient network timeouts, idempotency key assertions, and boundary edge cases (currency rounding, null inputs).
  - `Insufficient Evidence`: No testing concepts or mocking strategy described.

---

### Question 4 (ID: 4)
- **Competency**: `System Design`
- **Type**: `reasoning`
- **Question Text**:
  "You need to design a backend rate-limiting mechanism to prevent abusive clients from degrading API availability. What architectural approach would you take, where would rate-limit state be stored, and how would you handle distributed instances?"
- **Source Type**: `team_curated`
- **Source Reference**: `[standard interview pattern - unverified]`
- **Backend Rubric**:
  - `Beginner`: Proposes basic in-memory counter in the web server process; fails to consider multi-instance distributed environments.
  - `Intermediate`: Identifies centralized cache storage (e.g., Redis) using token bucket or sliding window algorithms, with client identification (IP or API key/JWT) and standard HTTP headers (`429 Too Many Requests`, `Retry-After`).
  - `Advanced`: Evaluates concurrency races (Redis Lua scripts or atomic increments), fail-open vs fail-closed strategies during cache outage, distributed race conditions, tiered rate limits, and gateway/middleware layer placement.
  - `Insufficient Evidence`: Off-topic or fails to present any architectural solution.

---

### Question 5 (ID: 5)
- **Competency**: `Authentication`
- **Type**: `knowledge`
- **Question Text**:
  "Explain how stateless JWT (JSON Web Token) authentication works in an API, how claims are verified, and how you would handle token revocation or user logout before expiration."
- **Source Type**: `team_curated`
- **Source Reference**: `[standard interview pattern - unverified]`
- **Backend Rubric**:
  - `Beginner`: Knows JWT contains user info and a token string, but cannot explain signature verification or believes the server must store the JWT in the database for basic validation.
  - `Intermediate`: Explains header/payload/signature structure, cryptographic verification using a secret or public/private key, and standard expiry handling. Proposes token blacklisting or refresh token rotation for revocation.
  - `Advanced`: Deeply addresses symmetric vs asymmetric signing (HMAC vs RSA/ECDSA), trade-offs of distributed revocation (Redis revocation list vs short-lived access tokens with sliding refresh tokens), and security vulnerabilities (XSS, CSRF, algorithm `none`).
  - `Insufficient Evidence`: Cannot explain token authentication or signature verification.

---

### Question 6 (ID: 6)
- **Competency**: `Programming Fundamentals`
- **Type**: `practical_case`
- **Question Text**:
  "Consider a background task that processes a collection of 50,000 records. Describe how you would manage memory allocation, asynchronous I/O, and cancellation if the user or orchestrator requests a shutdown mid-processing."
- **Source Type**: `team_curated`
- **Source Reference**: `[standard interview pattern - unverified]`
- **Backend Rubric**:
  - `Beginner`: Describes a simple `for` loop; does not address memory limits, thread blocking, or cancellation mechanisms.
  - `Intermediate`: Mentions batching/streaming (e.g., chunking, `IAsyncEnumerable`), non-blocking async/await for I/O operations, and passing a `CancellationToken` through downstream asynchronous methods.
  - `Advanced`: Detailed analysis of memory overhead (avoiding large object heap allocations, streaming pipelines), bounded parallelism (`SemaphoreSlim` or Channels), graceful teardown checkpoints, and transactional batch rollback upon cancellation.
  - `Insufficient Evidence`: Blank or incoherent response regarding asynchronous programming or memory.

---

## 3. Role 2: Financial Analyst (`financial-analyst`)

### Question 7 (ID: 7)
- **Competency**: `Financial Statements`
- **Type**: `interview`
- **Question Text**:
  "If a company's Net Income increases by $10M on the Income Statement, walk through how this flows through the Cash Flow Statement and impacts the Balance Sheet at the end of the period."
- **Source Type**: `team_curated`
- **Source Reference**: `[standard interview pattern - unverified]`
- **Backend Rubric**:
  - `Beginner`: Knows Net Income increases Cash or Retained Earnings, but fails to connect the 3 statements sequentially.
  - `Intermediate`: Traces Net Income as the starting line of Operating Cash Flow, accounts for working capital or non-cash adjustments, notes net cash change, and balances the Balance Sheet via Retained Earnings (Equity) and Cash (Assets).
  - `Advanced`: Explicitly distinguishes between cash vs accrual revenue, addresses tax implications, accounts for depreciation/amortization if relevant, and verifies the Balance Sheet equation ($Assets = Liabilities + Equity$) with clean precision.
  - `Insufficient Evidence`: Unable to explain the basic relationship between Net Income, Cash Flow, and Balance Sheet.

---

### Question 8 (ID: 8)
- **Competency**: `Excel / Spreadsheets`
- **Type**: `practical_case`
- **Question Text**:
  "You need to build a dynamic monthly revenue dashboard from multiple disparate transaction sheets. Which Excel functions or tools would you choose (e.g., XLOOKUP vs INDEX/MATCH, dynamic arrays, Pivot Tables), and how do you ensure formula auditability and error handling?"
- **Source Type**: `team_curated`
- **Source Reference**: `[standard interview pattern - unverified]`
- **Backend Rubric**:
  - `Beginner`: Relies strictly on basic `VLOOKUP` with hardcoded column indexes; no error handling (`IFERROR`) or formula auditing mentioned.
  - `Intermediate`: Explains `XLOOKUP` or `INDEX/MATCH` for robust bidirectional lookups, wraps calculations in `IFERROR` / `IFNA`, utilizes Pivot Tables or `SUMIFS` for multi-condition aggregation, and uses consistent naming conventions.
  - `Advanced`: Emphasizes dynamic array functions (`FILTER`, `UNIQUE`), structured table references, separation of raw data / calculation engine / presentation layers, and model auditing best practices (color-coding inputs vs formulas, circular reference guards).
  - `Insufficient Evidence`: Blank, vague, or mentions no spreadsheet functions.

---

### Question 9 (ID: 9)
- **Competency**: `Financial Modeling`
- **Type**: `reasoning`
- **Question Text**:
  "How would you structure a dynamic 3-statement financial model from scratch for a subscription SaaS business, and what are the critical operational drivers you would link to revenue and expenses?"
- **Source Type**: `team_curated`
- **Source Reference**: `[standard interview pattern - unverified]`
- **Backend Rubric**:
  - `Beginner`: Suggests putting revenue and cost lines on one sheet without dynamic linkages or understanding subscription revenue drivers.
  - `Intermediate`: Outlines the 3-statement model architecture (Income Statement, Balance Sheet, Cash Flow) connected by Net Income and Cash. Identifies SaaS drivers: Monthly Recurring Revenue (MRR), Churn Rate, Customer Acquisition Cost (CAC), and Customer Lifetime Value (LTV).
  - `Advanced`: Details cohort-based revenue schedules, deferred revenue balance sheet treatment, gross margin vs operating leverage, working capital schedules, dynamic debt/interest circularity switches, and scenario/sensitivity toggles.
  - `Insufficient Evidence`: Fails to explain financial modeling structure or SaaS business mechanics.

---

### Question 10 (ID: 10)
- **Competency**: `Forecasting`
- **Type**: `practical_case`
- **Question Text**:
  "A retail company experienced a 15% revenue increase year-over-year, but operating cash flow turned negative over the same period. What operational and balance sheet drivers would you investigate in your variance analysis to explain this discrepancy?"
- **Source Type**: `team_curated`
- **Source Reference**: `[standard interview pattern - unverified]`
- **Backend Rubric**:
  - `Beginner`: Assumes revenue increase always means more cash, or attributes the issue solely to generic increased expenses.
  - `Intermediate`: Identifies Working Capital deterioration: buildup of unsold inventory (Cash outflow), surge in Accounts Receivable (uncollected revenue), or aggressive supplier paydown (Accounts Payable reduction).
  - `Advanced`: Analyzes cash conversion cycle (Days Sales Outstanding, Days Inventory Outstanding, Days Payable Outstanding), customer credit terms, revenue recognition timing (aggressive unearned revenue), and margin compression due to discounting.
  - `Insufficient Evidence`: Cannot explain why cash flow can diverge from revenue growth.

---

### Question 11 (ID: 11)
- **Competency**: `Ratio Analysis`
- **Type**: `interview`
- **Question Text**:
  "When assessing a manufacturing firm's liquidity and solvency for credit risk, which specific ratios would you compute, and how would you interpret a high Current Ratio alongside a deteriorating Quick (Acid-Test) Ratio?"
- **Source Type**: `team_curated`
- **Source Reference**: `[standard interview pattern - unverified]`
- **Backend Rubric**:
  - `Beginner`: Lists basic terms like profit margin; cannot clearly define Current Ratio or Quick Ratio.
  - `Intermediate`: Defines Current Ratio ($Current Assets / Current Liabilities$) and Quick Ratio ($(Cash + Marketable Securities + Receivables) / Current Liabilities$). Explains that the divergence indicates excessive capital tied up in illiquid inventory or slow-moving stock.
  - `Advanced`: Connects liquidity to solvency metrics (Debt-to-Equity, Interest Coverage Ratio), analyzes inventory write-down risk, obsolescence exposure, operating cycle strain, and implications for covenant compliance and short-term debt servicing.
  - `Insufficient Evidence`: Provides incorrect ratio formulas or irrelevant analysis.

---

### Question 12 (ID: 12)
- **Competency**: `Data Analysis`
- **Type**: `reasoning`
- **Question Text**:
  "You are presented with 3 years of product-level sales data and asked by the CFO why operating margins are contracting despite top-line sales growth. Walk through your step-by-step quantitative analysis framework to isolate the root cause."
- **Source Type**: `team_curated`
- **Source Reference**: `[standard interview pattern - unverified]`
- **Backend Rubric**:
  - `Beginner`: Suggests making a graph of sales and costs without structured decomposition.
  - `Intermediate`: Proposes structured waterfall/variance analysis: decomposing Gross Margin by product line (price vs volume vs cost-of-goods-sold inflation), examining mix shift toward lower-margin products, and analyzing SG&A fixed vs variable overhead growth.
  - `Advanced`: Outlines a multi-factor attribution framework (price elasticity, product mix shift, customer concentration, input commodity variance, operating leverage analysis), statistical outlier screening, unit economic breakdown, and actionable sensitivity recommendations for executive presentation.
  - `Insufficient Evidence`: Lacks a quantitative framework or logical decomposition approach.
