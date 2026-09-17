# SkillProof V2.1A — Raw External Dataset Inventory

**Milestone**: V2.1A (Data Design Proposal)  
**Status**: INVENTORY COMPLETE  
**Date**: 2026-09-16  
**Auditor**: Antigravity Autonomous Agent  

---

## 1. Executive Summary

This inventory audits all existing, proposed, and historically discussed question datasets and candidate project sources for SkillProof V2.

In accordance with strict provenance rules:
- **No manufactured counts**: Unverified numbers or phantom files are not reported.
- **Explicit absence recording**: Any file discussed in historical brainstorming that is not present in the current repository is clearly declared absent.
- **Provenance gating**: Unaudited or raw community datasets are marked `needs audit` or `reject` prior to any consideration for production ingestion.

---

## 2. Dataset Inventory Table

| Dataset Identifier | Physical Status in Repo | Role Relevance | Usable Coverage Area | Provenance Quality | Proposed Canonical Mapping | Recommended Use |
|---|---|---|---|---|---|---|
| **SkillProof Backend Questions (`data/backend/questions.json`)** | **PRESENT & VERIFIED** | Backend Developer | 6 Core Competencies, 2 Recommended Competencies, 8 Programming Languages (48 calibrated questions). | **TIER 1 (High)**: Fully audited in V2.0; baseline locked; dual SHA-256 protected. | `backend.*`, `shared.sql`, `shared.python`, `shared.javascript` | **PRIMARY**: Immediate production foundation. |
| **SkillProof Sources Catalog (`data/backend/sources.json`)** | **PRESENT & VERIFIED** | Backend Developer, Cross-Role | 42 audited sources (RFCs, official language docs, SFIA 9, USENIX, LeetCode, SRE Book). | **TIER 1 (High)**: Full URL verification, canonical redirects, paywall downgrade. | Cross-role anchoring | **PRIMARY**: Production provenance foundation. |
| **"Software Questions CSV"** | **ABSENT FROM REPOSITORY** | Software Engineering (General) | Historical brainstorming item. Contains untracked raw interview dumps. | **UNKNOWN / UNAUDITED**: Unknown licensing, unknown question authorship, potential hallucination risk. | N/A | **NEEDS AUDIT**: Prohibited from ingestion until licensed and verified against canonical roadmap nodes. |
| **"Data Science / Data Analyst Interview Notebook"** | **ABSENT FROM REPOSITORY** | Data Analyst, Data Science | Historical brainstorming item. Contains mixed Python/ML notebooks. | **UNKNOWN / UNAUDITED**: Unknown origins; risk of blurring Data Analyst into Data Science. | N/A | **NEEDS AUDIT**: Must be screened strictly against the Data Analyst canonical framework table before any use. |
| **Roadmap.sh Official Role Content (`nilbuild/developer-roadmap`)** | **PRESENT VIA SUBMODULE / GIT AUDIT** | Frontend, Backend, Data Analyst | 379 raw topic definitions across Frontend (121), Backend (156), and Data Analyst (102). | **TIER 1 (High)**: Open-source community authority; commit `3cba37fa7440b2f8fafb234561c9e48f85ee1c2f`. | `frontend.*`, `backend.*`, `data-analyst.*`, `shared.*` | **PRIMARY CANONICAL FOUNDATION**: Approved source of truth for taxonomy and roadmap graph. |
| **`practical-tutorials/project-based-learning`** | **CANDIDATE REPOSITORY** | Frontend, Backend | Curated tutorials for building real-world projects in various languages. | **TIER 2 (Curated)**: Open-source repository with vetted community PRs. | Future V2.5 Practice Project Catalog | **CANDIDATE FOR V2.5**: Approved for blueprinting practice projects without AI generation. |
| **Roadmap.sh Project Ideas Catalog** | **CANDIDATE REPOSITORY** | Frontend, Backend, Data Analyst | Project blueprints published at `roadmap.sh/projects`. | **TIER 2 (Curated)**: Direct alignment with roadmap.sh role skills. | Future V2.5 Practice Project Catalog | **CANDIDATE FOR V2.5**: Ideal for milestone practice project pairing. |

---

## 3. Dataset Usage Governance Policy for V2

1. **Zero Hallucination Gate**: No question may enter the SkillProof catalog without direct alignment to a canonical competency ID and at least one verifiable framework source in `sources.json`.
2. **Quality Screening Before Ingestion**:
   - Any future external CSV or notebook must be parsed, deduplicated, calibrated to 3 tiers of difficulty (`foundation`, `applied`, `advanced-reasoning`), and equipped with a 4-tier qualitative rubric (`Insufficient Evidence`, `Beginner`, `Intermediate`, `Advanced`).
3. **No Batch Auto-Importing**: External questions must pass through human review and dataset validation before being added to production baselines.
