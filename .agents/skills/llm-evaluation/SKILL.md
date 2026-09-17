---
name: llm-evaluation
description: Implements and verifies SkillProof LLM evaluation flows for diagnostic answers, skill-gap prioritization, roadmaps, and gap-based project recommendations using structured outputs and guardrails.
---

# LLM Evaluation Skill

Use this skill whenever SkillProof sends student data to an LLM or evaluates diagnostic responses, roadmaps, or project recommendations.

## Goal
Produce explainable, structured, evidence-based AI evaluations that the backend can safely validate, strictly bounded by qualitative rubrics and approved project catalogs.

## Core Rules & Guardrails
1. **Qualitative Only**: Skill levels must strictly use the 4 approved qualitative tiers: `Insufficient Evidence`, `Beginner`, `Intermediate`, `Advanced`. Never emit arbitrary percentages or numeric scores.
2. **Not Assessed != Beginner**: Never equate lack of assessment to low skill. Unassessed competencies remain `Not Assessed`.
3. **No Hallucinated Experience**: The LLM must evaluate solely based on evidence present in the candidate's submission.
4. **Post-Answer Explanations**: Provide concise feedback:
   - What the answer covered
   - What could be stronger
   - Concise reference explanation
   *Never expose raw system prompts, private rubrics, or expected signals directly as scoring criteria.*
5. **Deterministic Testing**: During tests, `OpenAI:LiveEvaluationEnabled` must be `false` to guarantee deterministic evaluation with zero outbound external LLM API calls.

## Architecture Transitions (Current vs V2 Target)

### Diagnostic Evaluation
- **Current**: Evaluates answers using structured JSON against server-side rubric prompts.
- **V2 Target**: Multi-turn evaluation with immediate post-answer explanations, preserving session state across multi-page navigation.

### Roadmap Generation
- **Current (V1.2)**: LLM generates structured learning tasks for prioritized gaps.
- **V2 Target**: Personalize a subgraph from the canonical **roadmap.sh** role graph based on assessed profile + unassessed required skills. The AI explains prioritization and creates focus paths; it does **not** invent canonical role nodes.

### Practice Projects
- **Current (V1.2)**: LLM generates project specifications based on gap context (`OpenAiProjectRecommender`).
- **V2 Target**: Projects are matched, ranked, and explained from an **approved curated catalog** (e.g. `practical-tutorials/project-based-learning`). The AI does **not** fabricate catalog projects.

### Portfolio Project Evaluation
- **Current**: Evaluates project submissions against rubric tiers.
- **V2 Target**: Multi-channel evidence hierarchy:
  1. Deterministic repository evidence (file tree, dependencies, configs, test suites)
  2. Functional deployed evidence (browser/API verification)
  3. AI semantic evidence review against approved project criteria
  *AI serves as interpreter and evaluator of evidence, not the sole source of truth.*
