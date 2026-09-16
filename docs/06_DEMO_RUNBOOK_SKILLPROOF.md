# SkillProof MVP Demo Runbook

**Audience**: Presenters, Evaluators, Hackathon Judges  
**Duration Target**: 2–3 minutes  
**MVP Scope**: Assess → Skill Profile → Top Gaps → Personalized Roadmap → Gap-Backward Real-World Project  
**Security Invariant**: Never display, print, or log API keys or secrets.

---

## 1. Demo Prerequisites & Environment

| Component | Target URL | Tech Stack | Notes |
|---|---|---|---|
| **Frontend** | `http://localhost:3000` | Next.js 16 (Turbopack), React 19, Vanilla CSS | Fast client transitions, responsive dark mode |
| **Backend API** | `http://localhost:5000` | ASP.NET Core (.NET 10.0), Minimal APIs, OpenAI .NET SDK | CORS enabled for `http://localhost:3000` |
| **Model** | `gpt-5.4-mini` | OpenAI Chat Completions (Structured Outputs) | Fallback active if offline |

---

## 2. Startup Commands

Open two terminal windows:

### Terminal 1: Backend API
```powershell
cd d:\Roy\SkillProof-Support-AI\backend\SkillProof.Api
dotnet run --no-launch-profile --urls "http://localhost:5000"
```
*Expected log:*
`info: Microsoft.Hosting.Lifetime[14] Now listening on: http://localhost:5000`

### Terminal 2: Frontend Web App
```powershell
cd d:\Roy\SkillProof-Support-AI\frontend
npm run dev
```
*Expected log:*
`▲ Next.js 16.3.5 - Local: http://localhost:3000`

---

## 3. Primary Demo Path: Backend Developer (2 Minutes)

### Step 1: Role Selection (0:00 - 0:15)
1. Navigate browser to `http://localhost:3000`.
2. Emphasize the core thesis: **"SkillProof bridges the gap between learning and job readiness through evidence-based assessment and gap-backward projects."**
3. Point to the **Backend Developer** card.
4. Click **Start Career Diagnostic →**.

---

### Step 2: Diagnostic Assessment (0:15 - 0:45)
Enter these representative answers designed to showcase a candidate strong in fundamentals but needing practical database, testing, and distributed design growth:

* **Question 1: REST API (Competency: REST API)**
  > *Enter:* `PUT replaces the entire resource representation idempotently, meaning multiple identical requests produce the same state. PATCH applies partial modifications to a resource and is typically non-idempotent.`
  > *Click:* **Next Question →**

* **Question 2: SQL / Database (Competency: SQL / Database)**
  > *Enter:* `I would check if the query is slow and maybe add an index on CustomerId to see if that helps speed it up.`
  > *(Intentional gap: lacks execution plan analysis, composite indexing, and join optimization).*
  > *Click:* **Next Question →**

* **Question 3: Testing (Competency: Testing)**
  > *Enter:* `I would write basic unit tests to make sure the payment method doesn't crash when called.`
  > *(Intentional gap: lacks mock isolation, transient fault verification, and edge-case handling).*
  > *Click:* **Next Question →**

* **Question 4: System Design (Competency: System Design)**
  > *Enter:* `We could put a local in-memory counter in the web server to block IPs that send too many requests.`
  > *(Intentional gap: lacks distributed rate-limiting, Redis sliding windows, and cache outage mitigation).*
  > *Click:* **Next Question →**

* **Question 5: Authentication (Competency: Authentication)**
  > *Enter:* `Stateless JWT tokens signed with an RSA private key. The API verifies the cryptographic signature on every request without database lookups, and refresh tokens handle rotation.`
  > *Click:* **Next Question →**

* **Question 6: Programming Fundamentals (Competency: Programming Fundamentals)**
  > *Enter:* `Use asynchronous streaming with IAsyncEnumerable and pass CancellationToken through all async database and I/O calls to bound memory consumption.`
  > *Click:* **Review All Answers →**

---

### Step 3: Review & Diagnostic Submission (0:45 - 1:00)
1. Briefly highlight the **Review Screen**: The user can inspect all responses, see which are answered, or return to edit any question.
2. Click **Submit Diagnostic Assessment ✓**.

---

### Step 4: Qualitative Skill Profile & Top Gaps (1:00 - 1:25)
1. Explain to the audience:
   - **No fake percentages or arbitrary numbers**: Evaluation uses a strict 4-tier qualitative rubric (*Advanced, Intermediate, Beginner, Insufficient Evidence*).
   - Point out the **Top Prioritized Skill Gaps (Max 3)** banner:
     1. `SQL / Database` (Beginner)
     2. `Testing` (Beginner)
     3. `System Design` (Beginner)
   - Show that REST API and Authentication were evaluated as *Intermediate* with concrete demonstrated evidence cited.
2. Click **Generate My Roadmap →**.

---

### Step 5: Personalized Learning Roadmap (1:25 - 1:45)
1. Show the synthesized roadmap cards:
   - **Priority 1**: `SQL / Database` — Target Learning Goal & Actionable Practice Task.
   - **Priority 2**: `Testing` — Mock dependency isolation & automated test coverage.
   - **Priority 3**: `System Design` — Stateless scaling & rate-limiting middleware.
2. Emphasize: **"The roadmap tells the student what to practice next, but how do they prove job readiness to employers?"**
3. Click **Build a Project →**.

---

### Step 6: Gap-Backward Portfolio Project (1:45 - 2:15)
1. Reveal the **Recommended Real-World Portfolio Project**:
   - **Title**: *Expense Management API* (or live AI equivalent: *Rate-Limited Task API with PostgreSQL Optimization*).
   - **Strategic Rationale**: Call out the explanation of **WHY** this project was recommended: it directly engineers requirements around the student's diagnosed weaknesses.
   - **Expected Deliverables**: Highlight observable artifacts (*SQL schema DDL, EXPLAIN plan analysis, xUnit test suite, architecture diagram*).
   - **Project Requirements**: Walk through each requirement card showing:
     - `Requirement`: Concrete technical task
     - `Develops`: **SQL / Database** / **Testing** / **System Design**
     - `↳ Deliverable`: Observable proof of competence
2. Conclude: **"SkillProof turns interview failure into targeted practice and portfolio proof."**

---

## 4. Secondary Demo Path: Financial Analyst (Backup / 1 Minute)

To demonstrate multi-role flexibility:
1. Click **Change Role** in the top header.
2. Select **Financial Analyst**.
3. Enter representative answers:
   - **Q7 (Statements)**: Strong answer on 3-statement reconciliation.
   - **Q8 (Excel)**: Strong answer on `XLOOKUP`, `INDEX/MATCH`, and dynamic arrays.
   - **Q9 (Modeling)**: Basic answer lacking SaaS driver linkages (*Gap 1: Financial Modeling*).
   - **Q10 (Forecasting)**: Basic answer lacking cash conversion cycle decomposition (*Gap 2: Forecasting*).
   - **Q11 (Ratios)**: Strong answer calculating Current vs. Quick ratios.
   - **Q12 (Data Analysis)**: Basic answer without gross margin variance waterfall (*Gap 3: Data Analysis*).
4. Submit Diagnostic → Generates Financial Analyst Skill Profile with top gaps: `Financial Modeling`, `Forecasting`, `Data Analysis`.
5. Generate Roadmap → Prioritized financial modeling and variance decomposition tasks.
6. Build a Project → Recommends **Company Financial Health & 3-Year Outlook** with dynamic workbook, cash flow forecast, and margin variance deliverables.

---

## 5. Offline Fallback Demo Procedure (Zero Risk Guarantee)

If the venue Wi-Fi drops, OpenAI has an outage, or API rate limits trigger:

1. **No UI Error**: The system automatically delegates to curated deterministic evaluators, generators, and recommenders.
2. **Forced Offline Mode**: If you want to demonstrate 100% offline resilience on stage, launch the backend with:
   ```powershell
   $env:DISABLE_LIVE_AI="true"; dotnet run --no-launch-profile --urls "http://localhost:5000"
   ```
3. In this mode, latency is instant (< 50ms) and zero external API calls are made.

---

## 6. What NOT to Do During the Demo

* ❌ **Do NOT inspect the network tab or print backend terminal windows with secrets**: User Secrets and keys must remain strictly private.
* ❌ **Do NOT leave all answers blank unless explicitly demonstrating empty-answer fallback**: Submitting blank answers legitimately yields *Insufficient Evidence* across all competencies.
* ❌ **Do NOT click "Back" on browser navigation while an API request is pending**: Use the in-app breadcrumbs and buttons (`← Back to Roadmap`, `← Review Answers`).
* ❌ **Do NOT edit code or configurations during the live presentation**.
