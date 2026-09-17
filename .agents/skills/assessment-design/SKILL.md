---
name: assessment-design
description: Designs SkillProof career-readiness assessments using role competencies, realistic interview-style questions, practical cases, reasoning prompts, rubrics, and strict provenance verification rules.
---

# Assessment Design Skill

Use this skill when creating, auditing, or reviewing SkillProof diagnostic questions, skills, and assessment data.

## Goal
Create a career-readiness assessment that evaluates real job readiness and practical reasoning rather than academic trivia, anchored by verifiable provenance.

## Core Flow
```text
Canonical Role (roadmap.sh)
→ Competency / Skill
→ Question (Knowledge / Interview / Practical Case / Reasoning)
→ Rubric (Insufficient Evidence, Beginner, Intermediate, Advanced)
→ Expected Signals & Evidence
→ Provenance Gate (Source Inspected + Locator + Claim Verified)
```

## Anti-Hallucination & Provenance Protocol

When anchoring questions or skills to external sources, adhere strictly to the **Provenance Gate**:

1. **Source Discovery**: Search engines and directory listings are discovery evidence only. A snippet or search title MUST NOT be used as proof that a source supports a claim.
2. **Source Inspection**: The source must be opened and its actual accessible content inspected.
3. **Locator Identification**: A specific chapter, heading, section anchor, or problem ID must be recorded.
4. **Claim Verification**: The inspected content must directly support the question's validity or interview practice claim.
5. **Audit Metadata**: `verifiedAt`, `canonicalUrl`, and `locator` are required for claim-verified evidence.

### Prohibited Practices
- **No model-memory citations**: Never reconstruct quotations, page numbers, or interview claims from AI memory.
- **No title/reputation inference**: A book or author's reputation does not prove that a specific technical claim was verified.
- **No false company attribution**: Do not claim "Amazon/Google asks this" without direct, verifiable evidence from accessible company guidance.
- **No URL-exists assumption**: A working URL does not prove that the underlying content supports the attributed claim.
- **No textbook content verification claims**: Books like DDIA are catalogued as `reference-only` professional references; do not claim full-text inspection without authorized accessible sources.

## Role & Framework Taxonomy

### Canonical Role Foundation (V2 Target)
- Canonical role sources:
  - `https://roadmap.sh/frontend` (Frontend Developer)
  - `https://roadmap.sh/backend` (Backend Developer)
  - `https://roadmap.sh/data-analyst` (Data Analyst)
- Roadmap.sh is the canonical foundation for role skill requirements, prerequisite topologies, and learning paths.
- **Questions map INTO canonical skills**: The canonical taxonomy must not mutate merely to fit available interview questions.
- **AI must not invent canonical skills or nodes**: Internal slugs may be normalized, but nodes must trace to real roadmap.sh elements.

### Assessment Semantics
- Qualitative tiers: **`Insufficient Evidence`**, **`Beginner`**, **`Intermediate`**, **`Advanced`**.
- **`Not Assessed` is a distinct state**: Unassessed skills must NEVER silently become `Beginner`, `failed`, or `0%`. Lack of assessment evidence is not proof of lack of skill.
- **Mandatory Fundamentals**: Roles must assess appropriate fundamentals (OOP, SOLID, basic DSA, complexity/Big-O, runtime memory) alongside practical scenarios.

### Privacy Invariant
- Expected signals and scoring rubrics are backend-only evaluation data and must **NEVER** be exposed in public question DTOs to candidate browsers.
