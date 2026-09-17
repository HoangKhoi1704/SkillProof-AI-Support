# Implementation Plan — SkillProof V2.6: Project Evidence Verification + Portfolio/CV Proof

Complete the SkillProof product loop by upgrading project evaluation so claims are grounded in verifiable, structured project evidence across Frontend, Backend, and Data Analyst roles.

## User Review Required

> [!IMPORTANT]
> **Strict Safety & Non-Execution Principle:**
> Static inspection only. The system will **never** execute candidate code, run `npm install`, `docker build`, `dotnet test`, or execute Python notebooks. All repository, deployment, and notebook verifications are bounded, read-only, and protected against SSRF.

> [!NOTE]
> **Zero-OpenAI Deterministic Operation:**
> Verification and evaluation operate 100% deterministically without requiring an OpenAI API key or live network calls during tests. Regression tests utilize deterministic fixtures.

---

## Proposed Changes

### Phase 1: V2.5 Metadata Audit & Provenance Reconciliation

#### [MODIFY] [learning-resources.json](file:///d:/Roy/SkillProof-Support-AI/data/v3/learning-resources.json)
- Re-audit all 20 resources for `publisher`, `sourceType`, and `isOfficial`:
  - Change `res-be-jwt-guide-01` (`sourceName: "Auth0 by Okta"`, `sourceUrl: "https://jwt.io/introduction"`) `isOfficial` to `false` (vendor-provided guide, not official IETF RFC 7519 standard).
  - Verify all other official documentation sources (`MDN`, `WHATWG`, `PostgreSQL`, `Python`, `Microsoft`, `Git SCM`, `Redis`, `Pandas`, `Seaborn`).

#### [MODIFY] [V2_5_RESOURCE_CATALOG_AUDIT.md](file:///d:/Roy/SkillProof-Support-AI/docs/v2.5/V2_5_RESOURCE_CATALOG_AUDIT.md)
- Correct the audit table where "System Design Primer (Official)" was erroneously documented instead of `martinfowler.com` (`isOfficial: false`).
- Document explicit corrections for `jwt.io` (`isOfficial: false`) and `martinfowler.com` (`isOfficial: false`).

#### [MODIFY] [V2_5_CURATED_PROJECTS_IMPLEMENTATION_REPORT.md](file:///d:/Roy/SkillProof-Support-AI/docs/v2.5/V2_5_CURATED_PROJECTS_IMPLEMENTATION_REPORT.md)
- Reconcile Table 2.2 with the actual 9 projects in `data/v3/projects.json` (`proj-fe-weather-practice`, `proj-fe-todo-practice`, `proj-fe-platform-portfolio`, `proj-be-database-practice`, `proj-be-cache-practice`, `proj-be-resilient-portfolio`, `proj-da-eda-practice`, `proj-da-sql-practice`, `proj-da-bi-portfolio`).
- Validate all `commit:fcd321e` source locators in `practical-tutorials/project-based-learning`.

---

### Phase 2: Evidence Models & Verification Abstractions (Backend)

#### [NEW] [EvidenceVerificationModels.cs](file:///d:/Roy/SkillProof-Support-AI/backend/SkillProof.Api/Models/EvidenceVerificationModels.cs)
Define structured types:
- `EvidenceType`: `Repository`, `Deployment`, `Notebook`, `Dashboard`, `Dataset`, `CandidateNotes`.
- `EvidenceStatus`: `Verified`, `PartiallyVerified`, `Unverified`, `Unavailable`, `Invalid`.
- `RepositoryEvidenceResult`:
  - `RepositoryExists`, `DefaultBranch`, `FoundFiles`, `ManifestFiles`, `TestFiles`, `ReadmeExcerpt`, `LanguageIndicators`, `SecurityWarnings`.
- `DeploymentEvidenceResult`:
  - `IsReachable`, `StatusCode`, `ContentType`, `PageTitle`, `ObservedFeatures`, `ErrorMessage`.
- `DataAnalystEvidenceResult`:
  - `NotebookFound`, `NotebookImports`, `CellCounts`, `DatasetFound`, `DashboardReachable`, `NotesSummary`.
- `RequirementEvidenceItem`:
  - `Requirement`, `TargetsSkill`, `EvidenceFound`, `Status` (`Demonstrated`, `Partially Demonstrated`, `Insufficient Evidence`), `SourceArtifact`, `Explanation`.
- `SkillEvidenceItem`:
  - `SkillId`, `EvidenceStatus`, `SupportingEvidence`, `MissingEvidence`.

#### [MODIFY] [ProjectModels.cs](file:///d:/Roy/SkillProof-Support-AI/backend/SkillProof.Api/Models/ProjectModels.cs)
Extend `SubmitProjectEvidenceRequest` with structured optional role-specific fields while keeping full backward compatibility:
- `RepositoryUrl`, `DeployedUrl`, `NotebookUrl`, `DashboardUrl`, `DatasetUrl`, `Notes`.
- Backward-compatible optional string fields (`ProjectSummary`, `ImplementationExplanation`, etc.).

#### [NEW] [IUrlSafetyValidator.cs](file:///d:/Roy/SkillProof-Support-AI/backend/SkillProof.Api/Services/IUrlSafetyValidator.cs) & [UrlSafetyValidator.cs](file:///d:/Roy/SkillProof-Support-AI/backend/SkillProof.Api/Services/UrlSafetyValidator.cs)
Implement SSRF-safe URL validation:
- Enforce scheme: `https` only (or `http` strictly for loopback test fixtures in `Development`).
- Reject loopback (`127.0.0.0/8`, `::1`, `localhost`).
- Reject private IPv4 (`10.0.0.0/8`, `172.16.0.0/12`, `192.168.0.0/16`).
- Reject link-local and cloud metadata (`169.254.0.0/16`, `169.254.169.254`).
- Validate DNS resolution and reject unsafe redirects.

#### [NEW] [IRepositoryEvidenceVerifier.cs](file:///d:/Roy/SkillProof-Support-AI/backend/SkillProof.Api/Services/IRepositoryEvidenceVerifier.cs) & [RepositoryEvidenceVerifier.cs](file:///d:/Roy/SkillProof-Support-AI/backend/SkillProof.Api/Services/RepositoryEvidenceVerifier.cs)
Static, bounded repository inspection:
- Bounded to 50 files, 64KB per file, 2MB max content.
- Sensitive file filter (`.env`, `*.pem`, `id_rsa`, `credentials.json`): emit security warning without reading contents.
- Extract file presence, test directory presence, manifests, language signals.
- Support mock/fixture injection for test environments without internet dependency.

#### [NEW] [IDeploymentEvidenceVerifier.cs](file:///d:/Roy/SkillProof-Support-AI/backend/SkillProof.Api/Services/IDeploymentEvidenceVerifier.cs) & [DeploymentEvidenceVerifier.cs](file:///d:/Roy/SkillProof-Support-AI/backend/SkillProof.Api/Services/DeploymentEvidenceVerifier.cs)
- Safe GET/HEAD for web & APIs.
- HTML title extraction, HTTP status check, OpenAPI detection.

#### [NEW] [IDataAnalystEvidenceVerifier.cs](file:///d:/Roy/SkillProof-Support-AI/backend/SkillProof.Api/Services/IDataAnalystEvidenceVerifier.cs) & [DataAnalystEvidenceVerifier.cs](file:///d:/Roy/SkillProof-Support-AI/backend/SkillProof.Api/Services/DataAnalystEvidenceVerifier.cs)
- Static `.ipynb` inspection (cell counts, markdown headings, import extraction).
- Dashboard and dataset reachability without requiring a web deployment.

---

### Phase 3: Evidence-Gated Project Evaluator & Portfolio Proof

#### [MODIFY] [DeterministicProjectEvaluator.cs](file:///d:/Roy/SkillProof-Support-AI/backend/SkillProof.Api/Services/DeterministicProjectEvaluator.cs)
Upgrade to evidence-first evaluation:
- Runs layered verifiers across submitted evidence.
- Constructs `RequirementEvidenceMatrix` mapping each project requirement to actual verified artifacts.
- Maps verified requirements to targeted canonical skills.
- Strictly adheres to evidence states:
  - `Demonstrated`: Requirements backed by verified artifacts (code, tests, endpoints).
  - `Partially Demonstrated`: Incomplete artifacts or partial coverage.
  - `Insufficient Evidence`: Missing/unreachable/invalid evidence.
- Portfolio/CV Proof Gate:
  - ONLY `Demonstrated` skills receive strong portfolio and CV bullets.
  - `Partially Demonstrated` receives cautious development notes only.
  - `Insufficient Evidence` is completely omitted from positive claims.
  - Includes internal claim traceability (`projectId`, `canonicalSkillIds`, `evidenceIds`).

#### [MODIFY] [OpenAiProjectEvaluator.cs](file:///d:/Roy/SkillProof-Support-AI/backend/SkillProof.Api/Services/OpenAiProjectEvaluator.cs)
- Constrain AI prompt to verified excerpts only.
- Validate AI output against deterministic evidence; AI cannot cite unverified evidence or override deterministic facts.

#### [MODIFY] [Program.cs](file:///d:/Roy/SkillProof-Support-AI/backend/SkillProof.Api/Program.cs)
- Register `IUrlSafetyValidator`, `IRepositoryEvidenceVerifier`, `IDeploymentEvidenceVerifier`, `IDataAnalystEvidenceVerifier`.
- Wire upgraded verifiers into `IProjectEvaluator`.

---

### Phase 4: Frontend UI Upgrade (`/portfolio`)

#### [MODIFY] [types.ts](file:///d:/Roy/SkillProof-Support-AI/frontend/app/types.ts)
- Add structured submission fields: `repositoryUrl`, `deployedUrl`, `notebookUrl`, `dashboardUrl`, `datasetUrl`, `notes`.
- Add `RequirementEvidenceResult` fields: `evidenceFound`, `sourceArtifact`, `status`, `explanation`.

#### [MODIFY] [portfolio/page.tsx](file:///d:/Roy/SkillProof-Support-AI/frontend/app/portfolio/page.tsx)
- Dynamically adjust submission fields based on active role:
  - **Frontend:** Repository URL, Deployed URL, Notes.
  - **Backend:** Repository URL, Deployed API URL, Notes.
  - **Data Analyst:** Repository URL, Notebook (.ipynb) URL, Dataset URL, Dashboard/BI URL, Notes (explicitly note web app is NOT required).
- Add verification progress states ("Checking evidence...", "Verified", "Could not verify", "Invalid evidence").
- Render structured **Requirement Evidence Matrix** (Project Requirement $\rightarrow$ Evidence Found $\rightarrow$ Status $\rightarrow$ Artifact).
- Render **Skill Evidence** with neutral phrasing.
- Render **Gated Portfolio & CV Proof** with claim traceability.
- Add **Reassessment CTA** ("Re-assess this skill") linking back to diagnostic interview/skills for the target skill.

---

### Phase 5: Verification & Testing

#### [NEW] [MilestoneV26EvidenceVerificationTests.cs](file:///d:/Roy/SkillProof-Support-AI/backend/SkillProof.Api.Tests/MilestoneV26EvidenceVerificationTests.cs)
Comprehensive xUnit test suite:
- SSRF & IP rejection (127.0.0.1, localhost, ::1, 10.x, 172.16.x, 192.168.x, 169.254.x, file://, ftp://, private redirects).
- Repository verifier bounds (file count, size, sensitive file warnings without content leakage).
- Deployed web & API safe verification.
- Data Analyst static notebook inspection (no code execution).
- Requirement evidence matrix and skill evidence matrix generation.
- Gated portfolio proof generation (demonstrated vs partial vs insufficient).
- Zero-OpenAI deterministic fallback.

#### [NEW] [v2_6_project_verification.spec.ts](file:///d:/Roy/SkillProof-Support-AI/tests/e2e/v2_6_project_verification.spec.ts)
Playwright E2E test suite:
1. **Frontend:** Project selection $\rightarrow$ Repository + Deployed evidence $\rightarrow$ Verification $\rightarrow$ Portfolio proof.
2. **Backend:** Project selection $\rightarrow$ Repository + API evidence $\rightarrow$ Verification $\rightarrow$ Evidence matrix $\rightarrow$ CV bullet.
3. **Data Analyst:** Project selection $\rightarrow$ Repository + Notebook + Dashboard evidence $\rightarrow$ Verified without website.
4. **Invalid Evidence:** SSRF / Unreachable URL $\rightarrow$ Neutral handling $\rightarrow$ "Insufficient Evidence" $\rightarrow$ No strong CV claim.

#### [NEW] [V2_6_PROJECT_EVIDENCE_VERIFICATION_REPORT.md](file:///d:/Roy/SkillProof-Support-AI/docs/v2.6/V2_6_PROJECT_EVIDENCE_VERIFICATION_REPORT.md)
Comprehensive technical report covering submission model, verification architecture, SSRF protections, repository bounds, evidence matrices, and claim gates.

---

## Verification Plan

### Automated Tests
- Run all V3 validators:
  `node harness/validators/validate-v3-catalog.mjs; node harness/validators/validate-v3-question-bank.mjs; node harness/validators/validate-v3-resources.mjs; node harness/validators/validate-v3-projects.mjs`
- Run full backend test suite:
  `dotnet test backend/SkillProof.slnx`
- Run frontend production build:
  `npm --prefix frontend run build`
- Run Playwright V2.6 E2E suite:
  `npx playwright test tests/e2e/v2_6_project_verification.spec.ts`

### Manual Verification
- Verify that candidate-controlled code is NEVER executed (no `npm install`, `docker`, or notebook execution).
- Verify zero OpenAI calls are required for deterministic regression.
- Verify that numeric percentages remain strictly absent from all project evaluation and portfolio UI surfaces.
