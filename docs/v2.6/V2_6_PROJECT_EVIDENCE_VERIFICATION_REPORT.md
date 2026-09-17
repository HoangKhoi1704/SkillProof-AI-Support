# Milestone V2.6: Project Evidence Verification & Evidence-Gated Portfolio Proof Report
**Author:** Antigravity AI  
**Date:** September 17, 2026  
**Status:** Completed & Fully Verified  
**Milestone:** SkillProof V2.6 (Final Feature Milestone)

---

## Executive Summary

SkillProof Milestone V2.6 completes the end-to-end product loop:
$$\text{Career Goal} \longrightarrow \text{Adaptive Assessment} \longrightarrow \text{Diagnostic Skill Matrix} \longrightarrow \text{Canonical Roadmap} \longrightarrow \text{Curated Learning Resources} \longrightarrow \text{Curated Projects} \longrightarrow \text{Structured Evidence Submission} \longrightarrow \text{Layered Evidence Verification} \longrightarrow \text{Requirement Evidence Matrix} \longrightarrow \text{Evidence-Gated Portfolio / CV Proof} \longrightarrow \text{Re-assessment Handoff}$$

The foundational principle enforced across V2.6 is:
$$\textbf{PROJECT CLAIM} \quad \text{must be backed by} \quad \textbf{PROJECT EVIDENCE}$$
$$\text{No verified evidence} \implies \text{No strong competency claim}.$$

---

## 1. V2.5 Metadata Audit & Reconciliation

Before V2.6 implementation, an audit of V2.5 resource and project metadata was performed:
1. **Learning Resources Audit (`data/v3/learning-resources.json`)**:
   - Fixed `res-be-jwt-guide-01` (`JWT.io / Auth0 by Okta Introduction to JSON Web Tokens`): Corrected `isOfficial: false` (since Auth0 is a commercial vendor/educational provider, not RFC/IETF standards body).
   - Documented Martin Fowler architecture articles as community/authoritative open-source resources (`isOfficial: false`), reserving `isOfficial: true` strictly for vendor standards (e.g. Microsoft Learn, MDN Web Docs, PostgreSQL Docs).
   - Reconciled `docs/v2.5/V2_5_RESOURCE_CATALOG_AUDIT.md` with the 20 resources in `data/v3/learning-resources.json`.
2. **Project-Based-Learning Provenance (`data/v3/projects.json`)**:
   - Reconciled all project locators with the actual repository commit `fcd321e0503f8f2e21975e542ff543af6b5e0ef7`.
   - Verified that `docs/v2.5/V2_5_CURATED_PROJECTS_IMPLEMENTATION_REPORT.md` lists the exact 9 curated projects across Frontend, Backend, and Data Analyst roles.
3. **Validators**:
   - `validate-v3-resources.mjs` and `validate-v3-projects.mjs` executed and confirmed 100% compliant.

---

## 2. Submission Model V2 & Role-Specific Evidence

The submission contract supports structured, role-tailored evidence without forcing a single uniform format:

### DTO Schema (`SubmitProjectEvidenceRequest`)
```csharp
public class SubmitProjectEvidenceRequest
{
    public string? RepositoryUrl { get; set; }
    public string? DeployedUrl { get; set; }
    public string? NotebookUrl { get; set; }
    public string? DashboardUrl { get; set; }
    public string? DatasetUrl { get; set; }
    public string? Notes { get; set; }
    public string? ProjectSummary { get; set; }
    public string? ImplementationExplanation { get; set; }
    public string? ArchitectureDecisions { get; set; }
    public string? TestingExplanation { get; set; }
    public List<string>? EvidenceExcerpts { get; set; }
}
```

### Role-Specific Evidence Matrix
| Role | Allowed Evidence Artifacts | Required Artifacts | Special Guarantees |
| :--- | :--- | :--- | :--- |
| **Frontend Developer** | GitHub/Git Repository, Deployed Web Application, Architectural Notes | Repository URL | Deployed web app verified with safe GET/HEAD probe. |
| **Backend Developer** | GitHub/Git Repository, Deployed API / OpenAPI Documentation, Architectural Notes | Repository URL | Only safe non-destructive GET/HEAD requests used. |
| **Data Analyst** | Git Repository, Jupyter Notebook (`.ipynb`), Dataset Artifact, Dashboard / BI URL, Notes | Repository or Notebook | **Deployed website is strictly NOT required.** Static notebook AST & data schema analysis. |

---

## 3. Security Architecture & SSRF Defense

The verifier executes within a zero-trust model:

1. **Zero Arbitrary Code Execution**:
   - **NO** `npm install`, `npm test`, or `dotnet test` executed against candidate repositories.
   - **NO** candidate Docker containers built or executed.
   - **NO** arbitrary shell scripts executed.
   - **NO** notebook execution (static inspection of JSON cells, markdown, and imports only).
2. **Server-Side Request Forgery (SSRF) Protection (`UrlSafetyValidator`)**:
   - **Scheme Whitelist**: Strictly `https` in production.
   - **Loopback Blocking**: Rejects `localhost`, `127.0.0.0/8`, `::1`.
   - **Private IPv4 Blocking**: Rejects `10.0.0.0/8`, `172.16.0.0/12`, `192.168.0.0/16`.
   - **Link-Local / Cloud Metadata Blocking**: Rejects `169.254.0.0/16`, AWS/GCP metadata (`169.254.169.254`).
   - **Redirect Validation**: All redirects are bound (max 3) and re-validated against SSRF filters before following.
   - **Dangerous Scheme Blocking**: Rejects `file:`, `ftp:`, `javascript:`, `data:`, `gopher:`.
3. **Repository Inspection Bounds**:
   - Max 50 files inspected per repository.
   - Max 64 KB per individual file.
   - Max 2 MB total repository payload in memory.
   - 10-second request timeout with cancellation tokens.
4. **Sensitive File & Secret Protection**:
   - Inspects file paths for credentials and secret patterns (`.env`, `.env.local`, `credentials.json`, `private.key`, `id_rsa`).
   - If detected, records an audit warning: `Security Warning: Potential sensitive file detected: '.env'. Secrets inspection prevented.`
   - Contents of sensitive files are **never read, logged, or displayed**.

---

## 4. Verification Layers & Evidence States

### Verification Layers
$$\text{Submission} \longrightarrow \text{URL/Metadata Safety Validation} \longrightarrow \text{Deterministic Repo Verification} \longrightarrow \text{Deterministic Deployment/Notebook Verification} \longrightarrow \text{Requirement Evidence Matrix} \longrightarrow \text{Competency Evidence Matrix} \longrightarrow \text{Gated Portfolio Proof}$$

### Evidence Artifact States
- `Verified`: Concrete evidence artifact deterministically observed and confirmed reachable/valid.
- `PartiallyVerified`: Artifact observed but missing specific elements (e.g. repo exists but missing test directories).
- `Unverified`: Artifact not verified or missing from submission.
- `Unavailable`: Remote service timed out or offline; gracefully handled without failing candidate.
- `Invalid`: Malformed URL, SSRF violation, or insecure scheme.

> [!IMPORTANT]
> Unverified or unavailable evidence maps to `Insufficient Evidence` or `could not verify`. It **never** collapses into "failed candidate" or "project bad".

---

## 5. Requirement Evidence Matrix & Skill Evidence Matrix

### 1. Requirement Evidence Matrix
Every requirement from the curated project specification is mapped directly:
$$\text{Project Requirement} \longrightarrow \text{Targets Skill} \longrightarrow \text{Evidence Found} \longrightarrow \text{Status} \longrightarrow \text{Source Artifact}$$

### 2. Evaluated Competency States
- `Demonstrated`: Deliverables satisfied with verified repository/deployment artifacts.
- `Partially Demonstrated`: Partial evidence or written explanation observed; lacks automated verification.
- `Insufficient Evidence`: Required deliverables absent or unverified.

> [!NOTE]
> Numeric scores, percentages, and arbitrary matching algorithms are strictly disallowed.

---

## 6. Portfolio & CV Proof Claim Gate

The evidence-gating rule:
1. **Demonstrated Competencies**:
   - Generate positive, evidence-backed CV bullets with full provenance.
   - Example: `"Engineered backend.relational-databases for Production-Grade Resilient Order & Inventory Service, supported by verified repository artifacts and observable technical deliverables."`
   - Disallows overclaims like "Mastered system design" or "Expert engineer".
2. **Partially Demonstrated**:
   - Generates cautious interview notes only: `"Partial evidence submitted for frontend.html-core; additional integration verification recommended prior to CV inclusion."`
   - Does **not** produce strong CV bullets.
3. **Insufficient Evidence**:
   - Excluded completely from positive claims.
4. **Claim Traceability (`claimTraceability`)**:
   - Each claim maintains verifiable linkage: `ProjectId`, `CanonicalSkillIds`, `EvidenceIds`, `Status`.

---

## 7. Re-assessment Isolation

- Verified project evidence **does NOT** mutate or auto-upgrade the diagnostic Skill Matrix.
- Diagnostic assessment and project portfolio proof remain separate evidence streams.
- The UI provides an explicit **"Re-assess Skill ↻"** action, allowing candidates to update their baseline Skill Matrix through diagnostic assessment.

---

## 8. Test Verification Results

### 1. Backend Unit & Integration Tests (xUnit)
```
dotnet test backend/SkillProof.slnx
```
- Total Tests: **266**
- Passed: **266** (100%)
- Failed: **0**
- Skipped: **0**
- Duration: **19.2s**
- Includes 25 dedicated V2.6 tests in `MilestoneV26EvidenceVerificationTests.cs`:
  - SSRF private IP, loopback, and metadata blocking
  - Redirect loop and redirect-to-private-IP prevention
  - Bounded repository file inspection and `.env` sensitive file handling
  - Static notebook AST and Data Analyst verification without web servers
  - Evidence-gated portfolio proof generation (only `Demonstrated` yields strong CV bullets)
  - Zero-OpenAI deterministic fallback verification

### 2. Frontend Build & TypeScript Compliance
```
node ./frontend/node_modules/typescript/bin/tsc -p ./frontend/tsconfig.json --noEmit
next build ./frontend
```
- TypeScript check: **0 errors**
- Next.js build: **12 / 12 static & dynamic routes compiled successfully**

### 3. Playwright E2E Verification
```
npx playwright test tests/e2e/v2_6_project_verification.spec.ts
```
- **4 / 4 passed (1.0m)**:
  - `Frontend Developer`: Curated Project $\rightarrow$ Deployed Web + Repo Evidence $\rightarrow$ Evidence Matrix & Gated Proof (**PASS**)
  - `Backend Developer`: Curated Project $\rightarrow$ Repo + API Evidence $\rightarrow$ Traceability & Re-assessment CTA (**PASS**)
  - `Data Analyst`: Appropriate Projects & Evidence Flexibility (No Website Required) (**PASS**)
  - `Invalid / Insufficient Evidence`: Neutral Handling & No Overclaiming (**PASS**)

### 4. Regression Suites
- `tests/e2e/v2_3_assessment.spec.ts`: **3 / 3 passed**
- `tests/e2e/v2_4_canonical_roadmap.spec.ts`: **3 / 3 passed**
- `tests/e2e/v2_5_resources_projects.spec.ts`: **3 / 3 passed**

### 5. Repository Data Validators
- `node scripts/validate-v3-catalog.mjs`: **PASS (3 roles, 42 nodes, 37 relationships)**
- `node scripts/validate-v3-question-bank.mjs`: **PASS (48 questions, 100% verified)**
- `node scripts/validate-v3-resources.mjs`: **PASS (20 resources, 100% verified)**
- `node scripts/validate-v3-projects.mjs`: **PASS (9 projects, 100% verified)**

---

## 9. Known Prototype Limitations

1. **GitHub Live Inspection**: When running offline or without GitHub Personal Access Tokens, live rate limits may restrict deep file inspection for arbitrary unknown repositories; pre-registered fixtures provide deterministic testability.
2. **Session Lifetime**: Submission and evaluation data are preserved within the active 30-minute user journey session storage; multi-device persistent accounts belong to post-V2 roadmaps.
3. **Data Analyst Question Bank**: Full question bank coverage is currently calibrated for Backend Developer; Data Analyst and Frontend Developer continue to utilize the canonical framework, roadmap, and project catalog with evidence verification.

---

## 10. Conclusion

SkillProof Milestone V2.6 has successfully delivered structured, role-specific project evidence verification, SSRF and bounds protections, the Requirement Evidence Matrix, and evidence-gated portfolio and resume claims. All verification gates and regressions pass completely.
